public class LanguageScr : mScreen, IActionListener
{
    private int popupW, popupH, popupX, popupY;
    private int indexRow;
    private mScreen returnScreen;

    public override void switchToMe()
    {
        returnScreen = GameCanvas.currentScreen;
        GameScr.gH = GameCanvas.h;
        GameCanvas.loadBG(GameCanvas.typeBg == 2 ? 0 : TileMap.bgID);
        base.switchToMe();
        if (GameScr.instance != null) GameScr.instance = null;
        TileMap.bgID = (sbyte)(mSystem.getCurrentTimeMillis() % 9);
        if (TileMap.bgID == 5 || TileMap.bgID == 6) TileMap.bgID = 4;
        GameScr.loadCamera(true);
        GameScr.cmx = 100;
        popupW = 220; popupH = 160;
        popupX = GameCanvas.w / 2 - popupW / 2;
        popupY = GameCanvas.h / 2 - popupH / 2;
        if (GameCanvas.h <= 250) popupY -= 10;
        center = new Command(mResources.languageID == mResources.Lang_EN ? "Select" : "Chọn", this, 1000, null);
        right = new Command(mResources.languageID == mResources.Lang_EN ? "Back" : "Quay lại", this, 1001, null);
        indexRow = loadLanguageID() == mResources.Lang_EN ? 1 : 0;
        if (GameCanvas.isTouch && GameCanvas.w >= 320)
        {
            center.x = GameCanvas.w / 2 - 35;
            right.x = GameCanvas.w / 2 + 88;
            center.y = right.y = Paint.CmdY;
        }
    }

    public override void paint(mGraphics g)
    {
        bool en = mResources.languageID == mResources.Lang_EN;
        string title = en ? "Language" : "Ngôn ngữ";
        g.setColor(0); g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
        GameCanvas.paintBGGameScr(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        g.setColor(Paint.COLORDARK);
        g.fillRoundRect(GameCanvas.hw - mFont.tahoma_8b.getWidth(title) / 2 - 12, popupY + 7, mFont.tahoma_8b.getWidth(title) + 22, 24, 6, 6);
        g.setColor(Paint.COLORLIGHT);
        g.drawRoundRect(GameCanvas.hw - mFont.tahoma_8b.getWidth(title) / 2 - 12, popupY + 7, mFont.tahoma_8b.getWidth(title) + 22, 24, 6, 6);
        mFont.tahoma_8b.drawString(g, title, GameCanvas.hw, popupY + 10, 2);
        string[] labels = { "Tiếng Việt", "English" };
        int y = popupY + 45;
        for (int i = 0; i < labels.Length; i++)
        {
            g.setColor(i == indexRow ? Paint.COLORLIGHT : Paint.COLORDARK);
            g.fillRect(popupX + 15, y + i * 36, popupW - 30, 30);
            g.setColor(i == indexRow ? 11053224 : 5720393);
            g.drawRect(popupX + 15, y + i * 36, popupW - 30, 30);
            mFont.tahoma_7b_white.drawString(g, labels[i], GameCanvas.hw, y + i * 36 + 6, 2);
        }
        base.paint(g);
        GameCanvas.paintz.paintCmdBar(g, left, center, right);
    }

    public static void saveLanguageID(int languageID) { RMS.saveRMSInt("indLanguage", languageID); }
    public static int loadLanguageID() { return RMS.loadRMSInt("indLanguage"); }

    public override void update()
    {
        GameScr.cmx++;
        if (GameScr.cmx > GameCanvas.w * 3 + 100) GameScr.cmx = 100;
        base.update();
    }

    public override void updateKey()
    {
        if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(popupX + 10, popupY + 40, popupW - 20, 80))
        {
            if (GameCanvas.isPointerClick) indexRow = (GameCanvas.py - (popupY + 40)) / 36;
            if (indexRow >= 0 && indexRow < 2) perform(1000, null);
        }
        if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[24] || GameCanvas.keyPressedz[25])
        {
            indexRow = indexRow == 0 ? 1 : 0;
        }
        base.updateKey();
    }

    private void ReturnToPreviousScreen()
    {
        if (returnScreen == null || returnScreen is SplashScr) GameCanvas.selectsvScr.switchToMe();
        else returnScreen.switchToMe();
    }

    public void perform(int idAction, object p)
    {
        if (idAction == 1001)
        {
            ReturnToPreviousScreen();
            return;
        }
        if (idAction == 1000)
        {
            int lang = indexRow == 1 ? mResources.Lang_EN : mResources.Lang_VI;
            saveLanguageID(lang);
            mResources.loadLanguage(lang);
            GameMidlet.isWorldver = lang == mResources.Lang_EN;
            ReturnToPreviousScreen();
        }
    }
}
