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
3. Build `IM Data Core` first so the packaged artifact exists under `..\..\..\..\artifacts\mods\<Configuration>\IM Data Core\`.
4. Keep the IM Data Core DLL reference pointing to that packaged artifact, or override `IMDataCoreArtifactDir` if your workspace uses a different artifacts root.

## Required references

- `0Harmony.dll`
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `com.cosmo.imdatacore.dll`

All are wired in the template `.csproj` using `HintPath`, with the IM Data Core reference targeting the local build output.
In this repo, "local build output" means the packaged artifact folder under `artifacts\mods\<Configuration>`, not the raw per-project `bin` folder.

## Live deploy note

Packaged builds stay under `artifacts\mods\<Configuration>`.
For a live install into Idol Manager, deploy the packaged artifact into `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager\Mods` instead of redirecting package output into the game folder.
This repo's `scripts\Build-And-Deploy-Mods.ps1` does that and can target a live mod folder name that differs from the repo folder name.

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
