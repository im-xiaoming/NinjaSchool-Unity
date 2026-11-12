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
