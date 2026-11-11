// Offline build: there is no chat box. This is an inert placeholder that is never shown, kept only so that the many
// screen conditions that still test it compile; nothing here reads, sends or stores text.
public class ChatTextField
{
	private static ChatTextField instance;

	public TField tfChat;

	public bool isShow;

	public Command left;

	public Command right;

	public Command center;

	public Command cmdFireChat;

	protected ChatTextField()
	{
		tfChat = new TField();
		cmdFireChat = new Command();
	}

	public static ChatTextField gI()
	{
		return (instance != null) ? instance : (instance = new ChatTextField());
	}

	public void keyPressed(int keyCode)
	{
	}

	public void close()
	{
		isShow = false;
	}

	public void update()
	{
	}

	public void paint(mGraphics g)
	{
	}
}
