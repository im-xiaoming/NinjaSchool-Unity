using System;
using System.Collections.Generic;
using NsoServer.Net;

namespace NsoServer.Ai
{
    using Message = NsoServer.Net.Message; // the client has global Message / Cmd types too
    using Cmd = NsoServer.Net.Cmd;
    using Math = System.Math;

    /// <summary>
    /// The "game client" of an AI player: builds the same messages the Unity client sends (Service.java on the client
    /// side) and hands them to the server's Controller, which treats them exactly like a human player's input.
    /// Nothing here changes game state directly. Call under GameWorld.SyncRoot (the agent tick).
    /// </summary>
    public sealed class BotClient
    {
        private readonly Session session;
        private readonly BotMovement movement = new BotMovement();
        public BotClient(Session session)
        {
            this.session = session;
        }

        private void Send(sbyte cmd, Action<JavaWriter> body)
        {
            var m = new Message(cmd);
            if (body != null)
                body(m.Writer);
            byte[] data = m.GetData();
            if (!Rl.RlRuntime.Intercept(session, cmd, data))
            {
                if (cmd == Cmd.PLAYER_MOVE)
                {
                    var r = new JavaReader(data);
                    movement.Request(session.Controller.Actions != null ? session.Controller.Actions.State : null, r.ReadShort(), r.ReadShort(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
                else
                {
                    movement.Flush(this, session.Controller.Actions != null ? session.Controller.Actions.State : null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    session.Controller.OnMessage(new Message(cmd, data));
                }
            }
        }

        private void Sub(sbyte cmd, sbyte sub, Action<JavaWriter> body)
        {
            Send(cmd, w =>
            {
                w.WriteSByte(sub);
                if (body != null)
                    body(w);
            });
        }

        // ---- log in (Service.setClientType, Service.clientOk, Service.createChar, Service.selectChar)
        public void ClientInfo()
        {
            Sub(Cmd.NOT_LOGIN, Cmd.CLIENT_INFO, w =>
            {
                w.WriteByte(4);
                w.WriteByte(1);
            });
        } // type, zoom 1

        public void ClientOk()
        {
            Sub(Cmd.NOT_MAP, Cmd.CLIENT_OK, null);
        }

        public void CreateChar(string name, int gender, int head)
        {
            Sub(Cmd.NOT_MAP, Cmd.CREATE_PLAYER, w =>
            {
                w.WriteUTF(name);
                w.WriteByte(gender);
                w.WriteByte(head);
            });
        }

        public void SelectChar(string name)
        {
            Sub(Cmd.NOT_MAP, Cmd.SELECT_PLAYER, w => w.WriteUTF(name));
        }

        // ---- moving (Service.charMove, Service.requestChangeMap, Service.returnTownFromDead)
        public void Move(int x, int y)
        {
            Send(Cmd.PLAYER_MOVE, w =>
            {
                w.WriteShort(x);
                w.WriteShort(y);
            });
        }

        // A policy chooses one destination per 500 ms; these client frames execute it through normal validation.
        // Do not intercept frames as new PPO decisions or recursively renew the destination.
        internal void StepMovement(long now)
        {
            movement.Step(this, session.Controller.Actions != null ? session.Controller.Actions.State : null, session.Server.World, now);
        }

        internal void ReleaseMovement()
        {
            movement.Release();
        }

        /// <summary>The movement planner found no way to (x, y) (y &lt; 0 = any height).</summary>
        internal bool CannotReach(int x, int y)
        {
            return movement.CannotReach(x, y);
        }

        internal void SendMovementFrame(int x, int y)
        {
            var m = new Message(Cmd.PLAYER_MOVE);
            m.Writer.WriteShort(x);
            m.Writer.WriteShort(y);
            session.Controller.OnMessage(new Message(Cmd.PLAYER_MOVE, m.GetData()));
        }

        public void ChangeMap()
        {
            Observed("map", "walked into the exit", () => Send(Cmd.MAP_CHANGE, null), null);
        }

        public void ReturnTown()
        {
            Observed("town", "returned to the village", () => Send(Cmd.ME_BACK, null), null);
        }

        // Service.openUIZone / requestChangeZone: the bot approaches the same NPC before opening the list.
        public void OpenZones()
        {
            Send(Cmd.OPEN_UI_ZONE, null);
        }

        public void ChangeZone(int zoneId)
        {
            Observed("zone", "asked to change to zone " + zoneId, () => Send(Cmd.ZONE_CHANGE, w =>
            {
                w.WriteByte(zoneId);
                w.WriteSByte(-1);
            }), null);
        }

        // ---- fighting (Service.selectSkill cmd 41, Service.sendPlayerAttack cmd 60)
        public void SelectSkill(int templateId)
        {
            Send(41, w => w.WriteShort(templateId));
        }

        public void Attack(int mobId)
        {
            Send(60, w => w.WriteByte(mobId));
        }

        // ---- items and shops (Service.pickItem -14, useItem 11, buyItem 13, requestItem sub -103)
        public void Pick(int itemMapId)
        {
            var me = Me;
            int map = me != null ? me.MapId : -1;
            Observed("pick", "picked up from the ground", () => Send(-14, w => w.WriteShort(itemMapId)), (p, outcome) =>
            {
                // where loot was found: "how to get" knowledge shared by every AI player
                if (outcome.Contains("+"))
                    AgentRag.Remember("*", "seen:loot:" + map, "observed", "Observed in play: picked up in map " + map + " " + (p.Zone != null && p.MapId == map ? p.Zone.Tile.Name : "") + " -> " + outcome);
            });
        }

        public void UseItem(int bagIndex)
        {
            // debug view (/agents.html): which consumable (HP/MP potion, food, ...) the AI player used last
            var me = Me;
            var it = me != null && bagIndex >= 0 && bagIndex < me.Inventory.Bag.Length ? me.Inventory.Bag[bagIndex] : null;
            var t = it != null ? session.Server.World.FindItem(it.Id) : null;
            if (it != null && (t == null || t.Type >= PlayerItems.TypeHp)) // types below 16 are equipment (worn, not consumed)
            {
                LastUsedItemId = it.Id;
                LastUsedItemName = t != null ? t.Name : "#" + it.Id;
                LastUsedItemAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                UsedItems++;
            }

            if (it == null)
            {
                Send(11, w => w.WriteByte(bagIndex));
                return;
            }

            string name = t != null ? t.Name : "#" + it.Id;
            int id = it.Id;
            Observed("use " + name, "used " + name, () => Send(11, w => w.WriteByte(bagIndex)), (p, outcome) =>
            {
                // what using it really does, for this AI player and for every AI player (shared memory)
                Agent.Knowledge.Learn("item:" + id, "Using " + name + " at level " + p.Level + ": " + outcome);
                AgentRag.Remember("*", "seen:item:" + id, "observed", "Observed in play: using " + name + " (id " + id + ") at level " + p.Level + " -> " + outcome);
            });
        }

        /// <summary>Last consumable the AI player used (debug view only; game rules never read it).</summary>
        public int LastUsedItemId = -1, UsedItems;
        public string LastUsedItemName = "";
        public long LastUsedItemAt;
        // ---- interactions reported to the AI player's memory (and so to the LLM) -------------------------------------
        // Every interaction with the world (item use, purchase, pickup, NPC menu, quest talk, map/zone change) is followed
        // by what visibly changed (HP/MP, money, bag, effects, equipment, map, quest, menus, server notice). The line goes
        // to the AI's memory (prompt + RAG events + player log); what it teaches goes to its knowledge and the shared
        // "observed" knowledge, so the LLM sees new facts at its next decision. Offline addition, no Java counterpart.
        /// <summary>The AI player this client belongs to (null: no reporting, e.g. a test client).</summary>
        public NinjaAgent Agent;
        /// <summary>Seconds before the same interaction (same key) is reported again (potions in a fight, ...).</summary>
        public static int ReportRepeatSeconds = 60;
        private readonly Dictionary<string, long> reportedAt = new Dictionary<string, long>();
        private PlayerState Me
        {
            get
            {
                return session.Controller.Actions != null ? session.Controller.Actions.State : null;
            }
        }

        private sealed class Before
        {
            public int Hp, Mp, Map, Task, TaskIndex, TaskCount, Skills;
            public long Yen, Xu;
            public string Notice, Menu;
            public Dictionary<int, int> Bag = new Dictionary<int, int>();
            public HashSet<int> Effects = new HashSet<int>(), Equipment = new HashSet<int>();
        }

        private Before Capture(PlayerState me)
        {
            var b = new Before
            {
                Hp = me.Hp,
                Mp = me.Mp,
                Map = me.MapId,
                Task = me.Record.TaskId,
                TaskIndex = me.Progress.TaskIndex,
                TaskCount = me.Progress.TaskCount,
                Skills = me.Progress.Skills.Count,
                Yen = me.Progress.Yen,
                Xu = me.Progress.Xu,
                Notice = session.LastNotice ?? "",
                Menu = Menus(me)
            };
            foreach (var it in me.Inventory.Bag)
                if (it != null)
                {
                    int n;
                    b.Bag.TryGetValue(it.Id, out n);
                    b.Bag[it.Id] = n + Math.Max(1, it.Quantity);
                }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var e in me.Effects)
                if (!e.IsExpired(now))
                    b.Effects.Add(e.TemplateId);
            foreach (var it in me.Inventory.Equipment)
                if (it != null)
                    b.Equipment.Add(it.Id);
            return b;
        }

        private static string Menus(PlayerState me)
        {
            var names = new List<string>();
            foreach (var m in me.Menus)
                names.Add(m.Name);
            return string.Join("; ", names);
        }

        // what changed between before and now, in words
        private string Outcome(Before b, PlayerState me)
        {
            var world = session.Server.World;
            var parts = new List<string>();
            if (me.Hp != b.Hp)
                parts.Add("HP " + (me.Hp > b.Hp ? "+" : "") + (me.Hp - b.Hp));
            if (me.Mp != b.Mp)
                parts.Add("MP " + (me.Mp > b.Mp ? "+" : "") + (me.Mp - b.Mp));
            if (me.Progress.Yen != b.Yen)
                parts.Add("yen " + (me.Progress.Yen > b.Yen ? "+" : "") + (me.Progress.Yen - b.Yen));
            if (me.Progress.Xu != b.Xu)
                parts.Add("xu " + (me.Progress.Xu > b.Xu ? "+" : "") + (me.Progress.Xu - b.Xu));
            var now = Capture(me);
            foreach (var kv in now.Bag)
            {
                int was;
                b.Bag.TryGetValue(kv.Key, out was);
                if (kv.Value != was)
                    parts.Add((kv.Value > was ? "+" : "") + (kv.Value - was) + " " + ItemName(world, kv.Key));
            }

            foreach (var kv in b.Bag)
                if (!now.Bag.ContainsKey(kv.Key))
                    parts.Add("-" + kv.Value + " " + ItemName(world, kv.Key));
            long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var e in me.Effects)
                if (!e.IsExpired(ms) && !b.Effects.Contains(e.TemplateId))
                    parts.Add("effect " + world.EffectName(e.TemplateId) + " for " + Math.Max(1, (e.EndAt - ms) / 1000) + " s");
            foreach (int id in now.Equipment)
                if (!b.Equipment.Contains(id))
                    parts.Add("now wearing " + ItemName(world, id));
            if (now.Skills > b.Skills)
                parts.Add("learned a new skill");
            if (me.MapId != b.Map)
                parts.Add("now in map " + me.MapId + " " + (me.Zone != null ? me.Zone.Tile.Name : ""));
            if (me.Record.TaskId != b.Task || me.Progress.TaskIndex != b.TaskIndex || me.Progress.TaskCount != b.TaskCount)
                parts.Add("quest " + me.Record.TaskId + " step " + (me.Progress.TaskIndex + 1) + " count " + me.Progress.TaskCount);
            if (now.Menu != b.Menu && now.Menu.Length > 0)
                parts.Add("menu shows: " + now.Menu);
            string notice = session.LastNotice ?? "";
            if (notice != b.Notice && notice.Length > 0)
                parts.Add("server says \"" + notice + "\"");
            return parts.Count > 0 ? string.Join(", ", parts) : "no visible effect";
        }

        private static string ItemName(NsoServer.World.GameWorld world, int id)
        {
            var t = world.FindItem(id);
            return t != null ? t.Name : "item " + id;
        }

        // Runs the command and reports it with its outcome (at most once per ReportRepeatSeconds for the same key).
        private void Observed(string key, string action, Action command, Action<PlayerState, string> learn, bool memory = true)
        {
            var me = Me;
            if (Agent == null || me == null)
            {
                command();
                return;
            }

            var before = Capture(me);
            command();
            me = Me;
            if (me == null)
                return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long last;
            if (reportedAt.TryGetValue(key, out last) && now - last < ReportRepeatSeconds * 1000L)
                return;
            reportedAt[key] = now;
            if (reportedAt.Count > 512)
                reportedAt.Clear();
            string outcome = Outcome(before, me);
            try
            {
                if (memory)
                    AgentManager.Remember(Agent, action + " -> " + outcome);
                if (learn != null)
                    learn(me, outcome);
            }
            catch (Exception)
            {
            } // reporting never breaks the action itself
        }

        public void BodyToBag(int equipIndex)
        {
            Send(15, w => w.WriteByte(equipIndex));
        }

        // Char.upgradeItem (cmd 21) at Kenshinto: not careful (no luong), the bag index, then the crystal bag indexes
        public void Upgrade(int bagIndex, List<int> crystals)
        {
            Send(21, w =>
            {
                w.WriteBoolean(false);
                w.WriteByte(bagIndex);
                foreach (int c in crystals)
                    w.WriteByte(c);
            });
        }

        // Char.useItemChangeMap (cmd 12): Khả di lệnh / Vô hạn khả di lệnh, bag index then the destination menu index
        public void UseItemChangeMap(int bagIndex, int menuIndex)
        {
            Send(12, w =>
            {
                w.WriteByte(bagIndex);
                w.WriteByte(menuIndex);
            });
        }

        // Char.saleItem (cmd 14): bag index then an int quantity; sold anywhere, like the human shop "Bán" tab
        public void Sell(int bagIndex, int quantity)
        {
            Send(14, w =>
            {
                w.WriteByte(bagIndex);
                w.WriteInt(quantity);
            });
        }

        public void RequestShop(int type)
        {
            Sub(Cmd.SUB_COMMAND, -103, w => w.WriteByte(type));
        }

        public void Buy(int type, int index, int quantity)
        {
            var store = session.Server.World.Store(type);
            string item = store != null && index >= 0 && index < store.Count ? ItemName(session.Server.World, store[index].ItemId) : "item " + index;
            Observed("buy:" + type + ":" + index, "bought " + quantity + " " + item + " (shop " + type + ")", () => Send(13, w =>
            {
                w.WriteByte(type);
                w.WriteByte(index);
                w.WriteShort(quantity);
            }), (p, outcome) => AgentRag.Remember("*", "seen:buy:" + type + ":" + index, "observed", "Observed in play: buying " + quantity + " " + item + " in shop " + type + " at level " + p.Level + " -> " + outcome), false); // the shop step already reports the purchase (AgentEvent.Shop)
        }

        // ---- NPCs and quests (Service.openMenu cmd 40, Service.menu cmd 29, Service.getTask cmd 47)
        public void OpenMenu(int npcId)
        {
            string npc = AgentTactics.NpcName(session.Server.World, npcId);
            Observed("npc:" + npcId, "talked to " + npc, () => Send(40, w => w.WriteShort(npcId)), (p, outcome) =>
            {
                Agent.Knowledge.Learn("npc:" + npcId + ":" + p.MapId, npc + " in map " + p.MapId + ": " + outcome);
                AgentRag.Remember("*", "seen:npc:" + npcId + ":" + p.MapId, "observed", "Observed in play: talking to " + npc + " in map " + p.MapId + " at level " + p.Level + " (quest " + p.Record.TaskId + ") -> " + outcome);
            });
        }

        public void Menu(int npcId, int menuId)
        {
            var me = Me;
            string entry = me != null && menuId >= 0 && menuId < me.Menus.Count ? me.Menus[menuId].Name : "entry " + menuId;
            string npc = AgentTactics.NpcName(session.Server.World, npcId);
            Observed("menu:" + npcId + ":" + entry, "chose \"" + entry + "\" at " + npc, () => Send(29, w =>
            {
                w.WriteByte(0);
                w.WriteByte(npcId);
                w.WriteByte(menuId);
                w.WriteByte(0);
            }), (p, outcome) => AgentRag.Remember("*", "seen:menu:" + npcId + ":" + entry, "observed", "Observed in play: choosing \"" + entry + "\" at " + npc + " (level " + p.Level + ", quest " + p.Record.TaskId + ") -> " + outcome));
        }

        public void GetTask(int npcId, int menuId)
        {
            string npc = AgentTactics.NpcName(session.Server.World, npcId);
            Observed("task:" + npcId + ":" + menuId, "asked " + npc + " about the quest", () => Send(47, w =>
            {
                w.WriteByte(npcId);
                w.WriteByte(menuId);
            }), null);
        }

        // ---- points (Service.upPotential sub -109, Service.upSkill sub -108)
        public void UpPotential(int index, int points)
        {
            Sub(Cmd.SUB_COMMAND, -109, w =>
            {
                w.WriteByte(index);
                w.WriteShort(points);
            });
        }

        public void UpSkill(int templateId, int points)
        {
            Sub(Cmd.SUB_COMMAND, -108, w =>
            {
                w.WriteShort(templateId);
                w.WriteByte(points);
            });
        }
    }
}
