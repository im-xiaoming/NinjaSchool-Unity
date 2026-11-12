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
