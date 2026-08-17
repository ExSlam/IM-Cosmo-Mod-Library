# MUIP Vanilla Defaults

These are the **85 public `Michsky.UI.ModernUIPack.UIManager` fields** found in the decompiled Idol Manager code, paired with their values from the exported `Resources/MUIP Manager.asset`. `VanillaUiThemeSettings` mirrors this complete field set one-for-one.

For an isolated control variant, start from `VanillaUiThemeSettings.FromVanilla()`, change only the desired fields, and pass it through a `configureTheme` callback. Do not mutate `VanillaUiResources.GetMuipManager()` for one-off controls because that object is the game-global theme.

| Field | Type | Shipped value |
|---|---|---|
| `enableDynamicUpdate` | `bool` | `true` |
| `enableExtendedColorPicker` | `bool` | `true` |
| `editorHints` | `bool` | `true` |
| `animatedIconColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `contextBackgroundColor` | `Color` | `{r: 0.21568628, g: 0.29411766, b: 0.37254903, a: 1}` |
| `buttonThemeType` | `UIManager.ButtonThemeType` | `0` (serialized enum value) |
| `buttonFont` | `TMP_FontAsset` | `OpenSans-Semibold SDF` |
| `buttonFontSize` | `float` | `22.5` |
| `buttonBorderColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `buttonFilledColor` | `Color` | `{r: 0.1764706, g: 0.25490198, b: 0.33333334, a: 1}` |
| `buttonTextBasicColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `buttonTextColor` | `Color` | `{r: 0.37254903, g: 0.40784314, b: 0.4509804, a: 1}` |
| `buttonTextHighlightedColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `buttonIconBasicColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `buttonIconColor` | `Color` | `{r: 0.37254903, g: 0.40784314, b: 0.4509804, a: 1}` |
| `buttonIconHighlightedColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `dropdownItemFont` | `TMP_FontAsset` | `OpenSans-Regular SDF_0` |
| `dropdownItemFontSize` | `float` | `22.5` |
| `dropdownThemeType` | `UIManager.DropdownThemeType` | `0` (serialized enum value) |
| `dropdownAnimationType` | `UIManager.DropdownAnimationType` | `0` (serialized enum value) |
| `dropdownFont` | `TMP_FontAsset` | `Linotte-SemiBold SDF` |
| `dropdownFontSize` | `float` | `25` |
| `dropdownColor` | `Color` | `{r: 0.46666667, g: 0.48235294, b: 0.7294118, a: 1}` |
| `dropdownTextColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `dropdownIconColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `dropdownItemColor` | `Color` | `{r: 0.5764706, g: 0.5764706, b: 0.7607843, a: 1}` |
| `dropdownItemTextColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `dropdownItemIconColor` | `Color` | `{r: 255, g: 255, b: 255, a: 1}` |
| `selectorFont` | `TMP_FontAsset` | `OpenSans-Regular SDF_0` |
| `hSelectorFontSize` | `float` | `28` |
| `selectorColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `selectorHighlightedColor` | `Color` | `{r: 0.1764706, g: 0.25490198, b: 0.33333334, a: 1}` |
| `hSelectorInvertAnimation` | `bool` | `false` |
| `hSelectorLoopSelection` | `bool` | `true` |
| `inputFieldFont` | `TMP_FontAsset` | `OpenSans-Regular SDF_0` |
| `inputFieldFontSize` | `float` | `28` |
| `inputFieldColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `modalWindowTitleFont` | `TMP_FontAsset` | `OpenSans-Bold SDF` |
| `modalWindowContentFont` | `TMP_FontAsset` | `OpenSans-Regular SDF_0` |
| `modalThemeType` | `UIManager.DropdownThemeType` | `0` (serialized enum value) |
| `modalWindowTitleColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `modalWindowDescriptionColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `modalWindowIconColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `modalWindowBackgroundColor` | `Color` | `{r: 0.1764706, g: 0.25490198, b: 0.33333334, a: 1}` |
| `modalWindowContentPanelColor` | `Color` | `{r: 1, g: 1, b: 1, a: 0.019607844}` |
| `notificationTitleFont` | `TMP_FontAsset` | `OpenSans-Bold SDF` |
| `notificationTitleFontSize` | `float` | `22.5` |
| `notificationDescriptionFont` | `TMP_FontAsset` | `OpenSans-Light SDF` |
| `notificationDescriptionFontSize` | `float` | `18` |
| `notificationThemeType` | `UIManager.NotificationThemeType` | `0` (serialized enum value) |
| `notificationBackgroundColor` | `Color` | `{r: 0.1764706, g: 0.25490198, b: 0.33333334, a: 1}` |
| `notificationTitleColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `notificationDescriptionColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `notificationIconColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `progressBarLabelFont` | `TMP_FontAsset` | `OpenSans-Semibold SDF` |
| `progressBarLabelFontSize` | `float` | `25` |
| `progressBarColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `progressBarBackgroundColor` | `Color` | `{r: 1, g: 1, b: 1, a: 0.05882353}` |
| `progressBarLoopBackgroundColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `progressBarLabelColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `scrollbarColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `scrollbarBackgroundColor` | `Color` | `{r: 1, g: 1, b: 1, a: 0.09803922}` |
| `sliderLabelFont` | `TMP_FontAsset` | `OpenSans-Semibold SDF` |
| `sliderLabelFontSize` | `float` | `24` |
| `sliderThemeType` | `UIManager.SliderThemeType` | `0` (serialized enum value) |
| `sliderColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `sliderBackgroundColor` | `Color` | `{r: 1, g: 1, b: 1, a: 0.09803922}` |
| `sliderLabelColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `sliderPopupLabelColor` | `Color` | `{r: 0.37254903, g: 0.40784314, b: 0.4509804, a: 1}` |
| `sliderHandleColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `switchBorderColor` | `Color` | `{r: 0.1764706, g: 0.25490198, b: 0.33333334, a: 1}` |
| `switchBackgroundColor` | `Color` | `{r: 0.05882353, g: 0.5882353, b: 0.99215686, a: 1}` |
| `switchHandleOnColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `switchHandleOffColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `toggleFont` | `TMP_FontAsset` | `OpenSans-Regular SDF_0` |
| `toggleFontSize` | `float` | `35` |
| `toggleThemeType` | `UIManager.ToggleThemeType` | `0` (serialized enum value) |
| `toggleTextColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `toggleBorderColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `toggleBackgroundColor` | `Color` | `{r: 1, g: 1, b: 1, a: 0.29411766}` |
| `toggleCheckColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `tooltipFont` | `TMP_FontAsset` | `OpenSans-Regular SDF_0` |
| `tooltipFontSize` | `float` | `22` |
| `tooltipTextColor` | `Color` | `{r: 1, g: 1, b: 1, a: 1}` |
| `tooltipBackgroundColor` | `Color` | `{r: 0.1764706, g: 0.25490198, b: 0.33333334, a: 1}` |

## Resolved MUIP TMP font references

The font GUIDs in `MUIP Manager.asset` resolve to these exported TMP font assets:

- `a780ff447d6376a468fdf97ccdf2f79c` → `OpenSans-Semibold SDF`
- `2619bf943ea2a6c489e4770dc738382a` → `OpenSans-Regular SDF_0`
- `a4fd64f402c64644f8c1d700fdf72ce5` → `Linotte-SemiBold SDF`
- `18805039800f8c94aa2424bddc12d58c` → `OpenSans-Bold SDF`
- `c4ace1422fb3b534984fcca6554d4994` → `OpenSans-Light SDF`

The corresponding role accessors are exposed by `VanillaUiFonts.GetMuipFont(VanillaMuipFontRole role)`. The framework also exposes the game's `Resources/fonts & materials/LiberationSans SDF` TMP asset, the legacy `Fonts`/`Font_Replacer` system, loaded fonts, OS fonts, and runtime TMP creation from a resolved `UnityEngine.Font`.

