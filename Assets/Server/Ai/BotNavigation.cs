using System.Collections.Generic;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;

    /// <summary>
    /// Platform path finding for AI players (offline addition, no Java counterpart: a human finds the way by looking).
    /// Every edge is found by simulating the same client frames BotMovement executes (TileMotion: walk one tile, jump,
    /// jump + double jump, falling off a ledge), so a planned path is exactly what the executor will do.
    /// </summary>
    internal static class BotNavigation
    {
        /// <summary>Movement kinds of one edge.</summary>
        public const byte Walk = 0, Jump = 1, DoubleJump = 2;
        /// <summary>Frames simulated per edge at most (a jump with double jump and a long fall fit easily).</summary>
        public static int MaxFrames = 120;
        /// <summary>Extra cost (frames) of a jump / double jump edge: a jump covers ground as fast as walking, so without
        /// a cost the cheapest way hops along flat ground (user: "why do the bots keep jumping").</summary>
        public static int JumpCost = 30, DoubleJumpCost = 45;

        public struct Edge
        {
            public sbyte Dir; public byte Kind; public short FromX, FromY, ToX, ToY; public int Frames;
        }

        /// <summary>One client frame of BotMovement (shared by the planner and the executor so they never disagree).</summary>
        public static void Frame(TileGrid grid, ref int x, ref int y, ref int vy, ref bool doubled, int dx, bool wantDoubleJump)
        {
            // GameScr.updateKeyInGame: double jump once per jump while still rising with cvy > -4 (cvy = -7)
            if (wantDoubleJump && !doubled && vy < 0 && vy > TileMotion.AirJumpThreshold && !TileMotion.Supported(grid, x, y))
            { vy = TileMotion.AirJumpVelocity; doubled = true; }
            x = TileMotion.Horizontal(grid, x, y, dx);
            bool hit;
            y = TileMotion.Vertical(grid, x, y, vy, out hit);
            if (hit) vy = 0;
            if (!TileMotion.Supported(grid, x, y) || vy < 0) vy = Math.Min(TileMotion.MaxFallSpeed, vy + TileMotion.Gravity);
        }

        /// <summary>A position the server accepts and the character can stand on (PlayerActions.IsSoftBlock / IsCrossMap).</summary>
        public static bool Standable(TileGrid grid, int x, int y)
        {
            return x >= 24 && x <= grid.PxW - 24 && y > 0 && y < grid.PxH && !FallsOut(grid, x, y) && TileMotion.Supported(grid, x, y) &&
                !grid.IsWood(x, y - 1) && !grid.IsRock(x, y - 1);   // solid ground above the feet does not stop a client character
        }

        /// <summary>PlayerActions.IsCrossMap's bottom rule: the last tile row kills unless the character stands on a T_TOP
        /// tile there (offline deviation for maps whose last row is ground). Play mode 2026-11-13: map 6's bottom row is
        /// ground and holds the exit to map 20; bots arriving there had no graph node (the last row was always treated as
        /// lethal here) and stood still forever. Map tile 4 kills in the last two rows (Java).</summary>
        public static bool FallsOut(TileGrid grid, int x, int y)
        {
            if (grid.TileId == 4 && y >= grid.PxH - 48) return true;
            return y >= grid.PxH - 24 && !grid.TileTypeAt(x, y, TileGrid.T_TOP);
        }

        /// <summary>Simulates one edge from a standing position; false when it is refused, blocked or lethal.</summary>
        public static bool Simulate(TileGrid grid, int x0, int y0, int dir, byte kind, int speed, out Edge e)
        {
            e = new Edge { Dir = (sbyte)dir, Kind = kind, FromX = (short)x0, FromY = (short)y0 };
            int x = x0, y = y0, vy = kind == Walk ? 0 : TileMotion.JumpVelocity;
            bool doubled = false;
            for (int f = 1; f <= MaxFrames; f++)
            {
                int px = x;
                Frame(grid, ref x, ref y, ref vy, ref doubled, dir * speed, kind == DoubleJump);
                if (FallsOut(grid, x, y) || x < 24 || x > grid.PxW - 24) return false;    // falling out kills, edges refuse
                if (grid.IsWood(x, y - 1) || grid.IsRock(x, y - 1)) return false;            // the server puts it back
                bool grounded = TileMotion.Supported(grid, x, y) && vy == 0;
                if (!grounded) continue;
                if (kind == Walk && x == px && Math.Abs(x - x0) < 24) return false;           // walked into a wall
                if (kind == Walk && Math.Abs(x - x0) < 24 && y == y0) continue;               // still walking the tile
                if (x == x0 && y == y0) return false;                                         // went nowhere
                e.ToX = (short)x; e.ToY = (short)y; e.Frames = f + (kind == Walk ? 0 : kind == Jump ? JumpCost : DoubleJumpCost);   // players walk; they jump when needed
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ map graph (built once per map and speed)
        // Nodes are 24 px tile cells; an edge starts at the cell centre (x = col * 24 + 12) and is simulated from there.
        // The executor first walks to the centre of its cell, so the graph does not depend on where it stands exactly.
        // The graph is built on a worker thread (TileGrid is read-only after loading); until it is ready the executor
        // walks straight (Approach) as before.

        private sealed class Graph { public readonly Dictionary<long, Edge[]> Edges = new Dictionary<long, Edge[]>(); }
        private static readonly Dictionary<TileGrid, Dictionary<int, Graph>> graphs = new Dictionary<TileGrid, Dictionary<int, Graph>>();
        private static readonly object graphLock = new object();
        private static readonly int[] Dirs = { -1, 1, 0 };

        public static int Centre(int x) { return x / 24 * 24 + 12; }
        private static long Cell(int x, int y) { return ((long)(x / 24) << 20) | (long)(y / 24); }

        /// <summary>The graph of a map for a speed, or null while it is being built (the build starts on first use).</summary>
        private static Graph GetGraph(TileGrid grid, int speed)
        {
            lock (graphLock)
            {
                Dictionary<int, Graph> bySpeed;
                if (!graphs.TryGetValue(grid, out bySpeed)) { bySpeed = new Dictionary<int, Graph>(); graphs[grid] = bySpeed; }
                Graph g;
                if (bySpeed.TryGetValue(speed, out g)) return g;   // null value = being built
                bySpeed[speed] = null;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                Graph built = null;
                try { built = Build(grid, speed); }
                catch (System.Exception) { built = new Graph(); }   // an empty graph: straight walking only
                lock (graphLock) graphs[grid][speed] = built;
            });
            return null;
        }

        private static Graph Build(TileGrid grid, int speed)
        {
            var g = new Graph();
            var found = new List<Edge>(8);
            for (int cx = 12; cx < grid.PxW; cx += 24)
                for (int y = 24; y < grid.PxH; y += 24)
                {
                    if (!Standable(grid, cx, y)) continue;
                    found.Clear();
                    Edge e;
                    foreach (int d in Dirs)
                    {
                        if (d != 0 && Simulate(grid, cx, y, d, Walk, speed, out e)) found.Add(e);
                        if (Simulate(grid, cx, y, d, Jump, speed, out e)) found.Add(e);
                        if (Simulate(grid, cx, y, d, DoubleJump, speed, out e)) found.Add(e);
                    }
                    // keep one edge per destination cell (the fastest); landing in a cell that cannot be stood on is useless
                    var best = new Dictionary<long, Edge>();
                    foreach (var f in found)
                    {
                        if (!Standable(grid, Centre(f.ToX), f.ToY)) continue;
                        long k = Cell(f.ToX, f.ToY); Edge old;
                        if (k == Cell(cx, y)) continue;
                        if (!best.TryGetValue(k, out old) || f.Frames < old.Frames) best[k] = f;
                    }
                    var arr = new Edge[best.Count]; best.Values.CopyTo(arr, 0);
                    g.Edges[Cell(cx, y)] = arr;
                }
            return g;
        }

        /// <summary>Key of the 24 px cell of a position (as used by Reachable).</summary>
        public static long CellKey(int x, int y) { return Cell(x, y); }

        /// <summary>Every cell the character can get to from where it stands (walk, jump, double jump, drop), or null
        /// while the map graph is being built.</summary>
        public static HashSet<long> Reachable(TileGrid grid, int sx, int sy, int speed)
        {
            var graph = speed > 0 ? GetGraph(grid, speed) : null;
            if (graph == null) return null;
            var seen = new HashSet<long>();
            var todo = new Queue<long>();
            long start = Cell(Centre(sx), sy);
            seen.Add(start); todo.Enqueue(start);
            while (todo.Count > 0)
            {
                Edge[] edges;
                if (!graph.Edges.TryGetValue(todo.Dequeue(), out edges)) continue;
                foreach (var e in edges)
                {
                    long k = Cell(Centre(e.ToX), e.ToY);
                    if (seen.Add(k)) todo.Enqueue(k);
                }
            }
            return seen;
        }

        /// <summary>False while the map's graph is still being built.</summary>
        public static bool Ready(TileGrid grid, int speed) { return GetGraph(grid, speed) != null; }

        private sealed class Node { public int X, Y, G; public Node Parent; public Edge Via; }

        /// <summary>
        /// Shortest way (in frames) from a standing position to (tx, ty) (ty &lt; 0 = any height) over the map graph. The
        /// first edge starts at the centre of the current cell. When the target cannot be reached: the way to the closest
        /// cell and reachable = false (empty when nothing is closer). Null while the graph is being built.
        /// </summary>
        public static List<Edge> Find(TileGrid grid, int sx, int sy, int tx, int ty, int speed, out bool reachable)
        {
            reachable = false;
            if (speed <= 0) return new List<Edge>();
            var graph = GetGraph(grid, speed);
            if (graph == null) return null;
            var open = new List<Node>();
            var closed = new HashSet<long>();
            var start = new Node { X = Centre(sx), Y = sy };
            open.Add(start);
            Node best = start; int bestH = Heuristic(start.X, sy, tx, ty, speed);
            while (open.Count > 0)
            {
                int bi = 0, bf = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int fv = open[i].G + Heuristic(open[i].X, open[i].Y, tx, ty, speed);
                    if (fv < bf) { bf = fv; bi = i; }
                }
                var n = open[bi]; open[bi] = open[open.Count - 1]; open.RemoveAt(open.Count - 1);
                if (!closed.Add(Cell(n.X, n.Y))) continue;
                int h = Heuristic(n.X, n.Y, tx, ty, speed);
                if (h < bestH) { bestH = h; best = n; }
                if (Math.Abs(n.X - tx) <= 24 && (ty < 0 || Math.Abs(n.Y - ty) <= 30)) { best = n; reachable = true; break; }
                Edge[] edges;
                if (!graph.Edges.TryGetValue(Cell(n.X, n.Y), out edges)) continue;
                foreach (var e in edges)
                {
                    int ex = Centre(e.ToX);
                    if (!closed.Contains(Cell(ex, e.ToY)))
                        open.Add(new Node { X = ex, Y = e.ToY, G = n.G + e.Frames + Math.Abs(e.ToX - ex) / Math.Max(1, speed) + 1, Parent = n, Via = e });
                }
            }
            var path = new List<Edge>();
            for (var n = best; n.Parent != null; n = n.Parent) path.Add(n.Via);
            path.Reverse();
            return path;
        }

        // frames at full speed horizontally plus a rough vertical cost (a jump climbs about 50-80 px)
        private static int Heuristic(int x, int y, int tx, int ty, int speed)
        {
            int h = Math.Max(0, Math.Abs(x - tx) - 24) / Math.Max(1, speed);
            if (ty >= 0) h += Math.Max(0, Math.Abs(y - ty) - 30) / 8;
            return h;
        }
    }
}
