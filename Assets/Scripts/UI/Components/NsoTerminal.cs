// Offline dev terminal shown in the original message window (GameScr alert popup with the message-style
// title, isPaintMessage). Like a console, the input prompt is the last row of the window itself; the
// ChatTextField only edits the text (its own box is not painted while the terminal is open).
// "/name" lines are commands: /clear runs here, the rest go to the offline server as CHAT_MAP (-23) and
// Server/Net/DevTerminal.cs answers with SERVER_MESSAGE (-24) lines that start with ReplyMarker.
// Lines without "/" are public chat: a speech bubble over the player plus a "Name: text" row in the window.
public static class NsoTerminal
{
    /// <summary>Window title (drawn like a private-message tab title).</summary>
    public static string Title = "Terminal";
    /// <summary>Widest terminal window in pixels (it also shrinks to the screen width minus the margins).</summary>
    public static int MaxWidth = 420;
    /// <summary>Gap between the window and the screen top/sides.</summary>
    public static int ScreenMargin = 8;
    /// <summary>Gap between the window bottom and the bottom command buttons ("Đóng").</summary>
    public static int ButtonGap = 6;
    /// <summary>Prompt drawn before the text being typed.</summary>
    public static string Prompt = "> ";
    /// <summary>Logic ticks for one caret blink phase (on, then off).</summary>
    public static int CaretBlinkTicks = 10;
    /// <summary>Caret size in UI pixels and colour (RGB): an underscore after the typed text, like a console cursor.</summary>
    public static int CaretWidth = 5, CaretHeight = 1, CaretColor = 0xFFFFFF;
    /// <summary>Caret distance from the top of the text row (puts the underscore on the baseline).</summary>
    public static int CaretTop = 10;
    /// <summary>How long a public chat bubble stays over the player (ChatPopup delay, logic ticks).</summary>
    public static int ChatBubbleTicks = 100;
    /// <summary>Written before the player name in public chat rows ("@Name: text").</summary>
    public static string NamePrefix = "@";
    /// <summary>Commands must start with this prefix, e.g. /help.</summary>
    public const string CommandPrefix = "/";
    /// <summary>Oldest lines are dropped beyond this count.</summary>
    public static int MaxLogLines = 200;
    /// <summary>Splits a log row into a yellow head and a white tail (see GameScr.paintAlert). Not typeable.</summary>
    public const char SplitMark = (char)1;
    /// <summary>First character of a terminal reply. Must match Server/Net/DevTerminal.ReplyMarker.</summary>
    public const char ReplyMarker = (char)7;
    /// <summary>Colour codes understood by GameScr.paintAlert ("c3" yellow, "c0" white, "c9" green).</summary>
    private const string EchoColor = "c3", ReplyColor = "c0", PromptColor = "c3";
    /// <summary>Terminal history (echoed commands, replies, chat).</summary>
    public static readonly MyVector Log = new MyVector();
    /// <summary>What the window paints: Log plus the prompt row. GameScr.texts points here while open.</summary>
    public static readonly MyVector View = new MyVector();
    private static int lastViewSize = -1;
    // Prompt row text as painted (without colour code), used to place the caret.
    private static string promptLine = string.Empty;
    /// <summary>True while GameScr shows the terminal window.</summary>
    public static bool IsOpen
    {
        get
        {
            GameScr scr = GameScr.instance;
            return scr != null && GameScr.isPaintMessage && GameScr.isPaintAlert && scr.texts == View;
        }
    }

    public static void Submit(string text)
    {
        if (text == null)
        {
            return;
        }

        text = text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        if (!text.StartsWith(CommandPrefix))
        {
            PublicChat(text);
            return;
        }

        AddLine(EchoColor, Prompt + text);
        // /clear is handled on the client: it only empties this window.
        if (text.Equals(CommandPrefix + "clear", System.StringComparison.OrdinalIgnoreCase))
        {
            Log.removeAllElements();
            return;
        }

        Service.gI().chat(text);
    }

