using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Colony screenshots used as the main menu background. A screenshot (map view only, no UI) is taken
// shortly after every save and stored with a small metadata file; the menu then picks one by mode.
public static class Backdrops
{
	public class Entry
	{
		public string name;
		public string image;
		public string colony;
		public float playHours;
		public DateTime saved;
	}

	public static string Dir => Path.Combine(GenFilePaths.ConfigFolderPath, "Lattice", "Backdrops");

	private static List<Entry> entries;
	private static Texture2D tex;
	private static string texPath;

	public static IReadOnlyList<Entry> All()
	{
		if (entries == null)
		{
			Refresh();
		}
		return entries;
	}

	public static void Refresh()
	{
		entries = new List<Entry>();
		try
		{
			if (!Directory.Exists(Dir))
			{
				return;
			}
			foreach (string image in Directory.GetFiles(Dir, "*.jpg"))
			{
				Entry e = new Entry { name = Path.GetFileNameWithoutExtension(image), image = image, colony = Path.GetFileNameWithoutExtension(image), saved = File.GetLastWriteTime(image) };
				string meta = Path.ChangeExtension(image, ".txt");
				if (File.Exists(meta))
				{
					foreach (string line in File.ReadAllLines(meta))
					{
						int i = line.IndexOf('=');
						if (i < 0)
						{
							continue;
						}
						string key = line.Substring(0, i), value = line.Substring(i + 1);
						if (key == "colony") e.colony = value;
						else if (key == "hours" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float h)) e.playHours = h;
					}
				}
				entries.Add(e);
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[Lattice] Could not read backdrops: " + ex.Message);
		}
	}

	public static Entry Pick()
	{
		LatticeSettings s = LatticeMod.Settings;
		IReadOnlyList<Entry> all = All();
		if (s.backdropMode == BackdropMode.Custom && !s.backdropCustom.NullOrEmpty() && File.Exists(s.backdropCustom))
		{
			return all.FirstOrDefault(e => e.image == s.backdropCustom) ?? new Entry { name = "custom", image = s.backdropCustom, colony = Path.GetFileNameWithoutExtension(s.backdropCustom) };
		}
		if (all.Count == 0)
		{
			return null;
		}
		return s.backdropMode == BackdropMode.MostPlayed ? all.OrderByDescending(e => e.playHours).First() : all.OrderByDescending(e => e.saved).First();
	}

	public static Texture2D Texture(Entry e)
	{
		if (e == null)
		{
			return null;
		}
		if (tex != null && texPath == e.image)
		{
			return tex;
		}
		try
		{
			Texture2D loaded = new Texture2D(2, 2, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
			if (!ImageConversion.LoadImage(loaded, File.ReadAllBytes(e.image)))
			{
				return null;
			}
			if (tex != null)
			{
				UnityEngine.Object.Destroy(tex);
			}
			tex = loaded;
			texPath = e.image;
		}
		catch (Exception ex)
		{
			Log.Warning("[Lattice] Could not load backdrop " + e.image + ": " + ex.Message);
			texPath = e.image;
			tex = null;
		}
		return tex;
	}

	// Must run on the main thread (called from the game component's update).
	public static void Capture(string saveName)
	{
		try
		{
			if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || Find.World == null || Find.Camera == null)
			{
				return;
			}
			Directory.CreateDirectory(Dir);
			Camera cam = Find.Camera;
			const int w = 1920, h = 1080;
			RenderTexture rt = RenderTexture.GetTemporary(w, h, 24);
			RenderTexture prevTarget = cam.targetTexture, prevActive = RenderTexture.active;
			cam.targetTexture = rt;
			cam.Render();
			RenderTexture.active = rt;
			Texture2D shot = new Texture2D(w, h, TextureFormat.RGB24, false);
			shot.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
			shot.Apply();
			cam.targetTexture = prevTarget;
			RenderTexture.active = prevActive;
			RenderTexture.ReleaseTemporary(rt);

			string safe = string.Concat(saveName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
			string image = Path.Combine(Dir, safe + ".jpg");
			File.WriteAllBytes(image, ImageConversion.EncodeToJPG(shot, 88));
			UnityEngine.Object.Destroy(shot);
			string colony = Faction.OfPlayerSilentFail?.Name ?? safe;
			float hours = Find.GameInfo.RealPlayTimeInteracting / 3600f;
			File.WriteAllText(Path.ChangeExtension(image, ".txt"), $"colony={colony}\nhours={hours.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
			entries = null;
		}
		catch (Exception ex)
		{
			Log.Warning("[Lattice] Backdrop capture failed: " + ex);
		}
	}
}

public class LatticeGameComponent : GameComponent
{
	public static string PendingSave;

	public LatticeGameComponent(Game game)
	{
	}

	public override void GameComponentUpdate()
	{
		if (PendingSave != null)
		{
			string name = PendingSave;
			PendingSave = null;
			Backdrops.Capture(name);
		}
	}
}

[HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame))]
public static class Patch_SaveGameBackdrop
{
	// Saving may run off the main thread; only queue the capture here.
	public static void Postfix(string fileName) => LatticeGameComponent.PendingSave = fileName;
}
