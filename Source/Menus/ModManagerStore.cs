using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;

namespace Lattice;

// Everything the mod manager remembers between sessions: favourites, tags, presets, view state and the
// game version it last saw. One plain-text file (key=value lines, '|' between fields) next to the backdrops.
public static class ModStore
{
	public static readonly HashSet<string> Favourites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, HashSet<string>> Tags = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
	public static readonly List<string> PresetNames = new List<string>();
	public static readonly Dictionary<string, List<string>> Presets = new Dictionary<string, List<string>>();
	public static readonly Dictionary<string, string> Ui = new Dictionary<string, string>();
	public static string LastVersion = "";

	private static bool loaded;

	private static string FilePath => Path.Combine(GenFilePaths.ConfigFolderPath, "Lattice", "ModManager.txt");

	private static string Clean(string s) => (s ?? "").Replace("|", " ").Replace(",", " ").Replace("\n", " ").Replace("\r", " ").Trim();

	public static void Load()
	{
		if (loaded)
		{
			return;
		}
		loaded = true;
		try
		{
			if (!File.Exists(FilePath))
			{
				return;
			}
			foreach (string line in File.ReadAllLines(FilePath))
			{
				int eq = line.IndexOf('=');
				if (eq < 0)
				{
					continue;
				}
				string key = line.Substring(0, eq);
				string[] f = line.Substring(eq + 1).Split('|');
				switch (key)
				{
					case "fav" when f.Length >= 1:
						Favourites.Add(f[0]);
						break;
					case "tag" when f.Length >= 2:
						Tags[f[0]] = new HashSet<string>(f[1].Split(',').Where(t => t.Length > 0), StringComparer.OrdinalIgnoreCase);
						break;
					case "preset" when f.Length >= 2:
						PresetNames.Add(f[0]);
						Presets[f[0]] = f[1].Split(',').Where(t => t.Length > 0).ToList();
						break;
					case "ui" when f.Length >= 2:
						Ui[f[0]] = f[1];
						break;
					case "ver":
						LastVersion = f[0];
						break;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[Lattice] Could not read mod manager data: " + ex.Message);
		}
	}

	public static void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			List<string> lines = new List<string>();
			lines.Add("ver=" + LastVersion);
			lines.AddRange(Favourites.Select(id => "fav=" + id));
			lines.AddRange(Tags.Where(kv => kv.Value.Count > 0).Select(kv => "tag=" + kv.Key + "|" + string.Join(",", kv.Value)));
			lines.AddRange(PresetNames.Select(n => "preset=" + n + "|" + string.Join(",", Presets[n])));
			lines.AddRange(Ui.Select(kv => "ui=" + kv.Key + "|" + kv.Value));
			File.WriteAllLines(FilePath, lines);
		}
		catch (Exception ex)
		{
			Log.Warning("[Lattice] Could not save mod manager data: " + ex.Message);
		}
	}

	public static bool IsFavourite(ModMetaData m) => Favourites.Contains(m.PackageIdNonUnique);

	public static void SetFavourite(ModMetaData m, bool on)
	{
		if (on)
		{
			Favourites.Add(m.PackageIdNonUnique);
		}
		else
		{
			Favourites.Remove(m.PackageIdNonUnique);
		}
		Save();
	}

	public static IEnumerable<string> TagsOf(ModMetaData m) => Tags.TryGetValue(m.PackageIdNonUnique, out HashSet<string> t) ? t : Enumerable.Empty<string>();

	public static IEnumerable<string> AllTags() => Tags.Values.SelectMany(t => t).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

	public static void AddTag(ModMetaData m, string tag)
	{
		tag = Clean(tag);
		if (tag.Length == 0)
		{
			return;
		}
		if (!Tags.TryGetValue(m.PackageIdNonUnique, out HashSet<string> set))
		{
			Tags[m.PackageIdNonUnique] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		set.Add(tag);
		Save();
	}

	public static void RemoveTag(ModMetaData m, string tag)
	{
		if (Tags.TryGetValue(m.PackageIdNonUnique, out HashSet<string> set) && set.Remove(tag))
		{
			Save();
		}
	}

	public static void SavePreset(string name, List<string> ids)
	{
		name = Clean(name);
		if (name.Length == 0)
		{
			return;
		}
		if (!Presets.ContainsKey(name))
		{
			PresetNames.Add(name);
		}
		Presets[name] = ids;
		Save();
	}

	public static void DeletePreset(string name)
	{
		PresetNames.Remove(name);
		Presets.Remove(name);
		Save();
	}

	// Folder sizes, measured once on a background thread so the grid never stalls on disk.
	private static readonly ConcurrentDictionary<string, long> sizes = new ConcurrentDictionary<string, long>();
	private static int queued;

	public static long SizeOf(ModMetaData m) => sizes.TryGetValue(m.PackageId, out long s) ? s : -1;

	public static void MeasureAll(List<ModMetaData> mods)
	{
		if (Interlocked.Exchange(ref queued, 1) == 1)
		{
			return;
		}
		List<KeyValuePair<string, string>> work = mods.Where(m => m.RootDir != null).Select(m => new KeyValuePair<string, string>(m.PackageId, m.RootDir.FullName)).ToList();
		ThreadPool.QueueUserWorkItem(_ =>
		{
			foreach (KeyValuePair<string, string> w in work)
			{
				long total = 0;
				try
				{
					foreach (string file in Directory.EnumerateFiles(w.Value, "*", SearchOption.AllDirectories))
					{
						try
						{
							total += new FileInfo(file).Length;
						}
						catch (Exception)
						{
						}
					}
				}
				catch (Exception)
				{
				}
				sizes[w.Key] = total;
			}
		});
	}

	public static string FormatSize(long b) => b < 0 ? "..." : b >= 1L << 30 ? (b / (double)(1L << 30)).ToString("0.0") + " GB" : b >= 1L << 20 ? (b / (double)(1L << 20)).ToString("0.#") + " MB" : Mathf.Max(1, (int)(b / 1024)) + " KB";
}

// Small text-entry dialog (preset names, new tags).
public class Dialog_LatticeName : Window
{
	private readonly string title;
	private readonly Action<string> onOk;
	private string value;
	private bool focused;

	public override Vector2 InitialSize => new Vector2(380f, 170f);

	public Dialog_LatticeName(string title, string initial, Action<string> onOk)
	{
		this.title = title;
		this.value = initial ?? "";
		this.onOk = onOk;
		forcePause = true;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(0f, 0f, inRect.width, 34f), title);
		Text.Font = GameFont.Small;
		GUI.SetNextControlName("LatticeName");
		value = Widgets.TextField(new Rect(0f, 44f, inRect.width, 30f), value);
		if (!focused)
		{
			focused = true;
			GUI.FocusControl("LatticeName");
		}
		if (Widgets.ButtonText(new Rect(inRect.width - 120f, inRect.height - 36f, 120f, 32f), "OK"))
		{
			Accept();
		}
	}

	public override void OnAcceptKeyPressed() => Accept();

	private void Accept()
	{
		if (value.Trim().Length > 0)
		{
			onOk(value.Trim());
			Close();
		}
	}
}
