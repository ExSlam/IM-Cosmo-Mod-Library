# Changelog

## 3.4.7

- Removed runtime compatibility scaffolding for unsupported historical sidecar schemas: sidecar validation is exact `FormatVersion == 5`, pre-v3 event/custom-mutation readers are gone, and obsolete format-version plumbing was removed from current record decoders.
- Made `AgencyRoomIdentities` mandatory on every accepted v5 checkpoint, including an empty array for saves with no rooms. Removed the unreleased early-v5 missing-field compatibility path while retaining fail-safe handling for a present snapshot that does not match the loaded vanilla room layout.
- Removed the historical `show_episode` timeline alias. Current show episode history uses `show_episode_released`; the separate current money-detail token named `show_episode` is unchanged.
- Removed unused pre-transaction journal entry encode/decode routines, unused whole-document string sidecar encode/decode routines, obsolete checkpoint overloads, and unused save-scope compatibility wrappers.
- Removed bundled v2-v4 schema, migration, validation, implementation-note, and example documents. Historical-format migration is intentionally outside the runtime mod and can be handled by a future standalone migrator.
- Bumped project and mod metadata to 3.4.7 and updated current v5 documentation/examples to match the stricter schema.

## 3.4.6 Pass 6 build correction

- Removed the unavailable `JsonUtility.FromJsonOverwrite` call from standalone stable-save cloning. Idol Manager's UnityEngine reference exposes `FromJson<T>` but not `FromJsonOverwrite`; fallback now proceeds directly to the verified Unity-serialized-field clone.
- Changed the reference-identity comparer to explicit `IEqualityComparer<object>` implementations so it no longer triggers CS0108 by hiding `object.Equals(object, object)`.
- Updated the associated README/storage/validation documentation to match the actual game Unity API.

## 3.4.6 Pass 6 interoperability hardening

- Single `RemoveGirl` pre-state capture now runs before Unavailable Idols Fix and refuses to infer cast history from missing Harmony state. UIF declares the matching `HarmonyAfter` relationship.
- Injury, depression, and hiatus-start capture now uses reference-type pre-state plus real final-status postconditions, so UIF vetoes on announced-graduation idols cannot create false medical history.
- `loans.AddLoan` pre-state capture now runs at `Priority.First` before Assistant Manager replacement logic and requires a real loan-list insertion before emitting `loan_added`, preserving accurate before/delta fields and suppressing rejected developing-loan calls.
- When Save Write Ordering Fix 1.3.0 is loaded, save-directory deletion acquires SWOF's exclusive directory lease before vanilla deletion and releases it after IMDC archival. If SWOF is present but the boundary cannot be established, deletion is blocked rather than risking save resurrection.
- No backward-compatibility behavior was added or retained as part of this pass.

## Post-3.4.6 audit follow-up - Pass 5

- Replaced ephemeral `agency._room.id` historical identity with IMDC-owned room-generation IDs and persisted the room-generation map as an additive optional field on exact v5 checkpoints.
- Reassociated persisted room generations while vanilla reconstructs agency rooms, preserving room history across save/load without modifying vanilla save JSON. Early v5 checkpoints without the additive map remain readable and receive fresh forward-safe identities because their prior ambiguous room IDs cannot be reconstructed safely.
- Changed theater and cafe durable `EntityId` to the owning room generation so vanilla's recyclable `max(current IDs) + 1` identifiers cannot merge distinct historical facilities. Raw `theater_id`, `cafe_id`, and `room_id` payload fields remain unchanged for game-state correlation.
- Updated room-work identity to use the same durable room generation rather than runtime `agency._room.id`.
- Corrected the Event Catalog/API contract by separating 143 queryable built-in timeline event types from the three internal transient streams that retention intentionally drops before public queries: `idol_status_changed`, `research_points_accrued`, and `idol_earnings_recorded`.
- Kept sidecar format 5 and journal format 2. The new checkpoint field is additive and optional; no pre-v5 backward-compatibility policy was introduced.

## Post-3.4.6 audit follow-up - Pass 4

