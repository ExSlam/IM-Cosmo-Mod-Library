# IM Data Core 2.0 storage layout

IM Data Core stores its lightweight sidecars below:

```text
Application.persistentDataPath/IMDataCore/
```

This is a sibling of Idol Manager's vanilla `data` directory. Version 2.0 has
no required `IMDataCore/saves` layer and no normal-runtime SQLite database.

## Exact vanilla-to-sidecar mapping

IMDC canonicalizes a supported vanilla save beneath the `data` root, removes
that root, preserves every relative segment, and prepends the `IMDataCore` root.

| Vanilla save | IM Data Core sidecar |
| --- | --- |
| `data/auto_save.json` | `IMDataCore/auto_save.json` |
| `data/manual_save.json` | `IMDataCore/manual_save.json` |
| `data/manual_saves/1c5ec635/save.json` | `IMDataCore/manual_saves/1c5ec635/save.json` |
| `data/story_mode/Agency_123/auto_save.json` | `IMDataCore/story_mode/Agency_123/auto_save.json` |
| `data/story_mode/Agency_123/manual_save.json` | `IMDataCore/story_mode/Agency_123/manual_save.json` |
| `data/story_mode/Agency_123/manual_saves/A1B2C3D4/save.json` | `IMDataCore/story_mode/Agency_123/manual_saves/A1B2C3D4/save.json` |
| `data/story_mode/Agency_123/chapter_3/save.json` | `IMDataCore/story_mode/Agency_123/chapter_3/save.json` |

Manual-save and playthrough directory names are opaque. IMDC copies them
verbatim; it does not replace them with a save title, group name, or derived
identity. The public logical `save_key` remains separate from this physical
layout for consumer compatibility.

Only vanilla paths matching verified freeplay, Story, manual, autosave, and
chapter-save shapes are accepted. `data/global_data.json`, traversal paths,
paths outside `data`, and reparse escapes are rejected.

## Sidecar contents

The mirrored `.json` is an IMDC document, not a vanilla-save copy. Its envelope
is:

```text
FormatName    = IMDataCore.LightweightSidecar
FormatVersion = 1
```

It contains only:

- the exact relative vanilla path;
- the last-issued IMDC mutation sequence;
- tiny vanilla-stamp-to-sequence checkpoints;
- IMDC historical/supplemental event records;
- historical custom JSON `SET`/`REMOVE` mutations;

It does not contain `SaveManager.SavedData`, vanilla entity collections,
embedded checkpoint snapshots, SQL tables, or derived runtime indexes.

## Save, Save As, and rollback

At a vanilla save callsite, IMDC writes the current active in-memory branch to
the exactly mirrored target and adds the actual vanilla checkpoint. Saving to a
different or overwritten target never merges stale IMDC history already at that
target.

On load, IMDC reads only the selected sidecar. An exact vanilla stamp activates
history through its mapped sequence. Without a matching checkpoint, IMDC filters
its own records through the loaded vanilla game date. This rollback is initially
in memory: the durable sidecar is not shortened until a later vanilla save or an
explicit `TryFlushNow` commits the active branch.

Sidecar writes use an IMDC-owned temporary file, flush it, and atomically
replace or move the target. Every write, backup, and cleanup target is validated
under the private `IMDataCore` root. Vanilla files are never temporary,
replacement, backup, or cleanup targets.

## Legacy compatibility

Old 1.2 and 1.3 artifacts remain immutable. Discovery may find historical
`im_data_core.db`, `im_data_core.fallback.json`, and fallback recovery files in
the old keyed/mirrored roots, including `IMDataCore/saves`,
`Mods/IMDataCore`, installed-mod roots, and the prior Workshop location.

Automatic import is deliberately narrow:

- it runs only when the new lightweight sidecar is absent;
- it never opens legacy SQLite;
- it accepts only the late-1.3 fallback `FormatVersion = 2` format with valid
  integrity data and an exact generation matching the already-deserialized
  vanilla `SavedData`;
- it imports historical events and current custom values (as migration-baseline
  `SET` mutations), while omitting redundant legacy projections;
- conflicting exact sources are rejected;
- after success, the new sidecar is written and normal runtime uses only it.

The importer reproduces the old fingerprint solely in memory for this one-time
compatibility decision. Normal 2.0 identity and rollback do not hash vanilla.
Early/unversioned fallback files and legacy SQLite lack a safely proven mapping
to the loaded vanilla checkpoint, so they are left untouched and a clear
limitation is logged instead of guessing.
