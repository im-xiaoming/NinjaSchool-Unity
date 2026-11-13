using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;

// `movecheck [seconds] [bots]`: AI players (rules, scratch save folder) play; every position the server accepts (what
// the other clients receive as PLAYER_MOVE) is sampled. Reports move reports per second, direction reversals (the
// "turning left and right" a watching player sees: a reversal after less than 40 px of travel) and kills.
static class MoveCheck
{
    public static int Run(int seconds, int bots)
    {
        string dir = Path.Combine(Path.GetTempPath(), "nso_move_" + Guid.NewGuid().ToString("N"));
        var server = new GameServer(new ServerConfig { DataDirectory = Path.Combine(dir, "NsoData") });
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
        server.Start();
        NsoServer.Ai.Rl.RlRuntime.LearningRootOverride = Path.Combine(dir, "NsoLearning");
        NsoServer.Ai.Rl.RlRuntime.TrainerScript = "";
        AgentConfig.AgentCount = bots; AgentConfig.LlmEnabled = false; AgentConfig.RagEnabled = false; AgentConfig.LoginSpacingSeconds = 0;
        int bad = 0;
        try
        {
            AgentManager.Start(server);
            var field = typeof(AgentManager).GetField("Agents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var agents = (List<NinjaAgent>)field.GetValue(null);
            for (int i = 0; i < 200 && (agents.Count < bots || agents.Any(x => x.Me == null)); i++) System.Threading.Thread.Sleep(100);
            var last = new Dictionary<NinjaAgent, int[]>();   // x, y, last dir, px travelled since the last reversal
            var reports = new Dictionary<NinjaAgent, int>(); var flips = new Dictionary<NinjaAgent, int>(); var travel = new Dictionary<NinjaAgent, long>();
            var kills0 = new Dictionary<NinjaAgent, int>();
            lock (server.World.SyncRoot) foreach (var a in agents) { reports[a] = 0; flips[a] = 0; travel[a] = 0; kills0[a] = a.Kills; }
            var end = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < end)
            {
                System.Threading.Thread.Sleep(10);
                lock (server.World.SyncRoot)
                    foreach (var a in agents)
                    {
                        var me = a.Me; if (me == null) continue;
                        int[] l;
                        if (!last.TryGetValue(a, out l)) { last[a] = new[] { me.X, me.Y, 0, 0 }; continue; }
                        if (l[0] == me.X && l[1] == me.Y) continue;
                        int dx = me.X - l[0];
                        if (Math.Abs(dx) > 200 || Math.Abs(me.Y - l[1]) > 300) { last[a] = new[] { me.X, me.Y, 0, 0 }; continue; }   // map change / revive
                        reports[a]++; travel[a] += Math.Abs(dx);
                        int d = Math.Sign(dx);
                        if (d != 0 && l[2] != 0 && d != l[2] && l[3] < 40) flips[a]++;
                        if (d != 0 && d != l[2]) l[3] = 0;
                        l[3] += Math.Abs(dx);
                        l[0] = me.X; l[1] = me.Y; if (d != 0) l[2] = d;
                    }
            }
            int totalFlips = 0, totalReports = 0;
            lock (server.World.SyncRoot)
                foreach (var a in agents)
                {
                    totalFlips += flips[a]; totalReports += reports[a];
                    Console.WriteLine(a.Name.PadRight(10) + " reports/s " + (reports[a] / (double)seconds).ToString("0.0").PadLeft(5) +
                        "  short reversals " + flips[a].ToString().PadLeft(3) + "  px " + travel[a].ToString().PadLeft(6) +
                        "  kills " + (a.Kills - kills0[a]) + "  map " + a.Me.MapId + "  " + a.Activity);
                }
            double perMin = totalFlips * 60.0 / seconds / Math.Max(1, agents.Count);
            Console.WriteLine("short reversals per bot-minute: " + perMin.ToString("0.0") + ", reports/s per bot: " + (totalReports / (double)seconds / Math.Max(1, agents.Count)).ToString("0.0"));
            if (perMin > 6) { bad++; Console.WriteLine("FAIL: bots turn back and forth"); }
        }
        finally
        {
            AgentManager.Stop();
            server.Stop();
            try { Directory.Delete(dir, true); } catch { }
        }
        Console.WriteLine(bad == 0 ? "movecheck OK" : bad + " FAILURES");
        return bad;
    }
}
