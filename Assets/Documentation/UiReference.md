# UI Reference: button names, variables, tunables

A quick lookup for "which variable moves X?". All values are **game pixels**; the UI zoom multiplies them when drawing.
Positions use the top-left origin: +x goes right, +y goes down.

## 1. Inspector: object "NSO UI Layout" (in `Assets/Prefabs/NSO Game.prefab`)
Script: `Assets/Scripts/Diagnostics/NsoUiLayout.cs`. You can edit the values live during Play.

### Bottom Buttons (`NsoUiLayout.Buttons`)
These are the two open/close buttons at the bottom of the screen: **Chọn / Đóng**, Menu / Đóng and similar pairs.

| Field | Default | Effect |
|---|---|---|
| `bottomGap` | 6 | Space from the bottom of the button to the bottom edge of the screen. |
| `sidePadding` | 2 | Space from the left button to the left edge, and from the right button to the right edge. |

Code that uses these values (`Assets/Scripts/Rendering/Paint.cs`):
- `Paint.CmdY`: top of the buttons.
- `Paint.CmdLeftX`: x of the left button.
- `Paint.CmdRightX`: x of the right button.
- `Paint.CmdButtonW` / `Paint.CmdButtonH`: button image size (`GameScr.imgLbtn`).

The buttons themselves are drawn by `Paint.paintCmdBar(g, left, center, right)`:
- `left`, `center`, `right` are the `Command` objects of the current screen or menu.
- A `Command` with x > 0 and y > 0 draws itself at its own position (`Command.paint`).
- Otherwise `paintCmdBar` places it with the helpers above.
- The **center** button sits at `GameCanvas.hw - 35` and is not configurable yet.
- Screens that place their own buttons use `y = Paint.CmdY`, so `bottomGap` applies there too. These are SelectCharScr, CreateCharScr, LanguageScr, MapScr, GameScr, MsgDlg, InputDlg and Input2Dlg.
  - Their x stays the screen's own.
  - They read the y when the screen or dialog opens, so after changing `bottomGap` you have to reopen it.
- **Live gap:** a button that was on the bottom row (`y == Paint.CmdY`) follows the row when `bottomGap` changes, so there is no need to reopen the screen (`Command.onBottomRow`).
- **Safety net for every button:** `Command.paint` lifts any caption button that sits closer than `bottomGap` to the bottom up to `Paint.CmdY`.
  - It changes `y` itself, so the click area moves too.
  - Buttons placed higher are left alone.

Mouse hover:
- Only the left and right buttons light up (`Command.hoverEnabled` is turned on while they paint).
- The hit areas are in `Assets/Scripts/Screens/mScreen.cs` (`Paint.CmdY - 5`, height `CmdButtonH + 10`).

### Potion Buttons (`NsoUiLayout.Potions`)
These are the HP/MP potion buttons on PC (`GameScr.xHP/yHP`, `xMP/yMP`, drawn in `GameScr.paintTouchControl`).

| Field | Default | Effect |
|---|---|---|
| `bottomGap` | 6 | Space from the bottom of the buttons to the bottom edge of the screen. It is applied every frame. |

### Npc Menu (`NsoUiLayout.Menu`)
This is the row of NPC dialog buttons (`Assets/Scripts/UI/Components/Menu.cs`).

| Field | Default | Effect |
|---|---|---|
| `bottomGap` | 8 | Space from the menu buttons to the bottom edge of the screen. It is applied live (`Menu.updateMenu`). |

Other variables in `Menu.cs`:
- `menuX`, `menuY`, `menuW`, `menuH`: position and size of the button row.
- `left`: the **Chọn** command.
- `right`: the **Đóng** command, re-placed every frame at `Paint.CmdRightX`, `Paint.CmdY`.
- Navigation keys: ←/→ and **A/D** (`GameCanvas.keyPressedz[18]` = A, `[19]` = D).

### Settings Window (`NsoUiLayout.Settings`)
Layout of the F6 **Cài đặt** window (`Assets/Scripts/Diagnostics/NsoSettingsMenu.cs`). The frame margin, title height, bottom area and button position are shared with Developer Window below.

| Field | Default | Effect |
|---|---|---|
| `windowW` / `windowMaxH` | 300 / 300 | Window size. |
| `tabH` | 18 | Height of the tab row. |
| `rowH` | 20 | Height of one option row. |
| `labelX` | 8 | Left margin of the option names. |
| `controlW` | 104 | Width of the value control on the right (`< value >`, or the slider and its number). |
| `sliderW` | 70 | Width of the slider bar. |
| `controlRightMargin` | 8 | Space between the control and the right edge of the panel. |
| `rowTextY` | 5 | Vertical position of the text inside a row. |
| `hintH` | 24 | Height of the help box between the list and the buttons. |
| `hintGap` | 4 | Space between the list, the help box and the buttons. |
| `switchW` | 26 | Width of the On / Off switch. |

The colours (tab, accent, stripes, boxes, switch) are the `Col*` constants in `NsoSettingsMenu.cs`.

### Developer Window (`NsoUiLayout.Window`)
This is the text-alignment tool, opened from F6 > Dev > Căn chỉnh chữ (`Assets/Scripts/Diagnostics/TextAlignPanel.cs`). Every field has a tooltip in the Inspector.

| Field | Default | Effect |
|---|---|---|
| `valueRow` | 28 | Row height, which sets the space between rows. |
| `boxW` / `boxH` | 38 / 20 | Size of the X/Y value boxes. |
| `boxGap` | 5 | Space between the X box and the Y box. |
| `boxRightMargin` | 8 | Space between the Y box and the right edge of the list. |
| `windowW` / `windowMaxH` | 240 / 300 | Window size. |
| `frameMargin` | 7 | Space from the window edge to the inner panel. |
| `titleH` | 32 | Space reserved for the title. |
| `bottomH` | 70 | Space reserved for the buttons at the bottom. |
| `buttonFromBottom` | 28 | Top of the buttons, measured from the bottom of the window. |
| `buttonH` | 24 | Height of the button hit area. |
| `labelX` | 10 | Left margin of the item names. |
| `listTop` | 8 | Empty space above the first row. |
| `boxOffsetY` / `boxTextOffsetY` | 3 / 2 | Vertical position of the boxes and of their text. |
| `limit` | 100 | Range allowed for an offset. |
| `step` / `shiftStep` | 0.1 / 1 | Arrow-key step, normal and with Shift. |

