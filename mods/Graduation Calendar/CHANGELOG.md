# Graduation Calendar Changelog

## 0.3.2

- Updated the vendored UI runtime to the IM UI Framework 3.1 compatibility code.
- Fixed compilation against Idol Manager's older Unity scene/event/UI API surface without adding a standalone framework dependency.
- Added the embedded `IMUiCompat` shim for inactive-child lookup, loaded-scene discovery, persistent UnityEvent inspection, and read-only `ColorBlock` state mutation.
- Removed the embedded compile-time dependency on `TMPro.TMP_Dropdown`.

## 0.3.1

- Embedded the IM UI Framework v3 source directly into Graduation Calendar.
- Removed the standalone IM UI Framework project dependency.
- Isolated the vendored framework under `GraduationCalendar.EmbeddedIMUiFramework`.
- Omitted framework-global Harmony patches in the embedded copy to avoid duplicate patches if the standalone framework is also installed.
- Initialize the embedded `IMUiKit` from Graduation Calendar's existing `PopupManager.Start` patch.

# Changelog

## 0.3.0

- Added IM UI Framework v3 dependency.
- Replaced custom month/year arrow construction with the exact vanilla Singles-chart month navigator controls.
- Replaced the custom Unity Scrollbar with the vanilla producer-list Slider indicator pattern.
- Replaced calendar cell `Outline` borders with vanilla sliced card/panel styling.
- Replaced the hand-built ScrollRect/viewport/content/scrollbar setup with `IMUiComposer.TryCreateScrollView`.
