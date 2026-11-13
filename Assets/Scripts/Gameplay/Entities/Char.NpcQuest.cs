using Functions.Auto;
using System;
using System.Collections;

public partial class Char
{
    public static Npc FindNpc(int var0)
    {
        Char @char = getMyChar();
        MyVector vNpc = GameScr.vNpc;
        int num = -1;
        Npc result = null;
        for (int i = 0; i < vNpc.size(); i++)
        {
            Npc npc;
            if ((npc = (Npc)vNpc.elementAt(i)) != null && var0 == npc.template.npcTemplateId)
            {
                int num2 = pFidnNpc(@char.cx, @char.cy, npc.cx, npc.cy);
                if (num == -1 || num2 < num)
                {
                    num = num2;
                    result = npc;
                }
            }
        }

        return result;
    }

    public static int pFidnNpc(int paramInt1, int paramInt2, int paramInt3, int paramInt4)
    {
        if ((paramInt1 = (paramInt1 - paramInt3) * (paramInt1 - paramInt3) + (paramInt2 - paramInt4) * (paramInt2 - paramInt4)) <= 0)
        {
            return 0;
        }

        paramInt2 = (paramInt1 + 1) / 2;
        do
        {
            paramInt3 = paramInt2;
            paramInt2 = paramInt2 / 2 + paramInt1 / (paramInt2 << 1);
        }
        while (Math.abs(paramInt3 - paramInt2) > 1);
        return paramInt2;
    }

    public static void PickNpc(int npcID, int menuID, int optionID)
    {
        if (mSystem.currentTimeMillis() < 500)
        {
            Handle.Sleep(500 - (int)mSystem.currentTimeMillis());
        }

        Npc npc;
        if ((npc = FindNpc(npcID)) != null)
        {
            Move(npc.cx, npc.cy);
            getMyChar().npcFocus = npc;
            Service.gI().menu(-1, npcID, menuID, optionID);
        }
    }

    public static TaskOrder FindTask(int taskId)
    {
        for (int i = 0; i < getMyChar().taskOrders.size(); i++)
        {
            TaskOrder taskOrder;
            if ((taskOrder = (TaskOrder)getMyChar().taskOrders.elementAt(i)) != null && taskOrder.taskId == taskId)
            {
                return taskOrder;
            }
        }

        return null;
    }

    public void deFocusNPC()
    {
        if (me && npcFocus != null)
        {
            npcFocus.chatPopup = null;
            npcFocus = null;
        }
    }

    public void clearTask()
    {
        getMyChar().callEffTask(21);
        getMyChar().taskMaint = null;
        for (int i = 0; i < getMyChar().arrItemBag.Length; i++)
        {
            if (getMyChar().arrItemBag[i] != null && (getMyChar().arrItemBag[i].template.type == 25 || getMyChar().arrItemBag[i].template.type == 23 || getMyChar().arrItemBag[i].template.type == 24))
            {
                getMyChar().arrItemBag[i] = null;
            }
        }

        Npc.clearEffTask();
    }
}
