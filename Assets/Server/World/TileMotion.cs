namespace NsoServer.World
{
    /// <summary>Client terrain geometry without Unity/global TileMap dependencies.</summary>
    public static class TileMotion
    {
        // Char.java/Char.updateCharRun, updateCharJump and updateCharFall dimensions.
        public const int HalfWidth = 11, Height = 32, SideHeight = 16;
        public const int JumpVelocity = -10, Gravity = 1, MaxFallSpeed = 20, StepMilliseconds = 42;
        // GameScr.updateKeyInGame double jump: once per jump, while cvy > -4 (still rising), cvy = -7.
        public const int AirJumpVelocity = -7, AirJumpThreshold = -4;
        // Version execution semantics so a release evaluated with endpoint teleporting cannot be promoted.
        public const string Revision = "tile-motion-3"; // 2: double jump; 3: client wall/ceiling rules only

        private static bool Solid(TileGrid grid, int x, int y)
        {
            return x < 0 || x >= grid.PxW || y < 0 || y >= grid.PxH ||
                grid.TileTypeAt(x, y, TileGrid.T_SOLIDGROUND) || grid.IsWood(x, y) || grid.IsRock(x, y);
        }
        public static bool Supported(TileGrid grid, int x, int y)
        {
            return y % 24 == 0 && (grid.TileTypeAt(x, y, TileGrid.T_TOP) ||
                grid.TileTypeAt(x, y, TileGrid.T_BRIDGE) || grid.TileTypeAt(x, y, TileGrid.T_WATERFLOW));
        }
        public static int Horizontal(TileGrid grid, int x, int y, int delta)
        {
            int direction = System.Math.Sign(delta);
            for (int i = 0; i < System.Math.Abs(delta); i++)
            {
                int next = x + direction, edge = next + direction * HalfWidth;
                if (next < 24 || next >= grid.PxW - 24) break;
                // Char.updateCharRun / updateCharJump: only wall tiles stop a character sideways (T_LEFT when moving right,
                // T_RIGHT when moving left, at chh = 16 above the feet and at the feet). Solid ground inside the body does
                // not block in the client (tile-motion-2 blocked it and cut maps in half for the AI players).
                int side = direction > 0 ? TileGrid.T_LEFT : TileGrid.T_RIGHT;
                if (grid.TileTypeAt(edge, y - SideHeight, side) || grid.TileTypeAt(edge, y - 1, side)) break;
                // the server puts a character back when its feet enter a wood/rock tile (PlayerActions.IsSoftBlock)
                if (grid.IsWood(next, y - 1) || grid.IsRock(next, y - 1)) break;
                x = next;
            }
            return x;
        }
        public static int Vertical(TileGrid grid, int x, int y, int delta, out bool hit)
        {
            hit = false;
            int direction = System.Math.Sign(delta);
            for (int i = 0; i < System.Math.Abs(delta); i++)
            {
                if (direction > 0 && Supported(grid, x, y)) { hit = true; break; }
                int next = y + direction;
                // Char.updateCharJump: the only ceiling is a T_BOTTOM tile at the head (cx, cy - ch), ch = 32
                if (direction < 0 && next > Height && grid.TileTypeAt(x, next - Height, TileGrid.T_BOTTOM)) { hit = true; break; }
                if (direction < 0 && next <= 0) { hit = true; break; }
                else if (next >= grid.PxH - 1) { hit = true; break; }
                y = next;
            }
            return y;
        }
    }
}
