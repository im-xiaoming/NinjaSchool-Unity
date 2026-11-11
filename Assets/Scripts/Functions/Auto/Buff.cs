using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace Functions.Auto
{
	public class Buff : Auto
	{
		public bool isHsx;

		public bool isBux;

		public long buff;


		public long mob;

		public long z;

		public bool qw;

		public long read;

		public int saveCharID;

		public string saveCharName;

		public override void Run()
		{
			Char myChar = Char.getMyChar();
			Char @char = ((GameScr.vParty.size() > 0) ? ((Party)GameScr.vParty.firstElement()).c : null);
			if (@char != null)
			{
				saveCharID = @char.charID;
			}
			else
			{
				saveCharID = ((Party)GameScr.vParty.firstElement()).charId;
			}
			if (Auto.isDie(myChar))
			{
				Hoisinh(isWait: false);
				return;
			}
			if (TileMap.mapID != mapID || TileMap.zoneID != zoneID)
			{
				Go(mapID, zoneID);
				return;
			}
			if (mSystem.currentTimeMillis() - this.mob >= 60000)
			{
				this.mob = mSystem.currentTimeMillis();
				MyVector myVector = new MyVector();
				for (int i = 0; i < GameScr.vMob.size(); i++)
				{
					Mob mob = (Mob)GameScr.vMob.elementAt(i);
					if (mob != null && mob.status != 1 && mob.status != 0 && mob.hp > 0 && (myVector.size() == 0 || (mob.Absx() > ((Mob)myVector.firstElement()).Absx() && mob.Absy() > ((Mob)myVector.firstElement()).Absy())))
					{
						myVector.removeAllElements();
						myVector.addElement(mob);
					}
				}
				for (int j = 0; j < Char.getMyChar().vSkill.size(); j++)
				{
					Skill skill = (Skill)Char.getMyChar().vSkill.elementAt(j);
					if (skill != null && !skill.isCooldown() && skill.template.type == 1)
					{
						Service.gI().sendPlayerAttack(myVector, new MyVector(), 1);
						skill.Paint();
						return;
					}
				}
			}
			if (GameScr.vParty.size() > 0 && ((Party)GameScr.vParty.firstElement()).c == myChar)
			{
				Service.gI().outParty();
				Handle.Sleep(300);
				return;
			}
			string path = Application.persistentDataPath + "/buffmap";
			string path2 = Application.persistentDataPath + "/buffzone";
			if (File.Exists(path))
			{
				mapID = int.Parse(File.ReadAllText(path));
			}
			if (File.Exists(path2))
			{
				zoneID = int.Parse(File.ReadAllText(path2));
			}
			if (@char != null && myChar.nClass.classId == 6 && myChar != @char && isHsx && Auto.isDie(@char))
			{
				for (int k = 0; k < myChar.vSkill.size(); k++)
				{
					Skill skill2 = (Skill)myChar.vSkill.elementAt(k);
					if (skill2 == null || skill2.isCooldown(100L) || skill2.template.type != 4)
					{
						continue;
					}
					if (Char.getMyChar().cMP < skill2.manaUse)
					{
						Handle.Die();
						return;
					}
					int cx = myChar.cx;
					int cy = myChar.cy;
					if (Math.abs(@char.cx - myChar.cx) > 55 || Math.abs(@char.cy - myChar.cy) > 55)
					{
						Char.Move(@char.cx, @char.cy);
					}
					Service.gI().selectSkill(skill2.template.id);
					Service.gI().buffLive(@char.charID);
					skill2.Paint();
					Handle.Sleep(1000);
					Char.Move(cx, cy);
					return;
				}
			}
			if (!isBux || @char == null || myChar.nClass.classId != 6 || myChar == @char)
			{
				return;
			}
			for (int l = 0; l < Char.getMyChar().vSkill.size(); l++)
			{
				Skill skill3;
				if ((skill3 = (Skill)Char.getMyChar().vSkill.elementAt(l)) != null && !skill3.isCooldown(100L) && skill3.template.type == 2 && (skill3.template.id < 67 || skill3.template.id > 72) && (skill3.template.id != 47 || !Char.isAHP || (Char.isAHP && @char.cHP < @char.cMaxHP * Char.aHpValue / 100 && skill3.template.id == 47)) && mSystem.currentTimeMillis() - buff >= 2000)
				{
					if (Char.getMyChar().cMP < skill3.manaUse)
					{
						Handle.Die();
						break;
					}
					int cx2 = Char.getMyChar().cx;
					int cy2 = Char.getMyChar().cy;
					Char.Move(@char.cx, @char.cy);
					Service.gI().selectSkill(skill3.template.id);
					Service.gI().sendUseSkillMyBuff();
					skill3.Paint();
					Handle.Sleep(1000);
					Char.Move(cx2, cy2);
					buff = mSystem.currentTimeMillis();
					break;
				}
			}
		}

		public override string ToString()
		{
			string text = ": map: " + mapID + " | khu: " + zoneID;
			if (isHsx && isBux)
			{
				return "Buff hsx team" + text;
			}
			if (isHsx)
			{
				return "Chỉ hsx" + text;
			}
			return "Chỉ buff" + text;
		}

		public void update(bool isHsx = true, bool isBuff = true)
		{
			Update();
			mapID = TileMap.mapID;
			zoneID = TileMap.zoneID;
			this.isHsx = isHsx;
			isBux = isBuff;
		}

	}
}