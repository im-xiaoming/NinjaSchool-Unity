using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NsoServer.Data;
using NsoServer.World;

namespace NsoServer.Store
{
    public sealed class PlayerItemOption
    {
        public int Id, Param;
    }

    // Persistent item instance, using the useful fields from the Java Item JSON representation.
    public sealed class PlayerItem
    {
        public int Id, Index, Quantity = 1, Upgrade, System, Yen;
        public long Expire = -1;
        public bool IsLock;
        public readonly List<PlayerItemOption> Options = new List<PlayerItemOption>();
    }

    public sealed class PlayerInventory
    {
        public const int DefaultBagCells = 30, SlotCount = 16, BoxCells = 30;
        public PlayerItem[] Bag;
        public PlayerItem[] Box = new PlayerItem[BoxCells];   // storage box at Kamakura (PlayerStorage)
        public PlayerItem[] Equipment;
        public PlayerItem[] Fashion;

        private PlayerInventory(int bagCells)
        {
            Bag = new PlayerItem[bagCells];
            Equipment = new PlayerItem[SlotCount];
            Fashion = new PlayerItem[SlotCount];
        }

        public static PlayerInventory Load(string json, GameWorld world)
        {
            var root = string.IsNullOrEmpty(json) ? null : Json.Parse(json) as Dictionary<string, object>;
            int cells = ReadInt(root, "bagCells", DefaultBagCells);
            if (cells < 1 || cells > 127) cells = DefaultBagCells;
            var inventory = new PlayerInventory(cells);
            if (root == null) return inventory;
            LoadSlots(root, "bag", inventory.Bag, world, true);
            LoadSlots(root, "equipment", inventory.Equipment, world, false);
            if (!root.ContainsKey("equipment")) LoadSlots(root, "equiped", inventory.Equipment, world, false);
            LoadSlots(root, "fashion", inventory.Fashion, world, false);
            LoadSlots(root, "box", inventory.Box, world, true);
            return inventory;
        }

        private static void LoadSlots(Dictionary<string, object> root, string key, PlayerItem[] slots, GameWorld world, bool useIndex)
        {
            object raw;
            if (!root.TryGetValue(key, out raw) || !(raw is List<object>)) return;
            var values = (List<object>)raw;
            for (int i = 0; i < values.Count; i++)
            {
                if (!(values[i] is Dictionary<string, object>)) continue;
                var item = ParseItem((Dictionary<string, object>)values[i]);
                if (item == null || world.FindItem(item.Id) == null) continue;
                int slot = useIndex ? item.Index : i;
                if (slot < 0 || slot >= slots.Length) continue;
                slots[slot] = item;
            }
        }

        private static PlayerItem ParseItem(Dictionary<string, object> o)
        {
            if (!o.ContainsKey("id")) return null;
            var item = new PlayerItem
            {
                Id = ReadInt(o, "id", -1),
                Index = ReadInt(o, "index", 0),
                Quantity = ReadInt(o, "quantity", 1),
                Upgrade = ReadInt(o, "upgrade", 0),
                System = ReadInt(o, "sys", 0),
                Yen = ReadInt(o, "yen", 0),
                Expire = ReadLong(o, "expire", -1),
                IsLock = ReadBool(o, "isLock", false)
            };
            object rawOptions;
            if (o.TryGetValue("options", out rawOptions) && rawOptions is List<object>)
            {
                foreach (object raw in (List<object>)rawOptions)
                {
                    if (raw is List<object>)
                    {
                        var pair = (List<object>)raw;
                        if (pair.Count >= 2) item.Options.Add(new PlayerItemOption { Id = (int)Json.Long(pair[0]), Param = (int)Json.Long(pair[1]) });
                    }
                    else if (raw is Dictionary<string, object>)
                    {
                        var d = (Dictionary<string, object>)raw;
                        item.Options.Add(new PlayerItemOption { Id = ReadInt(d, "id", -1), Param = ReadInt(d, "param", 0) });
                    }
                }
            }
            return item.Id < 0 ? null : item;
        }

        public string ToJson()
        {
            var b = new StringBuilder("{\"bagCells\":");
            b.Append(Bag.Length).Append(",\"bag\":");
            WriteSlots(b, Bag, true);
            b.Append(",\"equipment\":"); WriteSlots(b, Equipment, false);
            b.Append(",\"fashion\":"); WriteSlots(b, Fashion, false);
            b.Append(",\"box\":"); WriteSlots(b, Box, true);
            return b.Append('}').ToString();
        }

        private static void WriteSlots(StringBuilder b, PlayerItem[] slots, bool writeNulls)
        {
            b.Append('[');
            for (int i = 0; i < slots.Length; i++)
            {
                if (i != 0) b.Append(',');
                var item = slots[i];
                if (item == null) { b.Append("null"); continue; }
                b.Append("{\"id\":").Append(item.Id)
                    .Append(",\"index\":").Append(writeNulls ? i : item.Index)
                    .Append(",\"expire\":").Append(item.Expire.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"isLock\":").Append(item.IsLock ? "true" : "false")
                    .Append(",\"yen\":").Append(item.Yen)
                    .Append(",\"upgrade\":").Append(item.Upgrade)
                    .Append(",\"sys\":").Append(item.System)
                    .Append(",\"quantity\":").Append(item.Quantity)
                    .Append(",\"options\":[");
                for (int j = 0; j < item.Options.Count; j++)
                {
                    if (j != 0) b.Append(',');
                    b.Append('[').Append(item.Options[j].Id).Append(',').Append(item.Options[j].Param).Append(']');
                }
                b.Append("]}");
            }
            b.Append(']');
        }

        private static int ReadInt(Dictionary<string, object> o, string key, int fallback)
        {
            if (o == null || !o.ContainsKey(key) || o[key] == null) return fallback;
            return unchecked((int)Json.Long(o[key]));
        }

        private static long ReadLong(Dictionary<string, object> o, string key, long fallback)
        {
            if (o == null || !o.ContainsKey(key) || o[key] == null) return fallback;
            return Json.Long(o[key]);
        }

        private static bool ReadBool(Dictionary<string, object> o, string key, bool fallback)
        {
            if (o == null || !o.ContainsKey(key) || o[key] == null) return fallback;
            return o[key] is bool ? (bool)o[key] : Convert.ToBoolean(o[key], CultureInfo.InvariantCulture);
        }
    }
}
