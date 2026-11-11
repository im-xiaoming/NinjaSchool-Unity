using System.Collections;
using UnityEngine;

// Hierarchy object "NSO Input Control": presses the client's own keys / pointer, so the game can be driven
// from the Inspector buttons (or from a script) without keyboard focus on the Game view.
// Key codes are the client's (MyKeyMap): up -1, down -2, left -3, right -4, enter -5, soft keys -21/-22.
public class NsoInputControl : MonoBehaviour
{
    public const int Up = -1, Down = -2, Left = -3, Right = -4, Enter = -5, SoftLeft = -21, SoftRight = -22;

    [Header("Diagnostic input")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("holdSeconds"), Min(0f)] private float _holdSeconds = 0.15f;
    [Tooltip("Screen pixels, origin top-left.")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("clickPixel")] private Vector2Int _clickPixel = new Vector2Int(400, 300);
    public float holdSeconds { get { return _holdSeconds; } set { _holdSeconds = Mathf.Max(0f, value); } }
    public Vector2Int clickPixel { get { return _clickPixel; } set { _clickPixel = value; } }
    private void OnValidate() { _holdSeconds = Mathf.Max(0f, _holdSeconds); }

    public void Press(int keyCode) { if (Ready) StartCoroutine(PressRoutine(keyCode)); }

    public void Click(int px, int py) { if (Ready) StartCoroutine(ClickRoutine(px, py)); }

    public void Click() { Click(clickPixel.x, clickPixel.y); }

    private static bool Ready { get { return Application.isPlaying && Main.canvas != null; } }

    private IEnumerator PressRoutine(int keyCode)
    {
        Main.canvas.keyPressed(keyCode);
        yield return new WaitForSecondsRealtime(holdSeconds);
        Main.canvas.keyReleased(keyCode);
    }

    private IEnumerator ClickRoutine(int px, int py)
    {
        Main.canvas.pointerPressed(px / mGraphics.zoomLevel, py / mGraphics.zoomLevel);
        yield return new WaitForSecondsRealtime(holdSeconds);
        Main.canvas.pointerReleased(px / mGraphics.zoomLevel, py / mGraphics.zoomLevel);
    }
}
