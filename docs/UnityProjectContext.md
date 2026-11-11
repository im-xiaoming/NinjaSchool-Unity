# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Scope and baseline
- Analyzed 2026-11-11, commit `3e740918a01e0b9dce5a0ea05c220b97029c0570`, branch `main`.
- Active root: `C:/My project`. `C:/NSO/docs` does not exist. Original `C:/NSO` and history/reference `C:/NSO_OFFLINE` remain read-only.
- User requested understanding and will assign the next task. No gameplay task was started.
- This is a representative source audit, not proof that every code path is correct or every Java behavior has been ported.

## Environment and boundaries (confirmed)
- Unity 6000.6.8f1 (237b6593792b); Windows single-player target.
- URP 17.6.0: GraphicsSettings has no default pipeline, but every quality tier references `Assets/Settings/UniversalRP.asset`.
- Input mode Both (`activeInputHandler: 2`), Input System 1.19.0 installed; legacy Input/Event APIs are used in Main.
- Unity Test Framework 1.8.0, uGUI 2.6.0, Unity Pipeline 1.0.0-pre.1 are installed. Presence is not proof of gameplay usage.
- Gameplay/client/server use the default Assembly-CSharp boundary. Only the Welcome content has an asmdef; no first-party game test asmdef was found.
- `Tools/ServerTests/ServerTests.csproj` compiles the same server sources except ServerBootstrap, targeting net8.0. Installed SDK used for this audit: 10.0.101.

## Structure and startup (confirmed)
- Client: `Assets/Scripts`, organized by responsibility with global types; preserve names and meta GUIDs.
- Embedded server: `Assets/Server`, namespace NsoServer, no UnityEngine dependency except ServerBootstrap.
- Static game data: `Assets/StreamingAssets/NsoStatic`; 25 static tables and captured protocol blobs.
- Dev wiki: `Assets/Server/DevWiki`, `Assets/StreamingAssets/NsoWiki`, generated `NsoWiki/db.json`.
- Build Settings enables `Assets/Scenes/NinjaGame.unity` first AND legacy `Assets/testNj.unity` second. Do not remove either without a task.
- ServerBootstrap starts AfterSceneLoad using NsoServerControl paths; Session_ME connects using GameServer.Connect and LocalPipe.
- Game traffic retains original binary framing/XOR through in-memory streams. The debug wiki alone uses loopback HTTP, ports 8787 through 8796.
- The 500 ms server tick and Controller messages share GameWorld.SyncRoot. Tick handles respawn, mob effects, expired drops, player recovery, mob AI.
- Main uses a legacy update/paint loop; NsoGameLayer redirects paint into a RenderTexture with optional FXAA. Map overlays are visual only.
- Local save is users.tsv/players.tsv. PlayerItems.Save updates PlayerRecord.Data in memory; PlayerActions.SaveAndLeave calls Accounts.Flush. Crash durability is not established.

## Completed versus partial work
- Confirmed handlers and tests exist for sign-in/character flow, walking/maps/zones, combat/exp/respawn, bag/equipment/consumables, xu/yen shops, drops/pickup, skill learning/points/passives, monster effects, quest/NPC flow and JSON progress.
- Quest data covers IDs 0-42; unsupported mechanics are explicitly skippable via PlayerTasks.IsStepSupported. This is not a complete port of all quest mechanics.
- Buff/NPC/live skill types 2/3/4 can be selected but AttackMonster returns without applying them. Java Char.useSkillBuff remains a future source reference.
- Item upgrade/gems/crystals/box/sell/throw, mounts/pets/clone/PvP/special maps and multiplayer-style functions remain future work.
- Wiki has 17 collections, 43 quests, 48 NPCs, 179 maps, 261 mobs, 1240 items, 23 shops and 91 skill templates in the inspected export. Counts describe data, not completed behavior.
- MenuAudio.menuMusicLoop is false and tracks one play per menu visit.

