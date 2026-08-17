# IM UI Framework 2.0.0: Vanilla UI Asset Layer

Version 2.0.0 changes the framework's preferred UI source from "find an instantiated game object and clone it" to "load the original Unity `Resources` prefab first".

This is based on the shipped Idol Manager assets recovered from `Idol Manager/IM_Data` and the game's decompiled Modern UI Pack code. The game's `UIManager*` components themselves load `Resources.Load<UIManager>("MUIP Manager")`, so the same runtime resource mechanism is used here.

## Installed-game location vs. Unity resource path

In an installed copy of Idol Manager, these objects remain packed beneath `Idol Manager/IM_Data` in Unity `.assets` / `.resource` data. The `Resources/...` folders discussed in this document are the **logical paths reconstructed by AssetRipper**, not folders that IM UI Framework expects players to have on disk.

At runtime the framework deliberately does **not** open or parse `IM_Data` files. Unity already knows the resource index, so a logical exported path such as `Resources/scrollbar/Scrollbar.prefab` is requested as `Resources.Load<GameObject>("scrollbar/Scrollbar")`. This keeps the framework tied to the game's own loaded assets rather than to an AssetRipper export.

## What is exposed

`VanillaUiPrefabCatalog` contains every runtime-loadable UI prefab found in the exported `Resources` tree: **218 prefabs**.

| Family | Count |
|---|---:|
| Animated icons | 11 |
| Buttons | 150 |
| Context menus | 2 |
| Dropdowns | 4 |
| Horizontal selectors | 2 |
| Input fields | 7 |
| List views | 1 |
| Modal windows | 2 |
| Movable windows | 1 |
| Notifications | 3 |
| Other | 1 |
| Progress bars | 7 |
| Looping progress bars | 6 |
| Scrollbars | 1 |
| Sliders | 12 |
| Switches | 1 |
| Toggles | 5 |
| Tooltips | 1 |
| Window managers | 1 |

Every path is a `Resources.Load` path without the `Resources/` prefix or file extension. `VanillaUiPrefabCatalog.AllPrefabPaths` exposes the complete list at runtime.

## Generic prefab loading

```csharp
GameObject prefab = VanillaUiResources.LoadPrefab(
    VanillaUiPrefabCatalog.Scrollbar.Standard);

GameObject instance = VanillaUiResources.InstantiatePrefab(
    VanillaUiPrefabCatalog.ModalWindow.Style_1,
    parent,
    "MyModal");
```

The generic typed form exposes the component's complete public API through a configuration callback:

```csharp
GameObject obj;
SliderManager slider;
VanillaUiResources.TryInstantiatePrefab(
    VanillaUiPrefabCatalog.Slider.gradient_Slider_Gradient_Value,
    parent,
    "VolumeSlider",
    out obj,
    out slider,
    s =>
    {
        // Any public SliderManager setting can be changed here.
    },
    theme =>
    {
        // Every MUIP Manager setting is available here.
        theme.sliderColor = new Color(0.25f, 0.8f, 1f, 1f);
        theme.sliderBackgroundColor = new Color(1f, 1f, 1f, 0.12f);
        theme.sliderHandleColor = Color.white;
    });
```

Typed convenience methods are provided for scrollbar, all six MUIP button-manager families, animated icons, dropdown and multi-select dropdown, input fields, standard/range/radial sliders, switch, toggle, standard/filled/looping progress bars, list view, modal window, movable window, tooltip, context menu, horizontal selector, notification, and window manager. Every remaining prefab is still available through `TryInstantiatePrefab<T>` and the complete catalog.

## Button prefab manager families

The exported prefab scripts show six distinct MUIP manager families. Version 2.0.0 maps each family only to compatible resource variants:

| Manager | Vanilla resource families |
|---|---|
| `ButtonManager` | basic outline, basic outline gradient, rounded outline, rounded outline gradient |
| `ButtonManagerBasic` | basic, basic gradient, rounded, rounded gradient |
| `ButtonManagerBasicIcon` | basic-only-icon, radial-only-icon |
| `ButtonManagerBasicWithIcon` | basic-with-icon |
| `ButtonManagerIcon` | basic-outline-only-icon, radial-outline-only-icon |
| `ButtonManagerWithIcon` | basic-outline-with-icon |

