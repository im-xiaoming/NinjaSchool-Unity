using System;
using System.Collections.Generic;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class
    using Zone = NsoServer.World.Zone;

    /// <summary>
    /// NV Đưa Jaian trở về (quest 17, step 1): escort Jaian out of Rừng Moshio.
    /// Port of Char.initMenu (NpcName.JAIAN "Đi nào"), Service.mapInfo (Jaian's NPC status), bot/move/JaianMove
    /// and Char.escortFailed. Jaian is a fake character (Bot) sent with PLAYER_ADD that walks 36 px to the left
    /// every 3 s; when he walks off the left edge the step is done.
    /// Monsters notice and hit Jaian like a player (MobBrain, Mob.update / Mob.attack); at 0 HP the escort fails.
    /// Offline deviation: a failure shows the client's own escort message (SUB_COMMAND -90).
    /// </summary>
    public static class Escort
    {
        public const int TaskDuaJaian = 17;   // TaskName.NV_DUA_JAIAN_TRO_VE
        public const int NpcJaian = 17;       // NpcName.JAIAN
        private const short Head = 96, Body = 94, Leg = 95;   // npc table: (17, 'Jaian', 96, 94, 95)

        /// <summary>JaianMove: milliseconds between two steps.</summary>
        public static int StepMs = 3000;
        /// <summary>JaianMove: pixels walked per step (to the left).</summary>
        public static int StepPx = 36;
        /// <summary>JaianMove: the escort fails when the player is this far (px) from Jaian.</summary>
        public static int MaxDistance = 500;

        /// <summary>AbilityCustom.hp(2000): Jaian's HP.</summary>
        public static int JaianHp = 2000;

        /// <summary>The escorted bot (Char.escorted). Read by MobBrain so monsters notice and hit Jaian.</summary>
        public sealed class State
        {
            public PlayerState Player;
            public Zone Zone;
            public int Id, NpcIndex, Hp, MaxHp, Level = 30;
            public short X, Y, LineY;   // LineY: the NPC's cy, the height JaianMove looks for the floor from
            public long LastMove;
            public bool IsDead;
        }

        private static readonly Dictionary<PlayerState, State> Active = new Dictionary<PlayerState, State>();

        /// <summary>The escorted bot in this zone with this char id, or null (Mob.chars holds char ids).</summary>
        public static State FindBot(Zone zone, int id)
        {
            if (Active.Count == 0) return null;
            foreach (var st in Active.Values) if (st.Zone == zone && st.Id == id && !st.IsDead) return st;
            return null;
        }

        /// <summary>Every live escorted bot in the zone (Zone.getChars includes bots).</summary>
        public static void BotsIn(Zone zone, List<State> into)
        {
            into.Clear();
            if (Active.Count == 0) return;
            foreach (var st in Active.Values) if (st.Zone == zone && !st.IsDead) into.Add(st);
        }

        /// <summary>Mob.attack(Char pl, Char owner) + attack(pl, dameHp, dameMp) on the bot: the HP is already the final
        /// damage (-1 = miss). Every player in the zone sees NPC_ATTACK_PLAYER; at 0 HP the bot dies (Char.startDie)
        /// and JaianMove fails the escort on the next tick.</summary>
        public static void Hit(Zone zone, int mobId, State st, int dameHp)
        {
            if (st.IsDead) return;
            if (dameHp > 0) st.Hp = Math.Max(0, st.Hp - dameHp);
            foreach (PlayerState p in zone.Players)
            {
                var all = new Message(Cmd.NPC_ATTACK_PLAYER);
                all.Writer.WriteByte(mobId);
                all.Writer.WriteInt(st.Id);
                all.Writer.WriteInt(st.Hp);
                all.Writer.WriteInt(0);
                all.Writer.WriteShort(-1);
                all.Writer.WriteByte(0);
                all.Writer.WriteByte(0);
                p.Session.Send(all);
            }
            if (st.Hp <= 0)
            {
                st.IsDead = true;
                var die = new Message(Cmd.PLAYER_DIE);   // MapService.waitToDie
                die.Writer.WriteInt(st.Id);
                die.Writer.WriteByte(0);
                die.Writer.WriteShort(st.X);
                die.Writer.WriteShort(st.Y);
                PlayerService.SendZone(zone, die);
            }
        }

        private static int BotId(PlayerState me) { return -me.Record.Id; }   // Bot.builder().id(-this.id)

        private static bool OnStep(PlayerState me)
        {
            return me.Record != null && me.Progress != null && me.Record.TaskId == TaskDuaJaian && me.Progress.TaskActive && me.Progress.TaskIndex == 1;
        }

        /// <summary>Char.isLeading: the player is escorting Jaian.</summary>
        public static bool IsLeading(PlayerState me) { return Active.ContainsKey(me); }

        /// <summary>Service.mapInfo: Jaian (status 15 = hidden in the map data) shows for a player on quest 17 step 1.
        /// Returns the NPC's status otherwise unchanged.</summary>
        public static int NpcStatus(PlayerState me, NpcSpawn n, int status)
        {
            if (n.TemplateId == NpcJaian && me != null && OnStep(me) && !IsLeading(me)) return 1;
            return status;
        }

        /// <summary>Char.initMenu, NpcName.JAIAN: his plea and "Đi nào" (starts the escort on quest 17 step 1).</summary>
        public static void InitMenu(PlayerState me, Action<string, Action> add)
        {
            if (IsLeading(me)) return;
            PlayerService.NpcChat(me, NpcJaian, "Xin chào, ta là người dân ở làng Chài, trên dường đi đã bị bọn quái thú tấn công, xin hãy giúp ta tìm đường quay trở về.");
            add("Đi nào", () => Start(me));
        }

        // the "Đi nào" lambda of Char.initMenu
        private static void Start(PlayerState me)
        {
            if (!OnStep(me) || IsLeading(me) || me.Zone == null) return;
            var zone = me.Zone;
            int index = -1;
            for (int i = 0; i < zone.Npcs.Count; i++) if (zone.Npcs[i].TemplateId == NpcJaian) { index = i; break; }
            if (index == -1) return;
            var npc = zone.Npcs[index];
            var st = new State { Player = me, Zone = zone, Id = BotId(me), NpcIndex = index, Hp = JaianHp, MaxHp = JaianHp,
                X = (short)npc.X, Y = (short)npc.Y, LineY = (short)npc.Y, LastMove = Now() };
            Active[me] = st;
            NpcUpdate(me, index, 15);   // getService().npcUpdate(npc.id, 15): the NPC hides, the bot takes its place
            // Bot.builder().level(30).typePk(PK_NORMAL) + AbilityCustom.hp(2000) + FashionCustom(npc head/body/leg)
            me.Session.Send(Gymnasium.FakePlayerAdd(st.Id, "Jaian", 0, Head, Body, Leg, st.Hp, st.MaxHp, st.Level, st.X, st.Y));
        }

        /// <summary>Failure reasons of the client's escort message (SUB_COMMAND -90): 1 too far, 2 Jaian injured, 0 none.</summary>
        public const int FailFar = 1, FailInjured = 2, FailOther = 0;

        /// <summary>Char.escortFailed: the bot leaves and Jaian shows again as an NPC.
        /// Offline: the client's own escort message (SUB_COMMAND -90, mResources.PROTECT_FAR / PROTECT_INJURE, which
        /// also turns the hidden NPC back to status 1) tells the player why; Java sent npcUpdate(npc.id, 1) silently.</summary>
        public static void Fail(PlayerState me, bool inZone, int reason)
        {
            State st;
            if (!Active.TryGetValue(me, out st)) return;
            Active.Remove(me);
            if (!inZone) return;   // the map was cleared, nothing to remove
            RemoveBot(me);
            NpcUpdate(me, st.NpcIndex, 1);
            var m = PlayerService.Sub(-90);
            m.Writer.WriteByte(reason);
            me.Session.Send(m);
        }

        /// <summary>bot/move/JaianMove.move, run from the zone tick for each player (every 500 ms).</summary>
        public static void UpdatePlayer(PlayerState me, GameWorld world, long now)
        {
            State st;
            if (!Active.TryGetValue(me, out st)) return;
            if (me.Zone != st.Zone) { Fail(me, false, FailOther); return; }   // Char.outZone -> escortFailed (the map was cleared)
            if (st.IsDead) { Fail(me, true, FailInjured); return; }           // JaianMove: owner.isDead
            if (me.IsDead) { Fail(me, true, FailOther); return; }             // Char.startDie -> escortFailed
            int dx = me.X - st.X, dy = me.Y - st.Y;
            if ((int)Math.Sqrt(dx * dx + dy * dy) >= MaxDistance) { Fail(me, true, FailFar); return; }
            if (now - st.LastMove < StepMs) return;
            st.LastMove = now;
            int x = st.X - StepPx;
            if (x < 0)
            {
                // owner.outZone(); escort.taskNext()
                Active.Remove(me);
                RemoveBot(me);
                PlayerTasks.TaskNext(me);
                return;
            }
            st.X = (short)x;
            var grid = world.GetGrid(st.Zone.Tile);
            if (grid != null) st.Y = grid.CollisionY(st.X, st.LineY);   // owner.zone.tilemap.collisionY(owner.x, y)
            var m = new Message(Cmd.PLAYER_MOVE);
            m.Writer.WriteInt(BotId(me));
            m.Writer.WriteShort(st.X);
            m.Writer.WriteShort(st.Y);
            me.Session.Send(m);
        }

        private static void RemoveBot(PlayerState me)
        {
            var rm = new Message(Cmd.PLAYER_REMOVE);
            rm.Writer.WriteInt(BotId(me));
            me.Session.Send(rm);
        }

        // MapService.npcUpdate (NPC_PLAYER_UPDATE -94): npc index, status
        private static void NpcUpdate(PlayerState me, int index, int status)
        {
            var up = PlayerService.Sub(-94);
            up.Writer.WriteByte(index);
            up.Writer.WriteByte(status);
            me.Session.Send(up);
        }

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
    }
}
