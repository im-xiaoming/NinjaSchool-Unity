using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public int getdxSkill()
    {
        if (myskill != null)
        {
            return myskill.dx;
        }

        return 0;
    }

    public int getdySkill()
    {
        if (myskill != null)
        {
            return myskill.dy;
        }

        return 0;
    }

    public Skill getSkill(SkillTemplate skillTemplate)
    {
        for (int i = 0; i < vSkill.size(); i++)
        {
            Skill skill = (Skill)vSkill.elementAt(i);
            if (skill.template.Equals(skillTemplate))
            {
                return skill;
            }
        }

        return null;
    }

    public void saveLoadPreviousSkill()
    {
        if (mSystem.currentTimeMillis() - timeLastCheck <= 500)
        {
            return;
        }

        if (myskill.template.type != 1 && lastNormalSkill != null)
        {
            myskill = lastNormalSkill;
            Service.gI().selectSkill(myskill.template.id);
        }

        if (currentFireByShortcut)
        {
            if (lastNormalSkill != null)
            {
                myskill = lastNormalSkill;
                Service.gI().selectSkill(myskill.template.id);
            }
        }
        else if (GameScr.auto != 1)
        {
            lastNormalSkill = myskill;
        }

        timeLastCheck = mSystem.currentTimeMillis();
    }

    public void updateSkillFall()
    {
    }

    public void updateSkillStand()
    {
        setAttk();
    }

    public void setAttack()
    {
        int num = 0;
        try
        {
            if (Handle.T != null)
            {
                return;
            }

            // Arrow.endMe also calls setAttack; its impact must consume this cast before the next logic tick.
            skillActionTriggered = true;
            GameCanvas.debug("SetAttk", 0);
            if (me)
            {
                num = 1;
                GameCanvas.debug("Sk1", 0);
                if (myskill.template.type == 2)
                {
                    return;
                }

                if (myskill.template.id == 42 && !myskill.isCooldown())
                {
                    isBlinking = true;
                    timeStartBlink = mSystem.getCurrentTimeMillis();
                }

                GameCanvas.debug("Sk2", 0);
                if (skillPaint != null && (mobFocus != null || (charFocus != null && isMeCanAttackOtherPlayer(charFocus))))
                {
                    int dx;
                    int num2;
                    if (isUseLongRangeWeapon())
                    {
                        dx = myskill.dx;
                        num2 = myskill.dy;
                    }
                    else
                    {
                        dx = myskill.dx;
                        num2 = myskill.dy;
                    }

                    GameCanvas.debug("Sk3", 0);
                    MyVector myVector = new MyVector();
                    MyVector myVector2 = new MyVector();
                    if (charFocus != null)
                    {
                        GameCanvas.debug("Sk5", 0);
                        myVector2.addElement(charFocus);
                        for (int i = 0; i < GameScr.vCharInMap.size(); i++)
                        {
                            if (myVector.size() + myVector2.size() >= myskill.maxFight)
                            {
                                break;
                            }

                            Char @char = (Char)GameScr.vCharInMap.elementAt(i);
                            if (@char.statusMe != 14 && @char.statusMe != 5 && @char.statusMe != 15 && !@char.isInvisible && !@char.Equals(charFocus) && isMeCanAttackOtherPlayer(@char) && charFocus.cx - dx <= @char.cx && @char.cx <= charFocus.cx + dx && charFocus.cy - num2 <= @char.cy && @char.cy <= charFocus.cy + num2 && ((cdir == -1 && @char.cx <= cx) || (cdir == 1 && @char.cx >= cx)))
                            {
                                myVector2.addElement(@char);
                            }
                        }

                        for (int j = 0; j < GameScr.vMob.size(); j++)
                        {
                            if (myVector.size() + myVector2.size() >= myskill.maxFight)
                            {
                                break;
                            }

                            Mob mob = (Mob)GameScr.vMob.elementAt(j);
                            if (mob.status != 1 && mob.status != 0 && charFocus.cx - dx <= mob.x && mob.x <= charFocus.cx + dx && charFocus.cy - num2 <= mob.y && mob.y <= charFocus.cy + num2 && ((cdir == -1 && mob.x <= cx) || (cdir == 1 && mob.x >= cx)))
                            {
                                myVector.addElement(mob);
                            }
                        }
                    }
                    else if (mobFocus != null && mobFocus.status != 1 && mobFocus.status != 0)
                    {
                        GameCanvas.debug("Sk6", 0);
                        myVector.addElement(mobFocus);
                        for (int k = 0; k < GameScr.vMob.size(); k++)
                        {
                            if (myVector.size() + myVector2.size() >= myskill.maxFight)
                            {
                                break;
                            }

                            Mob mob2 = (Mob)GameScr.vMob.elementAt(k);
                            if (mob2.status != 1 && mob2.status != 0 && !mob2.Equals(mobFocus) && mobFocus.x - 100 <= mob2.x && mob2.x <= mobFocus.x + 100 && mobFocus.y - 50 <= mob2.y && mob2.y <= mobFocus.y + 50)
                            {
                                myVector.addElement(mob2);
                            }
                        }

                        for (int l = 0; l < GameScr.vCharInMap.size(); l++)
                        {
                            if (myVector.size() + myVector2.size() >= myskill.maxFight)
                            {
                                break;
                            }

                            Char char2 = (Char)GameScr.vCharInMap.elementAt(l);
                            if (char2.statusMe != 14 && char2.statusMe != 5 && char2.statusMe != 15 && !char2.isInvisible && ((cTypePk >= PK_PHE1 && cTypePk <= PK_PHE3 && char2.cTypePk >= PK_PHE1 && char2.cTypePk <= PK_PHE3 && cTypePk != char2.cTypePk) || char2.cTypePk == 3 || cTypePk == 3 || (char2.cTypePk == PK_NHOM && cTypePk == PK_NHOM) || (testCharId >= 0 && testCharId == char2.charID) || (killCharId >= 0 && killCharId == char2.charID)) && mobFocus.x - dx <= char2.cx && char2.cx <= mobFocus.x + dx && mobFocus.y - num2 <= char2.cy && char2.cy <= mobFocus.y + num2 && ((cdir == -1 && char2.cx <= cx) || (cdir == 1 && char2.cx >= cx)))
                            {
                                myVector2.addElement(char2);
                            }
                        }
                    }

                    GameCanvas.debug("Sk7", 0);
                    effPaints = new EffectPaint[myVector.size() + myVector2.size()];
                    for (int m = 0; m < myVector.size(); m++)
                    {
                        effPaints[m] = new EffectPaint();
                        effPaints[m].effCharPaint = GameScr.efs[skillPaint.effId - 1];
                        effPaints[m].eMob = (Mob)myVector.elementAt(m);
                    }

                    for (int n = 0; n < myVector2.size(); n++)
                    {
                        effPaints[n + myVector.size()] = new EffectPaint();
                        effPaints[n + myVector.size()].effCharPaint = GameScr.efs[skillPaint.effId - 1];
                        effPaints[n + myVector.size()].eChar = (Char)myVector2.elementAt(n);
                    }

                    GameCanvas.debug("Sk8", 0);
                    if (effPaints.Length > 1)
                    {
                        EPosition firstPos = new EPosition();
                        if (effPaints[0].eMob != null)
                        {
                            firstPos = new EPosition(effPaints[0].eMob.x, effPaints[0].eMob.y);
                        }
                        else if (effPaints[0].eChar != null)
                        {
                            firstPos = new EPosition(effPaints[0].eChar.cx, effPaints[0].eChar.cy);
                        }

                        MyVector myVector3 = new MyVector();
                        for (int num3 = 1; num3 < effPaints.Length; num3++)
                        {
                            if (effPaints[num3].eMob != null)
                            {
                                myVector3.addElement(new EPosition(effPaints[num3].eMob.x, effPaints[num3].eMob.y));
                            }
                            else if (effPaints[num3].eChar != null)
                            {
                                myVector3.addElement(new EPosition(effPaints[num3].eChar.cx, effPaints[num3].eChar.cy));
                            }

                            if (num3 > 5)
                            {
                                break;
                            }
                        }

                        Lightning.addLight(myVector3, firstPos, true, getClassColor());
                    }

                    GameCanvas.debug("Sk9", 0);
                    int type = 0;
                    if (mobFocus != null)
                    {
                        type = 1;
                    }
                    else if (charFocus != null)
                    {
                        type = 2;
                    }

                    if (me)
                    {
                        Service.gI().sendPlayerAttack(myVector, myVector2, type);
                    }

                    GameCanvas.debug("SkA", 0);
                }
            }
            else if (skillPaint != null && (mobFocus != null || charFocus != null))
            {
                num = 2;
                GameCanvas.debug("SkB", 0);
                if (attMobs != null && attChars != null)
                {
                    num = 3;
                    effPaints = new EffectPaint[attMobs.Length + attChars.Length];
                    for (int num4 = 0; num4 < attMobs.Length; num4++)
                    {
                        effPaints[num4] = new EffectPaint();
                        effPaints[num4].effCharPaint = GameScr.efs[skillPaint.effId - 1];
                        effPaints[num4].eMob = attMobs[num4];
                    }

                    for (int num5 = 0; num5 < attChars.Length; num5++)
                    {
                        effPaints[num5 + attMobs.Length] = new EffectPaint();
                        effPaints[num5 + attMobs.Length].effCharPaint = GameScr.efs[skillPaint.effId - 1];
                        effPaints[num5 + attMobs.Length].eChar = attChars[num5];
                    }

                    attMobs = null;
                    attChars = null;
                }
                else if (attMobs != null)
                {
                    num = 4;
                    effPaints = new EffectPaint[attMobs.Length];
                    for (int num6 = 0; num6 < attMobs.Length; num6++)
                    {
                        effPaints[num6] = new EffectPaint();
                        effPaints[num6].effCharPaint = GameScr.efs[skillPaint.effId - 1];
                        effPaints[num6].eMob = attMobs[num6];
                    }

                    attMobs = null;
                }
                else if (attChars != null)
                {
                    num = 5;
                    effPaints = new EffectPaint[attChars.Length];
                    for (int num7 = 0; num7 < attChars.Length; num7++)
                    {
                        effPaints[num7] = new EffectPaint();
                        effPaints[num7].effCharPaint = GameScr.efs[skillPaint.effId - 1];
                        effPaints[num7].eChar = attChars[num7];
                    }

                    attChars = null;
                }

                GameCanvas.debug("SkC", 0);
                num = 6;
                if (effPaints.Length > 1 && effPaints[0] != null)
                {
                    num = 7;
                    EPosition firstPos2 = new EPosition();
                    if (effPaints[0].eMob != null)
                    {
                        firstPos2 = new EPosition(effPaints[0].eMob.x, effPaints[0].eMob.y);
                    }
                    else if (effPaints[0].eChar != null)
                    {
                        firstPos2 = new EPosition(effPaints[0].eChar.cx, effPaints[0].eChar.cy);
                    }

                    num = 8;
                    MyVector myVector4 = new MyVector();
                    for (int num8 = 1; num8 < effPaints.Length; num8++)
                    {
                        if (effPaints[num8].eMob != null)
                        {
                            myVector4.addElement(new EPosition(effPaints[num8].eMob.x, effPaints[num8].eMob.y));
                        }
                        else if (effPaints[num8].eChar != null)
                        {
                            myVector4.addElement(new EPosition(effPaints[num8].eChar.cx, effPaints[num8].eChar.cy));
                        }

                        if (num8 > 5)
                        {
                            break;
                        }
                    }

                    num = 9;
                    Lightning.addLight(myVector4, firstPos2, true, getClassColor());
                }

                GameCanvas.debug("SkD", 0);
            }

            GameCanvas.debug("SkE", 0);
        }
        catch (Exception)
        {
        }
    }

    public static DataSkillEff getPartEff(short id, bool ishorse)
    {
        if (id == -1)
        {
            return null;
        }

        DataSkillEff dataSkillEff = (DataSkillEff)ALL_PART_EFF.get(id + string.Empty);
        if (dataSkillEff == null)
        {
            dataSkillEff = new DataSkillEff(id, ishorse);
            ALL_PART_EFF.put(id + string.Empty, dataSkillEff);
            dataSkillEff.timeRemove = (int)(mSystem.currentTimeMillis() / 1000);
        }
        else
        {
            dataSkillEff.timeRemove = mSystem.currentTimeMillis();
        }

        return dataSkillEff;
    }

    public void startCoatEffect(int x, int y)
    {
        if (isWolf && arrItemMounts[4].sys >= 4 && getSys() > 0 && GameCanvas.gameTick % 8 == 0)
        {
            if (getSys() == 1)
            {
                ServerEffect.addServerEffect(116, x, y, 2);
            }
            else if (getSys() == 2)
            {
                ServerEffect.addServerEffect(117, x, y, 2);
            }
            else if (getSys() == 3)
            {
                ServerEffect.addServerEffect(118, x, y, 2);
            }
        }
    }

    private int[] getClanEffect()
    {
        if (statusMe != 6 && statusMe != 1 && statusMe != 2 && statusMe != 10 && statusMe != 11)
        {
            return null;
        }

        int[] result = null;
        if (me)
        {
            if (arrItemBody[13] != null)
            {
                if (arrItemBody[13].template.id == 425)
                {
                    result = new int[5]
                    {
                        1687,
                        1688,
                        1689,
                        1690,
                        1691
                    };
                }
                else if (arrItemBody[13].template.id == 426)
                {
                    result = new int[5]
                    {
                        1682,
                        1683,
                        1684,
                        1685,
                        1686
                    };
                }
                else if (arrItemBody[13].template.id == 427)
                {
                    result = new int[5]
                    {
                        1677,
                        1678,
                        1679,
                        1680,
                        1681
                    };
                }
            }
        }
        else
        {
            if (glove == -1)
            {
                return null;
            }

            if (glove == 425)
            {
                result = new int[5]
                {
                    1687,
                    1688,
                    1689,
                    1690,
                    1691
                };
            }
            else if (glove == 426)
            {
                result = new int[5]
                {
                    1682,
                    1683,
                    1684,
                    1685,
                    1686
                };
            }
            else if (glove == 427)
            {
                result = new int[5]
                {
                    1677,
                    1678,
                    1679,
                    1680,
                    1681
                };
            }
        }

        return result;
    }

    public void callEff(int effId)
    {
        indexEff = 0;
        eff = GameScr.efs[effId];
    }

    public void callEffTask(int effId)
    {
        indexEffTask = 0;
        effTask = GameScr.efs[effId];
    }

    public bool isMeCanAttackOtherPlayer(Char cAtt)
    {
        if (cAtt != null && cAtt.isNhanbanz())
        {
            return false;
        }

        if (cAtt == null || getMyChar().myskill == null || getMyChar().myskill.template.type == 2 || getMyChar().myskill.template.type == 3 || (getMyChar().myskill.template.type == 4 && cAtt.statusMe != 14 && cAtt.statusMe != 5))
        {
            return false;
        }

        return ((((getMyChar().cTypePk == PK_PHE4 && (cAtt.cTypePk == PK_PHE3 || cAtt.cTypePk == PK_PHE1 || cAtt.cTypePk == PK_PHE2)) || (getMyChar().cTypePk == PK_PHE3 && (cAtt.cTypePk == PK_PHE4 || cAtt.cTypePk == PK_PHE1 || cAtt.cTypePk == PK_PHE2)) || (getMyChar().cTypePk == PK_PHE1 && (cAtt.cTypePk == PK_PHE4 || cAtt.cTypePk == PK_PHE2 || cAtt.cTypePk == PK_PHE3)) || (getMyChar().cTypePk == PK_PHE2 && (cAtt.cTypePk == PK_PHE4 || cAtt.cTypePk == PK_PHE1 || cAtt.cTypePk == PK_PHE3))) && !getMyChar().isTeam(cAtt) && !isLang()) || (cAtt.cTypePk == 3 && !getMyChar().isTeam(cAtt) && !isLang()) || (getMyChar().cTypePk == 3 && !getMyChar().isTeam(cAtt) && !isLang()) || (getMyChar().cTypePk == PK_NHOM && cAtt.cTypePk == PK_NHOM && !getMyChar().isTeam(cAtt) && !isLang()) || (getMyChar().testCharId >= 0 && getMyChar().testCharId == cAtt.charID) || (getMyChar().killCharId >= 0 && getMyChar().killCharId == cAtt.charID && !isLang()) || (cAtt.killCharId >= 0 && cAtt.killCharId == getMyChar().charID && !isLang())) && cAtt.statusMe != 14 && cAtt.statusMe != 5;
    }

    private void updateEffectWolf()
    {
        tickEffWolf++;
        if (tickEffWolf > 5)
        {
            tickEffWolf = 0;
        }
    }

    private void updateEffwolfMove()
    {
        if (arrItemMounts[4].template.id != 443 || arrItemMounts[4].sys < 2)
        {
            return;
        }

        Effx = EffdefautX;
        Effy = EffdefautY;
        if (idWolfW[1] == 1737)
        {
            if (cdir != 1)
            {
                EffdefautY -= 5;
            }
            else
            {
                EffdefautY -= 5;
            }
        }

        Domsang o;
        Domsang o2;
        if (cdir != 1)
        {
            o = new Domsang(EffdefautX - 4, EffdefautY, 0);
            o2 = new Domsang(EffdefautX - 1, EffdefautY, 0);
        }
        else
        {
            o = new Domsang(EffdefautX + 4, EffdefautY, 0);
            o2 = new Domsang(EffdefautX + 1, EffdefautY, 0);
        }

        if (statusMe != 1 || statusMe != 6)
        {
            vDomsang.addElement(o);
            vDomsang.addElement(o2);
        }
    }

    private void updateEffmotoMove()
    {
        if (arrItemMounts[4].template.id == 524 && arrItemMounts[4].sys >= 2)
        {
            Domsang o;
            Domsang o2;
            if (cdir != 1)
            {
                o = new Domsang(EffdefautX, EffdefautY, 1);
                o2 = new Domsang(EffdefautX1, EffdefautY1, 1);
            }
            else
            {
                o = new Domsang(EffdefautX, EffdefautY, 1);
                o2 = new Domsang(EffdefautX1, EffdefautY1, 1);
            }

            if ((statusMe == 2 || statusMe == 10) && GameCanvas.gameTick % 3 == 0)
            {
                vDomsang.addElement(o);
                vDomsang.addElement(o2);
            }
        }
    }

    public int[] geteffOngbo()
    {
        int[] result = null;
        if (isMoto && arrItemMounts[4].template.id == 485 && arrItemMounts[4].sys >= 3)
        {
            result = new int[3]
            {
                2094,
                2095,
                2096
            };
        }

        return result;
    }

    public int[] getEffmoto()
    {
        int[] result = null;
        if (isMoto)
        {
            result = new int[12]
            {
                2082,
                2082,
                2083,
                2083,
                2084,
                2084,
                2089,
                2089,
                2082,
                2082,
                2083,
                2083
            };
        }

        return result;
    }

    public int[] getEffwolf()
    {
        int[] result = null;
        if (isWolf)
        {
            result = new int[4]
            {
                2085,
                2086,
                2087,
                2088
            };
        }

        return result;
    }
}
