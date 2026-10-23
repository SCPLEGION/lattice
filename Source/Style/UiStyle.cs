using UnityEngine;
using Verse;

namespace Lattice;

public static class UiStyle
{
	public static readonly Color PanelBg = new Color(0.09f, 0.1f, 0.11f, 0.94f);
	public static readonly Color CellBg = new Color(1f, 1f, 1f, 0.05f);
	public static readonly Color CellHover = new Color(1f, 1f, 1f, 0.14f);
	public static readonly Color Accent = new Color(0.95f, 0.72f, 0.25f);
	public static readonly Color TextDim = new Color(1f, 1f, 1f, 0.45f);

	public const float BarHeight = 36f;
	public const float Pad = 6f;

	// Draws one square icon cell with the name beneath the icon. Returns true when clicked.
	public static bool IconCell(Rect rect, Texture icon, string label, bool selected, bool disabled, bool dim,
		float iconScale = 1f, Vector2? iconProportions = null, Rect? texCoords = null, float angle = 0f, Color? iconColor = null)
	{
		bool hover = Mouse.IsOver(rect) && !disabled;
		Widgets.DrawBoxSolid(rect, hover ? CellHover : CellBg);
		if (selected)
		{
			Widgets.DrawBoxSolidWithOutline(rect, new Color(Accent.r, Accent.g, Accent.b, 0.18f), Accent, 2);
		}
		float alpha = disabled || dim ? 0.35f : 1f;
		Rect iconRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 34f);
		if (icon != null)
		{
			GUI.color = (iconColor ?? Color.white) * new Color(1f, 1f, 1f, alpha);
			Widgets.DrawTextureFitted(iconRect, icon, iconScale, iconProportions ?? Vector2.one, texCoords ?? new Rect(0f, 0f, 1f, 1f), angle);
			GUI.color = Color.white;
		}
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.UpperCenter;
		Text.WordWrap = true;
		GUI.color = new Color(1f, 1f, 1f, alpha);
		Rect labelRect = new Rect(rect.x + 2f, rect.yMax - 28f, rect.width - 4f, 28f);
		Widgets.Label(labelRect, label.Truncate(labelRect.width * 2f - 6f));
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
		return !disabled && Widgets.ButtonInvisible(rect);
	}
}
