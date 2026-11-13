using System;
using System.Collections.Generic;

namespace Functions
{
    // Token: 0x020000CE RID: 206
    public class ListCharFunctions
    {
        // Token: 0x06000AC1 RID: 2753 RVA: 0x000AD524 File Offset: 0x000AB724
        public static ListCharFunctions gI()
        {
            bool flag = ListCharFunctions.instance == null;
            if (flag)
            {
                ListCharFunctions.instance = new ListCharFunctions();
            }

            return ListCharFunctions.instance;
        }

        // Token: 0x06000AC2 RID: 2754 RVA: 0x000AD554 File Offset: 0x000AB754
        public static void Update()
        {
            ListCharFunctions.chars.Clear();
            for (int i = 0; i < GameScr.vCharInMap.size(); i++)
            {
                global::Char @char = (global::Char)GameScr.vCharInMap.elementAt(i);
                bool flag = @char.cName != null && @char.cName != "" && !@char.cName.StartsWith("#") && !@char.cName.StartsWith("$") && @char.cName != "Trọng tài";
                if (flag)
                {
                    ListCharFunctions.chars.Add(@char);
                }
            }

            ListCharFunctions.Check();
        }

        // Token: 0x06000AC3 RID: 2755 RVA: 0x000AD618 File Offset: 0x000AB818
        public static void Check()
        {
            bool flag = ListCharFunctions.chars.Count <= 2;
            if (!flag)
            {
                List<global::Char> list = new List<global::Char>();
                while (ListCharFunctions.chars.Count != 0)
                {
                    global::Char @char = ListCharFunctions.chars[0];
                    list.Add(@char);
                    string a = @char.CharCheck();
                    ListCharFunctions.chars.RemoveAt(0);
                    for (int i = 0; i < ListCharFunctions.chars.Count; i++)
                    {
                        global::Char char2 = ListCharFunctions.chars[i];
                        bool flag2 = a == char2.CharCheck();
                        if (flag2)
                        {
                            list.Add(char2);
                            ListCharFunctions.chars.RemoveAt(i);
                            i--;
                        }
                    }
                }

                ListCharFunctions.chars.Clear();
                ListCharFunctions.chars = list;
            }
        }

        // Token: 0x06000AC4 RID: 2756 RVA: 0x000AD6FC File Offset: 0x000AB8FC
        public static bool MapNro()
        {
            return TileMap.mapID >= 85 && TileMap.mapID <= 91;
        }

        // Token: 0x06000AC5 RID: 2757 RVA: 0x000AD728 File Offset: 0x000AB928
        public static bool CheckCharName(global::Char a)
        {
            return char.IsUpper(char.Parse(a.cName.Substring(0, 1))) && a.cName != "Trọng tài" && !a.cName.StartsWith("#") && a.cName != null && a.cName != "" && !a.cName.StartsWith("$");
        }

        // Token: 0x06000AC7 RID: 2759 RVA: 0x000AD8C8 File Offset: 0x000ABAC8
        public static void Paint(mGraphics a)
        {
            int num = 95;
            ListCharFunctions.X = 110;
            ListCharFunctions.Y = 7;
            bool flag = mGraphics.zoomLevel == 1;
            if (flag)
            {
                ListCharFunctions.X = 132;
                ListCharFunctions.Y = 9;
            }

            for (int i = 0; i < ListCharFunctions.chars.Count; i++)
            {
                global::Char @char = ListCharFunctions.chars[i];
                a.setColor(2721889, 0.5f);
                a.fillRect(GameCanvas.w - ListCharFunctions.X, num + 2, ListCharFunctions.X - 2, ListCharFunctions.Y);
                //ListCharFunctions.PaintFlag(@char, a, GameCanvas.w - ListCharFunctions.X - 9, num + 2);
                bool flag2 = !@char.cName.StartsWith("#") && !@char.cName.StartsWith("$") && @char.cName != "Trọng tài" && @char.cName != null && @char.cName != "";
                if (flag2)
                {
                    string str = string.Concat(new object[] { @char.cName, " [", NinjaUtil.getMoneys((long)@char.cHP), "]" });
                    bool flag4;
                    bool flag3 = !(flag4 = ListCharFunctions.CheckCharName(@char));
                    if (flag3)
                    {
                        str = string.Concat(new object[] { @char.cName, " [", NinjaUtil.getMoneys((long)@char.cHP), " - ", @char.HanhTinh(), "]" });
                    }

                    bool flag5 = global::Char.getMyChar().charFocus != null && global::Char.getMyChar().charFocus.cName == @char.cName;
                    if (flag5)
                    {
                        a.setColor(14155776);
                        a.drawLine(global::Char.getMyChar().cx - GameScr.cmx, global::Char.getMyChar().cy - GameScr.cmy + 1, @char.cx - GameScr.cmx, @char.cy - GameScr.cmy);
                        mFont.tahoma_7b_red.drawString(a, (i + 1).ToString() + ". " + str, GameCanvas.w - ListCharFunctions.X + 2, num, 0);
                    }
                    else
                    {
                        bool flag6 = flag4;
                        if (flag6)
                        {
                            a.setColor(16383818);
                            a.drawLine(global::Char.getMyChar().cx - GameScr.cmx, global::Char.getMyChar().cy - GameScr.cmy + 1, @char.cx - GameScr.cmx, @char.cy - GameScr.cmy);
                            mFont.tahoma_7b_red.drawString(a, (i + 1).ToString() + ". " + str, GameCanvas.w - ListCharFunctions.X + 2, num, 0);
                        }
                        else
                        {
                            mFont mfont = mFont.tahoma_7;
                            switch (@char.cgender)
                            {
                                case 0:
                                    mfont = mFont.tahoma_7_blue1;
                                    mFont.tahoma_7_blue1.drawString(a, (i + 1).ToString() + ". " + str, GameCanvas.w - ListCharFunctions.X + 2, num, 0, mFont.tahoma_7_grey);
                                    break;
                                case 1:
                                    mfont = mFont.tahoma_7_green;
                                    mFont.tahoma_7_green.drawString(a, (i + 1).ToString() + ". " + str, GameCanvas.w - ListCharFunctions.X + 2, num, 0, mFont.tahoma_7_grey);
                                    break;
                            }
                        }
                    }

                    num += ListCharFunctions.Y + 1;
                }
            }
        }

        // Token: 0x040013B1 RID: 5041
        public static List<global::Char> chars = new List<global::Char>();
        // Token: 0x040013B2 RID: 5042
        public static bool ShowChar;
        // Token: 0x040013B3 RID: 5043
        public static ListCharFunctions instance;
        // Token: 0x040013B4 RID: 5044
        public static int X;
        // Token: 0x040013B5 RID: 5045
        public static int Y;
    }
}
