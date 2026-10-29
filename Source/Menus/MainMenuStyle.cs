using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace Lattice;

// Cinematic main menu. The whole title screen is laid out here: a full-bleed colony backdrop with a dark falloff on
// the left, the game title, a "continue" card for the latest save, bare text options that slide in, and a quiet
// bottom bar (DLC icons, web links, language, version). The option list itself still comes from vanilla
// DoMainMenuControls, so options other mods add keep showing up.
public static class MainMenuStyle
{
	public static bool On => LatticeMod.Settings.restyleWidgets && LatticeMod.Settings.cinematicMenu;

	private const float MaxColumnWidth = 400f;
	private const float RowHeight = 46f;
	private const float RowGap = 2f;

	// Width of the option column this frame (DrawOptionListing only gets vanilla's narrow rect).
	public static float ColumnWidth = MaxColumnWidth;

	// Web links captured from the second DrawOptionListing call, drawn as icons in the bottom bar.
	public static List<ListableOption> Links = new List<ListableOption>();

	private static Texture2D texTitle;
	private static Texture2D texLudeon;
	private static Texture2D TexTitle => texTitle ??= ContentFinder<Texture2D>.Get("UI/HeroArt/GameTitle");
	private static Texture2D TexLudeon => texLudeon ??= ContentFinder<Texture2D>.Get("UI/HeroArt/LudeonLogoSmall");

	private static readonly AccessTools.FieldRef<bool> AnyMapFiles = AccessTools.StaticFieldRefAccess<bool>(AccessTools.Field(typeof(MainMenuDrawer), "anyMapFiles"));
	private static readonly AccessTools.FieldRef<ListableOption, string> HighlightTag = AccessTools.FieldRefAccess<ListableOption, string>("uiHighlightTag");

	// The menu counts as freshly opened when it was not drawn for a couple of frames (game start, back from a page).
	private static int lastFrame = -10;
	private static float openedAt;
	private static FileInfo latestSave;
	private static bool lookedForSave;

	private static void Touch()
	{
		if (Time.frameCount - lastFrame > 2)
		{
			openedAt = Time.realtimeSinceStartup;
			lookedForSave = false;
		}
		lastFrame = Time.frameCount;
	}

	// Staggered entrance: 0..1 for the i-th element, instant when animations are off.
	private static float Intro(int i)
	{
		if (!LatticeMod.Settings.animations)
		{
			return 1f;
		}
		return UiStyle.Smooth(Mathf.Clamp01((Time.realtimeSinceStartup - openedAt) * 2.6f - i * 0.09f));
	}

	// Looked up once per menu visit, not every frame (it lists the saves folder).
	private static FileInfo LatestSave()
	{
		if (!lookedForSave)
		{
			lookedForSave = true;
			try
			{
				latestSave = GenFilePaths.AllSavedGameFiles.FirstOrDefault();
			}
			catch (Exception ex)
			{
				latestSave = null;
				Log.Warning("[Lattice] Could not list saves: " + ex.Message);
			}
		}
		return latestSave;
	}

	public static void Draw()
	{
		Touch();
		float sw = UI.screenWidth, sh = UI.screenHeight;
		float left = Mathf.Clamp(sw * 0.065f, 32f, 120f);
		ColumnWidth = Mathf.Min(MaxColumnWidth, sw - left * 2f);
		float y = Mathf.Max(36f, sh * 0.11f);

		y = DrawTitle(left, y);

		FileInfo save = LatticeMod.Settings.menuContinue ? LatestSave() : null;
		if (save != null)
		{
			DrawContinue(new Rect(left, y, ColumnWidth, 78f), save);
			y += 78f + 22f;
		}

		Links = new List<ListableOption>();
		MainMenuDrawer.DoMainMenuControls(new Rect(left, y, ColumnWidth, sh - y - 72f), AnyMapFiles());

		DrawBottomBar(left, sw, sh);

		GUI.color = new Color(1f, 1f, 1f, 0.35f * Intro(0));
		GUI.DrawTexture(new Rect(sw - 8f - 160f, 10f, 160f, 46f), TexLudeon, ScaleMode.StretchToFill, true);
		GUI.color = Color.white;

		MainMenuDrawer.DoTranslationInfoRect(new Rect(sw - 316f, 70f, 300f, 400f));
		SteamDeck.ShowSteamDeckMainMenuControlsIfNotKnown();
	}

