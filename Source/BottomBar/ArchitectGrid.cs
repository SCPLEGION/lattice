using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lattice;

// Replaces the vanilla two-column architect list with a horizontal category strip above the main
// bar, and the vanilla gizmo grid with a popup icon+name grid. Vanilla state (selected tab, search
// filter, designator manager) is kept, so hotkeys and other mods that touch it keep working.
public static class ArchitectGrid
{
	public const float CellSize = UiStyle.CellSize;
	public const float InfoWidth = UiStyle.InfoWidth;
	public const float StripChipMaxWidth = 140f;
	public const float SearchWidth = 170f;
	private const int WindowId = 32520;

	public static Rect BlockRect = Rect.zero;
	private static Vector2 scroll;
	private static ArchitectCategoryTab lastTab;
	private const int FadeKey = 0x1A771CE;

	public static int Columns => 8;

	public static Texture CategoryIcon(DesignationCategoryDef def)
	{
		foreach (Designator d in def.ResolvedAllowedDesignators)
		{
			if (d.icon != null)
			{
				return d.icon;
			}
		}
		return null;
	}

	public static void DrawPopup(ArchitectCategoryTab tab, Designator forceActivated)
	{
		List<Designator> designators = tab.def.ResolvedAllowedDesignators.ToList();
		IEnumerable<Designator> extras = LatticeApi.ExtraDesignators(tab.def);
		if (extras != null)
		{
			designators.AddRange(extras);
		}
		int cols = Mathf.Min(Columns, Mathf.Max(1, designators.Count));
		int rows = Mathf.CeilToInt(designators.Count / (float)cols);
		float stripTop = UI.screenHeight - UiStyle.BarHeight - 1f - UiStyle.BarHeight;
		float gridWidth = cols * CellSize + UiStyle.Pad * 2f;
		float height = Mathf.Min(rows * CellSize + UiStyle.Pad * 2f, stripTop - 40f);
		height = Mathf.Max(height, 220f);
		Rect popup = new Rect(0f, stripTop - height - 6f, gridWidth + InfoWidth, height);
		BlockRect = popup;
		if (lastTab != tab)
		{
			lastTab = tab;
			UiStyle.Reset(FadeKey);
		}
		float fade = UiStyle.Smooth(UiStyle.Ease(FadeKey, 1f, 7f));
		popup.y += (1f - fade) * 14f;

		Designator selected = Find.DesignatorManager.SelectedDesignator;
		selected?.DoExtraGuiControls(0f, popup.y);

		Designator hovered = null;
		Find.WindowStack.ImmediateWindow(WindowId, popup, WindowLayer.GameUI, () =>
		{
			Rect inner = popup.AtZero();
			UiStyle.DrawPanel(inner, fade, false);
			Rect gridRect = new Rect(0f, 0f, gridWidth, inner.height);
			Rect view = new Rect(0f, 0f, gridWidth - UiStyle.Pad * 2f - 16f, rows * CellSize);
			bool needScroll = view.height > gridRect.height - UiStyle.Pad * 2f;
			if (!needScroll)
			{
				view.width = gridWidth;
			}
			Rect scrollOuter = gridRect.ContractedBy(UiStyle.Pad);
			Widgets.BeginScrollView(scrollOuter, ref scroll, new Rect(0f, 0f, scrollOuter.width - (needScroll ? 16f : 0f), rows * CellSize));
			for (int i = 0; i < designators.Count; i++)
			{
				Designator d = designators[i];
				Rect cell = new Rect(i % cols * CellSize, i / cols * CellSize, CellSize - 4f, CellSize - 4f);
				bool dim = !MatchesSearch(d);
				bool clicked = UiStyle.IconCell(cell, d.icon, d.LabelCap, selected == d, d.Disabled, dim,
					d.iconDrawScale, d.iconProportions, d.iconTexCoords, d.iconAngle, d.defaultIconColor);
				if (Mouse.IsOver(cell))
				{
					hovered = d;
				}
				if (forceActivated == d || clicked || (d.hotKey != null && d.hotKey.KeyDownEvent))
				{
					d.ProcessInput(Event.current);
					Event.current.Use();
				}
				else if (d.Disabled && Mouse.IsOver(cell) && !d.disabledReason.NullOrEmpty())
				{
					TooltipHandler.TipRegion(cell, d.disabledReason.Colorize(ColorLibrary.RedReadable));
				}
			}
			Widgets.EndScrollView();

			Designator info = hovered ?? Find.DesignatorManager.SelectedDesignator;
			Rect infoRect = new Rect(gridWidth, 0f, InfoWidth, inner.height).ContractedBy(10f);
			UiStyle.DrawGradient(new Rect(gridWidth, 0f, InfoWidth, inner.height), new Color(1f, 1f, 1f, 0.035f), new Color(0f, 0f, 0f, 0.18f));
			UiStyle.DrawVLine(gridWidth - 1f, 8f, inner.height - 16f, UiStyle.DividerAlpha);
			if (info != null)
			{
				DrawInfo(infoRect, info);
			}
		});
	}

