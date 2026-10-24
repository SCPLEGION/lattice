using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Lattice;

// Shared design system: palette, procedural textures, animation helpers and the icon cell widget.
public static class UiStyle
{
	// Palette
	public static readonly Color PanelTop = new Color(0.115f, 0.13f, 0.16f, 0.96f);
	public static readonly Color PanelBottom = new Color(0.045f, 0.05f, 0.065f, 0.97f);
	public static readonly Color PanelBg = new Color(0.07f, 0.08f, 0.1f, 0.96f);
	public static readonly Color CellBg = new Color(1f, 1f, 1f, 0.045f);
	public static readonly Color CellHover = new Color(1f, 1f, 1f, 0.12f);
	public static readonly Color Accent = new Color(0.98f, 0.74f, 0.28f);
	public static readonly Color AccentGlow = new Color(1f, 0.62f, 0.15f, 0.55f);
	public static readonly Color Highlight = new Color(1f, 1f, 1f, 0.16f);
	public static readonly Color Hairline = new Color(1f, 1f, 1f, 0.07f);
	public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
	public static readonly Color TextDim = new Color(1f, 1f, 1f, 0.45f);
	public static readonly Color TextSoft = new Color(0.86f, 0.88f, 0.92f, 0.85f);

	// Metrics
	public const float BarHeight = 36f;
	public const float Pad = 6f;
	public const float CellSize = 84f;
	public const float InfoWidth = 250f;
	public const float DescAlpha = 0.8f;
	public const float DividerAlpha = 0.1f;
	public const float HoverIconScale = 0.08f;

	// Procedural textures, built once and cached so no asset files are needed.
	private static Texture2D gradient;
	private static Texture2D glow;

	private static Texture2D Gradient => gradient ??= BuildGradient();
	private static Texture2D Glow => glow ??= BuildGlow();

	private static Texture2D BuildGradient()
	{
		Texture2D t = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
		for (int y = 0; y < 64; y++)
		{
			// Row 0 is the bottom in Unity textures; store white to transparent so it can be tinted.
			t.SetPixel(0, y, new Color(1f, 1f, 1f, y / 63f));
		}
		t.Apply(false, true);
		return t;
	}

