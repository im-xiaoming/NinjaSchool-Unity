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
        private static bool TryLootHabit(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            var step = a.Step;
            if (a.ZoneTarget >= 0 || a.Admitting || me.Menus.Count > 0 || me.CatchUntil > now || me.FishUntil > now || (step != null && (step.Goal == GoalType.Hunt || step.Goal == GoalType.Collect)))
                return false;
            // Quest hunts count their dead target before foraging; keep that executor's bookkeeping in one place.
            if (step != null && step.Goal == GoalType.Quest)
            {
                var q = world.FindTask(me.Record.TaskId);
                int index = me.Progress.TaskIndex;
                if (q != null && me.Progress.TaskActive && index >= 0 && ((index < q.KillMobs.Length && q.KillMobs[index][0] >= 0) || (index < q.PickItems.Length && q.PickItems[index] >= 0)))
                    return false;
            }

            var item = AgentForaging.Choose(a, me, world, AgentConfig.LootScanRadiusPx, now);
            if (item == null)
                return false;
            PickUp(a, me, world, item);
            return true;
        }

        private static bool TrySpreadZone(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            var npc = AgentZoneSelection.Prepare(a, me, world, now);
            if (npc == null)
                return false;
            a.TargetMob = -1;
            AgentForaging.Release(a);
            a.Activity = "walking to zone NPC for quieter zone " + a.ZoneTarget;
            if (Math.Abs(me.X - npc.X) >= 50 || Math.Abs(me.Y - npc.Y) >= 50)
            {
                WalkTo(a, me, world, npc.X, npc.Y);
                return true;
            }

            int target = a.ZoneTarget;
            int before = me.Zone.Id;
            a.Client.OpenZones(); // PlayerActions.OpenUiZone checks the same NPC proximity as the human UI.
            a.Client.ChangeZone(target);
            AgentZoneSelection.Cancel(a);
            a.LastZoneCheck = now;
            if (me.Zone.Id != before)
            {
                a.LastZoneChange = now;
                a.LastProgressAt = now;
                a.Route = null;
                a.LastTargetSeen = 0;
                AgentManager.Remember(a, "changed from zone " + before + " to quieter zone " + me.Zone.Id + " on map " + me.MapId);
            }

            return true;
        }

        // walk to the item, then Service.pickItem (the server checks distance, owner and bag space)
        private static bool PickUp(NinjaAgent a, PlayerState me, GameWorld world, ItemMapEntry it)
        {
            a.TargetItem = it.Id;
            a.Activity = "picking up an item";
            // Char.pickItem accepts anything inside 150 px; do not get stuck walking to its exact floor height.
            if (!PlayerItems.IsWithinPickupRange(me, it))
            {
                WalkTo(a, me, world, it.X, it.Y);
                return false;
            }

            var t = world.FindItem(it.Item.Id);
            int qty = it.Item.Quantity;
            a.Client.Pick(it.Id);
            if (!it.PickedUp)
                return false;
            AgentForaging.Release(a);
            a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (t != null)
                AgentManager.OnPicked(a, world, t, qty);
            return true;
        }

        private static Status Collect(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, long now)
        {
            var it = AgentForaging.Choose(a, me, world, 2000, now);
            if (it == null)
            {
                if (a.StepProgress > 0 || now - a.StepStartedAt > 6000)
                {
                    AgentManager.Report(a, AgentEvent.Loot, a.StepProgress > 0 ? "picked up " + a.StepProgress + " items (yen now " + me.Progress.Yen + ")" : "found nothing to pick up");
                    return Status.Done;
                }

                a.Activity = "looking for items";
                Wander(a, me, world, 200);
                return Status.Running;
            }

            if (PickUp(a, me, world, it))
            {
                a.LastProgressAt = now;
                if (++a.StepProgress >= step.Count)
                {
                    AgentManager.Report(a, AgentEvent.Loot, "picked up " + a.StepProgress + " items (yen now " + me.Progress.Yen + ")");
                    return Status.Done;
                }
            }

            return Status.Running;
        }
    }
}
