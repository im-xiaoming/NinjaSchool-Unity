using UnityEngine;

// Developer / Admin window (F6), built like the game's Features > Auto window: the popup frame, the title chip, the dark
// inner panel with a white border, rows with value boxes, the game's button image and the green tip line.
// It lists every kind of text with its own x / y offset. TextCalib.Open means "the window is open".
//   Up / Down   select a text            Left / Right   change the value by 0.1 (Shift: 1)
//   Space       switch between X and Y   Enter or typing   enter a value (validated), Enter confirms, Esc cancels
//   Tap a value to select it; tap a text in the game and drag it to move it.
public static class TextAlignPanel
{
    // ---- Layout numbers (game pixels, scaled by the UI zoom when drawn): edit them on the "NSO UI Layout" object ----
    private static int ValueRow
    {
        get
        {
            return NsoUiLayout.Window.valueRow;
        }
    }

    private static int ButtonH
    {
        get
        {
            return NsoUiLayout.Window.buttonH;
        }
    }

    private static int BoxW
    {
        get
        {
            return NsoUiLayout.Window.boxW;
        }
    }

    private static int BoxH
    {
        get
        {
            return NsoUiLayout.Window.boxH;
        }
    }

    private static int BoxGap
    {
        get
        {
            return NsoUiLayout.Window.boxGap;
        }
    }

    private static int BoxRightMargin
    {
        get
        {
            return NsoUiLayout.Window.boxRightMargin;
        }
    }

    private static int WindowW
    {
        get
        {
            return NsoUiLayout.Window.windowW;
        }
    }

    private static int WindowMaxH
    {
        get
        {
            return NsoUiLayout.Window.windowMaxH;
        }
    }

    private static int FrameMargin
    {
        get
        {
            return NsoUiLayout.Window.frameMargin;
        }
    }

    private static int TitleH
    {
        get
        {
            return NsoUiLayout.Window.titleH;
        }
    }

    private static int BottomH
    {
        get
        {
            return NsoUiLayout.Window.bottomH;
        }
    }

    private static int ButtonFromBottom
    {
        get
        {
            return NsoUiLayout.Window.buttonFromBottom;
        }
    }

    private static int LabelX
    {
        get
        {
            return NsoUiLayout.Window.labelX;
        }
    }

    private static int ListTop
    {
        get
        {
            return NsoUiLayout.Window.listTop;
        }
    }

    private static int BoxOffsetY
    {
        get
        {
            return NsoUiLayout.Window.boxOffsetY;
        }
    }

    private static int BoxTextOffsetY
    {
        get
        {
            return NsoUiLayout.Window.boxTextOffsetY;
        }
    }

    private static float Limit
    {
        get
        {
            return NsoUiLayout.Window.limit;
        }
    }

    private static int column; // 0 = x, 1 = y
    private static bool editing; // a value is being typed
    private static string buffer = string.Empty; // what has been typed so far
    private static bool dragText, pressedInList, scrolled; // pointer states: dragging a game text / pressed in the list / list scrolled
    private static int pressY, scrollY, pressScroll; // pointer start y, list scroll position and the scroll when pressed
    private static Vector3 lastMouse; // last mouse position while dragging a game text
    private static string notice = string.Empty; // validation message under the list (empty = none)
    // Size and place the window (centred horizontally); called before every draw / pointer event.
    private static void Layout()
    {
        GameScr.setPopupSize(WindowW, Mathf.Min(WindowMaxH, GameCanvas.h - 6));
        GameScr.popupX = GameScr.gW2 - GameScr.popupW / 2;
    }

    // The dark inner panel that holds the rows.
    private static int InnerX
    {
        get
        {
            return GameScr.popupX + FrameMargin;
        }
    }

    private static int InnerY
    {
        get
        {
            return GameScr.popupY + TitleH;
        }
    }

    private static int InnerW
    {
        get
        {
            return GameScr.popupW - 2 * FrameMargin;
        }
    }

    private static int InnerH
    {
        get
        {
            return GameScr.popupH - BottomH;
        }
    }

