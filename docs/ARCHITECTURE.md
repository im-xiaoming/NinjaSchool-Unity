# Architecture (active project `C:\My project`)

This file supersedes `C:\NSO_OFFLINE\docs\ARCHITECTURE.md` wherever the two differ. The biggest change is the transport: the client talks to the server through an in-memory pipe, not TCP 14444.
The wire protocol is still the original binary protocol. See `C:\NSO_OFFLINE\docs\PROTOCOL.md` for framing, and `Assets/Server/Net/Cmd.cs` plus the dev wiki `commands` collection for ids.

## Overview
```
Unity client (Assets/Scripts: original game code with the online features removed)
      │  in-memory pipe (Assets/Server/Net/LocalPipe.cs): no socket, no port
      │  Session_ME.cs calls ServerBootstrap.Server.Connect(); the bytes are the original NSO protocol (XOR key, framing)
      ▼
Embedded C# server (Assets/Server; started by ServerBootstrap at AfterSceneLoad, stopped on quit / leaving Play mode)
      ├─ GameServer.cs   holds Config, AccountStore, IStaticData, GameWorld; Connect() gives the client stream; 500 ms tick thread
      ├─ Net/            Session (key exchange, framing), Controller (dispatch), EnterWorld, PlayerActions (PlayerState),
      │                  PlayerCombat, PlayerSkills, PlayerItems, PlayerTasks, PlayerEffects, PlayerService (message builders),
      │                  MobBrain, Message/Cmd, LocalPipe, FileStaticData
      ├─ World/          GameWorld (partial: World.cs + GameData.cs): maps, zones, mobs, items, skills, exp table, quests,
      │                  shops, effects; TileGrid (terrain); MapUnlock (maps opened by quest progress)
      ├─ Store/          AccountStore (users.tsv, players.tsv); PlayerInventory and PlayerProgress (JSON in PlayerRecord.Data)
      ├─ Data/           TableFile (tables.txt reader/writer), SqlDump (dump parser for the export tool), Json (reader)
      └─ DevWiki/        WikiData (builds the lookup database), WikiApi (/api), WikiServer (HTTP on 127.0.0.1:8787)
Static data: Assets/StreamingAssets/NsoStatic (tables.txt with 25 static tables, *.bin blobs captured from Java, Data/Map,
             Data/Img/{Object,Small,Mob})
Dev wiki assets: Assets/StreamingAssets/NsoWiki (index.html, notes.json)
Save data: Application.persistentDataPath/NsoData (users.tsv, players.tsv)
```
Nothing in `Assets/Server` depends on UnityEngine except `ServerBootstrap.cs`. `Tools/ServerTests` therefore compiles the same files with `dotnet` (net8.0) and drives the server through the same pipe.

## Server tick (GameServer.TickLoop, every 500 ms, under GameWorld.SyncRoot)
For every created zone, in this order:
1. `PlayerCombat.UpdateRecovery`: monster respawn.
2. `PlayerEffects.UpdateMobEffects`: burn, freeze, stun and bind expire.
3. `PlayerItems.UpdateItemMaps`: ground items vanish after 30 s.
4. `PlayerEffects.UpdateHalfSecond` for each player: food, potion and equipment regeneration. Unsourced natural regeneration is disabled by default.
5. `MobBrain.UpdateZone`: monsters notice and attack.

Client messages are handled in `Controller` under the same lock, so there is no per-object locking.

## Player state
- `PlayerState` (in `PlayerActions.cs`) is the runtime character: stats, level, skills, `Inventory`, `Progress`, effects, the open NPC menu (`Menus`), and the quiz counter.
- Persistent fields live in `PlayerRecord`:
  - columns: map/x/y, `TaskId`, exp, class, ...;
  - `PlayerRecord.Data` JSON: bag, equipment, fashion, xu, yen, saveMap, task {index, count}, skills [[template, point]], potential, potentialPoint, skillPoint.
- `PlayerItems.Save` writes the record after every meaningful change.

## Quests and NPCs
- `PlayerTasks` ports `Char.openMenu / initMenu / menu / getTask / takingTask / taskNext / finishTask` and the quest hooks.
- The quest data comes from the `task_template` and `task` tables (`GameWorld.FindTask`, `TaskNpcs`).
- Hand-ported NPC menus are listed in `PlayerTasks.PortedNpcMenus`. Every other NPC answers "Chức năng này chưa có trong bản offline."
- A quest step whose mechanic is not ported (`PlayerTasks.IsStepSupported` returns false) gets a "Bỏ qua: <step>" entry at the quest giver.

