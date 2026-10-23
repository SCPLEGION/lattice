using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Adds a coloured edge to every alert row (red for critical, accent for the rest), drawn on top of the vanilla row.
[HarmonyPatch(typeof(Alert), nameof(Alert.DrawAt))]
public static class Patch_AlertDrawAt
{
	private const float EdgeWidth = 3f;

	public static void Postfix(Alert __instance, Rect __result)
	{
		Color edge = __instance.Priority == AlertPriority.Critical ? ColorLibrary.RedReadable : UiStyle.Accent;
		Widgets.DrawBoxSolid(new Rect(__result.x, __result.y, EdgeWidth, __result.height), edge);
	}
}
