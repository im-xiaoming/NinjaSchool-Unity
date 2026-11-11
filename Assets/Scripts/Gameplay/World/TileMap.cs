using Functions.Auto;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

public class TileMap
{
    public class PosWater
    {
        public int x = -1;

        public int y = -1;
    }

    public static int T_EMPTY;

    public static int T_TOP;

    public static int T_LEFT;

    public static int T_RIGHT;

    public static int T_TREE;

    public static int T_WATERFALL;

    public static int T_WATERFLOW;

    public static int T_TOPFALL;

    public static int T_OUTSIDE;

    public static int T_DOWN1PIXEL;

    public static int T_BRIDGE;

    public static int T_UNDERWATER;

    public static int T_SOLIDGROUND;

    public static int T_BOTTOM;

    public static int T_DIE;

    public static int T_HEBI;

    public static int T_BANG;

    public static int T_JUM8;

    public static int T_NT0;

    public static int T_NT1;

    public static int tmw;

    public static int tmh;

    public static int pxw;

    public static int pxh;

    public static int tileID;

    public static char[] maps;

    public static int[] types;

    public static Image imgTileSmall;

    public static Image imgMiniMap;

    public static Image imgWaterfall;

    public static Image imgTopWaterfall;

    public static Image imgWaterflow;

    public static Image imgLeaf;

    public static Image imgflowRiver;

    public static sbyte size;

    private static int bx;

    private static int dbx;

    private static int fx;

    private static int dfx;

    public static string[] instruction;

    public static int[] iX;

    public static int[] iY;

    public static int[] iW;

    public static int iCount;

    public static string mapName1;

    public static string mapName;

    public static sbyte versionMap;

    public static sbyte zoneID;

    public static sbyte bgID;

    public static sbyte typeMap;

    public static short mapID;

    public static short oldMapID;

    public static int cmtoYmini;

    public static int cmyMini;

    public static int cmdyMini;

    public static int cmvyMini;

    public static int cmtoXMini;

    public static int cmxMini;

    public static int cmdxMini;

    public static int cmvxMini;

    public static int wMiniMap;

    public static int hMiniMap;

    public static int posMiniMapX;

    public static int posMiniMapY;

    public static long timeTranMini;

    public static MyVector vGo;

    public static string[] mapNames;

    public static sbyte MAP_NORMAL;

    public static sbyte MAP_DAUTRUONG;

    public static sbyte MAP_PB;

    public static sbyte MAP_CHIENTRUONG;

    public static mHashtable locationStand;

    public static mHashtable itemMap;

    private static int defaultSolidTile;

    public static int totalTileLoad;

    public static MyVector totalWater;

    public static int[] totalTile;

    public static int sizeMiniMap;

    public static int gssx;

    public static int gssxe;

    public static int gssy;

    public static int gssye;

    public static int countx;

    public static int county;

    private static int[] colorMini;

    public static Image[] imgTileMap;

    public static Color[][][] colorMiniMap;

    public static Color[] blackAr;

    public static int miniSize;

    public static int saveTileId;

    public static bool isStopping;

    public static float volume;

    public static short[][] at2Short;

    private static bool[] atBool;

    private static int[] atInt;

    private static short[] atShort;

    public static bool cj;

    public static int ci;

    public static bool IsDangGoMap;

    public static int MapMod;

    public static int Time;

    public static bool isLockMap;

    public static object objLockMap;

    public static void setPosMiniMap(int x, int y, int w, int h)
    {
        wMiniMap = w;
        hMiniMap = h;
        posMiniMapX = x;
        posMiniMapY = y;
    }

    public static void updateMiniMap()
    {
        cmtoXMini = Char.getMyChar().cx / 12;
        cmtoYmini = Char.getMyChar().cy / 12;
        if (cmtoXMini > tmw * sizeMiniMap - wMiniMap / 2)
        {
            cmtoXMini = tmw * sizeMiniMap - wMiniMap;
        }
        else if (cmtoXMini < wMiniMap / 2)
        {
            cmtoXMini = 0;
        }
        else
        {
            cmtoXMini -= wMiniMap / 2;
        }
        if (cmtoYmini < hMiniMap / 2)
        {
            cmtoYmini = 0;
        }
        else
        {
            cmtoYmini -= hMiniMap / 2;
        }
        if (cmtoYmini > tmh * sizeMiniMap - hMiniMap)
        {
            cmtoYmini = tmh * sizeMiniMap - hMiniMap;
        }
    }

    public static void updateCmMiniMap()
    {
        if (tmw * sizeMiniMap >= wMiniMap || tmh * sizeMiniMap >= hMiniMap)
        {
            if (cmyMini != cmtoYmini)
            {
                cmvyMini = cmtoYmini - cmyMini << 2;
                cmdyMini += cmvyMini;
                cmyMini += cmdyMini >> 4;
                cmdyMini &= 15;
            }
            if (cmxMini != cmtoXMini)
            {
                cmvxMini = cmtoXMini - cmxMini << 2;
                cmdxMini += cmvxMini;
                cmxMini += cmdxMini >> 4;
                cmdxMini &= 15;
            }
        }
    }

    public static void freeTilemap()
    {
    }

    public static void loadTileImage()
    {
        if (imgWaterfall == null)
        {
            imgWaterfall = GameCanvas.loadImage("/t/wtf");
        }
        if (imgTopWaterfall == null)
        {
            imgTopWaterfall = GameCanvas.loadImage("/t/twtf");
        }
        if (imgWaterflow == null)
        {
            imgWaterflow = GameCanvas.loadImage("/t/wts");
        }
        if (imgflowRiver == null)
        {
            imgflowRiver = GameCanvas.loadImage("/t/wts1");
        }
    }

