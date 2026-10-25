using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Cinematic main menu: letterbox bars, a dark falloff on the left where the menu sits, a slow
// breathing glow along the bottom and a panel behind the option list.
[HarmonyPatch(typeof(UI_BackgroundMain), "DoOverlay")]
public static class Patch_MenuOverlay
{
	private const int Strips = 48;

	public static void Postfix(Rect bgRect)
	{
		float bar = bgRect.height * 0.09f;
		Color black = new Color(0f, 0f, 0f, 0.92f);
		UiStyle.DrawGradient(new Rect(bgRect.x, bgRect.y, bgRect.width, bar * 2.2f), black, new Color(0f, 0f, 0f, 0f));
		UiStyle.DrawGradient(new Rect(bgRect.x, bgRect.yMax - bar * 2.6f, bgRect.width, bar * 2.6f), new Color(0f, 0f, 0f, 0f), black);

		float width = bgRect.width * 0.55f;
		float step = width / Strips;
		for (int i = 0; i < Strips; i++)
		{
			float t = 1f - i / (float)Strips;
			Widgets.DrawBoxSolid(new Rect(bgRect.x + i * step, bgRect.y, step + 1f, bgRect.height), new Color(0.02f, 0.025f, 0.04f, 0.78f * t * t));
		}

		Rect glow = new Rect(bgRect.x, bgRect.yMax - bar * 1.6f, bgRect.width, bar * 1.6f);
		UiStyle.DrawGlow(glow, UiStyle.AccentGlow, 0.22f + 0.1f * UiStyle.Pulse(0.8f));
		Widgets.DrawBoxSolid(new Rect(bgRect.x, bgRect.yMax - bar * 0.9f, bgRect.width, 1f), UiStyle.WithAlpha(UiStyle.Accent, 0.5f));
	}
}

[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
public static class Patch_MainMenuControls
{
	public static void Prefix(Rect rect)
	{
		Rect panel = rect.ExpandedBy(18f);
		UiStyle.DrawPanel(panel, 0.9f);
		Widgets.DrawBoxSolid(new Rect(panel.x, panel.y + 10f, 3f, panel.height - 20f), UiStyle.Accent);
	}
}
