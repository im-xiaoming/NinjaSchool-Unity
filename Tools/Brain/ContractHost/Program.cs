using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NsoServer.Ai;
using NsoServer.Configuration;

// Shares the real C# sources, but starts no game server, bot, trainer, Play session or character database.
class Program
{
    static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        IncludeFields = true
    };
    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    static int Main(string[] args)
    {
        try
        {
            if (args[0] == "export")
            {
                string root = Path.GetFullPath("Assets/StreamingAssets/NsoStatic");
                var tables = NsoServer.Data.TableFile.Load(Path.Combine(root, "tables.txt"));
                var world = new NsoServer.World.GameWorld(tables, Path.Combine(root, "Data"));
                string wiki = NsoServer.DevWiki.WikiData.Build(tables, world, Path.GetFullPath("Assets/StreamingAssets/NsoWiki/notes.json"));
                File.WriteAllText(args[1], JsonSerializer.Serialize(GameKnowledge.Export(wiki, world), Options));
                Console.WriteLine("PASS C# knowledge export");
                return 0;
            }

            if (args[0] == "config")
            {
                string directory = Path.Combine(Path.GetTempPath(), "nso-config-contract-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "runtime-config.json");
                RuntimeConfiguration.Load(path);
                RuntimeConfiguration.Register("game", "fps", 120);
                Check(RuntimeConfiguration.Value("game", "fps", 60) == 120, "legacy fallback changed");
                File.WriteAllText(path, "{\"version\":1,\"game\":{\"fps\":144,\"invalid\":\"no\"}}");
                RuntimeConfiguration.Load(path);
                RuntimeConfiguration.Register("game", "fps", 120);
                Check(RuntimeConfiguration.Value("game", "fps", 60) == 144, "JSON must win");
                Check(RuntimeConfiguration.Value("game", "invalid", 24) == 24, "invalid config must preserve fallback");
                RuntimeConfiguration.Set("game", "fps", 90);
                Check(RuntimeConfiguration.Value("game", "fps", 60) == 90, "session setter ignored");
                RuntimeConfiguration.Export(Path.Combine(directory, "export.json"));
                bool refused = false;
                try
                {
                    RuntimeConfiguration.Export(path);
                }
                catch (IOException)
                {
                    refused = true;
                }

                Check(refused, "must not overwrite configuration");
                Console.WriteLine("PASS configuration fallback / JSON / setters / invalid input / export protection");
                return 0;
            }

            if (args[0] == "ragprobe")
            {
                AgentConfig.BrainEndpoint = args[1];
                AgentConfig.BrainAutoStart = false;
                string origin = new Uri(args[2]).GetLeftPart(UriPartial.Authority);
                AgentConfig.QdrantUrl = origin + "/qdrant";
                AgentConfig.EmbedEndpoint = origin + "/api/embed";
                AgentConfig.RagCollection = "nso_test_" + Guid.NewGuid().ToString("N");
                AgentConfig.RagEnabled = true;
                AgentConfig.RagGameKnowledge = true;
                GameKnowledge.Source = null;
                string root = Path.GetFullPath("Assets/StreamingAssets/NsoStatic");
                var world = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(root, "tables.txt")), Path.Combine(root, "Data"));
                try
                {
                    AgentRag.Start(world);
                    AgentRag.Remember("aisample", "fact:contract", "fact", "personal contract memory");
                    for (int i = 0; i < 40 && System.Threading.Interlocked.Read(ref AgentRag.Stored) < 2; i++)
                        System.Threading.Thread.Sleep(200);
                    var recalled = AgentRag.Recall("aisample", "contract memory", 8);
                    Check(recalled != null && recalled.Contains("[fact] personal contract memory"), "personal RAG blocked by absent wiki export");
                    Check(GameKnowledge.LastError == "no wiki data source", "missing knowledge source must remain diagnostic");
                    Console.WriteLine("PASS C# outbox -> mock embedding/Qdrant -> personal recall without wiki source");
                }
                finally
                {
                    AgentRag.Stop();
                }

                System.Threading.Thread.Sleep(250);
                return 0;
            }

            if (args[0] == "probe")
            {
                AgentConfig.BrainEndpoint = args[1];
                AgentConfig.BrainAutoStart = args[3] == "auto";
                AgentConfig.RagEnabled = false;
                AgentConfig.LlmEndpoint = args[2];
                AgentConfig.LlmTimeoutSeconds = 2;
                BrainClient.Start();
                var fixture = JsonDocument.Parse(File.ReadAllText("Tools/Brain/tests/fixtures/prompts.json"));
                var s = JsonSerializer.Deserialize<AgentSnapshot>(fixture.RootElement.GetProperty("cases")[0].GetProperty("snapshot").GetRawText(), Options);
                string prompt = LlmPlanner.BuildPrompt(s);
                Check(prompt == fixture.RootElement.GetProperty("cases")[0].GetProperty("expected").GetString(), "C# snapshot -> Python prompt mismatch");
                var decision = new LlmPlanner().Decide(s);
                Check(decision.Source == "llm" && decision.Steps.Count == 1 && decision.Steps[0].Goal == GoalType.Move && decision.Steps[0].X == 123, "C#/Python decision mismatch");
                Console.WriteLine("PASS real HTTP C# -> Python -> mock LLM -> C# decision");
                if (AgentConfig.BrainAutoStart)
                {
                    long version = BrainClient.ConnectionVersion;
                    var child = (System.Diagnostics.Process)typeof(BrainClient).GetField("ownedProcess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
                    child.Kill();
                    child.WaitForExit(5000);
                    try
                    {
                        LlmPlanner.BuildPrompt(s);
                    }
                    catch (Exception)
                    {
                    }

                    Check(LlmPlanner.BuildPrompt(s) == prompt && BrainClient.ConnectionVersion > version, "Brain did not recover after owned-process loss");
                    Console.WriteLine("PASS owned Brain restart and session recovery");
                }

                BrainClient.Stop();
                System.Threading.Thread.Sleep(250); // let the owned cleanup finish before the host exits
                return 0;
            }

            throw new ArgumentException("export <file> | config | probe <brain-url> <mock-llm-url> <external|auto>");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            BrainClient.Stop();
        }
    }
}
