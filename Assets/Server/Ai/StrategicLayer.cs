using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using NsoServer.Data;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>A hunting ground the planner may choose: a map (with its distance) and a monster kind on it.</summary>
    public sealed class HuntOption { public int MapId, Hops, MobId, MobLevel; public string MapName, MobName; }

    /// <summary>
    /// Everything a planner needs about one AI player, copied on the tick thread (under SyncRoot) so the planner can
    /// run on the LLM worker thread without touching live game objects: who it is, its character (level, experience,
    /// HP/MP, money, bag, equipment, school), its main quest, where it is, what it remembers (recent events and
    /// long-term gameplay knowledge) and its options (unlocked hunting grounds and neighbours).
    /// </summary>
    public sealed class AgentSnapshot
    {
        public int AgentId;
        public string Name, Bio, Traits, ClassName;
        public float Aggression, Caution, Greed, Curiosity, Sociability;
        public int Level, HpPercent, MpPercent, Kills, Deaths, QuestsDone, MapId, ItemsNearby, PlayersHere, HpPotions, MpPotions, FreeSlots;
        public long Yen, Xu, Exp, ExpNext;
        public string MapView = "", Activity = "", Terrain = "";   // AgentMapView.Build / BuildTerrain, NinjaAgent.Activity
        public string Account = "", Summary = "";                   // AgentRag owner; the AI's story so far
        public int EventsInPrompt = int.MaxValue;                   // newest events shown (older: summary / recall)
        public List<string> NpcMemory = new List<string>();   // remembered NPC positions of other maps (AgentKnowledge.NpcMaps)
        public double ExpPercent;
        public bool HasWeapon, CanShop, CanAffordPotions;
        public int Foods;            // food servings in the bag
        public float[] Priorities;   // the LLM's weights for the rule planner while they last (null: all 1)
        public bool Fed, WantsFood;  // a food effect is running; no food at all but it can buy some (AgentTactics.WantsFood)
        public string MapName, Trigger, CurrentPlan;
        public bool InVillage;
        // main quest
        public int QuestId = -1, QuestLevel;
        public string QuestName = "", QuestStep = "", QuestState = "", QuestHint = "";
        /// <summary>What the "quest" goal does now (AgentTactics.QuestGuide): NPC and place, monster and map, item.</summary>
        public string QuestGuide = "";
        public bool QuestBlockedByLevel;
        /// <summary>Item ledger: a village-shop item it never used and could buy to try (AgentTactics.PickTryItem), or -1.</summary>
        public int TryItemId = -1;
        public string TryItemText = "";
        public List<string> Bag = new List<string>();
        public List<string> Equipment = new List<string>();
        public List<string> Memory = new List<string>();
        public List<string> Knowledge = new List<string>();
        public List<HuntOption> Hunts = new List<HuntOption>();
        public List<int> Neighbors = new List<int>();
        public Dictionary<int, string> MapNames = new Dictionary<int, string>();
        public int UnlockedMaps;
        public int Seed;
    }

    /// <summary>The Strategic AI contract: decides what the agent wants to do next and why. Never touches game objects.</summary>
    public interface IStrategicPlanner
    {
        string Name { get; }
        /// <summary>Returns a decision, or throws when the planner cannot decide (the caller falls back to rules).</summary>
        Decision Decide(AgentSnapshot s);
    }

    // ====================================================================== fallback: utility AI

    /// <summary>
    /// Rule-based planner (utility AI). Scores rest / hunt / explore / collect / idle from the agent's state and
    /// personality and returns a short plan. Instant and always available: used when the LLM is disabled, busy,
    /// failing or returns an unusable answer.
    /// </summary>
    public sealed class RulePlanner : IStrategicPlanner
    {
        public string Name { get { return "rules"; } }

        public Decision Decide(AgentSnapshot s)
        {
            var rng = new Random(s.Seed);
            var d = new Decision { Source = "rules", FleeHpPercent = 20 + (int)(s.Caution * 25) };
            // 1) needs: low HP and no potions -> buy some if possible
            bool lowHp = s.HpPercent < 50;
            float shop = s.CanShop && s.CanAffordPotions && s.HpPotions < Math.Max(2, AgentConfig.PotionStock / 2) ? 0.6f + s.Caution * 0.6f + (lowHp ? 0.6f : 0f) : 0f;
            // Player log 2026-11-13: every AI death of the last 36 h was a character that could shop, had over 500 yen
            // and carried no HP potion (the shop score lost to the quest/hunt score). Restocking is mandatory first.
            if (s.CanShop && s.CanAffordPotions && s.HpPotions < AgentConfig.MinPotionsToFight) shop = 10f;
            // Food (HP and MP every half second for 30 min) is what players keep running while they train; the shop step
            // buys it after the potions. A short trip worth more than the next quest step.
            bool foodTrip = s.WantsFood && shop < 10f;
            if (foodTrip) shop = Math.Max(shop, 2.5f);   // above any hunt/quest score (at most about 2)
            // 2) the main quest: it unlocks maps, schools and rewards (players follow it). Play mode 2026-11-13: at 0.9-1.2
            // it lost to the hunt score (up to about 1.75 for aggressive characters) and bots trained beside an open
            // quest step; now it beats any hunt unless the LLM's priorities say otherwise (below the food trip, 2.5).
            float quest = s.QuestId >= 0 && !s.QuestBlockedByLevel ? 1.8f + (1f - s.Curiosity) * 0.3f : 0f;
            // 3) the best hunting ground: monsters near the level, not too far away
            HuntOption best = null;
            float bestScore = 0f;
            foreach (var h in s.Hunts)
            {
                int gap = h.MobLevel - s.Level;
                // too strong / no experience; without potions (before the shop opens) only monsters at most one level above
                int maxGap = s.HpPotions > 0 ? 2 + (int)(s.Aggression * 2) : 1;
                if (gap > maxGap || gap < -9) continue;
                float fit = 1f - Math.Abs(gap - 1) / 10f;
                float score = fit * (0.6f + s.Aggression) - h.Hops * 0.12f * (1.2f - s.Curiosity) + (float)rng.NextDouble() * 0.15f;
                if (score > bestScore) { bestScore = score; best = h; }
            }
            float hunt = best != null ? bestScore + (s.QuestBlockedByLevel ? 0.5f : 0f) : 0f;
            float explore = s.Neighbors.Count > 0 ? s.Curiosity * 0.5f + (float)rng.NextDouble() * 0.2f : 0f;
            float collect = s.ItemsNearby > 0 && s.FreeSlots > 0 ? 0.3f + s.Greed * 0.6f : 0f;
            float idle = s.InVillage ? s.Sociability * 0.4f * (s.PlayersHere > 0 ? 1.5f : 0.6f) : 0.05f;
            // User 2026-11-13: a curious player buys an unknown item to see what it does (item ledger). Below the quest
            // (1.8+) so it happens between quest steps or while training; the LLM's shop weight scales it.
            float tryItem = s.TryItemId >= 0 ? Math.Max(0.05f, 0.3f + s.Curiosity * 0.9f - s.Greed * 0.5f) : 0f;   // thrifty: tries less
            // the LLM's priorities for this situation (Priority; null = all 1); the mandatory potion restock stays
            var w = s.Priorities;
            if (w != null)
            {
                quest *= w[Priority.Quest]; hunt *= w[Priority.Hunt]; explore *= w[Priority.Explore];
                collect *= w[Priority.Collect]; idle = Math.Max(0.01f, idle * w[Priority.Idle]);
                if (shop < 10f) shop *= w[Priority.Shop];
                tryItem *= w[Priority.Shop];
            }

            float top = Math.Max(Math.Max(Math.Max(shop, quest), Math.Max(hunt, explore)), Math.Max(Math.Max(collect, idle), tryItem));
            if (top == shop && shop > 0)
            {
                d.Reason = foodTrip && s.HpPotions >= Math.Max(2, AgentConfig.PotionStock / 2) ? "No food left: buy some at Tabemono, it restores HP and MP for 30 minutes."
                    : "Only " + s.HpPotions + " HP potions left, buy more (and food) before fighting.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Shop });
                d.Steps.Add(new PlanStep { Goal = s.QuestId >= 0 && !s.QuestBlockedByLevel ? GoalType.Quest : GoalType.Hunt, MapId = best != null && s.QuestBlockedByLevel ? best.MapId : -1, MobId = best != null && s.QuestBlockedByLevel ? best.MobId : -1, Count = 10 });
            }
            else if (top == tryItem && tryItem > 0)
            {
                d.Reason = "Never used " + s.TryItemText + ": buy one and try what it does.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Shop, MobId = s.TryItemId, Count = 1 });
                if (s.QuestId >= 0 && !s.QuestBlockedByLevel) d.Steps.Add(new PlanStep { Goal = GoalType.Quest });
            }
            else if (top == quest && quest > 0)
            {
                d.Reason = "Continue the main quest " + s.QuestName + ".";
                d.Steps.Add(new PlanStep { Goal = GoalType.Quest });
            }
            else if (top == collect && collect > 0)
            {
                d.Reason = "There are items lying nearby.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Collect, Count = Math.Min(5, s.ItemsNearby) });
            }
            else if (top == hunt && best != null)
            {
                d.Reason = (s.QuestBlockedByLevel ? "Need level " + s.QuestLevel + " for the next quest: " : "") + "train on " + best.MobName + " (level " + best.MobLevel + ") in " + best.MapName + ".";
                d.Steps.Add(Hunt(best, s));
                if (s.Greed > 0.5f) d.Steps.Add(new PlanStep { Goal = GoalType.Collect, Count = 3 });
            }
            else if (top == explore && s.Neighbors.Count > 0)
            {
                int map = s.Neighbors[rng.Next(s.Neighbors.Count)];
                string name;
                s.MapNames.TryGetValue(map, out name);
                d.Reason = "Curious about " + (name ?? "map " + map) + ".";
                d.Steps.Add(new PlanStep { Goal = GoalType.Explore, MapId = map });
            }
            else
            {
                d.Reason = s.InVillage ? "Hang around the village for a while." : "Nothing better to do, wait a little.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 20 + rng.Next(30) });
            }
            return d;
        }

        private static PlanStep Hunt(HuntOption h, AgentSnapshot s)
        {
            return new PlanStep { Goal = GoalType.Hunt, MapId = h.MapId, MobId = h.MobId, Count = 5 + (int)(s.Aggression * 10) };
        }
    }

    // ====================================================================== local LLM

    /// <summary>
    /// Strategic planner backed by a local LLM through an OpenAI-compatible chat completions endpoint (Ollama,
    /// LM Studio, llama.cpp server). Builds a compact prompt from the snapshot and parses a JSON decision.
    /// Runs on the StrategicService worker thread only.
    /// </summary>
    public sealed class LlmPlanner : IStrategicPlanner
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };   // per-request token below

        public string Name { get { return "llm"; } }

        public const string SystemPrompt =
            "You are the mind of a ninja PLAYER in the MMORPG \"Ninja School Online\". You play by exactly the same rules as " +
            "human players: maps unlock only by finishing the main quest, HP never comes back by itself (drink potions bought " +
            "from Kiriko with yen or xu, or die and return to the village), you need a weapon to fight, the bag has limited " +
            "space, monsters more than 5 levels above you are dangerous and monsters more than 10 levels below give no " +
            "experience. You decide WHAT to do next and WHY; your hands (a game AI) do the walking, fighting, talking to NPCs " +
            "and picking up.\n" +
            "Reply with ONE JSON object and nothing else. Every step must contain goal, map, mob, count, seconds, x and y. " +
            "Use map=-1, mob=-1, x=-1 and y=-1 when unused, count=1 and seconds=30 when irrelevant. Never omit steps.\n" +
            "{\"reason\":\"<one short sentence>\",\"say\":\"<optional short line you say aloud, in Vietnamese, or empty>\"," +
            "\"fleeHp\":<10-60, HP percent at which you drink a potion or retreat>," +
            "\"priorities\":{\"quest\":<0-3>,\"hunt\":<0-3>,\"shop\":<0-3>,\"explore\":<0-3>,\"collect\":<0-3>,\"idle\":<0-3>}," +
            "\"steps\":[<1 to 4 steps, done in order>]}\n" +
            "priorities: you answer slowly, so between your answers a fast rule planner picks the next plan; it multiplies its " +
            "scores by these weights (1 = normal, 0 = never, 3 = strongly prefer). Set them for the situation every time, e.g. " +
            "quest 2 when the quest step is doable now, hunt 2 and quest 0.5 when the quest needs a higher level, shop 2 when low on potions. " +
            "Buying potions when none are left always happens.\n" +
            "Step kinds:\n" +
            "{\"goal\":\"quest\"}  work on your current main quest (talk to NPCs, kill or collect what it asks, report).\n" +
            "{\"goal\":\"hunt\",\"map\":<map id>,\"mob\":<monster id or -1 for any suitable>,\"count\":<1-30>}  defeat monsters there.\n" +
            "{\"goal\":\"shop\"}  walk to Kiriko and buy HP potions, then food at Tabemono (needs money and quest 3 done). Food restores HP and MP for 30 min and is eaten automatically.\n" +
            "{\"goal\":\"collect\",\"count\":<1-10>}  pick up items lying nearby.\n" +
            "{\"goal\":\"rest\"}  drink potions until HP is high, buying some if needed.\n" +
            "{\"goal\":\"explore\",\"map\":<map id, or -1 for a random neighbour>}  go and look around.\n" +
            "{\"goal\":\"travel\",\"map\":<map id>}  walk to a map.\n" +
            "{\"goal\":\"idle\",\"seconds\":<5-120>}  hang around, chat, wait.\n" +
            "{\"goal\":\"goto\",\"map\":<map id or -1 for this map>,\"x\":<pixel>,\"y\":<pixel or -1>}  walk/jump to that exact spot " +
            "(take the coordinates from CURRENT MAP: an NPC, an exit, a monster group, an item; it fails when no way is found).\n" +
            ActionsText +
            "Use only map ids and monster ids listed in the request. Learn from your memory and knowledge (avoid what killed " +
            "you, return to grounds that paid off). Act according to your personality.";

        /// <summary>What a player can do and how (human key bindings; the AI player's hands do the same through BotClient).</summary>
        public const string ActionsText =
            "YOUR ACTIONS (what any player can do; your hands do them for your steps): walk left/right (arrow keys), jump " +
            "(Up or Space; press again while still rising for a double jump), drop down from a ledge, attack a monster with the " +
            "selected skill (range, cooldown and MP apply), select a skill, pick up an item on the ground when standing on it, " +
            "use an item from the bag (potion, food, book), wear or take off equipment, talk to an NPC and choose a menu entry " +
            "(quests, shops, school, zone list), buy in a shop, change zone at the zone NPC, walk into an exit to change map " +
            "(only unlocked maps), return to the village after dying, say a line in chat. You cannot fly, teleport or pass " +
            "through walls, and some ledges cannot be reached at all.\n";

        /// <summary>The user message: who the ninja is, its state, memory, options and what just happened.</summary>
        public static string BuildPrompt(AgentSnapshot s) { return BuildPrompt(s, null, true); }

        /// <summary>The prompt with the memories recalled from the vector database (null: the in-context knowledge as
        /// before) and with or without the terrain grid (dropped when the context is full).</summary>
        public static string BuildPrompt(AgentSnapshot s, List<string> recalled, bool terrain)
        {
            var b = new StringBuilder(2500);
            b.Append("YOU: ").Append(s.Name).Append(". ").Append(s.Bio).Append('\n');
            b.Append("Traits: ").Append(s.Traits).Append('\n');
            // spending style (user 2026-11-13: players may save money or not); greed is the thrift trait
            b.Append("SPENDING STYLE: ").Append(s.Greed >= 0.67f ? "thrifty: you keep money back, carry few potions and rarely buy things just to try"
                : s.Greed <= 0.33f ? "spender: you stock up and happily buy new things to try" : "balanced").Append('\n');
            b.Append("CHARACTER: level ").Append(s.Level).Append(" (").Append(s.ExpPercent.ToString("0.0", CultureInfo.InvariantCulture)).Append("% to next), ")
             .Append(s.ClassName).Append(", HP ").Append(s.HpPercent).Append("%, MP ").Append(s.MpPercent).Append("%, yen ").Append(s.Yen)
             .Append(", xu ").Append(s.Xu).Append(s.HasWeapon ? "" : ", NO WEAPON").Append('\n');
            b.Append("LIFE: ").Append(s.Kills).Append(" kills, ").Append(s.Deaths).Append(" deaths, ").Append(s.QuestsDone).Append(" quests finished, ")
             .Append(s.UnlockedMaps).Append(" maps unlocked\n");
            b.Append("BAG (").Append(s.FreeSlots).Append(" free slots): HP potions ").Append(s.HpPotions).Append(", MP potions ").Append(s.MpPotions)
             .Append(", food ").Append(s.Foods).Append(s.Fed ? " (eating: HP/MP regenerating)" : " (not eating)");
            if (s.Bag.Count > 0) b.Append(", ").Append(string.Join(", ", s.Bag));
            b.Append('\n');
            b.Append("EQUIPPED: ").Append(s.Equipment.Count > 0 ? string.Join(", ", s.Equipment) : "nothing").Append('\n');
            if (s.QuestId >= 0)
                b.Append("MAIN QUEST ").Append(s.QuestId).Append(" \"").Append(s.QuestName).Append("\": ").Append(s.QuestState)
                 .Append(string.IsNullOrEmpty(s.QuestStep) ? "" : ", step \"" + s.QuestStep + "\"").Append(string.IsNullOrEmpty(s.QuestHint) ? "" : " (" + s.QuestHint + ")").Append('\n');
            else b.Append("MAIN QUEST: all done\n");
            // Play mode 2026-11-13: without this the LLM guessed goto coordinates for "report to Kiriko" steps.
            if (s.QuestId >= 0 && !string.IsNullOrEmpty(s.QuestGuide))
                b.Append("QUEST NEXT: ").Append(s.QuestGuide).Append(". The step {\"goal\":\"quest\"} does exactly this by itself (walks there, talks, hunts, picks up); prefer it over goto/hunt unless you must shop, rest or train for the level first.\n");
            if (s.TryItemId >= 0)
                b.Append("UNKNOWN ITEM YOU COULD TRY: ").Append(s.TryItemText).Append(" - the step {\"goal\":\"shop\",\"mob\":").Append(s.TryItemId)
                 .Append("} buys one and uses it once; what it did goes into your knowledge.\n");
            b.Append("LOCATION: map ").Append(s.MapId).Append(" \"").Append(s.MapName).Append("\"")
             .Append(s.InVillage ? " (village)" : "").Append(", players here ").Append(s.PlayersHere)
             .Append(", items on the ground you may take ").Append(s.ItemsNearby).Append('\n');
            // Runtime rules override older saved beginner guides, for both humans and AI players.
            b.Append("CURRENT GROUND LOOT RULES: exclusive owner for ").Append(NsoServer.World.GroundLootPolicy.OwnerProtectionMs / 1000)
             .Append(" seconds; disappears after ").Append(NsoServer.World.GroundLootPolicy.LifetimeMs / 1000).Append(" seconds. These timings override the beginner guide.\n");
            if (!string.IsNullOrEmpty(s.CurrentPlan)) b.Append("CURRENT PLAN: ").Append(s.CurrentPlan).Append('\n');
            b.Append("YOUR PRIORITIES NOW: ").Append(Priority.Text(s.Priorities)).Append('\n');
            if (!string.IsNullOrEmpty(s.Activity)) b.Append("DOING NOW: ").Append(s.Activity).Append('\n');
            if (!string.IsNullOrEmpty(s.MapView))
            {
                b.Append("CURRENT MAP (pixels; x grows right, y grows DOWN and is the floor under the feet):\n");
                if (terrain && !string.IsNullOrEmpty(s.Terrain)) b.Append(AgentMapView.GridLegend).Append(s.Terrain);
                b.Append(s.MapView);
            }
            if (!string.IsNullOrEmpty(s.Summary)) b.Append("YOUR STORY SO FAR (summary of older memories): ").Append(s.Summary).Append('\n');
            if (recalled != null)
            {
                // long-term memory: only what is relevant now (AgentRag.Recall); everything else stays in the database
                b.Append("MEMORIES RELEVANT NOW (recalled from your long-term memory):\n");
                if (recalled.Count == 0) b.Append("- nothing relevant\n");
                // long game documents (an NPC in 20 villages, a shop with 40 items) are cut to fit the small context
                foreach (var k in recalled) b.Append("- ").Append(Short(k, Math.Max(80, AgentConfig.RagMemoryChars))).Append('\n');
            }
            else
            {
                b.Append("KNOWLEDGE (what you learned playing):\n");
                if (s.Knowledge.Count == 0) b.Append("- nothing yet\n");
                foreach (var k in s.Knowledge) b.Append("- ").Append(k).Append('\n');
                if (s.NpcMemory.Count > 0)
                {
                    b.Append("NPC POSITIONS YOU REMEMBER IN OTHER MAPS (goto them with the map id and coordinates):\n");
                    foreach (var m in s.NpcMemory) b.Append("- ").Append(m).Append('\n');
                }
            }
            b.Append("RECENT EVENTS (oldest first):\n");
            if (s.Memory.Count == 0) b.Append("- nothing yet\n");
            for (int i = Math.Max(0, s.Memory.Count - s.EventsInPrompt); i < s.Memory.Count; i++) b.Append("- ").Append(s.Memory[i]).Append('\n');
            b.Append("HUNTING GROUNDS IN UNLOCKED MAPS (map id \"name\" hops: monster id name level):\n");
            int n = 0;
            foreach (var h in s.Hunts)
            {
                if (n++ >= 16) break;
                b.Append("- map ").Append(h.MapId).Append(" \"").Append(h.MapName).Append("\" ").Append(h.Hops).Append(" hops: mob ")
                 .Append(h.MobId).Append(' ').Append(h.MobName).Append(" lv ").Append(h.MobLevel).Append('\n');
            }
            if (s.Hunts.Count == 0) b.Append("- none suitable\n");
            b.Append("NEIGHBOUR MAPS: ");
            for (int i = 0; i < s.Neighbors.Count; i++)
            {
                string name;
                s.MapNames.TryGetValue(s.Neighbors[i], out name);
                if (i > 0) b.Append(", ");
                b.Append(s.Neighbors[i]).Append(" \"").Append(name).Append('"');
            }
            b.Append('\n');
            b.Append("EVENT: ").Append(s.Trigger).Append('\n');
            b.Append("Decide the next plan. JSON only.");
            return b.ToString();
        }

        /// <summary>What the long-term memory is searched with: the event, the quest step, the activity and the place.</summary>
        public static string RecallQuery(AgentSnapshot s)
        {
            return s.Trigger + " | quest " + s.QuestName + ": " + s.QuestStep + " " + s.QuestHint + " | " + s.Activity + " | map " + s.MapId + " " + s.MapName;
        }

        private static int Tokens(string text) { return (int)Math.Ceiling(text.Length / Math.Max(1.0, AgentConfig.CharsPerToken)); }

        public Decision Decide(AgentSnapshot s)
        {
            if (AgentConfig.LlmLocalOnly && !IsLocal(AgentConfig.LlmEndpoint))
                throw new InvalidOperationException("endpoint is not on this machine (LlmLocalOnly)");
            // Long-term memory: recall what matters now; the guide then also comes from the database.
            var recalled = AgentConfig.RagEnabled ? AgentRag.Recall(s.Account, RecallQuery(s), AgentConfig.RagTopK) : null;
            if (recalled != null) s.EventsInPrompt = Math.Max(1, AgentConfig.ContextEvents);
            string system = recalled == null && !string.IsNullOrEmpty(AgentConfig.LlmGuide) ? SystemPrompt + "\n\n" + AgentConfig.LlmGuide : SystemPrompt;
            int budget = AgentConfig.LlmContextTokens - AgentConfig.LlmMaxTokens - 150;
            string user = BuildPrompt(s, recalled, true);
            // Context full (or the event list full): the LLM summarises the older events into "the story so far", which
            // replaces them (DrainResults); the events themselves stay in the vector memory.
            string summary = null; int cut = 0;
            if (Tokens(system) + Tokens(user) > budget || s.Memory.Count >= Math.Max(2, AgentConfig.MemorySize))
            {
                int keep = Math.Max(1, AgentConfig.ContextEvents);
                if (s.Memory.Count > keep)
                {
                    try
                    {
                        summary = Summarize(s, s.Memory.Count - keep);
                        cut = s.Memory.Count - keep;
                        s.Summary = summary; s.Memory.RemoveRange(0, cut); s.EventsInPrompt = keep;
                    }
                    catch (Exception) { summary = null; cut = 0; s.EventsInPrompt = keep; }   // no summary this time: newest events only
                    user = BuildPrompt(s, recalled, true);
                }
            }
            // still too long: the terrain grid goes first (the object list with coordinates stays)
            if (Tokens(system) + Tokens(user) > budget) user = BuildPrompt(s, recalled, false);
            // then the least relevant recalled memories (the shared game knowledge comes last in the list)
            while (recalled != null && recalled.Count > 1 && Tokens(system) + Tokens(user) > budget)
            {
                recalled.RemoveAt(recalled.Count - 1);
                user = BuildPrompt(s, recalled, false);
            }
            var d = Ask(system, user);
            d.Summary = summary; d.SummaryCut = cut;
            return d;
        }

        /// <summary>Asks the LLM to compress the oldest events (and the previous summary) into a short story.</summary>
        private static string Summarize(AgentSnapshot s, int count)
        {
            var b = new StringBuilder();
            b.Append("You are ").Append(s.Name).Append(", a ninja player. Rewrite your memories as ONE short paragraph (at most ")
             .Append(AgentConfig.SummaryWords).Append(" words, first person, English) keeping what matters for playing: dangers, ")
             .Append("good hunting places, where NPCs are, quest progress, what failed and why. Plain text only.\n");
            if (!string.IsNullOrEmpty(s.Summary)) b.Append("Earlier summary: ").Append(s.Summary).Append('\n');
            b.Append("Events (oldest first):\n");
            for (int i = 0; i < count && i < s.Memory.Count; i++) b.Append("- ").Append(s.Memory[i]).Append('\n');
            string text = Chat(null, b.ToString(), 300, false).Trim();
            if (text.Length == 0) throw new InvalidOperationException("empty summary");
            return Short(text.Replace('\n', ' '), 800);
        }

        private static Decision Ask(string system, string user)
        {
            string content = Chat(system, user, AgentConfig.LlmMaxTokens, true);
            string error;
            var d = DecisionParser.Parse(content, out error);
            if (d == null) throw new LlmInvalidReplyException(error, content);
            d.Source = "llm";
            d.Raw = content;
            return d;
        }

        /// <summary>One chat completion (local endpoint only). json = the reply schema of a decision.</summary>
        private static string Chat(string system, string user, int maxTokens, bool json)
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Prop("model", AgentConfig.LlmModel);
            w.Prop("temperature", (double)AgentConfig.LlmTemperature);
            w.Prop("max_tokens", maxTokens);
            w.Prop("stream", false);
            if (!string.IsNullOrEmpty(AgentConfig.LlmReasoningEffort)) w.Prop("reasoning_effort", AgentConfig.LlmReasoningEffort);
            if (json && AgentConfig.LlmJsonMode)
                w.Prop("response_format", AgentConfig.LlmStructuredOutputs ? LlmReplyFormat.Build() : new Dictionary<string, object> { { "type", "json_object" } });
            w.Key("messages"); w.BeginArray();
            if (!string.IsNullOrEmpty(system)) { w.BeginObject(); w.Prop("role", "system"); w.Prop("content", system); w.EndObject(); }
            w.BeginObject(); w.Prop("role", "user"); w.Prop("content", user); w.EndObject();
            w.EndArray();
            w.EndObject();

            var req = new HttpRequestMessage(HttpMethod.Post, AgentConfig.LlmEndpoint) { Content = new StringContent(w.ToString(), Encoding.UTF8, "application/json") };
            if (!string.IsNullOrEmpty(AgentConfig.LlmApiKey)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AgentConfig.LlmApiKey);
            string text;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, AgentConfig.LlmTimeoutSeconds))))
            using (var resp = Http.SendAsync(req, cts.Token).GetAwaiter().GetResult())
            {
                text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + ": " + Short(text, 160));
            }
            return ContentOf(text);
        }

        // choices[0].message.content (OpenAI-compatible) or message.content (Ollama /api/chat)
        private static string ContentOf(string body)
        {
            var root = Json.Parse(body) as Dictionary<string, object>;
            if (root == null) throw new FormatException("response is not a JSON object");
            object o;
            if (root.TryGetValue("choices", out o) && o is List<object> choices && choices.Count > 0)
            {
                var c = choices[0] as Dictionary<string, object>;
                if (c != null && c.TryGetValue("message", out o) && o is Dictionary<string, object> msg && msg.TryGetValue("content", out o)) return Convert.ToString(o, CultureInfo.InvariantCulture);
            }
            if (root.TryGetValue("message", out o) && o is Dictionary<string, object> m2 && m2.TryGetValue("content", out o)) return Convert.ToString(o, CultureInfo.InvariantCulture);
            if (root.TryGetValue("error", out o)) throw new InvalidOperationException("server error: " + Short(Convert.ToString(o, CultureInfo.InvariantCulture), 160));
            throw new FormatException("no message content in the response");
        }

        internal static bool IsLocal(string url)
        {
            Uri u;
            if (!Uri.TryCreate(url, UriKind.Absolute, out u)) return false;
            return u.IsLoopback || u.Host == "localhost";
        }

        public static string Short(string s, int max) { return s == null ? "" : s.Length <= max ? s : s.Substring(0, max) + "..."; }
    }

    /// <summary>Parses the planner's JSON answer into a Decision (no world validation; AgentManager validates).</summary>
    public static class DecisionParser
    {
        public static Decision Parse(string content, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(content)) { error = "empty answer (thinking used the whole token budget?)"; return null; }
            // thinking models may still put their reasoning inline
            int think = content.LastIndexOf("</think>", StringComparison.Ordinal);
            if (think >= 0) content = content.Substring(think + 8);
            int a = content.IndexOf('{'), z = content.LastIndexOf('}');
            if (a < 0 || z <= a) { error = "no JSON object in the answer"; return null; }
            Dictionary<string, object> root;
            try { root = Json.Parse(content.Substring(a, z - a + 1)) as Dictionary<string, object>; }
            catch (Exception ex) { error = "invalid JSON: " + ex.Message; return null; }
            if (root == null) { error = "answer is not a JSON object"; return null; }
            var d = new Decision { Reason = Str(root, "reason"), Say = Str(root, "say"), FleeHpPercent = Int(root, "fleeHp", -1) };
            object o;
            if (!root.TryGetValue("steps", out o) || !(o is List<object> steps) || steps.Count == 0) { error = "no steps"; return null; }
            foreach (var so in steps)
            {
                var st = so as Dictionary<string, object>;
                if (st == null) continue;
                GoalType g;
                if (!TryGoal(Str(st, "goal"), out g)) continue;
                d.Steps.Add(new PlanStep { Goal = g, MapId = Int(st, "map", -1), MobId = Int(st, "mob", -1), Count = Int(st, "count", 5), Seconds = Int(st, "seconds", 30),
                    X = Int(st, "x", -1), Y = Int(st, "y", -1) });
            }
            if (d.Steps.Count == 0) { error = "no known goal in the steps"; return null; }
            // optional: weights for the rule planner (missing names stay 1)
            if (root.TryGetValue("priorities", out o) && o is Dictionary<string, object> pr)
            {
                d.Priorities = new float[Priority.Names.Length];
                for (int i = 0; i < d.Priorities.Length; i++)
                {
                    d.Priorities[i] = 1f;
                    object v;
                    if (pr.TryGetValue(Priority.Names[i], out v) && v != null)
                        try { d.Priorities[i] = (float)Convert.ToDouble(v, CultureInfo.InvariantCulture); } catch (Exception) { }
                }
            }
            return d;
        }

        private static bool TryGoal(string s, out GoalType g)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "hunt": case "fight": case "train": case "hit": case "attack": case "kill": case "farm": case "grind": g = GoalType.Hunt; return true;
                case "collect": case "gather": case "loot": g = GoalType.Collect; return true;
                case "rest": case "heal": case "recover": case "drink": g = GoalType.Rest; return true;
                case "explore": g = GoalType.Explore; return true;
                case "travel": case "go": case "move": case "walk": g = GoalType.Travel; return true;
                case "idle": case "wait": case "chat": g = GoalType.Idle; return true;
                case "quest": case "mission": case "task": case "questline": g = GoalType.Quest; return true;
                case "shop": case "buy": case "buy_potions": case "potions": g = GoalType.Shop; return true;
                case "goto": case "go_to": case "moveto": case "position": g = GoalType.Move; return true;
            }
            g = GoalType.Idle;
            return false;
        }

        private static string Str(Dictionary<string, object> d, string k)
        {
            object o;
            return d.TryGetValue(k, out o) && o != null ? Convert.ToString(o, CultureInfo.InvariantCulture) : "";
        }

        private static int Int(Dictionary<string, object> d, string k, int def)
        {
            object o;
            if (!d.TryGetValue(k, out o) || o == null) return def;
            try { return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Json.Long(o))); }
            catch (Exception) { return def; }
        }
    }

    // ====================================================================== shared LLM service

    /// <summary>The outcome of one queued request.</summary>
    public sealed class DecisionResult
    {
        public int AgentId;
        public Decision Decision;   // null on failure
        public string Error;
        public long LatencyMs;
    }

    /// <summary>
    /// One background worker shared by every agent: requests are queued (one per agent at most), answered in order
    /// and handed back to the tick thread through <see cref="TryTake"/>. The game never waits for it. After a
    /// failure the service backs off for AgentConfig.LlmBackoffSeconds so the rule planner takes over meanwhile.
    /// </summary>
    public sealed class StrategicService
    {
        private readonly IStrategicPlanner planner;
        private readonly object sync = new object();
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

        public StrategicService(IStrategicPlanner planner) { this.planner = planner; }

        public string PlannerName { get { return planner.Name; } }
        public int QueueLength { get { lock (sync) return queue.Count; } }
        public bool IsAvailable(long now) { return running && now >= Interlocked.Read(ref availableAfter); }
        public long AvailableAfter { get { return Interlocked.Read(ref availableAfter); } }

        public void Start()
        {
            if (running) return;
            running = true;
            thread = new Thread(Worker) { IsBackground = true, Name = "nso-agent-llm", Priority = ThreadPriority.BelowNormal };
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            lock (sync) Monitor.PulseAll(sync);
        }

        /// <summary>Queues a request; false when the queue is full or the agent already waits.</summary>
        public bool Enqueue(AgentSnapshot s)
        {
            lock (sync)
            {
                if (!running || queue.Count >= Math.Max(1, AgentConfig.LlmMaxQueue)) return false;
                foreach (var q in queue) if (q.AgentId == s.AgentId) return false;
                queue.Enqueue(s);
                Monitor.Pulse(sync);
                return true;
            }
        }

        public bool TryTake(out DecisionResult r)
        {
            lock (sync)
            {
                if (results.Count == 0) { r = null; return false; }
                r = results.Dequeue();
                return true;
            }
        }

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        private void Worker()
        {
            while (running)
            {
                AgentSnapshot s;
                lock (sync)
                {
                    while (running && queue.Count == 0) Monitor.Wait(sync, 1000);
                    if (!running) break;
                    s = queue.Dequeue();
                }
                var r = new DecisionResult { AgentId = s.AgentId };
                if (Now() < Interlocked.Read(ref availableAfter)) r.Error = "LLM backing off after an error";
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
                    finally { Busy = false; }
                    r.LatencyMs = sw.ElapsedMilliseconds;
                    LastLatencyMs = r.LatencyMs;
                }
                lock (sync) results.Enqueue(r);
            }
        }
    }
}
