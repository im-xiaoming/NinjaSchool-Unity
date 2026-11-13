using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>What a plan step asks the tactical layer to do.</summary>
    public enum GoalType
    {
        Hunt,
        Collect,
        Rest,
        Explore,
        Travel,
        Idle,
        Quest,
        Shop,
        Move
    } // Move: go to coordinates (LLM "goto")
}
