# HANDOFF.md

Last updated 2026-11-11. Read `AGENTS.md`, then `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, `docs/` and `README.md`.
Everything below was verified as stated. Anything marked NOT tested was not.
The handoff history before this project is in `C:\NSO_OFFLINE\HANDOFF.md`. It is useful background, but its status tables are stale.

## 1. State in one paragraph
- **What the game is:** an offline NSO game in Unity 6 (`C:\My project`). The Java server has been rewritten in C# (`Assets/Server`) and runs inside the game, connected to the client through an in-memory pipe.
- **What works in tests:** the beginner gameplay loop, verified by the automated server tests:
  - combat, skills and their effects;
  - items, shops, drops and regeneration;
  - quests 0–42 with NPC menus;
  - saving progress.
- **What is NOT verified:** nobody has played these flows by hand in the real client yet. This is the most important next step.
- **New this session:**
  - a debug-only lookup website and JSON API (the "dev wiki") that starts with the game on its own thread;
  - the menu music now plays once;
  - `README.md` and the handoff documents.

## 2. Completed in this project (latest sessions)
1. **Gameplay port from Java** (per-file details in `Assets/Documentation/ServerPort.md`):
   - combat (`PlayerCombat`), skills (`PlayerSkills`), effects and regeneration (`PlayerEffects`);
   - items, shops and drops (`PlayerItems`);
   - quests and NPC menus (`PlayerTasks`);
   - message builders (`PlayerService`), static data (`World/GameData.cs`), progress save (`Store/PlayerProgress.cs`).
2. **Dev wiki** (`Assets/Server/DevWiki`, `Assets/StreamingAssets/NsoWiki`):
   - website with search, filters and record details;
   - API at `/api`;
   - export to `NsoWiki/db.json`;
   - debug only and on its own thread (see `docs/ARCHITECTURE.md`).
3. **Menu music** plays once per visit to the menu (`Assets/Scripts/Audio/MenuAudio.cs`, `menuMusicLoop`).
4. **`README.md`** is written (it was empty).
5. **`walkcheck` repaired** for the in-memory pipe (`Tools/ServerTests/Program.cs`, `TestClient.HasMore`).
6. **Handoff set:** `AGENTS.md`, `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, this file, `docs/ARCHITECTURE.md`, `docs/TESTING.md`. A pointer line was added at the top of the old `C:\NSO_OFFLINE` documents.

## 3. Files
### Created
- **Server:**
  - `Assets/Server/World/GameData.cs`
  - `Assets/Server/Store/PlayerProgress.cs`
  - `Assets/Server/Net/PlayerService.cs`, `PlayerItems.cs`, `PlayerTasks.cs`, `PlayerSkills.cs`, `PlayerEffects.cs`
  - `Assets/Server/DevWiki/WikiData.cs`, `WikiApi.cs`, `WikiServer.cs`
- **Wiki assets:** `Assets/StreamingAssets/NsoWiki/index.html`, `notes.json`
- **Generated output:** `NsoWiki/db.json`, rewritten on every Play in the Editor and by the `wiki` test.
- **Documents:** `AGENTS.md`, `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, `HANDOFF.md`, `docs/ARCHITECTURE.md`, `docs/TESTING.md`; `README.md` content.
- Unity created `.meta` files for the new assets.
### Modified
- **Server:**
  - `Assets/Server/World/World.cs`: partial class, skills, exp table, item options, mob respawn and effect state, items on the ground.
  - `Assets/Server/World/TileGrid.cs`: added `CollisionY`.
  - `Assets/Server/Net/PlayerActions.cs`, `PlayerCombat.cs`, `EnterWorld.cs`, `Controller.cs`.
  - `Assets/Server/GameServer.cs`: the tick.
  - `Assets/Server/ServerBootstrap.cs`: `StartWiki`, `WikiUrl`, `TakeWikiOpenRequest`, wiki shutdown.
- **Client and Editor:**
  - `Assets/Scripts/Diagnostics/NsoServerControl.cs`: wiki settings, `wikiUrl`, opening the browser on the main thread.
  - `Assets/Editor/NsoControlEditors.cs`: "Open dev wiki in browser" button.
  - `Assets/Scripts/Audio/MenuAudio.cs`: plays once.
- **Tests:** `Tools/ServerTests/Program.cs`, which added the `combatcheck` updates, `questcheck`, `progresscheck`, `wiki [serve port]` and the `HasMore` fix.
- **Documents:** `Assets/Documentation/ServerPort.md`.
- **Old project documents:** `C:\NSO_OFFLINE\AGENTS.md`, `PROJECT_STATUS.md`, `HANDOFF.md`. Only a pointer line was added at the top of each.
### Deleted
None.

## 4. Implementation status
See the table in `PROJECT_STATUS.md`. For per-item status, the dev wiki is the most detailed source:
- `features` (done / partial / todo);
- `quests` (which steps are `skip`);
- `npcs` (menu ported or not);
- `skills` (ported types).

## 5. Tests performed (2026-11-11) and results
- **Unity compile:** `compilationFailed:false`, `consoleErrors:0`, after every change, including the last one.
- **Server suite (default):** 18 PASS, 2 FAIL.
  - The failures are "wrong password rejected" and "invalid username rejected". They are expected, because the offline build always signs in to the local account.
- **Other server checks:**
  - `combatcheck`: 6/6.
  - `questcheck`: 16/16.
  - `progresscheck`: 12/12.
  - `entercheck enter_map22`: matches Java.
  - `walkcheck`: walk_A, walk_C and walk_D match Java. walk_B has 2 mismatches, both the zone list (11 bytes instead of 161) caused by the 5-zone cap (user decision).
- **Wiki checks:**
  - `wiki`: 17 collections, each non-empty, every document has an id.
  - In Play mode: the URL is logged from the wiki thread; `/` and `/api/search?q=tajima` return 200; the server stops when Play stops (connection refused afterwards).
  - In the API through curl: `/api`, `/api/search`, `/api/quests/2`, filters, `fields`, `limit` and 404 errors.
  - In the browser: the overview, search ("kiem go"), quest details with links.
- **Menu music in Play mode:** on `SelectServerScr` the source plays with `loop=False`, then stops after 3.7 s and stays stopped.
- **NOT tested:**
  - playing quests, shops, combat and drops by hand in the real client;
  - a Windows build of this project;
  - the wiki in a development player build (only the Editor was checked);
  - the "open in browser" option (`_wikiOpenBrowser`, off by default).

## 6. Known bugs and open issues
- The new gameplay has never been played in the real client. Expect protocol issues in menus, shop windows, item info and effect icons. Ask the user for screenshots.
- The walk_B zone-list mismatch is expected (the 5-zone cap).
- `C:\NSO_OFFLINE\tools\build_and_run.ps1` still builds the OLD project (`C:\NSO_OFFLINE\NinjaOffline`, `testNj.unity`). It has not been adapted to this project.
- Skill types buff, NPC and live (2/3/4) can be selected but do nothing.
- Luong is always 0.
- Build warnings already present: the unused `bad` local in `Tools/ServerTests/Program.cs` and the unused `Controller.zoomLevel` field.
- Dead client code is still present (auto-play `Functions/Auto`, `ReLogin.cs`), kept by the user's decision.
- A Google sign-in pop-up appeared on the user's machine once during a session. It was not caused by the agent's commands (dotnet and the unity CLI only). Its source was not identified.

## 7. Dependencies
- Unity 6000.6.8f1 with the Editor open on `C:\My project`. Agents drive it with the `unity` CLI (`unity command script eval`, `console status`).
- .NET SDK 8, for `Tools/ServerTests`.
- Python 3, used only for ad-hoc editing scripts.
- No packages to install, no online services and no API keys.
- The dev wiki uses only `System.Net.Sockets` and listens on 127.0.0.1.

## 8. Exact next steps
1. **Play by hand in the Editor:**
   - New Game, create a character, enter Lang Tone.
   - Talk to the six villagers (quest 0), answer the quiz at Tabemono (quest 1), get the wooden sword from Tajima, equip it, beat the training dummies (quest 2), buy and eat rice balls (quest 3).
   - Check the shop windows (Kiriko, Tabemono, Goosho), item details, drops and pickup.
   - Go to school (quest 8: three schools, admission) and learn a skill.
   - Fix any protocol differences, comparing with the Java `Service`/`Char` code, and add a test for each fix.
2. **Port buff skills** (`Char.useSkillBuff`), then the most commonly skipped quest mechanics. The dev wiki lists them (`/api/quests?q=status:partial`).
3. **Adapt the build script** to `C:\My project` (`Assets/Scenes/NinjaGame.unity`), build, and run a Windows player offline.
4. **Keep the documentation current:**
   - `Assets/StreamingAssets/NsoWiki/notes.json` (features, constraints, commands, config);
   - `Assets/Documentation/ServerPort.md`;
   - these handoff files.

## 9. Important commands
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj"
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- questcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- walkcheck C:/NSO_OFFLINE/captures/walk_A.txt 22 10 150 0
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- wiki
```
```bash
curl -s "http://127.0.0.1:8787/api/search?q=tajima"
```
For the Unity compile check and Play-mode checks (refresh, console status, play/stop scripts), see `docs/TESTING.md` sections 2 and 3.

## Onboarding checkpoint (2026-11-11, commit 3e740918)
- Read the active handoff and representative source; `C:/NSO/docs` does not exist. No gameplay code, settings, scene, package or original/reference files changed.
- Added `docs/UnityProjectContext.md`; appended audit checkpoints to PROJECT_STATUS, TASKS, DECISIONS and this file. No feature/config/protocol changes requiring wiki notes or ServerPort updates.
- Current-source server build succeeded (0 errors, the 2 known unused-variable/field warnings). Isolated outputs: `%TEMP%/nso-onboarding-20261111/bin/ServerTests.dll`. Run `dotnet <that-path> combatcheck` (6/6), `questcheck` (16/16), `progresscheck` (12/12), or no argument (18 PASS / 2 documented login FAIL).
- Live Unity Console groundTruth: compilationFailed=false, compiling=false, consoleErrors=0. No refresh or Play mode was initiated; real-client gameplay and Windows build remain unverified.
- Initial dirty file NsoWiki/db.json was preserved; only generatedAt differs from HEAD. Existing tracked Tools/ServerTests bin/obj files were not rebuilt in place.
- Risks/dependencies: menu-music reference text and old overlay-plan status are stale; both build scenes remain enabled; progress reaches disk on disconnect; installed Unity CLI requires its full path in this shell. See the context document for evidence and exact Console command.
- Next step: wait for the user to assign a task. Do not redo completed ports or alter architectural decisions.

## Quest 2: dummy attack bug fixed (2026-11-11)
- Report: quest 2, equipped wooden sword, cannot attack training dummies in the real client.
- Confirmed cause: `Char.paintCharWithSkill` advances indexSkill on every render while `updateSkillPaint` dispatches only at exact equality on a logic tick. Runtime trace skipped index 4 (hit frame) from 1 to 6, then cleanup erased the cast without sending cmd 60. Direct client setAttack dealt damage, ruling out sword/range/server-damage failure.
- Fix: `Assets/Scripts/Gameplay/Entities/Char.cs` dispatches once after crossing the hit threshold, before end cleanup; new casts reset the guard. No server changes or save-format changes.
- Added `Tools/verify_skill_hit_frame.cs`. In Play mode after entering a map, run: `& 'C:/Users/PC/AppData/Local/Unity/bin/unity.exe' command script eval --file Tools/verify_skill_hit_frame.cs --project-path 'C:/My project' --format json`. Result: 8/8. Covers early/exact/skipped/end frames, duplicate suppression, next cast dead character and projectile-impact duplicate suppression; synthetic remote characters send no packets/change no saves.
- Play-mode verification used normal `GameScr.doUseSkill` calls on the saved character in range, without forcing HP/position/quest counters: five attacks reduced dummy HP 500000 to 0, server quest count became 1/10. This legitimately advances the user's current quest by one kill. No screenshot/manual keyboard play-through was performed; full quest 2 and ranged/buff animations remain unverified.
- Server questcheck: 16/16 passed using the isolated current-server build from onboarding; server source is unchanged. Unity refresh + Console groundTruth: compilationFailed=false, consoleErrors=0. Play was stopped normally for compilation, then reopened on the same saved character; left running for the user.
- Updated PROJECT_STATUS, TASKS, DECISIONS, docs/ARCHITECTURE, docs/TESTING, docs/UnityProjectContext and wiki notes. Wiki export may change during Play/reload; preserve pre-existing changes. No downloads/packages/builds.
- Next: user checks remaining quest-2 kills using keyboard/mouse; investigate only if another symptom remains. Do not mark the whole gameplay play-through complete.