## Dev wiki (debug only, own thread)
- **Debug only:** `ServerBootstrap.StartWiki` is compiled only under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, so a release build contains no wiki. It runs only when `NsoServerControl._wikiEnabled` is set.
- **Threading:** `WikiServer.Start()` only creates the background thread `NsoDevWiki` (below-normal priority) and returns.
  - That thread binds the port, builds the database and accepts connections.
  - Each request is answered on the thread pool.
  - The URL is logged from that thread (`Started` callback).
  - "Open in browser" is handed to the main thread through `ServerBootstrap.TakeWikiOpenRequest()` in `NsoServerControl.Update`, because `Application.OpenURL` must run on the main thread.
  - The game's main thread never waits on the wiki.
- **Routes:**
  - `/`: `index.html`, read from disk on every request;
  - `/db.json`: the database; `?reload=1` rebuilds it;
  - `/api/...`: `WikiApi`.
- **Building the database:** `WikiData.Build` reads `tables.txt` plus the ported code (`PlayerTasks.RewardOf`, `TaskDropList`, `IsStepSupported`, `PortedNpcMenus`, `MapUnlock`), then merges `notes.json`.
- **Export:** in the Editor it also writes `C:\My project\NsoWiki\db.json`.
- **Failure:** a wiki failure only logs a warning; the game is unaffected.

## Client side (Assets/Scripts)
- Scripts are grouped by responsibility (Application, Audio, Bridge, Data, Diagnostics, Functions, Gameplay, Input, Networking, Platform, Rendering, Screens, UI, Utilities).
  - These are the original global types; there are no namespaces.
- **Scene objects** (prefab `Assets/Prefabs/NSO Game.prefab`, scene `Assets/Scenes/NinjaGame.unity`):
  - NSO Game Control, NSO Input Control, NSO Server Control;
  - NSO UI Layout, NSO Game Layer, NSO Map Overlay.
  - Their settings are described in `Assets/Documentation/InspectorConfiguration.md` and `UiReference.md`.
- **Map overlay:** places Unity objects on the game map. It is visual only; see `Assets/Documentation/UnityMapOverlayBridge.md`.
- **Menu audio:** `Audio/MenuAudio.cs` plays the login track on the menu screens, once per visit (`menuMusicLoop`), plus the button click sounds.
- **Title flow:** `SelectServerScr` is the offline title menu (New Game, Continue, Language, Settings, Exit). After that the client signs in automatically to the one local account.

## Client skill hit timing
`Char.paintCharWithSkill` advances skill animation indices during rendering, independently of FixedUpdate. `Char.updateSkillPaint` therefore dispatches the hit/buff action once per cast when indexSkill reaches or crosses the original hit threshold, before end cleanup. `setAutoSkillPaint` resets the dispatch guard. This offline timing correction prevents missed high-FPS hits and duplicate low-FPS actions; server combat rules stay authoritative and unchanged.

## Effect presentation independent of logic ticks
Interp.BeforeTick snapshots floating-text/hit-splash positions and menu geometry even if world updates pause. Their paint methods draw fractional offsets through an allocation-free mGraphics.OffsetScope; GUI.matrix is restored on exit. Existing integer coordinates, lifetimes and input targets stay in logic. The menu has a minimum realtime opening for recent reopen; isScrolling guards activation until it is visible. Mob health uses NsoHealthTween to draw a fractional-width visible bar over 0.12 s without modifying hp/maxHp. Configuration is on NsoGameControl. General camera/entity interpolation and server timing are unchanged.

## Local workbench and persistent loot policy
GameWorld.LootRules is immutable, read under existing SyncRoot by drops. CanDropLoot checks dummy/global overlevel policy, then ordinary rewards apply the separate ordinary gate. Detached calls retain legacy DropLevelGapLimit fallback; running worlds use their policy.
GameServer.Start loads DataDirectory/game-rules.json version 1. Rules: blockTrainingDummy, blockOverlevel, overlevelGap (1..200), limitOrdinaryLoot, ordinaryMaxGap (0..200). Defaults: true,true,10,true,7. Loading has no Unity/wiki dependency.
WikiControl attaches only to the debug site. GET /control/state: token/revision/active+default rules/runtime. GET /control/status: player/map/quest/bag snapshot. GET /control/events: up to 100 session changes. GET /control/loot?playerLevel=20&mobId=0[&questId=2&step=1]: actual eligibility explanation.
POST /control/rules accepts {revision,rules}, application/json and X-Nso-Control from state. Validate, check revision, atomically save, then replace under SyncRoot. File IO is outside world lock under a separate change lock. No arbitrary player/code/Unity operations.
WikiServer bounds headers/body to 8192 bytes, validates Host/write Origin, sends no CORS and serves only index.html/app.css/app.js. /api and /db.json preserved. Shared serialization lives in Data/JsonWriter.cs. UI uses same-origin assets/system fonts, no new runtime dependency. Poll every 2 s when visible; lists page 75 records. Bookmarks/recent are browser-local. Responsive layout implemented but not visually verified here.

