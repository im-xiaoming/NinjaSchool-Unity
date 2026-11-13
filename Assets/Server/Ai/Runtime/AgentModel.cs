using System;
using System.Collections.Generic;
using NsoServer.Net;

namespace NsoServer.Ai
{
    using Math = System.Math; // the client has its own global Math class

    /// <summary>
    /// An offline AI player: a real character (account AccountPrefix + name in the save database) on a bot session.
    /// Everything about the character lives in its PlayerState, exactly as for a human player; this object only holds
    /// the AI's mind (plan, memory) and what the game AI is doing. All fields are touched under GameWorld.SyncRoot.
    /// </summary>
    public sealed class NinjaAgent
    {
        public int Index;
        public AgentPersonality P;
        public string Name
        {
            get
            {
                return P.Name;
            }
        }

        public string Account
        {
            get
            {
                return AgentConfig.AccountPrefix + P.Name;
            }
        }

        public Session Session;
        public BotClient Client;
        /// <summary>The character in the game (null while logging in or after an error).</summary>
        public PlayerState Me
        {
            get
            {
                return Session != null && Session.Connected && Session.Controller.Actions != null ? Session.Controller.Actions.State : null;
            }
        }

        public string LoginStatus = "waiting";
        public long LoginAt;
        // life statistics (the character's level, money and items are in Me)
        public int Kills, Deaths, QuestsDone;
        public readonly Rl.LearningState Learning = new Rl.LearningState();
        public string PolicyVersion = "";
        // strategic state
        public Decision Plan;
        public int StepIndex;
        public long StepStartedAt, PlanStartedAt, LastProgressAt, LastStallAskAt;
        public int StepProgress;
        public bool NeedsDecision = true, Pending;
        public long PendingSince, LastDecisionAt, NextReviewAt;
        public long DecisionRequest; // Correlates asynchronous replies; never persisted as character state.
        public string Trigger = "just arrived in the world";
        public readonly List<string> Memory = new List<string>();
        public readonly List<string> Decisions = new List<string>(); // debug history (last few)
        /// <summary>Long-term gameplay memory (saved), see AgentKnowledge.</summary>
        public readonly AgentKnowledge Knowledge = new AgentKnowledge();
        // tactical state (debug: what the game AI is doing right now)
        public string Activity = "logging in";
        public int TargetMob = -1, TargetMobTemplate = -1;
        public short TargetItem = -1;
        public NsoServer.World.ItemMapEntry LootTarget;
        public int LootBestDistance;
        public long LootProgressAt;
        /// <summary>Monsters the movement planner found no way to reach -> time until they may be chosen again.</summary>
        public readonly Dictionary<int, long> AvoidMobs = new Dictionary<int, long>();
        public readonly Dictionary<NsoServer.World.ItemMapEntry, long> AvoidLoot = new Dictionary<NsoServer.World.ItemMapEntry, long>();
        public int ZoneTarget = -1, ZoneMap = -1;
        public long LastZoneCheck, LastZoneChange, ZoneStartedAt, ZoneIntentAt;
        public List<int> Route; // maps still to walk through (travel)
        public long LastTargetSeen;
        public int WanderX = -1;
        public bool Recovering; // reflex in front of the plan step: low HP without potions / buying potions
        public long DeadSince;
        public int LastLevel, LastTaskId = -1, LastNpcMap = -1; // LastNpcMap: map whose NPC positions were last memorised
        public long LastMenuAt, LastPotionAt, LastEquipCheck;
        public int QuizChoice, QuizAnswered = -1;
        public string LastNoticeSeen = "";
        public readonly Dictionary<int, long> LockedMaps = new Dictionary<int, long>(); // map -> time it refused us
        public readonly HashSet<int> FailedItems = new HashSet<int>(); // bag items the server refused to equip / learn
        public bool Admitting; // putting the weapon away to join a school (do not re-equip)
        public float[] Priorities; // the LLM's weights for the rule planner (Priority), set at PrioritiesAt
        public long PrioritiesAt;
        public FastTrip Fast; // fast travel in progress (carriage or teleport scroll), see AgentTactics.PlanFastTravel
        public long NoFastTravelUntil; // a fast trip failed: walk until then
        public long NoTryUntil; // buying an item to try it failed: no new try until then (item ledger)
        public long NoFoodUntil; // the food shop refused or failed: do not plan a food trip again until then
        public long NoPotionsUntil; // after "no potions and no money": do not retry the recovery reflex until then
        public readonly HashSet<int> HumansSeen = new HashSet<int>(); // human players met in the current zone (greetings)
        public PlanStep Step
        {
            get
            {
                return Plan != null && StepIndex < Plan.Steps.Count ? Plan.Steps[StepIndex] : null;
            }
        }

        public int FleeHp
        {
            get
            {
                return Plan != null && Plan.FleeHpPercent >= 0 ? Plan.FleeHpPercent : AgentConfig.DefaultFleeHpPercent;
            }
        }

        /// <summary>Summary of older events written by the LLM when the context was full (saved).</summary>
        public string Summary = "";
        public void Remember(string text)
        {
            string line = DateTime.Now.ToString("MM-dd HH:mm") + " " + text;
            // every event also goes to the long-term memory (vector database), from where it can be recalled later
            AgentRag.Remember(Account, "event:" + DateTime.UtcNow.Ticks, "event", line);
            Memory.Add(line);
            while (Memory.Count > Math.Max(1, AgentConfig.MemorySize))
                Memory.RemoveAt(0);
        }
    }
}
