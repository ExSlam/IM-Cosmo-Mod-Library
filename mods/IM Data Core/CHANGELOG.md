# Changelog

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
