using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Bills tab of work tables (stoves, benches...): every bill becomes a card with an accent edge.
// Logic, buttons and layout stay vanilla; only the backdrop is drawn.
[HarmonyPatch(typeof(Bill), nameof(Bill.DoInterface))]
public static class Patch_BillDoInterface
{
	private const float BillHeight = 53f;

	public static void Prefix(float x, float y, float width)
	{
		Rect card = new Rect(x, y, width, BillHeight - 4f);
		bool hover = Mouse.IsOver(card);
		int key = UiStyle.Key(1301, card);
		float h = UiStyle.Ease(key, hover ? 1f : 0f);
		UiStyle.DrawGradient(card, UiStyle.WithAlpha(UiStyle.PanelTop, 0.8f + 0.2f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.9f));
		Widgets.DrawBoxSolid(new Rect(card.x, card.y, 3f, card.height), UiStyle.WithAlpha(UiStyle.Accent, 0.45f + 0.55f * h));
		Widgets.DrawBoxSolid(new Rect(card.x, card.yMax - 1f, card.width, 1f), UiStyle.Hairline);
	}
}

// Workbench "Add bill" list and the bill configuration dialog share the global panel style below.
[HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
public static class Patch_BillConfigContents
{
	public static void Prefix(Rect inRect)
	{
		UiStyle.DrawGradient(inRect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.35f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.35f));
		Widgets.DrawBoxSolid(new Rect(inRect.x, inRect.y, inRect.width, 2f), UiStyle.Accent);
	}
}
