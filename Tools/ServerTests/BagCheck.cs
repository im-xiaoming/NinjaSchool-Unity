using System;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;
using NsoServer.Store;
using NsoServer.Net;

// `bagcheck`: one AI player (rules, scratch save folder) whose bag is full of loose stones and one potion stack must
// sell stones (Char.saleItem) until AgentConfig.BagReserveSlots slots are free, and must keep the potions.
static class BagCheck
{
    public static int Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "nso_bag_" + Guid.NewGuid().ToString("N"));
        var server = new GameServer(new ServerConfig { DataDirectory = Path.Combine(dir, "NsoData") });
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
        server.Start();
        NsoServer.Ai.Rl.RlRuntime.LearningRootOverride = Path.Combine(dir, "NsoLearning");
        NsoServer.Ai.Rl.RlRuntime.TrainerScript = "";
        AgentConfig.AgentCount = 1; AgentConfig.LlmEnabled = false; AgentConfig.RagEnabled = false; AgentConfig.LoginSpacingSeconds = 0;
        AgentConfig.BagReserveSlots = 3;
        int bad = 0;
        try
        {
            AgentManager.Start(server);
            var field = typeof(AgentManager).GetField("Agents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var agents = (System.Collections.Generic.List<NinjaAgent>)field.GetValue(null);
            NinjaAgent a = null;
            for (int i = 0; i < 100 && (a == null || a.Me == null); i++) { System.Threading.Thread.Sleep(100); a = agents.FirstOrDefault(); }
            if (a == null || a.Me == null) { Console.WriteLine("FAIL: AI player did not log in"); return 1; }
            long yenBefore;
            lock (server.World.SyncRoot)
            {
                var me = a.Me; var bag = me.Inventory.Bag;
                for (int i = 0; i < bag.Length; i++)   // test setup: every slot taken, slot 0 potions, the rest stones (item 0)
                    bag[i] = new PlayerItem { Id = i == 0 ? 13 : 0, Index = i, Quantity = i == 0 ? 5 : 1, Yen = 5 };
                me.Record.TaskId = 13; me.Progress.TaskActive = false;   // past NV nâng cấp trang bị: crystals may be sold
                yenBefore = me.Progress.Yen;
                var plan = new Decision { Source = "test", Reason = "bag check" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            int free = 0;
            for (int i = 0; i < 40 && free < 3; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot) free = PlayerItemsFree(a.Me);
            }
            System.Threading.Thread.Sleep(6000);   // two more checks: it must stop at the reserve
            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                free = PlayerItemsFree(me);
                Console.WriteLine("free slots " + free + ", yen " + yenBefore + " -> " + me.Progress.Yen + ", slot 0 " + (me.Inventory.Bag[0] == null ? "empty" : "#" + me.Inventory.Bag[0].Id));
                Console.WriteLine("memory: " + string.Join(" | ", a.Memory.Skip(Math.Max(0, a.Memory.Count - 4))));
                if (free != 3) { bad++; Console.WriteLine("FAIL: expected exactly 3 free slots"); }
                if (me.Inventory.Bag[0] == null || me.Inventory.Bag[0].Id != 13) { bad++; Console.WriteLine("FAIL: the potions were sold"); }
                if (me.Progress.Yen <= yenBefore) { bad++; Console.WriteLine("FAIL: no yen from the sale"); }
            }
        }
        finally
        {
            AgentManager.Stop();
            server.Stop();
            try { Directory.Delete(dir, true); } catch { }
        }
        Console.WriteLine(bad == 0 ? "bagcheck OK" : bad + " FAILURES");
        return bad;
    }

    private static int PlayerItemsFree(PlayerState me) { return me.Inventory.Bag.Count(x => x == null); }
}
