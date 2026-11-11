using System;
using System.Collections.Generic;
using NsoServer.World;

namespace NsoServer.Net
{
    // the Unity client also defines global types with these names, so pick the server ones explicitly
    using TileMap = NsoServer.World.TileMap;
    using Zone = NsoServer.World.Zone;
    using Mob = NsoServer.World.Mob;

    // Port of Mob.update / Mob.attack: monsters notice players, attack them every few seconds and hurt them.
    public static class MobBrain
    {
        private static readonly System.Random Rng = new System.Random();

        // monsters that never act on their own (Mob.update excludes them from noticing players)
        private static bool NeverNotices(int id)
        {
            return id == 98 || id == 99 || id == 230 || id == 224 || id == 225 || id == 226;
        }

        private static bool NeverAttacks(int id)
        {
            return id == 0 || id == 113 || id == 54;
        }

        private static long DistanceSquared(int x1, int y1, int x2, int y2)
        {
            long dx = x2 - x1, dy = y2 - y1;
            return dx * dx + dy * dy;
        }

        // NinjaUtils.nextInt(min, max): inclusive, returns max when min >= max
        private static int NextInt(int min, int max)
        {
            if (min >= max) return max;
            return Rng.Next(max + 1 - min) + min;
        }

        public static void UpdateZone(Zone zone, long now)
        {
            if (zone.Players.Count == 0 || zone.Mobs.Count == 0) return;
            foreach (var mob in zone.Mobs) Update(zone, mob, now);
        }

        private static void Update(Zone zone, Mob mob, long now)
        {
            if (mob.IsDead) return;
            var tpl = mob.Template;
            if (tpl.Id != 98 && tpl.Id != 99)
            {
                if (!NeverNotices(tpl.Id))
                {
                    int add = mob.IsBoss ? 100 : 0;
                    foreach (PlayerState p in zone.Players)
                    {
                        if (p.IsDead) continue;
                        if (tpl.Type == 4)
                        {
                            int range = tpl.RangeMove + 50 + add;
                            if (DistanceSquared(mob.X, mob.Y, p.X, p.Y) < (long)range * range && !p.IsInvisible) AddChar(mob, p.Record.Id);
                        }
                        else if (mob.Y == p.Y && System.Math.Abs(mob.X - p.X) < tpl.RangeMove + 20 + add && !p.IsInvisible)
                        {
                            AddChar(mob, p.Record.Id);
                        }
                    }
                }
                if (!mob.IsIce && !mob.IsWind && !mob.IsDisable && !NeverAttacks(tpl.Id) && mob.Chars.Count > 0)
                {
                    if (now - mob.LastTimeAttack > mob.AttackDelay)
                    {
                        mob.LastTimeAttack = now;
                        Attack(zone, mob);
                    }
                }
            }
        }

        private static void AddChar(Mob mob, int id)
        {
            if (!mob.Chars.Contains(id)) mob.Chars.Add(id);
        }

        // Mob.attack(): pick one of the hostile players that is close enough
        private static void Attack(Zone zone, Mob mob)
        {
            PlayerState candidate = null;
            int candidateCount = 0;
            long maxDistance = mob.IsBoss ? 601L : 301L; // original code compares the floored pixel distance
            long maxDistanceSquared = maxDistance * maxDistance;
            for (int i = 0; i < mob.Chars.Count; i++)
            {
                int id = mob.Chars[i];
                foreach (PlayerState p in zone.Players)
                {
                    if (p.Record.Id != id || p.IsInvisible) continue;
                    if (DistanceSquared(mob.X, mob.Y, p.X, p.Y) >= maxDistanceSquared) continue;
                    candidateCount++;
                    // Reservoir sampling keeps the same uniform target choice without a per-attack list/array allocation.
                    if (Rng.Next(candidateCount) == 0) candidate = p;
                }
            }
            if (candidate != null) Hit(zone, mob, candidate);
        }

        // Mob.attack(Char pl, Char owner) for ordinary maps
        private static void Hit(Zone zone, Mob mob, PlayerState pl)
        {
            if (pl.IsDead) return;
            int dameHp = NextInt(mob.DamageOnPlayer2, mob.DamageOnPlayer);
            dameHp -= pl.DameDown;
            switch (mob.Sys)
            {
                case 1: dameHp -= dameHp * pl.Options[127] / 100; dameHp -= pl.ResFire; dameHp -= pl.Options[48]; break;
                case 2: dameHp -= dameHp * pl.Options[130] / 100; dameHp -= pl.ResIce; dameHp -= pl.Options[49]; break;
                case 3: dameHp -= dameHp * pl.Options[131] / 100; dameHp -= pl.ResWind; dameHp -= pl.Options[50]; break;
            }
            int exactly = Rng.Next(mob.Level * 10 + 100);
            int miss = Rng.Next(pl.Miss + 100);
            bool isMiss = exactly < miss;
            if (isMiss) dameHp = -1;
            else if (dameHp <= 0) dameHp = 1;
            int dameMp = 0;

            // what the client shows: the hit on the player, then the hit seen by everyone in the zone
            var me = new Message(Cmd.NPC_ATTACK_ME);
            me.Writer.WriteByte(mob.Id);
            me.Writer.WriteInt(dameHp);
            me.Writer.WriteInt(dameMp);
            me.Writer.WriteShort(-1);
            me.Writer.WriteByte(0);
            me.Writer.WriteByte(0);
            pl.Session.Send(me);
            foreach (PlayerState p in zone.Players)
            {
                var all = new Message(Cmd.NPC_ATTACK_PLAYER);
                all.Writer.WriteByte(mob.Id);
                all.Writer.WriteInt(pl.Record.Id);
                all.Writer.WriteInt(pl.Hp);
                all.Writer.WriteInt(pl.Mp);
                all.Writer.WriteShort(-1);
                all.Writer.WriteByte(0);
                all.Writer.WriteByte(0);
                p.Session.Send(all);
            }
            // the real damage is only a fifth of the displayed number
            dameHp = dameHp - dameHp * 80 / 100;
            if (dameHp < 0) dameHp = -1;
            if (dameHp > 0)
            {
                pl.Hp -= dameHp;
                if (pl.Hp <= 0) pl.Actions.StartDie();
            }
        }
    }
}


