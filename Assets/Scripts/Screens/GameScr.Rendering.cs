using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    // Original updateFlyText/updateSplash keep tick-based lifetime/velocity; only paint is blended.
    public static void CaptureEffectPaintState()
    {
        if (flyTextX != null)
            for (int i = 0; i < 5; i++)
            {
                flyPaintX[i] = flyTextX[i];
                flyPaintY[i] = flyTextY[i];
            }

        if (splashX != null)
            for (int i = 0; i < 2; i++)
            {
                splashPaintX[i] = splashX[i];
                splashPaintY[i] = splashY[i];
            }
    }

    public static UnityEngine.Vector2 FlyTextPaintPosition(int index)
    {
        return new UnityEngine.Vector2(Interp.EffectPosition(flyPaintX[index], flyTextX[index]), Interp.EffectPosition(flyPaintY[index], flyTextY[index]));
    }

    public override void paint(mGraphics g)
    {
        GameCanvas.debug("PA1", 1);
        if (Char.ischangingMap)
        {
            g.setColor(0);
            g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
            mFont.tahoma_7b_yellow.drawString(g, mResources.LOADING, GameCanvas.hw, GameCanvas.hh + 20, 2);
            GameCanvas.paintShukiren(GameCanvas.hw, GameCanvas.hh, g, false);
            return;
        }

        isPaintInfoHelpNotIphone = false;
        GameCanvas.paintBGGameScr(g);
        // World-only camera residual; dispose before drawing fixed HUD/panels.
        using (g.Offset(Interp.CameraOffset.x, Interp.CameraOffset.y))
        {
            if (Screen.width > TileMap.pxw && mGraphics.zoomLevel != 3 && TileMap.mapID == 6)
            {
                g.translate(-cmx - 40, -cmy);
            }
            else
            {
                g.translate(-cmx, -cmy);
            }

            for (int i = 0; i < vItemTreeBehind.size(); i++)
            {
                ((ItemTree)vItemTreeBehind.elementAt(i)).paint(g);
            }

            TileMap.paintTilemap(g);
            for (int i = 0; i < vItemTreeBetwen.size(); i++)
            {
                ((ItemTree)vItemTreeBetwen.elementAt(i)).paint(g);
            }

            for (int i = 0; i < vMob.size(); i++)
            {
                ((Mob)vMob.elementAt(i))?.paint(g);
            }

            for (int i = 0; i < Mob.vEggMonter.size(); i++)
            {
                ((EggMonters)Mob.vEggMonter.elementAt(i))?.paint(g);
            }

            for (int i = 0; i < vBuNhin.size(); i++)
            {
                ((BuNhin)vBuNhin.elementAt(i))?.paint(g);
            }

            GameCanvas.debug("PA6", 1);
            for (int i = 0; i < vNpc.size(); i++)
            {
                ((Npc)vNpc.elementAt(i))?.paint(g);
            }

            paintWaypointArrow(g);
            GameCanvas.debug("PA7", 1);
            GameCanvas.gI().paintDust(g);
            GameCanvas.debug("PA8", 1);
            for (int i = 0; i < vCharInMap.size(); i++)
            {
                Char @char = null;
                try
                {
                    @char = (Char)vCharInMap.elementAt(i);
                }
                catch (Exception)
                {
                }

                if (@char != null)
                {
                    if (TileMap.mapID == 111 && i > 19)
                    {
                        @char.paintCharName_HP_MP_Overhead(g);
                    }
                    else
                    {
                        @char.paint(g);
                    }
                }
            }

            for (int i = 0; i < vParty.size(); i++)
            {
                Party party = (Party)vParty.elementAt(i);
                if (party.c != null && party.c != Char.getMyChar())
                {
                    party.c.paintNameInSameParty(g);
                }
            }

            GameCanvas.debug("PA9", 1);
            paintFlyText(g);
            paintLanterns(g);
            paintSplash(g);
            GameCanvas.debug("PA10", 1);
            Char.getMyChar().paint(g);
            paintCaptcha(g);
            GameCanvas.debug("PA11", 1);
            for (int i = 0; i < vSet.size(); i++)
            {
                ((Set)vSet.elementAt(i))?.paint(g);
            }

            GameCanvas.debug("PA12", 1);
            for (int i = 0; i < vItemMap.size(); i++)
            {
                ((ItemMap)vItemMap.elementAt(i))?.paint(g);
            }

            for (int i = 0; i < vMobSoul.size(); i++)
            {
                ((MobSoul)vMobSoul.elementAt(i)).paint(g);
            }

            GameCanvas.debug("PA13", 1);
            TileMap.paintOutTilemap(g);
            GameCanvas.debug("PA14", 1);
            for (int i = 0; i < Effect2.vEffect2.size(); i++)
            {
                ((Effect2)Effect2.vEffect2.elementAt(i))?.paint(g);
            }

            for (int i = 0; i < vItemTreeFront.size(); i++)
            {
                ((ItemTree)vItemTreeFront.elementAt(i)).paint(g);
            }

            if (GameCanvas.imgfog1 != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    g.drawImage(GameCanvas.imgfog1, GameCanvas.cloudX[i], 100, 3);
                }

                for (int i = 0; i < TileMap.pxw; i += GameCanvas.imgfog.getWidth())
                {
                    g.drawImage(GameCanvas.imgfog, i, TileMap.pxh, 3);
                }
            }

            if (!GameCanvas.lowGraphic && !Main.isIpod && mGraphics.zoomLevel != 1)
            {
                for (int i = 0; i < Effect2.vAnimateEffect.size(); i++)
                {
                    ((Effect2)Effect2.vAnimateEffect.elementAt(i))?.paint(g);
                }
            }

            GameCanvas.debug("PA15", 1);
            for (int i = 0; i < vMobAttack.size(); i++)
            {
                ((MobAttack)vMobAttack.elementAt(i))?.paint(g);
            }

            GameCanvas.debug("PA16", 1);
            paintArrowPointToNPC(g);
        }

        GameCanvas.debug("PA17", 1);
        if (hideInterface)
        {
            return;
        }

        paintInfoBar(g);
        MainFuctions.Paint(g);
        //mFont.tahoma_7_white.drawString(g, "Time: "+ DateTime.Now.ToString(),GameCanvas.w / 2 , 0 , 3,mFont.tahoma_7_grey);
        if (!Char.getMyChar().isCaptcha)
        {
            paintDebugInfo(g);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            paintFoodEffect(g);
            GameCanvas.debug("PA21", 1);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            TileMap.paintMiniMap(g);
            GameCanvas.debug("PA18", 1);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            if (GameCanvas.isTouch)
            {
                isPaintSelectSkill = true;
            }

            GameCanvas.debug("PA19", 1);
            GameCanvas.debug("PA20", 1);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            paintSelectedSkill(g);
            GameCanvas.debug("PA22", 1);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            for (int i = 0; i < Effect2.vEffect2Outside.size(); i++)
            {
                ((Effect2)Effect2.vEffect2Outside.elementAt(i))?.paint(g);
            }

            g.translate(-g.getTranslateX(), -g.getTranslateY());
            g.setClip(0, -200, GameCanvas.w, 200 + GameCanvas.h);
            if (isPaintInfoMe)
            {
                GameCanvas.debug("PA24", 1);
                paintHanhTrang(g);
                GameCanvas.debug("PA25", 1);
                paintKyNang(g);
                GameCanvas.debug("PA26", 1);
                paintTiemNang(g);
                GameCanvas.debug("PA27", 1);
                paintThongtin(g);
                GameCanvas.debug("PA28", 1);
                paintTrangbi(g);
                GameCanvas.debug("PA29", 1);
                paintThuCuoi(g);
                paintBijuu(g);
            }
            else if (isPaintUI())
            {
                GameCanvas.debug("PA30", 1);
                paintUIStore(g);
                GameCanvas.debug("PA31", 1);
                paintNonNam(g);
                GameCanvas.debug("PA32", 1);
                paintNonNu(g);
                GameCanvas.debug("PA33", 1);
                paintAoNam(g);
                GameCanvas.debug("PA34", 1);
                paintAoNu(g);
                GameCanvas.debug("PA35", 1);
                paintGangTayNam(g);
                GameCanvas.debug("PA36", 1);
                paintGangTayNu(g);
                GameCanvas.debug("PA37", 1);
                paintQuanNam(g);
                GameCanvas.debug("PA38", 1);
                paintQuanNu(g);
                GameCanvas.debug("PA39", 1);
                paintGiayNam(g);
                GameCanvas.debug("PA40", 1);
                paintGiayNu(g);
                GameCanvas.debug("PA41", 1);
                paintLien(g);
                GameCanvas.debug("PA42", 1);
                paintNhan(g);
                GameCanvas.debug("PA43", 1);
                paintNgocBoi(g);
                GameCanvas.debug("PA44", 1);
                paintPhu(g);
                GameCanvas.debug("PA45", 1);
                paintBinhkhi(g);
                paintUIEliteShop(g);
                paintUILuckySprin(g);
                GameCanvas.debug("PA46", 1);
                paintThuoc(g);
                GameCanvas.debug("PA47", 1);
                paintThuocKhoa(g);
                GameCanvas.debug("PA48", 1);
                paintTaphoa(g);
                GameCanvas.debug("PA49", 1);
                paintTaphoakhoa(g);
                GameCanvas.debug("PA50", 1);
                paintCuonghoa(g);
                paintGianHang(g);
                PaintAuctionBuy(g);
                paintChuyenHoa(g);
                GameCanvas.debug("PA51", 1);
                paintTachHT(g);
                paintLuyenThu(g);
                paintLuyenAo(g);
                GameCanvas.debug("PA52", 1);
                paintGhepngoc(g);
                GameCanvas.debug("PA53", 1);
                paintLuyenThach(g);
                paintRuong(g);
                paintItemDell(g);
                GameCanvas.debug("PA54", 1);
                paintItemTrade(g);
                GameCanvas.debug("PA55", 1);
                paintLuyenNgoc(g);
                paintKhamNgoc(g);
                paintGiaoDo(g);
                paintGotNgoc(g);
                paintThaoNgoc(g);
            }
            else if (isPaintZone)
            {
                GameCanvas.debug("PA56", 1);
                paintZone(g);
            }

            paintUIAuto(g);
            GameCanvas.debug("PA57", 1);
            paintTask(g);
            paintCharInMap(g);
            paintTeam(g);
            paintFindTeam(g);
            paintFriend(g);
            paintList(g);
            paintClanInfo(g);
            GameCanvas.debug("PA58", 1);
            paintItemInfo(g);
            paintListRanked(g);
            GameCanvas.debug("PA59", 1);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            if (GameCanvas.isTouch && GameCanvas.w >= 320)
            {
                // Offline presentation deviation from GameScr.paint: wide popup commands follow
                // paintFrame's actual edges, not a separate hard-coded 320-pixel row.
                // Keep narrow layouts unchanged: three original-size buttons would overlap.
                int buttonW = Paint.CmdButtonW;
                int inset = NsoUiLayout.Buttons.popupSideInset;
                int rowX = popupX + inset;
                int rowW = popupW - inset * 2;
                bool alignPopup = (isPaintInfoMe || isPaintUI()) && rowX > 0 && rowX + rowW <= GameCanvas.w && rowW >= buttonW * 3;
                if (left != null && left != menu)
                {
                    left.x = alignPopup ? rowX : GameCanvas.w / 2 - 160;
                    left.y = Paint.CmdY;
                }

                if (center != null)
                {
                    center.x = alignPopup ? rowX + (rowW - buttonW) / 2 : GameCanvas.w / 2 - 35;
                    center.y = Paint.CmdY;
                }

                if (right != null && right != cmdFocus)
                {
                    right.x = alignPopup ? rowX + rowW - buttonW : GameCanvas.w / 2 + 88;
                    right.y = Paint.CmdY;
                }
            }
        }

        base.paint(g);
        if (GameCanvas.isTouch && GameCanvas.isTouchControl)
        {
            paintTouchControl(g);
            isPaintSelectSkill = true;
        }

        resetTranslate(g);
        paintAlert(g);
        resetTranslate(g);
        paintLucky_Draw(g);
        resetTranslate(g);
        Info.paint(g);
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        ChatTextField.gI().paint(g);
        resetTranslate(g);
        InfoMe.paint(g);
    }

    private void paintUIAuto(mGraphics g)
    {
        if (isPaintAuto)
        {
            resetTranslate(g);
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            if (indexTitle == 1)
            {
                g.setColor(Paint.COLORDARK);
                g.fillRect(popupX + 7, popupY + 32, popupW - 14, popupH - 55);
                g.setColor(16777215);
            }
            else
            {
                g.setColor(10249521);
            }

            g.drawRect(popupX + 7, popupY + 32, popupW - 14, popupH - 55);
            paintTitle(g, mResources.ACTIONS[7], false);
            xstart = popupX + 17;
            ystart = popupY + 45;
            indexRowMax = AutoRowCount; // 8 rows
            // Offline change (scroll only, layout unchanged): the scroll item size equals the 30 px row
            // step (was 35, so arrow scrolling drifted and the list scrolled past its end).
            scrMain.setStyle(indexRowMax, AutoRowStep, popupX, popupY + 39, popupW, popupH - 63, true, 1);
            scrMain.setClip(g);
            int num = ystart;
            paintCheckBox(g, mResources.AUTO_TEXT[0], Char.isAHP, Char.aHpValue + "%", xstart, num);
            paintCheckBox(g, mResources.AUTO_TEXT[1], Char.isAMP, Char.aMpValue + "%", xstart, num += 30);
            paintCheckBox(g, mResources.AUTO_TEXT[2], Char.isAFood, Char.aFoodValue + string.Empty, xstart, num += 30);
            paintCheckBox(g, mResources.AUTO_TEXT[3], Char.isABuff, string.Empty, xstart, num += 30);
            paintCheckBox(g, mResources.AUTO_TEXT[4], Char.isAPickYen, string.Empty, xstart, num += 30);
            paintCheckBox(g, mResources.AUTO_TEXT[5], Char.isAPickYHM, string.Empty, xstart, num += 30);
            paintCheckBox(g, mResources.AUTO_TEXT[6], Char.isAPickYHMS, string.Empty, xstart, num += 30);
            paintCheckBox(g, mResources.AUTO_TEXT[7], Char.isANoPick, string.Empty, xstart, num += 30);
            // Offline change (user request, permanent): the extra rows after "Không nhặt gì cả" (reMap, Chuyển map khi hết boss,
            // Tự sát khi hết Mp, Đánh tinh anh, Đánh thủ lĩnh, Auto Reconnect) are no longer shown; their flags are kept.
            if (indexTitle == 1 && indexRow >= 0)
            {
                SmallImage.drawSmallImage(g, 942, xstart - 8, ystart + 2 + indexRow * AutoRowStep, 0, StaticObj.TOP_LEFT);
            }

            resetTranslate(g);
            mFont.tahoma_7_green.drawString(g, (!GameCanvas.isTouch) ? mResources.AUTO_TIP1 : mResources.AUTO_TIP2, popupX + popupW / 2, popupY + popupH - 17, 2);
        }
    }

    private void paintCheckBox(mGraphics g, string title, bool isCheck, string value, int x, int y)
    {
        g.setColor(16777215);
        g.fillRect(x, y, 12, 12);
        if (isCheck)
        {
            g.setColor(9650442);
            // fillRect clips before drawing; rotating drawLine also rotates its GUI clip group.
            for (int pixel = 0; pixel < 8; pixel++)
            {
                g.fillRect(x + 2 + pixel, y + 2 + pixel, 1, 1);
                g.fillRect(x + 2 + pixel, y + 9 - pixel, 1, 1);
            }
        }

        mFont mFont2 = ((!isCheck) ? mFont.tahoma_7_grey : mFont.tahoma_7_white);
        mFont2.drawString(g, title, x + 18, y, 0);
        if (!value.Equals(string.Empty))
        {
            g.setColor(Paint.COLORLIGHT);
            g.fillRect(x + 115, y - 3, 30, 20);
            g.setColor(isCheck ? 16777215 : 0);
            g.drawRect(x + 115, y - 3, 30, 20);
            mFont2.drawString(g, value, x + 133, y + 2, mFont.CENTER);
        }
    }

    private void paintList(mGraphics g)
    {
        if (!isPaintList)
        {
            return;
        }

        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.LIST, false);
        xstart = popupX + 5;
        ystart = popupY + 40;
        if (vList.size() == 0)
        {
            mFont.tahoma_7_white.drawString(g, mResources.EMPTY_INFO, popupX + popupW / 2, popupY + 40, mFont.CENTER);
            return;
        }

        g.setColor(6425);
        g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * 5 + 8);
        resetTranslate(g);
        scrMain.setStyle(vList.size(), indexSize, xstart, ystart, popupW - 3, indexSize * 5 + 4, true, 1);
        if (Main.isPC)
        {
            scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * 5 + 6);
        }
        else
        {
            scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * 4 + 6);
        }

        indexRowMax = vList.size();
        for (int i = 0; i < vList.size(); i++)
        {
            DunItem dunItem = null;
            try
            {
                dunItem = (DunItem)vList.elementAt(i);
            }
            catch (Exception)
            {
            }

            if (dunItem != null)
            {
                if (indexRow == i)
                {
                    g.setColor(Paint.COLORLIGHT);
                    g.fillRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(16777215);
                    g.drawRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                }
                else
                {
                    g.setColor(Paint.COLORBACKGROUND);
                    g.fillRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(13932896);
                    g.drawRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                }

                mFont.tahoma_7_yellow.drawString(g, dunItem.name1, xstart + (popupW - 10) / 2 - popupW / 4, ystart + i * indexSize + indexSize / 2 - 6, 2);
                mFont.tahoma_7b_red.drawString(g, " vs ", xstart + (popupW - 10) / 2, ystart + i * indexSize + indexSize / 2 - 6, 2);
                mFont.tahoma_7_yellow.drawString(g, dunItem.name2, xstart + (popupW - 10) / 2 + popupW / 4, ystart + i * indexSize + indexSize / 2 - 6, 2);
            }
        }

        paintNumCount(g);
    }

    private void paintCharInMap(mGraphics g)
    {
        if (!isPaintCharInMap)
        {
            return;
        }

        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.LIST, false);
        xstart = popupX + 5;
        ystart = popupY + 40;
        int num = ((!Main.isPC) ? 4 : 5);
        if (vCharInMap.size() == 0)
        {
            mFont.tahoma_7_white.drawString(g, mResources.NO_ONE, popupX + popupW / 2, popupY + 40, mFont.CENTER);
            return;
        }

        g.setColor(6425);
        g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * num + 8);
        resetTranslate(g);
        scrMain.setStyle(vCharInMap.size(), indexSize, xstart, ystart, popupW - 3, indexSize * num + 4, true, 1);
        scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * num + 6);
        indexRowMax = vCharInMap.size();
        for (int i = 0; i < vCharInMap.size(); i++)
        {
            Char @char = null;
            try
            {
                @char = (Char)vCharInMap.elementAt(i);
                if (@char.isNhanbanz())
                {
                    continue;
                }
            }
            catch (Exception)
            {
            }

            if (@char != null)
            {
                if (indexRow == i)
                {
                    g.setColor(Paint.COLORLIGHT);
                    g.fillRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(16777215);
                    g.drawRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                }
                else
                {
                    g.setColor(Paint.COLORBACKGROUND);
                    g.fillRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(13932896);
                    g.drawRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                }

                SmallImage.drawSmallImage(g, 647, xstart + 12, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                if (cLastFocusID > 0 && cLastFocusID == @char.charID)
                {
                    mFont.tahoma_7_yellow.drawString(g, @char.cName + " - " + mResources.level + ": " + @char.clevel, xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
                }
                else if (@char.statusMe == 14)
                {
                    mFont.tahoma_7_grey.drawString(g, @char.cName + " - " + mResources.level + ": " + @char.clevel, xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
                }
                else
                {
                    mFont.tahoma_7_green.drawString(g, @char.cName + " - " + mResources.level + ": " + @char.clevel, xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
                }
            }
        }

        paintNumCount(g);
    }

    private void paintFoodEffect(mGraphics g)
    {
        try
        {
            int num = (int)(mSystem.getCurrentTimeMillis() / 1000);
            int num2 = 5;
            if (GameCanvas.isTouch && GameCanvas.isTouchControl)
            {
                num2 = 45 + Info.hI;
            }

            if (GameCanvas.isTouchControlSmallScreen && Char.getMyChar().vSkillFight.size() > 4)
            {
                num2 += 25;
            }

            resetTranslate(g);
            if (vParty.size() <= 0 || GameCanvas.w <= 128 || isPaintUI())
            {
                return;
            }

            num2 -= 18;
            for (int i = 0; i < vParty.size(); i++)
            {
                Party party = (Party)vParty.elementAt(i);
                if (party.c != null)
                {
                    mFont.tahoma_7_white.drawString(g, party.name + "(" + party.c.clevel + ")", GameCanvas.w - 14, num2 += 18, 1, mFont.tahoma_7_grey);
                    party.c.paintHp(g, GameCanvas.w - 41, num2 + 12);
                    SmallImage.drawSmallImage(g, party.iconId, GameCanvas.w - 7, num2 + 9, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
                else
                {
                    num2 += 16;
                    mFont.tahoma_7_green.drawString(g, party.name, GameCanvas.w - 14, num2 + 5, 1, mFont.tahoma_7_grey);
                    SmallImage.drawSmallImage(g, party.iconId, GameCanvas.w - 7, num2 + 11, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void paintWaypointArrow(mGraphics g)
    {
        int num = 10;
        for (int i = 0; i < TileMap.vGo.size(); i++)
        {
            Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
            if (waypoint.minY == 0 || waypoint.maxY >= TileMap.pxh - 24)
            {
                if (waypoint.maxY <= TileMap.pxh / 2)
                {
                    int x = waypoint.minX + (waypoint.maxX - waypoint.minX) / 2;
                    int y = waypoint.minY + (waypoint.maxY - waypoint.minY) / 2 + runArrow;
                    if (GameCanvas.isTouch)
                    {
                        y = waypoint.maxY + (waypoint.maxY - waypoint.minY) + runArrow + num;
                    }

                    SmallImage.drawSmallImage(g, 1213, x, y, 6, StaticObj.VCENTER_HCENTER);
                }
                else if (waypoint.minY >= TileMap.pxh / 2)
                {
                    SmallImage.drawSmallImage(g, 1213, waypoint.minX + (waypoint.maxX - waypoint.minX) / 2, waypoint.minY - 12 - runArrow, 4, StaticObj.VCENTER_HCENTER);
                }
            }
            else if (waypoint.maxX <= TileMap.pxw / 2)
            {
                if (!GameCanvas.isTouch)
                {
                    SmallImage.drawSmallImage(g, 1213, waypoint.maxX + 12 + runArrow, waypoint.maxY - 12, 2, StaticObj.VCENTER_HCENTER);
                }
                else
                {
                    SmallImage.drawSmallImage(g, 1213, waypoint.maxX + 12 + runArrow, waypoint.maxY - 32, 2, StaticObj.VCENTER_HCENTER);
                }
            }
            else if (waypoint.minX >= TileMap.pxw / 2)
            {
                if (!GameCanvas.isTouch)
                {
                    SmallImage.drawSmallImage(g, 1213, waypoint.minX - 12 - runArrow, waypoint.maxY - 12, 0, StaticObj.VCENTER_HCENTER);
                }
                else
                {
                    SmallImage.drawSmallImage(g, 1213, waypoint.minX - 12 - runArrow, waypoint.maxY - 32, 0, StaticObj.VCENTER_HCENTER);
                }
            }
        }
    }

    private void paintArrowPointToNPC(mGraphics g)
    {
        try
        {
            int num = getTaskNpcId();
            if (num == -1)
            {
                return;
            }

            Npc npc = null;
            for (int i = 0; i < vNpc.size(); i++)
            {
                Npc npc2 = (Npc)vNpc.elementAt(i);
                if (npc2.template.npcTemplateId == num)
                {
                    if (npc == null)
                    {
                        npc = npc2;
                    }
                    else if (Res.abs(npc2.cx - Char.getMyChar().cx) < Res.abs(npc.cx - Char.getMyChar().cx))
                    {
                        npc = npc2;
                    }
                }
            }

            if (npc == null || npc.statusMe == 15 || (npc.cx > cmx - 40 && npc.cx < cmx - 40 + gW && npc.cy > cmy && npc.cy < cmy + gH) || GameCanvas.gameTick % 10 < 5)
            {
                return;
            }

            int num2 = npc.cx - Char.getMyChar().cx;
            int num3 = npc.cy - Char.getMyChar().cy;
            int x = 0;
            int y = 0;
            int transform = 0;
            if (num2 > 0 && num3 >= 0)
            {
                if (Res.abs(num2) >= Res.abs(num3))
                {
                    x = gW - 10;
                    y = gH / 2 + 30;
                    if (GameCanvas.isTouch)
                    {
                        y = gH / 2 + 10;
                    }

                    transform = 0;
                }
                else
                {
                    x = gW / 2;
                    y = gH - 10;
                    transform = 5;
                }
            }
            else if (num2 >= 0 && num3 < 0)
            {
                if (Res.abs(num2) >= Res.abs(num3))
                {
                    x = gW - 10;
                    y = gH / 2 + 30;
                    if (GameCanvas.isTouch)
                    {
                        y = gH / 2 + 10;
                    }

                    transform = 0;
                }
                else
                {
                    x = gW / 2;
                    y = 10;
                    transform = 6;
                }
            }

            if (num2 < 0 && num3 >= 0)
            {
                if (Res.abs(num2) >= Res.abs(num3))
                {
                    x = 10;
                    y = gH / 2 + 30;
                    if (GameCanvas.isTouch)
                    {
                        y = gH / 2 + 10;
                    }

                    transform = 3;
                }
                else
                {
                    x = gW / 2;
                    y = gH - 10;
                    transform = 5;
                }
            }
            else if (num2 <= 0 && num3 < 0)
            {
                if (Res.abs(num2) >= Res.abs(num3))
                {
                    x = 10;
                    y = gH / 2 + 30;
                    if (GameCanvas.isTouch)
                    {
                        y = gH / 2 + 10;
                    }

                    transform = 3;
                }
                else
                {
                    x = gW / 2;
                    y = 10;
                    transform = 6;
                }
            }

            resetTranslate(g);
            SmallImage.drawSmallImage(g, 992, x, y, transform, StaticObj.VCENTER_HCENTER);
        }
        catch (Exception)
        {
        }
    }

    private void paintDebugInfo(mGraphics g)
    {
        int num = 0;
        try
        {
            if (GameCanvas.menu.showMenu || InfoDlg.isShow || isPaintPopup())
            {
                return;
            }

            int num2 = -7;
            if (GameCanvas.isTouch)
            {
                num2 = ((!GameCanvas.isKiemduyet) ? (-7) : 16);
            }

            int num3 = 3;
            num = 1;
            if ((!GameCanvas.isTouch || (GameCanvas.isTouch && !GameCanvas.isTouchControl)) && isPaintSelectSkill)
            {
                num3 += 30;
            }

            if (GameCanvas.isTouch)
            {
                num2 += 45 + Info.hI;
                if (GameCanvas.isTouchControlSmallScreen)
                {
                    num2 += 35;
                }
            }

            g.translate(-g.getTranslateX(), -g.getTranslateY());
            if (GameCanvas.isTouchControlLargeScreen)
            {
                int num4 = (int)(mSystem.getCurrentTimeMillis() / 1000);
                int num5 = 13;
                int num6;
                for (int i = 0; i < Char.getMyChar().vEff.size(); i++)
                {
                    Effect effect = (Effect)Char.getMyChar().vEff.elementAt(i);
                    SmallImage.drawSmallImage(g, effect.template.iconId, num3 + num5 + i * num5 * 2, num2 + 27, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                    num6 = effect.timeLenght - (num4 - effect.timeStart);
                    if (num6 >= 0)
                    {
                        mFont.tahoma_7_white.drawString(g, NinjaUtil.getTime(num6), num3 + num5 + i * num5 * 2, num2 + 28, mFont.CENTER, mFont.tahoma_7_grey);
                    }
                }

                num6 = timeLengthMap - (num4 - timeStartMap);
                if (Char.getMyChar().vEff.size() > 0)
                {
                    num2 += 27;
                }

                if (num6 > 0)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.TIME + NinjaUtil.getTime(num6), num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
            }

            if (Char.getMyChar().clevel <= 20)
            {
                if (Char.getMyChar().pPoint > 0)
                {
                    string st = "+" + Char.getMyChar().pPoint + " " + mResources.POTENIAL; // offline fix: missing space
                    mFont.tahoma_7_yellow.drawString(g, st, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }

                if (Char.getMyChar().sPoint > 0)
                {
                    string st2 = "+" + Char.getMyChar().sPoint + " " + mResources.SKILL_PT; // offline fix: missing space
                    mFont.tahoma_7_yellow.drawString(g, st2, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
            }

            if (Handle.T != null)
            {
                xM[0] = num3;
                yM[0] = num2 + 12;
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_red.drawString(g, Handle.T.ToString(), num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
                else
                {
                    mFont.tahoma_7_yellow.drawString(g, Handle.T.ToString(), num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
            }

            if (TileMap.typeMap != TileMap.MAP_CHIENTRUONG)
            {
                num = 2;
                if (Char.getMyChar().isHumanz())
                {
                    if (Char.getMyChar().taskMaint != null)
                    {
                        string text = Char.getMyChar().taskMaint.subNames[Char.getMyChar().taskMaint.index];
                        int num7 = 0;
                        while (text == null)
                        {
                            num7++;
                            text = Char.getMyChar().taskMaint.subNames[Char.getMyChar().taskMaint.index - num7];
                        }

                        if (Char.getMyChar().taskMaint.counts[Char.getMyChar().taskMaint.index] != -1)
                        {
                            string text2 = text;
                            text = text2 + " " + Char.getMyChar().taskMaint.count + "/" + Char.getMyChar().taskMaint.counts[Char.getMyChar().taskMaint.index];
                        }

                        if (GameCanvas.taskTick > 0 && GameCanvas.gameTick % 10 > 4)
                        {
                            mFont.tahoma_7_yellow.drawString(g, text, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                        }
                        else
                        {
                            mFont.tahoma_7_white.drawString(g, text, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                        }
                    }
                    else
                    {
                        sbyte taskMapId = getTaskMapId();
                        if (taskMapId >= 0)
                        {
                            mFont.tahoma_7_white.drawString(g, mResources.TASK_RECEIVE + " " + TileMap.mapNames[taskMapId], num3, num2 += 12, 0, mFont.tahoma_7_grey);
                        }
                    }
                }
            }
            else if (Char.getMyChar().charFocus != null)
            {
                if (Char.getMyChar().charFocus.cTypePk == Char.PK_PHE1)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.WHITE_PARTY, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
                else if (Char.getMyChar().charFocus.cTypePk == Char.PK_PHE2)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.BLACK_PARTY, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
                else if (Char.getMyChar().charFocus.cTypePk == Char.PK_PHE3)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.PHE3_PARTY, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
            }
            else if (Char.getMyChar().mobFocus != null)
            {
                if (Char.getMyChar().mobFocus.templateId == 96)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.BLACK_PARTY, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
                else if (Char.getMyChar().mobFocus.templateId == 97)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.WHITE_PARTY, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
                else if (Char.getMyChar().mobFocus.templateId == 93)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.PHE3_PARTY, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
                else
                {
                    mFont.tahoma_7_white.drawString(g, mResources.NEUTRAL, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                }
            }

            num = 3;
            if (Char.getMyChar().mobFocus != null)
            {
                num = 4;
                MobTemplate template = Char.getMyChar().mobFocus.getTemplate();
                string text3 = template.name + " lv" + Char.getMyChar().mobFocus.level;
                if (Char.getMyChar().mobFocus.templateId != 0 && Char.getMyChar().mobFocus.templateId != 142 && Char.getMyChar().mobFocus.templateId != 143)
                {
                    string text2 = text3;
                    text3 = text2 + ": " + Char.getMyChar().mobFocus.hp + "/" + Char.getMyChar().mobFocus.maxHp;
                }

                g.setColor(Char.getMyChar().mobFocus.getHPColor());
                num2 += 12;
                g.fillRect(num3, num2 + 5, 5, 5);
                g.setColor(0);
                g.drawRect(num3, num2 + 5, 5, 5);
                mFont.tahoma_7_white.drawString(g, text3, num3 + 12, num2, 0, mFont.tahoma_7_grey);
            }
            else if (Char.getMyChar().npcFocus != null)
            {
                num = 5;
                mFont.tahoma_7_yellow.drawString(g, Char.getMyChar().npcFocus.template.name, num3, num2 += 12, 0, mFont.tahoma_7_grey);
            }
            else if (Char.getMyChar().charFocus != null)
            {
                num = 6;
                g.setColor(Char.getMyChar().charFocus.getClassColor());
                num2 += 12;
                g.fillRect(num3, num2 + 5, 5, 5);
                g.setColor(0);
                g.drawRect(num3, num2 + 5, 5, 5);
                mFont.tahoma_7_white.drawString(g, Char.getMyChar().charFocus.cName + " lv" + Char.getMyChar().charFocus.clevel + ": " + Char.getMyChar().charFocus.cHP + "/" + Char.getMyChar().charFocus.cMaxHP, num3 + 12, num2, 0, mFont.tahoma_7_grey);
            }

            if (TileMap.typeMap == TileMap.MAP_DAUTRUONG)
            {
                mFont.tahoma_7_white.drawString(g, mResources.ACHIEVEMENT + ": " + Char.getMyChar().countKill, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                mFont.tahoma_7_white.drawString(g, mResources.MAX + ": " + Char.getMyChar().countKillMax, num3, num2 += 12, 0, mFont.tahoma_7_grey);
            }
            else if (TileMap.typeMap == TileMap.MAP_PB || TileMap.mapID == 114 || TileMap.mapID == 115 || TileMap.mapID == 116)
            {
                mFont.tahoma_7_white.drawString(g, mResources.PROPERTY + ": " + Char.pointPB, num3, num2 += 12, 0, mFont.tahoma_7_grey);
            }
            else if (TileMap.typeMap == TileMap.MAP_CHIENTRUONG)
            {
                mFont.tahoma_7_white.drawString(g, mResources.PROPERTY + ": " + Char.pointChienTruong, num3, num2 += 12, 0, mFont.tahoma_7_grey);
                mFont.tahoma_7_white.drawString(g, mResources.TITLE_CT[Char.getCT()], num3, num2 += 12, 0, mFont.tahoma_7_grey);
            }

            MainFuctions.paintMap(g, num3, num2 += 12);
            g.translate(-g.getTranslateX(), -g.getTranslateY());
        }
        catch (Exception)
        {
            Out.println("paintDebugInfo: line " + num);
        }
    }

    private void paintTouchControl(mGraphics g)
    {
        if (!GameCanvas.isTouch || (GameCanvas.menu.showMenu && GameCanvas.isTouchControlSmallScreen) || GameCanvas.currentDialog != null || ChatPopup.currentMultilineChatPopup != null || GameCanvas.menu.showMenu || isPaintPopup())
        {
            return;
        }

        resetTranslate(g);
        if (!ChatTextField.gI().isShow)
        {
            g.drawImageScale(imgChat, xC, yC, chatIconSize, chatIconSize, 0);
        }

        if (isNotPaintTouchControl())
        {
            return;
        }

        if (!Main.isPC)
        {
            if (!isTouchKey)
            {
                gamePad.paint(g);
            }
            else
            {
                g.drawImage(imgButton, xL, yL, 0);
                g.drawRegion(imgArrow, 0, 0, mGraphics.getImageWidth(imgArrow), mGraphics.getImageHeight(imgArrow), 2, xL + 15, yL + 16, mGraphics.HCENTER | mGraphics.VCENTER);
                if (mScreen.keyTouch == 4)
                {
                    g.drawImage(imgButton2, xL, yL, 0);
                    g.drawRegion(imgArrow2, 0, 0, mGraphics.getImageWidth(imgArrow), mGraphics.getImageHeight(imgArrow), 2, xL + 15, yL + 16, mGraphics.HCENTER | mGraphics.VCENTER);
                }

                g.drawImage(imgButton, xR, yR, 0);
                g.drawRegion(imgArrow, 0, 0, mGraphics.getImageWidth(imgArrow), mGraphics.getImageHeight(imgArrow), 0, xR + 17, yR + 16, mGraphics.HCENTER | mGraphics.VCENTER);
                if (mScreen.keyTouch == 6)
                {
                    g.drawImage(imgButton2, xR, yR, 0);
                    g.drawRegion(imgArrow2, 0, 0, mGraphics.getImageWidth(imgArrow), mGraphics.getImageHeight(imgArrow), 0, xR + 17, yR + 16, mGraphics.HCENTER | mGraphics.VCENTER);
                }

                g.drawImage(imgButton, xU, yU, 0);
                g.drawRegion(imgArrow, 0, 0, mGraphics.getImageWidth(imgArrow), mGraphics.getImageHeight(imgArrow), 7, xU + 17, yU + 14, mGraphics.HCENTER | mGraphics.VCENTER);
                if (mScreen.keyTouch == 3)
                {
                    g.drawImage(imgButton2, xU, yU, 0);
                    g.drawRegion(imgArrow2, 0, 0, mGraphics.getImageWidth(imgArrow), mGraphics.getImageHeight(imgArrow), 7, xU + 17, yU + 14, mGraphics.HCENTER | mGraphics.VCENTER);
                }
            }

            if (Char.getMyChar().ctaskId > 1)
            {
                g.drawImage(imgButton, xTG, yTG, 0);
                if (mScreen.keyTouch == 13)
                {
                    g.drawImage(imgButton2, xTG, yTG, 0);
                }

                g.drawImage(imgFocus, xTG + 16, yTG + 16, mGraphics.HCENTER | mGraphics.VCENTER);
            }

            g.drawImage(imgRight, xF, yF, 0);
            if (mScreen.keyTouch == 5)
            {
                g.drawImage(imgRight2, xF, yF, 0);
            }
        }

        if (isPcLevelSmall())
        {
            if (Main.isPC)
            {
                // PC: the potion buttons sit "NSO UI Layout" > Potion Buttons > Bottom Gap above the screen's bottom
                // (set every frame so Inspector edits show live; input reads the same yHP / yMP)
                int potionH = imgButton != null ? mGraphics.getImageHeight(imgButton) : 34;
                yHP = yMP = GameCanvas.h - potionH - NsoUiLayout.Potions.bottomGap;
            }

            g.drawImage(imgButton, xHP, yHP, 0);
            if (mScreen.keyTouch == 10)
            {
                g.drawImage(imgButton2, xHP, yHP, 0);
            }

            g.drawImage(imgHpp, xHP + 17, yHP + 15, mGraphics.HCENTER | mGraphics.VCENTER);
            if (Main.isPC)
            {
            }

            mFont.number_white.drawString(g, string.Empty + hpPotion, xHP + 30, yHP + 22, 1);
            g.drawImage(imgButton, xMP, yMP, 0);
            if (mScreen.keyTouch == 11)
            {
                g.drawImage(imgButton2, xMP, yMP, 0);
            }

            g.drawImage(imgMpp, xMP + 17, yMP + 15, mGraphics.HCENTER | mGraphics.VCENTER);
            if (Main.isPC)
            {
            }

            mFont.number_white.drawString(g, string.Empty + mpPotion, xMP + 30, yMP + 22, 1);
        }
    }

    private void paintInfoBar(mGraphics g)
    {
        if (GameCanvas.isTouch)
        {
            resetTranslate(g);
            loadInforBar();
            int num = Char.getMyChar().cHP * hpBarW / Char.getMyChar().cMaxHP;
            int w = Char.getMyChar().cMP * mpBarW / Char.getMyChar().cMaxMP;
            int w2 = (int)(Char.getMyChar().cExpR * expBarW / exps[Char.getMyChar().clevel]);
            if (num > hpBarW)
            {
                num = 0;
            }

            g.setColor(6191872);
            g.fillRect(0, hpBarY - 10, expBarW, 3);
            g.setColor(6350080);
            g.fillRect(0, hpBarY - 10, w2, 3);
            g.setColor(7020544);
            g.fillRect(0, hpBarY - 10, expBarW, 1);
            g.fillRect(0, hpBarY - 7, expBarW, 1);
            for (int i = 0; i < 10; i++)
            {
                g.fillRect(i * expBarW / 10 - 1, hpBarY - 10, 1, 3);
            }

            g.setColor(0, 0.8f);
            g.fillRect(hpBarX - 1, hpBarY, wColoerMp, hColorMp);
            g.setColor(15007764);
            g.fillRect(hpBarX, hpBarY, num, 9);
            g.setColor(0, 0.8f);
            g.fillRect(hpBarX - 28, hpBarY + 13, wColoerMp, hColorMp);
            g.setColor(21989);
            g.fillRect(hpBarX, hpBarY + 16, w, 7);
            g.drawImage(imgTopBar, 0, hpBarY - 7, 0);
            TextCalib.UseBox("hp", hpBarX + 9.67f, hpBarY + 4.67f);
            mFont.number_white.drawString(g, string.Empty + Char.getMyChar().cHP, 0, 0, 0);
            TextCalib.Clear();
            TextCalib.UseBox("mp", hpBarX + 9.67f, hpBarY + 19.33f);
            mFont.number_white.drawString(g, string.Empty + Char.getMyChar().cMP, 0, 0, 0);
            TextCalib.Clear();
            // Logical pixels: keep EXP on the badge axis and optically balance the heavier level font.
            float badgeCenterX = hpBarX - 28.17f;
            TextCalib.UseBox("level", badgeCenterX - 1f, hpBarY + 12.33f);
            mFont.bigNumber_yellow.drawString(g, string.Empty + Char.getMyChar().clevel, 0, 0, mFont.CENTER);
            TextCalib.Clear();
            long num2 = 0L;
            num2 = ((Char.getMyChar().cExpDown <= 0) ? (Char.getMyChar().cExpR * 10000 / exps[Char.getMyChar().clevel]) : (Char.getMyChar().cExpDown * 10000 / exps[Char.getMyChar().clevel]));
            int num3 = (int)(num2 % 100);
            TextCalib.UseBox("exp", badgeCenterX, hpBarY + 22f);
            mFont.number_white.drawString(g, ((Char.getMyChar().cExpDown <= 0) ? (string.Empty + num2 / 100) : ("-" + num2 / 100)) + "." + ((num3 >= 10) ? (string.Empty + num3) : ("0" + num3)) + "%", 0, 0, mFont.CENTER);
            TextCalib.Clear();
        }
    }

    private void paintSelectedSkill(mGraphics g)
    {
        if (GameCanvas.currentDialog != null || ChatPopup.currentMultilineChatPopup != null || GameCanvas.menu.showMenu || isPaintPopup() || center == cmdDead || (GameCanvas.isTouch && !hasBookSkill()) || !isPaintSelectSkill) // offline (user request): was vSkill.size() < 2
        {
            return;
        }

        // Offline change (user request): on PC the bar sits on the same row as the HP / MP potion buttons
        // (vertically centred on them; same formula as paintTouchControl's potion yHP).
        int pcBarY = -1;
        if (Main.isPC && !GameCanvas.isTouchControlSmallScreen)
        {
            int potionH = imgButton != null ? mGraphics.getImageHeight(imgButton) : 34;
            int slotH = imgSkill != null ? mGraphics.getImageHeight(imgSkill) : 26;
            pcBarY = GameCanvas.h - potionH - NsoUiLayout.Potions.bottomGap + (potionH - slotH) / 2 + 1;
        }

        for (int i = 0; i < onScreenSkill.Length; i++)
        {
            if (pcBarY >= 0)
            {
                yS[i] = pcBarY; // input (updateKeyTouch hit test) reads the same yS
            }

            if (GameCanvas.isTouchControlSmallScreen)
            {
                if (Info.hI > 0)
                {
                    yS[i] = 55 + Info.hI;
                }
                else
                {
                    yS[i] = 55;
                }
            }

            if (GameCanvas.isTouch && GameCanvas.isTouchControl)
            {
                g.drawImage(imgSkill, xSkill + xS[i] - 1, yS[i] - 1, 0);
            }
            else
            {
                g.setColor(16764040);
                g.drawRect(xSkill + xS[i] - 1, yS[i] - 1, 25, 25);
            }

            Skill skill = onScreenSkill[i];
            if (i == selectedIndexSkill && !isPaintUI() && GameCanvas.gameTick % 10 > 5)
            {
                g.setColor(16777215);
                g.fillRect(xSkill + xS[i] + 1, yS[i] + 1, 22, 22);
            }
            else if (!GameCanvas.isTouch)
            {
                g.setColor(0);
                g.fillRect(xSkill + xS[i], yS[i], 24, 24);
            }

            if (skill == null)
            {
                continue;
            }

            if (skill == Char.getMyChar().myskill)
            {
                g.setColor(16711680);
                g.drawRect(xSkill + xS[i] + 2, yS[i] + 2, 19, 19);
            }

            skill.paint(xSkill + xS[i] + 12, yS[i] + 12, g, false); // offline (user request): no cooldown seconds
            if (false && Main.isPC) // offline (user request): the slot index numbers are hidden
            {
                int num = i + ((!GameCanvas.isChangMapKey) ? 1 : 6);
                if (num >= 10)
                {
                    num = 0;
                }

                mFont.numberb_white.drawString(g, num + string.Empty, xSkill + xS[i] + 11, yS[i] - mFont.numberb_white.getHeight() - 2, 0);
            }
        }
    }

    public void paintOpen(mGraphics g)
    {
        if (isstarOpen)
        {
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            g.fillRect(0, 0, GameCanvas.w, moveUp);
            g.setColor(10275899);
            g.fillRect(0, moveUp - 1, GameCanvas.w, 1);
            g.fillRect(0, moveDow + 1, GameCanvas.w, 1);
        }
    }

    public static void paintFlyText(mGraphics g)
    {
        for (int i = 0; i < 5; i++)
        {
            if (flyTextState[i] != -1 && GameCanvas.isPaint(flyTextX[i], flyTextY[i]))
            {
                UnityEngine.Vector2 position = FlyTextPaintPosition(i);
                using (g.Offset(position.x - flyTextX[i], position.y - flyTextY[i]))
                {
                    if (flyTextColor[i] == mFont.RED)
                    {
                        mFont.bigNumber_red.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                    else if (flyTextColor[i] == mFont.YELLOW)
                    {
                        mFont.bigNumber_yellow.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                    else if (flyTextColor[i] == mFont.GREEN)
                    {
                        mFont.bigNumber_green.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                    else if (flyTextColor[i] == mFont.FATAL)
                    {
                        mFont.bigNumber_red.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                    else if (flyTextColor[i] == mFont.FATAL_ME)
                    {
                        mFont.bigNumber_While.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER, mFont.numberb_red);
                    }
                    else if (flyTextColor[i] == mFont.MISS)
                    {
                        mFont.bigNumber_blue.drawString(g, "Né", flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                    else if (flyTextColor[i] == mFont.ORANGE)
                    {
                        mFont.bigNumber_orange.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                    else if (flyTextColor[i] == mFont.ADDMONEY)
                    {
                        mFont.tahoma_7_yellow.drawString(g, flyTextString[i], flyTextX[i], flyTextY[i], mFont.CENTER, mFont.tahoma_7_red);
                    }
                    else if (flyTextColor[i] == mFont.MISS_ME)
                    {
                        mFont.bigNumber_red.drawString(g, "Né", flyTextX[i], flyTextY[i], mFont.CENTER);
                    }
                }
            }
        }
    }

    public static void paintLanterns(mGraphics g)
    {
        for (int i = 0; i < vLanterns.size(); i++)
        {
            ((Lanterns)vLanterns.elementAt(i)).paint(g);
        }
    }

    public static void paintSplash(mGraphics g)
    {
        for (int i = 0; i < 2; i++)
        {
            if (splashState[i] != -1)
            {
                using (g.Offset(Interp.EffectPosition(splashPaintX[i], splashX[i]) - splashX[i], Interp.EffectPosition(splashPaintY[i], splashY[i]) - splashY[i]))
                {
                    if (splashDir[i] == 1)
                    {
                        g.drawImage(imgSplash[splashF[i]], splashX[i], splashY[i], 3);
                    }
                    else
                    {
                        g.drawRegion(imgSplash[splashF[i]], 0, 0, mGraphics.getImageWidth(imgSplash[splashF[i]]), mGraphics.getImageHeight(imgSplash[splashF[i]]), 2, splashX[i], splashY[i], 3);
                    }
                }
            }
        }
    }

    public void paintTrangbi(mGraphics g)
    {
        if (indexMenu != 4)
        {
            return;
        }

        g.translate(-g.getTranslateX(), -g.getTranslateY());
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        g.setColor(Paint.COLORBACKGROUND);
        paintTitle(g, mResources.MENUME[indexMenu], true);
        if (currentCharViewInfo.arrItemBody == null)
        {
            GameCanvas.paintShukiren(popupX + 90, popupY + 75, g, false);
            mFont.tahoma_7b_white.drawString(g, mResources.PLEASEWAIT, popupX + popupW / 2, popupY + 90, 2);
            return;
        }

        g.setColor(13606712);
        g.drawRect(popupX + 33, popupY + ((!GameCanvas.isTouchControlLargeScreen) ? 34 : 87), popupW - 67, (!GameCanvas.isTouchControlLargeScreen) ? 128 : 76);
        int num = indexSize - 2;
        int num2 = 0;
        for (int i = 0; i < 16; i++)
        {
            switch (i)
            {
                case 0:
                case 2:
                case 4:
                case 6:
                case 8:
                    g.setColor(0);
                    g.fillRect(popupX + 4 + 1, popupY + 35 + i / 2 * num + 1, num - 1, num - 1);
                    if (indextabTrangbi == 0)
                    {
                        if (mResources.ITEMNAME[i].Length > 1)
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][0], popupX + 7 + 11, popupY + 36 + i / 2 * num + 2, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][1], popupX + 7 + 11, popupY + 36 + i / 2 * num + 2 + 9, 2);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][0], popupX + 7 + 11, popupY + 36 + i / 2 * num + 2 + 5, 2);
                        }
                    }

                    continue;
                case 1:
                case 3:
                case 5:
                case 7:
                case 9:
                    g.setColor(0);
                    g.fillRect(popupX + popupW - num - 4, popupY + 35 + i / 2 * num + 1, num - 1, num - 1);
                    if (indextabTrangbi == 0)
                    {
                        if (mResources.ITEMNAME[i].Length > 1)
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][0], popupX + popupW - num / 2 - 4, popupY + 36 + i / 2 * num + 2, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][1], popupX + popupW - num / 2 - 4, popupY + 36 + i / 2 * num + 2 + 9, 2);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][0], popupX + popupW - num / 2 - 4, popupY + 36 + i / 2 * num + 2 + 5, 2);
                        }
                    }

                    continue;
            }

            if (i != 9 && i != 10 && i != 11 && i != 12 && i != 13 && i != 14 && i != 15)
            {
                continue;
            }

            int num3 = popupX + 4 + 1 + num2 * (num + 2);
            int num4 = popupY + 35 + 5 * num + 1;
            g.setColor(0);
            g.fillRect(num3, popupY + 35 + 5 * num + 1, num - 1, num - 1);
            if (indextabTrangbi == 0)
            {
                if (mResources.ITEMNAME[i].Length > 1)
                {
                    mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][0], num3 + num / 2, num4 + 2, 2);
                    mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][1], num3 + num / 2, num4 + 2 + 9, 2);
                }
                else
                {
                    mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[i][0], num3 + num / 2, num4 + 2 + 5, 2);
                }
            }

            num2++;
        }

        if (indextabTrangbi == 0)
        {
            for (int j = 0; j < 16; j++)
            {
                Item item = currentCharViewInfo.arrItemBody[j];
                if (item == null)
                {
                    continue;
                }

                if (item.eff == null)
                {
                    item.eff = efs[56];
                }

                if (item.indexUI == 0 || item.indexUI == 2 || item.indexUI == 4 || item.indexUI == 6 || item.indexUI == 8 || item.indexUI == 10)
                {
                    int num5 = popupX + 4;
                    int y = popupY + 34 + item.indexUI / 2 * num;
                    paintItem(g, item, num5 - 1, y, 0, 1);
                }
                else if (item.indexUI == 1 || item.indexUI == 3 || item.indexUI == 5 || item.indexUI == 7 || item.indexUI == 9)
                {
                    int num6 = popupX + popupW - num - 5;
                    int num7 = popupY + 35 + item.indexUI / 2 * num;
                    paintItem(g, item, num6 - 1, num7 - 1, 0, 1);
                }
                else if (item.indexUI == 11 || item.indexUI == 12 || item.indexUI == 13 || item.indexUI == 14 || item.indexUI == 15)
                {
                    if (item.indexUI == 10)
                    {
                        num2 = 1;
                    }
                    else if (item.indexUI == 11)
                    {
                        num2 = 2;
                    }
                    else if (item.indexUI == 12)
                    {
                        num2 = 3;
                    }
                    else if (item.indexUI == 13)
                    {
                        num2 = 4;
                    }
                    else if (item.indexUI == 14)
                    {
                        num2 = 5;
                    }
                    else if (item.indexUI == 15)
                    {
                        num2 = 6;
                    }

                    int num8 = popupX + 2 + 1 + num2 * (num + 2) - num;
                    int num9 = popupY + 35 + 5 * num;
                    paintItem(g, item, num8 - 2, num9 - 1, 0, 1);
                }

                if (GameCanvas.gameTick % 4 == 0)
                {
                    item.indexEff++;
                    if (item.indexEff >= item.eff.arrEfInfo.Length)
                    {
                        item.indexEff = 0;
                    }
                }
            }
        }

        if (indextabTrangbi > 0)
        {
            for (int k = 0; k < 16; k++)
            {
                Item item2 = currentCharViewInfo.arrItemBody[k + indextabTrangbi];
                if (item2 == null)
                {
                    continue;
                }

                int num10 = item2.indexUI - 16;
                if (item2.eff == null)
                {
                    item2.eff = efs[56];
                }

                switch (num10)
                {
                    case 0:
                    case 2:
                    case 4:
                    case 6:
                    case 8:
                    case 10:
                    {
                        int num13 = popupX + 4;
                        int y2 = popupY + 34 + num10 / 2 * num;
                        paintItem(g, item2, num13 - 1, y2, 0, 1);
                        break;
                    }

                    case 1:
                    case 3:
                    case 5:
                    case 7:
                    case 9:
                    {
                        int num14 = popupX + popupW - num - 5;
                        int num15 = popupY + 35 + num10 / 2 * num;
                        paintItem(g, item2, num14 - 1, num15 - 1, 0, 1);
                        break;
                    }

                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    {
                        switch (num10)
                        {
                            case 10:
                                num2 = 1;
                                break;
                            case 11:
                                num2 = 2;
                                break;
                            case 12:
                                num2 = 3;
                                break;
                            case 13:
                                num2 = 4;
                                break;
                            case 14:
                                num2 = 5;
                                break;
                            case 15:
                                num2 = 6;
                                break;
                        }

                        int num11 = popupX + 2 + 1 + num2 * (num + 2) - num;
                        int num12 = popupY + 35 + 5 * num;
                        paintItem(g, item2, num11 - 2, num12 - 1, 0, 1);
                        break;
                    }
                }

                if (GameCanvas.gameTick % 4 == 0)
                {
                    item2.indexEff++;
                    if (item2.indexEff >= item2.eff.arrEfInfo.Length)
                    {
                        item2.indexEff = 0;
                    }
                }
            }
        }

        for (int l = 0; l < 16; l++)
        {
            if (indexTitle != 1 || l != indexSelect)
            {
                continue;
            }

            switch (l)
            {
                case 0:
                case 2:
                case 4:
                case 6:
                case 8:
                    g.setColor(16777215);
                    g.drawRect(popupX + 4, popupY + 35 + l / 2 * num, num, num);
                    paintSelectHighlight(popupX + 5 - 2, popupY + 35 + l / 2 * num - 1, g);
                    continue;
                case 1:
                case 3:
                case 5:
                case 7:
                case 9:
                    g.setColor(16777215);
                    g.drawRect(popupX + popupW - num - 4 - 1, popupY + 35 + l / 2 * num, num, num);
                    paintSelectHighlight(popupX + popupW - num - 4 - 2, popupY + 35 + l / 2 * num - 1, g);
                    continue;
            }

            if (l == 9 || l == 10 || l == 11 || l == 12 || l == 13 || l == 14 || l == 15)
            {
                switch (l)
                {
                    case 9:
                        num2 = 0;
                        break;
                    case 10:
                        num2 = 1;
                        break;
                    case 11:
                        num2 = 2;
                        break;
                    case 12:
                        num2 = 3;
                        break;
                    case 13:
                        num2 = 4;
                        break;
                    case 14:
                        num2 = 5;
                        break;
                    case 15:
                        num2 = 6;
                        break;
                }

                int num16 = popupX + 2 + 1 + num2 * (num + 2) - num;
                int num17 = popupY + 35 + 5 * num;
                g.setColor(16777215);
                g.drawRect(num16 - 1, num17, num, num);
                paintSelectHighlight(num16 - 2, num17 - 1, g);
            }
        }

        int num18 = ((!GameCanvas.isTouchControlLargeScreen) ? 16 : (-25));
        Part part = parts[currentCharViewInfo.head];
        Part part2 = parts[currentCharViewInfo.leg];
        Part part3 = parts[currentCharViewInfo.body];
        Part part4 = parts[currentCharViewInfo.wp];
        if (currentCharViewInfo.arrItemBody != null && currentCharViewInfo.arrItemBody[11] != null)
        {
            part = parts[currentCharViewInfo.arrItemBody[11].template.part];
        }

        if (part.pi == null || part.pi.Length < 8)
        {
            part = Char.getMyChar().getDefaultHead(Char.getMyChar().cgender);
        }
        else
        {
            for (int m = 0; m < part.pi.Length; m++)
            {
                if (part.pi[m] == null || !SmallImage.isExitsImage(part.pi[m].id))
                {
                    part = Char.getMyChar().getDefaultHead(Char.getMyChar().cgender);
                    break;
                }
            }
        }

        int[] classCoat = currentCharViewInfo.getClassCoat();
        int num19 = 13;
        int num20 = 24;
        int f = ((currentCharViewInfo.cp1 % 15 >= 5) ? 1 : 0);
        if (currentCharViewInfo.ID_WEA_PONE > -1)
        {
            currentCharViewInfo.paintWeaponUIBot(g, gW2, gH2 - num20, f);
        }

        if (currentCharViewInfo.ID_PP > -1)
        {
            currentCharViewInfo.paintPPUIBot(g, gW2, gH2 - num20, f);
        }

        if (classCoat != null && currentCharViewInfo.ID_PP == -1)
        {
            if (Char.getMyChar().tickCoat == 0)
            {
                SmallImage.drawSmallImage(g, classCoat[Char.getMyChar().tickCoat], gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][1] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dx - 2, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][2] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dy + 16, 0, 0);
            }
            else if (Char.getMyChar().tickCoat == 1)
            {
                SmallImage.drawSmallImage(g, classCoat[Char.getMyChar().tickCoat], gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][1] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dx - 9, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][2] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dy + 16, 0, 0);
            }
            else if (Char.getMyChar().tickCoat == 2)
            {
                SmallImage.drawSmallImage(g, classCoat[Char.getMyChar().tickCoat], gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][1] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dx - 12, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][2] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dy + 16, 0, 0);
            }
            else
            {
                SmallImage.drawSmallImage(g, classCoat[Char.getMyChar().tickCoat], gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][1] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dx - 9, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][2] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dy + 16, 0, 0);
            }
        }

        currentCharViewInfo.paintClanEffect(g, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][1] + part3.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][0]].dx + 18, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][2] + part2.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][0]].dy + 5);
        if (currentCharViewInfo.ID_WEA_PONE == -1)
        {
            SmallImage.drawSmallImage(g, part4.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][3][0]].id, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][3][1] + part4.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][3][0]].dx, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][3][2] + part4.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][3][0]].dy, 0, 0);
        }

        if (currentCharViewInfo.ID_LEG > -1)
        {
            currentCharViewInfo.paintLegUI(g, gW2, gH2 - num20, f);
        }
        else
        {
            SmallImage.drawSmallImage(g, part2.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][0]].id, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][1] + part2.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][0]].dx, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][2] + part2.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][0]].dy, 0, 0);
        }

        if (currentCharViewInfo.ID_Body > -1)
        {
            currentCharViewInfo.paintBodyUI(g, gW2, gH2 - num20, f);
        }
        else
        {
            SmallImage.drawSmallImage(g, part3.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][0]].id, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][1] + part3.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][0]].dx, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][2] + part3.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][0]].dy, 0, 0);
        }

        if (currentCharViewInfo.ID_HAIR > -1)
        {
            currentCharViewInfo.paintHeadUI(g, gW2, gH2 - num20, f);
        }
        else
        {
            SmallImage.drawSmallImage(g, part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].id, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][1] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dx, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][2] + part.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][0][0]].dy, 0, 0);
        }

        currentCharViewInfo.paintClanEffect(g, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][1] + part3.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][0]].dx + 5, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][2] + part2.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][0]].dy + 5);
        currentCharViewInfo.paintClanEffect2(g, gW2 + Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][1] + part3.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][2][0]].dx + 22, gH2 + num18 - Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][2] + part2.pi[Char.CharInfo[(currentCharViewInfo.cp1 % 15 >= 5) ? 1u : 0u][1][0]].dy + 5);
        if (currentCharViewInfo.ID_PP > -1)
        {
            currentCharViewInfo.paintPPUITop(g, gW2, gH2 - num20, f);
        }

        if (currentCharViewInfo.ID_WEA_PONE > -1)
        {
            currentCharViewInfo.paintWeaponUITop(g, gW2, gH2 - num20, f);
        }

        if (cmdTrangbi2 != null && GameCanvas.isTouch)
        {
            cmdTrangbi2.paint(g);
        }
    }

    public void paintThongtin(mGraphics g)
    {
        if (indexMenu != 3)
        {
            return;
        }

        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        if (indexTitle == 1)
        {
            g.setColor(Paint.COLORDARK);
            g.fillRect(popupX + 7, popupY + 32, popupW - 14, popupH - 40);
            g.setColor(16777215);
        }
        else
        {
            g.setColor(10249521);
        }

        g.drawRect(popupX + 7, popupY + 32, popupW - 14, popupH - 40);
        paintTitle(g, mResources.MENUME[indexMenu], true);
        xstart = popupX + 17;
        ystart = popupY + 34;
        scrMain.setStyle(indexRowMax, 12, popupX, popupY + 35, popupW, popupH - 44, true, 1);
        scrMain.setClip(g);
        if (typeViewInfo == 0)
        {
            indexRowMax = 19;
            int num = ystart;
            if (currentCharViewInfo == null)
            {
                return;
            }

            mFont.tahoma_7b_white.drawString(g, mResources.inforMe[0] + currentCharViewInfo.cName, xstart, num, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[1] + currentCharViewInfo.cPk, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[2] + currentCharViewInfo.clevel, xstart, num += 12, 0);
            mFont.tahoma_7_blue1.drawString(g, mResources.inforMe[3] + currentCharViewInfo.nClass.name, xstart, num += 12, 0);
            mFont.tahoma_7_blue1.drawString(g, mResources.inforMe[4] + mResources.SYS[currentCharViewInfo.getSys()], xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[5] + currentCharViewInfo.cHP + "/" + currentCharViewInfo.cMaxHP, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[6] + currentCharViewInfo.cMP + "/" + currentCharViewInfo.cMaxMP, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[7] + currentCharViewInfo.getSpeed(), xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[8] + (currentCharViewInfo.cdame - currentCharViewInfo.cdame / 10) + "-" + currentCharViewInfo.cdame, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[9] + currentCharViewInfo.cResFire, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[10] + currentCharViewInfo.cResIce, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[11] + currentCharViewInfo.cResWind, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[12] + currentCharViewInfo.cdameDown, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[13] + currentCharViewInfo.cExactly, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[14] + currentCharViewInfo.cMiss, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[15] + currentCharViewInfo.cFatal, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[16] + currentCharViewInfo.cReactDame, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[17] + currentCharViewInfo.sysUp, xstart, num += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMe[18] + currentCharViewInfo.sysDown, xstart, num += 12, 0);
        }
        else if (typeViewInfo == 1)
        {
            indexRowMax = 20;
            int num2 = ystart;
            if (currentCharViewInfo == null)
            {
                return;
            }

            mFont.tahoma_7b_white.drawString(g, mResources.inforMePoint[15] + ((!currentCharViewInfo.cClanName.Equals(string.Empty)) ? currentCharViewInfo.cClanName : mResources.NO_CLAN), xstart, num2, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[0] + currentCharViewInfo.pointUydanh, xstart, num2 += 12, 0);
            mFont.tahoma_7_blue1.drawString(g, mResources.inforMePoint[11] + currentCharViewInfo.countFinishDay + "/20", xstart, num2 += 12, 0);
            mFont.tahoma_7_blue1.drawString(g, mResources.inforMePoint[12] + currentCharViewInfo.countLoopBoos + mResources.NUMBER, xstart, num2 += 12, 0);
            mFont.tahoma_7_blue1.drawString(g, mResources.inforMePoint[16] + currentCharViewInfo.countPB + mResources.NUMBER, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[13] + currentCharViewInfo.limitTiemnangso, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[14] + currentCharViewInfo.limitKynangso, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[18] + currentCharViewInfo.limitPhongLoi + mResources.NUMBER, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[19] + currentCharViewInfo.limitBangHoa + mResources.NUMBER, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[17] + currentCharViewInfo.pointTinhTu, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[1] + currentCharViewInfo.pointVukhi, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[2] + currentCharViewInfo.pointLien, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[3] + currentCharViewInfo.pointNhan, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[4] + currentCharViewInfo.pointNgocboi, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[5] + currentCharViewInfo.pointPhu, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[6] + currentCharViewInfo.pointNon, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[7] + currentCharViewInfo.pointAo, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[8] + currentCharViewInfo.pointGangtay, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[9] + currentCharViewInfo.pointQuan, xstart, num2 += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.inforMePoint[10] + currentCharViewInfo.pointGiay, xstart, num2 += 12, 0);
        }

        if (indexTitle == 1 && indexRow >= 0)
        {
            SmallImage.drawSmallImage(g, 942, xstart - 8, ystart + 2 + indexRow * 12, 0, StaticObj.TOP_LEFT);
        }
    }

    public void paintThuCuoi(mGraphics g)
    {
        if (indexMenu != 5)
        {
            return;
        }

        g.translate(-g.getTranslateX(), -g.getTranslateY());
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        g.setColor(Paint.COLORBACKGROUND);
        paintTitle(g, mResources.MENUME[indexMenu], true);
        resetTranslate(g);
        g.setColor(0);
        g.fillRect(popupX + 2, popupY + 31, 171, popupH - 34);
        g.setColor(13606712);
        g.drawRect(popupX + 3, popupY + 32, 168, popupH - 37);
        g.setColor(Paint.COLORBACKGROUND);
        g.fillRect(popupX + 4, popupY + 34, 166, popupH - 39);
        if (currentCharViewInfo.arrItemMounts[4] != null)
        {
            mFont.tahoma_7b_white.drawString(g, currentCharViewInfo.arrItemMounts[4].template.name, popupX + 90, ystart + 2, 2);
            int num = currentCharViewInfo.arrItemMounts[4].sys + 1;
            for (int i = 0; i < num; i++)
            {
                SmallImage.drawSmallImage(g, 628, popupX + 90 + i * 12 - num * 6, ystart + 20, 0, mGraphics.HCENTER | mGraphics.VCENTER);
            }
        }
        else
        {
            mFont.tahoma_7b_white.drawString(g, mResources.MON_NAME, popupX + 90, ystart + 2, 2);
        }

        for (int j = 0; j < currentCharViewInfo.arrItemMounts.Length - 1; j++)
        {
            if (currentCharViewInfo.arrItemMounts[j] != null)
            {
                paintItem(g, currentCharViewInfo.arrItemMounts[j], xMounts[j], yMounts[j]);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(xMounts[j] - 1, yMounts[j] - 1, indexSize + 3, indexSize + 3);
                switch (j)
                {
                    case 0:
                        if (currentCharViewInfo.isHaveMoto())
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[22][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[22][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[19][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[19][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }

                        break;
                    case 1:
                        if (currentCharViewInfo.isHaveMoto())
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[20][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[20][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[16][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[16][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }

                        break;
                    case 2:
                        if (currentCharViewInfo.isHaveMoto())
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[21][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[21][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[17][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[17][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }

                        break;
                    case 3:
                        if (currentCharViewInfo.isHaveMoto())
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[23][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[23][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[18][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                            mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[18][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
                        }

                        break;
                }
            }

            if (indexSelect == j && indexTitle == 1 && indexSelect < 4)
            {
                g.setColor(16777215);
            }
            else
            {
                g.setColor(12281361);
            }

            g.drawRect(xMounts[j], yMounts[j], indexSize, indexSize);
        }

        int num2 = xMounts[0] + indexSize + 7;
        int num3 = yMounts[0] - 5;
        g.setColor(6425);
        g.fillRect(num2, num3, 84, 75);
        if (indexSelect == 4)
        {
            g.setColor(16777215);
        }
        else
        {
            g.setColor(12281361);
        }

        g.drawRect(num2, num3, 84, 75);
        int num4 = 0;
        int num5 = 0;
        int w = 0;
        int w2 = 0;
        int num6 = 0;
        if (currentCharViewInfo.ID_HORSE > -1)
        {
            currentCharViewInfo.paintHorseUI(g, num2 + 35, num3 + 55);
        }

        if (currentCharViewInfo.arrItemMounts[4] != null)
        {
            if (currentCharViewInfo.isHaveNewMount())
            {
                int[][] array = (int[][])CharPartInfo.newMount.get(currentCharViewInfo.arrItemMounts[4].template.id + string.Empty);
                if (GameCanvas.gameTick % 20 > 15)
                {
                    SmallImage.drawSmallImage(g, array[0][0], num2 + 45 - 10, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                }
                else
                {
                    SmallImage.drawSmallImage(g, array[0][1], num2 + 45 - 10, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                }
            }
            else if (currentCharViewInfo.isHaveMoto())
            {
                if (currentCharViewInfo.arrItemMounts[4].template.id == 485)
                {
                    if (currentCharViewInfo.arrItemMounts[4].sys < 2)
                    {
                        SmallImage.drawSmallImage(g, 1800, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 2063, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                }
                else if (currentCharViewInfo.arrItemMounts[4].template.id == 524)
                {
                    if (currentCharViewInfo.arrItemMounts[4].sys < 2)
                    {
                        SmallImage.drawSmallImage(g, 2067, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 2071, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                }
            }
            else if (currentCharViewInfo.isHaveWolf())
            {
                if (currentCharViewInfo.arrItemMounts[4].template.id == 443)
                {
                    if (currentCharViewInfo.arrItemMounts[4].sys < 2)
                    {
                        if (GameCanvas.gameTick % 20 > 15)
                        {
                            SmallImage.drawSmallImage(g, 1801, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                        }
                        else
                        {
                            SmallImage.drawSmallImage(g, 1802, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                        }
                    }
                    else if (GameCanvas.gameTick % 20 > 15)
                    {
                        SmallImage.drawSmallImage(g, 2080, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 2081, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                }
                else if (currentCharViewInfo.arrItemMounts[4].template.id == 523)
                {
                    if (GameCanvas.gameTick % 20 > 15)
                    {
                        SmallImage.drawSmallImage(g, 2062, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 2061, num2 + 45, num3 + 35, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                }
            }

            if (currentCharViewInfo.arrItemMounts[4].options != null)
            {
                for (int k = 0; k < currentCharViewInfo.arrItemMounts[4].options.size(); k++)
                {
                    ItemOption itemOption = (ItemOption)currentCharViewInfo.arrItemMounts[4].options.elementAt(k);
                    if (itemOption.optionTemplate.id == 65)
                    {
                        num4 = itemOption.param;
                    }
                    else if (itemOption.optionTemplate.id == 66)
                    {
                        num5 = itemOption.param;
                    }
                }
            }

            w = num4 * 85 / 1000;
            w2 = num5 * 85 / 1000;
            num6 = currentCharViewInfo.arrItemMounts[4].upgrade + 1;
        }

        int num7 = xstart + 5;
        int num8 = ystart + 112;
        mFont.tahoma_7b_white.drawString(g, mResources.Level + ": ", num7, num8, 0);
        mFont.tahoma_7b_white.drawString(g, num6 + string.Empty, num7 + 70, num8, 0);
        if (currentCharViewInfo.isHaveMoto())
        {
            mFont.tahoma_7b_white.drawString(g, mResources.MACHINE + ": ", num7, num8 += 15, 0);
        }
        else
        {
            mFont.tahoma_7b_white.drawString(g, mResources.EXP + ": ", num7, num8 += 15, 0);
        }

        g.setColor(6425);
        g.fillRect(num7 + 70, num8, 85, 14);
        g.setColor(371981);
        g.fillRect(num7 + 70, num8, w, 14);
        g.setColor(5131338);
        g.drawRect(num7 + 70, num8, 85, 14);
        mFont.tahoma_7_white.drawString(g, num4 + "/" + 1000, num7 + 113, num8 + 2, 2);
        if (currentCharViewInfo.isHaveMoto())
        {
            mFont.tahoma_7b_white.drawString(g, mResources.POWER + ": ", num7, num8 += 17, 0);
        }
        else
        {
            mFont.tahoma_7b_white.drawString(g, mResources.VITALITY + ": ", num7, num8 += 17, 0);
        }

        g.setColor(6425);
        g.fillRect(num7 + 70, num8, 85, 14);
        g.setColor(16711680);
        g.fillRect(num7 + 70, num8, w2, 14);
        g.setColor(5131338);
        g.drawRect(num7 + 70, num8, 85, 14);
        mFont.tahoma_7_white.drawString(g, num5 + "/" + 1000, num7 + 113, num8 + 2, 2);
    }

    public void paintClanInfo(mGraphics g)
    {
        if (!isPaintClan)
        {
            return;
        }

        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.CLAN[indexMenu], true);
        if (indexMenu == 0)
        {
            if (Char.clan == null || Char.clan.name == null || Char.clan.name.Equals(string.Empty))
            {
                indexRowMax = 1;
                mFont.tahoma_7_white.drawString(g, mResources.EMPTY_INFO, popupX + popupW / 2, popupY + 40, 2);
                return;
            }

            int[] array = new int[5]
            {
                1692,
                1693,
                1694,
                1695,
                1696
            };
            for (int i = 0; i < array.Length; i++)
            {
                g.setColor(6425);
                g.fillRect(popupX + i * indexSize + 18, popupY + 32, indexSize - 2, indexSize - 2);
                if (indexTitle == 1 && i == indexSelect)
                {
                    g.setColor(16777215);
                }
                else
                {
                    g.setColor(12281361);
                }

                g.drawRect(popupX + i * indexSize + 18, popupY + 32, indexSize - 2, indexSize - 2);
                if (i > Char.clan.itemLevel - 1)
                {
                    SmallImage.drawSmallImage(g, 1697, popupX + i * indexSize + 18 + indexSize / 2, popupY + 32 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
                else
                {
                    SmallImage.drawSmallImage(g, array[i], popupX + i * indexSize + 18 + indexSize / 2, popupY + 32 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (indexTitle == 2)
            {
                g.setColor(Paint.COLORDARK);
                g.fillRect(popupX + 7, popupY + 60, popupW - 14, popupH - 68);
                g.setColor(16777215);
            }
            else
            {
                g.setColor(10249521);
            }

            g.drawRect(popupX + 7, popupY + 60, popupW - 14, popupH - 68);
            xstart = popupX + 17;
            ystart = popupY + 62;
            indexRowMax = 12;
            scrMain.setStyle(indexRowMax, 12, popupX, popupY + 62, popupW, popupH - 72, true, 1);
            scrMain.setClip(g);
            mFont.tahoma_7b_yellow.drawString(g, mResources.CLANINFO[0] + Char.clan.name, xstart, ystart, 0);
            mFont.tahoma_7_blue1.drawString(g, mResources.CLANINFO[1] + Char.clan.main_name, xstart, ystart += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[2] + Char.clan.total + "/" + (Char.clan.level * 5 + 45), xstart, ystart += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[3] + Char.clan.level, xstart, ystart += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[4] + Char.clan.exp + "/" + Char.clan.expNext, xstart, ystart += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[5] + NinjaUtil.numberToString(Char.clan.coin + string.Empty) + " " + mResources.XU, xstart, ystart += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[8] + NinjaUtil.numberToString(Char.clan.freeCoin + string.Empty) + " " + mResources.XU, xstart, ystart += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[9] + NinjaUtil.numberToString(Char.clan.coinUp + string.Empty) + " " + mResources.XU, xstart, ystart += 12, 0);
            if (mFont.tahoma_7_white.getWidth(mResources.CLANINFO[10] + Char.clan.openDun + " " + mResources.COUNT) > inforW - 10)
            {
                paintMultiLine(g, mFont.tahoma_7_white, mResources.CLANINFO[10] + Char.clan.openDun + " " + mResources.COUNT, xstart, ystart += 12, 0, popupW - 20);
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[10] + Char.clan.openDun + " " + mResources.COUNT, xstart, ystart += 12, 0);
            }

            if (mFont.tahoma_7_white.getWidth(mResources.CLANINFO[12] + Char.clan.use_card + " " + mResources.COUNT) > inforW - 10)
            {
                paintMultiLine(g, mFont.tahoma_7_white, mResources.CLANINFO[12] + Char.clan.use_card + " " + mResources.COUNT, xstart, ystart += 12, 0, popupW - 20);
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[12] + Char.clan.use_card + " " + mResources.COUNT, xstart, ystart += 12, 0);
            }

            mFont.tahoma_7_white.drawString(g, mResources.CLANINFO[6] + Char.clan.reg_date, xstart, ystart += 12, 0);
            if (arrClanInfo == null)
            {
                arrClanInfo = splitMultiLine(mFont.tahoma_7_yellow, Char.clan.alert);
            }

            paintMultiLine(g, mFont.tahoma_7_yellow, arrClanInfo, xstart, ystart += 12, 0);
            if (indexTitle == 2 && indexRow >= 0)
            {
                SmallImage.drawSmallImage(g, 942, xstart - 8, popupY + 62 + 2 + indexRow * 12, 0, StaticObj.TOP_LEFT);
            }

            scrMain.setStyle(indexRowMax, 12, popupX, popupY + 62, popupW, popupH - 72, true, 1);
        }
        else if (indexMenu == 1)
        {
            xstart = popupX + 5;
            ystart = popupY + 32;
            if (vClan.size() == 0)
            {
                mFont.tahoma_7_white.drawString(g, mResources.EMPTY_INFO, popupX + popupW / 2, popupY + 40, mFont.CENTER);
                return;
            }

            g.setColor(6425);
            g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * 5 + 8);
            resetTranslate(g);
            if (Main.isPC)
            {
                scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * 5 + 6);
            }
            else
            {
                scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * 4 + 6);
            }

            indexMember = 0;
            for (int j = 0; j < vClan.size(); j++)
            {
                Member member = (Member)vClan.elementAt(j);
                if (isViewClanMemOnline && !member.isOnline)
                {
                    continue;
                }

                if (j * (indexSize + indexSize / 2) >= scrMain.cmy - (indexSize + indexSize / 2) && j * (indexSize + indexSize / 2) < scrMain.cmy + (indexSize * 5 + 8))
                {
                    if (indexRow == indexMember)
                    {
                        g.setColor(Paint.COLORLIGHT);
                        g.fillRect(xstart + 2, ystart + indexRow * (indexSize + indexSize / 2) + 2, popupW - 15, indexSize + indexSize / 2 - 4);
                        g.setColor(16777215);
                        g.drawRect(xstart + 2, ystart + indexRow * (indexSize + indexSize / 2) + 2, popupW - 15, indexSize + indexSize / 2 - 4);
                    }
                    else
                    {
                        g.setColor(Paint.COLORBACKGROUND);
                        g.fillRect(xstart + 2, ystart + indexMember * (indexSize + indexSize / 2) + 2, popupW - 15, indexSize + indexSize / 2 - 4);
                        g.setColor(13932896);
                        g.drawRect(xstart + 2, ystart + indexMember * (indexSize + indexSize / 2) + 2, popupW - 15, indexSize + indexSize / 2 - 4);
                    }

                    SmallImage.drawSmallImage(g, member.iconId, xstart + 12, ystart + indexMember * (indexSize + indexSize / 2) + 13, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    if (member.type == 4)
                    {
                        SmallImage.drawSmallImage(g, 1216, xstart + 12, ystart + indexMember * (indexSize + indexSize / 2) + 30, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                        if (member.isOnline)
                        {
                            mFont.tahoma_7_yellow.drawString(g, mResources.CLANS_STANDFOR[0] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0, mFont.tahoma_7_grey);
                            mFont.tahoma_7_white.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_green.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_blue1.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANS_STANDFOR[0] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                    }
                    else if (member.type == 3)
                    {
                        SmallImage.drawSmallImage(g, 1215, xstart + 12, ystart + indexMember * (indexSize + indexSize / 2) + 30, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                        if (member.isOnline)
                        {
                            mFont.tahoma_7_yellow.drawString(g, mResources.CLANS_STANDFOR[1] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0, mFont.tahoma_7_grey);
                            mFont.tahoma_7_white.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_green.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_blue1.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANS_STANDFOR[1] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                    }
                    else if (member.type == 2)
                    {
                        SmallImage.drawSmallImage(g, 1217, xstart + 12, ystart + indexMember * (indexSize + indexSize / 2) + 30, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                        if (member.isOnline)
                        {
                            mFont.tahoma_7_yellow.drawString(g, mResources.CLANS_STANDFOR[2] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0, mFont.tahoma_7_grey);
                            mFont.tahoma_7_white.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_green.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_blue1.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANS_STANDFOR[2] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                    }
                    else if (member.type == 1)
                    {
                        SmallImage.drawSmallImage(g, 1214, xstart + 12, ystart + indexMember * (indexSize + indexSize / 2) + 30, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                        if (member.isOnline)
                        {
                            mFont.tahoma_7_yellow.drawString(g, mResources.CLANS_STANDFOR[3] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0, mFont.tahoma_7_grey);
                            mFont.tahoma_7_white.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_green.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_blue1.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                        else
                        {
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANS_STANDFOR[3] + " ", xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 45, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                            mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                        }
                    }
                    else if (member.isOnline)
                    {
                        mFont.tahoma_7_white.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                        mFont.tahoma_7_green.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                        mFont.tahoma_7_blue1.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                    }
                    else
                    {
                        mFont.tahoma_7_grey.drawString(g, member.name + " - " + mResources.level + ": " + member.level, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 5, 0);
                        mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[7] + member.pointClan, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 16, 0);
                        mFont.tahoma_7_grey.drawString(g, mResources.CLANINFO[11] + member.pointClanWeek, xstart + 22, ystart + indexMember * (indexSize + indexSize / 2) + 26, 0);
                    }
                }

                indexMember++;
            }

            scrMain.setStyle(indexMember, indexSize + indexSize / 2, xstart, ystart, popupW - 3, indexSize * 5 + 4, true, 1);
            indexRowMax = indexMember;
            paintNumCount(g);
        }
        else if (indexMenu == 2)
        {
            xstart = popupX + 3;
            ystart = popupY + 32;
            g.setColor(6425);
            g.fillRect(xstart - 1, ystart - 1, columns * indexSize + 3, 5 * indexSize + 3);
            Item[] array2 = null;
            array2 = ((Char.clan != null && Char.clan.items != null) ? Char.clan.items : new Item[30]);
            paintBagItem(g, array2);
        }
        else if (indexMenu == 3)
        {
            if (Char.clan == null || Char.clan.name == null || Char.clan.name.Equals(string.Empty) || Char.clan.log.Equals(string.Empty))
            {
                indexRowMax = 1;
                mFont.tahoma_7_white.drawString(g, mResources.EMPTY_INFO, popupX + popupW / 2, popupY + 40, 2);
                return;
            }

            indexRowMax = 1;
            xstart = popupX + 17;
            ystart = popupY + 34;
            inforW = popupW - 30;
            scrMain.setClip(g);
            if (arrClanDongGop == null)
            {
                arrClanDongGop = splitMultiLine(mFont.tahoma_7_white, Char.clan.log);
            }

            paintMultiLine(g, mFont.tahoma_7_white, arrClanDongGop, xstart, ystart, 0);
            if (indexTitle == 1 && indexRow >= 0)
            {
                SmallImage.drawSmallImage(g, 942, xstart - 8, popupY + 34 + 2 + indexRow * 12, 0, StaticObj.TOP_LEFT);
            }

            scrMain.setStyle(indexRowMax, 12, popupX, popupY + 35, popupW, popupH - 44, true, 1);
        }
        else if (indexMenu == 4)
        {
            paintThanThu_Clan(g);
        }
    }

    private void paintNumCount(mGraphics g)
    {
        resetTranslate(g);
        int num = indexRow;
        if (isPaintAuctionBuy)
        {
            num = indexSelect;
        }

        if (num >= 0 && indexRowMax > 0)
        {
            int num2 = ((num + 1 >= indexRowMax) ? indexRowMax : (num + 1));
            mFont.tahoma_7_yellow.drawString(g, num2 + "/" + indexRowMax, popupX + popupW / 2, popupY + popupH - 14, 2, mFont.tahoma_7_grey);
        }
    }

    public bool isPaintPopup()
    {
        if (isPaintRankedList || isPaintLuckySpin || isPaintItemInfo || isPaintInfoMe || isPaintStore || isPaintEliteShop || isPaintAuctionBuy || isPaintWeapon || isPaintNonNam || isPaintNonNu || isPaintAoNam || isPaintAoNu || isPaintGangTayNam || isPaintGangTayNu || isPaintQuanNam || isPaintQuanNu || isPaintGiayNam || isPaintGiayNu || isPaintLien || isPaintNhan || isPaintNgocBoi || isPaintPhu || isPaintStack || isPaintStackLock || isPaintGrocery || isPaintGroceryLock || isPaintUpGrade || isPaintAuctionSale || isPaintConvert || isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintUpPearl || isPaintLuyenThach || isPaintBox || isPaintTrade || isPaintAlert || isPaintZone || isPaintAuto || isPaintTeam || isPaintClan || isPaintFindTeam || isPaintTask || isPaintFriend || isPaintList || isPaintEnemies || isPaintCharInMap || isPaintMessage || isPaintLucky_Draw || isPaintLuyenNgoc || isPaintKhamNgoc || isPaintGiaoDo || isPaintGotNgoc || isPaintThaoNgoc || isPaintItemDell)
        {
            return true;
        }

        return false;
    }

    public bool isNotPaintTouchControl()
    {
        if (!GameCanvas.isTouch || Char.ischangingMap || isPaintZone || isPaintAuto)
        {
            return true;
        }

        if (ChatTextField.gI().isShow)
        {
            return true;
        }

        if (center == cmdDead && GameCanvas.isTouchControlLargeScreen)
        {
            return true;
        }

        if (GameCanvas.currentDialog != null || ChatPopup.currentMultilineChatPopup != null || GameCanvas.menu.showMenu || isPaintPopup())
        {
            return true;
        }

        return false;
    }

    public bool isPaintUI()
    {
        if (isPaintAuto || isPaintStore || isPaintLuckySpin || isPaintEliteShop || isPaintWeapon || isPaintNonNam || isPaintNonNu || isPaintAoNam || isPaintAoNu || isPaintGangTayNam || isPaintGangTayNu || isPaintQuanNam || isPaintQuanNu || isPaintGiayNam || isPaintGiayNu || isPaintLien || isPaintNhan || isPaintNgocBoi || isPaintPhu || isPaintStack || isPaintStackLock || isPaintGrocery || isPaintGroceryLock || isPaintUpGrade || isPaintAuctionBuy || isPaintAuctionSale || isPaintConvert || isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintUpPearl || isPaintLuyenThach || isPaintBox || isPaintTrade || isPaintLuyenNgoc || isPaintKhamNgoc || isPaintGiaoDo || isPaintGotNgoc || isPaintThaoNgoc || isPaintItemDell)
        {
            return true;
        }

        return false;
    }

    public void paintSkillInfo(mGraphics g, Skill skill)
    {
        if (Char.getMyChar().clevel >= skill.level)
        {
            mFont.tahoma_7_white.drawString(g, mResources.LEVELNEED + " " + skill.level, xstart + 5, yPaint += 12, 0);
        }
        else
        {
            mFont.tahoma_7_red.drawString(g, mResources.LEVELNEED + " " + skill.level, xstart + 5, yPaint += 12, 0);
        }

        if (skill.template.type != 0)
        {
            indexRowMax += 4;
            mFont.tahoma_7_white.drawString(g, mResources.MAX_FIGHT + ": " + skill.maxFight, xstart + 5, yPaint += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.MANA_USE + ": " + skill.manaUse, xstart + 5, yPaint += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.RANGE_ATTACK + ": " + skill.dx, xstart + 5, yPaint += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.TIME_LOOP + ": " + skill.strTimeReplay() + " " + mResources.SECOND, xstart + 5, yPaint += 12, 0);
        }

        indexRowMax++;
    }

    public void paintSkillOption(mGraphics g, Skill skill)
    {
        SkillOption[] options = skill.options;
        foreach (SkillOption skillOption in options)
        {
            if (mFont.tahoma_7_white.getWidth(skillOption.getOptionString()) > 145)
            {
                MyVector myVector = mFont.tahoma_7_white.splitFontVector(skillOption.getOptionString(), 145);
                for (int j = 0; j < myVector.size(); j++)
                {
                    mFont.tahoma_7_white.drawString(g, myVector.elementAt(j).ToString(), xstart + 5, yPaint += 12, 0);
                    indexRowMax++;
                }
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, skillOption.getOptionString(), xstart + 5, yPaint += 12, 0);
                indexRowMax++;
            }
        }
    }

    public void paintKyNang(mGraphics g)
    {
        if (indexMenu != 1)
        {
            return;
        }

        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.MENUME[indexMenu], true);
        mFont.tahoma_7b_white.drawString(g, mResources.SKILL_POINT, popupX + 10, popupY + 30, 0);
        mFont.tahoma_7b_white.drawString(g, string.Empty + Char.getMyChar().sPoint, popupX + popupW - 10, popupY + 30, mFont.RIGHT);
        g.setColor(0);
        g.fillRect(popupX + 4, popupY + 44, popupW - 7, indexSize + 3);
        g.setColor(12281361);
        g.drawRect(popupX + 5, popupY + 45, popupW - 10, indexSize);
        if (indexTitle >= 1)
        {
            g.setColor(Paint.COLORBORDER);
            g.drawRect(popupX + 5, popupY + 48 + indexSize, popupW - 10, popupH - 64 - indexSize);
        }

        int num = Char.getMyChar().nClass.skillTemplates.Length;
        xstart = popupX + 5;
        ystart = popupY + 45;
        scrMain.setStyle(num, indexSize + 2, xstart + 1, ystart, popupW - 12, indexSize + 2, false, 1);
        scrMain.setClip(g);
        for (int i = 0; i < num; i++)
        {
            SmallImage.drawSmallImage(g, Char.getMyChar().nClass.skillTemplates[i].iconId, xstart + i * (indexSize + 2) + indexSize / 2, ystart + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            if (indexTitle == 1 && i == indexSelect)
            {
                g.setColor(16777215);
                g.drawRect(xstart + i * (indexSize + 2) + 2, ystart + 2, indexSize - 4, indexSize - 4);
                paintSelectHighlight(xstart + i * (indexSize + 2), ystart, g);
            }
        }

        xstart += 8;
        ystart += 6;
        if (indexTitle != 1 || indexSelect < 0)
        {
            return;
        }

        resetTranslate(g);
        SkillTemplate skillTemplate = Char.getMyChar().nClass.skillTemplates[indexSelect];
        indexRowMax = 4 + skillTemplate.description.Length;
        Skill skill = Char.getMyChar().getSkill(skillTemplate);
        int num2 = popupX;
        int num3 = ystart + indexSize + 2;
        int w = popupW - 6;
        int h = popupH - 70 - indexSize;
        scrInfo.setClip(g, num2, num3, w, h);
        yPaint = ystart + 18;
        if (skill == null)
        {
            skill = ((skillTemplate.skills.Length <= 1) ? skillTemplate.skills[0] : skillTemplate.skills[1]);
            mFont.tahoma_7b_red.drawString(g, skillTemplate.name, xstart + 5, yPaint += 12, 0);
            for (int j = 0; j < skillTemplate.description.Length; j++)
            {
                mFont.tahoma_7_white.drawString(g, skillTemplate.description[j], xstart + 5, yPaint += 12, 0);
            }

            mFont.tahoma_7_white.drawString(g, mResources.SKILLTYPE[skillTemplate.type], xstart + 5, yPaint += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.LEVELMAX + ": " + BasicSkillGrowth.DisplayMaxPoint(skillTemplate), xstart + 5, yPaint += 12, 0);
            mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.LEVELNEXT, skill.point + string.Empty), xstart + 5, yPaint += 12, 0);
            paintSkillInfo(g, skill);
            paintSkillOption(g, skill);
        }
        else
        {
            mFont.tahoma_7b_white.drawString(g, skillTemplate.name, xstart + 5, yPaint += 12, 0);
            for (int k = 0; k < skillTemplate.description.Length; k++)
            {
                mFont.tahoma_7_white.drawString(g, skillTemplate.description[k], xstart + 5, yPaint += 12, 0);
            }

            mFont.tahoma_7_white.drawString(g, mResources.SKILLTYPE[skillTemplate.type], xstart + 5, yPaint += 12, 0);
            mFont.tahoma_7_white.drawString(g, mResources.LEVELMAX + ": " + BasicSkillGrowth.DisplayMaxPoint(skillTemplate), xstart + 5, yPaint += 12, 0);
            if (skill.point >= BasicSkillGrowth.DisplayMaxPoint(skillTemplate) || skillTemplate.skills[skillTemplate.skills.Length - 1].Equals(skill)) // offline: the grown basic attack (BasicSkillGrowth) has no row after its last point
            {
                mFont.tahoma_7_blue.drawString(g, mResources.LEVELEDMAX, xstart + 5, yPaint += 12, 0);
                paintSkillInfo(g, skill);
                paintSkillOption(g, skill);
            }
            else
            {
                mFont.tahoma_7_blue.drawString(g, mResources.replace(mResources.LEVELNOW, skill.point + string.Empty), xstart + 5, yPaint += 12, 0);
                paintSkillInfo(g, skill);
                paintSkillOption(g, skill);
                int num4 = 0;
                for (num4 = 0; num4 < skillTemplate.skills.Length; num4++)
                {
                    if (skillTemplate.skills[num4].Equals(skill))
                    {
                        num4++;
                        break;
                    }
                }

                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.LEVELNEXT, string.Empty + skillTemplate.skills[num4].point), xstart + 5, yPaint += 12, 0);
                paintSkillInfo(g, skillTemplate.skills[num4]);
                indexRowMax++;
                paintSkillOption(g, skillTemplate.skills[num4]);
            }
        }

        scrInfo.setStyle(indexRowMax, 12, num2, num3, w, h, true, 1);
        if (indexRow >= 0)
        {
            SmallImage.drawSmallImage(g, 942, xstart + 2, ystart + 32 + indexRow * 12, 0, StaticObj.TOP_RIGHT);
        }
    }

    public void paintHanhTrang(mGraphics g)
    {
        if (indexMenu == 0)
        {
            paintBag(g, mResources.MENUME);
        }
    }

    public void paintBag(mGraphics g, string[] titles)
    {
        try
        {
            resetTranslate(g);
            paintItemFrame(g, titles, true);
            paintBagItem(g, Char.getMyChar().arrItemBag);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    private void paintBagItem(mGraphics g, Item[] items)
    {
        rows = items.Length / columns;
        int num = (Main.isPC ? 5 : 4);
        scrMain.setStyle(rows, indexSize, xstart, ystart, columns * indexSize, num * indexSize, true, 6);
        scrMain.setClip(g, xstart, ystart, scrMain.width + 2, scrMain.height + 2);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
            }
        }

        foreach (Item item in items)
        {
            if (item != null)
            {
                int num2 = item.indexUI / columns;
                int num3 = item.indexUI - num2 * columns;
                paintItem(g, item, xstart + num3 * indexSize, ystart + num2 * indexSize);
                if (item.quantity > 1)
                {
                    mFont.number_yellow.drawString(g, string.Empty + item.quantity, xstart + num3 * indexSize + indexSize, ystart + num2 * indexSize + indexSize - mFont.number_yellow.getHeight(), 1);
                }
            }
        }

        if (indexTitle > 0 && indexSelect >= 0)
        {
            int num4 = indexSelect / columns;
            int num5 = indexSelect - num4 * columns;
            g.setColor(16777215);
            g.drawRect(xstart + num5 * indexSize, ystart + num4 * indexSize, indexSize, indexSize);
            paintSelectHighlight(xstart + num5 * indexSize, ystart + num4 * indexSize, g);
        }
    }

    private void paintBagItem2(mGraphics g, short[] items)
    {
        rows = items.Length / columns;
        int num = (Main.isPC ? 5 : 4);
        scrMain.setStyle(rows, indexSize, xstart, ystart, columns * indexSize, num * indexSize, true, 6);
        scrMain.setClip(g, xstart, ystart, scrMain.width + 2, scrMain.height + 2);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            short id = items[i];
            if (id > 0)
            {
                int num2 = i / columns;
                int num3 = i - num2 * columns;
                ItemTemplate var7 = ItemTemplates.get(id);
                if (var7 != null)
                {
                    SmallImage.drawSmallImage(g, var7.iconID, xstart + num3 * indexSize + indexSize / 2, ystart + num2 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }
        }

        if (indexTitle > 0 && indexSelect >= 0)
        {
            int num4 = indexSelect / columns;
            int num5 = indexSelect - num4 * columns;
            g.setColor(16777215);
            g.drawRect(xstart + num5 * indexSize, ystart + num4 * indexSize, indexSize, indexSize);
            paintSelectHighlight(xstart + num5 * indexSize, ystart + num4 * indexSize, g);
        }
    }

    private void paintSelectHighlight(int x, int y, mGraphics g)
    {
        g.drawImage(imgSelect, x - 5, y - 5, 0);
    }

    private void paintUpgradeEffect(int x, int y, int upgrade, mGraphics g)
    {
        int num = indexSize - 2;
        int num2 = 0;
        int num3 = ((upgrade >= 4) ? ((upgrade < 8) ? 1 : ((upgrade < 12) ? 2 : ((upgrade > 14) ? 4 : 3))) : 0);
        for (int i = num2; i < size.Length; i++)
        {
            int num4 = x - num / 2 + upgradeEffectX(GameCanvas.gameTick - i * 4);
            int num5 = y - num / 2 + upgradeEffectY(GameCanvas.gameTick - i * 4);
            g.setColor(colorBorder[num3][i]);
            g.fillRect(num4 - size[i] / 2, num5 - size[i] / 2, size[i], size[i]);
        }

        if (upgrade == 4 || upgrade == 8)
        {
            for (int j = num2; j < size.Length; j++)
            {
                int num6 = x - num / 2 + upgradeEffectX(GameCanvas.gameTick - num * 2 - j * 4);
                int num7 = y - num / 2 + upgradeEffectY(GameCanvas.gameTick - num * 2 - j * 4);
                g.setColor(colorBorder[num3 - 1][j]);
                g.fillRect(num6 - size[j] / 2, num7 - size[j] / 2, size[j], size[j]);
            }
        }

        if (upgrade != 1 && upgrade != 4 && upgrade != 8)
        {
            for (int k = num2; k < size.Length; k++)
            {
                int num8 = x - num / 2 + upgradeEffectX(GameCanvas.gameTick - num * 2 - k * 4);
                int num9 = y - num / 2 + upgradeEffectY(GameCanvas.gameTick - num * 2 - k * 4);
                g.setColor(colorBorder[num3][k]);
                g.fillRect(num8 - size[k] / 2, num9 - size[k] / 2, size[k], size[k]);
            }
        }

        if (upgrade != 1 && upgrade != 4 && upgrade != 8 && upgrade != 12 && upgrade != 2 && upgrade != 5 && upgrade != 9)
        {
            for (int l = num2; l < size.Length; l++)
            {
                int num10 = x - num / 2 + upgradeEffectX(GameCanvas.gameTick - num - l * 4);
                int num11 = y - num / 2 + upgradeEffectY(GameCanvas.gameTick - num - l * 4);
                g.setColor(colorBorder[num3][l]);
                g.fillRect(num10 - size[l] / 2, num11 - size[l] / 2, size[l], size[l]);
            }
        }

        if (upgrade != 1 && upgrade != 4 && upgrade != 8 && upgrade != 12 && upgrade != 2 && upgrade != 5 && upgrade != 9 && upgrade != 13 && upgrade != 3 && upgrade != 6 && upgrade != 10 && upgrade != 15)
        {
            for (int m = num2; m < size.Length; m++)
            {
                int num12 = x - num / 2 + upgradeEffectX(GameCanvas.gameTick - num * 3 - m * 4);
                int num13 = y - num / 2 + upgradeEffectY(GameCanvas.gameTick - num * 3 - m * 4);
                g.setColor(colorBorder[num3][m]);
                g.fillRect(num12 - size[m] / 2, num13 - size[m] / 2, size[m], size[m]);
            }
        }
    }

    private void paintItemFrame_shopOnline(mGraphics g, string titles, bool paintMoney)
    {
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        if (paintMoney)
        {
            mFont.tahoma_7_white.drawString(g, mResources.XU + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().xu), popupX + 6, popupY + popupH - 26, 0);
            mFont.tahoma_7_white.drawString(g, mResources.YEN + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().yen), popupX + popupW - 6, popupY + popupH - 26, 1);
            if (isPaintTrade)
            {
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.SELECTTRADE, popupX + popupW / 2, popupY + popupH - 14, 2);
                }
            }
            else if (isPaintUpPearl)
            {
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.SELECTPEARL, popupX + popupW / 2, popupY + popupH - 14, 2);
                }
            }
            else if (isPaintSplit)
            {
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.SELECT_ITEM, popupX + popupW / 2, popupY + popupH - 14, 2);
                }
            }
            else
            {
                mFont.tahoma_7_yellow.drawString(g, mResources.LUONG + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().luong), popupX + popupW / 2, popupY + popupH - 16, 2);
            }
        }

        paintTitle(g, titles, titles.Length > 1);
        xstart = popupX + 3;
        ystart = popupY + 32;
        g.setColor(6425);
        int num = (Main.isPC ? 5 : 4);
        g.fillRect(xstart - 1, ystart - 1, columns * indexSize + 3, num * indexSize + 3);
    }

    public void paintShopOnline(mGraphics g, Item[] items)
    {
        try
        {
            resetTranslate(g);
            paintItemFrame_shopOnline(g, Name_store, true);
            if (items == null)
            {
                GameCanvas.paintShukiren(popupX + 90, popupY + 75, g, false);
                mFont.tahoma_7b_white.drawString(g, mResources.PLEASEWAIT, popupX + 90, popupY + 90, 2);
                return;
            }

            if (items.Length <= 30)
            {
                rows = 5;
            }
            else if (items.Length % columns == 0)
            {
                rows = items.Length / columns;
            }
            else
            {
                rows = items.Length / columns + 1;
            }

            scrMain.setStyle(rows, indexSize, xstart, ystart, columns * indexSize, 5 * indexSize, true, 6);
            scrMain.setClip(g, xstart, ystart, scrMain.width + 2, scrMain.height + 2);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            foreach (Item item in items)
            {
                if (item != null)
                {
                    int num = item.indexUI / columns;
                    int num2 = item.indexUI - num * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num2 * indexSize + 1, ystart + num * indexSize + 1, indexSize - 1, indexSize - 1);
                        SmallImage.drawSmallImage(g, 154, xstart + num2 * indexSize + indexSize / 2, ystart + num * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    }

                    SmallImage.drawSmallImage(g, item.template.iconID, xstart + num2 * indexSize + indexSize / 2, ystart + num * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (indexTitle > 0 && indexSelect >= 0)
            {
                int num3 = indexSelect / columns;
                int num4 = indexSelect - num3 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num4 * indexSize, ystart + num3 * indexSize, indexSize, indexSize);
                paintSelectHighlight(xstart + num4 * indexSize, ystart + num3 * indexSize, g);
            }
        }
        catch (Exception)
        {
        }
    }

    public void paintItemList(mGraphics g, string[] titles, Item[] items)
    {
        try
        {
            resetTranslate(g);
            paintItemFrame(g, titles, true);
            if (items == null)
            {
                GameCanvas.paintShukiren(popupX + 90, popupY + 75, g, false);
                mFont.tahoma_7b_white.drawString(g, mResources.PLEASEWAIT, popupX + 90, popupY + 90, 2);
                return;
            }

            if (items.Length <= 30)
            {
                rows = 5;
            }
            else if (items.Length % columns == 0)
            {
                rows = items.Length / columns;
            }
            else
            {
                rows = items.Length / columns + 1;
            }

            int num = (Main.isPC ? 5 : 4);
            scrMain.setStyle(rows, indexSize, xstart, ystart, columns * indexSize, num * indexSize, true, 6);
            scrMain.setClip(g, xstart, ystart, scrMain.width + 2, scrMain.height + 2);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            foreach (Item item in items)
            {
                if (item != null)
                {
                    int num2 = item.indexUI / columns;
                    int num3 = item.indexUI - num2 * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num3 * indexSize + 1, ystart + num2 * indexSize + 1, indexSize - 1, indexSize - 1);
                        SmallImage.drawSmallImage(g, 154, xstart + num3 * indexSize + indexSize / 2, ystart + num2 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    }

                    if (GameCanvas.gameTick % 6 == 0)
                    {
                        item.indexFrame = (item.indexFrame + 1) % 3;
                    }

                    if (item.isNewitem())
                    {
                        g.drawImage(GameCanvas.backitem, xstart + num3 * indexSize + indexSize / 2, ystart + num2 * indexSize + indexSize / 2, 3);
                    }

                    SmallImage.drawSmallImageNew(g, item.template.iconID, xstart + num3 * indexSize + indexSize / 2, ystart + num2 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER, item.indexFrame);
                }
            }

            if (indexTitle > 0 && indexSelect >= 0)
            {
                int num4 = indexSelect / columns;
                int num5 = indexSelect - num4 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num5 * indexSize, ystart + num4 * indexSize, indexSize, indexSize);
                paintSelectHighlight(xstart + num5 * indexSize, ystart + num4 * indexSize, g);
            }
        }
        catch (Exception)
        {
        }
    }

    private void paintItemFrame(mGraphics g, string[] titles, bool paintMoney)
    {
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        if (paintMoney)
        {
            mFont.tahoma_7_white.drawString(g, mResources.XU + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().xu), popupX + 6, popupY + popupH - 26, 0);
            mFont.tahoma_7_white.drawString(g, mResources.YEN + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().yen), popupX + popupW - 6, popupY + popupH - 26, 1);
            if (isPaintTrade)
            {
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.SELECTTRADE, popupX + popupW / 2, popupY + popupH - 14, 2);
                }
            }
            else if (isPaintUpPearl)
            {
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.SELECTPEARL, popupX + popupW / 2, popupY + popupH - 14, 2);
                }
            }
            else if (isPaintSplit)
            {
                if (GameCanvas.gameTick % 10 > 4)
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.SELECT_ITEM, popupX + popupW / 2, popupY + popupH - 14, 2);
                }
            }
            else
            {
                mFont.tahoma_7_yellow.drawString(g, mResources.LUONG + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().luong), popupX + popupW / 2, popupY + popupH - 16, 2);
            }
        }

        paintTitle(g, titles[indexMenu], titles.Length > 1);
        xstart = popupX + 3;
        ystart = popupY + 32;
        g.setColor(6425);
        int num = (Main.isPC ? 5 : 4);
        g.fillRect(xstart - 1, ystart - 1, columns * indexSize + 3, num * indexSize + 3);
    }

    public void paintBox(mGraphics g, string[] titles)
    {
        try
        {
            resetTranslate(g);
            paintItemFrame(g, titles, false);
            if (Char.getMyChar().arrItemBox == null)
            {
                GameCanvas.paintShukiren(popupX + 90, popupY + 75, g, false);
                mFont.tahoma_7b_white.drawString(g, mResources.PLEASEWAIT, popupX + 90, popupY + 90, 2);
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, mResources.XUINBAG + ": " + NinjaUtil.numberToString(string.Empty + Char.getMyChar().xuInBox), popupX + popupW / 2, popupY + popupH - 18, 2);
                paintBagItem(g, Char.getMyChar().arrItemBox);
            }
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void paintItemDell(mGraphics g, string[] titles)
    {
        try
        {
            resetTranslate(g);
            paintItemFrame(g, titles, false);
            paintBagItem2(g, Handle.arrItemDell);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void paintUpgrade(mGraphics g, string[] titles)
    {
        try
        {
            rows = 3;
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 34 + indexSize;
            int num = popupX + 45;
            int num2 = popupX + 100;
            int num3 = ystart - indexSize - 3;
            if (itemUpGrade != null)
            {
                paintItem(g, itemUpGrade, num, num3);
                g.setColor(12281361);
                g.drawRect(num, num3, indexSize, indexSize);
                mFont.tahoma_7_yellow.drawString(g, "(+" + itemUpGrade.upgrade + ")", num - 5, num3 + indexSize / 2 - 5, mFont.RIGHT);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num3 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num3 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(num, num3, indexSize, indexSize);
            }

            SmallImage.drawSmallImage(g, 942, num + 43, ystart - 15, 0, StaticObj.VCENTER_HCENTER);
            if (itemUpGrade != null && !itemUpGrade.isUpMax())
            {
                paintItem(g, itemUpGrade, num2, num3, 1, 0);
                g.setColor(12281361);
                g.drawRect(num2, num3, indexSize, indexSize);
                mFont.tahoma_7_yellow.drawString(g, "(+" + (itemUpGrade.upgrade + 1) + ")", num2 + indexSize + 10, num3 + indexSize / 2 - 5, mFont.LEFT);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num2 - 1, num3 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num2 + indexSize / 2, num3 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(num2, num3, indexSize, indexSize);
            }

            if (indexTitle == 1)
            {
                if (indexSelect == 0)
                {
                    g.setColor(16777215);
                    g.drawRect(num, num3, indexSize, indexSize);
                }

                if (indexSelect == 1)
                {
                    g.setColor(16777215);
                    g.drawRect(num2, num3, indexSize, indexSize);
                }
            }

            if (itemUpGrade != null)
            {
                if (itemUpGrade.isUpMax())
                {
                    if (!GameCanvas.isTouchControlLargeScreen)
                    {
                        mFont.tahoma_7_yellow.drawString(g, mResources.HELPUPGRADE[3], popupX + popupW / 2, ystart + rows * indexSize + 5, mFont.CENTER);
                    }
                    else
                    {
                        mFont.tahoma_7_yellow.drawString(g, mResources.HELPUPGRADE[3], popupX + 7, ystart + rows * indexSize + 5, mFont.LEFT);
                    }
                }
                else
                {
                    int num4 = 0;
                    for (int i = 0; i < arrItemUpGrade.Length; i++)
                    {
                        if (arrItemUpGrade[i] != null && arrItemUpGrade[i].template.type == 26)
                        {
                            num4 += crystals[arrItemUpGrade[i].template.id];
                        }
                    }

                    int num5 = 0;
                    int num6 = 0;
                    if (itemUpGrade.isTypeClothe())
                    {
                        num5 = num4 * 100 / upClothe[itemUpGrade.upgrade];
                        if (num5 > maxPercents[itemUpGrade.upgrade])
                        {
                            num5 = maxPercents[itemUpGrade.upgrade];
                        }

                        if (isPaintUpGradeGold)
                        {
                            num5 = (int)((double)num5 * 1.5);
                            num6 = goldUps[itemUpGrade.upgrade];
                        }

                        if (Main.isPC)
                        {
                            if (coinUpClothes[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpClothes[itemUpGrade.upgrade])), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpClothes[itemUpGrade.upgrade])), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                            }

                            if (num6 > Char.getMyChar().luong)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.GOLDNEED, num6 + string.Empty), xstart, ystart + rows * indexSize + 17, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.GOLDNEED, num6 + string.Empty), xstart, ystart + rows * indexSize + 17, mFont.LEFT);
                            }

                            mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT + ": " + num5 + "%", xstart, ystart + rows * indexSize + 29, mFont.LEFT);
                        }
                        else
                        {
                            string[] array = new string[3];
                            if (coinUpClothes[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
                            {
                                array[0] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpClothes[itemUpGrade.upgrade]));
                            }
                            else
                            {
                                array[0] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpClothes[itemUpGrade.upgrade]));
                            }

                            if (num6 > Char.getMyChar().luong)
                            {
                                array[1] = mResources.replace(mResources.GOLDNEED, num6 + string.Empty);
                            }
                            else
                            {
                                array[1] = mResources.replace(mResources.GOLDNEED, num6 + string.Empty);
                            }

                            array[2] = mResources.PERCENT + ": " + num5 + "%";
                            paintInfoHelpCheNgoc1(g, array, 0);
                        }
                    }
                    else if (itemUpGrade.isTypeAdorn())
                    {
                        num5 = num4 * 100 / upAdorn[itemUpGrade.upgrade];
                        if (num5 > maxPercents[itemUpGrade.upgrade])
                        {
                            num5 = maxPercents[itemUpGrade.upgrade];
                        }

                        if (isPaintUpGradeGold)
                        {
                            num5 = (int)((double)num5 * 1.5);
                            num6 = goldUps[itemUpGrade.upgrade];
                        }

                        if (Main.isPC)
                        {
                            if (coinUpAdorns[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpAdorns[itemUpGrade.upgrade])), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpAdorns[itemUpGrade.upgrade])), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                            }

                            if (num6 > Char.getMyChar().luong)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.GOLDNEED, num6 + string.Empty), xstart, ystart + rows * indexSize + 17, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.GOLDNEED, num6 + string.Empty), xstart, ystart + rows * indexSize + 17, mFont.LEFT);
                            }

                            mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT + ": " + num5 + "%", xstart, ystart + rows * indexSize + 29, mFont.LEFT);
                        }
                        else
                        {
                            string[] array2 = new string[3];
                            if (coinUpAdorns[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
                            {
                                array2[0] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpAdorns[itemUpGrade.upgrade]));
                            }
                            else
                            {
                                array2[0] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpAdorns[itemUpGrade.upgrade]));
                            }

                            if (num6 > Char.getMyChar().luong)
                            {
                                array2[1] = mResources.replace(mResources.GOLDNEED, num6 + string.Empty);
                            }
                            else
                            {
                                array2[1] = mResources.replace(mResources.GOLDNEED, num6 + string.Empty);
                            }

                            array2[2] = mResources.PERCENT + ": " + num5 + "%";
                            paintInfoHelpCheNgoc1(g, array2, 0);
                        }
                    }
                    else if (itemUpGrade.isTypeWeapon())
                    {
                        num5 = num4 * 100 / upWeapon[itemUpGrade.upgrade];
                        if (num5 > maxPercents[itemUpGrade.upgrade])
                        {
                            num5 = maxPercents[itemUpGrade.upgrade];
                        }

                        if (isPaintUpGradeGold)
                        {
                            num5 = (int)((double)num5 * 1.5);
                            num6 = goldUps[itemUpGrade.upgrade];
                        }

                        if (Main.isPC)
                        {
                            if (coinUpWeapons[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpWeapons[itemUpGrade.upgrade])), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpWeapons[itemUpGrade.upgrade])), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                            }

                            if (num6 > Char.getMyChar().luong)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.GOLDNEED, num6 + string.Empty), xstart, ystart + rows * indexSize + 17, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.GOLDNEED, num6 + string.Empty), xstart, ystart + rows * indexSize + 17, mFont.LEFT);
                            }

                            mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT + ": " + num5 + "%", xstart, ystart + rows * indexSize + 29, mFont.LEFT);
                        }
                        else
                        {
                            string[] array3 = new string[3];
                            if (coinUpWeapons[itemUpGrade.upgrade] > Char.getMyChar().xu + Char.getMyChar().yen)
                            {
                                array3[0] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpWeapons[itemUpGrade.upgrade]));
                            }
                            else
                            {
                                array3[0] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpWeapons[itemUpGrade.upgrade]));
                            }

                            if (num6 > Char.getMyChar().luong)
                            {
                                array3[1] = mResources.replace(mResources.GOLDNEED, num6 + string.Empty);
                            }
                            else
                            {
                                array3[1] = mResources.replace(mResources.GOLDNEED, num6 + string.Empty);
                            }

                            array3[2] = mResources.PERCENT + ": " + num5 + "%";
                            paintInfoHelpCheNgoc1(g, array3, 0);
                        }
                    }
                }
            }
            else if (Main.isPC)
            {
                for (int j = 0; j < 3; j++)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.HELPUPGRADE[j], xstart, ystart + rows * indexSize + 5 + j * 12, mFont.LEFT);
                }
            }
            else
            {
                paintInfoHelpCheNgoc(g, mResources.HELPUPGRADE, 3);
            }

            g.setColor(0);
            g.fillRect(xstart - 1, ystart - 1, columns * indexSize + 3, rows * indexSize + 3);
            for (int k = 0; k < rows; k++)
            {
                for (int l = 0; l < columns; l++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + l * indexSize + indexSize / 2, ystart + k * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + l * indexSize, ystart + k * indexSize, indexSize, indexSize);
                }
            }

            if (indexTitle == 2)
            {
                int num7 = indexSelect / columns;
                int num8 = indexSelect - num7 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num8 * indexSize, ystart + num7 * indexSize, indexSize, indexSize);
            }

            for (int m = 0; m < arrItemUpGrade.Length; m++)
            {
                Item item = arrItemUpGrade[m];
                if (item != null)
                {
                    int num9 = m / columns;
                    int num10 = m - num9 * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num10 * indexSize + 1, ystart + num9 * indexSize + 1, indexSize - 1, indexSize - 1);
                    }

                    SmallImage.drawSmallImage(g, item.template.iconID, xstart + num10 * indexSize + indexSize / 2, ystart + num9 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (effUpok == null)
            {
                return;
            }

            SmallImage.drawSmallImage(g, effUpok.arrEfInfo[indexEff].idImg, num + indexSize / 2 + effUpok.arrEfInfo[indexEff].dx + 1, num3 + indexSize / 2 + 9 + effUpok.arrEfInfo[indexEff].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            if (GameCanvas.gameTick % 2 == 0)
            {
                indexEff++;
                if (indexEff >= effUpok.arrEfInfo.Length)
                {
                    indexEff = 0;
                    effUpok = null;
                }
            }
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    private void paintItem(mGraphics g, Item item, int x, int y)
    {
        paintItem(g, item, x, y, 0, 0);
    }

    public void paintStands(mGraphics g, string[] titles)
    {
        try
        {
            rows = 5;
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintItemFrame(g, titles, false);
            g.setColor(6693376);
            g.fillRect(popupX + 3, popupY + 32, 168, 140);
            g.setColor(13408563);
            g.drawRect(popupX + 3, popupY + 32, 168, 140);
            int num = popupX + 74;
            int num2 = popupY + 40 + indexSize;
            mFont.tahoma_7_white.drawString(g, mResources.ITEM_SELL, num + indexSize / 2, num2 - indexSize / 2 - 4, mFont.CENTER);
            if (itemSell != null)
            {
                g.setColor(6425);
                g.fillRect(num - 1, num2 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num2 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                paintItem(g, itemSell, num, num2);
                if (itemSell.quantity > 1)
                {
                    mFont.number_yellow.drawString(g, string.Empty + itemSell.quantity, num + indexSize, num2 + indexSize / 2 + 6, 1);
                }

                g.setColor((indexTitle != 1) ? 12281361 : 16777215);
                g.drawRect(num, num2, indexSize, indexSize);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num2 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num2 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(num, num2, indexSize, indexSize);
            }

            mFont.tahoma_7_white.drawString(g, mResources.MONEY_SELL, num + indexSize / 2, num2 + 3 * indexSize / 2 + 2, mFont.CENTER);
            if (Char.getMyChar().xu < 5000)
            {
                mFont.tahoma_7_red.drawString(g, mResources.SALENEED, num + indexSize / 2, popupY + popupH - 25, mFont.CENTER);
                mFont.tahoma_7_red.drawString(g, mResources.SALETAX, num + indexSize / 2, popupY + popupH - 13, mFont.CENTER);
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, mResources.SALENEED, num + indexSize / 2, popupY + popupH - 25, mFont.CENTER);
                mFont.tahoma_7_white.drawString(g, mResources.SALETAX, num + indexSize / 2, popupY + popupH - 13, mFont.CENTER);
            }

            tfText.x = popupX + 40;
            tfText.y = popupY + 130;
            tfText.paint(g);
        }
        catch (Exception)
        {
        }
    }

    public void paintConvert(mGraphics g, string[] titles)
    {
        try
        {
            rows = 3;
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 34 + indexSize;
            int num = popupX + 45;
            int num2 = popupX + 100;
            int num3 = ystart - indexSize - 3;
            if (arrItemConvert[0] != null)
            {
                paintItem(g, arrItemConvert[0], num, num3);
                g.setColor(12281361);
                g.drawRect(num, num3, indexSize, indexSize);
                mFont.tahoma_7_yellow.drawString(g, "(+" + arrItemConvert[0].upgrade + ")", num - 5, num3 + indexSize / 2 - 5, mFont.RIGHT);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num3 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num3 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(num, num3, indexSize, indexSize);
            }

            SmallImage.drawSmallImage(g, 942, num + 43, ystart - 15, 0, StaticObj.VCENTER_HCENTER);
            if (arrItemConvert[1] != null)
            {
                Item item = arrItemConvert[1].clone();
                if (arrItemConvert[0] != null && arrItemConvert[0].template.type == item.template.type && arrItemConvert[1].template.level >= arrItemConvert[0].template.level)
                {
                    item.upgrade = arrItemConvert[0].upgrade;
                }

                paintItem(g, item, num2, num3);
                g.setColor(12281361);
                g.drawRect(num2, num3, indexSize, indexSize);
                mFont.tahoma_7_yellow.drawString(g, "(+" + item.upgrade + ")", num2 + indexSize + 10, num3 + indexSize / 2 - 5, mFont.LEFT);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num2 - 1, num3 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num2 + indexSize / 2, num3 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(num2, num3, indexSize, indexSize);
            }

            if (indexTitle == 1)
            {
                if (indexSelect == 0)
                {
                    g.setColor(16777215);
                    g.drawRect(num, num3, indexSize, indexSize);
                }

                if (indexSelect == 1)
                {
                    g.setColor(16777215);
                    g.drawRect(num2, num3, indexSize, indexSize);
                }
            }

            g.setColor(0);
            g.fillRect(xstart - 1, ystart - 1, columns * indexSize + 3, rows * indexSize + 3);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            Item item2 = arrItemConvert[2];
            if (item2 != null)
            {
                int num4 = 0 / columns;
                int num5 = -(num4 * columns);
                if (!item2.isLock)
                {
                    g.setColor(12083);
                    g.fillRect(xstart + num5 * indexSize + 1, ystart + num4 * indexSize + 1, indexSize - 1, indexSize - 1);
                }

                SmallImage.drawSmallImage(g, item2.template.iconID, xstart + num5 * indexSize + indexSize / 2, ystart + num4 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }

            if (Main.isPC)
            {
                mFont.tahoma_7_white.drawString(g, "- " + mResources.UPGRADE_CONDITION[0], xstart, ystart + rows * indexSize + 10, mFont.LEFT);
                mFont.tahoma_7_white.drawString(g, "- " + mResources.UPGRADE_CONDITION[1], xstart, ystart + rows * indexSize + 22, mFont.LEFT);
                mFont.tahoma_7_white.drawString(g, "- " + mResources.UPGRADE_CONDITION[2], xstart, ystart + rows * indexSize + 34, mFont.LEFT);
            }
            else
            {
                paintInfoHelpCheNgoc(g, new string[2] { "- " + mResources.UPGRADE_CONDITION[0] + " " + mResources.UPGRADE_CONDITION[1], "- " + mResources.UPGRADE_CONDITION[2] }, 3);
            }

            if (indexTitle == 2)
            {
                int num6 = indexSelect / columns;
                int num7 = indexSelect - num6 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num7 * indexSize, ystart + num6 * indexSize, indexSize, indexSize);
            }
        }
        catch (Exception)
        {
            Out.println("loi paintCOnvert");
        }
    }

    private void paintItem(mGraphics g, Item item, int x, int y, int dUpgrade, int dR)
    {
        if (!item.isTypeMounts())
        {
            int num = item.upgrade + dUpgrade;
            if (num > 0)
            {
                if (num >= 4)
                {
                    g.setColor(color[(num <= 16) ? num : 16]);
                    g.fillRect(x + 1 + dR, y + 1 + dR, indexSize - 1 - 2 * dR, indexSize - 1 - 2 * dR);
                    SmallImage.drawSmallImage(g, 154, x + indexSize / 2, y + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
                else
                {
                    paintItemBackground(g, item, x, y, dR);
                }

                if (num >= 1)
                {
                    paintUpgradeEffect(x + indexSize / 2, y + indexSize / 2, num, g);
                }
            }
            else
            {
                paintItemBackground(g, item, x, y, dR);
            }
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            item.indexFrame = (item.indexFrame + 1) % 3;
        }

        if (item.isNewitem())
        {
            g.drawImage(GameCanvas.backitem, x + indexSize / 2, y + indexSize / 2, 3);
        }

        SmallImage.drawSmallImageNew(g, item.template.iconID, x + indexSize / 2, y + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER, item.indexFrame);
    }

    private void paintItemBackground(mGraphics g, Item item, int x, int y, int dR)
    {
        if (!item.isLock)
        {
            g.setColor(12083);
        }
        else
        {
            g.setColor(6425);
        }

        g.fillRect(x + 1 + dR, y + 1 + dR, indexSize - 2 - 2 * dR, indexSize - 2 - 2 * dR);
        SmallImage.drawSmallImage(g, 154, x + indexSize / 2, y + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
    }

    public void paintSplit(mGraphics g, string[] titles)
    {
        try
        {
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 34 + indexSize;
            int num = popupX + 74;
            int num2 = ystart - indexSize - 3;
            rows = 4;
            if (itemSplit != null)
            {
                paintItem(g, itemSplit, num, num2);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num2 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num2 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }

            g.setColor(12281361);
            g.drawRect(num, num2, indexSize, indexSize);
            g.setColor(6425);
            g.fillRect(xstart - 1, ystart - 1, indexSize * columns + 3, indexSize * rows + 3);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            for (int k = 0; k < arrItemSplit.Length; k++)
            {
                Item item = arrItemSplit[k];
                if (item != null)
                {
                    int num3 = k / columns;
                    int num4 = k - num3 * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num4 * indexSize + 1, ystart + num3 * indexSize + 1, indexSize - 1, indexSize - 1);
                    }

                    SmallImage.drawSmallImage(g, item.template.iconID, xstart + num4 * indexSize + indexSize / 2, ystart + num3 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (indexTitle == 1)
            {
                g.setColor(16777215);
                g.drawRect(num, num2, indexSize, indexSize);
            }
            else if (indexTitle == 2)
            {
                int num5 = indexSelect / columns;
                int num6 = indexSelect - num5 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num6 * indexSize, ystart + num5 * indexSize, indexSize, indexSize);
            }

            if (effUpok != null)
            {
                SmallImage.drawSmallImage(g, effUpok.arrEfInfo[indexEff].idImg, num + indexSize / 2 + effUpok.arrEfInfo[indexEff].dx, num2 + indexSize / 2 + effUpok.arrEfInfo[indexEff].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                if (GameCanvas.gameTick % 2 == 0)
                {
                    indexEff++;
                    if (indexEff >= effUpok.arrEfInfo.Length)
                    {
                        indexEff = 0;
                        effUpok = null;
                    }
                }
            }

            if (!isPaintTinhluyen || itemSplit == null)
            {
                return;
            }

            ItemOption itemOption = null;
            for (int l = 0; l < itemSplit.options.size(); l++)
            {
                itemOption = (ItemOption)itemSplit.options.elementAt(l);
                if (itemOption.optionTemplate.id == 85)
                {
                    break;
                }

                itemOption = null;
            }

            if (itemOption != null)
            {
                int[] array = new int[9]
                {
                    60,
                    45,
                    34,
                    26,
                    20,
                    15,
                    11,
                    8,
                    6
                };
                int[] array2 = new int[9]
                {
                    150000,
                    247500,
                    408375,
                    673819,
                    1111801,
                    2056832,
                    4010822,
                    7420021,
                    12243035
                };
                byte[] array3 = new byte[9]
                {
                    3,
                    5,
                    9,
                    4,
                    7,
                    10,
                    5,
                    7,
                    9
                };
                mFont.tahoma_7_yellow.drawString(g, mResources.LEVEL_TINHLUYEN + ": " + NinjaUtil.numberToString(itemOption.param + 1 + string.Empty), xstart + 1, ystart + 114, mFont.LEFT);
                mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT1 + array[itemOption.param] + "%)", xstart + 70, ystart + 114, mFont.LEFT);
                mFont.tahoma_7_yellow.drawString(g, mResources.NEED + ": " + NinjaUtil.numberToString(array2[itemOption.param] + string.Empty) + " " + mResources.YEN + ", " + array3[itemOption.param] + " " + ((itemOption.param < 3) ? mResources.TUTINH[1] : ((itemOption.param >= 6) ? mResources.TUTINH[3] : mResources.TUTINH[2])), xstart + 1, ystart + 125, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CAN_NOT_TINHLUYEN, xstart + 1, ystart + 120, mFont.LEFT);
            }
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void paintTrade(mGraphics g, string[] titles)
    {
        try
        {
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 45;
            rows = 4;
            mFont.tahoma_7_yellow.drawString(g, Char.getMyChar().cName, xstart + 1, ystart - (Main.isPC ? 14 : 26), mFont.LEFT);
            int num = xstart;
            int num2 = ((!Main.isPC) ? (ystart + rows * indexSize) : (ystart + rows * (indexSize + 3) + 8));
            for (int i = 0; i < 3; i++)
            {
                if (i == typeTrade)
                {
                    mFont.tahoma_7_blue1.drawString(g, string.Empty + (i + 1), num + ((!Main.isPC) ? 5 : 2) + i * 20, num2, mFont.LEFT);
                }
                else
                {
                    mFont.tahoma_7_grey.drawString(g, string.Empty + (i + 1), num + ((!Main.isPC) ? 5 : 2) + i * 20, num2, mFont.LEFT);
                }

                if (i < 2)
                {
                    SmallImage.drawSmallImage(g, 942, num + ((!Main.isPC) ? 17 : 14) + i * 20, num2 + 7, 0, StaticObj.VCENTER_HCENTER);
                }
            }

            if (Main.isPC)
            {
                mFont.tahoma_7_white.drawString(g, NinjaUtil.numberToString(string.Empty + coinTrade) + " " + mResources.XU, xstart, ystart + rows * indexSize + 4, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, NinjaUtil.numberToString(string.Empty + coinTrade) + " " + mResources.XU, xstart, ystart - 14, mFont.LEFT);
                mFont.tahoma_7_white.drawString(g, NinjaUtil.numberToString(string.Empty + coinTradeOrder) + " " + mResources.XU, popupX + popupW - 2, ystart - 14, mFont.RIGHT);
            }

            if (typeTrade == 0)
            {
                g.setColor(0);
            }

            if (typeTrade == 1)
            {
                g.setColor(210986);
            }

            if (typeTrade == 2)
            {
                g.setColor(805690);
            }

            g.fillRect(xstart - 1, ystart - 1, indexSize * 3 + 3, indexSize * 4 + 3);
            for (int j = 0; j < rows; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + k * indexSize + indexSize / 2, ystart + j * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + k * indexSize, ystart + j * indexSize, indexSize, indexSize);
                }
            }

            if (indexTitle == 1)
            {
                int num3 = indexSelect / 3;
                int num4 = indexSelect - num3 * 3;
                g.setColor(16777215);
                g.drawRect(xstart + num4 * indexSize, ystart + num3 * indexSize, indexSize, indexSize);
            }

            if (arrItemTradeMe != null)
            {
                for (int l = 0; l < arrItemTradeMe.Length; l++)
                {
                    Item item = arrItemTradeMe[l];
                    if (item != null)
                    {
                        int num5 = l / 3;
                        int num6 = l - num5 * 3;
                        if (!item.isLock)
                        {
                            g.setColor(12083);
                            g.fillRect(xstart + num6 * indexSize + 1, ystart + num5 * indexSize + 1, indexSize - 1, indexSize - 1);
                        }

                        if (GameCanvas.gameTick % 6 == 0)
                        {
                            item.indexFrame = (item.indexFrame + 1) % 3;
                        }

                        if (item.isNewitem())
                        {
                            g.drawImage(GameCanvas.backitem, xstart + num6 * indexSize + indexSize / 2, ystart + num5 * indexSize + indexSize / 2, 3);
                        }

                        SmallImage.drawSmallImageNew(g, item.template.iconID, xstart + num6 * indexSize + indexSize / 2, ystart + num5 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER, item.indexFrame);
                        if (item.quantity > 1)
                        {
                            mFont.number_yellow.drawString(g, string.Empty + item.quantity, xstart + num6 * indexSize + indexSize, ystart + num5 * indexSize + indexSize - mFont.number_yellow.getHeight(), 1);
                        }

                        if (item.quantity > 1)
                        {
                            mFont.number_yellow.drawString(g, string.Empty + item.quantity, xstart + num6 * indexSize + indexSize, ystart + num5 * indexSize + indexSize - mFont.number_yellow.getHeight(), 1);
                        }
                    }
                }
            }

            xstart = popupX + popupW - 2 - indexSize * 3;
            rows = 4;
            mFont.tahoma_7_yellow.drawString(g, tradeName, popupX + popupW - 2, ystart - (Main.isPC ? 14 : 26), mFont.RIGHT);
            num = popupX + popupW - 3 - 60;
            int num7 = 7;
            for (int m = 0; m < 3; m++)
            {
                if (m == typeTradeOrder)
                {
                    mFont.tahoma_7_blue1.drawString(g, string.Empty + (m + 1), num + 2 + m * 20 + num7, num2, mFont.LEFT);
                }
                else
                {
                    mFont.tahoma_7_grey.drawString(g, string.Empty + (m + 1), num + 2 + m * 20 + num7, num2, mFont.LEFT);
                }

                if (m < 2)
                {
                    SmallImage.drawSmallImage(g, 942, num + 14 + m * 20 + num7, num2 + 7, 0, StaticObj.VCENTER_HCENTER);
                }
            }

            if (Main.isPC)
            {
                mFont.tahoma_7_white.drawString(g, NinjaUtil.numberToString(string.Empty + coinTradeOrder) + " " + mResources.XU, popupX + popupW - 2, ystart + rows * indexSize + 4, mFont.RIGHT);
            }

            if (typeTradeOrder == 0)
            {
                g.setColor(0);
            }

            if (typeTradeOrder == 1)
            {
                g.setColor(210986);
            }

            if (typeTradeOrder == 2)
            {
                g.setColor(805690);
            }

            g.fillRect(xstart - 1, ystart - 1, indexSize * 3 + 3, indexSize * 4 + 3);
            for (int n = 0; n < rows; n++)
            {
                for (int num8 = 0; num8 < 3; num8++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + num8 * indexSize + indexSize / 2, ystart + n * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + num8 * indexSize, ystart + n * indexSize, indexSize, indexSize);
                }
            }

            if (indexTitle == 2)
            {
                int num9 = indexSelect / 3;
                int num10 = indexSelect - num9 * 3;
                g.setColor(16777215);
                g.drawRect(xstart + num10 * indexSize, ystart + num9 * indexSize, indexSize, indexSize);
            }

            if (arrItemTradeOrder != null)
            {
                for (int num11 = 0; num11 < arrItemTradeOrder.Length; num11++)
                {
                    Item item2 = arrItemTradeOrder[num11];
                    if (item2 != null)
                    {
                        int num12 = num11 / 3;
                        int num13 = num11 - num12 * 3;
                        if (!item2.isLock)
                        {
                            g.setColor(12083);
                            g.fillRect(xstart + num13 * indexSize + 1, ystart + num12 * indexSize + 1, indexSize - 1, indexSize - 1);
                        }

                        if (GameCanvas.gameTick % 6 == 0)
                        {
                            item2.indexFrame = (item2.indexFrame + 1) % 3;
                        }

                        if (item2.isNewitem())
                        {
                            g.drawImage(GameCanvas.backitem, xstart + num13 * indexSize + indexSize / 2, ystart + num12 * indexSize + indexSize / 2, 3);
                        }

                        SmallImage.drawSmallImageNew(g, item2.template.iconID, xstart + num13 * indexSize + indexSize / 2, ystart + num12 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER, item2.indexFrame);
                        if (item2.quantity > 1)
                        {
                            mFont.number_yellow.drawString(g, string.Empty + item2.quantity, xstart + num13 * indexSize + indexSize, ystart + num12 * indexSize + indexSize - mFont.number_yellow.getHeight(), 1);
                        }

                        if (item2.quantity > 1)
                        {
                            mFont.number_yellow.drawString(g, string.Empty + item2.quantity, xstart + num13 * indexSize + indexSize, ystart + num12 * indexSize + indexSize - mFont.number_yellow.getHeight(), 1);
                        }
                    }
                }
            }

            int num14 = (int)(mSystem.getCurrentTimeMillis() / 1000);
            if (timeTrade - num14 > 0 && typeTrade == 1 && typeTradeOrder == 1)
            {
                mFont.tahoma_7_white.drawString(g, mResources.WAIT + " " + (timeTrade - num14) + " " + mResources.SECOND, popupX + popupW / 2, popupY + popupH - 15, 2);
            }
            else if (typeTrade == 0)
            {
                mFont.tahoma_7_white.drawString(g, mResources.TRADEHELP, popupX + popupW / 2, popupY + popupH - 15, 2);
            }
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void paintInfoHelpCheNgoc(mGraphics g, string[] infoArr, int kc)
    {
        if (infoArr == null || isPaintItemInfo)
        {
            isPaintInfoHelpNotIphone = false;
            return;
        }

        isPaintInfoHelpNotIphone = true;
        resetTranslate(g);
        yhelp = 0;
        int num = 120;
        int num2 = 4 * indexSize - 3 + kc;
        if (num > GameCanvas.w - 4)
        {
            num = GameCanvas.w - 4;
        }

        if (num2 > GameCanvas.h - 4)
        {
            num2 = GameCanvas.h - 4;
        }

        int num3 = gW / 2 - num / 2;
        int num4 = gH / 2 - num2 / 2;
        num3 = popupX + 175;
        num = popupW - 179;
        num4 = popupY + 33;
        g.setColor(0);
        g.fillRect(num3 - 2, num4 - 2, num + 5, num2 + 5);
        g.setColor(13606712);
        g.drawRect(num3 - 1, num4 - 1, num + 2, num2 + 2);
        g.setColor(Paint.COLORBACKGROUND);
        g.fillRect(num3, num4, num, num2);
        scrInfo.setClip(g, num3, num4 + 2, num, num2 - 2);
        int nItem = 3;
        yhelp = num4 + 3;
        mFont tahoma_7b_white = mFont.tahoma_7b_white;
        for (int i = 0; i < infoArr.Length; i++)
        {
            paintDetailHelpInfo(g, tahoma_7b_white, infoArr[i], num3 + 8, yhelp + i * 12, mFont.LEFT, num);
        }

        scrInfo.setStyle(nItem, 12, num3, num4 + 2, num, num2 - 4, true, 1);
        resetTranslate(g);
    }

    public void paintInfoHelpCheNgoc1(mGraphics g, string[] infoArr, int kc)
    {
        isPaintInfoHelpNotIphone = true;
        resetTranslate(g);
        yhelp = 0;
        int num = 120;
        int num2 = 4 * indexSize - 3 + kc;
        if (num > GameCanvas.w - 4)
        {
            num = GameCanvas.w - 4;
        }

        if (num2 > GameCanvas.h - 4)
        {
            num2 = GameCanvas.h - 4;
        }

        int num3 = gW / 2 - num / 2;
        int num4 = gH / 2 - num2 / 2;
        num3 = popupX + 175;
        num = popupW - 179;
        num4 = popupY + 33;
        g.setColor(0);
        g.fillRect(num3 - 2, num4 - 2, num + 5, num2 + 5);
        g.setColor(13606712);
        g.drawRect(num3 - 1, num4 - 1, num + 2, num2 + 2);
        g.setColor(Paint.COLORBACKGROUND);
        g.fillRect(num3, num4, num, num2);
        scrInfo.setClip(g, num3, num4 + 2, num, num2 - 2);
        int nItem = 3;
        yhelp = num4 + 3;
        mFont tahoma_7_yellow = mFont.tahoma_7_yellow;
        for (int i = 0; i < infoArr.Length; i++)
        {
            paintDetailHelpInfo(g, tahoma_7_yellow, infoArr[i], num3 + 8, yhelp + i * 12, mFont.LEFT, num);
        }

        scrInfo.setStyle(nItem, 12, num3, num4 + 2, num, num2 - 4, true, 1);
        resetTranslate(g);
    }

    public void paintDetailHelpInfo(mGraphics g, mFont f, string str, int x, int y, int align, int inforW)
    {
        int num = ((!GameCanvas.isTouch || GameCanvas.w < 320) ? 10 : 20);
        string[] array = f.splitFontArray(str, inforW - num);
        for (int i = 0; i < array.Length; i++)
        {
            if (i == 0)
            {
                f.drawString(g, array[i], x, y, 0);
                continue;
            }

            f.drawString(g, array[i], x, y + i * 12, 0);
            yhelp += 12;
        }
    }

    public void paintUpPearl(mGraphics g, string[] titles)
    {
        try
        {
            resetTranslate(g);
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 32;
            g.setColor(6425);
            rows = 4;
            if (isPaintUpPearl)
            {
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                bool flag = false;
                for (int i = 0; i < arrItemUpPeal.Length; i++)
                {
                    if (arrItemUpPeal[i] != null)
                    {
                        if (arrItemUpPeal[i].isLock)
                        {
                            flag = true;
                        }

                        num += crystals[arrItemUpPeal[i].template.id];
                        num2++;
                    }
                }

                if (num > 0)
                {
                    num3 = crystals.Length - 1;
                    while (num3 >= 0 && num <= crystals[num3])
                    {
                        num3--;
                    }
                }

                if (num3 >= crystals.Length - 1)
                {
                    num3 = crystals.Length - 2;
                }

                if (isTypeXu)
                {
                    if (num2 > 1)
                    {
                        if (Main.isPC)
                        {
                            mFont.tahoma_7_yellow.drawString(g, mResources.CRYSTALOUT + " " + (num3 + 2) + " " + ((!flag) ? string.Empty : mResources.LOCK), xstart + 3, ystart + rows * indexSize + 9, mFont.LEFT);
                            if (coinUpCrystals[num3 + 1] > Char.getMyChar().xu)
                            {
                                mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.COINNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1])), xstart + 3, ystart + rows * indexSize + 21, mFont.LEFT);
                            }
                            else
                            {
                                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COINNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1])), xstart + 3, ystart + rows * indexSize + 21, mFont.LEFT);
                            }

                            mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT + ": " + num * 100 / crystals[num3 + 1] + "%", xstart + 3, ystart + rows * indexSize + 33, mFont.LEFT);
                        }
                        else
                        {
                            string[] array = new string[3]
                            {
                                mResources.CRYSTALOUT + " " + (num3 + 2) + " " + ((!flag) ? string.Empty : mResources.LOCK),
                                null,
                                null
                            };
                            if (coinUpCrystals[num3 + 1] > Char.getMyChar().xu)
                            {
                                array[1] = mResources.replace(mResources.COINNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1]));
                            }
                            else
                            {
                                array[1] = mResources.replace(mResources.COINNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1]));
                            }

                            array[2] = mResources.PERCENT + ": " + num * 100 / crystals[num3 + 1] + "%";
                            paintInfoHelpCheNgoc1(g, array, 0);
                        }
                    }
                    else if (Main.isPC)
                    {
                        for (int j = 1; j <= 2; j++)
                        {
                            mFont.tahoma_7_white.drawString(g, mResources.HELPCRYSTAL[j], xstart + 3, ystart + rows * indexSize + 5 + (j - 1) * 12 - 4, mFont.LEFT);
                        }
                    }
                    else
                    {
                        string[] array2 = new string[2];
                        for (int k = 0; k < 2; k++)
                        {
                            array2[k] = mResources.HELPCRYSTAL[k];
                        }

                        paintInfoHelpCheNgoc(g, array2, 1);
                    }
                }
                else if (num2 > 1)
                {
                    if (Main.isPC)
                    {
                        mFont.tahoma_7_yellow.drawString(g, mResources.CRYSTALOUT + " " + (num3 + 2) + " " + mResources.LOCK, xstart + 3, ystart + rows * indexSize + 9, mFont.LEFT);
                        if (coinUpCrystals[num3 + 1] > Char.getMyChar().xu + Char.getMyChar().yen)
                        {
                            mFont.tahoma_7_red.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1])), xstart + 3, ystart + rows * indexSize + 21, mFont.LEFT);
                        }
                        else
                        {
                            mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1])), xstart + 3, ystart + rows * indexSize + 21, mFont.LEFT);
                        }

                        mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT + ": " + num * 100 / crystals[num3 + 1] + "%", xstart + 3, ystart + rows * indexSize + 33, mFont.LEFT);
                    }
                    else
                    {
                        string[] array3 = new string[3]
                        {
                            mResources.CRYSTALOUT + " " + (num3 + 2) + " " + mResources.LOCK,
                            null,
                            null
                        };
                        if (coinUpCrystals[num3 + 1] > Char.getMyChar().xu + Char.getMyChar().yen)
                        {
                            array3[1] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1]));
                        }
                        else
                        {
                            array3[1] = mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(string.Empty + coinUpCrystals[num3 + 1]));
                        }

                        array3[2] = mResources.PERCENT + ": " + num * 100 / crystals[num3 + 1] + "%";
                        paintInfoHelpCheNgoc1(g, array3, 0);
                    }
                }
                else if (Main.isPC)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        mFont.tahoma_7_white.drawString(g, mResources.HELPCRYSTAL[l], xstart + 3, ystart + rows * indexSize + 5 + l * 12, mFont.LEFT);
                    }
                }
                else
                {
                    paintInfoHelpCheNgoc(g, mResources.HELPCRYSTAL, 1);
                }
            }
            else
            {
                mFont.tahoma_7_white.drawString(g, mResources.LUYEN_THACH[0], xstart + 3, ystart + rows * indexSize + 9, mFont.LEFT);
                mFont.tahoma_7_white.drawString(g, mResources.LUYEN_THACH[1], xstart + 3, ystart + rows * indexSize + 21, mFont.LEFT);
                mFont.tahoma_7_white.drawString(g, mResources.LUYEN_THACH[2], xstart + 3, ystart + rows * indexSize + 33, mFont.LEFT);
            }

            g.setColor(0);
            g.fillRect(xstart, ystart, columns * indexSize + 1, rows * indexSize + 1);
            for (int m = 0; m < rows; m++)
            {
                for (int n = 0; n < columns; n++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + n * indexSize + indexSize / 2, ystart + m * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + n * indexSize, ystart + m * indexSize, indexSize, indexSize);
                }
            }

            for (int num4 = 0; num4 < arrItemUpPeal.Length; num4++)
            {
                Item item = arrItemUpPeal[num4];
                if (item != null)
                {
                    int num5 = num4 / columns;
                    int num6 = num4 - num5 * columns;
                    if (!item.isLock)
                    {
                        g.setColor(4543829);
                        g.fillRect(xstart + num6 * indexSize + 1, ystart + num5 * indexSize + 1, indexSize - 1, indexSize - 1);
                    }

                    SmallImage.drawSmallImage(g, item.template.iconID, xstart + num6 * indexSize + indexSize / 2, ystart + num5 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (indexTitle > 0)
            {
                int num7 = indexSelect / columns;
                int num8 = indexSelect - num7 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num8 * indexSize, ystart + num7 * indexSize, indexSize, indexSize);
            }

            if (effUpok == null)
            {
                return;
            }

            SmallImage.drawSmallImage(g, effUpok.arrEfInfo[indexEff].idImg, xstart + indexSize / 2 + effUpok.arrEfInfo[indexEff].dx + 1, ystart + indexSize / 2 + 9 + effUpok.arrEfInfo[indexEff].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            if (GameCanvas.gameTick % 2 == 0)
            {
                indexEff++;
                if (indexEff >= effUpok.arrEfInfo.Length)
                {
                    indexEff = 0;
                    effUpok = null;
                }
            }
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void paintUIEliteShop(mGraphics g)
    {
        if (isPaintEliteShop)
        {
            paintItemList(g, mResources.TITLEELITESHOP, arrItemElites);
        }
    }

    public void paintUIStore(mGraphics g)
    {
        if (isPaintStore)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLESTORE, arrItemStore);
            }
            else if (indexMenu == 1)
            {
                paintItemList(g, mResources.TITLESTORE, arrItemBook);
            }
            else if (indexMenu == 2)
            {
                paintItemList(g, mResources.TITLESTORE, arrItemFashion);
            }
            else if (indexMenu == 3)
            {
                paintItemList(g, mResources.TITLESTORE, arrItemClanShop);
            }
            else if (indexMenu == 52)
            {
                paintShopOnline(g, arrItemFashion);
            }
        }
    }

    public void paintUILuckySprin(mGraphics g)
    {
        if (!isPaintLuckySpin)
        {
            return;
        }

        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        int num = popupW;
        if (GameCanvas.isTouchControlLargeScreen)
        {
            num = popupW / 2 + 20;
        }

        g.setColor(0);
        g.fillRect(popupX + 7, popupY + 31, num - 14, popupH - 58);
        g.setColor(13606712);
        g.drawRect(popupX + 8, popupY + 32, num - 16, popupH - 60);
        g.setColor(Paint.COLORBACKGROUND);
        g.fillRect(popupX + 9, popupY + 33, num - 18, popupH - 62);
        paintTitle(g, mResources.LATQUA, false);
        int num2 = 40 - ((!Main.isPC) ? 3 : 0);
        int num3 = 35;
        int num4 = (Main.isPC ? 1 : 3);
        int num5 = ((!Main.isPC) ? 2 : 0);
        int num6 = 3;
        xstart = popupX + 33;
        ystart = popupY + 40;
        for (int i = 0; i < num6; i++)
        {
            for (int j = 0; j < num6; j++)
            {
                g.setColor(Paint.COLORDARK);
                g.fillRect(xstart + j * num2, ystart + 10 + i * num2, num3 - 6, num3 - 6);
                g.setColor(10249521);
                g.drawRect(xstart + j * num2, ystart + 10 + i * num2, num3 - 6, num3 - 6);
                g.setColor(10040064);
                g.fillRect(xstart + j * num2 + 2, ystart + 12 + i * num2, num3 - 9, num3 - 9);
                g.setColor(Paint.COLORDARK);
                g.fillRect(xstart + j * num2 + 4, ystart + 14 + i * num2, num3 - 13, num3 - 13);
                SmallImage.drawSmallImage(g, 1414, xstart + j * num2 + num2 / 2 - 5 + num5, ystart + i * num2 + num2 / 2 + 4 + num4, 0, StaticObj.VCENTER_HCENTER);
            }
        }

        num5 = ((num5 != 0) ? 1 : 0);
        num4 = ((num4 != 0) ? 1 : 0);
        for (int k = 0; k < 9; k++)
        {
            int num7 = k / num6;
            int num8 = k - num7 * num6;
            if (arrItemSprin != null)
            {
                g.setColor(6425);
                g.fillRect(xstart + num8 * num2 + 4, ystart + 14 + num7 * num2, num3 - 13, num3 - 13);
                g.setColor((k != indexSelect) ? 10249521 : 16777215);
                g.drawRect(xstart + num8 * num2 + 4, ystart + 14 + num7 * num2, num3 - 14, num3 - 14);
                SmallImage.drawSmallImage(g, 154, xstart + num8 * num2 + num3 / 2 - 3 + num5, ystart + 7 + num7 * num2 + num3 / 2 + num4, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                if (mSystem.getCurrentTimeMillis() - timePoint < 1000)
                {
                    if (k == indexCard)
                    {
                        SmallImage.drawSmallImage(g, ItemTemplates.getIcon(arrItemSprin[indexCard]), (Main.isPC ? 1 : 0) + num5 + xstart + num8 * num2 + num3 / 2 - 3, num4 + ystart + 7 + num7 * num2 + num3 / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 1414, num5 + xstart + num8 * num2 + num3 / 2 - 2 + ((!Main.isPC) ? (-1) : 0), num4 + ystart + 7 + num7 * num2 + num3 / 2, 0, StaticObj.VCENTER_HCENTER);
                    }
                }
                else if (arrItemSprin[k] < 0 || arrItemSprin[k] >= ItemTemplates.itemTemplates.size())
                {
                    SmallImage.drawSmallImage(g, ItemTemplates.getIcon(242), (Main.isPC ? 1 : 0) + num5 + xstart + num8 * num2 + num3 / 2 - 3, ystart + 7 + num7 * num2 + num3 / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
                else
                {
                    SmallImage.drawSmallImage(g, ItemTemplates.getIcon(arrItemSprin[k]), (Main.isPC ? 1 : 0) + num5 + xstart + num8 * num2 + num3 / 2 - 3, ystart + 7 + num7 * num2 + num3 / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }

                if (indexCard == k && yenTemp > 0)
                {
                    yenValue[k] = yenTemp + string.Empty;
                }

                if (indexTitle == 1)
                {
                    if (indexCard == k && GameCanvas.gameTick % 10 > 4)
                    {
                        g.setColor(13408563);
                        g.drawRect(xstart + num8 * num2, ystart + 10 + num7 * num2, num3 - 6, num3 - 6);
                    }
                    else if (k == indexSelect)
                    {
                        g.setColor(16777215);
                        g.drawRect(xstart + num8 * num2, ystart + 10 + num7 * num2, num3 - 6, num3 - 6);
                    }
                    else
                    {
                        g.setColor(Paint.COLORLIGHT);
                        g.drawRect(xstart + num8 * num2, ystart + 10 + num7 * num2, num3 - 6, num3 - 6);
                    }
                }
            }
            else if (indexTitle == 1)
            {
                g.setColor((k != indexSelect) ? Paint.COLORLIGHT : 16777215);
                g.drawRect(xstart + num8 * num2, ystart + 10 + num7 * num2, num3 - 6, num3 - 6);
            }
        }

        mFont.tahoma_7_yellow.drawString(g, mResources.LUOTLATQUA + numSprinLeft, popupX + popupW / 2, popupY + popupH - 20, mFont.CENTER);
    }

    public void paintNonNam(mGraphics g)
    {
        if (isPaintNonNam)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemNonNam);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLENON);
            }
        }
    }

    public void paintNonNu(mGraphics g)
    {
        if (isPaintNonNu)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemNonNu);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLENON);
            }
        }
    }

    public void paintAoNam(mGraphics g)
    {
        if (isPaintAoNam)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemAoNam);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEAO);
            }
        }
    }

    public void paintAoNu(mGraphics g)
    {
        if (isPaintAoNu)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemAoNu);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEAO);
            }
        }
    }

    public void paintGangTayNam(mGraphics g)
    {
        if (isPaintGangTayNam)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemGangTayNam);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGANGTAY);
            }
        }
    }

    public void paintGangTayNu(mGraphics g)
    {
        if (isPaintGangTayNu)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemGangTayNu);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGANGTAY);
            }
        }
    }

    public void paintQuanNam(mGraphics g)
    {
        if (isPaintQuanNam)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemQuanNam);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEQUAN);
            }
        }
    }

    public void paintQuanNu(mGraphics g)
    {
        if (isPaintQuanNu)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLEQUAN, arrItemQuanNu);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEQUAN);
            }
        }
    }

    public void paintGiayNam(mGraphics g)
    {
        if (isPaintGiayNam)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemGiayNam);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGIAY);
            }
        }
    }

    public void paintGiayNu(mGraphics g)
    {
        if (isPaintGiayNu)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLECLOTHE, arrItemGiayNu);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGIAY);
            }
        }
    }

    public void paintLien(mGraphics g)
    {
        if (isPaintLien)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLEDAYCHUYEN, arrItemLien);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEDAYCHUYEN);
            }
        }
    }

    public void paintNhan(mGraphics g)
    {
        if (isPaintNhan)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLENHAN, arrItemNhan);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLENHAN);
            }
        }
    }

    public void paintNgocBoi(mGraphics g)
    {
        if (isPaintNgocBoi)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLENGOCBOI, arrItemNgocBoi);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLENGOCBOI);
            }
        }
    }

    public void paintPhu(mGraphics g)
    {
        if (isPaintPhu)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLEPHU, arrItemPhu);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEPHU);
            }
        }
    }

    public void paintBinhkhi(mGraphics g)
    {
        if (isPaintWeapon)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLEWEAPON, arrItemWeapon);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEWEAPON);
            }
        }
    }

    public void paintThuoc(mGraphics g)
    {
        if (isPaintStack)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLESTACK, arrItemStack);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLESTACK);
            }
        }
    }

    public void paintThuocKhoa(mGraphics g)
    {
        if (isPaintStackLock)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLESTACKLOCK, arrItemStackLock);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLESTACKLOCK);
            }
        }
    }

    public void paintTaphoa(mGraphics g)
    {
        if (isPaintGrocery)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLEGROCERY, arrItemGrocery);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGROCERY);
            }
        }
    }

    public void paintTaphoakhoa(mGraphics g)
    {
        if (isPaintGroceryLock)
        {
            if (indexMenu == 0)
            {
                paintItemList(g, mResources.TITLEGROCERYLOCK, arrItemGroceryLock);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGROCERYLOCK);
            }
        }
    }

    public void paintGianHang(mGraphics g)
    {
        if (isPaintAuctionSale)
        {
            if (indexMenu == 0)
            {
                paintStands(g, mResources.TITLESTAND);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLESTAND);
            }
        }
    }

    public void paintChuyenHoa(mGraphics g)
    {
        if (isPaintConvert)
        {
            if (indexMenu == 0)
            {
                paintConvert(g, mResources.TITLECONVERT);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEUPGRADE);
            }
        }
    }

    public void paintCuonghoa(mGraphics g)
    {
        if (isPaintUpGrade)
        {
            if (indexMenu == 0)
            {
                paintUpgrade(g, mResources.TITLEUPGRADE);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEUPGRADE);
            }
        }
    }

    public void paintTachHT(mGraphics g)
    {
        if (isPaintSplit)
        {
            if (indexMenu == 0)
            {
                paintSplit(g, mResources.TITLESPLIT);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLESPLIT);
            }
        }
    }

    public void paintLuyenThu(mGraphics g)
    {
        if (isPaintDichChuyen)
        {
            if (indexMenu == 0)
            {
                paintSplit(g, mResources.TITLELUYENTHU);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLELUYENTHU);
            }
        }
    }

    public void paintLuyenAo(mGraphics g)
    {
        if (isPaintTinhluyen)
        {
            if (indexMenu == 0)
            {
                paintSplit(g, mResources.TITLELUYENAO);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLELUYENAO);
            }
        }
    }

    public void paintItemTrade(mGraphics g)
    {
        if (isPaintTrade)
        {
            if (indexMenu == 0)
            {
                paintTrade(g, mResources.TITLETRADE);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLETRADE);
            }
        }
    }

    public void paintGhepngoc(mGraphics g)
    {
        if (isPaintUpPearl)
        {
            if (indexMenu == 0)
            {
                paintUpPearl(g, mResources.TITLEUPPEARL);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEUPPEARL);
            }
        }
    }

    public void paintLuyenThach(mGraphics g)
    {
        if (isPaintLuyenThach)
        {
            if (indexMenu == 0)
            {
                paintUpPearl(g, mResources.TITLELUYENTHACH);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLELUYENTHACH);
            }
        }
    }

    public void paintRuong(mGraphics g)
    {
        if (isPaintBox)
        {
            if (indexMenu == 0)
            {
                paintBox(g, mResources.TITLEBOX);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEBOX);
            }
        }
    }

    public void paintItemDell(mGraphics g)
    {
        if (isPaintItemDell)
        {
            if (indexMenu == 0)
            {
                paintItemDell(g, mResources.TITLEDELL);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEDELL);
            }
        }
    }

    public void paintTiemNang(mGraphics g)
    {
        if (indexMenu != 2)
        {
            return;
        }

        g.translate(-g.getTranslateX(), -g.getTranslateY());
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.MENUME[indexMenu], true);
        mFont.tahoma_8b.drawString(g, mResources.POTENTIAL_POINT, popupX + 10, popupY + 33, 0);
        mFont.tahoma_8b.drawString(g, string.Empty + Char.getMyChar().pPoint, popupX + popupW - 10, popupY + 33, mFont.RIGHT);
        int num = (popupH - 80) / 5;
        for (int i = 0; i < Char.getMyChar().potential.Length; i++)
        {
            g.setColor(Paint.COLORBORDER);
            if (indexTitle > 0 && indexTitle - 1 == i)
            {
                g.setColor(Paint.COLORDARK);
                g.fillRect(popupX + 5, popupY + 52 + i * (num + 4), popupW - 10, num);
                g.setColor(Paint.COLORFOCUS);
            }

            g.drawRect(popupX + 5, popupY + 52 + i * (num + 4), popupW - 10, num);
            float cellCenterY = popupY + 52 + i * (num + 4) + num / 2f;
            try
            {
                TextCalib.UseBox("potentialValue", popupX + popupW - 10, cellCenterY);
                mFont.tahoma_7b_white.drawString(g, string.Empty + Char.getMyChar().potential[i], popupX + popupW - 10, (int)cellCenterY, mFont.RIGHT);
                TextCalib.UseBox("potentialLabel", popupX + 10, cellCenterY);
                mFont.tahoma_7b_white.drawString(g, mResources.NAMEPOTENTIAL[i], popupX + 10, (int)cellCenterY, 0);
            }
            finally
            {
                TextCalib.Clear();
            }
        }

        if (indexTitle > 0)
        {
            switch (Char.getMyChar().nClass.classId)
            {
                case 0:
                    mFont.tahoma_7_green.drawString(g, mResources.HELPPOTENTIAL1[0], popupX + 10, popupY + 52 + (num - 10) / 2 + 4 * (num + 4), 0);
                    break;
                case 1:
                case 3:
                case 5:
                    mFont.tahoma_7_green.drawString(g, mResources.HELPPOTENTIAL2[indexTitle - 1], popupX + 10, popupY + 52 + (num - 10) / 2 + 4 * (num + 4), 0);
                    break;
                case 2:
                case 4:
                case 6:
                    mFont.tahoma_7_green.drawString(g, mResources.HELPPOTENTIAL3[indexTitle - 1], popupX + 10, popupY + 52 + (num - 10) / 2 + 4 * (num + 4), 0);
                    break;
            }
        }
    }

    public void paintTitle(mGraphics g, string title, bool arrow)
    {
        if (!svTitle.Equals(string.Empty))
        {
            title = svTitle;
        }

        int num = 0;
        num = gW / 2;
        g.setColor(Paint.COLORDARK);
        g.fillRoundRect(num - mFont.tahoma_8b.getWidth(title) / 2 - 12, popupY + 5, mFont.tahoma_8b.getWidth(title) + 22, 24, 6, 6);
        if ((indexTitle == 0 || GameCanvas.isTouch) && arrow)
        {
            SmallImage.drawSmallImage(g, 989, num - mFont.tahoma_8b.getWidth(title) / 2 - 15 - 7 - ((GameCanvas.gameTick % 8 <= 3) ? 2 : 0), popupY + 17, 2, StaticObj.VCENTER_HCENTER);
            SmallImage.drawSmallImage(g, 989, num + mFont.tahoma_8b.getWidth(title) / 2 + 15 + 5 + ((GameCanvas.gameTick % 8 <= 3) ? 2 : 0), popupY + 17, 0, StaticObj.VCENTER_HCENTER);
        }

        if (indexTitle == 0)
        {
            g.setColor(Paint.COLORFOCUS);
        }
        else
        {
            g.setColor(Paint.COLORBORDER);
        }

        g.drawRoundRect(num - mFont.tahoma_8b.getWidth(title) / 2 - 12, popupY + 5, mFont.tahoma_8b.getWidth(title) + 22, 24, 6, 6);
        try
        {
            TextCalib.UseBox("tabHeader", num, popupY + 17);
            mFont.tahoma_8b.drawString(g, title, num, popupY + 9, 2);
        }
        finally
        {
            TextCalib.Clear();
        }
    }

    public void paintZone(mGraphics g)
    {
        if (!isPaintZone)
        {
            return;
        }

        resetTranslate(g);
        paintItemFrame(g, new string[1] { mResources.ZONE }, false);
        mFont.tahoma_7_yellow.drawString(g, TileMap.mapName, popupX + popupW / 2, popupY + popupH - 25, mFont.CENTER);
        if (indexSelect >= 0 && indexSelect < zones.Length)
        {
            mFont.tahoma_7_white.drawString(g, mResources.POPULATION + ": " + zones[indexSelect] + ", " + mResources.PARTY + ": " + pts[indexSelect], popupX + popupW / 2, popupY + popupH - 13, mFont.CENTER);
        }

        int num = indexSelect / zoneCol;
        int num2 = indexSelect % zoneCol;
        rows = zones.Length / zoneCol;
        if (zones.Length % zoneCol > 0)
        {
            rows++;
        }

        if (rows < 5)
        {
            rows = 5;
        }

        int num3 = (Main.isPC ? 5 : 4);
        scrMain.setStyle(rows, indexSize, xstart, ystart, columns * indexSize + 2, num3 * indexSize, true, 6);
        scrMain.height = num3 * indexSize + 2;
        scrMain.setClip(g);
        int num4 = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < zoneCol; j++)
            {
                g.setColor(12281361);
                g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                if (num4 < zones.Length)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    mFont zoneFont = zones[num4] >= 20 ? mFont.tahoma_7b_red : zones[num4] >= 15 ? mFont.tahoma_7b_yellow : mFont.tahoma_7b_white;
                    float zoneCenterX = xstart + j * indexSize + indexSize / 2f;
                    float zoneCenterY = ystart + i * indexSize + indexSize / 2f;
                    try
                    {
                        TextCalib.UseBox("zone", zoneCenterX, zoneCenterY);
                        zoneFont.drawString(g, string.Empty + num4, (int)zoneCenterX, (int)zoneCenterY, mFont.CENTER);
                    }
                    finally
                    {
                        TextCalib.Clear();
                    }

                    num4++;
                }
            }
        }

        if (indexSelect >= 0)
        {
            g.setColor(16777215);
            g.drawRect(xstart + num2 * indexSize, ystart + num * indexSize, indexSize, indexSize);
        }
    }

    public void paintAlert(mGraphics g)
    {
        if (texts == null || !isPaintAlert)
        {
            return;
        }

        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        if (textsTitle != null)
        {
            paintTitle(g, textsTitle, isPaintMessage);
        }

        xstart = popupX + 15;
        ystart = popupY + 15;
        if (textsTitle != null)
        {
            ystart += 10;
        }

        indexRowMax = texts.size();
        scrMain.setStyle(indexRowMax, 12, popupX, ystart + 12, popupW, popupH - 42 - ((textsTitle != null) ? 10 : 0), true, 1);
        scrMain.setClip(g);
        yPaint = ystart;
        mFont mFont2 = mFont.tahoma_7_white;
        for (int i = 0; i < texts.size(); i++)
        {
            string text = (string)texts.elementAt(i);
            if (text == null || texts == null || mFont2 == null)
            {
                break;
            }

            if (text.StartsWith("c"))
            {
                if (text.StartsWith("c0"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_white;
                }
                else if (text.StartsWith("c1"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_yellow;
                }
                else if (text.StartsWith("c2"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_white;
                }
                else if (text.StartsWith("c3"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_yellow;
                }
                else if (text.StartsWith("c4"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_red;
                }
                else if (text.StartsWith("c5"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_red;
                }
                else if (text.StartsWith("c6"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_grey;
                }
                else if (text.StartsWith("c7"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_blue;
                }
                else if (text.StartsWith("c8"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_blue;
                }
                else if (text.StartsWith("c9"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_green;
                }
            }

            // Offline terminal: a row holding NsoTerminal.SplitMark is drawn in two colours (chat name yellow, text white).
            int split = text.IndexOf(NsoTerminal.SplitMark);
            if (split >= 0)
            {
                string head = text.Substring(0, split);
                yPaint += 12;
                mFont.tahoma_7_yellow.drawString(g, head, xstart + 5, yPaint, 0);
                mFont.tahoma_7_white.drawString(g, text.Substring(split + 1), xstart + 5 + mFont.tahoma_7_yellow.getWidth(head), yPaint, 0);
                continue;
            }

            mFont2.drawString(g, text, xstart + 5, yPaint += 12, 0);
        }

        if (indexRow >= 0)
        {
            SmallImage.drawSmallImage(g, 942, xstart - 5, ystart + 12 + 2 + indexRow * 12, 0, StaticObj.TOP_LEFT);
        }
    }

    public void paintFindTeam(mGraphics g)
    {
        if (!isPaintFindTeam)
        {
            return;
        }

        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.FINDTEAM, false);
        xstart = popupX + 5;
        ystart = popupY + 40;
        if (vPtMap.size() == 0)
        {
            mFont.tahoma_7_white.drawString(g, mResources.NOT_TEAM_MAP, popupX + popupW / 2, popupY + 40, mFont.CENTER);
        }
        else
        {
            int num = (Main.isPC ? 5 : 4);
            g.setColor(6425);
            g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * num + 8);
            resetTranslate(g);
            scrMain.setStyle(vPtMap.size(), indexSize, xstart, ystart, popupW - 3, indexSize * num + 4, true, 1);
            scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * num + 6);
            indexRowMax = vPtMap.size();
            for (int i = 0; i < vPtMap.size(); i++)
            {
                Party party = (Party)vPtMap.elementAt(i);
                if (indexRow == i)
                {
                    g.setColor(Paint.COLORLIGHT);
                    g.fillRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(16777215);
                    g.drawRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                }
                else
                {
                    g.setColor(Paint.COLORBACKGROUND);
                    g.fillRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(13932896);
                    g.drawRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                }

                SmallImage.drawSmallImage(g, 647, xstart + 12, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                mFont.tahoma_7_white.drawString(g, party.name + " - " + mResources.level + ": " + party.level + " (" + party.size + ")", xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
            }
        }

        paintNumCount(g);
    }

    public void paintTeam(mGraphics g)
    {
        if (!isPaintTeam)
        {
            return;
        }

        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.TEAM, false);
        xstart = popupX + 5;
        ystart = popupY + 40;
        int num = ((!Main.isPC) ? 4 : 5);
        if (vParty.size() == 0)
        {
            mFont.tahoma_7_white.drawString(g, mResources.NOT_TEAM, popupX + popupW / 2, popupY + 40, mFont.CENTER);
        }
        else
        {
            g.setColor(6425);
            g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * num + 8);
            resetTranslate(g);
            scrMain.setStyle(vParty.size(), indexSize, xstart, ystart, popupW - 3, indexSize * num + 4, true, 1);
            scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * num + 6);
            indexRowMax = vParty.size();
            for (int i = 0; i < vParty.size(); i++)
            {
                Party party = (Party)vParty.elementAt(i);
                if (indexRow == i)
                {
                    g.setColor(Paint.COLORLIGHT);
                    g.fillRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(16777215);
                    g.drawRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                }
                else
                {
                    g.setColor(Paint.COLORBACKGROUND);
                    g.fillRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(13932896);
                    g.drawRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                }

                SmallImage.drawSmallImage(g, party.iconId, xstart + 12, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                if (party.c == null)
                {
                    mFont.tahoma_7_green.drawString(g, party.name, xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
                }
                else if (i == 0)
                {
                    mFont.tahoma_7_yellow.drawString(g, party.name + " - " + mResources.level + ": " + party.c.clevel, xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
                }
                else
                {
                    mFont.tahoma_7_white.drawString(g, party.name + " - " + mResources.level + ": " + party.c.clevel, xstart + 22, ystart + i * indexSize + indexSize / 2 - 6, 0);
                }
            }
        }

        paintNumCount(g);
    }

    public void PaintAuctionBuy(mGraphics g)
    {
        if (!isPaintAuctionBuy)
        {
            return;
        }

        int num = popupW;
        if (GameCanvas.isTouchControlLargeScreen)
        {
            num = popupW / 2 + 20;
        }

        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.MENUSTANDS[indexMenu], GameCanvas.isTouch || indexTitle == 0);
        g.setColor(6425);
        g.fillRect(xstart - 2, ystart - 2, num - 6, indexSize * 5 + 4);
        if (arrItemStands == null)
        {
            GameCanvas.paintShukiren(popupX + 90, popupY + 75, g, false);
            mFont.tahoma_7b_white.drawString(g, mResources.PLEASEWAIT, popupX + 90, popupY + 90, 2);
            return;
        }

        ItemStands[] array = arrItemStands;
        xstart = popupX + 5;
        ystart = popupY + 33;
        if (array.Length > 0)
        {
            indexRowMax = array.Length;
            resetTranslate(g);
            scrMain.setStyle(indexRowMax, indexSize, xstart, ystart, num - 3, indexSize * 5, true, 1);
            scrMain.setClip(g, xstart, ystart, num - 3, indexSize * 5 + 2);
            for (int i = 0; i < array.Length; i++)
            {
                ItemStands itemStands = array[i];
                if (itemStands == null || itemStands.item == null || itemStands.item.template == null)
                {
                    continue;
                }

                int num2 = (int)(mSystem.getCurrentTimeMillis() / 1000);
                if (i * indexSize >= scrMain.cmy - indexSize && i * indexSize < scrMain.cmy + (indexSize * 5 + 4))
                {
                    if (indexSelect == i)
                    {
                        g.setColor(Paint.COLORLIGHT);
                        g.fillRect(xstart + 2, ystart + indexSelect * indexSize + 2, num - 15, indexSize - 4);
                        g.setColor(16777215);
                        g.drawRect(xstart + 2, ystart + indexSelect * indexSize + 2, num - 15, indexSize - 4);
                    }
                    else
                    {
                        g.setColor(Paint.COLORBACKGROUND);
                        g.fillRect(xstart + 2, ystart + i * indexSize + 2, num - 15, indexSize - 4);
                        g.setColor(13932896);
                        g.drawRect(xstart + 2, ystart + i * indexSize + 2, num - 15, indexSize - 4);
                    }

                    g.setColor(0);
                    g.fillRect(xstart + 4, ystart + i * indexSize + 4, indexSize - 1, indexSize - 8);
                    g.setColor((indexSelect != i) ? 12281361 : 16777215);
                    g.drawRect(xstart + 4, ystart + i * indexSize + 4, indexSize - 1, indexSize - 8);
                    if (GameCanvas.gameTick % 6 == 0)
                    {
                        itemStands.item.indexFrame = (itemStands.item.indexFrame + 1) % 3;
                    }

                    g.setColor(11403519);
                    if (itemStands.item.isNewitem())
                    {
                        g.drawRect(xstart + 5, ystart + i * indexSize + 5, indexSize - 3, indexSize - 10);
                    }

                    SmallImage.drawSmallImageNew(g, itemStands.item.template.iconID, xstart + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER, itemStands.item.indexFrame);
                    if (itemStands.item.quantity > 1)
                    {
                        mFont.number_yellow.drawString(g, string.Empty + itemStands.item.quantity, xstart + indexSize, ystart + i * indexSize + indexSize / 2 + 2, 1);
                    }

                    int num3 = itemStands.timeEnd - (num2 - itemStands.timeStart);
                    mFont.tahoma_7_white.drawString(g, mResources.SELLER + ": " + itemStands.seller, xstart + indexSize + 7, ystart + i * indexSize + indexSize / 2 - mFont.number_yellow.getHeight() - 2, 0);
                    mFont.tahoma_7_white.drawString(g, mResources.PRICE + ": " + NinjaUtil.numberToString(string.Empty + itemStands.price) + " " + mResources.XU, xstart + indexSize + 7, ystart + i * indexSize + indexSize / 2 - mFont.number_yellow.getHeight() + 9, 0);
                    if (num3 < 60)
                    {
                        mFont.tahoma_7_blue.drawString(g, mResources.NEAR, xstart + num - 30, ystart + i * indexSize + indexSize / 2 - mFont.number_yellow.getHeight() - 2, 2);
                        mFont.tahoma_7_blue.drawString(g, mResources.DEADLINE, xstart + num - 30, ystart + i * indexSize + indexSize / 2 - mFont.number_yellow.getHeight() + 9, 2);
                    }
                    else
                    {
                        mFont.tahoma_7_green.drawString(g, mResources.REMAIN, xstart + num - 30, ystart + i * indexSize + indexSize / 2 - mFont.number_yellow.getHeight() - 2, 2);
                        mFont.tahoma_7_green.drawString(g, NinjaUtil.getTime(num3), xstart + num - 30, ystart + i * indexSize + indexSize / 2 - mFont.number_yellow.getHeight() + 9, 2);
                    }
                }
            }
        }
        else
        {
            indexRowMax = array.Length;
            mFont.tahoma_7_white.drawString(g, mResources.EMPTY_INFO, popupX + num / 2, popupY + 40, mFont.CENTER);
        }

        paintNumCount(g);
    }

    public void paintFriend(mGraphics g)
    {
        if (!isPaintFriend && !isPaintEnemies)
        {
            return;
        }

        string title = ((!isPaintFriend) ? mResources.FRIENDS[1] : mResources.FRIENDS[0]);
        MyVector myVector = ((!isPaintFriend) ? vEnemies : vFriend);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, title, false);
        if (myVector.size() > 0)
        {
            xstart = popupX + 5;
            ystart = popupY + 40;
            int num = (Main.isPC ? 5 : 4);
            g.setColor(6425);
            g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * num + 8);
            resetTranslate(g);
            scrMain.setStyle(myVector.size(), indexSize, xstart, ystart, popupW - 3, indexSize * num + 4, true, 1);
            scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * num + 6);
            indexRowMax = myVector.size();
            int num2 = 0;
            for (int i = 0; i < myVector.size(); i++)
            {
                Friend friend = (Friend)myVector.elementAt(i);
                if (isViewClanMemOnline && friend.type != 3)
                {
                    continue;
                }

                mFont mFont2 = mFont.tahoma_7_grey;
                if (friend.type == 1 || friend.type == 2)
                {
                    mFont2 = mFont.tahoma_7_green;
                }
                else if (friend.type == 3)
                {
                    mFont2 = mFont.tahoma_7_white;
                }
                else if (friend.type == 4)
                {
                    mFont2 = mFont.tahoma_7_red;
                }

                if (i * indexSize >= scrMain.cmy - indexSize && i * indexSize < scrMain.cmy + (indexSize * 5 + 8))
                {
                    if (indexRow == i)
                    {
                        g.setColor(Paint.COLORLIGHT);
                        g.fillRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                        g.setColor(16777215);
                        g.drawRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                    }
                    else
                    {
                        g.setColor(Paint.COLORBACKGROUND);
                        g.fillRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                        g.setColor(13932896);
                        g.drawRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                    }

                    if (friend.type == 4)
                    {
                        if (GameCanvas.gameTick % 10 > 7)
                        {
                            mFont2.drawString(g, friend.friendName, xstart + 8, ystart + i * indexSize + indexSize / 2 - 6, 0);
                        }
                        else
                        {
                            mFont.tahoma_7_yellow.drawString(g, friend.friendName, xstart + 8, ystart + i * indexSize + indexSize / 2 - 6, 0);
                        }

                        mFont.tahoma_7_blue.drawString(g, mResources.CONFIRM_FRIEND, xstart + popupW - 15, ystart + i * indexSize + indexSize / 2 - 6, 1);
                    }
                    else
                    {
                        mFont2.drawString(g, friend.friendName, xstart + 8, ystart + i * indexSize + indexSize / 2 - 6, 0);
                    }
                }

                num2++;
            }

            indexRowMax = num2;
            scrMain.setStyle(num2, indexSize, xstart, ystart, popupW - 3, indexSize * num + 4, true, 1);
        }
        else
        {
            mFont.tahoma_7_white.drawString(g, (!isPaintFriend) ? mResources.YOU_KILLED : mResources.NO_FRIEND, popupX + popupW / 2, popupY + 40, mFont.CENTER);
        }

        paintNumCount(g);
    }

    public void paintTask(mGraphics g)
    {
        if (!isPaintTask)
        {
            return;
        }

        indexRowMax = 0;
        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.TASK[indexMenu], false);
        xstart = popupX + 10;
        ystart = popupY + 32;
        if (indexMenu == 0)
        {
            bool flag = false;
            scrMain.setStyle(indexRowMax, 12, popupX, popupY + 32, popupW, popupH - 40, true, 1);
            scrMain.setClip(g);
            if (Char.getMyChar().taskMaint != null)
            {
                for (int i = 0; i < Char.getMyChar().taskMaint.names.Length; i++)
                {
                    mFont.tahoma_7b_white.drawString(g, Char.getMyChar().taskMaint.names[i], xstart, yPaint = ystart, 0);
                    indexRowMax++;
                }

                int num = 0;
                string empty = string.Empty;
                for (int j = 0; j < Char.getMyChar().taskMaint.subNames.Length; j++)
                {
                    if (Char.getMyChar().taskMaint.subNames[j] != null)
                    {
                        num = j;
                        empty = "- " + Char.getMyChar().taskMaint.subNames[j];
                        if (Char.getMyChar().taskMaint.counts[j] != -1)
                        {
                            if (Char.getMyChar().taskMaint.index == j)
                            {
                                string text = empty;
                                empty = text + " " + Char.getMyChar().taskMaint.count + "/" + Char.getMyChar().taskMaint.counts[j];
                                if (Char.getMyChar().taskMaint.count == Char.getMyChar().taskMaint.counts[j])
                                {
                                    mFont.tahoma_7_white.drawString(g, empty, xstart + 5, yPaint += 12, 0);
                                }
                                else
                                {
                                    mFont mFont2 = mFont.tahoma_7_grey;
                                    if (!flag)
                                    {
                                        flag = true;
                                        mFont2 = mFont.tahoma_7_yellow;
                                    }

                                    mFont2.drawString(g, empty, xstart + 5, yPaint += 12, 0);
                                }
                            }
                            else if (Char.getMyChar().taskMaint.index > j)
                            {
                                string text = empty;
                                empty = text + " " + Char.getMyChar().taskMaint.counts[j] + "/" + Char.getMyChar().taskMaint.counts[j];
                                mFont.tahoma_7_white.drawString(g, empty, xstart + 5, yPaint += 12, 0);
                            }
                            else
                            {
                                empty = empty + " 0/" + Char.getMyChar().taskMaint.counts[j];
                                mFont mFont3 = mFont.tahoma_7_grey;
                                if (!flag)
                                {
                                    flag = true;
                                    mFont3 = mFont.tahoma_7_yellow;
                                }

                                mFont3.drawString(g, empty, xstart + 5, yPaint += 12, 0);
                            }
                        }
                        else if (Char.getMyChar().taskMaint.index > j)
                        {
                            mFont.tahoma_7_white.drawString(g, empty, xstart + 5, yPaint += 12, 0);
                        }
                        else
                        {
                            mFont mFont4 = mFont.tahoma_7_grey;
                            if (!flag)
                            {
                                flag = true;
                                mFont4 = mFont.tahoma_7_yellow;
                            }

                            mFont4.drawString(g, empty, xstart + 5, yPaint += 12, 0);
                        }

                        indexRowMax++;
                    }
                    else if (Char.getMyChar().taskMaint.index <= j)
                    {
                        empty = "- " + Char.getMyChar().taskMaint.subNames[num];
                        mFont mFont5 = mFont.tahoma_7_grey;
                        if (!flag)
                        {
                            flag = true;
                            mFont5 = mFont.tahoma_7_yellow;
                        }

                        mFont5.drawString(g, empty, xstart + 5, yPaint, 0);
                    }
                }

                yPaint += 5;
                for (int k = 0; k < Char.getMyChar().taskMaint.details.Length; k++)
                {
                    mFont.tahoma_7_white.drawString(g, Char.getMyChar().taskMaint.details[k], xstart, yPaint += 12, 0);
                    indexRowMax++;
                }
            }
            else
            {
                sbyte taskMapId = getTaskMapId();
                sbyte taskNpcId = getTaskNpcId();
                string empty2 = string.Empty;
                if (taskMapId == -3 || taskNpcId == -3)
                {
                    empty2 = mResources.DES_TASK[3];
                }
                else if (Char.getMyChar().taskMaint == null && Char.getMyChar().ctaskId == 9 && Char.getMyChar().nClass.classId == 0)
                {
                    empty2 = mResources.TASK_INPUT_CLASS;
                }
                else
                {
                    if (taskNpcId < 0 || taskMapId < 0)
                    {
                        return;
                    }

                    empty2 = mResources.DES_TASK[0] + Npc.arrNpcTemplate[taskNpcId].name + mResources.DES_TASK[1] + TileMap.mapNames[taskMapId] + mResources.DES_TASK[2];
                }

                string[] array = mFont.tahoma_7_white.splitFontArray(empty2, 150);
                for (int l = 0; l < array.Length; l++)
                {
                    if (l == 0)
                    {
                        mFont.tahoma_7_white.drawString(g, array[l], xstart + 5, yPaint = ystart, 0);
                    }
                    else
                    {
                        mFont.tahoma_7_white.drawString(g, array[l], xstart + 5, yPaint += 12, 0);
                    }

                    indexRowMax++;
                }
            }

            if (indexTitle == 1 && indexRow >= 0 && indexRowMax > 0)
            {
                SmallImage.drawSmallImage(g, 942, xstart - 8, ystart + 2 + indexRow * 12, 0, StaticObj.TOP_LEFT);
            }

            scrMain.setStyle(indexRowMax, 12, popupX, popupY + 32, popupW, popupH - 44, true, 1);
        }
        else
        {
            if (indexMenu != 1)
            {
                return;
            }

            yPaint = ystart - 12;
            scrMain.setStyle(Char.getMyChar().taskOrders.size(), 12, popupX, popupY + 32, popupW, popupH - 44, true, 1);
            scrMain.setClip(g);
            indexRowMax = 0;
            for (int m = 0; m < Char.getMyChar().taskOrders.size(); m++)
            {
                TaskOrder taskOrder = (TaskOrder)Char.getMyChar().taskOrders.elementAt(m);
                mFont.tahoma_7b_white.drawString(g, taskOrder.name, xstart + 5, yPaint += 12, 0);
                string text2 = string.Empty;
                if (taskOrder.taskId == 0 || taskOrder.taskId == 3)
                {
                    text2 = mResources.KILL + " " + Mob.arrMobTemplate[taskOrder.killId].name;
                }
                else if (taskOrder.taskId == 1)
                {
                    text2 = mResources.KILLBOSS + " " + Mob.arrMobTemplate[taskOrder.killId].name;
                }
                else if (taskOrder.taskId == 2)
                {
                    text2 = mResources.GIOITHIEU;
                }
                else if (taskOrder.taskId == 4)
                {
                    text2 = mResources.KILLBOSS1 + " " + Mob.arrMobTemplate[taskOrder.killId].name;
                }
                else if (taskOrder.taskId == 5)
                {
                    text2 = mResources.KILLBOSS2 + " " + Mob.arrMobTemplate[taskOrder.killId].name;
                }
                else if (taskOrder.taskId == 6)
                {
                    text2 = mResources.KILLBOSS3;
                }

                if (taskOrder.taskId == 6)
                {
                    if (taskOrder.count == taskOrder.maxCount)
                    {
                        mFont.tahoma_7_white.drawString(g, text2, xstart + 5, yPaint += 12, 0);
                    }
                    else
                    {
                        mFont.tahoma_7_yellow.drawString(g, text2, xstart + 5, yPaint += 12, 0);
                    }
                }
                else if (taskOrder.count == taskOrder.maxCount)
                {
                    mFont.tahoma_7_white.drawString(g, text2 + " " + taskOrder.count + "/" + taskOrder.maxCount, xstart + 5, yPaint += 12, 0);
                }
                else
                {
                    mFont.tahoma_7_yellow.drawString(g, text2 + " " + taskOrder.count + "/" + taskOrder.maxCount, xstart + 5, yPaint += 12, 0);
                }

                indexRowMax += 3;
                inforW = popupW - 25;
                paintMultiLine(g, mFont.tahoma_7_white, taskOrder.description, xstart + 5, yPaint += 12, 0);
                yPaint += 12;
            }

            if (typeba > 0)
            {
                mFont.tahoma_7_white.drawString(g, mResources.noti_hiddenquest, xstart + 5, yPaint += 12, 0);
                switch (typeba)
                {
                    case 1:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_aggressivepoint, xstart + 5, yPaint += 12, 0);
                        break;
                    case 2:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_cave, xstart + 5, yPaint += 12, 0);
                        break;
                    case 3:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_huntingwildbeast, xstart + 5, yPaint += 12, 0);
                        break;
                    case 4:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_upgrade, xstart + 5, yPaint += 12, 0);
                        break;
                    case 5:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_dailyquest, xstart + 5, yPaint += 12, 0);
                        break;
                    case 6:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_battlefield, xstart + 5, yPaint += 12, 0);
                        break;
                    case 7:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_sevenbeasts, xstart + 5, yPaint += 12, 0);
                        break;
                    case 8:
                        mFont.tahoma_7_white.drawString(g, mResources.noti_luckycard, xstart + 5, yPaint += 12, 0);
                        break;
                }
            }

            if (indexTitle == 1 && indexRow >= 0 && indexRowMax > 0)
            {
                SmallImage.drawSmallImage(g, 942, xstart - 8, ystart + 2 + indexRow * 12, 0, StaticObj.TOP_LEFT);
            }

            indexRowMax++;
            scrMain.setStyle(indexRowMax, 12, popupX, popupY + 32, popupW, popupH - 44, true, 1);
        }
    }

    public void paintMultiLine(mGraphics g, mFont f, string[] arr, int x, int y, int align)
    {
        int num = y;
        for (int i = 0; i < arr.Length; i++)
        {
            string text = arr[i];
            if (text.StartsWith("c"))
            {
                if (text.StartsWith("c0"))
                {
                    text = text.Substring(2);
                    f = mFont.tahoma_7_white;
                }
                else if (text.StartsWith("c1"))
                {
                    text = text.Substring(2);
                    f = mFont.tahoma_7_yellow;
                }
                else if (text.StartsWith("c2"))
                {
                    text = text.Substring(2);
                    f = mFont.tahoma_7_green;
                }
            }

            if (i == 0)
            {
                f.drawString(g, text, x, y, align);
                continue;
            }

            if (i * scrMain.ITEM_SIZE + num >= scrMain.cmy - 12 && i * scrMain.ITEM_SIZE < scrMain.cmy + popupH - 44)
            {
                f.drawString(g, text, x, y += 12, align);
            }
            else
            {
                y += 12;
            }

            yPaint += 12;
            indexRowMax++;
        }
    }

    public void paintMultiLine(mGraphics g, mFont f, string str, int x, int y, int align)
    {
        int num = ((!GameCanvas.isTouch || GameCanvas.w < 320) ? 10 : 20);
        int num2 = y;
        string[] array = f.splitFontArray(str, inforW - num);
        for (int i = 0; i < array.Length; i++)
        {
            if (i == 0)
            {
                f.drawString(g, array[i], x, y, align);
                continue;
            }

            if (i * scrMain.ITEM_SIZE + num2 >= scrMain.cmy - 12 && i * scrMain.ITEM_SIZE < scrMain.cmy + popupH - 44)
            {
                f.drawString(g, array[i], x, y += 12, align);
                yPaint += 12;
            }
            else
            {
                y += 12;
            }

            indexRowMax++;
        }
    }

    public void paintMultiLine(mGraphics g, mFont f, string str, int x, int y, int align, int width)
    {
        int num = y;
        string[] array = f.splitFontArray(str, width);
        for (int i = 0; i < array.Length; i++)
        {
            if (i == 0)
            {
                f.drawString(g, array[i], x, y, align);
                continue;
            }

            if (i * scrMain.ITEM_SIZE + num >= scrMain.cmy - 12 && i * scrMain.ITEM_SIZE < scrMain.cmy + popupH - 44)
            {
                f.drawString(g, array[i], x, y += 12, align);
                yPaint += 12;
            }
            else
            {
                y += 12;
            }

            indexRowMax++;
        }
    }

    public void paintItemInfo(mGraphics g)
    {
        g.setClip(0, -200, GameCanvas.w, 200 + GameCanvas.h);
        if (isPaintInfoHelpNotIphone && !Main.isPC)
        {
            return;
        }

        if (GameCanvas.isTouchControlLargeScreen && !isSmallUI() && (isOpenUI() || isPaintPopup() || isPaintUI()))
        {
            setInfoFrameForLargeScreen();
            paintInforFrame(g);
            resetTranslate(g);
            paintMultiLine(g, mFont.tahoma_7_white, mResources.CHOOSE_ITEM, inforX + inforW / 2, inforY + inforH / 2 - 20, 2);
        }

        if (!isPaintItemInfo || itemFocus == null || itemFocus.template == null)
        {
            return;
        }

        Item item = itemFocus;
        if (isViewNext && !itemFocus.isUpMax() && indexMenu == 0)
        {
            item = itemFocus.viewNext(itemFocus.upgrade + 1);
        }

        if (isPaintConvert && indexMenu == 0 && indexTitle == 1 && item.isTypeBody() && item.upgrade == 0 && arrItemConvert[0] != null && arrItemConvert[0].template.type == arrItemConvert[1].template.type && arrItemConvert[1].template.level >= arrItemConvert[0].template.level)
        {
            item = itemFocus.viewNext(arrItemConvert[0].upgrade);
        }

        resetTranslate(g);
        int num;
        if (item.expires != 0 && item.options != null && item.options.size() > 0)
        {
            for (int i = 0; i < item.options.size(); i++)
            {
                ItemOption itemOption = (ItemOption)item.options.elementAt(i);
                num = itemOption.getOptionString().Length * 5;
                if (num > inforW && !GameCanvas.isTouchControlLargeScreen)
                {
                    inforW = num;
                }
            }
        }

        num = mFont.tahoma_7b_white.getWidth(item.template.name) + 10;
        if (num > inforW && !GameCanvas.isTouchControlLargeScreen)
        {
            inforW = num;
        }

        if (inforW > GameCanvas.w - 4)
        {
            inforW = GameCanvas.w - 4;
        }

        if (inforH > GameCanvas.h - 4)
        {
            inforW = GameCanvas.h - 4;
        }

        inforX = gW / 2 - inforW / 2;
        inforY = gH / 2 - inforH / 2;
        setInfoFrameForLargeScreen();
        if (inforX < 2)
        {
            inforX = 2;
        }

        if (inforY < 2)
        {
            inforY = 2;
        }

        paintInforFrame(g);
        if (isPaintClan && indexMenu == 0)
        {
            if (Char.clan != null)
            {
                yPaint = inforY - 9;
                indexRowMax = 2;
                scrInfo.setClip(g, inforX, inforY + 2, inforW, inforH - 2);
                inforW = mFont.tahoma_7_white.getWidth(mResources.CLAN_ITEM[Char.clan.itemLevel][1]) + 10;
                for (int j = 0; j < 2; j++)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.CLAN_ITEM[Char.clan.itemLevel][j], inforX + 8, yPaint += 12, mFont.LEFT);
                }

                if (indexRow >= 0 && (!GameCanvas.isTouch || (GameCanvas.isTouch && GameCanvas.w < 320)))
                {
                    SmallImage.drawSmallImage(g, 942, inforX + 1, inforY + 5 + indexRow * 12, 0, StaticObj.TOP_LEFT);
                }

                scrInfo.setStyle(indexRowMax, 12, inforX, inforY + 2, inforW, inforH - 4, true, 1);
            }

            return;
        }

        if (Main.isPC)
        {
            scrInfo.setClip(g, inforX, inforY + 2, inforW, inforH - 2);
        }
        else
        {
            scrInfo.setClip(g, inforX, inforY + 2, inforW, inforH - 25);
        }

        indexRowMax = 3;
        yPaint = inforY + 3;
        mFont mFont2 = mFont.tahoma_7b_white;
        if (item.isTypeMounts())
        {
            if (item.sys >= 1 && item.sys < 4)
            {
                mFont2 = mFont.tahoma_7b_blue;
            }
            else if (item.sys >= 4 && item.sys < 8)
            {
                mFont2 = mFont.tahoma_7b_green;
            }
            else if (item.sys >= 8 && item.sys < 12)
            {
                mFont2 = mFont.tahoma_7b_yellow;
            }
            else if (item.sys >= 12 && item.sys < 15)
            {
                mFont2 = mFont.tahoma_7b_purple;
            }
            else if (item.sys >= 15)
            {
                mFont2 = mFont.tahoma_7b_red;
            }
        }

        if (item.upgrade >= 1 && item.upgrade < 4)
        {
            mFont2 = mFont.tahoma_7b_blue;
        }
        else if (item.upgrade >= 4 && item.upgrade < 8)
        {
            mFont2 = mFont.tahoma_7b_green;
        }
        else if (item.upgrade >= 8 && item.upgrade < 12)
        {
            mFont2 = mFont.tahoma_7b_yellow;
        }
        else if (item.upgrade >= 12 && item.upgrade < 15)
        {
            mFont2 = mFont.tahoma_7b_purple;
        }
        else if (item.upgrade >= 15)
        {
            mFont2 = mFont.tahoma_7b_red;
        }

        if (item.img != null)
        {
            g.drawRegion(item.img, 0, 0, mGraphics.getImageWidth(item.img), mGraphics.getImageHeight(item.img), 0, inforX + inforW / 2, yPaint + inforH - 10, mGraphics.BOTTOM | mGraphics.HCENTER);
        }

        if (item.isTypeMounts())
        {
            paintMultiLine(g, mFont2, item.template.name + " + " + (item.sys + 1), inforX + 8, yPaint, mFont.LEFT);
        }
        else
        {
            paintMultiLine(g, mFont2, item.template.name + ((item.upgrade <= 0) ? string.Empty : (" +" + item.upgrade)), inforX + 8, yPaint, mFont.LEFT);
        }

        if (item.upgrade >= 15 && !isChop && !item.isTypeMounts())
        {
            string[] array = mFont2.splitFontArray(item.template.name + ((item.upgrade <= 0) ? string.Empty : (" +" + item.upgrade)), inforW - ((!GameCanvas.isTouch || GameCanvas.w < 320) ? 10 : 20));
            if (array.Length > 1)
            {
                yPaint -= 12;
            }

            paintMultiLine(g, mFont.tahoma_7b_white, item.template.name + ((item.upgrade <= 0) ? string.Empty : (" +" + item.upgrade)), inforX + 8, yPaint, mFont.LEFT);
        }

        if (item.isTypeBody())
        {
            yPaint += 12;
            indexRowMax++;
            if (isChop && GameCanvas.gameTick % 5 == 0)
            {
                isChop = !isChop;
            }
            else if (!isChop && GameCanvas.gameTick % 5 == 0)
            {
                isChop = !isChop;
            }

            int num2 = item.upgrade / 2 + 1;
            if (item.upgrade == 0)
            {
                for (int k = 0; k < num2; k++)
                {
                    SmallImage.drawSmallImage(g, 633, inforX + 12 + k * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                }
            }
            else if (item.upgrade >= 1 && item.upgrade < 4)
            {
                for (int l = 0; l < num2; l++)
                {
                    SmallImage.drawSmallImage(g, 625, inforX + 12 + l * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                }

                if (item.upgrade == 3)
                {
                    SmallImage.drawSmallImage(g, 635, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                }
            }
            else if (item.upgrade >= 4 && item.upgrade < 8)
            {
                for (int m = 0; m < num2; m++)
                {
                    SmallImage.drawSmallImage(g, 626, inforX + 12 + m * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                }

                if (item.upgrade % 2 != 0)
                {
                    SmallImage.drawSmallImage(g, 636, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                }
            }
            else if (item.upgrade >= 8 && item.upgrade < 12)
            {
                for (int n = 0; n < num2; n++)
                {
                    if (isChop)
                    {
                        SmallImage.drawSmallImage(g, 627, inforX + 12 + n * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 628, inforX + 12 + n * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                }

                if (item.upgrade % 2 != 0)
                {
                    if (isChop)
                    {
                        SmallImage.drawSmallImage(g, 637, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 638, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                }
            }
            else if (item.upgrade >= 12 && item.upgrade < 15)
            {
                for (int num3 = 0; num3 < num2; num3++)
                {
                    if (isChop)
                    {
                        SmallImage.drawSmallImage(g, 629, inforX + 12 + num3 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 630, inforX + 12 + num3 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                }

                if (item.upgrade % 2 != 0)
                {
                    if (isChop)
                    {
                        SmallImage.drawSmallImage(g, 639, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 640, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                }
            }
            else
            {
                for (int num4 = 0; num4 < num2; num4++)
                {
                    if (isChop)
                    {
                        SmallImage.drawSmallImage(g, 631, inforX + 12 + num4 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 632, inforX + 12 + num4 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                }

                if (item.upgrade % 2 != 0)
                {
                    if (isChop)
                    {
                        SmallImage.drawSmallImage(g, 641, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 642, inforX + 12 + num2 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
                    }
                }
            }
        }
        else if (item.isTypeMounts())
        {
            yPaint += 12;
            int num5 = item.sys + 1;
            for (int num6 = 0; num6 < num5; num6++)
            {
                SmallImage.drawSmallImage(g, 633, inforX + 12 + num6 * 10, yPaint + 5, 0, StaticObj.VCENTER_HCENTER);
            }
        }

        mFont.tahoma_7_white.drawString(g, item.getLockString(), inforX + 8, yPaint += 12, mFont.LEFT);
        if (item.isTypeBody())
        {
            string upgradeString = item.getUpgradeString();
            if (upgradeString != null)
            {
                paintMultiLine(g, mFont.tahoma_7_white, upgradeString, inforX + 8, yPaint += 12, mFont.LEFT);
                indexRowMax++;
            }
        }

        if (item.template.gender == 0 || item.template.gender == 1)
        {
            if (item.template.gender == Char.getMyChar().cgender)
            {
                mFont.tahoma_7_white.drawString(g, mResources.GENDER[item.template.gender], inforX + 8, yPaint += 12, mFont.LEFT);
                indexRowMax++;
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.GENDER[item.template.gender], inforX + 8, yPaint += 12, mFont.LEFT);
                indexRowMax++;
            }
        }

        if (Char.getMyChar().clevel != -1)
        {
            if (Char.getMyChar().clevel >= item.template.level)
            {
                paintMultiLine(g, mFont.tahoma_7_white, mResources.LEVELNEED + " " + item.template.level, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                paintMultiLine(g, mFont.tahoma_7_red, mResources.LEVELNEED + " " + item.template.level, inforX + 8, yPaint += 12, mFont.LEFT);
            }
        }

        if ((item.template.id >= 40 && item.template.id <= 48) || item.template.id == 311 || item.template.id == 375 || item.template.id == 397 || item.template.id == 552 || item.template.id == 558)
        {
            int num7 = 1;
            if (Char.getMyChar().nClass.classId == num7)
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLASSNEED + " " + nClasss[num7].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CLASSNEED + " " + nClasss[num7].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }
        else if ((item.template.id >= 49 && item.template.id <= 57) || item.template.id == 312 || item.template.id == 376 || item.template.id == 398 || item.template.id == 553 || item.template.id == 559)
        {
            int num8 = 2;
            if (Char.getMyChar().nClass.classId == num8)
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLASSNEED + " " + nClasss[num8].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CLASSNEED + " " + nClasss[num8].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }
        else if ((item.template.id >= 58 && item.template.id <= 66) || item.template.id == 313 || item.template.id == 377 || item.template.id == 399 || item.template.id == 554 || item.template.id == 560)
        {
            int num9 = 3;
            if (Char.getMyChar().nClass.classId == num9)
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLASSNEED + " " + nClasss[num9].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CLASSNEED + " " + nClasss[num9].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }
        else if ((item.template.id >= 67 && item.template.id <= 75) || item.template.id == 314 || item.template.id == 378 || item.template.id == 400 || item.template.id == 555 || item.template.id == 561)
        {
            int num10 = 4;
            if (Char.getMyChar().nClass.classId == num10)
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLASSNEED + " " + nClasss[num10].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CLASSNEED + " " + nClasss[num10].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }
        else if ((item.template.id >= 76 && item.template.id <= 84) || item.template.id == 315 || item.template.id == 379 || item.template.id == 401 || item.template.id == 556 || item.template.id == 562)
        {
            int num11 = 5;
            if (Char.getMyChar().nClass.classId == num11)
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLASSNEED + " " + nClasss[num11].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CLASSNEED + " " + nClasss[num11].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }
        else if ((item.template.id >= 85 && item.template.id <= 93) || item.template.id == 316 || item.template.id == 380 || item.template.id == 402 || item.template.id == 557 || item.template.id == 563)
        {
            int num12 = 6;
            if (Char.getMyChar().nClass.classId == num12)
            {
                mFont.tahoma_7_white.drawString(g, mResources.CLASSNEED + " " + nClasss[num12].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                mFont.tahoma_7_red.drawString(g, mResources.CLASSNEED + " " + nClasss[num12].name, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }

        if (!item.isTypeMounts())
        {
            if (item.template.id == 420)
            {
                if (Char.getMyChar().nClass.classId == 1 || Char.getMyChar().nClass.classId == 2)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.SYSITEM[1], inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else
                {
                    mFont.tahoma_7_red.drawString(g, mResources.SYSITEM[1], inforX + 8, yPaint += 12, mFont.LEFT);
                }

                indexRowMax++;
            }
            else if (item.template.id == 421)
            {
                if (Char.getMyChar().nClass.classId == 3 || Char.getMyChar().nClass.classId == 4)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.SYSITEM[2], inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else
                {
                    mFont.tahoma_7_red.drawString(g, mResources.SYSITEM[2], inforX + 8, yPaint += 12, mFont.LEFT);
                }

                indexRowMax++;
            }
            else if (item.template.id == 422)
            {
                if (Char.getMyChar().nClass.classId == 5 || Char.getMyChar().nClass.classId == 6)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.SYSITEM[3], inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else
                {
                    mFont.tahoma_7_red.drawString(g, mResources.SYSITEM[3], inforX + 8, yPaint += 12, mFont.LEFT);
                }

                indexRowMax++;
            }
        }

        if (item.expires > 0)
        {
            if (item.isTypeUIShop() || item.isTypeUIShopLock() || item.isTypeUIStore() || item.isTypeUIBook() || item.isTypeUIFashion() || item.isTypeUIClanShop() || item.isTypeUIClan())
            {
                num = mFont.tahoma_7.getWidth(mResources.EXPIRES + ": " + item.getExpiresShopString()) + 10;
                if (num > inforW && !GameCanvas.isTouchControlLargeScreen)
                {
                    inforW = num;
                }

                paintMultiLine(g, mFont.tahoma_7_yellow, mResources.EXPIRES + ": " + item.getExpiresShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
            }
            else
            {
                num = mFont.tahoma_7.getWidth(mResources.EXPIRES + ": " + item.getExpiresString()) + 10;
                if (num > inforW && !GameCanvas.isTouchControlLargeScreen)
                {
                    inforW = num;
                }

                paintMultiLine(g, mFont.tahoma_7_yellow, mResources.EXPIRES + ": " + item.getExpiresString(), inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }

        if (!item.template.description.Equals(string.Empty))
        {
            paintMultiLine(g, mFont.tahoma_7_white, item.template.description, inforX + 8, yPaint += 12, mFont.LEFT);
            indexRowMax++;
        }

        if (item.isTypeUIMe() || item.typeUI == 37)
        {
            mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COIN_LOCK_SALE, NinjaUtil.numberToString(string.Empty + item.saleCoinLock)), inforX + 8, yPaint += 12, mFont.LEFT);
            indexRowMax++;
        }
        else if (item.isTypeUIShop() || item.isTypeUIShopLock() || item.isTypeUIStore() || item.isTypeUIBook() || item.isTypeUIFashion() || item.isTypeUIClanShop())
        {
            if (item.buyCoin > 0)
            {
                if (item.isTypeUIClanShop())
                {
                    num = mFont.tahoma_7.getWidth(mResources.replace(mResources.COIN_BUY_CLAN, NinjaUtil.numberToString(string.Empty + item.buyCoin))) + 10;
                    if (num > inforW && !GameCanvas.isTouchControlLargeScreen)
                    {
                        inforW = num;
                    }

                    mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COIN_BUY_CLAN, NinjaUtil.numberToString(string.Empty + item.buyCoin)), inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else
                {
                    mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COIN_BUY, NinjaUtil.numberToString(string.Empty + item.buyCoin)), inforX + 8, yPaint += 12, mFont.LEFT);
                }

                indexRowMax++;
            }
            else if (item.buyCoinLock > 0)
            {
                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.COIN_LOCK_BUY, NinjaUtil.numberToString(string.Empty + item.buyCoinLock)), inforX + 8, yPaint += 12, mFont.LEFT);
                indexRowMax++;
            }
            else if (item.buyGold > 0)
            {
                mFont.tahoma_7_yellow.drawString(g, mResources.replace(mResources.GOLD_BUY, NinjaUtil.numberToString(string.Empty + item.buyGold)), inforX + 8, yPaint += 12, mFont.LEFT);
                indexRowMax++;
            }
        }

        if (item.isTypeBody() && item.sys != 0)
        {
            mFont.tahoma_7_blue1.drawString(g, mResources.SYSITEM[item.sys], inforX + 8, yPaint += 12, mFont.LEFT);
            indexRowMax++;
        }

        if (item.expires != 0 && item.options != null && item.options.size() > 0)
        {
            bool flag = false;
            bool flag2 = false;
            for (int num13 = 0; num13 < item.options.size(); num13++)
            {
                ItemOption itemOption2 = (ItemOption)item.options.elementAt(num13);
                if (!flag && itemOption2.optionTemplate.type == 2)
                {
                    flag = true;
                    string text = mResources.LINE[0] + ": ";
                    if (item.template.type == 1)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[item.template.type] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 0)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[6] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[5] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 6)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[0] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[5] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 5)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[0] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[6] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 2)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[8] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[7] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 8)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[2] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[7] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 7)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[2] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[8] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 4)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[3] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[9] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 3)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[4] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[9] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }
                    else if (item.template.type == 9)
                    {
                        string text2 = text;
                        text = text2 + mResources.BODY[4] + "(" + mResources.KICKSYS[itemFocus.sys] + "), " + mResources.BODY[3] + "(" + mResources.KICKSYS[itemFocus.sys] + ")";
                    }

                    num = mFont.tahoma_7_white.getWidth(text) + 15;
                    if (num > inforW && !GameCanvas.isTouchControlLargeScreen)
                    {
                        inforW = num;
                    }

                    paintMultiLine(g, mFont.tahoma_7_white, text, inforX + 8, yPaint += 12, mFont.LEFT);
                    indexRowMax++;
                }

                if (!flag2 && itemOption2.optionTemplate.type > 2 && itemOption2.optionTemplate.type < 8)
                {
                    flag2 = true;
                    mFont.tahoma_7_white.drawString(g, mResources.LINE[1], inforX + 8, yPaint += 12, mFont.LEFT);
                    indexRowMax++;
                }

                if (itemOption2.optionTemplate.id == 65)
                {
                    paintMultiLine(g, mFont.tahoma_7_blue, (item.template.id != 485) ? itemOption2.getOptionString() : NinjaUtil.replace(itemOption2.getOptionString(), mResources.EXP, mResources.MACHINE), inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else if (itemOption2.optionTemplate.id == 66)
                {
                    paintMultiLine(g, mFont.tahoma_7_blue1, (item.template.id != 485) ? itemOption2.getOptionString() : NinjaUtil.replace(itemOption2.getOptionString(), mResources.VITALITY, mResources.POWER), inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else if (itemOption2.optionTemplate.type == 0)
                {
                    paintMultiLine(g, mFont.tahoma_7_blue1, (!item.isTypeUIShopView()) ? itemOption2.getOptionString() : itemOption2.getOptionShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else if (itemOption2.optionTemplate.type == 1)
                {
                    paintMultiLine(g, mFont.tahoma_7_green, (!item.isTypeUIShopView()) ? itemOption2.getOptionString() : itemOption2.getOptionShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else if (itemOption2.optionTemplate.type == 8)
                {
                    if (itemOption2.optionTemplate.id == 85)
                    {
                        paintMultiLine(g, mFont.tahoma_7_yellow, (!item.isTypeUIShopView()) ? itemOption2.getOptionShopString1() : itemOption2.getOptionShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
                    }
                    else
                    {
                        paintMultiLine(g, mFont.tahoma_7b_blue, (!item.isTypeUIShopView()) ? itemOption2.getOptionShopString() : itemOption2.getOptionShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
                    }
                }
                else if ((itemOption2.optionTemplate.type == 2 && item.typeUI == 5 && itemOption2.active == 1) || (itemOption2.optionTemplate.type == 3 && item.upgrade >= 4) || (itemOption2.optionTemplate.type == 4 && item.upgrade >= 8) || (itemOption2.optionTemplate.type == 5 && item.upgrade >= 12) || (itemOption2.optionTemplate.type == 6 && item.upgrade >= 14) || (itemOption2.optionTemplate.type == 7 && item.upgrade >= 16))
                {
                    paintMultiLine(g, mFont.tahoma_7_green, (!item.isTypeUIShopView()) ? itemOption2.getOptionString() : itemOption2.getOptionShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
                }
                else
                {
                    paintMultiLine(g, mFont.tahoma_7_grey, (!item.isTypeUIShopView()) ? itemOption2.getOptionString() : itemOption2.getOptionShopString(), inforX + 8, yPaint += 12, mFont.LEFT);
                }

                indexRowMax++;
            }
        }

        if (isPaintLuckySpin)
        {
            if (item.template.id == 12)
            {
                mFont.tahoma_7_red.drawString(g, NinjaUtil.numberToString(yenValue[indexSelect]) + " " + mResources.YEN, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            if (item.template.type >= 0 && item.template.type <= 9)
            {
                mFont.tahoma_7_yellow.drawString(g, mResources.MAX_OPTIONS, inforX + 8, yPaint += 12, mFont.LEFT);
            }

            indexRowMax++;
        }

        if (indexRow >= 0 && (!GameCanvas.isTouch || (GameCanvas.isTouch && GameCanvas.w < 320)))
        {
            SmallImage.drawSmallImage(g, 942, inforX + 1, inforY + 5 + indexRow * 12, 0, StaticObj.TOP_LEFT);
        }

        scrInfo.setStyle(indexRowMax, 12, inforX, inforY + 2, inforW, inforH - 4, true, 1);
    }

    private void paintInforFrame(mGraphics g)
    {
        int num = ((!Main.isPC) ? indexSize : 0);
        if (indexMenu == 4 && !isPaintAuctionBuy)
        {
            num = 0;
        }

        if (isPaintSplit && indexMenu == 0)
        {
            num = -2;
        }

        resetTranslate(g);
        g.setColor(0);
        g.fillRect(inforX - 2, inforY - 2, inforW + 5, inforH + 5 - num);
        g.setColor(13606712);
        g.drawRect(inforX - 1, inforY - 1, inforW + 2, inforH + 2 - num);
        g.setColor(Paint.COLORBACKGROUND);
        g.fillRect(inforX, inforY, inforW, inforH - num);
    }

    private void paintListRanked(mGraphics g)
    {
        if (!isPaintRankedList)
        {
            return;
        }

        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        paintTitle(g, mResources.RANKED_MATCH, false);
        xstart = popupX + 5;
        ystart = popupY + 40;
        if (vList.size() == 0)
        {
            mFont.tahoma_7_white.drawString(g, mResources.EMPTY_INFO, popupX + popupW / 2, popupY + 40, mFont.CENTER);
            return;
        }

        g.setColor(6425);
        g.fillRect(xstart - 2, ystart - 2, popupW - 6, indexSize * 3 + 8);
        resetTranslate(g);
        scrMain.setStyle(vList.size(), indexSize, xstart, ystart, popupW - 3, indexSize * 3 + 4, true, 1);
        scrMain.setClip(g, xstart, ystart, popupW - 3, indexSize * 3 + 6);
        indexRowMax = vList.size();
        for (int i = 0; i < vList.size(); i++)
        {
            Ranked ranked = null;
            try
            {
                ranked = (Ranked)vList.elementAt(i);
            }
            catch (Exception)
            {
            }

            if (ranked != null)
            {
                if (indexRow == i)
                {
                    g.setColor(Paint.COLORLIGHT);
                    g.fillRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(16777215);
                    g.drawRect(xstart + 2, ystart + indexRow * indexSize + 2, popupW - 15, indexSize - 4);
                }
                else
                {
                    g.setColor(Paint.COLORBACKGROUND);
                    g.fillRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                    g.setColor(13932896);
                    g.drawRect(xstart + 2, ystart + i * indexSize + 2, popupW - 15, indexSize - 4);
                }

                mFont.tahoma_7_yellow.drawString(g, ranked.name, xstart + (popupW - 10) / 2 - popupW / 4, ystart + i * indexSize + indexSize / 2 - 10, 2);
                mFont.tahoma_7_yellow.drawString(g, "-", xstart + (popupW - 10) / 2, ystart + i * indexSize + indexSize / 2 - 10, 2);
                mFont.tahoma_7_yellow.drawString(g, mResources.RANKED + ": " + ranked.ranked, xstart + (popupW - 10) / 2 + popupW / 4, ystart + i * indexSize + indexSize / 2 - 10, 2);
                mFont.tahoma_7_white.drawString(g, ranked.stt, xstart + popupW / 2, ystart + i * indexSize + indexSize / 2 + 5, 2);
            }
        }
    }

    private void domenuLucky_Draw()
    {
        Service.gI().luckyDraw(102, string.Empty, typeLucky_Draw);
    }

    private void luckyDraw_refresh()
    {
        isRefresh = true;
        Service.gI().luckyDraw(101, string.Empty, typeLucky_Draw);
    }

    private void luckyDraw_Tf()
    {
        GameCanvas.inputDlg.show(mResources.Lucky_draw, new Command(mResources.Join_Now, GameCanvas.instance, 1700, 1), TField.INPUT_TYPE_NUMERIC);
    }

    public void showLucky_Draw(string title, short time, string totalMoney, short percentWin1, string percentWin2, short numPlayer, string winnerInfo, string myMoney, sbyte typeLucky)
    {
        InfoDlg.hide();
        isPaintLucky_Draw = true;
        isLockKey = true;
        indexRow = 0;
        setPopupSize(175, 200);
        left = null;
        center = new Command(mResources.Join_Now, 1701);
        right = new Command(mResources.CLOSE, 1702);
        typeLucky_Draw = typeLucky;
        textsTitle = title;
        timeLucky_Draw = time;
        totalMoneyLucky_Draw = totalMoney;
        percentWin1Lucky_Draw = percentWin1;
        percentWin2Lucky_Draw = percentWin2;
        numPlayerLucky_Draw = numPlayer;
        winnerInfoLucky_Draw = winnerInfo;
        myMoneyLucky_Draw = myMoney;
        string text = ((!(myMoneyLucky_Draw == string.Empty)) ? (mResources.Lucky_draw_Mymoney + myMoneyLucky_Draw + mResources.XU) : string.Empty);
        string src = "c3" + totalMoneyLucky_Draw + "\n" + mResources.Lucky_draw_Numplayer + numPlayer + "\n" + text + "\n\nc0" + winnerInfoLucky_Draw;
        texts = mFont.tahoma_7.splitFontVector(src, popupW - 30);
        timeStart = mSystem.currentTimeMillis();
        timeC = getTimeCountDown(timeStart, timeLucky_Draw);
        isRefresh = false;
    }

    public void doCloseLucky_Draw()
    {
        isPaintLucky_Draw = false;
        textsTitle = null;
        texts = null;
        center = null;
        left = null;
        right = null;
        resetButton();
    }

    public void scrollDownLucky_Draw()
    {
        indexRowMax = texts.size();
        scrMain.setStyle(indexRowMax, 12, popupX, ystart + 12, popupW, popupH - 42 - ((textsTitle != null) ? 10 : 0), true, 1);
        indexRow = texts.size() - 1;
        scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
    }

    public void updateLucky_Draw()
    {
        if (!isRefresh)
        {
            if (numPlayerLucky_Draw > 1)
            {
                timeC = getTimeCountDown(timeStart, timeLucky_Draw);
            }

            if (timeC == string.Empty)
            {
                luckyDraw_refresh();
            }

            long num = (timeStart + timeLucky_Draw * 1000 - mSystem.currentTimeMillis()) / 1000;
            if (num > 8 && num % 5 == 0)
            {
                luckyDraw_refresh();
            }

            if (num <= 10)
            {
                center = null;
            }

            if (num <= 20)
            {
                fnt = mFont.tahoma_7b_red;
            }
            else
            {
                fnt = mFont.tahoma_7b_yellow;
            }
        }
    }

    public void paintPercent(mGraphics g, short percent, int x, int y, int w, int h, string percentStr)
    {
        g.setColor(0);
        g.fillRect(x - w / 2, y, w, h);
        int num = percent * w / 100;
        if (num < 1)
        {
            num = 1;
        }

        g.setClip(x - w / 2, y, num, h);
        g.setColor(16711680);
        g.fillRect(x - w / 2, y, w, h);
        resetTranslate(g);
        mFont.tahoma_7_yellow.drawString(g, percentStr, popupX + popupW / 2, y, 2);
    }

    public void paintLucky_Draw(mGraphics g)
    {
        if (texts == null || !isPaintLucky_Draw)
        {
            return;
        }

        updateLucky_Draw();
        resetTranslate(g);
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        if (textsTitle != null)
        {
            paintTitle(g, textsTitle, isPaintMessage);
        }

        xstart = popupX + 15;
        ystart = popupY + 15;
        if (textsTitle != null)
        {
            ystart += 10;
        }

        fnt.drawString(g, timeC, popupX + popupW / 2, ystart + 12, 2);
        mFont.tahoma_7_yellow.drawString(g, mResources.winRate, popupX + popupW / 2, ystart + 30, 2);
        paintPercent(g, percentWin1Lucky_Draw, popupX + popupW / 2, ystart + 42, popupW / 2, 12, percentWin1Lucky_Draw + "." + percentWin2Lucky_Draw + "%");
        indexRowMax = texts.size();
        scrMain.setStyle(indexRowMax, 12, popupX, ystart + 48, popupW, popupH - 42 - ((textsTitle != null) ? 10 : 0), true, 1);
        scrMain.setClip(g);
        yPaint = ystart + 48;
        mFont mFont2 = mFont.tahoma_7_white;
        for (int i = 0; i < texts.size(); i++)
        {
            string text = (string)texts.elementAt(i);
            if (text == null || texts == null || mFont2 == null)
            {
                break;
            }

            if (text.StartsWith("c"))
            {
                if (text.StartsWith("c0"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_white;
                }
                else if (text.StartsWith("c1"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_yellow;
                }
                else if (text.StartsWith("c2"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_white;
                }
                else if (text.StartsWith("c3"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_yellow;
                }
                else if (text.StartsWith("c4"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_red;
                }
                else if (text.StartsWith("c5"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_red;
                }
                else if (text.StartsWith("c6"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_grey;
                }
                else if (text.StartsWith("c7"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7b_blue;
                }
                else if (text.StartsWith("c8"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_blue;
                }
                else if (text.StartsWith("c9"))
                {
                    text = text.Substring(2);
                    mFont2 = mFont.tahoma_7_green;
                }
            }

            mFont2.drawString(g, text, popupX + popupW / 2, yPaint += 12, 2);
        }
    }

    public void paintCaptcha(mGraphics g)
    {
        if (!Char.getMyChar().isCaptcha)
        {
            return;
        }

        for (int i = 0; i < arrowCaptcha.Length; i++)
        {
            if (arrowCaptcha[i] != -1)
            {
                int transform = 0;
                if (arrowCaptcha[i] == 0)
                {
                    transform = 2;
                }
                else if (arrowCaptcha[i] == 1)
                {
                    transform = 6;
                }
                else if (arrowCaptcha[i] == 2)
                {
                    transform = 0;
                }

                SmallImage.drawSmallImage(g, 989, Char.getMyChar().cx + i * 10 - (arrowCaptcha.Length - 1) * 10 / 2, Char.getMyChar().cy - 40, transform, mGraphics.HCENTER | mGraphics.VCENTER);
            }
        }
    }

    public void paintThanThu_Clan(mGraphics g)
    {
        try
        {
            setInfoThanThu();
            Clan_ThanThu clan_ThanThu = ((clan_thanthu.size() <= 0 || indexThanThu > clan_thanthu.size()) ? null : ((Clan_ThanThu)clan_thanthu.elementAt(indexThanThu)));
            if (clan_ThanThu == null)
            {
                g.setColor(13606712);
                g.drawRect(xRecThanThu - 1, yRecThanThu - 1, wRecThanThu + 1, hRecThanThu + 1);
                g.setClip(xRecThanThu, yRecThanThu, wRecThanThu, hRecThanThu);
                g.setColor(6425);
                g.fillRect(xRecThanThu, yRecThanThu, wRecThanThu, hRecThanThu);
                return;
            }

            mFont.tahoma_7b_white.drawString(g, clan_ThanThu.name, GameCanvas.hw, yThanThu + 30, 2);
            g.setColor(13606712);
            g.drawRect(xRecThanThu - 1, yRecThanThu - 1, wRecThanThu + 1, hRecThanThu + 1);
            g.setClip(xRecThanThu, yRecThanThu, wRecThanThu, hRecThanThu);
            g.setColor(6425);
            g.fillRect(xRecThanThu, yRecThanThu, wRecThanThu, hRecThanThu);
            SmallImage.drawSmallImage(g, clan_ThanThu.idThanThu, xRecThanThu + wRecThanThu / 2, yRecThanThu + hRecThanThu / 2 - 10, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            resetTranslate(g);
            if (clan_ThanThu.time_aptrung >= 0)
            {
                strTime = Res.getTimeCountDown(clan_ThanThu.timeStartThanThu, clan_ThanThu.time_aptrung);
                if (!strTime.Equals(string.Empty))
                {
                    mFont.tahoma_7_yellow.drawString(g, clan_ThanThu.str_trungno + " " + strTime, xRecThanThu, yExpThanThu + 15, 0);
                }
                else
                {
                    countSendRequests--;
                    if (countSendRequests <= 0)
                    {
                        Service.gI().requestClanItem();
                        countSendRequests = 100;
                    }
                }
            }
            else
            {
                for (int i = 0; i < clan_ThanThu.stars; i++)
                {
                    SmallImage.drawSmallImage(g, 628, xThanThu + 95 + i * 12 - clan_ThanThu.stars * 6, yThanThu + 50, 0, mGraphics.HCENTER | mGraphics.VCENTER);
                }

                int w = clan_ThanThu.curExp * wExpThanThu / clan_ThanThu.maxExp;
                g.setColor(2506246);
                g.fillRect(xExpThanThu, yExpThanThu, wExpThanThu, hExpThanThu);
                g.setColor(371981);
                g.fillRect(xExpThanThu, yExpThanThu, w, hExpThanThu);
                g.setColor(13606712);
                g.drawRect(xExpThanThu, yExpThanThu, wExpThanThu, hExpThanThu);
                mFont.tahoma_7_white.drawString(g, clan_ThanThu.curExp + "/" + clan_ThanThu.maxExp, xExpThanThu + wExpThanThu / 2, yExpThanThu - 2, 2);
                for (int j = 0; j < clan_ThanThu.vecInfo.size(); j++)
                {
                    string st = (string)clan_ThanThu.vecInfo.elementAt(j);
                    mFont.tahoma_7_yellow.drawString(g, st, xRecThanThu + wRecThanThu / 2, yExpThanThu + 15 + j * 10, 2);
                }
            }

            for (int k = 0; k < clan_thanthu.size(); k++)
            {
                Clan_ThanThu clan_ThanThu2 = ((clan_thanthu.size() <= 0 || k > clan_thanthu.size()) ? null : ((Clan_ThanThu)clan_thanthu.elementAt(k)));
                if (clan_ThanThu2 != null)
                {
                    g.setColor(0);
                    g.fillRect(xSelectThanThu + k * (wSelectThanThu + 5) - wSelectThanThu / 2, ySelectThanThu - wSelectThanThu / 2, wSelectThanThu, wSelectThanThu);
                    SmallImage.drawSmallImage(g, 154, xSelectThanThu + k * (wSelectThanThu + 5), ySelectThanThu, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xSelectThanThu + k * (wSelectThanThu + 5) - wSelectThanThu / 2, ySelectThanThu - wSelectThanThu / 2, wSelectThanThu, wSelectThanThu);
                    SmallImage.drawSmallImage(g, clan_ThanThu2.idIconItem, xSelectThanThu + k * (wSelectThanThu + 5), ySelectThanThu, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            g.setColor(16777215);
            g.drawRect(xSelectThanThu + indexThanThu * (wSelectThanThu + 5) - wSelectThanThu / 2, ySelectThanThu - wSelectThanThu / 2, wSelectThanThu, wSelectThanThu);
        }
        catch (Exception)
        {
        }
    }

    public void paintLuyenNgoc(mGraphics g, string[] titles)
    {
        try
        {
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 34 + indexSize;
            int num = popupX + 74;
            int num2 = ystart - indexSize - 3;
            rows = 4;
            if (itemSplit != null)
            {
                paintItem(g, itemSplit, num, num2);
                paint_exp_luyenngoc_bar(g, num + 35, num2 + 25);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num2 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num2 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }

            g.setColor(12281361);
            g.drawRect(num, num2, indexSize, indexSize);
            g.setColor(6425);
            g.fillRect(xstart - 1, ystart - 1, indexSize * columns + 3, indexSize * rows + 3);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            for (int k = 0; k < arrItemSplit.Length; k++)
            {
                Item item = arrItemSplit[k];
                if (item != null)
                {
                    int num3 = k / columns;
                    int num4 = k - num3 * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num4 * indexSize + 1, ystart + num3 * indexSize + 1, indexSize - 1, indexSize - 1);
                    }

                    paintItem(g, item, xstart + num4 * indexSize, ystart + num3 * indexSize);
                    if (item.quantity > 1)
                    {
                        mFont.number_yellow.drawString(g, string.Empty + item.quantity, xstart + num4 * indexSize + indexSize, ystart + num3 * indexSize + indexSize - mFont.number_yellow.getHeight(), 1);
                    }
                }
            }

            if (indexTitle == 1)
            {
                g.setColor(16777215);
                g.drawRect(num, num2, indexSize, indexSize);
            }
            else if (indexTitle == 2)
            {
                int num5 = indexSelect / columns;
                int num6 = indexSelect - num5 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num6 * indexSize, ystart + num5 * indexSize, indexSize, indexSize);
            }
        }
        catch (Exception)
        {
        }
    }

    public void paint_exp_luyenngoc_bar(mGraphics g, int x, int y)
    {
        int num = arrNgocKhamEXP[itemSplit.upgrade][0];
        int num2 = 0;
        ItemOption itemOption = null;
        if (itemSplit.options != null)
        {
            for (int i = 0; i < itemSplit.options.size(); i++)
            {
                itemOption = (ItemOption)itemSplit.options.elementAt(i);
                if (itemOption.optionTemplate.id == 104)
                {
                    num2 = itemOption.param;
                }
            }
        }

        int num3 = 0;
        for (int j = 0; j < arrItemSplit.Length; j++)
        {
            Item item = arrItemSplit[j];
            if (item != null)
            {
                num3 += arrNgocKhamEXP[item.upgrade][1];
            }
        }

        int num4 = 0;
        int num5 = 0;
        int num6 = 60;
        int num7 = 5;
        g.setColor(0);
        g.fillRect(x, y - num7, num6, num7);
        num4 = num2 * num6 / num;
        if (num4 <= 0)
        {
            num4 = 0;
        }
        else if (num4 > num6)
        {
            num4 = num6;
        }

        g.setColor(65280);
        g.fillRect(x, y - num7, num4, num7);
        num5 = num3 * num6 / num;
        if (num5 >= num6 - num4)
        {
            num5 = num6 - num4;
        }

        g.setColor(430630);
        g.fillRect(x + num4, y - num7, num5, num7);
        mFont.tahoma_7_yellow.drawString(g, num3 + num2 + "/" + num, x + num6 / 2, y - num7 - 15, 2);
    }

    public void paintKhamNgoc(mGraphics g, string[] titles)
    {
        try
        {
            rows = 3;
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintTitle(g, titles[indexMenu], titles.Length > 1);
            xstart = popupX + 3;
            ystart = popupY + 34 + indexSize;
            int num = popupX + 45;
            int num2 = popupX + 100;
            int num3 = ystart - indexSize - 3;
            if (itemSplit != null)
            {
                paintItem(g, itemSplit, num, num3);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num3 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num3 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }

            g.setColor(12281361);
            g.drawRect(num, num3, indexSize, indexSize);
            if (itemUpGrade != null)
            {
                paintItem(g, itemUpGrade, num2, num3);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num2 - 1, num3 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num2 + indexSize / 2, num3 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }

            g.setColor(12281361);
            g.drawRect(num, num3, indexSize, indexSize);
            g.drawRect(num2, num3, indexSize, indexSize);
            mFont.tahoma_7b_yellow.drawString(g, "+", num + indexSize + 15, num3 + indexSize / 2 - 5, 2);
            if (indexTitle == 1)
            {
                if (indexSelect == 0)
                {
                    g.setColor(16777215);
                    g.drawRect(num, num3, indexSize, indexSize);
                }

                if (indexSelect == 1)
                {
                    g.setColor(16777215);
                    g.drawRect(num2, num3, indexSize, indexSize);
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    g.setColor(6425);
                    g.fillRect(xstart + j * indexSize, ystart + i * indexSize, indexSize + 3, indexSize + 3);
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            if (indexTitle == 2)
            {
                int num4 = indexSelect / columns;
                int num5 = indexSelect - num4 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num5 * indexSize, ystart + num4 * indexSize, indexSize, indexSize);
            }

            for (int k = 0; k < arrItemUpGrade.Length; k++)
            {
                Item item = arrItemUpGrade[k];
                if (item != null)
                {
                    int num6 = k / columns;
                    int num7 = k - num6 * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num7 * indexSize + 1, ystart + num6 * indexSize + 1, indexSize - 1, indexSize - 1);
                    }

                    SmallImage.drawSmallImage(g, item.template.iconID, xstart + num7 * indexSize + indexSize / 2, ystart + num6 * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (itemUpGrade != null && itemSplit != null)
            {
                int num8 = 0;
                for (int l = 0; l < arrItemUpGrade.Length; l++)
                {
                    if (arrItemUpGrade[l] != null && arrItemUpGrade[l].template.type == 26)
                    {
                        num8 += crystals[arrItemUpGrade[l].template.id];
                    }
                }

                int num9 = 0;
                num9 = num8 * 100 / upWeapon[itemSplit.upgrade];
                if (num9 > maxPercents[itemSplit.upgrade])
                {
                    num9 = maxPercents[itemSplit.upgrade];
                }

                if (isPaintUpGradeGold)
                {
                    num9 = (int)((double)num9 * 1.5);
                }

                mFont mFont2 = mFont.tahoma_7_yellow;
                int num10 = 0;
                ItemOption itemOption = null;
                if (itemSplit.options != null)
                {
                    for (int m = 0; m < itemSplit.options.size(); m++)
                    {
                        itemOption = (ItemOption)itemSplit.options.elementAt(m);
                        if (itemOption.optionTemplate.id == 123)
                        {
                            num10 = itemOption.param;
                        }
                    }
                }
                else
                {
                    Service.gI().requestItemInfo(itemSplit.typeUI, itemSplit.indexUI);
                }

                if (num10 > Char.getMyChar().xu + Char.getMyChar().yen)
                {
                    mFont2 = mFont.tahoma_7_red;
                }

                mFont2.drawString(g, mResources.replace(mResources.COINLOCKNEED, NinjaUtil.numberToString(num10 + string.Empty)), xstart, ystart + rows * indexSize + 5, mFont.LEFT);
                mFont.tahoma_7_yellow.drawString(g, mResources.PERCENT + ": " + num9 + "%", xstart, ystart + rows * indexSize + 17, mFont.LEFT);
            }
            else
            {
                for (int n = 0; n < mResources.HELPKHAMNGOC.Length; n++)
                {
                    mFont.tahoma_7_white.drawString(g, mResources.HELPKHAMNGOC[n], xstart, ystart + rows * indexSize + 5 + n * 12, mFont.LEFT);
                }
            }

            if (effUpok == null)
            {
                return;
            }

            SmallImage.drawSmallImage(g, effUpok.arrEfInfo[indexEff].idImg, num2 + indexSize / 2 + effUpok.arrEfInfo[indexEff].dx + 1, num3 + indexSize / 2 + 9 + effUpok.arrEfInfo[indexEff].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            if (GameCanvas.gameTick % 2 == 0)
            {
                indexEff++;
                if (indexEff >= effUpok.arrEfInfo.Length)
                {
                    indexEff = 0;
                    effUpok = null;
                }
            }
        }
        catch (Exception)
        {
        }
    }

    public void paintLuyenNgoc(mGraphics g)
    {
        if (isPaintLuyenNgoc)
        {
            if (indexMenu == 0)
            {
                paintLuyenNgoc(g, mResources.TITLELUYENNGOC);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLELUYENNGOC);
            }
        }
    }

    public void paintKhamNgoc(mGraphics g)
    {
        if (isPaintKhamNgoc)
        {
            if (indexMenu == 0)
            {
                paintKhamNgoc(g, mResources.TITLEKHAMNGOC);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEKHAMNGOC);
            }
        }
    }

    public void paintGotNgoc(mGraphics g)
    {
        if (isPaintGotNgoc)
        {
            if (indexMenu == 0)
            {
                paintGotNgoc(g, mResources.TITLEGOTNGOC, 0);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGOTNGOC);
            }
        }
    }

    public void paintThaoNgoc(mGraphics g)
    {
        if (isPaintThaoNgoc)
        {
            if (indexMenu == 0)
            {
                paintGotNgoc(g, mResources.TITLETHAONGOC, 1);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLETHAONGOC);
            }
        }
    }

    public void paintGotNgoc(mGraphics g, string[] titles, sbyte typemoney)
    {
        try
        {
            rows = 5;
            GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
            paintItemFrame(g, titles, false);
            g.setColor(6693376);
            g.fillRect(popupX + 3, popupY + 32, 168, 140);
            g.setColor(13408563);
            g.drawRect(popupX + 3, popupY + 32, 168, 140);
            int num = popupX + 74;
            int num2 = popupY + 40 + indexSize;
            if (itemSplit != null)
            {
                g.setColor(6425);
                g.fillRect(num - 1, num2 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num2 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                paintItem(g, itemSplit, num, num2);
                if (itemSplit.quantity > 1)
                {
                    mFont.number_yellow.drawString(g, string.Empty + itemSplit.quantity, num + indexSize, num2 + indexSize / 2 + 6, 1);
                }

                g.setColor((indexTitle != 1) ? 12281361 : 16777215);
                g.drawRect(num, num2, indexSize, indexSize);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(num - 1, num2 - 1, indexSize + 3, indexSize + 3);
                SmallImage.drawSmallImage(g, 154, num + indexSize / 2, num2 + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                g.setColor(12281361);
                g.drawRect(num, num2, indexSize, indexSize);
            }

            if (itemSplit == null)
            {
                return;
            }

            int num3 = 0;
            ItemOption itemOption = null;
            if (itemSplit.options != null)
            {
                for (int i = 0; i < itemSplit.options.size(); i++)
                {
                    itemOption = (ItemOption)itemSplit.options.elementAt(i);
                    if (itemOption.optionTemplate.id == 122)
                    {
                        num3 = itemOption.param;
                    }
                }
            }
            else
            {
                Service.gI().requestItemInfo(itemSplit.typeUI, itemSplit.indexUI);
            }

            string st = mResources.MONEY_SELL2;
            string text = num3 + mResources.YEN;
            string src = mResources.HELP_THAONGOC;
            if (typemoney == 0)
            {
                st = mResources.MONEY_SELL;
                text = coinGotngoc[itemSplit.upgrade] + mResources.XU;
                src = mResources.HELP_GOTNGOC;
            }

            mFont.tahoma_7_white.drawString(g, st, num + indexSize / 2, num2 + 3 * indexSize / 2 + 2, mFont.CENTER);
            mFont.tahoma_7_yellow.drawString(g, string.Empty + text, num + indexSize / 2, num2 + 3 * indexSize / 2 + 14, mFont.CENTER);
            string[] array = mFont.tahoma_7_white.splitFontArray(src, 130);
            for (int j = 0; j < array.Length; j++)
            {
                mFont.tahoma_7_white.drawString(g, array[j], num + indexSize / 2, popupY + popupH - 25 + j * 12 - 2, 2);
            }
        }
        catch (Exception)
        {
        }
    }

    public void paintGiaoDo(mGraphics g)
    {
        if (isPaintGiaoDo)
        {
            if (indexMenu == 0)
            {
                paintGiaoDo(g, mResources.TITLEGIAODO, arrItemSplit);
            }
            else if (indexMenu == 1)
            {
                paintBag(g, mResources.TITLEGIAODO);
            }
        }
    }

    public void paintGiaoDo(mGraphics g, string[] titles, Item[] items)
    {
        try
        {
            resetTranslate(g);
            paintItemFrame(g, titles, true);
            if (items == null)
            {
                GameCanvas.paintShukiren(popupX + 90, popupY + 75, g, false);
                mFont.tahoma_7b_white.drawString(g, mResources.PLEASEWAIT, popupX + 90, popupY + 90, 2);
                return;
            }

            if (items.Length <= 30)
            {
                rows = 5;
            }
            else if (items.Length % columns == 0)
            {
                rows = items.Length / columns;
            }
            else
            {
                rows = items.Length / columns + 1;
            }

            scrMain.setStyle(rows, indexSize, xstart, ystart, columns * indexSize, 5 * indexSize, true, 6);
            scrMain.setClip(g, xstart, ystart, scrMain.width + 2, scrMain.height + 2);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SmallImage.drawSmallImage(g, 154, xstart + j * indexSize + indexSize / 2, ystart + i * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    g.setColor(12281361);
                    g.drawRect(xstart + j * indexSize, ystart + i * indexSize, indexSize, indexSize);
                }
            }

            for (int k = 0; k < items.Length; k++)
            {
                Item item = items[k];
                if (item != null)
                {
                    int num = k / columns;
                    int num2 = k - num * columns;
                    if (!item.isLock)
                    {
                        g.setColor(12083);
                        g.fillRect(xstart + num2 * indexSize + 1, ystart + num * indexSize + 1, indexSize - 1, indexSize - 1);
                        SmallImage.drawSmallImage(g, 154, xstart + num2 * indexSize + indexSize / 2, ystart + num * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    }

                    SmallImage.drawSmallImage(g, item.template.iconID, xstart + num2 * indexSize + indexSize / 2, ystart + num * indexSize + indexSize / 2, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                }
            }

            if (indexTitle > 0 && indexSelect >= 0)
            {
                int num3 = indexSelect / columns;
                int num4 = indexSelect - num3 * columns;
                g.setColor(16777215);
                g.drawRect(xstart + num4 * indexSize, ystart + num3 * indexSize, indexSize, indexSize);
                paintSelectHighlight(xstart + num4 * indexSize, ystart + num3 * indexSize, g);
            }
        }
        catch (Exception)
        {
        }
    }

    public void paintBijuu(mGraphics g)
    {
        if (indexMenu != 6)
        {
            return;
        }

        g.translate(-g.getTranslateX(), -g.getTranslateY());
        GameCanvas.paintz.paintFrame(popupX, popupY, popupW, popupH, g);
        g.setColor(Paint.COLORBACKGROUND);
        paintTitle(g, mResources.MENUME[indexMenu], true);
        resetTranslate(g);
        g.setColor(0);
        g.fillRect(popupX + 2, popupY + 31, 171, popupH - 34);
        g.setColor(13606712);
        g.drawRect(popupX + 3, popupY + 32, 168, popupH - 37);
        g.setColor(Paint.COLORBACKGROUND);
        g.fillRect(popupX + 4, popupY + 34, 166, popupH - 39);
        if (currentCharViewInfo.arrItemViThu[4] != null)
        {
            mFont.tahoma_7b_white.drawString(g, currentCharViewInfo.arrItemViThu[4].template.name, popupX + 90, ystart + 2, 2);
            int num = currentCharViewInfo.arrItemViThu[4].sys + 1;
            for (int i = 0; i < num; i++)
            {
                SmallImage.drawSmallImage(g, 628, popupX + 90 + i * 12 - num * 6, ystart + 20, 0, mGraphics.HCENTER | mGraphics.VCENTER);
            }
        }

        for (int j = 0; j < currentCharViewInfo.arrItemViThu.Length - 1; j++)
        {
            if (currentCharViewInfo.arrItemViThu[j] != null)
            {
                paintItem(g, currentCharViewInfo.arrItemViThu[j], xMounts[j], yMounts[j]);
            }
            else
            {
                g.setColor(6425);
                g.fillRect(xMounts[j] - 1, yMounts[j] - 1, indexSize + 3, indexSize + 3);
                mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[j + 24][0], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 - 10, 2);
                mFont.tahoma_7_grey.drawString(g, mResources.ITEMNAME[j + 24][1], xMounts[j] + indexSize / 2, yMounts[j] + indexSize / 2 + 2, 2);
            }

            if (indexSelect == j && indexTitle == 1 && indexSelect < 4)
            {
                g.setColor(16777215);
            }
            else
            {
                g.setColor(12281361);
            }

            g.drawRect(xMounts[j], yMounts[j], indexSize, indexSize);
        }

        int num2 = xMounts[0] + indexSize + 7;
        int num3 = yMounts[0] - 5;
        g.setColor(6425);
        g.fillRect(num2, num3, 84, 75);
        if (indexSelect == 4)
        {
            g.setColor(16777215);
        }
        else
        {
            g.setColor(12281361);
        }

        g.drawRect(num2, num3, 84, 75);
        if (currentCharViewInfo.mobVithu != null)
        {
            currentCharViewInfo.mobVithu.paintTopMobNew(g, num2 + 35, num3 + 55);
            currentCharViewInfo.mobVithu.paintBoTomMobNew(g, num2 + 35, num3 + 55);
        }

        int num4 = 0;
        int num5 = 0;
        int num6 = 0;
        if (currentCharViewInfo.arrItemViThu[4] != null)
        {
            if (currentCharViewInfo.arrItemViThu[4].options != null)
            {
                for (int k = 0; k < currentCharViewInfo.arrItemViThu[4].options.size(); k++)
                {
                    ItemOption itemOption = (ItemOption)currentCharViewInfo.arrItemViThu[4].options.elementAt(k);
                    if (itemOption.optionTemplate.id == 151)
                    {
                        num4 = itemOption.param;
                    }
                    else if (itemOption.optionTemplate.id == 150)
                    {
                        num5 = itemOption.param;
                    }
                }
            }

            num6 = currentCharViewInfo.arrItemViThu[4].upgrade;
        }

        int num7 = xstart + 5;
        int num8 = ystart + 112;
        mFont.tahoma_7b_white.drawString(g, mResources.Level + ": ", num7, num8, 0);
        mFont.tahoma_7b_white.drawString(g, num6 + string.Empty, num7 + 70, num8, 0);
        mFont.tahoma_7b_white.drawString(g, mResources.NAMEPOTENTIAL[2] + ": ", num7, num8 += 15, 0);
        mFont.tahoma_7b_white.drawString(g, num5 + string.Empty, num7 + 70, num8, 0);
        mFont.tahoma_7b_white.drawString(g, mResources.EXP + ": ", num7, num8 += 17, 0);
        mFont.tahoma_7b_white.drawString(g, num4 + string.Empty, num7 + 70, num8, 0);
    }
}
