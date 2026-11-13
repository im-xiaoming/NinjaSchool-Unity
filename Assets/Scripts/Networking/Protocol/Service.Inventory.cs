using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    public void requestChangeMap()
    {
        Message message = new Message((sbyte)(-17));
        session.sendMessage(message);
        message.cleanup();
    }

    public void requestItemInfo(int typeUI, int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)42);
            message.writer().writeByte(typeUI);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void requestItemPlayer(int charId, int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)94);
            message.writer().writeInt(charId);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void itemBodyToBag(int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)15);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void itemMonToBag(int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)108);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception)
        {
        }
        finally
        {
            message.cleanup();
        }
    }

    public void itemBoxToBag(int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)16);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void itemBagToBox(int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)17);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void useItem(int indexUI)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)11);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }

        if (Char.getMyChar().arrItemBag[indexUI] != null && Char.getMyChar().arrItemBag[indexUI].template.type == 24)
        {
            GameScr.gI().resetButton();
            InfoDlg.showWait(string.Empty);
        }
    }

    public void saleItem(int indexUI, int quantity)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)14);
            message.writer().writeByte(indexUI);
            if (quantity > 1)
            {
                message.writer().writeShort(quantity);
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void saleItem2(int indexUI, int quantity)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)14);
            message.writer().writeByte(indexUI);
            if (quantity > 1)
            {
                message.writer().writeShort(quantity);
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void buyItem(int typeUI, int indexUI, int quantity)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)13);
            message.writer().writeByte(typeUI);
            message.writer().writeByte(indexUI);
            if (quantity > 1)
            {
                message.writer().writeShort(quantity);
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void useItemChangeMap(int indexUI, int indexMenu)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)12);
            message.writer().writeByte(indexUI);
            message.writer().writeByte(indexMenu);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void textBoxId(short menuId, string str)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)92);
            message.writer().writeShort(menuId);
            message.writer().writeUTF(str);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void requestItem(int typeUI)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-103);
            message.writer().writeByte(typeUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void bagSort()
    {
        Handle.bagshort = true;
        Message message = null;
        try
        {
            message = messageSubCommand(-107);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void boxSort()
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-106);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void boxCoinIn(int coinIn)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-105);
            message.writer().writeInt(coinIn);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void boxCoinOut(int coinOut)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-104);
            message.writer().writeInt(coinOut);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void crystalCollect(Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)19);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    message.writer().writeByte(items[i].indexUI);
                }
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void upgradeItem(Item item, Item[] items, bool isGold)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)21);
            message.writer().writeBoolean(isGold);
            message.writer().writeByte(item.indexUI);
            Debug.Log(item.template.name);
            Debug.Log(item.indexUI);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    message.writer().writeByte(items[i].indexUI);
                }
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void crystalCollectLock(Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)20);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    message.writer().writeByte(items[i].indexUI);
                }
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void splitItem(Item item)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)22);
            message.writer().writeByte(item.indexUI);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void tradeItemLock(int coin, Item[] items)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)45);
            message.writer().writeInt(coin);
            int num = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    num++;
                }
            }

            message.writer().writeByte(num);
            for (int j = 0; j < items.Length; j++)
            {
                if (items[j] != null)
                {
                    message.writer().writeByte(items[j].indexUI);
                }
            }

            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void pickItem(int itemMapId)
    {
        Message message = null;
        ItemMap itemMap = null;
        for (int i = 0; i < GameScr.vItemMap.size(); i++)
        {
            itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
        }

        try
        {
            message = new Message((sbyte)(-14));
            message.writer().writeShort(itemMapId);
            session.sendMessage(message);
        }
        catch (Exception)
        {
        }
        finally
        {
            message.cleanup();
        }
    }

    public void throwItem(int index)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)(-12));
            message.writer().writeByte(index);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void updateItem()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-119);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void requestClanItem()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-111);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void doConvertUpgrade(int index1, int index2, int index3)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-88);
            message.writer().writeByte(index1);
            message.writer().writeByte(index2);
            message.writer().writeByte(index3);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void unlockClanItem()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-62);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void inputNumSplit(int indexItem, int numSplit, bool box = false)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-85);
            message.writer().writeByte(indexItem);
            message.writer().writeInt(numSplit);
            message.writer().writeBoolean(box);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void sendToSaleItem(Item item, int value)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)102);
            message.writer().writeByte(item.indexUI);
            message.writer().writeInt(value);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void requestItemAuction(int itemId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)104);
            message.writer().writeInt(itemId);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void buyItemAuction(int itemId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)105);
            message.writer().writeInt(itemId);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void sendClanItem(string name)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-61);
            message.writer().writeByte(GameScr.indexSelect);
            message.writer().writeUTF(name);
            session.sendMessage(message);
        }
        catch (Exception)
        {
        }
        finally
        {
            message.cleanup();
        }
    }

    public void useClanItem()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-60);
            message.writer().writeByte(GameScr.indexSelect);
            session.sendMessage(message);
        }
        catch (Exception)
        {
        }
        finally
        {
            message.cleanup();
        }
    }
}