	private static float DrawTitle(float left, float y)
	{
		float a = Intro(0);
		float w = Mathf.Min(ColumnWidth + 120f, 560f);
		float h = w * 146f / 1032f;
		Rect title = new Rect(left - 6f, y + (1f - a) * 14f, w, h);
		GUI.color = new Color(0f, 0f, 0f, 0.5f * a);
		GUI.DrawTexture(title.MovedBy(new Vector2(2f, 3f)), TexTitle, ScaleMode.StretchToFill, true);
		GUI.color = new Color(1f, 1f, 1f, a);
		GUI.DrawTexture(title, TexTitle, ScaleMode.StretchToFill, true);
		y = title.yMax + 6f;

		Text.Font = GameFont.Small;
		GUI.color = UiStyle.WithAlpha(UiStyle.TextDim, a);
		Widgets.Label(new Rect(left, y, ColumnWidth, 24f), "MainPageCredit".Translate());
		GUI.color = Color.white;
		y += 30f;
		Widgets.DrawBoxSolid(new Rect(left, y, 44f * a, 2f), UiStyle.WithAlpha(UiStyle.Accent, 0.9f * a));
		return y + 30f;
	}

	// Glass card for the most recent save: backdrop thumbnail, colony, save name, play time, how long ago. Click loads it.
	private static void DrawContinue(Rect r, FileInfo save)
	{
		string name = Path.GetFileNameWithoutExtension(save.Name);
		Backdrops.Entry entry = Backdrops.ForSave(name);
		float a = Intro(1);
		r.x -= (1f - a) * 24f;
		bool hover = Mouse.IsOver(r);
		float h = UiStyle.Smooth(UiStyle.Ease(UiStyle.Key(2950, r), hover ? 1f : 0f, 8f));

		UiStyle.DrawPanel(r, a * (0.72f + 0.28f * h));
		UiStyle.DrawGlow(r.ExpandedBy(14f), UiStyle.AccentGlow, 0.25f * h * a);
		Widgets.DrawBoxSolidWithOutline(r, Color.clear, UiStyle.WithAlpha(UiStyle.Accent, (0.15f + 0.55f * h) * a), 1);
		Widgets.DrawBoxSolid(new Rect(r.x, r.y, 3f, r.height), UiStyle.WithAlpha(UiStyle.Accent, (0.5f + 0.5f * h) * a));

		Rect thumb = new Rect(r.x + 10f, r.y + 9f, (r.height - 18f) * 16f / 9f, r.height - 18f);
		Texture2D tex = Backdrops.Thumbnail(entry);
		Widgets.DrawBoxSolid(thumb, new Color(0f, 0f, 0f, 0.45f * a));
		if (tex != null)
		{
			GUI.color = new Color(1f, 1f, 1f, a);
			GUI.DrawTextureWithTexCoords(thumb, tex, CoverUV(tex, thumb, 1f - 0.06f * h));
			GUI.color = Color.white;
		}
		Widgets.DrawBoxSolidWithOutline(thumb, Color.clear, new Color(1f, 1f, 1f, 0.1f * a), 1);

		float tx = thumb.xMax + 14f;
		float tw = r.xMax - tx - 34f;
		Text.Font = GameFont.Tiny;
		GUI.color = UiStyle.WithAlpha(UiStyle.Accent, a);
		Widgets.Label(new Rect(tx, r.y + 8f, tw, 18f), "CONTINUE");
		Text.Font = GameFont.Medium;
		GUI.color = new Color(1f, 1f, 1f, 0.95f * a);
		Widgets.Label(new Rect(tx, r.y + 22f, tw, 32f), (entry?.colony ?? name).Truncate(tw));
		Text.Font = GameFont.Tiny;
		GUI.color = UiStyle.WithAlpha(UiStyle.TextDim, a * 1.3f);
		string info = entry != null && entry.playHours > 0f ? $"{name}  ·  {entry.playHours:0.#} h  ·  {Ago(save.LastWriteTime)}" : $"{name}  ·  {Ago(save.LastWriteTime)}";
		Widgets.Label(new Rect(tx, r.y + 52f, tw, 18f), info.Truncate(tw));

		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = Color.Lerp(UiStyle.WithAlpha(UiStyle.TextSoft, 0.5f * a), UiStyle.WithAlpha(UiStyle.Accent, a), h);
		Widgets.Label(new Rect(r.xMax - 34f + 6f * h, r.y, 26f, r.height), "›");
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
		GUI.color = Color.white;

		TooltipHandler.TipRegion(r, "LoadGame".Translate() + ": " + name);
		if (Widgets.ButtonInvisible(r))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			GameDataSaveLoader.CheckVersionAndLoadGame(name);
		}
	}

	// UV rect that crops the texture to fill the box without stretching; zoom < 1 zooms in.
	private static Rect CoverUV(Texture tex, Rect box, float zoom)
	{
		float texAspect = tex.width / (float)tex.height, boxAspect = box.width / box.height;
		float uw = 1f, vh = 1f;
		if (texAspect > boxAspect)
		{
			uw = boxAspect / texAspect;
		}
		else
		{
			vh = texAspect / boxAspect;
		}
		uw *= zoom;
		vh *= zoom;
		return new Rect((1f - uw) / 2f, (1f - vh) / 2f, uw, vh);
	}

	private static string Ago(DateTime time)
	{
		TimeSpan span = DateTime.Now - time;
		if (span.TotalMinutes < 1)
		{
			return "just now";
		}
		if (span.TotalHours < 1)
		{
			return $"{(int)span.TotalMinutes} min ago";
		}
		if (span.TotalDays < 1)
		{
			return $"{(int)span.TotalHours} h ago";
		}
		if (span.TotalDays < 30)
		{
			int days = (int)span.TotalDays;
			return days == 1 ? "yesterday" : $"{days} days ago";
		}
		return time.ToString("d MMM yyyy");
	}

	// The main option column: large bare labels with a shadow; the hovered row lights up in the accent colour,
	// slides right and grows an accent edge while the other rows step back.
	public static float DrawOptions(Rect rect, List<ListableOption> opts)
	{
		int hovered = -1;
		float y = 0f;
		for (int i = 0; i < opts.Count; i++)
		{
			float rowH = Mathf.Max(RowHeight, opts[i].minHeight);
			if (opts[i].GetType() == typeof(ListableOption) && Mouse.IsOver(new Rect(rect.x, rect.y + y, rect.width, rowH)))
			{
				hovered = i;
			}
			y += rowH + RowGap;
		}

		y = 0f;
		for (int i = 0; i < opts.Count; i++)
		{
			ListableOption opt = opts[i];
			float a = Intro(2 + i);
			if (opt.GetType() != typeof(ListableOption))
			{
				y += opt.DrawOption(new Vector2(rect.x, rect.y + y), rect.width) + RowGap;
				continue;
			}
			float rowH = Mathf.Max(RowHeight, opt.minHeight);
			Rect row = new Rect(rect.x - (1f - a) * 28f, rect.y + y, rect.width, rowH);
			float h = UiStyle.Smooth(UiStyle.Ease(UiStyle.Key(2201 + i, new Rect(rect.x, rect.y + y, 1f, 1f)), hovered == i ? 1f : 0f, 9f));
			float back = UiStyle.Ease(UiStyle.Key(2401 + i, new Rect(rect.x, rect.y + y, 1f, 1f)), hovered >= 0 && hovered != i ? 1f : 0f, 6f);

			GUI.color = UiStyle.WithAlpha(UiStyle.Accent, 0.13f * h * a);
			GUI.DrawTexture(new Rect(row.x, row.y, row.width * (0.4f + 0.6f * h), row.height), Patch_MenuOverlay.Ramp);
			GUI.color = Color.white;
			float edge = row.height * 0.56f * h;
			Widgets.DrawBoxSolid(new Rect(row.x, row.center.y - edge / 2f, 3f, edge), UiStyle.WithAlpha(UiStyle.Accent, a * h));

			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleLeft;
			string label = LatticeMod.Settings.menuUppercase ? opt.label.ToUpperInvariant() : opt.label;
			Rect text = new Rect(row.x + 14f + 12f * h, row.y, row.width - 30f, row.height);
			GUI.color = new Color(0f, 0f, 0f, 0.8f * a);
			Widgets.Label(text.MovedBy(new Vector2(1.5f, 2f)), label);
			Color idle = UiStyle.WithAlpha(UiStyle.TextSoft, 1f - 0.45f * back);
			GUI.color = UiStyle.WithAlpha(Color.Lerp(idle, Color.Lerp(UiStyle.Accent, Color.white, 0.25f), h), a);
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
				SoundDefOf.Click.PlayOneShotOnCamera();
				opt.action?.Invoke();
			}
			y += rowH + RowGap;
		}
		return y;
	}

	// Bottom bar: DLC icons and web links as small icons on the left, language and version on the right.
	private static void DrawBottomBar(float left, float sw, float sh)
	{
		float a = Intro(3);
		float x = left;
		float cy = sh - 36f;

		UI_BackgroundMain bg = UIMenuBackgroundManager.background as UI_BackgroundMain;
		foreach (ExpansionDef exp in ModLister.AllExpansions)
		{
			if (exp.isCore)
			{
				continue;
			}
			Rect r = new Rect(x, cy - 17f, 34f, 34f);
			bool hover = Mouse.IsOver(r);
			float h = UiStyle.Ease(UiStyle.Key(2960, r), hover ? 1f : 0f);
			GUI.color = new Color(1f, 1f, 1f, a * (0.7f + 0.3f * h));
			GUI.DrawTexture(r.ContractedBy(3f - 3f * h), exp.IconFromStatus, ScaleMode.ScaleToFit);
			GUI.color = Color.white;
			if (hover)
			{
				bg?.Notify_Hovered(exp);
				TooltipHandler.TipRegion(r, exp.LabelCap.Colorize(ColorLibrary.Yellow) + "\n" + exp.StatusDescription + (exp.description.NullOrEmpty() ? "" : "\n\n" + exp.description));
			}
			if (Widgets.ButtonInvisible(r) && !exp.StoreURL.NullOrEmpty())
			{
				SteamUtility.OpenUrl(exp.StoreURL);
			}
			x += 40f;
		}

		if (Links.Count > 0)
		{
			if (x > left)
			{
				UiStyle.DrawVLine(x + 8f, cy - 12f, 24f, 0.14f * a);
				x += 24f;
			}
			foreach (ListableOption link in Links)
			{
				Texture2D icon = (link as ListableOption_WebLink)?.image;
				Text.Font = GameFont.Tiny;
				float w = icon != null ? 30f : Text.CalcSize(link.label).x + 12f;
				Rect r = new Rect(x, cy - 15f, w, 30f);
				bool hover = Mouse.IsOver(r);
				float h = UiStyle.Ease(UiStyle.Key(2980, r), hover ? 1f : 0f);
				GUI.color = UiStyle.WithAlpha(Color.Lerp(UiStyle.WithAlpha(UiStyle.TextSoft, 0.6f), UiStyle.Accent, h), a);
				if (icon != null)
				{
					GUI.DrawTexture(new Rect(r.center.x - 12f, r.center.y - 9f - 2f * h, 24f, 18f), icon);
				}
				else
				{
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(r, link.label);
					Text.Anchor = TextAnchor.UpperLeft;
				}
				GUI.color = Color.white;
				Text.Font = GameFont.Small;
				TooltipHandler.TipRegion(r, link.label);
				if (Widgets.ButtonInvisible(r))
				{
					if (link.action != null)
					{
						link.action();
					}
					else if (link is ListableOption_WebLink web && !web.url.NullOrEmpty())
					{
						Application.OpenURL(web.url);
					}
				}
				x += w + 2f;
			}
		}

		// Language: a quiet text link, accent on hover.
		string lang = LanguageDatabase.activeLanguage.FriendlyNameNative;
		Text.Font = GameFont.Small;
		float lw = Text.CalcSize(lang).x + 8f;
		Rect lr = new Rect(sw - 24f - lw, cy - 22f, lw, 24f);
		float lh = UiStyle.Ease(UiStyle.Key(2901, lr), Mouse.IsOver(lr) ? 1f : 0f);
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = new Color(0f, 0f, 0f, 0.8f * a);
		Widgets.Label(lr.MovedBy(new Vector2(1f, 1.5f)), lang);
		GUI.color = UiStyle.WithAlpha(Color.Lerp(UiStyle.TextSoft, UiStyle.Accent, lh), a);
		Widgets.Label(lr, lang);
		if (Widgets.ButtonInvisible(lr))
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			foreach (LoadedLanguage l in LanguageDatabase.AllLoadedLanguages)
			{
				LoadedLanguage pick = l;
				options.Add(new FloatMenuOption(pick.DisplayName, () =>
				{
					LanguageDatabase.SelectLanguage(pick);
					Prefs.Save();
				}));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}

		// Version (and Steam name) under it, replacing vanilla's block in the top-left corner.
		Text.Font = GameFont.Tiny;
		string version = "VersionIndicator".Translate(VersionControl.CurrentVersionString);
		if (SteamManager.Initialized)
		{
			version += "  ·  " + SteamUtility.SteamPersonaName;
		}
		GUI.color = UiStyle.WithAlpha(UiStyle.TextDim, a);
		Widgets.Label(new Rect(sw - 24f - 420f, cy + 2f, 420f, 20f), version);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
	}
}

