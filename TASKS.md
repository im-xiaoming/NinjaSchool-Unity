# TASKS.md

**Legend:**
- `[x]` done and verified as stated.
- `[~]` done in code; only compiled or tested automatically.
- `[ ]` not done.

Older completed tasks (online removal, title menu, fonts, mob art, performance pass, zone cap and others) are listed in `C:\NSO_OFFLINE\TASKS.md`.

## Completed (this project, 2026-11-11)
- [~] **Combat port:**
  - attack monsters (cmd 60) with damage, critical, miss and exp;
  - level up from the `others.exp` table;
  - monster respawn;
  - weapon requirement.
  - Tests: `combatcheck` 6/6.
- [~] **Skills:**
  - learning from books, skill points, potential points, selecting a skill, passive support options;
  - effects on monsters: burn (7/16 and fire skills), freeze (25/34, ice), stun (43, wind), bind (24), disable (40), Kakyuu sushi (4).
  - Tests: `progresscheck`.
- [~] **Items and shops:**
  - use, equip, unequip, potions, food, item info;
  - shops with xu and yen (needs task >= 3);
  - drops (quest items, yen, random items, equipment), pickup, 30 s expiry.
  - Tests: `questcheck`, `progresscheck`.
- [~] **HP/MP over time:** food, potions and equipment options. Unsourced offline natural recovery is disabled.
- [~] **Quests 0–42 and NPC menus:**
  - talk, kill, pick and level steps;
  - the quiz;
  - the wooden sword;
  - school admission;
  - teleports;
  - "Bỏ qua" (skip) for steps that are not ported.
  - Tests: `questcheck` 16/16 (quests 0–3), `progresscheck` 12/12.
- [~] **Progress save** in `PlayerRecord.Data` (`Store/PlayerProgress.cs`).
- [x] **Dev wiki:**
  - a website that starts with the game at 127.0.0.1:8787, with search, filters, record details and links;
  - a JSON API at `/api` for agents;
  - `NsoWiki/db.json` export;
  - the `wiki` test command.
  - Verified in Play mode, with curl and in the browser.
- [x] **Menu music** plays once per visit to the menu (`MenuAudio.menuMusicLoop = false`). Verified in Play mode.
- [x] **`README.md`** written.
- [x] **`walkcheck` repaired** for the in-memory pipe (`TestClient.HasMore`). walk_A, walk_C and walk_D match Java; walk_B differs only by the 5-zone cap.
- [x] **Handoff documents** created in `C:\My project`: AGENTS, PROJECT_STATUS, TASKS, DECISIONS, HANDOFF, `docs/`.

## Remaining (suggested order)
1. [ ] **Play the new gameplay by hand in the real client** (Editor Play mode):
   - create a character;
   - play quests 0–5;
   - buy in a shop;
   - pick up drops;
   - learn a skill at school.
   Fix protocol issues found. Ask the user for screenshots of anything that looks wrong.
2. [ ] **Buff skills** (skill types 2/3/4): `Char.useSkillBuff`.
3. [ ] **Unported quest mechanics** that are currently skipped: dungeons, escorting Jaian, fishing, carrying water, daily tasks, friends, upgrade, battlefield. The steps per quest (`status: skip`) are in the dev wiki `quests` collection.
4. [ ] **Item upgrade, gem and crystal windows; storage box; selling and throwing items.**
5. [ ] **Mounts, pets, clone skill, PvP.**
6. [ ] **Special maps:** dungeons, wars, events. Port them only when the user asks.
7. [ ] **Full English localization** of the gameplay text.
8. [ ] **Windows release build of `C:\My project`:**
   - adapt `C:\NSO_OFFLINE\tools\build_and_run.ps1` (source path, scene `Assets/Scenes/NinjaGame.unity`);
   - build;
   - run offline;
   - check that save and load survive a restart.
9. [ ] **Profile** release performance on the target PCs.
10. [ ] **Clean-ups:**
    - the dead auto-play code;
    - `ReLogin.cs`;
    - the unused `bad` local in `Tools/ServerTests/Program.cs`;
    - the unused `Controller.zoomLevel` field.

## Awaiting user decision (do not act without asking)
- Multiplayer-style features (friends, party, clan, trade, auction, ranking, mail, gift code) are KEPT in the protocol by the user's choice. Do not port or remove them unasked.
- **Luong (gold):** currently 0. Items priced in luong cannot be bought, and reviving in place needs luong.
- **Resolved: natural regeneration:** user requested no unsourced recovery; default is 0. The former 2 %/5 s addition is superseded.
- **Number of save slots:** currently one local account with up to 3 characters.
- **Copying more image folders from `C:\NSO`** (for example effects, about 111 MB) needs approval.

