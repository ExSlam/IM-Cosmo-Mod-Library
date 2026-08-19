# IM UI Framework 3.1.1 (Intermediary Mod)

`IM UI Framework` is a reusable helper layer for Idol Manager modders.

It targets three common problems:
- Modifying existing UI safely.
- Adding new buttons that match game style.
- Building fully custom popups that still look and behave like base game UI.




## 3.1.1: localized pager and scroll-geometry controls

3.1.1 keeps every 3.1.0 default intact but lets a custom screen opt into a different producer-list indicator inset/gutter, add explicit horizontal padding around month-pager labels, and copy the shipped rounded-white button visual onto compact custom badges. These are per-instance composition options rather than global scene changes.

## 3.1.0: Idol Manager Unity compatibility pass

Version 3.1 keeps the v3 composable/custom-UI API intact while making the implementation compile against Idol Manager's older Unity API surface. Scene discovery, inactive-child lookup, UnityEvent inspection, TMP dropdown detection, and ColorBlock mutation now go through compatibility shims instead of relying on newer Unity convenience members.

## 3.0.0: composable vanilla UI, not just popup cloning

Version 3's main job is to let a mod build **new UI that looks like Idol Manager**, while still choosing its own layout, content, dimensions and colors. Whole-popup cloning remains available as a low-level fidelity tool, but it is no longer the recommended starting point for custom interfaces.

The v3 stack is deliberately split into **source**, **style**, **primitive**, and **pattern** layers:

- `IMUiTheme` is a semantic palette. Vanilla colors are presets, not hard requirements. Start from `IMUiTheme.Vanilla()` or `IMUiTheme.FromPreset(...)`, then replace only the roles you want, such as `Accent`, `SurfaceRaised`, `Title`, or `ScrollTrack`.
- `IMUiVanillaColors` exposes every named `mainScript` color (`Blue`, `Pink`, `Green`, `Gold`, `Red`, `LightBlue`, `GreyLight`, etc.) plus scene-derived neutrals such as `PanelOuter`, `PanelInner`, `TrackGrey`, and the exact Singles-chart arrow hover color.
- `IMUiPrimitives` creates custom panels, cards, text, dividers, grids and buttons while borrowing the game's actual sprites/fonts/control prefabs. It does not force a copied vanilla layout.
  Common cases have short-form overloads (`CreateCard`, `CreatePanel`, `CreateLabel`, `TryCreateButton`) so mods do not need an options object for every small element.
- `IMUiElementBuilder` can borrow **any individual scene object** or **any Resources prefab**, then override text, size, callback and theme. This is the universal escape hatch when a dedicated helper does not exist.
- `IMUiComposer` builds recurring Idol Manager UI patterns with very little setup: the exact Singles-chart month navigator, the Contracts/Salaries/Loans fixed round list-scroll indicator, custom themed scroll views, vanilla card grids, and custom popup shells.
- `VanillaUiControlFactory` covers every shipped Modern UI Pack control family. `VanillaUiPrefabCatalog` still exposes all 218 exact prefab variants, including all button variants, inputs, dropdowns, selectors, toggles, switches, sliders, progress bars, modal/movable windows, list views, tooltips, notifications and context menus.
- `VanillaUiSceneCatalog`, popup templates, and serialized-reference templates remain available when a mod really does need a literal scene hierarchy, a vanilla repeated row/item prefab, or an exact popup reference.

### Exact Singles-chart month arrows

`IMUiComposer.TryCreateMonthPager(...)` clones the actual `Single_Chart/Panel/Prev Month`, `Month`, and `Next Month` controls from the inactive gameplay scene. The arrows retain the game's private-use TMP glyphs (`U+F33A` / `U+F33B`), exact `Button.ColorBlock`, dimensions, click/hover behavior and icon-capable source font. The source Singles chart never has to be opened.

```csharp
IMUiTheme theme = IMUiTheme.Vanilla().Clone()
    .WithAccent(IMUiVanillaColorPreset.Pink)
    .SetColor(IMUiColorRole.SurfaceRaised, new Color32(248, 244, 250, 255));

IMUiMonthPagerOptions pagerOptions = new IMUiMonthPagerOptions();
pagerOptions.Label = "August 2026";
pagerOptions.Theme = theme;
pagerOptions.OnPrevious = PreviousMonth;
pagerOptions.OnNext = NextMonth;

IMUiMonthPagerHandle pager;
IMUiComposer.TryCreateMonthPager(parent, pagerOptions, out pager);
```

