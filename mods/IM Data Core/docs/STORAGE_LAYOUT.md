# IM Data Core 3.4.24 storage layout

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

IMDC 3.4.24 writes and accepts sidecar format version 5 only. Transactional journals remain format version 2.
The source tree contains a bounded v1-v5 migration decoder that produces a v6-target logical result. The Wave-0 v6/v3 storage foundation is now fully staged, but normal activation/publication still remains v5/v2 so source generations are never overwritten automatically by this build.
The staged v6 logical codec also adds exact-checkpoint identity bindings; see `V6_IDENTITY_BINDING_SCHEMA.md`. Version 3.4.24 additionally stages the journal-v3 boundary/row framing and applies the shared affinity-before-version header classifier to live v2 selection and staged v3 selection. The live writer still emits v5/v2 only. Version 3.4.24 also requires checkpoint-owned `IdentityCandidates` in staged v6 and adds deterministic legacy-unbound adoption plus branch-safe candidate replacement; these fields are intentionally absent from the live v5 wire.
The staged v6 event row also carries `ParticipantSchemaVersion` for built-in shared-history compatibility. Loaded `show_cast_changed` history now normalizes only source-proven legacy per-idol fan-out to the shared envelope; canonical/source-distinct same-timestamp occurrences are preserved unless exact payload plus compatible historical source proves equivalence. Released v2-v5 shared envelopes are strict schema 1; only the bounded v1 archival/synthetic compatibility generation may derive a missing redundant participant count from an authoritative stored list. The v5 wire shape intentionally has no participant-schema field.
The staged v6 document additionally requires `NamespaceOwnerBindings` as document-level, non-rewinding provenance. Populated legacy v1-v5 namespaces migrate only as explicit `legacy_unbound` revision chains; checkpoint `EnabledMods` is never an ownership authority. Task 7 also fills the matching `NAMESPACE_OWNER_BINDING` journal-v3 row codec while live v5/v2 remains unchanged.
The staged v6 document also requires `CoverageModelVersion`, immutable `CoverageCapabilitySets`, branch-owned `CoverageTransitions`, and bounded branch-owned `HistoricalBaselineAssertions`. Task 8 fills the capability/transition rows; Task 9 fills the allow-listed #63 group-origin baseline carrier and the final `HISTORICAL_BASELINE_ASSERTION` v3 row. No frozen v3 semantic row family remains deferred.
The staged v6 root additionally requires non-rewinding `MigrationProvenance`. Native v6 and bounded v1-v5 conversions are distinguished explicitly; a migrated record stores deterministic source/target metadata and a save-scope-bound conversion ID. Live v5 downgrade protection now treats an unsupported primary generation or matching-base unsupported journal as authoritative/write-protected rather than recovering an older backup over it. See `V6_MIGRATION_PROVENANCE_SCHEMA.md`.
Version 3.4.24 cumulatively fills the first three Wave-1 generation families that those v6 checkpoint bindings are designed to carry. Accepted/activated contracts receive opaque `g:` generations, cliques receive opaque `q:` generations at `Relationships.StartNewClique`, and each real bullying interval receives an opaque `b:` episode generation when a target first enters `Bullied_Girls`. Contracts bind by `business__ActiveProposalsData` ordinal + saved-row witness; cliques bind by `Relationships__Cliques` ordinal + complete row witness; bullying episodes additionally require the parent clique generation, `bullied_target:<idolId>` child locator, and a target-specific witness. Legacy contract tuples, sorted-member clique signatures, and `leader|target` bullying keys are candidate metadata only. Because normal runtime still publishes v5/v2, canonical contract/clique/bullying `EntityId` emission and durable rebinding remain gated until the v6 cutover.

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
  ],
  "AgencyRoomIdentities": [
    {
      "EntityId": "g:0123456789abcdef0123456789abcdef",
      "FloorIndex": 0,
      "RoomIndex": 0,
      "RoomTypeRaw": 1,
      "TheaterId": -1
    }
  ]
}
```

Checkpoint identity is the tuple of normalized relative save path, vanilla `LastSave`, vanilla playtime seconds, vanilla game date/time, and `ContentFingerprint`. `Sequence` is the IMDC branch watermark activated by that checkpoint; it is not part of the vanilla-content identity.

`AgencyRoomIdentities` is required on every accepted v5 checkpoint. It is stored in vanilla's serialized floor/room order and may be an empty array when the save contains no agency rooms. The room-generation map is checkpoint state, not part of exact vanilla-save identity.

`ContentFingerprint` is SHA-256 over Unity's compact `JsonUtility.ToJson(savedData, false)` representation of the exact vanilla `SavedData` state. It is stored as `sha256:` followed by 64 lowercase hexadecimal characters. This prevents two distinct vanilla states that happen to share second-resolution timestamp/playtime fields from being treated as the same checkpoint.

Checkpoint `GameDateTime` intentionally uses vanilla's own `yyyy-MM-dd HH:mm:ss` representation. IMDC parses checkpoint dates through vanilla's `ExtensionMethods.ToDateTime`. Event and custom-mutation dates use IMDC's round-trip representation instead.

Every accepted v5 checkpoint carries the required agency-room generation snapshot. The snapshot mirrors vanilla `SavedData.agency__Floors` / room order and binds each serialized room to an IMDC-owned `g:<guid>` generation. Vanilla does not serialize `agency._room.id`, while theater/cafe IDs are recyclable, so IMDC uses the room generation as durable `EntityId` for `agency_room`, `theater`, and `cafe` history (and as the room component of room-work identity). Raw vanilla IDs remain in event payloads for current-state correlation. A v5 checkpoint that omits `AgencyRoomIdentities` is invalid; a present snapshot that does not match the loaded vanilla room layout is not bound to the wrong rooms.

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

## Staged sidecar-v6 namespace-owner provenance

Sidecar-v6 adds a required document-level `NamespaceOwnerBindings` collection. This is not checkpoint state. Exact F9/Save-As branch selection may rewind custom rows, but it must not rewind or erase who is authorized to reclaim durable namespaced state after restart.

Bindings are immutable per-namespace revisions. Revision 1 establishes either a native known owner or explicit `legacy_unbound` migration provenance. A legitimate owner-schema/binary change appends revision 2+, preserving the stable owner lineage and retaining the prior strong assembly witness. Re-registering the same owner with the same schema and strong witness is idempotent and does not grow the revision chain.

For v1-v5 migration, namespace tokens are derived only from persisted namespaced events and custom mutations. Migration never infers ownership from checkpoint `EnabledMods`, title/author/version metadata, DLL names, or the first current registrant. Explicit legacy adoption appends `migration_adopted`; it does not rewrite the unknown source revision. See `NAMESPACE_OWNER_PROVENANCE.md`.

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

A hard process kill can strand a temporary file before the writer's `finally` cleanup runs. On later initialization of that exact physical save scope, IMDC scans only the sidecar directory and only the two temp-name families it owns for that sidecar: `<sidecar>.imdc.tmp.*` and `<sidecar>.imdc.bak.imdc.journal.tmp.*`. Files must be at least 24 hours old before best-effort deletion, and cleanup is serialized with the per-path persistence I/O lock.

When replacing a healthy compact base, IMDC retains:

```text
<sidecar>.imdc.bak
<sidecar>.imdc.bak.imdc.journal   # when the previous generation used a journal
```

The backup journal is tied to the backup base by its stored base hash. Recovery may also pair a still-present current journal with the backup base if a complete parsed journal header contains that backup base hash, covering an interrupted backup-journal publication window. Missing files, empty files, and files torn before a complete header are not positive base-hash matches; recovery continues to the sibling backup journal instead of allowing such a preferred journal to mask it.

Recovery also records which physical journal supplied the matched generation. If `.imdc.bak` was completed by the primary `.imdc.journal`, the subsequent healing snapshot first durably publishes that journal as `.imdc.bak.imdc.journal` before deleting the primary-path journal. A publication failure leaves the original journal in place, preserving the already-proven `backup base + journal` recovery generation.

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

The checkpoint content fingerprint is computed only at vanilla save/load boundaries. In standalone IMDC, the defensive save freeze produces compact JSON, so the fingerprint reuses that JSON rather than performing a second serialization. If the normal `FromJson` reconstruction fails, IMDC constructs a Unity-serialized-field clone and compact-reserializes it, requiring exact JSON equivalence whenever the original compact JSON was available. This fallback avoids `FromJsonOverwrite`, which is unavailable in Idol Manager's UnityEngine API. The SHA-256 input is encoded in bounded UTF-8 chunks to avoid another save-sized byte-array allocation. When Save Write Ordering Fix is positively verified and IMDC skips its own defensive clone, one compact `JsonUtility.ToJson` call is required to obtain the exact checkpoint fingerprint.

Background journal compaction is normally requested when journal bytes reach a bounded threshold: 25% of the compact base, clamped to 1-16 MiB. Transaction count is only a replay-depth ceiling and scales with base size from 2,048 to 32,768 committed transactions.

## Persistence format policy

This development build activates only `IMDataCore.LightweightSidecar` format version 5 and transactional journal format 2. Earlier sidecar formats are left untouched by normal runtime activation. A bounded v1-v5 decoder now exists for the planned one-time v6 migration path, but publication is deliberately deferred until the complete v6/v3 storage foundation is present.

Pre-2.0 database persistence is also outside the runtime path. Historical v2-v4 schema, migration, validation, and implementation-note files are intentionally not shipped with the runtime source tree; a future external migrator can own historical-format knowledge.
