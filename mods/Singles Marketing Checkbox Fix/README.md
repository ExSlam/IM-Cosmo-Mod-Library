# Singles Marketing Checkbox Fix

`Singles Marketing Checkbox Fix` corrects a visual-only vanilla UI issue where the Marketing checklist row disappears from an in-development single when no marketing campaign was selected.

## Player-facing behavior

- Keeps the Marketing row visible for every in-development single.
- Shows the row unchecked when no marketing campaign was selected.
- Continues to show marketing production progress when a campaign was selected.
- Refreshes visible in-development single cards when the Singles/Releases tab is reopened.
- Does not change single data, marketing progress, release validation, or save files.

## Build

Project file:
- `mods/Singles Marketing Checkbox Fix/Singles Marketing Checkbox Fix.csproj`

Example command:
- `dotnet build "mods/Singles Marketing Checkbox Fix/Singles Marketing Checkbox Fix.csproj" -c Release`
