using HarmonyLib;
using UnityEngine;
using Verse;

namespace Lattice;

public class LatticeMod : Mod
{
	public static LatticeSettings Settings;

	// RELEASE.MINOR.PATCH from About.xml <modVersion>; see CHANGELOG.md for what each part means.
	public static string Version = "dev";

	public LatticeMod(ModContentPack content) : base(content)
	{
		if (!content.ModMetaData.ModVersion.NullOrEmpty())
		{
			Version = content.ModMetaData.ModVersion;
		}
		Settings = GetSettings<LatticeSettings>();
		new Harmony("scplegion.lattice").PatchAll();
		Log.Message("[Lattice] " + Version + " loaded");
	}

	public override string SettingsCategory() => "Lattice";

	public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
}
