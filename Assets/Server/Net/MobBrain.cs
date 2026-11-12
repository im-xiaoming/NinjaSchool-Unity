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
        private static readonly List<Escort.State> Bots = new List<Escort.State>();   // reused: escorted bots of the zone

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
            Escort.BotsIn(zone, Bots);   // Zone.getChars also holds bots (the escorted Jaian)
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
                    // the same notice rule for bots (Jaian, Escort.cs)
                    foreach (var b in Bots)
                    {
                        if (tpl.Type == 4)
                        {
                            int range = tpl.RangeMove + 50 + add;
                            if (DistanceSquared(mob.X, mob.Y, b.X, b.Y) < (long)range * range) AddChar(mob, b.Id);
                        }
                        else if (mob.Y == b.Y && System.Math.Abs(mob.X - b.X) < tpl.RangeMove + 20 + add) AddChar(mob, b.Id);
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
            Escort.State botCandidate = null;
            int candidateCount = 0;
            long maxDistance = mob.IsBoss ? 601L : 301L; // original code compares the floored pixel distance
            long maxDistanceSquared = maxDistance * maxDistance;
            for (int i = 0; i < mob.Chars.Count; i++)
            {
                int id = mob.Chars[i];
                foreach (PlayerState p in zone.Players)
                {
                    if (p.Record.Id != id || PlayerBuff.IsHidden(p)) continue;   // isInvisible || isHide
                    if (DistanceSquared(mob.X, mob.Y, p.X, p.Y) >= maxDistanceSquared) continue;
                    candidateCount++;
                    // Reservoir sampling keeps the same uniform target choice without a per-attack list/array allocation.
                    if (Rng.Next(candidateCount) == 0) { candidate = p; botCandidate = null; }
                }
                var b = id < 0 ? Escort.FindBot(zone, id) : null;   // a bot (Jaian) is a char too
                if (b != null && DistanceSquared(mob.X, mob.Y, b.X, b.Y) < maxDistanceSquared)
                {
                    candidateCount++;
                    if (Rng.Next(candidateCount) == 0) { botCandidate = b; candidate = null; }
                }
            }
            if (botCandidate != null) HitBot(zone, mob, botCandidate);
            else if (candidate != null) Hit(zone, mob, candidate);
        }

        // Mob.attack(Char pl, Char owner) on a bot: a bot has no defence options, dodge from Bot.setDefault (miss 0)
        private static void HitBot(Zone zone, Mob mob, Escort.State b)
        {
            int dameHp = GameBalanceRules.Stat(NextInt(mob.DamageOnPlayer2, mob.DamageOnPlayer));
            var rules = b.Player.Session.Server.World.CombatRules;
            if (Rng.NextDouble() < rules.DodgeChance(mob.Level, 0)) dameHp = -1;
            else if (dameHp <= 0) dameHp = 1;
            if (dameHp > 0) dameHp = rules.ActualDamage(dameHp);   // same scaling as a hit on a player (Hit)
            Escort.Hit(zone, mob.Id, b, dameHp);
        }

        // Mob.attack(Char pl, Char owner) for ordinary maps
        private static void Hit(Zone zone, Mob mob, PlayerState pl)
        {
            if (pl.IsDead) return;
            long reducedDamage = NextInt(mob.DamageOnPlayer2, mob.DamageOnPlayer);
            reducedDamage -= pl.DameDown;
            switch (mob.Sys)
            {
                case 1: reducedDamage -= reducedDamage * pl.Options[127] / 100; reducedDamage -= pl.ResFire; reducedDamage -= pl.Options[48]; break;
                case 2: reducedDamage -= reducedDamage * pl.Options[130] / 100; reducedDamage -= pl.ResIce; reducedDamage -= pl.Options[49]; break;
                case 3: reducedDamage -= reducedDamage * pl.Options[131] / 100; reducedDamage -= pl.ResWind; reducedDamage -= pl.Options[50]; break;
            }
            int dameHp = GameBalanceRules.Stat(reducedDamage);
            var rules = pl.Session.Server.World.CombatRules;
            bool isMiss = Rng.NextDouble() < rules.DodgeChance(mob.Level, pl.Miss);
            if (PlayerBuff.Find(pl, PlayerBuff.TypeDodge) != null) isMiss = true;   // pl.isMiss
            if (isMiss) dameHp = -1;
            else if (dameHp <= 0) dameHp = 1;
            int dameMp = 0;
            var shield = dameHp > 0 ? PlayerBuff.Find(pl, PlayerBuff.TypeManaShield) : null;   // pl.isShieldMana
            if (shield != null && pl.MaxMp > 0 && (long)pl.Mp * 100 / pl.MaxMp >= 10)
            {
                dameMp = System.Math.Min(pl.Mp, dameHp * shield.Param / 100);
                dameHp -= dameMp;
                pl.Mp -= dameMp;
            }

            // Offline correction to Mob.attack(Char, Char): scale once BEFORE sending the hit.
            // Sending raw damage but deducting only a fraction on the server made later HP syncs
            // look like free healing. Preserve MISS and a fully absorbed hit (zero damage).
            if (dameHp > 0) dameHp = rules.ActualDamage(dameHp);
            if (dameHp > 0)
            {
                TaskMechanics.OnHit(pl);
                pl.Hp = System.Math.Max(0, pl.Hp - dameHp);
            }

            // The victim receives the damage packet; observers receive post-hit HP/MP.
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
                if (p == pl) continue; // do not replay the same attack through the observer path
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
            if (pl.Hp <= 0) pl.Actions.StartDie();
        }
    }
}


