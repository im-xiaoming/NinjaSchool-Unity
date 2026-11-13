using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai.Rl
{
    using Math = System.Math;
    using Message = NsoServer.Net.Message;
    using Cmd = NsoServer.Net.Cmd;

    /// <summary>Offline learning integration. Called under SyncRoot, except FPS and background IO publication.</summary>
    public static class RlRuntime
    {
        public static volatile RlSettings Settings = new RlSettings();
        public static string TrainerScript = "", PythonExecutable = "python", LearningRootOverride = "";
        public static bool ExternalTrainer;
        public static string EvaluationModelFolder = ""; // CLI-only scratch evaluator; never set by the web
        private static RlPolicy evaluationStrategy, evaluationTactical;
        private static RlBridge bridge;
        private static readonly Random Rng = new Random();
        private static readonly Dictionary<Session, NinjaAgent> owners = new Dictionary<Session, NinjaAgent>();
        private static readonly Dictionary<Session, NinjaAgent> humans = new Dictionary<Session, NinjaAgent>();
        private static readonly string RunId = Guid.NewGuid().ToString("N");
        private static long sequence;
        private static float fps = 120;
        private static bool executing;
        private static double inferenceMs;
        private static double tickInferenceMs;
        private static readonly Queue<double> inferenceWindow = new Queue<double>();
        private static string lastRelease = "";
        public static void Start(GameServer server)
        {
            Stop();
            owners.Clear();
            humans.Clear();
            sequence = 0;
            inferenceWindow.Clear();
            tickInferenceMs = 0;
            if (!string.IsNullOrEmpty(EvaluationModelFolder))
            {
                evaluationStrategy = RlPolicy.Load(Path.Combine(EvaluationModelFolder, "strategy.json"), "strategy");
                string tactical = Path.Combine(EvaluationModelFolder, "tactical.json");
                evaluationTactical = File.Exists(tactical) ? RlPolicy.Load(tactical, "tactical") : null;
                Settings = new RlSettings
                {
                    Mode = PolicyMode.RL,
                    Stage = evaluationStrategy.Stage,
                    Learning = false,
                    Guidance = 0
                };
                return;
            }

            string root = string.IsNullOrEmpty(LearningRootOverride) ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(server.Config.DataDirectory)), "NsoLearning") : LearningRootOverride;
            bridge = new RlBridge(root, TrainerScript, server.Config.DataDirectory, Path.GetDirectoryName(server.World.DataDirectory), ExternalTrainer);
            lastRelease = "";
        }

        public static void Stop()
        {
            if (bridge != null)
            {
                bridge.Dispose();
                bridge = null;
            }

            owners.Clear();
            humans.Clear();
            evaluationStrategy = evaluationTactical = null;
            executing = false;
        }

        public static void Register(NinjaAgent a)
        {
            owners[a.Session] = a;
            a.Learning.Visited.Add(a.Me.MapId);
        }

        public static void Fps(float value)
        {
            if (!float.IsNaN(value) && !float.IsInfinity(value))
                fps = value;
            if (bridge != null)
            {
                bridge.FramesPerSecond = fps;
                Interlocked.Exchange(ref bridge.FrameSampleAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }

        public static void Tick(GameWorld world, long now)
        {
            tickInferenceMs = 0;
            var release = Active(true);
            if (release != null && release.Version != lastRelease && evaluationStrategy == null)
            {
                // A passing release enables only its evaluated tier. Manual reductions remain until a new release.
                lastRelease = release.Version;
                Configure(new Dictionary<string, object> { { "stage", release.Stage }, { "guidance", release.Stage == 1 ? .5 : release.Stage == 2 ? .25 : .1 } });
            }

            foreach (var a in owners.Values)
            {
                if (a.Me == null)
                    continue;
                if (a.Me.IsDead)
                {
                    Close(a, false, null, true, false, now);
                    Close(a, true, null, true, false, now);
                }
            }

            var remove = new List<Session>();
            foreach (var kv in humans)
            {
                if (kv.Value.Me == null)
                {
                    remove.Add(kv.Key);
                    continue;
                }

                if (kv.Value.Learning.Tactical != null && now - kv.Value.Learning.Tactical.Start.At >= 500)
                    Close(kv.Value, false, RlCandidates.Tactical(kv.Value, world, 0, null, now), kv.Value.Me.IsDead, false, now);
            }

            foreach (var s in remove)
            {
                SaveHumanRewardHistory(s);
                humans.Remove(s);
            }
        }

        public static void EndTick()
        {
            inferenceWindow.Enqueue(tickInferenceMs);
            while (inferenceWindow.Count > 128)
                inferenceWindow.Dequeue();
        }

        public static Decision Decide(NinjaAgent a, GameWorld w, AgentSnapshot snapshot, Decision teacher, long now)
        {
            var o = RlCandidates.Strategy(a, w, snapshot, teacher);
            Close(a, true, o, false, false, now);
            int teacherAction = 47;
            if (teacher.Steps.Count > 0)
                for (int i = 0; i < 47; i++)
                {
                    var proposed = o.Candidates[i];
                    var target = teacher.Steps[0];
                    if (proposed != null && proposed.Valid && proposed.Step != null && proposed.Step.Goal == target.Goal && (target.MapId < 0 || proposed.Step.MapId == target.MapId) && (target.MobId < 0 || proposed.Step.MobId == target.MobId))
                    {
                        teacherAction = i;
                        break;
                    }
                }

            int action = Choose(a, o, teacherAction, true, now);
            var c = o.Candidates[action];
            if (action == teacherAction || action == 47 || c.Step == null)
            {
                var guided = new Decision
                {
                    Source = a.Learning.LastSource,
                    Reason = teacher.Reason,
                    Say = teacher.Say,
                    FleeHpPercent = teacher.FleeHpPercent
                };
                guided.Steps.AddRange(teacher.Steps);
                return guided;
            }

            return new Decision
            {
                Source = "rl",
                Reason = "RL: " + c.Name,
                FleeHpPercent = a.Learning.Advice != null ? a.Learning.Advice.FleeHpPercent : teacher.FleeHpPercent,
                Steps =
                {
                    c.Step
                }
            };
        }

        // One primitive decision per bot per server tick. Menu/item/equipment macros remain shared existing skills.
        public static bool Intercept(Session session, sbyte cmd, byte[] payload)
        {
            if (executing || !session.IsBot || !owners.TryGetValue(session, out var a) || a.Me == null || a.Me.IsDead)
                return false;
            var w = session.Server.World;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!RlCandidates.IsPrimitive(cmd, payload, a.Me, w))
                return false;
            long tick = now / 500;
            if (a.Learning.LastTacticTick == tick)
                return false;
            // Selecting a skill is part of an attack macro; learn the resulting attack instead of cancelling selection.
            if (cmd == 41)
                return false;
            var o = RlCandidates.Tactical(a, w, cmd, payload, now);
            int teacher = RlCandidates.Match(o, cmd, payload, a.Me);
            if (teacher < 0)
                return false;
            var active = Active(false);
            int stage = active != null ? Math.Min(Settings.Stage, active.Stage) : 0;
            // Stage 2 learns combat only; teacher movement is still recorded for later imitation.
            bool movement = cmd == Cmd.PLAYER_MOVE;
            bool policyAllowed = stage >= (movement ? 3 : 2);
            if (!movement && stage < 3)
                for (int i = 1; i <= 8; i++)
                {
                    o.ActionMask[i] = false;
                    o.Candidates[i].Valid = false;
                    o.Values[Observation.GlobalSize + i * Observation.CandidateSize] = 0;
                }

            Close(a, false, o, false, false, now);
            int action = Choose(a, o, teacher, false, now, policyAllowed, movement ? 3 : 2);
            a.Learning.LastTacticTick = tick;
            if (action == teacher)
                return false; // preserve the exact teacher action; movement destinations use the common client executor
            var c = o.Candidates[action];
            executing = true;
            try
            {
                if (c.Kind == 1)
                    a.Client.Move(c.X, c.Y);
                else if (c.Kind == 2)
                {
                    a.TargetMob = c.Id;
                    foreach (var mob in a.Me.Zone.Mobs)
                        if (mob.Id == c.Id)
                            a.TargetMobTemplate = mob.Template.Id;
                    a.Client.SelectSkill(c.Skill);
                    a.Client.Attack(c.Id);
                }
                else if (c.Kind == 3)
                    a.Client.UseItem(c.Id);
                else if (c.Kind == 4)
                    a.Client.Pick(c.Id);
            // kind 0 deliberately waits; the next tick continues the same tactical skill.
            }
            finally
            {
                executing = false;
            }

            return true;
        }

        public static void RecordHuman(Session session, PlayerState me, Message message)
        {
            if (!Settings.RecordHuman || session.IsBot || me == null || me.IsDead || bridge == null)
                return;
            byte[] data = message.Data;
            if (!RlCandidates.IsPrimitive(message.Command, data, me, session.Server.World) || message.Command == 41)
                return;
            var a = HumanAgent(session);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (a.Learning.LastTacticTick == now / 500)
                return; // opt-in input capture must not become per-frame inference work
            a.Learning.LastTacticTick = now / 500;
            var o = RlCandidates.Tactical(a, session.Server.World, message.Command, data, now);
            Close(a, false, o, false, false, now);
            int action = RlCandidates.Match(o, message.Command, data, me);
            if (action >= 0)
                a.Learning.Tactical = Begin(a, o, action, "human", "teacher", 0, 0, now);
        }

        private static NinjaAgent HumanAgent(Session session)
        {
            if (humans.TryGetValue(session, out var existing))
                return existing;
            var a = new NinjaAgent
            {
                Session = session,
                P = new AgentPersonality
                {
                    Name = "human",
                    Aggression = .5f,
                    Caution = .5f,
                    Greed = .5f,
                    Curiosity = .5f,
                    Sociability = .5f
                }
            };
            a.Learning.Visited.Add(a.Me.MapId);
            try
            {
                string saved = session.Server.Accounts.GetDocument("rl_reward_history_" + a.Me.Record.Id);
                if (saved != null)
                    a.Learning.ItemRewards.Restore(Json.Parse(saved));
            }
            catch (Exception ex)
            {
                session.Server.Log("Reward history load error: " + ex.Message);
            }

            humans[session] = a;
            return a;
        }

        public static ActionReceipt BeginAction(Session session, Message message)
        {
            if (!ActionRewards.Observes(message.Command))
                return null;
            try
            {
                NinjaAgent a;
                if (session.IsBot)
                {
                    if (!owners.TryGetValue(session, out a))
                        return null;
                }
                else
                {
                    if (!Settings.RecordHuman || bridge == null || session.Controller.Actions == null || !session.Connected)
                        return null;
                    a = HumanAgent(session);
                }

                return ActionRewards.Begin(a, message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            catch (Exception ex)
            {
                session.Server.Log("Reward observation error (before): " + ex.Message);
                return null;
            }
        }

        public static void EndAction(ActionReceipt receipt)
        {
            try
            {
                ActionRewards.Complete(receipt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            catch (Exception ex)
            {
                if (receipt != null)
                    receipt.Agent.Session.Server.Log("Reward observation error (after): " + ex.Message);
            }
        }

        public static void SaveHumanRewardHistory(Session session)
        {
            if (!humans.TryGetValue(session, out var a) || !a.Learning.ItemRewards.Dirty || session.Controller.Actions == null)
                return;
            try
            {
                var state = session.Controller.Actions.State;
                session.Server.Accounts.SetDocument("rl_reward_history_" + state.Record.Id, RlBridge.Serialize(a.Learning.ItemRewards.ToObject()));
                a.Learning.ItemRewards.Dirty = false;
            }
            catch (Exception ex)
            {
                session.Server.Log("Reward history save error: " + ex.Message);
            }
        }

        public static void SaveHumanRewardHistories()
        {
            foreach (var session in humans.Keys)
                SaveHumanRewardHistory(session);
        }

        private static RlPolicy Active(bool strategic)
        {
            if (evaluationStrategy != null)
                return strategic ? evaluationStrategy : evaluationTactical;
            return bridge == null ? null : strategic ? bridge.Strategy : bridge.Tactical;
        }

        private static int Choose(NinjaAgent a, Observation o, int teacher, bool strategy, long now, bool allowed = true, int minimumStage = 1)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int action = teacher;
            string source = "rules", version = "teacher";
            double lp = 0, value = 0;
            var p = Active(strategy);
            var cfg = Settings;
            if (p != null && p.Version != a.PolicyVersion)
            {
                if (InCombat(a.Me))
                    p = strategy ? a.Learning.StrategyPolicy : a.Learning.TacticalPolicy;
                else
                {
                    a.Learning.StrategyPolicy = Active(true);
                    a.Learning.TacticalPolicy = Active(false);
                    a.PolicyVersion = p.Version;
                }
            }

            bool usable = allowed && p != null && p.Stage >= minimumStage && cfg.Stage >= minimumStage && cfg.Mode != PolicyMode.Rules;
            // A policy change never interrupts a fight. Old streams are truncated at their next decision boundary.
            if (usable && (cfg.Mode == PolicyMode.RL || Rng.NextDouble() >= cfg.Guidance))
            {
                try
                {
                    action = p.Sample(o, Rng, out lp, out value);
                    source = "rl";
                    version = p.Version;
                    a.PolicyVersion = version;
                    a.Learning.Fallback = "";
                }
                catch (Exception ex)
                {
                    a.Learning.Fallback = "Inference rejected: " + ex.Message;
                }
            }
            else
                a.Learning.Fallback = p == null ? "No evaluated model; collecting teacher demonstrations" : !allowed ? "Tactical stage still guided" : "Teacher guidance / safe model boundary";
            a.Learning.LastSource = source;
            var t = Begin(a, o, action, source, version, lp, value, now);
            if (strategy)
                a.Learning.Strategy = t;
            else
                a.Learning.Tactical = t;
            string name = o.Candidates[action].Name;
            a.Learning.Actions.TryGetValue(name, out int count);
            a.Learning.Actions[name] = count + 1;
            a.Learning.History.Enqueue(action / (float)Observation.ActionCount);
            while (a.Learning.History.Count > 8)
                a.Learning.History.Dequeue();
            inferenceMs = sw.Elapsed.TotalMilliseconds;
            tickInferenceMs += inferenceMs;
            return action;
        }

        private static Transition Begin(NinjaAgent a, Observation o, int action, string source, string version, double lp, double value, long now)
        {
            return new Transition
            {
                Before = o,
                Start = Outcome.Read(a.Me, now),
                Action = action,
                Source = source,
                Version = version,
                LogProbability = lp,
                Value = value,
                Stream = RunId + ":" + a.Account + ":" + o.Domain,
                ActionRewardBaseline = (double[])a.Learning.ItemRewards.Totals.Clone()
            };
        }

        private static bool InCombat(PlayerState p)
        {
            if (p == null || p.Zone == null)
                return false;
            foreach (var m in p.Zone.Mobs)
                if (!m.IsDead && m.Chars.Contains(p.Record.Id))
                    return true;
            return false;
        }

        public static void FlushAgent(NinjaAgent a, long now)
        {
            if (a.Me != null)
            {
                Close(a, false, null, a.Me.IsDead, true, now);
                Close(a, true, null, a.Me.IsDead, true, now);
            }
        }

        /// <summary>What dying on <paramref name = "map"/> really costs (RlSettings): the revive wait, plus the walk back from
        /// the save village when the current plan has business away from the village.</summary>
        public static double DeathCost(NinjaAgent a, int map, RlSettings cfg)
        {
            var me = a.Me;
            if (me == null)
                return cfg.DeathPenalty;
            var world = me.Session.Server.World;
            int home = me.Progress != null ? me.Progress.SaveMap : 22;
            var step = a.Step;
            bool wantsVillage = a.Recovering || (step != null && (step.Goal == GoalType.Shop || step.Goal == GoalType.Rest)) || AgentTactics.HpPotions(me, world) < AgentConfig.MinPotionsToFight;
            if (wantsVillage)
                return cfg.DeathPenalty;
            var route = WorldKnowledge.Route(world, home, map, me.Record.TaskId);
            int hops = route != null ? route.Count : 3; // unknown way back: a typical distance
            return cfg.DeathPenalty + cfg.DeathHopPenalty * hops;
        }

        private static void Close(NinjaAgent a, bool strategic, Observation next, bool terminated, bool truncated, long now)
        {
            var t = strategic ? a.Learning.Strategy : a.Learning.Tactical;
            if (t == null)
                return;
            if (strategic)
                a.Learning.Strategy = null;
            else
                a.Learning.Tactical = null;
            var end = Outcome.Read(a.Me, now);
            var cfg = Settings;
            var start = t.Start;
            // Reward verified outcomes, never HP gained from respawn, chatter, menu clicks or repeat map crossings.
            double quest = end.Task > start.Task ? cfg.QuestReward * Math.Min(2, end.Task - start.Task) : 0;
            double step = end.Task == start.Task && end.Step > start.Step ? cfg.StepReward : 0;
            double exp = cfg.ExpReward * Math.Max(-1, Math.Min(1, (end.Exp - start.Exp) / 100.0)); // exp lost counts too
            double money = cfg.MoneyReward * Math.Max(-1, Math.Min(1, (end.Money - start.Money) / 100.0)) * (0.5 + a.P.Greed);
            double death = !start.Dead && end.Dead ? -DeathCost(a, end.Map, cfg) * (0.5 + a.P.Caution) : 0;
            double explore = a.Learning.Visited.Add(end.Map) ? cfg.ExplorationReward * (0.5 + a.P.Curiosity) : 0;
            bool stuck = t.Before.Candidates[t.Action].Kind == 1 && start.Map == end.Map && start.X == end.X && start.Y == end.Y && now - start.At >= 400;
            double stall = stuck ? -cfg.StuckPenalty : 0;
            if (stuck)
                a.Learning.Stuck++;
            if (death < 0 && !strategic)
                a.Learning.Deaths++;
            var actionRewards = new Dictionary<string, object>();
            double actionTotal = 0;
            for (int i = 0; i < ActionRewardHistory.Names.Length; i++)
            {
                double delta = a.Learning.ItemRewards.Totals[i] - (t.ActionRewardBaseline != null ? t.ActionRewardBaseline[i] : 0);
                actionRewards[ActionRewardHistory.Names[i]] = delta;
                actionTotal += delta;
            }

            double reward = quest + step + exp + money + death + explore + stall + actionTotal;
            a.Learning.Reward += reward;
            if (bridge == null || (!cfg.Learning && t.Source != "human") || (t.Source == "human" && !cfg.RecordHuman))
                return;
            if (next == null)
            {
                if (strategic)
                {
                    var snapshot = AgentManager.Snapshot(a);
                    next = RlCandidates.Strategy(a, a.Me.Session.Server.World, snapshot, new RulePlanner().Decide(snapshot));
                }
                else
                    next = RlCandidates.Tactical(a, a.Me.Session.Server.World, 0, null, now);
            }

            // No expensive string building or disk writes under SyncRoot. Everything below is a detached copy.
            bridge.Enqueue(new Dictionary<string, object> { { "schema", Observation.Schema }, { "id", RunId + ":" + Interlocked.Increment(ref sequence) }, { "stream", t.Stream }, { "domain", t.Before.Domain }, { "source", t.Source }, { "version", t.Version }, { "obs", t.Before.Values }, { "mask", t.Before.ActionMask }, { "action", t.Action }, { "logp", t.LogProbability }, { "value", t.Value }, { "reward", reward }, { "components", new Dictionary<string, object> { { "quest", quest }, { "step", step }, { "exp", exp }, { "money", money }, { "death", death }, { "explore", explore }, { "stuck", stall }, { "actions", actionRewards } } }, { "next", next.Values }, { "nextMask", next.ActionMask }, { "terminated", terminated }, { "truncated", truncated }, { "elapsedMs", now - start.At }, { "personality", a.P.Name }, { "task", end.Task }, { "at", now } });
        }

        public static Dictionary<string, object> SettingsObject()
        {
            var s = Settings;
            return new Dictionary<string, object>
            {
                {
                    "defaultsVersion",
                    RlSettings.DefaultsVersion
                },
                {
                    "mode",
                    s.Mode.ToString()
                },
                {
                    "learning",
                    s.Learning
                },
                {
                    "recordHuman",
                    s.RecordHuman
                },
                {
                    "autoApply",
                    s.AutoApply
                },
                {
                    "evaluationEnabled",
                    s.EvaluationEnabled
                },
                {
                    "stage",
                    s.Stage
                },
                {
                    "guidance",
                    s.Guidance
                },
                {
                    "device",
                    s.Device
                },
                {
                    "questReward",
                    s.QuestReward
                },
                {
                    "stepReward",
                    s.StepReward
                },
                {
                    "expReward",
                    s.ExpReward
                },
                {
                    "moneyReward",
                    s.MoneyReward
                },
                {
                    "deathPenalty",
                    s.DeathPenalty
                },
                {
                    "deathHopPenalty",
                    s.DeathHopPenalty
                },
                {
                    "stuckPenalty",
                    s.StuckPenalty
                },
                {
                    "explorationReward",
                    s.ExplorationReward
                },
                {
                    "newItemUseReward",
                    s.NewItemUseReward
                },
                {
                    "newItemPurchaseReward",
                    s.NewItemPurchaseReward
                },
                {
                    "timelyItemUseReward",
                    s.TimelyItemUseReward
                },
                {
                    "npcDiscoveryReward",
                    s.NpcDiscoveryReward
                },
                {
                    "usefulNpcFeatureReward",
                    s.UsefulNpcFeatureReward
                },
                {
                    "usefulHpPercent",
                    s.UsefulHpPercent
                },
                {
                    "usefulMpPercent",
                    s.UsefulMpPercent
                },
                {
                    "usefulItemCooldownSeconds",
                    s.UsefulItemCooldownSeconds
                },
                {
                    "usefulBagFreeSlots",
                    s.UsefulBagFreeSlots
                },
                {
                    "ledgerUselessAfter",
                    s.LedgerUselessAfter
                },
                {
                    "wastefulPurchasePenalty",
                    s.WastefulPurchasePenalty
                }
            };
        }

        public static void RestoreSettings(string json)
        {
            var d = Json.Parse(json) as Dictionary<string, object>;
            Configure(d, true);
            // Settings saved before RlSettings.DefaultsVersion 2 were Guided / stage 0 / 50 % guidance: adopt RL control once.
            object v;
            int saved = d != null && d.TryGetValue("defaultsVersion", out v) ? Convert.ToInt32(v) : 0;
            var fresh = new RlSettings();
            if (d != null && saved < 2)
                Configure(new Dictionary<string, object> { { "mode", fresh.Mode.ToString() }, { "stage", fresh.Stage }, { "guidance", (double)fresh.Guidance } });
            // version 3: the old flat death penalty (3) becomes the revive-wait part of the real death cost, once
            if (d != null && saved < 3)
                Configure(new Dictionary<string, object> { { "deathPenalty", fresh.DeathPenalty } });
        }

        public static void Configure(Dictionary<string, object> d, bool restore = false)
        {
            if (d == null)
                throw new ArgumentException("Expected settings object");
            var old = Settings;
            // Replace rather than mutate: worker/main/tick threads read a consistent settings snapshot.
            var s = new RlSettings
            {
                Mode = old.Mode,
                Learning = old.Learning,
                RecordHuman = old.RecordHuman,
                AutoApply = old.AutoApply,
                EvaluationEnabled = old.EvaluationEnabled,
                Stage = old.Stage,
                Guidance = old.Guidance,
                Device = old.Device,
                QuestReward = old.QuestReward,
                StepReward = old.StepReward,
                ExpReward = old.ExpReward,
                MoneyReward = old.MoneyReward,
                DeathPenalty = old.DeathPenalty,
                DeathHopPenalty = old.DeathHopPenalty,
                StuckPenalty = old.StuckPenalty,
                ExplorationReward = old.ExplorationReward,
                NewItemUseReward = old.NewItemUseReward,
                NewItemPurchaseReward = old.NewItemPurchaseReward,
                TimelyItemUseReward = old.TimelyItemUseReward,
                NpcDiscoveryReward = old.NpcDiscoveryReward,
                UsefulNpcFeatureReward = old.UsefulNpcFeatureReward,
                UsefulHpPercent = old.UsefulHpPercent,
                UsefulMpPercent = old.UsefulMpPercent,
                UsefulItemCooldownSeconds = old.UsefulItemCooldownSeconds,
                UsefulBagFreeSlots = old.UsefulBagFreeSlots,
                LedgerUselessAfter = old.LedgerUselessAfter,
                WastefulPurchasePenalty = old.WastefulPurchasePenalty
            };
            foreach (var kv in d)
            {
                switch (kv.Key)
                {
                    case "mode":
                        if (!Enum.TryParse(Convert.ToString(kv.Value), out PolicyMode mode) || !Enum.IsDefined(typeof(PolicyMode), mode))
                            throw new ArgumentException("Unknown policy mode");
                        s.Mode = mode;
                        break;
                    case "defaultsVersion":
                        break; // written by SettingsObject, read by RestoreSettings
                    case "learning":
                        s.Learning = Convert.ToBoolean(kv.Value);
                        break;
                    case "recordHuman":
                        s.RecordHuman = Convert.ToBoolean(kv.Value);
                        break;
                    case "autoApply":
                        s.AutoApply = Convert.ToBoolean(kv.Value);
                        break;
                    case "evaluationEnabled":
                        s.EvaluationEnabled = Convert.ToBoolean(kv.Value);
                        break;
                    case "stage":
                        s.Stage = Math.Max(0, Math.Min(3, Convert.ToInt32(kv.Value)));
                        break;
                    case "guidance":
                        s.Guidance = (float)Number(kv.Value, 0, 1);
                        break;
                    case "device":
                        if (Convert.ToString(kv.Value) != "cpu" && Convert.ToString(kv.Value) != "cuda")
                            throw new ArgumentException("Device must be cpu or cuda");
                        s.Device = Convert.ToString(kv.Value);
                        break;
                    case "questReward":
                        s.QuestReward = Number(kv.Value, 0, 20);
                        break;
                    case "stepReward":
                        s.StepReward = Number(kv.Value, 0, 10);
                        break;
                    case "expReward":
                        s.ExpReward = Number(kv.Value, 0, 5);
                        break;
                    case "moneyReward":
                        s.MoneyReward = Number(kv.Value, 0, 5);
                        break;
                    case "deathPenalty":
                        s.DeathPenalty = Number(kv.Value, 0, 20);
                        break;
                    case "deathHopPenalty":
                        s.DeathHopPenalty = Number(kv.Value, 0, 5);
                        break;
                    case "stuckPenalty":
                        s.StuckPenalty = Number(kv.Value, 0, 5);
                        break;
                    case "explorationReward":
                        s.ExplorationReward = Number(kv.Value, 0, 5);
                        break;
                    case "newItemUseReward":
                        s.NewItemUseReward = Number(kv.Value, 0, 5);
                        break;
                    case "newItemPurchaseReward":
                        s.NewItemPurchaseReward = Number(kv.Value, 0, 5);
                        break;
                    case "timelyItemUseReward":
                        s.TimelyItemUseReward = Number(kv.Value, 0, 5);
                        break;
                    case "npcDiscoveryReward":
                        s.NpcDiscoveryReward = Number(kv.Value, 0, 5);
                        break;
                    case "usefulNpcFeatureReward":
                        s.UsefulNpcFeatureReward = Number(kv.Value, 0, 5);
                        break;
                    case "usefulHpPercent":
                        s.UsefulHpPercent = Integer(kv.Value, 1, 100);
                        break;
                    case "usefulMpPercent":
                        s.UsefulMpPercent = Integer(kv.Value, 1, 100);
                        break;
                    case "usefulItemCooldownSeconds":
                        s.UsefulItemCooldownSeconds = Integer(kv.Value, 1, 3600);
                        break;
                    case "usefulBagFreeSlots":
                        s.UsefulBagFreeSlots = Integer(kv.Value, 0, 200);
                        break;
                    case "ledgerUselessAfter":
                        s.LedgerUselessAfter = Integer(kv.Value, 1, 100);
                        break;
                    case "wastefulPurchasePenalty":
                        s.WastefulPurchasePenalty = Number(kv.Value, 0, 5);
                        break;
                    default:
                        if (!restore)
                            throw new ArgumentException("Unknown setting " + kv.Key);
                        break;
                }
            }

            Settings = s;
        }

        private static double Number(object v, double min, double max)
        {
            double n = Convert.ToDouble(v);
            if (double.IsNaN(n) || double.IsInfinity(n) || n < min || n > max)
                throw new ArgumentException("Setting outside range");
            return n;
        }

        private static int Integer(object value, int min, int max)
        {
            double n = Number(value, min, max);
            if (n != Math.Truncate(n))
                throw new ArgumentException("Setting must be an integer");
            return (int)n;
        }

        public static void Request(string command)
        {
            if (bridge == null)
                throw new ArgumentException("Learning runtime is not running");
            if (command == "apply")
                bridge.ApplyRequested = true;
            else if (command == "rollback")
                bridge.RollbackRequested = true;
            else
                throw new ArgumentException("Unknown learning command");
        }

        public static Dictionary<string, object> Debug()
        {
            var d = SettingsObject();
            d["model"] = Active(true) != null ? Active(true).Version : "teacher";
            d["modelStage"] = Active(true) != null ? Active(true).Stage : 0;
            d["samples"] = bridge != null ? Interlocked.Read(ref bridge.Written) : 0;
            d["dropped"] = bridge != null ? Interlocked.Read(ref bridge.Dropped) : 0;
            d["trainer"] = bridge != null ? bridge.TrainerStatus : "off";
            d["trainerDetails"] = bridge != null ? bridge.TrainerDetails : null;
            d["evaluationDetails"] = bridge != null ? bridge.EvaluationDetails : null;
            d["candidate"] = bridge != null ? bridge.Candidate : "";
            d["evaluation"] = bridge != null ? bridge.Evaluation : "";
            d["error"] = bridge != null ? bridge.Error : "";
            d["fps"] = fps;
            d["lastInferenceMs"] = inferenceMs;
            var samples = inferenceWindow.ToArray();
            Array.Sort(samples);
            d["inferenceTickP95Ms"] = samples.Length > 0 ? samples[(int)Math.Floor((samples.Length - 1) * .95)] : 0;
            return d;
        }
    }
}