    private static int ButtonY
    {
        get
        {
            return GameScr.popupY + GameScr.popupH - ButtonFromBottom;
        }
    }

    // Total height of all rows, and how far the list can scroll.
    private static int ContentH
    {
        get
        {
            return TextCalib.Items.Length * ValueRow + 8;
        }
    }

    private static int MaxScroll
    {
        get
        {
            return Mathf.Max(0, ContentH - InnerH);
        }
    }

    private static string[] ButtonNames
    {
        get
        {
            return new[]
            {
                "Reset",
                "Back"
            };
        }
    }

    // Left edge of button i: the first sits on the left edge of the panel, the last on the right edge.
    private static int ButtonX(int i, int count)
    {
        int w = GameScr.imgLbtn.getWidth();
        return count < 2 ? InnerX + (InnerW - w) / 2 : InnerX + i * (InnerW - w) / (count - 1);
    }

    private static bool InWindow(int x, int y)
    {
        return x >= GameScr.popupX && x <= GameScr.popupX + GameScr.popupW && y >= GameScr.popupY && y <= GameScr.popupY + GameScr.popupH;
    }

    // Left edge of the X (col 0) or Y (col 1) value box, counted from the right edge of the list.
    private static int BoxX(int col)
    {
        return InnerX + InnerW - BoxRightMargin - 2 * BoxW - BoxGap + col * (BoxW + BoxGap);
    }

    // Current offset of a row's X or Y.
    private static float Value(int row, int col)
    {
        Vector2 v = TextCalib.Get(TextCalib.Items[row]);
        return col == 0 ? v.x : v.y;
    }

    // Store an offset: clamped to the allowed range and rounded to 2 decimals.
    private static void SetValue(int row, int col, float value)
    {
        string key = TextCalib.Items[row];
        Vector2 v = TextCalib.Get(key);
        value = Mathf.Round(Mathf.Clamp(value, -Limit, Limit) * 100f) / 100f;
        if (col == 0)
            v.x = value;
        else
            v.y = value;
        TextCalib.Put(key, v);
    }

    // Scroll the list so the selected row is fully visible.
    private static void ScrollToSelected()
    {
        int top = TextCalib.Selected * ValueRow, bottom = top + ValueRow;
        if (top < scrollY)
            scrollY = top;
        else if (bottom > scrollY + InnerH - 8)
            scrollY = bottom - InnerH + 8;
        scrollY = Mathf.Clamp(scrollY, 0, MaxScroll);
    }

    // Validate the typed text: only a number inside -Limit..Limit is accepted, anything else shows a message.
    private static void Commit()
    {
        float parsed;
        string text = buffer.Replace(',', '.');
        if (text.Length == 0 || text == "-" || text == "." || text == "-.")
        {
            notice = "Not a number";
        }
        else if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
        {
            notice = "Not a number";
        }
        else if (float.IsNaN(parsed) || float.IsInfinity(parsed) || parsed < -Limit || parsed > Limit)
        {
            notice = "Allowed: -" + Limit + " to " + Limit;
        }
        else
        {
            SetValue(TextCalib.Selected, column, parsed);
            notice = string.Empty;
        }

        editing = false;
        buffer = string.Empty;
    }

    // Left / Right: change the focused value by 0.1 (Shift: 1).
    private static void Step(int direction, bool shift)
    {
        float step = shift ? NsoUiLayout.Window.shiftStep : NsoUiLayout.Window.step;
        SetValue(TextCalib.Selected, column, Value(TextCalib.Selected, column) + direction * step);
    }

