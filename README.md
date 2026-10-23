# Lattice

A RimWorld 1.7 UI overhaul: clean icon + name grids instead of cramped lists.

- **Architect menu** – horizontal category strip above the main bar, with a Cities-style popup icon+name grid for designators. Vanilla state (selected tab, search filter, designator manager) is kept, so hotkeys and other mods keep working.
- **Reworked bottom bar** – restyled main buttons.
- **Colonist bar** – dark cell behind each portrait, accent outline on selected colonists.
- **Alerts** – coloured edge on every alert row (red for critical).
- **Inspect pane** – dark panel with an accent line, matching the rest of the UI.
- Grids are built from live defs, so modded buildings and tabs appear automatically.

## Settings

Options → Mod settings → **Lattice**: architect grid columns (4–12), cell size (64–110 px), and whether to show the info panel next to the grid. Columns are reduced automatically on narrow screens.

## Build

Requires the .NET SDK and a RimWorld 1.7 install. Harmony (`Lib.Harmony` 2.3.6) is restored from NuGet; the game references (`Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll`) are read from the game folder.

```
dotnet build Source -p:GameDir=/path/to/RimWorld
```

`GameDir` defaults to `~/.steam/steam/steamapps/common/RimWorld`. The output (`Lattice.dll`, `0Harmony.dll`) goes to `1.7/Assemblies/`. Put the repo folder in RimWorld's `Mods/` directory and enable **Lattice**.

## License

[MIT](LICENSE).
