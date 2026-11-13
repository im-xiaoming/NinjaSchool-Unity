using System;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;
using NsoServer.Store;

// `upgradecheck` (Play mode 2026-11-13: every bot stopped at "Nâng cấp vũ khí"): an AI player on NV nâng cấp trang bị
// step 1, wearing its school weapon, with a level 10+ charm and shoes in the bag, crystals and yen, must upgrade and wear
// the weapon, the adornment and the clothing (steps 1-3) within 3 minutes.
static class UpgradeCheck
{
    public static int Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "nso_upgrade_" + Guid.NewGuid().ToString("N"));
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
                me.Record.Exp = server.World.ExpForLevel(13);
                me.Level = 13;
                me.MaxHp = me.Hp = 100000;
                me.Record.ClassId = 1;
                me.Record.TaskId = 12;
                me.Progress.TaskActive = true;
                me.Progress.TaskIndex = 1;
                me.Progress.TaskCount = 0;
                me.Progress.Yen = 50000;
                var bag = me.Inventory.Bag;
                for (int i = 0; i < bag.Length; i++)
                    bag[i] = null;
                me.Inventory.Equipment[1] = new PlayerItem
                {
                    Id = 94,
                    Index = 1,
                    Quantity = 1
                }; // Bạch Hạc Kiếm (level 10, class 1)
                bag[0] = new PlayerItem
                {
                    Id = 13,
                    Index = 0,
                    Quantity = 10
                }; // HP potions
                bag[1] = new PlayerItem
                {
                    Id = 174,
                    Index = 1,
                    Quantity = 1
                }; // Bùa Hộ Thân (level 12)
                bag[2] = new PlayerItem
                {
                    Id = me.Record.Gender == 0 ? 125 : 124,
                    Index = 2,
                    Quantity = 1
                }; // Giày Thô Bố (level 11)
                for (int i = 3; i < 15; i++)
                    bag[i] = new PlayerItem
                    {
                        Id = 2,
                        Index = i,
                        Quantity = 1
                    }; // Đá cấp 3 (16 each)
                me.Actions.ChangeMap(22);
                var plan = new Decision
                {
                    Source = "test",
                    Reason = "upgrade check"
                };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Quest });
                a.Plan = plan;
                a.StepIndex = 0;
                a.NeedsDecision = false;
                a.Route = null;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Console.WriteLine("start: quest " + me.Record.TaskId + " step " + me.Progress.TaskIndex + ", gender " + me.Record.Gender);
            }

            bool done = false;
            for (int i = 0; i < 360 && !done; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot)
                    done = a.Me.Record.TaskId > 12 || (a.Me.Record.TaskId == 12 && a.Me.Progress.TaskIndex >= 4);
            }

            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                Console.WriteLine("end: quest " + me.Record.TaskId + " step " + me.Progress.TaskIndex + "; " + a.Activity);
                Console.WriteLine("worn: " + string.Join(", ", me.Inventory.Equipment.Where(x => x != null).Select(x => server.World.FindItem(x.Id).Name + " +" + x.Upgrade)));
                Console.WriteLine("memory: " + string.Join(" | ", a.Memory.Where(m => m.Contains("upgrade") || m.Contains("put on")).Skip(0).Take(10)));
                if (!done)
                {
                    bad++;
                    Console.WriteLine("FAIL: the upgrade steps were not completed");
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

        Console.WriteLine(bad == 0 ? "upgradecheck OK" : bad + " FAILURES");
        return bad;
    }
}
