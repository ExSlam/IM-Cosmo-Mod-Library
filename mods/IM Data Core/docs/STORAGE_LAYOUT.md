# IM Data Core 3.1 storage layout

## Physical mapping

IMDC mirrors each supported vanilla save path below the sibling `IMDataCore` root.

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

All IMDC mutation paths are canonicalized and required to remain beneath the private `IMDataCore` root. Existing path chains are checked for reparse points before reads, writes, or deletes that could escape containment. The private root is required to remain separate from the vanilla `data` root.

## V3 document identity

IMDC 3.1 still uses sidecar format version 3:

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

Checkpoint activation is exact. If an existing valid sidecar contains no checkpoint matching the loaded vanilla save stamp, IMDC 3.1 does not use an in-game-date approximation. Supplemental state is detached read-only and the sidecar is protected from overwrite.

### Event

A normal event:

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

A custom event written through `TryAppendCustomEventOnce` may also contain:

```json
"IdempotencyKey": "promotion.14.2031-05-03.2"
```

`IdempotencyKey` is optional and only meaningful for namespaced custom events. Older v3 documents without it remain valid.

`Sequence` is the stored event identity. Public `EventId` is derived from it. `GameDateKey` is derived from `GameDateTime` and is not serialized.

Built-in IMDC payloads use native arrays for known ID-list fields. Built-in money `detail_json` is stored as nested `detail` JSON. These transformations are reversed when producing the stable public `PayloadJson` view. Namespaced custom-event payloads are stored structurally but are otherwise semantically untouched.

### Custom-data SET

```json
{
  "Sequence": 420,
  "GameDateTime": "2028-04-16T00:00:00.0000000",
  "NamespaceIdentifier": "com.example.mod",
  "DataKey": "idol_14_state",
  "Operation": "SET",
  "Value": {
    "tier": 3,
    "flags": ["a", "b"]
  }
}
```

### Custom-data REMOVE

```json
{
  "Sequence": 421,
  "GameDateTime": "2028-04-17T00:00:00.0000000",
  "NamespaceIdentifier": "com.example.mod",
  "DataKey": "idol_14_state",
  "Operation": "REMOVE"
}
```

## What is intentionally not persisted

The sidecar does not persist runtime-derived structures such as:

- timeline indexes by idol
- global timeline index
- custom-data materialized dictionary
- per-namespace quota counters
- custom-event idempotency lookup sets
- active mutation-sequence set
- `GameDateKey`
- duplicated public `EventId`
- legacy database or fallback-file state

Those values are derived from source records or are transient runtime bookkeeping.

## Atomic writes, streaming, and backup

A persistence boundary first creates a stable shallow snapshot of the active source-record lists. Runtime locks are then released before JSON serialization and durable disk I/O.

The v3 JSON document is streamed to a validated temporary file using buffered sequential I/O. IMDC does not first allocate one complete JSON string plus a second complete UTF-8 byte array. The temporary file is flushed durably and atomically promoted.

When replacing an existing healthy sidecar, IMDC retains one sibling:

```text
<sidecar>.imdc.bak
```

as the previous known-good generation.

If the primary is unreadable or invalid, IMDC validates the backup. A valid backup can be loaded for recovery. The damaged primary remains untouched until a later successful save. That save replaces the damaged primary while preserving the recovery backup rather than overwriting it.

## Missing, unreadable, and unmatched sidecars

These states deliberately differ:

- **Missing sidecar:** ordinary empty writable IMDC branch.
- **Unreadable/invalid/newer primary with valid backup:** recover from backup, then still require an exact vanilla checkpoint.
- **Unreadable/invalid/newer primary and unusable backup:** expose safe empty supplemental state and block writes to that physical sidecar path.
- **Valid existing sidecar with no exact checkpoint for the loaded vanilla save:** fail closed, expose detached supplemental state, and protect the sidecar from overwrite.

A Save As to a different valid physical vanilla save path may establish a new writable branch.

## Long-campaign characteristics

Complete event history remains complete. File size and full persistence time therefore remain proportional to genuine retained history. IMDC 3.1 removes avoidable deep-copy and whole-document-buffer costs, keeps query indexes sorted incrementally, and logs write counts/bytes/timing so very long campaigns can be profiled from real data.

## Compatibility

Format versions 1 and 2 of `IMDataCore.LightweightSidecar` remain readable. On a later successful persistence boundary they are written as format version 3.

Pre-2.0 database persistence is outside the runtime migration path. IMDC 3.1 does not discover or import historical databases or flat fallback files.
