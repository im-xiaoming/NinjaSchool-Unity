using System;
using System.Collections.Generic;
using System.Text;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>
    /// The whole game as shared long-term memory of the AI players (offline addition, no Java counterpart): every item
    /// (what it is, how to get it, what using it does and whether this offline server supports using it), monster, map,
    /// NPC, shop, quest and skill becomes one RAG document of the shared owner "*" (AgentRag). The LLM then recalls the
    /// relevant ones for its situation. Built from the dev wiki's records (WikiData.Build), which already join the static
    /// tables (shops, drops, quest steps, NPC positions); built on the RAG worker thread, never on the game thread.
    /// </summary>
    public static class GameKnowledge
    {
        /// <summary>Returns the dev wiki database JSON (set by ServerBootstrap / the tests). Null: no game knowledge.</summary>
        public static Func<string> Source;
        public static volatile int Documents;
        public static volatile string LastError = "";

        // ------------------------------------------------------------------ build

        /// <summary>All documents (key, kind, text). Pure: reads the wiki JSON and the static item templates.</summary>
        public static List<string[]> Build(string json, GameWorld world)
        {
            var docs = new List<string[]>();
            var root = Json.Parse(json) as Dictionary<string, object>;
            var c = root != null ? root["collections"] as Dictionary<string, object> : null;
            if (c == null) return docs;
            var npcs = List(c, "npcs");
            var mobs = List(c, "mobs");
            var maps = List(c, "maps");
            var shops = List(c, "shops");
            // shop -> NPC: the NPC menu entry carries the shop's name ("Dược phẩm (Yên)" opens "Dược phẩm khóa")
            var sellers = new Dictionary<int, string>();
            foreach (var s in shops)
            {
                string name = Str(s, "name"), plain = name.EndsWith(" khóa") ? name.Substring(0, name.Length - 5) : name;
                foreach (var n in npcs)
                    foreach (var m in List(n, "menu"))
                    {
                        string entry = Str(m, "value");
                        if (entry.StartsWith(plain + " (") || entry == plain) { sellers[Int(s, "id")] = Str(n, "name"); break; }
                    }
            }
            var mobLevel = new Dictionary<int, int>();
            foreach (var m in mobs) mobLevel[Int(m, "id")] = Int(m, "level");

            foreach (var it in List(c, "items")) docs.Add(new[] { "item:" + Int(it, "id"), "item", Item(it, world, sellers) });
            foreach (var m in mobs) docs.Add(new[] { "mob:" + Int(m, "id"), "monster", Mob(m) });
            foreach (var m in maps) docs.Add(new[] { "map:" + Int(m, "id"), "map", Map(m, mobLevel) });
            foreach (var n in npcs) docs.Add(new[] { "npc:" + Int(n, "id"), "npc", Npc(n) });
            foreach (var s in shops) docs.Add(new[] { "shop:" + Int(s, "id"), "shop", Shop(s, sellers) });
            foreach (var q in List(c, "quests")) docs.Add(new[] { "quest:" + Int(q, "id"), "quest", Quest(q) });
            foreach (var s in List(c, "skills")) docs.Add(new[] { "skill:" + Int(s, "id"), "skill", Skill(s) });
            return docs;
        }

        /// <summary>Queues every document into the shared RAG memory (unchanged ones are not embedded again).</summary>
        public static void Inject(GameWorld world)
        {
            try
            {
                var source = Source;
                if (source == null || world == null) { LastError = "no wiki data source"; return; }
                var docs = Build(source(), world);
                foreach (var d in docs) AgentRag.Remember("*", d[0], d[1], d[2]);
                Documents = docs.Count;
                LastError = "";
            }
            catch (Exception ex) { LastError = ex.GetType().Name + ": " + ex.Message; }
        }

        // ------------------------------------------------------------------ documents

        private static string Item(Dictionary<string, object> it, GameWorld world, Dictionary<int, string> sellers)
        {
            int id = Int(it, "id"), level = Int(it, "level");
            var b = new StringBuilder();
            b.Append("ITEM ").Append(Str(it, "name")).Append(" (id ").Append(id).Append("): ").Append(Str(it, "typeName"))
             .Append(", level ").Append(level).Append(", ").Append(Gender(Str(it, "gender"))).Append('.');
            string desc = Str(it, "description");
            if (desc.Length > 0) b.Append(" Description: ").Append(desc.TrimEnd('.')).Append('.');
            var get = new List<string>();
            foreach (var s in List(it, "shops"))
            {
                string seller; sellers.TryGetValue(Int(s, "shop"), out seller);
                get.Add("bought at " + (seller != null ? seller + " (" + Str(s, "shopName") + ")" : "the shop " + Str(s, "shopName")) + " for " + Price(s));
            }
            foreach (var d in List(it, "droppedBy"))
                get.Add("dropped by " + Str(d, "mobName") + (Int(d, "quest") >= 0 && d.ContainsKey("quest") ? " during quest " + Int(d, "quest") : ""));
            foreach (var q in List(it, "questPick")) get.Add("collected for quest step " + Str(q, "value"));
            b.Append(" How to get: ").Append(get.Count > 0 ? string.Join("; ", get) : "not sold and not dropped in this offline game (quest reward, event or unavailable)").Append('.');
            var t = world.FindItem(id);
            b.Append(" Use: ").Append(t != null ? UseInfo(world, t) : "unknown").Append('.');
            if (t != null && UsableFromBag(world, t)) b.Append(" Needs level ").Append(level).Append(t.Gender == 0 || t.Gender == 1 ? " and the right gender" : "").Append('.');
            return b.ToString();
        }

        /// <summary>What using the item does in this offline server, and whether it can be used at all (PlayerItems.UseItem).</summary>
        public static string UseInfo(GameWorld world, ItemTemplateData t)
        {
            int id = t.Id;
            if (PlayerService.IsWearable(t)) return "equipment: wear it from the bag (slot " + t.Type + "), it adds its stats; usable offline";
            if (PlayerItems.IsMartialArtBook(id))
            {
                int cls = PlayerSkills.BookClass(world, id);
                return "skill book: read it to learn a skill" + (cls >= 1 ? " of class " + cls + " (only that school)" : "") + "; usable offline";
            }
            if (t.Type == PlayerItems.TypeHp) return "HP potion: restores HP over 3 seconds (one at a time, not when HP is full); usable offline";
            if (t.Type == PlayerItems.TypeMp) return "MP potion: restores MP at once; usable offline";
            if (t.Type == PlayerItems.TypeEat)
            {
                int param, minutes;
                if (Food(id, out param, out minutes)) return "food: +" + param + " HP and MP every half second for " + (minutes >= 120 ? minutes / 60 + " hours" : minutes + " minutes") + " (a new serving replaces the running one); usable offline";
                return "food without a ported effect; using it does nothing offline";
            }
            switch (id)
            {
                case PlayerItemUse.ItemBachBien: case PlayerItemUse.ItemVoHanBachBien: return "teleports back to the village saved at Kamakura; usable offline";
                case PlayerItemUse.ItemCanCauVang: return "golden fishing rod; usable offline";
                case PlayerItemUse.ItemThienBienLenh: return "Thiên biến lệnh; usable offline";
                case PlayerItemUse.ItemPhucNang: return "Phúc nang nhẫn giả: open it for a random reward; usable offline";
                case PlayerItemUse.ItemManhGiayVun: return "scrap paper (combine/use as in the original); usable offline";
                case PlayerItemUse.ItemSachKyNangSo: return "gives skill points; usable offline";
                case PlayerItemUse.ItemSachTiemNangSo: return "gives potential points; usable offline";
                case PlayerItemUse.ItemSachPhongLoi: case PlayerItemUse.ItemSachBangHoa: return "special skill book; usable offline";
                case 240: return "resets the potential points; usable offline";
                case 241: return "resets the skill points; usable offline";
                case 248: return "Nấm linh chi: double experience for 5 hours; usable offline";
                case 254: case 255: case 256: return "clears the experience lost by dying; usable offline";
                case 257: return "clears hiếu chiến points (always 0 offline); usable offline";
                case TaskOrders.ItemTaThuLenh: return "mission order: starts a Tà thú mission; usable offline";
                case TaskMechanics.ItemChiaKhoaCoQuan: return "key: opens the mechanism box during its quest; usable offline";
                case TaskMechanics.ItemBinhRong: return "empty bottle: fill it with water during its quest; usable offline";
            }
            if (id >= TaskMechanics.ItemDiaDo2 && id <= TaskMechanics.ItemDiaDo4) return "treasure map: use it at the marked place; usable offline";
            if (t.Type == PlayerItems.TypeTask || t.Type == PlayerItems.TypeTaskSave || t.Type == PlayerItems.TypeTaskWait)
                return "quest item: kept for a quest step (bring or show it), not used from the bag";
            if (t.Type == 26) return "material (stone): used at an NPC to upgrade or craft, not used from the bag";
            return "no use from the bag is ported in this offline version (using it does nothing)";
        }

        private static bool UsableFromBag(GameWorld world, ItemTemplateData t)
        {
            return !UseInfo(world, t).Contains("does nothing") && !UseInfo(world, t).StartsWith("quest item") && !UseInfo(world, t).StartsWith("material");
        }

        // PlayerItems.UseItem (TypeEat): HP/MP per half second and duration
        private static bool Food(int id, out int param, out int minutes)
        {
            switch (id)
            {
                case 23: param = 3; minutes = 30; return true;
                case 24: param = 20; minutes = 30; return true;
                case 25: param = 30; minutes = 30; return true;
                case 26: param = 40; minutes = 30; return true;
                case 27: param = 50; minutes = 30; return true;
                case 29: param = 60; minutes = 30; return true;
                case 30: param = 60; minutes = 4320; return true;
                case 249: param = 40; minutes = 4320; return true;
                case 250: param = 50; minutes = 4320; return true;
                case 409: param = 150; minutes = 1440; return true;
                case 410: param = 200; minutes = 1440; return true;
                case 567: param = 300; minutes = 1440; return true;
            }
            param = minutes = 0;
            return false;
        }

        private static string Mob(Dictionary<string, object> m)
        {
            var b = new StringBuilder();
            b.Append("MONSTER ").Append(Str(m, "name")).Append(" (id ").Append(Int(m, "id")).Append("): level ").Append(Int(m, "level"))
             .Append(", HP ").Append(Int(m, "hp")).Append(Bool(m, "boss") ? ", BOSS" : "").Append('.');
            var where = new List<string>();
            foreach (var x in List(m, "maps")) where.Add(Str(x, "name") + " (map " + Int(x, "id") + ")");
            b.Append(" Lives in: ").Append(where.Count > 0 ? string.Join(", ", where) : "no normal map").Append('.');
            var kill = List(m, "questKill");
            if (kill.Count > 0) b.Append(" Killed for quest steps ").Append(Join(kill)).Append('.');
            var drops = List(m, "questDrops");
            if (drops.Count > 0) b.Append(" Drops quest items for steps ").Append(Join(drops)).Append('.');
            b.Append(" Train on monsters from your level to about 3 above; 10 levels below give no experience. Monsters drop yen (about one kill in ten), sometimes stones and potions.");
            return b.ToString();
        }

        private static string Map(Dictionary<string, object> m, Dictionary<int, int> mobLevel)
        {
            var b = new StringBuilder();
            b.Append("MAP ").Append(Str(m, "name")).Append(" (id ").Append(Int(m, "id")).Append("): ").Append(Str(m, "typeName"));
            int unlock = m.ContainsKey("unlockQuest") ? Int(m, "unlockQuest") : -1;
            b.Append(unlock > 0 ? ", opens after quest " + unlock : "").Append('.');
            var mobs = new List<string>();
            foreach (var x in List(m, "mobs")) { int lv; mobLevel.TryGetValue(Int(x, "id"), out lv); mobs.Add(Str(x, "name") + " level " + lv); }
            if (mobs.Count > 0) b.Append(" Monsters: ").Append(string.Join(", ", mobs)).Append('.');
            var npcs = new List<string>();
            foreach (var x in List(m, "npcs")) { string n = Str(x, "name") + " (" + Int(x, "x") + "," + Int(x, "y") + ")"; if (!npcs.Contains(n)) npcs.Add(n); }
            if (npcs.Count > 0) b.Append(" NPCs: ").Append(string.Join(", ", npcs)).Append('.');
            var exits = new List<string>();
            foreach (var x in List(m, "exits")) exits.Add(Str(x, "mapName") + " (map " + Int(x, "map") + ")");
            if (exits.Count > 0) b.Append(" Exits to: ").Append(string.Join(", ", exits)).Append('.');
            return b.ToString();
        }

        private static string Npc(Dictionary<string, object> n)
        {
            var b = new StringBuilder();
            b.Append("NPC ").Append(Str(n, "name")).Append(" (id ").Append(Int(n, "id")).Append(").");
            var menu = List(n, "menu");
            if (menu.Count > 0) b.Append(" Menu: ").Append(Join(menu, "; ")).Append('.');
            var gives = List(n, "questGiver");
            if (gives.Count > 0) b.Append(" Gives quests ").Append(Join(gives)).Append('.');
            var where = new List<string>();
            foreach (var x in List(n, "maps")) where.Add(Str(x, "mapName") + " (map " + Int(x, "map") + " at " + Int(x, "x") + "," + Int(x, "y") + ")");
            if (where.Count > 0) b.Append(" Found in: ").Append(string.Join(", ", where)).Append('.');
            if (Str(n, "status") != "done") b.Append(" Some of its features are not ported offline.");
            return b.ToString();
        }

        private static string Shop(Dictionary<string, object> s, Dictionary<int, string> sellers)
        {
            string seller;
            sellers.TryGetValue(Int(s, "id"), out seller);
            var b = new StringBuilder();
            b.Append("SHOP ").Append(Str(s, "name")).Append(" (shop ").Append(Int(s, "id")).Append(')')
             .Append(seller != null ? ", opened at NPC " + seller : "").Append(". Shops open after quest 3. Sells: ");
            var items = new List<string>();
            foreach (var x in List(s, "items")) items.Add(Str(x, "name") + " " + Price(x));
            b.Append(string.Join(", ", items)).Append('.');
            if (Str(s, "name").EndsWith(" khóa")) b.Append(" Items bought with yen are locked (cannot be traded).");
            return b.ToString();
        }

        private static string Quest(Dictionary<string, object> q)
        {
            var b = new StringBuilder();
            b.Append("QUEST ").Append(Int(q, "id")).Append(' ').Append(Str(q, "name")).Append(": from ").Append(Str(q, "giverNpcName"))
             .Append(", level ").Append(Int(q, "levelRequire")).Append(", reward ").Append(Int(q, "rewardExp")).Append(" exp and ")
             .Append(Int(q, "rewardYen")).Append(" yen.");
            var steps = new List<string>();
            foreach (var s in List(q, "steps"))
            {
                string step = (Int(s, "index") + 1) + ". " + Str(s, "name");
                if (Int(s, "count") > 0) step += " x" + Int(s, "count");
                if (Str(s, "npcName").Length > 0) step += " (NPC " + Str(s, "npcName") + ")";
                if (Str(s, "pickItemName").Length > 0) step += " (item " + Str(s, "pickItemName") + ")";
                if (Str(s, "droppedByName").Length > 0) step += " (dropped by " + Str(s, "droppedByName") + ")";
                steps.Add(step);
            }
            if (steps.Count > 0) b.Append(" Steps: ").Append(string.Join("; ", steps)).Append('.');
            string detail = Str(q, "detail");
            if (detail.Length > 0) b.Append(' ').Append(detail.TrimEnd('.')).Append('.');
            if (Int(q, "unsupportedSteps") > 0) b.Append(" Some steps are not ported offline.");
            return b.ToString();
        }

        private static string Skill(Dictionary<string, object> s)
        {
            var b = new StringBuilder();
            b.Append("SKILL ").Append(Str(s, "name")).Append(" (id ").Append(Int(s, "id")).Append("): ").Append(Str(s, "className"))
             .Append(", ").Append(Str(s, "typeName")).Append(", up to ").Append(Int(s, "maxPoint")).Append(" points.");
            var levels = List(s, "levels");
            if (levels.Count > 0)
            {
                var first = levels[0] as Dictionary<string, object>;
                var last = levels[levels.Count - 1] as Dictionary<string, object>;
                b.Append(" From level ").Append(Int(first, "level")).Append("; mana ").Append(Int(first, "mana")).Append('-').Append(Int(last, "mana"))
                 .Append(", cooldown ").Append(Int(first, "cooldown")).Append('-').Append(Int(last, "cooldown")).Append(" ms, up to ")
                 .Append(Int(last, "targets")).Append(" targets.");
            }
            string desc = Str(s, "description");
            if (desc.Length > 0) b.Append(' ').Append(desc.TrimEnd('.')).Append('.');
            return b.ToString();
        }

        // ------------------------------------------------------------------ JSON helpers

        private static List<Dictionary<string, object>> List(Dictionary<string, object> d, string key)
        {
            var list = new List<Dictionary<string, object>>();
            object o;
            if (d == null || !d.TryGetValue(key, out o) || !(o is List<object>)) return list;
            foreach (var x in (List<object>)o)
                list.Add(x as Dictionary<string, object> ?? new Dictionary<string, object> { { "value", x } });
            return list;
        }

        private static string Join(List<Dictionary<string, object>> values, string sep = ", ")
        {
            var parts = new List<string>();
            foreach (var v in values) parts.Add(Str(v, "value"));
            return string.Join(sep, parts);
        }

        private static string Str(Dictionary<string, object> d, string key)
        {
            object o;
            return d != null && d.TryGetValue(key, out o) && o != null ? Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture) : "";
        }

        private static int Int(Dictionary<string, object> d, string key)
        {
            object o;
            if (d == null || !d.TryGetValue(key, out o) || o == null) return -1;
            try { return Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture); } catch { return -1; }
        }

        private static bool Bool(Dictionary<string, object> d, string key)
        {
            object o;
            return d != null && d.TryGetValue(key, out o) && o is bool && (bool)o;
        }

        private static string Price(Dictionary<string, object> s)
        {
            var p = new List<string>();
            if (Int(s, "yen") > 0) p.Add(Int(s, "yen") + " yen");
            if (Int(s, "xu") > 0) p.Add(Int(s, "xu") + " xu");
            if (Int(s, "luong") > 0) p.Add(Int(s, "luong") + " lượng");
            return p.Count > 0 ? string.Join(" / ", p) : "free";
        }

        private static string Gender(string g) { return g == "cả hai" || g.Length == 0 ? "both genders" : "gender " + g; }
    }
}
