using System.Collections.Generic;
using UnityEngine;

// Text alignment tuner. F6 opens a small panel: Tab / Shift+Tab picks an item, the arrow keys nudge it by 0.1 of a
// logical pixel (hold Shift for whole pixels), Backspace resets the item, F6 closes. Offsets are in logical pixels
// (the game's own units, so they stay right at every zoom level) and are saved in PlayerPrefs.
public class TextCalib : MonoBehaviour
{
    public static readonly string[] Items = { "global", "level", "exp", "hp", "mp", "zone", "button", "sign", "buttonLeft", "buttonCenter", "buttonRight", "command", "titleMenu" };
    public static readonly string[] Labels =
    {
        "All text", "Level number (HUD)", "Exp percent (HUD)", "HP number", "MP number", "Zone numbers", "Button captions (all)", "Zone sign number", "Left button caption", "Center button caption", "Right button caption", "Menu command button caption", "Title menu rows"
    };

    // Items: global, level, exp, hp, mp, zone, button, sign, buttonLeft, buttonCenter, buttonRight, command.
    // Each text has its own offset (game pixels, +x right, +y down) on top of the original coordinates. Measured from a 1920x1080 run
    // at UI zoom 3 so the level and exp sit on the circle's centre and the hp/mp numbers on their bars' centre lines.
    private static readonly float[] defaultX = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
    private static readonly float[] defaultY = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
    private static readonly Dictionary<string, Vector2> values = new Dictionary<string, Vector2>();

    public static bool Open;

    // offset applied to the text drawn between Use() and Clear()
    public static float X, Y;

    public static int Selected = 1;
    private static int selected { get { return Selected; } set { Selected = value; } }

    public static Vector2 Get(string key)
    {
        Vector2 v;
        if (values.TryGetValue(key, out v)) return v;
        int i = System.Array.IndexOf(Items, key);
        v = new Vector2(PlayerPrefs.GetFloat("calibX_" + key, i >= 0 ? defaultX[i] : 0f), PlayerPrefs.GetFloat("calibY_" + key, i >= 0 ? defaultY[i] : 0f));
        values[key] = v;
        return v;
    }

    public static void Put(string key, Vector2 v) { Set(key, v); }

    public static bool TryLive(string key, out Rect r) { return rects.TryGetValue(key, out r) && Live(key); }

    // index of the item whose box (grown by margin screen pixels) contains the point, or -1
    public static int HitTest(float x, float y, float margin)
    {
        for (int i = Items.Length - 1; i >= 0; i--)
        {
            Rect r;
            if (!TryLive(Items[i], out r)) continue;
            r.xMin -= margin; r.xMax += margin; r.yMin -= margin; r.yMax += margin;
            if (r.Contains(new Vector2(x, y))) return i;
        }
        return -1;
    }

    private static void Set(string key, Vector2 v)
    {
        values[key] = v;
        PlayerPrefs.SetFloat("calibX_" + key, v.x);
        PlayerPrefs.SetFloat("calibY_" + key, v.y);
        PlayerPrefs.Save();
    }

    // key of the item being drawn right now (null for text that is not tuned) and where each tuned item was last drawn (screen pixels)
    public static string Key;
    private static readonly Dictionary<string, Rect> rects = new Dictionary<string, Rect>();
    private static readonly Dictionary<string, int> stamps = new Dictionary<string, int>();

    // an item counts as on screen if it was drawn in the last few frames
    private static bool Live(string key) { int f; return stamps.TryGetValue(key, out f) && Time.frameCount - f <= 3; }

    public static void Record(string s, GUIStyle style, float x, float y)
    {
        if (Key == null || !Open) return;
        Vector2 size = style.CalcSize(new GUIContent(s));
        float rx = x;
        if (style.alignment == TextAnchor.UpperCenter) rx = x + ScaleGUI.WIDTH / 2f - size.x / 2f;
        else if (style.alignment == TextAnchor.UpperRight) rx = x + ScaleGUI.WIDTH - size.x;
        rects[Key] = new Rect(rx, y, size.x, size.y);
        stamps[Key] = Time.frameCount;
    }

    public static void RecordBitmap(string key, float x, float y, float width, float height)
    {
        if (key == null || !Open) return;
        rects[key] = new Rect(x, y, width, height);
        stamps[key] = Time.frameCount;
    }

    // The text drawn until Clear() is placed by its drawn pixels on this point (game pixels): centred on it (align 2) or with
    // its left edge on it (align 0), and always centred vertically. Same result at every zoom level. The item's own
    // offset (the Developer / Admin window) is added on top.
    public static bool HasBox;
    public static float BoxX, BoxY;

    public static void UseBox(string key, float x, float y)
    {
        Use(key);
        HasBox = true;
        BoxX = x;
        BoxY = y;
    }

    public static void Use(string key)
    {
        Key = key;
        Vector2 v = Get(key);
        X = v.x;
        Y = v.y;
    }

