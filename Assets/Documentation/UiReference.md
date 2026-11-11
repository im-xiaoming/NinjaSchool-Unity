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
| Chơi | Tự kết nối lại (`autoReconnect`) | `NsoGameControl.autoReconnect`. |
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
