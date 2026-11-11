using UnityEngine;

// Hierarchy object "NSO Game Control": live game settings, edited in the Inspector while playing.
public class NsoGameControl : MonoBehaviour
{
    [Header("Display — Zoom applies on next Play")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiZoom"), Range(0, 4)] private int _uiZoom = 3;
    [Tooltip("Windows build only; applies on next Play/build launch. -windowed still overrides this.")]
    [SerializeField] private bool _fullscreenOnStart = true;
    [Header("Timing — Inspector defaults take priority")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("logicTicksPerSecond"), Range(10, 60)] private int _logicTicksPerSecond = 24;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetFrameRate"), Range(15, 144)] private int _targetFrameRate = 60;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("runInBackground")] private bool _runInBackground = true;
    [Header("Gameplay — client preferences")]
    [Tooltip("Reconnect after an unexpected disconnect; does not change server gameplay stats.")]
    [SerializeField] private bool _autoReconnect;
    public int uiZoom { get { return _uiZoom; } set { _uiZoom = Mathf.Clamp(value, 0, 4); } }
    public int logicTicksPerSecond { get { return _logicTicksPerSecond; } set { _logicTicksPerSecond = Mathf.Clamp(value, 10, 60); } }
    public int targetFrameRate { get { return _targetFrameRate; } set { _targetFrameRate = Mathf.Clamp(value, 15, 144); } }
    public bool runInBackground { get { return _runInBackground; } set { _runInBackground = value; } }
    // used by the Settings menu (F6 > Chơi); Update copies it to GameScr.autoReconnect every frame
    public bool autoReconnect { get { return _autoReconnect; } set { _autoReconnect = value; } }
    public static NsoGameControl Active { get; private set; }
    private int _appliedTicks = -1;
    public static int InitialTickRate { get { return Active != null ? Active.logicTicksPerSecond : 24; } }
    public static int InitialFrameRate { get { return Active != null ? Active.targetFrameRate : 60; } }
    public static bool InitialRunInBackground { get { return Active == null || Active.runInBackground; } }
    public static bool InitialFullscreen { get { return Active == null || Active._fullscreenOnStart; } }

    public static int UiZoomOverride;

    private void Awake()
    {
        Validate();
        if (Active != null && Active != this) { Debug.LogError("Only one NSO Game Control may own configuration.", this); enabled = false; return; }
        Active = this;
        UiZoomOverride = uiZoom;
        GameScr.autoReconnect = _autoReconnect;
    }

    private void OnDestroy() { if (Active == this) { Active = null; UiZoomOverride = 0; } }
    private void Validate()
    {
        _uiZoom = Mathf.Clamp(_uiZoom, 0, 4);
        _logicTicksPerSecond = Mathf.Clamp(_logicTicksPerSecond, 10, 60);
        _targetFrameRate = Mathf.Clamp(_targetFrameRate, 15, 144);
    }
    private void OnValidate() { Validate(); }

    private void Update()
    {
        if (Active != this) return;
        if (Main.main != null && _appliedTicks != logicTicksPerSecond)
        {
            Main.main.tickRate = logicTicksPerSecond;
            Time.fixedDeltaTime = 1f / logicTicksPerSecond;
            _appliedTicks = logicTicksPerSecond;
        }
        if (Application.targetFrameRate != targetFrameRate) Application.targetFrameRate = targetFrameRate;
        Application.runInBackground = runInBackground;
        GameScr.autoReconnect = _autoReconnect;
    }
}
