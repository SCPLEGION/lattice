# Graph Report - rimworld-ui-rework  (2026-10-26)

## Corpus Check
- 27 files · ~14,733 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 4 file(s) not represented in the graph (top: (none) 3, .xml 1)

## Summary
- 443 nodes · 981 edges · 32 communities (26 shown, 6 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 3 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b8e88f73`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- UiStyle
- Lattice
- ArchitectGrid
- ModManager
- Backdrops
- WindowPins
- .DrawCatalog
- Dialog_LatticeName
- Lattice (RimWorld 1.7 UI overhaul mod)
- .Draw
- Category
- .Postfix
- .Chip
- Lattice.csproj
- GroupBy
- VerMode
- .IsResearch
- .DrawGradient
- ModStore
- .WithAlpha
- LatticeSettings
- Rect
- Rect
- Patch_MenuLanguageButton
- .Postfix
- Patch_ExpansionIcons
- .Postfix
- .MovedBy
- .Prefix
- Patch_MainMenuControls
- Rect
- .Prefix

## God Nodes (most connected - your core abstractions)
1. `ModManager` - 71 edges
2. `Lattice` - 25 edges
3. `UiStyle` - 21 edges
4. `ModStore` - 20 edges
5. `ArchitectGrid` - 14 edges
6. `Backdrops` - 11 edges
7. `WindowPins` - 11 edges
8. `Dialog_LatticeName` - 10 edges
9. `Category` - 9 edges
10. `BillsTab` - 8 edges

## Surprising Connections (you probably didn't know these)
- `LatticeMod` --references--> `LatticeSettings`  [EXTRACTED]
  Source/Mod.cs → Source/Settings/LatticeSettings.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Lattice UI features** — readme_icon_dock, readme_architect_window, readme_cinematic_main_menu, readme_bills_tab, readme_restyled_widgets [EXTRACTED 1.00]

## Communities (32 total, 6 thin omitted)

### Community 0 - "UiStyle"
Cohesion: 0.16
Nodes (12): MainButtonWorker, Rect, MainButtonStyle, Color, Dictionary, Rect, Texture, Texture2D (+4 more)

### Community 1 - "Lattice"
Cohesion: 0.13
Nodes (20): Lattice, harmonylib, rimworld, MainTabWindow, Patch_ArchitectPosition, Patch_DesignationTabOnGUI, Rect, Patch_InspectPaneOnGUI (+12 more)

### Community 2 - "ArchitectGrid"
Cohesion: 0.08
Nodes (25): ArchitectCategoryTab, MainTabWindow_Architect, Action, DesignationCategoryDef, Designator, FieldRef, MethodInfo, Rect (+17 more)

### Community 3 - "ModManager"
Cohesion: 0.09
Nodes (22): Category, Func, GroupBy, Page_ModsConfig, SortBy, Action, Color, FieldRef (+14 more)

### Community 4 - "Backdrops"
Cohesion: 0.10
Nodes (18): DateTime, Entry, GameComponent, IReadOnlyList, List, Texture2D, Backdrops, Dir (+10 more)

### Community 5 - "WindowPins"
Cohesion: 0.20
Nodes (10): Flags, Dictionary, HashSet, Rect, Window, Flags, Patch_PinChip, Patch_PinnedAdd (+2 more)

### Community 6 - ".DrawCatalog"
Cohesion: 0.11
Nodes (19): Bill, BillStack, Building_WorkTable, ITab_Bills, RecipeDef, FieldRef, MethodInfo, Rect (+11 more)

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

### Community 11 - ".Postfix"
Cohesion: 0.50
Nodes (3): Page, Vector2, Patch_ModsPageSize

### Community 13 - "Lattice.csproj"
Cohesion: 0.50
Nodes (3): net10.0, Lib.Harmony (2.3.6), Microsoft.NET.Sdk

### Community 14 - "GroupBy"
Cohesion: 0.33
Nodes (6): GroupBy, Author, None, Source, Tag, Version

### Community 15 - "VerMode"
Cohesion: 0.40
Nodes (5): VerMode, AtLeast, Below, Excludes, Supports

### Community 16 - ".IsResearch"
Cohesion: 0.38
Nodes (4): MainTabWindow, Window, Patch_TabWindowsCentered, Patch_WindowsMovable

### Community 17 - ".DrawGradient"
Cohesion: 0.16
Nodes (9): Patch_BillDoInterface, List, Rect, ModMetaData, Patch_ModRow, Patch_OptionBackground, FieldRef, Patch_TabRecordDraw (+1 more)

### Community 18 - "ModStore"
Cohesion: 0.19
Nodes (8): ConcurrentDictionary, Patch_ModsPageClose, Dictionary, HashSet, List, ModMetaData, ModStore, FilePath

### Community 19 - ".WithAlpha"
Cohesion: 0.16
Nodes (9): MainButtonsRoot, FieldRef, Patch_Dock, Patch_GlobalControlsPanel, Color, Rect, Patch_DrawMenuSection, Patch_DrawWindowBackground (+1 more)

### Community 20 - "LatticeSettings"
Cohesion: 0.15
Nodes (10): Mod, ModSettings, Rect, LatticeMod, Rect, BackdropMode, Custom, Latest (+2 more)

### Community 21 - "Rect"
Cohesion: 0.29
Nodes (4): Rect, Patch_HighlightMouseover, Patch_HighlightSelected, Patch_ModInfo

### Community 22 - "Rect"
Cohesion: 0.29
Nodes (4): Rect, Patch_AltRect, Patch_LightHighlight, Patch_TitleBG

### Community 23 - "Patch_MenuLanguageButton"
Cohesion: 0.40
Nodes (3): MethodBase, IEnumerable, Patch_MenuLanguageButton

### Community 24 - ".Postfix"
Cohesion: 0.33
Nodes (4): Pawn, Rect, Patch_DrawColonist, Patch_DrawSelectionOverlay

### Community 26 - ".Postfix"
Cohesion: 0.50
Nodes (3): Alert, Rect, Patch_AlertDrawAt

### Community 27 - ".MovedBy"
Cohesion: 0.50
Nodes (3): Rect, Vector2, RectExt

## Knowledge Gaps
- **44 isolated node(s):** `CellSize`, `InfoWidth`, `Columns`, `net10.0`, `Lib.Harmony (2.3.6)` (+39 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 120 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModManager` connect `ModManager` to `Lattice`, `.DrawCatalog`, `Category`, `.Postfix`, `GroupBy`, `VerMode`, `.DrawGradient`, `ModStore`?**
  _High betweenness centrality (0.310) - this node is a cross-community bridge._
- **Why does `Lattice` connect `Lattice` to `.Draw`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **Why does `ModStore` connect `ModStore` to `Lattice`, `ModManager`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **What connects `CellSize`, `InfoWidth`, `Columns` to the rest of the system?**
  _44 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Lattice` be split into smaller, more focused modules?**
  _Cohesion score 0.12755102040816327 - nodes in this community are weakly interconnected._
- **Should `ArchitectGrid` be split into smaller, more focused modules?**
  _Cohesion score 0.08258258258258258 - nodes in this community are weakly interconnected._
- **Should `ModManager` be split into smaller, more focused modules?**
  _Cohesion score 0.08828828828828829 - nodes in this community are weakly interconnected._