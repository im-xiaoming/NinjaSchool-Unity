using System;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;
using NsoServer.Store;

// `ledgercheck` (item ledger, user 2026-11-13): one AI player (rules, scratch save folder) with money and potions
// 1) picks a never-used village-shop item, buys one and uses it -> the ledger knows it and the knowledge has "item:<id>";
// 2) an item marked useless (used 3 times, never helped) bought again gives the wastefulPurchase penalty.
static class LedgerCheck
{
    public static int Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "nso_ledger_" + Guid.NewGuid().ToString("N"));
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
            int tryId = -1;
            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                me.Record.TaskId = 4; me.Progress.TaskActive = false; me.Progress.Yen = 20000; me.Progress.Xu = 20000;   // shops open, money
                var bag = me.Inventory.Bag;
                for (int i = 0; i < bag.Length; i++) bag[i] = null;
                bag[0] = new PlayerItem { Id = 13, Index = 0, Quantity = 10 };   // 10 HP potions: enough to fight, so trying is allowed
                long price;
                if (!AgentTactics.PickTryItem(a, me, server.World, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), out tryId, out price))
                { Console.WriteLine("FAIL: no item to try"); return 1; }
                Console.WriteLine("try item " + tryId + " " + server.World.FindItem(tryId).Name + " for " + price);
                var plan = new Decision { Source = "test", Reason = "ledger check" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Shop, MobId = tryId, Count = 1 });
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            bool known = false;
            for (int i = 0; i < 180 && !known; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot) known = a.Learning.ItemRewards.Knows(tryId) || a.FailedItems.Contains(tryId);
            }
            lock (server.World.SyncRoot)
            {
                var h = a.Learning.ItemRewards;
                var note = h.Ledger.TryGetValue(tryId, out var n) ? n : null;
                Console.WriteLine("ledger: " + (note != null ? note.Text(server.World.FindItem(tryId).Name) : "none") + "; " + a.Activity);
                Console.WriteLine("memory: " + string.Join(" | ", a.Memory.Skip(Math.Max(0, a.Memory.Count - 4))));
                if (note == null || note.Bought < 1 || note.Used < 1) { bad++; Console.WriteLine("FAIL: item not bought and used"); }
                var fact = a.Knowledge.Get("ledger:" + tryId);
                Console.WriteLine("knowledge: " + (fact != null ? fact.Text : "none"));
                if (fact == null) { bad++; Console.WriteLine("FAIL: no knowledge about the item"); }
                // 2) the same item is now known; mark another shop item useless and buy it
                var useless = h.Note(18 == tryId ? 19 : 18); useless.Used = 3; useless.Useful = 0;
            }
            int uselessId = tryId == 18 ? 19 : 18;
            double before;
            lock (server.World.SyncRoot) before = a.Learning.ItemRewards.Totals[5];
            lock (server.World.SyncRoot)
            {
                var plan = new Decision { Source = "test", Reason = "wasteful purchase" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false;
            }
            var store = server.World.Store(7); int index = store.FindIndex(e => e.ItemId == uselessId);
            if (index < 0) { store = server.World.Store(6); index = store.FindIndex(e => e.ItemId == uselessId); }
            int shop = store == server.World.Store(7) ? 7 : 6;
            if (index < 0) { bad++; Console.WriteLine("FAIL: item " + uselessId + " not in Kiriko's shops"); }
            else
            {
                lock (server.World.SyncRoot)
                {
                    a.Client.RequestShop(shop);
                    a.Client.Buy(shop, index, 1);
                }
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot)
                {
                    double after = a.Learning.ItemRewards.Totals[5];
                    Console.WriteLine("wastefulPurchase reward " + before + " -> " + after + ", bought " + a.Learning.ItemRewards.Note(uselessId).Bought);
                    if (after >= before) { bad++; Console.WriteLine("FAIL: buying a useless item was not penalised"); }
                }
            }
            // 3) death priced by its real cost: far from the village it costs the walk back; when the plan needs the
            //    village anyway (shop step) only the revive wait
            lock (server.World.SyncRoot)
            {
                var cfg = NsoServer.Ai.Rl.RlRuntime.Settings;
                double village = NsoServer.Ai.Rl.RlRuntime.DeathCost(a, 22, cfg), far = NsoServer.Ai.Rl.RlRuntime.DeathCost(a, 23, cfg);
                var plan = new Decision { Source = "test", Reason = "death cost" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Shop });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false;
                double farShopping = NsoServer.Ai.Rl.RlRuntime.DeathCost(a, 23, cfg);
                Console.WriteLine("death cost village " + village + ", map 23 " + far + ", map 23 while going shopping " + farShopping);
                if (!(far > village) || Math.Abs(farShopping - cfg.DeathPenalty) > 1e-9) { bad++; Console.WriteLine("FAIL: death cost"); }
            }
            // 4) stronger items when needed: the cheapest potion that covers the missing HP, the strongest when urgent
            lock (server.World.SyncRoot)
            {
                var me = a.Me; var bag = me.Inventory.Bag;
                for (int i = 0; i < bag.Length; i++) bag[i] = null;
                bag[0] = new PlayerItem { Id = 13, Index = 0, Quantity = 5 };   // 25 HP x 6 ticks
                bag[1] = new PlayerItem { Id = 14, Index = 1, Quantity = 5 };   // 90 HP x 6 ticks, level 10
                int savedLevel = me.Level, savedHp = me.Hp; me.Level = 10; me.MaxHp = Math.Max(me.MaxHp, 1000);
                me.Hp = me.MaxHp - 50; int small = AgentTactics.ChoosePotion(me, server.World, NsoServer.Net.PlayerItems.TypeHp, false);
                me.Hp = me.MaxHp - 400; int big = AgentTactics.ChoosePotion(me, server.World, NsoServer.Net.PlayerItems.TypeHp, false);
                me.Hp = me.MaxHp - 50; int urgent = AgentTactics.ChoosePotion(me, server.World, NsoServer.Net.PlayerItems.TypeHp, true);
                Console.WriteLine("potion choice: missing 50 -> slot " + small + ", missing 400 -> slot " + big + ", urgent -> slot " + urgent);
                if (small != 0 || big != 1 || urgent != 1) { bad++; Console.WriteLine("FAIL: potion choice"); }
                me.Level = savedLevel; me.Hp = savedHp;
            }
            // 5) fast travel: a short trip walks, a long one uses the teleport scroll (Vô hạn khả di lệnh) and arrives
            int farTarget = -1;
            lock (server.World.SyncRoot)
            {
                var me = a.Me; var bag = me.Inventory.Bag;
                me.Record.TaskId = 30; me.Progress.TaskActive = false;   // test setup: most maps unlocked
                bag[2] = new PlayerItem { Id = NsoServer.Net.PlayerItemUse.ItemVoHanKhaDi, Index = 2, Quantity = 1 };
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var near = NsoServer.Ai.WorldKnowledge.Route(server.World, me.MapId, 23, me.Record.TaskId);
                var shortTrip = AgentTactics.PlanFastTravel(a, me, server.World, 23, near != null ? near.Count : 1, now);
                int bestHops = 0;
                foreach (int m in new[] { 38, 43, 48, 32, 17, 10 })
                {
                    var r = NsoServer.Ai.WorldKnowledge.Route(server.World, me.MapId, m, me.Record.TaskId);
                    if (r != null && r.Count > bestHops) { bestHops = r.Count; farTarget = m; }
                }
                var longTrip = farTarget >= 0 ? AgentTactics.PlanFastTravel(a, me, server.World, farTarget, bestHops, now) : null;
                Console.WriteLine("from map " + me.MapId + ": map 23 (" + (near != null ? near.Count : -1) + " hops) fast=" + (shortTrip != null) +
                    "; map " + farTarget + " (" + bestHops + " hops) fast=" + (longTrip != null ? longTrip.Label + " to " + longTrip.Dest + ", saves " + longTrip.Saved : "none"));
                if (shortTrip != null || longTrip == null) { bad++; Console.WriteLine("FAIL: fast travel judgement"); }
                var plan = new Decision { Source = "test", Reason = "fast travel" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Travel, MapId = farTarget });
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false; a.Route = null; a.Fast = null;
                a.StepStartedAt = a.LastProgressAt = now;
            }
            bool took = false;
            for (int i = 0; i < 20 && !took; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot) took = a.Memory.Any(m => m.Contains("took "));
            }
            lock (server.World.SyncRoot)
            {
                Console.WriteLine("after 10 s: map " + a.Me.MapId + "; " + a.Memory.LastOrDefault(m => m.Contains("took ") || m.Contains("could not take")));
                if (!took) { bad++; Console.WriteLine("FAIL: the teleport scroll was not used"); }
                // without the scroll, back to Làng Tone from a village: the carriage (Umayaki)
                var me = a.Me; me.Inventory.Bag[2] = null; a.Memory.Clear();
                var plan = new Decision { Source = "test", Reason = "carriage" };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Travel, MapId = 22 });
                plan.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 600 });
                a.Plan = plan; a.StepIndex = 0; a.NeedsDecision = false; a.Route = null; a.Fast = null; a.NoFastTravelUntil = 0;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            took = false;
            for (int i = 0; i < 90 && !took; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot) took = a.Memory.Any(m => m.Contains("took ") || m.Contains("could not take"));
            }
            lock (server.World.SyncRoot)
            {
                Console.WriteLine("carriage: map " + a.Me.MapId + "; " + a.Memory.LastOrDefault(m => m.Contains("took ") || m.Contains("could not take")) + "; " + a.Activity);
                if (a.Me.MapId != 22) { bad++; Console.WriteLine("FAIL: the carriage was not used"); }
            }
        }
        finally
        {
            AgentManager.Stop();
            server.Stop();
            try { Directory.Delete(dir, true); } catch { }
        }
        Console.WriteLine(bad == 0 ? "ledgercheck OK" : bad + " FAILURES");
        return bad;
    }
}
