using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Offline client addition: gamepad support (PS5 DualSense, also any gamepad the Input System knows).
/// Default layout follows Elden Ring (user request):
///   Left stick = run only in the world (no jump), D-pad = move (up = jump); both navigate menus, Cross = jump in the world / back in menus,
///   Circle = interact / attack in the world and select in menus (user request), Triangle = interact,
///   R1 = change target (user request),
///   R2 / L2 / L1 = skill shortcuts 1 / 2 / 3, Options = menu.
/// Each button is translated into the same client key code the keyboard sends through Main.OnGUI
/// (see MyKeyMap), so the game reacts exactly as if that key were pressed: keyPressed on the
/// press edge, keyReleased on the release edge. No gameplay code is changed.
/// Created automatically at startup; place one in the scene to change the mapping in the Inspector.
/// </summary>
public class NsoGamepadInput : MonoBehaviour
{
    // Client key codes (MyKeyMap).
    private const int Up = -1, Down = -2, Left = -3, Right = -4, Enter = -5, SoftLeft = -21, SoftRight = -22;

    [Tooltip("Turn gamepad input on or off.")]
    [SerializeField] private bool _enabled = true;
    [Tooltip("Left stick tilt (0-1) needed to count as a direction press.")]
    [SerializeField, Range(0.1f, 0.95f)] private float _stickDeadzone = 0.5f;
    [Tooltip("In the world (no menu open) the left stick only runs left/right; its up/down (jump) is ignored. User request. D-pad up still jumps; menus still use the stick's up/down.")]
    [SerializeField] private bool _stickRunOnlyInWorld = true;

    [Header("Client key codes (see MyKeyMap). 0 = unused")]
    [Tooltip("Cross (X) in the world, no menu/popup open: jump (up arrow).")]
    [SerializeField] private int _crossWorld = Up;
    [Tooltip("Cross (X) in menus, dialogs, popups and other screens: F2 = back / close.")]
    [SerializeField] private int _crossMenu = SoftRight;
    [Tooltip("Circle in the world: Enter = interact / attack the focused target (user request).")]
    [SerializeField] private int _circleWorld = Enter;
    [Tooltip("Circle in menus, dialogs, popups and other screens: Enter = select (user request).")]
    [SerializeField] private int _circleMenu = Enter;
    [Tooltip("Triangle: Enter = interact (talk to NPC, pick up item).")]
    [SerializeField] private int _triangle = Enter;
    [Tooltip("Square (Elden Ring: use item). No quick-item key in the client yet.")]
    [SerializeField] private int _square = 0;
    [Tooltip("R1 in the world: change target (TargetCycler, nearest first). User request.")]
    [SerializeField] private bool _r1ChangesTarget = true;

    [Header("Target switching (right stick flick / R1)")]
    [Tooltip("Flicking the right stick past this tilt (0-1) switches to the next target; it must return to centre before the next flick.")]
    [SerializeField, Range(0.2f, 0.95f)] private float _flickThreshold = 0.6f;
    [Tooltip("N: how many nearest targets the switch list holds.")]
    [SerializeField, Range(1, 30)] private int _targetCount = 5;
    [Tooltip("Ellipse horizontal radius in map pixels. Distance = (dx/X)^2 + (dy/Y)^2.")]
    [SerializeField, Range(10f, 500f)] private float _ellipseRadiusX = 120f;
    [Tooltip("Ellipse vertical radius in map pixels. Smaller than X = targets on other floors count as farther.")]
    [SerializeField, Range(10f, 500f)] private float _ellipseRadiusY = 60f;
    [Tooltip("Seconds between list refreshes (the list is rebuilt continuously; also rebuilt right before each switch).")]
    [SerializeField, Range(0.02f, 1f)] private float _refreshSeconds = 0.1f;
    private float _nextRefresh;
    private bool _flickHeld;
    [Tooltip("R1 key code used when 'R1 Changes Target' is off, or outside the world.")]
    [SerializeField] private int _r1 = 0;
    [Tooltip("R2: skill shortcut 1 (key 7).")]
    [SerializeField] private int _r2 = 55;
    [Tooltip("L2: skill shortcut 2 (key 8).")]
    [SerializeField] private int _l2 = 56;
    [Tooltip("L1: skill shortcut 3 (key 9).")]
    [SerializeField] private int _l1 = 57;
    [Tooltip("Options: F1 = left soft key (menu).")]
    [SerializeField] private int _options = SoftLeft;
    [Tooltip("Create: unused by default.")]
    [SerializeField] private int _create = 0;
    [Tooltip("R3 (Elden Ring: lock on). Unused by default.")]
    [SerializeField] private int _r3 = 0;

    private const int Slots = 15;
    private readonly bool[] _down = new bool[Slots];   // state last frame, per slot
    private readonly int[] _sentCode = new int[Slots]; // code sent on press, so release matches it