## Fractional world and scroll presentation (2026-11-12)
Interp.BeforeTick snapshots the integer world and list states. BeginPaint retains the existing temporary integer positions for legacy drawing/culling, adds a fractional residual per tracked object and restores authority in EndPaint. Additional projectile/particle objects keep their authoritative fields untouched and use render offsets only. ObjectScope replaces the parent residual for nested absolute-coordinate projectiles/pets; a world-only camera scope ends before fixed HUD. Map identity/teleport guards prevent blending across transitions. No timing/network/save changes.
Scroll weak registration captures pre-tick content offsets; only its drawn content is positioned fractionally, while viewport and hit testing use original coordinates. mGraphics clears the content offset at clip/origin/reset boundaries; Main.finally also clears it if painting fails. Smooth World Motion and Smooth Effects opt-outs are Inspector settings. Render-cap requests range 60-144; finite sprite artwork and old paint-owned pose counters remain distinct from position interpolation.

## Global balancing dashboard (2026-11-12)
WikiControl's partial WikiDashboard handles GET/POST /control/dashboard; WikiServer serves dashboard.js. Writes target global files, never player records or item instances. GameBalanceRules owns player/EXP coefficients and effective equipment option rules. PlayerTuning applies these around AbilityFromEquip.setAbility using a temporary potential array. Global equipment rules (-1) then template rules evaluate at ability/info time; saved options stay raw. MobTuning extends spawn/recovery. GameServer.Start loads both config files even without the debug site. Existing revision/token/origin/size checks remain. Atomic replacement/publication/live refresh use changes -> SyncRoot; existing packets refresh caches at unchanged coordinates, without Unity calls on HTTP threads. Samples are unsaved temporary objects and forms do not auto-refresh over drafts.

## Character progression profiles (2026-11-12)
CharacterProgressionRules owns typed optional base/growth fields and inheritance. GameBalanceRules holds published profiles and versioned persistence; PlayerTuning uses temporary potentials and cached native curves; PlayerCombat.SetAbility is the shared calculator for real characters and explicit draft snapshots. WikiProgression handles validated preview metadata/HTTP projection; progression.js contains only form/draft/display logic. Profiles augment native growth without duplicating stat formulas or writing allocation saves. Full equation and maintenance boundaries: CHARACTER_PROGRESSION.md.

## Original game art on the local wiki (2026-11-12)
WikiSprites is a read-only asset adapter wired by ServerBootstrap. Strict template-ID routes resolve original item icons and monster images under GameWorld.DataDirectory/Img. PNGs are served as binary; boss portraits are self-contained SVGs assembled from existing metadata and embedded source sheets, never Unity textures. Bounded per-instance caching avoids repeat IO; binary assets permit browser caching while HTML/control API remain no-store. Frontend sprites.js supplies reusable markup/error handling; no image paths/data added to static records or gameplay objects.


## Shared tab/grid navigation (2026-11-12)
`Assets/Scripts/UI/Components/TabGridNavigation.cs` is a pure selection policy: direction + cell index/count/columns produces a new index or the Title sentinel. Equipment supplies its irregular vertical slot map. `GameScr.NavigateInfoItemGrid` adapts bag/equipment/mount/bijuu equipment; `NavigateCraftItemGrid` adapts upgrade and chest, including the extra upgrade equipment/material hierarchy. GameScr owns command refresh, scroll, tooltip and tab state; existing action handlers own all gameplay changes. Directional input is consumed before crafting command refresh and the handler returns immediately to prevent multiple transitions in one frame. These are existing states of GameScr, so sharing the policy avoids a new screen-inheritance hierarchy. New matching item panels should use the same policy through a small adapter.