With `Theme = null`, the component remains literally vanilla. With a theme, its geometry, glyphs and interaction structure stay vanilla while semantic colors change.

### Exact vanilla list scrolling in a custom layout

Producer Contracts, Salaries and Loans do **not** use `ScrollRect.verticalScrollbar`. They use a separate vertical `Slider` with an 11.14-unit circular handle and two-way normalized-position binding. V3 exposes that pattern directly:

```csharp
IMUiScrollViewOptions options = new IMUiScrollViewOptions();
options.Theme = theme;
options.OffsetMin = new Vector2(16f, 56f);
options.OffsetMax = new Vector2(-32f, -92f);
// Optional. Defaults remain the exact vanilla producer-list geometry.
options.VanillaIndicatorRightCenterInset = 7f;
options.VanillaViewportRightInset = 22f;

IMUiScrollViewHandle list;
IMUiComposer.TryCreateScrollView(panel, options, out list);
```

If a mod supplies a custom accent, v3 keeps the exact producer-list geometry but switches the baked-purple thumb to the game's neutral rounded UI sprite before tinting, avoiding muddy multiplied colors. With no custom theme, the original scene thumb remains untouched.

### Any individual vanilla piece, customized

```csharp
IMUiElementHandle previous;
IMUiPresets.PreviousMonthButton()
    .Parent(parent)
    .Named("Back")
    .Theme(theme, IMUiThemeApplication.AccentOnly)
    .OnClick(PreviousMonth)
    .Build(out previous);
```

Or borrow any other inactive scene child by popup type/path:

```csharp
IMUiElementHandle element;
IMUiElementBuilder
    .FromPopup(PopupManager._type.single_chart, "Panel/Month")
    .Parent(parent)
    .Text("Custom text")
    .Theme(theme, IMUiThemeApplication.Full)
    .Build(out element);
```

For a Modern UI Pack control, use the same theme rather than manually reconfiguring its `UIManager` fields:

```csharp
VanillaControlOptions control = new VanillaControlOptions();
control.Type = VanillaControlType.Dropdown;
control.SemanticTheme = theme;

GameObject dropdown;
VanillaUiControlFactory.TryCreate(parent, control, out dropdown);
```

See [`docs/V3 Composable Custom UI.md`](docs/V3%20Composable%20Custom%20UI.md) for the design and examples, and [`docs/V3 Custom Component Coverage.md`](docs/V3%20Custom%20Component%20Coverage.md) for the concrete coverage inventory. The exhaustive scene/popup APIs are documented separately in [`docs/V3 Scene Template API.md`](docs/V3%20Scene%20Template%20API.md), [`docs/Vanilla Popup Catalog.md`](docs/Vanilla%20Popup%20Catalog.md), and [`docs/Serialized UI Reference Templates.md`](docs/Serialized%20UI%20Reference%20Templates.md).

## 2.1.0: scene-native UI patterns + Modern UI Pack resources

The framework now treats Idol Manager UI as two complementary sources instead of assuming every vanilla-looking control should come from Michsky Modern UI Pack `Resources`:

- **Scene-native game patterns** are resolved through `PopupManager` and cloned from inactive serialized popup hierarchies. The source popup does not need to be opened.
- **Michsky Modern UI Pack controls** remain available through `VanillaUiResources` and the complete `Resources` prefab catalog.

The first scene-native control is the producer-list scroll indicator used identically by **Producer Contracts**, **Producer Salaries**, and **Producer Loans**. Those screens do not assign a Unity `Scrollbar` to `ScrollRect.verticalScrollbar`; they use a separate vertical `Slider` with `SliderDefault`, a fixed circular handle, and two-way normalized-position events. `TryCreateStyledScrollView` now prefers that exact pattern in the gameplay scene and falls back to the genuine MUIP `Resources/scrollbar/Scrollbar` only when the scene template is unavailable.

New public scene APIs:
- `VanillaUiSceneTemplates.TryGetPopupRoot(...)`
- `VanillaUiSceneTemplates.TryFindPopupChild(...)`
- `VanillaUiSceneTemplates.TryClonePopupChild(...)`
- `VanillaUiSceneTemplates.TryCreateProducerListScrollSlider(...)`
- `VanillaUiSceneTemplates.ReserveProducerListViewportGutter(...)`

