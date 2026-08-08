# Graduation Details

`Graduation Details` adds a graduated idol details popup with earnings, singles, and marriage info.

## Player-facing behavior

- Adds a dedicated graduated-idol details view.
- Preserves richer post-graduation information than the base game normally exposes in one place.
- Stores former-idol records and portraits under the exact vanilla save being loaded or written.
- Validates staff-to-idol identity before opening profiles, preventing portraits or records from
  leaking across saves.

## Save data

Graduation Details mirrors each vanilla save below the game's persistent data folder:

`C:\Users\<user>\AppData\LocalLow\Glitch Pitch\Idol Manager\GraduationDetails\saves`

The vanilla path below `data` is preserved. A terminal `save.json` is represented by its parent
directory; meaningful filenames such as `auto_save.json` and `manual_save.json` retain their stem.
For example:

| Vanilla save | Graduation Details directory |
| --- | --- |
| `data\manual_saves\12\save.json` | `GraduationDetails\saves\manual_saves\12` |
| `data\story_mode\Agency Name\auto_save.json` | `GraduationDetails\saves\story_mode\Agency Name\auto_save` |
| `data\story_mode\Agency Name\manual_saves\AB12CD34\save.json` | `GraduationDetails\saves\story_mode\Agency Name\manual_saves\AB12CD34` |
| `data\story_mode\Agency Name\chapter_1\save.json` | `GraduationDetails\saves\story_mode\Agency Name\chapter_1` |

Each directory can contain `marriage_data.json`, `staff_idol_map.json`,
`graduation_snapshots.json`, and a `Portraits` directory. Save As writes a complete snapshot of
the current in-memory records into the new vanilla slot and then binds future changes to it.

## Legacy migration

When a vanilla save is successfully loaded, the mod looks for its historical agency/fallback and
save-owner keys in these legacy locations:

- `...\Idol Manager\GraduationDetails\saves\<legacy-key>` and the older direct keyed form.
- `...\Idol Manager\Mods\GraduationDetails\...` and
  `...\Idol Manager\Mods\Graduation Details\...`.
- The installed mod/assembly directory and matching installed Workshop mod directories, including
  `steamapps\workshop\content\821880\3646637689\...`.

Migration is copy-only and idempotent: it merges missing JSON and portrait files into the new
directory, never moves or deletes legacy data, and never overwrites a file already present at the
destination. Ambiguous root-level legacy files are imported through a guarded one-time fallback.

## Build

Project file:

- `mods/Graduation Details/Graduation Details.csproj`

Example command:

- `dotnet build "mods/Graduation Details/Graduation Details.csproj" -c Release`
