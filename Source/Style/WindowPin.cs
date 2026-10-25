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

	public static bool Eligible(Window w) =>
		LatticeMod.Settings.pinWindows && w != null && w.doWindowBackground
		&& !(w is Page) && !(w is FloatMenu) && !(w is MainTabWindow_Inspect) && !w.GetType().Name.Contains("Immediate");

	public static Rect ChipRect(Window w) => new Rect(w.windowRect.width - 50f - (w.doCloseX ? 30f : 0f), 2f, 46f, 16f);

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
		Widgets.DrawBoxSolid(chip, pinned ? UiStyle.WithAlpha(UiStyle.Accent, 0.9f) : new Color(0f, 0f, 0f, hover ? 0.7f : 0.45f));
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = pinned ? Color.black : (hover ? Color.white : UiStyle.TextSoft);
		Widgets.Label(chip, pinned ? "Pinned" : "Pin");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
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
