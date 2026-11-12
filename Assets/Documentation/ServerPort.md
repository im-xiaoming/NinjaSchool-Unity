# Server port (Java → C#) status

The game server from the original Java source (`C:\NSO_OFFLINE\reference\server_java`) is being rewritten in C# under
`Assets/Server` and runs inside the game (in-memory pipe, no network). This page lists what is ported and how to test it.

## Ported
| Area | C# file | Java source |
|---|---|---|
| Sessions, login, character list/create/select | `Net/Session.cs`, `Net/Controller.cs`, `Store/AccountStore.cs` | `network/*`, `model/User.java` |
| Enter world, map info, scenery, items on the ground | `Net/EnterWorld.cs` | `Service.loadAll`, `sendZone`, `sendItemMap` |
| Walking, map/zone change, death, revive (back to the saved village) | `Net/PlayerActions.cs` | `Zone.move`, `Char.changeMap/startDie/returnTownFromDead` |
| Monsters notice and attack the player | `Net/MobBrain.cs` | `Mob.update`, `Mob.attack` |
| Player attacks monsters, damage, critical, miss, death, respawn, exp, level up | `Net/PlayerCombat.cs` | `Char.attackMonster`, `Char.addExp`, `Mob.die/recovery` |
| **Bag sort** (BAG_SORT -107): merge stackable items, pack to the front | `Net/PlayerItems.cs` (`BagSort`) | `Char.bagSort` |
| **Skills**: learn from books, raise skill points, raise potential, select skill, passive skill bonuses | `Net/PlayerSkills.cs` | `Char.learnSkill/upSkill/upPotential/selectSkill` |
| **Skill effects on monsters**: burn, freeze, stun, bind, disable, Kakyuu (sushi) | `PlayerCombat.Hit`, `PlayerEffects.AddMobEffect` | `Char.attackMonster(ArrayList)`, `Mob.update` |
| **Items**: bag, use (HP/MP potions, food), equip / unequip, item details | `Net/PlayerItems.cs` | `Char.useItem/useEquipment/itemBodyToBag/requestItemInfo` |
| **Shops**: shop lists, buying with xu / yen | `Net/PlayerItems.cs` | `Char.buyItem`, `Store.buy`, `Char.requestItem` |
| **Drops and pickup**: quest items, yen, random items, equipment; items vanish after 30 s | `Net/PlayerItems.cs` | `Mob.dead/dropItem`, `Char.pickItem`, `Zone.updateItemMap` |
| **HP/MP over time**: food, potions, equipment options | `Net/PlayerEffects.cs` | `EffectManager`, `Char.updateEveryHalfSecond/FiveSecond` |
| **Quests and NPC menus**: take, steps, counts, finish, rewards (quests 0–42), quiz, school admission | `Net/PlayerTasks.cs` | `Char.openMenu/initMenu/menu/getTask/takingTask/taskNext/finishTask` |
| Static data: quests, shops, skill templates, effects, item options, exp table | `World/World.cs`, `World/GameData.cs` | `GameData`, `StoreManager`, `Server.npcTasks` |
| Message helpers (dialogs, menus, quest/item/effect messages) | `Net/PlayerService.cs` | `Service`, `AbsService`, `MapService` |
| Saved progress: bag, equipment, xu, yen, quest step, skills, potentials, saved village | `Store/PlayerProgress.cs` (inside `PlayerRecord.Data`) | `players` columns |

### NPC menus ported
Tajima (`Char.npcTajima`: "Hủy vật phẩm và nhiệm vụ" = `Char.clearTask`, "Nói chuyện", offline "Nhận kiếm gỗ" once per character), Kiriko (potions), Tabemono (food), Kenshinto, Okanechan, Kamakura ("Lưu tọa độ"),
Umayaki (village teleports) and Umayaki 2 (school teleports, `Char.npcUmayaki_2`), the NV Thách đấu gyms ("Vào phòng thi đấu", `Net/Gymnasium.cs`: Gymnasium, Bot, AttackAround, MoveWithinCustom, PvP hit of Char.attackCharacter), the Jaian escort (NV Đưa Jaian trở về, `Net/Escort.cs`: Jaian NPC status in mapInfo, "Đi nào", JaianMove, escortFailed), the three teachers (Toyotomi, Ookamesama, Kazeto: "Nhập học"), Kanata (weapons),
Furoya (clothes), Ameji (jewellery), Goosho (shop, books, fashion). Other NPCs show their own menu and answer
"Chức năng này chưa có trong bản offline."

