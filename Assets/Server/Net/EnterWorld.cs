using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    // the Unity client also defines global types with these names, so pick the server ones explicitly
    using TileMap = NsoServer.World.TileMap;
    using Zone = NsoServer.World.Zone;

    // Messages the Java server sends when a character is selected (User.selectChar -> Service.*), in the same order.
    // A character that has not been played yet has fixed starting values; they are captured from the original server
    // and replaced by real calculations when equipment, skills and levelling are ported.
    public static class EnterWorld
    {
        private const int FreshHp = 50, FreshMp = 100, FreshSpeed = 5, BoxCells = 30;
        private static readonly short[] FreshPotential = { 15, 5, 5, 5 };

        public static PlayerState Send(Session session, UserRecord user, PlayerRecord p, GameWorld world)
        {
            // a character saved on a map that cannot be entered (special maps are not ported yet) starts in Lang Tone
            var tile = world.FindMap(p.MapId);
            if (tile == null || !tile.IsNormal) { p.MapId = 22; p.X = 228; p.Y = 192; }
            // a character saved inside a private instance (a gym of NV Thách đấu, the Chikatoya tunnel) has no zone to
            // join: gyms send it back to their school (Gymnasium.close -> mapBeforeEnterPB), anything else to Lang Tone
            if (world.RandomZoneId(p.MapId) < 0)
            {
                int back = Gymnasium.SchoolOf(p.MapId);
                var xy = PlayerTasks.VillageXY(back);
                p.MapId = back; p.X = xy[0]; p.Y = xy[1];
            }
            var zone = world.GetZone(p.MapId, world.RandomZoneId(p.MapId));
            var state = new PlayerState { Record = p, Zone = zone, X = (short)p.X, Y = (short)p.Y, PreX = (short)p.X, PreY = (short)p.Y };
            zone.Players.Add(state);
            PlayerCombat.Init(state, world);   // level, potentials, inventory, basic skill, HP/MP and attack values

            session.Send(MobMe());
            session.Send(DataBox(state, world));
            session.Send(LoadAll(p, world, state));
            session.Send(NotMap(Cmd.ME_LOAD_ACTIVE, w => w.WriteByte(0)));
            session.Send(MapInfo(p.MapId, p.X, p.Y, zone, state));
            foreach (var it in zone.ItemMaps)   // quest-gated items (TaskMechanics) this character may see
                if (it.GateTask >= 0 && TaskMechanics.CanSee(state, it))
                {
                    var add = new Message(PlayerService.ITEMMAP_ADD);
                    add.Writer.WriteShort(it.Id); add.Writer.WriteShort(it.Item.Id); add.Writer.WriteShort(it.X); add.Writer.WriteShort(it.Y);
                    session.Send(add);
                }
            session.Send(MapItems(zone.Tile, world, session.ZoomLevel));
            session.Send(BijuuInfo(user.Id));
            // Char.sendInfo: the saved shortcut bars (Char.onOSkill / onKSkill / onCSkill)
            session.Send(PlayerService.SkillShortcut("OSkill", state.Progress.OSkill));
            session.Send(PlayerService.SkillShortcut("KSkill", state.Progress.KSkill));
            session.Send(PlayerService.SkillShortcut("CSkill", state.Progress.CSkill));
            return state;
        }

        private static Message Sub(sbyte sub)
        {
            var m = new Message(Cmd.SUB_COMMAND);
            m.Writer.WriteSByte(sub);
            return m;
        }

        private static Message NotMap(sbyte sub, System.Action<JavaWriter> body)
        {
            var m = new Message(Cmd.NOT_MAP);
            m.Writer.WriteSByte(sub);
            body(m.Writer);
            return m;
        }

        // Service.loadMobMe: no pet
        private static Message MobMe()
        {
            var m = Sub(Cmd.ME_LOAD_THU_NUOI);
            m.Writer.WriteShort(0);
            m.Writer.WriteByte(0);
            return m;
        }

        // Service.sendDataBox: the storage box (coins in the box are not ported: 0)
        public static Message DataBox(PlayerState me, GameWorld world)
        {
            var box = me.Inventory.Box;
            var m = new Message(Cmd.OPEN_UI_BOX);
            m.Writer.WriteInt(0);
            m.Writer.WriteByte(box.Length);
            foreach (var item in box)
            {
                var t = item == null ? null : world.FindItem(item.Id);
                if (t == null) { m.Writer.WriteShort(-1); continue; }
                m.Writer.WriteShort(item.Id);
                m.Writer.WriteBoolean(item.IsLock);
                if ((t.Type >= 0 && t.Type <= 15) || t.Type == 34) m.Writer.WriteByte(item.Upgrade);   // isTypeBody || isTypeNgocKham
                m.Writer.WriteBoolean(item.Expire > 0);
                m.Writer.WriteShort(PlayerService.QuantityDisplay(item, t));
            }
            return m;
        }

        // Service.loadAll
        // state: the character's computed stats (level, potentials, HP/MP); null = values of a new level-1 character
        internal static Message LoadAll(PlayerRecord p, GameWorld world, PlayerState state = null)
        {
            int maxHp = state != null ? state.MaxHp : FreshHp, hp = state != null ? state.Hp : FreshHp;
            int maxMp = state != null ? state.MaxMp : FreshMp, mp = state != null ? state.Mp : FreshMp;
            var inventory = state != null && state.Inventory != null ? state.Inventory : PlayerInventory.Load(p.Data, world);
            var m = Sub(Cmd.ME_LOAD_ALL);
            var w = m.Writer;
            w.WriteInt(p.Id);
            w.WriteUTF("");              // no clan
            w.WriteByte(p.TaskId);
            w.WriteByte(p.Gender);
            w.WriteShort(p.Head);
            w.WriteByte(state != null ? state.Speed : FreshSpeed);
            w.WriteUTF(p.Name);
            w.WriteByte(0);              // hieuChien
            w.WriteByte(0);              // typePk
            w.WriteInt(maxHp);           // maxHP
            w.WriteInt(hp);              // hp
            w.WriteInt(maxMp);           // maxMP
            w.WriteInt(mp);              // mp
            w.WriteLong(p.Exp);
            w.WriteLong(state != null ? state.ExpDown : 0); // expDown
            w.WriteShort(0);             // eff5buffhp
            w.WriteShort(0);             // eff5buffmp
            w.WriteByte(p.ClassId);
            w.WriteShort(state != null ? state.PotentialPoint : 0);   // potentialPoint
            w.WriteShort(state != null ? state.Potential[0] : FreshPotential[0]);
            w.WriteShort(state != null ? state.Potential[1] : FreshPotential[1]);
            w.WriteInt(state != null ? state.Potential[2] : FreshPotential[2]);
            w.WriteInt(state != null ? state.Potential[3] : FreshPotential[3]);
            w.WriteShort(state != null ? state.SkillPoint : 0);       // skillPoint
            // learned skills (`skill` row ids); a new character only has the basic attack (id 0)
            var skillIds = new List<int>();
            if (state != null && state.Progress != null)
                foreach (var s in state.Progress.Skills) { var row = world.FindSkill(s[0], s[1]); if (row != null) skillIds.Add(row.Id); }
            else skillIds.Add(0);
            w.WriteByte(skillIds.Count);
            foreach (int id in skillIds) w.WriteShort(id);
            w.WriteInt(state != null && state.Progress != null ? (int)System.Math.Min(int.MaxValue, state.Progress.Xu) : 0);    // xu
            w.WriteInt(state != null && state.Progress != null ? (int)System.Math.Min(int.MaxValue, state.Progress.Yen) : 0);   // yen
            w.WriteInt(state != null ? PlayerLuong.Get(state) : 0);   // luong
            w.WriteByte(inventory.Bag.Length);
            foreach (var item in inventory.Bag)
            {
                if (item == null) { w.WriteShort(-1); continue; }
                var template = world.FindItem(item.Id);
                w.WriteShort(item.Id);
                w.WriteBoolean(item.IsLock);
                if (IsWearableOrGemOrMount(template)) w.WriteByte(item.Upgrade);
                w.WriteBoolean(item.Expire > 0);
                int quantity = template != null && template.IsUpToUp ? item.Quantity : 1;
                w.WriteShort(quantity < 1 ? 1 : quantity > 65535 ? 65535 : quantity);
            }
            foreach (var item in inventory.Equipment)
            {
                if (item == null) { w.WriteShort(-1); continue; }
                w.WriteShort(item.Id); w.WriteByte(item.Upgrade); w.WriteByte(item.System);
            }
            w.WriteBoolean(true);        // isHuman
            w.WriteBoolean(false);       // isNhanBan
            short head = p.Head, weapon = 15, body = (short)(p.Gender == 0 ? 10 : 1), leg = (short)(p.Gender == 0 ? 9 : 0);
            foreach (var item in inventory.Equipment)
            {
                if (item == null) continue;
                var template = world.FindItem(item.Id);
                if (template == null) continue;
                switch (template.Type)
                {
                    case 0: head = template.Part; break;
                    case 1: weapon = template.Fashion >= 0 ? (short)-1 : template.Part; break;
                    case 2: body = template.Part; break;
                    case 6: leg = template.Part; break;
                }
            }
            w.WriteShort(head);
            w.WriteShort(weapon);
            w.WriteShort(body);
            w.WriteShort(leg);
            for (int i = 0; i < 10; i++) w.WriteShort(-1);     // fashion display ids not yet modeled
            foreach (var item in inventory.Fashion)
            {
                if (item == null) { w.WriteShort(-1); continue; }
                w.WriteShort(item.Id); w.WriteByte(item.Upgrade); w.WriteByte(item.System);
            }
            return m;
        }

        private static bool IsWearableOrGemOrMount(NsoServer.World.ItemTemplateData template)
        {
            return template != null && ((template.Type >= 0 && template.Type <= 15) ||
                (template.Type >= 29 && template.Type <= 34));
        }

        // Service.sendZone
        public static Message MapInfo(int mapId, int x, int y, Zone zone, PlayerState me = null)
        {
            var tile = zone.Tile;
            var m = new Message(Cmd.MAP_INFO);
            var w = m.Writer;
            w.WriteByte(mapId);
            w.WriteByte(tile.TileId);
            w.WriteByte(tile.BgId);
            w.WriteByte(tile.Type);
            w.WriteUTF(tile.Name);
            w.WriteByte(zone.Id);
            w.WriteShort(x);
            w.WriteShort(y);
            w.WriteByte(tile.Waypoints.Count);
            foreach (var wp in tile.Waypoints)
            {
                w.WriteShort(wp.MinX); w.WriteShort(wp.MinY); w.WriteShort(wp.MaxX); w.WriteShort(wp.MaxY);
            }
            w.WriteByte(zone.Mobs.Count);
            foreach (var mob in zone.Mobs)
            {
                int levelBoss = mob.LevelBoss, hp = mob.Hp, maxHp = mob.MaxHp;
                // a chief/elite is only shown as such to players who have fought it
                if ((levelBoss == 1 || levelBoss == 2) && !mob.HasTunedHp)
                {
                    levelBoss = 0;
                    maxHp = mob.Template.Hp;
                    hp = System.Math.Min(hp, maxHp);
                }
                w.WriteBoolean(mob.IsDisable);
                w.WriteBoolean(mob.IsDontMove);
                w.WriteBoolean(mob.IsFire);
                w.WriteBoolean(mob.IsIce);
                w.WriteBoolean(mob.IsWind);
                w.WriteShort(mob.Template.Id);
                w.WriteByte(mob.Sys);
                w.WriteInt(hp);
                w.WriteByte(mob.Level);
                w.WriteInt(maxHp);
                w.WriteShort(mob.X);
                w.WriteShort(mob.Y);
                w.WriteByte(mob.Status);
                w.WriteByte(levelBoss);
                w.WriteBoolean(mob.IsBoss);
            }
            w.WriteByte(0);              // figureheads (clone dolls)
            w.WriteByte(zone.Npcs.Count);
            foreach (var n in zone.Npcs)
            {
                w.WriteByte(Escort.NpcStatus(me, n, Gymnasium.NpcStatus(zone, n)));   // the headmaster is hidden while its bot fights; Jaian shows on quest 17
                w.WriteShort(n.X);
                w.WriteShort(n.Y);
                w.WriteByte(n.TemplateId);
            }
            int shown = 0;
            foreach (var it in zone.ItemMaps) if (it.GateTask < 0) shown++;
            w.WriteByte(shown);   // items lying on the ground (quest-gated ones follow per player, TaskMechanics.RefreshVisible)
            foreach (var it in zone.ItemMaps) if (it.GateTask < 0) { w.WriteShort(it.Id); w.WriteShort(it.Item.Id); w.WriteShort(it.X); w.WriteShort(it.Y); }
            w.WriteUTF(tile.Name);
            w.WriteByte(tile.LocationStand.Count);
            foreach (var s in tile.LocationStand) { w.WriteByte(s[0]); w.WriteByte(s[1]); }
            return m;
        }

        // Service.sendItemMap: scenery images and tree positions of the map
        public static Message MapItems(TileMap tile, GameWorld world, int zoomLevel)
        {
            var m = new Message(Cmd.MAP_ITEM);
            var w = m.Writer;
            w.WriteByte(tile.Items.Count);
            foreach (int id in tile.Items)
            {
                var png = world.LoadFile("Img/Object/" + zoomLevel + "/" + id + ".png");
                w.WriteShort(id);
                w.WriteInt(png.Length);
                w.Write(png);
            }
            foreach (var list in new[] { tile.Behind, tile.Between, tile.Front })
            {
                w.WriteByte(list.Count);
                foreach (var t in list) { w.WriteByte(t.Id); w.WriteByte(t.X); w.WriteByte(t.Y); }
            }
            return m;
        }

        // AbsService.onBijuuInfo: no beast items
        private static Message BijuuInfo(int userId)
        {
            var m = new Message(Cmd.MAP_ITEM);
            m.Writer.WriteByte(-1);
            m.Writer.WriteByte(0);
            m.Writer.WriteInt(userId);
            for (int i = 0; i < 5; i++) m.Writer.WriteShort(-1);
            return m;
        }

        // Service.sendSkillShortcut: shortcut slots, all empty
        private static Message SkillShortcut(string key, int slots)
        {
            var m = Sub(Cmd.LOAD_RMS);
            m.Writer.WriteUTF(key);
            m.Writer.WriteInt(slots);
            for (int i = 0; i < slots; i++) m.Writer.WriteByte(-1);
            m.Writer.WriteByte(0);
            return m;
        }
    }
}
