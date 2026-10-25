using HarmonyLib;
using UnityEngine;
using Verse;

namespace Lattice;

// Global backdrops: every vanilla window and menu section (and those of other mods that call the
// vanilla helpers) gets the dark gradient panel with a soft shadow and accent hairline.
[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawWindowBackground), typeof(Rect))]
public static class Patch_DrawWindowBackground
{
	public static bool Prefix(Rect rect)
	{
		UiStyle.DrawPanel(rect);
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawWindowBackground), typeof(Rect), typeof(Color))]
public static class Patch_DrawWindowBackgroundTinted
{
	public static bool Prefix(Rect rect, Color colorFactor)
	{
		UiStyle.DrawPanel(rect);
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(colorFactor, 0.08f), UiStyle.WithAlpha(colorFactor, 0f));
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawMenuSection))]
public static class Patch_DrawMenuSection
{
	public static bool Prefix(Rect rect)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.7f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.7f));
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), UiStyle.Hairline);
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), UiStyle.Hairline);
		return false;
	}
}
