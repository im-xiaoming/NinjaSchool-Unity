using System;
using System.Collections.Generic;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.DevWiki
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;

    // Local debug control surface. Reads snapshots only; the sole write replaces validated loot rules.
    // It cannot execute code, edit player records, grant items or call Unity APIs from an HTTP thread.
    public sealed class WikiControl
    {
        private readonly GameServer server;
        private readonly object changes = new object();
        private readonly List<object> events = new List<object>();
        private readonly DateTime started = DateTime.UtcNow;
        private int revision;
        public readonly string Token = Guid.NewGuid().ToString("N");

        public WikiControl(GameServer server) { this.server = server; }

        public KeyValuePair<int, string> Handle(string method, string path, string query, string body)
        {
            try
            {
                if (server.World == null) return Reply(503, Obj("error", "World data is not loaded."));
                if (path == "/control/state" && method == "GET")
                {
                    lock (server.World.SyncRoot)
                        return Reply(200, Obj("token", Token, "revision", revision, "rules", server.World.LootRules.ToObject(),
                            "defaults", new LootRules().ToObject(), "persistence", "game-rules.json", "runtime", Runtime()));
                }
                if (path == "/control/status" && method == "GET")
                {
                    lock (server.World.SyncRoot) return Reply(200, Runtime());
                }
                if (path == "/control/events" && method == "GET")
                {
                    lock (changes) return Reply(200, Obj("events", new List<object>(events)));
                }
                if (path == "/control/loot" && method == "GET") return Explain(query);
                if (path == "/control/rules" && method == "POST") return Update(body);
                return Reply(404, Obj("error", "Unknown control route or method."));
            }
            catch (ArgumentException ex) { return Reply(400, Obj("error", ex.Message)); }
            catch (Exception) { return Reply(500, Obj("error", "Operation failed; no settings change was applied.")); }
        }

        private KeyValuePair<int, string> Update(string body)
        {
            Dictionary<string, object> root;
            try { root = Json.Parse(body) as Dictionary<string, object>; }
            catch (Exception) { return Reply(400, Obj("error", "Invalid JSON.")); }
            if (root == null || root.Count != 2 || !root.ContainsKey("rules"))
                return Reply(400, Obj("error", "Expected revision and rules."));
            int expected = LootRules.Number(root, "revision");
            var next = LootRules.FromObject(root["rules"] as Dictionary<string, object>);
            lock (changes)
            {
                lock (server.World.SyncRoot)
                    if (expected != revision) return Reply(409, Obj("error", "Settings changed in another tab. Reload before saving.", "revision", revision));
                // Persist first, away from the world lock. A disk error must not partially apply live rules.
                try { next.Save(server.RulesPath); }
                catch (Exception) { return Reply(500, Obj("error", "Cannot save game-rules.json; active rules were not changed.")); }
                lock (server.World.SyncRoot)
                {
                    var previous = server.World.LootRules;
                    server.World.LootRules = next;
                    revision++;
                    events.Insert(0, Obj("time", DateTime.UtcNow.ToString("o"), "type", "rules", "revision", revision,
                        "before", previous.ToObject(), "after", next.ToObject()));
                    if (events.Count > 100) events.RemoveAt(events.Count - 1);
                    return Reply(200, Obj("revision", revision, "rules", next.ToObject(), "saved", true, "applied", "next loot event"));
                }
            }
        }

        private Dictionary<string, object> Runtime()
        {
            var players = new List<object>();
            int mobs = 0, groundItems = 0, zones = 0;
            foreach (var zone in server.World.AllZones())
            {
                zones++; mobs += zone.Mobs.Count; groundItems += zone.ItemMaps.Count;
                foreach (PlayerState p in zone.Players)
                {
                    var task = server.World.FindTask(p.Record.TaskId);
                    int index = p.Progress == null ? 0 : p.Progress.TaskIndex;
                    string step = task != null && index >= 0 && index < task.SubNames.Length ? task.SubNames[index] : null;
                    var bag = new List<object>();
                    if (p.Inventory != null)
                        foreach (var item in p.Inventory.Bag)
                            if (item != null) { var tpl = server.World.FindItem(item.Id); bag.Add(Obj("slot", item.Index, "id", item.Id,
                                "name", tpl == null ? null : tpl.Name, "quantity", item.Quantity)); }
                    players.Add(Obj("id", p.Record.Id, "name", p.Record.Name, "level", p.Level, "classId", p.Record.ClassId,
                        "hp", p.Hp, "maxHp", p.MaxHp, "mp", p.Mp, "maxMp", p.MaxMp, "mapId", p.MapId,
                        "mapName", zone.Tile.Name, "zone", zone.Id, "x", p.X, "y", p.Y, "dead", p.IsDead,
                        "questId", p.Record.TaskId, "questName", task == null ? null : task.Name,
                        "questActive", p.Progress != null && p.Progress.TaskActive, "stepIndex", index, "stepName", step,
                        "stepCount", p.Progress == null ? 0 : p.Progress.TaskCount,
                        "stepTarget", task != null && index >= 0 && index < task.Counts.Length ? task.Counts[index] : -1,
                        "yen", p.Progress == null ? 0 : p.Progress.Yen, "xu", p.Progress == null ? 0 : p.Progress.Xu,
                        "bagCells", p.Inventory == null ? 0 : p.Inventory.Bag.Length, "bag", bag));
                }
            }
            return Obj("time", DateTime.UtcNow.ToString("o"), "uptimeSeconds", (long)(DateTime.UtcNow - started).TotalSeconds,
                "zones", zones, "mobs", mobs, "groundItems", groundItems, "players", players, "revision", revision);
        }

        private KeyValuePair<int, string> Explain(string query)
        {
            var args = new Dictionary<string, object>();
            foreach (string part in query.Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0) { long n; if (!long.TryParse(Uri.UnescapeDataString(part.Substring(eq + 1)), out n))
                        throw new ArgumentException("Loot inspector expects numeric values."); args[Uri.UnescapeDataString(part.Substring(0, eq))] = n; }
            }
            int level = LootRules.Number(args, "playerLevel"), mobId = LootRules.Number(args, "mobId");
            if (level < 1 || level > 200) throw new ArgumentException("Player level must be 1..200.");
            lock (server.World.SyncRoot)
            {
                var tpl = server.World.FindMob(mobId);
                if (tpl == null) return Reply(404, Obj("error", "Unknown monster template."));
                var me = new PlayerState { Level = level, Record = new PlayerRecord(), Progress = new PlayerProgress() };
                if (args.ContainsKey("questId") && args.ContainsKey("step"))
                {
                    me.Record.TaskId = LootRules.Number(args, "questId"); me.Progress.TaskIndex = LootRules.Number(args, "step"); me.Progress.TaskActive = true;
                    var task = server.World.FindTask(me.Record.TaskId);
                    if (task == null || me.Progress.TaskIndex >= task.SubNames.Length) throw new ArgumentException("Unknown quest or step.");
                }
                var mob = new Mob { Template = tpl, Level = tpl.Level, IsBoss = tpl.IsBoss };
                var rules = server.World.LootRules;
                bool allowed = PlayerItems.CanDropLoot(me, mob, rules);
                bool normal = allowed && !mob.IsBoss && (!rules.LimitOrdinaryLoot || Math.Abs(level - mob.Level) <= rules.OrdinaryMaxGap);
                string reason = !allowed ? (rules.BlockTrainingDummy && mobId == 0 ? "training-dummy" : "overlevel") :
                    mob.IsBoss ? "boss-not-ported" : !normal ? "ordinary-level-gap" : "eligible";
                return Reply(200, Obj("playerLevel", level, "mobId", mobId, "mobName", tpl.Name, "mobLevel", tpl.Level,
                    "levelGap", level - tpl.Level, "globalAllowed", allowed, "ordinaryAllowed", normal, "reason", reason,
                    "questItemId", allowed ? PlayerTasks.TaskItemFor(me, mobId) : -1,
                    "chances", Obj("yen", normal ? 10 : 0, "item", normal ? 10 : 0, "equipment", normal ? 5 : 0, "none", normal ? 75 : 100),
                    "rules", rules.ToObject(), "note", "Eligibility is not a guaranteed drop; quest and equipment availability have separate rules."));
            }
        }

        public static Dictionary<string, object> Obj(params object[] pairs)
        {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < pairs.Length; i += 2) d[(string)pairs[i]] = pairs[i + 1];
            return d;
        }
        public static KeyValuePair<int, string> Reply(int status, object value)
        {
            var w = new JsonWriter(); w.Value(value);
            return new KeyValuePair<int, string>(status, w.ToString());
        }
    }
}
