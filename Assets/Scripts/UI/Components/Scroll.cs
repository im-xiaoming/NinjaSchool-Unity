public class Scroll
{
    // ---- Offline addition: mouse wheel scrolls any up/down list under the cursor (user request 2026-11-12) ----
    /// <summary>Pixels scrolled per wheel notch.</summary>
    public static int WheelStepPx = 24;
    /// <summary>A wheel movement older than this (seconds) is dropped if no list was under the cursor.</summary>
    public static float WheelKeepSeconds = 0.25f;
    private static float wheelAccum, wheelTime;
    private static int wheelX, wheelY;
    /// <summary>Called by the input bridge with normalized wheel notches and the cursor in game coordinates.</summary>
    public static void AddWheel(float delta, int x, int y)
    {
        if (delta == 0f)
        {
            return;
        }

        wheelAccum += delta;
        wheelX = x;
        wheelY = y;
        wheelTime = UnityEngine.Time.unscaledTime;
    }

    // Takes the pending wheel movement (in pixels, positive = down) when the cursor is over this list.
    private bool TakeWheel(out int px)
    {
        px = 0;
        if (wheelAccum == 0f)
        {
            return false;
        }

        if (UnityEngine.Time.unscaledTime - wheelTime > WheelKeepSeconds)
        {
            wheelAccum = 0f;
            return false;
        }

        if (wheelX < xPos || wheelX >= xPos + width || wheelY < yPos || wheelY >= yPos + height)
        {
            return false;
        }

        px = -(int)System.Math.Round(wheelAccum * WheelStepPx);
        wheelAccum = 0f;
        return px != 0;
    }

    private static readonly System.Collections.Generic.List<System.WeakReference> paintScrolls = new System.Collections.Generic.List<System.WeakReference>();
    private int paintX, paintY, paintWidth, paintHeight, paintItems;
    private bool havePaintState;
    public Scroll()
    {
        paintScrolls.Add(new System.WeakReference(this));
    }

    public static void CaptureAllPaintStates()
    {
        for (int i = paintScrolls.Count - 1; i >= 0; i--)
        {
            var scroll = paintScrolls[i].Target as Scroll;
            if (scroll == null)
            {
                paintScrolls.RemoveAt(i);
                continue;
            }

            scroll.paintX = scroll.cmx;
            scroll.paintY = scroll.cmy;
            scroll.paintWidth = scroll.width;
            scroll.paintHeight = scroll.height;
            scroll.paintItems = scroll.nITEM;
            scroll.havePaintState = true;
        }
    }

    // Original Scroll.updatecm remains the input/inertia authority. Only its painted content moves fractionally.
    public UnityEngine.Vector2 PaintOffset(float fraction)
    {
        if (!havePaintState || !NsoGameControl.SmoothEffects || paintWidth != width || paintHeight != height || paintItems != nITEM || System.Math.Abs(cmx - paintX) > System.Math.Max(width, 1) || System.Math.Abs(cmy - paintY) > System.Math.Max(height, 1))
            return UnityEngine.Vector2.zero;
        return new UnityEngine.Vector2(cmx - UnityEngine.Mathf.Lerp(paintX, cmx, fraction), cmy - UnityEngine.Mathf.Lerp(paintY, cmy, fraction));
    }

    public int cmtoX;
    public int cmtoY;
    public int cmx;
    public int cmy;
    public int cmvx;
    public int cmvy;
    public int cmdx;
    public int cmdy;
    public int xPos;
    public int yPos;
    public int width;
    public int height;
    private int cmxLim;
    private int cmyLim;
    public static Scroll me;
    private int pointerDownTime;
    private int pointerDownFirstX;
    private int[] pointerDownLastX = new int[3];
    private bool pointerIsDowning;
    private bool isDownWhenRunning;
    private int cmRun;
    public int selectedItem;
    public int ITEM_SIZE;
    public int nITEM;
    public int ITEM_PER_LINE;
    public bool styleUPDOWN = true;
    public void clear()
    {
        havePaintState = false;
        cmtoX = 0;
        cmtoY = 0;
        cmx = 0;
        cmy = 0;
        cmvx = 0;
        cmvy = 0;
        cmdx = 0;
        cmdy = 0;
        cmxLim = 0;
        cmyLim = 0;
        width = 0;
        height = 0;
    }

    public void setClip(mGraphics g, int x, int y, int w, int h)
    {
        g.setClip(x, y, w, h - 1);
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        g.translate(-cmx, -cmy);
        var offset = PaintOffset(Interp.TickFraction);
        g.SetScrollPaintOffset(offset.x, offset.y);
    }

    public void setClip(mGraphics g)
    {
        g.setClip(xPos, yPos, width, height - 1);
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        g.translate(-cmx, -cmy);
        var offset = PaintOffset(Interp.TickFraction);
        g.SetScrollPaintOffset(offset.x, offset.y);
    }

    public ScrollResult updateKey()
    {
        if (styleUPDOWN)
        {
            return updateKeyScrollUpDown();
        }

        return updateKeyScrollLeftRight();
    }

    private ScrollResult updateKeyScrollUpDown()
    {
        int num = xPos;
        int num2 = yPos;
        int w = width;
        int h = height;
        int wheelPx;
        if (!pointerIsDowning && TakeWheel(out wheelPx))
        {
            // mouse wheel: move the target offset; updatecm eases cmy toward it like a fling
            cmRun = 0;
            cmtoY = System.Math.Max(0, System.Math.Min(cmyLim, cmtoY + wheelPx));
        }

        if (GameCanvas.isPointerDown)
        {
            if (!pointerIsDowning && GameCanvas.isPointer(num, num2, w, h))
            {
                for (int i = 0; i < pointerDownLastX.Length; i++)
                {
                    pointerDownLastX[0] = GameCanvas.py;
                }

                pointerDownFirstX = GameCanvas.py;
                pointerIsDowning = true;
                selectedItem = -1;
                isDownWhenRunning = cmRun != 0;
                cmRun = 0;
            }
            else if (pointerIsDowning)
            {
                pointerDownTime++;
                if (pointerDownTime > 5 && pointerDownFirstX == GameCanvas.py && !isDownWhenRunning)
                {
                    pointerDownFirstX = -1000;
                    if (ITEM_PER_LINE > 1)
                    {
                        int num3 = (cmtoY + GameCanvas.py - num2) / ITEM_SIZE;
                        int num4 = (cmtoX + GameCanvas.px - num) / ITEM_SIZE;
                        selectedItem = num3 * ITEM_PER_LINE + num4;
                    }
                    else
                    {
                        selectedItem = (cmtoY + GameCanvas.py - num2) / ITEM_SIZE;
                    }
                }

                int num5 = GameCanvas.py - pointerDownLastX[0];
                if (num5 != 0 && selectedItem != -1)
                {
                    selectedItem = -1;
                }

                for (int num6 = pointerDownLastX.Length - 1; num6 > 0; num6--)
                {
                    pointerDownLastX[num6] = pointerDownLastX[num6 - 1];
                }

                pointerDownLastX[0] = GameCanvas.py;
                cmtoY -= num5;
                if (cmtoY < 0)
                {
                    cmtoY = 0;
                }

                if (cmtoY > cmyLim)
                {
                    cmtoY = cmyLim;
                }

                if (cmy < 0 || cmy > cmyLim)
                {
                    num5 /= 2;
                }

                cmy -= num5;
            }
        }

        bool isFinish = false;
        if (GameCanvas.isPointerJustRelease && pointerIsDowning)
        {
            int i2 = GameCanvas.py - pointerDownLastX[0];
            GameCanvas.isPointerJustRelease = false;
            if (Res.abs(i2) < 20 && Res.abs(GameCanvas.py - pointerDownFirstX) < 20 && !isDownWhenRunning)
            {
                cmRun = 0;
                cmtoY = cmy;
                pointerDownFirstX = -1000;
                if (ITEM_PER_LINE > 1)
                {
                    int num7 = (cmtoY + GameCanvas.py - num2) / ITEM_SIZE;
                    int num8 = (cmtoX + GameCanvas.px - num) / ITEM_SIZE;
                    selectedItem = num7 * ITEM_PER_LINE + num8;
                }
                else
                {
                    selectedItem = (cmtoY + GameCanvas.py - num2) / ITEM_SIZE;
                }

                pointerDownTime = 0;
                isFinish = true;
            }
            else if (selectedItem != -1 && pointerDownTime > 5)
            {
                pointerDownTime = 0;
                isFinish = true;
            }
            else if (selectedItem == -1 && !isDownWhenRunning)
            {
                if (cmy < 0)
                {
                    cmtoY = 0;
                }
                else if (cmy > cmyLim)
                {
                    cmtoY = cmyLim;
                }
                else
                {
                    int num9 = GameCanvas.py - pointerDownLastX[0] + (pointerDownLastX[0] - pointerDownLastX[1]) + (pointerDownLastX[1] - pointerDownLastX[2]);
                    num9 = ((num9 > 10) ? 10 : ((num9 < -10) ? (-10) : 0));
                    cmRun = -num9 * 100;
                }
            }

            pointerIsDowning = false;
            pointerDownTime = 0;
            GameCanvas.isPointerJustRelease = false;
        }

        ScrollResult scrollResult = new ScrollResult();
        scrollResult.selected = selectedItem;
        scrollResult.isFinish = isFinish;
        scrollResult.isDowning = pointerIsDowning;
        return scrollResult;
    }

    private ScrollResult updateKeyScrollLeftRight()
    {
        int num = xPos;
        int y = yPos;
        int w = width;
        int h = height;
        if (GameCanvas.isPointerDown)
        {
            if (!pointerIsDowning && GameCanvas.isPointer(num, y, w, h))
            {
                for (int i = 0; i < pointerDownLastX.Length; i++)
                {
                    pointerDownLastX[0] = GameCanvas.px;
                }

                pointerDownFirstX = GameCanvas.px;
                pointerIsDowning = true;
                selectedItem = -1;
                isDownWhenRunning = cmRun != 0;
                cmRun = 0;
            }
            else if (pointerIsDowning)
            {
                pointerDownTime++;
                if (pointerDownTime > 5 && pointerDownFirstX == GameCanvas.px && !isDownWhenRunning)
                {
                    pointerDownFirstX = -1000;
                    selectedItem = (cmtoX + GameCanvas.px - num) / ITEM_SIZE;
                }

                int num2 = GameCanvas.px - pointerDownLastX[0];
                if (num2 != 0 && selectedItem != -1)
                {
                    selectedItem = -1;
                }

                for (int num3 = pointerDownLastX.Length - 1; num3 > 0; num3--)
                {
                    pointerDownLastX[num3] = pointerDownLastX[num3 - 1];
                }

                pointerDownLastX[0] = GameCanvas.px;
                cmtoX -= num2;
                if (cmtoX < 0)
                {
                    cmtoX = 0;
                }

                if (cmtoX > cmxLim)
                {
                    cmtoX = cmxLim;
                }

                if (cmx < 0 || cmx > cmxLim)
                {
                    num2 /= 2;
                }

                cmx -= num2;
            }
        }

        bool isFinish = false;
        if (GameCanvas.isPointerJustRelease && pointerIsDowning)
        {
            int i2 = GameCanvas.px - pointerDownLastX[0];
            GameCanvas.isPointerJustRelease = false;
            if (Res.abs(i2) < 20 && Res.abs(GameCanvas.px - pointerDownFirstX) < 20 && !isDownWhenRunning)
            {
                cmRun = 0;
                cmtoX = cmx;
                pointerDownFirstX = -1000;
                selectedItem = (cmtoX + GameCanvas.px - num) / ITEM_SIZE;
                pointerDownTime = 0;
                isFinish = true;
            }
            else if (selectedItem != -1 && pointerDownTime > 5)
            {
                pointerDownTime = 0;
                isFinish = true;
            }
            else if (selectedItem == -1 && !isDownWhenRunning)
            {
                if (cmx < 0)
                {
                    cmtoX = 0;
                }
                else if (cmx > cmxLim)
                {
                    cmtoX = cmxLim;
                }
                else
                {
                    int num4 = GameCanvas.px - pointerDownLastX[0] + (pointerDownLastX[0] - pointerDownLastX[1]) + (pointerDownLastX[1] - pointerDownLastX[2]);
                    num4 = ((num4 > 10) ? 10 : ((num4 < -10) ? (-10) : 0));
                    cmRun = -num4 * 100;
                }
            }

            pointerIsDowning = false;
            pointerDownTime = 0;
            GameCanvas.isPointerJustRelease = false;
        }

        ScrollResult scrollResult = new ScrollResult();
        scrollResult.selected = selectedItem;
        scrollResult.isFinish = isFinish;
        scrollResult.isDowning = pointerIsDowning;
        return scrollResult;
    }

    public void updatecm()
    {
        if (cmRun != 0 && !pointerIsDowning)
        {
            if (styleUPDOWN)
            {
                cmtoY += cmRun / 100;
                if (cmtoY < 0)
                {
                    cmtoY = 0;
                }
                else if (cmtoY > cmyLim)
                {
                    cmtoY = cmyLim;
                }
                else
                {
                    cmy = cmtoY;
                }
            }
            else
            {
                cmtoX += cmRun / 100;
                if (cmtoX < 0)
                {
                    cmtoX = 0;
                }
                else if (cmtoX > cmxLim)
                {
                    cmtoX = cmxLim;
                }
                else
                {
                    cmx = cmtoX;
                }
            }

            cmRun = cmRun * 9 / 10;
            if (cmRun < 100 && cmRun > -100)
            {
                cmRun = 0;
            }
        }

        if (cmx != cmtoX && !pointerIsDowning)
        {
            cmvx = cmtoX - cmx << 2;
            cmdx += cmvx;
            cmx += cmdx >> 4;
            cmdx &= 15;
        }

        if (cmy != cmtoY && !pointerIsDowning)
        {
            cmvy = cmtoY - cmy << 2;
            cmdy += cmvy;
            cmy += cmdy >> 4;
            cmdy &= 15;
        }
    }

    public void setStyle(int nItem, int ITEM_SIZE, int xPos, int yPos, int width, int height, bool styleUPDOWN, int ITEM_PER_LINE)
    {
        this.xPos = xPos;
        this.yPos = yPos;
        this.ITEM_SIZE = ITEM_SIZE;
        nITEM = nItem;
        this.width = width;
        this.height = height;
        this.styleUPDOWN = styleUPDOWN;
        this.ITEM_PER_LINE = ITEM_PER_LINE;
        if (styleUPDOWN)
        {
            cmyLim = nItem * ITEM_SIZE - height;
        }
        else
        {
            cmxLim = nItem * ITEM_SIZE - width;
        }

        if (cmyLim < 0)
        {
            cmyLim = 0;
        }

        if (cmxLim < 0)
        {
            cmxLim = 0;
        }
    }

    /// <summary>Offline addition: scrolls by a pixel amount from the current target, clamped to the content.</summary>
    public void ScrollBy(int px)
    {
        if (styleUPDOWN)
        {
            cmtoY = System.Math.Max(0, System.Math.Min(cmyLim, cmtoY + px));
        }
        else
        {
            cmtoX = System.Math.Max(0, System.Math.Min(cmxLim, cmtoX + px));
        }
    }

    public void moveTo(int to)
    {
        if (styleUPDOWN)
        {
            to -= (height - ITEM_SIZE) / 2;
            cmtoY = to;
            if (cmtoY < 0)
            {
                cmtoY = 0;
            }

            if (cmtoY > cmyLim)
            {
                cmtoY = cmyLim;
            }
        }
        else
        {
            to -= (width - ITEM_SIZE) / 2;
            cmtoX = to;
            if (cmtoX < 0)
            {
                cmtoX = 0;
            }

            if (cmtoX > cmxLim)
            {
                cmtoX = cmxLim;
            }
        }
    }

    public static Scroll gI()
    {
        if (me == null)
        {
            me = new Scroll();
        }

        return me;
    }
}
