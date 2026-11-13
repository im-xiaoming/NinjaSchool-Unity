using System.Collections.Generic;
using UnityEngine;

// Render-time interpolation. The client logic runs at a fixed, low tick rate (it was written for ~25 ticks per second) and
// moves things by whole pixels per tick. To paint at the screen's refresh rate without changing game speed, the positions
// of the camera, the player, other players, monsters and dropped items are blended between the state before the last
// logic tick and the state after it while painting, then put back. Game logic never sees the blended values.
public static class Interp
{
    private struct Pos
    {
        public int x, y;
    }

    private static readonly Dictionary<object, Pos> prev = new Dictionary<object, Pos>();
    private static readonly Dictionary<object, Vector2> residual = new Dictionary<object, Vector2>();
    private static Vector2 activeObjectOffset;
    private static Vector2 cameraOffset;
    private static int previousMap = -1;
    private static bool painting;
    private static int prevCmx, prevCmy;
    private static bool havePrevCamera;
    // blended entries of the current paint, restored afterwards
    private struct Saved
    {
        public object o;
        public int x, y;
    }

    private static readonly List<Saved> saved = new List<Saved>();
    private static int savedCmx, savedCmy;
    private static bool cameraBlended;
    // Jumps larger than this (logical pixels) are teleports (map change, respawn): shown without blending.
    private const int TeleportDistance = 90;
    // Presentation only: interpolate between the same logic snapshots used by the original client.
    public static float TickFraction
    {
        get
        {
            return Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        }
    }

    public static float EffectPosition(float previous, float current)
    {
        return EffectPosition(previous, current, TickFraction);
    }

    public static float EffectPosition(float previous, float current, float fraction)
    {
        return NsoGameControl.SmoothEffects ? Mathf.Lerp(previous, current, Mathf.Clamp01(fraction)) : current;
    }

    // Called right before every logic tick.
    public static void BeforeTick()
    {
        // Capture even while a menu pauses world updates: frozen effects must not oscillate on each new tick.
        GameScr.CaptureEffectPaintState();
        if (GameCanvas.menu != null)
            GameCanvas.menu.CapturePaintState();
        Scroll.CaptureAllPaintStates();
        Info.CapturePaintState();
        InfoMe.CapturePaintState();
        prevCmx = GameScr.cmx;
        prevCmy = GameScr.cmy;
        havePrevCamera = true;
        previousMap = TileMap.mapID;
        RemoteMotion.Prune(GameScr.vCharInMap);
        prev.Clear();
        Remember(Char.getMyChar());
        Collect(GameScr.vCharInMap);
        Collect(GameScr.vMob);
        Collect(GameScr.vItemMap);
        Collect(GameScr.vNpc);
        Collect(GameScr.vMobSoul);
        Collect(GameScr.vLanterns);
        Collect(GameScr.vBuNhin);
        Collect(Mob.vEggMonter);
        Collect(Effect2.vEffect2);
        Collect(Effect2.vEffect2Outside);
        Collect(Effect2.vAnimateEffect);
        Collect(MonsterDart.vDomsang);
    }

    private static void Collect(MyVector v)
    {
        if (v == null)
            return;
        for (int i = 0; i < v.size(); i++)
            Remember(v.elementAt(i));
    }

    private static void Remember(object o)
    {
        if (o == null || prev.ContainsKey(o))
            return;
        if (o is AnimateEffect)
        {
            ((AnimateEffect)o).CapturePaintState();
            return;
        }

        int x, y;
        if (!TryGet(o, out x, out y))
            return;
        prev[o] = new Pos
        {
            x = x,
            y = y
        };
        if (o is Char)
        {
            Char c = (Char)o;
            Remember(c.arr);
            Remember(c.mobMe);
            Remember(c.mobVithu);
            Collect(c.vDomsang);
        }
    }

    private static bool TryGet(object o, out int x, out int y)
    {
        x = y = 0;
        if (o is Char)
        {
            Char c = (Char)o;
            x = c.cx;
            y = c.cy;
            return true;
        }

        if (o is Mob)
        {
            Mob m = (Mob)o;
            x = m.x;
            y = m.y;
            return true;
        }

        if (o is ItemMap)
        {
            ItemMap it = (ItemMap)o;
            x = it.x;
            y = it.y;
            return true;
        }

        if (o is Arrow)
        {
            Arrow a = (Arrow)o;
            x = a.ax;
            y = a.ay;
            return true;
        }

        if (o is MonsterDart)
        {
            MonsterDart d = (MonsterDart)o;
            x = d.x;
            y = d.y;
            return true;
        }

        if (o is Domsang)
        {
            Domsang d = (Domsang)o;
            x = d.xS;
            y = d.yS;
            return true;
        }

        if (o is MobSoul)
        {
            MobSoul s = (MobSoul)o;
            x = s.x;
            y = s.y;
            return true;
        }

        if (o is Lanterns)
        {
            Lanterns l = (Lanterns)o;
            x = l.x;
            y = l.y;
            return true;
        }

        if (o is BuNhin)
        {
            BuNhin b = (BuNhin)o;
            x = b.x;
            y = b.y;
            return true;
        }

        if (o is EggMonters)
        {
            EggMonters e = (EggMonters)o;
            x = e.x;
            y = e.y;
            return true;
        }

        return false;
    }

    private static void Set(object o, int x, int y)
    {
        if (o is Char)
        {
            Char c = (Char)o;
            c.cx = x;
            c.cy = y;
        }
        else if (o is Mob)
        {
            Mob m = (Mob)o;
            m.x = x;
            m.y = y;
        }
        else if (o is ItemMap)
        {
            ItemMap it = (ItemMap)o;
            it.x = x;
            it.y = y;
        }
    }

