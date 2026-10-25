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

	public static void Postfix() => Draw();

	public static void Draw()
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

// Replaces the vanilla artwork with a slowly drifting colony screenshot when one is available.
[HarmonyPatch(typeof(UI_BackgroundMain), nameof(UI_BackgroundMain.BackgroundOnGUI))]
public static class Patch_MenuBackground
{
	private static float fade;
	private static string shown;

	public static bool Prefix()
	{
		Backdrops.Entry entry = Backdrops.Pick();
		Texture2D tex = Backdrops.Texture(entry);
		if (tex == null)
		{
			return true;
		}
		if (shown != entry.image)
		{
			shown = entry.image;
			fade = 0f;
		}
		fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime * 0.8f);

		float sw = UI.screenWidth, sh = UI.screenHeight;
		float t = Time.realtimeSinceStartup;
		// Cover the screen, then add a slow zoom + drift (Ken Burns).
		float cover = Mathf.Max(sw / tex.width, sh / tex.height);
		float zoom = 1.06f + 0.04f * Mathf.Sin(t * 0.045f);
		float w = tex.width * cover * zoom, h = tex.height * cover * zoom;
		float x = (sw - w) / 2f + Mathf.Sin(t * 0.03f) * (w - sw) * 0.4f;
		float y = (sh - h) / 2f + Mathf.Cos(t * 0.025f) * (h - sh) * 0.4f;
		Widgets.DrawBoxSolid(new Rect(0f, 0f, sw, sh), Color.black);
		GUI.color = new Color(1f, 1f, 1f, UiStyle.Smooth(fade));
		GUI.DrawTexture(new Rect(x, y, w, h), tex, ScaleMode.StretchToFill);
		GUI.color = Color.white;
		Patch_MenuOverlay.Draw();
		return false;
	}
}

// Menu rows (main menu and the in-game pause menu): tall rows with an accent edge that grows on hover.
[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
public static class Patch_OptionListing
{
	private const float RowHeight = 46f;
	private const float Gap = 4f;
	private static readonly AccessTools.FieldRef<ListableOption, string> HighlightTag = AccessTools.FieldRefAccess<ListableOption, string>("uiHighlightTag");

	public static bool Prefix(Rect rect, System.Collections.Generic.List<ListableOption> optList, ref float __result)
	{
		float y = 0f;
		GUI.BeginGroup(rect);
		for (int i = 0; i < optList.Count; i++)
		{
			ListableOption opt = optList[i];
			if (opt.GetType() != typeof(ListableOption))
			{
				y += opt.DrawOption(new Vector2(0f, y), rect.width) + Gap;
				continue;
			}
			float rowH = Mathf.Max(RowHeight, opt.minHeight);
			Rect row = new Rect(0f, y, rect.width, rowH);
			bool hover = Mouse.IsOver(row);
			float h = UiStyle.Ease(UiStyle.Key(2201 + i, row), hover ? 1f : 0f);
			UiStyle.DrawGradient(row, UiStyle.WithAlpha(UiStyle.PanelTop, 0.55f + 0.4f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.55f + 0.3f * h));
			Widgets.DrawBoxSolid(new Rect(row.x, row.y, 3f + 5f * h, row.height), UiStyle.WithAlpha(UiStyle.Accent, 0.35f + 0.65f * h));
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = Color.Lerp(UiStyle.TextSoft, Color.white, h);
			Widgets.Label(new Rect(row.x + 18f + 6f * h, row.y, row.width - 24f, row.height), opt.label);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
			string tag = HighlightTag(opt);
			if (!tag.NullOrEmpty())
			{
				UIHighlighter.HighlightOpportunity(row, tag);
			}
			if (Widgets.ButtonInvisible(row))
			{
				opt.action?.Invoke();
			}
			y += rowH + Gap;
		}
		GUI.EndGroup();
		__result = y;
		return false;
	}
}
