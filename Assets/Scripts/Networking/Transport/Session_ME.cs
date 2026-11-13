using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

public class Session_ME : ISession
{
    public class Sender
    {
        private readonly ConcurrentQueue<Message> sendingMessage = new ConcurrentQueue<Message>();
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        public void AddMessage(Message message)
        {
            sendingMessage.Enqueue(message);
            wake.Set();
        }

        public void NotifyKeyReady()
        {
            wake.Set();
        }

        public void Stop()
        {
            wake.Set();
        }

        public void run()
        {
            while (connected)
            {
                wake.WaitOne();
                if (!connected)
                    break;
                if (!getKeyComplete)
                    continue;
                Message message;
                while (connected && getKeyComplete && sendingMessage.TryDequeue(out message))
                    doSendMessage(message);
            }
        }
    }

    private class MessageCollector
    {
        public void run()
        {
            try
            {
                while (connected)
                {
                    Message message = readMessage();
                    if (message == null)
                    {
                        break;
                    }

                    try
                    {
                        if (message.command == -27)
                        {
                            getKey(message);
                        }
                        else
                        {
                            onRecieveMsg(message);
                        }
                    }
                    catch (Exception)
                    {
                    }
                //try
                //{
                //    Thread.Sleep(5);
                //}
                //catch (Exception)
                //{
                //}
                }
            }
            catch (Exception ex3)
            {
                mSystem.println("err run session_me:" + ex3.ToString());
            }

            if (!connected)
            {
                return;
            }

            if (messageHandler != null)
            {
                if (currentTimeMillis() - timeConnected > 500)
                {
                    messageHandler.onDisconnected();
                }
                else
                {
                    messageHandler.onConnectionFail();
                }
            }

            if (dataStream != null)
            {
                cleanNetwork();
            }
        }

        private void getKey(Message message)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sbyte b = message.reader().readSByte();
                key = new sbyte[b];
                for (int i = 0; i < b; i++)
                {
                    key[i] = message.reader().readSByte();
                //Debug.Log("Key " + i + " ] " + key[i]);
                }

                for (int j = 0; j < key.Length - 1; j++)
                {
                    ref sbyte reference = ref key[j + 1];
                    reference = (sbyte)(reference ^ key[j]);
                }

                getKeyComplete = true;
                sender.NotifyKeyReady();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private Message readMessage()
        {
            int num = 200;
            try
            {
                sbyte b = dis.ReadSByte();
                num = b;
                if (getKeyComplete)
                {
                    b = readKey(b);
                }

                int num2;
                if (b == -32)
                {
                    b = dis.ReadSByte();
                    if (getKeyComplete)
                    {
                        b = readKey(b);
                    }

                    sbyte b2 = readKey(dis.ReadSByte());
                    sbyte b3 = readKey(dis.ReadSByte());
                    sbyte b4 = readKey(dis.ReadSByte());
                    sbyte b5 = readKey(dis.ReadSByte());
                    num2 = ((b2 & 0xFF) << 24) | ((b3 & 0xFF) << 16) | ((b4 & 0xFF) << 8) | (b5 & 0xFF);
                }
                else if (getKeyComplete)
                {
                    sbyte b6 = dis.ReadSByte();
                    sbyte b7 = dis.ReadSByte();
                    num2 = ((readKey(b6) & 0xFF) << 8) | (readKey(b7) & 0xFF);
                }
                else
                {
                    sbyte b8 = dis.ReadSByte();
                    sbyte b9 = dis.ReadSByte();
                    num2 = (b8 & 0xFF00) | (b9 & 0xFF);
                }

                sbyte[] array = new sbyte[num2];
                int num3 = 0;
                int num4 = 0;
                byte[] src = dis.ReadBytes(num2);
                //Buffer.BlockCopy(src, 0, array, 0, num2);
                array = ArrayCast.cast(src);
                recvByteCount += 5 + num2;
                int num5 = recvByteCount + sendByteCount;
                strRecvByteCount = num5 / 1024 + "." + num5 % 1024 / 102 + "Kb";
                if (getKeyComplete)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = readKey(array[i]);
                    }
                }

