using Functions.Auto;

public class MapScr : mScreen, IActionListener
{
    private static MapScr instance;
    public static Image imgMap = null;
    public static Image imgX;
    public static Image imgPointer;
    public static int mapW;
    public static int mapH;
    public static int mfx;
    public static int mfy;
    public static int mpoint;
    public static int tick3;
    public static int mcmtoX;
    public static int mcmtoY;
    public static int mcmvx;
    public static int mcmvy;
    public static int mcmdx;
    public static int mcmdy;
    public static int mcmx;
    public static int mcmy;
    public static int mcmxLim;
    public static int mcmyLim;
    public static int taskmapId;
    private static int dx = 0;
    private static int dy = 0;
    public static int[] x = new int[193]
    {
        1,
        156,
        140,
        174,
        196,
        195,
        125,
        148,
        156,
        173,
        199,
        203,
        222,
        264,
        283,
        277,
        298,
        307,
        311,
        315,
        116,
        90,
        59,
        31,
        252,
        55,
        81,
        111,
        148,
        187,
        219,
        253,
        278,
        304,
        311,
        310,
        284,
        309,
        294,
        62,
        92,
        117,
        99,
        134,
        154,
        175,
        34,
        52,
        40,
        78,
        59,
        82,
        114,
        179,
        158,
        142,
        1,
        215,
        291,
        242,
        147,
        301,
        71,
        23,
        116,
        126,
        305,
        286,
        264,
        20,
        46,
        70,
        78,
        2,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1
    };
    public static int[] y = new int[193]
    {
        1,
        68,
        75,
        88,
        80,
        107,
        87,
        114,
        136,
        160,
        168,
        196,
        216,
        219,
        248,
        265,
        276,
        260,
        232,
        204,
        111,
        82,
        79,
        59,
        168,
        33,
        28,
        34,
        45,
        20,
        54,
        44,
        19,
        40,
        60,
        100,
        175,
        165,
        134,
        181,
        199,
        208,
        221,
        220,
        219,
        221,
        195,
        217,
        246,
        244,
        250,
        263,
        262,
        241,
        252,
        244,
        2,
        240,
        197,
        139,
        16,
        18,
        208,
        223,
        239,
        186,
        120,
        119,
        135,
        107,
        125,
        126,
        148,
        3,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1
    };
    private int maxPX;
    private int maxPY;
    private bool trans;
    private UnityEngine.Vector2 controllerCursorRemainder;
    private int lastX;
    private int lastY;
    public static int lastpoint;
    public MapScr()
    {
        right = new Command(mResources.CLOSE, this, 1000, null);
        // Offline deviation from MapScr: world map only, per user request.
        center = null;
        // Offline UI deviation: no Go To button on the world map, per user request.
        left = null;
    }

    protected void resetCMLim()
    {
        mcmxLim = 340 - GameCanvas.w;
        mcmyLim = 340 - GameCanvas.h;
        mfx = x[TileMap.mapID] + dx;
        mfy = y[TileMap.mapID] + dy;
        maxPX = 330 + dx;
        maxPY = 310 + dy;
        maxPX -= 10;
        maxPY -= 10;
        if (mcmxLim < 0)
        {
            mcmxLim = 0;
        }

        if (mcmyLim < 0)
        {
            mcmyLim = 0;
        }

        mcmx = (mcmy = 0);
        mcmtoX = (mcmtoY = 0);
        mcmtoX = mfx - GameCanvas.hw;
        mcmtoY = mfy - GameCanvas.hh;
    }

    public static MapScr gI()
    {
        if (instance == null)
        {
            instance = new MapScr();
        }

        return instance;
    }

    public override void switchToMe()
    {
        base.switchToMe();
        right = new Command(mResources.CLOSE, this, 1000, null);
        // Offline deviation from MapScr: world map only, per user request.
        center = null;
        left = null;
        if (imgMap == null)
        {
            imgMap = GameCanvas.loadImage("/wm");
            imgX = GameCanvas.loadImage("/u/x");
            imgPointer = GameCanvas.loadImage("/u/wpt1");
            mapW = mGraphics.getImageHeight(imgMap);
            mapH = mGraphics.getImageHeight(imgMap);
        }

        if (GameCanvas.w > mapW)
        {
            dx = GameCanvas.hw - mapW / 2 - 12;
        }

        if (GameCanvas.h > mapH)
        {
            dy = GameCanvas.hh - mapH / 2;
        }

        resetCMLim();
        findMapNearestPoint();
        TileMap.oldMapID = TileMap.mapID;
        trans = false;
        controllerCursorRemainder = UnityEngine.Vector2.zero;
        LayoutCommands();
    }

    public override void unLoad()
    {
        base.unLoad();
    }

