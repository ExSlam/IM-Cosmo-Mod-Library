# Single Marketing Checkbox Fix

`Single Marketing Checkbox Fix` corrects a visual-only vanilla UI issue where the Marketing checklist row on an in-development single can remain inactive after the Releases tab/list is refreshed.

## Player-facing behavior

- Restores the Marketing row when the single actually has marketing selected.
- Keeps the row hidden for singles that do not have marketing selected, matching vanilla intent.
- Refreshes visible in-development single cards when the Singles/Releases tab is reopened.
- Does not change single data, marketing progress, release validation, or save files.

## Build

Project file:
- `mods/Single Marketing Checkbox Fix/Single Marketing Checkbox Fix.csproj`

Example command:
- `dotnet build "mods/Single Marketing Checkbox Fix/Single Marketing Checkbox Fix.csproj" -c Release`
