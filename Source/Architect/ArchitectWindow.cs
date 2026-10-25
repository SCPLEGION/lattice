using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lattice;

// The architect is a floating window opened by the hammer button in the dock: categories on the left,
// icon+name grid in the middle, info panel on the right. It still IS MainTabWindow_Architect, so vanilla
// state (selected category, search filter, designator manager), hotkeys and other mods keep working.
public static class ArchitectGrid
{
	public const float CatWidth = 180f;
	public const float RowHeight = 34f;

	public static float CellSize => LatticeMod.Settings.cellSize;
	public static float InfoWidth => LatticeMod.Settings.showInfoPanel ? 250f : 0f;
	public static int Columns => LatticeMod.Settings.columns;

	public static Rect BlockRect = Rect.zero;
	private static Vector2 gridScroll;
	private static ArchitectCategoryTab lastTab;
	private const int FadeKey = 0x1A771CE;

	public static Texture CategoryIcon(DesignationCategoryDef def) => Icons.For(def);

	public static Vector2 WindowSize()
	{
		int fit = Mathf.Max(3, Mathf.FloorToInt((UI.screenWidth - 60f - CatWidth - InfoWidth - 32f) / CellSize));
		int cols = Mathf.Min(Columns, fit);
		float w = CatWidth + cols * CellSize + 32f + InfoWidth;
		float h = Mathf.Min(UI.screenHeight - 36f - 70f, 3.7f * CellSize + 90f);
		return new Vector2(w, Mathf.Max(h, 380f));
	}

	private static readonly AccessTools.FieldRef<MainTabWindow_Architect, QuickSearchWidget> SearchRef =
		AccessTools.FieldRefAccess<MainTabWindow_Architect, QuickSearchWidget>("quickSearchWidget");

