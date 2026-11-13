# TASKS.md

**Legend:**
- `[x]` done and verified as stated.
- `[~]` done in code; only compiled or tested automatically.
- `[ ]` not done.

Older completed tasks (online removal, title menu, fonts, mob art, performance pass, zone cap and others) are listed in `C:\NSO_OFFLINE\TASKS.md`.

## Completed (this project, 2026-11-11)
- [~] **Combat port:**
  - attack monsters (cmd 60) with damage, critical, miss and exp;
  - level up from the `others.exp` table;
  - monster respawn;
  - weapon requirement.
  - Tests: `combatcheck` 6/6.
- [~] **Skills:**
  - learning from books, skill points, potential points, selecting a skill, passive support options;
  - effects on monsters: burn (7/16 and fire skills), freeze (25/34, ice), stun (43, wind), bind (24), disable (40), Kakyuu sushi (4).
  - Tests: `progresscheck`.
- [~] **Items and shops:**
  - use, equip, unequip, potions, food, item info;
  - shops with xu and yen (needs task >= 3);
  - drops (quest items, yen, random items, equipment), pickup, 30 s expiry.
  - Tests: `questcheck`, `progresscheck`.
- [~] **HP/MP over time:** food, potions and equipment options. Unsourced offline natural recovery is disabled.
- [~] **Quests 0–42 and NPC menus:**
  - talk, kill, pick and level steps;
  - the quiz;
  - the wooden sword;
  - school admission;
  - teleports;
  - "Bỏ qua" (skip) for steps that are not ported.
  - Tests: `questcheck` 16/16 (quests 0–3), `progresscheck` 12/12.
- [~] **Progress save** in `PlayerRecord.Data` (`Store/PlayerProgress.cs`).
- [x] **Dev wiki:**
  - a website that starts with the game at 127.0.0.1:8787, with search, filters, record details and links;
  - a JSON API at `/api` for agents;
  - `NsoWiki/db.json` export;
  - the `wiki` test command.
  - Verified in Play mode, with curl and in the browser.
- [x] **Menu music** plays once per visit to the menu (`MenuAudio.menuMusicLoop = false`). Verified in Play mode.
- [x] **`README.md`** written.
- [x] **`walkcheck` repaired** for the in-memory pipe (`TestClient.HasMore`). walk_A, walk_C and walk_D match Java; walk_B differs only by the 5-zone cap.
- [x] **Handoff documents** created in `C:\My project`: AGENTS, PROJECT_STATUS, TASKS, DECISIONS, HANDOFF, `docs/`.

## Remaining (suggested order)
- [ ] Play mode: watch AI players from the player client (smooth walking, no left/right turning); confirm rules restock potions; let the 256-wide RL model clone and pass evaluation (2026-11-13).
1. [ ] **Play the new gameplay by hand in the real client** (Editor Play mode):
   - create a character;
   - play quests 0–5;
   - buy in a shop;
   - pick up drops;
   - learn a skill at school.
   Fix protocol issues found. Ask the user for screenshots of anything that looks wrong.
2. [ ] **Buff skills** (skill types 2/3/4): `Char.useSkillBuff`.
3. [ ] **Unported quest mechanics** that are currently skipped: dungeons, fishing, carrying water, daily tasks, friends, upgrade, battlefield. The steps per quest (`status: skip`) are in the dev wiki `quests` collection. Jaian escort ported 2026-11-12 (`Net/Escort.cs`).
4. [ ] **Item upgrade, gem and crystal windows; storage box; selling and throwing items.**
5. [ ] **Mounts, pets, clone skill, PvP.**
6. [ ] **Special maps:** dungeons, wars, events. Port them only when the user asks.
7. [ ] **Full English localization** of the gameplay text.
8. [ ] **Windows release build of `C:\My project`:**
   - adapt `C:\NSO_OFFLINE\tools\build_and_run.ps1` (source path, scene `Assets/Scenes/NinjaGame.unity`);
   - build;
   - run offline;
   - check that save and load survive a restart.
9. [ ] **Profile** release performance on the target PCs.
10. [ ] **Clean-ups:**
    - the dead auto-play code;
    - `ReLogin.cs`;
    - the unused `bad` local in `Tools/ServerTests/Program.cs`;
    - the unused `Controller.zoomLevel` field.

