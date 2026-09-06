# Assistant Manager

`Assistant Manager` adds dedicated Assistant Manager Offices and staff who can share eligible office work with the Producer.

## Player-facing behavior

- Build up to two Assistant Manager Offices and hire up to two Assistant Managers.
- Assistant Managers can handle eligible office work independently of the Producer.
- Keeps Assistant Manager date and audition cooldowns separate and prevents overlapping auditions.
- Supports custom Assistant Manager portraits and Idol Career Diary integration.

## Compatibility

- Requires IM-HarmonyIntegration.
- Room Assignment Fix and Staff Firing Freeze Fix are recommended.
- IM Data Core integration is optional. When present, Assistant Manager uses the IMDataCore v6 explicit-owner interop facade for its supplemental office/cooldown state.
- Assistant Manager does not call Save n Load Fixes or Save Write Ordering Fix directly; vanilla-save transport remains below IM Data Core.

## Build

Project file:

- `mods/Assistant Manager/Assistant Manager.csproj`

Example command:

- `dotnet build "mods/Assistant Manager/Assistant Manager.csproj" -c Release`
