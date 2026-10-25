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
	private static Texture2D fadeTex;

	// 1px-high horizontal alpha ramp (opaque -> clear), stretched over the shaded area: no seams.
	private static Texture2D FadeTex
	{
		get
		{
			if (fadeTex == null)
			{
				const int w = 128;
				fadeTex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
				for (int i = 0; i < w; i++)
				{
					float t = 1f - i / (float)(w - 1);
					fadeTex.SetPixel(i, 0, new Color(0.02f, 0.025f, 0.04f, 0.78f * t * t));
				}
				fadeTex.Apply();
			}
			return fadeTex;
		}
	}

	public static void Postfix()
	{
		// The background rect extends past the visible artwork, so shade the screen, not bgRect.
		Rect screen = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
		GUI.DrawTexture(new Rect(0f, 0f, screen.width * 0.5f, screen.height), FadeTex);
		float top = screen.height * 0.12f;
		UiStyle.DrawGradient(new Rect(0f, 0f, screen.width, top), new Color(0f, 0f, 0f, 0.7f), new Color(0f, 0f, 0f, 0f));
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
