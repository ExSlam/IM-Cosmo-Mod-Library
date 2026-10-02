# Staff Portraits

**Author: Cosmo**

Staff Portraits gives ordinary agency staff composite portraits assembled from a dedicated staff-only asset pool. It also adds a **Staff Stylist** action to the Mod Buttons Action Hub so the player can choose a staff member, select a portrait pack, change outfit/body, hair, face, and accessory parts, preview the result, randomize a combination, and apply it.

Cosmo is the human author and namesake of this mod. In the implementation, the Staff Portraits assembly, Harmony patches, asset catalog, assignment service, and Staff Stylist UI perform the runtime work described below.

## Requirements

- Idol Manager with Idol Manager Harmony Integration.
- **Mod Buttons** is required to open Staff Stylist from the Action Hub.
- At least one enabled mod containing a valid `StaffPortraits/pack.json` is required before Staff Portraits can replace or edit staff portraits.

The required IM UI Framework functions and the shared localization runtime are embedded in the Staff Portraits assembly, so the standalone IM UI Framework and Mod Localization System Workshop items are not runtime dependencies.

## Player behavior

When a normal staff member is hired, Staff Portraits checks whether the staff member already has a composite portrait. If not, and a valid staff portrait pack is available, the mod assigns a random body, hair, and face from one pack and may also assign an accessory. Existing saves are checked after vanilla staff loading completes so ordinary staff without composite portraits can receive the same treatment.

Staff Portraits deliberately excludes:

- former idols who became staff, because their idol portrait data should remain theirs;
- unique named staff, because their authored appearance should remain intact;
- the player/producer staff record; and
- staff who already have any composite portrait data.

Open **Action Hub > Staff Stylist** to browse all hired non-producer staff in the mini-portrait picker. Each entry shows its name and translated job title beneath the portrait, and the selected entry is highlighted. Former idols, unique staff, and staff with other authored portraits remain visible for inspection with portrait editing disabled. Ordinary staff using this mod's own portrait parts remain editable. Hover an entry to see the game's staff card with its portrait, skill stars, name, and current assignment. The editor changes its preview first. **Apply** writes the selected staff-only texture assets to that staff member's existing `textureAssets` field. The mod then refreshes the staff texture IDs, queues the composite portrait through the same renderer used by vanilla staff loading, and refreshes staff portrait views when that request finishes. An older render request cannot refresh a newer assignment.

If no valid staff portrait pack is installed, Staff Portraits leaves existing staff portraits unchanged and the Staff Stylist shows a localized explanation instead of borrowing assets from the idol pool.

## Staff portrait packs

Staff portrait packs are ordinary enabled Idol Manager mods that contain this structure:

```text
StaffPortraits/
  pack.json
  Textures/
    body/
    hair/
    face/
    acc/
```

See [docs/STAFF_PORTRAIT_PACKS.md](docs/STAFF_PORTRAIT_PACKS.md) for the complete manifest format and validation rules.

The important separation is architectural: `StaffPortraitCatalog` creates private runtime texture-asset objects for staff portrait packs and does **not** insert them into Idol Manager's global idol texture list. A namespaced save/load bridge resolves only IDs beginning with `staffportraits:1:`. All other portrait IDs continue through vanilla lookup behavior.

## Persistence

Vanilla staff saving serializes each composite texture asset by calling the asset's `GetID()` method. For texture assets owned by Staff Portraits, `textureAsset_GetID_StaffPortraitsPatch` substitutes a stable namespaced ID based on pack ID, part type, and asset ID.

During load, `data_girls_textures_GetTextureAssetByID_StaffPortraitsPatch` handles only those namespaced IDs and resolves them from the enabled staff portrait packs. Non-Staff-Portraits IDs are not changed by this patch.

A save that uses a third-party staff portrait pack should keep that pack enabled. If the pack is unavailable when a save is loaded, the namespaced lookup patch returns no staff-only runtime asset instead of allowing vanilla's same-type fallback to substitute an idol asset. Do not save over that game state unless the missing pack has been restored. Once the same pack is available again, loading the save can resolve its stable pack and asset IDs normally.

Cosmo Mod Library's separate **Save n Load Fixes** mod contains its own general repair for unresolved external portrait identity. Staff Portraits does not require that mod and does not claim its repair behavior as part of Staff Portraits.

## UI implementation

Staff Stylist uses an embedded, mod-specific copy of the IM UI Framework components used elsewhere in Cosmo Mod Library. The editor is constructed on first opening, after native button templates are available. The popup is registered with `PopupManager` so normal popup queueing, input blocking, blur/darken behavior, and close handling remain under the game's popup lifecycle.

The picker alone scrolls. Its viewport and native indicator align with the preview and selector columns, and its content height follows the full grid. The indicator is disabled when the list fits. Close, Randomize, and Apply share one footer row; Close discards unapplied preview choices. The preview's empty-state message and selector text wrap inside fixed column bounds. Action buttons clone the Settings-tab scene controls using the same binding reset as Cheats Mod and Mod Buttons. Selectors preserve the Singles-chart arrow glyphs and icon font. Picker entries call TooltipManager.ShowStatusButtonTooltip's staff overload; closing the popup or leaving a clipped entry stops the hover preview. Preview textures are released when the popup closes. All Staff Portraits player-facing strings are loaded through the embedded localization runtime.

## Localization

The mod supplies `strings.txt` for all seven library languages:

- English (`en`)
- Simplified Chinese (`cn`)
- French (`fr`)
- Japanese (`jp`)
- Korean (`kr`)
- Brazilian Portuguese (`ptbr`)
- Russian (`ru`)

`info.json` itself supports only Idol Manager Harmony Integration's normal metadata fields. The current repository format has no verified per-language `info.json` title/description field, so this mod does not invent one. Localized Workshop-description source files are included under each language directory for publishing workflows that support localized Steam descriptions.

## Build

The project uses the repository-level `Directory.Build.props` and expects the same Idol Manager DLL layout as the other Cosmo Mod Library projects.

```text
dotnet build "mods/Staff Portraits/Staff Portraits.csproj" -c Release
```

The project assembly name and Harmony ID are both `com.cosmo.staffportraits`. The current mod version is `1.0.2`. The runtime package includes the Release DLL, mod metadata, thumbnail, Mod Buttons action, all seven localizations, Workshop description assets, and license. Source, tests, build intermediates, debug symbols, project files, README, and changelog are excluded from the live mod folder.
