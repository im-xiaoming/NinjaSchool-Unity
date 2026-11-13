using System.IO;
using UnityEngine;
using NsoServer.Configuration;

// Hierarchy object "NSO Server Control": shows the embedded server's state and where the save data lives.
public class NsoServerControl : MonoBehaviour
{
    [Header("Server paths — apply on next Play")]
    [Tooltip("Relative folder under Application.persistentDataPath. Changing it does not migrate or delete saves.")]
    [SerializeField]
    private string _saveSubfolder = "NsoData";
    [Tooltip("Save accounts, characters (bag, quests, progress) and AI agents in the SQLite database nso.db. Off: the former users.tsv / players.tsv files. Old TSV saves are imported into a new database once.")]
    [SerializeField]
    private bool _useSqlite = true;
    public static bool UseSqlite
    {
        get
        {
            return RuntimeConfiguration.Value("server", "_useSqlite", Active == null || Active._useSqlite);
        }
    }

    [Tooltip("Relative folder under StreamingAssets containing tables.txt and Data.")]
    [SerializeField]
    private string _staticSubfolder = "NsoStatic";
    [Header("Dev wiki (Editor / development builds) — apply on next Play")]
    [Tooltip("Start the lookup website (items, quests, NPCs, maps, ported/not ported) together with the game, on 127.0.0.1 only.")]
    [SerializeField]
    private bool _wikiEnabled = true;
    [Tooltip("First port to try; the next 9 are tried if it is taken.")]
    [SerializeField]
    private int _wikiPort = 8787;
    [Tooltip("Open the wiki in the default browser when the game starts.")]
    [SerializeField]
    private bool _wikiOpenBrowser = false;
    [Header("Player log (debug) — apply on next Play")]
    [Tooltip("Write every player action (client message), server reply, server trace line and periodic state snapshot to a text file. Editor: <project>/Logs/PlayerLog; build: <persistentDataPath>/Logs/PlayerLog.")]
    [SerializeField]
    private bool _playerLogEnabled = true;
    [Tooltip("Write the log into the SQLite database Logs/PlayerLog/player_log.db (one session per Play; table entries).")]
    [SerializeField]
    private bool _playerLogToDatabase = true;
    [Tooltip("Also write the classic text file player_<time>.log.")]
    [SerializeField]
    private bool _playerLogToFile = false;
    [Tooltip("Also log every server-to-client message (much larger files).")]
    [SerializeField]
    private bool _playerLogOutgoing = true;
    [Tooltip("Seconds between two full state snapshots of the character (0 = none).")]
    [SerializeField, Range(0, 60)]
    private int _playerLogSnapshotSeconds = 5;
    [Tooltip("Payload bytes written in hex per message.")]
    [SerializeField, Range(0, 512)]
    private int _playerLogPayloadBytes = 48;
    [Tooltip("Number of log sessions kept in the database / text files kept (one per Play session); older ones are deleted.")]
    [SerializeField, Range(1, 200)]
    private int _playerLogKeepFiles = 20;
    [Header("Ground drops — shared by every character, next Play")]
    [SerializeField, Range(0, 600)]
    private int _groundLootOwnerSeconds = 30;
    [SerializeField, Range(1, 3600)]
    private int _groundLootLifetimeSeconds = 60;
    public static void ApplyGroundLoot()
    {
        NsoServer.World.GroundLootPolicy.Configure(RuntimeConfiguration.Value("server", "_groundLootOwnerSeconds", Active != null ? Active._groundLootOwnerSeconds : 30), RuntimeConfiguration.Value("server", "_groundLootLifetimeSeconds", Active != null ? Active._groundLootLifetimeSeconds : 60));
    }

