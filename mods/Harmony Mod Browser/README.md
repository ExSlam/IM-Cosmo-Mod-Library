# Harmony Mod Browser 1.0.0

Source-only package for the standalone Idol Manager **Harmony Mod Browser** Harmony mod.

**Author: Cosmo**

This project is the UI half extracted from the HarmonyIntegration fork. HarmonyIntegration 1.2.0 remains the small bootstrap/loader; Harmony Mod Browser is an ordinary Idol Manager Harmony mod that can be distributed and updated through Steam Workshop.

## Dependency model

```text
Idol Manager
    -> HarmonyIntegration 1.2.0+
        -> Harmony Mod Browser 1.0.0
```

The dependency is intentionally one-way. Harmony Mod Browser does **not** call private APIs in HarmonyIntegration and does not require a shared library from it. It reads Idol Manager's native `Mods._Mods` collection and patches the game's UI directly.

If Harmony Mod Browser is disabled or removed, Idol Manager falls back to its vanilla mod UI. HarmonyIntegration continues loading other Harmony mods.

## What 1.0.0 changes

### Installed-mod browser

- Replaces the vanilla all-at-once mod-card render with a paged browser.
- Searches native Idol Manager mods by title, description, author, version, internal mod name, and tags.
- Cycles source filters: **All Mods -> Local -> Workshop**.
- Uses 24 cards per page and creates cards incrementally.
- Loads thumbnails sequentially/deferred instead of synchronously loading every thumbnail at once.
- Adds version and Local/Workshop metadata to each card.
- Preserves each vanilla `Mod_Button`, including enable/disable and per-card Upload/Update actions.
- Re-renders ownership-sensitive cards when Steam Workshop details arrive.

### Upload a new Workshop mod

A global **Upload Mod** button opens a searchable selector containing **local mods only**. Selecting a mod hands off to Idol Manager's vanilla `Mods_Popup.Upload(mod)` flow. The mod does not reimplement Steam `CreateItem` behavior.

### Update an existing Workshop mod

A global **Update Mod** button opens a searchable selector containing only Workshop items that are verified as owned by the currently signed-in Steam account.

Ownership uses Idol Manager's own `Mods._mod.IsPlayerCreated()` result. Items with unavailable/pending Steam details are never treated as owned by assumption.

After a target is selected, Idol Manager's normal update popup opens. Harmony Mod Browser adds a second search bar to that popup's local-version list, so the user can quickly choose which local copy/version should be uploaded.

The actual update still runs through vanilla `Mods_Upload_Update.OnUpload`, preserving the game's change note, visibility, Steam update call, progress state, and success/error handling.

## Steam ownership timing

Idol Manager loads Workshop item metadata asynchronously. A Workshop mod can exist in `Mods._Mods` before its `SteamDetails` field is populated. Since `IsPlayerCreated()` returns false when those details are missing, treating the first UI frame as authoritative would temporarily hide all owned items.

Harmony Mod Browser therefore patches the game's Steam-details completion/failure callbacks and refreshes ownership-sensitive UI when metadata arrives. While details are pending, the Update selector reports that ownership is loading. On query failure it shows only items whose ownership was already verified.

## Source layout

```text
assets/
  info.json
  Localization/<language>/strings.txt
source/
  HarmonyModBrowser.csproj
  Patches.cs
  BrowserCore.cs
  RuntimeUi.cs
  ModBrowserController.cs
  SelectionOverlayController.cs
  UpdateSourceSearchController.cs
```

`HarmonyID` is `com.cosmo.harmonymodbrowser`, so the compiled assembly must be named:

```text
com.cosmo.harmonymodbrowser.dll
```

That name is already configured as the project `AssemblyName`.

## Building elsewhere

This package deliberately contains **no compiled DLL**.

The project targets .NET Framework 4.6, matching Idol Manager Harmony mods. Point the `dllDir` MSBuild property at a directory containing the game's managed DLLs and Harmony, for example:

```text
/p:dllDir=C:\path\to\idol-manager-dlls
```

The required references are declared in `source/HarmonyModBrowser.csproj`. The provided `dll(1).zip` from the development/reference set contains those dependencies, but game/runtime DLLs are not redistributed inside this source package.

After compiling elsewhere, a deployable mod folder should contain at minimum:

```text
Harmony Mod Browser/
  info.json
  com.cosmo.harmonymodbrowser.dll
  Localization/
    en/strings.txt
    ...
```

A `thumb.png` can be added later for the Workshop/listing artwork.

## Design boundary

Harmony Mod Browser is an enhanced front end for **Idol Manager's native mod system**, not a UI for HarmonyIntegration itself. Normal data mods and Harmony mods share the same browser. Harmony-specific loading, duplicate-HarmonyID resolution, DLL loading, patch application, and patch removal stay in HarmonyIntegration 1.2.0.

See `IMPLEMENTATION_NOTES.md` for the patch map, failure behavior, and source-level verification performed for this package.
