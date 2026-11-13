using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math; // the client has its own global Math class
    using Zone = NsoServer.World.Zone;

    // Quest mechanics of the main chain (levels 1-39) that are not plain kill / pick-from-monster / talk steps:
    //   - fixed pick-up nodes that come back 30 s after being taken (Java map/zones/AkaCave + KugyouCave, item Ore / IceCrystal,
    //     visibility from Zone.getItemMaps, pick check from Char.pickItem);
    //   - the empty bottle of "NV Lấy nước hang sâu" (Java Char.useItem, ItemName.BINH_RONG): stand in waterflow in Hang Ha,
    //     wait 3 s, a monster hit cancels it (Mob.attack sets isFailure while isCatchItem).
    public static class TaskMechanics
    {
        public const int TaskThuThapNguyenLieu = 14, TaskLayNuocHangSau = 19, TaskTruyTimDiaDo = 23, TaskTruyTimBaoVat = 24, TaskThuThapTinhTheBang = 26;
        public const int ItemKhoangThach = 212, ItemBinhRong = 219, ItemBinhNuoc = 220, ItemChiaKhoaCoQuan = 231, ItemDiaDo = 232, ItemDiaDo2 = 233, ItemDiaDo4 = 235, ItemTinhTheBang = 236, ItemDaCap9 = 8;
        public const int MapHangAka = 29, MapDaoHebi = 34, MapHangMeiro = 35, MapRungKappa = 52, MapHangHa = 63, MapHangKugyou = 64, MapKheNuiChorochoro = 66, MapDiaDaoChikatoya = 78;
        /// <summary>Time limit of the Dia dao Chikatoya instance (Java ChikatoyaTunnels: 15 * 60 s).</summary>
        public static int TunnelSeconds = 15 * 60;
        /// <summary>Seconds left in the tunnel after the map piece is picked (Java updateTaskPickItem: setTimeMap(3)).</summary>
        public static int TunnelAfterPickSeconds = 3;
        /// <summary>Chance (1 in N) that a dig with a treasure map finds the treasure (Java: nextInt(3) == 0).</summary>
        public static int DigChanceOneIn = 3;
        // Server.initImageMap: where each treasure map (233, 234, 235) points: map, x, y, w, h
        private static readonly int[][] TreasureAreas =
        {
            new[]
            {
                MapRungKappa,
                1746,
                408,
                80,
                50
            },
            new[]
            {
                MapKheNuiChorochoro,
                880,
                320,
                80,
                50
            },
            new[]
            {
                MapDaoHebi,
                180,
                260,
                80,
                40
            },
        };
        private static readonly Random rnd = new Random();
        /// <summary>Seconds before a taken node lies on the ground again (Java AkaCave/KugyouCave.update: 30000 ms).</summary>
        public static int NodeRespawnMs = 30000;
        /// <summary>Waiting time of a "catch" action such as filling the bottle (Java: Thread.sleep(3000)).</summary>
        public static int CatchWaitMs = 3000;
        /// <summary>Empty bottles given when the water quest is taken (Java Char.getTask: 50 BINH_RONG).</summary>
        public static int EmptyBottles = 50;
        // a fixed node: map, quest + step that may see and pick it, item, positions (pixels)
        private sealed class NodeSpec
        {
            public int Map, Task, Step, Item;
            public short[][] Xy;
        }

        private static readonly NodeSpec[] Nodes =
        {
            new NodeSpec
            {
                Map = MapHangAka,
                Task = TaskThuThapNguyenLieu,
                Step = 1,
                Item = ItemKhoangThach,
                Xy = new[]
                {
                    new short[]
                    {
                        530,
                        624
                    },
                    new short[]
                    {
                        900,
                        384
                    },
                    new short[]
                    {
                        1093,
                        192
                    }
                }
            },
            new NodeSpec
            {
                Map = MapHangKugyou,
                Task = TaskThuThapTinhTheBang,
                Step = 1,
                Item = ItemTinhTheBang,
                Xy = new[]
                {
                    new short[]
                    {
                        420,
                        648
                    },
                    new short[]
                    {
                        60,
                        336
                    },
                    new short[]
                    {
                        490,
                        216
                    }
                }
            },
        };
        // per zone: the node entries (kept while picked so they can come back) and the last respawn check
        private sealed class ZoneNodes
        {
            public readonly List<ItemMapEntry> Items = new List<ItemMapEntry>();
            public long LastUpdate;
        }

        private static readonly Dictionary<Zone, ZoneNodes> zoneNodes = new Dictionary<Zone, ZoneNodes>();
        // ---------------------------------------------------------------- fixed nodes
        private static NodeSpec SpecOf(int map)
        {
            foreach (var s in Nodes)
                if (s.Map == map)
                    return s;
            return null;
        }

        // Offline /goto reuses mechanic-owned locations instead of duplicating them in the terminal.
        // Resolves a destination only; picking, filling and digging retain their normal requirements.
        public static bool TryTravelPosition(PlayerState me, GameWorld world, NsoServer.World.TileMap tile, out int x, out int y)
        {
            x = y = -1;
            var spec = SpecOf(tile.Id);
            if (spec != null && me.Record.TaskId == spec.Task && me.Progress.TaskIndex == spec.Step)
            {
                x = spec.Xy[0][0];
                y = spec.Xy[0][1];
                return true;
            }

            if (me.Record.TaskId == TaskTruyTimBaoVat && me.Progress.TaskIndex == 1)
                for (int i = 0; i < TreasureAreas.Length; i++)
                {
                    var area = TreasureAreas[i];
                    if (area[0] == tile.Id && PlayerItems.IndexInBag(me, ItemDiaDo2 + i) >= 0)
                    {
                        x = area[1] + area[3] / 2;
                        y = area[2] + area[4] / 2;
                        return true;
                    }
                }

            if (tile.Id == MapHangHa && me.Record.TaskId == TaskLayNuocHangSau && me.Progress.TaskIndex == 1)
            {
                var grid = world.GetGrid(tile);
                for (int cy = 24; cy < grid.PxH; cy += 24)
                    for (int cx = 36; cx < grid.PxW - 24; cx += 24)
                        if (grid.TileTypeAt(cx, cy - 12, TileGrid.T_WATERFLOW))
                        {
                            x = cx;
                            y = cy;
                            return true;
                        }
            }

            return false;
        }

        /// <summary>Java AkaCave/KugyouCave.init + update: creates the nodes once, then puts back taken ones every 30 s.
        /// Called from the zone tick (under World.SyncRoot).</summary>
        public static void UpdateZone(Zone zone, long now)
        {
            if (zone.IsInstance)
            {
                UpdateInstance(zone, now);
                return;
            }

            var spec = SpecOf(zone.Tile.Id);
            if (spec == null)
                return;
            ZoneNodes zn;
            if (!zoneNodes.TryGetValue(zone, out zn))
            {
                zn = new ZoneNodes
                {
                    LastUpdate = now
                };
                zoneNodes[zone] = zn;
                foreach (var xy in spec.Xy)
                {
                    var it = NewNode(zone, spec, xy);
                    zn.Items.Add(it);
                    zone.ItemMaps.Add(it);
                    AddItemMapVisible(zone, it);
                }

                return;
            }

            if (now - zn.LastUpdate <= NodeRespawnMs)
                return;
            zn.LastUpdate = now;
            foreach (var it in zn.Items)
            {
                if (!it.PickedUp)
                    continue;
                it.Id = zone.NumberDropItem++;
                it.PickedUp = false;
                it.Item = PlayerItems.NewItem(spec.Item, 1, true);
                zone.ItemMaps.Add(it);
                AddItemMapVisible(zone, it);
            }
        }

        private static ItemMapEntry NewNode(Zone zone, NodeSpec spec, short[] xy)
        {
            return new ItemMapEntry
            {
                Id = zone.NumberDropItem++,
                X = xy[0],
                Y = xy[1],
                OwnerId = -1,
                CreatedAt = 0,
                Fixed = true,
                GateTask = spec.Task,
                GateStep = spec.Step,
                Item = PlayerItems.NewItem(spec.Item, 1, true)
            };
        }

        /// <summary>Zone.getItemMaps: a gated item is only shown to a player on its quest step.</summary>
        public static bool CanSee(PlayerState me, ItemMapEntry it)
        {
            if (it.GateTask < 0)
                return true;
            return me.Record.TaskId == it.GateTask && me.Progress.TaskActive && me.Progress.TaskIndex == it.GateStep;
        }

        // MapService.addItemMap, only to the players allowed to see it
        private static void AddItemMapVisible(Zone zone, ItemMapEntry it)
        {
            foreach (PlayerState p in zone.Players)
                if (CanSee(p, it))
                    PlayerService.AddItemMapTo(p, it);
        }

        /// <summary>Char.pickItem: KHOANG_THACH / TINH_THE_BANG only while on their step. Returns false (with Java's
        /// message) when the pick is refused.</summary>
        public static bool CanPick(PlayerState me, ItemMapEntry it)
        {
            if (CanSee(me, it))
                return true;
            PlayerService.ServerMessage(me, it.Item.Id == ItemTinhTheBang ? "Không thể nhặt tinh thể " : "Không thể nhặt khoáng thạch");
            return false;
        }

        /// <summary>After a quest step change: shows / hides the gated items of the player's zone.</summary>
        public static void RefreshVisible(PlayerState me)
        {
            var zone = me.Zone;
            if (zone == null)
                return;
            foreach (var it in zone.ItemMaps)
            {
                if (it.GateTask < 0)
                    continue;
                if (CanSee(me, it))
                    PlayerService.AddItemMapTo(me, it);
                else
                    PlayerService.RemoveItemMapTo(me, it.Id);
            }
        }

        // ---------------------------------------------------------------- quest items used from the bag
        /// <summary>Char.getTask extras: items handed out when a quest is taken.</summary>
        public static void OnTakeTask(PlayerState me, GameWorld world, int taskId)
        {
            if (taskId == TaskLayNuocHangSau)
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(ItemBinhRong, EmptyBottles));
            if (taskId == TaskTruyTimDiaDo)
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(ItemChiaKhoaCoQuan, 1));
            if (taskId == TaskTruyTimBaoVat)
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(ItemDiaDo2 + rnd.Next(3), 1));
        }

        /// <summary>Char.npcMatsurugi: a new key while on the tunnel step (the old one is taken back).</summary>
        public static void GiveNewKey(PlayerState me, GameWorld world)
        {
            int i = PlayerItems.IndexInBag(me, ItemChiaKhoaCoQuan);
            if (i != -1)
                PlayerItems.RemoveItem(me, i, 1, true);
            if (PlayerItems.SlotNull(me) == 0)
            {
                PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
                return;
            }

            PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(ItemChiaKhoaCoQuan, 1));
        }

        public static bool OnTunnelStep(PlayerState me)
        {
            return me.Record.TaskId == TaskTruyTimDiaDo && me.Progress.TaskActive && me.Progress.TaskIndex == 1;
        }

        /// <summary>Char.useItem for quest items. Returns true when the item was handled.</summary>
        public static bool UseTaskItem(PlayerState me, GameWorld world, int index, PlayerItem item)
        {
            if (item.Id == ItemChiaKhoaCoQuan)
            {
                UseKey(me, world, index);
                return true;
            }

            if (item.Id >= ItemDiaDo2 && item.Id <= ItemDiaDo4)
            {
                UseTreasureMap(me, world, item);
                return true;
            }

            if (item.Id != ItemBinhRong)
                return false;
            if (me.CatchUntil > 0)
                return true; // Java: if (isCatchItem) return
            if (me.Record.TaskId != TaskLayNuocHangSau || !me.Progress.TaskActive || me.Progress.TaskIndex != 1)
            {
                PlayerService.ServerDialog(me, "Bạn không làm nhiệm vụ này");
                return true;
            }

            if (me.Zone == null || me.Zone.Tile.Id != MapHangHa)
            {
                PlayerService.ServerDialog(me, "Hãy đến Hang Ha để lấy nước");
                return true;
            }

            var grid = world.GetGrid(me.Zone.Tile);
            if (!grid.TileTypeAt(me.X, me.Y - 12, TileGrid.T_WATERFLOW))
            {
                PlayerService.ServerDialog(me, "Hãy đứng chỗ có nước để lấy");
                return true;
            }

            if (PlayerItems.SlotNull(me) == 0)
            {
                PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
                return true;
            }

            PlayerService.ShowWait(me, me.Zone.Tile.Name);
            StartCatch(me, () =>
            {
                int i = PlayerItems.IndexInBag(me, ItemBinhRong);
                if (i < 0)
                {
                    PlayerService.EndWait(me, "");
                    return;
                }

                PlayerItems.RemoveItem(me, i, 1, true);
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(ItemBinhNuoc, 1));
                PlayerService.EndWait(me, "Bạn nhận được bình nước");
                PlayerTasks.UpdateTaskCount(me, world, 1);
            });
            return true;
        }

        // ---------------------------------------------------------------- NV Truy tìm địa đồ: Địa đạo Chikatoya
        // Char.useItem(CHIA_KHOA_CO_QUAN2): at the gate of Hang Meiro, opens a private tunnel for 15 minutes
        private static void UseKey(PlayerState me, GameWorld world, int index)
        {
            if (me.Zone.Tile.Id != MapHangMeiro || me.Y != 432 || me.X < 1700 || me.X > 1896 || !OnTunnelStep(me))
                return;
            var tunnel = world.CreateInstance(MapDiaDaoChikatoya);
            if (tunnel == null)
                return;
            PlayerItems.RemoveItem(me, index, 1, true);
            tunnel.InstanceEndAt = Now() + TunnelSeconds * 1000L;
            me.X = 35;
            me.Y = 96;
            me.Actions.JoinZone(tunnel);
            SendTimeInMap(me, TunnelSeconds);
        }

        // Service.sendTimeInMap (sub command MAP_TIME -95): the countdown shown on screen
        private static void SendTimeInMap(PlayerState me, int seconds)
        {
            var m = PlayerService.Sub(-95);
            m.Writer.WriteInt(seconds);
            me.Session.Send(m);
        }

        // ChikatoyaTunnels.update / close: drop the map piece when every monster is dead; send everybody back at the end
        private static void UpdateInstance(Zone zone, long now)
        {
            if (!zone.InstanceDropped)
            {
                bool alive = false;
                foreach (var mob in zone.Mobs)
                    if (!mob.IsDead)
                    {
                        alive = true;
                        break;
                    }

                if (!alive)
                {
                    zone.InstanceDropped = true;
                    var it = new ItemMapEntry
                    {
                        Id = zone.NumberDropItem++,
                        X = 220,
                        Y = 96,
                        OwnerId = -1,
                        Fixed = true,
                        Item = PlayerItems.NewItem(ItemDiaDo, 1, true)
                    };
                    zone.ItemMaps.Add(it);
                    PlayerService.AddItemMap(zone, it);
                }
            }

            if (now < zone.InstanceEndAt && zone.Players.Count > 0)
                return;
            foreach (PlayerState p in zone.Players.ToArray())
            {
                // ChikatoyaTunnels.close: back to the saved coordinate (Kamakura's "Lưu tọa độ")
                int home = p.Progress != null ? p.Progress.SaveMap : 22;
                var xy = PlayerTasks.VillageXY(home);
                p.X = xy[0];
                p.Y = xy[1];
                p.Actions.ChangeMap(home);
            }
        }

        /// <summary>Char.updateTaskPickItem: picking the map piece leaves 3 s in the tunnel.</summary>
        public static void OnPicked(PlayerState me, ItemMapEntry it)
        {
            if (it.Item.Id != ItemDiaDo || me.Zone == null || !me.Zone.IsInstance)
                return;
            me.Zone.InstanceEndAt = Now() + TunnelAfterPickSeconds * 1000L;
            SendTimeInMap(me, TunnelAfterPickSeconds);
        }

        // ---------------------------------------------------------------- NV Truy tìm bảo vật: dig with a treasure map
        // Char.useItem(DIA_DO2..4)
        private static void UseTreasureMap(PlayerState me, GameWorld world, PlayerItem item)
        {
            if (me.CatchUntil > 0)
                return;
            if (me.Record.TaskId != TaskTruyTimBaoVat || !me.Progress.TaskActive || me.Progress.TaskIndex != 1)
                return;
            var t = world.FindItem(item.Id);
            string title = t == null ? "Địa đồ" : t.Name;
            var a = TreasureAreas[item.Id - ItemDiaDo2];
            if (me.Zone.Tile.Id != a[0])
            {
                var map = world.FindMap(a[0]);
                PlayerService.ShowAlert(me, title, string.Format("Bạn đang ở rất xa bảo vật. Hãy đi đến khu vực {0} và xem tiếp địa đồ", map == null ? "?" : map.Name));
                return;
            }

            if (me.X < a[1] || me.X > a[1] + a[3] || me.Y < a[2] || me.Y > a[2] + a[4])
            {
                PlayerService.ShowAlert(me, title, "Bảo vật đang ở quanh đây, hãy tìm đúng vị trí trong hình của địa đồ và đào lấy bảo vật.");
                return;
            }

            if (PlayerItems.SlotNull(me) == 0)
            {
                PlayerService.ServerDialog(me, "Hành trang không đủ chỗ trống");
                return;
            }

            int id = item.Id;
            PlayerService.ShowWait(me, me.Zone.Tile.Name);
            StartCatch(me, () =>
            {
                if (rnd.Next(Math.Max(1, DigChanceOneIn)) != 0)
                {
                    PlayerService.EndWait(me, "Bảo vật đang ở đây, hãy đào sâu hơn nữa.");
                    return;
                }

                int i = PlayerItems.IndexInBag(me, id);
                if (i != -1)
                    PlayerItems.RemoveItem(me, i, 1, true);
                PlayerTasks.TaskNext(me);
                PlayerItems.AddItemToBag(me, world, PlayerItems.NewItem(ItemDaCap9, 1));
                PlayerService.EndWait(me, "Bạn nhận được bảo vật.");
            });
        }

        /// <summary>Service.itemInfo for DIA_DO2..4: ImageMap.getData, the area of the map drawn from its tiles at the
        /// client's zoom (TilePicture, port of TileMap.createImage).</summary>
        public static byte[] TreasureImage(GameWorld world, int itemId, int zoom)
        {
            var a = TreasureAreas[itemId - ItemDiaDo2];
            var tile = world.FindMap(a[0]);
            if (tile == null)
                return new byte[0];
            return TilePicture.Create(world, tile, zoom, a[1], a[2], a[3], a[4]);
        }

        // ---------------------------------------------------------------- timed "catch" actions
        /// <summary>Starts a timed action (Java isCatchItem + Thread.sleep); a monster hit before the end cancels it.</summary>
        public static void StartCatch(PlayerState me, Action done)
        {
            me.CatchUntil = Now() + CatchWaitMs;
            me.CatchFailed = false;
            me.CatchDone = done;
        }

        /// <summary>Mob.attack: a hit while catching makes the action fail.</summary>
        public static void OnHit(PlayerState me)
        {
            if (me.CatchUntil > 0)
                me.CatchFailed = true;
        }

        /// <summary>Half-second tick: finishes a timed action.</summary>
        public static void UpdatePlayer(PlayerState me, long now)
        {
            if (me.CatchUntil <= 0 || now < me.CatchUntil)
                return;
            var done = me.CatchDone;
            bool failed = me.CatchFailed || me.IsDead;
            me.CatchUntil = 0;
            me.CatchDone = null;
            me.CatchFailed = false;
            if (failed)
                PlayerService.EndWait(me, "");
            else if (done != null)
                done();
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
