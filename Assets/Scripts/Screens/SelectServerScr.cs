using Functions.Auto;

// Offline title menu. Continue loads the character list; New Game opens character creation.
public class SelectServerScr : mScreen, IActionListener
{
    private int menuSelection;
    public static string uname;
    public static string pass;
    public static string unameChange = string.Empty;
    public static string passChange = string.Empty;
    public static int ipSelect;

    public static void loadIP()
    {
        ipSelect = loadIndexServer();
        if (ipSelect < 0 || ipSelect > GameMidlet.nameServer.Length - 1) loadInfoIp(GameMidlet.GetWorldIndex());
        else loadInfoIp(ipSelect);
    }

    public static void loadInfoIp(int index)
    {
        if (Session_ME.gI().isConnected()) Session_ME.gI().close();
        ipSelect = index;
        saveIndexServer(ipSelect);
        GameMidlet.IP = LocalLogin.Host;
        GameMidlet.PORT = LocalLogin.Port;
        GameMidlet.serverLogin = 0;
        int lang = LanguageScr.loadLanguageID();
        mResources.loadLanguage(lang == mResources.Lang_EN ? mResources.Lang_EN : mResources.Lang_VI);
    }

    public override void switchToMe()
    {
        Handle.Stop();
        GameCanvas.isKiemduyet = RMS.loadRMSInt("isKiemduyet") == 0;
        GameScr.gH = GameCanvas.h;
        GameCanvas.loadBG(GameCanvas.typeBg == 2 ? 0 : TileMap.bgID);
        base.switchToMe();
        GameScr.instance = null;
        TileMap.bgID = (sbyte)(mSystem.currentTimeMillis() % 9);
        if (TileMap.bgID == 5 || TileMap.bgID == 6) TileMap.bgID = 4;
        GameScr.loadCamera(true);
        GameScr.cmx = 100;
        if (LocalLogin.imgTitle == null) LocalLogin.imgTitle = GameCanvas.loadImage("/logo");
        int lang = LanguageScr.loadLanguageID();
        mResources.loadLanguage(lang == mResources.Lang_EN ? mResources.Lang_EN : mResources.Lang_VI);
        menuSelection = 0;
        GameCanvas.menu.showMenu = false;
    }

    private const int MainMenuRows = 5;

    private int MainMenuRowHeight
    {
        get { return GameCanvas.h <= 208 ? 16 : (GameCanvas.h <= 250 ? 18 : 20); }
    }

    private int MainMenuRowStep
    {
        get { return GameCanvas.h <= 208 ? 20 : (GameCanvas.h <= 250 ? 24 : 29); }
    }

    private int MainMenuWidth
    {
        get { return System.Math.Min(GameCanvas.w - 24, GameCanvas.w <= 208 ? 82 : 115); }
    }

    private int MainMenuX
    {
        get { return (GameCanvas.w - MainMenuWidth) / 2; }
    }

    private int MainMenuY
    {
        get
        {
            int contentHeight = (MainMenuRows - 1) * MainMenuRowStep + MainMenuRowHeight;
            int y = GameCanvas.h * 41 / 100;
            return System.Math.Max(4, System.Math.Min(y, GameCanvas.h - contentHeight - 4));
        }
    }

    private int TitleLogoWidth
    {
        get { return System.Math.Min(81, GameCanvas.w - 24); }
    }

    // The logo art is only 108 x 62 pixels: draw it at the whole-number scale closest to TitleLogoWidth so it stays sharp.
    private int TitleLogoScale
    {
        get
        {
            int texW = LocalLogin.imgTitle != null && LocalLogin.imgTitle.texture != null ? LocalLogin.imgTitle.texture.width : 108;
            return System.Math.Max(1, (int)UnityEngine.Mathf.Round(TitleLogoWidth * mGraphics.zoomLevel / (float)texW));
        }
    }

    private int TitleLogoHeight
    {
        get { return TitleLogoWidth * 62 / 108; }
    }

    private int TitleLogoCenterY
    {
        get { return GameCanvas.h * (GameCanvas.h <= 208 ? 17 : (GameCanvas.h <= 250 ? 22 : 26)) / 100; }
    }
    private MyVector BuildMainMenu()
    {
        bool en = mResources.languageID == mResources.Lang_EN;
        var items = new MyVector();
        items.addElement(new Command(en ? "New Game" : "Ch\u01A1i m\u1EDBi", this, 1000, null));
        items.addElement(new Command(en ? "Continue" : "Ti\u1EBFp t\u1EE5c", this, 1001, null));
        items.addElement(new Command(en ? "Language" : "Ng\u00F4n ng\u1EEF", this, 1002, null));
        items.addElement(new Command(en ? "Settings" : "C\u00E0i \u0111\u1EB7t", this, 1003, null));
        items.addElement(new Command(en ? "Exit" : "Tho\u00E1t", this, 1004, null));
        return items;
    }

