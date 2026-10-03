# Changelog

## 1.0.4 - 2026-10-02

- Keep the Pack Source arrows enabled for editable staff even when the current source has no discovered packs, so a player with no dedicated Staff Packs can still switch to Unique Idol Packs.
- Refresh the loaded unique-idol catalog when switching to Unique Idol Packs.
- Stop applying the dedicated Staff Pack 1024 x 1500 file-size contract to already-loaded unique-idol assets; the game's own unique-idol loader is the authority for those source assets.
- Show a source-specific empty-state message after switching instead of the generic no-packs message.

## 1.0.3 - 2026-10-02

- Added a Staff Stylist pack-source selector that switches between dedicated Staff Packs and compatible loaded Unique Idol Packs.
- Group unique-idol portrait parts by their loaded source mod and body ID, reuse the source assets directly, and keep the source mod's own texture IDs for vanilla save/load persistence.
- Keep automatic hire/backfill portrait assignment restricted to dedicated staff packs; unique-idol artwork is used only when the player selects it in Staff Stylist.
- Keep staff editable after applying a compatible unique-idol portrait while continuing to protect former idols, unique staff, and unrelated authored portraits.
- Replaced the picker grid's deferred ContentSizeFitter sizing with deterministic fixed-grid content height so the staff list is scrollable on the first popup open whenever its rows exceed the viewport.
- Added and repaired Staff Stylist localization strings across all seven supported languages.

## 1.0.2 - 2026-10-02

- Show each staff member's translated job title below their name in the picker.
- Include all hired non-producer staff in the picker. Former idols, unique staff, and staff with other authored portraits remain view-only.
- Keep ordinary staff with this mod's portrait parts editable after applying a portrait; vanilla's IsIdol flag also identifies staff with any composite texture data.
- Size the scroll content from the complete grid and disable the scroll indicator when the list fits.
- Align the staff viewport and its scroll track with the preview and selector columns.
- Place Close immediately to the left of Randomize on the same footer row.
- Construct the editor when its action opens so native button templates are available after game UI initialization.
- Translate the protected-portrait explanation and unavailable-job fallback in all seven languages.

## 1.0.1 - 2026-10-02

- Replaced previous/next staff navigation with a scrollable mini-portrait picker, wrapped name captions, and a visible selection highlight.
- Added native staff-card hover previews showing staff portraits, skill stars, names, and current assignments.
- Bounded the preview, empty-state message, and each selector row within separate columns to prevent overlapping text.
- Replaced generic resource action buttons with native Settings-tab scene buttons and preserved the native chart-arrow glyphs.
- Added all new picker labels and selector tooltips in the seven supported languages.
- Generate composite thumbnails after applying portrait parts so the picker and staff cards can refresh without reloading the save.

## 1.0.0

- Fixed Staff Stylist compilation against the game's Unity assemblies by using GameObject.GetComponentInChildren for inactive button-label lookup.
- Added a staff-only composite portrait asset catalog with `StaffPortraits/pack.json` manifests.
- Added random portrait assignment for newly hired ordinary staff when a valid staff portrait pack is available.
- Added backfill for eligible ordinary staff in existing saves after vanilla staff loading completes.
- Added Staff Stylist, opened through the Mod Buttons Action Hub, with staff navigation, portrait-pack selection, body/outfit, hair, face, accessory selectors, preview, randomize, and apply actions.
- Added stable namespaced portrait IDs so staff-only texture parts can use vanilla staff save/load data without being inserted into the idol texture pool.
- Excluded the player/producer, former-idol staff, unique named staff, and staff who already have any composite portrait data from automatic replacement.
- Added embedded IM UI Framework support and seven-language localization.
- Added documentation for third-party staff portrait pack creators, including the required 1024 x 1500 layer canvas.
