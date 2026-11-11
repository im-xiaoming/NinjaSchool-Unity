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
