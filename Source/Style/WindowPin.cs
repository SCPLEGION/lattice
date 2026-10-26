using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lattice;

// Pin chip in the corner of windows. Pinned dialogs ignore Esc/Enter/click-outside; pinned tab windows
// (Work, Research, ...) stay open when another tab is opened, so several can be on screen at once.
// Unpinning a tab window closes it.
public static class WindowPins
{
	private struct Flags
	{
		public bool accept, cancel, outside;
	}

	private static readonly Dictionary<Window, Flags> Pinned = new Dictionary<Window, Flags>();
	public static bool Bypass;

	public static bool IsPinned(Window w) => Pinned.ContainsKey(w);

	// Pin only where keeping a window open is useful: the main tab windows and a few dialogs
	// (info cards, bill config, policy/area managers). Debug/log/edit windows and everything else are left alone.
	private static readonly HashSet<string> PinnableDialogs = new HashSet<string>
	{
		"Dialog_InfoCard", "Dialog_BillConfig", "Dialog_MechBillConfig", "Dialog_ManageAreas",
		"Dialog_ManageApparelPolicies", "Dialog_ManageDrugPolicies", "Dialog_ManageFoodPolicies",
		"Dialog_ManageReadingPolicies", "Dialog_MedicalDefaults", "Dialog_Ideo",
	};

	public static bool Eligible(Window w) =>
		LatticeMod.Settings.pinWindows && w != null && w.doWindowBackground
		&& ((w is MainTabWindow && !(w is MainTabWindow_Inspect)) || PinnableDialogs.Contains(w.GetType().Name));

	public static Rect ChipRect(Window w) => new Rect(w.windowRect.width - 24f - (w.doCloseX ? 30f : 0f), 3f, 18f, 18f);

	public static void Toggle(Window w)
	{
		if (Pinned.TryGetValue(w, out Flags f))
		{
			Pinned.Remove(w);
			w.closeOnAccept = f.accept;
			w.closeOnCancel = f.cancel;
			w.closeOnClickedOutside = f.outside;
			if (w is MainTabWindow)
			{
				Find.WindowStack.TryRemove(w);
			}
			return;
		}
		Pinned[w] = new Flags { accept = w.closeOnAccept, cancel = w.closeOnCancel, outside = w.closeOnClickedOutside };
		w.closeOnAccept = false;
		w.closeOnCancel = false;
		w.closeOnClickedOutside = false;
	}

	public static void Forget(Window w) => Pinned.Remove(w);
}

[HarmonyPatch(typeof(Window), "InnerWindowOnGUI")]
public static class Patch_PinChip
{
	public static void Prefix(Window __instance)
	{
		if (!WindowPins.Eligible(__instance))
		{
			return;
		}
		Event e = Event.current;
		if (e.type == EventType.MouseDown && e.button == 0 && WindowPins.ChipRect(__instance).Contains(e.mousePosition))
		{
			WindowPins.Toggle(__instance);
			e.Use();
		}
	}

	public static void Postfix(Window __instance)
	{
		if (!WindowPins.Eligible(__instance) || Event.current.type != EventType.Repaint)
		{
			return;
		}
		Rect chip = WindowPins.ChipRect(__instance);
		bool pinned = WindowPins.IsPinned(__instance);
		bool hover = Mouse.IsOver(chip);
		Color col = pinned ? UiStyle.Accent : (hover ? Color.white : UiStyle.TextSoft);
		Matrix4x4 prev = GUI.matrix;
		if (!pinned)
		{
			GUIUtility.RotateAroundPivot(45f, chip.center);
		}
		GUI.color = col;
		float cx = chip.center.x, top = chip.y + 1f;
		Widgets.DrawBoxSolid(new Rect(cx - 4f, top, 8f, 2f), col);
		Widgets.DrawBoxSolid(new Rect(cx - 2f, top + 2f, 4f, 5f), col);
		Widgets.DrawBoxSolid(new Rect(cx - 5f, top + 7f, 10f, 2f), col);
		Widgets.DrawBoxSolid(new Rect(cx - 0.5f, top + 9f, 1f, 6f), col);
		GUI.matrix = prev;
		GUI.color = Color.white;
		if (hover)
		{
			TooltipHandler.TipRegion(chip, pinned ? "Unpin" : "Pin: keep this window open (and open other tabs next to it)");
		}
	}
}

[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove), typeof(Window), typeof(bool))]
public static class Patch_PinnedRemove
{
	public static bool Prefix(Window window)
	{
		if (window == null || !WindowPins.IsPinned(window))
		{
			return true;
		}
		if (window is MainTabWindow)
		{
			return false;
		}
		WindowPins.Forget(window);
		return true;
	}
}

[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
public static class Patch_PinnedAdd
{
	// Re-opening a pinned tab that is already on screen would log a duplicate-window warning.
	public static bool Prefix(Window window) => window == null || !(window is MainTabWindow) || !WindowPins.IsPinned(window);
}
