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
- Limits: no quick-item key, so Square/R3/Create are unused. Cross decides jump vs confirm at press time with `InWorld()`.
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
