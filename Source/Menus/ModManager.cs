using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Mod manager as a card grid (preview image, name, author, state, one-click toggle) with a category
// sidebar, search, filters, sorting, grouping, presets, favourites/tags, multi-select and a detail drawer.
// It is a view on the vanilla page: activation, requirements, saving, workshop buttons and the details pane
// are the vanilla methods. The load-order list view stays available (toggle) for drag-reordering.
public static class ModManager
{
	private enum Category { All, Enabled, Disabled, Favourites, Official, Workshop, Local, Issues, Older }
	private enum SortBy { Name, Status, Source, Version, Author, Size }
	private enum VerMode { Any, Supports, Excludes, Below, AtLeast }
	private enum GroupBy { None, Author, Version, Source, Tag }

	public static bool ListMode;

	private static readonly AccessTools.FieldRef<Page_ModsConfig, ModMetaData> Primary =
		AccessTools.FieldRefAccess<Page_ModsConfig, ModMetaData>("primarySelectedMod");
	private static readonly AccessTools.FieldRef<Page_ModsConfig, bool> Dirty =
		AccessTools.FieldRefAccess<Page_ModsConfig, bool>("modListsDirty");
	private static Action<Page_ModsConfig, ModMetaData> SelectMod;
	private static Func<Page_ModsConfig, ModMetaData, bool> TryActivate;
	private static Action<Page_ModsConfig, ModMetaData> TryDeactivate;
	private static Action<Page_ModsConfig, Rect, ModMetaData> DoInfo;
	private static Action<Page_ModsConfig, Rect> DoBottom;
	private static Action<Page_ModsConfig, IEnumerable<ModMetaData>> Unsubscribe;

	// False when a vanilla method could not be bound; the page then falls back to the vanilla list.
	public static readonly bool Ready;

