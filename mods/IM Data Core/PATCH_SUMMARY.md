# IM Data Core 3.4.6 patch summary

## Post-`cc924b6` audit follow-up - Pass 6

1. **UIF single-removal veto:** IMDC captures `singles._single.RemoveGirl` pre-state at `Priority.First` and explicitly orders before UIF. UIF declares the reciprocal `HarmonyAfter`; missing state is non-evidence rather than an empty cast.
2. **UIF medical vetoes:** injury, depression, and hiatus-start use reference-type pre-state and require the requested final status transition. Announced-graduation vetoes cannot emit false medical history.
3. **Assistant Manager loan replacement:** IMDC snapshots `loans.AddLoan` at `Priority.First` before Assistant Manager and emits `loan_added` only when the loan collection actually gains the loan, preserving true before/delta fields and suppressing rejected developing-loan calls.
4. **SWOF deletion boundary:** Save Write Ordering Fix 1.3.0 exposes a directory-exclusive lease. IMDC holds it across vanilla save-directory deletion and supplemental archival so an earlier queued writer cannot recreate the deleted save after sidecar detachment. Failed coordination blocks the deletion.
5. **Scope policy:** no backward-compatibility work was required or added for Pass 6; hypothetical unknown-transpiler and stack-provenance limitations remain out of the fix queue.


## Post-`cc924b6` audit follow-up - Pass 5

1. **Durable agency-room generations:** timeline history no longer promotes runtime-only `agency._room.id` into a persistent key. Each room receives an IMDC-owned `g:<guid>` generation.
2. **Exact-checkpoint reassociation:** new v5 checkpoints optionally freeze room generations in vanilla's serialized floor/room order; the load hook reassociates those generations as vanilla reconstructs each room. Older v5 checkpoints without the additive field remain valid and receive fresh forward-safe IDs.
3. **Theater/cafe collision prevention:** theater and cafe history use the owning room generation as `EntityId`, so destroyed highest IDs may be recycled by vanilla without merging two historical facilities. Raw vanilla IDs remain payload fields.
4. **Room-work continuity:** room-work compound history uses the durable room generation component and therefore survives save/load identity loss in vanilla.
5. **Public catalog contract:** `idol_status_changed`, `research_points_accrued`, and `idol_earnings_recorded` are explicitly catalogued as internal transient/non-queryable streams; the remaining 143 built-in types are the queryable catalog.
6. **Compatibility preserved:** sidecar format remains 5, journal format remains 2, and the new checkpoint member is optional/additive. Pass 5 does not attempt to retroactively invent identities absent from older checkpoints.

## Post-`cc924b6` audit follow-up - Pass 4

1. **Show compaction-window integrity:** `FlushAfterCaptureLocked()` defers non-forced threshold materialization while a post-mod show settlement is active. Ordinary and canonical show rows therefore remain together until canonical reconciliation has had a chance to replace/deduplicate the ordinary representation.
2. **Editor settlement scope:** the show editor canonical observer now wraps `Show_Popup.OnContinue()` rather than only nested `SaveShow()`. This covers the ordinary OnContinue Postfix in the same settlement window and closes the reverse ordering case where a canonical row could flush before its ordinary counterpart was enqueued.
3. **Idol-hire postcondition:** `data_girls.Hire()` records `idol_hired` only when a valid Prefix snapshot proves the idol was absent before and the runtime list contains that same idol afterward. Missing Harmony state is not interpreted as an empty pre-state.
4. **Rival-trend postcondition:** `Rivals.UpdateTrends()` records `rival_trends_updated` only when vanilla was eligible before the call and the successful-update timestamp marker actually changes. The generic monthly rival snapshot remains free of this unrelated eligibility check.
5. **Agency-room destruction postcondition:** `agency.DestroyRoom()` records destruction only for a room that was contained in the agency floor graph before the call and is absent afterward. Missing/unknown agency post-state cannot manufacture a destruction event.
6. **Compatibility preserved:** Pass 4 does not alter event payload schemas, event type names, sidecar format 5, journal format 2, or the Pass 1-3 persistence fixes.

## Post-`cc924b6` audit follow-up - Pass 3