    private static int Lerp(int a, int b, float t)
    {
        return a + Mathf.RoundToInt((b - a) * t);
    }

    private static void Blend(object o, float t)
    {
        if (o == null)
            return;
        Pos p;
        if (!prev.TryGetValue(o, out p))
            return;
        int x, y;
        if (!TryGet(o, out x, out y))
            return;
        if (o is Char remote && RemoteMotion.TryPosition(remote, out Vector2 networkVisual))
        {
            // Sparse 500 ms network packets need packet-cadence smoothing, not only the last 24 Hz logic tick.
            saved.Add(new Saved { o = o, x = x, y = y });
            int rx = Mathf.RoundToInt(networkVisual.x), ry = Mathf.RoundToInt(networkVisual.y);
            Set(o, rx, ry);
            residual[o] = networkVisual - new Vector2(rx, ry);
            return;
        }

        if (Mathf.Abs(x - p.x) > TeleportDistance || Mathf.Abs(y - p.y) > TeleportDistance)
            return;
        if (x == p.x && y == p.y)
            return;
        Vector2 visual = new Vector2(Mathf.Lerp(p.x, x, t), Mathf.Lerp(p.y, y, t));
        if (o is Char || o is Mob || o is ItemMap)
        {
            saved.Add(new Saved { o = o, x = x, y = y });
            int ix = Lerp(p.x, x, t), iy = Lerp(p.y, y, t);
            Set(o, ix, iy);
            residual[o] = visual - new Vector2(ix, iy);
        }
        else
            residual[o] = visual - new Vector2(x, y);
    }

    private static void BlendAll(MyVector v, float t)
    {
        if (v == null)
            return;
        for (int i = 0; i < v.size(); i++)
            Blend(v.elementAt(i), t);
    }

    // Called right before painting; t is how far we are into the current tick (0..1).
    public static void BeginPaint(float t)
    {
        saved.Clear();
        residual.Clear();
        activeObjectOffset = cameraOffset = Vector2.zero;
        painting = false;
        cameraBlended = false;
        if (GameCanvas.currentScreen != GameScr.instance || !havePrevCamera || previousMap != TileMap.mapID || Char.ischangingMap)
            return;
        t = Mathf.Clamp01(t);
        painting = true;
        savedCmx = GameScr.cmx;
        savedCmy = GameScr.cmy;
        if (Mathf.Abs(savedCmx - prevCmx) <= TeleportDistance * 2 && Mathf.Abs(savedCmy - prevCmy) <= TeleportDistance * 2)
        {
            GameScr.cmx = Lerp(prevCmx, savedCmx, t);
            GameScr.cmy = Lerp(prevCmy, savedCmy, t);
            cameraOffset = new Vector2(GameScr.cmx - Mathf.Lerp(prevCmx, savedCmx, t), GameScr.cmy - Mathf.Lerp(prevCmy, savedCmy, t));
            cameraBlended = true;
        }

        // Each object is blended once even if it appears in several vectors (party/pet aliases).
        foreach (var entry in prev)
            Blend(entry.Key, t);
    }

    // Offline presentation deviation from the original integer client paint paths.
    // Nested projectiles/pets use absolute world coordinates: replace their parent's residual rather than adding it.
    public static Vector2 ObjectOffset(object o)
    {
        Vector2 offset;
        return painting && NsoGameControl.SmoothWorldMotion && o != null && residual.TryGetValue(o, out offset) ? offset : Vector2.zero;
    }

    public static Vector2 CameraOffset
    {
        get
        {
            return painting && NsoGameControl.SmoothWorldMotion ? cameraOffset : Vector2.zero;
        }
    }

    public struct ObjectScope : System.IDisposable
    {
        private Vector2 previous;
        private mGraphics.OffsetScope graphics;
        public ObjectScope(mGraphics g, object o)
        {
            previous = activeObjectOffset;
            Vector2 offset = ObjectOffset(o);
            graphics = g.Offset(offset.x - previous.x, offset.y - previous.y);
            activeObjectOffset = offset;
        }

        public void Dispose()
        {
            graphics.Dispose();
            activeObjectOffset = previous;
        }
    }

    public static ObjectScope Offset(mGraphics g, object o)
    {
        return new ObjectScope(g, o);
    }

    // Called right after painting.
    public static void EndPaint()
    {
        for (int i = 0; i < saved.Count; i++)
            Set(saved[i].o, saved[i].x, saved[i].y);
        saved.Clear();
        residual.Clear();
        painting = false;
        activeObjectOffset = cameraOffset = Vector2.zero;
        if (cameraBlended)
        {
            GameScr.cmx = savedCmx;
            GameScr.cmy = savedCmy;
            cameraBlended = false;
        }
    }
}

// Render-time health transition. This never writes authoritative HP or advances a game timer.
public struct NsoHealthTween
{
    private bool initialized;
    private float start, target;
    private double changedAt;
    public float Sample(float value, double now, float seconds)
    {
        value = Mathf.Clamp01(value);
        if (!initialized || seconds <= 0f || value >= 1f)
        {
            initialized = true;
            start = target = value;
            changedAt = now;
            return value;
        }

        float displayed = Evaluate(now, seconds);
        if (value != target)
        {
            start = displayed;
            target = value;
            changedAt = now;
        }

        return Evaluate(now, seconds);
    }

    private float Evaluate(double now, float seconds)
    {
        float t = Mathf.Clamp01((float)((now - changedAt) / seconds));
        return Mathf.Lerp(start, target, t * t * (3f - 2f * t));
    }
}
