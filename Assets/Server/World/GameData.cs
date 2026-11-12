using System;
using System.Collections.Generic;
using NsoServer.Data;

namespace NsoServer.World
{
    using Math = System.Math;   // the client has its own global Math class

    // One row of `task_template` (TaskTemplate.java): a main quest with its steps.
    public sealed class TaskTemplateData
    {
        public int Id;
        public string Name = "", Detail = "";
        public string[] SubNames = new string[0];
        public short[] Counts = new short[0];
        public int LevelRequire;
        public short[][] KillMobs = new short[0][];   // per step: [mob template id, levelBoss] (-1 = no kill step)
        public short[] PickItems = new short[0];      // per step: item id to pick (-1 = no pick step)
    }

    // One row of `skill_template`.
    public sealed class SkillTemplateData
    {
        public int Id, ClassId, MaxPoint, Type;
        public string Name = "";
    }

    // One row of `store_data` (ItemStore.java). Options are the maximum values; the minimum is derived as in ItemStore.
    public sealed class StoreItem
    {
        public int Id, ItemId, Sys, Store, Coin, Gold, Yen;
        public bool IsLock;
        public long Expire;
        public int[] OptionIds = new int[0], MaxParams = new int[0], MinParams = new int[0];
    }

    // Static game data used by quests, items, shops and skills (GameData / StoreManager / Server.npcTasks / RandomItem).
    public sealed partial class GameWorld
    {
        private readonly Dictionary<int, TaskTemplateData> taskTemplates = new Dictionary<int, TaskTemplateData>();
        private readonly Dictionary<int, short[]> taskNpcs = new Dictionary<int, short[]>();
        private readonly Dictionary<int, short[]> taskMaps = new Dictionary<int, short[]>();
        private readonly Dictionary<int, SkillTemplateData> skillTemplates = new Dictionary<int, SkillTemplateData>();
        private readonly Dictionary<int, List<StoreItem>> stores = new Dictionary<int, List<StoreItem>>();
        private readonly Random dataRng = new Random();

        // RandomItem.ITEM: what an ordinary monster may drop on an ordinary map (weight, item id)
        private static readonly int[,] RandomDrop =
        {
            { 5, 0 }, { 5, 1 }, { 5, 2 }, { 1, 3 }, { 1, 4 },   // stones level 1..5
            { 1, 38 },                                          // Phúc nang nhẫn giả
            { 1, 13 }, { 1, 18 }                                // smallest HP / MP potion (scaled by monster level)
        };

        // called from the constructor in World.cs
        private void LoadGameData(Dictionary<string, SqlTable> tables)
        {
            LoadTaskTemplates(tables["task_template"]);
            LoadTaskNpcs(tables["task"]);
            LoadSkillTemplates(tables["skill_template"]);
            LoadStores(tables["store_data"]);
            int eid = tables["effect"].Col("id"), etype = tables["effect"].Col("type");
            foreach (var r in tables["effect"].Rows) effectTypes[int.Parse(r[eid])] = int.Parse(r[etype]);
        }

        // `effect` table: effect template id -> effect type (0 food, 1 burn, 2 freeze, 3 stun, 4/17 HP over time...)
        private readonly Dictionary<int, int> effectTypes = new Dictionary<int, int>();

        public int EffectType(int templateId)
        {
            int t;
            return effectTypes.TryGetValue(templateId, out t) ? t : -1;
        }

        public TaskTemplateData FindTask(int id)
        {
            TaskTemplateData t;
            return taskTemplates.TryGetValue(id, out t) ? t : null;
        }

        // Server.npcTasks[taskId]: the NPC to see before taking the quest (index 0) and for each step (index + 1)
        public short[] TaskNpcs(int taskId)
        {
            short[] n;
            return taskNpcs.TryGetValue(taskId, out n) ? n : null;
        }

        // GameScr.getTaskMapId / task.maps: index 0 before acceptance, step + 1 while active.
        public short[] TaskMaps(int taskId)
        {
            short[] maps;
            return taskMaps.TryGetValue(taskId, out maps) ? maps : null;
        }

        public SkillTemplateData FindSkillTemplate(int id)
        {
            SkillTemplateData t;
            return skillTemplates.TryGetValue(id, out t) ? t : null;
        }

        // StoreManager.find(type): the items of one shop in display order
        public StoreItem ItemStoreSample(int itemId)
        {
            foreach (var store in stores.Values) foreach (var item in store) if (item.ItemId == itemId) return item;
            return null;
        }

        public List<StoreItem> Store(int type)
        {
            List<StoreItem> s;
            return stores.TryGetValue(type, out s) ? s : null;
        }

        // StoreManager.getListEquipmentWithLevelRange: wearable items of all shops within a level range
        public List<StoreItem> EquipmentInLevelRange(int min, int max)
        {
            var list = new List<StoreItem>();
            foreach (var kv in stores)
                foreach (var s in kv.Value)
                {
                    var t = FindItem(s.ItemId);
                    if (t != null && t.Type >= 0 && t.Type <= 15 && t.Level >= min && t.Level <= max && t.Fashion < 0) list.Add(s);
                }
            return list;
        }

