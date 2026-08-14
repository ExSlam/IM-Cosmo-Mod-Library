# IM Data Core 3.2.0 patch summary

This source package layers long-history query and load-time scalability work on the 3.1 exact-checkpoint persistence design. EroEvents assets are intentionally outside this revision.

## Applied changes

1. **Paged idol timelines**: `TryReadEventsForIdolPage` walks the same merged idol/global timeline as recent reads using an exclusive EventId cursor, binary-search seeks, and a `hasMore` result.
2. **Streaming sidecar loading**: sidecars are read from a buffered sequential stream and normal v3 collections are converted one record at a time rather than through a whole-file string and whole-document JSON tree.
3. **Single-pass v3 payload validation**: structurally parsed v3 payload/custom SET JSON is no longer parsed again only to prove it is JSON.
4. **Linear checkpoint duplicate validation**: exact checkpoint identities are tracked in a `HashSet` instead of scanning every earlier checkpoint.
5. **Existing recent idol timeline ordering**: `TryReadRecentEventsForIdol` now performs a true newest-first merge of idol-specific and global timelines instead of filling from the idol list first.
6. **Substory completion after reload**: pending dialogue-completion tokens are rebuilt from vanilla's restored `Substories_Manager.dialogueQueue` after `LoadEvent` reconstruction.
7. **Exact checkpoint fail-closed loading**: the date-only activation fallback is removed. If an existing sidecar document has no exact checkpoint for the loaded vanilla save, supplemental state is detached read-only and the file is protected from overwrite. A physical save with no sidecar yet remains writable.
8. **Duplicate-safe custom event API**: both public API aliases expose `TryAppendCustomEventOnce(session, idempotencyKey, ...)`. The namespace/key identity is persisted on the event and follows active-branch rollback.
9. **Long-campaign persistence**: save snapshots use shallow immutable record lists, sidecar JSON streams directly to disk, JSON/fsync runs outside the controller runtime lock, normal forward saves avoid unnecessary complete index rebuilds, and sorted timeline indexes support efficient merged reads.
10. **Backup recovery**: an invalid primary sidecar may recover from a validated `.imdc.bak`; the damaged primary and known-good backup are protected until a successful replacement.
11. **Legacy/adjacent cleanup**: the obsolete legacy importer stub is removed, SQL-era notes are explicitly historical, and namespaced custom events cannot be misclassified as internal money-ledger rows solely by event-type text.

## Compatibility

- Public legacy API methods remain available.
- Sidecar `FormatVersion` remains **3**.
- `IdempotencyKey` is an optional v3 event member, so older v3 sidecars remain readable.
- Project/mod version is **3.2.0**.

See `docs/V3_VALIDATION.md` for validation details and compiler-environment notes.
