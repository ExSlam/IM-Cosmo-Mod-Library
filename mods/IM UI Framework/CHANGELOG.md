# Changelog

## 3.1.1

- Added optional per-scroll-view producer-list Slider right inset and viewport gutter values while preserving the vanilla constants as defaults.
- Added month-pager horizontal label padding so localized labels can be sized without changing the arrow control geometry.
- Added `IMUiPrimitives.TryCopyRoundedWhiteVisual` for reusing Idol Manager's shipped `button/rounded/White` image on compact badges and custom surfaces.
- Kept the old producer-slider creation overload for source compatibility.

## 3.1.0

- Added `IMUiCompat`, a centralized compatibility layer for Idol Manager's older Unity API assemblies.
- Replaced `SceneManager.GetActiveScene()`, `Scene.IsValid()`, `Scene.GetRootGameObjects()`, and `Scene.name` dependencies with inactive-safe loaded-scene discovery based on reflection and loaded object scene identity.
- Replaced unsupported `GetComponentInChildren<T>(true)` calls with an include-inactive compatibility helper built on the supported plural overload.
- Reworked safe persistent UnityEvent inspection to use reflection and the serialized `m_PersistentCalls -> m_Calls -> m_Target` graph when `UnityEventBase.GetPersistentEventCount/GetPersistentTarget` are unavailable. This preserves internal vanilla wiring such as the producer-list ScrollRect/Slider pair without compiling against newer UnityEvent APIs.
- Removed the compile-time dependency on `TMPro.TMP_Dropdown`, which Idol Manager's TextMeshPro assembly does not define; TMP dropdowns are detected by runtime type name when present.
- Reworked v3 semantic `ColorBlock` theming to mutate the serialized backing fields by reflection, matching the compatibility strategy already used elsewhere in the framework for Idol Manager's read-only normal/pressed/selected/disabled color properties.
- No public v3 custom-UI API was removed or renamed.

## 3.0.0

- Reframed v3 around **composable custom UI** rather than whole-popup cloning. Exact popup/scene cloning remains available, but custom mods can now borrow individual vanilla pieces and freely choose their own layout, size, text, events and colors.
- Added `IMUiTheme`, semantic `IMUiColorRole`s, and `IMUiThemePreset`s. Vanilla is now a preset rather than a hard-coded palette.
- Added `IMUiVanillaColors` exposing every named `mainScript` Color32 preset: white, red/light red, green/light green, blue/light blue/dark blue, pink, grey, black/black-gold, gold/dark gold, active/inactive tab blue, and transparent.
- Added `IMUiPrimitives` for vanilla-sprite surfaces/cards, text, dividers, grids, and buttons without copying a whole source popup.
- Added `IMUiElementBuilder` for fluent cloning of any individual popup child, arbitrary scene object, exact `Resources` prefab, or Modern UI Pack control, with per-instance text/size/font/callback/theme overrides and post-theme per-child `GraphicColor(...)` overrides.
- Added `IMUiComposer.TryCreateMonthPager`, using the literal Singles-chart previous/next month controls (`U+F33A` / `U+F33B`) and month label. The icon-capable source font is intentionally preserved so the private-use arrow glyphs cannot become missing-character boxes.
- Added `IMUiComposer.TryCreateScrollView` and `TryAttachVanillaListScrollIndicator` around the exact Contracts/Salaries/Loans `Slider` scrollbar surrogate. Vanilla geometry remains fixed; custom accents use a neutral shipped rounded sprite for the thumb so baked purple pixels do not contaminate custom colors.
- Added semantic-theme bridging for every Modern UI Pack control family through `VanillaUiControlFactory`, including buttons, inputs, dropdowns, selectors, switches, toggles, sliders, range/radial sliders, progress bars, lists, modal/movable windows, notifications, tooltips, context menus and window managers. Exact variants remain available through all 218 `VanillaUiPrefabCatalog` paths.
- Added role-aware theming for Selectables, inputs/carets/selections, sliders, scrollbars, toggles, structural surfaces and text while avoiding blanket portrait/icon tinting.
- Added vanilla-style calendar/card cell helpers that reuse the Singles-chart sliced panel visual instead of spreadsheet-like `Outline` borders.
- Kept the exhaustive inactive-safe scene index, all popup template descriptors, and serialized `prefab_*` reference access from the earlier v3 work as lower-level source layers.
- Added short-form primitive/composer overloads (`CreatePanel`, `CreateCard`, `CreateLabel`, simple themed buttons, month pager, scroll view, popup shell) so basic custom UI does not need an options object for every element.
- Added scene-derived neutral color presets (`PanelOuter`, `PanelInner`, `TrackGrey`, `ChartArrowHover`) alongside the complete `mainScript` palette and arbitrary `Color32` overrides.
- Added `V3 Composable Custom UI.md` and `V3 Custom Component Coverage.md`, and rewrote migration guidance so the recommended path is now "compose vanilla pieces" instead of "clone the closest popup".

