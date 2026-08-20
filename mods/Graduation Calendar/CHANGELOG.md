# Graduation Calendar Changelog

## 0.3.9

- Changed only the calendar day-number badge surface from the shipped `button/rounded/White` visual to `button/basic/White`, reducing corner rounding while preserving the existing manual badge size and position controls.

## 0.3.8

- Added explicit manual constants for calendar day-number badge width, height, and top-left position within each day cell.
- Added independent number offsets measured from the badge rectangle's top-left corner, with top-left `RectTransform` anchoring so position edits behave predictably.

## 0.3.7

- Fixed the remaining rounded-sheet clipping mismatch by nesting the rectangular ScrollRect viewport under an invisible mask that uses the exact same sliced vanilla panel sprite and bounds as the visible white sheet. Scrolled portraits/content can no longer show through the panel sprite's transparent rounded-edge pixels, while the rounded corners remain visible.
- Moved the fixed circular scroll indicator inward from 16 UI units outside the white sheet to 8 UI units outside it, matching the requested popup-gutter position without consuming content width.
- Increased Monthly-view spacing between each month heading and its idol portrait grid.

## 0.3.6

- Rebalanced the calendar/list scroll sheet by extending its right edge and moving the native scroll indicator farther into the popup chrome, restoring matching visual breathing room on the left and right of the seven-column calendar.
- Restored gently rounded corners on the white scroll sheet by leaving the rounded vanilla panel surface visible and making the inner `RectMask2D` viewport a transparent clipper instead of an opaque square overlay.
- Added a small symmetric inset inside the rounded viewport so scrolled content stays clear of the curved sheet edges.
- Increased the Monthly-view gap between its year selector and the white results sheet so the selector no longer crowds the content surface.
- Added extra vertical separation between section headings and idol portrait grids in Graduations Only and Yearly views.

## 0.3.5

- Aligned the year previous/next controls to the same horizontal rails as the month controls and expanded the year-label center span, improving consistency and adding breathing room around the year number.
- Moved the producer-list scroll indicator outside the white calendar/list viewport, removed the now-unneeded right-side content gutter, and hid the Slider fill graphic so it reads as a fixed-thumb scrollbar rather than a value/progress slider.
- Switched the embedded framework viewport clipper from sprite-alpha `Mask` clipping to rectangular `RectMask2D` clipping so calendar cells, portraits, and section contents are cleanly cut at the visible sheet boundary instead of partially leaking beyond it.
- Increased left padding for section headers used by Graduations Only, Yearly, and Monthly views.
- Preserved the 0.3.4 bootstrap/menu-button injection correction.

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
