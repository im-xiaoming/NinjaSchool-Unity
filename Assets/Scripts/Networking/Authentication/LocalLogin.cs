// Offline build: the only account/server is local to the game.
public static class LocalLogin
{
    public enum StartMode
    {
        Continue,
        NewGame
    }

    public static StartMode NextStartMode = StartMode.Continue;
    public const string Host = "127.0.0.1";
    public const int Port = 14444;
    public const string Account = "player";
    public const string Password = "local";
    public static bool IsLoggingIn;
    public static Image imgTitle;
    public static void Begin()
    {
        if (Session_ME.gI().isConnected())
            Session_ME.gI().close();
        GameMidlet.IP = Host;
        GameMidlet.PORT = Port;
        GameMidlet.serverLogin = 0;
        SelectServerScr.uname = Account;
        SelectServerScr.pass = Password;
        SelectServerScr.unameChange = string.Empty;
        SelectServerScr.passChange = string.Empty;
        RMS.saveRMSString("acc", Account);
        RMS.saveRMSString("pass", Password);
        GameCanvas.startWaitDlgWithoutCancel();
        GameCanvas.connect(4);
        Service.gI().setClientType(); // the embedded server signs the local player in on its own
        IsLoggingIn = true;
    }
}