	static ModManager()
	{
		try
		{
			Type page = typeof(Page_ModsConfig);
			SelectMod = AccessTools.MethodDelegate<Action<Page_ModsConfig, ModMetaData>>(AccessTools.Method(page, "SelectMod", new[] { typeof(ModMetaData) }));
			TryActivate = AccessTools.MethodDelegate<Func<Page_ModsConfig, ModMetaData, bool>>(AccessTools.Method(page, "TrySetModActive", new[] { typeof(ModMetaData) }));
			TryDeactivate = AccessTools.MethodDelegate<Action<Page_ModsConfig, ModMetaData>>(AccessTools.Method(page, "TrySetModInactive", new[] { typeof(ModMetaData) }));
			DoInfo = AccessTools.MethodDelegate<Action<Page_ModsConfig, Rect, ModMetaData>>(AccessTools.Method(page, "DoModInfo", new[] { typeof(Rect), typeof(ModMetaData) }));
			DoBottom = AccessTools.MethodDelegate<Action<Page_ModsConfig, Rect>>(AccessTools.Method(page, "DoBottomButtons", new[] { typeof(Rect) }));
			Ready = true;
			try
			{
				Unsubscribe = AccessTools.MethodDelegate<Action<Page_ModsConfig, IEnumerable<ModMetaData>>>(AccessTools.Method(page, "UnsubscribeMods", new[] { typeof(IEnumerable<ModMetaData>) }));
			}
			catch (Exception ex)
			{
				Log.Warning("[Lattice] Bulk unsubscribe unavailable: " + ex.Message);
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[Lattice] Mod manager cards disabled, vanilla list used instead: " + ex.Message);
		}
	}

	// ---- state (persisted through ModStore.Ui) -------------------------------------------------

	private static Category category;
	private static SortBy sortBy;
	private static string search = "";
	private static Vector2 scroll;
	private static List<ModMetaData> all;
	private static HashSet<ModMetaData> issues = new HashSet<ModMetaData>();
	private static int cachedAt = -1000;

	private static VerMode verMode;
	private static int verKey = VKey(VersionControl.CurrentVersion);
	private static bool compatibleOnly, conflictsOnly, missingDepsOnly, duplicatesOnly, sortDesc, compact;
	private static float cardScale = 1f;
	private static GroupBy groupBy;
	private static readonly HashSet<string> tagFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static HashSet<ModMetaData> conflicts = new HashSet<ModMetaData>();
	private static HashSet<ModMetaData> missingDeps = new HashSet<ModMetaData>();
	private static HashSet<ModMetaData> duplicates = new HashSet<ModMetaData>();
	private static List<int> knownVersions = new List<int>();
	private static readonly HashSet<ModMetaData> selection = new HashSet<ModMetaData>();
	private static List<ModMetaData> displayed = new List<ModMetaData>();
	private static ModMetaData lastClicked;
	private static bool stateLoaded;

	private static int VKey(Version v) => v.Major * 1000 + Math.Max(v.Minor, 0);
	private static string VLabel(int key) => (key / 1000) + "." + (key % 1000);
	private static readonly Color OlderColor = new Color(0.45f, 0.78f, 1f);

	// Not marked for this game version, but made for one of the last few minor versions of the same major
	// (1.7 usually still loads 1.6 and 1.5 mods). How far back counts is a setting; 0 turns the state off.
	private static bool Older(ModMetaData m)
	{
		int back = LatticeMod.Settings.olderVersionsBack;
		if (back <= 0 || m.VersionCompatible || m.SupportedVersionsReadOnly == null)
		{
			return false;
		}
		int major = VersionControl.CurrentMajor, minor = VersionControl.CurrentMinor;
		return m.SupportedVersionsReadOnly.Any(v => v.Major == major && v.Minor < minor && v.Minor >= minor - back);
	}

	// Really not made for this version: neither compatible nor recent enough to count as older.
	private static bool Bad(ModMetaData m) => !m.VersionCompatible && !Older(m);

	private static int TopVersion(ModMetaData m) => m.SupportedVersionsReadOnly != null && m.SupportedVersionsReadOnly.Count > 0 ? m.SupportedVersionsReadOnly.Max(VKey) : -1;
	private static bool Filtering => verMode != VerMode.Any || compatibleOnly || conflictsOnly || missingDepsOnly || duplicatesOnly || tagFilter.Count > 0;

	private static string UiVal(string key, string fallback) => ModStore.Ui.TryGetValue(key, out string v) ? v : fallback;
	private static bool UiBool(string key) => UiVal(key, "0") == "1";
	private static int UiInt(string key, int fallback) => int.TryParse(UiVal(key, ""), out int v) ? v : fallback;
	private static T UiEnum<T>(string key) where T : struct => Enum.TryParse(UiVal(key, ""), out T v) ? v : default;

	private static void LoadState()
	{
		if (stateLoaded)
		{
			return;
		}
		stateLoaded = true;
		ModStore.Load();
		category = UiEnum<Category>("cat");
		sortBy = UiEnum<SortBy>("sort");
		groupBy = UiEnum<GroupBy>("group");
		verMode = UiEnum<VerMode>("vmode");
		verKey = UiInt("vkey", verKey);
		sortDesc = UiBool("desc");
		compatibleOnly = UiBool("compat");
		conflictsOnly = UiBool("conf");
		missingDepsOnly = UiBool("deps");
		duplicatesOnly = UiBool("dups");
		compact = UiBool("compact");
		cardScale = Mathf.Clamp(float.TryParse(UiVal("scale", "1"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sc) ? sc : 1f, 0.7f, 1.3f);
		search = UiVal("search", "");
		scroll = new Vector2(0f, UiInt("scrolly", 0));
		foreach (string t in UiVal("tagfilter", "").Split(';'))
		{
			if (t.Length > 0)
			{
				tagFilter.Add(t);
			}
		}
		if (ModStore.LastVersion.NullOrEmpty())
		{
			ModStore.LastVersion = VLabel(VKey(VersionControl.CurrentVersion));
		}
	}

	public static void SaveState()
	{
		if (!stateLoaded)
		{
			return;
		}
		Dictionary<string, string> ui = ModStore.Ui;
		ui["cat"] = category.ToString();
		ui["sort"] = sortBy.ToString();
		ui["group"] = groupBy.ToString();
		ui["vmode"] = verMode.ToString();
		ui["vkey"] = verKey.ToString();
		ui["desc"] = sortDesc ? "1" : "0";
		ui["compat"] = compatibleOnly ? "1" : "0";
		ui["conf"] = conflictsOnly ? "1" : "0";
		ui["deps"] = missingDepsOnly ? "1" : "0";
		ui["dups"] = duplicatesOnly ? "1" : "0";
		ui["compact"] = compact ? "1" : "0";
		ui["scale"] = cardScale.ToString(System.Globalization.CultureInfo.InvariantCulture);
		ui["search"] = search ?? "";
		ui["scrolly"] = Mathf.RoundToInt(scroll.y).ToString();
		ui["tagfilter"] = string.Join(";", tagFilter);
		ModStore.Save();
	}

	// ---- data ----------------------------------------------------------------------------------

	private static ModMetaData FindInstalled(string id) =>
		all?.FirstOrDefault(m => string.Equals(m.PackageIdNonUnique, id, StringComparison.OrdinalIgnoreCase));

	private static void Refresh(bool force)
	{
		if (!force && all != null && Time.frameCount - cachedAt < 45)
		{
			return;
		}
		cachedAt = Time.frameCount;
		all = ModLister.AllInstalledMods.ToList();
		HashSet<string> activeIds = new HashSet<string>(all.Where(m => m.Active).Select(m => m.PackageIdNonUnique), StringComparer.OrdinalIgnoreCase);
		conflicts = new HashSet<ModMetaData>(all.Where(m => m.Active && m.IncompatibleWith != null
			&& m.IncompatibleWith.Any(id => !id.NullOrEmpty() && !id.Equals(m.PackageIdNonUnique, StringComparison.OrdinalIgnoreCase) && activeIds.Contains(id))));
		missingDeps = new HashSet<ModMetaData>(all.Where(m => m.Active && m.UnsatisfiedDependencies().Count > 0));
		issues = new HashSet<ModMetaData>(all.Where(m => m.Active && Bad(m)).Concat(missingDeps).Concat(conflicts));
		duplicates = new HashSet<ModMetaData>(all.GroupBy(m => m.PackageIdNonUnique, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).SelectMany(g => g));
		knownVersions = all.Where(m => m.SupportedVersionsReadOnly != null).SelectMany(m => m.SupportedVersionsReadOnly).Select(VKey).Distinct().OrderByDescending(k => k).ToList();
		selection.RemoveWhere(m => !all.Contains(m));
		ModStore.MeasureAll(all);
	}

	private static bool InCategory(ModMetaData m, Category c)
	{
		switch (c)
		{
			case Category.Enabled: return m.Active;
			case Category.Disabled: return !m.Active;
			case Category.Favourites: return ModStore.IsFavourite(m);
			case Category.Official: return m.Official;
			case Category.Workshop: return m.Source == ContentSource.SteamWorkshop;
			case Category.Local: return m.Source == ContentSource.ModsFolder;
			case Category.Issues: return issues.Contains(m);
			case Category.Older: return Older(m);
			default: return true;
		}
	}

	private static bool PassesFilters(ModMetaData m)
	{
		if (compatibleOnly && Bad(m))
		{
			return false;
		}
		if (conflictsOnly && !conflicts.Contains(m))
		{
			return false;
		}
		if (missingDepsOnly && !missingDeps.Contains(m))
		{
			return false;
		}
		if (duplicatesOnly && !duplicates.Contains(m))
		{
			return false;
		}
		if (tagFilter.Count > 0 && !ModStore.TagsOf(m).Any(t => tagFilter.Contains(t)))
		{
			return false;
		}
		List<Version> vs = m.SupportedVersionsReadOnly;
		switch (verMode)
		{
			case VerMode.Supports: return vs != null && vs.Any(v => VKey(v) == verKey);
			case VerMode.Excludes: return vs == null || !vs.Any(v => VKey(v) == verKey);
			case VerMode.Below: return vs != null && vs.Count > 0 && vs.Max(VKey) < verKey;
			case VerMode.AtLeast: return vs != null && vs.Any(v => VKey(v) >= verKey);
			default: return true;
		}
	}

	private static string ModeLabel(VerMode m)
	{
		switch (m)
		{
			case VerMode.Supports: return "Supports";
			case VerMode.Excludes: return "Excludes";
			case VerMode.Below: return "Only below";
			case VerMode.AtLeast: return "At least";
			default: return "Any version";
		}
	}

	private static List<ModMetaData> BuildList()
	{
		IEnumerable<ModMetaData> list = all.Where(m => InCategory(m, category) && PassesFilters(m));
		if (!search.NullOrEmpty())
		{
			list = list.Where(m => m.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
				|| (m.AuthorsString ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
				|| m.PackageId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
				|| ModStore.TagsOf(m).Any(t => t.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
		}
		switch (sortBy)
		{
			case SortBy.Status: list = list.OrderByDescending(m => m.Active).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			case SortBy.Source: list = list.OrderBy(m => m.Source).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			case SortBy.Version: list = list.OrderBy(m => TopVersion(m)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			case SortBy.Author: list = list.OrderBy(m => m.AuthorsString ?? "", StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			case SortBy.Size: list = list.OrderBy(m => ModStore.SizeOf(m)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
			default: list = list.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase); break;
		}
		List<ModMetaData> mods = list.ToList();
		if (sortDesc)
		{
			mods.Reverse();
		}
		return mods;
	}

	private static string GroupKey(ModMetaData m)
	{
		switch (groupBy)
		{
			case GroupBy.Author: return m.Official ? "Ludeon Studios" : string.IsNullOrEmpty(m.AuthorsString) ? "Unknown author" : m.AuthorsString;
			case GroupBy.Version: return TopVersion(m) < 0 ? "No version info" : "Up to " + VLabel(TopVersion(m));
			case GroupBy.Source: return m.Official ? "Official" : m.Source == ContentSource.SteamWorkshop ? "Workshop" : "Local";
			case GroupBy.Tag: return ModStore.TagsOf(m).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? "(untagged)";
			default: return null;
		}
	}

	// ---- enabling with checks, dependencies and dependents -------------------------------------

	private static List<string> Warnings(ModMetaData mod)
	{
		List<string> w = new List<string>();
		foreach (string dep in mod.UnsatisfiedDependencies())
		{
			w.Add("Missing requirement: " + dep);
		}
		HashSet<string> active = new HashSet<string>(all.Where(m => m.Active).Select(m => m.PackageIdNonUnique), StringComparer.OrdinalIgnoreCase);
		if (mod.IncompatibleWith != null)
		{
			foreach (string id in mod.IncompatibleWith.Where(id => active.Contains(id)))
			{
				w.Add("Incompatible with enabled: " + (FindInstalled(id)?.Name ?? id));
			}
		}
		foreach (ModMetaData other in all.Where(m => m.Active && m.IncompatibleWith != null && m.IncompatibleWith.Any(id => string.Equals(id, mod.PackageIdNonUnique, StringComparison.OrdinalIgnoreCase))))
		{
			w.Add("Enabled mod lists it as incompatible: " + other.Name);
		}
		if (Bad(mod))
		{
			w.Add("Not made for this game version");
		}
		return w;
	}

	private static void EnableWithDeps(Page_ModsConfig page, ModMetaData mod, HashSet<ModMetaData> seen)
	{
		if (mod.Active || !seen.Add(mod))
		{
			return;
		}
		foreach (ModDependency dep in mod.Dependencies ?? new List<ModDependency>())
		{
			ModMetaData d = FindInstalled(dep.packageId);
			if (d == null)
			{
				Messages.Message("Not installed: " + (dep.displayName.NullOrEmpty() ? dep.packageId : dep.displayName), MessageTypeDefOf.RejectInput, false);
				continue;
			}
			EnableWithDeps(page, d, seen);
		}
		TryActivate(page, mod);
	}

	private static List<ModMetaData> ActiveDependents(ModMetaData mod)
	{
		List<ModMetaData> result = new List<ModMetaData>();
		Queue<ModMetaData> queue = new Queue<ModMetaData>();
		queue.Enqueue(mod);
		while (queue.Count > 0)
		{
			ModMetaData cur = queue.Dequeue();
			foreach (ModMetaData m in all.Where(x => x.Active && x != mod && !result.Contains(x) && x.Dependencies != null
				&& x.Dependencies.Any(d => string.Equals(d.packageId, cur.PackageIdNonUnique, StringComparison.OrdinalIgnoreCase))).ToList())
			{
				result.Add(m);
				queue.Enqueue(m);
			}
		}
		return result;
	}

	private static void DisableWithDependents(Page_ModsConfig page, ModMetaData mod)
	{
		List<ModMetaData> deps = ActiveDependents(mod);
		deps.Reverse();
		foreach (ModMetaData m in deps)
		{
			TryDeactivate(page, m);
		}
		TryDeactivate(page, mod);
		Refresh(true);
	}

	private static void RequestToggle(Page_ModsConfig page, ModMetaData mod)
	{
		if (mod.IsCoreMod)
		{
			return;
		}
		if (mod.Active)
		{
			List<ModMetaData> dependents = ActiveDependents(mod);
			if (dependents.Count == 0)
			{
				TryDeactivate(page, mod);
			}
			else
			{
				string names = string.Join("\n", dependents.Take(10).Select(m => "- " + m.Name)) + (dependents.Count > 10 ? $"\n... +{dependents.Count - 10} more" : "");
				Find.WindowStack.Add(new Dialog_MessageBox($"{dependents.Count} enabled mods need {mod.Name}:\n\n{names}",
					buttonAText: "Disable all of them", buttonAAction: () => DisableWithDependents(page, mod),
					buttonBText: "Disable only this", buttonBAction: () => { TryDeactivate(page, mod); Refresh(true); },
					title: "Required by other mods"));
				return;
			}
		}
		else
		{
			List<string> warnings = Warnings(mod);
			if (warnings.Count == 0)
			{
				TryActivate(page, mod);
			}
			else
			{
				bool hasDeps = mod.Dependencies != null && mod.Dependencies.Count > 0;
				Find.WindowStack.Add(new Dialog_MessageBox($"{mod.Name}\n\n" + string.Join("\n", warnings.Select(x => "- " + x)),
					buttonAText: "Enable anyway", buttonAAction: () => { TryActivate(page, mod); Refresh(true); },
					buttonBText: hasDeps ? "Enable with requirements" : "Cancel", buttonBAction: hasDeps ? (Action)(() => { EnableWithDeps(page, mod, new HashSet<ModMetaData>()); Refresh(true); }) : null,
					title: "Check before enabling"));
				return;
			}
		}
		Refresh(true);
	}

	// ---- presets -------------------------------------------------------------------------------

	private static List<string> CurrentActiveIds() => ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageIdNonUnique).ToList();

	private static string NameOf(string id) => FindInstalled(id)?.Name ?? id;

	private static void OpenPresetMenu(Page_ModsConfig page)
	{
		List<FloatMenuOption> opts = new List<FloatMenuOption>
		{
			new FloatMenuOption("Save current as...", () => Find.WindowStack.Add(new Dialog_LatticeName("Save preset", "", name => ModStore.SavePreset(name, CurrentActiveIds()))))
		};
		foreach (string name in ModStore.PresetNames.ToList())
		{
			string n = name;
			opts.Add(new FloatMenuOption($"{n} ({ModStore.Presets[n].Count})", () => OpenPresetActions(page, n)));
		}
		Find.WindowStack.Add(new FloatMenu(opts));
	}

	private static void OpenPresetActions(Page_ModsConfig page, string name)
	{
		List<FloatMenuOption> opts = new List<FloatMenuOption>
		{
			new FloatMenuOption("Load", () => ConfirmLoadPreset(page, name)),
			new FloatMenuOption("Compare with current", () => ShowDiff(name)),
			new FloatMenuOption("Overwrite with current", () => ModStore.SavePreset(name, CurrentActiveIds())),
			new FloatMenuOption("Delete", () => ModStore.DeletePreset(name)),
		};
		Find.WindowStack.Add(new FloatMenu(opts));
	}

	private static string DiffText(string name, out int added, out int removed, out int missing)
	{
		List<string> target = ModStore.Presets[name];
		HashSet<string> cur = new HashSet<string>(CurrentActiveIds(), StringComparer.OrdinalIgnoreCase);
		HashSet<string> tgt = new HashSet<string>(target, StringComparer.OrdinalIgnoreCase);
		List<string> add = target.Where(id => !cur.Contains(id)).ToList();
		List<string> rem = cur.Where(id => !tgt.Contains(id)).ToList();
		missing = add.Count(id => FindInstalled(id) == null);
		added = add.Count;
		removed = rem.Count;
		string Block(string title, List<string> ids) => ids.Count == 0 ? "" : $"{title} ({ids.Count}):\n" + string.Join("\n", ids.Take(12).Select(id => "  " + NameOf(id) + (FindInstalled(id) == null ? " (not installed)" : ""))) + (ids.Count > 12 ? $"\n  ... +{ids.Count - 12} more" : "") + "\n\n";
		string text = Block("+ Would be enabled", add) + Block("- Would be disabled", rem);
		return text.Length == 0 ? "Identical to the current selection." : text.TrimEnd();
	}

	private static void ShowDiff(string name)
	{
		string text = DiffText(name, out _, out _, out _);
		Find.WindowStack.Add(new Dialog_MessageBox(text, title: "Preset: " + name));
	}

	private static void ConfirmLoadPreset(Page_ModsConfig page, string name)
	{
		string text = DiffText(name, out int added, out int removed, out int missing);
		if (added == 0 && removed == 0)
		{
			Messages.Message("Preset already matches the current selection.", MessageTypeDefOf.NeutralEvent, false);
			return;
		}
		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"Load preset \"{name}\"?\n\n{text}" + (missing > 0 ? $"\n\n{missing} mods are not installed and will be skipped." : ""), () =>
		{
			List<string> ids = ModStore.Presets[name].Where(id => FindInstalled(id) != null).ToList();
			ModsConfig.SetActiveToList(ids);
			ModsConfig.TrySortMods();
			Dirty(page) = true;
			Refresh(true);
		}));
	}

	// ---- sizing helpers ------------------------------------------------------------------------

	private static float CardW => 232f * cardScale;
	private const float RowH = 30f;

	// ---- chips / flow header -------------------------------------------------------------------

	private static bool FilterChip(Rect r, string label, bool on, bool enabled = true)
	{
		bool hover = enabled && Mouse.IsOver(r);
		Widgets.DrawBoxSolidWithOutline(r, on ? UiStyle.WithAlpha(UiStyle.Accent, 0.22f) : new Color(1f, 1f, 1f, hover ? 0.1f : 0.04f), on ? UiStyle.Accent : UiStyle.Hairline, 1);
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = !enabled ? UiStyle.TextDim : on ? UiStyle.Accent : Color.Lerp(UiStyle.TextSoft, Color.white, hover ? 1f : 0f);
		Widgets.Label(r, label);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		return enabled && Widgets.ButtonInvisible(r);
	}

	private static float flowX0, flowX, flowY, flowMax;

	private static void FlowStart(Rect rect, float y)
	{
		flowX0 = flowX = rect.x + 10f;
		flowMax = rect.xMax - 10f;
		flowY = y;
	}

	private static void FlowBreak()
	{
		if (flowX > flowX0)
		{
			flowX = flowX0;
			flowY += 32f;
		}
	}

	private static bool Chip(string label, bool on = false, bool enabled = true)
	{
		float w = Text.CalcSize(label).x + 18f;
		if (flowX + w > flowMax && flowX > flowX0)
		{
			flowX = flowX0;
			flowY += 32f;
		}
		bool hit = FilterChip(new Rect(flowX, flowY, w, 26f), label, on, enabled);
		flowX += w + 6f;
		return hit;
	}

	private static void FlowLabel(string text)
	{
		float w = Text.CalcSize(text).x + 8f;
		if (flowX + w > flowMax && flowX > flowX0)
		{
			flowX = flowX0;
			flowY += 32f;
		}
		GUI.color = UiStyle.TextSoft;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(flowX, flowY, w, 26f), text);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		flowX += w + 4f;
	}

	private static void ResetFilters()
	{
		verMode = VerMode.Any;
		verKey = VKey(VersionControl.CurrentVersion);
		compatibleOnly = conflictsOnly = missingDepsOnly = duplicatesOnly = false;
		tagFilter.Clear();
		scroll = Vector2.zero;
	}

	private static void NewTagFor(List<ModMetaData> targets) =>
		Find.WindowStack.Add(new Dialog_LatticeName("New tag", "", tag => targets.ForEach(m => ModStore.AddTag(m, tag))));

	private static void TagMenu(List<ModMetaData> targets, bool remove)
	{
		List<FloatMenuOption> opts = new List<FloatMenuOption>();
		if (!remove)
		{
			opts.Add(new FloatMenuOption("New tag...", () => NewTagFor(targets)));
			foreach (string t in ModStore.AllTags())
			{
				string tag = t;
				opts.Add(new FloatMenuOption(tag, () => targets.ForEach(m => ModStore.AddTag(m, tag))));
			}
		}
		else
		{
			foreach (string t in targets.SelectMany(m => ModStore.TagsOf(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList())
			{
				string tag = t;
				opts.Add(new FloatMenuOption("Remove: " + tag, () => targets.ForEach(m => ModStore.RemoveTag(m, tag))));
			}
		}
		if (opts.Count > 0)
		{
			Find.WindowStack.Add(new FloatMenu(opts));
		}
	}

	// Header: everything under the search field. Returns the y where the grid may start.
	private static float DrawHeader(Page_ModsConfig page, Rect rect, List<ModMetaData> mods)
	{
		FlowStart(rect, rect.y + 42f);

		// Version filter
		if (Chip(ModeLabel(verMode) + " ▾", verMode != VerMode.Any))
		{
			List<FloatMenuOption> opts = new List<FloatMenuOption>();
			foreach (VerMode vm in Enum.GetValues(typeof(VerMode)))
			{
				VerMode pick = vm;
				opts.Add(new FloatMenuOption(ModeLabel(vm), () => { verMode = pick; scroll = Vector2.zero; }));
			}
			Find.WindowStack.Add(new FloatMenu(opts));
		}
		if (Chip("v" + VLabel(verKey) + " ▾", verMode != VerMode.Any, verMode != VerMode.Any))
		{
			List<int> keys = new List<int>(knownVersions);
			int cur = VKey(VersionControl.CurrentVersion);
			if (!keys.Contains(cur))
			{
				keys.Insert(0, cur);
			}
			Find.WindowStack.Add(new FloatMenu(keys.Select(k => new FloatMenuOption("v" + VLabel(k) + (k == cur ? " (current)" : ""), () => { verKey = k; scroll = Vector2.zero; })).ToList()));
		}
		if (Chip("Compatible only", compatibleOnly))
		{
			compatibleOnly = !compatibleOnly;
			scroll = Vector2.zero;
		}
		if (Chip($"Conflicts ({conflicts.Count})", conflictsOnly))
		{
			conflictsOnly = !conflictsOnly;
			scroll = Vector2.zero;
		}
		if (Chip($"Missing deps ({missingDeps.Count})", missingDepsOnly))
		{
			missingDepsOnly = !missingDepsOnly;
			scroll = Vector2.zero;
		}
		if (Chip($"Duplicates ({duplicates.Count})", duplicatesOnly))
		{
			duplicatesOnly = !duplicatesOnly;
			scroll = Vector2.zero;
		}
		if (Filtering && Chip("Reset"))
		{
			ResetFilters();
		}

		// Tags
		List<string> tags = ModStore.AllTags().ToList();
		if (tags.Count > 0)
		{
			FlowBreak();
			FlowLabel("Tags:");
			foreach (string t in tags)
			{
				if (Chip(t, tagFilter.Contains(t)))
				{
					if (!tagFilter.Remove(t))
					{
						tagFilter.Add(t);
					}
					scroll = Vector2.zero;
				}
			}
		}

		// View, grouping, presets
		FlowBreak();
		if (Chip("Presets ▾"))
		{
			OpenPresetMenu(page);
		}
		if (Chip("Group: " + groupBy + " ▾", groupBy != GroupBy.None))
		{
			List<FloatMenuOption> opts = new List<FloatMenuOption>();
			foreach (GroupBy g in Enum.GetValues(typeof(GroupBy)))
			{
				GroupBy pick = g;
				opts.Add(new FloatMenuOption(g.ToString(), () => { groupBy = pick; scroll = Vector2.zero; }));
			}
			Find.WindowStack.Add(new FloatMenu(opts));
		}
		if (Chip(compact ? "View: Compact" : "View: Cards"))
		{
			compact = !compact;
			scroll = Vector2.zero;
		}
		if (!compact)
		{
			float sw = 90f;
			if (flowX + sw > flowMax && flowX > flowX0)
			{
				flowX = flowX0;
				flowY += 32f;
			}
			cardScale = Widgets.HorizontalSlider(new Rect(flowX, flowY + 6f, sw, 16f), cardScale, 0.7f, 1.3f);
			flowX += sw + 10f;
		}

		// Bulk actions: on the selection when there is one, otherwise on everything shown.
		FlowBreak();
		List<ModMetaData> targets = selection.Count > 0 ? mods.Where(m => selection.Contains(m)).ToList() : mods;
		string who = selection.Count > 0 ? $"selected ({targets.Count})" : $"shown ({targets.Count})";
		List<ModMetaData> toDisable = targets.Where(m => m.Active && !m.IsCoreMod).ToList();
		List<ModMetaData> toEnable = targets.Where(m => !m.Active).ToList();
		List<ModMetaData> toUnsub = targets.Where(m => m.Source == ContentSource.SteamWorkshop && !m.Active).ToList();
		if (Chip($"Disable {who}", false, toDisable.Count > 0))
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"Disable {toDisable.Count} mods?", () =>
			{
				toDisable.ForEach(m => TryDeactivate(page, m));
				Refresh(true);
			}));
		}
		if (Chip($"Enable {who}", false, toEnable.Count > 0))
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"Enable {toEnable.Count} mods?", () =>
			{
				HashSet<ModMetaData> seen = new HashSet<ModMetaData>();
				toEnable.ForEach(m => EnableWithDeps(page, m, seen));
				Refresh(true);
			}));
		}
		if (Unsubscribe != null && Chip($"Unsubscribe {who}", false, toUnsub.Count > 0))
		{
			int skipped = targets.Count(m => m.Source == ContentSource.SteamWorkshop && m.Active);
			string names = string.Join("\n", toUnsub.Take(12).Select(m => "- " + m.Name)) + (toUnsub.Count > 12 ? $"\n... +{toUnsub.Count - 12} more" : "");
			string text = $"Unsubscribe from {toUnsub.Count} Workshop mods?\n\n{names}"
				+ (skipped > 0 ? $"\n\n{skipped} enabled Workshop mods in this list are skipped - disable them first." : "");
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () =>
			{
				Unsubscribe(page, toUnsub);
				Refresh(true);
			}, true));
		}
		if (selection.Count > 0)
		{
			if (Chip("Favourite"))
			{
				bool on = targets.Any(m => !ModStore.IsFavourite(m));
				targets.ForEach(m => ModStore.SetFavourite(m, on));
			}
			if (Chip("Tag ▾"))
			{
				TagMenu(targets, false);
			}
			if (targets.Any(m => ModStore.TagsOf(m).Any()) && Chip("Untag ▾"))
			{
				TagMenu(targets, true);
			}
			if (Chip("Clear selection"))
			{
				selection.Clear();
			}
		}

		// Game-version change report.
		string curLabel = VLabel(VKey(VersionControl.CurrentVersion));
		if (!ModStore.LastVersion.NullOrEmpty() && ModStore.LastVersion != curLabel)
		{
			FlowBreak();
			int bad = all.Count(m => m.Active && Bad(m));
			int older = all.Count(m => m.Active && Older(m));
			FlowLabel($"Game updated {ModStore.LastVersion} → {curLabel}: {bad} enabled mods are not marked compatible" + (older > 0 ? $", {older} more are made for an older version." : "."));
			if (Chip("Show them"))
			{
				category = Category.Issues;
				scroll = Vector2.zero;
			}
			if (Chip("Dismiss"))
			{
				ModStore.LastVersion = curLabel;
				ModStore.Save();
			}
		}
		FlowBreak();
		return flowY + 2f;
	}

	// ---- drawing -------------------------------------------------------------------------------

	public static void Draw(Page_ModsConfig page, Rect rect)
	{
		LoadState();
		Refresh(Dirty(page));
		float bottomH = 44f;
		// Panels follow the window: narrower sidebar and detail pane on small windows, wider on large ones.
		float sideW = Mathf.Clamp(rect.width * 0.14f, 150f, 220f);
		Rect side = new Rect(0f, 0f, sideW, rect.height - bottomH - 8f);
		ModMetaData selected = Primary(page);
		float detail = selected != null ? Mathf.Clamp(rect.width * 0.26f, 300f, 420f) : 0f;
		Rect main = new Rect(sideW + 14f, 0f, rect.width - sideW - 14f - detail - (detail > 0f ? 12f : 0f), rect.height - bottomH - 8f);
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

		float y = 58f;
		foreach (Category c in Enum.GetValues(typeof(Category)))
		{
			int count = all.Count(m => InCategory(m, c));
			Rect row = new Rect(6f, y, rect.width - 12f, 28f);
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
			y += 30f;
		}

		y += 10f;
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(14f, y, rect.width - 20f, 22f), "SORT BY");
		GUI.color = Color.white;
		y += 22f;
		foreach (SortBy s in Enum.GetValues(typeof(SortBy)))
		{
			Rect row = new Rect(6f, y, rect.width - 12f, 24f);
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
			y += 25f;
		}
		if (FilterChip(new Rect(6f, y + 4f, rect.width - 12f, 26f), sortDesc ? "Descending" : "Ascending", sortDesc))
		{
			sortDesc = !sortDesc;
		}
	}

	private struct Block
	{
		public string header;
		public List<ModMetaData> mods;
		public float y;
	}

	private static void DrawMain(Page_ModsConfig page, Rect rect)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.45f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.45f));

		Rect searchRect = new Rect(rect.x + 10f, rect.y + 8f, Mathf.Clamp(rect.width * 0.42f, 140f, 320f), 28f);
		search = Widgets.TextField(searchRect, search);
		if (search.NullOrEmpty())
		{
			GUI.color = UiStyle.TextDim;
			Widgets.Label(new Rect(searchRect.x + 6f, searchRect.y + 3f, searchRect.width, 24f), "Search mods, authors, tags...");
			GUI.color = Color.white;
		}
		int active = all.Count(m => m.Active);
		long activeSize = all.Where(m => m.Active).Sum(m => Math.Max(0L, ModStore.SizeOf(m)));
		bool partial = all.Any(m => m.Active && ModStore.SizeOf(m) < 0);
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = UiStyle.TextSoft;
		Widgets.Label(new Rect(searchRect.xMax + 6f, rect.y + 8f, rect.xMax - searchRect.xMax - 16f, 28f), $"{active} enabled / {all.Count} installed · {(partial ? "~" : "")}{ModStore.FormatSize(activeSize)}");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;

		List<ModMetaData> mods = BuildList();
		float top = DrawHeader(page, rect, mods);

		// Blocks (one per group, or a single headerless block).
		List<Block> blocks = new List<Block>();
		if (groupBy == GroupBy.None)
		{
			blocks.Add(new Block { mods = mods });
		}
		else
		{
			IEnumerable<IGrouping<string, ModMetaData>> groups = mods.GroupBy(GroupKey);
			groups = groupBy == GroupBy.Version ? groups.OrderByDescending(g => TopVersion(g.First())) : groups.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
			foreach (IGrouping<string, ModMetaData> g in groups)
			{
				blocks.Add(new Block { header = $"{g.Key}  ({g.Count()})", mods = g.ToList() });
			}
		}

		Rect outRect = new Rect(rect.x + 6f, top, rect.width - 12f, rect.yMax - top - 6f);
		float viewW = outRect.width - 16f;
		int cols = compact ? 1 : Mathf.Max(1, Mathf.FloorToInt(viewW / CardW));
		float cellW = viewW / cols;
		// Cards keep their proportions when the cell stretches to fill the row.
		float cellH = compact ? RowH : cellW * (178f / 232f);
		float total = 0f;
		for (int b = 0; b < blocks.Count; b++)
		{
			Block bl = blocks[b];
			bl.y = total;
			blocks[b] = bl;
			total += (bl.header != null ? 30f : 0f) + Mathf.CeilToInt(bl.mods.Count / (float)cols) * cellH + (bl.header != null ? 6f : 0f);
		}
		displayed = blocks.SelectMany(b => b.mods).ToList();

		Rect view = new Rect(0f, 0f, viewW, total);
		Widgets.BeginScrollView(outRect, ref scroll, view);
		foreach (Block bl in blocks)
		{
			float y = bl.y;
			if (bl.header != null)
			{
				GUI.color = UiStyle.Accent;
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(new Rect(6f, y, viewW - 12f, 28f), bl.header);
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
				Widgets.DrawBoxSolid(new Rect(6f, y + 28f, viewW - 12f, 1f), UiStyle.Hairline);
				y += 30f;
			}
			for (int i = 0; i < bl.mods.Count; i++)
			{
				Rect cell = new Rect(i % cols * cellW + 4f, y + i / cols * cellH + (compact ? 1f : 4f), cellW - 8f, cellH - (compact ? 2f : 8f));
				if (cell.yMax < scroll.y || cell.y > scroll.y + outRect.height)
				{
					continue;
				}
				if (compact)
				{
					DrawRow(page, cell, bl.mods[i]);
				}
				else
				{
					DrawCard(page, cell, bl.mods[i]);
				}
			}
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

	private static readonly Color SelectColor = new Color(0.4f, 0.8f, 1f);

	private static List<ModMetaData> TargetsFor(ModMetaData mod) => selection.Contains(mod) ? selection.ToList() : new List<ModMetaData> { mod };

	private static void ContextMenu(Page_ModsConfig page, ModMetaData mod)
	{
		List<ModMetaData> targets = TargetsFor(mod);
		List<FloatMenuOption> opts = new List<FloatMenuOption>();
		if (!mod.IsCoreMod)
		{
			opts.Add(new FloatMenuOption(mod.Active ? "Disable" : "Enable", () => RequestToggle(page, mod)));
			if (!mod.Active && mod.Dependencies != null && mod.Dependencies.Count > 0)
			{
				opts.Add(new FloatMenuOption("Enable with requirements", () => { EnableWithDeps(page, mod, new HashSet<ModMetaData>()); Refresh(true); }));
			}
			if (mod.Active && ActiveDependents(mod).Count > 0)
			{
				opts.Add(new FloatMenuOption("Disable with dependents", () => DisableWithDependents(page, mod)));
			}
		}
		bool fav = ModStore.IsFavourite(mod);
		opts.Add(new FloatMenuOption(fav ? "Remove from favourites" : "Add to favourites", () => targets.ForEach(m => ModStore.SetFavourite(m, !fav))));
		opts.Add(new FloatMenuOption("Add tag...", () => TagMenu(targets, false)));
		if (targets.Any(m => ModStore.TagsOf(m).Any()))
		{
			opts.Add(new FloatMenuOption("Remove tag...", () => TagMenu(targets, true)));
		}
		opts.Add(new FloatMenuOption(selection.Contains(mod) ? "Deselect" : "Select", () =>
		{
			if (!selection.Remove(mod))
			{
				selection.Add(mod);
			}
		}));
		Find.WindowStack.Add(new FloatMenu(opts));
	}

	// Click on a card/row: Ctrl toggles selection, Shift selects a range, plain click opens the detail pane.
	private static void HandleClick(Page_ModsConfig page, Rect rect, ModMetaData mod)
	{
		Event e = Event.current;
		if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
		{
			ContextMenu(page, mod);
			e.Use();
			return;
		}
		bool ctrl = e.control || e.command, shift = e.shift;
		if (!Widgets.ButtonInvisible(rect))
		{
			return;
		}
		if (ctrl)
		{
			if (!selection.Remove(mod))
			{
				selection.Add(mod);
			}
			lastClicked = mod;
		}
		else if (shift && lastClicked != null)
		{
			int a = displayed.IndexOf(lastClicked), b = displayed.IndexOf(mod);
			if (a >= 0 && b >= 0)
			{
				for (int i = Math.Min(a, b); i <= Math.Max(a, b); i++)
				{
					selection.Add(displayed[i]);
				}
			}
		}
		else
		{
			selection.Clear();
			lastClicked = mod;
			SelectMod(page, mod);
		}
	}

	private static string Tooltip(ModMetaData mod)
	{
		string s = $"{mod.Name}\n{mod.PackageIdPlayerFacing}";
		if (mod.SupportedVersionsReadOnly != null && mod.SupportedVersionsReadOnly.Count > 0)
		{
			s += "\nVersions: " + string.Join(", ", mod.SupportedVersionsReadOnly.Select(v => VLabel(VKey(v))));
		}
		long size = ModStore.SizeOf(mod);
		if (size >= 0)
		{
			s += "\nSize: " + ModStore.FormatSize(size);
		}
		string tags = string.Join(", ", ModStore.TagsOf(mod));
		if (tags.Length > 0)
		{
			s += "\nTags: " + tags;
		}
		if (Older(mod))
		{
			s += "\n" + $"Made for {VLabel(TopVersion(mod))} - an older version that this game usually still loads".Colorize(OlderColor);
		}
		else if (!mod.VersionCompatible)
		{
			s += "\n" + "Not made for this game version".Colorize(ColorLibrary.RedReadable);
		}
		if (conflicts.Contains(mod))
		{
			s += "\n" + "Conflicts with another enabled mod".Colorize(ColorLibrary.RedReadable);
		}
		if (duplicates.Contains(mod))
		{
			s += "\n" + $"Duplicate: also installed from another source ({mod.Source})".Colorize(new Color(1f, 0.75f, 0.2f));
		}
		s += "\n\nCtrl+click: select - Shift+click: range - Right-click: menu";
		return s;
	}

	private static void DrawStar(Rect box, ModMetaData mod)
	{
		bool fav = ModStore.IsFavourite(mod);
		bool hover = Mouse.IsOver(box);
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = fav ? UiStyle.Accent : hover ? Color.white : UiStyle.TextDim;
		Widgets.Label(box, fav ? "★" : "☆");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		if (Widgets.ButtonInvisible(box))
		{
			ModStore.SetFavourite(mod, !fav);
		}
	}

	private static void DrawCard(Page_ModsConfig page, Rect card, ModMetaData mod)
	{
		bool hover = Mouse.IsOver(card);
		bool selected = Primary(page) == mod;
		bool picked = selection.Contains(mod);
		float h = UiStyle.Ease(UiStyle.Key(7301, card), hover ? 1f : 0f, 10f);
		bool bad = Bad(mod);
		bool older = Older(mod);
		bool warn = issues.Contains(mod);
		Color edge = bad ? ColorLibrary.RedReadable : warn ? new Color(1f, 0.75f, 0.2f) : older ? OlderColor : mod.Active ? UiStyle.Accent : UiStyle.Hairline;

		UiStyle.DrawGradient(card, UiStyle.WithAlpha(UiStyle.PanelTop, 0.75f + 0.25f * h), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.85f));
		if (selected)
		{
			Widgets.DrawBoxSolidWithOutline(card, Color.clear, UiStyle.Accent, 2);
		}
		if (picked)
		{
			Widgets.DrawBoxSolidWithOutline(card, UiStyle.WithAlpha(SelectColor, 0.1f), SelectColor, 2);
		}
		Widgets.DrawBoxSolid(new Rect(card.x, card.y, card.width, 3f), UiStyle.WithAlpha(edge, mod.Active || bad || warn || older ? 1f : 0.5f));

		Rect preview = new Rect(card.x + 4f, card.y + 7f, card.width - 8f, card.height * 0.585f);
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
		Rect authorRect = new Rect(card.x + 8f, card.yMax - 20f, card.width - 16f - 56f, 18f);
		Widgets.Label(authorRect, (mod.AuthorsString ?? "").Truncate(authorRect.width));
		Text.Anchor = TextAnchor.UpperRight;
		Widgets.Label(new Rect(card.xMax - 64f, card.yMax - 20f, 56f, 18f), ModStore.FormatSize(ModStore.SizeOf(mod)));
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		Text.Font = GameFont.Small;

		float badgeY = preview.y + 28f;
		if (duplicates.Contains(mod))
		{
			Text.Font = GameFont.Tiny;
			GUI.color = new Color(1f, 0.75f, 0.2f);
			Widgets.Label(new Rect(preview.x + 6f, badgeY, 60f, 18f), "dup");
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			badgeY += 16f;
		}
		if (older)
		{
			Text.Font = GameFont.Tiny;
			Rect badge = new Rect(preview.x + 6f, badgeY, 54f, 16f);
			Widgets.DrawBoxSolid(badge, new Color(0f, 0f, 0f, 0.6f));
			GUI.color = OlderColor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(badge, "for " + VLabel(TopVersion(mod)));
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
		}

		// Star and toggle first so they win the click over the card.
		DrawStar(new Rect(preview.x + 4f, preview.y + 4f, 26f, 26f), mod);
		Rect box = new Rect(preview.xMax - 30f, preview.y + 6f, 24f, 24f);
		Widgets.DrawBoxSolid(box, new Color(0f, 0f, 0f, 0.65f));
		Widgets.DrawBoxSolidWithOutline(box, Color.clear, mod.Active ? UiStyle.Accent : UiStyle.TextDim, 1);
		if (mod.Active)
		{
			Widgets.DrawBoxSolid(box.ContractedBy(6f), UiStyle.Accent);
		}
		if (!mod.IsCoreMod && Widgets.ButtonInvisible(box))
		{
			RequestToggle(page, mod);
		}
		HandleClick(page, card, mod);
		if (hover)
		{
			TooltipHandler.TipRegion(card, Tooltip(mod));
		}
	}

	private static void DrawRow(Page_ModsConfig page, Rect row, ModMetaData mod)
	{
		bool hover = Mouse.IsOver(row);
		bool selected = Primary(page) == mod;
		bool picked = selection.Contains(mod);
		bool bad = Bad(mod);
		bool older = Older(mod);
		bool warn = issues.Contains(mod);
		Color edge = bad ? ColorLibrary.RedReadable : warn ? new Color(1f, 0.75f, 0.2f) : older ? OlderColor : mod.Active ? UiStyle.Accent : UiStyle.Hairline;
		Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, hover ? 0.1f : 0.04f));
		if (picked)
		{
			Widgets.DrawBoxSolidWithOutline(row, UiStyle.WithAlpha(SelectColor, 0.1f), SelectColor, 1);
		}
		if (selected)
		{
			Widgets.DrawBoxSolidWithOutline(row, Color.clear, UiStyle.Accent, 1);
		}
		Widgets.DrawBoxSolid(new Rect(row.x, row.y, 3f, row.height), edge);

		Rect box = new Rect(row.x + 10f, row.y + 5f, 20f, 20f);
		Widgets.DrawBoxSolid(box, new Color(0f, 0f, 0f, 0.5f));
		Widgets.DrawBoxSolidWithOutline(box, Color.clear, mod.Active ? UiStyle.Accent : UiStyle.TextDim, 1);
		if (mod.Active)
		{
			Widgets.DrawBoxSolid(box.ContractedBy(5f), UiStyle.Accent);
		}
		bool toggled = !mod.IsCoreMod && Widgets.ButtonInvisible(box);
		DrawStar(new Rect(box.xMax + 4f, row.y + 2f, 26f, 26f), mod);

		float right = row.xMax - 8f;
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = UiStyle.TextDim;
		Widgets.Label(new Rect(right - 70f, row.y, 70f, row.height), ModStore.FormatSize(ModStore.SizeOf(mod)));
		right -= 76f;
		int top = TopVersion(mod);
		GUI.color = older ? OlderColor : bad ? ColorLibrary.RedReadable : UiStyle.TextDim;
		Widgets.Label(new Rect(right - 50f, row.y, 50f, row.height), top < 0 ? "" : VLabel(top));
		GUI.color = UiStyle.TextDim;
		right -= 56f;
		float authorW = Mathf.Min(170f, row.width * 0.25f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(new Rect(right - authorW, row.y, authorW, row.height), (mod.AuthorsString ?? "").Truncate(authorW));
		right -= authorW + 6f;
		GUI.color = Color.white;
		float nameX = box.xMax + 34f;
		Widgets.Label(new Rect(nameX, row.y, Mathf.Max(40f, right - nameX), row.height), mod.Name.Truncate(Mathf.Max(40f, right - nameX)));
		Text.Anchor = TextAnchor.UpperLeft;

		if (toggled)
		{
			RequestToggle(page, mod);
		}
		HandleClick(page, row, mod);
		if (hover)
		{
			TooltipHandler.TipRegion(row, Tooltip(mod));
		}
	}

	private static void DrawBottom(Page_ModsConfig page, Rect rect)
	{
		UiStyle.DrawGradient(rect, UiStyle.WithAlpha(UiStyle.PanelTop, 0.8f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.9f));
		Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), UiStyle.Hairline);
		Rect left = new Rect(rect.x + 6f, rect.y + 6f, Mathf.Max(120f, rect.width - 460f), 32f);
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
	}
}