    public static NsoGamepadInput Active { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<NsoGamepadInput>() != null) return;
        GameObject go = new GameObject("NSO Gamepad Input");
        DontDestroyOnLoad(go);
        go.AddComponent<NsoGamepadInput>();
    }

    private void OnEnable() { Active = this; }

    private void OnDisable()
    {
        ReleaseAll();
        if (Active == this) Active = null;
    }

    /// <summary>True when walking around the map with no menu, dialog, popup or UI panel open.</summary>
    private static bool InWorld()
    {
        GameScr gs = GameScr.instance;
        return gs != null && GameCanvas.currentScreen == gs && GameCanvas.currentDialog == null
            && !GameCanvas.menu.showMenu && !ChatTextField.gI().isShow
            && !gs.isPaintPopup() && !gs.isPaintUI() && !gs.isOpenUI();
    }

    private void Update()
    {
        Gamepad pad = Gamepad.current;
        if (!_enabled || pad == null || Main.canvas == null || !Application.isFocused)
        {
            ReleaseAll();
            return;
        }
        bool world = InWorld();
        Vector2 stick = pad.leftStick.ReadValue();
        // Run-only stick in the world: drop its vertical axis so tilting it never jumps (user request).
        bool stickVertical = !(world && _stickRunOnlyInWorld);
        Set(0, Up, pad.dpad.up.isPressed || (stickVertical && stick.y > _stickDeadzone));
        Set(1, Down, pad.dpad.down.isPressed || (stickVertical && stick.y < -_stickDeadzone));
        Set(2, Left, pad.dpad.left.isPressed || stick.x < -_stickDeadzone);
        Set(3, Right, pad.dpad.right.isPressed || stick.x > _stickDeadzone);
        // Cross and Circle pick their code at the moment they are pressed; the release matches that code.
        // Menus: D-pad/stick up/down move the selection, Circle selects, Cross goes back.
        Set(4, world ? _crossWorld : _crossMenu, pad.buttonSouth.isPressed);
        Set(5, world ? _circleWorld : _circleMenu, pad.buttonEast.isPressed);
        Set(6, _triangle, pad.buttonNorth.isPressed);
        Set(7, _square, pad.buttonWest.isPressed);
        // Target switching: right stick flick (any direction) and R1. No keyboard key exists for it,
        // so TargetCycler sets the focus directly. The list is refreshed continuously while in the world.
        bool switchTarget = false;
        float tilt = pad.rightStick.ReadValue().magnitude;
        if (!_flickHeld && tilt > _flickThreshold) { _flickHeld = true; switchTarget = world; }
        else if (_flickHeld && tilt < _flickThreshold * 0.5f) _flickHeld = false; // back near centre: re-arm
        if (_r1ChangesTarget && world)
        {
            if (pad.rightShoulder.wasPressedThisFrame) switchTarget = true;
            Set(8, 0, pad.rightShoulder.isPressed);
        }
        else Set(8, _r1, pad.rightShoulder.isPressed);
        if (world && (switchTarget || Time.unscaledTime >= _nextRefresh))
        {
            _nextRefresh = Time.unscaledTime + _refreshSeconds;
            TargetCycler.Refresh(_targetCount, _ellipseRadiusX, _ellipseRadiusY);
        }
        if (switchTarget) TargetCycler.Next();
        Set(9, _r2, pad.rightTrigger.isPressed);
        Set(10, _l2, pad.leftTrigger.isPressed);
        Set(11, _l1, pad.leftShoulder.isPressed);
        // Options toggles the game menu (user request): in the world it opens the menu (F1);
        // if the menu or any game panel is open, a second press closes everything, whatever tab is shown.
        if (!world && pad.startButton.wasPressedThisFrame && CloseGameMenus())
            _down[12] = true; // consumed; the release is swallowed (_sentCode = 0)
        else Set(12, _options, pad.startButton.isPressed);
        Set(13, _create, pad.selectButton.isPressed);
        Set(14, _r3, pad.rightStickButton.isPressed);
    }

    /// <summary>
    /// Closes the in-game menu and every GameScr panel (bag, skills, info, shops, lists...).
    /// Uses the client's own close paths: Menu.doCloseMenu and GameScr.resetButton (the action of
    /// the "Đóng" close-all command, id 11021). Returns false when nothing in GameScr was open.
    /// </summary>
    private bool CloseGameMenus()
    {
        GameScr gs = GameScr.instance;
        if (gs == null || GameCanvas.currentScreen != gs || GameCanvas.currentDialog != null) return false;
        bool open = GameCanvas.menu.showMenu || gs.isPaintPopup() || gs.isPaintUI() || gs.isOpenUI();
        if (!open) return false;
        if (GameCanvas.menu.showMenu) GameCanvas.menu.doCloseMenu();
        if (gs.isPaintPopup() || gs.isPaintUI() || gs.isOpenUI())
        {
            GameScr.svTitle = string.Empty; // same as GameScr.perform case 11021
            GameScr.svAction = string.Empty;
            gs.resetButton();
        }
        _sentCode[12] = 0;
        return true;
    }

    /// <summary>Sends keyPressed/keyReleased on state edges of one slot.</summary>
    private void Set(int slot, int code, bool pressed)
    {
        if (pressed == _down[slot]) return;
        _down[slot] = pressed;
        if (pressed)
        {
            _sentCode[slot] = code;
            if (code != 0) Main.canvas.keyPressed(code);
        }
        else if (_sentCode[slot] != 0)
        {
            Main.canvas.keyReleased(_sentCode[slot]);
        }
    }

    /// <summary>Releases every held key (pad unplugged, disabled or window unfocused) so nothing sticks.</summary>
    private void ReleaseAll()
    {
        for (int i = 0; i < Slots; i++)
        {
            if (!_down[i]) continue;
            _down[i] = false;
            if (_sentCode[i] != 0 && Main.canvas != null) Main.canvas.keyReleased(_sentCode[i]);
        }
    }
}
