using System;
using System.Collections.Generic;
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