    public override void paint(mGraphics g)
    {
        LayoutCommands();
        bool flag = false;
        g.setColor(0);
        g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
        g.translate(10, 10);
        g.translate(-mcmx, -mcmy);
        if (GameCanvas.w > mapW && GameCanvas.h > mapH)
        {
            g.drawImage(imgMap, GameCanvas.hw, GameCanvas.hh, StaticObj.VCENTER_HCENTER);
        }
        else if (GameCanvas.w > mapW)
        {
            g.drawImage(imgMap, GameCanvas.hw, 0, StaticObj.TOP_CENTER);
        }
        else if (GameCanvas.h > mapH)
        {
            g.drawImage(imgMap, 0, GameCanvas.hh, StaticObj.VCENTER_LEFT);
        }
        else
        {
            g.drawImage(imgMap, 0, 0, 0);
        }

        if (TileMap.mapID < TileMap.mapNames.Length && TileMap.mapID >= 0)
        {
            int align = 0;
            if (x[TileMap.mapID] != 1 || y[TileMap.mapID] != 1)
            {
                align = ((x[TileMap.mapID] >= 100) ? ((x[TileMap.mapID] > 200) ? 1 : 2) : 0);
                GameCanvas.paintShukiren(x[TileMap.mapID] + dx, y[TileMap.mapID] + dy, g, false);
            }

            int num4 = 0;
            int num5 = 0;
            if (taskmapId >= 0)
            {
                align = ((x[taskmapId] >= 100) ? ((x[taskmapId] > 200) ? 1 : 2) : 0);
                mFont.tahoma_7_white.drawString(g, TileMap.mapNames[taskmapId], x[taskmapId] + dx, y[taskmapId] + dy - 20, align, mFont.tahoma_7_grey);
                g.drawImage(imgX, x[taskmapId] + dx, y[taskmapId] + dy, 3);
                flag = true;
                num4 = x[taskmapId];
                num5 = y[taskmapId] - 20;
            }
            else if (x[TileMap.mapID] != 1 || y[TileMap.mapID] != 1)
            {
                num4 = x[TileMap.mapID];
                num5 = y[TileMap.mapID] - 20;
                mFont.tahoma_7_yellow.drawString(g, TileMap.mapNames[TileMap.mapID], x[TileMap.mapID] + dx, y[TileMap.mapID] + dy - 20, align, mFont.tahoma_7_grey);
            }

            for (int l = 0; l < Char.getMyChar().taskOrders.size(); l++)
            {
                TaskOrder taskOrder = (TaskOrder)Char.getMyChar().taskOrders.elementAt(l);
                if (taskOrder.mapId >= 0 && taskOrder.mapId < x.Length)
                {
                    g.drawImage(imgX, x[taskOrder.mapId] + dx, y[taskOrder.mapId] + dy, 3);
                }
            }

            if (mpoint >= 0 && ((taskmapId < 0 && TileMap.mapID != mpoint) || (taskmapId >= 0 && mpoint != taskmapId)))
            {
                align = ((x[mpoint] >= 100) ? ((x[mpoint] > 200) ? 1 : 2) : 0);
                int num6 = x[mpoint];
                int num7 = y[mpoint] - 20;
                if (num7 > num5 && num7 - num5 < 30)
                {
                    num7 += 40;
                }

                if (num7 < num5 && num5 - num7 < 20)
                {
                    num7 -= 5;
                }

                mFont.tahoma_7_yellow.drawString(g, TileMap.mapNames[mpoint], num6 + dx, num7 + dy, align, mFont.tahoma_7_grey);
            }
        }

        g.drawImage(imgPointer, mfx - 2, mfy, 0);
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        base.paint(g);
        if (flag)
        {
            g.drawImage(imgX, 10, GameCanvas.h - 12, mGraphics.VCENTER | mGraphics.HCENTER);
            mFont.tahoma_7_white.drawString(g, mResources.ME_TASK, 20, GameCanvas.h - 17, 0);
        }
    }

