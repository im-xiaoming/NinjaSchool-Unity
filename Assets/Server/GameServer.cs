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
            Accounts = new AccountStore(config.DataDirectory);
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
            new Thread(TickLoop) { IsBackground = true, Name = "nso-tick" }.Start();
            Log("Server started");
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
                            foreach (var z in World.AllZones())
                            {
                                PlayerCombat.UpdateRecovery(z, World);   // dead monsters come back (Zone.updateRecovery)
                                PlayerEffects.UpdateMobEffects(z);       // burn / freeze / stun end (Mob.update)
                                PlayerItems.UpdateItemMaps(z);           // items on the ground vanish after 30 s
                                for (int i = z.Players.Count - 1; i >= 0; i--)
                                    PlayerEffects.UpdateHalfSecond((PlayerState)z.Players[i], World);   // effects, HP/MP regeneration
                                MobBrain.UpdateZone(z, now);
                            }
                    }
                }
                catch (Exception ex) { Log("tick error: " + ex.Message); }
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
            Log("Server stopped");
        }

        public void Log(string text) { Logger(text); }
    }
}
