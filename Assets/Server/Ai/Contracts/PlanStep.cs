using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>One step of a strategic plan (Strategic AI -> Tactical AI). -1 means "not set".</summary>
    public sealed class PlanStep
    {
        public GoalType Goal;
        public int MapId = -1; // hunt / explore / travel
        public int MobId = -1; // hunt: monster template
        public int Count = 1; // hunt: kills, collect: items, shop: potions
        public int Seconds = 30; // idle
        public int X = -1, Y = -1; // move: pixel coordinates in MapId (or the current map); Y -1 = any height
        public string Note = ""; // validator remark (dropped parameter...)
        public override string ToString()
        {
            switch (Goal)
            {
                case GoalType.Hunt:
                    return "hunt " + Count + (MobId >= 0 ? " mob " + MobId : "") + (MapId >= 0 ? " @map " + MapId : "");
                case GoalType.Collect:
                    return "collect " + Count;
                case GoalType.Explore:
                    return "explore" + (MapId >= 0 ? " map " + MapId : "");
                case GoalType.Travel:
                    return "travel map " + MapId;
                case GoalType.Idle:
                    return "idle " + Seconds + "s";
                case GoalType.Quest:
                    return "quest";
                case GoalType.Shop:
                    return MobId >= 0 ? "try item " + MobId : "buy potions";
                case GoalType.Move:
                    return "go to (" + X + "," + Y + ")" + (MapId >= 0 ? " @map " + MapId : "");
                default:
                    return "rest";
            }
        }
    }
}
