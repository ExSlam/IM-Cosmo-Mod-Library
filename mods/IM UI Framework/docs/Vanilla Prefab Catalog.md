# Vanilla Prefab Catalog

This catalog is generated from the prefab files actually exported beneath Idol Manager's Unity `Resources` tree. Paths below are passed to `Resources.Load<GameObject>(path)` without the `Resources/` prefix or `.prefab` suffix. The framework exposes the same set through `VanillaUiPrefabCatalog.AllPrefabPaths`.

**Total runtime-loadable UI prefabs: 218.**

For per-instance colors, fonts, sizes, and MUIP theme variants, combine any path with `TryInstantiatePrefab<T>(..., configure, configureTheme)`. Prefab-specific public component fields remain configurable through `configure`; all global MUIP fields are configurable through `configureTheme`.

## animated icon (11)

- `animated icon/Hamburger Menu`
- `animated icon/Heart Pop`
- `animated icon/Load`
- `animated icon/Lock`
- `animated icon/Message Bubbles`
- `animated icon/No to Yes`
- `animated icon/Notification Bell`
- `animated icon/Sand Clock`
- `animated icon/Slider`
- `animated icon/Window`
- `animated icon/Yes to No`

## button (150)

- `button/basic - gradient/Blue`
- `button/basic - gradient/Brown`
- `button/basic - gradient/Gray`
- `button/basic - gradient/Green`
- `button/basic - gradient/Night`
- `button/basic - gradient/Orange`
- `button/basic - gradient/Pink`
- `button/basic - gradient/Purple`
- `button/basic - gradient/Red`
- `button/basic - gradient/White`
- `button/basic - only icon/Blue`
- `button/basic - only icon/Brown`
- `button/basic - only icon/Gray`
- `button/basic - only icon/Green`
- `button/basic - only icon/Night`
- `button/basic - only icon/Orange`
- `button/basic - only icon/Pink`
- `button/basic - only icon/Purple`
- `button/basic - only icon/Red`
- `button/basic - only icon/Standard`
- `button/basic - only icon/White`
- `button/basic - outline gradient/Blue`
- `button/basic - outline gradient/Brown`
- `button/basic - outline gradient/Gray`
- `button/basic - outline gradient/Green`
- `button/basic - outline gradient/Night`
- `button/basic - outline gradient/Orange`
- `button/basic - outline gradient/Pink`
- `button/basic - outline gradient/Purple`
- `button/basic - outline gradient/Red`
- `button/basic - outline gradient/White`
- `button/basic - outline only icon/Blue`
- `button/basic - outline only icon/Brown`
- `button/basic - outline only icon/Gray`
- `button/basic - outline only icon/Green`
- `button/basic - outline only icon/Night`
- `button/basic - outline only icon/Orange`
- `button/basic - outline only icon/Pink`
- `button/basic - outline only icon/Purple`
- `button/basic - outline only icon/Red`
- `button/basic - outline only icon/Standard`
- `button/basic - outline only icon/White`
- `button/basic - outline with icon/Blue`
- `button/basic - outline with icon/Brown`
- `button/basic - outline with icon/Gray`
- `button/basic - outline with icon/Green`
- `button/basic - outline with icon/Night`
- `button/basic - outline with icon/Orange`
- `button/basic - outline with icon/Pink`
- `button/basic - outline with icon/Purple`
- `button/basic - outline with icon/Red`
- `button/basic - outline with icon/Standard`
- `button/basic - outline with icon/White`
- `button/basic - outline/Blue`
- `button/basic - outline/Brown`
- `button/basic - outline/Gray`
- `button/basic - outline/Green`
- `button/basic - outline/Night`
- `button/basic - outline/Orange`
- `button/basic - outline/Pink`
- `button/basic - outline/Purple`
- `button/basic - outline/Red`
- `button/basic - outline/Standard`
- `button/basic - outline/White`
- `button/basic - with icon/Blue`
- `button/basic - with icon/Brown`
- `button/basic - with icon/Gray`
- `button/basic - with icon/Green`
- `button/basic - with icon/Night`
- `button/basic - with icon/Orange`
- `button/basic - with icon/Pink`
- `button/basic - with icon/Purple`
- `button/basic - with icon/Red`
- `button/basic - with icon/Standard`
- `button/basic - with icon/White`
- `button/basic/Blue`
- `button/basic/Brown`
- `button/basic/Gray`
- `button/basic/Green`
- `button/basic/Night`
- `button/basic/Orange`
- `button/basic/Pink`
- `button/basic/Purple`
- `button/basic/Red`
- `button/basic/Standard`
- `button/basic/White`
- `button/radial - only icon/Blue`
- `button/radial - only icon/Brown`
- `button/radial - only icon/Gray`
- `button/radial - only icon/Green`
- `button/radial - only icon/Night`
- `button/radial - only icon/Orange`
- `button/radial - only icon/Pink`
- `button/radial - only icon/Purple`
- `button/radial - only icon/Red`
- `button/radial - only icon/Standard`
- `button/radial - only icon/White`
- `button/radial - outline only icon/Blue`
- `button/radial - outline only icon/Brown`
- `button/radial - outline only icon/Gray`
- `button/radial - outline only icon/Green`
- `button/radial - outline only icon/Night`
- `button/radial - outline only icon/Orange`
- `button/radial - outline only icon/Pink`
- `button/radial - outline only icon/Purple`
- `button/radial - outline only icon/Red`
- `button/radial - outline only icon/Standard`
- `button/radial - outline only icon/White`
- `button/rounded - gradient/Blue`
- `button/rounded - gradient/Brown`
- `button/rounded - gradient/Gray`
- `button/rounded - gradient/Green`
- `button/rounded - gradient/Night`
- `button/rounded - gradient/Orange`
- `button/rounded - gradient/Pink`
- `button/rounded - gradient/Purple`
- `button/rounded - gradient/Red`
- `button/rounded - gradient/White`
- `button/rounded - outline gradient/Blue`
- `button/rounded - outline gradient/Brown`
- `button/rounded - outline gradient/Gray`
- `button/rounded - outline gradient/Green`
- `button/rounded - outline gradient/Night`
- `button/rounded - outline gradient/Orange`
- `button/rounded - outline gradient/Pink`
- `button/rounded - outline gradient/Purple`
- `button/rounded - outline gradient/Red`
- `button/rounded - outline gradient/White`
- `button/rounded - outline/Blue`
- `button/rounded - outline/Brown`
- `button/rounded - outline/Gray`
- `button/rounded - outline/Green`
- `button/rounded - outline/Night`
- `button/rounded - outline/Orange`
- `button/rounded - outline/Pink`
- `button/rounded - outline/Purple`
- `button/rounded - outline/Red`
- `button/rounded - outline/Standard`
- `button/rounded - outline/White`
- `button/rounded/Blue`
- `button/rounded/Brown`
- `button/rounded/Gray`
- `button/rounded/Green`
- `button/rounded/Night`
- `button/rounded/Orange`
- `button/rounded/Pink`
- `button/rounded/Purple`
- `button/rounded/Red`
- `button/rounded/Standard`
- `button/rounded/White`

