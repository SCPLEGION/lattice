using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lattice;

// Replaces the bills tab of work tables: current bills on the left (vanilla Bill.DoInterface, so every
// button keeps working), a searchable icon grid of everything the bench can make on the right.
// One click on a tile queues the bill; no float menus.
[HarmonyPatch(typeof(ITab_Bills), MethodType.Constructor)]
public static class Patch_BillsTabSize
{
	private static readonly AccessTools.FieldRef<InspectTabBase, Vector2> Size = AccessTools.FieldRefAccess<InspectTabBase, Vector2>("size");

	public static void Postfix(ITab_Bills __instance) => Size(__instance) = BillsTab.TabSize;
}

[HarmonyPatch(typeof(ITab_Bills), "FillTab")]
public static class Patch_BillsFillTab
{
	public static bool Prefix(ITab_Bills __instance)
	{
		BillsTab.Draw(__instance);
		return false;
	}
}

public static class BillsTab
{
	public static readonly Vector2 TabSize = new Vector2(760f, 560f);
	private const float ListWidth = 300f;
	private const float TileW = 96f;
	private const float TileH = 88f;

	private static readonly AccessTools.FieldRef<ITab_Bills, Bill> Mouseover = AccessTools.FieldRefAccess<ITab_Bills, Bill>("mouseoverBill");
	private static readonly System.Reflection.MethodInfo SelTableGetter = AccessTools.PropertyGetter(typeof(ITab_Bills), "SelTable");

	private static Vector2 listScroll;
	private static Vector2 gridScroll;
	private static string search = "";

	public static void Draw(ITab_Bills tab)
	{
		Building_WorkTable table = (Building_WorkTable)SelTableGetter.Invoke(tab, null);
		Rect full = new Rect(0f, 0f, TabSize.x, TabSize.y).ContractedBy(12f);
		BillStack stack = table.billStack;
		bool full_ = stack.Count >= BillStack.MaxCount;

		// Header
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(full.x, full.y, full.width - 160f, 32f), table.LabelCap);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = full_ ? ColorLibrary.RedReadable : UiStyle.TextSoft;
		Widgets.Label(new Rect(full.xMax - 160f, full.y, 160f, 32f), $"{stack.Count} / {BillStack.MaxCount}");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		UiStyle.DrawAccentLine(new Rect(full.x, full.y + 34f, full.width, 2f), 1f, false, 2f);

		float top = full.y + 44f;
		Rect listRect = new Rect(full.x, top, ListWidth, full.yMax - top);
		Rect gridRect = new Rect(full.x + ListWidth + 12f, top, full.width - ListWidth - 12f, full.yMax - top);

		Mouseover(tab) = DrawBillList(listRect, stack);
		DrawCatalog(gridRect, table, stack, full_);
	}

	private static Bill DrawBillList(Rect rect, BillStack stack)
	{
		Bill hovered = null;
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.5f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.5f));
		Rect inner = rect.ContractedBy(6f);
		if (stack.Count == 0)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(inner, "Pick something to make on the right.");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			return null;
		}

		float viewHeight = 0f;
		for (int i = 0; i < stack.Count; i++)
		{
			viewHeight += 53f + 6f;
		}
		Rect view = new Rect(0f, 0f, inner.width - 16f, viewHeight);
		Widgets.BeginScrollView(inner, ref listScroll, view);
		float y = 0f;
		for (int i = 0; i < stack.Count; i++)
		{
			Bill bill = stack.Bills[i];
			Rect r = bill.DoInterface(0f, y, view.width, i);
			if (!bill.DeletedOrDereferenced && Mouse.IsOver(r))
			{
				hovered = bill;
			}
			y += r.height + 6f;
		}
		Widgets.EndScrollView();
		return hovered;
	}

	private static void DrawCatalog(Rect rect, Building_WorkTable table, BillStack stack, bool full)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.5f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.5f));
		Rect inner = rect.ContractedBy(8f);

		Rect searchRect = new Rect(inner.x, inner.y, inner.width, 26f);
		search = Widgets.TextField(searchRect, search);
		if (search.NullOrEmpty())
		{
			GUI.color = UiStyle.TextDim;
			Widgets.Label(searchRect.ContractedBy(4f), "Search recipes...");
			GUI.color = Color.white;
		}

		List<RecipeDef> recipes = table.def.AllRecipes
			.Where(r => r.AvailableNow && r.AvailableOnNow(table, null))
			.Where(r => search.NullOrEmpty() || r.LabelCap.ToString().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
			.OrderBy(r => r.LabelCap.ToString())
			.ToList();

		Rect gridOuter = new Rect(inner.x, inner.y + 32f, inner.width, inner.height - 32f);
		int cols = Mathf.Max(1, Mathf.FloorToInt((gridOuter.width - 16f) / TileW));
		int rows = Mathf.CeilToInt(recipes.Count / (float)cols);
		Rect view = new Rect(0f, 0f, gridOuter.width - 16f, rows * TileH);
		Widgets.BeginScrollView(gridOuter, ref gridScroll, view);
		for (int i = 0; i < recipes.Count; i++)
		{
			RecipeDef recipe = recipes[i];
			Rect cell = new Rect(i % cols * TileW, i / cols * TileH, TileW - 4f, TileH - 4f);
			Texture icon = recipe.UIIcon ?? recipe.ProducedThingDef?.uiIcon;
			bool clicked = UiStyle.IconCell(cell, icon, recipe.LabelCap, false, full, false);
			if (Mouse.IsOver(cell))
			{
				TooltipHandler.TipRegion(cell, recipe.LabelCap.Colorize(ColorLibrary.Yellow) + (recipe.description.NullOrEmpty() ? "" : "\n\n" + recipe.description));
			}
			if (clicked)
			{
				AddBill(table, recipe);
			}
		}
		Widgets.EndScrollView();

		if (recipes.Count == 0)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(gridOuter, "Nothing matches.");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	private static void AddBill(Building_WorkTable table, RecipeDef recipe)
	{
		if (!table.Map.mapPawns.FreeColonists.Any(c => recipe.PawnSatisfiesSkillRequirements(c)))
		{
			Bill.CreateNoPawnsWithSkillDialog(recipe);
		}
		Bill bill = BillUtility.MakeNewBill(recipe, null);
		table.billStack.AddBill(bill);
		if (recipe.conceptLearned != null)
		{
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
		}
		SoundDefOf.Tick_High.PlayOneShotOnCamera();
	}
}
