# Room Assignment Fix

`Room Assignment Fix` prevents a vanilla room-input path or automatic assignment from assigning an idol to more than one room.

## Player-facing behavior

- Uses the same `canAssign` and `canTrain` validation as normal drag-and-drop for mouse assignments.
- Prevents the legacy room mouse-up handler from bypassing those checks.
- Guards all idol `agency._room.assign` calls, including auto-practice, against assigning an idol already owned by another room.
- Preserves vanilla treatment-room handling for sick idols.
- Works independently of Assistant Manager and other Cosmo Mod Library mods.

## Build

Project file:
- `mods/Room Assignment Fix/Room Assignment Fix.csproj`

Example command:
- `dotnet build "mods/Room Assignment Fix/Room Assignment Fix.csproj" -c Release`
