using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class NsoInputSystemBridge : MonoBehaviour
{
    [SerializeField] private UnityEngine.InputSystem.InputActionAsset _actions;
    [SerializeField, Min(0.01f)] private float _repeatDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float _repeatInterval = 0.05f;
    private UnityEngine.InputSystem.InputActionAsset _runtime;
    private UnityEngine.InputSystem.Keyboard _keyboard;
    private readonly Queue<System.Action> _events = new Queue<System.Action>();
    private readonly Dictionary<string, int> _sent = new Dictionary<string, int>();
    private readonly Dictionary<string, KeyCode> _keys = new Dictionary<string, KeyCode>();
    private readonly Dictionary<int, float> _pressedAt = new Dictionary<int, float>();
    private readonly Queue<int> _releases = new Queue<int>();
    private bool _pointerHeld, _chat, _diagnostic;
    private KeyCode _repeatKey;
    private float _nextRepeat;
    private int _textBackspaces;
    public static string Composition { get; private set; } = string.Empty;
    public static Vector3 MousePosition { get { var m = UnityEngine.InputSystem.Mouse.current; return m == null ? Vector3.zero : (Vector3)m.position.ReadValue(); } }
    public static bool MousePresent { get { return UnityEngine.InputSystem.Mouse.current != null; } }
    private static readonly Dictionary<int, int> Holds = new Dictionary<int, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Holds.Clear(); Composition = string.Empty; }

    public static void Press(int code)
    {
        if (code == 0 || Main.canvas == null) return;
        int count; Holds.TryGetValue(code, out count); Holds[code] = count + 1;
        if (count == 0) Main.canvas.keyPressed(code);
    }
    public static void Release(int code)
    {
        int count;
        if (code == 0 || !Holds.TryGetValue(code, out count)) return;
        if (count > 1) Holds[code] = count - 1;
        else { Holds.Remove(code); if (Main.canvas != null) Main.canvas.keyReleased(code); }
    }
    public static KeyCode ToLegacyKey(string name)
    {
        if (name.Length == 1 && name[0] >= 'a' && name[0] <= 'z') return (KeyCode)((int)KeyCode.A + name[0] - 'a');
        if (name.Length == 1 && char.IsDigit(name[0])) return (KeyCode)((int)KeyCode.Alpha0 + name[0] - '0');
        switch (name) {
            case "enter": return KeyCode.Return;
            case "numpadEnter": return KeyCode.KeypadEnter;
            case "numpadDivide": return KeyCode.KeypadDivide;
            case "upArrow": return KeyCode.UpArrow;
            case "downArrow": return KeyCode.DownArrow;
            case "leftArrow": return KeyCode.LeftArrow;
            case "rightArrow": return KeyCode.RightArrow;
        }
        KeyCode key; return System.Enum.TryParse(name, true, out key) ? key : KeyCode.None;
    }
    private void OnEnable()
    {
        if (_actions == null) { Debug.LogError("NSO input bridge requires its controls action asset.", this); enabled = false; return; }
        _runtime = Instantiate(_actions);
        foreach (var action in _runtime.actionMaps[0].actions)
        {
            var captured = action;
            if (action.name == "Click") { action.performed += c => _events.Enqueue(() => Pointer(true)); action.canceled += c => _events.Enqueue(() => Pointer(false)); }
            else if (action.name == "Scroll") action.performed += c => { var value = c.ReadValue<Vector2>(); _events.Enqueue(() => Wheel(value.y)); };
            else if (action.name != "Position")
            {
                _keys[action.name] = ToLegacyKey(action.name);
                action.performed += c => { var keyboard = c.control.device as UnityEngine.InputSystem.Keyboard; bool shift = keyboard != null && keyboard.shiftKey.isPressed; bool alt = keyboard != null && keyboard.altKey.isPressed; _events.Enqueue(() => KeyDown(captured.name, shift, alt)); };
                action.canceled += c => _events.Enqueue(() => KeyUp(captured.name));
            }
        }
        UnityEngine.InputSystem.InputSystem.onDeviceChange += DeviceChanged;
        AttachKeyboard();
        _runtime.Enable();
    }
    private void AttachKeyboard()
    {
        if (_keyboard != null) { _keyboard.onTextInput -= Text; _keyboard.onIMECompositionChange -= Ime; _keyboard.SetIMEEnabled(false); }
        _keyboard = UnityEngine.InputSystem.Keyboard.current;
        Composition = string.Empty;
        if (_keyboard != null) { _keyboard.onTextInput += Text; _keyboard.onIMECompositionChange += Ime; _keyboard.SetIMEEnabled(ChatTextField.IsShowing); }
    }
    private void DeviceChanged(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (change == UnityEngine.InputSystem.InputDeviceChange.Removed || change == UnityEngine.InputSystem.InputDeviceChange.Disconnected) Cancel();
        if (device is UnityEngine.InputSystem.Keyboard) AttachKeyboard();
    }
    private void Text(char c) { _events.Enqueue(() => CommitText(c)); }
    private void Ime(UnityEngine.InputSystem.LowLevel.IMECompositionString value) { Composition = value.ToString(); }
    private void CommitText(char c)
    {
        if (Main.canvas == null || !Application.isFocused) return;
        if (c == '\b') _textBackspaces++;
        if (TextCalib.Open) {
            if (NsoSettingsMenu.TextOffsets && c >= ' ') NsoSettingsMenu.Key(new Event { type = EventType.KeyDown, character = c });
            return;
        }
        if (!ChatTextField.IsShowing) return;
        if (c == '\b') ChatTextField.gI().tfChat.clear();
        else if (c >= ' ' && c != (char)127) ChatTextField.gI().keyPressed(c);
    }
    private void KeyDown(string name, bool shift, bool alt)
    {
        if (Main.canvas == null || !Application.isFocused || _sent.ContainsKey(name)) return;
        KeyCode key = _keys[name];
        _sent[name] = 0;
        if (key == KeyCode.F9 || key == KeyCode.F10) { if (Main.main != null) { Main.main.tickRate = Mathf.Clamp(Main.main.tickRate + (key == KeyCode.F10 ? 2 : -2), 10, 60); Time.fixedDeltaTime = 1f / Main.main.tickRate; } return; }
        if (key == KeyCode.F11 || (alt && key == KeyCode.Return)) { Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow; return; }
        if (key == KeyCode.F6) { CancelHeld(); TextCalib.Open = !TextCalib.Open; if (TextCalib.Open) NsoSettingsMenu.OnOpened(); return; }
        if (TextCalib.Open) { NsoSettingsMenu.Key(new Event { type = EventType.KeyDown, keyCode = key, shift = shift }); StartRepeat(key); return; }
        int code = MyKeyMap.map(key);
        if (shift && key == KeyCode.Alpha2) code = 64;
        if (shift && key == KeyCode.Minus) code = 95;
        if (ChatTextField.IsShowing) {
            if (key == KeyCode.Backspace) { StartRepeat(key); return; }
            if (key == KeyCode.Escape) { ChatTextField.gI().perform(8001, null); return; }
            if (code >= 32 || key == KeyCode.Space) return;
        }
        if (code != 0) { _sent[name] = code; Press(code); _pressedAt[code] = Time.fixedTime; }
    }
    private void StartRepeat(KeyCode key) { _repeatKey = key; _nextRepeat = Time.unscaledTime + _repeatDelay; if (key == KeyCode.Backspace && !TextCalib.Open) _events.Enqueue(() => { if (_textBackspaces == 0 && ChatTextField.IsShowing) ChatTextField.gI().tfChat.clear(); }); }
    private void KeyUp(string name)
    {
        if (_keys[name] == _repeatKey) _repeatKey = KeyCode.None;
        int code; if (!_sent.TryGetValue(name, out code)) return;
        _sent.Remove(name);
        if (code != 0) _releases.Enqueue(code);
    }
    private Vector2 GamePointer() { Vector3 m = MousePosition; float z = Mathf.Max(1, mGraphics.zoomLevel); return new Vector2((int)(m.x / z), (int)((Screen.height - m.y) / z)); }
    private void Pointer(bool down) { if (Main.canvas == null) return; Vector2 p = GamePointer(); _pointerHeld = down; if (down) Main.canvas.pointerPressed((int)p.x, (int)p.y); else Main.canvas.pointerReleased((int)p.x, (int)p.y); }
    private void Wheel(float value)
    {
        if (Main.canvas == null) return;
        // Input System 1.19 normalizes wheel deltas to notches by default.
        // Divide by 120 only when native Windows deltas are explicitly enabled;
        // normalizing twice makes Scroll round a normal notch down to zero pixels.
        bool native = UnityEngine.InputSystem.InputSystem.settings.scrollDeltaBehavior ==
            UnityEngine.InputSystem.InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange;
        bool windows = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
        var p = GamePointer();
        Scroll.AddWheel(native && windows ? value / 120f : value, (int)p.x, (int)p.y);
    }
    private void Update()
    {
        if (Main.canvas == null || !Application.isFocused) { Cancel(); return; }
        bool chat = ChatTextField.IsShowing, diagnostic = TextCalib.Open;
        if (chat != _chat || diagnostic != _diagnostic) { CancelHeld(); _chat = chat; _diagnostic = diagnostic; Composition = string.Empty; if (_keyboard != null) _keyboard.SetIMEEnabled(chat); }
        _textBackspaces = 0;
        while (_events.Count > 0) _events.Dequeue()();
        if (_pointerHeld && MousePresent) { Vector2 p = GamePointer(); Main.canvas.pointerDragged((int)p.x, (int)p.y); }
        int pending = _releases.Count;
        while (pending-- > 0) { int code = _releases.Dequeue(); float at; if (_pressedAt.TryGetValue(code, out at) && Time.fixedTime <= at) _releases.Enqueue(code); else Release(code); }
        if (_repeatKey != KeyCode.None && Time.unscaledTime >= _nextRepeat) {
            _nextRepeat = Time.unscaledTime + _repeatInterval;
            if (TextCalib.Open) NsoSettingsMenu.Key(new Event { type = EventType.KeyDown, keyCode = _repeatKey, shift = _keyboard != null && _keyboard.shiftKey.isPressed });
            else if (_repeatKey == KeyCode.Backspace && ChatTextField.IsShowing && _textBackspaces == 0) ChatTextField.gI().tfChat.clear();
        }
    }
    private void CancelHeld()
    {
        foreach (var pair in _sent) if (pair.Value != 0) Release(pair.Value);
        _sent.Clear(); while (_releases.Count > 0) Release(_releases.Dequeue()); _pressedAt.Clear(); _repeatKey = KeyCode.None;
        if (_pointerHeld) { if (Main.canvas != null) { if (TextCalib.Open) NsoSettingsMenu.Pointer(0, 0, 2); GameCanvas.isPointerDown = false; GameCanvas.isPointerClick = false; } _pointerHeld = false; }
    }
    private void Cancel() { CancelHeld(); _events.Clear(); Composition = string.Empty; }
    private void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }
    private void OnDisable()
    {
        Cancel(); UnityEngine.InputSystem.InputSystem.onDeviceChange -= DeviceChanged;
        if (_keyboard != null) { _keyboard.onTextInput -= Text; _keyboard.onIMECompositionChange -= Ime; _keyboard.SetIMEEnabled(false); _keyboard = null; }
        if (_runtime != null) { _runtime.Disable(); Destroy(_runtime); _runtime = null; }
    }
}