See [`docs/Scene UI Patterns.md`](docs/Scene%20UI%20Patterns.md) for the scene-derived measurements and event wiring.

## 2.0.3: selected game font and vanilla corner shader

- `IMUiKit.CreateText` now uses Idol Manager's currently selected game font by default, including runtime OS fonts through the TMP bridge.
- `IMUiKit.ApplyGameFont(root)` normalizes TMP and legacy text in cloned/custom UI and keeps legacy `Text` tied to `Font_Replacer`.
- Framework-created/cloned button labels now follow the selected game font unless a caller explicitly overrides the framework default.
- `IMUiKit.TryApplyVanillaRoundedCorners` uses the shipped `UI/RoundedCorners/RoundedCorners` shader with live RectTransform dimensions, avoiding oversized rounded-button sprites for panel chrome.


## 2.0.2: Idol Manager Unity compatibility fixes

- Uses the parent library's shared `dll` directory for the additional `UnityEngine.TextCoreModule.dll` reference required by TMP `FaceInfo`.
- Enumerates loaded fonts through a version-compatible reflection bridge instead of requiring the unavailable generic `Resources.FindObjectsOfTypeAll<T>()` overload.
- Uses the correct `Dropdown.Dropdown_Multi_Select` catalog constant for the vanilla multi-select prefab.
- Preserves the vanilla emergency-scrollbar ColorBlock values on Idol Manager's older UnityEngine.UI build even though several ColorBlock properties are read-only there.

## 2.0.1: catalog compile fix

- Fixed three C# CS0542 naming collisions in `VanillaUiPrefabCatalog`.
- `Dropdown.Dropdown` is now `Dropdown.Standard`.
- `Scrollbar.Scrollbar` is now `Scrollbar.Standard`.
- `Tooltip.Tooltip` is now `Tooltip.Standard`.
- Resource paths and runtime behavior are unchanged.

## 2.0.0: vanilla-first asset layer

Version 2.0.0 adds a resource-backed UI layer built from Idol Manager's shipped Unity assets and decompiled UI code. The framework now prefers the game's actual `Resources` prefabs over cloning whichever control happens to be instantiated in a popup.

New public APIs:
- `VanillaUiPrefabCatalog`: complete catalog of all 218 UI prefabs found under the game's exported `Resources` tree, including all 150 button variants.
- `VanillaUiResources`: `Resources.Load`/instantiate helpers, typed control creators, per-instance MUIP theme assignment, and generic component/theme configuration callbacks.
- `VanillaUiThemeSettings`: typed snapshot of every public setting on the shipped `MUIP Manager` `UIManager` ScriptableObject.
- `VanillaUiFonts`: access to the game's selected/bundled legacy fonts, the TMP equivalent of the currently selected game font, loaded TMP fonts, MUIP role fonts, OS/external dynamic fonts, and runtime TMP font creation.
- `IMUiBridges.TryCloneModernControl<T>(resourcePath, ...)`: explicit prefab-variant loading while retaining the old generic API.

Existing 1.x APIs remain source-compatible. Known Modern UI Pack controls now load their real vanilla prefab first and only fall back to scene/popup template discovery if the resource is unavailable.

See [`docs/Vanilla UI Asset Layer.md`](docs/Vanilla%20UI%20Asset%20Layer.md) for the 2.0.0 behavior, [`docs/Vanilla Prefab Catalog.md`](docs/Vanilla%20Prefab%20Catalog.md) for every resource prefab path, and [`docs/MUIP Vanilla Defaults.md`](docs/MUIP%20Vanilla%20Defaults.md) for every shipped `MUIP Manager` setting and default.

## Included API

Namespace: `IMUiFramework`

Main class: `IMUiKit`

Composable custom-UI layer:
- `IMUiTheme` / `IMUiThemePreset` / `IMUiVanillaColors` / `IMUiColorRole`
- `IMUiPrimitives.CreateSurface(...)` / `CreateText(...)` / `TryCreateButton(...)` / `CreateGrid(...)`
- `IMUiElementBuilder` / `IMUiElementHandle`
- `IMUiPresets.PreviousMonthButton()` / `NextMonthButton()` / `ChartMonthLabel()`
- `IMUiComposer.TryCreateMonthPager(...)`
- `IMUiComposer.TryCreateScrollView(...)` / `TryAttachVanillaListScrollIndicator(...)`
- `IMUiComposer.TryCreateRegisteredPopup(...)`

