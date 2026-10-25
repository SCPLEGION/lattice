using HarmonyLib;
using UnityEngine;
using Verse;

namespace Lattice;

// Every vanilla text button (menus, dialogs, mod screens) gets the gradient card look. Same return value
// as vanilla: true on click. Both ButtonText overloads are patched because one may not delegate to the other.
public static class ButtonStyle
{
	public static bool Draw(Rect rect, string label, bool drawBackground, bool doMouseoverSound, Color textColor, bool active, TextAnchor? anchor)
	{
		bool hover = active && Mouse.IsOver(rect);
		float h = UiStyle.Ease(System.HashCode.Combine(UiStyle.Key(3301, rect), label), hover ? 1f : 0f);
		if (drawBackground)
		{
			UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.7f + 0.3f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.75f + 0.2f * h));
			Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f + 0.1f * h));
			Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 2f, rect.width * h, 2f), UiStyle.Accent);
		}
		else if (hover)
		{
			Widgets.DrawBoxSolid(rect, UiStyle.CellHover);
		}
		Text.Anchor = anchor ?? TextAnchor.MiddleCenter;
		GUI.color = active ? Color.Lerp(textColor * new Color(1f, 1f, 1f, 0.85f), textColor, h) : UiStyle.TextDim;
		Widgets.Label(rect, label);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		if (!active)
		{
			return false;
		}
		if (doMouseoverSound)
		{
			Verse.Sound.MouseoverSounds.DoRegion(rect);
		}
		return Widgets.ButtonInvisible(rect, false);
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText), typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?))]
public static class Patch_ButtonText
{
	public static bool Prefix(Rect rect, string label, bool drawBackground, bool doMouseoverSound, bool active, TextAnchor? overrideTextAnchor, ref bool __result)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		__result = ButtonStyle.Draw(rect, label, drawBackground, doMouseoverSound, Color.white, active, overrideTextAnchor);
		return false;
	}
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText), typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(Color), typeof(bool), typeof(TextAnchor?))]
public static class Patch_ButtonTextColored
{
	public static bool Prefix(Rect rect, string label, bool drawBackground, bool doMouseoverSound, Color textColor, bool active, TextAnchor? overrideTextAnchor, ref bool __result)
	{
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
		__result = ButtonStyle.Draw(rect, label, drawBackground, doMouseoverSound, textColor, active, overrideTextAnchor);
		return false;
	}
}
