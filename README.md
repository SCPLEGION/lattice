# Lattice

A RimWorld 1.7 UI overhaul: clean icon + name grids instead of cramped lists.

- **Architect menu** – horizontal category strip above the main bar, with a Cities-style popup icon+name grid for designators. Vanilla state (selected tab, search filter, designator manager) is kept, so hotkeys and other mods keep working.
- **Reworked bottom bar** – restyled main buttons.
- Grids are built from live defs, so modded buildings and tabs appear automatically.

## Build

Requires the .NET SDK and a RimWorld install (Harmony is restored from NuGet).

```
dotnet build Source -p:GameDir=/path/to/RimWorld
```

The output goes to `1.7/Assemblies/`. Put the repo folder in RimWorld's `Mods/` directory and enable **Lattice**.

## License

Add a license of your choice.
