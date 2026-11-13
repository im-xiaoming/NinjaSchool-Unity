using System;
using System.IO;
using System.Text;

namespace NsoServer.Net
{
    // Wire message: one command byte plus a payload using java.io.DataInput/OutputStream encoding (big-endian, modified UTF-8).
    public sealed class Message
    {
        public sbyte Command;
        public readonly JavaWriter Writer;
        public readonly JavaReader Reader;
        public Message(sbyte command)
        {
            Command = command;
            Writer = new JavaWriter();
        }

        /// <summary>The received payload (null for messages built to send); read by PlayerLog.</summary>
        public readonly byte[] Data;
        public Message(sbyte command, byte[] data)
        {
            Command = command;
            Data = data;
            Reader = new JavaReader(data);
        }

        public byte[] GetData() => Writer.ToArray();
    }

    public sealed class JavaWriter
    {
        private readonly MemoryStream ms = new MemoryStream();
        public byte[] ToArray() => ms.ToArray();
        public void WriteByte(int v) => ms.WriteByte((byte)v);
        public void WriteSByte(sbyte v) => ms.WriteByte((byte)v);
        public void WriteBoolean(bool v) => ms.WriteByte((byte)(v ? 1 : 0));
        public void WriteShort(int v)
        {
            ms.WriteByte((byte)(v >> 8));
            ms.WriteByte((byte)v);
        }

        public void WriteInt(int v)
        {
            ms.WriteByte((byte)(v >> 24));
            ms.WriteByte((byte)(v >> 16));
            ms.WriteByte((byte)(v >> 8));
            ms.WriteByte((byte)v);
        }

        public void WriteLong(long v)
        {
            WriteInt((int)(v >> 32));
            WriteInt((int)v);
        }

        public void Write(byte[] b) => ms.Write(b, 0, b.Length);
        public void WriteUTF(string s)
        {
            var buf = new MemoryStream();
            foreach (char c in s)
            {
                if (c >= 0x0001 && c <= 0x007F)
                    buf.WriteByte((byte)c);
                else if (c <= 0x07FF)
                {
                    buf.WriteByte((byte)(0xC0 | (c >> 6)));
                    buf.WriteByte((byte)(0x80 | (c & 0x3F)));
                }
                else
                {
                    buf.WriteByte((byte)(0xE0 | (c >> 12)));
                    buf.WriteByte((byte)(0x80 | ((c >> 6) & 0x3F)));
                    buf.WriteByte((byte)(0x80 | (c & 0x3F)));
                }
            }

            if (buf.Length > 65535)
                throw new IOException("UTF too long");
            WriteShort((int)buf.Length);
            buf.WriteTo(ms);
        }
    }

    public sealed class JavaReader
    {
        private readonly byte[] data;
        private int pos;
        public JavaReader(byte[] data)
        {
            this.data = data;
        }

        public int Remaining
        {
            get
            {
                return data.Length - pos;
            }
        }

        public byte[] ReadBytes(int count)
        {
            if (count < 0 || pos + count > data.Length)
                throw new EndOfStreamException();
            var result = new byte[count];
            Buffer.BlockCopy(data, pos, result, 0, count);
            pos += count;
            return result;
        }

        private int Next()
        {
            if (pos >= data.Length)
                throw new EndOfStreamException();
            return data[pos++];
        }

        public sbyte ReadSByte() => (sbyte)Next();
        public byte ReadByte() => (byte)Next();
        public bool ReadBoolean() => Next() != 0;
        public short ReadShort() => (short)((Next() << 8) | Next());
        public int ReadInt() => (Next() << 24) | (Next() << 16) | (Next() << 8) | Next();
        public long ReadLong() => ((long)ReadInt() << 32) | (uint)ReadInt();
        public string ReadUTF()
        {
            int len = (Next() << 8) | Next();
            var sb = new StringBuilder(len);
            int end = pos + len;
            if (end > data.Length)
                throw new EndOfStreamException();
            while (pos < end)
            {
                int a = data[pos++];
                if (a < 0x80)
                    sb.Append((char)a);
                else if ((a >> 5) == 0x6)
                    sb.Append((char)(((a & 0x1F) << 6) | (data[pos++] & 0x3F)));
                else
                {
                    int b = data[pos++], c = data[pos++];
                    sb.Append((char)(((a & 0x0F) << 12) | ((b & 0x3F) << 6) | (c & 0x3F)));
                }
            }

            return sb.ToString();
        }
    }
}
