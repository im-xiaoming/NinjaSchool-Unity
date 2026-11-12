# DECISIONS.md

## Manual movement release (2026-11-11, user correction)
- User requested removal of the roughly 0.4 s delay after releasing movement. Play-mode baseline: speed 8 retains cvx=8 on release; original client Char.updateCharRun friction needs 9 ticks at 24 Hz (0.375 s) and adds 36 pixels.
- Intentional offline client deviation: stop only grounded manual running when the last held horizontal control is released. Keyboard arrows/number-pad and pointer controls share Char.StopGroundMovementOnRelease. Preserve other held directions, jump/fall, remote characters and currentMovePoint paths. No change to speed, logic rate or server movement rules.
- NSO Game Control / Instant Movement Stop defaults true; disabling it restores the original friction path. No scene or prefab default overrides required.

Format: decision — why — who decided. New entries go at the bottom of each section.

All decisions in `C:\NSO_OFFLINE\DECISIONS.md` still apply unless changed below. Among them:
- approach B2;
- never modify `C:\NSO`;
- automatic local sign-in;
- chat removed;
- card top-up removed;
- multiplayer-style features kept;
- maximum 5 zones per map;
- the title-menu style;
- the bitmap fonts.

## Carried over and changed
- **Transport:** the earlier "server listens on 127.0.0.1:14444" is replaced by an in-memory pipe (`Assets/Server/Net/LocalPipe.cs`, `GameServer.Connect`). No socket and no port are used for the game itself. The protocol bytes are unchanged. — Made in an earlier session of this project; recorded here because the old documents still say TCP.
- **Active project:** `C:\My project` is the continuation of `C:\NSO_OFFLINE\NinjaOffline`. New work goes only into `C:\My project`. — Inferred from the code: `C:\My project` has every later feature.

## User decisions (binding)
1. Port drops and pickup, quests, NPCs, shops, skill effects (burn, freeze, stun), the other skills and HP/MP over time, with priority on the quest system "để có thể qua màn" (so the game can be progressed). — user, 2026-11-11.
2. Build a lookup website that starts with the game, holding items, quests, rules, NPC ids, and what is done and not done, stored NoSQL-style for bug lookup. Simple UI, a readable font, search and filters. — user, 2026-11-11.
3. Give the website an API so agents can look things up themselves. — user, 2026-11-11.
4. The website must start on a background thread and exist for debugging only. — user, 2026-11-11.
5. The menu-screen music plays once, not in a loop. — user, 2026-11-11.
6. Write the project documentation into `README.md` (the user already had one, and it was empty). — user, 2026-11-11.
7. Prepare the project for a handoff to another agent using the AGENTS/PROJECT_STATUS/TASKS/DECISIONS/HANDOFF rules. — user, 2026-11-11.

## Technical decisions (agent)
- **Offline natural regeneration (historical, now disabled by user correction):** previously 2 % of max HP and MP every 5 s (`PlayerEffects.NaturalRegenPercent`, `NaturalRegenMs`).
  - Why: the user asked for HP/MP over time, and the original only regenerates from food, potions and equipment.
  - Setting it to 0 restores the original behaviour.
- **Skipping quest steps:** a step whose mechanic is not ported gets a "Bỏ qua: <step>" entry at the quest giver (`PlayerTasks.IsStepSupported`).
  - Why: the chain must never block.
- **Generic talk entries:** a "talk to X" step on an NPC whose menu is not hand-ported gets a menu entry named after the step.
- **No luong (gold):** items priced in luong cannot be bought.
- **Minimum max MP:** never below 100, the value Java gives a new character.
- **Weapon requirement kept as in Java:** attacking needs a suitable weapon.
  - The wooden sword comes from Tajima, as the reward of the quest-1 quiz or through the "Nhận kiếm gỗ" entry.
  - The test character gets a sword through `Data`.
- **Progress storage:** inventory and progress stay in the `PlayerRecord.Data` JSON (`Store/PlayerInventory.cs`, `Store/PlayerProgress.cs`).
  - Keys: bag, equipment, fashion, bagCells, `yen`, `xu`, `saveMap`, `task{index,count}`, `skills[[tpl,point]]`, `potential`, `potentialPoint`, `skillPoint`.
  - Nothing new is added at runtime.
