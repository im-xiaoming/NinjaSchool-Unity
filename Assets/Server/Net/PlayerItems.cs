using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Zone = NsoServer.World.Zone;
    using Mob = NsoServer.World.Mob;
    using Math = System.Math;   // the client has its own global Math class

    // Port of the item side of com.nsoz.model.Char for ordinary maps:
    //   addItemToBag, removeItem, useItem (equipment, HP/MP potions, food, skill books), itemBodyToBag, buyItem + Store.buy,
    //   requestItem (shop list), requestItemInfo, pickItem, Mob.dropItem (task items, yen, random items, equipment),
    //   Zone.updateItemMap (items on the ground disappear after 30 s).
    public static class PlayerItems
    {
        // ItemTemplate types
        public const int TypeWeapon = 1, TypeHp = 16, TypeMp = 17, TypeEat = 18, TypeMoney = 19, TypeTaskSave = 23, TypeTaskWait = 24, TypeTask = 25;
        private const int ItemYen = 12, ItemComNam = 23, ItemWoodenSword = 194, ItemSushi = 218;
        private const int MaxYen = 2000000000;
        private const int MobTrainingDummy = 0;
        // Legacy fallback for detached test states. Running worlds use their immutable LootRules snapshot,
        // editable through the dev workbench; probabilities remain the Java defaults.
        public static int DropLevelGapLimit = 10;

        private static readonly Random Rng = new Random();
        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        private static int NextInt(int min, int max) { return min >= max ? max : Rng.Next(max + 1 - min) + min; }

        // Char.receiveWoodenSword: shared quest reward/preview factory, not a new character class.
        public static PlayerItem CreateTaskWoodenSword()
        {
            var sword = NewItem(ItemWoodenSword);
            sword.Options.Add(new PlayerItemOption { Id = 0, Param = 10 });
            sword.Options.Add(new PlayerItemOption { Id = 8, Param = 1 });
            return sword;
        }

        // ---------------------------------------------------------------- bag basics

        // Char.getSlotNull
        public static int SlotNull(PlayerState me)
        {
            int n = 0;
            foreach (var it in me.Inventory.Bag) if (it == null) n++;
            return n;
        }

        // Char.getIndexItemByIdInBag(id, isLock)
        public static int IndexInBag(PlayerState me, int id, bool isLock)
        {
            var bag = me.Inventory.Bag;
            for (int i = 0; i < bag.Length; i++) if (bag[i] != null && bag[i].Id == id && bag[i].IsLock == isLock) return i;
            return -1;
        }

        public static int IndexInBag(PlayerState me, int id)
        {
            var bag = me.Inventory.Bag;
            for (int i = 0; i < bag.Length; i++) if (bag[i] != null && bag[i].Id == id) return i;
            return -1;
        }

        // a new item instance (ItemFactory.newItem)
        public static PlayerItem NewItem(int id, int quantity = 1, bool isLock = true)
        {
            return new PlayerItem { Id = id, Quantity = quantity, IsLock = isLock, Expire = -1 };
        }

        // Char.addItemToBag: stack on an existing slot or take the first free one
        public static bool AddItemToBag(PlayerState me, GameWorld world, PlayerItem item)
        {
            if (item == null) return false;
            var t = world.FindItem(item.Id);
            if (t == null) return false;
            var bag = me.Inventory.Bag;
            int index = IndexInBag(me, item.Id, item.IsLock);
            if (index == -1 || !t.IsUpToUp || bag[index].Expire > 0)
            {
                for (int i = 0; i < bag.Length; i++)
                {
                    if (bag[i] != null) continue;
                    item.Index = i;
                    bag[i] = item;
                    PlayerService.AddItem(me, item, t);
                    Save(me);
                    return true;
                }
                return false;
            }
            bag[index].Quantity += item.Quantity;
            PlayerService.AddItem(me, bag[index], t);
            Save(me);
            return true;
        }

        // Char.removeItem(index, quantity, isUpdate)
        public static void RemoveItem(PlayerState me, int index, int quantity, bool update)
        {
            var bag = me.Inventory.Bag;
            if (index < 0 || index >= bag.Length || bag[index] == null) return;
            bag[index].Quantity -= quantity;
            if (bag[index].Quantity <= 0) bag[index] = null;
            if (update) PlayerService.UseItemUpToUp(me, index, quantity);
            Save(me);
        }

        // Char.addYen / addCoin
        public static void AddYen(PlayerState me, long add)
        {
            me.Progress.Yen = Math.Max(0, Math.Min(MaxYen, me.Progress.Yen + add));
            PlayerService.AddYen(me, (int)add);
            Save(me);
        }

        public static void AddXu(PlayerState me, long add)
        {
            me.Progress.Xu = Math.Max(0, Math.Min(MaxYen, me.Progress.Xu + add));
            PlayerService.AddXu(me, (int)add);
            Save(me);
        }

        // writes the inventory and progress back into PlayerRecord.Data (flushed to disk when the character leaves)
        public static void Save(PlayerState me)
        {
            if (me.Inventory == null || me.Progress == null) return;
            me.Progress.PotentialPoint = me.PotentialPoint;
            me.Progress.SkillPoint = me.SkillPoint;
            if (me.Record.ClassId > 0) me.Progress.Potential = (int[])me.Potential.Clone();
            me.Progress.FoodEffect = null;
            long now = Now();
            foreach (var effect in me.Effects)
            {
                if (effect.Type != 0 || effect.IsExpired(now)) continue;
                me.Progress.FoodEffect = new[] { (long)effect.TemplateId, (long)effect.Param, effect.StartAt, effect.EndAt };
                break;
            }
            me.Record.Data = PlayerProgress.ToJson(me.Inventory, me.Progress);
        }

        // ---------------------------------------------------------------- use / equip (cmd 11, 15)

        // Char.useItem
        public static void UseItem(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead) return;
            int indexUI = m.Reader.ReadSByte();
            var bag = me.Inventory.Bag;
            if (indexUI < 0 || indexUI >= bag.Length) return;
            var item = bag[indexUI];
            if (item == null || item.Quantity <= 0) return;
            var t = world.FindItem(item.Id);
            if (t == null || me.Level < t.Level) return;
            if (item.Expire > 0 && item.Expire < Now()) { RemoveItem(me, indexUI, item.Quantity, true); return; }
            if ((t.Gender == 0 || t.Gender == 1) && t.Gender != me.Record.Gender) return;
            if (PlayerService.IsWearable(t)) { UseEquipment(me, world, item, t); return; }
            if (IsMartialArtBook(item.Id)) { PlayerSkills.LearnSkill(me, world, item); return; }
            if (t.Type == TypeHp)
            {
                if (me.Hp >= me.MaxHp) { PlayerService.ServerMessage(me, "HP đã đầy."); return; }
                int param = item.Id == 13 ? 25 : item.Id == 14 ? 90 : item.Id == 15 ? 230 : item.Id == 16 ? 400 : item.Id == 17 ? 650 : item.Id == 565 ? 1500 : 0;
                PlayerEffects.SetEffect(me, world, 21, 3000, param);   // "Hiệu ứng bom máu": HP every half second for 3 s
                RemoveItem(me, indexUI, 1, true);
                return;
            }
            if (t.Type == TypeMp)
            {
                if (me.Mp >= me.MaxMp) PlayerService.ServerMessage(me, "MP đã đầy.");
                int mp = item.Id == 18 ? 150 : item.Id == 19 ? 500 : item.Id == 20 ? 1000 : item.Id == 21 ? 2000 : item.Id == 22 ? 3500 : item.Id == 566 ? 5000 : 0;
                me.Mp = Math.Min(me.MaxMp, me.Mp + mp);
                PlayerService.UpdateMp(me);
                RemoveItem(me, indexUI, 1, true);
                return;
            }
            if (t.Type == TypeEat)
            {
                int time = 0, param = 0, template = 0;
                switch (item.Id)
                {
                    case 23: time = 1800000; param = 3; template = 0; break;
                    case 24: time = 1800000; param = 20; template = 1; break;
                    case 25: time = 1800000; param = 30; template = 2; break;
                    case 26: time = 1800000; param = 40; template = 3; break;
                    case 27: time = 1800000; param = 50; template = 4; break;
                    case 29: time = 1800000; param = 60; template = 28; break;
                    case 30: time = 259200000; param = 60; template = 28; break;
                    case 249: time = 259200000; param = 40; template = 3; break;
                    case 250: time = 259200000; param = 50; template = 4; break;
                    case 409: time = 86400000; param = 150; template = 30; break;
                    case 410: time = 86400000; param = 200; template = 31; break;
                    case 567: time = 86400000; param = 300; template = 35; break;
                }
                PlayerEffects.SetEffect(me, world, template, time, param);   // food: HP and MP every half second
                RemoveItem(me, indexUI, t.IsUpToUp ? 1 : item.Quantity, true);
                PlayerTasks.OnEat(me, world);
            }
        }

        // ItemTemplate.isTypeMartialArtBook
        public static bool IsMartialArtBook(int id)
        {
            return (id >= 40 && id <= 93) || (id >= 311 && id <= 316) || (id >= 375 && id <= 380) || (id >= 547 && id <= 563) ||
                   (id >= 839 && id <= 844) || (id >= 1125 && id <= 1130);
        }

        // weapons each class may use (ItemTemplate.isKiem / isTieu / isKunai / isCung / isDao / isQuat)
        private static readonly int[][] ClassWeapons =
        {
            new[] { 194, 94, 95, 96, 97, 98, 369, 506, 632, 331, 1111 },      // sword (class 0 and 1)
            new[] { 114, 115, 116, 117, 118, 370, 332, 507, 633, 1112 },     // dart (2)
            new[] { 99, 100, 101, 102, 103, 333, 508, 634, 371, 1113 },      // kunai (3)
            new[] { 109, 110, 111, 112, 113, 372, 334, 509, 635, 1114 },     // bow (4)
            new[] { 104, 105, 106, 107, 108, 373, 335, 510, 636, 1115 },     // blade (5)
            new[] { 119, 120, 121, 122, 123, 374, 336, 511, 637, 1116 }      // fan (6)
        };

        private static bool WeaponFitsClass(int classId, int itemId)
        {
            int set = classId <= 1 ? 0 : classId - 1;
            return Array.IndexOf(ClassWeapons[set], itemId) >= 0;
        }

        // Char.useEquipment: swap the bag item with the one worn in that slot
        private static void UseEquipment(PlayerState me, GameWorld world, PlayerItem item, ItemTemplateData t)
        {
            int indexUI = item.Index;
            int slot = t.Type;
            bool isFashion = t.Fashion > -1;
            var target = isFashion ? me.Inventory.Fashion : me.Inventory.Equipment;
            if (!isFashion)
            {
                if (t.Type == TypeWeapon && !WeaponFitsClass(me.Record.ClassId, item.Id)) { PlayerService.ServerDialog(me, "Vũ khí không thích hợp."); return; }
                if (me.Record.ClassId > 0 && item.Id == ItemWoodenSword) { PlayerService.ServerDialog(me, "Vũ khí không thích hợp."); return; }
                if (slot == 15) return;   // secret books need class rules that are not ported
            }
            var worn = target[slot];
            if (worn != null) { worn.Index = indexUI; worn.IsLock = true; me.Inventory.Bag[indexUI] = worn; }
            else me.Inventory.Bag[indexUI] = null;
            item.IsLock = true;
            item.Index = slot;
            target[slot] = item;
            if (!isFashion && t.Type == TypeWeapon) PlayerTasks.OnEquipWeapon(me, world);
            if (!isFashion) PlayerTasks.OnEquipUpgraded(me, world, item, t);
            PlayerCombat.SetAbility(me, world);
            PlayerService.UseItem(me, indexUI);
            Save(me);
        }

        // Char.itemBodyToBag
        public static void ItemBodyToBag(Message m, PlayerState me, GameWorld world)
        {
            int indexUI = m.Reader.ReadSByte();
            if (indexUI < 0) return;
            var source = indexUI < 16 ? me.Inventory.Equipment : me.Inventory.Fashion;
            int slot = indexUI < 16 ? indexUI : indexUI - 16;
            if (slot >= source.Length || source[slot] == null) return;
            var bag = me.Inventory.Bag;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] != null) continue;
                var item = source[slot];
                item.IsLock = true;
                item.Index = i;
                bag[i] = item;
                source[slot] = null;
                PlayerCombat.SetAbility(me, world);
                PlayerService.ItemBodyToBag(me, indexUI, i);
                Save(me);
                return;
            }
            PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
        }

        // ---------------------------------------------------------------- shops (cmd 13, sub -103, cmd 42)

        // Char.requestItem: the client asks for the content of a shop window
        // Char.bagSort (Java): merge stackable, non-expiring items with the same template and lock state,
        // then pack every item to the front of the bag. The client runs the same Char.bagSort when it
        // receives BAG_SORT (-107), so both sides end with the same layout. No trade exists offline,
        // so Java's "warningTrade" check is not needed.
        public const sbyte BagSortSub = -107; // CMD.BAG_SORT
        public static void BagSort(PlayerState me, GameWorld world)
        {
            var bag = me.Inventory.Bag;
            var items = new List<PlayerItem>();
            foreach (var it in bag)
            {
                if (it == null) continue;
                var t = world.FindItem(it.Id);
                if (t != null && t.IsUpToUp && !(it.Expire > 0)) items.Add(it);
            }
            for (int i = 0; i < items.Count; i++)
            {
                var a = items[i];
                if (a == null) continue;
                for (int j = i + 1; j < items.Count; j++)
                {
                    var b = items[j];
                    if (b != null && b.Id == a.Id && b.IsLock == a.IsLock)
                    {
                        a.Quantity += b.Quantity;
                        bag[b.Index] = null;
                        items[j] = null;
                    }
                }
            }
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] == null) continue;
                for (int j = 0; j <= i; j++)
                {
                    if (bag[j] == null)
                    {
                        bag[j] = bag[i];
                        bag[j].Index = j;
                        bag[i] = null;
                        break;
                    }
                }
            }
            Save(me);
            me.Session.Send(PlayerService.Sub(BagSortSub));
        }

        public static void RequestShop(Message m, PlayerState me, GameWorld world)
        {
            int type = m.Reader.ReadSByte();
            var store = world.Store(type);
            if (store != null) PlayerService.OpenUiShop(me, type, store);
        }

        // Char.buyItem + Store.buy
        public static void BuyItem(Message m, PlayerState me, GameWorld world)
        {
            if (me.Record.TaskId < PlayerTasks.TaskDietSenTruCoc)
            {
                PlayerService.ServerDialog(me, "Hãy hoàn nhiệm vụ diệt trừ cóc để sử dụng chức năng này.");
                return;
            }
            int typeUI = m.Reader.ReadSByte();
            int indexUI = m.Reader.ReadSByte();
            int quantity = m.Reader.Remaining >= 2 ? (ushort)m.Reader.ReadShort() : 1;
            quantity = Math.Min(quantity, 30000);
            if (quantity < 1) { PlayerService.ServerDialog(me, "Số lượng không hợp lệ!"); return; }
            var store = world.Store(typeUI);
            if (store == null || indexUI < 0 || indexUI >= store.Count) return;
            var s = store[indexUI];
            var t = world.FindItem(s.ItemId);
            if (t == null) return;
            int slotNull = SlotNull(me);
            if ((t.IsUpToUp && slotNull == 0) || (!t.IsUpToUp && slotNull < quantity)) { PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống"); return; }
            long xu = (long)s.Coin * quantity, yen = (long)s.Yen * quantity, gold = (long)s.Gold * quantity;
            if (gold > 0) { PlayerService.ServerDialog(me, "Vật phẩm cần lượng, bản offline không có lượng."); return; }
            if (xu > me.Progress.Xu || yen > me.Progress.Yen) { PlayerService.ServerDialog(me, "Không đủ tiền."); return; }
            me.Progress.Xu -= xu;
            me.Progress.Yen -= yen;
            PlayerService.Buy(me);
            int n = t.IsUpToUp ? 1 : quantity;
            for (int i = 0; i < n; i++)
            {
                var item = FromStore(s, t, true);
                item.Quantity = t.IsUpToUp ? quantity : 1;
                if (yen > 0) item.IsLock = true;
                AddItemToBag(me, world, item);
            }
            if (s.ItemId == ItemComNam) PlayerTasks.OnBuyComNam(me, world, quantity);
            Save(me);
        }

        // Converter.toItem(ItemStore, RANDOM_OPTION): a shop item with random options between the minimum and the maximum
        public static PlayerItem FromStore(StoreItem s, ItemTemplateData t, bool random)
        {
            var item = NewItem(s.ItemId, 1, s.IsLock);
            if (s.Expire != -1) item.Expire = Now() + s.Expire;
            if (PlayerService.IsWearable(t) || (t.Type >= 29 && t.Type <= 34))
            {
                item.System = s.Sys;
                for (int i = 0; i < s.OptionIds.Length; i++)
                    item.Options.Add(new PlayerItemOption { Id = s.OptionIds[i], Param = random ? NextInt(s.MinParams[i], s.MaxParams[i]) : s.MaxParams[i] });
            }
            return item;
        }

        // Char.requestItemInfo for shops (types 2, 6-9, 14-29, 32, 34), the bag (3) and the body (5)
        public static void RequestItemInfo(Message m, PlayerState me, GameWorld world)
        {
            int typeUI = m.Reader.ReadSByte();
            int indexUI = m.Reader.ReadSByte();
            if (indexUI < 0) return;
            if (typeUI == 3)
            {
                var bag = me.Inventory.Bag;
                if (indexUI < bag.Length && bag[indexUI] != null) PlayerService.ItemInfo(me, bag[indexUI], world.FindItem(bag[indexUI].Id), typeUI, indexUI);
            }
            else if (typeUI == 5)
            {
                var src = indexUI < 16 ? me.Inventory.Equipment : me.Inventory.Fashion;
                int slot = indexUI < 16 ? indexUI : indexUI - 16;
                if (slot < src.Length && src[slot] != null) PlayerService.ItemInfo(me, src[slot], world.FindItem(src[slot].Id), typeUI, indexUI);
            }
            else
            {
                var store = world.Store(typeUI);
                if (store != null && indexUI < store.Count) PlayerService.ItemStoreInfo(me, store[indexUI], world.FindItem(store[indexUI].ItemId), typeUI, indexUI);
            }
        }

        // ---------------------------------------------------------------- drops and pickup

        // Mob.dead excludes BU_NHIN from loot. Offline deviation: apply that exclusion to every drop path,
        // and block all loot at the user-configured overlevel limit before quest drops or currency rewards.
        public static bool CanDropLoot(PlayerState killer, Mob mob, LootRules rules = null)
        {
            return (!(rules == null || rules.BlockTrainingDummy) || mob.Template.Id != MobTrainingDummy) &&
                (!(rules == null || rules.BlockOverlevel) || killer.Level - mob.Level < Math.Max(1, rules == null ? DropLevelGapLimit : rules.OverlevelGap));
        }

        // Mob.dead for ordinary maps (non-boss): quest item, then 10 % yen / 10 % item / 5 % equipment for monsters within 7 levels
        public static void OnMobDead(PlayerState killer, GameWorld world, Zone zone, Mob mob)
        {
            if (!CanDropLoot(killer, mob, world == null ? null : world.LootRules)) return;
            int taskItem = PlayerTasks.TaskItemFor(killer, mob.Template.Id);
            if (taskItem != -1 && (Rng.Next(3) == 0 || mob.Template.Id == 69))   // heo rừng always drops the axe
                Drop(killer, world, zone, mob, NewItem(taskItem));
            if (mob.IsBoss) return;
            int dLevel = Math.Abs(mob.Level - killer.Level);
            if (world.LootRules.LimitOrdinaryLoot && dLevel > world.LootRules.OrdinaryMaxGap) return;
            int r = Rng.Next(100);
            if (r < 10)
            {
                // Mob.dropItem(YEN): ordinary monsters give yen straight to the killer
                int yen = NextInt(mob.Level * 15, (mob.Level + 10) * 15);
                if (mob.LevelBoss == 1) yen *= 20; else if (mob.LevelBoss == 2) yen *= 30;
                AddYen(killer, yen);
                PlayerService.ServerMessage(killer, "Bạn nhận được " + yen + " yên.");
            }
            else if (r < 20)
            {
                int id = world.RandomDropItem(mob.Level, mob.IsBoss);
                var item = NewItem(id);
                item.IsLock = id < 12 && mob.LevelBoss == 0;
                Drop(killer, world, zone, mob, item);
            }
            else if (r < 25)
            {
                int min = mob.Level / 10 * 10, max = Math.Min(min + 9, 69);
                var list = world.EquipmentInLevelRange(min, max);
                if (list.Count == 0) return;
                var s = list[Rng.Next(list.Count)];
                var item = FromStore(s, world.FindItem(s.ItemId), true);
                int remove = item.Options.Count > 1 ? Rng.Next(item.Options.Count - 1) : 0;
                for (int i = 0; i < remove; i++) item.Options.RemoveAt(Rng.Next(item.Options.Count));
                if (remove > 0) item.Yen = 5;
                item.IsLock = false;
                Drop(killer, world, zone, mob, item);
            }
        }

        // Mob.dropItem(SUSHI): a roasted monster (skill Kakyuu) leaves sushi that heals `heal` HP when picked up
        public static void DropSushi(PlayerState owner, GameWorld world, Zone zone, Mob mob, int heal)
        {
            Drop(owner, world, zone, mob, NewItem(ItemSushi, Math.Max(1, heal)));
        }

        // Mob.dropItem: put the item on the ground next to the monster
        private static void Drop(PlayerState owner, GameWorld world, Zone zone, Mob mob, PlayerItem item)
        {
            if (!CanDropLoot(owner, mob, world == null ? null : world.LootRules)) return; // includes DropSushi, which bypasses OnMobDead
            if (zone.ItemMaps.Count > 100) return;
            var t = world.FindItem(item.Id);
            if (t == null) return;
            if (t.Type == TypeTask) item.IsLock = true;
            item.Expire = -1;
            short x = (short)NextInt(mob.X - 20, mob.X + 20);
            var grid = world.GetGrid(zone.Tile);
            if (x < 50) x = 50;
            else if (x > grid.PxW - 50) x = (short)(grid.PxW - 50);
            short y = grid.CollisionY(x, (short)(mob.Y / 24 * 24));
            var it = new ItemMapEntry { Id = zone.NumberDropItem++, X = x, Y = y, OwnerId = owner.Record.Id, CreatedAt = Now(), Item = item };
            zone.ItemMaps.Add(it);
            PlayerService.AddItemMap(zone, it);
        }

        // Char.pickItem (cmd -14)
        public static void PickItem(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead) return;
            short id = m.Reader.ReadShort();
            var zone = me.Zone;
            ItemMapEntry it = null;
            foreach (var x in zone.ItemMaps) if (x.Id == id) { it = x; break; }
            if (it == null || it.PickedUp) return;
            long dx = it.X - me.X, dy = it.Y - me.Y;
            if (dx * dx + dy * dy >= 150 * 150) return;
            var item = it.Item;
            var t = world.FindItem(item.Id);
            if (t == null) return;
            bool canStack = t.IsUpToUp && IndexInBag(me, item.Id, item.IsLock) != -1;
            if (SlotNull(me) == 0 && !canStack) { PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống"); return; }
            if (it.OwnerId != -1 && it.OwnerId != me.Record.Id && Now() - it.CreatedAt < 20000) { PlayerService.ServerMessage(me, "Vật phẩm của người khác"); return; }
            it.PickedUp = true;
            if (item.Id == ItemSushi)
            {
                me.Hp = Math.Min(me.MaxHp, me.Hp + item.Quantity);
                PlayerService.LoadHp(me);
                PlayerService.UpdateHp(me);
            }
            else if (t.Type == TypeMoney) AddYen(me, item.Quantity);
            else
            {
                if (t.Type == TypeTask || t.Type == TypeTaskWait || t.Type == TypeTaskSave) item.IsLock = true;
                AddItemToBag(me, world, item);
            }
            PlayerService.PickItem(me, it, t);
            zone.ItemMaps.Remove(it);
            PlayerTasks.OnPickItem(me, world, item, t);
        }

        // Zone.updateItemMap: items vanish 30 s after they were dropped
        public static void UpdateItemMaps(Zone zone)
        {
            if (zone.ItemMaps.Count == 0) return;
            long now = Now();
            for (int i = zone.ItemMaps.Count - 1; i >= 0; i--)
            {
                var it = zone.ItemMaps[i];
                if (now - it.CreatedAt < 30000) continue;
                zone.ItemMaps.RemoveAt(i);
                PlayerService.RemoveItemMap(zone, it.Id);
            }
        }
    }
}