## Awaiting user decision (do not act without asking)
- Multiplayer-style features (friends, party, clan, trade, auction, ranking, mail, gift code) are KEPT in the protocol by the user's choice. Do not port or remove them unasked.
- **Luong (gold):** currently 0. Items priced in luong cannot be bought, and reviving in place needs luong.
- **Resolved: natural regeneration:** user requested no unsourced recovery; default is 0. The former 2 %/5 s addition is superseded.
- **Number of save slots:** currently one local account with up to 3 characters.
- **Copying more image folders from `C:\NSO`** (for example effects, about 111 MB) needs approval.

## Onboarding checkpoint (2026-11-11)
- [x] Review handoff, representative source, Git and current server-test baseline; recorded in `docs/UnityProjectContext.md`.
- No gameplay backlog item was completed or started. The user will assign the next task.

## Quest 2 bug follow-up (2026-11-11)
- [x] Reproduce and fix client hit dispatch skipped at high render FPS; verify normal client attacks kill a dummy and advance quest 2 to 1/10.
- [x] Add `Tools/verify_skill_hit_frame.cs`: 8/8 timing/once-per-cast/death regression checks in Play mode.
- [ ] User keyboard/mouse replay of the remaining quest-2 kills; full real-client quests 0-5/shop/school verification remains pending.

## Loot bug follow-up (2026-11-11)
- [x] Exclude training dummies and mobs at least 10 levels below the killer from all loot paths, per clarified user request.
- [x] Add lootcheck: nine boundary/config/blocked-path checks; questcheck and progresscheck remain passing.

## High-FPS effect presentation (2026-11-11)
- [x] Smooth floating damage numbers, hit splash motion and F1 menu rows between ticks; tween visible mob HP-bar width.
- [x] Recent menu reopening now has a configurable minimum opening; selection waits for the popup to appear. Avoid per-caption temporary arrays in menu paint.
- [x] Play checks: verify_effect_presentation 17/17, verify_skill_hit_frame 8/8, runtime sampling at about 120 paints/s, current Console clean.
- [ ] User visual confirmation during real combat and repeated F1 use; limited source sprite poses were not replaced or synthesized.

## Workbench upgrade (2026-11-11)
- [x] Web switches for dummy/overlevel exclusions, cutoff and separate ordinary-loot level gate; explicit save, revision conflict, persistence and defaults as a draft.
- [x] Live player/quest/map/bag overview and loot-condition inspector using actual policy.
- [x] Responsive UI, full details, quest timeline, cross-links, search/filter/paging, browser-local bookmarks/recent, JSON copy/export, config history and read-only API tool.
- [x] API/persistence/integration checks 27/27 and pure UI checks 18/18; nearby gameplay tests and current Unity compile pass as documented.
- [ ] Browser visual/click desktop/mobile checks (automation host unavailable); release-player saved-policy check.

## Recovery correction (2026-11-11)
- [x] Disable unsolicited natural HP/MP recovery; default NaturalRegenPercent=0 supersedes the former 2% addition.
- [x] Add regencheck 9/9: source-free boundaries, food, potion, equipment intervals, expired food, caps and death; nearby quest/progress tests pass.

## Movement release correction (2026-11-11)
- [x] Reproduce release inertia: speed 8, 9 ticks at 24 Hz, 36 extra pixels.
- [x] Stop grounded manual running on final horizontal release; retain held aliases, destination paths, airborne and remote movement. Configurable Instant Movement Stop.
- [x] Play regression 18/18 and repeated baseline probe: zero residual velocity/ticks/distance; Unity compilation/current Console clean.
- [ ] User confirmation with physical keyboard and on-screen movement buttons; no Windows build performed.

## Broad smooth-motion request (2026-11-12)
- [x] Preserve gameplay tick rate; render cap configurable from 60 to 144.
- [x] Add fractional camera/entity/item/projectile/pet/weather and list-content painting, scoped/restored placement, map/teleport/recycle guards.
- [x] Play checks: 94 world/camera/scroll, 17 effect, 18 release, 8 skill-hit checks. Sample actual 60/120 paints per second, 24 logic ticks per second; inspect world/bag screenshots and Console.
- [ ] User visual confirmation on physical display/input; representative combat/map and frame-time percentile profiling; Windows build.
- [ ] A universal 60-FPS/60-unique-pose claim is NOT established. Original sprite pose sets remain finite; original paint-owned pose/effect counters (including Char.paintCharWithSkill) retain existing behavior. New artwork or a gameplay-sensitive timeline redesign needs a separate scoped decision/validation.

