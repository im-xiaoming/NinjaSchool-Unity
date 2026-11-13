using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>
    /// Everything a planner needs about one AI player, copied on the tick thread (under SyncRoot) so the planner can
    /// run on the LLM worker thread without touching live game objects: who it is, its character (level, experience,
    /// HP/MP, money, bag, equipment, school), its main quest, where it is, what it remembers (recent events and
    /// long-term gameplay knowledge) and its options (unlocked hunting grounds and neighbours).
    /// </summary>
    public sealed class AgentSnapshot
    {
        public int AgentId;
        public long RequestSequence;
        public string Name, Bio, Traits, ClassName;
        public float Aggression, Caution, Greed, Curiosity, Sociability;
        public int Level, HpPercent, MpPercent, Kills, Deaths, QuestsDone, MapId, ItemsNearby, PlayersHere, HpPotions, MpPotions, FreeSlots;
        public long Yen, Xu, Exp, ExpNext;
        public string MapView = "", Activity = "", Terrain = ""; // AgentMapView.Build / BuildTerrain, NinjaAgent.Activity
        public string Account = "", Summary = ""; // AgentRag owner; the AI's story so far
        public int EventsInPrompt = int.MaxValue; // newest events shown (older: summary / recall)
        public List<string> NpcMemory = new List<string>(); // remembered NPC positions of other maps (AgentKnowledge.NpcMaps)
        public double ExpPercent;
        public bool HasWeapon, CanShop, CanAffordPotions;
        public int Foods; // food servings in the bag
        public float[] Priorities; // the LLM's weights for the rule planner while they last (null: all 1)
        public bool Fed, WantsFood; // a food effect is running; no food at all but it can buy some (AgentTactics.WantsFood)
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
}
