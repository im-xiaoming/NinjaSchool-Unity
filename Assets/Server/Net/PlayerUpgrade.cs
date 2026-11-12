using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class

    // Kenshinto's forge: port of com.nsoz.model.Char.upgradeItem (cmd 21), upPearl (cmd 19 xu / 20 yen+xu "luyện đá")
    // and splitItem (cmd 22), with Item.next and the GameData tables. Answers use Service.upgrade / upPearl / splitItem.
    // "Cẩn thận" (careful upgrade) works as in Java: +50 % chance, costs GoldUp[level] luong.
    public static class PlayerUpgrade
    {
        public const int CmdUpPearl = 19, CmdUpPearlLock = 20, CmdUpgrade = 21, CmdSplit = 22;
        private const int TypeCrystal = 26, TypeInsurance = 28;

        // GameData: value of each crystal (đá cấp 1..12 = item 0..11) and what each upgrade level needs
        public static readonly int[] UpCrystal = { 1, 4, 16, 64, 256, 1024, 4096, 16384, 65536, 262144, 1048576, 4194304 };
        public static readonly int[] UpClothe = { 4, 9, 33, 132, 177, 256, 656, 2880, 3968, 6016, 13440, 54144, 71680, 108544, 225280, 1032192 };
        public static readonly int[] UpAdorn = { 6, 14, 50, 256, 320, 512, 1024, 5120, 6016, 9088, 19904, 86016, 108544, 166912, 360448, 1589248 };
        public static readonly int[] UpWeapon = { 18, 42, 132, 627, 864, 1360, 2816, 13824, 17792, 26880, 54016, 267264, 315392, 489472, 1032192, 4587520 };
        public static readonly int[] CoinUpCrystal = { 10, 40, 160, 640, 2560, 10240, 40960, 163840, 655360, 1310720, 3932160, 11796480 };
        public static readonly int[] CoinUpClothe = { 120, 270, 990, 3960, 5310, 7680, 19680, 86400, 119040, 180480, 403200, 1624320, 2150400, 3256320, 6758400, 10137600 };
        public static readonly int[] CoinUpAdorn = { 180, 420, 1500, 7680, 9600, 15360, 30720, 153600, 180480, 272640, 597120, 2580480, 3256320, 5007360, 10813440, 16220160 };
        public static readonly int[] CoinUpWeapon = { 540, 1260, 3960, 18810, 25920, 40800, 84480, 414720, 533760, 806400, 1620480, 8017920, 9461760, 14684160, 22026240, 33039360 };
        public static readonly int[] GoldUp = { 1, 2, 3, 4, 5, 10, 15, 20, 50, 100, 150, 200, 300, 400, 500, 600 };
        public static readonly int[] MaxPercent = { 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 2 };

        /// <summary>Config.getMaxPercentAdd: extra fraction added to the success cap (0 = original).</summary>
        public static double MaxPercentAdd = 0;

        private static readonly Random rnd = new Random();

        public static bool IsWeapon(ItemTemplateData t) { return t.Type == 1; }
        public static bool IsClothe(ItemTemplateData t) { return t.Type == 0 || t.Type == 2 || t.Type == 4 || t.Type == 6 || t.Type == 8; }
        public static bool IsAdorn(ItemTemplateData t) { return t.Type == 3 || t.Type == 5 || t.Type == 7 || t.Type == 9; }

        // ItemTemplate.getUpMax
        public static int UpMax(ItemTemplateData t)
        {
            int lv = t.Level;
            if (lv < 10) return 0;
            if (lv < 20) return 4;
            if (lv < 40) return 8;
            if (lv < 50) return 12;
            if (lv < 60) return 14;
            return 16;
        }

        // Item.next: upgrade levels and the option growth that comes with them
        public static void Next(PlayerItem item, int next)
        {
            if (next == 0) return;
            item.IsLock = true;
            item.Upgrade += next;
            foreach (var o in item.Options)
            {
                int id = o.Id, step = 0;
                if (id == 6 || id == 7) step = 15;
                else if (id == 8 || id == 9 || id == 19) step = 10;
                else if ((id >= 10 && id <= 15) || id == 17 || id == 18 || id == 20) step = 5;
                else if (id >= 21 && id <= 26) step = 150;
                else if (id == 16) step = 3;
                o.Param += (short)(step * next);
            }
        }

        // yen first, the rest from xu (Char.upgradeItem payment)
        private static bool Pay(PlayerState me, long coin)
        {
            if (coin > me.Progress.Yen + me.Progress.Xu) return false;
            if (coin > me.Progress.Yen)
            {
                me.Progress.Xu -= coin - me.Progress.Yen;
                me.Progress.Yen = 0;
            }
            else me.Progress.Yen -= coin;
            return true;
        }

        /// <summary>user.gold (PlayerLuong).</summary>
        public static int Luong(PlayerState me) { return PlayerLuong.Get(me); }

        private static void AddLuong(PlayerState me, int add) { PlayerLuong.Add(me, add); }

        private static int Int(long v) { return (int)Math.Min(int.MaxValue, Math.Max(0, v)); }

        // ---------------------------------------------------------------- upgrade (cmd 21)

        public static void Upgrade(Message m, PlayerState me, GameWorld world)
        {
            bool isGold = m.Reader.ReadBoolean();
            int equipIndex = m.Reader.ReadByte() & 0xFF;
            var bag = me.Inventory.Bag;
            if (equipIndex >= bag.Length || bag[equipIndex] == null) { EndDialog(me); return; }
            var item = bag[equipIndex];
            var t = world.FindItem(item.Id);
            if (t == null || (!IsClothe(t) && !IsAdorn(t) && !IsWeapon(t)) || t.Fashion > -1)
            {
                PlayerService.ServerDialog(me, "Trang bị không thể nâng cấp");
                return;
            }
            if (item.Upgrade >= UpMax(t)) { PlayerService.ServerDialog(me, "Trang bị đã đạt cấp tối đa."); return; }

            int insurance = 0, crystalsCount = 0;
            var crystals = new List<int>();   // bag indexes
            var seen = new bool[bag.Length];
            while (m.Reader.Remaining > 0)
            {
                int index = m.Reader.ReadSByte();
                if (index < 0 || index >= bag.Length || seen[index]) continue;
                seen[index] = true;
                var c = bag[index];
                if (c == null || c.Quantity != 1) continue;
                var ct = world.FindItem(c.Id);
                if (ct == null || !(c.Id <= 11 || ct.Type == TypeInsurance)) continue;
                if (ct.Type == TypeCrystal) crystalsCount++;
                else if (ct.Type == TypeInsurance) insurance++;
                crystals.Add(index);
            }
            if (crystals.Count > 18) { PlayerService.ServerDialog(me, "Chỉ được bỏ tối đa 18 viên đá."); return; }
            if (insurance > 1) { PlayerService.ServerDialog(me, "Chỉ được sử dụng một bảo hiểm!"); return; }
            if (crystalsCount == 0) { PlayerService.ServerDialog(me, "Hãy chọn đá nâng cấp!"); return; }

            long temp = 0;
            foreach (int i in crystals) if (world.FindItem(bag[i].Id).Type == TypeCrystal) temp += UpCrystal[bag[i].Id];
            int up = item.Upgrade, percent = 0;
            long coin = 0;
            if (IsClothe(t)) { percent = (int)(temp * 100 / UpClothe[up]); coin = CoinUpClothe[up]; }
            if (IsAdorn(t)) { percent = (int)(temp * 100 / UpAdorn[up]); coin = CoinUpAdorn[up]; }
            if (IsWeapon(t)) { percent = (int)(temp * 100 / UpWeapon[up]); coin = CoinUpWeapon[up]; }
            int maxPercent = MaxPercent[up];
            maxPercent += (int)(maxPercent * MaxPercentAdd);
            if (percent > maxPercent) percent = maxPercent;
            int gold = 0;
            if (isGold)
            {
                percent = (int)(percent * 1.5);
                gold = GoldUp[up];
            }
            if (coin > me.Progress.Yen + me.Progress.Xu) { PlayerService.ServerDialog(me, "Bạn không đủ xu và yên để nâng cấp."); return; }
            if (isGold && Luong(me) < gold) { PlayerService.ServerDialog(me, "Bạn không đủ lượng để nâng cấp."); return; }
            if (isGold) AddLuong(me, -gold);
            Pay(me, coin);

            bool isInsured = false;
            foreach (int i in crystals)
            {
                var c = bag[i];
                if (c == null) continue;
                if (!isInsured)
                {
                    if ((c.Id == 242 && up < 8) || (c.Id == 284 && up < 12) || (c.Id == 285 && up < 14) || (c.Id == 475 && up < 16)) isInsured = true;
                }
                PlayerItems.RemoveItem(me, i, c.Quantity, false);
            }
            int type, up2 = up;
            if (rnd.Next(100) < percent) { type = 1; up2++; }
            else
            {
                type = 0;
                if (!isInsured && up > 4) up2 = up >= 14 ? 14 : up >= 12 ? 12 : up / 4 * 4;
            }
            item.IsLock = true;
            Next(item, up2 - up);
            PlayerItems.Save(me);

            var r = new Message(CmdUpgrade);   // Service.upgrade
            r.Writer.WriteByte(type);
            r.Writer.WriteInt(Luong(me));      // luong
            r.Writer.WriteInt(Int(me.Progress.Xu));
            r.Writer.WriteInt(Int(me.Progress.Yen));
            r.Writer.WriteByte(item.Upgrade);
            me.Session.Send(r);
        }

        // ---------------------------------------------------------------- crystal merge (cmd 19 xu, 20 yen then xu)

        public static void UpPearl(Message m, PlayerState me, GameWorld world, bool isCoin)
        {
            var bag = me.Inventory.Bag;
            var crystals = new List<int>();
            var seen = new bool[bag.Length];
            while (m.Reader.Remaining > 0)
            {
                int index = m.Reader.ReadByte() & 0xFF;
                if (index >= bag.Length || seen[index]) continue;
                seen[index] = true;
                if (bag[index] != null && bag[index].Id <= 11)
                {
                    // Grouped crystals must be explicitly split into the units selected by this legacy UI.
                    // Never consume an entire stack for the contribution of one crystal.
                    if (bag[index].Quantity != 1)
                    { PlayerService.ServerDialog(me, "Hãy tách đá thành từng viên trước khi luyện."); return; }
                    if (bag[index].Id == 11) { PlayerService.ServerDialog(me, "Đá đã đạt cấp tối đa."); return; }
                    crystals.Add(index);
                }
            }
            if (crystals.Count > 24 || crystals.Count < 2) { PlayerService.ServerDialog(me, "Cần ít nhất 2 viên đá để luyện."); return; }
            int percent = 0, i;
            foreach (int c in crystals) percent += UpCrystal[bag[c].Id];
            for (i = UpCrystal.Length - 1; i >= 0; i--) if (percent > UpCrystal[i]) break;
            if (i >= UpCrystal.Length - 1) i = UpCrystal.Length - 2;
            percent = percent * 100 / UpCrystal[i + 1];
            if (percent <= 40) { PlayerService.ServerDialog(me, "Tỉ lệ trên 40% mới có thể luyện"); return; }
            int id = i + 1;
            long coin = CoinUpCrystal[i + 1];
            if (isCoin)
            {
                if (me.Progress.Xu < coin) { PlayerService.ServerDialog(me, "Bạn không đủ xu để luyện đá."); return; }
                me.Progress.Xu -= coin;
            }
            else if (!Pay(me, coin)) { PlayerService.ServerDialog(me, "Bạn không đủ xu và yên để luyện đá."); return; }
            foreach (int c in crystals) PlayerItems.RemoveItem(me, c, bag[c].Quantity, false);
            int indexNull = -1;
            for (int k = 0; k < bag.Length; k++) if (bag[k] == null) { indexNull = k; break; }
            if (indexNull < 0) return;   // the merged crystals just freed at least one cell
            int type = 0;
            if (rnd.Next(100) < percent) { type = 1; if (id == 11) id = 10; }
            else id = id - 1;
            var made = PlayerItems.NewItem(id, 1, true);
            made.Index = indexNull;
            bag[indexNull] = made;
            PlayerItems.Save(me);

            var r = new Message((sbyte)(isCoin ? CmdUpPearl : CmdUpPearlLock));   // Service.upPearl
            r.Writer.WriteByte(type);
            r.Writer.WriteByte(indexNull);
            r.Writer.WriteShort(made.Id);
            r.Writer.WriteBoolean(made.IsLock);
            r.Writer.WriteBoolean(false);   // hasExpire
            if (!isCoin) r.Writer.WriteInt(Int(me.Progress.Yen));
            r.Writer.WriteInt(Int(me.Progress.Xu));
            me.Session.Send(r);
        }

        // ---------------------------------------------------------------- split (cmd 22)

        public static void Split(Message m, PlayerState me, GameWorld world)
        {
            int index = m.Reader.ReadByte() & 0xFF;
            var bag = me.Inventory.Bag;
            if (index >= bag.Length || bag[index] == null) { EndDialog(me); return; }
            var item = bag[index];
            var t = world.FindItem(item.Id);
            if (t == null || !(IsWeapon(t) || IsClothe(t) || IsAdorn(t))) { EndDialog(me); return; }
            if (item.Upgrade <= 0) { PlayerService.ServerDialog(me, "Không thể tách trang bị chưa nâng cấp"); return; }
            int num = 0;
            int[] table = IsWeapon(t) ? UpWeapon : t.Type % 2 == 0 ? UpClothe : UpAdorn;
            for (int k = 0; k < item.Upgrade; k++) num += table[k];
            num /= 2;
            var made = new List<int>();
            for (int n = UpCrystal.Length - 1; n >= 0; n--)
                if (num >= UpCrystal[n]) { made.Add(n); num -= UpCrystal[n]; n++; }
            if (made.Count > PlayerItems.SlotNull(me)) { PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống"); return; }
            var r = new Message(CmdSplit);   // Service.splitItem
            r.Writer.WriteByte(made.Count);
            int next = 0;
            for (int k = 0; k < bag.Length && next < made.Count; k++)
            {
                if (bag[k] != null) continue;
                var c = PlayerItems.NewItem(made[next++], 1, true);
                c.Index = k;
                bag[k] = c;
                r.Writer.WriteByte(k);
                r.Writer.WriteShort(c.Id);
            }
            Next(item, -item.Upgrade);
            PlayerItems.Save(me);
            me.Session.Send(r);
        }

        // ---------------------------------------------------------------- convert (NOT_MAP -88)

        public const sbyte CmdConvertUpgrade = -88;

        /// <summary>Char.convertUpgrade: moves the upgrade of item 1 onto un-upgraded item 2 of the same kind and an equal or
        /// higher level, using Hoán chuyển 269 (up to +10), 270 (up to +13) or 271 (any). Kenshinto "Chuyển hóa" (UI 33).</summary>
        public static void ConvertUpgrade(Message m, PlayerState me, GameWorld world)
        {
            int index1 = m.Reader.ReadByte() & 0xFF, index2 = m.Reader.ReadByte() & 0xFF, index3 = m.Reader.ReadByte() & 0xFF;
            var bag = me.Inventory.Bag;
            if (Math.Max(Math.Max(index1, index2), index3) >= bag.Length) return;
            var item1 = bag[index1]; var item2 = bag[index2]; var item3 = bag[index3];
            if (item1 == null || item2 == null || item3 == null) return;
            var t1 = world.FindItem(item1.Id); var t2 = world.FindItem(item2.Id); var t3 = world.FindItem(item3.Id);
            if (t1 == null || t2 == null || t3 == null) return;
            if (IsWeapon(t1) != IsWeapon(t2) || IsAdorn(t1) != IsAdorn(t2) || IsClothe(t1) != IsClothe(t2) || t1.Fashion != t2.Fashion)
            { PlayerService.ServerDialog(me, "Trang bị không cùng loại."); return; }
            if (t2.Fashion > 0) { PlayerService.ServerDialog(me, "Không thể chuyển hóa sang trang bị này."); return; }
            if ((!IsClothe(t1) && !IsAdorn(t1) && !IsWeapon(t1)) || t1.Fashion > -1) { PlayerService.ServerDialog(me, "Không thể chuyển hóa sang trang bị này."); return; }
            if ((!IsClothe(t2) && !IsAdorn(t2) && !IsWeapon(t2)) || t2.Fashion > -1) { PlayerService.ServerDialog(me, "Không thể chuyển hóa sang trang bị này."); return; }
            if (item1.Expire != -1) { PlayerService.ServerDialog(me, "Trang bị này không thể chuyển hoá."); return; }
            if (item1.Upgrade == 0) { PlayerService.ServerDialog(me, "Trang bị chưa nâng câp."); return; }
            if (item2.Upgrade > 0) { PlayerService.ServerDialog(me, "Trang bị cần chuyển hoá sang không được nâng cấp."); return; }
            if (t1.Level > t2.Level) { PlayerService.ServerDialog(me, "Trang bị chuyển hoá sang phải có cấp độ ngang bằng hoặc lớn hơn."); return; }
            if (t3.Type != 27) { PlayerService.ServerDialog(me, t3.Name + " không phải vật phẩm chuyển hoá"); return; }
            if ((item3.Id == 270 && item1.Upgrade > 13) || (item3.Id == 269 && item1.Upgrade > 10))
            { PlayerService.ServerDialog(me, t3.Name + " không phù hợp để chuyển hoá trang bị này."); return; }
            int upgrade = item1.Upgrade;
            item2.Upgrade = 0;
            Next(item2, upgrade);
            item2.IsLock = true;
            Next(item1, -upgrade);
            item1.IsLock = true;
            var r = new Message(Cmd.NOT_MAP);   // Service.convertUpgrade
            r.Writer.WriteSByte(CmdConvertUpgrade);
            r.Writer.WriteByte(index1);
            r.Writer.WriteByte(item1.Upgrade);
            r.Writer.WriteByte(index2);
            r.Writer.WriteByte(item2.Upgrade);
            me.Session.Send(r);
            PlayerItems.RemoveItem(me, index3, item3.Quantity, true);
        }

        // closes the client's "please wait" box when a request is ignored
        private static void EndDialog(PlayerState me) { PlayerService.ServerMessage(me, ""); }
    }
}