## 2026-11-12
- [~] **Double-click = Enter** on world targets (monster, NPC, player, ground item). `GameScr.onTargetClicked`, config `NsoGameControl._doubleClickMs`. Unity compile clean; `Tools/verify_double_click.cs` 6/6 in Play. Hand test by the user pending.
- [~] **Gamepad (PS5) input**, Elden Ring layout: `Assets/Scripts/Input/NsoGamepadInput.cs`. Sends the same key codes as the keyboard. Unity compile clean; hand test by the user pending.
- [x] Buy-quantity dialog: keyboard typing works, Left/Right choose the 3 buttons, Up/Down step the number (shared Dialog base). Needs user test.
- [x] Removed auto panel extras after 'Không nhặt gì cả' and their code (user request). Needs user test.

## Scrolling notification (2026-11-12)
- [~] Render-rate fractional placement for Info/InfoMe ticker text, controlled by Smooth Effects. Final revision compiled in Unity with zero current errors; visual debugging is delegated to the user by explicit request.

- [~] Fix mouse wheel in information/skill panels: honor Input System scrollDeltaBehavior instead of always dividing by 120. Compile-only validation; user handles runtime debugging.

- [~] Workbench dark mode, larger/heavier typography and Apple-first system fonts; local CSS/HTML/inline text size updated. Browser visual debugging left to user.

- [~] Unify workbench typography to one font family across UI, code, JSON and IDs. CSS source checked; browser QA left to user.

## Global balancing dashboard (2026-11-12)
- [~] Whole-game coefficients for four potentials, fourteen combat stats and normal EXP gain.
- [~] Global/per-template equipment option coefficients, effective on old/new items and shop info without modifying saved raw options.
- [~] Persistent monster-template level/HP/damage/attack-interval/element rules and reset.
- [~] Read-only level/player/item/monster previews, explicit save/reset, revision-protected API and activity history.
- [ ] User gameplay/browser/restart verification. No agent-started Play or automated runtime tests per user instruction.

## Character progression configuration (2026-11-12)
- [~] Separate unclassed/shared-classed/per-class progression profiles, Level-1 original/base/result tables, per-stat growth parameters and reset/inheritance.
- [~] Server-calculated draft preview over selectable level ranges/classes, milestones/deltas and advanced-stat visibility; shared dark appearance/font.
- [~] Reuse gameplay SetAbility, preserve allocated points and old balance settings, version-2 persistence and scoped curve caches.
- [ ] User validates browser edits/preview, save/restart, class inheritance and non-compounding gameplay. Agent validation remains compile/Console/syntax only.

- [~] Add contextual progression formula help, worked HP example and complete attribute glossary under Step 02. JavaScript syntax passed; user handles visual verification.

- [~] Separate Wooden Sword (unclassed) preview from classed comparisons, with its own toggle/row and real quest-equipment stats. Compile/syntax only verified.

- [x] Diagnose wiki access: current runtime URL 8788 works (root/data/control/dashboard/script HTTP 200); 8787 is occupied and unresponsive. Use actual WikiUrl instead of assuming the default port.

- [~] Display original item icons and monster/boss sprite portraits throughout Workbench item/mob views and selected balance editors. Compile/syntax verified; user checks browser appearance.

- [~] F6 > Dev action opens the running Workbench dashboard in the default browser, resolving the live port on click. Unity compile checked; user tests click/Enter.

- [~] Remove screenshot-marked top group card and separate Wooden Sword into its own Step-03 comparison box; preserve profile selection in Step 01. Syntax-only verified.

- [~] Step 02 growth panel and nested guidance default collapsed; save actions remain accessible. JavaScript syntax checked, user handles browser verification.


