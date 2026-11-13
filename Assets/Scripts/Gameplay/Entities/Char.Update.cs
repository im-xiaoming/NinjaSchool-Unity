using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    private void updateCharSound()
    {
        if (me && getMyChar().statusMe == 2 && !Sound.isPlayingSound())
        {
            Sound.playSoundRun(Sound.MBuocChan, 0.8f);
        }
    }

    public void updateFramecharByhorse()
    {
        if (ID_HORSE == -1)
        {
            dynewhhorse = 0;
            dxnewhorse = 0;
            return;
        }

        DataSkillEff partEff = getPartEff(ID_HORSE, true);
        if (partEff != null && partEff.isLoadData)
        {
            if (ActionHorse == null)
            {
                ActionHorse = partEff.ActionStand;
            }

            dynewhhorse = partEff.dyHorse + partEff.Dy_Char[getFrameHores()];
            dxnewhorse = partEff.dxHorse + partEff.Dx_Char[getFrameHores()];
            if (cdir == 1)
            {
                dxnewhorse *= -1;
            }

            if (statusMe == 1 || statusMe == 6)
            {
                ActionHorse = partEff.ActionStand;
            }
            else if (statusMe == 2 || statusMe == 10)
            {
                ActionHorse = partEff.ActionMove;
            }
            else if (statusMe == 4)
            {
                ActionHorse = partEff.ActionFall;
            }
            else if (statusMe == 3)
            {
                ActionHorse = partEff.ActionJum;
            }
            else
            {
                ActionHorse = partEff.ActionStand;
            }

            if (GameCanvas.gameTick % 3 == 0)
            {
                fho = (fho + 1) % ActionHorse.Length;
                FramecharRideHorse = partEff.FrameChar[getFrameHores()];
            }

            cf = FramecharRideHorse;
        }
    }

    public void updateFrameSuSan()
    {
        if (ID_SUSANO <= -1)
        {
            return;
        }

        DataSkillEff partEff = getPartEff(ID_SUSANO, false);
        if (partEff != null && partEff.sequence != null)
        {
            fsusan++;
            if (fsusan > partEff.sequence.Length - 1)
            {
                fsusan = 0;
            }

            FrameSusan = partEff.sequence[fsusan];
        }
    }

    public void updateFrameRank()
    {
        if (ID_RANK <= -1)
        {
            return;
        }

        DataSkillEff partEff = getPartEff(ID_RANK, false);
        if (partEff != null && partEff.sequence != null)
        {
            fRank++;
            if (fRank > partEff.sequence.Length - 1)
            {
                fRank = 0;
            }

            FrameRank = partEff.sequence[fRank];
        }
    }

    public void updateFrameName()
    {
        if (ID_NAME <= -1)
        {
            return;
        }

        DataSkillEff partEff = getPartEff(ID_NAME, false);
        if (partEff != null && partEff.sequence != null)
        {
            fName++;
            if (fName > partEff.sequence.Length - 1)
            {
                fName = 0;
            }

            FrameName = partEff.sequence[fName];
        }
    }

    public virtual void update()
    {
        updateFrameName();
        updateFrameRank();
        updateFrameSuSan();
        cX0 = cx;
        cY0 = cy;
        if (arrItemBody != null && arrItemBody[10] == null && mobMe != null)
        {
            mobMe = null;
        }

        if (ID_Bien_Hinh > -1)
        {
            fBienhinh = (fBienhinh + 1) % 10;
        }

        if (ID_MAT_NA > -1)
        {
            fMatNa = (fMatNa + 1) % 10;
        }

        if (ID_WEA_PONE > -1)
        {
            fWeapone = (fWeapone + 1) % 10;
        }

        if (ID_LEG > -1)
        {
            fLeg = (fLeg + 1) % 10;
        }

        if (ID_Body > -1)
        {
            fBody = (fBody + 1) % 10;
        }

        if (ID_HAIR > -1)
        {
            fHair = (fHair + 1) % 10;
        }

        if (ID_PP > -1)
        {
            fPP = (fPP + 1) % 10;
        }

        updatefHorseui();
        update_No();
        sendMove();
        if (mobMe != null)
        {
            updateMobMe();
        }

        if (mobVithu != null)
        {
            updateNew_MobVithu();
        }

        isEffBatTu = false;
        if (resultTest > 0 && GameCanvas.gameTick % 2 == 0)
        {
            resultTest = (sbyte)(resultTest - 1);
            if (resultTest == 30 || resultTest == 60)
            {
                resultTest = 0;
            }
        }

        updateSkillPaint();
        if (mobMe != null)
        {
            mobMe.update();
        }

        if (mobVithu != null)
        {
            mobVithu.update();
        }

        if (arr != null)
        {
            arr.update();
        }

        if (arrItemMounts[4] != null && arrItemMounts[4].options != null)
        {
            for (int i = 0; i < arrItemMounts[4].options.size(); i++)
            {
                ItemOption itemOption = (ItemOption)arrItemMounts[4].options.elementAt(i);
                if (itemOption.optionTemplate.id == 66)
                {
                    vitaWolf = itemOption.param;
                }
            }
        }

        if (isWolf && vitaWolf < 500)
        {
            isWolf = false;
        }

        if (isWolf)
        {
            updateEffwolfMove();
            for (int j = 0; j < vDomsang.size(); j++)
            {
                Domsang domsang = (Domsang)vDomsang.elementAt(j);
                domsang.update();
                if (domsang.frame >= 6)
                {
                    vDomsang.removeElementAt(j);
                }
            }
        }
        else if (isMoto)
        {
            updateEffmotoMove();
            for (int k = 0; k < vDomsang.size(); k++)
            {
                Domsang domsang2 = (Domsang)vDomsang.elementAt(k);
                domsang2.update();
                if (domsang2.frame >= 6)
                {
                    vDomsang.removeElementAt(k);
                }
            }
        }
        else
        {
            for (int l = 0; l < vDomsang.size(); l++)
            {
                Domsang domsang3 = (Domsang)vDomsang.elementAt(l);
                domsang3.update();
                if (domsang3.frame >= 6)
                {
                    vDomsang.removeElementAt(l);
                }
            }
        }

        if (me && Handle.T == null && isAHP && cHP < cMaxHP * aHpValue / 100 && mSystem.currentTimeMillis() - lastUseHP > 5000 && statusMe != 14 && statusMe != 5 && cHP > 0)
        {
            if (vEff.size() == 0)
            {
                for (int m = 0; m < arrItemBag.Length; m++)
                {
                    Item item = arrItemBag[m];
                    if (item != null && item.template.type == 16 && item.template.level == aHpValue)
                    {
                        GameScr.gI().doUseHP();
                        lastUseHP = mSystem.currentTimeMillis();
                        break;
                    }
                }
            }
            else
            {
                for (int n = 0; n < vEff.size(); n++)
                {
                    Effect effect = (Effect)getMyChar().vEff.elementAt(n);
                    if (effect.template.type == 17)
                    {
                        break;
                    }

                    if (n == vEff.size() - 1)
                    {
                        GameScr.gI().doUseHP();
                        lastUseHP = mSystem.currentTimeMillis();
                    }
                }
            }
        }

        if (me && isAFood && Handle.T == null && GameCanvas.gameTick % 10 == 0 && !GameScr.isPaintAuto && statusMe != 14 && statusMe != 5 && cHP > 0)
        {
            if (vEff.size() == 0)
            {
                for (int num = 0; num < arrItemBag.Length; num++)
                {
                    Item item2 = arrItemBag[num];
                    if (item2 != null && item2.template.type == 18 && item2.template.level == aFoodValue)
                    {
                        Service.gI().useItem(num);
                        break;
                    }
                }
            }
            else
            {
                for (int num2 = 0; num2 < vEff.size(); num2++)
                {
                    Effect effect2 = (Effect)getMyChar().vEff.elementAt(num2);
                    if (effect2.template.type == 0)
                    {
                        break;
                    }

                    if (num2 != vEff.size() - 1)
                    {
                        continue;
                    }

                    for (int num3 = 0; num3 < arrItemBag.Length; num3++)
                    {
                        Item item3 = arrItemBag[num3];
                        if (item3 != null && item3.template.type == 18 && item3.template.level == aFoodValue)
                        {
                            Service.gI().useItem(num3);
                            break;
                        }
                    }
                }
            }
        }

        if (me && isABuff && Handle.T == null && getMyChar().statusMe != 14 && getMyChar().statusMe != 5 && cHP > 0 && mSystem.currentTimeMillis() - lastTimeUseSkill > 500)
        {
            for (int num4 = 0; num4 < vSkill.size(); num4++)
            {
                bool flag = false;
                Skill skill = (Skill)vSkill.elementAt(num4);
                if ((skill.template.id >= 67 && skill.template.id <= 72) || skill == null || skill.template.type != 2 || skill.isCooldown())
                {
                    continue;
                }

                for (int num5 = 0; num5 < vEff.size(); num5++)
                {
                    Effect effect3 = (Effect)vEff.elementAt(num5);
                    if (effect3 != null && effect3.template.iconId == skill.template.iconId)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    GameScr.gI().doUseSkill(skill, true, false);
                    Service.gI().sendUseSkillMyBuff();
                    saveLoadPreviousSkill();
                    lastTimeUseSkill = mSystem.currentTimeMillis();
                    break;
                }
            }
        }

        if (me && Handle.T == null && isAMP && cMP < cMaxMP * aMpValue / 100 && GameCanvas.gameTick % 4 == 0 && statusMe != 14 && statusMe != 5 && cHP > 0)
        {
            GameScr.gI().doUseMP();
        }

        if (me && isAResuscitate && nClass.classId == 6 && aCID > 0 && getMyChar().statusMe != 14 && getMyChar().statusMe != 5)
        {
            Char @char = GameScr.findCharInMap(aCID);
            if (@char != null && (@char.cHP <= 0 || @char.statusMe == 14 || @char.statusMe == 5 || isAFocusDie))
            {
                for (int num6 = 0; num6 < vSkill.size(); num6++)
                {
                    Skill skill2 = (Skill)getMyChar().vSkill.elementAt(num6);
                    if (skill2 != null && skill2.template.type == 4)
                    {
                        if (Res.abs(cx - @char.cx) < skill2.dx && Res.abs(cy - @char.cy) < skill2.dy)
                        {
                            getMyChar().myskill = skill2;
                            GameScr.gI().doRescuedOtherChar(aCID);
                            isAFocusDie = false;
                            saveLoadPreviousSkill();
                        }
                        else
                        {
                            InfoMe.addInfo(mResources.LIVE_TEXT, 20, mFont.tahoma_7_white);
                        }

                        break;
                    }
                }
            }
        }

        if (cHP > 0)
        {
            for (int num7 = 0; num7 < vEff.size(); num7++)
            {
                Effect effect4 = (Effect)vEff.elementAt(num7);
                if (effect4.template.type == 0 || effect4.template.type == 12)
                {
                    if (GameCanvas.isEff1)
                    {
                        cHP += effect4.param;
                        cMP += effect4.param;
                    }
                }
                else if (effect4.template.type == 4 || effect4.template.type == 17)
                {
                    if (GameCanvas.isEff1)
                    {
                        cHP += effect4.param;
                    }
                }
                else if (effect4.template.type == 13)
                {
                    if (GameCanvas.isEff1)
                    {
                        cHP -= cMaxHP * 3 / 100;
                        if (cHP < 1)
                        {
                            cHP = 1;
                        }
                    }
                }
                else if (effect4.template.type == 7)
                {
                    isEffBatTu = true;
                }
                else if (effect4.template.type == 1)
                {
                    cHP += autoUpHp;
                }
            }

            if (isEffBatTu)
            {
                count++;
            }
            else
            {
                count = 0;
            }

            if (eff5BuffHp > 0 && GameCanvas.isEff2)
            {
                cHP += eff5BuffHp;
            }

            if (eff5BuffMp > 0 && GameCanvas.isEff2)
            {
                cMP += eff5BuffMp;
            }

            if (cHP > cMaxHP)
            {
                cHP = cMaxHP;
            }

            if (cMP > cMaxMP)
            {
                cMP = cMaxMP;
            }
        }

        if (cmtoChar)
        {
            GameScr.cmtoX = cx - GameScr.gW2 + GameScr.gW6 * cdir;
            GameScr.cmtoY = cy - GameScr.gH23;
        }

        tick = (tick + 1) % 100;
        if (me)
        {
            if (charFocus != null && !GameScr.vCharInMap.contains(charFocus))
            {
                charFocus = null;
            }

            if (cx < 10)
            {
                cvx = 0;
                cx = 10;
            }
            else if (cx > TileMap.pxw - 10)
            {
                cx = TileMap.pxw - 10;
                cvx = 0;
            }

            if (!ischangingMap && isInWaypoint())
            {
                isSendMove = true;
                Service.gI().requestChangeMap();
                isLockKey = true;
                ischangingMap = true;
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                return;
            }

            if (isBlinking)
            {
                isBlinking = mSystem.currentTimeMillis() - timeStartBlink < 2000;
            }
            else if (statusMe != 4 && (Res.abs(cx - cxSend) >= 90 || Res.abs(cy - cySend) >= 90) && cy - cySend > 0)
            {
            }

            if (isLockMove)
            {
                currentMovePoint = null;
            }

            if (currentMovePoint != null && (statusMe == 1 || statusMe == 2))
            {
                statusMe = 2;
                if (cx - currentMovePoint.xEnd > 0)
                {
                    cdir = -1;
                    if (cx - currentMovePoint.xEnd <= 10)
                    {
                        currentMovePoint = null;
                    }
                }
                else
                {
                    cdir = 1;
                    if (cx - currentMovePoint.xEnd >= -10)
                    {
                        currentMovePoint = null;
                    }
                }

                if (currentMovePoint != null)
                {
                    cvx = getSpeed() * cdir;
                    cvy = 0;
                }
            }

            searchFocus();
            if (statusMe == 1 || statusMe == 6)
            {
                if (mSystem.currentTimeMillis() - currMove > 500 && isLastMove)
                {
                    isSendMove = true;
                    isLastMove = false;
                    currMove = mSystem.currentTimeMillis();
                }
            }
            else
            {
                currMove = mSystem.currentTimeMillis();
                isLastMove = true;
            }
        }
        else
        {
            if (GameCanvas.gameTick % 20 == 0 && charID >= 0)
            {
                for (int num8 = 0; num8 < GameScr.vCharInMap.size(); num8++)
                {
                    Char char2 = null;
                    try
                    {
                        char2 = (Char)GameScr.vCharInMap.elementAt(num8);
                    }
                    catch (Exception)
                    {
                    }

                    if (char2 == null || char2.Equals(this))
                    {
                    }
                }
            }

            if (statusMe == 1 || statusMe == 6)
            {
                bool flag2 = false;
                if (currentMovePoint != null)
                {
                    if (abs(currentMovePoint.xEnd - cx) < 4 && abs(currentMovePoint.yEnd - cy) < 4)
                    {
                        cx = currentMovePoint.xEnd;
                        cy = currentMovePoint.yEnd;
                        currentMovePoint = null;
                        if ((TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) == TileMap.T_TOP)
                        {
                            changeStatusStand();
                            GameCanvas.gI().startDust(-1, cx - -8, cy);
                            GameCanvas.gI().startDust(1, cx - 8, cy);
                        }
                        else
                        {
                            statusMe = 4;
                            cvy = 0;
                        }

                        flag2 = true;
                    }
                    else if (cy == currentMovePoint.yEnd)
                    {
                        if (cx != currentMovePoint.xEnd)
                        {
                            cx = (cx + currentMovePoint.xEnd) / 2;
                            cf = GameCanvas.gameTick % 5 + 2;
                        }
                    }
                    else if (cy < currentMovePoint.yEnd)
                    {
                        cf = 12;
                        cx = (cx + currentMovePoint.xEnd) / 2;
                        if (cvy < 0)
                        {
                            cvy = 0;
                        }

                        cy += cvy;
                        if ((TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) == TileMap.T_TOP)
                        {
                            GameCanvas.gI().startDust(-1, cx - -8, cy);
                            GameCanvas.gI().startDust(1, cx - 8, cy);
                        }

                        cvy++;
                        if (cvy > 16)
                        {
                            cy = (cy + currentMovePoint.yEnd) / 2;
                        }
                    }
                    else
                    {
                        cf = 7;
                        cx = (cx + currentMovePoint.xEnd) / 2;
                        cy = (cy + currentMovePoint.yEnd) / 2;
                    }
                }
                else
                {
                    flag2 = true;
                }

                if (flag2 && vMovePoints.size() > 0)
                {
                    if (vMovePoints.size() > 5)
                    {
                        currentMovePoint = (MovePoint)vMovePoints.lastElement();
                        vMovePoints.removeElementAt(0);
                        cx = currentMovePoint.xEnd;
                        cy = currentMovePoint.yEnd;
                        vMovePoints.removeAllElements();
                        statusMe = 6;
                        currentMovePoint = null;
                        return;
                    }

                    currentMovePoint = (MovePoint)vMovePoints.firstElement();
                    vMovePoints.removeElementAt(0);
                    if (currentMovePoint.status == 2)
                    {
                        statusMe = 2;
                        if (cx - currentMovePoint.xEnd > 0)
                        {
                            cdir = -1;
                        }
                        else if (cx - currentMovePoint.xEnd < 0)
                        {
                            cdir = 1;
                        }

                        cvx = 5 * cdir;
                        cvy = 0;
                    }
                    else if (currentMovePoint.status == 3)
                    {
                        statusMe = 3;
                        GameCanvas.gI().startDust(-1, cx - -8, cy);
                        GameCanvas.gI().startDust(1, cx - 8, cy);
                        if (cx - currentMovePoint.xEnd > 0)
                        {
                            cdir = -1;
                        }
                        else if (cx - currentMovePoint.xEnd < 0)
                        {
                            cdir = 1;
                        }

                        cvx = abs(cx - currentMovePoint.xEnd) / 9 * cdir;
                        cvy = -10;
                    }
                    else if (currentMovePoint.status == 4)
                    {
                        statusMe = 4;
                        if (cx - currentMovePoint.xEnd > 0)
                        {
                            cdir = -1;
                        }
                        else if (cx - currentMovePoint.xEnd < 0)
                        {
                            cdir = 1;
                        }

                        cvx = abs(cx - currentMovePoint.xEnd) / 9 * cdir;
                        cvy = 0;
                    }
                    else
                    {
                        cx = currentMovePoint.xEnd;
                        cy = currentMovePoint.yEnd;
                        currentMovePoint = null;
                    }
                }

                if (statusMe == 6)
                {
                    if (cf >= 8 && cf <= 11)
                    {
                        cf++;
                        cp1++;
                        if (cf > 11)
                        {
                            cf = 8;
                        }

                        if (cp1 > 5)
                        {
                            cf = 0;
                        }
                    }

                    if (cf <= 1)
                    {
                        cp1++;
                        if (cp1 > 6)
                        {
                            cf = 0;
                        }
                        else
                        {
                            cf = 1;
                        }

                        if (cp1 > 10)
                        {
                            cp1 = 0;
                        }
                    }
                }

                lcx = cx;
                lcy = cy;
                if (mSystem.currentTimeMillis() - timeSummon > 7000)
                {
                    if (!isWolf && isHaveWolf() && vitaWolf >= 500)
                    {
                        isWolf = true;
                        ServerEffect.addServerEffect(60, this, 1);
                    }

                    if (isMoto && isHaveMoto())
                    {
                        isMoto = false;
                        isMotoBehind = true;
                    }
                }
            }
        }

        if (isInjure > 0)
        {
            cf = 21;
            isInjure = (sbyte)(isInjure - 1);
        }
        else
        {
            switch (statusMe)
            {
                case 1:
                    isBocdau = false;
                    timeBocdau = 0;
                    if (isWolf)
                    {
                        if (cdir == 1)
                        {
                            EffdefautX = cx + 21 + 4;
                            EffdefautY = cy - 15;
                        }
                        else
                        {
                            EffdefautX = cx - 24 - 4;
                            EffdefautY = cy - 15;
                        }
                    }

                    updateCharStand();
                    break;
                case 2:
                    if (isMoto)
                    {
                        timeBocdau++;
                        if (arrItemMounts[4].template.id == 485 && arrItemMounts[4].sys >= 4)
                        {
                            isBocdau = true;
                        }

                        if (timeBocdau > 20)
                        {
                            isBocdau = false;
                        }
                    }

                    if (isWolf)
                    {
                        if (cdir == 1)
                        {
                            if (idWolfW[tickWolf] == 1737)
                            {
                                EffdefautX = cx + 21 + 4;
                                EffdefautY = cy - 19;
                            }
                            else
                            {
                                EffdefautX = cx + 21 + 4;
                                EffdefautY = cy - 16;
                            }
                        }
                        else if (idWolfW[tickWolf] == 1737)
                        {
                            EffdefautX = cx - 24 - 4;
                            EffdefautY = cy - 19;
                        }
                        else
                        {
                            EffdefautX = cx - 24 - 4;
                            EffdefautY = cy - 16;
                        }
                    }
                    else if (isMoto)
                    {
                        if (cdir == 1)
                        {
                            EffdefautX = cx + 15;
                            EffdefautX1 = cx - 25;
                            EffdefautY = cy;
                            EffdefautY1 = cy;
                        }
                        else
                        {
                            EffdefautX = cx - 18;
                            EffdefautX1 = cx + 25;
                            EffdefautY = cy;
                            EffdefautY1 = cy;
                        }
                    }

                    updateCharRun();
                    break;
                case 3:
                    isBocdau = false;
                    timeBocdau = 0;
                    if (isWolf)
                    {
                        if (cdir == 1)
                        {
                            EffdefautX = cx + 21 + 4;
                            EffdefautY = cy - 30;
                        }
                        else
                        {
                            EffdefautX = cx - 23 - 4;
                            EffdefautY = cy - 30;
                        }
                    }

                    updateCharJump();
                    break;
                case 4:
                    isBocdau = false;
                    timeBocdau = 0;
                    if (isWolf)
                    {
                        if (cdir == 1)
                        {
                            EffdefautX = cx + 21 + 4;
                            EffdefautY = cy - 19;
                        }
                        else
                        {
                            EffdefautX = cx - 24;
                            EffdefautY = cy - 20;
                        }
                    }

                    updateCharFall();
                    break;
                case 5:
                    isBocdau = false;
                    timeBocdau = 0;
                    updateCharDeadFly();
                    break;
                case 9:
                    isBocdau = false;
                    timeBocdau = 0;
                    updateCharAutoJump();
                    break;
                case 10:
                    isBocdau = false;
                    timeBocdau = 0;
                    updateCharWaterRun();
                    break;
                case 11:
                    isBocdau = false;
                    timeBocdau = 0;
                    updateCharWaterDown();
                    break;
                case 12:
                    isBocdau = false;
                    timeBocdau = 0;
                    updateSkillStand();
                    break;
                case 13:
                    isBocdau = false;
                    timeBocdau = 0;
                    updateSkillFall();
                    break;
                case 14:
                    isBocdau = false;
                    timeBocdau = 0;
                    break;
                case 6:
                    isBocdau = false;
                    timeBocdau = 0;
                    if (cf == 21 && isInjure <= 0)
                    {
                        cf = 0;
                    }

                    break;
            }
        }

        if (wdx != 0 || wdy != 0)
        {
            startDie(wdx, wdy);
            wdx = 0;
            wdy = 0;
        }

        if (moveFast != null)
        {
            if (moveFast[0] == 0)
            {
                moveFast[0]++;
                ServerEffect.addServerEffect(60, this, 1);
            }
            else if (moveFast[0] < 10)
            {
                moveFast[0]++;
            }
            else
            {
                cx = moveFast[1];
                cy = moveFast[2];
                moveFast = null;
                ServerEffect.addServerEffect(60, this, 1);
                if (me)
                {
                    if ((TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) != TileMap.T_TOP)
                    {
                        statusMe = 4;
                        getMyChar().setAutoSkillPaint(GameScr.sks[38], 1);
                    }
                    else
                    {
                        getMyChar().setAutoSkillPaint(GameScr.sks[38], 0);
                    }
                }
            }
        }

        if (!me && vMovePoints.size() == 0 && cxMoveLast != 0 && cyMoveLast != 0 && currentMovePoint == null)
        {
            if (cxMoveLast != cx)
            {
                cx = cxMoveLast;
            }

            if (cyMoveLast != cy)
            {
                cy = cyMoveLast;
            }

            if (cHP > 0)
            {
                statusMe = 6;
            }
        }

        updateEffectWolf();
        fixMove();
        updateDataEff(0, statusMe);
        if (ID_HORSE > 0)
        {
            isNewMount = false;
            isMoto = false;
            isWolf = false;
        }

        updNewMount();
        updateFramecharByhorse();
    }

    public void updateNew_MobVithu()
    {
        if (mobVithu != null)
        {
            mobVithu.owner = this;
        }

        if (mobVithu.getTemplate() != null && mobVithu.getTemplate().typeFly == 1)
        {
            if (Mob.getDistance(mobVithu.x, mobVithu.y, cx, cy) > Mob.arrMobTemplate[mobVithu.templateId].rangeMove + 100)
            {
                mobVithu.xFirst = cx + (3 - GameCanvas.gameTick % 6) * 6;
                mobVithu.yFirst = cy - 60;
                int num = mobVithu.xFirst - mobVithu.x;
                int num2 = mobVithu.yFirst - mobVithu.y;
                if (num > 50 || num < -50)
                {
                    mobVithu.x += num / 10;
                }

                if (num2 > 50 || num2 < -50)
                {
                    mobVithu.y += num2 / 10;
                }
            }

            mobVithu.update();
            return;
        }

        if (mobVithu.status != 3)
        {
            if (cdir == -1)
            {
                mobVithu.xFirst = cx + 20;
                mobVithu.yFirst = cy;
                mobVithu.dir = cdir;
                mobVithu.y = cy - 20;
            }
            else
            {
                mobVithu.xFirst = cx - 20;
                mobVithu.yFirst = cy;
                mobVithu.dir = cdir;
                mobVithu.y = cy - 20;
            }

            int num3 = mobVithu.xFirst - mobVithu.x;
            int num4 = mobVithu.yFirst - mobVithu.y;
            if (num3 > 50 || num3 < -50)
            {
                mobVithu.x += num3 / 10;
            }
            else
            {
                mobVithu.x += num3;
            }

            if (num4 > 50 || num4 < -50)
            {
                mobVithu.y += num4 / 10;
            }
        }

        mobVithu.update();
    }

    public void updateNew_NobMe()
    {
        if (mobMe != null)
        {
            mobMe.owner = this;
        }

        if (mobMe.templateId == 122 || mobMe.templateId == 70 || (mobMe.getTemplate() != null && mobMe.getTemplate().typeFly == 1))
        {
            if (mobMe.status != 3)
            {
                mobMe.xFirst = cx + (3 - GameCanvas.gameTick % 6) * 6;
                mobMe.yFirst = cy - 60;
            }

            mobMe.update();
            return;
        }

        if (mobMe.status != 3)
        {
            if (cdir == -1)
            {
                mobMe.xFirst = cx + 20;
                mobMe.yFirst = cy;
                mobMe.dir = cdir;
                mobMe.y = cy - 20;
            }
            else
            {
                mobMe.xFirst = cx - 20;
                mobMe.yFirst = cy;
                mobMe.dir = cdir;
                mobMe.y = cy - 20;
            }

            int num = mobMe.xFirst - mobMe.x;
            int num2 = mobMe.yFirst - mobMe.y;
            if (num > 50 || num < -50)
            {
                mobMe.x += num / 10;
            }
            else
            {
                mobMe.x += num;
            }

            if (num2 > 50 || num2 < -50)
            {
                mobMe.y += num2 / 10;
            }
        }

        mobMe.update();
    }

    private void updateMobMe()
    {
        if (mobMe != null && mobMe.templateId >= 236)
        {
            updateNew_NobMe();
            return;
        }

        if (mobMe.templateId == 122 || mobMe.templateId == 70 || (mobMe.getTemplate() != null && mobMe.getTemplate().typeFly == 1))
        {
            if (mobMe.status != 3)
            {
                mobMe.xFirst = cx + (3 - GameCanvas.gameTick % 6) * 6;
                mobMe.yFirst = cy - 60;
                int num = mobMe.xFirst - mobMe.x;
                int num2 = mobMe.yFirst - mobMe.y;
                if (num > 50 || num < -50)
                {
                    mobMe.x += num / 10;
                }

                if (num2 > 50 || num2 < -50)
                {
                    mobMe.y += num2 / 10;
                }
            }

            mobMe.update();
            return;
        }

        if (mobMe.status != 3)
        {
            if (cdir == -1)
            {
                mobMe.xFirst = cx + 20;
                mobMe.yFirst = cy;
                mobMe.dir = cdir;
                mobMe.y = cy - 20;
            }
            else
            {
                mobMe.xFirst = cx - 20;
                mobMe.yFirst = cy;
                mobMe.dir = cdir;
                mobMe.y = cy - 20;
            }

            int num3 = mobMe.xFirst - mobMe.x;
            int num4 = mobMe.yFirst - mobMe.y;
            if (num3 > 50 || num3 < -50)
            {
                mobMe.x += num3 / 10;
            }
            else
            {
                mobMe.x += num3;
            }

            if (num4 > 50 || num4 < -50)
            {
                mobMe.y += num4 / 10;
            }
        }

        mobMe.update();
    }

    private void updateCharDeadFly()
    {
        cp1++;
        cx += (cp2 - cx) / 4;
        if (cp1 > 7)
        {
            cy += (cp3 - cy) / 4;
        }
        else
        {
            cy += cp1 - 10;
        }

        if (Res.abs(cp2 - cx) < 4 && Res.abs(cp3 - cy) < 10)
        {
            cx = cp2;
            cy = cp3;
            statusMe = 14;
            callEff(60);
            if (me)
            {
                GameScr.gI().resetButton();
            }
        }

        cf = 21;
    }

    public void updateCharStand()
    {
        isAttack = false;
        isAttFly = false;
        cvx = 0;
        cvy = 0;
        cp1++;
        lcx = cx;
        lcy = cy;
        if (cp1 > 30)
        {
            cp1 = 0;
        }

        if (cp1 % 15 < 5)
        {
            cf = 0;
        }
        else
        {
            cf = 1;
        }

        updateCharInBridge();
        if (mSystem.currentTimeMillis() - timeSummon > 7000)
        {
            if (!isWolf && isHaveWolf() && vitaWolf >= 500)
            {
                isWolf = true;
                ServerEffect.addServerEffect(60, this, 1);
            }

            if (isMoto && isHaveMoto())
            {
                isMoto = false;
                isMotoBehind = true;
            }
        }
    }

    public void updateCharRun()
    {
        int num = 0;
        if (!me && currentMovePoint != null)
        {
            num = abs(cx - currentMovePoint.xEnd);
        }

        cp1++;
        if (cp1 >= 10)
        {
            cp1 = 0;
            cBonusSpeed = 0;
        }

        cf = (cp1 >> 1) + 2;
        if ((TileMap.tileTypeAtPixel(cx, cy - 1) & TileMap.T_WATERFLOW) == TileMap.T_WATERFLOW)
        {
            cx += cvx >> 1;
        }
        else
        {
            cx += cvx;
        }

        if (cdir == 1)
        {
            if (GameScr.auto != 1)
            {
                if (TileMap.tileTypeAt(cx + chw, cy - chh, TileMap.T_LEFT))
                {
                    if (me)
                    {
                        cvx = 0;
                        cx = TileMap.tileXofPixel(cx + chw) - chw;
                    }
                    else
                    {
                        stop();
                    }
                }
            }
            else if (TileMap.tileTypeAt(cx + chw, cy - chh, TileMap.T_LEFT))
            {
                if (me)
                {
                    statusMe = 3;
                    if (statusMe == 3)
                    {
                        cvy -= 10;
                    }
                }
                else
                {
                    stop();
                }
            }
        }
        else if (GameScr.auto != 1)
        {
            if (TileMap.tileTypeAt(cx - chw - 1, cy - chh, TileMap.T_RIGHT))
            {
                if (me)
                {
                    cvx = 0;
                    cx = TileMap.tileXofPixel(cx - chw - 1) + TileMap.size + chw;
                }
                else
                {
                    stop();
                }
            }
        }
        else if (TileMap.tileTypeAt(cx - chw - 1, cy - chh, TileMap.T_RIGHT))
        {
            if (me)
            {
                statusMe = 3;
                if (statusMe == 3)
                {
                    cvy -= 10;
                }
            }
            else
            {
                stop();
            }
        }

        if (!isNewMount && isHaveNewMount())
        {
            isNewMount = true;
            isMotoBehind = false;
        }

        if (!isMoto && isHaveMoto())
        {
            isMoto = true;
            isMotoBehind = false;
        }

        if (!isWolf && isHaveWolf() && vitaWolf >= 500)
        {
            dx0 = Res.abs(cx - lcx);
            dy0 = Res.abs(cy - lcy);
            dx0 = ((dx0 <= dy0) ? dy0 : dx0);
            if ((me && dx0 > 150) || (!me && dx0 > 40))
            {
                isWolf = true;
                ServerEffect.addServerEffect(60, this, 1);
            }

            dx0 = (dy0 = 0);
        }

        if (me)
        {
            if (cvx > 0)
            {
                cvx--;
            }
            else if (cvx < 0)
            {
                cvx++;
            }
            else
            {
                if (cx - cxSend != 0)
                {
                }

                changeStatusStand();
                cBonusSpeed = 0;
            }
        }

        if ((TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) != TileMap.T_TOP)
        {
            if (me)
            {
                cf = 7;
                statusMe = 4;
                cvx = 3 * cdir;
                cp2 = 0;
            }
            else
            {
                stop();
            }
        }

        if (!me && currentMovePoint != null)
        {
            int num2 = abs(cx - currentMovePoint.xEnd);
            if (num2 > num)
            {
                stop();
            }
        }

        if (isMoto || isNewMount)
        {
            if (GameCanvas.gameTick % 5 == 0)
            {
                ServerEffect.addServerEffect(120, cx - (cdir << 5), cy, 0, (sbyte)cdir);
            }
        }
        else
        {
            GameCanvas.gI().startDust(cdir, cx - (cdir << 3), cy);
        }

        updateCharInBridge();
        startCoatEffect(cx - (cdir << 4), cy);
    }

    public void updateCharWaterRun()
    {
        if (!TileMap.tileTypeAt(cx, cy, TileMap.T_WATERFLOW))
        {
            statusMe = 4;
        }

        cp1++;
        if (cp1 >= 5)
        {
            cp1 = 0;
            cBonusSpeed = 0;
        }

        cf = cp1 + 2;
        if (cdir == 1)
        {
            if (TileMap.tileTypeAt(cx + chw, cy - 1, TileMap.T_LEFT))
            {
                cvx = 0;
                cx = TileMap.tileXofPixel(cx + chw) - chw;
            }
        }
        else if (TileMap.tileTypeAt(cx - chw - 1, cy - 1, TileMap.T_RIGHT))
        {
            cvx = 0;
            cx = TileMap.tileXofPixel(cx - chw - 1) + TileMap.size + chw;
        }

        cx += cvx;
        if (cvx > 0)
        {
            cvx--;
        }
        else if (cvx < 0)
        {
            cvx++;
        }
        else
        {
            if (me && mSystem.currentTimeMillis() - (timelastSendmove + delaySendmove) >= 0)
            {
                isSendMove = true;
            }

            statusMe = 11;
            cBonusSpeed = 0;
        }

        if (GameCanvas.gameTick % 8 == 0)
        {
        }

        GameCanvas.gI().startWaterSplash(cx, cy);
        GameCanvas.gI().startDust(cdir, cx - (cdir << 3), cy);
    }

    public void updateCharWaterDown()
    {
        cy += cvy;
        if (cvy < 20 && GameCanvas.gameTick % 2 == 0)
        {
            cvy++;
        }

        cf = 7;
        if (cy >= TileMap.pxh)
        {
            cHP = 0;
            statusMe = 5;
        }
        else if (TileMap.tileTypeAt(cx, cy, TileMap.T_TOP))
        {
            changeStatusStand();
            cvx = (cvy = 0);
            cp1 = (cp2 = 0);
            cy = TileMap.tileXofPixel(cy);
        }
        else if (TileMap.tileTypeAt(cx, cy, TileMap.T_UNDERWATER))
        {
            cHP = 0;
            statusMe = 5;
        }
    }

    public void updatefHorseui()
    {
        if (GameCanvas.gameTick % 3 == 0)
        {
            DataSkillEff partEff = getPartEff(ID_HORSE, true);
            if (partEff == null || !partEff.isLoadData)
            {
                fhorseui = null;
            }
            else
            {
                fhorsetrangbi = (fhorsetrangbi + 1) % partEff.ActionStand.Length;
            }
        }
    }

    public void updateCharInBridge()
    {
        if (!GameCanvas.lowGraphic && !Main.isIpod && mGraphics.zoomLevel != 1)
        {
            if (TileMap.tileTypeAt(cx, cy + 1, TileMap.T_BRIDGE))
            {
                TileMap.setTileTypeAtPixel(cx, cy + 1, TileMap.T_DOWN1PIXEL);
                TileMap.setTileTypeAtPixel(cx, cy - 2, TileMap.T_DOWN1PIXEL);
            }

            if (TileMap.tileTypeAt(cx - TileMap.size, cy + 1, TileMap.T_DOWN1PIXEL))
            {
                TileMap.killTileTypeAt(cx - TileMap.size, cy + 1, TileMap.T_DOWN1PIXEL);
                TileMap.killTileTypeAt(cx - TileMap.size, cy - 2, TileMap.T_DOWN1PIXEL);
            }

            if (TileMap.tileTypeAt(cx + TileMap.size, cy + 1, TileMap.T_DOWN1PIXEL))
            {
                TileMap.killTileTypeAt(cx + TileMap.size, cy + 1, TileMap.T_DOWN1PIXEL);
                TileMap.killTileTypeAt(cx + TileMap.size, cy - 2, TileMap.T_DOWN1PIXEL);
            }
        }
    }

    public void updateKickOption()
    {
        int num = 2;
        int num2 = 2;
        int num3 = 2;
        if (arrItemBody[0] == null)
        {
            num--;
        }

        if (arrItemBody[6] == null)
        {
            num--;
        }

        if (arrItemBody[5] == null)
        {
            num--;
        }

        kickOption(arrItemBody[0], num);
        kickOption(arrItemBody[6], num);
        kickOption(arrItemBody[5], num);
        if (arrItemBody[2] == null)
        {
            num2--;
        }

        if (arrItemBody[8] == null)
        {
            num2--;
        }

        if (arrItemBody[7] == null)
        {
            num2--;
        }

        kickOption(arrItemBody[2], num2);
        kickOption(arrItemBody[8], num2);
        kickOption(arrItemBody[7], num2);
        if (arrItemBody[4] == null)
        {
            num3--;
        }

        if (arrItemBody[3] == null)
        {
            num3--;
        }

        if (arrItemBody[9] == null)
        {
            num3--;
        }

        if (arrItemBody[1] != null)
        {
            if (arrItemBody[1].sys == getSys())
            {
                if (arrItemBody[1].options != null)
                {
                    for (int i = 0; i < arrItemBody[1].options.size(); i++)
                    {
                        ItemOption itemOption = (ItemOption)arrItemBody[1].options.elementAt(i);
                        if (itemOption.optionTemplate.type == 2)
                        {
                            itemOption.active = 1;
                        }
                    }
                }
            }
            else if (arrItemBody[1].options != null)
            {
                for (int j = 0; j < arrItemBody[1].options.size(); j++)
                {
                    ItemOption itemOption2 = (ItemOption)arrItemBody[1].options.elementAt(j);
                    if (itemOption2.optionTemplate.type == 2)
                    {
                        itemOption2.active = 0;
                    }
                }
            }
        }

        kickOption(arrItemBody[4], num3);
        kickOption(arrItemBody[3], num3);
        kickOption(arrItemBody[9], num3);
    }

    public void update_No()
    {
    }
}
