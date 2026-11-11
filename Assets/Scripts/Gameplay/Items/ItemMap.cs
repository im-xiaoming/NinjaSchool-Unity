using System.Threading;

public class ItemMap
{
	public int x;

	public int y;

	public int xEnd;

	public int yEnd;

	public int f;

	public int vx;

	public int vy;

	public int itemMapID;

	public int IdCharMove;

	public ItemTemplate template;

	public sbyte status;

	public int sizeItem = 24;

	public MyImage imgCaptcha;

	private int frame;

	public static int Da;

	public long TimeStart;

	public static object b;

	public static bool isb;

	public bool isWait;

	public ItemMap(short itemMapID, short itemTemplateID, int x, int y, int xEnd, int yEnd)
	{
		this.itemMapID = itemMapID;
		template = ItemTemplates.get(itemTemplateID);
		this.x = xEnd;
		this.y = y;
		this.xEnd = xEnd;
		this.yEnd = yEnd;
		vx = xEnd - x >> 2;
		vy = 5;
		TimeStart = mSystem.currentTimeMillis();
	}

	public ItemMap(short itemMapID, short itemTemplateID, int x, int y)
	{
		this.itemMapID = itemMapID;
		template = ItemTemplates.get(itemTemplateID);
		xEnd = x;
		this.x = x;
		yEnd = y;
		this.y = y;
		status = 1;
	}

	public void setPoint(int xEnd, int yEnd)
	{
		this.xEnd = xEnd;
		this.yEnd = yEnd;
		vx = xEnd - x >> 2;
		vy = yEnd - y >> 2;
		status = 2;
	}

	public void update()
	{
		if (status == 2 && x == xEnd && y == yEnd)
		{
			GameScr.vItemMap.removeElement(this);
			if (Char.getMyChar().itemFocus != null && Char.getMyChar().itemFocus.Equals(this))
			{
				Char.getMyChar().itemFocus = null;
			}
			return;
		}
		if (status > 0)
		{
			if (vx == 0)
			{
				x = xEnd;
			}
			if (vy == 0)
			{
				y = yEnd;
			}
			if (x != xEnd)
			{
				x += vx;
				if ((vx > 0 && x > xEnd) || (vx < 0 && x < xEnd))
				{
					x = xEnd;
				}
			}
			if (y != yEnd)
			{
				y += vy;
				if ((vy > 0 && y > yEnd) || (vy < 0 && y < yEnd))
				{
					y = yEnd;
				}
			}
		}
		else
		{
			status -= 4;
			if (status < -12)
			{
				y -= 12;
				status = 1;
			}
		}
		if (GameCanvas.gameTick % 5 == 0)
		{
			frame++;
			if (frame > 2)
			{
				frame = 0;
			}
		}
	}

	public void paint(mGraphics g)
	{
		if (imgCaptcha != null && imgCaptcha.img != null)
		{
			int num = 0;
			if (status <= 0)
			{
				num = status;
			}
			g.drawImage(imgCaptcha.img, x, y + num, mGraphics.BOTTOM | mGraphics.HCENTER);
			return;
		}
		int num2 = 0;
		if (status <= 0)
		{
			num2 = status;
		}
		SmallImage.drawSmallImage(g, template.iconID, x, y + num2, 0, mGraphics.BOTTOM | mGraphics.HCENTER);
		if (Char.getMyChar().itemFocus != null && Char.getMyChar().itemFocus.Equals(this) && status != 2)
		{
			SmallImage.drawSmallImage(g, 988, x, y - 20, 0, mGraphics.VCENTER | mGraphics.HCENTER);
		}
	}

	public bool isNoMyItem()
	{
		return mSystem.currentTimeMillis() - TimeStart >= 15000;
	}

	public static void s(int milisSeconds = 1000)
	{
		isb = true;
		lock (b)
		{
			Monitor.Wait(b, milisSeconds);
		}
	}

	public static void d()
	{
		if (isb)
		{
			lock (b)
			{
				Monitor.PulseAll(b);
			}
			isb = false;
		}
	}

	static ItemMap()
	{
		b = new object();
	}
}
