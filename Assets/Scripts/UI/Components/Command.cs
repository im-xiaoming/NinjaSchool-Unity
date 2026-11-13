public class Command
{
    public string caption;
    public string[] subCaption;
    public IActionListener actionListener;
    public int idAction;
    public Image back;
    public Image focus;
    public Image img;
    public int x;
    public int y;
    // paint(): whether this button was on the bottom row (y == Paint.CmdY) last time, and that row's y then
    private bool onBottomRow;
    private int bottomRowY;
    public int w = mScreen.cmdW;
    public int h = mScreen.cmdH;
    private int lenCaption;
    private bool isFocus;
    public object p;
    public Command(string caption, IActionListener actionListener, int action, object p, int x, int y)
    {
        this.caption = caption;
        idAction = action;
        this.actionListener = actionListener;
        this.p = p;
        this.x = x;
        this.y = y;
        w = mScreen.cmdW;
        h = mScreen.cmdH;
        back = null;
        focus = null;
    }

    public Command()
    {
    }

    public Command(string caption)
    {
        this.caption = caption;
    }

    public Command(string caption, IActionListener actionListener, int action, object p)
    {
        this.caption = caption;
        idAction = action;
        this.actionListener = actionListener;
        this.p = p;
    }

    public Command(string caption, int action, object p)
    {
        this.caption = caption;
        idAction = action;
        this.p = p;
    }

    public Command(string caption, int action)
    {
        this.caption = caption;
        idAction = action;
    }

    public Command(string caption, int action, int x, int y)
    {
        this.caption = caption;
        idAction = action;
        this.x = x;
        this.y = y;
    }

    public void setpos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void performAction()
    {
        MenuAudio.Click(); // button sound on menu screens (Settings > Âm thanh > Âm thanh nút bấm)
        if (idAction > 0)
        {
            if (actionListener != null)
            {
                actionListener.perform(idAction, p);
            }
            else
            {
                GameScr.gI().actionPerform(idAction, p);
            }
        }
    }

    public void performAction(string str)
    {
        MenuAudio.Click(); // button sound on menu screens (Settings > Âm thanh > Âm thanh nút bấm)
        if (idAction > 0)
        {
            if (actionListener != null)
            {
                actionListener.perform(idAction, p);
            }
            else
            {
                GameScr.gI().actionPerform(idAction, p);
            }
        }
    }

    // when above 0 the icon image is drawn at this size (logical pixels, top-left at x,y) instead of its natural size
    public int imgDrawSize;
    // Hover highlight is on only while the bottom bar paints its left / right button (Paint.paintCmdBar).
    public static bool hoverEnabled;
    // Offline addition: the bottom button chosen with the arrow keys / gamepad in a Dialog
    // (Dialog.paint sets it only while painting its command bar). Drawn with the focused image.
    public static Command keySelected;
    public void paint(mGraphics g)
    {
        if (back != null)
        {
            g.drawImage(back, x, y, 0);
        }

        if (focus != null && isFocus)
        {
            g.drawImage(focus, x, y, 0);
        }

        if (img != null && imgDrawSize > 0)
        {
            g.drawImageScale(img, x, y, imgDrawSize, imgDrawSize, 0);
        }
        else if (img != null)
        {
            g.drawImage(img, x + mGraphics.getImageWidth(img) / 2, y + mGraphics.getImageHeight(img) / 2, mGraphics.HCENTER | mGraphics.VCENTER);
        }
        else if (caption != string.Empty)
        {
            // every caption button keeps at least "NSO UI Layout" > Bottom Buttons > Bottom Gap above the screen's bottom:
            // a button placed lower (screens hard-code GameCanvas.h - 26 etc.) is lifted to Paint.CmdY.
            // y itself is changed so the click area (input) moves with the picture.
            // a button that sat on the bottom row last frame follows the row when Bottom Gap changes (Settings > Giao diện)
            if (onBottomRow && y == bottomRowY && y != Paint.CmdY)
            {
                y = Paint.CmdY;
            }

            if (y > Paint.CmdY && y + Paint.CmdButtonH > GameCanvas.h - NsoUiLayout.Buttons.bottomGap)
            {
                y = Paint.CmdY;
            }

            onBottomRow = y == Paint.CmdY;
            bottomRowY = Paint.CmdY;
            if (!isFocus && this != keySelected && !(hoverEnabled && g.isMouseOver(x, y, GameScr.imgLbtn.getWidth(), GameScr.imgLbtn.getHeight()))) // hover = focused look
            {
                g.drawImage(GameScr.imgLbtn, x, y, 0);
            }
            else
            {
                g.drawImage(GameScr.imgLbtnFocus, x, y, 0);
            }

            TextCalib.Use("command");
            mFont.tahoma_7b_yellow.drawString(g, caption, x + 36, y + 3, 2);
            TextCalib.Clear();
        }
    }

    public bool input()
    {
        if (GameCanvas.isPointerHoldIn(x - 3, y - 3, w + 6, h + 6))
        {
            isFocus = true;
            if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick)
            {
                Sound.play(Sound.MBClick, 0.5f);
                return true;
            }
        }
        else
        {
            isFocus = false;
        }

        return false;
    }

    public bool isPosCmd()
    {
        if (x > 0 && y > 0)
        {
            return true;
        }

        return false;
    }

    public void updatePointer()
    {
        if (isPosCmd() && input())
        {
            performAction();
            GameCanvas.isPointerDown = false;
            isFocus = false;
        }
    }
}
