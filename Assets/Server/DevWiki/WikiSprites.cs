using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NsoServer.Data;
using NsoServer.World;

namespace NsoServer.DevWiki
{
    // Read-only game art for the local wiki. No Unity APIs, extraction or generated bitmap assets.
    // Routes accept a template ID only; callers cannot supply file paths.
    public sealed class WikiSprites
    {
        public sealed class Asset
        {
            public byte[] Data;
            public string ContentType;
        }

        private const int CacheBytes = 16 * 1024 * 1024;
        private readonly GameWorld world;
        private readonly Dictionary<int, int> itemIcons = new Dictionary<int, int>();
        private readonly HashSet<int> effectIcons = new HashSet<int>();
        private readonly object sync = new object ();
        private readonly Dictionary<string, Asset> cache = new Dictionary<string, Asset>();
        private readonly Queue<string> age = new Queue<string>();
        private int cachedBytes;
        public WikiSprites(GameWorld world, SqlTable items, SqlTable effects, SqlTable animations)
        {
            this.world = world;
            int id = items.Col("id"), icon = items.Col("icon");
            foreach (var row in items.Rows)
                itemIcons[int.Parse(row[id])] = int.Parse(row[icon]);
            foreach (var row in effects.Rows)
                effectIcons.Add(int.Parse(row[effects.Col("icon")]));
            // GameScr.readEfect / ServerEffect.paint: animation IDs are separate from status IDs.
            foreach (var row in animations.Rows)
                foreach (var entry in (List<object>)Json.Parse(row[animations.Col("info")]))
                    effectIcons.Add(Convert.ToInt32(((Dictionary<string, object>)entry)["imgId"]));
        }

        public Asset Read(string path)
        {
            string[] parts = path.Split('/');
            if (parts.Length != 4 || parts[1] != "sprites")
                return null;
            bool item = parts[2] == "items";
            bool effect = parts[2] == "effects";
            if (!item && !effect && parts[2] != "mobs")
                return null;
            string suffix = item ? ".png" : ".svg", name = parts[3];
            if (!name.EndsWith(suffix, StringComparison.Ordinal))
                return null;
            string raw = name.Substring(0, name.Length - suffix.Length);
            int id;
            if (!int.TryParse(raw, out id) || id < 0 || id > short.MaxValue || id.ToString(System.Globalization.CultureInfo.InvariantCulture) != raw)
                return null;
            lock (sync)
            {
                Asset asset;
                if (cache.TryGetValue(path, out asset))
                    return asset;
                try
                {
                    asset = effect ? EffectIcon(id) : item ? Item(id) : Monster(id);
                }
                catch (IOException)
                {
                    return null;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }

                if (asset == null)
                    return null;
                if (asset.Data.Length <= CacheBytes)
                {
                    while (cachedBytes + asset.Data.Length > CacheBytes && age.Count > 0)
                    {
                        string key = age.Dequeue();
                        cachedBytes -= cache[key].Data.Length;
                        cache.Remove(key);
                    }

                    cache[path] = asset;
                    age.Enqueue(path);
                    cachedBytes += asset.Data.Length;
                }

                return asset;
            }
        }

        private byte[] Png(string relative, out int zoom)
        {
            // Use the highest available original zoom, never request or copy new assets.
            for (zoom = 4; zoom >= 1; zoom--)
            {
                string path = Path.Combine(world.DataDirectory, "Img", relative.Replace("{zoom}", zoom.ToString()));
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }

            return null;
        }

        private Asset Item(int id)
        {
            int icon, zoom;
            if (!itemIcons.TryGetValue(id, out icon) || icon < 0)
                return null;
            byte[] png = Png("Small/{zoom}/Small" + icon + ".png", out zoom);
            return png == null ? null : new Asset
            {
                Data = png,
                ContentType = "image/png"
            };
        }

        private Asset EffectIcon(int id)
        {
            if (!effectIcons.Contains(id))
                return null;
            int zoom;
            byte[] png = Png("Small/{zoom}/Small" + id + ".png", out zoom);
            if (png == null || png.Length < 24)
                return null;
            int width = BigInt(png, 16), height = BigInt(png, 20);
            // Normalize image dimensions to logical pixels, including lower-zoom fallbacks.
            string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + (width / zoom) + "\" height=\"" + (height / zoom) + "\" viewBox=\"0 0 " + width + " " + height + "\"><image width=\"" + width + "\" height=\"" + height + "\" href=\"data:image/png;base64," + Convert.ToBase64String(png) + "\"/></svg>";
            return new Asset
            {
                Data = Encoding.UTF8.GetBytes(svg),
                ContentType = "image/svg+xml"
            };
        }

