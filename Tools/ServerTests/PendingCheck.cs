using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NsoServer;
using NsoServer.Ai;

// Regression for the new asynchronous request correlation, using one real bot in a scratch-only world.
static class PendingCheck
{
    sealed class BlockedPlanner : IStrategicPlanner
    {
        public readonly ManualResetEventSlim Release = new ManualResetEventSlim();
        public string Name
        {
            get
            {
                return "contract-blocked";
            }
        }

        public Decision Decide(AgentSnapshot snapshot)
        {
            Release.Wait(5000);
            var decision = new Decision
            {
                Source = "llm"
            };
            decision.Steps.Add(new PlanStep { Goal = GoalType.Idle });
            return decision;
        }
    }

    public static int Run()
    {
        string scratch = Path.Combine(Path.GetTempPath(), "nso-pending-contract-" + Guid.NewGuid().ToString("N"));
        var server = new GameServer(new ServerConfig { DataDirectory = Path.Combine(scratch, "NsoData") });
        string data = Path.GetFullPath("Assets/StreamingAssets/NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(data, "tables.txt")), Path.Combine(data, "Data"));
        AgentConfig.RagEnabled = false;
        AgentConfig.LlmEnabled = false;
        AgentConfig.AgentCount = 1;
        AgentConfig.LoginSpacingSeconds = 0;
        NsoServer.Ai.Rl.RlRuntime.TrainerScript = "";
        NsoServer.Ai.Rl.RlRuntime.LearningRootOverride = Path.Combine(scratch, "learning");
        var blocked = new BlockedPlanner();
        StrategicService service = null;
        try
        {
            server.Start();
            AgentManager.Start(server);
            var agents = (List<NinjaAgent>)typeof(AgentManager).GetField("Agents", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            NinjaAgent agent = null;
            for (int i = 0; i < 100; i++)
            {
                lock (server.World.SyncRoot)
                    agent = agents.FirstOrDefault(a => a.Me != null);
                if (agent != null)
                    break;
                Thread.Sleep(50);
            }

            if (agent == null)
                throw new Exception("scratch bot did not log in");
            service = new StrategicService(blocked);
            service.Start();
            lock (server.World.SyncRoot)
            {
                typeof(AgentManager).GetField("service", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, service);
                AgentConfig.LlmEnabled = true;
                var decide = typeof(AgentManager).GetMethod("Decide", BindingFlags.Static | BindingFlags.NonPublic);
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                decide.Invoke(null, new object[] { agent, now, null, false });
                long request = agent.DecisionRequest;
                if (!agent.Pending || request == 0)
                    throw new Exception("no correlated request was queued");
                decide.Invoke(null, new object[] { agent, now, "interim fallback", true });
                if (!agent.Pending || agent.DecisionRequest != request)
                    throw new Exception("interim rules decision invalidated the in-flight LLM request");
                Console.WriteLine("PASS pending request survives interim rules decision; sequence changes only on enqueue");
            }

            return 0;
        }
        finally
        {
            blocked.Release.Set();
            AgentManager.Stop();
            server.Stop();
            if (service != null)
                service.Stop();
        }
    }
}
