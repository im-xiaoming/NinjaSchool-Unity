using Functions;
using Functions.Auto;
using System;
using UnityEngine;

public partial class Controller
{
    public void requestItemPlayer(Message msg)
    {
        try
        {
            int num = msg.reader().readUnsignedByte();
            Item item = GameScr.currentCharViewInfo.arrItemBody[num];
            item.expires = msg.reader().readLong();
            item.saleCoinLock = msg.reader().readInt();
            item.sys = msg.reader().readByte();
            item.options = new MyVector();
            try
            {
                while (true)
                {
                    item.options.addElement(new ItemOption(msg.reader().readUnsignedByte(), msg.reader().readInt()));
                }
            }
            catch (Exception ex)
            {
                Out.println(" >>>11  loi tai requestItemPlayer" + ex.ToString());
            }
        }
        catch (Exception ex2)
        {
            Out.println(">>>222 loi tai requestItemPlayer" + ex2.ToString());
        }
    }

    public void viewItemAuction(Message msg)
    {
        try
        {
            Item item = null;
            int num = msg.reader().readInt();
            for (int i = 0; i < GameScr.arrItemStands.Length; i++)
            {
                if (GameScr.arrItemStands[i].item.itemId == num)
                {
                    item = GameScr.arrItemStands[i].item;
                    break;
                }
            }

            item.typeUI = 37;
            item.expires = -1L;
            item.saleCoinLock = msg.reader().readInt();
            if (!item.isTypeBody() && !item.isTypeNgocKham())
            {
                return;
            }

            item.options = new MyVector();
            try
            {
                item.upgrade = msg.reader().readByte();
                item.sys = msg.reader().readByte();
                while (true)
                {
                    item.options.addElement(new ItemOption(msg.reader().readUnsignedByte(), msg.reader().readInt()));
                }
            }
            catch (Exception)
            {
            }
        }
        catch (Exception)
        {
        }
    }

    private void createItem(myReader d)
    {
        GameScr.vcItem = d.readByte();
        GameScr.iOptionTemplates = new ItemOptionTemplate[d.readUnsignedByte()];
        for (int i = 0; i < GameScr.iOptionTemplates.Length; i++)
        {
            GameScr.iOptionTemplates[i] = new ItemOptionTemplate();
            GameScr.iOptionTemplates[i].id = i;
            GameScr.iOptionTemplates[i].name = d.readUTF();
            GameScr.iOptionTemplates[i].type = d.readByte();
        }

        int num = d.readShort();
        for (int j = 0; j < num; j++)
        {
            ItemTemplate it = new ItemTemplate((short)j, d.readByte(), d.readByte(), d.readUTF(), d.readUTF(), d.readByte(), d.readShort(), d.readShort(), d.readBoolean());
            ItemTemplates.add(it);
        }
    }

