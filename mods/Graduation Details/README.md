# Graduation Details

`Graduation Details` adds a graduated idol details popup with earnings, singles, and marriage info.

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

Graduation Details mirrors each supported vanilla save below a sibling directory in the game's
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
checkpoint for the selected existing slot.

## Legacy migration

The lightweight format does not automatically import the older agency-keyed, fingerprinted, or
transactional layouts. Those files are left untouched. A missing sidecar starts writable-empty.
An existing sidecar that is corrupt, invalid, or has no exact checkpoint match starts empty but
read-only for that physical save path, so the original data cannot be overwritten. The writer
retains one `.graduationdetails.bak` recovery generation and can restore from it when the primary
sidecar cannot be activated.

When IM Data Core is enabled, ready, and writable, Graduation Details stores its detailed archival snapshot
inside IMDC's checkpointed custom state and leaves its standalone sidecar untouched. If IMDC is
not present, too old for the optional interop API, persistence-blocked for the active save, or has
not yet taken ownership of Graduation Details state, standalone persistence remains available. Once delegated state exists, a failed or
invalid IMDC update fails closed rather than creating a divergent standalone history.

## Build

Project file:

- `mods/Graduation Details/Graduation Details.csproj`

Example command:

- `dotnet build "mods/Graduation Details/Graduation Details.csproj" -c Release`
