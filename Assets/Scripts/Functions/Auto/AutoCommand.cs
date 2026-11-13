using System.Threading;
using UnityEngine;

namespace Functions.Auto
{
    public class AutoCommand : IActionListener
    {
        private static AutoCommand select;
        public static AutoCommand gI()
        {
            if (select == null)
            {
                select = new AutoCommand();
            }

            return select;
        }

        public void perform(int idAction, object p)
        {
            if (idAction == 1)
            {
                string text = " ";
                MyVector myVector;
                (myVector = new MyVector()).addElement(new Command("Tàn sát all", gI(), 2, 0));
                for (int i = 0; i < GameScr.vMob.size(); i++)
                {
                    Mob mob = (Mob)GameScr.vMob.elementAt(i);
                    if (!mob.isBoss && !text.Contains(mob.getTemplate().name))
                    {
                        myVector.addElement(new Command(mob.getTemplate().name, gI(), 3, mob));
                        text += mob.getTemplate().name;
                    }
                }

                GameCanvas.menu.startAt(myVector, 1);
            }

            switch (idAction)
            {
                case 2:
                    Handle.TS(-1);
                    return;
                case 3:
                    Handle.TS(((Mob)p).getTemplate().mobTemplateId);
                    return;
                case 4:
                    Handle.Abort();
                    return;
                case 5:
                {
                    string text2 = " ";
                    MyVector myVector2 = new MyVector();
                    for (int j = 0; j < GameScr.vNpc.size(); j++)
                    {
                        Npc npc = (Npc)GameScr.vNpc.elementAt(j);
                        if (!text2.Contains(npc.template.name))
                        {
                            myVector2.addElement(new Command(npc.template.name, gI(), 6, npc));
                            text2 += npc.template.name;
                        }
                    }

                    GameCanvas.menu.startAt(myVector2, 0);
                    break;
                }
            }

            Npc npc2;
            if (idAction == 6 && (npc2 = (Npc)p) != null)
            {
                if (Math.abs(npc2.cx - Char.getMyChar().cx) > 22)
                {
                    Char.Move(npc2.cx, npc2.cy);
                }

                Service.gI().openMenu(npc2.template.npcTemplateId);
            }

            switch (idAction)
            {
                case 8:
                    GameScr.GiuLV = !GameScr.GiuLV;
                    Handle.Paint("Giữ Level: " + (GameScr.GiuLV ? "Bật" : "Tắt"));
                    return;
                case 9:
                    GameScr.HoiSinhLuong = !GameScr.HoiSinhLuong;
                    Handle.Paint("HS lượng: " + (GameScr.HoiSinhLuong ? "Bật" : "Tắt"));
                    return;
                case 10:
                    GameCanvas.inputDlg.show("Độ trễ", new Command("Đặt", gI(), 11, 0), TField.INPUT_TYPE_NUMERIC);
                    return;
                case 11:
                    Time.timeScale = int.Parse(GameCanvas.inputDlg.tfInput.getText());
                    GameCanvas.endDlg();
                    GameCanvas.startOKDlg("Tốc độ game: " + Time.timeScale);
                    return;
                case 12:
                    GameCanvas.inputDlg.show("Tốc độ NextMap (milisecond)", new Command("Đặt", gI(), 13, 0), TField.INPUT_TYPE_NUMERIC);
                    return;
                case 13:
                    Handle.SpNextMap = int.Parse(GameCanvas.inputDlg.tfInput.getText());
                    GameCanvas.endDlg();
                    GameCanvas.startOKDlg("Tốc độ NextMap: " + Handle.SpNextMap + " milisecond");
                    return;
                case 14:
                    MenuKhac();
                    return;
                case 15:
                    FakeSkill();
                    return;
                case 16:
                    GameCanvas.inputDlg.show("Tên nhân vật", new Command(mResources.OK, gI(), 46, 0), TField.INPUT_TYPE_ANY);
                    return;
                case 46:
                    Char.getMyChar().cName = GameCanvas.inputDlg.tfInput.getText();
                    GameCanvas.endDlg();
                    Handle.Paint("Ingame của bạn là: " + Char.getMyChar().cName);
                    return;
                case 17:
                    GameCanvas.inputDlg.show("Level", new Command(mResources.OK, gI(), 47, 0), TField.INPUT_TYPE_NUMERIC);
                    return;
                case 47:
                    Char.getMyChar().clevel = int.Parse(GameCanvas.inputDlg.tfInput.getText());
                    GameCanvas.endDlg();
                    GameCanvas.startOKDlg("Level của bạn là: " + Char.getMyChar().clevel);
                    return;
                case 18:
                    GameCanvas.inputDlg.show("Yên", new Command(mResources.OK, gI(), 48, 0), TField.INPUT_TYPE_NUMERIC);
                    return;
                case 48:
                    Char.getMyChar().yen = int.Parse(GameCanvas.inputDlg.tfInput.getText());
                    GameCanvas.endDlg();
                    GameCanvas.startOKDlg("Yên của bạn là: " + Char.getMyChar().yen);
                    return;
                case 19:
                    GameCanvas.inputDlg.show("Xu", new Command(mResources.OK, gI(), 49, 0), TField.INPUT_TYPE_NUMERIC);
                    return;
                case 49:
                    Char.getMyChar().xu = int.Parse(GameCanvas.inputDlg.tfInput.getText());
                    GameCanvas.endDlg();
                    GameCanvas.startOKDlg("Xu của bạn là: " + Char.getMyChar().xu);
                    return;
                case 20:
                    GameCanvas.inputDlg.show("Lượng", new Command(mResources.OK, gI(), 50, 0), TField.INPUT_TYPE_NUMERIC);
                    return;
                case 50:
                    Char.getMyChar().luong = int.Parse(GameCanvas.inputDlg.tfInput.getText());
                    GameCanvas.endDlg();
                    GameCanvas.startOKDlg("Lượng của bạn là: " + Char.getMyChar().luong);
                    return;
                case 22:
                    ClassTieu();
                    return;
                case 23:
                    ClassKiem();
                    return;
                case 24:
                    ClassQuat();
                    return;
                case 25:
                    ClassDao();
                    return;
                case 26:
                    ClassKunai();
                    return;
                case 27:
                    ClassCung();
                    return;
                case 28:
                    Handle.Paint("Fake skill Tiêu 7x");
                    Char.getMyChar().myskill.template.id = 62;
                    return;
                case 29:
                    Handle.Paint("Fake skill Tiêu 8x");
                    Char.getMyChar().myskill.template.id = 78;
                    return;
                case 30:
                    Handle.Paint("Fake skill Tiêu 10x");
                    Char.getMyChar().myskill.template.id = 83;
                    return;
                case 31:
                    Handle.Paint("Fake skill Kiếm 7x");
                    Char.getMyChar().myskill.template.id = 61;
                    return;
                case 32:
                    Handle.Paint("Fake skill Kiếm 8x");
                    Char.getMyChar().myskill.template.id = 73;
                    return;
                case 33:
                    Handle.Paint("Fake skill Kiếm 10x");
                    Char.getMyChar().myskill.template.id = 79;
                    return;
                case 34:
                    Handle.Paint("Fake skill Quạt 7x");
                    Char.getMyChar().myskill.template.id = 66;
                    return;
                case 35:
                    Handle.Paint("Fake skill Quạt 8x");
                    Char.getMyChar().myskill.template.id = 77;
                    return;
                case 36:
                    Handle.Paint("Fake skill Quạt 10x");
                    Char.getMyChar().myskill.template.id = 84;
                    return;
                case 37:
                    Handle.Paint("Fake skill Đao 7x");
                    Char.getMyChar().myskill.template.id = 65;
                    return;
                case 38:
                    Handle.Paint("Fake skill Đao 8x");
                    Char.getMyChar().myskill.template.id = 74;
                    break;
                case 57:
                    GameScr.gI().right = GameScr.gI().cmdCloseAll;
                    GameScr.indexMenu = 0;
                    GameScr.isPaintItemDell = true;
                    GameScr.setPopupSize(175, 200);
                    return;
                case 1100074:
                    Handle.Paint("Auto nv ");
                    Handle.ATNVHN();
                    return;
            }

            switch (idAction)
            {
                case 39:
                    Handle.Paint("Fake skill Đao 10x");
                    Char.getMyChar().myskill.template.id = 80;
                    break;
                case 40:
                    Handle.Paint("Fake skill Kunai 7x");
                    Char.getMyChar().myskill.template.id = 63;
                    break;
                case 41:
                    Handle.Paint("Fake skill Kunai 8x");
                    Char.getMyChar().myskill.template.id = 85;
                    break;
                case 42:
                    Handle.Paint("Fake skill Kunai 10x");
                    Char.getMyChar().myskill.template.id = 81;
                    break;
                case 43:
                    Handle.Paint("Fake skill Cung 7x");
                    Char.getMyChar().myskill.template.id = 64;
                    break;
                case 44:
                    Handle.Paint("Fake skill Cung 8x");
                    Char.getMyChar().myskill.template.id = 76;
                    break;
                case 45:
                    Handle.Paint("Fake skill Cung 10x");
                    Char.getMyChar().myskill.template.id = 82;
                    break;
                case 51:
                    GameScr.HutVatPham = !GameScr.HutVatPham;
                    Handle.Paint(GameScr.HutVatPham ? "Bật hút VP" : "Bật nhặt xa");
                    break;
                case 52:
                    GameScr.CheDoDem = !GameScr.CheDoDem;
                    Handle.Paint("Chế độ đêm: " + (GameScr.CheDoDem ? "Bật" : "Tắt"));
                    break;
                case 53:
                    Assets.Scripts.Functions.CommandFuncs.startOKDlg("Lệnh Chat", GameTcp.readFileText2("hdsd"), true);
                    break;
                case 61:
                    GameScr.gI().doOpenUI(40);
                    break;
                case 62:
                    GameScr.nhanvat = !GameScr.nhanvat;
                    break;
            }
        }

