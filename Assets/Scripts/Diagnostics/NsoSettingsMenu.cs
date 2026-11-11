using UnityEngine;

// Settings window (F6), drawn with the game's own look (popup frame, title chip, dark inner panel, button image).
// Tabs at the top, rows of options (NsoSettings) below, a help line for the selected row and the buttons
// "Mặc định" (reset this tab) and "Đóng". Every change applies at once and is saved.
//   Q / E or Tab / Shift+Tab   change tab          Up / Down     select a row
//   Left / Right               change the value (Shift: 5 steps on sliders)
//   Enter                      open an action row / next choice        Backspace   this row back to default
//   Esc                        close
// Mouse: click a tab; click the left / right half of a choice; click or drag a slider; drag the list to scroll.
// The "Dev > Căn chỉnh chữ" row opens the old text-offset tool (TextAlignPanel) inside the same window; Esc goes back.
// TextCalib.Open means "the window is open"; layout numbers are on "NSO UI Layout > Settings Window".
public static class NsoSettingsMenu
{
    private static NsoUiLayout.SettingsWindow L { get { return NsoUiLayout.Settings; } }

    public static bool TextOffsets;          // the text-offset tool is shown instead of the settings
    private static int tab, row, scrollY;    // current tab, selected row, list scroll (game pixels)
    private static bool pressedInList, scrolled, dragSlider;
    private static int pressX, pressY, pressScroll;

    // FPS counter (smoothed)
    private static float fps, fpsTime;
    private static int fpsFrames;

    // ---- open / close ----
    public static void OnOpened() { TextOffsets = false; ClampRow(); }

    public static void OpenTextOffsets() { TextOffsets = true; }

    public static void CloseTextOffsets() { TextOffsets = false; }

    private static void Close() { TextOffsets = false; TextCalib.Open = false; }

    private static NsoSettings.Tab CurrentTab { get { return NsoSettings.Tabs[tab]; } }

    private static void ClampRow()
    {
        if (NsoSettings.Tabs.Count == 0) return;
        tab = Mathf.Clamp(tab, 0, NsoSettings.Tabs.Count - 1);
        row = Mathf.Clamp(row, 0, Mathf.Max(0, CurrentTab.options.Count - 1));
    }

    // ---- layout (game pixels) ----
    private static void Layout()
    {
        GameScr.setPopupSize(L.windowW, Mathf.Min(L.windowMaxH, GameCanvas.h - 6));
        GameScr.popupX = GameScr.gW2 - GameScr.popupW / 2;
    }

    private static int FrameMargin { get { return NsoUiLayout.Window.frameMargin; } }
    private static int InnerX { get { return GameScr.popupX + FrameMargin; } }
    private static int InnerY { get { return GameScr.popupY + NsoUiLayout.Window.titleH; } }
    private static int InnerW { get { return GameScr.popupW - 2 * FrameMargin; } }
    // the inner panel stops above the help box, which stops above the buttons, so nothing overlaps
    private static int InnerH { get { return Mathf.Max(L.tabH + L.rowH, HintY - L.hintGap - InnerY); } }
    private static int HintY { get { return ButtonY - L.hintGap - L.hintH; } }
    private static int ListY { get { return InnerY + L.tabH + 2; } }
    private static int ListH { get { return InnerY + InnerH - ListY; } }
    private static int ButtonY { get { return GameScr.popupY + GameScr.popupH - NsoUiLayout.Window.buttonFromBottom; } }
    private static int ContentH { get { return CurrentTab.options.Count * L.rowH + 4; } }
    private static int MaxScroll { get { return Mathf.Max(0, ContentH - ListH); } }
    private static int ControlX { get { return InnerX + InnerW - L.controlRightMargin - L.controlW; } }
    private static int TabW { get { return InnerW / Mathf.Max(1, NsoSettings.Tabs.Count); } }
    private static int BtnW { get { return GameScr.imgLbtn != null ? GameScr.imgLbtn.getWidth() : 70; } }
    private static int ButtonX(int i) { return i == 0 ? InnerX : InnerX + InnerW - BtnW; }   // 0 = Mặc định (left), 1 = Đóng (right)

