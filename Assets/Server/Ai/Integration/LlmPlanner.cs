using System;
using System.Collections.Generic;
using System.Text;

namespace NsoServer.Ai
{
    using Math = System.Math;

    // ====================================================================== local LLM
    /// <summary>
    /// Strategic planner backed by a local LLM through an OpenAI-compatible chat completions endpoint (Ollama,
    /// LM Studio, llama.cpp server), now delegated to the local Python Brain API.
    /// Runs on the StrategicService worker thread only.
    /// </summary>
    public sealed class LlmPlanner : IStrategicPlanner
    {
        public string Name
        {
            get
            {
                return "llm";
            }
        }

        // Compatibility facade for the existing headless diagnostics. Runtime planning lives in Python.
        public static string SystemPrompt
        {
            get
            {
                return System.IO.File.ReadAllText(BrainClient.Resource("system.txt"), Encoding.UTF8);
            }
        }

        public static string ActionsText
        {
            get
            {
                string prompt = SystemPrompt;
                int start = prompt.IndexOf("YOUR ACTIONS", StringComparison.Ordinal);
                return prompt.Substring(start, prompt.IndexOf("Use only map ids", start, StringComparison.Ordinal) - start);
            }
        }

        public static string BuildPrompt(AgentSnapshot s)
        {
            return BuildPrompt(s, null, true);
        }

        public static string BuildPrompt(AgentSnapshot s, List<string> recalled, bool terrain)
        {
            var result = BrainClient.Call("render", new Dictionary<string, object> { { "snapshot", BrainClient.Snapshot(s) }, { "recalled", recalled }, { "terrain", terrain } });
            return Convert.ToString(result["prompt"]);
        }

        public static string RecallQuery(AgentSnapshot s)
        {
            return s.Trigger + " | quest " + s.QuestName + ": " + s.QuestStep + " " + s.QuestHint + " | " + s.Activity + " | map " + s.MapId + " " + s.MapName;
        }

        public Decision Decide(AgentSnapshot s)
        {
            return BrainClient.Decide(s);
        }

        internal static bool IsLocal(string url)
        {
            Uri u;
            if (!Uri.TryCreate(url, UriKind.Absolute, out u))
                return false;
            return u.IsLoopback || u.Host == "localhost";
        }

        public static string Short(string s, int max)
        {
            return s == null ? "" : s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}
