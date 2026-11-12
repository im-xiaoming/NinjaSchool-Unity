using System.IO;
using UnityEngine;

// Hierarchy object "NSO Server Control": shows the embedded server's state and where the save data lives.
public class NsoServerControl : MonoBehaviour
{
    [Header("Server paths — apply on next Play")]
    [Tooltip("Relative folder under Application.persistentDataPath. Changing it does not migrate or delete saves.")]
    [SerializeField] private string _saveSubfolder = "NsoData";
    [Tooltip("Relative folder under StreamingAssets containing tables.txt and Data.")]
    [SerializeField] private string _staticSubfolder = "NsoStatic";
    [Header("Dev wiki (Editor / development builds) — apply on next Play")]
    [Tooltip("Start the lookup website (items, quests, NPCs, maps, ported/not ported) together with the game, on 127.0.0.1 only.")]
    [SerializeField] private bool _wikiEnabled = true;
    [Tooltip("First port to try; the next 9 are tried if it is taken.")]
    [SerializeField] private int _wikiPort = 8787;
    [Tooltip("Open the wiki in the default browser when the game starts.")]
    [SerializeField] private bool _wikiOpenBrowser = false;
    [Header("Player log (debug) — apply on next Play")]
    [Tooltip("Write every player action (client message), server reply, server trace line and periodic state snapshot to a text file. Editor: <project>/Logs/PlayerLog; build: <persistentDataPath>/Logs/PlayerLog.")]
    [SerializeField] private bool _playerLogEnabled = true;
    [Tooltip("Also log every server-to-client message (much larger files).")]
    [SerializeField] private bool _playerLogOutgoing = true;
    [Tooltip("Seconds between two full state snapshots of the character (0 = none).")]
    [SerializeField, Range(0, 60)] private int _playerLogSnapshotSeconds = 5;
    [Tooltip("Payload bytes written in hex per message.")]
    [SerializeField, Range(0, 512)] private int _playerLogPayloadBytes = 48;
    [Tooltip("Number of log files kept (one per Play session); older ones are deleted.")]
    [SerializeField, Range(1, 200)] private int _playerLogKeepFiles = 20;
    /// <summary>Copies the player log settings into the server (ServerBootstrap, before the log opens).</summary>
    public static void ApplyPlayerLog()
    {
        if (Active == null) return;
        NsoServer.Net.PlayerLog.Enabled = Active._playerLogEnabled;
        NsoServer.Net.PlayerLog.LogOutgoing = Active._playerLogOutgoing;
        NsoServer.Net.PlayerLog.SnapshotSeconds = Active._playerLogSnapshotSeconds;
        NsoServer.Net.PlayerLog.MaxPayloadBytes = Active._playerLogPayloadBytes;
        NsoServer.Net.PlayerLog.KeepFiles = Active._playerLogKeepFiles;
    }
    public static string PlayerLogFolder
    {
        get
        {
            string root = Application.isEditor ? Path.Combine(Application.dataPath, "..") : Application.persistentDataPath;
            return Path.GetFullPath(Path.Combine(root, "Logs", "PlayerLog"));
        }
    }
    public static bool WikiEnabled { get { return Active != null ? Active._wikiEnabled : true; } }
    public static int WikiPort { get { return Active != null ? Active._wikiPort : 8787; } }
    public static bool WikiOpenBrowser { get { return Active != null && Active._wikiOpenBrowser; } }
    public static NsoServerControl Active { get; private set; }
    public static string StaticFolder { get { return Resolve(Application.streamingAssetsPath, Active != null ? Active._staticSubfolder : "NsoStatic"); } }
    [Header("Runtime status")]
    public string status = "Not running";
    public string address = "";
    public string dataFolder = "";
    public string wikiUrl = "";
    public string playerLogFile = "";

    private static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new System.ArgumentException("Server folder must be a non-empty relative path.");
        string basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(basePath, relative));
        if (!candidate.StartsWith(basePath, System.StringComparison.OrdinalIgnoreCase)) throw new System.ArgumentException("Server folder must stay inside its data root.");
        return candidate;
    }
    private void Awake()
    {
        if (Active != null && Active != this) { Debug.LogError("Only one NSO Server Control may own configuration.", this); enabled = false; return; }
        Active = this;
        try { dataFolder = DataFolder; } catch (System.Exception ex) { Debug.LogError(ex.Message, this); }
    }
    private void OnDestroy() { if (Active == this) Active = null; }

    private void Update()
    {
        NsoServer.GameServer server = ServerBootstrap.Server;
        status = server != null ? "Running" : "Not running";
        address = server != null ? "in-process (no network)" : "";
        wikiUrl = ServerBootstrap.WikiUrl ?? "";
        playerLogFile = NsoServer.Net.PlayerLog.FilePath ?? "";
        if (ServerBootstrap.TakeWikiOpenRequest()) Application.OpenURL(wikiUrl);   // the wiki itself runs on its own thread
    }

    public string ConfiguredDataFolder { get { return Resolve(Application.persistentDataPath, _saveSubfolder); } }
    public static string DataFolder
    {
        get
        {
            if (ServerBootstrap.Server != null) return ServerBootstrap.Server.Config.DataDirectory;
            return Resolve(Application.persistentDataPath, Active != null ? Active._saveSubfolder : "NsoData");
        }
    }
}
