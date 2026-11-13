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
        // LLM "goto": travel to the map, then walk/jump to the coordinates over the platforms (BotNavigation).
        private static Status MoveTo(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, out string why)
        {
            why = null;
            if (step.MapId >= 0 && me.MapId != step.MapId)
            {
                a.Activity = "travelling to map " + step.MapId + " to reach (" + step.X + "," + step.Y + ")";
                return Travel(a, me, world, step.MapId, out why) == Status.Failed ? Status.Failed : Status.Running;
            }

            var grid = world.GetGrid(me.Zone.Tile);
            int x = Math.Max(30, Math.Min(grid.PxW - 31, step.X));
            int y = step.Y < 0 ? -1 : Math.Max(0, Math.Min(grid.PxH - 1, step.Y));
            a.Activity = "going to (" + x + "," + (y < 0 ? "any" : y.ToString()) + ")";
            if (WalkTo(a, me, world, x, y) || (Math.Abs(me.X - x) <= 24 && (y < 0 || Math.Abs(me.Y - y) <= 40)))
            {
                a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return Status.Done;
            }

            if (a.Client.CannotReach(x, y))
            {
                why = "no way from (" + me.X + "," + me.Y + ") to (" + x + "," + y + ") on map " + me.MapId;
                a.Knowledge.Learn("note:unreachable" + me.MapId + "_" + x / 48 + "_" + (y < 0 ? 0 : y / 48), "map " + me.MapId + ": (" + x + "," + y + ") cannot be reached from (" + me.X + "," + me.Y + ")");
                return Status.Failed;
            }

            return Status.Running;
        }

        // ================================================================== movement
        /// <summary>Walks towards a map through the waypoint graph over unlocked maps, then asks to change map at the
        /// exit (Service.requestChangeMap); the server checks MapUnlock and refuses locked maps.</summary>
        private static Status Travel(NinjaAgent a, PlayerState me, GameWorld world, int target, out string why)
        {
            why = null;
            int cur = me.MapId;
            int task = me.Record.TaskId;
            if (target < 0 || cur == target)
            {
                a.Route = null;
                return Status.Done;
            }

            if (a.Route == null || a.Route.Count == 0 || a.Route[a.Route.Count - 1] != target || WorldKnowledge.ExitTo(me.Zone.Tile, a.Route[0]) == null)
                a.Route = WorldKnowledge.Route(world, cur, target, task);
            if (a.Route == null)
            {
                why = MapUnlock.CanEnter(task, target) ? "no way from map " + cur + " to map " + target : "map " + target + " is locked until a later main quest";
                if (!MapUnlock.CanEnter(task, target))
                    a.Knowledge.Learn("locked:" + target, "map " + target + " " + MapName(world, target) + " is locked: needs a later main quest (now quest " + task + ")");
                return Status.Failed;
            }

            if (a.Route.Count == 0)
                return Status.Done;
            // fast travel (user 2026-11-13): carriage or teleport scroll when it saves enough walking
            if (a.Fast != null && a.Fast.Target != target)
                a.Fast = null;
            if (a.Fast == null)
                a.Fast = PlanFastTravel(a, me, world, target, a.Route.Count, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (a.Fast != null)
                return RunFastTravel(a, me, world, out why);
            int next = a.Route[0];
            var wp = WorldKnowledge.ExitTo(me.Zone.Tile, next);
            if (wp == null)
            {
                a.Route = null;
                why = "lost the way to map " + target;
                return Status.Failed;
            }

            int wx = Math.Max(wp.MinX, Math.Min(wp.MaxX, (wp.MinX + wp.MaxX) / 2)), wy = wp.MaxY;
            if (WalkTo(a, me, world, wx, wy, true))
            {
                // Play mode 2026-11-13: LastNotice is the last dialog ever shown (e.g. "Tên nhân vật đã tồn tại!" from
                // the login), so a refusal without its own notice must not report or learn it.
                string noticeBefore = a.Session.LastNotice ?? "";
                a.Session.LastNotice = "";
                a.Client.ChangeMap();
                if (me.MapId == next)
                {
                    a.Route.RemoveAt(0);
                    a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    a.Knowledge.Remove("locked:" + next);
                    if (a.Route.Count == 0)
                    {
                        a.Route = null;
                        return Status.Done;
                    }
                }
                else if (!me.IsChangeMap)
                {
                    a.Route = null;
                    string reason = string.IsNullOrEmpty(a.Session.LastNotice) ? "no reason given (at " + me.X + "," + me.Y + ", exit " + wp.MinX + "-" + wp.MaxX + "," + wp.MinY + "-" + wp.MaxY + ")" : a.Session.LastNotice;
                    why = "the server refused map " + next + ": " + reason;
                    // only a real refusal notice is knowledge; a silent failure is a positioning problem
                    if (!string.IsNullOrEmpty(a.Session.LastNotice))
                        a.Knowledge.Learn("locked:" + next, "map " + next + " " + MapName(world, next) + ": " + reason);
                    return Status.Failed;
                }

                if (string.IsNullOrEmpty(a.Session.LastNotice))
                    a.Session.LastNotice = noticeBefore;
            }

            return Status.Running;
        }

        private static string MapName(GameWorld world, int id)
        {
            var t = world.FindMap(id);
            return t != null ? t.Name : "?";
        }

        // ================================================================== fast travel
        // User 2026-11-13: players want to move fast by any means (NPCs, items), but judge whether it is needed.
        // Compares walking (map changes on the route) with each way to jump: a teleport scroll from anywhere to a
        // village or school (Char.useItemChangeMap), or the carriage NPC in a village/school (Char.npcUmayaki /
        // npcUmayaki_2), each followed by the walk from there. Used only when it saves FastTravelMinSavedHops.
        public static FastTrip PlanFastTravel(NinjaAgent a, PlayerState me, GameWorld world, int target, int walkHops, long now)
        {
            if (AgentConfig.FastTravelMinSavedHops <= 0 || now < a.NoFastTravelUntil || walkHops <= AgentConfig.FastTravelMinSavedHops)
                return null;
            int task = me.Record.TaskId, cur = me.MapId;
            FastTrip best = null;
            int bestCost = walkHops - AgentConfig.FastTravelMinSavedHops + 1;
            var bag = me.Inventory.Bag;
            int scroll = Array.FindIndex(bag, it => it != null && it.Id == PlayerItemUse.ItemVoHanKhaDi && (it.Expire <= 0 || it.Expire > now));
            if (scroll < 0)
                scroll = Array.FindIndex(bag, it => it != null && it.Id == PlayerItemUse.ItemKhaDi);
            if (scroll >= 0)
            {
                int extra = bag[scroll].Id == PlayerItemUse.ItemKhaDi ? AgentConfig.FastTravelScrollCostHops : 0;
                for (int m = 0; m < PlayerItemUse.KhaDiMaps.Length; m++)
                {
                    int dest = PlayerItemUse.KhaDiMaps[m];
                    if (dest == cur || !MapUnlock.CanEnter(task, dest))
                        continue;
                    var r = WorldKnowledge.Route(world, dest, target, task);
                    if (r == null)
                        continue;
                    int cost = 1 + r.Count + extra;
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = new FastTrip
                        {
                            Target = target,
                            Dest = dest,
                            BagIndex = scroll,
                            MenuIndex = m,
                            Label = world.FindItem(bag[scroll].Id).Name
                        };
                    }
                }
            }

            int npc = PlayerTasks.IsVillageMap(cur) ? PlayerTasks.NpcUmayakiId : PlayerTasks.IsSchoolMap(cur) ? PlayerTasks.NpcUmayaki2Id : -1;
            if (npc >= 0 && me.Zone.Npcs.Exists(n => n.TemplateId == npc))
                foreach (var d in npc == PlayerTasks.NpcUmayakiId ? PlayerTasks.UmayakiVillages : PlayerTasks.UmayakiSchools)
                {
                    if (d.Key == cur || !MapUnlock.CanEnter(task, d.Key))
                        continue;
                    var r = WorldKnowledge.Route(world, d.Key, target, task);
                    if (r == null)
                        continue;
                    int cost = 1 + r.Count;
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = new FastTrip
                        {
                            Target = target,
                            Dest = d.Key,
                            Npc = npc,
                            Label = d.Value
                        };
                    }
                }

            if (best != null)
                best.Saved = walkHops - bestCost;
            return best;
        }

        private static Status RunFastTravel(NinjaAgent a, PlayerState me, GameWorld world, out string why)
        {
            why = null;
            var f = a.Fast;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int from = me.MapId;
            string how;
            if (f.Npc >= 0)
            {
                how = "the carriage (" + NpcName(world, f.Npc) + ")";
                var go = GoToNpc(a, me, world, f.Npc, "going to " + NpcName(world, f.Npc) + " to ride to " + f.Label, out why);
                if (go == Status.Failed || me.MapId != from)
                {
                    a.Fast = null;
                    a.NoFastTravelUntil = now + 300000;
                    why = null;
                    return Status.Running;
                }

                if (go != Status.Done)
                    return Status.Running;
                if (now - a.LastMenuAt < ActionGapMs)
                    return Status.Running;
                a.LastMenuAt = now;
                a.Client.OpenMenu(f.Npc);
                int idx = me.Menus.FindIndex(e => e.Name == f.Label);
                if (idx >= 0)
                    a.Client.Menu(f.Npc, idx);
            }
            else
            {
                how = f.Label;
                var it = f.BagIndex >= 0 && f.BagIndex < me.Inventory.Bag.Length ? me.Inventory.Bag[f.BagIndex] : null;
                if (it == null || (it.Id != PlayerItemUse.ItemKhaDi && it.Id != PlayerItemUse.ItemVoHanKhaDi))
                {
                    a.Fast = null;
                    return Status.Running;
                }

                a.Activity = "using " + f.Label + " to go to " + MapName(world, f.Dest);
                a.Client.UseItemChangeMap(f.BagIndex, f.MenuIndex);
            }

            a.Fast = null;
            a.Route = null;
            if (me.MapId == f.Dest)
            {
                a.LastProgressAt = now;
                AgentManager.Remember(a, "took " + how + " from map " + from + " to " + MapName(world, f.Dest) + ", saving about " + f.Saved + " map changes");
                a.Knowledge.Learn("note:fasttravel:" + (f.Npc >= 0 ? "npc" : "item"), (f.Npc >= 0 ? NpcName(world, f.Npc) + " (in villages and schools) carries you to other villages or schools" : f.Label + " teleports you from anywhere to a village or school") + "; worth it for long trips");
            }
            else
            {
                a.NoFastTravelUntil = now + 300000;
                AgentManager.Remember(a, "could not take " + how + " to " + MapName(world, f.Dest) + ": " + a.Session.LastNotice);
            }

            return Status.Running;
        }

        /// <summary>One step towards (tx, ty), at most MoveStepPx (the server refuses more than Speed * 17 per move):
        /// x first, the floor below (TileGrid.CollisionY), near the target its floor. Returns true when arrived.</summary>
        private static bool WalkTo(NinjaAgent a, PlayerState me, GameWorld world, int tx, int ty, bool waypoint = false)
        {
            var grid = world.GetGrid(me.Zone.Tile);
            // stay 30 px off the map edge: outside a waypoint it is refused (IsSoftBlock) or kills (IsCrossMap), and
            // Zone.requestChangeMap finds the waypoint within 100 px anyway
            const int margin = 30;
            tx = Math.Max(margin, Math.Min(grid.PxW - 1 - margin, tx));
            int dx = tx - me.X;
            if (Math.Abs(dx) <= 8 && (ty < 0 || Math.Abs(ty - me.Y) <= 30))
                return true;
            // The destination itself (its floor, above a wood/rock tile); BotMovement plans the way over the platforms
            // (walk, jump, double jump, drop) instead of only walking straight at it.
            short y = -1;
            if (ty >= 0)
            {
                y = grid.CollisionY((short)tx, (short)Math.Max(0, Math.Min(grid.PxH - 1, ty - 24)));
                y = y >= grid.PxH - 1 ? (short)ty : OverObstacle(grid, tx, y);
            }

            a.Client.Move(tx, y);
            // A waypoint is reached within the server's box (TileMap.FindWaypoint: 100 px around it). Play mode 2026-11-13:
            // it used to ignore the height, so on map 6 bots asked to change map at (40,1238) under the exit at y 96-120
            // and the server refused silently, again and again.
            return Math.Abs(tx - me.X) <= 8 && (ty < 0 || Math.Abs(ty - me.Y) <= (waypoint ? 90 : 30));
        }

        // Find a proposed destination above a wood/rock tile. This is NOT an executable height change:
        // BotMovement must reach it through client-sized collision/gravity/jump steps (one double jump, as the client).
        internal static short OverObstacle(TileGrid grid, int x, short y)
        {
            for (int k = 0; k <= 6; k++)
            {
                int yy = y - k * 24;
                if (yy < 24)
                    break;
                if (!grid.IsWood(x, yy - 1) && !grid.IsRock(x, yy - 1))
                    return (short)yy;
            }

            return y;
        }

        private static void Wander(NinjaAgent a, PlayerState me, GameWorld world, int radius)
        {
            if (Rng.Next(3) == 0)
                return; // pauses look more human
            var grid = world.GetGrid(me.Zone.Tile);
            if (a.WanderX < 0 || Math.Abs(a.WanderX - me.X) <= 8)
                a.WanderX = Math.Max(60, Math.Min(grid.PxW - 60, me.X + Rng.Next(-radius, radius + 1)));
            if (WalkTo(a, me, world, a.WanderX, -1) || a.Client.CannotReach(a.WanderX, -1))
                a.WanderX = -1;
        }

        /// <summary>Travels to the closest unlocked map with the NPC and walks up to it. Done when next to it.</summary>
        private static Status GoToNpc(NinjaAgent a, PlayerState me, GameWorld world, int npc, string activity, out string why)
        {
            why = null;
            int map, x, y;
            if (!WorldKnowledge.FindNpc(world, npc, me.MapId, me.Record.TaskId, out map, out x, out y))
            {
                why = "cannot reach NPC " + npc + " from unlocked maps";
                return Status.Failed;
            }

            a.Activity = activity;
            if (me.MapId != map)
            {
                var t = Travel(a, me, world, map, out why);
                return t == Status.Failed ? Status.Failed : Status.Running;
            }

            a.Knowledge.Learn("place:npc" + npc, NpcName(world, npc) + " stands in map " + map + " " + me.Zone.Tile.Name);
            return WalkTo(a, me, world, x, y) || (Math.Abs(me.X - x) < 40 && Math.Abs(me.Y - y) < 60) ? Status.Done : Status.Running;
        }

        internal static string NpcName(GameWorld world, int npc)
        {
            string n;
            return NpcNames.TryGetValue(npc, out n) ? n : "NPC " + npc;
        }

        private static Status Explore(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, long now, out string why)
        {
            why = null;
            if (step.MapId < 0)
            {
                var n = WorldKnowledge.Neighbors(world, me.MapId, me.Record.TaskId);
                step.MapId = n.Count > 0 ? n[Rng.Next(n.Count)] : me.MapId;
            }

            if (me.MapId != step.MapId)
            {
                a.Activity = "exploring: on the way to map " + step.MapId;
                var t = Travel(a, me, world, step.MapId, out why);
                return t == Status.Failed ? Status.Failed : Status.Running;
            }

            a.Activity = "exploring " + me.Zone.Tile.Name;
            Wander(a, me, world, 400);
            if (now - Math.Max(a.StepStartedAt, a.LastProgressAt) < 20000)
                return Status.Running;
            var mobs = WorldKnowledge.MobsOf(world, me.MapId);
            string seen = "";
            for (int i = 0; i < mobs.Count && i < 3; i++)
                seen += (i > 0 ? ", " : "") + mobs[i].Name + " lv " + mobs[i].Level;
            a.Knowledge.Learn("map:" + me.MapId, "map " + me.MapId + " " + me.Zone.Tile.Name + (seen.Length > 0 ? ": " + seen : ": no monsters"));
            AgentManager.Report(a, AgentEvent.Note, "explored map " + me.MapId + " " + me.Zone.Tile.Name + (seen.Length > 0 ? " (monsters: " + seen + ")" : " (no monsters)"));
            return Status.Done;
        }

        private static Status Idle(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, long now)
        {
            a.Activity = "hanging around";
            Wander(a, me, world, 100);
            return now - a.StepStartedAt >= step.Seconds * 1000L ? Status.Done : Status.Running;
        }
    }
}
