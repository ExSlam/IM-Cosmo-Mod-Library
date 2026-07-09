# Graduation Details

`Graduation Details` adds a graduated idol details popup with earnings, singles, and marriage info.

## Player-facing behavior

- Adds a dedicated graduated-idol details view.
- Preserves richer post-graduation information than the base game normally exposes in one place.
- Stores former-idol staff profile portraits under the active save and validates staff-to-idol
  identity before opening profiles, preventing portraits or records from leaking across saves.

## Build

Project file:
- `mods/Graduation Details/Graduation Details.csproj`

Example command:
- `dotnet build "mods/Graduation Details/Graduation Details.csproj" -c Release`
