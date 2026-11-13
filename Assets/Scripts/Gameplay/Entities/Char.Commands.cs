using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public void doInjure(int HPShow, int MPShow, bool isBoss, int idBoss)
    {
        // Offline correction to the Mob.attack(Char, Char) display protocol: -1 means MISS,
        // not negative damage/healing. Keep the original values for the MISS text below;
        // only positive damage may reduce local HP/MP. Recovery uses separate effect packets.
        if (HPShow > 0)
            cHP -= HPShow;
        if (MPShow > 0)
            cMP -= MPShow;
        if (!me)
        {
            cHP = cHpNew;
        }

        if (cHP < 0)
        {
            cHP = 0;
        }

        if (cHP < 1 && statusMe != 14 && statusMe != 5)
        {
            cHP = 1;
        }

        if (HPShow <= 0)
        {
            if (me)
            {
                GameScr.startFlyText(string.Empty, cx, cy - ch, 0, -2, mFont.MISS_ME);
            }
            else
            {
                GameScr.startFlyText(string.Empty, cx, cy - ch, 0, -2, mFont.MISS);
            }
        }
        else
        {
            GameScr.startFlyText("-" + HPShow, cx, cy - ch, 0, -2, mFont.RED);
        }

        if (HPShow > 0)
        {
            isInjure = 4;
        }

        if (isBoss)
        {
            switch (idBoss)
            {
                case 114:
                    ServerEffect.addServerEffect(32, cx, cy - chh, 1);
                    break;
                case 115:
                    ServerEffect.addServerEffect(85, cx, cy, 1);
                    break;
                case 139:
                    GameScr.shaking = 1;
                    GameScr.count = 0;
                    ServerEffect.addServerEffect(91, this, 2);
                    break;
                case 144:
                    ServerEffect.addServerEffect(91, this, 1);
                    break;
            }
        }
        else
        {
            callEff(49);
        }
    }

    public void doInjure()
    {
        isInjure = 4;
        callEff(49);
    }

    public bool doUsePotion(int type)
    {
        if (arrItemBag == null)
        {
            return false;
        }

        for (int i = 0; i < arrItemBag.Length; i++)
        {
            if (arrItemBag[i] != null && arrItemBag[i].template.type == type && arrItemBag[i].template.level <= myChar.clevel)
            {
                Service.gI().useItem(i);
                return true;
            }
        }

        return false;
    }
}
