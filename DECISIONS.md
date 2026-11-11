# DECISIONS.md

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
- **Offline natural regeneration:** 2 % of max HP and MP every 5 s (`PlayerEffects.NaturalRegenPercent`, `NaturalRegenMs`).
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
