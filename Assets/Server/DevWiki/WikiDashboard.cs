using System;
using System.Collections.Generic;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.DevWiki
{
    using Math = System.Math;
    using Message = NsoServer.Net.Message;

    // Offline global balancing UI. Never edits a PlayerRecord or inventory instance.
    public sealed partial class WikiControl
    {
        private int dashboardRevision;
        private static Dictionary<string, object> Object(object raw)
        {
            var d = raw as Dictionary<string, object>;
            if (d == null)
                throw new ArgumentException("Expected a JSON object.");
            return d;
        }

        private static void Keys(Dictionary<string, object> d, params string[] allowed)
        {
            foreach (string key in d.Keys)
                if (Array.IndexOf(allowed, key) < 0)
                    throw new ArgumentException("Unknown field: " + key);
        }

        private object DashboardState()
        {
            var players = new List<object>();
            var monsters = new List<object>();
            var equipment = new List<object>();
            foreach (var z in server.World.AllZones())
                foreach (PlayerState p in z.Players)
                    players.Add(Obj("name", p.Record.Name, "level", p.Level, "stats", PlayerTuning.Values(p, server.World)));
            var samples = new List<object>();
            foreach (int level in new[]
            {
                1,
                10,
                30,
                50,
                100
            }

            )
            {
                if (level > server.World.MaxLevel)
                    continue;
                var p = new PlayerState
                {
                    Record = new PlayerRecord
                    {
                        Exp = server.World.ExpForLevel(level)
                    }
                };
                PlayerCombat.Init(p, server.World);
                samples.Add(Obj("level", p.Level, "stats", PlayerTuning.Values(p, server.World)));
            }

            foreach (var tpl in server.World.ItemTemplates())
                if (GameBalanceRules.IsEquipment(tpl))
                {
                    var baseline = new List<object>();
                    var store = server.World.ItemStoreSample(tpl.Id);
                    if (store != null)
                    {
                        var original = new PlayerItem
                        {
                            Id = tpl.Id
                        };
                        for (int i = 0; i < store.OptionIds.Length; i++)
                            original.Options.Add(new PlayerItemOption { Id = store.OptionIds[i], Param = store.MaxParams[i] });
                        foreach (var o in server.World.BalanceRules.EffectiveOptions(original))
                            baseline.Add(Obj("id", o.Id, "value", o.Param));
                    }

                    equipment.Add(Obj("id", tpl.Id, "name", tpl.Name, "level", tpl.Level, "type", tpl.Type, "sampleOptions", baseline));
                }

            foreach (var tpl in server.World.MonsterTemplates())
            {
                Dictionary<string, int> overrides;
                var live = new List<object>();
                foreach (var z in server.World.AllZones())
                    if (z.Players.Count > 0)
                        foreach (var m in z.Mobs)
                            if (m.Template.Id == tpl.Id)
                                live.Add(Obj("mapId", z.Tile.Id, "zone", z.Id, "id", m.Id, "hp", m.Hp, "maxHp", m.MaxHp, "level", m.Level, "damage", m.DamageOnPlayer, "damage2", m.DamageOnPlayer2, "delay", m.AttackDelay, "sys", m.Sys, "dead", m.IsDead));
                int d = (int)(tpl.Level + Math.Pow(tpl.Level, 2) / 5) * (tpl.IsBoss ? 20 : 1);
                monsters.Add(Obj("id", tpl.Id, "name", tpl.Name, "defaults", Obj("Level", tpl.Level, "MaxHp", tpl.Hp, "DamageOnPlayer", d, "DamageOnPlayer2", d - d / 10, "AttackDelay", 3000, "Sys", 1), "overrides", server.World.MobTuning.Entries.TryGetValue(tpl.Id, out overrides) ? (object)overrides : new Dictionary<string, int>(), "live", live));
            }

            return Obj("revision", dashboardRevision, "balance", server.World.BalanceRules.ToObject(), "playerFields", GameBalanceRules.PlayerFields, "progression", ProgressionMetadata(), "players", players, "samples", samples, "equipment", equipment, "monsters", monsters, "monsterSchema", MobTuning.Schema(), "persistence", new[] { "game-balance.json", "monster-tuning.json" });
        }

        private KeyValuePair<int, string> Dashboard(string method, string body)
        {
            if (method == "GET")
            {
                lock (server.World.SyncRoot)
                    return Reply(200, DashboardState());
            }

            Dictionary<string, object> root;
            try
            {
                root = Object(Json.Parse(body));
            }
            catch
            {
                throw new ArgumentException("Invalid dashboard JSON.");
            }

            Keys(root, "revision", "kind", "values", "templateId", "reset", "scope", "from", "to", "classes", "includeWoodenSword");
            if (!root.ContainsKey("revision") || !root.ContainsKey("kind") || !root.ContainsKey("values"))
                throw new ArgumentException("Missing dashboard fields.");
            if (root.ContainsKey("reset") && !(root["reset"] is bool))
                throw new ArgumentException("reset must be boolean.");
            int expected = GameBalanceRules.Integer(root["revision"], 0, int.MaxValue);
            string kind = root["kind"] as string;
            var values = Object(root["values"]);
            bool reset = root.ContainsKey("reset") && (bool)root["reset"];
            if (kind == "progressionPreview")
                return PreviewProgression(root, values, expected);
            if ((kind == "monster" || kind == "equipment") && !root.ContainsKey("templateId"))
                throw new ArgumentException("Missing template ID.");
            lock (changes)
                lock (server.World.SyncRoot)
                {
                    if (expected != dashboardRevision)
                        return Reply(409, Obj("error", "Balance changed in another tab. Reload before saving."));
                    object before;
                    if (kind == "monster")
                    {
                        before = server.World.MobTuning.ToObject();
                        EditMonster(GameBalanceRules.Integer(root["templateId"], 0, short.MaxValue), values, reset);
                    }
                    else if (kind == "player" || kind == "equipment" || kind == "progression")
                    {
                        before = server.World.BalanceRules.ToObject();
                        var writer = new JsonWriter();
                        writer.Value(before);
                        var next = GameBalanceRules.FromObject(Object(Json.Parse(writer.ToString())), server.World);
                        if (kind == "progression")
                            SetProgression(next, root, values, reset);
                        else if (kind == "player")
                        {
                            Keys(values, "stats", "experiencePercent");
                            if (reset)
                            {
                                next.Player.Clear();
                                next.ExperiencePercent = 100;
                            }
                            else
                            {
                                if (!values.ContainsKey("stats") || !values.ContainsKey("experiencePercent"))
                                    throw new ArgumentException("Supply player stats and experience rate.");
                                next.Player.Clear();
                                foreach (var pair in Object(values["stats"]))
                                {
                                    if (Array.IndexOf(GameBalanceRules.PlayerFields, pair.Key) < 0)
                                        throw new ArgumentException("Unknown player balance field.");
                                    next.Player[pair.Key] = GameBalanceRules.Pair(pair.Value);
                                }

                                next.ExperiencePercent = GameBalanceRules.Integer(values["experiencePercent"], 0, 1000);
                            }
                        }
                        else
                        {
                            int id = GameBalanceRules.Integer(root["templateId"], -1, short.MaxValue);
                            var tpl = server.World.FindItem(id);
                            if (id != -1 && (tpl == null || !GameBalanceRules.IsEquipment(tpl)))
                                throw new ArgumentException("Unknown equipment template.");
                            if (reset)
                                next.Equipment.Remove(id);
                            else
                            {
                                if (values.Count > 128)
                                    throw new ArgumentException("Too many equipment options.");
                                var options = new Dictionary<int, int[]>();
                                foreach (var option in values)
                                {
                                    int optionId = GameBalanceRules.Integer(option.Key, 0, 255);
                                    if (!server.World.HasItemOption(optionId) || options.ContainsKey(optionId))
                                        throw new ArgumentException("Unknown or duplicate equipment option.");
                                    options[optionId] = GameBalanceRules.Pair(option.Value);
                                }

                                next.Equipment[id] = options;
                            }
                        }

                        next.Save(System.IO.Path.Combine(server.Config.DataDirectory, "game-balance.json"));
                        server.World.BalanceRules = next;
                        foreach (var z in server.World.AllZones())
                            foreach (PlayerState p in z.Players)
                            {
                                PlayerCombat.SetAbility(p, server.World);
                                try
                                {
                                    p.Session.Send(EnterWorld.LoadAll(p.Record, server.World, p));
                                    PlayerService.LoadClass(p);
                                    SyncMap(p);
                                }
                                catch (Exception ex)
                                {
                                    server.Log("Global balance saved; client refresh deferred: " + ex.Message);
                                }
                            }
                    }
                    else
                        throw new ArgumentException("Unknown balance section.");
                    dashboardRevision++;
                    events.Insert(0, Obj("time", DateTime.UtcNow.ToString("o"), "type", "dashboard-" + kind, "revision", dashboardRevision, "before", before, "after", root));
                    if (events.Count > 100)
                        events.RemoveAt(events.Count - 1);
                    return Reply(200, Obj("saved", true, "scope", "global", "revision", dashboardRevision));
                }
        }

        private void EditMonster(int id, Dictionary<string, object> values, bool reset)
        {
            var tpl = server.World.FindMob(id);
            if (tpl == null)
                throw new ArgumentException("Unknown monster template.");
            var next = new MobTuning();
            foreach (var pair in server.World.MobTuning.Entries)
                next.Entries[pair.Key] = new Dictionary<string, int>(pair.Value);
            if (reset)
                next.Entries.Remove(id);
            else
                next.Entries[id] = MobTuning.Validate(values);
            next.Save(System.IO.Path.Combine(server.Config.DataDirectory, "monster-tuning.json"));
            server.World.MobTuning = next;
            foreach (var z in server.World.AllZones())
            {
                bool touched = false;
                foreach (var m in z.Mobs)
                    if (m.Template.Id == id)
                    {
                        m.Level = tpl.Level;
                        m.AttackDelay = 3000;
                        m.Sys = m.TuningBaseSys;
                        m.MaxHp = (int)Math.Min(int.MaxValue, (long)tpl.Hp * (m.LevelBoss == 1 ? 10 : m.LevelBoss == 2 ? 100 : m.LevelBoss == 3 ? 200 : 1));
                        int d = (int)(m.Level + Math.Pow(m.Level, 2) / 5) * (m.IsBoss ? 20 : m.LevelBoss == 1 ? 2 : m.LevelBoss == 2 ? 3 : 1);
                        m.DamageOnPlayer = d;
                        m.DamageOnPlayer2 = d - d / 10;
                        next.Apply(m);
                        m.Hp = Math.Min(m.Hp, m.MaxHp);
                        touched = true;
                    }

                if (touched)
                    foreach (PlayerState p in z.Players)
                        try
                        {
                            SyncMap(p);
                        }
                        catch (Exception ex)
                        {
                            server.Log("Monster balance saved; client refresh deferred: " + ex.Message);
                        }
            }
        }

        private void SyncMap(PlayerState p)
        {
            p.Session.Send(new Message(Cmd.MAP_CLEAR));
            p.Session.Send(EnterWorld.MapInfo(p.MapId, p.X, p.Y, p.Zone, p));
            p.Session.Send(EnterWorld.MapItems(p.Zone.Tile, server.World, p.Session.ZoomLevel));
        }
    }
}
