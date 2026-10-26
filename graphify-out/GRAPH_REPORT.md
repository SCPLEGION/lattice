# Graph Report - rimworld-ui-rework  (2026-10-26)

## Corpus Check
- 27 files · ~15,854 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 4 file(s) not represented in the graph (top: (none) 3, .xml 1)

## Summary
- 464 nodes · 1034 edges · 24 communities (23 shown, 1 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 3 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b75e5eb1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- VerMode
- Lattice
- ArchitectGrid
- ModManager
- MainMenuStyle.cs
- WindowPins
- .DrawCatalog
- Dialog_LatticeName
- Lattice (RimWorld 1.7 UI overhaul mod)
- .Draw
- Category
- WindowStyle.cs
- .Chip
- Lattice.csproj
- GroupBy
- Dock.cs
- WindowBehavior.cs
- harmonylib
- .WithAlpha
- LatticeSettings
- ArchitectWindow.cs
- ColonistBarStyle.cs
- unityengine
- RectExt.cs

## God Nodes (most connected - your core abstractions)
1. `ModManager` - 71 edges
2. `UiStyle` - 27 edges
3. `Lattice` - 25 edges
4. `ModStore` - 20 edges
5. `ArchitectGrid` - 19 edges
6. `LatticeSettings` - 14 edges
7. `Backdrops` - 11 edges
8. `WindowPins` - 11 edges
9. `Dialog_LatticeName` - 10 edges
10. `Category` - 9 edges

## Surprising Connections (you probably didn't know these)
- `LatticeMod` --references--> `LatticeSettings`  [EXTRACTED]
  Source/Mod.cs → Source/Settings/LatticeSettings.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Lattice UI features** — readme_icon_dock, readme_architect_window, readme_cinematic_main_menu, readme_bills_tab, readme_restyled_widgets [EXTRACTED 1.00]

## Communities (24 total, 1 thin omitted)

### Community 0 - "VerMode"
Cohesion: 0.40
Nodes (5): VerMode, AtLeast, Below, Excludes, Supports

### Community 1 - "Lattice"
Cohesion: 0.16
Nodes (11): Lattice, Patch_SaveGameBackdrop, Patch_ModsPageContents, Patch_ModsPageOpen, Patch_HighlightSelected, Patch_ModRow, system, system_collections_concurrent (+3 more)

### Community 2 - "ArchitectGrid"
Cohesion: 0.09
Nodes (26): ArchitectCategoryTab, MainTabWindow_Architect, Action, DesignationCategoryDef, Designator, FieldRef, List, MethodInfo (+18 more)

### Community 3 - "ModManager"
Cohesion: 0.07
Nodes (29): Category, ConcurrentDictionary, Func, GroupBy, Page_ModsConfig, SortBy, Action, Color (+21 more)

### Community 4 - "MainMenuStyle.cs"
Cohesion: 0.06
Nodes (25): DateTime, Entry, GameComponent, IReadOnlyList, Matrix4x4, MethodBase, List, Texture2D (+17 more)

### Community 5 - "WindowPins"
Cohesion: 0.20
Nodes (10): Flags, Dictionary, HashSet, Rect, Window, Flags, Patch_PinChip, Patch_PinnedAdd (+2 more)

### Community 6 - ".DrawCatalog"
Cohesion: 0.11
Nodes (18): Bill, BillStack, Building_WorkTable, ITab_Bills, RecipeDef, FieldRef, MethodInfo, Rect (+10 more)

### Community 7 - "Dialog_LatticeName"
Cohesion: 0.28
Nodes (6): Action, Rect, Vector2, Dialog_LatticeName, InitialSize, Window

### Community 8 - "Lattice (RimWorld 1.7 UI overhaul mod)"
Cohesion: 0.23
Nodes (12): Architect window, Colony screenshot backdrops, Bills tab, Build instructions, Cinematic main menu, Harmony 2.3.6, Centered icon dock, Lattice (RimWorld 1.7 UI overhaul mod) (+4 more)

### Community 9 - ".Draw"
Cohesion: 0.36
Nodes (6): Color, Rect, ButtonStyle, Patch_ButtonText, Patch_ButtonTextColored, TextAnchor

### Community 10 - "Category"
Cohesion: 0.22
Nodes (9): Category, All, Disabled, Enabled, Favourites, Issues, Local, Official (+1 more)

### Community 11 - "WindowStyle.cs"
Cohesion: 0.50
Nodes (3): Patch_DrawMenuSection, Patch_DrawWindowBackground, Patch_DrawWindowBackgroundTinted

### Community 12 - ".Chip"
Cohesion: 0.31
Nodes (5): Page, Vector2, Window, Patch_ModsPageSize, Patch_ModsViewSwitch

### Community 13 - "Lattice.csproj"
Cohesion: 0.50
Nodes (3): net10.0, Lib.Harmony (2.3.6), Microsoft.NET.Sdk

### Community 14 - "GroupBy"
Cohesion: 0.33
Nodes (6): GroupBy, Author, None, Source, Tag, Version

### Community 16 - "WindowBehavior.cs"
Cohesion: 0.31
Nodes (5): MainTabWindow, Window, Patch_TabWindowsCentered, Patch_WindowsMovable, Type, bool&gt;

### Community 17 - "harmonylib"
Cohesion: 0.20
Nodes (6): harmonylib, Mod, Rect, Patch_BillConfigContents, Patch_BillDoInterface, LatticeMod

### Community 19 - ".WithAlpha"
Cohesion: 0.05
Nodes (36): MainButtonsRoot, MainButtonWorker, Rect, List, Rect, ModMetaData, Rect, Patch_HighlightMouseover (+28 more)

### Community 20 - "LatticeSettings"
Cohesion: 0.16
Nodes (11): Listing_Standard, ModSettings, Rect, Color, Rect, Vector2, BackdropMode, Custom (+3 more)

### Community 22 - "ArchitectWindow.cs"
Cohesion: 0.18
Nodes (7): MainTabWindow, Patch_ArchitectPosition, Patch_DesignationTabOnGUI, Patch_BillsFillTab, Patch_TitleBG, system_linq, verse_sound

### Community 24 - "ColonistBarStyle.cs"
Cohesion: 0.33
Nodes (4): Pawn, Rect, Patch_DrawColonist, Patch_DrawSelectionOverlay

### Community 26 - "unityengine"
Cohesion: 0.16
Nodes (11): rimworld, Alert, Rect, Patch_AlertDrawAt, MainButtonStyle, Rect, Patch_InspectPaneOnGUI, Patch_OptionBackground (+3 more)

### Community 27 - "RectExt.cs"
Cohesion: 0.40
Nodes (3): Rect, Vector2, RectExt

## Knowledge Gaps
- **50 isolated node(s):** `CellSize`, `InfoWidth`, `Columns`, `net10.0`, `Lib.Harmony (2.3.6)` (+45 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 130 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModManager` connect `ModManager` to `VerMode`, `Lattice`, `.DrawCatalog`, `Category`, `.Chip`, `GroupBy`, `.WithAlpha`?**
  _High betweenness centrality (0.298) - this node is a cross-community bridge._
- **Why does `Lattice` connect `Lattice` to `MainMenuStyle.cs`, `WindowPins`, `.Draw`, `WindowStyle.cs`, `Dock.cs`, `WindowBehavior.cs`, `harmonylib`, `LatticeSettings`, `ArchitectWindow.cs`, `ColonistBarStyle.cs`, `unityengine`, `RectExt.cs`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Why does `UiStyle` connect `.WithAlpha` to `Lattice`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **What connects `CellSize`, `InfoWidth`, `Columns` to the rest of the system?**
  _50 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ArchitectGrid` be split into smaller, more focused modules?**
  _Cohesion score 0.09268292682926829 - nodes in this community are weakly interconnected._
- **Should `ModManager` be split into smaller, more focused modules?**
  _Cohesion score 0.07240948813982521 - nodes in this community are weakly interconnected._
- **Should `MainMenuStyle.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.06312292358803986 - nodes in this community are weakly interconnected._