    public static void Clear()
    {
        Key = null;
        HasBox = false;
        X = 0f;
        Y = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        GameObject go = new GameObject("TextCalib");
        DontDestroyOnLoad(go);
        go.AddComponent<TextCalib>();
    }

    private static float Cx(Rect r) { return r.x + r.width / 2f; }

    private static float Cy(Rect r) { return r.y + r.height / 2f; }

    // Enter: move the selected item so its centre lines up with the nearest other item (x), Shift+Enter: (y)
    private static void AlignToNeighbour(string key, bool vertical)
    {
        Rect me;
        if (!rects.TryGetValue(key, out me)) return;
        float best = float.MaxValue, delta = 0f;
        foreach (KeyValuePair<string, Rect> kv in rects)
        {
            if (kv.Key == key) continue;
            float d = vertical ? Cy(kv.Value) - Cy(me) : Cx(kv.Value) - Cx(me);
            if (Mathf.Abs(d) < best) { best = Mathf.Abs(d); delta = d; }
        }
        if (best == float.MaxValue) return;
        Vector2 v = Get(key);
        float logical = delta / mGraphics.zoomLevel;
        if (vertical) v.y = Mathf.Round((v.y + logical) * 100f) / 100f;
        else v.x = Mathf.Round((v.x + logical) * 100f) / 100f;
        Set(key, v);
    }

    private static void Line(float x, float y, float w, float h, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
    }

    private static void Outline(Rect r, Color c)
    {
        Line(r.x, r.y, r.width, 1f, c);
        Line(r.x, r.yMax, r.width, 1f, c);
        Line(r.x, r.y, 1f, r.height, c);
        Line(r.xMax, r.y, 1f, r.height, c);
    }

    // Boxes around every tuned text, and guide lines at the left / centre / right (x) and top / centre / bottom (y) of the
    // other items. A guide is cyan while the selected item is within 30 px of it and turns green (thicker, and both boxes
    // turn green) when it lines up with the selected item to within one pixel.
    private void DrawGuides()
    {
        Rect sel;
        bool haveSel = rects.TryGetValue(Items[selected], out sel) && Live(Items[selected]);
        Color cyan = new Color(0.3f, 0.9f, 1f, 0.8f), green = new Color(0.2f, 1f, 0.3f, 1f), white = new Color(1f, 1f, 1f, 0.6f);
        bool selAligned = false;
        List<string> alignedKeys = new List<string>();
        if (haveSel)
        {
            float[] sx = { sel.x, Cx(sel), sel.xMax }, sy = { sel.y, Cy(sel), sel.yMax };
            foreach (KeyValuePair<string, Rect> kv in rects)
            {
                if (kv.Key == Items[selected] || !Live(kv.Key)) continue;
                Rect o = kv.Value;
                float[] ox = { o.x, Cx(o), o.xMax }, oy = { o.y, Cy(o), o.yMax };
                bool any = false;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        float dx = Mathf.Abs(ox[i] - sx[j]), dy = Mathf.Abs(oy[i] - sy[j]);
                        if (dx < 1f) { Line(ox[i] - 1f, 0f, 2f, Screen.height, green); any = true; }
                        else if (dx < 30f && i == j) Line(ox[i], 0f, 1f, Screen.height, cyan);
                        if (dy < 1f) { Line(0f, oy[i] - 1f, Screen.width, 2f, green); any = true; }
                        else if (dy < 30f && i == j) Line(0f, oy[i], Screen.width, 1f, cyan);
                    }
                }
                if (any) { selAligned = true; alignedKeys.Add(kv.Key); }
            }
        }
        foreach (KeyValuePair<string, Rect> kv in rects)
        {
            if (!Live(kv.Key)) continue;
            bool isSel = kv.Key == Items[selected];
            Color c = isSel ? (selAligned ? green : Color.yellow) : (alignedKeys.Contains(kv.Key) ? green : white);
            Outline(kv.Value, c);
        }
        GUI.color = Color.white;
    }
    public static string CopyText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        string xs = "", ys = "";
        for (int i = 0; i < Items.Length; i++)
        {
            Vector2 v = Get(Items[i]);
            xs += (i > 0 ? ", " : "") + v.x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "f";
            ys += (i > 0 ? ", " : "") + v.y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "f";
        }
        sb.Append("// items: ").Append(string.Join(", ", Items)).Append("\n");
        sb.Append("private static readonly float[] defaultX = { ").Append(xs).Append(" };\n");
        sb.Append("private static readonly float[] defaultY = { ").Append(ys).Append(" };\n");
        return sb.ToString();
    }

    public static void ResetItem(string key)
    {
        PlayerPrefs.DeleteKey("calibX_" + key);
        PlayerPrefs.DeleteKey("calibY_" + key);
        values.Remove(key);
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;
        if (e.keyCode == KeyCode.F6)
        {
            Open = !Open;
            if (Open) NsoSettingsMenu.OnOpened();   // F6 always opens on the Settings page
            e.Use();
            return;
        }
        if (Open && NsoSettingsMenu.Key(e)) e.Use();   // Settings window, or the text-offset tool inside it
    }
}
