# IM Data Core 3.4.6 storage layout

## Physical mapping

IMDC mirrors each supported vanilla save path below the sibling `IMDataCore` root. It never writes supplemental state into vanilla's `data` tree.

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

All IMDC mutation paths are canonicalized and required to remain beneath the private `IMDataCore` root. Existing path chains are checked for reparse points before physical mutation. The private root is required to remain separate from vanilla's `data` root.

## V5 document identity

IMDC 3.4.6 writes and accepts sidecar format version 5 only. Transactional journals remain format version 2.

```json
{
  "FormatName": "IMDataCore.LightweightSidecar",
  "FormatVersion": 5,
  "RelativeSavePath": "manual_saves/4060ce4d/save.json",
  "LastIssuedSequence": 421,
  "Checkpoints": [],
  "Events": [],
  "CustomMutations": []
}
```

`RelativeSavePath` belongs to the document. Child checkpoints inherit that path and do not repeat it on disk.

## Source records

### Checkpoint

```json
{
  "LastSave": "2026-08-13 18:22:04",
  "PlaytimeSeconds": 58321,
  "GameDateTime": "2028-04-17 00:00:00",
  "ContentFingerprint": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
  "Sequence": 421,
  "EnabledMods": [
    {
      "ModName": "Example Mod",
      "Title": "Example Mod",
      "Author": "Example Author",
      "Version": "1.2.3",
      "DllNames": ["example.dll"]
    },
    {
      "ModName": "JSON Outcome Pack",
      "Title": "JSON Outcome Pack",
      "Author": "Example Author",
      "Version": "1.0.0",
      "DllNames": []
    }
  ]
}
```

Checkpoint identity is the tuple of normalized relative save path, vanilla `LastSave`, vanilla playtime seconds, vanilla game date/time, and `ContentFingerprint`. `Sequence` is the IMDC branch watermark activated by that checkpoint; it is not part of the vanilla-content identity.

`ContentFingerprint` is SHA-256 over Unity's compact `JsonUtility.ToJson(savedData, false)` representation of the exact vanilla `SavedData` state. It is stored as `sha256:` followed by 64 lowercase hexadecimal characters. This prevents two distinct vanilla states that happen to share second-resolution timestamp/playtime fields from being treated as the same checkpoint.

Checkpoint `GameDateTime` intentionally uses vanilla's own `yyyy-MM-dd HH:mm:ss` representation. IMDC parses checkpoint dates through vanilla's `ExtensionMethods.ToDateTime`. Event and custom-mutation dates use IMDC's round-trip representation instead.

`EnabledMods` is frozen at the save boundary from Idol Manager's enabled mod registry. JSON-only mods are represented even when `DllNames` is empty. After exact activation, IMDC compares the saved inventory with the current installed/enabled mod set and logs missing, disabled, author/version, and DLL-name mismatches. These diagnostics do not block vanilla loading.

If an existing valid sidecar contains no checkpoint matching the loaded vanilla state exactly, IMDC fails closed: supplemental state is detached read-only and the sidecar is protected from overwrite. There is no date-only fallback.

### Existing vanilla career with no sidecar

A missing sidecar is different from an existing unmatched sidecar. When IMDC first loads a vanilla career that has never had IMDC persistence, it seeds an in-memory sequence-0 checkpoint for that exact loaded vanilla state. Loading alone does not create a file. If a consumer later calls `TryFlushNow`, the first sidecar is therefore anchored to the vanilla save and remains matchable on the next load.

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

`IdempotencyKey` is optional and only meaningful for namespaced custom events. `Sequence` is the stored event identity. Public `EventId` is derived from it. `GameDateKey` is derived from `GameDateTime` and is not serialized.

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

The sidecar does not persist runtime-derived structures such as timeline indexes, custom-data materialized dictionaries, quota counters, custom-event idempotency lookup sets, active mutation-sequence sets, `GameDateKey`, duplicated public `EventId`, or persistence synchronization epochs. Those values are derived from source records or are process-local bookkeeping.

## Atomic snapshots, delta journal, and backup

The compact base is a v5 sidecar. A normal append-only save may additionally create:

```text
<sidecar>.imdc.journal
```

The first journal line contains `FormatName = IMDataCore.LightweightJournal`, journal `FormatVersion = 2`, and the SHA-256 of the exact compact base it extends. Each save delta is a bounded NDJSON transaction: `BEGIN`, record rows, then `COMMIT`. A transaction is visible only after its valid commit row.