- Kept ordinary show capture and the final post-mod canonical observation in one pending compaction window. Capture-triggered threshold flushes are deferred while `postModShowSettlementDepth > 0`, then resume after the settlement closes. The show editor settlement now spans the full `Show_Popup.OnContinue()` commit instead of only its nested `SaveShow()` call so the ordinary OnContinue rows cannot land in a later batch than the canonical editor observation.
- Hardened `data_girls.Hire()` capture with an explicit pre-state snapshot and an absent-before -> contained-after postcondition. Duplicate/no-op hires and Harmony-vetoed calls no longer emit `idol_hired` or consume hire-attribution context.
- Hardened `Rivals.UpdateTrends()` capture with the real vanilla eligibility precondition plus a changed `Trend_Data.LastUpdated` postcondition. Invalid/no-op calls no longer emit `rival_trends_updated`.
- Hardened `agency.DestroyRoom()` capture to require contained-before -> absent-after membership in the agency floor graph. Non-contained room arguments no longer emit `agency_room_destroyed`, and an unavailable post-state is treated as unknown rather than proof of destruction.
- No event schema or sidecar/journal format changed in this pass.

## Post-3.4.6 audit follow-up - Pass 3

- Preserved complete backup generations after `.imdc.bak + primary journal` recovery. IMDC now records which journal actually matched/replayed with the backup base; if that source was the primary journal, the healing write durably publishes it as `.imdc.bak.imdc.journal` before removing the primary journal, and keeps the source in place if publication fails.
- Reworked journal probing so `missing`, `torn before header`, `header mismatch`, and `header matched` are distinct outcomes. Empty or first-header-torn preferred journals no longer masquerade as positive base-hash matches and can no longer mask a valid backup journal.
- Added conservative orphan-temp scavenging when a physical save scope is initialized. Only temp files derived from that exact sidecar name are eligible, cleanup is serialized by the per-path persistence lock, and files younger than 24 hours are retained.
- Kept the amended audit policy unchanged: pre-v5 sidecars are intentionally unsupported and unbounded forward history/checkpoint retention is intentional rather than a defect.

## Post-3.4.6 audit follow-up - Pass 2

- Moved staff-severance money ambient attribution from ordinary `staff._staff.Fire()` to the actual `Fire_Severance()` transaction scope, with Postfix/finalizer cleanup so stale severance metadata cannot leak into a later unrelated money mutation.
- Fixed static in-development `Shows.CancelShow(show)` capture to validate the real vanilla postcondition (present before, removed after) instead of requiring a `canceled` status that vanilla never sets on that path.
- Suppressed terminal-scandal false `audition_started` history by carrying the vanilla `Scandal_Auditions_No_More` precondition in the audition snapshot and refusing capture when vanilla takes that early-return branch.
- Suppressed duplicate/no-op `random_event_started` rows by requiring the active-event collection to grow beyond its pre-call count before resolving the newly scheduled event.
- Tightened `loan_paid_off` capture to the real `active before -> inactive after` transition; unaffordable/programmatic no-op calls no longer emit payoff history.
- Clarified the public contract for `single_status_changed`, `show_status_changed`, and `tour_status_changed`: these rows observe their respective setter methods and are not exhaustive lifecycle journals. Vanilla direct release/finish assignments are represented by the retained lifecycle events `single_released`, `show_released`, and `tour_finished`.
- Regenerated the Event Catalog from current source constants, correcting stale summary counts and restoring seven portrait-identity payload field rows that were present in source but missing from the generated documentation; the generator now preserves the catalog's readable section spacing.

## Post-3.4.6 audit follow-up - Pass 1

- Hardened standalone vanilla-save detachment when Save Write Ordering Fix is unavailable or not positively healthy. A failed normal `JsonUtility.FromJson<SavedData>` reconstruction now falls through to a Unity-serialized-field graph clone that is compact-JSON validated before use. The fallback avoids `JsonUtility.FromJsonOverwrite`, which is not exposed by Idol Manager's UnityEngine API.
- Preserved the vanilla-save fail-open boundary: if every independent detachment strategy fails, IMDC still logs and lets vanilla attempt its save rather than throwing through the game save caller.
- Clarified that sidecar formats older than 5 are intentionally unsupported in 3.4.6 and are not a backward-compatibility bug for this development line.
- Clarified that stale generated DLL revision metadata is a local build-artifact concern. Project/mod metadata remains authoritative in source, and DLL/PDB/bin/obj/artifact outputs stay ignored and should be regenerated from the desired commit.

