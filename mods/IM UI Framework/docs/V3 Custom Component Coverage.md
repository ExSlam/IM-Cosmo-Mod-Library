# V3 Custom Component Coverage

The v3 custom-UI API is deliberately not limited to a whitelist of a few cloned popups. It combines four source classes, so a mod can reuse vanilla appearance/behavior while owning the layout and colors.

## Custom primitives

`IMUiPrimitives` supplies custom-layout building blocks:

- outer/inner/raised/muted/accent/transparent surfaces
- cards
- game-font body, secondary, muted, title, on-accent, success, danger, warning and gold text
- dividers
- fixed-column grids
- buttons using either scene-native game controls or shipped Modern UI Pack shapes

These pieces use semantic `IMUiTheme` roles. Vanilla values are presets and every role can be replaced with an arbitrary `Color32`.

## Scene-native patterns currently promoted to named helpers

These are game-specific composites whose important behavior is serialized in `main.unity`, so they are better represented by an exact scene template than by a generic Unity control:

- Singles chart previous-month arrow
- Singles chart next-month arrow
- Singles chart month label
- Singles chart panel/card sliced visual
- Contracts/Salaries/Loans fixed circular vertical list indicator
- complete custom ScrollRect + content + producer-list indicator
- month/year pager assembled from the exact Singles chart controls

Named helpers are conveniences, not a coverage limit. `IMUiElementBuilder.FromPopup(...)` and `.FromScene(...)` can use any other individual scene UI object, including inactive objects.

## Shipped Resources UI

The supplied game assets contain 218 UI prefabs under `Resources`, and `VanillaUiPrefabCatalog` contains 218 matching runtime paths:

| Resource family | Prefabs |
| --- | ---: |
| Animated icon | 11 |
| Button | 150 |
| Context menu | 2 |
| Dropdown | 4 |
| Horizontal selector | 2 |
| Input field | 7 |
| List view | 1 |
| Modal window | 2 |
| Movable window | 1 |
| Notification | 3 |
| Other/canvas | 1 |
| Progress bar | 7 |
| Loop progress bar | 6 |
| Scrollbar | 1 |
| Slider | 12 |
| Switch | 1 |
| Toggle | 5 |
| Tooltip | 1 |
| Window manager | 1 |
| **Total** | **218** |

`VanillaControlType` gives friendly defaults for all of those control families, while `VanillaUiPrefabCatalog` exposes every exact variant. The v3 semantic theme bridge applies custom colors/fonts to the Modern UI Pack manager settings before activation so the prefab does not immediately restore its original theme.

## Scene UI and serialized item references

The supplied scene export contains 9,651 gameplay RectTransforms and 508 main-menu RectTransforms. `VanillaUiSceneCatalog` can resolve any of them by hierarchy path/name and occurrence index, including inactive descendants.

The decompiled game source also contains 226 public `GameObject` fields named `prefab*` across 90 source files. `VanillaUiReferenceTemplates` resolves those live serialized references instead of pretending AssetRipper filenames are `Resources.Load` paths. This covers repeated vanilla rows/cards/items that are serialized on controllers rather than stored under Resources.

## Popup roots

All 60 `PopupManager._type` enum values are cataloged. The supplied scenes materialize 58 popup roots; `show_release` and `main_menu_load` have no serialized root in the supplied scenes and are explicitly recorded as such. Whole-popup cloning remains available for fidelity/testing, but custom UI should usually compose smaller pieces instead.

## Why this is enough for new custom UI

A custom mod can therefore choose among:

1. a v3 primitive when it only needs vanilla visual grammar,
2. a named game-specific pattern when behavior/geometry is subtle,
3. any of the 218 shipped Resources prefabs,
4. any individual loaded scene UI object,
5. any serialized vanilla `prefab_*` row/item reference,
6. or, as a last resort, an exact whole popup template.

The source is independent from the theme. That is what allows a pink or green custom calendar, ledger, chart, settings screen, etc. to retain vanilla sprites, geometry and interaction behavior instead of becoming a recolored screenshot of an existing popup.

## Coverage

- `PopupManager._type`: **60 / 60** enum entries represented.
- Materialized popup roots in the supplied scenes: **58 / 60**; `show_release` and `main_menu_load` are explicitly recorded as unmaterialized rather than given invented templates.
- Gameplay `main.unity`: **9,651** RectTransforms indexed.
- `Main Menu.unity`: **508** RectTransforms indexed.
- Runtime-loadable UI prefabs under exported `Resources`: **218**, matching `VanillaUiPrefabCatalog.AllPrefabPaths` with no missing or extra catalog paths in the audited asset set.
- Decompiled public `GameObject` fields named `prefab*`: **226** across **90** source files, handled through serialized-reference resolution rather than assumed `Resources.Load` paths.

Duplicate hierarchy paths are retained through occurrence-indexed lookup, and inactive descendants are included. Producer Contracts, Salaries, and Loans were also verified to use the same separate vertical `Slider`/`SliderDefault` list-scroll pattern rather than `ScrollRect.verticalScrollbar`.

These counts describe the supplied Idol Manager source/assets used to build the catalogs.