The journal writer flushes its buffered writer and then calls `FileStream.Flush(true)`. Replay ignores a transaction that does not reach a valid `COMMIT`. Base/target counts make a fully written retry idempotent, and a journal whose base hash does not match the current compact sidecar is never replayed onto that base.

A full boundary streams a stable shallow snapshot to a validated temporary file, computes its SHA-256 while writing, durably flushes it, and only then promotes it. Destructive branch changes, recovery writes, New Save, incompatible baselines, and compaction use a full snapshot. Routine compaction is queued after the triggering delta is durable so an ordinary save boundary does not pay the complete O(history) rewrite cost.

When replacing a healthy compact base, IMDC retains:

```text
<sidecar>.imdc.bak
<sidecar>.imdc.bak.imdc.journal   # when the previous generation used a journal
```

The backup journal is tied to the backup base by its stored base hash. Recovery may also pair a still-present current journal with the backup base if that journal's stored base hash matches, covering an interrupted backup-journal publication window.

## Deleted-save archival

Vanilla deletion does not delete IMDC history. After a successful vanilla save-directory deletion, IMDC maps that deleted vanilla directory to the corresponding mirrored IMDC directory and renames the entire supplemental directory in place:

```text
<name>      -> <name>OLD
<name>OLD   -> existing archive
<name>      -> <name>OLD2   # next collision-safe archive
```

Further collisions use `OLD3`, `OLD4`, and so on. No file inside the archived directory is deleted. Deleting an entire story playthrough archives the mirrored playthrough directory as one unit, preserving all chapter/manual-save sidecars beneath it for later diary export.

Archival takes an exclusive persistence-topology lease. Loads, writes, and background compaction take shared leases, so archival waits for already-running physical IMDC I/O and prevents new I/O from crossing the rename. Every prepared snapshot also carries a per-path archive epoch; a snapshot prepared before the archive becomes stale afterward and cannot recreate the deleted path.

If archival rename fails, the existing supplemental directory is left untouched. IMDC blocks subsequent writes beneath that deleted-save directory for the rest of the process rather than risk overwriting the historical material that was supposed to be preserved.

If the deleted save was the active scope, IMDC detaches the physical binding but retains the logical in-memory branch. A later vanilla New Save/Save As can bind that branch to a new physical path.

## Missing, unreadable, and unmatched sidecars

These states deliberately differ:

- **Missing sidecar:** writable empty/adopted IMDC branch; if a vanilla save was loaded, a sequence-0 exact checkpoint is held in memory.
- **Unreadable/invalid/unsupported primary with valid backup:** recover from backup, then still require an exact v5 vanilla checkpoint.
- **Unreadable/invalid/unsupported primary and unusable backup:** expose safe empty supplemental state and block writes to that physical sidecar path.
- **Valid existing sidecar with no exact checkpoint for the loaded vanilla save:** fail closed, expose detached supplemental state, and protect the sidecar from overwrite.

A New Save to a different valid physical vanilla save path may establish a new writable branch.

## Long-campaign characteristics

Complete event history remains complete, but an ordinary append-only save does not rewrite complete history. Active checkpoints are indexed by normalized save path, and IMDC snapshots only immutable event, custom-mutation, and checkpoint suffixes beyond durable counts before appending them to the journal. Full O(history) work is reserved for compaction, recovery, New Save, or destructive branch boundaries.

Storage-form JSON for immutable events and custom SET values is cached after validation/load, and the streaming writer avoids a temporary string allocation for each record. Forward-save sequence/date watermarks avoid complete trim scans when no active record can exceed the checkpoint.

The checkpoint content fingerprint is computed only at vanilla save/load boundaries. In standalone IMDC, the defensive save freeze already produces compact JSON, so the fingerprint reuses that JSON rather than performing a second serialization. The SHA-256 input is encoded in bounded UTF-8 chunks to avoid another save-sized byte-array allocation. When Save Write Ordering Fix is positively verified and IMDC skips its own defensive clone, one compact `JsonUtility.ToJson` call is required to obtain the exact checkpoint fingerprint.

Background journal compaction is normally requested when journal bytes reach a bounded threshold: 25% of the compact base, clamped to 1-16 MiB. Transaction count is only a replay-depth ceiling and scales with base size from 2,048 to 32,768 committed transactions.

## Persistence format policy

This development build accepts only `IMDataCore.LightweightSidecar` format version 5 and transactional journal format 2. Earlier sidecar formats are intentionally unsupported and are left untouched. No runtime migration path is provided.

Pre-2.0 database persistence is also outside the runtime path. Historical documents remain in `docs/` only as implementation history and do not describe accepted current persistence inputs.
