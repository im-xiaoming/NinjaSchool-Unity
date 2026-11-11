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
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetFrameRate"), Range(60, 144)] private int _targetFrameRate = 60;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("runInBackground")] private bool _runInBackground = true;
    [Header("Effect presentation - does not change gameplay timing")]
    [Tooltip("Fractional world/camera/projectile placement at render rate. Disable for legacy integer interpolation.")]
    [SerializeField] private bool _smoothWorldMotion = true;
    public static bool SmoothWorldMotion { get { return Active == null || Active._smoothWorldMotion; } }
    [Tooltip("Smooth damage numbers, hit splashes and menu movement between logic ticks.")]
    [SerializeField] private bool _smoothEffects = true;
    [Tooltip("Seconds for the visible monster HP bar to reach its actual HP. Zero makes it instant.")]
    [SerializeField, Range(0f, 0.5f)] private float _mobHealthSeconds = 0.12f;
    [Tooltip("Minimum opening animation for menus, including menus reopened within five seconds.")]
    [SerializeField, Range(0f, 0.5f)] private float _menuOpenSeconds = 0.14f;
    public static float MenuOpenSeconds { get { return Active != null ? Mathf.Clamp(Active._menuOpenSeconds, 0f, 0.5f) : 0.14f; } }
    public static bool SmoothEffects { get { return Active == null || Active._smoothEffects; } }
    public static float MobHealthSeconds { get { return Active != null ? Mathf.Clamp(Active._mobHealthSeconds, 0f, 0.5f) : 0.12f; } }
    [Header("Gameplay — client preferences")]
    [Tooltip("Stop grounded manual movement when its last horizontal button is released. Disable for original client inertia.")]
    [SerializeField] private bool _instantMovementStop = true;
    public static bool InstantMovementStop { get { return Active == null || Active._instantMovementStop; } }
    [Tooltip("Double-clicking the same monster, NPC, player or ground item acts like pressing Enter (fire/talk/pick). Zero disables it.")]
    [SerializeField, Range(0, 1000)] private int _doubleClickMs = 400;
    /// <summary>Max gap in ms between two clicks on the same world target to count as a double click; 0 = off.</summary>
    public static int DoubleClickMs { get { return Active != null ? Mathf.Clamp(Active._doubleClickMs, 0, 1000) : 400; } }
    public int uiZoom { get { return _uiZoom; } set { _uiZoom = Mathf.Clamp(value, 0, 4); } }
    public int logicTicksPerSecond { get { return _logicTicksPerSecond; } set { _logicTicksPerSecond = Mathf.Clamp(value, 10, 60); } }
    public int targetFrameRate { get { return Mathf.Clamp(_targetFrameRate, 60, 144); } set { _targetFrameRate = Mathf.Clamp(value, 60, 144); } }
    public bool runInBackground { get { return _runInBackground; } set { _runInBackground = value; } }
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
    }

    private void OnDestroy() { if (Active == this) { Active = null; UiZoomOverride = 0; } }
    private void Validate()
    {
        _uiZoom = Mathf.Clamp(_uiZoom, 0, 4);
        _logicTicksPerSecond = Mathf.Clamp(_logicTicksPerSecond, 10, 60);
        _targetFrameRate = Mathf.Clamp(_targetFrameRate, 60, 144);
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
    }
}
