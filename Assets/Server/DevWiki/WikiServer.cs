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
    public sealed class WikiServer
    {
        public const int PortTries = 10;
        private const int MaxRequestBytes = 8192;

        private readonly string folder;
        private readonly Func<string> buildDb;
        private readonly string exportPath;   // optional copy of db.json on disk (for grep / offline lookup), null = none
        private readonly int firstPort;
        private readonly object buildLock = new object();
        private TcpListener listener;
        private Thread thread;
        private volatile bool running;
        private string db;
        private System.Collections.Generic.Dictionary<string, object> dbParsed;   // db parsed once for the API

        public Action<string> Logger = _ => { };
        public string Url { get; private set; }

        public WikiServer(string folder, int port, Func<string> buildDb, string exportPath)
        {
            this.folder = folder;
            firstPort = port;
            this.buildDb = buildDb;
            this.exportPath = exportPath;
        }

        public void Start()
        {
            for (int p = firstPort; p < firstPort + PortTries; p++)
            {
                try
                {
                    var l = new TcpListener(IPAddress.Loopback, p);
                    l.Start();
                    listener = l;
                    Url = "http://127.0.0.1:" + p + "/";
                    break;
                }
                catch (SocketException) { }   // port in use: try the next one
            }
            if (listener == null) throw new IOException("Dev wiki: no free port in " + firstPort + ".." + (firstPort + PortTries - 1));
            running = true;
            thread = new Thread(AcceptLoop) { IsBackground = true, Name = "NsoDevWiki" };
            thread.Start();
            // build once in the background so the first page load is fast and the exported file exists
            ThreadPool.QueueUserWorkItem(_ => { try { Db(true); } catch (Exception ex) { Logger("Dev wiki build error: " + ex.Message); } });
        }

        public void Stop()
        {
            running = false;
            try { if (listener != null) listener.Stop(); } catch { }
            listener = null;
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
                try
                {
                    c.ReceiveTimeout = 5000;
                    var s = c.GetStream();
                    // read the request head (up to the blank line)
                    var buf = new byte[MaxRequestBytes];
                    int n = 0;
                    while (n < buf.Length)
                    {
                        int r = s.Read(buf, n, buf.Length - n);
                        if (r <= 0) break;
                        n += r;
                        if (Encoding.ASCII.GetString(buf, 0, n).Contains("\r\n\r\n")) break;
                    }
                    string head = Encoding.ASCII.GetString(buf, 0, n);
                    string[] first = head.Split('\n')[0].Trim().Split(' ');
                    if (first.Length < 2 || first[0] != "GET") { Send(s, 405, "text/plain", "Method not allowed"); return; }
                    string target = first[1], query = "";
                    int q = target.IndexOf('?');
                    if (q >= 0) { query = target.Substring(q + 1); target = target.Substring(0, q); }
                    switch (target)
                    {
                        case "/":
                        case "/index.html":
                            string page = Path.Combine(folder, "index.html");
                            if (File.Exists(page)) Send(s, 200, "text/html; charset=utf-8", File.ReadAllText(page, Encoding.UTF8));
                            else Send(s, 404, "text/plain; charset=utf-8", "index.html not found in " + folder);
                            break;
                        case "/db.json":
                            Send(s, 200, "application/json; charset=utf-8", Db(query.Contains("reload=1")));
                            break;
                        default:
                            if (target == "/api" || target.StartsWith("/api/", StringComparison.Ordinal))
                            {
                                if (query.Contains("reload=1")) Db(true);
                                var r = WikiApi.Handle(Parsed(), target, query);
                                Send(s, r.Key, "application/json; charset=utf-8", r.Value);
                            }
                            else Send(s, 404, "text/plain", "Not found");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger("Dev wiki request error: " + ex.Message);
                }
            }
        }

        private static void Send(Stream s, int status, string type, string body)
        {
            byte[] data = Encoding.UTF8.GetBytes(body);
            string reason = status == 200 ? "OK" : status == 404 ? "Not Found" : status == 400 ? "Bad Request" : status == 405 ? "Method Not Allowed" : "Error";
            string head = "HTTP/1.1 " + status + " " + reason + "\r\nContent-Type: " + type + "\r\nContent-Length: " + data.Length +
                          "\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n";
            byte[] h = Encoding.ASCII.GetBytes(head);
            s.Write(h, 0, h.Length);
            s.Write(data, 0, data.Length);
            s.Flush();
        }
    }
}
