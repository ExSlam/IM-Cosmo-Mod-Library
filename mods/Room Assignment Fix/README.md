# Room Assignment Fix

`Room Assignment Fix` prevents a vanilla room-input path or automatic assignment from assigning an idol to more than one room.

## Player-facing behavior

- Disables the obsolete room mouse-up assignment callback, which reads a stale idol ID and can
  assign idol 0 instead of the idol currently being dragged.
- Leaves manual assignment to `DragAndDropManager`, which uses the actual dragged idol and the
  normal `canAssign` and `canTrain` validation.
- Repairs stale idle-idol room pointers before assignment and retries a valid physical/mental
  clinic-slot drop if vanilla silently loses it.
- Guards all idol `agency._room.assign` calls, including auto-practice, against assigning an idol already owned by another room.
- Preserves vanilla treatment-room handling for sick idols.
- Allows announced graduates to keep receiving room assignments when another active patch,
  such as Unavailable Idols Fix, deliberately keeps them active until graduation finalizes.
- Keeps theater and breakroom/recreation-room ambient visuals synchronized with current room
  ownership, including when Tel's FastForward accelerates time enough that vanilla's
  real-time fadeouts or theater status buttons would otherwise show an idol in two rooms.
- Scales or skips remaining room sprite fade-in, sprite fade-out, and room pop-in animations
  while game time is running faster than vanilla fast speed. Room work progress bars are left
  alone because vanilla already drives them from game time through `TimeInterval`.
- Works independently of Assistant Manager and other Cosmo Mod Library mods.

## Build

Project file:
- `mods/Room Assignment Fix/Room Assignment Fix.csproj`

Example command:
- `dotnet build "mods/Room Assignment Fix/Room Assignment Fix.csproj" -c Release`
