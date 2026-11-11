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
4. `PlayerEffects.UpdateHalfSecond` for each player: food, potion and equipment regeneration, plus the offline natural regeneration.
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
