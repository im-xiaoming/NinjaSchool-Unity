using System.Collections.Generic;
using System.Text;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using TileMap = NsoServer.World.TileMap;   // the client defines a global TileMap (AGENTS.md pitfall)

    /// <summary>
    /// The current map as the LLM sees it (offline addition, no Java counterpart): the AI player's own position and the
    /// coordinates of every object a player sees on the screen or the map: exits, NPCs, monsters, ground items and other
    /// players. Pixels; x grows to the right, y grows downwards (y is the floor under the feet). Exits and NPCs say when
    /// the path finder knows no way there from where the AI stands. Built on the game thread from the live zone.
    /// </summary>
    internal static class AgentMapView
    {
        /// <summary>Monster positions listed per kind (nearest first).</summary>
        public static int MobPositionsPerKind = 5;
        /// <summary>Ground items listed (nearest first).</summary>
        public static int ItemsListed = 10;
        /// <summary>Largest terrain grid sent (cells); bigger maps are drawn with 2x2 / 3x3 cells per character.</summary>
        public static int MaxGridCells = 4000;

        /// <summary>Legend of the terrain grid (part of the prompt).</summary>
        public const string GridLegend =
            "TERRAIN of the whole map, one character per cell of S x S pixels (S given below); column c covers x = c*S .. c*S+S-1, " +
            "row r is y = r*S (a standing character's y is the row of the floor it stands on). " +
            "_ floor you can reach from where you are now; x floor you CANNOT reach from here; # solid ground/wall (not passable); " +
            "w wood/rock obstacle (not passable, jump over); . open air (passable, you fall through it); " +
            "@ you; E exit to another map; N NPC; M monster; i item on the ground; P another player.\n";

        public static string Build(NinjaAgent a, PlayerState me, GameWorld world)
        {
            var b = new StringBuilder(1200);
            var zone = me.Zone;
            var tile = zone.Tile;
            var grid = world.GetGrid(tile);
            int speed = Math.Max(1, Math.Min(me.Speed, AgentConfig.MoveStepPx / 12));
            b.Append("size ").Append(grid.PxW).Append('x').Append(grid.PxH).Append(", zone ").Append(zone.Id)
             .Append(". YOU at (").Append(me.X).Append(',').Append(me.Y).Append(")\n");

            b.Append("exits:");
            if (tile.Waypoints.Count == 0) b.Append(" none");
            foreach (var w in tile.Waypoints)
            {
                var next = world.FindMap(w.Next);
                int x = (w.MinX + w.MaxX) / 2, y = w.MaxY;
                b.Append(" to map ").Append(w.Next).Append(" \"").Append(next != null ? next.Name : "?").Append("\" at (")
                 .Append(x).Append(',').Append(y).Append(')')
                 .Append(MapUnlock.CanEnter(me.Record.TaskId, w.Next) ? "" : " LOCKED by quest").Append(Way(grid, me, x, y, speed)).Append(';');
            }
            b.Append('\n');

            b.Append("npcs:");
            if (tile.Npcs.Count == 0) b.Append(" none");
            foreach (var n in tile.Npcs)
                b.Append(' ').Append(AgentTactics.NpcName(world, n.TemplateId)).Append(" (id ").Append(n.TemplateId).Append(") at (")
                 .Append(n.X).Append(',').Append(n.Y).Append(')').Append(Way(grid, me, n.X, n.Y, speed)).Append(';');
            b.Append('\n');

            // monsters by kind: alive count and the nearest positions
            var kinds = new Dictionary<int, List<Mob>>();
            foreach (var m in zone.Mobs)
            {
                List<Mob> list;
                if (!kinds.TryGetValue(m.Template.Id, out list)) { list = new List<Mob>(); kinds[m.Template.Id] = list; }
                list.Add(m);
            }
            b.Append("monsters:");
            if (kinds.Count == 0) b.Append(" none");
            foreach (var kv in kinds)
            {
                var list = kv.Value;
                list.Sort((p, q) => Dist(me, p.X, p.Y).CompareTo(Dist(me, q.X, q.Y)));
                int alive = 0; foreach (var m in list) if (!m.IsDead) alive++;
                var t = list[0].Template;
                b.Append(' ').Append(t.Name).Append(" (id ").Append(t.Id).Append(", lv ").Append(list[0].Level).Append(t.IsBoss ? ", BOSS" : "")
                 .Append(") ").Append(alive).Append('/').Append(list.Count).Append(" alive at");
                int shown = 0;
                foreach (var m in list)
                {
                    if (m.IsDead) continue;
                    if (shown++ >= MobPositionsPerKind) { b.Append(" ..."); break; }
                    b.Append(" (").Append(m.X).Append(',').Append(m.Y).Append(m.Chars.Contains(me.Record.Id) ? ",hostile to you" : "").Append(')');
                }
                b.Append(';');
            }
            b.Append('\n');

            var items = new List<ItemMapEntry>();
            foreach (var it in zone.ItemMaps) if (!it.PickedUp && TaskMechanics.CanSee(me, it)) items.Add(it);
            items.Sort((p, q) => Dist(me, p.X, p.Y).CompareTo(Dist(me, q.X, q.Y)));
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            b.Append("ground items:");
            if (items.Count == 0) b.Append(" none");
            for (int i = 0; i < items.Count && i < ItemsListed; i++)
            {
                var it = items[i];
                var t = world.FindItem(it.Item.Id);
                bool mine = it.OwnerId == me.Record.Id, free = GroundLootPolicy.HasOwnership(me.Record.Id, it, now);
                b.Append(' ').Append(t != null ? t.Name : "item " + it.Item.Id).Append(" at (").Append(it.X).Append(',').Append(it.Y).Append(')')
                 .Append(mine ? " yours" : free ? " free" : " another player's").Append(';');
            }
            if (items.Count > ItemsListed) b.Append(" +").Append(items.Count - ItemsListed).Append(" more");
            b.Append('\n');

            b.Append("players:");
            int others = 0;
            foreach (PlayerState p in zone.Players)
            {
                if (p == me) continue;
                others++;
                b.Append(' ').Append(p.Record.Name).Append(p.Session != null && p.Session.IsBot ? "" : " (human)").Append(" lv ").Append(p.Level)
                 .Append(" at (").Append(p.X).Append(',').Append(p.Y).Append(");");
            }
            if (others == 0) b.Append(" none");
            b.Append('\n');
            return b.ToString();
        }

        /// <summary>The terrain grid alone (sent separately so it can be dropped when the context is full).</summary>
        public static string BuildTerrain(NinjaAgent a, PlayerState me, GameWorld world)
        {
            var b = new StringBuilder(2500);
            var tile = me.Zone.Tile;
            Terrain(b, a, me, world, world.GetGrid(tile), tile, Math.Max(1, Math.Min(me.Speed, AgentConfig.MoveStepPx / 12)));
            return b.ToString();
        }

        // The terrain grid: every cell of the map, which floors the character can reach from here (BotNavigation), and
        // the objects drawn on it. Rows are prefixed with their y so coordinates can be read off directly.
        private static void Terrain(StringBuilder b, NinjaAgent a, PlayerState me, GameWorld world, TileGrid grid, TileMap tile, int speed)
        {
            int f = 1;
            while ((grid.Tmw / f) * (grid.Tmh / f) > MaxGridCells && f < 4) f++;
            int cols = (grid.Tmw + f - 1) / f, rows = (grid.Tmh + f - 1) / f, size = 24 * f;
            var reach = BotNavigation.Reachable(grid, me.X, me.Y, speed);
            var cells = new char[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char best = '.';
                    for (int dr = 0; dr < f; dr++)
                        for (int dc = 0; dc < f; dc++)
                        {
                            int tc = c * f + dc, tr = r * f + dr;
                            if (tc >= grid.Tmw || tr >= grid.Tmh) continue;
                            int x = tc * 24 + 12, y = tr * 24;
                            char ch = '.';
                            if (BotNavigation.Standable(grid, x, y))
                                ch = reach == null || reach.Contains(BotNavigation.CellKey(x, y)) ? '_' : 'x';
                            else if (grid.IsWood(x, y + 1) || grid.IsRock(x, y + 1)) ch = 'w';
                            else if (grid.TileTypeAt(x, y + 1, TileGrid.T_SOLIDGROUND)) ch = '#';
                            if (Rank(ch) > Rank(best)) best = ch;
                        }
                    cells[r, c] = best;
                }
            System.Action<int, int, char> mark = (x, y, ch) =>
            {
                int c = Math.Max(0, Math.Min(cols - 1, x / size)), r = Math.Max(0, Math.Min(rows - 1, y / size));
                if (Rank(ch) > Rank(cells[r, c])) cells[r, c] = ch;
            };
            var zone = me.Zone;
            foreach (var it in zone.ItemMaps) if (!it.PickedUp && TaskMechanics.CanSee(me, it)) mark(it.X, it.Y, 'i');
            foreach (var m in zone.Mobs) if (!m.IsDead) mark(m.X, m.Y, 'M');
            foreach (PlayerState p in zone.Players) if (p != me) mark(p.X, p.Y, 'P');
            foreach (var n in tile.Npcs) mark(n.X, n.Y, 'N');
            foreach (var w in tile.Waypoints) mark((w.MinX + w.MaxX) / 2, w.MaxY, 'E');
            mark(me.X, me.Y, '@');

            b.Append("terrain (S = ").Append(size).Append(" px, ").Append(cols).Append(" columns x ").Append(rows).Append(" rows")
             .Append(reach == null ? "; reachability still being computed, every floor shown as _" : "").Append("):\n");
            // column ruler: tens digit, then units digit
            b.Append("      "); for (int c = 0; c < cols; c++) b.Append(c % 10 == 0 ? (char)('0' + (c / 10) % 10) : ' '); b.Append('\n');
            b.Append("      "); for (int c = 0; c < cols; c++) b.Append((char)('0' + c % 10)); b.Append('\n');
            for (int r = 0; r < rows; r++)
            {
                b.Append(("y" + r * size).PadLeft(5)).Append(' ');
                for (int c = 0; c < cols; c++) b.Append(cells[r, c]);
                b.Append('\n');
            }
        }

        // drawing priority: objects over terrain, reachable floor over the rest
        private static int Rank(char c)
        {
            switch (c)
            {
                case '@': return 12; case 'E': return 11; case 'N': return 10; case 'P': return 9; case 'M': return 8; case 'i': return 7;
                case '_': return 6; case 'x': return 5; case 'w': return 4; case '#': return 3; default: return 0;
            }
        }

        private static int Dist(PlayerState me, int x, int y) { return Math.Abs(x - me.X) + Math.Abs(y - me.Y); }

        // " (no way found from here)" when the platform graph knows no way (nothing while the graph is still being built)
        private static string Way(TileGrid grid, PlayerState me, int x, int y, int speed)
        {
            if (!BotNavigation.Standable(grid, BotNavigation.Centre(me.X), me.Y)) return "";
            short floor = grid.CollisionY((short)Math.Max(0, Math.Min(grid.PxW - 1, x)), (short)Math.Max(0, y - 24));
            bool ok;
            var path = BotNavigation.Find(grid, me.X, me.Y, x, floor < grid.PxH - 1 ? floor : -1, speed, out ok);
            return path == null || ok ? "" : " (no way found from here)";
        }
    }
}
