using Functions;
using Functions.Auto;
using System;
using UnityEngine;

public partial class Controller
{
    private void createMap(myReader d)
    {
        GameScr.vcMap = d.readByte();
        TileMap.mapNames = new string[d.readUnsignedByte()];
        for (int i = 0; i < TileMap.mapNames.Length; i++)
        {
            TileMap.mapNames[i] = d.readUTF();
        }

        Npc.arrNpcTemplate = new NpcTemplate[d.readByte()];
        for (sbyte b = 0; b < Npc.arrNpcTemplate.Length; b = (sbyte)(b + 1))
        {
            Npc.arrNpcTemplate[b] = new NpcTemplate();
            Npc.arrNpcTemplate[b].npcTemplateId = b;
            Npc.arrNpcTemplate[b].name = d.readUTF();
            Npc.arrNpcTemplate[b].headId = d.readShort();
            Npc.arrNpcTemplate[b].bodyId = d.readShort();
            Npc.arrNpcTemplate[b].legId = d.readShort();
            Npc.arrNpcTemplate[b].menu = new string[d.readByte()][];
            for (int j = 0; j < Npc.arrNpcTemplate[b].menu.Length; j++)
            {
                Npc.arrNpcTemplate[b].menu[j] = new string[d.readByte()];
                for (int k = 0; k < Npc.arrNpcTemplate[b].menu[j].Length; k++)
                {
                    Npc.arrNpcTemplate[b].menu[j][k] = d.readUTF();
                }
            }
        }

        int num = d.readShort();
        Mob.arrMobTemplate = new MobTemplate[num];
        for (int l = 0; l < num; l++)
        {
            Mob.arrMobTemplate[l] = new MobTemplate();
            Mob.arrMobTemplate[l].mobTemplateId = (short)l;
            Mob.arrMobTemplate[l].type = d.readByte();
            Mob.arrMobTemplate[l].name = d.readUTF();
            Mob.arrMobTemplate[l].hp = d.readInt();
            Mob.arrMobTemplate[l].rangeMove = d.readByte();
            Mob.arrMobTemplate[l].speed = d.readByte();
        }
    }

