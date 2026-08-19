# Graduation Calendar

`Graduation Calendar` shows graduation dates in Extras and adds a calendar popup.

## Player-facing behavior

- Surfaces upcoming graduation timing directly in idol profile extras.
- Adds a dedicated calendar view for browsing scheduled graduations.
- Fits month navigation to the active language's longest month name, keeps the arrows close to the label, and gives year/month labels a little more breathing room.
- Uses a brighter popup sheet, a farther-right native scroll indicator, and smaller rounded day-number badges aligned to each calendar cell.


## Embedded IM UI Framework v3

Version 0.3.3 vendors the IM UI Framework v3.1.1-compatible source directly into the Graduation Calendar assembly. The calendar therefore does **not** require the standalone IM UI Framework Workshop item to be installed.

The vendored copy lives under `src/EmbeddedIMUiFramework/` and is compiled into `com.cosmo.graduationcalendar.dll`. Its namespace is changed to `GraduationCalendar.EmbeddedIMUiFramework`, and the framework-global Harmony patches are intentionally omitted. Graduation Calendar initializes the embedded `IMUiKit` from its own existing `PopupManager.Start` patch. This prevents duplicate framework patches or runtime-type collisions when a player also happens to have the standalone IM UI Framework installed for another mod.

The calendar still uses the exact Singles-chart previous/next navigation controls, the Contracts/Salaries/Loans fixed circular list-scroll indicator, and vanilla sliced card surfaces for calendar cells. The ScrollRect setup remains delegated to the framework, reducing popup boilerplate.

## Repo Notes

- Runtime assets live under `assets/`.
- Screenshots moved into `docs/images/`.

## Build

Project file:
- `mods/Graduation Calendar/Graduation Calendar.csproj`

Example command:
- `dotnet build "mods/Graduation Calendar/Graduation Calendar.csproj" -c Release`
