# Changelog

## Unreleased

- Removed the redundant global Upload Mod and Update Mod buttons; kept each mod card's native actions.
- Moved the All Mods / Local / Workshop filter opposite Back and reduced the search header to reclaim list space.
- Added thumbnail-only mod details with the full title, thumbnail, version, author and a scrollable, unabridged description.
- Added incremental glyph warming for installed mod metadata and fallback fonts, refreshed when the language changes.
- Reserved separate space for search and pagination outside the scrolling mod list.
- Made browser and upload/update selection grids fit their viewport width, with bounded, wrapping card text.
- Reused native game buttons and scrollbars, and applied the selected game font to browser text.
- Kept selection panels opaque and refreshed scrolling correctly after filtering or paging.
- Fixed release compilation against the supplied game assemblies and embedded the library's shared localization implementation.

## 1.0.0

Initial standalone release extracted from the UI responsibilities formerly carried by the 1.1.0 Cosmo HarmonyIntegration fork.

- Added paginated installed-mod browser with search and Local/Workshop source filtering.
- Added deferred sequential thumbnail loading.
- Added version and source metadata to mod cards.
- Preserved vanilla enable/disable and per-card Upload/Update actions.
- Added global searchable Upload Mod selector limited to local mods.
- Added global searchable Update Mod selector limited to Workshop items owned by the current Steam account.
- Added refresh handling for asynchronously loaded Steam Workshop ownership metadata.
- Added search to the local-version list shown when updating a Workshop item.
- Kept Steam create/update operations in Idol Manager's vanilla upload/update classes.
- Added self-contained localization files for English, Chinese, Japanese, Russian, Brazilian Portuguese, Korean, French, and Spanish.
