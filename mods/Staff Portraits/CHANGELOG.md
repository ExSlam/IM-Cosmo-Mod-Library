# Changelog

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
