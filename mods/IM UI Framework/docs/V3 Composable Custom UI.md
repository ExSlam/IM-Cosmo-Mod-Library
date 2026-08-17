# V3 Composable Custom UI

Version 3 separates **where a vanilla visual comes from** from **how a mod wants to use and recolor it**. This is the recommended API for new custom UI.

A custom interface should not need to clone Producer Salaries just to obtain a panel, or reconstruct a Singles-chart arrow with `>` text. The framework now treats vanilla UI as a kit of parts.

## 1. Four layers

### Source layer

Use exact game material when fidelity matters:

- `IMUiElementBuilder.FromPopup(type, path)` clones one inactive popup child.
- `IMUiElementBuilder.FromScene(path, occurrence)` clones any serialized scene UI object.
- `IMUiElementBuilder.FromResource(path)` loads any exact `Resources` prefab.
- `IMUiElementBuilder.FromControl(type)` uses a friendly Modern UI Pack family.
- `VanillaUiReferenceTemplates` resolves serialized `prefab_*` row/item references that are neither scene children nor Resources assets.

The source popup does not have to be open. `VanillaUiSceneCatalog` indexes inactive scene objects from `Scene.GetRootGameObjects()` and `PopupManager` already holds serialized popup references.

### Theme layer

`IMUiTheme` maps semantic roles instead of forcing literal colors:

- accent + hover/pressed/disabled/soft variants
- outer/inner/raised/muted surfaces
- divider and scroll track
- primary/secondary/muted/on-accent text
- title, success, danger, warning and gold
- game-selected body/title/legacy fonts

`IMUiTheme.Vanilla()` is the faithful default. `IMUiTheme.FromPreset(...)` supplies convenient blue, pink, green, gold, red and dark starting points.

Every named base-game `mainScript` color is also available from `IMUiVanillaColors.Get(IMUiVanillaColorPreset...)`, together with scene-derived neutral presets (`PanelOuter`, `PanelInner`, `TrackGrey`, `ChartArrowHover`). These are presets, not restrictions. Any `Color32` can be supplied.

```csharp
IMUiTheme theme = IMUiTheme.Vanilla().Clone();
theme.WithAccent(IMUiVanillaColorPreset.Pink);
theme.SetColor(IMUiColorRole.SurfaceOuter, new Color32(238, 233, 241, 255));
theme.SetColor(IMUiColorRole.SurfaceRaised, new Color32(255, 250, 253, 255));
```

### Primitive layer

`IMUiPrimitives` is for truly custom layouts:

- `CreateSurface` uses the vanilla sliced panel visual but lets the mod pick role, size and placement.
- `CreateText` uses Idol Manager typography and semantic text colors.
- `TryCreateButton` offers normal shipped button shapes plus the exact Singles-chart previous/next month arrows.
- `CreateDivider` and `CreateGrid` avoid repetitive RectTransform/LayoutGroup setup.
- `ApplyCard` turns an arbitrary custom object into a vanilla-style card without `Outline` borders.
- `TryCreateControl` forwards every Modern UI Pack family through the semantic theme bridge.

### Pattern layer

`IMUiComposer` packages multi-object conventions that are easy to get subtly wrong:

- Singles-chart month/year pager
- producer-list fixed circular scroll indicator
- complete custom ScrollRect + content + vanilla list indicator
- themed registered popup shell
- vanilla card/calendar cell styling

Both the primitive and pattern layers have short-form overloads. Options objects are only necessary when a mod actually needs the extra knobs. For example:

```csharp
IMUiTheme pink = IMUiTheme.FromAccent(IMUiVanillaColorPreset.Pink);
GameObject card = IMUiPrimitives.CreateCard(parent, "Summary", new Vector2(360f, 90f), pink);
TextMeshProUGUI label = IMUiPrimitives.CreateLabel(card.transform, "Custom content", 22f, TextAlignmentOptions.Center, pink);

IMUiMonthPagerHandle pager;
IMUiComposer.TryCreateMonthPager(parent, "August 2026", Prev, Next, pink, out pager);
```

The intent is that a custom mod chooses content and layout, while the framework owns the fiddly vanilla visual setup.

## 2. The Singles-chart month pager

The source objects in `main.unity` are:

- `Single_Chart/Panel/Prev Month`
- `Single_Chart/Panel/Month`
- `Single_Chart/Panel/Next Month`

The previous/next buttons are **TMP glyph buttons**, not image arrows. Their text is `U+F33A` and `U+F33B`, and the source font is deliberately preserved because a normal game body font may not contain those private-use glyphs.

Their serialized normal color is the game's blue `(119, 123, 186)` and the highlighted/pressed state is approximately `(163, 163, 209)`. `IMUiTheme.Vanilla()` records those exact interaction values.

```csharp
IMUiMonthPagerOptions nav = new IMUiMonthPagerOptions();
nav.ObjectName = "MonthRow";
nav.Label = "August 2026";
nav.Width = 460f;
nav.LabelWidth = 200f;
nav.OnPrevious = OnPreviousMonth;
nav.OnNext = OnNextMonth;

IMUiMonthPagerHandle pager;
IMUiComposer.TryCreateMonthPager(parent, nav, out pager);
```

