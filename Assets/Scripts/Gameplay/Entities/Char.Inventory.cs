using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public static bool checkItem(int var0)
    {
        Item[] var1 = getMyChar().arrItemBag;
        for (int var2 = 0; var2 < var1.Length; ++var2)
        {
            if (var1[var2] != null && var1[var2].template.id == var0)
            {
                return true;
            }
        }

        return false;
    }

    public static Item checkItem2(int var0)
    {
        Item[] var1 = getMyChar().arrItemBag;
        for (int var2 = 0; var2 < var1.Length; ++var2)
        {
            if (var1[var2] != null && var1[var2].template.id == var0)
            {
                return var1[var2];
            }
        }

        return null;
    }

    public static int checkBag()
    {
        Item[] var0 = getMyChar().arrItemBag;
        int var1 = 0;
        for (int var2 = 0; var2 < var0.Length; ++var2)
        {
            if (var0[var2] == null)
            {
                ++var1;
            }
        }

        return var1;
    }

    public static int checkIndexBag(int var0)
    {
        Item[] var1 = getMyChar().arrItemBag;
        for (int var2 = 0; var2 < var1.Length; ++var2)
        {
            if (var1[var2] != null && var1[var2].template.id == var0)
            {
                return var2;
            }
        }

        return -1;
    }

    public void bagSort()
    {
        try
        {
            MyVector myVector = new MyVector();
            for (int i = 0; i < arrItemBag.Length; i++)
            {
                Item item = arrItemBag[i];
                if (item != null && NsoServer.Store.InventoryGrouping.CanStack(item.template.type, item.template.isUpToUp) && !item.isExpires)
                {
                    myVector.addElement(item);
                }
            }

            for (int j = 0; j < myVector.size(); j++)
            {
                Item item2 = (Item)myVector.elementAt(j);
                if (item2 == null)
                {
                    continue;
                }

                for (int k = j + 1; k < myVector.size(); k++)
                {
                    Item item3 = (Item)myVector.elementAt(k);
                    if (item3 != null && item2.template.Equals(item3.template) && item2.isLock == item3.isLock && (long)item2.quantity + item3.quantity <= NsoServer.Store.InventoryGrouping.MaxStackQuantity)
                    {
                        item2.quantity += item3.quantity;
                        arrItemBag[item3.indexUI] = null;
                        myVector.setElementAt(null, k);
                    }
                }
            }

            for (int l = 0; l < arrItemBag.Length; l++)
            {
                if (arrItemBag[l] == null)
                {
                    continue;
                }

                for (int m = 0; m <= l; m++)
                {
                    if (arrItemBag[m] == null)
                    {
                        arrItemBag[m] = arrItemBag[l];
                        arrItemBag[m].indexUI = m;
                        arrItemBag[l] = null;
                        break;
                    }
                }
            }

            // Offline grouping uses the same stable order as the server acknowledgement.
            NsoServer.Store.InventoryGrouping.Group(arrItemBag, item => item.template.type, item => item.template.id, item => item.isLock);
            for (int i = 0; i < arrItemBag.Length; i++)
                if (arrItemBag[i] != null)
                    arrItemBag[i].indexUI = i;
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
            Out.println("Char.bagSort()");
        }
    }

    public void boxSort()
    {
        try
        {
            MyVector myVector = new MyVector();
            for (int i = 0; i < arrItemBox.Length; i++)
            {
                Item item = arrItemBox[i];
                if (item != null && NsoServer.Store.InventoryGrouping.CanStack(item.template.type, item.template.isUpToUp) && !item.isExpires)
                {
                    myVector.addElement(item);
                }
            }

            for (int j = 0; j < myVector.size(); j++)
            {
                Item item2 = (Item)myVector.elementAt(j);
                if (item2 == null)
                {
                    continue;
                }

                for (int k = j + 1; k < myVector.size(); k++)
                {
                    Item item3 = (Item)myVector.elementAt(k);
                    if (item3 != null && item2.template.Equals(item3.template) && item2.isLock == item3.isLock && (long)item2.quantity + item3.quantity <= NsoServer.Store.InventoryGrouping.MaxStackQuantity)
                    {
                        item2.quantity += item3.quantity;
                        arrItemBox[item3.indexUI] = null;
                        myVector.setElementAt(null, k);
                    }
                }
            }

            for (int l = 0; l < arrItemBox.Length; l++)
            {
                if (arrItemBox[l] == null)
                {
                    continue;
                }

                for (int m = 0; m <= l; m++)
                {
                    if (arrItemBox[m] == null)
                    {
                        arrItemBox[m] = arrItemBox[l];
                        arrItemBox[m].indexUI = m;
                        arrItemBox[l] = null;
                        break;
                    }
                }
            }

            // Offline grouping uses the same stable order as the server acknowledgement.
            NsoServer.Store.InventoryGrouping.Group(arrItemBox, item => item.template.type, item => item.template.id, item => item.isLock);
            for (int i = 0; i < arrItemBox.Length; i++)
                if (arrItemBox[i] != null)
                    arrItemBox[i].indexUI = i;
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
            Out.println("Char.boxSort()");
        }
    }

    public bool useItemType(int var1)
    {
        if (this.arrItemBag == null)
        {
            return false;
        }
        else
        {
            for (int var2 = 0; var2 < this.arrItemBag.Length; ++var2)
            {
                if (this.arrItemBag[var2] != null && this.arrItemBag[var2].template.type == var1 && this.arrItemBag[var2].template.level <= myChar.clevel)
                {
                    Service.gI().useItem(var2);
                    return true;
                }
            }

            return false;
        }
    }

    public void useItem(int indexUI)
    {
        Item item = arrItemBag[indexUI];
        if (item.isTypeBody())
        {
            item.isLock = true;
            item.typeUI = 5;
            Item item2 = arrItemBody[item.template.type];
            arrItemBag[indexUI] = null;
            if (item2 != null)
            {
                item2.typeUI = 3;
                arrItemBody[item.template.type] = null;
                item2.indexUI = indexUI;
                arrItemBag[indexUI] = item2;
            }

            item.indexUI = item.template.type;
            arrItemBody[item.indexUI] = item;
            for (int i = 0; i < arrItemBody.Length; i++)
            {
                Item item3 = arrItemBody[i];
                if (item3 != null)
                {
                    if (item3.template.type == 1)
                    {
                        wp = item3.template.part;
                    }
                    else if (item3.template.type == 2)
                    {
                        body = item3.template.part;
                    }
                    else if (item3.template.type == 6)
                    {
                        leg = item3.template.part;
                    }
                }
            }
        }
        else
        {
            if (!item.isTypeMounts())
            {
                return;
            }

            item.isLock = true;
            item.typeUI = 41;
            arrItemBag[indexUI] = null;
            for (int j = 0; j < arrItemMounts.Length; j++)
            {
                int num = item.template.type - 29;
                if (num == j)
                {
                    Item item4 = arrItemMounts[num];
                    if (item4 != null)
                    {
                        item4.typeUI = 41;
                        arrItemMounts[num] = null;
                        item4.indexUI = indexUI;
                        arrItemBag[indexUI] = item4;
                    }

                    item.indexUI = item.template.type;
                    arrItemMounts[num] = item;
                    break;
                }
            }
        }
    }

    private void autoPickItemMap()
    {
        if (!me || cHP <= 0 || statusMe == 14 || statusMe == 5 || testCharId != -9999)
        {
            return;
        }

        int num = 70;
        if (GameScr.isUseitemAuto)
        {
            num = 240;
        }

        if (!isAPickYen && !isAPickYHM && !isAPickYHMS)
        {
            return;
        }

        for (int i = 0; i < GameScr.vItemMap.size(); i++)
        {
            ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
            if (itemMap == null)
            {
                continue;
            }

            int num2 = Math.abs(getMyChar().cx - itemMap.x);
            int num3 = Math.abs(getMyChar().cy - itemMap.y);
            if (num2 <= num && num3 <= num)
            {
                if ((isAPickYen || isAPickYHM || isAPickYHMS) && itemMap.template.type == 19)
                {
                    Service.gI().pickItem(itemMap.itemMapID);
                }
                else if ((itemMap.template.type == 16 || itemMap.template.type == 17) && (isAPickYHM || isAPickYHMS))
                {
                    Service.gI().pickItem(itemMap.itemMapID);
                }
                else if (itemMap.template.id == 27 && isAPickYHMS)
                {
                    Service.gI().pickItem(itemMap.itemMapID);
                }
            }
        }
    }

    public void itemMonToBag(Message msg)
    {
        try
        {
            readParam(msg, "itemMonToBag");
            getMyChar().eff5BuffHp = msg.reader().readShort();
            getMyChar().eff5BuffMp = msg.reader().readShort();
            int num = msg.reader().readUnsignedByte();
            Item item = arrItemMounts[num];
            item.typeUI = 3;
            arrItemMounts[num] = null;
            item.indexUI = msg.reader().readUnsignedByte();
            arrItemBag[item.indexUI] = item;
            if (num == 4)
            {
                isNewMount = (isWolf = (isMoto = (isMotoBehind = false)));
            }

            GameScr.isPaintItemInfo = false;
            GameScr.gI().setLCR();
        }
        catch (Exception)
        {
        }
    }

    public void itemBodyToBag(Message msg)
    {
        try
        {
            readParam(msg, "itemBodyToBag");
            getMyChar().eff5BuffHp = msg.reader().readShort();
            getMyChar().eff5BuffMp = msg.reader().readShort();
            Item item = arrItemBody[msg.reader().readUnsignedByte()];
            item.typeUI = 3;
            if (item.indexUI == 1)
            {
                setDefaultWeapon();
            }
            else if (item.indexUI == 2)
            {
                setDefaultBody();
            }
            else if (item.indexUI == 6)
            {
                setDefaultLeg();
            }

            arrItemBody[item.indexUI] = null;
            item.indexUI = msg.reader().readUnsignedByte();
            getMyChar().head = msg.reader().readShort();
            arrItemBag[item.indexUI] = item;
            GameScr.gI().left = (GameScr.gI().center = null);
            GameScr.gI().setLCR();
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
            Out.println("Char.itemBodyToBag()");
        }
    }

    public void itemBagToBox(Message msg)
    {
        try
        {
            int num = msg.reader().readUnsignedByte();
            int num2 = msg.reader().readUnsignedByte();
            Item item = arrItemBag[num];
            if (item != null)
            {
                if (item.template.type == 16)
                {
                    GameScr.hpPotion -= item.quantity;
                }

                if (item.template.type == 17)
                {
                    GameScr.mpPotion -= item.quantity;
                }

                arrItemBag[num] = null;
                if (arrItemBox[num2] == null)
                {
                    item.indexUI = num2;
                    item.typeUI = 4;
                    arrItemBox[num2] = item;
                }
                else
                {
                    arrItemBox[num2].quantity += item.quantity;
                }
            }

            GameScr.gI().left = (GameScr.gI().center = null);
            GameScr.gI().updateKeyBuyItemUI();
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
            Out.println("Char.itemBagToBox()");
        }
    }

    public void itemBoxToBag(Message msg)
    {
        try
        {
            int num = msg.reader().readUnsignedByte();
            int num2 = msg.reader().readUnsignedByte();
            Item item = arrItemBox[num];
            if (item != null)
            {
                if (item.template.type == 16)
                {
                    GameScr.hpPotion += item.quantity;
                }

                if (item.template.type == 17)
                {
                    GameScr.mpPotion += item.quantity;
                }

                arrItemBox[num] = null;
                if (arrItemBag[num2] == null)
                {
                    item.indexUI = num2;
                    item.typeUI = 3;
                    arrItemBag[num2] = item;
                }
                else
                {
                    arrItemBag[num2].quantity += item.quantity;
                }
            }

            GameScr.gI().left = (GameScr.gI().center = null);
            GameScr.gI().updateKeyBuyItemUI();
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
            Out.println("Char.itemBoxToBag()");
        }
    }

    public void crystalCollect(Message msg, bool isCoin)
    {
        try
        {
            for (int i = 0; i < GameScr.arrItemUpPeal.Length; i++)
            {
                GameScr.arrItemUpPeal[i] = null;
            }

            int num = msg.reader().readByte();
            Item item = new Item();
            item.typeUI = 3;
            item.indexUI = msg.reader().readByte();
            item.template = ItemTemplates.get(msg.reader().readShort());
            item.isLock = msg.reader().readBoolean();
            item.isExpires = msg.reader().readBoolean();
            item.quantity = 1;
            if (isCoin)
            {
                getMyChar().xu = msg.reader().readInt();
            }
            else
            {
                getMyChar().yen = msg.reader().readInt();
                try
                {
                    getMyChar().xu = msg.reader().readInt();
                }
                catch (Exception)
                {
                }
            }

            GameScr.arrItemUpPeal[0] = item;
            GameScr.effUpok = GameScr.efs[53];
            GameScr.indexEff = 0;
            GameScr.gI().left = (GameScr.gI().center = null);
            GameScr.gI().updateCommandForUI();
            GameCanvas.endDlg();
            if (num == 1)
            {
                InfoMe.addInfo(mResources.UPGRADE_SUCCESS + " " + item.template.name);
                return;
            }

            InfoMe.addInfo(mResources.UPGRADE + " " + ItemTemplates.get((short)(item.template.id + 1)).name + " " + mResources.UPGRADE_FAIL + " " + item.template.name, 25, mFont.tahoma_7_red);
        }
        catch (Exception ex2)
        {
            Out.println(ex2.Message + ex2.StackTrace);
            Out.println("Char.itemBagToBox()");
        }
    }
}
