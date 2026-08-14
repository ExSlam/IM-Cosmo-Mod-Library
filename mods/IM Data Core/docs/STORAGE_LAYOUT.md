# IM Data Core 3 storage layout

## Physical mapping

IMDC mirrors the supported vanilla save path below the sibling `IMDataCore` root.

If the persistent root is:

```text
%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager
```

then vanilla and IMDC roots are:

```text
data\
IMDataCore\
```

Examples:

| Vanilla | IMDC sidecar |
| --- | --- |
| `data\auto_save.json` | `IMDataCore\auto_save.json` |
| `data\manual_save.json` | `IMDataCore\manual_save.json` |
| `data\manual_saves\<id>\save.json` | `IMDataCore\manual_saves\<id>\save.json` |
| `data\story_mode\<playthrough>\auto_save.json` | `IMDataCore\story_mode\<playthrough>\auto_save.json` |
| `data\story_mode\<playthrough>\manual_saves\<id>\save.json` | `IMDataCore\story_mode\<playthrough>\manual_saves\<id>\save.json` |
| `data\story_mode\<playthrough>\chapter_0..6\save.json` | mirrored equivalent under `IMDataCore` |

`data\global_data.json` and arbitrary JSON files are rejected as game-save scopes.

## Containment safety

All IMDC mutations are canonicalized and required to remain beneath the private `IMDataCore` root. Existing path chains are checked for reparse points before reads/writes/deletes that could escape containment. The private root is required to remain separate from the vanilla `data` root.

## V3 document identity

Every sidecar root contains:

```json
{
  "FormatName": "IMDataCore.LightweightSidecar",
  "FormatVersion": 3,
  "RelativeSavePath": "manual_saves/4060ce4d/save.json",
  "LastIssuedSequence": 421,
  "Checkpoints": [],
  "Events": [],
  "CustomMutations": []
}
```

`RelativeSavePath` belongs to the document. Child checkpoints do not repeat it.

## Source records

### Checkpoint

```json
{
  "LastSave": "2026-08-13 18:22:04",
  "PlaytimeSeconds": 58321,
  "GameDateTime": "2028-04-17T00:00:00.0000000",
  "Sequence": 421
}
```

### Event

```json
{
  "Sequence": 419,
  "GameDateTime": "2028-04-16T00:00:00.0000000",
  "IdolId": 14,
  "EntityKind": "single",
  "EntityId": "32",
  "EventType": "single_released",
  "SourcePatch": "SingleRelease",
  "NamespaceIdentifier": "",
  "Payload": {
    "title": "Example",
    "cast_id_list": [14, 7, 21]
  }
}
```

`Sequence` is the stored event identity. Public `EventId` is derived from it. `GameDateKey` is derived from `GameDateTime` and is not serialized.

Built-in IMDC payloads use native arrays for known comma-delimited ID-list fields. Built-in money `detail_json` is stored as nested `detail` JSON. These transformations are reversed when producing the stable public `PayloadJson` string view.

Namespaced custom-event payloads are stored structurally but are otherwise semantically untouched.

### Custom-data SET

```json
{
  "Sequence": 420,
  "GameDateTime": "2028-04-16T00:00:00.0000000",
  "NamespaceIdentifier": "com.example.mod",
  "DataKey": "preferences",
  "Operation": "SET",
  "Value": {
    "enabled": true,
    "mode": "compact"
  }
}
```

### Custom-data REMOVE

```json
{
  "Sequence": 421,
  "GameDateTime": "2028-04-16T00:00:00.0000000",
  "NamespaceIdentifier": "com.example.mod",
  "DataKey": "preferences",
  "Operation": "REMOVE"
}
```

## What is intentionally not persisted

The v3 sidecar does not persist:

- vanilla `SaveManager.SavedData`
- whole vanilla entity collections
- runtime dictionaries or lookup indexes
- materialized custom key/value state
- `EventId` in addition to sequence
- `GameDateKey`
- checkpoint-relative path duplication
- stringified `PayloadJson` / `ValueJson`
- legacy database or fallback-file state

Those values are either vanilla-owned, derived, or materialized from source records.

## Atomic writes and backup

A sidecar is written to a validated temporary file and atomically promoted. When replacing an existing sidecar, IMDC retains one sibling:

```text
<sidecar>.imdc.bak
```

as the previous known-good generation. It is not a second persistence backend and is not part of normal reads.

## Unreadable sidecars

A missing sidecar means an ordinary empty writable IMDC branch.

An existing but corrupt, invalid-scope, or newer-format sidecar is different: IMDC preserves it and blocks writes to that same sidecar path for the session rather than replacing it with empty state. A Save As to a different valid physical save path may establish a new writable branch.

## Compatibility

Format versions 1 and 2 of `IMDataCore.LightweightSidecar` remain readable. On a later successful persistence boundary they are written as format version 3.

Pre-2.0 database persistence is outside the runtime migration path. IMDC 3 does not discover or import historical databases or flat fallback files.
