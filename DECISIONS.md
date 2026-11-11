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
