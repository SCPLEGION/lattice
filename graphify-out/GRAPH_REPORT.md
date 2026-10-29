# Graph Report - rimworld-ui-rework  (2026-10-29)

## Corpus Check
- 27 files · ~17,916 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 4 file(s) not represented in the graph (top: (none) 3, .xml 1)

## Summary
- 488 nodes · 1112 edges · 32 communities (30 shown, 2 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 6 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `fee8a207`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- ModManager
- .WithAlpha
- Backdrops
- ArchitectGrid
- .DrawCatalog
- LatticeSettings
- WindowPins
- MainMenuStyle.cs
- unityengine
- Lattice (RimWorld 1.7 UI overhaul mod)
- ArchitectWindow.cs
- Lattice
- .Draw
- .Chip
- Category
- Dialog_LatticeName
- .IsResearch
- ColonistBarStyle.cs
- GroupBy
- VerMode
- RectExt.cs
- Lattice.csproj
- MainMenuStyle
- .Ease
- UiStyle
- .IconCell
- TabsStyle.cs
- ModManager.cs
- Patch_MenuLanguageButton
- ModsStyle.cs
- .Postfix
- Patch_MainMenuControls

## God Nodes (most connected - your core abstractions)
1. `ModManager` - 71 edges
2. `UiStyle` - 27 edges
3. `Lattice` - 25 edges
4. `ModStore` - 20 edges
5. `ArchitectGrid` - 19 edges
6. `MainMenuStyle` - 19 edges
7. `Backdrops` - 15 edges
8. `LatticeSettings` - 14 edges
9. `WindowPins` - 11 edges
10. `Dialog_LatticeName` - 10 edges

## Surprising Connections (you probably didn't know these)
- `LatticeMod` --references--> `LatticeSettings`  [EXTRACTED]
  Source/Mod.cs → Source/Settings/LatticeSettings.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Lattice UI features** — readme_icon_dock, readme_architect_window, readme_cinematic_main_menu, readme_bills_tab, readme_restyled_widgets [EXTRACTED 1.00]

## Communities (32 total, 2 thin omitted)

### Community 0 - "ModManager"
Cohesion: 0.08
Nodes (28): Category, ConcurrentDictionary, Func, GroupBy, Page_ModsConfig, SortBy, Action, Color (+20 more)

### Community 1 - ".WithAlpha"
Cohesion: 0.12
Nodes (9): MainButtonsRoot, Rect, Patch_BillConfigContents, ModMetaData, Rect, Alert, Patch_AlertCard, Color (+1 more)

### Community 2 - "Backdrops"
Cohesion: 0.10
Nodes (19): Entry, GameComponent, IReadOnlyList, Slot, DateTime, List, Texture2D, Backdrops (+11 more)

### Community 3 - "ArchitectGrid"
Cohesion: 0.09
Nodes (26): ArchitectCategoryTab, MainTabWindow_Architect, Action, DesignationCategoryDef, Designator, FieldRef, List, MethodInfo (+18 more)

### Community 4 - ".DrawCatalog"
Cohesion: 0.11
Nodes (19): Bill, BillStack, Building_WorkTable, ITab_Bills, RecipeDef, FieldRef, MethodInfo, Rect (+11 more)

### Community 5 - "LatticeSettings"
Cohesion: 0.15
Nodes (12): Listing_Standard, ModSettings, Rect, Color, Rect, Vector2, BackdropMode, Custom (+4 more)

### Community 6 - "WindowPins"
Cohesion: 0.20
Nodes (10): Flags, Dictionary, HashSet, Rect, Window, Flags, Patch_PinChip, Patch_PinnedAdd (+2 more)

### Community 7 - "MainMenuStyle.cs"
Cohesion: 0.22
Nodes (7): Patch_SaveGameBackdrop, system, system_collections_concurrent, system_collections_generic, system_io, system_threading, verse_steam

### Community 8 - "unityengine"
Cohesion: 0.15
Nodes (15): harmonylib, rimworld, Alert, Rect, Patch_AlertDrawAt, Patch_BillDoInterface, FieldRef, Patch_Dock (+7 more)

### Community 9 - "Lattice (RimWorld 1.7 UI overhaul mod)"
Cohesion: 0.23
Nodes (12): Architect window, Colony screenshot backdrops, Bills tab, Build instructions, Cinematic main menu, Harmony 2.3.6, Centered icon dock, Lattice (RimWorld 1.7 UI overhaul mod) (+4 more)

### Community 10 - "ArchitectWindow.cs"
Cohesion: 0.18
Nodes (7): MainTabWindow, Patch_ArchitectPosition, Patch_DesignationTabOnGUI, Patch_WindowsMovable, system_linq, Type, bool&gt;, verse_sound

### Community 11 - "Lattice"
Cohesion: 0.22
Nodes (6): Lattice, Mod, LatticeMod, Patch_DrawMenuSection, Patch_DrawWindowBackground, Patch_DrawWindowBackgroundTinted

### Community 12 - ".Draw"
Cohesion: 0.36
Nodes (6): Color, Rect, ButtonStyle, Patch_ButtonText, Patch_ButtonTextColored, TextAnchor

### Community 14 - "Category"
Cohesion: 0.22
Nodes (9): Category, All, Disabled, Enabled, Favourites, Issues, Local, Official (+1 more)

### Community 15 - "Dialog_LatticeName"
Cohesion: 0.28
Nodes (6): Action, Rect, Vector2, Dialog_LatticeName, InitialSize, Window

### Community 16 - ".IsResearch"
Cohesion: 0.40
Nodes (3): MainTabWindow, Window, Patch_TabWindowsCentered

### Community 17 - "ColonistBarStyle.cs"
Cohesion: 0.33
Nodes (4): Pawn, Rect, Patch_DrawColonist, Patch_DrawSelectionOverlay

### Community 18 - "GroupBy"
Cohesion: 0.33
Nodes (6): GroupBy, Author, None, Source, Tag, Version

### Community 19 - "VerMode"
Cohesion: 0.40
Nodes (5): VerMode, AtLeast, Below, Excludes, Supports

### Community 20 - "RectExt.cs"
Cohesion: 0.40
Nodes (3): Rect, Vector2, RectExt

### Community 21 - "Lattice.csproj"
Cohesion: 0.50
Nodes (3): net10.0, Lib.Harmony (2.3.6), Microsoft.NET.Sdk

### Community 22 - "MainMenuStyle"
Cohesion: 0.20
Nodes (8): FileInfo, DateTime, Texture, MainMenuStyle, On, TexLudeon, TexTitle, Patch_MainMenuOnGUI

### Community 23 - ".Ease"
Cohesion: 0.17
Nodes (10): ListableOption, FieldRef, List, Rect, Patch_OptionListing, Rect, Patch_PageTitle, FieldRef (+2 more)

### Community 24 - "UiStyle"
Cohesion: 0.15
Nodes (11): Dictionary, Texture2D, UiStyle, Accent, AccentGlow, Glow, Gradient, PanelBg (+3 more)

### Community 25 - ".IconCell"
Cohesion: 0.27
Nodes (6): MainButtonWorker, Rect, Color, Rect, Texture, Vector2

### Community 26 - "TabsStyle.cs"
Cohesion: 0.32
Nodes (4): Rect, Patch_AltRect, Patch_LightHighlight, Patch_TitleBG

### Community 27 - "ModManager.cs"
Cohesion: 0.33
Nodes (3): Patch_ModsPageClose, Patch_ModsPageContents, Patch_ModsPageOpen

### Community 28 - "Patch_MenuLanguageButton"
Cohesion: 0.40
Nodes (3): MethodBase, IEnumerable, Patch_MenuLanguageButton

### Community 29 - "ModsStyle.cs"
Cohesion: 0.40
Nodes (4): Patch_HighlightMouseover, Patch_HighlightSelected, Patch_ModInfo, Patch_ModRow

### Community 30 - ".Postfix"
Cohesion: 0.50
Nodes (3): Page, Vector2, Patch_ModsPageSize

## Knowledge Gaps
- **55 isolated node(s):** `CellSize`, `InfoWidth`, `Columns`, `net10.0`, `Lib.Harmony (2.3.6)` (+50 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 135 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModManager` connect `ModManager` to `.WithAlpha`, `.DrawCatalog`, `Category`, `GroupBy`, `VerMode`, `ModManager.cs`, `.Postfix`?**
  _High betweenness centrality (0.282) - this node is a cross-community bridge._
- **Why does `Lattice` connect `Lattice` to `LatticeSettings`, `WindowPins`, `MainMenuStyle.cs`, `unityengine`, `ArchitectWindow.cs`, `.Draw`, `ColonistBarStyle.cs`, `RectExt.cs`, `TabsStyle.cs`, `ModManager.cs`, `ModsStyle.cs`?**
  _High betweenness centrality (0.106) - this node is a cross-community bridge._
- **Why does `UiStyle` connect `UiStyle` to `.IconCell`, `.Ease`, `.WithAlpha`, `MainMenuStyle.cs`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **What connects `CellSize`, `InfoWidth`, `Columns` to the rest of the system?**
  _55 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ModManager` be split into smaller, more focused modules?**
  _Cohesion score 0.07523510971786834 - nodes in this community are weakly interconnected._
- **Should `.WithAlpha` be split into smaller, more focused modules?**
  _Cohesion score 0.12433862433862433 - nodes in this community are weakly interconnected._
- **Should `Backdrops` be split into smaller, more focused modules?**
  _Cohesion score 0.0962566844919786 - nodes in this community are weakly interconnected._