    public static void resetDataImg()
    {
        if (imgTileMap != null)
        {
            mGraphics.cachedTextures.Clear();
            for (int i = 0; i < imgTileMap.Length; i++)
            {
                if (imgTileMap[i] != null)
                {
                    imgTileMap[i].texture = null;
                    imgTileMap[i] = null;
                }
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        else
        {
            imgTileMap = new Image[150];
        }
    }

    public static void loadTileUse(int id)
    {
        if (id >= 0 && imgTileMap[id] == null)
        {
            if (mGraphics.zoomLevel == 1)
            {
                imgTileMap[id] = GameCanvas.loadImage("/t/tile" + tileID + "/" + id);
            }
            else
            {
                imgTileMap[id] = GameCanvas.loadImage("/t/tile" + tileID + "/" + (id + 1));
            }
            totalTileLoad++;
        }
    }

    public static void setPosWater()
    {
        totalWater = new MyVector();
        for (int i = 0; i < tmw; i++)
        {
            for (int j = 0; j < tmh; j++)
            {
                _ = maps[j * tmw + i];
                if ((tileTypeAt(i, j) & T_OUTSIDE) != T_OUTSIDE && (tileTypeAt(i, j) & T_WATERFALL) == T_WATERFALL)
                {
                    putPosIntoVector(i * size, j * size);
                }
            }
        }
    }

    public static bool getPosVsMainChar()
    {
        for (int i = 0; i < totalWater.size(); i++)
        {
            PosWater posWater = (PosWater)totalWater.elementAt(i);
            int num = posWater.x - GameCanvas.w;
            int num2 = posWater.x + GameCanvas.w;
            int num3 = posWater.y - GameCanvas.h;
            int num4 = posWater.y + GameCanvas.h;
            if (Char.getMyChar().cx >= num && Char.getMyChar().cx <= num2 && Char.getMyChar().cy >= num3 && Char.getMyChar().cy <= num4 && posWater.x <= GameScr.cmx + GameCanvas.w && posWater.x >= GameScr.cmx && posWater.y >= GameScr.cmy && posWater.y <= GameScr.cmy + GameCanvas.h)
            {
                return true;
            }
        }
        return false;
    }

    public static void putPosIntoVector(int x, int y)
    {
        PosWater posWater = new PosWater();
        posWater.x = x;
        posWater.y = y;
        totalWater.addElement(posWater);
    }

    public static bool isStand(int index)
    {
        if (locationStand != null)
        {
            return locationStand.get(index + string.Empty) != null;
        }
        return false;
    }

    public static void loadMap(int tileId)
    {
        totalTileLoad = 0;
        resetDataImg();
        pxh = tmh * size;
        pxw = tmw * size;
        try
        {
            for (int i = 0; i < tmw * tmh; i++)
            {
                if (isStand(i))
                {
                    types[i] |= T_TOP;
                }
                loadTileUse(maps[i] - 1);
                if (tileId == 4)
                {
                    if (maps[i] == '\u0001' || maps[i] == '\u0002' || maps[i] == '\u0003' || maps[i] == '\u0004' || maps[i] == '\u0005' || maps[i] == '\u0006' || maps[i] == '\t' || maps[i] == '\n' || maps[i] == 'O' || maps[i] == 'P' || maps[i] == '\r' || maps[i] == '\u000e' || maps[i] == '+' || maps[i] == ',' || maps[i] == '-' || maps[i] == '2')
                    {
                        types[i] |= T_TOP;
                    }
                    if (maps[i] == '\t' || maps[i] == '\v')
                    {
                        types[i] |= T_LEFT;
                    }
                    if (maps[i] == '\n' || maps[i] == '\f')
                    {
                        types[i] |= T_RIGHT;
                    }
                    if (maps[i] == '\r' || maps[i] == '\u000e')
                    {
                        types[i] |= T_BRIDGE;
                    }
                    if (maps[i] == 'L' || maps[i] == 'M')
                    {
                        types[i] |= T_WATERFLOW;
                        if (maps[i] == 'N')
                        {
                            types[i] |= T_SOLIDGROUND;
                        }
                    }
                }
                if (tileId == 1)
                {
                    if (maps[i] == '\u0016')
                    {
                        defaultSolidTile = maps[i] - 1;
                    }
                    if (maps[i] == '\u0001' || maps[i] == '\u0002' || maps[i] == '\u0003' || maps[i] == '\u0004' || maps[i] == '\u0005' || maps[i] == '\u0006' || maps[i] == '\a' || maps[i] == '$' || maps[i] == '%' || maps[i] == '6' || maps[i] == '[' || maps[i] == '\\' || maps[i] == ']' || maps[i] == '^' || maps[i] == 'I' || maps[i] == 'J' || maps[i] == 'a' || maps[i] == 'b' || maps[i] == 't' || maps[i] == 'u' || maps[i] == 'v' || maps[i] == 'x' || maps[i] == '=')
                    {
                        types[i] |= T_TOP;
                    }
                    if (maps[i] == '\u0002' || maps[i] == '\u0003' || maps[i] == '\u0004' || maps[i] == '\u0005' || maps[i] == '\u0006' || maps[i] == '\u0014' || maps[i] == '\u0015' || maps[i] == '\u0016' || maps[i] == '\u0017' || maps[i] == '$' || maps[i] == '%' || maps[i] == '&' || maps[i] == '\'' || maps[i] == '=')
                    {
                        types[i] |= T_SOLIDGROUND;
                    }
                    if (maps[i] == '\b' || maps[i] == '\t' || maps[i] == '\n' || maps[i] == '\f' || maps[i] == '\r' || maps[i] == '\u000e' || maps[i] == '\u001e')
                    {
                        types[i] |= T_TREE;
                    }
                    if (maps[i] == '\u0011')
                    {
                        types[i] |= T_WATERFALL;
                    }
                    if (maps[i] == '\u0012')
                    {
                        types[i] |= T_TOPFALL;
                    }
                    if (maps[i] == '%' || maps[i] == '&' || maps[i] == '=')
                    {
                        types[i] |= T_LEFT;
                    }
                    if (maps[i] == '$' || maps[i] == '\'' || maps[i] == '=')
                    {
                        types[i] |= T_RIGHT;
                    }
                    if (maps[i] == '\u0013')
                    {
                        types[i] |= T_WATERFLOW;
                        if ((types[i - tmw] & T_SOLIDGROUND) == T_SOLIDGROUND)
                        {
                            types[i] |= T_SOLIDGROUND;
                        }
                    }
                    if (maps[i] == '#')
                    {
                        types[i] |= T_UNDERWATER;
                    }
                    if (maps[i] == '\a')
                    {
                        types[i] |= T_BRIDGE;
                    }
                    if (maps[i] == ' ' || maps[i] == '!' || maps[i] == '"')
                    {
                        types[i] |= T_OUTSIDE;
                    }
                }
                if (tileId == 2)
                {
                    if (maps[i] == '\u0016' || maps[i] == 'g' || maps[i] == 'o')
                    {
                        defaultSolidTile = maps[i] - 1;
                    }
                    if (maps[i] == '\u0001' || maps[i] == '\u0002' || maps[i] == '\u0003' || maps[i] == '\u0004' || maps[i] == '\u0005' || maps[i] == '\u0006' || maps[i] == '\a' || maps[i] == '$' || maps[i] == '%' || maps[i] == '6' || maps[i] == '=' || maps[i] == 'I' || maps[i] == 'L' || maps[i] == 'M' || maps[i] == 'N' || maps[i] == 'O' || maps[i] == 'R' || maps[i] == 'S' || maps[i] == 'b' || maps[i] == 'c' || maps[i] == 'd' || maps[i] == 'f' || maps[i] == 'g' || maps[i] == 'l' || maps[i] == 'm' || maps[i] == 'n' || maps[i] == 'p' || maps[i] == 'q' || maps[i] == 't' || maps[i] == 'u' || maps[i] == '}' || maps[i] == '~' || maps[i] == '\u007f' || maps[i] == '\u0081' || maps[i] == '\u0082')
                    {
                        types[i] |= T_TOP;
                    }
                    if (maps[i] == '\u0001' || maps[i] == '\u0003' || maps[i] == '\u0004' || maps[i] == '\u0005' || maps[i] == '\u0006' || maps[i] == '\u0014' || maps[i] == '\u0015' || maps[i] == '\u0016' || maps[i] == '\u0017' || maps[i] == '$' || maps[i] == '%' || maps[i] == '&' || maps[i] == '\'' || maps[i] == '7' || maps[i] == 'm' || maps[i] == 'o' || maps[i] == 'p' || maps[i] == 'q' || maps[i] == 'r' || maps[i] == 's' || maps[i] == 't' || maps[i] == '\u007f' || maps[i] == '\u0081' || maps[i] == '\u0082')
                    {
                        types[i] |= T_SOLIDGROUND;
                    }
                    if (maps[i] == '\b' || maps[i] == '\t' || maps[i] == '\n' || maps[i] == '\f' || maps[i] == '\r' || maps[i] == '\u000e' || maps[i] == '\u001e' || maps[i] == '\u0087')
                    {
                        types[i] |= T_TREE;
                    }
                    if (maps[i] == '\u0011')
                    {
                        types[i] |= T_WATERFALL;
                    }
                    if (maps[i] == '\u0012')
                    {
                        types[i] |= T_TOPFALL;
                    }
                    if (maps[i] == '=' || maps[i] == '%' || maps[i] == '&' || maps[i] == '\u007f' || maps[i] == '\u0082' || maps[i] == '\u0083')
                    {
                        types[i] |= T_LEFT;
                    }
                    if (maps[i] == '=' || maps[i] == '$' || maps[i] == '\'' || maps[i] == '\u007f' || maps[i] == '\u0081' || maps[i] == '\u0084')
                    {
                        types[i] |= T_RIGHT;
                    }
                    if (maps[i] == '\u0013')
                    {
                        types[i] |= T_WATERFLOW;
                        if ((types[i - tmw] & T_SOLIDGROUND) == T_SOLIDGROUND)
                        {
                            types[i] |= T_SOLIDGROUND;
                        }
                    }
                    if (maps[i] == '\u0086')
                    {
                        types[i] |= T_WATERFLOW;
                        if ((types[i - tmw] & T_SOLIDGROUND) == T_SOLIDGROUND)
                        {
                            types[i] |= T_SOLIDGROUND;
                        }
                    }
                    if (maps[i] == '#')
                    {
                        types[i] |= T_UNDERWATER;
                    }
                    if (maps[i] == '\a')
                    {
                        types[i] |= T_BRIDGE;
                    }
                    if (maps[i] == ' ' || maps[i] == '!' || maps[i] == '"')
                    {
                        types[i] |= T_OUTSIDE;
                    }
                    if (maps[i] == '=' || maps[i] == '\u007f')
                    {
                        types[i] |= T_BOTTOM;
                    }
                }
                if (tileId != 3)
                {
                    continue;
                }
                if (maps[i] == '\f' || maps[i] == '3' || maps[i] == 'X' || maps[i] == 't' || maps[i] == '\u0080')
                {
                    defaultSolidTile = maps[i] - 1;
                }
                if (maps[i] == 'm' || maps[i] == 'n')
                {
                    defaultSolidTile = maps[i];
                }
                if (maps[i] == '\u0001' || maps[i] == '\u0002' || maps[i] == '\u0003' || maps[i] == '\u0004' || maps[i] == '\u0005' || maps[i] == '\u0006' || maps[i] == '\a' || maps[i] == '\v' || maps[i] == '\u000e' || maps[i] == '\u0011' || maps[i] == '+' || maps[i] == '3' || maps[i] == '?' || maps[i] == 'A' || maps[i] == 'C' || maps[i] == 'D' || maps[i] == 'G' || maps[i] == 'H' || maps[i] == 'S' || maps[i] == 'T' || maps[i] == 'U' || maps[i] == 'W' || maps[i] == '[' || maps[i] == '^' || maps[i] == 'a' || maps[i] == 'b' || maps[i] == 'j' || maps[i] == 'k' || maps[i] == 'o' || maps[i] == 'q' || maps[i] == 'u' || maps[i] == 'v' || maps[i] == 'w' || maps[i] == '}' || maps[i] == '~' || maps[i] == '\u0081' || maps[i] == '\u0082' || maps[i] == '\u0083' || maps[i] == '\u0085' || maps[i] == '\u0088' || maps[i] == '\u008a' || maps[i] == '\u008b' || maps[i] == '\u008e')
                {
                    types[i] |= T_TOP;
                }
                if (maps[i] == '|' || maps[i] == 't' || maps[i] == '{' || maps[i] == ',' || maps[i] == '\f' || maps[i] == '\u000f' || maps[i] == '\u0010' || maps[i] == '-' || maps[i] == '\n' || maps[i] == '\t')
                {
                    types[i] |= T_SOLIDGROUND;
                }
                if (maps[i] == '\u0017')
                {
                    types[i] |= T_WATERFALL;
                }
                if (maps[i] == '\u0018')
                {
                    types[i] |= T_TOPFALL;
                }
                if (maps[i] == '\u0006' || maps[i] == '\u000f' || maps[i] == '3' || maps[i] == '_' || maps[i] == 'a' || maps[i] == 'j' || maps[i] == 'o' || maps[i] == '{' || maps[i] == '}' || maps[i] == '\u008a' || maps[i] == '\u008c')
                {
                    types[i] |= T_LEFT;
                }
                if (maps[i] == '\a' || maps[i] == '\u0010' || maps[i] == '3' || maps[i] == '`' || maps[i] == 'b' || maps[i] == 'k' || maps[i] == 'o' || maps[i] == '|' || maps[i] == '~' || maps[i] == '\u008b' || maps[i] == '\u008d')
                {
                    types[i] |= T_RIGHT;
                }
                if (maps[i] == '\u0019')
                {
                    types[i] |= T_WATERFLOW;
                    if ((types[i - tmw] & T_SOLIDGROUND) == T_SOLIDGROUND)
                    {
                        types[i] |= T_SOLIDGROUND;
                    }
                }
                if (maps[i] == '"')
                {
                    types[i] |= T_UNDERWATER;
                }
                if (maps[i] == '\u0011')
                {
                    types[i] |= T_BRIDGE;
                }
                if (maps[i] == '!' || maps[i] == 'g' || maps[i] == 'h' || maps[i] == 'i' || maps[i] == '\u001a' || maps[i] == '!')
                {
                    types[i] |= T_OUTSIDE;
                }
                if (maps[i] == '3' || maps[i] == 'o' || maps[i] == 'D')
                {
                    types[i] |= T_BOTTOM;
                }
                if (maps[i] == 'R' || maps[i] == 'n' || maps[i] == '\u008f')
                {
                    types[i] |= T_DIE;
                }
                if (maps[i] == 'q')
                {
                    types[i] |= T_BANG;
                }
                if (maps[i] == '\u008e')
                {
                    types[i] |= T_HEBI;
                }
                if (maps[i] == '(' || maps[i] == ')')
                {
                    types[i] |= T_JUM8;
                }
                if (maps[i] == 'n')
                {
                    types[i] |= T_NT0;
                }
                if (maps[i] == '\u008f')
                {
                    types[i] |= T_NT1;
                }
            }
            if (imgMiniMap != null)
            {
                imgMiniMap.texture = null;
                imgMiniMap = null;
                Resources.UnloadUnusedAssets();
                GC.Collect();
            }
            loadMiniMap();
            if (!GameCanvas.lowGraphic && !Main.isIpod && mGraphics.zoomLevel != 1)
            {
                if (mapID == 0 || mapID <= 4 || (mapID >= 16 && mapID <= 18) || (mapID >= 24 && mapID <= 27) || mapID == 22 || mapID == 33 || mapID == 34 || mapID == 38 || mapID == 57 || mapID == 58 || mapID == 60 || mapID == 68 || (mapID >= 70 && mapID <= 75) || mapID == 81)
                {
                    Effect2.vAnimateEffect.addElement(new AnimateEffect(1, isStart: true, 10, 200));
                }
                if ((mapID >= 39 && mapID <= 44) || (mapID >= 46 && mapID <= 48) || mapID == 56 || (mapID >= 62 && mapID <= 65))
                {
                    Effect2.vAnimateEffect.addElement(new AnimateEffect(3, isStart: true, Res.random(150, 200), 200));
                }
            }
            setPosWater();
        }
        catch (Exception ex)
        {
            Out.println("Error Load Map>>>>>>>>>>>>>>>>>>>>>.");
            Debug.Log(ex.Message + ex.StackTrace);
            GameMidlet.instance.exit();
        }
    }

    public static void loadColorMiniMap(int tileId)
    {
        if (colorMiniMap[tileId - 1] == null)
        {
            Image[] array = new Image[totalTile[tileId - 1]];
            colorMiniMap[tileId - 1] = new Color[totalTile[tileId - 1]][];
            for (int i = 0; i < totalTile[tileId - 1]; i++)
            {
                array[i] = GameCanvas.loadImage("/t/mini" + tileId + "/" + (i + 1));
                colorMiniMap[tileId - 1][i] = array[i].texture.GetPixels();
                array[i].texture = null;
                array[i] = null;
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }

    public static void loadMiniMap()
    {
        loadColorMiniMap(tileID);
        imgMiniMap = Image.createImage(tmw * sizeMiniMap * mGraphics.zoomLevel, tmh * sizeMiniMap * mGraphics.zoomLevel);
        for (int i = 0; i < tmw; i++)
        {
            for (int j = 0; j < tmh; j++)
            {
                int num = maps[j * tmw + i] - 1;
                if (num != -1 && num < colorMiniMap[tileID - 1].Length && num >= 0)
                {
                    imgMiniMap.texture.SetPixels(i * sizeMiniMap * mGraphics.zoomLevel, (tmh - 1 - j) * sizeMiniMap * mGraphics.zoomLevel, sizeMiniMap * mGraphics.zoomLevel, sizeMiniMap * mGraphics.zoomLevel, colorMiniMap[tileID - 1][num]);
                }
                else
                {
                    imgMiniMap.texture.SetPixels(i * sizeMiniMap * mGraphics.zoomLevel, (tmh - 1 - j) * sizeMiniMap * mGraphics.zoomLevel, sizeMiniMap * mGraphics.zoomLevel, sizeMiniMap * mGraphics.zoomLevel, blackAr);
                }
            }
        }
        imgMiniMap.texture.Apply();
    }

    public static void paintTilemapLOW(mGraphics g)
    {
        for (int i = GameScr.gssx; i < GameScr.gssxe; i++)
        {
            for (int j = GameScr.gssy; j < GameScr.gssye; j++)
            {
                int num = maps[j * tmw + i] - 1;
                if (num != -1)
                {
                    g.drawImage(imgTileMap[num], i * size, j * size, 0);
                }
                if ((tileTypeAt(i, j) & T_WATERFALL) == T_WATERFALL)
                {
                    g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                }
                else if ((tileTypeAt(i, j) & T_WATERFLOW) == T_WATERFLOW)
                {
                    if ((tileTypeAt(i, j - 1) & T_WATERFALL) == T_WATERFALL)
                    {
                        g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                    }
                    else if ((tileTypeAt(i, j - 1) & T_SOLIDGROUND) == T_SOLIDGROUND)
                    {
                        g.drawImage(imgTileMap[21], i * size, j * size, 0);
                    }
                    g.drawRegion(imgWaterflow, 0, (GameCanvas.gameTick % 8 >> 2) * 24, 24, 24, 0, i * size, j * size, 0);
                }
                if ((tileTypeAt(i, j) & T_UNDERWATER) == T_UNDERWATER)
                {
                    if ((tileTypeAt(i, j - 1) & T_WATERFALL) == T_WATERFALL)
                    {
                        g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                    }
                    else if ((tileTypeAt(i, j - 1) & T_SOLIDGROUND) == T_SOLIDGROUND)
                    {
                        g.drawImage(imgTileMap[21], i * size, j * size, 0);
                    }
                    g.drawImage(imgTileMap[maps[j * tmw + i] - 1], i * size, j * size, 0);
                }
            }
        }
    }

    public static void paintTilemap(mGraphics g)
    {
        if (GameCanvas.lowGraphic)
        {
            return;
        }
        try
        {
            for (int i = GameScr.gssx; i < GameScr.gssxe; i++)
            {
                for (int j = GameScr.gssy; j < GameScr.gssye; j++)
                {
                    int num = maps[j * tmw + i] - 1;
                    if ((tileTypeAt(i, j) & T_OUTSIDE) == T_OUTSIDE)
                    {
                        continue;
                    }
                    if (tileID == 4 && (tileTypeAt(i, j) & T_WATERFLOW) == T_WATERFLOW)
                    {
                        int num2 = j - 1;
                        num = maps[num2 * tmw + i] - 1;
                        if (num == 15)
                        {
                            num = 17;
                            g.drawImage(imgTileMap[num], i * size, j * size, 0);
                            continue;
                        }
                        if (num == 5)
                        {
                            num = 7;
                            g.drawImage(imgTileMap[num], i * size, j * size, 0);
                            continue;
                        }
                        if (num == 18 || num == 22 || num == 15)
                        {
                            num = 17;
                            g.drawImage(imgTileMap[num], i * size, j * size, 0);
                            continue;
                        }
                        if (num == 44 || num == 52 || num == 51)
                        {
                            num = 56;
                            g.drawImage(imgTileMap[num], i * size, j * size, 0);
                            continue;
                        }
                        if (num == 24 || num == 23 || num == 20 || num == 21 || num == 19 || num == 12 || num == 13)
                        {
                            continue;
                        }
                        if (num != -1)
                        {
                            g.drawImage(imgTileMap[num], i * size, j * size, 0);
                        }
                        else if (num == -1)
                        {
                            continue;
                        }
                    }
                    if (tileID == 1)
                    {
                        if ((tileTypeAt(i, j) & T_WATERFALL) == T_WATERFALL)
                        {
                            g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                            continue;
                        }
                        if ((tileTypeAt(i, j) & T_WATERFLOW) == T_WATERFLOW || (tileTypeAt(i, j) & T_UNDERWATER) == T_UNDERWATER)
                        {
                            if ((tileTypeAt(i, j - 1) & T_WATERFALL) == T_WATERFALL)
                            {
                                g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                                continue;
                            }
                            if ((tileTypeAt(i, j - 1) & T_SOLIDGROUND) == T_SOLIDGROUND)
                            {
                                g.drawImage(imgTileMap[21], i * size, j * size, 0);
                                continue;
                            }
                        }
                    }
                    if (tileID == 2)
                    {
                        if ((tileTypeAt(i, j) & T_WATERFALL) == T_WATERFALL)
                        {
                            g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 8 >> 1), 24, 24, 0, i * size, j * size, 0);
                            continue;
                        }
                        switch (num)
                        {
                            case 17:
                                g.drawRegion(imgTopWaterfall, 0, 24 * (GameCanvas.gameTick % 8 >> 1), 24, 24, 0, i * size, j * size, 0);
                                continue;
                            case 133:
                                g.drawImage(imgTileMap[132], i * size, j * size, 0);
                                continue;
                        }
                        if ((tileTypeAt(i, j) & T_WATERFLOW) == T_WATERFLOW || (tileTypeAt(i, j) & T_UNDERWATER) == T_UNDERWATER)
                        {
                            if ((tileTypeAt(i, j - 1) & T_WATERFALL) == T_WATERFALL)
                            {
                                g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                                continue;
                            }
                            if ((tileTypeAt(i, j - 1) & T_SOLIDGROUND) == T_SOLIDGROUND)
                            {
                                int num3 = tileAt(i, j - 1);
                                if (num3 == 55)
                                {
                                    num3 = 54;
                                }
                                else if (num3 != 19 && num3 != 35)
                                {
                                    num3 = ((num3 >= 40) ? 110 : 21);
                                }
                                else
                                {
                                    num3 = tileAt(i, j - 2);
                                    if (num3 == 55)
                                    {
                                        num3 = 54;
                                    }
                                    else if (num3 < 40)
                                    {
                                        num3 = 21;
                                    }
                                }
                                g.drawImage(imgTileMap[num3], i * size, j * size, 0);
                                continue;
                            }
                        }
                    }
                    if (tileID == 3)
                    {
                        if ((tileTypeAt(i, j) & T_WATERFALL) == T_WATERFALL)
                        {
                            g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 8 >> 1), 24, 24, 0, i * size, j * size, 0);
                            continue;
                        }
                        if (num == 23)
                        {
                            g.drawRegion(imgTopWaterfall, 0, 24 * (GameCanvas.gameTick % 8 >> 1), 24, 24, 0, i * size, j * size, 0);
                            continue;
                        }
                        if ((tileTypeAt(i, j) & T_WATERFLOW) == T_WATERFLOW || (tileTypeAt(i, j) & T_UNDERWATER) == T_UNDERWATER)
                        {
                            if ((tileTypeAt(i, j - 1) & T_WATERFALL) == T_WATERFALL)
                            {
                                g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, i * size, j * size, 0);
                                continue;
                            }
                            if ((tileTypeAt(i, j - 1) & T_SOLIDGROUND) == T_SOLIDGROUND)
                            {
                                int num4 = tileAt(i, j - 1);
                                if (num4 == 25)
                                {
                                    num4 = tileAt(i, j - 2);
                                }
                                if (num4 == 45)
                                {
                                    num4 = 44;
                                }
                                num4--;
                                g.drawImage(imgTileMap[num4], i * size, j * size, 0);
                                continue;
                            }
                        }
                    }
                    if ((tileTypeAt(i, j) & T_TREE) == T_TREE)
                    {
                        bx = i * size - GameScr.cmx;
                        dbx = bx - GameScr.gW2;
                        dfx = (size - 2) * dbx / size;
                        fx = dfx + GameScr.gW2;
                        g.drawImage(imgTileMap[num], fx + GameScr.cmx, j * size, 0);
                    }
                    else if ((tileTypeAt(i, j) & T_DOWN1PIXEL) == T_DOWN1PIXEL)
                    {
                        if (num != -1)
                        {
                            g.drawImage(imgTileMap[num], i * size, j * size, 0);
                            g.drawImage(imgTileMap[num], i * size, j * size + 1, 0);
                        }
                    }
                    else if (num != -1)
                    {
                        g.drawImage(imgTileMap[num], i * size, j * size, 0);
                    }
                }
            }
            if (mapID == 6 && Screen.width > pxw && mGraphics.zoomLevel != 3)
            {
                g.setColor(0);
                g.fillRect(GameScr.cmx, 0, 80, pxh);
                g.setColor(0);
                g.fillRect(pxw - 24, 0, 50, pxh);
            }
        }
        catch (IOException)
        {
        }
    }

    public static void paintMiniMap(mGraphics g)
    {
        Res.resetTrans(g);
        g.translate(posMiniMapX + 1, posMiniMapY + 2);
        int num = Char.getMyChar().cx / 12;
        int num2 = Char.getMyChar().cy / 12;
        g.setColor(0);
        g.fillRect(-2, -2, wMiniMap + 2, hMiniMap);
        g.setClip(-2, -2, wMiniMap + 4, hMiniMap + 4);
        for (int i = 0; i < 2; i++)
        {
            g.setColor(colorMini[i]);
            g.drawRect(i - 2, i - 2, wMiniMap + 2 - i * 2, hMiniMap - i * 2);
        }
        g.setClip(0, 0, wMiniMap - 2, hMiniMap - 3);
        if (mGraphics.getImageWidth(imgMiniMap) > wMiniMap || mGraphics.getImageHeight(imgMiniMap) > hMiniMap)
        {
            g.translate(-cmxMini, -cmyMini);
        }
        g.drawImage(imgMiniMap, 0, 0, 0);
        g.setColor(16777215);
        g.fillRect(num - 2, num2 - 2, 5, 5);
        g.setColor(16711680);
        g.fillRect(num - 1, num2 - 1, 3, 3);
        for (int j = 0; j < GameScr.vParty.size(); j++)
        {
            Party party = (Party)GameScr.vParty.elementAt(j);
            if (party.c != null && party.c != Char.getMyChar())
            {
                int num3 = party.c.cx / 12;
                int num4 = party.c.cy / 12;
                if (num3 < cmxMini)
                {
                    num3 = cmxMini;
                }
                if (num4 < cmyMini)
                {
                    num4 = cmyMini;
                }
                if (num3 > cmxMini + wMiniMap)
                {
                    num3 = cmxMini + wMiniMap;
                }
                if (num4 > cmyMini + hMiniMap)
                {
                    num4 = cmyMini + hMiniMap;
                }
                if (GameCanvas.gameTick % 10 < 8)
                {
                    g.setColor(16777215);
                    g.fillRect(num3 - 2, num4 - 2, 5, 5);
                    g.setColor(65280);
                    g.fillRect(num3 - 1, num4 - 1, 3, 3);
                }
            }
        }
        Res.resetTrans(g);
        if (GameCanvas.isTouch)
        {
            g.drawImage(GameScr.imgMapBorder, posMiniMapX - 1, posMiniMapY, 0);
        }
    }

    public static void paintOutTilemap(mGraphics g)
    {
        if (GameCanvas.lowGraphic || Main.isIpod || mGraphics.zoomLevel == 1)
        {
            return;
        }
        for (int i = GameScr.gssx; i < GameScr.gssxe; i++)
        {
            for (int j = GameScr.gssy; j < GameScr.gssye; j++)
            {
                Image arg = ((tileID != 4) ? imgWaterflow : imgflowRiver);
                if ((tileTypeAt(i, j) & T_WATERFLOW) == T_WATERFLOW)
                {
                    g.drawRegion(arg, 0, (GameCanvas.gameTick % 8 >> 2) * 24, 24, 24, 0, i * size, j * size, 0);
                }
                if ((tileTypeAt(i, j) & T_OUTSIDE) == T_OUTSIDE)
                {
                    g.drawImage(imgTileMap[maps[j * tmw + i] - 1], i * size, j * size, 0);
                }
            }
        }
        if (!GameCanvas.isTouch || !GameCanvas.isTouchControl || GameScr.gssye < tmh - 2)
        {
            return;
        }
        for (int k = GameScr.gssx; k < GameScr.gssxe; k++)
        {
            int num = tmh - 2;
            int num2 = maps[num * tmw + k] - 1;
            if ((tileTypeAt(k, num) & T_WATERFALL) == T_WATERFALL)
            {
                for (int l = 1; l <= 4; l++)
                {
                    g.drawRegion(imgWaterfall, 0, 24 * (GameCanvas.gameTick % 4), 24, 24, 0, k * size, (num + l) * size, 0);
                }
                continue;
            }
            if (mapID == 64)
            {
                defaultSolidTile = 115;
            }
            if ((tileTypeAt(k, num) & T_TOP) == T_TOP || (tileTypeAt(k, num) & T_WATERFLOW) == T_WATERFLOW)
            {
                num2 = defaultSolidTile;
            }
            if (num2 >= 0)
            {
                for (int m = 1; m <= 4; m++)
                {
                    g.drawImage(imgTileMap[num2], k * size, (num + m) * size, 0);
                }
            }
        }
    }

    public static int tileAt(int x, int y)
    {
        try
        {
            return maps[y * tmw + x];
        }
        catch (Exception)
        {
            return 1000;
        }
    }

    public static int tileTypeAt(int x, int y)
    {
        try
        {
            return types[y * tmw + x];
        }
        catch (Exception)
        {
            return 1000;
        }
    }

    public static int tileTypeAtPixel(int px, int py)
    {
        try
        {
            return types[py / size * tmw + px / size];
        }
        catch (Exception)
        {
            return 1000;
        }
    }

    public static bool tileTypeAt(int px, int py, int t)
    {
        try
        {
            return (types[py / size * tmw + px / size] & t) == t;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void setTileTypeAtPixel(int px, int py, int t)
    {
        types[py / size * tmw + px / size] |= t;
    }

    public static void setTileTypeAt(int x, int y, int t)
    {
        types[y * tmw + x] = t;
    }

    public static void killTileTypeAt(int px, int py, int t)
    {
        types[py / size * tmw + px / size] &= ~t;
    }

    public static int tileYofPixel(int py)
    {
        return py / size * size;
    }

    public static int tileXofPixel(int px)
    {
        return px / size * size;
    }

    public static void loadMapFromResource(short mapID)
    {
        int num = 0;
        DataInputStream resourceAsStream = DataInputStream.getResourceAsStream(Main.res + "/map/" + TileMap.mapID);
        tmw = (ushort)resourceAsStream.read();
        tmh = (ushort)resourceAsStream.read();
        maps = new char[resourceAsStream.available()];
        for (int i = 0; i < tmw * tmh; i++)
        {
            maps[i] = (char)resourceAsStream.read();
        }
        types = new int[maps.Length];
    }
    public static void loadTileMapArr()
    {
        Image image = GameCanvas.loadImage("/black");
        blackAr = image.texture.GetPixels(0, 0, miniSize * mGraphics.zoomLevel, miniSize * mGraphics.zoomLevel);
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    public static void loadMusicBackground()
    {
        if (oldMapID != mapID)
        {
            Sound.stopAllBg();
            switch (mapID)
            {
                case 1:
                    Sound.play(Sound.MHirosaki, 0.8f);
                    break;
                case 10:
                    Sound.play(Sound.MKojin, 0.8f);
                    break;
                case 17:
                    Sound.play(Sound.MSanzu, 0.8f);
                    break;
                case 22:
                    Sound.play(Sound.MTone, 0.8f);
                    break;
                case 27:
                    Sound.play(Sound.MHaruna, 0.8f);
                    break;
                case 32:
                    Sound.play(Sound.MChai, 0.8f);
                    break;
                case 38:
                    Sound.play(Sound.MChakumi, 0.8f);
                    break;
                case 48:
                    Sound.play(Sound.MOshin, 0.8f);
                    break;
                case 72:
                    Sound.play(Sound.MOokaza, 0.8f);
                    break;
                case 43:
                    Sound.play(Sound.MEchigo, 0.8f);
                    break;
            }
        }
    }

    public static void updateMusic()
    {
        if (GameCanvas.gameTick % 700 == 0 && !GameCanvas.lowGraphic && !Main.isIpod && mGraphics.zoomLevel != 1)
        {
            if (mapID == 0 || mapID <= 4 || (mapID >= 16 && mapID <= 18) || (mapID >= 24 && mapID <= 27) || mapID == 22 || mapID == 33 || mapID == 34 || mapID == 38 || mapID == 57 || mapID == 58 || mapID == 60 || mapID == 68 || (mapID >= 70 && mapID <= 75) || mapID == 81)
            {
                Sound.play(Sound.MChimKeu, 0.4f);
            }
            else if ((mapID >= 39 && mapID <= 44) || (mapID >= 46 && mapID <= 48) || mapID == 56 || (mapID >= 62 && mapID <= 65))
            {
                Sound.play(Sound.MGiotuyet, 0.4f);
            }
            else if (mapID == 29 || mapID == 35)
            {
                Sound.play(Sound.MHangdong, 0.4f);
            }
            else if (mapID == 50 || mapID == 51 || mapID == 52)
            {
                Sound.play(Sound.MDeKeu, 0.4f);
            }
            else if (mapID == 64)
            {
                if (Res.random(0, 8) % 2 == 0)
                {
                    Sound.play(Sound.MDeKeu, 0.4f);
                }
                else
                {
                    Sound.play(Sound.MHangdong, 0.4f);
                }
            }
        }
        if (getPosVsMainChar() && !isStopping)
        {
            volume += 0.01f;
            if (volume >= 0.1f)
            {
                volume = 0.1f;
            }
            if (!Sound.isPlayingSoundBG(Sound.MNuocChay))
            {
                Sound.playSoundBGLoop(Sound.MNuocChay, volume);
            }
            Sound.SoundBGLoop.GetComponent<AudioSource>().volume = volume;
        }
        else
        {
            Sound.SoundBGLoop.GetComponent<AudioSource>().volume -= 0.01f;
            isStopping = true;
            if (Sound.SoundBGLoop.GetComponent<AudioSource>().volume <= 0f)
            {
                Sound.SoundBGLoop.GetComponent<AudioSource>().volume = 0f;
                Sound.sTopSoundBG(Sound.MNuocChay);
                isStopping = false;
                volume = 0f;
            }
        }
    }

    static TileMap()
    {
        MapMod = -1;
        Time = 10;
        objLockMap = new object();
        T_EMPTY = 0;
        T_TOP = 2;
        T_LEFT = 4;
        T_RIGHT = 8;
        T_TREE = 16;
        T_WATERFALL = 32;
        T_WATERFLOW = 64;
        T_TOPFALL = 128;
        T_OUTSIDE = 256;
        T_DOWN1PIXEL = 512;
        T_BRIDGE = 1024;
        T_UNDERWATER = 2048;
        T_SOLIDGROUND = 4096;
        T_BOTTOM = 8192;
        T_DIE = 16384;
        T_HEBI = 32768;
        T_BANG = 65536;
        T_JUM8 = 131072;
        T_NT0 = 262144;
        T_NT1 = 524288;
        size = 24;
        mapName1 = null;
        mapName = string.Empty;
        versionMap = 1;
        vGo = new MyVector();
        MAP_NORMAL = 0;
        MAP_DAUTRUONG = 1;
        MAP_PB = 2;
        MAP_CHIENTRUONG = 3;
        locationStand = new mHashtable();
        itemMap = new mHashtable();
        totalTileLoad = 0;
        totalWater = new MyVector();
        totalTile = new int[4] { 120, 141, 143, 103 };
        sizeMiniMap = 2;
        colorMini = new int[2] { 5257738, 8807192 };
        imgTileMap = new Image[150];
        colorMiniMap = new Color[4][][];
        miniSize = 2;
        saveTileId = -1;
        at2Short = new short[160][];
        atBool = new bool[160];
        atInt = new int[160];
        atShort = new short[160];
        cj = true;
        ci = 1;
        at2Short[0] = new short[1] { 27 };
        at2Short[1] = new short[13]
        {
            2, 3, 27, 72, 91, 94, 105, 114, 125, 157,
            139, 113, 80
        };
        at2Short[2] = new short[2] { 6, 1 };
        at2Short[3] = new short[2] { 1, 4 };
        at2Short[4] = new short[2] { 3, 5 };
        at2Short[5] = new short[2] { 7, 4 };
        at2Short[6] = new short[4] { 7, 2, 20, 21 };
        at2Short[7] = new short[3] { 6, 5, 8 };
        at2Short[8] = new short[2] { 7, 9 };
        at2Short[9] = new short[2] { 8, 10 };
        at2Short[10] = new short[9] { 9, 11, 17, 22, 32, 38, 43, 48, 139 };
        at2Short[11] = new short[2] { 12, 10 };
        at2Short[12] = new short[2] { 11, 57 };
        at2Short[13] = new short[2] { 57, 14 };
        at2Short[14] = new short[2] { 13, 15 };
        at2Short[15] = new short[2] { 14, 16 };
        at2Short[16] = new short[2] { 15, 17 };
        at2Short[17] = new short[9] { 16, 18, 10, 22, 32, 38, 43, 48, 139 };
        at2Short[18] = new short[2] { 17, 19 };
        at2Short[19] = new short[2] { 18, 58 };
        at2Short[20] = new short[1] { 6 };
        at2Short[21] = new short[2] { 22, 6 };
        at2Short[22] = new short[9] { 23, 21, 10, 17, 32, 38, 43, 48, 139 };
        at2Short[23] = new short[3] { 22, 69, 25 };
        at2Short[24] = new short[2] { 59, 36 };
        at2Short[25] = new short[2] { 23, 26 };
        at2Short[26] = new short[2] { 27, 25 };
        at2Short[27] = new short[13]
        {
            26, 28, 1, 72, 91, 94, 105, 114, 125, 157,
            139, 113, 80
        };
        at2Short[28] = new short[2] { 27, 60 };
        at2Short[29] = new short[2] { 60, 30 };
        at2Short[30] = new short[2] { 29, 31 };
        at2Short[31] = new short[2] { 32, 30 };
        at2Short[32] = new short[9] { 31, 61, 10, 17, 22, 38, 43, 48, 139 };
        at2Short[33] = new short[2] { 61, 34 };
        at2Short[34] = new short[2] { 35, 33 };
        at2Short[35] = new short[2] { 34, 66 };
        at2Short[36] = new short[2] { 37, 24 };
        at2Short[37] = new short[1] { 36 };
        at2Short[38] = new short[9] { 67, 68, 10, 17, 22, 32, 43, 48, 139 };
        at2Short[39] = new short[3] { 72, 46, 40 };
        at2Short[40] = new short[3] { 39, 65, 41 };
        at2Short[41] = new short[3] { 42, 40, 43 };
        at2Short[42] = new short[2] { 62, 41 };
        at2Short[43] = new short[9] { 41, 44, 10, 17, 22, 32, 38, 48, 139 };
        at2Short[44] = new short[2] { 43, 45 };
        at2Short[45] = new short[2] { 44, 53 };
        at2Short[46] = new short[3] { 63, 39, 47 };
        at2Short[47] = new short[2] { 46, 48 };
        at2Short[48] = new short[9] { 47, 50, 10, 17, 22, 32, 38, 43, 139 };
        at2Short[49] = new short[2] { 50, 51 };
        at2Short[50] = new short[2] { 48, 49 };
        at2Short[51] = new short[2] { 52, 49 };
        at2Short[52] = new short[2] { 51, 64 };
        at2Short[53] = new short[2] { 54, 45 };
        at2Short[54] = new short[2] { 55, 53 };
        at2Short[55] = new short[1] { 54 };
        at2Short[56] = new short[1] { 72 };
        at2Short[57] = new short[2] { 12, 13 };
        at2Short[58] = new short[1] { 19 };
        at2Short[59] = new short[2] { 68, 24 };
        at2Short[60] = new short[2] { 28, 29 };
        at2Short[61] = new short[2] { 33, 32 };
        at2Short[62] = new short[1] { 42 };
        at2Short[63] = new short[1] { 46 };
        at2Short[64] = new short[1] { 52 };
        at2Short[65] = new short[1] { 40 };
        at2Short[66] = new short[2] { 67, 35 };
        at2Short[67] = new short[2] { 66, 38 };
        at2Short[68] = new short[2] { 59, 38 };
        at2Short[69] = new short[2] { 70, 23 };
        at2Short[70] = new short[2] { 69, 71 };
        at2Short[71] = new short[2] { 72, 70 };
        at2Short[72] = new short[13]
        {
            71, 39, 1, 27, 91, 94, 105, 114, 125, 157,
            139, 113, 80
        };
        at2Short[73] = new short[1] { 1 };
        at2Short[74] = new short[0];
        at2Short[75] = new short[0];
        at2Short[76] = new short[0];
        at2Short[77] = new short[0];
        at2Short[78] = new short[0];
        at2Short[79] = new short[0];
        at2Short[80] = new short[3] { 81, 82, 83 };
        at2Short[81] = new short[2] { 80, 84 };
        at2Short[82] = new short[2] { 80, 85 };
        at2Short[83] = new short[2] { 80, 86 };
        at2Short[84] = new short[2] { 81, 87 };
        at2Short[85] = new short[2] { 82, 88 };
        at2Short[86] = new short[2] { 83, 89 };
        at2Short[87] = new short[2] { 84, 90 };
        at2Short[88] = new short[2] { 85, 90 };
        at2Short[89] = new short[2] { 86, 90 };
        at2Short[90] = new short[0];
        at2Short[91] = new short[1] { 92 };
        at2Short[92] = new short[2] { 91, 93 };
        at2Short[93] = new short[1] { 92 };
        at2Short[94] = new short[1] { 95 };
        at2Short[95] = new short[2] { 94, 96 };
        at2Short[96] = new short[2] { 95, 97 };
        at2Short[97] = new short[1] { 96 };
        at2Short[98] = new short[1] { 99 };
        at2Short[99] = new short[4] { 98, 101, 100, 102 };
        at2Short[100] = new short[2] { 99, 103 };
        at2Short[101] = new short[2] { 99, 103 };
        at2Short[102] = new short[2] { 99, 103 };
        at2Short[103] = new short[4] { 101, 102, 104, 100 };
        at2Short[104] = new short[1] { 103 };
        at2Short[105] = new short[3] { 107, 106, 108 };
        at2Short[106] = new short[2] { 105, 109 };
        at2Short[107] = new short[2] { 105, 109 };
        at2Short[108] = new short[2] { 105, 109 };
        at2Short[109] = new short[3] { 106, 107, 108 };
        at2Short[110] = new short[0];
        at2Short[111] = new short[0];
        at2Short[112] = new short[1] { 113 };
        at2Short[113] = new short[1] { 112 };
        at2Short[114] = new short[1] { 115 };
        at2Short[115] = new short[2] { 114, 116 };
        at2Short[116] = new short[1] { 115 };
        at2Short[117] = new short[0];
        at2Short[118] = new short[0];
        at2Short[119] = new short[0];
        at2Short[120] = new short[0];
        at2Short[121] = new short[0];
        at2Short[122] = new short[0];
        at2Short[123] = new short[0];
        at2Short[124] = new short[0];
        at2Short[125] = new short[1] { 126 };
        at2Short[126] = new short[2] { 125, 127 };
        at2Short[127] = new short[2] { 126, 128 };
        at2Short[128] = new short[1] { 127 };
        at2Short[129] = new short[0];
        at2Short[130] = new short[0];
        at2Short[131] = new short[0];
        at2Short[132] = new short[0];
        at2Short[133] = new short[0];
        at2Short[134] = new short[1] { 138 };
        at2Short[135] = new short[1] { 138 };
        at2Short[136] = new short[1] { 138 };
        at2Short[137] = new short[1] { 138 };
        at2Short[138] = new short[4] { 134, 135, 136, 137 };
        at2Short[139] = new short[1] { 140 };
        at2Short[140] = new short[2] { 139, 141 };
        at2Short[141] = new short[2] { 140, 142 };
        at2Short[142] = new short[2] { 141, 143 };
        at2Short[143] = new short[2] { 142, 144 };
        at2Short[144] = new short[2] { 143, 145 };
        at2Short[145] = new short[2] { 144, 146 };
        at2Short[146] = new short[2] { 145, 147 };
        at2Short[147] = new short[2] { 146, 148 };
        at2Short[148] = new short[1] { 147 };
        at2Short[149] = new short[0];
        at2Short[150] = new short[0];
        at2Short[151] = new short[0];
        at2Short[152] = new short[0];
        at2Short[153] = new short[0];
        at2Short[154] = new short[0];
        at2Short[155] = new short[0];
        at2Short[156] = new short[0];
        at2Short[157] = new short[2] { 158, 159 };
        at2Short[158] = new short[2] { 157, 159 };
        at2Short[159] = new short[2] { 158, 157 };
    }

    public static void LockMap(int milisSeconds = 10000)
    {
        isLockMap = true;
        lock (objLockMap)
        {
            try
            {
                Monitor.Wait(objLockMap, milisSeconds);
            }
            catch (ThreadInterruptedException)
            {
            }
        }
    }

    public static void HuyLockMap()
    {
        if (isLockMap)
        {
            isLockMap = false;
            lock (objLockMap)
            {
                Monitor.PulseAll(objLockMap);
            }
        }
    }
    public static int collisionY(int x, int y)
    {
        // if (!(y % 24 != 0 && tileTypeAt(x, y, TileMap.T_TOP) && tileTypeAt(x, y,
        // TileMap.T_SOLIDGROUND))) {
        y = (y / 24 * 24);
        while (y < TileMap.pxh)
        {
            if (tileTypeAt(x, y, TileMap.T_TOP) || tileTypeAt(x, y, TileMap.T_BRIDGE)
                    || tileTypeAt(x, y, TileMap.T_WATERFLOW))
            {
                return y;
            }
            y += 24;
        }
        // }
        return (TileMap.pxh - 1);
    }
    public static int pMove(int n, int n2)
    {
        if (((uint)tileTypeAtPixel(n, n2 - 16) & 0x4002u) != 0)
        {
            n2 = tileXofPixel(n2);
            for (int i = 24; i < 240; i += 24)
            {
                int num = tileTypeAtPixel(n, n2 - i);
                if (n2 - i > 0 && (num & 0x4002) == 0)
                {
                    return n2 - i + 24;
                }
            }
            for (int j = 24; j < 120; j += 24)
            {
                int num2 = tileTypeAtPixel(n, n2 + j);
                if (n2 + j < pxh && (num2 & 0x4002) == 0)
                {
                    return n2 + j;
                }
            }
        }
        return n2;
    }

    public static bool isTruong(int paramInt)
    {
        if (paramInt != 1 && paramInt != 27)
        {
            return paramInt == 72;
        }
        return true;
    }

    public static bool isLang(int paramInt)
    {
        if (paramInt != 10 && paramInt != 17 && paramInt != 22 && paramInt != 32 && paramInt != 38 && paramInt != 43 && paramInt != 48)
        {
            return paramInt == 138;
        }
        return true;
    }

    public static void FixMove(int n, int n2)
    {
        int cx = Char.getMyChar().cx;
        int num = Math.abs(n - cx) / 50;
        cx = Char.getMyChar().cy;
        int num2 = Math.abs(n2 - cx) / 10;
        cx = Char.getMyChar().cx;
        int num3 = Char.getMyChar().cy;
        if (num2 < 3)
        {
            num3 = n2 - 60;
            Service.gI().charMove(Char.getMyChar().cx, num3);
            num2 = 3;
        }
        for (int i = 0; i < num; i++)
        {
            cx = ((n > Char.getMyChar().cx) ? (cx + 50) : (cx - 50));
            Service.gI().charMove(cx, num3);
        }
        Service.gI().charMove(n, num3);
        for (int j = 0; j < num2; j++)
        {
            num3 = ((n2 > Char.getMyChar().cy) ? (num3 + 10) : (num3 - 10));
            Service.gI().charMove(n, num3);
        }
        Service.gI().charMove(n, n2);
        Char.getMyChar().timelastSendmove = mSystem.currentTimeMillis();
        Char myChar = Char.getMyChar();
        Char.getMyChar().cxSend = n;
        myChar.cx = n;
        Char myChar2 = Char.getMyChar();
        Char.getMyChar().cySend = n2;
        myChar2.cy = n2;
    }

    public static void NextMap(int paramInt)
    {
        Waypoint waypoint = (Waypoint)vGo.elementAt(paramInt);
        int num = waypoint.minX;
        int num2 = waypoint.minY;
        if (waypoint.minY != 0 && waypoint.maxY < pxh - 24)
        {
            if (waypoint.maxX <= pxw / 2)
            {
                num = waypoint.maxX + 12;
                num2 = waypoint.maxY;
            }
            else if (waypoint.minX >= pxh / 2)
            {
                num = waypoint.minX - 12;
                num2 = waypoint.maxY;
            }
        }
        else if (waypoint.maxY <= pxh / 2)
        {
            num = (waypoint.maxX + waypoint.minX) / 2;
            num2 = waypoint.maxY + 24;
        }
        else if (waypoint.minY >= pxh / 2)
        {
            num = (waypoint.maxX + waypoint.minX) / 2 + 24;
            num2 = waypoint.maxY - 48;
        }
        if (pxw / 3 >= 1200 && Math.abs(Char.getMyChar().cx - num) > pxw / 3)
        {
            int num3 = pxw / 3;
            int num4 = pxw / 3 + pxw / 3;
            if (Char.getMyChar().cx > num)
            {
                if (Math.abs(Char.getMyChar().cx - num) > num4)
                {
                    Char.Move(num4, YDirtNeMob(num4, num2));
                    Cool();
                }
                if (Math.abs(Char.getMyChar().cx - num) > num3)
                {
                    Char.Move(num3, YDirtNeMob(num3, num2));
                    Cool();
                }
            }
            else
            {
                if (Math.abs(Char.getMyChar().cx - num) > num4)
                {
                    Char.Move(num3, YDirtNeMob(num3, num2));
                    Cool();
                }
                if (Math.abs(Char.getMyChar().cx - num) > num3)
                {
                    Char.Move(num4, YDirtNeMob(num4, num2));
                    Cool();
                }
            }
        }
        else if (pxw / 2 >= 1200 && Math.abs(Char.getMyChar().cx - num) > pxw / 2)
        {
            int num5 = pxw / 2;
            Char.Move(num5 + 35, YDirtNeMob(num5, num2));
            Cool();
        }
        if (mapID == 114 || mapID == 115 || mapID == 116)
        {
            FixMove(num, num2);
        }
        else if (mapID == 23)
        {
            Char.Move(num - 12, num2);
        }
        else if (mapID == 6)
        {
            if (paramInt == 1 || paramInt == 0)
            {
                Char.Move(num - 12, num2);
            }
            else
            {
                Char.Move(num, num2);
            }
        }
        else
        {
            Char.Move(num, num2);
        }
        Handle.Sleep(200);
        Service.gI().requestChangeMap();
    }

    public static bool GoMap(int mapGo)
    {
        short num = mapID;
        MapMod = mapGo;
        MyVector myVector;
        if (num >= 0 && num < at2Short.Length && mapGo >= 0 && mapGo < at2Short.Length && at2Short[num].Length != 0)
        {
            TaskOrder taskOrder = Char.FindTask(0);
            for (int i = 0; i < atBool.Length; i++)
            {
                atBool[i] = true;
                atInt[i] = -1;
                atShort[i] = -1;
            }
            atInt[num] = 0;
            while (true)
            {
                if (atBool[mapGo])
                {
                    int num2 = -1;
                    short num3 = -1;
                    for (int j = 0; j < at2Short.Length; j++)
                    {
                        if (atBool[j] && atInt[j] != -1 && (atInt[j] < num2 || num2 == -1))
                        {
                            num2 = atInt[j];
                            num3 = (short)j;
                        }
                    }
                    if (num3 == -1)
                    {
                        myVector = null;
                        break;
                    }
                    atBool[num3] = false;
                    bool flag = isTruong(num3);
                    short[] array = at2Short[num3];
                    foreach (short num4 in array)
                    {
                        if (!atBool[num4])
                        {
                            continue;
                        }
                        bool flag2;
                        if (Char.getMyChar().isHuman)
                        {
                            int ctaskId = Char.getMyChar().ctaskId;
                            if ((num4 == 1 || num4 == 27 || num4 == 72) && ctaskId < 6)
                            {
                                flag2 = false;
                            }
                            else if ((num4 == 10 || num4 == 32 || num4 == 48) && ctaskId < 17)
                            {
                                flag2 = false;
                            }
                            else if (num4 == 38 && ctaskId < 28)
                            {
                                flag2 = false;
                            }
                            else if (num4 == 43 && ctaskId < 33)
                            {
                                flag2 = false;
                            }
                            else if (num4 == 17 && ctaskId < 37)
                            {
                                flag2 = false;
                            }
                            else
                            {
                                if (num4 != 7 || ctaskId >= 15)
                                {
                                    goto IL_020b;
                                }
                                flag2 = false;
                            }
                            goto IL_01af;
                        }
                        goto IL_020b;
                    IL_01af:
                        if (flag2 && (!flag || !isTruong(num4) || Char.getMyChar().ctaskId >= 9) && (atInt[num4] == -1 || atInt[num4] > atInt[num3] + 1))
                        {
                            atInt[num4] = atInt[num3] + 1;
                            atShort[num4] = num3;
                        }
                        continue;
                    IL_020b:
                        flag2 = true;
                        goto IL_01af;
                    }
                    if (flag && taskOrder != null && atBool[taskOrder.mapId] && (atInt[taskOrder.mapId] == -1 || atInt[taskOrder.mapId] > atInt[num3] + 1))
                    {
                        atInt[taskOrder.mapId] = atInt[num3] + 1;
                        atShort[taskOrder.mapId] = num3;
                    }
                    if (flag)
                    {
                        int num5 = (cj ? 98 : 104);
                        if (atInt[num5] == -1 || atInt[num5] > atInt[num3] + 1)
                        {
                            int[] array2 = atInt;
                            array2[num5] = array2[num3] + 1;
                            atShort[num5] = num3;
                        }
                    }
                    continue;
                }
                MyVector myVector2;
                (myVector2 = new MyVector()).addElement(mapGo);
                for (int num6 = mapGo; num6 != num; num6 = atShort[num6])
                {
                    int num7;
                    if (isLang(num7 = atShort[num6]))
                    {
                        if (isLang(num6))
                        {
                            int num8 = 1;
                            switch (num6)
                            {
                                case 10:
                                    num8 = 1;
                                    break;
                                case 17:
                                    num8 = 2;
                                    break;
                                case 22:
                                    num8 = 3;
                                    break;
                                case 32:
                                    num8 = 4;
                                    break;
                                case 38:
                                    num8 = 5;
                                    break;
                                case 43:
                                    num8 = 6;
                                    break;
                                case 48:
                                    num8 = 7;
                                    break;
                            }
                            num7 = num7 | int.MinValue | 0x7000000 | ((num8 << 20) & 0xF00000);
                        }
                        else if (num6 == 139)
                        {
                            num7 = num7 | int.MinValue | 0x5000000 | 0x200000;
                        }
                    }
                    else if (isTruong(num7))
                    {
                        if (isTruong(num6))
                        {
                            int num9 = 0;
                            switch (num6)
                            {
                                case 1:
                                    num9 = 0;
                                    break;
                                case 27:
                                    num9 = 1;
                                    break;
                                case 72:
                                    num9 = 2;
                                    break;
                            }
                            num7 = num7 | int.MinValue | 0x8000000 | ((num9 << 20) & 0xF00000);
                        }
                        else if (taskOrder != null && num6 == taskOrder.mapId)
                        {
                            num7 = num7 | int.MinValue | 0x19000000 | ((ci << 20) & 0xF00000) | 0x30000;
                        }
                        else
                        {
                            switch (num6)
                            {
                                case 91:
                                    num7 = num7 | int.MinValue | 0x200000 | 0x10000;
                                    break;
                                case 80:
                                    num7 = num7 | int.MinValue | 0x100000 | 0x10000;
                                    break;
                                case 104:
                                    num7 = num7 | int.MinValue | 0x19000000 | ((ci + 2 << 20) & 0xF00000) | 0x10000;
                                    break;
                                case 98:
                                    num7 = num7 | int.MinValue | 0x19000000 | ((ci + 2 << 20) & 0xF00000);
                                    break;
                                case 94:
                                    num7 = num7 | int.MinValue | 0x200000 | 0x20000;
                                    break;
                                case 114:
                                    num7 = num7 | int.MinValue | 0x200000 | 0x40000;
                                    break;
                                case 113:
                                    num7 = num7 | int.MinValue | 0x19000000 | ((ci + 3 << 20) & 0xF00000);
                                    break;
                                case 105:
                                    num7 = num7 | int.MinValue | 0x200000 | 0x30000;
                                    break;
                                case 157:
                                    num7 = num7 | int.MinValue | 0x200000 | 0x60000;
                                    break;
                                case 139:
                                    num7 = num7 | int.MinValue | 0x5000000 | 0x200000;
                                    break;
                                case 125:
                                    num7 = num7 | int.MinValue | 0x200000 | 0x50000;
                                    break;
                            }
                        }
                    }
                    myVector2.addElement(num7);
                }
                MyVector myVector3 = new MyVector();
                for (int num10 = myVector2.size() - 1; num10 >= 0; num10--)
                {
                    myVector3.addElement(myVector2.elementAt(num10));
                }
                myVector = myVector3;
                break;
            }
        }
        else
        {
            myVector = null;
        }
        MyVector myVector4 = myVector;
        if (myVector == null)
        {
            InfoMe.addInfo("Không thể chuyển map!");
            return false;
        }
        IsDangGoMap = true;
        try
        {
            int num11 = mapID;
            for (int l = 1; l < myVector4.size(); l++)
            {
                if (!IsDangGoMap)
                {
                    break;
                }
                if (num11 != mapID)
                {
                    break;
                }
                int num12 = (int)myVector4.elementAt(l - 1);
                num11 = (int)myVector4.elementAt(l) & 0xFFFF;
                Npc npc;
                if (num12 < 0)
                {
                    Char.PickNpc((num12 >> 24) & 0x7F, (num12 >> 20) & 0xF, (num12 >> 16) & 0xF);
                }
                else if ((num12 < 134 || num12 > 138) && num11 == 138)
                {
                    if (Char.getMyChar().cPk > 0)
                    {
                        InfoMe.addInfo("Hiếu chiến quá cao!");
                        return false;
                    }
                }
                else if (num12 != 0 && num12 != 56 && num12 != 73)
                {
                    int num13 = -1;
                    for (int m = 0; m < at2Short[num12].Length; m++)
                    {
                        if (at2Short[num12][m] == num11)
                        {
                            num13 = m;
                            break;
                        }
                    }
                    if (num13 == -1)
                    {
                        InfoMe.addInfo("Không thể chuyển map!");
                        return false;
                    }
                    NM(num13);
                }
                else if ((npc = (Npc)GameScr.vNpc.elementAt(0)) != null && npc.statusMe != 15)
                {
                    Char.Move(npc.cx, npc.cy);
                    Char.getMyChar().npcFocus = npc;
                    Service.gI().requestItem(npc.template.npcTemplateId);
                    Service.gI().menu(-1, npc.template.npcTemplateId, 0, 0);
                    Service.gI().getTask(npc.template.npcTemplateId, 0, 0);
                }
                if (mapID != num11)
                {
                    LockMap();
                }
                Handle.Sleep(Handle.SpNextMap);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.Write(ex.StackTrace);
            return false;
        }
        IsDangGoMap = false;
        return mapID == mapGo;
    }

    public static bool a(int paramInt1, int paramInt2, int[] paramArrayOfInt)
    {
        paramInt2 = tileXofPixel(paramInt2);
        if (tileTypeAt(paramInt1, paramInt2, 2))
        {
            paramArrayOfInt[0] = paramInt1;
            paramArrayOfInt[1] = paramInt2;
            return true;
        }
        for (int i = 0; i < 5; i++)
        {
            int num = paramInt2 + i * 24;
            for (int j = 0; j < 5; j++)
            {
                int num2 = paramInt1 - 48 + j * 24;
                if (num < pxh && num2 > 24 && num2 < pxw - 24 && tileTypeAt(num2, num, 2))
                {
                    paramArrayOfInt[0] = num2;
                    paramArrayOfInt[1] = num;
                    return true;
                }
            }
        }
        return false;
    }

    public static bool FindMobnearPointXY(int cx, int cy)
    {
        for (int i = 0; i < GameScr.vMob.size(); i++)
        {
            Mob mob = (Mob)GameScr.vMob.elementAt(i);
            if (mob != null && Math.abs(cx - mob.xFirst) <= 80 && Math.abs(cy - mob.yFirst) <= 120)
            {
                return false;
            }
        }
        return true;
    }

    public static int YDirtNeMob(int cx, int cy)
    {
        cy = tileYofPixel(cy);
        for (int i = 0; i < 240; i++)
        {
            int num = cy - i * 17;
            if (tileTypeAt(cx, num, 2) && FindMobnearPointXY(cx, num) && num > 0 && num < pxh)
            {
                return num - 2;
            }
        }
        for (int j = 0; j < 240; j++)
        {
            int num2 = cy + j * 17;
            if (mapID == 7)
            {
                num2 = cy + j * 2;
            }
            if (tileTypeAt(cx, num2, 2) && FindMobnearPointXY(cx, num2) && num2 > 0 && num2 < pxh)
            {
                return num2 - 2;
            }
        }
        for (int k = 0; k < 240; k++)
        {
            int num3 = cy - k * 17;
            if (FindMobnearPointXY(cx, num3) && num3 > 0 && tileTypeAtPixel(cx, num3) != 1000 && tileTypeAtPixel(cx, num3) != 12302 && tileTypeAtPixel(cx, num3) != 4110 && num3 > 0 && num3 < pxh)
            {
                return num3;
            }
        }
        for (int l = 0; l < 240; l++)
        {
            int num4 = cy + l * 17;
            if (FindMobnearPointXY(cx, num4) && num4 > 0 && tileTypeAtPixel(cx, num4) != 1000 && tileTypeAtPixel(cx, num4) != 12302 && tileTypeAtPixel(cx, num4) != 4110 && num4 > 0 && num4 < pxh)
            {
                return num4;
            }
        }
        return cy;
    }

    public static bool W()
    {
        for (int i = 0; i < GameScr.vMob.size(); i++)
        {
            Mob mob = (Mob)GameScr.vMob.elementAt(i);
            if (mob != null && Math.abs(Char.getMyChar().cx - mob.xFirst) < 100 && Char.getMyChar().cy <= mob.yFirst)
            {
                return true;
            }
        }
        return false;
    }

    public static void Cool()
    {
        if (tileTypeAt(Char.getMyChar().cx, Char.getMyChar().cy, 2))
        {
            Handle.Sleep(2000);
        }
        else if (W())
        {
            Handle.Sleep(1000);
        }
        else
        {
            Handle.Sleep(2000);
        }
    }

    public static bool isHD(int var0)
    {
        return var0 == 91 || var0 == 92 || var0 == 93 || var0 == 94 || var0 == 95 || var0 == 96 || var0 == 97 || var0 == 105 || var0 == 106 || var0 == 107 || var0 == 108 || var0 == 109 || var0 == 114 || var0 == 115 || var0 == 116 || var0 == 125 || var0 == 126 || var0 == 127 || var0 == 128 || var0 == 157 || var0 == 158 || var0 == 159;

    }

    public static void NM(int paramInt)
    {
        Waypoint waypoint = (Waypoint)vGo.elementAt(paramInt);
        int num = waypoint.minX;
        int num2 = waypoint.minY;
        if (waypoint.minY != 0 && waypoint.maxY < pxh - 24)
        {
            if (waypoint.maxX <= pxw / 2)
            {
                num = waypoint.maxX + 12;
                num2 = waypoint.maxY;
            }
            else if (waypoint.minX >= pxh / 2)
            {
                num = waypoint.minX - 12;
                num2 = waypoint.maxY;
            }
        }
        else if (waypoint.maxY <= pxh / 2)
        {
            num = (waypoint.maxX + waypoint.minX) / 2;
            num2 = waypoint.maxY + 24;
        }
        else if (waypoint.minY >= pxh / 2)
        {
            num = (waypoint.maxX + waypoint.minX) / 2 + 24;
            num2 = waypoint.maxY - 48;
        }
        if (mapID == 114 || mapID == 115 || mapID == 116)
        {
            FixMove(num, num2);
        }
        else if (mapID == 23)
        {
            Char.Move(num - 12, num2);
        }
        else if (mapID == 6)
        {
            if (paramInt == 1 || paramInt == 0)
            {
                Char.Move(num - 12, num2);
            }
            else
            {
                Char.Move(num, num2);
            }
        }
        else
        {
            Char.Move(num, num2);
        }
      
        Service.gI().requestChangeMap();
    }
    public static int i(int var0)
    {
        if (isHD(var0))
        {
            switch (var0)
            {
                case 92:
                    return 91;
                case 93:
                    return 92;
                case 95:
                    return 94;
                case 96:
                    return 95;
                case 97:
                    return 96;
                case 106:
                    return 105;
                case 107:
                    return 106;
                case 108:
                    return 107;
                case 109:
                    return 108;
                case 115:
                    return 114;
                case 116:
                    return 115;
                case 126:
                    return 125;
                case 127:
                    return 126;
                case 128:
                    return 127;
                case 158:
                    return 157;
                case 159:
                    return 158;
            }
        }

        return -1;
    }
    public static int h(int var0)
    {
        if (isHD(var0))
        {
            switch (var0)
            {
                case 91:
                    return 92;
                case 92:
                    return 93;
                case 94:
                    return 95;
                case 95:
                    return 96;
                case 96:
                    return 97;
                case 105:
                    return 106;
                case 106:
                    return 107;
                case 107:
                    return 108;
                case 108:
                    return 109;
                case 114:
                    return 115;
                case 115:
                    return 116;
                case 125:
                    return 126;
                case 126:
                    return 127;
                case 127:
                    return 128;
                case 157:
                    return 158;
                case 158:
                    return 159;
                case 159:
                    return 157;
            }
        }

        return -1;
    }
}