Vanilla scene/popup layer:
- `VanillaUiSceneCatalog.TryFindSceneObject(...)` / `TryCloneSceneObject(...)`
- `VanillaUiSceneCatalog.DescribeCurrentSceneUi(...)` / `DescribeCurrentPopups()`
- `VanillaUiSceneCatalog.TryGetPopupRoot(...)` / `TryClonePopup(...)` / `TryClonePopupChild(...)`
- `VanillaUiSceneFactory.TryCreate(...)`
- `VanillaPopupTemplateCatalog.All` / `TryGet(...)`
- `VanillaUiPopupFactory.TryCreate(...)` / `TryCreateModPopup(...)`
- `VanillaPopupBuilder.From(...)`
- `VanillaUiSceneTemplates.TryCreateProducerListScrollSlider(...)`
- `VanillaUiSceneTemplates.ReserveProducerListViewportGutter(...)`

Serialized-reference template layer:
- `VanillaUiReferenceTemplates.TryGetTemplate(...)` / `TryCloneTemplate(...)`
- `VanillaUiReferenceTemplates.TryGetPopupTemplate(...)` / `TryClonePopupTemplate(...)`
- `VanillaUiReferenceTemplates.DescribeCurrentSerializedUiTemplates(...)`
- `VanillaUiReferenceTemplates.DescribePopupSerializedUiTemplates(...)`

Universal Resources control layer:
- `VanillaUiControlFactory.TryCreate(...)` / `TryCreateResource(...)`
- `VanillaUiPrefabCatalog.AllPrefabPaths`

Vanilla asset layer:
- `VanillaUiResources.GetMuipManager()` / `CloneMuipManager()`
- `VanillaUiResources.LoadPrefab(...)` / `InstantiatePrefab(...)`
- `VanillaUiResources.TryInstantiatePrefab<T>(...)`
- `VanillaUiResources.TryCreateScrollbar(...)`
- `VanillaUiResources.TryCreateModernButton(...)`
- `VanillaUiResources.TryCreateDropdown(...)`
- `VanillaUiResources.TryCreateInputField(...)`
- `VanillaUiResources.TryCreateSlider(...)`
- `VanillaUiResources.TryCreateSwitch(...)`
- `VanillaUiResources.TryCreateToggle(...)`
- `VanillaUiResources.TryCreateProgressBar(...)`
- `VanillaUiResources.TryCreateLoopProgressBar(...)`
- `VanillaUiResources.TryCreateFilledProgressBar(...)`
- `VanillaUiResources.TryCreateListView(...)`
- `VanillaUiResources.TryCreateMovableWindow(...)`
- `VanillaUiResources.TryCreateModalWindow(...)`
- `VanillaUiResources.TryCreateTooltip(...)`
- `VanillaUiResources.TryCreateContextMenu(...)`
- `VanillaUiResources.TryCreateHorizontalSelector(...)`
- `VanillaUiResources.TryCreateNotification(...)`
- `VanillaUiResources.TryCreateWindowManager(...)`
- `VanillaUiThemeSettings.FromVanilla()`
- `VanillaUiFonts.GetMuipFont(...)`
- `VanillaUiFonts.GetGameSelectedLegacyFont()`
- `VanillaUiFonts.LoadExternalOrOsLegacyFont(...)`
- `VanillaUiFonts.LoadExternalOrOsTmpFont(...)`

