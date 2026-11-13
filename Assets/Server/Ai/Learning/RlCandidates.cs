using System;
using System.Collections.Generic;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai.Rl
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using Message = NsoServer.Net.Message;
    using Cmd = NsoServer.Net.Cmd;

    /// <summary>Versioned, bounded candidate slots. Masks express feasibility, never bypass Controller validation.</summary>
    public static class RlCandidates
    {
        public static bool IsPrimitive(sbyte cmd, byte[] data, PlayerState me, GameWorld world)
        {
            if (cmd == Cmd.PLAYER_MOVE || cmd == 60 || cmd == 41 || cmd == -14)
                return true;
            if (cmd != 11 || data == null || data.Length < 1 || data[0] >= me.Inventory.Bag.Length)
                return false;
            var item = me.Inventory.Bag[data[0]];
            var t = item != null ? world.FindItem(item.Id) : null;
            return t != null && (t.Type == PlayerItems.TypeHp || t.Type == PlayerItems.TypeMp);
        }

        public static Observation Strategy(NinjaAgent a, GameWorld w, AgentSnapshot s, Decision teacher)
        {
            var o = Base(a, w, "strategy");
            Add(o, 0, new ActionCandidate { Name = "idle", Valid = true, Kind = 5, Step = new PlanStep { Goal = GoalType.Idle, Seconds = 10 } });
            Add(o, 1, Goal("quest", GoalType.Quest, s.QuestId >= 0 && !s.QuestBlockedByLevel));
            Add(o, 2, Goal("shop", GoalType.Shop, s.CanShop && s.CanAffordPotions && s.FreeSlots > 0));
            Add(o, 3, Goal("collect", GoalType.Collect, s.ItemsNearby > 0));
            Add(o, 4, Goal("rest", GoalType.Rest, s.HpPercent < 90 && (s.HpPotions > 0 || s.CanAffordPotions)));
            // item ledger (user 2026-11-13): buy and use one never-used village-shop item; the ledger rewards decide its worth
            Add(o, 5, new ActionCandidate { Name = "try item", Valid = s.TryItemId >= 0 && s.FreeSlots > 0, Kind = (int)GoalType.Shop + 1, Id = Math.Max(0, s.TryItemId), Step = new PlanStep { Goal = GoalType.Shop, MobId = s.TryItemId, Count = 1 } });
            int i = 8;
            foreach (var h in s.Hunts)
            {
                if (i >= 32)
                    break;
                Add(o, i++, new ActionCandidate { Name = "hunt", Valid = s.HasWeapon, Kind = 1, Id = h.MobId, Map = h.MapId, X = h.Hops, Y = h.MobLevel - s.Level, Step = new PlanStep { Goal = GoalType.Hunt, MapId = h.MapId, MobId = h.MobId, Count = 10 } });
            }

            i = 32;
            foreach (int map in s.Neighbors)
            {
                if (i >= 46)
                    break;
                Add(o, i++, new ActionCandidate { Name = "explore", Valid = true, Kind = 6, Map = map, Step = new PlanStep { Goal = GoalType.Explore, MapId = map } });
                if (i < 46)
                    Add(o, i++, new ActionCandidate { Name = "travel", Valid = true, Kind = 7, Map = map, Step = new PlanStep { Goal = GoalType.Travel, MapId = map } });
            }

            // The teacher's complete multi-step plan remains a candidate, preserving working quest/shop behaviour.
            if (teacher != null && teacher.Steps.Count > 0)
                Add(o, 47, new ActionCandidate { Name = "guided plan", Kind = 8, Valid = true, Step = teacher.Steps[0], Map = teacher.Steps[0].MapId });
            Encode(o, a.Me);
            return o;
        }

        private static ActionCandidate Goal(string name, GoalType goal, bool valid)
        {
            return new ActionCandidate
            {
                Name = name,
                Valid = valid,
                Kind = (int)goal + 1,
                Step = new PlanStep
                {
                    Goal = goal,
                    Count = 5
                }
            };
        }

        public static Observation Tactical(NinjaAgent a, GameWorld w, sbyte proposed, byte[] payload, long now)
        {
            var me = a.Me;
            var o = Base(a, w, "tactical");
            var grid = w.GetGrid(me.Zone.Tile);
            Add(o, 0, new ActionCandidate { Name = "wait", Kind = 0, Valid = true });
            // Eight local directions; the floor/obstacle calculation is shared with the original tactical movement.
            int step = Math.Max(8, Math.Min(AgentConfig.MoveStepPx, me.Speed * 16));
            int[] dx =
            {
                -1,
                0,
                1,
                -1,
                1,
                -1,
                0,
                1
            }, dy =
            {
                -1,
                -1,
                -1,
                0,
                0,
                1,
                1,
                1
            };
            for (int i = 0; i < 8; i++)
            {
                int x = me.X + dx[i] * step;
                short y = grid.CollisionY((short)Math.Max(30, Math.Min(grid.PxW - 31, x)), (short)Math.Max(0, me.Y + dy[i] * 24 - 24));
                y = AgentTactics.OverObstacle(grid, x, y);
                var m = new Message(Cmd.PLAYER_MOVE);
                m.Writer.WriteShort(x);
                m.Writer.WriteShort(y);
                Add(o, i + 1, new ActionCandidate { Name = "move", Kind = 1, X = x, Y = y, Command = Cmd.PLAYER_MOVE, Payload = m.GetData(), Valid = (x != me.X || y != me.Y) && x >= 30 && x < grid.PxW - 30 && y > 0 && y < grid.PxH - 1 && !grid.IsWood(x, y - 1) && !grid.IsRock(x, y - 1) });
            }

            // Preserve an exact demonstrated movement in its directional slot (including jumps/waypoint approaches).
            if (proposed == Cmd.PLAYER_MOVE && payload != null && payload.Length >= 4)
            {
                var r = new JavaReader(payload);
                int x = r.ReadShort(), y = r.ReadShort();
                int slot = Direction(x - me.X, y - me.Y);
                var c = o.Candidates[slot];
                c.X = x;
                c.Y = y;
                c.Payload = payload;
                c.Valid = (x != me.X || y != me.Y) && Math.Abs(x - me.X) <= me.Speed * 17 && x >= 30 && x < grid.PxW - 30 && y > 0 && y < grid.PxH - 1 && !grid.IsWood(x, y - 1) && !grid.IsRock(x, y - 1);
            }

            var mobs = new List<Mob>(me.Zone.Mobs);
            mobs.RemoveAll(m => m.IsDead || m.IsBoss);
            mobs.Sort((x, y) => x.Id.CompareTo(y.Id));
            var skills = new List<SkillData>();
            if (me.SelectedSkill != null)
                skills.Add(me.SelectedSkill);
            foreach (var s in me.Progress.Skills)
            {
                var skill = w.FindSkill(s[0], s[1]);
                var t = w.FindSkillTemplate(s[0]);
                if (skill != null && t != null && t.Type == PlayerSkills.TypeAttack && !skills.Exists(k => k.TemplateId == skill.TemplateId))
                    skills.Add(skill);
            }

            // Sort targets by distance then id, to keep the local window useful on large maps.
            mobs.Sort((x, y) =>
            {
                int d = (Math.Abs(x.X - me.X) + Math.Abs(x.Y - me.Y)).CompareTo(Math.Abs(y.X - me.X) + Math.Abs(y.Y - me.Y));
                return d != 0 ? d : x.Id.CompareTo(y.Id);
            });
            for (int mi = 0; mi < mobs.Count && mi < 8; mi++)
                for (int si = 0; si < skills.Count && si < 3; si++)
                {
                    var m = mobs[mi];
                    var s = skills[si];
                    Add(o, 9 + mi * 3 + si, new ActionCandidate { Name = "attack", Kind = 2, Id = m.Id, X = m.X, Y = m.Y, Skill = s.TemplateId, TemplateId = m.Template.Id, LevelGap = m.Level - me.Level, HpFraction = m.Hp / (float)Math.Max(1, m.MaxHp), Valid = me.Inventory.Equipment[1] != null && me.Mp >= s.ManaUse && now - me.LastTimeUseSkill >= s.CoolDown && AgentTactics.InRange(me.X, me.Y, s, m) });
                }

            Potion(o, 33, me, w, PlayerItems.TypeHp, me.Hp < me.MaxHp, now, false);
            Potion(o, 34, me, w, PlayerItems.TypeMp, me.Mp < me.MaxMp, now, false);
            // stronger items when needed (user 2026-11-13): the strongest potion as its own action, so the policy can learn
            // when the expensive one is worth it (the cheap fitting one is slot 33/34)
            Potion(o, 43, me, w, PlayerItems.TypeHp, me.Hp < me.MaxHp, now, true);
            Potion(o, 44, me, w, PlayerItems.TypeMp, me.Mp < me.MaxMp, now, true);
            var items = new List<ItemMapEntry>(me.Zone.ItemMaps);
            items.Sort((x, y) => x.Id.CompareTo(y.Id));
            int n = 35;
            foreach (var it in items)
            {
                if (n >= 43)
                    break;
                if (it.PickedUp || (it.GateTask >= 0 && !TaskMechanics.CanSee(me, it)))
                    continue;
                bool own = GroundLootPolicy.HasOwnership(me.Record.Id, it, now);
                bool close = PlayerItems.IsWithinPickupRange(me, it);
                var template = w.FindItem(it.Item.Id);
                bool room = PlayerItems.CanReceiveGroundItem(me, template, it.Item);
                Add(o, n++, new ActionCandidate { Name = "pick", Kind = 4, Id = it.Id, X = it.X, Y = it.Y, Valid = own && close && room });
            }

            Encode(o, me);
            return o;
        }

        private static void Potion(Observation o, int slot, PlayerState me, GameWorld w, int type, bool needed, long now, bool strongest)
        {
            int i = AgentTactics.ChoosePotion(me, w, type, strongest);
            if (i < 0)
                return;
            var it = me.Inventory.Bag[i];
            var t = w.FindItem(it.Id);
            if (it.Quantity <= 0 || (it.Expire > 0 && it.Expire < now) || ((t.Gender == 0 || t.Gender == 1) && t.Gender != me.Record.Gender))
                return;
            if (strongest && o.Candidates[slot - 10] != null && o.Candidates[slot - 10].Id == i)
                return; // same potion as the fitting one
            bool effect = me.Effects.Exists(e => e.TemplateId == (type == PlayerItems.TypeHp ? 21 : 22) && !e.IsExpired(now));
            Add(o, slot, new ActionCandidate { Name = (type == PlayerItems.TypeHp ? "hp potion" : "mp potion") + (strongest ? " (strongest)" : ""), Kind = 3, Id = i, Valid = needed && !effect });
        }

        public static int Direction(int x, int y)
        {
            int sx = Math.Sign(x), sy = Math.Sign(y);
            if (sy < 0)
                return sx < 0 ? 1 : sx == 0 ? 2 : 3;
            if (sy > 0)
                return sx < 0 ? 6 : sx == 0 ? 7 : 8;
            return sx < 0 ? 4 : 5;
        }

        public static int Match(Observation o, sbyte cmd, byte[] payload, PlayerState me)
        {
            try
            {
                var r = new JavaReader(payload);
                int id = cmd == -14 || cmd == 41 ? r.ReadShort() : cmd == Cmd.PLAYER_MOVE ? -1 : r.ReadByte();
                if (cmd == Cmd.PLAYER_MOVE)
                {
                    int x = r.ReadShort(), y = r.ReadShort();
                    int d = Direction(x - me.X, y - me.Y);
                    return o.ActionMask[d] ? d : -1;
                }

                for (int i = 9; i < Observation.ActionCount; i++)
                {
                    var c = o.Candidates[i];
                    if (c == null || !c.Valid)
                        continue;
                    if ((cmd == 60 && c.Kind == 2 && c.Id == id && me.SelectedSkill != null && c.Skill == me.SelectedSkill.TemplateId) || (cmd == -14 && c.Kind == 4 && c.Id == id) || (cmd == 11 && c.Kind == 3 && c.Id == id))
                        return i;
                }
            }
            catch (Exception)
            {
            } // Unsupported human packets are not learning samples.

            return -1;
        }

        private static Observation Base(NinjaAgent a, GameWorld w, string domain)
        {
            var p = a.Me;
            var o = new Observation
            {
                Domain = domain
            };
            var x = o.Values;
            x[0] = domain == "strategy" ? 1 : 0;
            x[1] = p.Level / 150f;
            x[2] = AgentTactics.HpPercent(p) / 100f;
            x[3] = AgentTactics.MpPercent(p) / 100f;
            x[4] = Clip(p.Progress.Yen / 10000f);
            x[5] = Clip(p.Progress.Xu / 10000f);
            x[6] = p.Record.ClassId / 6f;
            x[7] = p.Record.TaskId / 100f;
            x[8] = p.Progress.TaskIndex / 20f;
            x[9] = p.Progress.TaskActive ? 1 : 0;
            x[10] = Clip(AgentTactics.HpPotions(p, w) / 20f);
            x[11] = Clip(AgentTactics.MpPotions(p, w) / 20f);
            x[12] = PlayerItems.SlotNull(p) / (float)Math.Max(1, p.Inventory.Bag.Length);
            x[13] = p.Inventory.Equipment[1] != null ? 1 : 0;
            x[14] = p.X / (float)Math.Max(1, w.GetGrid(p.Zone.Tile).PxW);
            x[15] = p.Y / (float)Math.Max(1, w.GetGrid(p.Zone.Tile).PxH);
            x[16] = a.P.Aggression;
            x[17] = a.P.Caution;
            x[18] = a.P.Greed;
            x[19] = a.P.Curiosity;
            x[20] = a.P.Sociability;
            x[21] = a.Step != null ? ((int)a.Step.Goal + 1) / 8f : 0;
            x[22] = p.MapId / 200f;
            x[23] = Clip(p.Progress.TaskCount / 30f);
            x[24] = p.IsDead ? 1 : 0;
            var advice = a.Learning.Advice;
            x[25] = advice != null && advice.Steps.Count > 0 ? ((int)advice.Steps[0].Goal + 1) / 8f : 0;
            x[26] = advice != null ? Math.Max(0, advice.FleeHpPercent) / 100f : 0;
            long floor = w.ExpForLevel(p.Level), next = w.ExpForLevel(p.Level + 1);
            x[27] = next > floor ? Clip((float)((p.Record.Exp - floor) / (double)(next - floor))) : 1;
            x[28] = Clip(p.Damage / 1000f);
            x[29] = Clip(p.MaxHp / 1000f);
            x[30] = Clip(p.Miss / 1000f);
            x[31] = Clip(p.Exactly / 1000f);
            int h = 32;
            foreach (float v in a.Learning.History)
            {
                if (h >= 40)
                    break;
                x[h++] = v;
            }

            return o;
        }

        private static float Clip(float x)
        {
            return Math.Max(-2, Math.Min(2, x));
        }

        private static void Add(Observation o, int i, ActionCandidate c)
        {
            o.Candidates[i] = c;
            o.ActionMask[i] = c.Valid;
        }

        private static void Encode(Observation o, PlayerState p)
        {
            for (int i = 0; i < o.Candidates.Length; i++)
            {
                var c = o.Candidates[i];
                if (c == null)
                    continue;
                int k = Observation.GlobalSize + i * Observation.CandidateSize;
                bool attack = o.Domain == "tactical" && c.Kind == 2;
                o.Values[k] = c.Valid ? 1 : 0;
                o.Values[k + 1] = c.Kind / 8f;
                o.Values[k + 2] = (attack ? c.TemplateId : c.Id) / 500f;
                o.Values[k + 3] = attack ? c.HpFraction : c.Map / 200f;
                o.Values[k + 4] = o.Domain == "strategy" ? c.X / 10f : Clip((c.X - p.X) / 500f);
                o.Values[k + 5] = o.Domain == "strategy" ? c.Y / 10f : Clip((c.Y - p.Y) / 200f);
                o.Values[k + 6] = c.Skill / 100f;
                o.Values[k + 7] = attack ? Clip(c.LevelGap / 10f) : c.Step != null ? c.Step.Count / 30f : 0;
            }
        }
    }
}
