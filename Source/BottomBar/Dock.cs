using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Replaces the full-width bottom bar with a centered dock. The bar height stays at the vanilla 36 px
// (MainButtonDef.ButtonHeight is a compile-time constant that tab windows are positioned against).
[HarmonyPatch(typeof(MainButtonsRoot), "DoButtons")]
public static class Patch_Dock
{
	private const float Height = 36f;
	private const float IconButtonWidth = 52f;

	private static readonly AccessTools.FieldRef<MainButtonsRoot, List<MainButtonDef>> All =
		AccessTools.FieldRefAccess<MainButtonsRoot, List<MainButtonDef>>("allButtonsInOrder");

	public static bool Prefix(MainButtonsRoot __instance)
	{
		if (!LatticeMod.Settings.dock)
		{
			return true;
		}
		List<MainButtonDef> visible = All(__instance).Where(d => d.Worker.Visible).ToList();
		if (visible.Count == 0)
		{
			return false;
		}
		Text.Font = GameFont.Small;
		float[] widths = visible.Select(d => d.Icon != null ? IconButtonWidth : Mathf.Max(64f, d.LabelCapWidth + 24f)).ToArray();
		float total = widths.Sum();
		float x = (UI.screenWidth - total) / 2f;
		float y = UI.screenHeight - Height;

		Rect dock = new Rect(x - 10f, y, total + 20f, Height);
		UiStyle.DrawShadow(dock, 8f, 0.7f);
		UiStyle.DrawGradient(dock, UiStyle.WithAlpha(UiStyle.PanelTop, 0.92f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.96f));
		Widgets.DrawBoxSolid(new Rect(dock.x, dock.y, dock.width, 1f), new Color(1f, 1f, 1f, 0.14f));

		for (int i = 0; i < visible.Count; i++)
		{
			visible[i].Worker.DoButton(new Rect(x, y, widths[i], Height));
			x += widths[i];
		}
		GUI.color = Color.white;
		return false;
	}
}