    // Keyboard, called from TextCalib's OnGUI while the window is open. Returns true when the key was used.
    public static bool Key(Event e)
    {
        if (e.type != EventType.KeyDown)
            return false;
        if (e.keyCode == KeyCode.Escape)
        {
            if (editing)
            {
                editing = false;
                buffer = string.Empty;
                notice = string.Empty;
            }
            else
                NsoSettingsMenu.CloseTextOffsets(); // Esc goes back to the Settings window
            return true;
        }

        int n = TextCalib.Items.Length;
        switch (e.keyCode)
        {
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
                if (editing)
                    Commit();
                TextCalib.Selected = (TextCalib.Selected + (e.keyCode == KeyCode.UpArrow ? n - 1 : 1)) % n;
                ScrollToSelected();
                return true;
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
                if (!editing)
                {
                    Step(e.keyCode == KeyCode.LeftArrow ? -1 : 1, e.shift);
                    notice = string.Empty;
                }

                return true;
            case KeyCode.Space:
                if (!editing)
                    column = 1 - column;
                return true;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (editing)
                    Commit();
                else
                {
                    editing = true;
                    buffer = Value(TextCalib.Selected, column).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                    notice = string.Empty;
                }

                return true;
            case KeyCode.Backspace:
                if (editing && buffer.Length > 0)
                    buffer = buffer.Substring(0, buffer.Length - 1);
                return true;
        }

        char c = e.character;
        if ((c >= '0' && c <= '9') || c == '.' || c == ',' || c == '-')
        {
            if (!editing)
            {
                editing = true;
                buffer = string.Empty;
                notice = string.Empty;
            }

            if (buffer.Length < 8)
                buffer += c;
            return true;
        }

        return true; // while the window is open the keyboard belongs to it
    }

    // Reset (0) puts the selected text back to offset 0; Back (1) returns to the Settings window.
    private static void PressButton(int i)
    {
        Sound.play(Sound.MBClick, 0.4f);
        switch (i)
        {
            case 0:
                TextCalib.ResetItem(TextCalib.Items[TextCalib.Selected]);
                break;
            default:
                NsoSettingsMenu.CloseTextOffsets();
                break;
        }
    }

    // phase 0 = pressed, 1 = dragged, 2 = released. Returns true: the window handled the pointer, the game must not.
    public static bool Pointer(int x, int y, int phase)
    {
        Layout();
        if (phase == 1)
        {
            if (dragText)
            {
                Vector3 m = NsoInputSystemBridge.MousePosition;
                float z = Mathf.Max(1, mGraphics.zoomLevel);
                string key = TextCalib.Items[TextCalib.Selected];
                Vector2 v = TextCalib.Get(key);
                v.x = Mathf.Round(Mathf.Clamp(v.x + (m.x - lastMouse.x) / z, -Limit, Limit) * 100f) / 100f;
                v.y = Mathf.Round(Mathf.Clamp(v.y - (m.y - lastMouse.y) / z, -Limit, Limit) * 100f) / 100f; // screen y is up, game y is down
                TextCalib.Put(key, v);
                lastMouse = m;
            }
            else if (pressedInList)
            {
                if (Mathf.Abs(y - pressY) > 4)
                    scrolled = true;
                if (scrolled)
                    scrollY = Mathf.Clamp(pressScroll - (y - pressY), 0, MaxScroll);
            }

            return true;
        }

        if (phase == 2)
        {
            if (pressedInList && !scrolled)
            {
                int row = (pressY - InnerY - 4 + scrollY) / ValueRow;
                if (row >= 0 && row < TextCalib.Items.Length)
                {
                    Sound.play(Sound.MBClick, 0.4f);
                    if (editing)
                        Commit();
                    TextCalib.Selected = row;
                    if (lastPressX >= BoxX(1) - 2)
                        column = 1;
                    else if (lastPressX >= BoxX(0) - 2)
                        column = 0;
                }
            }

            dragText = false;
            pressedInList = false;
            return true;
        }

        pressY = y;
        lastPressX = x;
        scrolled = false;
        if (!InWindow(x, y))
        {
            int hit = TextCalib.HitTest(x * mGraphics.zoomLevel, y * mGraphics.zoomLevel, 6f);
            if (hit >= 0)
            {
                TextCalib.Selected = hit;
                ScrollToSelected();
                dragText = true;
                lastMouse = NsoInputSystemBridge.MousePosition;
            }

            return true;
        }

        if (y >= ButtonY && y <= ButtonY + ButtonH)
        {
            string[] names = ButtonNames;
            for (int i = 0; i < names.Length; i++)
            {
                int bx = ButtonX(i, names.Length);
                if (x >= bx && x <= bx + GameScr.imgLbtn.getWidth())
                {
                    PressButton(i);
                    break;
                }
            }

            return true;
        }

        if (x >= InnerX && x <= InnerX + InnerW && y >= InnerY && y <= InnerY + InnerH)
        {
            pressedInList = true;
            pressScroll = scrollY;
        }

        return true;
    }