// The whole title screen is drawn by MainMenuStyle; vanilla's layout only runs when the cinematic menu is off.
[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI))]
public static class Patch_MainMenuOnGUI
{
	public static bool Prefix()
	{
		if (!MainMenuStyle.On)
		{
			return true;
		}
		MainMenuStyle.Draw();
		return false;
	}
}

// Dark falloff on the left where the menu column sits, a faint shade on the far right and under the bottom bar.
[HarmonyPatch(typeof(UI_BackgroundMain), "DoOverlay")]
public static class Patch_MenuOverlay
{
	private static Texture2D fadeTex;
	private static Texture2D bottomTex;
	private static Texture2D ramp;

	// 1px-high horizontal ramp (opaque -> clear) with a smooth shoulder, stretched over the shaded area: no seams.
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
					fadeTex.SetPixel(i, 0, new Color(0.02f, 0.025f, 0.04f, 0.86f * UiStyle.Smooth(1f - i / (float)(w - 1))));
				}
				fadeTex.Apply();
			}
			return fadeTex;
		}
	}

	// 1px-wide vertical ramp: opaque at the bottom row, clear at the top.
	private static Texture2D BottomTex
	{
		get
		{
			if (bottomTex == null)
			{
				const int h = 64;
				bottomTex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
				for (int i = 0; i < h; i++)
				{
					float t = 1f - i / (float)(h - 1);
					bottomTex.SetPixel(0, i, new Color(0f, 0f, 0f, t * t));
				}
				bottomTex.Apply();
			}
			return bottomTex;
		}
	}

	// Horizontal white ramp (opaque -> clear) tinted per draw call; used for menu row washes.
	public static Texture2D Ramp
	{
		get
		{
			if (ramp == null)
			{
				ramp = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
				for (int i = 0; i < 64; i++)
				{
					ramp.SetPixel(i, 0, new Color(1f, 1f, 1f, 1f - i / 63f));
				}
				ramp.Apply();
			}
			return ramp;
		}
	}

	public static void Postfix()
	{
		if (MainMenuStyle.On)
		{
			Draw();
		}
	}

	public static void Draw()
	{
		float sw = UI.screenWidth, sh = UI.screenHeight;
		float dim = LatticeMod.Settings.menuDim;
		GUI.color = new Color(1f, 1f, 1f, dim);
		GUI.DrawTexture(new Rect(0f, 0f, sw * 0.68f, sh), FadeTex);
		GUI.color = new Color(1f, 1f, 1f, 0.3f * dim);
		GUI.DrawTextureWithTexCoords(new Rect(sw * 0.78f, 0f, sw * 0.22f, sh), FadeTex, new Rect(1f, 0f, -1f, 1f));
		GUI.color = new Color(1f, 1f, 1f, 0.7f * dim);
		GUI.DrawTexture(new Rect(0f, sh - 150f, sw, 150f), BottomTex);
		GUI.color = Color.white;
		UiStyle.DrawGradient(new Rect(0f, 0f, sw, sh * 0.12f), new Color(0f, 0f, 0f, 0.3f), new Color(0f, 0f, 0f, 0f));
	}
}

