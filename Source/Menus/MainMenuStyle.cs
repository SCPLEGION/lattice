using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Cinematic main menu: no boxes, no bars. A dark falloff on the left where the menu sits and a soft vignette;
// the options are bare, shadowed text with a faint accent tick on hover.
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
		// The option list sits on the right: shade that side, keep the left light.
		GUI.color = new Color(1f, 1f, 1f, LatticeMod.Settings.menuDim);
		GUI.DrawTextureWithTexCoords(new Rect(screen.width * 0.45f, 0f, screen.width * 0.55f, screen.height), FadeTex, new Rect(1f, 0f, -1f, 1f));
		GUI.color = new Color(1f, 1f, 1f, 0.45f * LatticeMod.Settings.menuDim);
		GUI.DrawTexture(new Rect(0f, 0f, screen.width * 0.2f, screen.height), FadeTex);
		GUI.color = Color.white;
		// Only a very soft top shade; nothing along the bottom edge (no letterbox look).
		UiStyle.DrawGradient(new Rect(0f, 0f, screen.width, screen.height * 0.14f), new Color(0f, 0f, 0f, 0.35f), new Color(0f, 0f, 0f, 0f));
	}
}

[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
public static class Patch_MainMenuControls
{
	public static bool Active;

	// No panel: the options float over the artwork, readable thanks to the dark falloff behind them.
	public static void Prefix() => Active = true;

	public static void Finalizer() => Active = false;

	// Language picker: drawn as a quiet text link in the bottom-right corner instead of a box under the links.
	public static void Postfix()
	{
		if (Current.ProgramState != ProgramState.Entry)
		{
			return;
		}
		string label = LanguageDatabase.activeLanguage.FriendlyNameNative;
		Rect r = new Rect(UI.screenWidth - 190f, UI.screenHeight - 46f, 170f, 28f);
		bool hover = Mouse.IsOver(r);
		float h = UiStyle.Ease(UiStyle.Key(2901, r), hover ? 1f : 0f);
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = new Color(0f, 0f, 0f, 0.8f);
		Widgets.Label(r.MovedBy(new Vector2(1f, 1.5f)), label);
		GUI.color = Color.Lerp(UiStyle.TextSoft, UiStyle.Accent, h);
		Widgets.Label(r, label);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		if (Widgets.ButtonInvisible(r))
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			foreach (LoadedLanguage lang in LanguageDatabase.AllLoadedLanguages)
			{
				LoadedLanguage pick = lang;
				options.Add(new FloatMenuOption(pick.DisplayName, () =>
				{
					LanguageDatabase.SelectLanguage(pick);
					Prefs.Save();
				}));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}
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
		bool drift = LatticeMod.Settings.kenBurns;
		float zoom = drift ? 1.06f + 0.04f * Mathf.Sin(t * 0.045f) : 1f;
		float w = tex.width * cover * zoom, h = tex.height * cover * zoom;
		float x = (sw - w) / 2f + (drift ? Mathf.Sin(t * 0.03f) * (w - sw) * 0.4f : 0f);
		float y = (sh - h) / 2f + (drift ? Mathf.Cos(t * 0.025f) * (h - sh) * 0.4f : 0f);
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
	private const float Gap = 2f;
	private static Texture2D Fade => FadeRamp ??= Ramp();
	private static Texture2D FadeRamp;

	// Horizontal white ramp (opaque -> clear) tinted per draw call.
	private static Texture2D Ramp()
	{
		Texture2D t = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
		for (int i = 0; i < 64; i++)
		{
			t.SetPixel(i, 0, new Color(1f, 1f, 1f, 1f - i / 63f));
		}
		t.Apply();
		return t;
	}
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
			// A faint light wash that follows the cursor, and a thin accent line that draws itself under the label.
			GUI.color = new Color(1f, 0.85f, 0.55f, 0.16f * h);
			GUI.DrawTexture(new Rect(row.x, row.y, row.width * (0.5f + 0.5f * h), row.height), Fade);
			GUI.color = Color.white;
			Widgets.DrawBoxSolid(new Rect(row.x + 2f, row.y + row.height * 0.25f, 2f, row.height * 0.5f * h), UiStyle.WithAlpha(UiStyle.Accent, 0.55f * h));
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleLeft;
			string label = LatticeMod.Settings.menuUppercase ? opt.label.ToUpperInvariant() : opt.label;
			Rect text = new Rect(row.x + 8f + 10f * h, row.y, row.width - 16f, row.height);
			GUI.color = new Color(0f, 0f, 0f, 0.85f);
			Widgets.Label(text.MovedBy(new Vector2(1.5f, 2f)), label);
			GUI.color = Color.Lerp(UiStyle.TextSoft, UiStyle.WithAlpha(UiStyle.Accent, 1f), h);
			Widgets.Label(text, label);
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

// DLC icons in the bottom-left corner drawn smaller (scaled around the corner; hit-testing follows the matrix).
[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoExpansionIcons))]
public static class Patch_ExpansionIcons
{
	private const float Scale = 0.62f;

	public static void Prefix(out Matrix4x4 __state)
	{
		__state = GUI.matrix;
		Matrix4x4 pivot = Matrix4x4.Translate(new Vector3(0f, UI.screenHeight, 0f));
		GUI.matrix = __state * pivot * Matrix4x4.Scale(new Vector3(Scale, Scale, 1f)) * pivot.inverse;
	}

	public static void Finalizer(Matrix4x4 __state) => GUI.matrix = __state;
}

// The translucent dark slab vanilla draws behind the link column.
[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D))]
public static class Patch_MenuLinksSlab
{
	public static bool Prefix(Texture2D atlas) => !(Patch_MainMenuControls.Active && atlas != null && atlas.name == "SubtleGradient");
}

// The vanilla language button (a box under the links) is swallowed; Patch_MainMenuControls draws its own.
[HarmonyPatch]
public static class Patch_MenuLanguageButton
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Widgets)))
		{
			ParameterInfo[] ps = m.GetParameters();
			if (m.Name == "ButtonText" && ps.Length >= 2 && ps[0].ParameterType == typeof(Rect) && ps[1].ParameterType == typeof(string))
			{
				yield return m;
			}
		}
	}

	public static bool Prefix(string label, ref bool __result)
	{
		if (Patch_MainMenuControls.Active && Current.ProgramState == ProgramState.Entry && label == LanguageDatabase.activeLanguage.FriendlyNameNative)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
