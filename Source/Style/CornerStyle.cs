using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Bottom-right corner (weather, date, time controls, view toggles) and the alert rows get a panel of their
// own instead of floating text over the map.
[HarmonyPatch(typeof(GlobalControls), nameof(GlobalControls.GlobalControlsOnGUI))]
public static class Patch_GlobalControlsPanel
{
	public static void Prefix()
	{
		if (!LatticeMod.Settings.restyleWidgets || Event.current.type != EventType.Repaint)
		{
			return;
		}
		Rect panel = new Rect(UI.screenWidth - 208f, UI.screenHeight - 300f, 208f, 300f);
		UiStyle.DrawShadow(panel, 10f, 0.6f);
		UiStyle.DrawGradient(panel, UiStyle.WithAlpha(UiStyle.PanelTop, 0.82f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.9f));
		Widgets.DrawBoxSolid(new Rect(panel.x, panel.y, 2f, panel.height), UiStyle.WithAlpha(UiStyle.Accent, 0.8f));
		Widgets.DrawBoxSolid(new Rect(panel.x, panel.y, panel.width, 1f), new Color(1f, 1f, 1f, 0.12f));
	}
}

[HarmonyPatch(typeof(Alert), nameof(Alert.DrawAt))]
public static class Patch_AlertCard
{
	public static void Prefix(Alert __instance, float topY)
	{
		if (!LatticeMod.Settings.restyleWidgets || Event.current.type != EventType.Repaint)
		{
			return;
		}
		Rect card = new Rect(UI.screenWidth - 154f, topY, 154f, __instance.Height);
		UiStyle.DrawGradient(card, UiStyle.WithAlpha(UiStyle.PanelTop, 0.78f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.85f));
		Widgets.DrawBoxSolid(new Rect(card.x, card.yMax - 1f, card.width, 1f), UiStyle.Hairline);
	}
}
