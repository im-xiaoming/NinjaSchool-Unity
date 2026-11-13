using UnityEngine;

namespace Assets.Scripts.Functions
{
    public class CommandFuncs : MonoBehaviour, IActionListener
    {
        public static CommandFuncs instance;
        private void Start()
        {
            instance = this;
        }

        public void perform(int idAction, object p)
        {
            switch (idAction)
            {
                case 8882:
                    InfoDlg.hide();
                    GameCanvas.currentDialog = null;
                    break;
            }
        }

        public static void closeKeyBoard()
        {
            bool visible = TouchScreenKeyboard.visible;
            if (visible)
            {
                TField.kb.active = false;
                TField.kb = null;
            }
        }

        public static void startOKDlg(string info, string str, bool show)
        {
            bool flag = GameCanvas.currentScreen == GameScr.instance;
            if (flag)
            {
                GameScr.gI().showAlert(info, str, show);
            }
            else
            {
                closeKeyBoard();
                GameCanvas.msgdlg.setInfo(info, null, new Command(mResources.OK, instance, 8882, null), null);
                GameCanvas.currentDialog = GameCanvas.msgdlg;
            }
        }
    }
}
