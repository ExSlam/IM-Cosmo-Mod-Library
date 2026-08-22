# Dialogue Click Race Fix

`Dialogue Click Race Fix` prevents a late click during dialogue fade-out from advancing after the active dialogue node has already been cleared.

## Player-facing behavior

- Stops the closing-window click race that can lead to `GetNextSiblingNode` / `NullReferenceException` errors.
- Preserves legitimate transition, dramatic-CG, proceed-handler, and text-animation click behavior.
- Includes a narrow null-node traversal guard as defense in depth.

## Requirements

- Requires IM-HarmonyIntegration.

## Build

Project file:

- `mods/Dialogue Click Race Fix/Dialogue Click Race Fix.csproj`

Example command:

- `dotnet build "mods/Dialogue Click Race Fix/Dialogue Click Race Fix.csproj" -c Release`
