using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NsoServer.Data;

namespace NsoServer.World
{
    // Offline configurable policy around Java Mob.dead/dropItem. Immutable snapshots are replaced
    // under GameWorld.SyncRoot, so a kill never observes half of a settings update.
    public sealed class LootRules
    {
        public readonly bool BlockTrainingDummy, BlockOverlevel, LimitOrdinaryLoot;
        public readonly int OverlevelGap, OrdinaryMaxGap;

        public LootRules(bool blockTrainingDummy = true, bool blockOverlevel = true, int overlevelGap = 10,
            bool limitOrdinaryLoot = true, int ordinaryMaxGap = 7)
        {
            if (overlevelGap < 1 || overlevelGap > 200 || ordinaryMaxGap < 0 || ordinaryMaxGap > 200)
                throw new ArgumentException("Level gaps must be 1..200 (overlevel) and 0..200 (ordinary).");
            BlockTrainingDummy = blockTrainingDummy; BlockOverlevel = blockOverlevel;
            OverlevelGap = overlevelGap; LimitOrdinaryLoot = limitOrdinaryLoot; OrdinaryMaxGap = ordinaryMaxGap;
        }

        public Dictionary<string, object> ToObject()
        {
            return new Dictionary<string, object> {
                {"blockTrainingDummy", BlockTrainingDummy}, {"blockOverlevel", BlockOverlevel},
                {"overlevelGap", OverlevelGap}, {"limitOrdinaryLoot", LimitOrdinaryLoot}, {"ordinaryMaxGap", OrdinaryMaxGap}
            };
        }

        public static LootRules FromObject(Dictionary<string, object> d)
        {
            if (d == null || d.Count != 5) throw new ArgumentException("Supply exactly the five loot rule fields.");
            return new LootRules(Bool(d, "blockTrainingDummy"), Bool(d, "blockOverlevel"), Number(d, "overlevelGap"),
                Bool(d, "limitOrdinaryLoot"), Number(d, "ordinaryMaxGap"));
        }

        private static bool Bool(Dictionary<string, object> d, string key)
        {
            object v;
            if (!d.TryGetValue(key, out v) || !(v is bool)) throw new ArgumentException(key + " must be a boolean.");
            return (bool)v;
        }
        public static int Number(Dictionary<string, object> d, string key)
        {
            object v;
            if (!d.TryGetValue(key, out v) || !(v is long || v is int)) throw new ArgumentException(key + " must be an integer.");
            long n = Convert.ToInt64(v);
            if (n < 0 || n > int.MaxValue) throw new ArgumentException(key + " is outside its allowed range.");
            return (int)n;
        }

        public static LootRules Load(string path)
        {
            if (!File.Exists(path)) return new LootRules();
            var root = Json.Parse(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>;
            if (root == null || Number(root, "version") != 1) throw new ArgumentException("Unsupported game-rules version.");
            object rules;
            return FromObject(root.TryGetValue("rules", out rules) ? rules as Dictionary<string, object> : null);
        }

        public void Save(string path)
        {
            var w = new JsonWriter();
            w.Value(new Dictionary<string, object> { {"version", 1}, {"rules", ToObject()} });
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + ".tmp";
            File.WriteAllText(temp, w.ToString(), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