    [Header("AI players — apply on next Play")]
    [Tooltip("Movement intent distance per 500 ms; execution is also capped by the character's normal speed and terrain collision.")]
    [SerializeField, Range(12, 120)]
    private int _aiMoveStepPx = 60;
    [Tooltip("AI players: real characters (own account in the save database) that log in and play by the player's rules; a local LLM decides goals, game AI plays them.")]
    [SerializeField]
    private bool _agentsEnabled = true;
    [Tooltip("Number of AI players (at most 13 personalities). They start as new characters (level 1, quest 0).")]
    [SerializeField, Range(0, 13)]
    private int _aiPlayerCount = 13;
    [Tooltip("Behaviour preferences only. Ownership, bag capacity and NPC/zone access still use normal player rules.")]
    [SerializeField, Range(150, 2000)]
    private int _aiLootScanRadius = 600;
    [SerializeField, Range(24, 600)]
    private int _aiLootVerticalRange = 144;
    [SerializeField, Range(1, 30)]
    private int _aiLootStallSeconds = 4;
    [SerializeField, Range(1, 60)]
    private int _aiLootRetrySeconds = 8;
    [SerializeField]
    private bool _aiSpreadHuntingZones = true;
    [SerializeField, Range(1, 120)]
    private int _aiZoneCheckSeconds = 10;
    [SerializeField, Range(5, 300)]
    private int _aiZoneChangeCooldownSeconds = 45;
    [SerializeField, Range(5, 120)]
    private int _aiZoneApproachTimeoutSeconds = 30;
    [SerializeField, Range(2, 20)]
    private int _aiZoneCrowdingThreshold = 3;
    [SerializeField, Range(1, 10)]
    private int _aiZonePopulationImprovement = 2;
    [Tooltip("Days the AI players' log lines (table bot_entries of player_log.db) are kept.")]
    [SerializeField, Range(1, 365)]
    private int _botLogKeepDays = 20;
    [Tooltip("Write the AI players' actions, screen messages, decisions and states to the log database.")]
    [SerializeField]
    private bool _botLogEnabled = true;
    [Tooltip("Seconds between two strategic decisions of one agent (decisions are event-driven; this is the throttle).")]
    [SerializeField, Range(5, 300)]
    private int _agentMinDecisionSeconds = 15;
    [Tooltip("Seconds after which a running plan is reviewed even without an event (0 = never).")]
    [SerializeField, Range(0, 1800)]
    private int _agentReviewSeconds = 300;
    [Tooltip("Ask the local LLM for advice and speech. RL and its rule teacher continue if it is off or unreachable.")]
    [SerializeField]
    private bool _llmEnabled = true;
    [Tooltip("The LLM leads: its plan is what the AI player does; rules/RL only fill in while it thinks or when it is unavailable.")]
    [SerializeField]
    private bool _llmLeads = true;
    [Tooltip("Seconds after which the LLM reviews a running plan when it leads.")]
    [SerializeField, Min(30)]
    private int _llmReviewSeconds = 120;
    [Tooltip("AI players report their position at most this often while moving (Char.sendMove: 250 ms).")]
    [SerializeField, Range(42, 1000)]
    private int _aiMoveSendMilliseconds = 250;
    [Tooltip("AI rules restock HP potions before fighting when they carry fewer than this (shop open and affordable).")]
    [SerializeField, Range(0, 20)]
    private int _aiMinPotionsToFight = 3;
    [Tooltip("HP percent at which an AI player without potions, but with money, turns back to buy some.")]
    [SerializeField, Range(0, 95)]
    private int _aiNoPotionFleeHpPercent = 55;
    [Tooltip("Long-term memory of the AI players in a local Qdrant collection; the LLM gets only the relevant memories. Off or unreachable: all knowledge goes into the prompt.")]
    [SerializeField]
    private bool _ragEnabled = true;
    [Tooltip("Food servings (Tabemono) an AI player keeps; it eats whenever no food effect runs. 0 = never buys food.")]
    [SerializeField, Range(0, 10)]
    private int _aiFoodStock = 2;
    [Tooltip("Yen/xu an AI player keeps for potions: food is bought only with the money above this.")]
    [SerializeField, Min(0)]
    private int _aiFoodMoneyReserve = 300;
    [Tooltip("Free bag slots an AI player keeps: below this it sells stones and gear it will not wear (finishing quests and joining a school need free slots). 0 = never sells.")]
    [SerializeField, Range(0, 10)]
    private int _aiBagReserveSlots = 3;
    [Tooltip("AI fast travel (carriage NPC Umayaki, Khả di lệnh scrolls) only when it saves at least this many map changes over walking. 0 = never.")]
    [SerializeField, Range(0, 20)]
    private int _aiFastTravelMinSavedHops = 2;
    [Tooltip("Extra map changes a single-use Khả di lệnh counts as (it is consumed; the unlimited scroll is not).")]
    [SerializeField, Range(0, 20)]
    private int _aiFastTravelScrollCostHops = 2;
    [Tooltip("Put the whole game (items, how to get them, what using them does, monsters, maps, NPCs, shops, quests, skills) into the RAG memory.")]
    [SerializeField]
    private bool _ragGameKnowledge = true;
    [Tooltip("Game-knowledge documents recalled into each LLM prompt (besides the AI player's own memories).")]
    [SerializeField, Range(0, 12)]
    private int _ragKnowledgeTopK = 4;
    [Tooltip("Longest recalled memory in the prompt, in characters (long documents are cut to fit the context).")]
    [SerializeField, Range(80, 3000)]
    private int _ragMemoryChars = 600;
    [Tooltip("Minutes the LLM's priorities (quest/hunt/shop/explore/collect/idle weights) steer the rule planner; 0 = the LLM cannot set them.")]
    [SerializeField, Range(0, 120)]
    private int _llmPriorityMinutes = 15;
    [Tooltip("Local Qdrant URL (the collection nso_agent_memory is created there).")]
    [SerializeField]
    private string _qdrantUrl = "http://127.0.0.1:6333";
    [Tooltip("Ollama embedding model for the memories (bge-m3: multilingual).")]
    [SerializeField]
    private string _embedModel = "bge-m3";
    [Tooltip("Context window of the LLM server in tokens (Ollama default 4096). Older events are summarised when the prompt would not fit.")]
    [SerializeField, Min(1024)]
    private int _llmContextTokens = 4096;
    [Tooltip("OpenAI-compatible chat completions URL. Ollama: http://127.0.0.1:11434/v1/chat/completions; LM Studio: http://127.0.0.1:1234/v1/chat/completions.")]
    [SerializeField]
    private string _llmEndpoint = "http://127.0.0.1:11434/v1/chat/completions";
    [Tooltip("Model name as the local server knows it (ollama list), for example qwen3.5:4b.")]
    [SerializeField]
    private string _llmModel = "qwen3.5:4b";
    [Tooltip("reasoning_effort sent to the LLM. \"none\" turns off the thinking of Qwen3-style models (needed: otherwise the answer is empty). Empty = not sent.")]
    [SerializeField]
    private string _llmReasoningEffort = "none";
    [Tooltip("Refuse endpoints that are not on this machine.")]
    [SerializeField]
    private bool _llmLocalOnly = true;
    [Tooltip("Send response_format json_object (Ollama, llama.cpp). Turn off if the server rejects it (some LM Studio versions).")]
    [SerializeField]
    private bool _llmJsonMode = true;
    [Tooltip("Constrain the model with JSON Schema (Ollama). Disable if the local server rejects json_schema.")]
    [SerializeField]
    private bool _llmStructuredOutputs = true;
    [SerializeField, Range(0, 60)]
    private int _llmFormatBackoffSeconds = 5;
    [Tooltip("Seconds before an LLM request is abandoned.")]
    [SerializeField, Range(5, 180)]
    private int _llmTimeoutSeconds = 40;
    [Tooltip("After a failed request the LLM is not asked again for this many seconds.")]
    [SerializeField, Range(0, 600)]
    private int _llmBackoffSeconds = 60;
    [Tooltip("Sampling temperature of the LLM.")]
    [SerializeField, Range(0f, 1.5f)]
    private float _llmTemperature = 0.7f;
    [Tooltip("Basic instructions given to the LLM at the start (a beginner's guide to the game, added to the system prompt). Empty = none.")]
    [SerializeField, TextArea(4, 14)]
    private string _llmGuide = NsoServer.Ai.AgentConfig.DefaultGuide;
    /// <summary>Copies the agent settings into the server (ServerBootstrap, before AgentManager.Start).</summary>
    public static void ApplyAgents()
    {
        NsoServer.Ai.AgentConfig.Enabled = RuntimeConfiguration.Value("server", "_agentsEnabled", Active != null ? Active._agentsEnabled : true);
        NsoServer.Ai.AgentConfig.AgentCount = RuntimeConfiguration.Value("server", "_aiPlayerCount", Active != null ? Active._aiPlayerCount : 13);
        NsoServer.Ai.AgentConfig.MoveStepPx = RuntimeConfiguration.Value("server", "_aiMoveStepPx", Active != null ? Active._aiMoveStepPx : 60);
        NsoServer.Ai.AgentConfig.LootScanRadiusPx = RuntimeConfiguration.Value("server", "_aiLootScanRadius", Active != null ? Active._aiLootScanRadius : 600);
        NsoServer.Ai.AgentConfig.LootVerticalRangePx = RuntimeConfiguration.Value("server", "_aiLootVerticalRange", Active != null ? Active._aiLootVerticalRange : 144);
        NsoServer.Ai.AgentConfig.LootStallSeconds = RuntimeConfiguration.Value("server", "_aiLootStallSeconds", Active != null ? Active._aiLootStallSeconds : 4);
        NsoServer.Ai.AgentConfig.LootRetrySeconds = RuntimeConfiguration.Value("server", "_aiLootRetrySeconds", Active != null ? Active._aiLootRetrySeconds : 8);
        NsoServer.Ai.AgentConfig.SpreadHuntingZones = RuntimeConfiguration.Value("server", "_aiSpreadHuntingZones", Active != null ? Active._aiSpreadHuntingZones : true);
        NsoServer.Ai.AgentConfig.ZoneCheckSeconds = RuntimeConfiguration.Value("server", "_aiZoneCheckSeconds", Active != null ? Active._aiZoneCheckSeconds : 10);
        NsoServer.Ai.AgentConfig.ZoneChangeCooldownSeconds = RuntimeConfiguration.Value("server", "_aiZoneChangeCooldownSeconds", Active != null ? Active._aiZoneChangeCooldownSeconds : 45);
        NsoServer.Ai.AgentConfig.ZoneApproachTimeoutSeconds = RuntimeConfiguration.Value("server", "_aiZoneApproachTimeoutSeconds", Active != null ? Active._aiZoneApproachTimeoutSeconds : 30);
        NsoServer.Ai.AgentConfig.ZoneCrowdingThreshold = RuntimeConfiguration.Value("server", "_aiZoneCrowdingThreshold", Active != null ? Active._aiZoneCrowdingThreshold : 3);
        NsoServer.Ai.AgentConfig.ZonePopulationImprovement = RuntimeConfiguration.Value("server", "_aiZonePopulationImprovement", Active != null ? Active._aiZonePopulationImprovement : 2);
        NsoServer.Ai.AgentConfig.MinDecisionSeconds = RuntimeConfiguration.Value("server", "_agentMinDecisionSeconds", Active != null ? Active._agentMinDecisionSeconds : 15);
        NsoServer.Ai.AgentConfig.ReviewSeconds = RuntimeConfiguration.Value("server", "_agentReviewSeconds", Active != null ? Active._agentReviewSeconds : 300);
        NsoServer.Ai.AgentConfig.LlmEnabled = RuntimeConfiguration.Value("server", "_llmEnabled", Active != null ? Active._llmEnabled : true);
        NsoServer.Ai.AgentConfig.LlmLeads = RuntimeConfiguration.Value("server", "_llmLeads", Active != null ? Active._llmLeads : true);
        NsoServer.Ai.AgentConfig.LlmReviewSeconds = RuntimeConfiguration.Value("server", "_llmReviewSeconds", Active != null ? Active._llmReviewSeconds : 120);
        NsoServer.Ai.AgentConfig.MoveSendMilliseconds = RuntimeConfiguration.Value("server", "_aiMoveSendMilliseconds", Active != null ? Active._aiMoveSendMilliseconds : 250);
        NsoServer.Ai.AgentConfig.MinPotionsToFight = RuntimeConfiguration.Value("server", "_aiMinPotionsToFight", Active != null ? Active._aiMinPotionsToFight : 3);
        NsoServer.Ai.AgentConfig.NoPotionFleeHpPercent = RuntimeConfiguration.Value("server", "_aiNoPotionFleeHpPercent", Active != null ? Active._aiNoPotionFleeHpPercent : 55);
        NsoServer.Ai.AgentConfig.RagEnabled = RuntimeConfiguration.Value("server", "_ragEnabled", Active != null ? Active._ragEnabled : true);
        NsoServer.Ai.AgentConfig.FoodStock = RuntimeConfiguration.Value("server", "_aiFoodStock", Active != null ? Active._aiFoodStock : 2);
        NsoServer.Ai.AgentConfig.FoodMoneyReserve = RuntimeConfiguration.Value("server", "_aiFoodMoneyReserve", Active != null ? Active._aiFoodMoneyReserve : 300);
        NsoServer.Ai.AgentConfig.BagReserveSlots = RuntimeConfiguration.Value("server", "_aiBagReserveSlots", Active != null ? Active._aiBagReserveSlots : 3);
        NsoServer.Ai.AgentConfig.FastTravelMinSavedHops = RuntimeConfiguration.Value("server", "_aiFastTravelMinSavedHops", Active != null ? Active._aiFastTravelMinSavedHops : 2);
        NsoServer.Ai.AgentConfig.FastTravelScrollCostHops = RuntimeConfiguration.Value("server", "_aiFastTravelScrollCostHops", Active != null ? Active._aiFastTravelScrollCostHops : 2);
        NsoServer.Ai.AgentConfig.RagGameKnowledge = RuntimeConfiguration.Value("server", "_ragGameKnowledge", Active != null ? Active._ragGameKnowledge : true);
        NsoServer.Ai.AgentConfig.RagKnowledgeTopK = RuntimeConfiguration.Value("server", "_ragKnowledgeTopK", Active != null ? Active._ragKnowledgeTopK : 4);
        NsoServer.Ai.AgentConfig.RagMemoryChars = RuntimeConfiguration.Value("server", "_ragMemoryChars", Active != null ? Active._ragMemoryChars : 600);
        NsoServer.Ai.AgentConfig.LlmPriorityMinutes = RuntimeConfiguration.Value("server", "_llmPriorityMinutes", Active != null ? Active._llmPriorityMinutes : 15);
        NsoServer.Ai.AgentConfig.QdrantUrl = RuntimeConfiguration.Value("server", "_qdrantUrl", Active != null ? Active._qdrantUrl : "http://127.0.0.1:6333");
        NsoServer.Ai.AgentConfig.EmbedModel = RuntimeConfiguration.Value("server", "_embedModel", Active != null ? Active._embedModel : "bge-m3");
        NsoServer.Ai.AgentConfig.LlmContextTokens = RuntimeConfiguration.Value("server", "_llmContextTokens", Active != null ? Active._llmContextTokens : 4096);
        NsoServer.Ai.AgentConfig.LlmEndpoint = RuntimeConfiguration.Value("server", "_llmEndpoint", Active != null ? Active._llmEndpoint : "http://127.0.0.1:11434/v1/chat/completions");
        NsoServer.Ai.AgentConfig.LlmModel = RuntimeConfiguration.Value("server", "_llmModel", Active != null ? Active._llmModel : "qwen3.5:4b");
        NsoServer.Ai.AgentConfig.LlmReasoningEffort = RuntimeConfiguration.Value("server", "_llmReasoningEffort", Active != null ? Active._llmReasoningEffort : "none");
        NsoServer.Ai.AgentConfig.LlmLocalOnly = RuntimeConfiguration.Value("server", "_llmLocalOnly", Active != null ? Active._llmLocalOnly : true);
        NsoServer.Ai.AgentConfig.LlmJsonMode = RuntimeConfiguration.Value("server", "_llmJsonMode", Active != null ? Active._llmJsonMode : true);
        NsoServer.Ai.AgentConfig.LlmStructuredOutputs = RuntimeConfiguration.Value("server", "_llmStructuredOutputs", Active != null ? Active._llmStructuredOutputs : true);
        NsoServer.Ai.AgentConfig.LlmFormatBackoffSeconds = RuntimeConfiguration.Value("server", "_llmFormatBackoffSeconds", Active != null ? Active._llmFormatBackoffSeconds : 5);
        NsoServer.Ai.AgentConfig.LlmTimeoutSeconds = RuntimeConfiguration.Value("server", "_llmTimeoutSeconds", Active != null ? Active._llmTimeoutSeconds : 40);
        NsoServer.Ai.AgentConfig.LlmBackoffSeconds = RuntimeConfiguration.Value("server", "_llmBackoffSeconds", Active != null ? Active._llmBackoffSeconds : 60);
        NsoServer.Ai.AgentConfig.LlmTemperature = RuntimeConfiguration.Value("server", "_llmTemperature", Active != null ? Active._llmTemperature : 0.7f);
        NsoServer.Ai.AgentConfig.LlmGuide = RuntimeConfiguration.Value("server", "_llmGuide", Active != null ? Active._llmGuide : NsoServer.Ai.AgentConfig.DefaultGuide);
    }

