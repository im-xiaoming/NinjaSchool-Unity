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
        // Rikudou's quests (TaskOrders): daily counters, reset when the local date changes (Char.load isNewDay)
        public string DailyDate = "";
        public int CountFinishDay = 20, CountDoneTaskDay, CountLoopBoss = 2, CountUseItemBeast = 2, PointUyDanh;
        // Char.taskOrders: {taskId, count, maxCount, killId, mapId}
        public readonly List<int[]> TaskOrders = new List<int[]>();
        // uses of the point books (Char.limitKyNangSo / limitTiemNangSo / limitPhongLoi / limitBangHoa)
        public int LimitKyNangSo, LimitTiemNangSo, LimitPhongLoi, LimitBangHoa;
        // user.gold (luong, offline per character) and the "tẩy" licences (Char.tayTiemNang / tayKyNang)
        public int Luong, TayTiemNang, TayKyNang;
        // Char.coinMax: bag xu limit, raised at Kamakura with luong
        public long XuMax = 2000000000L;
        // Char.onOSkill / onKSkill / onCSkill: skill template ids on the 5 screen slots, the 3 hot keys and the
        // current skill (-1 = empty). Empty arrays make the client fill its default bar (GameScr.loadDefault*).
        public sbyte[] OSkill = new sbyte[0], KSkill = new sbyte[0], CSkill = new sbyte[0];

        private static sbyte[] Bytes(Dictionary<string, object> root, string key)
        {
            object raw;
            if (!root.TryGetValue(key, out raw) || !(raw is List<object>)) return new sbyte[0];
            var list = (List<object>)raw;
            var a = new sbyte[list.Count];
            for (int i = 0; i < a.Length; i++) a[i] = (sbyte)Json.Long(list[i]);
            return a;
        }

        private static void AppendBytes(StringBuilder b, string key, sbyte[] a)
        {
            if (a == null || a.Length == 0) return;
            b.Append(",\"").Append(key).Append("\":[");
            for (int i = 0; i < a.Length; i++) { if (i > 0) b.Append(','); b.Append(a[i]); }
            b.Append(']');
        }

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
            p.DailyDate = root.ContainsKey("dailyDate") && root["dailyDate"] is string ? (string)root["dailyDate"] : "";
            p.CountFinishDay = (int)Long(root, "countFinishDay", 20);
            p.CountDoneTaskDay = (int)Long(root, "countDoneTaskDay", 0);
            p.CountLoopBoss = (int)Long(root, "countLoopBoss", 2);
            p.CountUseItemBeast = (int)Long(root, "countUseItemBeast", 2);
            p.PointUyDanh = (int)Long(root, "pointUyDanh", 0);
            p.LimitKyNangSo = (int)Long(root, "limitKyNangSo", 0);
            p.LimitTiemNangSo = (int)Long(root, "limitTiemNangSo", 0);
            p.LimitPhongLoi = (int)Long(root, "limitPhongLoi", 0);
            p.LimitBangHoa = (int)Long(root, "limitBangHoa", 0);
            p.Luong = (int)Long(root, "luong", 0);
            p.XuMax = Long(root, "coinMax", 2000000000L);
            p.OSkill = Bytes(root, "oSkill");
            p.KSkill = Bytes(root, "kSkill");
            p.CSkill = Bytes(root, "cSkill");
            p.TayTiemNang = (int)Long(root, "tayTiemNang", 0);
            p.TayKyNang = (int)Long(root, "tayKyNang", 0);
            if (root.TryGetValue("taskOrder", out raw) && raw is List<object>)
            {
                foreach (var o in (List<object>)raw)
                {
                    var l = o as List<object>;
                    if (l != null && l.Count == 5)
                        p.TaskOrders.Add(new[] { (int)Json.Long(l[0]), (int)Json.Long(l[1]), (int)Json.Long(l[2]), (int)Json.Long(l[3]), (int)Json.Long(l[4]) });
                }
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
            b.Append(",\"dailyDate\":\"").Append(p.DailyDate ?? "").Append('"');
            b.Append(",\"countFinishDay\":").Append(p.CountFinishDay);
            b.Append(",\"countDoneTaskDay\":").Append(p.CountDoneTaskDay);
            b.Append(",\"countLoopBoss\":").Append(p.CountLoopBoss);
            b.Append(",\"countUseItemBeast\":").Append(p.CountUseItemBeast);
            b.Append(",\"pointUyDanh\":").Append(p.PointUyDanh);
            b.Append(",\"limitKyNangSo\":").Append(p.LimitKyNangSo);
            b.Append(",\"limitTiemNangSo\":").Append(p.LimitTiemNangSo);
            b.Append(",\"limitPhongLoi\":").Append(p.LimitPhongLoi);
            b.Append(",\"limitBangHoa\":").Append(p.LimitBangHoa);
            b.Append(",\"luong\":").Append(p.Luong);
            b.Append(",\"coinMax\":").Append(p.XuMax.ToString(CultureInfo.InvariantCulture));
            AppendBytes(b, "oSkill", p.OSkill);
            AppendBytes(b, "kSkill", p.KSkill);
            AppendBytes(b, "cSkill", p.CSkill);
            b.Append(",\"tayTiemNang\":").Append(p.TayTiemNang);
            b.Append(",\"tayKyNang\":").Append(p.TayKyNang);
            b.Append(",\"taskOrder\":[");
            for (int i = 0; i < p.TaskOrders.Count; i++)
            {
                if (i > 0) b.Append(',');
                var t = p.TaskOrders[i];
                b.Append('[').Append(t[0]).Append(',').Append(t[1]).Append(',').Append(t[2]).Append(',').Append(t[3]).Append(',').Append(t[4]).Append(']');
            }
            b.Append(']');
            return b.Append('}').ToString();
        }

        private static long Long(Dictionary<string, object> o, string key, long fallback)
        {
            return o != null && o.ContainsKey(key) && o[key] != null ? Json.Long(o[key]) : fallback;
        }
    }
}
