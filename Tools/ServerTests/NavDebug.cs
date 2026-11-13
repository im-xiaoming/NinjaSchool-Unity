using System;
using System.Collections.Generic;

static class NavDebug
{
    public static void Dump(NsoServer.World.TileGrid g)
    {
        // ascii: '#' solid, '=' top, 'w' wood/rock, '.' empty, 'S' standable
        for (int r = 0; r < g.Tmh; r++)
        {
            var sb = new System.Text.StringBuilder();
            for (int c = 0; c < g.Tmw; c++)
            {
                int x = c * 24 + 12, y = r * 24;
                char ch = '.';
                if (g.TileTypeAt(x, y + 1, NsoServer.World.TileGrid.T_SOLIDGROUND))
                    ch = '#';
                if (g.IsWood(x, y + 1) || g.IsRock(x, y + 1))
                    ch = 'w';
                if (NsoServer.Ai.BotNavigation.Standable(g, x, y))
                    ch = 'S';
                else if (g.TileTypeAt(x, y, NsoServer.World.TileGrid.T_TOP))
                    ch = '=';
                sb.Append(ch);
            }

            Console.WriteLine(sb);
        }
    }
}