    /// <summary>Copies the player log settings into the server (ServerBootstrap, before the log opens).</summary>
    public static void ApplyPlayerLog()
    {
        NsoServer.Net.PlayerLog.Enabled = RuntimeConfiguration.Value("server", "_playerLogEnabled", Active != null ? Active._playerLogEnabled : true);
        NsoServer.Net.PlayerLog.LogOutgoing = RuntimeConfiguration.Value("server", "_playerLogOutgoing", Active != null ? Active._playerLogOutgoing : true);
        NsoServer.Net.PlayerLog.SnapshotSeconds = RuntimeConfiguration.Value("server", "_playerLogSnapshotSeconds", Active != null ? Active._playerLogSnapshotSeconds : 5);
        NsoServer.Net.PlayerLog.MaxPayloadBytes = RuntimeConfiguration.Value("server", "_playerLogPayloadBytes", Active != null ? Active._playerLogPayloadBytes : 48);
        NsoServer.Net.PlayerLog.KeepFiles = RuntimeConfiguration.Value("server", "_playerLogKeepFiles", Active != null ? Active._playerLogKeepFiles : 20);
        NsoServer.Net.PlayerLog.ToDatabase = RuntimeConfiguration.Value("server", "_playerLogToDatabase", Active != null ? Active._playerLogToDatabase : true);
        NsoServer.Net.PlayerLog.ToFile = RuntimeConfiguration.Value("server", "_playerLogToFile", Active != null ? Active._playerLogToFile : false);
        NsoServer.Net.PlayerLog.BotKeepDays = RuntimeConfiguration.Value("server", "_botLogKeepDays", Active != null ? Active._botLogKeepDays : 20);
        NsoServer.Net.PlayerLog.LogBots = RuntimeConfiguration.Value("server", "_botLogEnabled", Active != null ? Active._botLogEnabled : true);
    }

