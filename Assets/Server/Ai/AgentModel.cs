using System;
using System.Collections.Generic;
using NsoServer.Net;

namespace NsoServer.Ai
{
    using Math = System.Math;   // the client has its own global Math class

    /// <summary>What a plan step asks the tactical layer to do.</summary>
    public enum GoalType { Hunt, Collect, Rest, Explore, Travel, Idle, Quest, Shop, Move }   // Move: go to coordinates (LLM "goto")

    /// <summary>One step of a strategic plan (Strategic AI -> Tactical AI). -1 means "not set".</summary>
    public sealed class PlanStep
    {
        public GoalType Goal;
        public int MapId = -1;      // hunt / explore / travel
        public int MobId = -1;      // hunt: monster template
        public int Count = 1;       // hunt: kills, collect: items, shop: potions
        public int Seconds = 30;    // idle
        public int X = -1, Y = -1;  // move: pixel coordinates in MapId (or the current map); Y -1 = any height
        public string Note = "";    // validator remark (dropped parameter...)

        public override string ToString()
        {
            switch (Goal)
            {
                case GoalType.Hunt: return "hunt " + Count + (MobId >= 0 ? " mob " + MobId : "") + (MapId >= 0 ? " @map " + MapId : "");
                case GoalType.Collect: return "collect " + Count;
                case GoalType.Explore: return "explore" + (MapId >= 0 ? " map " + MapId : "");
                case GoalType.Travel: return "travel map " + MapId;
                case GoalType.Idle: return "idle " + Seconds + "s";
                case GoalType.Quest: return "quest";
                case GoalType.Shop: return MobId >= 0 ? "try item " + MobId : "buy potions";
                case GoalType.Move: return "go to (" + X + "," + Y + ")" + (MapId >= 0 ? " @map " + MapId : "");
                default: return "rest";
            }
        }
    }

    /// <summary>A fast trip towards a travel target: by the carriage NPC (Umayaki) or a teleport scroll, to Dest.</summary>
    public sealed class FastTrip { public int Target, Dest, Npc = -1, BagIndex = -1, MenuIndex = -1, Saved; public string Label = ""; }

    /// <summary>A strategic decision: why, an optional line to say, a risk constraint and the steps.</summary>
    public sealed class Decision
    {
        public string Reason = "";
        public string Say = "";
        public int FleeHpPercent = -1;   // constraint for the tactical layer; -1 = AgentConfig.DefaultFleeHpPercent
        public readonly List<PlanStep> Steps = new List<PlanStep>();
        public string Source = "";       // "rl" / "rules" / "llm" (the LLM plan when AgentConfig.LlmLeads)
        public string Raw = "";          // the LLM's text (debug)
        public string Summary;           // new "story so far" written by the LLM when the context was full (null = none)
        public int SummaryCut;           // oldest Memory events the summary replaces
        public float[] Priorities;       // LLM weights for the rule planner (Priority.Names order); null = unchanged
    }

    /// <summary>The rule planner's scores the LLM may weight (RulePlanner multiplies each by its weight, 0..3).</summary>
    public static class Priority
    {
        public static readonly string[] Names = { "quest", "hunt", "shop", "explore", "collect", "idle" };
        public const int Quest = 0, Hunt = 1, Shop = 2, Explore = 3, Collect = 4, Idle = 5;
        public const float Max = 3f;
        public static string Text(float[] w)
        {
            if (w == null) return "default (all 1)";
            var parts = new string[Names.Length];
            for (int i = 0; i < Names.Length; i++) parts[i] = Names[i] + " " + w[i].ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return string.Join(", ", parts);
        }
    }

    /// <summary>Kinds of reports from the tactical layer to the strategic layer.</summary>
    public enum AgentEvent { Spawned, StepDone, StepFailed, PlanDone, Died, Respawned, LevelUp, LowHp, Kill, Loot, PlayerNearby, Review, Note, Quest, Shop }

    /// <summary>Who the AI player is: character name, look, the school it wants to join and traits (0..1) that both
    /// planners read.</summary>
    public sealed class AgentPersonality
    {
        /// <summary>Character name: lower-case letters and digits, 5-15 characters (Controller.CreateCharacter rule).</summary>
        public string Name;
        public string Bio;
        /// <summary>The class it chooses at the school (1 kiếm, 2 phi tiêu: Hirosaki; 3 kunai, 4 cung: Ookaza; 5 đao, 6 quạt: Haruna).</summary>
        public int ClassId;
        public int Gender, Head;
        public float Aggression, Caution, Greed, Curiosity, Sociability;
        public string[] Greetings;

