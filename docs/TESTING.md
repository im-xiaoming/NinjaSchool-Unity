# Testing (active project `C:\My project`)

This file supersedes `C:\NSO_OFFLINE\docs\TESTING.md` for this project. Two sections of the old file are still valid for reference:
- capturing new Java reference traffic (section 8);
- the `-autotest` player driver (section 10).

## 1. Server tests (`Tools/ServerTests`, no Unity needed)
The test project compiles `Assets/Server/**/*.cs` (except `ServerBootstrap.cs`) and connects through `GameServer.Connect()`.
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj"
```
| Arguments | Checks | Last result (2026-11-11) |
|---|---|---|
| (none) | login, persistence, static payloads vs captured files, mob templates, inventory JSON, zone cap | 18 pass, 2 FAIL (expected: "wrong password rejected", "invalid username rejected"; the offline build always signs in to the local account) |
| `combatcheck` | attack, damage, miss, death, exp | 6 pass |
| `questcheck` | quests 0–3 played through NPC menus (talk, quiz, wooden sword, 10 dummy kills, buy and eat rice balls) | 16 pass |
| `progresscheck` | quest item drop and pickup, school admission, skill book, skill and potential points, school weapon | 12 pass |
| `entercheck C:/NSO_OFFLINE/captures/enter_map22.txt 22` | enter world, byte-compared with Java | ENTER WORLD MATCHES JAVA |
| `walkcheck C:/NSO_OFFLINE/captures/walk_A.txt 22 10 150 0` | movement and map change vs Java | MATCHES |
| `walkcheck .../walk_B.txt 22 420 216 0` | zone list and zone change | 2 mismatches, expected: the zone list has 5 zones (11 bytes) instead of Java's 161 bytes (the 5-zone cap is a user decision) |
| `walkcheck .../walk_C.txt 22 10 150 3` | movement with quest 3 | MATCHES |
| `walkcheck .../walk_D.txt 14 300 200 0` | monster attack and death | MATCHES (can be flaky because of random misses; re-run) |
| `wiki` | builds `NsoWiki/db.json`; every collection non-empty and every document has an id | 17 collections ok |
| `wiki serve 8790` | as `wiki`, then serves the website on that port until killed | page and API answered |

Set `NSO_TRACE=1` to print every command a test receives (handy when a quest test hangs).

## 2. Unity compile check (Editor open)
Scripts used by agents are kept in the scratchpad. Recreate them as needed:
- `refresh.cs`: `UnityEditor.AssetDatabase.Refresh(); return "refreshed";`
- `play.cs`: `UnityEditor.EditorApplication.isPlaying = true; return "play";`
- `stop.cs`: the same with `false`.
```bash
unity command script eval --file refresh.cs
```
```bash
unity command console status
```
Wait about 20 s after the refresh. In the `groundTruth` part of the status, require `compilationFailed:false` and `consoleErrors:0`. Compile errors themselves are listed in `%LOCALAPPDATA%\Unity\Editor\Editor.log` (grep for `error CS`).

## 3. Play-mode checks (Editor open, no batch mode)
1. Start Play with `play.cs`.
2. Poll state with a small eval script. Example: `return GameCanvas.currentScreen.GetType().Name + " " + Sound.SoundBGLoop.isPlaying;`
3. Stop with `stop.cs`.
- The title screen (`SelectServerScr`) appears about 6–8 s after Play starts.
- While playing, the dev wiki answers at `http://127.0.0.1:8787/`. After Stop it is closed (connection refused).
- `Tools/nso_world.sh` drives Play mode to the world (title → Continue → character select → world).
- `Tools/nso_ab.sh` with `Tools/nso_compare.ps1` compares screenshots of the game layer turned on and off.

## 4. Real-client build
`C:\NSO_OFFLINE\tools\build_and_run.ps1` still points at the OLD project (`C:\NSO_OFFLINE\NinjaOffline`, scene `testNj.unity`). Do not use it for this project until it is adapted (see TASKS.md).

## 5. Known limits
- Quest, shop, drop and skill behaviour is verified only by `questcheck` and `progresscheck` and by the Java source, not by Java captures. Only a level-1 character without items was ever recorded from Java.
- None of the quest, shop or combat flows has been played by hand in the real client yet.

