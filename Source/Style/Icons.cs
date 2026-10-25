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
	private static readonly HashSet<DesignationCategoryDef> Warned = new HashSet<DesignationCategoryDef>();
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
		// Every category starts with Cancel/Remove-style designators; prefer something buildable so the
		// tab shows what the category is about.
		Texture found = null;
		foreach (Designator d in def.ResolvedAllowedDesignators)
		{
			if (d is Designator_Build)
			{
				found = For(d);
				if (found != null)
				{
					break;
				}
			}
		}
		if (found == null)
		{
			foreach (Designator d in def.ResolvedAllowedDesignators)
			{
				if (d is Designator_Cancel)
				{
					continue;
				}
				found = For(d);
				if (found != null)
				{
					break;
				}
			}
		}
		if (found != null)
		{
			CategoryCache[def] = found;
		}
		else if (Warned.Add(def))
		{
			Log.Warning($"[Lattice] No icon for architect category {def.defName}: " + string.Join(", ", System.Linq.Enumerable.Select(def.ResolvedAllowedDesignators,
				d => $"{d.GetType().Name}(icon={(d.icon != null)})")));
		}
		return found;
	}
}