## Loot exclusions fixed (2026-11-11)
- User clarified that dummies and mobs at least 10 levels below the character must NOT drop items. Previously dummies could receive ordinary loot rolls; quest drops ran before the ordinary level gate, and skill sushi bypassed OnMobDead.
- Changed Assets/Server/Net/PlayerItems.cs: shared CanDropLoot gate at OnMobDead entry (including currency/quest paths) and Drop entry (including sushi). Training dummy is template 0, not runtime mob ID. Directional level gate is configurable via DropLevelGapLimit=10. Kept original ordinary-loot abs-gap<=7 rule, chances, XP and quest kill counts.
- Changed Tools/ServerTests/Program.cs: lootcheck tests dummy/template identity, gaps 9/10/11, higher-level direction, configurable threshold and blocked quest/currency/sushi paths. Fresh current-source isolated server build: 0 errors, the same two known warnings. lootcheck 9/9, questcheck 16/16, progresscheck 12/12, all exit 0.
- Unity AssetDatabase.Refresh + Console groundTruth: compilationFailed=false, consoleErrors=0. No manual loot combat play-through was performed. Play stopped normally for compilation; restored for user testing.
- Documentation updated: PROJECT_STATUS, TASKS, DECISIONS, HANDOFF, Assets/Documentation/ServerPort, docs/TESTING and wiki notes (features/constraints/config/deviations/tests). Existing changes from earlier tasks retained.
- Commands: `dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- lootcheck`; also questcheck/progresscheck. Full-path Unity CLI instructions remain above. No new packages/downloads or save migrations.
- Next: user gameplay check. At a 10+ level gap, quest-item collection is intentionally blocked by the chosen rule; use appropriately leveled monsters/character. No gameplay backlog task was otherwise completed.

## High-FPS hit and F1 presentation (2026-11-11)
- User report: game renders 120 FPS but animations look ~30 FPS; clarified monster being hit/damage and F1 popup. Baseline measured ~120 paints/s with ~24 logic ticks/s. Floating numbers/splashes/menu geometry were not interpolated; HP-bar width jumped on packet receipt. General movement interpolation was inspected but left unchanged.
- Changed Assets/Scripts/Application/Interp.cs: fractional effect helpers, pre-tick effect/menu snapshots (also during paused world updates), NsoHealthTween presentation-only HP transition. Changed Screens/GameScr.cs: snapshot/reset slots and fractional paint scopes for floating text/hit splash. Changed UI/Components/Menu.cs: per-row/scroll snapshot rendering, real-time minimum opening (including recent reopen), input guard during opening, remove temporary per-caption arrays. Changed Gameplay/Mobs/Mob.cs: visible HP-bar tween with float width; actual HP untouched. Changed Rendering/mGraphics.cs: float fillRect overload and scoped GUI offset with matrix restoration. Changed Diagnostics/NsoGameControl.cs: smooth-effects toggle, HP seconds=0.12 and minimum menu opening=0.14, Inspector config only, no prefab/scene edits.
- Added Tools/verify_effect_presentation.cs. In Play mode after entering a map: `unity command script eval --file Tools/verify_effect_presentation.cs --project-path "C:/My project" --format json`. 17/17: HP endpoints/midpoint/retarget/full-heal/rate-independence/instant mode, fractional effect samples/frozen snapshots/toggle, menu snapshots/recent reopen/input guard. verify_skill_hit_frame remains 8/8.
- Live diagnostic used startFlyText("0") and the normal F1 command (actionPerform 11000), no damage packets or save edits: 72 rendered floating positions vs 16 logical; 38 rendered menu positions vs 9 logical. Earlier post-change sample ~120.5 paints/s and ~24.5 logic ticks/s. Synthetic timing test is not a complete real-combat play-through. Current Unity compilation/Console groundTruth clean after final code.
- One mid-edit refresh briefly compiled incomplete edits; final current groundTruth has compilationFailed=false and consoleErrors=0. Historical collector count includes those transient errors; do not confuse it with current Console state.
- Updated PROJECT_STATUS/TASKS/DECISIONS/HANDOFF, docs/ARCHITECTURE/TESTING, InspectorConfiguration/UiReference and wiki notes. Prior combat/loot changes retained. No downloads, packages, new artwork, scene writes or Windows build. Play left running.
- Next: user compares repeated F1 opening and live combat feel. Source sprite pose counts remain unchanged; ranged/boss/VFX combinations and release-player performance were not exhaustively checked.

## NSO Workbench upgrade (2026-11-11)
- Completed local control/lookup upgrade. Routes: #overview, #settings, #inspector, #activity, #favorites, #api; old collection/id links preserved. index.html replaced, app.css/app.js added; no remote assets/packages.
- Added World/LootRules.cs (immutable validated policy/atomic persistence), DevWiki/WikiControl.cs (live snapshots/save/history/loot inspector), Data/JsonWriter.cs (existing serializer moved out of WikiData, smaller initial allocation). Modified World/World.cs, Net/PlayerItems.cs, GameServer.cs, ServerBootstrap.cs, DevWiki/WikiServer.cs. Existing data /api retained.
- game-rules.json lives in server save folder. Defaults unchanged: dummy blocked, global >=10 lower-level block, ordinary abs-gap<=7. Save & Apply persists then applies to next loot event; defaults button only fills draft. Failed disk writes/stale revision do not mutate active rules. No real-user policy was changed during tests; writes used temporary save folders.
- Added Tools/ServerTests/WikiControlTests.cs and webcheck; Program is static partial, csproj includes new test. Added Tools/verify_workbench_ui.cjs. Current-source server build: 0 errors / same 2 old warnings; webcheck 27/27; pure UI 18/18 plus node --check; lootcheck 9/9; questcheck 16/16; progresscheck 12/12; wiki 17 collections. Default suite: 18 PASS / 2 known auto-login FAIL.
- HTTP tests cover serving/compatibility, policy/persistence/restart, validation, failed write, revisions, token/Host/Origin, oversized/fragmented bodies, no CORS and file allowlist. UI tests cover all 17 list/detail generators, routing/filtering, escaping, bookmark corruption and live/settings/inspector/history HTML; not DOM/click/layout tests.
- Unity refreshed/current Console: compilationFailed=false, consoleErrors=0. Play left running. Live /control/state returned one real player and unchanged defaults at revision 0; dummy inspector returned training-dummy. Browser host unavailable and native pipe missing: visual/responsive/click QA remains pending. No Windows build.
- Updated README, PROJECT_STATUS, TASKS, DECISIONS, HANDOFF, docs/ARCHITECTURE/TESTING, Assets/Documentation/ServerPort, wiki notes and context. Unity generated .meta files. Preserve all earlier fixes in the working tree.
- Commands: `dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- webcheck`; `node Tools/verify_workbench_ui.cjs`; `node --check Assets/StreamingAssets/NsoWiki/app.js`. URL: http://127.0.0.1:8787/#settings. GET /control/state returns ephemeral token (do not print/log it); POST /control/rules requires X-Nso-Control and {revision,rules}.
- Next: browser refresh; check overview/settings/inspector/#quests/2, desired policy save and mobile layout. Release-player policy reload and debug-web exclusion remain unverified. Standalone wiki serve without GameServer is read-only; controls report unavailable.

