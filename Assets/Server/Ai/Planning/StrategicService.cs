using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>
    /// One background worker shared by every agent: requests are queued (one per agent at most), answered in order
    /// and handed back to the tick thread through <see cref = "TryTake"/>. The game never waits for it. After a
    /// failure the service backs off for AgentConfig.LlmBackoffSeconds so the rule planner takes over meanwhile.
    /// </summary>
    public sealed class StrategicService
    {
        private readonly IStrategicPlanner planner;
        private readonly object sync = new object ();
        private readonly Queue<AgentSnapshot> queue = new Queue<AgentSnapshot>();
        private readonly Queue<DecisionResult> results = new Queue<DecisionResult>();
        private Thread thread;
        private volatile bool running;
        private long availableAfter;
        // debug counters (read without the lock: approximate values are fine)
        public int Requests, Successes, Failures;
        public long LastLatencyMs;
        public string LastError = "", LastAnswer = "";
        public string LastFailedAnswer = "", LastFailedAt = "";
        public bool Busy;
        public StrategicService(IStrategicPlanner planner)
        {
            this.planner = planner;
        }

        public string PlannerName
        {
            get
            {
                return planner.Name;
            }
        }

        public int QueueLength
        {
            get
            {
                lock (sync)
                    return queue.Count;
            }
        }

        public bool IsAvailable(long now)
        {
            return running && now >= Interlocked.Read(ref availableAfter);
        }

        public long AvailableAfter
        {
            get
            {
                return Interlocked.Read(ref availableAfter);
            }
        }

        public void Start()
        {
            if (running)
                return;
            running = true;
            thread = new Thread(Worker)
            {
                IsBackground = true,
                Name = "nso-agent-llm",
                Priority = ThreadPriority.BelowNormal
            };
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            lock (sync)
                Monitor.PulseAll(sync);
        }

        /// <summary>Queues a request; false when the queue is full or the agent already waits.</summary>
        public bool Enqueue(AgentSnapshot s)
        {
            lock (sync)
            {
                if (!running || queue.Count >= Math.Max(1, AgentConfig.LlmMaxQueue))
                    return false;
                foreach (var q in queue)
                    if (q.AgentId == s.AgentId)
                        return false;
                queue.Enqueue(s);
                Monitor.Pulse(sync);
                return true;
            }
        }

        public bool TryTake(out DecisionResult r)
        {
            lock (sync)
            {
                if (results.Count == 0)
                {
                    r = null;
                    return false;
                }

                r = results.Dequeue();
                return true;
            }
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void Worker()
        {
            while (running)
            {
                AgentSnapshot s;
                lock (sync)
                {
                    while (running && queue.Count == 0)
                        Monitor.Wait(sync, 1000);
                    if (!running)
                        break;
                    s = queue.Dequeue();
                }

                var r = new DecisionResult
                {
                    AgentId = s.AgentId,
                    RequestSequence = s.RequestSequence
                };
                if (Now() < Interlocked.Read(ref availableAfter))
                    r.Error = "LLM backing off after an error";
                else
                {
                    var sw = Stopwatch.StartNew();
                    Busy = true;
                    try
                    {
                        Interlocked.Increment(ref Requests);
                        r.Decision = planner.Decide(s);
                        Interlocked.Increment(ref Successes);
                        LastAnswer = LlmPlanner.Short(r.Decision.Raw, 400);
                    }
                    catch (Exception ex)
                    {
                        var inner = ex is AggregateException && ex.InnerException != null ? ex.InnerException : ex;
                        r.Error = inner is OperationCanceledException ? "timeout after " + AgentConfig.LlmTimeoutSeconds + " s" : inner.GetType().Name + ": " + inner.Message;
                        Interlocked.Increment(ref Failures);
                        LastError = DateTime.Now.ToString("HH:mm:ss") + " " + r.Error;
                        if (inner is LlmInvalidReplyException invalid)
                        {
                            LastFailedAt = DateTime.Now.ToString("HH:mm:ss");
                            LastFailedAnswer = LlmPlanner.Short(invalid.Response, 16384);
                            // Preserve the response, not just its reason prefix, without touching live game objects.
                            NsoServer.Net.PlayerLog.Bot(AgentConfig.AccountPrefix + s.Name, "LLM_ERROR", LastError + "\n" + LastFailedAnswer);
                        }

                        int backoff = inner is LlmInvalidReplyException ? AgentConfig.LlmFormatBackoffSeconds : AgentConfig.LlmBackoffSeconds;
                        Interlocked.Exchange(ref availableAfter, Now() + Math.Max(0, backoff) * 1000L);
                    }
                    finally
                    {
                        Busy = false;
                    }

                    r.LatencyMs = sw.ElapsedMilliseconds;
                    LastLatencyMs = r.LatencyMs;
                }

                lock (sync)
                    results.Enqueue(r);
            }
        }
    }
}
