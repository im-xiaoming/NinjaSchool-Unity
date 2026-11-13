using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.DevWiki
{
    // Builds the dev wiki database: one JSON object { generatedAt, collections: { name: [documents] } }.
    // Every document has an "id" and flat, searchable fields (NoSQL style), so a bug can be looked up by id/name
    // without reading the code.
    //   - generated collections come from the static tables (tables.txt) and from the ported code (PlayerTasks...),
    //     so they always match what the server really uses;
    //   - hand-written collections (features, constraints, commands, config, issues...) come from notes.json
    //     (StreamingAssets/NsoWiki/notes.json) and are merged in as they are.
    // No UnityEngine here: the same code runs in Tools/ServerTests (`dotnet run -- wiki`).
    public static class WikiData
    {
        // ItemTemplate.TYPE_* names
        private static readonly string[] ItemTypeNames =
        {
            "Nón",
            "Vũ khí",
            "Áo",
            "Dây chuyền",
            "Găng tay",
            "Nhẫn",
            "Quần",
            "Ngọc bội",
            "Giày",
            "Phù",
            "Thú nuôi",
            "Mặt nạ",
            "Áo choàng",
            "Bao tay",
            "Mắt thần",
            "Bí kíp",
            "Thuốc HP",
            "Thuốc MP",
            "Thức ăn",
            "Tiền",
            "Túi tiền",
            "Thịt",
            "Ngọc rồng",
            "VP nhiệm vụ (lưu)",
            "VP nhiệm vụ (chờ)",
            "VP nhiệm vụ",
            "Đá / huyền tinh",
            "Khác",
            "Bảo hiểm",
            "Thú cưỡi 0",
            "Thú cưỡi 1",
            "Thú cưỡi 2",
            "Thú cưỡi 3",
            "Thú cưỡi 4",
            "Ngọc khảm"
        };
        private static readonly string[] SkillTypeNames =
        {
            "Tự động (bị động)",
            "Tấn công",
            "Buff",
            "NPC",
            "Hồi sinh"
        };
        private static readonly string[] MapTypeNames =
        {
            "Thường",
            "?",
            "Hang động / đặc biệt",
            "Chiến trường / sự kiện",
            "Khác"
        };
        public static string Build(Dictionary<string, SqlTable> tables, GameWorld world, string notesPath)
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Prop("generatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            w.Key("collections");
            w.BeginObject();
            var ctx = new Context(tables, world);
            w.Key("quests");
            Quests(w, ctx);
            w.Key("npcs");
            Npcs(w, ctx);
            w.Key("maps");
            Maps(w, ctx);
            w.Key("mobs");
            Mobs(w, ctx);
            w.Key("items");
            Items(w, ctx, notesPath == null ? null : Path.Combine(Path.GetDirectoryName(notesPath), "..", "NsoStatic", "Data"));
            w.Key("shops");
            Shops(w, ctx);
            w.Key("skills");
            Skills(w, ctx);
            w.Key("effects");
            Simple(w, tables["effect"], "id", new[] { "name", "type", "icon" });
            w.Key("effectAnimations");
            w.BeginArray();
            // GameScr.readEfect: retain original image order and signed frame offsets.
            var animations = tables["nj_effect"];
            foreach (var row in animations.Rows)
            {
                w.BeginObject();
                w.Prop("id", int.Parse(row[animations.Col("id")]));
                w.Key("frames");
                w.Value(Json.Parse(row[animations.Col("info")]));
                w.EndObject();
            }

            w.EndArray();
            w.Key("itemOptions");
            Simple(w, tables["item_option"], "id", new[] { "name", "type" });
            w.Key("classes");
            Simple(w, tables["clazz"], "id", new[] { "name" });
            // hand-written collections, merged raw (each value must be a JSON array of documents)
            if (notesPath != null && File.Exists(notesPath))
            {
                var notes = Json.Parse(File.ReadAllText(notesPath, new UTF8Encoding(false))) as Dictionary<string, object>;
                if (notes != null)
                    foreach (var kv in notes)
                    {
                        if (kv.Key.StartsWith("_", StringComparison.Ordinal))
                            continue; // "_comment" etc.
                        w.Key(kv.Key);
                        w.Value(kv.Value);
                    }
            }

            w.EndObject();
            w.EndObject();
            return w.ToString();
        }

        // ---------------------------------------------------------------- lookups shared by the collections
        private sealed class Context
        {
            public readonly Dictionary<string, SqlTable> T;
            public readonly GameWorld World;
            public readonly Dictionary<int, string> ItemName = new Dictionary<int, string>();
            public readonly Dictionary<int, string> NpcName = new Dictionary<int, string>();
            public readonly Dictionary<int, string> MobName = new Dictionary<int, string>();
            public readonly Dictionary<int, string> MapName = new Dictionary<int, string>();
            public readonly Dictionary<int, string> ShopName = new Dictionary<int, string>();
            public readonly Dictionary<int, string> ClassName = new Dictionary<int, string>();
            public readonly List<int[]> TaskDrops = PlayerTasks.TaskDropList(); // [mob, quest, step, item]
            public readonly HashSet<int> PortedNpcs = new HashSet<int>(PlayerTasks.PortedNpcMenus);
            public Context(Dictionary<string, SqlTable> t, GameWorld world)
            {
                T = t;
                World = world;
                Names(t["item"], ItemName);
                Names(t["npc"], NpcName);
                Names(t["monster"], MobName);
                Names(t["map"], MapName);
                Names(t["stores"], ShopName);
                Names(t["clazz"], ClassName);
            }

            private static void Names(SqlTable t, Dictionary<int, string> d)
            {
                int id = t.Col("id"), name = t.Col("name");
                foreach (var r in t.Rows)
                    d[int.Parse(r[id])] = r[name] ?? "";
            }

            public string Name(Dictionary<int, string> d, int id)
            {
                string s;
                return d.TryGetValue(id, out s) ? s : null;
            }
        }

        private static List<object> JsonList(string s)
        {
            if (string.IsNullOrEmpty(s))
                return new List<object>();
            try
            {
                return Json.Parse(s) as List<object> ?? new List<object>();
            }
            catch
            {
                return new List<object>();
            }
        }

        private static int Int(object o)
        {
            return (int)Json.Long(o);
        }

        private static int Field(Dictionary<string, object> d, string k)
        {
            object o;
            return d.TryGetValue(k, out o) ? Int(o) : 0;
        }

        // ---------------------------------------------------------------- quests
        private static void Quests(JsonWriter w, Context c)
        {
            w.BeginArray();
            for (int id = 0;; id++)
            {
                var t = c.World.FindTask(id);
                if (t == null)
                    break;
                var npcs = c.World.TaskNpcs(id) ?? new short[0];
                w.BeginObject();
                w.Prop("id", id);
                w.Prop("name", t.Name);
                w.Prop("detail", t.Detail);
                w.Prop("levelRequire", t.LevelRequire);
                int giver = npcs.Length > 0 ? npcs[0] : -1;
                w.Prop("giverNpc", giver);
                w.Prop("giverNpcName", giver == -1 ? "Thầy/cô của trường" : c.Name(c.NpcName, giver));
                var reward = PlayerTasks.RewardOf(id);
                if (reward != null)
                {
                    w.Prop("rewardExp", reward[0]);
                    w.Prop("rewardYen", reward[1]);
                }

                int unsupported = 0;
                w.Key("steps");
                w.BeginArray();
                for (int i = 0; i < t.SubNames.Length; i++)
                {
                    w.BeginObject();
                    w.Prop("index", i);
                    w.Prop("name", t.SubNames[i]);
                    if (i < t.Counts.Length)
                        w.Prop("count", t.Counts[i]);
                    // npc index i+1 = the NPC to see for step i (-1 = the class teacher, -2 = none)
                    int npc = i + 1 < npcs.Length ? npcs[i + 1] : -2;
                    w.Prop("npc", npc);
                    w.Prop("npcName", npc == -1 ? "Thầy/cô của trường" : c.Name(c.NpcName, npc));
                    if (i < t.KillMobs.Length && t.KillMobs[i][0] >= 0)
                    {
                        w.Prop("killMob", t.KillMobs[i][0]);
                        w.Prop("killMobName", c.Name(c.MobName, t.KillMobs[i][0]));
                        if (t.KillMobs[i][1] != 0)
                            w.Prop("killMobLevelBoss", t.KillMobs[i][1]);
                    }

                    if (i < t.PickItems.Length && t.PickItems[i] >= 0)
                    {
                        w.Prop("pickItem", t.PickItems[i]);
                        w.Prop("pickItemName", c.Name(c.ItemName, t.PickItems[i]));
                        foreach (var d in c.TaskDrops)
                            if (d[1] == id && d[2] == i)
                            {
                                w.Prop("droppedBy", d[0]);
                                w.Prop("droppedByName", c.Name(c.MobName, d[0]));
                            }
                    }

                    bool ok = PlayerTasks.IsStepSupported(c.World, t, i);
                    if (!ok)
                        unsupported++;
                    w.Prop("status", ok ? "done" : "skip"); // skip = mechanic not ported, "Bỏ qua" entry at the giver
                    w.EndObject();
                }

                w.EndArray();
                w.Prop("unsupportedSteps", unsupported);
                w.Prop("status", unsupported == 0 ? "done" : "partial");
                w.EndObject();
            }

            w.EndArray();
        }

        // ---------------------------------------------------------------- npcs
        private static void Npcs(JsonWriter w, Context c)
        {
            // where each NPC stands
            var places = new Dictionary<int, List<int[]>>();
            var map = c.T["map"];
            int mid = map.Col("id"), mnpc = map.Col("npc");
            foreach (var r in map.Rows)
                foreach (var o in JsonList(r[mnpc]))
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null)
                        continue;
                    int tpl = Field(d, "templateId");
                    List<int[]> l;
                    if (!places.TryGetValue(tpl, out l))
                        places[tpl] = l = new List<int[]>();
                    l.Add(new[] { int.Parse(r[mid]), Field(d, "x"), Field(d, "y") });
                }

            // quests where the NPC is the giver or a step target
            var giverOf = new Dictionary<int, List<int>>();
            var stepOf = new Dictionary<int, List<string>>();
            for (int id = 0; c.World.FindTask(id) != null; id++)
            {
                var npcs = c.World.TaskNpcs(id) ?? new short[0];
                for (int i = 0; i < npcs.Length; i++)
                {
                    if (npcs[i] < 0)
                        continue;
                    if (i == 0)
                        Add(giverOf, npcs[i], id);
                    else
                        Add(stepOf, npcs[i], id + "." + (i - 1));
                }
            }

            var t = c.T["npc"];
            int nid = t.Col("id"), nname = t.Col("name"), nmenu = t.Col("menu");
            w.BeginArray();
            foreach (var r in t.Rows)
            {
                int id = int.Parse(r[nid]);
                w.BeginObject();
                w.Prop("id", id);
                w.Prop("name", r[nname]);
                w.Prop("status", c.PortedNpcs.Contains(id) ? "done" : "todo"); // done = menu hand-ported in PlayerTasks.InitMenu
                // menu: [["Title","sub1","sub2"], ...] -> "Title (sub1, sub2)"
                w.Key("menu");
                w.BeginArray();
                foreach (var o in JsonList(r[nmenu]))
                {
                    var entry = o as List<object>;
                    if (entry == null || entry.Count == 0)
                        continue;
                    string s = Convert.ToString(entry[0]);
                    if (entry.Count > 1)
                    {
                        var subs = new List<string>();
                        for (int i = 1; i < entry.Count; i++)
                            subs.Add(Convert.ToString(entry[i]));
                        s += " (" + string.Join(", ", subs) + ")";
                    }

                    w.Value(s);
                }

                w.EndArray();
                List<int[]> pl;
                w.Key("maps");
                w.BeginArray();
                if (places.TryGetValue(id, out pl))
                    foreach (var p in pl)
                    {
                        w.BeginObject();
                        w.Prop("map", p[0]);
                        w.Prop("mapName", c.Name(c.MapName, p[0]));
                        w.Prop("x", p[1]);
                        w.Prop("y", p[2]);
                        w.EndObject();
                    }

                w.EndArray();
                List<int> g;
                List<string> st;
                w.Key("questGiver");
                w.Value(giverOf.TryGetValue(id, out g) ? (object)g : new List<int>());
                w.Key("questSteps");
                w.Value(stepOf.TryGetValue(id, out st) ? (object)st : new List<string>());
                w.EndObject();
            }

            w.EndArray();
        }

        private static void Add<T>(Dictionary<int, List<T>> d, int k, T v)
        {
            List<T> l;
            if (!d.TryGetValue(k, out l))
                d[k] = l = new List<T>();
            if (!l.Contains(v))
                l.Add(v);
        }

        // ---------------------------------------------------------------- maps
        private static void Maps(JsonWriter w, Context c)
        {
            // MapUnlock: first quest id at which the map can be entered
            var unlock = new Dictionary<int, int>();
            for (int task = 0; task <= 60; task++)
                foreach (int m in MapUnlock.Allowed(task))
                    if (!unlock.ContainsKey(m))
                        unlock[m] = task;
            var t = c.T["map"];
            int id = t.Col("id"), name = t.Col("name"), npc = t.Col("npc"), wp = t.Col("waypoint"), mob = t.Col("monster"), zones = t.Col("zone_number"), type = t.Col("type");
            w.BeginArray();
            foreach (var r in t.Rows)
            {
                int mapId = int.Parse(r[id]), mt = int.Parse(r[type]);
                w.BeginObject();
                w.Prop("id", mapId);
                w.Prop("name", r[name]);
                w.Prop("type", mt);
                w.Prop("typeName", mt >= 0 && mt < MapTypeNames.Length ? MapTypeNames[mt] : "?");
                w.Prop("zones", int.Parse(r[zones]));
                w.Prop("status", mt == 0 ? "done" : "todo"); // only ordinary maps are playable offline
                int u;
                if (mapId >= 72)
                    w.Prop("unlockQuest", 0);
                else if (unlock.TryGetValue(mapId, out u))
                    w.Prop("unlockQuest", u);
                var xy = PlayerTasks.VillageXY(mapId);
                // VillageXY answers {35,100} for any map that is not a village or school: only real spawn points are listed
                if (xy != null && !(xy[0] == 35 && xy[1] == 100))
                {
                    w.Prop("villageX", xy[0]);
                    w.Prop("villageY", xy[1]);
                }

                w.Key("npcs");
                w.BeginArray();
                foreach (var o in JsonList(r[npc]))
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null)
                        continue;
                    int tpl = Field(d, "templateId");
                    w.BeginObject();
                    w.Prop("id", tpl);
                    w.Prop("name", c.Name(c.NpcName, tpl));
                    w.Prop("x", Field(d, "x"));
                    w.Prop("y", Field(d, "y"));
                    w.EndObject();
                }

                w.EndArray();
                // monsters grouped by template
                var count = new SortedDictionary<int, int>();
                foreach (var o in JsonList(r[mob]))
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null)
                        continue;
                    int tpl = Field(d, "templateId");
                    int n;
                    count.TryGetValue(tpl, out n);
                    count[tpl] = n + 1;
                }

                w.Key("mobs");
                w.BeginArray();
                foreach (var kv in count)
                {
                    w.BeginObject();
                    w.Prop("id", kv.Key);
                    w.Prop("name", c.Name(c.MobName, kv.Key));
                    w.Prop("count", kv.Value);
                    w.EndObject();
                }

                w.EndArray();
                w.Key("exits");
                w.BeginArray();
                foreach (var o in JsonList(r[wp]))
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null)
                        continue;
                    int next = Field(d, "next");
                    w.BeginObject();
                    w.Prop("map", next);
                    w.Prop("mapName", c.Name(c.MapName, next));
                    w.Prop("minX", Field(d, "minX"));
                    w.Prop("minY", Field(d, "minY"));
                    w.Prop("maxX", Field(d, "maxX"));
                    w.Prop("maxY", Field(d, "maxY"));
                    w.EndObject();
                }

                w.EndArray();
                w.EndObject();
            }

            w.EndArray();
        }

        // ---------------------------------------------------------------- monsters
        private static void Mobs(JsonWriter w, Context c)
        {
            var onMaps = new Dictionary<int, List<int>>();
            var map = c.T["map"];
            int mid = map.Col("id"), mmob = map.Col("monster");
            foreach (var r in map.Rows)
                foreach (var o in JsonList(r[mmob]))
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null)
                        continue;
                    Add(onMaps, Field(d, "templateId"), int.Parse(r[mid]));
                }

            var killFor = new Dictionary<int, List<string>>();
            for (int id = 0; c.World.FindTask(id) != null; id++)
            {
                var t = c.World.FindTask(id);
                for (int i = 0; i < t.KillMobs.Length; i++)
                    if (t.KillMobs[i][0] >= 0)
                        Add(killFor, t.KillMobs[i][0], id + "." + i);
            }

            var tab = c.T["monster"];
            int cid = tab.Col("id"), name = tab.Col("name"), level = tab.Col("level"), boss = tab.Col("boss"), type = tab.Col("type"), hp = tab.Col("hp"), range = tab.Col("range_move"), speed = tab.Col("speed");
            w.BeginArray();
            foreach (var r in tab.Rows)
            {
                int id = int.Parse(r[cid]);
                w.BeginObject();
                w.Prop("id", id);
                w.Prop("name", r[name]);
                w.Prop("level", int.Parse(r[level]));
                w.Prop("hp", long.Parse(r[hp]));
                w.Prop("boss", r[boss] == "1" || r[boss] == "true");
                w.Prop("type", int.Parse(r[type]));
                w.Prop("rangeMove", int.Parse(r[range]));
                w.Prop("speed", int.Parse(r[speed]));
                List<int> ml;
                List<string> kl;
                w.Key("maps");
                w.BeginArray();
                if (onMaps.TryGetValue(id, out ml))
                    foreach (int m in ml)
                    {
                        w.BeginObject();
                        w.Prop("id", m);
                        w.Prop("name", c.Name(c.MapName, m));
                        w.EndObject();
                    }

                w.EndArray();
                w.Key("questKill");
                w.Value(killFor.TryGetValue(id, out kl) ? (object)kl : new List<string>());
                w.Key("questDrops");
                w.BeginArray();
                foreach (var d in c.TaskDrops)
                    if (d[0] == id)
                    {
                        w.BeginObject();
                        w.Prop("item", d[3]);
                        w.Prop("itemName", c.Name(c.ItemName, d[3]));
                        w.Prop("quest", d[1]);
                        w.Prop("step", d[2]);
                        w.EndObject();
                    }

                w.EndArray();
                w.EndObject();
            }

            w.EndArray();
        }

        // ---------------------------------------------------------------- items
        private static void Items(JsonWriter w, Context c, string projectArtDirectory)
        {
            var soldIn = new Dictionary<int, List<int[]>>(); // item -> [shop, xu, yen, luong, lock]
            var sd = c.T["store_data"];
            int sItem = sd.Col("item_id"), sStore = sd.Col("store"), sCoin = sd.Col("coin"), sYen = sd.Col("yen"), sGold = sd.Col("gold"), sLock = sd.Col("lock");
            foreach (var r in sd.Rows)
            {
                int item = int.Parse(r[sItem]);
                List<int[]> l;
                if (!soldIn.TryGetValue(item, out l))
                    soldIn[item] = l = new List<int[]>();
                l.Add(new[] { int.Parse(r[sStore]), int.Parse(r[sCoin]), int.Parse(r[sYen]), int.Parse(r[sGold]), r[sLock] == "1" || r[sLock] == "true" ? 1 : 0 });
            }

            var pickFor = new Dictionary<int, List<string>>();
            for (int id = 0; c.World.FindTask(id) != null; id++)
            {
                var t = c.World.FindTask(id);
                for (int i = 0; i < t.PickItems.Length; i++)
                    if (t.PickItems[i] >= 0)
                        Add(pickFor, t.PickItems[i], id + "." + i);
            }

            var tab = c.T["item"];
            int cid = tab.Col("id"), name = tab.Col("name"), type = tab.Col("type"), gender = tab.Col("gender"), desc = tab.Col("description"), level = tab.Col("level"), icon = tab.Col("icon"), part = tab.Col("part"), fashion = tab.Col("fashion"), up = tab.Col("isUpToUp");
            w.BeginArray();
            foreach (var r in tab.Rows)
            {
                int id = int.Parse(r[cid]), ty = int.Parse(r[type]), g = int.Parse(r[gender]);
                w.BeginObject();
                w.Prop("id", id);
                w.Prop("name", r[name]);
                w.Prop("type", ty);
                w.Prop("typeName", ty >= 0 && ty < ItemTypeNames.Length ? ItemTypeNames[ty] : "?");
                w.Prop("gender", g == 0 ? "nữ" : g == 1 ? "nam" : "cả hai");
                w.Prop("level", int.Parse(r[level]));
                w.Prop("description", r[desc] ?? "");
                w.Prop("icon", int.Parse(r[icon]));
                // Project lookup includes every zoom; the wiki preview and running game may use different zooms.
                var spriteFiles = ItemSpriteProjectFiles(projectArtDirectory, int.Parse(r[icon]));
                string preferredSprite = null;
                foreach (var file in spriteFiles)
                    if ((bool)file["exists"])
                        preferredSprite = (string)file["path"];
                w.Prop("spritePath", preferredSprite); // Compatibility for existing API consumers.
                w.Key("spriteFiles");
                w.Value(spriteFiles);
                w.Prop("part", int.Parse(r[part]));
                w.Prop("fashion", int.Parse(r[fashion]));
                w.Prop("stackable", r[up] == "1" || r[up] == "true");
                List<int[]> sl;
                List<string> pl;
                w.Key("shops");
                w.BeginArray();
                if (soldIn.TryGetValue(id, out sl))
                    foreach (var s in sl)
                    {
                        w.BeginObject();
                        w.Prop("shop", s[0]);
                        w.Prop("shopName", c.Name(c.ShopName, s[0]));
                        w.Prop("xu", s[1]);
                        w.Prop("yen", s[2]);
                        w.Prop("luong", s[3]);
                        w.Prop("locked", s[4] == 1);
                        w.EndObject();
                    }

                w.EndArray();
                w.Key("questPick");
                w.Value(pickFor.TryGetValue(id, out pl) ? (object)pl : new List<string>());
                w.Key("droppedBy");
                w.BeginArray();
                foreach (var d in c.TaskDrops)
                    if (d[3] == id)
                    {
                        w.BeginObject();
                        w.Prop("mob", d[0]);
                        w.Prop("mobName", c.Name(c.MobName, d[0]));
                        w.Prop("quest", d[1]);
                        w.Prop("step", d[2]);
                        w.EndObject();
                    }

                w.EndArray();
                w.EndObject();
            }

            w.EndArray();
        }

        private static List<Dictionary<string, object>> ItemSpriteProjectFiles(string directory, int icon)
        {
            var files = new List<Dictionary<string, object>>();
            if (directory == null || icon < 0)
                return files;
            for (int zoom = 1; zoom <= 4; zoom++)
            {
                string relative = "Img/Small/" + zoom + "/Small" + icon + ".png";
                string full = Path.Combine(directory, relative);
                bool exists = File.Exists(full);
                int width = 0, height = 0;
                if (exists)
                {
                    // Read only the PNG header; listing paths must not decode/load every texture.
                    try
                    {
                        using (var stream = File.OpenRead(full))
                        {
                            var header = new byte[24];
                            int count = 0, read;
                            while (count < header.Length && (read = stream.Read(header, count, header.Length - count)) > 0)
                                count += read;
                            if (count == header.Length && header[0] == 137 && header[1] == 80)
                            {
                                width = header[16] << 24 | header[17] << 16 | header[18] << 8 | header[19];
                                height = header[20] << 24 | header[21] << 16 | header[22] << 8 | header[23];
                            }
                        }
                    }
                    catch (IOException)
                    {
                    } // Asset may be replaced while the wiki is rebuilding.
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                files.Add(new Dictionary<string, object> { { "zoom", zoom }, { "path", "Assets/StreamingAssets/NsoStatic/Data/" + relative }, { "fullPath", Path.GetFullPath(full) }, { "exists", exists }, { "width", width }, { "height", height } });
            }

            return files;
        }

        // ---------------------------------------------------------------- shops
        private static void Shops(JsonWriter w, Context c)
        {
            w.BeginArray();
            foreach (var kv in c.ShopName)
            {
                var list = c.World.Store(kv.Key);
                w.BeginObject();
                w.Prop("id", kv.Key);
                w.Prop("name", kv.Value);
                w.Prop("itemCount", list == null ? 0 : list.Count);
                w.Key("items");
                w.BeginArray();
                if (list != null)
                    foreach (var s in list)
                    {
                        w.BeginObject();
                        w.Prop("item", s.ItemId);
                        w.Prop("name", c.Name(c.ItemName, s.ItemId));
                        w.Prop("xu", s.Coin);
                        w.Prop("yen", s.Yen);
                        w.Prop("luong", s.Gold);
                        w.Prop("locked", s.IsLock);
                        w.EndObject();
                    }

                w.EndArray();
                w.EndObject();
            }

            w.EndArray();
        }

        // ---------------------------------------------------------------- skills
        private static void Skills(JsonWriter w, Context c)
        {
            var tab = c.T["skill_template"];
            int id = tab.Col("id"), cls = tab.Col("class"), name = tab.Col("name"), max = tab.Col("max_point"), type = tab.Col("type"), desc = tab.Col("description");
            w.BeginArray();
            foreach (var r in tab.Rows)
            {
                int tpl = int.Parse(r[id]), ty = int.Parse(r[type]), cl = int.Parse(r[cls]);
                w.BeginObject();
                w.Prop("id", tpl);
                w.Prop("name", r[name]);
                w.Prop("class", cl);
                w.Prop("className", c.Name(c.ClassName, cl));
                w.Prop("type", ty);
                w.Prop("typeName", ty >= 0 && ty < SkillTypeNames.Length ? SkillTypeNames[ty] : "?");
                w.Prop("maxPoint", int.Parse(r[max]));
                w.Prop("description", r[desc] ?? "");
                w.Prop("status", ty == PlayerSkills.TypeAttack || ty == PlayerSkills.TypeAuto ? "done" : "todo"); // buff/npc/live not ported
                w.Key("levels");
                w.BeginArray();
                for (int p = 0; p <= int.Parse(r[max]); p++)
                {
                    var s = c.World.FindSkill(tpl, p);
                    if (s == null)
                        continue;
                    w.BeginObject();
                    w.Prop("point", p);
                    w.Prop("level", s.Level);
                    w.Prop("mana", s.ManaUse);
                    w.Prop("cooldown", s.CoolDown);
                    w.Prop("targets", s.MaxFight);
                    w.Prop("dx", s.Dx);
                    w.Prop("dy", s.Dy);
                    w.EndObject();
                }

                w.EndArray();
                w.EndObject();
            }

            w.EndArray();
        }

        // a table copied column by column
        private static void Simple(JsonWriter w, SqlTable t, string idCol, string[] cols)
        {
            int id = t.Col(idCol);
            var idx = new int[cols.Length];
            for (int i = 0; i < cols.Length; i++)
                idx[i] = t.Col(cols[i]);
            w.BeginArray();
            foreach (var r in t.Rows)
            {
                w.BeginObject();
                w.Prop("id", int.Parse(r[id]));
                for (int i = 0; i < cols.Length; i++)
                {
                    long n;
                    if (long.TryParse(r[idx[i]], out n))
                        w.Prop(cols[i], n);
                    else
                        w.Prop(cols[i], r[idx[i]]);
                }

                w.EndObject();
            }

            w.EndArray();
        }
    }
}
