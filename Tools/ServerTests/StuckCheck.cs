using System;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;

// `stuckcheck` (Play mode 2026-11-13, bots #7/#8): an AI player standing on map 6's bottom row at the exit to map 20
// (155,1920) - where it arrives from map 20 - must find its way out: travel to Làng Tone (22) and leave map 6 in 60 s.
static class StuckCheck
{
    public static int Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "nso_stuck_" + Guid.NewGuid().ToString("N"));
        var server = new GameServer(new ServerConfig { DataDirectory = Path.Combine(dir, "NsoData") });
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
        server.Start();
        NsoServer.Ai.Rl.RlRuntime.LearningRootOverride = Path.Combine(dir, "NsoLearning");
        NsoServer.Ai.Rl.RlRuntime.TrainerScript = "";
        AgentConfig.AgentCount = 1; AgentConfig.LlmEnabled = false; AgentConfig.RagEnabled = false; AgentConfig.LoginSpacingSeconds = 0;
        int bad = 0;
        try
        {
            AgentManager.Start(server);
            var field = typeof(AgentManager).GetField("Agents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var agents = (System.Collections.Generic.List<NinjaAgent>)field.GetValue(null);
            NinjaAgent a = null;
            for (int i = 0; i < 100 && (a == null || a.Me == null); i++) { System.Threading.Thread.Sleep(100); a = agents.FirstOrDefault(); }
            if (a == null || a.Me == null) { Console.WriteLine("FAIL: AI player did not log in"); return 1; }
            System.Threading.Thread.Sleep(3000);   // map graph
            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                me.Record.TaskId = 5; me.Progress.TaskActive = true; me.Progress.TaskIndex = 1;   // quest 5 like #7/#8: map 6 open
                me.Record.Exp = server.World.ExpForLevel(20); me.Level = 20; me.MaxHp = me.Hp = 100000;   // survive the monsters on the way
                me.SetXY(155, 1920);
                me.Actions.ChangeMap(6);
                me.SetXY(155, 1920);
                var plan = new Decision { Source = "test", Reason = "stuck check" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Travel, MapId = 22 });
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false; a.Route = null;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Console.WriteLine("start map " + me.MapId + " (" + me.X + "," + me.Y + ")");
            }
            bool left = false;
            for (int i = 0; i < 120 && !left; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot) left = a.Me.MapId != 6;
            }
            lock (server.World.SyncRoot)
            {
                Console.WriteLine("memory: " + string.Join(" | ", a.Memory.Skip(Math.Max(0, a.Memory.Count - 8))));
                if (a.Deaths > 0) { bad++; Console.WriteLine("FAIL: died on the way (" + a.Deaths + ")"); }
                Console.WriteLine("after: map " + a.Me.MapId + " (" + a.Me.X + "," + a.Me.Y + "); " + a.Activity);
                if (!left) { bad++; Console.WriteLine("FAIL: still in map 6"); }
            }
        }
        finally
        {
            AgentManager.Stop();
            server.Stop();
            try { Directory.Delete(dir, true); } catch { }
        }
        Console.WriteLine(bad == 0 ? "stuckcheck OK" : bad + " FAILURES");
        return bad;
    }
}
