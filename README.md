# Lattice

A RimWorld 1.7 UI overhaul in one dark, glassy style with a gold accent: clean icon + name grids instead of cramped lists, a cinematic main menu and consistent buttons, windows and tabs everywhere.

## Features

- **Centered icon dock** – the bottom bar becomes a compact dock of icons (names and hotkeys in tooltips).
- **Architect window** – the hammer button opens a floating window: categories on the left, icon + name grid in the middle, info panel on the right, search on top. It is still the vanilla architect underneath, so hotkeys, selected category and designator state keep working. Grids are built from live defs, so modded buildings and tabs appear automatically.
- **Cinematic main menu** – the background is a screenshot of your colony (latest save, most played, or one you pick), with a slow camera drift, vignette and tall menu rows. A screenshot of the map (no UI) is taken after every save.
- **Bills tab** – searchable one-click grid of everything a work table can make, current bills as cards next to it.
- **Everywhere else** – buttons, windows, menu sections, tab strips, table rows, list highlights, option cards (scenario, storyteller...), page titles, the mods screen (cards with state edges), colonist bar, alerts and inspect pane.

## Settings

Options → Mod settings → **Lattice**: architect columns, cell size and info panel, the dock on/off, restyling of vanilla widgets on/off (turn it off if another UI mod conflicts), and the main menu background mode.

Backdrops are stored in the game's config folder under `Lattice/Backdrops/` (`<save>.jpg` plus a small `<save>.txt`). You can drop your own JPGs there too.

## Build

Requires the .NET SDK and a RimWorld 1.7 install. Harmony (`Lib.Harmony` 2.3.6) is restored from NuGet; the game references (`Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll`) are read from the game folder.

```
dotnet build Source -p:GameDir=/path/to/RimWorld
```

`GameDir` defaults to `~/.steam/steam/steamapps/common/RimWorld`. The output (`Lattice.dll`, `0Harmony.dll`) goes to `1.7/Assemblies/`. Put the repo folder (or a symlink to it) in RimWorld's `Mods/` directory and enable **Lattice**.

## License

[MIT](LICENSE).
