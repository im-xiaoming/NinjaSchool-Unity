using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using NsoServer;
using NsoServer.Net;

// Headless client written after the Unity client's Session_ME / Service code (not after the server code),
// so it checks the real wire protocol: key exchange, keyed framing, login, character list, create character.
class TestClient
{
    private readonly TcpClient tcp = new TcpClient();
    private Stream s;
    private sbyte[] key;
    private int curR, curW;

    public static GameServer Srv; public TestClient(int port) { s = Srv.Connect(); }
    public void Close() { s.Dispose(); }

    private int Raw() { int v = s.ReadByte(); if (v < 0) throw new EndOfStreamException(); return v; }

    public void Handshake()
    {
        s.Write(new byte[] { 0xE5, 0, 0 }, 0, 3);           // Message(-27), empty payload, unkeyed
        s.Flush();
        sbyte cmd = (sbyte)Raw();
        if (cmd != -27) throw new Exception("expected -27 got " + cmd);
        int size = (Raw() << 8) | Raw();
        var d = new byte[size]; for (int i = 0; i < size; i++) d[i] = (byte)Raw();
        int n = (sbyte)d[0];
        key = new sbyte[n];
        for (int i = 0; i < n; i++) key[i] = (sbyte)d[1 + i];
        for (int j = 0; j < key.Length - 1; j++) key[j + 1] = (sbyte)(key[j + 1] ^ key[j]);
    }

    private sbyte RK(sbyte b) { sbyte r = (sbyte)((key[curR] & 0xFF) ^ (b & 0xFF)); if (++curR >= key.Length) curR = 0; return r; }
    private sbyte WK(sbyte b) { sbyte r = (sbyte)((key[curW] & 0xFF) ^ (b & 0xFF)); if (++curW >= key.Length) curW = 0; return r; }

    public void Send(Message m)
    {
        var data = m.GetData();
        s.WriteByte((byte)WK(m.Command));
        s.WriteByte((byte)WK((sbyte)(data.Length >> 8)));
        s.WriteByte((byte)WK((sbyte)(data.Length & 0xFF)));
        foreach (var b in data) s.WriteByte((byte)WK((sbyte)b));
        s.Flush();
    }

    public Message Receive()
    {
        sbyte b = RK((sbyte)Raw());
        int size;
        if (b == -32)
        {
            b = RK((sbyte)Raw());
            int b2 = RK((sbyte)Raw()) & 0xFF, b3 = RK((sbyte)Raw()) & 0xFF, b4 = RK((sbyte)Raw()) & 0xFF, b5 = RK((sbyte)Raw()) & 0xFF;
            size = (b2 << 24) | (b3 << 16) | (b4 << 8) | b5;
        }
        else
        {
            int h = (sbyte)Raw(), l = (sbyte)Raw();
            size = ((RK((sbyte)h) & 0xFF) << 8) | (RK((sbyte)l) & 0xFF);
        }
        var data = new byte[size];
        for (int i = 0; i < size; i++) data[i] = (byte)RK((sbyte)Raw());
        return new Message(b, data);
    }

    // true if another message arrives within the given time
    public bool HasMore(int ms)
    {
        int old = s.ReadTimeout;
        s.ReadTimeout = ms;
        try { Receive(); return true; }
        catch (IOException) { return false; }
        finally { s.ReadTimeout = old; }
    }

    public static Message NotLogin(sbyte sub) { var m = new Message(-29); m.Writer.WriteSByte(sub); return m; }
    public static Message NotMap(sbyte sub) { var m = new Message(-28); m.Writer.WriteSByte(sub); return m; }
}