## 2. F6: Settings window (Cài đặt)
F6 opens the Settings window. `TextCalib.Open` means the window is open, and `NsoSettingsMenu.TextOffsets` means the text-offset tool is shown inside it.
- Options are defined in `Assets/Scripts/Diagnostics/NsoSettings.cs`, in `Build()`. Each option has a getter and a setter.
  - Every change applies at once and is saved in PlayerPrefs `nsoset_<key>`.
  - Saved values are applied again at start, on top of the Inspector values.
  - The **default** of an option is the value the game had at start, so the Inspector stays the source of defaults.
- Window code: `Assets/Scripts/Diagnostics/NsoSettingsMenu.cs`.
- Keys:
  - Q/E or Tab: change tab.
  - Up/Down (W/S): select a row.
  - Left/Right (A/D): change the value. Shift moves sliders 5 steps.
  - Enter/Space: open an action or go to the next choice.
  - Backspace: put the row back to its default.
  - Esc: close.
- Mouse: click a tab, click the left or right half of a choice, click or drag a slider, drag the list to scroll.
- Buttons: **Mặc định** resets the current tab; **Đóng** closes the window.

| Tab | Option (key) | Changes |
|---|---|---|
| Hình ảnh | Chế độ cửa sổ (`windowMode`) | `Screen.fullScreenMode`. It does nothing in the Editor. A saved value is applied on the first frame, after `Main.Start`. |
| | VSync (`vsync`) | `QualitySettings.vSyncCount`. |
| | Giới hạn FPS (`fps`) | `NsoGameControl.targetFrameRate`: 30, 60, 75, 120 or 144. |
| | Khử răng cưa (`antiAliasing`) | NsoGameLayer FXAA. Off, or Thấp / Vừa / Cao presets (`NsoSettings.AaPresets`). |
| | Đồ họa thấp (`lowGraphic`) | `GameCanvas.lowGraphic`. |
| | Cỡ giao diện * (`uiZoom`) | `NsoGameControl.UiZoomOverride`. Takes effect after a restart. |
| | Hiện FPS (`showFps`) | FPS counter at the top right (`NsoSettingsMenu.PaintOverlay`). |
| Âm thanh | Âm thanh | `Sound.SetSound`. Not saved here, because the game saves it itself. |
| | Âm lượng chung (`masterVolume`) | `AudioListener.volume`. |
| | Hiệu ứng (`sfxVolume`) | `Sound.sfxVolume`: effects and footsteps. |
| | Nhạc nền / môi trường (`ambientVolume`) | `Sound.ambientVolume`: the BG loop, water and the menu music. |
| | Nhạc menu (`menuMusic`) | `MenuAudio.menuMusic`. |
| | Âm thanh nút bấm (`uiSounds`) | `MenuAudio.uiSounds`. |
| | Chạy khi thu nhỏ (`runInBackground`) | `NsoGameControl.runInBackground`. |
| Giao diện | `btnBottomGap`, `btnSidePadding`, `potionBottomGap`, `menuBottomGap` | The NSO UI Layout values in section 1. |
| Phím | Information only | The key list. |
| Dev | Căn chỉnh chữ | Opens the text-offset tool (section 3). Esc or **Back** returns to Settings. |
| | Tốc độ logic game (`tickRate`) | `NsoGameControl.logicTicksPerSecond`. |

To add an option, add a `Choice`, `Toggle` or `Slider` line in `NsoSettings.Build()`. The window picks it up by itself.

## 3. F6 > Dev > Căn chỉnh chữ: text offsets (`TextCalib`, saved in PlayerPrefs `calibX_/calibY_<key>`)
Script: `Assets/Scripts/Diagnostics/TextCalib.cs`.
- `TextCalib.Use(key)` adds the key's offset to the original coordinates.
- `TextCalib.UseBox(key, cx, cy)` centers the text by its ink, then adds the offset.