Set `nav.Theme` to recolor it without changing the glyphs, dimensions, transition type or button structure.

## 3. Vanilla list scrolling

Contracts, Salaries and Loans use the same serialized pattern:

- a separate `Slider`, not `Scrollbar`
- 20-unit root width
- 1.1004126 root scale
- approximately 4.21-unit grey track
- 11.14 x 11.14 fixed circular handle
- `ScrollRect.verticalScrollbar == null`
- two-way normalized-position synchronization

This is why the vanilla thumb stays a small circle rather than growing into a proportional pill.

Use:

```csharp
IMUiScrollViewOptions listOptions = new IMUiScrollViewOptions();
listOptions.OffsetMin = new Vector2(16f, 56f);
listOptions.OffsetMax = new Vector2(-32f, -92f);
listOptions.Theme = customTheme; // optional

IMUiScrollViewHandle list;
IMUiComposer.TryCreateScrollView(panel, listOptions, out list);
```

Or attach only the indicator to an existing custom `ScrollRect`:

```csharp
GameObject indicator;
IMUiComposer.TryAttachVanillaListScrollIndicator(
    scrollRect.transform,
    scrollRect,
    "Slider",
    customTheme,
    out indicator);
```

When no theme is passed, the exact baked scene thumb is kept. When a custom accent is passed, v3 preserves the scene geometry but uses the game's neutral rounded scrollbar sprite for a clean tint rather than multiplying a new color into baked purple pixels.

## 4. Cards and calendar grids

A vanilla-looking calendar should not be a matrix of transparent `Image`s plus `Outline` components. Use the same sliced panel language as the chart/popup UI:

```csharp
GameObject cell = new GameObject("Day", typeof(RectTransform));
cell.transform.SetParent(grid.transform, false);
IMUiComposer.ApplyCalendarCellStyle(cell, customTheme, false);
```

Or:

```csharp
IMUiPrimitives.ApplyCard(cell, customTheme, false);
```

For the grid itself:

```csharp
IMUiGridOptions gridOptions = new IMUiGridOptions();
gridOptions.Columns = 7;
gridOptions.CellSize = new Vector2(108f, 92f);
gridOptions.Spacing = new Vector2(3f, 3f);
GameObject grid = IMUiPrimitives.CreateGrid(parent, gridOptions);
```

The framework supplies the vanilla visual language. The mod still owns what goes inside each cell.

## 5. Every shipped Modern UI Pack control is themeable

`VanillaControlType` covers the control families. `VanillaUiPrefabCatalog` exposes every exact shipped variant when the family default is not specific enough.

```csharp
VanillaControlOptions inputOptions = new VanillaControlOptions();
inputOptions.Type = VanillaControlType.InputField;
inputOptions.SemanticTheme = customTheme;

GameObject input;
VanillaUiControlFactory.TryCreate(parent, inputOptions, out input);
```

The semantic theme is translated into the prefab's `UIManager` settings before activation, then role-aware styling handles Unity `Selectable`, input caret/selection, slider/scrollbar/toggle and text state. This prevents a Modern UI Pack component from restoring its own default colors on `OnEnable`.

For an exact variant:

```csharp
VanillaControlOptions buttonOptions = new VanillaControlOptions();
buttonOptions.ResourcePath = VanillaUiPrefabCatalog.Button.rounded_outline_Standard;
buttonOptions.SemanticTheme = customTheme;
```

This works for all 218 cataloged Resources UI prefabs.

## 6. Any scene UI not covered by a convenience preset

Convenience helpers are not a whitelist. The generic builder remains available:

```csharp
IMUiElementHandle piece;
IMUiElementBuilder
    .FromPopup(PopupManager._type.some_popup, "Panel/Some Child")
    .Parent(parent)
    .Named("MyVersion")
    .Theme(customTheme, IMUiThemeApplication.Interactive)
    .Configure(delegate(GameObject go)
    {
        // arbitrary mod-specific layout/content changes
    })
    .GraphicColor("Background", IMUiColorRole.SurfaceRaised)
    .GraphicColor("Header", new Color32(245, 235, 250, 255))
    .Build(out piece);
```

`GraphicColor(...)` is applied after broad semantic theming, so an individual child of a complex vanilla clone can be assigned a semantic role or an arbitrary `Color32` without rewriting the control's hierarchy.

`Template` clone mode keeps ordinary UI machinery and safe internal wiring while stripping source-popup business controllers. `VisualOnly` produces inert chrome. `Exact` keeps everything and should only be used when a mod deliberately wants the original controller behavior.

## 7. Whole-popup cloning is now a specialist tool

`VanillaPopupBuilder` and `TryCreateRegisteredVanillaPopupScaffold` still exist for cases where a mod truly wants to reproduce a whole vanilla popup. They are **not** the recommended default for custom UI.

For a new custom popup, prefer:

1. a custom shell (`IMUiComposer.TryCreateRegisteredPopup` or the existing generic scaffold),
2. individual scene-native pieces such as chart navigation,
3. `IMUiPrimitives` for custom panels/cards/text/grid,
4. `VanillaUiControlFactory` for standard controls,
5. exact row/item references through `VanillaUiReferenceTemplates` where appropriate.

That keeps the visual grammar vanilla without importing somebody else's entire layout and controller tree.
