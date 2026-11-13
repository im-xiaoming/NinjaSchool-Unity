using UnityEngine;
using NsoServer.Configuration;

// Hierarchy object "NSO Game Control": live game settings, edited in the Inspector while playing.
public class NsoGameControl : MonoBehaviour
{
    [Header("Display — Zoom applies on next Play")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiZoom"), Range(0, 4)]
    private int _uiZoom = 3;
    [Tooltip("Windows build only; applies on next Play/build launch. -windowed still overrides this.")]
    [SerializeField]
    private bool _fullscreenOnStart = true;
    [Header("Timing — Inspector defaults take priority")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("logicTicksPerSecond"), Range(10, 60)]
    private int _logicTicksPerSecond = 24;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetFrameRate"), Range(60, 144)]
    private int _targetFrameRate = 60;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("runInBackground")]
    private bool _runInBackground = true;
    [Header("Effect presentation - does not change gameplay timing")]
    [Tooltip("Fractional world/camera/projectile placement at render rate. Disable for legacy integer interpolation.")]
    [SerializeField]
    private bool _smoothWorldMotion = true;
    public static bool SmoothWorldMotion
    {
        get
        {
            return Value("_smoothWorldMotion", Active == null || Active._smoothWorldMotion);
        }
    }

    [Tooltip("Render remote characters continuously between sparse network positions. Presentation only; includes all other players.")]
    [SerializeField]
    private bool _smoothRemoteCharacters = true;
    [SerializeField, Range(0.06f, 1f)]
    private float _remoteInitialMoveSeconds = 0.5f;
    [SerializeField, Range(0.06f, 1f)]
    private float _remoteMaximumMoveSeconds = 0.75f;
    [SerializeField, Range(90f, 1000f)]
    private float _remoteTeleportDistance = 180f;
    public static bool SmoothRemoteCharacters
    {
        get
        {
            return SmoothWorldMotion && Value("_smoothRemoteCharacters", Active == null || Active._smoothRemoteCharacters);
        }
    }

    public static float RemoteMaximumMoveSeconds
    {
        get
        {
            return Mathf.Clamp(Value("_remoteMaximumMoveSeconds", Active != null ? Active._remoteMaximumMoveSeconds : .75f), .06f, 1f);
        }
    }

    public static float RemoteInitialMoveSeconds
    {
        get
        {
            return Mathf.Clamp(Value("_remoteInitialMoveSeconds", Active != null ? Active._remoteInitialMoveSeconds : .5f), .06f, RemoteMaximumMoveSeconds);
        }
    }

    public static float RemoteTeleportDistance
    {
        get
        {
            return Mathf.Clamp(Value("_remoteTeleportDistance", Active != null ? Active._remoteTeleportDistance : 180f), 90f, 1000f);
        }
    }

    [Tooltip("Smooth damage numbers, hit splashes and menu movement between logic ticks.")]
    [SerializeField]
    private bool _smoothEffects = true;
    [Tooltip("Seconds for the visible monster HP bar to reach its actual HP. Zero makes it instant.")]
    [SerializeField, Range(0f, 0.5f)]
    private float _mobHealthSeconds = 0.12f;
    [Tooltip("Minimum opening animation for menus, including menus reopened within five seconds.")]
    [SerializeField, Range(0f, 0.5f)]
    private float _menuOpenSeconds = 0.14f;
    public static float MenuOpenSeconds
    {
        get
        {
            return Mathf.Clamp(Value("_menuOpenSeconds", Active != null ? Active._menuOpenSeconds : .14f), 0f, .5f);
        }
    }

    public static bool SmoothEffects
    {
        get
        {
            return Value("_smoothEffects", Active == null || Active._smoothEffects);
        }
    }

    public static float MobHealthSeconds
    {
        get
        {
            return Mathf.Clamp(Value("_mobHealthSeconds", Active != null ? Active._mobHealthSeconds : .12f), 0f, .5f);
        }
    }

    [Header("Gameplay — client preferences")]
    [Tooltip("Stop grounded manual movement when its last horizontal button is released. Disable for original client inertia.")]
    [SerializeField]
    private bool _instantMovementStop = true;
    public static bool InstantMovementStop
    {
        get
        {
            return Value("_instantMovementStop", Active == null || Active._instantMovementStop);
        }
    }

