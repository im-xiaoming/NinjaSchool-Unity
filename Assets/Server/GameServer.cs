using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NsoServer.Net;
using NsoServer.Store;

namespace NsoServer
{
    public sealed class ServerConfig
    {
        public int ServerId = 1;
        public string DataDirectory = "NsoData";
        /// <summary>Save to the SQLite database nso.db (false: the former users.tsv / players.tsv files).</summary>
        public bool UseSqlite = true;
    }

    // In-process replacement for the Java server. The client connects through an in-memory pipe (Connect), no socket or port.
    public sealed class GameServer
    {
        public readonly ServerConfig Config;
        public readonly AccountStore Accounts;
        public IStaticData Static = new EmptyStaticData();
        public NsoServer.World.GameWorld World;   // maps, zones, monsters; null until static data is loaded
        public Action<string> Logger = delegate { };

        private int nextId;
        private readonly object sync = new object();
        private readonly List<Session> sessions = new List<Session>();
        private volatile bool running;

        public GameServer(ServerConfig config)
        {
            Config = config;
            Accounts = new AccountStore(config.DataDirectory, config.UseSqlite);
        }

        public string RulesPath { get { return Path.Combine(Config.DataDirectory, "game-rules.json"); } }

        public void Start()
        {
            // Saved offline settings work in release players too; no debug web server is required to read them.
            if (World != null)
            {
                try { World.LootRules = NsoServer.World.LootRules.Load(RulesPath); }
                catch (Exception ex) { Log("rules load error (defaults retained): " + ex.Message); }
                try { World.CombatRules = NsoServer.World.CombatRules.Load(Path.Combine(Config.DataDirectory, "combat-rules.json")); }
                catch (Exception ex) { Log("combat rules load error (defaults retained): " + ex.Message); }
            }
            if (World != null)
            {
                try { World.MobTuning = NsoServer.World.MobTuning.Load(System.IO.Path.Combine(Config.DataDirectory, "monster-tuning.json"), World); }
                catch (Exception ex) { Log("monster tuning load error (defaults retained): " + ex.Message); }
            }
            if (World != null)
            {
                try { World.BalanceRules = NsoServer.World.GameBalanceRules.Load(System.IO.Path.Combine(Config.DataDirectory, "game-balance.json"), World); }
                catch (Exception ex) { Log("balance load error (defaults retained): " + ex.Message); }
            }
            running = true;
            Log("save storage: " + Accounts.Backend + " in " + Config.DataDirectory + (Accounts.BackendError != null ? " (SQLite error: " + Accounts.BackendError + ")" : ""));
            new Thread(TickLoop) { IsBackground = true, Name = "nso-tick" }.Start();
            new Thread(ClientMovementLoop) { IsBackground = true, Name = "nso-client-movement" }.Start();
            Log("Server started");
        }

        // Offline headless clients need the same small collision/jump steps as a graphical client.
        // Keep Zone.run, AI decisions, cooldowns and effects on their existing 500 ms clock.
        private void ClientMovementLoop()
        {
            while (running)
            {
                try
                {
                    var w = World;
                    if (w != null) lock (w.SyncRoot)
                        NsoServer.Ai.AgentManager.TickMovement(w, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
                catch (Exception ex) { Log("client movement error: " + ex.Message); }
                Thread.Sleep(NsoServer.World.TileMotion.StepMilliseconds); // no accelerated catch-up after a stall
            }
        }

        // Zone.run: every half second the monsters of zones that have players act
        private void TickLoop()
        {
            while (running)
            {
                try
                {
                    if (World != null)
                    {
                        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        lock (World.SyncRoot)
                            foreach (var z in World.AllZones().ToArray())   // copy: instances may close during the tick
                            {
                                PlayerCombat.UpdateRecovery(z, World);   // dead monsters come back (Zone.updateRecovery)
                                PlayerEffects.UpdateMobEffects(z);       // burn / freeze / stun end (Mob.update)
                                PlayerItems.UpdateItemMaps(z);           // shared ground lifetime for all characters
                                if (!NsoServer.Net.Gymnasium.UpdateZone(z, World, now))   // NV Thách đấu room: headmaster AI, time limit
                                    TaskMechanics.UpdateZone(z, now);    // quest nodes (ore, ice crystal) come back
                                for (int i = z.Players.Count - 1; i >= 0; i--)
                                {
                                    PlayerLog.Tick((PlayerState)z.Players[i], now);                    // periodic STATE line (offline debug log)
                                    PlayerEffects.UpdateHalfSecond((PlayerState)z.Players[i], World);   // effects, HP/MP regeneration
                                    TaskMechanics.UpdatePlayer((PlayerState)z.Players[i], now);         // timed quest actions
                                    if (i < z.Players.Count) NsoServer.Net.Escort.UpdatePlayer((PlayerState)z.Players[i], World, now);   // NV Đưa Jaian trở về
                                }
                                MobBrain.UpdateZone(z, now);
                            }
                        lock (World.SyncRoot) NsoServer.Ai.AgentManager.Tick(World, now);   // autonomous agents (offline AI); no-op unless started
                    }
                }
                catch (Exception ex) { Log("tick error: " + ex.Message); }
                PlayerLog.Flush();
                Thread.Sleep(500);
            }
        }

        // A new client connection: returns the client's end of the pipe, the server reads the other end.
        public Stream Connect()
        {
            if (!running) throw new IOException("Server not running");
            Stream clientSide, serverSide;
            LocalPipe.Create(out clientSide, out serverSide);
            var s = new Session(this, serverSide, Interlocked.Increment(ref nextId));
            lock (sync) sessions.Add(s);
            s.Start();
            return clientSide;
        }

        /// <summary>An offline AI player (NsoServer.Ai): a session without a stream, signed in as <paramref name="account"/>.
        /// The bot drives it with Controller.OnMessage, exactly the messages a game client sends.</summary>
        public Session ConnectBot(string account)
        {
            if (!running) throw new IOException("Server not running");
            var s = new Session(this, Interlocked.Increment(ref nextId), account);
            lock (sync) sessions.Add(s);
            return s;
        }

        public void Remove(Session s)
        {
            lock (sync) sessions.Remove(s);
        }

        public void Stop()
        {
            running = false;
            Session[] copy;
            lock (sync) copy = sessions.ToArray();
            foreach (var s in copy) s.Close();
            Accounts.Close();   // flushes the SQLite write-ahead log
            Log("Server stopped");
        }

        public void Log(string text) { PlayerLog.Trace(text); Logger(text); }   // the player log keeps every line unfiltered
    }
}
