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
- [~] **HP/MP over time:** food, potions, equipment options, and the offline natural regeneration (2 %/5 s).
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
- **Natural regeneration** (2 %/5 s): added on the user's request for "HP/MP over time". Set it to 0 if the user wants the original behaviour.
- **Number of save slots:** currently one local account with up to 3 characters.
- **Copying more image folders from `C:\NSO`** (for example effects, about 111 MB) needs approval.
