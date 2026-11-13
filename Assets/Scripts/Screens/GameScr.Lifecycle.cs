using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    public static void load_GameScr()
    {
        GameCanvas.backitem = GameCanvas.loadImage("/Big4");
        GameCanvas.imgPlus12 = GameCanvas.loadImage("/plus12");
        imgEggMonters = GameCanvas.loadImage("/trung1");
        imgSelect = GameCanvas.loadImage("/u/select");
        imgMatcho = GameCanvas.loadImage("/hd/mat");
        imgFiremoto = GameCanvas.loadImage("/hd/lua");
        imgEffMob = GameCanvas.loadImage("/eff/g132");
        imgEffMob1 = GameCanvas.loadImage("/eff/g10");
        imgEffMob2 = GameCanvas.loadImage("/eff/g6");
        imgEffMob3 = GameCanvas.loadImage("/eff/g99");
        imgsmokeFollow = GameCanvas.loadImage("/eff/g9");
        imgTf = GameCanvas.loadImage("/hd/tf");
        imgMenu1 = GameCanvas.loadImage("/hd/btnlBig0");
        imgMenu2 = GameCanvas.loadImage("/hd/btnlBig1");
        imgButton = GameCanvas.loadImage("/hd/button");
        imgButton2 = GameCanvas.loadImage("/hd/button2");
        imgHpp = GameCanvas.loadImage("/hd/hpp");
        imgMpp = GameCanvas.loadImage("/hd/mpp");
        imgRight = GameCanvas.loadImage("/hd/right");
        imgRight2 = GameCanvas.loadImage("/hd/right2");
        imgSkill = GameCanvas.loadImage("/hd/skill");
        imgLbtn = GameCanvas.loadImage("/hd/btnl");
        mScreen.cmdW = imgLbtn.getWidth();
        imgLbtnFocus = GameCanvas.loadImage("/hd/btnlf");
        mScreen.cmdW = imgLbtn.getWidth();
        imgArrow = GameCanvas.loadImage("/hd/arrow");
        imgArrow2 = GameCanvas.loadImage("/hd/arrow2");
        imgChat = GameCanvas.loadImage("/hd/chat");
        imgFocus = GameCanvas.loadImage("/hd/focus");
        imgMenu = GameCanvas.loadImage("/hd/menu");
        imgTopBar = GameCanvas.loadImage("/hd/topbar");
        imgMapBorder = GameCanvas.loadImage("/hd/mapborder");
        imgAnalog1 = GameCanvas.loadImage("/hd/analog1");
        imgAnalog2 = GameCanvas.loadImage("/hd/analog2");
        sbyte[] array = RMS.loadRMS("dataVersion");
        sbyte[] array2 = RMS.loadRMS("mapVersion");
        sbyte[] array3 = RMS.loadRMS("skillVersion");
        sbyte[] array4 = RMS.loadRMS("itemVersion");
        if (array != null)
        {
            vcData = array[0];
        }

        if (array2 != null)
        {
            vcMap = array2[0];
        }

        if (array3 != null)
        {
            vcSkill = array3[0];
        }

        if (array4 != null)
        {
            vcItem = array4[0];
        }
    }

    public static void resetAllvector()
    {
        vCharInMap.removeAllElements();
        vItemMap.removeAllElements();
        vMobSoul.removeAllElements();
        Effect2.vEffect2.removeAllElements();
        Effect2.vAnimateEffect.removeAllElements();
        Effect2.vEffect2Outside.removeAllElements();
        vMobAttack.removeAllElements();
        vSet.removeAllElements();
        vMob.removeAllElements();
        vNpc.removeAllElements();
        vBuNhin.removeAllElements();
        Char.getMyChar().vMovePoints.removeAllElements();
    }

    public void readPart()
    {
        DataInputStream dataInputStream = null;
        try
        {
            dataInputStream = new DataInputStream(RMS.loadRMS("nj_part"));
            int num = dataInputStream.readShort();
            parts = new Part[num];
            for (int i = 0; i < num; i++)
            {
                int type = dataInputStream.readByte();
                parts[i] = new Part(type);
                for (int j = 0; j < parts[i].pi.Length; j++)
                {
                    parts[i].pi[j] = new PartImage();
                    parts[i].pi[j].id = dataInputStream.readShort();
                    parts[i].pi[j].dx = dataInputStream.readByte();
                    parts[i].pi[j].dy = dataInputStream.readByte();
                }
            }
        }
        catch (Exception ex)
        {
            Out.println(" loi doc part ");
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            try
            {
                dataInputStream.close();
            }
            catch (Exception ex2)
            {
                Out.println(ex2.Message + ex2.StackTrace);
            }
        }
    }

    public void readEfect()
    {
        DataInputStream dataInputStream = null;
        try
        {
            dataInputStream = new DataInputStream(RMS.loadRMS("nj_effect"));
            int num = dataInputStream.readShort();
            efs = new EffectCharPaint[num];
            for (int i = 0; i < num; i++)
            {
                efs[i] = new EffectCharPaint();
                efs[i].idEf = dataInputStream.readShort();
                efs[i].arrEfInfo = new EffectInfoPaint[dataInputStream.readByte()];
                for (int j = 0; j < efs[i].arrEfInfo.Length; j++)
                {
                    efs[i].arrEfInfo[j] = new EffectInfoPaint();
                    efs[i].arrEfInfo[j].idImg = dataInputStream.readShort();
                    efs[i].arrEfInfo[j].dx = dataInputStream.readByte();
                    efs[i].arrEfInfo[j].dy = dataInputStream.readByte();
                }
            }
        }
        catch (Exception ex)
        {
            Out.println("loi doc eff");
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            try
            {
                dataInputStream.close();
            }
            catch (Exception ex2)
            {
                Out.println(ex2.Message + ex2.StackTrace);
            }
        }
    }

    public void readArrow()
    {
        DataInputStream dataInputStream = null;
        try
        {
            dataInputStream = new DataInputStream(RMS.loadRMS("nj_arrow"));
            int num = dataInputStream.readShort();
            arrs = new Arrowpaint[num];
            for (int i = 0; i < num; i++)
            {
                arrs[i] = new Arrowpaint();
                arrs[i].id = dataInputStream.readShort();
                arrs[i].imgId[0] = dataInputStream.readShort();
                arrs[i].imgId[1] = dataInputStream.readShort();
                arrs[i].imgId[2] = dataInputStream.readShort();
            }
        }
        catch (Exception ex)
        {
            Out.println(" loi doc arr");
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            try
            {
                dataInputStream.close();
            }
            catch (Exception ex2)
            {
                Out.println(ex2.Message + ex2.StackTrace);
            }
        }
    }

    public static void clearGameScr()
    {
        instance = null;
        arrItemUpPeal = (arrItemUpGrade = (arrItemSplit = (arrItemTradeMe = (arrItemTradeOrder = null))));
        itemUpGrade = (itemSplit = null);
    }

    public void loadGameScr()
    {
        loadSplash();
        loadCmdBar();
        Res.init();
    }

    private void initCommand()
    {
        cmdBijuuAddPoint = new Command(mResources.ADD, 5043);
        cmdBijuuSkillUp = new Command(mResources.ADD, 5053);
        cmdAddFriend = new Command(mResources.ACCEPT, 11002);
        cmdSkillUp = new Command(mResources.ADD, 11003);
        cmdGangTayNuView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11004);
        cmdGangTayNuBuy = new Command(mResources.BUY, 11005);
        cmdPotentialAdd = new Command(mResources.ADD, 11006);
        cmdGangTayNamView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11007);
        cmdGangTayNamBuy = new Command(mResources.BUY, 11008);
        cmdAoNuView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11009);
        cmdAoNuBuy = new Command(mResources.BUY, 11010);
        cmdAoNamView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11011);
        cmdAoNamBuy = new Command(mResources.BUY, 11012);
        cmdNonNuView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11013);
        cmdNonNuBuy = new Command(mResources.BUY, 11014);
        cmdNonNamView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11015);
        cmdNonNamBuy = new Command(mResources.BUY, 11016);
        cmdStoreFashionView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 13001);
        cmdStoreLockBuy = new Command(mResources.BUY, 11018);
        cmdStoreFashionBuy = new Command(mResources.BUY, 13002);
        cmdStoreView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11019);
        cmdStoreBuy = new Command(mResources.BUY, 11020);
        cmdEliteShopBuy = new Command(mResources.BUY, 14022);
        cmdEliteShopView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 14023);
        cmdClanStoreView = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 14018);
        cmdClanStoreBuy = new Command(mResources.BUY, 14019);
        cmdCloseAll = new Command(mResources.CLOSE, 11021);
        cmdBagSelectItem = new Command(mResources.SELECT, 11022);
        cmdBagViewItemInfo = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11023);
        cmdBagThrowItem = new Command(mResources.THROW, 11024);
        cmdBagSplitItem = new Command("Tách số lượng", 110244);
        cmdItemInfoClose = new Command(mResources.CLOSE, 11025);
        cmdBagUseItem = new Command(mResources.USE, 11026);
        cmdBagSortItem = new Command(mResources.SORT, 110221);
        cmdTradeSelectInList = new Command(mResources.SELECT, 11027);
        cmdTradeSelectInBag = new Command(mResources.SELECT, 11028);
        cmdTradeSelectItem = new Command(mResources.SELECT, 11029);
        cmdTradeViewItemInfo = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11030);
        cmdback = new Command(mResources.BACK, 11021);
        menu = new Command(mResources.MENU, 11000);
        cmdFocus = new Command(string.Empty, 11001);
        cmdTradeLock = new Command(mResources.LOCK1, 11032);
        cmdTradeAccept = new Command(mResources.ACCEPT, 11033);
        cmdUpgradeMoveOut = new Command(mResources.MOVEOUT, 11034);
        cmdConvertMoveOut = new Command(mResources.MOVEOUT, 14014);
        cmdSplitMoveOut = new Command(mResources.MOVEOUT, 11035);
        cmdTradeSendMoney = new Command(mResources.SENDMONEY, 11036);
        cmdTradeMoveOut = new Command(mResources.MOVEOUT, 11037);
        cmdKhamNgocMoveOut_Ngoc = new Command(mResources.MOVEOUT, 339);
        cmdKhamNgocMoveOut_Item = new Command(mResources.MOVEOUT, 340);
        cmdKhamNgocMoveOut_Stone = new Command(mResources.MOVEOUT, 343);
        cmdGiaoDoMoveOut = new Command(mResources.MOVEOUT, 402);
        cmd240 = new Command("240", 110381);
        cmd360 = new Command("360", 1103911);
        cmdallmap = new Command("Toàn Map", 110401);
        cmdTrangbi2 = new Command(mResources.tbi2, 2003);
        cmdTrangbi2.setpos(GameCanvas.w / 2 - mScreen.cmdW / 2, Paint.CmdY);
        if (GameCanvas.isTouch && GameCanvas.isTouchControl)
        {
            menu.x = gW - 135;
            menu.y = 6;
            menu.img = imgMenu;
            cmdFocus.x = gW;
            cmdFocus.y = gH;
            if (GameCanvas.isTouchControlSmallScreen)
            {
                menu.x = gW / 2 - 38;
                menu.y = gH - 34;
            }
        }
    }

    public static void loadCamera(bool fullScreen)
    {
        gW = GameCanvas.w;
        if (!GameCanvas.isTouch || (GameCanvas.isTouch && !GameCanvas.isTouchControl))
        {
            cmdBarH = 36;
            if (GameCanvas.isTouch)
            {
                cmdBarH += 3;
            }
        }
        else
        {
            cmdBarH = 8;
        }

        gH = GameCanvas.h - cmdBarH - 20;
        if (fullScreen)
        {
            gH = GameCanvas.h;
        }

        if (GameCanvas.isTouch && GameCanvas.isTouchControl)
        {
            gH = GameCanvas.h;
        }

        if (GameCanvas.h == 160)
        {
            gH = 150;
        }

        cmdBarW = gW;
        if (GameCanvas.w > 176)
        {
            cmdBarW -= 50;
        }

        cmdBarX = 0;
        cmdBarY = GameCanvas.h - Paint.hTab - cmdBarH;
        girlHPBarY = 0;
        csPadMaxH = GameCanvas.h / 6;
        if (csPadMaxH < 48)
        {
            csPadMaxH = 48;
        }

        gW2 = gW >> 1;
        gH2 = gH >> 1;
        gW3 = gW / 3;
        gH3 = gH / 3;
        gW23 = 2 * gW / 3;
        gH23 = 2 * gH / 3;
        gW34 = 3 * gW / 4;
        gH34 = 3 * gH / 4;
        gW6 = gW / 6;
        gH6 = gH / 6;
        gssw = gW / TileMap.size + 2;
        gssh = gH / TileMap.size + 2;
        if (gW % 24 != 0)
        {
            gssw++;
        }

        cmxLim = (TileMap.tmw - 1) * TileMap.size - gW;
        cmyLim = (TileMap.tmh - 1) * TileMap.size - gH;
        if (GameCanvas.isTouch && GameCanvas.isTouchControl)
        {
            cmyLim += 60;
        }

        cmx = (cmtoX = Char.getMyChar().cx - gW2);
        cmy = (cmtoY = Char.getMyChar().cy - gH23);
        if (cmx < 24)
        {
            cmx = 24;
        }

        if (cmx > cmxLim)
        {
            cmx = cmxLim;
        }

        if (cmy < 0)
        {
            cmy = 0;
        }

        if (cmy > cmyLim)
        {
            cmy = cmyLim;
        }

        gssx = cmx / TileMap.size - 1;
        if (gssx < 0)
        {
            gssx = 0;
        }

        gssy = cmy / TileMap.size;
        gssxe = gssx + gssw;
        gssye = gssy + gssh;
        if (gssy < 0)
        {
            gssy = 0;
        }

        if (gssye > TileMap.tmh - 1)
        {
            gssye = TileMap.tmh - 1;
        }

        TileMap.countx = (gssxe - gssx) * 4;
        if (TileMap.countx > TileMap.tmw)
        {
            TileMap.countx = TileMap.tmw;
        }

        TileMap.county = (gssye - gssy) * 4;
        if (TileMap.county > TileMap.tmh)
        {
            TileMap.county = TileMap.tmh;
        }

        TileMap.gssx = (Char.getMyChar().cx - 2 * gW) / TileMap.size;
        if (TileMap.gssx < 0)
        {
            TileMap.gssx = 0;
        }

        TileMap.gssxe = TileMap.gssx + TileMap.countx;
        if (TileMap.gssxe > TileMap.tmw)
        {
            TileMap.gssxe = TileMap.tmw;
        }

        TileMap.gssy = (Char.getMyChar().cy - 2 * gH) / TileMap.size;
        if (TileMap.gssy < 0)
        {
            TileMap.gssy = 0;
        }

        TileMap.gssye = TileMap.gssy + TileMap.county;
        if (TileMap.gssye > TileMap.tmh)
        {
            TileMap.gssye = TileMap.tmh;
        }

        ChatTextField.gI().tfChat.y = GameCanvas.h - 35 - ChatTextField.gI().tfChat.height;
        if (!GameCanvas.isTouch || (GameCanvas.isTouch && !GameCanvas.isTouchControl))
        {
            TileMap.setPosMiniMap(GameCanvas.w - 51, cmdBarY - 4, 50, 40);
        }
        else
        {
            TileMap.setPosMiniMap(GameCanvas.w - 60, 0, 60, 42);
        }

        yTouchBar = gH - 88;
        if (!Main.isPC)
        {
            xC = gW - 100;
            yC = 2;
            xL = 1;
            yL = yTouchBar + 50;
            xR = 42;
            yR = yTouchBar + 50;
            xF = gW - 50;
            yF = yTouchBar + 35;
            xU = 22;
            yU = yTouchBar + 19;
            xHP = gW - 74;
            yHP = yTouchBar + 13;
            xMP = gW - 85;
            yMP = yTouchBar + 50;
            xTG = gW - 37;
            yTG = yTouchBar - 1;
            if (GameCanvas.w >= 450)
            {
                yU -= 15;
                xU += 28;
                xR += 45;
                xL += 10;
                yTG -= 12;
                yHP -= 7;
                xF -= 18;
                xTG -= 10;
                xHP -= 17;
                xMP -= 24;
            }
            else if (GameCanvas.w >= 360)
            {
                yU -= 5;
                xU += 6;
                xR += 12;
                yTG -= 2;
                yHP -= 2;
                xHP -= 2;
                xMP -= 2;
            }
        }
        else
        {
            xMP = gW - 40;
            yMP = yTouchBar + 51;
            xHP = gW - 85;
            yHP = yTouchBar + 51;
        }

        xS = new int[onScreenSkill.Length];
        yS = new int[onScreenSkill.Length];
        if (GameCanvas.isTouch)
        {
            if (GameCanvas.isTouchControlSmallScreen)
            {
                xSkill = 2;
                ySkill = 55;
                padSkill = 5;
                for (int i = 0; i < xS.Length; i++)
                {
                    xS[i] = i * (25 + padSkill);
                    yS[i] = ySkill;
                }

                return;
            }

            if (GameCanvas.w <= 320)
            {
                xSkill = gW2 - onScreenSkill.Length * 25 / 2 - 15;
            }
            else
            {
                xSkill = gW2 - onScreenSkill.Length * 25 / 2 - 15;
            }

            ySkill = yTouchBar + 58;
            padSkill = 5;
            for (int j = 0; j < xS.Length; j++)
            {
                xS[j] = j * (25 + padSkill);
                yS[j] = ySkill;
            }
        }
        else
        {
            xSkill = 0;
            for (int k = 0; k < yS.Length; k++)
            {
                xS[k] = 2;
                yS[k] = 2 + 25 * k;
            }
        }
    }

    public void resetButton()
    {
        if (Char.getMyChar().arrItemBag != null)
        {
            if ((isPaintUpPearl || isPaintLuyenThach) && arrItemUpPeal != null)
            {
                for (int i = 0; i < arrItemUpPeal.Length; i++)
                {
                    if (arrItemUpPeal[i] != null)
                    {
                        Char.getMyChar().arrItemBag[arrItemUpPeal[i].indexUI] = arrItemUpPeal[i];
                        arrItemUpPeal[i] = null;
                    }
                }
            }

            if (isPaintUpGrade)
            {
                if (itemUpGrade != null)
                {
                    Char.getMyChar().arrItemBag[itemUpGrade.indexUI] = itemUpGrade;
                    itemUpGrade = null;
                }

                if (arrItemUpGrade != null)
                {
                    for (int j = 0; j < arrItemUpGrade.Length; j++)
                    {
                        if (arrItemUpGrade[j] != null)
                        {
                            Char.getMyChar().arrItemBag[arrItemUpGrade[j].indexUI] = arrItemUpGrade[j];
                            arrItemUpGrade[j] = null;
                        }
                    }
                }
            }

            if (isPaintKhamNgoc)
            {
                if (itemUpGrade != null)
                {
                    Char.getMyChar().arrItemBag[itemUpGrade.indexUI] = itemUpGrade;
                    itemUpGrade = null;
                }

                if (itemSplit != null)
                {
                    Char.getMyChar().arrItemBag[itemSplit.indexUI] = itemSplit;
                    itemSplit = null;
                }

                if (arrItemUpGrade != null)
                {
                    for (int k = 0; k < arrItemUpGrade.Length; k++)
                    {
                        if (arrItemUpGrade[k] != null)
                        {
                            Char.getMyChar().arrItemBag[arrItemUpGrade[k].indexUI] = arrItemUpGrade[k];
                            arrItemUpGrade[k] = null;
                        }
                    }
                }
            }

            if (isPaintGiaoDo && arrItemUpGrade != null)
            {
                for (int l = 0; l < arrItemUpGrade.Length; l++)
                {
                    if (arrItemUpGrade[l] != null)
                    {
                        Char.getMyChar().arrItemBag[arrItemUpGrade[l].indexUI] = arrItemUpGrade[l];
                        arrItemUpGrade[l] = null;
                    }
                }
            }

            if (isPaintAuctionSale && itemSell != null)
            {
                Char.getMyChar().arrItemBag[itemSell.indexUI] = itemSell;
                itemSell = null;
            }

            if (isPaintConvert && arrItemConvert != null)
            {
                for (int m = 0; m < arrItemConvert.Length; m++)
                {
                    if (arrItemConvert[m] != null)
                    {
                        Char.getMyChar().arrItemBag[arrItemConvert[m].indexUI] = arrItemConvert[m];
                        arrItemConvert[m] = null;
                    }
                }
            }

            if (isPaintSplit || isPaintDichChuyen || isPaintTinhluyen || isPaintLuyenNgoc)
            {
                if (itemSplit != null)
                {
                    Char.getMyChar().arrItemBag[itemSplit.indexUI] = itemSplit;
                    itemSplit = null;
                }

                if (arrItemSplit != null)
                {
                    for (int n = 0; n < arrItemSplit.Length; n++)
                    {
                        if (arrItemSplit[n] != null)
                        {
                            if (isPaintTinhluyen || isPaintDichChuyen || isPaintLuyenNgoc)
                            {
                                Char.getMyChar().arrItemBag[arrItemSplit[n].indexUI] = arrItemSplit[n];
                            }

                            arrItemSplit[n] = null;
                        }
                    }
                }
            }

            if (isPaintGiaoDo && arrItemSplit != null)
            {
                for (int num = 0; num < arrItemSplit.Length; num++)
                {
                    if (arrItemSplit[num] != null)
                    {
                        Char.getMyChar().arrItemBag[arrItemSplit[num].indexUI] = arrItemSplit[num];
                        arrItemSplit[num] = null;
                    }
                }
            }

            if (isPaintTrade)
            {
                InfoDlg.hide();
                if (coinTrade > 0)
                {
                    Char.getMyChar().xu += coinTrade;
                }

                if (arrItemTradeMe != null)
                {
                    for (int num2 = 0; num2 < arrItemTradeMe.Length; num2++)
                    {
                        if (arrItemTradeMe[num2] != null)
                        {
                            Char.getMyChar().arrItemBag[arrItemTradeMe[num2].indexUI] = arrItemTradeMe[num2];
                            arrItemTradeMe[num2] = null;
                        }
                    }
                }

                if (arrItemTradeOrder != null)
                {
                    for (int num3 = 0; num3 < arrItemTradeOrder.Length; num3++)
                    {
                        arrItemTradeOrder[num3] = null;
                    }
                }
            }

            if ((isPaintGotNgoc || isPaintThaoNgoc) && itemSplit != null)
            {
                Char.getMyChar().arrItemBag[itemSplit.indexUI] = itemSplit;
                itemSplit = null;
            }
        }

        if (isPaintItemDell || isPaintAuto)
        {
            Handle.SaveAuto();
        }

        if (isPaintTrade)
        {
            Service.gI().cancelTrade();
        }

        GameCanvas.menu.showMenu = false;
        ChatTextField.gI().close();
        ChatTextField.gI().center = null;
        if (!GameCanvas.isTouch)
        {
            isPaintSelectSkill = false;
        }

        isMessageMenu = false;
        isPaintUpGradeGold = false;
        isLockKey = false;
        isPaintZone = false;
        isPaintInfoMe = false;
        isPaintItemInfo = false;
        isPaintTask = false;
        isPaintTeam = false;
        isPaintAuto = false;
        isPaintMessage = false;
        isPaintClan = false;
        isPaintLuckySpin = false;
        isRequestMember = false;
        isPaintCharInMap = false;
        isPaintFindTeam = false;
        isPaintFriend = false;
        isPaintAuctionBuy = false;
        isPaintList = false;
        isPaintRankedList = false;
        isPaintEnemies = false;
        isPaintAlert = false;
        isPaintLucky_Draw = false;
        typeTrade = 0;
        isPaintStore = false;
        isPaintEliteShop = false;
        isPaintNonNam = false;
        isPaintNonNu = false;
        isPaintAoNam = false;
        isPaintAoNu = false;
        isPaintGangTayNam = false;
        isPaintGangTayNu = false;
        isPaintQuanNam = false;
        isPaintQuanNu = false;
        isPaintGiayNam = false;
        isPaintGiayNu = false;
        isPaintLien = false;
        isPaintNhan = false;
        isPaintNgocBoi = false;
        isPaintPhu = false;
        isPaintWeapon = false;
        isPaintStack = false;
        isPaintStackLock = false;
        isPaintGrocery = false;
        isPaintGroceryLock = false;
        isPaintUpGrade = false;
        isPaintAuctionSale = false;
        isPaintConvert = false;
        isPaintSplit = (isPaintTinhluyen = (isPaintDichChuyen = false));
        isPaintTrade = false;
        isPaintUpPearl = (isPaintLuyenThach = false);
        isPaintBox = false;
        isPaintItemDell = false;
        isPaintKhamNgoc = false;
        isPaintLuyenNgoc = false;
        isPaintGiaoDo = false;
        isPaintGotNgoc = false;
        isPaintThaoNgoc = false;
        indexMenu = 0;
        indexSelect = 0;
        indexItemUse = -1;
        indexRow = -1;
        indexRowMax = 0;
        indexTitle = 0;
        typeTrade = (typeTradeOrder = 0);
        left = menu;
        right = cmdFocus;
        xMounts = (yMounts = null);
        center = null;
        if (Char.getMyChar().cHP <= 0 || Char.getMyChar().statusMe == 14 || Char.getMyChar().statusMe == 5)
        {
            if (GameCanvas.isTouchControlSmallScreen)
            {
                cmdDead.caption = string.Empty;
            }

            center = cmdDead;
        }

        scrMain.clear();
    }

    private void resetAuto()
    {
        auto = 0;
        lockAutoMove = (Char.getMyChar().isLockMove = false);
    }

    public static void resetTranslate(mGraphics g)
    {
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        g.setClip(0, -200, GameCanvas.w, 200 + GameCanvas.h);
    }

    public static void loadSplash()
    {
        if (imgSplash == null)
        {
            imgSplash = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                imgSplash[i] = GameCanvas.loadImage("/e/sp" + i + string.Empty);
            }
        }

        splashX = new int[2];
        splashY = new int[2];
        splashState = new int[2];
        splashF = new int[2];
        splashDir = new int[2];
        splashState[0] = (splashState[1] = -1);
    }

    public void loadCmdBar()
    {
        hpBarX = 63;
        hpBarY = cmdBarY + 7;
        hpBarW = gW - 84 - 30 + 15;
        expBarW = gW - 44 - 4;
        hpBarH = 5;
        if (GameCanvas.w > 176)
        {
            hpBarW -= 50;
            expBarW -= 50;
            hpBarX += 15;
            hpBarW -= 15;
        }

        loadInforBar();
    }

    private void loadInforBar()
    {
        if (GameCanvas.isTouch)
        {
            hpBarW = 82;
            mpBarW = 57;
            hpBarX = 52;
            hpBarY = ((!GameCanvas.isKiemduyet) ? (10 + Info.hI) : 25);
            expBarW = gW - 61;
            if (!Main.isPC)
            {
                menu.y = 6 + Info.hI;
                xC = gW - 100;
                yC = 2 + Info.hI;
            }
            else
            {
                // menu and chat icons sit in the top-right corner, to the left of the minimap
                int topY = ((!GameCanvas.isKiemduyet) ? Info.hI : 16) + (42 - topIconSize) / 2;
                int chatX = GameCanvas.w - 60 - topIconGap - topIconSize;
                int menuX = chatX - topIconGap - topIconSize;
                xC = chatX;
                yC = topY;
                menu.x = menuX;
                menu.y = topY;
                menu.imgDrawSize = topIconSize;
                menu.w = topIconSize;
                menu.h = topIconSize;
                chatIconSize = topIconSize;
            }

            TileMap.setPosMiniMap(GameCanvas.w - 60, (!GameCanvas.isKiemduyet) ? Info.hI : 16, 60, 42);
        }
    }

    public static void loadImg()
    {
        TileMap.loadTileImage();
    }

    public void resetCaptcha()
    {
        arrowCaptcha = new sbyte[6]
        {
            -1,
            -1,
            -1,
            -1,
            -1,
            -1
        };
    }
}
