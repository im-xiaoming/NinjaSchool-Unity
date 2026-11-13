namespace NsoServer.Ai
{
    /// <summary>
    /// Tunables of the offline AI players (no Java counterpart). The AI players are real characters on bot sessions:
    /// every game rule (quests and map unlocks, bag, shops, potions, drops, experience, cooldowns, MP, death) is the
    /// player's rule, so nothing here changes how the game treats them; these values only shape how they behave.
    /// NsoServerControl copies its Inspector values here before the server starts (ApplyAgents); the dotnet test build
    /// keeps these defaults but never starts the AI players (AgentManager.Start is only called by ServerBootstrap).
    /// </summary>
    public static class AgentConfig
    {
        // ------------------------------------------------------------------ population
        /// <summary>Start the AI players at all.</summary>
        public static bool Enabled = true;
        /// <summary>Number of AI players (at most the number of personalities, AgentPersonality.Presets).</summary>
        public static int AgentCount = 13;
        /// <summary>Account names are this prefix + the character name (password: Controller.BotPassword).</summary>
        public const string AccountPrefix = "ai";
        /// <summary>Seconds between two AI players entering the game (a crowd does not log in at the same instant).</summary>
        public static int LoginSpacingSeconds = 2;
        // ------------------------------------------------------------------ tactical layer (game AI, how a player plays)
        /// <summary>Movement intent distance per 500 ms. Client-sized execution also respects normal Speed and terrain.</summary>
        public static int MoveStepPx = 60;
        /// <summary>Milliseconds between two position reports while moving (Char.sendMove: 250). The bot moves every
        /// client frame locally; reporting every frame made the other clients turn the bot back and forth.</summary>
        public static int MoveSendMilliseconds = 250;
        /// <summary>Seconds a dead AI player lies on the ground before pressing "return to the village" (ME_BACK).</summary>
        public static int ReviveAfterSeconds = 8;
        /// <summary>Default HP percent below which the AI drinks a potion or, without one, retreats (the plan may change it).</summary>
        public static int DefaultFleeHpPercent = 30;
        /// <summary>MP percent below which an MP potion is used.</summary>
        public static int DrinkMpPercent = 15;
        /// <summary>HP and MP potions an AI player tries to carry; it goes shopping when it has fewer than half.</summary>
        public static int PotionStock = 10;
        /// <summary>HP potions below which the rules always restock before fighting (when the shop is open and affordable).</summary>
        public static int MinPotionsToFight = 3;
        /// <summary>HP percent at which an AI player without potions but with money turns back to buy some.</summary>
        public static int NoPotionFleeHpPercent = 55;
        /// <summary>Servings of food (Tabemono: HP and MP every half second for 30 min) an AI player keeps in the bag;
        /// 0 = never buy food (it still eats food it has). It eats whenever no food effect is running.</summary>
        public static int FoodStock = 2;
        /// <summary>Yen/xu kept for potions: food is only bought with the money left above this.</summary>
        public static int FoodMoneyReserve = 300;
        /// <summary>Free bag slots an AI player keeps; below this it sells stones and gear it will never wear (0 = never sells).</summary>
        public static int BagReserveSlots = 3;
        /// <summary>Fast travel (carriage NPC Umayaki, Khả di lệnh scrolls) only when it saves at least this many map changes
        /// compared with walking (0 = never use fast travel).</summary>
        public static int FastTravelMinSavedHops = 2;
        /// <summary>Extra map changes a single-use Khả di lệnh is worth: it is consumed, the unlimited one is not.</summary>
        public static int FastTravelScrollCostHops = 2;
        /// <summary>A step fails after this many seconds without progress.</summary>
        public static int StepTimeoutSeconds = 300;
        /// <summary>A hunt step fails when no suitable monster was found for this many seconds.</summary>
        public static int NoTargetFailSeconds = 25;
        /// <summary>Hostile monsters closer than this (px) make an AI without potions back away (MobBrain hits within 301 px).</summary>
        public static int SafeDistancePx = 340;
        /// <summary>Chance (percent) that a sociable AI player greets a player who enters its zone.</summary>
        public static int GreetPercent = 40;
        // Offline behavioural preferences only; pickup ownership/expiry and zone access are shared server rules.
        public static int LootScanRadiusPx = 600, LootVerticalRangePx = 144, LootStallSeconds = 4, LootRetrySeconds = 8;
        /// <summary>Seconds a monster the path finder could not reach is left alone (BotNavigation).</summary>
        public static int UnreachableRetrySeconds = 20;
        public static bool SpreadHuntingZones = true;
        public static int ZoneCheckSeconds = 10, ZoneChangeCooldownSeconds = 45, ZoneApproachTimeoutSeconds = 30;
        public static int ZoneCrowdingThreshold = 3, ZonePopulationImprovement = 2;
        // ------------------------------------------------------------------ strategic layer
        /// <summary>Seconds between two decisions of one AI player (event-driven requests are throttled to this).</summary>
        public static int MinDecisionSeconds = 15;
        /// <summary>Seconds after which a running plan is reviewed even without an event (0 = never).</summary>
        public static int ReviewSeconds = 300;
        /// <summary>Events remembered per AI player (sent to the LLM as memory; saved).</summary>
        public static int MemorySize = 20;
        /// <summary>Maps (within this many waypoint hops) offered to the planner.</summary>
        public static int OptionHops = 3;
        /// <summary>Seconds between two saves of the AI memory (the characters themselves are saved like players).</summary>
        public static int SaveSeconds = 60;
        // ------------------------------------------------------------------ local LLM (OpenAI-compatible chat completions)
        /// <summary>Ask the local LLM for advice/speech; RL and its rule teacher decide independently of availability.</summary>
        public static bool LlmEnabled = true;
        /// <summary>Local Python Brain API. Auto-start is development-only and never installs dependencies.</summary>
        public static string BrainEndpoint = "http://127.0.0.1:8797";
        public static bool BrainAutoStart = true;
        /// <summary>Lease for abandoned sessions on an externally managed Brain service (seconds).</summary>
        public static int BrainSessionIdleSeconds = 600;
        /// <summary>The LLM leads: its validated plan is what the AI player does (rules/RL only fill the gap while it
        /// thinks or when it is unavailable). Off = the LLM only advises RL and speaks.</summary>
        public static bool LlmLeads = true;
        /// <summary>Seconds without any result (kill, pickup, quest step, new map...) after which the LLM is asked to step in
        /// with a different approach (0 = never). Its plan replaces the stuck one.</summary>
        public static int LlmStallSeconds = 90;
        /// <summary>Other maps whose remembered NPC positions are sent to the LLM (most recently visited first).</summary>
        public static int NpcMapsInPrompt = 12;
        // ------------------------------------------------------------------ long-term memory (RAG) and context budget
        /// <summary>Store knowledge, events and the guide in a local Qdrant collection and send the LLM only the relevant
        /// ones (AgentRag). Off or unreachable: the knowledge goes into the prompt as before.</summary>
        public static bool RagEnabled = true;
        public static string QdrantUrl = "http://127.0.0.1:6333";
        public static string RagCollection = "nso_agent_memory";
        /// <summary>Ollama embedding model and endpoint (bge-m3: multilingual, 1024 dimensions).</summary>
        public static string EmbedModel = "bge-m3";
        public static string EmbedEndpoint = "http://127.0.0.1:11434/api/embed";
        /// <summary>Memories recalled per decision (the AI player's own facts and events).</summary>
        public static int RagTopK = 8;
        /// <summary>Put the whole game (items: how to get them, what using them does, whether usable offline; monsters,
        /// maps, NPCs, shops, quests, skills) into the shared memory (GameKnowledge) and recall this many per decision.</summary>
        public static bool RagGameKnowledge = true;
        public static int RagKnowledgeTopK = 4;
        /// <summary>Longest recalled memory in the prompt (characters); longer game documents are cut.</summary>
        public static int RagMemoryChars = 600;
        /// <summary>The LLM may set the rule planner's priorities (quest/hunt/shop/explore/collect/idle multipliers) for
        /// the decisions the rules make while it is busy; they expire after this many minutes (0: the LLM cannot set them).</summary>
        public static int LlmPriorityMinutes = 15;
        /// <summary>Most recent events kept in the prompt once older ones are summarised / recalled.</summary>
        public static int ContextEvents = 8;
        /// <summary>Context window of the LLM server in tokens (Ollama runs qwen3.5 with 4096 unless OLLAMA_CONTEXT_LENGTH
        /// is set). When system + prompt + answer would not fit, older events are summarised, then the terrain is dropped.</summary>
        public static int LlmContextTokens = 4096;
        /// <summary>Characters per token used to estimate the prompt size (measured about 2.7 for these prompts).</summary>
        public static double CharsPerToken = 2.5;
        /// <summary>Longest "story so far" the LLM writes when it summarises older events.</summary>
        public static int SummaryWords = 80;
        /// <summary>Seconds after which the LLM reviews a running plan when it leads (instead of ReviewSeconds).</summary>
        public static int LlmReviewSeconds = 120;
        /// <summary>Chat completions URL. Ollama: http://127.0.0.1:11434/v1/chat/completions, LM Studio: http://127.0.0.1:1234/v1/chat/completions.</summary>
        public static string LlmEndpoint = "http://127.0.0.1:11434/v1/chat/completions";
        /// <summary>Model name as the local server knows it.</summary>
        public static string LlmModel = "qwen3.5:4b";
        /// <summary>Optional bearer token (most local servers ignore it).</summary>
        public static string LlmApiKey = "";
        /// <summary>Refuse endpoints that are not on this machine (the game is offline; nothing leaves the PC).</summary>
        public static bool LlmLocalOnly = true;
        public static int LlmTimeoutSeconds = 40;
        public static float LlmTemperature = 0.7f;
        public static int LlmMaxTokens = 500;
        /// <summary>OpenAI "reasoning_effort" sent with the request; "none" turns off the thinking of Qwen3-style models
        /// (otherwise they spend the whole token budget thinking and return no content). Empty = not sent.</summary>
        public static string LlmReasoningEffort = "none";
        /// <summary>Send response_format {"type":"json_object"} (Ollama, llama.cpp). Turn off for servers that reject it.</summary>
        public static bool LlmJsonMode = true;
        /// <summary>Use strict JSON Schema through response_format; disable for older compatible servers.</summary>
        public static bool LlmStructuredOutputs = true;
        public static int LlmFormatBackoffSeconds = 5;
        /// <summary>After a failed request the LLM is not asked again for this many seconds (rules decide meanwhile).</summary>
        public static int LlmBackoffSeconds = 60;
        /// <summary>Pending LLM requests (one per AI player at most).</summary>
        public static int LlmMaxQueue = 16;
        /// <summary>Basic instructions given to the LLM at the start of every conversation (a beginner's guide to the
        /// game, appended to LlmPlanner.SystemPrompt). Editable in NSO Server Control (_llmGuide); empty = none.</summary>
        public static string LlmGuide = DefaultGuide;
        public const string DefaultGuide = "BEGINNER'S GUIDE:\n" + "- You start in Làng Tone. Follow the main quests of the village chief Tajima in order: they teach the basics, give " + "rewards and unlock new maps. When unsure, choose the quest.\n" + "- Without a weapon you cannot fight: Tajima gives a wooden sword.\n" + "- Shops open after quest 3 (NV diệt sên trừ cóc). Kiriko sells HP/MP potions, Tabemono food. Carry potions before long hunts.\n" + "- Monsters drop yen (about one kill in ten) and sometimes items; drops are owner-only for 30 seconds, then public until they expire.\n" + "- Train on monsters from your level to about 3 levels above. Far stronger ones kill you; ones 10 levels below give no experience.\n" + "- At level 10, after quest 8 (visit the 3 schools), join your school: Hirosaki (kiếm, phi tiêu), Ookaza (kunai, cung) or " + "Haruna (đao, quạt). Then read the school's book and use its weapon.\n" + "- Potential and skill points are spent for you. When a quest step needs a level, hunt until you reach it.\n" + "- If you die you return to the village with full HP; learn from it and avoid that monster for a while.";
    }
}