Core methods:
- `TryAddTopMenuButton(...)`
- `TryAddSettingsButton(...)`
- `QueueSettingsButton(...)`
- `TryCreatePopupScaffold(...)`
- `TryCreateRegisteredPopupScaffold(...)`
- `TryRegisterPopup(...)`
- `TryOpenRegisteredPopup(...)`
- `CreateStagedVariablesState()`
- `BindStagedApplyCancelButtons(...)`
- `TryCreateSettingsSlider(...)`
- `TryCreateSettingsCheckbox(...)`
- `BindLanguageData(...)`
- `ResolveLanguageDataText(...)`
- `TrySyncBackdropWithActiveManagedPopups(...)`
- `TryRunPopupBackdropSafetyNet(...)`
- `CloneStyledButton(...)`
- `CreateStyledButton(...)`
- `CreateButtonFromTemplateOrStyle(...)`
- `TryAppendProfileExtra(...)`
- `CreateText(...)`
- `CreateLegacyText(...)`
- `GetOrCreateUiObject(...)`
- `FindUiElement(...)`
- `SetText(...)`
- `ClearChildren(...)`
- `RebuildLayout(...)`
- `CreateVerticalLayoutContainer(...)`
- `CreateHorizontalLayoutContainer(...)`
- `CreateGridLayoutContainer(...)`
- `TryCreateStyledScrollView(...)`
- `CreateDivider(...)`
- `TryCreateProfileText(...)`
- `TryCreateProfileDivider(...)`
- `MeasurePreferredTextWidth(...)`
- `ConfigureButtonLayout(...)`
- `RebindAllButtons(...)`
- `ActivateButtonDefaults(...)`
- `ApplyLayerRecursively(...)`

`PopupScaffold` provides:
- `Root`
- `Popup`
- `PanelRect`
- `TitleText`
- `ContentRoot`
- `ScrollRect`
- `CloseButton`
- `IsRegistered`
- `Close(...)` for queue-aware registered-popup closing
- `Hide(...)` for direct scaffold hiding

Bridge class: `IMUiBridges`

Dedicated bridge/helper UI methods:
- `TryCreateBridgeShowcasePopup(...)`
- `TryCreateBridgeShowcaseContent(...)`
- `TryCreateCameraEffectsHelperPanel(...)`
- `TryCreateModernDropdownHelperPanel(...)`
- `TryCreateTooltipHelperPanel(...)`
- `TryCreateGradientPreviewHelperPanel(...)`

Low-level bridge methods:
- `EnsureCinematicBloom(...)`
- `EnsureCinematicLensAberrations(...)`
- `EnsureImageEffectsAntialiasing(...)`
- `EnsureImageEffectsBloom(...)`
- `TryFindModernTemplate<T>(...)`
- `TryCloneModernControl<T>(...)`
- `TryCreateModernButton(...)`
- `ConfigureModernButton(...)`
- `TryCreateModernDropdown(...)`
- `TryCloneModernWindowManager(...)`
- `TryCreateModernModalWindow(...)`
- `ConfigureModernModalWindow(...)`
- `TryCreateModernProgressBar(...)`
- `SetModernProgress(...)`
- `AddSoftMask(...)`
- `TryEnsureBoundTooltipItem(...)`
- `AddBoundTooltipTrigger(...)`
- `TryCreateLegacyToolTipWidget(...)`
- `AddLegacyToolTipTrigger(...)`
- `TryCreateHoverTooltipWidget(...)`
- `AddHoverTooltipTrigger(...)`
- `AddUiGradient(...)`
- `AddTwoColorUiGradient(...)`

DOTween UI animation class: `IMUiTween`

- `Fade(...)` for `CanvasGroup`, `Graphic`, and `TMP_Text`
- `Color(...)` for `Graphic` and `TMP_Text`
- `MoveAnchored(...)`, `Resize(...)`, and `ResizeMinimum(...)`
- `PunchAnchored(...)` and `ShakeAnchored(...)`
- `RevealText(...)`
- `Kill(...)`

Supported namespaces:
- `UnityStandardAssets.CinematicEffects`
- `UnityStandardAssets.ImageEffects`
- `Michsky.UI.ModernUIPack`
- `DG.Tweening`
- `UnityEngine.UI.Extensions`
- `UnityEngine.UI.Michsky.UI.ModernUIPack`

## Minimal usage: add a top button + custom popup

