using System;
using System.Collections.Generic;
using System.Text;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using Zone = NsoServer.World.Zone;

    /// <summary>
    /// Offline AI players (offline addition; the Java server only had scripted Bot instances).
    /// Each AI player is a real character: its own account (AgentConfig.AccountPrefix + name) and character in the save
    /// database, logged in on a bot session (GameServer.ConnectBot) and driven with the messages a game client sends
    /// (BotClient), so every game rule applies to it exactly as to a human player and other players see it through the
    /// normal zone messages (Presence). It starts as a new character (level 1, quest 0).
    /// - Strategic AI decides WHAT and WHY: evaluated RL, with the utility planner as teacher/fallback. The shared
    ///   LLM service provides asynchronous advice and speech. Requests are event-driven and throttled, never per frame.
    /// - Tactical AI decides HOW: AgentTactics shares its skills with RL primitive selection every 500 ms.
    /// - Memory: recent events (Memory) and long-term gameplay knowledge (AgentKnowledge), saved in the save database
    ///   (document "ai_players") and given to the LLM with the character's bag, equipment, experience and quest.
    /// - Their actions, screen messages, decisions and states go to the bot log (PlayerLog.Bot, kept 20 days).
    /// They cannot use the dev terminal (Controller ignores CHAT_MAP commands from bot sessions).
    /// Everything except the LLM worker runs on the server tick thread under GameWorld.SyncRoot.
    /// </summary>
    public static class AgentManager
    {
        private static readonly List<NinjaAgent> Agents = new List<NinjaAgent>();
        private static readonly Random Rng = new Random();
        private static readonly RulePlanner Rules = new RulePlanner();
        private static StrategicService service;
        private static GameServer server;
        private static GameWorld world;
        private static long lastSave;

        /// <summary>The AI players' memories in the save database (AccountStore.GetDocument / SetDocument).</summary>
        private const string SaveKey = "ai_players";

        public static bool Running { get { return world != null; } }
        public static StrategicService Service { get { return service; } }

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        // Courtesy reservations affect decisions only; the server never forbids another character from picking/hitting.
        internal static bool LootClaimed(NinjaAgent requester, short itemId)
        {
            foreach (var a in Agents)
                if (a != requester && a.Me != null && a.Me.Zone == requester.Me.Zone && !a.Me.IsDead && a.TargetItem == itemId &&
                    a.LootTarget != null && Now() - a.LootProgressAt <= System.Math.Max(1, AgentConfig.LootStallSeconds) * 1000L) return true;
            return false;
        }
        internal static bool MobClaimed(NinjaAgent requester, int mobId)
        {
            foreach (var a in Agents)
                if (a != requester && a.Me != null && a.Me.Zone == requester.Me.Zone && !a.Me.IsDead && !a.Recovering &&
                    a.ZoneTarget < 0 && a.TargetMob == mobId && a.Step != null) return true;
            return false;
        }
        internal static int ProjectedZonePopulation(int mapId, Zone zone, long now)
        {
            int count = zone.NumberChar;
            foreach (var a in Agents)
            {
                if (a.Me == null || a.Me.MapId != mapId || a.ZoneMap != mapId || a.ZoneTarget < 0 || now - a.ZoneIntentAt > 1500) continue;
                if (a.Me.Zone == zone) count--;
                if (a.ZoneTarget == zone.Id) count++;
            }
            return Math.Max(0, count);
        }

        // ================================================================== lifecycle

        /// <summary>Prepares the AI players (they log in one after another from the tick) and starts the LLM worker.
        /// Called by ServerBootstrap after the server started; never by the dotnet tests.</summary>
        public static void Start(GameServer s)
        {
            if (!AgentConfig.Enabled || s == null || s.World == null) return;
            lock (s.World.SyncRoot)
            {
                server = s;
                world = s.World;
                Agents.Clear();
                Rl.RlRuntime.Start(s); // offline learning owns IO/trainer lifecycle
                var saved = Load();
                AgentRag.Start(s.World);   // long-term memory in Qdrant (queues only; the worker embeds in the background)
                int count = Math.Max(0, Math.Min(AgentConfig.AgentCount, AgentPersonality.Presets.Length));
                long now = Now();
                for (int i = 0; i < count; i++)
                {
                    var a = new NinjaAgent { Index = i, P = AgentPersonality.Presets[i] };
                    Dictionary<string, object> rec;
                    a.Knowledge.Owner = a.Account;
                    if (saved.TryGetValue(a.Name, out rec)) Restore(a, rec);
                    a.Knowledge.MirrorAll();
                    a.LoginAt = now + 1000L + i * Math.Max(0, AgentConfig.LoginSpacingSeconds) * 1000L;
                    a.LastDecisionAt = now - AgentConfig.MinDecisionSeconds * 1000L + i * 2000L;   // stagger the first requests
                    Agents.Add(a);
                }
                lastSave = now;
                if (AgentConfig.LlmEnabled)
                {
                    service = new StrategicService(new LlmPlanner());
                    service.Start();
                }
                Log("AI players: " + Agents.Count + (service != null ? ", LLM " + AgentConfig.LlmModel + " at " + AgentConfig.LlmEndpoint : ", rules only"));
            }
        }

        /// <summary>Saves the memories, logs the characters out (they are saved like players) and stops.</summary>
        public static void Stop()
        {
            if (world == null) return;
            lock (world.SyncRoot)
            {
                try { Save(); } catch (Exception ex) { Log("AI save error: " + ex.Message); }
                foreach (var a in Agents) Rl.RlRuntime.FlushAgent(a, Now());
                Rl.RlRuntime.Stop();
                foreach (var a in Agents) if (a.Session != null) { try { a.Session.Close(); } catch (Exception) { } }
                if (service != null) service.Stop();
                AgentRag.Stop();
                service = null;
                Agents.Clear();
                world = null;
                server = null;
            }
        }

        // ================================================================== tick

        /// <summary>Headless client frames only; no decisions, training, combat ticks or gameplay clock changes.</summary>
        public static void TickMovement(GameWorld w, long now)
        {
            if (world == null || w != world) return;
            foreach (var a in Agents)
                if (a.Session != null && a.Session.Connected && a.Client != null) a.Client.StepMovement(now);
        }

        /// <summary>Server tick (GameServer.TickLoop, inside SyncRoot): log-ins, decisions in, tactics, decisions out.</summary>
        public static void Tick(GameWorld w, long now)
        {
            if (world == null || w != world) return;
            DrainResults(now);
            Rl.RlRuntime.Tick(w, now);
            foreach (var a in Agents)
            {
                if (!LoggedIn(a, now)) continue;
                try
                {
                    a.Client.ReleaseMovement(); // no renewed walk input means stop horizontal motion; gravity still runs
                    Observe(a, now);
                    AgentTactics.Update(a, world, now);
                }
                catch (Exception ex)
                {
                    Log("AI " + a.Name + " tactics error: " + ex.Message);
                    PlayerLog.Bot(a.Account, "ERR", ex.ToString());
                    a.Plan = null; a.NeedsDecision = true;
                }
                MaybeDecide(a, now);
            }
            Rl.RlRuntime.EndTick();
            if (AgentConfig.SaveSeconds > 0 && now - lastSave > AgentConfig.SaveSeconds * 1000L)
            {
                lastSave = now;
                try { Save(); } catch (Exception ex) { Log("AI save error: " + ex.Message); }
            }
        }

        // the log-in a player does: client info, data ok, create the character the first time, select it
        private static bool LoggedIn(NinjaAgent a, long now)
        {
            if (a.Me != null) return true;
            if (now < a.LoginAt) return false;
            if (a.Session != null && !a.Session.Connected) a.Session = null;
            try
            {
                if (a.Session == null)
                {
                    a.Session = server.ConnectBot(a.Account);
                    a.Client = new BotClient(a.Session) { Agent = a };
                    a.Client.ClientInfo();
                    a.Client.ClientOk();
                }
                a.Client.CreateChar(a.Name, a.P.Gender, a.P.Head);   // Controller refuses it when the character exists
                a.Client.SelectChar(a.Name);
            }
            catch (Exception ex) { a.LoginStatus = "error: " + ex.Message; }
            var me = a.Me;
            if (me == null)
            {
                a.LoginStatus = "could not enter the game" + (a.Session != null && a.Session.LastNotice.Length > 0 ? ": " + a.Session.LastNotice : " (character name taken by another account?)");
                if (a.Session != null) { try { a.Session.Close(); } catch (Exception) { } }
                a.Session = null;
                a.LoginAt = now + 60000;   // try again in a minute
                Log("AI " + a.Name + ": " + a.LoginStatus);
                return false;
            }
            a.LoginStatus = "in game";
            Rl.RlRuntime.Register(a);
            a.LastLevel = me.Level;
            a.LastTaskId = me.Record.TaskId;
            a.NextReviewAt = now + AgentConfig.ReviewSeconds * 1000L;
            Report(a, AgentEvent.Spawned, "entered the game in map " + me.MapId + " " + me.Zone.Tile.Name + " (level " + me.Level + ", quest " + me.Record.TaskId + ")");
            return true;
        }

        // what a player notices without acting: level ups, quest changes, messages on screen, human players nearby
        private static void Observe(NinjaAgent a, long now)
        {
            var me = a.Me;
            if (me.MapId != a.LastNpcMap && me.Zone != null)
            {
                // like a player looking around a new map: remember where every NPC of this map stands (kept and saved)
                a.LastNpcMap = me.MapId;
                var tile = me.Zone.Tile;
                var line = new StringBuilder();
                line.Append("map ").Append(me.MapId).Append(" \"").Append(tile.Name).Append("\": ");
                if (tile.Npcs.Count == 0) line.Append("no NPCs");
                for (int i = 0; i < tile.Npcs.Count; i++)
                {
                    var n = tile.Npcs[i];
                    line.Append(i > 0 ? "; " : "").Append(AgentTactics.NpcName(world, n.TemplateId)).Append(" (id ").Append(n.TemplateId)
                        .Append(") at (").Append(n.X).Append(',').Append(n.Y).Append(')');
                }
                a.Knowledge.Learn(AgentKnowledge.NpcPrefix + me.MapId, line.ToString());
            }
            if (me.Level > a.LastLevel)
            {
                a.LastLevel = me.Level;
                Report(a, AgentEvent.LevelUp, "reached level " + me.Level);
                if (a.P.Sociability >= 0.5f) Say(a, "Lên cấp " + me.Level + " rồi!");
            }
            string notice = a.Session.LastNotice;
            if (notice.Length > 0 && notice != a.LastNoticeSeen)
            {
                a.LastNoticeSeen = notice;
                if (notice.Length < 140) a.Remember("saw: " + notice);
            }
            // a human player came into the zone: sociable characters greet
            foreach (PlayerState p in me.Zone.Players)
            {
                if (p == me || p.Session == null || p.Session.IsBot || a.HumansSeen.Contains(p.Record.Id)) continue;
                a.HumansSeen.Add(p.Record.Id);
                if (Rng.Next(100) < (int)(a.P.Sociability * AgentConfig.GreetPercent) && a.P.Greetings.Length > 0)
                    Say(a, a.P.Greetings[Rng.Next(a.P.Greetings.Length)]);
                a.Remember("met player " + p.Record.Name + " on map " + me.MapId);
                a.Knowledge.Learn("player:" + p.Record.Id, "met player " + p.Record.Name + " (level " + p.Level + ")");
            }
            if (a.HumansSeen.Count > 0)
                a.HumansSeen.RemoveWhere(id => !me.Zone.Players.Exists(o => ((PlayerState)o).Record.Id == id));
        }

        // event-driven requests, throttled; a plan under review keeps running until the answer comes
        private static void MaybeDecide(NinjaAgent a, long now)
        {
            var me = a.Me;
            if (me == null || me.IsDead) return;
            // LLM advice is asynchronous and must never stop the RL/teacher decision loop.
            if (a.Pending && now - a.PendingSince > (AgentConfig.LlmTimeoutSeconds + 15) * 1000L)
                a.Pending = false;
            bool throttled = now - a.LastDecisionAt < AgentConfig.MinDecisionSeconds * 1000L;
            if (a.NeedsDecision)
            {
                // A player with no plan gets a teacher/RL decision immediately, independently of LLM availability.
                if (!throttled || a.Plan == null) Decide(a, now, null, false);
                return;
            }
            // Busy for a long time without any result (no kill, pickup, quest step, new map...): the LLM steps in, told what
            // is going on (with the map coordinates), and its plan replaces the stuck one (AgentConfig.LlmStallSeconds).
            if (AgentConfig.LlmStallSeconds > 0 && service != null && AgentConfig.LlmEnabled && service.IsAvailable(now) && !a.Pending &&
                a.Plan != null && now - a.LastProgressAt > AgentConfig.LlmStallSeconds * 1000L && now - a.LastStallAskAt > AgentConfig.LlmStallSeconds * 1000L)
            {
                a.LastStallAskAt = now;
                long idle = (now - a.LastProgressAt) / 1000;
                a.Trigger = "NO RESULT for " + idle + " s while \"" + a.Activity + "\" (step " + (a.Step != null ? a.Step.ToString() : "none") +
                    "). The current approach is not working: choose a different one (another target, place, map or goal; goto coordinates from CURRENT MAP).";
                Remember(a, "stuck: no result for " + idle + " s while " + a.Activity);
                Decide(a, now, null, false);
                return;
            }
            if (AgentConfig.ReviewSeconds > 0 && now >= a.NextReviewAt && !throttled)
            {
                a.Trigger = "periodic review of the current plan (" + (a.Step != null ? a.Step.ToString() : "none") + ")";
                Decide(a, now, null, false);
            }
        }

        private static void Decide(NinjaAgent a, long now, string fallbackWhy, bool forceRules)
        {
            a.NeedsDecision = false;
            a.LastDecisionAt = now;
            int review = AgentConfig.LlmLeads && service != null ? AgentConfig.LlmReviewSeconds : AgentConfig.ReviewSeconds;
            a.NextReviewAt = now + Math.Max(30, review) * 1000L;
            var snap = Snapshot(a);
            if (!a.Pending && !forceRules && service != null && AgentConfig.LlmEnabled && service.IsAvailable(now) && service.Enqueue(snap))
            {
                a.Pending = true;
                a.PendingSince = now;
            }
            // LLM leads: a plan still running is kept until the LLM answers; rules/RL only fill a gap (no plan, or the
            // LLM is unavailable / failed). Otherwise rules/RL decide at once and the LLM only advises.
            if (AgentConfig.LlmLeads && a.Pending && a.Plan != null && a.Step != null) return;
            Apply(a, Rl.RlRuntime.Decide(a, world, snap, Rules.Decide(snap), now), now, fallbackWhy);
        }

        private static void DrainResults(long now)
        {
            if (service == null) return;
            DecisionResult r;
            while (service.TryTake(out r))
            {
                NinjaAgent a = null;
                foreach (var x in Agents) if (x.Index == r.AgentId) a = x;
                if (a == null || !a.Pending) continue;   // timed out meanwhile: rules already decided
                a.Pending = false;
                if (a.Me == null || a.Me.IsDead) { a.NeedsDecision = true; continue; }
                // LLM leads (AgentConfig.LlmLeads): its validated plan replaces the interim rules/RL plan. Otherwise it only
                // suggests goals/risk/speech (RL features) and RL or its teacher installs plans.
                if (r.Decision != null)
                {
                    a.Learning.Advice = r.Decision;
                    if (r.Decision.Summary != null)
                    {
                        // the context was full: the LLM's summary replaces the oldest events (they stay in the vector memory)
                        a.Summary = r.Decision.Summary;
                        int cut = Math.Min(r.Decision.SummaryCut, a.Memory.Count);
                        if (cut > 0) a.Memory.RemoveRange(0, cut);
                        PlayerLog.Bot(a.Account, "AI", "summary: " + a.Summary);
                    }
                    if (AgentConfig.LlmLeads && r.Decision.Steps.Count > 0 && Apply(a, r.Decision, now, null)) { }
                    else if (!string.IsNullOrEmpty(r.Decision.Say)) Say(a, r.Decision.Say);
                }
                else a.Remember("LLM advice unavailable: " + r.Error);
            }
        }

        // ================================================================== strategic <-> tactical

        private static readonly string[] ClassNames = { "no school yet", "Ninja kiếm", "Ninja phi tiêu", "Ninja kunai", "Ninja cung", "Ninja đao", "Ninja quạt" };

        /// <summary>The planner's view of the AI player (copied, safe to use on the worker thread).</summary>
        internal static AgentSnapshot Snapshot(NinjaAgent a)
        {
            var p = a.P;
            var me = a.Me;
            int map = me.MapId;
            int task = me.Record.TaskId;
            long expLevel = world.ExpForLevel(me.Level), expNext = world.ExpForLevel(me.Level + 1);
            var s = new AgentSnapshot
            {
                AgentId = a.Index, Name = p.Name, Bio = p.Bio, Traits = p.Traits(),
                ClassName = me.Record.ClassId >= 0 && me.Record.ClassId < ClassNames.Length ? ClassNames[me.Record.ClassId] : "class " + me.Record.ClassId,
                Aggression = p.Aggression, Caution = p.Caution, Greed = p.Greed, Curiosity = p.Curiosity, Sociability = p.Sociability,
                Level = me.Level, HpPercent = AgentTactics.HpPercent(me), MpPercent = AgentTactics.MpPercent(me),
                Exp = me.Record.Exp, ExpNext = expNext,
                ExpPercent = expNext > expLevel ? Math.Max(0, Math.Min(100, (me.Record.Exp - expLevel) * 100.0 / (expNext - expLevel))) : 100,
                Kills = a.Kills, Deaths = a.Deaths, QuestsDone = a.QuestsDone, Yen = me.Progress.Yen, Xu = me.Progress.Xu,
                MapId = map, MapName = me.Zone.Tile.Name, Trigger = a.Trigger,
                PlayersHere = me.Zone.Players.Count - 1, InVillage = IsVillage(map),
                HpPotions = AgentTactics.HpPotions(me, world), MpPotions = AgentTactics.MpPotions(me, world), FreeSlots = PlayerItems.SlotNull(me),
                HasWeapon = me.Inventory.Equipment[1] != null, CanShop = task >= 3,
                CurrentPlan = a.Plan != null && a.Step != null ? a.Plan.Reason + " / step " + (a.StepIndex + 1) + " of " + a.Plan.Steps.Count + ": " + a.Step : "",
                UnlockedMaps = MapUnlock.Allowed(task).Count,
                Seed = Rng.Next()
            };
            s.CanAffordPotions = s.CanShop && (me.Progress.Yen >= 100 || me.Progress.Xu >= 100);
            long nowMs = Now();
            s.Foods = AgentTactics.Foods(me, world); s.Fed = AgentTactics.Fed(me, nowMs); s.WantsFood = AgentTactics.WantsFood(a, me, world, nowMs);
            s.Priorities = CurrentPriorities(a, nowMs);
            // bag and equipment, by name
            var counts = new Dictionary<string, int>();
            foreach (var it in me.Inventory.Bag)
            {
                if (it == null) continue;
                var t = world.FindItem(it.Id);
                if (t == null || t.Type == PlayerItems.TypeHp || t.Type == PlayerItems.TypeMp) continue;
                int n;
                counts.TryGetValue(t.Name, out n);
                counts[t.Name] = n + (t.IsUpToUp ? it.Quantity : 1);
            }
            foreach (var kv in counts) s.Bag.Add(kv.Key + (kv.Value > 1 ? " x" + kv.Value : ""));
            foreach (var it in me.Inventory.Equipment)
            {
                if (it == null) continue;
                var t = world.FindItem(it.Id);
                if (t != null) s.Equipment.Add(t.Name + (it.Upgrade > 0 ? " +" + it.Upgrade : ""));
            }
            // main quest
            var q = world.FindTask(task);
            if (q != null)
            {
                s.QuestId = task; s.QuestName = q.Name; s.QuestLevel = q.LevelRequire;
                int idx = me.Progress.TaskIndex;
                if (!me.Progress.TaskActive) s.QuestState = "not taken yet (talk to " + AgentTactics.NpcName(world, PlayerTasks.TaskNpcId(me, world)) + ")";
                else
                {
                    s.QuestState = "step " + (idx + 1) + " of " + q.SubNames.Length;
                    s.QuestStep = idx < q.SubNames.Length ? q.SubNames[idx] : "";
                    int target = idx < q.Counts.Length ? q.Counts[idx] : -1;
                    if (target > 0) s.QuestHint = me.Progress.TaskCount + "/" + target;
                }
                s.QuestGuide = AgentTactics.QuestGuide(me, world);
                s.QuestBlockedByLevel = me.Level < q.LevelRequire && (!me.Progress.TaskActive || idx == 0);
                if (s.QuestBlockedByLevel) s.QuestHint = "needs level " + q.LevelRequire;
                if (me.Record.ClassId == 0 && task >= 9 && !me.Progress.TaskActive)
                    s.QuestHint = me.Level >= 10 ? "join a school next (" + ClassNames[Math.Max(1, Math.Min(6, p.ClassId))] + ")" : "join a school at level 10";
            }
            int tryId; long tryPrice;   // item ledger: the cheapest village-shop item this character never used
            if (AgentTactics.PickTryItem(a, me, world, nowMs, out tryId, out tryPrice))
            {
                var tt = world.FindItem(tryId);
                s.TryItemId = tryId; s.TryItemText = (tt != null ? tt.Name : "item " + tryId) + " (" + (tryPrice == 0 ? "already in the bag" : tryPrice + " yen/xu") + ")";
            }
            s.Memory.AddRange(a.Memory);
            foreach (var f in a.Knowledge.Top(15)) s.Knowledge.Add(f.Text + (f.Count > 1 ? " (x" + f.Count + ")" : ""));
            foreach (var f in a.Knowledge.NpcMaps())
                if (f.Key != AgentKnowledge.NpcPrefix + me.MapId && s.NpcMemory.Count < AgentConfig.NpcMapsInPrompt) s.NpcMemory.Add(f.Text);
            long now = Now();
            foreach (var it in me.Zone.ItemMaps)
                if (AgentForaging.CanTake(me, world, it, now)) s.ItemsNearby++;
            s.Neighbors.AddRange(WorldKnowledge.Neighbors(world, map, task));
            var near = WorldKnowledge.Nearby(world, map, Math.Max(0, AgentConfig.OptionHops), task);
            foreach (var kv in near)
            {
                var tile = world.FindMap(kv.Key);
                if (tile == null) continue;
                s.MapNames[kv.Key] = tile.Name;
                foreach (var m in WorldKnowledge.MobsOf(world, kv.Key))
                {
                    if (m.IsBoss || m.TemplateId == 0 || m.Level > me.Level + 8 || m.Level < me.Level - 12) continue;
                    s.Hunts.Add(new HuntOption { MapId = kv.Key, Hops = kv.Value, MobId = m.TemplateId, MobLevel = m.Level, MapName = tile.Name, MobName = m.Name });
                }
            }
            s.Activity = a.Activity;
            try { s.MapView = AgentMapView.Build(a, me, world); s.Terrain = AgentMapView.BuildTerrain(a, me, world); }
            catch (Exception ex) { s.MapView = "unavailable: " + ex.Message + "\n"; }
            s.Account = a.Account; s.Summary = a.Summary;
            int lv = me.Level;
            s.Hunts.Sort((x, y) => (Math.Abs(x.MobLevel - lv - 1) * 2 + x.Hops).CompareTo(Math.Abs(y.MobLevel - lv - 1) * 2 + y.Hops));
            return s;
        }

        /// <summary>The LLM's latest priorities while they last (AgentConfig.LlmPriorityMinutes), else null (all 1).</summary>
        public static float[] CurrentPriorities(NinjaAgent a, long now)
        {
            if (a.Priorities == null || AgentConfig.LlmPriorityMinutes <= 0 || now - a.PrioritiesAt > AgentConfig.LlmPriorityMinutes * 60000L) return null;
            return (float[])a.Priorities.Clone();
        }

        private static bool IsVillage(int map) { return map == 10 || map == 17 || map == 22 || map == 32 || map == 38 || map == 43 || map == 48 || map == 1 || map == 27 || map == 72; }

        /// <summary>Validates a decision against what the character can do and installs it as the plan. Locked or
        /// unreachable maps and monsters that do not live there are corrected or dropped (noted on the step).</summary>
        private static bool Apply(NinjaAgent a, Decision d, long now, string fallbackWhy)
        {
            var me = a.Me;
            if (me == null) return false;
            int task = me.Record.TaskId;
            var v = new Decision { Reason = d.Reason ?? "", Say = d.Say ?? "", Source = d.Source, Raw = d.Raw,
                FleeHpPercent = d.FleeHpPercent < 0 ? -1 : Math.Max(10, Math.Min(60, d.FleeHpPercent)) };
            // The LLM re-weights the rule planner on every answer (dynamic, by context); the rules use the weights for
            // the decisions they make while the LLM is busy.
            if (d.Priorities != null && d.Priorities.Length == Priority.Names.Length && AgentConfig.LlmPriorityMinutes > 0)
            {
                var w = new float[Priority.Names.Length];
                for (int i = 0; i < w.Length; i++) w[i] = float.IsNaN(d.Priorities[i]) ? 1f : Math.Max(0f, Math.Min(Priority.Max, d.Priorities[i]));
                bool changed = a.Priorities == null;
                for (int i = 0; i < w.Length && !changed; i++) changed = Math.Abs(a.Priorities[i] - w[i]) > 0.01f;
                a.Priorities = w; a.PrioritiesAt = now;
                if (changed) PlayerLog.Bot(a.Account, "AI", "priorities: " + Priority.Text(w));
            }
            int map = me.MapId;   // where each step starts
            foreach (var st in d.Steps)
            {
                if (v.Steps.Count >= 4) break;
                var s = new PlanStep { Goal = st.Goal, MapId = st.MapId, MobId = st.MobId, Count = st.Count, Seconds = st.Seconds, X = st.X, Y = st.Y };
                if (s.Goal == GoalType.Move && s.X < 0) continue;   // goto without coordinates
                if ((s.Goal == GoalType.Hunt || s.Goal == GoalType.Explore || s.Goal == GoalType.Travel || s.Goal == GoalType.Move) && s.MapId >= 0
                    && (!WorldKnowledge.IsWalkable(world, s.MapId) || WorldKnowledge.Route(world, map, s.MapId, task) == null))
                {
                    s.Note = MapUnlock.CanEnter(task, s.MapId) ? "map " + s.MapId + " is not reachable" : "map " + s.MapId + " is locked (main quest)";
                    if (s.Goal == GoalType.Travel || s.Goal == GoalType.Move) continue;
                    s.MapId = -1;
                }
                if (s.Goal == GoalType.Hunt)
                {
                    int where = s.MapId >= 0 ? s.MapId : map;
                    if (s.MobId >= 0 && !WorldKnowledge.MobsOf(world, where).Exists(k => k.TemplateId == s.MobId))
                    { s.Note = "monster " + s.MobId + " does not live in map " + where; s.MobId = -1; }
                    s.Count = Math.Max(1, Math.Min(30, s.Count));
                }
                // shop with an item id = buy one and try it (AgentTactics.TryItem): only items of the village shops
                if (s.Goal == GoalType.Shop && s.MobId >= 0 && AgentTactics.SoldBy(world, s.MobId) < 0)
                { s.Note = "item " + s.MobId + " is not sold in the village shops"; s.MobId = -1; }
                if (s.Goal == GoalType.Collect) s.Count = Math.Max(1, Math.Min(10, s.Count));
                if (s.Goal == GoalType.Idle) s.Seconds = Math.Max(5, Math.Min(120, s.Seconds));
                if (s.MapId >= 0) map = s.MapId;
                v.Steps.Add(s);
            }
            if (v.Steps.Count == 0) return false;
            a.Plan = v;
            a.StepIndex = 0;
            a.PlanStartedAt = now;
            BeginStep(a, now);
            var line = new StringBuilder();
            line.Append(DateTime.Now.ToString("HH:mm:ss")).Append(" [").Append(v.Source).Append("] ").Append(v.Reason);
            if (fallbackWhy != null) line.Append(" (fallback: ").Append(fallbackWhy).Append(')');
            line.Append(" -> ");
            for (int i = 0; i < v.Steps.Count; i++) line.Append(i > 0 ? "; " : "").Append(v.Steps[i]).Append(v.Steps[i].Note.Length > 0 ? " [" + v.Steps[i].Note + "]" : "");
            a.Decisions.Add(line.ToString());
            while (a.Decisions.Count > 10) a.Decisions.RemoveAt(0);
            a.Remember("decided: " + v.Reason);
            PlayerLog.Bot(a.Account, "AI", line.ToString());
            if (!string.IsNullOrEmpty(v.Say)) Say(a, v.Say);
            return true;
        }

        private static void BeginStep(NinjaAgent a, long now)
        {
            a.StepStartedAt = now;
            a.LastProgressAt = now;
            a.StepProgress = 0;
            a.TargetMob = -1;
            a.TargetItem = -1;
            a.LastTargetSeen = 0;
            a.Route = null;
            a.WanderX = -1;
            if (a.Me != null) a.LastTaskId = a.Me.Record.TaskId;
        }

        /// <summary>Tactical -> strategic: the current step is complete; the plan moves on or is done.</summary>
        public static void StepDone(NinjaAgent a, GameWorld w, long now)
        {
            if (a.Plan == null) return;
            a.StepIndex++;
            BeginStep(a, now);
            if (a.StepIndex >= a.Plan.Steps.Count)
            {
                string reason = a.Plan.Reason;
                a.Plan = null;
                Report(a, AgentEvent.PlanDone, "completed the plan: " + reason);
            }
        }

        /// <summary>Tactical -> strategic: the current step cannot be done; the plan is dropped and a new decision is due.</summary>
        public static void StepFailed(NinjaAgent a, GameWorld w, long now, string why)
        {
            a.Plan = null;
            BeginStep(a, now);
            Report(a, AgentEvent.StepFailed, "failed: " + why);
        }

        /// <summary>An event from the tactical layer: remembered, logged, and for important ones a new decision is due.</summary>
        public static void Report(NinjaAgent a, AgentEvent e, string text)
        {
            Remember(a, text);
            switch (e)
            {
                case AgentEvent.PlanDone:
                case AgentEvent.StepFailed:
                case AgentEvent.Respawned:
                case AgentEvent.LevelUp:
                case AgentEvent.Spawned:
                case AgentEvent.PlayerNearby:
                case AgentEvent.Quest:
                    a.NeedsDecision = true;
                    a.Trigger = text;
                    break;
                case AgentEvent.Died:
                    a.Plan = null;
                    a.Trigger = text;
                    break;
            }
        }

        /// <summary>Remembers an event (short memory) and writes it to the bot log.</summary>
        public static void Remember(NinjaAgent a, string text)
        {
            a.Remember(text);
            PlayerLog.Bot(a.Account, "EVT", text);
        }

        // ================================================================== what happened (knowledge)

        /// <summary>The character died: remember who did it (the monster that was fighting it) and where.</summary>
        public static void OnDied(NinjaAgent a, GameWorld w)
        {
            var me = a.Me;
            a.Deaths++;
            a.Recovering = false;
            Mob killer = null;
            foreach (var mob in me.Zone.Mobs)
                if (!mob.IsDead && mob.Chars.Contains(me.Record.Id) && (killer == null || Math.Abs(mob.X - me.X) < Math.Abs(killer.X - me.X))) killer = mob;
            string who = killer != null ? killer.Template.Name + " (lv " + killer.Level + ")" : "monsters";
            Report(a, AgentEvent.Died, "was killed by " + who + " on map " + me.MapId + " " + me.Zone.Tile.Name + " at level " + me.Level +
                   " (HP potions left: " + AgentTactics.HpPotions(me, w) + ")");
            if (killer != null)
                a.Knowledge.Learn("danger:" + killer.Template.Id, killer.Template.Name + " lv " + killer.Level + " on map " + me.MapId + " " + me.Zone.Tile.Name +
                                  " killed me (last time at my level " + me.Level + ")");
            if (a.P.Sociability >= 0.6f) Say(a, "Ui da...");
        }

        /// <summary>A monster the character was fighting died: hunting-ground statistics.</summary>
        public static void OnKill(NinjaAgent a, GameWorld w, Mob mob)
        {
            var me = a.Me;
            string key = "ground:" + me.MapId + ":" + mob.Template.Id;
            var f = a.Knowledge.Get(key);
            int n = f != null ? f.Count + 1 : 1;
            a.Knowledge.Learn(key, "hunted " + mob.Template.Name + " lv " + mob.Level + " on map " + me.MapId + " " + me.Zone.Tile.Name + ": " + n + " kills, last at my level " + me.Level);
            if (n % 10 == 0) Report(a, AgentEvent.Kill, "defeated " + n + " " + mob.Template.Name + " on map " + me.MapId + " so far");
        }

        /// <summary>The character picked an item up: remember finds that are not money.</summary>
        public static void OnPicked(NinjaAgent a, GameWorld w, ItemTemplateData t, int quantity)
        {
            if (t.Type == PlayerItems.TypeMoney) { if (quantity >= 200) Remember(a, "picked up " + quantity + " yen"); return; }
            Remember(a, "picked up " + t.Name + (quantity > 1 ? " x" + quantity : ""));
            a.Knowledge.Learn("item:" + t.Id, "found " + t.Name + " (level " + t.Level + ") on map " + a.Me.MapId);
        }

        /// <summary>A quest menu click changed the quest state (taken, a talk step done, reported).</summary>
        public static void OnQuestProgress(NinjaAgent a, GameWorld w, string entry, string npc)
        {
            var me = a.Me;
            int task = me.Record.TaskId;
            if (task != a.LastTaskId)
            {
                var done = w.FindTask(a.LastTaskId);
                a.QuestsDone++;
                a.Knowledge.Learn("quest:" + a.LastTaskId, "finished quest " + (done != null ? done.Name : a.LastTaskId.ToString()) + " at level " + me.Level);
                var unlocked = new List<int>(MapUnlock.Allowed(task));
                unlocked.RemoveAll(m => MapUnlock.Allowed(a.LastTaskId).Contains(m));
                string maps = "";
                foreach (int m in unlocked) { var tile = w.FindMap(m); maps += (maps.Length > 0 ? ", " : "") + m + (tile != null ? " " + tile.Name : ""); }
                if (maps.Length > 0) a.Knowledge.Learn("note:unlocked" + task, "quest " + a.LastTaskId + " unlocked maps " + maps);
                Report(a, AgentEvent.Quest, "finished quest " + (done != null ? done.Name : "") + " with " + npc + (maps.Length > 0 ? "; new maps: " + maps : ""));
                a.LastTaskId = task;
                if (a.P.Sociability >= 0.5f) Say(a, "Xong nhiệm vụ rồi!");
            }
            else Remember(a, "quest: \"" + entry + "\" with " + npc);
        }

        // ================================================================== speech

        /// <summary>MapService.chat (CHAT_MAP): a speech bubble over the character for the players of its zone.</summary>
        public static void Say(NinjaAgent a, string text)
        {
            var me = a.Me;
            if (me == null || string.IsNullOrEmpty(text)) return;
            Presence.Say(me, text);
            PlayerLog.Bot(a.Account, "SAY", text);
        }

        // ================================================================== persistence (the AI's mind; the characters are saved as players)

        private static Dictionary<string, Dictionary<string, object>> Load()
        {
            var map = new Dictionary<string, Dictionary<string, object>>();
            try
            {
                string text = server.Accounts.GetDocument(SaveKey);
                if (text == null) return map;
                var list = Json.Parse(text) as List<object>;
                if (list == null) return map;
                foreach (var o in list)
                {
                    var d = o as Dictionary<string, object>;
                    object name;
                    if (d != null && d.TryGetValue("name", out name) && name is string) map[(string)name] = d;
                }
            }
            catch (Exception ex) { Log("AI memory load error (fresh minds): " + ex.Message); }
            return map;
        }

        private static void Restore(NinjaAgent a, Dictionary<string, object> d)
        {
            object o;
            if (d.TryGetValue("kills", out o)) a.Kills = (int)Json.Long(o);
            if (d.TryGetValue("deaths", out o)) a.Deaths = (int)Json.Long(o);
            if (d.TryGetValue("quests", out o)) a.QuestsDone = (int)Json.Long(o);
            if (d.TryGetValue("memory", out o) && o is List<object> mem)
                foreach (var m in mem) if (m is string) a.Memory.Add((string)m);
            if (d.TryGetValue("knowledge", out o)) a.Knowledge.FromJson(o);
            if (d.TryGetValue("summary", out o) && o is string) a.Summary = (string)o;
            if (d.TryGetValue("rewardHistory", out o)) a.Learning.ItemRewards.Restore(o);
        }

        private static void Save()
        {
            if (server == null) return;
            var w = new JsonWriter();
            w.BeginArray();
            foreach (var a in Agents)
            {
                w.BeginObject();
                w.Prop("name", a.Name);
                w.Prop("kills", a.Kills);
                w.Prop("deaths", a.Deaths);
                w.Prop("quests", a.QuestsDone);
                w.Key("memory"); w.Value(new List<string>(a.Memory));
                w.Key("knowledge"); w.Value(a.Knowledge.ToJson());
                w.Prop("summary", a.Summary);
                w.Key("rewardHistory"); w.Value(a.Learning.ItemRewards.ToObject());
                w.EndObject();
            }
            w.EndArray();
            server.Accounts.SetDocument(SaveKey, w.ToString());
            Rl.RlRuntime.SaveHumanRewardHistories();
        }

        // ================================================================== debugging

        private static void Log(string text) { if (server != null) server.Log("[ai] " + text); }

        /// <summary>Forces a new decision now (dev terminal /replan of the human player). Null or "" = every AI player.</summary>
        public static int ForceReplan(string name)
        {
            int n = 0;
            if (world == null) return 0;
            foreach (var a in Agents)
            {
                if (!string.IsNullOrEmpty(name) && !a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                if (a.Me == null || a.Me.IsDead || a.Pending) continue;
                a.Trigger = "asked to reconsider the plan";
                a.LastDecisionAt = 0;
                a.NeedsDecision = true;
                n++;
            }
            return n;
        }

        /// <summary>One line per AI player and one for the LLM (dev terminal /agents).</summary>
        public static List<string> Summary()
        {
            var lines = new List<string>();
            if (world == null) { lines.Add("AI players are off (NSO Server Control > AI players)."); return lines; }
            foreach (var a in Agents)
            {
                var me = a.Me;
                if (me == null) { lines.Add(a.Name + ": " + a.LoginStatus); continue; }
                lines.Add(a.Name + " lv" + me.Level + " map " + me.MapId + " quest " + me.Record.TaskId + " HP " + AgentTactics.HpPercent(me) + "% - " + a.Activity +
                          (a.Plan != null ? " | [" + a.Plan.Source + "] " + (a.Step != null ? a.Step.ToString() : "") : "") + (a.Pending ? " | waiting LLM" : ""));
            }
            lines.Add(LlmStatus(Now()));
            return lines;
        }

        private static string LlmStatus(long now)
        {
            if (service == null) return "LLM: off (rules only)";
            return "LLM " + AgentConfig.LlmModel + ": " + (service.IsAvailable(now) ? "ok" : "backing off") + ", " + service.Successes + "/" + service.Requests +
                   " answered, last " + service.LastLatencyMs + " ms" + (service.LastError.Length > 0 ? ", last error " + service.LastError : "");
        }

        /// <summary>Full debug state (wiki route /control/agents). Call under SyncRoot.</summary>
        public static Dictionary<string, object> DebugObject()
        {
            long now = Now();
            var root = new Dictionary<string, object>();
            root["running"] = world != null;
            root["learning"] = Rl.RlRuntime.Debug();
            var llm = new Dictionary<string, object>();
            llm["enabled"] = AgentConfig.LlmEnabled;
            llm["endpoint"] = AgentConfig.LlmEndpoint;
            llm["model"] = AgentConfig.LlmModel;
            llm["status"] = LlmStatus(now);
            if (service != null)
            {
                llm["available"] = service.IsAvailable(now);
                llm["busy"] = service.Busy;
                llm["queue"] = service.QueueLength;
                llm["requests"] = service.Requests;
                llm["successes"] = service.Successes;
                llm["failures"] = service.Failures;
                llm["lastLatencyMs"] = service.LastLatencyMs;
                llm["lastError"] = service.LastError;
                llm["lastAnswer"] = service.LastAnswer;
                llm["lastFailedAnswer"] = service.LastFailedAnswer;
                llm["lastFailedAt"] = service.LastFailedAt;
            }
            root["llm"] = llm;
            // long-term memory: the whole game knowledge (GameKnowledge) plus what the AI players learn while playing
            root["rag"] = new Dictionary<string, object> { { "enabled", AgentConfig.RagEnabled }, { "available", AgentRag.Available },
                { "stored", System.Threading.Interlocked.Read(ref AgentRag.Stored) }, { "recalls", System.Threading.Interlocked.Read(ref AgentRag.Recalls) },
                { "knowledge", GameKnowledge.Documents }, { "knowledgeError", GameKnowledge.LastError }, { "error", AgentRag.LastError } };
            var list = new List<object>();
            foreach (var a in Agents)
            {
                var d = new Dictionary<string, object>();
                var me = a.Me;
                d["id"] = a.Index; d["name"] = a.Name; d["account"] = a.Account; d["traits"] = a.P.Traits(); d["bio"] = a.P.Bio;
                d["online"] = me != null; d["loginStatus"] = a.LoginStatus;
                d["kills"] = a.Kills; d["deaths"] = a.Deaths; d["questsDone"] = a.QuestsDone;
                d["learning"] = new Dictionary<string, object> { { "source", a.Learning.LastSource }, { "fallback", a.Learning.Fallback },
                    { "version", a.PolicyVersion }, { "reward", a.Learning.Reward }, { "stuck", a.Learning.Stuck }, { "actions", new Dictionary<string, int>(a.Learning.Actions) } };
                d["actionRewards"] = a.Learning.ItemRewards.Debug();
                var ledger = new List<object>();   // item ledger: what this character learned each item does
                foreach (var kv in a.Learning.ItemRewards.Ledger)
                {
                    var t = server != null && server.World != null ? server.World.FindItem(kv.Key) : null;
                    ledger.Add(kv.Value.Text(t != null ? t.Name : "item " + kv.Key));
                }
                d["itemLedger"] = ledger;
                d["activity"] = a.Activity; d["recovering"] = a.Recovering; d["pending"] = a.Pending;
                d["needsDecision"] = a.NeedsDecision; d["trigger"] = a.Trigger;
                d["nextReviewInS"] = Math.Max(0, (a.NextReviewAt - now) / 1000);
                if (me != null)
                {
                    long expLevel = world.ExpForLevel(me.Level), expNext = world.ExpForLevel(me.Level + 1);
                    d["charId"] = me.Record.Id;
                    d["level"] = me.Level; d["exp"] = me.Record.Exp;
                    d["expPercent"] = expNext > expLevel ? Math.Round(Math.Max(0, Math.Min(100, (me.Record.Exp - expLevel) * 100.0 / (expNext - expLevel))), 1) : 100.0;
                    d["class"] = me.Record.ClassId; d["className"] = me.Record.ClassId >= 0 && me.Record.ClassId < ClassNames.Length ? ClassNames[me.Record.ClassId] : "";
                    d["gender"] = me.Record.Gender;
                    d["hp"] = me.Hp; d["maxHp"] = me.MaxHp; d["mp"] = me.Mp; d["maxMp"] = me.MaxMp;
                    d["yen"] = me.Progress.Yen; d["xu"] = me.Progress.Xu;
                    d["dead"] = me.IsDead;
                    d["map"] = me.MapId; d["mapName"] = me.Zone.Tile.Name; d["zone"] = me.Zone.Id; d["x"] = (int)me.X; d["y"] = (int)me.Y;
                    d["withHumans"] = me.Zone.Players.Exists(o => { var p = (PlayerState)o; return p.Session != null && !p.Session.IsBot; });
                    d["hpPotions"] = AgentTactics.HpPotions(me, world); d["mpPotions"] = AgentTactics.MpPotions(me, world); d["foods"] = AgentTactics.Foods(me, world);
                    // consumables: the last one used and the effects running now (potion regeneration, food...)
                    if (a.Client != null && a.Client.LastUsedItemId >= 0)
                        d["lastItem"] = new Dictionary<string, object> { { "id", a.Client.LastUsedItemId }, { "name", a.Client.LastUsedItemName },
                            { "secondsAgo", Math.Max(0, (now - a.Client.LastUsedItemAt) / 1000) }, { "count", a.Client.UsedItems } };
                    var effects = new List<object>();
                    foreach (var e in me.Effects)
                        if (!e.IsExpired(now))
                            effects.Add(new Dictionary<string, object> { { "id", e.TemplateId }, { "name", world.EffectName(e.TemplateId) },
                                { "type", e.Type }, { "seconds", Math.Max(0, (e.EndAt - now + 999) / 1000) } });
                    d["effects"] = effects;
                    d["freeSlots"] = PlayerItems.SlotNull(me); d["bagSize"] = me.Inventory.Bag.Length;
                    var bag = new List<object>();
                    foreach (var it in me.Inventory.Bag)
                    {
                        if (it == null) continue;
                        var t = world.FindItem(it.Id);
                        bag.Add((t != null ? t.Name : "#" + it.Id) + (it.Quantity > 1 ? " x" + it.Quantity : ""));
                    }
                    d["bag"] = bag;
                    var eq = new List<object>();
                    foreach (var it in me.Inventory.Equipment)
                    {
                        if (it == null) continue;
                        var t = world.FindItem(it.Id);
                        eq.Add((t != null ? t.Name : "#" + it.Id) + (it.Upgrade > 0 ? " +" + it.Upgrade : ""));
                    }
                    d["equipment"] = eq;
                    var q = world.FindTask(me.Record.TaskId);
                    var quest = new Dictionary<string, object>();
                    quest["id"] = me.Record.TaskId; quest["name"] = q != null ? q.Name : "(all done)";
                    quest["active"] = me.Progress.TaskActive; quest["index"] = me.Progress.TaskIndex;
                    quest["steps"] = q != null ? q.SubNames.Length : 0;
                    quest["step"] = q != null && me.Progress.TaskActive && me.Progress.TaskIndex < q.SubNames.Length ? q.SubNames[me.Progress.TaskIndex] : "";
                    quest["count"] = me.Progress.TaskCount;
                    quest["target"] = q != null && me.Progress.TaskActive && me.Progress.TaskIndex < q.Counts.Length ? (int)q.Counts[me.Progress.TaskIndex] : -1;
                    quest["unlockedMaps"] = MapUnlock.Allowed(me.Record.TaskId).Count;
                    d["quest"] = quest;
                }
                if (a.Plan != null)
                {
                    var p = new Dictionary<string, object>();
                    p["source"] = a.Plan.Source; p["reason"] = a.Plan.Reason; p["say"] = a.Plan.Say; p["fleeHp"] = a.FleeHp;
                    var steps = new List<object>();
                    foreach (var s in a.Plan.Steps) steps.Add(s + (s.Note.Length > 0 ? " [" + s.Note + "]" : ""));
                    p["steps"] = steps; p["stepIndex"] = a.StepIndex; p["stepProgress"] = a.StepProgress;
                    p["stepSeconds"] = (now - a.StepStartedAt) / 1000;
                    d["plan"] = p;
                }
                var pw = CurrentPriorities(a, now);
                if (pw != null)
                {
                    var pr = new Dictionary<string, object>();
                    for (int i = 0; i < pw.Length; i++) pr[Priority.Names[i]] = Math.Round(pw[i], 2);
                    d["priorities"] = pr;
                    d["prioritiesAgeS"] = (now - a.PrioritiesAt) / 1000;
                }
                d["memory"] = new List<object>(a.Memory.ToArray());
                var know = new List<object>();
                foreach (var f in a.Knowledge.All())
                    know.Add(new Dictionary<string, object> { { "key", f.Key }, { "text", f.Text }, { "count", f.Count } });
                d["knowledge"] = know;
                d["decisions"] = new List<object>(a.Decisions.ToArray());
                list.Add(d);
            }
            root["agents"] = list;
            return root;
        }
    }
}
