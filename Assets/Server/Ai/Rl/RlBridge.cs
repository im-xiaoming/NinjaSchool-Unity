using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NsoServer.Data;

namespace NsoServer.Ai.Rl
{
    /// <summary>Bounded disk IPC and trainer lifecycle. Only this worker touches learning files/processes.</summary>
    public sealed class RlBridge : IDisposable
    {
        public readonly string Root;
        private readonly Queue<object> queue = new Queue<object>();
        private readonly object gate = new object();
        private readonly Thread worker;
        private volatile bool running = true;
        private Process trainer;
        private readonly string script;
        private readonly string rulesDirectory;
        private readonly string staticDirectory;
        public volatile string Error = "", TrainerStatus = "starting";
        private sealed class Release { public RlPolicy Strategy, Tactical; }
        private volatile Release release;
        public RlPolicy Strategy { get { var r = release; return r != null ? r.Strategy : null; } }
        public RlPolicy Tactical { get { var r = release; return r != null ? r.Tactical : null; } }
        public volatile string Candidate = "", Evaluation = "not evaluated";
        public volatile Dictionary<string, object> TrainerDetails;
        public volatile Dictionary<string, object> EvaluationDetails;
        public volatile bool Ready;
        public long Written, Dropped;
        public volatile bool ApplyRequested, RollbackRequested;
        public volatile float FramesPerSecond = 120;
        public long FrameSampleAt;
        private string loadedVersion = "";
        private string loadedBalance = "";
        private long trainerAttempt;
        private readonly bool externalTrainer;
        private readonly string stopFile;