        private Asset Monster(int id)
        {
            var mob = world.FindMob(id);
            if (mob == null)
                return null;
            int zoom;
            // Controller.SendMobTemplate uses single files for the two tower objects.
            string file = id == 98 || id == 99 ? id + ".png" : id + "_0.png";
            byte[] png = Png("Mob/{zoom}/" + file, out zoom);
            if (png == null || png.Length < 24 || png[0] != 137 || png[1] != 80)
                return null;
            int width = BigInt(png, 16), height = BigInt(png, 20);
            if (width <= 0 || height <= 0 || width > 8192 || height > 8192)
                return null;
            string data = "data:image/png;base64," + Convert.ToBase64String(png);
            var body = new StringBuilder();
            int minX = 0, minY = 0, maxX = width, maxY = height;
            if (!mob.IsBoss)
                body.Append("<image width=\"").Append(width).Append("\" height=\"").Append(height).Append("\" href=\"").Append(data).Append("\"/>");
            else
            {
                // Mob.paint / paintBottomMobNew / paintTopMobNew use frame pieces from imgs[0].
                if (mob.Frames == null || mob.Frames.Length == 0 || mob.ImageInfo == null)
                    return null;
                int index = 0;
                if (mob.FrameBossMove != null && mob.FrameBossMove.Length > 0 && mob.FrameBossMove[0] >= 0 && mob.FrameBossMove[0] < mob.Frames.Length)
                    index = mob.FrameBossMove[0];
                var frame = mob.Frames[index];
                minX = minY = int.MaxValue;
                maxX = maxY = int.MinValue;
                bool modern = id >= 236 && id != 259;
                int[] layers = modern ? new[]
                {
                    1,
                    0
                }

                : new[]
                {
                    -1
                };
                // Embed the original sheet once. SVG remains self-contained when used in an HTML img.
                body.Append("<defs><image id=\"sheet\" width=\"").Append(width).Append("\" height=\"").Append(height).Append("\" href=\"").Append(data).Append("\"/></defs>");
                foreach (int layer in layers)
                    for (int i = 0; i < frame.IdImg.Length; i++)
                    {
                        int imageIndex = frame.IdImg[i];
                        if (imageIndex < 0 || imageIndex >= mob.ImageInfo.Length)
                            continue;
                        bool onTop = frame.OnTop != null && i < frame.OnTop.Length && frame.OnTop[i] != 0;
                        if (modern && (layer == 1) != onTop)
                            continue;
                        var info = mob.ImageInfo[imageIndex];
                        int sx = info.X * zoom, sy = info.Y * zoom;
                        int w = System.Math.Min(info.W * zoom, width - sx), h = System.Math.Min(info.H * zoom, height - sy);
                        if (sx < 0 || sy < 0 || w <= 0 || h <= 0)
                            continue;
                        int x = frame.Dx[i] * zoom, y = frame.Dy[i] * zoom;
                        minX = System.Math.Min(minX, x);
                        minY = System.Math.Min(minY, y);
                        maxX = System.Math.Max(maxX, x + w);
                        maxY = System.Math.Max(maxY, y + h);
                        bool flip = modern && frame.Flip != null && i < frame.Flip.Length && frame.Flip[i] == 1;
                        body.Append("<g transform=\"translate(").Append(x).Append(' ').Append(y).Append(")\">");
                        if (flip)
                            body.Append("<g transform=\"translate(").Append(w).Append(" 0) scale(-1 1)\">");
                        body.Append("<svg width=\"").Append(w).Append("\" height=\"").Append(h).Append("\" viewBox=\"").Append(sx).Append(' ').Append(sy).Append(' ').Append(w).Append(' ').Append(h).Append("\" overflow=\"hidden\"><use href=\"#sheet\"/></svg>");
                        if (flip)
                            body.Append("</g>");
                        body.Append("</g>");
                    }

                if (minX == int.MaxValue)
                    return null;
            }

            string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"" + minX + " " + minY + " " + (maxX - minX) + " " + (maxY - minY) + "\" style=\"image-rendering:pixelated\"><title>" + System.Security.SecurityElement.Escape(mob.Name) + "</title>" + body + "</svg>";
            return new Asset
            {
                Data = Encoding.UTF8.GetBytes(svg),
                ContentType = "image/svg+xml"
            };
        }

        private static int BigInt(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