    public void loadInfoMap(Message msg)
    {
        try
        {
            Char.getMyChar().cx = (Char.getMyChar().cxSend = (Char.getMyChar().cxFocus = msg.reader().readShort()));
            Char.getMyChar().cy = (Char.getMyChar().cySend = (Char.getMyChar().cyFocus = msg.reader().readShort()));
            int num = msg.reader().readByte();
            for (int i = 0; i < num; i++)
            {
                TileMap.vGo.addElement(new Waypoint(msg.reader().readShort(), msg.reader().readShort(), msg.reader().readShort(), msg.reader().readShort()));
            }

            num = msg.reader().readByte();
            for (sbyte b = 0; b < num; b = (sbyte)(b + 1))
            {
                Mob mob = new Mob(b, msg.reader().readBoolean(), msg.reader().readBoolean(), msg.reader().readBoolean(), msg.reader().readBoolean(), msg.reader().readBoolean(), msg.reader().readShort(), msg.reader().readByte(), msg.reader().readInt(), msg.reader().readUnsignedByte(), msg.reader().readInt(), msg.reader().readShort(), msg.reader().readShort(), msg.reader().readByte(), msg.reader().readByte(), msg.reader().readBoolean(), false);
                if (Mob.arrMobTemplate[mob.templateId].type != 0)
                {
                    if (b % 3 == 0)
                    {
                        mob.dir = -1;
                    }
                    else
                    {
                        mob.dir = 1;
                    }

                    mob.x += 10 - b % 20;
                }

                GameScr.vMob.addElement(mob);
            }

            num = msg.reader().readByte();
            for (byte b2 = 0; b2 < num; b2 = (byte)(b2 + 1))
            {
                GameScr.vBuNhin.addElement(new BuNhin(msg.reader().readUTF(), msg.reader().readShort(), msg.reader().readShort()));
            }

            num = msg.reader().readByte();
            for (int j = 0; j < num; j++)
            {
                GameScr.vNpc.addElement(new Npc(j, msg.reader().readByte(), msg.reader().readShort(), msg.reader().readShort(), msg.reader().readByte()));
            }

            num = msg.reader().readByte();
            for (int k = 0; k < num; k++)
            {
                ItemMap itemMap = new ItemMap(msg.reader().readShort(), msg.reader().readShort(), msg.reader().readShort(), msg.reader().readShort());
                bool flag = false;
                for (int l = 0; l < GameScr.vItemMap.size(); l++)
                {
                    ItemMap itemMap2 = (ItemMap)GameScr.vItemMap.elementAt(l);
                    if (itemMap2.itemMapID == itemMap.itemMapID)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    GameScr.vItemMap.addElement(itemMap);
                }
            }

            GameScr.loadCamera(false);
            try
            {
                TileMap.mapName1 = null;
                TileMap.mapName1 = msg.reader().readUTF();
                TileMap.mapName = TileMap.mapName1;
            }
            catch (Exception)
            {
            }

            try
            {
                TileMap.locationStand.clear();
                int num2 = msg.reader().readUnsignedByte();
                for (int m = 0; m < num2; m++)
                {
                    int num3 = msg.reader().readUnsignedByte();
                    int num4 = msg.reader().readUnsignedByte();
                    string k2 = (short)(num4 * TileMap.tmw + num3) + string.Empty;
                    TileMap.locationStand.put(k2, "location");
                }
            }
            catch (Exception ex2)
            {
                ex2.ToString();
            }

            TileMap.loadMap(TileMap.tileID);
            Char.getMyChar().cvx = 0;
            Char.getMyChar().statusMe = 4;
            GameScr.gI().loadGameScr();
            GameCanvas.loadBG(TileMap.bgID);
            Char.isLockKey = false;
            Char.ischangingMap = false;
            GameCanvas.clearKeyHold();
            GameCanvas.clearKeyPressed();
            GameScr.gI().switchToMe();
            InfoDlg.hide();
            InfoDlg.show(TileMap.mapName, mResources.ZONE + " " + TileMap.zoneID, 30);
            Party.refreshAll();
            GameCanvas.endDlg();
            GameCanvas.isLoading = false;
        }
        catch (Exception ex3)
        {
            Debug.Log("EROR: " + ex3.Message);
        }

        TileMap.HuyLockMap();
        if (!Handle.FirstZone)
        {
            if (map == TileMap.mapID)
            {
                Handle.timeChangeZone = mSystem.currentTimeMillis();
            }
            else
            {
                Handle.timeChangeZone = mSystem.currentTimeMillis() - 5000;
            }
        }

        Handle.FirstZone = false;
        map = TileMap.mapID;
    }

