using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Development test driver. Does nothing unless the player is started with "-autotest <scriptFile>".
// Script lines: wait <seconds> | key <code> | hold <code> <seconds> | click <screenPixelX> <screenPixelY> | shot <png path>
// Key codes are the client's own (see MyKeyMap): right -4, left -3, up -1, down -2, enter -5, space 32.
public class DevAutoTest : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-autotest");
        if (i < 0 || i + 1 >= args.Length) return;
        GameObject go = new GameObject("DevAutoTest");
        DontDestroyOnLoad(go);
        go.AddComponent<DevAutoTest>().StartCoroutine(go.GetComponent<DevAutoTest>().Run(args[i + 1]));
    }

    private IEnumerator Run(string file)
    {
        foreach (string raw in File.ReadAllLines(file))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            string[] p = line.Split(' ');
            Debug.Log("[AutoTest] " + line);
            switch (p[0])
            {
                case "wait":
                    yield return new WaitForSecondsRealtime(float.Parse(p[1]));
                    break;
                case "key":
                    Main.canvas.keyPressed(int.Parse(p[1]));
                    yield return new WaitForSecondsRealtime(0.15f);
                    Main.canvas.keyReleased(int.Parse(p[1]));
                    break;
                case "hold":
                    Main.canvas.keyPressed(int.Parse(p[1]));
                    yield return new WaitForSecondsRealtime(float.Parse(p[2]));
                    Main.canvas.keyReleased(int.Parse(p[1]));
                    break;
                case "click":
                    Main.canvas.pointerPressed(int.Parse(p[1]) / mGraphics.zoomLevel, int.Parse(p[2]) / mGraphics.zoomLevel);
                    yield return new WaitForSecondsRealtime(0.15f);
                    Main.canvas.pointerReleased(int.Parse(p[1]) / mGraphics.zoomLevel, int.Parse(p[2]) / mGraphics.zoomLevel);
                    break;
                case "calib":
                    TextCalib.Open = true;
                    NsoSettingsMenu.OpenTextOffsets();   // the test drives the text-offset tool
                    break;
                case "shot":
                    yield return new WaitForEndOfFrame();
                    if (File.Exists(p[1])) File.Delete(p[1]);
                    ScreenCapture.CaptureScreenshot(p[1]);
                    yield return new WaitForSecondsRealtime(0.5f);
                    break;
            }
        }
        Debug.Log("[AutoTest] done");
    }
}
