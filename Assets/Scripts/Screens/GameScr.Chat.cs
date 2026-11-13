using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    // IChatable: a line typed in the chat box. Offline the chat box is a dev terminal (see NsoTerminal).
    public void onChatFromMe(string text, string to)
    {
        NsoTerminal.Submit(text);
    }

    // IChatable: the input line was closed with its "Đóng" command; close the terminal window with it.
    public void onCancelChat()
    {
        auto = saveAuto;
        if (NsoTerminal.IsOpen)
        {
            actionPerform(11066, null); // same as the window's own close command
        }
    }

    // Offline dev terminal: the original message window (as openUIChatTab: alert popup, message-style title,
    // close command 11066) showing NsoTerminal.View; the prompt row inside it shows the ChatTextField text.
    private void openTerminal()
    {
        saveAuto = auto;
        isPaintAlert = true;
        isPaintMessage = true;
        isLockKey = true;
        setPopupSize(190, 176);
        NsoTerminal.ApplyLayout(); // large window ending just above the "Đóng" button
        right = new Command(mResources.CLOSE, 11066);
        left = (center = null);
        textsTitle = NsoTerminal.Title;
        texts = NsoTerminal.View;
        ChatTextField.gI().startChat(this, string.Empty);
        NsoTerminal.ResetScroll();
        NsoTerminal.Refresh(string.Empty);
    }
}
