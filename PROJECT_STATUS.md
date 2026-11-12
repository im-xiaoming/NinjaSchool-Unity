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

## Scrolling notification presentation (2026-11-12)
Info and InfoMe now interpolate text placement at render rate using Smooth Effects. Original movement steps, timers and clipping remain. Final revision: Unity compilationFailed=false, consoleErrors=0. User requested compile-only validation and will judge visual smoothness.

## Mouse wheel conversion fix (2026-11-12)
Fixed input bridge dividing already-normalized Input System wheel notches by 120, which made Scroll round ordinary input to zero. Native Windows mode still divides by 120. Compile/Console validation only per user instruction; physical skill/item information scrolling awaits user verification.

## Workbench dark appearance (2026-11-12)
Local dev wiki now uses a dark palette, larger 15-17px reading text, heavier labels and Apple-first system font stacks with Windows fallbacks. Sidebar widened for larger navigation. Presentation files only; user handles browser visual QA.

## Unified workbench font (2026-11-12)
All web text now uses the same --font-ui stack, including code, JSON, IDs and keyboard hints. Removed the separate monospace family at user request.

## Global game-balance dashboard (2026-11-12)
Added #dashboard for durable whole-game balancing, per the user's clarified scope. Player stat/potential percent + flat adjustments and future EXP gain rate apply globally. Equipment option rules target all equipment or a selected template and affect old/new items without rewriting saved options. Monster template rules persist across maps/spawns/restarts. game-balance.json and monster-tuning.json load without the website. Only compile/syntax validation; user handles runtime/browser/restart debugging.

## Character progression refactor (2026-11-12)
Character balancing now has unclassed and classed groups, inherited shared/per-class profiles, editable original/reference Level-1 bases and native-growth parameters. A read-only draft preview calls the gameplay calculator for each selected level/class. Classed manual allocation and original integer/MP-floor formulas remain. Version-2 balance settings accept version 1 without rewriting saves. See docs/CHARACTER_PROGRESSION.md. Compile/syntax only; user performs runtime/browser checks.

## Progression formula help (2026-11-12)
Step 02 now explains every formula term, percent conversion, cumulative native growth, per-level additions, separate contributions, inheritance vs zero and classed zero-growth behavior. Added an illustrative Level-5 HP calculation and expandable attribute glossary. UI-only; node syntax checked, browser QA left to user.

## Separate wooden-sword progression sample (2026-11-12)
Step 03 now separates unclassed/no equipment, unclassed/quest wooden sword and actual classed characters. The wooden-sword row uses class 0, real quest item #194 and current equipment rules through SetAbility. Unity compile/current Console clean; JS syntax passed. User handles runtime/browser checks.

## Wiki access diagnosis (2026-11-12)
Reported inaccessible URL was stale: port 8787 listened but timed out; current ServerBootstrap.WikiUrl was http://127.0.0.1:8788/. Root, db.json, control/state, control/dashboard and progression.js returned HTTP 200 on 8788. Use the Server Control open-wiki button for the actual port. No gameplay/config mutation.

## Item/monster sprites in Workbench (2026-11-12)
Added original item icons and representative monster sprites to item/mob lists, details, related links and balance editors. Boss portraits compose a source frame from the original sprite sheet. Read-only local asset routes, lazy loading, missing-art fallback and bounded cache; no copied/generated bitmap assets or gameplay changes. Unity compile/current Console clean and JS syntax passed; browser/image HTTP verification left to user.

## F6 Workbench shortcut (2026-11-12)
Added F6 > Dev > Open balance web action using the OS default browser and current ServerBootstrap.WikiUrl/#dashboard, including fallback ports. Shows readiness/failure guidance in the existing help line. Compile-only validation; no actual browser launch tested.

## Progression layout correction (2026-11-12)
Removed the standalone top group-selection card per screenshot. Profile selection is compactly placed inside Step 01. Step 03 now has three independent boxes: Unclassed, Wooden Sword, Classed. UI-only, JS syntax verified; browser QA left to user.

## Growth panel collapsed by default (2026-11-12)
Step 02 is now a closed-by-default details panel. Clicking its heading expands the growth table/help. Nested formula guidance also defaults closed. Save/discard/reset stay outside the collapsed content. UI-only syntax verified.


## 2026-11-12 - Slow effect animation previews
- Added original status icons and a separate paginated animation gallery in the Effects tab. Status IDs are not assumed to map to animation IDs.
- Sources: `nj_effect` frame order, image IDs and offsets from `GameScr.readEfect` / `ServerEffect.paint`; allowlisted original Small images served by WikiSprites as zoom-normalized SVGs. No gameplay calculation or timing changes.
- Files: WikiData.cs, WikiSprites.cs, WikiServer.cs, ServerBootstrap.cs; NsoWiki/effects.js (+ Unity meta), sprites.js, app.js, index.html, app.css; project status/task/decision/handoff documents, ServerPort.md and notes.json.
- Presentation defaults: 4 frames/second, 12 cards/page, at most 6 visible animated canvases, one shared timer, 128-image client cache and existing 16 MiB server asset cache. Pause on hidden document, offscreen cards and route changes; respect reduced-motion preference at initial load. Manual pause/resume available. Missing art displays a notice.
- Validation: node --check passed for effects.js, sprites.js and app.js. Unity refreshed and finished compiling: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. No automated gameplay tests or Play/browser validation, per user compile-only instruction.
- Known limitations: names of standalone VFX are numeric source IDs; no invented association to status effects. Browser rendering/performance remains unverified.
- Next: start Play manually, open the workbench through F6 > Dev > Open web, visit Effects, check looping, pagination, pause/resume and scrolling. CLI validation: unity command script eval --file <refresh.cs> --project-path "C:/My project" --format json; unity command console status --project-path "C:/My project" --format json.
