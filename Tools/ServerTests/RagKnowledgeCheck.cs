using System;
using System.IO;
using System.Linq;
using NsoServer.Ai;

// `ragknowledge`: puts the whole game knowledge into a scratch Qdrant collection (local Qdrant + Ollama bge-m3), checks
// that questions recall the right documents, then restarts the memory and checks that nothing is embedded again.
// The scratch collection is deleted at the end.
static class RagKnowledgeCheck
{
    public static int Run()
    {
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var tables = NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt"));
        var world = new NsoServer.World.GameWorld(tables, Path.Combine(staticRoot, "Data"));
        string notes = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoWiki\notes.json");
        GameKnowledge.Source = () => NsoServer.DevWiki.WikiData.Build(tables, world, notes);
        AgentConfig.RagEnabled = true;
        AgentConfig.RagGameKnowledge = true;
        AgentConfig.RagCollection = "nso_test_" + Guid.NewGuid().ToString("N");
        int bad = 0;
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            AgentRag.Start(world);
            int expected = 0;
            for (int i = 0; i < 1200; i++) // up to 10 minutes
            {
                System.Threading.Thread.Sleep(500);
                expected = GameKnowledge.Documents;
                if (expected > 0 && System.Threading.Interlocked.Read(ref AgentRag.Stored) >= expected)
                    break;
            }

            Console.WriteLine("stored " + AgentRag.Stored + "/" + expected + " in " + sw.Elapsed.TotalSeconds.ToString("0") + " s; knowledge error: " + GameKnowledge.LastError + "; rag error: " + AgentRag.LastError);
            if (expected == 0 || AgentRag.Stored < expected)
            {
                bad++;
                Console.WriteLine("FAIL: knowledge not stored");
            }

            System.Threading.Thread.Sleep(1500); // upserts are wait=false
            var questions = new[]
            {
                new[]
                {
                    "Where can I buy food that restores HP and MP?",
                    "Tabemono",
                    "food:"
                },
                new[]
                {
                    "How do I get Bông thảo dược for the herb quest?",
                    "Bông thảo dược"
                },
                new[]
                {
                    "Which NPC sells HP potions?",
                    "Kiriko",
                    "Bình HP"
                },
                new[]
                {
                    "What does a skill book do?",
                    "skill book"
                },
            };
            foreach (var q in questions)
            {
                var hits = AgentRag.Recall("nobody", q[0], 0) ?? new System.Collections.Generic.List<string>();
                Console.WriteLine("Q: " + q[0]);
                foreach (var h in hits)
                    Console.WriteLine("   " + (h.Length > 160 ? h.Substring(0, 160) + "..." : h));
                for (int i = 1; i < q.Length; i++)
                    if (!hits.Any(h => h.Contains(q[i])))
                    {
                        bad++;
                        Console.WriteLine("FAIL: nothing about \"" + q[i] + "\"");
                    }
            }

            // restart: the documents are already in Qdrant with the same text, nothing is embedded again
            AgentRag.Stop();
            System.Threading.Thread.Sleep(2500);
            var storedField = typeof(AgentRag).GetField("stored", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            ((System.Collections.IDictionary)storedField.GetValue(null)).Clear();
            long before = AgentRag.Stored;
            sw.Restart();
            AgentRag.Start(world);
            System.Threading.Thread.Sleep(30000);
            Console.WriteLine("after restart: " + (AgentRag.Stored - before) + " documents embedded again in 30 s");
            if (AgentRag.Stored - before > 50)
            {
                bad++;
                Console.WriteLine("FAIL: unchanged documents embedded again");
            }
        }
        finally
        {
            AgentRag.Stop();
            try
            {
                new System.Net.Http.HttpClient().DeleteAsync(AgentConfig.QdrantUrl + "/collections/" + AgentConfig.RagCollection).Wait();
            }
            catch
            {
            }
        }

        Console.WriteLine(bad == 0 ? "ragknowledge OK" : bad + " FAILURES");
        return bad;
    }
}