`VanillaUiResources.GetButtonResourcePath(style, palette)` constructs the correct path. The shipped gradient families do not contain a `Standard` palette prefab, so requesting `Standard` for a gradient style returns `null` instead of inventing a nonexistent path.

## Complete MUIP settings

`VanillaUiResources.GetMuipManager()` returns the game's original `MUIP Manager` ScriptableObject.

Do **not** mutate that object for a one-off mod control, because it is the global game theme.

Use `VanillaUiThemeSettings.FromVanilla()` or `VanillaUiResources.CloneMuipManager()` instead. The exact shipped values for all fields are listed in [`MUIP Vanilla Defaults.md`](MUIP%20Vanilla%20Defaults.md). `VanillaUiThemeSettings` mirrors every public field in the shipped `Michsky.UI.ModernUIPack.UIManager`, including:

- animated icon color
- context menu background color
- every button theme/font/font-size/border/fill/text/icon color
- every dropdown theme/animation/font/font-size/item/icon color
- selector font, size, colors, invert animation, and loop selection
- input-field font, size, and color
- modal title/content fonts, theme, title/description/icon/background/content-panel colors
- notification fonts, sizes, theme, background/title/description/icon colors
- progress-bar font, size, bar/background/loop-background/label colors
- scrollbar handle and background colors
- slider font, size, theme, bar/background/label/popup-label/handle colors
- switch border/background/on-handle/off-handle colors
- toggle font, size, theme, text/border/background/check colors
- tooltip font, size, text color, and background color
- MUIP dynamic-update/editor flags

Example: real vanilla scrollbar with a per-instance color variant:

```csharp
GameObject obj;
Scrollbar scrollbar;
VanillaUiResources.TryCreateScrollbar(
    parent,
    "MyScrollbar",
    out obj,
    out scrollbar,
    sb =>
    {
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.size = 0.35f;
        sb.numberOfSteps = 0;
    },
    theme =>
    {
        theme.scrollbarColor = new Color(0.35f, 0.8f, 1f, 1f);
        theme.scrollbarBackgroundColor = new Color(0.35f, 0.8f, 1f, 0.15f);
    });
```

The framework gives that instance its own cloned `UIManager` asset and assigns it to every `UIManager*` component below the prefab. The game-global `MUIP Manager` is not changed. The runtime theme is destroyed with the instantiated object.

## The vanilla scrollbar

The shipped prefab is loaded from:

```text
Resources/scrollbar/Scrollbar.prefab
```

Runtime path:

```csharp
VanillaUiPrefabCatalog.Scrollbar.Standard
// "scrollbar/Scrollbar"
```

Its actual hierarchy is:

```text
Scrollbar
├── Background
└── Sliding Area
    └── Handle
```

The root carries Unity's `Scrollbar` plus `UIManagerScrollbar`. The stock size is 20 x 250 and the stock direction is `BottomToTop`. The background and handle are separate sliced images controlled by `MUIP Manager.scrollbarBackgroundColor` and `MUIP Manager.scrollbarColor`.

`IMUiKit.TryCreateStyledScrollView` now uses this real prefab first. The old popup-search route remains only as compatibility fallback. Its last-resort hand-built scrollbar now follows the real hierarchy rather than parenting the handle directly under the root.

`IMUiKit.ApplyVanillaScrollDefaults` now mirrors `ScrollRectDefault` from the decompiled game code: movement is `Clamped`; Windows/non-macOS sensitivity is **25**; macOS sensitivity is **3** with deceleration rate **0.05**.

## Fonts

Idol Manager has two relevant font paths.

### Legacy `UnityEngine.UI.Text`

The decompiled `Fonts` component owns `FontFiles` and chooses `SelectedFont`. `Fonts.LoadFont()` reads the active mod `info.json` `Font` field, searches bundled `FontFiles`, then searches `Font.GetOSInstalledFontNames()` and creates an OS dynamic font. `Font_Replacer` updates legacy Text components from that selection.

