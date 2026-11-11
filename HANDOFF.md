# HANDOFF.md

Last updated 2026-11-11. Read `AGENTS.md`, then `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, `docs/` and `README.md`.
Everything below was verified as stated. Anything marked NOT tested was not.
The handoff history before this project is in `C:\NSO_OFFLINE\HANDOFF.md`. It is useful background, but its status tables are stale.

## 1. State in one paragraph
- **What the game is:** an offline NSO game in Unity 6 (`C:\My project`). The Java server has been rewritten in C# (`Assets/Server`) and runs inside the game, connected to the client through an in-memory pipe.
- **What works in tests:** the beginner gameplay loop, verified by the automated server tests:
  - combat, skills and their effects;
  - items, shops, drops and regeneration;
  - quests 0–42 with NPC menus;
  - saving progress.
- **What is NOT verified:** nobody has played these flows by hand in the real client yet. This is the most important next step.
- **New this session:**
  - a debug-only lookup website and JSON API (the "dev wiki") that starts with the game on its own thread;
  - the menu music now plays once;
  - `README.md` and the handoff documents.

## 2. Completed in this project (latest sessions)
1. **Gameplay port from Java** (per-file details in `Assets/Documentation/ServerPort.md`):
   - combat (`PlayerCombat`), skills (`PlayerSkills`), effects and regeneration (`PlayerEffects`);
   - items, shops and drops (`PlayerItems`);
   - quests and NPC menus (`PlayerTasks`);
   - message builders (`PlayerService`), static data (`World/GameData.cs`), progress save (`Store/PlayerProgress.cs`).
2. **Dev wiki** (`Assets/Server/DevWiki`, `Assets/StreamingAssets/NsoWiki`):
   - website with search, filters and record details;
   - API at `/api`;
   - export to `NsoWiki/db.json`;
   - debug only and on its own thread (see `docs/ARCHITECTURE.md`).
3. **Menu music** plays once per visit to the menu (`Assets/Scripts/Audio/MenuAudio.cs`, `menuMusicLoop`).
4. **`README.md`** is written (it was empty).
5. **`walkcheck` repaired** for the in-memory pipe (`Tools/ServerTests/Program.cs`, `TestClient.HasMore`).
6. **Handoff set:** `AGENTS.md`, `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, this file, `docs/ARCHITECTURE.md`, `docs/TESTING.md`. A pointer line was added at the top of the old `C:\NSO_OFFLINE` documents.

## 3. Files
### Created
- **Server:**
  - `Assets/Server/World/GameData.cs`
  - `Assets/Server/Store/PlayerProgress.cs`
  - `Assets/Server/Net/PlayerService.cs`, `PlayerItems.cs`, `PlayerTasks.cs`, `PlayerSkills.cs`, `PlayerEffects.cs`
  - `Assets/Server/DevWiki/WikiData.cs`, `WikiApi.cs`, `WikiServer.cs`
