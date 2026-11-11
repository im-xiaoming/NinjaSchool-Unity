using System;
using System.Collections.Generic;

namespace NsoServer.World
{
    // Port of TileMap.loadMapFromResource / loadMap: the tile ids of a map file and the terrain flags derived from them.
    public sealed class TileGrid
    {
        public const int T_TOP = 2, T_LEFT = 2 << 1, T_RIGHT = 2 << 2, T_TREE = 2 << 3, T_WATERFALL = 2 << 4, T_WATERFLOW = 2 << 5,
            T_TOPFALL = 2 << 6, T_OUTSIDE = 2 << 7, T_BRIDGE = 2 << 9, T_UNDERWATER = 2 << 10, T_SOLIDGROUND = 2 << 11,
            T_BOTTOM = 2 << 12, T_DIE = 2 << 13, T_HEBI = 2 << 14, T_BANG = 2 << 15, T_JUM8 = 2 << 16, T_NT0 = 2 << 17, T_NT1 = 2 << 18;

        public int Tmw, Tmh, PxW, PxH;
        public int[] Maps;
        public int[] Types;
        public readonly int TileId;

        public TileGrid(byte[] file, int tileId, List<int[]> locationStand)
        {
            TileId = tileId;
            // original quirk kept: a file starting 0,0 is "keyed"; the key is subtracted from every value
            int pos = 0;
            int key = 0;
            if ((sbyte)file[0] + (sbyte)file[1] == 0) { pos = 2; key = (sbyte)file[0]; }
            Tmw = file[pos++] - key;
            Tmh = file[pos++] - key;
            int size = Tmw * Tmh;
            Maps = new int[size];
            for (int i = 0; i < size; i++) Maps[i] = file[pos++] - key;

            // The original marks standing spots using locationStand[m][0] for both coordinates (a bug that is preserved).
            var stands = new HashSet<int>();
            foreach (var s in locationStand) stands.Add(s[0] * Tmw + s[0]);

            Types = new int[size];
            PxH = Tmh * 24;
            PxW = Tmw * 24;
            Classify(stands);
        }

        private static HashSet<int> S(params int[] v) { return new HashSet<int>(v); }

        private static readonly HashSet<int>
            T4Top = S(1, 2, 3, 4, 5, 6, 9, 10, 79, 80, 13, 14, 43, 44, 45, 50), T4Left = S(9, 11), T4Right = S(10, 12), T4Bridge = S(13, 14), T4Flow = S(76, 77),
            T1Top = S(1, 2, 3, 4, 5, 6, 7, 36, 37, 54, 91, 92, 93, 94, 73, 74, 97, 98, 116, 117, 118, 120, 61),
            T1Solid = S(2, 3, 4, 5, 6, 20, 21, 22, 23, 36, 37, 38, 39, 61), T1Tree = S(8, 9, 10, 12, 13, 14, 30), T1Left = S(37, 38, 61), T1Right = S(36, 39, 61),
            T2Top = S(1, 2, 3, 4, 5, 6, 7, 36, 37, 54, 61, 73, 76, 77, 78, 79, 82, 83, 98, 99, 100, 102, 103, 108, 109, 110, 112, 113, 116, 117, 125, 126, 127, 129, 130),
            T2Solid = S(1, 3, 4, 5, 6, 20, 21, 22, 23, 36, 37, 38, 39, 55, 109, 111, 112, 113, 114, 115, 116, 127, 129, 130),
            T2Tree = S(8, 9, 10, 12, 13, 14, 30, 135), T2Left = S(61, 37, 38, 127, 130, 131), T2Right = S(61, 36, 39, 127, 129, 132),
            T3Top = S(1, 2, 3, 4, 5, 6, 7, 11, 14, 17, 43, 51, 63, 65, 67, 68, 71, 72, 83, 84, 85, 87, 91, 94, 97, 98, 106, 107, 111, 113, 117, 118, 119, 125, 126, 129, 130, 131, 133, 136, 138, 139, 142),
            T3Solid = S(124, 116, 123, 44, 12, 15, 16, 45, 10, 9),
            T3Left = S(6, 15, 51, 95, 97, 106, 111, 123, 125, 138, 140), T3Right = S(7, 16, 51, 96, 98, 107, 111, 124, 126, 139, 141),
            T3Outside = S(33, 103, 104, 105, 26), T3Bottom = S(51, 111, 68);

        private void Add(int i, HashSet<int> ids, int flag)
        {
            if (ids.Contains(Maps[i])) Types[i] |= flag;
        }

