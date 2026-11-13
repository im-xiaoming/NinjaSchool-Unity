using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    protected void doMenusynthesis()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.SYNTHESIS[0], 110002));
        myVector.addElement(new Command(mResources.SYNTHESIS[1], 1100032));
        myVector.addElement(new Command(mResources.SYNTHESIS[2], 1100033));
        myVector.addElement(new Command(mResources.Lucky_draw, 1100034));
        myVector.addElement(new Command(mResources.CONFIG, SettingsMenu.gI(), 1006, null));
        GameCanvas.menu.startAt(myVector, 3);
    }

    public void createMenu(string[] menu, Npc npc)
    {
        MyVector myVector = new MyVector();
        for (int i = 0; i < menu.Length; i++)
        {
            myVector.addElement(new Command(menu[i], 11057, npc));
        }

        GameCanvas.menu.startAt(myVector, 2);
    }

    public void doMiniMenuInforMe()
    {
        isPaintInfoMe = true;
        if (!Main.isPC)
        {
            setPopupSize(175, 200 - ((indexMenu != 4) ? indexSize : 0));
        }
        else
        {
            setPopupSize(175, 200);
        }

        setLCR();
        if (indexMenu == 3 && currentCharViewInfo.charID == Char.getMyChar().charID)
        {
            Service.gI().viewInfo(currentCharViewInfo.cName);
        }

        if (indexMenu == 5)
        {
            xMounts = new int[5];
            yMounts = new int[5];
            xstart = popupX + 5;
            ystart = popupY + 35;
            xMounts[0] = xstart + 5;
            yMounts[0] = ystart + 35;
            xMounts[1] = xstart + 5;
            yMounts[1] = ystart + 70;
            xMounts[2] = xstart + 131;
            yMounts[2] = ystart + 35;
            xMounts[3] = xstart + 131;
            yMounts[3] = ystart + 70;
            xMounts[4] = xMounts[0] + indexSize + 7;
            yMounts[4] = yMounts[0] - 5;
        }

        if (indexMenu == 6)
        {
            xMounts = new int[5];
            yMounts = new int[5];
            xstart = popupX + 5;
            ystart = popupY + 35;
            xMounts[0] = xstart + 5;
            yMounts[0] = ystart + 35;
            xMounts[1] = xstart + 5;
            yMounts[1] = ystart + 70;
            xMounts[2] = xstart + 131;
            yMounts[2] = ystart + 35;
            xMounts[3] = xstart + 131;
            yMounts[3] = ystart + 70;
            xMounts[4] = xMounts[0] + indexSize + 7;
            yMounts[4] = yMounts[0] - 5;
        }

        right = new Command(mResources.BACK, 11060);
    }

    protected void doMenuInforMe()
    {
        scrMain.clear();
        scrInfo.clear();
        isViewNext = false;
        cmdBag = new Command(mResources.MENUME[0], 1100011);
        cmdSkill = new Command(mResources.MENUME[1], 1100012);
        cmdTiemnang = new Command(mResources.MENUME[2], 1100013);
        cmdInfo = new Command(mResources.MENUME[3], 1100014);
        cmdtrangbi = new Command(mResources.MENUME[4], 1100015);
        cmdBijuu = new Command(mResources.MENUME[6], 1100017);
        MyVector myVector = new MyVector();
        myVector.addElement(cmdBag);
        myVector.addElement(cmdSkill);
        myVector.addElement(cmdTiemnang);
        myVector.addElement(cmdInfo);
        myVector.addElement(cmdtrangbi);
        myVector.addElement(new Command(mResources.MENUME[5], 1100016));
        myVector.addElement(cmdBijuu);
        GameCanvas.menu.startAt(myVector, 3);
    }

    protected void actMenu()
    {
        MyVector myVector = new MyVector();
        Command o = new Command(mResources.MENUGAMESCR[1], 110001);
        new Command(mResources.MENUGAMESCR[2], 110002);
        Command o3 = new Command(mResources.MENUGAMESCR[3], 110003);
        Command o4 = new Command(mResources.MENUGAMESCR[4], 110004);
        new Command(mResources.MENUGAMESCR[0], 110005);
        Command o5 = new Command(mResources.MENUGAMESCR[6], 110006);
        new Command(mResources.MENUGAMESCR[7], 110007);
        new Command(mResources.MENUGAMESCR[8], 110008);
        new Command(mResources.MENUGAMESCR[9], 110009);
        new Command(mResources.MENUGAMESCR[10], 110010);
        new Command(mResources.MENUGAMESCR[11], 110011);
        new Command(mResources.MENUGAMESCR[12], 110012);
        new Command(mResources.MENUGAMESCR[13], 110013);
        new Command(mResources.MENUGAMESCR[14], 110014);
        // Offline change (user request, permanent): "Thủ khố" (110022), "Tự sát" (110023) and
        // "Khu vực" (110024) were removed from the game menu. Their perform() handlers are kept.
        new Command(mResources.MENUGAMESCR[15], 110015);
        new Command(mResources.MENUGAMESCR[16], 110016);
        new Command(mResources.MENUGAMESCR[17], 110017);
        myVector.addElement(o);
        myVector.addElement(o3);
        myVector.addElement(o5);
        myVector.addElement(o4);
        GameCanvas.menu.startAt(myVector, 20);
    }

    public void doShowTaskUI()
    {
        resetButton();
        isPaintTask = true;
        isLockKey = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
        left = new Command(mResources.MENUGAMESCR[2], 110002);
        center = new Command(mResources.CHANGE, 110019);
    }

    private int getMaxIndexForCurrentStore()
    {
        int num = 0;
        try
        {
            if (isPaintEliteShop)
            {
                num = ((arrItemElites.Length % columns != 0) ? ((arrItemElites.Length / columns + 1) * columns) : arrItemElites.Length);
            }
            else if (isPaintStore)
            {
                if (indexMenu == 0)
                {
                    num = ((arrItemStore.Length % columns != 0) ? ((arrItemStore.Length / columns + 1) * columns) : arrItemStore.Length);
                }
                else if (indexMenu == 1)
                {
                    num = ((arrItemBook.Length % columns != 0) ? ((arrItemBook.Length / columns + 1) * columns) : arrItemBook.Length);
                }
                else if (indexMenu == 2)
                {
                    num = ((arrItemFashion.Length % columns != 0) ? ((arrItemFashion.Length / columns + 1) * columns) : arrItemFashion.Length);
                }
                else if (indexMenu == 52)
                {
                    num = ((arrItemFashion.Length % columns != 0) ? ((arrItemFashion.Length / columns + 1) * columns) : arrItemFashion.Length);
                }
            }
            else if (isPaintNonNam)
            {
                num = ((arrItemNonNam.Length % columns != 0) ? ((arrItemNonNam.Length / columns + 1) * columns) : arrItemNonNam.Length);
            }
            else if (isPaintNonNu)
            {
                num = ((arrItemNonNu.Length % columns != 0) ? ((arrItemNonNu.Length / columns + 1) * columns) : arrItemNonNu.Length);
            }
            else if (isPaintAoNam)
            {
                num = ((arrItemAoNam.Length % columns != 0) ? ((arrItemAoNam.Length / columns + 1) * columns) : arrItemAoNam.Length);
            }
            else if (isPaintAoNu)
            {
                num = ((arrItemAoNu.Length % columns != 0) ? ((arrItemAoNu.Length / columns + 1) * columns) : arrItemAoNu.Length);
            }
            else if (isPaintGangTayNam)
            {
                num = ((arrItemGangTayNam.Length % columns != 0) ? ((arrItemGangTayNam.Length / columns + 1) * columns) : arrItemGangTayNam.Length);
            }
            else if (isPaintGangTayNu)
            {
                num = ((arrItemGangTayNu.Length % columns != 0) ? ((arrItemGangTayNu.Length / columns + 1) * columns) : arrItemGangTayNu.Length);
            }
            else if (isPaintQuanNam)
            {
                num = ((arrItemQuanNam.Length % columns != 0) ? ((arrItemQuanNam.Length / columns + 1) * columns) : arrItemQuanNam.Length);
            }
            else if (isPaintQuanNu)
            {
                num = ((arrItemQuanNu.Length % columns != 0) ? ((arrItemQuanNu.Length / columns + 1) * columns) : arrItemQuanNu.Length);
            }
            else if (isPaintGiayNam)
            {
                num = ((arrItemGiayNam.Length % columns != 0) ? ((arrItemGiayNam.Length / columns + 1) * columns) : arrItemGiayNam.Length);
            }
            else if (isPaintGiayNu)
            {
                num = ((arrItemGiayNu.Length % columns != 0) ? ((arrItemGiayNu.Length / columns + 1) * columns) : arrItemGiayNu.Length);
            }
            else if (isPaintLien)
            {
                num = ((arrItemLien.Length % columns != 0) ? ((arrItemLien.Length / columns + 1) * columns) : arrItemLien.Length);
            }
            else if (isPaintNhan)
            {
                num = ((arrItemNhan.Length % columns != 0) ? ((arrItemNhan.Length / columns + 1) * columns) : arrItemNhan.Length);
            }
            else if (isPaintNgocBoi)
            {
                num = ((arrItemNgocBoi.Length % columns != 0) ? ((arrItemNgocBoi.Length / columns + 1) * columns) : arrItemNgocBoi.Length);
            }
            else if (isPaintPhu)
            {
                num = ((arrItemPhu.Length % columns != 0) ? ((arrItemPhu.Length / columns + 1) * columns) : arrItemPhu.Length);
            }
            else if (isPaintWeapon)
            {
                num = ((arrItemWeapon.Length % columns != 0) ? ((arrItemWeapon.Length / columns + 1) * columns) : arrItemWeapon.Length);
            }
            else if (isPaintStack)
            {
                num = ((arrItemStack.Length % columns != 0) ? ((arrItemStack.Length / columns + 1) * columns) : arrItemStack.Length);
            }
            else if (isPaintStackLock)
            {
                num = ((arrItemStackLock.Length % columns != 0) ? ((arrItemStackLock.Length / columns + 1) * columns) : arrItemStackLock.Length);
            }
            else if (isPaintGrocery)
            {
                num = ((arrItemGrocery.Length % columns != 0) ? ((arrItemGrocery.Length / columns + 1) * columns) : arrItemGrocery.Length);
            }
            else if (isPaintGroceryLock)
            {
                num = ((arrItemGroceryLock.Length % columns != 0) ? ((arrItemGroceryLock.Length / columns + 1) * columns) : arrItemGroceryLock.Length);
            }

            if (isPaintBox)
            {
                num = Char.getMyChar().arrItemBox.Length;
            }

            if (isPaintItemDell)
            {
                num = Handle.arrItemDell.Length;
            }

            if (indexMenu == 1 && !isPaintStore)
            {
                num = Char.getMyChar().arrItemBag.Length;
            }
        }
        catch (Exception ex)
        {
            Out.println(" loi tai getMaxIndex  " + ex.ToString());
        }

        if ((isPaintUpPearl || isPaintLuyenThach || isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintLuyenNgoc) && indexMenu == 0)
        {
            num = 24;
        }
        else if ((isPaintKhamNgoc || isPaintUpGrade || isPaintConvert) && indexMenu == 0)
        {
            num = 18;
        }
        else if (num < 30)
        {
            num = 30;
        }

        return num;
    }

    private void getCurrentStoreData()
    {
        if (indexMenu == 0)
        {
            Service.gI().requestItem(14);
        }
        else if (indexMenu == 1)
        {
            Service.gI().requestItem(15);
        }
        else if (indexMenu == 2)
        {
            Service.gI().requestItem(32);
        }
        else if (indexMenu == 3)
        {
            Service.gI().requestItem(34);
        }
        else if (indexMenu == 52)
        {
            Service.gI().requestItem(32);
        }
    }

    public sbyte getTaskMapId()
    {
        if (Char.getMyChar().ctaskId >= tasks.Length)
        {
            return -3;
        }

        sbyte b = 0;
        b = ((Char.getMyChar().taskMaint != null) ? mapTasks[Char.getMyChar().ctaskId][Char.getMyChar().taskMaint.index + 1] : mapTasks[Char.getMyChar().ctaskId][0]);
        if (b == -1)
        {
            if (Char.getMyChar().nClass.classId == 0 && Char.getMyChar().ctaskId == 9)
            {
                b = -2;
            }
            else if (Char.getMyChar().nClass.classId == 0 || Char.getMyChar().nClass.classId == 1 || Char.getMyChar().nClass.classId == 2)
            {
                b = 1;
            }
            else if (Char.getMyChar().nClass.classId == 3 || Char.getMyChar().nClass.classId == 4)
            {
                b = 72;
            }
            else if (Char.getMyChar().nClass.classId == 5 || Char.getMyChar().nClass.classId == 6)
            {
                b = 27;
            }
        }

        return b;
    }

    public sbyte getTaskNpcId()
    {
        try
        {
            if (Char.getMyChar().ctaskId >= tasks.Length)
            {
                return -3;
            }

            sbyte b = 0;
            b = ((Char.getMyChar().taskMaint != null) ? tasks[Char.getMyChar().ctaskId][Char.getMyChar().taskMaint.index + 1] : tasks[Char.getMyChar().ctaskId][0]);
            if (b == -1)
            {
                if (Char.getMyChar().nClass.classId == 0 && Char.getMyChar().ctaskId == 9)
                {
                    b = -2;
                }
                else if (Char.getMyChar().nClass.classId == 0 || Char.getMyChar().nClass.classId == 1 || Char.getMyChar().nClass.classId == 2)
                {
                    b = 9;
                }
                else if (Char.getMyChar().nClass.classId == 3 || Char.getMyChar().nClass.classId == 4)
                {
                    b = 10;
                }
                else if (Char.getMyChar().nClass.classId == 5 || Char.getMyChar().nClass.classId == 6)
                {
                    b = 11;
                }
            }

            return b;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    private void domenuClan()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CLAN[0], 11000661));
        myVector.addElement(new Command(mResources.CLAN[1], 11000662));
        myVector.addElement(new Command(mResources.CLAN[2], 11000663));
        myVector.addElement(new Command(mResources.CLAN[3], 11000664));
        myVector.addElement(new Command(mResources.CLAN[4], 11000666));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void changeTaskInfo()
    {
        if (indexMenu == 0)
        {
            indexMenu = 1;
        }
        else
        {
            indexMenu = 0;
        }

        indexRow = 0;
        idTypeTask = indexMenu;
    }

    private void actClanStoreBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 140191));
        myVector.addElement(new Command(mResources.BUYS, 140192));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void domenuClanConfig()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CLAN_MENU[0] + ": " + ((!isViewClanMemOnline) ? mResources.ON : mResources.OFF), 140071));
        myVector.addElement(new Command(mResources.CLAN_SORTTYPE, 140072));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void domenuEnemieLeft()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CLAN_MENU[0] + ": " + ((!isViewClanMemOnline) ? mResources.ON : mResources.OFF), 140071));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actStoreFashionBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 130021));
        myVector.addElement(new Command(mResources.BUYS, 130022));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void domenuClanNotice()
    {
        MyVector myVector = new MyVector();
        if (Char.getMyChar().ctypeClan == 3 || Char.getMyChar().ctypeClan == 4)
        {
            if (indexTitle == 1)
            {
                myVector.addElement(new Command(mResources.CLAN_MENU[8], 140044));
            }

            myVector.addElement(new Command(mResources.CLAN_MENU[2], 140041));
            myVector.addElement(new Command(mResources.CLAN_MENU[5], 140042));
            if (Char.getMyChar().ctypeClan == 4)
            {
                myVector.addElement(new Command(mResources.CLAN_MENU[4], 140043));
            }

            myVector.addElement(new Command(mResources.CHAR_ORDER[10] + ": " + ((!isViewClanInvite) ? mResources.ON : mResources.OFF), 11000665));
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void domenuFindTeam()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.REFRESH, 110452));
        if (vPtMap.size() > 0)
        {
            myVector.addElement(new Command(mResources.INPUT_TEAM, 110451));
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void domenuClanPermission()
    {
        Member member = (Member)vClan.elementAt(indexRow);
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CLANS[1], 140091, member.name));
        myVector.addElement(new Command(mResources.CLANS[2], 140092, member.name));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void domenuFindTeamCenter()
    {
        Party party = (Party)vPtMap.elementAt(indexRow);
        if (party != null && !Char.getMyChar().cName.Equals(party.name))
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command(mResources.CHAR_ORDER[6], 1108041, party.name));
            myVector.addElement(new Command(mResources.TEAMLEADER_SELECT[2], 110803, party.name));
            GameCanvas.menu.startAt(myVector, 3);
        }
    }

    private void domenuClanLeader()
    {
        Member member = curMemberSelected();
        MyVector myVector = new MyVector();
        if (Char.getMyChar().ctypeClan == 4)
        {
            if (member.type != 4)
            {
                if (member.type != 3 && member.type != 2)
                {
                    myVector.addElement(new Command(mResources.SET_TYPE_CLAN, 14009, member.name));
                }
                else
                {
                    myVector.addElement(new Command(mResources.CLEAR_TYPE_CLAN, 140093, member.name));
                }

                if (TileMap.mapID == 98 || TileMap.mapID == 104)
                {
                    myVector.addElement(new Command(mResources.CLAN_MENU[7], 140096, member.name));
                }
                else
                {
                    myVector.addElement(new Command(mResources.CLAN_MENU[6], 140095, member.name));
                }

                myVector.addElement(new Command(mResources.FIRED_CLAN, 140094, member.name));
            }

            myVector.addElement(new Command(mResources.CONFIG, 14007));
        }
        else if (Char.getMyChar().ctypeClan == 3)
        {
            if (member.type != 4 && !member.name.Equals(Char.getMyChar().cName))
            {
                myVector.addElement(new Command(mResources.FIRED_CLAN, 140094, member.name));
                if (TileMap.mapID == 98 || TileMap.mapID == 104)
                {
                    myVector.addElement(new Command(mResources.CLAN_MENU[7], 140096, member.name));
                }
                else
                {
                    myVector.addElement(new Command(mResources.CLAN_MENU[6], 140095, member.name));
                }
            }

            myVector.addElement(new Command(mResources.CONFIG, 14007));
            myVector.addElement(new Command(mResources.CLAN_MENU[1], 14008, member.name));
        }
        else
        {
            myVector.addElement(new Command(mResources.CONFIG, 14007));
            myVector.addElement(new Command(mResources.CLAN_MENU[1], 14008, member.name));
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actsubMenuOrder_PK(int type)
    {
        switch (type)
        {
            case 1:
                Service.gI().changePk(0);
                break;
            case 2:
                Service.gI().changePk(Char.PK_NHOM);
                break;
            case 3:
                Service.gI().changePk(3);
                break;
        }
    }

    private void actsubMenuOrder()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.PKS[0], 11000651));
        myVector.addElement(new Command(mResources.PKS[1], 11000652));
        myVector.addElement(new Command(mResources.PKS[3], 11000653));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actMenu41()
    {
        GameCanvas.startWaitDlg();
        Session_ME.gI().close();
        Timer.setTimer(null, 9999, 1000L);
    }

    private void actMenu4()
    {
        GameCanvas.startYesNoDlg(mResources.DOYOUWANTEXIT2, new Command(mResources.YES, 1100041), new Command(mResources.NO, 1));
    }

    private void actEliteShopBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 140221));
        myVector.addElement(new Command(mResources.BUYS, 140222));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actStoreBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110201));
        myVector.addElement(new Command(mResources.BUYS, 110202));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actStoreLockBuy()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.BUY, 110181));
        myVector.addElement(new Command(mResources.BUYS, 110182));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void domenuPrivateLock()
    {
        MyVector myVector = new MyVector();
        if (typeActive == 0)
        {
            myVector.addElement(new Command(mResources.ENABLE, 11000671));
        }
        else if (typeActive == 1)
        {
            myVector.addElement(new Command(mResources.UNLOCK, 11000672));
        }

        if (typeActive == 1 || typeActive == 2)
        {
            myVector.addElement(new Command(mResources.CANCEL_PRIVATE_PROTECT, 11000674));
            myVector.addElement(new Command(mResources.CHANGE_PRIVATE_PASS, 11000673));
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void showStorageStackMenu(bool box)
    {
        MyVector actions = new MyVector();
        actions.addElement(new Command(box ? mResources.GETOUT : mResources.GETIN, box ? 111101 : 11113));
        Item[] slots = box ? Char.getMyChar().arrItemBox : Char.getMyChar().arrItemBag;
        Item item = slots[indexSelect];
        if (item != null && item.quantity > 1 && NsoServer.Store.InventoryGrouping.CanStack(item.template.type, item.template.isUpToUp))
            actions.addElement(new Command("Tách số lượng", box ? 900002 : 110244));
        actions.addElement(new Command(mResources.SORT, box ? 11112 : 110221));
        GameCanvas.menu.startAt(actions, 0);
    }

    private void domenuTeamLeft()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.CREATE_TEAM, 110471));
        myVector.addElement(new Command(mResources.ACTIONS[4], 1100061));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void domenuFriendLeft()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.ADDNEW, 110441));
        myVector.addElement(new Command(mResources.CLAN_MENU[0] + ": " + ((!isViewClanMemOnline) ? mResources.ON : mResources.OFF), 140071));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actBijuuMenuPoint()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.UPPOINT, 5041));
        myVector.addElement(new Command(mResources.UPPOINTS, 5042));
        GameCanvas.menu.startAt(myVector, 0);
    }
}
