using System;
using System.IO;
using System.Net;
using UnityEngine;

public class GameMidlet
{
    public static sbyte CLIENT_TYPE = 4;

    public static sbyte indexClient;

    public static string IP = string.Empty;

    public static int PORT = 14444;

    public static sbyte userProvider;

    public static string clientAgent;

    public static bool isWorldver;

    public static sbyte serverLogin;

    public const string VERSION = "2.1.9";

    public static GameMidlet instance;

    public static int muzic = -1;

    public static bool isPlaySound;

    public static string inFoSMS;

    public static string latitude = string.Empty;

    public static string longitude = string.Empty;

    public static string linkDefault;

    public static string java = "Local:127.0.0.1:14444:0:0";

    public static string smartPhone = "Local:127.0.0.1:14444:0:0";
    public static string[] nameServer;

    public static string[] ipList;

    public static short[] portList;

    public static sbyte[] serverLoginList;

    public static sbyte[] language = { 1 };

    public static sbyte[] serverST;

    public GameMidlet()
    {
        MotherCanvas.instance = new MotherCanvas();
        Session_ME.gI().setHandler(Controller.gI());
        instance = this;
        mFont.init();
        mScreen.ITEM_HEIGHT = mFont.tahoma_8b.getHeight() + 6;
        clientAgent = "0";
        if (Main.isPC)
        {
            userProvider = 1;
        }
        else
        {
            userProvider = 1;
        }
        SplashScr.loadSplashScr();
        GameCanvas.currentScreen = new SplashScr();
        Key.mapKeyPC();
    }

    public void exit()
    {
        GameCanvas.bRun = false;
        Main.exit();
    }

    public void notifyDestroyed()
    {
        GameCanvas.endDlg();
        Main.exit();
    }

    public string readFileText(string fileName)
    {
        try
        {
            StringReader stringReader = null;
            TextAsset textAsset = (TextAsset)Resources.Load(Main.res + "/" + fileName, typeof(TextAsset));
            stringReader = new StringReader(textAsset.text);
            string text = stringReader.ReadLine();
            return text.ToString();
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    public void CheckPerGPS()
    {
        getLocation();
    }

    public void getLocation()
    {
        longitude = GPS.Longitude;
        latitude = GPS.Latitude;
        Service.gI().sendGPS();
    }

    public static void getServerList(string str)

    {
        string[] array = Res.split(str.Trim(), ",", 0);
        nameServer = new string[array.Length];
        ipList = new string[array.Length];
        portList = new short[array.Length];
        serverLoginList = new sbyte[array.Length];
        language = new sbyte[array.Length];
        serverST = new sbyte[array.Length];
        sbyte b = 0;
        sbyte b2 = 0;
        for (int i = 0; i < array.Length; i++)
        {
            string[] array2 = Res.split(array[i].Trim(), ":", 0);
            nameServer[i] = array2[0];
            ipList[i] = array2[1];
            portList[i] = short.Parse(array2[2]);
            serverLoginList[i] = sbyte.Parse(array2[3]);
            language[i] = sbyte.Parse(array2[4]);
            if (language[i] == mResources.Lang_VI)
            {
                serverST[i] = b;
                b++;
            }
            else if (language[i] == mResources.Lang_EN)
            {
                serverST[i] = b2;
                b2++;
            }
        }
        saveIP();
    }

    public static void saveIP()
    {
        DataOutputStream dataOutputStream = new DataOutputStream();
        try
        {
            dataOutputStream.writeByte(nameServer.Length);
            for (int i = 0; i < nameServer.Length; i++)
            {
                dataOutputStream.writeUTF(nameServer[i]);
                dataOutputStream.writeUTF(ipList[i]);
                dataOutputStream.writeShort(portList[i]);
                dataOutputStream.writeByte(serverLoginList[i]);
                dataOutputStream.writeByte(language[i]);
                dataOutputStream.writeByte(serverST[i]);
            }
            RMS.saveRMS("NJlink", dataOutputStream.toByteArray());
            dataOutputStream.close();
            SelectServerScr.loadIP();
        }
        catch (Exception)
        {
        }
    }

    // ip sv

    public static void getStrSv()
    {
        // The original reads an encoded list of online servers from the "Unnamed" resource; offline there is only this one.
        getServerList("Local:127.0.0.1:14444:0:0");

    }

    // public static void getStrSv()
    // {
    //     string tcp = GameTcp.readFileText2("fullippp");
    //     string iptcp = GameTcp.Hex2String(GameTcp.ReverseString(GameTcp.BinaryToString(tcp)), null);
    //     Debug.Log(iptcp);
    //     string empty = string.Empty;
    //     empty = connectHTTP(iptcp);
    //     getServerList(empty);

    // }

    public static void loadLinkRMS()
    {
        sbyte[] array = RMS.loadRMS("NJlink");
        if (array == null)
        {
            getStrSv();
            return;
        }
        DataInputStream dataInputStream = new DataInputStream(array);
        if (dataInputStream == null)
        {
            return;
        }
        try
        {
            sbyte b = dataInputStream.readByte();
            nameServer = new string[b];
            ipList = new string[b];
            portList = new short[b];
            serverLoginList = new sbyte[b];
            language = new sbyte[b];
            serverST = new sbyte[b];
            for (int i = 0; i < b; i++)
            {
                nameServer[i] = dataInputStream.readUTF();

                ipList[i] = dataInputStream.readUTF();
                portList[i] = dataInputStream.readShort();
                serverLoginList[i] = dataInputStream.readByte();
                language[i] = dataInputStream.readByte();
                serverST[i] = dataInputStream.readByte();
            }
            dataInputStream.close();
            SelectServerScr.loadIP();
        }
        catch (IOException)
        {
        }
    }

    public static int GetWorldIndex()
    {
        int result = 0;
        int num = mResources.Lang_VI;
        if (isWorldver)
        {
            num = mResources.Lang_EN;
        }
        for (int i = 0; i <= language.Length - 1; i++)
        {
            if (language[i] == num)
            {
                return i;
            }
        }
        return result;
    }

    public static int GetLastIndex()
    {
        int result = 0;
        for (int i = 0; i <= language.Length - 1; i++)
        {
            if (language[i] == mResources.Lang_EN)
            {
                return i - 1;
            }
        }
        return result;
    }
}
