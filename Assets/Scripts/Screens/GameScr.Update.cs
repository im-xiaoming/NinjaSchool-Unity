using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    private static void updateCamera()
    {
        if (shaking != 0 && !GameCanvas.lowGraphic && !Main.isIpod && mGraphics.zoomLevel != 1)
        {
            cmx += NinjaUtil.random(-7, 7);
            count++;
            if (count > 20)
            {
                shaking = 0;
                count = 0;
            }
        }
        else if (cmx != cmtoX || cmy != cmtoY)
        {
            cmvx = cmtoX - cmx << 2;
            cmvy = cmtoY - cmy << 2;
            cmdx += cmvx;
            cmx += cmdx >> 4;
            cmdx &= 15;
            cmdy += cmvy;
            cmy += cmdy >> 4;
            cmdy &= 15;
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

        TileMap.gssx = (Char.getMyChar().cx - 2 * gW) / TileMap.size;
        if (TileMap.gssx < 0)
        {
            TileMap.gssx = 0;
        }

        TileMap.gssxe = TileMap.gssx + TileMap.countx;
        if (TileMap.gssxe > TileMap.tmw)
        {
            TileMap.gssxe = TileMap.tmw;
            TileMap.gssx = TileMap.gssxe - TileMap.countx;
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
            TileMap.gssy = TileMap.gssye - TileMap.county;
        }

        scrMain.updatecm();
        scrInfo.updatecm();
    }

    public void updateOpen()
    {
        if (isstarOpen)
        {
            if (moveUp > -3)
            {
                moveUp -= 4;
            }
            else
            {
                moveUp = -2;
            }

            if (moveDow < GameCanvas.h + 3)
            {
                moveDow += 4;
            }
            else
            {
                moveDow = GameCanvas.h + 2;
            }

            if (moveUp <= -2 && moveDow >= GameCanvas.h + 2)
            {
                isstarOpen = false;
            }
        }
    }

    public override void update()
    {
        if (mSystem.currentTimeMillis() - MainFuctions.time_ktg > 300000)
        {
            MainFuctions.time_ktg = mSystem.currentTimeMillis();
            Info.addInfo("Chào mừng bạn tới thế giới Ninja School Online", 80, mFont.tahoma_7b_yellow);
        }

        if (GiuLV && Char.getMyChar().cExpR * 100 / exps[Char.getMyChar().clevel] >= 98)
        {
            Main.exit();
        }

        if (isTudanh && GameCanvas.gameTick % 20 == 0)
        {
            TuDanh();
        }

        // Offline: ordinary loot is manual; enabled Thien bien lenh is the explicit exception.
        autoPickItem();
        if (charcountdonw != null)
        {
            charcountdonw.update();
            if (charcountdonw.WantDestroy)
            {
                charcountdonw = null;
            }
        }

        runArrow++;
        if (runArrow > 3)
        {
            runArrow = 0;
        }

        updateCamera();
        if (indexMenu == 4 && GameCanvas.isTouch && mScreen.getCmdPointerLast(cmdTrangbi2) && cmdTrangbi2 != null)
        {
            GameCanvas.isPointerJustRelease = false;
            GameCanvas.keyPressedz[5] = false;
            mScreen.keyTouch = -1;
            cmdTrangbi2.performAction();
        }

        if (GameCanvas.gameTick % 200 == 0)
        {
            Char.SetRemove();
        }

        ChatTextField.gI().update();
        TileMap.updateCmMiniMap();
        GameCanvas.gI().updateBallEffect();
        TileMap.updateMusic();
        if (GameCanvas.isGPRS)
        {
            MyVector myVector = new MyVector();
            long currentTimeMillis = mSystem.getCurrentTimeMillis();
            for (int i = 0; i < vCharInMap.size(); i++)
            {
                Char @char = (Char)vCharInMap.elementAt(i);
                if (@char == null)
                {
                    continue;
                }

                @char.update();
                if (@char.isPaint())
                {
                    if (@char.isDirtyPostion && currentTimeMillis - @char.lastUpdateTime > 10000 && currentTimeMillis - lastSendUpdatePostion > 10000)
                    {
                        @char.isDirtyPostion = false;
                        @char.lastUpdateTime = currentTimeMillis;
                        myVector.addElement(@char);
                    }
                }
                else
                {
                    @char.lastUpdateTime = currentTimeMillis;
                    @char.isDirtyPostion = true;
                }
            }

            if (myVector.size() > 0)
            {
                Service.gI().requestPlayerInfo(myVector);
                lastSendUpdatePostion = currentTimeMillis;
            }
        }
        else
        {
            for (int i = 0; i < vCharInMap.size(); i++)
            {
                ((Char)vCharInMap.elementAt(i))?.update();
            }
        }

        GameCanvas.debug("E5", 0);
        Char.getMyChar().update();
        GameCanvas.debug("E5x", 0);
        for (int i = 0; i < vMob.size(); i++)
        {
            ((Mob)vMob.elementAt(i))?.update();
        }

        GameCanvas.debug("E6", 0);
        for (int i = 0; i < vNpc.size(); i++)
        {
            ((Npc)vNpc.elementAt(i))?.update();
        }

        GameCanvas.debug("E7", 0);
        GameCanvas.gI().updateDust();
        GameCanvas.debug("E8", 0);
        updateFlyText();
        updateLanterns();
        updateSplash();
        updateSS();
        GameCanvas.updateBG();
        GameCanvas.debug("E9", 0);
        for (int i = 0; i < vMobAttack.size(); i++)
        {
            ((MobAttack)vMobAttack.elementAt(i))?.update();
        }

        GameCanvas.debug("E10", 0);
        for (int i = 0; i < vItemMap.size(); i++)
        {
            ((ItemMap)vItemMap.elementAt(i))?.update();
        }

        for (int i = 0; i < vMobSoul.size(); i++)
        {
            ((MobSoul)vMobSoul.elementAt(i)).update();
        }

        if (tfText != null && tfText.isFocus)
        {
            tfText.update();
        }

        GameCanvas.debug("E11", 0);
        for (int i = 0; i < vSet.size(); i++)
        {
            ((Set)vSet.elementAt(i))?.update();
        }

        GameCanvas.debug("E12", 0);
        if ((TileMap.tmw * TileMap.sizeMiniMap >= TileMap.wMiniMap || TileMap.tmh * TileMap.sizeMiniMap >= TileMap.hMiniMap) && mSystem.getCurrentTimeMillis() / 100 - TileMap.timeTranMini > 20)
        {
            TileMap.updateMiniMap();
        }

        GameCanvas.debug("E13", 0);
        for (int i = Effect2.vRemoveEffect2.size() - 1; i >= 0; i--)
        {
            Effect2.vEffect2.removeElement(Effect2.vRemoveEffect2.elementAt(i));
            Effect2.vRemoveEffect2.removeElementAt(i);
        }

        for (int i = 0; i < Effect2.vEffect2.size(); i++)
        {
            ((Effect2)Effect2.vEffect2.elementAt(i))?.update();
        }

        for (int i = 0; i < Effect2.vEffect2Outside.size(); i++)
        {
            ((Effect2)Effect2.vEffect2Outside.elementAt(i))?.update();
        }

        for (int i = 0; i < Effect2.vAnimateEffect.size(); i++)
        {
            ((Effect2)Effect2.vAnimateEffect.elementAt(i))?.update();
        }

        for (int i = 0; i < Mob.vEggMonter.size(); i++)
        {
            EggMonters eggMonters = (EggMonters)Mob.vEggMonter.elementAt(i);
            if (eggMonters == null)
            {
                continue;
            }

            eggMonters.update();
            if (eggMonters.frame == 6)
            {
                if (EggMonters.ownerEgg != null)
                {
                    EggMonters.ownerEgg.status = 5;
                }

                Mob.vEggMonter.removeElementAt(i);
            }
        }

        SmallImage.checkTimeUseImg();
        autoFocus();
        Info.update();
        InfoMe.update();
        if (currentCharViewInfo != null && currentCharViewInfo.charID != Char.getMyChar().charID)
        {
            currentCharViewInfo.update();
        }

        if (isPaintRankedList)
        {
            indexSize = 40;
        }
        else
        {
            indexSize = 28;
        }

        EffectAuto.checkTimeUseImg();
        EffectAuto.checkTimeUseData();
    }

    public static void updateFlyText()
    {
        for (int i = 0; i < 5; i++)
        {
            if (flyTextState[i] != -1)
            {
                flyTextState[i] += Res.abs(flyTextDy[i]);
                if (flyTextState[i] > 30)
                {
                    flyTextState[i] = -1;
                }

                flyTextX[i] += flyTextDx[i];
                flyTextY[i] += flyTextDy[i];
            }
        }
    }

    public static void updateLanterns()
    {
        for (int i = 0; i < vLanterns.size(); i++)
        {
            ((Lanterns)vLanterns.elementAt(i)).update();
            if (((Lanterns)vLanterns.elementAt(i)).isEnd)
            {
                vLanterns.removeElementAt(i);
            }
        }
    }

    public static void updateSplash()
    {
        for (int i = 0; i < 2; i++)
        {
            if (splashState[i] != -1)
            {
                splashState[i]++;
                splashX[i] += splashDir[i] << 2;
                splashY[i]--;
                if (splashState[i] >= 6)
                {
                    splashState[i] = -1;
                }
                else
                {
                    splashF[i] = (splashState[i] >> 1) % 3;
                }
            }
        }
    }

    public void updateSS()
    {
        if (indexMenu != -1)
        {
            if (cmySK != cmtoYSK)
            {
                cmvySK = cmtoYSK - cmySK << 2;
                cmdySK += cmvySK;
                cmySK += cmdySK >> 4;
                cmdySK &= 15;
            }

            if (Math.abs(cmtoYSK - cmySK) < 15 && cmySK < 0)
            {
                cmtoYSK = 0;
            }

            if (Math.abs(cmtoYSK - cmySK) < 15 && cmySK > cmyLimSK)
            {
                cmtoYSK = cmyLimSK;
            }
        }
    }

    public void updateSelectList()
    {
        if (!isPaintRankedList && !isPaintTeam && !isPaintFindTeam && !isPaintFriend && !isPaintEnemies && !isPaintCharInMap && !isPaintList && !isPaintAuctionBuy)
        {
            return;
        }

        if (isPaintAuctionBuy)
        {
            if (!isPaintAuctionBuy)
            {
                return;
            }

            ScrollResult scrollResult = scrMain.updateKey();
            if (scrollResult.isDowning || scrollResult.isFinish)
            {
                indexSelect = scrollResult.selected;
                if (indexSelect >= arrItemStands.Length)
                {
                    indexSelect = -1;
                }

                if (indexSelect >= 0)
                {
                    indexTitle = 1;
                }

                updateCommandForUI();
            }

            return;
        }

        if (isPaintTeam)
        {
            if (vParty.size() == 0)
            {
                return;
            }

            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= vParty.size())
                {
                    indexRow = vParty.size() - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setPartyCommand();
        }
        else if (isPaintFindTeam)
        {
            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= vPtMap.size())
                {
                    indexRow = vPtMap.size() - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            refreshFindTeam();
        }
        else if (isPaintFriend)
        {
            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= indexRowMax)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = indexRowMax - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setFriendCommand();
        }
        else if (isPaintEnemies)
        {
            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= indexRowMax)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = indexRowMax - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setEnemiesCommand();
        }
        else if (isPaintCharInMap)
        {
            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= vCharInMap.size())
                {
                    indexRow = vCharInMap.size() - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            if (cLastFocusID > 0 && !GameCanvas.isTouch)
            {
                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setCharFocusCommand();
        }
        else if (isPaintList)
        {
            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= vList.size())
                {
                    indexRow = vList.size() - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setDunListCommand();
        }
        else if (isPaintRankedList)
        {
            if (GameCanvas.keyPressedz[Key.NUM8])
            {
                indexRow++;
                if (indexRow >= vList.size())
                {
                    indexRow = vList.size() - 1;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM2])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setRankedListCommand();
        }

        if (GameCanvas.isTouch && GameCanvas.currentDialog == null && !GameCanvas.menu.showMenu)
        {
            MyVector myVector = null;
            if (isPaintTeam)
            {
                myVector = vParty;
            }
            else if (isPaintFriend)
            {
                myVector = vFriend;
            }
            else if (isPaintEnemies)
            {
                myVector = vEnemies;
            }
            else if (isPaintFindTeam)
            {
                myVector = vPtMap;
            }
            else if (isPaintCharInMap)
            {
                myVector = vCharInMap;
            }
            else if (isPaintList)
            {
                myVector = vList;
            }
            else if (isPaintRankedList)
            {
                myVector = vList;
            }

            ScrollResult scrollResult2 = scrMain.updateKey();
            if (scrollResult2.isDowning || scrollResult2.isFinish)
            {
                indexRow = scrollResult2.selected;
                if (indexRow >= myVector.size())
                {
                    indexRow = -1;
                }

                if (isPaintTeam)
                {
                    setPartyCommand();
                }
                else if (isPaintFriend)
                {
                    setFriendCommand();
                }
                else if (isPaintEnemies)
                {
                    setEnemiesCommand();
                }
                else if (isPaintCharInMap)
                {
                    setCharFocusCommand();
                }
                else if (isPaintList)
                {
                    setDunListCommand();
                }
                else if (isPaintRankedList)
                {
                    setRankedListCommand();
                }
            }
        }

        GameCanvas.clearKeyHold();
        GameCanvas.clearKeyPressed();
    }

    public void updateCommandForUI()
    {
        left = (center = null);
        if (indexSelect < 0)
        {
            return;
        }

        if (isPaintAuto)
        {
            // Offline change (user request): the original hid "Chọn" on touch/mouse clients, so PC
            // keyboard/gamepad users could not toggle an auto option. Now shown for every client;
            // clicking the checkboxes still works as before.
            if (indexTitle == 1)
            {
                left = new Command(mResources.SELECT, 1510);
            }

            return;
        }

        if (isPaintLuckySpin)
        {
            if (indexTitle == 1)
            {
                left = new Command(mResources.SELECT, 1506);
                center = new Command(string.Empty, 1507);
            }

            return;
        }

        if (isPaintEliteShop)
        {
            if (indexMenu != 0)
            {
                return;
            }

            Item item = getItemFocus(35);
            if (item != null)
            {
                left = cmdEliteShopBuy;
                if (!GameCanvas.isTouchControlLargeScreen)
                {
                    center = cmdEliteShopView;
                }
                else
                {
                    actView(35);
                }
            }

            return;
        }

        if (isPaintStore)
        {
            if (indexMenu == 52)
            {
                Item item2 = getItemFocus(32);
                if (item2 != null)
                {
                    left = cmdStoreFashionBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdStoreFashionView;
                    }
                    else
                    {
                        actView(32);
                    }
                }
            }
            else if (indexMenu == 0)
            {
                Item item3 = getItemFocus(14);
                if (item3 != null)
                {
                    left = cmdStoreBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdStoreView;
                    }
                    else
                    {
                        actView(14);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                Item item4 = getItemFocus(15);
                if (item4 != null)
                {
                    left = cmdStoreLockBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdStoreLockView;
                    }
                    else
                    {
                        actView(15);
                    }
                }
            }
            else if (indexMenu == 2)
            {
                Item item5 = getItemFocus(32);
                if (item5 != null)
                {
                    left = cmdStoreFashionBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdStoreFashionView;
                    }
                    else
                    {
                        actView(32);
                    }
                }
            }
            else
            {
                if (indexMenu != 3)
                {
                    return;
                }

                Item item6 = getItemFocus(34);
                if (item6 != null)
                {
                    left = cmdClanStoreBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdClanStoreView;
                    }
                    else
                    {
                        actView(34);
                    }
                }
            }

            return;
        }

        if (isPaintNonNam)
        {
            if (indexMenu == 0)
            {
                Item item7 = getItemFocus(20);
                if (item7 != null)
                {
                    left = cmdNonNamBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdNonNamView;
                    }
                    else
                    {
                        actView(20);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintNonNu)
        {
            if (indexMenu == 0)
            {
                Item item8 = getItemFocus(21);
                if (item8 != null)
                {
                    left = cmdNonNuBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdNonNuView;
                    }
                    else
                    {
                        actView(21);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintAoNam)
        {
            if (indexMenu == 0)
            {
                Item item9 = getItemFocus(22);
                if (item9 != null)
                {
                    left = cmdAoNamBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdAoNamView;
                    }
                    else
                    {
                        actView(22);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintAoNu)
        {
            if (indexMenu == 0)
            {
                Item item10 = getItemFocus(23);
                if (item10 != null)
                {
                    left = cmdAoNuBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdAoNuView;
                    }
                    else
                    {
                        actView(23);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintGangTayNam)
        {
            if (indexMenu == 0)
            {
                Item item11 = getItemFocus(24);
                if (item11 != null)
                {
                    left = cmdGangTayNamBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdGangTayNamView;
                    }
                    else
                    {
                        actView(24);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintGangTayNu)
        {
            if (indexMenu == 0)
            {
                Item item12 = getItemFocus(25);
                if (item12 != null)
                {
                    left = cmdGangTayNuBuy;
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = cmdGangTayNuView;
                    }
                    else
                    {
                        actView(25);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintQuanNam)
        {
            if (indexMenu == 0)
            {
                Item item13 = getItemFocus(26);
                if (item13 != null)
                {
                    left = new Command(mResources.BUY, 11092, item13);
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11088, item13);
                    }
                    else
                    {
                        updateItemInfo(26, item13);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintQuanNu)
        {
            if (indexMenu == 0)
            {
                Item item14 = getItemFocus(27);
                if (item14 != null)
                {
                    left = new Command(mResources.BUY, 11092, item14);
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11089);
                    }
                    else
                    {
                        updateItemInfo(27, item14);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintGiayNam)
        {
            if (indexMenu == 0)
            {
                Item item15 = getItemFocus(28);
                if (item15 != null)
                {
                    left = new Command(mResources.BUY, 11092, item15);
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11090);
                    }
                    else
                    {
                        updateItemInfo(28, item15);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintGiayNu)
        {
            if (indexMenu == 0)
            {
                Item item16 = getItemFocus(29);
                if (item16 != null)
                {
                    left = new Command(mResources.BUY, 11092, item16);
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11091);
                    }
                    else
                    {
                        updateItemInfo(29, item16);
                    }
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintLien)
        {
            if (indexMenu == 0)
            {
                Item item17 = getItemFocus(16);
                if (item17 != null)
                {
                    left = new Command(mResources.BUY, 11092, item17);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(16, item17);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 110923);
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintNhan)
        {
            if (indexMenu == 0)
            {
                Item item18 = getItemFocus(17);
                if (item18 != null)
                {
                    left = new Command(mResources.BUY, 11092, item18);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(17, item18);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 110924);
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintNgocBoi)
        {
            if (indexMenu == 0)
            {
                Item item19 = getItemFocus(18);
                if (item19 != null)
                {
                    left = new Command(mResources.BUY, 11092, item19);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(18, item19);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 110925);
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintPhu)
        {
            if (indexMenu == 0)
            {
                Item item20 = getItemFocus(19);
                if (item20 != null)
                {
                    left = new Command(mResources.BUY, 11092, item20);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(19, item20);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 110926);
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintWeapon)
        {
            if (indexMenu == 0)
            {
                Item item21 = getItemFocus(2);
                if (item21 != null)
                {
                    left = new Command(mResources.BUY, 11092, item21);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(2, item21);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11093);
                    }
                }
                else
                {
                    isPaintItemInfo = false;
                }
            }
            else if (indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
            {
                saleItem();
            }
        }
        else if (isPaintStack)
        {
            if (indexMenu == 0)
            {
                Item item22 = getItemFocus(6);
                if (item22 != null)
                {
                    left = new Command(mResources.BUY, 11092, item22);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(6, item22);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11094);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    saleItem();
                }
                else
                {
                    left = cmdBagSortItem;
                }
            }
        }
        else if (isPaintStackLock)
        {
            if (indexMenu == 0)
            {
                Item item23 = getItemFocus(7);
                if (item23 != null)
                {
                    left = new Command(mResources.BUY, 11092, item23);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(7, item23);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11095);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    saleItem();
                }
                else
                {
                    left = cmdBagSortItem;
                }
            }
        }
        else if (isPaintGrocery)
        {
            if (indexMenu == 0)
            {
                Item item24 = getItemFocus(8);
                if (item24 != null)
                {
                    left = new Command(mResources.BUY, 11092, item24);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(8, item24);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11096);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    saleItem();
                }
                else
                {
                    left = cmdBagSortItem;
                }
            }
        }
        else if (isPaintGroceryLock)
        {
            if (indexMenu == 0)
            {
                Item item25 = getItemFocus(9);
                if (item25 != null)
                {
                    left = new Command(mResources.BUY, 11092, item25);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(9, item25);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11097);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    saleItem();
                }
                else
                {
                    left = cmdBagSortItem;
                }
            }
        }
        else if (isPaintUpGrade)
        {
            if (indexMenu == 0)
            {
                if (indexTitle == 1)
                {
                    if (itemUpGrade != null)
                    {
                        if (indexSelect == 0)
                        {
                            left = new Command(mResources.SELECT, 11098);
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                isViewNext = false;
                                updateItemInfo(3, itemUpGrade);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11099);
                            }
                        }
                        else if (indexSelect == 1 && !itemUpGrade.isUpMax())
                        {
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                isViewNext = true;
                                updateItemInfo(3, itemUpGrade);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 110991);
                            }
                        }
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }
                }
                else if (indexTitle == 2)
                {
                    Item item26 = getItemFocus(10);
                    isViewNext = false;
                    if (item26 != null)
                    {
                        left = new Command(mResources.SELECT, 11100);
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(3, item26);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11101);
                        }
                    }
                    else
                    {
                        left = null;
                        isPaintItemInfo = false;
                        if (itemUpGrade != null)
                        {
                            for (int i = 0; i < arrItemUpGrade.Length; i++)
                            {
                                if (arrItemUpGrade[i] != null)
                                {
                                    left = new Command(mResources.BEGIN, 110981);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    left = new Command(mResources.SELECT, 11102);
                }
                else
                {
                    left = null;
                }
            }
        }
        else if (isPaintConvert)
        {
            if (indexMenu == 0)
            {
                if (indexTitle == 1)
                {
                    if (indexSelect == 0)
                    {
                        if (arrItemConvert[0] != null)
                        {
                            left = new Command(mResources.SELECT, 14013);
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                updateItemInfo(3, arrItemConvert[indexSelect]);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 14016);
                            }
                        }
                    }
                    else if (indexSelect == 1)
                    {
                        if (arrItemConvert[1] != null)
                        {
                            left = new Command(mResources.SELECT, 14013);
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                updateItemInfo(3, arrItemConvert[indexSelect]);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 14016);
                            }
                        }
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }
                }
                else if (indexTitle == 2)
                {
                    Item item27 = null;
                    int num = indexSelect + 2;
                    if (num <= arrItemConvert.Length - 1)
                    {
                        item27 = arrItemConvert[num];
                    }

                    if (item27 != null)
                    {
                        left = new Command(mResources.MOVEOUT, 140151);
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(3, item27);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 140161);
                        }
                    }
                    else
                    {
                        left = new Command(mResources.BEGIN, 140131);
                        for (int j = 0; j < arrItemConvert.Length; j++)
                        {
                            if (arrItemConvert[j] == null)
                            {
                                left = null;
                                break;
                            }
                        }

                        isPaintItemInfo = false;
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    left = new Command(mResources.SELECT, 14012);
                }
                else
                {
                    left = null;
                }
            }
        }
        else if (isPaintAuctionSale)
        {
            if (indexMenu == 0)
            {
                if (indexTitle == 2)
                {
                    tfText.isFocus = true;
                    right = tfText.cmdClear;
                }
                else
                {
                    tfText.isFocus = false;
                    right = cmdCloseAll;
                }

                int num2 = 0;
                try
                {
                    num2 = int.Parse(tfText.getText());
                }
                catch (Exception)
                {
                }

                if (itemSell != null && num2 > 0 && Char.getMyChar().xu >= 5000)
                {
                    left = new Command(mResources.SALE, 15002);
                }

                if (indexTitle == 1 && itemSell != null)
                {
                    left = new Command(mResources.SELECT, 1500);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(3, itemSell);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 1501);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                right = cmdCloseAll;
                if (Char.getMyChar().arrItemBag[indexSelect] != null)
                {
                    left = new Command(mResources.SELECT, 1503);
                }
                else
                {
                    left = null;
                    isPaintItemInfo = false;
                }
            }
        }
        else
        {
            if (isPaintAuctionBuy)
            {
                if (indexTitle != 1 || arrItemStands == null || indexSelect < 0 || indexSelect >= arrItemStands.Length)
                {
                    return;
                }

                ItemStands itemStands = arrItemStands[indexSelect];
                if (itemStands != null)
                {
                    left = new Command(mResources.SELECT, 1504);
                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        actionPerform(1505, null);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 1505);
                    }
                }

                return;
            }

            if (isPaintSplit)
            {
                if (indexMenu == 0)
                {
                    if (indexTitle == 1)
                    {
                        if (itemSplit != null && itemSplit.upgrade > 0)
                        {
                            left = new Command(mResources.SELECT, 11103);
                        }
                        else if (itemSplit != null)
                        {
                            left = cmdSplitMoveOut;
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
                    else if (indexTitle == 2)
                    {
                        Item item28 = arrItemSplit[indexSelect];
                        if (item28 != null)
                        {
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                updateItemInfo(3, item28);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, item28);
                            }
                        }
                        else
                        {
                            isPaintItemInfo = false;
                        }

                        if (itemSplit != null && itemSplit.upgrade > 0)
                        {
                            left = new Command(mResources.BEGIN, 11105);
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
                        left = new Command(mResources.SELECT, 11106);
                    }
                }
            }
            else if (isPaintTinhluyen)
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
                        else if (indexTitle == 2)
                        {
                            Item item29 = arrItemSplit[indexSelect];
                            if (item29 != null)
                            {
                                if (GameCanvas.isTouchControlLargeScreen)
                                {
                                    updateItemInfo(3, item29);
                                }
                                else
                                {
                                    center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, item29);
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
                            left = new Command(mResources.SELECT, 11106);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            else if (isPaintDichChuyen)
            {
                if (indexMenu == 0)
                {
                    if (indexTitle == 1)
                    {
                        if (itemSplit != null && itemSplit.upgrade > 11)
                        {
                            left = new Command(mResources.SELECT, 11103);
                        }
                        else if (itemSplit != null)
                        {
                            left = cmdSplitMoveOut;
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
                    else if (indexTitle == 2)
                    {
                        Item item30 = arrItemSplit[indexSelect];
                        if (item30 != null)
                        {
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                updateItemInfo(3, item30);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, item30);
                            }
                        }
                        else
                        {
                            isPaintItemInfo = false;
                        }

                        if (itemSplit != null && itemSplit.upgrade > 11)
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
                        left = new Command(mResources.SELECT, 1606);
                    }
                }
            }
            else if (isPaintUpPearl)
            {
                if (indexMenu == 0)
                {
                    int num3 = 0;
                    for (int k = 0; k < arrItemUpPeal.Length; k++)
                    {
                        if (arrItemUpPeal[k] != null)
                        {
                            num3++;
                            if (num3 >= 2)
                            {
                                break;
                            }
                        }
                    }

                    Item item31 = getItemFocus(11);
                    if (item31 != null)
                    {
                        if (num3 >= 2)
                        {
                            left = new Command(mResources.SELECT, 11107);
                        }
                        else
                        {
                            left = new Command(mResources.MOVEOUT, 111071);
                        }

                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(3, item31);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11108);
                        }
                    }
                    else
                    {
                        isPaintItemInfo = false;
                        if (num3 >= 2)
                        {
                            left = new Command(mResources.BEGIN, 11062);
                        }
                    }
                }
                else if (indexMenu == 1)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect] != null)
                    {
                        left = new Command(mResources.SELECT, 11109);
                    }
                    else
                    {
                        isPaintItemInfo = false;
                        left = null;
                    }
                }
            }
            else if (isPaintLuyenThach)
            {
                if (indexMenu == 0)
                {
                    int num4 = 0;
                    int num5 = 0;
                    int num6 = 0;
                    int num7 = 0;
                    for (int l = 0; l < arrItemUpPeal.Length; l++)
                    {
                        Item item32 = arrItemUpPeal[l];
                        if (item32 != null)
                        {
                            if (item32.template.id == 455)
                            {
                                num4++;
                            }
                            else if (item32.template.id == 456)
                            {
                                num5++;
                            }
                            else if (item32.template.type == 26)
                            {
                                num7 = item32.template.id;
                                num6++;
                            }
                        }

                        if (num4 >= 9 || num5 >= 9 || (num7 == 10 && num4 >= 3) || (num7 == 11 && num5 >= 3))
                        {
                            break;
                        }
                    }

                    Item item33 = getItemFocus(43);
                    if (item33 != null)
                    {
                        if (num4 == 9 || num5 == 9 || (num7 == 10 && num4 == 3 && num6 == 1) || (num7 == 11 && num5 == 3 && num6 == 1))
                        {
                            left = new Command(mResources.SELECT, 1601);
                        }
                        else
                        {
                            left = new Command(mResources.MOVEOUT, 111071);
                        }

                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(43, item33);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 1602);
                        }
                    }
                    else
                    {
                        isPaintItemInfo = false;
                        if (num4 >= 9 || num5 >= 9 || (num7 >= 10 && (num4 >= 3 || num5 >= 3)))
                        {
                            left = new Command(mResources.BEGIN, 1600);
                        }
                    }
                }
                else if (indexMenu == 1)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect] != null)
                    {
                        left = new Command(mResources.SELECT, 1603);
                    }
                    else
                    {
                        isPaintItemInfo = false;
                        left = null;
                    }
                }
            }
            else if (isPaintTrade)
            {
                if (indexMenu == 0)
                {
                    if (indexTitle == 1)
                    {
                        Item item34 = arrItemTradeMe[indexSelect];
                        if (item34 != null)
                        {
                            if (typeTrade == 0)
                            {
                                left = cmdTradeSelectInList;
                            }
                            else if (typeTrade == 1 && typeTradeOrder >= 1 && timeTrade - mSystem.getCurrentTimeMillis() / 1000 <= 0)
                            {
                                left = cmdTradeAccept;
                            }

                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                Item item35 = arrItemTradeMe[indexSelect];
                                updateItemInfo(3, item35);
                            }
                            else
                            {
                                center = cmdTradeViewItemInfo;
                            }
                        }
                        else
                        {
                            isPaintItemInfo = false;
                            if (typeTrade == 0)
                            {
                                left = cmdTradeLock;
                            }
                            else if (typeTrade == 1 && typeTradeOrder >= 1 && timeTrade - mSystem.getCurrentTimeMillis() / 1000 <= 0)
                            {
                                left = cmdTradeAccept;
                            }
                        }
                    }

                    if (indexTitle == 2)
                    {
                        Item item36 = arrItemTradeOrder[indexSelect];
                        if (item36 != null)
                        {
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                Item item37 = arrItemTradeOrder[indexSelect];
                                updateItemInfo(30, item37);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11110);
                            }
                        }
                        else
                        {
                            isPaintItemInfo = false;
                        }
                    }
                }
                else if (indexMenu == 1 && typeTrade == 0)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect] != null)
                    {
                        left = cmdTradeSelectInBag;
                    }
                    else
                    {
                        left = cmdTradeSendMoney;
                    }
                }
            }
            else if (isPaintBox)
            {
                if (indexMenu == 0)
                {
                    Item item38 = getItemFocus(4);
                    if (item38 != null)
                    {
                        string gETOUT = mResources.GETOUT;
                        if (!svTitle.Equals(string.Empty))
                        {
                            gETOUT = svAction;
                        }

                        left = new Command(gETOUT, item38.quantity > 1 ? 900001 : 111101);
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(4, item38);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11111);
                        }
                    }
                    else if (svTitle.Equals(string.Empty))
                    {
                        left = new Command(mResources.SORT, 11112);
                    }
                }
                else if (indexMenu == 1)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect] != null)
                    {
                        left = new Command(mResources.GETIN, Char.getMyChar().arrItemBag[indexSelect].quantity > 1 ? 900003 : 11113);
                    }
                    else
                    {
                        left = cmdBagSortItem;
                    }
                }
            }
            else if (isPaintItemDell)
            {
                if (indexMenu == 0)
                {
                    if (indexSelect >= 0 && indexSelect <= Handle.arrItemDell.Length && Handle.arrItemDell[indexSelect] > 0)
                    {
                        left = new Command(mResources.DELETE, 1234568);
                    }
                    else
                    {
                        left = new Command(mResources.SORT, 1234578);
                    }
                }
                else if (indexMenu == 1)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect] != null)
                    {
                        left = new Command(mResources.ADDNEW, 1234567);
                    }
                    else
                    {
                        left = cmdBagSortItem;
                    }
                }
            }
            else if (isPaintLuyenNgoc)
            {
                setCMD_luyenngoc();
            }
            else if (isPaintKhamNgoc)
            {
                setCMD_khamngoc();
            }
            else if (isPaintGiaoDo)
            {
                setCMD_giaodo();
            }
            else if (isPaintGotNgoc || isPaintThaoNgoc)
            {
                setCMD_gotngoc();
            }
        }

        if (isPaintUI() && indexMenu == 1 && Char.getMyChar().arrItemBag[indexSelect] != null)
        {
            if (GameCanvas.isTouchControlLargeScreen)
            {
                updateItemInfo(3, Char.getMyChar().arrItemBag[indexSelect]);
            }
            else
            {
                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11114);
            }
        }
    }

    public void updateTouchCaptcha()
    {
        if (GameCanvas.isPointer(xL, yL, 34, 34) && GameCanvas.isPointerJustRelease)
        {
            doClickCaptcha(0);
            GameCanvas.clearKeyHold();
        }

        if (GameCanvas.isPointer(xU, yU, 34, 34) && GameCanvas.isPointerJustRelease)
        {
            doClickCaptcha(1);
            GameCanvas.clearKeyHold();
        }

        if (GameCanvas.isPointer(xR - 5, yR, 40, 34) && GameCanvas.isPointerJustRelease)
        {
            doClickCaptcha(2);
            GameCanvas.clearKeyHold();
        }
    }

    public void updateTouchThanThu()
    {
        int num = xSelectThanThu - wSelectThanThu / 2;
        int y = ySelectThanThu - wSelectThanThu / 2;
        int w = (wSelectThanThu + 5) * clan_thanthu.size();
        int h = wSelectThanThu;
        if (GameCanvas.isPointerHoldIn(num, y, w, h))
        {
            int num2 = (GameCanvas.px - num) / (wSelectThanThu + 5);
            if (num2 >= 0 && num2 < clan_thanthu.size())
            {
                indexThanThu = (sbyte)num2;
            }
        }
    }
}
