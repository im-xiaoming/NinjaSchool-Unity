using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class
    using TileMap = NsoServer.World.TileMap;

    // Port of com.nsoz.map.TileMap.createImage(zoomLevel, x, y, w, h) + ImageMap: a piece of a map drawn from its tiles,
    // sent as PNG in the info of the treasure maps (DIA_DO2..4). Java reads one tile strip per tile set
    // ("Data/Img/Map/<zoom>/tile/<tileId>.png"); the client keeps the same tiles one file per tile
    // ("res/x<zoom>/t/tile<tileId>/<n>.png", n = id at zoom 1, id + 1 at higher zooms, TileMap.loadTileUse),
    // so those files are read here. Runs on the server thread: PNG decoding is done by hand (no Unity API).
    public static class TilePicture
    {
        /// <summary>Folder that holds x1..x4 (set by ServerBootstrap to Assets/Resources/res in the Editor).</summary>
        public static string TileRoot;

        private static readonly Dictionary<string, Rgba> cache = new Dictionary<string, Rgba>();

        private sealed class Rgba { public int W, H; public byte[] Px; }

        /// <summary>TileMap.createImage: the area (x, y, w, h in map pixels) at the given zoom, as PNG bytes.</summary>
        public static byte[] Create(GameWorld world, TileMap tile, int zoom, int x, int y, int w, int h)
        {
            zoom = Math.Max(1, Math.Min(4, zoom));
            var grid = world.GetGrid(tile);
            int size = 24 * zoom;
            int iw = w * zoom, ih = h * zoom, ox = x * zoom, oy = y * zoom;
            var outPx = new byte[iw * ih * 4];
            int i0 = Math.Max(0, ox / size), i1 = Math.Min(grid.Tmw - 1, (ox + iw) / size);
            int j0 = Math.Max(0, oy / size), j1 = Math.Min(grid.Tmh - 1, (oy + ih) / size);
            for (int i = i0; i <= i1; i++)
                for (int j = j0; j <= j1; j++)
                {
                    int id = grid.Maps[j * grid.Tmw + i] - 1;
                    if (id < 0) continue;
                    var t = LoadTile(tile.TileId, zoom, id);
                    if (t == null) continue;
                    Draw(t, outPx, iw, ih, i * size - ox, j * size - oy);
                }
            return Png.Encode(iw, ih, outPx);
        }

        // alpha-blended copy of a tile onto the picture
        private static void Draw(Rgba t, byte[] dst, int dw, int dh, int dx, int dy)
        {
            for (int ty = 0; ty < t.H; ty++)
            {
                int y = dy + ty;
                if (y < 0 || y >= dh) continue;
                for (int tx = 0; tx < t.W; tx++)
                {
                    int x = dx + tx;
                    if (x < 0 || x >= dw) continue;
                    int s = (ty * t.W + tx) * 4, d = (y * dw + x) * 4;
                    int a = t.Px[s + 3];
                    if (a == 0) continue;
                    for (int c = 0; c < 3; c++) dst[d + c] = (byte)((t.Px[s + c] * a + dst[d + c] * (255 - a)) / 255);
                    dst[d + 3] = (byte)Math.Max(dst[d + 3], a);
                }
            }
        }

        private static Rgba LoadTile(int tileId, int zoom, int id)
        {
            if (string.IsNullOrEmpty(TileRoot)) return null;
            string path = Path.Combine(TileRoot, "x" + zoom, "t", "tile" + tileId, (zoom == 1 ? id : id + 1) + ".png");
            lock (cache)
            {
                Rgba r;
                if (cache.TryGetValue(path, out r)) return r;
                try { r = File.Exists(path) ? Decode(File.ReadAllBytes(path)) : null; }
                catch (Exception) { r = null; }
                cache[path] = r;
                return r;
            }
        }

        // ---------------------------------------------------------------- PNG decoding (8-bit gray/RGB/palette/RGBA)

        private static Rgba Decode(byte[] png)
        {
            int pos = 8, w = 0, h = 0, depth = 0, color = 0;
            byte[] palette = null, trns = null;
            var idat = new MemoryStream();
            while (pos + 8 <= png.Length)
            {
                int len = (png[pos] << 24) | (png[pos + 1] << 16) | (png[pos + 2] << 8) | png[pos + 3];
                string type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
                int data = pos + 8;
                if (type == "IHDR")
                {
                    w = (png[data] << 24) | (png[data + 1] << 16) | (png[data + 2] << 8) | png[data + 3];
                    h = (png[data + 4] << 24) | (png[data + 5] << 16) | (png[data + 6] << 8) | png[data + 7];
                    depth = png[data + 8]; color = png[data + 9];
                    if (png[data + 12] != 0) return null;   // interlaced: not used by the game files
                }
                else if (type == "PLTE") { palette = new byte[len]; Buffer.BlockCopy(png, data, palette, 0, len); }
                else if (type == "tRNS") { trns = new byte[len]; Buffer.BlockCopy(png, data, trns, 0, len); }
                else if (type == "IDAT") idat.Write(png, data, len);
                else if (type == "IEND") break;
                pos = data + len + 4;
            }
            if (depth != 8 && color != 3) return null;
            int channels = color == 0 ? 1 : color == 2 ? 3 : color == 3 ? 1 : color == 4 ? 2 : 4;
            int bitsPerPixel = color == 3 ? depth : 8 * channels;
            int stride = (w * bitsPerPixel + 7) / 8, bpp = Math.Max(1, bitsPerPixel / 8);
            byte[] raw;
            idat.Position = 2;   // skip the zlib header
            using (var z = new DeflateStream(idat, CompressionMode.Decompress))
            using (var o = new MemoryStream()) { z.CopyTo(o); raw = o.ToArray(); }
            var cur = new byte[stride];
            var prev = new byte[stride];
            var px = new byte[w * h * 4];
            int rp = 0;
            for (int y = 0; y < h; y++)
            {
                int filter = raw[rp++];
                Buffer.BlockCopy(raw, rp, cur, 0, stride);
                rp += stride;
                for (int i = 0; i < stride; i++)
                {
                    int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                    switch (filter)
                    {
                        case 1: cur[i] = (byte)(cur[i] + a); break;
                        case 2: cur[i] = (byte)(cur[i] + b); break;
                        case 3: cur[i] = (byte)(cur[i] + ((a + b) >> 1)); break;
                        case 4:
                            int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                            cur[i] = (byte)(cur[i] + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c));
                            break;
                    }
                }
                for (int x = 0; x < w; x++)
                {
                    int d = (y * w + x) * 4;
                    switch (color)
                    {
                        case 6: Buffer.BlockCopy(cur, x * 4, px, d, 4); break;
                        case 2: px[d] = cur[x * 3]; px[d + 1] = cur[x * 3 + 1]; px[d + 2] = cur[x * 3 + 2]; px[d + 3] = 255; break;
                        case 0: px[d] = px[d + 1] = px[d + 2] = cur[x]; px[d + 3] = 255; break;
                        case 4: px[d] = px[d + 1] = px[d + 2] = cur[x * 2]; px[d + 3] = cur[x * 2 + 1]; break;
                        case 3:
                            int perByte = 8 / depth, shift = 8 - depth * (x % perByte + 1);
                            int idx = (cur[x / perByte] >> shift) & ((1 << depth) - 1);
                            if (palette != null && idx * 3 + 2 < palette.Length)
                            {
                                px[d] = palette[idx * 3]; px[d + 1] = palette[idx * 3 + 1]; px[d + 2] = palette[idx * 3 + 2];
                            }
                            px[d + 3] = trns != null && idx < trns.Length ? trns[idx] : (byte)255;
                            break;
                    }
                }
                var t = prev; prev = cur; cur = t;
            }
            return new Rgba { W = w, H = h, Px = px };
        }
    }
}
