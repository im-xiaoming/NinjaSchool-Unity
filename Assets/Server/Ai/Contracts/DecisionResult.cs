using System.Collections.Generic;

namespace NsoServer.Ai
{
    // ====================================================================== shared LLM service
    /// <summary>The outcome of one queued request.</summary>
    public sealed class DecisionResult
    {
        public int AgentId;
        public long RequestSequence;
        public Decision Decision; // null on failure
        public string Error;
        public long LatencyMs;
    }
}