    private static void ScrollToRow()
    {
        int top = row * L.rowH, bottom = top + L.rowH;
        if (top < scrollY) scrollY = top;
        else if (bottom > scrollY + ListH - 4) scrollY = bottom - ListH + 4;
        scrollY = Mathf.Clamp(scrollY, 0, MaxScroll);
    }

    private static void SetTab(int t)
    {
        int n = NsoSettings.Tabs.Count;
        if (n == 0) return;
        tab = ((t % n) + n) % n;
        row = 0;
        scrollY = 0;
        Sound.play(Sound.MBClick, 0.4f);
    }

    // Change the selected row's value by direction (-1 / +1).
    private static void Change(NsoSettings.Option o, int direction, bool big)
    {
        if (o.kind == NsoSettings.Kind.Choice) o.Put(o.Value + direction);
        else if (o.kind == NsoSettings.Kind.Slider) o.Put(o.Value + direction * o.step * (big ? 5 : 1));
        else return;
        Sound.play(Sound.MBClick, 0.3f);
    }

    // ---- keyboard (from TextCalib.OnGUI while the window is open); true = key used ----
    public static bool Key(Event e)
    {
        if (TextOffsets) return TextAlignPanel.Key(e);
        if (e.type != EventType.KeyDown || NsoSettings.Tabs.Count == 0) return false;
        ClampRow();
        NsoSettings.Option o = CurrentTab.options.Count > 0 ? CurrentTab.options[row] : null;
        switch (e.keyCode)
        {
            case KeyCode.Escape: Close(); return true;
            case KeyCode.Q: case KeyCode.PageUp: SetTab(tab - 1); return true;
            case KeyCode.E: case KeyCode.PageDown: SetTab(tab + 1); return true;
            case KeyCode.Tab: SetTab(tab + (e.shift ? -1 : 1)); return true;
            case KeyCode.UpArrow: case KeyCode.W:
                if (CurrentTab.options.Count > 0) { row = (row + CurrentTab.options.Count - 1) % CurrentTab.options.Count; ScrollToRow(); }
                return true;
            case KeyCode.DownArrow: case KeyCode.S:
                if (CurrentTab.options.Count > 0) { row = (row + 1) % CurrentTab.options.Count; ScrollToRow(); }
                return true;
            case KeyCode.LeftArrow: case KeyCode.A: if (o != null) Change(o, -1, e.shift); return true;
            case KeyCode.RightArrow: case KeyCode.D: if (o != null) Change(o, 1, e.shift); return true;
            case KeyCode.Return: case KeyCode.KeypadEnter: case KeyCode.Space:
                if (o == null) return true;
                if (o.kind == NsoSettings.Kind.Action && o.run != null) { Sound.play(Sound.MBClick, 0.4f); o.run(); }
                else Change(o, 1, false);
                return true;
            case KeyCode.Backspace: case KeyCode.Delete:
                if (o != null) { o.Reset(); Sound.play(Sound.MBClick, 0.3f); }
                return true;
        }
        return true; // while the window is open the keyboard belongs to it
    }

