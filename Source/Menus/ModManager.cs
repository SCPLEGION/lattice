using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Mod manager as a card grid (preview image, name, author, state, one-click toggle) with a category
// sidebar, search, sorting and a detail drawer. It is a view on the vanilla page: activation, requirements,
// saving, workshop buttons and the details pane are the vanilla methods. The load-order list view stays
// available (toggle) for drag-reordering.
public static class ModManager
{
	private enum Category { All, Enabled, Disabled, Official, Workshop, Local, Issues }
	private enum SortBy { Name, Status, Source }

	private const float SidebarWidth = 190f;
	private const float DetailWidth = 380f;
	private const float CardW = 232f;
	private const float CardH = 178f;
	private const float PreviewH = 104f;

	public static bool ListMode;

	private static readonly AccessTools.FieldRef<Page_ModsConfig, ModMetaData> Primary =
		AccessTools.FieldRefAccess<Page_ModsConfig, ModMetaData>("primarySelectedMod");
	private static readonly AccessTools.FieldRef<Page_ModsConfig, bool> Dirty =
		AccessTools.FieldRefAccess<Page_ModsConfig, bool>("modListsDirty");
	private static readonly Action<Page_ModsConfig, ModMetaData> SelectMod =
		AccessTools.MethodDelegate<Action<Page_ModsConfig, ModMetaData>>(AccessTools.Method(typeof(Page_ModsConfig), "SelectMod"));
	private static readonly Func<Page_ModsConfig, ModMetaData, bool> TryActivate =
		AccessTools.MethodDelegate<Func<Page_ModsConfig, ModMetaData, bool>>(AccessTools.Method(typeof(Page_ModsConfig), "TrySetModActive"));
	private static readonly Action<Page_ModsConfig, ModMetaData> TryDeactivate =
		AccessTools.MethodDelegate<Action<Page_ModsConfig, ModMetaData>>(AccessTools.Method(typeof(Page_ModsConfig), "TrySetModInactive"));
	private static readonly Action<Page_ModsConfig, Rect, ModMetaData> DoInfo =
		AccessTools.MethodDelegate<Action<Page_ModsConfig, Rect, ModMetaData>>(AccessTools.Method(typeof(Page_ModsConfig), "DoModInfo"));
	private static readonly Action<Page_ModsConfig, Rect> DoBottom =
		AccessTools.MethodDelegate<Action<Page_ModsConfig, Rect>>(AccessTools.Method(typeof(Page_ModsConfig), "DoBottomButtons"));

	private static Category category;
	private static SortBy sortBy;
	private static string search = "";
	private static Vector2 scroll;
	private static List<ModMetaData> all;
	private static HashSet<ModMetaData> issues = new HashSet<ModMetaData>();
	private static int cachedAt = -1000;

	private static void Refresh(bool force)
	{
		if (!force && all != null && Time.frameCount - cachedAt < 45)
		{
			return;
		}
		cachedAt = Time.frameCount;
		all = ModLister.AllInstalledMods.ToList();
		issues = new HashSet<ModMetaData>(all.Where(m => m.Active && (!m.VersionCompatible || m.UnsatisfiedDependencies().Count > 0)));
	}

	private static bool InCategory(ModMetaData m, Category c)
	{
		switch (c)
		{
			case Category.Enabled: return m.Active;
			case Category.Disabled: return !m.Active;
			case Category.Official: return m.Official;
			case Category.Workshop: return m.Source == ContentSource.SteamWorkshop;
			case Category.Local: return m.Source == ContentSource.ModsFolder;
			case Category.Issues: return issues.Contains(m) || (!m.VersionCompatible && m.Active);
			default: return true;
		}
	}

	public static void Draw(Page_ModsConfig page, Rect rect)
	{
		Refresh(Dirty(page));
		float bottomH = 44f;
		Rect side = new Rect(0f, 0f, SidebarWidth, rect.height - bottomH - 8f);
		ModMetaData selected = Primary(page);
		float detail = selected != null ? DetailWidth : 0f;
		Rect main = new Rect(SidebarWidth + 14f, 0f, rect.width - SidebarWidth - 14f - detail - (detail > 0f ? 12f : 0f), rect.height - bottomH - 8f);
		Rect detailRect = new Rect(main.xMax + 12f, 0f, detail, main.height);

		DrawSidebar(side);
		DrawMain(page, main);
		if (selected != null)
		{
			DoInfo(page, detailRect, selected);
		}
		DrawBottom(page, new Rect(0f, rect.height - bottomH, rect.width, bottomH));
	}

