using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Designator icons are filled in lazily: Designator_Build.UpdateIcon runs from GizmoOnGUI, which the
// custom grids bypass, so `icon` stays null until it has been called once.
public static class Icons
{
	private static readonly System.Reflection.MethodInfo UpdateIcon = AccessTools.Method(typeof(Designator_Build), "UpdateIcon");
	private static readonly Dictionary<DesignationCategoryDef, Texture> CategoryCache = new Dictionary<DesignationCategoryDef, Texture>();

	public static Texture For(Designator d)
	{
		if (d is Designator_Build build)
		{
			if (build.icon == null)
			{
				UpdateIcon?.Invoke(build, null);
			}
			return build.icon ?? build.ResolvedIcon(null);
		}
		return d.icon;
	}

	public static Texture For(DesignationCategoryDef def)
	{
		if (CategoryCache.TryGetValue(def, out Texture cached))
		{
			return cached;
		}
		Texture found = null;
		foreach (Designator d in def.ResolvedAllowedDesignators)
		{
			found = For(d);
			if (found != null)
			{
				break;
			}
		}
		if (found != null)
		{
			CategoryCache[def] = found;
		}
		return found;
	}
}
