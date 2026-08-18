# IM Data Core 3.4.2 patch summary

- Restored the missing transient `Dictionary<int, singles._single>` used by unresolved single chart-position backfill.

This revision focuses on persistence correctness under New Save/Overwrite Save branching, cross-engine compaction safety, and long-campaign save/load cost.

## Applied changes

1. **New Save checkpoint correctness:** a full New Save snapshot no longer prunes checkpoints belonging to other physical save paths from the active ledger, preserving incremental-prefix validity when a later Overwrite Save returns to an older path.
2. **Process-wide path serialization:** sidecar I/O locks are shared across engine instances, and same-path load replacement holds that lease through old-engine disposal and new-engine installation.
3. **Fail-open vanilla protection:** standalone `SavedData` snapshot cloning logs and returns the original object if Unity serialization fails, so IMDC cannot abort vanilla saving.
4. **Scaled compaction policy:** byte/base-ratio thresholds are primary; transaction count is only a scaled 2,048-32,768 replay-cost ceiling.
5. **Low-allocation compaction:** background compaction uses a shallow immutable-prefix snapshot of loaded records rather than deserializing the complete base+journal into duplicate event/mutation objects.
6. **Compaction generation verification:** the worker rechecks the physical base SHA-256 and journal length before committing.
7. **Incremental journal validation:** the base is fully validated once; replayed journal suffix rows reuse the same sequence/idempotency/checkpoint sets instead of validating the whole history again.
8. **Conditional sorting:** runtime index rebuilds sort only if a linear monotonicity check finds disorder.
9. **Dirty chart backfill:** save boundaries revisit only unresolved released singles, not the full historical singles list.
10. **Lower reconciliation allocation:** post-mod show scans reuse scratch `HashSet`/`List` instances.
11. **Backup crash-window recovery:** `.imdc.bak` can recover with the still-present primary journal when it matches the backup base, and a failed journal-copy attempt keeps that source journal.
12. **Current-format persistence only:** sidecar format 3 and transactional journal format 2 are accepted; older persistence formats are intentionally unsupported.

## Compatibility

- Public IM Data Core API surface remains unchanged.
- Sidecar `FormatVersion` remains **3**.
- Journal `FormatVersion` is **2**.
- Older sidecar/journal formats are intentionally not supported.
- Project/mod version is **3.4.2**.


## Election numbering

- `election_number` now follows vanilla `SEvent_SSK` display semantics exactly: finished elections use `_SSK.Count`; the current unfinished election uses `SEvent_SSK.CountElections() + 1`.
- Election `ID` / event `EntityId` remains an internal lookup identity and is never the election ordinal.