## Offline deviations (on purpose)
- **Natural regeneration disabled**: `PlayerEffects.NaturalRegenPercent = 0` by default, per the user's correction.
  Recovery requires food, potions or equipment, as in Java. The former 2 %/5 s addition is opt-in only.
- **Quest steps whose mechanic is not ported** (dungeons, escorting Jaian, fishing, carrying water, daily tasks, friends,
  item upgrades, battlefield) can be skipped at the NPC that gives the quest: its menu shows "Bỏ qua: <step>". Talk steps
  on NPCs without a hand-ported menu get an entry named after the step.
- No luong (gold): items that cost luong cannot be bought.
- Maximum MP never goes below 100 (the value the Java server gives a new character).

## Not ported yet
Buff skills (`useSkillBuff`, skill types 2/3/4), item upgrade / gem / crystal windows, storage box, mounts, pets,
clone skills, special maps (dungeons, wars, events), friends / party / trade / clan.

## Dev wiki (lookup website + API)
A small debug-only website (compiled only in the Editor and development builds; it runs on its own background thread
`NsoDevWiki`, so the game's main thread never waits on it) starts with the game at `http://127.0.0.1:8787/` (only on this machine;
if the port is taken the next 9 are tried, and the Console prints `[NsoWiki] Dev wiki: <url>`). The **NSO Server Control**
object has the settings (`_wikiEnabled`, `_wikiPort`, `_wikiOpenBrowser`), the current address and an "Open dev wiki"
button.

It holds NoSQL-style collections, one document per id:
- generated on every start from `tables.txt` and the ported code: `quests` (steps, NPC per step, kill/pick targets,
  which steps are skippable offline), `npcs` (menu, where they stand, ported or not), `maps` (type, unlock quest, NPCs,
  monsters, exits), `mobs`, `items` (shops, quest drops), `shops`, `skills` (levels, ported or not), `effects`,
  `itemOptions`, `classes`;
- hand-written in `Assets/StreamingAssets/NsoWiki/notes.json`: `features` (done / partial / todo), `constraints` (game
  rules with their message and code location), `commands` (network commands and handlers), `config`, `deviations`,
  `issues`, `tests`. Edit the file and press "Tải lại dữ liệu" in the page.

The page searches without accents (`kiem go`), filters by field (`id:194`, `status:todo`, `area:"Vật phẩm"`) and links
records as `#items/194`. In the Editor the whole database is also written to `NsoWiki/db.json` in the project folder.

API for scripts and agents (JSON):
| Request | Result |
|---|---|
| `GET /api` | collections, counts, field names, query syntax |
| `GET /api/search?q=kiem go` | hits in every collection |
| `GET /api/quests?q=status:partial&fields=id,name` | filtered documents (`&field=value`, `limit`, `offset`) |
| `GET /api/quests/2` | one document |

Without Unity: `dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- wiki` checks and writes
`NsoWiki/db.json`; add `serve 8790` to also run the website on that port.

Code: `Assets/Server/DevWiki/WikiData.cs` (builds the database), `WikiApi.cs` (API), `WikiServer.cs` (HTTP),
started from `ServerBootstrap.StartWiki`. A wiki failure only logs a warning; the game is not affected.

