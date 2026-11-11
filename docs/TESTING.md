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