        public static void Menuauto()
        {
            MyVector myVector = new MyVector();
            if (Handle.T != null)
            {
                myVector.addElement(new Command("Tắt Auto", gI(), 4, 0));
            }
            else
            {
                if (TileMap.mapID == 1 || TileMap.mapID == 27 || TileMap.mapID == 72)
                {
                    myVector.addElement(new Command("Auto NV", gI(), 1100074, 0));
                }

                myVector.addElement(new Command("Tàn Sát", gI(), 1, 0));
            }

            //myVector.addElement(new Command("AUTO Tà thú", gI(), 54, 0));
            myVector.addElement(new Command("NPC", gI(), 5, 0));
            myVector.addElement(new Command(GameScr.HutVatPham ? "Hút VP" : " Nhặt Xa", gI(), 51, 0));
            myVector.addElement(new Command(GameScr.nhanvat ? "[Tắt]\n" + "Nv trong map" : "[Bật]\n" + "Nv trong map", gI(), 62, 0));
            myVector.addElement(new Command("Item Del", gI(), 57, 0));
            //myVector.addElement(new Command("Item bán", gI(), 58, 0));
            //myVector.addElement(new Command("Item Nhặt", gI(), 59, 0));
            //myVector.addElement(new Command("Item Bỏ ra đất", gI(), 59, 0));
            //myVector.addElement(new Command("Nhặt xa", gI(), 60, 0));
            //myVector.addElement(new Command("Nhặt full", gI(), 61, 0));
            myVector.addElement(new Command("HS Lượng: " + (GameScr.HoiSinhLuong ? "Bật" : "Tắt"), gI(), 9, 0));
            //myVector.addElement(new Command("Chế độ đêm: " + (GameScr.CheDoDem ? "Bật" : "Tắt"), gI(), 52, 0));
            myVector.addElement(new Command("Giữ Level: " + (GameScr.GiuLV ? "Bật" : "Tắt"), gI(), 8, 0));
            myVector.addElement(new Command("SPGame: " + Time.timeScale, gI(), 10, 0));
            myVector.addElement(new Command("SPNextMap: " + Handle.SpNextMap, gI(), 12, 0));
            myVector.addElement(new Command("Hướng Dẫn", gI(), 53, 0));
            //myVector.addElement(new Command("Đánh CTV", gI(), 61, 0));
            //myVector.addElement(new Command("Đánh chuyển khu", gI(), 61, 0));
            myVector.addElement(new Command("Tự động", gI(), 61, 0));
            //myVector.addElement(new Command("Khác", gI(), 14, 0));
            GameCanvas.menu.startAt(myVector, 11);
        }