| Key | F6 label | Where it is drawn |
|---|---|---|
| `global` | All text | Every text. |
| `level` | Level number (HUD) | `GameScr.cs` (HUD, UseBox) |
| `exp` | Exp percent (HUD) | `GameScr.cs` (UseBox) |
| `hp` / `mp` | HP / MP number | `GameScr.cs` (UseBox) |
| `zone` | Zone numbers | `GameScr.cs` (zone list) |
| `sign` | Zone sign number | `Gameplay/NPCs/Npc.cs` |
| `button` | Button captions (all) | All button captions. |
| `buttonLeft` / `buttonCenter` / `buttonRight` | Left / Center / Right button caption | `Paint.paintCmdBar` |
| `command` | Menu command button caption | `Command.paint` (for example the menu's Đóng) |
| `titleMenu` | Title menu rows | `Screens/SelectServerScr.cs`: Chơi mới / Tiếp tục / Ngôn ngữ / Cài đặt / Thoát (UseBox) |

## 4. Menu music and button sounds (`Assets/Scripts/Audio/MenuAudio.cs`)
- **Menu screens:** SelectServerScr, LanguageScr, SelectCharScr and CreateCharScr (`MenuAudio.OnMenuScreen`).
- **Music:** `res/music/0.ogg` (`Sound.MLogin`) loops on `Sound.SoundBGLoop` while a menu screen is shown.
  - It fades in and out over `FadeSeconds` (1.2 s) and stops when the game screen opens.
  - Volume = `MenuAudio.menuMusicVolume` (0.6) × Settings > Nhạc nền / môi trường.
- **Button sounds:** `res/music/1.ogg` (`Sound.MBClick`), only on menu screens.
  - `MenuAudio.Click()` plays from `Command.performAction` and the title menu rows.
  - `MenuAudio.Tick()` plays when the title menu selection moves.

## 4b. Title screen (`Assets/Scripts/Screens/SelectServerScr.cs`)
- Logo: `LocalLogin.imgTitle`, drawn with `g.drawImagePixelScale` at a whole-number scale with Point filtering.
  - Constants: `TitleLogoWidth`, `TitleLogoCenterY`, `TitleLogoScale`.
  - The source is 108×62. To get a sharper logo, put a larger image at `Assets/Resources/res/x3/logo.png`.
- Menu rows: `rowY`, `rowHeight`. The text is centered with `UseBox("titleMenu")`.

## 5. Other objects in the prefab
- **NSO Map Overlay** (`NsoMapOverlayBridge`): places Unity objects on the map. See `UnityMapOverlayBridge.md`.
- **NsoGameLayer**: the quad that shows the game render texture (`sortingOrder` 0, `distance`).
  - **Anti-aliasing**: an FXAA pass over the whole game picture each frame (shader `Assets/Shaders/NsoFxaa.shader`).
    - `antiAliasing`: on/off.
    - `contrastThreshold`, `relativeThreshold`: lower values smooth more edges.
    - `subpixelBlending`: 0 to 1, how soft single-pixel details become.

## 6. Useful helpers
- `mGraphics.isMouseOver(x, y, w, h)`: true when the mouse is over a game rectangle.
- `mGraphics.drawImagePixelScale(img, cx, cy, scale)`: draws an image centered at a whole-number scale with sharp pixels.
- Keys: `GameCanvas.keyPressedz[n]`.
  - 13 = close / back.
  - 18 = A.
  - 19 = D.

*When you add a new tunable, add a row here too.*

## Effect animation tunables (2026-11-11)
NSO Game Control exposes `_smoothEffects` (true), `_mobHealthSeconds` (0.12) and `_menuOpenSeconds` (0.14). They control fractional floating-number/hit-splash/menu presentation and the visual monster HP bar; they do not alter actual HP, damage or tick speed. General movement/camera interpolation and original sprite poses are unchanged. Test: Tools/verify_effect_presentation.cs in Play mode after entering a map.

## Movement release tunable (2026-11-11)
NSO Game Control > Gameplay > `_instantMovementStop` (Instant Movement Stop, default true). On the last horizontal release, stop local grounded manual running without original friction. Disable to restore original client behavior. No change to speed or airborne/destination motion. PC aliases include arrows and A/D through Key.mapKeyPC; number-pad/pointer releases are also handled. Regression: Tools/verify_movement_release.cs, 18/18 in Play.

## Render-motion tunables (2026-11-12)
NSO Game Control: `_smoothWorldMotion` true adds fractional camera/world/projectile placement. `_smoothEffects` also smooths weather particles and Scroll content. Render cap `_targetFrameRate` range is 60-144, default 60; original logic remains 24. Scroll viewport/hit-testing are unchanged. No asset interpolation/new sprite frames. Checks: Tools/verify_world_presentation.cs (94); measurement: Tools/measure_presentation_rate.cs -> %TEMP%/nso-presentation-rate.txt.

## Notification ticker smoothing (2026-11-12)
Info/InfoMe use fractional render-time text placement when NSO Game Control > Smooth Effects is enabled. Logical text speed, dwell time and viewport clipping are unchanged. Final change is compile-verified; visual checks are left to the user.

## Wheel input (2026-11-12)
Information/skill panels use existing Scroll mouse-wheel handling. The input bridge now respects normalized wheel notches; only native Windows range is divided by 120. WheelStepPx remains 24. Hardware verification is left to the user.

## Workbench dark appearance (2026-11-12)
Dev wiki defaults to dark mode. Reading text is mostly 15-17px, labels heavier, navigation wider. UI fonts prefer native Apple/SF Pro; Windows uses Segoe UI when Apple fonts are unavailable. Code prefers SF Mono/Menlo with Consolas fallback. No external font requests or font assets. Refresh the local website to apply.

### Unified web font correction (2026-11-12)
Supersedes the separate code-font description above: code, JSON, IDs and all UI now share --font-ui. There is no separate monospace font family.

## Global balancing dashboard (2026-11-12)
Open #dashboard / Can bang game. Character tab: global percent/flat coefficients for four potentials, fourteen combat stats and future EXP gain. Equipment tab: all equipment or every instance of one template, with percent/flat option rules. Monster tab: durable template values across maps/spawns. Player/level/item/monster samples are read-only; no character/save editor. Save & apply writes game-balance.json or monster-tuning.json; Reset restores the chosen scope. Existing raw points/options are preserved. Percent 0..1000, flat +/-1000000; effective potential 0..30000 and speed 0..17. Forms keep drafts; shared dark font/layout. Browser/runtime validation belongs to user.

## Character progression dashboard (2026-11-12)
Under Character: choose unclassed/classed, then shared class profile or a specific class. Step 1 shows original Level-1 values, optional base targets and server-calculated results. Step 2 edits native growth percent and additive growth per level; blanks inherit. Advanced attributes can be revealed without crowding the core tables. Step 3 previews every level in a chosen range, compares classes and shows milestones/deltas. Inputs trigger read-only draft calculations; Save publishes the selected profile; Discard keeps saved settings; Reset removes selected rules. Classed samples have no allocated points/items/learned skills; level 1 is a theoretical class reference. Existing all-game coefficients/EXP remain in the advanced compatibility section. See docs/CHARACTER_PROGRESSION.md.

### Progression help panels (2026-11-12)
Step 02 and its formula-term guidance default collapsed; expand the heading to read explanations and the worked example. Guidance distinguishes cumulative native growth from per-level extra, blank from zero, and illustrative examples from actual draft results. Uses the existing dark palette/unified font and stacks terms on narrow screens. Syntax-only checked; visual QA left to user.

### Separate Wooden Sword preview (2026-11-12)
Step 03 has Unclassed and Classed comparison groups. Under Unclassed, bare character and Wooden Sword have separate choices/rows. Wooden Sword is enabled by default and uses the quest item; actual school classes stay separate. Step 01 still describes naked base stats.

## Item and monster portraits (2026-11-12)
Workbench item/mob lists, details, related links and selected balance editor headings show original game sprites. Default boxes 48px, large 96px (72px mobile), inline links 24px. Images preserve pixelated rendering and load lazily except large headings. Missing source art shows a question-mark placeholder with a descriptive title. Monsters use static representative frames; bosses are assembled from their sprite-sheet metadata. User checks visual appearance after Play/refresh.

## Open Workbench from F6 (2026-11-12)
F6 > Dev > Mo web can bang is the first action row. Click or Enter opens the live dashboard through the OS default browser. The URL is read at activation so alternate wiki ports work. The selected-row hint explains when the debug wiki has not started or could not open. Existing pointer/keyboard/gamepad action behavior is reused.

### Progression layout correction (2026-11-12)
The standalone top unclassed/classed tab panel is removed. Scope/class selection lives in Step 01. Step 03 has three separate boxes: Unclassed, Wooden Sword, Classed; Wooden Sword is not nested in either other box. Mobile layout stacks these boxes.

### Collapsed growth default (2026-11-12)
Step 02 and its nested explanatory guidance start collapsed. Click the Step-02 heading to expand. Save/discard/reset remain visible below it. Validation opens the section if a growth input needs correction.


## 2026-11-12 - Wide popup command alignment
Bottom Buttons.popupSideInset (default -2 logical pixels) aligns the left/right edges of wide inventory/shop command rows with paintFrame outer borders; the centre button uses actual image width. Applied live by GameScr.paint when the popup fits three original-size buttons and the row stays on screen. Narrow panels retain the legacy row. Vertical position still uses Paint.CmdY.


## 2026-11-12 - World map commands and mouse input
- User explicitly requested removal of the Change button and its feature. MapScr is world-map only: removed action 1001, modeCurrentMap, local-map rendering/markers, local-map scrolling bounds and unused xM/yM fields. Shared gameplay minimap resources remain in use elsewhere.
- Offline deviation from original MapScr: retain Go To (14004) and Close (1000); synchronize command positions and image-sized hitboxes before input and paint. Reserve the actual bottom command row (Paint.CmdY minus Command's 3-pixel hit padding) from map dragging/selection; only map-origin gestures select a destination. Short drags now use absolute displacement on both axes.
- Changed: Assets/Scripts/Screens/MapScr.cs; PROJECT_STATUS.md, TASKS.md, DECISIONS.md, HANDOFF.md, Assets/Documentation/UiReference.md and Assets/StreamingAssets/NsoWiki/notes.json. No architecture/server/save changes.
- Validation: Unity refreshed and compilation completed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/mouse runtime tests per user compile-only instruction. Runtime visual/input verification remains pending.
- Next: user opens the map and checks only Go To/Close are present; clicks across each button image including edges; checks map dragging, destination selection and Go To. Compile commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Small1131 background removal completed
- User explicitly selected deterministic code-based background removal. Added Tools/Remove-SmallIconBackground.ps1, using border-connected white/near-white flood fill (configurable WhiteThreshold=235; 255 selects pure white). No AI redraw, rescaling or RGB edits.
- Zoom 1 (16x14) already transparent and byte-identical. Changed zoom 2 (32x28): 181 pixels; zoom 3 (48x42): 426 pixels; restored original zoom 4 (64x56): 800 pixels. Only selected background alpha changed to zero. All foreground pixels, original RGB and .meta bytes verified unchanged. PNG output re-read to verify every pixel and dimensions before atomic replacement.
- Changed: Tools/Remove-SmallIconBackground.ps1, Assets/StreamingAssets/NsoStatic/Data/Img/Small/{2,3,4}/Small1131.png; status/task/decision/handoff docs, UiReference.md and wiki notes. Original PNG backups: C:/Users/PC/.codex/backups/Small1131-20261112-193523. Earlier user-edited zoom 4 remains in C:/Users/PC/.codex/backups/nso-Small1131-20261112-193205/zoom4-edited.png.
- Validation: script completed all four variants and verified saved pixels/dimensions/meta; inspected cleaned zoom 4. Unity refresh performed. No C# changes or Play/mouse/gameplay tests. No live icon/RMS/browser cache mutations; an already cached icon may still display old art until reloaded.
- Next: user verifies fresh icon rendering. Reusable command: powershell -NoProfile -File Tools/Remove-SmallIconBackground.ps1 -IconId 1131 -WhiteThreshold 235. Tool makes per-run backups outside Assets, keeps dimensions, and is idempotent. Unity Console checked: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0.


## 2026-11-12 - Small1131 white fringe correction
- Previous alpha-only removal left resampled matte pixels below brightness 235. Cleaned the border-connected bright fringe at threshold 160, removed pixels outside the original transparent zoom-1 silhouette, and recovered brightened contour colors from zoom 1 only in silhouette-edge cells. Interior colors are unchanged.
- Added opt-in -UseZoom1Silhouette to Tools/Remove-SmallIconBackground.ps1; validates exact integer zoom dimensions and checks every output pixel against either original foreground, transparent background or expected recovered contour color. Zoom 1 unchanged. Zooms 2/3/4 dimensions remain 32x28/48x42/64x56; all meta files unchanged. Corrected contour pixels: 40/63/88.
- Changed: Tools/Remove-SmallIconBackground.ps1 and Small1131 PNGs at zoom 2-4; project status/task/decision/handoff docs, UiReference.md, notes.json. Backups before each pass: .codex/backups/Small1131-20261112-193912, -193945, -194025 under the user's profile.
- Validation: saved-image alpha/color/dimension/meta checks passed; inspected all four icons enlarged against a dark background, including a second pass after visible grey contour artifacts were identified. Final preview no longer shows the white fringe. No Play/gameplay tests or live icon cache edits; Unity refreshed; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0.
- Command: powershell -NoProfile -File Tools/Remove-SmallIconBackground.ps1 -IconId 1131 -WhiteThreshold 160 -UseZoom1Silhouette. Only use the silhouette option when zoom 1 is the correct transparent reference. Next: user checks fresh-loaded icon art in-game; old in-memory/RMS or browser icon caches can still display older pixels.


## 2026-11-12 - Equipment upgrade controller navigation
- Scoped GameScr controller/keyboard fix to isPaintUpGrade (normal and gold upgrade): Enter/Circle invokes the existing left selection/action command before the optional centre info-view command; upgrade detail panes no longer capture directional keys; Up from the equipment slot row returns to the title/tab row.
- Original command handlers, upgrade eligibility/cost/chance, material movement, confirmation dialogs, networking and saves are unchanged. Pointer tooltip scrolling remains available. Other crafting panels and world controls unchanged. Offline input deviation from GameScr.updateKey/updateKeyBuyItemUI; uses the existing NsoGamepadInput Enter/directional translation.
- Changed: Assets/Scripts/Screens/GameScr.cs; status/task/decision/handoff docs, UiReference.md and wiki notes. No changes to the already-dirty NsoGamepadInput.cs.
- Controls: D-pad/left stick navigates; Up to the title row then Left/Right switches Upgrade/Bag; Down enters slots, Down from equipment row enters materials, Up returns; Circle selects the current action. With materials ready, title-row Circle invokes the existing Begin flow and confirmation. Cross remains Back/Close.
- Validation: Unity refreshed and compilation completed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play mode tests, physical-controller testing, item changes or actual upgrade attempts per user compile-only preference.
- Next: user verifies navigation and item/material selection with their controller, including normal/gold upgrade and confirmation/cancel. Compile commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Shared item-tab navigation and title return
- Fixed the upgrade title-return regression: a direction previously moved focus to title and then fell through to generic grid processing, immediately entering a child section again. Upgrade/chest navigation now resolves one transition, consumes the key, refreshes commands and returns.
- Shared pure policy TabGridNavigation.Move is used by bag, upgrade, chest, mount and bijuu equipment. Left before first/right after last returns title; Up from a top row returns its parent; bottom-row Down stays. Upgrade materials have the equipment row as their vertical parent, then title. Horizontal exits go directly to title. Equipment retains an explicit irregular Up/Down slot map and shares horizontal title exits.
- Info panes no longer swallow arrows for bag/equipment/mount/all bijuu pages. Bijuu attribute/skill pages retain their specialized navigation and clear detail focus at title. Mount/bijuu equipment Down can now reach slot 4 instead of wrapping to zero at slot 3.
- Reuse uses a shared navigation policy plus GameScr adapters because these panels are states of one existing screen, not separate screen classes. No scene/framework migration or new MonoBehaviour inheritance tree. Gameplay actions, inventory/server state and confirmations remain in existing handlers.
- Changed: Assets/Scripts/UI/Components/TabGridNavigation.cs (+ Unity meta), Assets/Scripts/Screens/GameScr.cs; project status/task/decision/handoff docs, docs/ARCHITECTURE.md, UiReference.md and wiki notes. Earlier duplicated bag/equipment/mount/bijuu-equipment navigation blocks replaced with the common adapter. No change to the dirty NsoGamepadInput.cs.
- Validation: source review and Unity refresh completed; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/controller tests, upgrade attempts or item/save mutations per user compile-only preference.
- Next: user checks bag, equipment, mount, bijuu equipment/attributes/skills, chest/bag and upgrade/bag. Enter cells, navigate to boundaries/title, switch tabs and reenter; verify material -> equipment -> title Up path and unchanged actions. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Share/Create opens world map
- Added Share/Create/Select button binding via Gamepad.selectButton: while in the world, the press edge invokes existing GameScr command 110002 -> doShowMap. Existing arena restriction retained; no new map implementation.
- Release held gameplay keys before opening the map and consume the button so holding cannot repeat or inject a key into the map screen. Menus/dialogs/chat retain their input ownership. New Inspector toggle _shareOpensMap defaults true; disabling restores the existing configurable _create key mapping.
- Changed: Assets/Scripts/Input/NsoGamepadInput.cs; status/task/decision/handoff docs, UiReference.md and wiki notes. Existing dirty controller mappings preserved.
- Validation: Unity refreshed and compiled: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play or physical-controller tests, per user compile-only preference.
- Next: user presses Share/Create in the world, verifies map opens once when held and Cross closes it; checks normal movement after closing. Config: NSO Gamepad Input > Share Opens Map. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Share/Create map handler correction
- Read-only Editor inspection identified the attached device as DualSenseGamepadHID. The small Share/Create button above the D-pad, left of the touchpad, maps to select (confirmed in the installed Input System HID layout).
- Root cause of the previous shortcut failure: NsoGamepadInput called GameScr.perform(110002,null), but perform handles only reconnect id 9999. Map action 110002 belongs to GameScr.actionPerform. Corrected the dispatch; button mapping, press-edge guard, world-only scope and key-release behavior unchanged.
- Changed: Assets/Scripts/Input/NsoGamepadInput.cs; status/task/decision/handoff docs, UiReference.md and wiki notes. No Play mode started, no simulated button presses or gameplay mutations. Unity compile/Console passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0.
- Next: user starts Play and presses the small Share/Create button above D-pad (not touchpad). Verify map opens; Cross closes. Validation commands remain unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json and unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Map toggle and analog cursor
- Share/Create now closes MapScr through its existing close action 1000; opening retains GameScr.actionPerform(110002) and arena restrictions. Dialog/menu ownership is respected. Held gameplay keys are released on either screen transition.
- Left stick moves the existing world-map cursor at render cadence, with fractional accumulation and deadzone rescaling. Inspector Map Cursor Speed defaults to 120 logical pixels/second; diagonal speed is normalized and long-frame displacement is capped. Analog input bypasses digital arrows on MapScr to prevent double movement.
- MapScr owns cursor bounds, camera target and nearest-map selection. Analog selection does not invoke legacy adminMove; travel remains the existing Go To command. Keyboard/pointer behavior unchanged. Offline input deviation from MapScr.updateKey.
- Changed: Assets/Scripts/Input/NsoGamepadInput.cs, Assets/Scripts/Screens/MapScr.cs, four root handoff/status/task/decision docs, UiReference.md and wiki notes. No scene, package or save format changes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play or physical-controller tests, per user preference.
- Next: user checks Share open/close, analog cursor in all directions and at bounds, Go To/Close, and world controls after closing. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Hide map travel button and add /next
- World map no longer creates the Go To left command; LayoutCommands positions only Close. Share/Create toggle, analog cursor, existing map pointer/keyboard behavior and unbound travel handler retained. No gameplay travel code removed.
- DevTerminal /next advances an active quest through PlayerTasks.NextTaskStep -> existing TaskNext (Char.taskNext), preserving save, client packets, removed-step policy and visible quest object refresh. It bypasses current step requirements only. No auto-accept or reward grant; final step asks the player to report to the NPC or use /skip. Missing/inactive/invalid/final quests return explicit replies without mutation. /help lists /next.
- Offline deviations requested by user; no duplicate quest system, protocol or save-schema changes.
- Changed: Scripts/Screens/MapScr.cs, Server/Net/DevTerminal.cs, Server/Net/PlayerTasks.cs; root status/task/decision/handoff docs, UiReference.md, ServerPort.md and wiki notes.
- Validation: Unity refresh/compile passed; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:06:36Z). No Play tests, terminal execution or save changes, per user compile-only preference.
- Next: user verifies map shows only Close; /help; /next on an active intermediate step, final step and before accepting a quest. Confirm progress persists after reload. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - MISS must not heal the player
- Report: HP rises over time without consuming items. Source trace found MobBrain.Hit uses -1 for MISS (Java Mob.attack(Char, Char)); client Char.doInjure subtracted that value unconditionally, adding 1 HP per missed attack. Other negative attack deltas could also increase local HP.
- Fix: Char.doInjure applies only positive HP/MP damage. Original signed values still drive MISS/floating text; remote-character authoritative cHpNew assignment and existing hit visuals/death guards retained. Food, potion, equipment recovery, server damage scaling and saves untouched. Offline client correction to the original Mob.attack display protocol.
- Read-only Editor baseline: Play was already stopped; natural regen=0, retained client HP=352/355, no effects, eff5BuffHp=0, autoUpHp=0, auto HP/food disabled. Server state unavailable outside Play. This confirms a code-level MISS arithmetic defect, not a runtime reproduction of every reported HP increase. Console baseline had zero current errors/warnings.
- Changed: Assets/Scripts/Gameplay/Entities/Char.cs; four root status/task/decision/handoff docs, UiReference.md and wiki notes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:12:29Z). Source diff check passed. No Play, synthetic damage execution, save changes or automated gameplay tests per user compile-only preference.
- Next: user takes damage without recovery effects/equipment, lets nearby monsters miss repeatedly, verifies MISS does not add HP; checks positive hits still subtract HP and legitimate food/potions still recover HP. If HP rises while away from monsters, collect the size/interval and active equipment/effects for a separate source investigation. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Align monster damage and remove false full heals
- Follow-up: MISS-only correction did not resolve reported full-HP recovery when hit. Source trace found raw NPC_ATTACK_ME damage differed from the 20%-scaled server deduction; subsequent HP sync restored the gap. NPC_ATTACK_PLAYER also carried pre-hit HP. Client ME_LOAD_SKILL/UPDATE_POTENTIAL handlers independently filled HP/MP on refresh although server SetAbility preserves current values. Runtime reproduction remains unverified (compile-only preference).
- MobBrain.Hit now scales positive HP damage once before applying/sending it, preserves MISS/zero fully absorbed damage, sends observers post-hit HP/MP and excludes the victim from the observer packet. Existing mana shield consumption, dodge, resistances, OnHit/death hooks retained. No new packet format. This supersedes the older intentional raw-display/scaled-damage behavior.
- Client skill/potential refreshes clamp current HP/MP to new maxima instead of healing. Dedicated HP/MP/recovery packets remain authoritative for actual recovery; food/potions/equipment and respawn retained. Earlier MISS guard remains.
- User requested stronger monsters: default mobDamagePercent raised 20 -> 100 (five times previous post-resistance HP damage before rounding); dodge defaults unchanged. Web remains configurable 0..500%. Existing saved combat rules retain their explicit settings. Read-only check found no combat-rules.json at the active Editor data folder, so this project's next run uses 100%. No save files modified.
- Offline deviations from Java Mob.attack(Char,Char)/display semantics and client stat-refresh healing are intentional corrections; documented in wiki deviations and CombatBalanceConfiguration.md.
- Changed: Assets/Server/Net/MobBrain.cs, Assets/Server/World/CombatRules.cs, Assets/Scripts/Networking/Protocol/Controller.cs, Assets/StreamingAssets/NsoWiki/app.js, CombatBalanceConfiguration.md, root docs, ServerPort.md, UiReference.md and wiki notes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:15:55Z). Source diff check passed. node --check app.js passed. No Play, gameplay tests, physical inputs or save mutation.
- Next: user starts Play, checks web combat damage=100, takes repeated nonlethal hits with no recovery sources, observes HP/floating damage through skill/stat refresh and a level-up; verifies MISS, shield, lethal hit/respawn, and legitimate item recovery. Existing saved combat rules in other installs need web adjustment to 100 if desired. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json; node --check Assets/StreamingAssets/NsoWiki/app.js.


