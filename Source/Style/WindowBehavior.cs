using System.Linq;
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
	// Vanilla research stays a normal movable, centered window. When a mod replaces it (another window class,
	// a subclass) or patches it (tree mods draw their own pannable view and handle mouse drags), the window is
	// left exactly as that mod made it: not draggable and not moved.
	private static readonly System.Collections.Generic.Dictionary<System.Type, bool> researchModded = new System.Collections.Generic.Dictionary<System.Type, bool>();

	public static bool IsResearch(Window w)
	{
		if (!(w is MainTabWindow))
		{
			return false;
		}
		System.Type t = w.GetType();
		if (researchModded.TryGetValue(t, out bool cached))
		{
			return cached;
		}
		bool modded;
		if (t != typeof(MainTabWindow_Research))
		{
			modded = w is MainTabWindow_Research || t.Name.IndexOf("Research", System.StringComparison.OrdinalIgnoreCase) >= 0;
		}
		else
		{
			modded = false;
			foreach (System.Reflection.MethodBase m in HarmonyLib.AccessTools.GetDeclaredMethods(t))
			{
				HarmonyLib.Patches info = Harmony.GetPatchInfo(m);
				if (info == null)
				{
					continue;
				}
				foreach (HarmonyLib.Patch p in info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers))
				{
					if (p.owner != "scplegion.lattice")
					{
						modded = true;
					}
				}
			}
		}
		researchModded[t] = modded;
		return modded;
	}

	public static void Prefix(Window window)
	{
		if (!LatticeMod.Settings.movableWindows || window == null)
		{
			return;
		}
		if (IsResearch(window) || window is Page || window is FloatMenu || window is MainTabWindow_Inspect || window.GetType().Name.Contains("Immediate"))
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
		if (!LatticeMod.Settings.centerWindows || __instance is MainTabWindow_Architect || __instance is MainTabWindow_Inspect || Patch_WindowsMovable.IsResearch(__instance))
		{
			return;
		}
		Rect r = __instance.windowRect;
		r.x = (UI.screenWidth - r.width) / 2f;
		__instance.windowRect = r;
	}
}