    public void messageNotMap(Message msg)
    {
        GameCanvas.debug("SA6", 2);
        try
        {
            switch (msg.reader().readByte())
            {
                case -59:
                    GameMidlet.instance.CheckPerGPS();
                    break;
                case -98:
                    Char.getMyChar().clearTask();
                    break;
                case -97:
                {
                    GameCanvas.isLoading = false;
                    GameCanvas.endDlg();
                    int num5 = msg.reader().readInt();
                    GameCanvas.inputDlg.show(mResources.NAME_CHANGE, new Command("OK", GameCanvas.instance, 88829, num5), TField.INPUT_TYPE_ANY);
                    break;
                }

                case -115:
                {
                    int id = msg.reader().readInt();
                    sbyte[] data = NinjaUtil.readByteArray(msg);
                    SmallImage.reciveImage(id, data);
                    break;
                }

                case -117:
                    Char.getMyChar().cPk = msg.reader().readByte();
                    Info.addInfo(mResources.PK_NOW + " " + Char.getMyChar().cPk, 15, mFont.tahoma_7_yellow);
                    Char.getMyChar().callEffTask(21);
                    break;
                case -96:
                    Char.getMyChar().cClanName = msg.reader().readUTF();
                    Char.getMyChar().ctypeClan = 4;
                    Char.getMyChar().luong = msg.reader().readInt();
                    Char.getMyChar().callEffTask(21);
                    break;
                case -113:
                    Out.println("vao REQUEST_CLAN_INFO roi ");
                    if (Char.clan == null)
                    {
                        Char.clan = new Clan();
                    }

                    Char.clan.name = msg.reader().readUTF();
                    Char.clan.main_name = msg.reader().readUTF();
                    Char.clan.assist_name = msg.reader().readUTF();
                    Char.clan.total = msg.reader().readShort();
                    Char.clan.openDun = msg.reader().readByte();
                    Char.clan.level = msg.reader().readByte();
                    Char.clan.exp = msg.reader().readInt();
                    Char.clan.expNext = msg.reader().readInt();
                    Char.clan.coin = msg.reader().readInt();
                    Char.clan.freeCoin = msg.reader().readInt();
                    Char.clan.coinUp = msg.reader().readInt();
                    Char.clan.reg_date = msg.reader().readUTF();
                    Char.clan.alert = msg.reader().readUTF();
                    Char.clan.use_card = msg.reader().readInt();
                    Char.clan.itemLevel = msg.reader().readByte();
                    break;
                case -93:
                {
                    int num = msg.reader().readInt();
                    if (num == Char.getMyChar().charID)
                    {
                        GameScr.vClan.removeAllElements();
                        Char.getMyChar().cClanName = string.Empty;
                        Char.getMyChar().ctypeClan = -1;
                        Char.clan = null;
                    }
                    else
                    {
                        GameScr.vClan.removeAllElements();
                        Char char3 = GameScr.findCharInMap(num);
                        char3.cClanName = string.Empty;
                        char3.ctypeClan = -1;
                    }

                    break;
                }

                case -114:
                    if (Char.clan == null)
                    {
                        Char.clan = new Clan();
                    }

                    Char.clan.writeLog(msg.reader().readUTF());
                    break;
                case -62:
                    Char.clan.itemLevel = msg.reader().readByte();
                    break;
                case -81:
                    Char.pointChienTruong = msg.reader().readShort();
                    break;
                case -77:
                    if (TileMap.mapID != 26)
                        TileMap.bgID = msg.reader().readByte();
                    GameCanvas.loadBG(TileMap.bgID);
                    break;
                case -70:
                {
                    string replacement = msg.reader().readUTF();
                    GameCanvas.startYesNoDlg(NinjaUtil.replace(mResources.INVITE_TO_CBT, "#", replacement), new Command(mResources.YES, GameCanvas.instance, 88842, null), new Command(mResources.NO, GameCanvas.instance, 8882, null));
                    break;
                }

                case -72:
                {
                    GameScr.gI().yenValue = new string[9];
                    GameScr.arrItemSprin = new short[9];
                    if (GameScr.indexSelect < 0 || GameScr.indexSelect > 8)
                    {
                        GameScr.indexSelect = (GameScr.indexCard = 0);
                    }

                    if (GameScr.indexSelect < 0 || GameScr.indexSelect > 8)
                    {
                        GameScr.indexSelect = (GameScr.indexCard = 0);
                    }

                    for (int k = 0; k < 9; k++)
                    {
                        GameScr.arrItemSprin[k] = msg.reader().readShort();
                        GameScr.gI().yenValue[k] = GameScr.gI().YenCards[NinjaUtil.randomNumber(6)];
                    }

                    GameScr.gI().left = new Command(mResources.CONTINUE, null, 1506, null);
                    GameScr.gI().timePoint = mSystem.getCurrentTimeMillis();
                    GameScr.gI().numSprinLeft--;
                    GameCanvas.endDlg();
                    break;
                }

                case -88:
                {
                    GameScr.gI().resetButton();
                    Item item = Char.getMyChar().arrItemBag[msg.reader().readByte()];
                    item.clearExpire();
                    item.isLock = true;
                    item.upgrade = msg.reader().readByte();
                    Item item2 = Char.getMyChar().arrItemBag[msg.reader().readByte()];
                    item2.clearExpire();
                    item2.isLock = true;
                    item2.upgrade = msg.reader().readByte();
                    Info.addInfo(mResources.CONVERT_OK, 20, mFont.tahoma_7b_yellow);
                    break;
                }

                case -112:
                {
                    GameScr.vClan.removeAllElements();
                    int num2 = msg.reader().readShort();
                    for (int i = 0; i < num2; i++)
                    {
                        GameScr.vClan.addElement(new Member(msg.reader().readByte(), msg.reader().readByte(), msg.reader().readByte(), msg.reader().readUTF(), msg.reader().readInt(), msg.reader().readBoolean()));
                    }

                    try
                    {
                        for (int j = 0; j < num2; j++)
                        {
                            ((Member)GameScr.vClan.elementAt(j)).pointClanWeek = msg.reader().readInt();
                        }
                    }
                    catch (Exception)
                    {
                    }

                    GameScr.gI().sortClan();
                    break;
                }

                case -111:
                {
                    Char.clan.items = new Item[30];
                    int num3 = msg.reader().readByte();
                    for (int num15 = 0; num15 < num3; num15++)
                    {
                        Char.clan.items[num15] = new Item();
                        Char.clan.items[num15].typeUI = 39;
                        Char.clan.items[num15].indexUI = num15;
                        Char.clan.items[num15].quantity = msg.reader().readShort();
                        Char.clan.items[num15].template = ItemTemplates.get(msg.reader().readShort());
                    }

                    GameScr.gI().clearVecThanThu();
                    sbyte b3 = msg.reader().readByte();
                    for (int num16 = 0; num16 < b3; num16++)
                    {
                        string name = msg.reader().readUTF();
                        short idIconItem = msg.reader().readShort();
                        short idThanThu = msg.reader().readShort();
                        int num17 = msg.reader().readInt();
                        string str_trungno = string.Empty;
                        MyVector myVector = new MyVector();
                        int curExp = -1;
                        int maxExp = -1;
                        sbyte b4 = msg.reader().readByte();
                        if (num17 >= 0)
                        {
                            str_trungno = msg.reader().readUTF();
                        }
                        else
                        {
                            for (int num18 = 0; num18 < b4; num18++)
                            {
                                string o = msg.reader().readUTF();
                                myVector.addElement(o);
                            }

                            curExp = msg.reader().readInt();
                            maxExp = msg.reader().readInt();
                        }

                        sbyte stars = msg.reader().readByte();
                        GameScr.gI().addInfo_ThanThu(new Clan_ThanThu(name, stars, idIconItem, idThanThu, num17, str_trungno, myVector, curExp, maxExp));
                    }

                    break;
                }

                case -116:
                    Char.getMyChar().xu = msg.reader().readInt();
                    Char.clan.coin = msg.reader().readInt();
                    break;
                case -90:
                    Char.getMyChar().xu = msg.reader().readInt();
                    GameScr.gI().resetButton();
                    break;
                case -86:
                    GameCanvas.endDlg();
                    GameScr.gI().resetButton();
                    InfoMe.addInfo(msg.reader().readUTF(), 20, mFont.tahoma_7_yellow);
                    break;
                case -106:
                    GameScr.typeActive = msg.reader().readByte();
                    Out.println("load Me Active: " + GameScr.typeActive);
                    break;
                case -84:
                    Char.pointPB = msg.reader().readShort();
                    break;
                case -80:
                    GameScr.gI().showAlert(mResources.RESULT, msg.reader().readUTF(), false);
                    if (msg.reader().readBoolean())
                    {
                        GameScr.gI().left = new Command(mResources.REWARD, 2000);
                    }

                    break;
                case -83:
                {
                    int num11 = msg.reader().readShort();
                    int num12 = msg.reader().readShort();
                    int num13 = msg.reader().readByte();
                    int num14 = msg.reader().readShort();
                    if (num11 == 0)
                    {
                        GameScr.gI().showAlert(mResources.REVIEW, "          " + mResources.EMPTY_INFO, false);
                        break;
                    }

                    string text = mResources.PROPERTY + ": " + num11 + "\n\n";
                    string text2;
                    if (num12 == 0)
                    {
                        text = text + mResources.NOT_FINISH + "\n\n";
                    }
                    else
                    {
                        text2 = text;
                        text = text2 + mResources.TIME_FINISH + ": " + NinjaUtil.getTime(num12) + "\n\n";
                    }

                    text2 = text;
                    text = text2 + mResources.TEAMWORK + ": " + num13 + "\n\n";
                    text2 = text;
                    text = text2 + mResources.REWARD + ": " + num14 + " " + mResources.LUCKY_GIFT + "\n\n";
                    GameScr.gI().showAlert(mResources.REVIEW, text, false);
                    if (num14 > 0)
                    {
                        GameScr.gI().left = new Command(mResources.REWARD, 1000);
                    }

                    break;
                }

                case -95:
                    if (Char.clan != null)
                    {
                        Char.clan.alert = msg.reader().readUTF();
                    }

                    break;
                case -126:
                {
                    GameCanvas.debug("SA7", 2);
                    int num3 = msg.reader().readByte();
                    LocalLogin.IsLoggingIn = false;
                    SelectCharScr.gI().initSelectChar();
                    for (sbyte b = 0; b < num3; b = (sbyte)(b + 1))
                    {
                        SelectCharScr.gI().gender[b] = msg.reader().readByte();
                        SelectCharScr.gI().name[b] = msg.reader().readUTF();
                        SelectCharScr.gI().phai[b] = msg.reader().readUTF();
                        SelectCharScr.gI().level[b] = msg.reader().readUnsignedByte();
                        SelectCharScr.gI().parthead[b] = msg.reader().readShort();
                        SelectCharScr.gI().partWp[b] = msg.reader().readShort();
                        SelectCharScr.gI().partbody[b] = msg.reader().readShort();
                        SelectCharScr.gI().partleg[b] = msg.reader().readShort();
                        if (SelectCharScr.gI().partWp[b] == -1)
                        {
                            SelectCharScr.gI().partWp[b] = 15;
                        }

                        if (SelectCharScr.gI().partbody[b] == -1)
                        {
                            if (SelectCharScr.gI().gender[b] == 0)
                            {
                                SelectCharScr.gI().partbody[b] = 10;
                            }
                            else
                            {
                                SelectCharScr.gI().partbody[b] = 1;
                            }
                        }

                        if (SelectCharScr.gI().partleg[b] == -1)
                        {
                            if (SelectCharScr.gI().gender[b] == 0)
                            {
                                SelectCharScr.gI().partleg[b] = 9;
                            }
                            else
                            {
                                SelectCharScr.gI().partleg[b] = 0;
                            }
                        }
                    }

                    if (LocalLogin.NextStartMode == LocalLogin.StartMode.NewGame && num3 < 3)
                    {
                        LocalLogin.NextStartMode = LocalLogin.StartMode.Continue;
                        CreateCharScr.gI().switchToMe();
                    }
                    else
                    {
                        LocalLogin.NextStartMode = LocalLogin.StartMode.Continue;
                        SelectCharScr.gI().switchToMe();
                    }

                    GameCanvas.endDlg();
                    break;
                }

                case -123:
                    GameCanvas.debug("SA8", 2);
                    GameScr.vsData = msg.reader().readByte();
                    GameScr.vsMap = msg.reader().readByte();
                    GameScr.vsSkill = msg.reader().readByte();
                    GameScr.vsItem = msg.reader().readByte();
                    if (GameScr.vsData != GameScr.vcData)
                    {
                        Service.gI().updateData();
                    }
                    else
                    {
                        try
                        {
                            DataInputStream dataInputStream = new DataInputStream(RMS.loadRMS("data"));
                            createData(dataInputStream.r);
                        }
                        catch (Exception)
                        {
                            GameScr.vcData = -1;
                            Service.gI().updateData();
                        }
                    }

                    if (GameScr.vsMap != GameScr.vcMap)
                    {
                        Service.gI().updateMap();
                    }
                    else
                    {
                        try
                        {
                            DataInputStream dataInputStream2 = new DataInputStream(RMS.loadRMS("map"));
                            createMap(dataInputStream2.r);
                        }
                        catch (Exception)
                        {
                            GameScr.vcMap = -1;
                            Service.gI().updateMap();
                        }
                    }

                    if (GameScr.vsSkill != GameScr.vcSkill)
                    {
                        Service.gI().updateSkill();
                    }
                    else
                    {
                        try
                        {
                            DataInputStream dataInputStream3 = new DataInputStream(RMS.loadRMS("skill"));
                            createSkill(dataInputStream3.r);
                        }
                        catch (Exception)
                        {
                            GameScr.vcSkill = -1;
                            Service.gI().updateSkill();
                        }
                    }

                    if (GameScr.vsItem != GameScr.vcItem)
                    {
                        Service.gI().updateItem();
                    }
                    else
                    {
                        try
                        {
                            DataInputStream dataInputStream4 = new DataInputStream(RMS.loadRMS("item"));
                            createItem(dataInputStream4.r);
                        }
                        catch (Exception)
                        {
                            GameScr.vcItem = -1;
                            Service.gI().updateItem();
                        }
                    }

                    if (GameScr.vsData == GameScr.vcData && GameScr.vsMap == GameScr.vcMap && GameScr.vsSkill == GameScr.vcSkill && GameScr.vsItem == GameScr.vcItem)
                    {
                        GameScr.gI().readEfect();
                        GameScr.gI().readArrow();
                        GameScr.gI().readSkill();
                        Service.gI().clientOk();
                    }

                    CharPartInfo.doSetInfo(msg);
                    break;
                case -122:
                {
                    Out.println("GET UPDATE_DATA " + msg.reader().available() + " sbytes");
                    msg.reader().mark(100000);
                    createData(msg.reader());
                    msg.reader().reset();
                    sbyte[] data4 = new sbyte[msg.reader().available()];
                    msg.reader().readFully(ref data4);
                    RMS.saveRMS("data", data4);
                    sbyte[] data5 = new sbyte[1]
                    {
                        GameScr.vcData
                    };
                    RMS.saveRMS("dataVersion", data5);
                    if (GameScr.vsData == GameScr.vcData && GameScr.vsMap == GameScr.vcMap && GameScr.vsSkill == GameScr.vcSkill && GameScr.vsItem == GameScr.vcItem)
                    {
                        GameScr.gI().readEfect();
                        GameScr.gI().readArrow();
                        GameScr.gI().readSkill();
                        Service.gI().clientOk();
                    }

                    break;
                }

                case -121:
                {
                    Out.println("GET UPDATE_MAP " + msg.reader().available() + " sbytes");
                    msg.reader().mark(100000);
                    createMap(msg.reader());
                    msg.reader().reset();
                    sbyte[] data2 = new sbyte[msg.reader().available()];
                    msg.reader().readFully(ref data2);
                    RMS.saveRMS("map", data2);
                    sbyte[] data3 = new sbyte[1]
                    {
                        GameScr.vcMap
                    };
                    RMS.saveRMS("mapVersion", data3);
                    if (GameScr.vsData == GameScr.vcData && GameScr.vsMap == GameScr.vcMap && GameScr.vsSkill == GameScr.vcSkill && GameScr.vsItem == GameScr.vcItem)
                    {
                        GameScr.gI().readEfect();
                        GameScr.gI().readArrow();
                        GameScr.gI().readSkill();
                        Service.gI().clientOk();
                    }

                    break;
                }

                case -120:
                {
                    Out.println("GET UPDATE_SKILL " + msg.reader().available() + " sbytes");
                    msg.reader().mark(100000);
                    createSkill(msg.reader());
                    msg.reader().reset();
                    sbyte[] data8 = new sbyte[msg.reader().available()];
                    msg.reader().readFully(ref data8);
                    if (Char.getMyChar().isHumanz())
                    {
                        RMS.saveRMS("skill", data8);
                    }
                    else
                    {
                        RMS.saveRMS("skillnhanban", data8);
                    }

                    sbyte[] data9 = new sbyte[1]
                    {
                        GameScr.vcSkill
                    };
                    RMS.saveRMS("skillVersion", data9);
                    if (GameScr.vsData == GameScr.vcData && GameScr.vsMap == GameScr.vcMap && GameScr.vsSkill == GameScr.vcSkill && GameScr.vsItem == GameScr.vcItem)
                    {
                        GameScr.gI().readEfect();
                        GameScr.gI().readArrow();
                        GameScr.gI().readSkill();
                        Service.gI().clientOk();
                    }

                    break;
                }

                case -119:
                {
                    Out.println("GET UPDATE_ITEM " + msg.reader().available() + " sbytes");
                    msg.reader().mark(100000);
                    createItem(msg.reader());
                    msg.reader().reset();
                    sbyte[] data6 = new sbyte[msg.reader().available()];
                    msg.reader().readFully(ref data6);
                    RMS.saveRMS("item", data6);
                    sbyte[] data7 = new sbyte[1]
                    {
                        GameScr.vcItem
                    };
                    RMS.saveRMS("itemVersion", data7);
                    if (GameScr.vsData == GameScr.vcData && GameScr.vsMap == GameScr.vcMap && GameScr.vsSkill == GameScr.vcSkill && GameScr.vsItem == GameScr.vcItem)
                    {
                        GameScr.gI().readEfect();
                        GameScr.gI().readArrow();
                        GameScr.gI().readSkill();
                        Service.gI().clientOk();
                    }

                    break;
                }

                case -108:
                {
                    int num6 = msg.reader().readShort();
                    try
                    {
                        sbyte typeFly = msg.reader().readByte();
                        Mob.arrMobTemplate[num6].typeFly = typeFly;
                    }
                    catch (Exception)
                    {
                    }

                    sbyte b2 = msg.reader().readByte();
                    Mob.arrMobTemplate[num6].imgs = new Image[b2];
                    if (num6 == 98 || num6 == 99)
                    {
                        Mob.arrMobTemplate[num6].imgs = new Image[3];
                        Image image = createImage(NinjaUtil.readByteArray(msg));
                        for (int m = 0; m < Mob.arrMobTemplate[num6].imgs.Length; m++)
                        {
                            Mob.arrMobTemplate[num6].imgs[m] = image;
                        }
                    }
                    else
                    {
                        for (int n = 0; n < Mob.arrMobTemplate[num6].imgs.Length; n++)
                        {
                            sbyte[] arr = NinjaUtil.readByteArray(msg);
                            Mob.arrMobTemplate[num6].imgs[n] = createImage(arr);
                        }
                    }

                    if (msg.reader().readBoolean())
                    {
                        int num3 = msg.reader().readByte();
                        Mob.arrMobTemplate[num6].frameBossMove = new sbyte[num3];
                        for (int num7 = 0; num7 < num3; num7++)
                        {
                            Mob.arrMobTemplate[num6].frameBossMove[num7] = msg.reader().readByte();
                        }

                        num3 = msg.reader().readByte();
                        Mob.arrMobTemplate[num6].frameBossAttack = new sbyte[num3][];
                        for (int num8 = 0; num8 < num3; num8++)
                        {
                            Mob.arrMobTemplate[num6].frameBossAttack[num8] = new sbyte[msg.reader().readByte()];
                            for (int num9 = 0; num9 < Mob.arrMobTemplate[num6].frameBossAttack[num8].Length; num9++)
                            {
                                Mob.arrMobTemplate[num6].frameBossAttack[num8][num9] = msg.reader().readByte();
                            }
                        }
                    }

                    int num10 = msg.reader().readInt();
                    if (num10 > 0)
                    {
                        if (num6 < 236)
                        {
                            readDataMobOld(msg, num6);
                        }
                        else
                        {
                            readDataMobNew(msg, num6);
                        }
                    }

                    break;
                }

                case -109:
                    try
                    {
                        GameCanvas.isLoading = true;
                        TileMap.maps = null;
                        TileMap.types = null;
                        GameCanvas.debug("SA99", 2);
                        TileMap.tmw = msg.reader().readByte();
                        TileMap.tmh = msg.reader().readByte();
                        TileMap.maps = new char[TileMap.tmw * TileMap.tmh];
                        for (int l = 0; l < TileMap.maps.Length; l++)
                        {
                            int num4 = msg.reader().readByte();
                            if (num4 < 0)
                            {
                                num4 += 256;
                            }

                            TileMap.maps[l] = (char)num4;
                        }

                        TileMap.types = new int[TileMap.maps.Length];
                        msg = messWait;
                        loadInfoMap(msg);
                    }
                    catch (Exception)
                    {
                        Out.println(" loi tai cmd  " + msg.command);
                    }

                    msg.cleanup();
                    messWait.cleanup();
                    msg = (messWait = null);
                    break;
                case -107:
                    GameCanvas.debug("SA10", 2);
                    break;
                case -110:
                    GameCanvas.debug("SA11", 2);
                    break;
                case -67:
                {
                    Mob mob = null;
                    try
                    {
                        int iD = msg.reader().readUnsignedByte();
                        mob = Mob.get_Mob(iD);
                    }
                    catch (Exception)
                    {
                    }

                    if (mob == null)
                    {
                        break;
                    }

                    int num = msg.reader().readInt();
                    if (num == Char.getMyChar().charID)
                    {
                        GameScr.vMobSoul.addElement(new MobSoul(mob.x, mob.y, Char.getMyChar()));
                        break;
                    }

                    Char char2 = GameScr.findCharInMap(num);
                    if (char2 != null)
                    {
                        GameScr.vMobSoul.addElement(new MobSoul(mob.x, mob.y, char2));
                    }

                    break;
                }

                case -66:
                {
                    int num = msg.reader().readInt();
                    if (Char.getMyChar().charID == num)
                    {
                        GameScr.vMobSoul.addElement(new MobSoul(Char.getMyChar().cx, Char.getMyChar().cy));
                        break;
                    }

                    Char @char = GameScr.findCharInMap(num);
                    if (@char != null)
                    {
                        GameScr.vMobSoul.addElement(new MobSoul(@char.cx, @char.cy));
                    }

                    break;
                }

                case -85: // Offline inputNumSplit acknowledgement, bag or box; no potion counter changes.
                {
                    bool box = msg.reader().readBoolean();
                    int from = msg.reader().readUnsignedByte(), to = msg.reader().readUnsignedByte();
                    int template = msg.reader().readShort();
                    int left = msg.reader().readUnsignedShort(), taken = msg.reader().readUnsignedShort();
                    Item[] slots = box ? Char.getMyChar().arrItemBox : Char.getMyChar().arrItemBag;
                    if (slots == null || from >= slots.Length || to >= slots.Length || slots[from] == null || slots[from].template.id != template || slots[to] != null)
                        break;
                    Item split = slots[from].clone();
                    split.quantity = taken;
                    split.indexUI = to;
                    slots[from].quantity = left;
                    slots[to] = split;
                    GameScr.isPaintItemInfo = false;
                    GameScr.gI().setLCR();
                    break;
                }

                case -125:
                case -124:
                case -118:
                case -105:
                case -104:
                case -103:
                case -102:
                case -101:
                case -100:
                case -94:
                case -92:
                case -91:
                case -89:
                case -87:
                case -82:
                case -79:
                case -78:
                case -76:
                case -75:
                case -74:
                case -73:
                case -71:
                case -69:
                case -68:
                case -65:
                case -64:
                case -63:
                case -61:
                case -60:
                    break;
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            msg?.cleanup();
        }
    }
}
