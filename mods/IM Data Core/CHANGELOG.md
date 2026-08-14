# Changelog

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
