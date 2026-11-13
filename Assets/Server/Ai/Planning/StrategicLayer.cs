namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>The Strategic AI contract: decides what the agent wants to do next and why. Never touches game objects.</summary>
    public interface IStrategicPlanner
    {
        string Name { get; }

        /// <summary>Returns a decision, or throws when the planner cannot decide (the caller falls back to rules).</summary>
        Decision Decide(AgentSnapshot s);
    }
}
