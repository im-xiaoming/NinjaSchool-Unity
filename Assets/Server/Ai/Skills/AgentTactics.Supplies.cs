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
        // ================================================================== habits
        // what any player does between fights: points, gear, books, the right skill selected, MP potions
        private static void Habits(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (MpPercent(me) < AgentConfig.DrinkMpPercent && me.SelectedSkill != null && me.SelectedSkill.ManaUse > 0)
                DrinkMp(a, me, world, now);
            if (now - a.LastEquipCheck < 2500)
                return;
            a.LastEquipCheck = now;
            Eat(a, me, world, now);
            bool quest9 = me.Record.TaskId == TaskGiaTangSucManh && me.Progress.TaskActive;
            int idx9 = quest9 ? me.Progress.TaskIndex : -1;
            // skill points to the main attack skill (NV gia tăng sức mạnh wants a book first)
            if (me.SkillPoint > 0 && (!quest9 || idx9 == 1))
            {
                var best = BestAttackSkill(me, world, true);
                if (best >= 0)
                    a.Client.UpSkill(best, 1);
            }

            // potential: mostly strength (damage), some HP for careful characters (index 0 strength, 2 HP)
            if (me.PotentialPoint > 0 && (!quest9 || idx9 == 2))
            {
                int hp = (int)Math.Round(me.PotentialPoint * (0.2 + a.P.Caution * 0.4));
                if (hp > 0)
                    a.Client.UpPotential(2, hp);
                if (me.PotentialPoint > 0)
                    a.Client.UpPotential(0, me.PotentialPoint);
            }

            // gear and books from the bag (the server checks class, gender and level like for any player)
            if (!a.Admitting)
                WearAndRead(a, me, world);
            if (!a.Admitting)
                SellJunk(a, me, world);
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
                if (t == null || t.Type != PlayerSkills.TypeAttack)
                    continue;
                if (forUpgrade && (s[1] >= t.MaxPoint || (s[0] >= 67 && s[0] <= 72)))
                    continue;
                if (forUpgrade)
                {
                    var next = world.FindSkill(s[0], s[1] + 1);
                    if (next == null || next.Level > me.Level)
                        continue;
                }

                if (s[1] > bestPoint)
                {
                    bestPoint = s[1];
                    best = s[0];
                }
            }

            return best;
        }

        private static void WearAndRead(NinjaAgent a, PlayerState me, GameWorld world)
        {
            var bag = me.Inventory.Bag;
            for (int i = 0; i < bag.Length; i++)
            {
                var it = bag[i];
                if (it == null || a.FailedItems.Contains(it.Id))
                    continue;
                var t = world.FindItem(it.Id);
                if (t == null)
                    continue;
                bool wear = PlayerService.IsWearable(t) && t.Type < me.Inventory.Equipment.Length && me.Inventory.Equipment[t.Type] == null && t.Level <= me.Level && (t.Gender == 2 || t.Gender == me.Record.Gender);
                bool book = PlayerSkills.BookClass(world, it.Id) >= 1 && PlayerSkills.BookClass(world, it.Id) == me.Record.ClassId;
                if (!wear && !book)
                    continue;
                int id = it.Id;
                a.Client.UseItem(i);
                if (bag[i] != null && bag[i].Id == id && bag[i] == it)
                    a.FailedItems.Add(id); // refused: do not try again
                else
                    AgentManager.Remember(a, (wear ? "put on " : "read ") + t.Name);
                return; // one item per check, like a player clicking
            }
        }

        // Play mode 2026-11-13: bags filled with loose stones and spare gear, then the server refused finishing
        // "Hái thuốc" (Char.finishTask needs 2 free slots) and joining a school (Char.nhapHoc needs 2) - the bots
        // repeated the menu forever. Like a player clearing the bag, sell one junk item per check (Char.saleItem)
        // while fewer than AgentConfig.BagReserveSlots are free: stones first (the AI never upgrades), then gear
        // it will never wear. Potions, food, quest items and "other" items (bags, travel scrolls) are kept.
        private static void SellJunk(NinjaAgent a, PlayerState me, GameWorld world)
        {
            if (AgentConfig.BagReserveSlots <= 0 || PlayerItems.SlotNull(me) >= AgentConfig.BagReserveSlots)
                return;
            // until NV nâng cấp trang bị is done its crystals and upgradable gear (level 10+, this gender) are kept
            bool upgradeQuest = me.Record.TaskId <= TaskNangCapTrangBi;
            var bag = me.Inventory.Bag;
            var eq = me.Inventory.Equipment;
            int pick = -1, pickRank = int.MaxValue;
            for (int i = 0; i < bag.Length; i++)
            {
                var it = bag[i];
                if (it == null || a.FailedItems.Contains(-1 - it.Id))
                    continue;
                var t = world.FindItem(it.Id);
                if (t == null)
                    continue;
                int rank;
                if (upgradeQuest && (t.Type == 26 || (PlayerService.IsWearable(t) && PlayerUpgrade.UpMax(t) > 0 && (t.Gender == 2 || t.Gender == me.Record.Gender))))
                    continue;
                if (t.Type == 26)
                    rank = 0; // stones
                else if (PlayerService.IsWearable(t) && t.Type != 12 && it.Upgrade == 0)
                {
                    bool mine = t.Gender == 2 || t.Gender == me.Record.Gender;
                    bool slotFree = t.Type < eq.Length && eq[t.Type] == null;
                    if (mine && slotFree)
                        continue; // WearAndRead puts it on once the level allows
                    rank = 1;
                }
                else
                    continue;
                if (rank < pickRank)
                {
                    pick = i;
                    pickRank = rank;
                }
            }

            if (pick < 0)
                return;
            var item = bag[pick];
            var tpl = world.FindItem(item.Id);
            int qty = item.Quantity < 1 ? 1 : item.Quantity;
            a.Client.Sell(pick, qty);
            if (bag[pick] == item)
            {
                a.FailedItems.Add(-1 - item.Id);
                return;
            } // refused: never retry this item

            AgentManager.Remember(a, "sold " + (qty > 1 ? qty + " " : "") + tpl.Name + " to free bag space");
        }

        private static int CountType(PlayerState me, GameWorld world, int type)
        {
            int n = 0;
            foreach (var it in me.Inventory.Bag)
            {
                if (it == null)
                    continue;
                var t = world.FindItem(it.Id);
                if (t != null && t.Type == type)
                    n += t.IsUpToUp ? it.Quantity : 1;
            }

            return n;
        }

        public static int HpPotions(PlayerState me, GameWorld world)
        {
            return CountType(me, world, PlayerItems.TypeHp);
        }

        public static int MpPotions(PlayerState me, GameWorld world)
        {
            return CountType(me, world, PlayerItems.TypeMp);
        }

        private static int FindType(PlayerState me, GameWorld world, int type)
        {
            var bag = me.Inventory.Bag;
            int best = -1, bestLevel = -1;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] == null)
                    continue;
                var t = world.FindItem(bag[i].Id);
                if (t != null && t.Type == type && t.Level <= me.Level && t.Level > bestLevel)
                {
                    best = i;
                    bestLevel = t.Level;
                }
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
                if (bag[i] == null)
                    continue;
                var t = world.FindItem(bag[i].Id);
                if (t == null || t.Type != type || t.Level > me.Level)
                    continue;
                // HP: 6 half-second ticks of the effect; unknown ids rank by level
                int amount = type == PlayerItems.TypeHp ? PlayerItems.HpPotionParam(bag[i].Id) * 6 : PlayerItems.MpPotionAmount(bag[i].Id);
                if (amount <= 0)
                    amount = 1 + t.Level;
                if (amount > strongestAmount)
                {
                    strongestAmount = amount;
                    strongest = i;
                }

                if (amount >= missing && amount < fitAmount)
                {
                    fitAmount = amount;
                    fit = i;
                }
            }

            return urgent || fit < 0 ? strongest : fit;
        }

        private static bool UnderBossAttack(PlayerState me)
        {
            foreach (var m in me.Zone.Mobs)
                if (!m.IsDead && m.IsBoss && m.Chars.Contains(me.Record.Id))
                    return true;
            return false;
        }

        private static bool DrinkHp(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            foreach (var e in me.Effects)
                if (e.TemplateId == 21 && !e.IsExpired(now))
                    return true;
            if (now - a.LastPotionAt < 1000)
                return true;
            int flee = a.Plan != null && a.Plan.FleeHpPercent > 0 ? a.Plan.FleeHpPercent : AgentConfig.DefaultFleeHpPercent;
            int i = ChoosePotion(me, world, PlayerItems.TypeHp, HpPercent(me) < flee / 2 || UnderBossAttack(me));
            if (i < 0)
                return false;
            a.LastPotionAt = now;
            a.Client.UseItem(i);
            return true;
        }

        private static bool DrinkMp(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (now - a.LastPotionAt < 1000)
                return true;
            int i = ChoosePotion(me, world, PlayerItems.TypeMp, UnderBossAttack(me));
            if (i < 0)
                return false;
            a.LastPotionAt = now;
            a.Client.UseItem(i);
            return true;
        }

        // Food (PlayerItems.UseItem, TypeEat): HP and MP every half second for 30 minutes. Players keep one running all
        // the time; a new serving replaces the running one, so the AI only eats when none is active.
        public static bool Fed(PlayerState me, long now)
        {
            foreach (var e in me.Effects)
                if (e.Type == 0 && !e.IsExpired(now))
                    return true;
            return false;
        }

        public static int Foods(PlayerState me, GameWorld world)
        {
            return CountType(me, world, PlayerItems.TypeEat);
        }

        private static void Eat(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (Fed(me, now) || now - a.LastPotionAt < 1000)
                return;
            int i = FindType(me, world, PlayerItems.TypeEat);
            if (i < 0)
                return;
            a.LastPotionAt = now;
            a.Client.UseItem(i); // the client reports what eating did (BotClient.Observe)
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
        public static bool PickFood(PlayerState me, GameWorld world, out int shop, out int index, out int price)
        {
            return PickFood(me, world, AgentConfig.FoodMoneyReserve, out shop, out index, out price);
        }

        public static bool PickFood(PlayerState me, GameWorld world, long reserve, out int shop, out int index, out int price)
        {
            shop = index = price = -1;
            if (me.Record.TaskId < TaskDietSenTruCoc || AgentConfig.FoodStock <= 0)
                return false;
            foreach (int s in new[]
            {
                ShopFoodYen,
                ShopFoodXu
            }

            )
            {
                var store = world.Store(s);
                if (store == null)
                    continue;
                int best = -1, bestLevel = -1;
                long money = (s == ShopFoodYen ? me.Progress.Yen : me.Progress.Xu) - reserve;
                for (int i = 0; i < store.Count; i++)
                {
                    var t = world.FindItem(store[i].ItemId);
                    if (t == null || t.Type != PlayerItems.TypeEat || t.Level > me.Level)
                        continue;
                    long cost = s == ShopFoodYen ? store[i].Yen : store[i].Coin;
                    if (cost <= 0 || cost > money)
                        continue;
                    if (t.Level > bestLevel)
                    {
                        bestLevel = t.Level;
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    shop = s;
                    index = best;
                    price = s == ShopFoodYen ? store[best].Yen : store[best].Coin;
                    return true;
                }
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
            if (now < a.NoFoodUntil || Foods(me, world) > 0 || !PickFood(me, world, MoneyReserve(a), out shop, out index, out price))
                return Status.Done;
            string why;
            var go = GoToNpc(a, me, world, NpcTabemono, "going to Tabemono to buy food", out why);
            if (go == Status.Failed)
            {
                a.NoFoodUntil = now + 300000;
                return Status.Done;
            }

            if (go != Status.Done)
                return go;
            if (now - a.LastMenuAt < ActionGapMs)
                return Status.Running;
            a.LastMenuAt = now;
            long money = (shop == ShopFoodYen ? me.Progress.Yen : me.Progress.Xu) - MoneyReserve(a);
            int qty = (int)Math.Min(AgentConfig.FoodStock, money / Math.Max(1, price));
            if (qty <= 0)
                return Status.Done;
            a.Client.RequestShop(shop);
            a.Client.Buy(shop, index, qty);
            int got = Foods(me, world);
            string item = world.FindItem(world.Store(shop)[index].ItemId).Name;
            if (got <= 0)
            {
                a.NoFoodUntil = now + 300000;
                AgentManager.Remember(a, "Tabemono refused " + item + ": " + a.Session.LastNotice);
                return Status.Done;
            }

            AgentManager.Report(a, AgentEvent.Shop, "bought " + got + " " + item + " from Tabemono for " + (long)got * price + (shop == ShopFoodYen ? " yen" : " xu"));
            a.Knowledge.Learn("place:food", "Tabemono sells food (" + item + " " + price + (shop == ShopFoodYen ? " yen" : " xu") + ": HP and MP back every half second for 30 min) in map " + me.MapId + " " + me.Zone.Tile.Name);
            return Status.Done;
        }

        // ================================================================== recovering and shopping
        // HP is low: drink while potions last; otherwise buy some (village shop), otherwise get away from monsters.
        // There is no free regeneration for players (PlayerEffects.NaturalRegenPercent), so none for the AI either.
        private static Status Recover(NinjaAgent a, PlayerState me, GameWorld world, long now)
        {
            if (HpPercent(me) >= 90)
            {
                a.Activity = "recovered";
                return Status.Done;
            }

            if (DrinkHp(a, me, world, now))
            {
                a.Activity = "drinking a potion (HP " + HpPercent(me) + "%)";
                return Status.Running;
            }

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
                if (mob.IsDead || !mob.Chars.Contains(me.Record.Id))
                    continue;
                int d = Math.Abs(mob.X - me.X) + Math.Abs(mob.Y - me.Y);
                if (d < AgentConfig.SafeDistancePx && d < best)
                {
                    best = d;
                    threat = mob;
                }
            }

            if (threat != null)
            {
                a.Activity = "backing away from " + threat.Template.Name + " (" + why + ")";
                int dir = me.X >= threat.X ? 1 : -1;
                var grid = world.GetGrid(me.Zone.Tile);
                int tx = me.X + dir * 200;
                if (tx < 40 || tx > grid.PxW - 40)
                    tx = me.X - dir * 400; // cornered: run past it
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
            if (me.Record.TaskId < TaskDietSenTruCoc)
                return false; // Store.buy needs NV diệt sên trừ cóc (PlayerItems.BuyItem)
            int id;
            int price;
            int shop;
            return PickPotion(me, world, PlayerItems.TypeHp, out shop, out id, out price);
        }

        // the best potion of a kind the character can use and pay for (yen shop first, then xu)
        private static bool PickPotion(PlayerState me, GameWorld world, int type, out int shop, out int index, out int price)
        {
            foreach (int s in new[]
            {
                ShopPotionYen,
                ShopPotionXu
            }

            )
            {
                var store = world.Store(s);
                if (store == null)
                    continue;
                int best = -1, bestLevel = -1;
                for (int i = 0; i < store.Count; i++)
                {
                    var t = world.FindItem(store[i].ItemId);
                    if (t == null || t.Type != type || t.Level > me.Level)
                        continue;
                    long cost = s == ShopPotionYen ? store[i].Yen : store[i].Coin;
                    long money = s == ShopPotionYen ? me.Progress.Yen : me.Progress.Xu;
                    if (cost <= 0 || cost > money)
                        continue;
                    if (t.Level > bestLevel)
                    {
                        bestLevel = t.Level;
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    shop = s;
                    index = best;
                    price = s == ShopPotionYen ? store[best].Yen : store[best].Coin;
                    return true;
                }
            }

            shop = index = price = -1;
            return false;
        }

        /// <summary>Walks to Kiriko and buys HP (and some MP) potions with yen, else xu, as a player does in the shop.</summary>
        private static Status Shop(NinjaAgent a, PlayerState me, GameWorld world, long now, out string why)
        {
            why = null;
            if (me.Record.TaskId < TaskDietSenTruCoc)
            {
                why = "shops open after the quest NV diệt sên trừ cóc";
                return Status.Failed;
            }

            int want = PotionTarget(a); // spending style: thrifty characters carry fewer
            if (HpPotions(me, world) >= want)
                return BuyFood(a, me, world, now); // potions stocked: food next
            int shop, index, price;
            if (!PickPotion(me, world, PlayerItems.TypeHp, out shop, out index, out price))
            {
                if (HpPotions(me, world) >= AgentConfig.MinPotionsToFight || (HpPotions(me, world) > 0 && now - a.LastMenuAt < 5000))
                    return BuyFood(a, me, world, now); // enough to fight, or just bought what the money allowed
                why = "not enough money for potions";
                return Status.Failed;
            }

            var go = GoToNpc(a, me, world, NpcKiriko, "going to Kiriko to buy potions", out why);
            if (go != Status.Done)
                return go;
            if (now - a.LastMenuAt < ActionGapMs)
                return Status.Running;
            a.LastMenuAt = now;
            long money = shop == ShopPotionYen ? me.Progress.Yen : me.Progress.Xu;
            int qty = (int)Math.Min(want - HpPotions(me, world), money / Math.Max(1, price));
            if (qty <= 0)
            {
                why = "not enough money for potions";
                return Status.Failed;
            }

            a.Client.RequestShop(shop);
            int before = HpPotions(me, world);
            a.Client.Buy(shop, index, qty);
            int got = HpPotions(me, world) - before;
            if (got <= 0)
            {
                why = "the shop refused: " + a.Session.LastNotice;
                return Status.Failed;
            }

            // some MP potions too when the skill needs mana
            int mshop, mindex, mprice;
            if (me.SelectedSkill != null && me.SelectedSkill.ManaUse > 0 && MpPotions(me, world) < want / 2 && PickPotion(me, world, PlayerItems.TypeMp, out mshop, out mindex, out mprice))
            {
                long mm = mshop == ShopPotionYen ? me.Progress.Yen : me.Progress.Xu;
                int mq = (int)Math.Min(want / 2, mm / Math.Max(1, mprice));
                if (mq > 0)
                {
                    a.Client.RequestShop(mshop);
                    a.Client.Buy(mshop, mindex, mq);
                }
            }

            string item = world.FindItem(world.Store(shop)[index].ItemId).Name;
            AgentManager.Report(a, AgentEvent.Shop, "bought " + got + " " + item + " from Kiriko for " + (long)got * price + (shop == ShopPotionYen ? " yen" : " xu"));
            a.Knowledge.Learn("place:shop", "Kiriko sells potions (" + item + " " + price + (shop == ShopPotionYen ? " yen" : " xu") + ") in map " + me.MapId + " " + me.Zone.Tile.Name);
            return Status.Running; // the next tick finds the potions stocked and goes on to the food (BuyFood)
        }

        // ================================================================== trying unknown items (item ledger)
        /// <summary>The village NPC whose shop sells the item (Kiriko: potions, Tabemono: food), or -1.</summary>
        public static int SoldBy(GameWorld world, int itemId)
        {
            foreach (int s in new[]
            {
                ShopPotionYen,
                ShopPotionXu,
                ShopFoodYen,
                ShopFoodXu
            }

            )
            {
                var store = world.Store(s);
                if (store != null && store.Exists(e => e.ItemId == itemId))
                    return s == ShopFoodYen || s == ShopFoodXu ? NpcTabemono : NpcKiriko;
            }

            return -1;
        }

        /// <summary>User 2026-11-13: "a player who sees something new buys one to try it". The cheapest village-shop item
        /// this character may use and has never used (item ledger), affordable while keeping FoodMoneyReserve, not known
        /// to be useless, only with enough HP potions for a fight. price 0: one is already in the bag.</summary>
        public static bool PickTryItem(NinjaAgent a, PlayerState me, GameWorld world, long now, out int itemId, out long price)
        {
            itemId = -1;
            price = long.MaxValue;
            if (now < a.NoTryUntil || me.Record.TaskId < TaskDietSenTruCoc || HpPotions(me, world) < AgentConfig.MinPotionsToFight)
                return false;
            var h = a.Learning.ItemRewards;
            foreach (int s in new[]
            {
                ShopPotionYen,
                ShopPotionXu,
                ShopFoodYen,
                ShopFoodXu
            }

            )
            {
                var store = world.Store(s);
                if (store == null)
                    continue;
                bool yen = s == ShopPotionYen || s == ShopFoodYen;
                long money = (yen ? me.Progress.Yen : me.Progress.Xu) - MoneyReserve(a);
                foreach (var e in store)
                {
                    var t = world.FindItem(e.ItemId);
                    if (t == null || t.Level > me.Level || ((t.Gender == 0 || t.Gender == 1) && t.Gender != me.Record.Gender))
                        continue;
                    if (h.Knows(e.ItemId) || h.Useless(e.ItemId) || a.FailedItems.Contains(e.ItemId) || !CouldHelpNow(me, t, now))
                        continue;
                    long cost = Array.Exists(me.Inventory.Bag, it => it != null && it.Id == e.ItemId) ? 0 : yen ? e.Yen : e.Coin;
                    if (cost < 0 || (cost > 0 && cost > money) || cost >= price)
                        continue;
                    itemId = e.ItemId;
                    price = cost;
                }
            }

            return itemId >= 0;
        }

        // A player tries a potion when it can show what it does (Java Char.useItem refuses an HP potion at full HP,
        // "HP đã đầy."), food when no food effect runs; other items any time.
        private static bool CouldHelpNow(PlayerState me, NsoServer.World.ItemTemplateData t, long now)
        {
            if (t.Type == PlayerItems.TypeHp)
                return me.Hp < me.MaxHp;
            if (t.Type == PlayerItems.TypeMp)
                return me.Mp < me.MaxMp;
            if (t.Type == PlayerItems.TypeEat)
                return !Fed(me, now);
            return true;
        }

        // Shop step with an item id: buy one (if none in the bag) and use it once; ActionRewards writes what it did into
        // the ledger and the character's knowledge. Done after the try, also when the server refuses the use.
        private static Status TryItem(NinjaAgent a, PlayerState me, GameWorld world, long now, int itemId, out string why)
        {
            why = null;
            var t = world.FindItem(itemId);
            if (t == null || a.Learning.ItemRewards.Knows(itemId) || a.FailedItems.Contains(itemId))
                return Status.Done;
            int idx = Array.FindIndex(me.Inventory.Bag, it => it != null && it.Id == itemId);
            if (idx >= 0)
            {
                if (!CouldHelpNow(me, t, now))
                {
                    a.NoTryUntil = now + 120000;
                    return Status.Done;
                } // keep it for when it can show its use

                if (now - a.LastMenuAt < ActionGapMs)
                    return Status.Running;
                a.LastMenuAt = now;
                a.Activity = "trying " + t.Name;
                var before = me.Inventory.Bag[idx];
                int qty = before.Quantity;
                a.Client.UseItem(idx);
                if (me.Inventory.Bag[idx] == before && before.Quantity == qty && !a.Learning.ItemRewards.Knows(itemId))
                { // refused now (full HP, level...): keep it and try again later, not forever banned
                    a.NoTryUntil = now + 120000;
                    AgentManager.Remember(a, "could not use " + t.Name + " yet: " + a.Session.LastNotice);
                }

                return Status.Done;
            }

            int npc = SoldBy(world, itemId);
            if (npc < 0)
            {
                why = t.Name + " is not sold in the village shops";
                return Status.Failed;
            }

            var st = BuyFrom(a, me, world, now, npc, itemId, 1, out why);
            if (st == Status.Done && !Array.Exists(me.Inventory.Bag, it => it != null && it.Id == itemId))
            {
                why = "the shop refused " + t.Name + ": " + a.Session.LastNotice;
                st = Status.Failed;
            }

            if (st == Status.Failed)
                a.NoTryUntil = now + 300000;
            return st == Status.Done ? Status.Running : st; // bought: the next tick uses it
        }

        /// <summary>Buys a shop item by id from a village NPC's shop (food for quest 3). Done when bought.</summary>
        private static Status BuyFrom(NinjaAgent a, PlayerState me, GameWorld world, long now, int npc, int itemId, int qty, out string why)
        {
            why = null;
            int yenShop = npc == NpcTabemono ? ShopFoodYen : ShopPotionYen, xuShop = npc == NpcTabemono ? ShopFoodXu : ShopPotionXu;
            int shop = -1, index = -1;
            long cost = 0;
            foreach (int s in new[]
            {
                yenShop,
                xuShop
            }

            )
            {
                var store = world.Store(s);
                if (store == null)
                    continue;
                for (int i = 0; i < store.Count; i++)
                {
                    if (store[i].ItemId != itemId)
                        continue;
                    long c = (long)(s == yenShop ? store[i].Yen : store[i].Coin) * qty;
                    long money = s == yenShop ? me.Progress.Yen : me.Progress.Xu;
                    if (c <= money)
                    {
                        shop = s;
                        index = i;
                        cost = c;
                    }

                    break;
                }

                if (shop >= 0)
                    break;
            }

            if (shop < 0)
            {
                why = "not enough money (need yen or xu) for " + world.FindItem(itemId).Name;
                return Status.Failed;
            }

            var go = GoToNpc(a, me, world, npc, "going to buy " + world.FindItem(itemId).Name, out why);
            if (go != Status.Done)
                return go;
            if (now - a.LastMenuAt < ActionGapMs)
                return Status.Running;
            a.LastMenuAt = now;
            a.Client.RequestShop(shop);
            a.Client.Buy(shop, index, qty);
            AgentManager.Remember(a, "bought " + qty + " " + world.FindItem(itemId).Name + " for " + cost + (shop == yenShop ? " yen" : " xu"));
            return Status.Done;
        }
    }
}