	private static bool MatchesSearch(Designator d)
	{
		QuickSearchFilter filter = SearchRef((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).filter;
		return !filter.Active || filter.Matches(d.Label);
	}

	public static void DrawGrid(ArchitectCategoryTab tab, Rect gridRect, Rect infoRect, Designator forceActivated)
	{
		List<Designator> designators = tab.def.ResolvedAllowedDesignators.ToList();
		if (lastTab != tab)
		{
			lastTab = tab;
			gridScroll = Vector2.zero;
			UiStyle.Reset(FadeKey);
		}
		float fade = UiStyle.Smooth(UiStyle.Ease(FadeKey, 1f, 7f));
		float cell = CellSize;
		int cols = Mathf.Max(1, Mathf.FloorToInt((gridRect.width - 16f) / cell));
		int rows = Mathf.CeilToInt(designators.Count / (float)cols);
		Designator selected = Find.DesignatorManager.SelectedDesignator;
		Designator hovered = null;

		GUI.color = new Color(1f, 1f, 1f, fade);
		Widgets.BeginScrollView(gridRect, ref gridScroll, new Rect(0f, 0f, gridRect.width - 16f, rows * cell));
		for (int i = 0; i < designators.Count; i++)
		{
			Designator d = designators[i];
			Rect c = new Rect(i % cols * cell, i / cols * cell + (1f - fade) * 8f, cell - 4f, cell - 4f);
			bool dim = !MatchesSearch(d);
			bool clicked = UiStyle.IconCell(c, Icons.For(d), d.LabelCap, selected == d, d.Disabled, dim,
				d.iconDrawScale, d.iconProportions, d.iconTexCoords, d.iconAngle, d.IconDrawColor);
			if (Mouse.IsOver(c))
			{
				hovered = d;
			}
			if (forceActivated == d || clicked || (d.hotKey != null && d.hotKey.KeyDownEvent))
			{
				d.ProcessInput(Event.current);
				Event.current.Use();
			}
			else if (d.Disabled && Mouse.IsOver(c) && !d.disabledReason.NullOrEmpty())
			{
				TooltipHandler.TipRegion(c, d.disabledReason.Colorize(ColorLibrary.RedReadable));
			}
		}
		Widgets.EndScrollView();
		GUI.color = Color.white;

		if (infoRect.width > 1f)
		{
			Designator info = hovered ?? Find.DesignatorManager.SelectedDesignator;
			Widgets.DrawBoxSolid(new Rect(infoRect.x - 6f, infoRect.y + 4f, 1f, infoRect.height - 8f), UiStyle.Hairline);
			if (info != null)
			{
				DrawInfo(infoRect, info);
			}
		}
	}

	private static void DrawInfo(Rect rect, Designator d)
	{
		GUI.BeginGroup(rect);
		Text.Font = GameFont.Medium;
		Rect title = new Rect(0f, 0f, rect.width - d.PanelReadoutTitleExtraRightMargin, 32f);
		Widgets.Label(title, d.LabelCap);
		Text.Font = GameFont.Small;
		Widgets.DrawBoxSolid(new Rect(0f, 34f, 36f, 2f), UiStyle.Accent);
		Widgets.DrawBoxSolid(new Rect(36f, 34f, rect.width - 36f, 1f), UiStyle.Hairline);
		float y = 42f;
		d.DrawPanelReadout(ref y, rect.width);
		Rect desc = new Rect(0f, y + 4f, rect.width, rect.height - y - 4f);
		GUI.color = new Color(UiStyle.TextSoft.r, UiStyle.TextSoft.g, UiStyle.TextSoft.b, UiStyle.DescAlpha);
		Widgets.Label(desc, d.Desc.TruncateHeight(desc.width, desc.height));
		GUI.color = Color.white;
		GUI.EndGroup();
	}

	public static void DrawCategoryRow(Rect row, ArchitectCategoryTab panel, bool isOpen, System.Action onClick)
	{
		bool visible = panel.Visible;
		bool hover = Mouse.IsOver(row) && visible;
		float h = UiStyle.Ease(UiStyle.Key(3101, row), hover ? 1f : 0f, 10f);
		float o = UiStyle.Ease(UiStyle.Key(3102, row), isOpen ? 1f : 0f, 8f);
		UiStyle.DrawGradient(row, new Color(1f, 1f, 1f, 0.03f + 0.07f * h + 0.08f * o), new Color(1f, 1f, 1f, 0.01f + 0.02f * o));
		Widgets.DrawBoxSolid(new Rect(row.x, row.y, 2f + 2f * o, row.height), UiStyle.WithAlpha(UiStyle.Accent, o));
		Texture icon = CategoryIcon(panel.def);
		float iconSpace = icon != null ? 30f : 0f;
		GUI.color = visible && panel.AnySearchMatches ? Color.white : UiStyle.TextDim;
		if (icon != null)
		{
			GUI.DrawTexture(new Rect(row.x + 10f, row.center.y - 11f, 22f, 22f), icon);
		}
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(row.x + 10f + iconSpace, row.y, row.width - iconSpace - 14f, row.height), panel.def.LabelCap);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		MouseoverSounds.DoRegion(row, SoundDefOf.Mouseover_Category);
		if (Widgets.ButtonInvisible(row, doMouseoverSound: false))
		{
			onClick();
		}
		if (!isOpen)
		{
			UIHighlighter.HighlightOpportunity(row, panel.def.cachedHighlightClosedTag);
		}
	}
}

[HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.InfoRect), MethodType.Getter)]
public static class Patch_InfoRect
{
	// Designator_Place/Designator_Build use this to ignore map clicks while the mouse is over the window.
	public static void Postfix(ref Rect __result)
	{
		__result = Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect ? ArchitectGrid.BlockRect : Rect.zero;
	}
}

[HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.RequestedTabSize), MethodType.Getter)]
public static class Patch_RequestedTabSize
{
	public static void Postfix(ref Vector2 __result) => __result = ArchitectGrid.WindowSize();
}

[HarmonyPatch(typeof(MainTabWindow), "SetInitialSizeAndPosition")]
public static class Patch_ArchitectPosition
{
	public static void Postfix(MainTabWindow __instance)
	{
		if (__instance is MainTabWindow_Architect)
		{
			Rect r = __instance.windowRect;
			r.x = (UI.screenWidth - r.width) / 2f;
			r.y -= 8f;
			__instance.windowRect = r;
		}
	}
}

