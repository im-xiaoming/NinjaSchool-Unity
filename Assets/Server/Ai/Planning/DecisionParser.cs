using System;
using System.Collections.Generic;
using System.Globalization;
using NsoServer.Data;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>Parses the planner's JSON answer into a Decision (no world validation; AgentManager validates).</summary>
    public static class DecisionParser
    {
        public static Decision Parse(string content, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(content))
            {
                error = "empty answer (thinking used the whole token budget?)";
                return null;
            }

            // thinking models may still put their reasoning inline
            int think = content.LastIndexOf("</think>", StringComparison.Ordinal);
            if (think >= 0)
                content = content.Substring(think + 8);
            int a = content.IndexOf('{'), z = content.LastIndexOf('}');
            if (a < 0 || z <= a)
            {
                error = "no JSON object in the answer";
                return null;
            }

            Dictionary<string, object> root;
            try
            {
                root = Json.Parse(content.Substring(a, z - a + 1)) as Dictionary<string, object>;
            }
            catch (Exception ex)
            {
                error = "invalid JSON: " + ex.Message;
                return null;
            }

            if (root == null)
            {
                error = "answer is not a JSON object";
                return null;
            }

            var d = new Decision
            {
                Reason = Str(root, "reason"),
                Say = Str(root, "say"),
                FleeHpPercent = Int(root, "fleeHp", -1)
            };
            object o;
            if (!root.TryGetValue("steps", out o) || !(o is List<object> steps) || steps.Count == 0)
            {
                error = "no steps";
                return null;
            }

            foreach (var so in steps)
            {
                var st = so as Dictionary<string, object>;
                if (st == null)
                    continue;
                GoalType g;
                if (!TryGoal(Str(st, "goal"), out g))
                    continue;
                d.Steps.Add(new PlanStep { Goal = g, MapId = Int(st, "map", -1), MobId = Int(st, "mob", -1), Count = Int(st, "count", 5), Seconds = Int(st, "seconds", 30), X = Int(st, "x", -1), Y = Int(st, "y", -1) });
            }

            if (d.Steps.Count == 0)
            {
                error = "no known goal in the steps";
                return null;
            }

            // optional: weights for the rule planner (missing names stay 1)
            if (root.TryGetValue("priorities", out o) && o is Dictionary<string, object> pr)
            {
                d.Priorities = new float[Priority.Names.Length];
                for (int i = 0; i < d.Priorities.Length; i++)
                {
                    d.Priorities[i] = 1f;
                    object v;
                    if (pr.TryGetValue(Priority.Names[i], out v) && v != null)
                        try
                        {
                            d.Priorities[i] = (float)Convert.ToDouble(v, CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                        }
                }
            }

            return d;
        }

        private static bool TryGoal(string s, out GoalType g)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "hunt":
                case "fight":
                case "train":
                case "hit":
                case "attack":
                case "kill":
                case "farm":
                case "grind":
                    g = GoalType.Hunt;
                    return true;
                case "collect":
                case "gather":
                case "loot":
                    g = GoalType.Collect;
                    return true;
                case "rest":
                case "heal":
                case "recover":
                case "drink":
                    g = GoalType.Rest;
                    return true;
                case "explore":
                    g = GoalType.Explore;
                    return true;
                case "travel":
                case "go":
                case "move":
                case "walk":
                    g = GoalType.Travel;
                    return true;
                case "idle":
                case "wait":
                case "chat":
                    g = GoalType.Idle;
                    return true;
                case "quest":
                case "mission":
                case "task":
                case "questline":
                    g = GoalType.Quest;
                    return true;
                case "shop":
                case "buy":
                case "buy_potions":
                case "potions":
                    g = GoalType.Shop;
                    return true;
                case "goto":
                case "go_to":
                case "moveto":
                case "position":
                    g = GoalType.Move;
                    return true;
            }

            g = GoalType.Idle;
            return false;
        }

        private static string Str(Dictionary<string, object> d, string k)
        {
            object o;
            return d.TryGetValue(k, out o) && o != null ? Convert.ToString(o, CultureInfo.InvariantCulture) : "";
        }

        private static int Int(Dictionary<string, object> d, string k, int def)
        {
            object o;
            if (!d.TryGetValue(k, out o) || o == null)
                return def;
            try
            {
                return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Json.Long(o)));
            }
            catch (Exception)
            {
                return def;
            }
        }
    }
}
