# Unity Map Overlay Bridge

Place Unity objects (prefabs, sprites, effects) on the old Java map. They follow the game's camera scroll and zoom.
This is **visual only**: overlays do not collide, trigger anything or change game state.

## Parts
- `NSO Map Overlay` (in `Assets/Prefabs/NSO Game.prefab`, component `NsoMapOverlayBridge`)
  - Moves every registered `NsoMapObject` each time the game paints.
  - The Runtime fields (map, zone, camera scroll, zoom) are read-only and show what the game painted last.
  - Disable the component to go back to the plain game view.
- `NsoMapObject`: put it on any object you want on the map.
  - `mapId` / `zone`: where it shows (-1 = everywhere).
  - `position`: logical map pixels, origin top-left, Y down. These are the same units as `TileMap` and `Char.cx/cy`; a tile is 24 px.
  - `sortingOrder`: higher than the game layer's `sortingOrder` (0) = in front of the whole game, lower = behind it.
  - `scaleWithZoom` + `spritePixelsPerUnit`: one sprite pixel = one game pixel at every zoom.
  - `visual`: child toggled by the map filter (empty = its renderers are toggled).

## Placing an object
1. Make it a child of `NSO Map Overlay` (or anywhere in the scene) and add `NsoMapObject`.
2. Outside Play mode, the bridge object is the map's top-left corner. Map pixel (x, y) sits at (x, -y) / `editPixelsPerUnit` (24 = one tile per unit). The blue gizmo shows `previewMapSize` with a tile grid.
3. Move the object in the Scene View, then press **Capture Scene Position** to store it in `position`.
   - Or type `position` and press **Apply Logical Position**.
   - Both buttons support Undo and prefab overrides.
4. Press Play. On the matching map the object scrolls with the map.

## Coordinates (NsoMapCoordinates)
- map → screen: `(x - cameraX) * zoom, (y - cameraY) * zoom`, top-left of the game texture.
- screen → world: through the camera's pixel rect, at the game layer's `distance` + `depthOffset`.
- The camera values come from `NsoMapSnapshot.Capture()`, which `Main.OnGUI` calls during paint with the interpolated camera, so overlays match the frame the game layer shows.

## Limits
- An overlay is in front of or behind the **whole** game layer. It cannot go between characters or tiles inside the game texture.
- Map tiles cannot be edited this way.
- There are no automated tests yet. The main scripts live in Assembly-CSharp, which a test assembly cannot reference without an asmdef split.
