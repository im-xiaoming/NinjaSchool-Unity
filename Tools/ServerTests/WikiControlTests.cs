using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using NsoServer;
using NsoServer.Data;
using NsoServer.DevWiki;
using NsoServer.Net;
using NsoServer.World;

static partial class Program
{
    // Real HTTP requests against an isolated world/save folder; never edits the user's running game.
    static int WebCheck()
    {
        string root = @"C:\My project", data = Path.Combine(root, "Assets", "StreamingAssets", "NsoStatic");
        var tables = TableFile.Load(Path.Combine(data, "tables.txt"));
        var world = new GameWorld(tables, Path.Combine(data, "Data"));
        var game = new GameServer(new ServerConfig { DataDirectory = Dir() }) { World = world };
        var control = new WikiControl(game);
        var wiki = new WikiServer(Path.Combine(root, "Assets", "StreamingAssets", "NsoWiki"), 18870,
            () => WikiData.Build(tables, world, Path.Combine(root, "Assets", "StreamingAssets", "NsoWiki", "notes.json")), null) { Control = control };
        try
        {
            var initial = control.Handle("GET", "/control/state", "", "");
            Check(initial.Key == 200 && initial.Value.Contains("blockTrainingDummy"), "control reads real defaults and runtime snapshot");
            var me = new PlayerState { Level = 20 };
            var dummy = new NsoServer.World.Mob { Template = new MobTemplate { Id = 0 }, Level = 1 };
            Check(!PlayerItems.CanDropLoot(me, dummy, world.LootRules), "defaults preserve dummy and overlevel exclusions");

            var next = new LootRules(false, false, 12, false, 9);
            Func<int, LootRules, string> payload = (revision, rules) => {
                var w = new JsonWriter(); w.Value(WikiControl.Obj("revision", revision, "rules", rules.ToObject())); return w.ToString(); };
            var saved = control.Handle("POST", "/control/rules", "", payload(0, next));
            Check(saved.Key == 200 && PlayerItems.CanDropLoot(me, dummy, world.LootRules), "validated web change immediately reaches gameplay loot policy");
            Check(File.Exists(game.RulesPath) && !LootRules.Load(game.RulesPath).BlockTrainingDummy &&
                LootRules.Load(game.RulesPath).OverlevelGap == 12, "rules persist and deserialize independently of wiki");
            Check(control.Handle("POST", "/control/rules", "", payload(0, new LootRules())).Key == 409 &&
                !world.LootRules.BlockTrainingDummy, "stale tab cannot overwrite a newer rules revision");
            Check(control.Handle("POST", "/control/rules", "", "{bad-json").Key == 400, "malformed JSON is rejected");
            Check(control.Handle("POST", "/control/rules", "", payload(1, next).Replace("\"overlevelGap\":12", "\"overlevelGap\":0")).Key == 400,
                "out-of-range rules are rejected before persistence");
            Check(control.Handle("POST", "/control/rules", "", payload(1, next).Replace("\"overlevelGap\":12", "\"overlevelGap\":12.5")).Key == 400,
                "fractional level thresholds are rejected");
            var explain = control.Handle("GET", "/control/loot", "playerLevel=20&mobId=0", "");
            Check(explain.Key == 200 && explain.Value.Contains("\"ordinaryAllowed\":true"), "loot inspector uses live switches instead of hardcoded defaults");
            Check(control.Handle("GET", "/control/loot", "playerLevel=0&mobId=0", "").Key == 400 &&
                control.Handle("GET", "/control/loot", "playerLevel=20&mobId=99999", "").Key == 404, "loot inspector validates levels and monster IDs");
            Check(control.Handle("GET", "/control/loot", "playerLevel=20&mobId=0&questId=2&step=999", "").Key == 400,
                "loot inspector rejects nonexistent quest steps");
            Check(control.Handle("GET", "/control/events", "", "").Value.Contains("\"revision\":1"), "configuration log records the successful change");
            string previousFolder = game.Config.DataDirectory, obstruction = Path.Combine(previousFolder, "not-a-folder");
            File.WriteAllText(obstruction, "test"); game.Config.DataDirectory = obstruction;
            Check(control.Handle("POST", "/control/rules", "", payload(1, new LootRules())).Key == 500 && !world.LootRules.BlockTrainingDummy,
                "disk failure leaves active rules and revision unchanged");
            game.Config.DataDirectory = previousFolder;

            wiki.Start();
            for (int i = 0; i < 200 && wiki.Url == null; i++) System.Threading.Thread.Sleep(10);
            if (wiki.Url == null) throw new Exception("Wiki listener did not start.");
            using (var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = new Uri(wiki.Url), Timeout = TimeSpan.FromSeconds(10) })
            {
                Check(client.GetStringAsync("/").GetAwaiter().GetResult().Contains("NSO Workbench"), "redesigned HTML is served locally");
                Check(client.GetStringAsync("/app.js").GetAwaiter().GetResult().Contains("saveRules") &&
                    client.GetStringAsync("/app.css").GetAwaiter().GetResult().Contains("@media"), "offline JS and responsive CSS assets are served");
                Check(client.GetAsync("/api/quests/2").GetAwaiter().GetResult().StatusCode == HttpStatusCode.OK,
                    "existing quest detail API remains compatible");
                Func<string, string, string, HttpResponseMessage> post = (token, origin, body) => {
                    var req = new HttpRequestMessage(HttpMethod.Post, "/control/rules") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                    if (token != null) req.Headers.Add("X-Nso-Control", token);
                    if (origin != null) req.Headers.Add("Origin", origin);
                    return client.SendAsync(req).GetAwaiter().GetResult(); };
                using (var r = post(null, null, payload(1, next))) Check(r.StatusCode == HttpStatusCode.Forbidden, "HTTP writes require local session token");
                using (var r = post(control.Token, "https://example.invalid", payload(1, next))) Check(r.StatusCode == HttpStatusCode.Forbidden, "foreign-origin writes are rejected");
                using (var req = new HttpRequestMessage(HttpMethod.Get, "/control/state"))
                {
                    req.Headers.Host = "attacker.invalid";
                    using (var r = client.SendAsync(req).GetAwaiter().GetResult()) Check(r.StatusCode == HttpStatusCode.Forbidden, "foreign Host headers cannot access controls");
                }
                using (var r = post(control.Token, wiki.Url.TrimEnd('/'), payload(1, new LootRules())))
                    Check(r.StatusCode == HttpStatusCode.OK && world.LootRules.BlockTrainingDummy, "same-origin JSON save applies through real HTTP");
                using (var r = post(control.Token, null, payload(1, next))) Check(r.StatusCode == HttpStatusCode.Conflict, "real HTTP reports revision conflict");
                using (var tcp = new System.Net.Sockets.TcpClient("127.0.0.1", new Uri(wiki.Url).Port))
                {
                    tcp.ReceiveTimeout = 3000;
                    var bytes = Encoding.ASCII.GetBytes("POST /control/rules HTTP/1.1\r\nHost: " + new Uri(wiki.Url).Authority +
                        "\r\nX-Nso-Control: " + control.Token + "\r\nContent-Type: application/json\r\nContent-Length: 8193\r\n\r\n");
                    tcp.GetStream().Write(bytes, 0, bytes.Length);
                    using (var reader = new StreamReader(tcp.GetStream())) Check(reader.ReadLine().Contains("400"), "oversized body is rejected before being read");
                }
                using (var r = client.GetAsync("/control/state").GetAwaiter().GetResult()) Check(!r.Headers.Contains("Access-Control-Allow-Origin"), "control API does not permit cross-origin reading");
                using (var tcp = new System.Net.Sockets.TcpClient("127.0.0.1", new Uri(wiki.Url).Port))
                {
                    tcp.ReceiveTimeout = 3000; tcp.NoDelay = true;
                    var body = Encoding.UTF8.GetBytes(payload(2, new LootRules()));
                    var head = Encoding.ASCII.GetBytes("POST /control/rules HTTP/1.1\r\nHost: " + new Uri(wiki.Url).Authority +
                        "\r\nX-Nso-Control: " + control.Token + "\r\nContent-Type: application/json\r\nContent-Length: " + body.Length + "\r\n\r\n");
                    var stream = tcp.GetStream(); stream.Write(head, 0, head.Length);
                    System.Threading.Thread.Sleep(10); stream.Write(body, 0, body.Length / 2);
                    System.Threading.Thread.Sleep(10); stream.Write(body, body.Length / 2, body.Length - body.Length / 2);
                    using (var reader = new StreamReader(stream)) Check(reader.ReadLine().Contains("200"), "fragmented request headers/body save correctly");
                }
                Check(client.GetAsync("/../game-rules.json").GetAwaiter().GetResult().StatusCode == HttpStatusCode.NotFound,
                    "static route allowlist does not expose saved files");
            }
            var restarted = new GameServer(new ServerConfig { DataDirectory = game.Config.DataDirectory }) { World = new GameWorld(tables, Path.Combine(data, "Data")) };
            restarted.Start();
            try { Check(restarted.World.LootRules.BlockTrainingDummy && restarted.World.LootRules.OverlevelGap == 10,
                "fresh GameServer startup loads the last saved rules"); } finally { restarted.Stop(); }
            // A separate world must retain defaults even if another world's settings change.
            world.LootRules = next;
            Check(new GameWorld(tables, Path.Combine(data, "Data")).LootRules.BlockTrainingDummy, "rules do not leak between game worlds");
        }
        catch (Exception ex) { Check(false, "webcheck exception: " + ex.Message); }
        finally { wiki.Stop(); }
        return failures;
    }
}
