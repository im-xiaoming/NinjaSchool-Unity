using System;
using System.Collections.Generic;
using NsoServer.World;

namespace NsoServer.Net
{
    using Zone = NsoServer.World.Zone;
    using Mob = NsoServer.World.Mob;
    using Math = System.Math;   // the client has its own global Math class

    // Port of com.nsoz.effect.EffectManager (setEffect / update) and the regeneration parts of
    // Char.updateEveryHalfSecond / updateEveryFiveSecond, plus the expiry of monster status effects (Mob.update).
    //
    // HP/MP over time, as in the original:
    //   - food (effect type 0): +param HP and MP every half second while it lasts (30 min for rice balls etc.)
    //   - HP potion (effect 21, type 17): +param HP every half second for 3 s
    //   - equipment options 99 (every half second), 30/120 (HP every 5 s), 27/119 (MP every 5 s)
    // Offline addition: a small natural regeneration (NaturalRegenPercent of max HP/MP every NaturalRegenMs) so a
    // character without food still recovers slowly. Set NaturalRegenPercent to 0 to play exactly like the original.
    public static class PlayerEffects
    {
        public static int NaturalRegenPercent = 2;
        public static int NaturalRegenMs = 5000;

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        // EffectManager.setEffect: replaces an effect of the same type, otherwise adds it
        public static void SetEffect(PlayerState me, GameWorld world, int templateId, long time, int param)
        {
            long now = Now();
            int type = world.EffectType(templateId);
            var e = new PlayerEffect { TemplateId = templateId, Type = type, Param = param, StartAt = now, EndAt = now + time };
            for (int i = 0; i < me.Effects.Count; i++)
            {
                if (me.Effects[i].Type != type) continue;
                PlayerService.RemoveEffect(me, me.Effects[i]);
                me.Effects[i] = e;
                PlayerService.AddEffect(me, e, now);
                return;
            }
            if (type == 0) me.Effects.Insert(0, e); else me.Effects.Add(e);
            PlayerService.AddEffect(me, e, now);
        }

        // Char.updateEveryHalfSecond (+ updateEveryFiveSecond), called by the server tick every 500 ms
        public static void UpdateHalfSecond(PlayerState me, GameWorld world)
        {
            long now = Now();
            // EffectManager.update: drop expired effects
            for (int i = me.Effects.Count - 1; i >= 0; i--)
            {
                if (!me.Effects[i].IsExpired(now)) continue;
                var e = me.Effects[i];
                me.Effects.RemoveAt(i);
                PlayerService.RemoveEffect(me, e);
            }
            if (me.IsDead) return;
            int hp = 0, mp = 0;
            foreach (var e in me.Effects)
            {
                if (e.TemplateId == 36 || e.TemplateId == 42) { hp += e.Param; mp += e.Param; continue; }
                switch (e.Type)
                {
                    case 0: case 12: hp += e.Param; mp += e.Param; break;
                    case 13: hp -= me.MaxHp * 3 / 100; break;
                    case 4: case 17: hp += e.Param; break;
                }
            }
            hp += me.Options[99];
            mp += me.Options[99];
            if (now - me.LastRegen >= NaturalRegenMs)
            {
                me.LastRegen = now;
                hp += me.Options[30] + me.Options[120];   // updateEveryFiveSecond
                mp += me.Options[27] + me.Options[119];
                if (NaturalRegenPercent > 0)
                {
                    hp += Math.Max(1, me.MaxHp * NaturalRegenPercent / 100);
                    mp += Math.Max(1, me.MaxMp * NaturalRegenPercent / 100);
                }
            }
            if (hp == 0 && mp == 0) return;
            int preHp = me.Hp, preMp = me.Mp;
            me.Hp = Math.Min(me.MaxHp, me.Hp + hp);
            me.Mp = Math.Min(me.MaxMp, me.Mp + mp);
            if (me.Hp == preHp && me.Mp == preMp) return;
            PlayerService.LoadHp(me);
            PlayerService.UpdateHp(me);
            PlayerService.UpdateMp(me);
            if (me.Hp <= 0) me.Actions.StartDie();
        }

        // ---- monster status effects from skills ----

        // effect type -> client command telling the zone about the status
        private static sbyte StateCommand(int type)
        {
            switch (type)
            {
                case 1: return PlayerService.NPC_IS_FIRE;
                case 2: return PlayerService.NPC_IS_ICE;
                case 3: return PlayerService.NPC_IS_WIND;
                case 14: return PlayerService.NPC_IS_MOVE;
                default: return PlayerService.NPC_IS_DISABLE;
            }
        }

        // Zone.setFire / setIce / setWind / setMove / setDisable
        private static void SetState(Zone zone, Mob mob, int type, bool on)
        {
            switch (type)
            {
                case 1: mob.IsFire = on; break;
                case 2: mob.IsIce = on; break;
                case 3: mob.IsWind = on; break;
                case 14: mob.IsDontMove = on; break;
                default: mob.IsDisable = on; break;
            }
            PlayerService.MobState(zone, StateCommand(type), mob, on);
        }

        // mob.effects.put(type, effect) + zone.setX(mob, true); a longer effect already running is kept
        public static void AddMobEffect(Zone zone, Mob mob, int type, long time)
        {
            long now = Now();
            long end;
            if (mob.EffectEnd.TryGetValue(type, out end) && end - now > time) return;
            mob.EffectEnd[type] = now + time;
            SetState(zone, mob, type, true);
        }

        // Mob.update: expired status effects end
        public static void UpdateMobEffects(Zone zone)
        {
            long now = Now();
            foreach (var mob in zone.Mobs)
            {
                if (mob.EffectEnd.Count == 0) continue;
                List<int> done = null;
                foreach (var kv in mob.EffectEnd) if (kv.Value <= now || mob.IsDead) (done ?? (done = new List<int>())).Add(kv.Key);
                if (done == null) continue;
                foreach (int type in done)
                {
                    mob.EffectEnd.Remove(type);
                    SetState(zone, mob, type, false);
                }
            }
        }
    }
}