        private bool pollFailed;
        public RlBridge(string root, string trainerScript, string rulesDirectory, string staticDirectory, bool external = false)
        {
            Root = root; script = trainerScript; this.rulesDirectory = rulesDirectory; this.staticDirectory = staticDirectory; externalTrainer = external;
            stopFile = Path.Combine(root, "stop-" + Guid.NewGuid().ToString("N"));
            worker = new Thread(Work) { Name = "nso-learning-io", IsBackground = true, Priority = ThreadPriority.BelowNormal };
            worker.Start();
        }
        public void Enqueue(object sample)
        {
            lock (gate) { if (queue.Count >= 2048) { Interlocked.Increment(ref Dropped); return; } queue.Enqueue(sample); Monitor.Pulse(gate); }
        }
        public static string Serialize(object o) { var w = new JsonWriter(); w.Value(o); return w.ToString(); }
        public static void Atomic(string path, object data)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, Serialize(data), new System.Text.UTF8Encoding(false));
            // Windows: File.Replace fails ("Unable to remove the file to be replaced") while the Python trainer has the
            // target open for reading. Retry briefly; the reader closes it within milliseconds.
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                    return;
                }
                catch (IOException) when (attempt < 20) { Thread.Sleep(25); }
                catch (UnauthorizedAccessException) when (attempt < 20) { Thread.Sleep(25); }
            }
        }
        private void Work()
        {
            StreamWriter writer = null; long bytes = 0; int chunk = 0; var session = Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Root);
                Atomic(Path.Combine(Root, "environment.json"), new Dictionary<string, object> { { "rules", rulesDirectory }, { "static", staticDirectory },
                    { "ownerSeconds", NsoServer.World.GroundLootPolicy.OwnerProtectionMs / 1000 }, { "lifetimeSeconds", NsoServer.World.GroundLootPolicy.LifetimeMs / 1000 },
                    { "moveStepPx", AgentConfig.MoveStepPx }, { "motionRevision", NsoServer.World.TileMotion.Revision } });
                // Config survives Play/restarts. Invalid settings leave the conservative defaults active.
                string settings = Path.Combine(Root, "settings.json");
                if (File.Exists(settings))
                    try { RlRuntime.RestoreSettings(File.ReadAllText(settings)); }
                    catch (Exception ex) { Error = "Invalid settings; defaults retained: " + ex.Message; }
                Ready = true;
                long lastPoll = 0;
                while (running || QueueCount() > 0)
                {
                    object item = null;
                    lock (gate) { if (queue.Count > 0) item = queue.Dequeue(); else if (running) Monitor.Wait(gate, 100); }
                    if (item != null)
                    {
                        if (writer == null) { writer = new StreamWriter(Path.Combine(Root, "experience-" + session + "-" + chunk++ + ".jsonl"), false, new System.Text.UTF8Encoding(false)); bytes = 0; }
                        string line = Serialize(item); writer.WriteLine(line); bytes += line.Length; Interlocked.Increment(ref Written);
                        if (bytes >= 2000000) { writer.Dispose(); writer = null; }
                    }
                    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (now - lastPoll < 1000 || !running) continue;
                    lastPoll = now; if (writer != null) writer.Flush();
                    try { Poll(now); if (pollFailed) { Error = ""; pollFailed = false; } }   // a transient file error clears itself
                    catch (Exception ex) { Error = ex.Message; pollFailed = true; }
                }
            }
            catch (Exception ex) { Error = "Learning IO: " + ex.Message; }
            finally
            {
                if (writer != null) writer.Dispose();
                if (trainer != null) trainer.Dispose();
                // The Python supervisor observes stop, cancels its own evaluator and exits; no process-wide kills.
            }
        }
        private int QueueCount() { lock (gate) return queue.Count; }
        private string BalanceHash()
        {
            using (var bytes = new MemoryStream())
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                foreach (string name in new[] { "game-rules.json", "combat-rules.json", "game-balance.json", "monster-tuning.json" })
                {
                    byte[] key = System.Text.Encoding.UTF8.GetBytes(name); bytes.Write(key, 0, key.Length);
                    string path = Path.Combine(rulesDirectory, name);
                    byte[] data = File.Exists(path) ? File.ReadAllBytes(path) : System.Text.Encoding.UTF8.GetBytes("<defaults>");
                    bytes.Write(data, 0, data.Length);
                }
                byte[] timing = System.Text.Encoding.UTF8.GetBytes("|ground:" + NsoServer.World.GroundLootPolicy.OwnerProtectionMs + ":" + NsoServer.World.GroundLootPolicy.LifetimeMs);
                bytes.Write(timing, 0, timing.Length);
                byte[] motion = System.Text.Encoding.UTF8.GetBytes("|motion:" + NsoServer.World.TileMotion.Revision + ":" + AgentConfig.MoveStepPx);
                bytes.Write(motion, 0, motion.Length);
                return BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
        // Read on the IO worker only. Publish a detached snapshot; web/game ticks never read learning files.
        private void ReadEvaluationDetails(string balance)
        {
            var view = new Dictionary<string, object> { { "version", Candidate }, { "completedRuns", 0 }, { "totalRuns", 6 } };
            try
            {
                string version = Candidate;
                if (string.IsNullOrEmpty(version)) { EvaluationDetails = view; return; }
                if (version.Length > 100 || version.Contains("..") || version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    version.Contains("/") || version.Contains("\\")) throw new FormatException("Invalid candidate version");
                string folder = Path.Combine(Root, "models", version);
                if (Directory.Exists(folder))
                    view["completedRuns"] = Directory.GetFiles(folder, "trial-*-rules.json").Length + Directory.GetFiles(folder, "trial-*-candidate.json").Length;
                string path = Path.Combine(folder, "evaluation.json");
                if (File.Exists(path))
                {
                    if (new FileInfo(path).Length > 1048576) throw new FormatException("Evaluation report exceeds 1 MiB");
                    var report = Json.Parse(File.ReadAllText(path)) as Dictionary<string, object>;
                    if (report == null || !report.ContainsKey("version") || Convert.ToString(report["version"]) != version)
                        throw new FormatException("Evaluation report version mismatch");
                    view["report"] = report;
                    view["matchesBalance"] = report.ContainsKey("balanceHash") && Convert.ToString(report["balanceHash"]) == balance;
                    if (report.ContainsKey("trials")) view["totalRuns"] = Convert.ToInt32(report["trials"]) * 2;
                }
            }
            catch (Exception ex) { view["error"] = ex.Message; } // diagnostic failure must not interrupt collection/promotion
            EvaluationDetails = view;
        }

        private void Poll(long now)
        {
            var settings = RlRuntime.SettingsObject();
            Atomic(Path.Combine(Root, "settings.json"), settings);
            Atomic(Path.Combine(Root, "budget.json"), new Dictionary<string, object> { { "fps", FramesPerSecond }, { "at", Interlocked.Read(ref FrameSampleAt) }, { "learning", RlRuntime.Settings.Learning } });
            string statusPath = Path.Combine(Root, "trainer-status.json");
            if (File.Exists(statusPath))
            {
                var d = Json.Parse(File.ReadAllText(statusPath)) as Dictionary<string, object>;
                if (d != null) { TrainerDetails = d; TrainerStatus = Convert.ToString(d["status"]); Candidate = Convert.ToString(d["candidate"]); Evaluation = Convert.ToString(d["evaluation"]); }
            }
            string active = Path.Combine(Root, "active.json");
            string balance = BalanceHash();
            ReadEvaluationDetails(balance);
            if (release != null && balance != loadedBalance) { release = null; loadedVersion = ""; }
            if (File.Exists(active))
            {
                var d = Json.Parse(File.ReadAllText(active)) as Dictionary<string, object>;
                string version = Convert.ToString(d["version"]);
                if (version != loadedVersion)
                {
                    if (version.Length == 0 || version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || version.Contains("..")) throw new FormatException("Invalid model version");
                    // Only evaluator-approved immutable releases may be loaded; never read a half-written model.
                    string folder = Path.Combine(Root, "models", version);
                    var report = Json.Parse(File.ReadAllText(Path.Combine(folder, "evaluation.json"))) as Dictionary<string, object>;
                    if (report == null || !report.ContainsKey("passed") || !Convert.ToBoolean(report["passed"]) || Convert.ToString(report["version"]) != version) throw new FormatException("Model has no passing evaluation");
                    if (!report.ContainsKey("balanceHash") || Convert.ToString(report["balanceHash"]) != balance) throw new FormatException("Balance changed; using teacher until a matching evaluation passes");
                    var hashes = report["hashes"] as Dictionary<string, object>;
                    if (hashes == null) throw new FormatException("Missing evaluated policy hashes");
                    foreach (var entry in hashes)
                    {
                        if (entry.Key != "strategy.json" && entry.Key != "tactical.json") throw new FormatException("Unexpected policy hash");
                        using (var sha = System.Security.Cryptography.SHA256.Create())
                        using (var file = File.OpenRead(Path.Combine(folder, entry.Key)))
                            if (BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant() != Convert.ToString(entry.Value)) throw new FormatException("Policy changed after evaluation");
                    }
                    if (!hashes.ContainsKey("strategy.json") || !hashes.ContainsKey("tactical.json")) throw new FormatException("Incomplete evaluated release");
                    var strategy = RlPolicy.Load(Path.Combine(folder, "strategy.json"), "strategy");
                    RlPolicy tactical = File.Exists(Path.Combine(folder, "tactical.json")) ? RlPolicy.Load(Path.Combine(folder, "tactical.json"), "tactical") : null;
                    if (strategy.Version != version || (tactical != null && tactical.Version != version)) throw new FormatException("Mixed policy versions");
                    release = new Release { Strategy = strategy, Tactical = tactical }; loadedVersion = version; loadedBalance = balance; Error = "";
                }
            }
            if (ApplyRequested || RollbackRequested)
            {
                Atomic(Path.Combine(Root, "request.json"), new Dictionary<string, object> { { "command", RollbackRequested ? "rollback" : "apply" }, { "id", Guid.NewGuid().ToString("N") } });
                ApplyRequested = RollbackRequested = false;
            }
            if (externalTrainer) return; // supervisor stays available for apply/rollback while optimisation is paused
            if (trainer != null && !trainer.HasExited) return;
            if (trainer != null) { TrainerStatus = "trainer exited (" + trainer.ExitCode + "); see trainer.log"; trainer.Dispose(); trainer = null; }
            if (now - trainerAttempt < 60000) return;
            trainerAttempt = now;
            if (string.IsNullOrEmpty(script) || !File.Exists(script)) { TrainerStatus = "trainer.py not configured; rules collecting demonstrations"; return; }
            var pi = new ProcessStartInfo(RlRuntime.PythonExecutable, "\"" + script + "\" --root \"" + Root + "\" --stop-file \"" + stopFile + "\"")
            { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(script) };
            trainer = Process.Start(pi); TrainerStatus = "trainer starting";
        }
        public void Dispose()
        {
            try { if (Directory.Exists(Root)) File.WriteAllText(stopFile, "stop this trainer only"); } catch (Exception) { }
            running = false; lock (gate) Monitor.PulseAll(gate);
            worker.Join(1500); // disk writer only; never wait for training under the world lock
        }
    }
}
