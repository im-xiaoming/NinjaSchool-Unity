using System.Threading;
using UnityEngine;
namespace Functions.Auto
{
    public class AutoTask : Auto
    {
        public int LessOrEqualsTask;

        public int Phai;
        public bool Level10;

        public static object objTask;

        public static bool isTask;

        public static int saveTD;

        public long bagsort;

        public long boxsort;

        public int useThucAn;

        public long timeuptn;

        public static object objSleep;

        public int taskId => Char.getMyChar().ctaskId;

        public int mapId => GameScr.gI().getTaskMapId();

        public int Npcid => GameScr.gI().getTaskNpcId();

        public int index => Char.getMyChar().taskMaint.index;

        public bool mainnull => Char.getMyChar().taskMaint == null;

        public Char me => Char.getMyChar();

        private static int[] w;
        private static int[] x;
        private static int[] y;
        private static int[] z;
        private static int[] aa;
        private static int[] ab;
        private static int[] ac;


        public override void Run()
        {

            if (Auto.isDie(Char.getMyChar()))
            {
                Hoisinh(isWait: true);
                Sleep(500);

            }
            else
            {
                //Actions.isThuLinh = false;
                //Actions.isTinhAnh = false;
                TuDong();
                a();
            }
        }

        public override string ToString()
        {
            if (LessOrEqualsTask == -1)
            {
                return "Auto nhiệm vụ: " + (Level10 ? "10" : "20:");
            }
            return "Auto nhiệm vụ: 20  < or = " + LessOrEqualsTask;
        }

        public void update(int phai, bool isLevel10, int LessOrEqualsTask = -1)
        {
            Update();
            this.LessOrEqualsTask = LessOrEqualsTask;
            this.Level10 = isLevel10;
            this.Phai = phai;
            w = new int[] { 0, 1, 1, 72, 72, 27, 27 };
            x = new int[] { 0, 9, 9, 10, 10, 11, 11 };
            y = new int[] { 0, 0, 1, 0, 1, 0, 1 };
            z = new int[] { 0, 94, 114, 99, 109, 105, 119 };
            aa = new int[] { -1, 40, 49, 58, 67, 76, 85 };
            ab = new int[] { -1, 41, 50, 59, 68, 77, 86 };
            ac = new int[] { -1, 42, 51, 60, 69, 78, 87 };
        }
        public bool checkDoneAS10()
        {
            return Char.getMyChar().ctaskId >= 9;
        }

        public bool checkDoneAS20()
        {
            return Char.getMyChar().ctaskId >= 20;
        }

