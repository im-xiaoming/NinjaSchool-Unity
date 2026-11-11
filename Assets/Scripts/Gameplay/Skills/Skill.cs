using Functions.Auto;

public class Skill
{
	public const sbyte ATT_STAND = 0;

	public const sbyte ATT_FLY = 1;

	public const sbyte SKILL_AUTO_USE = 0;

	public const sbyte SKILL_CLICK_USE_ATTACK = 1;

	public const sbyte SKILL_CLICK_USE_BUFF = 2;

	public const sbyte SKILL_CLICK_NPC = 3;

	public const sbyte SKILL_CLICK_LIVE = 4;

	public SkillTemplate template;

	public short skillId;

	public int point;

	public int level;

	public int coolDown;

	public long lastTimeUseThisSkill;

	public int dx;

	public int dy;

	public int maxFight;

	public int manaUse;

	public SkillOption[] options;

	public bool paintCanNotUseSkill;

	public static Skill me;

	public static Skill gI()
	{
		if (me == null)
		{
			return me = new Skill();
		}
		return me;
	}

	public string strTimeReplay()
	{
		if (coolDown % 1000 == 0)
		{
			return coolDown / 1000 + string.Empty;
		}
		int num = coolDown % 1000;
		return coolDown / 1000 + "." + ((num % 100 != 0) ? (num / 10) : (num / 100));
	}

	public void paint(int x, int y, mGraphics g)
	{
		SmallImage.drawSmallImage(g, template.iconId, x, y, 0, StaticObj.VCENTER_HCENTER);
		long num = mSystem.getCurrentTimeMillis() - lastTimeUseThisSkill;
       
        if (num < coolDown)
		{
			g.setColor(0, 0.5f);
			if (paintCanNotUseSkill)
			{
                long num2n = (coolDown - mSystem.getCurrentTimeMillis()) + lastTimeUseThisSkill;
              
                g.setColor(0, 0.5f);
                mFont.tahoma_7b_red.drawString(g, string.Concat(num2n / 1000L), x, y - 9, mFont.CENTER);

            }
			int num2 = (int)(num * 18 / coolDown);
			g.fillRect(x - 9, y - 9 + num2, 18, 18 - num2);
		}
		else
		{
			paintCanNotUseSkill = false;
		}
	}

	public bool isCooldown()
	{
		return mSystem.getCurrentTimeMillis() - lastTimeUseThisSkill < coolDown;
	}

	public bool isCooldown(long add)
	{
		return mSystem.getCurrentTimeMillis() - lastTimeUseThisSkill < coolDown + add;
	}

	public void Paint(int type = 0)
	{
		Char myChar = Char.getMyChar();
		lastTimeUseThisSkill = mSystem.currentTimeMillis();
		paintCanNotUseSkill = true;
		Char.getMyChar().setAutoSkillPaint(GameScr.sks[template.id], type);
		myChar.effPaints = new EffectPaint[Auto.Quai.size() + Auto.Nguoi.size()];
		for (int i = 0; i < Auto.Quai.size(); i++)
		{
			myChar.effPaints[i] = new EffectPaint();
			myChar.effPaints[i].effCharPaint = GameScr.efs[myChar.skillPaint.effId - 1];
			myChar.effPaints[i].eMob = (Mob)Auto.Quai.elementAt(i);
		}
		for (int j = 0; j < Auto.Nguoi.size(); j++)
		{
			myChar.effPaints[j + Auto.Quai.size()] = new EffectPaint();
			myChar.effPaints[j + Auto.Quai.size()].effCharPaint = GameScr.efs[myChar.skillPaint.effId - 1];
			myChar.effPaints[j + Auto.Quai.size()].eChar = (Char)Auto.Nguoi.elementAt(j);
		}
		GameCanvas.debug("Sk8", 0);
		if (myChar.effPaints.Length <= 1)
		{
			return;
		}
		EPosition firstPos = new EPosition();
		if (myChar.effPaints[0].eMob != null)
		{
			firstPos = new EPosition(myChar.effPaints[0].eMob.x, myChar.effPaints[0].eMob.y);
		}
		else if (myChar.effPaints[0].eChar != null)
		{
			firstPos = new EPosition(myChar.effPaints[0].eChar.cx, myChar.effPaints[0].eChar.cy);
		}
		MyVector myVector = new MyVector();
		for (int k = 1; k < myChar.effPaints.Length; k++)
		{
			if (myChar.effPaints[k].eMob != null)
			{
				myVector.addElement(new EPosition(myChar.effPaints[k].eMob.x, myChar.effPaints[k].eMob.y));
			}
			else if (myChar.effPaints[k].eChar != null)
			{
				myVector.addElement(new EPosition(myChar.effPaints[k].eChar.cx, myChar.effPaints[k].eChar.cy));
			}
			if (k > 5)
			{
				break;
			}
		}
		Lightning.addLight(myVector, firstPos, isContinue: true, myChar.getClassColor());
	}
}
