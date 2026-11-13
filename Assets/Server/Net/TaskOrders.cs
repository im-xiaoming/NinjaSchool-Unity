using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class
    using Mob = NsoServer.World.Mob;
    using TileMap = NsoServer.World.TileMap;

    // Rikudou's repeatable quests (com.nsoz.task.TaskOrder / TaskFactory, Char.orderTaskDay / cancelTaskDay /
    // finishTaskDay / workTaskDay / orderTaskBoss / cancelTaskBoss / finishTaskBoss, Mob.dead):
    //   - "NV mỗi ngày" (TASK_DAY 0): kill 20-39 of a random monster within 5 levels of the task-map level closest
    //     to the player's level; 20 per day; reward 2 x item 251, 20000 yen, 1 uy danh point.
    //   - "NV Truy bắt Tà Thú" (TASK_BOSS 1): kill the beast (levelBoss 3) whose level is closest to the player's;
    //     2 per day (+1 per "Tà thú lệnh" item 268, at most 2 a day); reward 5 x item 251.
    // Messages: GET_TASK_ORDER 96, GET_TASK_UPDATE 97, CLEAR_TASK_ORDER 98 (the client lists them under Nhiệm vụ).
    // No party offline, so the group parts of orderTaskBoss / Mob.dead do not apply.
    public static class TaskOrders
    {
        public const sbyte CmdGetTaskOrder = 96, CmdGetTaskUpdate = 97, CmdClearTaskOrder = 98;
        public const int TaskDay = 0, TaskBoss = 1;
        public const int Rikudou = 25;
        public const int ItemTaThuLenh = 268;
        // tunables (Java constants)
        public static int LevelRequire = 30;
        public static int DayPerDay = 20, BossPerDay = 2, BeastItemPerDay = 2;
        public static int DayCountMin = 20, DayCountMax = 40; // NinjaUtils.nextInt(20, 40): 20..39
        public static int DayLevelRange = 5;
        public static int RewardItem = 251, DayRewardQuantity = 2, DayRewardYen = 20000, BossRewardQuantity = 5;
        /// <summary>Maps that never count as task maps (Zone.createMonster isMapTask: dungeons, arenas, battlefields,
        /// legendary / ancient / hell villages, Chikatoya, Inoshishi...). Non-normal map types are excluded as well.</summary>
        public static readonly HashSet<int> ExcludedMaps = new HashSet<int>
        {
            74,
            78,
            110,
            112,
            113,
            117,
            130,
            131,
            132,
            133,
            134,
            135,
            136,
            137,
            138,
            154,
            155,
            156,
            161,
            162,
            163,
            164,
            165,
            168,
            176,
            177,
            178
        };
        private static readonly Random rnd = new Random();
        // TaskFactory.taskDays / taskBosss: {mobId, level, mapId}
        private static List<int[]> taskDays, taskBosss;
        private static void Build(GameWorld world)
        {
            if (taskDays != null)
                return;
            var days = new List<int[]>();
            var bosses = new List<int[]>();
            var maps = new List<TileMap>();
            for (int id = 0; id < 1000; id++)
            {
                var t = world.FindMap(id);
                if (t != null)
                    maps.Add(t);
            }

            foreach (var tile in maps)
            {
                if (!tile.IsNormal || ExcludedMaps.Contains(tile.Id))
                    continue;
                foreach (var pos in tile.Monsters)
                {
                    var tpl = world.FindMob(pos.TemplateId);
                    if (tpl == null || tpl.IsBoss)
                        continue;
                    var list = pos.IsBeast ? bosses : days; // zone 0: MobFactory isBeast && zone.id % 5 == 0
                    bool found = false;
                    foreach (var e in list)
                        if (e[0] == tpl.Id && e[1] == tpl.Level && e[2] == tile.Id)
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                        list.Add(new[] { tpl.Id, (int)tpl.Level, tile.Id });
                }
            }

            taskDays = days;
            taskBosss = bosses;
        }

        // TaskFactory.createTaskOrder
        private static int[] Create(GameWorld world, int type, int level)
        {
            Build(world);
            int[] info = null;
            if (type == TaskDay)
            {
                int levelMob = 0, min = -1; // getLevellMobInfoClosestLevel
                foreach (var e in taskDays)
                    if (min == -1 || Math.Abs(e[1] - level) < min)
                    {
                        min = Math.Abs(e[1] - level);
                        levelMob = e[1];
                    }

                var list = taskDays.FindAll(e => Math.Abs(e[1] - levelMob) <= DayLevelRange); // randomMobInfoTaskDay
                if (list.Count == 0)
                    return null;
                info = list[rnd.Next(list.Count)];
                return new[]
                {
                    TaskDay,
                    0,
                    rnd.Next(DayCountMin, DayCountMax),
                    info[0],
                    info[2]
                };
            }

            int bmin = -1; // getMobInfoTaskBossClosestLevel
            foreach (var e in taskBosss)
                if (bmin == -1 || Math.Abs(e[1] - level) < bmin)
                {
                    bmin = Math.Abs(e[1] - level);
                    info = e;
                }

            if (info == null)
                return null;
            return new[]
            {
                TaskBoss,
                0,
                1,
                info[0],
                info[2]
            };
        }

        private static int[] Find(PlayerState me, int type)
        {
            foreach (var t in me.Progress.TaskOrders)
                if (t[0] == type)
                    return t;
            return null;
        }

        // ---------------------------------------------------------------- messages (Service)
        private static void Send(PlayerState me, GameWorld world, int[] t)
        {
            var tile = world.FindMap(t[4]);
            string mapName = tile != null ? tile.Name : "";
            var m = new Message(CmdGetTaskOrder);
            m.Writer.WriteByte((byte)t[0]);
            m.Writer.WriteInt(t[1]);
            m.Writer.WriteInt(t[2]);
            m.Writer.WriteUTF(t[0] == TaskDay ? "Nhiệm vụ hàng ngày" : "Nhiệm vụ truy bắt tà thú");
            m.Writer.WriteUTF("Ghi chú: đi đến " + mapName + " để làm nhiệm vụ.");
            m.Writer.WriteByte((byte)t[3]);
            m.Writer.WriteByte((byte)t[4]);
            me.Session.Send(m);
        }

        private static void SendUpdate(PlayerState me, int[] t)
        {
            var m = new Message(CmdGetTaskUpdate);
            m.Writer.WriteByte((byte)t[0]);
            m.Writer.WriteInt(t[1]);
            me.Session.Send(m);
        }

        private static void SendClear(PlayerState me, int type)
        {
            var m = new Message(CmdClearTaskOrder);
            m.Writer.WriteByte((byte)type);
            me.Session.Send(m);
        }

        // ---------------------------------------------------------------- hooks
        /// <summary>Char.load (new day resets the daily counters) + User.selectChar (sendTaskOrder for each one).</summary>
        public static void OnEnterWorld(PlayerState me, GameWorld world)
        {
            var p = me.Progress;
            string today = DateTime.Now.ToString("yyyyMMdd");
            if (p.DailyDate != today)
            {
                p.DailyDate = today;
                p.CountFinishDay = DayPerDay;
                p.CountDoneTaskDay = 0;
                p.CountLoopBoss = BossPerDay;
                p.CountUseItemBeast = BeastItemPerDay;
                PlayerItems.Save(me);
            }

            foreach (var t in p.TaskOrders)
                Send(me, world, t);
        }

        /// <summary>Mob.dead: a kill of the ordered monster counts (beast quest: only the beast, levelBoss 3).</summary>
        public static void OnKill(PlayerState me, Mob mob)
        {
            foreach (var t in me.Progress.TaskOrders)
            {
                if (t[1] >= t[2] || t[3] != mob.Template.Id)
                    continue;
                if (t[0] == TaskBoss && mob.LevelBoss != 3)
                    continue;
                t[1] = Math.Min(t[2], t[1] + 1); // TaskOrder.updateTask
                SendUpdate(me, t);
                PlayerItems.Save(me);
            }
        }

        /// <summary>Char.useItem TA_THU_LENH: one more beast quest today (at most BeastItemPerDay uses a day).</summary>
        public static bool UseItem(PlayerState me, int indexUI, PlayerItem item)
        {
            if (item.Id != ItemTaThuLenh)
                return false;
            var p = me.Progress;
            if (p.CountUseItemBeast > 0)
            {
                p.CountLoopBoss++;
                p.CountUseItemBeast--;
                PlayerItems.RemoveItem(me, indexUI, 1, true);
                PlayerService.ServerMessage(me, "Bạn nhận được thêm 1 lần làm nhiệm vụ tà thú.");
            }
            else
                PlayerService.ServerMessage(me, "Đã hết số lần dùng trong ngày hôm nay rồi.");
            return true;
        }

        // ---------------------------------------------------------------- Rikudou menu (Char.npcRikudou)
        public static void NpcRikudou(PlayerState me, GameWorld world, Action<string, Action> add, Action<object[]> subMenu)
        {
            add("NV mỗi ngày", () => subMenu(new object[] { "Nhận", (Action)(() => OrderDay(me, world)), "Hủy", (Action)(() => Cancel(me, TaskDay)), "Hoàn thành", (Action)(() => FinishDay(me, world)), "Đi làm NV", (Action)(() => WorkDay(me, world)) }));
            add("NV Truy bắt Tà Thú", () => subMenu(new object[] { "Nhận", (Action)(() => OrderBoss(me, world)), "Hủy", (Action)(() => Cancel(me, TaskBoss)), "Hoàn Thành", (Action)(() => FinishBoss(me, world)) }));
        }

        private static void Chat(PlayerState me, string text)
        {
            PlayerService.NpcChat(me, Rikudou, text);
        }

        // Char.orderTaskDay
        private static void OrderDay(PlayerState me, GameWorld world)
        {
            var p = me.Progress;
            if (Find(me, TaskDay) != null)
            {
                Chat(me, "Nhiệm vụ lần trước ta giao, con vẫn chưa hoàn thành.");
                return;
            }

            if (me.Level < LevelRequire)
            {
                Chat(me, "Con hãy có gắng luyện tập đạt cấp " + LevelRequire + " rồi quay lại đây gặp ta.");
                return;
            }

            if (p.CountFinishDay <= 0)
            {
                Chat(me, "Hôm nay con đã làm hết nhiệm vụ ta giao. Hãy quay lại vào ngày hôm sau.");
                return;
            }

            var t = Create(world, TaskDay, me.Level);
            if (t == null)
                return;
            p.CountFinishDay--;
            Send(me, world, t);
            p.TaskOrders.Add(t);
            PlayerItems.Save(me);
            Chat(me, "Đây là lần nhận nhiệm vụ thứ " + (DayPerDay - p.CountFinishDay) + " trong ngày hôm nay. Mỗi ngày được nhận tối đa " + DayPerDay + " lần con nhé.");
        }

        // Char.orderTaskBoss (single player: no group checks)
        private static void OrderBoss(PlayerState me, GameWorld world)
        {
            var p = me.Progress;
            if (Find(me, TaskBoss) != null)
            {
                Chat(me, "Nhiệm vụ lần trước ta giao, con vẫn chưa hoàn thành.");
                return;
            }

            if (me.Level < LevelRequire)
            {
                Chat(me, "Con hãy có gắng luyện tập đạt cấp " + LevelRequire + " rồi quay lại đây gặp ta.");
                return;
            }

            if (p.CountLoopBoss <= 0)
            {
                Chat(me, "Nhiệm vụ truy bắt tà thú của ngày hôm nay đã hết con hãy quay lại vào ngày mai.");
                return;
            }

            var t = Create(world, TaskBoss, me.Level);
            if (t == null)
                return;
            p.CountLoopBoss--;
            Send(me, world, t);
            p.TaskOrders.Add(t);
            PlayerItems.Save(me);
            Chat(me, "Con hay sớm hoàn thành nhiệm vụ để nhận thưởng từ ta.");
        }

        // Char.cancelTaskDay / cancelTaskBoss
        private static void Cancel(PlayerState me, int type)
        {
            var t = Find(me, type);
            if (t == null)
            {
                Chat(me, "Hiện tại con không có nhiệm vụ để hủy.");
                return;
            }

            SendClear(me, type);
            me.Progress.TaskOrders.Remove(t);
            PlayerItems.Save(me);
            Chat(me, "Ta đã hủy nhiệm vụ của con. Lần sau cố gắng hoàn thành tốt nhiệm vụ con nhé.");
        }

        // Char.finishTaskDay (clan points, events and the weekly quests are not part of the offline game)
        private static void FinishDay(PlayerState me, GameWorld world)
        {
            var t = Find(me, TaskDay);
            if (t == null)
            {
                Chat(me, "Hãy làm việc cho ta bằng cách làm nhiệm vụ, sau khi hoàn thành con sẽ nhận được phần thưởng từ ta.");
                return;
            }

            if (t[1] < t[2])
            {
                Chat(me, "Con hãy hoàn thành nhiệm vụ rồi quay lại đây.");
                return;
            }

            Chat(me, "Tốt lắm! Ta có phần thưởng dành cho con.");
            var p = me.Progress;
            p.PointUyDanh++;
            PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(RewardItem, DayRewardQuantity));
            PlayerItems.AddYen(me, DayRewardYen);
            p.CountDoneTaskDay++;
            SendClear(me, TaskDay);
            p.TaskOrders.Remove(t);
            PlayerItems.Save(me);
        }

        // Char.finishTaskBoss
        private static void FinishBoss(PlayerState me, GameWorld world)
        {
            var t = Find(me, TaskBoss);
            if (t == null)
            {
                Chat(me, "Hãy làm việc cho ta bằng cách làm nhiệm vụ, sau khi hoàn thành con sẽ nhận được phần thưởng từ ta.");
                return;
            }

            if (t[1] < t[2])
            {
                Chat(me, "Con hãy hoàn thành nhiệm vụ rồi quay lại đây.");
                return;
            }

            Chat(me, "Tốt lắm! Ta có phần thưởng dành cho con.");
            PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(RewardItem, BossRewardQuantity));
            SendClear(me, TaskBoss);
            me.Progress.TaskOrders.Remove(t);
            PlayerItems.Save(me);
        }

        // Char.workTaskDay: go to the quest map at NinjaUtils.getFirstPosition
        private static void WorkDay(PlayerState me, GameWorld world)
        {
            var t = Find(me, TaskDay);
            if (t == null)
            {
                Chat(me, "Hãy nhận nhiệm vụ mỗi ngày từ ta rồi mới sử dụng tính năng này.");
                return;
            }

            var tile = world.FindMap(t[4]);
            if (tile == null)
                return;
            var grid = world.GetGrid(tile);
            short x = 120, y = 0;
            while (grid.TileTypeAt(x, y, TileGrid.T_TOP))
            {
                y += 24;
                if (y > grid.PxH)
                    break;
            }

            while (!grid.TileTypeAt(x, y, TileGrid.T_TOP))
            {
                y += 24;
                if (y > grid.PxH)
                    break;
            }

            me.SetXY(x, y);
            me.Actions.ChangeMap(t[4]);
        }
    }
}
