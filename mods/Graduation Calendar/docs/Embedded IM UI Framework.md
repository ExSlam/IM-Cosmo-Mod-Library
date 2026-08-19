# Embedded IM UI Framework

Graduation Calendar 0.3.3 is self-contained. IM UI Framework v3.1.1-compatible source is vendored under `src/EmbeddedIMUiFramework/` and compiled into the Graduation Calendar DLL.

## Why

This avoids relying on Steam Workshop dependency propagation for players who subscribed to Graduation Calendar before the UI framework existed. It also means a missing standalone framework cannot break the calendar UI.

## Isolation rules

- Vendored namespace: `GraduationCalendar.EmbeddedIMUiFramework`
- No framework `PopupManager.Start`, `PopupManager.Close`, or `Popup.Hide` Harmony patches are included in the embedded copy.
- Graduation Calendar calls `IMUiKit.Initialize(__instance)` from its own `PopupManager.Start` postfix.
- The standalone `com.cosmo.imuiframework` assembly may still be installed for other mods; Graduation Calendar does not bind to it.
- Framework config/localization assets are not required by Graduation Calendar's embedded UI usage.

## Updating the embedded framework

When adopting a newer IM UI Framework release, replace the files in `src/EmbeddedIMUiFramework/`, change their namespace from `IMUiFramework` to `GraduationCalendar.EmbeddedIMUiFramework`, and keep the framework-global Harmony patch classes omitted. Then rebuild Graduation Calendar and test both with and without the standalone IM UI Framework installed.

## 0.3.3 embedded geometry additions

The vendored composer exposes per-scroll-view producer-slider right inset and viewport gutter values, month-pager label padding, and the shipped rounded-white visual helper. Graduation Calendar uses those options locally, so the standalone framework keeps vanilla defaults unless another caller opts into custom geometry.