[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
public static class Patch_ModsPageContents
{
	public static bool Prefix(Page_ModsConfig __instance, Rect rect)
	{
		if (!LatticeMod.Settings.restyleWidgets || ModManager.ListMode || !ModManager.Ready)
		{
			return true;
		}
		ModManager.Draw(__instance, rect);
		return false;
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

// View switch in the top-right corner of the page. Clicked in the Prefix (before the page draws), so no
// vanilla control can swallow it; drawn in the Postfix, on top.
[HarmonyPatch(typeof(Window), "InnerWindowOnGUI")]
public static class Patch_ModsViewSwitch
{
	private static Rect Chip(Window w) => new Rect(w.windowRect.width - 198f, 8f, 180f, 28f);

	public static void Prefix(Window __instance)
	{
		if (!(__instance is Page_ModsConfig) || !LatticeMod.Settings.restyleWidgets || !ModManager.Ready)
		{
			return;
		}
		Event e = Event.current;
		if (e.type == EventType.MouseDown && e.button == 0 && Chip(__instance).Contains(e.mousePosition))
		{
			ModManager.ListMode = !ModManager.ListMode;
			e.Use();
		}
	}

	public static void Postfix(Window __instance)
	{
		if (!(__instance is Page_ModsConfig) || !LatticeMod.Settings.restyleWidgets || !ModManager.Ready || Event.current.type != EventType.Repaint)
		{
			return;
		}
		Rect chip = Chip(__instance);
		bool hover = Mouse.IsOver(chip);
		UiStyle.DrawGradient(chip, UiStyle.WithAlpha(UiStyle.PanelTop, 0.95f), UiStyle.WithAlpha(UiStyle.PanelBottom, 0.95f));
		Widgets.DrawBoxSolid(new Rect(chip.x, chip.yMax - 2f, chip.width * (hover ? 1f : 0.35f), 2f), UiStyle.Accent);
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(chip, ModManager.ListMode ? "Card view" : "Load order view");
		Text.Anchor = TextAnchor.UpperLeft;
	}
}

[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.PreOpen))]
public static class Patch_ModsPageOpen
{
	public static void Postfix() => ModManager.ListMode = false;
}

[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.PostClose))]
public static class Patch_ModsPageClose
{
	public static void Postfix() => ModManager.SaveState();
}
