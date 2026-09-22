# Graduation Details

`Graduation Details` adds a graduated idol details popup with earnings, singles, and marriage info.

The current 1.2.1 Release build, compiled API binding and native Unity persistence checks pass against IMDataCore 3.4.34 (sidecar v6 / journal v3). Nonempty records survive actual Save & Quit/restart and standalone persistence after the codec repair described below. See [current compatibility evidence and remaining qualification](../IM%20Data%20Core/docs/V6V3_QUALIFICATION_STATUS.md).

## Player-facing behavior

- Adds a dedicated graduated-idol details view.
- Preserves richer post-graduation information than the base game normally exposes in one place.
- Stores former-idol records under the exact vanilla save being loaded or written. New snapshots preserve vanilla portrait asset references instead of copying portrait PNGs.
- Validates staff-to-idol identity before opening profiles, preventing portraits or records from
  leaking across saves.
- Graduated-profile rendering is fail-soft: matching live portrait identities stay on vanilla's
  single render path, while archival reconstruction validates exact sprite types and a body asset
  before queueing any detached portrait work.

## Save data

Version 1.2.1 uses an explicit JSON codec for both IMDC snapshots and standalone sidecars. Live Unity testing showed that the previous `JsonUtility` path could omit record collections while writing a valid-looking header. The new codec preserves nested records and exact 64-bit values, and rejects header-only or incomplete documents. Fields already omitted from an older file cannot be recovered from that file; no existing file is automatically converted or repaired.

When using standalone persistence, Graduation Details mirrors each supported vanilla save below a sibling directory in the game's
persistent data folder:

`C:\Users\<user>\AppData\LocalLow\Glitch Pitch\Idol Manager\GraduationDetails`

The complete vanilla path below `data` is preserved. There is no `Mods` or `saves` layer:

| Vanilla save | Graduation Details sidecar |
| --- | --- |
| `data\auto_save.json` | `GraduationDetails\auto_save.json` |
| `data\manual_saves\12\save.json` | `GraduationDetails\manual_saves\12\save.json` |
| `data\story_mode\Agency Name\auto_save.json` | `GraduationDetails\story_mode\Agency Name\auto_save.json` |
| `data\story_mode\Agency Name\manual_saves\AB12CD34\save.json` | `GraduationDetails\story_mode\Agency Name\manual_saves\AB12CD34\save.json` |
| `data\story_mode\Agency Name\chapter_1\save.json` | `GraduationDetails\story_mode\Agency Name\chapter_1\save.json` |

Each sidecar uses the named `GraduationDetails.LightweightSidecar` format. It contains only
sequenced Graduation Details mutations, exact vanilla-save checkpoints, and the supplemental
records that vanilla does not preserve. It never serializes a copy of Idol Manager's canonical
save state. Portrait identity is stored using the same sprite-type/asset-ID references vanilla
uses for normal idols, plus vanilla's custom idol type/addressable reference for unique idols.
Archived portraits are rendered through a detached vanilla-compatible girl shell so restoring a
historical portrait cannot rewrite the live idol's `Type` or `textureAssets`. Legacy copied
portrait files are read only as a compatibility fallback for older sidecars.

Every checkpoint also records the mods that were enabled at that save boundary, including the
mod folder identifier, display title, version, and Steam Workshop ID when available.

Changes remain in memory until one of Idol Manager's real save operations writes its vanilla
save. At that boundary Graduation Details records the vanilla relative path, real-world save
time, playtime, and in-game date. Loading requires that complete tuple to select the matching
supplemental checkpoint; sequence numbers order history but never decide which save is loaded.
New Save carries the active branch into the new exact vanilla slot; Overwrite Save updates the
checkpoint for the selected existing slot. When vanilla deletes a manual-save slot or story
playthrough, Graduation Details archives the matching standalone mirror as `.OLD`, `.OLD2`, and
so on instead of leaving stale state behind for a future slot reuse. The archive step never
touches the vanilla save file and never participates in SNLF/SWOF transport locking.

## Legacy migration

The lightweight format does not automatically import the older agency-keyed, fingerprinted, or
transactional layouts. Those files are left untouched. A missing sidecar starts writable-empty.
An existing sidecar that is corrupt, invalid, or has no exact checkpoint match starts empty but
read-only for that physical save path, so the original data cannot be overwritten. The writer
retains one `.graduationdetails.bak` recovery generation and can restore from it when the primary
sidecar cannot be activated.

With the IM Data Core 3.4.33-or-later sidecar-v6 / journal-v3 consumer contract, Graduation
Details binds only to the `com.cosmo.imdatacore` assembly and uses `IMDataCoreInteropApi` with
its own assembly supplied explicitly for namespace registration and custom-state access. When
IM Data Core is ready and writable, Graduation Details stores its detailed archival snapshot
inside IMDC's checkpointed custom state and leaves its standalone sidecar untouched. If IMDC is
absent, does not expose the complete current owner-safe interop contract, is persistence-blocked
for the active save, or has not yet taken ownership of Graduation Details state, standalone
persistence remains available. Once delegated state exists, a failed or invalid IMDC update
fails closed rather than creating a divergent standalone history.

Graduation Details does not call Save n Load Fixes or Save Write Ordering Fix directly. Those
mods coordinate the physical vanilla-save transport below IM Data Core; Graduation Details owns
only its domain state and its optional standalone mirror.

## Build

Project file:

- `mods/Graduation Details/Graduation Details.csproj`

Example command:

- `dotnet build "mods/Graduation Details/Graduation Details.csproj" -c Release`
