using System;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class

    // Luong (user.gold) and what it pays for up to level 39:
    //   - storage on the character (offline: per character; Java keeps it on the account) and Service.addGold (ME_UP_GOLD);
    //   - simulated top-up at Okanechan (offline deviation: Java credits luong from the web payment site, not in game);
    //   - Char.npcOkanechan "Đổi lượng": luong to xu (1 : 3000) or yen (1 : 10000) through a text box (OPEN_TEXT_BOX_ID 92);
    //   - Char.expandBag (Túi vải 215/229/283/801/1094/1158), the "tẩy" licences 240/241 and the teachers' "Tẩy điểm"
    //     (Char.tayTiemNang / tayKyNang), Nấm linh chi 248 (x2 exp), Hoàn cốt chi chú 254/255, Hoàn lương chi thảo 257.
    // Shops, careful upgrade and revive-in-place read Get/Add from their own files.
    public static class PlayerLuong
    {
        public const sbyte ME_UP_GOLD = -71, UPDATE_BAG_COUNT = -91, OPEN_TEXT_BOX_ID = 92;
        public const int Okanechan = 24;
        public const int MaxLuong = 2000000000;

        // ---- tunables
        /// <summary>Simulated top-up cards (VND).</summary>
        public static int[] TopUpCards = { 10000, 20000, 50000, 100000, 200000, 500000 };
        /// <summary>Luong credited per 1 VND of a simulated card.</summary>
        public static int TopUpLuongPerVnd = 1;
        public static int XuPerLuong = 3000, YenPerLuong = 10000;            // Okanechan rates
        public static int ExchangeMin = 1000, ExchangeXuMax = 200000;        // luong per exchange
        public static long YenMax = 2000000000;
        public static long XuMaxStep = 100000000, XuMaxCap = 3000000000L;   // Kamakura "Mở giới hạn xu"
        public static int XuMaxFeePerStep = 200;
        public static readonly int[] BagItems = { 215, 229, 283, 801, 1094, 1158 };   // Char.expandBag order
        public static readonly int[] BagCells = { 6, 6, 12, 24, 24, 18 };
        public static int NamLinhChiMs = 5 * 60 * 60 * 1000;                 // effect 22 "x2 kinh nghiệm"

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        // ---------------------------------------------------------------- balance

        public static int Get(PlayerState me) { return me.Progress != null ? me.Progress.Luong : 0; }

        /// <summary>User.addGold: clamp to 0..2e9 and show the change (Service.addGold).</summary>
        public static void Add(PlayerState me, int add)
        {
            long sum = (long)me.Progress.Luong + add;
            int pre = me.Progress.Luong;
            me.Progress.Luong = (int)Math.Max(0, Math.Min(MaxLuong, sum));
            var m = PlayerService.Sub(ME_UP_GOLD);
            m.Writer.WriteInt(me.Progress.Luong - pre);
            me.Session.Send(m);
            PlayerItems.Save(me);
        }

        // ---------------------------------------------------------------- text box (Service.showInputDialog / Char.input)

        public static void ShowInput(PlayerState me, string title, Action<string> onText)
        {
            me.InputHandler = onText;
            var m = new Message(OPEN_TEXT_BOX_ID);
            m.Writer.WriteUTF(title);
            m.Writer.WriteShort(1);
            me.Session.Send(m);
        }

        /// <summary>cmd 92 from the client: the id of the text box, then the text.</summary>
        public static void Input(Message m, PlayerState me)
        {
            m.Reader.ReadShort();
            string text = m.Reader.ReadUTF();
            var h = me.InputHandler;
            me.InputHandler = null;
            if (h != null) h(text);
        }

        // ---------------------------------------------------------------- Okanechan

        public static void NpcOkanechan(PlayerState me, Action<string, Action> add, Action<object[]> subMenu)
        {
            add("Nạp lượng", () =>
            {
                var items = new object[TopUpCards.Length * 2];
                for (int i = 0; i < TopUpCards.Length; i++)
                {
                    int vnd = TopUpCards[i];
                    items[i * 2] = "Thẻ " + Money(vnd) + "đ";
                    items[i * 2 + 1] = (Action)(() => TopUp(me, vnd));
                }
                subMenu(items);
            });
            add("Đổi lượng", () => subMenu(new object[]
            {
                "Đổi xu", (Action)(() => ShowInput(me, "Số lượng đổi(1 Lượng = " + XuPerLuong + " xu)", s => ToXu(me, s))),
                "Đổi yên", (Action)(() => ShowInput(me, "Số lượng đổi(1 Lượng = " + YenPerLuong + " yên)", s => ToYen(me, s)))
            }));
        }

        private static string Money(long v) { return v.ToString("N0").Replace(',', '.'); }

        // offline deviation: a card is credited at once, no payment involved
        private static void TopUp(PlayerState me, int vnd)
        {
            int luong = (int)Math.Min(MaxLuong, (long)vnd * TopUpLuongPerVnd);
            Add(me, luong);
            PlayerService.NpcChat(me, Okanechan, "Nạp thành công thẻ " + Money(vnd) + "đ, con nhận được " + Money(luong) + " lượng. (Bản offline: nạp mô phỏng)");
        }

        private static int ParseAmount(PlayerState me, string s)
        {
            int v;
            if (string.IsNullOrEmpty(s) || !int.TryParse(s.Trim(), out v) || v < 0)
            {
                PlayerService.NpcChat(me, Okanechan, "Số lượng nhập vào không hợp lệ");
                return -1;
            }
            return v;
        }

        private static void ToXu(PlayerState me, string s)
        {
            int n = ParseAmount(me, s);
            if (n < 0) return;
            if (n > ExchangeXuMax) { PlayerService.NpcChat(me, Okanechan, "Chỉ có thể đổi tối đa " + ExchangeXuMax / 1000 + "k lượng 1 lần"); return; }
            if (n < ExchangeMin) { PlayerService.NpcChat(me, Okanechan, "Chỉ phải đổi ít nhất " + ExchangeMin / 1000 + "k lượng 1 lần"); return; }
            if (Get(me) < n) { PlayerService.NpcChat(me, Okanechan, "Không đủ lượng để đổi"); return; }
            long xu = (long)XuPerLuong * n;
            if (me.Progress.Xu + xu > me.Progress.XuMax) { PlayerService.NpcChat(me, Okanechan, "Số lượng sau khi đổi vượt quá sức chứa hành trang không thể đổi"); return; }
            Add(me, -n);
            PlayerItems.AddXu(me, xu);
            PlayerService.NpcChat(me, Okanechan, "Bạn đã quy đổi thành công " + Money(xu) + " xu");
        }

        private static void ToYen(PlayerState me, string s)
        {
            int n = ParseAmount(me, s);
            if (n < 0) return;
            long yen = (long)YenPerLuong * n;
            if (me.Progress.Yen + yen > YenMax) { PlayerService.NpcChat(me, Okanechan, "Chỉ có thể đổi không được quá 2 tỷ yên"); return; }
            if (n < ExchangeMin) { PlayerService.NpcChat(me, Okanechan, "Chỉ phải đổi ít nhất " + ExchangeMin / 1000 + "k lượng 1 lần"); return; }
            if (Get(me) < n) { PlayerService.NpcChat(me, Okanechan, "Không đủ lượng để đổi"); return; }
            Add(me, -n);
            PlayerItems.AddYen(me, yen);
            PlayerService.NpcChat(me, Okanechan, "Bạn đã quy đổi thành công " + Money(yen) + " yên");
        }

        // ---------------------------------------------------------------- Kamakura: "Mở giới hạn xu"

        /// <summary>Char.npcKamakura "Mở giới hạn xu" / "Xu hành trang": +100 million xu of bag limit, up to 3 billion.
        /// Fee = (coinMax - 1.4 billion) / 100 million * 200 luong (Java casts coinMax to int, which overflows above
        /// 2.1 billion; the fee is computed in long here).</summary>
        public static void RaiseXuMax(PlayerState me, Action<object[]> subMenu)
        {
            const int Kamakura = 5;
            subMenu(new object[]
            {
                "Xu hành trang", (Action)(() =>
                {
                    long fee = (me.Progress.XuMax - 1400000000L) / XuMaxStep * XuMaxFeePerStep;
                    subMenu(new object[]
                    {
                        "Mở 100tr (" + Money(fee) + " lượng)", (Action)(() =>
                        {
                            if (me.Progress.XuMax >= XuMaxCap) { PlayerService.NpcChat(me, Kamakura, "Đã đạt giới hạn cao nhất"); return; }
                            if (Get(me) < fee) { PlayerService.NpcChat(me, Kamakura, "Không đủ lượng"); return; }
                            Add(me, (int)-fee);
                            me.Progress.XuMax += XuMaxStep;
                            PlayerItems.Save(me);
                            PlayerService.NpcChat(me, Kamakura, "Giới hạn xu của bạn đã được tăng lên " + Money(me.Progress.XuMax) + " xu");
                        })
                    });
                })
            });
        }

        // ---------------------------------------------------------------- items bought with luong

        /// <summary>Char.useItemTypeOrder for the luong items; false when the item is not one of them.</summary>
        public static bool UseItem(PlayerState me, GameWorld world, int indexUI, PlayerItem item)
        {
            var p = me.Progress;
            switch (item.Id)
            {
                case 240:   // Giấy phép tẩy tiềm năng
                    p.TayTiemNang++;
                    PlayerItems.RemoveItem(me, indexUI, item.Quantity, true);
                    PlayerService.ServerMessage(me, "Số lần tẩy điểm tiềm năng của bạn là " + p.TayTiemNang);
                    return true;
                case 241:   // Giấy phép tẩy kỹ năng (Java prints tayTiemNang here, a typo: the right counter is shown)
                    p.TayKyNang++;
                    PlayerItems.RemoveItem(me, indexUI, item.Quantity, true);
                    PlayerService.ServerMessage(me, "Số lần tẩy điểm kỹ năng của bạn là " + p.TayKyNang);
                    return true;
                case 248:   // Nấm linh chi: effect 22 (x2 exp, type 18) for 5 hours, added to the time left
                    {
                        long left = 0, now = Now();
                        foreach (var e in me.Effects) if (e.TemplateId == 22 && !e.IsExpired(now)) left = e.EndAt - now;
                        PlayerEffects.SetEffect(me, world, 22, left + NamLinhChiMs, 2);
                        PlayerItems.RemoveItem(me, indexUI, item.Quantity, true);
                        return true;
                    }
                case 254:
                case 255:
                case 256:   // Hoàn cốt chi chú: clear the lost experience
                    if (item.Id == 254 && me.Level >= 30) { PlayerService.ServerDialog(me, "Vật phẩm chỉ có thể xóa kinh nghiệm âm dưới cấp 30"); return true; }
                    if (item.Id == 255 && me.Level >= 60) { PlayerService.ServerDialog(me, "Vật phẩm chỉ có thể xóa kinh nghiệm âm dưới cấp 60"); return true; }
                    PlayerCombat.AddExp(me, world, me.ExpDown, false);
                    PlayerItems.RemoveItem(me, indexUI, 1, true);
                    PlayerService.ServerMessage(me, "Đã xóa kinh nghiệm âm");
                    return true;
                case 257:   // Hoàn lương chi thảo: no PK offline, so the hiếu chiến points are always 0
                    PlayerService.ServerMessage(me, "Điểm hiếu chiến của bạn đã bằng 0.");
                    return true;
            }
            if (Array.IndexOf(BagItems, item.Id) >= 0) { ExpandBag(me, item); return true; }
            return false;
        }

        // Char.expandBag: the bag grows in a fixed order of bags; offline the number used comes from the bag size
        private static void ExpandBag(PlayerState me, PlayerItem item)
        {
            int used = 0, cells = PlayerInventory.DefaultBagCells;
            while (used < BagItems.Length && cells + BagCells[used] <= me.Inventory.Bag.Length) { cells += BagCells[used]; used++; }
            if (used >= BagItems.Length) { PlayerService.ServerMessage(me, "Bạn đã sử dụng tất cả các loại túi vải."); return; }
            if (BagItems[used] != item.Id)
            {
                var t = me.Session.Server.World.FindItem(BagItems[used]);
                PlayerService.ServerMessage(me, "Hãy dùng sử dụng " + (t != null ? t.Name : ""));
                return;
            }
            var bag = new PlayerItem[me.Inventory.Bag.Length + BagCells[used]];
            Array.Copy(me.Inventory.Bag, bag, me.Inventory.Bag.Length);
            me.Inventory.Bag = bag;
            int index = item.Index;
            var m = PlayerService.Sub(UPDATE_BAG_COUNT);
            m.Writer.WriteByte((byte)bag.Length);
            m.Writer.WriteByte((byte)index);
            me.Session.Send(m);
            PlayerItems.RemoveItem(me, index, item.Quantity, false);
        }

        // ---------------------------------------------------------------- teachers: "Tẩy điểm"

        /// <summary>The "Tẩy điểm" entry of Toyotomi (9), Ookamesama (10) and Kazeto (11).</summary>
        public static void TeacherMenu(PlayerState me, GameWorld world, int npc, Action<string, Action> add, Action<object[]> subMenu)
        {
            add("Tẩy điểm", () => subMenu(new object[]
            {
                "Tiềm năng", (Action)(() => TayTiemNang(me, world, npc)),
                "Kỹ năng", (Action)(() => TayKyNang(me, world, npc))
            }));
        }

        // Char.getSys: school of the class
        private static int Sys(PlayerState me) { int c = me.Record.ClassId; return c == 0 ? 0 : (c + 1) / 2; }

        private static bool OwnSchool(PlayerState me, int npc)
        {
            int sys = Sys(me);
            return (npc == 9 && sys == 1) || (npc == 10 && sys == 2) || (npc == 11 && sys == 3);
        }

        // Char.tayTiemNang
        private static void TayTiemNang(PlayerState me, GameWorld world, int npc)
        {
            var p = me.Progress;
            if (!OwnSchool(me, npc)) { PlayerService.NpcChat(me, npc, "Con không phải là học sinh của trường này, không thể tẩy điểm ở đây."); return; }
            if (p.TayTiemNang <= 0) { PlayerService.NpcChat(me, npc, "Số lần tẩy điểm tiềm năng của con đã hết."); return; }
            int lv = me.Level;
            int point = lv * 10;
            foreach (int from in new[] { 70, 80, 90, 100, 130, 145 }) if (lv >= from) point += (lv - from) * 10;
            point += 10 * (p.LimitTiemNangSo + p.LimitBangHoa);
            me.PotentialPoint = point;
            int c = me.Record.ClassId;
            me.Potential[1] = 5;
            me.Potential[2] = 5;
            if (c == 1 || c == 3 || c == 5) { me.Potential[0] = 10; me.Potential[3] = 5; }
            else { me.Potential[0] = 5; me.Potential[3] = 10; }
            p.TayTiemNang--;
            PlayerCombat.SetAbility(me, world);
            PlayerService.UpdatePotential(me);
            PlayerItems.Save(me);
            PlayerService.NpcChat(me, npc, p.TayTiemNang > 0
                ? "Ta đã giúp con tẩy điểm tiềm năng. Con vẫn có thể tẩy thêm được " + p.TayTiemNang + " lần tẩy tiềm năng nữa."
                : "Ta đã giúp con tẩy điểm tiềm năng. Đây là lần cuối con được tẩy tiềm năng, hãy sử dụng cho thật tốt điểm tiềm năng nhé.");
        }

        // Char.tayKyNang: every learned skill back to level 1, points refunded (the clone skills are not ported)
        private static void TayKyNang(PlayerState me, GameWorld world, int npc)
        {
            var p = me.Progress;
            if (!OwnSchool(me, npc)) { PlayerService.NpcChat(me, npc, "Con không phải là học sinh của trường này, không thể tẩy điểm ở đây."); return; }
            if (p.TayKyNang <= 0) { PlayerService.NpcChat(me, npc, "Số lần tẩy điểm kỹ năng của con đã hết."); return; }
            me.SkillPoint = me.Level - 9 + p.LimitKyNangSo + p.LimitPhongLoi;
            foreach (var s in p.Skills)
                if (world.FindSkill(s[0], 1) != null) s[1] = 1;   // GameData.getSkill(classId, templateId, 1)
            p.TayKyNang--;
            PlayerCombat.SetAbility(me, world);
            PlayerService.LoadSkill(me, world);
            PlayerService.SkillBars(me);   // Char.tayKyNang: sendSkillShortcut
            PlayerItems.Save(me);
            PlayerService.NpcChat(me, npc, p.TayKyNang > 0
                ? "Ta đã giúp con tẩy điểm kỹ năng. Con vẫn có thể tẩy thêm được " + p.TayKyNang + " lần tẩy kỹ năng nữa."
                : "Ta đã giúp con tẩy điểm kỹ năng. Đây là lần cuối con được tẩy kỹ năng, hãy sử dụng cho thật tốt điểm kỹ năng nhé.");
        }
    }
}
