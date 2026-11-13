using System.Collections.Generic;
using System.Reflection;
using NsoServer.World;

namespace NsoServer.Net
{
    // Shared gameplay/preview overlays; raw potentials and player saves always retain original values.
    public static class PlayerTuning
    {
        private static readonly Dictionary<string, FieldInfo> CombatFields = CreateCombatFields();
        private static Dictionary<string, FieldInfo> CreateCombatFields()
        {
            var result = new Dictionary<string, FieldInfo>();
            foreach (string key in GameBalanceRules.PlayerFields)
                if (!key.StartsWith("Potential"))
                    result[key] = typeof(PlayerState).GetField(key);
            return result;
        }

        public static int[] Potentials(PlayerState p, GameBalanceRules balance)
        {
            var values = new int[4];
            int cls = p.Record.ClassId, level = p.Level;
            var basis = CharacterProgressionRules.OriginalPotential(cls);
            var growth = CharacterProgressionRules.OriginalGrowth(cls);
            for (int i = 0; i < 4; i++)
            {
                string key = "Potential" + i;
                int raw = balance.PlayerValue(key, p.Potential[i], 0, 30000);
                int start = balance.PlayerValue(key, basis[i], 0, 30000);
                int native = balance.PlayerValue(key, basis[i] + growth[i] * (level - 1), 0, 30000);
                values[i] = CharacterProgressionRules.Adjust(raw, start, native, level, balance.CharacterProgression.Resolve(cls, key), 0, 30000);
            }

            return values;
        }

        private static Dictionary<string, int> NativeCurve(GameWorld world, GameBalanceRules balance, int cls, int level)
        {
            var key = (ClassId: cls, Level: level);
            lock (balance.ProgressionCacheLock)
            {
                Dictionary<string, int> cached;
                if (balance.ProgressionCurves.TryGetValue(key, out cached))
                    return cached;
                var sample = PlayerCombat.ProgressionSample(world, balance, cls, level, false);
                cached = new Dictionary<string, int>();
                foreach (string stat in GameBalanceRules.PlayerFields)
                    if (!stat.StartsWith("Potential"))
                        cached[stat] = (int)CombatFields[stat].GetValue(sample);
                balance.ProgressionCurves[key] = cached;
                return cached;
            }
        }

        public static void ApplyAbility(PlayerState p, GameWorld world, GameBalanceRules balance, bool applyProgression)
        {
            foreach (string key in GameBalanceRules.PlayerFields)
            {
                if (key.StartsWith("Potential"))
                    continue;
                var field = CombatFields[key];
                int original = key == "Speed" ? 5 : (int)field.GetValue(p);
                int min = key == "MaxHp" || key == "MaxMp" ? 1 : 0, max = key == "Speed" ? 17 : 100000000;
                int value = balance.PlayerValue(key, original, min, max);
                var rule = balance.CharacterProgression.Resolve(p.Record.ClassId, key);
                if (applyProgression && (rule.Base.HasValue || rule.GrowthPercent.HasValue || rule.GrowthFlat.HasValue))
                {
                    var start = NativeCurve(world, balance, p.Record.ClassId, 1);
                    var curve = NativeCurve(world, balance, p.Record.ClassId, p.Level);
                    value = CharacterProgressionRules.Adjust(value, start[key], curve[key], p.Level, rule, min, max);
                }

                field.SetValue(p, value);
            }

            p.Damage2 = System.Math.Min(p.Damage2, p.Damage);
            p.Hp = System.Math.Min(p.Hp, p.MaxHp);
            p.Mp = System.Math.Min(p.Mp, p.MaxMp);
        }

        public static Dictionary<string, object> Values(PlayerState p, GameWorld world, GameBalanceRules balance = null)
        {
            balance = balance ?? world.BalanceRules;
            var values = new Dictionary<string, object>();
            foreach (string key in GameBalanceRules.PlayerFields)
                if (!key.StartsWith("Potential"))
                    values[key] = CombatFields[key].GetValue(p);
            int[] pot = Potentials(p, balance);
            for (int i = 0; i < 4; i++)
                values["Potential" + i] = pot[i];
            return values;
        }
    }
}