	private static void DrawSidebar(Rect rect)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.6f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.6f));
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(14f, 10f, rect.width - 20f, 34f), "Mods");
		Text.Font = GameFont.Small;
		Widgets.DrawBoxSolid(new Rect(14f, 46f, 36f, 2f), UiStyle.Accent);

		float y = 60f;
		foreach (Category c in Enum.GetValues(typeof(Category)))
		{
			int count = all.Count(m => InCategory(m, c));
			Rect row = new Rect(6f, y, rect.width - 12f, 30f);
			bool on = category == c;
			float h = UiStyle.Ease(UiStyle.Key(7101 + (int)c, row), Mouse.IsOver(row) ? 1f : 0f, 10f);
			float o = UiStyle.Ease(UiStyle.Key(7201 + (int)c, row), on ? 1f : 0f, 8f);
			UiStyle.DrawGradient(row, new Color(1f, 1f, 1f, 0.02f + 0.06f * h + 0.1f * o), new Color(1f, 1f, 1f, 0.01f * o));
			Widgets.DrawBoxSolid(new Rect(row.x, row.y, 3f * o, row.height), UiStyle.Accent);
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = on ? UiStyle.Accent : Color.Lerp(UiStyle.TextSoft, Color.white, h);
			Widgets.Label(new Rect(row.x + 12f, row.y, row.width - 50f, row.height), c.ToString());
			GUI.color = UiStyle.TextDim;
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(new Rect(row.xMax - 44f, row.y, 38f, row.height), count.ToString());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			if (Widgets.ButtonInvisible(row))
			{
				category = c;
				scroll = Vector2.zero;
			}
			y += 32f;
		}

		y += 14f;
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(14f, y, rect.width - 20f, 22f), "SORT BY");
		GUI.color = Color.white;
		y += 24f;
		foreach (SortBy s in Enum.GetValues(typeof(SortBy)))
		{
			Rect row = new Rect(6f, y, rect.width - 12f, 26f);
			bool on = sortBy == s;
			if (on)
			{
				Widgets.DrawBoxSolid(new Rect(row.x, row.y, 3f, row.height), UiStyle.Accent);
			}
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = on ? UiStyle.Accent : UiStyle.TextSoft;
			Widgets.Label(new Rect(row.x + 12f, row.y, row.width, row.height), s.ToString());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			Widgets.DrawHighlightIfMouseover(row);
			if (Widgets.ButtonInvisible(row))
			{
				sortBy = s;
			}
			y += 28f;
		}
	}

	private static void DrawMain(Page_ModsConfig page, Rect rect)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.45f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.45f));

		Rect searchRect = new Rect(rect.x + 10f, rect.y + 8f, 280f, 28f);
		search = Widgets.TextField(searchRect, search);
		if (search.NullOrEmpty())
		{
			GUI.color = UiStyle.TextDim;
			Widgets.Label(new Rect(searchRect.x + 6f, searchRect.y + 3f, searchRect.width, 24f), "Search mods...");
			GUI.color = Color.white;
		}
		int active = all.Count(m => m.Active);
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = UiStyle.TextSoft;
		Widgets.Label(new Rect(rect.xMax - 260f, rect.y + 8f, 250f, 28f), $"{active} enabled / {all.Count} installed");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;

		IEnumerable<ModMetaData> list = all.Where(m => InCategory(m, category));
		if (!search.NullOrEmpty())
		{
			list = list.Where(m => m.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
				|| (m.AuthorsString ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
				|| m.PackageId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		switch (sortBy)
		{
			case SortBy.Status: list = list.OrderByDescending(m => m.Active).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			case SortBy.Source: list = list.OrderBy(m => m.Source).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			default: list = list.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
		}
		List<ModMetaData> mods = list.ToList();

		Rect outRect = new Rect(rect.x + 6f, rect.y + 44f, rect.width - 12f, rect.height - 50f);
		int cols = Mathf.Max(1, Mathf.FloorToInt((outRect.width - 16f) / CardW));
		float cellW = (outRect.width - 16f) / cols;
		int rows = Mathf.CeilToInt(mods.Count / (float)cols);
		Rect view = new Rect(0f, 0f, outRect.width - 16f, rows * CardH);
		Widgets.BeginScrollView(outRect, ref scroll, view);
		for (int i = 0; i < mods.Count; i++)
		{
			Rect card = new Rect(i % cols * cellW + 4f, i / cols * CardH + 4f, cellW - 8f, CardH - 8f);
			if (card.yMax < scroll.y || card.y > scroll.y + outRect.height)
			{
				continue;
			}
			DrawCard(page, card, mods[i]);
		}
		Widgets.EndScrollView();
		if (mods.Count == 0)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(outRect, "No mods match.");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	private static void DrawCard(Page_ModsConfig page, Rect card, ModMetaData mod)
	{
		bool hover = Mouse.IsOver(card);
		bool selected = Primary(page) == mod;
		float h = UiStyle.Ease(UiStyle.Key(7301, card), hover ? 1f : 0f, 10f);
		bool bad = !mod.VersionCompatible;
		bool warn = issues.Contains(mod);
		Color edge = bad ? ColorLibrary.RedReadable : warn ? new Color(1f, 0.75f, 0.2f) : mod.Active ? UiStyle.Accent : UiStyle.Hairline;

		UiStyle.DrawGradient(card, UiStyle.WithAlpha(UiStyle.PanelTop, 0.75f + 0.25f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.85f));
		if (selected)
		{
			Widgets.DrawBoxSolidWithOutline(card, Color.clear, UiStyle.Accent, 2);
		}
		Widgets.DrawBoxSolid(new Rect(card.x, card.y, card.width, 3f), UiStyle.WithAlpha(edge, mod.Active || bad || warn ? 1f : 0.5f));

		Rect preview = new Rect(card.x + 4f, card.y + 7f, card.width - 8f, PreviewH);
		Texture tex = mod.PreviewImage ?? mod.Icon;
		Widgets.DrawBoxSolid(preview, new Color(0f, 0f, 0f, 0.4f));
		if (tex != null)
		{
			GUI.color = mod.Active ? Color.white : new Color(1f, 1f, 1f, 0.55f);
			GUI.DrawTexture(preview, tex, ScaleMode.ScaleAndCrop);
			GUI.color = Color.white;
		}
		UiStyle.DrawGradient(new Rect(preview.x, preview.yMax - 28f, preview.width, 28f), new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.75f));
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.LowerRight;
		Widgets.Label(new Rect(preview.x, preview.y, preview.width - 6f, preview.height - 4f), mod.Official ? "official" : mod.ModVersion ?? "");
		Text.Anchor = TextAnchor.UpperLeft;

		Rect nameRect = new Rect(card.x + 8f, preview.yMax + 4f, card.width - 16f, 32f);
		Text.WordWrap = true;
		Widgets.Label(nameRect, mod.Name.Truncate(nameRect.width * 2f - 16f));
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(card.x + 8f, card.yMax - 20f, card.width - 16f, 18f), (mod.AuthorsString ?? "").Truncate(card.width - 24f));
		GUI.color = Color.white;
		Text.Font = GameFont.Small;

		// Toggle first so it wins the click over the card.
		Rect box = new Rect(preview.xMax - 30f, preview.y + 6f, 24f, 24f);
		Widgets.DrawBoxSolid(box, new Color(0f, 0f, 0f, 0.65f));
		Widgets.DrawBoxSolidWithOutline(box.ContractedBy(0f), Color.clear, mod.Active ? UiStyle.Accent : UiStyle.TextDim, 1);
		if (mod.Active)
		{
			Widgets.DrawBoxSolid(box.ContractedBy(6f), UiStyle.Accent);
		}
		if (!mod.IsCoreMod && Widgets.ButtonInvisible(box))
		{
			if (mod.Active)
			{
				TryDeactivate(page, mod);
			}
			else
			{
				TryActivate(page, mod);
			}
			Refresh(true);
		}
		if (Widgets.ButtonInvisible(card))
		{
			SelectMod(page, mod);
		}
		if (hover)
		{
			TooltipHandler.TipRegion(card, $"{mod.Name}\n{mod.PackageIdPlayerFacing}" + (bad ? "\n" + "Not made for this game version".Colorize(ColorLibrary.RedReadable) : ""));
		}
	}

	private static void DrawBottom(Page_ModsConfig page, Rect rect)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.8f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.9f));
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), UiStyle.Hairline);
		Rect left = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 460f, 32f);
		DoBottom(page, left);

		float x = rect.xMax - 6f;
		x -= 110f;
		if (Widgets.ButtonText(new Rect(x, rect.y + 6f, 110f, 32f), "Close"))
		{
			page.Close();
		}
		x -= 150f;
		if (Widgets.ButtonText(new Rect(x, rect.y + 6f, 144f, 32f), "Resolve order"))
		{
			ModsConfig.TrySortMods();
			Dirty(page) = true;
			Refresh(true);
		}
		x -= 150f;
		if (Widgets.ButtonText(new Rect(x, rect.y + 6f, 144f, 32f), "Load order view"))
		{
			ListMode = true;
		}
	}
}

[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
public static class Patch_ModsPageContents
{
	public static bool Prefix(Page_ModsConfig __instance, Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets || ModManager.ListMode)
		{
			return true;
		}
		ModManager.Draw(__instance, rect);
		return false;
	}

	public static void Postfix(Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets || !ModManager.ListMode)
		{
			return;
		}
		if (Widgets.ButtonText(new Rect(rect.x + 514f, rect.y, 160f, 30f), "Card view"))
		{
			ModManager.ListMode = false;
		}
	}
}

// The mods page opens bigger so the card grid has room.
[HarmonyPatch(typeof(Page), nameof(Page.InitialSize), MethodType.Getter)]
public static class Patch_ModsPageSize
{
	public static void Postfix(Page __instance, ref Vector2 __result)
	{
		if (__instance is Page_ModsConfig && LatticeMod.Settings.restyleWidgets)
		{
			__result = new Vector2(Mathf.Min(UI.screenWidth - 80f, 1700f), Mathf.Min(UI.screenHeight - 60f, 960f));
		}
	}
}
