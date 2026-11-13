using System;
using System.Collections.Generic;
using NsoServer.Net;

namespace NsoServer.Ai.Rl
{
    // Offline addition: learning contracts are independent of saves, packets and Unity.
    public enum PolicyMode
    {
        Rules,
        Guided,
        RL
    }

    public sealed class RlSettings
    {
        // User 2026-11-13: "bỏ rule chuyển sang RL" -> RL controls by default, no teacher mixing; the rule teacher stays
        // only as the fallback while no evaluated model exists (and as the demonstration source for training).
        public const int DefaultsVersion = 3; // 3: death priced by its real cost (DeathPenalty 3 -> 0.5 + DeathHopPenalty)
        public PolicyMode Mode = PolicyMode.RL;
        public bool Learning = true, RecordHuman, AutoApply = true, EvaluationEnabled;
        public int Stage = 3; // 0 teacher, 1 strategy, 2 combat, 3 movement; a model still only acts up to its own evaluated stage
        public float Guidance = 0f;
        public string Device = "cuda"; // User requested RTX GPU training; inference stays lightweight CPU C#.
        // User 2026-11-13: "dying is not always bad, e.g. dying to get back faster than walking". A monster kill costs no
        // experience here (Java takes exp only in PK, Char.java hieuChien), so the real cost is time: the revive wait
        // (DeathPenalty) plus walking back from the village, DeathHopPenalty per map between the village and the place
        // of death. Nothing to walk back to (the plan needs the village: shop, rest, out of potions) = only the wait.
        // Experience actually lost still counts through the exp reward, which may now be negative.
        public double QuestReward = 4, StepReward = 1, ExpReward = 0.3, MoneyReward = 0.05, DeathPenalty = 0.5, DeathHopPenalty = 0.4, StuckPenalty = 0.2, ExplorationReward = 0.2;
        public double NewItemUseReward = 0.2, NewItemPurchaseReward = 0.1, TimelyItemUseReward = 0.3, NpcDiscoveryReward = 0.1, UsefulNpcFeatureReward = 0.3;
        public int UsefulHpPercent = 50, UsefulMpPercent = 25, UsefulItemCooldownSeconds = 30;
        public int UsefulBagFreeSlots = 2;
        // Item ledger (user 2026-11-13): an item used this many times without any visible use counts as useless;
        // buying it again costs WastefulPurchasePenalty on top of the money.
        public int LedgerUselessAfter = 3;
        public double WastefulPurchasePenalty = 0.3;
    }

    public sealed class ActionCandidate
    {
        public string Name;
        public bool Valid;
        public int Kind, Id, Map = -1, X, Y, Skill = -1;
        public int TemplateId, LevelGap;
        public float HpFraction;
        public PlanStep Step;
        public sbyte Command;
        public byte[] Payload;
    }

    public sealed class Observation
    {
        public const int Schema = 1, ActionCount = 48, GlobalSize = 40, CandidateSize = 8;
        public const int Size = GlobalSize + ActionCount * CandidateSize;
        public float[] Values = new float[Size];
        public bool[] ActionMask = new bool[ActionCount];
        public ActionCandidate[] Candidates = new ActionCandidate[ActionCount];
        public string Domain;
    }

    public sealed class Outcome
    {
        public long Exp, Money, At;
        public int Task, Step, Hp, Map, X, Y;
        public bool Dead;
        public static Outcome Read(PlayerState p, long now)
        {
            return new Outcome
            {
                Exp = p.Record.Exp,
                Money = p.Progress.Yen + p.Progress.Xu,
                At = now,
                Task = p.Record.TaskId,
                Step = p.Progress.TaskActive ? p.Progress.TaskIndex + 1 : 0,
                Hp = p.Hp,
                Dead = p.IsDead,
                Map = p.MapId,
                X = p.X,
                Y = p.Y
            };
        }
    }

    public sealed class Transition
    {
        public Observation Before;
        public Outcome Start;
        public int Action;
        public string Source, Version, Stream;
        public double LogProbability, Value;
        public double[] ActionRewardBaseline;
    }

    public sealed class LearningState
    {
        public Transition Strategy, Tactical;
        public long LastTacticTick = -1;
        public string LastSource = "rules", Fallback = "No evaluated model yet";
        public Decision Advice;
        public RlPolicy StrategyPolicy, TacticalPolicy;
        public readonly HashSet<int> Visited = new HashSet<int>();
        public readonly Queue<float> History = new Queue<float>();
        public readonly Dictionary<string, int> Actions = new Dictionary<string, int>();
        public int Stuck, Deaths;
        public double Reward;
        public int LastNpc = -1;
        public int PendingUseItem = -1;
        public string PendingItemMenu = "";
        public readonly ActionRewardHistory ItemRewards = new ActionRewardHistory();
    }
}
