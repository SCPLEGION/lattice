using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

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
	public bool cinematicMenu = true;
	public bool menuContinue = true;
	public int olderVersionsBack = 2;

	// The field initializers above are the single source of defaults; saving and Reset read them from here.
	private static readonly LatticeSettings Defaults = new LatticeSettings();

	public override void ExposeData()
	{
		Scribe_Values.Look(ref columns, "columns", Defaults.columns);
		Scribe_Values.Look(ref cellSize, "cellSize", Defaults.cellSize);
		Scribe_Values.Look(ref showInfoPanel, "showInfoPanel", Defaults.showInfoPanel);
		Scribe_Values.Look(ref compactArchitect, "compactArchitect", Defaults.compactArchitect);
		Scribe_Values.Look(ref dock, "dock", Defaults.dock);
		Scribe_Values.Look(ref restyleWidgets, "restyleWidgets", Defaults.restyleWidgets);
		Scribe_Values.Look(ref movableWindows, "movableWindows", Defaults.movableWindows);
		Scribe_Values.Look(ref centerWindows, "centerWindows", Defaults.centerWindows);
		Scribe_Values.Look(ref pinWindows, "pinWindows", Defaults.pinWindows);
		Scribe_Values.Look(ref backdropMode, "backdropMode", Defaults.backdropMode);
		Scribe_Values.Look(ref backdropCustom, "backdropCustom", Defaults.backdropCustom);
		Scribe_Values.Look(ref accentR, "accentR", Defaults.accentR);
		Scribe_Values.Look(ref accentG, "accentG", Defaults.accentG);
		Scribe_Values.Look(ref accentB, "accentB", Defaults.accentB);
		Scribe_Values.Look(ref panelOpacity, "panelOpacity", Defaults.panelOpacity);
		Scribe_Values.Look(ref animations, "animations", Defaults.animations);
		Scribe_Values.Look(ref menuDim, "menuDim", Defaults.menuDim);
		Scribe_Values.Look(ref kenBurns, "kenBurns", Defaults.kenBurns);
		Scribe_Values.Look(ref menuUppercase, "menuUppercase", Defaults.menuUppercase);
		Scribe_Values.Look(ref cinematicMenu, "cinematicMenu", Defaults.cinematicMenu);
		Scribe_Values.Look(ref menuContinue, "menuContinue", Defaults.menuContinue);
		Scribe_Values.Look(ref olderVersionsBack, "olderVersionsBack", Defaults.olderVersionsBack);
	}

	private void ResetAll()
	{
		foreach (FieldInfo f in typeof(LatticeSettings).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
		{
			f.SetValue(this, f.GetValue(Defaults));
		}
	}

	// Settings window: a section sidebar on the left (version and reset at its foot), the selected section on the
	// right in its own scroll view. Rows are toggle switches and labelled sliders with a short dim description.

	private enum Section { Appearance, Windows, Architect, MainMenu, Mods, About }

	private static readonly (Section section, string label, string blurb)[] Sections =
	{
		(Section.Appearance, "Appearance", "Accent, panels, motion"),
		(Section.Windows, "Windows", "Dragging, pins, dock"),
		(Section.Architect, "Architect", "Grid and info panel"),
		(Section.MainMenu, "Main menu", "Layout and background"),
		(Section.Mods, "Mods", "Mod manager"),
		(Section.About, "About", "Version and credits"),
	};

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

	private const float SidebarWidth = 200f;
	private const float ContentMaxWidth = 560f;

	private Section section;
	private Vector2 scroll;
	private readonly Dictionary<Section, float> contentHeight = new Dictionary<Section, float>();

	public void DoWindowContents(Rect inRect)
	{
		Rect sidebar = new Rect(inRect.x, inRect.y, SidebarWidth, inRect.height);
		Rect content = new Rect(sidebar.xMax + 16f, inRect.y, inRect.width - SidebarWidth - 16f, inRect.height);
		DoSidebar(sidebar);

		UiStyle.DrawPanel(content, 0.55f, false);
		Rect inner = content.ContractedBy(18f);
		float width = Mathf.Min(ContentMaxWidth, inner.width - 20f);
		contentHeight.TryGetValue(section, out float height);
		Rect view = new Rect(0f, 0f, inner.width - 20f, Mathf.Max(height, inner.height));
		Widgets.BeginScrollView(inner, ref scroll, view);
		Listing_Standard list = new Listing_Standard();
		list.Begin(new Rect(0f, 0f, width, view.height));
		SectionTitle(list);
		switch (section)
		{
			case Section.Appearance:
				DoAppearance(list);
				break;
			case Section.Windows:
				DoWindows(list);
				break;
			case Section.Architect:
				DoArchitect(list);
				break;
			case Section.MainMenu:
				DoMenu(list);
				break;
			case Section.Mods:
				DoMods(list);
				break;
			case Section.About:
				DoAbout(list);
				break;
		}
		contentHeight[section] = list.CurHeight + 12f;
		list.End();
		Widgets.EndScrollView();
	}

	private void DoSidebar(Rect rect)
	{
		float y = rect.y;
		for (int i = 0; i < Sections.Length; i++)
		{
			(Section s, string label, string blurb) = Sections[i];
			Rect row = new Rect(rect.x, y, rect.width, 48f);
			bool selected = section == s;
			bool hover = Mouse.IsOver(row);
			float h = UiStyle.Smooth(UiStyle.Ease(UiStyle.Key(3001 + i, row), hover ? 1f : 0f, 10f));
			float on = UiStyle.Smooth(UiStyle.Ease(UiStyle.Key(3011 + i, row), selected ? 1f : 0f, 10f));

			Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f * h + 0.05f * on));
			if (on > 0f)
			{
				UiStyle.DrawGradient(row, UiStyle.WithAlpha(UiStyle.Accent, 0.12f * on), UiStyle.WithAlpha(UiStyle.Accent, 0.02f * on));
			}
			float edge = row.height * 0.6f * Mathf.Max(on, 0.35f * h);
			Widgets.DrawBoxSolid(new Rect(row.x, row.center.y - edge / 2f, 3f, edge), UiStyle.WithAlpha(UiStyle.Accent, Mathf.Max(on, 0.5f * h)));

			float tx = row.x + 14f + 4f * h;
			GUI.color = Color.Lerp(UiStyle.TextSoft, Color.Lerp(UiStyle.Accent, Color.white, 0.3f), on);
			Widgets.Label(new Rect(tx, row.y + 5f, row.width - 18f, 24f), label);
			Text.Font = GameFont.Tiny;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(new Rect(tx, row.y + 25f, row.width - 18f, 18f), blurb);
			Text.Font = GameFont.Small;
			GUI.color = Color.white;

			if (Widgets.ButtonInvisible(row) && !selected)
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				section = s;
				scroll = Vector2.zero;
			}
			y += row.height + 4f;
		}

		Rect reset = new Rect(rect.x, rect.yMax - 34f, rect.width, 34f);
		if (Widgets.ButtonText(reset, "Reset to defaults"))
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("Reset every Lattice setting to its default?", ResetAll, destructive: true));
		}
		Text.Font = GameFont.Tiny;
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(rect.x + 2f, reset.y - 22f, rect.width, 18f), "Lattice " + LatticeMod.Version);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
	}

	private void SectionTitle(Listing_Standard list)
	{
		string label = Array.Find(Sections, s => s.section == section).label;
		Rect r = list.GetRect(30f);
		Text.Font = GameFont.Medium;
		Widgets.Label(r, label);
		Text.Font = GameFont.Small;
		Widgets.DrawBoxSolid(new Rect(r.x, r.yMax + 2f, 36f, 2f), UiStyle.Accent);
		list.Gap(18f);
	}

	private static void Group(Listing_Standard list, string text)
	{
		list.Gap(10f);
		Rect r = list.GetRect(20f);
		Text.Font = GameFont.Tiny;
		GUI.color = UiStyle.WithAlpha(UiStyle.Accent, 0.85f);
		Widgets.Label(r, text.ToUpperInvariant());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Widgets.DrawBoxSolid(new Rect(r.x, r.yMax, r.width, 1f), UiStyle.Hairline);
		list.Gap(6f);
	}

	// A full-width row with a label (and optional description) on the left and a pill switch on the right.
	private static void Toggle(Listing_Standard list, string label, ref bool value, string desc = null)
	{
		Rect r = list.GetRect(desc == null ? 32f : 48f);
		bool hover = Mouse.IsOver(r);
		float h = UiStyle.Ease(UiStyle.Key(3101, r), hover ? 1f : 0f, 10f);
		float t = UiStyle.Smooth(UiStyle.Ease(UiStyle.Key(3102, r), value ? 1f : 0f, 9f));
		Widgets.DrawBoxSolid(r, new Color(1f, 1f, 1f, 0.035f * h));

		Rect pill = new Rect(r.xMax - 46f, r.y + 7f, 38f, 18f);
		Widgets.DrawBoxSolid(pill, Color.Lerp(new Color(1f, 1f, 1f, 0.12f + 0.05f * h), UiStyle.WithAlpha(UiStyle.Accent, 0.9f), t));
		float k = pill.height - 4f;
		Widgets.DrawBoxSolid(new Rect(pill.x + 2f + t * (pill.width - k - 4f), pill.y + 2f, k, k), new Color(1f, 1f, 1f, 0.75f + 0.25f * t));

		GUI.color = value ? Color.white : UiStyle.TextSoft;
		Widgets.Label(new Rect(r.x + 6f, r.y + 5f, r.width - 64f, 24f), label);
		if (desc != null)
		{
			Text.Font = GameFont.Tiny;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(new Rect(r.x + 6f, r.y + 26f, r.width - 64f, 20f), desc);
			Text.Font = GameFont.Small;
		}
		GUI.color = Color.white;

		if (Widgets.ButtonInvisible(r))
		{
			value = !value;
			(value ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
		}
		list.Gap(2f);
	}

	// Label on the left, current value in the accent colour on the right, slider underneath.
	private static float Slider(Listing_Standard list, string label, string valueText, float value, float min, float max, float step, string desc = null)
	{
		Rect r = list.GetRect(desc == null ? 52f : 68f);
		GUI.color = UiStyle.TextSoft;
		Widgets.Label(new Rect(r.x + 6f, r.y + 4f, r.width - 90f, 24f), label);
		Text.Anchor = TextAnchor.UpperRight;
		GUI.color = UiStyle.Accent;
		Widgets.Label(new Rect(r.xMax - 90f, r.y + 4f, 84f, 24f), valueText);
		Text.Anchor = TextAnchor.UpperLeft;
		float y = r.y + 28f;
		if (desc != null)
		{
			Text.Font = GameFont.Tiny;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(new Rect(r.x + 6f, y - 4f, r.width - 12f, 20f), desc);
			Text.Font = GameFont.Small;
			y += 16f;
		}
		GUI.color = Color.white;
		value = Widgets.HorizontalSlider(new Rect(r.x + 6f, y, r.width - 12f, 20f), value, min, max);
		list.Gap(4f);
		return Mathf.Round(value / step) * step;
	}

	// Row of equal buttons; the selected one is filled with the accent colour.
	private static int Segmented(Listing_Standard list, string[] labels, int selected)
	{
		Rect r = list.GetRect(32f);
		float w = r.width / labels.Length;
		for (int i = 0; i < labels.Length; i++)
		{
			Rect seg = new Rect(r.x + i * w, r.y, w - 2f, r.height);
			bool on = i == selected;
			float h = UiStyle.Ease(UiStyle.Key(3201 + i, seg), Mouse.IsOver(seg) ? 1f : 0f, 10f);
			Widgets.DrawBoxSolid(seg, on ? UiStyle.WithAlpha(UiStyle.Accent, 0.85f) : new Color(1f, 1f, 1f, 0.06f + 0.06f * h));
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = on ? new Color(0.06f, 0.06f, 0.08f) : UiStyle.TextSoft;
			Widgets.Label(seg, labels[i]);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			if (!on && Widgets.ButtonInvisible(seg))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				selected = i;
			}
		}
		list.Gap(6f);
		return selected;
	}

	private static void Note(Listing_Standard list, string text)
	{
		Text.Font = GameFont.Tiny;
		Rect r = list.GetRect(Text.CalcHeight(text, list.ColumnWidth - 12f) + 4f);
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(r.x + 6f, r.y, r.width - 12f, r.height), text);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
	}

	private void DoAppearance(Listing_Standard list)
	{
		DrawAccentPreview(list.GetRect(96f));
		list.Gap(8f);

		Group(list, "Accent colour");
		Rect row = list.GetRect(34f);
		float x = row.x + 6f;
		foreach ((string name, Color color) in Swatches)
		{
			Rect box = new Rect(x, row.y + 2f, 30f, 30f);
			bool on = Mathf.Abs(accentR - color.r) + Mathf.Abs(accentG - color.g) + Mathf.Abs(accentB - color.b) < 0.02f;
			float h = UiStyle.Ease(UiStyle.Key(3301, box), Mouse.IsOver(box) ? 1f : 0f, 10f);
			Rect sw = box.ContractedBy(2f - 2f * h);
			Widgets.DrawBoxSolid(sw, color);
			if (on)
			{
				Widgets.DrawBoxSolidWithOutline(box.ExpandedBy(3f), Color.clear, Color.white, 2);
			}
			TooltipHandler.TipRegion(box, name);
			if (Widgets.ButtonInvisible(box))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				accentR = color.r;
				accentG = color.g;
				accentB = color.b;
			}
			x += 40f;
		}
		list.Gap(6f);
		accentR = Slider(list, "Red", $"{accentR:0.00}", accentR, 0f, 1f, 0.01f);
		accentG = Slider(list, "Green", $"{accentG:0.00}", accentG, 0f, 1f, 0.01f);
		accentB = Slider(list, "Blue", $"{accentB:0.00}", accentB, 0f, 1f, 0.01f);

		Group(list, "Panels and motion");
		panelOpacity = Slider(list, "Panel opacity", $"{panelOpacity * 100f:0}%", panelOpacity, 0.4f, 1f, 0.05f, "How much of the map shows through windows and panels.");
		Toggle(list, "Animations", ref animations, "Hover glow, slides and fades. Off = everything is instant.");
		Toggle(list, "Restyle vanilla widgets", ref restyleWidgets, "Buttons, windows, tabs and lists everywhere. Turn off if another UI mod conflicts.");
	}

	// A small mock panel using the live accent and opacity: selected row, button, underline.
	private static void DrawAccentPreview(Rect r)
	{
		Widgets.DrawBoxSolid(r, new Color(0.25f, 0.3f, 0.22f, 1f));
		for (int i = 0; i < 6; i++)
		{
			Widgets.DrawBoxSolid(new Rect(r.x + i * r.width / 6f, r.y, r.width / 12f, r.height), new Color(0.3f, 0.36f, 0.26f, 1f));
		}
		Rect panel = r.ContractedBy(12f);
		UiStyle.DrawPanel(panel);
		Rect sel = new Rect(panel.x + 10f, panel.y + 10f, 150f, panel.height - 20f);
		UiStyle.DrawGlow(sel.ExpandedBy(8f), UiStyle.AccentGlow, 0.5f);
		UiStyle.DrawGradient(sel, UiStyle.WithAlpha(UiStyle.Accent, 0.26f), UiStyle.WithAlpha(UiStyle.Accent, 0.06f));
		Widgets.DrawBoxSolidWithOutline(sel, Color.clear, UiStyle.Accent, 1);
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(sel, "Selected");
		Rect text = new Rect(sel.xMax + 16f, panel.y + 10f, panel.width - sel.width - 36f, 24f);
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = UiStyle.TextSoft;
		Widgets.Label(text, "Preview");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		UiStyle.DrawAccentLine(new Rect(text.x, text.yMax, 70f, 4f), UiStyle.Pulse(1.6f) * 0.5f + 0.5f, false);
		Text.Font = GameFont.Tiny;
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(text.x, text.yMax + 8f, text.width, 20f), "Accent and panel opacity update live.");
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
	}

	private void DoWindows(Listing_Standard list)
	{
		Group(list, "Behaviour");
		Toggle(list, "Movable windows", ref movableWindows, "Drag any window by its edge or title.");
		Toggle(list, "Centered tab windows", ref centerWindows, "Work, research and other tabs open in the middle of the screen.");
		Toggle(list, "Pin button", ref pinWindows, "Pin a window to keep several tabs open at once.");
		Group(list, "Bottom bar");
		Toggle(list, "Icon dock", ref dock, "A centered dock of icons instead of the full-width bottom bar.");
	}

	private void DoArchitect(Listing_Standard list)
	{
		Group(list, "Grid");
		columns = Mathf.RoundToInt(Slider(list, "Columns", columns.ToString(), columns, 4f, 12f, 1f));
		cellSize = Slider(list, "Cell size", $"{cellSize:0} px", cellSize, 64f, 110f, 1f);
		Group(list, "Layout");
		Toggle(list, "Info panel", ref showInfoPanel, "Description, cost and stats next to the grid.");
		Toggle(list, "Slim bar after selecting", ref compactArchitect, "The window shrinks to a toolbar once you pick something to build.");
	}

	private void DoMenu(Listing_Standard list)
	{
		Group(list, "Layout");
		Toggle(list, "Cinematic main menu", ref cinematicMenu, "Off = vanilla layout. Also off while vanilla restyling is off.");
		Toggle(list, "Continue card", ref menuContinue, "Your latest save above the options; one click loads it.");
		Toggle(list, "Capital letters", ref menuUppercase, "Option labels in capitals.");
		menuDim = Slider(list, "Shade behind the options", $"{menuDim * 100f:0}%", menuDim, 0f, 1f, 0.05f);

		Group(list, "Background");
		DrawBackdropPreview(list.GetRect(Mathf.Min(list.ColumnWidth, 360f) * 9f / 16f));
		list.Gap(8f);
		backdropMode = (BackdropMode)Segmented(list, new[] { "Latest save", "Most played", "Pick one" }, (int)backdropMode);
		if (backdropMode == BackdropMode.Custom && list.ButtonText(backdropCustom.NullOrEmpty() ? "Pick an image..." : System.IO.Path.GetFileName(backdropCustom)))
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
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
		Toggle(list, "Slow zoom and drift", ref kenBurns, "Ken Burns style camera motion on the background.");
		Note(list, "A map screenshot (no UI) is taken after every save and kept in the config folder under Lattice/Backdrops. You can drop your own JPGs there too.");
	}

	// The image the menu will show, with the left shade at the current strength.
	private void DrawBackdropPreview(Rect r)
	{
		r.width = r.height * 16f / 9f;
		Backdrops.Entry entry = Backdrops.Pick();
		Texture2D tex = Backdrops.Thumbnail(entry);
		Widgets.DrawBoxSolid(r, new Color(0f, 0f, 0f, 0.5f));
		if (tex != null)
		{
			float texAspect = tex.width / (float)tex.height, boxAspect = r.width / r.height;
			Rect uv = texAspect > boxAspect ? new Rect((1f - boxAspect / texAspect) / 2f, 0f, boxAspect / texAspect, 1f) : new Rect(0f, (1f - texAspect / boxAspect) / 2f, 1f, texAspect / boxAspect);
			GUI.DrawTextureWithTexCoords(r, tex, uv);
			if (cinematicMenu && restyleWidgets)
			{
				GUI.color = new Color(0.02f, 0.025f, 0.04f, 0.8f * menuDim);
				GUI.DrawTexture(new Rect(r.x, r.y, r.width * 0.68f, r.height), Patch_MenuOverlay.Ramp);
				GUI.color = Color.white;
			}
		}
		else
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = UiStyle.TextDim;
			Widgets.Label(r, "No screenshot yet\n(vanilla artwork)");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
		Widgets.DrawBoxSolidWithOutline(r, Color.clear, UiStyle.Hairline, 1);
		if (entry != null)
		{
			Text.Font = GameFont.Tiny;
			GUI.color = UiStyle.TextSoft;
			Widgets.Label(new Rect(r.xMax + 12f, r.yMax - 36f, 200f, 18f), entry.colony);
			GUI.color = UiStyle.TextDim;
			Widgets.Label(new Rect(r.xMax + 12f, r.yMax - 20f, 200f, 18f), entry.playHours > 0f ? $"{entry.playHours:0.#} h played" : entry.name);
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
		}
	}

	private void DoMods(Listing_Standard list)
	{
		Group(list, "Version status");
		string cur = VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor;
		string range = olderVersionsBack <= 0 ? "off" : olderVersionsBack == 1 ? "1 version" : olderVersionsBack + " versions";
		olderVersionsBack = Mathf.RoundToInt(Slider(list, "Count older versions as \"Older\"", range, olderVersionsBack, 0f, 3f, 1f,
			$"Mods made only for the last few versions before {cur} get a blue \"Older\" state instead of red: they usually still load."));
		if (olderVersionsBack > 0)
		{
			string from = VersionControl.CurrentMajor + "." + Mathf.Max(0, VersionControl.CurrentMinor - olderVersionsBack);
			string to = VersionControl.CurrentMajor + "." + (VersionControl.CurrentMinor - 1);
			Note(list, from == to ? $"Now: mods for {to} count as Older." : $"Now: mods for {from} to {to} count as Older.");
		}
		Note(list, "Older mods are not counted as issues and pass the \"Compatible only\" filter. They get their own category in the mods screen.");
	}

	private void DoAbout(Listing_Standard list)
	{
		Rect r = list.GetRect(40f);
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(r.x + 6f, r.y, r.width, 34f), "Lattice " + LatticeMod.Version);
		Text.Font = GameFont.Small;
		Note(list, "A RimWorld 1.7 UI overhaul by SCPLEGION. MIT licensed.");

		Group(list, "Versioning");
		Note(list, "RELEASE.MINOR.PATCH\n\nRELEASE - a big rework that changes how things look or behave.\nMINOR - new features and options; settings carry over.\nPATCH - fixes only (the \"shame\" number: it goes up when something shipped broken).");

		Group(list, "Links");
		if (list.ButtonText("Source code and issues (GitHub)"))
		{
			Application.OpenURL("https://github.com/SCPLEGION/lattice");
		}
	}
}
