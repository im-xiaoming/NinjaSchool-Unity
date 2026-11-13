using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NsoServer.Data;

namespace NsoServer.World
{
    // Offline dashboard deviation: per-template instance-stat overrides, reapplied on spawn/recovery.
    // Static SQL/artwork/reference data stays unchanged.
    public sealed class MobTuning
    {
        public readonly Dictionary<int, Dictionary<string, int>> Entries = new Dictionary<int, Dictionary<string, int>>();
        public static Dictionary<string, object> Schema()
        {
            return new Dictionary<string, object>
            {
                {
                    "Level",
                    new object[]
                    {
                        1,
                        200
                    }
                },
                {
                    "MaxHp",
                    new object[]
                    {
                        1,
                        100000000
                    }
                },
                {
                    "DamageOnPlayer",
                    new object[]
                    {
                        0,
                        1000000
                    }
                },
                {
                    "DamageOnPlayer2",
                    new object[]
                    {
                        0,
                        1000000
                    }
                },
                {
                    "AttackDelay",
                    new object[]
                    {
                        100,
                        600000
                    }
                },
                {
                    "Sys",
                    new object[]
                    {
                        0,
                        3
                    }
                }
            };
        }

        public static Dictionary<string, int> Validate(Dictionary<string, object> d)
        {
            if (d == null)
                throw new ArgumentException("Expected monster stat object.");
            var result = new Dictionary<string, int>();
            var schema = Schema();
            foreach (var pair in d)
            {
                object bounds;
                if (!schema.TryGetValue(pair.Key, out bounds))
                    throw new ArgumentException("Unknown monster stat: " + pair.Key);
                long n;
                if (pair.Value == null || !long.TryParse(Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture), out n))
                    throw new ArgumentException("Monster values must be integers.");
                var b = (object[])bounds;
                if (n < (int)b[0] || n > (int)b[1])
                    throw new ArgumentException(pair.Key + " is outside its allowed range.");
                result[pair.Key] = (int)n;
            }

            return result;
        }

        public void Apply(Mob m)
        {
            Dictionary<string, int> d;
            Entries.TryGetValue(m.Template.Id, out d);
            m.HasTunedHp = d != null && d.ContainsKey("MaxHp");
            if (d == null || d.Count == 0)
                return;
            if (d.ContainsKey("Level"))
            {
                m.Level = (short)d["Level"];
                int damage = (int)(m.Level + System.Math.Pow(m.Level, 2) / 5) * (m.IsBoss ? 20 : m.LevelBoss == 1 ? 2 : m.LevelBoss == 2 ? 3 : 1);
                m.DamageOnPlayer = damage;
                m.DamageOnPlayer2 = damage - damage / 10;
            }

            foreach (var pair in d)
            {
                if (pair.Key == "Level")
                    m.Level = (short)pair.Value;
                else if (pair.Key == "MaxHp")
                    m.MaxHp = pair.Value;
                else if (pair.Key == "DamageOnPlayer")
                    m.DamageOnPlayer = pair.Value;
                else if (pair.Key == "DamageOnPlayer2")
                    m.DamageOnPlayer2 = pair.Value;
                else if (pair.Key == "AttackDelay")
                    m.AttackDelay = pair.Value;
                else if (pair.Key == "Sys")
                    m.Sys = (sbyte)pair.Value;
            }

            m.Hp = System.Math.Min(m.Hp, m.MaxHp);
        }

        public object ToObject()
        {
            var d = new Dictionary<string, object>();
            foreach (var pair in Entries)
                d[pair.Key.ToString()] = pair.Value;
            return d;
        }

        public static MobTuning Load(string path, GameWorld world)
        {
            var result = new MobTuning();
            if (!File.Exists(path))
                return result;
            var root = Json.Parse(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>;
            if (root == null || LootRules.Number(root, "version") != 1 || !root.ContainsKey("monsters"))
                throw new ArgumentException("Invalid monster tuning document.");
            var entries = root["monsters"] as Dictionary<string, object>;
            if (entries == null)
                throw new ArgumentException("Invalid monster tuning entries.");
            foreach (var pair in entries)
            {
                int id;
                if (!int.TryParse(pair.Key, out id) || world.FindMob(id) == null)
                    throw new ArgumentException("Unknown monster template.");
                result.Entries[id] = Validate(pair.Value as Dictionary<string, object>);
            }

            return result;
        }

        public void Save(string path)
        {
            var w = new JsonWriter();
            w.Value(new Dictionary<string, object> { { "version", 1 }, { "monsters", ToObject() } });
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + ".tmp";
            File.WriteAllText(temp, w.ToString(), new UTF8Encoding(false));
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }
    }
}