        // Mob.randomItemID: a weighted random drop; potions follow the monster's level
        public int RandomDropItem(int mobLevel, bool isBoss)
        {
            int total = 0;
            for (int i = 0; i < RandomDrop.GetLength(0); i++) total += RandomDrop[i, 0];
            int r = dataRng.Next(total), id = RandomDrop[0, 1];
            for (int i = 0; i < RandomDrop.GetLength(0); i++)
            {
                r -= RandomDrop[i, 0];
                if (r < 0) { id = RandomDrop[i, 1]; break; }
            }
            if (!isBoss && id == 0) id = Math.Min(mobLevel / 15, 4);    // stone level from the monster level
            else if (id == 13) id = mobLevel < 10 ? 13 : mobLevel < 30 ? 14 : mobLevel < 40 ? 15 : mobLevel < 70 ? 16 : mobLevel < 90 ? 17 : 565;
            else if (id == 18) id = mobLevel < 10 ? 18 : mobLevel < 30 ? 19 : mobLevel < 40 ? 20 : mobLevel < 70 ? 21 : mobLevel < 90 ? 22 : 566;
            return id;
        }

        // ---- loading ----

        private static short[] Shorts(string json)
        {
            var list = new List<short>();
            if (string.IsNullOrEmpty(json)) return list.ToArray();
            foreach (var v in Json.Parse(json) as List<object> ?? new List<object>()) list.Add((short)Json.Long(v));
            return list.ToArray();
        }

        private void LoadTaskTemplates(SqlTable t)
        {
            int id = t.Col("taskId"), name = t.Col("name"), detail = t.Col("detail"), subs = t.Col("subnames"), counts = t.Col("counts"),
                level = t.Col("level_require"), kill = t.Col("kill_mob"), pick = t.Col("pick_item");
            foreach (var r in t.Rows)
            {
                var task = new TaskTemplateData
                {
                    Id = int.Parse(r[id]),
                    Name = r[name] ?? "",
                    Detail = r[detail] ?? "",
                    Counts = Shorts(r[counts]),
                    LevelRequire = string.IsNullOrEmpty(r[level]) ? 0 : int.Parse(r[level]),
                    PickItems = Shorts(r[pick])
                };
                var names = new List<string>();
                foreach (var s in Json.Parse(r[subs] ?? "[]") as List<object> ?? new List<object>()) names.Add(Convert.ToString(s));
                task.SubNames = names.ToArray();
                var mobs = new List<short[]>();
                foreach (var m in Json.Parse(string.IsNullOrEmpty(r[kill]) ? "[]" : r[kill]) as List<object> ?? new List<object>())
                {
                    var pair = m as List<object>;
                    mobs.Add(pair == null ? new short[] { -1, 0 } : new[] { (short)Json.Long(pair[0]), (short)Json.Long(pair[1]) });
                }
                task.KillMobs = mobs.ToArray();
                taskTemplates[task.Id] = task;
            }
        }

        private void LoadTaskNpcs(SqlTable t)
        {
            int id = t.Col("id"), npcs = t.Col("npcs"), maps = t.Col("maps");
            foreach (var r in t.Rows)
            {
                int task = int.Parse(r[id]);
                taskNpcs[task] = Shorts(r[npcs]);
                taskMaps[task] = Shorts(r[maps]);
            }
        }

        private void LoadSkillTemplates(SqlTable t)
        {
            int id = t.Col("id"), cls = t.Col("class"), name = t.Col("name"), max = t.Col("max_point"), type = t.Col("type");
            foreach (var r in t.Rows)
                skillTemplates[int.Parse(r[id])] = new SkillTemplateData
                {
                    Id = int.Parse(r[id]), ClassId = int.Parse(r[cls]), Name = r[name] ?? "",
                    MaxPoint = int.Parse(r[max]), Type = int.Parse(r[type])
                };
        }

        private void LoadStores(SqlTable t)
        {
            int id = t.Col("id"), item = t.Col("item_id"), sys = t.Col("sys"), store = t.Col("store"), lck = t.Col("lock"),
                coin = t.Col("coin"), gold = t.Col("gold"), yen = t.Col("yen"), expire = t.Col("expire"), options = t.Col("options");
            foreach (var r in t.Rows)
            {
                var s = new StoreItem
                {
                    Id = int.Parse(r[id]), ItemId = int.Parse(r[item]), Sys = int.Parse(r[sys]), Store = int.Parse(r[store]),
                    IsLock = r[lck] == "1" || r[lck] == "true", Coin = int.Parse(r[coin]), Gold = int.Parse(r[gold]),
                    Yen = int.Parse(r[yen]), Expire = long.Parse(r[expire])
                };
                var ids = new List<int>(); var max = new List<int>(); var min = new List<int>();
                foreach (var o in Json.Parse(string.IsNullOrEmpty(r[options]) ? "[]" : r[options]) as List<object> ?? new List<object>())
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    int oid = (int)Json.Long(d["id"]), p = (int)Json.Long(d["param"]);
                    ids.Add(oid); max.Add(p); min.Add(MinParam(oid, p));
                }
                s.OptionIds = ids.ToArray(); s.MaxParams = max.ToArray(); s.MinParams = min.ToArray();
                List<StoreItem> list;
                if (!stores.TryGetValue(s.Store, out list)) stores[s.Store] = list = new List<StoreItem>();
                list.Add(s);
            }
        }

        // ItemStore constructor: the lowest value an option of a bought item can roll
        private static int MinParam(int id, int p)
        {
            if (id == 0 || id == 1 || (id >= 21 && id <= 26)) return p - 49;
            if (id == 6 || id == 7 || id == 8 || id == 9 || id == 19) return p - 9;
            if ((id >= 2 && id <= 5) || (id >= 10 && id <= 15) || id == 17 || id == 18) return p - 4;
            if (id == 16) return p - 2;
            return p;
        }
    }
}