- **Wiki assets:** `Assets/StreamingAssets/NsoWiki/index.html`, `notes.json`
- **Generated output:** `NsoWiki/db.json`, rewritten on every Play in the Editor and by the `wiki` test.
- **Documents:** `AGENTS.md`, `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, `HANDOFF.md`, `docs/ARCHITECTURE.md`, `docs/TESTING.md`; `README.md` content.
- Unity created `.meta` files for the new assets.
### Modified
- **Server:**
  - `Assets/Server/World/World.cs`: partial class, skills, exp table, item options, mob respawn and effect state, items on the ground.
  - `Assets/Server/World/TileGrid.cs`: added `CollisionY`.
  - `Assets/Server/Net/PlayerActions.cs`, `PlayerCombat.cs`, `EnterWorld.cs`, `Controller.cs`.
  - `Assets/Server/GameServer.cs`: the tick.
  - `Assets/Server/ServerBootstrap.cs`: `StartWiki`, `WikiUrl`, `TakeWikiOpenRequest`, wiki shutdown.
- **Client and Editor:**
  - `Assets/Scripts/Diagnostics/NsoServerControl.cs`: wiki settings, `wikiUrl`, opening the browser on the main thread.
  - `Assets/Editor/NsoControlEditors.cs`: "Open dev wiki in browser" button.
  - `Assets/Scripts/Audio/MenuAudio.cs`: plays once.
- **Tests:** `Tools/ServerTests/Program.cs`, which added the `combatcheck` updates, `questcheck`, `progresscheck`, `wiki [serve port]` and the `HasMore` fix.
- **Documents:** `Assets/Documentation/ServerPort.md`.
- **Old project documents:** `C:\NSO_OFFLINE\AGENTS.md`, `PROJECT_STATUS.md`, `HANDOFF.md`. Only a pointer line was added at the top of each.
### Deleted
None.

## 4. Implementation status
See the table in `PROJECT_STATUS.md`. For per-item status, the dev wiki is the most detailed source:
- `features` (done / partial / todo);
- `quests` (which steps are `skip`);
- `npcs` (menu ported or not);
- `skills` (ported types).

## 5. Tests performed (2026-11-11) and results
- **Unity compile:** `compilationFailed:false`, `consoleErrors:0`, after every change, including the last one.
- **Server suite (default):** 18 PASS, 2 FAIL.
  - The failures are "wrong password rejected" and "invalid username rejected". They are expected, because the offline build always signs in to the local account.
- **Other server checks:**
  - `combatcheck`: 6/6.
  - `questcheck`: 16/16.
  - `progresscheck`: 12/12.
  - `entercheck enter_map22`: matches Java.
  - `walkcheck`: walk_A, walk_C and walk_D match Java. walk_B has 2 mismatches, both the zone list (11 bytes instead of 161) caused by the 5-zone cap (user decision).
- **Wiki checks:**
  - `wiki`: 17 collections, each non-empty, every document has an id.
  - In Play mode: the URL is logged from the wiki thread; `/` and `/api/search?q=tajima` return 200; the server stops when Play stops (connection refused afterwards).
  - In the API through curl: `/api`, `/api/search`, `/api/quests/2`, filters, `fields`, `limit` and 404 errors.
  - In the browser: the overview, search ("kiem go"), quest details with links.
- **Menu music in Play mode:** on `SelectServerScr` the source plays with `loop=False`, then stops after 3.7 s and stays stopped.
- **NOT tested:**
  - playing quests, shops, combat and drops by hand in the real client;
  - a Windows build of this project;
  - the wiki in a development player build (only the Editor was checked);
  - the "open in browser" option (`_wikiOpenBrowser`, off by default).

## 6. Known bugs and open issues
- The new gameplay has never been played in the real client. Expect protocol issues in menus, shop windows, item info and effect icons. Ask the user for screenshots.
- The walk_B zone-list mismatch is expected (the 5-zone cap).
- `C:\NSO_OFFLINE\tools\build_and_run.ps1` still builds the OLD project (`C:\NSO_OFFLINE\NinjaOffline`, `testNj.unity`). It has not been adapted to this project.
- Skill types buff, NPC and live (2/3/4) can be selected but do nothing.
- Luong is always 0.
- Build warnings already present: the unused `bad` local in `Tools/ServerTests/Program.cs` and the unused `Controller.zoomLevel` field.
- Dead client code is still present (auto-play `Functions/Auto`, `ReLogin.cs`), kept by the user's decision.
- A Google sign-in pop-up appeared on the user's machine once during a session. It was not caused by the agent's commands (dotnet and the unity CLI only). Its source was not identified.

## 7. Dependencies
- Unity 6000.6.8f1 with the Editor open on `C:\My project`. Agents drive it with the `unity` CLI (`unity command script eval`, `console status`).
- .NET SDK 8, for `Tools/ServerTests`.
- Python 3, used only for ad-hoc editing scripts.
- No packages to install, no online services and no API keys.
- The dev wiki uses only `System.Net.Sockets` and listens on 127.0.0.1.

## 8. Exact next steps
1. **Play by hand in the Editor:**
   - New Game, create a character, enter Lang Tone.
   - Talk to the six villagers (quest 0), answer the quiz at Tabemono (quest 1), get the wooden sword from Tajima, equip it, beat the training dummies (quest 2), buy and eat rice balls (quest 3).
   - Check the shop windows (Kiriko, Tabemono, Goosho), item details, drops and pickup.
   - Go to school (quest 8: three schools, admission) and learn a skill.
   - Fix any protocol differences, comparing with the Java `Service`/`Char` code, and add a test for each fix.
2. **Port buff skills** (`Char.useSkillBuff`), then the most commonly skipped quest mechanics. The dev wiki lists them (`/api/quests?q=status:partial`).
3. **Adapt the build script** to `C:\My project` (`Assets/Scenes/NinjaGame.unity`), build, and run a Windows player offline.
4. **Keep the documentation current:**
   - `Assets/StreamingAssets/NsoWiki/notes.json` (features, constraints, commands, config);
   - `Assets/Documentation/ServerPort.md`;
   - these handoff files.

## 9. Important commands
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj"
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- questcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- walkcheck C:/NSO_OFFLINE/captures/walk_A.txt 22 10 150 0
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- wiki
```
```bash
curl -s "http://127.0.0.1:8787/api/search?q=tajima"
```
For the Unity compile check and Play-mode checks (refresh, console status, play/stop scripts), see `docs/TESTING.md` sections 2 and 3.
