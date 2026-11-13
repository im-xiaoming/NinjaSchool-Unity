using System;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;

// `foodcheck`: one AI player (rules, scratch save folder) that has finished quest 3 and has money gets a shop step: it
// must buy potions at Kiriko, then food at Tabemono, and eat it (a food effect running) within 90 s.
static class FoodCheck
{
    public static int Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "nso_food_" + Guid.NewGuid().ToString("N"));
        var server = new GameServer(new ServerConfig { DataDirectory = Path.Combine(dir, "NsoData") });
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
        server.Start();
        NsoServer.Ai.Rl.RlRuntime.LearningRootOverride = Path.Combine(dir, "NsoLearning");
        NsoServer.Ai.Rl.RlRuntime.TrainerScript = "";
        AgentConfig.AgentCount = 1;
        AgentConfig.LlmEnabled = false;
        AgentConfig.RagEnabled = false;
        AgentConfig.LoginSpacingSeconds = 0;
        int bad = 0;
        try
        {
            AgentManager.Start(server);
            var field = typeof(AgentManager).GetField("Agents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var agents = (System.Collections.Generic.List<NinjaAgent>)field.GetValue(null);
            NinjaAgent a = null;
            for (int i = 0; i < 100 && (a == null || a.Me == null); i++)
            {
                System.Threading.Thread.Sleep(100);
                a = agents.FirstOrDefault();
            }

            if (a == null || a.Me == null)
            {
                Console.WriteLine("FAIL: AI player did not log in");
                return 1;
            }

            System.Threading.Thread.Sleep(3000); // map graph
            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                me.Record.TaskId = 4;
                me.Progress.TaskActive = false;
                me.Progress.Yen = 20000; // test setup: shops open, money
                var plan = new Decision
                {
                    Source = "test",
                    Reason = "food check"
                };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Shop });
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan;
                a.StepIndex = 0;
                a.NeedsDecision = false;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Console.WriteLine("start map " + me.MapId + " (" + me.X + "," + me.Y + ") yen " + me.Progress.Yen);
            }

            bool fed = false;
            for (int i = 0; i < 180 && !fed; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot)
                    fed = AgentTactics.Fed(a.Me, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                Console.WriteLine("HP potions " + AgentTactics.HpPotions(me, server.World) + ", food " + AgentTactics.Foods(me, server.World) + ", fed " + fed + ", last item " + a.Client.LastUsedItemName + ", yen " + me.Progress.Yen + "; " + a.Activity);
                Console.WriteLine("memory: " + string.Join(" | ", a.Memory.Skip(Math.Max(0, a.Memory.Count - 6))));
                if (!fed)
                {
                    bad++;
                    Console.WriteLine("FAIL: no food effect running");
                }
            }
        }
        finally
        {
            AgentManager.Stop();
            server.Stop();
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
            }
        }

        Console.WriteLine(bad == 0 ? "foodcheck OK" : bad + " FAILURES");
        return bad;
    }
}
