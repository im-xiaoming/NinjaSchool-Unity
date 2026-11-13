using System;
using System.Collections.Generic;
using NsoServer.Data;

namespace NsoServer.Ai
{
    using Math = System.Math; // the client has its own global Math class

    /// <summary>
    /// Long-term gameplay memory of an AI player (offline addition): facts it learned by playing, kept across sessions
    /// in the save database and shown to the LLM. Unlike the short event memory (NinjaAgent.Memory, the last events),
    /// a fact is keyed and updated in place, so the AI keeps what matters: which monsters killed it, where hunting paid
    /// off, the quiz answers it found by trying, maps still locked, where NPCs and shops are, quests it finished,
    /// items it found. Keys: "danger:mob", "ground:map:mob", "quiz:...", "locked:map", "place:npc", "quest:id", "item:id",
    /// "note:...". Call under GameWorld.SyncRoot.
    /// </summary>
    public sealed class AgentKnowledge
    {
        public sealed class Fact
        {
            public string Key, Text;
            public int Count;
            public long Updated;
        }

        /// <summary>Facts kept per AI player; the least recently updated are forgotten first.</summary>
        public static int MaxFacts = 80;
        private readonly Dictionary<string, Fact> facts = new Dictionary<string, Fact>();
        public int Count
        {
            get
            {
                return facts.Count;
            }
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>Learns or refreshes a fact; its counter goes up each time (how often it happened).</summary>
        /// <summary>Account of the AI player owning this memory (AgentRag point owner); null = not mirrored.</summary>
        public string Owner;
        public Fact Learn(string key, string text)
        {
            Fact f;
            if (!facts.TryGetValue(key, out f))
            {
                f = new Fact
                {
                    Key = key
                };
                facts[key] = f;
            }

            f.Text = text;
            Mirror(key, text);
            f.Count++;
            f.Updated = Now();
            if (facts.Count > MaxFacts)
                Forget();
            return f;
        }

        public Fact Get(string key)
        {
            Fact f;
            return facts.TryGetValue(key, out f) ? f : null;
        }

        public void Remove(string key)
        {
            facts.Remove(key);
        }

        private void Forget()
        {
            Fact oldest = null;
            // NPC positions per map ("npcs:map") are kept: one fact per visited map, the AI's map of the world
            foreach (var f in facts.Values)
                if (!f.Key.StartsWith(NpcPrefix, StringComparison.Ordinal) && (oldest == null || f.Updated < oldest.Updated))
                    oldest = f;
            if (oldest != null)
                facts.Remove(oldest.Key);
        }

        /// <summary>The most useful facts for the planner: dangers and quests first, then the most recent.</summary>
        public List<Fact> Top(int n)
        {
            var list = new List<Fact>(facts.Values);
            list.RemoveAll(f => f.Key.StartsWith("quiz:", StringComparison.Ordinal)); // answers matter to the game AI only
            list.RemoveAll(f => f.Key.StartsWith(NpcPrefix, StringComparison.Ordinal)); // own prompt section (NpcMaps)
            list.Sort((a, b) =>
            {
                int pa = Priority(a.Key), pb = Priority(b.Key);
                return pa != pb ? pa.CompareTo(pb) : b.Updated.CompareTo(a.Updated);
            });
            if (list.Count > n)
                list.RemoveRange(n, list.Count - n);
            return list;
        }

        private static int Priority(string key)
        {
            if (key.StartsWith("danger:", StringComparison.Ordinal))
                return 0;
            if (key.StartsWith("locked:", StringComparison.Ordinal) || key.StartsWith("note:", StringComparison.Ordinal))
                return 1;
            if (key.StartsWith("ground:", StringComparison.Ordinal))
                return 2;
            return 3;
        }

        public List<Fact> All()
        {
            return new List<Fact>(facts.Values);
        }

        /// <summary>Copies the fact to the long-term memory (quiz answers are only for the game AI).</summary>
        public void Mirror(string key, string text)
        {
            if (Owner == null || key.StartsWith("quiz:", StringComparison.Ordinal))
                return;
            int colon = key.IndexOf(':');
            AgentRag.Remember(Owner, "fact:" + key, colon > 0 ? key.Substring(0, colon) : "fact", text);
        }

        /// <summary>Mirrors every fact (after loading a save).</summary>
        public void MirrorAll()
        {
            foreach (var f in facts.Values)
                Mirror(f.Key, f.Text);
        }

        /// <summary>Key prefix of the remembered NPC positions of a map ("npcs:22").</summary>
        public const string NpcPrefix = "npcs:";
        /// <summary>The remembered NPC positions, one fact per map, most recently seen first.</summary>
        public List<Fact> NpcMaps()
        {
            var list = new List<Fact>();
            foreach (var f in facts.Values)
                if (f.Key.StartsWith(NpcPrefix, StringComparison.Ordinal))
                    list.Add(f);
            list.Sort((a, b) => b.Updated.CompareTo(a.Updated));
            return list;
        }

        // ---- persistence (part of the AI players' document in the save database)
        public List<object> ToJson()
        {
            var list = new List<object>();
            foreach (var f in facts.Values)
            {
                var d = new Dictionary<string, object>();
                d["k"] = f.Key;
                d["t"] = f.Text;
                d["n"] = f.Count;
                d["u"] = f.Updated;
                list.Add(d);
            }

            return list;
        }

        public void FromJson(object o)
        {
            facts.Clear();
            var list = o as List<object>;
            if (list == null)
                return;
            foreach (var x in list)
            {
                var d = x as Dictionary<string, object>;
                if (d == null)
                    continue;
                object k, t, n, u;
                if (!d.TryGetValue("k", out k) || !(k is string) || !d.TryGetValue("t", out t))
                    continue;
                var f = new Fact
                {
                    Key = (string)k,
                    Text = Convert.ToString(t)
                };
                if (d.TryGetValue("n", out n))
                    f.Count = (int)Math.Max(0, Json.Long(n));
                if (d.TryGetValue("u", out u))
                    f.Updated = Json.Long(u);
                facts[f.Key] = f;
            }
        }
    }
}