    [Tooltip("Double-clicking the same monster, NPC, player or ground item acts like pressing Enter (fire/talk/pick). Zero disables it.")]
    [SerializeField, Range(0, 1000)]
    private int _doubleClickMs = 400;
    /// <summary>Max gap in ms between two clicks on the same world target to count as a double click; 0 = off.</summary>
    public static int DoubleClickMs
    {
        get
        {
            return Mathf.Clamp(Value("_doubleClickMs", Active != null ? Active._doubleClickMs : 400), 0, 1000);
        }
    }

    public int uiZoom
    {
        get
        {
            return Mathf.Clamp(Value("_uiZoom", _uiZoom), 0, 4);
        }

        set
        {
            _uiZoom = Mathf.Clamp(value, 0, 4);
            RuntimeConfiguration.Set("game", "_uiZoom", _uiZoom);
        }
    }

    public int logicTicksPerSecond
    {
        get
        {
            return Mathf.Clamp(Value("_logicTicksPerSecond", _logicTicksPerSecond), 10, 60);
        }

        set
        {
            _logicTicksPerSecond = Mathf.Clamp(value, 10, 60);
            RuntimeConfiguration.Set("game", "_logicTicksPerSecond", _logicTicksPerSecond);
        }
    }

    public int targetFrameRate
    {
        get
        {
            return Mathf.Clamp(Value("_targetFrameRate", _targetFrameRate), 60, 144);
        }

        set
        {
            _targetFrameRate = Mathf.Clamp(value, 60, 144);
            RuntimeConfiguration.Set("game", "_targetFrameRate", _targetFrameRate);
        }
    }

    public bool runInBackground
    {
        get
        {
            return Value("_runInBackground", _runInBackground);
        }

        set
        {
            _runInBackground = value;
            RuntimeConfiguration.Set("game", "_runInBackground", _runInBackground);
        }
    }

    public static NsoGameControl Active { get; private set; }

    private int _appliedTicks = -1;
    public static int InitialTickRate
    {
        get
        {
            return Mathf.Clamp(Value("_logicTicksPerSecond", Active != null ? Active._logicTicksPerSecond : 24), 10, 60);
        }
    }

    public static int InitialFrameRate
    {
        get
        {
            return Mathf.Clamp(Value("_targetFrameRate", Active != null ? Active._targetFrameRate : 60), 60, 144);
        }
    }

    public static bool InitialRunInBackground
    {
        get
        {
            return Value("_runInBackground", Active == null || Active._runInBackground);
        }
    }

    public static bool InitialFullscreen
    {
        get
        {
            return Value("_fullscreenOnStart", Active == null || Active._fullscreenOnStart);
        }
    }

    public static int UiZoomOverride;
    private static T Value<T>(string key, T fallback)
    {
        return RuntimeConfiguration.Value("game", key, fallback);
    }

    private void Awake()
    {
        Validate();
        if (Active != null && Active != this)
        {
            Debug.LogError("Only one NSO Game Control may own configuration.", this);
            enabled = false;
            return;
        }

        Active = this;
        NsoConfigurationBridge.Register("game", this);
        UiZoomOverride = uiZoom;
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
            UiZoomOverride = 0;
        }
    }

    private void Validate()
    {
        _uiZoom = Mathf.Clamp(_uiZoom, 0, 4);
        _logicTicksPerSecond = Mathf.Clamp(_logicTicksPerSecond, 10, 60);
        _targetFrameRate = Mathf.Clamp(_targetFrameRate, 60, 144);
    }

    private void OnValidate()
    {
        Validate();
        NsoConfigurationBridge.Register("game", this);
    }

    private void Update()
    {
        if (Active != this)
            return;
        if (Main.main != null && _appliedTicks != logicTicksPerSecond)
        {
            Main.main.tickRate = logicTicksPerSecond;
            Time.fixedDeltaTime = 1f / logicTicksPerSecond;
            _appliedTicks = logicTicksPerSecond;
        }

        if (Application.targetFrameRate != targetFrameRate)
            Application.targetFrameRate = targetFrameRate;
        Application.runInBackground = runInBackground;
    }
}