static class Program
{
    static int failures;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what);
        if (!ok) failures++;
    }

    static string Dir()
    {
        var d = Path.Combine(Path.GetTempPath(), "nso_test_" + Guid.NewGuid().ToString("N"));
        return d;
    }

    static void Login(TestClient c, string user, string pass)
    {
        var info = TestClient.NotLogin(-125);
        info.Writer.WriteByte(0); info.Writer.WriteByte(1); info.Writer.WriteBoolean(false);
        info.Writer.WriteInt(800); info.Writer.WriteInt(600); info.Writer.WriteBoolean(false); info.Writer.WriteBoolean(true);
        info.Writer.WriteUTF("iphone platform xxx"); info.Writer.WriteInt(0); info.Writer.WriteByte(0);
        info.Writer.WriteByte(0); info.Writer.WriteInt(0); info.Writer.WriteUTF("agent");
        c.Send(info);
        var m = TestClient.NotLogin(-127);
        m.Writer.WriteUTF(user); m.Writer.WriteUTF(pass); m.Writer.WriteUTF("2.1.8");
        m.Writer.WriteUTF(""); m.Writer.WriteUTF(""); m.Writer.WriteUTF("rnd"); m.Writer.WriteByte(0);
        c.Send(m);
    }

    static int Main(string[] args)
    {
        if (args.Length > 1 && args[0] == "sql")
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tables = NsoServer.Data.SqlDump.Load(args[1]);
            foreach (var kv in tables) Console.WriteLine(kv.Key + ": " + kv.Value.Rows.Count + " rows, " + kv.Value.Columns.Count + " cols");
            Console.WriteLine("loaded in " + sw.ElapsedMilliseconds + " ms");
            return 0;
        }
        if (args.Length > 0 && args[0] == "grids")
        {
            string bk = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
            var w = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(bk, "tables.txt")), Path.Combine(bk, "Data"));
            int ok = 0, fail = 0;
            for (int id = 0; id < 400; id++)
            {
                var t = w.FindMap(id);
                if (t == null) continue;
                try { var g = w.GetGrid(t); if (g.Tmw > 0 && g.Tmh > 0) ok++; else fail++; }
                catch (Exception ex) { fail++; Console.WriteLine("map " + id + ": " + ex.Message); }
            }
            Console.WriteLine(ok + " grids loaded, " + fail + " failed");
            return fail;
        }
        // dev wiki without Unity: `wiki` builds and checks db.json, writes it to <project>/NsoWiki/db.json;
        // `wiki serve [port]` also serves the website until Enter is pressed
        if (args.Length > 0 && args[0] == "wiki")
        {
            string bk = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
            string wikiDir = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoWiki");
            string export = Path.GetFullPath(@"C:\My project\NsoWiki\db.json");
            var tables = NsoServer.Data.TableFile.Load(Path.Combine(bk, "tables.txt"));
            var w = new NsoServer.World.GameWorld(tables, Path.Combine(bk, "Data"));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var srv = new NsoServer.DevWiki.WikiServer(wikiDir, args.Length > 2 ? int.Parse(args[2]) : 8787,
                () => NsoServer.DevWiki.WikiData.Build(tables, w, Path.Combine(wikiDir, "notes.json")), export);
            srv.Logger = Console.WriteLine;
            string db = srv.Db(true);
            Console.WriteLine("db.json: " + db.Length + " chars in " + sw.ElapsedMilliseconds + " ms -> " + export);
            // the JSON must parse back, and every collection must be a non-empty array of documents with an id
            var root = (Dictionary<string, object>)NsoServer.Data.Json.Parse(db);
            int bad = 0;
            foreach (var kv in (Dictionary<string, object>)root["collections"])
            {
                var list = kv.Value as List<object>;
                int n = list == null ? -1 : list.Count, noId = 0;
                if (list != null) foreach (var d in list) if (!(d is Dictionary<string, object> dd) || !dd.ContainsKey("id")) noId++;
                bool ok = n > 0 && noId == 0;
                if (!ok) bad++;
                Console.WriteLine((ok ? "  ok   " : "  FAIL ") + kv.Key + ": " + n + " docs" + (noId > 0 ? ", " + noId + " without id" : ""));
            }
            if (args.Length > 1 && args[1] == "serve")
            {
                srv.Start();
                Console.WriteLine("Dev wiki: " + srv.Url + "  (Enter to stop)");
                if (Console.IsInputRedirected) System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);   // no console: run until killed
                Console.ReadLine();
                srv.Stop();
            }
            return bad;
        }
        if (args.Length > 5 && args[0] == "walkcheck") return WalkCheck(args[1], int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]));
        if (args.Length > 0 && args[0] == "combatcheck") return CombatCheck();
        if (args.Length > 0 && args[0] == "progresscheck") return ProgressCheck();
        if (args.Length > 0 && args[0] == "questcheck") return QuestCheck();
        if (args.Length > 1 && args[0] == "entercheck") return EnterCheck(args[1], args.Length > 2 ? int.Parse(args[2]) : 22);
        if (args.Length > 2 && args[0] == "exporttables")
        {
            var all = NsoServer.Data.SqlDump.Load(args[1], NsoServer.Data.TableFile.StaticTables);
            NsoServer.Data.TableFile.Save(args[2], all);
            var back = NsoServer.Data.TableFile.Load(args[2]);
            int bad = 0;
            foreach (var kv in all)
            {
                var b = back[kv.Key];
                if (b.Rows.Count != kv.Value.Rows.Count) bad++;
                else for (int i = 0; i < b.Rows.Count; i++)
                        for (int j = 0; j < b.Rows[i].Length; j++)
                            if (b.Rows[i][j] != kv.Value.Rows[i][j]) bad++;
                Console.WriteLine(kv.Key + ": " + b.Rows.Count + " rows");
            }
            Console.WriteLine(bad == 0 ? "round trip OK" : bad + " MISMATCHES");
            return bad;
        }
        string dir = Dir();
        var server = new GameServer(new ServerConfig { DataDirectory = dir });
        TestClient.Srv = server;
        string staticRoot = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
        bool zoneLimitVerified = false;
        for (int mapId = 0; mapId < 400 && !zoneLimitVerified; mapId++)
        {
            var map = server.World.FindMap(mapId);
            if (map == null || map.ZoneNumber <= 5) continue;
            var zones = server.World.GetZones(mapId);
            zoneLimitVerified = zones.Count == 5 && zones[4].Id == 4 && server.World.GetZone(mapId, 5) == null;
        }
        Check(zoneLimitVerified, "maps expose at most five zones with zero-based ids");        server.Logger = t => Console.WriteLine("  [server] " + t);
        server.Start();
        int port = 0;
        try
        {
            // 1. handshake + login (auto-registers) -> UPDATE_VERSION
            var c = new TestClient(port);
            c.Handshake();
            Check(true, "key exchange");
            Login(c, "player01", "secret");
            var r = c.Receive();
            Check(r.Command == -28 && r.Reader.ReadSByte() == -123, "login answered with NOT_MAP/UPDATE_VERSION");

            // 2. client ok -> empty character list
            c.Send(TestClient.NotMap(-101));
            r = c.Receive();
            bool isList = r.Command == -28 && r.Reader.ReadSByte() == -126;
            Check(isList && r.Reader.ReadByte() == 0, "character list is empty for a new account");

            // 3. create character -> list with 1 entry, correct fields
            var cr = TestClient.NotMap(-125);
            cr.Writer.WriteUTF("ninjatest"); cr.Writer.WriteByte(1); cr.Writer.WriteByte(23);
            c.Send(cr);
            r = c.Receive();
            r.Reader.ReadSByte();
            int count = r.Reader.ReadByte();
            sbyte gender = r.Reader.ReadSByte(); string name = r.Reader.ReadUTF(); string school = r.Reader.ReadUTF();
            int level = r.Reader.ReadByte(); short head = r.Reader.ReadShort();
            Check(count == 1 && gender == 1 && name == "ninjatest" && head == 23 && level == 1 && school == "Chưa vào trường",
                  "created character appears in list (" + name + ", head " + head + ", " + school + ")");

            // 4. duplicate name rejected via server dialog
            c.Send(cr);
            r = c.Receive();
            Check(r.Command == -26 && r.Reader.ReadUTF() == "Tên nhân vật đã tồn tại!", "duplicate name rejected");

            // 5. invalid head falls back to the default head for the gender
            var bad = TestClient.NotMap(-125);
            bad.Writer.WriteUTF("secondone"); bad.Writer.WriteByte(0); bad.Writer.WriteByte(99);
            c.Send(bad);
            r = c.Receive(); r.Reader.ReadSByte(); r.Reader.ReadByte();
            bool found = false;
            for (int i = 0; i < 2; i++)
            {
                r.Reader.ReadSByte(); string n = r.Reader.ReadUTF(); r.Reader.ReadUTF(); r.Reader.ReadByte();
                short h = r.Reader.ReadShort(); r.Reader.ReadShort(); r.Reader.ReadShort(); r.Reader.ReadShort();
                if (n == "secondone") found = h == 11;
            }
            Check(found, "invalid head replaced by gender default");

            // 6. second connection: wrong password rejected, right password sees saved characters
            var c2 = new TestClient(port);
            c2.Handshake();
            Login(c2, "player01", "wrong");
            r = c2.Receive();
            Check(r.Command == -26 && r.Reader.ReadUTF() == "Tài khoản hoặc mật khẩu không chính xác.", "wrong password rejected");
            var c3 = new TestClient(port);
            c3.Handshake();
            Login(c3, "player01", "secret");
            c3.Receive();
            c3.Send(TestClient.NotMap(-101));
            r = c3.Receive(); r.Reader.ReadSByte();
            Check(r.Reader.ReadByte() == 2, "saved characters returned on a new connection");

            // 7. data survives a server restart
            server.Stop();
            server = new GameServer(new ServerConfig { DataDirectory = dir }); TestClient.Srv = server;
            server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(staticRoot, "tables.txt")), Path.Combine(staticRoot, "Data"));
            server.Logger = t => Console.WriteLine("  [server] " + t);
            server.Start();
            var c4 = new TestClient(0);
            c4.Handshake();
            Login(c4, "player01", "secret");
            c4.Receive();
            c4.Send(TestClient.NotMap(-101));
            r = c4.Receive(); r.Reader.ReadSByte();
            Check(r.Reader.ReadByte() == 2, "characters persisted across restart");

            // 8. invalid account name
            var c5 = new TestClient(0);
            c5.Handshake();
            Login(c5, "bad name!", "x");
            r = c5.Receive();
            Check(r.Command == -26, "invalid username rejected");

            // 8b. the four static downloads equal the files captured from the original Java server
            string baked = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
            server.Static = new FileStaticData(baked);
            var c7 = new TestClient(0);
            c7.Handshake();
            Login(c7, "player03", "pw");
            r = c7.Receive();
            Check(r.Command == -28 && Same(Rest(r, -123), File.ReadAllBytes(Path.Combine(baked, "version.bin"))), "UPDATE_VERSION payload identical to captured file");
            string[] names = { "data", "map", "skill", "item" };
            sbyte[] subs = { -122, -121, -120, -119 };
            for (int i = 0; i < 4; i++)
            {
                c7.Send(TestClient.NotMap(subs[i]));
                r = c7.Receive();
                Check(r.Command == -28 && Same(Rest(r, subs[i]), File.ReadAllBytes(Path.Combine(baked, names[i] + ".bin"))), names[i] + " payload identical to captured file");
            }
            Check(CheckMobTemplate(c7, 0, false), "ordinary mob template includes its image and complete response");
            Check(CheckMobTemplate(c7, 114, true), "boss mob template includes images and animation/frame metadata");
            Check(CheckInventorySerialization(server.World), "ME_LOAD_ALL serializes persisted bag/equipment and round-trips inventory JSON");

            // 9. large payload uses FULL_SIZE framing
            server.Static = new BigData();
            var c6 = new TestClient(0);
            c6.Handshake();
            Login(c6, "player02", "pw");
            r = c6.Receive();
            c6.Send(TestClient.NotMap(-122));
            r = c6.Receive();
            Check(r.Command == -28 && r.Reader.ReadSByte() == -122, "large UPDATE_DATA (50 KB) received through FULL_SIZE framing");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION " + ex);
            failures++;
        }
        server.Stop();
        try { Directory.Delete(dir, true); } catch (Exception) { }
        Console.WriteLine(failures == 0 ? "ALL PASSED" : failures + " FAILED");
        return failures;
    }

    static byte[] Hex(string h)
    {
        var b = new byte[h.Length / 2];
        for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
        return b;
    }

    // Zeroes what legitimately differs between two runs: random monster class (sys), and the ids the database assigns.
    static byte[] Mask(sbyte cmd, byte[] d)
    {
        var c = (byte[])d.Clone();
        if (cmd == -30 && c.Length > 5 && (sbyte)c[0] == -127) { c[1] = c[2] = c[3] = c[4] = 0; }            // ME_LOAD_ALL player id
        if (cmd == 117 && c.Length == 16 && (sbyte)c[0] == -1) { c[2] = c[3] = c[4] = c[5] = 0; }             // bijuu: user id
        if (cmd == 1 && c.Length == 8) { c[0] = c[1] = c[2] = c[3] = 0; }                                      // move echo: player id
        if (cmd == -3 && c.Length == 13) { c[1] = c[2] = c[3] = c[4] = 0; }                                    // damage shown is random
        if (cmd == 88 && c.Length == 8) { c[0] = c[1] = c[2] = c[3] = 0; }                                     // RETURN_POINT_MAP: player id
        if (cmd == 0 && c.Length == 9) { c[0] = c[1] = c[2] = c[3] = 0; }                                      // PLAYER_DIE: player id
        if (cmd == -2 && c.Length == 17) { c[1] = c[2] = c[3] = c[4] = 0; }                                    // hit seen by all: player id
        if (cmd == -18)
        {
            // walk the layout: 4 header bytes, name, zone id, x, y, waypoints, then the monster list
            int p = 4;
            int nameLen = (d[p] << 8) | d[p + 1]; p += 2 + nameLen;
            p += 1 + 2 + 2;                 // zone id, x, y
            int wps = d[p++]; p += wps * 8;
            int mobs = d[p++];
            for (int i = 0; i < mobs; i++) { c[p + 7] = 0; p += 24; }
        }
        return c;
    }

    // Quest chain check: NV chào làng, NV kiến thức (quiz + wooden sword), NV lần đầu dùng kiếm (equip + training dummy),
    // NV diệt sên trừ cóc (shop, buy, eat).
    static int QuestCheck()
    {
        string baked = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var server = new GameServer(new ServerConfig { DataDirectory = Dir() });
        TestClient.Srv = server;
        server.Static = new FileStaticData(baked);
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(baked, "tables.txt")), Path.Combine(baked, "Data"));
        server.Start();
        int bad = 0;
        try
        {
            var c = new TestClient(0);
            c.Handshake();
            Login(c, "player01", "secret");
            c.Receive();
            c.Send(TestClient.NotMap(-101));
            c.Receive();
            var cr = TestClient.NotMap(-125);
            cr.Writer.WriteUTF("ninjatest"); cr.Writer.WriteByte(1); cr.Writer.WriteByte(2);
            c.Send(cr);
            c.Receive();
            foreach (var rec in server.Accounts.GetPlayers(1)) { rec.MapId = 22; rec.X = 1741; rec.Y = 264; }
            var sel = TestClient.NotMap(-126);
            sel.Writer.WriteUTF("ninjatest");
            c.Send(sel);
            for (int i = 0; i < 10; i++) c.Receive();
            NsoServer.Store.PlayerRecord me = null; foreach (var rec in server.Accounts.GetPlayers(1)) me = rec;

            // --- NV chào làng ---
            var names = OpenNpc(c, 12);
            Check(names.Count > 0 && names[0] == "Nhiệm vụ chào làng", "Tajima offers the first quest (got " + string.Join("|", names) + ")");
            TakeQuest(c, 12);
            int talked = 0;
            foreach (int npc in new[] { 3, 4, 6, 24, 5, 7 })
            {
                var n = OpenNpc(c, npc);
                int idx = n.IndexOf("Nói chuyện");
                if (idx < 0) { Console.WriteLine("  npc " + npc + " menu: " + string.Join("|", n)); continue; }
                ChooseMenu(c, npc, idx);
                if (ReceiveCmd(c, 48, 20) != null) talked++;
            }
            Check(talked == 6, "talking to the six villagers advances the quest (" + talked + "/6 TASK_NEXT)");
            names = OpenNpc(c, 12);
            Check(names.Count > 0 && names[0] == "Hoàn thành nhiệm vụ", "Tajima offers to finish the quest");
            ChooseMenu(c, 12, 0);
            Check(ReceiveCmd(c, 49, 30) != null, "quest 0 finished (TASK_FINISH)");
            Check(me.TaskId == 1 && me.Exp >= 200, "next quest id 1, reward exp " + me.Exp);

            // --- NV kiến thức: quiz at Tabemono ---
            OpenNpc(c, 12); TakeQuest(c, 12);
            OpenNpc(c, 4);
            ChooseMenu(c, 4, 0);   // the quest entry
            string[] right = { "Cấp 150", "Cấp 60", "Hồi phục thể lực", "Tất cả", "Tất cả" };   // the answers the original code accepts
            int answered = 0;
            for (int q = 0; q < 5; q++)
            {
                var qm = ReceiveCmd(c, 39, 20);
                if (qm == null) break;
                qm.Reader.ReadShort(); qm.Reader.ReadUTF();
                int count = qm.Reader.ReadByte(), pick = -1;
                for (int i = 0; i < count; i++) if (qm.Reader.ReadUTF() == right[q]) pick = i;
                var ans = new Message(47); ans.Writer.WriteByte(4); ans.Writer.WriteByte(pick); c.Send(ans);
                answered++;
            }
            int nexts = 0;
            for (int i = 0; i < 5; i++) if (ReceiveCmd(c, 48, 20) != null) nexts++;
            Check(answered == 5 && nexts == 5, "quiz answered and its five steps completed (" + nexts + ")");
            OpenNpc(c, 12); ChooseMenu(c, 12, 0);
            Check(ReceiveCmd(c, 49, 40) != null, "quest 1 finished");
            var bag = NsoServer.Store.PlayerInventory.Load(me.Data, server.World);
            int sword = -1; for (int i = 0; i < bag.Bag.Length; i++) if (bag.Bag[i] != null && bag.Bag[i].Id == 194) sword = i;
            Check(sword >= 0, "wooden sword received in bag slot " + sword);

            // --- NV lần đầu dùng kiếm: equip, knock the training dummy down 10 times ---
            OpenNpc(c, 12); TakeQuest(c, 12);
            var use = new Message(11); use.Writer.WriteByte(sword); c.Send(use);
            Check(ReceiveCmd(c, 48, 20) != null, "equipping the sword completes the 'use the weapon' step");
            NsoServer.World.Mob dummy = null;
            PlayerState meState = null;
            foreach (var z in server.World.GetZones(22))
                foreach (PlayerState p in z.Players) { meState = p; foreach (var mb in z.Mobs) if (mb.Template.Id == 0 && dummy == null) dummy = mb; }
            int kills = 0;
            for (int k = 0; k < 30 && kills < 10 && dummy != null; k++)   // misses are possible
            {
                meState.X = dummy.X; meState.Y = dummy.Y;
                dummy.Hp = 1; dummy.IsDead = false; dummy.IsWaitingRespawn = false;
                System.Threading.Thread.Sleep(550);
                var atk = new Message(60); atk.Writer.WriteByte(dummy.Id); c.Send(atk);
                var hr = ReceiveCmd(c, new sbyte[] { 50, 48, 51 }, 30);   // 51 = miss
                if (hr != null && hr.Command != 51) kills++;
            }
            Check(kills == 10, "10 training-dummy kills counted (" + kills + ")");
            names = OpenNpc(c, 12);
            Check(names.Count > 0 && names[0] == "Hoàn thành nhiệm vụ", "quest 2 ready to report");
            ChooseMenu(c, 12, 0);
            Check(ReceiveCmd(c, 49, 40) != null && me.TaskId == 3, "quest 2 finished, next quest " + me.TaskId);

            // --- NV diệt sên trừ cóc: shop, buy rice balls, eat ---
            OpenNpc(c, 12); TakeQuest(c, 12);
            var req = new Message(-30); req.Writer.WriteByte(-103); req.Writer.WriteByte(9); c.Send(req);
            Check(ReceiveCmd(c, 33, 20) != null, "Tabemono's yen food shop lists its items (OPEN_UI_SHOP)");
            var buy = new Message(13); buy.Writer.WriteByte(9); buy.Writer.WriteByte(0); buy.Writer.WriteShort(2); c.Send(buy);
            Check(ReceiveCmd(c, 48, 30) != null, "buying 2 rice balls completes the 'buy food' step");
            bag = NsoServer.Store.PlayerInventory.Load(me.Data, server.World);
            int rice = -1; for (int i = 0; i < bag.Bag.Length; i++) if (bag.Bag[i] != null && bag.Bag[i].Id == 23) rice = i;
            use = new Message(11); use.Writer.WriteByte(rice); c.Send(use);
            Check(rice >= 0 && ReceiveCmd(c, 48, 30) != null, "eating completes the 'use food' step");
            Check(meState.Effects.Count == 1 && meState.Effects[0].Type == 0, "rice ball effect active (HP/MP every half second)");
        }
        catch (Exception ex) { Console.WriteLine("EXCEPTION " + ex); bad++; }
        server.Stop();
        return bad + failures;
    }

    static void TakeQuest(TestClient c, int npc)
    {
        ChooseMenu(c, npc, 0);
        ReceiveCmd(c, 39, 20);
        var tg = new Message(47); tg.Writer.WriteByte(npc); tg.Writer.WriteByte(0); c.Send(tg);
        ReceiveCmd(c, 47, 20);
    }

    static List<string> OpenNpc(TestClient c, int npc)
    {
        var m = new Message(40); m.Writer.WriteShort(npc); c.Send(m);
        var r = ReceiveCmd(c, new sbyte[] { 63, 40, -26 }, 20);
        var list = new List<string>();
        if (r != null && r.Command == 63) while (r.Reader.Remaining > 0) list.Add(r.Reader.ReadUTF());
        return list;
    }

    static void ChooseMenu(TestClient c, int npc, int index)
    {
        var m = new Message(29); m.Writer.WriteByte(0); m.Writer.WriteByte(npc); m.Writer.WriteByte(index); m.Writer.WriteByte(0); c.Send(m);
    }

    // Progress check: quest item drop + pickup (NV vật liệu tạo giáp), school admission, skill book, skill and potential points.
    static int ProgressCheck()
    {
        string baked = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var server = new GameServer(new ServerConfig { DataDirectory = Dir() });
        TestClient.Srv = server;
        server.Static = new FileStaticData(baked);
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(baked, "tables.txt")), Path.Combine(baked, "Data"));
        server.Start();
        int bad = 0;
        try
        {
            var world = server.World;
            // a map whose zone 0 has a "nhím đá" (mob template 3)
            int mapId = -1; NsoServer.World.Mob target = null;
            for (int id = 0; id < 200 && target == null; id++)
            {
                var t = world.FindMap(id);
                if (t == null || !t.IsNormal) continue;
                bool has = false; foreach (var s in t.Monsters) if (s.TemplateId == 3) has = true;
                if (!has) continue;
                foreach (var mb in world.GetZone(id, 0).Mobs) if (mb.Template.Id == 3 && !mb.IsDead && target == null) { target = mb; mapId = id; }
            }
            long exp5 = 0; while (world.GetLevel(exp5) < 5) exp5 += 50;
            var c = new TestClient(0);
            c.Handshake();
            Login(c, "player01", "secret");
            c.Receive();
            c.Send(TestClient.NotMap(-101));
            c.Receive();
            var cr = TestClient.NotMap(-125);
            cr.Writer.WriteUTF("ninjatest"); cr.Writer.WriteByte(1); cr.Writer.WriteByte(2);
            c.Send(cr);
            c.Receive();
            NsoServer.Store.PlayerRecord me = null; foreach (var rec in server.Accounts.GetPlayers(1)) me = rec;
            // NV vật liệu tạo giáp, step 1 "Nhặt lông nhím", with a sword equipped
            me.MapId = mapId; me.X = target.X; me.Y = target.Y; me.TaskId = 4; me.Exp = exp5;
            me.Data = "{\"equipment\":[null,{\"id\":194,\"index\":1,\"options\":[[0,10]]}],\"task\":{\"index\":1,\"count\":0},\"yen\":0}";
            var sel = TestClient.NotMap(-126);
            sel.Writer.WriteUTF("ninjatest");
            c.Send(sel);
            for (int i = 0; i < 10; i++) c.Receive();
            Console.WriteLine("  map " + mapId + ", mob " + target.Id + ", level " + world.GetLevel(me.Exp));
            PlayerState st = null; foreach (PlayerState p in world.GetZone(mapId, 0).Players) st = p;
            Message drop = null;
            for (int k = 0; k < 40 && drop == null; k++)
            {
                st.X = target.X; st.Y = target.Y; st.Hp = st.MaxHp;
                target.Hp = 1; target.IsDead = false; target.IsWaitingRespawn = false;
                System.Threading.Thread.Sleep(550);
                var atk = new Message(60); atk.Writer.WriteByte(target.Id); c.Send(atk);
                // the item appears (ITEMMAP_ADD) before the death message, as in the original
                for (int n = 0; n < 6; n++)
                {
                    var r = ReceiveCmd(c, new sbyte[] { 6, -4, 51 }, 30);
                    if (r == null || r.Command != 6) break;
                    var raw = RawBytes(r);
                    if (((raw[2] << 8) | raw[3]) == 209) { drop = new Message(6, raw); break; }
                }
            }
            Check(drop != null, "a killed nhím đá drops the quest item (ITEMMAP_ADD)");
            if (drop != null)
            {
                short itemMapId = drop.Reader.ReadShort(), itemId = drop.Reader.ReadShort();
                Check(itemId == 209, "dropped item is lông nhím (209), got " + itemId);
                var pick = new Message(-14); pick.Writer.WriteShort(itemMapId); c.Send(pick);
                Check(ReceiveCmd(c, -14, 20) != null, "pickup confirmed (ITEMMAP_MYPICK)");
                var upd = ReceiveCmd(c, new sbyte[] { 50, 48 }, 20);
                Check(upd != null, "pickup counts for the quest (TASK_UPDATE)");
            }
            c.Close();
            System.Threading.Thread.Sleep(300);

            // --- school: level 10, quest 9 not taken, no weapon worn ---
            long exp10 = 0; while (world.GetLevel(exp10) < 11) exp10 += 500;
            me.MapId = 1; me.X = 193; me.Y = 384; me.TaskId = 9; me.Exp = exp10; me.ClassId = 0;
            me.Data = "{\"yen\":0}";
            var c2 = new TestClient(0);
            c2.Handshake();
            Login(c2, "player01", "secret");
            c2.Receive();
            c2.Send(TestClient.NotMap(-101));
            c2.Receive();
            sel = TestClient.NotMap(-126);
            sel.Writer.WriteUTF("ninjatest");
            c2.Send(sel);
            for (int i = 0; i < 10; i++) c2.Receive();
            var names = OpenNpc(c2, 9);
            int join = names.IndexOf("Nhập học");
            Check(join >= 0, "Toyotomi offers 'Nhập học' (" + string.Join("|", names) + ")");
            ChooseMenu(c2, 9, join);
            ReceiveCmd(c2, 63, 20);
            ChooseMenu(c2, 9, 0);   // Ninja Kiếm
            var info = ReceiveCmd(c2, 47, 30);
            Check(info != null && me.ClassId == 1, "joined the sword class, quest 9 started (class " + me.ClassId + ")");
            var bag = NsoServer.Store.PlayerInventory.Load(me.Data, world);
            int book = -1, weapon = -1;
            for (int i = 0; i < bag.Bag.Length; i++) { if (bag.Bag[i] == null) continue; if (bag.Bag[i].Id == 40) book = i; if (bag.Bag[i].Id == 94) weapon = i; }
            Check(book >= 0 && weapon >= 0, "school gives skill book 40 and sword 94");
            var use = new Message(11); use.Writer.WriteByte(book); c2.Send(use);
            Check(ReceiveCmd(c2, 48, 20) != null && ReceiveCmd(c2, 48, 20) != null, "reading the book completes two steps");
            var up = new Message(-30); up.Writer.WriteByte(-108); up.Writer.WriteShort(1); up.Writer.WriteByte(1); c2.Send(up);
            // the original's learnSkill already skips the 'raise a skill' step (two taskNext), so only the skill update comes back
            var sk = ReceiveCmd(c2, -30, 20);
            Check(sk != null && RawBytes(sk)[0] == unchecked((byte)-125), "skill raised to point 2 (ME_LOAD_SKILL)");
            var pot = new Message(-30); pot.Writer.WriteByte(-109); pot.Writer.WriteByte(0); pot.Writer.WriteShort(5); c2.Send(pot);
            Check(ReceiveCmd(c2, 48, 20) != null, "potential points spent completes 'Tăng điểm tiềm năng'");
            use = new Message(11); use.Writer.WriteByte(weapon); c2.Send(use);
            Check(ReceiveCmd(c2, 11, 20) != null, "the school sword can be worn by the sword class");
            PlayerState st2 = null; foreach (PlayerState p in world.GetZone(1, me.MapId == 1 ? 0 : 0).Players) st2 = p;
            foreach (var z in world.GetZones(1)) foreach (PlayerState p in z.Players) st2 = p;
            Check(st2 != null && st2.SelectedSkill != null && st2.SelectedSkill.TemplateId == 1, "skill Hiyoko is the selected attack");
        }
        catch (Exception ex) { Console.WriteLine("EXCEPTION " + ex); bad++; }
        server.Stop();
        return bad + failures;
    }

    // Combat port check: hit a low-level monster, kill it, gain experience, see it respawn.
    static int CombatCheck()
    {
        string baked = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var server = new GameServer(new ServerConfig { DataDirectory = Dir() });
        TestClient.Srv = server;
        server.Static = new FileStaticData(baked);
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(baked, "tables.txt")), Path.Combine(baked, "Data"));
        server.Start();
        int bad = 0;
        try
        {
            // a normal map whose zone 0 has an ordinary monster of level <= 5
            NsoServer.World.Mob target = null; int mapId = -1;
            for (int id = 0; id < 200 && target == null; id++)
            {
                var t = server.World.FindMap(id);
                if (t == null || !t.IsNormal || t.Monsters.Count == 0) continue;
                var z = server.World.GetZone(id, 0);
                if (z == null) continue;
                foreach (var mob in z.Mobs)
                    if (!mob.IsDead && !mob.IsBoss && mob.LevelBoss == 0 && mob.Template.Id != 0 && mob.Level <= 5) { target = mob; mapId = id; break; }
            }
            Console.WriteLine("target: map " + mapId + " mob " + target.Id + " tpl " + target.Template.Id + " lv " + target.Level + " hp " + target.Hp);
            var c = new TestClient(0);
            c.Handshake();
            Login(c, "player01", "secret");
            c.Receive();
            c.Send(TestClient.NotMap(-101));
            c.Receive();
            var cr = TestClient.NotMap(-125);
            cr.Writer.WriteUTF("ninjatest"); cr.Writer.WriteByte(1); cr.Writer.WriteByte(2);
            c.Send(cr);
            c.Receive();
            foreach (var rec in server.Accounts.GetPlayers(1))
            {
                rec.MapId = mapId; rec.X = target.X; rec.Y = target.Y;
                rec.Data = "{\"equipment\":[null,{\"id\":194,\"index\":1,\"options\":[[0,10]]}]}";   // a weapon is needed to attack
            }
            var sel = TestClient.NotMap(-126);
            sel.Writer.WriteUTF("ninjatest");
            c.Send(sel);
            for (int i = 0; i < 10; i++) c.Receive();   // the 10 enter-world messages

            // 1. one hit: skill animation then HP / miss
            var atk = new Message(60); atk.Writer.WriteByte(target.Id); c.Send(atk);
            var r = ReceiveCmd(c, 60, 20);
            Check(r != null && r.Reader.ReadInt() > 0 && r.Reader.ReadSByte() == 0 && r.Reader.ReadByte() == target.Id, "attack animation (cmd 60) names the skill and the monster");
            r = ReceiveCmd(c, new sbyte[] { -1, 51, -4 }, 20);
            Check(r != null, "monster answers with HP / miss / death (got " + (r == null ? "nothing" : r.Command.ToString()) + ")");

            // 2. kill: leave 1 HP, hit until it dies (misses are possible)
            // make sure at least one hit lands for real experience (a 1-HP kill gives 0 exp, as in Java)
            long expGot0 = 0;
            for (int tries = 0; tries < 15 && expGot0 == 0; tries++)
            {
                System.Threading.Thread.Sleep(550);
                atk = new Message(60); atk.Writer.WriteByte(target.Id); c.Send(atk);
                var x = ReceiveCmd(c, new sbyte[] { 5, 51 }, 20);
                if (x != null && x.Command == 5) expGot0 = x.Reader.ReadLong();
            }
            Console.WriteLine("  exp from a normal hit: " + expGot0);
            target.Hp = 1;
            bool died = false; long expGot = 0;
            for (int tries = 0; tries < 15 && !died; tries++)
            {
                System.Threading.Thread.Sleep(550);   // skill cooldown 500 ms
                atk = new Message(60); atk.Writer.WriteByte(target.Id); c.Send(atk);
                r = ReceiveCmd(c, new sbyte[] { -1, 51, -4, 5 }, 20);
                if (r != null && r.Command == 5) { expGot += r.Reader.ReadLong(); r = ReceiveCmd(c, new sbyte[] { -1, 51, -4 }, 20); }
                if (r != null && r.Command == -4) died = true;
            }
            Check(died, "monster dies (NPC_DIE -4)");
            NsoServer.Store.PlayerRecord me = null; foreach (var rec in server.Accounts.GetPlayers(1)) me = rec;
            Console.WriteLine("  exp message " + expGot + ", saved exp " + me.Exp);
            Check(me.Exp > 0, "experience gained and stored on the character");

            // 3. respawn after 20 half-second ticks
            r = ReceiveCmd(c, -5, 400);
            Check(r != null && r.Reader.ReadByte() == target.Id && !target.IsDead && target.Hp == target.MaxHp, "monster respawns (NPC_LIVE -5) with full HP");

            // 4. level up from experience
            Check(server.World.GetLevel(0) == 1 && server.World.GetLevel(200) == 2, "level table: 0 exp = level 1, 200 exp = level 2");
        }
        catch (Exception ex) { Console.WriteLine("EXCEPTION " + ex); bad++; }
        server.Stop();
        return bad + failures;
    }

    // reads messages until one of the wanted commands arrives (null after max messages)
    static Message ReceiveCmd(TestClient c, sbyte want, int max) { return ReceiveCmd(c, new[] { want }, max); }
    static Message ReceiveCmd(TestClient c, sbyte[] want, int max)
    {
        for (int i = 0; i < max; i++)
        {
            var task = System.Threading.Tasks.Task.Run(() => c.Receive());
            if (!task.Wait(15000)) return null;
            var m = task.Result;
            if (Environment.GetEnvironmentVariable("NSO_TRACE") == "1") Console.WriteLine("    recv " + m.Command);
            if (Array.IndexOf(want, m.Command) >= 0) return m;
        }
        return null;
    }

    // Enters the world with a fresh character on the C# server and compares each message with the capture
    // taken from the original Java server for the same character.
    static int EnterCheck(string capturePath, int mapId)
    {
        string baked = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var server = new GameServer(new ServerConfig { DataDirectory = Dir() });
        TestClient.Srv = server;
        server.Logger = t => Console.WriteLine("  [server] " + t);
        server.Static = new FileStaticData(baked);
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(baked, "tables.txt")), Path.Combine(baked, "Data"));
        server.Start();
        var expected = new System.Collections.Generic.List<string[]>();
        foreach (var line in File.ReadAllLines(capturePath)) expected.Add(line.Split(' '));
        int bad = 0;
        try
        {
            var c = new TestClient(0);
            c.Handshake();
            Login(c, "player01", "secret");
            c.Receive();
            c.Send(TestClient.NotMap(-101));
            c.Receive();
            var cr = TestClient.NotMap(-125);
            cr.Writer.WriteUTF("ninjatest"); cr.Writer.WriteByte(1); cr.Writer.WriteByte(2);
            c.Send(cr);
            c.Receive();
            foreach (var rec in server.Accounts.GetPlayers(1)) { rec.MapId = mapId; rec.X = 300; rec.Y = 200; }
            var sel = TestClient.NotMap(-126);
            sel.Writer.WriteUTF("ninjatest");
            c.Send(sel);
            int ei = 0;
            while (ei < expected.Count)
            {
                var e = expected[ei];
                sbyte ecmd = sbyte.Parse(e[0]);
                if (ecmd == 53)
                {
                    // the Java welcome pop-up has no offline equivalent; what follows is monsters attacking the new character
                    if (mapId == 14) { ei++; continue; }   // one hit kills: the whole sequence is deterministic apart from damage numbers
                    if (mapId == 3)
                    {
                        bad += DeathStructure(c, want: 0);
                    }
                    break;
                }
                var m = c.Receive();
                var got = RawBytes(m);
                var want = Hex(e[2]);
                bool same = m.Command == ecmd && got.Length == want.Length && Same(Mask(ecmd, got), Mask(ecmd, want));
                Console.WriteLine((same ? "PASS  " : "FAIL  ") + "message " + ei + " cmd " + ecmd + " (" + want.Length + " bytes)");
                if (!same)
                {
                    bad++;
                    Console.WriteLine("      got cmd " + m.Command + " len " + got.Length);
                    int n = Math.Min(got.Length, want.Length), k = 0;
                    while (k < n && Mask(ecmd, got)[k] == Mask(ecmd, want)[k]) k++;
                    Console.WriteLine("      first difference at byte " + k);
                }
                ei++;
            }
        }
        catch (Exception ex) { Console.WriteLine("EXCEPTION " + ex.Message); bad++; }
        server.Stop();
        Console.WriteLine(bad == 0 ? "ENTER WORLD MATCHES JAVA" : bad + " MISMATCHES");
        return bad;
    }

    // Replays the actions recorded from the Java server (walk_*.txt) against the C# server and compares every reply.
    static int WalkCheck(string capturePath, int mapId, int x, int y, int taskId)
    {
        string baked = Path.GetFullPath(@"C:\My project\Assets\StreamingAssets\NsoStatic");
        var server = new GameServer(new ServerConfig { DataDirectory = Dir() });
        TestClient.Srv = server;
        server.Static = new FileStaticData(baked);
        server.World = new NsoServer.World.GameWorld(NsoServer.Data.TableFile.Load(Path.Combine(baked, "tables.txt")), Path.Combine(baked, "Data"));
        server.Start();
        // sections: marker -> expected messages
        var names = new System.Collections.Generic.List<string>();
        var sections = new System.Collections.Generic.List<System.Collections.Generic.List<string[]>>();
        foreach (var line in File.ReadAllLines(capturePath))
        {
            if (line.StartsWith("# ")) { names.Add(line.Substring(2)); sections.Add(new System.Collections.Generic.List<string[]>()); }
            else if (sections.Count > 0) sections[sections.Count - 1].Add(line.Split(' '));
        }
        int bad = 0;
        try
        {
            var c = new TestClient(0);
            c.Handshake();
            Login(c, "player01", "secret");
            c.Receive();
            c.Send(TestClient.NotMap(-101));
            c.Receive();
            var cr = TestClient.NotMap(-125);
            cr.Writer.WriteUTF("ninjatest"); cr.Writer.WriteByte(1); cr.Writer.WriteByte(2);
            c.Send(cr);
            c.Receive();
            foreach (var rec in server.Accounts.GetPlayers(1)) { rec.MapId = mapId; rec.X = x; rec.Y = y; rec.TaskId = taskId; }
            var sel = TestClient.NotMap(-126);
            sel.Writer.WriteUTF("ninjatest");
            c.Send(sel);
            // everything the server sends right after entering (checked message by message by entercheck); the Java pop-up has no equivalent
            int enterCount = 0;
            foreach (var e in sections[0]) if (e[0] != "53") enterCount++;
            for (int i = 0; i < enterCount; i++) c.Receive();
            for (int s = 1; s < names.Count; s++)
            {
                var a = names[s].Split(':');
                Message m;
                switch (a[0])
                {
                    case "move": m = new Message(1); m.Writer.WriteShort(int.Parse(a[1])); m.Writer.WriteShort(int.Parse(a[2])); break;
                    case "mapchange": m = new Message(-17); break;
                    case "openzone": m = new Message(36); break;
                    case "back": m = new Message(-9); break;
                    case "live": m = new Message(-10); break;
                    case "zone": m = new Message(28); m.Writer.WriteByte(int.Parse(a[1])); m.Writer.WriteByte(0); break;
                    default: throw new Exception("unknown action " + names[s]);
                }
                c.Send(m);
                foreach (var e in sections[s])
                {
                    sbyte ecmd = sbyte.Parse(e[0]);
                    var want = e.Length > 2 ? Hex(e[2]) : new byte[0];
                    var got = c.Receive();
                    var gb = RawBytes(got);
                    bool same = got.Command == ecmd && gb.Length == want.Length && Same(Mask(ecmd, gb), Mask(ecmd, want));
                    Console.WriteLine((same ? "PASS  " : "FAIL  ") + names[s] + " -> cmd " + ecmd + " (" + want.Length + " bytes)");
                    if (!same) { bad++; Console.WriteLine("      got cmd " + got.Command + " len " + gb.Length); }
                }
                if (c.HasMore(400)) { bad++; Console.WriteLine("FAIL  " + names[s] + " -> unexpected extra message"); }
            }
        }
        catch (Exception ex) { Console.WriteLine("EXCEPTION " + ex.Message); bad++; }
        server.Stop();
        Console.WriteLine(bad == 0 ? "WALK MATCHES JAVA" : bad + " MISMATCHES");
        return bad;
    }

    // Several weak monsters hit a level-1 character before it dies, and the damage is random, so only the shape is checked:
    // pairs of (hit on me, hit seen by all), HP strictly falling, then move, reset point, ME_DIE, PLAYER_DIE at a floor position.
    static int DeathStructure(TestClient c, int want)
    {
        int hits = 0, bad = 0, lastHp = 50;
        while (true)
        {
            var m = c.Receive();
            var b = RawBytes(m);
            if (m.Command == -3 && b.Length == 13)
            {
                var m2 = c.Receive(); var b2 = RawBytes(m2);
                int hp = (b2[5] << 24) | (b2[6] << 16) | (b2[7] << 8) | b2[8];
                if (m2.Command != -2 || b2.Length != 17 || hp > lastHp) { Console.WriteLine("FAIL  hit pair malformed"); return 1; }
                lastHp = hp; hits++;
                continue;
            }
            if (m.Command == 1 && b.Length == 8)
            {
                var steps = new[] { c.Receive(), c.Receive(), c.Receive() };
                bool ok = steps[0].Command == 52 && steps[1].Command == -11 && steps[2].Command == 0 && hits > 0;
                Console.WriteLine((ok ? "PASS  " : "FAIL  ") + "death after " + hits + " hits: move, reset point, ME_DIE, PLAYER_DIE");
                return ok ? 0 : 1;
            }
            Console.WriteLine("FAIL  unexpected cmd " + m.Command);
            return 1;
        }
    }

    static byte[] RawBytes(Message m)
    {
        var list = new System.Collections.Generic.List<byte>();
        try { while (true) list.Add(m.Reader.ReadByte()); } catch (EndOfStreamException) { }
        return list.ToArray();
    }

    // payload after the sub-command byte; null if the sub-command differs
    static byte[] Rest(Message m, sbyte expectedSub)
    {
        if (m.Reader.ReadSByte() != expectedSub) return null;
        var list = new System.Collections.Generic.List<byte>();
        try { while (true) list.Add(m.Reader.ReadByte()); } catch (EndOfStreamException) { }
        return list.ToArray();
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static bool CheckMobTemplate(TestClient c, short id, bool boss)
    {
        var request = TestClient.NotMap(-108);
        request.Writer.WriteShort(id);
        c.Send(request);
        var response = c.Receive();
        if (response.Command != -28 || response.Reader.ReadSByte() != -108 || response.Reader.ReadShort() != id) return false;
        response.Reader.ReadSByte();
        int imageCount = response.Reader.ReadSByte();
        if (imageCount <= 0) return false;
        for (int i = 0; i < imageCount; i++)
        {
            int length = response.Reader.ReadInt();
            if (length <= 8 || response.Reader.ReadBytes(length).Length != length) return false;
        }
        if (response.Reader.ReadBoolean() != boss) return false;
        if (boss)
        {
            int moveCount = response.Reader.ReadByte();
            if (moveCount <= 0) return false;
            response.Reader.ReadBytes(moveCount);
            int attackCount = response.Reader.ReadByte();
            if (attackCount <= 0) return false;
            for (int i = 0; i < attackCount; i++) response.Reader.ReadBytes(response.Reader.ReadByte());
            if (response.Reader.ReadInt() != 1) return false;
            int imageInfoCount = response.Reader.ReadByte();
            if (imageInfoCount <= 0) return false;
            response.Reader.ReadBytes(imageInfoCount * 5);
            int frameCount = response.Reader.ReadShort();
            if (frameCount <= 0) return false;
            for (int i = 0; i < frameCount; i++) response.Reader.ReadBytes(response.Reader.ReadByte() * 5);
            if (response.Reader.ReadShort() != 0) return false;
        }
        else if (response.Reader.ReadInt() != 0) return false;
        return response.Reader.Remaining == 0;
    }

    static bool CheckInventorySerialization(NsoServer.World.GameWorld world)
    {
        const string json = "{\"bagCells\":30,\"bag\":[{\"id\":13,\"index\":2,\"quantity\":7,\"isLock\":false,\"expire\":-1}],\"equipment\":[null,{\"id\":194,\"upgrade\":2,\"sys\":3}],\"fashion\":[]}";
        var record = new NsoServer.Store.PlayerRecord { Id = 9, Name = "inventorytest", Gender = 0, Head = 11, Data = json };
        var message = NsoServer.Net.EnterWorld.LoadAll(record, world);
        var wire = new NsoServer.Net.Message(message.Command, message.GetData());
        if (wire.Command != -30 || wire.Reader.ReadSByte() != -127) return false;
        wire.Reader.ReadInt(); wire.Reader.ReadUTF();
        wire.Reader.ReadByte(); wire.Reader.ReadByte(); wire.Reader.ReadShort(); wire.Reader.ReadByte(); wire.Reader.ReadUTF();
        wire.Reader.ReadByte(); wire.Reader.ReadByte();
        for (int i = 0; i < 4; i++) wire.Reader.ReadInt();
        wire.Reader.ReadLong(); wire.Reader.ReadLong(); wire.Reader.ReadShort(); wire.Reader.ReadShort();
        wire.Reader.ReadByte(); wire.Reader.ReadShort(); wire.Reader.ReadShort(); wire.Reader.ReadShort();
        wire.Reader.ReadInt(); wire.Reader.ReadInt(); wire.Reader.ReadShort();
        int skills = wire.Reader.ReadByte(); wire.Reader.ReadBytes(skills * 2);
        wire.Reader.ReadInt(); wire.Reader.ReadInt(); wire.Reader.ReadInt();
        if (wire.Reader.ReadByte() != 30) return false;
        for (int i = 0; i < 2; i++) if (wire.Reader.ReadShort() != -1) return false;
        if (wire.Reader.ReadShort() != 13 || wire.Reader.ReadBoolean() || wire.Reader.ReadBoolean() || wire.Reader.ReadShort() != 7) return false;
        for (int i = 3; i < 30; i++) if (wire.Reader.ReadShort() != -1) return false;
        if (wire.Reader.ReadShort() != -1 || wire.Reader.ReadShort() != 194 || wire.Reader.ReadByte() != 2 || wire.Reader.ReadByte() != 3) return false;
        for (int i = 2; i < 16; i++) if (wire.Reader.ReadShort() != -1) return false;
        if (!wire.Reader.ReadBoolean() || wire.Reader.ReadBoolean()) return false;
        if (wire.Reader.ReadShort() != 11 || wire.Reader.ReadShort() != 12 || wire.Reader.ReadShort() != 10 || wire.Reader.ReadShort() != 9) return false;
        for (int i = 0; i < 10; i++) if (wire.Reader.ReadShort() != -1) return false;
        for (int i = 0; i < 16; i++) if (wire.Reader.ReadShort() != -1) return false;
        if (wire.Reader.Remaining != 0) return false;
        var inventory = NsoServer.Store.PlayerInventory.Load(json, world);
        var roundTrip = NsoServer.Store.PlayerInventory.Load(inventory.ToJson(), world);
        return roundTrip.Bag[2] != null && roundTrip.Bag[2].Id == 13 && roundTrip.Bag[2].Quantity == 7 &&
            roundTrip.Equipment[1] != null && roundTrip.Equipment[1].Id == 194 && roundTrip.Equipment[1].System == 3;
    }

    class BigData : IStaticData
    {
        public byte[] Version { get { return new byte[3]; } }
        public byte[] Data { get { return new byte[50000]; } }
        public byte[] Map { get { return new byte[0]; } }
        public byte[] Skill { get { return new byte[0]; } }
        public byte[] Item { get { return new byte[0]; } }
    }
}
