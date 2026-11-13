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
        public static Dictionary<string, object> Export(string json, GameWorld world)
        {
            var wiki = Json.Parse(json) as Dictionary<string, object>;
            var uses = new Dictionary<string, object>();
            var collections = (Dictionary<string, object>)wiki["collections"];
            foreach (Dictionary<string, object> item in (List<object>)collections["items"])
            {
                int id = (int)Json.Long(item["id"]);
                var template = world.FindItem(id);
                if (template == null)
                    continue;
                uses[id.ToString(System.Globalization.CultureInfo.InvariantCulture)] = new Dictionary<string, object>
                {
                    {
                        "text",
                        UseInfo(world, template)
                    },
                    {
                        "usable",
                        UsableFromBag(world, template)
                    },
                    {
                        "gender",
                        template.Gender
                    }
                };
            }

            return new Dictionary<string, object>
            {
                {
                    "wiki",
                    wiki
                },
                {
                    "itemUse",
                    uses
                }
            };
        }

        /// <summary>Diagnostic compatibility entry; worker/test only, never while holding the world lock.</summary>
        public static List<string[]> Build(string json, GameWorld world)
        {
            var result = BrainClient.Call("render", Export(json, world), 30);
            var docs = new List<string[]>();
            foreach (Dictionary<string, object> d in (List<object>)result["documents"])
                docs.Add(new[] { Convert.ToString(d["key"]), Convert.ToString(d["kind"]), Convert.ToString(d["text"]) });
            return docs;
        }

        public static void Inject(GameWorld world)
        {
            try
            {
                var source = Source;
                if (source == null || world == null)
                {
                    LastError = "no wiki data source";
                    return;
                }

                var result = BrainClient.Call("knowledge/batch", Export(source(), world), 30);
                Documents = (int)Json.Long(result["documents"]);
                LastError = "";
            }
            catch (Exception ex)
            {
                LastError = ex.GetType().Name + ": " + ex.Message;
            }
        }

        /// <summary>What using the item does in this offline server, and whether it can be used at all (PlayerItems.UseItem).</summary>
        public static string UseInfo(GameWorld world, ItemTemplateData t)
        {
            int id = t.Id;
            if (PlayerService.IsWearable(t))
                return "equipment: wear it from the bag (slot " + t.Type + "), it adds its stats; usable offline";
            if (PlayerItems.IsMartialArtBook(id))
            {
                int cls = PlayerSkills.BookClass(world, id);
                return "skill book: read it to learn a skill" + (cls >= 1 ? " of class " + cls + " (only that school)" : "") + "; usable offline";
            }

            if (t.Type == PlayerItems.TypeHp)
                return "HP potion: restores HP over 3 seconds (one at a time, not when HP is full); usable offline";
            if (t.Type == PlayerItems.TypeMp)
                return "MP potion: restores MP at once; usable offline";
            if (t.Type == PlayerItems.TypeEat)
            {
                int param, minutes;
                if (Food(id, out param, out minutes))
                    return "food: +" + param + " HP and MP every half second for " + (minutes >= 120 ? minutes / 60 + " hours" : minutes + " minutes") + " (a new serving replaces the running one); usable offline";
                return "food without a ported effect; using it does nothing offline";
            }

            switch (id)
            {
                case PlayerItemUse.ItemBachBien:
                case PlayerItemUse.ItemVoHanBachBien:
                    return "teleports back to the village saved at Kamakura; usable offline";
                case PlayerItemUse.ItemCanCauVang:
                    return "golden fishing rod; usable offline";
                case PlayerItemUse.ItemThienBienLenh:
                    return "Thiên biến lệnh; usable offline";
                case PlayerItemUse.ItemPhucNang:
                    return "Phúc nang nhẫn giả: open it for a random reward; usable offline";
                case PlayerItemUse.ItemManhGiayVun:
                    return "scrap paper (combine/use as in the original); usable offline";
                case PlayerItemUse.ItemSachKyNangSo:
                    return "gives skill points; usable offline";
                case PlayerItemUse.ItemSachTiemNangSo:
                    return "gives potential points; usable offline";
                case PlayerItemUse.ItemSachPhongLoi:
                case PlayerItemUse.ItemSachBangHoa:
                    return "special skill book; usable offline";
                case 240:
                    return "resets the potential points; usable offline";
                case 241:
                    return "resets the skill points; usable offline";
                case 248:
                    return "Nấm linh chi: double experience for 5 hours; usable offline";
                case 254:
                case 255:
                case 256:
                    return "clears the experience lost by dying; usable offline";
                case 257:
                    return "clears hiếu chiến points (always 0 offline); usable offline";
                case TaskOrders.ItemTaThuLenh:
                    return "mission order: starts a Tà thú mission; usable offline";
                case TaskMechanics.ItemChiaKhoaCoQuan:
                    return "key: opens the mechanism box during its quest; usable offline";
                case TaskMechanics.ItemBinhRong:
                    return "empty bottle: fill it with water during its quest; usable offline";
            }

            if (id >= TaskMechanics.ItemDiaDo2 && id <= TaskMechanics.ItemDiaDo4)
                return "treasure map: use it at the marked place; usable offline";
            if (t.Type == PlayerItems.TypeTask || t.Type == PlayerItems.TypeTaskSave || t.Type == PlayerItems.TypeTaskWait)
                return "quest item: kept for a quest step (bring or show it), not used from the bag";
            if (t.Type == 26)
                return "material (stone): used at an NPC to upgrade or craft, not used from the bag";
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
                case 23:
                    param = 3;
                    minutes = 30;
                    return true;
                case 24:
                    param = 20;
                    minutes = 30;
                    return true;
                case 25:
                    param = 30;
                    minutes = 30;
                    return true;
                case 26:
                    param = 40;
                    minutes = 30;
                    return true;
                case 27:
                    param = 50;
                    minutes = 30;
                    return true;
                case 29:
                    param = 60;
                    minutes = 30;
                    return true;
                case 30:
                    param = 60;
                    minutes = 4320;
                    return true;
                case 249:
                    param = 40;
                    minutes = 4320;
                    return true;
                case 250:
                    param = 50;
                    minutes = 4320;
                    return true;
                case 409:
                    param = 150;
                    minutes = 1440;
                    return true;
                case 410:
                    param = 200;
                    minutes = 1440;
                    return true;
                case 567:
                    param = 300;
                    minutes = 1440;
                    return true;
            }

            param = minutes = 0;
            return false;
        }
    }
}
