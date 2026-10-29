# Graph Report - rimworld-ui-rework  (2026-10-29)

## Corpus Check
- 29 files · ~20,376 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 4 file(s) not represented in the graph (top: (none) 3, .xml 1)

## Summary
- 515 nodes · 1212 edges · 34 communities (30 shown, 4 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 6 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `22590c40`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- ModManager
- .DrawGradient
- Backdrops
- ArchitectGrid
- .DrawCatalog
- LatticeSettings
- WindowPins
- SortBy
- Lattice
- Lattice (RimWorld 1.7 UI overhaul mod)
- Section
- .Prefix
- .Draw
- .Chip
- Category
- Dialog_LatticeName
- Patch_WindowsMovable
- .Prefix
- GroupBy
- VerMode
- .MovedBy
- Lattice.csproj
- MainMenuStyle
- .Prefix
- UiStyle
- .WithAlpha
- Rect
- .Postfix
- Patch_MenuLanguageButton
- .Prefix
- .Postfix
- Patch_MainMenuControls
- Changelog
- bump.sh

## God Nodes (most connected - your core abstractions)
1. `ModManager` - 73 edges
2. `LatticeSettings` - 27 edges
3. `UiStyle` - 27 edges
4. `Lattice` - 25 edges
5. `ModStore` - 20 edges
6. `ArchitectGrid` - 19 edges
7. `MainMenuStyle` - 19 edges
8. `Backdrops` - 15 edges
9. `WindowPins` - 11 edges
10. `Category` - 10 edges

## Surprising Connections (you probably didn't know these)
- `LatticeMod` --references--> `LatticeSettings`  [EXTRACTED]
  Source/Mod.cs → Source/Settings/LatticeSettings.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Lattice UI features** — readme_icon_dock, readme_architect_window, readme_cinematic_main_menu, readme_bills_tab, readme_restyled_widgets [EXTRACTED 1.00]

## Communities (34 total, 4 thin omitted)

### Community 0 - "ModManager"
Cohesion: 0.07
Nodes (28): Category, ConcurrentDictionary, Func, GroupBy, Page_ModsConfig, SortBy, Action, Color (+20 more)

### Community 1 - ".DrawGradient"
Cohesion: 0.18
Nodes (6): Rect, Patch_BillConfigContents, Alert, Patch_AlertCard, Patch_GlobalControlsPanel, Patch_TitleBG

### Community 2 - "Backdrops"
Cohesion: 0.10
Nodes (19): Entry, GameComponent, IReadOnlyList, Slot, DateTime, List, Texture2D, Backdrops (+11 more)

### Community 3 - "ArchitectGrid"
Cohesion: 0.09
Nodes (26): ArchitectCategoryTab, MainTabWindow_Architect, Action, DesignationCategoryDef, Designator, FieldRef, List, MethodInfo (+18 more)

### Community 4 - ".DrawCatalog"
Cohesion: 0.20
Nodes (10): Bill, BillStack, Building_WorkTable, ITab_Bills, RecipeDef, MethodInfo, Rect, Vector2 (+2 more)

### Community 5 - "LatticeSettings"
Cohesion: 0.09
Nodes (22): Listing_Standard, Mod, ModSettings, Section, Patch_BillDoInterface, Rect, Patch_OptionBackground, Patch_PageTitle (+14 more)

### Community 6 - "WindowPins"
Cohesion: 0.20
Nodes (10): Flags, Dictionary, HashSet, Rect, Window, Flags, Patch_PinChip, Patch_PinnedAdd (+2 more)

### Community 7 - "SortBy"
Cohesion: 0.20
Nodes (9): FieldRef, Patch_BillsTabSize, SortBy, Author, Name, Size, Source, Status (+1 more)

### Community 8 - "Lattice"
Cohesion: 0.10
Nodes (27): Lattice, harmonylib, Pawn, rimworld, MainTabWindow, Patch_ArchitectPosition, Patch_DesignationTabOnGUI, Rect (+19 more)

### Community 9 - "Lattice (RimWorld 1.7 UI overhaul mod)"
Cohesion: 0.23
Nodes (12): Architect window, Colony screenshot backdrops, Bills tab, Build instructions, Cinematic main menu, Harmony 2.3.6, Centered icon dock, Lattice (RimWorld 1.7 UI overhaul mod) (+4 more)

### Community 10 - "Section"
Cohesion: 0.29
Nodes (7): Section, About, Appearance, Architect, MainMenu, Mods, Windows

### Community 11 - ".Prefix"
Cohesion: 0.25
Nodes (5): Color, Rect, Patch_DrawMenuSection, Patch_DrawWindowBackground, Patch_DrawWindowBackgroundTinted

### Community 12 - ".Draw"
Cohesion: 0.36
Nodes (6): Color, Rect, ButtonStyle, Patch_ButtonText, Patch_ButtonTextColored, TextAnchor

### Community 14 - "Category"
Cohesion: 0.20
Nodes (10): Category, All, Disabled, Enabled, Favourites, Issues, Local, Official (+2 more)

### Community 15 - "Dialog_LatticeName"
Cohesion: 0.28
Nodes (6): Action, Rect, Vector2, Dialog_LatticeName, InitialSize, Window

### Community 16 - "Patch_WindowsMovable"
Cohesion: 0.32
Nodes (5): MainTabWindow, Window, Patch_TabWindowsCentered, Patch_WindowsMovable, Type, bool&gt;

### Community 17 - ".Prefix"
Cohesion: 0.40
Nodes (3): MainButtonWorker, Rect, MainButtonStyle

### Community 18 - "GroupBy"
Cohesion: 0.33
Nodes (6): GroupBy, Author, None, Source, Tag, Version

### Community 19 - "VerMode"
Cohesion: 0.40
Nodes (5): VerMode, AtLeast, Below, Excludes, Supports

### Community 20 - ".MovedBy"
Cohesion: 0.50
Nodes (3): Rect, Vector2, RectExt

### Community 21 - "Lattice.csproj"
Cohesion: 0.50
Nodes (3): net10.0, Lib.Harmony (2.3.6), Microsoft.NET.Sdk

### Community 22 - "MainMenuStyle"
Cohesion: 0.14
Nodes (13): FileInfo, ListableOption, DateTime, FieldRef, List, Rect, Texture, MainMenuStyle (+5 more)

### Community 23 - ".Prefix"
Cohesion: 0.50
Nodes (3): MainButtonsRoot, FieldRef, Patch_Dock

### Community 24 - "UiStyle"
Cohesion: 0.15
Nodes (11): Dictionary, Texture2D, UiStyle, Accent, AccentGlow, Glow, Gradient, PanelBg (+3 more)

### Community 25 - ".WithAlpha"
Cohesion: 0.37
Nodes (4): Color, Rect, Texture, Vector2

### Community 26 - "Rect"
Cohesion: 0.40
Nodes (3): Rect, Patch_AltRect, Patch_LightHighlight

### Community 27 - ".Postfix"
Cohesion: 0.50
Nodes (3): Alert, Rect, Patch_AlertDrawAt

### Community 28 - "Patch_MenuLanguageButton"
Cohesion: 0.40
Nodes (3): MethodBase, IEnumerable, Patch_MenuLanguageButton

### Community 29 - ".Prefix"
Cohesion: 0.20
Nodes (6): ModMetaData, Rect, Patch_HighlightMouseover, Patch_HighlightSelected, Patch_ModInfo, Patch_ModRow

### Community 30 - ".Postfix"
Cohesion: 0.50
Nodes (3): Page, Vector2, Patch_ModsPageSize

## Knowledge Gaps
- **64 isolated node(s):** `CellSize`, `InfoWidth`, `Columns`, `net10.0`, `Lib.Harmony (2.3.6)` (+59 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 147 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModManager` connect `ModManager` to `SortBy`, `Lattice`, `Category`, `GroupBy`, `VerMode`, `.Postfix`?**
  _High betweenness centrality (0.264) - this node is a cross-community bridge._
- **Why does `LatticeSettings` connect `LatticeSettings` to `Lattice`, `.WithAlpha`, `Section`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **What connects `CellSize`, `InfoWidth`, `Columns` to the rest of the system?**
  _64 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ModManager` be split into smaller, more focused modules?**
  _Cohesion score 0.07410004675081813 - nodes in this community are weakly interconnected._
- **Should `Backdrops` be split into smaller, more focused modules?**
  _Cohesion score 0.0962566844919786 - nodes in this community are weakly interconnected._
- **Should `ArchitectGrid` be split into smaller, more focused modules?**
  _Cohesion score 0.09268292682926829 - nodes in this community are weakly interconnected._
- **Should `LatticeSettings` be split into smaller, more focused modules?**
  _Cohesion score 0.08880666049953746 - nodes in this community are weakly interconnected._