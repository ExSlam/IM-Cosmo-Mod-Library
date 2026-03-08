# IMDataCore Template Mod

Minimal example mod project that integrates with IM Data Core.

## What this includes

- Harmony bootstrap patch (`PopupManager.Start`).
- Namespace registration.
- Custom JSON set/get.
- Custom event append.
- Optional unregister on shutdown.

## Setup

1. Copy this folder to your own mod workspace.
2. Rename assembly and namespace as needed.
3. Build `IM Data Core` first so `com.cosmo.imdatacore.dll` exists under `..\..\bin\<Configuration>\net46\`.
4. Keep the IM Data Core DLL reference pointing to that built DLL, or replace it with your own local path.

## Required references

- `0Harmony.dll`
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `com.cosmo.imdatacore.dll`

All are wired in the template `.csproj` using `HintPath`, with the IM Data Core reference targeting the local build output.

## First things to customize

- `NamespaceIdentifier` in `TemplateDataCoreBridge`.
- Event names and key names to your mod domain.
- Harmony ID in your plugin bootstrap (if applicable).

## What you can rename safely

- `TemplateDataCoreBridge` class name.
- Helper method names (`InitializeIfAvailable`, `SaveSampleState`, etc.).
- Variable names (`sharedSession`, `errorMessage`).
- Patch class name.
- C# namespace and assembly name.

## What must stay exact

- `IMDataCoreApi`, `IMDataCoreSession`, and `IMDataCoreEvent` API type names.
- Harmony target symbols in `[HarmonyPatch(typeof(PopupManager), "Start")]`.
- The referenced IM Data Core DLL path must resolve to `com.cosmo.imdatacore.dll`.

## Harmony callback naming note

This template uses `[HarmonyPostfix]`, so callback method names can be changed safely.
If you remove that attribute, the method name must be exactly `Postfix`.

Full naming guide: `..\..\docs\NAMING_CONVENTIONS.md`