- **Dev wiki data:**
  - Game data collections are generated at runtime from `tables.txt` and the ported code, so they cannot drift from the server.
  - Project knowledge is hand-written in `notes.json` and merged in.
  - Every document has an `id`.
  - Status values: `done`, `partial`, `todo`, `skip`.
- **Dev wiki HTTP server:**
  - a small `TcpListener` HTTP server bound to 127.0.0.1 only, starting at port 8787 and trying the next 9 ports;
  - not `HttpListener`, which behaves differently between Mono and .NET and may need URL ACLs on Windows.
  - It sends no CORS header, so other websites in the browser cannot read it.
- **Dev wiki is debug only and threaded:**
  - compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD`;
  - binding, building and the accept loop run on the background thread `NsoDevWiki` (below-normal priority); requests run on the thread pool;
  - the main thread only starts the thread and, if asked, opens the browser.
  - A failure only logs a warning.
- **Dev wiki file export:** in the Editor the database is also written to `NsoWiki/db.json` (project root, outside `Assets`, so Unity does not import it). Agents can grep it without Play mode.
- **Dev wiki hooks in `PlayerTasks`:** small read-only members were added (`PortedNpcMenus`, `RewardOf`, `TaskDropList`), and `IsStepSupported(world, task, idx)` was made public and static. Game logic is unchanged.
- **Menu music once per visit:** the login track (3.7 s) plays once when a menu screen is reached.
  - Moving between menu screens does not replay it; coming back from the game does.
  - `MenuAudio.menuMusicLoop = true` makes it repeat again.
- **Test harness:** `TestClient.HasMore` reads in the background with a wait instead of using `ReadTimeout`, because the in-memory pipe cannot time out. The pending byte is kept for the next read, so nothing is lost.
- **Handoff documents:** they live in `C:\My project`. The old set in `C:\NSO_OFFLINE` stays as history, with a pointer line added at its top.

## Onboarding scope (2026-11-11)
- The user requested understanding first and will assign the next task. Do not automatically implement the suggested backlog during onboarding. No architecture or gameplay decision changed. Audit evidence and documentation discrepancies are in `docs/UnityProjectContext.md`.

## Client skill timing correction (2026-11-11)
- Offline timing deviation: preserve the original client `Char.updateSkillPaint/setAttack` hit-frame threshold, but dispatch once per animation after reaching OR crossing it, before animation cleanup. Unity renders independently of logic ticks, unlike the original assumed update/paint cadence. Exact frame equality skipped basic sword attacks at high FPS; using >= without a per-cast guard could duplicate actions at low FPS. Reset the guard in `setAutoSkillPaint`. No damage, range, cooldown, quest requirement or server rule was changed.

## Loot policy clarification (2026-11-11, user)
- The user explicitly selected: no loot from training dummies or monsters at least 10 levels below the player. Applies to quest items, ordinary items/equipment, yen rewards and skill-created sushi. The dummy exclusion follows Java Mob.dead; extending the directional overlevel restriction to quest/sushi paths is an intentional offline deviation. Tunable: PlayerItems.DropLevelGapLimit = 10 (minimum effective value 1). Existing ordinary-loot absolute seven-level restriction and probabilities remain unchanged; passing the new global gate does not guarantee loot. Quest kill counts, damage and experience rules are unchanged.

## Render-time effect presentation (2026-11-11)
- User specified monster hit/HP presentation and F1 popup as the low-FPS-looking effects. Keep client logic at the configured rate (24 ticks/s by default) and preserve all server/gameplay timing. Extend presentation interpolation only for floating numbers, hit splashes and menu geometry; use a visual-only smoothstep transition for HP-bar ratios, never write actual mob.hp/maxHp.
- Intentional offline presentation deviation from original client GameScr.updateFlyText/updateSplash, Menu.updateMenu and Mob.paint: draw fractional positions/HP widths each render. The original menu skips its slide when reopened within five seconds; retain a configurable minimum 0.14 s opening, with input blocked until visible, while preserving its longer staggered path. HP transition default 0.12 s. NsoGameControl exposes Smooth Effects, Mob Health Seconds and Menu Open Seconds; disable/zero options restore instant presentation where applicable.
- Original sprite atlases/poses, pixel filtering, general movement/camera interpolation, damage, cooldowns, skill dispatch, quest/drop rules and networking remain unchanged. No claim of extra sprite poses or a CPU/GPU speedup. Removed per-caption one-element arrays from menu drawing as a local allocation reduction.

## Configurable local workbench (2026-11-11, user request)
- User requested changing the dummy/10-level option on the website, a broader redesign, and both live character/map/drop diagnostics and detailed quest/NPC/item lookup.
- Keep the local debug wiki lifecycle, B2 embedded server and in-memory game transport. Upgrade in place; no hosted service, online dependency, framework/package download or original-source edit.
- LootRules is immutable per GameWorld. Defaults remain blockTrainingDummy=true, blockOverlevel=true, overlevelGap=10, limitOrdinaryLoot=true, ordinaryMaxGap=7. The ordinary Java Mob.dead gate is independently adjustable so disabling the global gate is not misleading. This configurability is an intentional offline deviation; probabilities and other gameplay rules are unchanged.
- Saves validate schema and revision, persist game-rules.json atomically before replacing the snapshot under SyncRoot, and apply at the next loot event. File IO occurs outside the world lock, under a separate change lock. No player/static/reference files are edited. Failed writes leave active rules unchanged. GameServer.Start loads saved rules without the wiki; invalid saved rules log a warning and retain defaults without deleting the file.
- HTTP control routes use bounded JSON POST, a per-session local token and same-origin/Host checks; no CORS, UnityEngine calls on HTTP threads or arbitrary commands. Runtime snapshots use SyncRoot; history holds at most 100 session changes.
- Move JsonWriter from DevWiki/WikiData to Data/JsonWriter so persistence has no world-to-debug dependency. Use a 4 KB initial buffer instead of 1 MB for small polled responses; JSON semantics unchanged.
- Bookmarks/recent use browser localStorage, separate from game configuration. Deep links and /api remain. Browser/native automation were unavailable; pure UI tests are not visual evidence.

## No recovery without a source (2026-11-11, user correction)
The user reported gradual HP gain without items/external effects and asked for a fix. This supersedes the agent's earlier interpretation of HP/MP-over-time as free 2% regeneration. PlayerEffects.NaturalRegenPercent defaults to 0; HP and MP now require a legitimate effect or equipment option, following Java Char.updateEveryHalfSecond/updateEveryFiveSecond. Keep existing food/potion/equipment behavior and five-second equipment cadence. The old positive-percent branch remains explicit opt-in code only; it is not enabled by the Workbench or saved loot settings.

## Broad render-motion smoothing (2026-11-12, user request)
- User asked for all animations to look smooth at >=60 FPS. Extend the established render-only Interp architecture; do not raise the logic rate or change combat/server/save rules. Render cap range becomes 60-144. A cap is a request to Unity, not a hardware performance guarantee.
- Intentional offline presentation deviation: retain fractional residuals after legacy integer entity/camera interpolation and draw them with scoped GUI transforms. Include NPCs, pets, dropped items, arrows/darts/trails, souls, lanterns, eggs, figures and weather. Nested absolute-coordinate projectile/pet scopes replace the parent's residual so offsets do not add twice. World camera scope ends before fixed HUD. Existing teleports/map changes snap.
- Scroll input/inertia/hit-testing remain integer and authoritative. Weakly registered instances capture pre-tick state; only rendered content receives a fractional offset. Clip/origin/reset/finally clear the content offset; recycled or changed geometry snaps. Smooth Effects controls weather/scroll too; new Smooth World Motion (default true) disables fractional world placement when needed. No new packages, assets, filtering or sprite frames.
- Finite source artwork cannot supply 60 distinct authored poses each second. Do not speed up sprite cycling or claim universal FPS from the sampled village. This pass preserves existing paint-owned pose counters; broader animation timeline changes are not silently included.

## 2026-11-12: double-click = Enter (offline client addition)
Not in the original client. Double-click on a focused world target simulates the fire key so the existing Enter path runs; no new gameplay logic. User request. Configurable via NSO Game Control.

## 2026-11-12: gamepad input (offline client addition)
User request: PS5 controller with Elden Ring-style buttons. Implemented as a key-code translator so no gameplay path changes; mapping configurable per button.

## 2026-11-12: dialog arrow selection; menu entries removed
- Dialog bottom buttons can be chosen with Left/Right + Enter (keyboard/gamepad). Offline client addition.
- User decision (permanent): the game menu no longer lists "Thủ khố", "Tự sát", "Khu vực".
- 2026-11-12: Dialog keyboard/gamepad handling is centralised in `Dialog` (FocusedField / UpDownStepsNumber hooks) instead of patching each dialog. Up/Down step numeric input fields (offline deviation, gamepad has no digits).
- 2026-11-12: User decision (permanent): auto options reMap, Chuyển map khi hết boss, Tự sát khi hết Mp, Đánh tinh anh, Đánh thủ lĩnh and Auto Reconnect are removed from the game. Former defaults are kept as fixed behaviour; no reconnect.
- 2026-11-12: Offline deviation (user request): the chat box is a dev terminal. CHAT_MAP (-23) runs Server/Net/DevTerminal commands on the local player instead of public chat; DevTerminal.Enabled switches it off.

### Basic attack grows for class 0 (2026-11-12, user request)
- Java gives class 0 no skill points and the data has one row for skill template 0. Offline, the basic attack gets one automatic point per level (max 9, -20 ms cooldown each). Generated rows ids 30001..30009 on client and server from `BasicSkillGrowth`. Offline deviation.

## Notification text interpolation (2026-11-12)
Offline presentation deviation: interpolate Info/InfoMe text between pre-tick and current coordinates using the existing Smooth Effects option. Preserve original update timing and text speed. Apply the offset only to text inside a fixed clip; new message identity snaps, paused logic captures stationary state. No server or gameplay changes. User requests compile/Console checks only; leave runtime debugging to them.

## Wheel unit correction (2026-11-12)
Keep existing offline mouse-wheel support and Scroll.WheelStepPx. Input System 1.19 defaults to UniformAcrossAllPlatforms (one notch); divide by 120 only for KeepPlatformSpecificInputRange on Windows. No input settings, bindings, selection, clipping or gameplay change.

## Workbench typography/theme (2026-11-12)
User requested dark mode, larger/heavier text and Apple fonts. Make dark the default (including native color-scheme). Use locally available -apple-system/BlinkMacSystemFont/SF Pro Text/SF Pro Display with Segoe UI/Arial fallbacks, and ui-monospace/SFMono-Regular/Menlo/Consolas for code. Do not download or bundle proprietary Apple fonts. Existing local API, routes and settings behavior are preserved.

## Single workbench font (2026-11-12)
User supersedes the separate UI/mono font choice: use --font-ui everywhere, including code/pre/IDs. Retain Apple-first system UI stack with Segoe UI fallback; no separate monospace stack or downloads.

## Whole-game balancing system (2026-11-12, clarified user request)
- User clarified a durable game-balancing system, not a character/session editor. The final dashboard writes global rules only; it never changes PlayerRecord, currency, allocated points or raw inventory options.
- Intentional offline deviation: GameBalanceRules in game-balance.json holds percent + flat adjustments for four potentials and fourteen final combat stats, plus experiencePercent. Potentials feed a temporary calculation array before AbilityFromEquip.setAbility; final-stat adjustments run afterwards. Each recalculation starts from the original base, avoiding compounding. Normal EXP gains use the rate; /lv and /tolv bypass it.
- Equipment rules run for all templates (key -1), then per template, using value * percent / 100 + flat at each stage. Effective options are computed at ability/item/shop-info time. Already-owned and future items use the same rules; saves keep raw options. Original upgrade-option gates remain.
- Percent range 0..1000, flat +/-1000000; integer fractions truncate. Effective potentials are bounded 0..30000, speed 0..17 and adjusted combat values to protocol-safe bounds; maxima remain positive. Wide formula/damage intermediates prevent 32-bit wrap under tuning.
- MobTuning in monster-tuning.json applies six per-template combat fields across zones/spawn/recovery. Live changes preserve HP/death/rank. Explicit HP overrides are absolute final maxima and are reflected accurately in map/recovery packets; reset removes template overrides.
- Both version-1 files load from the save folder in GameServer.Start without the wiki, including release. Atomic file replacement precedes rule publication. Dashboard changes then SyncRoot locks serialize save/recalculation; no account writes or Unity API on HTTP threads. Existing protocol refreshes client caches at unchanged coordinates.
- Keep B2 embedded server, in-memory transport, original tables/references and debug-site authentication. GET/POST /control/dashboard uses existing token/Host/Origin/size checks and its own revision. Sample characters are temporary unsaved objects. Forms are explicit-save snapshots.
- User requested compile/Console checks only. Runtime/visual/restart behavior remains unverified.

## Character progression profiles (2026-11-12)
User requested intuitive Level-1 bases, configurable growth, two character groups and per-class differences, with maintainable code. Keep Char.addExp/selectClass default potential curves and AbilityFromEquip.setAbility as the single calculator. Classed characters still receive and manually allocate points; default previews assume none allocated. Profiles 0/classed/1..6 inherit each base/growthPercent/growthFlat field. Derived stat rules adjust the native naked curve and retain actual equipment/skill/allocation contributions; potential rules use original class curves and temporary arrays. This configurable overlay is an intentional offline deviation; empty profiles preserve native progression. Advanced global coefficients remain compatible. game-balance.json now writes version 2 and reads versions 1/2; no player-save migration. Draft previews validate and calculate without saving/publishing/packets. Core config, stat application, HTTP representation and frontend drafts remain separate; formulas are not duplicated in JavaScript. See docs/CHARACTER_PROGRESSION.md for exact semantics, bounds and extension points.

## Formula guidance (2026-11-12)
Explain the existing progression formula in the dashboard, including explicit percent /100, cumulative native growth rather than growth per tick, blank inheritance versus numeric zero, classed manual allocation and an illustrative example distinct from current preview values. Keep the calculation engine unchanged.

## Wooden-sword preview scenario (2026-11-12)
User requested a distinct Wooden Sword entry in Step 03, outside the classed group. Model it as unclassed class 0 equipped with the quest wooden sword #194, not a new class/profile. Share CreateTaskWoodenSword with the existing quest reward so preview options do not diverge (0:10, 8:1). The separate Tajima menu sword variant (0:10, 9:10) stays unchanged. Do not grant/equip a real player; compute a temporary sample through SetAbility, including global/template equipment rules.

## Wiki URL guidance (2026-11-12)
Keep existing fallback-port behavior. Runtime WikiUrl and NSO Server Control > Open dev wiki in browser are authoritative; 8787 is only the preferred starting port. Do not terminate Unity or disturb the running game just to reclaim a default port when the current wiki is healthy elsewhere.

## Workbench sprite display (2026-11-12)
User requested corresponding sprites for items and monsters. Reuse existing NsoStatic/Data/Img art by numeric template ID, without external services, asset copying or UnityEngine calls on HTTP threads. WikiSprites resolves item icon IDs from the original item table and serves PNGs; monster thumbnails use a representative frame, composed as a self-contained SVG for bosses following Mob.paint frame offsets/layers/flips. Static portrait only, no animation/gameplay timing changes. Numeric-only routes prevent arbitrary file reads; 16 MiB per-wiki FIFO cache and one-hour browser asset caching. Existing HTML/API keep no-store. Missing art returns 404 and a visible UI fallback.

## F6 web shortcut (2026-11-12)
User requested a default-browser button in F6. Reuse the existing Dev Action row/input/rendering, with Application.OpenURL on the main thread and live WikiUrl instead of a hardcoded port. Availability/errors appear in the normal hint area. Do not change the debug-only web lifecycle or saved settings.

## Progression layout correction (2026-11-12)
User clarified Wooden Sword must be a separate entry in Step 03, then requested removing the top unclassed/classed card. Remove that card; keep a compact target profile selector in Step 01. Present Unclassed / Wooden Sword / Classed as three separate comparison boxes. Wooden Sword remains a class-0 equipment scenario, not a new class or saved profile.

## Collapsed growth section (2026-11-12)
User requested the pictured growth section collapsed by default. Use native details/summary for Step 02 and close nested guidance initially. Keep shared form actions outside the collapsible section; reveal it when an invalid growth field needs focus. Stat formulas and saved settings remain unchanged.


## 2026-11-12 - Slow effect animation previews
- Added original status icons and a separate paginated animation gallery in the Effects tab. Status IDs are not assumed to map to animation IDs.
- Sources: `nj_effect` frame order, image IDs and offsets from `GameScr.readEfect` / `ServerEffect.paint`; allowlisted original Small images served by WikiSprites as zoom-normalized SVGs. No gameplay calculation or timing changes.
- Files: WikiData.cs, WikiSprites.cs, WikiServer.cs, ServerBootstrap.cs; NsoWiki/effects.js (+ Unity meta), sprites.js, app.js, index.html, app.css; project status/task/decision/handoff documents, ServerPort.md and notes.json.
- Presentation defaults: 4 frames/second, 12 cards/page, at most 6 visible animated canvases, one shared timer, 128-image client cache and existing 16 MiB server asset cache. Pause on hidden document, offscreen cards and route changes; respect reduced-motion preference at initial load. Manual pause/resume available. Missing art displays a notice.
- Validation: node --check passed for effects.js, sprites.js and app.js. Unity refreshed and finished compiling: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No automated gameplay tests or Play/browser validation, per user compile-only instruction.
- Known limitations: names of standalone VFX are numeric source IDs; no invented association to status effects. Browser rendering/performance remains unverified.
- Next: start Play manually, open the workbench through F6 > Dev > Open web, visit Effects, check looping, pagination, pause/resume and scrolling. CLI validation: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.

- 2026-11-12: Offline deviation (user request): quest 11 "NV Bạn hữu tâm giao" no longer has the "Giao lưu bạn hữu" (make friends) step; PlayerTasks.TaskNext skips it. Later the same day (user request) the whole quest was removed: TaskFinish goes 10 -> 12 (two TASK_FINISH so the client follows) and `PlayerTasks.SkipRemovedTask` moves saves sitting on 11 to 12 at login.

- 2026-11-12: Offline deviations, level 1–39 completion:
  - **Chikatoya tunnel:** when its time ends, the player returns to the Hang Meiro gate (Java: saved coordinate).
  - **Treasure map picture:** drawn from the collision grid (Java: rendered tile map).
  - **Taiyoutama (effect type 5):** its param is added to each hit (Java: burns the monster).
  - **Joutenhitomi (type 13):** raises each hit by param % (Java: potential-based attack bonus).
  - **Careful upgrade:** refused, because luong is 0.

- 2026-11-12 (user: "làm theo bản gốc"): the five deviations above (tunnel return, treasure image, Taiyoutama, Joutenhitomi, careful upgrade) are reverted to Java behaviour. Treasure image = TilePicture (TileMap.createImage from the client tile files; Editor path Assets/Resources/res). Luong is still 0 offline, so the careful upgrade fails on its luong check exactly as Java would with 0 luong.

- 2026-11-12: **Simulated top-up (offline deviation).** Okanechan "Nạp lượng" credits a card's value as luong at once (`PlayerLuong.TopUpLuongPerVnd`, default 1 luong per VND). Java credits luong from its payment website. Luong is stored per character in the progress JSON; Java keeps it on the account (`user.gold`).

- 2026-11-12: Small offline deviations:
  - **Cần câu vàng:** the "line broke" text is a server message; Java shows it as a chat bubble.
  - **Kamakura coin-limit fee:** computed in long; Java's int cast overflows above 2.1 billion.
  - **Thiên biến lệnh settings:** range and pick-up mode are not saved.

- 2026-11-12: In Trang bị on large screens, left/right move the equipment selection while the item info is shown, and Enter / Circle opens "Chọn" (offline client input addition, user request).
- 2026-11-12: Skill bar (user request): the 5-slot bar is shown once the first book skill is learned (`GameScr.hasBookSkill`; the original hid it below 2 skills on touch), and "Ô kỹ năng" offers all 5 slots (the original stopped at the first empty slot).
- 2026-11-12: Skill bar on PC (user request): drawn on the HP/MP potion row (`GameScr.paintSelectedSkill`, pcBarY), without slot index numbers or cooldown seconds (`Skill.paint(..., showCountdown: false)`).
- 2026-11-12: NV Thách đấu (quest 13) ported in `Assets/Server/Net/Gymnasium.cs` (Java Gymnasium + Bot AI). Offline deviations: the room is offered only on the matching headmaster's step and only that step advances; bot skill range is dx/dy + 100 (Java grew it by 100 every tick); HAVE_ATTACK_PLAYER sends the HP after the hit; PvP (cmd 61) only works against the gym bot.
- 2026-11-12: client fix: `Char.changeStatusStand` clears `cyStartFall`. The original left it stale after landing on the cy%24==0 path, which made later falls skip floors (fell through the ground in map 61 and died).
- 2026-11-12: offline deviation: `PlayerActions.RequestChangeMap` / `ChangeMap` answer with RESET_POINT when no change happens (no waypoint at the server position, dead, unknown/unsupported map). Java returned silently; the client had already locked on its black loading screen (`Char.ischangingMap`) and stayed black. The server position can lag the client by the last unsent move.
- 2026-11-12: offline debug feature (user request): `NsoServer.Net.PlayerLog` writes one text file per Play session to `<project>/Logs/PlayerLog/player_<time>.log` (builds: `<persistentDataPath>/Logs/PlayerLog`). It records every client message (IN, with position/HP), every server message (OUT, optional), every GameServer.Log line unfiltered (LOG), events (EVT: enter map, die, leave, refused map change) and a STATE snapshot every 5 s. Settings are on NsoServerControl (enabled, outgoing, snapshot seconds, payload bytes, files kept = 20). Read this file first when debugging a Play mode report.
- 2026-11-12: offline deviation: `PlayerActions.IsCrossMap` no longer kills at the bottom edge (cy >= pxh - 24) when the tile under the feet is T_TOP. Maps 61 and 63 have solid ground as their last tile row, so walking on it (y = pxh - 24) killed the character (map 63 at x 144+, where the water tiles stop exempting it).
- 2026-11-12: `/goto` (QuestTravel.GoToObjective) arrives at the quest map's entrance (arrival point of a waypoint leading there, preferring one from the current map), then LocationStand; the NPC / quest spot / monster positions are only fallbacks. User request: stand at the start of the map, not on the objective.
- 2026-11-12: NV Đưa Jaian trở về (quest 17 step 1) ported in `Assets/Server/Net/Escort.cs` (Char.initMenu JAIAN, Service.mapInfo NPC status, JaianMove, escortFailed). Monsters notice and hit Jaian (MobBrain, Mob.update/attack); 0 HP fails the escort. Offline deviations: a failure sends the client's own escort message (SUB_COMMAND -90: PROTECT_FAR / PROTECT_INJURE; Java was silent); the client draws "do <name> hộ tống" in yellow (user request, Char.cs).


## 2026-11-12 - Wide popup command alignment
Offline presentation deviation from GameScr.paint: wide inventory and shared shop UI command rows use popupX/popupW plus configurable popupSideInset instead of fixed screen-centred -160/-35/+88 offsets. Default inset -2 matches Paint.paintFrame outer horizontal border. Preserve Paint.CmdY, command actions and narrow layouts to avoid three-button overlap.


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

## 2026-11-12 - Tajima menu (Char.npcTajima)
- Ported "Hủy vật phẩm và nhiệm vụ" (Char.clearTask): level 10+, drops the quest in progress (same quest id, retaken from step 0 at its giver), removes bag items of types 23/24/25 (and one DA_CAP_9 on NV 24 step 2), ends a Jaian escort, sends NOT_MAP -98 so the client clears its task and items.
- Offline deviations: "[Bật/Tắt] nhận lời mời" and "Thứ thân" are left out (no other players, no clones). The offline "Nhận kiếm gỗ" entry stays but only gives a sword when the character has none in bag, box or equipment (it was unlimited).
- Changed: Assets/Server/Net/PlayerTasks.cs (NpcTajima, HasItem, ClearTask). Unity compiled clean (compilationFailed=false, consoleErrors=0); not run in Play mode.

## 2026-11-12 - Skill animation advances once per logic tick
- The original client advanced a skill animation inside paint (Char.paintCharWithSkill: indexSkill++, effect frames i0..i2, arrow launch), one paint per game tick. Unity paints at the screen rate (60 fps, more with VSync on a high-refresh monitor) while the logic runs at 24 ticks/s, so skills looked 3-6 times too short.
- Offline fix: Main.LogicTick counts FixedUpdate ticks; only the first paint after a tick (or after a skill restart) runs the original step; later paints in the same tick restore the pre-step state, redraw the same frame without launching an arrow, and put the post-step state back. Hit timing (updateSkillPaint) is unchanged.
- Changed: Assets/Scripts/Gameplay/Entities/Char.cs, Assets/Scripts/Application/Main.cs. Unity compiled clean; not run in Play mode.