## Tests
`Tools/ServerTests` is the original test suite pointed at this project's server (in-memory pipe instead of TCP).
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj"
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- combatcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- questcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- progresscheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- entercheck C:/NSO_OFFLINE/captures/enter_map22.txt 22
```
Results on 2026-11-11:
- `questcheck`: 16/16 pass. Quests 0–3 played through: talk to the six villagers, the quiz, the wooden sword, equipping
  it, 10 training-dummy kills, buying and eating rice balls.
- `progresscheck`: 12/12 pass. A quest item drops and is picked up, joining the sword school, reading the skill book,
  raising skill and potential points, wearing the school sword.
- `combatcheck`: all pass.
- `entercheck`: matches Java byte for byte.
- Full suite: 18 pass, 2 fail. The 2 failures are expected: this build always signs in to the one local account.

## Loot exclusions (user clarification, 2026-11-11)
PlayerItems.CanDropLoot rejects training dummy template 0 and any mob at least PlayerItems.DropLevelGapLimit (default 10) levels below the killer. Applies before quest drops, normal loot/currency and to DropSushi through Drop. Dummy exclusion mirrors Java Mob.dead; applying the directional level restriction to all item paths is an offline deviation. Original ordinary loot requires absolute level gap <=7 and retains its probabilities; quest kill counters and XP rules are unchanged. Verified: lootcheck 9/9, questcheck 16/16, progresscheck 12/12; Unity compiles with 0 current Console errors. Manual loot play-through remains pending.

## Workbench-controlled loot rules (2026-11-11)
Gates now use immutable GameWorld.LootRules, saved in server save-folder game-rules.json. Defaults preserve prior policy. #settings can toggle dummy/global overlevel exclusions, inclusive cutoff (10), and the independent ordinary Java Mob.dead gate (7). Probabilities unchanged. OnMobDead/Drop share the policy including quest items, yen and sushi. #inspector calls /control/loot without creating items. GameServer.Start reads saved rules in every build; web writes only exist with debug wiki. webcheck 27/27; lootcheck/questcheck/progresscheck pass. See architecture for control API and scope.

## Recovery default corrected (2026-11-11)
NaturalRegenPercent defaults to 0 per user correction. Source-free HP/MP gain is disabled; Java Char.updateEveryHalfSecond/updateEveryFiveSecond food/potion/equipment recovery remains. NaturalRegenMs=5000 still governs the equipment five-second cadence. regencheck 9/9, questcheck 16/16, progresscheck 12/12; Unity compile/Console clean. This supersedes the earlier agent-added 2% natural-recovery default.

## Dev terminal (2026-11-12)
| Client | Server | Status |
|---|---|---|
| CHAT_MAP (-23) from the chat box | `DevTerminal.Run` (offline deviation: commands, not chat) | done |

- Dev terminal `/skip`: `PlayerTasks.SkipTask` finishes the current quest (offline deviation, no Java equivalent).

- Dev terminal `/lv`, `/tolv n`: `DevTerminal.LevelTo` adds the missing experience via `PlayerCombat.AddExp` (offline deviation).

- Basic attack growth (class 0): `GameWorld.AddBasicSkillGrowth` adds skill rows, `PlayerCombat.SyncBasicSkill` sets the point from the level (offline deviation, see `BasicSkillGrowth`).

## Offline global balance system (2026-11-12)
Protected GET/POST /control/dashboard edits whole-game rules. GameBalanceRules (game-balance.json) stores player/potential coefficients, normal EXP gain and global/per-template equipment coefficients. PlayerTuning wraps AbilityFromEquip.setAbility using temporary values; saved allocation/raw options stay original. Item/shop info uses the same effective option calculation as combat. MobTuning (monster-tuning.json) extends Mob.setClass/setHP/setDamage/recovery globally. Both files load in release without the wiki. Existing packets refresh clients; no account writes/new protocol. Wide math supports configurable values. Defaults preserve original gameplay. Compile/syntax only; runtime verification left to user.

## Configurable character progression (2026-11-12)
CharacterProgressionRules centralizes the original Char.addExp/selectClass potential bases and native growth. PlayerTuning overlays configured Level-1/native-growth parameters around the existing AbilityFromEquip.setAbility port, preserving raw allocated points/equipment contributions. PlayerCombat.ProgressionSample and the protected WikiProgression draft API reuse this calculator. game-balance.json v2 reads v1/v2 and keeps older coefficients. No actual player save changes or new game protocol. Detailed semantics: docs/CHARACTER_PROGRESSION.md. Compile-only validation; user owns runtime verification.

## Quest wooden-sword preview factory (2026-11-12)
PlayerItems.CreateTaskWoodenSword shares the existing Char.receiveWoodenSword quest reward construction (item 194, option 0=10 and 8=1) with the read-only progression preview. The quest still receives the exact same item; the alternate NPC-menu variant remains unchanged. Preview equipment exists only in a temporary class-0 PlayerState and is calculated by SetAbility. Compile-only validation.

## Workbench sprite endpoints (2026-11-12)
GET /sprites/items/{templateId}.png maps item.icon to Img/Small/{zoom}/Small{icon}.png. GET /sprites/mobs/{templateId}.svg uses Img/Mob/{zoom}/{id}_0.png (98/99 single files), composing bosses from MobTemplate.ImageInfo/Frames/FrameBossMove as the client does. WikiSprites requires the existing item table and world data root, accepts canonical numeric IDs only, uses a 16 MiB FIFO cache and returns 404 for missing art. No gameplay/protocol changes; compile-only verified.


## 2026-11-12 - Slow effect animation previews
- Added original status icons and a separate paginated animation gallery in the Effects tab. Status IDs are not assumed to map to animation IDs.
- Sources: `nj_effect` frame order, image IDs and offsets from `GameScr.readEfect` / `ServerEffect.paint`; allowlisted original Small images served by WikiSprites as zoom-normalized SVGs. No gameplay calculation or timing changes.
- Files: WikiData.cs, WikiSprites.cs, WikiServer.cs, ServerBootstrap.cs; NsoWiki/effects.js (+ Unity meta), sprites.js, app.js, index.html, app.css; project status/task/decision/handoff documents, ServerPort.md and notes.json.
- Presentation defaults: 4 frames/second, 12 cards/page, at most 6 visible animated canvases, one shared timer, 128-image client cache and existing 16 MiB server asset cache. Pause on hidden document, offscreen cards and route changes; respect reduced-motion preference at initial load. Manual pause/resume available. Missing art displays a notice.
- Validation: node --check passed for effects.js, sprites.js and app.js. Unity refreshed and finished compiling: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No automated gameplay tests or Play/browser validation, per user compile-only instruction.
- Known limitations: names of standalone VFX are numeric source IDs; no invented association to status effects. Browser rendering/performance remains unverified.
- Next: start Play manually, open the workbench through F6 > Dev > Open web, visit Effects, check looping, pagination, pause/resume and scrolling. CLI validation: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Item sprite project paths
- Item records now expose spritePath: an existing project-relative PNG under Assets/StreamingAssets/NsoStatic/Data/Img/Small, resolved by icon ID with zoom 4-to-1 preference. Missing art returns null. Runtime save paths are not exposed.
- Item detail in the workbench shows the path with a copy button and a clear missing-file message. Read-only metadata; gameplay and sprite rendering are unchanged.
- Changed: Assets/Server/DevWiki/WikiData.cs, Assets/StreamingAssets/NsoWiki/app.js; project status/task/decision/handoff docs, ServerPort.md and notes.json.
- Validation: app.js node --check passed. Unity refreshed and compile/Console passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/browser tests per user compile-only preference.
- Next: start Play, refresh workbench data, open Items > any item, inspect Sprite in project and copy the path into the IDE. Compile: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - All item sprite zoom paths
- Item metadata now includes spriteFiles for zoom 1-4, each with project-relative path, absolute fullPath (explicit user request), existence and PNG-header dimensions. Legacy spritePath remains the preferred highest existing zoom for compatibility.
- Item detail lists every zoom with full path and a per-file copy button; explains that gameplay zoom can differ from web preview and cached game icons need refreshing after replacement. Missing variants are explicit. Reads only 24-byte PNG headers; no texture decoding or image/metadata edits.
- Changed: Assets/Server/DevWiki/WikiData.cs; NsoWiki/sprites.js, app.js, app.css; project status/task/decision/handoff docs, ServerPort.md and notes.json.
- Validation: node --check passed for app.js and sprites.js. Unity refresh/compilation passed: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No Play/browser tests per user compile-only instruction.
- Next: start Play and refresh the workbench data, open an item, check zoom 1-4 sizes/full paths and copy each path. Small1131 sizes observed read-only: 16x14, 32x28, 48x42, 64x56. Gameplay zoom was observed as 3 before edits; zoom 4 edits alone do not affect zoom 3 art.
- Validation commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Hide map travel button and add /next
- World map no longer creates the Go To left command; LayoutCommands positions only Close. Share/Create toggle, analog cursor, existing map pointer/keyboard behavior and unbound travel handler retained. No gameplay travel code removed.
- DevTerminal /next advances an active quest through PlayerTasks.NextTaskStep -> existing TaskNext (Char.taskNext), preserving save, client packets, removed-step policy and visible quest object refresh. It bypasses current step requirements only. No auto-accept or reward grant; final step asks the player to report to the NPC or use /skip. Missing/inactive/invalid/final quests return explicit replies without mutation. /help lists /next.
- Offline deviations requested by user; no duplicate quest system, protocol or save-schema changes.
- Changed: Scripts/Screens/MapScr.cs, Server/Net/DevTerminal.cs, Server/Net/PlayerTasks.cs; root status/task/decision/handoff docs, UiReference.md, ServerPort.md and wiki notes.
- Validation: Unity refresh/compile passed; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:06:36Z). No Play tests, terminal execution or save changes, per user compile-only preference.
- Next: user verifies map shows only Close; /help; /next on an active intermediate step, final step and before accepting a quest. Confirm progress persists after reload. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Align monster damage and remove false full heals
- Follow-up: MISS-only correction did not resolve reported full-HP recovery when hit. Source trace found raw NPC_ATTACK_ME damage differed from the 20%-scaled server deduction; subsequent HP sync restored the gap. NPC_ATTACK_PLAYER also carried pre-hit HP. Client ME_LOAD_SKILL/UPDATE_POTENTIAL handlers independently filled HP/MP on refresh although server SetAbility preserves current values. Runtime reproduction remains unverified (compile-only preference).
- MobBrain.Hit now scales positive HP damage once before applying/sending it, preserves MISS/zero fully absorbed damage, sends observers post-hit HP/MP and excludes the victim from the observer packet. Existing mana shield consumption, dodge, resistances, OnHit/death hooks retained. No new packet format. This supersedes the older intentional raw-display/scaled-damage behavior.
- Client skill/potential refreshes clamp current HP/MP to new maxima instead of healing. Dedicated HP/MP/recovery packets remain authoritative for actual recovery; food/potions/equipment and respawn retained. Earlier MISS guard remains.
- User requested stronger monsters: default mobDamagePercent raised 20 -> 100 (five times previous post-resistance HP damage before rounding); dodge defaults unchanged. Web remains configurable 0..500%. Existing saved combat rules retain their explicit settings. Read-only check found no combat-rules.json at the active Editor data folder, so this project's next run uses 100%. No save files modified.
- Offline deviations from Java Mob.attack(Char,Char)/display semantics and client stat-refresh healing are intentional corrections; documented in wiki deviations and CombatBalanceConfiguration.md.
- Changed: Assets/Server/Net/MobBrain.cs, Assets/Server/World/CombatRules.cs, Assets/Scripts/Networking/Protocol/Controller.cs, Assets/StreamingAssets/NsoWiki/app.js, CombatBalanceConfiguration.md, root docs, ServerPort.md, UiReference.md and wiki notes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:15:55Z). Source diff check passed. node --check app.js passed. No Play, gameplay tests, physical inputs or save mutation.
- Next: user starts Play, checks web combat damage=100, takes repeated nonlethal hits with no recovery sources, observes HP/floating damage through skill/stat refresh and a level-up; verifies MISS, shield, lethal hit/respawn, and legitimate item recovery. Existing saved combat rules in other installs need web adjustment to 100 if desired. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json; node --check Assets/StreamingAssets/NsoWiki/app.js.


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
