using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public void findNextFocusByKey()
    {
        if (charFocus != null && charFocus.isNhanbanz())
        {
            charFocus = null;
        }

        if (getMyChar().skillPaint != null || getMyChar().arr != null)
        {
            return;
        }

        int num = 0;
        if (nClass.classId == 0 || nClass.classId == 1 || nClass.classId == 3 || nClass.classId == 5)
        {
            num = 40;
        }

        focus.removeAllElements();
        int num2 = 0;
        int num3 = GameScr.cmx + 10;
        int num4 = GameScr.cmx + GameCanvas.w - 10;
        int num5 = GameScr.cmy + 10;
        int num6 = GameScr.cmy + GameScr.gH;
        if (TileMap.typeMap == TileMap.MAP_CHIENTRUONG || TileMap.mapID == 111)
        {
            if (TileMap.mapID == 98 || TileMap.mapID == 104)
            {
                for (int i = 0; i < GameScr.vCharInMap.size(); i++)
                {
                    Char @char = (Char)GameScr.vCharInMap.elementAt(i);
                    if (!@char.isNhanbanz() && @char.statusMe != 15 && !@char.isInvisible && num3 <= @char.cx && @char.cx <= num4 && num5 <= @char.cy && @char.cy <= num6)
                    {
                        focus.addElement(@char);
                        if (charFocus != null && @char.Equals(charFocus))
                        {
                            num2 = focus.size();
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < GameScr.vCharInMap.size(); j++)
                {
                    Char char2 = (Char)GameScr.vCharInMap.elementAt(j);
                    if (char2.isNhanbanz() || char2.statusMe == 15 || char2.isInvisible || num3 > char2.cx || char2.cx > num4 || num5 > char2.cy || char2.cy > num6)
                    {
                        continue;
                    }

                    if (TileMap.mapID != 111)
                    {
                        if (char2.cTypePk != getMyChar().cTypePk && char2.statusMe != 14 && char2.statusMe != 5)
                        {
                            focus.addElement(char2);
                            if (charFocus != null && char2.Equals(charFocus))
                            {
                                num2 = focus.size();
                            }
                        }
                    }
                    else if (myChar.cTypePk == 0)
                    {
                        focus.addElement(char2);
                        if (charFocus != null && char2.Equals(charFocus))
                        {
                            num2 = focus.size();
                        }
                    }
                    else if (myChar.nClass.classId == 6)
                    {
                        if (myChar.cTypePk == char2.cTypePk)
                        {
                            if (char2.statusMe == 14 || char2.statusMe == 5)
                            {
                                focus.addElement(char2);
                                if (charFocus != null && char2.Equals(charFocus))
                                {
                                    num2 = focus.size();
                                }
                            }
                        }
                        else if ((myChar.cTypePk != PK_PHE1 || char2.cTypePk == PK_PHE2 || char2.cTypePk == PK_PHE3 || char2.cTypePk == PK_PHE4) && (myChar.cTypePk != PK_PHE2 || char2.cTypePk == PK_PHE1 || char2.cTypePk == PK_PHE3 || char2.cTypePk == PK_PHE4) && (myChar.cTypePk != PK_PHE3 || char2.cTypePk == PK_PHE1 || char2.cTypePk == PK_PHE2 || char2.cTypePk == PK_PHE4) && (myChar.cTypePk != PK_PHE4 || char2.cTypePk == PK_PHE1 || char2.cTypePk == PK_PHE2 || char2.cTypePk == PK_PHE3) && char2.statusMe != 14 && char2.statusMe != 5)
                        {
                            focus.addElement(char2);
                            if (charFocus != null && char2.Equals(charFocus))
                            {
                                num2 = focus.size();
                            }
                        }
                    }
                    else if ((myChar.cTypePk != PK_PHE1 || char2.cTypePk == PK_PHE2 || char2.cTypePk == PK_PHE3 || char2.cTypePk == PK_PHE4) && (myChar.cTypePk != PK_PHE2 || char2.cTypePk == PK_PHE1 || char2.cTypePk == PK_PHE3 || char2.cTypePk == PK_PHE4) && (myChar.cTypePk != PK_PHE3 || char2.cTypePk == PK_PHE1 || char2.cTypePk == PK_PHE2 || char2.cTypePk == PK_PHE4) && (myChar.cTypePk != PK_PHE4 || char2.cTypePk == PK_PHE1 || char2.cTypePk == PK_PHE2 || char2.cTypePk == PK_PHE3) && char2.statusMe != 14 && char2.statusMe != 5)
                    {
                        focus.addElement(char2);
                        if (charFocus != null && char2.Equals(charFocus))
                        {
                            num2 = focus.size();
                        }
                    }
                }
            }

            for (int k = 0; k < GameScr.vItemMap.size(); k++)
            {
                ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(k);
                if (num3 <= itemMap.x && itemMap.x <= num4 && num5 <= itemMap.y && itemMap.y <= num6)
                {
                    focus.addElement(itemMap);
                    if (itemFocus != null && itemMap.Equals(itemFocus))
                    {
                        num2 = focus.size();
                    }
                }
            }

            for (int l = 0; l < GameScr.vMob.size(); l++)
            {
                Mob mob = (Mob)GameScr.vMob.elementAt(l);
                if ((mob.templateId != 97 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 98 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 167 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 96 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 99 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 166 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 200 || getMyChar().cTypePk != PK_PHE1) && (mob.templateId != 199 || getMyChar().cTypePk != PK_PHE2) && (mob.templateId != 198 || getMyChar().cTypePk != PK_PHE3) && (mob.templateId != 202 || mob.status != 8 || mob.isPaint()) && mob.status != 1 && mob.status != 0 && num3 <= mob.x && mob.x <= num4 && num5 <= mob.y && mob.y <= num6)
                {
                    focus.addElement(mob);
                    if (mobFocus != null && mob.Equals(mobFocus))
                    {
                        num2 = focus.size();
                    }
                }
            }

            for (int m = 0; m < GameScr.vNpc.size(); m++)
            {
                Npc npc = (Npc)GameScr.vNpc.elementAt(m);
                if (npc.statusMe != 15 && num3 <= npc.cx && npc.cx <= num4 && num5 <= npc.cy && npc.cy <= num6)
                {
                    focus.addElement(npc);
                    if (npcFocus != null && npc.Equals(npcFocus))
                    {
                        num2 = focus.size();
                    }
                }
            }

            if (focus.size() > 0)
            {
                if (num2 >= focus.size())
                {
                    num2 = 0;
                }

                if (focus.elementAt(num2) is Char)
                {
                    mobFocus = null;
                    deFocusNPC();
                    charFocus = (Char)focus.elementAt(num2);
                    itemFocus = null;
                    isManualFocus = true;
                }
                else if (focus.elementAt(num2) is Npc)
                {
                    mobFocus = null;
                    deFocusNPC();
                    npcFocus = (Npc)focus.elementAt(num2);
                    charFocus = null;
                    itemFocus = null;
                    isManualFocus = true;
                }
                else if (focus.elementAt(num2) is Mob)
                {
                    mobFocus = (Mob)focus.elementAt(num2);
                    deFocusNPC();
                    charFocus = null;
                    itemFocus = null;
                    isManualFocus = true;
                }
                else if (focus.elementAt(num2) is ItemMap)
                {
                    mobFocus = null;
                    deFocusNPC();
                    charFocus = null;
                    itemFocus = (ItemMap)focus.elementAt(num2);
                    isManualFocus = true;
                }
            }
            else
            {
                mobFocus = null;
                deFocusNPC();
                charFocus = null;
                itemFocus = null;
                isManualFocus = false;
            }

            return;
        }

        for (int n = 0; n < GameScr.vItemMap.size(); n++)
        {
            ItemMap itemMap2 = (ItemMap)GameScr.vItemMap.elementAt(n);
            if (num3 <= itemMap2.x && itemMap2.x <= num4 && num5 <= itemMap2.y && itemMap2.y <= num6)
            {
                focus.addElement(itemMap2);
                if (itemFocus != null && itemMap2.Equals(itemFocus))
                {
                    num2 = focus.size();
                }
            }
        }

        for (int num7 = 0; num7 < GameScr.vMob.size(); num7++)
        {
            Mob mob2 = (Mob)GameScr.vMob.elementAt(num7);
            if ((mob2.templateId != 97 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 96 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 98 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 167 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 99 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 166 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 200 || getMyChar().cTypePk != PK_PHE1) && (mob2.templateId != 199 || getMyChar().cTypePk != PK_PHE2) && (mob2.templateId != 198 || getMyChar().cTypePk != PK_PHE3) && (mob2.templateId != 202 || mob2.status != 8 || mob2.isPaint()) && mob2.status != 1 && mob2.status != 0 && num3 <= mob2.x && mob2.x <= num4 && num5 <= mob2.y && mob2.y <= num6)
            {
                focus.addElement(mob2);
                if (mobFocus != null && mob2.Equals(mobFocus))
                {
                    num2 = focus.size();
                }
            }
        }

        for (int num8 = 0; num8 < GameScr.vNpc.size(); num8++)
        {
            Npc npc2 = (Npc)GameScr.vNpc.elementAt(num8);
            if (npc2.statusMe != 15 && num3 <= npc2.cx && npc2.cx <= num4 && num5 <= npc2.cy && npc2.cy <= num6)
            {
                focus.addElement(npc2);
                if (npcFocus != null && npc2.Equals(npcFocus))
                {
                    num2 = focus.size();
                }
            }
        }

        for (int num9 = 0; num9 < GameScr.vCharInMap.size(); num9++)
        {
            Char char3 = (Char)GameScr.vCharInMap.elementAt(num9);
            if (!char3.isNhanbanz() && char3.statusMe != 15 && !char3.isInvisible && num3 <= char3.cx && char3.cx <= num4 && num5 <= char3.cy && char3.cy <= num6)
            {
                focus.addElement(char3);
                if (charFocus != null && char3.Equals(charFocus))
                {
                    num2 = focus.size();
                }
            }
        }

        if (focus.size() > 0)
        {
            if (num2 >= focus.size())
            {
                num2 = 0;
            }

            if (focus.elementAt(num2) is Mob)
            {
                mobFocus = (Mob)focus.elementAt(num2);
                deFocusNPC();
                charFocus = null;
                itemFocus = null;
                isManualFocus = true;
            }
            else if (focus.elementAt(num2) is Npc)
            {
                mobFocus = null;
                deFocusNPC();
                npcFocus = (Npc)focus.elementAt(num2);
                charFocus = null;
                itemFocus = null;
                isManualFocus = true;
            }
            else if (focus.elementAt(num2) is Char)
            {
                mobFocus = null;
                deFocusNPC();
                charFocus = (Char)focus.elementAt(num2);
                itemFocus = null;
                isManualFocus = true;
            }
            else if (focus.elementAt(num2) is ItemMap)
            {
                mobFocus = null;
                deFocusNPC();
                charFocus = null;
                itemFocus = (ItemMap)focus.elementAt(num2);
                isManualFocus = true;
            }
        }
        else
        {
            mobFocus = null;
            deFocusNPC();
            charFocus = null;
            itemFocus = null;
            isManualFocus = false;
        }
    }
}
