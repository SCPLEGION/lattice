using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Restyles the base MainButtonWorker.DoButton. Subclasses that override DoButton keep their own drawing.
[HarmonyPatch(typeof(MainButtonWorker), nameof(MainButtonWorker.DoButton))]
public static class MainButtonStyle
{
	private const float IconSize = 22f;

	public static bool Prefix(MainButtonWorker __instance, Rect rect)
	{
		MainButtonDef def = __instance.def;
		bool disabled = __instance.Disabled;
		bool open = Find.MainTabsRoot.OpenTab == def;
		bool hover = Mouse.IsOver(rect) && !disabled;

		Widgets.DrawBoxSolid(rect, open ? new Color(1f, 1f, 1f, 0.12f) : hover ? UiStyle.CellHover : UiStyle.PanelBg);
		if (open)
		{
			Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 2f), UiStyle.Accent);
		}
		if (__instance.ButtonBarPercent > 0f)
		{
			Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 3f, rect.width * __instance.ButtonBarPercent, 3f), UiStyle.Accent);
		}

		Text.Font = GameFont.Small;
		string label = def.LabelCap;
		float labelWidth = def.LabelCapWidth;
		float iconSpace = def.Icon != null ? IconSize + 6f : 0f;
		if (labelWidth + iconSpace > rect.width - 8f)
		{
			label = def.ShortenedLabelCap;
			labelWidth = def.ShortenedLabelCapWidth;
		}
		float total = iconSpace + labelWidth;
		float x = rect.x + (rect.width - total) / 2f;
		GUI.color = disabled ? UiStyle.TextDim : Color.white;
		if (def.Icon != null)
		{
			GUI.DrawTexture(new Rect(x, rect.center.y - IconSize / 2f, IconSize, IconSize), def.Icon);
			x += iconSpace;
		}
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(x, rect.y, labelWidth + 4f, rect.height), label);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;

		if (disabled)
		{
			if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
			{
				Event.current.Use();
			}
			return false;
		}
		if (Widgets.ButtonInvisible(rect))
		{
			__instance.InterfaceTryActivate();
		}
		if (!open && !Find.WindowStack.NonImmediateDialogWindowOpen)
		{
			UIHighlighter.HighlightOpportunity(rect, def.cachedHighlightTagClosed);
		}
		if (Mouse.IsOver(rect) && !def.description.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, def.LabelCap.Colorize(ColorLibrary.Yellow) + "\n\n" + def.description);
		}
		return false;
	}
}
