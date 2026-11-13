using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public static bool isCharInScreen(Char c)
    {
        int cmx = GameScr.cmx;
        int num = GameScr.cmx + GameCanvas.w;
        int num2 = GameScr.cmy + 10;
        int num3 = GameScr.cmy + GameScr.gH;
        if (c.statusMe != 15 && !c.isInvisible && cmx <= c.cx && c.cx <= num && num2 <= c.cy && c.cy <= num3)
        {
            return true;
        }

        return false;
    }

    public static int getIndexChar(int ID)
    {
        for (int i = 0; i < GameScr.vCharInMap.size(); i++)
        {
            Char @char = (Char)GameScr.vCharInMap.elementAt(i);
            if (@char.charID == ID)
            {
                return i;
            }
        }

        return -1;
    }

    public void searchFocus()
    {
        if (charFocus != null && charFocus.isNhanban)
        {
            charFocus = null;
        }

        if (isManualFocus && charFocus != null && (charFocus.statusMe == 15 || charFocus.isInvisible))
        {
            charFocus = null;
        }

        if (GameCanvas.gameTick % 2 == 0 || isMeCanAttackOtherPlayer(charFocus))
        {
            return;
        }

        int num = 0;
        if (nClass.classId == 0 || nClass.classId == 1 || nClass.classId == 3 || nClass.classId == 5)
        {
            num = ((GameScr.auto != 1) ? 40 : 0);
        }

        int[] array = new int[4]
        {
            -1,
            -1,
            -1,
            -1
        };
        int num2 = GameScr.cmx - 10;
        int num3 = GameScr.cmx + GameCanvas.w + 10;
        int cmy = GameScr.cmy;
        int num4 = GameScr.cmy + GameCanvas.h - GameScr.cmdBarH + 10;
        if (isManualFocus)
        {
            if ((mobFocus != null && mobFocus.status != 1 && mobFocus.status != 0 && num2 <= mobFocus.x && mobFocus.x <= num3 && cmy <= mobFocus.y && mobFocus.y <= num4) || (npcFocus != null && num2 <= npcFocus.cx && npcFocus.cx <= num3 && cmy <= npcFocus.cy && npcFocus.cy <= num4) || (charFocus != null && num2 <= charFocus.cx && charFocus.cx <= num3 && cmy <= charFocus.cy && charFocus.cy <= num4) || (itemFocus != null && num2 <= itemFocus.x && itemFocus.x <= num3 && cmy <= itemFocus.y && itemFocus.y <= num4))
            {
                return;
            }

            isManualFocus = false;
        }

        if (itemFocus == null)
        {
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
            {
                ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
                int num5 = Math.abs(getMyChar().cx - itemMap.x);
                int num6 = Math.abs(getMyChar().cy - itemMap.y);
                int num7 = ((num5 <= num6) ? num6 : num5);
                if (num5 > 48 || num6 > 48 || (itemFocus != null && num7 >= array[3]))
                {
                    continue;
                }

                if (GameScr.auto != 0 && GameScr.gI().isBagFull())
                {
                    if (itemMap.template.type == 19)
                    {
                        if (GameScr.isUseitemAuto)
                        {
                            itemFocus = null;
                        }
                        else
                        {
                            itemFocus = itemMap;
                        }

                        array[3] = num7;
                    }
                }
                else if (isAPickYen || isAPickYHM || isAPickYHMS || isANoPick)
                {
                    if ((isAPickYen && itemMap.template.type == 19) || (isAPickYHM && (itemMap.template.type == 19 || itemMap.template.type == 16 || itemMap.template.type == 17)) || (isAPickYHMS && (itemMap.template.type == 19 || itemMap.template.type == 16 || itemMap.template.type == 17 || itemMap.template.type == 26)))
                    {
                        if (GameScr.isUseitemAuto)
                        {
                            itemFocus = null;
                        }
                        else
                        {
                            itemFocus = itemMap;
                        }

                        array[3] = num7;
                    }
                }
                else
                {
                    if (GameScr.isUseitemAuto)
                    {
                        itemFocus = null;
                    }
                    else
                    {
                        itemFocus = itemMap;
                    }

                    array[3] = num7;
                }
            }
        }
        else
        {
            if (num2 <= itemFocus.x && itemFocus.x <= num3 && cmy <= itemFocus.y && itemFocus.y <= num4)
            {
                clearFocus(3);
                return;
            }

            itemFocus = null;
            for (int j = 0; j < GameScr.vItemMap.size(); j++)
            {
                ItemMap itemMap2 = (ItemMap)GameScr.vItemMap.elementAt(j);
                int num8 = Math.abs(getMyChar().cx - itemMap2.x);
                int num9 = Math.abs(getMyChar().cy - itemMap2.y);
                int num10 = ((num8 <= num9) ? num9 : num8);
                if (num2 > itemMap2.x || itemMap2.x > num3 || cmy > itemMap2.y || itemMap2.y > num4 || (itemFocus != null && num10 >= array[3]))
                {
                    continue;
                }

                if (GameScr.auto != 0 && GameScr.gI().isBagFull())
                {
                    if (itemMap2.template.type == 19)
                    {
                        if (GameScr.isUseitemAuto)
                        {
                            itemFocus = null;
                        }
                        else
                        {
                            itemFocus = itemMap2;
                        }

                        array[3] = num10;
                    }
                }
                else if (isAPickYen || isAPickYHM || isAPickYHMS || isANoPick)
                {
                    if ((isAPickYen && itemMap2.template.type == 19) || (isAPickYHM && (itemMap2.template.type == 19 || itemMap2.template.type == 16 || itemMap2.template.type == 17)) || (isAPickYHMS && (itemMap2.template.type == 19 || itemMap2.template.type == 16 || itemMap2.template.type == 17 || itemMap2.template.type == 26)))
                    {
                        if (GameScr.isUseitemAuto)
                        {
                            itemFocus = null;
                        }
                        else
                        {
                            itemFocus = itemMap2;
                        }

                        array[3] = num10;
                    }
                }
                else
                {
                    if (GameScr.isUseitemAuto)
                    {
                        itemFocus = null;
                    }
                    else
                    {
                        itemFocus = itemMap2;
                    }

                    array[3] = num10;
                }
            }
        }

        if (TileMap.typeMap == TileMap.MAP_CHIENTRUONG || TileMap.mapID == 111)
        {
            num2 = getMyChar().cx - getMyChar().getdxSkill();
            num3 = getMyChar().cx + getMyChar().getdxSkill();
            cmy = getMyChar().cy - getMyChar().getdySkill() - num;
            num4 = getMyChar().cy + getMyChar().getdySkill();
            if (num4 > getMyChar().cy + 30)
            {
                num4 = getMyChar().cy + 30;
            }

            if (mobFocus == null)
            {
                for (int k = 0; k < GameScr.vMob.size(); k++)
                {
                    Mob mob = (Mob)GameScr.vMob.elementAt(k);
                    int num11 = Math.abs(getMyChar().cx - mob.x);
                    int num12 = Math.abs(getMyChar().cy - mob.y);
                    int num13 = ((num11 <= num12) ? num12 : num11);
                    if ((mob.templateId != 97 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 96 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 98 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 167 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 99 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 166 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 200 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 199 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 198 || getMyChar().cTypePk != PK_PHE3) && (mob.templateId != 202 || mob.status != 8 || mob.isPaint()) && (!GameScr.isUseitemAuto || mob.levelBoss != 3) && num2 <= mob.x && mob.x <= num3 && cmy <= mob.y && mob.y <= num4 && mob.status != 0 && mob.status != 1 && (mobFocus == null || num13 < array[0]))
                    {
                        mobFocus = mob;
                        array[0] = num13;
                    }
                }
            }
            else
            {
                if (mobFocus.status != 1 && mobFocus.status != 0 && num2 <= mobFocus.x && mobFocus.x <= num3 && cmy <= mobFocus.y && mobFocus.y <= num4)
                {
                    clearFocus(0);
                    return;
                }

                mobFocus = null;
                for (int l = 0; l < GameScr.vMob.size(); l++)
                {
                    Mob mob2 = (Mob)GameScr.vMob.elementAt(l);
                    int num14 = Math.abs(getMyChar().cx - mob2.x);
                    int num15 = Math.abs(getMyChar().cy - mob2.y);
                    int num16 = ((num14 <= num15) ? num15 : num14);
                    if ((mob2.templateId != 97 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 96 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 98 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 167 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 99 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 166 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 200 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 199 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 198 || getMyChar().cTypePk != PK_PHE3) && (mob2.templateId != 202 || mob2.status != 8 || mob2.isPaint()) && (!GameScr.isUseitemAuto || mob2.levelBoss != 3) && num2 <= mob2.x && mob2.x <= num3 && cmy <= mob2.y && mob2.y <= num4 && mob2.status != 0 && mob2.status != 1 && (mobFocus == null || num16 < array[0]))
                    {
                        mobFocus = mob2;
                        array[0] = num16;
                    }
                }
            }

            num2 = getMyChar().cx - 80;
            num3 = getMyChar().cx + 80;
            cmy = getMyChar().cy - 30;
            num4 = getMyChar().cy + 30;
            if (npcFocus != null && npcFocus.template.npcTemplateId == 13)
            {
                num2 = getMyChar().cx - 20;
                num3 = getMyChar().cx + 20;
                cmy = getMyChar().cy - 10;
                num4 = getMyChar().cy + 10;
            }

            if (npcFocus == null)
            {
                for (int m = 0; m < GameScr.vNpc.size(); m++)
                {
                    Npc npc = (Npc)GameScr.vNpc.elementAt(m);
                    if (npc.statusMe == 15)
                    {
                        continue;
                    }

                    int num17 = Math.abs(getMyChar().cx - npc.cx);
                    int num18 = Math.abs(getMyChar().cy - npc.cy);
                    int num19 = ((num17 <= num18) ? num18 : num17);
                    num2 = getMyChar().cx - 80;
                    num3 = getMyChar().cx + 80;
                    cmy = getMyChar().cy - 30;
                    num4 = getMyChar().cy + 30;
                    if (npc.template.npcTemplateId == 13)
                    {
                        num2 = getMyChar().cx - 20;
                        num3 = getMyChar().cx + 20;
                        cmy = getMyChar().cy - 10;
                        num4 = getMyChar().cy + 10;
                    }

                    if (num2 <= npc.cx && npc.cx <= num3 && cmy <= npc.cy && npc.cy <= num4 && (npcFocus == null || num19 < array[1]))
                    {
                        if (GameScr.isUseitemAuto && GameScr.auto == 1)
                        {
                            break;
                        }

                        npcFocus = npc;
                        array[1] = num19;
                    }
                }
            }
            else
            {
                if (num2 <= npcFocus.cx && npcFocus.cx <= num3 && cmy <= npcFocus.cy && npcFocus.cy <= num4)
                {
                    clearFocus(1);
                    return;
                }

                deFocusNPC();
                for (int n = 0; n < GameScr.vNpc.size(); n++)
                {
                    Npc npc2 = (Npc)GameScr.vNpc.elementAt(n);
                    if (npc2.statusMe == 15)
                    {
                        continue;
                    }

                    int num20 = Math.abs(getMyChar().cx - npc2.cx);
                    int num21 = Math.abs(getMyChar().cy - npc2.cy);
                    int num22 = ((num20 <= num21) ? num21 : num20);
                    num2 = getMyChar().cx - 80;
                    num3 = getMyChar().cx + 80;
                    cmy = getMyChar().cy - 30;
                    num4 = getMyChar().cy + 30;
                    if (npc2.template.npcTemplateId == 13)
                    {
                        num2 = getMyChar().cx - 20;
                        num3 = getMyChar().cx + 20;
                        cmy = getMyChar().cy - 10;
                        num4 = getMyChar().cy + 10;
                    }

                    if (num2 <= npc2.cx && npc2.cx <= num3 && cmy <= npc2.cy && npc2.cy <= num4 && (npcFocus == null || num22 < array[1]))
                    {
                        if (GameScr.isUseitemAuto && GameScr.auto == 1)
                        {
                            break;
                        }

                        npcFocus = npc2;
                        array[1] = num22;
                    }
                }
            }

            num2 = getMyChar().cx - 40;
            num3 = getMyChar().cx + 40;
            cmy = getMyChar().cy - 30;
            num4 = getMyChar().cy + 30;
            if (charFocus == null)
            {
                for (int num23 = 0; num23 < GameScr.vCharInMap.size(); num23++)
                {
                    Char @char = (Char)GameScr.vCharInMap.elementAt(num23);
                    if (@char.isNhanbanz())
                    {
                        continue;
                    }

                    if (TileMap.mapID != 111)
                    {
                        if (@char.statusMe == 15 || @char.isInvisible || @char.cTypePk == myChar.cTypePk || wdx != 0 || wdy != 0 || @char.statusMe == 14 || @char.statusMe == 5)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (@char.statusMe == 15 || @char.isInvisible || wdx != 0 || wdy != 0)
                        {
                            continue;
                        }

                        if (myChar.nClass.classId == 6)
                        {
                            if (myChar.cTypePk == @char.cTypePk)
                            {
                                if (@char.statusMe != 14 || @char.statusMe != 5)
                                {
                                    continue;
                                }
                            }
                            else if (@char.statusMe == 14 || @char.statusMe == 5)
                            {
                                continue;
                            }
                        }
                        else if (myChar.cTypePk == @char.cTypePk || @char.statusMe == 14 || @char.statusMe == 5)
                        {
                            continue;
                        }
                    }

                    int num24 = Math.abs(getMyChar().cx - @char.cx);
                    int num25 = Math.abs(getMyChar().cy - @char.cy);
                    int num26 = ((num24 <= num25) ? num25 : num24);
                    if (num2 <= @char.cx && @char.cx <= num3 && cmy <= @char.cy && @char.cy <= num4 && (charFocus == null || num26 < array[2]))
                    {
                        charFocus = @char;
                        array[2] = num26;
                    }
                }
            }
            else
            {
                if (num2 <= charFocus.cx && charFocus.cx <= num3 && cmy <= charFocus.cy && charFocus.cy <= num4 && charFocus.statusMe != 15 && !charFocus.isInvisible && charFocus.statusMe != 14 && charFocus.statusMe != 5)
                {
                    clearFocus(2);
                    return;
                }

                charFocus = null;
                for (int num27 = 0; num27 < GameScr.vCharInMap.size(); num27++)
                {
                    Char char2 = (Char)GameScr.vCharInMap.elementAt(num27);
                    if (char2.isNhanbanz())
                    {
                        continue;
                    }

                    if (TileMap.mapID != 111)
                    {
                        if (char2.statusMe == 15 || char2.isInvisible || char2.cTypePk == myChar.cTypePk || wdx != 0 || wdy != 0 || char2.statusMe == 14 || char2.statusMe == 5)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (char2.statusMe == 15 || char2.isInvisible || wdx != 0 || wdy != 0)
                        {
                            continue;
                        }

                        if (myChar.nClass.classId == 6)
                        {
                            if (myChar.cTypePk == char2.cTypePk)
                            {
                                if (char2.statusMe != 14 || char2.statusMe != 5)
                                {
                                    continue;
                                }
                            }
                            else if (char2.statusMe == 14 || char2.statusMe == 5)
                            {
                                continue;
                            }
                        }
                        else if (myChar.cTypePk == char2.cTypePk || char2.statusMe == 14 || char2.statusMe == 5)
                        {
                            continue;
                        }
                    }

                    int num28 = Math.abs(getMyChar().cx - char2.cx);
                    int num29 = Math.abs(getMyChar().cy - char2.cy);
                    int num30 = ((num28 <= num29) ? num29 : num28);
                    if (num2 <= char2.cx && char2.cx <= num3 && cmy <= char2.cy && char2.cy <= num4 && (charFocus == null || num30 < array[2]))
                    {
                        charFocus = char2;
                        array[2] = num30;
                    }
                }
            }

            int num31 = -1;
            for (int num32 = 0; num32 < array.Length; num32++)
            {
                if (num31 == -1)
                {
                    if (array[num32] != -1)
                    {
                        num31 = num32;
                    }
                }
                else if (array[num32] < array[num31] && array[num32] != -1)
                {
                    num31 = num32;
                }
            }

            clearFocus(num31);
            return;
        }

        num2 = getMyChar().cx - getMyChar().getdxSkill() - 10;
        num3 = getMyChar().cx + getMyChar().getdxSkill() + 10;
        cmy = getMyChar().cy - getMyChar().getdySkill() - num;
        num4 = getMyChar().cy + getMyChar().getdySkill();
        if (num4 > getMyChar().cy + 30)
        {
            num4 = getMyChar().cy + 30;
        }

        if (mobFocus == null)
        {
            for (int num33 = 0; num33 < GameScr.vMob.size(); num33++)
            {
                Mob mob3 = (Mob)GameScr.vMob.elementAt(num33);
                int num34 = Math.abs(getMyChar().cx - mob3.x);
                int num35 = Math.abs(getMyChar().cy - mob3.y);
                int num36 = ((num34 <= num35) ? num35 : num34);
                if ((mob3.templateId != 97 || getMyChar().cTypePk != PK_PHE1) && (mob3.templateId != 98 || getMyChar().cTypePk != PK_PHE1) && (mob3.templateId != 167 || getMyChar().cTypePk != PK_PHE1) && (mob3.templateId != 99 || getMyChar().cTypePk != PK_PHE2) && (mob3.templateId != 96 || getMyChar().cTypePk != PK_PHE2) && (mob3.templateId != 166 || getMyChar().cTypePk != PK_PHE2) && (mob3.templateId != 200 || getMyChar().cTypePk != PK_PHE1) && (mob3.templateId != 199 || getMyChar().cTypePk != PK_PHE2) && (mob3.templateId != 198 || getMyChar().cTypePk != PK_PHE3) && (mob3.templateId != 202 || mob3.status != 8 || mob3.isPaint()) && (!GameScr.isUseitemAuto || mob3.levelBoss != 3) && mob3.templateId != 202 && num2 <= mob3.x && mob3.x <= num3 && cmy <= mob3.y && mob3.y <= num4 && mob3.status != 0 && mob3.status != 1 && (mobFocus == null || num36 < array[0]))
                {
                    mobFocus = mob3;
                    array[0] = num36;
                }
            }
        }
        else
        {
            if (mobFocus.status != 1 && mobFocus.status != 0 && num2 <= mobFocus.x && mobFocus.x <= num3 && cmy <= mobFocus.y && mobFocus.y <= num4)
            {
                clearFocus(0);
                return;
            }

            mobFocus = null;
            for (int num37 = 0; num37 < GameScr.vMob.size(); num37++)
            {
                Mob mob4 = (Mob)GameScr.vMob.elementAt(num37);
                int num38 = Math.abs(getMyChar().cx - mob4.x);
                int num39 = Math.abs(getMyChar().cy - mob4.y);
                int num40 = ((num38 <= num39) ? num39 : num38);
                if ((mob4.templateId != 97 || getMyChar().cTypePk != PK_PHE1) && (mob4.templateId != 96 || getMyChar().cTypePk != PK_PHE2) && (mob4.templateId != 98 || getMyChar().cTypePk != PK_PHE1) && (mob4.templateId != 167 || getMyChar().cTypePk != PK_PHE1) && (mob4.templateId != 99 || getMyChar().cTypePk != PK_PHE2) && (mob4.templateId != 166 || getMyChar().cTypePk != PK_PHE2) && (mob4.templateId != 96 || getMyChar().cTypePk != PK_PHE3) && (mob4.templateId != 99 || getMyChar().cTypePk != PK_PHE3) && (mob4.templateId != 166 || getMyChar().cTypePk != PK_PHE3) && (mob4.templateId != 202 || mob4.status != 8 || mob4.isPaint()) && (!GameScr.isUseitemAuto || mob4.levelBoss != 3) && mob4.templateId != 202 && (!GameScr.isUseitemAuto || mob4.levelBoss != 3) && mob4.templateId != 202 && num2 <= mob4.x && mob4.x <= num3 && cmy <= mob4.y && mob4.y <= num4 && mob4.status != 0 && mob4.status != 1 && (mobFocus == null || num40 < array[0]))
                {
                    mobFocus = mob4;
                    array[0] = num40;
                }
            }
        }

        num2 = getMyChar().cx - 80;
        num3 = getMyChar().cx + 80;
        cmy = getMyChar().cy - 30;
        num4 = getMyChar().cy + 30;
        if (npcFocus != null && npcFocus.template.npcTemplateId == 13)
        {
            num2 = getMyChar().cx - 20;
            num3 = getMyChar().cx + 20;
            cmy = getMyChar().cy - 10;
            num4 = getMyChar().cy + 10;
        }

        if (npcFocus == null)
        {
            for (int num41 = 0; num41 < GameScr.vNpc.size(); num41++)
            {
                Npc npc3 = (Npc)GameScr.vNpc.elementAt(num41);
                if (npc3.statusMe == 15 || TileMap.typeMap == TileMap.MAP_DAUTRUONG)
                {
                    continue;
                }

                int num42 = Math.abs(getMyChar().cx - npc3.cx);
                int num43 = Math.abs(getMyChar().cy - npc3.cy);
                int num44 = ((num42 <= num43) ? num43 : num42);
                num2 = getMyChar().cx - 80;
                num3 = getMyChar().cx + 80;
                cmy = getMyChar().cy - 30;
                num4 = getMyChar().cy + 30;
                if (npc3.template.npcTemplateId == 13)
                {
                    num2 = getMyChar().cx - 20;
                    num3 = getMyChar().cx + 20;
                    cmy = getMyChar().cy - 10;
                    num4 = getMyChar().cy + 10;
                }

                if (num2 <= npc3.cx && npc3.cx <= num3 && cmy <= npc3.cy && npc3.cy <= num4 && (npcFocus == null || num44 < array[1]))
                {
                    if (GameScr.isUseitemAuto && GameScr.auto == 1)
                    {
                        break;
                    }

                    npcFocus = npc3;
                    array[1] = num44;
                }
            }
        }
        else
        {
            if (num2 <= npcFocus.cx && npcFocus.cx <= num3 && cmy <= npcFocus.cy && npcFocus.cy <= num4)
            {
                clearFocus(1);
                return;
            }

            deFocusNPC();
            for (int num45 = 0; num45 < GameScr.vNpc.size(); num45++)
            {
                Npc npc4 = (Npc)GameScr.vNpc.elementAt(num45);
                if (npc4.statusMe == 15 || TileMap.typeMap == TileMap.MAP_DAUTRUONG)
                {
                    continue;
                }

                int num46 = Math.abs(getMyChar().cx - npc4.cx);
                int num47 = Math.abs(getMyChar().cy - npc4.cy);
                int num48 = ((num46 <= num47) ? num47 : num46);
                num2 = getMyChar().cx - 80;
                num3 = getMyChar().cx + 80;
                cmy = getMyChar().cy - 30;
                num4 = getMyChar().cy + 30;
                if (npc4.template.npcTemplateId == 13)
                {
                    num2 = getMyChar().cx - 20;
                    num3 = getMyChar().cx + 20;
                    cmy = getMyChar().cy - 10;
                    num4 = getMyChar().cy + 10;
                }

                if (num2 <= npc4.cx && npc4.cx <= num3 && cmy <= npc4.cy && npc4.cy <= num4 && (npcFocus == null || num48 < array[1]))
                {
                    if (GameScr.isUseitemAuto && GameScr.auto == 1)
                    {
                        break;
                    }

                    npcFocus = npc4;
                    array[1] = num48;
                }
            }
        }

        if (charFocus == null)
        {
            for (int num49 = 0; num49 < GameScr.vCharInMap.size(); num49++)
            {
                Char char3 = (Char)GameScr.vCharInMap.elementAt(num49);
                if (!char3.isNhanbanz() && char3.statusMe != 15 && !char3.isInvisible && char3.charID < -1 && wdx == 0 && wdy == 0 && char3.statusMe != 14 && char3.statusMe != 5)
                {
                    int num50 = Math.abs(getMyChar().cx - char3.cx);
                    int num51 = Math.abs(getMyChar().cy - char3.cy);
                    int num52 = ((num50 <= num51) ? num51 : num50);
                    if (num2 <= char3.cx && char3.cx <= num3 && cmy <= char3.cy && char3.cy <= num4 && (charFocus == null || num52 < array[2]))
                    {
                        charFocus = char3;
                        array[2] = num52;
                    }
                }
            }
        }
        else
        {
            if (num2 <= charFocus.cx && charFocus.cx <= num3 && cmy <= charFocus.cy && charFocus.cy <= num4 && charFocus.statusMe != 15 && !charFocus.isInvisible)
            {
                clearFocus(2);
                return;
            }

            charFocus = null;
            for (int num53 = 0; num53 < GameScr.vCharInMap.size(); num53++)
            {
                Char char4 = (Char)GameScr.vCharInMap.elementAt(num53);
                if (!char4.isNhanbanz() && char4.statusMe != 15 && !char4.isInvisible && char4.charID < 0 && wdx == 0 && wdy == 0 && char4.statusMe != 14 && char4.statusMe != 5)
                {
                    int num54 = Math.abs(getMyChar().cx - char4.cx);
                    int num55 = Math.abs(getMyChar().cy - char4.cy);
                    int num56 = ((num54 <= num55) ? num55 : num54);
                    if (num2 <= char4.cx && char4.cx <= num3 && cmy <= char4.cy && char4.cy <= num4 && (charFocus == null || num56 < array[2]))
                    {
                        charFocus = char4;
                        array[2] = num56;
                    }
                }
            }
        }

        int num57 = -1;
        for (int num58 = 0; num58 < array.Length; num58++)
        {
            if (num57 == -1)
            {
                if (array[num58] != -1)
                {
                    num57 = num58;
                }
            }
            else if (array[num58] < array[num57] && array[num58] != -1)
            {
                num57 = num58;
            }
        }

        // Resolve the old focus first so it cannot clear a target chosen by item auto-movement.
        clearFocus(num57);
        if (GameScr.isUseitemAuto && GameScr.auto == 1 && !GameScr.gI().lockAutoMove)
        {
            GameScr.updateAutoMovetoMob();
        }
    }

    public void clearFocus(int index)
    {
        switch (index)
        {
            case 0:
                deFocusNPC();
                charFocus = null;
                itemFocus = null;
                break;
            case 1:
                mobFocus = null;
                charFocus = null;
                itemFocus = null;
                break;
            case 2:
                mobFocus = null;
                deFocusNPC();
                itemFocus = null;
                break;
            case 3:
                mobFocus = null;
                deFocusNPC();
                charFocus = null;
                break;
        }
    }
}
