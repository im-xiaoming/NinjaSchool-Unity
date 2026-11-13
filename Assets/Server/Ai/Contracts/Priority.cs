using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>The rule planner's scores the LLM may weight (RulePlanner multiplies each by its weight, 0..3).</summary>
    public static class Priority
    {
        public static readonly string[] Names =
        {
            "quest",
            "hunt",
            "shop",
            "explore",
            "collect",
            "idle"
        };
        public const int Quest = 0, Hunt = 1, Shop = 2, Explore = 3, Collect = 4, Idle = 5;
        public const float Max = 3f;
        public static string Text(float[] w)
        {
            if (w == null)
                return "default (all 1)";
            var parts = new string[Names.Length];
            for (int i = 0; i < Names.Length; i++)
                parts[i] = Names[i] + " " + w[i].ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return string.Join(", ", parts);
        }
    }
}
