using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using NsoServer.Data;

namespace NsoServer.Ai
{
    /// <summary>
    /// Offline Brain transport. Workers send detached snapshots only; the server tick remains the only game authority.
    /// No network/disk/process operation in this class is invoked while holding GameWorld.SyncRoot.
    /// </summary>
    public static class BrainClient
    {
        static BrainClient()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                try
                {
                    var child = ownedProcess;
                    if (child != null && !child.HasExited)
                        child.Kill();
                }
                catch (Exception)
                {
                }
            };
        }

        public const int ContractVersion = 1;
        public static string PythonExecutable = "", Script = "";
        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        private static readonly object gate = new object ();
        private static readonly object connectionGate = new object ();
        private static bool active = true;
        private static Process ownedProcess;
        private static string sessionId;
        private static long requestId;
        private static long connectionVersion;
        public static long ConnectionVersion
        {
            get
            {
                return Interlocked.Read(ref connectionVersion);
            }
        }

        private static bool connected;
        private static int generation;
        public static string LastError = "";
        // Called once before workers start. Invalidate late HTTP responses from an earlier Play session.
        public static void ConfigureDevelopmentPaths()
        {
            if (string.IsNullOrEmpty(Script))
                Script = Path.GetFullPath("Tools/Brain/serve.py");
            if (string.IsNullOrEmpty(PythonExecutable))
                PythonExecutable = Path.GetFullPath("Tools/Brain/.venv/Scripts/python.exe");
        }

        public static string Resource(string name)
        {
            ConfigureDevelopmentPaths();
            return Path.Combine(Path.GetDirectoryName(Script), "nso_brain", "resources", name);
        }

        public static void Start()
        {
            lock (gate)
            {
                active = true;
                generation++;
                sessionId = Guid.NewGuid().ToString();
                connected = false;
                LastError = "";
            }
        }

        private static Dictionary<string, object> Send(string method, string path, object body, int timeout = 15, string endpoint = null)
        {
            endpoint = endpoint ?? AgentConfig.BrainEndpoint;
            if (!LlmPlanner.IsLocal(endpoint))
                throw new InvalidOperationException("Brain endpoint must be loopback");
            var request = new HttpRequestMessage(new HttpMethod(method), endpoint.TrimEnd('/') + path);
            if (body != null)
            {
                var writer = new JsonWriter();
                writer.Value(body);
                request.Content = new StringContent(writer.ToString(), Encoding.UTF8, "application/json");
            }

            using (request)
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(System.Math.Max(1, timeout))))
            using (var response = Http.SendAsync(request, cts.Token).GetAwaiter().GetResult())
            {
                string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Brain HTTP " + (int)response.StatusCode + ": " + LlmPlanner.Short(text, 300));
                return Json.Parse(text) as Dictionary<string, object> ?? throw new FormatException("invalid Brain response");
            }
        }

        public static Dictionary<string, object> Settings()
        {
            var settings = new Dictionary<string, object>();
            // Configuration fields are primitives; never serialize game objects or provider secrets.
            foreach (var field in typeof(AgentConfig).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (field.Name != "LlmApiKey")
                    settings[field.Name] = field.GetValue(null);
            settings["GroundOwnerSeconds"] = World.GroundLootPolicy.OwnerProtectionMs / 1000;
            settings["GroundLifetimeSeconds"] = World.GroundLootPolicy.LifetimeMs / 1000;
            return settings;
        }

        private static void EnsureSession()
        {
            // Never hold the lifecycle gate across HTTP or process startup: Stop runs under the world lock.
            lock (connectionGate)
            {
                string id;
                int epoch;
                Process process;
                lock (gate)
                {
                    if (!active)
                        throw new OperationCanceledException("Brain session ended");
                    if (connected)
                        return;
                    if (sessionId == null)
                        sessionId = Guid.NewGuid().ToString();
                    id = sessionId;
                    epoch = generation;
                    process = ownedProcess;
                }

                ConfigureDevelopmentPaths();
                bool ready = false;
                try
                {
                    VerifyHealth();
                    ready = true;
                }
                catch (Exception)
                {
                }

                if (!ready && AgentConfig.BrainAutoStart)
                {
                    if (process == null || process.HasExited)
                    {
                        if (!File.Exists(PythonExecutable) || !File.Exists(Script))
                            throw new FileNotFoundException("Brain environment missing; run uv sync --project Tools/Brain --locked");
                        var uri = new Uri(AgentConfig.BrainEndpoint);
                        if (uri.Host != "127.0.0.1")
                            throw new InvalidOperationException("Auto-start requires a 127.0.0.1 Brain endpoint");
                        var start = new ProcessStartInfo(PythonExecutable, "\"" + Script + "\" --port " + uri.Port)
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            WorkingDirectory = Path.GetDirectoryName(Script)
                        };
                        start.EnvironmentVariables.Remove("NSO_DEVWIKI_LISTENER");
                        process = Process.Start(start);
                        lock (gate)
                        {
                            if (!active || epoch != generation)
                            {
                                if (!process.HasExited)
                                    process.Kill();
                                process.Dispose();
                                throw new OperationCanceledException("Brain session ended");
                            }

                            ownedProcess = process;
                        }
                    }

                    var until = Stopwatch.StartNew();
                    while (until.ElapsedMilliseconds < 10000)
                    {
                        lock (gate)
                            if (!active || epoch != generation)
                                throw new OperationCanceledException("Brain session ended");
                        try
                        {
                            VerifyHealth();
                            ready = true;
                            break;
                        }
                        catch (Exception)
                        {
                        }

                        if (process.HasExited)
                            break;
                        Thread.Sleep(100);
                    }
                }

                if (!ready)
                    throw new InvalidOperationException("Brain unavailable at " + AgentConfig.BrainEndpoint);
                var result = Send("POST", "/v1/sessions", new Dictionary<string, object> { { "contractVersion", ContractVersion }, { "sessionId", id }, { "settings", Settings() } });
                if (Convert.ToString(result["sessionId"]) != id)
                    throw new FormatException("Brain session mismatch");
                lock (gate)
                {
                    if (!active || epoch != generation)
                        throw new OperationCanceledException("Brain session ended");
                    connected = true;
                    Interlocked.Increment(ref connectionVersion);
                }
            }
        }

        private static void VerifyHealth()
        {
            var health = Send("GET", "/health", null, 1);
            if (Convert.ToString(health["service"]) != "nso-brain" || Json.Long(health["contractVersion"]) != ContractVersion)
                throw new InvalidOperationException("Incompatible service on Brain port");
        }

        public static Dictionary<string, object> Call(string route, object body, int timeout = 15)
        {
            EnsureSession();
            string id;
            int epoch;
            lock (gate)
            {
                id = sessionId;
                epoch = generation;
            }

            try
            {
                var result = Send("POST", "/v1/sessions/" + id + "/" + route, body, timeout);
                lock (gate)
                    if (epoch != generation)
                        throw new OperationCanceledException("Brain session ended");
                object returnedSession;
                if (result.TryGetValue("sessionId", out returnedSession) && Convert.ToString(returnedSession) != id)
                    throw new FormatException("Brain response session mismatch");
                object rag;
                if (result.TryGetValue("rag", out rag) && rag is Dictionary<string, object> status)
                    AgentRag.UpdateStatus(status);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                lock (gate)
                    if (epoch == generation)
                        connected = false;
                throw;
            }
        }

        public static Dictionary<string, object> Snapshot(AgentSnapshot snapshot)
        {
            var data = new Dictionary<string, object>();
            foreach (var field in typeof(AgentSnapshot).GetFields())
            {
                object value = field.GetValue(snapshot);
                if (field.FieldType == typeof(string) && value == null)
                    value = "";
                if (value is List<HuntOption> hunts)
                {
                    var list = new List<object>();
                    foreach (var hunt in hunts)
                    {
                        var item = new Dictionary<string, object>();
                        foreach (var f in typeof(HuntOption).GetFields())
                            item[f.Name] = f.GetValue(hunt) ?? "";
                        list.Add(item);
                    }

                    value = list;
                }

                if (value is Dictionary<int, string> maps)
                {
                    var names = new Dictionary<string, object>();
                    foreach (var pair in maps)
                        names[pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)] = pair.Value ?? "";
                    value = names;
                }

                data[field.Name] = value;
            }

            return data;
        }

        public static Decision Decide(AgentSnapshot snapshot)
        {
            long request = Interlocked.Increment(ref requestId);
            var result = Call("decide", new Dictionary<string, object> { { "requestId", request }, { "snapshot", Snapshot(snapshot) } }, AgentConfig.LlmTimeoutSeconds + 15);
            if (Json.Long(result["requestId"]) != request || Json.Long(result["contractVersion"]) != ContractVersion)
                throw new FormatException("Brain request mismatch");
            object error;
            if (result.TryGetValue("error", out error))
            {
                if (Convert.ToString(result["errorKind"]) == "format")
                    throw new LlmInvalidReplyException(Convert.ToString(error), Convert.ToString(result["raw"]));
                throw new InvalidOperationException(Convert.ToString(error));
            }

            var writer = new JsonWriter();
            writer.Value(result["decision"]);
            string parseError;
            var decision = DecisionParser.Parse(writer.ToString(), out parseError);
            if (decision == null)
                throw new LlmInvalidReplyException(parseError, Convert.ToString(result["raw"]));
            decision.Raw = Convert.ToString(result["raw"]);
            decision.Source = "llm";
            decision.Summary = result["summary"] as string;
            decision.SummaryCut = (int)Json.Long(result["summaryCut"]);
            return decision;
        }

        public static void Stop()
        {
            // Detach immediately under the world lock; cleanup waits only on this owned background thread.
            string id;
            Process process;
            string endpoint = AgentConfig.BrainEndpoint;
            lock (gate)
            {
                active = false;
                generation++;
                id = sessionId;
                sessionId = null;
                connected = false;
                process = ownedProcess;
                ownedProcess = null;
            }

            try
            {
                if (process != null && !process.HasExited)
                    process.Kill();
            }
            catch (Exception)
            {
            }

            new Thread(() =>
            {
                try
                {
                    if (id != null)
                        Send("DELETE", "/v1/sessions/" + id, null, 2, endpoint);
                }
                catch (Exception)
                {
                }

                try
                {
                    if (process != null)
                    {
                        if (!process.HasExited)
                            process.Kill();
                        process.Dispose();
                    }
                }
                catch (Exception)
                {
                }
            })
            {
                IsBackground = true,
                Name = "nso-brain-stop"
            }.Start();
        }
    }
}
