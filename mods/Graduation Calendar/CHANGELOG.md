# Graduation Calendar Changelog

## 0.3.4

- Fixed a bootstrap regression in 0.3.3 that could prevent the Graduation Calendar menu icon from being injected. Localized month-width measurement ran before `selectedYear` was initialized and attempted to construct a `DateTime` with year 0.
- Made localized month-width measurement independent of gameplay state by using a fixed valid reference year.
- Hardened initialization so the menu button is injected before popup construction, UI exceptions are logged, and the retry bootstrap cannot remain permanently locked after an exception.

## 0.3.3

- Moved the scene-native producer-list scroll indicator farther right for the calendar while keeping a safe content gutter.
- Sized month navigation from the longest localized month name plus small explicit padding, and tightened only the space between the previous/next arrow controls and the label.
- Increased month/year label breathing room without hard-coding English month widths.
- Restored the calendar popup to a brighter near-white surface treatment.
- Reduced and re-aligned day-number badges and reused the shipped rounded white button visual for softer rounded corners.
- Updated the vendored IM UI Framework helpers with per-scroll-view indicator geometry, month-label padding, and rounded-white visual reuse.

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
