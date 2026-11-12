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
Tajima (quests, "Nhận kiếm gỗ"), Kiriko (potions), Tabemono (food), Kenshinto, Okanechan, Kamakura ("Lưu tọa độ"),
Umayaki (village teleports), the three teachers (Toyotomi, Ookamesama, Kazeto: "Nhập học"), Kanata (weapons),
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