## Client skill hit-frame regression (2026-11-11)
After entering a map in Editor Play mode:
```powershell
& 'C:/Users/PC/AppData/Local/Unity/bin/unity.exe' command script eval --file Tools/verify_skill_hit_frame.cs --project-path 'C:/My project' --format json
```
Expected: 8/8. Uses synthetic remote characters, no packets/save changes. Covers original hit frame, skipping the frame, skipping to animation end, no early hit, no duplicate hit, new cast, dead character and projectile-impact duplicate suppression.
Live reproduction: equipped wooden sword, skill 0, in-range dummy, invoke normal client attack. Before fix: render index skipped hit frame 4 and no damage was sent. After fix: five normal client attacks reduced HP 500000 -> 0 and quest 2 count 0 -> 1. Verified by runtime state, not screenshots or a complete manual play-through. Unity compilation and current Console were clean; server questcheck remained 16/16.

## Loot policy regression (2026-11-11)
`dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- lootcheck`: 9/9. Deterministic policy boundaries cover dummy template vs spawn identity, gaps 9/10/11, direction and configuration. Blocked-path calls run before quest/currency/drop dependencies, including sushi. Nearby questcheck 16/16 and progresscheck 12/12 still pass. Global gate eligibility does not bypass the original ordinary-loot seven-level restriction. Unity compile/Console checked; no manual loot play-through claimed.

## Effect presentation regression (2026-11-11)
In Play mode, in a loaded map:
```powershell
& 'C:/Users/PC/AppData/Local/Unity/bin/unity.exe' command script eval --file Tools/verify_effect_presentation.cs --project-path 'C:/My project' --format json
```
Expected 17/17 with smooth effects enabled and menu opening duration >0. Includes HP retargeting/timing/rate independence, fractional samples, frozen/recycled geometry, toggle, recent menu reopening and input guard. Test temporarily changes and restores presentation config/shared menu geometry; sends no packets or save edits. Nearby verify_skill_hit_frame: 8/8.
Live samples at ~120 renders/s: floating-number presentation 72 positions over 16 logical changes; F1 menu 38 over 9. These are diagnostic samples, not a claim of new sprite poses, complete real-combat verification or release performance. Unity final compile/current Console clean.

## Workbench checks (2026-11-11)
```powershell
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- webcheck
node Tools/verify_workbench_ui.cjs
node --check Assets/StreamingAssets/NsoWiki/app.js
```
webcheck 27/27: real HTTP plus isolated save/world; schema/bounds/policy/persistence/restart, failed writes, revisions, token/Host/Origin, fragmented/oversized requests, compatibility and allowlist. Real user settings were not changed. Pure UI 18/18: all 17 collection/detail generators, routes, escaping, filters, corrupt bookmarks, live/settings/inspector/history HTML. Not browser DOM/visual/click tests.
After integration: lootcheck 9/9, questcheck 16/16, progresscheck 12/12, wiki 17 collections; default retains 18 PASS / 2 documented login FAIL. Unity current compile/Console clean. Live Play API showed one player/default policy and correct dummy explanation. Browser/native automation unavailable; desktop/mobile visual checks pending. Release readiness also requires saved-policy reload and confirming no debug web server in release.

## Recovery regression (2026-11-11)
`dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- regencheck`: 9/9. Before fix the same test produced 7 FAIL / 2 PASS. Tests call real PlayerEffects.UpdateHalfSecond with forced five-second boundaries and isolated packet-discarding sessions; no sleeps/read timeouts on the game pipe. Checks no source-free recovery, valid food/potion/equipment amounts/intervals, expiry, max caps and death. questcheck 16/16 and progresscheck 12/12 still pass; Unity final compile/current Console clean. Real-player HP/inventory is not forced for validation.

