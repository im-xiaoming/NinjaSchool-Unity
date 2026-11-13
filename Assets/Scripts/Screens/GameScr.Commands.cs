using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    public void doTiemnangMe()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 2;
        doMiniMenuInforMe();
    }

    public void doInfo()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 3;
        doMiniMenuInforMe();
    }

    public void doTrangbi()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 4;
        doMiniMenuInforMe();
    }

    public void doThucuoi()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 5;
        doMiniMenuInforMe();
    }

    public void doBiJuu()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 6;
        indexBijuuMenu = 0;
        doMiniMenuInforMe();
    }

    public void doBiJuu_Point()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 6;
        indexBijuuMenu = 1;
        doMiniMenuInforMe();
    }

    public void doUpPotential()
    {
        if ((indexTitle > 0 && indexTitle <= 4) || GameCanvas.isTouch)
        {
            GameCanvas.inputDlg.show(mResources.INPUT_POINT, cmdPotentialAdd, TField.INPUT_TYPE_NUMERIC);
        }
    }

    public void doAddFriend()
    {
        if (isPaintFriend)
        {
            GameCanvas.inputDlg.show(mResources.BE_FRIEND_WITH, cmdAddFriend, TField.INPUT_TYPE_ANY);
        }
    }

    public void doShowFindTeamUI()
    {
        resetButton();
        isPaintFindTeam = true;
        isLockKey = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
        Service.gI().openFindParty();
        refreshFindTeam();
    }

    public void doShowTeamUI()
    {
        resetButton();
        isPaintTeam = true;
        isLockKey = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
        refreshTeam();
    }

    public void doShowRankedListUI()
    {
        resetButton();
        isPaintRankedList = true;
        isLockKey = true;
        setPopupSize(175, 200);
        right = cmdCloseAll;
        left = (center = null);
        indexRow = 0;
    }

    public void doShowListUI()
    {
        resetButton();
        isPaintList = true;
        isLockKey = true;
        setPopupSize(175, 200);
        right = cmdCloseAll;
        left = (center = null);
        indexRow = 0;
    }

    public void doShowFriendUI()
    {
        resetButton();
        isPaintFriend = true;
        isLockKey = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
        left = new Command(mResources.LIST, 11044);
        center = null;
        indexRow = 0;
        Service.gI().requestFriend();
    }

    public void doShowEnemiesUI()
    {
        resetButton();
        isPaintEnemies = true;
        isLockKey = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
        left = new Command(mResources.LIST, 14017);
        center = null;
        indexRow = 0;
        Service.gI().requestEnemies();
    }

    private void doFocusbyTouch()
    {
        if (isPaintPopup() || isPaintUI() || isOpenUI() || !GameCanvas.isPointerClick)
        {
            return;
        }

        for (int i = 0; i < vMob.size(); i++)
        {
            Mob mob = (Mob)vMob.elementAt(i);
            if (mob.isPaint() && GameCanvas.isPointerInGame(mob.x - mob.w / 2, mob.y - mob.h, mob.w, mob.h) && GameCanvas.isPointerJustRelease)
            {
                Char.getMyChar().mobFocus = mob;
                Char.getMyChar().deFocusNPC();
                Char.getMyChar().charFocus = null;
                Char.getMyChar().itemFocus = null;
                Char.isManualFocus = true;
                onTargetClicked(mob);
                return;
            }
        }

        for (int i = 0; i < vNpc.size(); i++)
        {
            Npc npc = (Npc)vNpc.elementAt(i);
            if (npc.isPaint() && GameCanvas.isPointerInGame(npc.cx - npc.cw / 2, npc.cy - npc.ch, npc.cw, npc.ch) && GameCanvas.isPointerJustRelease)
            {
                Char.getMyChar().mobFocus = null;
                Char.getMyChar().deFocusNPC();
                Char.getMyChar().npcFocus = npc;
                Char.getMyChar().charFocus = null;
                Char.getMyChar().itemFocus = null;
                Char.isManualFocus = true;
                onTargetClicked(npc);
                return;
            }
        }

        for (int i = 0; i < vCharInMap.size(); i++)
        {
            Char @char = (Char)vCharInMap.elementAt(i);
            if (@char.isPaint() && !@char.isNhanbanz())
            {
                if (TileMap.mapID == 111 && Char.getMyChar().cTypePk == 0)
                {
                    return;
                }

                if (GameCanvas.isPointerInGame(@char.cx - @char.cw / 2, @char.cy - @char.ch, @char.cw, @char.ch) && GameCanvas.isPointerJustRelease)
                {
                    Char.getMyChar().mobFocus = null;
                    Char.getMyChar().deFocusNPC();
                    Char.getMyChar().charFocus = @char;
                    Char.getMyChar().itemFocus = null;
                    Char.isManualFocus = true;
                    onTargetClicked(@char);
                    return;
                }
            }
        }

        for (int i = 0; i < vItemMap.size(); i++)
        {
            ItemMap itemMap = (ItemMap)vItemMap.elementAt(i);
            if (GameCanvas.isPointerInGame(itemMap.x - itemMap.sizeItem / 2, itemMap.y - itemMap.sizeItem, itemMap.sizeItem, itemMap.sizeItem) && GameCanvas.isPointerJustRelease)
            {
                Char.getMyChar().mobFocus = null;
                Char.getMyChar().deFocusNPC();
                Char.getMyChar().charFocus = null;
                Char.getMyChar().itemFocus = itemMap;
                Char.isManualFocus = true;
                onTargetClicked(itemMap);
                break;
            }
        }
    }

    public void doUseHP()
    {
        int num = (int)(mSystem.getCurrentTimeMillis() / 1000);
        for (int i = 0; i < Char.getMyChar().vEff.size(); i++)
        {
            Effect effect = (Effect)Char.getMyChar().vEff.elementAt(i);
            if (effect.template.id == 21 && effect.timeLenght - (num - effect.timeStart) >= 2)
            {
                return;
            }
        }

        if (Char.getMyChar().doUsePotion(16))
        {
            return;
        }

        for (int j = 0; j < Char.getMyChar().arrItemBag.Length; j++)
        {
            if (Char.getMyChar().arrItemBag[j] != null && Char.getMyChar().arrItemBag[j].template.type == 16)
            {
                InfoMe.addInfo(mResources.NOT_ENOUGH_LEVEL);
                return;
            }
        }

        if (auto != 1)
        {
            InfoMe.addInfo(mResources.HP_EMPTY);
        }
    }

    public void doUseMP()
    {
        if (Char.getMyChar().doUsePotion(17))
        {
            return;
        }

        for (int i = 0; i < Char.getMyChar().arrItemBag.Length; i++)
        {
            if (Char.getMyChar().arrItemBag[i] != null && Char.getMyChar().arrItemBag[i].template.type == 17)
            {
                InfoMe.addInfo(mResources.NOT_ENOUGH_LEVEL);
                return;
            }
        }

        if (auto != 1)
        {
            InfoMe.addInfo(mResources.MP_EMPTY);
        }
    }

    private void doFire(bool isFireByShortCut)
    {
        if (Char.getMyChar().statusMe == 14 || !isAttack())
        {
            return;
        }

        if (isMobSameParty())
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command(mResources.CAT_KEO, 151301));
            GameCanvas.menu.startAt(myVector, 0);
            return;
        }

        if (Char.getMyChar().mobFocus != null && Char.getMyChar().mobFocus.getTemplate().mobTemplateId == 144 && TileMap.mapID == 130)
        {
            MyVector myVector2 = new MyVector();
            myVector2.addElement(new Command(mResources.CHO_KEO, 151301));
            GameCanvas.menu.startAt(myVector2, 0);
            return;
        }

        Char.getMyChar().setSkillPaint(sks[Char.getMyChar().myskill.template.id], 0);
        Char.getMyChar().currentFireByShortcut = isFireByShortCut;
        if (Char.getMyChar().isWolf)
        {
            Char.getMyChar().isWolf = false;
            Char.getMyChar().timeSummon = mSystem.getCurrentTimeMillis();
            ServerEffect.addServerEffect(60, Char.getMyChar(), 1);
        }

        if (Char.getMyChar().isHaveMoto() && !Char.getMyChar().isMotoBehind)
        {
            Char.getMyChar().isMoto = false;
            Char.getMyChar().isMotoBehind = true;
            ServerEffect.addServerEffect(60, Char.getMyChar(), 1);
        }

        if (Char.getMyChar().isHaveNewMount() && !Char.getMyChar().isMotoBehind)
        {
            Char.getMyChar().isNewMount = false;
            Char.getMyChar().isMotoBehind = true;
            ServerEffect.addServerEffect(60, Char.getMyChar(), 1);
        }

        if (!isFireByShortCut)
        {
            Char.getMyChar().lastNormalSkill = Char.getMyChar().myskill;
        }
    }

    private void doUPPOINT()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.UPPOINT, 11064));
        myVector.addElement(new Command(mResources.UPPOINTS, 11065));
        GameCanvas.menu.startAt(myVector, 0);
    }

    public void doLuyenThach()
    {
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        int num4 = 0;
        for (int i = 0; i < arrItemUpPeal.Length; i++)
        {
            Item item = arrItemUpPeal[i];
            if (item != null)
            {
                if (item.template.id == 455)
                {
                    num++;
                }
                else if (item.template.id == 456)
                {
                    num2++;
                }
                else if (item.template.type == 26)
                {
                    num3++;
                    num4 = item.template.id;
                }
            }
        }

        if (num3 > 1)
        {
            GameCanvas.startOKDlg(mResources.ONLY_LUYENTHACH3);
        }
        else if (num > 9 || num2 > 9 || (num4 >= 10 && (num > 3 || num2 > 3)))
        {
            GameCanvas.startOKDlg(mResources.MAX_LUYENTHACH1);
        }
        else if (num + num2 < 3)
        {
            GameCanvas.startOKDlg(mResources.NOT_ENOUGH_LUYENTHACH1);
        }
        else if ((num4 == 10 && num2 == 3) || (num4 == 11 && num == 3))
        {
            GameCanvas.startOKDlg(mResources.ONLY_LUYENTHACH4);
        }
        else
        {
            Service.gI().luyenthach(arrItemUpPeal);
        }
    }

    public void doCloseAlert()
    {
        isPaintAlert = false;
        textsTitle = null;
        texts = null;
        center = null;
        resetButton();
    }

    public void doOpenUI(int typeUI)
    {
        int num = ((!Main.isPC) ? indexSize : 0);
        indexTitle = 0;
        right = cmdCloseAll;
        switch (typeUI)
        {
            case 52:
                isPaintStore = true;
                indexMenu = 52;
                if (arrItemFashion == null)
                {
                    Service.gI().requestItem(32);
                }

                break;
            case 40:
                isPaintAuto = true;
                indexTitle = 1;
                indexRow = 0;
                updateCommandForUI();
                break;
            case 38:
                arrItemSprin = null;
                isPaintLuckySpin = true;
                indexTitle = 1;
                updateCommandForUI();
                break;
            case 14:
                isPaintStore = true;
                indexMenu = 0;
                if (arrItemStore == null)
                {
                    Service.gI().requestItem(14);
                }

                break;
            case 35:
                isPaintEliteShop = true;
                indexMenu = 0;
                if (arrItemElites == null)
                {
                    Service.gI().requestItem(35);
                }

                break;
            case 15:
                isPaintStore = true;
                indexMenu = 1;
                if (arrItemBook == null)
                {
                    Service.gI().requestItem(15);
                }

                break;
            case 32:
                isPaintStore = true;
                indexMenu = 2;
                if (arrItemFashion == null)
                {
                    Service.gI().requestItem(32);
                }

                break;
            case 34:
                isPaintStore = true;
                indexMenu = 3;
                if (arrItemClanShop == null)
                {
                    Service.gI().requestItem(34);
                }

                break;
            case 2:
                indexMenu = 0;
                isPaintWeapon = true;
                if (arrItemWeapon == null)
                {
                    Service.gI().requestItem(2);
                }

                break;
            case 20:
                indexMenu = 0;
                isPaintNonNam = true;
                if (arrItemNonNam == null)
                {
                    Service.gI().requestItem(20);
                }

                break;
            case 21:
                indexMenu = 0;
                isPaintNonNu = true;
                if (arrItemNonNu == null)
                {
                    Service.gI().requestItem(21);
                }

                break;
            case 22:
                indexMenu = 0;
                isPaintAoNam = true;
                if (arrItemAoNam == null)
                {
                    Service.gI().requestItem(22);
                }

                break;
            case 23:
                indexMenu = 0;
                isPaintAoNu = true;
                if (arrItemAoNu == null)
                {
                    Service.gI().requestItem(23);
                }

                break;
            case 24:
                indexMenu = 0;
                isPaintGangTayNam = true;
                if (arrItemGangTayNam == null)
                {
                    Service.gI().requestItem(24);
                }

                break;
            case 25:
                indexMenu = 0;
                isPaintGangTayNu = true;
                if (arrItemGangTayNu == null)
                {
                    Service.gI().requestItem(25);
                }

                break;
            case 26:
                indexMenu = 0;
                isPaintQuanNam = true;
                if (arrItemQuanNam == null)
                {
                    Service.gI().requestItem(26);
                }

                break;
            case 27:
                indexMenu = 0;
                isPaintQuanNu = true;
                if (arrItemQuanNu == null)
                {
                    Service.gI().requestItem(27);
                }

                break;
            case 28:
                indexMenu = 0;
                isPaintGiayNam = true;
                if (arrItemGiayNam == null)
                {
                    Service.gI().requestItem(28);
                }

                break;
            case 29:
                indexMenu = 0;
                isPaintGiayNu = true;
                if (arrItemGiayNu == null)
                {
                    Service.gI().requestItem(29);
                }

                break;
            case 16:
                indexMenu = 0;
                isPaintLien = true;
                if (arrItemLien == null)
                {
                    Service.gI().requestItem(16);
                }

                break;
            case 17:
                indexMenu = 0;
                isPaintNhan = true;
                if (arrItemNhan == null)
                {
                    Service.gI().requestItem(17);
                }

                break;
            case 18:
                indexMenu = 0;
                isPaintNgocBoi = true;
                if (arrItemNgocBoi == null)
                {
                    Service.gI().requestItem(18);
                }

                break;
            case 19:
                indexMenu = 0;
                isPaintPhu = true;
                if (arrItemPhu == null)
                {
                    Service.gI().requestItem(19);
                }

                break;
            case 6:
                indexMenu = 0;
                isPaintStack = true;
                if (arrItemStack == null)
                {
                    Service.gI().requestItem(6);
                }

                break;
            case 7:
                indexMenu = 0;
                isPaintStackLock = true;
                if (arrItemStackLock == null)
                {
                    Service.gI().requestItem(7);
                }

                break;
            case 8:
                indexMenu = 0;
                isPaintGrocery = true;
                if (arrItemGrocery == null)
                {
                    Service.gI().requestItem(8);
                }

                break;
            case 9:
                indexMenu = 0;
                isPaintGroceryLock = true;
                if (arrItemGroceryLock == null)
                {
                    Service.gI().requestItem(9);
                }

                break;
            case 4:
                indexMenu = 0;
                isPaintBox = true;
                if (Char.getMyChar().arrItemBox == null)
                {
                    Service.gI().requestItem(4);
                }

                break;
            case 10:
                indexMenu = 0;
                isPaintUpGrade = true;
                arrItemUpGrade = new Item[18];
                break;
            case 36:
                indexMenu = 0;
                openItemSaleUI();
                break;
            case 37:
                isPaintAuctionBuy = true;
                updateCommandForUI();
                break;
            case 33:
                indexMenu = 0;
                isPaintConvert = true;
                arrItemConvert = new Item[3];
                break;
            case 31:
                indexMenu = 0;
                isPaintUpGrade = true;
                isPaintUpGradeGold = true;
                arrItemUpGrade = new Item[18];
                break;
            case 11:
                indexMenu = 0;
                isPaintUpPearl = true;
                isTypeXu = true;
                arrItemUpPeal = new Item[24];
                break;
            case 12:
                indexMenu = 0;
                isPaintUpPearl = true;
                isTypeXu = false;
                arrItemUpPeal = new Item[24];
                break;
            case 13:
                indexMenu = 0;
                setPopupSize(175, 180);
                isPaintSplit = true;
                arrItemSplit = new Item[24];
                break;
            case 43:
                indexMenu = 0;
                isPaintLuyenThach = true;
                arrItemUpPeal = new Item[24];
                break;
            case 44:
                indexMenu = 0;
                isPaintTinhluyen = true;
                arrItemSplit = new Item[24];
                break;
            case 45:
                indexMenu = 0;
                isPaintDichChuyen = true;
                arrItemSplit = new Item[24];
                break;
            case 46:
                indexMenu = 0;
                isPaintLuyenNgoc = true;
                arrItemSplit = new Item[24];
                break;
            case 47:
                indexMenu = 0;
                isPaintKhamNgoc = true;
                arrItemUpGrade = new Item[18];
                break;
            case 48:
                indexMenu = 0;
                isPaintGiaoDo = true;
                arrItemSplit = new Item[18];
                break;
            case 49:
                indexMenu = 0;
                openGotNgocUI();
                break;
            case 50:
                indexMenu = 0;
                openThaoNgocUI();
                break;
        }

        setPopupSize(175, 200);
    }

    private void doShowClan()
    {
        arrClanInfo = (arrClanDongGop = null);
        isPaintClan = true;
        Out.println("xem clan");
        isRequestMember = true;
        isSortClanByPointWeek = false;
        arrClanInfo = (arrClanDongGop = null);
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        updateCommandForUI();
        right = cmdCloseAll;
        Service.gI().requestClanInfo();
    }

    private void doUnfocusChar()
    {
        Char.getMyChar().charFocus = null;
        Char.isManualFocus = false;
        cLastFocusID = -1;
        isPaintCharInMap = false;
        resetButton();
    }

    private void doAutoSelect()
    {
        if (indexRow == 0)
        {
            Char.isAHP = !Char.isAHP;
            if (typeActive == 1)
            {
                Char.isAHP = false;
                InfoMe.addInfo(mResources.AOPENACC, 20, mFont.tahoma_7_yellow);
            }
        }
        else if (indexRow == 1)
        {
            Char.isAMP = !Char.isAMP;
            if (typeActive == 1)
            {
                Char.isAMP = false;
                InfoMe.addInfo(mResources.AOPENACC, 20, mFont.tahoma_7_yellow);
            }
        }
        else if (indexRow == 2)
        {
            Char.isAFood = !Char.isAFood;
            if (typeActive == 1)
            {
                Char.isAFood = false;
                InfoMe.addInfo(mResources.AOPENACC, 20, mFont.tahoma_7_yellow);
            }
        }
        else if (indexRow == 3)
        {
            Char.isABuff = !Char.isABuff;
        }
        else if (indexRow == 4)
        {
            Char.isAPickYen = !Char.isAPickYen;
            if (Char.isAPickYen)
            {
                Char.isAPickYHM = (Char.isAPickYHMS = (Char.isANoPick = false));
            }
        }
        else if (indexRow == 5)
        {
            Char.isAPickYHM = !Char.isAPickYHM;
            if (Char.isAPickYHM)
            {
                Char.isAPickYen = (Char.isAPickYHMS = (Char.isANoPick = false));
            }
        }
        else if (indexRow == 6)
        {
            Char.isAPickYHMS = !Char.isAPickYHMS;
            if (Char.isAPickYHMS)
            {
                Char.isAPickYen = (Char.isAPickYHM = (Char.isANoPick = false));
            }
        }
        else if (indexRow == 7)
        {
            Char.isANoPick = !Char.isANoPick;
            if (Char.isANoPick)
            {
                Char.isAPickYen = (Char.isAPickYHM = (Char.isAPickYHMS = false));
            }
        }
    }

    private void doviewCardsInfo()
    {
        if (arrItemSprin != null)
        {
            Item item = new Item();
            item.template = ItemTemplates.get(arrItemSprin[indexSelect]);
            updateItemInfo(38, item);
        }
    }

    private void doCharFocusList()
    {
        Char @char = (Char)vCharInMap.elementAt(indexRow);
        if (!@char.isNhanbanz())
        {
            cLastFocusID = @char.charID;
            Char.getMyChar().mobFocus = null;
            Char.getMyChar().deFocusNPC();
            Char.getMyChar().itemFocus = null;
            Char.isManualFocus = true;
            isPaintCharInMap = false;
            Char.getMyChar().charFocus = @char;
        }

        resetButton();
    }

    private void doActionSaleOk(Item item)
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (!text.Trim().Equals(string.Empty))
        {
            int num = 0;
            try
            {
                num = int.Parse(text);
            }
            catch (Exception)
            {
                GameCanvas.inputDlg.hide();
                return;
            }

            if (num <= 0)
            {
                GameCanvas.inputDlg.hide();
                return;
            }

            if (num > item.quantity)
            {
                GameCanvas.startOKDlg(mResources.NOT_ENOUGH_QUANTITY);
                return;
            }

            GameCanvas.inputDlg.hide();
            GameCanvas.startYesNoDlg(mResources.CONFIRMSALEITEM, new Command(mResources.YES, 11058, item), new Command(mResources.NO, 1));
        }
    }

    private void doActionBuy(Item item)
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.msgdlg.setInfo(mResources.NOT_INPUT_QUANTITY, null, new Command(mResources.CLOSE, 1), null);
            GameCanvas.msgdlg.show();
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (num <= 0)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        Service.gI().buyItem(item.typeUI, item.indexUI, num);
        GameCanvas.inputDlg.hide();
    }

    public void doRescuedOtherChar(int charID)
    {
        if (mSystem.currentTimeMillis() - timeLastBuff > 500)
        {
            Service.gI().buffLive(charID);
            timeLastBuff = mSystem.currentTimeMillis();
        }

        if ((TileMap.tileTypeAtPixel(Char.getMyChar().cx, Char.getMyChar().cy) & TileMap.T_TOP) == TileMap.T_TOP)
        {
            Char.getMyChar().setSkillPaint(sks[49], 0);
        }
        else
        {
            Char.getMyChar().setSkillPaint(sks[49], 1);
        }
    }

    public void perform(int idAction, object p)
    {
        if (idAction == 9999)
        {
            GameCanvas.instance.resetToLoginScr();
        }
    }

    public void doClickCaptcha(sbyte typeKey)
    {
        for (int i = 0; i < arrowCaptcha.Length; i++)
        {
            if (i != arrowCaptcha.Length - 1)
            {
                arrowCaptcha[i] = arrowCaptcha[i + 1];
                continue;
            }

            arrowCaptcha[i] = typeKey;
            Service.gI().send_Captcha(typeKey);
        }
    }

    public void doBijuuUpPoint()
    {
        if ((indexTitle > 0 && indexTitle <= 4) || GameCanvas.isTouch)
        {
            GameCanvas.inputDlg.show(mResources.INPUT_POINT, cmdBijuuAddPoint, TField.INPUT_TYPE_NUMERIC);
        }
    }

    public void actionBuy(Item item)
    {
        Command ok = new Command(mResources.ACCEPT, 11055, item);
        GameCanvas.inputDlg.show(mResources.INPUT_QUANTITY, ok, TField.INPUT_TYPE_NUMERIC);
    }

    public void actionSale(Item item)
    {
        GameCanvas.inputDlg.tfInput.getText();
        if (item.quantity > 1)
        {
            Command ok = new Command(mResources.ACCEPT, 110562, item);
            GameCanvas.inputDlg.show(mResources.INPUT_QUANTITY, ok, TField.INPUT_TYPE_NUMERIC);
        }
        else
        {
            GameCanvas.startYesNoDlg(mResources.CONFIRMSALEITEM, new Command(mResources.YES, 11061, item), new Command(mResources.NO, 1));
        }
    }

    public void actionCoinOut()
    {
        Command ok = new Command(mResources.ACCEPT, 11042);
        GameCanvas.inputDlg.show(mResources.INPUT_COIN, ok, TField.INPUT_TYPE_NUMERIC);
    }

    public void actionCoinTrade()
    {
        Command ok = new Command(mResources.ACCEPT, 110361);
        GameCanvas.inputDlg.show(mResources.INPUT_COIN, ok, TField.INPUT_TYPE_NUMERIC);
    }

    public void actionCoinIn()
    {
        Command ok = new Command(mResources.ACCEPT, 11043);
        GameCanvas.inputDlg.show(mResources.INPUT_COIN, ok, TField.INPUT_TYPE_NUMERIC);
    }

    private void actTrangBiCenter()
    {
        indexRow = 0;
        scrMain.clear();
        if (typeViewInfo == 0)
        {
            typeViewInfo = 1;
        }
        else
        {
            typeViewInfo = 0;
        }
    }

    public void actionPerform(int idAction, object p)
    {
        Item item = null;
        string empty = string.Empty;
        MyVector myVector = null;
        switch (idAction)
        {
            case 2003:
                if (indextabTrangbi == 0)
                {
                    indextabTrangbi = 16;
                    cmdTrangbi2.caption = mResources.tbi1;
                    break;
                }

                indextabTrangbi = 0;
                cmdTrangbi2.caption = mResources.tbi2;
                if (!GameCanvas.isTouch)
                {
                    left = new Command(mResources.SELECT, 11082);
                }

                break;
            case 2002:
                GameCanvas.endDlg();
                break;
            case 2001:
                GameCanvas.endDlg(); // offline fix: the "bỏ vật phẩm" yes/no dialog stayed open after "Có"
                Service.gI().throwItem(indexSelect);
                break;
            case 110022:
                gI().doOpenUI(4);
                break;
            case 110023:
                Handle.Paint("Tự sát");
                Handle.Die();
                break;
            case 110024:
            {
                Npc npc2 = Char.FindNpc(13);
                if (Math.abs(Char.getMyChar().cx - npc2.cx) > 22 || Math.abs(Char.getMyChar().cy - npc2.cy) > 22)
                {
                    Char.Move(npc2.cx, npc2.cy);
                }

                Service.gI().openUIZone();
                break;
            }

            case 909090:
                doOpenUI(38);
                break;
            case 1:
                GameCanvas.endDlg();
                break;
            case 2:
                GameCanvas.endDlg();
                left = (center = null);
                updateCommandForUI();
                break;
            case 3:
                doCloseAlert();
                break;
            case 1000:
                Service.gI().rewardPB();
                resetButton();
                break;
            case 2000:
                Service.gI().rewardCT();
                resetButton();
                break;
            case 11000:
                actMenu();
                break;
            case 110001:
                doMenuInforMe();
                break;
            case 1100011:
                doBag();
                break;
            case 1100012:
                doskill();
                break;
            case 1100013:
                doTiemnangMe();
                break;
            case 1100014:
                doInfo();
                break;
            case 1100015:
                doTrangbi();
                break;
            case 1100016:
                doThucuoi();
                break;
            case 1100017:
                doBiJuu();
                break;
            case 110002:
                doShowMap();
                break;
            case 110003:
                doMenusynthesis();
                break;
            case 1100032:
                doShowTaskUI();
                break;
            case 1100033:
                domenuClan();
                break;
            case 1100034:
                domenuLucky_Draw();
                break;
            case 110004:
                actMenu4();
                break;
            case 1100041:
                actMenu41();
                break;
            case 110005:
                doOpenUI(14);
                break;
            case 110006:
                actOrder();
                break;
            case 1100061:
                doShowFindTeamUI();
                break;
            case 1100062:
                doShowTeamUI();
                break;
            case 1100063:
                doShowFriendUI();
                break;
            case 1100064:
                doShowEnemiesUI();
                break;
            case 1100065:
                actsubMenuOrder();
                break;
            case 11000651:
                actsubMenuOrder_PK(1);
                break;
            case 11000652:
                actsubMenuOrder_PK(2);
                break;
            case 11000653:
                actsubMenuOrder_PK(3);
                break;
            case 1100067:
                domenuPrivateLock();
                break;
            case 11000671:
                GameCanvas.startYesNoDlg(mResources.ACTIVE_PROTECT_ACC, 88836, null, 8882, null);
                break;
            case 11000672:
                GameCanvas.inputDlg.tfInput.setMaxTextLenght(6);
                GameCanvas.inputDlg.show(mResources.INPUT_PRIVATE_PASS, new Command(mResources.ACCEPT, GameCanvas.instance, 88837, null), TField.INPUT_TYPE_NUMERIC);
                break;
            case 11000673:
                GameCanvas.input2Dlg.setTitle(mResources.OLD_PASS, mResources.NEW_PASS);
                GameCanvas.input2Dlg.tfInput.setMaxTextLenght(6);
                GameCanvas.input2Dlg.tfInput2.setMaxTextLenght(6);
                GameCanvas.input2Dlg.show(mResources.INPUT_PRIVATE_PASS, new Command(mResources.CLOSE, GameCanvas.instance, 8882, null), new Command(mResources.ACCEPT, GameCanvas.instance, 88838, null), TField.INPUT_TYPE_NUMERIC, TField.INPUT_TYPE_NUMERIC);
                break;
            case 11000674:
                GameCanvas.inputDlg.tfInput.setMaxTextLenght(6);
                GameCanvas.inputDlg.show(mResources.INPUT_PRIVATE_PASS, new Command(mResources.ACCEPT, GameCanvas.instance, 88839, null), TField.INPUT_TYPE_NUMERIC);
                break;
            case 1100068:
                doOpenUI(40);
                break;
            case 11000661:
                indexMenu = 0;
                doShowClan();
                break;
            case 11000662:
                indexMenu = 1;
                doShowClan();
                Service.gI().requestClanMember();
                break;
            case 11000663:
                indexMenu = 2;
                doShowClan();
                Service.gI().requestClanItem();
                break;
            case 11000664:
                indexMenu = 3;
                doShowClan();
                Service.gI().requestClanLog();
                break;
            case 11000665:
                isViewClanInvite = !isViewClanInvite;
                if (isViewClanInvite)
                {
                    RMS.saveRMSInt(Char.getMyChar().cName + "vci", 1);
                }
                else
                {
                    RMS.saveRMSInt(Char.getMyChar().cName + "vci", 0);
                }

                break;
            case 11000666:
                indexMenu = 4;
                doShowClan();
                Service.gI().requestClanItem();
                break;
            case 110007:
                doOpenUI(7);
                break;
            case 110008:
                doOpenUI(6);
                break;
            case 110009:
                doOpenUI(9);
                break;
            case 110010:
                doOpenUI(8);
                break;
            case 110011:
                doOpenUI(10);
                break;
            case 110012:
                doOpenUI(11);
                break;
            case 110013:
                doOpenUI(12);
                break;
            case 110014:
                doOpenUI(4);
                break;
            case 110015:
                doOpenUI(13);
                break;
            case 110016:
                Service.gI().openUIZone();
                break;
            case 110017:
                openUITrade();
                break;
            case 110019:
                changeTaskInfo();
                break;
            case 11001:
                Char.getMyChar().findNextFocusByKey();
                break;
            case 11002:
                actAddFriendAccept();
                break;
            case 11003:
                actSkillUpPoint();
                break;
            case 11004:
                actView(25);
                break;
            case 11005:
                actGangTayNuBuy();
                break;
            case 110051:
                actBuy(25);
                break;
            case 110052:
                actBuys(25);
                break;
            case 11006:
                actAddPotential();
                break;
            case 11007:
                actView(24);
                break;
            case 11008:
                actGangTayNamBuy();
                break;
            case 110081:
                actBuy(24);
                break;
            case 110082:
                actBuys(24);
                break;
            case 11009:
                actView(23);
                break;
            case 11010:
                actAoNuBuy();
                break;
            case 110101:
                actBuy(23);
                break;
            case 110102:
                actBuys(23);
                break;
            case 11011:
                actView(22);
                break;
            case 11012:
                actAoNamBuy();
                break;
            case 110121:
                actBuy(22);
                break;
            case 110122:
                actBuys(22);
                break;
            case 11013:
                actView(21);
                break;
            case 11014:
                actNonNuBuy();
                break;
            case 110141:
                actBuy(21);
                break;
            case 110142:
                actBuys(21);
                break;
            case 11015:
                actView(20);
                break;
            case 11016:
                actNonNamBuy();
                break;
            case 110161:
                actBuy(20);
                break;
            case 110162:
                actBuys(20);
                break;
            case 11017:
                actView(15);
                break;
            case 11018:
                actStoreLockBuy();
                break;
            case 110181:
                actBuy(15);
                break;
            case 110182:
                actBuys(15);
                break;
            case 11019:
                actView(14);
                break;
            case 11020:
                actStoreBuy();
                break;
            case 110201:
                actBuy(14);
                break;
            case 110202:
                actBuys(14);
                break;
            case 11021:
                svTitle = string.Empty;
                svAction = string.Empty;
                resetButton();
                break;
            case 11022:
                actBagSelectItem();
                break;
            case 110221:
                actBagSortItem();
                break;
            case 11023:
                actBagViewItemInfo();
                break;
            case 11024:
                actBagThrowItem();
                break;
            case 110244:
                actBagSplitItem();
                break;
            case 11025:
                doCloseItemInfo();
                break;
            case 11026:
                actBagUseItem();
                break;
            case 110261:
                actBagUseAllItem();
                break;
            case 110262:
                Item var9;
                if ((var9 = getItemFocus(3)) != null)
                {
                    addAutoUse((int)var9.template.id);
                }

                return;
            case 110263:
                Item var10;
                if ((var10 = getItemFocus(3)) != null)
                {
                    removeAutoUse(var10.template.id);
                }

                return;
            case 11027:
                actTradeSelectInList();
                break;
            case 11028:
                actTradeSelectInBag();
                break;
            case 11029:
                actTradeSelectItem();
                break;
            case 11030:
                actTradeViewItemInfo();
                break;
            case 11032:
                actTradeLock();
                break;
            case 11033:
                actTradeAccept();
                break;
            case 11034:
                actUpgradeMoveOut();
                break;
            case 11035:
                actSplitMoveOut();
                break;
            case 11036:
                actionCoinTrade();
                break;
            case 110361:
                actionCoinTradeAccept();
                break;
            case 11037:
                actTradeMoveOut();
                break;
            case 11038:
                actDead();
                GameCanvas.isPointerJustRelease = false;
                break;
            case 110382:
                Service.gI().returnTownFromDead();
                break;
            case 110383:
                Service.gI().wakeUpFromDead();
                break;
            case 110391:
                actmenuAttack(1);
                break;
            case 110392:
                actmenuAttack(2);
                break;
            case 110393:
                actmenuAttack(3);
                break;
            case 110394:
                actmenuAttack(4);
                break;
            case 110395:
                actmenuAttack(5);
                break;
            case 110396:
                actmenuAttack(6);
                break;
            case 110397:
                actmenuAttack(7);
                break;
            case 110398:
                actmenuAttack(8);
                break;
            case 110399:
                actmenuAttack(9);
                break;
            case 1103991:
                actmenuAttack(10);
                break;
            case 11040:
                actleftItemInfo();
                break;
            case 11041:
                actrightItemInfo();
                break;
            case 11042:
                actCoinOut();
                break;
            case 11043:
                actCoinIn();
                break;
            case 11044:
                domenuFriendLeft();
                break;
            case 110441:
                doAddFriend();
                break;
            case 11045:
                domenuFindTeam();
                break;
            case 110451:
                actInputTeam();
                break;
            case 110452:
                actRefreshFindTeam();
                break;
            case 11046:
                domenuFindTeamCenter();
                break;
            case 11047:
                domenuTeamLeft();
                break;
            case 110471:
                actCreateTeam();
                break;
            case 11048:
                actBoxSort();
                break;
            case 11049:
                actionCoinOut();
                break;
            case 11050:
                actionCoinIn();
                break;
            case 11051:
                actUseItem();
                break;
            case 11052:
            {
                Item item13 = (Item)p;
                Service.gI().useItemChangeMap(item13.indexUI, GameCanvas.menu.menuSelectedItem);
                break;
            }

            case 11053:
            {
                Item itemBag = (Item)p;
                doItemChangeMap(itemBag);
                break;
            }

            case 110531:
            {
                Item item12 = (Item)p;
                Service.gI().useItemChangeMap(item12.indexUI, GameCanvas.menu.menuSelectedItem + 3);
                break;
            }

            case 11054:
                doOpenUIZone();
                break;
            case 11055:
            {
                Item item11 = (Item)p;
                doActionBuy(item11);
                break;
            }

            case 110561:
                GameCanvas.endDlg();
                updateCommandForUI();
                break;
            case 110562:
            {
                Item item10 = (Item)p;
                doActionSaleOk(item10);
                break;
            }

            case 11057:
            {
                Npc npc3 = (Npc)p;
                Service.gI().getTask(npc3.template.npcTemplateId, GameCanvas.menu.menuSelectedItem, -1);
                break;
            }

            case 11058:
            {
                Item item9 = (Item)p;
                GameCanvas.endDlg();
                Service.gI().saleItem(item9.indexUI, int.Parse(GameCanvas.inputDlg.tfInput.getText()));
                break;
            }

            case 11059:
                actdoChooseSkill();
                break;
            case 11060:
                actdoMiniInfo();
                break;
            case 11061:
            {
                Item item8 = (Item)p;
                actSaleConfirm(item8);
                break;
            }

            case 11062:
                upPearl();
                break;
            case 11063:
                actConfirmUpgrade();
                break;
            case 11064:
                Service.gI().upPotential(indexTitle - 1, 1);
                setLCR();
                break;
            case 11065:
                doUpPotential();
                break;
            case 11066:
                doCloseAlert();
                isPaintMessage = false;
                isMessageMenu = false;
                ChatTextField.gI().center = null;
                break;
            case 11067:
                if (TileMap.zoneID != indexSelect)
                {
                    Service.gI().requestChangeZone(indexSelect, indexItemUse);
                }
                else
                {
                    InfoMe.addInfo(mResources.ZONE_HERE);
                }

                break;
            case 11070:
            {
                Party pt = (Party)p;
                actRefresh_TeamLeader(pt);
                break;
            }

            case 110701:
                Service.gI().outParty();
                break;
            case 110702:
                Service.gI().lockParty(true);
                break;
            case 110703:
                Service.gI().lockParty(false);
                break;
            case 11071:
                Service.gI().outParty();
                break;
            case 11072:
                actSaleItem();
                break;
            case 110721:
                Service.gI().saleItem(indexSelect, 1);
                break;
            case 110722:
                actionSale(Char.getMyChar().arrItemBag[indexSelect]);
                break;
            case 110723:
                Service.gI().saleItem(indexSelect, Char.getMyChar().arrItemBag[indexSelect].quantity);
                break;
            case 11073:
                actionSale(Char.getMyChar().arrItemBag[indexSelect]);
                break;
            case 11076:
                empty = (string)p;
                Service.gI().addParty(empty);
                break;
            case 11077:
                empty = (string)p;
                actConfirmRemoveFriend(empty);
                break;
            case 110771:
                empty = (string)p;
                GameCanvas.endDlg();
                Service.gI().removeFriend(empty);
                break;
            case 11078:
                actSetEnemiesCommand();
                break;
            case 11079:
                actSetFriendCommand();
                break;
            case 110791:
                empty = (string)p;
                Service.gI().addParty(empty);
                break;
            case 110792:
                empty = (string)p;
                actSetDeleteFriend(empty);
                break;
            case 1107921:
                empty = (string)p;
                GameCanvas.endDlg();
                Service.gI().removeFriend(empty);
                actRemoveWaitAcceptFriend(empty);
                break;
            case 1107931:
                empty = (string)p;
                Service.gI().addFriend(empty);
                break;
            case 1107932:
                empty = (string)p;
                actRemoveWaitAcceptFriend(empty);
                break;
            case 11080:
                empty = (string)p;
                actSetPartyCommand(empty);
                break;
            case 110801:
                Service.gI().moveMember(indexRow);
                break;
            case 110802:
                Service.gI().changeTeamLeader(indexRow);
                break;
            case 110803:
                empty = (string)p;
                Service.gI().addFriend(empty);
                break;
            case 110804:
                viewMemberInfo();
                break;
            case 1108041:
                empty = (string)p;
                Service.gI().viewInfo(empty);
                gI().resetButton();
                break;
            case 110805:
                viewMemberClanInfo();
                break;
            case 11081:
                actdoGan();
                break;
            case 110811:
            {
                SkillTemplate skillTemplate2 = Char.getMyChar().nClass.skillTemplates[indexSelect];
                doSetKeySkill(skillTemplate2);
                break;
            }

            case 110812:
            {
                SkillTemplate skillTemplate = Char.getMyChar().nClass.skillTemplates[indexSelect];
                doSetOnScreenSkill(skillTemplate);
                break;
            }

            case 11082:
                actTrangBiSelect();
                break;
            case 110821:
                Service.gI().itemBodyToBag(indexSelect + indextabTrangbi);
                break;
            case 11083:
                actView(5);
                break;
            case 11084:
                actdoUpPoint();
                break;
            case 110841:
                Service.gI().upPotential(indexTitle - 1, 1);
                setLCR();
                break;
            case 110842:
                doUpPotential();
                break;
            case 11085:
            {
                Item item7 = (Item)p;
                actBuyQuanNam(item7);
                break;
            }

            case 110851:
            {
                Item item6 = (Item)p;
                Service.gI().buyItem(item6.typeUI, item6.indexUI, 1);
                break;
            }

            case 110852:
            {
                Item item5 = (Item)p;
                actionBuy(item5);
                break;
            }

            case 110854:
                actTrangBiCenter();
                break;
            case 11086:
                actTrangBiRight();
                break;
            case 11087:
            {
                Item item4 = (Item)p;
                GameCanvas.endDlg();
                Service.gI().splitItem(item4);
                break;
            }

            case 11088:
                actView(26);
                break;
            case 11089:
                actView(27);
                break;
            case 11090:
                actView(28);
                break;
            case 11091:
                actView(29);
                break;
            case 11092:
            {
                Item itemBuy = (Item)p;
                actBuyLeft(itemBuy);
                break;
            }

            case 110921:
            {
                Item item3 = (Item)p;
                Service.gI().buyItem(item3.typeUI, item3.indexUI, 1);
                break;
            }

            case 110922:
            {
                Item item2 = (Item)p;
                actionBuy(item2);
                break;
            }

            case 110923:
                actView(16);
                break;
            case 110924:
                actView(17);
                break;
            case 110925:
                actView(18);
                break;
            case 110926:
                actView(19);
                break;
            case 11093:
                actView(2);
                break;
            case 11094:
                actView(6);
                break;
            case 11095:
                actView(7);
                break;
            case 11096:
                actView(8);
                break;
            case 11097:
                actView(9);
                break;
            case 11098:
                actUpgradeLeft();
                break;
            case 110981:
                upGrade();
                break;
            case 11099:
                isViewNext = false;
                updateItemInfo(3, itemUpGrade);
                break;
            case 110991:
                isViewNext = true;
                updateItemInfo(3, itemUpGrade);
                break;
            case 11100:
                actUpgradeBag();
                break;
            case 111001:
                actItemUpgradeMoveOut();
                break;
            case 11101:
                item = getItemFocus(10);
                updateItemInfo(3, item);
                break;
            case 11102:
                actUpgrade_HanhTrang();
                break;
            case 11103:
                actSplitItem();
                break;
            case 111031:
                if (isPaintTinhluyen)
                {
                    Service.gI().tinhluyen(itemSplit, arrItemSplit);
                }
                else if (isPaintDichChuyen)
                {
                    Service.gI().dichchuyen(itemSplit, arrItemSplit);
                }
                else if (isPaintLuyenNgoc)
                {
                    Service.gI().ngockham(1, null, itemSplit, arrItemSplit);
                }
                else if (isPaintGotNgoc)
                {
                    Service.gI().ngockham(2, null, itemSplit, null);
                }
                else if (isPaintThaoNgoc)
                {
                    Service.gI().ngockham(3, null, itemSplit, null);
                }

                break;
            case 11104:
                updateItemInfo(3, (Item)p);
                break;
            case 11105:
                split();
                break;
            case 11106:
                actSplitItemBag();
                break;
            case 11107:
                actUpPearlSelect();
                break;
            case 111071:
                actUpPearlMoveOut();
                break;
            case 11108:
                actView(3);
                break;
            case 11109:
                actUpPearlItems();
                break;
            case 11110:
                item = arrItemTradeOrder[indexSelect];
                updateItemInfo(30, item);
                break;
            case 900001:
                showStorageStackMenu(true);
                break;
            case 900003:
                showStorageStackMenu(false);
                break;
            case 900002:
                showStackSplit(true);
                break;
            case 111101:
                item = getItemFocus(4);
                Service.gI().itemBoxToBag(item.indexUI);
                break;
            case 11111:
                item = getItemFocus(4);
                updateItemInfo(4, item);
                break;
            case 11112:
                Service.gI().boxSort();
                break;
            case 11113:
                Service.gI().itemBagToBox(Char.getMyChar().arrItemBag[indexSelect].indexUI);
                break;
            case 1234567:
                Handle.addItemDell(Char.getMyChar().arrItemBag[indexSelect].template.id);
                break;
            case 1234568:
                Handle.removeItemDell(Handle.arrItemDell[indexSelect]);
                break;
            case 1234578:
                Handle.sortItemDel();
                break;
            case 11114:
                updateItemInfo(3, Char.getMyChar().arrItemBag[indexSelect]);
                break;
            case 11115:
                actBuyItemUILeft();
                break;
            case 11116:
                actBuyItemLeft2();
                break;
            case 11120:
            {
                object[] array2 = (object[])p;
                Skill skill2 = (Skill)array2[0];
                int num4 = int.Parse((string)array2[1]);
                onScreenSkill[num4] = skill2;
                saveOnScreenSkillToRMS();
                break;
            }

            case 11121:
            {
                object[] array = (object[])p;
                Skill skill = (Skill)array[0];
                int num3 = int.Parse((string)array[1]);
                keySkill[num3] = skill;
                saveKeySkillToRMS();
                break;
            }

            case 12003:
                doShowFriendUI();
                break;
            case 130011:
            {
                Npc npc2 = (Npc)p;
                Service.gI().getTask(npc2.template.npcTemplateId, 0, -1);
                npc2.chatPopup = null;
                resetButton();
                break;
            }

            case 130012:
            {
                Npc npc = (Npc)p;
                npc.chatPopup = null;
                resetButton();
                break;
            }

            case 13001:
                actView(32);
                break;
            case 13002:
                actStoreFashionBuy();
                break;
            case 130021:
                actBuy(32);
                break;
            case 130022:
                actBuys(32);
                break;
            case 14001:
                actdoKyNang();
                break;
            case 140011:
                Service.gI().upSkill(Char.getMyChar().nClass.skillTemplates[indexSelect].id, 1);
                setLCR();
                break;
            case 140012:
                doUpSkill();
                break;
            case 14002:
                doUnfocusChar();
                break;
            case 14003:
                doCharFocusList();
                break;
            case 14004:
                domenuClanNotice();
                break;
            case 140041:
                GameCanvas.inputDlg.tfInput.setMaxTextLenght(180);
                GameCanvas.inputDlg.show(mResources.INPUT_CLAN_TEXT, new Command(mResources.ACCEPT, GameCanvas.instance, 88832, null), TField.INPUT_TYPE_ANY);
                break;
            case 140042:
                Service.gI().clanUpLevel();
                break;
            case 140043:
                GameCanvas.inputDlg.show(mResources.INPUT_CLAN_MONEY, new Command(mResources.ACCEPT, GameCanvas.instance, 88834, null), TField.INPUT_TYPE_ANY);
                break;
            case 140044:
                Service.gI().unlockClanItem();
                break;
            case 14005:
                domenuClanLeader();
                break;
            case 14006:
                empty = (string)p;
                actSetClanCommand(empty);
                break;
            case 14007:
                domenuClanConfig();
                break;
            case 140071:
                indexRow = 0;
                indexSelect = 0;
                scrMain.clear();
                isViewClanMemOnline = !isViewClanMemOnline;
                sortClan();
                break;
            case 140072:
                indexRow = 0;
                indexSelect = 0;
                scrMain.clear();
                isSortClanByPointWeek = !isSortClanByPointWeek;
                sortClan();
                break;
            case 14008:
                GameCanvas.startYesNoDlg(mResources.MOVE_OUT_CLAN1, new Command(mResources.YES, 140081), new Command(mResources.NO, 1));
                break;
            case 140081:
                Service.gI().outClan();
                GameCanvas.endDlg();
                break;
            case 14009:
                domenuClanPermission();
                break;
            case 140091:
                Service.gI().changeClanType(((Member)vClan.elementAt(indexRow)).name, 3);
                break;
            case 140092:
                Service.gI().changeClanType(((Member)vClan.elementAt(indexRow)).name, 2);
                break;
            case 140093:
                GameCanvas.startYesNoDlg(mResources.CLEAR_CLAN_TYPE, new Command(mResources.YES, 1400931), new Command(mResources.NO, 1));
                break;
            case 140094:
                GameCanvas.startYesNoDlg(mResources.MOVE_OUT_CLAN, new Command(mResources.YES, 1400941), new Command(mResources.NO, 1));
                break;
            case 1400931:
                Service.gI().changeClanType(((Member)vClan.elementAt(indexRow)).name, 0);
                GameCanvas.endDlg();
                break;
            case 1400941:
                Service.gI().moveOutClan(((Member)vClan.elementAt(indexRow)).name);
                GameCanvas.endDlg();
                break;
            case 140095:
            {
                Member member2 = (Member)vClan.elementAt(indexRow);
                Service.gI().inviteClanDun(member2.name);
                break;
            }

            case 140096:
                myVector = new MyVector();
                myVector.addElement(new Command(mResources.INVITE_THIS_PERSON, 1400961));
                myVector.addElement(new Command(mResources.INVITE_ALL, 1400962));
                GameCanvas.menu.startAt(myVector, 0);
                break;
            case 1400961:
            {
                Member member = (Member)vClan.elementAt(indexRow);
                Service.gI().inviteClanBattlefield(member.name);
                break;
            }

            case 1400962:
                Service.gI().inviteClanBattlefieldAll();
                break;
            case 14010:
                GameCanvas.inputDlg.show(mResources.INPUT_CLAN_CONTRIBUTE, new Command(mResources.ACCEPT, GameCanvas.instance, 88833, null), TField.INPUT_TYPE_NUMERIC);
                break;
            case 140101:
                item = new Item();
                item.template = ItemTemplates.get(0);
                item.expires = -1L;
                updateItemInfo(39, item);
                break;
            case 14011:
                isMessageMenu = false;
                break;
            case 14012:
                actConvert_HanhTrang();
                break;
            case 14013:
                actConvertLeft();
                break;
            case 140131:
                GameCanvas.startYesNoDlg(mResources.CONFIRMCONVERT, new Command(mResources.YES, 140132), new Command(mResources.NO, 1));
                break;
            case 140132:
                Convert();
                break;
            case 14014:
                actConvertMoveOut();
                break;
            case 14015:
                actItemConvertMoveOut();
                break;
            case 140151:
                actItemConvertMoveOut2();
                break;
            case 14016:
                updateItemInfo(3, arrItemConvert[indexSelect]);
                break;
            case 140161:
                updateItemInfo(3, arrItemConvert[2]);
                break;
            case 14017:
                domenuEnemieLeft();
                break;
            case 14018:
                actView(34);
                break;
            case 14019:
                actClanStoreBuy();
                break;
            case 140191:
                actBuy(34);
                break;
            case 140192:
                actBuys(34);
                break;
            case 14020:
                if (isHaveVanBienLenh())
                {
                    empty = (string)p;
                    Service.gI().textBoxId(1, empty);
                }
                else
                {
                    GameCanvas.startOKDlg(mResources.ALERT_MOVETO);
                }

                break;
            case 14021:
                actSetDunListCommand();
                break;
            case 14022:
                actEliteShopBuy();
                break;
            case 140221:
                actBuy(35);
                break;
            case 140222:
                actBuys(35);
                break;
            case 14023:
                actView(35);
                break;
            case 14024:
                actSetRankedListCommand(1);
                break;
            case 14025:
                actSetRankedListCommand(0);
                break;
            case 1500:
                myVector = new MyVector();
                myVector.addElement(new Command(mResources.MOVEOUT, 15001));
                if (Char.getMyChar().xu >= 1000)
                {
                    myVector.addElement(new Command(mResources.SALE, 15002));
                }

                GameCanvas.menu.startAt(myVector, 1);
                break;
            case 15001:
                doStandToBag();
                break;
            case 15002:
                try
                {
                    int value = int.Parse(tfText.getText());
                    if (value <= 0)
                    {
                        GameCanvas.startOKDlg(mResources.INVALID_NUM);
                    }

                    GameCanvas.startYesNoDlg(mResources.replace(mResources.SALE_ASK, NinjaUtil.numberToString(value + string.Empty)), new Command(mResources.YES, 150021), new Command(mResources.NO, 1));
                    break;
                }
                catch (Exception)
                {
                    GameCanvas.startOKDlg(mResources.INVALID_NUM);
                    break;
                }

            case 150021:
            {
                GameCanvas.startWaitDlg();
                int value = 0;
                try
                {
                    value = int.Parse(tfText.getText());
                }
                catch (Exception)
                {
                }

                Service.gI().sendToSaleItem(itemSell, value);
                break;
            }

            case 1501:
                updateItemInfo(3, itemSell);
                break;
            case 1502:
                tfText.isFocus = true;
                break;
            case 1503:
                doBagToStand();
                break;
            case 1504:
                domenuItemStand();
                break;
            case 15041:
                domenuSortItemStand();
                break;
            case 150411:
                typeSortLevel = 0;
                typeSortName = 0;
                if (typeSortPrice == 0)
                {
                    typeSortPrice = 1;
                }
                else if (typeSortPrice == 1)
                {
                    typeSortPrice = 2;
                }
                else if (typeSortPrice == 2)
                {
                    typeSortPrice = 1;
                }

                sortItemStand();
                break;
            case 150412:
                typeSortLevel = 0;
                typeSortPrice = 0;
                if (typeSortName == 0)
                {
                    typeSortName = 1;
                }
                else if (typeSortName == 1)
                {
                    typeSortName = 2;
                }
                else if (typeSortName == 2)
                {
                    typeSortName = 1;
                }

                sortItemStand();
                break;
            case 150413:
                typeSortPrice = 0;
                typeSortName = 0;
                if (typeSortLevel == 0)
                {
                    typeSortLevel = 1;
                }
                else if (typeSortLevel == 1)
                {
                    typeSortLevel = 2;
                }
                else if (typeSortLevel == 2)
                {
                    typeSortLevel = 1;
                }

                sortItemStand();
                break;
            case 15042:
                GameCanvas.startYesNoDlg(mResources.replace(mResources.BUY_ASK, NinjaUtil.numberToString(arrItemStands[indexSelect].price + string.Empty)), new Command(mResources.YES, 150421), new Command(mResources.NO, 1));
                break;
            case 150421:
                GameCanvas.endDlg();
                Service.gI().buyItemAuction(arrItemStands[indexSelect].item.itemId);
                break;
            case 1505:
                updateItemInfo(3, arrItemStands[indexSelect].item);
                break;
            case 1506:
                if (arrItemSprin != null)
                {
                    yenTemp = 0;
                    isPaintItemInfo = false;
                    indexCard = -1;
                    arrItemSprin = null;
                    gI().left = new Command(mResources.SELECT, 1506);
                }
                else
                {
                    indexCard = indexSelect;
                    Service.gI().selectCard();
                    GameCanvas.startWaitDlgWithoutCancel();
                }

                break;
            case 1507:
                doviewCardsInfo();
                break;
            case 1508:
            {
                MyVector myVector2 = new MyVector();
                item = Char.clan.items[indexSelect];
                if (item.template.id == 281)
                {
                    myVector2.addElement(new Command(mResources.USE, 15081));
                }
                else
                {
                    myVector2.addElement(new Command(mResources.CLAN_BOX, 15082));
                }

                GameCanvas.menu.startAt(myVector2, 0);
                break;
            }

            case 15081:
                Service.gI().useClanItem();
                break;
            case 15082:
                GameCanvas.inputDlg.show(mResources.INPUT_MEM_NAME, new Command(mResources.OK, GameCanvas.instance, 88843, indexSelect), TField.INPUT_TYPE_ANY);
                break;
            case 1509:
                if (indexSelect >= 0 && Char.clan != null)
                {
                    updateItemInfo(39, Char.clan.items[indexSelect]);
                }
                else
                {
                    isPaintItemInfo = false;
                }

                break;
            case 1510:
                doAutoSelect();
                break;
            case 1511:
                empty = GameCanvas.inputDlg.tfInput.getText();
                GameCanvas.endDlg();
                try
                {
                    if (empty.Equals(string.Empty))
                    {
                        GameCanvas.startOKDlg(mResources.INVALID_NUM);
                        break;
                    }

                    int num2 = int.Parse(empty);
                    if (num2 < 10 || num2 > 90)
                    {
                        GameCanvas.startOKDlg(mResources.INVALID_NUM);
                    }
                    else
                    {
                        Char.aHpValue = num2;
                    }

                    break;
                }
                catch (Exception)
                {
                    GameCanvas.startOKDlg(mResources.INVALID_NUM);
                    break;
                }

            case 1512:
                empty = GameCanvas.inputDlg.tfInput.getText();
                GameCanvas.endDlg();
                try
                {
                    if (empty.Equals(string.Empty))
                    {
                        GameCanvas.startOKDlg(mResources.INVALID_NUM);
                        break;
                    }

                    int num = int.Parse(empty);
                    if (num < 10 || num > 90)
                    {
                        GameCanvas.startOKDlg(mResources.INVALID_NUM);
                    }
                    else
                    {
                        Char.aMpValue = num;
                    }

                    break;
                }
                catch (Exception)
                {
                    GameCanvas.startOKDlg(mResources.INVALID_NUM);
                    break;
                }

            case 151301:
                Service.gI().sendCatkeo(Char.getMyChar().mobFocus.getTemplate().mobTemplateId);
                break;
            case 15130:
                Char.aFoodValue = 1;
                break;
            case 15131:
                Char.aFoodValue = 10;
                break;
            case 15132:
                Char.aFoodValue = 20;
                break;
            case 15133:
                Char.aFoodValue = 30;
                break;
            case 15134:
                Char.aFoodValue = 40;
                break;
            case 15135:
                Char.aFoodValue = 50;
                break;
            case 15136:
                Char.aFoodValue = 60;
                break;
            case 15137:
                Char.aFoodValue = 70;
                break;
            case 1515:
                updateItemInfo(41, currentCharViewInfo.arrItemMounts[indexSelect]);
                break;
            case 1516:
                Service.gI().itemMonToBag(indexSelect);
                break;
            case 1600:
                doLuyenThach();
                break;
            case 1601:
                actLuyenThachSelect();
                break;
            case 1602:
                item = getItemFocus(43);
                updateItemInfo(3, item);
                break;
            case 1603:
                actgetLuyenThachItems();
                break;
            case 1604:
            {
                MyVector myVector2 = new MyVector();
                if (arrItemSplit[indexSelect] != null)
                {
                    myVector2.addElement(new Command(mResources.MOVEOUT, 1605));
                }

                myVector2.addElement(new Command(mResources.BEGIN, 11105));
                GameCanvas.menu.startAt(myVector2, 0);
                break;
            }

            case 1605:
                actTinhLuyenMoveOut();
                break;
            case 1606:
                actDichChuyenTrangBiLeft();
                break;
            case 999:
                doOpenUI(35);
                break;
            case 1700:
                luckyDraw_refresh();
                break;
            case 1701:
                luckyDraw_Tf();
                break;
            case 1702:
                doCloseLucky_Draw();
                break;
            case 222:
                actSplitItemBag_luyenngoc();
                break;
            case 333:
                doOpenUI(47);
                break;
            case 334:
                actUpgrade_HanhTrang();
                break;
            case 335:
                isViewNext = false;
                updateItemInfo(3, itemSplit);
                break;
            case 336:
                isViewNext = false;
                updateItemInfo(3, itemUpGrade);
                break;
            case 337:
                act_update_khamngoc();
                break;
            case 338:
                setCMD_act_KhamNgoc(0);
                break;
            case 344:
                setCMD_act_KhamNgoc(1);
                break;
            case 345:
                setCMD_act_KhamNgoc(2);
                break;
            case 339:
                actKhamNgocMoveOut(0);
                break;
            case 340:
                actKhamNgocMoveOut(1);
                break;
            case 343:
                actKhamNgocMoveOut(2);
                break;
            case 341:
                khamngoc();
                break;
            case 342:
                actConfirmKhamNgoc();
                break;
            case 400:
                act_update_giaodo();
                break;
            case 401:
                setCMD_act_GiaoDo();
                break;
            case 402:
                actGiaoDoMoveOut();
                break;
            case 403:
                sendGiaoDo();
                break;
            case 405:
                actSplitItemBag_gotngoc();
                break;
            case 508:
                Service.gI().doRemoveVithu(indexSelect);
                break;
            case 500:
                actBijuuSelect();
                break;
            case 501:
                Service.gI().itemMonToBag(indexSelect);
                break;
            case 502:
                doBiJuu();
                break;
            case 5021:
                doBiJuu_Point();
                break;
            case 5022:
                doBiJuu_Skill();
                break;
            case 503:
                updateItemInfo(51, currentCharViewInfo.arrItemViThu[indexSelect]);
                break;
            case 504:
                actBijuuMenuPoint();
                break;
            case 5041:
                Service.gI().upPotential(indexTitle - 1, 1);
                setLCR();
                break;
            case 5042:
                doBijuuUpPoint();
                break;
            case 5043:
                actBijuuUpPoint();
                break;
            case 505:
                actBijuuKyNang();
                break;
            case 5051:
                Service.gI().upSkill(Char.getMyChar().nClass.skillTemplates[indexSelect].id, 1);
                setLCR();
                break;
            case 5052:
                doBijuuUpSkill();
                break;
            case 5053:
                actBijSkillUpPoint();
                break;
        }
    }

    private void actDichChuyenTrangBiLeft()
    {
        try
        {
            if (Char.getMyChar().arrItemBag[indexSelect].template.id == 454)
            {
                if (arrItemSplit == null)
                {
                    arrItemSplit = new Item[24];
                }

                for (int i = 0; i < arrItemSplit.Length; i++)
                {
                    if (i == 20)
                    {
                        GameCanvas.startOKDlg(mResources.CRYSTAL_FULL);
                        break;
                    }

                    if (arrItemSplit[i] == null)
                    {
                        arrItemSplit[i] = Char.getMyChar().arrItemBag[indexSelect];
                        Char.getMyChar().arrItemBag[indexSelect] = null;
                        left = (center = null);
                        updateCommandForUI();
                        break;
                    }
                }
            }
            else if (Char.getMyChar().arrItemBag[indexSelect].upgrade > 11)
            {
                if (itemSplit == null)
                {
                    itemSplit = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                }
                else
                {
                    Item item = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    Char.getMyChar().arrItemBag[itemSplit.indexUI] = itemSplit;
                    itemSplit = item;
                }

                Service.gI().requestItemInfo(itemSplit.typeUI, itemSplit.indexUI);
            }
            else
            {
                GameCanvas.startOKDlg(mResources.CAN_NOT_DICHCHUYEN);
            }
        }
        catch (Exception)
        {
        }
    }

    private void actConvertLeft()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(cmdConvertMoveOut);
        for (int i = 0; i < arrItemConvert.Length; i++)
        {
            if (arrItemConvert[i] == null)
            {
                left = null;
                break;
            }

            if (i == arrItemConvert.Length - 1)
            {
                myVector.addElement(new Command(mResources.BEGIN, 140131));
            }
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actConvert_HanhTrang()
    {
        if (Char.getMyChar().arrItemBag[indexSelect].isTypeBody())
        {
            if (Char.getMyChar().arrItemBag[indexSelect].upgrade > 0)
            {
                convert(0);
            }
            else
            {
                convert(1);
            }
        }
        else if (Char.getMyChar().arrItemBag[indexSelect].template.id == 269 || Char.getMyChar().arrItemBag[indexSelect].template.id == 270 || Char.getMyChar().arrItemBag[indexSelect].template.id == 271)
        {
            convert(2);
        }
        else
        {
            GameCanvas.msgdlg.setInfo(mResources.ONLY_CONVERT, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void actSetClanCommand(string mName)
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CHAR_ORDER[6], 110805));
        myVector.addElement(new Command(mResources.PT, 110791, mName));
        myVector.addElement(new Command(mResources.MOVETO, 14020, mName));
        myVector.addElement(new Command(mResources.TEAMLEADER_SELECT[2], 110803, mName));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actLuyenThachSelect()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.MOVEOUT, 111071));
        for (int i = 0; i < arrItemUpPeal.Length; i++)
        {
            if (arrItemUpPeal[i] != null)
            {
                myVector.addElement(new Command(mResources.BEGIN, 1600));
                break;
            }
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actUpPearlSelect()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.MOVEOUT, 111071));
        for (int i = 0; i < arrItemUpPeal.Length; i++)
        {
            if (arrItemUpPeal[i] != null)
            {
                myVector.addElement(new Command(mResources.BEGIN, 11062));
                break;
            }
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actBuyLeft(Item itemBuy)
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110921, itemBuy));
        myVector.addElement(new Command(mResources.BUYS, 110922, itemBuy));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actTrangBiRight()
    {
        indexMenu = 0;
        isPaintInfoMe = false;
        left = menu;
        right = cmdFocus;
        center = null;
        resetButton();
        doMenuInforMe();
    }

    private void actBuyQuanNam(Item item)
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110851));
        myVector.addElement(new Command(mResources.BUYS, 110852));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actdoUpPoint()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.UPPOINT, 110841));
        myVector.addElement(new Command(mResources.UPPOINTS, 110842));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actTrangBiSelect()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.MOVETOBAG, 110821));
        if (!GameCanvas.isTouch)
        {
            if (indextabTrangbi == 0)
            {
                myVector.addElement(new Command(mResources.tbi2, 2003));
            }
            else
            {
                myVector.addElement(new Command(mResources.tbi1, 2003));
            }
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actdoGan()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.ASSIGN_KEY[0], 110811));
        myVector.addElement(new Command(mResources.ASSIGN_KEY[1], 110812));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actSetPartyCommand(string partyName)
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.TEAMLEADER_SELECT[0], 110801));
        myVector.addElement(new Command(mResources.TEAMLEADER_SELECT[1], 110802));
        myVector.addElement(new Command(mResources.TEAMLEADER_SELECT[2], 110803, partyName));
        myVector.addElement(new Command(mResources.CHAR_ORDER[6], 110804));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actdoKyNang()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.UPPOINT, 140011));
        myVector.addElement(new Command(mResources.UPPOINTS, 140012));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actSetDeleteFriend(string friendName)
    {
        GameCanvas.startYesNoDlg(mResources.CONFIRM_REMOVE_FRIEND, new Command(mResources.YES, 1107921, friendName), new Command(mResources.NO, 1));
    }

    private void actSetFriendCommand()
    {
        Friend friend = (Friend)vFriend.elementAt(indexRow);
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CHAR_ORDER[6], 1108041, friend.friendName));
        if (friend.type == 4)
        {
            myVector.addElement(new Command(mResources.ACCEPT, 1107931, friend.friendName));
            myVector.addElement(new Command(mResources.CANCEL, 1107932, friend.friendName));
        }
        else
        {
            myVector.addElement(new Command(mResources.PT, 110791, friend.friendName));
            myVector.addElement(new Command(mResources.MOVETO, 14020, friend.friendName));
            myVector.addElement(new Command(mResources.DELETE, 110792, friend.friendName));
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actSetRankedListCommand(sbyte index)
    {
        if (indexRow < 0 || indexRow >= vList.size())
        {
            return;
        }

        Ranked ranked = null;
        try
        {
            ranked = (Ranked)vList.elementAt(indexRow);
            string empty = string.Empty;
            empty = ((ranked == null) ? "raned=null" : ranked.name);
            Service.gI().requestRanked(index, empty);
            resetButton();
        }
        catch (Exception)
        {
        }
    }

    private void actSetDunListCommand()
    {
        if (indexRow >= 0 && indexRow < vList.size())
        {
            int id = ((DunItem)vList.elementAt(indexRow)).id;
            Service.gI().requestMatchInfo(id);
        }
    }

    private void actSetEnemiesCommand()
    {
        Friend friend = (Friend)vEnemies.elementAt(indexRow);
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.PT, 11076, friend.friendName));
        myVector.addElement(new Command(mResources.MOVETO, 14020, friend.friendName));
        myVector.addElement(new Command(mResources.DELETE, 11077, friend.friendName));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actRefresh_TeamLeader(Party pt)
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.OUT_TEAM, 110701));
        if (!pt.isLock)
        {
            myVector.addElement(new Command(mResources.LOCK_TEAM, 110702));
        }
        else
        {
            myVector.addElement(new Command(mResources.UNLOCK_TEAM, 110703));
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actSaleConfirm(Item item)
    {
        GameCanvas.endDlg();
        Service.gI().saleItem(item.indexUI, 1);
    }

    private void actdoMiniInfo()
    {
        indexMenu = 0;
        isPaintInfoMe = false;
        resetButton();
        if (currentCharViewInfo.charID == Char.getMyChar().charID)
        {
            doMenuInforMe();
        }
    }

    private void actCreateTeam()
    {
        Service.gI().createParty();
    }

    private void actRefreshFindTeam()
    {
        Service.gI().openFindParty();
    }

    private void actInputTeam()
    {
        if (indexRow >= 0 && indexRow < vPtMap.size())
        {
            Party party = (Party)vPtMap.elementAt(indexRow);
            if (party != null)
            {
                Service.gI().pleaseInputParty(party.name);
            }
        }
    }

    private void actCoinIn()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (num <= 0)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().xu == 0 || num > Char.getMyChar().xu)
        {
            GameCanvas.startOKDlg(mResources.NOT_ENOUGH_COIN_IN);
            return;
        }

        Service.gI().boxCoinIn(num);
        GameCanvas.inputDlg.hide();
    }

    private void actionCoinTradeAccept()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (num <= 0)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().xu == 0 || num > Char.getMyChar().xu)
        {
            GameCanvas.startOKDlg(mResources.NOT_ENOUGH_COIN_IN1);
            return;
        }

        coinTrade += num;
        Char.getMyChar().xu -= num;
        GameCanvas.inputDlg.hide();
    }

    private void actCoinOut()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (num <= 0)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().xuInBox == 0 || num > Char.getMyChar().xuInBox)
        {
            GameCanvas.startOKDlg(mResources.NOT_ENOUGH_COIN_OUT);
            return;
        }

        Service.gI().boxCoinOut(num);
        GameCanvas.inputDlg.hide();
    }

    private void actOrder()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.ACTIONS[6], 1100067));
        myVector.addElement(new Command(mResources.ACTIONS[3], 1100062));
        myVector.addElement(new Command(mResources.ACTIONS[1], 1100063));
        myVector.addElement(new Command(mResources.ACTIONS[2], 1100064));
        myVector.addElement(new Command(mResources.ACTIONS[0], 1100065));
        myVector.addElement(new Command(mResources.ACTIONS[7], 1100068));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actDead()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.DIES[1], 110381));
        myVector.addElement(new Command(mResources.DIES[2], 110382));
        myVector.addElement(new Command(mResources.DIES[3], 110383));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actTradeAccept()
    {
        Service.gI().tradeAccept();
        typeTrade = 2;
        if (gI().typeTrade >= 2 && gI().typeTradeOrder >= 2)
        {
            InfoDlg.showWait();
        }
    }

    private void actTradeLock()
    {
        Service.gI().tradeItemLock(coinTrade, arrItemTradeMe);
        typeTrade = 1;
        if (gI().typeTrade == 1 && gI().typeTradeOrder == 1)
        {
            gI().timeTrade = (int)(mSystem.getCurrentTimeMillis() / 1000 + 5);
        }

        left = cmdTradeAccept;
    }

    private void actTradeSelectInList()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(cmdTradeMoveOut);
        if (typeTrade == 0)
        {
            myVector.addElement(cmdTradeLock);
        }
        else if (typeTrade == 1 && typeTradeOrder >= 1 && timeTrade - mSystem.getCurrentTimeMillis() / 1000 <= 0)
        {
            myVector.addElement(cmdTradeAccept);
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actNonNamBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110161));
        myVector.addElement(new Command(mResources.BUYS, 110162));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actNonNuBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110141));
        myVector.addElement(new Command(mResources.BUYS, 110142));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actAoNamBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110121));
        myVector.addElement(new Command(mResources.BUYS, 110122));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actAoNuBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110101));
        myVector.addElement(new Command(mResources.BUYS, 110102));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actGangTayNamBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110081));
        myVector.addElement(new Command(mResources.BUYS, 110082));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actGangTayNuBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110051));
        myVector.addElement(new Command(mResources.BUYS, 110052));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actBuy(sbyte itembuy)
    {
        Item item = getItemFocus(itembuy);
        Service.gI().buyItem(item.typeUI, item.indexUI, 1);
    }

    private void actBuys(sbyte itembuys)
    {
        Item item = getItemFocus(itembuys);
        actionBuy(item);
    }

    private void actView(sbyte itemview)
    {
        Item item = getItemFocus(itemview);
        if (item != null)
        {
            updateItemInfo(item.typeUI, item);
        }
        else
        {
            Out.println("nullll");
        }
    }

    private void actAddPotential()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.startOKDlg(mResources.NOT_INPUT_POINT);
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (num <= 0)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().pPoint == 0 || num > Char.getMyChar().pPoint)
        {
            GameCanvas.startOKDlg(mResources.NOT_ENOUGH_POINT);
            return;
        }

        Service.gI().upPotential(indexTitle - 1, num);
        GameCanvas.inputDlg.hide();
    }

    private void actAddFriendAccept()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.startOKDlg(mResources.NOT_INPUT_NAME);
            return;
        }

        Service.gI().addFriend(text);
        GameCanvas.inputDlg.hide();
    }
}
