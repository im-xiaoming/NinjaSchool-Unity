using System.Collections.Generic;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using TileMap = NsoServer.World.TileMap;
    using Waypoint = NsoServer.World.Waypoint;

    /// <summary>
    /// What an AI player "knows" about the world, read from the static map data (what a player sees on the world map):
    /// which normal maps connect through waypoints, which monsters and NPCs live where, and where a character arrives
    /// when it walks from one map into another (Zone.requestChangeMap: the waypoint's X/Y are the arrival point).
    /// Routes only use maps the character's main quest has unlocked (MapUnlock.CanEnter, the rule the server applies to
    /// every map change), so an AI player never plans a trip a player could not make. Used by both layers.
    /// Static data never changes at runtime, so results are cached. Call under GameWorld.SyncRoot.
    /// </summary>
    public static class WorldKnowledge
    {
        /// <summary>A monster kind living on a map.</summary>
        public struct MobKind
        {
            public int TemplateId, Level;
            public string Name;
            public bool IsBoss;
        }

        private static GameWorld cachedFor;
        private static readonly Dictionary<int, List<int>> Links = new Dictionary<int, List<int>>();
        private static readonly Dictionary<int, List<MobKind>> Mobs = new Dictionary<int, List<MobKind>>();
        private static void Ensure(GameWorld world)
        {
            if (cachedFor == world)
                return;
            Links.Clear();
            Mobs.Clear();
            cachedFor = world;
        }

        /// <summary>A map an agent may walk into: normal (not a special area) and with at least one zone.</summary>
        public static bool IsWalkable(GameWorld world, int mapId)
        {
            var tile = world.FindMap(mapId);
            return tile != null && tile.IsNormal && world.RandomZoneId(mapId) >= 0;
        }

        /// <summary>Normal maps directly reachable through a waypoint of this map.</summary>
        public static List<int> Neighbors(GameWorld world, int mapId)
        {
            Ensure(world);
            List<int> list;
            if (Links.TryGetValue(mapId, out list))
                return list;
            list = new List<int>();
            var tile = world.FindMap(mapId);
            if (tile != null)
                foreach (var wp in tile.Waypoints)
                    if (wp.Next >= 0 && wp.Next != mapId && !list.Contains(wp.Next) && IsWalkable(world, wp.Next))
                        list.Add(wp.Next);
            Links[mapId] = list;
            return list;
        }

        /// <summary>Monster kinds of a map (template, level, name), without duplicates.</summary>
        public static List<MobKind> MobsOf(GameWorld world, int mapId)
        {
            Ensure(world);
            List<MobKind> list;
            if (Mobs.TryGetValue(mapId, out list))
                return list;
            list = new List<MobKind>();
            var tile = world.FindMap(mapId);
            if (tile != null)
                foreach (var spawn in tile.Monsters)
                {
                    bool seen = false;
                    foreach (var k in list)
                        if (k.TemplateId == spawn.TemplateId)
                            seen = true;
                    if (seen)
                        continue;
                    var t = world.FindMob(spawn.TemplateId);
                    if (t == null)
                        continue;
                    list.Add(new MobKind { TemplateId = t.Id, Level = t.Level, Name = t.Name, IsBoss = t.IsBoss });
                }

            Mobs[mapId] = list;
            return list;
        }

        /// <summary>Neighbours the character may enter with its main quest <paramref name = "taskId"/> (MapUnlock).</summary>
        public static List<int> Neighbors(GameWorld world, int mapId, int taskId)
        {
            var list = new List<int>();
            foreach (int n in Neighbors(world, mapId))
                if (MapUnlock.CanEnter(taskId, n))
                    list.Add(n);
            return list;
        }

        /// <summary>Breadth-first route through waypoints over unlocked maps: the maps to walk into, excluding
        /// <paramref name = "from"/>. Empty when already there, null when unreachable or locked.</summary>
        public static List<int> Route(GameWorld world, int from, int to, int taskId)
        {
            if (from == to)
                return new List<int>();
            if (!MapUnlock.CanEnter(taskId, to))
                return null;
            var prev = new Dictionary<int, int>
            {
                {
                    from,
                    -1
                }
            };
            var queue = new Queue<int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int m = queue.Dequeue();
                foreach (int n in Neighbors(world, m, taskId))
                {
                    if (prev.ContainsKey(n))
                        continue;
                    prev[n] = m;
                    if (n == to)
                    {
                        var path = new List<int>();
                        for (int x = to; x != from; x = prev[x])
                            path.Insert(0, x);
                        return path;
                    }

                    queue.Enqueue(n);
                }
            }

            return null;
        }

        /// <summary>Maps within <paramref name = "hops"/> waypoint hops, with their distance (the start map has 0).</summary>
        public static Dictionary<int, int> Nearby(GameWorld world, int from, int hops, int taskId)
        {
            var dist = new Dictionary<int, int>
            {
                {
                    from,
                    0
                }
            };
            var queue = new Queue<int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int m = queue.Dequeue();
                if (dist[m] >= hops)
                    continue;
                foreach (int n in Neighbors(world, m, taskId))
                    if (!dist.ContainsKey(n))
                    {
                        dist[n] = dist[m] + 1;
                        queue.Enqueue(n);
                    }
            }

            return dist;
        }

        /// <summary>The closest unlocked map (fewest hops, the current map first) where an NPC template stands, with its
        /// position; false when none is reachable.</summary>
        public static bool FindNpc(GameWorld world, int npcTemplate, int from, int taskId, out int map, out int x, out int y)
        {
            foreach (var kv in Ordered(Nearby(world, from, 30, taskId)))
            {
                var tile = world.FindMap(kv);
                if (tile == null)
                    continue;
                foreach (var n in tile.Npcs)
                    if (n.TemplateId == npcTemplate)
                    {
                        map = kv;
                        x = n.X;
                        y = n.Y;
                        return true;
                    }
            }

            map = -1;
            x = y = 0;
            return false;
        }

        /// <summary>The closest unlocked map where a monster template lives (-1 when none).</summary>
        public static int FindMobMap(GameWorld world, int mobTemplate, int from, int taskId, int preferred = -1)
        {
            if (preferred >= 0 && MapUnlock.CanEnter(taskId, preferred) && MobsOf(world, preferred).Exists(k => k.TemplateId == mobTemplate) && Route(world, from, preferred, taskId) != null)
                return preferred;
            foreach (var kv in Ordered(Nearby(world, from, 30, taskId)))
                if (MobsOf(world, kv).Exists(k => k.TemplateId == mobTemplate))
                    return kv;
            return -1;
        }

        private static List<int> Ordered(Dictionary<int, int> dist)
        {
            var list = new List<int>(dist.Keys);
            list.Sort((a, b) => dist[a].CompareTo(dist[b]));
            return list;
        }

        /// <summary>The waypoint of <paramref name = "tile"/> that leads to <paramref name = "next"/>, or null.</summary>
        public static Waypoint ExitTo(TileMap tile, int next)
        {
            foreach (var wp in tile.Waypoints)
                if (wp.Next == next)
                    return wp;
            return null;
        }

        /// <summary>Where a character walking from <paramref name = "from"/> appears in <paramref name = "to"/>
        /// (Zone.requestChangeMap: setXY(wp.x, wp.y) of the source waypoint), on the floor.</summary>
        public static void Arrival(GameWorld world, int from, int to, out short x, out short y)
        {
            var src = world.FindMap(from);
            var dst = world.FindMap(to);
            var grid = world.GetGrid(dst);
            var wp = src != null ? ExitTo(src, to) : null;
            int ax, ay;
            if (wp != null)
            {
                ax = wp.X;
                ay = wp.Y;
            }
            else if (dst.LocationStand.Count > 0 && dst.LocationStand[0].Length >= 2)
            {
                ax = dst.LocationStand[0][0];
                ay = dst.LocationStand[0][1];
            }
            else
            {
                ax = grid.PxW / 2;
                ay = 0;
            }

            ax = Math.Max(24, Math.Min(grid.PxW - 25, ax));
            x = (short)ax;
            y = grid.CollisionY(x, (short)Math.Max(0, Math.Min(grid.PxH - 1, ay)));
        }
    }
}
