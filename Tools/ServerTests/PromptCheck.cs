using System;
using System.IO;
using System.Linq;
using NsoServer;
using NsoServer.Ai;

// `promptcheck`: one AI player (rules only, scratch save folder) logs in; prints the LLM prompt it would send (map
// coordinates, actions), parses a "goto" reply, then runs a goto step to an NPC and reports where the player ends up.
static class PromptCheck
{
    public static int Run(bool args_rag = false)
    {
        string ragCollection = "nso_test_" + Guid.NewGuid().ToString("N");
        AgentConfig.RagEnabled = args_rag;
        AgentConfig.RagCollection = ragCollection;
        string dir = Path.Combine(Path.GetTempPath(), "nso_prompt_" + Guid.NewGuid().ToString("N"));
        var server = new GameServer(new ServerConfig { DataDirectory = Path.Combine(dir, "NsoData") });
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
        server.Start();
        NsoServer.Ai.Rl.RlRuntime.LearningRootOverride = Path.Combine(dir, "NsoLearning");
        NsoServer.Ai.Rl.RlRuntime.TrainerScript = "";
        AgentConfig.AgentCount = 1;
        AgentConfig.LlmEnabled = false;
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

            System.Threading.Thread.Sleep(3000); // let the map graph build on its worker
            string prompt;
            AgentSnapshot initial;
            lock (server.World.SyncRoot)
                initial = AgentManager.Snapshot(a);
            prompt = LlmPlanner.BuildPrompt(initial); // HTTP is outside the game lock.
            Console.WriteLine(prompt);
            if (!prompt.Contains("CURRENT MAP") || !prompt.Contains("YOU at ("))
            {
                bad++;
                Console.WriteLine("FAIL: no map view");
            }

            if (!LlmPlanner.SystemPrompt.Contains("YOUR ACTIONS"))
            {
                bad++;
                Console.WriteLine("FAIL: no actions text");
            }

            if (!prompt.Contains("QUEST NEXT: ") || !prompt.Contains("YOUR PRIORITIES NOW: "))
            {
                bad++;
                Console.WriteLine("FAIL: no quest guide / priorities in the prompt");
            }

            string error;
            var d = DecisionParser.Parse("{\"reason\":\"r\",\"say\":\"\",\"fleeHp\":30,\"steps\":[{\"goal\":\"goto\",\"map\":-1,\"mob\":-1,\"count\":1,\"seconds\":30,\"x\":500,\"y\":-1}]}", out error);
            if (d == null || d.Steps.Count != 1 || d.Steps[0].Goal != GoalType.Move || d.Steps[0].X != 500)
            {
                bad++;
                Console.WriteLine("FAIL: goto not parsed " + error);
            }
            else
                Console.WriteLine("goto parsed: " + d.Steps[0]);
            // long-term memory (Qdrant + bge-m3) and the summary when the context is full, with the real local LLM
            if (args_rag)
            {
                for (int i = 0; i < 100 && System.Threading.Interlocked.Read(ref AgentRag.Stored) < 5; i++)
                    System.Threading.Thread.Sleep(200);
                Console.WriteLine("rag stored " + AgentRag.Stored + ", error: " + AgentRag.LastError);
                AgentSnapshot snap;
                lock (server.World.SyncRoot)
                {
                    for (int i = 0; i < 22; i++)
                        a.Remember("test event " + i + ": killed a snail near Kiriko, map 22");
                    a.Knowledge.Learn("danger:test", "Ốc sên killed me once in map 21");
                    snap = AgentManager.Snapshot(a);
                }

                System.Threading.Thread.Sleep(3000); // let the worker embed the new memories
                var recalled = AgentRag.Recall(a.Account, LlmPlanner.RecallQuery(snap), 6);
                Console.WriteLine("recalled: " + (recalled == null ? "null (" + AgentRag.LastError + ")" : string.Join(" || ", recalled)));
                if (recalled == null || recalled.Count == 0)
                {
                    bad++;
                    Console.WriteLine("FAIL: nothing recalled");
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    AgentConfig.LlmEnabled = true;
                    var decision = new LlmPlanner().Decide(snap);
                    Console.WriteLine("LLM " + sw.ElapsedMilliseconds + " ms: " + decision.Reason + " -> " + string.Join("; ", decision.Steps));
                    Console.WriteLine("summary (cut " + decision.SummaryCut + "): " + decision.Summary);
                    if (decision.Summary == null)
                    {
                        bad++;
                        Console.WriteLine("FAIL: full memory was not summarised");
                    }
                }
                catch (Exception ex)
                {
                    bad++;
                    Console.WriteLine("FAIL: LLM " + ex.Message);
                }
                finally
                {
                    AgentConfig.LlmEnabled = false;
                }
            }

            // walk to the farthest NPC of the map
            int tx, ty;
            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                var npc = me.Zone.Tile.Npcs.OrderByDescending(n => Math.Abs(n.X - me.X)).First();
                tx = npc.X;
                ty = npc.Y;
                var plan = new Decision
                {
                    Source = "test",
                    Reason = "goto check"
                };
                plan.Steps.Add(new PlanStep { Goal = GoalType.Move, X = tx, Y = ty });
                a.Plan = plan;
                a.StepIndex = 0;
                a.NeedsDecision = false;
                a.StepStartedAt = a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Console.WriteLine("goto NPC at (" + tx + "," + ty + ") from (" + me.X + "," + me.Y + ") map " + me.MapId);
            }

            for (int i = 0; i < 60; i++)
            {
                System.Threading.Thread.Sleep(500);
                lock (server.World.SyncRoot)
                    if (a.Plan == null || a.Plan.Source != "test")
                        break;
            }

            lock (server.World.SyncRoot)
            {
                var me = a.Me;
                bool arrived = Math.Abs(me.X - tx) <= 24 && Math.Abs(me.Y - ty) <= 40;
                Console.WriteLine("ended at (" + me.X + "," + me.Y + ") " + (arrived ? "ARRIVED" : "NOT ARRIVED") + "; last: " + string.Join(" | ", a.Memory.Skip(Math.Max(0, a.Memory.Count - 3))));
                if (!arrived)
                    bad++;
            }
        }
        finally
        {
            AgentManager.Stop();
            if (args_rag)
                try
                {
                    new System.Net.Http.HttpClient().DeleteAsync(AgentConfig.QdrantUrl + "/collections/" + ragCollection).Wait();
                }
                catch
                {
                }

            server.Stop();
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
            }
        }

        Console.WriteLine(bad == 0 ? "promptcheck OK" : bad + " FAILURES");
        return bad;
    }
}
