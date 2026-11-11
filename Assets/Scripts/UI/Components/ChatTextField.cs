// Offline build: the chat box is reused as a developer terminal.
// It keeps the original look (one TField at the bottom of the screen, painted by GameScr, with the
// command bar drawn by mScreen), but the text is sent as an offline terminal command instead of
// public chat. GameScr.onChatFromMe forwards it to the server (Service.chat -> CHAT_MAP), and the
// server replies with yellow InfoMe lines (SERVER_MESSAGE -24).
public class ChatTextField : IActionListener
{
	private static ChatTextField instance;

	/// <summary>Maximum characters per command line. The original public chat used 40; commands need more room.</summary>
	public static int MaxCommandLength = 80;

	/// <summary>Minimum milliseconds between two sent lines (the original anti-spam gap was 1000).</summary>
	public static int SendCooldownMs = 300;

	public TField tfChat;

	public bool isShow;

	/// <summary>Screen that receives the submitted text (GameScr).</summary>
	public IChatable parentScreen;

	public Command left;

	public Command right;

	public Command center;

	public Command cmdFireChat;

	private string to = string.Empty;

	private long lastSendTime;

	protected ChatTextField()
	{
		// 8000 = send, 8001 = close, 8002 = send from Enter (same ids as the original chat box).
		left = new Command(mResources.CHAT, this, 8000, null, 1, GameCanvas.h - mScreen.cmdH + 1);
		right = new Command(mResources.CLOSE, this, 8001, null, GameCanvas.w - 72, GameCanvas.h - mScreen.cmdH - 2);
		cmdFireChat = new Command(mResources.CHAT, this, 8002, null, 1, GameCanvas.h - mScreen.cmdH + 1);
		tfChat = new TField();
		tfChat.x = 16;
		tfChat.width = GameCanvas.w - 32;
		tfChat.height = mScreen.ITEM_HEIGHT + 2;
		tfChat.setMaxTextLenght(MaxCommandLength);
		tfChat.showSubTextField = false;
		tfChat.setText(string.Empty);
		tfChat.setIputType(TField.INPUT_TYPE_ANY);
		if (Main.isPC)
		{
			// PC: Enter sends (cmdFireChat), so no left soft key is shown.
			left = null;
		}
	}

	/// <summary>True while the chat box is open; safe to call before the game has created the box.</summary>
	public static bool IsShowing
	{
		get { return instance != null && instance.isShow; }
	}

	public static ChatTextField gI()
	{
		return (instance != null) ? instance : (instance = new ChatTextField());
	}

	/// <summary>Opens the input line (the chat icon in GameScr). "to" is the title shown when empty.</summary>
	public void startChat(IChatable parentScreen, string to)
	{
		if (GameCanvas.currentDialog != null)
		{
			return;
		}
		GameCanvas.clearKeyHold();
		GameCanvas.clearKeyPressed();
		this.parentScreen = parentScreen;
		this.to = to ?? string.Empty;
		right.caption = mResources.CLOSE;
		tfChat.width = GameCanvas.w - 32;
		tfChat.setText(string.Empty);
		tfChat.title_Null = this.to;
		tfChat.clearAll();
		tfChat.isFocus = true;
		isShow = true;
	}

	/// <summary>Typed characters (GameScr routes GameCanvas.keyAsciiPress here while the box is open).</summary>
	public void keyPressed(int keyCode)
	{
		if (isShow)
		{
			tfChat.keyPressed(keyCode);
		}
	}

	public void close()
	{
		tfChat.setText(string.Empty);
		isShow = false;
	}

	public void update()
	{
		if (!isShow)
		{
			return;
		}
		tfChat.isFocus = true;
		tfChat.update();
		if (NsoTerminal.IsOpen)
		{
			// Terminal: the prompt row inside the window shows the text being typed.
			NsoTerminal.ApplyLayout();
			NsoTerminal.Refresh(tfChat.getText());
		}
		if (tfChat.justReturnFromTextBox)
		{
			// Native keyboard (mobile) returned a line.
			tfChat.justReturnFromTextBox = false;
			Submit();
		}
	}

	public void paint(mGraphics g)
	{
		g.setClip(0, -200, GameCanvas.w, 200 + GameCanvas.h);
		// The terminal draws its own prompt row, so the input box is not painted then.
		if (isShow && !NsoTerminal.IsOpen)
		{
			tfChat.paint(g);
		}
		else if (isShow)
		{
			NsoTerminal.PaintCaret(g);
		}
	}

	/// <summary>Sends the current line to the parent screen, then clears it. The box stays open for the next command.</summary>
	private void Submit()
	{
		if (parentScreen == null)
		{
			return;
		}
		long now = mSystem.getCurrentTimeMillis();
		if (now - lastSendTime < SendCooldownMs)
		{
			return;
		}
		lastSendTime = now;
		string text = tfChat.getText();
		tfChat.setText(string.Empty);
		tfChat.clearAll();
		parentScreen.onChatFromMe(text, to);
	}

	public void perform(int idAction, object p)
	{
		switch (idAction)
		{
		case 8000:
		case 8002:
			Submit();
			break;
		case 8001:
			tfChat.clearAll();
			isShow = false;
			if (parentScreen != null)
			{
				parentScreen.onCancelChat();
			}
			break;
		}
	}
}

/// <summary>Screen that receives text from ChatTextField (original client interface).</summary>
public interface IChatable
{
	void onChatFromMe(string text, string to);

	void onCancelChat();
}
