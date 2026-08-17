# IM UI Framework 3.0.0 Coverage Audit

This audit was produced from the supplied decompiled Idol Manager source, `IM_Scenes` export, and `IM some assets` export.

## Popup coverage

- `PopupManager._type` enum entries represented: **60 / 60**
- Materialized serialized popup roots found in the supplied scenes: **58 / 60**
- Cataloged materialized paths missing from the supplied scene YAML: **0**
- Explicitly unmaterialized enum slots: `show_release` and `main_menu_load`
- Shared popup types that have both gameplay and main-menu roots are supported through scene-local resolution.

V3 does not invent fake roots for the two enum values that are not serialized in either supplied scene.

## Scene UI coverage

- Gameplay `main.unity` RectTransforms: **9,651**
- Main-menu `Main Menu.unity` RectTransforms: **508**
- Duplicate gameplay hierarchy-path strings are retained through occurrence-indexed lookup rather than collapsed.
- Inactive descendants are indexed from `Scene.GetRootGameObjects()`, so the source UI does not need to be opened first.

## Runtime Resource UI coverage

- UI prefabs under exported `Resources`: **218**
- Entries in `VanillaUiPrefabCatalog.AllPrefabPaths`: **218**
- Missing catalog paths: **0**
- Extra catalog paths: **0**

Resource families include buttons, dropdowns, input fields, sliders, range/radial sliders, scrollbars, switches, toggles, list views, progress bars, modal/movable windows, notifications, context menus, selectors, tooltips, animated icons, canvas, and window manager controls.

## Serialized-reference UI coverage

The decompiled source contains **226 public `GameObject` fields named with `prefab` across 90 source files**. These are not assumed to be `Resources` assets. V3 adds a runtime resolver that reads serialized `GameObject`/`Component` fields and serialized arrays/lists from the live scene component and exposes UI-valued references as cloneable templates.

This closes the important gap represented by AssetRipper's exported `GameObject/*.prefab` files: those filenames alone are not proof of runtime `Resources.Load` paths.

## Vanilla list scrolling

Producer Contracts, Salaries, and Loans use the same scene-serialized list-scroll pattern:

- separate vertical `Slider`, not `ScrollRect.verticalScrollbar`
- fixed circular handle
- thin grey track
- `SliderDefault` synchronization with `ScrollRect.verticalNormalizedPosition`

V3 preserves that internal UI event wiring in `Template` mode.

## Build validation available in this environment

No C#/.NET compiler (`dotnet`, `msbuild`, `csc`, or `mcs`) is installed in the execution environment, so no DLL was produced. Source validation performed here includes:

- project compile-item/source-file set equality
- lexical C# brace balancing across every source file
- complete 218 Resource-path set comparison
- complete popup enum/catalog comparison
- every materialized popup hierarchy path checked against supplied scene YAML

A real local build remains the final compile-time verification step.
