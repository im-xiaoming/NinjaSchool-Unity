using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NsoServer.DevWiki
{
    // Tiny HTTP server for the dev wiki, started together with the game (ServerBootstrap) and listening on
    // 127.0.0.1 only (never reachable from other machines). Routes:
    //   /            index.html from the wiki folder (read from disk on every request, so edits show on refresh)
    //   /db.json     the database built by WikiData; /db.json?reload=1 rebuilds it (re-reads notes.json)
    //   /api/...     JSON API for scripts and agents (WikiApi.cs; GET /api lists the collections and the query syntax)
    // If the port is taken, the next PortTries ports are tried; Url tells which one was used.
    // Threading: Start() only creates the background thread "NsoDevWiki" (below-normal priority). Binding the port, building
    // the database and accepting connections all happen there; each request is answered on the thread pool. The game's
    // main thread never waits on the wiki. Debug only: ServerBootstrap starts it in the Editor / development builds.
    public sealed class WikiServer
    {
        public const int PortTries = 10;
        private const int MaxRequestBytes = 8192;

        private readonly string folder;
        private readonly Func<string> buildDb;
        private readonly string exportPath;   // optional copy of db.json on disk (for grep / offline lookup), null = none
        private readonly int firstPort;
        private readonly object buildLock = new object();
        private volatile TcpListener listener;
        private Thread thread;
        private volatile bool running;
        private string db;
        private System.Collections.Generic.Dictionary<string, object> dbParsed;   // db parsed once for the API

        public WikiControl Control; // optional: read-only wiki still works without a live game
        public WikiSprites Sprites; // optional: original game images, read-only and bounded-cache
        public Action<string> Logger = _ => { };
        public string Url { get; private set; }   // null until the wiki thread has bound a port
        public Action<string> Started = _ => { };  // called on the wiki thread once listening (argument: Url)

        public WikiServer(string folder, int port, Func<string> buildDb, string exportPath)
        {
            this.folder = folder;
            firstPort = port;
            this.buildDb = buildDb;
            this.exportPath = exportPath;
        }

        // returns at once; the wiki comes up on its own background thread
        public void Start()
        {
            running = true;
            thread = new Thread(Run) { IsBackground = true, Name = "NsoDevWiki", Priority = ThreadPriority.BelowNormal };
            thread.Start();
        }

        // wiki thread: bind a port, build the database once (first page load is fast, exported file exists), then accept
        private void Run()
        {
            try
            {
                for (int p = firstPort; p < firstPort + PortTries && listener == null; p++)
                {
                    try
                    {
                        var l = new TcpListener(IPAddress.Loopback, p);
                        l.Start();
                        listener = l;
                        Url = "http://127.0.0.1:" + p + "/";
                    }
                    catch (SocketException) { }   // port in use: try the next one
                }
                if (listener == null) { Logger("Dev wiki: no free port in " + firstPort + ".." + (firstPort + PortTries - 1)); return; }
                if (!running) { Stop(); return; }   // stopped while binding
                Started(Url);
                try { Db(true); } catch (Exception ex) { Logger("Dev wiki build error: " + ex.Message); }
                AcceptLoop();
            }
            catch (Exception ex) { Logger("Dev wiki stopped: " + ex.Message); }
        }

        public void Stop()
        {
            running = false;
            var l = listener;
            try { if (l != null) l.Stop(); } catch { }
        }

        // the database text; rebuilt on demand
        public string Db(bool rebuild)
        {
            lock (buildLock)
            {
                if (db == null || rebuild)
                {
                    db = buildDb();
                    dbParsed = null;
                    if (exportPath != null)
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
                            File.WriteAllText(exportPath, db, new UTF8Encoding(false));
                        }
                        catch (Exception ex) { Logger("Dev wiki export error: " + ex.Message); }
                    }
                }
                return db;
            }
        }

        // the database as objects (parsed on first API call after each build)
        private System.Collections.Generic.Dictionary<string, object> Parsed()
        {
            lock (buildLock)
            {
                if (dbParsed == null) dbParsed = (System.Collections.Generic.Dictionary<string, object>)NsoServer.Data.Json.Parse(Db(false));
                return dbParsed;
            }
        }

        private void AcceptLoop()
        {
            while (running)
            {
                TcpClient c;
                try { c = listener.AcceptTcpClient(); }
                catch { break; }   // listener stopped
                ThreadPool.QueueUserWorkItem(_ => Handle(c));
            }
        }

        private void Handle(TcpClient c)
        {
            using (c)
            {
                c.ReceiveTimeout = 5000; c.SendTimeout = 5000;
                var stream = c.GetStream();
                try
                {
                    var buffer = new byte[MaxRequestBytes];
                    int count = 0, end = -1;
                    while (count < buffer.Length && end < 0)
                    {
                        int read = stream.Read(buffer, count, System.Math.Min(1024, buffer.Length - count));
                        if (read <= 0) return;
                        count += read;
                        for (int i = 0; i + 3 < count; i++)
                            if (buffer[i] == 13 && buffer[i + 1] == 10 && buffer[i + 2] == 13 && buffer[i + 3] == 10) { end = i; break; }
                    }
                    if (end < 0) { Send(stream, 413, "application/json", "{\"error\":\"Request headers too large\"}"); return; }
                    string[] lines = Encoding.ASCII.GetString(buffer, 0, end).Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string[] first = lines[0].Split(' ');
                    if (first.Length != 3) throw new ArgumentException("Malformed request.");
                    var headers = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        int colon = lines[i].IndexOf(':');
                        if (colon <= 0) throw new ArgumentException("Malformed header.");
                        string name = lines[i].Substring(0, colon).Trim();
                        if (headers.ContainsKey(name)) throw new ArgumentException("Duplicate header.");
                        headers[name] = lines[i].Substring(colon + 1).Trim();
                    }
                    string authority = new Uri(Url).Authority, local = "localhost:" + new Uri(Url).Port;
                    string host;
                    if (!headers.TryGetValue("Host", out host) || (host != authority && host != local))
                    { Send(stream, 403, "application/json", "{\"error\":\"Local host required\"}"); return; }
                    string method = first[0], path = first[1], query = "";
                    int q = path.IndexOf('?');
                    if (q >= 0) { query = path.Substring(q + 1); path = path.Substring(0, q); }
                    if (method != "GET" && method != "POST")
                    { Send(stream, 405, "application/json", "{\"error\":\"Method not allowed\"}"); return; }
                    string body = "";
                    if (method == "POST")
                    {
                        string token, origin, contentType, length;
                        if ((path != "/control/rules" && path != "/control/combat" && path != "/control/dashboard") || Control == null)
                        { Send(stream, 404, "application/json", "{\"error\":\"Unknown write route\"}"); return; }
                        if (!headers.TryGetValue("X-Nso-Control", out token) || token != Control.Token ||
                            (headers.TryGetValue("Origin", out origin) && origin != "http://" + authority && origin != "http://" + local))
                        { Send(stream, 403, "application/json", "{\"error\":\"Refresh this local page before saving\"}"); return; }
                        int size;
                        if (headers.ContainsKey("Transfer-Encoding") || !headers.TryGetValue("Content-Length", out length) ||
                            !int.TryParse(length, out size) || size < 1 || size > MaxRequestBytes ||
                            !headers.TryGetValue("Content-Type", out contentType) || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                        { Send(stream, 400, "application/json", "{\"error\":\"Expected a JSON body of at most 8192 bytes\"}"); return; }
                        var bytes = new byte[size];
                        int have = System.Math.Min(size, count - end - 4);
                        Array.Copy(buffer, end + 4, bytes, 0, have);
                        while (have < size) { int n = stream.Read(bytes, have, size - have); if (n <= 0) throw new ArgumentException("Incomplete body."); have += n; }
                        body = new UTF8Encoding(false, true).GetString(bytes);
                    }
                    if (path.StartsWith("/control/", StringComparison.Ordinal))
                    {
                        var result = Control == null ? WikiControl.Reply(503, WikiControl.Obj("error", "Live controls require the game.")) : Control.Handle(method, path, query, body);
                        Send(stream, result.Key, "application/json; charset=utf-8", result.Value); return;
                    }
                    if (method != "GET") { Send(stream, 405, "application/json", "{\"error\":\"Read-only route\"}"); return; }
                    if (path.StartsWith("/sprites/", StringComparison.Ordinal))
                    {
                        var asset = Sprites == null ? null : Sprites.Read(path);
                        if (asset == null) Send(stream, 404, "text/plain", "Sprite unavailable");
                        else Send(stream, 200, asset.ContentType, asset.Data, "public, max-age=3600");
                        return;
                    }
                    if (path == "/" || path == "/index.html" || path == "/app.css" || path == "/app.js" || path == "/dashboard.js" || path == "/progression.js" || path == "/sprites.js" || path == "/effects.js" || path == "/effect-usage.json")
                    {
                        string name = path == "/" ? "index.html" : path.Substring(1);
                        string file = Path.Combine(folder, name);
                        string type = name.EndsWith(".json") ? "application/json" : name.EndsWith(".css") ? "text/css" : name.EndsWith(".js") ? "text/javascript" : "text/html";
                        if (File.Exists(file)) Send(stream, 200, type + "; charset=utf-8", File.ReadAllText(file, Encoding.UTF8));
                        else Send(stream, 404, "text/plain", "Asset not found");
                    }
                    else if (path == "/db.json") Send(stream, 200, "application/json; charset=utf-8", Db(query.Contains("reload=1")));
                    else if (path == "/api" || path.StartsWith("/api/", StringComparison.Ordinal))
                    {
                        if (query.Contains("reload=1")) Db(true);
                        var result = WikiApi.Handle(Parsed(), path, query);
                        Send(stream, result.Key, "application/json; charset=utf-8", result.Value);
                    }
                    else Send(stream, 404, "application/json", "{\"error\":\"Not found\"}");
                }
                catch (ArgumentException ex) { var r = WikiControl.Reply(400, WikiControl.Obj("error", ex.Message)); Send(stream, r.Key, "application/json", r.Value); }
                catch (Exception ex) { Logger("Dev wiki request error: " + ex.Message); }
            }
        }

        private static void Send(Stream s, int status, string type, string body)
        {
            Send(s, status, type, Encoding.UTF8.GetBytes(body), "no-store");
        }

        private static void Send(Stream s, int status, string type, byte[] data, string cacheControl)
        {
            string reason = status == 200 ? "OK" : status == 404 ? "Not Found" : status == 400 ? "Bad Request" : status == 405 ? "Method Not Allowed" : status == 403 ? "Forbidden" : status == 409 ? "Conflict" : status == 413 ? "Payload Too Large" : status == 503 ? "Service Unavailable" : "Error";
            string head = "HTTP/1.1 " + status + " " + reason + "\r\nContent-Type: " + type + "\r\nContent-Length: " + data.Length +
                          "\r\nCache-Control: " + cacheControl + "\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\nContent-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'none'; form-action 'self'\r\nConnection: close\r\n\r\n";
            byte[] h = Encoding.ASCII.GetBytes(head);
            s.Write(h, 0, h.Length);
            s.Write(data, 0, data.Length);
            s.Flush();
        }
    }
}