        public string Traits()
        {
            return "aggression " + Aggression.ToString("0.0") + ", caution " + Caution.ToString("0.0") + ", greed " + Greed.ToString("0.0") +
                   ", curiosity " + Curiosity.ToString("0.0") + ", sociability " + Sociability.ToString("0.0");
        }

        private static AgentPersonality P(string name, int cls, int gender, int head, float ag, float ca, float gr, float cu, float so, string bio, params string[] hi)
        {
            return new AgentPersonality { Name = name, ClassId = cls, Gender = gender, Head = head, Aggression = ag, Caution = ca, Greed = gr, Curiosity = cu, Sociability = so, Bio = bio, Greetings = hi };
        }

        // heads: CreateCharScr.hairID (male 11/26/27/28, female 2/23/24/25)
        public static readonly AgentPersonality[] Presets =
        {
            P("raiden", 1, 0, 26, 0.9f, 0.2f, 0.4f, 0.3f, 0.4f, "Hot-headed swordsman who wants to be the strongest ninja of the village. Loves hard fights, hates waiting.",
                "Đấu với ta không?", "Ta sẽ mạnh nhất làng này!", "Đi săn quái cùng không?"),
            P("hanabi", 4, 1, 23, 0.4f, 0.7f, 0.8f, 0.4f, 0.6f, "Careful trader's daughter. Counts every yen, avoids risky fights and always keeps potions.",
                "Chào bạn! Có bán gì không?", "Hôm nay kiếm được nhiều yên ghê.", "Cẩn thận quái mạnh đấy."),
            P("sorano", 5, 0, 27, 0.5f, 0.4f, 0.3f, 0.9f, 0.5f, "Wanderer who wants to see every map of the world. Gets bored staying in one place.",
                "Bạn đã tới Rừng Moshio chưa?", "Thế giới rộng lớn quá!", "Mình vừa khám phá chỗ mới."),
            P("yukine", 3, 1, 2, 0.3f, 0.6f, 0.4f, 0.5f, 0.9f, "Friendly girl who loves meeting other ninjas and chatting in the village between hunts.",
                "Xin chào! Bạn mới tới à?", "Hôm nay trời đẹp ghê.", "Cùng làm nhiệm vụ nhé!"),
            P("kenta", 2, 0, 11, 0.6f, 0.5f, 0.6f, 0.3f, 0.2f, "Quiet, methodical grinder. Hunts monsters of his level in long efficient sessions.",
                "...", "Đừng làm phiền.", "Ừ, chào."),
            P("mikasa", 6, 1, 24, 0.7f, 0.3f, 0.5f, 0.7f, 0.7f, "Cheerful adventurer who takes on any challenge and tells everyone about it.",
                "Ê! Đi phiêu lưu không?", "Mình vừa hạ được con quái to lắm!", "Chào nha~"),
            P("haruto", 1, 0, 28, 0.6f, 0.5f, 0.3f, 0.4f, 0.5f, "Dutiful student who follows every quest of the village elders to the letter.",
                "Trưởng làng giao nhiệm vụ mới rồi!", "Học hành chăm chỉ nhé.", "Chào bạn đồng môn."),
            P("akane", 4, 1, 25, 0.5f, 0.5f, 0.6f, 0.6f, 0.4f, "Archer with sharp eyes for loot. Picks up everything and sells nothing.",
                "Có thấy đồ rơi không?", "Túi mình sắp đầy rồi.", "Bắn trúng rồi!"),
            P("renji", 2, 0, 11, 0.8f, 0.3f, 0.4f, 0.5f, 0.3f, "Shuriken thrower who loves speed. Rushes to the next quest as soon as he can.",
                "Nhanh lên nào!", "Chậm quá đó.", "Tới lượt ta!"),
            P("takumi", 3, 0, 26, 0.4f, 0.8f, 0.5f, 0.3f, 0.4f, "Cautious kunai user. Never fights without a full stack of potions.",
                "An toàn là trên hết.", "Bạn có đủ bình máu chưa?", "Đừng liều quá."),
            P("sakura", 6, 1, 2, 0.5f, 0.4f, 0.4f, 0.6f, 0.8f, "Fan ninja who cheers on every newcomer and likes busy villages.",
                "Chào mừng tới làng!", "Cố lên nha!", "Hôm nay vui ghê."),
            P("daichi", 5, 0, 27, 0.9f, 0.4f, 0.3f, 0.2f, 0.3f, "Blade ninja built like a rock. Picks the strongest monster he can survive.",
                "Quái càng mạnh càng tốt.", "Hừ.", "Thử sức không?"),
            P("natsuki", 3, 1, 24, 0.4f, 0.6f, 0.7f, 0.5f, 0.6f, "Practical girl saving yen for better equipment. Plans every trip.",
                "Tiết kiệm là quan trọng.", "Mua đồ ở đâu rẻ nhỉ?", "Chào bạn nhé."),
        };
    }

