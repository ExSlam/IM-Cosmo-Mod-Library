# Changelog

## 3.4.1

- Fixed the 3.4.0 unresolved-single chart backfill optimization failing to compile because its transient `pendingSingleChartResolutionBySingleId` dictionary declaration was omitted from `IMDataCoreController`.
- Synchronized the project assembly version with the mod metadata at 3.4.1.

## 3.4.0

- Fixed New Save -> later Overwrite Save checkpoint persistence by keeping the active multi-path checkpoint ledger intact after a full New Save snapshot.
- Replaced engine-local sidecar I/O locks with process-wide per-path locks and made load replacement hold the same lease through old-engine disposal/new-engine installation, eliminating stale background-compaction races.
- Made standalone `SavedData` defensive cloning fail open so an IMDC clone failure can never prevent vanilla from attempting its save.
- Changed journal compaction policy to use byte/base-ratio thresholds with a scaled 2,048-32,768 transaction replay ceiling instead of rewriting large bases after only 256 tiny saves.
- Reworked background compaction to shallow-copy the committed immutable in-memory prefix and stream it, avoiding a second full object graph produced by deserializing base+journal on the worker.
- Added a physical base SHA-256/journal-length generation check immediately before background compaction commits.
- Reused first-pass validation state to validate only journal-appended suffix rows after replay, removing a redundant second full-history validation pass.
- Avoided unconditional history sorting by checking sequence monotonicity before sorting.
- Replaced save-boundary full-history single chart scans with a transient unresolved-single set seeded on load/release and cleared as chart positions resolve.
- Reused scratch collections in post-mod show reconciliation to eliminate per-save `HashSet`/`List` allocations.
- Hardened backup recovery: if compaction dies before the backup-journal copy completes, recovery can pair `.imdc.bak` with the still-present primary journal when its base hash matches; a failed backup-journal copy no longer deletes that only matching journal.
- Current-format only: this build accepts v3 sidecars and transactional v2 journals and intentionally drops older persistence-format compatibility.

## 3.3.0

- Added a versioned append journal (`.imdc.journal`) tied to the compact v3 base snapshot by SHA-256. Normal append-only saves now persist only the immutable suffix since the last durable generation instead of rewriting complete campaign history.
- Added periodic journal compaction back into the existing atomic v3 snapshot. Rewinds, destructive branch changes, recovery writes, journal thresholds, and incompatible baselines automatically use a full snapshot.
- Preserved a matching `.imdc.bak.imdc.journal` whenever a compacted base is replaced, so backup recovery represents the complete previous logical generation rather than only its old base file.
- Added torn-tail detection and safe snapshot fallback for interrupted journal appends; stale journals are rejected by base-file hash and cannot be replayed onto a different snapshot.
- Added incremental save snapshots that copy only new immutable records on the journal fast path. Full-list shallow snapshots are now reserved for compaction/destructive boundaries.
- Cached storage-form event payload/custom SET JSON at record creation/load time, avoiding repeated parsing and built-in payload transforms on every save.
- Removed per-record `StringBuilder.ToString()` allocations from the streaming sidecar/journal writers by writing reusable character buffers directly.
- Added O(1) watermark checks that skip complete event/custom/checkpoint trim scans on ordinary forward saves.
- Indexed active checkpoints by normalized save path so journal snapshots copy only the new checkpoint suffix instead of filtering complete checkpoint history.
- Fixed filesystem identity on case-sensitive platforms: containment, checkpoint/path identity, generation maps, and save-key hashing now follow the host OS path comparison rules.
- Save Write Ordering Fix integration now verifies its public interception-health capability before skipping IMDC's standalone `SavedData` clone; merely loading the SWOF assembly is no longer trusted.
- Same-value custom SETs and missing-key REMOVEs no longer consume otherwise-unused capture sequence numbers.
- Replaced the global persistence I/O lock with per-sidecar locks and prevented a superseded concurrent snapshot from regressing controller save scope.
- Added `TryGetPersistenceDiagnostics` to both public API names, reporting persistence mode, counts, snapshot/journal sizes, recovery/block state, dirty buffered events, and generation information without forcing a save.
- Reduced money-ledger stack walking by using known ambient source contexts for business, singles, shows, theaters, cafes, and concerts, and replaced show-money stack inspection with a scoped Harmony marker. Unknown sources still use the existing stack-based fallback.
- Sidecar `FormatVersion` remains `3`; existing v1/v2/v3 sidecars remain readable.

## 3.2.0

- Added `TryReadEventsForIdolPage` to `IMDataCoreApi` and `IMDataCoreAPI`, using an exclusive EventId cursor and `hasMore` so consumers can walk complete idol/global history without raising the existing 1,000-row per-call cap.
- Added binary-search cursor positioning over the already-sorted idol/global timeline indexes, keeping page traversal proportional to page size plus logarithmic seek work.
- Changed sidecar loading to a buffered sequential `TextReader` path that materializes one v3 record tree at a time instead of reading the entire file into a string and whole-document JSON tree first.
- Removed the redundant second JSON parse/normalization pass for v3 event payloads and custom SET values after structural deserialization.
- Replaced quadratic checkpoint-identity duplicate validation with a `HashSet` keyed by the exact checkpoint identity fields.
- Sidecar format remains version `3`; no persisted schema migration is required.

## 3.1.0

- Fixed recent-idol timeline reads to perform a true newest-first merge of idol-specific and global event indexes.
- Rebuild pending substory completion bookkeeping from vanilla's restored dialogue queue after load.
- Fail closed when an existing sidecar has no exact checkpoint for the loaded vanilla save; removed date-only activation fallback.
- Added `TryAppendCustomEventOnce(session, idempotencyKey, ...)` with persistent, namespace-scoped, active-branch idempotency.
- Added optional v3 event `IdempotencyKey` storage without changing sidecar `FormatVersion`.
- Added automatic validation/recovery from `.imdc.bak` when the primary sidecar is unreadable or invalid, while preserving the damaged primary until a later successful save.
- Reduced long-campaign save memory pressure with shallow persistence snapshots and streaming JSON output.
- Moved JSON serialization and fsync outside the controller runtime lock while preserving exact vanilla-save checkpoint preparation.
- Added persistence telemetry for source-record counts, file bytes, and write elapsed time.
- Reused immutable loaded records where safe and maintained sorted timeline indexes incrementally.
- Avoided a redundant full vanilla `SavedData` JSON clone when Save Write Ordering Fix is loaded.
- Fixed namespaced custom events that reuse a built-in money event type from being indexed as internal money-ledger rows.
- Removed the obsolete `LegacyFlatFileImporter.cs` stub and marked v2 migration notes as historical.

## 3.0.0

- Introduced JSON-native sidecar format version 3.
- Store event payloads and custom values as actual JSON nodes instead of escaped JSON strings.
- Store built-in identifier lists as arrays and nested money details as nested JSON on disk.
- Removed persisted duplicate `EventId`, derived `GameDateKey`, and checkpoint path duplication.
- Kept public `EventId`, `GameDateKey`, and `PayloadJson` views for consumer compatibility.
- Added v1/v2 lightweight-sidecar read compatibility with one-way v3 rewrite on successful persistence.
- Removed pre-2.0 flat-file importer and legacy database/fallback discovery code from runtime IMDC.
- Reject malformed public custom/event JSON before it can enter history.
- Added O(1) per-namespace custom-data quota accounting.
- Suppress same-value SET and missing-key REMOVE history no-ops.
- Preserve corrupt, invalid-scope, or newer sidecars instead of overwriting them as empty state.
- Retain one `.imdc.bak` previous sidecar generation after atomic replacement.