```csharp
using HarmonyLib;
using IMUiFramework;
using TMPro;
using UnityEngine;

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class DemoPatch
{
    private static PopupScaffold scaffold;

    private static void Postfix(PopupManager __instance)
    {
        if (!IMUiKit.IsInitialized)
        {
            IMUiKit.Initialize(__instance);
        }

        GameObject button;
        if (IMUiKit.TryAddTopMenuButton(
            "MyDemoButton",
            "Demo",
            "Open demo popup",
            ToggleDemoPopup,
            out button))
        {
            // Button injected near Awards
        }

        if (scaffold == null)
        {
            PopupScaffold created;
            if (IMUiKit.TryCreatePopupScaffold("MyDemoPopup", "Demo Popup", new Vector2(860f, 520f), out created))
            {
                scaffold = created;
                IMUiKit.CreateText(scaffold.ContentRoot, "Body", "Hello from IMUiFramework.", 22, TextAlignmentOptions.Center, mainScript.black32);
                IMUiKit.RebuildLayout(scaffold.ContentRoot);
            }
        }
    }

    private static void ToggleDemoPopup()
    {
        if (scaffold == null || scaffold.Root == null)
        {
            return;
        }

        if (scaffold.Root.activeSelf)
        {
            scaffold.Hide();
        }
        else
        {
            scaffold.Show();
        }
    }
}
```

## Harmony-safe Settings button

`QueueSettingsButton` folds the retry pattern used by UI-heavy mods into the
framework. It is safe to invoke from `PopupManager.Start`, `Tabs_Manager.Awake`,
or a tab-open postfix: repeated calls update the existing button rather than
cloning another one.

```csharp
using HarmonyLib;
using IMUiFramework;

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class SettingsButtonPatch
{
    private static void Postfix()
    {
        IMUiKit.QueueSettingsButton(
            "MyMod_SettingsButton",
            "My Mod",
            "Open My Mod settings",
            OpenMyModSettings);
    }

    private static void OpenMyModSettings()
    {
        // IMUiKit.TryOpenRegisteredPopup((PopupManager._type)9001);
    }
}
```

## One-call registered popup

Use this form for a custom popup that should participate in the game's popup
manager, including its backdrop and close handling.

```csharp
using TMPro;
using UnityEngine;

PopupScaffold scaffold;
if (IMUiKit.TryCreateRegisteredPopupScaffold(
    (PopupManager._type)9001,
    "MyModPopup",
    "My Mod",
    new Vector2(860f, 520f),
    true,
    true,
    out scaffold))
{
    IMUiKit.CreateText(scaffold.ContentRoot, "Body", "Hello", 22,
        TextAlignmentOptions.Center, mainScript.black32);
}
```

## Modern UI Pack controls and DOTween animation

The framework clones a control from an existing Idol Manager template, rather
than constructing a partial Modern UI Pack prefab at runtime. This preserves its
serialized references, animator, and style. `TryCloneModernControl<T>` is the
generic entry point for any installed Modern UI Pack component.

```csharp
using IMUiFramework;
using Michsky.UI.ModernUIPack;
using UnityEngine;

GameObject modernButton;
ButtonManager buttonManager;
if (IMUiBridges.TryCreateModernButton(
    scaffold.ContentRoot,
    "MyModernButton",
    "Refresh",
    RefreshContent,
    out modernButton,
    out buttonManager))
{
    CanvasGroup group = modernButton.GetComponent<CanvasGroup>();
    if (group != null)
    {
        IMUiTween.Fade(group, 1f, 0.20f);
    }
}
```

`IMUiTween` uses the game's bundled DOTween assembly and configures animations
to use unscaled time by default. That keeps custom UI responsive while a popup
has paused simulation. Call `IMUiTween.Kill(target)` before replacing an
animation on the same target when that is the intended behavior.

## Minimal usage: profile Extras line

```csharp
[HarmonyPatch(typeof(Profile_Popup), "RenderTab_Extras")]
internal static class ProfilePatch
{
    private static void Postfix(Profile_Popup __instance)
    {
        if (__instance == null || __instance.Girl == null)
        {
            return;
        }

        IMUiKit.TryAppendProfileExtra(__instance, "<color=#5274FF>Example:</color> Added by framework", true);
    }
}
```

## Minimal usage: staged settings controls (Apply/Cancel)

