using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public bool isInWaypoint()
    {
        int num = TileMap.vGo.size();
        for (sbyte b = 0; b < num; b = (sbyte)(b + 1))
        {
            Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(b);
            if (cx >= waypoint.minX && cx <= waypoint.maxX && cy >= waypoint.minY && cy <= waypoint.maxY)
            {
                return true;
            }
        }

        return false;
    }

    public void sendMove()
    {
        if (me && cHP > 0)
        {
            int distance = getDistance(cxSend, cx);
            int distance2 = getDistance(cySend, cy);
            if ((distance > 0 || distance2 > 0) && mSystem.currentTimeMillis() - timeSendmove >= 250 || Handle.isHackGiay)
            {
                isSendMove = true;
            }

            if (isSendMove)
            {
                isSendMove = false;
                Service.gI().charMove("--1:", cx, cy);
                timeSendmove = mSystem.currentTimeMillis();
                cxSend = cx;
                cySend = cy;
                cdirSend = cdir;
                cactFirst = statusMe;
            }
        }
    }

    public void fixMove()
    {
        if (me)
        {
            return;
        }

        if (cf == 12 && cX0 == cx && cY0 == cy)
        {
            inAir++;
        }
        else if (cf == 0 && (TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) != TileMap.T_TOP)
        {
            inAir++;
        }
        else
        {
            inAir = 0;
        }

        if (inAir <= 1)
        {
            return;
        }

        for (int i = cy; i < cy + 150; i += 24)
        {
            if ((TileMap.tileTypeAtPixel(cx, i) & TileMap.T_TOP) == TileMap.T_TOP)
            {
                continue;
            }

            int num = TileMap.tileYofPixel(i) + 24;
            if (num - cy > 24)
            {
                cy += (num - cy) / 2;
                if (!isMoto && !isWolf && !isNewMount)
                {
                    cf = 12;
                }

                vMovePoints.removeAllElements();
                currentMovePoint = null;
            }
            else
            {
                statusMe = 1;
                vMovePoints.removeAllElements();
                currentMovePoint = null;
                cvx = 0;
                cvy = 0;
                cp1 = 0;
                cp2 = 0;
                cp3 = 0;
                inAir = 0;
                cxMoveLast = 0;
                cyMoveLast = 0;
                cy = num;
            }

            lcy = cy;
            break;
        }
    }

    public void updateCharAutoJump()
    {
        cx += cvx * cdir;
        cy += cvyJump;
        cvyJump++;
        if (cp1 == 0)
        {
            cf = 7;
        }
        else
        {
            cf = 23;
        }

        if (canJumpHigh)
        {
            if (cvyJump == -3)
            {
                cf = 8;
            }
            else if (cvyJump == -2)
            {
                cf = 9;
            }
            else if (cvyJump == -1)
            {
                cf = 10;
            }
            else if (cvyJump == 0)
            {
                cf = 11;
            }
        }

        if (cvyJump == 0)
        {
            statusMe = 6;
            ((MovePoint)vMovePoints.firstElement()).status = 4;
            isJump = true;
            cp1 = 0;
            cvy = 1;
        }
    }

    public void setAutoJump()
    {
        int num = ((MovePoint)vMovePoints.firstElement()).xEnd - cx;
        if (canJumpHigh)
        {
            cvyJump = -10;
        }
        else
        {
            cvyJump = -8;
        }

        cp1 = 0;
        cdir = ((num > 0) ? 1 : (-1));
        if (num <= 6)
        {
            cvx = 0;
        }
        else if (num <= 20)
        {
            cvx = 3;
        }
        else
        {
            cvx = 5;
        }
    }

    // Offline input deviation: original client updateCharRun decays cvx by one per tick.
    // Only a released manual ground control requests this; auto paths and airborne motion keep their rules.
    public void StopGroundMovementOnRelease()
    {
        if (!NsoGameControl.InstantMovementStop || !me || statusMe != 2 || currentMovePoint != null)
            return;
        if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23] || GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
            return;
        cvx = 0;
        cBonusSpeed = 0;
        changeStatusStand();
    }

    public void updateCharJump()
    {
        if (GameScr.auto == 1)
        {
            if (cdir == 1)
            {
                cvx = 5;
            }
            else
            {
                cvx = -5;
            }
        }

        if (isHaveNewMount())
        {
            isNewMount = true;
            isMotoBehind = false;
        }

        if (isHaveMoto())
        {
            isMoto = true;
            isMotoBehind = false;
        }

        cx += cvx;
        cy += cvy;
        if (cy < 0)
        {
            cy = 0;
            cvy = -1;
        }

        cvy++;
        if (!me && currentMovePoint != null)
        {
            int num = currentMovePoint.xEnd - cx;
            if (num > 0)
            {
                if (cvx > num)
                {
                    cvx = num;
                }

                if (cvx < 0)
                {
                    cvx = num;
                }
            }
            else if (num < 0)
            {
                if (cvx < num)
                {
                    cvx = num;
                }

                if (cvx > 0)
                {
                    cvx = num;
                }
            }
            else
            {
                cvx = num;
            }
        }

        if (cp1 == 0)
        {
            cf = 7;
        }
        else
        {
            cf = 23;
        }

        if (canJumpHigh)
        {
            if (cvy == -3)
            {
                cf = 8;
            }
            else if (cvy == -2)
            {
                cf = 9;
            }
            else if (cvy == -1)
            {
                cf = 10;
            }
            else if (cvy == 0)
            {
                cf = 11;
            }
        }

        if (cdir == 1)
        {
            if ((TileMap.tileTypeAtPixel(cx + chw, cy - 1) & TileMap.T_LEFT) == TileMap.T_LEFT && cx <= TileMap.tileXofPixel(cx + chw) + 12)
            {
                cx = TileMap.tileXofPixel(cx + chw) - chw;
                cvx = 0;
            }
        }
        else if ((TileMap.tileTypeAtPixel(cx - chw, cy - 1) & TileMap.T_RIGHT) == TileMap.T_RIGHT && cx >= TileMap.tileXofPixel(cx - chw) + 12)
        {
            cx = TileMap.tileXofPixel(cx + 24 - chw) + chw;
            cvx = 0;
        }

        if (cvy == 0)
        {
            if (!isAttFly)
            {
                if (me)
                {
                    setCharFallFromJump();
                }
                else
                {
                    stop();
                }
            }
            else
            {
                setCharFallFromJump();
            }
        }

        if (me && !ischangingMap && isInWaypoint())
        {
            InfoDlg.hide();
            isSendMove = true;
            Service.gI().requestChangeMap();
            isLockKey = true;
            ischangingMap = true;
            GameCanvas.clearKeyHold();
            GameCanvas.clearKeyPressed();
            return;
        }

        if (cp3 < 0)
        {
            cp3++;
        }

        if (cy > ch && TileMap.tileTypeAt(cx, cy - ch, TileMap.T_BOTTOM))
        {
            if (me)
            {
                statusMe = 4;
                cp1 = 0;
                cp2 = 0;
                cvy = 1;
            }
            else
            {
                stop();
            }
        }

        if (!me && currentMovePoint != null && cy < currentMovePoint.yEnd)
        {
            stop();
        }
    }

    public bool checkInRangeJump(int x1, int xw1, int xmob, int y1, int yh1, int ymob)
    {
        if (xmob > xw1 || xmob < x1 || ymob > y1 || ymob < yh1)
        {
            return false;
        }

        return true;
    }

    public void setCharFallFromJump()
    {
        cyStartFall = cy;
        statusMe = 4;
        cp1 = 0;
        if (canJumpHigh)
        {
            cp2 = 1;
        }
        else
        {
            cp2 = 0;
        }

        cvy = 1;
        if (!me)
        {
        }
    }

    public void updateCharFall()
    {
        if (cy + 4 >= TileMap.pxh)
        {
            changeStatusStand();
            cvx = (cvy = 0);
            return;
        }

        if (cy % 24 == 0 && (TileMap.tileTypeAtPixel(cx, cy) & TileMap.T_TOP) == TileMap.T_TOP)
        {
            if (me)
            {
                cvx = (cvy = 0);
                cp1 = (cp2 = 0);
                changeStatusStand();
                return;
            }

            stop();
            cf = 0;
            GameCanvas.gI().startDust(-1, cx - -8, cy);
            GameCanvas.gI().startDust(1, cx - 8, cy);
        }

        cf = 12;
        cx += cvx;
        if (!me && currentMovePoint != null)
        {
            int num = currentMovePoint.xEnd - cx;
            if (num > 0)
            {
                if (cvx > num)
                {
                    cvx = num;
                }

                if (cvx < 0)
                {
                    cvx = num;
                }
            }
            else if (num < 0)
            {
                if (cvx < num)
                {
                    cvx = num;
                }

                if (cvx > 0)
                {
                    cvx = num;
                }
            }
            else
            {
                cvx = num;
            }
        }

        cy += cvy;
        if (cvy < 20)
        {
            cvy++;
        }

        if (cdir == 1)
        {
            if ((TileMap.tileTypeAtPixel(cx + chw, cy - 1) & TileMap.T_LEFT) == TileMap.T_LEFT && cx <= TileMap.tileXofPixel(cx + chw) + 12)
            {
                cx = TileMap.tileXofPixel(cx + chw) - chw;
                cvx = 0;
            }
        }
        else if ((TileMap.tileTypeAtPixel(cx - chw, cy - 1) & TileMap.T_RIGHT) == TileMap.T_RIGHT && cx >= TileMap.tileXofPixel(cx - chw) + 12)
        {
            cx = TileMap.tileXofPixel(cx + 24 - chw) + chw;
            cvx = 0;
        }

        if (cvy > 4 && (cyStartFall == 0 || cyStartFall <= TileMap.tileYofPixel(cy + 3)) && (TileMap.tileTypeAtPixel(cx, cy + 3) & TileMap.T_TOP) == TileMap.T_TOP)
        {
            if (me)
            {
                cyStartFall = 0;
                cvx = (cvy = 0);
                cp1 = (cp2 = 0);
                cy = TileMap.tileXofPixel(cy + 3);
                changeStatusStand();
                GameCanvas.gI().startDust(-1, cx - -8, cy);
                GameCanvas.gI().startDust(1, cx - 8, cy);
            }
            else
            {
                stop();
                cy = TileMap.tileXofPixel(cy + 3);
                cf = 0;
                GameCanvas.gI().startDust(-1, cx - -8, cy);
                GameCanvas.gI().startDust(1, cx - 8, cy);
            }

            return;
        }

        if (cp2 == 1)
        {
            if (cvy == 3)
            {
                cf = 11;
            }
            else if (cvy == 2)
            {
                cf = 8;
            }
            else if (cvy == 1)
            {
                cf = 9;
            }
            else if (cvy == 0)
            {
                cf = 10;
            }
        }
        else
        {
            cf = 12;
        }

        if (cvy > 6 && TileMap.tileTypeAt(cx, cy, TileMap.T_WATERFLOW) && cy % TileMap.size > 8)
        {
            cy = TileMap.tileYofPixel(cy) + 8;
            statusMe = 10;
            cvx = cdir << 1;
            cvy >>= 2;
            cy = TileMap.tileYofPixel(cy) + 12;
        }

        if (me)
        {
            if (!isAttack)
            {
            }

            return;
        }

        if ((TileMap.tileTypeAtPixel(cx, cy + 1) & TileMap.T_TOP) == TileMap.T_TOP)
        {
            cf = 0;
        }

        if (currentMovePoint != null && cy > currentMovePoint.yEnd)
        {
            stop();
        }
    }

    public static void SetRemove()
    {
        IDictionaryEnumerator enumerator = ALL_PART_EFF.GetEnumerator();
        MyVector myVector = new MyVector();
        while (enumerator.MoveNext())
        {
            string text = enumerator.Key.ToString();
            DataSkillEff dataSkillEff = (DataSkillEff)ALL_PART_EFF.get(text);
            if (mSystem.currentTimeMillis() / 1000 - dataSkillEff.timeRemove > 60)
            {
                myVector.addElement(text);
            }

            for (int i = 0; i < myVector.size(); i++)
            {
                string k = (string)myVector.elementAt(i);
                ALL_PART_EFF.remove(k);
            }
        }
    }

    public static bool Move(int cxSend, int cySend)
    {
        Char @char = getMyChar();
        if (cxSend == -1)
        {
            cxSend = @char.cx;
        }

        if (cySend == -1)
        {
            cySend = @char.cy;
        }

        if (cxSend == @char.cx && cySend == @char.cy)
        {
            return false;
        }

        int num = @char.cx;
        int n = (TileMap.tileTypeAt(cxSend, cySend - 12, 64) ? (TileMap.tileYofPixel(cySend) - 24) : cySend);
        if (cxSend > num)
        {
            do
            {
                Service.gI().charMove(num, TileMap.pMove(num, n));
            }
            while ((num += 50) < cxSend);
        }
        else
        {
            do
            {
                Service.gI().charMove(num, TileMap.pMove(num, n));
            }
            while ((num -= 50) > cxSend);
        }

        Service.gI().charMove(cxSend, cySend);
        @char.cx = (@char.cxSend = cxSend);
        @char.cy = (@char.cySend = cySend);
        return true;
    }

    public bool Move2(int cxSend, int cySend)
    {
        Char @char = getMyChar();
        @char.cx = cxSend;
        @char.cy = cxSend;
        if (cxSend == @char.cx && cySend == @char.cy)
        {
            return false;
        }

        sendMove();
        return true;
    }

    public void moveTo(int toX, int toY)
    {
        int dir = 0;
        int act = 0;
        int num = toX - cx;
        int num2 = toY - cy;
        if (num == 0 && num2 == 0)
        {
            act = 1;
        }
        else if (num2 == 0)
        {
            act = 2;
            if (vMovePoints.size() > 0)
            {
                MovePoint movePoint = null;
                try
                {
                    movePoint = (MovePoint)vMovePoints.lastElement();
                }
                catch (Exception)
                {
                }

                if (movePoint != null && TileMap.tileTypeAt(movePoint.xEnd, movePoint.yEnd, TileMap.T_WATERFLOW) && movePoint.yEnd % TileMap.size > 8)
                {
                    act = 10;
                }
            }

            if (num > 0)
            {
                dir = 1;
            }

            if (num < 0)
            {
                dir = -1;
            }
        }
        else if (num2 != 0)
        {
            if (num2 < 0)
            {
                act = 3;
            }

            if (num2 > 0)
            {
                act = 4;
            }

            if (num < 0)
            {
                dir = -1;
            }

            if (num > 0)
            {
                dir = 1;
            }
        }

        int num3 = 0;
        int num4 = 0;
        num3 = cx + num;
        num4 = cy + num2;
        vMovePoints.addElement(new MovePoint(num3, num4, act, dir));
        statusMe = 6;
    }
}
