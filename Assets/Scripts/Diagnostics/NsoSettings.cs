using System;
using System.Collections.Generic;
using UnityEngine;

// Player settings shown in the Settings window (F6, NsoSettingsMenu). Every option is an int value with a getter and a
// setter that changes the game right away. Changed values are saved in PlayerPrefs ("nsoset_<key>") and applied again on
// the next start, on top of the Inspector values. "Default" is the value the game had at start before any saved value
// (so the Inspector stays the source of the defaults).
public static class NsoSettings
{
    public enum Kind { Choice, Slider, Action, Info }

    public sealed class Option
    {
        public string key;          // PlayerPrefs key (null = not saved, e.g. sound on/off which the game saves itself)
        public string label;        // name shown on the left
        public string hint;         // help line shown under the list while the row is selected
        public Kind kind;
        public string[] choices;    // Choice: the names of the values 0..n-1
        public int min, max, step;  // Slider: range and arrow-key step
        public string unit = "";    // Slider: text after the number
        public bool restart;        // the value applies on the next start
        public bool toggle;         // Choice drawn as an On / Off switch (Tắt / Bật)
        public Func<int> get;
        public Action<int> set;
        public Action run;          // Action: what Enter / click does
        public string info;         // Info: the text on the right
        public int def;             // value at start (before saved values)

        public int Value { get { return get != null ? get() : 0; } }

        // Text shown for the current value.
        public string ValueText
        {
            get
            {
                switch (kind)
                {
                    case Kind.Choice: { int v = Mathf.Clamp(Value, 0, choices.Length - 1); return choices[v]; }
                    case Kind.Slider: return Value + unit;
                    case Kind.Action: return "Mở  >";
                    default: return info ?? string.Empty;
                }
            }
        }

        // Set a value (clamped / wrapped) and save it.
        public void Put(int v)
        {
            if (set == null) return;
            if (kind == Kind.Choice) v = ((v % choices.Length) + choices.Length) % choices.Length;
            else if (kind == Kind.Slider) v = Mathf.Clamp(v, min, max);
            set(v);
            if (key != null) { PlayerPrefs.SetInt(Prefix + key, v); PlayerPrefs.Save(); }
        }

        // Back to the start value and forget the saved one.
        public void Reset()
        {
            if (set == null) return;
            set(def);
            if (key != null) { PlayerPrefs.DeleteKey(Prefix + key); PlayerPrefs.Save(); }
        }
    }

    public sealed class Tab
    {
        public string name;
        public readonly List<Option> options = new List<Option>();
    }

    private const string Prefix = "nsoset_";
    public static readonly List<Tab> Tabs = new List<Tab>();

    // ---- values that only exist here ----
    public static bool showFps;                 // FPS counter in the top-right corner (NsoSettingsMenu.PaintOverlay)
    private static int pendingWindowMode = -1;  // saved window mode, applied on the first frame (after Main.Start)

    private static readonly int[] FpsChoices = { 30, 60, 75, 120, 144 };

    // FXAA presets (index 1..3 of the anti-aliasing choice): contrast threshold, relative threshold, sub-pixel blend
    private static readonly float[,] AaPresets = { { 0.0833f, 0.166f, 0.25f }, { 0.0625f, 0.125f, 0.5f }, { 0.0312f, 0.063f, 0.75f } };

    // ---- small helpers for building the lists ----
    private static Tab AddTab(string name) { Tab t = new Tab { name = name }; Tabs.Add(t); return t; }

    private static Option Choice(Tab t, string key, string label, string hint, string[] choices, Func<int> get, Action<int> set)
    {
        Option o = new Option { key = key, label = label, hint = hint, kind = Kind.Choice, choices = choices, get = get, set = set };
        t.options.Add(o);
        return o;
    }

    private static Option Toggle(Tab t, string key, string label, string hint, Func<bool> get, Action<bool> set)
    {
        Option o = Choice(t, key, label, hint, new[] { "Tắt", "Bật" }, () => get() ? 1 : 0, v => set(v != 0));
        o.toggle = true;
        return o;
    }

    private static Option Slider(Tab t, string key, string label, string hint, int min, int max, int step, string unit, Func<int> get, Action<int> set)
    {
        Option o = new Option { key = key, label = label, hint = hint, kind = Kind.Slider, min = min, max = max, step = step, unit = unit, get = get, set = set };
        t.options.Add(o);
        return o;
    }

    private static void Info(Tab t, string label, string info)
    {
        t.options.Add(new Option { label = label, kind = Kind.Info, info = info, hint = string.Empty });
    }

    private static int Percent(float v) { return Mathf.RoundToInt(Mathf.Clamp01(v) * 100f); }

    // Offline workbench shortcut. Action rows run on the main thread; resolve the live fallback port on click.
    private static void OpenWorkbench(Option option)
    {
        string url = ServerBootstrap.WikiUrl;
        if (string.IsNullOrEmpty(url))
        {
            option.hint = "Web chưa sẵn sàng. Cần bật Dev Wiki và chạy game trong Editor hoặc bản development.";
            return;
        }
        try
        {
            Application.OpenURL(url + "#dashboard");
            option.hint = "Đã gửi yêu cầu mở trình duyệt: " + url;
        }
        catch (Exception)
        {
            option.hint = "Không mở được trình duyệt. Mở thủ công: " + url;
        }
    }