    // Offline public chat: there is nobody else on the map, so the bubble is shown locally (no server round trip).
    private static void PublicChat(string text)
    {
        Char me = Char.getMyChar();
        if (me != null)
        {
            ChatPopup.addChatPopup(text, ChatBubbleTicks, me);
        }

        // First row: "@Name:" yellow + text white (GameScr.paintAlert splits at SplitMark); wrapped rows are white.
        string name = NamePrefix + (me != null ? me.cName : "") + ":";
        int width = GameScr.popupW > 60 ? GameScr.popupW - 30 : 160;
        MyVector parts = mFont.tahoma_7.splitFontVector(name + " " + text, width);
        for (int i = 0; i < parts.size(); i++)
        {
            string part = (string)parts.elementAt(i);
            if (i == 0 && part.StartsWith(name))
            {
                Log.addElement(name + SplitMark + part.Substring(name.Length));
            }
            else
            {
                Log.addElement(ReplyColor + part);
            }
        }

        TrimLog();
    }

    /// <summary>Window size: as wide as allowed, from the screen top down to just above the "Đóng" button.</summary>
    public static void ApplyLayout()
    {
        int w = System.Math.Min(MaxWidth, GameCanvas.w - 2 * ScreenMargin);
        GameScr.popupW = w;
        GameScr.popupX = GameCanvas.w / 2 - w / 2;
        GameScr.popupY = ScreenMargin;
        GameScr.popupH = System.Math.Max(80, Paint.CmdY - ButtonGap - GameScr.popupY);
    }

    /// <summary>
    /// Rebuilds View (Log + prompt row with a blinking caret). Called every update while open; scrolls to the
    /// bottom only when the row count changes, so the mouse wheel can still scroll back through history.
    /// </summary>
    public static void Refresh(string typing)
    {
        View.removeAllElements();
        for (int i = 0; i < Log.size(); i++)
        {
            View.addElement(Log.elementAt(i));
        }

        string line = Prompt + (typing ?? "") + NsoInputSystemBridge.Composition; // IME text not yet committed
        // Long input: show its tail so the caret stays visible.
        int width = GameScr.popupW > 60 ? GameScr.popupW - 30 : 160;
        while (line.Length > 1 && mFont.tahoma_7.getWidth(line) + CaretWidth + 2 > width)
        {
            line = line.Substring(1);
        }

        promptLine = line;
        View.addElement(PromptColor + line);
        if (View.size() != lastViewSize && IsOpen)
        {
            lastViewSize = View.size();
            GameScr.instance.scrollDownAlert();
            GameScr.indexRow = -1; // hide the list-selection arrow; the caret marks the input row
        }
    }

    /// <summary>
    /// Draws the blinking caret after the prompt text. Called after GameScr.paintAlert; uses the same row layout
    /// (row i at ystart + 12 * (i + 1), text at xstart + 5) and the same scroll clip as the window.
    /// </summary>
    public static void PaintCaret(mGraphics g)
    {
        if (!IsOpen || View.size() == 0)
        {
            return;
        }

        if (CaretBlinkTicks > 0 && (GameCanvas.gameTick / CaretBlinkTicks) % 2 != 0)
        {
            return;
        }

        int row = View.size() - 1;
        int x = GameScr.xstart + 5 + mFont.tahoma_7.getWidth(promptLine) + 1;
        int y = GameScr.ystart + 12 * (row + 1) + CaretTop;
        GameScr.scrMain.setClip(g);
        g.setColor(CaretColor);
        g.fillRect(x, y, CaretWidth, CaretHeight);
        g.ClearScrollPaintOffset();
        GameScr.resetTranslate(g);
    }

    /// <summary>Forces a scroll to the newest row on the next Refresh (window just opened).</summary>
    public static void ResetScroll()
    {
        lastViewSize = -1;
    }

    /// <summary>Called for every SERVER_MESSAGE. Returns true when the text was a terminal reply.</summary>
    public static bool TryAddReply(string text)
    {
        if (string.IsNullOrEmpty(text) || text[0] != ReplyMarker)
        {
            return false;
        }

        AddLine(ReplyColor, text.Substring(1));
        return true;
    }

    // Wraps to the window width and adds the colour code to every wrapped piece.
    private static void AddLine(string color, string text)
    {
        int width = GameScr.popupW > 60 ? GameScr.popupW - 30 : 160;
        MyVector parts = mFont.tahoma_7.splitFontVector(text, width);
        for (int i = 0; i < parts.size(); i++)
        {
            Log.addElement(color + (string)parts.elementAt(i));
        }

        TrimLog();
    }

    private static void TrimLog()
    {
        while (Log.size() > MaxLogLines)
        {
            Log.removeElementAt(0);
        }
    }
}