    public void requestItemInfo(Message msg)
    {
        try
        {
            int num = msg.reader().readByte();
            int num2 = msg.reader().readUnsignedByte();
            Item item = null;
            switch (num)
            {
                case 3:
                {
                    item = Char.getMyChar().arrItemBag[num2];
                    if (item != null)
                    {
                        break;
                    }

                    if (GameScr.itemSplit != null && GameScr.itemSplit.indexUI == num2)
                    {
                        item = GameScr.itemSplit;
                    }

                    if (GameScr.itemUpGrade != null && GameScr.itemUpGrade.indexUI == num2)
                    {
                        item = GameScr.itemUpGrade;
                    }

                    if (GameScr.itemSell != null && GameScr.itemSell.indexUI == num2)
                    {
                        item = GameScr.itemSell;
                    }

                    if (item == null && GameScr.arrItemUpGrade != null)
                    {
                        for (int i = 0; i < GameScr.arrItemUpGrade.Length; i++)
                        {
                            if (GameScr.arrItemUpGrade[i] != null && GameScr.arrItemUpGrade[i].indexUI == num2)
                            {
                                item = GameScr.arrItemUpGrade[i];
                                break;
                            }
                        }
                    }

                    if (item == null && GameScr.arrItemConvert != null)
                    {
                        for (int j = 0; j < GameScr.arrItemConvert.Length; j++)
                        {
                            if (GameScr.arrItemConvert[j] != null && GameScr.arrItemConvert[j].indexUI == num2)
                            {
                                item = GameScr.arrItemConvert[j];
                                break;
                            }
                        }
                    }

                    if (item == null && GameScr.arrItemUpPeal != null)
                    {
                        for (int k = 0; k < GameScr.arrItemUpPeal.Length; k++)
                        {
                            if (GameScr.arrItemUpPeal[k] != null && GameScr.arrItemUpPeal[k].indexUI == num2)
                            {
                                item = GameScr.arrItemUpPeal[k];
                                break;
                            }
                        }
                    }

                    if (item == null && GameScr.arrItemTradeMe != null)
                    {
                        for (int l = 0; l < GameScr.arrItemTradeMe.Length; l++)
                        {
                            if (GameScr.arrItemTradeMe[l] != null && GameScr.arrItemTradeMe[l].indexUI == num2)
                            {
                                item = GameScr.arrItemTradeMe[l];
                                break;
                            }
                        }
                    }

                    if (item != null || GameScr.arrItemSplit == null)
                    {
                        break;
                    }

                    for (int m = 0; m < GameScr.arrItemSplit.Length; m++)
                    {
                        if (GameScr.arrItemSplit[m] != null && GameScr.arrItemSplit[m].indexUI == num2)
                        {
                            item = GameScr.arrItemSplit[m];
                            break;
                        }
                    }

                    break;
                }

                case 4:
                    item = Char.getMyChar().arrItemBox[num2];
                    break;
                case 39:
                    item = Char.clan.items[GameScr.indexSelect];
                    break;
                case 5:
                    item = Char.getMyChar().arrItemBody[num2];
                    break;
                case 20:
                    item = GameScr.arrItemNonNam[num2];
                    break;
                case 21:
                    item = GameScr.arrItemNonNu[num2];
                    break;
                case 22:
                    item = GameScr.arrItemAoNam[num2];
                    break;
                case 23:
                    item = GameScr.arrItemAoNu[num2];
                    break;
                case 24:
                    item = GameScr.arrItemGangTayNam[num2];
                    break;
                case 25:
                    item = GameScr.arrItemGangTayNu[num2];
                    break;
                case 26:
                    item = GameScr.arrItemQuanNam[num2];
                    break;
                case 27:
                    item = GameScr.arrItemQuanNu[num2];
                    break;
                case 28:
                    item = GameScr.arrItemGiayNam[num2];
                    break;
                case 29:
                    item = GameScr.arrItemGiayNu[num2];
                    break;
                case 16:
                    item = GameScr.arrItemLien[num2];
                    break;
                case 17:
                    item = GameScr.arrItemNhan[num2];
                    break;
                case 18:
                    item = GameScr.arrItemNgocBoi[num2];
                    break;
                case 19:
                    item = GameScr.arrItemPhu[num2];
                    break;
                case 2:
                    item = GameScr.arrItemWeapon[num2];
                    break;
                case 6:
                    item = GameScr.arrItemStack[num2];
                    break;
                case 7:
                    item = GameScr.arrItemStackLock[num2];
                    break;
                case 8:
                    item = GameScr.arrItemGrocery[num2];
                    break;
                case 9:
                    item = GameScr.arrItemGroceryLock[num2];
                    break;
                case 14:
                    item = GameScr.arrItemStore[num2];
                    Clock.HuyLockTypeUI(num);
                    break;
                case 35:
                    item = GameScr.arrItemElites[num2];
                    Clock.HuyLockTypeUI(num);
                    break;
                case 15:
                    item = GameScr.arrItemBook[num2];
                    Clock.HuyLockTypeUI(num);
                    break;
                case 32:
                    item = GameScr.arrItemFashion[num2];
                    Clock.HuyLockTypeUI(num);
                    break;
                case 34:
                    item = GameScr.arrItemClanShop[num2];
                    break;
                case 30:
                    item = GameScr.arrItemTradeOrder[num2];
                    break;
            }

            item.expires = msg.reader().readLong();
            if (item.isTypeUIMe())
            {
                item.saleCoinLock = msg.reader().readInt();
            }
            else if (item.isTypeUIShop() || item.isTypeUIShopLock() || item.isTypeUIStore() || item.isTypeUIBook() || item.isTypeUIFashion() || item.isTypeUIClanShop())
            {
                item.buyCoin = msg.reader().readInt();
                item.buyCoinLock = msg.reader().readInt();
                item.buyGold = msg.reader().readInt();
            }

            if (item.isTypeBody() || item.isTypeMounts() || item.isTypeNgocKham())
            {
                item.sys = msg.reader().readByte();
                item.options = new MyVector();
                try
                {
                    while (true)
                    {
                        item.options.addElement(new ItemOption(msg.reader().readUnsignedByte(), msg.reader().readInt()));
                    }
                }
                catch (Exception)
                {
                }
            }
            else if (item.template.id == 233)
            {
                item.img = createImage(NinjaUtil.readByteArray(msg));
            }
            else if (item.template.id == 234)
            {
                item.img = createImage(NinjaUtil.readByteArray(msg));
            }
            else if (item.template.id == 235)
            {
                item.img = createImage(NinjaUtil.readByteArray(msg));
            }

            item.s = true;
            if (num == 5)
            {
                Char.getMyChar().updateKickOption();
            }
        }
        catch (Exception)
        {
            Out.println("loi tai day ham requet item info ---------------------------------------------------------");
        }
    }
}
