# Idol Career Diary

`Idol Career Diary` adds a profile-integrated timeline view that turns IM Data Core events into readable career history.

## Dependencies

- `IM Data Core` (`com.cosmo.imdatacore`) version `1.0.0` or higher
- `IM UI Framework` (`com.cosmo.imuiframework`) version `1.0.0` or higher

This mod does not ship a separate persistence backend. It reads timeline events from IM Data Core and renders UI with IM UI Framework.

## Player-facing behavior

- Adds a dedicated career diary view in idol profile flow.
- Reads recent timeline events for the selected idol.
- Groups and formats event types (career, contracts, singles, shows, finance, relationships, and more).
- Keeps timeline rendering stable when dependencies are present; shows dependency errors when required mods are missing.

## Installation

1. Install `IM Data Core` first.
2. Install `IM UI Framework` second.
3. Install `Idol Career Diary`.
4. Launch game and open an idol profile to verify diary UI appears.

## 1.0 release contract

- Runtime behavior and user-facing diary feature set are considered stable in `1.x`.
- Dependency requirement remains hard: missing IM Data Core or IM UI Framework is an install error.
- Save compatibility is inherited from IM Data Core event schema and namespace usage.

## Troubleshooting

- If diary UI is missing, confirm both dependency mods are installed and loaded.
- If timeline is empty on older saves, continue gameplay to generate new captured events.
- If dependency errors appear, check `info.json` Harmony IDs and matching DLL names in mod folders.

## Build

Project file:
- `mods/Idol Career Diary/Idol Career Diary.csproj`

Example command:
- `dotnet build "mods/Idol Career Diary/Idol Career Diary.csproj" -c Release`
