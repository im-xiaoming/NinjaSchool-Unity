using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    public static Service gI()
    {
        if (instance == null)
        {
            instance = new Service();
        }

        return instance;
    }

    public void clanPlease(int charId)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-61);
            message.writer().writeInt(charId);
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

    public void acceptInviteClan(int playerMapId)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-62);
            message.writer().writeInt(playerMapId);
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

    public void clanInvite(int charId)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-63);
            message.writer().writeInt(charId);
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

    public Message messageNotLogin(sbyte command)
    {
        Message message = new Message((sbyte)(-29));
        message.writer().writeByte(command);
        return message;
    }

    public void changeName(string name, int id)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-97);
            message.writer().writeInt(id);
            message.writer().writeUTF(name);
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

    public static Message messageSubCommand(sbyte command)
    {
        Message message = new Message((sbyte)(-30));
        message.writer().writeByte(command);
        return message;
    }

    public void setClientType()
    {
        try
        {
            Message message = messageNotLogin(-125);
            GameMidlet.CLIENT_TYPE = 4;
            if (mGraphics.zoomLevel == 1)
            {
                GameMidlet.CLIENT_TYPE = 0;
            }

            if (!Main.isPC)
            {
                if (Main.isWp)
                {
                    GameMidlet.CLIENT_TYPE = 6;
                }
                else if (Main.IphoneVersionApp)
                {
                    GameMidlet.CLIENT_TYPE = 5;
                }
                else
                {
                    GameMidlet.CLIENT_TYPE = 3;
                }
            }

            message.writer().writeByte(GameMidlet.CLIENT_TYPE);
            message.writer().writeByte(mGraphics.zoomLevel);
            message.writer().writeBoolean(GameCanvas.isGPRS);
            message.writer().writeInt(GameCanvas.w);
            message.writer().writeInt(GameCanvas.h);
            message.writer().writeBoolean(TField.isQwerty);
            message.writer().writeBoolean(GameCanvas.isTouch);
            message.writer().writeUTF("iphone platform xxx");
            if (!Main.isPC)
            {
                message.writer().writeInt(Main.versionIp);
            }
            else
            {
                message.writer().writeInt(0);
            }

            message.writer().writeByte(0);
            message.writer().writeByte(mResources.languageID);
            message.writer().writeInt(GameMidlet.userProvider);
            message.writer().writeUTF(GameMidlet.clientAgent);
            session.sendMessage(message);
            message.cleanup();
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void login(string username, string pass, string version)
    {
        gI().setClientType();
        try
        {
            Message message = messageNotLogin(-127);
            message.writer().writeUTF(username);
            message.writer().writeUTF(pass);
            message.writer().writeUTF(version);
            message.writer().writeUTF(string.Empty);
            message.writer().writeUTF(string.Empty);
            message.writer().writeUTF(RMS.loadRMSString("random") ?? string.Empty); // null on a fresh PlayerPrefs store
            message.writer().writeByte(GameMidlet.serverLogin);
            session.sendMessage(message);
            message.cleanup();
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void selectCharToPlay(string charname)
    {
        Message message = new Message((sbyte)(-28));
        try
        {
            message.writer().writeByte((sbyte)(-126));
            message.writer().writeUTF(charname);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }

        session.sendMessage(message);
    }

    public void createChar(string name, int gender, int hair)
    {
        Message message = new Message((sbyte)(-28));
        try
        {
            message.writer().writeByte((sbyte)(-125));
            message.writer().writeUTF(name);
            message.writer().writeByte(gender);
            message.writer().writeByte(hair);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }

        session.sendMessage(message);
    }

    public void upPotential(int index, int point)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-109);
            message.writer().writeByte(index);
            message.writer().writeShort(point);
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

    public void openFindParty()
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-77);
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

    // CHAT_MAP (-23): offline the text is a dev terminal command, run by the server (Server/Net/DevTerminal.cs).
    public void chat(string text)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)(-23));
            message.writer().writeUTF(text);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            if (message != null)
            {
                message.cleanup();
            }
        }
    }

    public void acceptInviteTrade(int playerMapId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)44);
            message.writer().writeInt(playerMapId);
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

    public void acceptInviteTestDun(int playerMapId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)99);
            message.writer().writeInt(playerMapId);
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

    public void acceptInviteTestGT(int playerMapId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)106);
            message.writer().writeInt(playerMapId);
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

    public void sendUIConfirmID(int Id)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)107);
            message.writer().writeByte(Id);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void acceptInviteTest(int playerMapId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)66);
            message.writer().writeInt(playerMapId);
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

    public void cancelInviteTrade()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)56);
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

    public void cancelTrade()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)57);
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

    public void tradeAccept()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)46);
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

    public void returnTownFromDead()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)(-9));
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

    public void wakeUpFromDead()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)(-10));
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

    public void clientOk()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-101);
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

    public void tradeInvite(int charId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)43);
            message.writer().writeInt(charId);
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

    public void addFriend(string name)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)59);
            message.writer().writeUTF(name);
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

    public void addPartyAccept(int charId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)80);
            message.writer().writeInt(charId);
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

    public void addPartyCancel(int charId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)81);
            message.writer().writeInt(charId);
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

    public void testInvite(int charId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)65);
            message.writer().writeInt(charId);
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

    public void addCuuSat(int charId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)68);
            message.writer().writeInt(charId);
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

    public void buffLive(int charId)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-79);
            message.writer().writeInt(charId);
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

    public void addParty(string name)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)79);
            message.writer().writeUTF(name);
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

    public void changePk(int typePk)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-93);
            message.writer().writeByte(typePk);
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

    public void changeTeamLeader(int index)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-87);
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

    public void createParty()
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-88);
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

    public void lockParty(bool isLock)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-76);
            message.writer().writeBoolean(isLock);
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

    public void outParty()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)83);
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

    public void pleaseInputParty(string str)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)23);
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

    public void acceptPleaseParty(string str)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)24);
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

    public void saveRms(string key, sbyte[] data, sbyte type)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-67);
            message.writer().writeUTF(key);
            message.writer().writeInt(data.Length);
            message.writer().write(data);
            message.writer().writeByte(type);
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

    public void loadRMS(string key)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-65);
            message.writer().writeUTF(key);
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

    public void acceptPleaseClan(int playerMapId)
    {
        Message message = null;
        try
        {
            message = messageSubCommand(-60);
            message.writer().writeInt(playerMapId);
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

    public void changeClanType(string name, int type)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-94);
            message.writer().writeUTF(name);
            message.writer().writeByte(type);
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

    public void changeClanAlert(string str)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-95);
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

    public void inputCoinClan(int coin)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-90);
            message.writer().writeInt(coin);
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

    public void outputCoinClan(int coin)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-89);
            message.writer().writeInt(coin);
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

    public void outClan()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-92);
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

    public void clanUpLevel()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-91);
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

    public void viewInfo(string cName)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)93);
            message.writer().writeUTF(cName);
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

    public void inviteClanDun(string name)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-87);
            message.writer().writeUTF(name);
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

    public void openLockAccProtect(int pass2)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-103);
            message.writer().writeInt(pass2);
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

    public void inviteClanBattlefield(string name)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-70);
            message.writer().writeUTF(name);
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

    public void acceptClanBattlefield()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-68);
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

    public void inviteClanBattlefieldAll()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-69);
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

    public void activeAccProtect(int pass)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-105);
            message.writer().writeInt(pass);
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

    public void clearAccProtect(int pass)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-102);
            message.writer().writeInt(pass);
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

    public void rewardPB()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-82);
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

    public void rewardCT()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-79);
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

    public void selectCard()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-72);
            message.writer().writeByte(GameScr.indexSelect);
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

    public void luyenthach(Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)110);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && (items[i].template.id == 10 || items[i].template.id == 11))
                {
                    message.writer().writeByte(items[i].indexUI);
                    break;
                }
            }

            for (int j = 0; j < items.Length; j++)
            {
                if (items[j] != null && (items[j].template.id == 455 || items[j].template.id == 456))
                {
                    message.writer().writeByte(items[j].indexUI);
                }
            }

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

    public void tinhluyen(Item it, Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)111);
            message.writer().writeByte(it.indexUI);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && (items[i].template.id == 455 || items[i].template.id == 456 || items[i].template.id == 457))
                {
                    message.writer().writeByte(items[i].indexUI);
                }
            }

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

    public void dichchuyen(Item it, Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)112);
            message.writer().writeByte(it.indexUI);
            int num = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].template.id == 454)
                {
                    message.writer().writeByte(items[i].indexUI);
                    num++;
                }
            }

            if (num < 20)
            {
                GameCanvas.startOKDlg(mResources.NOT_ENOUGH_DICHCHUYEN);
            }
            else
            {
                session.sendMessage(message);
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            message.cleanup();
        }
    }

    public void sendCatkeo(int id)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)113);
            message.writer().writeShort(id);
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

    public void ChucTet(string to, string loichuc, byte type)
    {
        Message message = null;
        string value = to + " " + loichuc;
        try
        {
            message = new Message((sbyte)(-21));
            message.writer().writeUTF(value);
            message.writer().writeByte(type);
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

    public void SendCapcha(short idRe, string capcha)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)118);
            message.writer().writeUTF(capcha);
            message.writer().writeShort(idRe);
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

    public void info_Kiemduyet(string name, string ngaysinh, string diachi, string cmnd, string ngaycap, string noicap, string email)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)123);
            message.writer().writeUTF(name);
            message.writer().writeUTF(ngaysinh);
            message.writer().writeUTF(diachi);
            message.writer().writeUTF(cmnd);
            message.writer().writeUTF(ngaycap);
            message.writer().writeUTF(noicap);
            message.writer().writeUTF(email);
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

    public void send_Captcha(sbyte type)
    {
        Message message = null;
        try
        {
            message = messageNotMap(122);
            message.writer().writeByte(4);
            message.writer().writeByte(type);
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

    public void ngockham(sbyte type, Item item, Item ngoc, Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)124);
            message.writer().writeByte(type);
            if (type == 0)
            {
                message.writer().writeByte(item.indexUI);
                message.writer().writeByte(ngoc.indexUI);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        message.writer().writeByte(items[i].indexUI);
                    }
                }
            }
            else if (type == 1)
            {
                message.writer().writeByte(ngoc.indexUI);
                for (int j = 0; j < items.Length; j++)
                {
                    if (items[j] != null)
                    {
                        message.writer().writeByte(items[j].indexUI);
                    }
                }
            }
            else if (type == 2 || type == 3)
            {
                message.writer().writeByte(ngoc.indexUI);
            }

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

    public void giaodo(Item[] items)
    {
        GameCanvas.msgdlg.pleasewait();
        Message message = null;
        try
        {
            message = new Message((sbyte)126);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    message.writer().writeByte(items[i].indexUI);
                }
            }

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

    public void sendGPS()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-59);
            message.writer().writeUTF(GameMidlet.longitude);
            message.writer().writeUTF(GameMidlet.latitude);
            session.sendMessage(message);
        }
        catch (Exception)
        {
        }
    }
}
