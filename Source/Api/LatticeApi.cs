using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Public API for other mods. Call from a [StaticConstructorOnStartup] class (or Mod constructor for
// bar buttons, before the main tabs root is built); Lattice is a soft dependency via reflection or
// a direct assembly reference loaded after Lattice.
public static class LatticeApi
{
	private static readonly Dictionary<DesignationCategoryDef, List<Designator>> architectExtras = new();
	private static readonly Dictionary<MainButtonDef, Action> barCallbacks = new();
	private static readonly Dictionary<MainButtonDef, Func<string>> barBadges = new();
	private static readonly Dictionary<MainButtonDef, Func<bool>> barVisibility = new();
	private static readonly List<Predicate<Designator>> architectHidden = new();

	// Adds a designator to a category's popup grid, in addition to the def's allowedDesignatorClasses.
	// Appears for vanilla and modded categories alike. Duplicates are ignored.
	public static void AddArchitectEntry(DesignationCategoryDef category, Designator designator)
	{
		if (category == null)
		{
			throw new ArgumentNullException(nameof(category));
		}
		if (designator == null)
		{
			throw new ArgumentNullException(nameof(designator));
		}
		if (!architectExtras.TryGetValue(category, out List<Designator> list))
		{
			architectExtras[category] = list = new List<Designator>();
		}
		if (!list.Contains(designator))
		{
			list.Add(designator);
		}
	}

	// Adds a button to the bottom bar that runs onClick. iconPath is a Textures/ path (optional).
	// Must be called before the bottom bar is built, i.e. during mod loading / startup.
	public static MainButtonDef AddBarButton(string defName, string label, Action onClick, string iconPath = null,
		string description = null, int order = 1000)
	{
		if (defName.NullOrEmpty() || onClick == null)
		{
			throw new ArgumentException("defName and onClick are required");
		}
		if (DefDatabase<MainButtonDef>.GetNamedSilentFail(defName) != null)
		{
			Log.Warning($"[Lattice] Bar button '{defName}' already exists, skipping.");
			return null;
		}
		MainButtonDef def = new MainButtonDef
		{
			defName = defName,
			label = label,
			description = description,
			iconPath = iconPath,
			order = order,
			workerClass = typeof(CallbackButtonWorker),
			validWithoutMap = true,
		};
		DefDatabase<MainButtonDef>.Add(def);
		barCallbacks[def] = onClick;
		return def;
	}

	// Hides every designator the predicate matches from the architect popups (vanilla and modded).
	public static void HideArchitectEntries(Predicate<Designator> hide)
	{
		if (hide == null)
		{
			throw new ArgumentNullException(nameof(hide));
		}
		architectHidden.Add(hide);
	}

	// Hides a designator that places/builds the given def (buildings, terrain, etc.).
	public static void HideArchitectBuildable(BuildableDef buildable)
	{
		if (buildable == null)
		{
			throw new ArgumentNullException(nameof(buildable));
		}
		HideArchitectEntries(d => d is Designator_Build build && build.PlacingDef == buildable);
	}

	// Shows a small pill on a bottom-bar button (any button, vanilla included). The function is
	// polled every frame; return null or empty to show nothing, e.g. a count like "3".
	public static void SetBarButtonBadge(MainButtonDef def, Func<string> badge)
	{
		if (def == null)
		{
			throw new ArgumentNullException(nameof(def));
		}
		if (badge == null)
		{
			barBadges.Remove(def);
		}
		else
		{
			barBadges[def] = badge;
		}
	}

	// Hides a bottom-bar button while the function returns false (any button, vanilla included).
	// Only takes effect on buttons whose worker does not override Visible itself.
	public static void SetBarButtonVisible(MainButtonDef def, Func<bool> visible)
	{
		if (def == null)
		{
			throw new ArgumentNullException(nameof(def));
		}
		if (visible == null)
		{
			barVisibility.Remove(def);
		}
		else
		{
			barVisibility[def] = visible;
		}
	}

	internal static bool IsHidden(Designator d)
	{
		foreach (Predicate<Designator> hide in architectHidden)
		{
			if (hide(d))
			{
				return true;
			}
		}
		return false;
	}

	internal static string BadgeFor(MainButtonDef def)
	{
		return barBadges.TryGetValue(def, out Func<string> badge) ? badge() : null;
	}

	internal static bool VisibleFor(MainButtonDef def)
	{
		return !barVisibility.TryGetValue(def, out Func<bool> visible) || visible();
	}

	internal static IEnumerable<Designator> ExtraDesignators(DesignationCategoryDef category)
	{
		return architectExtras.TryGetValue(category, out List<Designator> list) ? list : null;
	}

	internal static void Invoke(MainButtonDef def)
	{
		if (barCallbacks.TryGetValue(def, out Action action))
		{
			action();
		}
	}
}

public class CallbackButtonWorker : MainButtonWorker
{
	public override void Activate() => LatticeApi.Invoke(def);
}

[HarmonyPatch(typeof(MainButtonWorker), nameof(MainButtonWorker.Visible), MethodType.Getter)]
public static class Patch_ButtonVisible
{
	public static void Postfix(MainButtonWorker __instance, ref bool __result)
	{
		if (__result)
		{
			__result = LatticeApi.VisibleFor(__instance.def);
		}
	}
}