```csharp
using IMUiFramework;
using UnityEngine;
using UnityEngine.UI;

// Assume scaffold was created via TryCreatePopupScaffold(...)
StagedVariablesState stage = IMUiKit.CreateStagedVariablesState();

GameObject speedSlider;
IMUiKit.TryCreateSettingsSlider(
    scaffold.ContentRoot,
    "SpeedSlider",
    "MyMod_Speed",
    "MYMOD__SPEED",
    1f,
    20f,
    5f,
    stage,
    out speedSlider,
    true);

GameObject enabledCheckbox;
IMUiKit.TryCreateSettingsCheckbox(
    scaffold.ContentRoot,
    "EnabledCheckbox",
    "MyMod_Enabled",
    "MYMOD__ENABLED",
    true,
    stage,
    out enabledCheckbox);

Button apply = IMUiKit.CreateStyledButton(scaffold.PanelRect, "Apply", "APPLY", 140f, 36f, null);
Button cancel = IMUiKit.CreateStyledButton(scaffold.PanelRect, "Cancel", "CANCEL", 140f, 36f, null);

IMUiKit.BindStagedApplyCancelButtons(stage, apply, cancel, null, null, scaffold.Popup, true);
```

## Minimal usage: one-call bridge helper showcase popup

```csharp
using HarmonyLib;
using IMUiFramework;
using UnityEngine;

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class BridgeShowcasePatch
{
    private static PopupScaffold bridgeScaffold;

    private static void Postfix(PopupManager __instance)
    {
        if (!IMUiKit.IsInitialized)
        {
            IMUiKit.Initialize(__instance);
        }

        if (bridgeScaffold == null)
        {
            GameObject showcaseRoot;
            IMUiBridges.TryCreateBridgeShowcasePopup(
                "MyBridgeShowcasePopup",
                "UI Bridge Showcase",
                new Vector2(900f, 580f),
                Camera.main,
                out bridgeScaffold,
                out showcaseRoot);
        }
    }
}
```

## 1.x API stability contract

- `IMUiKit`, `IMUiBridges`, `IMUiTween`, `PopupScaffold`, `ToolTipTriggerBridge`, and `HoverTooltipTriggerBridge` public members are the supported API for `1.x`.
- Method names and signatures in that public surface are treated as stable across `1.x` patch/minor updates.
- Internal classes (`internal` visibility) are runtime implementation details and may change without notice.

## Notes

- This framework reads style templates from base game popups/buttons when possible.
- If templates are missing, it falls back to safe runtime-created controls.
- Popup close/hide patching now resolves `PopupManager.Close` overloads dynamically and reconciles stale queue/blur/backdrop state more aggressively.
- Registered popup scaffold close buttons call `PopupManager.Close_` so queued popups, pause state, input blocking, blur, and backdrop state advance together.
- Runtime-created popup scaffolds initialize the vanilla `Popup.OnOpen` event before consumers register listeners or the popup is activated.
- Runtime popup recovery can now be called directly through `TrySyncBackdropWithActiveManagedPopups(...)` and `TryRunPopupBackdropSafetyNet(...)`.
- The built-in `UI Bridge` helper/showcase button is controlled by `IMUiFramework.config.ini` (`enable_bridge_showcase`).
- Default release behavior keeps `enable_bridge_showcase=false` so dependency installs are non-intrusive.
- `HoverTooltip` requires a `GUICamera` object in scene.
- `ToolTip` works only for `ScreenSpaceCamera` canvases (UI Extensions behavior).
- Modern UI Pack helpers require a matching control template to exist in the game's UI. The bridge searches registered popup prefabs, including inactive ones, before active scene objects.
- DOTween is supplied by Idol Manager at runtime; it is referenced with `Private=false`, so the framework does not ship a competing copy.
- It is intentionally utility-focused, not a forced architecture.

## Build

Project file:
- `mods/IM UI Framework/IM UI Framework.csproj`

Example command:
- `dotnet build "mods/IM UI Framework/IM UI Framework.csproj" -c Release`


## 2.0.3 font and corner behavior

`IMUiKit.CreateText` now follows Idol Manager's active `Fonts.GetFont()` selection by default. `VanillaUiFonts.GetGameSelectedTmpFont()` matches a loaded TMP asset for bundled fonts and creates a runtime TMP asset for an OS/dynamic font when needed. Use `IMUiKit.ApplyGameFont(root)` to normalize cloned or manually-created TMP/legacy text under an existing hierarchy.

`IMUiKit.TryApplyVanillaRoundedCorners(image, radius)` uses the game's shipped `UI/RoundedCorners/RoundedCorners` shader and updates `_WidthHeightRadius` whenever the target RectTransform changes. This is intended for the subtle 4-8 UI-unit corner radii used by vanilla panels and cards, not button-pill stretching.
