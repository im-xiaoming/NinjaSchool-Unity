using System.Collections;
using UnityEngine;

public class mGraphics
{
	public static int HCENTER = 1;

	public static int VCENTER = 2;

	public static int LEFT = 4;

	public static int RIGHT = 8;

	public static int TOP = 16;

	public static int BOTTOM = 32;

	private float r;

	private float g;

	private float b;

	private float a;

	public int clipX;

	public int clipY;

	public int clipW;

	public int clipH;

	private bool isClip;

	private bool isTranslate = true;

	private int translateX;

	private int translateY;

	public static int zoomLevel = 1;

	public const int BASELINE = 64;

	public const int SOLID = 0;

	public const int DOTTED = 1;

	public const int TRANS_MIRROR = 2;

	public const int TRANS_MIRROR_ROT180 = 1;

	public const int TRANS_MIRROR_ROT270 = 4;

	public const int TRANS_MIRROR_ROT90 = 7;

	public const int TRANS_NONE = 0;

	public const int TRANS_ROT180 = 3;

	public const int TRANS_ROT270 = 6;

	public const int TRANS_ROT90 = 5;

	public static Hashtable cachedTextures = new Hashtable();

	private int clipTX;

	private int clipTY;

	private int currentBGColor;

	private Vector2 pos = new Vector2(0f, 0f);

	private Rect rect;

	private Matrix4x4 matrixBackup;

	private Vector2 pivot;

	public Vector2 size = new Vector2(128f, 128f);

	public Vector2 relativePosition = new Vector2(0f, 0f);

	private void cache(string key, Texture value)
	{
		if (cachedTextures.Count > 400)
		{
			cachedTextures.Clear();
		}
		if (value.width * value.height < GameCanvas.w * GameCanvas.h)
		{
			cachedTextures.Add(key, value);
		}
	}

	public void translate(int tx, int ty)
	{
        if (tx == -getTranslateX() && ty == -getTranslateY()) ClearScrollPaintOffset();
		tx *= zoomLevel;
		ty *= zoomLevel;
		translateX += tx;
		translateY += ty;
		isTranslate = true;
		if (translateX == 0 && translateY == 0)
		{
			isTranslate = false;
		}
	}

	// True when the mouse is over this rectangle (game pixels, in the current translation). Used for hover highlights.
	public bool isMouseOver(int x, int y, int w, int h)
	{
		Vector3 m = Input.mousePosition;
		float mx = m.x, my = Screen.height - m.y;   // screen pixels, Y down like the game
		float rx = x * zoomLevel + (isTranslate ? translateX : 0);
		float ry = y * zoomLevel + (isTranslate ? translateY : 0);
		return mx >= rx && mx < rx + w * zoomLevel && my >= ry && my < ry + h * zoomLevel;
	}

	public int getTranslateX()
	{
		return translateX / zoomLevel;
	}

	public int getTranslateY()
	{
		return translateY / zoomLevel;
	}

	public void setClip(int x, int y, int w, int h)
	{
        ClearScrollPaintOffset();
		x *= zoomLevel;
		y *= zoomLevel;
		w *= zoomLevel;
		h *= zoomLevel;
		clipTX = translateX;
		clipTY = translateY;
		clipX = x;
		clipY = y;
		clipW = w;
		clipH = h;
		isClip = true;
	}

	public void drawLine(int x1, int y1, int x2, int y2)
	{
		x1 *= zoomLevel;
		y1 *= zoomLevel;
		x2 *= zoomLevel;
		y2 *= zoomLevel;
		if (y1 == y2)
		{
			if (x1 > x2)
			{
				int num = x2;
				x2 = x1;
				x1 = num;
			}
			fillRect(x1, y1, x2 - x1, 1);
			return;
		}
		if (x1 == x2)
		{
			if (y1 > y2)
			{
				int num2 = y2;
				y2 = y1;
				y1 = num2;
			}
			fillRect(x1, y1, 1, y2 - y1);
			return;
		}
		if (isTranslate)
		{
			x1 += translateX;
			y1 += translateY;
			x2 += translateX;
			y2 += translateY;
		}
		string key = "dl" + r + g + b;
		Texture2D texture2D = (Texture2D)cachedTextures[key];
		if (texture2D == null)
		{
			texture2D = new Texture2D(1, 1);
			Color color = new Color(r, g, b);
			texture2D.SetPixel(0, 0, color);
			texture2D.Apply();
			cache(key, texture2D);
		}
		Vector2 vector = new Vector2(x1 + scrollPaintOffset.x, y1 + scrollPaintOffset.y);
		Vector2 vector2 = new Vector2(x2 + scrollPaintOffset.x, y2 + scrollPaintOffset.y);
		Vector2 vector3 = vector2 - vector;
		float num3 = 57.29578f * Mathf.Atan(vector3.y / vector3.x);
		if (vector3.x < 0f)
		{
			num3 += 180f;
		}
		int num4 = (int)Mathf.Ceil(0f);
		GUIUtility.RotateAroundPivot(num3, vector);
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		if (isClip)
		{
			num5 = clipX;
			num6 = clipY;
			num7 = clipW;
			num8 = clipH;
			if (isTranslate)
			{
				num5 += clipTX;
				num6 += clipTY;
			}
		}
		if (isClip)
		{
			GUI.BeginGroup(new Rect(num5, num6, num7, num8));
		}
		Graphics.DrawTexture(new Rect(vector.x - (float)num5, vector.y - (float)num4 - (float)num6, vector3.magnitude, 1f), texture2D);
		if (isClip)
		{
			GUI.EndGroup();
		}
		GUIUtility.RotateAroundPivot(0f - num3, vector);
	}

