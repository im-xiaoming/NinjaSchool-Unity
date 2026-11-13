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
    /// - Strategic AI decides WHAT and WHY: the Python LLM when LlmLeads is on, otherwise evaluated RL/rules.
    ///   Rules/RL keep playing while Brain is unavailable; requests are event-driven and throttled, never per frame.
    /// - Tactical AI decides HOW: AgentTactics shares its skills with RL primitive selection every 500 ms.
    /// - Memory: recent events (Memory) and long-term gameplay knowledge (AgentKnowledge), saved in the save database
    ///   (document "ai_players") and given to the LLM with the character's bag, equipment, experience and quest.
    /// - Their actions, screen messages, decisions and states go to the bot log (PlayerLog.Bot, kept 20 days).
    /// They cannot use the dev terminal (Controller ignores CHAT_MAP commands from bot sessions).
    /// Gameplay and client movement frames share GameWorld.SyncRoot. Brain/RAG/network workers use detached data.
    /// </summary>
    public static partial class AgentManager
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
        public static bool Running
        {
            get
            {
                return world != null;
            }
        }

        public static StrategicService Service
        {
            get
            {
                return service;
            }
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // Courtesy reservations affect decisions only; the server never forbids another character from picking/hitting.
        internal static bool LootClaimed(NinjaAgent requester, short itemId)
        {
            foreach (var a in Agents)
                if (a != requester && a.Me != null && a.Me.Zone == requester.Me.Zone && !a.Me.IsDead && a.TargetItem == itemId && a.LootTarget != null && Now() - a.LootProgressAt <= System.Math.Max(1, AgentConfig.LootStallSeconds) * 1000L)
                    return true;
            return false;
        }

        internal static bool MobClaimed(NinjaAgent requester, int mobId)
        {
            foreach (var a in Agents)
                if (a != requester && a.Me != null && a.Me.Zone == requester.Me.Zone && !a.Me.IsDead && !a.Recovering && a.ZoneTarget < 0 && a.TargetMob == mobId && a.Step != null)
                    return true;
            return false;
        }

        internal static int ProjectedZonePopulation(int mapId, Zone zone, long now)
        {
            int count = zone.NumberChar;
            foreach (var a in Agents)
            {
                if (a.Me == null || a.Me.MapId != mapId || a.ZoneMap != mapId || a.ZoneTarget < 0 || now - a.ZoneIntentAt > 1500)
                    continue;
                if (a.Me.Zone == zone)
                    count--;
                if (a.ZoneTarget == zone.Id)
                    count++;
            }

            return Math.Max(0, count);
        }

        // ================================================================== lifecycle
        /// <summary>Prepares the AI players (they log in one after another from the tick) and starts the LLM worker.
        /// Called by ServerBootstrap after the server started; never by the dotnet tests.</summary>
        public static void Start(GameServer s)
        {
            if (!AgentConfig.Enabled || s == null || s.World == null)
                return;
            lock (s.World.SyncRoot)
            {
                server = s;
                world = s.World;
                Agents.Clear();
                Rl.RlRuntime.Start(s); // offline learning owns IO/trainer lifecycle
                var saved = Load();
                AgentRag.Start(s.World); // long-term memory in Qdrant (queues only; the worker embeds in the background)
                int count = Math.Max(0, Math.Min(AgentConfig.AgentCount, AgentPersonality.Presets.Length));
                long now = Now();
                for (int i = 0; i < count; i++)
                {
                    var a = new NinjaAgent
                    {
                        Index = i,
                        P = AgentPersonality.Presets[i]
                    };
                    Dictionary<string, object> rec;
                    a.Knowledge.Owner = a.Account;
                    if (saved.TryGetValue(a.Name, out rec))
                        Restore(a, rec);
                    a.Knowledge.MirrorAll();
                    a.LoginAt = now + 1000L + i * Math.Max(0, AgentConfig.LoginSpacingSeconds) * 1000L;
                    a.LastDecisionAt = now - AgentConfig.MinDecisionSeconds * 1000L + i * 2000L; // stagger the first requests
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
            if (world == null)
                return;
            lock (world.SyncRoot)
            {
                try
                {
                    Save();
                }
                catch (Exception ex)
                {
                    Log("AI save error: " + ex.Message);
                }

                foreach (var a in Agents)
                    Rl.RlRuntime.FlushAgent(a, Now());
                Rl.RlRuntime.Stop();
                foreach (var a in Agents)
                    if (a.Session != null)
                    {
                        try
                        {
                            a.Session.Close();
                        }
                        catch (Exception)
                        {
                        }
                    }

                if (service != null)
                    service.Stop();
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
            if (world == null || w != world)
                return;
            foreach (var a in Agents)
                if (a.Session != null && a.Session.Connected && a.Client != null)
                    a.Client.StepMovement(now);
        }

        /// <summary>Server tick (GameServer.TickLoop, inside SyncRoot): log-ins, decisions in, tactics, decisions out.</summary>
        public static void Tick(GameWorld w, long now)
        {
            if (world == null || w != world)
                return;
            DrainResults(now);
            Rl.RlRuntime.Tick(w, now);
            foreach (var a in Agents)
            {
                if (!LoggedIn(a, now))
                    continue;
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
                    a.Plan = null;
                    a.NeedsDecision = true;
                }

                MaybeDecide(a, now);
            }

            Rl.RlRuntime.EndTick();
            if (AgentConfig.SaveSeconds > 0 && now - lastSave > AgentConfig.SaveSeconds * 1000L)
            {
                lastSave = now;
                try
                {
                    Save();
                }
                catch (Exception ex)
                {
                    Log("AI save error: " + ex.Message);
                }
            }
        }

        // the log-in a player does: client info, data ok, create the character the first time, select it
        private static bool LoggedIn(NinjaAgent a, long now)
        {
            if (a.Me != null)
                return true;
            if (now < a.LoginAt)
                return false;
            if (a.Session != null && !a.Session.Connected)
                a.Session = null;
            try
            {
                if (a.Session == null)
                {
                    a.Session = server.ConnectBot(a.Account);
                    a.Client = new BotClient(a.Session)
                    {
                        Agent = a
                    };
                    a.Client.ClientInfo();
                    a.Client.ClientOk();
                }

                a.Client.CreateChar(a.Name, a.P.Gender, a.P.Head); // Controller refuses it when the character exists
                a.Client.SelectChar(a.Name);
            }
            catch (Exception ex)
            {
                a.LoginStatus = "error: " + ex.Message;
            }

            var me = a.Me;
            if (me == null)
            {
                a.LoginStatus = "could not enter the game" + (a.Session != null && a.Session.LastNotice.Length > 0 ? ": " + a.Session.LastNotice : " (character name taken by another account?)");
                if (a.Session != null)
                {
                    try
                    {
                        a.Session.Close();
                    }
                    catch (Exception)
                    {
                    }
                }

                a.Session = null;
                a.LoginAt = now + 60000; // try again in a minute
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
                if (tile.Npcs.Count == 0)
                    line.Append("no NPCs");
                for (int i = 0; i < tile.Npcs.Count; i++)
                {
                    var n = tile.Npcs[i];
                    line.Append(i > 0 ? "; " : "").Append(AgentTactics.NpcName(world, n.TemplateId)).Append(" (id ").Append(n.TemplateId).Append(") at (").Append(n.X).Append(',').Append(n.Y).Append(')');
                }

                a.Knowledge.Learn(AgentKnowledge.NpcPrefix + me.MapId, line.ToString());
            }

            if (me.Level > a.LastLevel)
            {
                a.LastLevel = me.Level;
                Report(a, AgentEvent.LevelUp, "reached level " + me.Level);
                if (a.P.Sociability >= 0.5f)
                    Say(a, "Lên cấp " + me.Level + " rồi!");
            }

            string notice = a.Session.LastNotice;
            if (notice.Length > 0 && notice != a.LastNoticeSeen)
            {
                a.LastNoticeSeen = notice;
                if (notice.Length < 140)
                    a.Remember("saw: " + notice);
            }

            // a human player came into the zone: sociable characters greet
            foreach (PlayerState p in me.Zone.Players)
            {
                if (p == me || p.Session == null || p.Session.IsBot || a.HumansSeen.Contains(p.Record.Id))
                    continue;
                a.HumansSeen.Add(p.Record.Id);
                if (Rng.Next(100) < (int)(a.P.Sociability * AgentConfig.GreetPercent) && a.P.Greetings.Length > 0)
                    Say(a, a.P.Greetings[Rng.Next(a.P.Greetings.Length)]);
                a.Remember("met player " + p.Record.Name + " on map " + me.MapId);
                a.Knowledge.Learn("player:" + p.Record.Id, "met player " + p.Record.Name + " (level " + p.Level + ")");
            }

            if (a.HumansSeen.Count > 0)
                a.HumansSeen.RemoveWhere(id => !me.Zone.Players.Exists(o => ((PlayerState)o).Record.Id == id));
        }

        // ================================================================== strategic <-> tactical
        private static readonly string[] ClassNames =
        {
            "no school yet",
            "Ninja kiếm",
            "Ninja phi tiêu",
            "Ninja kunai",
            "Ninja cung",
            "Ninja đao",
            "Ninja quạt"
        };
        // ================================================================== debugging
        private static void Log(string text)
        {
            if (server != null)
                server.Log("[ai] " + text);
        }
    }
}
