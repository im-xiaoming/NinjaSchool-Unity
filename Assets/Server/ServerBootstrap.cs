using NsoServer;
using UnityEngine;

// Starts the embedded server when the game starts. The client talks to it in memory (GameServer.Connect), no network.
public static class ServerBootstrap
{
    private static GameServer server;
    public static GameServer Server { get { return server; } }

    // Dev wiki (lookup website for items / quests / NPCs / what is ported), Editor and development builds only.
    private static NsoServer.DevWiki.WikiServer wiki;
    public static string WikiUrl { get { return wiki != null ? wiki.Url : null; } }
    private static volatile bool wikiOpenRequested;   // set by the wiki thread when "open in browser" is on

    // Main thread: true once if the wiki is up and should be opened in the browser (Application.OpenURL is main-thread only).
    public static bool TakeWikiOpenRequest()
    {
        if (!wikiOpenRequested) return false;
        wikiOpenRequested = false;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (server != null) return;
        // Register cleanup before startup so a partial initialization is also cleaned up.
        Application.quitting -= Shutdown;
        Application.quitting += Shutdown;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
#endif
        try
        {
            server = new GameServer(new ServerConfig
            {
                DataDirectory = NsoServerControl.DataFolder
            });
            // only problems reach the Console; the per-command trace is too noisy
            server.Logger = text => { if (text.IndexOf("error", System.StringComparison.OrdinalIgnoreCase) >= 0) Debug.LogWarning("[NsoServer] " + text); };
            string staticDir = NsoServerControl.StaticFolder;
            server.Static = new NsoServer.Net.FileStaticData(staticDir);
            var tables = NsoServer.Data.TableFile.Load(System.IO.Path.Combine(staticDir, "tables.txt"));
            server.World = new NsoServer.World.GameWorld(tables, System.IO.Path.Combine(staticDir, "Data"));
            server.Start();
            StartWiki(tables, server.World);
        }
        catch (System.Exception ex)
        {
            Shutdown();
            Debug.LogError("[NsoServer] Embedded server could not start: " + ex.Message);
        }
    }

    // Starts the dev wiki next to the game. A failure here only logs a warning: the game itself is not affected.
    private static void StartWiki(System.Collections.Generic.Dictionary<string, NsoServer.Data.SqlTable> tables, NsoServer.World.GameWorld world)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!NsoServerControl.WikiEnabled) return;
        try
        {
            string folder = System.IO.Path.Combine(Application.streamingAssetsPath, "NsoWiki");
            string notes = System.IO.Path.Combine(folder, "notes.json");
            // Editor: also write the database to <project>/NsoWiki/db.json so it can be searched without the browser
            string export = Application.isEditor ? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "NsoWiki", "db.json")) : null;
            wiki = new NsoServer.DevWiki.WikiServer(folder, NsoServerControl.WikiPort,
                () => NsoServer.DevWiki.WikiData.Build(tables, world, notes), export);
            wiki.Control = new NsoServer.DevWiki.WikiControl(server);
            wiki.Logger = text => Debug.LogWarning("[NsoWiki] " + text);   // Debug.Log* is safe from other threads
            bool open = NsoServerControl.WikiOpenBrowser;
            // runs on the wiki thread; opening the browser is left to the main thread (NsoServerControl.Update)
            wiki.Started = url => { Debug.Log("[NsoWiki] Dev wiki: " + url); if (open) wikiOpenRequested = true; };
            wiki.Start();   // returns at once: binding, data building and requests all run on the wiki's own thread
        }
        catch (System.Exception ex)
        {
            wiki = null;
            Debug.LogWarning("[NsoWiki] Dev wiki could not start: " + ex.Message);
        }
#endif
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) Shutdown();
    }
#endif

    private static void Shutdown()
    {
        Application.quitting -= Shutdown;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
#endif
        if (wiki != null) { wiki.Stop(); wiki = null; }
        if (server == null) return;
        // Mark the client disconnected before the server closes its stream.
        // Otherwise the collector treats normal game shutdown as an unexpected disconnect.
        Session_ME.isCancel = false;
        Session_ME.huyLock();
        Session_ME.gI().close();
        GameServer stoppingServer = server;
        server = null;
        stoppingServer.Stop();
    }
}