	public Color setColorMiniMap(int rgb)
	{
		int num = rgb & 0xFF;
		int num2 = (rgb >> 8) & 0xFF;
		int num3 = (rgb >> 16) & 0xFF;
		float num4 = (float)num / 256f;
		float num5 = (float)num2 / 256f;
		float num6 = (float)num3 / 256f;
		return new Color(num6, num5, num4);
	}

	public float[] getRGB(Color cl)
	{
		float num = 256f * cl.r;
		float num2 = 256f * cl.g;
		float num3 = 256f * cl.b;
		return new float[3] { num, num2, num3 };
	}

	public void drawRect(int x, int y, int w, int h)
	{
		int num = 1;
		fillRect(x, y, w, num);
		fillRect(x, y, num, h);
		fillRect(x + w, y, num, h + 1);
		fillRect(x, y + h, w + 1, num);
	}

    // Fractional placement for selected visual effects; the old integer drawing API stays intact.
    public struct OffsetScope : System.IDisposable
    {
        private Matrix4x4 previous;
        private bool changed;
        public OffsetScope(float x, float y)
        {
            changed = x != 0f || y != 0f;
            previous = changed ? GUI.matrix : Matrix4x4.identity;
            if (changed) GUI.matrix = previous * Matrix4x4.Translate(new Vector3(x * zoomLevel, y * zoomLevel, 0f));
        }
        public void Dispose() { if (changed) GUI.matrix = previous; }
    }
    public OffsetScope Offset(float x, float y) { return new OffsetScope(x, y); }

    private Vector2 scrollPaintOffset;
    // Apply before clipping: the viewport itself must not move with the scrolled content.
    public void SetScrollPaintOffset(float x, float y)
    {
        scrollPaintOffset = new Vector2(x * zoomLevel, y * zoomLevel);
    }
    public void ClearScrollPaintOffset()
    {
        scrollPaintOffset = Vector2.zero;
    }

    public void fillRect(int x, int y, int w, int h) { fillRect((float)x, y, w, h); }
    // Keep fractional HP-bar width until rasterization.
    public void fillRect(float x, float y, float w, float h)
    {
		x *= zoomLevel;
		y *= zoomLevel;
        x += scrollPaintOffset.x;
        y += scrollPaintOffset.y;
		w *= zoomLevel;
		h *= zoomLevel;
		if (w < 0 || h < 0)
		{
			return;
		}
		if (isTranslate)
		{
			x += translateX;
			y += translateY;
		}
		int num = 1;
		int num2 = 1;
		string key = "fr" + num + num2 + r + g + b + a;
		Texture2D texture2D = (Texture2D)cachedTextures[key];
		if (texture2D == null)
		{
			texture2D = new Texture2D(num, num2);
			Color color = new Color(r, g, b, a);
			texture2D.SetPixel(0, 0, color);
			texture2D.Apply();
			cache(key, texture2D);
		}
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		if (isClip)
		{
			num3 = clipX;
			num4 = clipY;
			num5 = clipW;
			num6 = clipH;
			if (isTranslate)
			{
				num3 += clipTX;
				num4 += clipTY;
			}
		}
		if (isClip)
		{
			GUI.BeginGroup(new Rect(num3, num4, num5, num6));
		}
		GUI.DrawTexture(new Rect(x - num3, y - num4, w, h), texture2D);
		if (isClip)
		{
			GUI.EndGroup();
		}
	}