    // ---- pointer (from GameCanvas): phase 0 = pressed, 1 = dragged, 2 = released; true = handled ----
    public static bool Pointer(int x, int y, int phase)
    {
        if (TextOffsets) return TextAlignPanel.Pointer(x, y, phase);
        if (NsoSettings.Tabs.Count == 0) return true;
        Layout();
        ClampRow();
        if (phase == 1)
        {
            if (dragSlider) SliderAt(CurrentTab.options[row], x);
            else if (pressedInList)
            {
                if (Mathf.Abs(y - pressY) > 4) scrolled = true;
                if (scrolled) scrollY = Mathf.Clamp(pressScroll - (y - pressY), 0, MaxScroll);
            }
            return true;
        }
        if (phase == 2)
        {
            if (pressedInList && !scrolled && !dragSlider) ClickRow(pressX, pressY);
            pressedInList = false;
            dragSlider = false;
            return true;
        }

        // pressed
        pressX = x; pressY = y; scrolled = false; dragSlider = false; pressedInList = false;
        if (x < GameScr.popupX || x > GameScr.popupX + GameScr.popupW || y < GameScr.popupY || y > GameScr.popupY + GameScr.popupH) return true;
        if (y >= InnerY && y < InnerY + L.tabH && x >= InnerX && x < InnerX + InnerW)
        {
            int t = Mathf.Clamp((x - InnerX) / TabW, 0, NsoSettings.Tabs.Count - 1);
            if (t != tab) SetTab(t);
            return true;
        }
        if (y >= ButtonY && y <= ButtonY + NsoUiLayout.Window.buttonH)
        {
            if (x >= ButtonX(0) && x <= ButtonX(0) + BtnW) { foreach (NsoSettings.Option o in CurrentTab.options) o.Reset(); Sound.play(Sound.MBClick, 0.4f); }
            else if (x >= ButtonX(1) && x <= ButtonX(1) + BtnW) { Sound.play(Sound.MBClick, 0.4f); Close(); }
            return true;
        }
        if (x >= InnerX && x <= InnerX + InnerW && y >= ListY && y <= ListY + ListH)
        {
            pressedInList = true;
            pressScroll = scrollY;
            int r = RowAt(y);
            if (r >= 0 && CurrentTab.options[r].kind == NsoSettings.Kind.Slider && x >= ControlX - 4 && x <= ControlX + L.sliderW + 4)
            {
                row = r;
                dragSlider = true;
                SliderAt(CurrentTab.options[r], x);
            }
        }
        return true;
    }

    private static int RowAt(int y)
    {
        int r = (y - ListY - 2 + scrollY) / L.rowH;
        return y >= ListY && r >= 0 && r < CurrentTab.options.Count ? r : -1;
    }

    // Select the clicked row; on a choice the left half of the control goes back, the right half forward.
    private static void ClickRow(int x, int y)
    {
        int r = RowAt(y);
        if (r < 0) return;
        row = r;
        NsoSettings.Option o = CurrentTab.options[r];
        if (x < ControlX - 4) { Sound.play(Sound.MBClick, 0.3f); return; }
        if (o.kind == NsoSettings.Kind.Choice) Change(o, x < ControlX + L.controlW / 2 ? -1 : 1, false);
        else if (o.kind == NsoSettings.Kind.Action && o.run != null) { Sound.play(Sound.MBClick, 0.4f); o.run(); }
    }

    // Slider value from the pointer x over the bar, snapped to the step.
    private static void SliderAt(NsoSettings.Option o, int x)
    {
        float t = Mathf.Clamp01((x - ControlX) / (float)Mathf.Max(1, L.sliderW));
        int v = o.min + Mathf.RoundToInt(t * (o.max - o.min) / Mathf.Max(1, o.step)) * Mathf.Max(1, o.step);
        if (v != o.Value) o.Put(v);
    }

    // ---- drawing ----
    // Always called from GameCanvas.paint: the FPS counter.
    public static void PaintOverlay(mGraphics g)
    {
        fpsFrames++;
        fpsTime += Time.unscaledDeltaTime;
        if (fpsTime >= 0.5f) { fps = fpsFrames / fpsTime; fpsFrames = 0; fpsTime = 0f; }
        if (!NsoSettings.showFps) return;
        GameCanvas.resetTrans(g);
        mFont.tahoma_7b_yellow.drawString(g, Mathf.RoundToInt(fps) + " FPS", GameCanvas.w - 4, 4, mFont.RIGHT);
    }

    // ---- colours (0xRRGGBB) ----
    private const int ColTabIdle = 0x2E0F00;     // tab not selected
    private const int ColAccent = 0xFFD200;      // yellow accent: selected tab underline, selected row bar, focused borders
    private const int ColStripe = 0x461A04;      // every second row
    private const int ColLine = 0x5E2A0A;        // thin separators
    private const int ColBox = 0x240B00;         // value box / slider track / help box
    private const int ColBoxBorder = 0x8A5A2A;
    private const int ColOn = 0x3FB34F;          // switch on, slider fill
    private const int ColOff = 0x5A5A5A;         // switch off

