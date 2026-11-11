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
