using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>A strategic decision: why, an optional line to say, a risk constraint and the steps.</summary>
    public sealed class Decision
    {
        public string Reason = "";
        public string Say = "";
        public int FleeHpPercent = -1; // constraint for the tactical layer; -1 = AgentConfig.DefaultFleeHpPercent
        public readonly List<PlanStep> Steps = new List<PlanStep>();
        public string Source = ""; // "rl" / "rules" / "llm" (the LLM plan when AgentConfig.LlmLeads)
        public string Raw = ""; // the LLM's text (debug)
        public string Summary; // new "story so far" written by the LLM when the context was full (null = none)
        public int SummaryCut; // oldest Memory events the summary replaces
        public float[] Priorities; // LLM weights for the rule planner (Priority.Names order); null = unchanged
    }
}
