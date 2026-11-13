using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class

    // Port of the main quest chain and NPC menus of com.nsoz.model.Char (ordinary maps):
    //   openMenu / initMenu / menu / getTask, takingTask, taskNext, updateTaskCount, finishTask + updateTask,
    //   updateTaskLevelUp / updateTaskKillMonster / updateTaskPickItem / getIdItemTask, questionAndAnswer, admission,
    //   the village and school NPC menus (Kiriko, Tabemono, Kenshinto, Okanechan, Kamakura, Umayaki, Tajima, the three
    //   teachers, Kanata, Furoya, Ameji, Goosho) and the quest hooks in useItem / buyItem / changeMap / learnSkill.
    //
    // Offline additions (marked "offline" below):
    //   - a quest step whose mechanic is not ported (dungeons, escort, fishing, daily tasks, friends, upgrades...) can be
    //     skipped at the NPC that gives the quest, so the chain never blocks;
    //   - a "talk to X" step on an NPC that has no hand-ported menu gets a generic entry named after the step.
    public static class PlayerTasks
    {
        // TaskName
        public const int TaskChaoLang = 0, TaskKienThuc = 1, TaskLanDauDungKiem = 2, TaskDietSenTruCoc = 3, TaskVatLieuTaoGiap = 4, TaskHaiThuoc = 5, TaskKhamPha = 6, TaskBaiHocVaoTruong = 7, TaskTimHieu3Truong = 8, TaskGiaTangSucManh = 9, TaskBanHuuTamGiao = 11, TaskNangCapTrangBi = 12, TaskTruyenTaiTinTuc = 15;
        // NpcName
        private const int Kanata = 0, Furoya = 1, Ameji = 2, Kiriko = 3, Tabemono = 4, Kamakura = 5, Kenshinto = 6, Umayaki = 7, Umayaki2 = 8, Toyotomi = 9, Ookamesama = 10, Kazeto = 11, Tajima = 12, Matsurugi = 23, Okanechan = 24, Goosho = 26;
        // StoreManager types
        private const int ShopWeapon = 2, ShopPotion = 6, ShopPotionLock = 7, ShopFood = 8, ShopFoodLock = 9, ShopMisc = 14, ShopBook = 15, ShopNecklace = 16, ShopRing = 17, ShopPearl = 18, ShopSpell = 19, ShopFashion = 32;
        // finishTask rewards: [exp, yen] per quest id
        private static readonly long[, ] Rewards =
        {
            {
                200,
                10000
            },
            {
                400,
                100
            },
            {
                800,
                200
            },
            {
                1600,
                300
            },
            {
                3200,
                400
            },
            {
                4000,
                500
            },
            {
                6000,
                600
            },
            {
                8000,
                800
            },
            {
                12000,
                1000
            },
            {
                16000,
                5000
            },
            {
                22000,
                10000
            },
            {
                30000,
                20000
            },
            {
                40000,
                30000
            },
            {
                60000,
                30000
            },
            {
                80000,
                40000
            },
            {
                100000,
                40000
            },
            {
                140000,
                50000
            },
            {
                200000,
                60000
            },
            {
                250000,
                70000
            },
            {
                300000,
                80000
            },
            {
                360000,
                90000
            },
            {
                400000,
                100000
            },
            {
                500000,
                110000
            },
            {
                600000,
                120000
            },
            {
                700000,
                130000
            },
            {
                800000,
                150000
            },
            {
                900000,
                160000
            },
            {
                1000000,
                170000
            },
            {
                1100000,
                180000
            },
            {
                1200000,
                190000
            },
            {
                1300000,
                200000
            },
            {
                1400000,
                210000
            },
            {
                1500000,
                220000
            },
            {
                1600000,
                230000
            },
            {
                1700000,
                240000
            },
            {
                1800000,
                250000
            },
            {
                1900000,
                260000
            },
            {
                2000000,
                270000
            },
            {
                2100000,
                280000
            },
            {
                2200000,
                290000
            },
            {
                2300000,
                300000
            },
            {
                2400000,
                400000
            },
            {
                10000000,
                1000000
            }
        };
        // Char.getIdItemTask: [mob template, quest, step, item dropped]
        private static readonly int[, ] TaskDrops =
        {
            {
                3,
                4,
                1,
                209
            }, // nhím đá -> lông nhím
            {
                4,
                4,
                2,
                210
            }, // thỏ xám -> da thỏ
            {
                26,
                18,
                1,
                216
            }, // khỉ -> nhị hầu tửu
            {
                27,
                18,
                2,
                217
            }, // châu chấu -> xác châu chấu
            {
                15,
                14,
                2,
                213
            }, // ốc đá -> vỏ ốc đá
            {
                54,
                5,
                1,
                211
            }, // thảo dược -> bông thảo dược
            {
                33,
                22,
                1,
                230
            }, // xương khô -> chìa khóa cơ quan
            {
                60,
                38,
                1,
                349
            }, // ma đêm -> lồng đèn ma
            {
                61,
                38,
                2,
                350
            }, // bướm phấn -> cánh bướm
            {
                69,
                20,
                1,
                221
            }, // heo rừng -> rìu bạc
            {
                41,
                27,
                1,
                238
            }, // dơi lửa -> xác dơi lửa
            {
                47,
                31,
                1,
                264
            }, // cá hổ -> giỏ cá hổ
            {
                48,
                31,
                2,
                265
            }, // rắn tía -> giỏ rắn tía
            {
                57,
                36,
                1,
                348
            } // oan hồn -> oan hồn
        };
        // NPCs with a hand-ported menu (the cases of the switch in InitMenu). Read by the dev wiki (DevWiki/WikiData.cs).
        public static readonly int[] PortedNpcMenus =
        {
            Kanata,
            Furoya,
            Ameji,
            Kiriko,
            Tabemono,
            Kamakura,
            Kenshinto,
            Okanechan,
            Umayaki,
            Umayaki2,
            Tajima,
            Toyotomi,
            Ookamesama,
            Kazeto,
            Goosho
        };
        // read-only views of the tables above for the dev wiki
        public static long[] RewardOf(int taskId)
        {
            return taskId >= 0 && taskId < Rewards.GetLength(0) ? new[]
            {
                Rewards[taskId, 0],
                Rewards[taskId, 1]
            }

            : null;
        }

        public static List<int[]> TaskDropList()
        {
            var list = new List<int[]>();
            for (int i = 0; i < TaskDrops.GetLength(0); i++)
                list.Add(new[] { TaskDrops[i, 0], TaskDrops[i, 1], TaskDrops[i, 2], TaskDrops[i, 3] });
            return list;
        }

        private static readonly Random Rng = new Random();
        private static string Pick(params string[] s)
        {
            return s[Rng.Next(s.Length)];
        }

        private static TaskTemplateData Current(PlayerState me, GameWorld world)
        {
            return me.Progress.TaskActive ? world.FindTask(me.Record.TaskId) : null;
        }

        private static bool OnStep(PlayerState me, int task, int index)
        {
            return me.Record.TaskId == task && me.Progress.TaskActive && me.Progress.TaskIndex == index;
        }

        // Char.getTaskNpcId: the NPC to talk to now (-2 = none, -3 = no more quests)
        public static int TaskNpcId(PlayerState me, GameWorld world)
        {
            var npcs = world.TaskNpcs(me.Record.TaskId);
            if (npcs == null)
                return -3;
            int i = me.Progress.TaskActive ? me.Progress.TaskIndex + 1 : 0;
            if (i >= npcs.Length)
                return -1;
            int b = npcs[i];
            if (b == -1)
            {
                int c = me.Record.ClassId;
                if (c == 0 && me.Record.TaskId == TaskGiaTangSucManh)
                    b = -2;
                else if (c <= 2)
                    b = Toyotomi;
                else if (c <= 4)
                    b = Ookamesama;
                else
                    b = Kazeto;
            }

            return b;
        }

        // ================================================================ entering the world
        // User.selectChar: show the quest in progress
        public static void OnEnterWorld(PlayerState me, GameWorld world)
        {
            TaskOrders.OnEnterWorld(me, world);
            var t = Current(me, world);
            if (t == null)
                return;
            UpdateTaskLevelUp(me, world);
            PlayerService.SendTaskInfo(me, t);
        }

        // ================================================================ NPC menus (cmd 40, 29, 47)
        // Char.openMenu (cmd 40): build the NPC's menu; without entries the client shows the NPC's own menu
        public static void OpenMenu(Message m, PlayerState me, GameWorld world)
        {
            int npcId = m.Reader.ReadShort();
            if (!HasNpc(me, npcId))
            {
                PlayerService.ServerDialog(me, "Không tìm thấy NPC này");
                return;
            }

            me.BoxOpen = false;
            InitMenu(me, world, npcId);
            if (me.Menus.Count > 0)
                PlayerService.OpenUiMenu(me);
            else
                PlayerService.ClientMenu(me);
        }

        // Char.menu (cmd 29): typeClose, npc template, menu index, option index
        public static void Menu(Message m, PlayerState me, GameWorld world)
        {
            m.Reader.ReadSByte(); // typeClose
            int npcId = m.Reader.ReadSByte();
            int menuId = m.Reader.ReadSByte();
            m.Reader.ReadSByte(); // optionId (only used by NPCs that are not ported)
            if (npcId != 0 && !HasNpc(me, npcId))
            {
                PlayerService.ServerDialog(me, "Không tìm thấy NPC này");
                return;
            }

            if (me.Menus.Count == 0)
                InitMenu(me, world, npcId);
            if (me.Menus.Count == 0 || menuId < 0 || menuId >= me.Menus.Count)
            {
                me.Menus.Clear();
                PlayerService.ServerDialog(me, "Chức năng này chưa có trong bản offline.");
                return;
            }

            var entry = me.Menus[menuId];
            me.Menus.Clear();
            Run(me, world, entry, npcId);
        }

        // Char.getTask (cmd 47): an answer chosen in a confirm dialog (accept quest, quiz answers)
        public static void GetTask(Message m, PlayerState me, GameWorld world)
        {
            int npcId = m.Reader.ReadSByte();
            int menuId = m.Reader.ReadSByte();
            if (!HasNpc(me, npcId))
            {
                PlayerService.ServerDialog(me, "Không tìm thấy NPC này");
                return;
            }

            if (me.Menus.Count == 0 || menuId < 0 || menuId >= me.Menus.Count)
                return;
            var entry = me.Menus[menuId];
            me.Menus.Clear();
            Run(me, world, entry, npcId);
        }

        private static bool HasNpc(PlayerState me, int templateId)
        {
            foreach (var n in me.Zone.Npcs)
                if (n.TemplateId == templateId)
                    return true;
            return false;
        }

        // Char.menu(Menu, npc)
        private static void Run(PlayerState me, GameWorld world, MenuEntry e, int npc)
        {
            switch (e.Kind)
            {
                case MenuEntry.Execute:
                    if (e.Run != null)
                        e.Run();
                    break;
                case MenuEntry.FinishTask:
                    FinishTask(me, world);
                    break;
                case MenuEntry.TakeTask:
                    me.Menus.Clear();
                    me.Menus.Add(new MenuEntry(MenuEntry.ConfirmTakeTask, "Nhận"));
                    me.Menus.Add(new MenuEntry(MenuEntry.Cancel, "Hủy"));
                    PlayerService.OpenUiConfirm(me, npc, "Bạn có muốn nhận nhiệm vụ này.");
                    break;
                case MenuEntry.ConfirmTakeTask:
                    TakingTask(me, world);
                    break;
                case MenuEntry.DoTask:
                    if (!me.Progress.TaskActive)
                        break;
                    if (npc == 14 || npc == 15 || npc == 16)
                    {
                        // NV truyền tải tin tức: hand over one letter
                        TaskNext(me);
                        int i = PlayerItems.IndexInBag(me, 214);
                        if (i != -1)
                            PlayerItems.RemoveItem(me, i, 1, true);
                    }
                    else if (me.Record.TaskId == TaskKienThuc || me.Record.TaskId == TaskBaiHocVaoTruong)
                        QuestionAndAnswer(me);
                    else if (e.Run != null)
                        e.Run();
                    break;
                case MenuEntry.A:
                case MenuEntry.B:
                case MenuEntry.C:
                case MenuEntry.D:
                    Answer(me, e.Kind);
                    break;
            }

            // after a quiz answer: next question, or the quiz step is done (five steps at once)
            if (me.Progress.TaskActive && e.Kind >= MenuEntry.A && e.Kind <= MenuEntry.D)
            {
                int task = me.Record.TaskId, idx = me.Progress.TaskIndex;
                bool quiz = (task == TaskKienThuc && idx == 0) || (task == TaskBaiHocVaoTruong && (idx == 1 || idx == 6 || idx == 11));
                if (quiz)
                {
                    if (me.NAnswer < 5)
                        QuestionAndAnswer(me);
                    else
                    {
                        for (int i = 0; i < 5; i++)
                            TaskNext(me);
                        me.NAnswer = 0;
                        if (task == TaskKienThuc)
                            PlayerService.NpcChat(me, Kamakura, "Haha, tốt hãy quay về tìm trưởng làng, ta đã gửi ông ấy 1 món quà đặc biệt cho con.");
                    }
                }
            }
        }

        // Char.initMenu: quest entries first, then the NPC's own functions
        private static void InitMenu(PlayerState me, GameWorld world, int npc)
        {
            me.Menus.Clear();
            int taskNpc = TaskNpcId(me, world);
            var t = world.FindTask(me.Record.TaskId);
            if (taskNpc == npc && t != null)
            {
                if (!me.Progress.TaskActive)
                    me.Menus.Add(new MenuEntry(MenuEntry.TakeTask, t.Name));
                else if (me.Progress.TaskIndex == t.SubNames.Length - 1)
                    me.Menus.Add(new MenuEntry(MenuEntry.FinishTask, "Hoàn thành nhiệm vụ"));
                else if (npc == 14 || npc == 15 || npc == 16)
                    me.Menus.Add(new MenuEntry(MenuEntry.DoTask, "Giao thư"));
                else if (me.Record.TaskId == TaskKienThuc || me.Record.TaskId == TaskBaiHocVaoTruong)
                    me.Menus.Add(new MenuEntry(MenuEntry.DoTask, t.Name));
                else if (!HasOwnTalkHook(npc))
                {
                    // offline: a "talk to this NPC" step on an NPC whose special menu is not ported
                    string step = t.SubNames[me.Progress.TaskIndex];
                    me.Menus.Add(new MenuEntry(MenuEntry.DoTask, string.IsNullOrEmpty(step) ? "Nói chuyện" : step, () => TaskNext(me)));
                }
            }

            // offline: let the quest giver skip a step whose mechanic is not ported, so the chain never blocks
            if (t != null && me.Progress.TaskActive && npc == QuestGiver(world, me.Record.TaskId) && !IsStepSupported(world, t, me.Progress.TaskIndex))
            {
                string step = t.SubNames[me.Progress.TaskIndex];
                me.Menus.Add(new MenuEntry(MenuEntry.Execute, "Bỏ qua: " + step, () =>
                {
                    TaskNext(me);
                    PlayerService.ServerMessage(me, "Bản offline: đã bỏ qua bước \"" + step + "\".");
                }));
            }

            // Char.initMenu: inside a gymnasium the NPC only offers "Rời khỏi nơi này" (zone.tilemap.isNhaThiDau)
            if (Gymnasium.IsGym(me.Zone))
            {
                Add(me, "Rời khỏi nơi này", () => Gymnasium.Leave(me, world));
                return;
            }

            switch (npc)
            {
                case Kanata:
                    NpcKanata(me);
                    break;
                case Furoya:
                    NpcFuroya(me);
                    break;
                case Ameji:
                    NpcAmeji(me);
                    break;
                case Kiriko:
                    NpcKiriko(me);
                    break;
                case Tabemono:
                    NpcTabemono(me);
                    break;
                case Kamakura:
                    NpcKamakura(me);
                    break;
                case Kenshinto:
                    NpcKenshinto(me);
                    break;
                case Okanechan:
                    PlayerLuong.NpcOkanechan(me, (n, a) => Add(me, n, a), items => SubMenu(me, items));
                    NpcTalk(me, Okanechan, TaskChaoLang, 3, "Ta là hiện thân của thần tài sẽ mang đến tài lộc cho mọi người.", "Con hãy chăm đánh quái, làm nhiệm vụ để có thêm nhiều yên hơn.");
                    break;
                case Umayaki:
                    NpcUmayaki(me, world);
                    break;
                case Umayaki2:
                    NpcUmayaki2(me, world);
                    break;
                case Tajima:
                    NpcTajima(me, world);
                    break;
                case Toyotomi:
                    NpcTeacher(me, world, Toyotomi, 1, "Ninja Kiếm", 2, "Ninja Phi Tiêu", "Trường ta dạy kiếm và phi tiêu, chẳng dạy các vũ khí vô danh khác");
                    break;
                case Ookamesama:
                    NpcTeacher(me, world, Ookamesama, 3, "Ninja Kunai", 4, "Ninja Cung", "Học, để thành tài, để thành người tốt, chứ không phải để ganh đua với đời.");
                    break;
                case Kazeto:
                    NpcTeacher(me, world, Kazeto, 5, "Ninja Đao", 6, "Ninja Quạt", "So với các trường khác, trường Gió là tốt nhất");
                    break;
                case Goosho:
                    NpcGoosho(me);
                    break;
                case TaskOrders.Rikudou:
                    TaskOrders.NpcRikudou(me, world, (n, a) => Add(me, n, a), items => SubMenu(me, items));
                    break;
                case Escort.NpcJaian: // Char.initMenu NpcName.JAIAN: "Đi nào" starts the escort (Escort.cs)
                    Escort.InitMenu(me, (n, a) => Add(me, n, a));
                    break;
                case Matsurugi: // Char.npcMatsurugi: a new key for the tunnel
                    if (TaskMechanics.OnTunnelStep(me))
                        Add(me, "Nhận chìa khóa", () => TaskMechanics.GiveNewKey(me, world));
                    break;
            }
        }

        // NPCs whose "Nói chuyện" entry advances talk steps on their own
        private static bool HasOwnTalkHook(int npc)
        {
            return npc == Kiriko || npc == Tabemono || npc == Kenshinto || npc == Okanechan || npc == Kamakura || npc == Umayaki || npc == Umayaki2 || npc == Toyotomi || npc == Ookamesama || npc == Kazeto || npc == Escort.NpcJaian;
        }

        // the NPC that hands out and takes back a quest (first entry of the `task.npcs` row)
        private static int QuestGiver(GameWorld world, int taskId)
        {
            var npcs = world.TaskNpcs(taskId);
            return npcs != null && npcs.Length > 0 ? npcs[0] : -1;
        }

        // whether step `idx` of a quest can be done with what is ported (public: the dev wiki lists it per step)
        public static bool IsStepSupported(GameWorld world, TaskTemplateData t, int idx)
        {
            int task = t.Id;
            if (idx >= t.SubNames.Length - 1)
                return true; // report step
            if (idx == 0 && t.LevelRequire > 0)
                return true; // level step
            if (idx < t.KillMobs.Length && t.KillMobs[idx][0] >= 0)
                return true; // kill step
            if (idx < t.PickItems.Length && t.PickItems[idx] >= 0) // pick step: a monster drops it
            {
                for (int i = 0; i < TaskDrops.GetLength(0); i++)
                    if (TaskDrops[i, 1] == task && TaskDrops[i, 2] == idx)
                        return true;
                return false;
            }

            var npcs = world.TaskNpcs(task);
            if (npcs != null && idx + 1 < npcs.Length && npcs[idx + 1] >= 0)
                return true; // talk step
            // steps with a hand-ported hook
            if (task == TaskLanDauDungKiem && idx == 0)
                return true; // use the weapon
            if (task == TaskDietSenTruCoc && idx <= 1)
                return true; // buy / eat food
            if (task == TaskKhamPha && idx >= 1 && idx <= 3)
                return true; // reach places
            if (task == TaskGiaTangSucManh && idx <= 2)
                return true; // learn a book, raise a skill, raise potential
            if (task == TaskBanHuuTamGiao && idx == 1)
                return true; // removed offline, TaskNext jumps over it
            if ((task == TaskMechanics.TaskThuThapNguyenLieu || task == TaskMechanics.TaskThuThapTinhTheBang || task == TaskMechanics.TaskLayNuocHangSau) && idx == 1)
                return true; // TaskMechanics
            if ((task == TaskMechanics.TaskTruyTimDiaDo || task == TaskMechanics.TaskTruyTimBaoVat) && idx == 1)
                return true; // tunnel / dig
            if (task == TaskNangCapTrangBi)
                return true; // upgrade at Kenshinto (PlayerUpgrade), checked on equip
            return false;
        }

        // ---- quest flow ----
        // Char.takingTask
        private static void TakingTask(PlayerState me, GameWorld world)
        {
            if (me.Progress.TaskActive)
                return;
            var t = world.FindTask(me.Record.TaskId);
            if (t == null)
                return;
            me.Progress.TaskActive = true;
            me.Progress.TaskIndex = 0;
            me.Progress.TaskCount = t.Counts.Length > 0 && t.Counts[0] < 0 ? t.Counts[0] : 0;
            if (t.Id == TaskTruyenTaiTinTuc)
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(214, 3)); // three letters
            TaskMechanics.OnTakeTask(me, world, t.Id);
            PlayerItems.Save(me);
            PlayerService.SendTaskInfo(me, t);
            if (t.Id == TaskLanDauDungKiem && me.Inventory.Equipment[1] != null)
                TaskNext(me);
            UpdateTaskLevelUp(me, world);
        }

        // Char.taskNext
        public static void TaskNext(PlayerState me)
        {
            if (!me.Progress.TaskActive)
                return;
            me.Progress.TaskIndex++;
            // Offline deviation (user request 2026-11-12): the "Giao lưu bạn hữu" step of NV Bạn hữu tâm giao is removed
            if (me.Record.TaskId == TaskBanHuuTamGiao && me.Progress.TaskIndex == 1)
                me.Progress.TaskIndex++;
            me.Progress.TaskCount = 0;
            var t = me.Session.Server.World.FindTask(me.Record.TaskId);
            if (t != null && me.Progress.TaskIndex > t.Counts.Length - 1)
                me.Progress.TaskIndex = Math.Max(0, t.Counts.Length - 1);
            PlayerItems.Save(me);
            PlayerService.TaskNext(me);
            if (me.Record.TaskId == TaskBanHuuTamGiao && me.Progress.TaskIndex == 2)
                PlayerService.TaskNext(me); // client skips the removed step too
            TaskMechanics.RefreshVisible(me); // ore / ice crystal appear on their step
        }

        // Char.updateTaskCount
        public static void UpdateTaskCount(PlayerState me, GameWorld world, int count)
        {
            var t = Current(me, world);
            if (t == null)
                return;
            me.Progress.TaskCount += count;
            PlayerService.TaskCount(me, me.Progress.TaskCount);
            int idx = me.Progress.TaskIndex;
            if (idx < t.Counts.Length && me.Progress.TaskCount >= t.Counts[idx])
                TaskNext(me);
            else
                PlayerItems.Save(me);
        }

        // Offline deviation (dev terminal /next): bypasses one step's requirements via Char.taskNext.
        // Reuse TaskNext for saves, client sync, removed-step policy and visible quest objects.
        // Accepting/finishing a quest stays explicit; /skip already covers whole-quest completion.
        public static string NextTaskStep(PlayerState me, GameWorld world)
        {
            var t = world.FindTask(me.Record.TaskId);
            if (t == null)
                return "Không còn nhiệm vụ nào.";
            if (!me.Progress.TaskActive)
                return "Chưa nhận nhiệm vụ. Hãy nhận nhiệm vụ trước khi dùng /next.";
            int lastStep = Math.Min(t.SubNames.Length, t.Counts.Length) - 1;
            if (lastStep < 0)
                return "Nhiệm vụ không có bước hợp lệ.";
            if (me.Progress.TaskIndex < 0 || me.Progress.TaskIndex > lastStep)
                return "Bước nhiệm vụ không hợp lệ.";
            if (me.Progress.TaskIndex == lastStep)
                return "Đã ở bước cuối. Hãy trả nhiệm vụ hoặc dùng /skip để hoàn thành.";
            TaskNext(me);
            return "Nhiệm vụ '" + t.Name + "': bước " + (me.Progress.TaskIndex + 1) + " - " + t.SubNames[me.Progress.TaskIndex] + ".";
        }

        // Offline deviation (dev terminal /skip): completes the current quest at once. Takes it if not taken yet,
        // jumps to the last step and runs FinishTask (normal rewards, next quest). Returns a reply for the terminal.
        public static string SkipTask(PlayerState me, GameWorld world)
        {
            var t = world.FindTask(me.Record.TaskId);
            if (t == null)
                return "Không còn nhiệm vụ nào.";
            if (!me.Progress.TaskActive)
                TakingTask(me, world);
            if (!me.Progress.TaskActive)
                return "Không nhận được nhiệm vụ '" + t.Name + "'.";
            me.Progress.TaskIndex = Math.Max(0, t.SubNames.Length - 1);
            me.Progress.TaskCount = 0;
            PlayerService.SendTaskInfo(me, t);
            int before = me.Record.TaskId;
            FinishTask(me, world);
            if (me.Record.TaskId == before)
                return "Chưa hoàn thành được (hành trang đầy?).";
            return "Đã hoàn thành nhiệm vụ '" + t.Name + "'.";
        }

        // Char.finishTask + updateTask
        private static void FinishTask(PlayerState me, GameWorld world)
        {
            var t = Current(me, world);
            if (t == null || me.Progress.TaskIndex != t.SubNames.Length - 1)
                return;
            int task = t.Id;
            int free = PlayerItems.SlotNull(me);
            int needSlots = task == TaskKienThuc || task == TaskVatLieuTaoGiap || task == TaskTimHieu3Truong || task >= 36 ? 1 : task == TaskHaiThuoc ? 2 : 0;
            if (free < needSlots)
            {
                PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
                return;
            }

            // quest items are taken back before the reward goes into the bag
            var bag = me.Inventory.Bag;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] == null)
                    continue;
                var it = world.FindItem(bag[i].Id);
                if (it != null && (it.Type == 23 || it.Type == 24 || it.Type == 25))
                {
                    int q = bag[i].Quantity;
                    bag[i] = null;
                    PlayerService.UseItemUpToUp(me, i, q);
                }
            }

            switch (task)
            {
                case TaskKienThuc:
                    // Char.receiveWoodenSword (the basic attack is already known offline)
                    var sword = PlayerItems.CreateTaskWoodenSword();
                    PlayerItems.AddItemToBag(me, world, sword);
                    PlayerService.LoadClass(me);
                    PlayerService.LoadSkill(me, world);
                    break;
                case TaskVatLieuTaoGiap:
                    var pants = PlayerItems.NewItem(me.Record.Gender == 0 ? 198 : 197);
                    pants.System = Rng.Next(3) + 1;
                    pants.Options.Add(new PlayerItemOption { Id = 47, Param = 1 });
                    pants.Options.Add(new PlayerItemOption { Id = 6, Param = 5 });
                    PlayerItems.AddItemToBag(me, world, pants);
                    break;
                case TaskHaiThuoc:
                    PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(13, 10));
                    PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(18, 10));
                    break;
                case TaskTimHieu3Truong:
                    var lamp = PlayerItems.NewItem(37);
                    lamp.Expire = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 604800000L;
                    PlayerItems.AddItemToBag(me, world, lamp);
                    break;
            }

            if (task >= 36 && task <= 42)
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(222 + task - 36)); // 1..7 star stones
            if (task < Rewards.GetLength(0))
            {
                PlayerCombat.AddExp(me, world, Rewards[task, 0]);
                PlayerItems.AddYen(me, Rewards[task, 1]);
            }

            // Char.updateTask: next quest
            me.Progress.TaskActive = false;
            me.Progress.TaskIndex = 0;
            me.Progress.TaskCount = 0;
            me.Record.TaskId++;
            // Offline deviation (user request 2026-11-12): NV Bạn hữu tâm giao (11) is removed, the chain goes 10 -> 12.
            // A second TASK_FINISH moves the client's ctaskId past it too.
            bool skip = me.Record.TaskId == TaskBanHuuTamGiao;
            if (skip)
                me.Record.TaskId++;
            PlayerItems.Save(me);
            PlayerService.TaskFinish(me);
            if (skip)
                PlayerService.TaskFinish(me);
        }

        /// <summary>Offline deviation (user request 2026-11-12): NV Bạn hữu tâm giao is removed. A save sitting on it
        /// (before this change) moves on to NV Nâng cấp trang bị at login.</summary>
        public static void SkipRemovedTask(PlayerState me)
        {
            if (me.Record.TaskId != TaskBanHuuTamGiao)
                return;
            me.Record.TaskId = TaskNangCapTrangBi;
            me.Progress.TaskActive = false;
            me.Progress.TaskIndex = 0;
            me.Progress.TaskCount = 0;
        }

        // ---- quest hooks ----
        // Char.updateTaskLevelUp: step 0 of most quests is "reach level N"
        public static void UpdateTaskLevelUp(PlayerState me, GameWorld world)
        {
            var t = Current(me, world);
            if (t != null && me.Progress.TaskIndex == 0 && t.LevelRequire > 0 && me.Level >= t.LevelRequire)
                TaskNext(me);
        }

        // Char.updateTaskKillMonster
        public static void OnKill(PlayerState me, GameWorld world, NsoServer.World.Mob mob)
        {
            var t = Current(me, world);
            if (t == null || t.KillMobs.Length == 0)
                return;
            int idx = me.Progress.TaskIndex;
            if (idx >= t.KillMobs.Length)
                return;
            var k = t.KillMobs[idx];
            if (k[0] == mob.Template.Id && (k[1] == 0 || k[1] == mob.LevelBoss))
                UpdateTaskCount(me, world, 1);
        }

        // Char.updateTaskPickItem
        public static void OnPickItem(PlayerState me, GameWorld world, PlayerItem item, ItemTemplateData it)
        {
            var t = Current(me, world);
            if (t == null || t.PickItems.Length == 0)
                return;
            int idx = me.Progress.TaskIndex;
            if (idx < t.PickItems.Length && t.PickItems[idx] == item.Id)
                UpdateTaskCount(me, world, it.IsUpToUp ? item.Quantity : 1);
        }

        // Char.getIdItemTask: which quest item a monster drops for the current step (-1 none)
        public static int TaskItemFor(PlayerState me, int mobTemplateId)
        {
            if (!me.Progress.TaskActive)
                return -1;
            for (int i = 0; i < TaskDrops.GetLength(0); i++)
                if (TaskDrops[i, 0] == mobTemplateId && TaskDrops[i, 1] == me.Record.TaskId && TaskDrops[i, 2] == me.Progress.TaskIndex)
                    return TaskDrops[i, 3];
            return -1;
        }

        // Char.useEquipment: NV lần đầu dùng kiếm, step "use the weapon"
        public static void OnEquipWeapon(PlayerState me, GameWorld world)
        {
            if (OnStep(me, TaskLanDauDungKiem, 0))
                TaskNext(me);
        }

        // Char.useEquipment: NV nâng cấp trang bị (needs upgraded items; upgrading itself is not ported)
        public static void OnEquipUpgraded(PlayerState me, GameWorld world, PlayerItem item, ItemTemplateData t)
        {
            if (me.Record.TaskId != TaskNangCapTrangBi || !me.Progress.TaskActive || item.Upgrade <= 0)
                return;
            int idx = me.Progress.TaskIndex;
            bool adorn = t.Type == 3 || t.Type == 5 || t.Type == 7 || t.Type == 9;
            bool clothe = t.Type == 0 || t.Type == 2 || t.Type == 4 || t.Type == 6 || t.Type == 8;
            if ((idx == 1 && t.Type == 1) || (idx == 2 && adorn) || (idx == 3 && clothe))
                TaskNext(me);
        }

        // Store.buy: NV diệt sên trừ cóc, step "buy rice balls"
        public static void OnBuyComNam(PlayerState me, GameWorld world, int quantity)
        {
            if (OnStep(me, TaskDietSenTruCoc, 0))
                UpdateTaskCount(me, world, quantity);
        }

        // Char.useItem (food): NV diệt sên trừ cóc, step "eat"
        public static void OnEat(PlayerState me, GameWorld world)
        {
            if (OnStep(me, TaskDietSenTruCoc, 1))
                TaskNext(me);
        }

        // Char.changeMap: NV khám phá xa làng, reach the three places
        public static void OnChangeMap(PlayerState me, GameWorld world, int mapId)
        {
            if (me.Record.TaskId != TaskKhamPha || !me.Progress.TaskActive)
                return;
            int idx = me.Progress.TaskIndex;
            if ((mapId == 2 && idx == 1) || (mapId == 71 && idx == 2) || (mapId == 26 && idx == 3))
                TaskNext(me);
        }

        // Char.learnSkill / upSkill / upPotential: NV gia tăng sức mạnh
        public static void OnLearnBook(PlayerState me)
        {
            if (OnStep(me, TaskGiaTangSucManh, 0))
            {
                TaskNext(me);
                TaskNext(me);
            }
        }

        public static void OnSkillUp(PlayerState me)
        {
            if (OnStep(me, TaskGiaTangSucManh, 1))
                TaskNext(me);
        }

        public static void OnPotentialUp(PlayerState me)
        {
            if (OnStep(me, TaskGiaTangSucManh, 2))
                TaskNext(me);
        }

        // ================================================================ quiz (NV kiến thức, NV bài học vào trường)
        // Char.questionAndAnswer
        private static void QuestionAndAnswer(PlayerState me)
        {
            string q = "";
            int npc = Tabemono;
            me.Menus.Clear();
            Action<int, string> add = (k, s) => me.Menus.Add(new MenuEntry(k, s));
            int task = me.Record.TaskId, idx = me.Progress.TaskIndex;
            const int A = MenuEntry.A, B = MenuEntry.B, C = MenuEntry.C, D = MenuEntry.D;
            if (task == TaskKienThuc)
            {
                switch (me.NAnswer)
                {
                    case 0:
                        q = "Trình độ tối đa có thể đạt được là bao nhiêu?";
                        add(B, "Cấp 130");
                        add(A, "Cấp 150");
                        add(C, "Cấp 99");
                        break;
                    case 1:
                        q = "Trình độ bao nhiêu mới có thể khám phá Vùng đất ma quỷ??";
                        add(A, "Cấp 50");
                        add(C, "Cấp 60");
                        add(B, "Cấp 100");
                        break;
                    case 2:
                        q = "Thức ăn dùng để làm gì?";
                        add(A, "Ăn cho ngon");
                        add(B, "Hồi phục thể lực");
                        add(C, "Để phí tiền");
                        break;
                    case 3:
                        q = "Khám phá làng cổ cần những điều kiện gì?";
                        add(A, "Cấp 70");
                        add(D, "Tất cả");
                        add(B, "Có cổ lệnh");
                        add(C, "Không có điểm hiếu chiến");
                        break;
                    case 4:
                        q = "Muốn tinh luyện trang bị cần phải chuẩn bị gì?";
                        add(A, "Trang bị cấp 12");
                        add(D, "Tất cả");
                        add(B, "Tinh thạch");
                        add(C, "Xu hoặc yên");
                        break;
                }
            }
            else if (task == TaskBaiHocVaoTruong)
            {
                if (idx == 1)
                {
                    npc = Tabemono;
                    switch (me.NAnswer)
                    {
                        case 0:
                            q = "Con có thể trao đổi buôn bán những loại vật phẩm nào?";
                            add(A, "Tiền yên và vật phẩm khoá");
                            add(B, "Tiền xu và vật phẩm không khoá");
                            add(C, "Tiền yên, xu và vật phẩm không khoá");
                            break;
                        case 1:
                            q = "Có bao nhiều trường học quanh đây?";
                            add(B, "2 trường");
                            add(A, "3 trường");
                            add(C, "4 trường");
                            break;
                        case 2:
                            q = "Tổng cộng có bao nhiêu lớp học?";
                            add(A, "3 lớp");
                            add(B, "6 lớp");
                            add(C, "12 lớp");
                            break;
                        case 3:
                            q = "Nội công bao gồm những lớp nào?";
                            add(A, "Kiếm, phi tiêu, quạt");
                            add(B, "Phi tiêu, quạt, cung");
                            add(C, "Kunai, cung, đao");
                            break;
                        case 4:
                            q = "Ngoại công bao gồm những lớp nào?";
                            add(A, "Phi tiêu, đao, cung");
                            add(C, "Kiếm, kunai, đao");
                            add(B, "Quạt, kiếm, kunai");
                            break;
                    }
                }
                else if (idx == 6)
                {
                    npc = Kamakura;
                    switch (me.NAnswer)
                    {
                        case 0:
                            q = "Hiện tại con đang có bao nhiêu ô trong hành trang?";
                            add(A, "10 ô");
                            add(B, "20 ô");
                            add(C, "30 ô");
                            break;
                        case 1:
                            q = "Lưu toạ độ mặc định nhằm mục đích gì?";
                            add(B, "Nơi xuất hiện khi đăng nhập");
                            add(C, "Nơi đứng của người giữ rương");
                            add(A, "Là nơi về khi bị thương");
                            break;
                        case 2:
                            q = "Có mấy loại tiền tệ ở thế giới Ninja này?";
                            add(A, "1 loại");
                            add(B, "2 loại");
                            add(C, "3 loại");
                            break;
                        case 3:
                            q = "Tiền yên kiếm được bằng cách nào?";
                            add(A, "Làm nhiệm vụ, tham gia hoạt động");
                            add(B, "Nhặt được khi đánh quái");
                            add(C, "Cả 2 trường hợp");
                            break;
                        case 4:
                            q = "Loại tiền nào có thể chuyển đổi qua lại giữa các người chơi?";
                            add(A, "Tiền yên");
                            add(C, "Tiền lượng");
                            add(B, "Tiền xu");
                            break;
                    }
                }
                else if (idx == 11)
                {
                    npc = Kenshinto;
                    switch (me.NAnswer)
                    {
                        case 0:
                            q = "Con có biết khi nâng cấp trang bị cần những gì không?";
                            add(A, "Đá + yên hoặc xu");
                            add(B, "Đá");
                            add(C, "Yên hoặc xu");
                            break;
                        case 1:
                            q = "Tách trang bị sau khi nâng cấp sẽ nhận được gì?";
                            add(B, "50% Đá đã ép vào");
                            add(A, "Xu + 100% Đá khi ép vào");
                            add(C, "Không được gì cả");
                            break;
                        case 2:
                            q = "Trang bị được nâng cấp tối đa lên cấp mấy?";
                            add(C, "Cấp 16");
                            add(A, "Cấp 12");
                            add(B, "Cấp 14");
                            break;
                        case 3:
                            q = "Trang bị căn bản có mấy loại?";
                            add(C, "3 loại");
                            add(A, "1 loại");
                            add(B, "2 loại");
                            break;
                        case 4:
                            q = "Sau khi nâng cấp trang bị sẽ như thế nào";
                            add(B, "Lên cấp và chỉ số cao hơn");
                            add(A, "Không thay đổi");
                            add(C, "Trông đẹp hơn");
                            break;
                    }
                }
            }

            PlayerService.OpenUiConfirm(me, npc, q);
        }

        // CMDMenu.A..D in Char.menu: an answer counts only when it is the right one for the current question
        private static void Answer(PlayerState me, int kind)
        {
            int n = me.NAnswer, task = me.Record.TaskId, idx = me.Progress.TaskIndex;
            bool ok = false;
            if (task == TaskKienThuc)
            {
                ok = (kind == MenuEntry.A && (n == 0 || n == 5)) || (kind == MenuEntry.B && n == 2) || (kind == MenuEntry.C && (n == 1 || n == 6)) || (kind == MenuEntry.D && (n == 3 || n == 4));
            }
            else if (task == TaskBaiHocVaoTruong)
            {
                if (idx == 1)
                    ok = (kind == MenuEntry.A && n == 1) || (kind == MenuEntry.B && (n == 0 || n == 2 || n == 3)) || (kind == MenuEntry.C && n == 4);
                else if (idx == 6)
                    ok = (kind == MenuEntry.A && n == 1) || (kind == MenuEntry.B && n == 4) || (kind == MenuEntry.C && (n == 0 || n == 2 || n == 3));
                else if (idx == 11)
                    ok = (kind == MenuEntry.A && n == 0) || (kind == MenuEntry.B && (n == 1 || n == 4)) || (kind == MenuEntry.C && (n == 2 || n == 3));
            }

            if (ok)
                me.NAnswer++;
        }

        // ================================================================ NPC functions
        private static void Add(PlayerState me, string name, Action run)
        {
            me.Menus.Add(new MenuEntry(MenuEntry.Execute, name, run));
        }

        // a sub-menu opened from a menu entry
        private static void SubMenu(PlayerState me, params object[] nameThenAction)
        {
            me.Menus.Clear();
            for (int i = 0; i + 1 < nameThenAction.Length; i += 2)
                Add(me, (string)nameThenAction[i], (Action)nameThenAction[i + 1]);
            PlayerService.OpenUiMenu(me);
        }

        // "Nói chuyện" entry that also completes a "talk to X" step of NV chào làng
        private static void NpcTalk(PlayerState me, int npc, int task, int index, params string[] talk)
        {
            Add(me, "Nói chuyện", () =>
            {
                if (OnStep(me, task, index))
                    TaskNext(me);
                PlayerService.NpcChat(me, npc, Pick(talk));
            });
        }

        // Char.npcKenshinto: the forge. UI 10 = upgrade, 31 = careful upgrade (luong), 12/11 = crystal merge (yen / xu),
        // 13 = split. "Chuyển hóa" (33) and "Luyện ngọc" (46) are not ported.
        private static void NpcKenshinto(PlayerState me)
        {
            Add(me, "Nâng cấp", () => SubMenu(me, "Thường", (Action)(() => PlayerService.OpenUi(me, 10)), "Cẩn thận", (Action)(() => PlayerService.OpenUi(me, 31)), "Hướng dẫn", (Action)(() => PlayerService.NpcChat(me, Kenshinto, "Bỏ Trang bị và Đá vào trong khung để nâng cấp, Khi nâng cấp cẩn thận thì phải có lượng."))));
            Add(me, "Luyện đá", () => SubMenu(me, "Yên", (Action)(() => PlayerService.OpenUi(me, 12)), "Xu", (Action)(() => PlayerService.OpenUi(me, 11))));
            Add(me, "Tách", () => PlayerService.OpenUi(me, 13));
            Add(me, "Chuyển hóa", () => PlayerService.OpenUi(me, 33)); // Char.npcKenshinto: UI 33
            NpcTalk(me, Kenshinto, TaskChaoLang, 2, "Bạn muốn cải tiến trang bị?", "Nâng cấp trang bị: Uy tín, giá cả phải chăng");
        }

        private static void NpcKiriko(PlayerState me)
        {
            Add(me, "Dược phẩm (Yên)", () => PlayerService.OpenUi(me, ShopPotionLock));
            Add(me, "Dược phẩm (Xu)", () => PlayerService.OpenUi(me, ShopPotion));
            NpcTalk(me, Kiriko, TaskChaoLang, 0, "Đi đường cần mang ít dược phẩm", "Không mang theo HP, Hp bên mình, con sẽ gặp nguy hiểm.");
        }

        private static void NpcTabemono(PlayerState me)
        {
            Add(me, "Thức ăn (Yên)", () => PlayerService.OpenUi(me, ShopFoodLock));
            Add(me, "Thức ăn (Xu)", () => PlayerService.OpenUi(me, ShopFood));
            NpcTalk(me, Tabemono, TaskChaoLang, 1, "Thức ăn của ta là ngon nhất rồi!", "Ăn xong đảm bảo bạn sẽ quay lại lần sau.");
        }

        private static void NpcKamakura(PlayerState me)
        {
            Add(me, "Mở rương", () => SubMenu(me, "Mở rương", (Action)(() => PlayerStorage.OpenBox(me, me.Session.Server.World)), "Mở giới hạn xu", (Action)(() => PlayerLuong.RaiseXuMax(me, items => SubMenu(me, items)))));
            Add(me, "Lưu tọa độ", () =>
            {
                if (IsVillageOrSchool(me.MapId))
                {
                    me.Progress.SaveMap = me.MapId;
                    PlayerItems.Save(me);
                    PlayerService.NpcChat(me, Kamakura, "Lưu toạ độ thành công.");
                }
            });
            NpcTalk(me, Kamakura, TaskChaoLang, 4, "Hãy an tâm giao đồ cho ta nào!", "Ta giữ đồ chưa hề để thất lạc bao giờ.");
        }

        private static void NpcUmayaki(PlayerState me, GameWorld world)
        {
            NpcTalk(me, Umayaki, TaskChaoLang, 5, "Nhà bạn muốn đi đâu?", "Ngựa của ta rất khỏe có thể chạy ngàn dặm.", "Đi xe kéo của ta an toàn số một.");
            if (!IsVillage(me.MapId))
                return;
            foreach (var dest in UmayakiVillages)
            {
                int map = dest.Key;
                Add(me, dest.Value, () => Teleport(me, world, map));
            }
        }

        /// <summary>Char.npcUmayaki / npcUmayaki_2 carriage destinations (map, menu label), in menu order.</summary>
        public static readonly KeyValuePair<int, string>[] UmayakiVillages =
        {
            new KeyValuePair<int, string>(10, "Làng Kojin"),
            new KeyValuePair<int, string>(17, "Làng Sanzu"),
            new KeyValuePair<int, string>(22, "Làng Tone"),
            new KeyValuePair<int, string>(32, "Làng chài"),
            new KeyValuePair<int, string>(38, "Làng Chakumi"),
            new KeyValuePair<int, string>(43, "Làng Echigo"),
            new KeyValuePair<int, string>(48, "Làng Oshin")
        };
        public static readonly KeyValuePair<int, string>[] UmayakiSchools =
        {
            new KeyValuePair<int, string>(1, "Trường Hirosaki"),
            new KeyValuePair<int, string>(27, "Trường Haruna"),
            new KeyValuePair<int, string>(72, "Trường Ookaza")
        };
        public const int NpcUmayakiId = Umayaki, NpcUmayaki2Id = Umayaki2;
        public static bool IsVillageMap(int map)
        {
            return IsVillage(map);
        }

        public static bool IsSchoolMap(int map)
        {
            return IsVillageOrSchool(map) && !IsVillage(map);
        }

        // Char.npcUmayaki_2: the carriage at the three schools
        private static void NpcUmayaki2(PlayerState me, GameWorld world)
        {
            if (!IsVillageOrSchool(me.MapId) || IsVillage(me.MapId))
                return; // Char.isSchool
            foreach (var dest in UmayakiSchools)
            {
                int map = dest.Key;
                Add(me, dest.Value, () => Teleport(me, world, map));
            }

            NpcTalk(me, Umayaki2, TaskChaoLang, 5, "Nhà bạn muốn đi đâu?", "Ngựa của ta rất khỏe có thể chạy ngàn dặm.", "Đi xe kéo của ta an toàn số một.");
        }

        // Char.npcTajima. Offline deviations: "[Bật/Tắt] nhận lời mời" (invites from other players) and "Thứ thân"
        // (switch back from a clone) are left out, there are no other players and no clones offline. The offline
        // "Nhận kiếm gỗ" entry stays, but gives a sword only when the character has none (it was unlimited).
        private static void NpcTajima(PlayerState me, GameWorld world)
        {
            Add(me, "Hủy vật phẩm và nhiệm vụ", () => ClearTask(me, world));
            Add(me, "Nói chuyện", () =>
            {
                var t = Current(me, world);
                if (t != null)
                    PlayerService.NpcChat(me, Tajima, "Con đang làm nhiệm vụ " + t.Name + ", hãy chọn Menu/Nhiệm vụ để biết mình đang làm tới đâu.");
                else
                    PlayerService.NpcChat(me, Tajima, Pick("Đi thưa, về trình, nhé các con", "Làng Tone là ngôi làng cổ xưa, đã có từ rất lâu.", "Ta là Tajima, mọi việc ở đây đều do ta quản lý"));
            });
            Add(me, "Nhận kiếm gỗ", () =>
            {
                if (HasItem(me, WoodenSword))
                {
                    PlayerService.NpcChat(me, Tajima, "Con đã có kiếm gỗ rồi.");
                    return;
                }

                var sword = PlayerItems.NewItem(WoodenSword, 1, false);
                sword.Options.Add(new PlayerItemOption { Id = 0, Param = 10 });
                sword.Options.Add(new PlayerItemOption { Id = 9, Param = 10 });
                if (!PlayerItems.AddItemToBag(me, world, sword))
                    PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
            });
        }

        private const int WoodenSword = 194, DaCap9 = 8; // ItemName: wooden sword, DA_CAP_9
        // the item is in the bag, the box or worn
        private static bool HasItem(PlayerState me, int id)
        {
            var inv = me.Inventory;
            foreach (var a in new[]
            {
                inv.Bag,
                inv.Box,
                inv.Equipment
            }

            )
                if (a != null)
                    foreach (var it in a)
                        if (it != null && it.Id == id)
                            return true;
            return false;
        }

        /// <summary>Char.clearTask (Tajima "Hủy vật phẩm và nhiệm vụ"): from level 10, drops the quest in progress
        /// (it can be taken again from its giver, from step 0) and every quest item (types 23, 24, 25) in the bag.
        /// The client does the same on CLEAR_TASK (NOT_MAP -98, Char.clearTask).</summary>
        public static void ClearTask(PlayerState me, GameWorld world)
        {
            if (me.Level < 10)
            {
                PlayerService.NpcChat(me, Tajima, "Trình độ của con không đạt cấp 10 nên không được sử dụng chức năng này");
                return;
            }

            var bag = me.Inventory.Bag;
            // removeItemBags(DA_CAP_9, 1): the stone given for NV 24 step 2
            if (me.Record.TaskId == 24 && me.Progress.TaskActive && me.Progress.TaskIndex == 2)
            {
                int idx = -1;
                for (int i = 0; i < bag.Length; i++)
                    if (bag[i] != null && bag[i].Id == DaCap9)
                        idx = i;
                if (idx >= 0)
                    PlayerItems.RemoveItem(me, idx, 1, true);
            }

            if (Escort.IsLeading(me))
                Escort.Fail(me, true, Escort.FailOther); // the escort belongs to the dropped quest
            me.Progress.TaskActive = false; // taskMain = null
            me.Progress.TaskIndex = 0;
            me.Progress.TaskCount = 0;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] == null)
                    continue;
                var t = world.FindItem(bag[i].Id);
                if (t != null && (t.Type == 25 || t.Type == 23 || t.Type == 24))
                    bag[i] = null;
            }

            PlayerItems.Save(me);
            var m = new Message(Cmd.NOT_MAP); // Service.clearTask
            m.Writer.WriteSByte(-98);
            me.Session.Send(m);
            PlayerLog.Event(me, "quest " + me.Record.TaskId + " and quest items dropped (Tajima)");
            PlayerService.NpcChat(me, Tajima, "Tất cả vật phẩm nhiệm vụ và nhiệm vụ hiện tại trên người con đã được hủy.");
        }

        // Toyotomi / Ookamesama / Kazeto
        private static void NpcTeacher(PlayerState me, GameWorld world, int npc, int class1, string name1, int class2, string name2, string talk)
        {
            Add(me, "Nhập học", () => SubMenu(me, name1, (Action)(() => Admission(me, world, class1, npc)), name2, (Action)(() => Admission(me, world, class2, npc))));
            // Char.initMenu (NV_THACH_DAU, isSchool): "Vào phòng thi đấu". Offline: only on this headmaster's step.
            if (IsVillageOrSchool(me.MapId) && !IsVillage(me.MapId) && OnStep(me, Gymnasium.TaskThachDau, Gymnasium.StepOf(npc)))
                Add(me, "Vào phòng thi đấu", () => Gymnasium.Enter(me, world, npc));
            PlayerLuong.TeacherMenu(me, world, npc, (n, a) => Add(me, n, a), items => SubMenu(me, items));
            Add(me, "Nói chuyện", () =>
            {
                PlayerService.NpcChat(me, npc, talk);
                int step = npc == Toyotomi ? 1 : npc == Ookamesama ? 2 : 3;
                if (OnStep(me, TaskTimHieu3Truong, step))
                    TaskNext(me);
            });
        }

        private static void NpcKanata(PlayerState me)
        {
            Add(me, "Binh khí", () => PlayerService.OpenUi(me, ShopWeapon));
            Add(me, "Nói chuyện", () => PlayerService.NpcChat(me, Kanata, "Vũ khí tốt sẽ giúp con mạnh hơn."));
        }

        private static void NpcFuroya(PlayerState me)
        {
            bool male = me.Record.Gender == 1;
            Add(me, "Nón", () => PlayerService.OpenUi(me, male ? 20 : 21));
            Add(me, "Áo", () => PlayerService.OpenUi(me, male ? 22 : 23));
            Add(me, "Găng tay", () => PlayerService.OpenUi(me, male ? 24 : 25));
            Add(me, "Quần", () => PlayerService.OpenUi(me, male ? 26 : 27));
            Add(me, "Giày", () => PlayerService.OpenUi(me, male ? 28 : 29));
        }

        private static void NpcAmeji(PlayerState me)
        {
            Add(me, "Liên", () => PlayerService.OpenUi(me, ShopNecklace));
            Add(me, "Nhẫn", () => PlayerService.OpenUi(me, ShopRing));
            Add(me, "Ngọc bội", () => PlayerService.OpenUi(me, ShopPearl));
            Add(me, "Phù", () => PlayerService.OpenUi(me, ShopSpell));
        }

        private static void NpcGoosho(PlayerState me)
        {
            Add(me, "Cửa hàng", () => PlayerService.OpenUi(me, ShopMisc));
            Add(me, "Quầy sách", () => PlayerService.OpenUi(me, ShopBook));
            Add(me, "Thời trang", () => PlayerService.OpenUi(me, ShopFashion));
        }

        // villages (Char.isVillage) and schools (Char.isSchool)
        private static bool IsVillage(int map)
        {
            return map == 10 || map == 17 || map == 22 || map == 32 || map == 38 || map == 43 || map == 48;
        }

        private static bool IsVillageOrSchool(int map)
        {
            return IsVillage(map) || map == 1 || map == 27 || map == 72;
        }

        // NinjaUtils.getXY: where a teleport to a village or school puts the character
        public static short[] VillageXY(int map)
        {
            switch (map)
            {
                case 22:
                    return new short[]
                    {
                        228,
                        192
                    };
                case 10:
                    return new short[]
                    {
                        109,
                        264
                    };
                case 17:
                    return new short[]
                    {
                        1480,
                        264
                    };
                case 32:
                    return new short[]
                    {
                        2502,
                        384
                    };
                case 38:
                    return new short[]
                    {
                        397,
                        336
                    };
                case 43:
                    return new short[]
                    {
                        2529,
                        240
                    };
                case 48:
                    return new short[]
                    {
                        140,
                        432
                    };
                case 1:
                    return new short[]
                    {
                        193,
                        384
                    };
                case 27:
                    return new short[]
                    {
                        647,
                        408
                    };
                case 72:
                    return new short[]
                    {
                        1611,
                        672
                    };
                default:
                    return new short[]
                    {
                        35,
                        100
                    };
            }
        }

        // Char.teleport
        private static void Teleport(PlayerState me, GameWorld world, int map)
        {
            if (!MapUnlock.CanEnter(me.Record.TaskId, map))
            {
                PlayerService.ServerDialog(me, "Bạn chưa thể đến khu vực này. Hãy hoàn thành nhiệm vụ trước.");
                return;
            }

            var xy = VillageXY(map);
            me.SetXY(xy[0], xy[1]);
            me.Actions.ChangeMap(map);
        }

        // Char.admission: join a school (class 1..6), get its first skill book and weapon
        private static void Admission(PlayerState me, GameWorld world, int cls, int npc)
        {
            if (me.Record.ClassId != 0)
            {
                PlayerService.NpcChat(me, npc, "Con đã vào lớp từ trước rồi mà!");
                return;
            }

            if (!(me.Record.TaskId >= TaskGiaTangSucManh && !me.Progress.TaskActive && me.Level >= 10))
            {
                PlayerService.NpcChat(me, npc, "Con vẫn chưa đủ điều kiện để vào lớp (trình độ từ cấp 10 và làm xong nhiệm vụ tìm hiểu trường");
                return;
            }

            if (me.Inventory.Equipment[1] != null)
            {
                PlayerService.NpcChat(me, npc, "Con hãy gỡ bỏ tạp niệm bằng cách cất vũ khí đang sử dụng vào hành trang trước rồi hãy vào lớp học của ta.");
                return;
            }

            if (PlayerItems.SlotNull(me) < 2)
            {
                PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
                return;
            }

            me.Record.ClassId = (sbyte)cls;
            me.SkillPoint = me.Level - 9;
            me.PotentialPoint = me.Level * 10;
            for (int lv = 70; lv <= 100; lv += 10)
                if (me.Level >= lv)
                    me.PotentialPoint += (me.Level - lv) * 10;
            // Char.selectClass: keep the original saved base; tuning changes only effective stats.
            bool outer = cls == 1 || cls == 3 || cls == 5;
            Array.Copy(CharacterProgressionRules.OriginalPotential(cls), me.Potential, 4);
            me.Progress.Skills.Clear();
            me.SelectedSkill = null;
            me.Progress.OSkill = new sbyte[]
            {
                -1,
                -1,
                -1,
                -1,
                -1
            }; // Char.selectClass
            me.Progress.KSkill = new sbyte[]
            {
                -1,
                -1,
                -1
            };
            me.Progress.CSkill = new sbyte[0];
            PlayerService.SkillShortcut(me, "OSkill", 5);
            PlayerService.SkillShortcut(me, "KSkill", 3);
            PlayerService.SkillShortcut(me, "CSkill", 0);
            string school = npc == Kazeto ? "Haruna" : npc == Toyotomi ? "Hirosaki" : "Ookaza";
            PlayerService.NpcChat(me, npc, "Chào mừng con đến với trường " + school + ". Con hãy sử dụng vũ khí và đọc sách võ công mà ta tặng (mở Menu/Bản thân/Hành trang) để bước đầu chuẩn bị cho việc học tập");
            // first skill book (id 40/49/58/67/76/85) and the school weapon
            int[] books =
            {
                0,
                40,
                49,
                58,
                67,
                76,
                85
            }, weapons =
            {
                0,
                94,
                114,
                99,
                109,
                104,
                119
            }, sys =
            {
                0,
                1,
                1,
                2,
                2,
                3,
                3
            };
            PlayerCombat.SetAbility(me, world);
            PlayerService.LoadClass(me);
            PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(books[cls]));
            var w = PlayerItems.NewItem(weapons[cls]);
            w.System = sys[cls];
            bool inner = !outer;
            w.Options.Add(new PlayerItemOption { Id = 0, Param = 100 });
            w.Options.Add(new PlayerItemOption { Id = 1, Param = 100 });
            w.Options.Add(new PlayerItemOption { Id = inner ? 9 : 8, Param = 10 });
            w.Options.Add(new PlayerItemOption { Id = 10, Param = 5 });
            w.Options.Add(new PlayerItemOption { Id = inner ? 22 : 21, Param = 100 });
            w.Options.Add(new PlayerItemOption { Id = 19, Param = 10 });
            w.Options.Add(new PlayerItemOption { Id = 30, Param = 5 });
            PlayerItems.AddItemToBag(me, world, w);
            // NV gia tăng sức mạnh starts right away
            me.Progress.TaskActive = true;
            me.Progress.TaskIndex = 0;
            me.Progress.TaskCount = -1;
            PlayerItems.Save(me);
            var t = world.FindTask(me.Record.TaskId);
            if (t != null)
                PlayerService.SendTaskInfo(me, t);
        }
    }
}