        public void checkTask()
        {

        }
        public void Auto10()
        {
            Task taskMain = Char.getMyChar().taskMaint;
            if (checkDoneAS10())
            {
                Handle.Paint("Xong!!");
                Handle.Abort();
                return;
            }
            if (taskMain == null)
            {
                if (TileMap.mapID != mapId)
                {
                    Go(mapId);
                    return;
                }
                Pick(Npcid, 0, optionId: 0);
                Service.gI().getTask(Npcid, 0, 0);
                Lock(2000);
                return;
            }
            if (taskMain.index >= taskMain.counts.Length - 1)
            {
                if (TileMap.mapID != mapId)
                {
                    Go(mapId);
                    return;
                }

                Pick(Npcid, 0, optionId: 0);
                Service.gI().getTask(Npcid, 0, 0);
                Lock();
                return;
            }

            switch (taskMain.taskId)
            {
                case 0:

                    for (int i = 0; i < Npc.arrNpcTemplate[Npcid].menu.Length; i++)
                    {
                        string text = Npc.arrNpcTemplate[Npcid].menu[i][0];
                        if (text.Equals("Talk") || (text.Equals("Nói chuyện") && Npcid != 12))
                        {
                            Pick(Npcid, i, optionId: 0);
                            Lock(500);
                            return;
                        }
                    }

                    return;
                case 1:
                    if (taskMain.index == 0)
                    {
                        Pick(Npcid, 0, optionId: 0);
                        Service.gI().getTask(Npcid, 1, 0);
                        Lock(1000);
                        return;
                    }
                    if (taskMain.index == 1)
                    {

                        Service.gI().getTask(Npcid, 0, 0);
                        Lock(1000);
                        return;
                    }
                    if (taskMain.index == 2)
                    {

                        Service.gI().getTask(Npcid, 1, 0);
                        Lock(1000);
                        return;
                    }
                    if (taskMain.index == 3)
                    {


                        Service.gI().getTask(Npcid, 2, 0);
                        Lock(1000);
                        return;
                    }
                    if (taskMain.index == 4)
                    {

                        Service.gI().getTask(Npcid, 0, 0);
                        Lock(1000);
                        return;
                    }
                    return;
                case 2:
                    if (taskMain.index == 0)
                    {
                        if (ItemBagItemId(194) != null)
                        {
                            Service.gI().useItem(ItemBagItemId(194).indexUI);
                            Lock(1000);

                        }
                        return;
                    }
                    if (taskMain.index == 1)
                    {
                        Attack(0, -1);
                        return;
                    }
                    return;

                case 3:
                    if (taskMain.index == 0)
                    {
                        Thread.Sleep(2000);
                        Pick(4, 0);
                        Service.gI().buyItem(9, 0, 3);
                        Clock.LockBuy(2000);
                        return;
                    }
                    if (taskMain.index == 1)
                    {
                        if (ItemBagItemId(23) != null)
                        {
                            Service.gI().useItem(ItemBagItemId(23).indexUI);
                            Lock(1000);
                        }
                        else
                        {
                            S(2);
                        }
                        return;
                    }
                    if (taskMain.index == 2)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                        }
                        else
                        {
                            Attack(1, -1);
                        }
                        return;
                    }
                    if (taskMain.index == 3)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                        }
                        else
                        {
                            Attack(2, -1);
                        }
                        return;
                    }
                    return;
                case 4:
                    if (taskMain.index == 0)
                    {
                        if (TileMap.mapID != 23)
                        {
                            Go(23);
                        }
                        else
                        {
                            Attack(1, -1);
                        }
                        return;
                    }
                    if (taskMain.index == 1)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }
                        PickItemNv();
                        Attack(3, -1);
                        return;
                    }
                    if (taskMain.index == 2)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }
                        PickItemNv();
                        Attack(4, -1);
                        return;
                    }
                    return;
                case 5:
                    if (taskMain.index == 0)
                    {
                        if (TileMap.mapID != 23)
                        {
                            Go(23);
                        }
                        else
                        {
                            Attack(1, -1);
                        }
                        return;
                    }
                    if (taskMain.index == 1)
                    {
                        if (TileMap.mapID != 20)
                        {
                            Go(20);
                            return;
                        }
                        PickItemNv();
                        Attack(54, -1);
                        return;
                    }

                    return;
                case 6:
                    if (taskMain.index == 0)
                    {
                        if (TileMap.mapID != 23)
                        {
                            Go(23);
                        }
                        else
                        {
                            Attack(1, -1);
                        }
                        return;
                    }
                    if (taskMain.index == 1)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                        }
                        return;
                    }
                    if (taskMain.index == 2)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                        }
                        return;
                    }
                    if (taskMain.index == 3)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                        }
                        return;
                    }
                    return;
                case 7:
                    if (taskMain.index == 0)
                    {
                        if (TileMap.mapID != 2)
                        {
                            Go(2);
                        }
                        else
                        {
                            Attack(-1, -1);
                        }
                        return;
                    }
                    if (taskMain.index == 1)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }
                        Pick(Npcid, 0, 0);
                        Service.gI().getTask(Npcid, 1, 0);
                        return;
                    }
                    if (taskMain.index == 2)
                    {

                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 0, 0);
                        return;
                    }
                    if (taskMain.index == 3)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 1, 0);

                        return;
                    }
                    if (taskMain.index == 4)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 1, 0);

                        return;
                    }
                    if (taskMain.index == 5)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 2, 0);

                        return;
                    }
                    if (taskMain.index == 6)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }
                        Pick(Npcid, 0, 0);
                        Service.gI().getTask(Npcid, 2, 0);

                        return;
                    }
                    if (taskMain.index == 7)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 0, 0);

                        return;
                    }
                    if (taskMain.index == 8)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 2, 0);

                        return;
                    }
                    if (taskMain.index == 9)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 2, 0);

                        return;
                    }
                    if (taskMain.index == 10)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 1, 0);

                        return;
                    }
                    if (taskMain.index == 11)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 0, 0);

                        return;
                    }
                    if (taskMain.index == 12)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 1, 0);

                        return;
                    }
                    if (taskMain.index == 13)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 2, 0);

                        return;
                    }
                    if (taskMain.index == 14)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 2, 0);

                        return;
                    }
                    if (taskMain.index == 15)
                    {
                        if (TileMap.mapID != mapId)
                        {
                            Go(mapId);
                            return;
                        }

                        Service.gI().getTask(Npcid, 1, 0);

                        return;
                    }
                    return;
                case 8:
                    if (taskMain.index == 0)
                    {
                        if (TileMap.mapID != 2)
                        {
                            Go(2);
                        }
                        else
                        {
                            Attack(-1, -1);
                        }
                        return;
                    }
                    if (TileMap.mapID != mapId)
                    {
                        Go(mapId);
                        return;
                    }
                    for (int i = 0; i < Npc.arrNpcTemplate[Npcid].menu.Length; i++)
                    {
                        string text = Npc.arrNpcTemplate[Npcid].menu[i][0];
                        if (text.Equals("Talk") || (text.Equals("Nói chuyện") && Npcid != 12))
                        {
                            Pick(Npcid, i, optionId: 0);
                            Lock(500);
                            return;
                        }
                    }

                    return;
            }
        }

        public void Auto20()
        {
            if (Char.getMyChar().ctaskId < 9)
            {
                Auto10();
            }
            else
            {
                if (this.Phai == 0 && Char.getMyChar().nClass.classId == 0)
                {
                    Handle.Paint("Hãy vào lớp!");
                    Handle.Abort();
                    return;
                }
                if (checkDoneAS20())
                {
                    Handle.Paint("Xong!!");
                    Handle.Abort();
                    return;
                }
                if (Char.getMyChar().nClass.classId == 0)
                {
                    if (Char.getMyChar().clevel < 10)
                    {
                        if (TileMap.mapID != 26)
                        {
                            Go(26);
                            return;
                        }
                        Attack(-1, -1);
                    }
                    else
                    {

                        Vo();
                    }

                    return;
                }


                Task taskMain = Char.getMyChar().taskMaint;

                if (taskMain == null)
                {
                    if (TileMap.mapID != mapId)
                    {
                        Go(mapId);
                        return;
                    }
                    Debug.Log("Npcid " + Npcid + " Fase " + 0);
                    Pick(Npcid, 0);
                    Service.gI().getTask(Npcid, 0, 0);
                    Lock(2000);
                    return;
                }
                if (taskMain.index >= taskMain.counts.Length - 1)
                {
                    if (TileMap.mapID != mapId)
                    {
                        Go(mapId);
                        return;
                    }

                    Pick(Npcid, 0, optionId: 0);
                    Service.gI().getTask(Npcid, 0, 0);
                    Lock();
                    return;
                }

                switch (taskMain.taskId)
                {

                    case 9:
                        if (taskMain.index == 0)
                        {
                            SVC();
                            return;
                        }
                        if (taskMain.index == 2)
                        {
                            upPotential();
                            return;
                        }




                        return;
                    case 10:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID != 28)
                            {
                                Go(28);
                                return;
                            }

                            Attack(5, -1);
                            return;
                        }
                        if (taskMain.index == 1)
                        {
                            if (TileMap.mapID != 4)
                            {
                                Go(4);
                                return;
                            }

                            Attack(6, -1);
                            return;
                        }

                        if (taskMain.index == 2)
                        {
                            if (TileMap.mapID != 46)
                            {
                                Go(46);
                                return;
                            }

                            Attack(7, -1);
                            return;
                        }
                        return;
                    case 11:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 28)
                            {

                                this.Attack(-1, 1);
                                return;
                            }

                            this.Go(28, -1, -1, -1);
                            return;
                        }
                        if (taskMain.index == 1)
                        {
                            Debug.Log("Index " + taskMain.index + " Fase " + 1);
                            if (TileMap.mapID != 27)
                            {
                                Go(27);

                                return;
                            }
                            int num = 0;
                            Service.gI().requestFriend();
                            Sleep(1000);
                            Debug.Log("Index " + taskMain.index + " Fase " + 2);
                            for (int j = 0; j < GameScr.vCharInMap.size(); j++)
                            {
                                Char @char = (Char)GameScr.vCharInMap.elementAt(j);
                                if (GameScr.vFriend.size() == 0)
                                {
                                    Service.gI().addFriend(@char.cName);
                                }
                                else if (GameScr.gI().checkExistFriend(@char.cName))
                                {
                                    num++;
                                    if (num == 5)
                                    {
                                        break;
                                    }
                                    Service.gI().addFriend(@char.cName);
                                    Sleep(500);
                                }
                            }
                            Debug.Log("Index " + taskMain.index + " Fase " + 3);
                            return;
                        }
                        return;
                    case 12:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 28)
                            {

                                this.Attack(-1, 1);
                                return;
                            }

                            this.Go(28, -1, -1, -1);
                            return;
                        }
                        bool var14 = false;
                        int var5 = -1;
                        Item var11 = null;
                        if (taskMain.index == 1)
                        {
                            var14 = true;
                            var5 = (new int[] { 194, 94, 114, 99, 109, 105, 119 })[Char.getMyChar().nClass.classId];
                            if ((var11 = Char.getMyChar().arrItemBody[1]) == null)
                            {
                                var14 = false;
                                var11 = Char.checkItem2(var5);
                            }
                        }
                        else if (taskMain.index == 2)
                        {
                            var14 = true;
                            var5 = 174;
                            if ((var11 = Char.getMyChar().arrItemBody[9]) == null)
                            {
                                var14 = false;
                                var11 = Char.checkItem2(174);
                            }
                        }
                        else if (taskMain.index == 3)
                        {
                            var14 = true;
                            var5 = Char.getMyChar().cgender == 1 ? 124 : 125;
                            if ((var11 = Char.getMyChar().arrItemBody[8]) == null)
                            {
                                var14 = false;
                                var11 = Char.checkItem2(var5);
                            }
                        }
                        if (var11 == null)
                        {
                            if (TileMap.mapID == 4)
                            {
                                this.PickItem(var5);
                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(4, -1, -1, -1);
                            return;
                        }
                        int var3 = 0;
                        int var16 = 0;
                        if (var11.isTypeClothe())
                        {
                            var3 = (byte)(GameScr.upClothe[var11.upgrade] / 2);
                            var16 = GameScr.coinUpClothes[var11.upgrade];
                        }
                        else if (var11.isTypeAdorn())
                        {
                            var3 = (byte)(GameScr.upAdorn[var11.upgrade] / 2);
                            var16 = GameScr.coinUpAdorns[var11.upgrade];
                        }
                        else if (var11.isTypeWeapon())
                        {
                            var3 = (byte)(GameScr.upWeapon[var11.upgrade] / 2);
                            var16 = GameScr.coinUpWeapons[var11.upgrade];
                        }
                        if (var3 << 1 > Char.checkStone() || var16 << 1 > Char.getMyChar().yen)
                        {
                            if (TileMap.mapID == 46)
                            {
                                this.PickItem(1);

                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(46, -1, -1, -1);
                            return;
                        }
                        if (TileMap.mapID != 22)
                        {
                            this.Go(22, -2, -1, -1);
                            return;
                        }
                        if (var14)
                        {
                            Service.gI().itemBodyToBag((int)var11.indexUI);
                            Lock();
                            return;
                        }

                        int var7 = var11.upgrade;
                        Pick(6, 0, 0);
                        Lock();
                        GameScr.itemUpGrade = var11;
                        for (int var8 = 0; var8 < 2 && var11.upgrade == var7; ++var8)
                        {
                            GameScr.arrItemUpGrade = new Item[18];
                            int var9 = 0;
                            int var22 = 0;
                            Item var19;
                            for (int var18 = 0; var18 < Char.getMyChar().arrItemBag.Length && var22 < var3; ++var18)
                            {
                                if ((var19 = Char.getMyChar().arrItemBag[var18]) != null && var19.template.type == 26 && var19.template.id <= 3)
                                {
                                    Char.getMyChar().arrItemBag[var18] = null;
                                    GameScr.arrItemUpGrade[var9++] = var19;
                                    var22 += GameScr.upClothe[var19.template.id];
                                }
                            }

                            do
                            {
                                Sleep(3000);

                                Service.gI().upgradeItem(var11, GameScr.arrItemUpGrade, false);
                                Lock();
                            } while (GameScr.arrItemUpGrade[0] != null);
                        }
                        GameScr.itemUpGrade = null;
                        Service.gI().useItem(var11.indexUI);
                        if (var11.upgrade > var7)
                        {
                            Lock();
                            return;
                        }

                        return;
                    case 13:


                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 4)
                            {

                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(4, -1, -1, -1);
                            return;
                        }
                        int mapvar5 = taskMain.index == 1 ? 56 : (taskMain.index == 2 ? 0 : 73);
                        if (TileMap.mapID != mapvar5)
                        {
                            if (TileMap.mapID != mapId)
                            {
                                Go(mapId, -2, -1, -1);
                                return;
                            }

                            if (GameScr.hpPotion < 10 && Char.getMyChar().yen >= 300 * (10 - GameScr.hpPotion))
                            {
                                Pick(3, 0, 0);
                                Service.gI().buyItem(7, 1, 10 - GameScr.hpPotion);
                                Lock();
                                return;
                            }


                            Pick(Npcid, 0, 0);
                            Service.gI().getTask(Npcid, 0, 0);
                            TileMap.LockMap();
                            return;
                        }
                        if (Char.getMyChar().cHP < Char.getMyChar().cMaxHP / 2 && Char.getMyChar().cHP > 0)
                        {
                            Char.getMyChar().useItemType(16);
                        }

                        if (Char.getMyChar().cMP < Char.getMyChar().cMaxMP / 2 && Char.getMyChar().cMP > 0)
                        {
                            Char.getMyChar().useItemType(17);
                        }
                        Char var13;
                        if (GameScr.vCharInMap.size() > 0 && (var13 = (Char)GameScr.vCharInMap.elementAt(0)) != null)
                        {
                            Skill var15 = Char.getMyChar().myskill;
                            if (Res.e(Char.getMyChar().cx - var13.cx) > var15.dx || Res.e(Char.getMyChar().cy - var13.cy) > var15.dy)
                            {
                                Char.Move(var13.cx < TileMap.pxw ? var13.cx : TileMap.pxw - 50, var13.cy);
                            }

                            Quai.removeAllElements();
                            Nguoi.removeAllElements();
                            Nguoi.addElement(var13);
                            Service.gI().selectSkill(var15.template.id);
                            if (var15.template.type == 2)
                            {
                                Service.gI().sendUseSkillMyBuff();
                            }
                            else if (var15.template.type == 1)
                            {
                                Service.gI().sendPlayerAttack(Quai, Nguoi, 1);
                            }
                            if (mSystem.currentTimeMillis() - var15.lastTimeUseThisSkill >= (long)var15.coolDown)
                            {
                                var15.lastTimeUseThisSkill = mSystem.currentTimeMillis();
                                var15.paintCanNotUseSkill = true;
                                Char.getMyChar().setAutoSkillPaint(GameScr.sks[var15.template.id], 0);
                                return;
                            }
                        }
                        return;
                    case 14:
                        if (Char.getMyChar().clevel >= 15 && (var11 = Char.checkItem2(ab[Char.getMyChar().nClass.classId])) != null)
                        {
                            Handle.Paint("Học sách kĩ năng");
                            Service.gI().useItem(var11.indexUI);
                            Handle.Sleep(1000);
                        }
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 29)
                            {
                                this.Go(-1);
                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(29, -1, -1, -1);
                            return;
                        }
                        if (taskMain.index == 1)
                        {

                            if (TileMap.mapID == 29 && zoneID == TileMap.zoneID)
                            {
                                ItemMap var12 = null;
                                var5 = -1;
                                for (var3 = 0; var3 < GameScr.vItemMap.size(); ++var3)
                                {
                                    ItemMap var6 = (ItemMap)GameScr.vItemMap.elementAt(var3);
                                    var7 = Math.abs(var6.x - Char.getMyChar().cx);
                                    int var8 = Math.abs(var6.y - Char.getMyChar().cy);
                                    int var9 = var7 * var7 + var8 * var8;
                                    if (!var6.isWait && var6.template.id == 212 && (Char.checkBag() > 2 || Char.checkItem(212)) && (var5 < 0 || var9 < var5))
                                    {
                                        var5 = var9;
                                        var12 = var6;
                                    }
                                }

                                if (var12 == null)
                                {
                                    zoneID = (TileMap.zoneID + 1) % 30;

                                    return;
                                }

                                Char.Move(var12.xEnd, var12.yEnd);
                                Service.gI().pickItem(var12.itemMapID);
                                var12.isWait = true;
                                return;
                            }
                            this.Go(29, zoneID, -1, -1);

                            return;
                        }
                        if (taskMain.index == 2)
                        {
                            if (TileMap.mapID == 40)
                            {
                                this.Attack(15, -1);
                                PickItemNv();
                                return;
                            }

                            this.Go(40, -1, -1, -1);
                            return;
                        }
                        return;
                    case 15:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 8)
                            {
                                this.Go(-1);
                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(8, -1, -1, -1);
                            return;
                        }

                        if (TileMap.mapID != mapId)
                        {
                            this.Go(mapId, -2, -1, -1);
                            return;
                        }

                        this.Pick(Npcid, 0, 0);
                        Lock();
                        this.RandomZone();
                        return;
                    case 16:
                        if (Char.getMyChar().clevel >= 20 && (var11 = Char.checkItem2(ac[Char.getMyChar().nClass.classId])) != null)
                        {
                            Handle.Paint("Học sách kĩ năng");
                            Service.gI().useItem(var11.indexUI);
                            Handle.Sleep(1000);
                        }

                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 8)
                            {
                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(8, -1, -1, -1);
                            return;
                        }

                        if (taskMain.index == 1)
                        {
                            if (TileMap.mapID == 63)
                            {

                                this.Attack(23, -1);
                                return;
                            }

                            this.Go(63, -1, -1, -1);
                            return;
                        }

                        if (taskMain.index == 2)
                        {
                            if (TileMap.mapID == 47)
                            {

                                this.Attack(24, -1);
                                return;
                            }

                            this.Go(47, -1, -1, -1);
                        }
                        return;
                    case 17:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 33)
                            {

                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(33, -1, -1, -1);
                            return;
                        }
                        if (taskMain.index == 1)
                        {
                            if (TileMap.mapID == 33)
                            {
                                Npc jaian = Char.FindNpc(17);
                                if (jaian != null)
                                {
                                    if (jaian.statusMe != 15)
                                    {
                                        this.Pick(jaian.template.npcTemplateId, 0);
                                        return;
                                    }
                                    GameScr.isTudanh = true;
                                    if (Char.getMyChar().cx > 59)
                                    {
                                        Char.Move(Char.getMyChar().cx - 36, Char.getMyChar().cy);
                                    }
                                    Sleep(3000);
                                }
                                return;
                            }

                            this.Go(33, -1, -1, -1);

                            return;
                        }
                        GameScr.isTudanh = false;
                        return;
                    case 18:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 50)
                            {

                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(50, -1, -1, -1);
                            return;
                        }
                        if (taskMain.index == 1)
                        {
                            if (TileMap.mapID == 50)
                            {

                                this.Attack(26, -1);
                                PickItemNv();
                                return;
                            }

                            this.Go(50, -1, -1, -1);
                            return;
                        }
                        if (taskMain.index == 2)
                        {
                            if (TileMap.mapID == 11)
                            {

                                this.Attack(27, -1);
                                PickItemNv();
                                return;
                            }

                            this.Go(11, -1, -1, -1);
                            return;
                        }
                        return;
                    case 19:
                        if (taskMain.index == 0)
                        {
                            if (TileMap.mapID == 49)
                            {

                                this.Attack(-1, -1);
                                return;
                            }

                            this.Go(49, -1, -1, -1);
                            return;
                        }
                        if (taskMain.index == 1)
                        {
                            if (TileMap.mapID == 63)
                            {
                                int x = Char.getMyChar().cx;
                                int y = Char.getMyChar().cy;
                                if (x != 1878 || y != 336)
                                {
                                    Char.Move(1878, 336);
                                }
                                Item itm = Char.checkItem2(219);
                                if (itm != null)
                                {
                                    Service.gI().useItem(itm.indexUI);
                                    Sleep(3000);
                                }
                                return;
                            }

                            this.Go(63, -1, -1, -1);
                        }
                        return;
                    case 20:

                        return;
                }
            }
        }

        public void Start()
        {
            if (Level10)
            {
                Auto10();
            }
            else if (!Level10)
            {

                Auto20()
;
            }



        }

        public static void Lock(int milisSeconds = 2000)
        {
            isTask = true;
            lock (objTask)
            {
                Monitor.Wait(objTask, milisSeconds);
            }
            Sleep(200);
        }

        public static void StopLock()
        {
            if (isTask)
            {
                lock (objTask)
                {
                    Monitor.PulseAll(objTask);
                }
                isTask = false;
            }
        }

        static AutoTask()
        {
            objTask = new object();
            objSleep = new object();
        }

        public void Pick(int npcId, int menuId, int optionId = 0)
        {
            Npc npc = Char.FindNpc(npcId);
            if (npc != null)
            {
                Char.Move(npc.cx, npc.cy);
                Service.gI().menu(-1, npcId, menuId, optionId);

            }
        }

        public Item ItemBagItemId(int id)
        {
            for (int i = 0; i < Char.getMyChar().arrItemBag.Length; i++)
            {
                Item item = Char.getMyChar().arrItemBag[i];
                if (item != null && item.template.id == id)
                {
                    return item;
                }
            }
            return null;
        }

        public Item ItemBoxItemId(int id)
        {
            for (int i = 0; i < Char.getMyChar().arrItemBox.Length; i++)
            {
                Item item = Char.getMyChar().arrItemBox[i];
                if (item != null && item.template.id == id)
                {
                    return item;
                }
            }
            return null;
        }

        public Item ItemBodyItemID(int id)
        {
            for (int i = 0; i < Char.getMyChar().arrItemBody.Length; i++)
            {
                Item item = Char.getMyChar().arrItemBody[i];
                if (item != null && item.template.id == id)
                {
                    return item;
                }
            }
            return null;
        }

        public void Fix()
        {
            if (TileMap.mapID != 22)
            {
                Go(22);
            }
            else if (ItemBodyItemID(194) == null)
            {
                if (ItemBagItemId(194) != null)
                {
                    Service.gI().useItem(ItemBagItemId(194).indexUI);
                    Sleep();
                }
                else
                {
                    Pick(5, -1);
                    Service.gI().itemBoxToBag(ItemBoxItemId(194).indexUI);
                    Sleep();
                }
            }
        }

        public void S(int w)
        {
            Pick(4, 0);
            Sleep();
            if (!GameScr.isPaintGroceryLock)
            {
                return;
            }
            for (int i = 0; i < GameScr.arrItemGroceryLock.Length; i++)
            {
                Item item = GameScr.arrItemGroceryLock[i];
                if (item != null && item.template.id == 23)
                {
                    Service.gI().buyItem(item.typeUI, item.indexUI, w);
                    Lock(1000);
                    break;
                }
            }
        }

        public int ThucAn()
        {
            int clevel = Char.getMyChar().clevel;
            if (clevel < 10)
            {
                return 1;
            }
            if (clevel >= 10 && clevel < 20)
            {
                return 10;
            }
            if (clevel >= 20 && clevel < 30)
            {
                return 20;
            }
            if (clevel >= 30 && clevel < 40)
            {
                return 30;
            }
            if (clevel >= 40 && clevel < 50)
            {
                return 40;
            }
            if (clevel >= 50 && clevel < 130)
            {
                return 50;
            }
            return 1;
        }

        public void Vo()
        {
            if (ItemBodyItemID(194) != null)
            {
                Service.gI().itemBodyToBag(ItemBodyItemID(194).indexUI);
                Sleep();
                return;
            }
            int mapId = w[this.Phai];

            if (TileMap.mapID != mapId)
            {
                this.Go(mapId, -1, -1, -1);
                return;
            }
            Pick(x[this.Phai], 1, y[this.Phai]);
            Pick(5, 1, 1);

        }

        public void SVC()
        {
            Item item = ItemBagItemId(idSVC());
            if (item != null)
            {
                Service.gI().useItem(item.indexUI);
                Lock(1000);
            }
            else if (!Char.getMyChar().isLang())
            {
                Handle.Die();
            }
            else if (Char.FindNpc(5) != null)
            {
                Pick(5, -1);
                if (me.arrItemBox == null)
                {
                    UI(4);
                }
                if (ItemBoxItemId(idSVC()) != null)
                {
                    Service.gI().itemBoxToBag(ItemBoxItemId(idSVC()).indexUI);
                    Sleep(400);
                }
            }
        }

        public void LuuToaDo()
        {
            if (TileMap.mapID != 27)
            {
                Go(27);
                Sleep();
            }
            if (TileMap.mapID != 27)
            {
                TileMap.LockMap(2000);
            }
            Pick(5, 1);
            Sleep();
            saveTD = 27;
        }

        public int idThucAn()
        {
            if (ThucAn() == 1)
            {
                return 23;
            }
            if (ThucAn() == 10)
            {
                return 24;
            }
            if (ThucAn() == 20)
            {
                return 25;
            }
            if (ThucAn() == 30)
            {
                return 26;
            }
            if (ThucAn() == 40)
            {
                return 27;
            }
            if (ThucAn() == 50)
            {
                return 29;
            }
            return 23;
        }

        public void Mua(int id, int sl)
        {
            Pick(4, 0);
            if (GameScr.arrItemGroceryLock == null)
            {
                UI(9);
                return;
            }
            for (int i = 0; i < GameScr.arrItemGroceryLock.Length; i++)
            {
                Item item = GameScr.arrItemGroceryLock[i];
                if (item != null && item.template.id == id)
                {
                    Service.gI().buyItem(item.typeUI, item.indexUI, sl);
                    if (taskId == 3 && !mainnull && index == 0)
                    {
                        Lock(2000);
                    }
                    else
                    {
                        Clock.LockBuy(2000);
                    }
                    break;
                }
            }
        }

        public void VuKhi()
        {
            if (Char.getMyChar().nClass.classId == 4 && ItemBodyItemID(Idvukhi()) == null)
            {
                Item item = ItemBagItemId(Idvukhi());
                if (item != null)
                {
                    Service.gI().useItem(item.indexUI);
                    Sleep(200);
                }
                else if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                }
                else if (Char.FindNpc(5) != null)
                {
                    Pick(5, -1);
                    if (me.arrItemBox == null)
                    {
                        UI(4);
                    }
                    if (ItemBoxItemId(Idvukhi()) != null)
                    {
                        Service.gI().itemBoxToBag(ItemBoxItemId(Idvukhi()).indexUI);
                        Sleep();
                    }
                }
            }
            else if (Char.getMyChar().nClass.classId == 1 && ItemBodyItemID(Idvukhi()) == null)
            {
                Item item2 = ItemBagItemId(Idvukhi());
                if (item2 != null)
                {
                    Service.gI().useItem(item2.indexUI);
                    Sleep(200);
                }
                else if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                }
                else if (Char.FindNpc(5) != null)
                {
                    Pick(5, -1);
                    if (me.arrItemBox == null)
                    {
                        UI(4);
                    }
                    if (ItemBoxItemId(Idvukhi()) != null)
                    {
                        Service.gI().itemBoxToBag(ItemBoxItemId(Idvukhi()).indexUI);
                        Sleep();
                    }
                }
            }
            else if (Char.getMyChar().nClass.classId == 2 && ItemBodyItemID(Idvukhi()) == null)
            {
                Item item3 = ItemBagItemId(Idvukhi());
                if (item3 != null)
                {
                    Service.gI().useItem(item3.indexUI);
                    Sleep(200);
                }
                else if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                }
                else if (Char.FindNpc(5) != null)
                {
                    Pick(5, -1);
                    if (me.arrItemBox == null)
                    {
                        UI(4);
                    }
                    if (ItemBoxItemId(Idvukhi()) != null)
                    {
                        Service.gI().itemBoxToBag(ItemBoxItemId(Idvukhi()).indexUI);
                        Sleep();
                    }
                }
            }
            else if (Char.getMyChar().nClass.classId == 6 && ItemBodyItemID(Idvukhi()) == null)
            {
                Item item4 = ItemBagItemId(Idvukhi());
                if (item4 != null)
                {
                    Service.gI().useItem(item4.indexUI);
                    Sleep(200);
                }
                else if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                }
                else if (Char.FindNpc(5) != null)
                {
                    Pick(5, -1);
                    if (me.arrItemBox == null)
                    {
                        UI(4);
                    }
                    if (ItemBoxItemId(Idvukhi()) != null)
                    {
                        Service.gI().itemBoxToBag(ItemBoxItemId(Idvukhi()).indexUI);
                        Sleep();
                    }
                }
            }
            else if (Char.getMyChar().nClass.classId == 3 && ItemBodyItemID(Idvukhi()) == null)
            {
                Item item5 = ItemBagItemId(Idvukhi());
                if (item5 != null)
                {
                    Service.gI().useItem(item5.indexUI);
                    Sleep(200);
                }
                else if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                }
                else if (Char.FindNpc(5) != null)
                {
                    Pick(5, -1);
                    if (me.arrItemBox == null)
                    {
                        UI(4);
                    }
                    if (ItemBoxItemId(Idvukhi()) != null)
                    {
                        Service.gI().itemBoxToBag(ItemBoxItemId(Idvukhi()).indexUI);
                        Sleep();
                    }
                }
            }
            else
            {
                if (Char.getMyChar().nClass.classId != 5 || ItemBodyItemID(119) != null)
                {
                    return;
                }
                Item item6 = ItemBagItemId(119);
                if (item6 != null)
                {
                    Service.gI().useItem(item6.indexUI);
                    Sleep(200);
                }
                else if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                }
                else if (Char.FindNpc(5) != null)
                {
                    Pick(5, -1);
                    if (me.arrItemBox == null)
                    {
                        UI(4);
                    }
                    if (ItemBoxItemId(119) != null)
                    {
                        Service.gI().itemBoxToBag(ItemBoxItemId(119).indexUI);
                        Sleep();
                    }
                }
            }
        }

        public bool isTruong()
        {
            if (TileMap.mapID != 1 && TileMap.mapID != 72)
            {
                return TileMap.mapID == 27;
            }
            return true;
        }

        public void UseKiemGo()
        {
            if (ItemBodyItemID(194) != null)
            {
                return;
            }
            if (ItemBagItemId(194) != null)
            {
                Service.gI().useItem(ItemBagItemId(194).indexUI);
                Sleep();
                return;
            }
            if (!Char.getMyChar().isLang())
            {
                Handle.Die();
                Sleep(500);
            }
            if (Char.FindNpc(5) != null)
            {
                Pick(5, -1);
                if (me.arrItemBox == null)
                {
                    UI(4);
                }
                if (ItemBoxItemId(194) != null)
                {
                    Service.gI().itemBoxToBag(ItemBoxItemId(194).indexUI);
                    Sleep(400);
                }
            }
        }

        public void upPotential()
        {
            if (mSystem.currentTimeMillis() - timeuptn < 1000)
            {
                return;
            }
            if (Char.getMyChar().nClass.classId == 1)
            {
                if (Char.getMyChar().clevel <= 10)
                {
                    if (Char.getMyChar().pPoint > 0)
                    {
                        if (Char.getMyChar().potential[2] < 35)
                        {
                            Service.gI().upPotential(2, (Char.getMyChar().pPoint >= 35 - Char.getMyChar().potential[2]) ? (35 - Char.getMyChar().potential[2]) : Char.getMyChar().pPoint);
                            Sleep(100);
                        }
                        else
                        {
                            Service.gI().upPotential(0, Char.getMyChar().pPoint);
                            Sleep(100);
                        }
                    }
                }
                else if (Char.getMyChar().clevel >= 11 && Char.getMyChar().pPoint > 0 && (taskId != 9 || mainnull || index != 1))
                {
                    if (Char.getMyChar().potential[2] < 50)
                    {
                        Service.gI().upPotential(2, (Char.getMyChar().pPoint >= 50 - Char.getMyChar().potential[2]) ? (50 - Char.getMyChar().potential[2]) : Char.getMyChar().pPoint);
                        Sleep(100);
                    }
                    else
                    {
                        Service.gI().upPotential(0, Char.getMyChar().pPoint);
                        Sleep(100);
                    }
                }
            }
            else if (Char.getMyChar().nClass.classId == 6)
            {
                if (Char.getMyChar().clevel <= 10)
                {
                    if (Char.getMyChar().pPoint > 0)
                    {
                        if (Char.getMyChar().potential[2] < 35)
                        {
                            Service.gI().upPotential(2, (Char.getMyChar().pPoint >= 35 - Char.getMyChar().potential[2]) ? (35 - Char.getMyChar().potential[2]) : Char.getMyChar().pPoint);
                            Sleep(100);
                        }
                        else
                        {
                            Service.gI().upPotential(3, Char.getMyChar().pPoint);
                            Sleep(100);
                        }
                    }
                }
                else if (Char.getMyChar().clevel >= 11 && Char.getMyChar().pPoint > 0 && (taskId != 9 || mainnull || index != 1))
                {
                    if (Char.getMyChar().potential[2] < 50)
                    {
                        Service.gI().upPotential(2, (Char.getMyChar().pPoint >= 50 - Char.getMyChar().potential[2]) ? (50 - Char.getMyChar().potential[2]) : Char.getMyChar().pPoint);
                        Sleep(100);
                    }
                    else
                    {
                        Service.gI().upPotential(3, Char.getMyChar().pPoint);
                        Sleep(100);
                    }
                }
            }
            else
            {
                if (Char.getMyChar().nClass.classId != 3)
                {
                    return;
                }
                if (Char.getMyChar().clevel <= 10)
                {
                    if (Char.getMyChar().pPoint > 0)
                    {
                        if (Char.getMyChar().potential[2] < 35)
                        {
                            Service.gI().upPotential(2, (Char.getMyChar().pPoint >= 35 - Char.getMyChar().potential[2]) ? (35 - Char.getMyChar().potential[2]) : Char.getMyChar().pPoint);
                            Sleep(100);
                        }
                        else
                        {
                            Service.gI().upPotential(0, Char.getMyChar().pPoint);
                            Sleep(100);
                        }
                    }
                }
                else if (Char.getMyChar().clevel >= 11 && Char.getMyChar().pPoint > 0 && (taskId != 9 || mainnull || index != 1))
                {
                    if (Char.getMyChar().potential[2] < 50)
                    {
                        Service.gI().upPotential(2, (Char.getMyChar().pPoint >= 50 - Char.getMyChar().potential[2]) ? (50 - Char.getMyChar().potential[2]) : Char.getMyChar().pPoint);
                        Sleep(100);
                    }
                    else
                    {
                        Service.gI().upPotential(0, Char.getMyChar().pPoint);
                        Sleep(100);
                    }
                }
            }
        }

        public void TuDong()
        {
            bool flag = false;
            for (int i = 0; i < Char.getMyChar().vEff.size(); i++)
            {
                Effect effect = (Effect)Char.getMyChar().vEff.elementAt(i);
                if (effect != null && effect.template.id == EffidThucAn())
                {
                    flag = true;
                }
            }
            if (!flag && ItemBagItemId(idThucAn()) != null)
            {
                Service.gI().useItem(ItemBagItemId(idThucAn()).indexUI);
            }
            if (mSystem.currentTimeMillis() - bagsort >= 10000)
            {
                Service.gI().bagSort();
                bagsort = mSystem.currentTimeMillis();
            }
            if (mSystem.currentTimeMillis() - boxsort >= 10000 && me.arrItemBox != null)
            {
                Service.gI().boxSort();
                boxsort = mSystem.currentTimeMillis();
            }
            if ((taskId > 2 && taskId < 8) || (taskId == 2 && !mainnull && index > 0) || (taskId == 8 && !mainnull && index <= 1))
            {
                UseKiemGo();
            }
            if (taskId >= 9 && !GameScr.isPaintUpGrade)
            {
                VuKhi();
            }
            Char.isAFood = true;
            Char.aFoodValue = ThucAn();
            //upPotential();
        }

        public int EffidThucAn()
        {
            if (ThucAn() == 1)
            {
                return 0;
            }
            if (ThucAn() == 10)
            {
                return 1;
            }
            if (ThucAn() == 20)
            {
                return 2;
            }
            if (ThucAn() == 30)
            {
                return 3;
            }
            if (ThucAn() == 40)
            {
                return 4;
            }
            if (ThucAn() == 50)
            {
                return 28;
            }
            return 0;
        }

        public void upSkillTask()
        {
            if (Char.getMyChar().sPoint > 0)
            {
                if (Char.getMyChar().nClass.classId == 6)
                {
                    Service.gI().upSkill(46, 1);
                    Lock(1000);
                }
                else if (Char.getMyChar().nClass.classId == 3)
                {
                    Service.gI().upSkill(19, 1);
                    Lock(1000);
                }
            }
        }

        public void a()
        {
            if (taskId > 2 && ItemBagItemId(idThucAn()) == null && (taskId != 3 || !mainnull))
            {
                if (!Char.getMyChar().isLang())
                {
                    Handle.Die();
                    return;
                }
                Mua(idThucAn(), 2);
                Sleep();
            }
            Default();
            Start();
        }

        public static void Sleep(int milisSeconds = 1000)
        {
            lock (objSleep)
            {
                Monitor.Wait(objSleep, milisSeconds);
            }
        }

        public bool CheckPercentVuKhi()
        {
            int num = 0;
            for (int i = 0; i < Char.getMyChar().arrItemBag.Length; i++)
            {
                Item item = Char.getMyChar().arrItemBag[i];
                if (item != null && item.template.type == 26)
                {
                    num += item.getPecentUpgradeVuKhi(0);
                    if (num >= 80)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool CheckPercentClothes()
        {
            int num = 0;
            for (int i = 0; i < Char.getMyChar().arrItemBag.Length; i++)
            {
                Item item = Char.getMyChar().arrItemBag[i];
                if (item != null && item.template.type == 26)
                {
                    num += item.getPecentUpgradeDoLeft(0);
                    if (num >= 80)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void UI(int typeUI)
        {
            Service.gI().requestItem(typeUI);
            Clock.LockTypeUI(typeUI, 5000);
        }

        public int Idvukhi()
        {
            if (me.nClass.classId == 6)
            {
                return 119;
            }
            if (me.nClass.classId == 4)
            {
                return 109;
            }
            if (me.nClass.classId == 1)
            {
                return 94;
            }
            if (me.nClass.classId == 2)
            {
                return 65;
            }
            if (me.nClass.classId == 3)
            {
                return 99;
            }
            return 194;
        }

        public int idSVC()
        {
            if (Char.getMyChar().nClass.classId == 4)
            {
                return 67;
            }
            if (Char.getMyChar().nClass.classId == 1)
            {
                return 40;
            }
            if (Char.getMyChar().nClass.classId == 6)
            {
                return 85;
            }
            if (Char.getMyChar().nClass.classId == 3)
            {
                return 58;
            }
            return 0;
        }
    }
}