## 2026-11-12 - Slow effect animation previews
- Added original status icons and a separate paginated animation gallery in the Effects tab. Status IDs are not assumed to map to animation IDs.
- Sources: `nj_effect` frame order, image IDs and offsets from `GameScr.readEfect` / `ServerEffect.paint`; allowlisted original Small images served by WikiSprites as zoom-normalized SVGs. No gameplay calculation or timing changes.
- Files: WikiData.cs, WikiSprites.cs, WikiServer.cs, ServerBootstrap.cs; NsoWiki/effects.js (+ Unity meta), sprites.js, app.js, index.html, app.css; project status/task/decision/handoff documents, ServerPort.md and notes.json.
- Presentation defaults: 4 frames/second, 12 cards/page, at most 6 visible animated canvases, one shared timer, 128-image client cache and existing 16 MiB server asset cache. Pause on hidden document, offscreen cards and route changes; respect reduced-motion preference at initial load. Manual pause/resume available. Missing art displays a notice.
- Validation: node --check passed for effects.js, sprites.js and app.js. Unity refreshed and finished compiling: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No automated gameplay tests or Play/browser validation, per user compile-only instruction.
- Known limitations: names of standalone VFX are numeric source IDs; no invented association to status effects. Browser rendering/performance remains unverified.
- Next: start Play manually, open the workbench through F6 > Dev > Open web, visit Effects, check looping, pagination, pause/resume and scrolling. CLI validation: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.

## Levels 1–39 completion (2026-11-12)
- [~] Quests 11 (friend step removed), 12 (upgrade), 14, 19, 23, 24 and 26: mechanics ported, so no step needs "Bỏ qua" up to level 39. Compile only.
- [~] Kenshinto forge (upgrade, crystal merge, split), buff skills up to level 39, storage box, selling, throwing. Compile only.
- [ ] User Play test of the above.
- [ ] Not ported: gems, "Chuyển hóa", box coins, Aoiame dummy, clone.
- [~] Rikudou "NV mỗi ngày" and "NV Truy bắt Tà Thú" (`Net/TaskOrders.cs`). Compile only; Play test pending.
- [~] Item audit, levels 1–39: TYPE_ORDER items obtainable offline ported (`Net/PlayerItemUse.cs`). Compile only.
- [~] Luong: simulated top-up, exchange, luong shop, revive, careful upgrade, bags, tẩy điểm, Nấm linh chi, Hoàn cốt chi chú (`Net/PlayerLuong.cs`). Compile only.
- [~] Chuyển hóa, Thiên biến lệnh, Kamakura coin limit, Cần câu vàng. Compile only.
- [~] Skill bar shortcuts saved (Char.saveRms). Compile only.


## 2026-11-12 - Wide popup command alignment
Implemented wide popup/button horizontal alignment. Runtime visual verification remains with the user. Animation/filter work was explicitly cancelled as already completed; no edits made to those features in this task.


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

## 2026-11-12 - Hybrid AI agents (MVP)
- [x] Strategic layer: local LLM planner (OpenAI-compatible), shared worker, JSON decisions, validation, rule-planner fallback, event-driven + throttled.
- [x] Tactical layer: travel through waypoints, hunt, collect own drops, rest/retreat reflex, explore, idle; background simulation.
- [x] Presence: PLAYER_ADD/MOVE/REMOVE/DIE, monsters fight agents, speech bubbles (CHAT_MAP), persistence (save database document `agents`).
- [x] Debug: /agents.html, /control/agents, /agents, /replan, [ai] log lines. Inspector settings.
- [ ] Play-mode test by the user (compile only so far).
- [ ] Next: shops/trading with yen, quests, agent-to-agent conversation, equipment, PvP rules.

## 2026-11-13 - SQLite storage
- [x] Save data (accounts, characters with bag/quest/progress, AI agents) in NsoData/nso.db; TSV import + fallback.
- [x] Player log in Logs/PlayerLog/player_log.db; text file optional.
- [x] Migrated the existing save and the 5 old text logs.
- [ ] Play-mode check by the user.
- [ ] Other platforms: add their e_sqlite3 native library if the game is ever built for them.

## 2026-11-13 - AI players (real characters)
- [x] 13 AI players on bot sessions, player rules for everything, start at level 1 / quest 0.
- [x] Characters see each other in a zone (Presence).
- [x] Quest play through NPC menus (quests 0-3 seen in the probe), potions, shop, gear, points, school admission (untested).
- [x] Gameplay memory (AgentKnowledge) + full context and beginner's guide for the LLM.
- [x] Bot log (bot_entries, 20 days); bots cannot use the dev terminal.
- [x] New agents.html (cards, filters, detail drawer).
- [ ] Play-mode check by the user (13 AI players in Làng Tone at start).
- [ ] Watch later quests (8-12) and add tactics where an AI gets stuck.


