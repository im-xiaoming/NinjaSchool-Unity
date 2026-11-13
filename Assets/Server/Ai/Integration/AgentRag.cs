using System;
using System.Collections.Generic;
using System.Threading;

namespace NsoServer.Ai
{
    /// <summary>
    /// Nonblocking game-side memory outbox. Python owns embedding, Qdrant, deduplication and recall.
    /// Facts/events still belong to C# and its existing save document; this is only a mirror.
    /// </summary>
    public static class AgentRag
    {
        private static readonly object sync = new object ();
        private static readonly Queue<Dictionary<string, object>> queue = new Queue<Dictionary<string, object>>();
        private static CancellationTokenSource stop;
        public static volatile bool Available;
        public static volatile string LastError = "";
        public static long Stored, Recalls, Failures;
        public static void Start(World.GameWorld world = null)
        {
            lock (sync)
            {
                if (stop != null)
                    return;
                BrainClient.Start();
                BrainClient.ConfigureDevelopmentPaths();
                queue.Clear();
                Available = false;
                LastError = "";
                Stored = Recalls = Failures = 0;
                stop = new CancellationTokenSource();
                var token = stop.Token;
                if (!AgentConfig.RagEnabled)
                    return;
                new Thread(() => Worker(world, token))
                {
                    IsBackground = true,
                    Name = "nso-brain-outbox",
                    Priority = ThreadPriority.BelowNormal
                }.Start();
            }
        }

        public static void Stop()
        {
            lock (sync)
            {
                if (stop != null)
                    stop.Cancel();
                stop = null;
                queue.Clear();
                Available = false;
            }

            BrainClient.Stop();
        }

        public static void Remember(string agent, string key, string kind, string text)
        {
            lock (sync)
            {
                if (stop == null || !AgentConfig.RagEnabled || string.IsNullOrEmpty(text))
                    return;
                if (queue.Count >= 20000)
                    queue.Dequeue();
                queue.Enqueue(new Dictionary<string, object> { { "agent", agent }, { "key", key }, { "kind", kind }, { "text", text }, { "updated", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } });
            }
        }

        public static List<string> Recall(string agent, string query, int k)
        {
            try
            {
                var result = BrainClient.Call("recall", new Dictionary<string, object> { { "agent", agent }, { "query", query }, { "count", k } }, 45);
                var values = result["memories"] as List<object>;
                return values == null ? null : values.ConvertAll(Convert.ToString);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Available = false;
                return null;
            }
        }

        internal static void UpdateStatus(Dictionary<string, object> status)
        {
            Available = status["available"] is bool value && value;
            Interlocked.Exchange(ref Stored, Data.Json.Long(status["stored"]));
            Interlocked.Exchange(ref Recalls, Data.Json.Long(status["recalls"]));
            Interlocked.Exchange(ref Failures, Data.Json.Long(status["failures"]));
            LastError = Convert.ToString(status["error"]);
        }

        private static void Worker(World.GameWorld world, CancellationToken token)
        {
            long knowledgeVersion = -1, knowledgeRetryAt = 0;
            if (AgentConfig.RagGameKnowledge && world != null && GameKnowledge.Source == null)
                GameKnowledge.LastError = "no wiki data source";
            while (!token.IsCancellationRequested)
            {
                var batch = new List<object>();
                lock (sync)
                    while (queue.Count > 0 && batch.Count < 32)
                        batch.Add(queue.Dequeue());
                try
                {
                    // Re-send source records after a restarted Brain: Qdrant deduplication avoids re-embedding,
                    // and documents accepted just before a crash are not silently lost forever.
                    if (AgentConfig.RagGameKnowledge && world != null && GameKnowledge.Source != null && knowledgeVersion != BrainClient.ConnectionVersion && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= knowledgeRetryAt)
                    {
                        GameKnowledge.Inject(world);
                        if (GameKnowledge.LastError.Length == 0)
                            knowledgeVersion = BrainClient.ConnectionVersion;
                        else
                            knowledgeRetryAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 30000; // Personal memories still flow when only the wiki export fails.
                    }

                    if (batch.Count > 0)
                        BrainClient.Call("memories/batch", new Dictionary<string, object> { { "items", batch } });
                    else
                        BrainClient.Call("status", new Dictionary<string, object>());
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        return;
                    Available = false;
                    LastError = ex.Message;
                    lock (sync)
                        foreach (Dictionary<string, object> item in batch)
                            if (queue.Count < 20000)
                                queue.Enqueue(item);
                    if (token.WaitHandle.WaitOne(30000))
                        return;
                }

                if (batch.Count == 0 && token.WaitHandle.WaitOne(2000))
                    return;
            }
        }
    }
}