## 3.4.6

- Added sidecar format 5 `ContentFingerprint` to exact vanilla-save checkpoints. The fingerprint is SHA-256 over Unity's compact serialized `SavedData`, closing same-second identity collisions that could occur when path/`LastSave`/playtime/game-date fields alone matched.
- Reused IMDC's existing standalone defensive-save JSON when available and streamed UTF-8 into SHA-256 in bounded chunks to avoid a redundant full serialization or save-sized byte-array allocation.
- Seeded an in-memory sequence-0 checkpoint when adopting an existing vanilla career with no IMDC sidecar, so a subsequent `TryFlushNow` persists an anchored sidecar that can match the vanilla save on reload.
- Fixed checkpoint date watermarks to parse vanilla checkpoint `GameDateTime` through `ExtensionMethods.ToDateTime` instead of the round-trip event-date parser.
- Added preservation hooks for vanilla manual-save, story-save, and whole-playthrough deletion. Mirrored IMDC directories are renamed to `OLD`, `OLD2`, `OLD3`, etc. rather than deleted, retaining complete supplemental history for future diary export.
- Serialized deleted-save archival against IMDC loads/writes/background compaction with a persistence-topology lease and per-path archive epochs so pre-delete snapshots cannot resurrect the deleted path.
- If an archive rename fails, IMDC leaves the supplemental directory untouched and blocks writes beneath that deleted-save directory for the rest of the process.
- Deleting the active physical save now detaches that physical binding while retaining the logical in-memory branch for a later New Save/Save As.
- Development persistence policy is now v5-only: older sidecar formats are not migrated or activated. Transactional journal format remains 2.

## 3.4.5

- Added sidecar format 4 checkpoint mod inventories. Every vanilla-save checkpoint now records every enabled Idol Manager mod from `Mods._Mods`, including JSON-only/non-Harmony/non-IMDC mods, with mod name, title, author, declared version, and discovered DLL file name(s).
- On exact-checkpoint load, compares the saved mod inventory with the current registry and warns about required mods that are missing, disabled, or have changed author/version/DLL names. The diagnostic never blocks vanilla loading.
- Retained read compatibility with sidecar format 3; older checkpoints simply have no mod inventory and migrate to format 4 on a later full sidecar write.
- Added a dedicated `idol_graduation_outcome` lifecycle milestone carrying vanilla's resolved `Graduation_Trivia_Text`, captured after graduation so JSON-only outcome additions are preserved.
- Added exact staff-severance money attribution at `staff._staff.Fire_Severance`, including severance amount, role, salary, and staff skill snapshot.
- Idol weekly salary allocations continue to use the exact final salary in vanilla's weekly deduction, now explicitly consumable as that week's paid amount.

## 3.4.4

- Added stable portrait identity fields to idol lifecycle events: raw idol type, custom-id/addressable identity, and exact body/hair/face/accessory asset IDs. This gives profile/history consumers enough vanilla-style references to identify both built-in unique idols and normal/modded portrait compositions without storing rendered images.
- Portrait identity capture is fail-soft so malformed or partially populated portrait data from another mod cannot interrupt hiring, graduation, or other lifecycle capture.

## 3.4.3

- Added an uncapped money-ledger aggregate query so consumers can calculate exact totals even when a month contains more rows than the display-page limit.
- Added a reflection-safe optional-integration API that accepts the consumer assembly explicitly, allowing Graduation Details to delegate its checkpointed supplemental snapshot to IM Data Core without a hard assembly dependency.
- Raised the per-value custom JSON ceiling to the existing 5 MiB per-namespace budget so one archival snapshot can carry complete Graduation Details state.

## 3.4.2

- Made every persisted `election_number` mirror vanilla election numbering directly: finished elections use `_SSK.Count`, while the current unfinished election uses `SEvent_SSK.CountElections() + 1`.
- Kept `_SSK.ID`/event `EntityId` strictly as internal identity and lookup data; it is not used as the player-facing election number.

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
