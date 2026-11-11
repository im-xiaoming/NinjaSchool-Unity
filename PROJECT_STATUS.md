# PROJECT_STATUS.md

Last updated: 2026-11-11 (handoff after the quest/combat port, the dev wiki and the menu-music fix).
The active project is `C:\My project`. `C:\NSO_OFFLINE` holds the earlier history; its status tables predate the work below.

## Goal
A complete offline, single-player Windows game in Unity 6 (6000.6.8f1): no Internet, no external server, no login and no player chat.
Approach "B2" (user decision): rewrite the Java game server in C# and run it inside the Unity game.

## Overall state: PLAYABLE CORE IN TESTS, NOT YET PLAYED BY HAND
The server side of the beginner loop is ported and passes the automated tests:
- quests 0–42 with NPC menus;
- combat, skills and their effects;
- items, shops, drops and regeneration.

Nobody has played these flows by hand in the real client yet. The title menu, character select, entering Lang Tone and walking were seen working in earlier sessions.

| Area | State | Verified how |
|---|---|---|
| Unity project compiles | done | Unity `compilationFailed:false`, 0 console errors (2026-11-11) |
| Embedded server over an in-memory pipe (no TCP), login, character list, create, select | done | server test suite |
| Enter world, movement, map and zone change, monster aggro, death, revive | done | entercheck and walkcheck vs Java captures |
| Player attacks monsters: damage, critical, miss, exp, level up, respawn | done | `combatcheck` |
| Skills: learn from books, skill and potential points, select, passive bonuses; effects on monsters (burn, freeze, stun, bind) | done; buff/NPC/live skill types NOT done | `progresscheck`, Java source |
| Items: bag, equip and unequip, HP/MP potions, food, item info; shops with xu and yen; drops and pickup | done (no luong, no upgrade, no box) | `questcheck`, `progresscheck` |
| HP/MP over time (food, potions, equipment, plus offline natural regeneration of 2 % every 5 s) | done | code; food effect seen in `questcheck` |
| Main quests 0–42, quiz, school admission, NPC menus of the village and school NPCs | done; steps without a ported mechanic can be skipped | `questcheck` (quests 0–3), `progresscheck` |
| Save of bag, equipment, xu, yen, quest step, skills, potential, saved village | done | `progresscheck` + JSON round trip |
| Dev wiki website and JSON API (starts with the game) | done | Play mode in the Editor, curl on the routes, `wiki` test |
| Menu music plays once per visit to the menu | done | Play mode: `loop=False`, the track stopped after 3.7 s |
| README | done | `README.md` |
| Real-client play-through of quests, shops and combat | NOT done | — |
| Upgrade, gem, crystal, storage box, mounts, pets, clone, PvP, special maps, friends, party, trade, clan | NOT done | — |
| Full English localization | NOT done (the English branch still holds Vietnamese text) | — |
| Windows release build of this project | NOT done; the build script still targets the old project | — |

## Where things live
- **Unity project:** `C:\My project`.
  - Client: `Assets/Scripts`.
  - Server: `Assets/Server`.
  - Static data: `Assets/StreamingAssets/NsoStatic`.
  - Wiki: `Assets/StreamingAssets/NsoWiki`.
- **Tests:** `C:\My project\Tools\ServerTests`. Java captures are in `C:\NSO_OFFLINE\captures`.
- **Java reference:** `C:\NSO_OFFLINE\reference\server_java\src\main\java\com\nsoz`.
- **Documents:** `docs/`, `Assets/Documentation/`, `README.md`, and the dev wiki.

## Main risks
1. Nobody has played the new gameplay by hand. Protocol details that the tests do not cover may break the real client:
   - menus;
   - shop windows;
   - item info;
   - effect icons.
2. The quest chain past quest 3 is verified only step by step against the Java source. Quests whose mechanics are not ported rely on the "Bỏ qua" (skip) entry.
3. `C:\NSO_OFFLINE\reference\nsonew_full.sql` and `C:\NSO\nsonew.sql` contain real people's data and must never ship. `tables.txt` holds only the 25 static tables.