    public override void updateKey()
    {
        LayoutCommands();
        base.updateKey();
        if (GameCanvas.currentScreen != this)
            return;
        tick3++;
        if (tick3 > 10000)
        {
            tick3 = 0;
        }

        if (mcmx != mcmtoX || mcmy != mcmtoY)
        {
            mcmvx = mcmtoX - mcmx << 1;
            mcmvy = mcmtoY - mcmy << 1;
            mcmdx += mcmvx;
            mcmx += mcmdx >> 4;
            mcmdx &= 15;
            mcmdy += mcmvy;
            mcmy += mcmdy >> 4;
            mcmdy &= 15;
            if (mcmx < 0)
            {
                mcmx = 0;
            }

            if (mcmx > mcmxLim)
            {
                mcmx = mcmxLim;
            }

            if (mcmy < 0)
            {
                mcmy = 0;
            }

            if (mcmy > mcmyLim)
            {
                mcmy = mcmyLim;
            }
        }

        bool flag = false;
        if (GameCanvas.keyHold[Key.NUM2])
        {
            mfy -= 4;
            if (mfy < dy - 10)
            {
                mfy = dy - 10;
            }

            flag = true;
        }

        if (GameCanvas.keyHold[Key.NUM8])
        {
            mfy += 4;
            if (mfy > maxPY)
            {
                mfy = maxPY;
            }

            flag = true;
        }

        if (GameCanvas.keyHold[Key.NUM4])
        {
            mfx -= 4;
            if (mfx < dx - 10)
            {
                mfx = dx - 10;
            }

            flag = true;
        }

        if (GameCanvas.keyHold[Key.NUM6])
        {
            mfx += 4;
            if (mfx > maxPX)
            {
                mfx = maxPX;
            }

            flag = true;
        }

        if (flag)
        {
            mcmtoX = mfx - GameCanvas.hw;
            mcmtoY = mfy - GameCanvas.hh;
            findMapNearestPoint();
        }

        taskmapId = GameScr.gI().getTaskMapId();
        if (GameCanvas.isPointerClick && GameCanvas.py < Paint.CmdY - 3)
        {
            GameCanvas.isPointerClick = false;
            trans = true;
            lastX = GameCanvas.px;
            lastY = GameCanvas.py;
        }
        else if (GameCanvas.isPointerDown && trans)
        {
            mcmtoX -= GameCanvas.px - lastX;
            mcmtoY -= GameCanvas.py - lastY;
            if (mcmtoX < 0)
            {
                mcmtoX = 0;
            }

            if (mcmtoY < 0)
            {
                mcmtoY = 0;
            }

            if (mcmtoX > mcmxLim)
            {
                mcmtoX = mcmxLim;
            }

            if (mcmtoY > mcmyLim)
            {
                mcmtoY = mcmyLim;
            }

            mcmx = mcmtoX;
            mcmy = mcmtoY;
            lastX = GameCanvas.px;
            lastY = GameCanvas.py;
        }

        if (GameCanvas.isPointerJustRelease && trans)
        {
            int num = GameCanvas.pxLast - GameCanvas.px;
            int num2 = GameCanvas.pyLast - GameCanvas.py;
            if (Res.abs(num) < 10 && Res.abs(num2) < 10 && GameCanvas.py < Paint.CmdY - 3)
            {
                mfx = mcmx + GameCanvas.pxLast - 8;
                mfy = mcmy + GameCanvas.pyLast - 8;
                findMapNearestPoint();
            }

            trans = false;
            GameCanvas.isPointerJustRelease = false;
        }
    }

    // Offline fix to MapScr.updateKey: command images and hitboxes share one layout,
    // before input as well as paint; map dragging never owns the bottom command row.
    private void LayoutCommands()
    {
        int buttonW = Paint.CmdButtonW, buttonH = Paint.CmdButtonH;
        right.x = GameCanvas.isTouch && GameCanvas.w >= 320 ? GameCanvas.w / 2 + 160 - buttonW : Paint.CmdRightX;
        right.y = Paint.CmdY;
        right.w = buttonW;
        right.h = buttonH;
    }

    // Offline controller extension of MapScr.updateKey: retain fractional movement between
    // render frames and reuse the existing cursor/camera/selection. Moving is never a travel action.
    public void MoveControllerCursor(UnityEngine.Vector2 delta)
    {
        controllerCursorRemainder += new UnityEngine.Vector2(delta.x, -delta.y);
        int moveX = (int)controllerCursorRemainder.x;
        int moveY = (int)controllerCursorRemainder.y;
        controllerCursorRemainder -= new UnityEngine.Vector2(moveX, moveY);
        if (moveX == 0 && moveY == 0)
            return;
        mfx = UnityEngine.Mathf.Clamp(mfx + moveX, dx - 10, maxPX);
        mfy = UnityEngine.Mathf.Clamp(mfy + moveY, dy - 10, maxPY);
        mcmtoX = mfx - GameCanvas.hw;
        mcmtoY = mfy - GameCanvas.hh;
        findMapNearestPoint(false);
    }

    private static void findMapNearestPoint(bool allowAdminMove = true)
    {
        lastpoint = mpoint;
        mpoint = -1;
        for (int i = 0; i < x.Length; i++)
        {
            if (Res.abs(mfx - (x[i] + dx)) < 10 && Res.abs(mfy - (y[i] + dy)) < 10)
            {
                mpoint = i;
                if (allowAdminMove && lastpoint != -1 && lastpoint == mpoint)
                {
                    Service.gI().adminMove(mpoint);
                    lastpoint = -1;
                    mpoint = -1;
                }

                break;
            }
        }
    }

    public void perform(int idAction, object p)
    {
        switch (idAction)
        {
            case 1000:
                if (Char.getMyChar().cHP <= 0 || Char.getMyChar().statusMe == 14)
                {
                    center = GameScr.gI().cmdDead;
                    Char.getMyChar().cHP = 0;
                }

                GameScr.gI().switchToMe();
                break;
            case 14004:
                Handle.GoMap(mpoint);
                break;
        }
    }
}
