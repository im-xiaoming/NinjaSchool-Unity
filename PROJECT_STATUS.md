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
| HP/MP over time from food, potions and equipment; unsourced natural recovery disabled | done | `regencheck` 9/9, food in `questcheck` |
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

## Onboarding verification (2026-11-11)
Current source was reviewed at commit `3e740918`. Server tests were rerun: combat 6/6, quest 16/16, progress 12/12; default 18 PASS / 2 documented login FAIL. Live Unity Console groundTruth: compilationFailed=false, consoleErrors=0. No Play mode or Windows build was run. See `docs/UnityProjectContext.md`; manual gameplay verification is still pending.

## Quest 2 client attack fix (2026-11-11)
Fixed skipped skill-hit dispatch when Unity renders faster than the client logic tick. Play-mode runtime inspection confirmed ordinary client attacks lowered dummy HP 500000 -> 400000 -> 300000 -> 200000 -> 100000 -> 0; quest 2 count advanced 0 -> 1. This validates one dummy kill, not the full manual quest chain. Unity compiled with 0 current Console errors; skill-frame regression 8/8 and server questcheck 16/16 passed.

## Loot exclusions (2026-11-11)
User-confirmed rule: training dummies and monsters at least 10 levels below the player give no loot. PlayerItems.CanDropLoot gates quest drops, ordinary loot/currency and skill-created sushi. Original ordinary-loot absolute seven-level restriction/probabilities remain unchanged. Server lootcheck 9/9, questcheck 16/16, progresscheck 12/12; Unity compilation/current Console clean. No manual loot play-through claimed.

## Effect presentation at high FPS (2026-11-11)
User narrowed the issue to monster-hit/damage presentation and the F1 popup. Added render-time interpolation of floating numbers, hit splashes and menu row/scroll offsets, a minimum 0.14 s menu opening even on recent reopen, and a 0.12 s visible mob HP-bar transition. Authoritative HP, effect lifetimes, gameplay ticks and attack cadence stay unchanged. Play-mode regression 17/17 and hit-frame regression 8/8; live samples: 72 floating-text positions vs 16 logical positions, 38 menu positions vs 9 logical positions, roughly 120 paints/s and 24 logic ticks/s. Unity compiles, current Console 0 errors. Full combat visual play-through and Windows build not performed.

## NSO Workbench (2026-11-11)
Dev wiki upgraded: live player/map/quest/bag snapshots; persistent live loot settings; loot-condition inspector; redesigned responsive explorer with quest steps, cross-links, bookmarks/history, JSON export and read-only API console; session configuration log. Existing #quests/2 and /api remain. Default loot policy unchanged. Immutable per-world rules load from save-folder game-rules.json in every build; only the debug site writes them. Tests: webcheck 27/27, pure UI 18/18, lootcheck 9/9, questcheck 16/16, progresscheck 12/12, wiki 17 collections. Default suite retains 18 PASS / 2 known login FAIL. Unity compile/current Console clean; live GET endpoints verified with a player in Play. Browser automation unavailable; visual/click/mobile QA and release build remain unverified.

## Unsourced HP/MP recovery fixed (2026-11-11)
User reported HP recovering without items/effects. Confirmed offline NaturalRegenPercent=2 was the source; default is now 0. Java food/potion/equipment recovery remains. regencheck reproduced 7 failures before the fix and passes 9/9 after; questcheck 16/16 and progresscheck 12/12 still pass. Unity compilation/current Console clean. No actual player HP/inventory was forcibly changed during validation.

## Movement release delay fixed (2026-11-11)
Removed the manual ground-running friction tail on button release. Play-mode baseline at speed 8: 9 ticks / 0.375 seconds and 36 extra pixels; after fix: cvx=0, standing immediately, no extra distance in the same isolated probe. Arrow keys, PC A/D mapping, number-pad and pointer control release use the same guarded stop. Inspector Instant Movement Stop defaults true and can restore legacy inertia. Play-mode regression 18/18; Unity compilation/current Console clean. Physical keyboard/touch feel still needs user confirmation; no Windows build.

## Broad render-motion pass (2026-11-12)
Extended render-rate fractional placement to the world camera, player/remote characters/NPCs, monsters/pets, dropped items, arrows, monster darts/trails, lanterns, eggs, training figures, weather particles and Scroll content. Configured render cap range is now 60-144; logic remains 24 by default. Play sampling in the current village: 60.0 paints/s at cap 60 and 120.0 at cap 120, both 24.0 logic ticks/s. Regression: world/camera/scroll 94, effects 17/17, movement release 18/18, skill-hit 8/8. Unity compile/current Console clean. World and bag screenshots inspected; this is not all-map/combat frame-time certification. Original finite sprite poses and remaining paint-owned animation counters are not converted or certified as 60 unique poses/s.