    private void ActivateMenuRow(int row)
    {
        if (row < 0 || row >= MainMenuRows) return;
        menuSelection = row;
        perform(1000 + row, null);
    }
    public void perform(int idAction, object p)
    {
        switch (idAction)
        {
            case 1000:
                LocalLogin.NextStartMode = LocalLogin.StartMode.NewGame;
                LocalLogin.Begin();
                break;
            case 1001:
                LocalLogin.NextStartMode = LocalLogin.StartMode.Continue;
                LocalLogin.Begin();
                break;
            case 1002:
                GameCanvas.languageScr.switchToMe();
                break;
            case 1003:
                SettingsMenu.gI().perform(1006, null);
                break;
            case 1004:
                GameMidlet.instance.exit();
                break;
        }
    }

    public override void paint(mGraphics g)
    {
        g.setColor(0);
        g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
        GameCanvas.paintBGGameScr(g);
        if (LocalLogin.imgTitle != null)
            g.drawImagePixelScale(LocalLogin.imgTitle, GameCanvas.hw, TitleLogoCenterY, TitleLogoScale);
        base.paint(g);
        MyVector items = BuildMainMenu();
        int x = MainMenuX;
        int y = MainMenuY;
        int rowHeight = MainMenuRowHeight;
        int rowStep = MainMenuRowStep;
        int width = MainMenuWidth;
        for (int i = 0; i < MainMenuRows; i++)
        {
            int rowY = y + i * rowStep;
            g.setColor(Paint.COLORDARK);
            g.fillRect(x, rowY, width, rowHeight);
            GameCanvas.paintz.paintFrameBorder(x, rowY, width, rowHeight, g);
            if (i == menuSelection)
            {
                g.setColor(Paint.COLORLIGHT);
                g.fillRect(x, rowY, width, rowHeight);
                GameCanvas.paintz.paintFrameBorder(x, rowY, width, rowHeight, g);
            }
            // caption centred on the row by its drawn pixels (tune: F6 > Title menu rows)
            TextCalib.UseBox("titleMenu", GameCanvas.hw, rowY + rowHeight / 2f);
            mFont.tahoma_7b_white.drawString(g, ((Command)items.elementAt(i)).caption, 0, 0, 2);
            TextCalib.Clear();
        }
    }
    public override void update()
    {
        GameScr.cmx++;
        if (GameScr.cmx > GameCanvas.w * 3 + 100) GameScr.cmx = 100;
        base.update();
    }

    public override void updateKey()
    {
        if (GameCanvas.keyPressedz[2] || GameCanvas.keyPressedz[21])
        {
            menuSelection = (menuSelection + MainMenuRows - 1) % MainMenuRows;
            MenuAudio.Tick();   // selection moved
        }
        else if (GameCanvas.keyPressedz[8] || GameCanvas.keyPressedz[22])
        {
            menuSelection = (menuSelection + 1) % MainMenuRows;
            MenuAudio.Tick();
        }
        else if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[12])
        {
            MenuAudio.Click();   // row chosen
            ActivateMenuRow(menuSelection);
        }
        else if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick)
        {
            int relativeY = GameCanvas.py - MainMenuY;
            int rowStep = MainMenuRowStep;
            int offsetInRow = relativeY % rowStep;
            if (GameCanvas.px >= MainMenuX && GameCanvas.px < MainMenuX + MainMenuWidth && relativeY >= 0 && relativeY < (MainMenuRows - 1) * rowStep + MainMenuRowHeight && offsetInRow < MainMenuRowHeight)
            {
                int row = relativeY / rowStep;
                GameCanvas.isPointerJustRelease = false;
                MenuAudio.Click();
                ActivateMenuRow(row);
            }
        }
        GameCanvas.clearKeyPressed();
    }
    public static void saveIndexServer(int index) { RMS.saveRMSInt("indServer", index); }
    public static int loadIndexServer() { return RMS.loadRMSInt("indServer"); }

    public static string randomNumberlist()
    {
        string text = string.Empty;
        for (int i = 0; i < 12; i++) text += Res.random(0, 9).ToString();
        return text;
    }
}