IM UI Framework exposes this through:

```csharp
Font current = VanillaUiFonts.GetGameSelectedLegacyFont();
IList<Font> bundled = VanillaUiFonts.GetGameBundledLegacyFonts();
IList<Font> loaded = VanillaUiFonts.GetLoadedLegacyFonts();
Font named = VanillaUiFonts.FindLoadedLegacyFont("OpenSans");
Font externalOrOs = VanillaUiFonts.LoadExternalOrOsLegacyFont(pathOrFontName, 18);
```

`IMUiKit.CreateLegacyText` has a new overload accepting an explicit `Font` and a `followGameFont` flag. When `followGameFont` is true, the framework adds the game's `Font_Replacer` so later language/font resets follow vanilla behavior.

### TextMesh Pro / MUIP

MUIP uses `TMP_FontAsset` references stored directly on `MUIP Manager`. `VanillaUiFonts.GetMuipFont(role)` exposes the font used for each MUIP role, including button, dropdown, selector, input, modal, notification, progress, slider, toggle, and tooltip roles.

Also available:

```csharp
TMP_FontAsset font = VanillaUiFonts.GetMuipFont(VanillaMuipFontRole.Button);
TMP_FontAsset liberation = VanillaUiFonts.GetLiberationSansSdf();
IList<TMP_FontAsset> loaded = VanillaUiFonts.GetLoadedTmpFonts();
TMP_FontAsset found = VanillaUiFonts.FindLoadedTmpFont("Open Sans");
TMP_FontAsset external = VanillaUiFonts.LoadExternalOrOsTmpFont(pathOrFontName, 32);
```

The external TMP helper first resolves a legacy dynamic font, then invokes the runtime TextMesh Pro `TMP_FontAsset.CreateFontAsset` API through reflection so the framework is not tied to one overload signature.

`IMUiKit.CreateText` has an overload accepting an explicit `TMP_FontAsset`. In 2.0.3 its normal default follows the game's active `Fonts.GetFont()` selection through `VanillaUiFonts.GetGameSelectedTmpFont()`: a loaded matching TMP asset is preferred, an OS/dynamic legacy font can be converted to a runtime TMP asset, then the game Fonts/MUIP/Liberation fallbacks are used only if no selected-font equivalent can be resolved.

The game's language system also manages `TMP_Settings.fallbackFontAssets`; IM UI Framework does not rewrite the global fallback list when merely choosing a font for one mod control.

## Compatibility strategy

Resource source order in 2.0.0 is:

1. the genuine `Resources` prefab from the shipped game
2. known popup/runtime template lookup where an actual Resources prefab is unavailable
3. faithful framework reconstruction
4. simple generic fallback only as a last resort

Existing 1.x APIs remain available. `IMUiBridges.TryCloneModernControl<T>` now silently gains the Resources-first behavior for known MUIP component types. An overload accepting an explicit resource path, component callback, and theme callback is available when a specific prefab variant is required.

## Why `GameObject/*.prefab` from AssetRipper is not in this catalog

AssetRipper can reconstruct prefab-like files for serialized scene GameObjects. Those exported paths do not prove that the original build registered them under Unity `Resources`, so `Resources.Load("that reconstructed path")` is not safe to assume. Version 2.0.0 deliberately catalogs only the prefab files proven to live in the exported `Resources` tree. Scene/popup cloning remains the fallback for non-Resources UI.


### Vanilla rounded-corner shader

Idol Manager ships `UI/RoundedCorners/RoundedCorners` and uses `Nobi.UiRoundedCorners.ImageWithRoundedCorners` on game UI with small radii such as 6 and 8 in the exported assets. `IMUiKit.TryApplyVanillaRoundedCorners` uses that same shader, keeps an instance-local material, and updates the shader's `_WidthHeightRadius` vector when RectTransform dimensions change. This avoids using highly-rounded button sprites as generic panel backgrounds.