## Unsourced HP/MP recovery correction (2026-11-11)
- Report: HP gradually restored without items/external effects. Source confirmed: offline NaturalRegenPercent=2 branch, independent of effects and equipment. Java reference Char.updateEveryHalfSecond/updateEveryFiveSecond only adds effect/equipment recovery.
- Changed Assets/Server/Net/PlayerEffects.cs default 2 -> 0 and comments; PlayerActions.cs LastRegen comment now identifies five-second equipment timing. No damage/loot/save/web behavior changed. NaturalRegenMs remains 5000 so equipment recovery is preserved; positive natural percent remains explicit opt-in code, not active default.
- Added Tools/ServerTests/RegenTests.cs; csproj includes it and Program dispatches regencheck. Fixture tests actual UpdateHalfSecond with forced recovery boundaries; no reader thread/gameplay/save edits. Before fix: 7 FAIL / 2 PASS; after fix: 9/9. Covers source-free HP/MP, food, HP potion, equipment half-/five-second intervals, expired food, caps and dead players.
- Nearby checks: questcheck 16/16, progresscheck 12/12. Current-source .NET build 0 errors / same 2 old warnings. Unity refreshed; current Console compilationFailed=false, consoleErrors=0. Play restarted normally; live check uses an isolated state, never forces the user's HP/inventory.
- Updated PROJECT_STATUS/TASKS/DECISIONS/HANDOFF, README, docs/ARCHITECTURE/TESTING, Assets/Documentation/ServerPort and wiki notes (regen feature/default/interval/deviation/test). Historical 2% notes are superseded, not a current requirement.
- Command: `dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- regencheck`. Next: user verifies idle behavior with no active food/equipment recovery. No Windows build; actual food/gear still restores HP/MP by design.
- Final Play-mode probe on an isolated state (not the user's character): after 6 seconds with no effects/equipment, HP stayed 50 and MP stayed 40; NaturalRegenPercent=0. Workbench config API also reports default 0. Play left running.

## Movement release delay correction (2026-11-11)
- User report: character continues moving for about 0.4 seconds after movement is released. Confirmed in Play: original Char.updateCharRun decrements velocity by one per logic tick. Isolated character at current speed 8 took 9 ticks (0.375 s at 24 Hz) and moved 36 pixels after release.
- Changed Assets/Scripts/Application/GameCanvas.cs: detect owned horizontal release (arrows, PC A/D aliases, number-pad) before clearing it, then request guarded stop. Assets/Scripts/Screens/GameScr.cs: request the same stop after pointer horizontal holds are cleared. Assets/Scripts/Gameplay/Entities/Char.cs: StopGroundMovementOnRelease only affects local status-2 running with no destination and no remaining horizontal hold; resets cvx/bonus speed and enters standing. Existing friction remains for other movement.
- Assets/Scripts/Diagnostics/NsoGameControl.cs: Instant Movement Stop defaults true, Inspector opt-out restores legacy behavior. No speed, server, save format, scene, package or original-source changes. Commented/documented intentional offline client deviation.
- Added Tools/verify_movement_release.cs: 18/18 Play checks, using isolated characters and restoring singleton/input/config; no packets/save edits. Covers six keyboard aliases, remaining direction/alias, unrelated/unowned release, jump/fall/special state, destination/remote motion, pointer-cleared helper and opt-out. Same baseline probe after fix: velocity 0, stopTicks 0, distance 0, standing. Pointer helper is tested; physical on-screen interaction is not claimed.
- Unity source refresh initially left input assembly stale; explicitly imported changed input scripts and requested compilation, waited for reload, then reran all checks successfully. Final compilationFailed=false, compiling=false, consoleErrors=0. Play left running; no Windows build.
- Updated PROJECT_STATUS, TASKS, DECISIONS, HANDOFF, docs/TESTING, Assets/Documentation/InspectorConfiguration and UiReference, wiki config/deviation notes. Preserve all earlier uncommitted work.
- Reproduce checks in a map: `unity command script eval --file Tools/verify_movement_release.cs --project-path "C:/My project" --format json`. Next: user confirms physical A/D/arrows and on-screen button release feel; compare Inspector Instant Movement Stop on/off if needed.

## Broad render-motion pass (2026-11-12)
- User requested generally smooth animations at >=60 FPS. Extended existing render-only interpolation; did not change logic rate, combat, loot, server or save schema. Render cap range is 60-144 (default 60). Smooth World Motion is a new Inspector opt-out (default true). Existing Smooth Effects also controls weather/list motion.
- Files: Assets/Scripts/Application/Interp.cs (fractional residual dictionary, capture of additional world objects, nested absolute-coordinate ObjectScope, camera/map guards); Main.cs (scroll-transform cleanup in paint finally); Diagnostics/NsoGameControl.cs (world toggle/render-cap floor). Small paint wrappers in Gameplay/Entities/Char.cs, NPCs/Npc.cs, Items/ItemMap.cs, Mobs/{Mob,MobSoul,EggMonters,BuNhin}.cs and Effects/{Arrow,MonsterDart,Domsang,Lanterns,ServerEffect}.cs. Effects/AnimateEffect.cs captures weather-particle positions. Screens/GameScr.cs scopes fractional camera translation to world pass. UI/Components/Scroll.cs captures weakly registered list states; Rendering/mGraphics.cs applies/clears list-content placement before clipping without changing viewport/hit tests.
- Added Tools/verify_world_presentation.cs: 94 passed, including 10 object types, fractional one-pixel motion, teleports, coordinate restoration, camera sign/restoration, toggle, scroll pause/recycle/geometry and 60-cap floor. Regression: verify_effect_presentation 17/17, verify_movement_release 18/18, verify_skill_hit_frame 8/8. No gameplay packets/save edits from these isolated checks.
- Added Tools/measure_presentation_rate.cs: asynchronous actual Main Repaint/logic counter measurement; temporarily samples caps 60/120, restores prior setting, reports to %TEMP%/nso-presentation-rate.txt. Observed 60.0/120.0 paints per second and 24.0 logic ticks per second in the current village. This is throughput in a two-second sample per cap, not p99 combat profiling or minimum FPS on every device.
- Unity refresh/compile complete; current Console compilationFailed=false, compiling=false, consoleErrors=0. Captured and inspected world and bag layouts via ScreenCapture; physical scroll/keyboard feel and every effect/combat scene are NOT visually certified. No Windows build. Editor remains in Play; bag opened for UI smoke inspection.
- Known limits: no new sprite poses or sprite blending; finite source frames still step. Original paint-owned pose/effect counters (e.g. Char.paintCharWithSkill) retain pre-existing behavior and need a separate timing audit before any FPS-independent combat-animation claim. Lightning/random flicker and other discrete frame-only effects retain original pose behavior. General 60+ FPS is NOT guaranteed from a render cap or this limited sample.
- Updated PROJECT_STATUS/TASKS/DECISIONS/HANDOFF, docs/ARCHITECTURE/TESTING, Assets/Documentation/InspectorConfiguration/UiReference and wiki notes. Preserve all prior uncommitted fixes.
- Commands in Play, in a map: `unity command script eval --file Tools/verify_world_presentation.cs --project-path "C:/My project" --format json`; same for Tools/measure_presentation_rate.cs (read report after ~6 s), verify_effect_presentation.cs, verify_movement_release.cs, verify_skill_hit_frame.cs.
- Next: physical-display feedback and representative combat/map frame-time captures; if genuinely more sprite poses are required, agree artwork/timeline scope before adding them.

## 2026-11-12: double-click = Enter
- Changed: `Assets/Scripts/Screens/GameScr.cs` (`onTargetClicked`, called from each branch of `doFocusbyTouch`), `Assets/Scripts/Diagnostics/NsoGameControl.cs` (`DoubleClickMs`, default 400, 0 = off). Added `Tools/verify_double_click.cs`.
- Tests: Unity compile 0 errors; `verify_double_click.cs` 6/6 in Play mode (logic only). User does the hand test.
- Known limit: only world targets; menus/lists unchanged. If auto-attack is on, Enter keeps its original auto-attack behaviour.

## 2026-11-12: gamepad (PS5 DualSense) input
- Added `Assets/Scripts/Input/NsoGamepadInput.cs`: auto-created MonoBehaviour that polls `Gamepad.current` (Input System 1.19, already installed; Active Input Handling = Both) and calls `GameCanvas.keyPressed/keyReleased` with MyKeyMap codes. Layout in `Assets/Documentation/InspectorConfiguration.md`.
- Tests: Unity compile only (0 errors). Not run with a real pad; user tests.
- Limits: no quick-item key, so Square/R3/Create are unused (except: Square in the Kỹ năng tab runs the "+" UPPOINT command 14001, `_squareUpSkill`, compile only). Cross decides jump vs confirm at press time with `InWorld()`.
- Title menu (`SelectServerScr.updateKey`): Up/Down arrows (keyPressedz[24]/[25], keyboard and gamepad) now move the row selection; old keys kept. In-game `Menu` and the bag (left/right) are unchanged. Compile only.
- Target switching for gamepad: `Assets/Scripts/Input/TargetCycler.cs` (new). Right-stick flick or R1 focuses the next of the N nearest on-screen targets (mob, NPC, player, ground item) sorted by elliptical distance; wraps to 0. List refreshed every 0.1 s in `NsoGamepadInput.Update`, top-N insertion, no allocation. Replaces the use of `Char.findNextFocusByKey` for R1. Compile only.
- Gamepad Options toggles: second press closes the menu and all GameScr panels (`NsoGamepadInput.CloseGameMenus`). Compile only.
- Dialog bottom buttons (yes/no etc.): Left/Right arrows (keyboard + gamepad) choose a button, Enter/Circle presses it (`Dialog.MoveSelection`, `Command.keySelected` highlight in `Command.paint`/`Paint.paintCmdBar`). Not in InputDlg/Input2Dlg. Without an arrow press, Enter keeps the original centre-button behaviour. Compile only.
- Game menu (`GameScr.actMenu`): removed "Thủ khố", "Tự sát", "Khu vực" permanently (user request); their perform cases 110022-110024 are kept. Compile only.
- Shop grid: arrows move the item selection even while the item-info panel is open (`GameScr.updateKeyBuyItemUI`, gridShop check). Info text keeps mouse scroll only in those shops. Compile only.
- 2026-11-12: Grid shops on PC have no center command, so Enter / gamepad Circle did nothing on a selected item. GameScr now runs the left Buy command on Enter in grid shops (opens the buy popup); `IsGridShop` property shared with updateKeyBuyItemUI. Compiled only; user tests.
- 2026-11-12: Dialog input moved into the base class `Dialog` (user request: one shared class, not per-dialog fixes). Subclasses override `FocusedField` (and `UpDownStepsNumber`); the base routes typed keys, updates the field, keeps it focused, lets Left/Right choose a bottom button in every dialog (input dialogs included), and Up/Down step numeric fields by `Dialog.NumberStep` (min `Dialog.NumberMin`) for gamepads. InputDlg/Input2Dlg trimmed. TField.update syncs from TouchScreenKeyboard only when `isSupported` (on PC the stub keyboard wiped typed text). Fixes the buy-quantity dialog. Compiled only; user tests.
- 2026-11-12: Auto panel (Tính năng > Tự động): GameScr.updateCommandForUI now shows the "Chọn" (1510, doAutoSelect) command on touch/mouse clients too; it was hidden by `!GameCanvas.isTouch`. Compiled only; user tests.
- 2026-11-12: Auto panel keyboard/gamepad: selected-row highlight + arrow on all clients; scroll item size 35 -> `AutoRowStep` 30 (matches row spacing, also fixes click row picking); visible height snapped to whole rows (no half-cut 'Không nhặt gì cả'); `AutoScrollToRow` scrolls by whole rows; up/down wrap off-by-one fixed. GameScr.paintUIAuto / updateKeyBuyItemUI. Compiled only; user tests.
- 2026-11-12: User rule: do not change the original UI. Reverted the auto-panel highlight box and the snapped view height; kept the original selection arrow (now also on PC), the 30 px scroll item size, whole-row scrolling and the wrap fix.
- 2026-11-12: Auto panel: Enter / gamepad Circle now toggles the selected row (doAutoSelect) in GameScr.updateKey. Open issue: user reports the 'Không nhặt gì cả' row missing after the scroll change; waiting for a screenshot.
- 2026-11-12: Auto panel rows unreachable past 'Nhặt Yên, HP, MP và N.Liệu' (user screenshot). Arrow navigation now uses the const `AutoRowCount` (14) instead of the shared `indexRowMax`, and AutoScrollToRow clamps to the last row. Compiled only; user tests.
- 2026-11-12: Root cause of auto-panel scroll stop (runtime probe: row=9, cmtoY=67, indexSelect=24): on PC (isTouch) keys were not consumed after the auto block, so the grid-shop key code also ran and scrolled by indexSelect. updateKeyBuyItemUI now clears keys for isPaintAuto too. Compiled only; user tests.
- 2026-11-12: Removed from the game (user request, permanent): auto panel rows after 'Không nhặt gì cả' (paint + click areas, AutoRowCount 14 -> 8) and the features behind them: Auto.Actions.reMap / ChuyenMapHetBoss / isDieMP / isTinhAnh / isThuLinh (default behaviour hard-coded: return to auto map, change map when no boss, attack elites and bosses) and auto reconnect (GameScr.autoReconnect, NsoGameControl._autoReconnect, F6 > Chơi toggle, Controller branches; Session_ME.autoReconnect() left unused). Files: GameScr.cs, Auto.cs, TanSat.cs, AutoNVHN.cs, AutoTask.cs, Controller.cs, NsoGameControl.cs, NsoSettings.cs, docs. Compiled only; user tests.
- 2026-11-12: Gamepad left stick is run-only in the world (user request): its up/down is ignored while InWorld(), so tilting it never jumps. D-pad up still jumps; menus still navigate with the stick. Toggle: NsoGamepadInput._stickRunOnlyInWorld (default on). Compiled only; user tests.
- 2026-11-12: Bag (Hành trang): Enter / gamepad Circle now runs the item-info left command 'Sử dụng' (11040 -> actleftItemInfo) when no center command exists (large screens). GameScr.updateKey Enter fallback, limited to isPaintInfoMe && indexMenu == 0. Compiled only; user tests.
- 2026-11-12: Bag Enter fix v2 (user: still failing on gamepad): with arrow/D-pad navigation setLCR leaves left = 'Chọn' (11022), not 11040, so the first fallback never matched. Enter / Circle in the bag (isPaintInfoMe, indexMenu 0, item focused, no center) now calls actBagUseItem directly (closing the info panel first). Compiled only; user tests.
- 2026-11-12: Bag Enter v3 (user request): Enter / Circle on a bag item now opens the original 'Chọn' option menu (actBagSelectItem) with 'Sử dụng' pre-selected (Menu.startAt selects entry 0 on PC); a second Enter uses the item. Compiled only; user tests.
- 2026-11-12: Bag 'Sắp xếp' did nothing: the offline server ignored sub-command BAG_SORT (-107). Ported Java Char.bagSort as PlayerItems.BagSort (merge stackable non-expiring items of the same id/lock, pack to the front, save, reply -107 so the client runs its own Char.bagSort). Dispatch in Server/Net/Controller.OnSubCommand. Box sort (-106) is still not ported. Compiled only; user tests.
- 2026-11-12: Bag empty cell: Enter / Circle now runs the left 'Sắp xếp' command (110221) when no center command exists (GameScr.updateKey fallback, bag only). Compiled only; user tests.
- 2026-11-12: Chat box = dev terminal (user request). The original chat icon (GameScr touch control) opens a restored ChatTextField (rewritten: original TField at the bottom, 'Đóng' command bar). Enter sends the line through NsoTerminal -> Service.chat (CHAT_MAP -23); Server/Net/DevTerminal.cs runs it and replies with SERVER_MESSAGE (-24) yellow InfoMe lines. Commands: help, item <id> [sl], tim <tên>, timmap <tên>, map <id>, yen <n>, xu <n>, exp <n>, hoi, vitri. Files: ChatTextField.cs, NsoTerminal.cs (new), GameScr.cs (IChatable, icon click, onChatFromMe/onCancelChat), Service.cs (chat), Server/Net/DevTerminal.cs (new), Server/Net/Controller.cs (-23), Server/World/World.cs (AllItems/AllMaps). Compiled only; user tests.
- 2026-11-12: Dev terminal v2 (user): the chat icon now opens the original message window (GameScr.openTerminal: isPaintAlert + isPaintMessage popup like openUIChatTab, title 'Terminal', close 11066) showing NsoTerminal.Log, with the ChatTextField input under it. Replies come as SERVER_MESSAGE with a char-7 marker and are routed into the log by the client Controller (-24); other server messages still go to InfoMe. All commands except help were removed at the user's request (World.AllItems/AllMaps removed too). Compiled only; user tests.
- 2026-11-12: Dev terminal v3 (user): input line no longer overlaps 'Đóng' (NsoTerminal.ApplyLayout puts it ButtonGap px above Paint.CmdY); the window is larger (up to MaxWidth 420, from ScreenMargin at the top down to the input line). Commands need the '/' prefix: /help (server) and /clear (client, empties the log). Compiled only; user tests.
- 2026-11-12: Dev terminal v4 (user): console style. The prompt ("> text_" with blinking caret) is the last row of the window (NsoTerminal.View = Log + prompt); the ChatTextField box is no longer painted while the terminal is open, and the window reaches down to just above 'Đóng'. '/' could not be typed: MyKeyMap had no Slash mapping and GameCanvas.keyPressed dropped 47 from keyAsciiPress; added KeyCode.Slash/KeypadDivide -> 47, passed as text only while the chat box is open. Lines without '/' are public chat: speech bubble over the player (ChatPopup, ChatBubbleTicks) plus a green 'Name: text' row; offline deviation, no server round trip (nobody else on the map). Compiled only; user tests.
- 2026-11-12: Terminal 'Đóng' needed two clicks: onCancelChat called GameScr.perform(11066) (IActionListener, no such case) instead of actionPerform(11066). Fixed. Compiled only; user tests.
- 2026-11-12: Terminal caret: the '_' text caret was replaced by a blinking bar drawn after the prompt text (NsoTerminal.PaintCaret from ChatTextField.paint, inside the window's scroll clip; CaretWidth/Height/Color/BlinkTicks configurable). Compiled only; user tests.
- 2026-11-12: Chat/terminal Vietnamese input: while ChatTextField is open, Main.OnGUI types Event.current.character (Unicode from Unikey / Windows keyboard, case and punctuation) straight into the field and skips the ASCII key-code half of printable keys; control keys (Backspace, Enter, arrows) keep the key-code path. Input.imeCompositionMode is On while the box is open. Caret stays the blinking '|' bar. Compiled only; user tests.
- 2026-11-12: Vietnamese typing fix v2: Unikey writes a mark as Backspace + marked letter; Backspace went through the deferred keyAsciiPress path with TField's 3-tick repeat block, so letters were dropped/doubled. While the chat box is open Main.OnGUI now applies Backspace immediately (tfChat.clear) in order with typed characters. Compiled only; user tests.
- 2026-11-12: Vietnamese typing v3 (user: still failing with a Vietnamese keyboard): chat text now comes from Input.inputString in Main.Update (UpdateChatText: Unicode chars, char 8 = Backspace in order, IME composition On while open, uncommitted Input.compositionString shown in the terminal prompt). Main.OnGUI only swallows printable/Backspace/Space key events while the box is open. Terminal caret changed back to an underscore (CaretWidth 5, CaretHeight 1, CaretTop 10). Compiled only; user tests.
- 2026-11-12: Terminal public chat rows now read '@Name: text' (NsoTerminal.NamePrefix). Compiled only.
- 2026-11-12: Terminal chat rows: '@Name:' yellow, text white. NsoTerminal writes the first row as head + SplitMark (char 1) + tail; GameScr.paintAlert draws such rows in two colours (other alerts unaffected, they never contain char 1). Compiled only; user tests.

### Dev terminal: /skip (2026-11-12)
- `/skip` completes the current quest: takes it if not taken, jumps to the last step, runs `FinishTask` (normal rewards, next quest). Fails with a reply when the bag lacks free slots.
- Files: `Assets/Server/Net/PlayerTasks.cs` (`SkipTask`), `Assets/Server/Net/DevTerminal.cs` (case "skip", /help line).
- Unity compiled clean; not tested in Play mode.

### Dev terminal: /lv, /tolv n (2026-11-12)
- `/lv` = +1 level; `/tolv n` (also `/tolv50`, `/tolv[50]`) = up to level n, 0 <= n <= `DevTerminal.MaxTargetLevel` (150, also capped by the level table). Only raises the level.
- Gives exactly the missing experience through `PlayerCombat.AddExp` (normal level-up: points, client sync, quest hook).
- Files: `Assets/Server/Net/DevTerminal.cs` (`LevelTo`), `Assets/Server/World/World.cs` (`ExpForLevel`, `MaxLevel`).
- Unity compiled clean; not tested in Play mode.

### Basic attack grows without a school (2026-11-12)
- Class 0 (wooden sword): +1 point of "Chiêu cơ bản" (skill template 0) per level, spent automatically, up to 9 points (level 10). Each point -20 ms cooldown (500 -> 320). After joining a school the player spends skill points as before (the school clears the basic skill).
- The data has only one row for template 0 (likely a data bug). Rows for points 1..9 (ids 30001..30009) are generated on both sides: client `Controller.AddBasicSkillGrowth` (after createSkill), server `GameWorld.AddBasicSkillGrowth` (World.cs). Config: `Assets/Scripts/Gameplay/Skills/BasicSkillGrowth.cs` (MaxPoint, CooldownStepMs, MinCooldownMs).
- Server: `PlayerCombat.SyncBasicSkill` on Init (old saves) and on level-up (then Select + LoadSkill).
- Unity compiled clean; not tested in Play mode. Check: level up with the wooden sword, the basic attack should hit a bit faster; skill info shows the point.
- Fix (same day): skill panel could not scroll for the grown basic attack. paintKyNang looked up the "next level" row past the last point (IndexOutOfRange in paint, so scrInfo.setStyle never ran). Now the panel uses `BasicSkillGrowth.DisplayMaxPoint` (shows max 9, "max level" at point 9) and never indexes past the last row. Compiled only.

### Mouse wheel scrolling (2026-11-12)
- Every up/down `Scroll` list scrolls with the mouse wheel when the cursor is over it (skill info, other InfoMe tabs, alerts/terminal, shops...). `Main.Update` feeds `Scroll.AddWheel` (Input.mouseScrollDelta, cursor in game coords); `Scroll.updateKeyScrollUpDown` takes it and moves `cmtoY` (clamped). Config: `Scroll.WheelStepPx` (24), `Scroll.WheelKeepSeconds`.
- Works only where the screen calls `updateKey()` on that list (same as drag scrolling). Left/right lists unchanged. Compiled only; user tests.

## Scrolling text follow-up (2026-11-12)
- Report: the MP-full scrolling notification still stepped at the low logic rate.
- Changed Assets/Scripts/UI/HUD/Info.cs, InfoMe.cs and Application/Interp.cs: capture pre-tick notification identity/position, interpolate fractional text placement under Smooth Effects, preserve fixed clip and original timers/speed, clear content offset in finally.
- Added Tools/verify_notice_presentation.cs (isolated state-restoring probe). Preliminary version passed 24 checks; final version adds pre-tick capture/paused checks and was only compiled, per the user request to stop runtime debugging. Do not report final runtime or visual verification.
- Unity final groundTruth: compiling=false, compilationFailed=false, consoleErrors=0. No release build. Existing unrelated dirty changes preserved.
- Next: user checks entrance/exit of MP-full notifications with Smooth Effects enabled. Optional probe command: unity command script eval --file Tools/verify_notice_presentation.cs --project-path "C:/My project" --format json (not run on the final revision).

## Wheel in information panels (2026-11-12)
- User reports wheel does not scroll information/skill descriptions. Source inspection found unconditional value/120 in NsoInputSystemBridge.Wheel, although installed Input System 1.19 defaults to normalized +/-1. Scroll rounds 1/120*24 to zero.
- Changed Assets/Scripts/Input/NsoInputSystemBridge.cs to honor scrollDeltaBehavior and platform; native Windows still divides by 120, normalized/non-Windows units pass through. Updated Scroll.cs input-owner comment and InputSystemMigration.md. Existing changes preserved.
- Validation: Unity compilationFailed=false, compiling=false, consoleErrors=0 after refresh. Compile/Console only at user request; no new Play session, automated runtime tests or hardware checks. Next: user opens a long skill/item description and scrolls both directions with pointer over the content.

## Workbench dark mode (2026-11-12)
- Updated Assets/StreamingAssets/NsoWiki/app.css: charcoal surfaces, high-contrast text/status colors, larger fonts, heavier labels, Apple-first native UI/mono stacks, wider sidebar, larger buttons and focus indicators. index.html declares dark color-scheme; app.js has one inline help font increased from 11px to 16px.
- No C# or backend changes, dependencies/downloads/publication. Apple typography uses installed system fonts; Windows falls back to Segoe UI. Existing unrelated edits preserved.
- Validation limited to source/syntax checks at user request; no browser screenshots, live click tests or Play session. Next: user refreshes http://127.0.0.1:8787/ while game runs and checks overview, long records/settings and narrow windows.

## Unified font follow-up (2026-11-12)
User requested one font style. app.css removes --font-mono and uses --font-ui for code/pre/.mono/table IDs/record IDs too. All typography now shares the existing Apple-first system UI stack (Segoe UI fallback on Windows). No C# changes or runtime tests; source checked for remaining alternate families. Next: user refreshes the web page.

## Global game-balance dashboard handoff (2026-11-12)
Final scope is whole-game persistent balancing, per user clarification. No new PlayerProgress/PlayerItems persistence fields or changes remain.
Added: World/GameBalanceRules.cs, World/MobTuning.cs, Net/PlayerTuning.cs, DevWiki/WikiDashboard.cs, NsoWiki/dashboard.js and meta files.
Modified: WikiControl/WikiServer (protected route/asset), GameServer (config load), World/GameData (catalog/shop/option access and spawn hook), PlayerCombat (global formulas/EXP/wide math/configured monster HP), MobBrain (wide resistance math), PlayerService (effective item/shop options), DevTerminal (level-command EXP bypass), EnterWorld (speed/EXP-debt sync and tuned HP), JsonWriter (string-key IDictionary), app.js/index.html/app.css (dashboard navigation, UI and general history).
Persistence: save-folder game-balance.json v1 stores {player:{stat:[percent,flat]},equipment:{templateId:{optionId:[percent,flat]}},experiencePercent}. Equipment -1 means all templates; template rules follow global rules. monster-tuning.json v1 stores Level/MaxHp/DamageOnPlayer/DamageOnPlayer2/AttackDelay/Sys per template. Defaults/reset retain original gameplay. Both files load in release without web.
Character balance covers four potentials and MaxHp/MaxMp/Speed/Damage/Damage2/Exactly/Miss/DameDown/ResFire/ResIce/ResWind/Fatal/FatalDame/PercentFatalDame. Potential calculations use a copy, final stats scale once, original save allocations stay unchanged. Equipment rules are effective overlays on old/new items, never baked into saves. Normal EXP gains scale; /lv and /tolv retain their exact target.
API: GET /control/dashboard returns config/schemas/catalogs/read-only samples. POST same path accepts {revision,kind,values,reset?,templateId?}, kinds player/equipment/monster. Existing token/origin/Host/8192-byte body guards remain. Writes replace a temp file before publishing; lock order changes -> SyncRoot; existing packets refresh same-map caches. Client refresh failures after saving are logged as deferred.
Validation: JS syntax checked. Unity final compilation/current Console state recorded below. No dashboard mutation, browser test, agent-started Play, restart test or release build. User requested compile only.
Standalone dotnet build attempt found the existing test project omits shared BasicSkillGrowth source/type dependencies already referenced by World/PlayerCombat. No test-project workaround retained; no server tests run.
Next: user opens Play and http://127.0.0.1:8787/#dashboard. Compare player coefficients on old/new characters and after repeated skill/equip recalculation; check global/template equipment overlays on old/new items and shop info, then reset; change/reset monster template, spawn/recover and restart. Extreme tuning/cache refresh/runtime behavior is not certified.
Commands: node --check Assets/StreamingAssets/NsoWiki/app.js; node --check Assets/StreamingAssets/NsoWiki/dashboard.js; Unity script eval refresh then command console status.
Final validation: Unity compilationFailed=false, compiling=false, consoleErrors=0 (2026-11-12). Both JS files pass node --check. No runtime tests run. During an external Editor/Play transition the Console briefly showed Main.FixedUpdate:217 NullReferenceException; it is absent from the final current Console, and was not investigated at the user's compile-only request.

## Character progression refactor handoff (2026-11-12)
- Added World/CharacterProgressionRules.cs, DevWiki/WikiProgression.cs, NsoWiki/progression.js and docs/CHARACTER_PROGRESSION.md (+ generated meta files for assets).
- Modified GameBalanceRules for optional inherited profiles/version-2 persistence/curve cache; PlayerTuning for shared native-curve application and cached field mappings; PlayerCombat for explicit snapshot calculation and unsaved reference samples; PlayerTasks/Init share original Java class potential defaults. Missing classed allocation data now falls back to its class base rather than the unclassed level curve. Valid existing saves stay unchanged.
- WikiDashboard adds progression save/read-only preview kinds and metadata; WikiServer/index load the new frontend module. dashboard.js retains equipment/monster/legacy controls and mounts the new character UI; app.css adds restrained tables/groups and advanced visibility.
- UI: unclassed/classed groups, shared class defaults and per-class overrides, original/editable/calculated Level-1 columns, native-growth percent plus per-level additions, scoped reset/discard, local drafts, debounced server preview with level range/class comparison/milestones/deltas. Naked classed previews deliberately do not allocate free points. Derived formulas remain original, including MP floor; Defense means DameDown.
- Config: balance.characterProgression[scope][stat] with optional base/growthPercent/growthFlat; scopes 0/classed/1..6. Specific fields inherit from shared classed profile; reset removes selected scope, leaving parent rules. Existing player/equipment/EXP settings preserved. Version 1 is accepted, next save writes version 2. No player-save rewrite or config write on preview.
- API: POST /control/dashboard kind progression saves a scope; progressionPreview with scope/values/from/to/classes/revision calculates a draft only. Existing token/origin/size/revision checks apply. Both preview/gameplay use SetAbility; no frontend stat calculator. Curves cached per immutable balance snapshot/class/level.
- Validation: final Unity compile/Console and JS syntax results recorded below. No Play/browser/API mutations/save-restart tests run, per user request. Prior unrelated working-tree edits preserved; no commits/dependencies/downloads.
- Next user checks: empty-profile parity; class 0 automatic growth vs classed manual allocation; base potential affecting HP/MP/attack; classed inherited vs per-class override; draft preview without persistence; save/restart/relog with unchanged raw potential allocation and repeated skill/equipment recalculation. Compare both outer and inner classes.
- Commands: node --check Assets/StreamingAssets/NsoWiki/progression.js; node --check Assets/StreamingAssets/NsoWiki/dashboard.js; Unity script eval refresh, then command console status. Standalone dotnet test project still has the earlier shared BasicSkillGrowth dependency issue; no tests run.
- Final refactor validation: Unity compilationFailed=false, compiling=false, consoleErrors=0 (2026-11-12). progression.js, dashboard.js and app.js pass node --check. Scoped diff whitespace checks pass. No runtime/API/browser/save/restart tests executed.

## Progression formula explanations (2026-11-12)
Changed progression.js and app.css only for UI behavior: Step 02 includes open formula-term guidance, example HP at level 5 (80 + 40*1.5 + 2*4 + 20 = 168), inheritance/zero/classed-growth notes and an expandable glossary for all displayed character attributes. No stat math, API or persistence changes. node --check progression.js passed; no Unity C# change/runtime/browser tests. User refreshes #dashboard to inspect the help panels. Docs/wiki notes updated.

## Wooden-sword preview separation (2026-11-12)
- Changed progression.js/app.css: separate unclassed/classed comparison fieldsets; Wooden Sword is a default-on independent toggle and labeled scenario row under unclassed. Other unclassed rows remain unequipped. Labels/explanations distinguish equipment scenarios from classes.
- WikiDashboard allows includeWoodenSword; WikiProgression validates the bool, adds scenario keys and computes temporary class-0 samples equipped with quest sword #194. Step-01 naked base calculations stay unchanged.
- PlayerItems.CreateTaskWoodenSword centralizes the exact existing quest reward; PlayerTasks uses it, preserving original options/lock/quantity. Tajima alternate menu reward is untouched. No actual player, save, class membership or balancing rule changes.
- Unity compilationFailed=false, compiling=false, consoleErrors=0. node --check progression.js passed; scoped whitespace checked. No browser/API/runtime/save tests per user request. Next: user refreshes #dashboard and compares bare unclassed, Wooden Sword and actual classes; inspect its attack difference under equipment rules.

## Wiki access diagnosis (2026-11-12)
User could not reach hardcoded 8787. Read-only checks: that listener timed out; current Play runtime reported WikiUrl=http://127.0.0.1:8788/ and wiki thread alive. HTTP 200 verified on 8788 for /, /db.json, /control/state, /control/dashboard and /progression.js. Both listeners belonged to the same Unity process; exact origin of the stale 8787 listener was not established. No source/config/gameplay changes or process termination performed. User can use 8788/#dashboard now and Server Control open-wiki button on later runs. Listener cleanup across reloads is not verified by this access diagnosis.

## Workbench sprite display handoff (2026-11-12)
- Added DevWiki/WikiSprites.cs and NsoWiki/sprites.js (+ generated meta files). Modified ServerBootstrap to wire original world/item-table art; WikiServer serves sprites.js and read-only /sprites/items/{id}.png and /sprites/mobs/{id}.svg with a binary response overload. Existing route/header/security behavior remains; HTML/API still no-store.
- Item IDs resolve to original icon PNGs. Mob portraits use original first images; bosses compose the first movement frame with ImageInfo/Frames offsets, flips and layer order, using embedded source PNG data. Zoom 4 preferred, falling back to 3/2/1. Towers 98/99 use their original single filenames. No image generation/copying, external dependencies or Unity APIs on HTTP threads.
- Frontend app.js shows sprites in item/mob rows, details and related links; dashboard.js shows the selected equipment/monster sprite. CSS preserves pixel art; sprites.js handles lazy decoding and missing-art fallback without inline events.
- Validation: Unity compilationFailed=false, compiling=false, consoleErrors=0; sprites.js/app.js/dashboard.js syntax passed. Source dummy image was inspected, not a browser screenshot. No Play/API/image route or browser tests, per user compile-only request. Prior working-tree changes preserved.
- Next: start Play, use NSO Server Control > Open dev wiki in browser (actual port can differ from 8787), refresh, check item #194, mob #0/1, old/modern bosses and missing-art fallback. Boss composition/orientation across all templates remains visually unverified. Cached image changes need Play restart/browser refresh.

## F6 default-browser shortcut (2026-11-12)
Changed Assets/Scripts/Diagnostics/NsoSettings.cs: first Dev Action is Open balance web; click/Enter calls OpenWorkbench, resolves current WikiUrl and opens #dashboard with Application.OpenURL. Existing hint line reports not-ready/failure and the current address. No new saved option/UI layout/gameplay/server changes. Unity final compile/Console result recorded below; no Play/click/browser launch test per user preference. Next: user opens Play, F6 > Dev > Mo web can bang, then click/Enter; verify default browser and actual fallback port.
Final F6 validation: compilationFailed=false, compiling=false, consoleErrors=0. Scoped source whitespace check passed.

## Progression header/card correction (2026-11-12)
Changed progression.js/app.css: removed top group tab card and obsolete state/handlers; moved compact scope/reference dropdowns into Step 01. Step 03 has independent Unclassed, Wooden Sword and Classed fieldsets. Draft retention/inheritance/backend calculations unchanged. node --check progression.js passed; no C# changes or runtime/browser tests. User refreshes the current web URL with Ctrl+F5.

## Step 02 collapsed by default (2026-11-12)
Changed progression.js/app.css: Step 02 uses details/summary without open, nested term help is also closed, form actions moved outside so Level-1 edits can still be saved while growth is collapsed. Invalid-input handling reveals the relevant growth section for browser focus. node --check progression.js passed; no C#/runtime/browser tests. User refreshes the web page.


## 2026-11-12 - Slow effect animation previews
- Added original status icons and a separate paginated animation gallery in the Effects tab. Status IDs are not assumed to map to animation IDs.
- Sources: `nj_effect` frame order, image IDs and offsets from `GameScr.readEfect` / `ServerEffect.paint`; allowlisted original Small images served by WikiSprites as zoom-normalized SVGs. No gameplay calculation or timing changes.
- Files: WikiData.cs, WikiSprites.cs, WikiServer.cs, ServerBootstrap.cs; NsoWiki/effects.js (+ Unity meta), sprites.js, app.js, index.html, app.css; project status/task/decision/handoff documents, ServerPort.md and notes.json.
- Presentation defaults: 4 frames/second, 12 cards/page, at most 6 visible animated canvases, one shared timer, 128-image client cache and existing 16 MiB server asset cache. Pause on hidden document, offscreen cards and route changes; respect reduced-motion preference at initial load. Manual pause/resume available. Missing art displays a notice.
- Validation: node --check passed for effects.js, sprites.js and app.js. Unity refreshed and finished compiling: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No automated gameplay tests or Play/browser validation, per user compile-only instruction.
- Known limitations: names of standalone VFX are numeric source IDs; no invented association to status effects. Browser rendering/performance remains unverified.
- Next: start Play manually, open the workbench through F6 > Dev > Open web, visit Effects, check looping, pagination, pause/resume and scrolling. CLI validation: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.

## Menu cell caption centring (2026-11-12)
- `Menu.paintMenu` draws the cell captions under `TextCalib.Use("menuCell")`, so they are centred by their drawn pixels instead of the bitmap font's advance box (the text looked shifted right). New tunable item "Menu cell captions" in `TextCalib` (default X -0.5: the anchor is half a pixel right of the 57 px cell image centre). Files: `Assets/Scripts/UI/Components/Menu.cs`, `Assets/Scripts/Diagnostics/TextCalib.cs`. Compiled clean; not tested in Play mode.

## Dev wiki: effects preview and filters (2026-11-12)
- `effects.js`: all 12 cards of a page animate (`maxPlaying = pageSize`; was capped at 6).
- `app.js`: every data tab gets a filter: "Lọc theo" (column) + "Giá trị" (value) + "Xóa lọc". Filterable columns are plain columns with at most `FILTER_MAX_VALUES` (80) distinct values; the default is the tab's old facet. The choice resets when switching tabs. Syntax checked with node; not tested in a browser.
- `effects.js`: animation library has filters (frame count, image id used) and a basic/detailed view (detailed adds image ids, dx/dy range and the frame table).
- Effect animations: the frame-table detail was replaced by in-game usage. `Tools/gen_effect_usage.py` scans `Assets/Scripts` for `ServerEffect.addServerEffect*` calls and writes `Assets/StreamingAssets/NsoWiki/effect-usage.json` (per id: file, method, line, enclosing conditions, how it plays; non-literal ids under `dynamic`). `WikiServer` serves that file. The library shows the call-site count (basic) or the full list (detailed), plus a "Sử dụng" filter. Re-run the script after changing effect calls.
- `gen_effect_usage.py` now resolves who triggers each effect: real enclosing conditions (brace/switch aware), innermost mob-id condition -> mob name + level (from NsoWiki/db.json), status-effect type -> effect names, getSys -> element, mounts, Controller packet cases. Rule: effect 59 = default hit of every non-boss mob (server sends attack effect -1). Player class skills do not use nj_effect animations (skill visuals come from SkillPaint), so none are listed.

## 2026-11-12: Quest mechanics up to level 39 (phase 1)
- New `Assets/Server/Net/TaskMechanics.cs`:
  - fixed nodes (Java AkaCave / KugyouCave): khoáng thạch in Hang Aka (map 29) for quest 14 step 1, tinh thể băng in Hang Kugyou (map 64) for quest 26 step 1. They come back 30 s after being picked (`NodeRespawnMs`). Only a player on that step sees or picks them (Java Zone.getItemMaps / Char.pickItem).
  - quest 19 water: taking the quest gives 50 empty bottles (219). Use one standing in waterflow in Hang Ha (map 63); after a 3 s wait (`CatchWaitMs`, SHOW_WAIT/END_WAIT) you get a full bottle (220) and the count goes up. A monster hit during the wait cancels it.
- Offline deviation (user request): quest 11 "NV Bạn hữu tâm giao" is removed entirely: finishing quest 10 goes to 12 (`PlayerTasks.TaskFinish`, two TASK_FINISH), saves on 11 jump to 12 at login (`SkipRemovedTask`, called from `PlayerCombat.Init`). Compile only.
- Wiring:
  - `ItemMapEntry.Fixed/GateTask/GateStep`;
  - `PlayerState.CatchUntil/CatchFailed/CatchDone`;
  - `PlayerService.AddItemMapTo/RemoveItemMapTo/ShowWait/EndWait`;
  - zone tick, MobBrain hit, `UseItem` and `PickItem` hooks;
  - MapInfo hides gated items; they are sent per player.
- Tests: Unity compile clean, 0 Console errors. Not run in Play mode.
- Still skipped up to level 39:
  - quest 12 (needs the item upgrade system);
  - quest 23 (key 231 at Hang Meiro → private Địa đạo Chikatoya instance, 15 min, drop map piece when all mobs die);
  - quest 24 (dig with treasure map 233-235 at a map area; the original sends a rendered map image).
- Also missing systems: buff skills, storage box, selling / throwing items, gems / crystals.

## 2026-11-12: Features up to level 39 (phase 2)

### Completed
- **Quest 12, item upgrade:** new `Net/PlayerUpgrade.cs`, a port of Char.upgradeItem / upPearl / splitItem, Item.next and the GameData tables.
  - Kenshinto menu:
    - "Nâng cấp": Thường (UI 10) or Cẩn thận (UI 31, needs luong, so it is refused offline);
    - "Luyện đá": Yên (UI 12) or Xu (UI 11);
    - "Tách" (UI 13).
  - Controller cmds 19, 20, 21, 22.
  - The quest step advances on equip, as in Java (`OnEquipUpgraded`).
- **Quest 23, map piece:**
  - taking the quest gives key 231;
  - use the key at the Hang Meiro gate (y 432, x 1700–1896) to enter a private copy of Địa đạo Chikatoya (map 78) with a 15-minute timer (MAP_TIME -95). Monsters there do not respawn;
  - when all monsters are dead, a map piece (232) drops at (220, 96). Picking it leaves 3 s, then the player is sent back to Hang Meiro;
  - Matsurugi gives a new key with "Nhận chìa khóa".
  - Code: `World.CreateInstance/RemoveInstance`, `Zone.IsInstance/NoRespawn/InstanceEndAt`, `PlayerActions.JoinZone`.
- **Quest 24, treasure:**
  - taking the quest gives a random map, 233–235;
  - using the map inside its area starts a 3 s dig with a 1-in-3 chance; on success the quest step advances and the player gets item 8;
  - the item info shows a picture drawn from the collision grid with the spot in red (`Net/Png.cs`).
- **Buff skills** (`Net/PlayerBuff.cs`, cmd 74): Raikou 6 (invisible), Hoshitama 15 (hide), Taiyoutama 13 (fire hits), Sogekihei 31 (mana shield), Joutenhitomi 33 (damage up), Totaaigo 58 (dodge).
  - Monsters skip invisible or hidden players.
  - Invisibility makes the next hit critical and then ends.
  - Mana shield moves part of the damage to MP.
  - Not ported: Aoiame 22 (dummy) and the clone 67–72; using them shows a message.
- **Storage box, selling, throwing** (`Net/PlayerStorage.cs`):
  - Kamakura "Mở rương" opens the box (UI 4);
  - cmds 17, 16, 14 and -12;
  - the box is saved in the inventory JSON (`box`).

### Tests
Unity compile clean, 0 Console errors. Nothing was run in Play mode, per the user's compile-only rule.

### Known gaps
- "Chuyển hóa", "Luyện ngọc" and gems (level 40+, luong).
- Coins in the box.
- Buff effects on party members (no party offline).
- The careful upgrade needs luong, and luong is 0.

## 2026-11-12: Phase 2 deviations reverted to Java (user request)
- **Tunnel end:** returns to the saved coordinate (`Progress.SaveMap` + `VillageXY`).
- **Treasure map info:** new `Net/TilePicture.cs`, a port of TileMap.createImage. It draws the exact area (x, y, w, h at the client zoom) from the client tile PNGs `res/x<zoom>/t/tile<id>/<n>.png` with a hand-written PNG decoder. `TileRoot` is set in ServerBootstrap.
  - Limitation: in a player build `Assets/Resources` is not on disk; the files must be copied (for example to StreamingAssets) before release.
- **Taiyoutama:** hit monsters get the burn effect (`Mob.BurnEnd/BurnParam/BurnOwner`). `PlayerCombat.FireTick` (Mob.callFireEffect) takes param HP every half second, with exp and kill credit.
- **Joutenhitomi:** `incrDame2` is in SetAbility (`basicAttack += potentialDame * param / 100`), recomputed when the effect starts and ends.
- **Careful upgrade:** Java code path (+50 % chance, `GoldUp` luong). `Luong()` is still 0, so it fails on the luong check.
- Tests: Unity compile clean, 0 errors.

## 2026-11-12: Rikudou's repeatable quests

### Completed
- New `Net/TaskOrders.cs`, a port of TaskOrder, TaskFactory and Char.orderTaskDay / cancelTaskDay / finishTaskDay / workTaskDay / orderTaskBoss / cancelTaskBoss / finishTaskBoss.
- Rikudou (NPC 25) menu:
  - "NV mỗi ngày": Nhận / Hủy / Hoàn thành / Đi làm NV;
  - "NV Truy bắt Tà Thú": Nhận / Hủy / Hoàn Thành.
- Daily quest:
  - from level 30, 20 per day;
  - kill 20–39 of a random monster within 5 levels of the task-map level closest to the player;
  - reward: 2 × item 251, 20000 yen and 1 uy danh point.
- Beast quest:
  - kill the beast (levelBoss 3) whose level is closest to the player's;
  - 2 per day;
  - "Tà thú lệnh" (item 268) adds one more, at most 2 uses a day;
  - reward: 5 × item 251.
- Messages 96, 97 and 98. Orders are sent again on login.
- Kill counting is in `PlayerCombat.DieBy`.
- Counters and orders are saved in the progress JSON: `dailyDate`, `countFinishDay`, `countLoopBoss`, `countUseItemBeast`, `countDoneTaskDay`, `pointUyDanh` and `taskOrder`.
- The daily reset happens at login when the local date changed (Java: last login on another day).
- The task-map list is built from zone 0 spawns of normal maps, minus `TaskOrders.ExcludedMaps`.

### Changed files
`Net/TaskOrders.cs` (new), `Store/PlayerProgress.cs`, `Net/PlayerTasks.cs`, `Net/PlayerCombat.cs`, `Net/PlayerItems.cs`.

### Tests
Unity compile clean, 0 Console errors. Not run in Play mode.

### Notes
- Not ported: party sharing, clan points, events and the weekly-quest hooks. They do not exist offline.
- Beasts only appear in zones whose id is a multiple of 5, as in Java.

## 2026-11-12: Item audit, levels 1–39

### Method
- Listed every usable item type (22, 24, 27) up to level 39 that a player can obtain offline: xu/yen shops, drops and rewards.
- Compared each against Java Char.useItem, useItemTypeOrder and useItemChangeMap.
- Food, potions, books, equipment and the quest items were already ported.

### Completed
New `Net/PlayerItemUse.cs`:
- 34 / 36 Bách biến lệnh: go back to the saved village. 34 is used up; 36 is not.
- 35 / 37 Khả di lệnh: cmd 12 (Controller), to the 3 schools and 7 villages. Map-unlock checks apply. 35 is used up; 37 only when expired.
- 38 Phúc nang (drop): 1000–19999 yen.
- 251 Mảnh giấy vụn (Rikudou reward): 300 pieces become book 253 or book 252 (menu).
- 252 / 308: +1 skill point (limits 3 and 10).
- 253 / 309: +10 potential points (limits 8 and 10).
- The limits are saved in progress: `limitKyNangSo`, `limitTiemNangSo`, `limitPhongLoi`, `limitBangHoa`.
- `PlayerActions.ResetPoint` is now public.

### Not ported
Each item below is left out because it is luong-only (0 offline), or because it needs a system that does not exist:
- bag expansion 215 / 229;
- "tẩy" licences 240 / 241;
- mount items 444 / 469 (no mounts);
- Thiên biến lệnh 572 (server auto-play);
- dragon balls 222–228 (not dropped);
- Mảnh Quần 736 / 763 (event exchange only).

### Tests
Unity compile clean, 0 Console errors. Not run in Play mode.

## 2026-11-12: Luong (gold) and luong features up to level 39

### Completed
- New `Net/PlayerLuong.cs`.
- **Balance:**
  - `PlayerProgress.Luong` is saved as `luong`;
  - `PlayerLuong.Add` sends ME_UP_GOLD (-71);
  - the balance is shown in the char info (EnterWorld), in ME_LOAD_INFO and in Service.buy.
- **Okanechan:**
  - "Nạp lượng": simulated cards of 10k–500k VND, `TopUpLuongPerVnd` = 1 (offline deviation);
  - "Đổi lượng" → xu (1 : 3000) or yen (1 : 10000), minimum 1000 luong, through the text box (cmd 92 `OPEN_TEXT_BOX_ID`, `PlayerState.InputHandler`).
- **Things luong now pays for:**
  - shop items priced in luong (they come locked, as in Store.buy);
  - revive in place (1 luong);
  - careful upgrade.
- **Items:**
  - bags 215/229/283/801/1094/1158 (Char.expandBag; the number used is derived from the bag size; UPDATE_BAG_COUNT -91);
  - licences 240/241 and the teachers' "Tẩy điểm" (Tiềm năng / Kỹ năng);
  - Nấm linh chi 248 (effect 22, x2 exp for 5 h; type-18 incrExp applied in `PlayerCombat.AddExp(mob)`);
  - Hoàn cốt chi chú 254/255/256 (clears lost exp);
  - 257 (hiếu chiến is always 0 offline).

### Changed files
`Net/PlayerLuong.cs` (new), `Store/PlayerProgress.cs`, `Net/PlayerActions.cs`, `Net/Controller.cs`, `Net/PlayerTasks.cs`, `Net/PlayerItems.cs`, `Net/PlayerService.cs`, `Net/EnterWorld.cs`, `Net/PlayerUpgrade.cs`, `Net/PlayerCombat.cs`, `Net/PlayerItemUse.cs`.

### Tests
Unity compile clean, 0 Console errors. Not run in Play mode.

### Still not ported
These need systems that are missing or are level 40+:
- 269–271 Chuyển hóa;
- 279 teleport to a player;
- 280 caves;
- 340 lucky draw;
- 548 fishing;
- 572 auto-play;
- 705 danh vọng;
- 735/762 and 736/763 fashion pieces;
- 780 mount evolution;
- 444/469 mount fuel;
- the Kamakura coin-limit extension.

Exp effects are not saved on logout; only food is.

## 2026-11-12: More luong features

### Completed
- **Kenshinto "Chuyển hóa"** (UI 33): `PlayerUpgrade.ConvertUpgrade`, NOT_MAP sub -88.
  - Moves an item's upgrade onto an un-upgraded item of the same kind and an equal or higher level.
  - Uses Hoán chuyển 269 (up to +10), 270 (up to +13) or 271 (any upgrade).
- **Thiên biến lệnh 572:** "Bật/Tắt" auto-play (AUTO_ATTACK_MOVE 119), with range and pick-up settings.
  - The pick-up setting is only stored, as in Java.
  - The move speed check is skipped while auto-playing.
  - Auto-play stops when the item is gone (every 5 s, `PlayerItemUse.AutoTick`).
- **Kamakura "Mở rương":** a sub-menu with "Mở rương" and "Mở giới hạn xu".
  - Each step adds 100 million xu of bag limit, up to 3 billion.
  - The fee is (limit − 1.4 billion) / 100 million × 200 luong.
  - `PlayerProgress.XuMax` is saved as `coinMax`; `PlayerItems.AddXu` clamps to it.
- **Cần câu vàng 548:** fish while standing in water. The catch comes after 3 s: stones 7–11, or the line breaks.

### Changed files
`Net/PlayerUpgrade.cs`, `Net/PlayerItemUse.cs`, `Net/PlayerLuong.cs`, `Net/PlayerActions.cs`, `Net/PlayerEffects.cs`, `Net/PlayerTasks.cs`, `Net/Controller.cs`, `Net/PlayerItems.cs`, `Store/PlayerProgress.cs`.

### Tests
Unity compile clean, 0 Console errors. Not run in Play mode.

### Still not ported
- 279 (teleport to a player: there are no other players);
- 280 (caves);
- 340 (lucky-draw wheel);
- 705 (danh vọng quests);
- Jirai/Jumito pieces (collection box);
- mounts 444/469/780;
- the mask box.

## 2026-11-12: Gamepad / keyboard in Trang bị (equipment tab)
- **Fix:** on large screens `setLCR` shows the item info automatically, which caused two problems in Trang bị (`indexMenu` 4):
  - the `isPaintItemInfo` branch of `GameScr.updateKeyUIInforMe` swallowed left/right, so the selection could not move. Left/right now move between equipment slots; up/down still scroll the info;
  - there was no center command, so Enter / Circle did nothing. Enter now runs the left "Chọn" command (11082, `actTrangBiSelect`).
- **Changed file:** `Assets/Scripts/Screens/GameScr.cs`.
- **Tests:** Unity compile clean, 0 Console errors. Not run in Play mode.
- **Follow-up:** up/down in an item's info panel (`GameScr.updateKeyUIInforMe`) used `scrInfo.moveTo`, which centres the row, so the first presses did nothing. Each press now scrolls by one line with `Scroll.ScrollBy` (new, `Assets/Scripts/UI/Components/Scroll.cs`). Compile clean; not run in Play mode.
- **Follow-up 2:** in Trang bị, left on the first slot or right on the last slot (15) returns to the tab title (`TrangBiBackToTitle`) so left/right switch tabs again. Compile clean; not run in Play mode.
- **Follow-up 3:** Thú cưỡi tab (indexMenu 5) now works like Trang bị on large screens: item info shown automatically, left/right walk slots 0..4 and return to the title at either end, up/down scroll the info, Enter / Circle opens the "Tháo ra" menu (1516). Up arrow (24) also accepted (original checked only NUM2). Compile clean; not run in Play mode.
- **Follow-up 4 (all tabs of the character panel):** left/right always pass through an open item info; every tab returns to its title at either end instead of wrapping (Hành trang, Kỹ năng, Trang bị, Thú cưỡi, Vĩ thú list/skills); Tiềm năng, Thông tin and Vĩ thú points return to the title on left/right. Vĩ thú accepts the arrow keys (original: number keys only), auto-shows item info on large screens and Enter opens "Tháo ra" (508). In Hành trang an open info panel follows the selection (`BagInfoFollow`). Compile clean; not run in Play mode.

## 2026-11-12: Skill bar (5 screen slots + 3 hot keys) saved
- The server ignored SAVE_RMS (-67) and always sent empty bars on enter, so the 5 skill slots at the bottom stayed empty.
- Now ported `Char.saveRms`: `PlayerSkills.SaveRms` keeps `OSkill`/`KSkill`/`CSkill` (learned skills only) in `PlayerProgress` (`oSkill`/`kSkill`/`cSkill`), and `EnterWorld` sends them back. An empty array lets the client fill its default bar (`GameScr.loadDefaultOnScreenSkill`).
- Assign in Kỹ năng tab: select a skill → "Gán" (center, Enter/O) → "Ô kỹ năng" → slot 1-5.
- Files: `Store/PlayerProgress.cs`, `Net/PlayerSkills.cs`, `Net/PlayerService.cs`, `Net/EnterWorld.cs`, `Net/PlayerTasks.cs`, `Net/Controller.cs`.
- Compile clean, 0 errors. Not run in Play mode.
- Follow-up: the bar shows once the first book skill is learned (`GameScr.hasBookSkill`), and the assign menu lists all 5 slots. Compile clean.
- Follow-up: on PC the bar is on the potion row, no index numbers, no cooldown seconds. Compile clean.

## 2026-11-12: Umayaki at the schools
- Ported `Char.npcUmayaki_2` (NPC 8) in `Assets/Server/Net/PlayerTasks.cs` (`NpcUmayaki2`): teleports to Hirosaki (1), Haruna (27), Ookaza (72) plus "Nói chuyện" (NV chào làng step 5). Compile only.

## 2026-11-12: skill bar cooldown overlay after skill changes
- Bug: after "+" (upSkill), learning a book, tẩy kỹ năng or the basic-attack sync on level up, the client reloads its Skill objects but the 5 screen slots kept the old objects, so no cooldown overlay showed. Java resends the bars after loadSkill (`Char.upSkill` / `useBookSkill` / `tayKyNang`); ported as `PlayerService.SkillBars(me)` called after those `LoadSkill` calls (PlayerSkills, PlayerLuong, PlayerCombat). The cooldown overlay itself was never removed: only the red seconds are hidden on the bar. Compile only.
- Fix: "Bạn có muốn bỏ vật phẩm này không?" stayed open after "Có"; `GameScr.perform` case 2001 now calls `GameCanvas.endDlg()` before `throwItem`. Compile only.

## 2026-11-12: NV Thách đấu (quest 13)
- New `Assets/Server/Net/Gymnasium.cs`: "Vào phòng thi đấu" at Ookamesama / Kazeto / Toyotomi (school, quest 13, matching step) opens a private copy of Nhà thi đấu Ookaza (56) / Haruna (0) / Hirosaki (73) for 300 s. The headmaster is a bot char (id -11111, PLAYER_ADD) with 1000 HP, damage 900-1000, two level-1 skills; it chases within its area (MoveWithinCustom) and hits with the PvP formula. The map's headmaster NPC is hidden (status 15) until the bot dies, then shown again (NPC_PLAYER_UPDATE). Bot death advances the step; time out sends the player back to the school.
- Wiring: Controller case 61 (PLAYER_ATTACK_PLAYER), GameServer tick (`Gymnasium.UpdateZone` before TaskMechanics), EnterWorld.MapInfo NPC status, PlayerTasks.NpcTeacher menu, Cmd constants PLAYER_REMOVE/ADD/ATTACK_PLAYER/HAVE_ATTACK_PLAYER.
- Compile only (Unity clean). Not tested in Play mode: check the bot appears, can be focused/attacked, hits back, and the step advances.
- Fix (black screen entering a gym): the client has no `Resources/res/map` file for maps 0, 56, 73 (and 78) and asks the server with REQUEST_MAPTEMPLATE (notMap -109), which was unhandled. `Controller.SendMapTemplate` now answers with the tiles of the current map (Java Service.requestMapTemplate). Compile only.
- Fix (black screen at login, NRE "message error"): a character saved inside a gym/tunnel instance (maps with no zones) got a null zone in `EnterWorld.Send`. It now goes back to the gym's school (`Gymnasium.SchoolOf`) or Lang Tone. Controller errors now log the command and full stack trace. Not compiled yet (Editor was in Play mode).
- Fix (no way out after the fight): inside a gym, talking to the NPC now shows only "Rời khỏi nơi này" (Java Char.initMenu, isNhaThiDau), which returns to the map the player came from (`Gymnasium.Leave`). Compiled (Unity clean, together with the two fixes above).


## 2026-11-12: fall death in Cửa biển Kawaguchi (map 61), fixed
- Report: after walking off a ledge in map 61 the character fell through the ground and died.
- Cause (found with temporary [diag] logs, now removed): client `Char.cyStartFall` was stale (401, from an earlier jump). `updateCharFall` ignores every T_TOP row above cyStartFall, so the character fell to the bottom row (y 312) and the server killed it (isCrossMap, cy >= pxh - 24). The original client only cleared cyStartFall on one of its two landing paths.
- Fix: `Char.changeStatusStand` clears cyStartFall (offline client fix, Assets/Scripts/Gameplay/Entities/Char.cs). Compile only.

### Rare black screen on map change (fixed)
- Cause: on entering a waypoint the client sends MAP_CHANGE and locks on a black screen until MAP_INFO or RESET_POINT. `PlayerActions.RequestChangeMap` returned silently when the server position (one move behind) was not inside the waypoint, so the screen stayed black.
- Fix: RESET_POINT on every no-change path of RequestChangeMap / ChangeMap (Assets/Server/Net/PlayerActions.cs). The client snaps to the server position and walking on retriggers the change. Compile only.

### Player log (debug, user request)
- New `Assets/Server/Net/PlayerLog.cs`. Hooks: Controller.OnMessage (IN), Session.Send (OUT), GameServer.Log (LOG), GameServer tick (STATE + flush), PlayerActions JoinZone / StartDie / SaveAndLeave / RequestChangeMap (EVT). Message now keeps its received payload (`Message.Data`).
- Settings: NsoServerControl "Player log" header; ServerBootstrap opens the file before server.Start and closes it on shutdown. The current file path shows in NsoServerControl.playerLogFile.
- Output: `<project>/Logs/PlayerLog/player_yyyy-MM-dd_HH-mm-ss.log` (git-ignored). Only messages that reach the server are seen; client-only UI actions are not.
- Compiled clean in Unity (also compiles the cyStartFall and RESET_POINT fixes). Not run in Play mode.

### Death while walking on the bottom floor (maps 61, 63), fixed
- Found with the player log: map 63 walking at y 336 (= pxh 360 - 24) died at x 147. The last tile row of maps 61 and 63 is all T_TOP ground; Java's isCrossMap bottom rule (cy >= pxh - 24) killed anyone standing on it, except over water tiles.
- Fix: PlayerActions.IsCrossMap skips the bottom rule when the tile at (cx, cy) is T_TOP. Compile only.

### /goto lands at the map entrance
- QuestTravel.GoToObjective: new EntryPoint() picks the arrival x/y of a waypoint whose Next is the quest map (from the current map if possible, else the lowest map id), then LocationStand[0]; old NPC/quest-spot/monster positions are fallbacks. DevTerminal help text updated. Compile only.

## 2026-11-12: NV Đưa Jaian trở về (quest 17 step 1)
- Bug: Jaian (NPC 17, Rừng Moshio map 33) is status 15 (hidden) in the map data, so the player could not find him. Java Service.mapInfo shows him (status 1) on quest 17 step 1.
- New `Assets/Server/Net/Escort.cs`: Jaian NPC status per player (EnterWorld.MapInfo now takes the player), Char.initMenu JAIAN ("Đi nào" + his plea), JaianMove (bot id -playerId, 2000 HP, walks 36 px left every 3 s; off the left edge = TaskNext), escortFailed (death, leaving the zone, 500 px away). `Gymnasium.FakePlayerAdd` is the shared PLAYER_ADD for bots. GameServer tick calls `Escort.UpdatePlayer`. PlayerTasks: Jaian menu case, Jaian in HasOwnTalkHook (no generic instant-advance entry).
- Monsters notice and hit Jaian like a player (MobBrain: `Escort.BotsIn`, `FindBot`, `HitBot`; NPC_ATTACK_PLAYER, PLAYER_DIE at 0 HP), 0 HP = escort failed.
- Offline deviations: failure sends the client's SUB_COMMAND -90 (reason 1 too far, 2 Jaian injured); the client draws "do <name> hộ tống" in yellow (`Assets/Scripts/Gameplay/Entities/Char.cs`, user request).
- Compile only (Unity clean). Not tested in Play mode: take quest 17 step 1, go to Rừng Moshio (x 3180), talk to Jaian, "Đi nào", follow him to the left edge (~4.5 min), then report to Bà Rei.



## 2026-11-12 - Wide popup command alignment
Completed source change: GameScr.cs derives left/centre/right bottom command X positions from wide popup bounds and Paint.CmdButtonW; NsoUiLayout.cs exposes popupSideInset. Updated UiReference and wiki notes/config/deviation plus status/task/decision/handoff docs. No server, web animation/filter, gameplay or save changes in this task. Unity refresh and Console validation passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/browser or automated runtime tests, per compile-only user preference. Validation commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; check unity command console status. User opens inventory and related wide panels to compare borders with buttons; narrow layouts intentionally retain legacy spacing.


## 2026-11-12 - World map commands and mouse input
- User explicitly requested removal of the Change button and its feature. MapScr is world-map only: removed action 1001, modeCurrentMap, local-map rendering/markers, local-map scrolling bounds and unused xM/yM fields. Shared gameplay minimap resources remain in use elsewhere.
- Offline deviation from original MapScr: retain Go To (14004) and Close (1000); synchronize command positions and image-sized hitboxes before input and paint. Reserve the actual bottom command row (Paint.CmdY minus Command's 3-pixel hit padding) from map dragging/selection; only map-origin gestures select a destination. Short drags now use absolute displacement on both axes.
- Changed: Assets/Scripts/Screens/MapScr.cs; PROJECT_STATUS.md, TASKS.md, DECISIONS.md, HANDOFF.md, Assets/Documentation/UiReference.md and Assets/StreamingAssets/NsoWiki/notes.json. No architecture/server/save changes.
- Validation: Unity refreshed and compilation completed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/mouse runtime tests per user compile-only instruction. Runtime visual/input verification remains pending.
- Next: user opens the map and checks only Go To/Close are present; clicks across each button image including edges; checks map dragging, destination selection and Go To. Compile commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Item sprite project paths
- Item records now expose spritePath: an existing project-relative PNG under Assets/StreamingAssets/NsoStatic/Data/Img/Small, resolved by icon ID with zoom 4-to-1 preference. Missing art returns null. Runtime save paths are not exposed.
- Item detail in the workbench shows the path with a copy button and a clear missing-file message. Read-only metadata; gameplay and sprite rendering are unchanged.
- Changed: Assets/Server/DevWiki/WikiData.cs, Assets/StreamingAssets/NsoWiki/app.js; project status/task/decision/handoff docs, ServerPort.md and notes.json.
- Validation: app.js node --check passed. Unity refreshed and compile/Console passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/browser tests per user compile-only preference.
- Next: start Play, refresh workbench data, open Items > any item, inspect Sprite in project and copy the path into the IDE. Compile: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Item sprite label encoding fix
Corrected corrupted Vietnamese copy-path and missing-sprite labels in NsoWiki/app.js. Cause: Unicode literals passed through the PowerShell/Python pipe were replaced by question marks. This repair uses ASCII Unicode escapes with explicit UTF-8 output. JS syntax and exact label checks passed; no C# changes or Play/browser tests. Next: reload the web page to verify both labels.


## 2026-11-12 - All item sprite zoom paths
- Item metadata now includes spriteFiles for zoom 1-4, each with project-relative path, absolute fullPath (explicit user request), existence and PNG-header dimensions. Legacy spritePath remains the preferred highest existing zoom for compatibility.
- Item detail lists every zoom with full path and a per-file copy button; explains that gameplay zoom can differ from web preview and cached game icons need refreshing after replacement. Missing variants are explicit. Reads only 24-byte PNG headers; no texture decoding or image/metadata edits.
- Changed: Assets/Server/DevWiki/WikiData.cs; NsoWiki/sprites.js, app.js, app.css; project status/task/decision/handoff docs, ServerPort.md and notes.json.
- Validation: node --check passed for app.js and sprites.js. Unity refresh/compilation passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/browser tests per user compile-only instruction.
- Next: start Play and refresh the workbench data, open an item, check zoom 1-4 sizes/full paths and copy each path. Small1131 sizes observed read-only: 16x14, 32x28, 48x42, 64x56. Gameplay zoom was observed as 3 before edits; zoom 4 edits alone do not affect zoom 3 art.
- Validation commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Small1131 original zoom restoration
- Compared all four Small1131 PNGs against Git HEAD. Original dimensions: zoom 1=16x14, 2=32x28, 3=48x42, 4=64x56. Only working zoom 4 differed (48x42).
- Per explicit user request, restored only Assets/StreamingAssets/NsoStatic/Data/Img/Small/4/Small1131.png from HEAD. Preserved the edited version outside Unity at C:/Users/PC/.codex/backups/nso-Small1131-20261112-193205/zoom4-edited.png. All .meta files unchanged.
- Read-only pixel inspection: zoom 1 already has 50 transparent pixels; zooms 2-4 have opaque white backgrounds. Background removal is pending the user's choice of deterministic pixel processing or AI extraction; no removal performed yet.
- Validation: binary Git comparison and PNG dimensions/pixel alpha inspection only. No C# changes, Play tests or gameplay changes. Next: finish background removal using the user's selected method, retain dimensions, verify foreground pixels and alpha, then update completion evidence.


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


## 2026-11-12 - Gradual HP recovery traced to equipped weapon
- Follow-up investigation (no gameplay edits): user clarified gradual HP gain and asked to check hidden/miscalculated food. Read-only stopped-Editor inspection: natural regen=0, retained HP=294/355, vEff empty, eff5BuffHp=0, autoUpHp=0, auto HP/food disabled. No active server snapshot was available because Play was stopped.
- Read-only current players.tsv inspection found no foodEffect, but equipped item 94 (Bach Hac Kiem), upgrade +4, has option [30,5]. Dev wiki itemOptions[30] confirms the +4 every-five-seconds HP recovery option, type 3; AddOptions enables type 3 at +4, and PlayerEffects.UpdateHalfSecond adds Options[30]+Options[120] every 5000ms. This explains gradual +5 HP/5s without consuming a new item. No save data changed.
- Food review: saved food restores only before UTC expiry; effects expire before server recovery; server ticks food every 500ms while legacy client predicts every 780ms, so both sides write displayed HP and can cause visual correction while food is active. This is a separate code-level risk, not evidence that food caused this save's recovery. No speculative food/timing rewrite applied.
- Preserve legitimate equipment recovery under the existing no-recovery-without-a-source decision. Next: user unequips item 94 and compares HP while idle with no food/other recovery gear; decides separately if equipment regeneration should be changed as a balance rule.
- Changed documentation only: PROJECT_STATUS.md, TASKS.md, DECISIONS.md, HANDOFF.md and wiki notes. No source changes this turn; no new compile needed, no Play/automated gameplay tests run, per user preference. Last code compile remains the previous clean Unity result. Evidence: current save inventory plus NsoWiki/db.json item 94/itemOption 30 and PlayerCombat.AddOptions/PlayerEffects.UpdateHalfSecond.


## 2026-11-12 - Stronger monsters and reduced player dodge
- User approved legitimate equipment recovery and requested harder combat. Shared CombatRules defaults now use dodgeScalePercent=50 (was 100), maxDodgeChancePercent=40 (was 100), mobDamagePercent=150 (was 100); mobAccuracyScalePercent stays 100. This supersedes earlier default profiles. Effective dodge points are halved, not final dodge probability; the final probability is capped at 40%. Positive monster HP damage is 1.5x the previous default before rounding.
- Defaults affect all ordinary monster hits and Workbench Reset through the same constructor. Existing saved overrides remain supported; no combat-rules.json existed in the current Editor save directory at inspection, so next Play uses these defaults. No character/save stat edits, source recovery changes, monster HP/EXP or PvP changes. Web help updated to actual defaults and matching displayed/actual damage.
- Changed: Assets/Server/World/CombatRules.cs, Assets/StreamingAssets/NsoWiki/app.js, Assets/Documentation/CombatBalanceConfiguration.md, ServerPort.md, root status/task/decision/handoff docs and wiki notes. Offline balance deviation from Java requested by user; no architecture change.
- Validation: node --check app.js passed; Unity compile/Console passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:22:24Z). Diff check passed. No Play/gameplay tests per user compile-only preference.
- Next: user starts Play, verifies Workbench values 50/100/40/150 and compares hits/dodges against the same monsters. Adjust further via web if desired. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json; node --check Assets/StreamingAssets/NsoWiki/app.js.


## 2026-11-12 - Main quest objective teleport /goto
- User deferred further resistance/monster balance changes; previous CombatRules remain unchanged. Added /goto (also in /help) to travel to the current main-quest objective, not advance the quest explicitly. Before acceptance this means the quest giver; active quests use task.maps[step+1] and existing TaskNpcId class-school resolution. Report steps therefore return to their NPC.
- New focused QuestTravel resolver: NPC position first, then mechanic-owned collection/water/treasure locations, then required kill/drop mob spawn, then designated map standing location. Kill steps with map=-2 prefer a suitable current map, otherwise the lowest-ID normal map with that spawn. Unknown/no-fixed objectives return a reply without moving.
- Reuses SetXY/ResetPoint/ChangeMap and normal entry hooks; exploration steps may advance naturally upon entering their objective map. No TaskNext/FinishTask, auto-accept, auto-fight, reward or inventory edits. Dead players and active catch/fishing actions are refused. Special/private maps retain NPC/item entry requirements. Dev terminal teleport intentionally bypasses walking/map-unlock routing on supported normal maps.
- GameWorld now loads task.maps alongside task.npcs and exposes read-only lookup by convention; original static data unchanged. TaskMechanics owns special positions so the terminal does not duplicate quest coordinates. No new package, scene or save schema.
- Changed: Assets/Server/Net/QuestTravel.cs (+ Unity meta), DevTerminal.cs, TaskMechanics.cs, World/GameData.cs, World/World.cs; four root docs, docs/ARCHITECTURE.md, ServerPort.md and wiki notes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:31:14Z). Diff check passed; Unity generated QuestTravel.cs.meta. No Play, terminal execution or save mutation, per user compile-only preference.
- Next: user checks /goto before quest acceptance, NPC/report, kill/drop quests, collection/water maps, same-map reposition, class-specific school, missing/final objectives and dead-state refusal. Special-map replies should preserve entry mechanics. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


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

## 2026-11-12 - Tajima menu ported (Char.npcTajima)
- Menu: "Hủy vật phẩm và nhiệm vụ" (Char.clearTask, level 10+), "Nói chuyện", "Nhận kiếm gỗ" (only if the character has no wooden sword). Invite toggle and "Thứ thân" omitted offline (see DECISIONS).
- File: Assets/Server/Net/PlayerTasks.cs. Compile only (clean); user tests in Play mode: at level < 10 the refusal line; at 10+ the quest goes back to "not taken", quest items vanish, the giver offers the quest again; second "Nhận kiếm gỗ" says "Con đã có kiếm gỗ rồi."

## 2026-11-12 - Skill animation too short (fixed, compile only)
- Cause: Char.paintCharWithSkill advanced the animation on every paint (60-144 fps) instead of every logic tick (24/s).
- Fix: Main.LogicTick + per-tick gating in Char.paintCharWithSkill (wrapper around paintCharWithSkillStep). Test: use several skills (melee, arrow/ranged, buff) and watch other players' skills; length should match the original; hits and arrows land once.
