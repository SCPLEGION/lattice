using HarmonyLib;
using Verse;

namespace Lattice;

public class LatticeMod : Mod
{
	public LatticeMod(ModContentPack content) : base(content)
	{
		new Harmony("scplegion.lattice").PatchAll();
		Log.Message("[Lattice] loaded");
	}
}