        private void Classify(HashSet<int> stands)
        {
            int length = Tmh * Tmw;
            try
            {
                for (int i = 0; i < length; i++)
                {
                    if (stands.Contains(i)) Types[i] |= T_TOP;
                    int m = Maps[i];
                    if (TileId == 4)
                    {
                        Add(i, T4Top, T_TOP); Add(i, T4Left, T_LEFT); Add(i, T4Right, T_RIGHT); Add(i, T4Bridge, T_BRIDGE); Add(i, T4Flow, T_WATERFLOW);
                    }
                    if (TileId == 1)
                    {
                        Add(i, T1Top, T_TOP); Add(i, T1Solid, T_SOLIDGROUND); Add(i, T1Tree, T_TREE);
                        if (m == 17) Types[i] |= T_WATERFALL;
                        if (m == 18) Types[i] |= T_TOPFALL;
                        Add(i, T1Left, T_LEFT); Add(i, T1Right, T_RIGHT);
                        if (m == 19) FlowOverSolid(i);
                        if (m == 35) Types[i] |= T_UNDERWATER;
                        if (m == 7) Types[i] |= T_BRIDGE;
                        if (m == 32 || m == 33 || m == 34) Types[i] |= T_OUTSIDE;
                    }
                    if (TileId == 2)
                    {
                        Add(i, T2Top, T_TOP); Add(i, T2Solid, T_SOLIDGROUND); Add(i, T2Tree, T_TREE);
                        if (m == 17) Types[i] |= T_WATERFALL;
                        if (m == 18) Types[i] |= T_TOPFALL;
                        Add(i, T2Left, T_LEFT); Add(i, T2Right, T_RIGHT);
                        if (m == 19 || m == 134) FlowOverSolid(i);
                        if (m == 35) Types[i] |= T_UNDERWATER;
                        if (m == 7) Types[i] |= T_BRIDGE;
                        if (m == 32 || m == 33 || m == 34) Types[i] |= T_OUTSIDE;
                        if (m == 61 || m == 127) Types[i] |= T_BOTTOM;
                    }
                    if (TileId == 3)
                    {
                        Add(i, T3Top, T_TOP); Add(i, T3Solid, T_SOLIDGROUND);
                        if (m == 23) Types[i] |= T_WATERFALL;
                        if (m == 24) Types[i] |= T_TOPFALL;
                        Add(i, T3Left, T_LEFT); Add(i, T3Right, T_RIGHT);
                        if (m == 25) FlowOverSolid(i);
                        if (m == 34) Types[i] |= T_UNDERWATER;
                        if (m == 17) Types[i] |= T_BRIDGE;
                        Add(i, T3Outside, T_OUTSIDE);
                        Add(i, T3Bottom, T_BOTTOM);
                        if (m == 82 || m == 110 || m == 143) Types[i] |= T_DIE;
                        if (m == 113) Types[i] |= T_BANG;
                        if (m == 142) Types[i] |= T_HEBI;
                        if (m == 40 || m == 41) Types[i] |= T_JUM8;
                        if (m == 110) Types[i] |= T_NT0;
                        if (m == 143) Types[i] |= T_NT1;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // the original aborts classification at this point (an exception inside its loop); same here
            }
        }

        // water tiles inherit "solid ground" from the tile above them
        private void FlowOverSolid(int i)
        {
            Types[i] |= T_WATERFLOW;
            if ((Types[i - Tmw] & T_SOLIDGROUND) == T_SOLIDGROUND) Types[i] |= T_SOLIDGROUND;
        }

        public bool TileTypeAt(int px, int py, int t)
        {
            int idx = py / 24 * Tmw + px / 24;
            if (idx < 0 || idx >= Types.Length) return false;
            return (Types[idx] & t) == t;
        }

        public int TileId2(int px, int py)
        {
            int idx = py / 24 * Tmw + px / 24;
            if (idx < 0 || idx >= Maps.Length) return 1000;
            return Maps[idx];
        }

        // TileMap.collisionY: the first floor at or below (x, y)
        public short CollisionY(short x, short y)
        {
            y = (short)(y / 24 * 24);
            while (y < PxH)
            {
                if (TileTypeAt(x, y, T_TOP) || TileTypeAt(x, y, T_BRIDGE) || TileTypeAt(x, y, T_WATERFLOW)) return y;
                y += 24;
            }
            return (short)(PxH - 1);
        }

        public bool IsWood(int x, int y)
        {
            int[] arr = { 61, 61, 51, -1 };
            return arr[TileId - 1] == TileId2(x, y);
        }

        public bool IsRock(int x, int y)
        {
            int[] arr = { -1, 127, 111, -1 };
            return arr[TileId - 1] == TileId2(x, y);
        }
    }
}
