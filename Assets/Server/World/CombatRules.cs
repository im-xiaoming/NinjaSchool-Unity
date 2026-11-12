using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NsoServer.Data;

namespace NsoServer.World
{
    public sealed class CombatRules
    {
        public readonly int DodgeScalePercent, MobAccuracyScalePercent, MaxDodgeChancePercent, MobDamagePercent;
        // Offline balance defaults requested by the user: harder monster hits and less player dodge.
        // The same defaults feed gameplay and the Workbench reset; saved explicit overrides still win.
        public CombatRules(int dodgeScalePercent = 50, int mobAccuracyScalePercent = 100,
            int maxDodgeChancePercent = 40, int mobDamagePercent = 150)
        {
            if (dodgeScalePercent < 0 || dodgeScalePercent > 500 || mobAccuracyScalePercent < 0 || mobAccuracyScalePercent > 500 ||
                maxDodgeChancePercent < 0 || maxDodgeChancePercent > 100 || mobDamagePercent < 0 || mobDamagePercent > 500)
                throw new ArgumentException("Combat scales must be 0..500; dodge chance cap must be 0..100.");
            DodgeScalePercent = dodgeScalePercent; MobAccuracyScalePercent = mobAccuracyScalePercent;
            MaxDodgeChancePercent = maxDodgeChancePercent; MobDamagePercent = mobDamagePercent;
        }
        public long EffectiveDodge(int raw) { return System.Math.Max(0L, raw) * DodgeScalePercent / 100; }
        public double DodgeChance(int mobLevel, int rawDodge)
        {
            double a = System.Math.Max(0L, mobLevel) * 10L * MobAccuracyScalePercent / 100 + 100;
            double b = EffectiveDodge(rawDodge) + 100;
            // Count pairs of uniform integer rolls x in [0,a), y in [0,b) where x < y.
            double n = System.Math.Min(a, b - 1);
            double chance = (n * (b - 1) - n * (n - 1) / 2) / (a * b);
            return System.Math.Min(chance, MaxDodgeChancePercent / 100.0);
        }
        public int ActualDamage(int displayedDamage)
        {
            if (displayedDamage < 0) return -1;
            if (MobDamagePercent == 0) return 0;
            long scaled = ((long)System.Math.Max(1, displayedDamage) * MobDamagePercent + 99) / 100;
            return (int)System.Math.Min(int.MaxValue, scaled);
        }
        public Dictionary<string, object> ToObject()
        {
            return new Dictionary<string, object> {
                {"dodgeScalePercent", DodgeScalePercent}, {"mobAccuracyScalePercent", MobAccuracyScalePercent},
                {"maxDodgeChancePercent", MaxDodgeChancePercent}, {"mobDamagePercent", MobDamagePercent} };
        }
        public static CombatRules FromObject(Dictionary<string, object> d)
        {
            if (d == null || d.Count != 4) throw new ArgumentException("Supply exactly the four combat fields.");
            return new CombatRules(LootRules.Number(d, "dodgeScalePercent"), LootRules.Number(d, "mobAccuracyScalePercent"),
                LootRules.Number(d, "maxDodgeChancePercent"), LootRules.Number(d, "mobDamagePercent"));
        }
        public static CombatRules Load(string path)
        {
            if (!File.Exists(path)) return new CombatRules();
            var root = Json.Parse(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>;
            if (root == null || root.Count != 2 || LootRules.Number(root, "version") != 1)
                throw new ArgumentException("Unsupported combat-rules document.");
            object raw;
            return FromObject(root.TryGetValue("combat", out raw) ? raw as Dictionary<string, object> : null);
        }
        public void Save(string path)
        {
            var w = new JsonWriter();
            w.Value(new Dictionary<string, object> { {"version", 1}, {"combat", ToObject()} });
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + ".tmp";
            File.WriteAllText(temp, w.ToString(), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
}