## 2.1.0

- Added a scene-template layer alongside the existing Michsky Modern UI Pack Resources layer. `VanillaUiSceneTemplates` can resolve and clone inactive popup children through `PopupManager` without opening the source popup.
- Added `TryCreateProducerListScrollSlider`, reproducing the exact list-scroll pattern serialized in `main.unity` for Producer Contracts, Producer Salaries, and Producer Loans. These lists use a separate vertical `Slider` with a fixed circular handle, not `ScrollRect.verticalScrollbar`.
- Framework-created styled scroll views now prefer the producer-list Slider pattern in the gameplay scene, preserving the game's fixed round thumb and thin grey track. If scene templates are unavailable, the framework still falls back to the genuine `Resources/scrollbar/Scrollbar` Modern UI Pack prefab.
- Added scene-derived producer-list geometry constants and automatic right-side viewport reservation.

## 2.0.3

- Fixed the framework TMP default so `IMUiKit.CreateText` follows Idol Manager's currently selected game font instead of defaulting to the MUIP button font. Bundled fonts are matched against loaded TMP assets; OS/external fonts are converted through the runtime TMP bridge and cached per selected legacy font.
- Added `VanillaUiFonts.GetGameSelectedTmpFont`, `VanillaUiFonts.ApplyGameFont`, and `IMUiKit.ApplyGameFont` so cloned prefabs and dynamically-created UI can consistently follow the active game font.
- Made framework-created/cloned button labels use the selected game font by default while retaining explicit `SetDefaultTmpFont`/`SetDefaultLegacyFont` overrides.
- Added `IMUiKit.TryApplyVanillaRoundedCorners`, which uses Idol Manager's shipped `UI/RoundedCorners/RoundedCorners` shader with an instance-local material and live RectTransform size updates. This gives mods the same small-radius corner system as vanilla UI without a compile-time dependency on `Nobi.UiRoundedCorners.dll`.
- Added a base `TMP_Text` font-apply overload so MUIP controls do not require `TextMeshProUGUI` casts.

## 2.0.2

- Fixed compatibility with Idol Manager's older Unity API surface after a real local `dotnet build` pass.
- Replaced compile-time calls to the unavailable generic `Resources.FindObjectsOfTypeAll<T>()` overload with a reflection-backed compatibility enumerator and safe `Object.FindObjectsOfType(Type)` fallback.
- Added the missing `UnityEngine.TextCoreModule.dll` project reference from the parent library's shared `dll` directory so TMP `FaceInfo` access compiles correctly.
- Fixed two stale multi-select dropdown references to use `Dropdown.Dropdown_Multi_Select`.
- Reworked the emergency scrollbar ColorBlock setup for Idol Manager's UnityEngine.UI build, where normal/pressed/selected/disabled colors are read-only properties, while retaining the vanilla serialized color values via reflection.

## 2.0.1

- Fixed CS0542 compile failures caused by constants sharing their enclosing catalog type names.
- Renamed the affected constants to `Dropdown.Standard`, `Scrollbar.Standard`, and `Tooltip.Standard`.
- Updated all framework call sites and documentation references.
- No vanilla resource paths or prefab behavior changed.

## 2.0.0

- Added the vanilla Resources prefab layer, MUIP theme bridge, complete resource catalog, and expanded font support.
