# IM Data Core Save Storage Layout

IM Data Core stores persistent data below this Windows directory:

```text
%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager\IMDataCore\saves
```

`Application.persistentDataPath` is used at runtime, so the same rule also works
if the game runs under a different user profile or supported operating system.

## Vanilla-to-sidecar mapping

The path below Idol Manager's vanilla `data` directory is mirrored below
`IMDataCore\saves`. A terminal `save.json` represents its owner directory. A
direct file such as `auto_save.json` or `manual_save.json` gets a directory with
the same filename stem.

| Vanilla save | IM Data Core directory |
| --- | --- |
| `data\manual_saves\12\save.json` | `IMDataCore\saves\manual_saves\12` |
| `data\story_mode\Agency_123\manual_saves\A1B2C3D4\save.json` | `IMDataCore\saves\story_mode\Agency_123\manual_saves\A1B2C3D4` |
| `data\story_mode\Agency_123\chapter_1\save.json` | `IMDataCore\saves\story_mode\Agency_123\chapter_1` |
| `data\story_mode\Agency_123\auto_save.json` | `IMDataCore\saves\story_mode\Agency_123\auto_save` |
| `data\story_mode\Agency_123\manual_save.json` | `IMDataCore\saves\story_mode\Agency_123\manual_save` |
| `data\auto_save.json` | `IMDataCore\saves\auto_save` |
| `data\manual_save.json` | `IMDataCore\saves\manual_save` |

Each directory contains either `im_data_core.db` or, on a runtime without the
required SQLite support, `im_data_core.fallback.json`.

The readable directory and the internal `save_key` serve different purposes.
The directory follows the vanilla path. The persisted key retains the previous
absolute-path hash convention so existing rows and API behavior remain compatible.

## Save As and overwrite saves

When vanilla writes a different save target, IM Data Core first flushes and
closes the currently active sidecar, then clones that explicit source directory
into the resolved target directory. This gives a new manual save the same history
as the game state from which it was created. A failed clone does not cause IM Data
Core to open an older, unrelated target sidecar.

## Backward-compatible migration

If the mirrored target has no storage yet, IM Data Core copies compatible data
from older layouts. It tries save identities in this order:

1. The prior exact full-path hash key.
2. The prior immediate owner-directory key.
3. The historical `PlayerData.SaveFolderName` key.
4. The historical player/agency identity fallback key.

For each identity it probes the old keyed layout under:

- `Application.persistentDataPath\IMDataCore`
- `Application.persistentDataPath\Mods\IMDataCore`
- `Application.persistentDataPath\Mods\IM Data Core`
- The currently loaded or assembly-adjacent mod installation directory
- The known Workshop installation (`workshop\content\821880\3680836490`)

Both `saves\<key>` and the older direct `<key>` form are recognized at these
roots. Existing flat-file fallback storage remains a fallback file; migration
does not silently replace it with an empty SQLite database.

Migration is copy-only. Legacy files and directories are not moved or deleted,
so they remain available for rollback to an older mod build. Direct auto/manual
directories that already overlap the new layout are opened in place and their
plausible historical row keys are remapped to the exact current key.
