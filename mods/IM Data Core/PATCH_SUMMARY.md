# IM Data Core 3.1.0 patch summary

This source package applies the persistence and long-campaign corrections audited against the uploaded decompiled Idol Manager source and EroEvents integration.

## Applied changes

1. **Recent idol timeline ordering**: `TryReadRecentEventsForIdol` now performs a true newest-first merge of idol-specific and global timelines instead of filling from the idol list first.
2. **Substory completion after reload**: pending dialogue-completion tokens are rebuilt from vanilla's restored `Substories_Manager.dialogueQueue` after `LoadEvent` reconstruction.
3. **Exact checkpoint fail-closed loading**: the date-only activation fallback is removed. If an existing sidecar document has no exact checkpoint for the loaded vanilla save, supplemental state is detached read-only and the file is protected from overwrite. A physical save with no sidecar yet remains writable.
4. **Duplicate-safe custom event API**: both public API aliases expose `TryAppendCustomEventOnce(session, idempotencyKey, ...)`. The namespace/key identity is persisted on the event and follows active-branch rollback.
5. **Long-campaign persistence**: save snapshots use shallow immutable record lists, sidecar JSON streams directly to disk, JSON/fsync runs outside the controller runtime lock, normal forward saves avoid unnecessary complete index rebuilds, and sorted timeline indexes support efficient merged reads.
6. **Backup recovery**: an invalid primary sidecar may recover from a validated `.imdc.bak`; the damaged primary and known-good backup are protected until a successful replacement.
7. **Legacy/adjacent cleanup**: the obsolete legacy importer stub is removed, SQL-era notes are explicitly historical, and namespaced custom events cannot be misclassified as internal money-ledger rows solely by event-type text.

## Compatibility

- Public legacy API methods remain available.
- Sidecar `FormatVersion` remains **3**.
- `IdempotencyKey` is an optional v3 event member, so older v3 sidecars remain readable.
- Project/mod version is **3.1.0**.

See `docs/V3_VALIDATION.md` for validation details and `BUILD_NOTES.md` for the compiler limitation of this packaging environment.
