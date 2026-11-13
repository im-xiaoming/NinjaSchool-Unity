// The in-game "Config" menu (controls, screen size, sound, language). It used to live inside the login screen.
public class SettingsMenu : IActionListener
{
    private static SettingsMenu instance;
    public static SettingsMenu gI()
    {
        return (instance != null) ? instance : (instance = new SettingsMenu());
    }

    public void perform(int idAction, object p)
    {
        switch (idAction)
        {
            case 1006:
                doMenuConfig();
                break;
            case 10061:
                RMS.saveRMSInt("isSoftKey", 1);
                GameScr.isTouchKey = true;
                break;
            case 10062:
                RMS.saveRMSInt("isSoftKey", 2);
                GameScr.isTouchKey = false;
                break;
            case 1006190:
                RMS.saveRMSInt("mapKey", 1);
                GameCanvas.isChangMapKey = false;
                break;
            case 1006291:
                RMS.saveRMSInt("mapKey", 2);
                GameCanvas.isChangMapKey = true;
                break;
            case 1007:
                Sound.isSound = !Sound.isSound;
                if (Sound.isSound)
                {
                    RMS.saveRMSInt("isSound", 1);
                    GameMidlet.isPlaySound = true;
                    Sound.bMuzikDisable = false;
                }
                else
                {
                    RMS.saveRMSInt("isSound", 2);
                    Sound.stopAll();
                    Sound.stopAllBg();
                    GameMidlet.isPlaySound = false;
                }

                GameMidlet.isPlaySound = Sound.isSound;
                break;
            case 10071:
                AskChangeScreen();
                break;
            case 10072:
                doChangeScreen();
                break;
            case 1008:
                GameCanvas.languageScr.switchToMe();
                break;
            case 10081:
                GameCanvas.endDlg();
                LanguageScr.saveLanguageID(-1);
                GameMidlet.instance.notifyDestroyed();
                break;
            case 10082:
                LanguageScr.saveLanguageID(mResources.Lang_EN);
                RMS.clearRMS();
                GameCanvas.startOK(mResources.RESTART2, 8885, null);
                break;
        }
    }

    private void doMenuConfig()
    {
        MyVector myVector = new MyVector();
        if (!Main.isPC)
        {
            myVector.addElement(new Command(mResources.SOFT_KEY, this, 10061, null));
            myVector.addElement(new Command(mResources.ANALOG_KEY, this, 10062, null));
        }
        else
        {
            myVector.addElement(new Command(mResources.control1, this, 1006190, null));
            myVector.addElement(new Command(mResources.control2, this, 1006291, null));
            myVector.addElement(new Command((mGraphics.zoomLevel != 1) ? mResources.SCREEN1 : mResources.SCREEN2, this, 10071, null));
        }

        myVector.addElement(new Command(mResources.SOUND + (Sound.isSound ? mResources.OFF : mResources.ON), this, 1007, null));
        myVector.addElement(new Command(mResources.LANGUAGE, this, 1008, null));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void doChangeScreen()
    {
        if (mGraphics.zoomLevel != 1)
        {
            RMS.saveRMSInt("levelScreenKN", 1);
            UnityEngine.Screen.SetResolution(480, 320, false);
        }
        else
        {
            RMS.saveRMSInt("levelScreenKN", 0);
            UnityEngine.Screen.SetResolution(1024, 600, false);
        }

        GameMidlet.instance.exit();
    }

    private void AskChangeScreen()
    {
        GameCanvas.startYesNoDlg(mResources.Noti_ResizeScr + ((mGraphics.zoomLevel != 1) ? mResources.large : mResources.small), new Command(mResources.YES, this, 10072, null), new Command(mResources.NO, GameCanvas.gI(), 8882, null));
    }
}
