using UnityEngine;
using Verse;

namespace Lattice;

public class LatticeSettings : ModSettings
{
	public int columns = 8;
	public float cellSize = 84f;
	public bool showInfoPanel = true;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref columns, "columns", 8);
		Scribe_Values.Look(ref cellSize, "cellSize", 84f);
		Scribe_Values.Look(ref showInfoPanel, "showInfoPanel", true);
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
		if (list.ButtonText("Reset to defaults"))
		{
			columns = 8;
			cellSize = 84f;
			showInfoPanel = true;
		}
		list.End();
	}
}
