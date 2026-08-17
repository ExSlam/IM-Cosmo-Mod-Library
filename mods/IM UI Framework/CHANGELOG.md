# Changelog

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
