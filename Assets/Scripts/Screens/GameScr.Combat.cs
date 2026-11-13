using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    public void loadSkillShortcut()
    {
        Service.gI().loadRMS("KSkill");
        Service.gI().loadRMS("OSkill");
        Service.gI().loadRMS("CSkill");
    }

    public void loadSkillShortcutNhanban()
    {
        Service.gI().loadRMS("KSkill");
        Service.gI().loadRMS("OSkill");
        Service.gI().loadRMS("CSkill");
    }

    public void onOSkill(sbyte[] oSkillID)
    {
        onScreenSkill = new Skill[5];
        if (oSkillID == null)
        {
            loadDefaultOnScreenSkill();
            return;
        }

        for (int i = 0; i < oSkillID.Length; i++)
        {
            for (int j = 0; j < Char.getMyChar().vSkillFight.size(); j++)
            {
                Skill skill = (Skill)Char.getMyChar().vSkillFight.elementAt(j);
                if (skill.template.id == oSkillID[i])
                {
                    onScreenSkill[i] = skill;
                    break;
                }
            }
        }
    }

    public void onKSkill(sbyte[] kSkillID)
    {
        keySkill = new Skill[3];
        if (kSkillID == null)
        {
            loadDefaultKeySkill();
            return;
        }

        for (int i = 0; i < kSkillID.Length; i++)
        {
            for (int j = 0; j < Char.getMyChar().vSkillFight.size(); j++)
            {
                Skill skill = (Skill)Char.getMyChar().vSkillFight.elementAt(j);
                if (skill.template.id == kSkillID[i])
                {
                    keySkill[i] = skill;
                    break;
                }
            }
        }
    }

    public void onCSkill(sbyte[] cSkillID)
    {
        if (cSkillID == null || cSkillID.Length == 0)
        {
            if (Char.getMyChar().vSkillFight.size() > 0)
            {
                Char.getMyChar().myskill = (Skill)Char.getMyChar().vSkillFight.elementAt(0);
            }
        }
        else
        {
            for (int i = 0; i < Char.getMyChar().vSkillFight.size(); i++)
            {
                Skill skill = (Skill)Char.getMyChar().vSkillFight.elementAt(i);
                if (skill.template.id == cSkillID[0])
                {
                    Char.getMyChar().myskill = skill;
                    Char.getMyChar().lastNormalSkill = skill;
                    break;
                }
            }
        }

        if (Char.getMyChar().myskill != null)
        {
            Service.gI().selectSkill(Char.getMyChar().myskill.template.id);
            saveRMSCurrentSkill(Char.getMyChar().myskill.template.id);
        }
    }

    private void loadDefaultOnScreenSkill()
    {
        Out.println("LOAD DEFAULT ONSCREEN SKILL");
        for (int i = 0; i < onScreenSkill.Length && i < Char.getMyChar().vSkillFight.size(); i++)
        {
            Skill skill = (Skill)Char.getMyChar().vSkillFight.elementAt(i);
            onScreenSkill[i] = skill;
        }

        saveOnScreenSkillToRMS();
    }

    protected void doSetOnScreenSkill(SkillTemplate skillTemplate)
    {
        Skill skill = Char.getMyChar().getSkill(skillTemplate);
        MyVector myVector = new MyVector();
        // Offline change (user request): all 5 slots are offered; the original stopped at the first empty slot.
        for (int i = 0; i < 5; i++)
        {
            myVector.addElement(new Command(p: new object[2] { skill, i + string.Empty }, caption: mResources.CELLS + " " + (i + 1), action: 11120));
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    protected void saveOnScreenSkillToRMS()
    {
        sbyte[] array = new sbyte[onScreenSkill.Length];
        for (int i = 0; i < onScreenSkill.Length; i++)
        {
            if (onScreenSkill[i] == null)
            {
                array[i] = -1;
            }
            else
            {
                array[i] = onScreenSkill[i].template.id;
            }
        }

        if (Char.getMyChar().isHumanz())
        {
            Service.gI().saveRms("OSkill", array, 0);
        }
        else
        {
            Service.gI().saveRms("OSkill", array, 1);
        }
    }

    protected void saveRMSCurrentSkill(sbyte id)
    {
    }

    public void addSkillShortcut(Skill skill)
    {
        if (skill.template.type == 0)
        {
            return;
        }

        Out.println("ADD SKILL SHORTCUT TO SKILL " + skill.template.id);
        for (int i = 0; i < onScreenSkill.Length; i++)
        {
            if (onScreenSkill[i] == null)
            {
                onScreenSkill[i] = skill;
                break;
            }
        }

        for (int j = 0; j < keySkill.Length; j++)
        {
            if (keySkill[j] == null)
            {
                keySkill[j] = skill;
                break;
            }
        }

        if (Char.getMyChar().myskill == null)
        {
            Char.getMyChar().myskill = skill;
        }

        saveKeySkillToRMS();
        saveOnScreenSkillToRMS();
    }

    public void doskill()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 1;
        doMiniMenuInforMe();
    }

    public void doBiJuu_Skill()
    {
        currentCharViewInfo = Char.getMyChar();
        indexMenu = 6;
        indexBijuuMenu = 2;
        doMiniMenuInforMe();
    }

    public void readSkill()
    {
        DataInputStream dataInputStream = null;
        try
        {
            dataInputStream = new DataInputStream(RMS.loadRMS("nj_skill"));
            int num = dataInputStream.readShort();
            int num2 = 0;
            for (int i = 0; i < nClasss.Length; i++)
            {
                num2 += nClasss[i].skillTemplates.Length;
            }

            sks = new SkillPaint[num2];
            for (int j = 0; j < num; j++)
            {
                short num3 = dataInputStream.readShort();
                sks[num3] = new SkillPaint();
                sks[num3].id = num3;
                sks[num3].effId = dataInputStream.readShort();
                sks[num3].numEff = dataInputStream.readByte();
                sks[num3].skillStand = new SkillInfoPaint[dataInputStream.readByte()];
                for (int k = 0; k < sks[num3].skillStand.Length; k++)
                {
                    sks[num3].skillStand[k] = new SkillInfoPaint();
                    sks[num3].skillStand[k].status = dataInputStream.readByte();
                    sks[num3].skillStand[k].effS0Id = dataInputStream.readShort();
                    sks[num3].skillStand[k].e0dx = dataInputStream.readShort();
                    sks[num3].skillStand[k].e0dy = dataInputStream.readShort();
                    sks[num3].skillStand[k].effS1Id = dataInputStream.readShort();
                    sks[num3].skillStand[k].e1dx = dataInputStream.readShort();
                    sks[num3].skillStand[k].e1dy = dataInputStream.readShort();
                    sks[num3].skillStand[k].effS2Id = dataInputStream.readShort();
                    sks[num3].skillStand[k].e2dx = dataInputStream.readShort();
                    sks[num3].skillStand[k].e2dy = dataInputStream.readShort();
                    sks[num3].skillStand[k].arrowId = dataInputStream.readShort();
                    sks[num3].skillStand[k].adx = dataInputStream.readShort();
                    sks[num3].skillStand[k].ady = dataInputStream.readShort();
                }

                sks[num3].skillfly = new SkillInfoPaint[dataInputStream.readByte()];
                for (int l = 0; l < sks[num3].skillfly.Length; l++)
                {
                    sks[num3].skillfly[l] = new SkillInfoPaint();
                    sks[num3].skillfly[l].status = dataInputStream.readByte();
                    sks[num3].skillfly[l].effS0Id = dataInputStream.readShort();
                    sks[num3].skillfly[l].e0dx = dataInputStream.readShort();
                    sks[num3].skillfly[l].e0dy = dataInputStream.readShort();
                    sks[num3].skillfly[l].effS1Id = dataInputStream.readShort();
                    sks[num3].skillfly[l].e1dx = dataInputStream.readShort();
                    sks[num3].skillfly[l].e1dy = dataInputStream.readShort();
                    sks[num3].skillfly[l].effS2Id = dataInputStream.readShort();
                    sks[num3].skillfly[l].e2dx = dataInputStream.readShort();
                    sks[num3].skillfly[l].e2dy = dataInputStream.readShort();
                    sks[num3].skillfly[l].arrowId = dataInputStream.readShort();
                    sks[num3].skillfly[l].adx = dataInputStream.readShort();
                    sks[num3].skillfly[l].ady = dataInputStream.readShort();
                }
            }
        }
        catch (Exception ex)
        {
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

    public void doUpSkill()
    {
        if (indexTitle > 0 && indexTitle <= 4)
        {
            GameCanvas.inputDlg.show(mResources.INPUT_POINT, cmdSkillUp, TField.INPUT_TYPE_NUMERIC);
        }
    }

    public bool isAttack()
    {
        if (InfoDlg.isLock || Char.getMyChar().isLockAttack || Char.isLockKey || Char.getMyChar().isBlinking)
        {
            return false;
        }

        if (Char.getMyChar().mobFocus != null && ((Char.getMyChar().mobFocus.templateId == 97 && Char.getMyChar().cTypePk == Char.PK_PHE1) || (Char.getMyChar().mobFocus.templateId == 98 && Char.getMyChar().cTypePk == Char.PK_PHE1) || (Char.getMyChar().mobFocus.templateId == 96 && Char.getMyChar().cTypePk == Char.PK_PHE2) || (Char.getMyChar().mobFocus.templateId == 99 && Char.getMyChar().cTypePk == Char.PK_PHE2) || (Char.getMyChar().mobFocus.templateId == 200 && Char.getMyChar().cTypePk == Char.PK_PHE1) || (Char.getMyChar().mobFocus.templateId == 199 && Char.getMyChar().cTypePk == Char.PK_PHE2) || (Char.getMyChar().mobFocus.templateId == 198 && Char.getMyChar().cTypePk == Char.PK_PHE3)))
        {
            return false;
        }

        if (Char.getMyChar().myskill != null && Char.getMyChar().myskill.template.type == 2 && Char.getMyChar().npcFocus == null)
        {
            if (!checkSkillValid())
            {
                return false;
            }

            return true;
        }

        if (Char.getMyChar().skillPaint != null || (Char.getMyChar().charFocus != null && Char.getMyChar().charFocus.isNhanbanz()) || (Char.getMyChar().mobFocus == null && Char.getMyChar().npcFocus == null && Char.getMyChar().charFocus == null && Char.getMyChar().itemFocus == null))
        {
            return false;
        }

        if (Char.getMyChar().mobFocus != null)
        {
            if (Char.getMyChar().myskill == null)
            {
                return false;
            }

            if (Char.getMyChar().arrItemBody[1] == null)
            {
                InfoMe.addInfo(mResources.WEAPON_FAIL);
                return false;
            }

            if (Char.getMyChar().mobFocus.status == 1 || Char.getMyChar().mobFocus.status == 0 || Char.getMyChar().myskill.template.type == 4)
            {
                return false;
            }

            if (!checkSkillValid())
            {
                return false;
            }

            if (Char.getMyChar().cx < Char.getMyChar().mobFocus.x)
            {
                Char.getMyChar().cdir = 1;
            }
            else
            {
                Char.getMyChar().cdir = -1;
            }

            int num = Math.abs(Char.getMyChar().cx - Char.getMyChar().mobFocus.x);
            int num2 = Math.abs(Char.getMyChar().cy - Char.getMyChar().mobFocus.y);
            Char.getMyChar().cvx = 0;
            if (Char.getMyChar().isUseLongRangeWeapon())
            {
                if (num <= Char.getMyChar().myskill.dx && num2 <= Char.getMyChar().myskill.dy)
                {
                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                    return true;
                }

                Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().mobFocus.x, Char.getMyChar().cy);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                return false;
            }

            if ((Char.getMyChar().myskill.template.id == 24 || Char.getMyChar().myskill.template.id == 40 || Char.getMyChar().myskill.template.id == 42) && num <= Char.getMyChar().myskill.dx && num2 <= Char.getMyChar().myskill.dy)
            {
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                Char.getMyChar().cvx = 0;
                return true;
            }

            if (num <= Char.getMyChar().myskill.dx && num2 <= Char.getMyChar().myskill.dy && Char.getMyChar().cy >= Char.getMyChar().mobFocus.y - 10)
            {
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                Char.getMyChar().cvx = 0;
                return true;
            }

            Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().mobFocus.x + Char.getMyChar().mobFocus.dir * 12, Char.getMyChar().cy);
            GameCanvas.clearKeyHold();
            GameCanvas.clearKeyPressed();
            return false;
        }

        if (Char.getMyChar().npcFocus != null)
        {
            if (Char.getMyChar().cx < Char.getMyChar().npcFocus.cx)
            {
                Char.getMyChar().cdir = 1;
            }
            else
            {
                Char.getMyChar().cdir = -1;
            }

            if (Char.getMyChar().cx < Char.getMyChar().npcFocus.cx)
            {
                Char.getMyChar().npcFocus.cdir = -1;
            }
            else
            {
                Char.getMyChar().npcFocus.cdir = 1;
            }

            int num3 = Math.abs(Char.getMyChar().cx - Char.getMyChar().npcFocus.cx);
            int num4 = Math.abs(Char.getMyChar().cy - Char.getMyChar().npcFocus.cy);
            if (num3 < 60 && num4 < 40)
            {
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                if (Char.getMyChar().npcFocus.template.npcTemplateId == 13)
                {
                    InfoDlg.showWait();
                    Service.gI().openUIZone();
                }
                else
                {
                    Service.gI().openMenu(Char.getMyChar().npcFocus.template.npcTemplateId);
                    InfoDlg.showWait();
                }
            }
            else
            {
                Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().npcFocus.cx, Char.getMyChar().cy);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
            }

            return false;
        }

        if (Char.getMyChar().charFocus != null)
        {
            if (Char.getMyChar().cx < Char.getMyChar().charFocus.cx)
            {
                Char.getMyChar().cdir = 1;
            }
            else
            {
                Char.getMyChar().cdir = -1;
            }

            int num5 = Math.abs(Char.getMyChar().cx - Char.getMyChar().charFocus.cx);
            int num6 = Math.abs(Char.getMyChar().cy - Char.getMyChar().charFocus.cy);
            if (Char.getMyChar().isMeCanAttackOtherPlayer(Char.getMyChar().charFocus))
            {
                if (Char.getMyChar().myskill == null)
                {
                    return false;
                }

                if (Char.getMyChar().arrItemBody[1] == null)
                {
                    InfoMe.addInfo(mResources.WEAPON_FAIL);
                    return false;
                }

                if (!checkSkillValid())
                {
                    return false;
                }

                if (Char.getMyChar().cx < Char.getMyChar().charFocus.cx)
                {
                    Char.getMyChar().cdir = 1;
                }
                else
                {
                    Char.getMyChar().cdir = -1;
                }

                Char.getMyChar().cvx = 0;
                if (Char.getMyChar().isUseLongRangeWeapon())
                {
                    if (num5 <= Char.getMyChar().myskill.dx && num6 <= Char.getMyChar().myskill.dy)
                    {
                        GameCanvas.clearKeyHold();
                        GameCanvas.clearKeyPressed();
                        return true;
                    }

                    Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().charFocus.cx, Char.getMyChar().cy);
                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                    return false;
                }

                if ((Char.getMyChar().myskill.template.id == 24 || Char.getMyChar().myskill.template.id == 40 || Char.getMyChar().myskill.template.id == 42) && num5 <= Char.getMyChar().myskill.dx && num6 <= Char.getMyChar().myskill.dy)
                {
                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                    Char.getMyChar().cvx = 0;
                    return true;
                }

                if (num5 <= Char.getMyChar().myskill.dx && num6 <= Char.getMyChar().myskill.dy && Char.getMyChar().cy >= Char.getMyChar().charFocus.cy)
                {
                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                    Char.getMyChar().cvx = 0;
                    return true;
                }

                Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().charFocus.cx + Char.getMyChar().charFocus.cdir * 12, Char.getMyChar().cy);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                return false;
            }

            if (num5 < 60 && num6 < 40 && Char.getMyChar().charFocus.charID >= 0)
            {
                GameCanvas.clearKeyHold();
                if (Char.getMyChar().charFocus.statusMe != 14 && Char.getMyChar().charFocus.statusMe != 5 && TileMap.typeMap == TileMap.MAP_DAUTRUONG)
                {
                    return false;
                }

                if (!isShortcut)
                {
                    MyVector myVector = new MyVector();
                    myVector.addElement(new Command(mResources.CHAR_ORDER[6], 110397));
                    myVector.addElement(new Command(mResources.CHAR_ORDER[4], 110391));
                    if ((Char.getMyChar().ctypeClan == 4 || Char.getMyChar().ctypeClan == 3 || Char.getMyChar().ctypeClan == 2) && Char.getMyChar().charFocus.cClanName.Equals(string.Empty))
                    {
                        myVector.addElement(new Command(mResources.CHAR_ORDER[8], 110398));
                    }

                    if ((Char.getMyChar().charFocus.ctypeClan == 4 || Char.getMyChar().charFocus.ctypeClan == 3 || Char.getMyChar().charFocus.ctypeClan == 2) && Char.getMyChar().cClanName.Equals(string.Empty))
                    {
                        myVector.addElement(new Command(mResources.CHAR_ORDER[9], 110399));
                    }

                    if (Char.getMyChar().nClass.classId == 6)
                    {
                        myVector.addElement(new Command(mResources.CHAR_ORDER[11] + ": " + (Char.isAResuscitate ? mResources.OFF : mResources.ON), 1103991));
                    }

                    if (Char.getMyChar().charFocus.statusMe != 14 && Char.getMyChar().charFocus.statusMe != 5)
                    {
                        myVector.addElement(new Command(mResources.CHAR_ORDER[0], 110392));
                        myVector.addElement(new Command(mResources.CHAR_ORDER[1], 110393));
                        myVector.addElement(new Command(mResources.CHAR_ORDER[2], 110394));
                    }
                    else if (Char.getMyChar().myskill.template.type == 4)
                    {
                        myVector.addElement(new Command(mResources.CHAR_ORDER[5], 110395));
                    }

                    myVector.addElement(new Command(mResources.CHAR_ORDER[3], 110396));
                    GameCanvas.menu.startAt(myVector, 3);
                    GameCanvas.menu.wait = 5;
                }

                isShortcut = false;
            }
            else
            {
                Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().charFocus.cx, Char.getMyChar().cy);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
            }

            return false;
        }

        if (Char.getMyChar().itemFocus != null)
        {
            if (Char.getMyChar().statusMe != 1)
            {
                return false;
            }

            if (Char.getMyChar().cx < Char.getMyChar().itemFocus.x)
            {
                Char.getMyChar().cdir = 1;
            }
            else
            {
                Char.getMyChar().cdir = -1;
            }

            int num7 = Math.abs(Char.getMyChar().cx - Char.getMyChar().itemFocus.x);
            int num8 = Math.abs(Char.getMyChar().cy - Char.getMyChar().itemFocus.y);
            if ((num7 <= 35 && num8 < 35) || (auto != 0 && num7 <= 48 && num8 <= 48))
            {
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                Service.gI().pickItem(Char.getMyChar().itemFocus.itemMapID);
            }
            else
            {
                Char.getMyChar().currentMovePoint = new MovePoint(Char.getMyChar().itemFocus.x, Char.getMyChar().cy);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
            }

            return false;
        }

        return true;
    }

    private bool checkSkillValid()
    {
        if (Char.getMyChar().myskill != null && Char.getMyChar().cMP < Char.getMyChar().myskill.manaUse)
        {
            InfoMe.addInfo(mResources.NOT_ENOUGH_MP);
            return false;
        }

        if (Char.getMyChar().myskill == null || (Char.getMyChar().myskill.template.maxPoint > 0 && Char.getMyChar().myskill.point == 0))
        {
            GameCanvas.startOKDlg(mResources.SKILL_FAIL);
            return false;
        }

        if (Char.getMyChar().arrItemBody[1] == null)
        {
            GameCanvas.startOKDlg(mResources.WEAPON_EMPTY);
            return false;
        }

        return true;
    }

    private bool checkSkillValid2()
    {
        if (Char.getMyChar().myskill != null && Char.getMyChar().cMP < Char.getMyChar().myskill.manaUse)
        {
            return false;
        }

        if (Char.getMyChar().myskill == null || (Char.getMyChar().myskill.template.maxPoint > 0 && Char.getMyChar().myskill.point == 0))
        {
            return false;
        }

        if (Char.getMyChar().arrItemBody[1] == null)
        {
            return false;
        }

        return true;
    }

    private void doChangeSkill()
    {
        nSkill = 0;
        for (int i = 0; i < onScreenSkill.Length; i++)
        {
            if (onScreenSkill[i] != null)
            {
                nSkill++;
            }
        }

        if (nSkill > 1)
        {
            if (!isPaintSelectSkill || selectedIndexSkill == -1)
            {
                isPaintSelectSkill = true;
                for (int j = 0; j < onScreenSkill.Length; j++)
                {
                    if (onScreenSkill[j] == Char.getMyChar().myskill)
                    {
                        selectedIndexSkill = j;
                        break;
                    }
                }
            }

            selectedIndexSkill++;
            if (selectedIndexSkill >= onScreenSkill.Length)
            {
                selectedIndexSkill = 0;
            }

            if (onScreenSkill[selectedIndexSkill] == null)
            {
                selectedIndexSkill = 0;
            }

            center = new Command(string.Empty, 11059);
        }
        else if (!InfoMe.isEmpty())
        {
        }
    }

    public void doUseSkill(Skill skill, bool isShortcut, bool isSelectSkill)
    {
        selectedIndexSkill = -1;
        if (skill != null)
        {
            if (skill.template.type == 4 && Char.getMyChar().charFocus != null)
            {
                if (Char.getMyChar().charFocus.isNhanbanz())
                {
                    return;
                }

                if (Char.getMyChar().charFocus.statusMe == 14 || Char.getMyChar().charFocus.statusMe == 5)
                {
                    Service.gI().buffLive(Char.getMyChar().charFocus.charID);
                    if ((TileMap.tileTypeAtPixel(Char.getMyChar().cx, Char.getMyChar().cy) & TileMap.T_TOP) == TileMap.T_TOP)
                    {
                        Char.getMyChar().setSkillPaint(sks[49], 0);
                    }
                    else
                    {
                        Char.getMyChar().setSkillPaint(sks[49], 1);
                    }
                }
            }

            if (isSelectSkill)
            {
                Service.gI().selectSkill(skill.template.id);
            }

            saveRMSCurrentSkill(skill.template.id);
        }

        if (skill.template.type != 2)
        {
            resetButton();
        }

        if (skill != null)
        {
            Char.getMyChar().myskill = skill;
            if (Char.getMyChar().npcFocus == null && skill.template.type != 4)
            {
                doFire(isShortcut);
            }
        }
    }

    public void sortSkill()
    {
        for (int i = 0; i < Char.getMyChar().vSkillFight.size() - 1; i++)
        {
            Skill skill = (Skill)Char.getMyChar().vSkillFight.elementAt(i);
            for (int j = i + 1; j < Char.getMyChar().vSkillFight.size(); j++)
            {
                Skill skill2 = (Skill)Char.getMyChar().vSkillFight.elementAt(j);
                if (skill2.template.id < skill.template.id)
                {
                    Skill skill3 = skill2;
                    skill2 = skill;
                    skill = skill3;
                    Char.getMyChar().vSkillFight.setElementAt(skill, i);
                    Char.getMyChar().vSkillFight.setElementAt(skill2, j);
                }
            }
        }
    }

    /// <summary>Offline addition (user request): the 5-slot skill bar appears once the character has learned
    /// its first skill from a book (any skill other than the basic attack, template 0).</summary>
    private static bool hasBookSkill()
    {
        MyVector v = Char.getMyChar().vSkill;
        for (int i = 0; i < v.size(); i++)
        {
            Skill skill = (Skill)v.elementAt(i);
            if (skill != null && skill.template != null && skill.template.id != 0)
            {
                return true;
            }
        }

        return false;
    }

    private void actdoChooseSkill()
    {
        if (selectedIndexSkill >= 0 && selectedIndexSkill < onScreenSkill.Length)
        {
            Skill skill = onScreenSkill[selectedIndexSkill];
            doUseSkill(skill, false, true);
        }
    }

    private void actmenuAttack(int idAction)
    {
        if (Char.getMyChar().charFocus != null && !Char.getMyChar().charFocus.isNhanbanz())
        {
            switch (idAction)
            {
                case 1:
                    Service.gI().addParty(Char.getMyChar().charFocus.cName);
                    break;
                case 2:
                    Service.gI().tradeInvite(Char.getMyChar().charFocus.charID);
                    break;
                case 3:
                    Service.gI().testInvite(Char.getMyChar().charFocus.charID);
                    break;
                case 4:
                    Service.gI().addCuuSat(Char.getMyChar().charFocus.charID);
                    break;
                case 5:
                    doRescuedOtherChar(Char.getMyChar().charFocus.charID);
                    break;
                case 6:
                    Service.gI().addFriend(Char.getMyChar().charFocus.cName);
                    break;
                case 7:
                    Service.gI().viewInfo(Char.getMyChar().charFocus.cName);
                    gI().resetButton();
                    break;
                case 8:
                    Service.gI().clanInvite(Char.getMyChar().charFocus.charID);
                    break;
                case 9:
                    Service.gI().clanPlease(Char.getMyChar().charFocus.charID);
                    break;
                case 10:
                    Char.isAResuscitate = !Char.isAResuscitate;
                    Char.aCID = Char.getMyChar().charFocus.charID;
                    break;
            }
        }
    }

    private void actSkillUpPoint()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.startOKDlg(mResources.NOT_INPUT_POINT);
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().sPoint == 0 || num > Char.getMyChar().sPoint)
        {
            GameCanvas.startOKDlg(mResources.NOT_INPUT_POINT);
            GameCanvas.msgdlg.show();
        }
        else
        {
            Service.gI().upSkill(Char.getMyChar().nClass.skillTemplates[indexSelect].id, num);
            GameCanvas.inputDlg.hide();
        }
    }

    public void doBijuuUpSkill()
    {
        if (indexTitle > 0 && indexTitle <= 4)
        {
            GameCanvas.inputDlg.show(mResources.INPUT_POINT, cmdBijuuSkillUp, TField.INPUT_TYPE_NUMERIC);
        }
    }

    private void actBijSkillUpPoint()
    {
        string text = GameCanvas.inputDlg.tfInput.getText();
        if (text.Trim().Equals(string.Empty))
        {
            GameCanvas.startOKDlg(mResources.NOT_INPUT_POINT);
            return;
        }

        int num = 0;
        try
        {
            num = int.Parse(text);
        }
        catch (Exception)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().bijuuSkillPoint == 0 || num > Char.getMyChar().bijuuSkillPoint)
        {
            GameCanvas.startOKDlg(mResources.NOT_INPUT_POINT);
            GameCanvas.msgdlg.show();
        }
        else
        {
            Service.gI().upSkill(Char.getMyChar().nClass.skillTemplates[indexSelect].id, num);
            GameCanvas.inputDlg.hide();
        }
    }
}
