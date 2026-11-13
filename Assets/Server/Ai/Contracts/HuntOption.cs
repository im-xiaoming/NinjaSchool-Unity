using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>A hunting ground the planner may choose: a map (with its distance) and a monster kind on it.</summary>
    public sealed class HuntOption
    {
        public int MapId, Hops, MobId, MobLevel;
        public string MapName, MobName;
    }
}
