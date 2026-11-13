using System;
using System.IO;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class

    // Minimal PNG writer (RGBA, 8 bits, uncompressed "stored" deflate blocks). Used for pictures the server builds
    // itself, such as the treasure-map image (TaskMechanics.TreasureImage). No dependency on Unity or System.Drawing,
    // so it works on the server thread.
    public static class Png
    {
        public static byte[] Encode(int width, int height, byte[] rgba)
        {
            // raw scanlines: filter byte 0 + RGBA row
            int stride = width * 4 + 1;
            var raw = new byte[stride * height];
            for (int y = 0; y < height; y++)
                Buffer.BlockCopy(rgba, y * width * 4, raw, y * stride + 1, width * 4);
            using (var ms = new MemoryStream())
            {
                ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
                var ihdr = new byte[13];
                PutInt(ihdr, 0, width);
                PutInt(ihdr, 4, height);
                ihdr[8] = 8; // bit depth
                ihdr[9] = 6; // colour type RGBA
                Chunk(ms, "IHDR", ihdr);
                Chunk(ms, "IDAT", Zlib(raw));
                Chunk(ms, "IEND", new byte[0]);
                return ms.ToArray();
            }
        }

        // zlib stream made of stored (not compressed) deflate blocks of at most 65535 bytes
        private static byte[] Zlib(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78);
                ms.WriteByte(0x01);
                int pos = 0;
                do
                {
                    int len = Math.Min(65535, data.Length - pos);
                    bool last = pos + len >= data.Length;
                    ms.WriteByte((byte)(last ? 1 : 0));
                    ms.WriteByte((byte)len);
                    ms.WriteByte((byte)(len >> 8));
                    ms.WriteByte((byte)~len);
                    ms.WriteByte((byte)(~len >> 8));
                    ms.Write(data, pos, len);
                    pos += len;
                }
                while (pos < data.Length);
                uint a = 1, b = 0;
                foreach (byte x in data)
                {
                    a = (a + x) % 65521;
                    b = (b + a) % 65521;
                }

                uint adler = (b << 16) | a;
                var tail = new byte[4];
                PutInt(tail, 0, (int)adler);
                ms.Write(tail, 0, 4);
                return ms.ToArray();
            }
        }

        private static void Chunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4];
            PutInt(len, 0, data.Length);
            s.Write(len, 0, 4);
            var td = new byte[4 + data.Length];
            for (int i = 0; i < 4; i++)
                td[i] = (byte)type[i];
            Buffer.BlockCopy(data, 0, td, 4, data.Length);
            s.Write(td, 0, td.Length);
            var crc = new byte[4];
            PutInt(crc, 0, (int)Crc(td));
            s.Write(crc, 0, 4);
        }

        private static uint[] table;
        private static uint Crc(byte[] data)
        {
            if (table == null)
            {
                var t = new uint[256];
                for (uint n = 0; n < 256; n++)
                {
                    uint c = n;
                    for (int k = 0; k < 8; k++)
                        c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                    t[n] = c;
                }

                table = t;
            }

            uint crc = 0xFFFFFFFFu;
            foreach (byte x in data)
                crc = table[(crc ^ x) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        private static void PutInt(byte[] b, int o, int v)
        {
            b[o] = (byte)(v >> 24);
            b[o + 1] = (byte)(v >> 16);
            b[o + 2] = (byte)(v >> 8);
            b[o + 3] = (byte)v;
        }
    }
}
