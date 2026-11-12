using System;
using System.Collections.Generic;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class
    using Zone = NsoServer.World.Zone;

    /// <summary>
    /// NV Thách đấu (quest 13): a private "phòng thi đấu" where a headmaster fights the player.
    /// Port of Java map/zones/Gymnasium (+ Haruna/Hirosaki/OokazaGymnasium), AreaWithCountdownTime, bot/Bot,
    /// bot/attack/AttackAround and bot/move/MoveWithinCustom (MoveToTarget), with the player-vs-player parts of
    /// Char.attackCharacter that the fight needs. The headmaster is a fake character (Bot) sent with PLAYER_ADD.
    /// </summary>
    public static class Gymnasium
    {
        public const int TaskThachDau = 13;
        public const int NpcToyotomi = 9, NpcOokamesama = 10, NpcKazeto = 11;
        public const int MapHaruna = 0, MapOokaza = 56, MapHirosaki = 73;   // MapName.NHA_THI_DAU_*

        /// <summary>Time limit of the room in seconds (Gymnasium: countDown = 300).</summary>
        public static int RoomSeconds = 300;
        /// <summary>Where the player appears in the room (Char: setXY(55, 264)).</summary>
        public static short EnterX = 55, EnterY = 264;

        private const int BotId = -11111;
        private static readonly Random Rng = new Random();

        // one headmaster (Bot.builder + AbilityCustom + FashionCustom + MoveWithinCustom + AttackAround)
        private sealed class Bot
        {
            public string Name;
            public short Head, Body, Leg;
            public int Hp = 1000, MaxHp = 1000, Mp = 1000, MaxMp = 1000;
            public int Damage = 1000, Damage2 = 900, Miss = 10, Exactly = 100, Fatal = 100, Level = 50;
            public short X, Y;
            public int MinX, MaxX, MinY = 100, MaxY = 240;
            public SkillData[] Skills;
            public long[] LastUse;
            public bool IsDead;
        }

        private sealed class Room
        {
            public Bot Bot;
            public PlayerState Player;
            public int Npc, BackMap;
            public bool Won, Seen;   // Seen: AttackAround found its target (within 100 px once)
        }

        private static readonly Dictionary<Zone, Room> Rooms = new Dictionary<Zone, Room>();

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        private static int NextInt(int min, int max) { return max <= min ? min : Rng.Next(min, max); }

        /// <summary>The school a gym map belongs to (Haruna 27, Ookaza 72, Hirosaki 1); 22 (Lang Tone) for other maps.</summary>
        public static int SchoolOf(int mapId) { return mapId == MapHaruna ? 27 : mapId == MapOokaza ? 72 : mapId == MapHirosaki ? 1 : 22; }

        public static bool IsGym(Zone zone) { return zone != null && Rooms.ContainsKey(zone); }

        // Char.initMenu "Rời khỏi nơi này": back to mapBeforeEnterPB at NinjaUtils.getXY
        public static void Leave(PlayerState me, GameWorld world)
        {
            int back = Rooms.TryGetValue(me.Zone, out Room room) ? room.BackMap : SchoolOf(me.MapId);
            var xy = PlayerTasks.VillageXY(back);
            me.X = xy[0]; me.Y = xy[1];
            me.Actions.ChangeMap(back);
        }

        /// <summary>NPC status sent with the map: 15 (hidden) for the headmaster while its bot is alive
        /// (Gymnasium.setWin sets it back with npc.setStatus(1)).</summary>
        public static int NpcStatus(Zone zone, NpcSpawn n)
        {
            Room room;
            if (zone != null && Rooms.TryGetValue(zone, out room) && !room.Bot.IsDead && n.TemplateId == room.Npc) return 15;
            return n.Status;
        }

        /// <summary>The headmaster of each step: 1 Ookamesama, 2 Kazeto, 3 Toyotomi (quest 13 sub names).</summary>
        public static int StepOf(int npc) { return npc == NpcOokamesama ? 1 : npc == NpcKazeto ? 2 : npc == NpcToyotomi ? 3 : -1; }

        /// <summary>Char menu "Vào phòng thi đấu" (THAY_OOKAMESAMA / THAY_KAZETO / CO_TOYOTOMI, NV_THACH_DAU).</summary>
        public static void Enter(PlayerState me, GameWorld world, int npc)
        {
            int mapId = npc == NpcKazeto ? MapHaruna : npc == NpcToyotomi ? MapHirosaki : MapOokaza;
            var zone = world.CreateInstance(mapId);
            if (zone == null) { PlayerService.ServerDialog(me, "Không tìm thấy nơi này!"); return; }
            zone.InstanceEndAt = Now() + RoomSeconds * 1000L;
            var room = new Room { Bot = NewBot(world, npc), Player = me, Npc = npc, BackMap = me.Zone.Tile.Id };
            Rooms[zone] = room;
            me.X = EnterX; me.Y = EnterY;
            me.Actions.JoinZone(zone);
            SendTimeInMap(me, RoomSeconds);    // AreaWithCountdownTime.join
            me.Session.Send(PlayerAdd(room.Bot)); // Zone.loadMap: the other characters of the zone
        }

        // HirosakiGymnasium / HarunaGymnasium / OokazaGymnasium.initBot
        private static Bot NewBot(GameWorld world, int npc)
        {
            Bot b;
            int[] skills;
            if (npc == NpcToyotomi)
            {
                b = new Bot { Name = "Cô Toyotomi", Head = 44, Body = 45, Leg = 46, MinX = 744, MaxX = 1176, X = 1000, Y = 240 };
                skills = new[] { 9, 18 };    // CHIEU_CHOUKOUHIRENZO, CHIEU_CHOUKOU_SHURIKEN
            }
            else if (npc == NpcKazeto)
            {
                b = new Bot { Name = "Thầy Kazeto", Head = 65, Body = 66, Leg = 67, MinX = 792, MaxX = 1176, X = 1000, Y = 240 };
                skills = new[] { 45, 54 };   // CHIEU_RAIKOUTO, CHIEU_TATSUMAKI
            }
            else
            {
                b = new Bot { Name = "Thầy Ookamesama", Head = 53, Body = 54, Leg = 55, MinX = 696, MaxX = 1176, X = 900, Y = 240 };
                skills = new[] { 27, 36 };   // CHIEU_CHOUKOUKOGO, CHIEU_CHOUSOUKINKINSA
            }
            var list = new List<SkillData>();
            foreach (int t in skills)
            {
                var s = world.FindSkill(t, 1);   // SkillFactory.newSkill(id, 1)
                if (s != null) list.Add(s);
            }
            b.Skills = list.ToArray();
            b.LastUse = new long[b.Skills.Length];
            return b;
        }

        // ---------------------------------------------------------------- messages

        // AbsService.playerAdd + charInfo
        private static Message PlayerAdd(Bot b)
        {
            return FakePlayerAdd(BotId, b.Name, 3, b.Head, b.Body, b.Leg, b.Hp, b.MaxHp, b.Level, b.X, b.Y);   // typePk = PK_DOSAT
        }

        /// <summary>AbsService.playerAdd + charInfo for a bot (fake character): no clan, no weapon, no effects, no fashion.
        /// Also used by the Jaian escort (Escort.cs).</summary>
        public static Message FakePlayerAdd(int id, string name, int typePk, short head, short body, short leg, int hp, int maxHp, int level, short x, short y)
        {
            var b = new { Name = name, Head = head, Body = body, Leg = leg, Hp = hp, MaxHp = maxHp, Level = level, X = x, Y = y };
            var m = new Message(Cmd.PLAYER_ADD);
            var w = m.Writer;
            w.WriteInt(id);
            w.WriteUTF("");                // no clan
            w.WriteBoolean(false);         // invisible
            w.WriteByte(typePk);
            w.WriteByte(0);                // class
            w.WriteByte(0);                // gender
            w.WriteShort(b.Head);
            w.WriteUTF(b.Name);
            w.WriteInt(b.Hp);
            w.WriteInt(b.MaxHp);
            w.WriteByte(b.Level);
            w.WriteShort(-1);              // weapon
            w.WriteShort(b.Body);
            w.WriteShort(b.Leg);
            w.WriteByte(-1);               // mobMe
            w.WriteShort(b.X);
            w.WriteShort(b.Y);
            w.WriteShort(0);               // eff5buffhp
            w.WriteShort(0);               // eff5buffmp
            w.WriteByte(0);                // effects
            w.WriteBoolean(true);          // isHuman
            w.WriteBoolean(false);         // isNhanBan
            w.WriteShort(b.Head);
            w.WriteShort(-1);
            w.WriteShort(b.Body);
            w.WriteShort(b.Leg);
            for (int i = 0; i < 10; i++) w.WriteShort(-1);   // fashion
            return m;
        }

        private static void SendTimeInMap(PlayerState me, int seconds)
        {
            var m = PlayerService.Sub(-95);   // MAP_TIME
            m.Writer.WriteInt(seconds);
            me.Session.Send(m);
        }

        // MapService.setSkillPaint_2: attacker, skill template, targets
        private static void SkillPaint(Zone zone, int attackerId, int skillTemplateId, int targetId)
        {
            var m = new Message(Cmd.PLAYER_ATTACK_PLAYER);
            m.Writer.WriteInt(attackerId);
            m.Writer.WriteByte(skillTemplateId);
            m.Writer.WriteInt(targetId);
            PlayerService.SendZone(zone, m);
        }

        // MapService.attackCharacter (HAVE_ATTACK_PLAYER). Offline: the HP sent is the HP after the hit (the client
        // shows it as the new HP; Java sent it before subtracting).
        private static void HaveAttack(Zone zone, int id, int hp, int damage, int mp)
        {
            var m = new Message(Cmd.HAVE_ATTACK_PLAYER);
            m.Writer.WriteInt(id);
            m.Writer.WriteInt(hp);
            m.Writer.WriteInt(damage);
            m.Writer.WriteInt(mp);
            m.Writer.WriteInt(0);
            PlayerService.SendZone(zone, m);
        }

        private static void PlayerMove(Zone zone, Bot b)
        {
            var m = new Message(Cmd.PLAYER_MOVE);
            m.Writer.WriteInt(BotId);
            m.Writer.WriteShort(b.X);
            m.Writer.WriteShort(b.Y);
            PlayerService.SendZone(zone, m);
        }

        // ---------------------------------------------------------------- the player hits the headmaster

        /// <summary>Char.attackCharacter(Message) (PLAYER_ATTACK_PLAYER 61) inside a gym; ignored elsewhere
        /// (no PK offline).</summary>
        public static void AttackPlayer(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead || me.Zone == null) return;
            Room room;
            if (!Rooms.TryGetValue(me.Zone, out room) || room.Bot.IsDead) return;
            var skill = me.SelectedSkill;
            if (skill == null) return;
            long now = Now();
            if (now - me.LastTimeUseSkill < skill.CoolDown) return;
            me.LastTimeUseSkill = now;
            if (me.Inventory == null || me.Inventory.Equipment[1] == null) return;
            var tpl = world.FindSkillTemplate(skill.TemplateId);
            if (tpl != null && tpl.Type != PlayerSkills.TypeAttack) return;
            bool named = false;
            while (m.Reader.Remaining >= 4) if (m.Reader.ReadInt() == BotId) named = true;
            if (!named) return;
            var b = room.Bot;
            int up = me.Record.ClassId == 0 || me.Record.ClassId == 1 || me.Record.ClassId == 3 || me.Record.ClassId == 5 ? 40 : 0;
            if (b.X < me.X - skill.Dx || b.X > me.X + skill.Dx || b.Y < me.Y - skill.Dy - up || b.Y > me.Y + skill.Dy)
            {
                HaveAttack(me.Zone, BotId, b.Hp, 0, b.Mp);   // out of range: shown as a miss
                SkillPaint(me.Zone, me.Record.Id, skill.TemplateId, BotId);
                return;
            }
            if (me.Mp < skill.ManaUse) { PlayerService.ServerMessage(me, "Không đủ MP để sử dụng"); PlayerService.UpdateMp(me); return; }
            me.Mp -= skill.ManaUse;
            PlayerService.UpdateMp(me);
            SkillPaint(me.Zone, me.Record.Id, skill.TemplateId, BotId);
            int dmg = PvpDamage(me.Damage2, me.Damage, me.Fatal, me.PercentFatalDame, me.FatalDame, me.Exactly, me.Level,
                b.Miss, b.Level, 0, 0, 0, out bool fatal);
            b.Hp = Math.Max(0, b.Hp - dmg);
            HaveAttack(me.Zone, BotId, b.Hp, fatal ? -dmg : dmg, b.Mp);
            if (b.Hp <= 0) BotDie(me.Zone, room);
        }

        // Char.attackCharacter(ArrayList): damage of one hit on a character, PvP scale included
        // ((hit + fatal / 3) * (6 - levelGap / 40) / 100). Returns 0 for a miss.
        private static int PvpDamage(int dmg2, int dmg, int fatalChance, int percentFatal, int fatalDame, int exactly, int level,
            int targetMiss, int targetLevel, int targetDameDown, int targetReduce, int targetFatalRes, out bool isFatal)
        {
            isFatal = fatalChance > Rng.Next(1000);
            int basic = NextInt(dmg2, dmg);
            int hit = basic;
            int dameFatal = 0;
            if (isFatal) dameFatal = basic + basic * percentFatal / 100 + fatalDame;
            dameFatal -= dameFatal * Math.Min(100, targetFatalRes) / 100;
            hit -= hit * targetReduce / 100;
            hit = (hit + dameFatal / 3) * (6 - (targetLevel - level) / 40) / 100;
            if (hit <= 0) hit = 1;
            bool miss = Rng.Next(targetMiss + 100) > Rng.Next(exactly + 100);
            hit -= targetDameDown;
            if (hit < 0) { hit = 0; miss = true; }
            if (miss) { hit = 0; isFatal = false; }
            return hit;
        }

        // Char.die in a Gymnasium -> Gymnasium.setWin
        private static void BotDie(Zone zone, Room room)
        {
            var b = room.Bot;
            b.IsDead = true;
            var die = new Message(Cmd.PLAYER_DIE);   // MapService.waitToDie
            die.Writer.WriteInt(BotId);
            die.Writer.WriteByte(3);
            die.Writer.WriteShort(b.X);
            die.Writer.WriteShort(b.Y);
            PlayerService.SendZone(zone, die);
            var rm = new Message(Cmd.PLAYER_REMOVE);  // out(bot)
            rm.Writer.WriteInt(BotId);
            PlayerService.SendZone(zone, rm);
            room.Won = true;
            // npc.setStatus(1) + MapService.npcUpdate (NPC_PLAYER_UPDATE -94): the headmaster NPC shows again
            for (int i = 0; i < zone.Npcs.Count; i++)
                if (zone.Npcs[i].TemplateId == room.Npc)
                {
                    var up = PlayerService.Sub(-94);
                    up.Writer.WriteByte(i);
                    up.Writer.WriteByte(1);
                    PlayerService.SendZone(zone, up);
                }
            var me = room.Player;
            // Offline: only the step of this headmaster advances (Java called taskNext on any win)
            if (me != null && me.Zone == zone && me.Record.TaskId == TaskThachDau && me.Progress.TaskActive
                && me.Progress.TaskIndex == StepOf(room.Npc))
                PlayerTasks.TaskNext(me);
        }

        // ---------------------------------------------------------------- tick (every 500 ms, Bot.updateEveryHalfSecond)

        /// <summary>Called from the zone tick. Returns true when the zone is a gym (TaskMechanics skips it).</summary>
        public static bool UpdateZone(Zone zone, GameWorld world, long now)
        {
            Room room;
            if (!Rooms.TryGetValue(zone, out room)) return false;
            var me = room.Player;
            bool inside = me != null && me.Zone == zone;
            if (!inside || zone.Players.Count == 0)
            {
                // Gymnasium.out: the player left; the instance is dropped by PlayerActions.JoinZone when empty
                Rooms.Remove(zone);
                if (zone.Players.Count == 0) world.RemoveInstance(zone);
                return true;
            }
            if (now >= zone.InstanceEndAt)
            {
                // Gymnasium.close: back to the map the player came from
                Rooms.Remove(zone);
                foreach (PlayerState p in zone.Players.ToArray())
                {
                    var xy = PlayerTasks.VillageXY(room.BackMap);
                    p.X = xy[0]; p.Y = xy[1];
                    p.Actions.ChangeMap(room.BackMap);
                }
                return true;
            }
            if (!room.Bot.IsDead && !me.IsDead)
            {
                Attack(zone, world, room, now);
                Move(zone, world, room);
            }
            return true;
        }

        // AttackAround.attack: the target is the player once within 100 px; a random skill off cooldown hits when in range
        private static void Attack(Zone zone, GameWorld world, Room room, long now)
        {
            var b = room.Bot;
            var pl = room.Player;
            if (b.Skills.Length == 0) return;
            int d = Distance(b.X, b.Y, pl.X, pl.Y);
            if (d >= 100 && !room.Seen) return;   // AttackAround.detect
            room.Seen = true;
            int i = Rng.Next(b.Skills.Length);
            var s = b.Skills[i];
            if (now - b.LastUse[i] < s.CoolDown) return;
            // Offline: range = skill dx/dy + 100 (Java added 100 on every call, so the range kept growing)
            int dx = s.Dx + 100, dy = s.Dy + 100;
            if (pl.X < b.X - dx || pl.X > b.X + dx || pl.Y < b.Y - dy - 40 || pl.Y > b.Y + dy) return;
            b.LastUse[i] = now;
            SkillPaint(zone, BotId, s.TemplateId, pl.Record.Id);
            int dmg = PvpDamage(b.Damage2, b.Damage, b.Fatal, 0, 0, b.Exactly, b.Level,
                pl.Miss, pl.Level, pl.DameDown, pl.Options[63] + pl.Options[98], pl.Options[121], out bool fatal);
            if (dmg > 0) TaskMechanics.OnHit(pl);
            pl.Hp = Math.Max(0, pl.Hp - dmg);
            HaveAttack(zone, pl.Record.Id, pl.Hp, fatal ? -dmg : dmg, pl.Mp);
            if (pl.Hp <= 0) pl.Actions.StartDie();
        }

        // MoveWithinCustom.move -> MoveToTarget.move: steps of 50-90 px towards the player while the player is inside
        // the headmaster's area and more than 50 px away
        private static void Move(Zone zone, GameWorld world, Room room)
        {
            var b = room.Bot;
            var t = room.Player;
            if (!room.Seen) return;
            if (t.X < b.MinX || t.X > b.MaxX || t.Y < b.MinY || t.Y > b.MaxY) return;
            if (Distance(b.X, b.Y, t.X, t.Y) < 50) return;
            var grid = world.GetGrid(zone.Tile);
            int dir = t.X > b.X ? 1 : -1;
            int x = b.X + NextInt(50, 90) * dir;
            x = Math.Max(24, Math.Min(grid.PxW - 24, x));
            if (dir > 0 && x > t.X) x = t.X;   // offline: do not step past the player
            if (dir < 0 && x < t.X) x = t.X;
            short y = grid.CollisionY((short)x, (short)(b.Y - 24));
            if (Math.Abs(x - t.X) < 100)
            {
                if (t.Y < y) y = (short)(y - 72);
                else if (t.Y > y) y = t.Y;
            }
            b.X = (short)x;
            b.Y = y;
            PlayerMove(zone, b);
        }

        private static int Distance(int x1, int y1, int x2, int y2)
        {
            int dx = x1 - x2, dy = y1 - y2;
            return (int)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
