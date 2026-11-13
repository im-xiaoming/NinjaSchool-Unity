using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    public void clearTask()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-98);
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

    public void requestClanInfo()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-113);
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

    public void requestIcon(int id)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-115);
            message.writer().writeInt(id);
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

    public void requestClanMember()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-112);
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

    public void requestChangeZone(int zoneId, int indexUI)
    {
        Message message = new Message((sbyte)28);
        try
        {
            message.writer().writeByte(zoneId);
            message.writer().writeByte(indexUI);
            session.sendMessage(message);
            message.cleanup();
        }
        catch (Exception)
        {
        }
    }

    public void requestModTemplate(int modTemplateId)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-108);
            message.writer().writeShort(modTemplateId);
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

    public void requestNpcTemplate(int npcTemplateId)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-107);
            message.writer().writeByte(npcTemplateId);
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

    public void openMenu(int npcId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)40);
            message.writer().writeShort(npcId);
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

    public void menu(sbyte typeclose, int npcId, int menuId, int optionId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)29);
            message.writer().writeByte(typeclose);
            message.writer().writeByte(npcId);
            message.writer().writeByte(menuId);
            message.writer().writeByte(optionId);
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

    public void menuId(short menuId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)34);
            message.writer().writeShort(menuId);
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

    public void getTask(int npcTemplateId, int menuId, int optionId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)47);
            message.writer().writeByte(npcTemplateId);
            message.writer().writeByte(menuId);
            if (optionId >= 0)
            {
                message.writer().writeByte(optionId);
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

    public void requestMaptemplate(int maptemplateId)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-109);
            message.writer().writeByte(maptemplateId);
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

    public void requestFriend()
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-85);
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

    public void requestMatchInfo(int zoneid)
    {
        Message message = null;
        try
        {
            Out.println("zone id: " + zoneid);
            message = new Message((sbyte)100);
            message.writer().writeByte(zoneid);
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

    public void requestPlayerInfo(MyVector chars)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)25);
            message.writer().writeByte(chars.size());
            for (int i = 0; i < chars.size(); i++)
            {
                Char @char = (Char)chars.elementAt(i);
                message.writer().writeInt(@char.charID);
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

    public void requestEnemies()
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-84);
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

    public void requestClanLog()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-114);
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

    public void requestChangePass(string username, string oldpass, string newpass)
    {
        try
        {
            Message message = messageNotLogin(120);
            message.writer().writeUTF(username);
            message.writer().writeUTF(oldpass);
            message.writer().writeUTF(newpass);
            session.sendMessage(message);
            message.cleanup();
        }
        catch (Exception)
        {
        }
    }

    public void requestRanked(sbyte index, string name)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)121);
            message.writer().writeByte(index);
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
}
