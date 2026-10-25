# Lattice

A RimWorld 1.7 UI overhaul: clean icon + name grids instead of cramped lists.

- **Architect menu** – horizontal category strip above the main bar, with a Cities-style popup icon+name grid for designators. Vanilla state (selected tab, search filter, designator manager) is kept, so hotkeys and other mods keep working.
- **Reworked bottom bar** – restyled main buttons.
- **Cinematic look** – gradient panels, soft shadows, amber glow accents and eased hover/open animations, all drawn procedurally (no texture assets). Shared tokens and helpers live in `Source/Style/UiStyle.cs`.
- Grids are built from live defs, so modded buildings and tabs appear automatically.

## API for other mods

`Lattice.LatticeApi` (see `Source/Api/LatticeApi.cs`):

```csharp
// Extra entry in an architect category popup (vanilla or modded category)
LatticeApi.AddArchitectEntry(DesignationCategoryDefOf.Structure, new MyDesignator());

// Extra bottom-bar button; call during mod load, before the bar is built
LatticeApi.AddBarButton("MyMod_Button", "My button", () => Find.WindowStack.Add(new MyWindow()),
    iconPath: "UI/MyIcon", description: "Opens my window", order: 1000);

// Badge pill on any bottom-bar button (polled each frame; null/empty hides it)
LatticeApi.SetBarButtonBadge(MainButtonDefOf.Architect, () => pending > 0 ? pending.ToString() : null);

// Show a bottom-bar button only under a condition
LatticeApi.SetBarButtonVisible(myButtonDef, () => Find.CurrentMap != null);

// Hide architect entries
LatticeApi.HideArchitectBuildable(ThingDefOf.Wall);
LatticeApi.HideArchitectEntries(d => d is Designator_Dropdown);
```

## Build

Requires the .NET SDK and a RimWorld install (Harmony is restored from NuGet).

```
dotnet build Source -p:GameDir=/path/to/RimWorld
```

The output goes to `1.7/Assemblies/`. Put the repo folder in RimWorld's `Mods/` directory and enable **Lattice**.

## License

Add a license of your choice.