    /// <summary>
    /// An offline AI player: a real character (account AccountPrefix + name in the save database) on a bot session.
    /// Everything about the character lives in its PlayerState, exactly as for a human player; this object only holds
    /// the AI's mind (plan, memory) and what the game AI is doing. All fields are touched under GameWorld.SyncRoot.
    /// </summary>
    public sealed class NinjaAgent
    {
        public int Index;
        public AgentPersonality P;
        public string Name { get { return P.Name; } }
        public string Account { get { return AgentConfig.AccountPrefix + P.Name; } }

        public Session Session;
        public BotClient Client;
        /// <summary>The character in the game (null while logging in or after an error).</summary>
        public PlayerState Me { get { return Session != null && Session.Connected && Session.Controller.Actions != null ? Session.Controller.Actions.State : null; } }
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
        public string Trigger = "just arrived in the world";
        public readonly List<string> Memory = new List<string>();
        public readonly List<string> Decisions = new List<string>();   // debug history (last few)
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
        public List<int> Route;              // maps still to walk through (travel)
        public long LastTargetSeen;
        public int WanderX = -1;
        public bool Recovering;              // reflex in front of the plan step: low HP without potions / buying potions
        public long DeadSince;
        public int LastLevel, LastTaskId = -1, LastNpcMap = -1;   // LastNpcMap: map whose NPC positions were last memorised
        public long LastMenuAt, LastPotionAt, LastEquipCheck;
        public int QuizChoice, QuizAnswered = -1;
        public string LastNoticeSeen = "";
        public readonly Dictionary<int, long> LockedMaps = new Dictionary<int, long>();   // map -> time it refused us
        public readonly HashSet<int> FailedItems = new HashSet<int>();   // bag items the server refused to equip / learn
        public bool Admitting;               // putting the weapon away to join a school (do not re-equip)
        public float[] Priorities;           // the LLM's weights for the rule planner (Priority), set at PrioritiesAt
        public long PrioritiesAt;
        public FastTrip Fast;                // fast travel in progress (carriage or teleport scroll), see AgentTactics.PlanFastTravel
        public long NoFastTravelUntil;       // a fast trip failed: walk until then
        public long NoTryUntil;              // buying an item to try it failed: no new try until then (item ledger)
        public long NoFoodUntil;             // the food shop refused or failed: do not plan a food trip again until then
        public long NoPotionsUntil;          // after "no potions and no money": do not retry the recovery reflex until then
        public readonly HashSet<int> HumansSeen = new HashSet<int>();      // human players met in the current zone (greetings)

        public PlanStep Step { get { return Plan != null && StepIndex < Plan.Steps.Count ? Plan.Steps[StepIndex] : null; } }
        public int FleeHp { get { return Plan != null && Plan.FleeHpPercent >= 0 ? Plan.FleeHpPercent : AgentConfig.DefaultFleeHpPercent; } }

        /// <summary>Summary of older events written by the LLM when the context was full (saved).</summary>
        public string Summary = "";

        public void Remember(string text)
        {
            string line = DateTime.Now.ToString("MM-dd HH:mm") + " " + text;
            // every event also goes to the long-term memory (vector database), from where it can be recalled later
            AgentRag.Remember(Account, "event:" + DateTime.UtcNow.Ticks, "event", line);
            Memory.Add(line);
            while (Memory.Count > Math.Max(1, AgentConfig.MemorySize)) Memory.RemoveAt(0);
        }
    }
}