1. **Backup-healing journal provenance:** backup recovery records the exact journal whose parsed header matched the backup base. If recovery used the still-present primary journal, a later healing snapshot durably publishes that same journal as `<sidecar>.imdc.bak.imdc.journal` before the primary journal can be removed. If the copy fails, the original journal is retained so the known-good `backup base + journal` generation remains recoverable.
2. **Preferred-journal selection:** journal probing now distinguishes missing journals, files torn before a complete header, header/base-hash mismatches, and real header matches. An empty or first-header-torn preferred primary journal is no longer treated as proof that it belongs to the backup base, so backup recovery proceeds to a valid sibling `.imdc.bak.imdc.journal` when available.
3. **Crash-temp scavenging:** physical-scope initialization removes only stale temporary files derived from that exact sidecar name (`<sidecar>.imdc.tmp.*` and backup-journal copy temps), under the per-path persistence lock. Files newer than 24 hours are left alone. Cleanup is best-effort and never blocks vanilla/IMDC loading.
4. **Policy preserved:** sidecar formats older than v5 remain intentionally unsupported, and IMDC still imposes no arbitrary forward checkpoint/history retention bound. Pass 3 does not convert either policy into a bug.

## Post-`cc924b6` audit follow-up - Pass 2

1. **Severance attribution scope:** ordinary `Fire()` no longer installs severance ambient context. `Fire_Severance()` now installs it immediately before the real deduction and clears it in both normal and exceptional exits.
2. **Static show cancellation postcondition:** `Shows.CancelShow(show)` records `show_cancelled` only when the show was in `Shows.shows` before the call and is absent afterward. This follows vanilla's actual removal semantics instead of waiting for a status mutation that does not occur.
3. **Audition terminal no-op guard:** the pre-call audition snapshot records the final-scandal blockade and `CaptureAuditionStarted` requires a valid snapshot that was not blocked.
4. **Random-event start proof:** `random_event_started` requires the active-event list to grow after `Event_Manager.StartEvent`; duplicate/disabled/actor-resolution early returns therefore cannot rediscover an older matching row and report it as new.
5. **Loan payoff transition proof:** `loan_paid_off` now requires a valid pre-snapshot with an active loan and an inactive post-state.
6. **Status-stream contract cleanup:** `single_status_changed`, `show_status_changed`, and `tour_status_changed` are documented as setter-observation streams, not exhaustive lifecycle journals. Initial release/tour-finish direct assignments remain represented by their dedicated lifecycle events.
7. **Generated catalog cleanup:** the Event Catalog is regenerated from current constants, so its summary counts and portrait-identity payload fields match source; the generator also emits the same readable section spacing as the checked-in catalog.

## Post-`cc924b6` audit follow-up - Pass 1

1. **Standalone snapshot failure hardening:** the primary compact-JSON round trip is no longer the only detachment mechanism when SWOF is unavailable/unhealthy. IMDC next uses a Unity-serialized-field graph clone, avoiding `JsonUtility.FromJsonOverwrite` because Idol Manager's UnityEngine API does not expose it.
2. **Fallback verification:** any fallback detached graph is compact-reserialized. If the source compact JSON was captured before the primary reconstruction failed, the fallback must reproduce it exactly before IMDC passes the graph to checkpointing and vanilla `DataSaver`.
3. **Vanilla remains canonical:** all-detachment failure still logs and fails open to vanilla rather than throwing through a save caller. The new fallbacks reduce the path that can reach the original live `SaveManager.Data` object without changing the five vanilla write targets or SWOF transpiler ordering.
4. **Policy/documentation cleanup:** older sidecar formats remain intentionally unsupported; unlimited history/checkpoint retention remains intentional; generated DLL revision metadata is treated as rebuildable output rather than source authority.

The original `cc924b6` release summary follows for historical context.

## 3.4.6 persistence-correctness changes

This revision closes four persistence/lifecycle defects found by static comparison with the supplied decompiled Idol Manager code and hardens the new deletion archive path against concurrent IMDC I/O.

