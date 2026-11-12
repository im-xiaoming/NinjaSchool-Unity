using System;
using System.Collections.Generic;
using System.Globalization;

namespace NsoServer.World
{
    // Offline configurable overlays on Char.addExp/selectClass and AbilityFromEquip.setAbility.
    // Classed characters keep their allocated potential points; no save allocation is rewritten.
    public sealed class CharacterProgressionRules
    {
        public const int MaxClassId = 6;
        public sealed class Rule
        {
            public int? Base;
            public int? GrowthPercent;
            public decimal? GrowthFlat;
        }
        public readonly Dictionary<string, Dictionary<string, Rule>> Profiles = new Dictionary<string, Dictionary<string, Rule>>();
        public static int[] OriginalPotential(int cls) { return cls == 0 || cls == 1 || cls == 3 || cls == 5 ? new[] { 15, 5, 5, 5 } : new[] { 5, 5, 10, 10 }; }
        public static int[] OriginalGrowth(int cls) { return cls == 0 ? new[] { 2, 1, 1, 1 } : new[] { 0, 0, 0, 0 }; }
        public static bool ValidScope(string scope)
        {
            if (scope == "classed") return true;
            int id;
            return int.TryParse(scope, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id >= 0 && id <= MaxClassId && id.ToString(CultureInfo.InvariantCulture) == scope;
        }
        public Rule Resolve(int cls, string stat)
        {
            var result = new Rule();
            if (cls != 0) Merge(result, "classed", stat);
            Merge(result, cls.ToString(CultureInfo.InvariantCulture), stat);
            return result;
        }
        private void Merge(Rule result, string scope, string stat)
        {
            Dictionary<string, Rule> profile; Rule own;
            if (!Profiles.TryGetValue(scope, out profile) || !profile.TryGetValue(stat, out own)) return;
            if (own.Base.HasValue) result.Base = own.Base;
            if (own.GrowthPercent.HasValue) result.GrowthPercent = own.GrowthPercent;
            if (own.GrowthFlat.HasValue) result.GrowthFlat = own.GrowthFlat;
        }
        public static Dictionary<string, Rule> ParseProfile(Dictionary<string, object> values)
        {
            if (values == null || values.Count > GameBalanceRules.PlayerFields.Length) throw new ArgumentException("Invalid progression profile.");
            var result = new Dictionary<string, Rule>();
            foreach (var stat in values)
            {
                if (Array.IndexOf(GameBalanceRules.PlayerFields, stat.Key) < 0) throw new ArgumentException("Unknown progression stat: " + stat.Key);
                var raw = stat.Value as Dictionary<string, object>;
                if (raw == null) throw new ArgumentException("Expected stat progression object.");
                var rule = new Rule();
                foreach (var field in raw)
                {
                    if (field.Key == "base") rule.Base = GameBalanceRules.Integer(field.Value, stat.Key == "MaxHp" || stat.Key == "MaxMp" ? 1 : 0, stat.Key == "Speed" ? 17 : stat.Key.StartsWith("Potential") ? 30000 : 100000000);
                    else if (field.Key == "growthPercent") rule.GrowthPercent = GameBalanceRules.Integer(field.Value, 0, 1000);
                    else if (field.Key == "growthFlat")
                    {
                        decimal n;
                        if (field.Value == null || field.Value is bool || !decimal.TryParse(Convert.ToString(field.Value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out n) || n < -10000 || n > 10000 || decimal.Round(n, 2) != n)
                            throw new ArgumentException("Per-level growth must be -10000..10000, with at most two decimal places.");
                        rule.GrowthFlat = n;
                    }
                    else throw new ArgumentException("Unknown progression formula parameter.");
                }
                if (raw.Count > 0) result[stat.Key] = rule;
            }
            return result;
        }
        public static CharacterProgressionRules FromObject(Dictionary<string, object> raw)
        {
            if (raw == null) throw new ArgumentException("Invalid character progression profiles.");
            var result = new CharacterProgressionRules();
            foreach (var pair in raw)
            {
                if (!ValidScope(pair.Key)) throw new ArgumentException("Unknown class/group profile.");
                result.Profiles[pair.Key] = ParseProfile(pair.Value as Dictionary<string, object>);
            }
            return result;
        }
        public object ToObject()
        {
            var result = new Dictionary<string, object>();
            foreach (var profile in Profiles)
            {
                var stats = new Dictionary<string, object>();
                foreach (var stat in profile.Value)
                {
                    var d = new Dictionary<string, object>(); var r = stat.Value;
                    if (r.Base.HasValue) d["base"] = r.Base.Value;
                    if (r.GrowthPercent.HasValue) d["growthPercent"] = r.GrowthPercent.Value;
                    if (r.GrowthFlat.HasValue) d["growthFlat"] = r.GrowthFlat.Value;
                    stats[stat.Key] = d;
                }
                result[profile.Key] = stats;
            }
            return result;
        }
        // Adjust only the native progression curve; equipment, passive/selected skill differences and
        // allocated points remain additional contributions, rather than being scaled a second time.
        public static int Adjust(int fullValue, int nativeBase, int nativeAtLevel, int level, Rule rule, int min, int max)
        {
            decimal value = fullValue + (decimal)(rule.Base ?? nativeBase) - nativeBase
                + ((decimal)nativeAtLevel - nativeBase) * ((rule.GrowthPercent ?? 100) - 100) / 100
                + (level - 1m) * (rule.GrowthFlat ?? 0);
            return (int)System.Math.Max(min, System.Math.Min(max, decimal.Truncate(value)));
        }
    }
}
