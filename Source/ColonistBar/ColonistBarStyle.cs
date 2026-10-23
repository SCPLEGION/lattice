using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Restyles the colonist bar: a dark cell behind each portrait and an accent outline on selected colonists.
// Both patches only add drawing, so vanilla clicks, drag-reordering and overlays are untouched.
[HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
public static class Patch_DrawColonist
{
	public static void Prefix(Rect rect)
	{
		Widgets.DrawBoxSolid(rect, UiStyle.PanelBg);
	}
}

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawSelectionOverlayOnGUI", new[] { typeof(Pawn), typeof(Rect) })]
public static class Patch_DrawSelectionOverlay
{
	public static void Postfix(Pawn colonist, Rect rect)
	{
		if (Find.Selector.IsSelected(colonist))
		{
			Widgets.DrawBox(rect, 2);
			Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), UiStyle.Accent);
		}
	}
}
