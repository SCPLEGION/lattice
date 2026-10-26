using UnityEngine;
using Verse;

namespace Lattice;

public enum BackdropMode { Latest, MostPlayed, Custom }

public class LatticeSettings : ModSettings
{
	public int columns = 8;
	public float cellSize = 84f;
	public bool showInfoPanel = true;
	public bool compactArchitect = true;
	public bool dock = true;
	public bool restyleWidgets = true;
	public bool movableWindows = true;
	public bool centerWindows = true;
	public bool pinWindows = true;
	public BackdropMode backdropMode = BackdropMode.Latest;
	public string backdropCustom = "";
	public float accentR = 0.98f, accentG = 0.74f, accentB = 0.28f;
	public float panelOpacity = 1f;
	public bool animations = true;
	public float menuDim = 1f;
	public bool kenBurns = true;
	public bool menuUppercase = true;
	private Vector2 scroll;
	private float contentHeight = 1400f;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref columns, "columns", 8);
		Scribe_Values.Look(ref cellSize, "cellSize", 84f);
		Scribe_Values.Look(ref showInfoPanel, "showInfoPanel", true);
		Scribe_Values.Look(ref compactArchitect, "compactArchitect", true);
		Scribe_Values.Look(ref dock, "dock", true);
		Scribe_Values.Look(ref restyleWidgets, "restyleWidgets", true);
		Scribe_Values.Look(ref movableWindows, "movableWindows", true);
		Scribe_Values.Look(ref centerWindows, "centerWindows", true);
		Scribe_Values.Look(ref pinWindows, "pinWindows", true);
		Scribe_Values.Look(ref backdropMode, "backdropMode", BackdropMode.Latest);
		Scribe_Values.Look(ref backdropCustom, "backdropCustom", "");
		Scribe_Values.Look(ref accentR, "accentR", 0.98f);
		Scribe_Values.Look(ref accentG, "accentG", 0.74f);
		Scribe_Values.Look(ref accentB, "accentB", 0.28f);
		Scribe_Values.Look(ref panelOpacity, "panelOpacity", 1f);
		Scribe_Values.Look(ref animations, "animations", true);
		Scribe_Values.Look(ref menuDim, "menuDim", 1f);
		Scribe_Values.Look(ref kenBurns, "kenBurns", true);
		Scribe_Values.Look(ref menuUppercase, "menuUppercase", true);
	}

	private static readonly (string name, Color color)[] Swatches =
	{
		("Amber", new Color(0.98f, 0.74f, 0.28f)),
		("Cyan", new Color(0.35f, 0.82f, 0.95f)),
		("Mint", new Color(0.4f, 0.9f, 0.6f)),
		("Rose", new Color(0.96f, 0.45f, 0.6f)),
		("Violet", new Color(0.7f, 0.55f, 1f)),
		("Coral", new Color(1f, 0.5f, 0.35f)),
		("Ice", new Color(0.85f, 0.92f, 1f)),
	};

	private void Heading(Listing_Standard list, string text)
	{
		list.Gap(8f);
		Rect r = list.GetRect(24f);
		Text.Font = GameFont.Medium;
		Widgets.Label(r, text);
		Text.Font = GameFont.Small;
		Widgets.DrawBoxSolid(new Rect(r.x, r.yMax, 36f, 2f), UiStyle.Accent);
		list.Gap(8f);
	}

	private void DoAppearance(Listing_Standard list)
	{
		Heading(list, "Appearance");
		list.Label("Accent color");
		Rect row = list.GetRect(30f);
		float x = row.x;
		foreach ((string name, Color color) in Swatches)
		{
			Rect box = new Rect(x, row.y, 30f, 30f);
			bool on = Mathf.Abs(accentR - color.r) + Mathf.Abs(accentG - color.g) + Mathf.Abs(accentB - color.b) < 0.02f;
			Widgets.DrawBoxSolid(box, color);
			if (on)
			{
				Widgets.DrawBoxSolidWithOutline(box.ExpandedBy(3f), Color.clear, Color.white, 2);
			}
			TooltipHandler.TipRegion(box, name);
			if (Widgets.ButtonInvisible(box))
			{
				accentR = color.r;
				accentG = color.g;
				accentB = color.b;
			}
			x += 38f;
		}
		list.Label($"Red {accentR:0.00}");
		accentR = list.Slider(accentR, 0f, 1f);
		list.Label($"Green {accentG:0.00}");
		accentG = list.Slider(accentG, 0f, 1f);
		list.Label($"Blue {accentB:0.00}");
		accentB = list.Slider(accentB, 0f, 1f);
		list.Label($"Panel opacity: {panelOpacity * 100f:0}%");
		panelOpacity = Mathf.Round(list.Slider(panelOpacity, 0.4f, 1f) * 20f) / 20f;
		list.CheckboxLabeled("Animations (hover glow, slides); off = instant", ref animations);
		list.CheckboxLabeled("Restyle vanilla buttons, windows, tabs and lists everywhere (turn off if another UI mod conflicts)", ref restyleWidgets);
	}

	private void DoWindows(Listing_Standard list)
	{
		Heading(list, "Windows");
		list.CheckboxLabeled("Windows can be dragged anywhere", ref movableWindows);
		list.CheckboxLabeled("Open tab windows (work, research...) in the center", ref centerWindows);
		list.CheckboxLabeled("Pin button on windows (keep several open at once)", ref pinWindows);
		list.CheckboxLabeled("Centered icon dock instead of the full-width bottom bar", ref dock);
	}

	private void DoArchitect(Listing_Standard list)
	{
		Heading(list, "Architect");
		list.Label($"Grid columns: {columns}");
		columns = Mathf.RoundToInt(list.Slider(columns, 4f, 12f));
		list.Label($"Cell size: {cellSize:0}px");
		cellSize = Mathf.Round(list.Slider(cellSize, 64f, 110f));
		list.CheckboxLabeled("Show info panel next to the grid", ref showInfoPanel);
		list.CheckboxLabeled("Shrink to a slim bar once something is selected", ref compactArchitect);
	}

	private void DoMenu(Listing_Standard list)
	{
		Heading(list, "Main menu");
		list.Label($"Shade behind the options: {menuDim * 100f:0}%");
		menuDim = Mathf.Round(list.Slider(menuDim, 0f, 1f) * 20f) / 20f;
		list.CheckboxLabeled("Slow zoom and drift on the background (Ken Burns)", ref kenBurns);
		list.CheckboxLabeled("Option labels in capitals", ref menuUppercase);
		list.Label("Background");
		if (list.ButtonText($"Mode: {backdropMode}"))
		{
			Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption>
			{
				new FloatMenuOption("Latest colony (last save)", () => backdropMode = BackdropMode.Latest),
				new FloatMenuOption("Most played colony", () => backdropMode = BackdropMode.MostPlayed),
				new FloatMenuOption("Custom (pick below)", () => backdropMode = BackdropMode.Custom),
			}));
		}
		if (backdropMode == BackdropMode.Custom && list.ButtonText(backdropCustom.NullOrEmpty() ? "Pick an image..." : System.IO.Path.GetFileName(backdropCustom)))
		{
			var options = new System.Collections.Generic.List<FloatMenuOption>();
			foreach (Backdrops.Entry e in Backdrops.All())
			{
				Backdrops.Entry captured = e;
				options.Add(new FloatMenuOption($"{e.colony} ({e.name}, {e.playHours:0.#} h)", () => backdropCustom = captured.image));
			}
			if (options.Count == 0)
			{
				options.Add(new FloatMenuOption("No screenshots yet - save a game first (or put images in the Lattice/Backdrops config folder)", null));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}
	}

	private void ResetAll()
	{
		columns = 8;
		cellSize = 84f;
		showInfoPanel = true;
		compactArchitect = true;
		dock = true;
		restyleWidgets = true;
		movableWindows = true;
		centerWindows = true;
		pinWindows = true;
		backdropMode = BackdropMode.Latest;
		backdropCustom = "";
		accentR = 0.98f;
		accentG = 0.74f;
		accentB = 0.28f;
		panelOpacity = 1f;
		animations = true;
		menuDim = 1f;
		kenBurns = true;
		menuUppercase = true;
	}

	public void DoWindowContents(Rect inRect)
	{
		Rect view = new Rect(0f, 0f, inRect.width - 20f, contentHeight);
		Widgets.BeginScrollView(inRect, ref scroll, view);
		Listing_Standard list = new Listing_Standard();
		list.Begin(view);
		DoAppearance(list);
		DoWindows(list);
		DoArchitect(list);
		DoMenu(list);
		list.Gap(10f);
		if (list.ButtonText("Reset to defaults"))
		{
			ResetAll();
		}
		contentHeight = list.CurHeight + 20f;
		list.End();
		Widgets.EndScrollView();
	}
}
