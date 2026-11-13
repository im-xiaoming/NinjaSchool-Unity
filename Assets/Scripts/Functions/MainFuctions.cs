using Functions.Auto;
using System;
using System.Threading;

namespace Functions
{
    internal class MainFuctions : mSystem
    {
        public static int X = 0;
        public static int Y = 0;
        public static long time_ktg;
        public static bool showLogo = true;
        public static bool showktg = true;
        public static void paintMap(mGraphics g, int x, int y)
        {
            if (!DevOptions.ShowInfo)
                return;
            int step = 12;
            mFont.tahoma_7_white.drawString(g, "Map: " + TileMap.mapName + "[" + TileMap.mapID + "] - Khu: " + TileMap.zoneID, x, y += step, 0, mFont.tahoma_7_grey);
            mFont.tahoma_7_white.drawString(g, "Time: " + DateTime.Now.ToString("HH:mm:ss"), x, y += step, 0, mFont.tahoma_7_grey);
            mFont.tahoma_7_white.drawString(g, "Site: [" + Char.getMyChar().cx + "-" + Char.getMyChar().cy + "]", x, y += step, 0, mFont.tahoma_7_grey);
        }

        public static void Paint(mGraphics g)
        {
            GameScr.resetTranslate(g);
            var logo = GameCanvas.loadImage2("/logo");
            if (showLogo)
            {
                g.drawImage(logo, GameCanvas.w / 2, ((!GameCanvas.isKiemduyet) ? Info.hI : 16) + 35, 3);
            }

            if (GameScr.nhanvat)
            {
                paintUser(g);
            }
        }

        public static void paintUser(mGraphics a)
        {
            int num = ((!GameCanvas.isKiemduyet) ? Info.hI : 16) + TileMap.hMiniMap + 3;
            mFont mfont = mFont.tahoma_7b_white;
            X = 130;
            Y = mfont.getHeight();
            for (int i = 0; i < GameScr.vCharInMap.size(); i++)
            {
                Char char2 = (Char)GameScr.vCharInMap.elementAt(i);
                a.setColor(char2.getClassColor(), 0.5f);
                a.fillRect(GameCanvas.w - X, num + 2, X - 2, Y);
                string str = string.Concat(new object[] { char2.cName, " [", NinjaUtil.getMoneys((long)char2.cHP), "]-", "[", "lv: " + char2.clevel, "]" });
                bool flag5 = Char.getMyChar().charFocus != null && Char.getMyChar().charFocus.cName == char2.cName;
                if (flag5)
                {
                    a.setColor(14155776);
                    a.drawLine(Char.getMyChar().cx - GameScr.cmx, Char.getMyChar().cy - GameScr.cmy + 1, char2.cx - GameScr.cmx, char2.cy - GameScr.cmy);
                    mFont.tahoma_7_yellow.drawString(a, (i + 1).ToString() + ". " + str, GameCanvas.w - X + 2, num, 0);
                }
                else
                {
                    mFont.tahoma_7_white.drawString(a, (i + 1).ToString() + ". " + str, GameCanvas.w - X + 2, num, 0, mFont.tahoma_7_grey);
                }

                if (GameCanvas.isPointerClick)
                {
                    if (GameCanvas.isPointerHoldIn(GameCanvas.w - X + 2, num, 100, 12) && GameCanvas.isPointerJustRelease)
                    {
                        new Thread((ThreadStart)delegate
                        {
                            Handle.Paint("MoveTo " + char2.cName);
                            Char.Move(char2.cx, char2.cy);
                        }).Start();
                        Char myChar = Char.getMyChar();
                        myChar.findNextFocusByKey();
                        myChar.charFocus = char2;
                    }
                }

                num += Y + 1;
            }
        }
    }
}
