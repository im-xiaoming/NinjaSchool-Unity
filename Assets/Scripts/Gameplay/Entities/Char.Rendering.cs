using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    private void updateSkillPaint()
    {
        if (statusMe == 14 || statusMe == 5)
        {
            return;
        }

        if (skillPaint != null && mobFocus != null && mobFocus.status == 0)
        {
            if (!me)
            {
                if ((TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) == TileMap.T_TOP)
                {
                    changeStatusStand();
                }
                else
                {
                    statusMe = 6;
                }
            }

            indexSkill = 0;
            skillPaint = null;
            eff0 = (eff1 = (eff2 = null));
            i0 = (i1 = (i2 = 0));
            mobFocus = null;
            effPaints = null;
            arr = null;
        }

        SkillInfoPaint[] array = skillInfoPaint1();
        if (array == null)
        {
            return;
        }

        // The original client updateSkillPaint/setAttack assumes one paint per game tick.
        // Offline timing deviation: Unity may paint several frames between logic ticks, so
        // indexSkill can pass the hit frame (or the animation end). Dispatch once before
        // cleanup instead of requiring equality, preserving the original hit-frame timing.
        if (!skillActionTriggered && indexSkill >= array.Length - array.Length / 3)
        {
            if (me && myskill.template.type == 2)
            {
                skillActionTriggered = true;
                Service.gI().sendUseSkillMyBuff();
                saveLoadPreviousSkill();
            }
            else if ((mobFocus != null || (!me && charFocus != null) || (me && charFocus != null && isMeCanAttackOtherPlayer(charFocus))) && arr == null)
            {
                setAttack();
                if (me)
                {
                    saveLoadPreviousSkill();
                }
            }
        }

        if (skillPaint != null && arr == null && indexSkill >= skillInfoPaint1().Length)
        {
            if (!me)
            {
                if ((TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) == TileMap.T_TOP)
                {
                    changeStatusStand();
                }
                else
                {
                    statusMe = 6;
                }
            }

            indexSkill = 0;
            skillPaint = null;
            eff0 = (eff1 = (eff2 = null));
            i0 = (i1 = (i2 = 0));
            arr = null;
        }
    }

    public void setSkillPaint(SkillPaint skillPaint, int sType)
    {
        if (Handle.T != null)
        {
            return;
        }

        long currentTimeMillis = mSystem.getCurrentTimeMillis();
        if (me)
        {
            if (currentTimeMillis - myskill.lastTimeUseThisSkill < myskill.coolDown)
            {
                myskill.paintCanNotUseSkill = true;
                return;
            }

            myskill.lastTimeUseThisSkill = currentTimeMillis;
            cMP -= myskill.manaUse;
            if (cMP < 0)
            {
                cMP = 0;
            }

            playSound(skillPaint);
        }
        else if (isCharInScreen(this))
        {
            playSound(skillPaint);
        }

        setAutoSkillPaint(skillPaint, sType);
    }

    public void setAutoSkillPaint(SkillPaint skillPaint, int sType)
    {
        this.skillPaint = skillPaint;
        skillActionTriggered = false;
        this.sType = sType;
        indexSkill = 0;
        i0 = (i1 = (i2 = (dx0 = (dx1 = (dx2 = (dy0 = (dy1 = (dy2 = 0))))))));
        eff0 = null;
        eff1 = null;
        eff2 = null;
    }

    public SkillInfoPaint[] skillInfoPaint1()
    {
        if (skillPaint == null)
        {
            return null;
        }

        if (sType == 0)
        {
            return skillPaint.skillStand;
        }

        return skillPaint.skillfly;
    }

    public void paintHead(mGraphics g, int xStart, int yStart)
    {
        Part part = GameScr.parts[head];
        Part part2 = GameScr.parts[leg];
        Part part3 = GameScr.parts[body];
        if (arrItemBody != null && arrItemBody[11] != null)
        {
            part = GameScr.parts[arrItemBody[11].template.part];
        }

        if (cdir == 1)
        {
            SmallImage.drawSmallImage(g, part.pi[CharInfo[cf][0][0]].id, xStart + CharInfo[cf][0][1] + part.pi[CharInfo[cf][0][0]].dx, yStart - CharInfo[cf][0][2] + part.pi[CharInfo[cf][0][0]].dy, 0, 0);
            SmallImage.drawSmallImage(g, part2.pi[CharInfo[cf][1][0]].id, xStart + CharInfo[cf][1][1] + part2.pi[CharInfo[cf][1][0]].dx, yStart - CharInfo[cf][1][2] + part2.pi[CharInfo[cf][1][0]].dy, 0, 0);
            SmallImage.drawSmallImage(g, part3.pi[CharInfo[cf][2][0]].id, xStart + CharInfo[cf][2][1] + part3.pi[CharInfo[cf][2][0]].dx, yStart - CharInfo[cf][2][2] + part3.pi[CharInfo[cf][2][0]].dy, 0, 0);
        }
        else
        {
            SmallImage.drawSmallImage(g, part.pi[CharInfo[cf][0][0]].id, xStart - CharInfo[cf][0][1] - part.pi[CharInfo[cf][0][0]].dx, yStart - CharInfo[cf][0][2] + part.pi[CharInfo[cf][0][0]].dy, 2, 24);
            SmallImage.drawSmallImage(g, part2.pi[CharInfo[cf][1][0]].id, xStart - CharInfo[cf][1][1] - part2.pi[CharInfo[cf][1][0]].dx, yStart - CharInfo[cf][1][2] + part2.pi[CharInfo[cf][1][0]].dy, 2, 24);
            SmallImage.drawSmallImage(g, part3.pi[CharInfo[cf][2][0]].id, xStart - CharInfo[cf][2][1] - part3.pi[CharInfo[cf][2][0]].dx, yStart - CharInfo[cf][2][2] + part3.pi[CharInfo[cf][2][0]].dy, 2, 24);
        }
    }

    public bool isPaint()
    {
        if (cx < GameScr.cmx)
        {
            return false;
        }

        if (cx > GameScr.cmx + GameScr.gW)
        {
            return false;
        }

        if (cy < GameScr.cmy)
        {
            return false;
        }

        if (cy > GameScr.cmy + GameScr.gH + 30)
        {
            return false;
        }

        return true;
    }

    public virtual void paint(mGraphics g)
    {
        using (Interp.Offset(g, this))
            PaintAtRenderPosition(g);
    }

    private void PaintAtRenderPosition(mGraphics g)
    {
        if (!isPaint())
        {
            if (skillPaint != null)
            {
                indexSkill = skillInfoPaint1().Length;
                skillPaint = null;
                effPaints = null;
                eff = null;
                effTask = null;
                indexEff = -1;
                indexEffTask = -1;
            }

            return;
        }

        paintBottomDataEff(g, cx, cy, -heightCharName);
        if (statusMe == 15 || (moveFast != null && moveFast[0] > 0))
        {
            return;
        }

        if (statusMe == 1 || statusMe == 6)
        {
            paint_No(g);
        }

        paintCharName_HP_MP_Overhead(g);
        if (skillPaint != null && indexSkill < skillInfoPaint1().Length)
        {
            paintCharWithSkill(g);
        }
        else
        {
            paintCharWithoutSkill(g);
        }

        paintArrowAttack(g);
        if (effPaints != null)
        {
            for (int i = 0; i < effPaints.Length; i++)
            {
                if (effPaints[i] == null)
                {
                    continue;
                }

                if (effPaints[i].eMob != null)
                {
                    if (!effPaints[i].isFly)
                    {
                        effPaints[i].eMob.setInjure();
                        effPaints[i].eMob.injureBy = this;
                        if (me)
                        {
                            effPaints[i].eMob.hpInjure = getMyChar().cdame / 2 - getMyChar().cdame * NinjaUtil.randomNumber(11) / 100;
                        }

                        if (effPaints[i].eMob.templateId != 98 && effPaints[i].eMob.templateId != 99)
                        {
                            GameScr.startSplash(effPaints[i].eMob.x, effPaints[i].eMob.y - (effPaints[i].eMob.h >> 1), cdir);
                        }

                        effPaints[i].isFly = true;
                    }

                    SmallImage.drawSmallImage(g, effPaints[i].getImgId(), effPaints[i].eMob.x, effPaints[i].eMob.y, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                }
                else if (effPaints[i].eChar != null)
                {
                    if (!effPaints[i].isFly)
                    {
                        if (effPaints[i].eChar.charID >= 0)
                        {
                            effPaints[i].eChar.doInjure();
                        }

                        GameScr.startSplash(effPaints[i].eChar.cx, effPaints[i].eChar.cy - (effPaints[i].eChar.ch >> 1), cdir);
                        effPaints[i].isFly = true;
                    }

                    SmallImage.drawSmallImage(g, effPaints[i].getImgId(), effPaints[i].eChar.cx, effPaints[i].eChar.cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                }

                effPaints[i].index++;
                if (effPaints[i].index >= effPaints[i].effCharPaint.arrEfInfo.Length)
                {
                    effPaints[i] = null;
                }
            }
        }

        if (indexEff >= 0 && eff != null)
        {
            SmallImage.drawSmallImage(g, eff.arrEfInfo[indexEff].idImg, cx + eff.arrEfInfo[indexEff].dx, cy + eff.arrEfInfo[indexEff].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            if (Main.isPC)
            {
                if (GameCanvas.gameTick % 2 == 0)
                {
                    indexEff++;
                    if (indexEff >= eff.arrEfInfo.Length)
                    {
                        indexEff = -1;
                        eff = null;
                    }
                }
            }
            else
            {
                indexEff++;
                if (indexEff >= eff.arrEfInfo.Length)
                {
                    indexEff = -1;
                    eff = null;
                }
            }
        }

        if (indexEffTask >= 0 && effTask != null)
        {
            SmallImage.drawSmallImage(g, effTask.arrEfInfo[indexEffTask].idImg, cx + effTask.arrEfInfo[indexEffTask].dx, cy + effTask.arrEfInfo[indexEffTask].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            if (GameCanvas.gameTick % 2 == 0)
            {
                indexEffTask++;
                if (indexEffTask >= effTask.arrEfInfo.Length)
                {
                    indexEffTask = -1;
                    effTask = null;
                }
            }
        }

        if (isEffBatTu)
        {
            if (count < 2)
            {
                SmallImage.drawSmallImage(g, 1366, cx, cy - chh, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }
            else if (count < 4)
            {
                SmallImage.drawSmallImage(g, 1367, cx, cy - chh, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }
            else if (count < 8)
            {
                SmallImage.drawSmallImage(g, 1368, cx, cy - chh, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }
            else if (GameCanvas.gameTick % 2 == 0)
            {
                SmallImage.drawSmallImage(g, 1369, cx, cy - chh, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }
            else
            {
                SmallImage.drawSmallImage(g, 1370, cx, cy - chh, 0, mGraphics.VCENTER | mGraphics.HCENTER);
            }
        }

        if (mobMe != null)
        {
            mobMe.paint(g);
        }

        if (mobVithu != null)
        {
            mobVithu.paint(g);
        }

        if (statusMe != 1 && statusMe != 6)
        {
            for (int j = 0; j < vDomsang.size(); j++)
            {
                Domsang domsang = (Domsang)vDomsang.elementAt(j);
                domsang.paint(g);
            }
        }

        paintTopDataEff(g, cx, cy, -heightCharName);
    }

    private void paintArrowAttack(mGraphics g)
    {
        if (arr != null)
        {
            arr.paint(g);
        }
    }

    public void paintHp(mGraphics g, int px, int py)
    {
        int num = cHP * 26 / cMaxHP;
        if (statusMe != 5 && statusMe != 14 && num < 2)
        {
            num = 2;
        }
        else if (statusMe == 5 || statusMe == 14)
        {
            num = 0;
        }

        if (num > 26)
        {
            num = 0;
        }

        g.setColor(16777215);
        g.fillRect(px, py, 26, 4);
        g.setColor(getClassColor());
        g.fillRect(px, py, num, 4);
        g.setColor(0);
        g.drawRect(px, py, 26, 4);
    }

    public void paintNameInSameParty(mGraphics g)
    {
        if (isPaint())
        {
            if (getMyChar().charFocus == null || !getMyChar().charFocus.Equals(this))
            {
                mFont.tahoma_7_yellow.drawString(g, cName, cx, cy - (ch + 5 + mFont.tahoma_7.getHeight()), mFont.CENTER);
            }
            else if (getMyChar().charFocus != null && getMyChar().charFocus.Equals(this))
            {
                mFont.tahoma_7_yellow.drawString(g, cName, cx, cy - ch - mFont.tahoma_7_green.getHeight() - 10, mFont.CENTER, mFont.tahoma_7_grey);
            }
        }
    }

    public void paintCharName_HP_MP_Overhead(mGraphics g)
    {
        heightCharName = ch + 5;
        heightCharName += ((isWolf || isMoto || isNewMount) ? 15 : 0);
        if (ID_HORSE > -1)
        {
            DataSkillEff partEff = getPartEff(ID_HORSE, true);
            if (partEff != null && partEff.isLoadData)
            {
                heightCharName += partEff.dyHorse;
            }
        }

        if (isInvisible && !me)
        {
            return;
        }

        bool flag = false;
        if (me)
        {
            if (GameScr.auto == 1)
            {
                if (!GameScr.gI().lockAutoMove)
                {
                    heightCharName += mFont.tahoma_7.getHeight();
                    mFont.tahoma_7_yellow.drawString(g, mResources.AUTO_FIRE, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
                    heightCharName++;
                }
                else
                {
                    heightCharName += mFont.tahoma_7.getHeight();
                    mFont.tahoma_7_yellow.drawString(g, mResources.UNMOVE, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
                    heightCharName++;
                }
            }
            else if (ColorName > 0)
            {
                flag = true;
                heightCharName += mFont.tahoma_7.getHeight();
                if (!isHuman)
                {
                    mFont.tahoma_7_blue1.drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
                }
                else
                {
                    color_font_name[ColorName].drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
                }

                heightCharName++;
            }
            else if (npcFocus == null && charFocus == null && mobFocus == null && itemFocus == null)
            {
                flag = true;
                heightCharName += mFont.tahoma_7.getHeight();
                if (!isHuman)
                {
                    mFont.tahoma_7_blue1.drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
                }
                else
                {
                    color_font_name[ColorName].drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
                }

                heightCharName++;
            }
        }
        else if (getMyChar().charFocus != null && getMyChar().charFocus.Equals(this))
        {
            flag = true;
            heightCharName += 5;
            paintHp(g, cx - 13, cy - heightCharName);
            heightCharName += mFont.tahoma_7.getHeight();
            if (!isHuman)
            {
                mFont.tahoma_7_blue1.drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
            }
            else
            {
                color_font_name[ColorName].drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
            }

            heightCharName++;
        }
        else if (paintName)
        {
            flag = true;
            heightCharName += mFont.tahoma_7.getHeight();
            if (!isHuman)
            {
                mFont.tahoma_7_blue1.drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
            }
            else
            {
                color_font_name[ColorName].drawString(g, cName, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
            }

            heightCharName++;
        }

        if (charID == -getMyChar().charID)
        {
            heightCharName += color_font_name[ColorName].getHeight();
            // Offline (user request): the escort line "do <name> hộ tống" is drawn in yellow, like the client's escort messages.
            mFont.tahoma_7_yellow.drawString(g, mResources.BY + " " + getMyChar().cName + " " + mResources.PROTECT, cx, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_grey);
            heightCharName++;
        }

        if (!cClanName.Equals(string.Empty) && flag)
        {
            heightCharName += mFont.tahoma_7.getHeight() - 1;
            int num = 0;
            if (ctypeClan > 0)
            {
                num = 5;
            }

            mFont.tahoma_7_white.drawString(g, cClanName, cx + num, cy - heightCharName, mFont.CENTER, mFont.tahoma_7_blue);
            if (ctypeClan == 3)
            {
                SmallImage.drawSmallImage(g, 1215, cx - (mFont.tahoma_7_white.getWidth(cClanName) / 2 + (7 - num)), cy - heightCharName + 1, 0, mGraphics.HCENTER | mGraphics.TOP);
            }
            else if (ctypeClan == 4)
            {
                SmallImage.drawSmallImage(g, 1216, cx - (mFont.tahoma_7_white.getWidth(cClanName) / 2 + (7 - num)), cy - heightCharName + 1, 0, mGraphics.HCENTER | mGraphics.TOP);
            }
            else if (ctypeClan == 2)
            {
                SmallImage.drawSmallImage(g, 1217, cx - (mFont.tahoma_7_white.getWidth(cClanName) / 2 + (7 - num)), cy - heightCharName + 1, 0, mGraphics.HCENTER | mGraphics.TOP);
            }

            heightCharName++;
        }

        if (resultTest > 0 && resultTest < 30)
        {
            heightCharName += SmallImage.smallImg[1117][4];
            SmallImage.drawSmallImage(g, 1117, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
            heightCharName++;
        }
        else if (resultTest > 30 && resultTest < 60)
        {
            heightCharName += SmallImage.smallImg[1117][4];
            SmallImage.drawSmallImage(g, 1126, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
            heightCharName++;
        }
        else if (resultTest > 60 && resultTest < 90)
        {
            heightCharName += SmallImage.smallImg[1117][4];
            SmallImage.drawSmallImage(g, 1118, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
            heightCharName++;
        }
        else if (charID >= 0)
        {
            if (killCharId >= 0)
            {
                heightCharName += SmallImage.smallImg[1122][4];
                SmallImage.drawSmallImage(g, 1122, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == 3)
            {
                heightCharName += SmallImage.smallImg[1121][4];
                SmallImage.drawSmallImage(g, 1121, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == 2)
            {
                heightCharName += SmallImage.smallImg[1124][4];
                SmallImage.drawSmallImage(g, 1124, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == PK_NHOM)
            {
                heightCharName += SmallImage.smallImg[1123][4];
                SmallImage.drawSmallImage(g, 1123, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == PK_PHE1)
            {
                heightCharName += SmallImage.smallImg[1240][4];
                SmallImage.drawSmallImage(g, 1240, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == PK_PHE2)
            {
                heightCharName += SmallImage.smallImg[1241][4];
                SmallImage.drawSmallImage(g, 1241, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == PK_PHE3)
            {
                heightCharName += SmallImage.smallImg[1241][4];
                SmallImage.drawSmallImage(g, 1123, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (cTypePk == PK_PHE4)
            {
                heightCharName += SmallImage.smallImg[1241][4];
                SmallImage.drawSmallImage(g, 3410, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
            else if (testCharId > 0)
            {
                heightCharName += SmallImage.smallImg[1116][4];
                SmallImage.drawSmallImage(g, 1116, cx, cy - heightCharName, 0, mGraphics.HCENTER | mGraphics.TOP);
                heightCharName++;
            }
        }
    }

    public void paintHorseUI(mGraphics g, int x, int y)
    {
        DataSkillEff partEff = getPartEff(ID_HORSE, true);
        if (partEff == null || !partEff.isLoadData)
        {
            return;
        }

        if (GameCanvas.gameTick % 10 == 0)
        {
            int num = partEff.listFrame.size() / partEff.sequence.Length;
            if (num == 0)
            {
                num = 1;
            }

            fHorseUI = (byte)((fHorseUI + 1) % num);
        }

        partEff.paintBottomEff_new(g, x, y, getFrameHorseui(partEff.ActionStand[fhorsetrangbi]), 2);
        partEff.paintTopEff_new(g, x, y, getFrameHorseui(partEff.ActionStand[fhorsetrangbi]), 2);
    }

    public void paintMatna(mGraphics g, int cx, int cy, int f)
    {
        if (ID_MAT_NA < 0)
        {
            return;
        }

        DataSkillEff partEff = getPartEff(ID_MAT_NA, false);
        int num = 0;
        if ((statusMe == 1 || statusMe == 6) && cf <= 1)
        {
            num = cf * f;
        }

        if (partEff == null)
        {
            return;
        }

        if (isWolf && (arrItemMounts[4].template.id == 443 || arrItemMounts[4].template.id == 523))
        {
            int num2 = -2;
            if (statusMe == 1 || statusMe == 6)
            {
                num2 = 0;
            }

            int num3 = 0;
            if (cgender == 1)
            {
                num3 = 3;
            }

            if (statusMe == 1 || statusMe == 6)
            {
                num3 = 0;
            }

            partEff.paintBottomEff_new(g, cx + num2, cy + num - getys() - num3, (statusMe != 1 && statusMe != 6) ? (5 + FrameMatna * 30) : getFrameMatna(), (cdir != 1) ? 2 : 0);
            partEff.paintTopEff_new(g, cx + num2, cy + num - getys() - num3, (statusMe != 1 && statusMe != 6) ? (5 + FrameMatna * 30) : getFrameMatna(), (cdir != 1) ? 2 : 0);
        }
        else
        {
            partEff.paintBottomEff_new(g, cx + getxs(), cy + num - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameMatna * 30) : getFrameMatna(), (cdir != 1) ? 2 : 0);
            partEff.paintTopEff_new(g, cx + getxs(), cy + num - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameMatna * 30) : getFrameMatna(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintLeg(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_LEG, false);
        int num = 0;
        if ((statusMe == 1 || statusMe == 6) && cf <= 1)
        {
            num = cf * f;
        }

        if (partEff != null)
        {
            partEff.paintBottomEff_new(g, cx, cy + num, (statusMe != 1 && statusMe != 6) ? (5 + FrameLeg * 30) : getFrameLeg(), (cdir != 1) ? 2 : 0);
            partEff.paintTopEff_new(g, cx, cy + num, (statusMe != 1 && statusMe != 6) ? (5 + FrameLeg * 30) : getFrameLeg(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintPPUIBot(mGraphics g, int cx, int cy, int f)
    {
        getPartEff(ID_PP, false)?.paintBottomEff_new(g, cx, cy, getFramePP(f), 0);
    }

    public void paintPPUITop(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_PP, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 10 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            fPPUI = (byte)((fPPUI + 1) % num);
        }

        partEff.paintTopEff_new(g, cx, cy, getFramePP(f), 0);
    }

    public void paintMatNaUI(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_MAT_NA, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            fMatNaUI = (byte)((fMatNaUI + 1) % num);
        }

        partEff.paintTopEff_new(g, cx, cy, getFrameMatNaUI(f), 0);
        partEff.paintBottomEff_new(g, cx, cy, getFrameMatNaUI(f), 0);
    }

    public void paintWeaponUITop(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_WEA_PONE, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            fWeaponUI = (byte)((fWeaponUI + 1) % num);
        }

        partEff.paintTopEff_new(g, cx, cy, getFrameWeaponUI(f), 0);
    }

    public void paintWeaponUIBot(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_WEA_PONE, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            fWeaponUI = (byte)((fWeaponUI + 1) % num);
        }

        partEff.paintBottomEff_new(g, cx, cy, getFrameWeaponUI(f), 0);
    }

    public void paintHeadUI(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_HAIR, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            fHeadUI = (byte)((fHeadUI + 1) % num);
        }

        partEff.paintBottomEff_new(g, cx, cy, getFrameHeadUI(f), 0);
        partEff.paintTopEff_new(g, cx, cy, getFrameHeadUI(f), 0);
    }

    public void paintLegUI(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_LEG, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            FlegUI = (byte)((FlegUI + 1) % num);
        }

        partEff.paintBottomEff_new(g, cx, cy, getFrameLegUI(f), 0);
        partEff.paintTopEff_new(g, cx, cy, getFrameLegUI(f), 0);
    }

    public void paintBodyUI(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_Body, false);
        if (partEff == null)
        {
            return;
        }

        if (GameCanvas.gameTick % 6 == 0)
        {
            int num = partEff.listFrame.size() / 30;
            if (num == 0)
            {
                num = 1;
            }

            FbodyUI = (byte)((FbodyUI + 1) % num);
        }

        partEff.paintBottomEff_new(g, cx, cy, getFrameBodyUI(f), 0);
        partEff.paintTopEff_new(g, cx, cy, getFrameBodyUI(f), 0);
    }

    public void paintMatNaBot(mGraphics g, int cx, int cy, int f, DataSkillEff partMatNa)
    {
    }

    public void paintMatNaTop(mGraphics g, int cx, int cy, int f, DataSkillEff partMatNa)
    {
    }

    public void paintWeaponBot(mGraphics g, int cx, int cy, int f, DataSkillEff partWeapon)
    {
        int num = 0;
        if ((statusMe == 1 || statusMe == 6) && cf <= 1)
        {
            num = cf * f;
        }

        if (partWeapon == null)
        {
            return;
        }

        if (isNewMount)
        {
            int num2 = -2;
            if (cdir == 1)
            {
                num2 = 2;
            }

            if ((statusMe == 1 || statusMe == 6) && cf <= 1)
            {
                num = cf;
            }

            partWeapon.paintBottomEff_new(g, cx + getxs() + num2, cy + num - getDyHorse() + dyMount - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameWeaPone * 30) : getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else if (isWolf)
        {
            partWeapon.paintBottomEff_new(g, cx + getxs(), cy + num - getDyHorse() - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameWeaPone * 30) : getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else if (isMoto)
        {
            partWeapon.paintBottomEff_new(g, cx + getxs(), cy + num - getDyHorse() - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameWeaPone * 30) : getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else if (ID_HORSE > -1)
        {
            partWeapon.paintBottomEff_new(g, cx + getxs() + dxnewhorse, cy - getys() - getDyHorse(), getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else
        {
            partWeapon.paintBottomEff_new(g, cx + getxs(), cy - getys(), getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintWeaponTop(mGraphics g, int cx, int cy, int f, DataSkillEff partWeapon)
    {
        int num = 0;
        if ((statusMe == 1 || statusMe == 6) && cf <= 1)
        {
            num = cf * f;
        }

        if (partWeapon == null)
        {
            return;
        }

        if (isNewMount)
        {
            int num2 = -2;
            if (cdir == 1)
            {
                num2 = 2;
            }

            if ((statusMe == 1 || statusMe == 6) && cf <= 1)
            {
                num = cf;
            }

            partWeapon.paintTopEff_new(g, cx + getxs() + num2, cy + num - getDyHorse() + dyMount - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameWeaPone * 30) : getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else if (isWolf)
        {
            partWeapon.paintTopEff_new(g, cx + getxs(), cy + num - getDyHorse() - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameWeaPone * 30) : getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else if (isMoto)
        {
            partWeapon.paintTopEff_new(g, cx + getxs(), cy + num - getDyHorse() - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameWeaPone * 30) : getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else if (ID_HORSE > -1)
        {
            partWeapon.paintTopEff_new(g, cx + getxs() + dxnewhorse, cy - getys() - getDyHorse(), getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
        else
        {
            partWeapon.paintTopEff_new(g, cx + getxs(), cy - getys(), getFrameWeapone(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintBody(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_Body, false);
        int num = 0;
        if ((statusMe == 1 || statusMe == 6) && cf <= 1)
        {
            num = cf * f;
        }

        if (partEff != null)
        {
            partEff.paintBottomEff_new(g, cx + getxs(), cy + num - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameBody * 30) : getFrameBody(), (cdir != 1) ? 2 : 0);
            partEff.paintTopEff_new(g, cx + getxs(), cy + num - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameBody * 30) : getFrameBody(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintHair(mGraphics g, int cx, int cy, int f)
    {
        DataSkillEff partEff = getPartEff(ID_HAIR, false);
        int num = 0;
        if ((statusMe == 1 || statusMe == 6) && cf <= 1)
        {
            num = cf * f;
        }

        if (partEff != null)
        {
            partEff.paintBottomEff_new(g, cx + getxs(), cy + num - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameHair * 30) : getFrameHair(), (cdir != 1) ? 2 : 0);
            partEff.paintTopEff_new(g, cx + getxs(), cy + num - getys(), (statusMe != 1 && statusMe != 6) ? (5 + FrameHair * 30) : getFrameHair(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintPP_Bot(mGraphics g)
    {
        DataSkillEff partEff = getPartEff(ID_PP, false);
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        if (isMoto)
        {
            if (arrItemMounts[4].template.id == 485)
            {
                num = 9;
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                partEff?.paintBottomEff_new(g, cx + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }

            if (arrItemMounts[4].template.id == 524)
            {
                num = 12;
                num2 = 2;
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                if (cdir == 1)
                {
                    num2 = -2;
                }

                partEff?.paintBottomEff_new(g, cx + num2 + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }
        }
        else if (isWolf)
        {
            if (arrItemMounts[4].template.id == 523)
            {
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                num = 15;
                num2 = 2;
                if (cdir == 1)
                {
                    num2 = -2;
                }

                partEff?.paintBottomEff_new(g, cx + num2 + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }
            else if (arrItemMounts[4].template.id == 443)
            {
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                num = 15;
                num2 = 2;
                if (cdir == 1)
                {
                    num2 = -2;
                }

                partEff?.paintBottomEff_new(g, cx + num2 + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }
        }
        else if (isNewMount)
        {
            num = 18;
            num2 = -4;
            if ((statusMe == 1 || statusMe == 6) && cf <= 1)
            {
                num3 = cf;
            }

            if (cdir == 1)
            {
                num2 = 4;
            }

            partEff?.paintBottomEff_new(g, cx + num2 + getxs(), cy - num + dyMount + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
        }
        else
        {
            partEff?.paintBottomEff_new(g, cx + dxnewhorse + getxs(), cy - dynewhhorse - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
        }
    }

    public void paintPP_Top(mGraphics g)
    {
        DataSkillEff partEff = getPartEff(ID_PP, false);
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        if (isMoto)
        {
            if (arrItemMounts[4].template.id == 485)
            {
                num = 9;
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                partEff?.paintTopEff_new(g, cx + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }

            if (arrItemMounts[4].template.id == 524)
            {
                num = 12;
                num2 = 2;
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                if (cdir == 1)
                {
                    num2 = -2;
                }

                partEff?.paintTopEff_new(g, cx + num2 + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }
        }
        else if (isWolf)
        {
            if (arrItemMounts[4].template.id == 523)
            {
                num = 15;
                num2 = 2;
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                if (cdir == 1)
                {
                    num2 = -2;
                }

                partEff?.paintTopEff_new(g, cx + num2 + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }
            else if (arrItemMounts[4].template.id == 443)
            {
                if ((statusMe == 1 || statusMe == 6) && cf <= 1)
                {
                    num3 = cf * 2;
                }

                num = 15;
                num2 = 2;
                if (cdir == 1)
                {
                    num2 = -2;
                }

                partEff?.paintTopEff_new(g, cx + num2 + getxs(), cy - num + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
            }
        }
        else if (isNewMount)
        {
            num = 18;
            num2 = -4;
            if ((statusMe == 1 || statusMe == 6) && cf <= 1)
            {
                num3 = cf;
            }

            if (cdir == 1)
            {
                num2 = 4;
            }

            partEff?.paintTopEff_new(g, cx + num2 + getxs(), cy - num + dyMount + num3 - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
        }
        else
        {
            partEff?.paintTopEff_new(g, cx + dxnewhorse + getxs(), cy - dynewhhorse - getys(), getFramePP(), (cdir != 1) ? 2 : 0);
        }
    }

    private void paintCharWithoutSkill(mGraphics g)
    {
        try
        {
            DataSkillEff partEff = getPartEff(ID_WEA_PONE, false);
            if (partEff != null && GameCanvas.gameTick % 3 == 0)
            {
                int num = partEff.listFrame.size() / 30;
                if (num == 0)
                {
                    num = 1;
                }

                FrameWeaPone = (byte)((FrameWeaPone + 1) % num);
            }

            DataSkillEff partEff2 = getPartEff(ID_Bien_Hinh, false);
            if (partEff2 != null && GameCanvas.gameTick % 6 == 0)
            {
                int num2 = partEff2.listFrame.size() / 30;
                if (num2 == 0)
                {
                    num2 = 1;
                }

                FrameBienHinh = (byte)((FrameBienHinh + 1) % num2);
            }

            DataSkillEff partEff3 = getPartEff(ID_MAT_NA, false);
            if (partEff3 != null && GameCanvas.gameTick % 6 == 0)
            {
                int num3 = partEff3.listFrame.size() / 30;
                if (num3 == 0)
                {
                    num3 = 1;
                }

                FrameMatna = (byte)((FrameMatna + 1) % num3);
            }

            DataSkillEff partEff4 = getPartEff(ID_SUSANO, false);
            DataSkillEff partEff5 = getPartEff(ID_RANK, false);
            DataSkillEff partEff6 = getPartEff(ID_NAME, false);
            DataSkillEff partEff7 = getPartEff(ID_LEG, false);
            if (partEff7 != null && GameCanvas.gameTick % 6 == 0)
            {
                int num4 = partEff7.listFrame.size() / 30;
                if (num4 == 0)
                {
                    num4 = 1;
                }

                FrameLeg = (byte)((FrameLeg + 1) % num4);
            }

            DataSkillEff partEff8 = getPartEff(ID_HORSE, true);
            if (partEff8 != null && partEff8.isLoadData && GameCanvas.gameTick % 10 == 0)
            {
                int num5 = partEff8.listFrame.size() / partEff8.sequence.Length;
                if (num5 == 0)
                {
                    num5 = 1;
                }

                FrameHorse = (byte)((FrameHorse + 1) % num5);
            }

            DataSkillEff partEff9 = getPartEff(ID_Body, false);
            if (partEff9 != null && GameCanvas.gameTick % 6 == 0)
            {
                int num6 = partEff9.listFrame.size() / 30;
                if (num6 == 0)
                {
                    num6 = 1;
                }

                FrameBody = (byte)((FrameBody + 1) % num6);
            }

            DataSkillEff partEff10 = getPartEff(ID_HAIR, false);
            if (partEff10 != null && GameCanvas.gameTick % 6 == 0)
            {
                int num7 = partEff10.listFrame.size() / 30;
                if (num7 == 0)
                {
                    num7 = 1;
                }

                FrameHair = (byte)((FrameHair + 1) % num7);
            }

            DataSkillEff partEff11 = getPartEff(ID_PP, false);
            if (partEff11 != null && GameCanvas.gameTick % 6 == 0)
            {
                int num8 = partEff11.listFrame.size() / 30;
                if (num8 == 0)
                {
                    num8 = 1;
                }

                FramePP = (byte)((FramePP + 1) % num8);
            }

            int[] array = null;
            Part part = GameScr.parts[head];
            Part part2 = GameScr.parts[leg];
            Part part3 = GameScr.parts[body];
            Part part4 = GameScr.parts[wp];
            if (partEff != null)
            {
                part4 = null;
            }

            if (arrItemBody != null && arrItemBody[11] != null)
            {
                part = GameScr.parts[arrItemBody[11].template.part];
                head = arrItemBody[11].template.part;
            }

            if (part.pi == null || part.pi.Length < 8)
            {
                part = getDefaultHead(cgender);
            }
            else
            {
                for (int i = 0; i < part.pi.Length; i++)
                {
                    if (part.pi[i] == null || !SmallImage.isExitsImage(part.pi[i].id))
                    {
                        part = getDefaultHead(cgender);
                        break;
                    }
                }
            }

            array = ((part.pi[CharInfo[cf][0][0]].id > 4) ? getClassCoat() : null);
            if ((((statusMe == 1 || statusMe == 6) && GameCanvas.gameTick % 10 == 0) || ((statusMe == 2 || statusMe == 10) && GameCanvas.gameTick % 2 == 0) || ((statusMe == 4 || statusMe == 3) && GameCanvas.gameTick % 3 == 0)) && array != null)
            {
                tickCoat++;
                if (tickCoat > array.Length - 1)
                {
                    tickCoat = 0;
                }
            }

            if (statusMe == 14)
            {
                if (isHaveNewMount())
                {
                    paintBehindNewMount(g);
                }
                else if (isHaveMoto())
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        if (arrItemMounts[4].sys < 2)
                        {
                            SmallImage.drawSmallImage(g, 1800, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                        }
                        else
                        {
                            SmallImage.drawSmallImage(g, 2063, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                        }
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        if (arrItemMounts[4].sys < 2)
                        {
                            SmallImage.drawSmallImage(g, 2064, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                        }
                        else
                        {
                            SmallImage.drawSmallImage(g, 2068, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                        }
                    }
                }

                SmallImage.drawSmallImage(g, 1040, cx, cy, 0, mGraphics.HCENTER | mGraphics.BOTTOM);
            }
            else if (isInvisible)
            {
                if (me)
                {
                    if (GameCanvas.gameTick % 50 == 48 || GameCanvas.gameTick % 50 == 90)
                    {
                        SmallImage.drawSmallImage(g, 1196, cx, cy - 18, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, 1195, cx, cy - 18, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    }
                }
            }
            else
            {
                partEff4?.paintBottomEff_new(g, cx, cy - getDyHorse(), FrameSusan, (cdir != 1) ? 2 : 0);
                if (partEff != null)
                {
                    paintWeaponBot(g, cx, cy, 2, partEff);
                }

                if (partEff2 != null)
                {
                    partEff2.paintBottomEff_new(g, cx, cy, getFrameBienHinh(), (cdir != 1) ? 2 : 0);
                    partEff2.paintTopEff_new(g, cx, cy, getFrameBienHinh(), (cdir != 1) ? 2 : 0);
                }

                partEff8?.paintBottomEff_new(g, cx, cy, getFrameHores(), (cdir == 1) ? 2 : 0);
                partEff5?.paintBottomEff_new(g, cx, cy - getDyHorse(), FrameRank, (cdir != 1) ? 2 : 0);
                partEff6?.paintBottomEff_new(g, cx, cy - getDyHorse(), FrameName, 0);
                paintPP_Bot(g);
                if (isMoto)
                {
                    int num9 = 0;
                    int num10 = 0;
                    int num11 = 0;
                    int num12 = 0;
                    int[] array2 = geteffOngbo();
                    if (array2 != null)
                    {
                        tickEffmoto++;
                        if (tickEffmoto >= array2.Length)
                        {
                            tickEffmoto = 0;
                        }
                    }

                    if (arrItemMounts[4].template.id == 485)
                    {
                        if (arrItemMounts[4].sys < 2)
                        {
                            if (statusMe == 1 || statusMe == 6)
                            {
                                num9 = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
                                if ((partEff11 != null || partEff9 != null || partEff10 != null || partEff != null || partEff3 != null) && cf <= 1)
                                {
                                    num9 = cf;
                                }
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                num9 = ((GameCanvas.gameTick % 6 > 3) ? 1 : 0);
                            }

                            if (statusMe == 3)
                            {
                                num11 = -5 * cdir;
                            }

                            if (cdir == 1)
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 1711, cx, cy + 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 1710 : 1709, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                            else
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 1711, cx, cy + 2, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 1710 : 1709, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                        }
                        else
                        {
                            if (statusMe == 1 || statusMe == 6)
                            {
                                num9 = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
                                if ((partEff11 != null || partEff9 != null || partEff10 != null || partEff != null || partEff3 != null) && cf <= 1)
                                {
                                    num9 = cf;
                                }
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                num9 = ((GameCanvas.gameTick % 6 > 3) ? 1 : 0);
                            }

                            if (statusMe == 3)
                            {
                                num11 = -5 * cdir;
                            }

                            if (cdir == 1)
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2057, cx, cy + 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (!isBocdau)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2055 : 2056, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, 2057, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (statusMe == 2 && array2 != null)
                                {
                                    SmallImage.drawSmallImage(g, array2[tickEffmoto], cx - 25, cy - 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (!isBocdau)
                                {
                                    if (partEff10 != null)
                                    {
                                        paintHair(g, cx, cy - 8, 2);
                                    }
                                    else
                                    {
                                        SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                    }

                                    if (partEff9 != null)
                                    {
                                        paintBody(g, cx, cy - 8, 2);
                                    }
                                    else
                                    {
                                        SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    }

                                    paintMatna(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    if (partEff10 != null)
                                    {
                                        paintHair(g, cx, cy - 8, 2);
                                    }
                                    else
                                    {
                                        SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir - 3, cy - 1 - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                    }

                                    if (partEff9 != null)
                                    {
                                        paintBody(g, cx, cy - 8, 2);
                                    }
                                    else
                                    {
                                        SmallImage.drawSmallImage(g, getBodyPaintId(), cx - 3 + dxBody * cdir, cy - 1 - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    }

                                    paintMatna(g, cx, cy - 8, 2);
                                }
                            }
                            else
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2057, cx, cy + 2, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (!isBocdau)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2055 : 2056, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, 2057, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (statusMe == 2 && array2 != null)
                                {
                                    SmallImage.drawSmallImage(g, array2[tickEffmoto], cx + 25, cy - 2, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                        }
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        if (arrItemMounts[4].sys < 2)
                        {
                            if (statusMe == 1 || statusMe == 6)
                            {
                                num9 = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
                                if ((partEff11 != null || partEff9 != null || partEff10 != null || partEff != null || partEff3 != null) && cf <= 1)
                                {
                                    num9 = cf;
                                }
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                num9 = ((GameCanvas.gameTick % 6 > 3) ? 1 : 0);
                            }

                            if (statusMe == 3)
                            {
                                num11 = -5 * cdir;
                            }

                            if (cdir == 1)
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2066, cx, cy + 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2065 : 2064, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                            else
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2066, cx, cy + 2, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2065 : 2064, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                        }
                        else if (arrItemMounts[4].sys >= 2 && arrItemMounts[4].sys < 4)
                        {
                            int[] effmoto = getEffmoto();
                            if (effmoto != null)
                            {
                                tickEffmoto++;
                                if (tickEffmoto >= effmoto.Length)
                                {
                                    tickEffmoto = 0;
                                }
                            }

                            if (statusMe == 1 || statusMe == 6)
                            {
                                num9 = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
                                if ((partEff11 != null || partEff9 != null || partEff10 != null || partEff != null || partEff3 != null) && cf <= 1)
                                {
                                    num9 = cf;
                                }
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                num9 = ((GameCanvas.gameTick % 6 > 3) ? 1 : 0);
                            }

                            if (statusMe == 3)
                            {
                                num11 = -5 * cdir;
                            }

                            if (cdir == 1)
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2070, cx, cy + 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 4)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 2 || statusMe == 10)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 1 || statusMe == 6)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                            else
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2070, cx, cy + 2, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (statusMe == 4)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 2 || statusMe == 10)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 1 || statusMe == 6)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                        }
                        else
                        {
                            int[] effmoto2 = getEffmoto();
                            if (effmoto2 != null)
                            {
                                tickEffmoto++;
                                if (tickEffmoto >= effmoto2.Length)
                                {
                                    tickEffmoto = 0;
                                }
                            }

                            if (statusMe == 1 || statusMe == 6)
                            {
                                num9 = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
                                if ((partEff11 != null || partEff9 != null || partEff10 != null || partEff != null || partEff3 != null) && cf <= 1)
                                {
                                    num9 = cf;
                                }
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                num9 = ((GameCanvas.gameTick % 6 > 3) ? 1 : 0);
                            }

                            if (statusMe == 3)
                            {
                                num11 = -5 * cdir;
                            }

                            if (cdir == 1)
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2070, cx, cy + 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 13, cy - 17, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 24, cy + 2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 4)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 15, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 27, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 2 || statusMe == 10)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 15, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 27, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 1 || statusMe == 6)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 15, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 27, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                            else
                            {
                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2070, cx, cy + 2, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 12, cy - 17, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 25, cy + 3, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (statusMe == 4)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 15, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 27, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 2 || statusMe == 10)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 15, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 27, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else if (statusMe == 1 || statusMe == 6)
                                {
                                    SmallImage.drawSmallImage(g, (num9 != 0) ? 2069 : 2068, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx - 15, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    SmallImage.drawSmallImage(g, effmoto2[tickEffmoto], cx + 27, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + num11 + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 + num9 + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx, cy - 8, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 8 + num9 + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx, cy - 8, 2);
                            }
                        }
                    }
                }
                else if (isWolf)
                {
                    int[] effwolf = getEffwolf();
                    if (effwolf != null)
                    {
                        tickEffFireW++;
                        if (tickEffFireW >= effwolf.Length)
                        {
                            tickEffFireW = 0;
                        }
                    }

                    if (statusMe == 1 || statusMe == 6)
                    {
                        tickWolf = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
                        if ((partEff11 != null || partEff9 != null || partEff10 != null || partEff != null || partEff3 != null) && cf <= 1)
                        {
                            tickWolf = cf;
                        }

                        dy = -tickWolf;
                    }
                    else if (statusMe == 2 || statusMe == 10)
                    {
                        if (GameCanvas.gameTick % 12 <= 3)
                        {
                            tickWolf = 0;
                        }
                        else if (GameCanvas.gameTick % 12 <= 6)
                        {
                            tickWolf = 1;
                            dy = 2;
                        }
                        else if (GameCanvas.gameTick % 12 <= 9)
                        {
                            tickWolf = 2;
                            dy = 4;
                        }
                        else
                        {
                            tickWolf = 3;
                            dy = 2;
                        }
                    }

                    int[] array3 = new int[4]
                    {
                        2050,
                        2053,
                        2049,
                        2052
                    };
                    int[] array4 = new int[4]
                    {
                        2075,
                        2078,
                        2074,
                        2079
                    };
                    int[] array5 = new int[4]
                    {
                        cy - 22,
                        cy - 23,
                        cy - 22,
                        cy - 23
                    };
                    int[] array6 = new int[4]
                    {
                        cy - 22,
                        cy - 23,
                        cy - 22,
                        cy - 22
                    };
                    if (statusMe == 3)
                    {
                        hdx = -5 * cdir;
                    }
                    else
                    {
                        hdx = -3 * cdir;
                    }

                    if (arrItemMounts[4].template.id == 523)
                    {
                        if (cdir == 1)
                        {
                            if (part4 != null)
                            {
                                SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx + CharInfo[cf][3][1] + part4.pi[CharInfo[cf][3][0]].dx, cy - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy - 10, 0, 0);
                            }

                            if (statusMe == 3)
                            {
                                SmallImage.drawSmallImage(g, 2051, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 4)
                            {
                                SmallImage.drawSmallImage(g, 2052, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 1 || statusMe == 6)
                            {
                                SmallImage.drawSmallImage(g, (tickWolf != 0) ? 2048 : 2047, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                SmallImage.drawSmallImage(g, array3[tickWolf], cx, cy - dy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            int num13 = 15;
                            int num14 = 2;
                            if (cdir == 1)
                            {
                                num14 = -2;
                            }

                            if (partEff10 != null)
                            {
                                paintHair(g, cx + num14, cy - num13, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 - hdy - dy + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                            }

                            if (partEff9 != null)
                            {
                                paintBody(g, cx + num14, cy - num13, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 - hdy - dy + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            paintMatna(g, cx + num14, cy - num13, 2);
                        }
                        else
                        {
                            if (part4 != null)
                            {
                                SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx - CharInfo[cf][3][1] - part4.pi[CharInfo[cf][3][0]].dx, cy - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy - 10, 2, 24);
                            }

                            if (statusMe == 3)
                            {
                                SmallImage.drawSmallImage(g, 2051, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 4)
                            {
                                SmallImage.drawSmallImage(g, 2052, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 1 || statusMe == 6)
                            {
                                SmallImage.drawSmallImage(g, (tickWolf != 0) ? 2048 : 2047, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                SmallImage.drawSmallImage(g, array3[tickWolf], cx, cy - dy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            int num15 = 15;
                            int num16 = 2;
                            if (cdir == 1)
                            {
                                num16 = -2;
                            }

                            if (partEff10 != null)
                            {
                                paintHair(g, cx + num16, cy - num15, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 - hdy - dy + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                            }

                            if (partEff9 != null)
                            {
                                paintBody(g, cx + num16, cy - num15, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 - hdy - dy + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            paintMatna(g, cx + num16, cy - num15, 2);
                        }
                    }
                    else if (arrItemMounts[4].template.id == 443)
                    {
                        if (arrItemMounts[4].sys >= 3)
                        {
                            if (cdir == 1)
                            {
                                if (part4 != null)
                                {
                                    SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx + CharInfo[cf][3][1] + part4.pi[CharInfo[cf][3][0]].dx, cy - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy - 10, 0, 0);
                                }

                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2077, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx + 21, cy - 30, 0);
                                }
                                else if (statusMe == 4)
                                {
                                    SmallImage.drawSmallImage(g, 2076, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx + 21, cy - 19, 0);
                                }
                                else if (statusMe == 1 || statusMe == 6)
                                {
                                    SmallImage.drawSmallImage(g, (tickWolf != 0) ? 2072 : 2073, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx + 21, cy - 19, 0);
                                }
                                else if (statusMe == 2 || statusMe == 10)
                                {
                                    SmallImage.drawSmallImage(g, array4[tickWolf], cx, cy - dy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx + 21, array5[tickWolf], 0);
                                }

                                int num17 = 15;
                                int num18 = 2;
                                if (cdir == 1)
                                {
                                    num18 = -2;
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx + num18, cy - num17, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 - hdy - dy + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx + num18, cy - num17, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 - hdy - dy + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx + num18, cy - num17, 2);
                            }
                            else
                            {
                                if (part4 != null)
                                {
                                    SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx - CharInfo[cf][3][1] - part4.pi[CharInfo[cf][3][0]].dx, cy - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy - 10, 2, 24);
                                }

                                if (statusMe == 3)
                                {
                                    SmallImage.drawSmallImage(g, 2077, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx - 23, cy - 30, 0);
                                }
                                else if (statusMe == 4)
                                {
                                    SmallImage.drawSmallImage(g, 2076, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx - 24, cy - 20, 0);
                                }
                                else if (statusMe == 1 || statusMe == 6)
                                {
                                    SmallImage.drawSmallImage(g, (tickWolf != 0) ? 2072 : 2073, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx - 24, cy - 20, 0);
                                }
                                else if (statusMe == 2 || statusMe == 10)
                                {
                                    SmallImage.drawSmallImage(g, array4[tickWolf], cx, cy - dy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                    g.drawRegion(GameScr.imgMatcho, 0, tickEffWolf * 3, 3, 3, 0, cx - 24, array6[tickWolf], 0);
                                }

                                int num19 = 15;
                                int num20 = 2;
                                if (cdir == 1)
                                {
                                    num20 = -2;
                                }

                                if (partEff10 != null)
                                {
                                    paintHair(g, cx + num20, cy - num19, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 - hdy - dy + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                                }

                                if (partEff9 != null)
                                {
                                    paintBody(g, cx + num20, cy - num19, 2);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 - hdy - dy + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }

                                paintMatna(g, cx + num20, cy - num19, 2);
                            }
                        }
                        else if (cdir == 1)
                        {
                            if (part4 != null)
                            {
                                SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx + CharInfo[cf][3][1] + part4.pi[CharInfo[cf][3][0]].dx, cy - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy - 10, 0, 0);
                            }

                            if (statusMe == 3)
                            {
                                SmallImage.drawSmallImage(g, 1716, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 4)
                            {
                                SmallImage.drawSmallImage(g, 1717, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 1 || statusMe == 6)
                            {
                                SmallImage.drawSmallImage(g, (tickWolf != 0) ? 1713 : 1712, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                SmallImage.drawSmallImage(g, idWolfW[tickWolf], cx, cy - dy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            int num21 = 14;
                            int num22 = 2;
                            if (cdir == 1)
                            {
                                num22 = -2;
                            }

                            if (partEff10 != null)
                            {
                                paintHair(g, cx + num22, cy - num21, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 - hdy - dy + dyHead, 0, mGraphics.TOP | mGraphics.HCENTER);
                            }

                            if (partEff9 != null)
                            {
                                paintBody(g, cx + num22, cy - num21, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 - hdy - dy + dyBody, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            paintMatna(g, cx + num22, cy - num21, 2);
                        }
                        else
                        {
                            if (part4 != null)
                            {
                                SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx - CharInfo[cf][3][1] - part4.pi[CharInfo[cf][3][0]].dx, cy - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy - 10, 2, 24);
                            }

                            if (statusMe == 3)
                            {
                                SmallImage.drawSmallImage(g, 1716, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 4)
                            {
                                SmallImage.drawSmallImage(g, 1717, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 1 || statusMe == 6)
                            {
                                SmallImage.drawSmallImage(g, (tickWolf != 0) ? 1713 : 1712, cx, cy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }
                            else if (statusMe == 2 || statusMe == 10)
                            {
                                SmallImage.drawSmallImage(g, idWolfW[tickWolf], cx, cy - dy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            int num23 = 14;
                            int num24 = 2;
                            if (cdir == 1)
                            {
                                num24 = -2;
                            }

                            if (partEff10 != null)
                            {
                                paintHair(g, cx - num24, cy - num23, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + part.pi[CharInfo[0][0][0]].dy - 12 - hdy - dy + dyHead, 2, mGraphics.TOP | mGraphics.HCENTER);
                            }

                            if (partEff9 != null)
                            {
                                paintBody(g, cx + num24, cy - num23, 2);
                            }
                            else
                            {
                                SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 - hdy - dy + dyBody, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                            }

                            paintMatna(g, cx + num24, cy - num23, 2);
                        }
                    }
                }
                else
                {
                    int transform = 2;
                    int anchor = 24;
                    if (cdir == 1)
                    {
                        transform = 0;
                        anchor = 0;
                    }

                    if (isNewMount)
                    {
                        paintNewMount(g, part4, part, array, partEff3);
                        partEff4?.paintTopEff_new(g, cx, cy - getDyHorse(), FrameSusan, (cdir != 1) ? 2 : 0);
                        paintPP_Top(g);
                        partEff6?.paintTopEff_new(g, cx, cy - getDyHorse(), FrameName, 0);
                        partEff5?.paintTopEff_new(g, cx, cy - getDyHorse(), FrameRank, (cdir != 1) ? 2 : 0);
                        if (partEff != null)
                        {
                            paintWeaponTop(g, cx, cy, 2, partEff);
                        }

                        return;
                    }

                    if (isMotoBehind)
                    {
                        if (isHaveNewMount())
                        {
                            paintBehindNewMount(g);
                        }
                        else if (!isMoto && !isWolf)
                        {
                            if (arrItemMounts[4].template.id == 485)
                            {
                                if (arrItemMounts[4].sys <= 1)
                                {
                                    SmallImage.drawSmallImage(g, 1800, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, 2063, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                            }
                            else if (arrItemMounts[4].template.id == 524)
                            {
                                if (arrItemMounts[4].sys <= 1)
                                {
                                    SmallImage.drawSmallImage(g, 2067, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                                else
                                {
                                    SmallImage.drawSmallImage(g, 2071, lcx, lcy, 2, mGraphics.BOTTOM | mGraphics.HCENTER);
                                }
                            }
                        }
                    }

                    if (array != null && partEff11 == null)
                    {
                        SmallImage.drawSmallImage(g, array[tickCoat], cx + dxnewhorse - 7 * cdir, cy - dynewhhorse - 18, transform, mGraphics.TOP | mGraphics.HCENTER);
                    }

                    if (part4 != null)
                    {
                        SmallImage.drawSmallImage(g, part4.pi[CharInfo[cf][3][0]].id, cx + dxnewhorse + (CharInfo[cf][3][1] + part4.pi[CharInfo[cf][3][0]].dx) * cdir, cy - dynewhhorse - CharInfo[cf][3][2] + part4.pi[CharInfo[cf][3][0]].dy, transform, anchor);
                    }

                    if (partEff8 == null)
                    {
                        if (partEff7 != null)
                        {
                            partEff7.paintBottomEff_new(g, cx, cy, getFrameLeg(), (cdir != 1) ? 2 : 0);
                            partEff7.paintTopEff_new(g, cx, cy, getFrameLeg(), (cdir != 1) ? 2 : 0);
                        }
                        else
                        {
                            SmallImage.drawSmallImage(g, part2.pi[CharInfo[cf][1][0]].id, cx + (CharInfo[cf][1][1] + part2.pi[CharInfo[cf][1][0]].dx) * cdir, cy - CharInfo[cf][1][2] + part2.pi[CharInfo[cf][1][0]].dy, transform, anchor);
                        }
                    }

                    if (statusMe != 2)
                    {
                        paintClanEffect(g, cx + 7 * cdir, cy - 2 - getDyHorse());
                    }

                    if (statusMe != 1 && statusMe != 6)
                    {
                        if (partEff10 != null)
                        {
                            partEff10.paintBottomEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameHair(), (cdir != 1) ? 2 : 0);
                            partEff10.paintTopEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameHair(), (cdir != 1) ? 2 : 0);
                        }
                        else
                        {
                            SmallImage.drawSmallImage(g, part.pi[CharInfo[cf][0][0]].id, cx + dxnewhorse + (CharInfo[cf][0][1] + part.pi[CharInfo[cf][0][0]].dx) * cdir, cy - dynewhhorse - CharInfo[cf][0][2] + part.pi[CharInfo[cf][0][0]].dy, transform, anchor);
                        }
                    }

                    if (partEff9 != null)
                    {
                        partEff9.paintBottomEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameBody(), (cdir != 1) ? 2 : 0);
                        partEff9.paintTopEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameBody(), (cdir != 1) ? 2 : 0);
                    }
                    else
                    {
                        SmallImage.drawSmallImage(g, part3.pi[CharInfo[cf][2][0]].id, cx + dxnewhorse + (CharInfo[cf][2][1] + part3.pi[CharInfo[cf][2][0]].dx) * cdir, cy - CharInfo[cf][2][2] + part3.pi[CharInfo[cf][2][0]].dy - dynewhhorse, transform, anchor);
                    }

                    if (statusMe == 1 || statusMe == 6)
                    {
                        if (partEff10 != null)
                        {
                            partEff10.paintBottomEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameHair(), (cdir != 1) ? 2 : 0);
                            partEff10.paintTopEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameHair(), (cdir != 1) ? 2 : 0);
                        }
                        else
                        {
                            SmallImage.drawSmallImage(g, part.pi[CharInfo[cf][0][0]].id, cx + dxnewhorse + (CharInfo[cf][0][1] + part.pi[CharInfo[cf][0][0]].dx) * cdir, cy - dynewhhorse - CharInfo[cf][0][2] + part.pi[CharInfo[cf][0][0]].dy, transform, anchor);
                        }
                    }

                    if (partEff3 != null)
                    {
                        partEff3.paintBottomEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameMatna(), (cdir != 1) ? 2 : 0);
                        partEff3.paintTopEff_new(g, cx + dxnewhorse, cy - dynewhhorse, getFrameMatna(), (cdir != 1) ? 2 : 0);
                    }

                    if (statusMe == 2)
                    {
                        paintClanEffectRun(g, cx - 14 * cdir, cy - 2 - getDyHorse());
                        paintClanEffect2(g, cx + 7 * cdir, cy - 2 - getDyHorse());
                    }
                    else
                    {
                        paintClanEffect(g, cx - 7 * cdir, cy - 2 - getDyHorse());
                        paintClanEffect2(g, cx + 11 * cdir, cy - 2 - getDyHorse());
                    }
                }

                partEff8?.paintTopEff_new(g, cx, cy, getFrameHores(), (cdir == 1) ? 2 : 0);
                partEff4?.paintTopEff_new(g, cx, cy - getDyHorse(), FrameSusan, (cdir != 1) ? 2 : 0);
                paintPP_Top(g);
                partEff6?.paintTopEff_new(g, cx, cy - getDyHorse(), FrameName, 0);
                partEff5?.paintTopEff_new(g, cx, cy - getDyHorse(), FrameRank, (cdir != 1) ? 2 : 0);
                if (partEff != null)
                {
                    paintWeaponTop(g, cx, cy, 2, partEff);
                }
            }

            if (isLockAttack)
            {
                SmallImage.drawSmallImage(g, 290, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
            }
        }
        catch (Exception)
        {
        }
    }

    private int getBodyPaintId()
    {
        dxBody = 0;
        dyBody = 0;
        CharPartInfo charPartInfo = null;
        Part part = GameScr.parts[body];
        if (statusMe == 3)
        {
            charPartInfo = (CharPartInfo)CharPartInfo.body_jump.get(body + string.Empty);
            if (charPartInfo != null)
            {
                CharPartInfo charPartInfo2 = (CharPartInfo)charPartInfo.item.get(arrItemMounts[4].template.id + string.Empty);
                if (charPartInfo2 != null)
                {
                    dxBody = charPartInfo2.dx;
                    dyBody = charPartInfo2.dy;
                    return (part == null) ? charPartInfo.idSmall : part.pi[frameFix].id;
                }
            }

            switch (body)
            {
                case 1:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -6;
                    }

                    return 13;
                case 3:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -6;
                    }

                    return 45;
                case 5:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 73;
                case 7:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 101;
                case 10:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 137;
                case 18:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 365;
                case 20:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 391;
                case 22:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 417;
                case 29:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 1;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 1;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -11;
                        dyBody = -6;
                    }

                    return 472;
                case 32:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 1;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 1;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -11;
                        dyBody = -6;
                    }

                    return 506;
                case 34:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 531;
                case 36:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 559;
                case 38:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 798;
                case 42:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 970;
                case 94:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -12;
                        dyBody = -7;
                    }

                    return 1142;
                case 141:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -7;
                    }

                    return 1348;
                case 154:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -8;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -15;
                        dyBody = -3;
                    }

                    return 1487;
                case 156:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 1;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 1;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -4;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -10;
                        dyBody = -7;
                    }

                    return 1507;
                case 157:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 1812;
                case 173:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 1838;
                case 180:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 1959;
                case 183:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 4;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -7;
                    }

                    return 1987;
                case 186:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -3;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -3;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -14;
                        dyBody = -5;
                    }

                    return 2117;
                case 189:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -3;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -3;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -14;
                        dyBody = -5;
                    }

                    return 2144;
                case 206:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -5;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -10;
                        dyBody = -6;
                    }

                    return 2459;
                case 197:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -5;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -10;
                        dyBody = -6;
                    }

                    return 2342;
                case 199:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -1;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -5;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -10;
                        dyBody = -6;
                    }

                    return 2373;
                default:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -4;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -7;
                        dyBody = -6;
                    }

                    return 13;
            }
        }

        if (!isBocdau)
        {
            charPartInfo = (CharPartInfo)CharPartInfo.body_normal.get(body + string.Empty);
            if (charPartInfo != null)
            {
                CharPartInfo charPartInfo3 = (CharPartInfo)charPartInfo.item.get(arrItemMounts[4].template.id + string.Empty);
                if (charPartInfo3 != null)
                {
                    dxBody = charPartInfo3.dx;
                    dyBody = charPartInfo3.dy;
                    return (part == null) ? charPartInfo.idSmall : part.pi[frameFix].id;
                }
            }

            switch (body)
            {
                case 1:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 9;
                case 3:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -3;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -3;
                    }

                    return 41;
                case 5:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 70;
                case 7:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 97;
                case 10:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 133;
                case 18:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 369;
                case 20:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 395;
                case 22:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 421;
                case 29:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 468;
                case 32:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 502;
                case 34:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 540;
                case 36:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 555;
                case 38:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 794;
                case 42:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 966;
                case 94:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 1139;
                case 141:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -7;
                        dyBody = -1;
                    }

                    return 1343;
                case 154:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = 1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -3;
                        dyBody = -1;
                    }

                    return 1479;
                case 156:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 3;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 3;
                        dyBody = -2;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = 2;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }

                    return 1502;
                case 157:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 1808;
                case 173:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 1834;
                case 180:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 1955;
                case 183:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -1;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -6;
                        dyBody = -2;
                    }

                    return 1983;
                case 186:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -2;
                    }

                    return 2135;
                case 189:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 0;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -8;
                        dyBody = -2;
                    }

                    return 2135;
                case 206:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -4;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -9;
                        dyBody = -1;
                    }

                    return 2456;
                case 197:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -4;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -1;
                    }

                    return 2337;
                case 199:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = -3;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = -4;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -1;
                    }

                    return 2363;
                default:
                    if (arrItemMounts[4].template.id == 443)
                    {
                        dxBody = 2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 523)
                    {
                        dxBody = 2;
                        dyBody = -1;
                    }
                    else if (arrItemMounts[4].template.id == 485)
                    {
                        dxBody = 0;
                        dyBody = 0;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxBody = -5;
                        dyBody = -2;
                    }

                    return 9;
            }
        }

        charPartInfo = (CharPartInfo)CharPartInfo.body_boc_dau.get(body + string.Empty);
        if (charPartInfo != null)
        {
            CharPartInfo charPartInfo4 = (CharPartInfo)charPartInfo.item.get(arrItemMounts[4].template.id + string.Empty);
            if (charPartInfo4 != null)
            {
                dxBody = charPartInfo4.dx;
                dyBody = charPartInfo4.dy;
                return (part == null) ? charPartInfo.idSmall : part.pi[frameFix].id;
            }
        }

        switch (body)
        {
            case 1:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -3;
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -3;
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -6;
                }

                return 13;
            case 3:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -6;
                }

                return 45;
            case 5:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 73;
            case 7:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 101;
            case 10:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 137;
            case 18:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 365;
            case 20:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 391;
            case 22:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 417;
            case 29:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 1;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 1;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -11;
                    dyBody = -6;
                }

                return 472;
            case 32:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 1;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 1;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -11;
                    dyBody = -6;
                }

                return 506;
            case 34:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 531;
            case 36:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 559;
            case 38:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 798;
            case 42:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 970;
            case 94:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 0;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 0;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -12;
                    dyBody = -7;
                }

                return 1142;
            case 141:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -9;
                    dyBody = -7;
                }

                return 1348;
            case 154:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = -3;
                    dyBody = 0;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = -3;
                    dyBody = 0;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -8;
                    dyBody = 0;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -15;
                    dyBody = -3;
                }

                return 1487;
            case 156:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 1;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 1;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -4;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -10;
                    dyBody = -7;
                }

                return 1507;
            case 157:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 1812;
            case 173:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 1838;
            case 180:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 1959;
            case 183:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 4;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -8;
                    dyBody = -7;
                }

                return 1987;
            case 186:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = -3;
                    dyBody = -2;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = -3;
                    dyBody = -2;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -6;
                    dyBody = -2;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -14;
                    dyBody = -5;
                }

                return 2117;
            case 189:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = -3;
                    dyBody = -2;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = -3;
                    dyBody = -2;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -6;
                    dyBody = -2;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -14;
                    dyBody = -5;
                }

                return 2144;
            case 206:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -5;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -10;
                    dyBody = -6;
                }

                return 2459;
            case 197:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -5;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -10;
                    dyBody = -6;
                }

                return 2342;
            case 199:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = -1;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = -5;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -10;
                    dyBody = -6;
                }

                return 2373;
            default:
                if (arrItemMounts[4].template.id == 443)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 523)
                {
                    dxBody = 3;
                    dyBody = -3;
                }
                else if (arrItemMounts[4].template.id == 485)
                {
                    dxBody = 0;
                    dyBody = -4;
                }
                else if (arrItemMounts[4].template.id == 524)
                {
                    dxBody = -7;
                    dyBody = -6;
                }

                return 13;
        }
    }

    public void paintClanEffect(mGraphics g, int x, int y)
    {
        int[] clanEffect = getClanEffect();
        if (clanEffect != null)
        {
            int transform = 0;
            if (statusMe == 2)
            {
                transform = ((cdir != 1) ? 5 : 6);
            }

            int num = GameCanvas.gameTick % 13;
            if (num > 9)
            {
                SmallImage.drawSmallImage(g, clanEffect[0], x, y, transform, mGraphics.HCENTER | mGraphics.BOTTOM);
            }
            else if (num > 6)
            {
                SmallImage.drawSmallImage(g, clanEffect[1], x, y, transform, mGraphics.HCENTER | mGraphics.BOTTOM);
            }
            else if (num > 3)
            {
                SmallImage.drawSmallImage(g, clanEffect[2], x - 2, y, transform, mGraphics.HCENTER | mGraphics.BOTTOM);
            }
            else
            {
                SmallImage.drawSmallImage(g, clanEffect[3], x - 2, y, transform, mGraphics.HCENTER | mGraphics.BOTTOM);
            }
        }
    }

    private void paintClanEffectRun(mGraphics g, int x, int y)
    {
        int[] clanEffect = getClanEffect();
        if (clanEffect != null)
        {
            int transform = ((cdir != 1) ? 5 : 6);
            int anchor = ((cdir != -1) ? (mGraphics.BOTTOM | mGraphics.LEFT) : (mGraphics.BOTTOM | mGraphics.RIGHT));
            int num = GameCanvas.gameTick % 13;
            if (num > 9)
            {
                SmallImage.drawSmallImage(g, clanEffect[0], x, y, transform, anchor);
            }
            else if (num > 6)
            {
                SmallImage.drawSmallImage(g, clanEffect[1], x, y, transform, anchor);
            }
            else if (num > 3)
            {
                SmallImage.drawSmallImage(g, clanEffect[2], x, y, transform, anchor);
            }
            else
            {
                SmallImage.drawSmallImage(g, clanEffect[3], x, y, transform, anchor);
            }
        }
    }

    public void paintClanEffect2(mGraphics g, int x, int y)
    {
        int[] clanEffect = getClanEffect();
        if (clanEffect != null)
        {
            SmallImage.drawSmallImage(g, clanEffect[4], x - 2, y, 0, mGraphics.HCENTER | mGraphics.BOTTOM);
        }
    }

    private SkillPaintState SaveSkillPaint()
    {
        return new SkillPaintState
        {
            cf = cf,
            indexSkill = indexSkill,
            i0 = i0,
            i1 = i1,
            i2 = i2,
            dx0 = dx0,
            dx1 = dx1,
            dx2 = dx2,
            dy0 = dy0,
            dy1 = dy1,
            dy2 = dy2,
            eff0 = eff0,
            eff1 = eff1,
            eff2 = eff2,
            arr = arr
        };
    }

    private void RestoreSkillPaint(SkillPaintState s)
    {
        cf = s.cf;
        indexSkill = s.indexSkill;
        i0 = s.i0;
        i1 = s.i1;
        i2 = s.i2;
        dx0 = s.dx0;
        dx1 = s.dx1;
        dx2 = s.dx2;
        dy0 = s.dy0;
        dy1 = s.dy1;
        dy2 = s.dy2;
        eff0 = s.eff0;
        eff1 = s.eff1;
        eff2 = s.eff2;
        arr = s.arr;
    }

    private void paintCharWithSkill(mGraphics g)
    {
        // a skill (re)started by a message between two paints also takes a step
        if (skillPaintTick != Main.LogicTick || skillPaint != skillPaintStepped || indexSkill != skillPaintSteppedIndex)
        {
            // first paint of this tick: the original step (draw the frame, then advance)
            skillPaintTick = Main.LogicTick;
            skillPaintBefore = SaveSkillPaint();
            paintCharWithSkillStep(g, true);
            skillPaintStepped = skillPaint;
            skillPaintSteppedIndex = indexSkill;
            return;
        }

        // repaint of the same tick: draw the same frame again without advancing or launching another arrow
        SkillPaintState after = SaveSkillPaint();
        RestoreSkillPaint(skillPaintBefore);
        paintCharWithSkillStep(g, false);
        RestoreSkillPaint(after);
    }

    private void paintCharWithSkillStep(mGraphics g, bool advance)
    {
        try
        {
            SkillInfoPaint[] array = skillInfoPaint1();
            cf = array[indexSkill].status;
            if (array[indexSkill].effS0Id != 0)
            {
                eff0 = GameScr.efs[array[indexSkill].effS0Id - 1];
                i0 = (dx0 = (dy0 = 0));
            }

            if (array[indexSkill].effS1Id != 0)
            {
                eff1 = GameScr.efs[array[indexSkill].effS1Id - 1];
                i1 = (dx1 = (dy1 = 0));
            }

            if (array[indexSkill].effS2Id != 0)
            {
                eff2 = GameScr.efs[array[indexSkill].effS2Id - 1];
                i2 = (dx2 = (dy2 = 0));
            }

            SkillInfoPaint[] array2 = array;
            if (advance && array2 != null && array2[indexSkill] != null && array2[indexSkill].arrowId != 0)
            {
                arr = new Arrow(this, GameScr.arrs[array2[indexSkill].arrowId - 1]);
                arr.life = 10;
                arr.ax = cx + array2[indexSkill].adx;
                arr.ay = cy + array2[indexSkill].ady;
            }

            paintCharWithoutSkill(g);
            if (cdir == 1)
            {
                if (eff0 != null)
                {
                    if (dx0 == 0)
                    {
                        dx0 = array[indexSkill].e0dx;
                    }

                    if (dy0 == 0)
                    {
                        dy0 = array[indexSkill].e0dy;
                    }

                    SmallImage.drawSmallImage(g, eff0.arrEfInfo[i0].idImg, cx + dx0 + eff0.arrEfInfo[i0].dx, cy + dy0 + eff0.arrEfInfo[i0].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    i0++;
                    if (i0 >= eff0.arrEfInfo.Length)
                    {
                        eff0 = null;
                        i0 = (dx0 = (dy0 = 0));
                    }
                }

                if (eff1 != null)
                {
                    if (dx1 == 0)
                    {
                        dx1 = array[indexSkill].e1dx;
                    }

                    if (dy1 == 0)
                    {
                        dy1 = array[indexSkill].e1dy;
                    }

                    SmallImage.drawSmallImage(g, eff1.arrEfInfo[i1].idImg, cx + dx1 + eff1.arrEfInfo[i1].dx, cy + dy1 + eff1.arrEfInfo[i1].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    i1++;
                    if (i1 >= eff1.arrEfInfo.Length)
                    {
                        eff1 = null;
                        i1 = (dx1 = (dy1 = 0));
                    }
                }

                if (eff2 != null)
                {
                    if (dx2 == 0)
                    {
                        dx2 = array[indexSkill].e2dx;
                    }

                    if (dy2 == 0)
                    {
                        dy2 = array[indexSkill].e2dy;
                    }

                    SmallImage.drawSmallImage(g, eff2.arrEfInfo[i2].idImg, cx + dx2 + eff2.arrEfInfo[i2].dx, cy + dy2 + eff2.arrEfInfo[i2].dy, 0, mGraphics.VCENTER | mGraphics.HCENTER);
                    i2++;
                    if (eff2.arrEfInfo != null && i2 >= eff2.arrEfInfo.Length)
                    {
                        eff2 = null;
                        i2 = (dx2 = (dy2 = 0));
                    }
                }
            }
            else
            {
                if (eff0 != null)
                {
                    if (dx0 == 0)
                    {
                        dx0 = array[indexSkill].e0dx;
                    }

                    if (dy0 == 0)
                    {
                        dy0 = array[indexSkill].e0dy;
                    }

                    SmallImage.drawSmallImage(g, eff0.arrEfInfo[i0].idImg, cx - dx0 - eff0.arrEfInfo[i0].dx, cy + dy0 + eff0.arrEfInfo[i0].dy, 2, mGraphics.VCENTER | mGraphics.HCENTER);
                    i0++;
                    if (i0 >= eff0.arrEfInfo.Length)
                    {
                        eff0 = null;
                        i0 = 0;
                        dx0 = 0;
                        dy0 = 0;
                    }
                }

                if (eff1 != null)
                {
                    if (dx1 == 0)
                    {
                        dx1 = array[indexSkill].e1dx;
                    }

                    if (dy1 == 0)
                    {
                        dy1 = array[indexSkill].e1dy;
                    }

                    SmallImage.drawSmallImage(g, eff1.arrEfInfo[i1].idImg, cx - dx1 - eff1.arrEfInfo[i1].dx, cy + dy1 + eff1.arrEfInfo[i1].dy, 2, mGraphics.VCENTER | mGraphics.HCENTER);
                    i1++;
                    if (i1 >= eff1.arrEfInfo.Length)
                    {
                        eff1 = null;
                        i1 = 0;
                        dx1 = 0;
                        dy1 = 0;
                    }
                }

                if (eff2 != null)
                {
                    if (dx2 == 0)
                    {
                        dx2 = array[indexSkill].e2dx;
                    }

                    if (dy2 == 0)
                    {
                        dy2 = array[indexSkill].e2dy;
                    }

                    SmallImage.drawSmallImage(g, eff2.arrEfInfo[i2].idImg, cx - dx2 - eff2.arrEfInfo[i2].dx, cy + dy2 + eff2.arrEfInfo[i2].dy, 2, mGraphics.VCENTER | mGraphics.HCENTER);
                    i2++;
                    if (eff2.arrEfInfo != null && i2 >= eff2.arrEfInfo.Length)
                    {
                        eff2 = null;
                        i2 = 0;
                        dx2 = 0;
                        dy2 = 0;
                    }
                }
            }

            indexSkill++;
        }
        catch (Exception)
        {
        }
    }

    public void paint_No(mGraphics g)
    {
    }

    public void paintBehindNewMount(mGraphics g)
    {
        int transform = 2;
        if (cdir == 1)
        {
            transform = 0;
        }

        SmallImage.drawSmallImage(g, frameMount[fNewMount][tickNewMount], cx, cy, transform, mGraphics.BOTTOM | mGraphics.HCENTER);
    }

    public void paintNewMount(mGraphics g, Part pW, Part ph, int[] idCoat, DataSkillEff partMatna)
    {
        int transform = 2;
        int anchor = 24;
        if (cdir == 1)
        {
            transform = 0;
            anchor = 0;
        }

        int num = 0;
        int num2 = -7;
        if (pW != null)
        {
            SmallImage.drawSmallImage(g, pW.pi[CharInfo[cf][3][0]].id, cx + (CharInfo[cf][3][1] + pW.pi[CharInfo[cf][3][0]].dx) * cdir, cy - CharInfo[cf][3][2] + pW.pi[CharInfo[cf][3][0]].dy - 10 + dyMount + num, transform, anchor);
        }

        if (idCoat != null && ID_PP == -1)
        {
            SmallImage.drawSmallImage(g, idCoat[tickCoat], cx + hdx + (dxBody + num2) * cdir, cy - SmallImage.getHeight(getLegId()) - 19 + dyBody + dyMount + num, transform, mGraphics.TOP | mGraphics.HCENTER);
        }

        int num3 = 18;
        int num4 = -4;
        if (cdir == 1)
        {
            num4 = 4;
        }

        SmallImage.drawSmallImage(g, frameMount[fNewMount][tickNewMount], cx, cy, transform, mGraphics.BOTTOM | mGraphics.HCENTER);
        if (ID_Body > -1)
        {
            paintBody(g, cx + num4 + getxs(), cy - num3 + dyMount - getys(), 1);
        }
        else
        {
            SmallImage.drawSmallImage(g, getBodyPaintId(), cx + hdx + dxBody * cdir, cy - SmallImage.getHeight(getLegId()) - 9 + dyBody + dyMount + num, transform, mGraphics.BOTTOM | mGraphics.HCENTER);
        }

        if (ID_HAIR > -1)
        {
            paintHair(g, cx + num4 + getxs(), cy - num3 + dyMount - getys(), 1);
        }
        else
        {
            SmallImage.drawSmallImage(g, getHeadId(), cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + ph.pi[CharInfo[0][0][0]].dy - 12 + dyHead + dyMount + num, transform, mGraphics.TOP | mGraphics.HCENTER);
        }

        if (partMatna != null)
        {
            paintMatNaTop(g, cx + hdx + dxHead * cdir, cy - CharInfo[0][0][2] + ph.pi[CharInfo[0][0][0]].dy - 12 + dyHead + dyMount + num + 31, 1, partMatna);
        }
    }
}
