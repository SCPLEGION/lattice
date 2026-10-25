using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// New-game flow (scenario, storyteller, world, landing site, pawns, ideology): shared page title and
// selectable option cards. Page logic stays vanilla.
[HarmonyPatch(typeof(Page), "DrawPageTitle")]
public static class Patch_PageTitle
{
	public static void Postfix(Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return;
		}
		float y = rect.y + 38f;
		Widgets.DrawBoxSolid(new Rect(rect.center.x - 48f, y, 96f, 2f), UiStyle.Accent);
		Widgets.DrawBoxSolid(new Rect(rect.center.x - 200f, y, 100f, 1f), UiStyle.Hairline);
		Widgets.DrawBoxSolid(new Rect(rect.center.x + 100f, y, 100f, 1f), UiStyle.Hairline);
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawOptionBackground))]
public static class Patch_OptionBackground
{
	public static bool Prefix(Rect rect, bool selected)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		bool hover = Mouse.IsOver(rect);
		float h = UiStyle.Ease(UiStyle.Key(6101, rect), hover ? 1f : 0f, 10f);
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.7f + 0.25f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.75f + 0.2f * h));
		if (selected)
		{
			Widgets.DrawBoxSolidWithOutline(rect, UiStyle.WithAlpha(UiStyle.Accent, 0.14f), UiStyle.Accent, 2);
		}
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 3f + 2f * h, rect.height), UiStyle.WithAlpha(UiStyle.Accent, selected ? 1f : 0.3f + 0.5f * h));
		return false;
	}
}
