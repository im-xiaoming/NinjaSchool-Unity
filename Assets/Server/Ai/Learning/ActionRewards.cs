using System;
using System.Collections.Generic;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai.Rl
{
    using Math = System.Math;
    using Message = NsoServer.Net.Message;

    /// <summary>What one character learned about one item by buying and using it (the "item ledger").</summary>
    public sealed class ItemNote
    {
        public int Bought, Used, Useful, Wasted;
        public long Spent; // yen + xu paid in total
        public string Effect = ""; // what the last useful use did, e.g. "HP back over 3 s", "stronger gear"
        /// <summary>Tried enough times and never helped: a player stops buying it (but keeps the memory).</summary>
        public bool Useless
        {
            get
            {
                return Used >= RlRuntime.Settings.LedgerUselessAfter && Useful == 0;
            }
        }

        public string Text(string name)
        {
            string verdict = Useful > 0 ? "useful (" + Useful + "/" + Used + " uses" + (Effect.Length > 0 ? ": " + Effect : "") + ")" : Used == 0 ? "bought but never used yet" : Useless ? "no visible use after " + Used + " uses" : "no visible use yet (" + Used + " uses)";
            return name + ": " + verdict + (Bought > 0 ? ", bought " + Bought + " for " + Spent + " in total" : "");
        }
    }

    /// <summary>Offline reward shaping only: observe confirmed Controller outcomes; never mutate gameplay.</summary>
    public sealed class ActionRewardHistory
    {
        public readonly HashSet<int> Used = new HashSet<int>(), Bought = new HashSet<int>(), Npcs = new HashSet<int>();
        public readonly HashSet<string> Features = new HashSet<string>();
        // User 2026-11-13: a player tries an unknown item, stops buying it when it does nothing, but remembers what
        // each item does to buy it again when the situation needs it. Persisted with the reward history.
        public readonly Dictionary<int, ItemNote> Ledger = new Dictionary<int, ItemNote>();
        public ItemNote Note(int id)
        {
            if (!Ledger.TryGetValue(id, out var n))
                Ledger[id] = n = new ItemNote();
            return n;
        }

        public bool Knows(int id)
        {
            return Ledger.TryGetValue(id, out var n) && n.Used > 0;
        }

        public bool Useless(int id)
        {
            return Ledger.TryGetValue(id, out var n) && n.Useless;
        }

        public long LastUsefulUse;
        public bool Dirty;
        // Weighted totals are session-local. A transition snapshots them, so each policy gets only its interval's events.
        public readonly double[] Totals = new double[6];
        public readonly int[] Counts = new int[6];
        public static readonly string[] Names =
        {
            "newItemUse",
            "newItemPurchase",
            "timelyItemUse",
            "npcDiscovery",
            "usefulNpcFeature",
            "wastefulPurchase"
        };
        public void Award(int kind, double weight)
        {
            Totals[kind] += weight;
            Counts[kind]++;
        }

        public Dictionary<string, object> ToObject()
        {
            return new Dictionary<string, object>
            {
                {
                    "used",
                    new List<int>(Used)
                },
                {
                    "bought",
                    new List<int>(Bought)
                },
                {
                    "npcs",
                    new List<int>(Npcs)
                },
                {
                    "features",
                    new List<string>(Features)
                },
                {
                    "lastUsefulUse",
                    LastUsefulUse
                },
                {
                    "ledger",
                    LedgerObject()
                }
            };
        }

        private Dictionary<string, object> LedgerObject()
        {
            var d = new Dictionary<string, object>();
            foreach (var kv in Ledger)
                d[kv.Key.ToString()] = new List<object>
                {
                    kv.Value.Bought,
                    kv.Value.Used,
                    kv.Value.Useful,
                    kv.Value.Wasted,
                    kv.Value.Spent,
                    kv.Value.Effect
                };
            return d;
        }

        public void Restore(object value)
        {
            var d = value as Dictionary<string, object>;
            if (d == null)
                return;
            foreach (var pair in new[]
            {
                new
                {
                    Key = "used",
                    Set = Used
                },
                new
                {
                    Key = "bought",
                    Set = Bought
                },
                new
                {
                    Key = "npcs",
                    Set = Npcs
                }
            }

            )
                if (d.TryGetValue(pair.Key, out var raw) && raw is List<object> list)
                    foreach (var id in list)
                        pair.Set.Add((int)Json.Long(id));
            if (d.TryGetValue("features", out var features) && features is List<object> keys)
                foreach (var key in keys)
                    if (key is string text && text.Length < 300)
                        Features.Add(text);
            if (d.TryGetValue("lastUsefulUse", out var last))
                LastUsefulUse = Json.Long(last);
            if (d.TryGetValue("ledger", out var ledger) && ledger is Dictionary<string, object> notes)
                foreach (var kv in notes)
                {
                    if (!int.TryParse(kv.Key, out int id) || !(kv.Value is List<object> v) || v.Count < 6)
                        continue;
                    Ledger[id] = new ItemNote
                    {
                        Bought = (int)Json.Long(v[0]),
                        Used = (int)Json.Long(v[1]),
                        Useful = (int)Json.Long(v[2]),
                        Wasted = (int)Json.Long(v[3]),
                        Spent = Json.Long(v[4]),
                        Effect = v[5] as string ?? ""
                    };
                }
        }

        public Dictionary<string, object> Debug()
        {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < Names.Length; i++)
                d[Names[i]] = new Dictionary<string, object>
                {
                    {
                        "count",
                        Counts[i]
                    },
                    {
                        "reward",
                        Totals[i]
                    }
                };
            return d;
        }
    }

    public sealed class ActionReceipt
    {
        public NinjaAgent Agent;
        public sbyte Command;
        public int Item = -1, ItemType = -1, Npc = -1, Quantity, Hp, Mp, MaxHp, MaxMp, Damage, ClassId, Zone, Population, Task, Step, Map, X, Y, Answer, FreeSlots, UpgradeTotal, SkillCount, SkillScore;
        public bool Active, HpEffect, FoodEffect, AutoPlay, BoxOpen;
        public string Feature = "", Skills, Menu;
        public NsoServer.Store.PlayerItem ItemInstance;
        public NsoServer.Net.MenuEntry FirstMenu;
        public long CatchUntil, FishUntil, Money;
        public Dictionary<int, int> Bag;
    }

    public static class ActionRewards
    {
        public static bool Observes(sbyte command)
        {
            switch (command)
            {
                case 11:
                case 12:
                case 13:
                case 16:
                case 17:
                case 19:
                case 20:
                case 21:
                case 22:
                case 40:
                case 29:
                case 47:
                case 36:
                case 28:
                    return true;
                default:
                    return false;
            }
        }

        private static Dictionary<int, int> BagCounts(PlayerState p)
        {
            var counts = new Dictionary<int, int>();
            foreach (var it in p.Inventory.Bag)
                if (it != null)
                {
                    counts.TryGetValue(it.Id, out int n);
                    counts[it.Id] = n + it.Quantity;
                }

            return counts;
        }

        private static int Count(Dictionary<int, int> bag, int id)
        {
            return bag.TryGetValue(id, out int n) ? n : 0;
        }

        private static string Skills(PlayerState p)
        {
            var b = new System.Text.StringBuilder();
            foreach (var s in p.Progress.Skills)
                b.Append(s[0]).Append(':').Append(s[1]).Append(';');
            return b.ToString();
        }

        private static int SkillScore(PlayerState p)
        {
            int total = 0;
            foreach (var s in p.Progress.Skills)
                total += s[1] + 1;
            return total;
        }

        private static string Menus(PlayerState p)
        {
            var b = new System.Text.StringBuilder();
            foreach (var e in p.Menus)
                b.Append(e.Kind).Append(':').Append(e.Name).Append(';');
            return b.ToString();
        }

        private static int Upgrades(PlayerState p)
        {
            int total = 0;
            foreach (var container in new[]
            {
                p.Inventory.Bag,
                p.Inventory.Box,
                p.Inventory.Equipment
            }

            )
                foreach (var item in container)
                    if (item != null)
                        total += item.Upgrade;
            return total;
        }

        private static bool Near(PlayerState p, int npc)
        {
            int distance = npc == 13 ? 49 : 120;
            foreach (var n in p.Zone.Npcs)
                if (n.TemplateId == npc && Math.Abs(n.X - p.X) <= distance && Math.Abs(n.Y - p.Y) <= distance)
                    return true;
            return false;
        }

        public static ActionReceipt Begin(NinjaAgent a, Message message, long now)
        {
            sbyte cmd = message.Command;
            if (!Observes(cmd))
                return null;
            var p = a.Me;
            if (p == null || p.IsDead || p.Inventory == null || p.Progress == null || p.Zone == null)
                return null;
            var r = new ActionReceipt
            {
                Agent = a,
                Command = cmd,
                Bag = BagCounts(p),
                Hp = p.Hp,
                Mp = p.Mp,
                MaxHp = p.MaxHp,
                MaxMp = p.MaxMp,
                Damage = p.Damage,
                ClassId = p.Record.ClassId,
                Zone = p.Zone.Id,
                Population = p.Zone.NumberChar,
                Task = p.Record.TaskId,
                Step = p.Progress.TaskIndex,
                Active = p.Progress.TaskActive,
                Skills = Skills(p),
                CatchUntil = p.CatchUntil,
                FishUntil = p.FishUntil,
                AutoPlay = p.IsAutoPlay,
                Map = p.MapId,
                X = p.X,
                Y = p.Y,
                Menu = Menus(p),
                Answer = p.NAnswer,
                BoxOpen = p.BoxOpen,
                FreeSlots = PlayerItems.SlotNull(p),
                UpgradeTotal = Upgrades(p),
                SkillCount = p.Progress.Skills.Count,
                SkillScore = SkillScore(p),
                FirstMenu = p.Menus.Count > 0 ? p.Menus[0] : null,
                Money = p.Progress.Yen + p.Progress.Xu
            };
            foreach (var e in p.Effects)
                if (!e.IsExpired(now))
                {
                    if (e.TemplateId == 21)
                        r.HpEffect = true;
                    if (e.Type == 0)
                        r.FoodEffect = true;
                }

            var reader = new JavaReader(message.Data ?? new byte[0]);
            try
            {
                if (cmd == 11 || cmd == 12)
                {
                    int index = reader.ReadByte();
                    if (index >= p.Inventory.Bag.Length || p.Inventory.Bag[index] == null)
                        return null;
                    var item = p.Inventory.Bag[index];
                    r.ItemInstance = item;
                    r.Item = item.Id;
                    r.Quantity = Count(r.Bag, item.Id);
                    var t = p.Session.Server.World.FindItem(item.Id);
                    if (t != null)
                        r.ItemType = t.Type;
                }
                else if (cmd == 13)
                {
                    int shop = reader.ReadSByte();
                    // These are the same village shop types used by PlayerTasks/AgentTactics.
                    r.Npc = shop == 6 || shop == 7 ? 3 : shop == 8 || shop == 9 ? 4 : -1;
                    if (r.Npc < 0 && Near(p, a.Learning.LastNpc))
                        r.Npc = a.Learning.LastNpc;
                    r.Feature = "purchase";
                }
                else if (cmd == 40)
                {
                    r.Npc = reader.ReadShort();
                    r.Feature = "discover";
                }
                else if (cmd == 29 || cmd == 47)
                {
                    if (cmd == 29)
                        reader.ReadSByte();
                    r.Npc = reader.ReadSByte();
                    int menu = reader.ReadSByte();
                    if (menu < 0 || menu >= p.Menus.Count)
                        return null;
                    r.Feature = "menu:" + p.Menus[menu].Kind + ":" + p.Menus[menu].Name;
                    if (a.Learning.PendingUseItem >= 0 && a.Learning.PendingItemMenu == r.Menu)
                    {
                        r.Item = a.Learning.PendingUseItem;
                        r.Quantity = Count(r.Bag, r.Item);
                        var t = p.Session.Server.World.FindItem(r.Item);
                        if (t != null)
                            r.ItemType = t.Type;
                    }
                }
                else if (cmd == 36 || cmd == 28)
                {
                    r.Npc = 13;
                    r.Feature = cmd == 36 ? "zone-list" : "zone-change";
                }
                else
                {
                    r.Npc = cmd == 16 || cmd == 17 ? 5 : 6;
                    r.Feature = "service:" + cmd;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return r;
        }

        private static bool NeededPurchase(ActionReceipt r, PlayerState p, GameWorld world, int id)
        {
            var t = world.FindItem(id);
            if (t == null || t.Level > p.Level)
                return false;
            int stock = 0;
            foreach (var kv in r.Bag)
            {
                var item = world.FindItem(kv.Key);
                if (item != null && item.Type == t.Type)
                    stock += kv.Value;
            }

            if (t.Type == PlayerItems.TypeHp)
                return stock < Math.Max(2, AgentConfig.PotionStock / 2);
            if (t.Type == PlayerItems.TypeMp)
                return p.SelectedSkill != null && p.SelectedSkill.ManaUse > 0 && stock < Math.Max(2, AgentConfig.PotionStock / 2);
            if (id == 23 && r.Task == 3 && r.Active && r.Step == 0)
                return true;
            return PlayerSkills.BookClass(world, id) == p.Record.ClassId && p.Record.ClassId > 0 && !r.Agent.Learning.ItemRewards.Used.Contains(id);
        }

        // what a use visibly did, for the ledger and the LLM's knowledge
        private static string Effect(ActionReceipt r, PlayerState p, bool quest, bool skill, bool hpEffect, bool foodEffect)
        {
            if (quest)
                return "advanced quest " + r.Task;
            if (skill)
                return "learned or raised a skill";
            if (r.ItemType == PlayerItems.TypeHp && hpEffect)
                return "HP back over 3 s";
            if (r.ItemType == PlayerItems.TypeMp && p.Mp > r.Mp)
                return "MP +" + (p.Mp - r.Mp);
            if (r.ItemType == PlayerItems.TypeEat && foodEffect)
                return "HP and MP back for 30 min";
            if (p.MapId != r.Map)
                return "moved to map " + p.MapId;
            if (p.X != r.X || p.Y != r.Y)
                return "moved the character";
            return "";
        }

        // One use of an item into the ledger; the verdict also goes into the character's knowledge (prompt and RAG),
        // so the LLM and the rules see what the item is good for the next time the situation comes up.
        private static void Ledger(NinjaAgent a, GameWorld world, int id, bool useful, string effect)
        {
            var h = a.Learning.ItemRewards;
            var n = h.Note(id);
            bool wasUseless = n.Useless;
            int wasUseful = n.Useful;
            n.Used++;
            if (useful)
            {
                n.Useful++;
                if (effect.Length > 0)
                    n.Effect = effect;
            }
            else
                n.Wasted++;
            h.Dirty = true;
            if (n.Used == 1 || (wasUseful == 0 && n.Useful > 0) || wasUseless != n.Useless)
            {
                var t = world.FindItem(id);
                try
                {
                    if (a.Knowledge != null)
                        a.Knowledge.Learn("ledger:" + id, n.Text(t != null ? t.Name : "item " + id));
                }
                catch (Exception)
                {
                }
            }
        }

        public static void Complete(ActionReceipt r, long now)
        {
            if (r == null || r.Agent.Me == null)
                return;
            var a = r.Agent;
            var p = a.Me;
            var world = p.Session.Server.World;
            var h = a.Learning.ItemRewards;
            var cfg = RlRuntime.Settings;
            var after = BagCounts(p);
            bool quest = p.Record.TaskId > r.Task || (p.Record.TaskId == r.Task && (p.Progress.TaskIndex > r.Step || (!r.Active && p.Progress.TaskActive)));
            bool skillChanged = r.Skills != Skills(p);
            bool skill = p.Progress.Skills.Count > r.SkillCount || SkillScore(p) > r.SkillScore;
            bool usefulNpc = false;
            if (r.Item >= 0)
            {
                bool equipped = false;
                foreach (var item in p.Inventory.Equipment)
                    if (item != null && ReferenceEquals(item, r.ItemInstance))
                        equipped = true;
                bool consumed = Count(after, r.Item) < r.Quantity;
                bool accepted = consumed || equipped || skillChanged || quest || p.CatchUntil > r.CatchUntil || p.FishUntil > r.FishUntil || (!r.AutoPlay && p.IsAutoPlay) || p.MapId != r.Map || p.X != r.X || p.Y != r.Y;
                bool novel = accepted && h.Used.Add(r.Item);
                if (novel)
                {
                    h.Dirty = true;
                    h.Award(0, cfg.NewItemUseReward);
                }

                bool hpEffect = p.Effects.Exists(e => e.TemplateId == 21 && e.Param > 0 && !e.IsExpired(now));
                bool foodEffect = p.Effects.Exists(e => e.Type == 0 && e.Param > 0 && !e.IsExpired(now));
                bool timely = consumed && ((r.ItemType == PlayerItems.TypeHp && !r.HpEffect && hpEffect && r.Hp * 100L <= r.MaxHp * cfg.UsefulHpPercent) || (r.ItemType == PlayerItems.TypeMp && p.Mp > r.Mp && r.Mp * 100L <= r.MaxMp * cfg.UsefulMpPercent) || (r.ItemType == PlayerItems.TypeEat && !r.FoodEffect && foodEffect && (r.Hp < r.MaxHp || r.Mp < r.MaxMp)));
                string objective = "item-objective:" + r.Item + ":" + r.Task + ":" + r.Step;
                bool objectiveUse = accepted && quest && h.Features.Add(objective);
                if (objectiveUse)
                    h.Dirty = true;
                timely |= accepted && (objectiveUse || (novel && (skill || (equipped && (p.Damage > r.Damage || p.MaxHp > r.MaxHp || p.MaxMp > r.MaxMp)))));
                if (equipped)
                    Ledger(a, world, r.Item, p.Damage > r.Damage || p.MaxHp > r.MaxHp || p.MaxMp > r.MaxMp, "stronger gear");
                else if (accepted)
                { // the ledger asks "does it do anything?" (any visible effect), the timely reward asks "was it the right moment?"
                    string effect = Effect(r, p, quest, skill, hpEffect, foodEffect);
                    Ledger(a, world, r.Item, timely || effect.Length > 0, effect);
                }

                if (timely && now - h.LastUsefulUse >= cfg.UsefulItemCooldownSeconds * 1000L)
                {
                    h.Award(2, cfg.TimelyItemUseReward);
                    h.LastUsefulUse = now;
                    h.Dirty = true;
                }

                if (accepted || p.Menus.Count == 0)
                {
                    a.Learning.PendingUseItem = -1;
                    a.Learning.PendingItemMenu = "";
                }
                else if ((r.Command == 11 && p.Menus.Count > 0 && !ReferenceEquals(r.FirstMenu, p.Menus[0])) || a.Learning.PendingUseItem == r.Item)
                {
                    a.Learning.PendingUseItem = r.Item;
                    a.Learning.PendingItemMenu = Menus(p);
                }
            }

            if (r.Command == 13)
                foreach (var kv in after)
                    if (kv.Value > Count(r.Bag, kv.Key))
                    {
                        int qty = kv.Value - Count(r.Bag, kv.Key);
                        // known useless (tried, never helped): the money is wasted; unknown: curiosity is rewarded once
                        if (h.Useless(kv.Key))
                            h.Award(5, -cfg.WastefulPurchasePenalty);
                        else if (h.Bought.Add(kv.Key))
                            h.Award(1, cfg.NewItemPurchaseReward);
                        var note = h.Note(kv.Key);
                        note.Bought += qty;
                        note.Spent += Math.Max(0, r.Money - (p.Progress.Yen + p.Progress.Xu));
                        h.Dirty = true;
                        if (NeededPurchase(r, p, world, kv.Key))
                            usefulNpc = true;
                    }

            bool near = r.Npc >= 0 && Near(p, r.Npc);
            if (r.Command == 40)
            {
                a.Learning.PendingUseItem = -1;
                a.Learning.PendingItemMenu = "";
            }

            bool discovered = (r.Command == 40 && p.Menus.Count > 0) || (r.Command == 36 && !p.Zone.Tile.IsNotChangeZone) || (r.Command == 13 && usefulNpc);
            if (near && discovered)
            {
                a.Learning.LastNpc = r.Npc;
                if (h.Npcs.Add(r.Npc))
                {
                    h.Dirty = true;
                    h.Award(3, cfg.NpcDiscoveryReward);
                }
            }

            usefulNpc |= r.Item < 0 && (r.Command == 29 || r.Command == 47) && (quest || skill || r.ClassId != p.Record.ClassId);
            usefulNpc |= r.Command == 47 && r.Item < 0 && p.NAnswer > r.Answer;
            usefulNpc |= r.Command == 29 && r.Item < 0 && !r.BoxOpen && p.BoxOpen && r.FreeSlots <= cfg.UsefulBagFreeSlots;
            usefulNpc |= r.Command == 28 && r.Zone != p.Zone.Id && p.Zone.NumberChar < r.Population;
            usefulNpc |= r.Command == 17 && r.BoxOpen && PlayerItems.SlotNull(p) > r.FreeSlots && r.FreeSlots <= cfg.UsefulBagFreeSlots;
            if (r.Command == 16 && r.BoxOpen)
                foreach (var kv in after)
                    if (kv.Value > Count(r.Bag, kv.Key) && NeededPurchase(r, p, world, kv.Key))
                        usefulNpc = true;
            if (r.Command >= 19 && r.Command <= 22)
                usefulNpc |= quest || Upgrades(p) > r.UpgradeTotal;
            string key = r.Npc + ":" + r.Feature + ":" + r.Task + ":" + r.Step + (r.Command == 47 ? ":quiz:" + r.Answer : "");
            if (near && usefulNpc && h.Features.Add(key))
            {
                h.Dirty = true;
                h.Award(4, cfg.UsefulNpcFeatureReward);
            }
        }
    }
}