[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
public static class Patch_MainMenuControls
{
	public static bool Active;
	public static int Calls;

	// Title screen with the cinematic layout (not the in-game pause menu, which only gets restyled rows).
	public static bool Cinematic => Active && Current.ProgramState == ProgramState.Entry && MainMenuStyle.On;

	public static void Prefix()
	{
		Active = true;
		Calls = 0;
	}

	public static void Finalizer() => Active = false;
}

// Replaces the vanilla artwork with a slowly drifting colony screenshot when one is available.
[HarmonyPatch(typeof(UI_BackgroundMain), nameof(UI_BackgroundMain.BackgroundOnGUI))]
public static class Patch_MenuBackground
{
	private static float fade;
	private static string shown;

	public static bool Prefix()
	{
		if (!MainMenuStyle.On)
		{
			return true;
		}
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

// Menu rows. On the title screen the first listing (options) goes to MainMenuStyle and the second (web links) is
// captured for the bottom bar; in the pause menu the rows get a lighter restyle: tall rows with an accent edge.
[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
public static class Patch_OptionListing
{
	private const float RowHeight = 46f;
	private const float Gap = 2f;
	private static readonly AccessTools.FieldRef<ListableOption, string> HighlightTag = AccessTools.FieldRefAccess<ListableOption, string>("uiHighlightTag");

	public static bool Prefix(Rect rect, List<ListableOption> optList, ref float __result)
	{
		if (Patch_MainMenuControls.Cinematic)
		{
			int call = Patch_MainMenuControls.Calls++;
			if (call == 0)
			{
				__result = MainMenuStyle.DrawOptions(new Rect(rect.x, rect.y, MainMenuStyle.ColumnWidth, rect.height), optList);
				return false;
			}
			if (call == 1)
			{
				MainMenuStyle.Links = optList;
				__result = 0f;
				return false;
			}
			return true;
		}
		if (!LatticeMod.Settings.restyleWidgets)
		{
			return true;
		}
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
			GUI.DrawTexture(new Rect(row.x, row.y, row.width * (0.5f + 0.5f * h), row.height), Patch_MenuOverlay.Ramp);
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

// The translucent dark slab vanilla draws behind the link column.
[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D))]
public static class Patch_MenuLinksSlab
{
	public static bool Prefix(Texture2D atlas) => !(Patch_MainMenuControls.Cinematic && atlas != null && atlas.name == "SubtleGradient");
}

// The vanilla language button (a box under the links) is swallowed; the bottom bar draws its own.
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
		if (Patch_MainMenuControls.Cinematic && label == LanguageDatabase.activeLanguage.FriendlyNameNative)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