    private static int lastPressX;
    // One value box: light fill, white border (yellow when focused) and the centred number.
    private static void ValueBox(mGraphics g, int x, int y, string text, bool focused, mFont font)
    {
        g.setColor(Paint.COLORLIGHT);
        g.fillRect(x, y - BoxOffsetY, BoxW, BoxH);
        g.setColor(focused ? 0xFFFF00 : 16777215);
        g.drawRect(x, y - BoxOffsetY, BoxW, BoxH);
        font.drawString(g, text, x + BoxW / 2, y + BoxTextOffsetY, mFont.CENTER);
    }

    // Paint the window, called from GameCanvas.paint while TextCalib.Open.
    public static void Draw(mGraphics g)
    {
        Layout();
        GameCanvas.resetTrans(g);
        int zoom = Mathf.Max(1, mGraphics.zoomLevel);
        // boxes around the texts that are on screen
        for (int i = 0; i < TextCalib.Items.Length; i++)
        {
            Rect r;
            if (!TextCalib.TryLive(TextCalib.Items[i], out r))
                continue;
            g.setColor(i == TextCalib.Selected ? 0xFFFF00 : 0xFFFFFF);
            g.drawRect(Mathf.RoundToInt(r.x / zoom), Mathf.RoundToInt(r.y / zoom), Mathf.RoundToInt(r.width / zoom), Mathf.RoundToInt(r.height / zoom));
        }

        GameCanvas.paintz.paintFrame(GameScr.popupX, GameScr.popupY, GameScr.popupW, GameScr.popupH, g);
        g.setColor(Paint.COLORDARK);
        g.fillRect(InnerX, InnerY, InnerW, InnerH);
        g.setColor(16777215);
        g.drawRect(InnerX, InnerY, InnerW, InnerH);
        GameScr.gI().paintTitle(g, "Developer / Admin", false);
        scrollY = Mathf.Clamp(scrollY, 0, MaxScroll);
        g.setClip(InnerX + 1, InnerY + 1, InnerW - 1, InnerH - 1);
        int x = InnerX + LabelX, y = InnerY + ListTop - scrollY;
        for (int i = 0; i < TextCalib.Items.Length; i++)
        {
            Rect r;
            bool live = TextCalib.TryLive(TextCalib.Items[i], out r);
            int ry = y + i * ValueRow;
            if (ry - 4 < InnerY + 1 || ry - 4 + ValueRow > InnerY + InnerH)
                continue;
            bool sel = i == TextCalib.Selected;
            if (sel)
            {
                g.setColor(Paint.COLORLIGHT);
                g.fillRect(InnerX + 3, ry - 4, InnerW - 5, ValueRow);
            }

            mFont f = sel ? mFont.tahoma_7b_yellow : (live ? mFont.tahoma_7_white : mFont.tahoma_7_grey);
            f.drawString(g, TextCalib.Labels[i], x, ry, 0);
            for (int c = 0; c < 2; c++)
            {
                bool focused = sel && column == c;
                string text = focused && editing ? buffer + ((GameCanvas.gameTick % 8 <= 3) ? "|" : string.Empty) : Value(i, c).ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture);
                ValueBox(g, BoxX(c), ry, text, focused, f);
            }
        }

        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        string[] names = ButtonNames;
        for (int i = 0; i < names.Length; i++)
        {
            int bx = ButtonX(i, names.Length);
            g.drawImage(GameScr.imgLbtn, bx, ButtonY, 0);
            mFont.tahoma_7b_yellow.drawString(g, names[i], bx + GameScr.imgLbtn.getWidth() / 2, ButtonY + 4, 2);
        }

        if (notice.Length > 0)
            mFont.tahoma_7_green.drawString(g, notice, GameScr.popupX + GameScr.popupW / 2, GameScr.popupY + GameScr.popupH - 17, 2);
    }
}
