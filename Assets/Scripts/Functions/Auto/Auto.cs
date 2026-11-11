using System;
namespace Functions.Auto
{
    public abstract class Auto
    {
        public class Actions
        {

            public static bool isBuff;

            public static bool TanSatMapTrong = true;


            public static bool AttackParty;






            // Offline change (user request, permanent): reMap, ChuyenMapHetBoss, isDieMP, isTinhAnh and isThuLinh
            // were removed from the game. Their default behaviour is kept: return to the auto map,
            // change map when no boss is left, attack elites (tinh anh) and bosses (thủ lĩnh).
        }

        public int xMob;

        public int yMob;

        public int jdField_c_of_type_Int;

        public int jdField_d_of_type_Int;

        public static MyVector Quai;

        public static MyVector Nguoi;

        public Auto T;

        public long timeWaitHS;

        public static bool isHoiSinh;

        public int mapID;

        public int zoneID;

        public long timecall;

        public long timeUseHp;

        protected event EventHandler e;

        public abstract void Run();

        protected void UseAuto()
        {
            Char myChar = Char.getMyChar();
            if (Char.isAHP && myChar.cHP < myChar.cMaxHP * Char.aHpValue / 100 && mSystem.currentTimeMillis() - timeUseHp > 2500 && myChar.statusMe != 14 && myChar.statusMe != 5 && myChar.cHP > 0)
            {
                for (int i = 0; i < myChar.arrItemBag.Length; i++)
                {
                    Item item = myChar.arrItemBag[i];
                    if (item != null && item.template.type == 16 && item.template.level <= myChar.clevel)
                    {
                        Service.gI().useItem(i);
                        timeUseHp = mSystem.currentTimeMillis();
                        break;
                    }
                }
            }
            if (Char.isAFood && myChar.statusMe != 14 && myChar.statusMe != 5 && myChar.cHP > 0)
            {
                if (myChar.vEff.size() == 0)
                {
                    for (int j = 0; j < myChar.arrItemBag.Length; j++)
                    {
                        Item item2 = myChar.arrItemBag[j];
                        if (item2 != null && item2.template.type == 18 && item2.template.level <= myChar.clevel)
                        {
                            Service.gI().useItem(j);
                            break;
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < myChar.vEff.size() && ((Effect)Char.getMyChar().vEff.elementAt(k)).template.type != 0; k++)
                    {
                        if (k != myChar.vEff.size() - 1)
                        {
                            continue;
                        }
                        for (int l = 0; l < myChar.arrItemBag.Length; l++)
                        {
                            Item item3 = myChar.arrItemBag[l];
                            if (item3 != null && item3.template.type == 18 && item3.template.level == Char.aFoodValue)
                            {
                                Service.gI().useItem(l);
                                break;
                            }
                        }
                    }
                }
            }
            if (Char.isAMP && myChar.cMP < myChar.cMaxMP * Char.aMpValue / 100 && myChar.statusMe != 14 && myChar.statusMe != 5 && myChar.cHP > 0)
            {
                GameScr.gI().doUseMP();
            }
        }

        public static bool isDie(Char @char)
        {
            if (@char.cHP > 0 && @char.statusMe != 14)
            {
                return @char.statusMe == 5;
            }
            return true;
        }

        protected void c(Mob paramMob)
        {
            if (paramMob == null)
            {
                return;
            }
            int paramInt = paramMob.xFirst;
            int paramInt2 = paramMob.yFirst;
            Char myChar = Char.getMyChar();
            if (TileMap.mapID == 35)
            {
                if (paramMob.xFirst == 1428 && paramMob.yFirst == 528)
                {
                    paramInt = 1452;
                    paramInt2 = 552;
                }
                else if (paramMob.xFirst == 1284 && paramMob.yFirst == 528)
                {
                    paramInt = 1308;
                    paramInt2 = 552;
                }
                else if (paramMob.xFirst == 1836 && paramMob.yFirst == 648)
                {
                    paramInt = 1812;
                    paramInt2 = 672;
                }
            }
            else if (TileMap.mapID == 37)
            {
                if ((paramMob.xFirst == 876 || paramMob.xFirst == 900) && paramMob.yFirst == 408)
                {
                    paramInt = 900;
                    paramInt2 = 432;
                }
                else if ((paramMob.xFirst == 828 || paramMob.xFirst == 852) && paramMob.yFirst == 360)
                {
                    paramInt = 852;
                    paramInt2 = 384;
                }
                else if ((paramMob.xFirst == 924 || paramMob.xFirst == 876) && paramMob.yFirst == 624)
                {
                    paramInt = 924;
                    paramInt2 = 648;
                }
                else if ((paramMob.xFirst == 732 && paramMob.yFirst == 600) || (paramMob.xFirst == 756 && paramMob.yFirst == 576))
                {
                    paramInt = 756;
                    paramInt2 = 600;
                }
            }
            if (Char.b(paramInt, paramInt2))
            {
                xMob = jdField_c_of_type_Int;
                yMob = jdField_d_of_type_Int;
                jdField_c_of_type_Int = myChar.cx;
                jdField_d_of_type_Int = myChar.cy;
                myChar.mobFocus = paramMob;
                Handle.Sleep(500);
            }
            else
            {
                Char.getMyChar().mobFocus = null;
            }
        }

        static Auto()
        {
            Quai = new MyVector();
            Nguoi = new MyVector();
        }

        protected Mob mobMobFirst(int X, int Y, int templateID = -1, int mobID = -1)
        {
            for (int i = 0; i < GameScr.vMob.size(); i++)
            {
                Mob mob = (Mob)GameScr.vMob.elementAt(i);
                if (mob != null && mob.hp > 0 && mob.status != 1 && mob.status != 0 && mob.xFirst == X && mob.yFirst == Y && mob.levelBoss != 3 && (templateID == -1 || mob.templateId == templateID) && (mobID == -1 || mob.mobId == mobID))
                {
                    return mob;
                }
            }
            EndMob();
            return null;
        }

        protected Mob Fight(int templateId, int mobID)
        {
            for (int i = 0; i < GameScr.vMob.size(); i++)
            {
                Mob mob = (Mob)GameScr.vMob.elementAt(i);
                if (mob != null && mob.status != 1 && mob.status != 0 && mob.hp > 0 && (templateId == -1 || templateId == mob.templateId) && mob.levelBoss != 3 && (mobID == -1 || mob.mobId == mobID) && TileMap.tileTypeAt(mob.xFirst, mob.yFirst, 2))
                {
                    c(mob);
                    return mob;
                }
            }
            for (int j = 0; j < GameScr.vMob.size(); j++)
            {
                Mob mob2 = (Mob)GameScr.vMob.elementAt(j);
                if (mob2 != null && mob2.status != 1 && mob2.status != 0 && mob2.hp > 0 && (templateId == -1 || templateId == mob2.templateId) && mob2.levelBoss != 3 && (mobID == -1 || mob2.mobId == mobID))
                {
                    c(mob2);
                    return mob2;
                }
            }
            EndMob();
            return null;
        }

        protected void EndMob()
        {
            Char.getMyChar().mobFocus = null;
            xMob = 0;
            yMob = 0;
            jdField_c_of_type_Int = 0;
            jdField_d_of_type_Int = 0;
        }

        protected void Attack(int templateId, int mobID)
        {
            Char myChar = Char.getMyChar();
            Mob mob = Char.getMyChar().mobFocus;
            if (jdField_c_of_type_Int > 0 && jdField_d_of_type_Int > 0)
            {
                mob = mobMobFirst(jdField_c_of_type_Int, jdField_d_of_type_Int, templateId);
            }
            if (mob == null)
            {
                mob = Fight(templateId, mobID);
            }
            if (mob != null && (mob.hp <= 0 || mob.status == 1 || mob.status == 0 || mob.levelBoss == 3 || (mobID != -1 && mobID != mob.mobId) || (templateId != -1 && templateId != mob.templateId)))
            {
                mob = null;
                Char.getMyChar().mobFocus = null;
            }
            Q(mob);
            Check(templateId, mobID);
            Skill myskill = myChar.myskill;
            int dx = myskill.dx;
            int dy = myskill.dy;
            Quai.removeAllElements();
            Nguoi.removeAllElements();
            Quai.addElement(mob);
            if (mob == null || mob.status == 0 || mob.status == 1 || mob.hp <= 0)
            {
                return;
            }
            for (int i = 0; i < GameScr.vMob.size(); i++)
            {
                if (Quai.size() + Nguoi.size() < myskill.maxFight)
                {
                    Mob mob2 = (Mob)GameScr.vMob.elementAt(i);
                    if (mob2.status != 1 && mob2.status != 0 && !mob2.Equals(mob) && mob.xFirst - 100 <= mob2.xFirst && mob2.xFirst <= mob.xFirst + 100 && mob.yFirst - 50 <= mob2.yFirst && mob2.yFirst <= mob.yFirst + 50 && (templateId == -1 || templateId == mob2.templateId) && (mobID == -1 || mobID == mob2.mobId) && mob2.levelBoss != 3)
                    {
                        Quai.addElement(mob2);
                    }
                }
            }
            for (int j = 0; j < GameScr.vCharInMap.size(); j++)
            {
                if (Quai.size() + Nguoi.size() < myskill.maxFight)
                {
                    Char @char = (Char)GameScr.vCharInMap.elementAt(j);
                    if (@char.statusMe != 14 && @char.statusMe != 5 && @char.statusMe != 15 && !@char.isInvisible && ((myChar.cTypePk >= Char.PK_PHE1 && myChar.cTypePk <= Char.PK_PHE3 && @char.cTypePk >= Char.PK_PHE1 && @char.cTypePk <= Char.PK_PHE3 && myChar.cTypePk != @char.cTypePk) || @char.cTypePk == 3 || myChar.cTypePk == 3 || (@char.cTypePk == Char.PK_NHOM && myChar.cTypePk == Char.PK_NHOM) || (myChar.testCharId >= 0 && myChar.testCharId == @char.charID) || (myChar.killCharId >= 0 && myChar.killCharId == @char.charID)) && mob.x - dx <= @char.cx && @char.cx <= mob.x + dx && mob.y - dy <= @char.cy && @char.cy <= mob.y + dy && ((myChar.cdir == -1 && @char.cx <= myChar.cx) || (myChar.cdir == 1 && @char.cx >= myChar.cx)))
                    {
                        Nguoi.addElement(@char);
                    }
                }
            }
            if (myskill == null || myskill.isCooldown(100L))
            {
                return;
            }
            if (myChar.cMP < myskill.manaUse)
            {
                Handle.Die();
                return;
            }
            Service.gI().selectSkill(myskill.template.id);
            if (myskill.template.type == 2)
            {
                Service.gI().sendUseSkillMyBuff();
            }
            else if (myskill.template.type == 1)
            {
                Service.gI().sendPlayerAttack(Quai, Nguoi, 1);
            }
            myskill.Paint();
        }

        protected void Q(Mob mob)
        {
            Char myChar = Char.getMyChar();
            if (mob != null)
            {
                int num = (myChar.isUseLongRangeWeapon() ? (myChar.getdxSkill() + 30) : (myChar.getdxSkill() + 10));
                int num2 = (myChar.isUseLongRangeWeapon() ? (myChar.getdySkill() + 30) : (myChar.getdySkill() + 10));
                if (Math.abs(mob.xFirst - myChar.cx) > num || Math.abs(mob.yFirst - myChar.cy) > num2)
                {
                    c(mob);
                }
            }
        }

        protected void Hoisinh(bool isWait)
        {
            if (isWait)
            {
                if (!isHoiSinh)
                {
                    if (mSystem.currentTimeMillis() - timeWaitHS < 2000)
                    {
                        return;
                    }
                    isHoiSinh = true;
                }
                else if (!Actions.AttackParty && containFan())
                {
                    Handle.Paint("Chờ hồi sinh!");
                    isHoiSinh = false;
                    timeWaitHS = mSystem.currentTimeMillis();
                    return;
                }
            }
            if (GameScr.HoiSinhLuong && Char.getMyChar().cHP <= 0 && Char.getMyChar().luong > 1)
            {
                Service.gI().wakeUpFromDead();
            }
            else
            {
                Service.gI().returnTownFromDead();
            }
            TileMap.LockMap(5000);
            Handle.Sleep(300);
        }

        protected bool containFan()
        {
            for (int i = 0; i < GameScr.vParty.size(); i++)
            {
                Party party = (Party)GameScr.vParty.elementAt(i);
                if (party != null && party.c != null && party.c.nClass.classId == 6 && !isDie(party.c))
                {
                    return true;
                }
            }
            return false;
        }

        protected void Default()
        {
            UseAuto();
            try
            {
                if (Handle.w == 0L)
                {
                    Handle.w = mSystem.currentTimeMillis();
                }
                if (mSystem.currentTimeMillis() - Handle.w >= (long)GameScr.gI().zones.Length * 1000L && Handle.s.size() > 0)
                {
                    Handle.s.removeElementAt(new Random().Next(0, Handle.s.size() - 1));
                    Handle.w = mSystem.currentTimeMillis();
                }
            }
            catch (Exception)
            {
            }
        }

        protected void RandomZone()
        {
            if (mSystem.currentTimeMillis() - Handle.timeChangeZone < 2000)
            {
                return;
            }
            Npc npc = Char.FindNpc(13);
            if (npc == null)
            {
                return;
            }
            if (Math.abs(Char.getMyChar().cx - npc.cx) > 22 || Math.abs(Char.getMyChar().cy - npc.cy) > 22)
            {
                Char.Move(npc.cx, npc.cy);
            }
            Service.gI().openUIZone();

            TileMap.LockMap(2000);

            if (!GameScr.isPaintZone)
            {
                return;
            }

            if (Handle.s.size() == GameScr.gI().zones.Length)
            {
                Handle.s.removeAllElements();
                return;
            }
            MyVector myVector = new MyVector();
            for (int i = 0; i < GameScr.gI().zones.Length; i++)
            {
                bool flag = false;
                int num = GameScr.gI().zones[i];
                for (int j = 0; j < Handle.s.size(); j++)
                {
                    if (i == (int)Handle.s.elementAt(j))
                    {
                        flag = true;
                        break;
                    }
                }
                if (i == TileMap.zoneID || (Handle.s.size() > 0 && flag) || num >= 20)
                {
                    continue;
                }
                if (myVector.size() <= 0)
                {
                    myVector.addElement(i);
                    continue;
                }
                for (int k = 0; k < myVector.size(); k++)
                {
                    if (num < (int)myVector.elementAt(k))
                    {
                        myVector.removeAllElements();
                        myVector.addElement(i);
                    }
                }
            }
            if (myVector.size() > 0)
            {
                Service.gI().requestChangeZone((int)myVector.elementAt(0), -1);
                if (TileMap.zoneID != (int)myVector.elementAt(0))
                {
                    TileMap.LockMap(2000);
                }
                if (TileMap.zoneID == (int)myVector.elementAt(0))
                {
                    zoneID = (int)myVector.elementAt(0);

                    Handle.s.addElement((int)myVector.elementAt(0));
                    Handle.timeChangeZone = mSystem.currentTimeMillis();

                }

            }
        }


        protected void Go(int mapID, int zone = -1, int cx = -1, int cy = -1)
        {
            if (mapID <= -1)
            {
                this.mapID = TileMap.mapID;
            }
            if (zone <= -2)
            {
                zoneID = TileMap.zoneID;
            }
            if (TileMap.mapID != mapID && mapID >= 0 && mapID <= 159)
            {
                try
                {
                    if (!TileMap.GoMap(mapID))
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
            if (zone == -1)
            {
                RandomZone();
                Handle.Sleep(500);
            }
            else if (zone > -1 && TileMap.zoneID != zone)
            {
                ChuyenKhu(zone);
                Handle.Sleep(500);
            }
        }

        protected void ChuyenKhu(int zoneID)
        {
            if (mSystem.currentTimeMillis() - Handle.timeChangeZone < 10000)
            {
                return;
            }
            Npc npc = Char.FindNpc(13);
            if (npc == null)
            {
                return;
            }
            if (Math.abs(Char.getMyChar().cx - npc.cx) > 22 || Math.abs(Char.getMyChar().cy - npc.cy) > 22)
            {
                Char.Move(npc.cx, npc.cy);
            }
            Service.gI().openUIZone();
            TileMap.LockMap(2000);
            if (GameScr.isPaintZone)
            {
                Service.gI().requestChangeZone(zoneID, -1);
                if (TileMap.zoneID != zoneID)
                {
                    TileMap.LockMap(1000);
                }
                if (TileMap.zoneID == zoneID)
                {
                    this.zoneID = zoneID;

                    Handle.timeChangeZone = mSystem.currentTimeMillis();

                }

            }
        }
        protected static bool checkChar()
        {
            return Char.getMyChar().cHP <= 0 || Char.getMyChar().statusMe == 14 || Char.getMyChar().statusMe == 5;
        }
        public void Update()
        {
            mapID = -1;
            zoneID = -1;
            Handle.s.removeAllElements();
        }

        protected void Check(int templateId, int mobID)
        {
            bool flag = false;
            for (int i = 0; i < GameScr.vMob.size(); i++)
            {
                Mob mob = (Mob)GameScr.vMob.elementAt(i);
                if (mob != null && mob.status != 1 && mob.status != 0 && mob.hp > 0 && (templateId == -1 || templateId == mob.templateId) && mob.levelBoss != 3 && (mobID == -1 || mob.mobId == mobID))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                if (TileMap.mapID == 114)
                {
                    mapID = 115;
                    return;
                }
                if (TileMap.mapID == 115)
                {
                    mapID = 116;
                    return;
                }
                RandomZone();
                Handle.Sleep(500);
            }

        }

        public void PickItemNv()
        {
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
            {
                ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
                if (itemMap != null && itemMap.template.isVatPhamNhiemVu())
                {
                    if (Math.abs(itemMap.x - Char.getMyChar().cx) > 22 || Math.abs(itemMap.y - Char.getMyChar().cy) > 22)
                    {
                        Char.Move(itemMap.x, itemMap.y);
                    }
                    Service.gI().pickItem(itemMap.itemMapID);
                    Handle.Sleep(1000);
                    break;
                }
            }
        }

        public void PickDa()
        {
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
            {
                ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
                if (itemMap != null && itemMap.template.id == 11 && (!itemMap.isWait || itemMap.isNoMyItem()))
                {
                    if (Math.abs(itemMap.x - Char.getMyChar().cx) > 22 || Math.abs(itemMap.y - Char.getMyChar().cy) > 22)
                    {
                        Char.Move(itemMap.x, itemMap.y);
                        Char.getMyChar().itemFocus = itemMap;
                    }
                    Service.gI().pickItem(itemMap.itemMapID);
                    ItemMap.s();
                    break;
                }
            }
        }

        public static long thoigian;
        public void PickItem(int itemID)
        {
            if (!GameScr.HutVatPham)
            {
                return;
            }
            try
            {
                if (mSystem.currentTimeMillis() - thoigian > 1000)
                {
                    for (int i = 0; i < GameScr.vItemMap.size(); i++)
                    {
                        ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);

                        if (itemMap.template.id == itemID)
                        {
                            Service.gI().pickItem(itemMap.itemMapID);
                            thoigian = mSystem.currentTimeMillis();

                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        public void PickHpMp()
        {
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
            {
                ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
                if (itemMap != null && (itemMap.template.type == 16 || itemMap.template.type == 17) && (!itemMap.isWait || itemMap.isNoMyItem()))
                {
                    if (Math.abs(itemMap.x - Char.getMyChar().cx) <= 150)
                    {
                        Char.Move(itemMap.x, itemMap.y);
                        Char.getMyChar().itemFocus = itemMap;
                    }
                    Service.gI().pickItem(itemMap.itemMapID);
                    ItemMap.s();
                    break;
                }
            }
        }

        public void PickNlsk()
        {
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
            {
                ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
                if (itemMap != null && (itemMap.template.type == 27 || itemMap.template.id == 12) && (!itemMap.isWait || itemMap.isNoMyItem()))
                {
                    if (Math.abs(itemMap.x - Char.getMyChar().cx) <= 150)
                    {
                        Char.Move(itemMap.x, itemMap.y);
                        Char.getMyChar().itemFocus = itemMap;
                    }
                    Service.gI().pickItem(itemMap.itemMapID);
                    ItemMap.s();
                    break;
                }
            }
        }
    }
}