using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NsoServer.Data;

namespace NsoServer.Store
{
    // Character progress that the Java server keeps in separate `players` columns (xu, yen, task, skill, potential...).
    // Offline it is stored in PlayerRecord.Data next to the inventory (see ToJson). Missing keys = a new character.
    public sealed class PlayerProgress
    {
        public long Yen, Xu;
        // Char.taskMain: the quest in progress (TaskActive = false means none has been taken yet; the quest id is PlayerRecord.TaskId)
        public bool TaskActive;
        public int TaskIndex, TaskCount;
        // Char.vSkill: learned skills as [template id, point]; a new character knows the basic attack (template 0, point 0)
        public readonly List<int[]> Skills = new List<int[]>();
        // potentials and free points for characters with a class (class 0 derives them from the level)
        public int[] Potential;
        public int PotentialPoint, SkillPoint;
        // Char.saveCoordinate: village to go back to when killed (Lang Tone by default)
        public int SaveMap = 22;
        // Food: template id, strength, start and expiry UTC milliseconds.
        public long[] FoodEffect;

        public static PlayerProgress Load(string json)
        {
            var p = new PlayerProgress();
            var root = string.IsNullOrEmpty(json) ? null : Json.Parse(json) as Dictionary<string, object>;
            if (root == null) { p.Skills.Add(new[] { 0, 0 }); return p; }
            p.Yen = Long(root, "yen", 0);
            p.Xu = Long(root, "xu", 0);
            p.SaveMap = (int)Long(root, "saveMap", 22);
            object raw;
            if (root.TryGetValue("task", out raw) && raw is Dictionary<string, object>)
            {
                var t = (Dictionary<string, object>)raw;
                p.TaskActive = true;
                p.TaskIndex = (int)Long(t, "index", 0);
                p.TaskCount = (int)Long(t, "count", 0);
            }
            if (root.TryGetValue("skills", out raw) && raw is List<object>)
            {
                foreach (var s in (List<object>)raw)
                {
                    var pair = s as List<object>;
                    if (pair != null && pair.Count >= 2) p.Skills.Add(new[] { (int)Json.Long(pair[0]), (int)Json.Long(pair[1]) });
                }
            }
            if (p.Skills.Count == 0) p.Skills.Add(new[] { 0, 0 });
            if (root.TryGetValue("potential", out raw) && raw is List<object>)
            {
                var list = (List<object>)raw;
                if (list.Count == 4) p.Potential = new[] { (int)Json.Long(list[0]), (int)Json.Long(list[1]), (int)Json.Long(list[2]), (int)Json.Long(list[3]) };
            }
            p.PotentialPoint = (int)Long(root, "potentialPoint", 0);
            p.SkillPoint = (int)Long(root, "skillPoint", 0);
            if (root.TryGetValue("foodEffect", out raw) && raw is List<object>)
            {
                var food = (List<object>)raw;
                if (food.Count == 4)
                    p.FoodEffect = new[] { Json.Long(food[0]), Json.Long(food[1]), Json.Long(food[2]), Json.Long(food[3]) };
            }
            return p;
        }

        // The whole `PlayerRecord.Data` value: the inventory object with the progress keys added.
        public static string ToJson(PlayerInventory inventory, PlayerProgress p)
        {
            string inv = inventory.ToJson();
            var b = new StringBuilder(inv, 0, inv.Length - 1, inv.Length + 256);   // drop the closing brace
            b.Append(",\"yen\":").Append(p.Yen.ToString(CultureInfo.InvariantCulture));
            b.Append(",\"xu\":").Append(p.Xu.ToString(CultureInfo.InvariantCulture));
            b.Append(",\"saveMap\":").Append(p.SaveMap);
            if (p.TaskActive) b.Append(",\"task\":{\"index\":").Append(p.TaskIndex).Append(",\"count\":").Append(p.TaskCount).Append('}');
            b.Append(",\"skills\":[");
            for (int i = 0; i < p.Skills.Count; i++)
            {
                if (i > 0) b.Append(',');
                b.Append('[').Append(p.Skills[i][0]).Append(',').Append(p.Skills[i][1]).Append(']');
            }
            b.Append(']');
            if (p.Potential != null)
                b.Append(",\"potential\":[").Append(p.Potential[0]).Append(',').Append(p.Potential[1]).Append(',')
                 .Append(p.Potential[2]).Append(',').Append(p.Potential[3]).Append(']');
            b.Append(",\"potentialPoint\":").Append(p.PotentialPoint);
            b.Append(",\"skillPoint\":").Append(p.SkillPoint);
            if (p.FoodEffect != null && p.FoodEffect.Length == 4)
            {
                b.Append(",\"foodEffect\":[");
                for (int i = 0; i < 4; i++)
                {
                    if (i > 0) b.Append(',');
                    b.Append(p.FoodEffect[i].ToString(CultureInfo.InvariantCulture));
                }
                b.Append(']');
            }
            return b.Append('}').ToString();
        }

        private static long Long(Dictionary<string, object> o, string key, long fallback)
        {
            return o != null && o.ContainsKey(key) && o[key] != null ? Json.Long(o[key]) : fallback;
        }
    }
}
