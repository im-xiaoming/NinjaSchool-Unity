using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NsoServer.Data;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>
    /// Long-term memory of the AI players in a vector database (offline addition, no Java counterpart). What an AI player
    /// learns (AgentKnowledge facts, NPC positions per map) and lives (Memory events), plus the beginner's guide, is
    /// embedded with a local Ollama embedding model and stored in a local Qdrant collection. The LLM prompt then carries
    /// only the facts relevant to the current situation (Recall) instead of everything.
    /// Threads: Remember is called on the game thread and only queues; a background worker embeds and upserts in batches.
    /// Recall runs on the LLM worker thread (synchronous HTTP with a short timeout). Nothing leaves the PC (local URLs only).
    /// When Qdrant or the embedding model is unreachable, Available is false and the prompt falls back to the in-context
    /// knowledge as before.
    /// </summary>
    public static class AgentRag
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private struct Item { public string Agent, Key, Kind, Text; public long Updated; }

        private static readonly Queue<Item> queue = new Queue<Item>();
        private static readonly Dictionary<string, string> stored = new Dictionary<string, string>();   // point id -> text (dedupe)
        private static readonly object sync = new object();
        private static Thread worker;
        private static volatile bool running, collectionReady;
        private static long retryAfter;

        /// <summary>True once the collection exists and the last call succeeded.</summary>
        public static volatile bool Available;
        public static volatile string LastError = "";
        public static long Stored, Recalls, Failures;

        private const int QueueLimit = 20000;   // the whole game knowledge (about 2 000 documents) fits
        private static NsoServer.World.GameWorld knowledgeWorld;

        public static void Start(NsoServer.World.GameWorld world = null)
        {
            if (!AgentConfig.RagEnabled || running) return;
            knowledgeWorld = AgentConfig.RagGameKnowledge ? world : null;
            if (!LlmPlanner.IsLocal(AgentConfig.QdrantUrl) || !LlmPlanner.IsLocal(AgentConfig.EmbedEndpoint))
            { LastError = "RAG endpoints must be on this machine"; return; }
            running = true;
            worker = new Thread(Worker) { IsBackground = true, Name = "nso-agent-rag", Priority = ThreadPriority.BelowNormal };
            worker.Start();
            // the beginner's guide is shared knowledge of every AI player ("*"), one point per line
            var guide = (AgentConfig.LlmGuide ?? "").Split('\n');
            for (int i = 0; i < guide.Length; i++)
                if (guide[i].Trim().Length > 10) Remember("*", "guide:" + i, "guide", guide[i].Trim().TrimStart('-', ' '));
        }

        public static void Stop()
        {
            running = false;
            lock (sync) Monitor.PulseAll(sync);
        }

        /// <summary>Queues a memory (same agent + key replaces the earlier text). Game thread; never blocks.</summary>
        public static void Remember(string agent, string key, string kind, string text)
        {
            if (!running || string.IsNullOrEmpty(text)) return;
            lock (sync)
            {
                if (queue.Count >= QueueLimit) queue.Dequeue();   // the game never waits for the database
                queue.Enqueue(new Item { Agent = agent, Key = key, Kind = kind, Text = text, Updated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
                Monitor.Pulse(sync);
            }
        }

        /// <summary>The memories of this AI player (and the shared guide) closest to the query; null when unavailable.
        /// LLM worker thread.</summary>
        public static List<string> Recall(string agent, string query, int k)
        {
            if (!running || !collectionReady || string.IsNullOrEmpty(query)) return null;
            try
            {
                var vector = Embed(new List<string> { query })[0];
                // the agent's own facts/events, then the shared game knowledge and guide ("*"): searched apart so the
                // ~2 000 game documents never push the agent's own experience out of the prompt
                var list = Search(vector, agent, k);
                foreach (var hit in Search(vector, "*", Math.Max(0, AgentConfig.RagKnowledgeTopK))) if (!list.Contains(hit)) list.Add(hit);
                Interlocked.Increment(ref Recalls);
                Available = true;
                return list;
            }
            catch (Exception ex) { Fail(ex); return null; }
        }

        private static List<string> Search(List<object> vector, string owner, int k)
        {
            var list = new List<string>();
            if (k <= 0) return list;
            {
                var w = new JsonWriter();
                w.BeginObject();
                w.Key("vector"); w.Value(vector);
                w.Prop("limit", Math.Max(1, k));
                w.Prop("with_payload", true);
                w.Key("filter"); w.BeginObject(); w.Key("must"); w.BeginArray();
                w.BeginObject(); w.Prop("key", "agent"); w.Key("match"); w.BeginObject(); w.Prop("value", owner); w.EndObject(); w.EndObject();
                w.EndArray(); w.EndObject();
                w.EndObject();
                var r = Post("/collections/" + AgentConfig.RagCollection + "/points/search", w.ToString(), "POST");
                var result = r["result"] as List<object>;
                if (result != null)
                    foreach (var o in result)
                    {
                        var hit = o as Dictionary<string, object>;
                        var payload = hit != null ? hit["payload"] as Dictionary<string, object> : null;
                        if (payload == null) continue;
                        object kind, text;
                        payload.TryGetValue("kind", out kind); payload.TryGetValue("text", out text);
                        list.Add("[" + kind + "] " + text);
                    }
            }
            return list;
        }

        // ------------------------------------------------------------------ worker

        private static void Worker()
        {
            try { OpenCollection(); } catch (Exception) { }   // Qdrant may still be starting: created on the first upsert
            if (knowledgeWorld != null) GameKnowledge.Inject(knowledgeWorld);
            while (running)
            {
                var batch = new List<Item>();
                lock (sync)
                {
                    while (running && queue.Count == 0) Monitor.Wait(sync, 2000);
                    if (!running) break;
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < retryAfter) { Monitor.Wait(sync, 2000); continue; }
                    while (queue.Count > 0 && batch.Count < 32)
                    {
                        var it = queue.Dequeue();
                        string id = PointId(it.Agent, it.Key), old;
                        if (stored.TryGetValue(id, out old) && old == it.Text) continue;   // unchanged: no new embedding
                        batch.Add(it);
                    }
                }
                if (batch.Count == 0) continue;
                try
                {
                    // points already in Qdrant with the same text (an earlier session): not embedded again
                    if (collectionReady) batch = Unchanged(batch);
                    if (batch.Count == 0) continue;
                    var texts = new List<string>();
                    foreach (var it in batch) texts.Add(it.Text);
                    var vectors = Embed(texts);
                    if (!collectionReady) EnsureCollection(vectors[0].Count);
                    var w = new JsonWriter();
                    w.BeginObject(); w.Key("points"); w.BeginArray();
                    for (int i = 0; i < batch.Count; i++)
                    {
                        var it = batch[i];
                        w.BeginObject();
                        w.Prop("id", PointId(it.Agent, it.Key));
                        w.Key("vector"); w.Value(vectors[i]);
                        w.Key("payload"); w.BeginObject();
                        w.Prop("agent", it.Agent); w.Prop("key", it.Key); w.Prop("kind", it.Kind); w.Prop("text", it.Text); w.Prop("updated", it.Updated);
                        w.EndObject();
                        w.EndObject();
                    }
                    w.EndArray(); w.EndObject();
                    Post("/collections/" + AgentConfig.RagCollection + "/points?wait=false", w.ToString(), "PUT");
                    lock (sync) foreach (var it in batch) stored[PointId(it.Agent, it.Key)] = it.Text;
                    Interlocked.Add(ref Stored, batch.Count);
                    Available = true;
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    lock (sync) foreach (var it in batch) if (queue.Count < QueueLimit) queue.Enqueue(it);   // retry later
                    retryAfter = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 30000;
                }
            }
        }

        private static void OpenCollection()
        {
            Get("/collections/" + AgentConfig.RagCollection);
            collectionReady = true;
        }

        // drops the items whose point already holds the same text (and remembers them as stored)
        private static List<Item> Unchanged(List<Item> batch)
        {
            var w = new JsonWriter();
            w.BeginObject(); w.Key("ids"); w.BeginArray();
            foreach (var it in batch) w.Value(PointId(it.Agent, it.Key));
            w.EndArray(); w.Prop("with_payload", true); w.Prop("with_vector", false); w.EndObject();
            var r = Post("/collections/" + AgentConfig.RagCollection + "/points", w.ToString(), "POST");
            var have = new Dictionary<string, string>();
            var result = r["result"] as List<object>;
            if (result != null)
                foreach (var o in result)
                {
                    var p = o as Dictionary<string, object>;
                    var payload = p != null ? p["payload"] as Dictionary<string, object> : null;
                    object text;
                    if (payload != null && payload.TryGetValue("text", out text)) have[Convert.ToString(p["id"])] = Convert.ToString(text);
                }
            var left = new List<Item>();
            lock (sync)
                foreach (var it in batch)
                {
                    string id = PointId(it.Agent, it.Key), old;
                    if (have.TryGetValue(id, out old) && old == it.Text) stored[id] = it.Text; else left.Add(it);
                }
            return left;
        }

        private static void EnsureCollection(int size)
        {
            try { Get("/collections/" + AgentConfig.RagCollection); }
            catch (Exception)
            {
                Post("/collections/" + AgentConfig.RagCollection,
                    "{\"vectors\":{\"size\":" + size + ",\"distance\":\"Cosine\"}}", "PUT");
                Post("/collections/" + AgentConfig.RagCollection + "/index", "{\"field_name\":\"agent\",\"field_schema\":\"keyword\"}", "PUT");
            }
            collectionReady = true;
        }

        // Ollama /api/embed: {"model", "input": [..]} -> {"embeddings": [[..], ..]}
        private static List<List<object>> Embed(List<string> texts)
        {
            var w = new JsonWriter();
            w.BeginObject(); w.Prop("model", AgentConfig.EmbedModel); w.Key("input"); w.Value(texts); w.EndObject();
            var req = new HttpRequestMessage(HttpMethod.Post, AgentConfig.EmbedEndpoint) { Content = new StringContent(w.ToString(), Encoding.UTF8, "application/json") };
            using (var resp = Http.SendAsync(req).GetAwaiter().GetResult())
            {
                string text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("embedding HTTP " + (int)resp.StatusCode + ": " + LlmPlanner.Short(text, 120));
                var d = Json.Parse(text) as Dictionary<string, object>;
                var list = new List<List<object>>();
                foreach (var e in (List<object>)d["embeddings"]) list.Add((List<object>)e);
                if (list.Count != texts.Count) throw new InvalidOperationException("embedding count mismatch");
                return list;
            }
        }

        private static Dictionary<string, object> Get(string path) { return Send(HttpMethod.Get, path, null); }
        private static Dictionary<string, object> Post(string path, string body, string method)
        { return Send(new HttpMethod(method), path, body); }

        private static Dictionary<string, object> Send(HttpMethod method, string path, string body)
        {
            var req = new HttpRequestMessage(method, AgentConfig.QdrantUrl.TrimEnd('/') + path);
            if (body != null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using (var resp = Http.SendAsync(req).GetAwaiter().GetResult())
            {
                string text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("qdrant HTTP " + (int)resp.StatusCode + ": " + LlmPlanner.Short(text, 160));
                return Json.Parse(text) as Dictionary<string, object>;
            }
        }

        private static void Fail(Exception ex)
        {
            Available = false;
            Interlocked.Increment(ref Failures);
            LastError = DateTime.Now.ToString("HH:mm:ss") + " " + ex.GetType().Name + ": " + ex.Message;
        }

        // deterministic UUID per (agent, key): the same fact overwrites its point
        private static string PointId(string agent, string key)
        {
            using (var md5 = MD5.Create())
            {
                var h = md5.ComputeHash(Encoding.UTF8.GetBytes(agent + "|" + key));
                return new Guid(h).ToString();
            }
        }
    }
}
