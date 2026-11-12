using System;
using System.IO;
using System.Threading;

namespace NsoServer.Net
{
    // One connected client. Mirrors com.nsoz.network.Session: the first message from the client triggers a key exchange,
    // after which every byte (command, size, payload) is XORed with a rolling key in each direction.
    public sealed class Session
    {
        private const int MaxMessageSize = 1024 * 1024;
        private static readonly Random Rng = new Random();

        private readonly Stream stream;
        private readonly object sendLock = new object();
        private byte[] key;
        private int curR, curW;
        private volatile bool keyComplete;

        public readonly int Id;
        public int ZoomLevel = 1; // client art scale (1..4) from the client-info message, picks Img/*/<zoom> folders
        public readonly GameServer Server;
        public readonly Controller Controller;
        public volatile bool Connected = true;

        public Session(GameServer server, Stream stream, int id)
        {
            Server = server;
            Id = id;
            this.stream = stream;
            Controller = new Controller(this);
        }

        public void Start()
        {
            var t = new Thread(ReadLoop) { IsBackground = true, Name = "reader-" + Id };
            t.Start();
        }

        private void ReadLoop()
        {
            try
            {
                while (Connected)
                {
                    var m = ReadMessage();
                    if (m == null) break;
                    if (!keyComplete) SendKey();
                    else Controller.OnMessage(m);
                }
            }
            catch (Exception ex) { if (Connected) Server.Log("Client " + Id + " read error: " + ex); }
            Close();
        }

        private Message ReadMessage()
        {
            int c = stream.ReadByte();
            if (c < 0) return null;
            sbyte cmd = (sbyte)c;
            int size;
            if (keyComplete)
            {
                cmd = (sbyte)ReadKey((byte)cmd);
                int b1 = ReadRaw(), b2 = ReadRaw();
                size = (ReadKey((byte)b1) << 8) | ReadKey((byte)b2);
            }
            else
            {
                size = (ReadRaw() << 8) | ReadRaw();
            }
            if (size > MaxMessageSize) throw new IOException("Data too big");
            var data = new byte[size];
            int got = 0;
            while (got < size)
            {
                int n = stream.Read(data, got, size - got);
                if (n <= 0) return null;
                got += n;
            }
            if (keyComplete)
                for (int i = 0; i < data.Length; i++) data[i] = ReadKey(data[i]);
            return new Message(cmd, data);
        }

        private int ReadRaw()
        {
            int v = stream.ReadByte();
            if (v < 0) throw new EndOfStreamException();
            return v;
        }

        private byte ReadKey(byte b)
        {
            byte r = (byte)(key[curR] ^ b);
            if (++curR >= key.Length) curR = 0;
            return r;
        }

        private byte WriteKey(byte b)
        {
            byte r = (byte)(key[curW] ^ b);
            if (++curW >= key.Length) curW = 0;
            return r;
        }

        private void SendKey()
        {
            key = System.Text.Encoding.ASCII.GetBytes("nso_sony_" + (Server.Config.ServerId * Rng.Next(1, 10000)));
            var ms = new Message(Cmd.GET_SESSION_ID);
            ms.Writer.WriteByte(key.Length);
            ms.Writer.WriteByte(key[0]);
            for (int i = 1; i < key.Length; i++) ms.Writer.WriteByte(key[i] ^ key[i - 1]);
            var data = ms.GetData();
            lock (sendLock)
            {
                // plain framing for the key message only: cmd, ushort size, payload
                stream.WriteByte(unchecked((byte)Cmd.GET_SESSION_ID));
                stream.WriteByte((byte)(data.Length >> 8));
                stream.WriteByte((byte)data.Length);
                stream.Write(data, 0, data.Length);
                stream.Flush();
                keyComplete = true;
            }
        }

        public void Send(Message m)
        {
            if (!Connected || !keyComplete) return;
            try
            {
                var data = m.GetData();
                PlayerLog.Out(this, m.Command, data);   // offline debug log of every server reply
                int num = data.Length;
                lock (sendLock)
                {
                    var buf = new MemoryStream(num + 8);
                    bool full = num > short.MaxValue;
                    buf.WriteByte(WriteKey(unchecked((byte)(full ? Cmd.FULL_SIZE : m.Command))));
                    if (full)
                    {
                        buf.WriteByte(WriteKey(unchecked((byte)m.Command)));
                        buf.WriteByte(WriteKey((byte)(num >> 24)));
                        buf.WriteByte(WriteKey((byte)(num >> 16)));
                        buf.WriteByte(WriteKey((byte)(num >> 8)));
                        buf.WriteByte(WriteKey((byte)num));
                    }
                    else
                    {
                        buf.WriteByte(WriteKey((byte)(num >> 8)));
                        buf.WriteByte(WriteKey((byte)num));
                    }
                    for (int i = 0; i < num; i++) data[i] = WriteKey(data[i]);
                    buf.Write(data, 0, num);
                    buf.WriteTo(stream);
                    stream.Flush();
                }
            }
            catch (Exception) { Close(); }
        }

        public void Close()
        {
            if (!Connected) return;
            Connected = false;
            try { Controller.OnDisconnected(); } catch (Exception) { }
            try { stream.Close(); } catch (Exception) { }
            Server.Remove(this);
        }
    }
}
