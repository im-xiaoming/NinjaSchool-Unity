using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    // the Unity client also defines global types with these names, so pick the server ones explicitly
    using TileMap = NsoServer.World.TileMap;
    using Zone = NsoServer.World.Zone;
    using Mob = NsoServer.World.Mob;

    // What the server tracks about the character that is currently playing (the runtime part of Java's `Char`).
    // com.nsoz.model.Menu: one entry of a server-built NPC menu
    public sealed class MenuEntry
    {
        public const int Execute = 0, TakeTask = 1, ConfirmTakeTask = 2, FinishTask = 3, DoTask = 4, A = 5, B = 6, C = 7, D = 8, Cancel = 9;
        public int Kind;
        public string Name;
        public System.Action Run;
        public MenuEntry(int kind, string name, System.Action run = null) { Kind = kind; Name = name; Run = run; }
    }

    // com.nsoz.effect.Effect on a character: template id, its type (`effect` table), time span and strength
    public sealed class PlayerEffect
    {
        public int TemplateId, Type, Param;
        public long StartAt, EndAt;
        public bool IsExpired(long now) { return now >= EndAt; }
    }

    public sealed class PlayerState
    {
        public PlayerRecord Record;
        public Session Session;
        public PlayerActions Actions;
        public Zone Zone;
        public short X, Y, PreX, PreY;
        public int Speed = 5;
        public bool IsDead;
        public bool IsChangeMap;
        public long TimeChangeMap;

        // Starting values of a level-1 character; they come from equipment, level and potential once those are ported.
        public int Hp = 50, MaxHp = 50, Mp = 100, MaxMp = 100;
        public long Exp, ExpDown;
        public sbyte TypePk;
        // damage reduction / resistances used when monsters hit the character
        public int DameDown, Miss, ResFire, ResIce, ResWind;
        public readonly int[] Options = new int[256];
        public bool IsInvisible;

        // Combat (see PlayerCombat): level from Exp, potential points, the selected skill and the attack values of
        // AbilityFromEquip.setAbility.
        public int Level = 1;
        public int PotentialPoint, SkillPoint;
        public readonly int[] Potential = { 15, 5, 5, 5 };
        public NsoServer.World.SkillData SelectedSkill;
        public long LastTimeUseSkill;            // Skill.lastTimeUseThisSkill of the selected skill
        public int Damage, Damage2, Exactly, Fatal, FatalDame, PercentFatalDame;
        public NsoServer.Store.PlayerInventory Inventory;
        public NsoServer.Store.PlayerProgress Progress;
        public int[] SupportOptions;                       // Char.optionsSupportSkill (passive skills), set by SetAbility   // yen, xu, quest step, skills (saved in PlayerRecord.Data)

        // Char.menus: the NPC menu currently shown (server-built, see PlayerTasks), and the quiz answer counter (Char.nAnswer)
        public readonly List<MenuEntry> Menus = new List<MenuEntry>();
        public int NAnswer;

        // Char.em: the character's effects (food, potions...), see PlayerEffects
        public readonly List<PlayerEffect> Effects = new List<PlayerEffect>();
        public long LastRegen;   // last natural HP/MP regeneration (offline addition, see PlayerEffects)

        public int MapId { get { return Zone.Tile.Id; } }
        public void SetXY(short x, short y) { X = x; Y = y; }
    }

    // Port of Zone.move / Char.isCrossMap / Zone.requestChangeMap / Char.changeMap / Char.changeZone and the zone list.
    // Everything is single-player: the replies other players would get are not needed.
    public sealed class PlayerActions
    {
        private const int LimitSpeed = 17;
        private static readonly System.Random Rng = new System.Random();

        private readonly Session session;
        private readonly GameWorld world;
        private readonly PlayerState me;

        public PlayerActions(Session session, GameWorld world, PlayerState me)
        {
            this.session = session;
            this.world = world;
            this.me = me;
            me.Actions = this;
            me.Session = session;
        }

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        // the character these actions belong to (used by PlayerCombat)
        public PlayerState State { get { return me; } }

        // Char.move -> Zone.move
        public void Move(Message m)
        {
            short x = m.Reader.ReadShort();
            short y = m.Reader.ReadShort();
            short preX = me.X, preY = me.Y;
            if (me.X == x && me.Y == y) { ResetPoint(); return; }
            if (System.Math.Abs(x - preX) > me.Speed * LimitSpeed) { ResetPoint(); return; }
            me.PreX = preX;
            me.PreY = preY;
            me.SetXY(x, y);
            // Offline deviation from the original: touching a wood/rock tile or the side edge of the map only puts the
            // character back where it was (the original kills it). Only falling out of the bottom of the map still kills.
            if (!me.IsChangeMap && IsSoftBlock(me.X, me.Y))
            {
                me.X = preX;
                me.Y = preY;
                ResetPoint();
                return;
            }
            if (IsCrossMap(me.X, me.Y) && !me.IsChangeMap)
            {
                // walking off the edge of the map kills the character
                me.X = preX;
                me.Y = preY;
                StartDie();
                return;
            }
            var ms = new Message(Cmd.PLAYER_MOVE);
            ms.Writer.WriteInt(me.Record.Id);
            ms.Writer.WriteShort(me.X);
            ms.Writer.WriteShort(me.Y);
            session.Send(ms);
        }

        private bool IsSoftBlock(short cx, short cy)
        {
            if (Now() - me.TimeChangeMap < 500 || me.IsDead) return false;
            var tile = me.Zone.Tile;
            var grid = world.GetGrid(tile);
            if (grid.TileTypeAt(cx, cy - 1, TileGrid.T_WATERFLOW)) return false;
            if (grid.IsWood(cx, cy - 1) || grid.IsRock(cx, cy - 1)) return true;
            return (cx < 24 || cx > grid.PxW - 24) && !tile.IsInWaypoint(cx, cy);
        }

        private bool IsCrossMap(short cx, short cy)
        {
            if (Now() - me.TimeChangeMap < 500) return false;
            if (me.IsDead) return false;
            var tile = me.Zone.Tile;
            var grid = world.GetGrid(tile);
            if (grid.TileTypeAt(cx, cy - 1, TileGrid.T_WATERFLOW)) return false;
            if (grid.TileId == 4 && cy >= grid.PxH - 48) return true;
            if (grid.IsWood(cx, cy - 1)) return true;
            if (grid.IsRock(cx, cy - 1)) return true;
            if ((cx < 24 || cx > grid.PxW - 24) && !tile.IsInWaypoint(cx, cy)) return true;
            if (cy >= grid.PxH - 24 && !tile.IsInWaypoint(cx, cy)) return true;
            return false;
        }

        // Zone.requestChangeMap
        public void RequestChangeMap()
        {
            if (me.IsDead) return;
            var wp = me.Zone.Tile.FindWaypoint(me.X, me.Y);
            if (wp == null) return;
            me.TimeChangeMap = Now();
            int next = wp.Next;
            if (next == -1)
            {
                // the "random" portal sends the player to one of four fixed spots
                int[] ids = { 134, 135, 136, 137 };   // NUI_DORAGON, RUNG_MAJO, VUC_YUNIKOON, DONG_KINGU
                next = ids[Rng.Next(ids.Length)];
                short x = 0, y = 0;
                switch (next)
                {
                    case 137: x = 45; y = 264; break;
                    case 136: x = 1011; y = 1128; break;
                    case 135: x = 2595; y = 240; break;
                    case 134: x = 1011; y = 720; break;
                }
                me.SetXY(x, y);
                ChangeMap(next);
                return;
            }
            if (MapUnlock.CanEnter(me.Record.TaskId, next))
            {
                me.SetXY(wp.X, wp.Y);
                ChangeMap(next);
            }
            else
            {
                me.SetXY(me.PreX, me.PreY);
                ResetPoint();
                Dialog("Bạn chưa thể đến khu vực này. Hãy hoàn thành nhiệm vụ trước.");
            }
        }

        // Char.changeMap
        public void ChangeMap(int id)
        {
            var tile = world.FindMap(id);
            if (tile == null) { Dialog("Không tìm thấy nơi này!"); return; }
            if (!tile.IsNormal) { Dialog("Khu vực này chưa được hỗ trợ ở bản offline."); return; }
            int zoneId = world.RandomZoneId(id);
            if (zoneId == -1) { Dialog("Không tìm thấy nơi này!"); return; }
            me.IsChangeMap = true;
            Join(id, zoneId);
        }

        // Char.outZone + Char.joinZone: leave the current zone, clear the client's map, describe the new one
        private void Join(int mapId, int zoneId)
        {
            var target = world.GetZone(mapId, zoneId);
            me.Zone.Players.Remove(me);
            session.Send(new Message(Cmd.MAP_CLEAR));
            me.Zone = target;
            target.Players.Add(me);
            session.Send(EnterWorld.MapInfo(mapId, me.X, me.Y, target));
            session.Send(EnterWorld.MapItems(target.Tile, world, session.ZoomLevel));
            me.IsChangeMap = false;
            PlayerTasks.OnChangeMap(me, world, mapId);   // NV khám phá xa làng
        }

        // Service.openUIZone: the list of zones with their player counts; needs Goosho nearby or a teleport scroll
        public void OpenUiZone()
        {
            var tile = me.Zone.Tile;
            if (tile.IsNotChangeZone) { Dialog("Không thể chuyển khu"); return; }
            bool near = false;
            foreach (var n in me.Zone.Npcs)
                if (n.TemplateId == 13 && System.Math.Abs(me.X - n.X) < 50 && System.Math.Abs(me.Y - n.Y) < 50) near = true;
            // Items 35 and 37 (teleport scrolls) also allow it; the bag is not ported yet, so only the NPC counts.
            if (!near) { Dialog("Bạn cần đứng gần Goosho để chuyển khu."); return; }
            var ms = new Message(Cmd.OPEN_UI_ZONE);
            var zones = world.GetZones(tile.Id);
            ms.Writer.WriteByte(zones.Count);
            foreach (var z in zones)
            {
                ms.Writer.WriteByte(z.NumberChar);
                ms.Writer.WriteByte(0);   // number of parties in the zone
            }
            session.Send(ms);
        }

        // Char.changeZone
        public void ChangeZone(Message m)
        {
            var tile = me.Zone.Tile;
            if (tile.IsNotChangeZone) { Dialog("Không thể chuyển khu"); return; }
            sbyte zoneId = m.Reader.ReadSByte();
            m.Reader.ReadSByte();   // indexUI
            var zones = world.GetZones(tile.Id);
            if (zoneId < 0 || zoneId >= zones.Count) return;
            int cap = (me.MapId == 3 || me.MapId == 28 || me.MapId == 39 || (me.MapId >= 134 && me.MapId <= 137) ||
                       (me.MapId >= 141 && me.MapId <= 143) || (me.MapId >= 163 && me.MapId <= 165)) ? 90 : 20;
            if (zones[zoneId].NumberChar >= cap) return;
            Join(tile.Id, zoneId);
        }

        // Char.startDie for ordinary maps: the character drops to the ground below it and waits to be revived.
        public void StartDie()
        {
            var tile = me.Zone.Tile;
            var grid = world.GetGrid(tile);
            me.Y = (short)(me.Y / 24 * 24);
            if (me.Y == 0)
            {
                while (grid.TileTypeAt(me.X, me.Y, TileGrid.T_TOP))
                {
                    me.Y += 24;
                    if (me.Y > grid.PxH) break;
                }
            }
            while (!grid.TileTypeAt(me.X, me.Y, TileGrid.T_TOP))
            {
                me.Y += 24;
                if (me.Y > grid.PxH) break;
            }
            SendMove();
            ResetPoint();
            me.IsDead = true;
            me.Hp = 0;
            var die = new Message(me.ExpDown > 0 ? Cmd.ME_DIE_EXP_DOWN : Cmd.ME_DIE);
            die.Writer.WriteByte(me.TypePk);
            die.Writer.WriteShort(me.X);
            die.Writer.WriteShort(me.Y);
            die.Writer.WriteLong(me.ExpDown > 0 ? me.ExpDown : me.Exp);
            session.Send(die);
            var wait = new Message(Cmd.PLAYER_DIE);
            wait.Writer.WriteInt(me.Record.Id);
            wait.Writer.WriteByte(me.TypePk);
            wait.Writer.WriteShort(me.X);
            wait.Writer.WriteShort(me.Y);
            session.Send(wait);
        }

        // Char.wakeUpFromDead(): full health, "alive" signal, then the client is told where it stands
        private void WakeUp()
        {
            if (!me.IsDead) return;
            me.IsDead = false;
            me.Hp = me.MaxHp;
            me.Mp = me.MaxMp;
            session.Send(new Message(Cmd.ME_LIVE));
            ResetPoint();
            var ret = new Message(Cmd.RETURN_POINT_MAP);
            ret.Writer.WriteInt(me.Record.Id);
            ret.Writer.WriteShort(me.X);
            ret.Writer.WriteShort(me.Y);
            session.Send(ret);
        }

        // ME_BACK: revive and go back to the save point (Lang Tone) - Char.returnTownFromDead
        public void ReturnTownFromDead()
        {
            if (!me.IsDead) return;
            WakeUp();
            // back to the saved village (Kamakura's "Lưu tọa độ", Lang Tone by default)
            int home = me.Progress != null ? me.Progress.SaveMap : 22;
            var xy = PlayerTasks.VillageXY(home);
            me.SetXY(xy[0], xy[1]);
            ChangeMap(home);
            var info = new Message(Cmd.SUB_COMMAND);
            info.Writer.WriteSByte(Cmd.ME_LOAD_INFO);
            info.Writer.WriteInt(me.Progress != null ? (int)System.Math.Min(int.MaxValue, me.Progress.Xu) : 0);    // xu
            info.Writer.WriteInt(me.Progress != null ? (int)System.Math.Min(int.MaxValue, me.Progress.Yen) : 0);   // yen
            info.Writer.WriteInt(0);          // luong
            info.Writer.WriteInt(me.Hp);
            info.Writer.WriteInt(me.Mp);
            info.Writer.WriteByte(0);         // captcha
            session.Send(info);
        }

        // ME_LIVE: revive on the spot, which costs 1 luong (gold)
        public void ReviveInPlace()
        {
            if (!me.IsDead) return;
            // luong (gold) is bought with real money in the original; offline starts with none, so this path is closed
            Dialog("Cần 1 lượng để hồi sinh tại chỗ.");
        }

        private void SendMove()
        {
            var ms = new Message(Cmd.PLAYER_MOVE);
            ms.Writer.WriteInt(me.Record.Id);
            ms.Writer.WriteShort(me.X);
            ms.Writer.WriteShort(me.Y);
            session.Send(ms);
        }

        // Service.resetPoint: tell the client where the server thinks it is
        private void ResetPoint()
        {
            var ms = new Message(Cmd.RESET_POINT);
            ms.Writer.WriteShort(me.X);
            ms.Writer.WriteShort(me.Y);
            session.Send(ms);
        }

        private void Dialog(string text)
        {
            var ms = new Message(Cmd.SERVER_DIALOG);
            ms.Writer.WriteUTF(text);
            session.Send(ms);
        }

        // On disconnect the position is saved (only on ordinary maps, which is all that can be entered now).
        public void SaveAndLeave()
        {
            me.Zone.Players.Remove(me);
            me.Record.MapId = me.MapId;
            me.Record.X = me.X;
            me.Record.Y = me.Y;
            PlayerItems.Save(me);   // bag, money, quest step, skills -> PlayerRecord.Data
            session.Server.Accounts.Flush();
        }
    }
}

