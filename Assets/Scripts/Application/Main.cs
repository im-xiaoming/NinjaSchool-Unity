using Functions.Auto;
using System.Threading;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static Main main;

    public static GameCanvas canvas;

    public static mGraphics g;

    public static GameMidlet midlet;

    public static string res = "res";

    public static string mainThreadName;

    public static bool started;

    public static bool isIpod;

    public static bool isAppTeam;

    public static bool isPC;

    public static bool isWp;

    public static bool IphoneVersionApp;

    public static string IMEI;

    public static int versionIp;

    public static int level;

    public bool isWorldver;
    public bool isLocal;

    private int updateCount;

    /// <summary>Offline: counts logic ticks (FixedUpdate), so paint code can tell a new tick from a repaint.</summary>
    public static int LogicTick;

    private int paintCount;

    private Vector2 lastMousePos = default(Vector2);

    public static int a = 1;

    public static bool isCompactDevice = true;

    private void Start()
    {

        if (!started)
        {
            if (Thread.CurrentThread.Name != "Main")
            {
                Thread.CurrentThread.Name = "Main";
            }
            mainThreadName = Thread.CurrentThread.Name;
            level = RMS.loadRMSInt("levelScreenKN");
            if (isPC)
            {
                if (level == 1)
                {
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                }
                else
                {
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                }
            }
            if (Thread.CurrentThread.Name != "Main")
            {
                Thread.CurrentThread.Name = "Main";
            }
            mainThreadName = Thread.CurrentThread.Name;
            if (iPhoneSettings.generation == iPhoneGeneration.iPodTouch4Gen)
            {
                isIpod = true;
            }
            Screen.orientation = ScreenOrientation.AutoRotation;
            Application.runInBackground = NsoGameControl.InitialRunInBackground;
            Application.targetFrameRate = NsoGameControl.InitialFrameRate;
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int li = 1; li < listeners.Length; li++)
            {
                Object.Destroy(listeners[li]);
            }
            tickRate = NsoGameControl.InitialTickRate;
            Time.fixedDeltaTime = 1f / tickRate; // game speed: logic ticks per second (F9 slower, F10 faster)
            base.useGUILayout = false;
            isCompactDevice = detectCompactDevice();
            if (main == null)
            {
                main = this;
            }
            started = true;
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            {
                isPC = false;
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    IphoneVersionApp = true;
                }
            }
            else
            {
                isPC = true;
            }
            ScaleGUI.initScaleGUI();
            IMEI = SystemInfo.deviceUniqueIdentifier;
            //isPC = true;
            isWp = false;
            isAppTeam = false;
            IphoneVersionApp = false;
            if (isPC)
            {
                if (!Application.isEditor && NsoGameControl.InitialFullscreen && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-windowed") < 0)
                {
                    Resolution desktop = new Resolution { width = Display.main.systemWidth, height = Display.main.systemHeight };
                    Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
                    ScaleGUI.WIDTH = desktop.width;
                    ScaleGUI.HEIGHT = desktop.height;
                }
                else if (!Application.isEditor)
                {
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                }
            }
            g = new mGraphics();
            midlet = new GameMidlet();
            canvas = new GameCanvas();
            TileMap.loadTileMapArr();
            SplashScr.gI().switchToMe();
            Sound.init();
        }
    }

    public void doClearRMS()
    {
        if (isPC)
        {
            int num = RMS.loadRMSInt("lastZoomlevel");
            if (num != mGraphics.zoomLevel)
            {
                RMS.clearRMS();
                RMS.saveRMSInt("lastZoomlevel", mGraphics.zoomLevel);
                RMS.saveRMSInt("levelScreenKN", level);
            }
        }
    }

    private void OnGUI()
    {
        if (canvas == null || g == null) return;   // statics are gone after a script reload during Play
        Session_ME.update();
        if (Event.current.type == EventType.Repaint)
        {
            bool inLayer = NsoGameLayer.BeginPaint(); // redirects the drawing into the scene's game layer when one exists
            try
            {
                Interp.BeginPaint(Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime));
                NsoBridge.NsoMapSnapshot.Capture();   // map overlays line up with this paint
                canvas.paint(g);
            }
            finally
            {
                g.ClearScrollPaintOffset(); // also restore a scrolled panel if its paint throws
                Interp.EndPaint();   // restore the real positions even if paint throws
                if (inLayer) NsoGameLayer.EndPaint();
            }
            paintCount++;
            g.reset();
#if DEVELOPMENT_BUILD
            frameCount++;
#endif
        }
#if DEVELOPMENT_BUILD
        if (Event.current.type == EventType.Repaint) drawFps();
#endif
    }


    public int tickRate = 24;

    private int frameCount;

    private int shownFps;

    private float fpsTimer;

    private GUIStyle fpsStyle;

    // frames per second actually painted (the logic runs at 30 ticks per second)
    private void drawFps()
    {
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 1f)
        {
            shownFps = (int)(frameCount / fpsTimer);
            frameCount = 0;
            fpsTimer = 0f;
        }
        if (fpsStyle == null)
        {
            fpsStyle = new GUIStyle(GUI.skin.label);
            fpsStyle.fontSize = 18;
            fpsStyle.fontStyle = FontStyle.Bold;
            fpsStyle.normal.textColor = Color.yellow;
        }
        if (DevOptions.ShowFps) GUI.Label(new Rect(Screen.width / 2 - 100, 2, 240, 26), "FPS: " + shownFps + "  Speed: " + tickRate, fpsStyle);
    }

    private void FixedUpdate()
    {
        Interp.BeforeTick();
        canvas.update();
        RMS.update();
        Image.update();
        DataInputStream.update();
        updateCount++;
        LogicTick++;
        Sound.update();
        Handle.Fixed();
    }

    // Input acquisition and text/IME routing are owned by NsoInputSystemBridge.

private void OnApplicationQuit()
    {

        Sound.stopAll();
        Sound.stopAllBg();
        GameCanvas.bRun = false;
        Session_ME.gI().close();

        if (isPC)
        {
            Application.Quit();
        }
    }

    public static void exit()
    {
        if (isPC)
        {
            main.OnApplicationQuit();
        }
        else
        {
            a = 0;
        }
    }

    public static bool detectCompactDevice()
    {
        if (iPhoneSettings.generation == iPhoneGeneration.iPhone || iPhoneSettings.generation == iPhoneGeneration.iPhone3G || iPhoneSettings.generation == iPhoneGeneration.iPodTouch1Gen || iPhoneSettings.generation == iPhoneGeneration.iPodTouch2Gen)
        {
            return false;
        }
        return true;
    }

    public static bool checkCanSendSMS()
    {
        if (iPhoneSettings.generation == iPhoneGeneration.iPhone3GS || iPhoneSettings.generation == iPhoneGeneration.iPhone4 || iPhoneSettings.generation > iPhoneGeneration.iPodTouch4Gen)
        {
            return true;
        }
        return false;
    }
}
