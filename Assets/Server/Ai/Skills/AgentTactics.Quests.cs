using System;
using System.Collections.Generic;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using Zone = NsoServer.World.Zone;
    using Waypoint = NsoServer.World.Waypoint;

    /// <summary>
    /// Tactical AI (game AI) of an AI player: plays the current plan step every server tick, like a person at the
    /// controls. It only perceives the character's state (PlayerState: position, HP, bag, quest...) and acts through
    /// BotClient, i.e. the messages the game client sends; the server applies the player's rules to everything
    /// (MapUnlock on map changes, cooldown/MP/range on attacks, shop prices and quest gates, pickup ownership, bag space).
    /// Besides the goals there are habits every player has: drink a potion when HP is low, spend potential and skill
    /// points, wear equipment and read skill books from the bag, pick up its own loot, return to the village after death.
    /// Reports progress, completion and failure back through AgentManager (StepDone / StepFailed / Report).
    /// </summary>
    public static partial class AgentTactics
    {
        /// <summary>Without a weapon a character cannot attack (PlayerCombat.AttackMonster): take one from the bag, or
        /// ask Tajima for the wooden sword ("Nhận kiếm gỗ").</summary>
        private static Status GetSword(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            int i = PlayerItems.IndexInBag(me, ItemWoodenSword);
            if (i >= 0 && !a.Admitting)
            {
                a.Client.UseItem(i);
                return Status.Running;
            }

            string why;
            var go = GoToNpc(a, me, world, NpcTajima, "going to Tajima for a wooden sword", out why);
            if (go != Status.Done)
                return go == Status.Failed ? Status.Failed : Status.Running;
            if (now - a.LastMenuAt < ActionGapMs)
                return Status.Running;
            a.LastMenuAt = now;
            a.Client.OpenMenu(NpcTajima);
            int idx = me.Menus.FindIndex(e => e.Name == "Nhận kiếm gỗ");
            if (idx < 0)
                return Status.Failed;
            a.Client.Menu(NpcTajima, idx);
            AgentManager.Remember(a, "asked Tajima for a wooden sword");
            return Status.Running;
        }

        // ================================================================== quest play
        /// <summary>
        /// Plays the main quest like a player: talks to the quest NPC through its menu (take, report, "talk" steps,
        /// quizzes answered by trying and remembering), hunts the step's monsters or the ones dropping the step's
        /// item, buys and eats food, walks to the places, learns the school's book and spends points, joins a school.
        /// The server checks and counts everything (PlayerTasks). Steps the AI cannot play are skipped only where the
        /// quest giver offers it to every player ("Bỏ qua: ..."). Done when the current quest advanced to another one.
        /// </summary>
        /// <summary>What the "quest" goal does for the current step, in words for the LLM prompt: the NPC and where it
        /// stands, the monster to hunt and its map, the item it drops, the map to reach. Mirrors QuestPlay.Run.</summary>
        public static string QuestGuide(PlayerState me, GameWorld world)
        {
            int task = me.Record.TaskId;
            var t = world.FindTask(task);
            if (t == null)
                return "";
            int npc = PlayerTasks.TaskNpcId(me, world);
            if (me.Record.ClassId == 0 && task >= TaskGiaTangSucManh && !me.Progress.TaskActive)
                return me.Level >= 10 ? "join a school (the quest goal walks to the school NPC)" : "train to level 10, then join a school";
            if (!me.Progress.TaskActive)
                return "talk to " + NpcAt(world, me, npc) + " to take it";
            int idx = me.Progress.TaskIndex, last = t.SubNames.Length - 1;
            if (idx >= last)
                return "report to " + NpcAt(world, me, npc);
            if (idx == 0 && t.LevelRequire > 0 && me.Level < t.LevelRequire)
                return "train to level " + t.LevelRequire;
            if (idx < t.KillMobs.Length && t.KillMobs[idx][0] >= 0)
                return "kill " + MobAt(world, me, t.KillMobs[idx][0], idx);
            if (idx < t.PickItems.Length && t.PickItems[idx] >= 0)
            {
                var item = world.FindItem(t.PickItems[idx]);
                foreach (var d in PlayerTasks.TaskDropList())
                    if (d[1] == task && d[2] == idx)
                        return "kill " + MobAt(world, me, d[0], idx) + " and pick up the " + (item != null ? item.Name : "item") + " it drops";
            }

            if (task == TaskDietSenTruCoc && idx == 0)
                return "buy Cơm nắm at " + NpcAt(world, me, NpcTabemono);
            if (task == TaskDietSenTruCoc && idx == 1)
                return "eat the Cơm nắm (done automatically)";
            if (task == TaskKhamPha && idx >= 1 && idx <= 3)
            {
                int place = idx == 1 ? 2 : idx == 2 ? 71 : 26;
                return "walk into map " + place + " " + MapName(world, place);
            }

            if (npc >= 0)
                return "talk to " + NpcAt(world, me, npc);
            return "";
        }

        private static string NpcAt(GameWorld world, PlayerState me, int npc)
        {
            int map, x, y;
            if (npc < 0)
                return "the quest NPC";
            return NpcName(world, npc) + (WorldKnowledge.FindNpc(world, npc, me.MapId, me.Record.TaskId, out map, out x, out y) ? " (map " + map + " " + MapName(world, map) + " at " + x + "," + y + ")" : " (not reachable yet)");
        }

        private static string MobAt(GameWorld world, PlayerState me, int mob, int idx)
        {
            var m = world.FindMob(mob);
            var maps = world.TaskMaps(me.Record.TaskId);
            int map = WorldKnowledge.FindMobMap(world, mob, me.MapId, me.Record.TaskId, maps != null && idx + 1 < maps.Length ? maps[idx + 1] : -1);
            return (m != null ? m.Name : "monster") + " (mob " + mob + ")" + (map >= 0 ? " in map " + map + " " + MapName(world, map) : "");
        }

        private static class QuestPlay
        {
            public static Status Run(NinjaAgent a, PlayerState me, GameWorld world, long now, out string why)
            {
                why = null;
                int task = me.Record.TaskId;
                if (a.LastTaskId < 0)
                    a.LastTaskId = task;
                if (task != a.LastTaskId)
                {
                    a.LastTaskId = task;
                    return Status.Done;
                } // quest finished: the plan moves on

                var t = world.FindTask(task);
                if (t == null)
                {
                    why = "no more main quests";
                    return Status.Failed;
                }

                // join a school after "NV tìm hiểu 3 trường" (Admission: level 10, quest 9 not taken yet, no weapon in hand)
                if (me.Record.ClassId == 0 && task >= TaskGiaTangSucManh && !me.Progress.TaskActive)
                    return me.Level >= 10 ? JoinSchool(a, me, world, now, out why) : Hunt(a, me, world, -1, -1, 0, now, out why) == Status.Failed ? Status.Failed : Status.Running;
                a.Admitting = false;
                int npc = PlayerTasks.TaskNpcId(me, world);
                if (!me.Progress.TaskActive)
                {
                    a.Activity = "taking quest " + t.Name;
                    return Talk(a, me, world, npc, now, out why);
                }

                int idx = me.Progress.TaskIndex, last = t.SubNames.Length - 1;
                string stepName = idx < t.SubNames.Length ? t.SubNames[idx] : "";
                if (idx >= last)
                {
                    a.Activity = "reporting quest " + t.Name;
                    return Talk(a, me, world, npc, now, out why);
                }

                // level step: train until the level is reached (UpdateTaskLevelUp advances it)
                if (idx == 0 && t.LevelRequire > 0 && me.Level < t.LevelRequire)
                {
                    var h = Hunt(a, me, world, -1, -1, 0, now, out why);
                    a.Activity = "training for quest " + t.Name + " (level " + me.Level + "/" + t.LevelRequire + "): " + a.Activity;
                    return h == Status.Failed ? Status.Failed : Status.Running;
                }

                if (idx < t.KillMobs.Length && t.KillMobs[idx][0] >= 0)
                    return HuntFor(a, me, world, now, t.KillMobs[idx][0], idx, "quest " + t.Name + ": " + stepName, out why);
                if (idx < t.PickItems.Length && t.PickItems[idx] >= 0)
                {
                    foreach (var d in PlayerTasks.TaskDropList())
                        if (d[1] == task && d[2] == idx)
                            return HuntFor(a, me, world, now, d[0], idx, "quest " + t.Name + ": " + stepName, out why);
                }

                // hand-made steps every player does by hand
                if (task == TaskLanDauDungKiem && idx == 0)
                    return GetSword(a, me, world, now) == Status.Failed ? Fail(out why, "cannot get a weapon") : Status.Running;
                if (task == TaskDietSenTruCoc && idx == 0)
                {
                    int need = Math.Max(1, (idx < t.Counts.Length ? t.Counts[idx] : 2) - me.Progress.TaskCount);
                    var b = BuyFrom(a, me, world, now, NpcTabemono, ItemComNam, need, out why);
                    if (b == Status.Failed)
                        return EarnMoney(a, me, world, now, why, out why);
                    return Status.Running;
                }

                if (task == TaskDietSenTruCoc && idx == 1)
                {
                    int food = PlayerItems.IndexInBag(me, ItemComNam);
                    if (food >= 0 && now - a.LastMenuAt > ActionGapMs)
                    {
                        a.LastMenuAt = now;
                        a.Client.UseItem(food);
                    }

                    return Status.Running;
                }

                if (task == TaskKhamPha && idx >= 1 && idx <= 3)
                {
                    int place = idx == 1 ? 2 : idx == 2 ? 71 : 26; // PlayerTasks.OnChangeMap
                    a.Activity = "quest " + t.Name + ": " + stepName;
                    var tr = Travel(a, me, world, place, out why);
                    return tr == Status.Failed ? Status.Failed : Status.Running; // arriving advances the step
                }

                if (task == TaskGiaTangSucManh)
                {
                    a.Activity = "quest " + t.Name + ": " + stepName; // the habits read the book, then spend the points
                    if (now - a.StepStartedAt > 30000 && me.SkillPoint == 0 && me.PotentialPoint == 0)
                        return SkipOrFail(a, me, world, now, t, stepName, out why);
                    return Status.Running;
                }

                if (task == TaskNangCapTrangBi && idx >= 1 && idx <= 3)
                {
                    a.Activity = "quest " + t.Name + ": " + stepName;
                    return UpgradeStep(a, me, world, now, idx, out why);
                }

                // talk steps: the NPC of the step (Server.npcTasks)
                if (npc >= 0)
                {
                    a.Activity = "quest " + t.Name + ": " + stepName;
                    return Talk(a, me, world, npc, now, out why);
                }

                return SkipOrFail(a, me, world, now, t, stepName, out why);
            }

            // NV nâng cấp trang bị (Play mode 2026-11-13: every bot stopped here). Like a player: take a piece of the
            // step's kind (1 weapon, 2 adornment, 3 clothing) of level 10+ that the character wears or can wear, go to
            // Kenshinto, upgrade it with crystals (Char.upgradeItem, cmd 21: bag index, then crystal indexes; yen first),
            // then put it on - the step advances on equipping an upgraded piece (PlayerTasks.OnEquipUpgraded).
            private static Status UpgradeStep(NinjaAgent a, PlayerState me, GameWorld world, long now, int idx, out string why)
            {
                why = null;
                Func<ItemTemplateData, bool> kind = idx == 1 ? (Func<ItemTemplateData, bool>)PlayerUpgrade.IsWeapon : idx == 2 ? PlayerUpgrade.IsAdorn : PlayerUpgrade.IsClothe;
                var bag = me.Inventory.Bag;
                var eq = me.Inventory.Equipment;
                // 1) an upgraded piece in the bag: put it on (this is what completes the step)
                for (int i = 0; i < bag.Length; i++)
                {
                    var it = bag[i];
                    var tt = it != null ? world.FindItem(it.Id) : null;
                    if (tt == null || !kind(tt) || it.Upgrade <= 0 || a.FailedItems.Contains(it.Id))
                        continue;
                    if (now - a.LastMenuAt < ActionGapMs)
                        return Status.Running;
                    a.LastMenuAt = now;
                    a.Client.UseItem(i);
                    if (bag[i] == it)
                        a.FailedItems.Add(it.Id); // cannot wear it (class, gender, level)
                    else
                        AgentManager.Remember(a, "put on the upgraded " + tt.Name + " +" + it.Upgrade);
                    return Status.Running;
                }

                // 2) the piece to upgrade: a worn one first (known to fit), else one from the bag it can wear
                int slot = -1, bagIndex = -1;
                for (int s = 0; s < eq.Length; s++)
                {
                    var tt = eq[s] != null ? world.FindItem(eq[s].Id) : null;
                    if (tt != null && kind(tt) && tt.Fashion <= -1 && eq[s].Upgrade < PlayerUpgrade.UpMax(tt))
                    {
                        slot = s;
                        break;
                    }
                }

                if (slot < 0)
                    for (int i = 0; i < bag.Length; i++)
                    {
                        var it = bag[i];
                        var tt = it != null ? world.FindItem(it.Id) : null;
                        if (tt == null || !kind(tt) || tt.Fashion > -1 || it.Upgrade >= PlayerUpgrade.UpMax(tt) || a.FailedItems.Contains(it.Id))
                            continue;
                        if (tt.Level > me.Level || ((tt.Gender == 0 || tt.Gender == 1) && tt.Gender != me.Record.Gender))
                            continue;
                        bagIndex = i;
                        break;
                    }

                string what = idx == 1 ? "weapon" : idx == 2 ? "adornment (necklace, ring, jade or charm)" : "clothing (hat, coat, gloves, trousers or shoes)";
                if (slot < 0 && bagIndex < 0)
                { // nothing to upgrade yet: train, monsters drop gear (the equipment shops take xu)
                    a.Activity = "no " + what + " of level 10+ to upgrade - training for drops";
                    a.Knowledge.Learn("note:upgrade" + idx, "the upgrade quest needs a " + what + " of level 10 or more; monsters drop them, shops sell them for xu");
                    return Hunt(a, me, world, -1, -1, 0, now, out why) == Status.Failed ? Status.Failed : Status.Running;
                }

                // 3) crystals worth enough for the success cap (UpCrystal values, at most 18)
                var piece = slot >= 0 ? eq[slot] : bag[bagIndex];
                var pt = world.FindItem(piece.Id);
                int up = piece.Upgrade;
                int[] need = PlayerUpgrade.IsWeapon(pt) ? PlayerUpgrade.UpWeapon : PlayerUpgrade.IsAdorn(pt) ? PlayerUpgrade.UpAdorn : PlayerUpgrade.UpClothe;
                long coin = PlayerUpgrade.IsWeapon(pt) ? PlayerUpgrade.CoinUpWeapon[up] : PlayerUpgrade.IsAdorn(pt) ? PlayerUpgrade.CoinUpAdorn[up] : PlayerUpgrade.CoinUpClothe[up];
                long target = (long)Math.Ceiling(need[up] * PlayerUpgrade.MaxPercent[up] / 100.0);
                var stones = new List<int>();
                for (int i = 0; i < bag.Length; i++)
                    if (bag[i] != null && bag[i].Id <= 11 && bag[i].Quantity == 1)
                        stones.Add(i);
                if (stones.Count == 0)
                {
                    a.Activity = "no crystals to upgrade the " + what + " - training for drops";
                    a.Knowledge.Learn("note:upgrade-stones", "upgrading needs crystals (Đá cấp 1-12): monsters drop them; keep them, do not sell");
                    return Hunt(a, me, world, -1, -1, 0, now, out why) == Status.Failed ? Status.Failed : Status.Running;
                }

                if (coin > me.Progress.Yen + me.Progress.Xu)
                {
                    why = "not enough yen/xu to upgrade (" + coin + ")";
                    return Status.Failed;
                }

                stones.Sort((x, y) => PlayerUpgrade.UpCrystal[bag[y].Id].CompareTo(PlayerUpgrade.UpCrystal[bag[x].Id])); // biggest first
                var use = new List<int>();
                long sum = 0;
                foreach (int i in stones) // big ones that do not overshoot, then the smallest one that closes the gap
                    if (use.Count < 18 && sum + PlayerUpgrade.UpCrystal[bag[i].Id] <= target)
                    {
                        use.Add(i);
                        sum += PlayerUpgrade.UpCrystal[bag[i].Id];
                    }

                if (sum < target)
                    for (int k = stones.Count - 1; k >= 0; k--)
                        if (!use.Contains(stones[k]) && use.Count < 18)
                        {
                            use.Add(stones[k]);
                            sum += PlayerUpgrade.UpCrystal[bag[stones[k]].Id];
                            if (sum >= target)
                                break;
                        }

                // 4) to the forge
                var go = GoToNpc(a, me, world, NpcKenshinto, "going to Kenshinto to upgrade the " + pt.Name, out why);
                if (go != Status.Done)
                    return go;
                if (now - a.LastMenuAt < ActionGapMs)
                    return Status.Running;
                a.LastMenuAt = now;
                if (slot >= 0)
                { // the forge takes it from the bag; same tick, so the wear habit cannot put it back on first
                    a.Client.BodyToBag(slot);
                    bagIndex = Array.IndexOf(bag, piece);
                    if (bagIndex < 0)
                    {
                        why = "could not take off " + pt.Name + " (bag full?)";
                        return Status.Failed;
                    }
                }

                int before = piece.Upgrade;
                a.Client.Upgrade(bagIndex, use);
                a.LastProgressAt = now;
                AgentManager.Remember(a, (piece.Upgrade > before ? "upgraded " : "failed to upgrade ") + pt.Name + " to +" + piece.Upgrade + " with " + use.Count + " crystals worth " + sum + "/" + need[up] + (piece.Upgrade > before ? "" : " (" + a.Session.LastNotice + ")"));
                if (piece.Upgrade > before)
                    a.Knowledge.Learn("note:upgrade", "Kenshinto upgrades gear of level 10+ with crystals (" + pt.Name + " +" + piece.Upgrade + "); wear it afterwards for the quest");
                return Status.Running;
            }

            private static Status Fail(out string why, string text)
            {
                why = text;
                return Status.Failed;
            }

            // a kill / pick step: hunt that monster where it lives (the step's map first)
            private static Status HuntFor(NinjaAgent a, PlayerState me, GameWorld world, long now, int mob, int idx, string label, out string why)
            {
                why = null;
                var maps = world.TaskMaps(me.Record.TaskId);
                int pref = maps != null && idx + 1 < maps.Length ? maps[idx + 1] : -1;
                int map = WorldKnowledge.FindMobMap(world, mob, me.MapId, me.Record.TaskId, pref);
                if (map < 0)
                {
                    why = "monster " + mob + " lives in no unlocked map";
                    return Status.Failed;
                }

                int before = me.Progress.TaskCount;
                var h = Hunt(a, me, world, map, mob, 0, now, out why);
                if (me.Progress.TaskCount != before)
                    a.LastProgressAt = now;
                a.Activity = label + " (" + me.Progress.TaskCount + ") - " + a.Activity;
                return h == Status.Failed ? Status.Failed : Status.Running;
            }

            // a step the AI cannot play: the quest giver's "Bỏ qua" entry (offered to every player for unported steps)
            private static Status SkipOrFail(NinjaAgent a, PlayerState me, GameWorld world, long now, TaskTemplateData t, string stepName, out string why)
            {
                why = null;
                if (PlayerTasks.IsStepSupported(world, t, me.Progress.TaskIndex))
                {
                    why = "does not know how to do \"" + stepName + "\" of " + t.Name;
                    a.Knowledge.Learn("note:quest" + t.Id, "quest " + t.Name + ": stuck at \"" + stepName + "\"; train instead");
                    return Status.Failed;
                }

                var npcs = world.TaskNpcs(t.Id);
                int giver = npcs != null && npcs.Length > 0 ? npcs[0] : -1;
                if (giver < 0)
                    giver = PlayerTasks.TaskNpcId(me, world);
                a.Activity = "asking to skip \"" + stepName + "\"";
                return Talk(a, me, world, giver, now, out why, "Bỏ qua");
            }

            // quests need yen or xu (food): hunt for a while, money drops on the ground
            private static Status EarnMoney(NinjaAgent a, PlayerState me, GameWorld world, long now, string reason, out string why)
            {
                a.Knowledge.Learn("note:money", "needed money: " + reason + "; monsters drop yen (about 1 in 10 kills)");
                var h = Hunt(a, me, world, -1, -1, 0, now, out why);
                a.Activity = "earning money (" + reason + "): " + a.Activity;
                return h == Status.Failed ? Status.Failed : Status.Running;
            }

            /// <summary>Walks to the NPC and clicks through its menu: quest entries first (take + confirm, report,
            /// talk / quiz, or the entry named <paramref name = "prefer"/>). Running until the quest state changes.</summary>
            private static Status Talk(NinjaAgent a, PlayerState me, GameWorld world, int npc, long now, out string why, string prefer = null)
            {
                why = null;
                if (npc < 0)
                {
                    why = "no NPC for this step";
                    return Status.Failed;
                }

                var go = GoToNpc(a, me, world, npc, "walking to " + NpcName(world, npc), out why);
                if (go != Status.Done)
                    return go;
                if (now - a.LastMenuAt < ActionGapMs)
                    return Status.Running;
                a.LastMenuAt = now;
                // a quiz is open: me.Menus holds the answers (QuestionAndAnswer)
                if (me.Menus.Count > 0 && me.Menus[0].Kind >= MenuEntry.A && me.Menus[0].Kind <= MenuEntry.D)
                    return Answer(a, me, npc);
                int stateBefore = me.Record.TaskId * 1000 + (me.Progress.TaskActive ? 500 : 0) + me.Progress.TaskIndex;
                a.Client.OpenMenu(npc);
                int pick = -1;
                for (int i = 0; i < me.Menus.Count && pick < 0; i++)
                {
                    var e = me.Menus[i];
                    if (prefer != null ? e.Name.StartsWith(prefer, StringComparison.Ordinal) : e.Kind == MenuEntry.TakeTask || e.Kind == MenuEntry.FinishTask || e.Kind == MenuEntry.DoTask)
                        pick = i;
                }

                // NPCs with their own menu advance a "talk to ..." step through their "Nói chuyện" entry (PlayerTasks.NpcTalk)
                if (pick < 0 && prefer == null)
                    pick = me.Menus.FindIndex(e => e.Name == "Nói chuyện");
                if (pick < 0)
                {
                    why = "nothing to do with " + NpcName(world, npc) + " now" + (me.Menus.Count > 0 ? "" : " (no menu)");
                    me.Menus.Clear();
                    return Status.Failed;
                }

                var entry = me.Menus[pick];
                a.Client.Menu(npc, pick);
                if (entry.Kind == MenuEntry.TakeTask && me.Menus.Count > 0 && me.Menus[0].Kind == MenuEntry.ConfirmTakeTask)
                    a.Client.GetTask(npc, 0); // "Nhận"
                if (me.Menus.Count > 0 && me.Menus[0].Kind >= MenuEntry.A && me.Menus[0].Kind <= MenuEntry.D)
                    return Status.Running; // a quiz started
                int stateAfter = me.Record.TaskId * 1000 + (me.Progress.TaskActive ? 500 : 0) + me.Progress.TaskIndex;
                if (stateAfter != stateBefore)
                {
                    a.LastProgressAt = now;
                    AgentManager.OnQuestProgress(a, world, entry.Name, NpcName(world, npc));
                }

                return Status.Running;
            }

            // quiz: answers known from earlier tries first, otherwise the next untried one; a right answer is remembered
            private static Status Answer(NinjaAgent a, PlayerState me, int npc)
            {
                string key = "quiz:" + me.Record.TaskId + ":" + me.Progress.TaskIndex + ":" + me.NAnswer;
                var known = a.Knowledge.Get(key);
                int pick = -1;
                if (known != null)
                    pick = me.Menus.FindIndex(e => e.Name == known.Text);
                if (pick < 0)
                {
                    if (a.QuizAnswered != me.NAnswer)
                    {
                        a.QuizAnswered = me.NAnswer;
                        a.QuizChoice = 0;
                    }

                    pick = Math.Min(a.QuizChoice++, me.Menus.Count - 1);
                }

                string chosen = me.Menus[pick].Name;
                int before = me.NAnswer, task = me.Record.TaskId, idx = me.Progress.TaskIndex;
                a.Client.GetTask(npc, pick);
                bool right = me.NAnswer > before || me.Record.TaskId != task || me.Progress.TaskIndex != idx;
                if (right)
                {
                    a.Knowledge.Learn(key, chosen);
                    a.QuizChoice = 0;
                    AgentManager.Remember(a, "answered a quiz question right: " + chosen);
                }

                return Status.Running;
            }

            // Admission (PlayerTasks.Admission): the head of the chosen school; the weapon must be put away first
            private static Status JoinSchool(NinjaAgent a, PlayerState me, GameWorld world, long now, out string why)
            {
                why = null;
                int cls = Math.Max(1, Math.Min(6, a.P.ClassId));
                int npc = cls <= 2 ? NpcToyotomi : cls <= 4 ? NpcOokamesama : NpcKazeto;
                a.Admitting = true;
                var go = GoToNpc(a, me, world, npc, "going to " + NpcName(world, npc) + " to join the school", out why);
                if (go != Status.Done)
                    return go;
                if (now - a.LastMenuAt < ActionGapMs)
                    return Status.Running;
                a.LastMenuAt = now;
                if (me.Inventory.Equipment[1] != null)
                {
                    a.Client.BodyToBag(1);
                    return Status.Running;
                }

                a.Client.OpenMenu(npc);
                int join = me.Menus.FindIndex(e => e.Name == "Nhập học");
                if (join < 0)
                {
                    why = "no admission menu";
                    return Status.Failed;
                }

                a.Client.Menu(npc, join);
                if (me.Menus.Count >= 2)
                    a.Client.Menu(npc, (cls % 2 == 1) ? 0 : 1); // the sub-menu: first or second class
                if (me.Record.ClassId != 0)
                {
                    a.Admitting = false;
                    a.FailedItems.Clear(); // new class: the school's weapon and book can be used now
                    a.LastProgressAt = now;
                    AgentManager.Report(a, AgentEvent.Quest, "joined the school of " + NpcName(world, npc) + " as class " + me.Record.ClassId);
                    a.Knowledge.Learn("note:school", "joined " + NpcName(world, npc) + "'s school (class " + me.Record.ClassId + ") at level " + me.Level);
                }
                else
                    why = a.Session.LastNotice;
                return Status.Running;
            }
        }
    }
}
