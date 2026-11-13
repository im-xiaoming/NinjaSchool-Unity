using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NsoServer;
using NsoServer.Ai;
using NsoServer.Ai.Rl;
using NsoServer.Data;
using NsoServer.World;

// Explicit standalone tools only. Never called by compile checks. No production save is opened.
public static class Program
{
    private static string Arg(string[] args, string name, string fallback = "")
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
    }

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
                throw new ArgumentException("Use parity or evaluate");
            if (args[0] == "parity")
            {
                var p = RlPolicy.Load(Arg(args, "--model"), Arg(args, "--domain", "strategy"));
                var d = Json.Parse(File.ReadAllText(Arg(args, "--observation"))) as Dictionary<string, object>;
                var o = new Observation();
                var obs = (List<object>)d["obs"];
                var mask = (List<object>)d["mask"];
                for (int i = 0; i < o.Values.Length; i++)
                    o.Values[i] = Convert.ToSingle(obs[i]);
                for (int i = 0; i < o.ActionMask.Length; i++)
                    o.ActionMask[i] = Convert.ToBoolean(mask[i]);
                Console.WriteLine(RlBridge.Serialize(new Dictionary<string, object> { { "probabilities", p.Probabilities(o) }, { "value", p.Value(o.Values) } }));
                return 0;
            }

            if (args[0] != "evaluate")
                throw new ArgumentException("Unknown command");
            int seconds = int.Parse(Arg(args, "--seconds", "600"));
            if (seconds < 60 || seconds > 3600)
                throw new ArgumentException("Evaluation duration must be 60..3600 seconds");
            string output = Path.GetFullPath(Arg(args, "--output"));
            string scratch = Path.Combine(Path.GetTempPath(), "nso-rl-eval-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            var config = new ServerConfig
            {
                DataDirectory = scratch,
                UseSqlite = false
            };
            string rules = Arg(args, "--rules");
            // Copy only balance/config, never accounts or characters. Both arms receive the same files.
            foreach (string file in new[]
            {
                "game-rules.json",
                "combat-rules.json",
                "game-balance.json",
                "monster-tuning.json"
            }

            )
                if (rules.Length > 0 && File.Exists(Path.Combine(rules, file)))
                    File.Copy(Path.Combine(rules, file), Path.Combine(scratch, file));
            var server = new GameServer(config);
            GroundLootPolicy.Configure(int.Parse(Arg(args, "--owner-seconds", "30")), int.Parse(Arg(args, "--lifetime-seconds", "60")));
            AgentConfig.MoveStepPx = Math.Clamp(int.Parse(Arg(args, "--move-step-px", "60")), 12, 120);
            string data = Path.GetFullPath(Arg(args, "--static"));
            var tables = TableFile.Load(Path.Combine(data, "tables.txt"));
            server.Static = new NsoServer.Net.FileStaticData(data);
            server.World = new GameWorld(tables, Path.Combine(data, "Data"));
            AgentConfig.Enabled = true;
            AgentConfig.AgentCount = 13;
            AgentConfig.LlmEnabled = false;
            AgentConfig.RagEnabled = false;
            AgentConfig.BrainAutoStart = false;
            AgentConfig.LoginSpacingSeconds = 0;
            RlRuntime.ExternalTrainer = true;
            RlRuntime.EvaluationModelFolder = Arg(args, "--model");
            RlRuntime.LearningRootOverride = Path.Combine(scratch, "learning");
            RlRuntime.Settings = new RlSettings
            {
                Mode = PolicyMode.Rules,
                Learning = false
            };
            server.Start();
            AgentManager.Start(server);
            try
            {
                Thread.Sleep(seconds * 1000); // real game time; never accelerate cooldowns or effect clocks
                Dictionary<string, object> result;
                lock (server.World.SyncRoot)
                    result = AgentManager.DebugObject();
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                RlBridge.Atomic(output, result);
            }
            finally
            {
                AgentManager.Stop();
                server.Stop();
            }

            // Keep isolated evidence for diagnosis; no recursive deletion and no access to the real save.
            Console.WriteLine("Evaluation output: " + output + "; scratch: " + scratch);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
