using System;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lattice;

// Tab strips (pawn tables, inspect, research, factions, storage...), table rows and headings.
[HarmonyPatch(typeof(TabRecord), nameof(TabRecord.Draw))]
public static class Patch_TabRecordDraw
{
	private static readonly AccessTools.FieldRef<TabRecord, bool> Selected = AccessTools.FieldRefAccess<TabRecord, bool>("selected");

	public static bool Prefix(TabRecord __instance, Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		Func<bool> getter = __instance.selectedGetter;
		bool selected = getter != null ? getter() : Selected(__instance);
		Selected(__instance) = selected;
		bool hover = Mouse.IsOver(rect);
		float h = UiStyle.Ease(UiStyle.Key(5101, rect), hover ? 1f : 0f, 10f);
		float o = UiStyle.Ease(UiStyle.Key(5102, rect), selected ? 1f : 0f, 8f);

		Rect pill = new Rect(rect.x + 1f, rect.y + 2f, rect.width - 2f, rect.height);
		UiStyle.DrawGradient(pill, new Color(1f, 1f, 1f, 0.05f + 0.06f * h + 0.1f * o), new Color(1f, 1f, 1f, 0.01f + 0.03f * o));
		Widgets.DrawBoxSolid(new Rect(pill.x, pill.y, pill.width, 1f), new Color(1f, 1f, 1f, 0.08f + 0.1f * o));
		Widgets.DrawBoxSolid(new Rect(pill.x, pill.y, pill.width, 2f * o), UiStyle.Accent);

		Text.Anchor = TextAnchor.MiddleCenter;
		Text.WordWrap = false;
		Color baseColor = __instance.labelColor ?? Color.white;
		GUI.color = Color.Lerp(baseColor * UiStyle.TextSoft, baseColor, Mathf.Max(h, o));
		Widgets.Label(pill, __instance.label);
		GUI.color = Color.white;
		Text.WordWrap = true;
		Text.Anchor = TextAnchor.UpperLeft;

		MouseoverSounds.DoRegion(rect);
		if (Widgets.ButtonInvisible(rect))
		{
			__instance.clickedAction?.Invoke();
		}
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAltRect))]
public static class Patch_AltRect
{
	public static bool Prefix(Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.035f));
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawLightHighlight))]
public static class Patch_LightHighlight
{
	public static bool Prefix(Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.07f));
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawTitleBG))]
public static class Patch_TitleBG
{
	public static bool Prefix(Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.9f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.9f));
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), UiStyle.WithAlpha(UiStyle.Accent, 0.55f));
		return false;
	}
}