    // ---- the option lists ----
    private static void Build()
    {
        Tabs.Clear();

        Tab display = AddTab("Hình ảnh");
        Choice(display, "windowMode", "Chế độ cửa sổ", "Toàn màn hình hoặc cửa sổ (cũng đổi bằng F11 / Alt+Enter). Không có tác dụng trong Editor.",
            new[] { "Toàn màn hình", "Cửa sổ" },
            () => Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0,
            v => { if (!Application.isEditor) Screen.fullScreenMode = v == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow; });
        Toggle(display, "vsync", "VSync", "Đồng bộ với tần số màn hình, chống xé hình. Khi bật, giới hạn FPS bị bỏ qua.",
            () => QualitySettings.vSyncCount > 0, on => QualitySettings.vSyncCount = on ? 1 : 0);
        Choice(display, "fps", "Giới hạn FPS", "Số khung hình tối đa mỗi giây.",
            new[] { "30", "60", "75", "120", "144" },
            () => { int f = NsoGameControl.Active != null ? NsoGameControl.Active.targetFrameRate : Application.targetFrameRate; int best = 1; for (int i = 0; i < FpsChoices.Length; i++) if (Mathf.Abs(FpsChoices[i] - f) < Mathf.Abs(FpsChoices[best] - f)) best = i; return best; },
            v => { if (NsoGameControl.Active != null) NsoGameControl.Active.targetFrameRate = FpsChoices[v]; else Application.targetFrameRate = FpsChoices[v]; });
        Choice(display, "antiAliasing", "Khử răng cưa", "FXAA cho toàn bộ hình game. Cao = mượt hơn nhưng chữ hơi mềm.",
            new[] { "Tắt", "Thấp", "Vừa", "Cao" },
            () =>
            {
                NsoGameLayer l = NsoGameLayer.Active;
                if (l == null || !l.antiAliasing) return 0;
                return l.subpixelBlending >= 0.7f ? 3 : l.subpixelBlending >= 0.4f ? 2 : 1;
            },
            v =>
            {
                NsoGameLayer l = NsoGameLayer.Active;
                if (l == null) return;
                l.antiAliasing = v > 0;
                if (v == 0) return;
                l.contrastThreshold = AaPresets[v - 1, 0];
                l.relativeThreshold = AaPresets[v - 1, 1];
                l.subpixelBlending = AaPresets[v - 1, 2];
            });
        Toggle(display, "lowGraphic", "Đồ họa thấp", "Bỏ mây, nền và một số hiệu ứng để chạy nhẹ hơn.",
            () => GameCanvas.lowGraphic, on => GameCanvas.lowGraphic = on);
        Choice(display, "uiZoom", "Cỡ giao diện", "Độ phóng to của game (Tự động theo chiều cao màn hình). Áp dụng khi khởi động lại.",
            new[] { "Tự động", "1x", "2x", "3x", "4x" },
            () => Mathf.Clamp(NsoGameControl.UiZoomOverride, 0, 4),
            v => { NsoGameControl.UiZoomOverride = v; if (NsoGameControl.Active != null) NsoGameControl.Active.uiZoom = v; }).restart = true;
        Toggle(display, "showFps", "Hiện FPS", "Hiện số khung hình mỗi giây ở góc trên bên phải.",
            () => showFps, on => showFps = on);

        Tab audio = AddTab("Âm thanh");
        Toggle(audio, null, "Âm thanh", "Bật / tắt toàn bộ âm thanh của game.",
            () => Sound.isSound, on => { if (on != Sound.isSound) Sound.SetSound(on); });
        Slider(audio, "masterVolume", "Âm lượng chung", "Âm lượng tổng.", 0, 100, 5, "%",
            () => Percent(AudioListener.volume), v => AudioListener.volume = v / 100f);
        Slider(audio, "sfxVolume", "Hiệu ứng", "Tiếng chiêu, tiếng bấm nút, tiếng bước chân.", 0, 100, 5, "%",
            () => Percent(Sound.sfxVolume), v => { Sound.sfxVolume = v / 100f; Sound.ApplyLoopVolumes(); });
        Slider(audio, "ambientVolume", "Nhạc nền / môi trường", "Nhạc nền và tiếng nước, gió của bản đồ.", 0, 100, 5, "%",
            () => Percent(Sound.ambientVolume), v => { Sound.ambientVolume = v / 100f; Sound.ApplyLoopVolumes(); });

        Toggle(audio, "menuMusic", "Nhạc menu", "Nhạc nền ở màn hình chọn server / nhân vật. Âm lượng theo mục Nhạc nền.",
            () => MenuAudio.menuMusic, on => MenuAudio.menuMusic = on);
        Toggle(audio, "uiSounds", "Âm thanh nút bấm", "Tiếng bấm khi chọn nút ở các màn hình menu.",
            () => MenuAudio.uiSounds, on => MenuAudio.uiSounds = on);

        Tab game = AddTab("Chơi");
        Toggle(game, "runInBackground", "Chạy khi thu nhỏ", "Game vẫn chạy khi cửa sổ không được chọn.",
            () => NsoGameControl.Active != null ? NsoGameControl.Active.runInBackground : Application.runInBackground,
            on => { if (NsoGameControl.Active != null) NsoGameControl.Active.runInBackground = on; Application.runInBackground = on; });

        Tab ui = AddTab("Giao diện");
        Slider(ui, "btnBottomGap", "Nút dưới: cách đáy", "Khoảng cách từ nút Chọn / Đóng tới đáy màn hình (mở lại màn hình để thấy hết).", 0, 40, 1, "",
            () => NsoUiLayout.Buttons.bottomGap, v => NsoUiLayout.Buttons.bottomGap = v);
        Slider(ui, "btnSidePadding", "Nút dưới: cách lề", "Khoảng cách từ nút trái / phải tới mép màn hình.", 0, 40, 1, "",
            () => NsoUiLayout.Buttons.sidePadding, v => NsoUiLayout.Buttons.sidePadding = v);
        Slider(ui, "potionBottomGap", "Nút bình HP/MP: cách đáy", "Khoảng cách từ 2 nút bình tới đáy màn hình.", 0, 40, 1, "",
            () => NsoUiLayout.Potions.bottomGap, v => NsoUiLayout.Potions.bottomGap = v);
        Slider(ui, "menuBottomGap", "Menu NPC: cách đáy", "Khoảng cách từ hàng nút NPC tới đáy màn hình (áp dụng khi mở menu).", 0, 40, 1, "",
            () => NsoUiLayout.Menu.bottomGap, v => NsoUiLayout.Menu.bottomGap = v);

        Tab keys = AddTab("Phím");
        Info(keys, "Mở / đóng Cài đặt", "F6");
        Info(keys, "Toàn màn hình", "F11 / Alt+Enter");
        Info(keys, "Chậm / nhanh game", "F9 / F10");
        Info(keys, "Đóng / quay lại", "Esc");
        Info(keys, "Menu NPC: trái / phải", "A / D, mũi tên");
        Info(keys, "Cài đặt: đổi tab", "Q / E, Tab");
        Info(keys, "Cài đặt: đổi giá trị", "Mũi tên trái / phải (Shift: x5)");
        Info(keys, "Cài đặt: về mặc định", "Backspace");

        Tab dev = AddTab("Dev");
        var workbench = new Option {
            label = "Mở web cân bằng",
            hint = "Mở Workbench bằng trình duyệt mặc định, dùng đúng cổng web đang chạy.",
            kind = Kind.Action
        };
        workbench.run = () => OpenWorkbench(workbench);
        dev.options.Add(workbench);
        dev.options.Add(new Option { label = "Căn chỉnh chữ", hint = "Bảng chỉnh vị trí X / Y của từng loại chữ (công cụ cũ của F6).", kind = Kind.Action, run = NsoSettingsMenu.OpenTextOffsets });
        Slider(dev, "tickRate", "Tốc độ logic game", "Số nhịp logic mỗi giây (24 = gốc). F9 / F10 cũng đổi được.", 10, 60, 2, "",
            () => NsoGameControl.Active != null ? NsoGameControl.Active.logicTicksPerSecond : (Main.main != null ? Main.main.tickRate : 24),
            v => { if (NsoGameControl.Active != null) NsoGameControl.Active.logicTicksPerSecond = v; if (Main.main != null) { Main.main.tickRate = v; Time.fixedDeltaTime = 1f / v; } });
    }

