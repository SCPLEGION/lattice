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

		int key = UiStyle.Key(2, rect);
		float h = UiStyle.Smooth(UiStyle.Ease(key, hover ? 1f : 0f, 9f));
		float o = UiStyle.Smooth(UiStyle.Ease(key + 1, open ? 1f : 0f, 7f));

		UiStyle.DrawGradient(rect, UiStyle.PanelTop, UiStyle.PanelBottom);
		if (h > 0f || o > 0f)
		{
			UiStyle.DrawGradient(rect, new Color(1f, 1f, 1f, 0.05f + 0.1f * h + 0.05f * o), new Color(1f, 1f, 1f, 0.01f * h));
		}
		if (o > 0f)
		{
			UiStyle.DrawGlow(new Rect(rect.x - 10f, rect.y - 14f, rect.width + 20f, 40f), UiStyle.AccentGlow, o * 0.8f);
		}
		UiStyle.DrawAccentLine(rect, Mathf.Max(o, h * 0.5f), true, 2f);
		UiStyle.DrawVLine(rect.xMax - 1f, rect.y + 6f, rect.height - 12f, 0.07f);
		if (__instance.ButtonBarPercent > 0f)
		{
			Rect bar = new Rect(rect.x, rect.yMax - 3f, rect.width * __instance.ButtonBarPercent, 3f);
			UiStyle.DrawGlow(bar.ExpandedBy(6f), UiStyle.AccentGlow, 0.9f);
			Widgets.DrawBoxSolid(bar, UiStyle.Accent);
		}

		Text.Font = GameFont.Small;
		string label = def.LabelCap;
		float labelWidth = def.LabelCapWidth;
		bool iconOnly = LatticeMod.Settings.dock && def.Icon != null;
		float iconSpace = def.Icon != null ? IconSize + 6f : 0f;
		if (iconOnly)
		{
			label = "";
			labelWidth = 0f;
			iconSpace = IconSize;
		}
		else if (labelWidth + iconSpace > rect.width - 8f)
		{
			label = def.ShortenedLabelCap;
			labelWidth = def.ShortenedLabelCapWidth;
		}
		float total = iconSpace + labelWidth;
		float x = rect.x + (rect.width - total) / 2f;
		float lift = -1f * h;
		Color content = disabled ? UiStyle.TextDim : Color.Lerp(UiStyle.TextSoft, Color.white, Mathf.Max(h, o));
		if (def.Icon != null)
		{
			Rect iconRect = new Rect(x, rect.center.y - IconSize / 2f + lift, IconSize, IconSize);
			GUI.color = new Color(0f, 0f, 0f, 0.4f * content.a);
			GUI.DrawTexture(iconRect.MovedBy(new Vector2(1f, 1.5f)), def.Icon);
			GUI.color = content;
			GUI.DrawTexture(iconRect, def.Icon);
			x += iconSpace;
		}
		GUI.color = content;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(x, rect.y + lift, labelWidth + 4f, rect.height), label);
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
		if (Mouse.IsOver(rect) && (!def.description.NullOrEmpty() || iconOnly))
		{
			string keyHint = def.hotKey != null ? $" ({def.hotKey.MainKeyLabel})" : "";
			TooltipHandler.TipRegion(rect, (def.LabelCap + keyHint).Colorize(ColorLibrary.Yellow) + "\n\n" + def.description);
		}
		return false;
	}
}