[HarmonyPatch(typeof(MainTabWindow_Architect), "ExtraOnGUI")]
public static class Patch_ArchitectExtraOnGUI
{
	public static void Postfix(MainTabWindow_Architect __instance)
	{
		Find.DesignatorManager.SelectedDesignator?.DoExtraGuiControls(__instance.windowRect.x, __instance.windowRect.y);
	}
}

[HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.DoWindowContents))]
public static class Patch_DoWindowContents
{
	private static readonly AccessTools.FieldRef<MainTabWindow_Architect, List<ArchitectCategoryTab>> Panels =
		AccessTools.FieldRefAccess<MainTabWindow_Architect, List<ArchitectCategoryTab>>("desPanelsCached");
	private static readonly AccessTools.FieldRef<MainTabWindow_Architect, QuickSearchWidget> Search =
		AccessTools.FieldRefAccess<MainTabWindow_Architect, QuickSearchWidget>("quickSearchWidget");
	private static readonly AccessTools.FieldRef<MainTabWindow_Architect, Designator> ForceActivated =
		AccessTools.FieldRefAccess<MainTabWindow_Architect, Designator>("forceActivatedCommand");
	private static readonly System.Reflection.MethodInfo ClickedCategory = AccessTools.Method(typeof(MainTabWindow_Architect), "ClickedCategory");
	private static readonly System.Reflection.MethodInfo CacheSearchState = AccessTools.Method(typeof(MainTabWindow_Architect), "CacheSearchState");

	public static bool Prefix(MainTabWindow_Architect __instance, Rect inRect)
	{
		List<ArchitectCategoryTab> panels = Panels(__instance);
		QuickSearchWidget search = Search(__instance);
		ArchitectCategoryTab open = __instance.selectedDesPanel;
		ArchitectGrid.BlockRect = __instance.windowRect;

		if (KeyBindingDefOf.Accept.KeyDownEvent)
		{
			if (search.filter.Active && open?.UniqueSearchMatch != null)
			{
				ForceActivated(__instance) = open.UniqueSearchMatch;
				Event.current.Use();
			}
			else if (!Verse.Steam.SteamDeck.IsSteamDeck)
			{
				__instance.Close();
				Event.current.Use();
			}
		}

		// Left column: categories (vanilla order is reversed, so draw bottom-up).
		Rect catRect = new Rect(0f, 0f, ArchitectGrid.CatWidth, inRect.height);
		Widgets.DrawBoxSolid(catRect, new Color(0f, 0f, 0f, 0.22f));
		Text.Font = GameFont.Small;
		Rect catView = new Rect(0f, 0f, catRect.width - 16f, panels.Count * ArchitectGrid.RowHeight);
		Widgets.BeginScrollView(catRect, ref catScrollRef, catView);
		for (int i = 0; i < panels.Count; i++)
		{
			ArchitectCategoryTab panel = panels[panels.Count - 1 - i];
			Rect row = new Rect(0f, i * ArchitectGrid.RowHeight, catView.width, ArchitectGrid.RowHeight - 2f);
			ArchitectGrid.DrawCategoryRow(row, panel, open == panel, () => ClickedCategory.Invoke(__instance, new object[] { panel }));
		}
		Widgets.EndScrollView();

		// Right: search, then the grid + info for the open category.
		float x = catRect.xMax + 12f;
		Rect searchRect = new Rect(x, 4f, inRect.width - x - 4f, 26f);
		search.OnGUI(searchRect, () => CacheSearchState.Invoke(__instance, null));

		Rect body = new Rect(x, 38f, inRect.width - x, inRect.height - 38f);
		if (open == null)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(body, "Choose a category");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
		else
		{
			float info = ArchitectGrid.InfoWidth;
			Rect grid = new Rect(body.x, body.y, body.width - info, body.height);
			Rect infoRect = new Rect(grid.xMax + 10f, body.y, info - 10f, body.height);
			ArchitectGrid.DrawGrid(open, grid, infoRect, ForceActivated(__instance));
		}
		ForceActivated(__instance) = null;
		return false;
	}

	private static Vector2 catScrollRef;
}