    // Build the lists, remember the start values as defaults and apply the saved values. Runs after the scene's
    // Awake / OnEnable (so the Inspector objects exist) and before Main.Start (so the UI zoom is read in time).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        Build();
        foreach (Tab t in Tabs)
        {
            foreach (Option o in t.options)
            {
                if (o.get == null) continue;
                o.def = o.get();
                if (o.key == null || !PlayerPrefs.HasKey(Prefix + o.key)) continue;
                int v = PlayerPrefs.GetInt(Prefix + o.key);
                if (o.key == "windowMode") { pendingWindowMode = v; continue; }   // Main.Start sets the window; apply after it
                try { o.set(v); } catch (Exception ex) { Debug.LogWarning("Setting " + o.key + " not applied: " + ex.Message); }
            }
        }
        if (pendingWindowMode >= 0)
        {
            GameObject go = new GameObject("NsoSettings (apply)");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<LateApply>();
        }
    }

    // Applies the saved window mode on the first frame, after Main.Start chose its own, then removes itself.
    private sealed class LateApply : MonoBehaviour
    {
        private void Update()
        {
            if (pendingWindowMode >= 0 && !Application.isEditor) Screen.fullScreenMode = pendingWindowMode == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            pendingWindowMode = -1;
            Destroy(gameObject);
        }
    }
}