## 2026-11-12 - Main quest objective teleport /goto
- User deferred further resistance/monster balance changes; previous CombatRules remain unchanged. Added /goto (also in /help) to travel to the current main-quest objective, not advance the quest explicitly. Before acceptance this means the quest giver; active quests use task.maps[step+1] and existing TaskNpcId class-school resolution. Report steps therefore return to their NPC.
- New focused QuestTravel resolver: NPC position first, then mechanic-owned collection/water/treasure locations, then required kill/drop mob spawn, then designated map standing location. Kill steps with map=-2 prefer a suitable current map, otherwise the lowest-ID normal map with that spawn. Unknown/no-fixed objectives return a reply without moving.
- Reuses SetXY/ResetPoint/ChangeMap and normal entry hooks; exploration steps may advance naturally upon entering their objective map. No TaskNext/FinishTask, auto-accept, auto-fight, reward or inventory edits. Dead players and active catch/fishing actions are refused. Special/private maps retain NPC/item entry requirements. Dev terminal teleport intentionally bypasses walking/map-unlock routing on supported normal maps.
- GameWorld now loads task.maps alongside task.npcs and exposes read-only lookup by convention; original static data unchanged. TaskMechanics owns special positions so the terminal does not duplicate quest coordinates. No new package, scene or save schema.
- Changed: Assets/Server/Net/QuestTravel.cs (+ Unity meta), DevTerminal.cs, TaskMechanics.cs, World/GameData.cs, World/World.cs; four root docs, docs/ARCHITECTURE.md, ServerPort.md and wiki notes.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:31:14Z). Diff check passed; Unity generated QuestTravel.cs.meta. No Play, terminal execution or save mutation, per user compile-only preference.
- Next: user checks /goto before quest acceptance, NPC/report, kill/drop quests, collection/water maps, same-map reposition, class-specific school, missing/final objectives and dead-state refusal. Special-map replies should preserve entry mechanics. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


## 2026-11-12 - Thien bien lenh automation and explicit auto-loot exception
- User reported item 572 not working, then explicitly clarified it SHOULD auto-pick while enabled. Ordinary play remains manual; this supersedes the manual-only rule solely for enabled Thien bien lenh.
- Source gaps: client command 119 set isUseitemAuto but did not start persistent GameScr.auto=1; server ignored movement command 119; local movement omitted X distances, skipped a reusable index, had an inverted 2s cooldown expression and could select multiple mobs in one scan. Focus cleanup could erase the newly selected mob.
- Client enable now starts existing auto-fire and clears movement lock; disable stops it and unlocks. Focus resolves before item movement. Nearest live non-rank-3 mob is selected with range checks, no distance-array allocations, one move/2s; no movement while dialogs/panels/chat are open. Existing attacks, skill cooldowns, mana and combat packet path retained. Manual input retains the legacy auto-pause behavior; toggle off/on to resume.
- Server command 119 now validates enabled mode, unexpired owned item, living character/mob, captured range origin and move cooldown; synchronizes position through ResetPoint. Item existence/expiry checked at enable, movement and periodic tick; no item consumed. Range changes recapture origin consistently with client. No unrestricted teleport endpoint.
- Enabled item invokes the existing GameScr.autoPickItem entry point: settled loot within 120px, every 350ms, at most eight requests; skips obvious full-bag noncurrency cases. Public tunables ItemAutoPickupRadius/IntervalMs, server AutoMoveIntervalMs. Normal pickup ownership/quest/range/bag rules retained. Toggling off or auto-pause stops automatic requests. Existing always-on HutVP suction remains disconnected. Settings menu now states auto-pick while enabled instead of offering ineffective stored-only pickup modes.
- Changed: Assets/Server/Net/PlayerItemUse.cs, PlayerActions.cs, Controller.cs; Assets/Scripts/Networking/Protocol/Controller.cs, Screens/GameScr.cs, Gameplay/Entities/Char.cs; root docs, docs/ARCHITECTURE.md, ServerPort.md, UiReference.md and wiki notes. Offline completion of Char.useItemTypeOrder THIEN_BIEN_LENH, Service.turnOnAuto/turnOffAuto and MapService AUTO_ATTACK_MOVE. No save-schema/scene/package change.
- Validation: Unity refreshed and compiled; compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0 (2026-11-12T13:46:57Z); source diff check passed; no Play, automation execution, item pickup or save mutation per user compile-only preference.
- Next: user uses item -> On in a monster map, closes menus, verifies targeting/move/attack/nearby item+yen pickup; tests 240/360/whole-map range, Off, item expiry/removal, manual movement pause and menu pause. Ordinary play must not auto-pick. Commands: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.


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
