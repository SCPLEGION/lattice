using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Puts the inspect pane (selected thing, its tabs) on the same dark panel as the rest of the UI.
[HarmonyPatch(typeof(InspectPaneUtility), nameof(InspectPaneUtility.InspectPaneOnGUI))]
public static class Patch_InspectPaneOnGUI
{
	public static void Prefix(Rect inRect)
	{
		Widgets.DrawBoxSolid(inRect, UiStyle.PanelBg);
		Widgets.DrawBoxSolid(new Rect(inRect.x, inRect.y, inRect.width, 2f), UiStyle.Accent);
	}
}
