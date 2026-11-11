using System.Collections.Generic;
using UnityEngine;

// On-screen console: shows the latest Debug.Log / warning / error lines. F8 shows or hides it, F7 clears it.
public class LogOverlay : MonoBehaviour
{
    private const int MaxLines = 14;

    private struct Line { public string text; public LogType type; }

    private readonly List<Line> lines = new List<Line>();
    public static bool Visible = false; // off by default (costs frame time); switch it on in Settings > Developer / Admin > Debug
    private GUIStyle style;
    private GUIStyle boxStyle;
    private Texture2D bg;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Application.isEditor || !Debug.isDebugBuild) return; // production builds do not install the debug overlay
        GameObject go = new GameObject("LogOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<LogOverlay>();
    }

    private void OnEnable() { Application.logMessageReceivedThreaded += OnLog; }

    private void OnDisable() { Application.logMessageReceivedThreaded -= OnLog; }

    private void OnLog(string message, string stack, LogType type)
    {
        int nl = message.IndexOf('\n');
        if (nl > 0) message = message.Substring(0, nl);
        if (message.Length > 220) message = message.Substring(0, 220);
        lock (lines)
        {
            lines.Add(new Line { text = message, type = type });
            if (lines.Count > MaxLines) lines.RemoveAt(0);
        }
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.F8) Visible = !Visible;
            if (e.keyCode == KeyCode.F7) { lock (lines) lines.Clear(); }
        }
        if (!Visible || e.type != EventType.Repaint) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = false, clipping = TextClipping.Clip };
            style.padding = new RectOffset(0, 0, 0, 0);
            bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            bg.Apply();
            boxStyle = new GUIStyle();
            boxStyle.normal.background = bg;
        }
        float w = Mathf.Min(Screen.width * 0.6f, 1100f);
        float lineH = 18f;
        float h = lineH * (MaxLines + 1) + 8f;
        float x = 4f, y = Screen.height - h - 4f;
        GUI.Box(new Rect(x, y, w, h), GUIContent.none, boxStyle);
        GUI.color = Color.white;
        style.normal.textColor = new Color(0.7f, 0.9f, 1f);
        GUI.Label(new Rect(x + 6, y + 2, w, lineH), "Console (F8 hide, F7 clear)", style);
        lock (lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Line l = lines[i];
                style.normal.textColor = l.type == LogType.Log ? Color.white : l.type == LogType.Warning ? Color.yellow : new Color(1f, 0.4f, 0.4f);
                GUI.Label(new Rect(x + 6, y + 4 + lineH * (i + 1), w - 12, lineH), l.text, style);
            }
        }
    }
}
