using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Zone = NsoServer.World.Zone;
    using Mob = NsoServer.World.Mob;

    // Messages from com.nsoz.network.Service / AbsService / MapService used by items, quests, menus and effects.
    // Command values are identical to com.nsoz.constants.CMD.
    public static class PlayerService
    {
        // top-level commands
        public const sbyte SERVER_MESSAGE = -24, SERVER_DIALOG = -26, ALERT_MESSAGE = 53;
        public const sbyte OPEN_UI_SAY = 38, OPEN_UI_CONFIRM = 39, OPEN_UI_MENU = 40, OPEN_UI_NEWMENU = 63, OPEN_UI = 30, OPEN_UI_SHOP = 33;
        public const sbyte TASK_GET = 47, TASK_NEXT = 48, TASK_FINISH = 49, TASK_UPDATE = 50;
        public const sbyte ITEM_BAG_ADD = 8, ITEM_USE = 11, ITEM_BUY = 13, ITEM_BODY_TO_BAG = 15, ITEM_USE_UPTOUP = 18, REQUEST_ITEM_INFO = 42;
        public const sbyte ITEMMAP_ADD = 6, ITEMMAP_MYPICK = -14, ITEMMAP_REMOVE = -15;
        public const sbyte ME_UP_COIN_LOCK = -8, ME_UP_COIN_BAG = 95;
        public const sbyte NPC_IS_DISABLE = 85, NPC_IS_MOVE = 86, NPC_IS_FIRE = 89, NPC_IS_ICE = 90, NPC_IS_WIND = 91;
        // sub commands (under SUB_COMMAND -30)
        public const sbyte ME_LOAD_CLASS = -126, ME_LOAD_SKILL = -125, ME_LOAD_HP = -122, ME_LOAD_MP = -121, PLAYER_LOAD_HP = -111;
        public const sbyte POTENTIAL_UP = -109, USE_BOOK_SKILL = -102, ME_ADD_EFFECT = -101, ME_REMOVE_EFFECT = -99;

        public static Message Sub(sbyte sub)
        {
            var m = new Message(Cmd.SUB_COMMAND);
            m.Writer.WriteSByte(sub);
            return m;
        }

        public static void ServerMessage(PlayerState me, string text)
        {
            var m = new Message(SERVER_MESSAGE);
            m.Writer.WriteUTF(text);
            me.Session.Send(m);
        }

        public static void ServerDialog(PlayerState me, string text)
        {
            var m = new Message(SERVER_DIALOG);
            m.Writer.WriteUTF(text);
            me.Session.Send(m);
        }

        public static void ShowAlert(PlayerState me, string title, string text)
        {
            var m = new Message(ALERT_MESSAGE);
            m.Writer.WriteUTF(title);
            m.Writer.WriteUTF(text);
            me.Session.Send(m);
        }

        // Service.npcChat: the NPC says something in a speech bubble
        public static void NpcChat(PlayerState me, int npcId, string text)
        {
            var m = new Message(OPEN_UI_SAY);
            m.Writer.WriteShort(npcId);
            m.Writer.WriteUTF(text);
            me.Session.Send(m);
        }

        // Service.openUIMenu: the server-built menu (me.Menus) replaces the NPC's own menu
        public static void OpenUiMenu(PlayerState me)
        {
            var m = new Message(OPEN_UI_NEWMENU);
            foreach (var e in me.Menus) m.Writer.WriteUTF(e.Name);
            me.Session.Send(m);
        }

        // Service.menu(): no server entries, the client shows the NPC's own menu
        public static void ClientMenu(PlayerState me)
        {
            me.Session.Send(new Message(OPEN_UI_MENU));
        }

        // Service.openUIConfirm: a question from the NPC with the menu entries as answers
        public static void OpenUiConfirm(PlayerState me, int npcId, string title)
        {
            var m = new Message(OPEN_UI_CONFIRM);
            m.Writer.WriteShort(npcId);
            m.Writer.WriteUTF(title);
            m.Writer.WriteByte(me.Menus.Count);
            foreach (var e in me.Menus) m.Writer.WriteUTF(e.Name);
            me.Session.Send(m);
        }

        // Service.openUI(type): opens a client window (shops, upgrade...)
        public static void OpenUi(PlayerState me, int type)
        {
            var m = new Message(OPEN_UI);
            m.Writer.WriteByte(type);
            me.Session.Send(m);
        }

        // Service.openUIShop: the items of a shop
        public static void OpenUiShop(PlayerState me, int type, List<StoreItem> items)
        {
            var m = new Message(OPEN_UI_SHOP);
            m.Writer.WriteByte(type);
            m.Writer.WriteByte(items.Count);
            for (int i = 0; i < items.Count; i++) { m.Writer.WriteByte(i); m.Writer.WriteShort(items[i].ItemId); }
            me.Session.Send(m);
        }

        // ---- quests ----

        // Service.sendTaskInfo
        public static void SendTaskInfo(PlayerState me, TaskTemplateData t)
        {
            var m = new Message(TASK_GET);
            m.Writer.WriteShort(t.Id);
            m.Writer.WriteByte(me.Progress.TaskIndex);
            m.Writer.WriteUTF(t.Name);
            m.Writer.WriteUTF(t.Detail);
            m.Writer.WriteByte(t.SubNames.Length);
            foreach (var s in t.SubNames) m.Writer.WriteUTF(s);
            m.Writer.WriteShort(me.Progress.TaskCount);
            for (int i = 0; i < t.SubNames.Length; i++) m.Writer.WriteShort(i < t.Counts.Length ? t.Counts[i] : -1);
            me.Session.Send(m);
        }

        public static void TaskNext(PlayerState me) { me.Session.Send(new Message(TASK_NEXT)); }
        public static void TaskFinish(PlayerState me) { me.Session.Send(new Message(TASK_FINISH)); }

        public static void TaskCount(PlayerState me, int count)
        {
            var m = new Message(TASK_UPDATE);
            m.Writer.WriteShort(count);
            me.Session.Send(m);
        }

        // ---- items and money ----

        public static bool IsWearable(ItemTemplateData t) { return t != null && t.Type >= 0 && t.Type <= 15; }
        private static bool HasUpgradeByte(ItemTemplateData t) { return t != null && ((t.Type >= 0 && t.Type <= 15) || t.Type == 34); }

        // Service.addItem: a bag slot was filled or its quantity changed
        public static void AddItem(PlayerState me, PlayerItem item, ItemTemplateData t)
        {
            var m = new Message(ITEM_BAG_ADD);
            m.Writer.WriteByte(item.Index);
            m.Writer.WriteShort(item.Id);
            m.Writer.WriteBoolean(item.IsLock);
            if (HasUpgradeByte(t)) m.Writer.WriteByte(item.Upgrade);
            m.Writer.WriteBoolean(item.Expire > 0);
            m.Writer.WriteShort(QuantityDisplay(item, t));
            me.Session.Send(m);
        }

        public static int QuantityDisplay(PlayerItem item, ItemTemplateData t)
        {
            int q = t != null && t.IsUpToUp ? item.Quantity : 1;
            return q < 1 ? 1 : q > 32767 ? 32767 : q;
        }

        // Service.useItemUpToUp: quantity taken from a bag slot (the client empties it at 0)
        public static void UseItemUpToUp(PlayerState me, int index, int quantity)
        {
            var m = new Message(ITEM_USE_UPTOUP);
            m.Writer.WriteByte(index);
            m.Writer.WriteShort(quantity);
            me.Session.Send(m);
        }

        // Service.useItem: an equipment item went from the bag to the body
        public static void UseItem(PlayerState me, int index)
        {
            var m = new Message(ITEM_USE);
            m.Writer.WriteByte(index);
            m.Writer.WriteByte(me.Speed);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteInt(me.MaxMp);
            m.Writer.WriteShort(0);   // eff5buffhp
            m.Writer.WriteShort(0);   // eff5buffmp
            me.Session.Send(m);
        }

        // Service.itemBodyToBag
        public static void ItemBodyToBag(PlayerState me, int equipIndex, int bagIndex)
        {
            var m = new Message(ITEM_BODY_TO_BAG);
            m.Writer.WriteByte(me.Speed);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteInt(me.MaxMp);
            m.Writer.WriteShort(0);
            m.Writer.WriteShort(0);
            m.Writer.WriteByte(equipIndex);
            m.Writer.WriteByte(bagIndex);
            m.Writer.WriteShort(me.Record.Head);
            me.Session.Send(m);
        }

        // Service.buy: money after a purchase
        public static void Buy(PlayerState me)
        {
            var m = new Message(ITEM_BUY);
            m.Writer.WriteInt((int)System.Math.Min(int.MaxValue, me.Progress.Xu));
            m.Writer.WriteInt((int)System.Math.Min(int.MaxValue, me.Progress.Yen));
            m.Writer.WriteInt(0);   // luong (gold) does not exist offline
            me.Session.Send(m);
        }

        // Service.addYen / addXu: money gained, shown by the client
        public static void AddYen(PlayerState me, int add)
        {
            var m = new Message(ME_UP_COIN_LOCK);
            m.Writer.WriteInt(add);
            me.Session.Send(m);
        }

        public static void AddXu(PlayerState me, int add)
        {
            var m = new Message(ME_UP_COIN_BAG);
            m.Writer.WriteInt(add);
            me.Session.Send(m);
        }

        // Service.itemInfo / equipmentInfo: details of a bag (type 3) or equipment (type 5) item
        public static void ItemInfo(PlayerState me, PlayerItem item, ItemTemplateData t, int typeUI, int indexUI)
        {
            var m = new Message(REQUEST_ITEM_INFO);
            m.Writer.WriteByte(typeUI);
            m.Writer.WriteByte(indexUI);
            m.Writer.WriteLong(item.Expire);
            m.Writer.WriteInt(item.Yen);
            if (t != null && (IsWearable(t) || (t.Type >= 29 && t.Type <= 34)))
            {
                m.Writer.WriteByte(item.System);
                foreach (var o in item.Options) { m.Writer.WriteByte(o.Id); m.Writer.WriteInt(o.Param); }
            }
            me.Session.Send(m);
        }

        // Service.itemStoreInfo: price and best options of a shop item
        public static void ItemStoreInfo(PlayerState me, StoreItem s, ItemTemplateData t, int typeUI, int indexUI)
        {
            var m = new Message(REQUEST_ITEM_INFO);
            m.Writer.WriteByte(typeUI);
            m.Writer.WriteByte(indexUI);
            m.Writer.WriteLong(s.Expire);
            m.Writer.WriteInt(s.Coin);
            m.Writer.WriteInt(s.Yen);
            m.Writer.WriteInt(s.Gold);
            if (t != null && (IsWearable(t) || (t.Type >= 29 && t.Type <= 34)))
            {
                m.Writer.WriteByte(s.Sys);
                for (int i = 0; i < s.OptionIds.Length; i++) { m.Writer.WriteByte(s.OptionIds[i]); m.Writer.WriteInt(s.MaxParams[i]); }
            }
            me.Session.Send(m);
        }

        // ---- HP / MP / class / skills ----

        public static void UpdateHp(PlayerState me)
        {
            var m = Sub(ME_LOAD_HP);
            m.Writer.WriteInt(me.Hp);
            me.Session.Send(m);
        }

        public static void UpdateMp(PlayerState me)
        {
            var m = Sub(ME_LOAD_MP);
            m.Writer.WriteInt(me.Mp);
            me.Session.Send(m);
        }

        // MapService.loadHP: the zone sees the character's HP
        public static void LoadHp(PlayerState me)
        {
            var m = Sub(PLAYER_LOAD_HP);
            m.Writer.WriteInt(me.Record.Id);
            m.Writer.WriteInt(me.Hp);
            foreach (PlayerState p in me.Zone.Players) p.Session.Send(m);
        }

        // Service.loadClass
        public static void LoadClass(PlayerState me)
        {
            var m = Sub(ME_LOAD_CLASS);
            m.Writer.WriteByte(me.Speed);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteInt(me.MaxMp);
            m.Writer.WriteShort(me.Potential[0]);
            m.Writer.WriteShort(me.Potential[1]);
            m.Writer.WriteInt(me.Potential[2]);
            m.Writer.WriteInt(me.Potential[3]);
            m.Writer.WriteByte(me.Record.ClassId);
            m.Writer.WriteShort(me.SkillPoint);
            m.Writer.WriteShort(me.PotentialPoint);
            me.Session.Send(m);
        }

        // Service.loadSkill: the learned skill rows (skill ids, not template ids)
        public static void LoadSkill(PlayerState me, GameWorld world)
        {
            var m = Sub(ME_LOAD_SKILL);
            m.Writer.WriteByte(me.Speed);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteInt(me.MaxMp);
            m.Writer.WriteShort(me.SkillPoint);
            var ids = new List<int>();
            foreach (var s in me.Progress.Skills)
            {
                var row = world.FindSkill(s[0], s[1]);
                if (row != null) ids.Add(row.Id);
            }
            m.Writer.WriteByte(ids.Count);
            foreach (int id in ids) m.Writer.WriteShort(id);
            me.Session.Send(m);
        }

        // Service.updatePotential
        public static void UpdatePotential(PlayerState me)
        {
            var m = Sub(POTENTIAL_UP);
            m.Writer.WriteByte(me.Speed);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteInt(me.MaxMp);
            m.Writer.WriteShort(me.PotentialPoint);
            m.Writer.WriteShort(me.Potential[0]);
            m.Writer.WriteShort(me.Potential[1]);
            m.Writer.WriteInt(me.Potential[2]);
            m.Writer.WriteInt(me.Potential[3]);
            me.Session.Send(m);
        }

        // Service.useBookSkill
        public static void UseBookSkill(PlayerState me, int itemIndex, int skillId)
        {
            var m = Sub(USE_BOOK_SKILL);
            m.Writer.WriteByte(itemIndex);
            m.Writer.WriteShort(skillId);
            me.Session.Send(m);
        }

        // Service.sendSkillShortcut: shortcut bars, all empty
        public static void SkillShortcut(PlayerState me, string key, int slots)
        {
            var m = Sub(Cmd.LOAD_RMS);
            m.Writer.WriteUTF(key);
            m.Writer.WriteInt(slots);
            for (int i = 0; i < slots; i++) m.Writer.WriteByte(-1);
            m.Writer.WriteByte(0);
            me.Session.Send(m);
        }

        // ---- effects ----

        // Service.addEffect
        public static void AddEffect(PlayerState me, PlayerEffect e, long now)
        {
            var m = Sub(ME_ADD_EFFECT);
            m.Writer.WriteByte(e.TemplateId);
            m.Writer.WriteInt((int)((now - e.StartAt) / 1000));
            m.Writer.WriteInt((int)(e.EndAt - e.StartAt));
            m.Writer.WriteShort(e.Param);
            if (e.Type == 14 || e.Type == 2 || e.Type == 3) { m.Writer.WriteShort(me.X); m.Writer.WriteShort(me.Y); }
            me.Session.Send(m);
        }

        // Service.removeEffect
        public static void RemoveEffect(PlayerState me, PlayerEffect e)
        {
            var m = Sub(ME_REMOVE_EFFECT);
            m.Writer.WriteByte(e.TemplateId);
            if (e.Type == 0 || e.Type == 12) { m.Writer.WriteInt(me.Hp); m.Writer.WriteInt(me.Mp); }
            else if (e.Type == 4 || e.Type == 13 || e.Type == 17) m.Writer.WriteInt(me.Hp);
            else if (e.Type == 23) { m.Writer.WriteInt(me.Hp); m.Writer.WriteInt(me.MaxHp); }
            me.Session.Send(m);
        }

        // ---- ground items and monster states ----

        public static void SendZone(Zone zone, Message m)
        {
            foreach (PlayerState p in zone.Players) p.Session.Send(m);
        }

        // MapService.addItemMap
        public static void AddItemMap(Zone zone, ItemMapEntry it)
        {
            var m = new Message(ITEMMAP_ADD);
            m.Writer.WriteShort(it.Id);
            m.Writer.WriteShort(it.Item.Id);
            m.Writer.WriteShort(it.X);
            m.Writer.WriteShort(it.Y);
            SendZone(zone, m);
        }

        // MapService.removeItem
        public static void RemoveItemMap(Zone zone, short id)
        {
            var m = new Message(ITEMMAP_REMOVE);
            m.Writer.WriteShort(id);
            SendZone(zone, m);
        }

        // Service.pickItem: the character picked it up
        public static void PickItem(PlayerState me, ItemMapEntry it, ItemTemplateData t)
        {
            var m = new Message(ITEMMAP_MYPICK);
            m.Writer.WriteShort(it.Id);
            m.Writer.WriteShort(QuantityDisplay(it.Item, t));
            me.Session.Send(m);
        }

        // MapService.setFire / setIce / setWind / setDisable / setMove
        public static void MobState(Zone zone, sbyte cmd, Mob mob, bool on)
        {
            var m = new Message(cmd);
            m.Writer.WriteByte(mob.Id);
            m.Writer.WriteBoolean(on);
            SendZone(zone, m);
        }
    }
}
