# IM Data Core 3.3.0 patch summary

This revision focuses on persistence correctness, crash recovery, and long-campaign save cost while keeping the public v3 sidecar schema compatible.

## Applied changes

1. **Delta-journal persistence:** ordinary append-only saves copy and write only records added since the durable generation. The journal is bound to the exact compact base by SHA-256.
2. **Safe compaction:** journals compact back into the existing atomic v3 snapshot at size/entry thresholds or whenever the active branch changes destructively.
3. **Complete backup generations:** compact replacement preserves the previous base plus its matching journal, so `.imdc.bak` recovery does not discard journal-only generations.
4. **Torn/stale journal handling:** incomplete tails are ignored safely; a journal with the wrong base hash is never replayed; append failure can reconstruct the frozen logical state and compact it.
5. **Incremental frozen snapshots:** the common journal path shallow-copies only new immutable record references instead of all retained events and mutations.
6. **Cached storage JSON:** immutable event payload/custom-value structural JSON is prepared once and reused on later persistence.
7. **Lower allocation streaming:** record fragments are copied from reusable `StringBuilder` buffers directly to the writer rather than calling `ToString()` once per record.
8. **Forward-save trim fast path:** maintained sequence/date watermarks avoid O(history) trim scans when the checkpoint is provably at or beyond active state.
9. **Checkpoint path index:** active checkpoints are keyed by normalized save path so journal snapshots copy only the new checkpoint suffix.
10. **Filesystem correctness:** physical path comparison and file-scoped identity follow Windows case-insensitivity only on Windows and remain case-sensitive on case-sensitive hosts.
11. **Verified SWOF interoperability:** IMDC trusts Save Write Ordering Fix only when its health API says all required write-call interceptions succeeded.
12. **Mutation sequence cleanliness:** custom SET/REMOVE no-ops allocate no sequence.
13. **Concurrent persistence hardening:** I/O serialization is per physical sidecar and superseded snapshots cannot move the controller back to an older scope.
14. **Persistence diagnostics API:** `TryGetPersistenceDiagnostics` reports logical counts, bytes, journal depth, blocking/recovery state, buffered events, and generations.
15. **Money-ledger hot-path reduction:** known ambient sources bypass stack walking and show-money detection uses an explicit scoped marker; unknown callers retain the stack fallback.

## Compatibility

- Sidecar `FormatVersion` remains **3**.
- Journal `FormatVersion` is **1** and is an implementation-detail sibling of the v3 base.
- V1/V2/v3 sidecars remain readable.
- Public API compatibility is retained; diagnostics are additive.
- Project/mod version is **3.3.0**.

See `docs/STORAGE_LAYOUT.md` and `docs/V3_VALIDATION.md` for persistence/recovery details.