## 2026-11-13 - Guided online reinforcement learning
- [x] Versioned observations/candidates/masks/experience, teacher fallback and retained NPC/quest macros.
- [x] MaskablePPO trainer, behavioural cloning, policy-version/source separation, CPU C# model interchange and validation.
- [x] uv project/lock/local venv, CUDA installation and RTX 3060 tensor check (user authorised downloads and GPU).
- [x] Learning dashboard, protected control route, configurable rewards, opt-in human demonstrations, budget pause, candidate promotion/rollback.
- [x] Standalone scratch-only real-time evaluator and short synthetic contract checks (passed).
- [ ] User Play/browser check; verify rules behaviour, spool and trainer startup with 13 agents.
- [ ] Enable isolated long evaluation explicitly; confirm quests 0-3, model transitions and later tiers in gameplay.
- [ ] Profile total 13-bot inference p95 and game frame time; judge human likeness. No trained strong model is claimed yet.


Final validation (2026-11-13): Unity refresh and current Console passed at 2026-11-12T19:01:12Z: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. Release host build passed (only the pre-existing unused Controller.zoomLevel warning). Seven short synthetic CUDA contracts passed; JS syntax, Python byte-compilation, uv lock and changed-file whitespace checks passed. No Play/browser/live training/long evaluation run; production saves unchanged. The earlier dirty NSO Game.prefab has a pre-existing trailing-space warning and was not edited for this change.


## 2026-11-13 - Shared ground loot and less competitive AI hunting

- [x] One shared ownership/range/bag/capacity path for human and bot characters; 30 s exclusive / 60 s lifetime, configurable.
- [x] Wider AI foraging, bounded stalled-item retries and courteous target/loot selection.
- [x] Population-based zone selection through NPC 13 and regular Controller messages, with reservations/cooldown.
- [x] RL evaluator receives/fingerprints the same ground timing; headless build and Python syntax pass.
- [ ] User checks live pickup before/after 30 s, expiry at 60 s, full bags/quest gates, zone spreading and NPC walking. No gameplay test run by the agent.


Final compile-only validation for this update: Unity at 2026-11-12T19:21:56Z reported compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. Release host build, Python syntax, wiki JSON and changed-file whitespace checks passed; the host retains its pre-existing unused zoomLevel warning. No post-fix Play/gameplay tests or long training were run. User starts the next Play to apply the shared timing and observe foraging/zone changes.


## 2026-11-13 - Item/NPC reward shaping
- [x] Observe successful item/shop/NPC/storage/upgrade/zone outcomes without changing gameplay handlers.
- [x] Persistent template/NPC novelty, objective keys and useful-use cooldown; no spam/open/cancel reward.
- [x] Configurable weights/thresholds on AI dashboard; per-transition components and per-bot session counters.
- [ ] User checks denied actions, first/repeated uses/purchases, useful potion/food/book/gear use, NPC/quest/quiz/storage/upgrade/zone outcomes, and relog history. No gameplay execution by the agent.


Final compile-only validation (2026-11-13, action rewards): Unity current Console at 2026-11-12T19:40:48Z reported compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. Release host build passed (pre-existing unused zoomLevel warning); agents.js syntax, wiki JSON, meta presence and changed-file whitespace checks passed. No gameplay/Play/browser/learning-session tests were run; user validation is pending.


## 2026-11-13 - AI follow-up acceptance
- Implemented, user runtime check pending: required LLM steps schema, separate format backoff/raw failure diagnostics; remote character render smoothing between sparse movement packets.
- Pending: shared human/bot movement collision parity; isolated candidate evaluation on the current balance; per-personality regression and 13-bot performance checks before enabling RL-primary. Do not promote the unevaluated candidate.


## 2026-11-13 - Bot movement collision follow-up
- Implemented (compile-only acceptance): swept terrain traversal, grounded jumps, ceiling/landing handling, input release and cancellation on correction/death/map/zone change. Both rule and RL movement use this executor.
- User checks pending: approach wood/rock from both sides, low/high walls, ceiling, one-way platforms, walking off a ledge, stop during jump, loot/NPC approaches and map/zone changes. Confirm visible movement and no new stalls. This is not a complete obstacle-route/pathfinding system.
- RL primary remains pending isolated evaluation; previous movement evaluations are invalidated by the new execution fingerprint.

## 2026-11-13 - Path finding / port / LLM leads
- [x] Platform path finding with jump and double jump; unreachable targets are dropped.
- [x] Wiki port not inherited by the RL trainer.
- [x] LLM leads the plan (configurable).
- [ ] Unity compile check after Play mode; watch bots climb in Play mode; check Vách Ichidai reachability.