    public static string PlayerLogFolder
    {
        get
        {
            string root = Application.isEditor ? Path.Combine(Application.dataPath, "..") : Application.persistentDataPath;
            return Path.GetFullPath(Path.Combine(root, "Logs", "PlayerLog"));
        }
    }

    public static bool WikiEnabled
    {
        get
        {
            return RuntimeConfiguration.Value("server", "_wikiEnabled", Active != null ? Active._wikiEnabled : true);
        }
    }

    public static int WikiPort
    {
        get
        {
            return RuntimeConfiguration.Value("server", "_wikiPort", Active != null ? Active._wikiPort : 8787);
        }
    }

    public static bool WikiOpenBrowser
    {
        get
        {
            return RuntimeConfiguration.Value("server", "_wikiOpenBrowser", Active != null && Active._wikiOpenBrowser);
        }
    }

    public static NsoServerControl Active { get; private set; }

    public static string StaticFolder
    {
        get
        {
            return Resolve(Application.streamingAssetsPath, RuntimeConfiguration.Value("server", "_staticSubfolder", Active != null ? Active._staticSubfolder : "NsoStatic"));
        }
    }

    [Header("Runtime status")]
    public string status = "Not running";
    public string address = "";
    public string dataFolder = "";
    public string wikiUrl = "";
    public string playerLogFile = "";
    private static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            throw new System.ArgumentException("Server folder must be a non-empty relative path.");
        string basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(basePath, relative));
        if (!candidate.StartsWith(basePath, System.StringComparison.OrdinalIgnoreCase))
            throw new System.ArgumentException("Server folder must stay inside its data root.");
        return candidate;
    }

    private void Awake()
    {
        if (Active != null && Active != this)
        {
            Debug.LogError("Only one NSO Server Control may own configuration.", this);
            enabled = false;
            return;
        }

        Active = this;
        NsoConfigurationBridge.Register("server", this);
        try
        {
            dataFolder = DataFolder;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message, this);
        }
    }

    [ContextMenu("Export runtime configuration (never overwrite)")]
    private void ExportRuntimeConfiguration()
    {
        NsoConfigurationBridge.Export();
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    private float _learningFrames, _learningSeconds;
    private void Update()
    {
        _learningFrames++;
        _learningSeconds += UnityEngine.Time.unscaledDeltaTime;
        if (_learningSeconds >= 5f)
        {
            NsoServer.Ai.Rl.RlRuntime.Fps(_learningFrames / _learningSeconds);
            _learningFrames = _learningSeconds = 0;
        }

        NsoServer.GameServer server = ServerBootstrap.Server;
        status = server != null ? "Running" : "Not running";
        address = server != null ? "in-process (no network)" : "";
        wikiUrl = ServerBootstrap.WikiUrl ?? "";
        playerLogFile = NsoServer.Net.PlayerLog.DatabasePath != null ? NsoServer.Net.PlayerLog.DatabasePath + " (session " + NsoServer.Net.PlayerLog.SessionId + ")" + (NsoServer.Net.PlayerLog.FilePath != null ? " + " + NsoServer.Net.PlayerLog.FilePath : "") : NsoServer.Net.PlayerLog.FilePath ?? "";
        if (ServerBootstrap.TakeWikiOpenRequest())
            Application.OpenURL(wikiUrl); // the wiki itself runs on its own thread
    }

    public string ConfiguredDataFolder
    {
        get
        {
            return Resolve(Application.persistentDataPath, RuntimeConfiguration.Value("server", "_saveSubfolder", _saveSubfolder));
        }
    }

    public static string DataFolder
    {
        get
        {
            if (ServerBootstrap.Server != null)
                return ServerBootstrap.Server.Config.DataDirectory;
            return Resolve(Application.persistentDataPath, RuntimeConfiguration.Value("server", "_saveSubfolder", Active != null ? Active._saveSubfolder : "NsoData"));
        }
    }
}
