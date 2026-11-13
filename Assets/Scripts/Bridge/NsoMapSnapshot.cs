using UnityEngine;

namespace NsoBridge
{
    // What the game showed in its last paint: map, zone, camera scroll and zoom. Captured by Main.OnGUI while the
    // interpolated (blended) camera is in place, so overlays line up with the texture the game layer shows.
    // Read-only for everything else: overlays never write back into GameScr / TileMap.
    public struct NsoMapSnapshot
    {
        public bool Ready; // the game screen is showing a loaded map
        public int MapId;
        public int Zone;
        public float CameraX; // logical map pixel at the screen's left edge (Y down)
        public float CameraY; // logical map pixel at the screen's top edge
        public int Zoom; // screen pixels per logical pixel (mGraphics.zoomLevel, 1..4)
        public int ScreenWidth; // size of the texture the game painted into
        public int ScreenHeight;
        public int MapWidth; // map size in logical pixels (TileMap.pxw / pxh)
        public int MapHeight;
        public int Version; // goes up on every capture
        public static NsoMapSnapshot Current;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = default(NsoMapSnapshot);
        }

        // Called from Main.OnGUI between Interp.BeginPaint and canvas.paint.
        public static void Capture()
        {
            NsoMapSnapshot s;
            s.Version = Current.Version + 1;
            s.Zoom = Mathf.Max(1, mGraphics.zoomLevel);
            s.ScreenWidth = Screen.width;
            s.ScreenHeight = Screen.height;
            s.MapId = TileMap.mapID;
            s.Zone = TileMap.zoneID;
            s.MapWidth = TileMap.pxw;
            s.MapHeight = TileMap.pxh;
            s.Ready = GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance && !Char.ischangingMap && TileMap.pxw > 0 && TileMap.pxh > 0;
            // same translate as GameScr.paint (map 6 is shifted 40 px on wide screens)
            int extra = Screen.width > TileMap.pxw && mGraphics.zoomLevel != 3 && TileMap.mapID == 6 ? 40 : 0;
            s.CameraX = GameScr.cmx + extra;
            s.CameraY = GameScr.cmy;
            Current = s;
        }
    }
}
