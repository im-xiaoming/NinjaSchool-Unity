using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>Kinds of reports from the tactical layer to the strategic layer.</summary>
    public enum AgentEvent
    {
        Spawned,
        StepDone,
        StepFailed,
        PlanDone,
        Died,
        Respawned,
        LevelUp,
        LowHp,
        Kill,
        Loot,
        PlayerNearby,
        Review,
        Note,
        Quest,
        Shop
    }
}
