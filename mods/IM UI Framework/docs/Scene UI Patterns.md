# Scene UI Patterns

IM UI Framework can use UI that Idol Manager serializes directly into a loaded scene even when that UI is not a `Resources` prefab. The source popup does **not** have to be open: `PopupManager.popups` keeps a serialized reference to each popup root, and `Transform.Find` can resolve inactive descendants.

## Producer list scroll indicator

`Scenes/main.unity` shows the same scroll arrangement in all three producer lists:

- `Producer_Contracts/Panel/Slider`
- `Producer_Salaries/Panel/Slider`
- `Producer_Loans/Panel/Credit History/Slider`

Hierarchy:

```text
Slider
├── Background
├── Fill Area
│   └── Fill
└── Handle Slide Area
    └── Handle
```

The list container itself has a `ScrollRect`, but its serialized `m_VerticalScrollbar` is `{fileID: 0}`. The scrollbar-looking control is a separate `UnityEngine.UI.Slider`.

### Event wiring

The producer `ScrollRect.onValueChanged(Vector2)` calls:

```text
SliderDefault.setValueFromVector(Vector2)
```

The `Slider.onValueChanged(float)` calls:

```text
ScrollRect.set_verticalNormalizedPosition(float)
```

The decompiled `SliderDefault.setValueFromVector` simply assigns `Slider.value = val.y` (with one small first-frame guard). Because the visible control is a `Slider`, Unity never applies the proportional `Scrollbar.size` calculation that creates an oversized pill-shaped thumb.

### Shared serialized geometry

All three producer lists use the same slider geometry:

| Property | Value |
|---|---:|
| Slider width | `20` |
| Slider local scale | `1.1004126` |
| Direction | `BottomToTop` |
| Min / Max / Value | `0 / 1 / 1` |
| Handle Slide Area sizeDelta.y | `-20` |
| Handle sizeDelta | `(-8.86, 11.14)` |
| Effective unscaled handle size | `11.14 × 11.14` |
| Background anchors X | `0.25 .. 0.75` |
| Background sizeDelta.x | `-5.79` |
| Effective unscaled background width | `4.21` |

The slider center sits about `19.34` UI units in from the panel's right edge, while the list viewport ends about `25.6` UI units in from that edge. `VanillaUiSceneTemplates` exposes rounded constants for these dimensions and automatically reserves the viewport gutter.

### Shared visuals

The background image is serialized with:

```text
RGB = 225, 222, 221
Hex = #E1DEDD
Sprite GUID = 5182040c268a5c74ab094a71cfb41bfa
```

The handle image is white-tinted, so its purple appearance comes from the sprite itself:

```text
Sprite GUID = dc4197232f020ba489786de68afe1c8e
```

Cloning the scene object is therefore more accurate than recoloring the generic MUIP `Resources/scrollbar/Scrollbar` prefab: it preserves the exact game sprite, fixed circular handle, thin grey track, `ButtonDefault` behavior, and Slider geometry.

## Runtime access while the popup is closed

Use:

```csharp
GameObject root;
VanillaUiSceneTemplates.TryGetPopupRoot(
    PopupManager._type.producer_contracts,
    out root);
```

This does not call `PopupManager.Open`, does not activate `Producer_Contracts`, and does not display it. The framework reads the already-loaded scene reference from `PopupManager.GetByType(...)`.

To create a bound copy:

```csharp
GameObject indicator;
Slider slider;
VanillaUiSceneTemplates.TryCreateProducerListScrollSlider(
    scrollRect.transform,
    scrollRect,
    "Slider",
    out indicator,
    out slider);
```

The framework clears the clone's persistent event that points back to the source popup, binds the copy to the requested `ScrollRect`, leaves `ScrollRect.verticalScrollbar` null, and keeps the cloned slider height synchronized with the target list.

## Relationship to Michsky Modern UI Pack

This scene layer does not replace `VanillaUiResources`. Idol Manager uses both kinds of UI:

- **MUIP Resources controls:** buttons, input fields, dropdowns, sliders, modal windows, the generic `Resources/scrollbar/Scrollbar`, and other prefabs that Unity can load through `Resources.Load`.
- **Idol Manager scene composites:** arrangements whose exact hierarchy, sprites, dimensions, and event wiring live directly in a scene/popup.

IM UI Framework 2.1 exposes both, so a mod can choose the same source the base game uses for the UI pattern it is trying to reproduce.
