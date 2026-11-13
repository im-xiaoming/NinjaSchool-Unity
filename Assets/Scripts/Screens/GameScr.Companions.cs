using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    public void setInfoThanThu()
    {
        indexRowMax = 1;
        wThanThu = popupW;
        hThanThu = popupH;
        xThanThu = popupX;
        yThanThu = popupY;
        xRecThanThu = xThanThu + 25;
        yRecThanThu = yThanThu + 60;
        wRecThanThu = wThanThu - 50;
        hRecThanThu = 70;
        wExpThanThu = wThanThu - 49;
        hExpThanThu = 10;
        xExpThanThu = GameCanvas.hw - wExpThanThu / 2;
        yExpThanThu = yRecThanThu + hRecThanThu - hExpThanThu;
        wSelectThanThu = 18;
        xSelectThanThu = GameCanvas.hw - (clan_thanthu.size() - 1) * ((wSelectThanThu + 5) / 2);
        ySelectThanThu = yThanThu + hThanThu - wSelectThanThu / 2 - 5;
    }

    public void addInfo_ThanThu(Clan_ThanThu thanthu)
    {
        clan_thanthu.addElement(thanthu);
    }

    public void clearVecThanThu()
    {
        clan_thanthu.removeAllElements();
    }

    private void actBijuuSelect()
    {
        MyVector myVector = new MyVector();
        if (indexBijuuMenu == 0)
        {
            myVector.addElement(new Command(mResources.POTENTIAL_POINT, 5021));
            myVector.addElement(new Command(mResources.MENUME[1], 5022));
            myVector.addElement(cmdCloseAll);
        }
        else if (indexBijuuMenu == 1)
        {
            myVector.addElement(new Command(mResources.MENUME[indexMenu], 502));
            myVector.addElement(new Command(mResources.MENUME[1], 5022));
            myVector.addElement(cmdCloseAll);
        }
        else if (indexBijuuMenu == 2)
        {
            myVector.addElement(new Command(mResources.MENUME[indexMenu], 502));
            myVector.addElement(new Command(mResources.POTENTIAL_POINT, 5021));
            myVector.addElement(cmdCloseAll);
        }

        GameCanvas.menu.startAt(myVector, 3);
    }

    private void actBijuuUpPoint()
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

        if (num <= 0)
        {
            GameCanvas.inputDlg.hide();
            return;
        }

        if (Char.getMyChar().bijuuPoint == 0 || num > Char.getMyChar().bijuuPoint)
        {
            GameCanvas.startOKDlg(mResources.NOT_ENOUGH_POINT);
            return;
        }

        Service.gI().upPotential(indexTitle - 1, num);
        GameCanvas.inputDlg.hide();
    }

    private void actBijuuKyNang()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(new Command(mResources.UPPOINT, 5051));
        myVector.addElement(new Command(mResources.UPPOINTS, 5052));
        GameCanvas.menu.startAt(myVector, 0);
    }
}