## context menu (2)

- `context menu/Context Menu`
- `context menu/Context Menu Button`

## dropdown (4)

- `dropdown/Dropdown`
- `dropdown/Dropdown - Multi Select`
- `dropdown/Dropdown Item`
- `dropdown/Multi Select Dropdown Item`

## horizontal selector (2)

- `horizontal selector/Horizontal Selector`
- `horizontal selector/Indicator Item`

## input field (7)

- `input field/Input Field - Fading (Left)`
- `input field/Input Field - Fading (Middle)`
- `input field/Input Field - Fading (Right)`
- `input field/Input Field - Multi-Line`
- `input field/Input Field - Standard (Left)`
- `input field/Input Field - Standard (Middle)`
- `input field/Input Field - Standard (Right)`

## list view (1)

- `list view/List View`

## modal window (2)

- `modal window/Style 1`
- `modal window/Style 2`

## movable window (1)

- `movable window/Movable Window`

## notification (3)

- `notification/Fading Notification`
- `notification/Popup Notification`
- `notification/Sliding Notification`

## other (1)

- `other/Canvas`

## progress bar (7)

- `progress bar/PB - Radial (Bold)`
- `progress bar/PB - Radial (Light)`
- `progress bar/PB - Radial (Regular)`
- `progress bar/PB - Radial (Thin)`
- `progress bar/PB - Radial Filled Horizontal`
- `progress bar/PB - Radial Filled Vertical`
- `progress bar/PB - Standard`

## progress bar (loop) (6)

- `progress bar (loop)/PB Loop - Radial Material`
- `progress bar (loop)/PB Loop - Radial Pie`
- `progress bar (loop)/PB Loop - Radial Run`
- `progress bar (loop)/PB Loop - Radial Trapez`
- `progress bar (loop)/PB Loop - Standard Fastly`
- `progress bar (loop)/PB Loop - Standard Run`

## scrollbar (1)

- `scrollbar/Scrollbar`

## slider (12)

- `slider/gradient/Slider - Gradient`
- `slider/gradient/Slider - Gradient (Popup)`
- `slider/gradient/Slider - Gradient (Value)`
- `slider/outline/Slider - Outline`
- `slider/outline/Slider - Outline (Popup)`
- `slider/outline/Slider - Outline (Value)`
- `slider/radial/Slider - Radial`
- `slider/radial/Slider - Radial (Gradient)`
- `slider/range/Slider - Range`
- `slider/standard/Slider - Standard`
- `slider/standard/Slider - Standard (Popup)`
- `slider/standard/Slider - Standard (Value)`

## switch (1)

- `switch/Switch - Standard`

## toggle (5)

- `toggle/Toggle - Standard`
- `toggle/Toggle - Standard (Bold)`
- `toggle/Toggle - Standard (Light)`
- `toggle/Toggle - Standard (Regular)`
- `toggle/Toggle Group Panel`

## tooltip (1)

- `tooltip/Tooltip`

## window manager (1)

- `window manager/Window Manager`