	public void setColor(int rgb)
	{
		int num = rgb & 0xFF;
		int num2 = (rgb >> 8) & 0xFF;
		int num3 = (rgb >> 16) & 0xFF;
		b = (float)num / 256f;
		g = (float)num2 / 256f;
		r = (float)num3 / 256f;
		a = 255f;
	}

	public void setColor(Color color)
	{
		b = color.b;
		g = color.g;
		r = color.r;
	}

	public void setBgColor(int rgb)
	{
		if (rgb != currentBGColor)
		{
			currentBGColor = rgb;
			int num = rgb & 0xFF;
			int num2 = (rgb >> 8) & 0xFF;
			int num3 = (rgb >> 16) & 0xFF;
			b = (float)num / 256f;
			g = (float)num2 / 256f;
			r = (float)num3 / 256f;
			Main.main.GetComponent<Camera>().backgroundColor = new Color(r, g, b);
		}
	}

	public void drawString(string s, float x, float y, GUIStyle style)
	{
		x *= zoomLevel;
		y *= zoomLevel;
        x += scrollPaintOffset.x;
        y += scrollPaintOffset.y;
		if (isTranslate)
		{
			x += translateX;
			y += translateY;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (isClip)
		{
			num = clipX;
			num2 = clipY;
			num3 = clipW;
			num4 = clipH;
			if (isTranslate)
			{
				num += clipTX;
				num2 += clipTY;
			}
		}
		if (isClip)
		{
			GUI.BeginGroup(new Rect(num, num2, num3, num4));
		}
		TextCalib.Record(s, style, x, y);
		GUI.Label(new Rect(x - num, y - num2, ScaleGUI.WIDTH, 100f), s, style);
		if (isClip)
		{
			GUI.EndGroup();
		}
	}

	public void setColor(int rgb, float alpha)
	{
		int num = rgb & 0xFF;
		int num2 = (rgb >> 8) & 0xFF;
		int num3 = (rgb >> 16) & 0xFF;
		b = (float)num / 256f;
		g = (float)num2 / 256f;
		r = (float)num3 / 256f;
		a = alpha;
	}

	public void drawString(string s, float x, float y, GUIStyle style, int w)
	{
		x *= zoomLevel;
		y *= zoomLevel;
        x += scrollPaintOffset.x;
        y += scrollPaintOffset.y;
		if (isTranslate)
		{
			x += translateX;
			y += translateY;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (isClip)
		{
			num = clipX;
			num2 = clipY;
			num3 = clipW;
			num4 = clipH;
			if (isTranslate)
			{
				num += clipTX;
				num2 += clipTY;
			}
		}
		if (isClip)
		{
			GUI.BeginGroup(new Rect(num, num2, num3, num4));
		}
		GUI.Label(new Rect(x - num, y - num2 - 4, w, 100f), s, style);
		if (isClip)
		{
			GUI.EndGroup();
		}
	}

	private void UpdatePos(int anchor)
	{
		Vector2 vector = new Vector2(0f, 0f);
		switch (anchor)
		{
		case 3:
			vector = new Vector2(size.x / 2f, size.y / 2f);
			break;
		case 20:
			vector = new Vector2(0f, 0f);
			break;
		case 17:
			vector = new Vector2(Screen.width / 2, 0f);
			break;
		case 24:
			vector = new Vector2(Screen.width, 0f);
			break;
		case 6:
			vector = new Vector2(0f, Screen.height / 2);
			break;
		case 10:
			vector = new Vector2(Screen.width, Screen.height / 2);
			break;
		case 36:
			vector = new Vector2(0f, Screen.height);
			break;
		case 33:
			vector = new Vector2(Screen.width / 2, Screen.height);
			break;
		case 40:
			vector = new Vector2(Screen.width, Screen.height);
			break;
		}
		pos = vector + relativePosition;
		rect = new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y);
		pivot = new Vector2(rect.xMin + rect.width * 0.5f, rect.yMin + rect.height * 0.5f);
	}

	public void drawRegion(Image arg0, int x0, int y0, int w0, int h0, int arg5, int x, int y, int arg8)
	{
		x *= zoomLevel;
		y *= zoomLevel;
		x0 *= zoomLevel;
		y0 *= zoomLevel;
		w0 *= zoomLevel;
		h0 *= zoomLevel;
		_drawRegion(arg0, x0, y0, w0, h0, arg5, x, y, arg8);
	}

	public void _drawRegion(Image image, float x0, float y0, int w, int h, int transform, float x, float y, int anchor)
	{
		_drawRegion(image, x0, y0, w, h, transform, x, y, anchor, null, GUI.color);
	}

	public void _drawRegion(Image image, float x0, float y0, int w, int h, int transform, float x, float y, int anchor, Material material, Color color)
	{
        x += scrollPaintOffset.x;
        y += scrollPaintOffset.y;
		if (image == null)
		{
			return;
		}
		if (isTranslate)
		{
			x += translateX;
			y += translateY;
		}
		float num = w;
		float num2 = h;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 1f;
		float num8 = 0f;
		int num9 = 1;
		if ((anchor & HCENTER) == HCENTER)
		{
			num5 -= num / 2f;
		}
		if ((anchor & VCENTER) == VCENTER)
		{
			num6 -= num2 / 2f;
		}
		if ((anchor & RIGHT) == RIGHT)
		{
			num5 -= num;
		}
		if ((anchor & BOTTOM) == BOTTOM)
		{
			num6 -= num2;
		}
		x += (int)num5;
		y += (int)num6;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		if (isClip)
		{
			if (transform == 5 && y + w < clipY + clipH && y > clipY)
			{
				num10 = 0;
				num11 = 0;
				num12 = GameCanvas.w;
				num13 = GameCanvas.h;
			}
			else
			{
				num10 = clipX;
				num11 = clipY;
				num12 = clipW;
				num13 = clipH;
			}
			if (isTranslate)
			{
				num10 += clipTX;
				num11 += clipTY;
			}
			Rect r = new Rect(x, y, w, h);
			Rect rect = intersectRect(r2: new Rect(num10, num11, num12, num13), r1: r);
			if (rect.width <= 0f || rect.height <= 0f)
			{
				return;
			}
			num = rect.width;
			num2 = rect.height;
			num3 = rect.x - r.x;
			num4 = rect.y - r.y;
		}
		float num14 = 0f;
		float num15 = 0f;
		switch (transform)
		{
		case 2:
			num14 += num;
			num7 = -1f;
			if (isClip)
			{
				if (num10 > x)
				{
					num8 = 0f - num3;
				}
				else if (num10 + num12 < x + w)
				{
					num8 = -(num10 + num12 - x - w);
				}
			}
			break;
		case 1:
			num9 = -1;
			num15 += num2;
			break;
		case 3:
			num9 = -1;
			num15 += num2;
			num7 = -1f;
			num14 += num;
			break;
		}
		int num16 = 0;
		int num17 = 0;
		if (transform == 5 || transform == 6 || transform == 4 || transform == 7)
		{
			matrixBackup = GUI.matrix;
			size = new Vector2(w, h);
			relativePosition = new Vector2(x, y);
			UpdatePos(3);
			switch (transform)
			{
			case 6:
				UpdatePos(3);
				break;
			case 5:
				size = new Vector2(w, h);
				UpdatePos(3);
				break;
			}
			switch (transform)
			{
			case 5:
				GUIUtility.RotateAroundPivot(90f, pivot);
				break;
			case 6:
				GUIUtility.RotateAroundPivot(270f, pivot);
				break;
			case 4:
				GUIUtility.RotateAroundPivot(270f, pivot);
				num14 += num;
				num7 = -1f;
				if (isClip)
				{
					if (num10 > x)
					{
						num8 = 0f - num3;
					}
					else if (num10 + num12 < x + w)
					{
						num8 = -(num10 + num12 - x - w);
					}
				}
				break;
			case 7:
				GUIUtility.RotateAroundPivot(270f, pivot);
				num9 = -1;
				num15 += num2;
				break;
			}
		}
		Rect destination = new Rect((float)x + num3 + num14 + (float)num16, (float)y + num4 + (float)num17 + num15, num * num7, num2 * (float)num9);
		Rect source = new Rect((x0 + num3 + num8) / (float)image.texture.width, ((float)image.texture.height - num2 - (y0 + num4)) / (float)image.texture.height, num / (float)image.texture.width, num2 / (float)image.texture.height);
		if (material == null)
		{
			Graphics.DrawTexture(destination, image.texture, source, 0, 0, 0, 0);
		}
		else
		{
			Graphics.DrawTexture(destination, image.texture, source, 0, 0, 0, 0, color, material);
		}
		if (transform == 5 || transform == 6 || transform == 4 || transform == 7)
		{
			GUI.matrix = matrixBackup;
		}
	}

	// Draws an atlas region that holds a glyph packed turned 90 degrees. (x, y) is the top-left of the upright glyph,
	// which is h wide and w tall on screen (the region is w wide and h tall in the atlas).
	public void _drawRotatedGlyph(Image image, float x0, float y0, int w, int h, float x, float y, Material material, Color color)
	{
        x += scrollPaintOffset.x;
        y += scrollPaintOffset.y;
		if (image == null)
		{
			return;
		}
		if (isTranslate)
		{
			x += translateX;
			y += translateY;
		}
		Vector2 center = new Vector2(x + h / 2f, y + w / 2f);
		Rect destination = new Rect(center.x - w / 2f, center.y - h / 2f, w, h);
		Rect source = new Rect(x0 / image.texture.width, (image.texture.height - h - y0) / image.texture.height, (float)w / image.texture.width, (float)h / image.texture.height);
		matrixBackup = GUI.matrix;
		GUIUtility.RotateAroundPivot(-90f, center);
		if (material == null)
		{
			Graphics.DrawTexture(destination, image.texture, source, 0, 0, 0, 0, color);
		}
		else
		{
			Graphics.DrawTexture(destination, image.texture, source, 0, 0, 0, 0, color, material);
		}
		GUI.matrix = matrixBackup;
	}

	public void drawImage(Image image, int x, int y, int anchor)
	{
		if (image != null)
		{
			drawRegion(image, 0, 0, getImageWidth(image), getImageHeight(image), 0, x, y, anchor);
		}
	}

	public void drawImage(Image image, int x, int y)
	{
		if (image != null)
		{
			drawRegion(image, 0, 0, getImageWidth(image), getImageHeight(image), 0, x, y, TOP | LEFT);
		}
	}

	public void drawRoundRect(int x, int y, int w, int h, int arcWidth, int arcHeight)
	{
		drawRect(x, y, w, h);
	}

	public void fillRoundRect(int x, int y, int width, int height, int arcWidth, int arcHeight)
	{
		fillRect(x, y, width, height);
	}

	public void reset()
	{
        ClearScrollPaintOffset();
		isClip = false;
		isTranslate = false;
		translateX = 0;
		translateY = 0;
	}

	public Rect intersectRect(Rect r1, Rect r2)
	{
		float num = r1.x;
		float num2 = r1.y;
		float x = r2.x;
		float y = r2.y;
		float num3 = num;
		num3 += r1.width;
		float num4 = num2;
		num4 += r1.height;
		float num5 = x;
		num5 += r2.width;
		float num6 = y;
		num6 += r2.height;
		if (num < x)
		{
			num = x;
		}
		if (num2 < y)
		{
			num2 = y;
		}
		if (num3 > num5)
		{
			num3 = num5;
		}
		if (num4 > num6)
		{
			num4 = num6;
		}
		num3 -= num;
		num4 -= num2;
		if (num3 < -30000f)
		{
			num3 = -30000f;
		}
		if (num4 < -30000f)
		{
			num4 = -30000f;
		}
		return new Rect(num, num2, (int)num3, (int)num4);
	}

	public void drawImageScale(Image image, int x, int y, int w, int h, int tranform)
	{
		x *= zoomLevel;
		y *= zoomLevel;
		w *= zoomLevel;
		h *= zoomLevel;
		if (image != null)
		{
			if (image.texture.filterMode != FilterMode.Bilinear) image.texture.filterMode = FilterMode.Bilinear; // scaled art would be jagged with point filtering
			Graphics.DrawTexture(new Rect(x + translateX + scrollPaintOffset.x, y + translateY + scrollPaintOffset.y, (tranform != 0) ? (-w) : w, h), image.texture);
		}
	}

	// Pixel art drawn at a whole-number scale with point filtering, centred on (cx, cy) in game pixels: every source
	// pixel becomes a clean scale x scale block, so the edges stay sharp (no uneven, jagged steps from a fractional scale).
	public void drawImagePixelScale(Image image, int cx, int cy, int scale)
	{
		if (image == null || image.texture == null) return;
		if (image.texture.filterMode != FilterMode.Point) image.texture.filterMode = FilterMode.Point;
		int w = image.texture.width * scale, h = image.texture.height * scale;
		float x = cx * zoomLevel + (isTranslate ? translateX : 0) - w / 2;
		float y = cy * zoomLevel + (isTranslate ? translateY : 0) - h / 2;
		Graphics.DrawTexture(new Rect(Mathf.Round(x) + scrollPaintOffset.x, Mathf.Round(y) + scrollPaintOffset.y, w, h), image.texture);
	}

	public void drawImageSimple(Image image, int x, int y)
	{
		x *= zoomLevel;
		y *= zoomLevel;
		if (image != null)
		{
			Graphics.DrawTexture(new Rect(x + scrollPaintOffset.x, y + scrollPaintOffset.y, image.w, image.h), image.texture);
		}
	}

	public static int getImageWidth(Image image)
	{
		return image.getWidth();
	}

	public static int getImageHeight(Image image)
	{
		return image.getHeight();
	}
}