## Validation performed during onboarding
- Built current server/test sources into `%TEMP%/nso-onboarding-20261111` using existing NuGet assets, without downloading or modifying tracked build outputs.
- Build: 0 errors; existing warnings CS0219 (`bad`) and CS0414 (`Controller.zoomLevel`).
- combatcheck: 6 PASS; questcheck: 16 PASS; progresscheck: 12 PASS; all exited 0.
- Default suite: 18 PASS, 2 FAIL, exit 1. Failures are the documented obsolete login expectations (wrong password and invalid username) under automatic local sign-in.
- Live Console read for C:/My project: groundTruth.compilationFailed=false, compiling=false, consoleErrors=0. Historical collector counts include 42 errors and are not current groundTruth.
- No refresh/reimport, Play mode, manual gameplay, Unity tests, capture comparison, wiki regeneration or Windows build was initiated.

## Tooling
- CLI exists at `C:/Users/PC/AppData/Local/Unity/bin/unity.exe`, but is absent from this shell PATH. Pass full path and explicit --project-path (multiple Editors are open).
- Working read: `& 'C:/Users/PC/AppData/Local/Unity/bin/unity.exe' command console status --project-path 'C:/My project' --format json`.
- This installed CLI rejects --caller/--skill on console; retry without them succeeded. No installation/update was performed.
- Unity MCP tools are not exposed in this chat. CLI Console access is available; scene/gameobject/profiler/test capabilities remain unverified.

## Constraints and discrepancies
- Preserve B2 embedded C# server, in-memory transport, original protocol, fixed local account, five-zone cap, original art/fonts/title style and visual-only overlays.
- Preserve multiplayer-style protocol/features unless the user asks; no unapproved downloads, large original asset copies, gameplay deletions or architectural changes.
- Keep Java method references in port comments; record intentional offline deviations in DECISIONS and wiki notes. Server aliases must account for global client Math/Mob/Zone types.
- UiReference section 4 still describes looping menu music; actual code and newer handoff say once. Recorded here; source/reference document left unchanged.
- The overlay plan still says configuration work awaits approval, while later Inspector documentation and current components show implementation. Do not treat the old plan as an instruction to redo that work.
- Generated wiki dirty state at entry differs from HEAD only in generatedAt (22:26:47 to 22:34:10). Preserve it.
- Tools/ServerTests bin/obj files are already tracked; isolate future audit builds or review their diff carefully. No cleanup was performed.
- Full manual quest/shop/combat/skill flow and Windows release restart persistence remain unverified. The old build script targets the predecessor.

## Next step
Wait for the user's task. Existing backlog first suggests manual real-client quests 0-5, shops, drops and school learning; buff skills follow. Do not begin automatically in this onboarding session.

## Evidence inspected
AGENTS; PROJECT_STATUS; TASKS; DECISIONS; HANDOFF; docs architecture/testing; all Assets/Documentation Markdown; README; overlay plan; older DECISIONS, protocol/online-removal and handoff summary; project/package/build/quality settings; GameServer; ServerBootstrap; Controller; LocalPipe/Session_ME connection; PlayerTasks; PlayerSkills; PlayerEffects; PlayerCombat; PlayerItems; PlayerProgress; PlayerActions.SaveAndLeave; AccountStore.Flush; WikiServer; MenuAudio; Main/NsoGameLayer; diagnostics sample; test project and test cases; representative Java Char methods.

<!-- unity-onboarding:generated:end -->

## Follow-up: quest 2 timing defect (2026-11-11)
The reported inability to hit dummies was reproduced in the live client and fixed in Char.updateSkillPaint/setAutoSkillPaint. Render-driven animation indices crossed the exact hit frame between logic ticks; once-per-cast crossing detection now runs before cleanup. Regression 8/8, server questcheck 16/16, live dummy kill and quest progress 0 -> 1 confirmed. See the latest HANDOFF checkpoint; the original onboarding-only scope above is historical and the user has now authorized this bug fix.

## Follow-up: local Workbench (2026-11-11)
Wiki is now a local control/lookup workbench. LootRules is immutable per world, loaded from save-folder game-rules.json at startup; default behavior preserved. WikiControl exposes validated rule saves and readonly runtime/loot/history snapshots. UI app.js/app.css preserves deep links. API checks 27/27, pure UI 18/18; nearby gameplay tests and Unity current Console clean. Browser automation unavailable: visual/click/mobile QA pending. Read latest handoff/architecture before changing persistence/control routes.