## Onboarding checkpoint (2026-11-11)
- [x] Review handoff, representative source, Git and current server-test baseline; recorded in `docs/UnityProjectContext.md`.
- No gameplay backlog item was completed or started. The user will assign the next task.

## Quest 2 bug follow-up (2026-11-11)
- [x] Reproduce and fix client hit dispatch skipped at high render FPS; verify normal client attacks kill a dummy and advance quest 2 to 1/10.
- [x] Add `Tools/verify_skill_hit_frame.cs`: 8/8 timing/once-per-cast/death regression checks in Play mode.
- [ ] User keyboard/mouse replay of the remaining quest-2 kills; full real-client quests 0-5/shop/school verification remains pending.

## Loot bug follow-up (2026-11-11)
- [x] Exclude training dummies and mobs at least 10 levels below the killer from all loot paths, per clarified user request.
- [x] Add lootcheck: nine boundary/config/blocked-path checks; questcheck and progresscheck remain passing.

## High-FPS effect presentation (2026-11-11)
- [x] Smooth floating damage numbers, hit splash motion and F1 menu rows between ticks; tween visible mob HP-bar width.
- [x] Recent menu reopening now has a configurable minimum opening; selection waits for the popup to appear. Avoid per-caption temporary arrays in menu paint.
- [x] Play checks: verify_effect_presentation 17/17, verify_skill_hit_frame 8/8, runtime sampling at about 120 paints/s, current Console clean.
- [ ] User visual confirmation during real combat and repeated F1 use; limited source sprite poses were not replaced or synthesized.

## Workbench upgrade (2026-11-11)
- [x] Web switches for dummy/overlevel exclusions, cutoff and separate ordinary-loot level gate; explicit save, revision conflict, persistence and defaults as a draft.
- [x] Live player/quest/map/bag overview and loot-condition inspector using actual policy.
- [x] Responsive UI, full details, quest timeline, cross-links, search/filter/paging, browser-local bookmarks/recent, JSON copy/export, config history and read-only API tool.
- [x] API/persistence/integration checks 27/27 and pure UI checks 18/18; nearby gameplay tests and current Unity compile pass as documented.
- [ ] Browser visual/click desktop/mobile checks (automation host unavailable); release-player saved-policy check.

## Recovery correction (2026-11-11)
- [x] Disable unsolicited natural HP/MP recovery; default NaturalRegenPercent=0 supersedes the former 2% addition.
- [x] Add regencheck 9/9: source-free boundaries, food, potion, equipment intervals, expired food, caps and death; nearby quest/progress tests pass.

## Movement release correction (2026-11-11)
- [x] Reproduce release inertia: speed 8, 9 ticks at 24 Hz, 36 extra pixels.
- [x] Stop grounded manual running on final horizontal release; retain held aliases, destination paths, airborne and remote movement. Configurable Instant Movement Stop.
- [x] Play regression 18/18 and repeated baseline probe: zero residual velocity/ticks/distance; Unity compilation/current Console clean.
- [ ] User confirmation with physical keyboard and on-screen movement buttons; no Windows build performed.

## Broad smooth-motion request (2026-11-12)
- [x] Preserve gameplay tick rate; render cap configurable from 60 to 144.
- [x] Add fractional camera/entity/item/projectile/pet/weather and list-content painting, scoped/restored placement, map/teleport/recycle guards.
- [x] Play checks: 94 world/camera/scroll, 17 effect, 18 release, 8 skill-hit checks. Sample actual 60/120 paints per second, 24 logic ticks per second; inspect world/bag screenshots and Console.
- [ ] User visual confirmation on physical display/input; representative combat/map and frame-time percentile profiling; Windows build.
- [ ] A universal 60-FPS/60-unique-pose claim is NOT established. Original sprite pose sets remain finite; original paint-owned pose/effect counters (including Char.paintCharWithSkill) retain existing behavior. New artwork or a gameplay-sensitive timeline redesign needs a separate scoped decision/validation.

## 2026-11-12
- [~] **Double-click = Enter** on world targets (monster, NPC, player, ground item). `GameScr.onTargetClicked`, config `NsoGameControl._doubleClickMs`. Unity compile clean; `Tools/verify_double_click.cs` 6/6 in Play. Hand test by the user pending.
- [~] **Gamepad (PS5) input**, Elden Ring layout: `Assets/Scripts/Input/NsoGamepadInput.cs`. Sends the same key codes as the keyboard. Unity compile clean; hand test by the user pending.
- [x] Buy-quantity dialog: keyboard typing works, Left/Right choose the 3 buttons, Up/Down step the number (shared Dialog base). Needs user test.
- [x] Removed auto panel extras after 'Không nhặt gì cả' and their code (user request). Needs user test.