## 2026-11-12 - Character list level, nearby auto pickup and ground yen
- Character selection previously called the phase-1 Level(exp) stub (always 1). It now uses GameWorld.GetLevel(saved Exp), matching gameplay initialization; stub removed. No EXP/save rewrite.
- Existing Char.autoPickItemMap was never called. Wired it into Char.update and extended its existing request path with offline nearby-all-loot defaults. NSO Game Control: Auto Pickup=true, radius=120 logical px (24..145, below server 150px limit), interval=0.35s (0.1..2). At most eight settled nearby item requests per interval; skips pickup-flight items and locally obvious full-bag cases. Legacy Do Not Pick takes priority; when global toggle is off, original yen/HP/MP/food selection flags remain usable. No automatic player movement. Server ownership, quest visibility and inventory checks remain authoritative.
- Mob.dropItem(YEN) now creates ground item 12 instead of crediting directly. Same drop probability, amount formula, dummy/overlevel/ordinary-loot gates and despawn/owner rules. Bundles split at ushort.MaxValue for the existing pickup packet. Yen consumes no bag slot, even when bag is full.
- Currency pickup saves the server balance without sending an extra ADD_YEN delta because ITEMMAP_MYPICK already credits the client. The packet carries actual credited yen (including cap truncation), not generic stack/display count. Prevents double credit and quantity truncation. Other reward AddYen callers retain notifications by default; sushi can also be picked with a full bag because it heals directly.
- Changed: Assets/Server/Net/Controller.cs, PlayerItems.cs, PlayerService.cs; Assets/Scripts/Gameplay/Entities/Char.cs, Diagnostics/NsoGameControl.cs; four root docs, ServerPort.md, UiReference.md and wiki notes. Nearby all-loot behavior is an offline deviation requested by user, reusing the original client pickup method/protocol. No scene/package/save-schema changes.
- Validation: source diff check passed; Unity compile/Console passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:36:31Z). No Play, item pickup, inventory/save mutation or gameplay tests per user compile-only preference.
- Next: user reopens character selection and compares level; kills eligible ordinary mobs and observes ground yen then a single pickup credit; compares yen client/server across reconnect; checks bag-full yen, item/quest pickup and toggles/radius. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Manual ground pickup correction
- User clarified ground loot must wait for explicit interaction, with no automatic suction/pickup. This supersedes the previous interpretation of their pickup report.
- Removed the newly introduced Char.update autoPickItemMap call, nearby-loot timer/logic and NsoGameControl auto-pickup settings. Restored the pre-change unused legacy autoPickItemMap method. Removed the normal GameScr.update HutVP suction invocation so even an old HutVatPham flag cannot automatically collect loot in ordinary play. Existing deliberate item-focus/interact pickup and separately invoked legacy auto-play/task routines remain intact.
- Retained the requested real level on character selection, ground yen and single-credit currency pickup/full-bag support. Items and yen stay on the ground until explicit pickup (or normal despawn). No server loot probability, amount, ownership or range changes.
- Changed: Assets/Scripts/Gameplay/Entities/Char.cs, Assets/Scripts/Diagnostics/NsoGameControl.cs, Assets/Scripts/Screens/GameScr.cs; four root docs, UiReference.md, ServerPort.md and wiki notes. No save changes. Offline manual-pickup rule documented; no architecture change.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:39:53Z). Source diff check passed. No Play/gameplay tests per user preference.
- Next: user kills an eligible mob, waits near loot to confirm it remains, focuses an item/yen and explicitly interacts to pick it up; confirms yen credited once and character-list level unchanged. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Thien bien lenh automation and explicit auto-loot exception
- User reported item 572 not working, then explicitly clarified it SHOULD auto-pick while enabled. Ordinary play remains manual; this supersedes the manual-only rule solely for enabled Thien bien lenh.
- Source gaps: client command 119 set isUseitemAuto but did not start persistent GameScr.auto=1; server ignored movement command 119; local movement omitted X distances, skipped a reusable index, had an inverted 2s cooldown expression and could select multiple mobs in one scan. Focus cleanup could erase the newly selected mob.
- Client enable now starts existing auto-fire and clears movement lock; disable stops it and unlocks. Focus resolves before item movement. Nearest live non-rank-3 mob is selected with range checks, no distance-array allocations, one move/2s; no movement while dialogs/panels/chat are open. Existing attacks, skill cooldowns, mana and combat packet path retained. Manual input retains the legacy auto-pause behavior; toggle off/on to resume.
- Server command 119 now validates enabled mode, unexpired owned item, living character/mob, captured range origin and move cooldown; synchronizes position through ResetPoint. Item existence/expiry checked at enable, movement and periodic tick; no item consumed. Range changes recapture origin consistently with client. No unrestricted teleport endpoint.
- Enabled item invokes the existing GameScr.autoPickItem entry point: settled loot within 120px, every 350ms, at most eight requests; skips obvious full-bag noncurrency cases. Public tunables ItemAutoPickupRadius/IntervalMs, server AutoMoveIntervalMs. Normal pickup ownership/quest/range/bag rules retained. Toggling off or auto-pause stops automatic requests. Existing always-on HutVP suction remains disconnected. Settings menu now states auto-pick while enabled instead of offering ineffective stored-only pickup modes.
- Changed: Assets/Server/Net/PlayerItemUse.cs, PlayerActions.cs, Controller.cs; Assets/Scripts/Networking/Protocol/Controller.cs, Screens/GameScr.cs, Gameplay/Entities/Char.cs; root docs, docs/ARCHITECTURE.md, ServerPort.md, UiReference.md and wiki notes. Offline completion of Char.useItemTypeOrder THIEN_BIEN_LENH, Service.turnOnAuto/turnOffAuto and MapService AUTO_ATTACK_MOVE. No save-schema/scene/package change.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:46:57Z); source diff check passed; no Play, automation execution, item pickup or save mutation per user compile-only preference.
- Next: user uses item -> On in a monster map, closes menus, verifies targeting/move/attack/nearby item+yen pickup; tests 240/360/whole-map range, Off, item expiry/removal, manual movement pause and menu pause. Ordinary play must not auto-pick. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - School-specific elemental shops
- User requested school NPCs sell only items matching their school's element. Shared PlayerItems.SchoolStore filters map 1 -> fire/system 1, map 72 -> ice/system 2, map 27 -> wind/system 3, regardless of visitor class. Other maps retain original catalogs. Neutral supplies (potions, food, general goods) remain available.
- StoreItem.Sys determines equipment element. Neutral-sys skill books use new PlayerSkills.BookClass, reusing the existing Char.learnSkill BookToTemplate mapping and skill-template class; classes 1/2 fire, 3/4 ice, 5/6 wind. Original store tables and item options/prices untouched.
- One deterministic filter/order is shared by requestItem, requestItemInfo and buyItem so UI indices and purchased items match and off-element entries are absent from the server's purchase list. No client-only visibility patch or duplicate item data. Config switch: PlayerItems.RestrictSchoolStores=true.
- Offline deviation from shared Char.requestItem/Store.show/Store.buy catalogs requested by user. No changes to inventory/equipped items, loot, class selection or saves.
- Changed: Assets/Server/Net/PlayerItems.cs, PlayerSkills.cs; four root docs, ServerPort.md, UiReference.md and wiki notes. No architecture/scene/package changes.
- Validation: source review completed; Unity compilation/Console passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:53:54Z); diff check passed. No Play, purchases or save mutation per user compile-only preference.
- Next: user visits each school as any class, checks weapon/armor/jewelry/book lists and first/last item details/purchase; confirms neutral supplies remain, village catalogs unchanged. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Explicit Sort groups bag and box items
- User clarified no automatic grouping: keep normal layout until pressing Sort, then place same-type items together. Added stable ordering by item type -> template id -> lock state -> original position; empty slots packed at end. Distinct equipment instances retain their order within identical keys and are not merged.
- New pure InventoryGrouping.Group<T> is shared by server PlayerItem arrays and client Item arrays, avoiding divergent ordering rules. Existing Java Char.bagSort/boxSort compatible non-expiring stack merging retained; mirrored overflow guard prevents wrapping int quantities. Indices refreshed after grouping. No grouping added to acquisition, movement, load or equip.
- Added missing server BOX_SORT (-106) handler with BoxOpen validation; bag and box use one server SortContainer implementation and existing acknowledgement packets. Client applies the shared policy only on the matching acknowledgements. No packet/save-schema changes, no new parallel inventory system.
- Changed: Assets/Server/Store/InventoryGrouping.cs (+ Unity meta), Assets/Server/Net/PlayerItems.cs, Controller.cs, Assets/Scripts/Gameplay/Entities/Char.cs; root docs, docs/ARCHITECTURE.md, ServerPort.md, UiReference.md and wiki notes. Offline extension of Java bagSort/boxSort requested by user.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T14:46:37Z); diff check passed and Unity generated InventoryGrouping.cs.meta. No Play, inventory sorting execution or save mutation per user compile-only preference.
- Next: user mixes equipment/consumables/materials/quest items in bag and box, presses Sort in each; checks grouping, same-id lock separation, expiring item preservation, quantities, bag-box moves and reload persistence. Ordinary acquisition/moves should not reorder slots automatically. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Actual supply stacks and quantity splitting
- User corrected grouping to mean one cell with a quantity (like HP/MP), confirmed consumables/materials only, equipment separate; then requested a split-quantity action. Sorting now merges eligible non-expiring same-template/same-lock items into actual stacks, then retains shared type ordering. No automatic grouping added on acquisition/load/movement.
- Shared InventoryGrouping.CanStack covers original stackable supplies plus food (18), star stones (22), crystals (26), insurance (28); body/mount equipment (0..15,29..38) excluded. Unique nonstackable quest artifacts/permanent utility items remain separate. Expiring instances remain separate; stack merge cap is 32767 to preserve signed-short box protocol. Equipment stats/options never merged.
- QuantityDisplay now preserves eligible stack amounts in bag/box/add/pickup serialization. Existing item quantity badges render the stack count. Food consumes one serving instead of the whole newly grouped pile.
- Existing bag Split command relabeled Split quantity; numeric dialog parsing hardened and shared with chest split. Chest stacked-item actions now offer Withdraw/Deposit, Split quantity, Sort. Requests use existing NOT_MAP -85 inputNumSplit payload (index, int amount), optional bool box; server validates eligibility, amount and a free slot, deep-copies fields/options, updates both slots and saves. Reply NOT_MAP -85 includes box flag, from/to byte indices, template short, left/taken short counts. Client clones metadata and applies authoritative counts after acknowledgement; no potion-counter or receive-item side effects. Bag/chest accept splitting N units into one new stack; one free slot is required.
- Crystal/insurance crafting retains the original one-instance-per-selected-slot protocol. Client prompts to split a grouped material first; server crystal refining rejects grouped input before charging/consuming, and equipment upgrade already rejects quantity !=1. This prevents consuming a full pile for one crystal contribution. No automatic bulk crafting introduced.
- Changed: InventoryGrouping.cs, PlayerItems.cs, PlayerService.cs, PlayerStorage.cs, PlayerUpgrade.cs, server Controller.cs; Char.cs, GameScr.cs, GameCanvas.cs, client Service.cs/Controller.cs; four root docs, architecture/UI/server docs and wiki notes. Offline extensions of Java Char.bagSort/boxSort and existing inputNumSplit. Existing packet namespace extended; no save-schema/scene/package changes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T14:58:33Z); diff check passed. Read-only reflection confirmed CanStack and SplitStack are in the loaded assembly; no Play, sorting/splitting/crafting execution or save mutation per user compile-only preference.
- Next: user presses Sort on duplicate food/crystals in bag/box; checks one-cell quantities, locked/expiring separation and equipment unchanged. Split e.g. 3 from 10 -> 7+3; reject 0/10/bad input/full container; split one crystal then upgrade/refine, cancel without loss; eat once from a food stack, transfer/sell/reload and compare totals. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.
