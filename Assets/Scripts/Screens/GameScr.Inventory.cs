using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    public void doBag()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 0;
        doMiniMenuInforMe();
    }

    protected void doCloseItemInfo()
    {
        if (!GameCanvas.isTouch || (GameCanvas.isTouch && GameCanvas.w < 320) || (isPaintInfoMe && indexMenu > 0) || (isPaintClan && indexMenu == 0))
        {
            isPaintItemInfo = false;
        }

        isViewNext = false;
        if (isPaintUI())
        {
            updateCommandForUI();
            right = cmdCloseAll;
        }
        else
        {
            setLCR();
        }
    }

    public bool isBagFull()
    {
        for (int num = Char.getMyChar().arrItemBag.Length - 1; num >= 0; num--)
        {
            if (Char.getMyChar().arrItemBag[num] == null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Offline addition (user request): in Hành trang, an info panel that is already open follows
    /// the selection when moving left/right (the bag does not open it automatically).</summary>
    private void BagInfoFollow()
    {
        if (!isPaintItemInfo || indexTitle < 1)
            return;
        Item item = getItemFocus(3);
        if (item != null)
            updateItemInfo(3, item);
        else
            isPaintItemInfo = false;
    }

    private void doTouchUpgrade()
    {
        int x = popupX + 45;
        int y = popupY + 32;
        int x2 = popupX + 100;
        if (!GameCanvas.isPointerJustRelease)
        {
            return;
        }

        if (GameCanvas.isPointerHoldIn(x, y, 29, 29))
        {
            indexTitle = 1;
            indexSelect = 0;
            Sound.play(Sound.MBClick, 0.5f);
            updateCommandForUI();
        }

        if (GameCanvas.isPointerHoldIn(x2, y, 29, 29))
        {
            indexTitle = 1;
            indexSelect = 1;
            Sound.play(Sound.MBClick, 0.5f);
            updateCommandForUI();
            if (isPaintUpPearl && indexMenu == 0)
            {
                for (int i = 0; i < arrItemUpPeal.Length; i++)
                {
                    if (arrItemUpPeal[i] != null)
                    {
                        center = new Command(mResources.ACCEPT, 11062);
                        break;
                    }
                }
            }
        }

        if (GameCanvas.isPointerHoldIn(popupX, popupY + 2 * Hitem + 5, popupW, popupH - Hitem * 3))
        {
            indexPointer(popupX, popupY + 2 * Hitem + 5, 6, 3, 2);
        }
    }

    public void saleItem()
    {
        if (Char.getMyChar().arrItemBag[indexSelect].quantity > 1)
        {
            left = new Command(mResources.SALE, 11072);
        }
        else
        {
            left = new Command(mResources.SALE, 11073);
        }
    }

    private int upgradeEffectY(int tick)
    {
        int num = indexSize - 2;
        int num2 = tick % (4 * num);
        if (0 <= num2 && num2 < num)
        {
            return 0;
        }

        if (num <= num2 && num2 < num * 2)
        {
            return num2 % num;
        }

        if (num * 2 <= num2 && num2 < num * 3)
        {
            return num;
        }

        return num - num2 % num;
    }

    private int upgradeEffectX(int tick)
    {
        int num = indexSize - 2;
        int num2 = tick % (4 * num);
        if (0 <= num2 && num2 < num)
        {
            return num2 % num;
        }

        if (num <= num2 && num2 < num * 2)
        {
            return num;
        }

        if (num * 2 <= num2 && num2 < num * 3)
        {
            return num - num2 % num;
        }

        return 0;
    }

    public static Item getItemFocus(int typeUI)
    {
        if (indexSelect < 0)
        {
            return null;
        }

        try
        {
            switch (typeUI)
            {
                case 3:
                    return Char.getMyChar().arrItemBag[indexSelect];
                case 4:
                    return Char.getMyChar().arrItemBox[indexSelect];
                case 5:
                    return currentCharViewInfo.arrItemBody[indexSelect + indextabTrangbi];
                case 20:
                    return (arrItemNonNam.Length <= indexSelect) ? null : arrItemNonNam[indexSelect];
                case 21:
                    return (arrItemNonNu.Length <= indexSelect) ? null : arrItemNonNu[indexSelect];
                case 22:
                    return (arrItemAoNam.Length <= indexSelect) ? null : arrItemAoNam[indexSelect];
                case 23:
                    return (arrItemAoNu.Length <= indexSelect) ? null : arrItemAoNu[indexSelect];
                case 24:
                    return (arrItemGangTayNam.Length <= indexSelect) ? null : arrItemGangTayNam[indexSelect];
                case 25:
                    return (arrItemGangTayNu.Length <= indexSelect) ? null : arrItemGangTayNu[indexSelect];
                case 26:
                    return (arrItemQuanNam.Length <= indexSelect) ? null : arrItemQuanNam[indexSelect];
                case 27:
                    return (arrItemQuanNu.Length <= indexSelect) ? null : arrItemQuanNu[indexSelect];
                case 28:
                    return (arrItemGiayNam.Length <= indexSelect) ? null : arrItemGiayNam[indexSelect];
                case 29:
                    return (arrItemGiayNu.Length <= indexSelect) ? null : arrItemGiayNu[indexSelect];
                case 16:
                    return (arrItemLien.Length <= indexSelect) ? null : arrItemLien[indexSelect];
                case 17:
                    return (arrItemNhan.Length <= indexSelect) ? null : arrItemNhan[indexSelect];
                case 18:
                    return (arrItemNgocBoi.Length <= indexSelect) ? null : arrItemNgocBoi[indexSelect];
                case 19:
                    return (arrItemPhu.Length <= indexSelect) ? null : arrItemPhu[indexSelect];
                case 2:
                    return (arrItemWeapon.Length <= indexSelect) ? null : arrItemWeapon[indexSelect];
                case 6:
                    return (arrItemStack.Length <= indexSelect) ? null : arrItemStack[indexSelect];
                case 7:
                    return (arrItemStackLock.Length <= indexSelect) ? null : arrItemStackLock[indexSelect];
                case 8:
                    return (arrItemGrocery.Length <= indexSelect) ? null : arrItemGrocery[indexSelect];
                case 9:
                    return (arrItemGroceryLock.Length <= indexSelect) ? null : arrItemGroceryLock[indexSelect];
                case 14:
                    return (arrItemStore.Length <= indexSelect) ? null : arrItemStore[indexSelect];
                case 35:
                    return (arrItemElites.Length <= indexSelect) ? null : arrItemElites[indexSelect];
                case 15:
                    return (arrItemBook.Length <= indexSelect) ? null : arrItemBook[indexSelect];
                case 52:
                    return (arrItemFashion.Length <= indexSelect) ? null : arrItemFashion[indexSelect];
                case 32:
                    return (arrItemFashion.Length <= indexSelect) ? null : arrItemFashion[indexSelect];
                case 11:
                    return arrItemUpPeal[indexSelect];
                case 10:
                    return arrItemUpGrade[indexSelect];
                case 43:
                    return arrItemUpPeal[indexSelect];
                case 44:
                    return arrItemSplit[indexSelect];
                case 45:
                    return arrItemSplit[indexSelect];
                case 34:
                    return (arrItemClanShop.Length <= indexSelect) ? null : arrItemClanShop[indexSelect];
                case 46:
                    return arrItemSplit[indexSelect];
                case 47:
                    return arrItemUpGrade[indexSelect];
                case 48:
                    return arrItemSplit[indexSelect];
                case 12:
                case 13:
                case 30:
                case 31:
                case 33:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 49:
                case 50:
                case 51:
                case 99:
                    break;
            }
        }
        catch (Exception ex)
        {
            Out.println("loi tai getItemFocus " + ex.ToString() + typeUI);
        }

        return null;
    }

    public void upGrade()
    {
        int num = 0;
        for (int i = 0; i < arrItemUpGrade.Length; i++)
        {
            if (arrItemUpGrade[i] != null && arrItemUpGrade[i].template.type == 26)
            {
                num += crystals[arrItemUpGrade[i].template.id];
            }
        }

        bool flag = false;
        bool flag2 = false;
        int num2 = 0;
        if (itemUpGrade.isTypeClothe())
        {
            if (coinUpClothes[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
            {
                flag = true;
            }

            num2 = num * 100 / upClothe[itemUpGrade.upgrade];
        }
        else if (itemUpGrade.isTypeAdorn())
        {
            if (coinUpAdorns[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
            {
                flag = true;
            }

            num2 = num * 100 / upAdorn[itemUpGrade.upgrade];
        }
        else if (itemUpGrade.isTypeWeapon())
        {
            if (coinUpWeapons[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
            {
                flag = true;
            }

            num2 = num * 100 / upWeapon[itemUpGrade.upgrade];
        }

        if (isPaintUpGradeGold && goldUps[itemUpGrade.upgrade] > Char.getMyChar().luong)
        {
            flag2 = true;
        }

        if (flag)
        {
            InfoMe.addInfo(mResources.NOT_ENOUGH_COIN_UPGRADE, 15, mFont.tahoma_7_red);
        }
        else if (flag2)
        {
            InfoMe.addInfo(mResources.NOT_ENOUGH_GOLD_UPGRADE, 15, mFont.tahoma_7_red);
        }
        else if (num2 > 250)
        {
            GameCanvas.startYesNoDlg(mResources.PERCENT_BIG, 88815, null, 8882, null);
        }
        else
        {
            doUpgrade();
        }
    }

    public void doUpgrade()
    {
        if (!itemUpGrade.isLock)
        {
            GameCanvas.startYesNoDlg(mResources.CONFIRMUPGRADE, new Command(mResources.YES, 11063), new Command(mResources.NO, 1));
        }
        else
        {
            Service.gI().upgradeItem(itemUpGrade, arrItemUpGrade, isPaintUpGradeGold);
        }
    }

    public void split()
    {
        if (isPaintLuyenNgoc)
        {
            Service.gI().ngockham(1, null, itemSplit, arrItemSplit);
            return;
        }

        if (isPaintTinhluyen)
        {
            Service.gI().tinhluyen(itemSplit, arrItemSplit);
            return;
        }

        if (isPaintDichChuyen)
        {
            Service.gI().dichchuyen(itemSplit, arrItemSplit);
            return;
        }

        if (itemSplit.upgrade == 0)
        {
            GameCanvas.msgdlg.setInfo(mResources.NOT_UPGRADE, null, new Command(mResources.CLOSE, 1), null);
            GameCanvas.msgdlg.show();
            return;
        }

        int num = -1;
        int num2 = 0;
        for (int i = 0; i < Char.getMyChar().arrItemBag.Length; i++)
        {
            if (Char.getMyChar().arrItemBag[i] == null)
            {
                num++;
            }
        }

        for (int j = 0; j < arrItemSplit.Length; j++)
        {
            if (arrItemSplit[j] != null)
            {
                num2++;
            }
        }

        if (num2 > num)
        {
            GameCanvas.msgdlg.setInfo(mResources.BAG_FULL, null, new Command(mResources.CLOSE, 1), null);
            GameCanvas.msgdlg.show();
        }
        else
        {
            GameCanvas.startYesNoDlg(mResources.CONFIRMSPLIT, new Command(mResources.YES, 11087, itemSplit), new Command(mResources.NO, 1));
        }
    }

    public void updateItemInfo(int typeUI, Item item, Command left, Command right)
    {
        updateItemInfo(typeUI, item);
        if (left != null)
        {
            base.left = new Command(left.caption, 11040);
        }

        if (right != null)
        {
            base.right = new Command(right.caption, 11041);
        }
    }

    public void updateItemInfo(int typeUI, Item item)
    {
        if (item == null)
        {
            return;
        }

        itemFocus = item;
        inforW = 120;
        inforH = 120 - ((!Main.isPC) ? indexSize : 0);
        if (GameCanvas.isTouch && !GameCanvas.isTouchControlSmallScreen)
        {
            inforH += 18;
        }

        isPaintItemInfo = true;
        scrInfo.clear();
        indexRow = 0;
        if (item.expires == 0)
        {
            if (isPaintTinhluyen || isPaintDichChuyen || isPaintLuyenNgoc || isPaintKhamNgoc || isPaintThaoNgoc || isPaintGotNgoc)
            {
                Service.gI().requestItemInfo(item.typeUI, item.indexUI);
            }

            if (isPaintAuctionBuy)
            {
                Service.gI().requestItemAuction(item.itemId);
            }
            else if (currentCharViewInfo.charID == Char.getMyChar().charID)
            {
                Service.gI().requestItemInfo(typeUI, item.indexUI);
            }
            else
            {
                Service.gI().requestItemPlayer(currentCharViewInfo.charID, item.indexUI);
            }
        }

        if (typeUI == 5)
        {
            Char.getMyChar().updateKickOption();
        }

        if (!GameCanvas.isTouch || (GameCanvas.isTouch && GameCanvas.isTouchControlSmallScreen) || (isPaintInfoMe && indexMenu > 0 && indexMenu < 4))
        {
            center = cmdItemInfoClose;
            right = null;
            left = null;
        }

        GameCanvas.clearKeyHold();
        GameCanvas.clearKeyPressed();
    }

    public string[] splitMultiLine(mFont f, string str)
    {
        return f.splitFontArray(str, popupW - 20);
    }

    private void openItemSaleUI()
    {
        isPaintAuctionSale = true;
        itemSell = null;
        tfText = new TField();
        tfText.setMaxTextLenght(9);
        tfText.setIputType(TField.INPUT_TYPE_NUMERIC);
        tfText.width = 100;
        int num = ((!Main.isPC) ? (130 - indexSize) : 130);
        tfText.height = mScreen.ITEM_HEIGHT + 2;
    }

    private void sortItemStand()
    {
        if (typeSortLevel == 0 && typeSortPrice == 0 && typeSortName == 0)
        {
            return;
        }

        for (int i = 0; i < arrItemStands.Length - 1; i++)
        {
            for (int j = i + 1; j < arrItemStands.Length; j++)
            {
                if (typeSortPrice == 1)
                {
                    if (arrItemStands[i].price < arrItemStands[j].price)
                    {
                        ItemStands itemStands = arrItemStands[i];
                        arrItemStands[i] = arrItemStands[j];
                        arrItemStands[j] = itemStands;
                    }
                }
                else if (typeSortPrice == 2 && arrItemStands[i].price > arrItemStands[j].price)
                {
                    ItemStands itemStands2 = arrItemStands[i];
                    arrItemStands[i] = arrItemStands[j];
                    arrItemStands[j] = itemStands2;
                }

                if (typeSortName == 1)
                {
                    if (!arrItemStands[i].item.template.name.Equals(arrItemStands[j].item.template.name) && arrItemStands[i].item.template.name.CompareTo(arrItemStands[j].item.template.name) > 0)
                    {
                        ItemStands itemStands3 = arrItemStands[i];
                        arrItemStands[i] = arrItemStands[j];
                        arrItemStands[j] = itemStands3;
                    }
                }
                else if (typeSortName == 2 && !arrItemStands[i].item.template.name.Equals(arrItemStands[j].item.template.name) && arrItemStands[i].item.template.name.CompareTo(arrItemStands[j].item.template.name) < 0)
                {
                    ItemStands itemStands4 = arrItemStands[i];
                    arrItemStands[i] = arrItemStands[j];
                    arrItemStands[j] = itemStands4;
                }

                if (typeSortLevel == 1)
                {
                    if (arrItemStands[i].item.template.level < arrItemStands[j].item.template.level)
                    {
                        ItemStands itemStands5 = arrItemStands[i];
                        arrItemStands[i] = arrItemStands[j];
                        arrItemStands[j] = itemStands5;
                    }
                }
                else if (typeSortLevel == 2 && arrItemStands[i].item.template.level > arrItemStands[j].item.template.level)
                {
                    ItemStands itemStands6 = arrItemStands[i];
                    arrItemStands[i] = arrItemStands[j];
                    arrItemStands[j] = itemStands6;
                }
            }
        }
    }

    private void domenuSortItemStand()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.SORT_BY_PRICE, 150411));
        myVector.addElement(new Command(mResources.SORT_BY_NAME, 150412));
        myVector.addElement(new Command(mResources.SORT_BY_LEVEL, 150413));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void domenuItemStand()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.SORT, 15041));
        myVector.addElement(new Command(mResources.BUY, 15042));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void doStandToBag()
    {
        Char.getMyChar().arrItemBag[itemSell.indexUI] = itemSell;
        itemSell = null;
        left = (center = null);
    }

    private void doBagToStand()
    {
        Item item = Char.getMyChar().arrItemBag[indexSelect];
        if (item != null)
        {
            if (item.isLock || item.isExpires)
            {
                GameCanvas.startOKDlg(mResources.ONLY_NO_EXPIRE);
                return;
            }

            if (itemSell == null)
            {
                itemSell = Char.getMyChar().arrItemBag[indexSelect];
                Char.getMyChar().arrItemBag[indexSelect] = null;
                return;
            }

            item = Char.getMyChar().arrItemBag[indexSelect];
            Char.getMyChar().arrItemBag[indexSelect] = null;
            Char.getMyChar().arrItemBag[itemSell.indexUI] = itemSell;
            itemSell = item;
        }
    }

    private void actItemConvertMoveOut2()
    {
        Item item = null;
        item = arrItemConvert[2];
        arrItemConvert[2] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    private void actItemConvertMoveOut()
    {
        Item item = null;
        item = arrItemConvert[indexSelect];
        arrItemConvert[indexSelect] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    private void actBuyItemLeft2()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.SORT, 110221));
        myVector.addElement(new Command(mResources.GETINMONEY, 11050));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actBuyItemUILeft()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.SORT, 11048));
        myVector.addElement(new Command(mResources.GETOUTMONEY, 11049));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actgetLuyenThachItems()
    {
        if (requireSingleCraftMaterial())
            return;
        if (Char.getMyChar().arrItemBag[indexSelect].template.type == 26 || Char.getMyChar().arrItemBag[indexSelect].template.id == 455 || Char.getMyChar().arrItemBag[indexSelect].template.id == 456)
        {
            if (Char.getMyChar().arrItemBag[indexSelect].template.type == 26 && Char.getMyChar().arrItemBag[indexSelect].template.id < 10)
            {
                GameCanvas.startOKDlg(mResources.ONLY_LUYENTHACH5);
                return;
            }

            int num = 0;
            int num2 = 0;
            int num3 = ((arrItemUpPeal[12] != null) ? 1 : 0);
            for (int i = 0; i < arrItemUpPeal.Length; i++)
            {
                if (arrItemUpPeal[i] != null && arrItemUpPeal[i].template.type != 26)
                {
                    num = arrItemUpPeal[i].template.id;
                    num2++;
                }
            }

            for (int j = 0; j < arrItemUpPeal.Length; j++)
            {
                if (arrItemUpPeal[j] != null)
                {
                    continue;
                }

                if (Char.getMyChar().arrItemBag[indexSelect].template.type == 26)
                {
                    if (arrItemUpPeal[12] == null)
                    {
                        arrItemUpPeal[12] = Char.getMyChar().arrItemBag[indexSelect];
                        Char.getMyChar().arrItemBag[indexSelect] = null;
                    }
                    else
                    {
                        Item item = Char.getMyChar().arrItemBag[indexSelect];
                        Char.getMyChar().arrItemBag[indexSelect] = null;
                        Char.getMyChar().arrItemBag[arrItemUpPeal[12].indexUI] = arrItemUpPeal[12];
                        arrItemUpPeal[12] = item;
                    }
                }
                else if (num > 0 && Char.getMyChar().arrItemBag[indexSelect].template.id != num)
                {
                    GameCanvas.startOKDlg(mResources.ONLY_LUYENTHACH6);
                }
                else if ((num3 == 1 && num2 >= 3) || num2 >= 9)
                {
                    GameCanvas.startOKDlg(mResources.MAX_LUYENTHACH1);
                }
                else
                {
                    arrItemUpPeal[j] = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                }

                left = (center = null);
                updateCommandForUI();
                return;
            }

            GameCanvas.msgdlg.setInfo(mResources.CRYSTAL_FULL, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
        else
        {
            if (Char.getMyChar().arrItemBag[indexSelect].template.id == 457)
            {
                GameCanvas.msgdlg.setInfo(mResources.ONLY_LUYENTHACH1, null, new Command(mResources.CLOSE, 2), null);
            }
            else
            {
                GameCanvas.msgdlg.setInfo(mResources.ONLY_LUYENTHACH2, null, new Command(mResources.CLOSE, 2), null);
            }

            GameCanvas.msgdlg.show();
        }
    }

    private void actUpPearlItems()
    {
        if (requireSingleCraftMaterial())
            return;
        if (Char.getMyChar().arrItemBag[indexSelect].template.type == 26)
        {
            for (int i = 0; i < arrItemUpPeal.Length; i++)
            {
                if (arrItemUpPeal[i] == null)
                {
                    arrItemUpPeal[i] = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    left = (center = null);
                    updateCommandForUI();
                    return;
                }
            }

            GameCanvas.msgdlg.setInfo(mResources.CRYSTAL_FULL, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
        else
        {
            GameCanvas.msgdlg.setInfo(mResources.ONLY_CRYSTAL, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void actSplitItemBag()
    {
        try
        {
            if (isPaintTinhluyen)
            {
                if (Char.getMyChar().arrItemBag[indexSelect].isTypeBody() || Char.getMyChar().arrItemBag[indexSelect].isTypeMounts() || Char.getMyChar().arrItemBag[indexSelect].template.id == 455 || Char.getMyChar().arrItemBag[indexSelect].template.id == 456 || Char.getMyChar().arrItemBag[indexSelect].template.id == 457)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect].template.id == 455 || Char.getMyChar().arrItemBag[indexSelect].template.id == 456 || Char.getMyChar().arrItemBag[indexSelect].template.id == 457)
                    {
                        if (arrItemSplit == null)
                        {
                            arrItemSplit = new Item[24];
                        }

                        for (int i = 0; i < arrItemSplit.Length; i++)
                        {
                            if (arrItemSplit[i] == null)
                            {
                                arrItemSplit[i] = Char.getMyChar().arrItemBag[indexSelect];
                                Char.getMyChar().arrItemBag[indexSelect] = null;
                                left = (center = null);
                                updateCommandForUI();
                                break;
                            }

                            if (i == arrItemSplit.Length - 1)
                            {
                                GameCanvas.startOKDlg(mResources.CRYSTAL_FULL);
                            }
                        }

                        return;
                    }

                    ItemOption itemOption = null;
                    for (int j = 0; j < Char.getMyChar().arrItemBag[indexSelect].options.size(); j++)
                    {
                        itemOption = (ItemOption)Char.getMyChar().arrItemBag[indexSelect].options.elementAt(j);
                        if (itemOption.optionTemplate.id == 85)
                        {
                            break;
                        }

                        itemOption = null;
                    }

                    if (itemOption != null && itemOption.param >= 9)
                    {
                        GameCanvas.startOKDlg(mResources.MAX_TINHLUYEN);
                        return;
                    }

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
                    GameCanvas.msgdlg.setInfo(mResources.CAN_NOT_TINHLUYEN, null, new Command(mResources.CLOSE, 2), null);
                    GameCanvas.msgdlg.show();
                }
            }
            else if (Char.getMyChar().arrItemBag[indexSelect].isTypeBody() && Char.getMyChar().arrItemBag[indexSelect].upgrade > 0)
            {
                if (itemSplit == null)
                {
                    itemSplit = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                }
                else
                {
                    Item item2 = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    Char.getMyChar().arrItemBag[itemSplit.indexUI] = itemSplit;
                    itemSplit = item2;
                }

                if (itemSplit != null)
                {
                    int num = 0;
                    if (itemSplit.isTypeClothe())
                    {
                        for (int k = 0; k < itemSplit.upgrade; k++)
                        {
                            num += upClothe[k];
                        }
                    }
                    else if (itemSplit.isTypeAdorn())
                    {
                        for (int l = 0; l < itemSplit.upgrade; l++)
                        {
                            num += upAdorn[l];
                        }
                    }
                    else if (itemSplit.isTypeWeapon())
                    {
                        for (int m = 0; m < itemSplit.upgrade; m++)
                        {
                            num += upWeapon[m];
                        }
                    }

                    num /= 2;
                    int num2 = 0;
                    arrItemSplit = new Item[24];
                    for (int num3 = crystals.Length - 1; num3 >= 0; num3--)
                    {
                        if (num >= crystals[num3])
                        {
                            arrItemSplit[num2] = new Item();
                            arrItemSplit[num2].typeUI = 3;
                            arrItemSplit[num2].template = ItemTemplates.get((short)num3);
                            arrItemSplit[num2].isLock = itemSplit.isLock;
                            arrItemSplit[num2].expires = -1L;
                            num -= crystals[num3];
                            num3++;
                            num2++;
                        }
                    }
                }

                left = (center = null);
                updateCommandForUI();
            }
            else
            {
                GameCanvas.msgdlg.setInfo(mResources.ONLY_BODY, null, new Command(mResources.CLOSE, 2), null);
                GameCanvas.msgdlg.show();
            }
        }
        catch (Exception)
        {
            GameCanvas.msgdlg.setInfo(mResources.CAN_NOT_TINHLUYEN, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void actSplitItem()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(cmdSplitMoveOut);
        if (itemSplit != null)
        {
            myVector.addElement(new Command(mResources.BEGIN, 111031));
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actUpgrade_HanhTrang()
    {
        if (requireSingleCraftMaterial())
            return;
        if (Char.getMyChar().arrItemBag[indexSelect].isTypeBody())
        {
            if (Char.getMyChar().arrItemBag[indexSelect].template.level < 10 || Char.getMyChar().arrItemBag[indexSelect].template.type >= 10)
            {
                GameCanvas.msgdlg.setInfo(mResources.BODY_NOTUPGRADE, null, new Command(mResources.CLOSE, 2), null);
                GameCanvas.msgdlg.show();
                return;
            }

            if (Char.getMyChar().arrItemBag[indexSelect].upgrade >= Char.getMyChar().arrItemBag[indexSelect].getUpMax())
            {
                GameCanvas.msgdlg.setInfo(mResources.BODY_MAXUPGRADE, null, new Command(mResources.CLOSE, 2), null);
                GameCanvas.msgdlg.show();
                return;
            }

            if (itemUpGrade == null)
            {
                itemUpGrade = Char.getMyChar().arrItemBag[indexSelect];
                Char.getMyChar().arrItemBag[indexSelect] = null;
            }
            else
            {
                Item item = Char.getMyChar().arrItemBag[indexSelect];
                Char.getMyChar().arrItemBag[indexSelect] = null;
                Char.getMyChar().arrItemBag[itemUpGrade.indexUI] = itemUpGrade;
                itemUpGrade = item;
            }

            left = (center = null);
            updateCommandForUI();
        }
        else if (Char.getMyChar().arrItemBag[indexSelect].template.type == 26 || Char.getMyChar().arrItemBag[indexSelect].template.type == 28)
        {
            if (Char.getMyChar().arrItemBag[indexSelect].template.type == 28)
            {
                for (int i = 0; i < arrItemUpGrade.Length; i++)
                {
                    if (arrItemUpGrade[i] != null && arrItemUpGrade[i].template.type == 28)
                    {
                        Item item2 = Char.getMyChar().arrItemBag[indexSelect];
                        Char.getMyChar().arrItemBag[indexSelect] = null;
                        Char.getMyChar().arrItemBag[arrItemUpGrade[i].indexUI] = arrItemUpGrade[i];
                        arrItemUpGrade[i] = item2;
                        return;
                    }
                }
            }

            for (int j = 0; j < arrItemUpGrade.Length; j++)
            {
                if (arrItemUpGrade[j] == null)
                {
                    arrItemUpGrade[j] = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    left = (center = null);
                    updateCommandForUI();
                    return;
                }
            }

            GameCanvas.msgdlg.setInfo(mResources.CRYSTAL_FULL, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
        else
        {
            GameCanvas.msgdlg.setInfo(mResources.ONLY_UPGRADE, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void actItemUpgradeMoveOut()
    {
        Item item = getItemFocus(10);
        arrItemUpGrade[indexSelect] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    private void actUpgradeBag()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.MOVEOUT, 111001));
        if (itemUpGrade != null)
        {
            for (int i = 0; i < arrItemUpGrade.Length; i++)
            {
                if (arrItemUpGrade[i] != null)
                {
                    myVector.addElement(new Command(mResources.BEGIN, 110981));
                    break;
                }
            }
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actUpgradeLeft()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(cmdUpgradeMoveOut);
        for (int i = 0; i < arrItemUpGrade.Length; i++)
        {
            if (arrItemUpGrade[i] != null)
            {
                myVector.addElement(new Command(mResources.BEGIN, 110981));
                break;
            }
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actSaleItem()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.SALE, 110721));
        myVector.addElement(new Command(mResources.SALES, 110722));
        myVector.addElement(new Command(mResources.SALEALL, 110723));
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actConfirmUpgrade()
    {
        GameCanvas.endDlg();
        Service.gI().upgradeItem(itemUpGrade, arrItemUpGrade, isPaintUpGradeGold);
    }

    private void doItemChangeMap(Item itemBag)
    {
        MyVector myVector = new MyVector();
        for (int i = 1; i < mResources.TELEPORT[3].Length; i++)
        {
            myVector.addElement(new Command(mResources.TELEPORT[3][i], 110531, itemBag));
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actUseItem()
    {
        GameCanvas.endDlg();
        Service.gI().useItem(indexSelect);
    }

    private void actBoxSort()
    {
        Service.gI().boxSort();
    }

    private void actrightItemInfo()
    {
        doCloseItemInfo();
        if (right != null)
        {
            right.performAction();
        }
    }

    private void actleftItemInfo()
    {
        doCloseItemInfo();
        if (left != null)
        {
            left.performAction();
        }
    }

    private void actBagSortItem()
    {
        Service.gI().bagSort();
    }

    private void actSplitMoveOut()
    {
        Item item = itemSplit;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        itemSplit = null;
        if (isPaintTinhluyen || isPaintDichChuyen || isPaintLuyenNgoc)
        {
            return;
        }

        if (arrItemSplit != null)
        {
            for (int i = 0; i < arrItemSplit.Length; i++)
            {
                arrItemSplit[i] = null;
            }
        }

        left = (center = null);
        updateCommandForUI();
    }

    private void actUpgradeMoveOut()
    {
        Item item = itemUpGrade;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        itemUpGrade = null;
        left = (center = null);
        updateCommandForUI();
    }

    private void actTradeViewItemInfo()
    {
        Item item = arrItemTradeMe[indexSelect];
        updateItemInfo(3, item);
    }

    private void actTradeSelectItem()
    {
        if (!Char.getMyChar().arrItemBag[indexSelect].isLock)
        {
            for (int i = 0; i < arrItemTradeMe.Length; i++)
            {
                if (arrItemTradeMe[i] == null)
                {
                    arrItemTradeMe[i] = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    left = (center = null);
                    updateCommandForUI();
                    return;
                }
            }

            GameCanvas.msgdlg.setInfo(mResources.TRADE_FULL, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
        else
        {
            GameCanvas.msgdlg.setInfo(mResources.ONLY_NO_LOCK, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void actTradeSelectInBag()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(cmdTradeSelectItem);
        myVector.addElement(cmdTradeSendMoney);
        GameCanvas.menu.startAt(myVector, 0);
    }

    private void actBagUseItem()
    {
        try
        {
            Item item = getItemFocus(3);
            if (item.template.gender != 2 && item.template.gender != Char.getMyChar().cgender)
            {
                GameCanvas.msgdlg.setInfo(mResources.BODY_ERROR, null, new Command(mResources.CLOSE, 1), null);
                GameCanvas.msgdlg.show();
            }
            else if (item.template.level > Char.getMyChar().clevel)
            {
                GameCanvas.msgdlg.setInfo(mResources.LEVEL_ENOUGH, null, new Command(mResources.CLOSE, 1), null);
                GameCanvas.msgdlg.show();
            }
            else if ((item.isTypeBody() || item.isTypeMounts()) && !item.isLock)
            {
                GameCanvas.startYesNoDlg(mResources.LOCKITEM, new Command(mResources.YES, 11051, null), new Command(mResources.NO, 1));
            }
            else if (item.template.id == 35 || item.template.id == 37)
            {
                MyVector myVector = new MyVector();
                for (int i = 0; i < 3; i++)
                {
                    myVector.addElement(new Command(mResources.TELEPORT[i][0], 11052, item));
                }

                myVector.addElement(new Command(mResources.TELEPORT[3][0], 11053, item));
                myVector.addElement(new Command(mResources.TELEPORT[4][0], 11054));
                GameCanvas.menu.startAt(myVector, 3);
            }
            else if (item.template.id == 514)
            {
                GameCanvas.endDlg();
                GameCanvas.input2Dlg.setTitle("Đến: ", "Lời chúc: ");
                GameCanvas.input2Dlg.show("Chúc tết", new Command(mResources.CLOSE, GameCanvas.instance, 8882, null), new Command("Gửi", GameCanvas.instance, 1608, null), TField.INPUT_TYPE_ANY, TField.INPUT_TYPE_ANY);
            }
            else if (item.template.id == 515)
            {
                GameCanvas.endDlg();
                GameCanvas.input2Dlg.setTitle("Đến: ", "Lời chúc: ");
                GameCanvas.input2Dlg.show("Chúc tết", new Command(mResources.CLOSE, GameCanvas.instance, 8882, null), new Command("Gửi", GameCanvas.instance, 16081, null), TField.INPUT_TYPE_ANY, TField.INPUT_TYPE_ANY);
            }
            else
            {
                Service.gI().useItem(indexSelect);
            }
        }
        catch (Exception)
        {
        }
    }

    private void actBagUseAllItem()
    {
        Handle.acUse = true;
        Item var0;
        if ((var0 = getItemFocus(3)) != null)
        {
            StartAutoUseThread(var0);
        }
        else
        {
            Handle.acUse = false;
        }
    }

    private void actBagThrowItem()
    {
        if (Char.getMyChar().arrItemBag[indexSelect] != null)
        {
            if (Char.getMyChar().arrItemBag[indexSelect].isLock)
            {
                InfoMe.addInfo(mResources.ITEM_LOCK);
            }
            else
            {
                GameCanvas.startYesNoDlg(mResources.REMOVEVP, new Command(mResources.YES, 2001), new Command(mResources.NO, 2002));
            }
        }
    }

    private void actBagViewItemInfo()
    {
        if (indexTitle == 1)
        {
            Item item = getItemFocus(3);
            if (GameCanvas.isTouchControlLargeScreen)
            {
                updateItemInfo(3, item, cmdBagUseItem, null);
            }
            else
            {
                updateItemInfo(3, item, null, null);
            }
        }
    }

    private void actBagSelectItem()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(cmdBagUseItem);
        myVector.addElement(cmdBagThrowItem);
        if (Char.getMyChar().arrItemBag[indexSelect] != null)
        {
            if (checkAutouse((int)Char.getMyChar().arrItemBag[indexSelect].template.id))
            {
                myVector.addElement(new Command("Tắt Tự dùng", 110263));
            }
            else
            {
                myVector.addElement(new Command("Tự dùng", 110262));
            }
        }

        if (Char.getMyChar().arrItemBag[indexSelect] != null && Char.getMyChar().arrItemBag[indexSelect].quantity > 1)
        {
            myVector.addElement(new Command("Mở all", 110261));
            myVector.addElement(cmdBagSplitItem);
        }

        myVector.addElement(new Command(mResources.SORT, 110221));
        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actBagSplitItem()
    {
        showStackSplit(false);
    }

    // Offline stack splitting reuses the existing numeric dialog and inputNumSplit command.
    private void showStackSplit(bool box)
    {
        Item[] slots = box ? Char.getMyChar().arrItemBox : Char.getMyChar().arrItemBag;
        if (slots == null || indexSelect < 0 || indexSelect >= slots.Length)
            return;
        Item item = slots[indexSelect];
        if (item == null || item.quantity <= 1 || !NsoServer.Store.InventoryGrouping.CanStack(item.template.type, item.template.isUpToUp))
            return;
        GameCanvas.inputDlg.tfInput.setMaxTextLenght(5);
        GameCanvas.inputDlg.show(mResources.INPUT_NUMSPLIT, new Command("OK", GameCanvas.instance, 88835, (box ? "box:" : "") + indexSelect), TField.INPUT_TYPE_NUMERIC);
    }

    private bool requireSingleCraftMaterial()
    {
        var bag = Char.getMyChar().arrItemBag;
        if (indexSelect < 0 || indexSelect >= bag.Length || bag[indexSelect] == null)
            return false;
        Item item = bag[indexSelect];
        if (item.quantity <= 1 || (item.template.type != 26 && item.template.type != 28))
            return false;
        GameCanvas.startOKDlg("Hãy tách nguyên liệu thành từng viên trước khi nâng cấp/luyện. Dùng Tách số lượng trong hành trang.");
        return true;
    }

    // Offline exception requested by the user: only enabled Thien bien lenh may auto-pick.
    // Ordinary play stays manual. Use normal server validation and leave distant loot in place.
    public static void autoPickItem()
    {
        Char c = Char.getMyChar();
        if (!isUseitemAuto || auto != 1 || c.cHP <= 0 || c.statusMe == 14 || c.statusMe == 5 || GameCanvas.menu.showMenu || GameCanvas.currentDialog != null || ChatTextField.gI().isShow || gI().isPaintPopup() || gI().isPaintUI())
            return;
        long now = mSystem.currentTimeMillis();
        if (now < nextItemAutoPickup)
            return;
        nextItemAutoPickup = now + System.Math.Max(100, ItemAutoPickupIntervalMs);
        int radius = System.Math.Max(24, System.Math.Min(145, ItemAutoPickupRadius));
        int sent = 0;
        for (int i = 0; i < vItemMap.size() && sent < 8; i++)
        {
            ItemMap item = (ItemMap)vItemMap.elementAt(i);
            if (item == null || item.template == null || item.status != 1)
                continue;
            long dx = c.cx - item.x, dy = c.cy - item.y;
            if (dx * dx + dy * dy > radius * radius)
                continue;
            bool room = item.template.type == 19 || item.template.id == 218;
            if (!room && c.arrItemBag != null)
                foreach (Item bagItem in c.arrItemBag)
                    if (bagItem == null || (item.template.isUpToUp && bagItem.template != null && bagItem.template.id == item.template.id))
                    {
                        room = true;
                        break;
                    }

            if (!room)
                continue;
            Service.gI().pickItem(item.itemMapID);
            sent++;
        }
    }

    private void actSplitItemBag_luyenngoc()
    {
        try
        {
            if (!isPaintLuyenNgoc || !Char.getMyChar().arrItemBag[indexSelect].isTypeNgocKham())
            {
                return;
            }

            if (itemSplit == null)
            {
                if (Char.getMyChar().arrItemBag[indexSelect].upgrade < 10)
                {
                    itemSplit = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                }
                else
                {
                    GameCanvas.msgdlg.setInfo(mResources.CAN_NOT_LUYENNGOC, null, new Command(mResources.CLOSE, 2), null);
                    GameCanvas.msgdlg.show();
                }

                return;
            }

            if (arrItemSplit == null)
            {
                arrItemSplit = new Item[24];
            }

            for (int i = 0; i < arrItemSplit.Length; i++)
            {
                if (arrItemSplit[i] == null)
                {
                    arrItemSplit[i] = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    left = (center = null);
                    updateCommandForUI();
                    break;
                }

                if (i == arrItemSplit.Length - 1)
                {
                    GameCanvas.startOKDlg(mResources.CRYSTAL_FULL);
                }
            }
        }
        catch (Exception)
        {
            GameCanvas.msgdlg.setInfo(mResources.CAN_NOT_LUYENNGOC, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void actSplitItemBag_gotngoc()
    {
        try
        {
            if (isPaintGotNgoc)
            {
                if (Char.getMyChar().arrItemBag[indexSelect].isTypeNgocKham())
                {
                    itemSplit = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                }
            }
            else if (isPaintThaoNgoc && Char.getMyChar().arrItemBag[indexSelect].isTypeBody())
            {
                itemSplit = Char.getMyChar().arrItemBag[indexSelect];
                Char.getMyChar().arrItemBag[indexSelect] = null;
            }
        }
        catch (Exception)
        {
            GameCanvas.msgdlg.setInfo(mResources.CAN_NOT_GOTNGOC, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void setCMD_luyenngoc()
    {
        try
        {
            if (indexMenu == 0)
            {
                if (indexTitle == 1)
                {
                    if (itemSplit != null)
                    {
                        left = new Command(mResources.SELECT, 11103);
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }

                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(3, itemSplit);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, itemSplit);
                    }
                }
                else
                {
                    if (indexTitle != 2)
                    {
                        return;
                    }

                    Item item = arrItemSplit[indexSelect];
                    if (item != null)
                    {
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(3, item);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, item);
                        }

                        left = new Command(mResources.MOVEOUT, 1605);
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }

                    if (itemSplit != null)
                    {
                        left = new Command(mResources.SELECT, 1604);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] == null)
                {
                    left = null;
                    isPaintItemInfo = false;
                }
                else
                {
                    left = new Command(mResources.SELECT, 222);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void setCMD_khamngoc()
    {
        if (indexMenu == 0)
        {
            if (indexTitle == 1)
            {
                if (itemSplit != null && indexSelect == 0)
                {
                    left = new Command(mResources.SELECT, 338);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        isViewNext = false;
                        updateItemInfo(3, itemSplit);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 335);
                    }
                }

                if (itemUpGrade != null && indexSelect == 1)
                {
                    left = new Command(mResources.SELECT, 344);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        isViewNext = false;
                        updateItemInfo(3, itemUpGrade);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 336);
                    }
                }

                if (itemSplit == null && itemUpGrade == null)
                {
                    isPaintItemInfo = false;
                }
            }
            else
            {
                if (indexTitle != 2)
                {
                    return;
                }

                Item item = getItemFocus(47);
                isViewNext = false;
                if (item != null)
                {
                    left = new Command(mResources.SELECT, 345);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(3, item);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11101);
                    }

                    return;
                }

                left = null;
                isPaintItemInfo = false;
                if (itemUpGrade == null || itemSplit == null)
                {
                    return;
                }

                for (int i = 0; i < arrItemUpGrade.Length; i++)
                {
                    if (arrItemUpGrade[i] != null)
                    {
                        left = new Command(mResources.BEGIN, 341);
                        break;
                    }
                }
            }
        }
        else if (indexMenu == 1)
        {
            if (Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                left = new Command(mResources.SELECT, 337);
            }
            else
            {
                left = null;
            }
        }
    }

    private void act_update_khamngoc()
    {
        if (requireSingleCraftMaterial())
            return;
        if (Char.getMyChar().arrItemBag[indexSelect].isTypeNgocKham())
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

            left = (center = null);
            updateCommandForUI();
        }
        else if (Char.getMyChar().arrItemBag[indexSelect].isTypeBody())
        {
            if (itemUpGrade == null)
            {
                itemUpGrade = Char.getMyChar().arrItemBag[indexSelect];
                Char.getMyChar().arrItemBag[indexSelect] = null;
            }
            else
            {
                Item item2 = Char.getMyChar().arrItemBag[indexSelect];
                Char.getMyChar().arrItemBag[indexSelect] = null;
                Char.getMyChar().arrItemBag[itemUpGrade.indexUI] = itemUpGrade;
                itemUpGrade = item2;
            }

            left = (center = null);
            updateCommandForUI();
        }
        else if (Char.getMyChar().arrItemBag[indexSelect].template.type == 26 || Char.getMyChar().arrItemBag[indexSelect].template.type == 28)
        {
            if (Char.getMyChar().arrItemBag[indexSelect].template.type == 28)
            {
                for (int i = 0; i < arrItemUpGrade.Length; i++)
                {
                    if (arrItemUpGrade[i] != null && arrItemUpGrade[i].template.type == 28)
                    {
                        Item item3 = Char.getMyChar().arrItemBag[indexSelect];
                        Char.getMyChar().arrItemBag[indexSelect] = null;
                        Char.getMyChar().arrItemBag[arrItemUpGrade[i].indexUI] = arrItemUpGrade[i];
                        arrItemUpGrade[i] = item3;
                        return;
                    }
                }
            }

            for (int j = 0; j < arrItemUpGrade.Length; j++)
            {
                if (arrItemUpGrade[j] == null)
                {
                    arrItemUpGrade[j] = Char.getMyChar().arrItemBag[indexSelect];
                    Char.getMyChar().arrItemBag[indexSelect] = null;
                    left = (center = null);
                    updateCommandForUI();
                    return;
                }
            }

            GameCanvas.msgdlg.setInfo(mResources.CRYSTAL_FULL, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
        else
        {
            GameCanvas.msgdlg.setInfo(mResources.ONLY_UPGRADE, null, new Command(mResources.CLOSE, 2), null);
            GameCanvas.msgdlg.show();
        }
    }

    private void setCMD_act_KhamNgoc(int type)
    {
        MyVector myVector = new MyVector();
        switch (type)
        {
            case 0:
                myVector.addElement(cmdKhamNgocMoveOut_Ngoc);
                break;
            case 1:
                myVector.addElement(cmdKhamNgocMoveOut_Item);
                break;
            case 2:
                myVector.addElement(cmdKhamNgocMoveOut_Stone);
                break;
        }

        if (itemUpGrade != null && itemSplit != null)
        {
            for (int i = 0; i < arrItemUpGrade.Length; i++)
            {
                if (arrItemUpGrade[i] != null)
                {
                    myVector.addElement(new Command(mResources.BEGIN, 341));
                    break;
                }
            }
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    public void khamngoc()
    {
        int num = 0;
        for (int i = 0; i < arrItemUpGrade.Length; i++)
        {
            if (arrItemUpGrade[i] != null && arrItemUpGrade[i].template.type == 26)
            {
                num += crystals[arrItemUpGrade[i].template.id];
            }
        }

        bool flag = false;
        bool flag2 = false;
        int num2 = 0;
        if (itemSplit != null)
        {
            if (coinUpWeapons[itemSplit.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
            {
                flag = true;
            }

            num2 = num * 100 / upWeapon[itemSplit.upgrade];
        }

        if (flag)
        {
            InfoMe.addInfo(mResources.NOT_ENOUGH_COIN_UPGRADE, 15, mFont.tahoma_7_red);
        }
        else if (flag2)
        {
            InfoMe.addInfo(mResources.NOT_ENOUGH_GOLD_UPGRADE, 15, mFont.tahoma_7_red);
        }
        else if (num2 > 250)
        {
            GameCanvas.startYesNoDlg(mResources.PERCENT_BIG, new Command(mResources.YES, 342), new Command(mResources.NO, 1));
        }
        else
        {
            sendKhamNgoc();
        }
    }

    public void sendKhamNgoc()
    {
        if (itemSplit != null && itemUpGrade != null && arrItemUpGrade.Length > 0)
        {
            if (!itemUpGrade.isLock)
            {
                GameCanvas.startYesNoDlg(mResources.CONFIRMUPGRADE, new Command(mResources.YES, 342), new Command(mResources.NO, 1));
            }
            else
            {
                Service.gI().ngockham(0, itemUpGrade, itemSplit, arrItemUpGrade);
            }
        }
    }

    private void actConfirmKhamNgoc()
    {
        GameCanvas.endDlg();
        if (itemSplit != null && itemUpGrade != null && arrItemUpGrade.Length > 0)
        {
            Service.gI().ngockham(0, itemUpGrade, itemSplit, arrItemUpGrade);
        }
    }

    private void openGotNgocUI()
    {
        isPaintGotNgoc = true;
        itemSplit = null;
    }

    private void openThaoNgocUI()
    {
        isPaintThaoNgoc = true;
        itemSplit = null;
    }

    private void setCMD_gotngoc()
    {
        if (indexMenu == 0)
        {
            if (itemSplit != null)
            {
                left = new Command(mResources.SELECT, 11103);
            }
            else
            {
                isPaintItemInfo = false;
            }

            if (GameCanvas.isTouchControlLargeScreen)
            {
                updateItemInfo(3, itemSplit);
            }
            else
            {
                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, itemSplit);
            }
        }
        else if (indexMenu == 1)
        {
            if (Char.getMyChar().arrItemBag[indexSelect] == null)
            {
                left = null;
                isPaintItemInfo = false;
            }
            else
            {
                left = new Command(mResources.SELECT, 405);
            }
        }
    }

    private void act_update_giaodo()
    {
        for (int i = 0; i < arrItemSplit.Length; i++)
        {
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

    private void setCMD_giaodo()
    {
        if (indexMenu == 0)
        {
            Item item = getItemFocus(48);
            isViewNext = false;
            if (item != null)
            {
                left = new Command(mResources.SELECT, 401);
                if (GameCanvas.isTouchControlLargeScreen)
                {
                    updateItemInfo(3, item);
                }
                else
                {
                    center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11101);
                }

                return;
            }

            for (int i = 0; i < arrItemSplit.Length; i++)
            {
                if (arrItemSplit[i] != null)
                {
                    left = new Command(mResources.TITLEGIAODO[0], 403);
                    break;
                }
            }
        }
        else if (indexMenu == 1)
        {
            if (Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                left = new Command(mResources.SELECT, 400);
            }
            else
            {
                left = null;
            }
        }
    }

    private void setCMD_act_GiaoDo()
    {
        MyVector myVector = new MyVector();
        for (int i = 0; i < arrItemSplit.Length; i++)
        {
            if (arrItemSplit[i] != null)
            {
                myVector.addElement(cmdGiaoDoMoveOut);
                break;
            }
        }

        if (arrItemSplit.Length > 0)
        {
            myVector.addElement(new Command(mResources.TITLEGIAODO[0], 403));
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    public void sendGiaoDo()
    {
        Service.gI().giaodo(arrItemSplit);
    }
}
