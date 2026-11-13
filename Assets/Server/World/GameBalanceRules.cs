using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NsoServer.Data;
using NsoServer.Store;

namespace NsoServer.World
{
    // Offline balancing configuration, separate from player saves and original tables.
    // Every pair means truncate(original * percent / 100) + flat. Defaults are 100%, +0.
    public sealed class GameBalanceRules
    {
        public readonly Dictionary<string, int[]> Player = new Dictionary<string, int[]>();
        public CharacterProgressionRules CharacterProgression = new CharacterProgressionRules();
        // Memoized naked native curves, scoped to this immutable published settings snapshot.
        internal readonly object ProgressionCacheLock = new object ();
        internal readonly Dictionary<(int ClassId, int Level), Dictionary<string, int>> ProgressionCurves = new Dictionary<(int ClassId, int Level), Dictionary<string, int>>();
        // -1 means every equipment template; template-specific adjustments run afterwards.
        public readonly Dictionary<int, Dictionary<int, int[]>> Equipment = new Dictionary<int, Dictionary<int, int[]>>();
        public int ExperiencePercent = 100;
        public static readonly string[] PlayerFields =
        {
            "MaxHp",
            "MaxMp",
            "Speed",
            "Damage",
            "Damage2",
            "Exactly",
            "Miss",
            "DameDown",
            "ResFire",
            "ResIce",
            "ResWind",
            "Fatal",
            "FatalDame",
            "PercentFatalDame",
            "Potential0",
            "Potential1",
            "Potential2",
            "Potential3"
        };
        public static int[] Pair(object raw)
        {
            var a = raw as List<object>;
            if (a == null || a.Count != 2)
                throw new ArgumentException("Expected [percent, flat].");
            return new[]
            {
                Integer(a[0], 0, 1000),
                Integer(a[1], -1000000, 1000000)
            };
        }

        public static int Integer(object raw, int min, int max)
        {
            int n;
            if (raw == null || raw is bool || !int.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) || n < min || n > max)
                throw new ArgumentException("Expected integer in range " + min + ".." + max + ".");
            return n;
        }

        public static int Scale(int value, int[] pair, int min = 0, int max = 100000000)
        {
            long scaled = (long)value * pair[0] / 100 + pair[1];
            return (int)System.Math.Max(min, System.Math.Min(max, scaled));
        }

        public static int Stat(long value)
        {
            return (int)System.Math.Max(int.MinValue, System.Math.Min(int.MaxValue, value));
        }

        public int PlayerValue(string key, int value, int min = 0, int max = 100000000)
        {
            int[] pair;
            return Player.TryGetValue(key, out pair) ? Scale(value, pair, min, max) : value;
        }

        public List<PlayerItemOption> EffectiveOptions(PlayerItem item)
        {
            if (!Equipment.ContainsKey(-1) && !Equipment.ContainsKey(item.Id))
                return item.Options;
            var values = new Dictionary<int, int>();
            foreach (var option in item.Options)
            {
                int previous;
                values.TryGetValue(option.Id, out previous);
                values[option.Id] = previous + option.Param;
            }

            ApplyOptions(values, -1);
            ApplyOptions(values, item.Id);
            var result = new List<PlayerItemOption>();
            foreach (var pair in values)
                result.Add(new PlayerItemOption { Id = pair.Key, Param = pair.Value });
            return result;
        }

        private void ApplyOptions(Dictionary<int, int> values, int template)
        {
            Dictionary<int, int[]> rules;
            if (!Equipment.TryGetValue(template, out rules))
                return;
            foreach (var pair in rules)
            {
                int original;
                values.TryGetValue(pair.Key, out original);
                values[pair.Key] = Scale(original, pair.Value, -1000000, 1000000);
            }
        }

        public object ToObject()
        {
            var equipment = new Dictionary<string, object>();
            foreach (var tpl in Equipment)
            {
                var options = new Dictionary<string, object>();
                foreach (var option in tpl.Value)
                    options[option.Key.ToString(CultureInfo.InvariantCulture)] = option.Value;
                equipment[tpl.Key.ToString(CultureInfo.InvariantCulture)] = options;
            }

            return new Dictionary<string, object>
            {
                {
                    "player",
                    Player
                },
                {
                    "equipment",
                    equipment
                },
                {
                    "experiencePercent",
                    ExperiencePercent
                },
                {
                    "characterProgression",
                    CharacterProgression.ToObject()
                }
            };
        }

        public static GameBalanceRules FromObject(Dictionary<string, object> root, GameWorld world)
        {
            if (root == null || (root.Count != 3 && root.Count != 4) || !root.ContainsKey("player") || !root.ContainsKey("equipment") || !root.ContainsKey("experiencePercent") || (root.Count == 4 && !root.ContainsKey("characterProgression")))
                throw new ArgumentException("Supply player, equipment and experiencePercent balance sections.");
            var result = new GameBalanceRules();
            if (root.ContainsKey("characterProgression"))
                result.CharacterProgression = CharacterProgressionRules.FromObject(root["characterProgression"] as Dictionary<string, object>);
            result.ExperiencePercent = Integer(root["experiencePercent"], 0, 1000);
            var player = root["player"] as Dictionary<string, object>;
            var equipment = root["equipment"] as Dictionary<string, object>;
            if (player == null || equipment == null)
                throw new ArgumentException("Invalid balance sections.");
            foreach (var pair in player)
            {
                if (Array.IndexOf(PlayerFields, pair.Key) < 0)
                    throw new ArgumentException("Unknown player stat: " + pair.Key);
                result.Player[pair.Key] = Pair(pair.Value);
            }

            foreach (var template in equipment)
            {
                int id = Integer(template.Key, -1, short.MaxValue);
                var tpl = world.FindItem(id);
                if (id != -1 && (tpl == null || !IsEquipment(tpl)))
                    throw new ArgumentException("Unknown equipment template.");
                var options = template.Value as Dictionary<string, object>;
                if (options == null || options.Count > 128)
                    throw new ArgumentException("Expected at most 128 equipment options.");
                var values = new Dictionary<int, int[]>();
                foreach (var option in options)
                {
                    int optionId = Integer(option.Key, 0, 255);
                    if (!world.HasItemOption(optionId))
                        throw new ArgumentException("Unknown item option.");
                    values[optionId] = Pair(option.Value);
                }

                result.Equipment[id] = values;
            }

            return result;
        }

        public static bool IsEquipment(ItemTemplateData tpl)
        {
            return tpl.Type >= 0 && tpl.Type <= 15 || tpl.Type >= 29 && tpl.Type <= 34;
        }

        public static GameBalanceRules Load(string path, GameWorld world)
        {
            if (!File.Exists(path))
                return new GameBalanceRules();
            var root = Json.Parse(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>;
            if (root == null || (LootRules.Number(root, "version") != 1 && LootRules.Number(root, "version") != 2) || !root.ContainsKey("balance"))
                throw new ArgumentException("Invalid balance document.");
            return FromObject(root["balance"] as Dictionary<string, object>, world);
        }

        public void Save(string path)
        {
            var w = new JsonWriter();
            w.Value(new Dictionary<string, object> { { "version", 2 }, { "balance", ToObject() } });
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