1. **Collision-resistant exact checkpoints:** sidecar format 5 adds `ContentFingerprint`, a SHA-256 fingerprint of Unity's compact serialized vanilla `SavedData`. Exact checkpoint identity now includes normalized save path, vanilla `LastSave`, playtime seconds, vanilla game date/time, and this content fingerprint. Distinct vanilla saves can no longer collapse merely because their timestamp/playtime fields collide within the same second.
2. **Low-overhead fingerprinting:** standalone IMDC reuses the compact JSON already produced by its defensive `SavedData` freeze. SHA-256 consumes UTF-8 in bounded chunks so fingerprinting does not allocate a second save-sized byte array. When Save Write Ordering Fix is positively verified and IMDC skips its own freeze, one compact serialization is performed to obtain the fingerprint.
3. **Anchored adoption:** loading a vanilla career with no existing IMDC sidecar seeds an in-memory sequence-0 exact checkpoint. Loading alone still performs no unsolicited write, but a later `TryFlushNow` cannot create an unanchored sidecar that fails exact matching on the next load.
4. **Checkpoint watermark parser fix:** checkpoint `GameDateTime` is parsed through vanilla `ExtensionMethods.ToDateTime`, matching the persisted vanilla `yyyy-MM-dd HH:mm:ss` representation. Event/custom-mutation round-trip timestamp parsing remains unchanged.
5. **Deleted-save preservation:** successful vanilla deletion archives the matching mirrored IMDC directory instead of deleting it. `<name>` becomes `<name>OLD`; collisions use `<name>OLD2`, `<name>OLD3`, and so on. Whole story-playthrough deletion archives the corresponding mirrored playthrough tree as one unit.
6. **Archive/write serialization:** an exclusive persistence-topology lease serializes archival against sidecar load, write, and background compaction. Prepared snapshots carry per-path archive epochs, so a snapshot prepared before deletion cannot recreate the old path after archival.
7. **Archive-failure safety:** if the preservation rename fails, existing supplemental files are left untouched and writes beneath that deleted-save directory are blocked for the remainder of the process.
8. **Active-scope deletion:** deleting the currently active save detaches the physical scope while preserving the logical in-memory branch, allowing a later vanilla New Save/Save As to carry that history to a new path.
9. **Current-format-only development policy:** sidecar format 5 is the only accepted sidecar format. Older sidecars are preserved on disk but are not migrated or activated by this build. Transactional journal format remains 2.

## Retained 3.4.x behavior

- Exact checkpoints still capture the enabled Idol Manager mod inventory and emit diagnostic missing/disabled/metadata/DLL mismatch warnings without blocking vanilla load.
- Existing unmatched sidecars still fail closed rather than activating by in-game date alone.
- New Save/Overwrite Save keeps the active multi-path checkpoint ledger consistent while each physical sidecar serializes only checkpoints for its own target path.
- Process-wide per-path I/O locks, journal hashing, transactional journal replay, atomic compact-base replacement, backup recovery, and background compaction verification remain in place.
- Vanilla remains canonical: IMDC failures are fail-soft/fail-open at Harmony boundaries and do not intentionally prevent a vanilla save or load action.
- Public API behavior, money-ledger aggregation, reflection-safe interop, custom-event idempotency, portrait identity capture, graduation outcome capture, and staff-severance attribution remain unchanged.

## Compatibility and versions

- Project/mod version: **3.4.6**.
- Sidecar `FormatName`: `IMDataCore.LightweightSidecar`.
- Sidecar `FormatVersion`: **5 only**.
- Journal `FormatName`: `IMDataCore.LightweightJournal`.
- Journal `FormatVersion`: **2**.
- No runtime backwards-compatibility or migration path is provided for older sidecar formats in this development build.

## Vanilla deletion targets verified

The preservation hooks correspond to the three game-save directory deletion methods in the supplied decompilation:

- `Popup_Save.Delete()` for legacy/freeplay manual saves.
- `Popup_Load_Story.Delete_Save(save_info)` for a story save directory.
- `Popup_Load_Story.Delete_Playthrough(playthrough_info)` for a complete story playthrough directory.

Autosave UI does not expose the story save delete button, and unrelated `Directory.Delete` uses such as portrait/temp cleanup are intentionally not patched.
