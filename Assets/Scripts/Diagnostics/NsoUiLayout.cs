using System;
using UnityEngine;
using NsoServer.Configuration;

// Hierarchy object "NSO UI Layout": the UI spacing numbers, edited in the Inspector (also live while playing).
// All values are game pixels (multiplied by the UI zoom when drawn). Without this object in the scene the defaults below
// are used, so the game looks the same as before.
public class NsoUiLayout : MonoBehaviour
{
    [Serializable]
    public class BottomButtons
    {
        [Tooltip("Space between the bottom buttons (Chọn / Đóng ...) and the bottom edge of the screen.")]
        public int bottomGap = 6;
        [Tooltip("Space between the left button and the left screen edge, and between the right button and the right screen edge.")]
        public int sidePadding = 2;
        [Tooltip("Horizontal inset of popup bottom buttons from the logical panel edge. -2 aligns with paintFrame's outer border; narrow panels keep the legacy row to avoid overlap.")]
        public int popupSideInset = -2;
    }

    [Serializable]
    public class PotionButtons
    {
        [Tooltip("Space between the HP / MP potion buttons (PC) and the bottom edge of the screen.")]
        public int bottomGap = 6;
    }

    [Serializable]
    public class NpcMenu
    {
        [Tooltip("Space between the NPC menu buttons and the bottom edge of the screen. Applied when a menu opens.")]
        public int bottomGap = 8;
    }

    [Serializable]
    public class DeveloperWindow
    {
        [Tooltip("Height of one row: raise it for more space between rows.")]
        public int valueRow = 28;
        [Tooltip("Height of the button hit area.")]
        public int buttonH = 24;
        [Tooltip("Width of one value box (X or Y).")]
        public int boxW = 38;
        [Tooltip("Height of one value box.")]
        public int boxH = 20;
        [Tooltip("Space between the X box and the Y box of the same row.")]
        public int boxGap = 5;
        [Tooltip("Space between the Y box and the right edge of the list.")]
        public int boxRightMargin = 8;
        [Tooltip("Window width.")]
        public int windowW = 240;
        [Tooltip("Window height limit (also limited by the screen height).")]
        public int windowMaxH = 300;
        [Tooltip("Space between the window edge and the inner panel.")]
        public int frameMargin = 7;
        [Tooltip("Space reserved at the top for the title chip.")]
        public int titleH = 32;
        [Tooltip("Space reserved at the bottom for the buttons: smaller = longer list.")]
        public int bottomH = 70;
        [Tooltip("Button top, measured from the window's bottom: smaller = lower buttons.")]
        public int buttonFromBottom = 28;
        [Tooltip("Left margin of the text names inside the list.")]
        public int labelX = 10;
        [Tooltip("Empty space above the first row.")]
        public int listTop = 8;
        [Tooltip("Value box is drawn this much above the row's text line.")]
        public int boxOffsetY = 3;
        [Tooltip("Value text is drawn this much below the row's text line.")]
        public int boxTextOffsetY = 2;
        [Tooltip("Allowed range of every offset: -limit .. limit.")]
        public float limit = 100f;
        [Tooltip("Left / Right arrow step.")]
        public float step = 0.1f;
        [Tooltip("Left / Right arrow step while Shift is held.")]
        public float shiftStep = 1f;
    }

    // Settings window (F6): tabs, rows of options, the hint line and the two buttons.
    [Serializable]
    public class SettingsWindow
    {
        [Tooltip("Window width.")]
        public int windowW = 300;
        [Tooltip("Largest window height (it also never gets taller than the screen).")]
        public int windowMaxH = 300;
        [Tooltip("Height of the tab row at the top of the inner panel.")]
        public int tabH = 18;
        [Tooltip("Height of one option row.")]
        public int rowH = 20;
        [Tooltip("Left margin of the option names.")]
        public int labelX = 8;
        [Tooltip("Width of the value control on the right of a row (< value > or the slider and its number).")]
        public int controlW = 104;
        [Tooltip("Width of the slider bar.")]
        public int sliderW = 70;
        [Tooltip("Space between the control and the right edge of the panel.")]
        public int controlRightMargin = 8;
        [Tooltip("Vertical position of the row text inside a row.")]
        public int rowTextY = 5;
        [Tooltip("Height of the help box between the list and the buttons (two text lines).")]
        public int hintH = 24;
        [Tooltip("Space between the list, the help box and the buttons.")]
        public int hintGap = 4;
        [Tooltip("Width of the On / Off switch.")]
        public int switchW = 26;
    }

    [SerializeField]
    private BottomButtons bottomButtons = new BottomButtons();
    [SerializeField]
    private PotionButtons potionButtons = new PotionButtons();
    [SerializeField]
    private NpcMenu npcMenu = new NpcMenu();
    [SerializeField]
    private DeveloperWindow developerWindow = new DeveloperWindow();
    [SerializeField]
    private SettingsWindow settingsWindow = new SettingsWindow();
    private static NsoUiLayout active;
    private static readonly BottomButtons defaultBottomButtons = new BottomButtons();
    private static readonly PotionButtons defaultPotionButtons = new PotionButtons();
    private static readonly NpcMenu defaultNpcMenu = new NpcMenu();
    private static readonly DeveloperWindow defaultDeveloperWindow = new DeveloperWindow();
    private static readonly SettingsWindow defaultSettingsWindow = new SettingsWindow();
    // Values in use: this object's when it is in the scene and enabled, else the defaults.
    public static BottomButtons Buttons
    {
        get
        {
            return RuntimeConfiguration.Value("ui", "bottomButtons", active != null ? active.bottomButtons : defaultBottomButtons);
        }
    }

    public static PotionButtons Potions
    {
        get
        {
            return RuntimeConfiguration.Value("ui", "potionButtons", active != null ? active.potionButtons : defaultPotionButtons);
        }
    }

    public static NpcMenu Menu
    {
        get
        {
            return RuntimeConfiguration.Value("ui", "npcMenu", active != null ? active.npcMenu : defaultNpcMenu);
        }
    }

    public static DeveloperWindow Window
    {
        get
        {
            return RuntimeConfiguration.Value("ui", "developerWindow", active != null ? active.developerWindow : defaultDeveloperWindow);
        }
    }

    public static SettingsWindow Settings
    {
        get
        {
            return RuntimeConfiguration.Value("ui", "settingsWindow", active != null ? active.settingsWindow : defaultSettingsWindow);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
    }

    private void OnEnable()
    {
        active = this;
        NsoConfigurationBridge.Register("ui", this);
    }

    private void OnValidate()
    {
        NsoConfigurationBridge.Register("ui", this);
    }

    private void OnDisable()
    {
        if (active == this)
            active = null;
    }
}
