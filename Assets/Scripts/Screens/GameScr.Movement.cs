using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    public void doShowCharInMap()
    {
        resetButton();
        if (cLastFocusID > 0)
        {
            indexRow = Char.getIndexChar(cLastFocusID);
        }
        else
        {
            indexRow = 0;
            cLastFocusID = -1;
        }

        isPaintCharInMap = true;
        isLockKey = true;
        int num = ((!Main.isPC) ? indexSize : 0);
        setPopupSize(175, 200 - num);
        right = cmdCloseAll;
    }

    protected void doShowMap()
    {
        if (TileMap.typeMap != TileMap.MAP_DAUTRUONG)
        {
            MapScr.gI().switchToMe();
        }
    }

    public void openUIZone(Message message)
    {
        InfoDlg.hide();
        try
        {
            sbyte b = message.reader().readByte();
            if (b > 0)
            {
                zones = new int[b];
                pts = new int[zones.Length];
                for (int i = 0; i < zones.Length; i++)
                {
                    zones[i] = message.reader().readByte();
                    pts[i] = message.reader().readByte();
                }

                isPaintZone = true;
                indexSelect = TileMap.zoneID;
                int num = ((!Main.isPC) ? indexSize : 0);
                setPopupSize(175, 200 - num);
                left = new Command(mResources.SELECT, 11067);
                center = new Command(string.Empty, 11067);
                right = cmdCloseAll;
            }
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
    }

    public void setCharJumpAtt()
    {
        Char.getMyChar().cvy = ((!Char.getMyChar().canJumpHigh) ? (-8) : (-10));
        Char.getMyChar().statusMe = 3;
        Char.getMyChar().cp1 = 0;
    }

    public void setCharJump(int cvx)
    {
        Char.getMyChar().cvy = ((!Char.getMyChar().canJumpHigh) ? (-8) : (-10));
        Char.getMyChar().cvx = cvx;
        Char.getMyChar().statusMe = 3;
        Char.getMyChar().cp1 = 0;
    }

    public static void LockZone(int milisSeconds = 2000)
    {
        isLockKhu = true;
        lock (objKhu)
        {
            Monitor.Wait(objKhu, milisSeconds);
        }
    }

    public static Char findCharInMap(int charId)
    {
        for (int i = 0; i < vCharInMap.size(); i++)
        {
            Char @char = (Char)vCharInMap.elementAt(i);
            if (@char.charID == charId)
            {
                return @char;
            }
        }

        return null;
    }

    public static BuNhin findBuNhinInMap(int index)
    {
        if (vBuNhin.size() > 0)
        {
            return (BuNhin)vBuNhin.elementAt(index);
        }

        return null;
    }

    private void actTinhLuyenMoveOut()
    {
        Item item = getItemFocus(44);
        arrItemSplit[indexSelect] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    public void actRemoveWaitAcceptFriend(string strFWname)
    {
        for (int i = 0; i < vFriend.size(); i++)
        {
            Friend friend = (Friend)vFriend.elementAt(i);
            if (friend.friendName.Equals(strFWname) && friend.type == 4)
            {
                vFriend.removeElementAt(i);
                break;
            }
        }

        for (int j = 0; j < vFriendWait.size(); j++)
        {
            Friend friend2 = (Friend)vFriendWait.elementAt(j);
            if (friend2.friendName.Equals(strFWname))
            {
                vFriendWait.removeElementAt(j);
                break;
            }
        }
    }

    private void actConvertMoveOut()
    {
        Item item = arrItemConvert[indexSelect];
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        arrItemConvert[indexSelect] = null;
        left = (center = null);
        updateCommandForUI();
    }

    private void actUpPearlMoveOut()
    {
        Item item = getItemFocus(11);
        arrItemUpPeal[indexSelect] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    private void actConfirmRemoveFriend(string strFriendNameDel)
    {
        GameCanvas.startYesNoDlg(mResources.CONFIRM_REMOVE_FRIEND, new Command(mResources.YES, 110771, strFriendNameDel), new Command(mResources.NO, 1));
    }

    private void doOpenUIZone()
    {
        int num = indexSelect;
        resetButton();
        indexItemUse = num;
        Service.gI().openUIZone();
    }

    private void actTradeMoveOut()
    {
        Item item = arrItemTradeMe[indexSelect];
        arrItemTradeMe[indexSelect] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    public static void removeAutoUse(int var0)
    {
        ItemAutoUse.removeElement(var0);
    }

    public static void updateAutoMovetoMob()
    {
        // No per-tick distance arrays: resolve the nearest eligible mob in moveFast itself.
        moveFast();
    }

    public static void moveFast()
    {
        Char c = Char.getMyChar();
        long now = mSystem.currentTimeMillis();
        if (!isUseitemAuto || auto != 1 || c.mobFocus != null || c.npcFocus != null || c.charFocus != null || c.statusMe == 14 || c.statusMe == 5 || c.cHP <= 0 || c.cMP <= 0 || c.itemFocus != null || gI().lockAutoMove || now - lastAutoMove < 2000 || GameCanvas.menu.showMenu || GameCanvas.currentDialog != null || ChatTextField.gI().isShow || gI().isPaintPopup() || gI().isPaintUI())
            return;
        Mob nearest = null;
        long nearestDistance = long.MaxValue;
        for (int i = 0; i < vMob.size(); i++)
        {
            Mob mob = (Mob)vMob.elementAt(i);
            if (mob == null || mob.hp <= 0 || mob.status == 0 || mob.status == 1 || mob.levelBoss == 3)
                continue;
            if (!isAllmap && (System.Math.Abs(mob.xFirst - pointCenterX) > rangeSearch || System.Math.Abs(mob.yFirst - pointCenterY) > rangeSearch))
                continue;
            long dx = mob.xFirst - c.cx, dy = mob.yFirst - c.cy;
            long distance = dx * dx + dy * dy;
            if (distance >= nearestDistance)
                continue;
            nearest = mob;
            nearestDistance = distance;
            index = i;
        }

        if (nearest == null)
            return;
        ServerEffect.addServerEffect(141, c.cx, c.cy, 2);
        c.cx = c.cxSend = nearest.xFirst;
        c.cy = c.cySend = nearest.yFirst;
        c.statusMe = 4;
        c.mobFocus = nearest;
        ServerEffect.addServerEffect(141, c.cx, c.cy, 2);
        Service.gI().sendAttackMobFast(nearest.mobId);
        lastAutoMove = now;
    }

    private void actKhamNgocMoveOut(int i)
    {
        Item item = itemSplit;
        switch (i)
        {
            case 1:
                item = itemUpGrade;
                itemUpGrade = null;
                break;
            case 2:
                item = getItemFocus(47);
                arrItemUpGrade[indexSelect] = null;
                break;
            default:
                itemSplit = null;
                break;
        }

        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }

    private void actGiaoDoMoveOut()
    {
        Item item = getItemFocus(48);
        arrItemSplit[indexSelect] = null;
        Char.getMyChar().arrItemBag[item.indexUI] = item;
        left = (center = null);
        updateCommandForUI();
    }
}
