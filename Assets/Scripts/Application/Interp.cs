using System.Collections.Generic;
using UnityEngine;

// Render-time interpolation. The client logic runs at a fixed, low tick rate (it was written for ~25 ticks per second) and
// moves things by whole pixels per tick. To paint at the screen's refresh rate without changing game speed, the positions
// of the camera, the player, other players, monsters and dropped items are blended between the state before the last
// logic tick and the state after it while painting, then put back. Game logic never sees the blended values.
public static class Interp
{
    private struct Pos { public int x, y; }

    private static readonly Dictionary<object, Pos> prev = new Dictionary<object, Pos>();
    private static int prevCmx, prevCmy;
    private static bool havePrevCamera;

    // blended entries of the current paint, restored afterwards
    private struct Saved { public object o; public int x, y; }
    private static readonly List<Saved> saved = new List<Saved>();
    private static int savedCmx, savedCmy;
    private static bool cameraBlended;

    // Jumps larger than this (logical pixels) are teleports (map change, respawn): shown without blending.
    private const int TeleportDistance = 90;

    // Called right before every logic tick.
    public static void BeforeTick()
    {
        prevCmx = GameScr.cmx;
        prevCmy = GameScr.cmy;
        havePrevCamera = true;
        prev.Clear();
        Remember(Char.getMyChar());
        Collect(GameScr.vCharInMap);
        Collect(GameScr.vMob);
        Collect(GameScr.vItemMap);
    }

    private static void Collect(MyVector v)
    {
        if (v == null) return;
        for (int i = 0; i < v.size(); i++) Remember(v.elementAt(i));
    }

    private static void Remember(object o)
    {
        int x, y;
        if (!TryGet(o, out x, out y)) return;
        prev[o] = new Pos { x = x, y = y };
    }

    private static bool TryGet(object o, out int x, out int y)
    {
        x = y = 0;
        if (o is Char) { Char c = (Char)o; x = c.cx; y = c.cy; return true; }
        if (o is Mob) { Mob m = (Mob)o; x = m.x; y = m.y; return true; }
        if (o is ItemMap) { ItemMap it = (ItemMap)o; x = it.x; y = it.y; return true; }
        return false;
    }

    private static void Set(object o, int x, int y)
    {
        if (o is Char) { Char c = (Char)o; c.cx = x; c.cy = y; }
        else if (o is Mob) { Mob m = (Mob)o; m.x = x; m.y = y; }
        else if (o is ItemMap) { ItemMap it = (ItemMap)o; it.x = x; it.y = y; }
    }

    private static int Lerp(int a, int b, float t)
    {
        return a + Mathf.RoundToInt((b - a) * t);
    }

    private static void Blend(object o, float t)
    {
        if (o == null) return;
        Pos p;
        if (!prev.TryGetValue(o, out p)) return;
        int x, y;
        if (!TryGet(o, out x, out y)) return;
        if (Mathf.Abs(x - p.x) > TeleportDistance || Mathf.Abs(y - p.y) > TeleportDistance) return;
        if (x == p.x && y == p.y) return;
        saved.Add(new Saved { o = o, x = x, y = y });
        Set(o, Lerp(p.x, x, t), Lerp(p.y, y, t));
    }

    private static void BlendAll(MyVector v, float t)
    {
        if (v == null) return;
        for (int i = 0; i < v.size(); i++) Blend(v.elementAt(i), t);
    }

    // Called right before painting; t is how far we are into the current tick (0..1).
    public static void BeginPaint(float t)
    {
        saved.Clear();
        cameraBlended = false;
        if (GameCanvas.currentScreen != GameScr.instance || !havePrevCamera) return;
        savedCmx = GameScr.cmx;
        savedCmy = GameScr.cmy;
        if (Mathf.Abs(savedCmx - prevCmx) <= TeleportDistance * 2 && Mathf.Abs(savedCmy - prevCmy) <= TeleportDistance * 2)
        {
            GameScr.cmx = Lerp(prevCmx, savedCmx, t);
            GameScr.cmy = Lerp(prevCmy, savedCmy, t);
            cameraBlended = true;
        }
        Blend(Char.getMyChar(), t);
        BlendAll(GameScr.vCharInMap, t);
        BlendAll(GameScr.vMob, t);
        BlendAll(GameScr.vItemMap, t);
    }

    // Called right after painting.
    public static void EndPaint()
    {
        for (int i = 0; i < saved.Count; i++) Set(saved[i].o, saved[i].x, saved[i].y);
        saved.Clear();
        if (cameraBlended)
        {
            GameScr.cmx = savedCmx;
            GameScr.cmy = savedCmy;
            cameraBlended = false;
        }
    }
}