        protected void MenuKhac()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Fake Skill", gI(), 15, 0));
            myVector.addElement(new Command("Fake Ingame", gI(), 16, 0));
            myVector.addElement(new Command("Fake Level", gI(), 17, 0));
            myVector.addElement(new Command("Fake Yên", gI(), 18, 0));
            myVector.addElement(new Command("Fake Xu", gI(), 19, 0));
            myVector.addElement(new Command("Fake Lượng", gI(), 20, 0));
            GameCanvas.menu.startAt(myVector, 6);
        }

        protected void FakeSkill()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Tiêu", gI(), 22, 0));
            myVector.addElement(new Command("Kiếm", gI(), 23, 0));
            myVector.addElement(new Command("Quạt", gI(), 24, 0));
            myVector.addElement(new Command("Đao", gI(), 25, 0));
            myVector.addElement(new Command("Kunai", gI(), 25, 0));
            myVector.addElement(new Command("Cung", gI(), 27, 0));
            GameCanvas.menu.startAt(myVector, 6);
        }

        protected void ClassTieu()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Tiêu 7x", gI(), 28, 0));
            myVector.addElement(new Command("Tiêu 8x", gI(), 29, 0));
            myVector.addElement(new Command("Tiêu 10x", gI(), 30, 0));
            GameCanvas.menu.startAt(myVector, 3);
        }

        protected void ClassKiem()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Kiếm 7x", gI(), 31, 0));
            myVector.addElement(new Command("Kiếm 8x", gI(), 32, 0));
            myVector.addElement(new Command("Kiếm 10x", gI(), 33, 0));
            GameCanvas.menu.startAt(myVector, 3);
        }

        protected void ClassQuat()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Quạt 7x", gI(), 34, 0));
            myVector.addElement(new Command("Quạt 8x", gI(), 35, 0));
            myVector.addElement(new Command("Quạt 10x", gI(), 36, 0));
            GameCanvas.menu.startAt(myVector, 3);
        }

        protected void ClassDao()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Đao 7x", gI(), 37, 0));
            myVector.addElement(new Command("Đao 8x", gI(), 38, 0));
            myVector.addElement(new Command("Đao 10x", gI(), 39, 0));
            GameCanvas.menu.startAt(myVector, 3);
        }

        protected void ClassKunai()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Kunai 7x", gI(), 40, 0));
            myVector.addElement(new Command("Kunai 8x", gI(), 41, 0));
            myVector.addElement(new Command("Kunai 10x", gI(), 42, 0));
            GameCanvas.menu.startAt(myVector, 3);
        }

        protected void ClassCung()
        {
            MyVector myVector = new MyVector();
            myVector.addElement(new Command("Cung 7x", gI(), 43, 0));
            myVector.addElement(new Command("Cung 8x", gI(), 44, 0));
            myVector.addElement(new Command("Cung 10x", gI(), 45, 0));
            GameCanvas.menu.startAt(myVector, 3);
        }
    }
}
