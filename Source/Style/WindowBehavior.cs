using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Windows open centered and can be dragged anywhere. Pages, float menus, the inspect pane and
// immediate windows keep their vanilla behaviour.
[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
public static class Patch_WindowsMovable
{
	public static void Prefix(Window window)
	{
		if (!LatticeMod.Settings.movableWindows || window == null)
		{
			return;
		}
		if (window is Page || window is FloatMenu || window is MainTabWindow_Inspect || window.GetType().Name.Contains("Immediate"))
		{
			return;
		}
		if (window.doWindowBackground)
		{
			window.draggable = true;
		}
	}
}

[HarmonyPatch(typeof(MainTabWindow), "SetInitialSizeAndPosition")]
public static class Patch_TabWindowsCentered
{
	public static void Postfix(MainTabWindow __instance)
	{
		if (!LatticeMod.Settings.centerWindows || __instance is MainTabWindow_Architect || __instance is MainTabWindow_Inspect)
		{
			return;
		}
		Rect r = __instance.windowRect;
		r.x = (UI.screenWidth - r.width) / 2f;
		__instance.windowRect = r;
	}
}
