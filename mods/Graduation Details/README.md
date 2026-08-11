# Graduation Details

`Graduation Details` adds a graduated idol details popup with earnings, singles, and marriage info.

## Player-facing behavior

- Adds a dedicated graduated-idol details view.
- Preserves richer post-graduation information than the base game normally exposes in one place.
- Stores former-idol records and portraits under the exact vanilla save being loaded or written.
- Validates staff-to-idol identity before opening profiles, preventing portraits or records from
  leaking across saves.

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
save state. Identity-named portrait files are stored beside the sidecar in a matching
`<save-name>.portraits` directory.

Changes remain in memory until one of Idol Manager's real save operations writes its vanilla
save. At that boundary Graduation Details records the vanilla relative path, real-world save
time, playtime, and in-game date. Loading requires that complete tuple to select the matching
supplemental checkpoint; sequence numbers order history but never decide which save is loaded.
Save As carries the active branch into the new exact vanilla slot.

## Legacy migration

The lightweight format does not automatically import the older agency-keyed, fingerprinted, or
transactional layouts. Those files are left untouched. A missing, invalid, or non-matching
sidecar loads as safe empty supplemental state and never blocks the vanilla save from loading.

## Build

Project file:

- `mods/Graduation Details/Graduation Details.csproj`

Example command:

- `dotnet build "mods/Graduation Details/Graduation Details.csproj" -c Release`
