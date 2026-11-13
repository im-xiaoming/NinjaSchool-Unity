using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public sbyte getFrameHores()
    {
        DataSkillEff partEff = getPartEff(ID_HORSE, true);
        if (partEff == null || !partEff.isLoadData)
        {
            return 0;
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

        if (fho >= ActionHorse.Length)
        {
            return (sbyte)(ActionHorse[ActionHorse.Length - 1] + FrameHorse * partEff.nHorseFrame);
        }

        return (sbyte)(ActionHorse[fho] + FrameHorse * partEff.nHorseFrame);
    }

    public int getFrameHorseui(int fr)
    {
        return fr + fHorseUI * 30;
    }

    public int getFrameBienHinh()
    {
        if (isMoto || isWolf || isNewMount || ID_HORSE > -1)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FrameBienHinh * 30;
            }

            return fRun_PP[fBienhinh] + FrameBienHinh * 30;
        }

        return cf + FrameBienHinh * 30;
    }

    public int getFrameLeg()
    {
        if (isMoto || isWolf || isNewMount || ID_HORSE > -1)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FrameLeg * 30;
            }

            return fRun_PP[fLeg] + FrameLeg * 30;
        }

        return cf + FrameLeg * 30;
    }

    public int getFrameMatna()
    {
        if (isMoto || isWolf || isNewMount)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FrameMatna * 30;
            }

            return fRun_PP[fMatNa] + FrameMatna * 30;
        }

        return cf + FrameMatna * 30;
    }

    public int getFrameWeapone()
    {
        if (isMoto || isWolf || isNewMount)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FrameWeaPone * 30;
            }

            return fRun_PP[fWeapone] + FrameWeaPone * 30;
        }

        return cf + FrameWeaPone * 30;
    }

    public int getFrameBody()
    {
        if (isMoto || isWolf || isNewMount)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FrameBody * 30;
            }

            return fRun_PP[fBody] + FrameBody * 30;
        }

        return cf + FrameBody * 30;
    }

    public int getFrameHair()
    {
        if (isMoto || isWolf || isNewMount)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FrameHair * 30;
            }

            return fRun_PP[fHair] + FrameHair * 30;
        }

        return cf + FrameHair * 30;
    }

    public int getFramePP()
    {
        if (isMoto || isWolf || isNewMount || ID_HORSE > -1)
        {
            if (statusMe == 1 || statusMe == 6)
            {
                return cf + FramePP * 30;
            }

            return fRun_PP[fPP] + FramePP * 30;
        }

        return cf + FramePP * 30;
    }

    public int getFramePP(int fr)
    {
        return fr + fPPUI * 30;
    }

    public int getFrameHeadUI(int fr)
    {
        return fr + fHeadUI * 30;
    }

    public int getFrameBodyUI(int fr)
    {
        return fr + FbodyUI * 30;
    }

    public int getFrameLegUI(int fr)
    {
        return fr + FlegUI * 30;
    }

    public int getFrameWeaponUI(int fr)
    {
        return fr + fWeaponUI * 30;
    }

    public int getFrameMatNaUI(int fr)
    {
        return fr + fMatNaUI * 30;
    }

    public bool isoldHorse()
    {
        if (arrItemMounts[4] == null)
        {
            return false;
        }

        if (arrItemMounts[4].template.id == 485 || arrItemMounts[4].template.id == 524 || arrItemMounts[4].template.id == 523 || arrItemMounts[4].template.id == 443)
        {
            return true;
        }

        return false;
    }

    public int getDyHorse()
    {
        if (ID_HORSE > -1)
        {
            DataSkillEff partEff = getPartEff(ID_HORSE, true);
            if (partEff != null && partEff.isLoadData)
            {
                return partEff.dyHorse + partEff.Dy_Char[getFrameHores()];
            }
        }

        if (isWolf || isMoto || isNewMount)
        {
            return 12;
        }

        return 0;
    }

    private int getLegId()
    {
        CharPartInfo charPartInfo = (CharPartInfo)CharPartInfo.head_jump.get(leg + string.Empty);
        if (charPartInfo != null)
        {
            return charPartInfo.idSmall;
        }

        switch (leg)
        {
            case 0:
                return 26;
            case 4:
                return 58;
            case 6:
                return 86;
            case 8:
                return 114;
            case 9:
                return 123;
            case 17:
                return 353;
            case 19:
                return 379;
            case 21:
                return 405;
            case 30:
                return 484;
            case 33:
                return 518;
            case 35:
                return 544;
            case 37:
                return 571;
            case 39:
                return 810;
            case 43:
                return 982;
            case 95:
                return 1156;
            case 142:
                return 1360;
            case 155:
                return 1494;
            case 157:
                return 1519;
            default:
                return 26;
        }
    }

    private int getHeadId()
    {
        dxHead = (dyHead = 0);
        Part part = GameScr.parts[head];
        CharPartInfo charPartInfo = null;
        if (statusMe == 3)
        {
            charPartInfo = (CharPartInfo)CharPartInfo.head_jump.get(head + string.Empty);
            if (charPartInfo != null)
            {
                CharPartInfo charPartInfo2 = (CharPartInfo)charPartInfo.item.get(arrItemMounts[4].template.id + string.Empty);
                if (charPartInfo2 != null)
                {
                    dxHead = charPartInfo2.dx;
                    dyHead = charPartInfo2.dy;
                    return (part == null) ? charPartInfo.idSmall : part.pi[frameFix].id;
                }
            }

            switch (head)
            {
                case 2:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 33;
                case 11:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 147;
                case 23:
                    dxHead = 1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 427;
                case 24:
                    dxHead = 1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 430;
                case 25:
                    dxHead = 3;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 433;
                case 26:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 436;
                case 27:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 439;
                case 28:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 442;
                case 112:
                    dxHead = 1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 148;
                case 113:
                    dxHead = -1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 443;
                case 124:
                    dxHead = 1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1235;
                case 125:
                    dxHead = -1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1237;
                case 126:
                    dxHead = -1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1255;
                case 127:
                    dxHead = -1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1257;
                case 137:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1309;
                case 138:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1311;
                case 139:
                    dxHead = 2;
                    dyHead = -5;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1315;
                case 140:
                    dxHead = 3;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1313;
                case 146:
                    dxHead = 1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1416;
                case 147:
                    dxHead = -2;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1418;
                case 148:
                    dxHead = 0;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1422;
                case 149:
                    dxHead = -2;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1424;
                case 150:
                    dxHead = 0;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1441;
                case 151:
                    dxHead = -2;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1439;
                case 152:
                    dxHead = 1;
                    dyHead = -4;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1447;
                case 153:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1445;
                case 158:
                    dxHead = -2;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1585;
                case 159:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1589;
                case 160:
                    dxHead = 2;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1587;
                case 161:
                    dxHead = 3;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1595;
                case 162:
                    dxHead = -5;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1597;
                case 163:
                    dxHead = -3;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1604;
                case 179:
                    dxHead = 3;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 1978;
                case 182:
                    dxHead = 3;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2006;
                case 185:
                    dxHead = -4;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2129;
                case 188:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2156;
                case 205:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2451;
                case 210:
                    dxHead = 0;
                    dyHead = -5;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2519;
                case 211:
                    dxHead = -1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2521;
                case 212:
                    dxHead = -2;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2523;
                case 213:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2525;
                case 214:
                    dxHead = 1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 2526;
                default:
                    dxHead = 2;
                    dyHead = -5;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dxHead -= 2;
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 7;
                            dyHead--;
                        }
                    }

                    return 33;
            }
        }

        if (!isBocdau)
        {
            charPartInfo = (CharPartInfo)CharPartInfo.head_normal.get(head + string.Empty);
            if (charPartInfo != null)
            {
                CharPartInfo charPartInfo3 = (CharPartInfo)charPartInfo.item.get(arrItemMounts[4].template.id + string.Empty);
                if (charPartInfo3 != null)
                {
                    dxHead = charPartInfo3.dx;
                    dyHead = charPartInfo3.dy;
                    return (part == null) ? charPartInfo.idSmall : part.pi[frameFix].id;
                }
            }

            switch (head)
            {
                case 2:
                    dxHead = -1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 33;
                case 11:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 147;
                case 23:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 427;
                case 24:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 430;
                case 25:
                    dxHead = 1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 433;
                case 26:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 436;
                case 27:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 439;
                case 28:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 442;
                case 112:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 148;
                case 113:
                    dxHead = -3;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 443;
                case 124:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1235;
                case 125:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1237;
                case 126:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1255;
                case 127:
                    dxHead = -3;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1257;
                case 137:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1309;
                case 138:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1311;
                case 139:
                    dxHead = 0;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1315;
                case 140:
                    dxHead = 1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1313;
                case 146:
                    dxHead = -1;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1416;
                case 147:
                    dxHead = -4;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1418;
                case 148:
                    dxHead = -2;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1422;
                case 149:
                    dxHead = -4;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1424;
                case 150:
                    dxHead = -2;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1441;
                case 151:
                    dxHead = -4;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1439;
                case 152:
                    dxHead = -1;
                    dyHead = -2;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1447;
                case 153:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1445;
                case 158:
                    dxHead = -4;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1585;
                case 159:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1589;
                case 160:
                    dxHead = 0;
                    dyHead = 0;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1587;
                case 161:
                    dxHead = 1;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1595;
                case 162:
                    dxHead = -7;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1597;
                case 163:
                    dxHead = -5;
                    dyHead = 0;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1604;
                case 179:
                    dxHead = 1;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 1978;
                case 182:
                    dxHead = 1;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2006;
                case 185:
                    dxHead = -6;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2129;
                case 188:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2156;
                case 205:
                    dxHead = -2;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2451;
                case 210:
                    dxHead = -2;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2519;
                case 211:
                    dxHead = -3;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2521;
                case 212:
                    dxHead = -4;
                    dyHead = 0;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2523;
                case 213:
                    dxHead = 0;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2525;
                case 214:
                    dxHead = -1;
                    dyHead = -1;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 2526;
                default:
                    dxHead = -1;
                    dyHead = -3;
                    if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                    {
                        if (arrItemMounts[4].template.id == 485)
                        {
                            dyHead++;
                        }
                        else if (arrItemMounts[4].template.id == 524)
                        {
                            dxHead -= 5;
                            dyHead--;
                        }
                    }

                    return 33;
            }
        }

        charPartInfo = (CharPartInfo)CharPartInfo.head_boc_dau.get(head + string.Empty);
        if (charPartInfo != null)
        {
            CharPartInfo charPartInfo4 = (CharPartInfo)charPartInfo.item.get(arrItemMounts[4].template.id + string.Empty);
            if (charPartInfo4 != null)
            {
                dxHead = charPartInfo4.dx;
                dyHead = charPartInfo4.dy;
                return (part == null) ? charPartInfo.idSmall : part.pi[frameFix].id;
            }
        }

        switch (head)
        {
            case 2:
                dxHead = 2;
                dyHead = -5;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 33;
            case 11:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 147;
            case 23:
                dxHead = 1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 427;
            case 24:
                dxHead = 1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 430;
            case 25:
                dxHead = 3;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 433;
            case 26:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 436;
            case 27:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 439;
            case 28:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 442;
            case 112:
                dxHead = 1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 148;
            case 113:
                dxHead = -1;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 443;
            case 124:
                dxHead = 1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1235;
            case 125:
                dxHead = -1;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1237;
            case 126:
                dxHead = -1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1255;
            case 127:
                dxHead = -1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1257;
            case 137:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1309;
            case 138:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1311;
            case 139:
                dxHead = 2;
                dyHead = -5;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1315;
            case 140:
                dxHead = 3;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1313;
            case 146:
                dxHead = 1;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1416;
            case 147:
                dxHead = -2;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1418;
            case 148:
                dxHead = 0;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1422;
            case 149:
                dxHead = -2;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1424;
            case 150:
                dxHead = 0;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1441;
            case 151:
                dxHead = -2;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1439;
            case 152:
                dxHead = 1;
                dyHead = -4;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1447;
            case 153:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1445;
            case 158:
                dxHead = -2;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1585;
            case 159:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1589;
            case 160:
                dxHead = 2;
                dyHead = -2;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1587;
            case 161:
                dxHead = 3;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1595;
            case 162:
                dxHead = -5;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1597;
            case 163:
                dxHead = -3;
                dyHead = -2;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1604;
            case 179:
                dxHead = 3;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 1978;
            case 182:
                dxHead = 3;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2006;
            case 185:
                dxHead = -4;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2129;
            case 188:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2156;
            case 205:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2451;
            case 210:
                dxHead = 0;
                dyHead = -5;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2519;
            case 211:
                dxHead = -1;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2521;
            case 212:
                dxHead = -2;
                dyHead = -2;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2523;
            case 213:
                dxHead = 0;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2525;
            case 214:
                dxHead = 1;
                dyHead = -3;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 2526;
            default:
                dxHead = 2;
                dyHead = -5;
                if (arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523)
                {
                    if (arrItemMounts[4].template.id == 485)
                    {
                        dxHead -= 2;
                        dyHead++;
                    }
                    else if (arrItemMounts[4].template.id == 524)
                    {
                        dxHead -= 7;
                        dyHead--;
                    }
                }

                return 33;
        }
    }

    public void GetNewMount()
    {
        frameMount = (int[][])CharPartInfo.newMount.get(arrItemMounts[4].template.id + string.Empty);
    }

    public bool isHaveNewMount()
    {
        if (ID_HORSE > -1)
        {
            return false;
        }

        if (arrItemMounts != null && arrItemMounts[4] != null && arrItemMounts[4].template != null && arrItemMounts[4].template.id != 443 && arrItemMounts[4].template.id != 523 && arrItemMounts[4].template.id != 485 && arrItemMounts[4].template.id != 524)
        {
            return true;
        }

        return false;
    }

    public void updNewMount()
    {
        if (ID_HORSE > -1 || !isNewMount || frameMount == null)
        {
            return;
        }

        tickNewMount++;
        if (isMotoBehind)
        {
            fNewMount = 5;
        }
        else if (statusMe == 1 || statusMe == 6)
        {
            fNewMount = 0;
            tickNewMount = ((GameCanvas.gameTick % 20 > 12) ? 1 : 0);
            if (ID_PP > -1)
            {
                tickNewMount = cf;
            }

            dyMount = tickNewMount;
        }
        else if (statusMe == 2 || statusMe == 10)
        {
            fNewMount = 1;
            int num = frameMount[fNewMount].Length / 3;
            if (tickNewMount < num)
            {
                dyMount = 0;
            }
            else if (tickNewMount < num * 2)
            {
                dyMount = 1;
            }
            else
            {
                dyMount = 2;
            }
        }
        else if (statusMe == 3)
        {
            fNewMount = 2;
        }
        else if (statusMe == 4)
        {
            fNewMount = 3;
        }
        else if (statusMe == 14)
        {
            fNewMount = 4;
        }

        if (tickNewMount > frameMount[fNewMount].Length - 1)
        {
            tickNewMount = 0;
        }
    }

    public override void setThoiTrang(short[] arr)
    {
        if (arr != null)
        {
            ID_HAIR = arr[0];
            ID_Body = arr[1];
            ID_LEG = arr[2];
            ID_WEA_PONE = arr[3];
            ID_PP = arr[4];
            ID_NAME = arr[5];
            ID_HORSE = arr[6];
            ID_RANK = arr[7];
            ID_MAT_NA = arr[8];
            ID_Bien_Hinh = arr[9];
            ColorName = 0;
            if (ID_NAME > 3000)
            {
                ColorName = (sbyte)(ID_NAME - 3000);
                ID_NAME = -1;
            }
        }
    }
}
