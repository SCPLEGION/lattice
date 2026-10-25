using UnityEngine;
using Verse;

namespace Lattice;

public enum BackdropMode { Latest, MostPlayed, Custom }

public class LatticeSettings : ModSettings
{
	public int columns = 8;
	public float cellSize = 84f;
	public bool showInfoPanel = true;
	public bool dock = true;
	public bool restyleWidgets = true;
	public bool movableWindows = true;
	public bool centerWindows = true;
	public BackdropMode backdropMode = BackdropMode.Latest;
	public string backdropCustom = "";

	public override void ExposeData()
	{
		Scribe_Values.Look(ref columns, "columns", 8);
		Scribe_Values.Look(ref cellSize, "cellSize", 84f);
		Scribe_Values.Look(ref showInfoPanel, "showInfoPanel", true);
		Scribe_Values.Look(ref dock, "dock", true);
		Scribe_Values.Look(ref restyleWidgets, "restyleWidgets", true);
		Scribe_Values.Look(ref movableWindows, "movableWindows", true);
		Scribe_Values.Look(ref centerWindows, "centerWindows", true);
		Scribe_Values.Look(ref backdropMode, "backdropMode", BackdropMode.Latest);
		Scribe_Values.Look(ref backdropCustom, "backdropCustom", "");
	}

	public void DoWindowContents(Rect inRect)
	{
		Listing_Standard list = new Listing_Standard();
		list.Begin(inRect);
		list.Label($"Architect grid columns: {columns}");
		columns = Mathf.RoundToInt(list.Slider(columns, 4f, 12f));
		list.Label($"Architect cell size: {cellSize:0}px");
		cellSize = Mathf.Round(list.Slider(cellSize, 64f, 110f));
		list.CheckboxLabeled("Show info panel next to the architect grid", ref showInfoPanel);
		list.CheckboxLabeled("Centered icon dock instead of the full-width bottom bar", ref dock);
		list.CheckboxLabeled("Restyle vanilla buttons, windows, tabs and lists everywhere (turn off if another UI mod conflicts)", ref restyleWidgets);
		list.CheckboxLabeled("Windows can be dragged anywhere", ref movableWindows);
		list.CheckboxLabeled("Open tab windows (work, research...) in the center", ref centerWindows);
		list.GapLine();
		list.Label("Main menu background");
		if (list.ButtonText($"Mode: {backdropMode}"))
		{
			Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption>
			{
				new FloatMenuOption("Latest colony (last save)", () => backdropMode = BackdropMode.Latest),
				new FloatMenuOption("Most played colony", () => backdropMode = BackdropMode.MostPlayed),
				new FloatMenuOption("Custom (pick below)", () => backdropMode = BackdropMode.Custom),
			}));
		}
		if (backdropMode == BackdropMode.Custom)
		{
			if (list.ButtonText(backdropCustom.NullOrEmpty() ? "Pick an image..." : System.IO.Path.GetFileName(backdropCustom)))
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
		list.Gap(6f);
		if (list.ButtonText("Reset to defaults"))
		{
			columns = 8;
			cellSize = 84f;
			showInfoPanel = true;
			dock = true;
			restyleWidgets = true;
			movableWindows = true;
			centerWindows = true;
			backdropMode = BackdropMode.Latest;
			backdropCustom = "";
		}
		list.End();
	}
}
