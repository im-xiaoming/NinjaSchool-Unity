using System;
using System.Collections.Generic;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>Offline behaviour, not a gameplay privilege: choose eligible loot and avoid contested/stalled walks.</summary>
    public static class AgentForaging
    {
        public static bool CanTake(PlayerState me, GameWorld world, ItemMapEntry it, long now)
        {
            if (it.PickedUp || !TaskMechanics.CanSee(me, it) || !GroundLootPolicy.HasOwnership(me.Record.Id, it, now)) return false;
            var t = world.FindItem(it.Item.Id);
            if (t == null) return false;
            return PlayerItems.CanReceiveGroundItem(me, t, it.Item);
        }

        public static ItemMapEntry Choose(NinjaAgent a, PlayerState me, GameWorld world, int radius, long now)
        {
            var expired = new List<ItemMapEntry>();
            foreach (var kv in a.AvoidLoot) if (kv.Value <= now || kv.Key.PickedUp) expired.Add(kv.Key);
            foreach (var it in expired) a.AvoidLoot.Remove(it);
            ItemMapEntry best = null; int bestScore = int.MaxValue;
            var task = world.FindTask(me.Record.TaskId);
            int idx = me.Progress.TaskIndex;
            int wanted = me.Progress.TaskActive && task != null && idx >= 0 && idx < task.PickItems.Length ? task.PickItems[idx] : -1;
            foreach (var it in me.Zone.ItemMaps)
            {
                int distance = Math.Abs(it.X - me.X) + Math.Abs(it.Y - me.Y);
                if (distance > radius || Math.Abs(it.Y - me.Y) > AgentConfig.LootVerticalRangePx || a.AvoidLoot.ContainsKey(it) || !CanTake(me, world, it, now)) continue;
                if (AgentManager.LootClaimed(a, it.Id)) continue; // a courtesy between bots, never a server pickup lock
                int score = distance - (it.Item.Id == wanted ? 2000 : 0) - (it.OwnerId == me.Record.Id ? 1000 : 0);
                if (it == a.LootTarget) score -= 100; // finish walking instead of oscillating between equal-distance items
                if (score < bestScore) { bestScore = score; best = it; }
            }
            if (best == null) { Release(a); return null; }
            int d = Math.Abs(best.X - me.X) + Math.Abs(best.Y - me.Y);
            if (best != a.LootTarget)
            {
                a.LootTarget = best; a.LootBestDistance = d; a.LootProgressAt = now;
            }
            else if (d < a.LootBestDistance - 3) { a.LootBestDistance = d; a.LootProgressAt = now; }
            else if (now - a.LootProgressAt > Math.Max(1, AgentConfig.LootStallSeconds) * 1000L)
            {
                if (a.AvoidLoot.Count >= 32) a.AvoidLoot.Clear();
                a.AvoidLoot[best] = now + Math.Max(1, AgentConfig.LootRetrySeconds) * 1000L;
                Release(a); return null;
            }
            a.TargetItem = best.Id;
            return best;
        }

        public static void Release(NinjaAgent a) { a.TargetItem = -1; a.LootTarget = null; }
    }
}