	private static readonly AccessTools.FieldRef<MainTabWindow_Architect, QuickSearchWidget> SearchRef =
		AccessTools.FieldRefAccess<MainTabWindow_Architect, QuickSearchWidget>("quickSearchWidget");

	private static bool MatchesSearch(Designator d)
	{
		QuickSearchFilter filter = SearchRef((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).filter;
		return !filter.Active || filter.Matches(d.Label);
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
}

[HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.DesignationTabOnGUI))]
public static class Patch_DesignationTabOnGUI
{
	public static bool Prefix(ArchitectCategoryTab __instance, Designator forceActivatedCommand)
	{
		ArchitectGrid.DrawPopup(__instance, forceActivatedCommand);
		return false;
	}
}

[HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.InfoRect), MethodType.Getter)]
public static class Patch_InfoRect
{
	// Designator_Place/Designator_Build use this to ignore map clicks while the mouse is over the panel.
	public static void Postfix(ref Rect __result)
	{
		__result = ArchitectGrid.BlockRect;
	}
}

[HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.WinHeight), MethodType.Getter)]
public static class Patch_WinHeight
{
	public static void Postfix(ref float __result) => __result = UiStyle.BarHeight;
}

[HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.RequestedTabSize), MethodType.Getter)]
public static class Patch_RequestedTabSize
{
	public static void Postfix(ref Vector2 __result) => __result = new Vector2(UI.screenWidth, UiStyle.BarHeight);
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

		UiStyle.DrawShadow(inRect, 8f, 0.6f);
		UiStyle.DrawPanel(inRect, 1f, false);
		float chipW = Mathf.Min(ArchitectGrid.StripChipMaxWidth, (inRect.width - ArchitectGrid.SearchWidth - 8f) / Mathf.Max(1, panels.Count));
		// Vanilla order is reversed (descending); draw in on-screen order left to right.
		for (int i = 0; i < panels.Count; i++)
		{
			ArchitectCategoryTab panel = panels[panels.Count - 1 - i];
			Rect chip = new Rect(i * chipW, 0f, chipW, inRect.height);
			bool visible = panel.Visible;
			bool isOpen = open == panel;
			int key = UiStyle.Key(4, chip);
			float h = UiStyle.Smooth(UiStyle.Ease(key, Mouse.IsOver(chip) && visible ? 1f : 0f, 9f));
			float o = UiStyle.Smooth(UiStyle.Ease(key + 1, isOpen ? 1f : 0f, 7f));
			if (h > 0f || o > 0f)
			{
				UiStyle.DrawGradient(chip, new Color(1f, 1f, 1f, 0.05f + 0.1f * h + 0.07f * o), new Color(1f, 1f, 1f, 0.01f));
			}
			if (o > 0f)
			{
				UiStyle.DrawGlow(new Rect(chip.x - 10f, chip.y - 14f, chip.width + 20f, 40f), UiStyle.AccentGlow, o * 0.8f);
			}
			UiStyle.DrawAccentLine(chip, Mathf.Max(o, h * 0.5f), true, 2f);
			UiStyle.DrawVLine(chip.xMax - 1f, chip.y + 8f, chip.height - 16f, 0.06f);
			Texture icon = ArchitectGrid.CategoryIcon(panel.def);
			float iconSpace = icon != null ? 24f : 0f;
			bool lit = visible && panel.AnySearchMatches;
			GUI.color = lit ? Color.Lerp(UiStyle.TextSoft, Color.white, Mathf.Max(h, o)) : UiStyle.TextDim;
			if (icon != null)
			{
				GUI.DrawTexture(new Rect(chip.x + 6f, chip.center.y - 10f, 20f, 20f), icon);
			}
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(chip.x + 6f + iconSpace, chip.y, chip.width - iconSpace - 8f, chip.height), panel.def.LabelCap);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			MouseoverSounds.DoRegion(chip, SoundDefOf.Mouseover_Category);
			if (Widgets.ButtonInvisible(chip, doMouseoverSound: false))
			{
				ClickedCategory.Invoke(__instance, new object[] { panel });
			}
			if (!isOpen)
			{
				UIHighlighter.HighlightOpportunity(chip, panel.def.cachedHighlightClosedTag);
			}
		}
		Rect searchRect = new Rect(inRect.xMax - ArchitectGrid.SearchWidth - 4f, 6f, ArchitectGrid.SearchWidth, 24f);
		Widgets.DrawBoxSolid(searchRect.ExpandedBy(2f), new Color(0f, 0f, 0f, 0.35f));
		Widgets.DrawBoxSolid(new Rect(searchRect.x - 2f, searchRect.yMax + 1f, searchRect.width + 4f, 1f), UiStyle.Hairline);
		search.OnGUI(searchRect, () => CacheSearchState.Invoke(__instance, null));
		return false;
	}
}
