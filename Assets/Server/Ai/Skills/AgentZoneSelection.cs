using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Zone = NsoServer.World.Zone;

    /// <summary>Population-based hunting-zone choice using the same list counts a player sees. NPC walking and
    /// OPEN_UI_ZONE / ZONE_CHANGE remain normal client requests; no direct Join or private-zone creation.</summary>
    public static class AgentZoneSelection
    {
        public static NpcSpawn Prepare(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (!AgentConfig.SpreadHuntingZones || me.Zone.IsInstance || me.Zone.Tile.IsNotChangeZone || me.IsDead || me.IsChangeMap)
            {
                Cancel(a);
                return null;
            }

            if (a.ZoneTarget >= 0 && (a.ZoneMap != me.MapId || a.ZoneTarget == me.Zone.Id || now - a.ZoneStartedAt > AgentConfig.ZoneApproachTimeoutSeconds * 1000L))
            {
                Cancel(a);
                a.LastZoneCheck = now;
                return null;
            }

            NpcSpawn npc = null;
            int distance = int.MaxValue;
            foreach (var n in me.Zone.Npcs)
            {
                if (n.TemplateId != 13)
                    continue; // PlayerActions.OpenUiZone's real NPC gate
                int d = Math.Abs(n.X - me.X) + 2 * Math.Abs(n.Y - me.Y);
                if (d < distance)
                {
                    distance = d;
                    npc = n;
                }
            }

            if (npc == null)
            {
                Cancel(a);
                return null;
            }

            if (a.ZoneTarget >= 0)
            {
                a.ZoneIntentAt = now;
                return npc;
            }

            if (now - a.LastZoneCheck < Math.Max(1, AgentConfig.ZoneCheckSeconds) * 1000L || now - a.LastZoneChange < Math.Max(1, AgentConfig.ZoneChangeCooldownSeconds) * 1000L)
                return null;
            a.LastZoneCheck = now;
            var zones = world.GetZones(me.MapId);
            if (zones == null || zones.Count < 2)
                return null;
            int current = AgentManager.ProjectedZonePopulation(me.MapId, me.Zone, now);
            if (current < AgentConfig.ZoneCrowdingThreshold)
                return null;
            Zone best = null;
            int bestPopulation = current;
            // Rotate equal-count preferences by personality index, while reservations prevent a herd following it.
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[(i + a.Index) % zones.Count];
                if (z == me.Zone || z.IsInstance || z.NumberChar >= PlayerActions.ZoneCapacity(me.MapId))
                    continue;
                int population = AgentManager.ProjectedZonePopulation(me.MapId, z, now);
                if (population < bestPopulation)
                {
                    bestPopulation = population;
                    best = z;
                }
            }

            if (best == null || current - bestPopulation < Math.Max(1, AgentConfig.ZonePopulationImprovement))
                return null;
            a.ZoneTarget = best.Id;
            a.ZoneMap = me.MapId;
            a.ZoneStartedAt = a.ZoneIntentAt = now;
            return npc;
        }

        public static void Cancel(NinjaAgent a)
        {
            a.ZoneTarget = a.ZoneMap = -1;
            a.ZoneIntentAt = 0;
        }
    }
}