## Movement release regression (2026-11-11)
In Play mode after entering a map: `unity command script eval --file Tools/verify_movement_release.cs --project-path "C:/My project" --format json`. Expected 18/18 on PC. Exercises actual GameCanvas.keyReleased with isolated Char singleton/input snapshots and restored Inspector flag; no network/save mutation. Pointer-cleared stop helper is covered, not physical touch UI. Original baseline at speed 8: release velocity 8, 9 ticks at 24 Hz, 36 extra pixels. After fix: velocity 0, 0 ticks and 0 pixels. Final Unity compilation/current Console clean. Physical device feel and Windows build remain unverified.

## Broad world presentation (2026-11-12)
Run `unity command script eval --file Tools/verify_world_presentation.cs --project-path "C:/My project" --format json` in Play after entering a map. 94 checks passed: ten object types, five distinct fractional samples during one-pixel motion, restoration, teleport snapping, camera sign, toggle, scroll/recycle/geometry and render-cap floor. Nearby checks: effect 17/17, movement release 18/18, skill-hit 8/8. These are isolated runtime checks, not all-effect visual certification.
Run Tools/measure_presentation_rate.cs the same way; after about six seconds read %TEMP%/nso-presentation-rate.txt. It samples actual Main paint/logic counters for two seconds per cap, with warm-up, then restores the old cap. Current village: cap 60 -> 60.0 paints/s, cap 120 -> 120.0 paints/s, both 24.0 ticks/s. No frame-time percentile/combat stress or Windows-build claim. World and bag screenshots were inspected and current Unity Console was clean. Physical scroll and all original sprite pose timings remain unverified.

## Notification ticker (2026-11-12)
Per user instruction, final validation is Unity compilation/Console only (compilationFailed=false, consoleErrors=0); user handles runtime debugging. Tools/verify_notice_presentation.cs is an optional isolated probe, not run on the final revision. An earlier version passed 24 checks before pre-tick/paused-state coverage was added.

## Global balance dashboard validation (2026-11-12)
Only compile/Console and JavaScript syntax checks, per user request. No dashboard runtime mutations, browser/Play/save/restart tests or Windows build. Standalone dotnet currently lacks shared BasicSkillGrowth source/type dependencies; no workaround retained. User checks global player coefficients on old/new characters without compounding, global/template equipment rules on old/new items/shop info without rewriting raw saves, and monster-template reset/spawn/recovery/restart. Check HP/death preservation and same-map client cache refresh. Final Unity state is in HANDOFF.

## Character progression refactor validation (2026-11-12)
Compile/Console and node --check only, per user instruction. No actual preview request, browser, save/restart or Play tests were run. Manual matrix and equations are in CHARACTER_PROGRESSION.md. Required user checks: default parity, native classed allocation, version-1 configuration reload, inherited/shared/specific reset, independent native growth/base inputs, negative/fractional growth clamping, comparison ranges, stale preview rejection, no save mutation during previews and no compounding during gameplay recalculation.


## Guided RL contract checks (2026-11-13)
`uv run --project Tools/Learning --locked python Tools/Learning/check_contracts.py` passed on CUDA: teacher cloning; actual C#/Python masked MLP parity with nonzero inputs; empty-mask rejection; PPO excludes demonstrations; saved checkpoint reload and another PPO update; regression-gate rejection; partial JSONL recovery. This is synthetic data only and starts no game server, Play mode or real training session. `dotnet build Tools/Learning/RlHost/RlHost.csproj -c Release` passes with the pre-existing unused zoomLevel warning. The user's long isolated gameplay evaluation remains opt-in; no quests or 13-bot frame-time acceptance was run for this RL change. See AI_LEARNING.md.


## Bot collision manual acceptance (2026-11-13)
Current validation is compile-only at the user's request. In Play, observe a bot approaching wood/rock from both directions, a low jumpable obstacle, an unjumpable wall, a low ceiling, a one-way platform and a ledge. Expect a visible grounded jump or stop, no arbitrary vertical lift, no wall/ceiling crossing and landing/falling. Interrupt movement during a jump and verify gravity continues. Check death/respawn, correction, map/zone switches and loot/NPC/waypoint approaches for stale input or new stalls. Compare unchanged human walking and ensure combat/cooldowns/effects still run at normal speed. Special mount/flight and full route solving are not certified. Do not execute this checklist automatically without changing the user's compile-only preference.
