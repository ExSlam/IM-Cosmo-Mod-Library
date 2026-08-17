# Version 3 Scene Template API

Version 3 treats the shipped game itself as a UI template library. The framework now has four complementary sources instead of forcing every mod UI through hand-built rectangles:

1. **Serialized scene UI** through `VanillaUiSceneCatalog` and `VanillaUiSceneFactory`.
2. **Complete vanilla popup roots** through `VanillaPopupTemplateCatalog` and `VanillaUiPopupFactory`.
3. **Serialized prefab/object references** through `VanillaUiReferenceTemplates`, for repeated vanilla rows/items held on game components rather than under `Resources`.
4. **All 218 shipped Modern UI Pack Resources prefabs** through `VanillaUiPrefabCatalog`, `VanillaUiResources`, and `VanillaUiControlFactory`.

The supplied vanilla scenes contain 9,651 `RectTransform` objects in `main.unity` and 508 in `Main Menu.unity`. The scene catalog indexes the actual loaded hierarchy from `Scene.GetRootGameObjects()`, including inactive descendants, so any of those UI objects can be found or cloned by its exact path. It is not restricted to a small hard-coded list of widgets.

## Why inactive UI works

`GameObject.Find` ignores inactive objects, which made it the wrong primitive for closed popups. Version 3 builds its own current-scene index from root GameObjects and walks every descendant. `PopupManager` references are also serialized, so a closed popup such as Producer Contracts is available without opening it.

```csharp
GameObject contracts;
VanillaUiSceneCatalog.TryGetPopupRoot(
    PopupManager._type.producer_contracts,
    out contracts);
```

For arbitrary scene UI, use its full hierarchy path:

```csharp
VanillaSceneTemplateHandle row;
VanillaUiSceneFactory.TryCreate(
    "AgencyPopups/Producer_Contracts/Panel/Titles",
    parent,
    "MyTitles",
    out row);
```

Names can be used for unique objects, but full paths are preferred because scene names such as `Panel`, `Container`, and `Button` occur many times. The gameplay scene also has a small number of duplicate same-name sibling paths. `TryFindSceneObject(path, occurrenceIndex, out obj)` and `FindSceneObjectsByPath(path)` expose every occurrence; `VanillaSceneUiDescriptor.HierarchyOccurrenceIndex` reports which one a descriptor represents.

## Clone modes

### `Template` (default)

This is the normal modding mode. It preserves the serialized RectTransforms, sprites, masks, layout components, fonts, Modern UI Pack machinery, and safe generic Idol Manager UI behaviours. Popup-specific Assembly-CSharp controllers are disabled and destroyed.

Version 3 also **preserves safe serialized event links that stay inside the clone**. That is important for composite vanilla controls. For example, Producer Contracts/Salaries/Loans use a `ScrollRect` and a separate `SliderDefault` slider whose two-way event wiring is serialized in the scene. Earlier blanket event clearing would have destroyed that behaviour. V3 prunes only listeners that leave the clone or target a controller that will be removed.

### `Exact`

Preserves every component and listener. Use it for inspection, or only when you deliberately want the original game logic. Exact mode can carry popup-specific data/controller behaviour and is therefore not the normal choice for mod-owned UI.

### `VisualOnly`

Removes game-specific controllers, clears interactions, and disables Selectables. It is useful when a mod wants vanilla chrome as an inert visual template.

## Universal scene discovery

```csharp
IList<VanillaSceneUiDescriptor> ui =
    VanillaUiSceneCatalog.DescribeCurrentSceneUi();

GameObject source;
VanillaUiSceneCatalog.TryFindSceneObject(
    "AgencyPopups/Producer_Salaries/Panel",
    out source);

GameObject clone;
VanillaUiSceneCatalog.TryCloneSceneObject(
    "AgencyPopups/Producer_Salaries/Panel",
    parent,
    "SalaryStylePanel",
    VanillaUiCloneMode.Template,
    true,
    out clone);
```

`DescribeCurrentSceneUi(rootPath)` can be restricted to one subtree and reports hierarchy path, active state, depth, and the attached component types. This gives modders a runtime inventory even for UI that does not deserve a dedicated framework method.

## Popup builder

The popup builder is the main preparation-code reduction in v3:

```csharp
VanillaPopupHandle popup;
bool ok = VanillaPopupBuilder
    .From(PopupManager._type.producer_salaries)
    .Named("MonthlyLedgerPopup")
    .WithTitle("Monthly Ledger")
    .ContentAt("Panel/Container", true)
    .RegisterAs((PopupManager._type)9100)
    .Build(out popup);
```

That clone starts with the vanilla popup's serialized geometry, masks, background, typography hierarchy, scrolling machinery, sprites, layout, and animation settings. The mod no longer needs to prepare all of those pieces manually.

When a template has an unusual hierarchy, explicit paths can be supplied with `PanelAt`, `ContentAt`, `ScrollAt`, and `CloseAt`. If no safe content container is detected, the factory leaves `ContentRoot` null. It never falls back to the popup root, so `clearContent` cannot accidentally erase the whole popup.

The migration-friendly `IMUiKit.TryCreateVanillaPopupScaffold(...)` and `TryCreateRegisteredVanillaPopupScaffold(...)` return the familiar `PopupScaffold` shape for existing mods.

## Serialized prefab-reference templates

The scene hierarchy is not the whole asset graph. Idol Manager controller components also hold serialized references such as `prefab_line`, `prefab_button`, and `prefab_stat`. V3 resolves those live references directly, including while the source popup is closed:

```csharp
GameObject row;
VanillaUiReferenceTemplates.TryClonePopupTemplate(
    PopupManager._type.producer_loans,
    "Loans_Popup",
    "prefab_line",
    -1,
    parent,
    "LoanRow",
    VanillaUiCloneMode.Template,
    true,
    out row);
```

This fills the gap between scene objects and `Resources`. AssetRipper's `GameObject/*.prefab` export directory is useful evidence, but its filenames are not runtime `Resources.Load` addresses. The serialized-reference layer uses the actual Unity object reference instead.


## All Modern UI Pack controls

The exact Resources catalog still matters because some controls are genuine reusable prefabs rather than scene composites. Every exported Resources prefab is present in `VanillaUiPrefabCatalog.AllPrefabPaths`.

For a normal family default:

```csharp
GameObject input;
VanillaUiControlFactory.TryCreate(
    VanillaControlType.InputField,
    parent,
    "SearchInput",
    out input);
```

For an exact shipped variant:

```csharp
GameObject exact;
VanillaUiControlFactory.TryCreateResource(
    VanillaUiPrefabCatalog.InputField.Input_Field_Fading_Left,
    parent,
    "SearchInput",
    true,
    out exact);
```

The factory applies a per-instance copy of the vanilla MUIP theme and the current Idol Manager game font by default. It does not mutate the game's global `MUIP Manager` asset.

## Scene locality

Scene-native templates exist only while their scene is loaded. Main-menu-only popup templates are available on the main menu; gameplay-only templates are available in gameplay. V3 does not silently load another Unity scene merely to steal a UI object, because doing so could alter game state. Shared Resources controls remain available anywhere.