	private static Texture2D BuildGlow()
	{
		const int size = 64;
		Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dx = (x - size / 2f + 0.5f) / (size / 2f);
				float dy = (y - size / 2f + 0.5f) / (size / 2f);
				float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
				t.SetPixel(x, y, new Color(1f, 1f, 1f, d * d));
			}
		}
		t.Apply(false, true);
		return t;
	}

	// Animation: per-key eased floats. Stepped only on Repaint so the multiple OnGUI events per frame do not speed it up.
	private static readonly Dictionary<int, float> eased = new Dictionary<int, float>();

	public static int Key(int salt, Rect rect) => HashCode.Combine(salt, Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y));

	public static float Ease(int key, float target, float speed = 12f)
	{
		eased.TryGetValue(key, out float v);
		if (Event.current != null && Event.current.type == EventType.Repaint)
		{
			v = Mathf.MoveTowards(v, target, Mathf.Max(0.0001f, speed) * Time.unscaledDeltaTime);
			eased[key] = v;
		}
		return v;
	}

	public static void Reset(int key) => eased.Remove(key);

	public static float Smooth(float t) => t * t * (3f - 2f * t);

	public static float Pulse(float speed = 2f) => 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * speed);

	public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);

	// Drawing helpers
	public static void DrawGradient(Rect rect, Color top, Color bottom)
	{
		GUI.color = bottom;
		GUI.DrawTexture(rect, BaseContent.WhiteTex);
		GUI.color = top;
		GUI.DrawTexture(rect, Gradient);
		GUI.color = Color.white;
	}

	public static void DrawGlow(Rect rect, Color color, float strength)
	{
		if (strength <= 0.001f)
		{
			return;
		}
		GUI.color = WithAlpha(color, Mathf.Clamp01(strength));
		GUI.DrawTexture(rect, Glow);
		GUI.color = Color.white;
	}

	// Soft drop shadow built from stacked translucent boxes around the rect.
	public static void DrawShadow(Rect rect, float spread = 10f, float alpha = 1f)
	{
		const int steps = 5;
		for (int i = steps; i >= 1; i--)
		{
			float e = spread * i / steps;
			Widgets.DrawBoxSolid(rect.ExpandedBy(e), WithAlpha(Shadow, alpha * 0.07f));
		}
	}

	public static void DrawPanel(Rect rect, float alpha = 1f, bool shadow = true)
	{
		if (shadow)
		{
			DrawShadow(rect, 10f, alpha);
		}
		DrawGradient(rect, WithAlpha(PanelTop, alpha), WithAlpha(PanelBottom, alpha));
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), WithAlpha(Highlight, alpha));
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), WithAlpha(Hairline, alpha));
	}

	public static void DrawVLine(float x, float y, float h, float alpha)
	{
		Widgets.DrawBoxSolid(new Rect(x, y, 1f, h), new Color(1f, 1f, 1f, alpha));
	}

	// Accent underline that grows from the center; t in 0..1.
	public static void DrawAccentLine(Rect rect, float t, bool top, float thickness = 2f)
	{
		if (t <= 0.001f)
		{
			return;
		}
		float w = rect.width * Smooth(t);
		float y = top ? rect.y : rect.yMax - thickness;
		Rect line = new Rect(rect.center.x - w / 2f, y, w, thickness);
		DrawGlow(new Rect(line.x - 8f, y - (top ? 4f : 10f), line.width + 16f, 16f), AccentGlow, t);
		Widgets.DrawBoxSolid(line, WithAlpha(Accent, t));
	}

	// Draws one square icon cell with the name beneath the icon. Returns true when clicked.
	public static bool IconCell(Rect rect, Texture icon, string label, bool selected, bool disabled, bool dim,
		float iconScale = 1f, Vector2? iconProportions = null, Rect? texCoords = null, float angle = 0f, Color? iconColor = null)
	{
		int key = Key(1, rect);
		bool hover = Mouse.IsOver(rect) && !disabled;
		float h = Smooth(Ease(key, hover ? 1f : 0f, 9f));
		float s = Smooth(Ease(key + 1, selected ? 1f : 0f, 9f));

		Widgets.DrawBoxSolid(rect, CellBg);
		if (h > 0f)
		{
			DrawGradient(rect, WithAlpha(CellHover, h), WithAlpha(CellHover, h * 0.35f));
		}
		if (s > 0f)
		{
			DrawGlow(rect.ExpandedBy(10f), AccentGlow, s * 0.7f);
			DrawGradient(rect, new Color(Accent.r, Accent.g, Accent.b, 0.26f * s), new Color(Accent.r, Accent.g, Accent.b, 0.06f * s));
			Widgets.DrawBoxSolidWithOutline(rect, Color.clear, WithAlpha(Accent, s), 1);
		}
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f + 0.1f * h));
		DrawAccentLine(rect, h * (selected ? 0f : 1f), false, 2f);

		float alpha = disabled || dim ? 0.35f : 1f;
		Rect iconRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 34f);
		iconRect = iconRect.ExpandedBy(iconRect.width * HoverIconScale * h * 0.5f);
		if (icon != null)
		{
			GUI.color = new Color(0f, 0f, 0f, 0.35f * alpha);
			Widgets.DrawTextureFitted(iconRect.MovedBy(new Vector2(1f, 2f)), icon, iconScale, iconProportions ?? Vector2.one, texCoords ?? new Rect(0f, 0f, 1f, 1f), angle);
			GUI.color = (iconColor ?? Color.white) * new Color(1f, 1f, 1f, alpha);
			Widgets.DrawTextureFitted(iconRect, icon, iconScale, iconProportions ?? Vector2.one, texCoords ?? new Rect(0f, 0f, 1f, 1f), angle);
			GUI.color = Color.white;
		}

		Rect labelRect = new Rect(rect.x + 2f, rect.yMax - 28f, rect.width - 4f, 28f);
		DrawGradient(new Rect(rect.x, rect.yMax - 30f, rect.width, 30f), new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.4f));
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.UpperCenter;
		Text.WordWrap = true;
		GUI.color = Color.Lerp(new Color(1f, 1f, 1f, alpha * 0.85f), new Color(1f, 1f, 1f, alpha), Mathf.Max(h, s));
		Widgets.Label(labelRect, label.Truncate(labelRect.width * 2f - 6f));
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
		return !disabled && Widgets.ButtonInvisible(rect);
	}
}