    // Filled rectangle with a 1 px border.
    private static void Box(mGraphics g, int x, int y, int w, int h, int fill, int border)
    {
        g.setColor(fill);
        g.fillRect(x, y, w, h);
        g.setColor(border);
        g.drawRect(x, y, w, h);
    }

    // The window, called from GameCanvas.paint while TextCalib.Open.
    public static void Draw(mGraphics g)
    {
        if (TextOffsets) { TextAlignPanel.Draw(g); return; }
        if (NsoSettings.Tabs.Count == 0) return;
        Layout();
        ClampRow();
        GameCanvas.resetTrans(g);

        GameCanvas.paintz.paintFrame(GameScr.popupX, GameScr.popupY, GameScr.popupW, GameScr.popupH, g);
        Box(g, InnerX, InnerY, InnerW, InnerH, Paint.COLORDARK, 16777215);
        GameScr.gI().paintTitle(g, "Cài đặt", false);

        // tabs: dark when idle, light with a yellow underline when selected
        int tw = TabW;
        for (int i = 0; i < NsoSettings.Tabs.Count; i++)
        {
            int tx = InnerX + 1 + i * tw;
            int w = i == NsoSettings.Tabs.Count - 1 ? InnerX + InnerW - tx : tw;   // last tab takes the rounding rest
            bool sel = i == tab;
            g.setColor(sel ? Paint.COLORLIGHT : ColTabIdle);
            g.fillRect(tx, InnerY + 1, w, L.tabH - 1);
            if (sel) { g.setColor(ColAccent); g.fillRect(tx, InnerY + L.tabH - 2, w, 2); }
            if (i > 0) { g.setColor(ColLine); g.fillRect(tx, InnerY + 3, 1, L.tabH - 6); }
            (sel ? mFont.tahoma_7b_yellow : mFont.tahoma_7_grey).drawString(g, NsoSettings.Tabs[i].name, tx + w / 2, InnerY + (L.tabH - 10) / 2, mFont.CENTER);
        }
        g.setColor(16777215);
        g.fillRect(InnerX, InnerY + L.tabH, InnerW, 1);

        // rows
        scrollY = Mathf.Clamp(scrollY, 0, MaxScroll);
        g.setClip(InnerX + 1, ListY, InnerW - 1, ListH);
        int cx = ControlX;
        for (int i = 0; i < CurrentTab.options.Count; i++)
        {
            int ry = ListY + 2 + i * L.rowH - scrollY;
            if (ry + L.rowH < ListY || ry > ListY + ListH) continue;
            NsoSettings.Option o = CurrentTab.options[i];
            bool sel = i == row;
            int rx = InnerX + 3, rw = InnerW - 5;
            // selected row: light fill with a yellow bar on the left; others: zebra stripes
            if (sel) { g.setColor(Paint.COLORLIGHT); g.fillRect(rx, ry, rw, L.rowH - 1); g.setColor(ColAccent); g.fillRect(rx, ry, 2, L.rowH - 1); }
            else if (i % 2 == 1) { g.setColor(ColStripe); g.fillRect(rx, ry, rw, L.rowH - 1); }
            int ty = ry + L.rowTextY;
            mFont label = sel ? mFont.tahoma_7b_yellow : (o.kind == NsoSettings.Kind.Info ? mFont.tahoma_7_grey : mFont.tahoma_7_white);
            label.drawString(g, o.restart ? o.label + " *" : o.label, InnerX + L.labelX, ty, 0);
            mFont valueFont = sel ? mFont.tahoma_7b_white : mFont.tahoma_7_white;
            int right = cx + L.controlW;
            switch (o.kind)
            {
                case NsoSettings.Kind.Choice:
                    if (o.toggle)
                    {
                        // On / Off switch: green with the knob on the right when on, grey with the knob on the left when off
                        bool on = o.Value != 0;
                        int sw = L.switchW, sh = 10, sx = right - sw, sy = ry + (L.rowH - sh) / 2 - 1;
                        Box(g, sx, sy, sw, sh, on ? ColOn : ColOff, sel ? ColAccent : ColBoxBorder);
                        g.setColor(16777215);
                        g.fillRect(on ? sx + sw - 9 : sx + 2, sy + 2, 8, sh - 3);
                        (on ? valueFont : mFont.tahoma_7_grey).drawString(g, o.ValueText, sx - 5, ty, mFont.RIGHT);
                    }
                    else
                    {
                        // < value > with the value in a box
                        int bx = cx + 10, bw = L.controlW - 20, by = ry + 2, bh = L.rowH - 5;
                        Box(g, bx, by, bw, bh, ColBox, sel ? ColAccent : ColBoxBorder);
                        mFont arrow = sel ? mFont.tahoma_7b_yellow : mFont.tahoma_7_grey;
                        arrow.drawString(g, "<", cx + 2, ty, 0);
                        arrow.drawString(g, ">", right - 2, ty, mFont.RIGHT);
                        valueFont.drawString(g, o.ValueText, bx + bw / 2, ty, mFont.CENTER);
                    }
                    break;
                case NsoSettings.Kind.Slider:
                {
                    int th = 5, by = ry + (L.rowH - th) / 2 - 1;
                    float t = (o.Value - o.min) / (float)Mathf.Max(1, o.max - o.min);
                    int fill = Mathf.RoundToInt(t * L.sliderW);
                    Box(g, cx, by, L.sliderW, th, ColBox, ColBoxBorder);              // track
                    g.setColor(sel ? ColAccent : ColOn);
                    g.fillRect(cx + 1, by + 1, Mathf.Max(0, fill - 1), th - 1);        // filled part
                    Box(g, cx + fill - 2, by - 3, 4, th + 6, 16777215, sel ? ColAccent : 0x303030);   // knob
                    valueFont.drawString(g, o.ValueText, right, ty, mFont.RIGHT);
                    break;
                }
                case NsoSettings.Kind.Action:
                    (sel ? mFont.tahoma_7b_yellow : mFont.tahoma_7_white).drawString(g, o.ValueText, right, ty, mFont.RIGHT);
                    break;
                default:
                    (sel ? mFont.tahoma_7b_white : mFont.tahoma_7_yellow).drawString(g, o.ValueText, right, ty, mFont.RIGHT);
                    break;
            }
            if (!sel) { g.setColor(ColLine); g.fillRect(rx, ry + L.rowH - 1, rw, 1); }   // separator
        }
        // scroll bar when the list is longer than the panel
        if (MaxScroll > 0)
        {
            int trackH = ListH - 4, barH = Mathf.Max(8, trackH * ListH / ContentH);
            int barY = ListY + 2 + (trackH - barH) * scrollY / MaxScroll;
            g.setColor(ColLine);
            g.fillRect(InnerX + InnerW - 4, ListY + 2, 2, trackH);
            g.setColor(ColAccent);
            g.fillRect(InnerX + InnerW - 4, barY, 2, barH);
        }
        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);

        // help box for the selected row (two lines at most), between the list and the buttons
        Box(g, InnerX, HintY, InnerW, L.hintH, ColBox, ColBoxBorder);
        if (CurrentTab.options.Count > 0)
        {
            NsoSettings.Option o = CurrentTab.options[row];
            string hint = o.hint ?? string.Empty;
            if (o.restart) hint += " (* = cần khởi động lại)";
            MyVector lines = mFont.tahoma_7_green.splitFontVector(hint, InnerW - 8);
            int n = Mathf.Min(2, lines.size());
            int hy = HintY + (L.hintH - n * 10) / 2;
            for (int i = 0; i < n; i++) mFont.tahoma_7_green.drawString(g, (string)lines.elementAt(i), InnerX + 4, hy + i * 10, 0);
        }
        string[] names = { "Mặc định", "Đóng" };
        for (int i = 0; i < 2; i++)
        {
            int bx = ButtonX(i);
            g.drawImage(GameScr.imgLbtn, bx, ButtonY, 0);
            mFont.tahoma_7b_yellow.drawString(g, names[i], bx + BtnW / 2, ButtonY + 4, mFont.CENTER);
        }
    }
}
