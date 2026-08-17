# Version 3 Migration

Version 3 keeps the old scaffold, Resources, scene-clone and popup-template APIs. The recommended migration for a custom UI is now **compose vanilla pieces**, not **clone the closest whole popup**.

## Custom popup shell

If your mod has its own layout, keep it. Create/register the shell and let v3 supply the visual language:

```csharp
IMUiPopupOptions popupOptions = new IMUiPopupOptions();
popupOptions.RegistrationType = PopupType;
popupOptions.ObjectName = "MyPopup";
popupOptions.Title = "My Popup";
popupOptions.Size = new Vector2(900f, 560f);
popupOptions.Theme = IMUiTheme.Vanilla();

PopupScaffold popup;
IMUiComposer.TryCreateRegisteredPopup(popupOptions, out popup);
```

Do not use `TryCreateRegisteredVanillaPopupScaffold` merely because you want vanilla colors or panel chrome. That API is for intentionally cloning an entire vanilla hierarchy.

## Replace hand-made month arrows

Old custom UI often used `<`, `>`, generic button prefabs, or guessed arrow sprites. Replace that with the Singles-chart pattern:

```csharp
IMUiMonthPagerOptions options = new IMUiMonthPagerOptions();
options.Label = currentMonthLabel;
options.OnPrevious = PreviousMonth;
options.OnNext = NextMonth;

IMUiMonthPagerHandle pager;
IMUiComposer.TryCreateMonthPager(parent, options, out pager);
```

The framework keeps the exact `U+F33A` / `U+F33B` icon font and serialized ColorBlock.

## Replace custom Unity Scrollbar code

For list-style popup scrolling, replace manually created `Scrollbar` objects with:

```csharp
GameObject indicator;
IMUiComposer.TryAttachVanillaListScrollIndicator(
    scrollRect.transform,
    scrollRect,
    "Slider",
    null,
    out indicator);
```

Or replace the entire ScrollRect setup with `IMUiComposer.TryCreateScrollView`.

## Replace spreadsheet-style grid cells

Instead of transparent images plus `Outline`:

```csharp
IMUiComposer.ApplyCalendarCellStyle(cell, theme, isEmpty);
```

This reuses a vanilla sliced panel visual and semantic surface colors.

## Add custom colors without abandoning vanilla UI

```csharp
IMUiTheme theme = IMUiTheme.Vanilla().Clone();
theme.WithAccent(IMUiVanillaColorPreset.Green);
theme.SetColor(IMUiColorRole.SurfaceRaised, new Color32(248, 252, 249, 255));
```

Pass that same theme to scene pieces, composers and Modern UI Pack controls. You keep vanilla geometry/sprites/behavior while changing the palette coherently.

## Modern UI Pack controls

Use `VanillaUiControlFactory` for control families and `VanillaUiPrefabCatalog` when you need an exact one of the 218 Resources variants:

```csharp
VanillaControlOptions options = new VanillaControlOptions();
options.Type = VanillaControlType.Dropdown;
options.SemanticTheme = theme;

GameObject dropdown;
VanillaUiControlFactory.TryCreate(parent, options, out dropdown);
```

## One exact scene piece

When a vanilla screen contains a component you want, borrow only that component:

```csharp
IMUiElementHandle piece;
IMUiElementBuilder
    .FromPopup(PopupManager._type.single_chart, "Panel/Prev Month")
    .Parent(parent)
    .OnClick(PreviousMonth)
    .Build(out piece);
```

The source popup can remain inactive.

## When whole-popup cloning still makes sense

Use `VanillaPopupBuilder` or `TryCreateRegisteredVanillaPopupScaffold` when the desired result truly is "this vanilla popup, with different content." They remain useful, but they are no longer the general-purpose path for custom UI.
