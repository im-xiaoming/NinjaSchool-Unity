using System;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class

    // Storage box, selling and throwing items: port of Char.openUIBox / itemBagToBox / itemBoxToBag (cmd 17 / 16),
    // Char.saleItem (cmd 14) and Char.throwItem (cmd -12), with the matching Service answers.
    // The box is opened at Kamakura ("Mở rương", Char.npcKamakura) and is saved with the inventory.
    public static class PlayerStorage
    {
        public const sbyte ITEM_SALE = 14, ITEM_BOX_TO_BAG = 16, ITEM_BAG_TO_BOX = 17, ME_THROW = -12;
        public const sbyte StackSplitSub = -85; // existing inputNumSplit NOT_MAP request, extended with box flag
        // Offline completion of the client's inputNumSplit command. Split only stackable supplies;
        // clone every instance field and commit both slots together before acknowledging.
        public static void SplitStack(Message message, PlayerState me, GameWorld world)
        {
            int index = message.Reader.ReadByte();
            int quantity = message.Reader.ReadInt();
            bool box = message.Reader.Remaining > 0 && message.Reader.ReadBoolean();
            if (box && !me.BoxOpen)
                return;
            var slots = box ? me.Inventory.Box : me.Inventory.Bag;
            if (index < 0 || index >= slots.Length || slots[index] == null)
                return;
            var source = slots[index];
            var template = world.FindItem(source.Id);
            if (template == null || !InventoryGrouping.CanStack(template.Type, template.IsUpToUp))
                return;
            if (quantity < 1 || quantity >= source.Quantity || quantity > InventoryGrouping.MaxStackQuantity || source.Quantity - quantity > InventoryGrouping.MaxStackQuantity)
            {
                PlayerService.ServerDialog(me, "Số lượng tách không hợp lệ.");
                return;
            }

            int free = Array.IndexOf(slots, null);
            if (free < 0)
            {
                PlayerService.ServerDialog(me, "Cần một ô trống để tách vật phẩm.");
                return;
            }

            var split = new PlayerItem
            {
                Id = source.Id,
                Index = free,
                Quantity = quantity,
                Upgrade = source.Upgrade,
                System = source.System,
                Yen = source.Yen,
                Expire = source.Expire,
                IsLock = source.IsLock
            };
            foreach (var option in source.Options)
                split.Options.Add(new PlayerItemOption { Id = option.Id, Param = option.Param });
            source.Quantity -= quantity;
            slots[free] = split;
            PlayerItems.Save(me);
            var reply = new Message(Cmd.NOT_MAP);
            reply.Writer.WriteSByte(StackSplitSub);
            reply.Writer.WriteBoolean(box);
            reply.Writer.WriteByte(index);
            reply.Writer.WriteByte(free);
            reply.Writer.WriteShort(source.Id);
            reply.Writer.WriteShort(source.Quantity);
            reply.Writer.WriteShort(split.Quantity);
            me.Session.Send(reply);
        }

        /// <summary>Most items allowed on the ground of a zone before throwing is refused (Java: zone.getNumberItem() > 100).</summary>
        public static int MaxGroundItems = 100;
        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // whether two items stack (Char.itemBagToBox / itemBoxToBag: same id and lock, stackable, no expiry)
        private static int StackIndex(PlayerItem[] slots, PlayerItem item, GameWorld world)
        {
            var t = world.FindItem(item.Id);
            if (t == null || !t.IsUpToUp)
                return -1;
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s != null && s != item && s.Id == item.Id && s.IsLock == item.IsLock && !(s.Expire > 0))
                    return i;
            }

            return -1;
        }

        // Char.openUIBox: open UI 4 and send the box contents (Service.sendDataBox)
        public static void OpenBox(PlayerState me, GameWorld world)
        {
            me.BoxOpen = true;
            PlayerService.OpenUi(me, 4);
            me.Session.Send(EnterWorld.DataBox(me, world));
        }

        // Char.itemBagToBox (cmd 17)
        public static void BagToBox(Message m, PlayerState me, GameWorld world)
        {
            if (!me.BoxOpen)
                return; // getCommandBox() != RUONG_DO
            int index = m.Reader.ReadSByte();
            var bag = me.Inventory.Bag;
            var box = me.Inventory.Box;
            if (index < 0 || index >= bag.Length || bag[index] == null)
                return;
            var item = bag[index];
            if (item.Expire > 0 && item.Expire < Now())
            {
                PlayerItems.RemoveItem(me, index, item.Quantity, true);
                return;
            }

            int stack = StackIndex(box, item, world);
            int to = stack;
            if (stack >= 0)
                box[stack].Quantity += item.Quantity;
            else
            {
                to = Array.IndexOf(box, null);
                if (to < 0)
                {
                    PlayerService.ServerDialog(me, "Rương đồ không đủ chỗ trống");
                    return;
                }

                item.Index = to;
                box[to] = item;
            }

            bag[index] = null;
            Moved(me, ITEM_BAG_TO_BOX, index, to);
        }

        // Char.itemBoxToBag (cmd 16)
        public static void BoxToBag(Message m, PlayerState me, GameWorld world)
        {
            if (!me.BoxOpen)
                return;
            int index = m.Reader.ReadSByte();
            var bag = me.Inventory.Bag;
            var box = me.Inventory.Box;
            if (index < 0 || index >= box.Length || box[index] == null)
                return;
            var item = box[index];
            int stack = StackIndex(bag, item, world);
            int to = stack;
            if (stack >= 0)
                bag[stack].Quantity += item.Quantity;
            else
            {
                to = Array.IndexOf(bag, null);
                if (to < 0)
                {
                    PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
                    return;
                }

                item.Index = to;
                bag[to] = item;
            }

            box[index] = null;
            Moved(me, ITEM_BOX_TO_BAG, index, to);
        }

        private static void Moved(PlayerState me, sbyte cmd, int from, int to)
        {
            PlayerItems.Save(me);
            var r = new Message(cmd);
            r.Writer.WriteByte(from);
            r.Writer.WriteByte(to);
            me.Session.Send(r);
        }

        // Char.saleItem (cmd 14): index, then an optional int or short quantity
        public static void Sale(Message m, PlayerState me, GameWorld world)
        {
            int index = m.Reader.ReadSByte();
            int quantity = 1;
            if (m.Reader.Remaining == 4)
                quantity = m.Reader.ReadInt();
            else if (m.Reader.Remaining == 2)
                quantity = m.Reader.ReadShort();
            var bag = me.Inventory.Bag;
            if (index < 0 || index >= bag.Length || bag[index] == null)
                return;
            var item = bag[index];
            var t = world.FindItem(item.Id);
            if (t == null)
                return;
            if (item.Upgrade > 0 && PlayerService.IsWearable(t))
            {
                PlayerService.ServerDialog(me, "Vật phẩm này đã nâng cấp không thể bán.");
                return;
            }

            if (quantity < 1 || quantity > item.Quantity)
            {
                PlayerService.ServerDialog(me, "Số lượng bán không hợp lệ.");
                return;
            }

            if (t.Type == 24 || t.Type == 25)
            {
                PlayerService.ServerDialog(me, "Không thể bán vật phẩm nhiệm vụ.");
                return;
            }

            if (t.Type == 12)
            {
                PlayerService.ServerDialog(me, "Vật phẩm quý giá không thể bán.");
                return;
            } // isTypeYoroi

            long yen = (long)quantity * item.Yen;
            me.Progress.Yen = Math.Max(0, Math.Min(2000000000L, me.Progress.Yen + yen));
            PlayerItems.RemoveItem(me, index, quantity, false);
            PlayerItems.Save(me);
            var r = new Message(ITEM_SALE); // Service.saleItem
            r.Writer.WriteByte(index);
            r.Writer.WriteInt((int)Math.Min(int.MaxValue, me.Progress.Yen));
            r.Writer.WriteShort(quantity);
            me.Session.Send(r);
        }

        // Char.throwItem (cmd -12): an unlocked item goes to the ground next to the character
        public static void Throw(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead)
                return;
            var zone = me.Zone;
            if (zone.ItemMaps.Count > MaxGroundItems)
            {
                PlayerService.ServerDialog(me, "Vật phẩm ở đất đã quá nhiều.");
                return;
            }

            int index = m.Reader.ReadSByte();
            var bag = me.Inventory.Bag;
            if (index < 0 || index >= bag.Length || bag[index] == null || bag[index].IsLock)
                return;
            var item = bag[index];
            if (item.Id >= 292 && item.Id <= 305)
            {
                PlayerService.ServerDialog(me, "Không thể vất vật phẩm này, chỉ có thể bán.");
                return;
            }

            var grid = world.GetGrid(zone.Tile);
            short x = (short)(me.X + new Random().Next(-30, 31));
            x = (short)Math.Max(24, Math.Min(grid.PxW - 24, x));
            short y = grid.CollisionY(x, me.Y);
            bag[index] = null;
            var it = new ItemMapEntry
            {
                Id = zone.NumberDropItem++,
                X = x,
                Y = y,
                OwnerId = -1,
                CreatedAt = Now(),
                Item = item
            };
            zone.ItemMaps.Add(it);
            PlayerItems.Save(me);
            var r = new Message(ME_THROW); // Service.throwItem
            r.Writer.WriteByte(index);
            r.Writer.WriteShort(it.Id);
            r.Writer.WriteShort(x);
            r.Writer.WriteShort(y);
            me.Session.Send(r);
            foreach (PlayerState p in zone.Players)
                if (p != me)
                    PlayerService.AddItemMapTo(p, it);
        }
    }
}
