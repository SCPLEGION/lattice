using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// The vanilla mods screen is kept (drag-reorder, multi-select, requirements, Workshop) and restyled in
// place: every mod row is a card whose edge shows its state, the info pane gets a panel of its own.
[HarmonyPatch(typeof(Page_ModsConfig), "DoModRow")]
public static class Patch_ModRow
{
	public static void Prefix(Rect r, ModMetaData mod)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return;
		}
		Rect card = new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f);
		bool hover = Mouse.IsOver(card);
		float h = UiStyle.Ease(UiStyle.Key(4101, card), hover ? 1f : 0f, 10f);
		UiStyle.DrawGradient(card, UiStyle.WithAlpha(UiStyle.PanelTop, 0.65f + 0.3f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.7f + 0.2f * h));
		Color edge = !mod.VersionCompatible ? ColorLibrary.RedReadable : mod.Active ? UiStyle.Accent : UiStyle.Hairline;
		Widgets.DrawBoxSolid(new Rect(card.x, card.y, 3f + 2f * h, card.height), UiStyle.WithAlpha(edge, mod.Active || !mod.VersionCompatible ? 1f : 0.6f));
		Widgets.DrawBoxSolid(new Rect(card.x, card.yMax - 1f, card.width, 1f), UiStyle.Hairline);
	}
}

[HarmonyPatch(typeof(Page_ModsConfig), "DoModInfo")]
public static class Patch_ModInfo
{
	public static void Prefix(Rect r)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return;
		}
		UiStyle.DrawPanel(r, 0.9f, false);
		Widgets.DrawBoxSolid(new Rect(r.x, r.y, r.width, 2f), UiStyle.Accent);
	}
}

// Selection / hover highlights used by lists across the game (mods, saves, scenarios, pawn tables).
[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawHighlightSelected))]
public static class Patch_HighlightSelected
{
	public static bool Prefix(Rect r)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		UiStyle.DrawGradient(r, UiStyle.WithAlpha(UiStyle.Accent, 0.22f), UiStyle.WithAlpha(UiStyle.Accent, 0.06f));
		Widgets.DrawBoxSolid(new Rect(r.x, r.y, 3f, r.height), UiStyle.Accent);
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawHighlightIfMouseover))]
public static class Patch_HighlightMouseover
{
	public static bool Prefix(Rect r)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		if (Mouse.IsOver(r))
		{
			Widgets.DrawBoxSolid(r, UiStyle.CellHover);
			Widgets.DrawBoxSolid(new Rect(r.x, r.y, 2f, r.height), UiStyle.WithAlpha(UiStyle.Accent, 0.7f));
		}
		return false;
	}
}
