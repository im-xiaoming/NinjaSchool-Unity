# Server port (Java → C#) status

The game server from the original Java source (`C:\NSO_OFFLINE\reference\server_java`) is being rewritten in C# under
`Assets/Server` and runs inside the game (in-memory pipe, no network). This page lists what is ported and how to test it.

## Ported
| Area | C# file | Java source |
|---|---|---|
| Sessions, login, character list/create/select | `Net/Session.cs`, `Net/Controller.cs`, `Store/AccountStore.cs` | `network/*`, `model/User.java` |
| Enter world, map info, scenery, items on the ground | `Net/EnterWorld.cs` | `Service.loadAll`, `sendZone`, `sendItemMap` |
| Walking, map/zone change, death, revive (back to the saved village) | `Net/PlayerActions.cs` | `Zone.move`, `Char.changeMap/startDie/returnTownFromDead` |
| Monsters notice and attack the player | `Net/MobBrain.cs` | `Mob.update`, `Mob.attack` |
| Player attacks monsters, damage, critical, miss, death, respawn, exp, level up | `Net/PlayerCombat.cs` | `Char.attackMonster`, `Char.addExp`, `Mob.die/recovery` |
| **Skills**: learn from books, raise skill points, raise potential, select skill, passive skill bonuses | `Net/PlayerSkills.cs` | `Char.learnSkill/upSkill/upPotential/selectSkill` |
| **Skill effects on monsters**: burn, freeze, stun, bind, disable, Kakyuu (sushi) | `PlayerCombat.Hit`, `PlayerEffects.AddMobEffect` | `Char.attackMonster(ArrayList)`, `Mob.update` |
| **Items**: bag, use (HP/MP potions, food), equip / unequip, item details | `Net/PlayerItems.cs` | `Char.useItem/useEquipment/itemBodyToBag/requestItemInfo` |
| **Shops**: shop lists, buying with xu / yen | `Net/PlayerItems.cs` | `Char.buyItem`, `Store.buy`, `Char.requestItem` |
| **Drops and pickup**: quest items, yen, random items, equipment; items vanish after 30 s | `Net/PlayerItems.cs` | `Mob.dead/dropItem`, `Char.pickItem`, `Zone.updateItemMap` |
| **HP/MP over time**: food, potions, equipment options | `Net/PlayerEffects.cs` | `EffectManager`, `Char.updateEveryHalfSecond/FiveSecond` |
| **Quests and NPC menus**: take, steps, counts, finish, rewards (quests 0–42), quiz, school admission | `Net/PlayerTasks.cs` | `Char.openMenu/initMenu/menu/getTask/takingTask/taskNext/finishTask` |
| Static data: quests, shops, skill templates, effects, item options, exp table | `World/World.cs`, `World/GameData.cs` | `GameData`, `StoreManager`, `Server.npcTasks` |
| Message helpers (dialogs, menus, quest/item/effect messages) | `Net/PlayerService.cs` | `Service`, `AbsService`, `MapService` |
| Saved progress: bag, equipment, xu, yen, quest step, skills, potentials, saved village | `Store/PlayerProgress.cs` (inside `PlayerRecord.Data`) | `players` columns |

### NPC menus ported
Tajima (quests, "Nhận kiếm gỗ"), Kiriko (potions), Tabemono (food), Kenshinto, Okanechan, Kamakura ("Lưu tọa độ"),
Umayaki (village teleports), the three teachers (Toyotomi, Ookamesama, Kazeto: "Nhập học"), Kanata (weapons),
Furoya (clothes), Ameji (jewellery), Goosho (shop, books, fashion). Other NPCs show their own menu and answer
"Chức năng này chưa có trong bản offline."

## Offline deviations (on purpose)
- **Natural regeneration**: `PlayerEffects.NaturalRegenPercent` (default 2 % of max HP/MP every 5 s). The original only
  regenerates from food, potions and equipment. Set it to 0 to play exactly like the original.
- **Quest steps whose mechanic is not ported** (dungeons, escorting Jaian, fishing, carrying water, daily tasks, friends,
  item upgrades, battlefield) can be skipped at the NPC that gives the quest: its menu shows "Bỏ qua: <step>". Talk steps
  on NPCs without a hand-ported menu get an entry named after the step.
- No luong (gold): items that cost luong cannot be bought.
- Maximum MP never goes below 100 (the value the Java server gives a new character).

## Not ported yet
Buff skills (`useSkillBuff`, skill types 2/3/4), item upgrade / gem / crystal windows, storage box, mounts, pets,
clone skills, special maps (dungeons, wars, events), friends / party / trade / clan.

## Tests
`Tools/ServerTests` is the original test suite pointed at this project's server (in-memory pipe instead of TCP).
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj"
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- combatcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- questcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- progresscheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- entercheck C:/NSO_OFFLINE/captures/enter_map22.txt 22
```
Results on 2026-11-11:
- `questcheck`: 16/16 pass. Quests 0–3 played through: talk to the six villagers, the quiz, the wooden sword, equipping
  it, 10 training-dummy kills, buying and eating rice balls.
- `progresscheck`: 12/12 pass. A quest item drops and is picked up, joining the sword school, reading the skill book,
  raising skill and potential points, wearing the school sword.
- `combatcheck`: all pass.
- `entercheck`: matches Java byte for byte.
- Full suite: 18 pass, 2 fail. The 2 failures are expected: this build always signs in to the one local account.
