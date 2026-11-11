using System.Collections.Generic;

/// <summary>
/// Offline client addition (user request): gamepad target switching.
/// Algorithm: a list of the N nearest on-screen targets sorted by ascending elliptical distance
/// from the player, d = (dx / radiusX)^2 + (dy / radiusY)^2. Each switch moves one step along the
/// list from the current focus; after the last entry it wraps to index 0. If the current focus is
/// not in the list, the switch picks index 0 (the nearest target).
/// The list is refreshed continuously (NsoGamepadInput calls Refresh every few frames on the main
/// thread). A refresh is one pass over the map's entity lists (m entities) keeping the N best in a
/// fixed array by insertion: O(m * N) with tiny N, no full sort, no per-call allocation, never
/// blocks the game. Membership checks (O(m)) are only done for the single target picked by Next.
/// Candidates use the same visibility rules as the client's Char.findNextFocusByKey for normal maps
/// (on screen, mob alive, NPC not hidden, player not a clone/invisible/hidden).
/// </summary>
public static class TargetCycler
{
    private static readonly List<object> list = new List<object>();
    // Top-N buffers (nearest first), reused between refreshes; grown only when N grows.
    private static float[] bestD = new float[8];
    private static object[] bestO = new object[8];
    private static int bestCount, bestCap;

    /// <summary>Current sorted list (nearest first). Read-only view for debugging.</summary>
    public static IReadOnlyList<object> Targets { get { return list; } }

    /// <summary>Rebuilds the sorted list. Cheap; call it periodically while in the world.</summary>
    /// <param name="maxTargets">N: how many nearest targets the list holds.</param>
    /// <param name="radiusX">Horizontal ellipse radius (bigger = horizontal distance counts less).</param>
    /// <param name="radiusY">Vertical ellipse radius.</param>
    public static void Refresh(int maxTargets, float radiusX, float radiusY)
    {
        list.Clear();
        Char me = Char.getMyChar();
        if (me == null || GameScr.vMob == null) return;
        float rx = radiusX < 1f ? 1f : radiusX, ry = radiusY < 1f ? 1f : radiusY;
        bestCap = maxTargets < 1 ? 1 : maxTargets;
        if (bestD.Length < bestCap) { bestD = new float[bestCap]; bestO = new object[bestCap]; }
        bestCount = 0;
        // Entities come straight from the map lists, so the O(m) "still in map" check is skipped here.
        for (int i = 0; i < GameScr.vMob.size(); i++) Add(me, GameScr.vMob.elementAt(i), rx, ry);
        for (int i = 0; i < GameScr.vNpc.size(); i++) Add(me, GameScr.vNpc.elementAt(i), rx, ry);
        for (int i = 0; i < GameScr.vCharInMap.size(); i++) Add(me, GameScr.vCharInMap.elementAt(i), rx, ry);
        for (int i = 0; i < GameScr.vItemMap.size(); i++) Add(me, GameScr.vItemMap.elementAt(i), rx, ry);
        for (int i = 0; i < bestCount; i++) { list.Add(bestO[i]); bestO[i] = null; } // ascending distance
    }

    /// <summary>Focuses the next target in the list (wrapping to index 0 after the last).</summary>
    public static void Next()
    {
        Char me = Char.getMyChar();
        if (me == null || me.skillPaint != null || me.arr != null) return; // same guard as findNextFocusByKey
        if (list.Count == 0) return;
        int current = list.IndexOf(CurrentFocus(me));
        // current == -1 (no focus / focus not in list) -> index 0, the nearest target.
        int next = (current + 1) % list.Count;
        if (IsValid(list[next], true)) Focus(me, list[next]);
    }

    private static object CurrentFocus(Char me)
    {
        if (me.mobFocus != null) return me.mobFocus;
        if (me.npcFocus != null) return me.npcFocus;
        if (me.charFocus != null) return me.charFocus;
        return me.itemFocus;
    }

    private static void Add(Char me, object o, float rx, float ry)
    {
        if (o == null || o == me || !IsValid(o, false)) return;
        int x, y;
        Position(o, out x, out y);
        float dx = (x - me.cx) / rx, dy = (y - me.cy) / ry;
        float d = dx * dx + dy * dy;
        if (bestCount == bestCap && d >= bestD[bestCap - 1]) return; // farther than the current N-th
        // Insertion into the sorted top-N array: shift farther entries right by one.
        int j = bestCount < bestCap ? bestCount++ : bestCap - 1;
        while (j > 0 && bestD[j - 1] > d) { bestD[j] = bestD[j - 1]; bestO[j] = bestO[j - 1]; j--; }
        bestD[j] = d; bestO[j] = o;
    }

    private static void Position(object o, out int x, out int y)
    {
        Mob mob = o as Mob; if (mob != null) { x = mob.x; y = mob.y; return; }
        Npc npc = o as Npc; if (npc != null) { x = npc.cx; y = npc.cy; return; }
        Char c = o as Char; if (c != null) { x = c.cx; y = c.cy; return; }
        ItemMap it = (ItemMap)o; x = it.x; y = it.y;
    }

    /// <summary>Still in the map, on screen and targetable (rules from Char.findNextFocusByKey).</summary>
    /// <param name="checkInMap">Also check the object is still in the map list (O(m)); false when it was just read from it.</param>
    private static bool IsValid(object o, bool checkInMap)
    {
        Mob mob = o as Mob;
        if (mob != null)
        {
            if ((checkInMap && !GameScr.vMob.contains(mob)) || mob.status == 0 || mob.status == 1 || !mob.isPaint()) return false;
        }
        else if (o is Npc)
        {
            Npc npc = (Npc)o;
            if ((checkInMap && !GameScr.vNpc.contains(npc)) || npc.statusMe == 15 || !npc.isPaint()) return false;
        }
        else if (o is Char)
        {
            Char c = (Char)o;
            if ((checkInMap && !GameScr.vCharInMap.contains(c)) || c.isNhanbanz() || c.isInvisible || c.statusMe == 15) return false;
        }
        else if (!(o is ItemMap) || (checkInMap && !GameScr.vItemMap.contains(o))) return false;
        int x, y;
        Position(o, out x, out y);
        // Same on-screen rectangle as Char.findNextFocusByKey.
        return GameScr.cmx + 10 <= x && x <= GameScr.cmx + GameCanvas.w - 10
            && GameScr.cmy + 10 <= y && y <= GameScr.cmy + GameScr.gH;
    }

    /// <summary>Sets the focus the same way GameScr.doFocusbyTouch does.</summary>
    private static void Focus(Char me, object o)
    {
        me.mobFocus = null;
        me.deFocusNPC();
        me.charFocus = null;
        me.itemFocus = null;
        if (o is Mob) me.mobFocus = (Mob)o;
        else if (o is Npc) me.npcFocus = (Npc)o;
        else if (o is Char) me.charFocus = (Char)o;
        else me.itemFocus = (ItemMap)o;
        Char.isManualFocus = true;
    }
}
