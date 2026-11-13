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
    public static class AgentTactics
    {
        private static readonly Random Rng = new Random();
        private enum Status { Running, Done, Failed }

        // NPC templates (PlayerTasks): Kiriko sells potions, Tabemono food, Tajima gives the wooden sword; school heads
        private const int NpcKiriko = 3, NpcTabemono = 4, NpcKenshinto = 6, NpcTajima = 12, NpcToyotomi = 9, NpcOokamesama = 10, NpcKazeto = 11;
        // shop types (PlayerTasks.ShopPotion / ShopPotionLock / ShopFood / ShopFoodLock)
        private const int ShopPotionXu = 6, ShopPotionYen = 7, ShopFoodXu = 8, ShopFoodYen = 9;
        private const int ItemComNam = 23, ItemWoodenSword = 194;
        private const int TaskLanDauDungKiem = 2, TaskDietSenTruCoc = 3, TaskKhamPha = 6, TaskGiaTangSucManh = 9, TaskNangCapTrangBi = 12;
        private const int ActionGapMs = 900;   // a player needs about a second per menu click

        // ================================================================== tick

        /// <summary>One tick (500 ms) of the AI player.</summary>
        public static void Update(NinjaAgent a, GameWorld world, long now)
        {
            var me = a.Me;
            if (me == null || me.Zone == null) return;
            if (me.IsDead)
            {
                a.Activity = "dead, waiting to return to the village";
                if (a.DeadSince == 0) { a.DeadSince = now; AgentManager.OnDied(a, world); }
                else if (now - a.DeadSince > Math.Max(1, AgentConfig.ReviveAfterSeconds) * 1000L) a.Client.ReturnTown();   // Service.returnTownFromDead
                return;
            }
            if (a.DeadSince != 0) { a.DeadSince = 0; AgentManager.Report(a, AgentEvent.Respawned, "came back to life in map " + me.MapId + " " + me.Zone.Tile.Name); }
            if (me.IsChangeMap) return;

            Habits(a, me, world, now);

            // reflex: low HP overrides the plan (the plan's fleeHp is the strategic constraint)
            // Without potions HP does not come back, so the retreat starts earlier (NoPotionFleeHpPercent) while the
            // shop is affordable: walking to Kiriko through monsters with 20 % HP was the usual death.
            int fleeAt = a.FleeHp;
            if (HpPotions(me, world) == 0 && CanAffordPotions(me, world)) fleeAt = Math.Max(fleeAt, AgentConfig.NoPotionFleeHpPercent);
            if (!a.Recovering && HpPercent(me) < fleeAt && !DrinkHp(a, me, world, now) && now >= a.NoPotionsUntil)
            {
                a.Recovering = true;
                AgentManager.Report(a, AgentEvent.LowHp, "HP fell to " + HpPercent(me) + "% with no potion left");
            }
            if (a.Recovering)
            {
                if (Recover(a, me, world, now) != Status.Running) a.Recovering = false;
                return;
            }

            var step = a.Step;
            if (TryLootHabit(a, me, world, now)) return;
            if (step == null)
            {
                Wander(a, me, world, 120);
                a.Activity = a.Pending ? "waiting for a decision" : "idle";
                return;
            }
            if (now - Math.Max(a.StepStartedAt, a.LastProgressAt) > Math.Max(10, AgentConfig.StepTimeoutSeconds) * 1000L)
            {
                AgentManager.StepFailed(a, world, now, "no progress for " + AgentConfig.StepTimeoutSeconds + " s (" + step + ")");
                return;
            }
            string why = null;
            Status st;
            switch (step.Goal)
            {
                case GoalType.Travel: st = Travel(a, me, world, step.MapId, out why); a.Activity = "travelling to map " + step.MapId; break;
                case GoalType.Hunt: st = Hunt(a, me, world, step.MapId, step.MobId, step.Count, now, out why); break;
                case GoalType.Collect: st = Collect(a, me, world, step, now); break;
                case GoalType.Rest: st = Recover(a, me, world, now); break;
                case GoalType.Shop: st = step.MobId >= 0 ? TryItem(a, me, world, now, step.MobId, out why) : Shop(a, me, world, now, out why); break;
                case GoalType.Quest: st = QuestPlay.Run(a, me, world, now, out why); break;
                case GoalType.Explore: st = Explore(a, me, world, step, now, out why); break;
                case GoalType.Move: st = MoveTo(a, me, world, step, out why); break;
                default: st = Idle(a, me, world, step, now); break;
            }
            if (st == Status.Done) AgentManager.StepDone(a, world, now);
            else if (st == Status.Failed) AgentManager.StepFailed(a, world, now, why ?? "failed");
        }

        // LLM "goto": travel to the map, then walk/jump to the coordinates over the platforms (BotNavigation).
        private static Status MoveTo(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, out string why)
        {
            why = null;
            if (step.MapId >= 0 && me.MapId != step.MapId)
            {
                a.Activity = "travelling to map " + step.MapId + " to reach (" + step.X + "," + step.Y + ")";
                return Travel(a, me, world, step.MapId, out why) == Status.Failed ? Status.Failed : Status.Running;
            }
            var grid = world.GetGrid(me.Zone.Tile);
            int x = Math.Max(30, Math.Min(grid.PxW - 31, step.X));
            int y = step.Y < 0 ? -1 : Math.Max(0, Math.Min(grid.PxH - 1, step.Y));
            a.Activity = "going to (" + x + "," + (y < 0 ? "any" : y.ToString()) + ")";
            if (WalkTo(a, me, world, x, y) || (Math.Abs(me.X - x) <= 24 && (y < 0 || Math.Abs(me.Y - y) <= 40)))
            {
                a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return Status.Done;
            }
            if (a.Client.CannotReach(x, y))
            {
                why = "no way from (" + me.X + "," + me.Y + ") to (" + x + "," + y + ") on map " + me.MapId;
                a.Knowledge.Learn("note:unreachable" + me.MapId + "_" + x / 48 + "_" + (y < 0 ? 0 : y / 48), "map " + me.MapId + ": (" + x + "," + y + ") cannot be reached from (" + me.X + "," + me.Y + ")");
                return Status.Failed;
            }
            return Status.Running;
        }

        public static int HpPercent(PlayerState me) { return me.MaxHp <= 0 ? 0 : (int)((long)me.Hp * 100 / me.MaxHp); }
        public static int MpPercent(PlayerState me) { return me.MaxMp <= 0 ? 0 : (int)((long)me.Mp * 100 / me.MaxMp); }

        // ================================================================== habits

        // what any player does between fights: points, gear, books, the right skill selected, MP potions
        private static void Habits(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (MpPercent(me) < AgentConfig.DrinkMpPercent && me.SelectedSkill != null && me.SelectedSkill.ManaUse > 0) DrinkMp(a, me, world, now);
            if (now - a.LastEquipCheck < 2500) return;
            a.LastEquipCheck = now;
            Eat(a, me, world, now);
            bool quest9 = me.Record.TaskId == TaskGiaTangSucManh && me.Progress.TaskActive;
            int idx9 = quest9 ? me.Progress.TaskIndex : -1;
            // skill points to the main attack skill (NV gia tăng sức mạnh wants a book first)
            if (me.SkillPoint > 0 && (!quest9 || idx9 == 1))
            {
                var best = BestAttackSkill(me, world, true);
                if (best >= 0) a.Client.UpSkill(best, 1);
            }
            // potential: mostly strength (damage), some HP for careful characters (index 0 strength, 2 HP)
            if (me.PotentialPoint > 0 && (!quest9 || idx9 == 2))
            {
                int hp = (int)Math.Round(me.PotentialPoint * (0.2 + a.P.Caution * 0.4));
                if (hp > 0) a.Client.UpPotential(2, hp);
                if (me.PotentialPoint > 0) a.Client.UpPotential(0, me.PotentialPoint);
            }
            // gear and books from the bag (the server checks class, gender and level like for any player)
            if (!a.Admitting) WearAndRead(a, me, world);
            if (!a.Admitting) SellJunk(a, me, world);
            if (me.SelectedSkill == null || !IsAttack(world, me.SelectedSkill.TemplateId))
            {
                var best = BestAttackSkill(me, world, false);
                a.Client.SelectSkill(best >= 0 ? best : 0);
            }
        }

        private static bool IsAttack(GameWorld world, int templateId)
        {
            var t = world.FindSkillTemplate(templateId);
            return t != null && t.Type == PlayerSkills.TypeAttack;
        }

        // the learned attack skill with the highest point (forUpgrade: one that can still go up)
        private static int BestAttackSkill(PlayerState me, GameWorld world, bool forUpgrade)
        {
            int best = -1, bestPoint = -1;
            foreach (var s in me.Progress.Skills)
            {
                var t = world.FindSkillTemplate(s[0]);
                if (t == null || t.Type != PlayerSkills.TypeAttack) continue;
                if (forUpgrade && (s[1] >= t.MaxPoint || (s[0] >= 67 && s[0] <= 72))) continue;
                if (forUpgrade)
                {
                    var next = world.FindSkill(s[0], s[1] + 1);
                    if (next == null || next.Level > me.Level) continue;
                }
                if (s[1] > bestPoint) { bestPoint = s[1]; best = s[0]; }
            }
            return best;
        }

        private static void WearAndRead(NinjaAgent a, PlayerState me, GameWorld world)
        {
            var bag = me.Inventory.Bag;
            for (int i = 0; i < bag.Length; i++)
            {
                var it = bag[i];
                if (it == null || a.FailedItems.Contains(it.Id)) continue;
                var t = world.FindItem(it.Id);
                if (t == null) continue;
                bool wear = PlayerService.IsWearable(t) && t.Type < me.Inventory.Equipment.Length && me.Inventory.Equipment[t.Type] == null
                            && t.Level <= me.Level && (t.Gender == 2 || t.Gender == me.Record.Gender);
                bool book = PlayerSkills.BookClass(world, it.Id) >= 1 && PlayerSkills.BookClass(world, it.Id) == me.Record.ClassId;
                if (!wear && !book) continue;
                int id = it.Id;
                a.Client.UseItem(i);
                if (bag[i] != null && bag[i].Id == id && bag[i] == it) a.FailedItems.Add(id);   // refused: do not try again
                else AgentManager.Remember(a, (wear ? "put on " : "read ") + t.Name);
                return;   // one item per check, like a player clicking
            }
        }

        // Play mode 2026-11-13: bags filled with loose stones and spare gear, then the server refused finishing
        // "Hái thuốc" (Char.finishTask needs 2 free slots) and joining a school (Char.nhapHoc needs 2) - the bots
        // repeated the menu forever. Like a player clearing the bag, sell one junk item per check (Char.saleItem)
        // while fewer than AgentConfig.BagReserveSlots are free: stones first (the AI never upgrades), then gear
        // it will never wear. Potions, food, quest items and "other" items (bags, travel scrolls) are kept.
        private static void SellJunk(NinjaAgent a, PlayerState me, GameWorld world)
        {
            if (AgentConfig.BagReserveSlots <= 0 || PlayerItems.SlotNull(me) >= AgentConfig.BagReserveSlots) return;
            // until NV nâng cấp trang bị is done its crystals and upgradable gear (level 10+, this gender) are kept
            bool upgradeQuest = me.Record.TaskId <= TaskNangCapTrangBi;
            var bag = me.Inventory.Bag; var eq = me.Inventory.Equipment;
            int pick = -1, pickRank = int.MaxValue;
            for (int i = 0; i < bag.Length; i++)
            {
                var it = bag[i];
                if (it == null || a.FailedItems.Contains(-1 - it.Id)) continue;
                var t = world.FindItem(it.Id);
                if (t == null) continue;
                int rank;
                if (upgradeQuest && (t.Type == 26 || (PlayerService.IsWearable(t) && PlayerUpgrade.UpMax(t) > 0 && (t.Gender == 2 || t.Gender == me.Record.Gender)))) continue;
                if (t.Type == 26) rank = 0;                                   // stones
                else if (PlayerService.IsWearable(t) && t.Type != 12 && it.Upgrade == 0)
                {
                    bool mine = t.Gender == 2 || t.Gender == me.Record.Gender;
                    bool slotFree = t.Type < eq.Length && eq[t.Type] == null;
                    if (mine && slotFree) continue;                           // WearAndRead puts it on once the level allows
                    rank = 1;
                }
                else continue;
                if (rank < pickRank) { pick = i; pickRank = rank; }
            }
            if (pick < 0) return;
            var item = bag[pick]; var tpl = world.FindItem(item.Id);
            int qty = item.Quantity < 1 ? 1 : item.Quantity;
            a.Client.Sell(pick, qty);
            if (bag[pick] == item) { a.FailedItems.Add(-1 - item.Id); return; }   // refused: never retry this item
            AgentManager.Remember(a, "sold " + (qty > 1 ? qty + " " : "") + tpl.Name + " to free bag space");
        }

        private static int CountType(PlayerState me, GameWorld world, int type)
        {
            int n = 0;
            foreach (var it in me.Inventory.Bag)
            {
                if (it == null) continue;
                var t = world.FindItem(it.Id);
                if (t != null && t.Type == type) n += t.IsUpToUp ? it.Quantity : 1;
            }
            return n;
        }

        public static int HpPotions(PlayerState me, GameWorld world) { return CountType(me, world, PlayerItems.TypeHp); }
        public static int MpPotions(PlayerState me, GameWorld world) { return CountType(me, world, PlayerItems.TypeMp); }

        private static int FindType(PlayerState me, GameWorld world, int type)
        {
            var bag = me.Inventory.Bag;
            int best = -1, bestLevel = -1;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] == null) continue;
                var t = world.FindItem(bag[i].Id);
                if (t != null && t.Type == type && t.Level <= me.Level && t.Level > bestLevel) { best = i; bestLevel = t.Level; }
            }
            return best;
        }

        // an HP potion heals over 3 s (effect 21); one at a time
        // User 2026-11-13: "use a stronger item when it works and is needed". The cheapest potion that covers what is
        // missing; the strongest when in danger (HP under half the flee level, a boss on it) or when none covers it.
        public static int ChoosePotion(PlayerState me, GameWorld world, int type, bool urgent)
        {
            var bag = me.Inventory.Bag;
            int missing = type == PlayerItems.TypeHp ? me.MaxHp - me.Hp : me.MaxMp - me.Mp;
            int strongest = -1, strongestAmount = -1, fit = -1, fitAmount = int.MaxValue;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] == null) continue;
                var t = world.FindItem(bag[i].Id);
                if (t == null || t.Type != type || t.Level > me.Level) continue;
                // HP: 6 half-second ticks of the effect; unknown ids rank by level
                int amount = type == PlayerItems.TypeHp ? PlayerItems.HpPotionParam(bag[i].Id) * 6 : PlayerItems.MpPotionAmount(bag[i].Id);
                if (amount <= 0) amount = 1 + t.Level;
                if (amount > strongestAmount) { strongestAmount = amount; strongest = i; }
                if (amount >= missing && amount < fitAmount) { fitAmount = amount; fit = i; }
            }
            return urgent || fit < 0 ? strongest : fit;
        }

        private static bool UnderBossAttack(PlayerState me)
        {
            foreach (var m in me.Zone.Mobs) if (!m.IsDead && m.IsBoss && m.Chars.Contains(me.Record.Id)) return true;
            return false;
        }

        private static bool DrinkHp(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            foreach (var e in me.Effects) if (e.TemplateId == 21 && !e.IsExpired(now)) return true;
            if (now - a.LastPotionAt < 1000) return true;
            int flee = a.Plan != null && a.Plan.FleeHpPercent > 0 ? a.Plan.FleeHpPercent : AgentConfig.DefaultFleeHpPercent;
            int i = ChoosePotion(me, world, PlayerItems.TypeHp, HpPercent(me) < flee / 2 || UnderBossAttack(me));
            if (i < 0) return false;
            a.LastPotionAt = now;
            a.Client.UseItem(i);
            return true;
        }

        private static bool DrinkMp(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (now - a.LastPotionAt < 1000) return true;
            int i = ChoosePotion(me, world, PlayerItems.TypeMp, UnderBossAttack(me));
            if (i < 0) return false;
            a.LastPotionAt = now;
            a.Client.UseItem(i);
            return true;
        }

        // Food (PlayerItems.UseItem, TypeEat): HP and MP every half second for 30 minutes. Players keep one running all
        // the time; a new serving replaces the running one, so the AI only eats when none is active.
        public static bool Fed(PlayerState me, long now)
        {
            foreach (var e in me.Effects) if (e.Type == 0 && !e.IsExpired(now)) return true;
            return false;
        }

        public static int Foods(PlayerState me, GameWorld world) { return CountType(me, world, PlayerItems.TypeEat); }

        private static void Eat(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (Fed(me, now) || now - a.LastPotionAt < 1000) return;
            int i = FindType(me, world, PlayerItems.TypeEat);
            if (i < 0) return;
            a.LastPotionAt = now;
            a.Client.UseItem(i);   // the client reports what eating did (BotClient.Observe)
        }

        // ================================================================== spending style (thrift)

        // User 2026-11-13: "players may save money or not". The greed trait is the spending style: a thrifty character
        // (greed 1) carries fewer potions and keeps more money back; a spender (greed 0) stocks up and keeps little.
        // Greed 0.5 gives exactly the configured AgentConfig.PotionStock / FoodMoneyReserve.
        public static int PotionTarget(NinjaAgent a)
        {
            float g = a != null && a.P != null ? a.P.Greed : 0.5f;
            return Math.Max(Math.Max(2, AgentConfig.MinPotionsToFight), (int)Math.Round(AgentConfig.PotionStock * (1.3f - 0.6f * g)));
        }
        public static long MoneyReserve(NinjaAgent a)
        {
            float g = a != null && a.P != null ? a.P.Greed : 0.5f;
            return (long)Math.Round(AgentConfig.FoodMoneyReserve * (0.5f + 1.5f * g) / 1.25f);
        }

        /// <summary>The best food (highest level the character may eat) of the Tabemono shops it can pay for while
        /// keeping FoodMoneyReserve for potions: yen shop first, then xu.</summary>
        public static bool PickFood(PlayerState me, GameWorld world, out int shop, out int index, out int price) { return PickFood(me, world, AgentConfig.FoodMoneyReserve, out shop, out index, out price); }
        public static bool PickFood(PlayerState me, GameWorld world, long reserve, out int shop, out int index, out int price)
        {
            shop = index = price = -1;
            if (me.Record.TaskId < TaskDietSenTruCoc || AgentConfig.FoodStock <= 0) return false;
            foreach (int s in new[] { ShopFoodYen, ShopFoodXu })
            {
                var store = world.Store(s);
                if (store == null) continue;
                int best = -1, bestLevel = -1;
                long money = (s == ShopFoodYen ? me.Progress.Yen : me.Progress.Xu) - reserve;
                for (int i = 0; i < store.Count; i++)
                {
                    var t = world.FindItem(store[i].ItemId);
                    if (t == null || t.Type != PlayerItems.TypeEat || t.Level > me.Level) continue;
                    long cost = s == ShopFoodYen ? store[i].Yen : store[i].Coin;
                    if (cost <= 0 || cost > money) continue;
                    if (t.Level > bestLevel) { bestLevel = t.Level; best = i; }
                }
                if (best >= 0) { shop = s; index = best; price = s == ShopFoodYen ? store[best].Yen : store[best].Coin; return true; }
            }
            return false;
        }

        /// <summary>True when the AI should go and buy food: none in the bag, none running, affordable, not refused lately.</summary>
        public static bool WantsFood(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            int shop, index, price;
            return now >= a.NoFoodUntil && Foods(me, world) == 0 && !Fed(me, now) && PickFood(me, world, MoneyReserve(a), out shop, out index, out price);
        }

        // Shop step, second part: Tabemono's food once the potions are stocked. Never fails the step (food is a bonus).
        private static Status BuyFood(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            int shop, index, price;
            if (now < a.NoFoodUntil || Foods(me, world) > 0 || !PickFood(me, world, MoneyReserve(a), out shop, out index, out price)) return Status.Done;
            string why;
            var go = GoToNpc(a, me, world, NpcTabemono, "going to Tabemono to buy food", out why);
            if (go == Status.Failed) { a.NoFoodUntil = now + 300000; return Status.Done; }
            if (go != Status.Done) return go;
            if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
            a.LastMenuAt = now;
            long money = (shop == ShopFoodYen ? me.Progress.Yen : me.Progress.Xu) - MoneyReserve(a);
            int qty = (int)Math.Min(AgentConfig.FoodStock, money / Math.Max(1, price));
            if (qty <= 0) return Status.Done;
            a.Client.RequestShop(shop);
            a.Client.Buy(shop, index, qty);
            int got = Foods(me, world);
            string item = world.FindItem(world.Store(shop)[index].ItemId).Name;
            if (got <= 0) { a.NoFoodUntil = now + 300000; AgentManager.Remember(a, "Tabemono refused " + item + ": " + a.Session.LastNotice); return Status.Done; }
            AgentManager.Report(a, AgentEvent.Shop, "bought " + got + " " + item + " from Tabemono for " + (long)got * price + (shop == ShopFoodYen ? " yen" : " xu"));
            a.Knowledge.Learn("place:food", "Tabemono sells food (" + item + " " + price + (shop == ShopFoodYen ? " yen" : " xu") + ": HP and MP back every half second for 30 min) in map " + me.MapId + " " + me.Zone.Tile.Name);
            return Status.Done;
        }

        // ================================================================== recovering and shopping

        // HP is low: drink while potions last; otherwise buy some (village shop), otherwise get away from monsters.
        // There is no free regeneration for players (PlayerEffects.NaturalRegenPercent), so none for the AI either.
        private static Status Recover(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (HpPercent(me) >= 90) { a.Activity = "recovered"; return Status.Done; }
            if (DrinkHp(a, me, world, now)) { a.Activity = "drinking a potion (HP " + HpPercent(me) + "%)"; return Status.Running; }
            string why;
            if (CanAffordPotions(me, world))
            {
                var s = Shop(a, me, world, now, out why);
                return s == Status.Failed ? Retreat(a, me, world, now, why) : Status.Running;
            }
            return Retreat(a, me, world, now, "no potions and no money for them");
        }

        private static Status Retreat(NinjaAgent a, PlayerState me, GameWorld world, long now, string why)
        {
            Mob threat = null;
            int best = int.MaxValue;
            foreach (var mob in me.Zone.Mobs)
            {
                if (mob.IsDead || !mob.Chars.Contains(me.Record.Id)) continue;
                int d = Math.Abs(mob.X - me.X) + Math.Abs(mob.Y - me.Y);
                if (d < AgentConfig.SafeDistancePx && d < best) { best = d; threat = mob; }
            }
            if (threat != null)
            {
                a.Activity = "backing away from " + threat.Template.Name + " (" + why + ")";
                int dir = me.X >= threat.X ? 1 : -1;
                var grid = world.GetGrid(me.Zone.Tile);
                int tx = me.X + dir * 200;
                if (tx < 40 || tx > grid.PxW - 40) tx = me.X - dir * 400;   // cornered: run past it
                WalkTo(a, me, world, tx, -1);
                return Status.Running;
            }
            a.NoPotionsUntil = now + 60000;
            AgentManager.Report(a, AgentEvent.Note, "out of danger with HP " + HpPercent(me) + "% (" + why + ")");
            a.Knowledge.Learn("note:potions", "HP does not come back by itself; potions (Kiriko, from quest 3 on) or dying are the only ways to heal");
            return Status.Done;
        }

        private static bool CanAffordPotions(PlayerState me, GameWorld world)
        {
            if (me.Record.TaskId < TaskDietSenTruCoc) return false;   // Store.buy needs NV diệt sên trừ cóc (PlayerItems.BuyItem)
            int id; int price; int shop;
            return PickPotion(me, world, PlayerItems.TypeHp, out shop, out id, out price);
        }

        // the best potion of a kind the character can use and pay for (yen shop first, then xu)
        private static bool PickPotion(PlayerState me, GameWorld world, int type, out int shop, out int index, out int price)
        {
            foreach (int s in new[] { ShopPotionYen, ShopPotionXu })
            {
                var store = world.Store(s);
                if (store == null) continue;
                int best = -1, bestLevel = -1;
                for (int i = 0; i < store.Count; i++)
                {
                    var t = world.FindItem(store[i].ItemId);
                    if (t == null || t.Type != type || t.Level > me.Level) continue;
                    long cost = s == ShopPotionYen ? store[i].Yen : store[i].Coin;
                    long money = s == ShopPotionYen ? me.Progress.Yen : me.Progress.Xu;
                    if (cost <= 0 || cost > money) continue;
                    if (t.Level > bestLevel) { bestLevel = t.Level; best = i; }
                }
                if (best >= 0) { shop = s; index = best; price = s == ShopPotionYen ? store[best].Yen : store[best].Coin; return true; }
            }
            shop = index = price = -1;
            return false;
        }

        /// <summary>Walks to Kiriko and buys HP (and some MP) potions with yen, else xu, as a player does in the shop.</summary>
        private static Status Shop(NinjaAgent a, PlayerState me, GameWorld world, long now, out string why)
        {
            why = null;
            if (me.Record.TaskId < TaskDietSenTruCoc) { why = "shops open after the quest NV diệt sên trừ cóc"; return Status.Failed; }
            int want = PotionTarget(a);   // spending style: thrifty characters carry fewer
            if (HpPotions(me, world) >= want) return BuyFood(a, me, world, now);   // potions stocked: food next
            int shop, index, price;
            if (!PickPotion(me, world, PlayerItems.TypeHp, out shop, out index, out price))
            {
                if (HpPotions(me, world) >= AgentConfig.MinPotionsToFight || (HpPotions(me, world) > 0 && now - a.LastMenuAt < 5000))
                    return BuyFood(a, me, world, now);   // enough to fight, or just bought what the money allowed
                why = "not enough money for potions"; return Status.Failed;
            }
            var go = GoToNpc(a, me, world, NpcKiriko, "going to Kiriko to buy potions", out why);
            if (go != Status.Done) return go;
            if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
            a.LastMenuAt = now;
            long money = shop == ShopPotionYen ? me.Progress.Yen : me.Progress.Xu;
            int qty = (int)Math.Min(want - HpPotions(me, world), money / Math.Max(1, price));
            if (qty <= 0) { why = "not enough money for potions"; return Status.Failed; }
            a.Client.RequestShop(shop);
            int before = HpPotions(me, world);
            a.Client.Buy(shop, index, qty);
            int got = HpPotions(me, world) - before;
            if (got <= 0) { why = "the shop refused: " + a.Session.LastNotice; return Status.Failed; }
            // some MP potions too when the skill needs mana
            int mshop, mindex, mprice;
            if (me.SelectedSkill != null && me.SelectedSkill.ManaUse > 0 && MpPotions(me, world) < want / 2 && PickPotion(me, world, PlayerItems.TypeMp, out mshop, out mindex, out mprice))
            {
                long mm = mshop == ShopPotionYen ? me.Progress.Yen : me.Progress.Xu;
                int mq = (int)Math.Min(want / 2, mm / Math.Max(1, mprice));
                if (mq > 0) { a.Client.RequestShop(mshop); a.Client.Buy(mshop, mindex, mq); }
            }
            string item = world.FindItem(world.Store(shop)[index].ItemId).Name;
            AgentManager.Report(a, AgentEvent.Shop, "bought " + got + " " + item + " from Kiriko for " + (long)got * price + (shop == ShopPotionYen ? " yen" : " xu"));
            a.Knowledge.Learn("place:shop", "Kiriko sells potions (" + item + " " + price + (shop == ShopPotionYen ? " yen" : " xu") + ") in map " + me.MapId + " " + me.Zone.Tile.Name);
            return Status.Running;   // the next tick finds the potions stocked and goes on to the food (BuyFood)
        }

        // ================================================================== trying unknown items (item ledger)

        /// <summary>The village NPC whose shop sells the item (Kiriko: potions, Tabemono: food), or -1.</summary>
        public static int SoldBy(GameWorld world, int itemId)
        {
            foreach (int s in new[] { ShopPotionYen, ShopPotionXu, ShopFoodYen, ShopFoodXu })
            {
                var store = world.Store(s);
                if (store != null && store.Exists(e => e.ItemId == itemId)) return s == ShopFoodYen || s == ShopFoodXu ? NpcTabemono : NpcKiriko;
            }
            return -1;
        }

        /// <summary>User 2026-11-13: "a player who sees something new buys one to try it". The cheapest village-shop item
        /// this character may use and has never used (item ledger), affordable while keeping FoodMoneyReserve, not known
        /// to be useless, only with enough HP potions for a fight. price 0: one is already in the bag.</summary>
        public static bool PickTryItem(NinjaAgent a, PlayerState me, GameWorld world, long now, out int itemId, out long price)
        {
            itemId = -1; price = long.MaxValue;
            if (now < a.NoTryUntil || me.Record.TaskId < TaskDietSenTruCoc || HpPotions(me, world) < AgentConfig.MinPotionsToFight) return false;
            var h = a.Learning.ItemRewards;
            foreach (int s in new[] { ShopPotionYen, ShopPotionXu, ShopFoodYen, ShopFoodXu })
            {
                var store = world.Store(s);
                if (store == null) continue;
                bool yen = s == ShopPotionYen || s == ShopFoodYen;
                long money = (yen ? me.Progress.Yen : me.Progress.Xu) - MoneyReserve(a);
                foreach (var e in store)
                {
                    var t = world.FindItem(e.ItemId);
                    if (t == null || t.Level > me.Level || ((t.Gender == 0 || t.Gender == 1) && t.Gender != me.Record.Gender)) continue;
                    if (h.Knows(e.ItemId) || h.Useless(e.ItemId) || a.FailedItems.Contains(e.ItemId) || !CouldHelpNow(me, t, now)) continue;
                    long cost = Array.Exists(me.Inventory.Bag, it => it != null && it.Id == e.ItemId) ? 0 : yen ? e.Yen : e.Coin;
                    if (cost < 0 || (cost > 0 && cost > money) || cost >= price) continue;
                    itemId = e.ItemId; price = cost;
                }
            }
            return itemId >= 0;
        }

        // A player tries a potion when it can show what it does (Java Char.useItem refuses an HP potion at full HP,
        // "HP đã đầy."), food when no food effect runs; other items any time.
        private static bool CouldHelpNow(PlayerState me, NsoServer.World.ItemTemplateData t, long now)
        {
            if (t.Type == PlayerItems.TypeHp) return me.Hp < me.MaxHp;
            if (t.Type == PlayerItems.TypeMp) return me.Mp < me.MaxMp;
            if (t.Type == PlayerItems.TypeEat) return !Fed(me, now);
            return true;
        }

        // Shop step with an item id: buy one (if none in the bag) and use it once; ActionRewards writes what it did into
        // the ledger and the character's knowledge. Done after the try, also when the server refuses the use.
        private static Status TryItem(NinjaAgent a, PlayerState me, GameWorld world, long now, int itemId, out string why)
        {
            why = null;
            var t = world.FindItem(itemId);
            if (t == null || a.Learning.ItemRewards.Knows(itemId) || a.FailedItems.Contains(itemId)) return Status.Done;
            int idx = Array.FindIndex(me.Inventory.Bag, it => it != null && it.Id == itemId);
            if (idx >= 0)
            {
                if (!CouldHelpNow(me, t, now)) { a.NoTryUntil = now + 120000; return Status.Done; }   // keep it for when it can show its use
                if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
                a.LastMenuAt = now;
                a.Activity = "trying " + t.Name;
                var before = me.Inventory.Bag[idx]; int qty = before.Quantity;
                a.Client.UseItem(idx);
                if (me.Inventory.Bag[idx] == before && before.Quantity == qty && !a.Learning.ItemRewards.Knows(itemId))
                {   // refused now (full HP, level...): keep it and try again later, not forever banned
                    a.NoTryUntil = now + 120000; AgentManager.Remember(a, "could not use " + t.Name + " yet: " + a.Session.LastNotice);
                }
                return Status.Done;
            }
            int npc = SoldBy(world, itemId);
            if (npc < 0) { why = t.Name + " is not sold in the village shops"; return Status.Failed; }
            var st = BuyFrom(a, me, world, now, npc, itemId, 1, out why);
            if (st == Status.Done && !Array.Exists(me.Inventory.Bag, it => it != null && it.Id == itemId))
            { why = "the shop refused " + t.Name + ": " + a.Session.LastNotice; st = Status.Failed; }
            if (st == Status.Failed) a.NoTryUntil = now + 300000;
            return st == Status.Done ? Status.Running : st;   // bought: the next tick uses it
        }

        /// <summary>Buys a shop item by id from a village NPC's shop (food for quest 3). Done when bought.</summary>
        private static Status BuyFrom(NinjaAgent a, PlayerState me, GameWorld world, long now, int npc, int itemId, int qty, out string why)
        {
            why = null;
            int yenShop = npc == NpcTabemono ? ShopFoodYen : ShopPotionYen, xuShop = npc == NpcTabemono ? ShopFoodXu : ShopPotionXu;
            int shop = -1, index = -1; long cost = 0;
            foreach (int s in new[] { yenShop, xuShop })
            {
                var store = world.Store(s);
                if (store == null) continue;
                for (int i = 0; i < store.Count; i++)
                {
                    if (store[i].ItemId != itemId) continue;
                    long c = (long)(s == yenShop ? store[i].Yen : store[i].Coin) * qty;
                    long money = s == yenShop ? me.Progress.Yen : me.Progress.Xu;
                    if (c <= money) { shop = s; index = i; cost = c; }
                    break;
                }
                if (shop >= 0) break;
            }
            if (shop < 0) { why = "not enough money (need yen or xu) for " + world.FindItem(itemId).Name; return Status.Failed; }
            var go = GoToNpc(a, me, world, npc, "going to buy " + world.FindItem(itemId).Name, out why);
            if (go != Status.Done) return go;
            if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
            a.LastMenuAt = now;
            a.Client.RequestShop(shop);
            a.Client.Buy(shop, index, qty);
            AgentManager.Remember(a, "bought " + qty + " " + world.FindItem(itemId).Name + " for " + cost + (shop == yenShop ? " yen" : " xu"));
            return Status.Done;
        }

        // ================================================================== movement

        /// <summary>Walks towards a map through the waypoint graph over unlocked maps, then asks to change map at the
        /// exit (Service.requestChangeMap); the server checks MapUnlock and refuses locked maps.</summary>
        private static Status Travel(NinjaAgent a, PlayerState me, GameWorld world, int target, out string why)
        {
            why = null;
            int cur = me.MapId;
            int task = me.Record.TaskId;
            if (target < 0 || cur == target) { a.Route = null; return Status.Done; }
            if (a.Route == null || a.Route.Count == 0 || a.Route[a.Route.Count - 1] != target || WorldKnowledge.ExitTo(me.Zone.Tile, a.Route[0]) == null)
                a.Route = WorldKnowledge.Route(world, cur, target, task);
            if (a.Route == null)
            {
                why = MapUnlock.CanEnter(task, target) ? "no way from map " + cur + " to map " + target : "map " + target + " is locked until a later main quest";
                if (!MapUnlock.CanEnter(task, target)) a.Knowledge.Learn("locked:" + target, "map " + target + " " + MapName(world, target) + " is locked: needs a later main quest (now quest " + task + ")");
                return Status.Failed;
            }
            if (a.Route.Count == 0) return Status.Done;
            // fast travel (user 2026-11-13): carriage or teleport scroll when it saves enough walking
            if (a.Fast != null && a.Fast.Target != target) a.Fast = null;
            if (a.Fast == null) a.Fast = PlanFastTravel(a, me, world, target, a.Route.Count, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (a.Fast != null) return RunFastTravel(a, me, world, out why);
            int next = a.Route[0];
            var wp = WorldKnowledge.ExitTo(me.Zone.Tile, next);
            if (wp == null) { a.Route = null; why = "lost the way to map " + target; return Status.Failed; }
            int wx = Math.Max(wp.MinX, Math.Min(wp.MaxX, (wp.MinX + wp.MaxX) / 2)), wy = wp.MaxY;
            if (WalkTo(a, me, world, wx, wy, true))
            {
                // Play mode 2026-11-13: LastNotice is the last dialog ever shown (e.g. "Tên nhân vật đã tồn tại!" from
                // the login), so a refusal without its own notice must not report or learn it.
                string noticeBefore = a.Session.LastNotice ?? "";
                a.Session.LastNotice = "";
                a.Client.ChangeMap();
                if (me.MapId == next)
                {
                    a.Route.RemoveAt(0);
                    a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    a.Knowledge.Remove("locked:" + next);
                    if (a.Route.Count == 0) { a.Route = null; return Status.Done; }
                }
                else if (!me.IsChangeMap)
                {
                    a.Route = null;
                    string reason = string.IsNullOrEmpty(a.Session.LastNotice) ? "no reason given (at " + me.X + "," + me.Y + ", exit " + wp.MinX + "-" + wp.MaxX + "," + wp.MinY + "-" + wp.MaxY + ")" : a.Session.LastNotice;
                    why = "the server refused map " + next + ": " + reason;
                    // only a real refusal notice is knowledge; a silent failure is a positioning problem
                    if (!string.IsNullOrEmpty(a.Session.LastNotice)) a.Knowledge.Learn("locked:" + next, "map " + next + " " + MapName(world, next) + ": " + reason);
                    return Status.Failed;
                }
                if (string.IsNullOrEmpty(a.Session.LastNotice)) a.Session.LastNotice = noticeBefore;
            }
            return Status.Running;
        }

        private static string MapName(GameWorld world, int id) { var t = world.FindMap(id); return t != null ? t.Name : "?"; }

        // ================================================================== fast travel

        // User 2026-11-13: players want to move fast by any means (NPCs, items), but judge whether it is needed.
        // Compares walking (map changes on the route) with each way to jump: a teleport scroll from anywhere to a
        // village or school (Char.useItemChangeMap), or the carriage NPC in a village/school (Char.npcUmayaki /
        // npcUmayaki_2), each followed by the walk from there. Used only when it saves FastTravelMinSavedHops.
        public static FastTrip PlanFastTravel(NinjaAgent a, PlayerState me, GameWorld world, int target, int walkHops, long now)
        {
            if (AgentConfig.FastTravelMinSavedHops <= 0 || now < a.NoFastTravelUntil || walkHops <= AgentConfig.FastTravelMinSavedHops) return null;
            int task = me.Record.TaskId, cur = me.MapId;
            FastTrip best = null; int bestCost = walkHops - AgentConfig.FastTravelMinSavedHops + 1;
            var bag = me.Inventory.Bag;
            int scroll = Array.FindIndex(bag, it => it != null && it.Id == PlayerItemUse.ItemVoHanKhaDi && (it.Expire <= 0 || it.Expire > now));
            if (scroll < 0) scroll = Array.FindIndex(bag, it => it != null && it.Id == PlayerItemUse.ItemKhaDi);
            if (scroll >= 0)
            {
                int extra = bag[scroll].Id == PlayerItemUse.ItemKhaDi ? AgentConfig.FastTravelScrollCostHops : 0;
                for (int m = 0; m < PlayerItemUse.KhaDiMaps.Length; m++)
                {
                    int dest = PlayerItemUse.KhaDiMaps[m];
                    if (dest == cur || !MapUnlock.CanEnter(task, dest)) continue;
                    var r = WorldKnowledge.Route(world, dest, target, task);
                    if (r == null) continue;
                    int cost = 1 + r.Count + extra;
                    if (cost < bestCost) { bestCost = cost; best = new FastTrip { Target = target, Dest = dest, BagIndex = scroll, MenuIndex = m, Label = world.FindItem(bag[scroll].Id).Name }; }
                }
            }
            int npc = PlayerTasks.IsVillageMap(cur) ? PlayerTasks.NpcUmayakiId : PlayerTasks.IsSchoolMap(cur) ? PlayerTasks.NpcUmayaki2Id : -1;
            if (npc >= 0 && me.Zone.Npcs.Exists(n => n.TemplateId == npc))
                foreach (var d in npc == PlayerTasks.NpcUmayakiId ? PlayerTasks.UmayakiVillages : PlayerTasks.UmayakiSchools)
                {
                    if (d.Key == cur || !MapUnlock.CanEnter(task, d.Key)) continue;
                    var r = WorldKnowledge.Route(world, d.Key, target, task);
                    if (r == null) continue;
                    int cost = 1 + r.Count;
                    if (cost < bestCost) { bestCost = cost; best = new FastTrip { Target = target, Dest = d.Key, Npc = npc, Label = d.Value }; }
                }
            if (best != null) best.Saved = walkHops - bestCost;
            return best;
        }

        private static Status RunFastTravel(NinjaAgent a, PlayerState me, GameWorld world, out string why)
        {
            why = null;
            var f = a.Fast; long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int from = me.MapId;
            string how;
            if (f.Npc >= 0)
            {
                how = "the carriage (" + NpcName(world, f.Npc) + ")";
                var go = GoToNpc(a, me, world, f.Npc, "going to " + NpcName(world, f.Npc) + " to ride to " + f.Label, out why);
                if (go == Status.Failed || me.MapId != from) { a.Fast = null; a.NoFastTravelUntil = now + 300000; why = null; return Status.Running; }
                if (go != Status.Done) return Status.Running;
                if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
                a.LastMenuAt = now;
                a.Client.OpenMenu(f.Npc);
                int idx = me.Menus.FindIndex(e => e.Name == f.Label);
                if (idx >= 0) a.Client.Menu(f.Npc, idx);
            }
            else
            {
                how = f.Label;
                var it = f.BagIndex >= 0 && f.BagIndex < me.Inventory.Bag.Length ? me.Inventory.Bag[f.BagIndex] : null;
                if (it == null || (it.Id != PlayerItemUse.ItemKhaDi && it.Id != PlayerItemUse.ItemVoHanKhaDi)) { a.Fast = null; return Status.Running; }
                a.Activity = "using " + f.Label + " to go to " + MapName(world, f.Dest);
                a.Client.UseItemChangeMap(f.BagIndex, f.MenuIndex);
            }
            a.Fast = null; a.Route = null;
            if (me.MapId == f.Dest)
            {
                a.LastProgressAt = now;
                AgentManager.Remember(a, "took " + how + " from map " + from + " to " + MapName(world, f.Dest) + ", saving about " + f.Saved + " map changes");
                a.Knowledge.Learn("note:fasttravel:" + (f.Npc >= 0 ? "npc" : "item"), (f.Npc >= 0 ? NpcName(world, f.Npc) + " (in villages and schools) carries you to other villages or schools"
                    : f.Label + " teleports you from anywhere to a village or school") + "; worth it for long trips");
            }
            else
            {
                a.NoFastTravelUntil = now + 300000;
                AgentManager.Remember(a, "could not take " + how + " to " + MapName(world, f.Dest) + ": " + a.Session.LastNotice);
            }
            return Status.Running;
        }

        /// <summary>One step towards (tx, ty), at most MoveStepPx (the server refuses more than Speed * 17 per move):
        /// x first, the floor below (TileGrid.CollisionY), near the target its floor. Returns true when arrived.</summary>
        private static bool WalkTo(NinjaAgent a, PlayerState me, GameWorld world, int tx, int ty, bool waypoint = false)
        {
            var grid = world.GetGrid(me.Zone.Tile);
            // stay 30 px off the map edge: outside a waypoint it is refused (IsSoftBlock) or kills (IsCrossMap), and
            // Zone.requestChangeMap finds the waypoint within 100 px anyway
            const int margin = 30;
            tx = Math.Max(margin, Math.Min(grid.PxW - 1 - margin, tx));
            int dx = tx - me.X;
            if (Math.Abs(dx) <= 8 && (ty < 0 || Math.Abs(ty - me.Y) <= 30)) return true;
            // The destination itself (its floor, above a wood/rock tile); BotMovement plans the way over the platforms
            // (walk, jump, double jump, drop) instead of only walking straight at it.
            short y = -1;
            if (ty >= 0)
            {
                y = grid.CollisionY((short)tx, (short)Math.Max(0, Math.Min(grid.PxH - 1, ty - 24)));
                y = y >= grid.PxH - 1 ? (short)ty : OverObstacle(grid, tx, y);
            }
            a.Client.Move(tx, y);
            // A waypoint is reached within the server's box (TileMap.FindWaypoint: 100 px around it). Play mode 2026-11-13:
            // it used to ignore the height, so on map 6 bots asked to change map at (40,1238) under the exit at y 96-120
            // and the server refused silently, again and again.
            return Math.Abs(tx - me.X) <= 8 && (ty < 0 || Math.Abs(ty - me.Y) <= (waypoint ? 90 : 30));
        }

        // Find a proposed destination above a wood/rock tile. This is NOT an executable height change:
        // BotMovement must reach it through client-sized collision/gravity/jump steps (one double jump, as the client).
        internal static short OverObstacle(TileGrid grid, int x, short y)
        {
            for (int k = 0; k <= 6; k++)
            {
                int yy = y - k * 24;
                if (yy < 24) break;
                if (!grid.IsWood(x, yy - 1) && !grid.IsRock(x, yy - 1)) return (short)yy;
            }
            return y;
        }

        private static void Wander(NinjaAgent a, PlayerState me, GameWorld world, int radius)
        {
            if (Rng.Next(3) == 0) return;   // pauses look more human
            var grid = world.GetGrid(me.Zone.Tile);
            if (a.WanderX < 0 || Math.Abs(a.WanderX - me.X) <= 8)
                a.WanderX = Math.Max(60, Math.Min(grid.PxW - 60, me.X + Rng.Next(-radius, radius + 1)));
            if (WalkTo(a, me, world, a.WanderX, -1) || a.Client.CannotReach(a.WanderX, -1)) a.WanderX = -1;
        }

        /// <summary>Travels to the closest unlocked map with the NPC and walks up to it. Done when next to it.</summary>
        private static Status GoToNpc(NinjaAgent a, PlayerState me, GameWorld world, int npc, string activity, out string why)
        {
            why = null;
            int map, x, y;
            if (!WorldKnowledge.FindNpc(world, npc, me.MapId, me.Record.TaskId, out map, out x, out y))
            { why = "cannot reach NPC " + npc + " from unlocked maps"; return Status.Failed; }
            a.Activity = activity;
            if (me.MapId != map)
            {
                var t = Travel(a, me, world, map, out why);
                return t == Status.Failed ? Status.Failed : Status.Running;
            }
            a.Knowledge.Learn("place:npc" + npc, NpcName(world, npc) + " stands in map " + map + " " + me.Zone.Tile.Name);
            return WalkTo(a, me, world, x, y) || (Math.Abs(me.X - x) < 40 && Math.Abs(me.Y - y) < 60) ? Status.Done : Status.Running;
        }

        // names of the NPCs the quests use (PlayerTasks constants; npc_template is not kept by GameWorld)
        private static readonly Dictionary<int, string> NpcNames = new Dictionary<int, string>
        {
            { 0, "Kanata" }, { 1, "Furoya" }, { 2, "Ameji" }, { 3, "Kiriko" }, { 4, "Tabemono" }, { 5, "Kamakura" }, { 6, "Kenshinto" },
            { 7, "Umayaki" }, { 8, "Umayaki" }, { 9, "Toyotomi" }, { 10, "Ookamesama" }, { 11, "Kazeto" }, { 12, "Tajima" },
            { 23, "Matsurugi" }, { 24, "Okanechan" }, { 26, "Goosho" },
        };

        internal static string NpcName(GameWorld world, int npc)
        {
            string n;
            return NpcNames.TryGetValue(npc, out n) ? n : "NPC " + npc;
        }

        // ================================================================== goals

        /// <summary>Goes to the map and fights monsters (a kind, or any within the experience window), picking up its
        /// own loot first. Every hit is a PLAYER_ATTACK_NPC message: range, cooldown, MP and damage are the server's.</summary>
        private static Status Hunt(NinjaAgent a, PlayerState me, GameWorld world, int mapId, int mobId, int count, long now, out string why)
        {
            why = null;
            if (me.Inventory.Equipment[1] == null)
            {
                why = "has no weapon (Tajima gives a wooden sword)";
                return GetSword(a, me, world, now);
            }
            if (mapId >= 0 && me.MapId != mapId)
            {
                a.Activity = "travelling to map " + mapId + " to hunt";
                var t = Travel(a, me, world, mapId, out why);
                return t == Status.Failed ? Status.Failed : Status.Running;
            }
            var mob = FindMob(me.Zone, a.TargetMob);
            if (mob != null && mob.IsDead && a.TargetMob >= 0)
            {
                // the monster we were fighting died: count it as ours if we hit it last (approximation of Mob.dead)
                a.Kills++;
                a.StepProgress++;
                a.LastProgressAt = now;
                a.TargetMob = -1;
                AgentManager.OnKill(a, world, mob);
                if (count > 0 && a.StepProgress >= count) return Status.Done;
                mob = null;
            }
            if (a.ZoneTarget >= 0 && TrySpreadZone(a, me, world, now)) return Status.Running;
            var loot = AgentForaging.Choose(a, me, world, AgentConfig.LootScanRadiusPx, now);
            if (loot != null) { PickUp(a, me, world, loot); return Status.Running; }
            // Finish an active exchange before walking away to the public zone-selection NPC.
            if ((mob == null || mob.IsDead || now - me.LastTimeUseSkill > 2000) && TrySpreadZone(a, me, world, now)) return Status.Running;
            if (mob == null || mob.IsDead || !Suitable(me, mob, mobId)) { mob = ChooseMob(a, me, mobId); a.TargetMob = mob != null ? mob.Id : -1; a.TargetMobTemplate = mob != null ? mob.Template.Id : -1; }
            if (mob == null && mobId >= 0)
            {
                // the kind we need is dead for now: wait next to it until it comes back (Zone.updateRecovery), as players do
                Mob dead = null;
                foreach (var m in me.Zone.Mobs) if (m.Template.Id == mobId && m.IsDead && (dead == null || Math.Abs(m.X - me.X) < Math.Abs(dead.X - me.X))) dead = m;
                if (dead != null)
                {
                    a.LastTargetSeen = now;
                    a.Activity = "waiting for " + dead.Template.Name + " to come back";
                    if (Math.Abs(dead.X - me.X) > 60) WalkTo(a, me, world, dead.X, dead.Y);
                    return Status.Running;
                }
            }
            if (mob == null)
            {
                if (a.LastTargetSeen == 0) a.LastTargetSeen = now;
                if (now - a.LastTargetSeen > AgentConfig.NoTargetFailSeconds * 1000L)
                {
                    why = "no suitable monster" + (mobId >= 0 ? " (id " + mobId + ")" : "") + " on map " + me.MapId;
                    return Status.Failed;
                }
                a.Activity = "looking for monsters";
                Wander(a, me, world, 300);
                return Status.Running;
            }
            a.LastTargetSeen = now;
            var skill = me.SelectedSkill;
            if (!InRange(me.X, me.Y, skill, mob))
            {
                a.Activity = "approaching " + mob.Template.Name + " lv " + mob.Level;
                int ax, ay;
                ApproachPoint(me, world.GetGrid(me.Zone.Tile), skill, mob, out ax, out ay);
                if (Math.Abs(ax - me.X) <= 8) a.Client.Move(ax, ay);   // same column, other height: one step up or down
                else WalkTo(a, me, world, ax, ay);
                if (a.Client.CannotReach(ax, ay))
                {
                    // no way up/over to it (BotNavigation): choose another monster for a while instead of standing there
                    if (a.AvoidMobs.Count >= 64) a.AvoidMobs.Clear();
                    a.AvoidMobs[mob.Id] = now + AgentConfig.UnreachableRetrySeconds * 1000L;
                    a.TargetMob = -1;
                }
                return Status.Running;
            }
            a.Activity = "fighting " + mob.Template.Name + " lv " + mob.Level + (count > 0 ? " (" + a.StepProgress + "/" + count + ")" : "");
            if (skill == null || now - me.LastTimeUseSkill < skill.CoolDown) return Status.Running;
            if (me.Mp < skill.ManaUse)
            {
                if (!DrinkMp(a, me, world, now)) { why = "out of MP and MP potions"; a.Recovering = CanAffordPotions(me, world); if (!a.Recovering) return Status.Failed; }
                return Status.Running;
            }
            a.Client.Attack(mob.Id);
            return Status.Running;
        }

        private static Mob FindMob(Zone zone, int id)
        {
            if (id < 0) return null;
            foreach (var m in zone.Mobs) if (m.Id == id) return m;
            return null;
        }

        // monsters worth hunting: the plan's kind, otherwise within the experience window (Char.addExp: 10 levels)
        private static bool Suitable(PlayerState me, Mob m, int mobId)
        {
            if (m.IsDead || m.Hp <= 0 || m.IsBoss) return false;
            if (mobId >= 0) return m.Template.Id == mobId;
            if (m.Template.Id == 0) return false;   // the training dummy only for its quest
            return m.Level >= me.Level - 10 && m.Level <= me.Level + 5;
        }

        private static Mob ChooseMob(NinjaAgent a, PlayerState me, int mobId)
        {
            Mob best = null;
            int bestD = int.MaxValue;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var m in me.Zone.Mobs)
            {
                if (!Suitable(me, m, mobId)) continue;
                long until;
                if (!m.Chars.Contains(me.Record.Id) && a.AvoidMobs.TryGetValue(m.Id, out until) && until > now) continue;
                int d = Math.Abs(m.X - me.X) + 2 * Math.Abs(m.Y - me.Y);
                if (!m.Chars.Contains(me.Record.Id) && (m.Chars.Count > 0 || AgentManager.MobClaimed(a, m.Id))) d += 500;
                if (m.Chars.Contains(me.Record.Id)) d -= 200;   // finish what attacks us first
                if (d < bestD) { bestD = d; best = m; }
            }
            return best;
        }

        // a standable spot (no wood / rock) from which the selected skill reaches the monster: either side, near first
        private static void ApproachPoint(PlayerState me, TileGrid grid, SkillData skill, Mob mob, out int x, out int y)
        {
            int dx = skill != null ? Math.Max(12, skill.Dx) : 40;
            int side = me.X < mob.X ? -1 : 1;
            foreach (int d in new[] { dx / 2, dx - 4, 6, dx / 4 })
                foreach (int s in new[] { side, -side })
                {
                    int cx = Math.Max(30, Math.Min(grid.PxW - 31, mob.X + s * d));
                    short cy = grid.CollisionY((short)cx, (short)Math.Max(0, mob.Y - 24));
                    if (cy >= grid.PxH - 1) continue;
                    cy = OverObstacle(grid, cx, cy);
                    if (InRange(cx, cy, skill, mob)) { x = cx; y = cy; return; }
                }
            x = mob.X + side * dx / 2;
            y = mob.Y;
        }

        // the range check of Char.attackMonster(Message), with the selected skill, from (px, py)
        internal static bool InRange(int px, int py, SkillData skill, Mob mob)
        {
            int sdx = skill != null ? skill.Dx : 40, sdy = skill != null ? skill.Dy : 40;
            int left = px - sdx, right = px + sdx, top = py - sdy - 75, bottom = py + sdy;
            int add = mob.Template.Type == 4 ? 50 : 0;
            int range = mob.Template.RangeMove;
            return mob.X >= left - range && mob.X <= right + range && mob.Y >= top - range - add && mob.Y <= bottom + range;
        }

        private static bool TryLootHabit(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            var step = a.Step;
            if (a.ZoneTarget >= 0 || a.Admitting || me.Menus.Count > 0 || me.CatchUntil > now || me.FishUntil > now ||
                (step != null && (step.Goal == GoalType.Hunt || step.Goal == GoalType.Collect))) return false;
            // Quest hunts count their dead target before foraging; keep that executor's bookkeeping in one place.
            if (step != null && step.Goal == GoalType.Quest)
            {
                var q = world.FindTask(me.Record.TaskId); int index = me.Progress.TaskIndex;
                if (q != null && me.Progress.TaskActive && index >= 0 &&
                    ((index < q.KillMobs.Length && q.KillMobs[index][0] >= 0) || (index < q.PickItems.Length && q.PickItems[index] >= 0))) return false;
            }
            var item = AgentForaging.Choose(a, me, world, AgentConfig.LootScanRadiusPx, now);
            if (item == null) return false;
            PickUp(a, me, world, item); return true;
        }

        private static bool TrySpreadZone(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            var npc = AgentZoneSelection.Prepare(a, me, world, now);
            if (npc == null) return false;
            a.TargetMob = -1; AgentForaging.Release(a);
            a.Activity = "walking to zone NPC for quieter zone " + a.ZoneTarget;
            if (Math.Abs(me.X - npc.X) >= 50 || Math.Abs(me.Y - npc.Y) >= 50)
            { WalkTo(a, me, world, npc.X, npc.Y); return true; }
            int target = a.ZoneTarget; int before = me.Zone.Id;
            a.Client.OpenZones(); // PlayerActions.OpenUiZone checks the same NPC proximity as the human UI.
            a.Client.ChangeZone(target);
            AgentZoneSelection.Cancel(a); a.LastZoneCheck = now;
            if (me.Zone.Id != before)
            {
                a.LastZoneChange = now; a.LastProgressAt = now; a.Route = null; a.LastTargetSeen = 0;
                AgentManager.Remember(a, "changed from zone " + before + " to quieter zone " + me.Zone.Id + " on map " + me.MapId);
            }
            return true;
        }

        // walk to the item, then Service.pickItem (the server checks distance, owner and bag space)
        private static bool PickUp(NinjaAgent a, PlayerState me, GameWorld world, ItemMapEntry it)
        {
            a.TargetItem = it.Id;
            a.Activity = "picking up an item";
            // Char.pickItem accepts anything inside 150 px; do not get stuck walking to its exact floor height.
            if (!PlayerItems.IsWithinPickupRange(me, it))
            {
                WalkTo(a, me, world, it.X, it.Y);
                return false;
            }
            var t = world.FindItem(it.Item.Id);
            int qty = it.Item.Quantity;
            a.Client.Pick(it.Id);
            if (!it.PickedUp) return false;
            AgentForaging.Release(a);
            a.LastProgressAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (t != null) AgentManager.OnPicked(a, world, t, qty);
            return true;
        }

        private static Status Collect(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, long now)
        {
            var it = AgentForaging.Choose(a, me, world, 2000, now);
            if (it == null)
            {
                if (a.StepProgress > 0 || now - a.StepStartedAt > 6000)
                {
                    AgentManager.Report(a, AgentEvent.Loot, a.StepProgress > 0 ? "picked up " + a.StepProgress + " items (yen now " + me.Progress.Yen + ")" : "found nothing to pick up");
                    return Status.Done;
                }
                a.Activity = "looking for items";
                Wander(a, me, world, 200);
                return Status.Running;
            }
            if (PickUp(a, me, world, it))
            {
                a.LastProgressAt = now;
                if (++a.StepProgress >= step.Count)
                {
                    AgentManager.Report(a, AgentEvent.Loot, "picked up " + a.StepProgress + " items (yen now " + me.Progress.Yen + ")");
                    return Status.Done;
                }
            }
            return Status.Running;
        }

        private static Status Explore(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, long now, out string why)
        {
            why = null;
            if (step.MapId < 0)
            {
                var n = WorldKnowledge.Neighbors(world, me.MapId, me.Record.TaskId);
                step.MapId = n.Count > 0 ? n[Rng.Next(n.Count)] : me.MapId;
            }
            if (me.MapId != step.MapId)
            {
                a.Activity = "exploring: on the way to map " + step.MapId;
                var t = Travel(a, me, world, step.MapId, out why);
                return t == Status.Failed ? Status.Failed : Status.Running;
            }
            a.Activity = "exploring " + me.Zone.Tile.Name;
            Wander(a, me, world, 400);
            if (now - Math.Max(a.StepStartedAt, a.LastProgressAt) < 20000) return Status.Running;
            var mobs = WorldKnowledge.MobsOf(world, me.MapId);
            string seen = "";
            for (int i = 0; i < mobs.Count && i < 3; i++) seen += (i > 0 ? ", " : "") + mobs[i].Name + " lv " + mobs[i].Level;
            a.Knowledge.Learn("map:" + me.MapId, "map " + me.MapId + " " + me.Zone.Tile.Name + (seen.Length > 0 ? ": " + seen : ": no monsters"));
            AgentManager.Report(a, AgentEvent.Note, "explored map " + me.MapId + " " + me.Zone.Tile.Name + (seen.Length > 0 ? " (monsters: " + seen + ")" : " (no monsters)"));
            return Status.Done;
        }

        private static Status Idle(NinjaAgent a, PlayerState me, GameWorld world, PlanStep step, long now)
        {
            a.Activity = "hanging around";
            Wander(a, me, world, 100);
            return now - a.StepStartedAt >= step.Seconds * 1000L ? Status.Done : Status.Running;
        }

        /// <summary>Without a weapon a character cannot attack (PlayerCombat.AttackMonster): take one from the bag, or
        /// ask Tajima for the wooden sword ("Nhận kiếm gỗ").</summary>
        private static Status GetSword(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            int i = PlayerItems.IndexInBag(me, ItemWoodenSword);
            if (i >= 0 && !a.Admitting) { a.Client.UseItem(i); return Status.Running; }
            string why;
            var go = GoToNpc(a, me, world, NpcTajima, "going to Tajima for a wooden sword", out why);
            if (go != Status.Done) return go == Status.Failed ? Status.Failed : Status.Running;
            if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
            a.LastMenuAt = now;
            a.Client.OpenMenu(NpcTajima);
            int idx = me.Menus.FindIndex(e => e.Name == "Nhận kiếm gỗ");
            if (idx < 0) return Status.Failed;
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
            if (t == null) return "";
            int npc = PlayerTasks.TaskNpcId(me, world);
            if (me.Record.ClassId == 0 && task >= TaskGiaTangSucManh && !me.Progress.TaskActive)
                return me.Level >= 10 ? "join a school (the quest goal walks to the school NPC)" : "train to level 10, then join a school";
            if (!me.Progress.TaskActive) return "talk to " + NpcAt(world, me, npc) + " to take it";
            int idx = me.Progress.TaskIndex, last = t.SubNames.Length - 1;
            if (idx >= last) return "report to " + NpcAt(world, me, npc);
            if (idx == 0 && t.LevelRequire > 0 && me.Level < t.LevelRequire) return "train to level " + t.LevelRequire;
            if (idx < t.KillMobs.Length && t.KillMobs[idx][0] >= 0) return "kill " + MobAt(world, me, t.KillMobs[idx][0], idx);
            if (idx < t.PickItems.Length && t.PickItems[idx] >= 0)
            {
                var item = world.FindItem(t.PickItems[idx]);
                foreach (var d in PlayerTasks.TaskDropList())
                    if (d[1] == task && d[2] == idx) return "kill " + MobAt(world, me, d[0], idx) + " and pick up the " + (item != null ? item.Name : "item") + " it drops";
            }
            if (task == TaskDietSenTruCoc && idx == 0) return "buy Cơm nắm at " + NpcAt(world, me, NpcTabemono);
            if (task == TaskDietSenTruCoc && idx == 1) return "eat the Cơm nắm (done automatically)";
            if (task == TaskKhamPha && idx >= 1 && idx <= 3) { int place = idx == 1 ? 2 : idx == 2 ? 71 : 26; return "walk into map " + place + " " + MapName(world, place); }
            if (npc >= 0) return "talk to " + NpcAt(world, me, npc);
            return "";
        }

        private static string NpcAt(GameWorld world, PlayerState me, int npc)
        {
            int map, x, y;
            if (npc < 0) return "the quest NPC";
            return NpcName(world, npc) + (WorldKnowledge.FindNpc(world, npc, me.MapId, me.Record.TaskId, out map, out x, out y)
                ? " (map " + map + " " + MapName(world, map) + " at " + x + "," + y + ")" : " (not reachable yet)");
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
                if (a.LastTaskId < 0) a.LastTaskId = task;
                if (task != a.LastTaskId) { a.LastTaskId = task; return Status.Done; }   // quest finished: the plan moves on
                var t = world.FindTask(task);
                if (t == null) { why = "no more main quests"; return Status.Failed; }
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
                        if (d[1] == task && d[2] == idx) return HuntFor(a, me, world, now, d[0], idx, "quest " + t.Name + ": " + stepName, out why);
                }
                // hand-made steps every player does by hand
                if (task == TaskLanDauDungKiem && idx == 0) return GetSword(a, me, world, now) == Status.Failed ? Fail(out why, "cannot get a weapon") : Status.Running;
                if (task == TaskDietSenTruCoc && idx == 0)
                {
                    int need = Math.Max(1, (idx < t.Counts.Length ? t.Counts[idx] : 2) - me.Progress.TaskCount);
                    var b = BuyFrom(a, me, world, now, NpcTabemono, ItemComNam, need, out why);
                    if (b == Status.Failed) return EarnMoney(a, me, world, now, why, out why);
                    return Status.Running;
                }
                if (task == TaskDietSenTruCoc && idx == 1)
                {
                    int food = PlayerItems.IndexInBag(me, ItemComNam);
                    if (food >= 0 && now - a.LastMenuAt > ActionGapMs) { a.LastMenuAt = now; a.Client.UseItem(food); }
                    return Status.Running;
                }
                if (task == TaskKhamPha && idx >= 1 && idx <= 3)
                {
                    int place = idx == 1 ? 2 : idx == 2 ? 71 : 26;   // PlayerTasks.OnChangeMap
                    a.Activity = "quest " + t.Name + ": " + stepName;
                    var tr = Travel(a, me, world, place, out why);
                    return tr == Status.Failed ? Status.Failed : Status.Running;   // arriving advances the step
                }
                if (task == TaskGiaTangSucManh)
                {
                    a.Activity = "quest " + t.Name + ": " + stepName;   // the habits read the book, then spend the points
                    if (now - a.StepStartedAt > 30000 && me.SkillPoint == 0 && me.PotentialPoint == 0) return SkipOrFail(a, me, world, now, t, stepName, out why);
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
                var bag = me.Inventory.Bag; var eq = me.Inventory.Equipment;
                // 1) an upgraded piece in the bag: put it on (this is what completes the step)
                for (int i = 0; i < bag.Length; i++)
                {
                    var it = bag[i]; var tt = it != null ? world.FindItem(it.Id) : null;
                    if (tt == null || !kind(tt) || it.Upgrade <= 0 || a.FailedItems.Contains(it.Id)) continue;
                    if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
                    a.LastMenuAt = now;
                    a.Client.UseItem(i);
                    if (bag[i] == it) a.FailedItems.Add(it.Id);   // cannot wear it (class, gender, level)
                    else AgentManager.Remember(a, "put on the upgraded " + tt.Name + " +" + it.Upgrade);
                    return Status.Running;
                }
                // 2) the piece to upgrade: a worn one first (known to fit), else one from the bag it can wear
                int slot = -1, bagIndex = -1;
                for (int s = 0; s < eq.Length; s++)
                {
                    var tt = eq[s] != null ? world.FindItem(eq[s].Id) : null;
                    if (tt != null && kind(tt) && tt.Fashion <= -1 && eq[s].Upgrade < PlayerUpgrade.UpMax(tt)) { slot = s; break; }
                }
                if (slot < 0)
                    for (int i = 0; i < bag.Length; i++)
                    {
                        var it = bag[i]; var tt = it != null ? world.FindItem(it.Id) : null;
                        if (tt == null || !kind(tt) || tt.Fashion > -1 || it.Upgrade >= PlayerUpgrade.UpMax(tt) || a.FailedItems.Contains(it.Id)) continue;
                        if (tt.Level > me.Level || ((tt.Gender == 0 || tt.Gender == 1) && tt.Gender != me.Record.Gender)) continue;
                        bagIndex = i; break;
                    }
                string what = idx == 1 ? "weapon" : idx == 2 ? "adornment (necklace, ring, jade or charm)" : "clothing (hat, coat, gloves, trousers or shoes)";
                if (slot < 0 && bagIndex < 0)
                {   // nothing to upgrade yet: train, monsters drop gear (the equipment shops take xu)
                    a.Activity = "no " + what + " of level 10+ to upgrade - training for drops";
                    a.Knowledge.Learn("note:upgrade" + idx, "the upgrade quest needs a " + what + " of level 10 or more; monsters drop them, shops sell them for xu");
                    return Hunt(a, me, world, -1, -1, 0, now, out why) == Status.Failed ? Status.Failed : Status.Running;
                }
                // 3) crystals worth enough for the success cap (UpCrystal values, at most 18)
                var piece = slot >= 0 ? eq[slot] : bag[bagIndex]; var pt = world.FindItem(piece.Id);
                int up = piece.Upgrade;
                int[] need = PlayerUpgrade.IsWeapon(pt) ? PlayerUpgrade.UpWeapon : PlayerUpgrade.IsAdorn(pt) ? PlayerUpgrade.UpAdorn : PlayerUpgrade.UpClothe;
                long coin = PlayerUpgrade.IsWeapon(pt) ? PlayerUpgrade.CoinUpWeapon[up] : PlayerUpgrade.IsAdorn(pt) ? PlayerUpgrade.CoinUpAdorn[up] : PlayerUpgrade.CoinUpClothe[up];
                long target = (long)Math.Ceiling(need[up] * PlayerUpgrade.MaxPercent[up] / 100.0);
                var stones = new List<int>();
                for (int i = 0; i < bag.Length; i++)
                    if (bag[i] != null && bag[i].Id <= 11 && bag[i].Quantity == 1) stones.Add(i);
                if (stones.Count == 0)
                {
                    a.Activity = "no crystals to upgrade the " + what + " - training for drops";
                    a.Knowledge.Learn("note:upgrade-stones", "upgrading needs crystals (Đá cấp 1-12): monsters drop them; keep them, do not sell");
                    return Hunt(a, me, world, -1, -1, 0, now, out why) == Status.Failed ? Status.Failed : Status.Running;
                }
                if (coin > me.Progress.Yen + me.Progress.Xu) { why = "not enough yen/xu to upgrade (" + coin + ")"; return Status.Failed; }
                stones.Sort((x, y) => PlayerUpgrade.UpCrystal[bag[y].Id].CompareTo(PlayerUpgrade.UpCrystal[bag[x].Id]));   // biggest first
                var use = new List<int>(); long sum = 0;
                foreach (int i in stones)   // big ones that do not overshoot, then the smallest one that closes the gap
                    if (use.Count < 18 && sum + PlayerUpgrade.UpCrystal[bag[i].Id] <= target) { use.Add(i); sum += PlayerUpgrade.UpCrystal[bag[i].Id]; }
                if (sum < target)
                    for (int k = stones.Count - 1; k >= 0; k--)
                        if (!use.Contains(stones[k]) && use.Count < 18) { use.Add(stones[k]); sum += PlayerUpgrade.UpCrystal[bag[stones[k]].Id]; if (sum >= target) break; }
                // 4) to the forge
                var go = GoToNpc(a, me, world, NpcKenshinto, "going to Kenshinto to upgrade the " + pt.Name, out why);
                if (go != Status.Done) return go;
                if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
                a.LastMenuAt = now;
                if (slot >= 0)
                {   // the forge takes it from the bag; same tick, so the wear habit cannot put it back on first
                    a.Client.BodyToBag(slot);
                    bagIndex = Array.IndexOf(bag, piece);
                    if (bagIndex < 0) { why = "could not take off " + pt.Name + " (bag full?)"; return Status.Failed; }
                }
                int before = piece.Upgrade;
                a.Client.Upgrade(bagIndex, use);
                a.LastProgressAt = now;
                AgentManager.Remember(a, (piece.Upgrade > before ? "upgraded " : "failed to upgrade ") + pt.Name + " to +" + piece.Upgrade + " with " + use.Count + " crystals worth " + sum + "/" + need[up] +
                    (piece.Upgrade > before ? "" : " (" + a.Session.LastNotice + ")"));
                if (piece.Upgrade > before)
                    a.Knowledge.Learn("note:upgrade", "Kenshinto upgrades gear of level 10+ with crystals (" + pt.Name + " +" + piece.Upgrade + "); wear it afterwards for the quest");
                return Status.Running;
            }

            private static Status Fail(out string why, string text) { why = text; return Status.Failed; }

            // a kill / pick step: hunt that monster where it lives (the step's map first)
            private static Status HuntFor(NinjaAgent a, PlayerState me, GameWorld world, long now, int mob, int idx, string label, out string why)
            {
                why = null;
                var maps = world.TaskMaps(me.Record.TaskId);
                int pref = maps != null && idx + 1 < maps.Length ? maps[idx + 1] : -1;
                int map = WorldKnowledge.FindMobMap(world, mob, me.MapId, me.Record.TaskId, pref);
                if (map < 0) { why = "monster " + mob + " lives in no unlocked map"; return Status.Failed; }
                int before = me.Progress.TaskCount;
                var h = Hunt(a, me, world, map, mob, 0, now, out why);
                if (me.Progress.TaskCount != before) a.LastProgressAt = now;
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
                if (giver < 0) giver = PlayerTasks.TaskNpcId(me, world);
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
            /// talk / quiz, or the entry named <paramref name="prefer"/>). Running until the quest state changes.</summary>
            private static Status Talk(NinjaAgent a, PlayerState me, GameWorld world, int npc, long now, out string why, string prefer = null)
            {
                why = null;
                if (npc < 0) { why = "no NPC for this step"; return Status.Failed; }
                var go = GoToNpc(a, me, world, npc, "walking to " + NpcName(world, npc), out why);
                if (go != Status.Done) return go;
                if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
                a.LastMenuAt = now;
                // a quiz is open: me.Menus holds the answers (QuestionAndAnswer)
                if (me.Menus.Count > 0 && me.Menus[0].Kind >= MenuEntry.A && me.Menus[0].Kind <= MenuEntry.D) return Answer(a, me, npc);
                int stateBefore = me.Record.TaskId * 1000 + (me.Progress.TaskActive ? 500 : 0) + me.Progress.TaskIndex;
                a.Client.OpenMenu(npc);
                int pick = -1;
                for (int i = 0; i < me.Menus.Count && pick < 0; i++)
                {
                    var e = me.Menus[i];
                    if (prefer != null ? e.Name.StartsWith(prefer, StringComparison.Ordinal)
                        : e.Kind == MenuEntry.TakeTask || e.Kind == MenuEntry.FinishTask || e.Kind == MenuEntry.DoTask) pick = i;
                }
                // NPCs with their own menu advance a "talk to ..." step through their "Nói chuyện" entry (PlayerTasks.NpcTalk)
                if (pick < 0 && prefer == null) pick = me.Menus.FindIndex(e => e.Name == "Nói chuyện");
                if (pick < 0) { why = "nothing to do with " + NpcName(world, npc) + " now" + (me.Menus.Count > 0 ? "" : " (no menu)"); me.Menus.Clear(); return Status.Failed; }
                var entry = me.Menus[pick];
                a.Client.Menu(npc, pick);
                if (entry.Kind == MenuEntry.TakeTask && me.Menus.Count > 0 && me.Menus[0].Kind == MenuEntry.ConfirmTakeTask) a.Client.GetTask(npc, 0);   // "Nhận"
                if (me.Menus.Count > 0 && me.Menus[0].Kind >= MenuEntry.A && me.Menus[0].Kind <= MenuEntry.D) return Status.Running;   // a quiz started
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
                if (known != null) pick = me.Menus.FindIndex(e => e.Name == known.Text);
                if (pick < 0)
                {
                    if (a.QuizAnswered != me.NAnswer) { a.QuizAnswered = me.NAnswer; a.QuizChoice = 0; }
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
                if (go != Status.Done) return go;
                if (now - a.LastMenuAt < ActionGapMs) return Status.Running;
                a.LastMenuAt = now;
                if (me.Inventory.Equipment[1] != null) { a.Client.BodyToBag(1); return Status.Running; }
                a.Client.OpenMenu(npc);
                int join = me.Menus.FindIndex(e => e.Name == "Nhập học");
                if (join < 0) { why = "no admission menu"; return Status.Failed; }
                a.Client.Menu(npc, join);
                if (me.Menus.Count >= 2) a.Client.Menu(npc, (cls % 2 == 1) ? 0 : 1);   // the sub-menu: first or second class
                if (me.Record.ClassId != 0)
                {
                    a.Admitting = false;
                    a.FailedItems.Clear();   // new class: the school's weapon and book can be used now
                    a.LastProgressAt = now;
                    AgentManager.Report(a, AgentEvent.Quest, "joined the school of " + NpcName(world, npc) + " as class " + me.Record.ClassId);
                    a.Knowledge.Learn("note:school", "joined " + NpcName(world, npc) + "'s school (class " + me.Record.ClassId + ") at level " + me.Level);
                }
                else why = a.Session.LastNotice;
                return Status.Running;
            }
        }
    }
}
