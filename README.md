# Lattice

A RimWorld 1.7 UI overhaul in one dark, glassy style with a gold accent: clean icon + name grids instead of cramped lists, a cinematic main menu and consistent buttons, windows and tabs everywhere.

## Features

- **Centered icon dock** – the bottom bar becomes a compact dock of icons (names and hotkeys in tooltips).
- **Architect window** – the hammer button opens a floating window: categories on the left, icon + name grid in the middle, info panel on the right, search on top. It is still the vanilla architect underneath, so hotkeys, selected category and designator state keep working. Grids are built from live defs, so modded buildings and tabs appear automatically.
- **Cinematic main menu** – the background is a screenshot of your colony (latest save, most played, or one you pick) with a slow camera drift and a dark falloff on the left. The game title, a **Continue** card for your latest save (thumbnail, colony, play time, how long ago; one click loads it) and large bare options that slide in sit in one column; DLC icons, web links, language and version live in a quiet bottom bar. Options added by other mods still show up. A screenshot of the map (no UI) is taken after every save.
- **Bills tab** – searchable one-click grid of everything a work table can make, current bills as cards next to it.
- **Mods screen** – cards or rows with state edges: red for mods not made for this version, blue **Older** for mods made for the last one or two versions (1.7 usually still loads 1.6 and 1.5 mods), amber for conflicts and missing requirements.
- **Everywhere else** – buttons, windows, menu sections, tab strips, table rows, list highlights, option cards (scenario, storyteller...), page titles, colonist bar, alerts and inspect pane.

## Settings

Options → Mod settings → **Lattice** (sections: Appearance, Windows, Architect, Main menu, Mods, About): architect columns, cell size and info panel, the dock on/off, restyling of vanilla widgets on/off (turn it off if another UI mod conflicts; it also turns the cinematic menu off), and the main menu: cinematic layout on/off, the Continue card, shade, drift, capitals and background mode, and how many older game versions the mods screen counts as Older.

Backdrops are stored in the game's config folder under `Lattice/Backdrops/` (`<save>.jpg` plus a small `<save>.txt`). You can drop your own JPGs there too.

## Versioning

`RELEASE.MINOR.PATCH`, kept in `About/About.xml` and shown in the settings and on the main menu: RELEASE for big reworks, MINOR for features, PATCH for fixes only (the "shame" number). Bump with `scripts/bump.sh release|minor|patch`; see [CHANGELOG.md](CHANGELOG.md).

## Build

Requires the .NET SDK and a RimWorld 1.7 install. Harmony (`Lib.Harmony` 2.3.6) is restored from NuGet; the game references (`Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll`) are read from the game folder.

```
dotnet build Source -p:GameDir=/path/to/RimWorld
```

`GameDir` defaults to `~/.steam/steam/steamapps/common/RimWorld`. The output (`Lattice.dll`, `0Harmony.dll`) goes to `1.7/Assemblies/`. Put the repo folder (or a symlink to it) in RimWorld's `Mods/` directory and enable **Lattice**.

## License

[MIT](LICENSE).
