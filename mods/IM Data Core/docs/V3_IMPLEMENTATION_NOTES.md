# IM Data Core 3.2 implementation notes

> **Historical reference only.** IMDC 3.4.6 writes and accepts sidecar format 5 only. Older migration/compatibility behavior described below is not active in the current development build. See `V5_SIDECAR_SCHEMA.md` and `V5_VALIDATION.md` for current behavior.

## Goals

IMDC 3.2 preserves the JSON-native, memory-first, exact-save-scoped architecture while tightening branch correctness and reducing the memory/lock cost of large histories.

The sidecar remains a source document rather than a serialized database projection. Derived indexes are rebuilt at load and are not written to disk.

## Disk format

`LightweightCoreStorageEngine.SidecarFormatVersion` remains `3`.

V3 writes native JSON event payloads and custom SET values. IMDC 3.2 adds only one optional event member, `IdempotencyKey`, for namespaced events created through the idempotent custom-event API. No sidecar format bump is required.

V3 still does not write duplicate `EventId`, derived `GameDateKey`, checkpoint path repetition, `PayloadJson`, or `ValueJson` string wrappers.

## V1/V2 normalization

The codec accepts lightweight sidecar format versions 1, 2, and 3. Older sidecars are parsed into the current runtime record model. A successful later write emits v3 only.

This is a lightweight-sidecar migration, not a migration from pre-2.0 database persistence.

## Exact checkpoint policy

Existing sidecars are branch/checkpoint ledgers. Loading by game date alone is ambiguous because different save branches can share the same in-game date.

IMDC 3.2 therefore requires `TryActivateCheckpoint` to find the exact vanilla save stamp. If it does not, the engine enters read-only empty supplemental state for that physical sidecar and protects the existing file from overwrite. The former date-only activation fallback is removed.

## Runtime indexes

Derived state rebuilt after load includes:

- timeline events by idol
- global timeline events
- exact money-ledger indexes
- active mutation-sequence set
- materialized custom values
- per-namespace custom-data quota usage
- namespace/idempotency-key set for custom events

Timeline indexes are kept sorted as records enter them. `TryReadRecentEventsForIdol` and `TryReadEventsForIdolPage` perform a newest-first two-way merge of the idol-specific and global lists, so a newer global row cannot be pushed out merely because the idol-specific list reaches the requested limit first.

## Custom-event idempotency

`TryAppendCustomEvent` remains append-only.

`TryAppendCustomEventOnce(session, idempotencyKey, ...)` adds caller-controlled idempotency without guessing from payload equality. The key is sanitized, paired with the registered namespace, checked against both pending buffered events and the active persisted branch, and stored on the event record.

An existing namespace/key pair returns success without allocating another sequence/event. Branch rollback naturally rewinds the key because the lookup is rebuilt from active events only.

## Substory completion reconstruction

Vanilla persists `Substories_Manager.dialogueQueue`. IMDC's pending completion counters are transient and previously disappeared across load.

After vanilla load reconstruction completes, IMDC 3.2 seeds those counters from the restored dialogue queue without emitting events. A queued dialogue can then emit its matching `substory_completed` event when it later closes.

## Custom-data quota accounting

Per-namespace key count and normalized-value character usage are maintained incrementally. SET validation does not rescan the whole namespace.

The mutation layer suppresses two no-op records:

- SET where normalized JSON equals the current value
- REMOVE where the key does not exist

## Paged timeline reads

`TryReadEventsForIdolPage` takes an exclusive EventId cursor. A non-positive cursor requests the newest page; subsequent calls pass the EventId of the last (oldest) row from the previous page. The storage engine resolves that EventId back to its active record, binary-searches the idol and global timeline indexes by the record's `(GameDateKey, Sequence)` sort key, and continues the same newest-first two-way merge used by recent reads.

Each page remains capped by the existing per-call maximum. `hasMore` reports whether an older idol/global row remains. The API does not change retention policy and does not copy or persist any new timeline structure.

## Streaming load and validation

Sidecar reads now use a buffered sequential `TextReader`. For the normal writer-produced property order, collection records are parsed and converted one at a time, avoiding a complete sidecar string and complete campaign JSON DOM in memory. Out-of-order legacy-compatible top-level fields remain accepted; only an unusually early collection must be temporarily materialized until format metadata is known.

For format v3, event payloads and custom SET values are already guaranteed to be syntactically valid JSON by structural deserialization, so validation no longer parses those serialized payload strings a second time. V1/V2 compatibility retains normalization because those formats store JSON in strings.

Checkpoint duplicate detection now uses a `HashSet` over normalized relative save path, vanilla `LastSave`, playtime seconds, and game date/time, replacing the previous pairwise scan.

## Backup recovery and failure policy

Missing and unreadable are different states.

- Missing sidecar: ordinary empty writable physical branch.
- Unreadable/invalid primary: attempt strict validation of sibling `.imdc.bak`.
- Valid backup: load it as recovery state, preserve the damaged primary, and still require an exact checkpoint.
- No valid recovery source: expose safe empty supplemental state and block writes to that path.

A later successful persistence after backup recovery replaces the damaged primary while preserving the known-good recovery backup.

## Long-campaign persistence

The source history is intentionally complete, so serialization work remains O(number of retained source records). IMDC 3.2 reduces avoidable multipliers around that work:

1. Persistence snapshots shallow-copy record lists rather than deep-cloning every immutable event and mutation.
2. The controller captures/flushed/checkpoints under `runtimeLock`, takes the stable storage snapshot, then releases `runtimeLock` before serialization and fsync.
3. The serializer streams JSON directly through a buffered `StreamWriter` to the temporary file instead of materializing a complete JSON string and then a complete UTF-8 byte array.
4. Loaded/active branches reuse immutable event records where compaction did not change the record.
5. Timeline indexes are incrementally sorted, avoiding whole-index sort/copy work for recent timeline reads.
6. Each successful persistence logs event count, custom mutation count, checkpoint count, final bytes, and elapsed milliseconds.
7. If Save Write Ordering Fix is loaded, the save lifecycle skips IMDC's redundant full `SavedData` JSON clone because that mod freezes the exact vanilla payload synchronously after IMDC's hook. Standalone IMDC retains its own detached snapshot path.
8. Per-path persistence generations prevent an older snapshot from overwriting a newer already-committed snapshot for the same physical sidecar.

This does not impose retention limits or silently discard history. Campaign owners can therefore retain decades of event history while avoiding the previous whole-document memory duplication on both save output and normal sidecar load input.

## Internal money indexing guard

Built-in money-ledger classification now also requires an empty namespace. A consumer custom event that happens to use a built-in-looking event type such as `money_transaction` remains a namespaced custom timeline event rather than being swallowed by an internal money index.

## Public API compatibility

IMDC 3.2 retains `TryAppendCustomEventOnce` on both public API aliases and adds `TryReadEventsForIdolPage` without removing the existing recent-event or custom-data methods.

The public string-JSON interface is retained so consumers can continue using their preferred JSON library rather than depending on IMDC's private AST implementation.
