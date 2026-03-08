# Staff Firing Freeze Fix

`Staff Firing Freeze Fix` prevents room and business proposal state from getting stuck when staff are fired or severed mid-task.

## Player-facing behavior

- Clears deferred room task state before staff removal.
- Prevents business proposal rooms from entering invalid processing state when no staffer remains assigned.

## Build

Project file:
- `mods/Staff Firing Freeze Fix/Staff Firing Freeze Fix.csproj`

Example command:
- `dotnet build "mods/Staff Firing Freeze Fix/Staff Firing Freeze Fix.csproj" -c Release`
