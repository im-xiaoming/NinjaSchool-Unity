using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mFont
{
	public static int LEFT;

	public static int RIGHT = 1;

	public static int CENTER = 2;

	public static int RED;

	public static int YELLOW = 1;

	public static int GREEN = 2;

	public static int FATAL = 3;

	public static int MISS = 4;

	public static int ORANGE = 5;

	public static int ADDMONEY = 6;

	public static int MISS_ME = 7;

	public static int FATAL_ME = 8;

	private int space;

	private Image imgFont;

	private string strFont;

	private int[][] fImages;

	private BitmapFontAtlas bitmapAtlas;

	private static readonly Dictionary<Font, BitmapFontAtlas> bitmapAtlases = new Dictionary<Font, BitmapFontAtlas>();

	public static mFont gI;

	public static mFont tahoma_7b_red;

	public static mFont tahoma_7b_blue;

	public static mFont tahoma_7b_purple;

	public static mFont tahoma_7b_yellow;

	public static mFont tahoma_7b_green;

	public static mFont tahoma_7b_white;

	public static mFont tahoma_7;

	public static mFont tahoma_7_blue1;

	public static mFont tahoma_7_white;

	public static mFont tahoma_7_yellow;

	public static mFont tahoma_7_grey;

	public static mFont tahoma_7_red;

	public static mFont tahoma_7_blue;

	public static mFont tahoma_7_green;

	public static mFont tahoma_8b;

	public static mFont number_yellow;

	public static mFont number_red;

	public static mFont number_green;

	public static mFont number_white;

	public static mFont number_orange;

	public static mFont numberb_yellow;

	public static mFont numberb_red;

	public static mFont numberb_green;

	public static mFont numberb_white;

	public static mFont numberb_orange;

	public static mFont tahoma_7_white_b;

	public static mFont number_fly_txt;

	public static mFont bigNumber_red;

	public static mFont bigNumber_While;

	public static mFont bigNumber_yellow;

	public static mFont bigNumber_green;

	public static mFont bigNumber_orange;

	public static mFont bigNumber_blue;

	public Font myFont;

	private int fontPx = 20;

	private FontStyle fontStyle = FontStyle.Normal;

	private int height;

	private int wO;

	public Color color1 = Color.white;

	public Color color2 = Color.gray;

	public sbyte id;

	public int fstyle;

	public string st1 = "áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ";

	public string st2 = "\u00b8µ¶·¹\u00a8¾»¼½Æ©ÊÇÈÉËÐÌÎÏÑªÕÒÓÔÖÝ×ØÜÞãßáâä«èåæçé¬íêëìîóïñòô\u00adøõö÷ùýúûüþ®\u00b8µ¶·¹¡¾»¼½Æ¢ÊÇÈÉËÐÌÎÏÑ£ÕÒÓÔÖÝ×ØÜÞãßáâä¤èåæçé¥íêëìîóïñòô¦øõö÷ùýúûüþ§";

	public mFont(sbyte id)
	{
		string text = "normalFont2";
		if (id > 0 && id < 8)
		{
			text = "normalFont3";
		}
		else if (id >= 17 && id < 22)
		{
			text = "normalFont0";
		}
		else if (id >= 8 && id <= 15)
		{
			text = "normalFont1";
		}
		else if (id >= 22)
		{
			text = "normalFont4";
		}
		if (id >= 28)
		{
			text = "normalFont5";
		}
		if (id == 16)
		{
			text = "normalFont3";
		}
		this.id = id;
		text = "res/x" + mGraphics.zoomLevel + "/" + text;
		Font asset = (Font)Resources.Load(text);
		if (asset != null)
		{
			fontPx = asset.fontSize > 0 ? asset.fontSize : 10 * mGraphics.zoomLevel;
			if (!UseOriginalText && !bitmapAtlases.TryGetValue(asset, out bitmapAtlas))
			{
				bitmapAtlas = BitmapFontAtlas.Create(asset);
				if (bitmapAtlas != null)
				{
					bitmapAtlases[asset] = bitmapAtlas;
				}
			}
			myFont = asset;
			height = (int)Mathf.Ceil(asset.lineHeight);
		}
		else
		{
			fontPx = 10 * mGraphics.zoomLevel;
		}
		if (bitmapAtlas == null && !(UseOriginalText && asset != null))
		{
			fontStyle = FontStyle.Bold;
			myFont = Font.CreateDynamicFontFromOSFont(new string[] { "Tahoma", "Arial", "Segoe UI" }, fontPx);
		}
		color1 = setColorFont(id);
		color2 = setColorFont(id);
		wO = bitmapAtlas != null ? bitmapAtlas.GetAdvance('o') : getWidthExactOf("o") * mGraphics.zoomLevel;
	}

	// true = draw text the way the original client did (Unity GUIStyle with the game's font assets, original coordinates)
	public static bool UseOriginalText = false; // tried: the original GUIStyle path draws faint text at wrong positions in Unity 6

	private struct BitmapGlyph
	{
		public int x;
		public int y;
		public int width;
		public int height;
		public int advance;
		public int offX; // glyph bearing: left edge relative to the pen
		public int offY; // top edge relative to the line top, measured from the tallest capital so letters share a baseline
		public bool rotated; // glyph is stored turned 90 degrees in the atlas; width/height are the atlas region size
	}

	private sealed class BitmapFontAtlas
	{
		public readonly Image image;
		public readonly Material material;
		public readonly Dictionary<int, BitmapGlyph> glyphs;
		public readonly int lineHeight;

		private BitmapFontAtlas(Image image, Material material, Dictionary<int, BitmapGlyph> glyphs, int lineHeight)
		{
			this.image = image;
			this.material = material;
			this.glyphs = glyphs;
			this.lineHeight = lineHeight;
		}

		public static BitmapFontAtlas Create(Font font)
		{
			if (font == null || font.material == null)
			{
				return null;
			}
			Texture2D texture = font.material.mainTexture as Texture2D;
			CharacterInfo[] characters = font.characterInfo;
			if (texture == null || characters == null || characters.Length == 0)
			{
				return null;
			}

			Image image = new Image();
			image.texture = texture;
			image.w = texture.width;
			image.h = texture.height;
			// Unity's own text puts the baseline one font ascent below the top of the line box; the original coordinates assume that
			int referenceMaxY = font.ascent > 0 ? font.ascent : int.MinValue;
			if (referenceMaxY == int.MinValue)
			{
				for (int i = 0; i < characters.Length; i++) referenceMaxY = Mathf.Max(referenceMaxY, characters[i].maxY);
			}
			Dictionary<int, BitmapGlyph> glyphs = new Dictionary<int, BitmapGlyph>(characters.Length);
			for (int i = 0; i < characters.Length; i++)
			{
				CharacterInfo character = characters[i];
				Rect uv = character.uv;
				BitmapGlyph glyph = new BitmapGlyph
				{
					x = Mathf.RoundToInt(uv.x * texture.width),
					y = texture.height - Mathf.RoundToInt(uv.y * texture.height),
					width = Mathf.RoundToInt(Mathf.Abs(uv.width) * texture.width),
					height = Mathf.RoundToInt(Mathf.Abs(uv.height) * texture.height),
					advance = character.advance,
					offX = character.minX,
					offY = referenceMaxY - character.maxY
				};
				// Unity packs some legacy-font glyphs turned 90 degrees: the glyph's top edge then runs along +u instead of -v.
				Vector2 bl = character.uvBottomLeft, tl = character.uvTopLeft, br = character.uvBottomRight;
				if (Mathf.Abs(tl.x - bl.x) > Mathf.Abs(tl.y - bl.y) && tl.x > bl.x && br.y < bl.y)
				{
					float minU = Mathf.Min(bl.x, tl.x, br.x), maxU = Mathf.Max(bl.x, tl.x, br.x);
					float minV = Mathf.Min(bl.y, tl.y, br.y), maxV = Mathf.Max(bl.y, tl.y, br.y);
					glyph.rotated = true;
					glyph.x = Mathf.RoundToInt(minU * texture.width);
					glyph.y = texture.height - Mathf.RoundToInt(maxV * texture.height);
					glyph.width = Mathf.RoundToInt((maxU - minU) * texture.width);
					glyph.height = Mathf.RoundToInt((maxV - minV) * texture.height);
				}
				if (glyph.width > 0 && glyph.height > 0 && glyph.advance >= 0)
				{
					glyphs[character.index] = glyph;
				}
			}
			return glyphs.Count == 0 ? null : new BitmapFontAtlas(image, font.material, glyphs, Mathf.Max(1, Mathf.RoundToInt(font.lineHeight)));
		}

		public bool TryGet(char value, out BitmapGlyph glyph)
		{
			if (glyphs.TryGetValue(value, out glyph))
			{
				return true;
			}
			if (glyphs.TryGetValue('?', out glyph))
			{
				return true;
			}
			return glyphs.TryGetValue(' ', out glyph);
		}

		public int GetAdvance(char value)
		{
			BitmapGlyph glyph;
			return TryGet(value, out glyph) ? glyph.advance : 0;
		}
	}

	public static void init()
	{
		gI = new mFont(0);
		tahoma_7b_red = new mFont(1);
		tahoma_7b_blue = new mFont(2);
		tahoma_7b_purple = new mFont(3);
		tahoma_7b_yellow = new mFont(4);
		tahoma_7b_green = new mFont(5);
		tahoma_7b_white = new mFont(7);
		tahoma_7 = new mFont(8);
		tahoma_7_blue1 = new mFont(9);
		tahoma_7_white = new mFont(10);
		tahoma_7_yellow = new mFont(11);
		tahoma_7_grey = new mFont(12);
		tahoma_7_red = new mFont(13);
		tahoma_7_blue = new mFont(14);
		tahoma_7_green = new mFont(15);
		tahoma_8b = new mFont(16);
		number_yellow = new mFont(17);
		number_red = new mFont(18);
		number_green = new mFont(19);
		number_white = new mFont(20);
		number_orange = new mFont(21);
		numberb_yellow = new mFont(22);
		numberb_red = new mFont(23);
		numberb_green = new mFont(24);
		numberb_white = new mFont(25);
		numberb_orange = new mFont(26);
		tahoma_7_white_b = new mFont(27);
		bigNumber_red = new mFont(28);
		bigNumber_While = new mFont(29);
		bigNumber_yellow = new mFont(30);
		bigNumber_green = new mFont(31);
		bigNumber_orange = new mFont(32);
		bigNumber_blue = new mFont(33);
	}

	public Color setColor(int rgb)
	{
		int num = rgb & 0xFF;
		int num2 = (rgb >> 8) & 0xFF;
		int num3 = (rgb >> 16) & 0xFF;
		float b = (float)num / 256f;
		float g = (float)num2 / 256f;
		float r = (float)num3 / 256f;
		return new Color(r, g, b);
	}

	public void setTypePaint(mGraphics g, string st, int x, int y, int align, sbyte idFont)
	{
		sbyte b = id;
		if (idFont > 0)
		{
			b = idFont;
		}
		// the original also nudged x by -1 to suit its bitmap fonts; the system font is already centred, so only the y nudge stays
		if (mGraphics.zoomLevel == 4)
		{
			y += 2;
		}
		switch (b)
		{
			case 1:
				color1 = Color.red;
				color2 = Color.red;
				_drawString(g, st, x, y, align);
				break;
			case 2:
				color1 = Color.blue;
				color2 = Color.blue;
				_drawString(g, st, x, y, align);
				break;
			case 3:
				color1 = setColor(13500671);
				color2 = setColor(13500671);
				_drawString(g, st, x, y, align);
				break;
			case 4:
				color1 = setColor(16755200);
				color2 = setColor(16755200);
				_drawString(g, st, x, y, align);
				break;
			case 5:
				color1 = Color.green;
				color2 = Color.green;
				_drawString(g, st, x, y, align);
				break;
			case 6:
				color1 = setColor(16776960);
				color2 = setColor(16776960);
				_drawString(g, st, x, y, align);
				break;
			case 7:
			case 27:
				color1 = Color.white;
				color2 = Color.white;
				_drawString(g, st, x, y, align);
				break;
			case 8:
				color1 = Color.black;
				color2 = Color.black;
				_drawString(g, st, x, y, align);
				break;
			case 9:
				color1 = setColor(65535);
				color2 = setColor(65535);
				_drawString(g, st, x, y, align);
				break;
			case 10:
				color1 = Color.white;
				color2 = Color.white;
				_drawString(g, st, x, y, align);
				break;
			case 11:
				color1 = Color.yellow;
				color2 = Color.yellow;
				_drawString(g, st, x, y, align);
				break;
			case 12:
				color1 = setColor(5592405);
				color2 = setColor(5592405);
				_drawString(g, st, x, y, align);
				break;
			case 13:
				color1 = Color.red;
				color2 = Color.red;
				_drawString(g, st, x, y, align);
				break;
			case 14:
				color1 = setColor(33023);
				color2 = setColor(33023);
				_drawString(g, st, x, y, align);
				break;
			case 15:
				color1 = setColor(8701737);
				color2 = setColor(8701737);
				_drawString(g, st, x, y, align);
				break;
			case 16:
				color1 = setColor(15655191);
				color2 = setColor(15655191);
				_drawString(g, st, x, y, align);
				break;
			case 17:
				color1 = setColor(0);
				color2 = setColor(0);
				_drawString(g, st, x - 1, y - 3, align);
				color1 = Color.yellow;
				color2 = Color.yellow;
				_drawString(g, st, x - 2, y - 3, align);
				break;
			case 18:
				color1 = setColor(0);
				color2 = setColor(0);
				_drawString(g, st, x - 1, y - 3, align);
				color1 = Color.red;
				color2 = Color.red;
				_drawString(g, st, x - 2, y - 3, align);
				break;
			case 19:
				color1 = setColor(0);
				color2 = setColor(0);
				_drawString(g, st, x - 1, y - 3, align);
				color1 = Color.green;
				color2 = Color.green;
				_drawString(g, st, x - 2, y - 3, align);
				break;
			case 20:
				color1 = setColor(0);
				color2 = setColor(0);
				_drawString(g, st, x - 1, y - 3, align);
				color1 = Color.white;
				color2 = Color.white;
				_drawString(g, st, x - 2, y - 3, align);
				break;
			case 21:
				color1 = setColor(0);
				color2 = setColor(0);
				_drawString(g, st, x - 1, y - 3, align);
				color1 = setColor(16096312);
				color2 = setColor(16096312);
				_drawString(g, st, x - 2, y - 3, align);
				break;
			case 22:
				color1 = setColor(5592405);
				color2 = setColor(5592405);
				_drawString(g, st, x, y + 1, align);
				color1 = Color.yellow;
				color2 = Color.yellow;
				_drawString(g, st, x, y, align);
				break;
			case 30:
				color1 = Color.black;
				color2 = Color.black;
				_drawString(g, st, x, y + 1, align);
				color1 = Color.yellow;
				color2 = Color.yellow;
				_drawString(g, st, x, y, align);
				break;
			case 23:
			case 28:
				color1 = setColor(5592405);
				color2 = setColor(5592405);
				_drawString(g, st, x, y + 1, align);
				color1 = Color.red;
				color2 = Color.red;
				_drawString(g, st, x, y, align);
				break;
			case 24:
			case 31:
				color1 = setColor(5592405);
				color2 = setColor(5592405);
				_drawString(g, st, x, y + 1, align);
				color1 = Color.green;
				color2 = Color.green;
				_drawString(g, st, x, y, align);
				break;
			case 25:
			case 29:
				color1 = setColor(5592405);
				color1 = setColor(5592405);
				_drawString(g, st, x, y + 1, align);
				color1 = Color.white;
				color2 = Color.white;
				_drawString(g, st, x, y, align);
				break;
			case 26:
			case 32:
				color1 = setColor(5592405);
				color1 = setColor(5592405);
				_drawString(g, st, x, y + 1, align);
				color1 = setColor(16096312);
				color1 = setColor(16096312);
				_drawString(g, st, x, y, align);
				break;
			case 33:
				color1 = setColor(5592405);
				color1 = setColor(5592405);
				_drawString(g, st, x, y + 1, align);
				color1 = Color.blue;
				color1 = Color.blue;
				_drawString(g, st, x, y, align);
				break;
		}
	}

	public Color setColorFont(sbyte id)
	{
		switch (id)
		{
			case 1:
				return Color.red;
			case 2:
				return Color.blue;
			case 3:
				return setColor(13500671);
			case 4:
				return setColor(16755200);
			case 5:
				return Color.green;
			case 6:
				return setColor(16776960);
			case 7:
				return Color.white;
			case 8:
				return Color.black;
			case 9:
				return setColor(65535);
			case 10:
				return Color.white;
			case 11:
				return Color.yellow;
			case 12:
				return setColor(5592405);
			case 13:
				return Color.red;
			case 14:
				return setColor(33023);
			case 15:
				return setColor(8701737);
			case 16:
				return setColor(15655191);
			case 17:
				return setColor(0);
			case 18:
				return setColor(0);
			case 19:
				return setColor(0);
			case 20:
				return setColor(0);
			case 21:
				return setColor(0);
			case 22:
			case 30:
				return setColor(5592405);
			case 23:
			case 28:
				return setColor(5592405);
			case 24:
			case 31:
				return setColor(5592405);
			case 25:
			case 29:
				return setColor(5592405);
			case 26:
			case 32:
				return setColor(5592405);
			default:
				return Color.white;
		}
	}

	public void drawString(mGraphics g, string st, int x, int y, int align)
	{
		setTypePaint(g, st, x, y, align, id);
	}

	public void drawString(mGraphics g, string st, int x, int y, int align, mFont font)
	{
		setTypePaint(g, st, x, y + 1, align, font.id);
		setTypePaint(g, st, x, y, align, 0);
	}

	public MyVector splitFontVector(string src, int lineWidth)
	{
		MyVector myVector = new MyVector();
		string text = string.Empty;
		for (int i = 0; i < src.Length; i++)
		{
			if (src[i] == '\n')
			{
				myVector.addElement(text);
				text = string.Empty;
				continue;
			}
			text += src[i];
			if (getWidth(text) > lineWidth)
			{
				int num = 0;
				num = text.Length - 1;
				while (num >= 0 && text[num] != ' ')
				{
					num--;
				}
				if (num < 0)
				{
					num = text.Length - 1;
				}
				myVector.addElement(text.Substring(0, num));
				i = i - (text.Length - num) + 1;
				text = string.Empty;
			}
			if (i == src.Length - 1 && !text.Trim().Equals(string.Empty))
			{
				myVector.addElement(text);
			}
		}
		return myVector;
	}

	public string splitFirst(string str)
	{
		string text = string.Empty;
		bool flag = false;
		for (int i = 0; i < str.Length; i++)
		{
			if (!flag)
			{
				string text2 = str.Substring(i);
				text = ((!compare(text2, " ")) ? (text + text2) : (text + str[i] + "-"));
				flag = true;
			}
			else if (str[i] == ' ')
			{
				flag = false;
			}
		}
		return text;
	}

	public string[] splitStrInLine(string src, int lineWidth)
	{
		ArrayList arrayList = splitStrInLineA(src, lineWidth);
		string[] array = new string[arrayList.Count];
		for (int i = 0; i < arrayList.Count; i++)
		{
			array[i] = (string)arrayList[i];
		}
		return array;
	}

	public ArrayList splitStrInLineA(string src, int lineWidth)
	{
		ArrayList arrayList = new ArrayList();
		int i = 0;
		int num = 0;
		int length = src.Length;
		if (length < 5)
		{
			arrayList.Add(src);
			return arrayList;
		}
		string text = string.Empty;
		try
		{
			while (true)
			{
				if (getWidthNotExactOf(text) < lineWidth)
				{
					text += src[num];
					num++;
					if (src[num] != '\n')
					{
						if (num < length - 1)
						{
							continue;
						}
						num = length - 1;
					}
				}
				if (num != length - 1 && src[num + 1] != ' ')
				{
					int num2 = num;
					while (src[num + 1] != '\n' && (src[num + 1] != ' ' || src[num] == ' ') && num != i)
					{
						num--;
					}
					if (num == i)
					{
						num = num2;
					}
				}
				string text2 = src.Substring(i, num + 1 - i);
				if (text2[0] == '\n')
				{
					text2 = text2.Substring(1, text2.Length - 1);
				}
				if (text2[text2.Length - 1] == '\n')
				{
					text2 = text2.Substring(0, text2.Length - 1);
				}
				arrayList.Add(text2);
				if (num == length - 1)
				{
					break;
				}
				for (i = num + 1; i != length - 1 && src[i] == ' '; i++)
				{
				}
				if (i == length - 1)
				{
					break;
				}
				num = i;
				text = string.Empty;
			}
		}
		catch (Exception ex)
		{
			Out.LogWarning("EXCEPTION WHEN REAL SPLIT " + src + "\nend=" + num + "\n" + ex.Message + "\n" + ex.StackTrace);
			arrayList.Add(src);
		}
		return arrayList;
	}

	public string[] splitFontArray(string src, int lineWidth)
	{
		MyVector myVector = splitFontVector(src, lineWidth);
		string[] array = new string[myVector.size()];
		for (int i = 0; i < myVector.size(); i++)
		{
			array[i] = (string)myVector.elementAt(i);
		}
		return array;
	}

	public bool compare(string strSource, string str)
	{
		for (int i = 0; i < strSource.Length; i++)
		{
			if ((string.Empty + strSource[i]).Equals(str))
			{
				return true;
			}
		}
		return false;
	}

	public int getWidth(string s)
	{
		return getWidthExactOf(s);
	}

	public int getWidthExactOf(string s)
	{
		if (bitmapAtlas != null)
		{
			return GetBitmapWidth(s) / Mathf.Max(1, mGraphics.zoomLevel);
		}
		try
		{
			GUIStyle gUIStyle = new GUIStyle();
			gUIStyle.font = myFont;
			gUIStyle.fontSize = fontPx;
			gUIStyle.fontStyle = fontStyle;
			return (int)gUIStyle.CalcSize(new GUIContent(s)).x / mGraphics.zoomLevel;
		}
		catch (Exception ex)
		{
			Out.LogError("GET WIDTH OF " + s + " FAIL.\n" + ex.Message + "\n" + ex.StackTrace);
			return getWidthNotExactOf(s);
		}
	}

	public int getWidthNotExactOf(string s)
	{
		if (bitmapAtlas != null)
		{
			return GetBitmapWidth(s) / Mathf.Max(1, mGraphics.zoomLevel);
		}
		return s.Length * wO / Mathf.Max(1, mGraphics.zoomLevel);
	}

	private int GetBitmapWidth(string text)
	{
		int maxWidth = 0;
		int lineWidth = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				maxWidth = Mathf.Max(maxWidth, lineWidth);
				lineWidth = 0;
				continue;
			}
			if (text[i] == '\r')
			{
				continue;
			}
			lineWidth += bitmapAtlas.GetAdvance(text[i]);
		}
		return Mathf.Max(maxWidth, lineWidth);
	}

	public int getHeight()
	{
		if (bitmapAtlas != null)
		{
			return bitmapAtlas.lineHeight / Mathf.Max(1, mGraphics.zoomLevel);
		}
		if (height > 0)
		{
			return height / mGraphics.zoomLevel;
		}
		GUIStyle gUIStyle = new GUIStyle();
		gUIStyle.font = myFont;
		gUIStyle.fontSize = fontPx;
		gUIStyle.fontStyle = fontStyle;
		try
		{
			height = (int)gUIStyle.CalcSize(new GUIContent("Adg")).y + 2;
		}
		catch (Exception ex)
		{
			Out.LogError("FAIL GET HEIGHT " + ex.StackTrace);
			height = 20;
		}
		return height / mGraphics.zoomLevel;
	}

	public void _drawString(mGraphics g, string st, int x0, int y0, int align)
	{
		if (bitmapAtlas != null)
		{
			_drawBitmapString(g, st, x0, y0, align);
			return;
		}
		GUIStyle gUIStyle = new GUIStyle(GUI.skin.label);
		gUIStyle.padding = new RectOffset(0, 0, 0, 0);
		gUIStyle.font = myFont;
		gUIStyle.fontSize = fontPx;
		gUIStyle.fontStyle = fontStyle;
		float num = 0f;
		float num2 = 0f;
		switch (align)
		{
			case 0:
				num = x0;
				num2 = y0;
				gUIStyle.alignment = TextAnchor.UpperLeft;
				break;
			case 1:
				num = x0 - ScaleGUI.WIDTH / mGraphics.zoomLevel;
				num2 = y0;
				gUIStyle.alignment = TextAnchor.UpperRight;
				break;
			case 2:
			case 3:
				num = x0 - ScaleGUI.WIDTH / mGraphics.zoomLevel / 2f;
				num2 = y0;
				gUIStyle.alignment = TextAnchor.UpperCenter;
				break;
		}
		gUIStyle.normal.textColor = color1;
		Vector2 gc = TextCalib.Get("global");
		g.drawString(st, num + gc.x + TextCalib.X, num2 + gc.y + TextCalib.Y, gUIStyle);
	}

	private void _drawBitmapString(mGraphics g, string text, int x0, int y0, int align)
	{
		int zoom = Mathf.Max(1, mGraphics.zoomLevel);
		Vector2 global = TextCalib.Get("global");
		float boxX = x0, boxY = y0;
		if (TextCalib.HasBox && text.IndexOf('\n') < 0)
		{
			// vertical ink extent of the line, so its drawn pixels (not the line box) are centred on the requested point
			float top = float.MaxValue, bottom = float.MinValue;
			for (int k = 0; k < text.Length; k++)
			{
				BitmapGlyph bg;
				if (char.IsWhiteSpace(text[k]) || !bitmapAtlas.TryGet(text[k], out bg)) continue;
				top = Mathf.Min(top, bg.offY);
				bottom = Mathf.Max(bottom, bg.offY + bg.height);
			}
			boxX = TextCalib.BoxX;
			boxY = top > bottom ? TextCalib.BoxY : TextCalib.BoxY - (top + bottom) / 2f / zoom;
		}
		float adjustedX = boxX + global.x + TextCalib.X;
		float adjustedY = boxY + global.y + TextCalib.Y;
		float anchorX = adjustedX * zoom;
		float lineStartX = AlignBitmapLine(anchorX, text, 0, align);
		float penX = lineStartX;
		float penY = adjustedY * zoom;
		int lineHeightPx = bitmapAtlas.lineHeight;
		float rectLeft = lineStartX;
		float rectRight = lineStartX + GetBitmapLineWidth(text, 0);
		int lineStart = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char value = text[i];
			if (value == '\r')
			{
				continue;
			}
			if (value == '\n')
			{
				lineStart = i + 1;
				lineStartX = AlignBitmapLine(anchorX, text, lineStart, align);
				penX = lineStartX;
				rectLeft = Mathf.Min(rectLeft, lineStartX);
				rectRight = Mathf.Max(rectRight, lineStartX + GetBitmapLineWidth(text, lineStart));
				penY += lineHeightPx;
				continue;
			}

			BitmapGlyph glyph;
			if (!bitmapAtlas.TryGet(value, out glyph))
			{
				continue;
			}
			if (!char.IsWhiteSpace(value))
			{
				if (glyph.rotated)
				{
					g._drawRotatedGlyph(bitmapAtlas.image, glyph.x, glyph.y, glyph.width, glyph.height,
						Mathf.RoundToInt(penX) + glyph.offX, Mathf.RoundToInt(penY) + glyph.offY, bitmapAtlas.material, color1);
				}
				else
				{
					g._drawRegion(bitmapAtlas.image, glyph.x, glyph.y, glyph.width, glyph.height, mGraphics.TRANS_MIRROR_ROT180,
						Mathf.RoundToInt(penX) + glyph.offX, Mathf.RoundToInt(penY) + glyph.offY, mGraphics.TOP | mGraphics.LEFT, bitmapAtlas.material, color1);
				}
			}
			penX += glyph.advance;
		}
		TextCalib.RecordBitmap(TextCalib.Key, Mathf.RoundToInt(rectLeft), Mathf.RoundToInt(adjustedY * zoom),
			Mathf.RoundToInt(rectRight - rectLeft), lineHeightPx * CountLines(text));
	}

	private int GetBitmapLineWidth(string text, int start)
	{
		int width = 0;
		for (int i = start; i < text.Length && text[i] != '\n'; i++)
		{
			if (text[i] != '\r') width += bitmapAtlas.GetAdvance(text[i]);
		}
		return width;
	}

	// Text of the tuned items (level, exp, hp, mp, zone numbers, captions...) is aligned by its drawn pixels, not by its advance
	// box: the ink (first glyph's left edge to last glyph's right edge) is centred, or put flush left/right, on the anchor.
	// That makes "centre" mean the visual centre whatever the glyph bearings or italic slant are. Other text keeps the box rule.
	private float AlignBitmapLine(float anchorX, string text, int start, int align)
	{
		int width = GetBitmapLineWidth(text, start);
		if (TextCalib.Key == null) return GetAlignedBitmapLineStart(anchorX, width, align);
		float inkLeft = float.MaxValue, inkRight = float.MinValue, pen = 0f;
		for (int i = start; i < text.Length && text[i] != '\n'; i++)
		{
			if (text[i] == '\r') continue;
			BitmapGlyph glyph;
			if (!bitmapAtlas.TryGet(text[i], out glyph)) continue;
			if (!char.IsWhiteSpace(text[i]))
			{
				inkLeft = Mathf.Min(inkLeft, pen + glyph.offX);
				inkRight = Mathf.Max(inkRight, pen + glyph.offX + glyph.width);
			}
			pen += glyph.advance;
		}
		if (inkLeft > inkRight) return GetAlignedBitmapLineStart(anchorX, width, align);
		if (align == 1) return anchorX - inkRight;
		if (align == 2 || align == 3) return anchorX - (inkLeft + inkRight) / 2f;
		return anchorX - inkLeft;
	}

	private static float GetAlignedBitmapLineStart(float anchorX, int width, int align)
	{
		if (align == 1) return anchorX - width;
		if (align == 2 || align == 3) return anchorX - width / 2f;
		return anchorX;
	}

	private static int CountLines(string text)
	{
		int lines = 1;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				lines++;
			}
		}
		return lines;
	}

	public static string[] splitStringSv(string _text, string _searchStr)
	{
		int num = 0;
		int startIndex = 0;
		int length = _searchStr.Length;
		int num2 = _text.IndexOf(_searchStr, startIndex);
		while (num2 != -1)
		{
			startIndex = num2 + length;
			num2 = _text.IndexOf(_searchStr, startIndex);
			num++;
		}
		string[] array = new string[num + 1];
		int num3 = _text.IndexOf(_searchStr);
		int num4 = 0;
		int num5 = 0;
		while (num3 != -1)
		{
			array[num5] = _text.Substring(num4, num3 - num4);
			num4 = num3 + length;
			num3 = _text.IndexOf(_searchStr, num4);
			num5++;
		}
		array[num5] = _text.Substring(num4, _text.Length - num4);
		return array;
	}

	public void reloadImage()
	{
	}

	public void freeImage()
	{
	}
}
