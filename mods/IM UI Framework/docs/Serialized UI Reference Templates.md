# Serialized UI Reference Templates

Not every vanilla UI prefab is a `Resources.Load` asset and not every reusable UI element is already instantiated as a child in the current scene.

Idol Manager frequently stores repeated UI templates in serialized fields on scene behaviours. The decompiled game code alone contains **226 public `GameObject` fields whose names contain `prefab` across 90 source files**. Examples include `Loans_Popup.prefab_line`, `Awards_Popup.prefab_item_win`, `SNS_Popup.prefab_message`, `Girl_Select_Popup.prefab_girl_button`, `Cafe_Popup.prefab_dish`, and many more.

AssetRipper may export these referenced objects under a `GameObject` folder, but that export path is **not** a valid runtime `Resources.Load` path. IM UI Framework 3 therefore resolves the serialized reference from the live vanilla component instead of guessing an asset path.

## Find a scalar template field

```csharp
GameObject source;
VanillaUiReferenceTemplates.TryGetPopupTemplate(
    PopupManager._type.producer_loans,
    "Loans_Popup",
    "prefab_line",
    -1,
    out source);
```

The source popup may be inactive and closed.

## Clone it safely

```csharp
GameObject row;
VanillaUiReferenceTemplates.TryClonePopupTemplate(
    PopupManager._type.producer_loans,
    "Loans_Popup",
    "prefab_line",
    -1,
    parent,
    "MyLoanStyleRow",
    VanillaUiCloneMode.Template,
    true,
    out row);
```

`Template` mode runs through the same v3 sanitization pipeline as scene-template cloning. Use `Exact` only when the original game-specific controller logic is intentionally wanted.

## Use it from a popup handle

```csharp
GameObject row;
popup.TryAddPopupReferencedTemplate(
    PopupManager._type.producer_loans,
    "Loans_Popup",
    "prefab_line",
    -1,
    "Row",
    out row);
```

When the referenced prefab belongs to the same popup used as the visual template, the shorter helper is available:

```csharp
GameObject row;
popup.TryAddReferencedTemplate("Loans_Popup", "prefab_line", "Row", out row);
```

## Arrays and Lists

For serialized `GameObject[]`, `Component[]`, `List<GameObject>`, or `List<Component>` fields, pass the desired `elementIndex` instead of `-1`.

## Discover what a popup references

```csharp
IList<VanillaUiReferenceTemplateDescriptor> templates =
    VanillaUiReferenceTemplates.DescribePopupSerializedUiTemplates(
        PopupManager._type.producer_loans,
        true);
```

Each descriptor reports the owner hierarchy path/component/field, optional collection index, source object, whether the source is already a scene object, and the source hierarchy path when applicable.

For broader diagnostics:

```csharp
IList<VanillaUiReferenceTemplateDescriptor> templates =
    VanillaUiReferenceTemplates.DescribeCurrentSerializedUiTemplates(
        "AgencyPopups",
        false); // false = external/non-scene references only
```

Serialized template graphs can also be followed recursively. Once a referenced vanilla prefab is resolved, pass that source object to `TryGetTemplate(GameObject, ...)`, `TryCloneTemplate(GameObject, ...)`, or `DescribeSerializedUiTemplates(GameObject, ...)` to inspect references stored on the prefab's own components.

The broad discovery call is deliberately on-demand. The framework does **not** reflect across the entire gameplay scene at startup.

## Coverage model

V3 now has four complementary vanilla-source layers:

1. **Popup templates**: all `PopupManager._type` values are cataloged; all materialized popup roots in the supplied gameplay/main-menu scenes are addressable while inactive.
2. **Scene UI**: any current-scene hierarchy, including inactive descendants and duplicate hierarchy paths, can be resolved/cloned.
3. **Serialized reference templates**: prefab/object references held by vanilla components can be resolved/cloned without fake Resource paths.
4. **Resources / Michsky Modern UI Pack**: all 218 shipped UI Resource prefabs are cataloged and loadable directly.

Together these cover the different ways Idol Manager actually stores and composes its UI instead of forcing every control through one asset-loading mechanism.
