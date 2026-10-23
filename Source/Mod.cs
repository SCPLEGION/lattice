using HarmonyLib;
using UnityEngine;
using Verse;

namespace Lattice;

public class LatticeMod : Mod
{
	public static LatticeSettings Settings;

	public LatticeMod(ModContentPack content) : base(content)
	{
		Settings = GetSettings<LatticeSettings>();
		new Harmony("scplegion.lattice").PatchAll();
		Log.Message("[Lattice] loaded");
	}

	public override string SettingsCategory() => "Lattice";

	public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
}
