# Harmony Mod Browser 1.0.0 - implementation notes

## Architecture

The extraction follows a strict ownership rule:

- **HarmonyIntegration 1.2.0 owns execution infrastructure.** It discovers Harmony IDs/DLLs and applies/unapplies patches.
- **Harmony Mod Browser owns mod-management presentation.** It patches Idol Manager's native browser and Steam upload/update selection UI.
- **Idol Manager owns actual publishing.** This project deliberately calls the existing upload/update entry points rather than wrapping Steam UGC itself.

There is no source reference to a HarmonyIntegration type or API. The only contract is the standard `HarmonyID` + matching DLL filename convention used for all Harmony mods.

## Patch map

`Patches.cs` contains seven small patch entry points:

1. `Mods_Popup.Render` - substitutes the paged/searchable installed-mod browser. If controller initialization cannot find the expected vanilla layout, the prefix returns `true` and vanilla rendering proceeds.
2. `Mod_Button.RenderScreenshot` - suppresses synchronous screenshot loading only for cards managed by the enhanced browser; deferred loading supplies those thumbnails instead.
3. `Mod_Button.RenderTooltips` - suppresses redundant vanilla card-wide tooltips only for managed cards.
4. `Mods.LoadMods` - resets the Steam ownership-query state for a new load cycle.
5. `Mods.OnSteamRequestDone` - marks Workshop metadata complete and refreshes ownership-sensitive UI.
6. `Mods.OnSteamRequestFailed` - marks the query failed and refreshes ownership-sensitive UI without pretending unknown items are owned.
7. `Mods_Upload_Update.Set` - augments the vanilla local-version update chooser with search.

## Installed-mod browser

`ModBrowserController` operates directly on `Mods._Mods`.

- Page size: 24.
- Card creation budget: 4 per frame.
- Search debounce: 0.18 seconds realtime.
- Search is whitespace-AND across title, description, author, version, internal mod name, and tags.
- Source filtering uses `_mod.IsWorkshop()`.
- Vanilla `Mod_Button.Set(mod)` still renders button state and owns enable/disable and per-card Upload/Update behavior.
- Thumbnails are loaded sequentially via `UnityWebRequestTexture`, with the game's `IMG2Sprite` helper as fallback.
- Runtime-created sprites/textures are released when the popup is disabled or cards are replaced.

## Upload selector

`SelectionOverlayController` in `Upload` mode:

- Lists only `_mod` entries where `!mod.IsWorkshop()`.
- Supports search and pagination.
- Reuses the game's `Mods_Upload_Update_Button` visual prefab for selection cards.
- Replaces only that card's click listener, then calls `Mods_Popup.Upload(selectedMod)`.

No Steam create-item code lives in this project.

## Update target selector

`SelectionOverlayController` in `UpdateTarget` mode:

- Lists only `_mod` entries where `mod.IsWorkshop()` and `mod.SteamDetails.HasValue` and `mod.IsPlayerCreated()` is true.
- Never lists a Workshop item merely because it is installed/subscribed.
- Supports search and pagination.
- Rechecks ownership before opening the update flow.
- Calls `Mods_Popup.UploadUpdate(selectedOwnedWorkshopMod)`.

This implements the Steam ownership restriction at the UI level while retaining the game's publishing code.

## Asynchronous ownership metadata

The game's `_mod.IsPlayerCreated()` returns false until `SteamDetails` is populated. `SteamOwnershipMonitor` therefore distinguishes three states:

- ownership metadata still pending;
- ownership metadata completed;
- ownership metadata query failed.

The update selector can poll while a query is pending, but the authoritative refresh is driven by Harmony postfixes on the game's completion/failure callbacks. Unknown ownership never becomes an implicit yes.

## Local-version search in the update popup

The vanilla `Mods_Upload_Update.Set(target)` method renders every local mod as a possible source version. Its actual upload method contains the important target Workshop ID, change note, visibility, tags, and completion callback.

`UpdateSourceSearchController` therefore leaves `OnUpload` alone. It replaces only the rendered list after `Set` completes, filtering `!IsWorkshop()` items against the search query and binding the normal `Mods_Upload_Update_Button` back to the existing popup instance.

## Failure behavior

The intended fallback hierarchy is:

- If Harmony Mod Browser is absent/disabled: vanilla Idol Manager UI.
- If enhanced main-browser initialization fails: the `Mods_Popup.Render` prefix returns true, so vanilla render runs.
- If update-source search cannot attach: the postfix logs and leaves the vanilla list produced by `Set` in place.
- If Steam ownership lookup fails: the Update selector shows a warning and only exposes already-verified owned items.
- HarmonyIntegration is never required to know whether any of these UI features succeeded.

## Localization

Localization is intentionally self-contained. `Localization/en/strings.txt` is the fallback; supported overlays are `cn`, `jp`, `ru`, `ptbr`, `kr`, `fr`, and `es`.

The runtime resolves its own mod directory from the assembly path, with a fallback scan of `Mods._Mods` for `com.cosmo.harmonymodbrowser.dll`. There is no dependency on the separate Cosmo localization framework.

## Source-level verification

This package is intentionally not compiled in the provided environment. Verification is limited to source/static checks, including:

- balanced braces across source files;
- `HarmonyID`, assembly name, and version consistency;
- expected Harmony patch targets present;
- no source dependency on HarmonyIntegration implementation types;
- ownership selector contains an explicit `SteamDetails.HasValue` / `IsPlayerCreated()` gate;
- upload selector explicitly excludes Workshop entries;
- update-source list explicitly includes only local entries;
- UI localization keys present in the English fallback;
- no compiled DLL included in the package.

Runtime testing in Idol Manager is still required before publishing to Workshop.