                return new Message(b, array);
            }
            catch (Exception ex)
            {
                if (!connected || ex is System.IO.EndOfStreamException || ex is System.IO.IOException || ex is ObjectDisposedException)
                {
                    Debug.Log("[Net] connection closed (" + ex.GetType().Name + ")"); // normal close, not an error
                }
                else
                {
                    Debug.LogError(num + "-----------------:" + ex.ToString());
                }
            }

            return null;
        }
    }

    protected static Session_ME instance = new Session_ME();
    private static Stream dataStream;
    private static BinaryReader dis;
    private static BinaryWriter dos;
    public static IMessageHandler messageHandler;
    public static volatile bool connected;
    public static volatile bool connecting;
    private static Sender sender = new Sender();
    public static Thread initThread;
    public static Thread collectorThread;
    public static Thread sendThread;
    public static int sendByteCount;
    public static int recvByteCount;
    private static bool getKeyComplete;
    public static sbyte[] key = null;
    private static sbyte curR;
    private static sbyte curW;
    private static int timeConnected;
    public static string strRecvByteCount = string.Empty;
    public static bool isCancel;
    public string host;
    public int port;
    public static bool autoLocin;
    public static MyVector recieveMsg = new MyVector();
    private static object lockAuto;
    public static sbyte serverLogin;
    public Session_ME()
    {
        lockAuto = new object ();
    }

    public static Session_ME gI()
    {
        return instance;
    }

    public bool isConnected()
    {
        return connected;
    }

    public void autoReconnect()
    {
        if (GameCanvas.currentScreen != GameCanvas.selectsvScr)
        {
            GameCanvas.gI().resetToLoginScr();
        }

        if (isCancel)
        {
            huyLock();
        }
        else
        {
            isCancel = true;
            new Thread(new ThreadStart(AutoLogin)).Start();
        }
    }

    public void AutoLogin()
    {
        while (isCancel && GameCanvas.currentScreen != GameScr.instance)
        {
            try
            {
                GameCanvas.startWaitDlgWithoutCancel();
                this.close();
                Thread.Sleep(5000);
                this.connect(this.host, this.port);
                GameMidlet.serverLogin = serverLogin;
                GameCanvas.startWaitDlg(mResources.LOGGING);
                Service.gI().login(SelectServerScr.uname, SelectServerScr.pass, "2.1.8");
                LockAuto(10000);
                GameCanvas.startWaitDlg(mResources.LOGGING);
                Service.gI().selectCharToPlay(SelectCharScr.nameSelect);
                LockAuto(5000);
            }
            catch (Exception var2)
            {
                Debug.LogError("Kết nối lại lỗi " + var2.Message);
            }
        }
    }

    public static void LockAuto(int mili = 10000)
    {
        lock (lockAuto)
        {
            Monitor.Wait(lockAuto, mili);
        }
    }

    public static void huyLock()
    {
        lock (lockAuto)
        {
            Monitor.PulseAll(lockAuto);
        }
    }

    public void setHandler(IMessageHandler msgHandler)
    {
        messageHandler = msgHandler;
    }

    public void connect(string host, int port)
    {
        if (!connected && !connecting)
        {
            this.host = host;
            this.port = port;
            getKeyComplete = false;
            initThread = new Thread(NetworkInit);
            initThread.Start();
            serverLogin = GameMidlet.serverLogin;
        }
    }

    private void NetworkInit()
    {
        connecting = true;
        connected = true;
        try
        {
            doConnect(host, port);
            messageHandler.onConnectOK();
        }
        catch (Exception ex)
        {
            if (messageHandler != null)
            {
                close();
                Debug.LogError(">>>>>>>>>>>:" + ex.ToString());
                messageHandler.onConnectionFail();
            }
        }
    }

    public void doConnect(string host, int port)
    {
        // the embedded server runs in this process: connect through its in-memory pipe (host / port are not used)
        NsoServer.GameServer server = ServerBootstrap.Server;
        if (server == null)
            throw new IOException("Embedded server is not running");
        dataStream = server.Connect();
        dis = new BinaryReader(dataStream, new UTF8Encoding());
        dos = new BinaryWriter(dataStream, new UTF8Encoding());
        new Thread(sender.run).Start();
        MessageCollector @object = new MessageCollector();
        collectorThread = new Thread(@object.run);
        collectorThread.Start();
        timeConnected = currentTimeMillis();
        connecting = false;
        doSendMessage(new Message(-27));
    }

    public void sendMessage(Message message)
    {
        sender.AddMessage(message);
    }

    private static void doSendMessage(Message m)
    {
        sbyte[] data = m.getData();
        try
        {
            if (getKeyComplete)
            {
                sbyte value = writeKey(m.command);
                dos.Write(value);
            }
            else
            {
                dos.Write(m.command);
            }

            if (data != null)
            {
                int num = data.Length;
                if (m.command == -31)
                {
                    dos.Write((short)num);
                }
                else if (getKeyComplete)
                {
                    int num2 = writeKey((sbyte)(num >> 8));
                    dos.Write((sbyte)num2);
                    int num3 = writeKey((sbyte)(num & 0xFF));
                    dos.Write((sbyte)num3);
                }
                else
                {
                    dos.Write((ushort)num);
                }

                if (getKeyComplete)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        sbyte value2 = writeKey(data[i]);
                        dos.Write(value2);
                    }
                }

                sendByteCount += 5 + data.Length;
            }
            else
            {
                if (getKeyComplete)
                {
                    int num4 = 0;
                    int num5 = writeKey((sbyte)(num4 >> 8));
                    dos.Write((sbyte)num5);
                    int num6 = writeKey((sbyte)(num4 & 0xFF));
                    dos.Write((sbyte)num6);
                }
                else
                {
                    dos.Write((ushort)0);
                }

                sendByteCount += 5;
            }

            dos.Flush();
        }
        catch (Exception ex)
        {
            mSystem.println("err doSendMessage:" + ex.StackTrace);
        }
    }

    public static sbyte readKey(sbyte b)
    {
        sbyte[] array = key;
        sbyte num = curR;
        curR = (sbyte)(num + 1);
        sbyte result = (sbyte)((array[num] & 0xFF) ^ (b & 0xFF));
        if (curR >= key.Length)
        {
            curR = (sbyte)(curR % (sbyte)key.Length);
        }

        return result;
    }

    public static sbyte writeKey(sbyte b)
    {
        sbyte[] array = key;
        sbyte num = curW;
        curW = (sbyte)(num + 1);
        sbyte result = (sbyte)((array[num] & 0xFF) ^ (b & 0xFF));
        if (curW >= key.Length)
        {
            curW = (sbyte)(curW % (sbyte)key.Length);
        }

        return result;
    }

    public static void onRecieveMsg(Message msg)
    {
        if (Thread.CurrentThread.Name == Main.mainThreadName)
        {
            messageHandler.onMessage(msg);
        }
        else
        {
            recieveMsg.addElement(msg);
        }
    }

    public static void update()
    {
        while (recieveMsg.size() > 0)
        {
            Message message = (Message)recieveMsg.elementAt(0);
            if (message == null)
            {
                recieveMsg.removeElementAt(0);
                break;
            }

            messageHandler.onMessage(message);
            recieveMsg.removeElementAt(0);
        }
    }

    public void close()
    {
        recieveMsg.removeAllElements();
        cleanNetwork();
    }

    private static void cleanNetwork()
    {
        key = null;
        curR = 0;
        curW = 0;
        try
        {
            connected = false;
            connecting = false;
            sender.Stop();
            if (dataStream != null)
            {
                dataStream.Close();
                dataStream = null;
            }

            if (dos != null)
            {
                dos.Close();
                dos = null;
            }

            if (dis != null)
            {
                dis.Close();
                dis = null;
            }

            sendThread = null;
            collectorThread = null;
        }
        catch (Exception)
        {
        }
    }

    public static int currentTimeMillis()
    {
        return Environment.TickCount;
    }

    public static byte convertSbyteToByte(sbyte var)
    {
        if (var > 0)
        {
            return (byte)var;
        }

        return (byte)(var + 256);
    }

    public static byte[] convertSbyteToByte(sbyte[] var)
    {
        byte[] array = new byte[var.Length];
        for (int i = 0; i < var.Length; i++)
        {
            if (var[i] > 0)
            {
                array[i] = (byte)var[i];
            }
            else
            {
                array[i] = (byte)(var[i] + 256);
            }
        }

        return array;
    }
}
