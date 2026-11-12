# Input System migration

Unity 6000.6.8f1, Input System 1.19.0. Active Input Handling is saved as **Input System Package (New)**. Restart Unity before treating the old backend as disabled; settings on disk and compiled backend defines can differ until restart.

## Ownership and controls
- `Assets/Settings/NsoControls.inputactions` contains the `Client` action map. The shared `Assets/Prefabs/NSO Game.prefab` assigns this asset to `NsoInputSystemBridge` on Main Camera. Runtime clones avoid changing the authoring asset.
- Action names for keys match Input System keyboard control names. The bridge translates to existing MyKeyMap codes; binding edits may change paths but retain names. Click, Position and Scroll describe mouse controls.
- Main.OnGUI only performs existing session/render work; it no longer acquires keyboard/mouse input repeatedly per repaint/layout.
- Existing key mappings and gamepad Inspector mappings remain intact. Space/up jump; arrows and client letter/digit bindings; F1/F2/F3 soft keys; skill shortcuts; Shift+2 and Shift+minus special codes.
- F6 toggles settings, F9/F10 adjust tick rate within 10–60, F11/Alt+Enter toggle fullscreen. Diagnostics receive keys before gameplay and keep their existing pointer handlers.
- New keyboard text/IME callbacks feed chat and terminal; printable keys are suppressed as gameplay while chat is showing. Escape closes chat, Enter follows existing command path. Terminal composition comes from IME events.
- Mouse conversion preserves zoom and screen-Y inversion. Wheel input honors InputSystem.settings.scrollDeltaBehavior: normalized notches pass through; only native Windows mode divides by 120 units per detent (fixed 2026-11-12). Verify physical wheel/trackpad feel against existing lists.
- Shared reference counts coordinate identical keyboard/gamepad client codes. Releases from keyboard are deferred until a logic tick can observe short taps. Focus loss, disable and removal cancel keyboard-held input; gamepad retains its own cancellation logic.
- EventSystem now uses InputSystemUIInputModule with explicit default UI actions. No existing Canvas/HUD was redesigned. Game UI still uses GameCanvas.
- GPS `Input.location` remains: location services have no direct Input System replacement and are not legacy gameplay polling.

## Verified before backend restart
All edited scripts compiled successfully. Automated multi-frame Play Mode regression passed:
1. Bridge enabled and canvas initialized.
2. Zero StandaloneInputModule components, one InputSystemUIInputModule.
3. F10 delivered once per press.
4. F6 opens and closes settings.
5. Unicode `ếx` committed exactly once.
6. Combined Backspace action/text event deleted only one character.
7. Pointer press held and release cleared state.
8. Shared held-key reference count retained a second source, then released on final source.

The editor exited Play Mode and the temporary test runner was removed. The input scan found no remaining legacy keyboard/mouse/text polling in project scripts; only GPS location and an outdated explanatory Scroll comment remained at scan time.

## Required remaining checks
- Restart Unity, confirm Active Input Handling is Input System Package (New), compile, and rerun gameplay regression without legacy backend enabled.
- Real Windows Unikey/IME composition, commit/cancel and Backspace repeat/replacement ordering. Simulated Unicode events do not prove real OS input correctness.
- Full movement/combat/skill/menu regression with keyboard and physical gamepad, source overlap, focus/hot-plug, repeated Play with Domain Reload enabled/disabled.
- Fullscreen, mouse wheel/trackpad and pointer alignment over zoom/window changes; diagnostic numeric editing and text dragging.
- Windows development build smoke test. None of these hardware/build checks was claimed as completed.

## Rollback
Restore Both under Player > Other Settings > Active Input Handling and restart Unity, then revert the input bridge/action asset and affected Main, pointer/terminal/diagnostic/gamepad source edits and game prefab. Restoring Both alone does not restore removed legacy acquisition. Do not change save/server data, combat balance, renderer or Inspector gameplay configuration.

Reference: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Migration.html
