using System;
using System.Collections.Generic;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using Zone = NsoServer.World.Zone;
    using Waypoint = NsoServer.World.Waypoint;

    /// <summary>
    /// Tactical AI (game AI) of an AI player: plays the current plan step every server tick, like a person at the
    /// controls. It only perceives the character's state (PlayerState: position, HP, bag, quest...) and acts through
    /// BotClient, i.e. the messages the game client sends; the server applies the player's rules to everything
    /// (MapUnlock on map changes, cooldown/MP/range on attacks, shop prices and quest gates, pickup ownership, bag space).
    /// Besides the goals there are habits every player has: drink a potion when HP is low, spend potential and skill
    /// points, wear equipment and read skill books from the bag, pick up its own loot, return to the village after death.
    /// Reports progress, completion and failure back through AgentManager (StepDone / StepFailed / Report).
    /// </summary>
    public static partial class AgentTactics
    {
        // ================================================================== goals
        /// <summary>Goes to the map and fights monsters (a kind, or any within the experience window), picking up its
        /// own loot first. Every hit is a PLAYER_ATTACK_NPC message: range, cooldown, MP and damage are the server's.</summary>
        private static Status Hunt(NinjaAgent a, PlayerState me, GameWorld world, int mapId, int mobId, int count, long now, out string why)
        {
            why = null;
            if (me.Inventory.Equipment[1] == null)
            {
                why = "has no weapon (Tajima gives a wooden sword)";
                return GetSword(a, me, world, now);
            }

            if (mapId >= 0 && me.MapId != mapId)
            {
                a.Activity = "travelling to map " + mapId + " to hunt";
                var t = Travel(a, me, world, mapId, out why);
                return t == Status.Failed ? Status.Failed : Status.Running;
            }

            var mob = FindMob(me.Zone, a.TargetMob);
            if (mob != null && mob.IsDead && a.TargetMob >= 0)
            {
                // the monster we were fighting died: count it as ours if we hit it last (approximation of Mob.dead)
                a.Kills++;
                a.StepProgress++;
                a.LastProgressAt = now;
                a.TargetMob = -1;
                AgentManager.OnKill(a, world, mob);
                if (count > 0 && a.StepProgress >= count)
                    return Status.Done;
                mob = null;
            }

            if (a.ZoneTarget >= 0 && TrySpreadZone(a, me, world, now))
                return Status.Running;
            var loot = AgentForaging.Choose(a, me, world, AgentConfig.LootScanRadiusPx, now);
            if (loot != null)
            {
                PickUp(a, me, world, loot);
                return Status.Running;
            }

            // Finish an active exchange before walking away to the public zone-selection NPC.
            if ((mob == null || mob.IsDead || now - me.LastTimeUseSkill > 2000) && TrySpreadZone(a, me, world, now))
                return Status.Running;
            if (mob == null || mob.IsDead || !Suitable(me, mob, mobId))
            {
                mob = ChooseMob(a, me, mobId);
                a.TargetMob = mob != null ? mob.Id : -1;
                a.TargetMobTemplate = mob != null ? mob.Template.Id : -1;
            }

            if (mob == null && mobId >= 0)
            {
                // the kind we need is dead for now: wait next to it until it comes back (Zone.updateRecovery), as players do
                Mob dead = null;
                foreach (var m in me.Zone.Mobs)
                    if (m.Template.Id == mobId && m.IsDead && (dead == null || Math.Abs(m.X - me.X) < Math.Abs(dead.X - me.X)))
                        dead = m;
                if (dead != null)
                {
                    a.LastTargetSeen = now;
                    a.Activity = "waiting for " + dead.Template.Name + " to come back";
                    if (Math.Abs(dead.X - me.X) > 60)
                        WalkTo(a, me, world, dead.X, dead.Y);
                    return Status.Running;
                }
            }

            if (mob == null)
            {
                if (a.LastTargetSeen == 0)
                    a.LastTargetSeen = now;
                if (now - a.LastTargetSeen > AgentConfig.NoTargetFailSeconds * 1000L)
                {
                    why = "no suitable monster" + (mobId >= 0 ? " (id " + mobId + ")" : "") + " on map " + me.MapId;
                    return Status.Failed;
                }

                a.Activity = "looking for monsters";
                Wander(a, me, world, 300);
                return Status.Running;
            }

            a.LastTargetSeen = now;
            var skill = me.SelectedSkill;
            if (!InRange(me.X, me.Y, skill, mob))
            {
                a.Activity = "approaching " + mob.Template.Name + " lv " + mob.Level;
                int ax, ay;
                ApproachPoint(me, world.GetGrid(me.Zone.Tile), skill, mob, out ax, out ay);
                if (Math.Abs(ax - me.X) <= 8)
                    a.Client.Move(ax, ay); // same column, other height: one step up or down
                else
                    WalkTo(a, me, world, ax, ay);
                if (a.Client.CannotReach(ax, ay))
                {
                    // no way up/over to it (BotNavigation): choose another monster for a while instead of standing there
                    if (a.AvoidMobs.Count >= 64)
                        a.AvoidMobs.Clear();
                    a.AvoidMobs[mob.Id] = now + AgentConfig.UnreachableRetrySeconds * 1000L;
                    a.TargetMob = -1;
                }

                return Status.Running;
            }

            a.Activity = "fighting " + mob.Template.Name + " lv " + mob.Level + (count > 0 ? " (" + a.StepProgress + "/" + count + ")" : "");
            if (skill == null || now - me.LastTimeUseSkill < skill.CoolDown)
                return Status.Running;
            if (me.Mp < skill.ManaUse)
            {
                if (!DrinkMp(a, me, world, now))
                {
                    why = "out of MP and MP potions";
                    a.Recovering = CanAffordPotions(me, world);
                    if (!a.Recovering)
                        return Status.Failed;
                }

                return Status.Running;
            }

            a.Client.Attack(mob.Id);
            return Status.Running;
        }

        private static Mob FindMob(Zone zone, int id)
        {
            if (id < 0)
                return null;
            foreach (var m in zone.Mobs)
                if (m.Id == id)
                    return m;
            return null;
        }

        // monsters worth hunting: the plan's kind, otherwise within the experience window (Char.addExp: 10 levels)
        private static bool Suitable(PlayerState me, Mob m, int mobId)
        {
            if (m.IsDead || m.Hp <= 0 || m.IsBoss)
                return false;
            if (mobId >= 0)
                return m.Template.Id == mobId;
            if (m.Template.Id == 0)
                return false; // the training dummy only for its quest
            return m.Level >= me.Level - 10 && m.Level <= me.Level + 5;
        }

        private static Mob ChooseMob(NinjaAgent a, PlayerState me, int mobId)
        {
            Mob best = null;
            int bestD = int.MaxValue;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var m in me.Zone.Mobs)
            {
                if (!Suitable(me, m, mobId))
                    continue;
                long until;
                if (!m.Chars.Contains(me.Record.Id) && a.AvoidMobs.TryGetValue(m.Id, out until) && until > now)
                    continue;
                int d = Math.Abs(m.X - me.X) + 2 * Math.Abs(m.Y - me.Y);
                if (!m.Chars.Contains(me.Record.Id) && (m.Chars.Count > 0 || AgentManager.MobClaimed(a, m.Id)))
                    d += 500;
                if (m.Chars.Contains(me.Record.Id))
                    d -= 200; // finish what attacks us first
                if (d < bestD)
                {
                    bestD = d;
                    best = m;
                }
            }

            return best;
        }

        // a standable spot (no wood / rock) from which the selected skill reaches the monster: either side, near first
        private static void ApproachPoint(PlayerState me, TileGrid grid, SkillData skill, Mob mob, out int x, out int y)
        {
            int dx = skill != null ? Math.Max(12, skill.Dx) : 40;
            int side = me.X < mob.X ? -1 : 1;
            foreach (int d in new[]
            {
                dx / 2,
                dx - 4,
                6,
                dx / 4
            }

            )
                foreach (int s in new[]
                {
                    side,
                    -side
                }

                )
                {
                    int cx = Math.Max(30, Math.Min(grid.PxW - 31, mob.X + s * d));
                    short cy = grid.CollisionY((short)cx, (short)Math.Max(0, mob.Y - 24));
                    if (cy >= grid.PxH - 1)
                        continue;
                    cy = OverObstacle(grid, cx, cy);
                    if (InRange(cx, cy, skill, mob))
                    {
                        x = cx;
                        y = cy;
                        return;
                    }
                }

            x = mob.X + side * dx / 2;
            y = mob.Y;
        }

        // the range check of Char.attackMonster(Message), with the selected skill, from (px, py)
        internal static bool InRange(int px, int py, SkillData skill, Mob mob)
        {
            int sdx = skill != null ? skill.Dx : 40, sdy = skill != null ? skill.Dy : 40;
            int left = px - sdx, right = px + sdx, top = py - sdy - 75, bottom = py + sdy;
            int add = mob.Template.Type == 4 ? 50 : 0;
            int range = mob.Template.RangeMove;
            return mob.X >= left - range && mob.X <= right + range && mob.Y >= top - range - add && mob.Y <= bottom + range;
        }
    }
}
