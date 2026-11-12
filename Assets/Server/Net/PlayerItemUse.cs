using System;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class

    // The TYPE_ORDER (27) items a level 1-39 character can get offline (xu/yen shops, drops, Rikudou rewards).
    // Ports of the matching branches of Char.useItemTypeOrder and Char.useItemChangeMap (cmd 12).
    //
    // Luong items (bag, "tẩy" licences, Nấm linh chi...) are in PlayerLuong. Not ported (systems that do not exist):
    //   mount fuel 444/469 (no mounts), dragon balls 222-228 (not dropped), event / level 40+ items.
    public static class PlayerItemUse
    {
        public const int ItemBachBien = 34, ItemKhaDi = 35, ItemVoHanBachBien = 36, ItemVoHanKhaDi = 37, ItemPhucNang = 38;
        public const int ItemManhGiayVun = 251, ItemSachKyNangSo = 252, ItemSachTiemNangSo = 253, ItemSachPhongLoi = 308, ItemSachBangHoa = 309;

        // tunables (Java constants)
        public static int PhucNangYenMin = 1000, PhucNangYenMax = 20000;   // NinjaUtils.nextInt(1000, 20000)
        public static int GiayVunPerBook = 300;
        public static int LimitKyNangSo = 3, LimitTiemNangSo = 8, LimitPhongLoi = 10, LimitBangHoa = 10;

        /// <summary>Destinations of the Khả di lệnh menu (Char.useItemChangeMap): 3 schools, then 7 villages.</summary>
        public static readonly int[] KhaDiMaps = { 1, 27, 72, 10, 17, 22, 32, 38, 43, 48 };

        private static readonly Random rnd = new Random();

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        /// <summary>Char.useItemTypeOrder for the ported ids; false when the item is not one of them.</summary>
        public static bool Use(PlayerState me, GameWorld world, int indexUI, PlayerItem item)
        {
            var p = me.Progress;
            switch (item.Id)
            {
                case ItemBachBien:
                case ItemVoHanBachBien:
                    // back to the saved village (Kamakura's "Lưu tọa độ")
                    if (me.Zone != null && me.Zone.IsInstance) { PlayerService.ServerMessage(me, "Không thể sử dụng tính năng này tại đây."); return true; }
                    var xy = PlayerTasks.VillageXY(p.SaveMap);
                    me.SetXY(xy[0], xy[1]);
                    if (item.Id == ItemBachBien) PlayerItems.RemoveItem(me, indexUI, item.Quantity, true);
                    me.Actions.ChangeMap(p.SaveMap);
                    return true;

                case ItemCanCauVang:
                    StartFishing(me, world, indexUI);
                    return true;

                case ItemThienBienLenh:
                    ThienBienLenh(me);
                    return true;

                case ItemPhucNang:
                    int yen = rnd.Next(PhucNangYenMin, PhucNangYenMax);
                    PlayerItems.AddYen(me, yen);
                    PlayerService.ServerMessage(me, "Bạn nhận được " + yen.ToString("N0").Replace(',', '.') + " yên");
                    PlayerItems.RemoveItem(me, indexUI, 1, true);
                    return true;

                case ItemManhGiayVun:
                    if (item.Quantity < GiayVunPerBook) { PlayerService.ServerMessage(me, "Không đủ giấy vụn."); return true; }
                    me.Menus.Clear();
                    me.Menus.Add(new MenuEntry(MenuEntry.Execute, "Sách tiềm năng sơ", () => PaperToBook(me, world, ItemSachTiemNangSo)));
                    me.Menus.Add(new MenuEntry(MenuEntry.Execute, "Sách kỹ năng sơ", () => PaperToBook(me, world, ItemSachKyNangSo)));
                    PlayerService.OpenUiMenu(me);
                    return true;

                case ItemSachKyNangSo:
                    if (p.LimitKyNangSo >= LimitKyNangSo) { PlayerService.ServerDialog(me, "Đã hết số lần sử dụng."); return true; }
                    p.LimitKyNangSo++;
                    AddSkillPoint(me, world, indexUI, item);
                    return true;

                case ItemSachPhongLoi:
                    if (p.LimitPhongLoi >= LimitPhongLoi) { PlayerService.ServerDialog(me, "Đã hết số lần sử dụng."); return true; }
                    p.LimitPhongLoi++;
                    AddSkillPoint(me, world, indexUI, item);
                    return true;

                case ItemSachTiemNangSo:
                    if (p.LimitTiemNangSo >= LimitTiemNangSo) { PlayerService.ServerDialog(me, "Đã hết số lần sử dụng."); return true; }
                    p.LimitTiemNangSo++;
                    AddPotentialPoint(me, indexUI, item);
                    return true;

                case ItemSachBangHoa:
                    if (p.LimitBangHoa >= LimitBangHoa) { PlayerService.ServerDialog(me, "Đã hết số lần sử dụng."); return true; }
                    p.LimitBangHoa++;
                    AddPotentialPoint(me, indexUI, item);
                    return true;
            }
            return false;
        }

        private static void AddSkillPoint(PlayerState me, GameWorld world, int indexUI, PlayerItem item)
        {
            me.SkillPoint++;
            PlayerService.LoadSkill(me, world);
            PlayerItems.RemoveItem(me, indexUI, item.Quantity, true);
            PlayerService.ServerMessage(me, "Bạn nhận được 1 điểm kỹ năng.");
        }

        private static void AddPotentialPoint(PlayerState me, int indexUI, PlayerItem item)
        {
            me.PotentialPoint += 10;
            PlayerService.UpdatePotential(me);
            PlayerItems.RemoveItem(me, indexUI, item.Quantity, true);
            PlayerService.ServerMessage(me, "Bạn nhận được 10 điểm tiềm năng.");
        }

        // MANH_GIAY_VUN menu entries: 300 pieces become a beginner potential / skill book
        private static void PaperToBook(PlayerState me, GameWorld world, int book)
        {
            if (PlayerItems.SlotNull(me) == 0) { PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống"); return; }
            int index = PlayerItems.IndexInBag(me, ItemManhGiayVun);
            if (index == -1 || me.Inventory.Bag[index].Quantity < GiayVunPerBook) return;
            PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(book));
            PlayerItems.RemoveItem(me, index, GiayVunPerBook, true);
        }

        // ---------------------------------------------------------------- Thiên biến lệnh (572): auto-play

        public const int ItemThienBienLenh = 572;
        public const sbyte CmdAutoAttackMove = 119;
        public static int AutoMoveIntervalMs = 2000;

        // Service.turnOnAuto / turnOffAuto
        private static void SendAuto(PlayerState me, bool on)
        {
            var m = new Message(CmdAutoAttackMove);
            if (on)
            {
                m.Writer.WriteSByte(-1);
                m.Writer.WriteInt(me.AutoRange <= 0 ? 1000 : me.AutoRange);
            }
            else m.Writer.WriteSByte(1);
            me.Session.Send(m);
        }

        private static void Menu(PlayerState me, params object[] nameThenAction)
        {
            me.Menus.Clear();
            for (int i = 0; i + 1 < nameThenAction.Length; i += 2)
                me.Menus.Add(new MenuEntry(MenuEntry.Execute, (string)nameThenAction[i], (Action)nameThenAction[i + 1]));
            PlayerService.OpenUiMenu(me);
        }

        // Char.setRange: auto-play re-sends its range when it is on
        private static void SetRange(PlayerState me, int range)
        {
            me.AutoRange = range;
            me.AutoOriginX = me.X; me.AutoOriginY = me.Y;
            if (me.IsAutoPlay) SendAuto(me, true);
        }

        // Offline completion of AUTO_ATTACK_MOVE / MapService movement: never trust the client
        // teleport alone. An active, unexpired Thien bien lenh and an in-range live mob are required.
        public static void MoveForAuto(Message message, PlayerState me, GameWorld world)
        {
            int id = message.Reader.ReadByte();
            long now = Now();
            if (!me.IsAutoPlay || me.IsDead || me.Zone == null) return;
            if (!HasAutoItem(me, now)) { me.IsAutoPlay = false; SendAuto(me, false); return; }
            if (now - me.LastAutoMove < AutoMoveIntervalMs) { me.Actions.ResetPoint(); return; }
            foreach (var mob in me.Zone.Mobs)
            {
                if (mob.Id != id || mob.IsDead || mob.Hp <= 0 || mob.LevelBoss == 3) continue;
                if (me.AutoRange > 0 && me.AutoRange <= 360 &&
                    (Math.Abs(mob.X - me.AutoOriginX) > me.AutoRange || Math.Abs(mob.Y - me.AutoOriginY) > me.AutoRange))
                { me.Actions.ResetPoint(); return; }
                me.LastAutoMove = now;
                me.SetXY(mob.X, mob.Y);
                me.PreX = me.X; me.PreY = me.Y;
                me.Actions.ResetPoint();
                return;
            }
            me.Actions.ResetPoint();
        }

        private static bool HasAutoItem(PlayerState me, long now)
        {
            foreach (var item in me.Inventory.Bag)
                if (item != null && item.Id == ItemThienBienLenh && item.Quantity > 0 && (item.Expire <= 0 || item.Expire > now)) return true;
            return false;
        }

        /// <summary>Char.useItemTypeOrder THIEN_BIEN_LENH: switch auto-play and set its range; enabled item auto-picks nearby loot.</summary>
        private static void ThienBienLenh(PlayerState me)
        {
            Menu(me,
                me.IsAutoPlay ? "Tắt" : "Bật", (Action)(() =>
                {
                    me.IsAutoPlay = !me.IsAutoPlay;
                    me.AutoOriginX = me.X; me.AutoOriginY = me.Y;
                    me.LastAutoMove = 0;
                    if (me.IsAutoPlay && !HasAutoItem(me, Now())) me.IsAutoPlay = false;
                    SendAuto(me, me.IsAutoPlay);
                }),
                "Cài đặt", (Action)(() => Menu(me,
                    "Phạm vi: " + (me.AutoRange <= 0 || me.AutoRange > 360 ? "Toàn map" : me.AutoRange.ToString()), (Action)(() => Menu(me,
                        "Toàn map", (Action)(() => SetRange(me, -1)),
                        "Phạm vi 240", (Action)(() => SetRange(me, 240)),
                        "Phạm vi 360", (Action)(() => SetRange(me, 360)),
                        "Tùy chỉnh", (Action)(() => PlayerLuong.ShowInput(me, "Phạm vi", s =>
                        {
                            int r;
                            if (!int.TryParse((s ?? "").Trim(), out r)) { PlayerService.ServerDialog(me, "Số không hợp lệ"); return; }
                            SetRange(me, r > 360 ? -1 : r);
                        })))),
                    "Tự nhặt khi bật lệnh", (Action)(() => PlayerService.ServerMessage(me, "Thiên biến lệnh tự nhặt vật phẩm gần nhân vật khi đang bật.")))));
        }

        // ---------------------------------------------------------------- Cần câu vàng (548): fishing

        public const int ItemCanCauVang = 548;
        public static int FishWaitMs = 3000;
        public static readonly int[] FishItems = { 6, 7, 8, 9, 10, -2 };   // stones 7-11, or the line breaks (-2)
        public static readonly int[] FishRates = { 50, 50, 40, 30, 20, 20 }; // NinjaUtils.randomWithRate(percent, 100)

        // Char.useItemTypeOrder CAN_CAU_VANG: stand in water, the rod is used up, the catch comes 3 s later
        private static void StartFishing(PlayerState me, GameWorld world, int indexUI)
        {
            if (me.FishUntil > 0) return;
            var grid = me.Zone != null ? world.GetGrid(me.Zone.Tile) : null;
            if (grid == null || !grid.TileTypeAt(me.X, me.Y - 12, TileGrid.T_WATERFLOW)) { PlayerService.ServerDialog(me, "Hãy đứng chỗ có nước để câu"); return; }
            if (PlayerItems.SlotNull(me) == 0) { PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống"); return; }
            PlayerItems.RemoveItem(me, indexUI, 1, true);
            PlayerService.ServerMessage(me, "Đang thả câu....");
            me.FishUntil = Now() + FishWaitMs;
        }

        private static void FinishFishing(PlayerState me)
        {
            me.FishUntil = 0;
            int next = rnd.Next(100), index = 0;
            for (; index < FishRates.Length; index++) { if (next < FishRates[index]) break; next -= FishRates[index]; }
            if (index >= FishItems.Length) index = FishItems.Length - 1;
            int id = FishItems[index];
            // offline: Java says these lines in a chat bubble; shown as a server message here
            if (id == -2) { PlayerService.ServerMessage(me, "Ôi? Dây câu đứt mất rồi!"); return; }
            PlayerItems.AddItemToBag(me, me.Session.Server.World, PlayerItems.NewItem(id));
        }

        /// <summary>Char.updateEveryFiveSecond: auto-play stops when the Thiên biến lệnh is gone or expired.</summary>
        public static void AutoTick(PlayerState me, long now)
        {
            if (me.FishUntil > 0 && now >= me.FishUntil) FinishFishing(me);
            if (!me.IsAutoPlay || now - me.LastAutoCheck < 5000) return;
            me.LastAutoCheck = now;
            if (!HasAutoItem(me, now))
            {
                me.IsAutoPlay = false;
                SendAuto(me, false);
            }
        }

        /// <summary>Char.useItemChangeMap (cmd 12): Khả di lệnh / Vô hạn khả di lệnh to a school or village.</summary>
        public static void ChangeMap(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead) { PlayerService.ServerDialog(me, "Không thể sử dụng vật phẩm ở trạng thái này."); return; }
            int mapNow = me.Zone != null ? me.Zone.Tile.Id : -1;
            if (mapNow == 74 || (me.Zone != null && me.Zone.IsInstance) || mapNow == 110 || mapNow == 117 || mapNow == 161)
            {
                PlayerService.ServerMessage(me, "Không thể sử dụng tính năng này tại đây.");
                return;
            }
            int indexUI = m.Reader.ReadSByte();
            int indexMenu = m.Reader.ReadSByte();
            var bag = me.Inventory.Bag;
            if (indexUI < 0 || indexUI >= bag.Length || indexMenu < 0 || indexMenu >= KhaDiMaps.Length) return;
            var item = bag[indexUI];
            if (item == null || (item.Id != ItemKhaDi && item.Id != ItemVoHanKhaDi)) return;
            int map = KhaDiMaps[indexMenu];
            if (!MapUnlock.CanEnter(me.Record.TaskId, map))
            {
                me.Actions.ResetPoint();
                PlayerService.ServerDialog(me, "Bạn chưa thể đến khu vực này. Hãy hoàn thành nhiệm vụ trước.");
                return;
            }
            var xy = PlayerTasks.VillageXY(map);
            me.SetXY(xy[0], xy[1]);
            me.Actions.ChangeMap(map);
            if (item.Id == ItemKhaDi || (item.Expire > 0 && item.Expire < Now())) PlayerItems.RemoveItem(me, indexUI, 1, true);
        }
    }
}
