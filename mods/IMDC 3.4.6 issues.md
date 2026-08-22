# IMDataCore 3.4.6 Audit Issues

## Scope

This document consolidates all findings reported during the multi-pass audit of **Cosmo Mod Library 3.4.6 IMDataCore** against the supplied **decompiled Idol Manager vanilla source**, plus interoperability checks against other bundled Cosmo mods.

The audit was performed in six passes:

1. Persistence lifecycle and vanilla save/load parity
2. Gameplay capture correctness
3. Storage engine, journal, compaction, and crash recovery
4. Broad Harmony patch audit
5. Historical reconstruction and public API consistency
6. Mod interoperability and Harmony ordering

## Issue-classification policy

For this audit, the following are **not bugs by themselves** and are excluded from the fix queue unless a concrete supported-use failure is demonstrated:

- **Backward compatibility with older IMDC sidecar formats is not a requirement.** IMDC 3.4.6 intentionally accepting only sidecar format 5 is therefore a supported-format policy, not a defect.
- **The absence of arbitrary caps on retained mod data, history size, or custom JSON nesting/depth is not an issue by itself.** The audit requires a concrete correctness, stability, or supported-use failure rather than treating an unlimited design as inherently defective.
- **Unsupported hypothetical future third-party Harmony rewrites are out of scope.** A problem must be present in the supplied code/mod set or otherwise concretely reproducible; the audit does not attempt to solve an unlimited set of theoretical future patch combinations.
- **Generated DLL metadata is not the source of truth for the audited commit.** At `cc924b6`, IM Data Core's project and mod metadata report 3.4.6, `*.dll` is already ignored by `.gitignore`, and no DLL is tracked by that commit. A clean/manual `dotnet build` regenerates the assembly from the checked-out source and project metadata.

The intent throughout was to avoid false positives. Findings were only retained when their control flow was traced against the relevant vanilla and/or bundled-mod implementation. Several initially suspicious behaviors were explicitly ruled out and are documented later in this file.

---

# Executive Summary

IMDataCore 3.4.6 has a **strong persistence core** in its normal v5 sidecar + v2 journal path. The save-path mirroring, checkpoint fingerprinting, rewind semantics, transaction framing, replay logic, and most historical reconstruction behavior are careful and generally correct.

The most important problems found are not ordinary base/journal corruption. They are instead:

- **incorrect capture caused by Harmony-hook placement or vetoed originals**;
- **historical entity IDs that vanilla does not actually preserve uniquely**;
- **narrow backup-recovery holes**;
- **a post-mod show canonicalization race at the flush boundary**;
- **several interoperability bugs with other bundled Cosmo mods**.

The single most serious live-history correctness defect found is the **staff severance ambient-context leak**, because it can mislabel a later unrelated money transaction and persist the incorrect attribution.

The strongest historical reconstruction defect is the use of **`agency._room.id` as a durable entity identity**, even though vanilla does not serialize that room ID and reloads old rooms with the default ID `0`.

---

# Priority Ranking

A reasonable priority order based on correctness impact is:

1. **Staff severance stale ambient attribution**
2. **Agency room historical identity failure across save/load**
3. **UIF temporary single-preservation creating false `single_cast_changed` history**
4. **UIF vetoed medical mutations still producing IMDC medical events**
5. **Assistant Manager `loans.AddLoan()` replacement skipping IMDC pre-state snapshot**
6. **Theater/cafe historical ID reuse**
7. **Backup recovery preserving `.bak` while deleting the journal needed to complete it**
8. **Backup journal selection treating empty/torn preferred journal as a positive base-hash match**
9. **Static show cancellation not captured**
10. **Show canonical ordinary/canonical duplication at the 256-event flush boundary**
11. **False audition-start history on the final scandal no-op branch**
12. **Duplicate/no-op random event scheduling producing false history**
13. **Public Event Catalog listing built-in timeline event types that retention drops before query**
14. Lower-severity no-op/interoperability issues

---

# Pass 1: Vanilla Persistence Lifecycle and Save/Load Parity

## Overall result

No confirmed normal-operation vanilla-save corruption or load mismatch was found in the core 3.4.6 persistence flow.

### Areas checked

- every vanilla `SavedData` write site
- `DataSaver.saveData<T>` and `loadData<T>`
- both `SaveManager.LoadData` overloads
- `SaveManager.GetLatestAutosavePath`
- chapter, autosave, freeplay manual-save, and story manual-save paths
- `Popup_Save`
- `Popup_Load_Story`
- new-game detachment
- save deletion and playthrough deletion
- sidecar checkpoint identity and fingerprinting
- sidecar initialization/adoption
- high-level journal/snapshot persistence ordering
- bundled Release DLL versus source revision

## Clean: vanilla write-site coverage

IMDC mirrors all five vanilla `SavedData` write sites correctly:

1. `SaveManager.SaveData(bool,bool)`
2. `SaveManager.SaveChapter(tasks._chapter)`
3. `Popup_Save.Save()`
4. `Popup_Load_Story.Do_Overwrite_Save(save_info)`
5. `Popup_Load_Story.Do_New_Save(string)`

The IMDC transpiler expects exactly one `DataSaver.saveData<SaveManager.SavedData>` call in each target, and the supplied vanilla decompilation has exactly one in each.

## Clean: vanilla path mirroring

`CorePaths.TryResolveDataSaverPath` correctly reproduces vanilla's mixed path behavior, including:

- relative `manual_saves/<id>/save`
- direct `auto_save` / `manual_save`
- rooted chapter paths
- full-path story-save calls
- `.json` appending behavior

## False positive ruled out: restoration before `LoadEvent`

IMDC restores supplemental state **before** `SaveManager.LoadEvent()`.

This initially looks dangerous, but vanilla `staticVars.LoadFunction()` mutates the just-loaded save by calling:

```csharp
PlayerData.SetLastSaveNow(false)
```

Therefore restoring/fingerprinting after `LoadEvent` would observe a vanilla object that has already been changed from the on-disk save.

**Conclusion:** IMDC's pre-`LoadEvent` restoration timing is correct. Moving it later would introduce an identity bug.

## Clean: exact checkpoint matching

V5 checkpoint identity includes:

- normalized vanilla save path
- `LastSave`
- playtime seconds
- in-game date/time
- SHA-256 of compact `SavedData` JSON

This prevents same-timestamp collisions and lets IMDC cope with out-of-order completion of vanilla asynchronous saves.

IMDC does not silently select the newest sidecar checkpoint when the vanilla save does not exactly match. It detaches rather than loading the wrong supplemental history.

## Clean: first adoption of a vanilla save with no sidecar

3.4.6 seeds an in-memory sequence-0 checkpoint for an exact vanilla save that has no IMDC sidecar yet. This avoids creating an unanchored sidecar if `TryFlushNow` happens before the next vanilla save.

## Clean: back-to-back asynchronous vanilla saves

Vanilla `DataSaver.saveData` starts a raw background thread. In principle two same-slot writes can finish out of initiation order.

IMDC handles this by retaining fingerprinted checkpoints rather than assuming "last initiated" equals "last physical file". If the older vanilla payload wins the race, its matching IMDC checkpoint can still be selected.

## Confirmed conditional persistence weakness: failed defensive stable-snapshot clone

When IMDC is running without a verified healthy Save Write Ordering Fix, it normally protects vanilla's asynchronous serializer by cloning the `SavedData` graph with a `JsonUtility` round trip and handing the detached snapshot to both IMDC and vanilla.

If this defensive clone fails, `CreateStableSaveSnapshot()` deliberately falls back to the original live `SaveManager.Data` object so vanilla can still try to save.

That reintroduces vanilla's race:

1. IMDC fingerprints the live object synchronously.
2. Vanilla receives the same live object.
3. Vanilla serializes it later on a worker thread.
4. Gameplay may mutate it before serialization completes.

Possible result: the physical vanilla file no longer matches the checkpoint fingerprint IMDC created.

On next load, IMDC correctly refuses the mismatch rather than loading the wrong history.

**Classification:** low-probability conditional supplemental-persistence availability issue. It does not corrupt the vanilla save, but the corresponding IMDC history can become inaccessible for that checkpoint.

## Clean: deletion handling

IMDC correctly mirrors the vanilla deletion methods for:

- freeplay manual save: `Popup_Save.Delete()`
- story individual save: `Popup_Load_Story.Delete_Save()`
- whole story playthrough: `Popup_Load_Story.Delete_Playthrough()`

It waits until the vanilla directory is actually absent before archiving the mirrored sidecar directory.

The apparent story-autosave deletion issue is not a normal UI path because vanilla disables the delete button for auto saves.

## Build artifact note: stale DLL revision metadata is not a `cc924b6` source defect

The bundled Release DLL identifies itself as 3.4.6 and contains the newer 3.4.6 persistence code, but its embedded source-revision metadata points to:

```text
431cadde1c811a08ba25cccb90e966d015d987a9
```

while the source tree is at:

```text
cc924b6c130c27d62b71b40aab16472bdb698ba4
```

The DLL also contains code added in the newer revision, so the likely explanation is that it was built from an uncommitted 3.4.6 working tree while Git HEAD still pointed at the older commit.

For the audited commit, the canonical source metadata is already consistent: `mods/IM Data Core/IM Data Core.csproj` reports version 3.4.6 and `mods/IM Data Core/assets/info.json` reports version 3.4.6. The repository's `.gitignore` already contains `*.dll`, `**/bin/`, `**/obj/`, and `artifacts/`, and `cc924b6` tracks no DLL files. A clean/manual `dotnet build` from the checked-out commit therefore regenerates the DLL metadata from the current source/project state rather than relying on the stale untracked build output bundled in the supplied working-tree ZIP.

**Classification:** not an IMDC 3.4.6 source/runtime bug. Treat stale generated DLLs as local build artifacts; keep DLL/build outputs ignored and rebuild them from the desired commit.

---

# Pass 2: Gameplay Capture Correctness

## 1. High: staff severance ambient-context hook is attached to the wrong method

IMDC attaches severance ambient context around ordinary:

```csharp
staff._staff.Fire()
```

instead of the actual severance method:

```csharp
staff._staff.Fire_Severance()
```

Vanilla behavior:

- `Fire()` removes the staffer and updates fired state, but performs **no money deduction**.
- `Fire_Severance()` performs the actual `resources.Add(... -Severance())` deduction.

IMDC's ordinary `Fire()` Prefix can install a static severance context. Because no money transaction happens in `Fire()`, nothing consumes that context.

The next unrelated money transaction can then be classified as:

```text
staff._staff / Fire_Severance
```

and can inherit the fired staffer's severance metadata.

A later actual severance deduction for another staffer can also consume stale metadata from the previously fired employee.

This is reachable through normal vanilla UI because ordinary firing and severance firing are independently exposed when their respective eligibility checks pass.

**Classification:** confirmed normal-gameplay persistent ledger misattribution bug.

### Fix direction

Install and clear the detailed severance context around `Fire_Severance()`, not `Fire()`, with exception-safe cleanup.

## 2. Moderate: static in-development show cancellation is never captured

IMDC patches:

```csharp
Shows.CancelShow(show)
```

and requires the resulting show status to be `canceled` before recording `show_cancelled`.

Vanilla `Shows.CancelShow()` does not set that status. It:

- cancels the room job
- removes the show from `Shows.shows`

Therefore the IMDC guard never succeeds for the normal static cancellation route.

**Classification:** confirmed missing persistent lifecycle event in normal gameplay.

## 3. Moderate/edge: final-scandal `GenerateAudition()` early return still emits `audition_started`

Vanilla `Auditions.GenerateAudition()` can return immediately when:

```csharp
Story_Data.Scandal_Auditions_No_More
```

is set. It starts the final scandal/game-over dialogue and does not start an audition, spend money, or begin the audition coroutine.

IMDC's postfix still calls `CaptureAuditionStarted(...)` without proving the audition actually started.

**Classification:** confirmed false persistent event on a real vanilla terminal-scandal state.

## 4. Moderate/conditional: duplicate/no-op `Event_Manager.StartEvent()` can emit duplicate `random_event_started`

Vanilla refuses to schedule a duplicate active random event and simply returns.

IMDC snapshots `ActiveEventCountBefore`, but `CaptureRandomEventStarted()` does not use that count to prove a new active event was appended. Instead it searches for a matching active event after the call and can find the already-existing one.

Result:

```text
same random event already active
→ vanilla no-ops
→ IMDC finds old active event
→ IMDC records another random_event_started
```

**Classification:** confirmed capture-guard defect.

## 5. Low–Moderate semantic inconsistency: status-change streams are not exhaustive

IMDC exposes event types such as:

- `single_status_changed`
- `show_status_changed`
- `tour_status_changed`

by patching setter methods.

Vanilla sometimes bypasses those setters with direct assignments, including:

- initial single release
- initial show release
- tour completion

Those lifecycle actions are usually captured by separate retained lifecycle events, so the underlying action is not necessarily lost.

However consumers cannot safely treat `*_status_changed` as a complete transition journal.

**Classification:** confirmed semantic coverage inconsistency.

## 6. Low/latent: failed `loan.PayOff()` can theoretically emit `loan_paid_off`

Vanilla `loan.PayOff()` returns without changing the loan when `CanPayOff()` is false.

IMDC suppresses only the case where the loan was already inactive both before and after. It does not require the real transition:

```text
active before → inactive after
```

Therefore a programmatic call while the loan is active but unaffordable can still produce `loan_paid_off`.

Normal vanilla UI disables the payoff action when `CanPayOff()` is false.

**Classification:** real robustness defect, low practical vanilla severity.

## False positives ruled out in Pass 2

### Policy selection

Initially suspected because public `Select(bool)` can defer through a confirmation popup. IMDC actually patches private `_Select(bool)`, the commit method, so canceling the popup does not produce a false policy decision.

### Dating stage timing

Initially suspected because some route-stage changes occur in callbacks passed to `Date_Popup.End(...)`. Vanilla invokes the callback synchronously before returning, so IMDC's postfix sees the settled stage.

### Load-time synthetic history

Vanilla reconstructs some state by invoking gameplay-like methods during `LoadEvent`. IMDC keeps `saveLoadPreparationActive` set during that process and the central enqueue path suppresses captures, so reload does not synthesize policy/research history.

### Room-work cancellation/completion

The room-work subsystem generally matches vanilla completion conditions, snapshots cancellation before `CancelJob()` erases useful state, and distinguishes forced training cancellation.

---

# Pass 3: Storage Engine, Journal, Compaction, Crash Recovery

## Overall result

The normal v5 base + v2 journal design is strong.

- Transactions are framed with `BEGIN` / rows / `COMMIT`.
- Incomplete transactions remain invisible.
- A torn first header is treated as disposable while the already-safe compact base remains authoritative.
- Append uncertainty is re-read before fallback materialization, preventing ordinary double-application.
- Base/journal pairing uses the compact-base SHA-256.
- Same-path compaction/save operations serialize correctly.

The confirmed weaknesses are mostly in **second-order recovery**.

## Supported-format policy note: 3.4.5 v4 sidecars are not loaded/migrated by 3.4.6

3.4.6 sets:

```csharp
MinimumSupportedSidecarFormatVersion = 5;
SidecarFormatVersion = 5;
```

But 3.4.5 wrote sidecar format 4.

So a normal upgrade can produce:

```text
3.4.5 v4 history
→ install 3.4.6
→ primary v4 rejected
→ backup usually v4 too
→ old sidecar remains preserved on disk but cannot be activated
```

This is deliberate according to the 3.4.6 policy. It is not silent file destruction.

The design reason is understandable: v5 adds a vanilla-save SHA-256 fingerprint, and old historical v4 checkpoints do not contain the exact vanilla serialized content needed to manufacture safe fingerprints retroactively.

**Classification:** intentional supported-format policy, **not a bug for this audit**. Backward compatibility with earlier IMDC sidecar formats is not a requirement.

## 1. Moderate: healing after `.bak + primary journal` recovery can destroy the journal that completes the preserved backup

IMDC supports a narrow interrupted-compaction recovery state where:

- `.imdc.bak` contains the old compact base
- the still-present **primary** journal contains the committed suffix for that backup
- `.imdc.bak.imdc.journal` may not yet exist because the crash happened before the copy step

IMDC can successfully recover from:

```text
backup base + current primary journal
```

After that it heals the primary with `PreserveExistingBackup = true`.

The problem: when preserving the existing backup, the healing path skips copying the current journal to `.bak.imdc.journal`, then later deletes the current journal anyway.

Result:

```text
B + J = complete known-good backup generation
↓ recover
new primary healed
↓
B preserved
J deleted
BJ absent
```

The preserved backup is no longer a complete copy of the generation it just proved recoverable.

If the newly healed primary later becomes unreadable, backup recovery may no longer contain the exact checkpoint that matched the vanilla save.

**Classification:** confirmed recovery-redundancy bug.

### Fix direction

Track **which journal** participated in backup recovery. If the backup relied on the primary journal, durably establish `.bak.imdc.journal` before deleting the original journal.

## 2. Moderate/edge: empty/torn preferred journal can falsely mask a valid backup journal

`TryReplayJournalFileLocked()` initializes `baseHashMatched = true` and returns success for an empty journal or a journal torn before its header is complete.

For normal primary loading, treating such a journal as ignorable is reasonable.

But backup recovery interprets `baseHashMatched == true` as proof that the preferred journal belongs to the backup base and immediately returns, skipping the sibling `.bak.imdc.journal`.

Possible state:

```text
valid backup base B
valid backup journal BJ
empty/torn current journal J
corrupt primary P
```

Backup recovery tries J first, gets success + `baseHashMatched=true`, and never examines valid BJ.

If the exact vanilla checkpoint exists only in BJ, IMDC detaches instead of recovering.

**Classification:** confirmed recovery-selection bug.

### Fix direction

Do not report a positive base-hash match until a real journal header has been parsed and compared. Distinguish states such as:

- no journal
- torn before header
- header mismatch
- header match + replay success
- header match + replay failure

## 3. Low: crash-created `.imdc.tmp.*` files are not scavenged on later startup

Atomic snapshot/journal-copy helpers create GUID temp files and clean them in `finally`.

A hard process kill bypasses `finally`, and no later startup scavenger removes abandoned temp files.

Repeated interrupted full snapshots can leave full-history-sized temp files behind.

**Classification:** confirmed persistence hygiene/disk-growth issue, not canonical sidecar corruption.

## Intentional design note: checkpoint retention has no normal forward bound

Forward autosaves create new exact checkpoint identities whenever the vanilla save fingerprint changes.

Old checkpoints are removed on branch rewind or whole-scope deletion, but not simply because many newer checkpoints exist for the same physical autosave path.

Vanilla repeatedly overwrites one autosave file; IMDC can retain many historical exact identities for it.

Each checkpoint also contains enabled-mod inventory metadata.

This is partly intentional because retaining multiple identities helps survive out-of-order vanilla asynchronous writes.

The audit does not treat the absence of an arbitrary retention limit as a defect. The same policy applies to supported mod data/JSON depth or nesting: an unlimited design is not itself an issue without a demonstrated correctness, stability, or supported-use failure.

**Classification:** intentional/unbounded storage characteristic, **not a bug for this audit**.

## Clean storage behaviors confirmed

- incomplete v2 transaction is not exposed
- final COMMIT without trailing newline can still be parsed
- append uncertainty does not blindly duplicate data
- journal header ties suffix to exact compact-base SHA-256
- newly replayed payloads undergo sequence/idempotency/fingerprint validation
- custom-event idempotency survives reload correctly
- background compaction validates physical base hash and journal length before replacement
- topology/archive epochs prevent stale writers from recreating an archived sidecar path

---

# Pass 4: Broad Harmony Capture Audit

## Scope note

Pass 4 covered the substantial majority of vanilla-facing Harmony patches and cross-checked controller capture logic as well as patch entry points.

It was not claimed as literally 100% complete. A few miscellaneous/dynamic targets remained less deeply checked, notably:

- `vn_actions.DoActor`
- Summer Games neighboring hooks
- optional external `EroEvents` semantic integration

The EroEvents part also cannot be semantically proven from vanilla source alone because it depends on another mod's implementation.

## 1. Moderate: show ordinary + canonical post-mod events can escape deduplication at the 256-event flush boundary

IMDC intentionally observes some show operations twice:

1. ordinary capture immediately after vanilla mutation
2. later canonical post-mod observation after known mod postfixes settle

`CorePayloadCompaction` normally removes the ordinary row when both ordinary and canonical records are still in the same pending batch.

The problem is that ordinary capture can call `FlushAfterCaptureLocked()`, and the normal buffered threshold is 256 events.

If the ordinary show row crosses the threshold before the post-mod canonical finalizer runs:

```text
ordinary row captured
→ threshold reached
→ ordinary row flushed/materialized
→ canonical row captured later
→ canonical row enters a new pending batch
```

Compaction cannot compare the new canonical row against already-active history.

Result: one vanilla show occurrence can appear twice during the current session.

Loaded-history compaction can later collapse the duplicate after restarting, meaning the same historical query can produce a different answer before and after reload.

The same architectural risk applies to canonical show cast reconciliation.

**Classification:** confirmed session-consistency defect, timing/buffer dependent.

### Fix direction

While `postModShowSettlementDepth > 0`, defer ordinary show flushing until the canonical observation has been captured. Then both representations remain in the same pending batch and existing compaction can do its job.

## 2. Low: duplicate `data_girls.Hire()` can emit false `idol_hired`

Vanilla returns immediately when the girl is already present in `data_girls.girl`.

IMDC's postfix can still call `CaptureIdolHired(girl)` without proving the idol was newly inserted.

Normal vanilla UI does not ordinarily try to hire an already-hired idol.

**Classification:** real interoperability/programmatic no-op defect, low normal-play severity.

## 3. Low: invalid `Rivals.UpdateTrends()` can emit false trend history

Vanilla returns when `Rivals.CanUpdateTrends()` is false.

IMDC can still emit `rival_trends_updated` without proving the update actually happened.

Normal UI guards the action.

**Classification:** robustness/inter-mod defect.

## 4. Low: non-contained `agency.DestroyRoom()` can emit false destruction history

Vanilla can return when the supplied room object is not actually found in agency floors.

IMDC can still record `agency_room_destroyed` based on a valid-looking pre-snapshot without proving contained-before / absent-after.

Normal room UI supplies a real agency room.

**Classification:** API robustness defect.

## Important findings reaffirmed during Pass 4

The exhaustive sweep strengthened confidence in earlier findings:

- staff severance context leak remains the most serious capture defect
- static show cancellation remains definitely broken
- audition terminal early return remains a real false event
- random-event no-op duplication remains real
- direct field assignments confirm status-stream non-exhaustiveness

## Areas that largely survived the broader audit

- singles lifecycle/cast changes, aside from known direct-status semantics and later UIF interoperability issue
- groups
- contracts
- relationships
- research
- tasks/wishes
- scandal state
- ordinary medical state under vanilla-only operation
- room work
- concerts/awards
- tours/elections
- theater/cafe revenue scopes
- show/cast post-mod settlement architecture in normal non-boundary cases

## False positive ruled out: canonical show payload `ShowId`

A canonical show payload looked like it might not initialize `ShowId`, but the serializer does not serialize that payload field for either representation. Entity identity carries the show ID instead.

**Not a bug.**

---

# Pass 5: Historical Reconstruction and Public API Consistency

## 1. High for room history: `agency._room.id` is not persisted by vanilla but IMDC treats it as durable identity

IMDC uses vanilla room IDs as persistent entity identity for:

- `agency_room_built`
- `agency_room_destroyed`
- room-work identity strings

Vanilla assigns new rooms from a runtime-only static counter:

```csharp
agency.roomIDCounter++;
room.id = agency.roomIDCounter;
```

But vanilla `agency.RoomData` does **not** serialize that `id`, and load reconstruction creates a fresh `agency._room` without assigning an ID.

Therefore loaded rooms get the default integer value:

```text
room.id = 0
```

IMDC has no load hook or side mapping that restores those room IDs, and vanilla does not reconstruct `roomIDCounter` from loaded rooms.

### Consequences

Before save:

```text
Room A → id 1
Room B → id 2
Room C → id 3
```

After reload:

```text
Room A → id 0
Room B → id 0
Room C → id 0
```

So:

- one physical room changes historical identity across reload
- multiple loaded rooms collapse onto entity ID `0`
- post-load room destruction/work can refer to ID `0` even though no pre-save `agency_room_built` for ID `0` exists
- after a fresh process restart, the next newly built room starts again at ID `1`, colliding with a historical pre-restart room `1`
- grouping by `(EntityKind, EntityId)` can merge unrelated rooms

This is not merely vanilla's fault because vanilla itself does not rely on `_room.id` as a serialized durable key. The historical inconsistency arises from IMDC promoting an ephemeral runtime field into a durable entity identity.

**Classification:** confirmed normal-gameplay historical identity break.

### Fix direction

Use an IMDC-owned durable room-generation identity and persist/reassociate it across load. Simply renumbering rooms after load avoids some current-session collisions but does not reconnect them to pre-save history.

## 2. Moderate: theater IDs are recyclable but used as durable historical identity

Vanilla theater IDs come from:

```text
max(current theater IDs) + 1
```

There is no monotonic historical counter.

Destroying the highest-numbered theater can allow the next theater to reuse the same ID.

IMDC uses raw `theater.ID` as `EntityId` for lifecycle and daily history.

Result: two distinct theaters can appear as one historical entity with two creation lifecycles.

**Classification:** confirmed normal-play historical identity collision.

## 3. Moderate: cafe IDs have the same recycling problem

Vanilla cafes use the same `max(current IDs) + 1` strategy.

Destroying and rebuilding can reuse a prior cafe ID, while IMDC uses raw `cafe.ID` as durable entity identity.

**Classification:** confirmed normal-play historical identity collision.

## 4. Low–Moderate API inconsistency: Event Catalog advertises three built-in types that public timeline APIs cannot return

The generated Event Catalog lists built-in event types including:

- `idol_status_changed`
- `research_points_accrued`
- `idol_earnings_recorded`

and presents the catalog as a reference for filtering/classifying timeline rows.

But `CoreEventRetention` explicitly drops those three built-in streams before durable/queryable history.

Public timeline APIs force a flush before reading, so even an immediate query causes these buffered rows to be filtered out first.

Thus the documented timeline event types are effectively unqueryable through the advertised timeline APIs.

The retention policy itself may be intentional. The inconsistency is between that policy and the public Event Catalog/API contract.

**Classification:** public API/documentation defect.

### Fix direction

Either:

- remove them from the consumer event catalog
- clearly mark them transient/internal/non-queryable
- or expose an actual live telemetry API if consumers are meant to observe them

## Clean: rewind/custom-data/idempotency reconstruction

Rewinding to an older exact checkpoint correctly rebuilds runtime indexes from the active branch only.

- abandoned-future custom JSON mutations disappear
- abandoned-future idempotency keys disappear
- idempotency can legitimately happen again on the new branch
- `LastIssuedSequence` deliberately stays high, preventing EventId reuse/collision

## Clean: timeline pagination

The paged APIs do not incorrectly assume numeric EventId order. They resolve the cursor EventId back to its row and page using its `(GameDateKey, Sequence)` position.

No skipped boundary row, repeated cursor row, or obvious `hasMore` inconsistency was found.

## Clean: shared-event projection

Shared event rows are fanned out into per-idol derived timelines using participant lists. The checked serializers and participant parsers align for shows, singles, concerts, tours, elections, room work, relationships, mentorship, random events, and substories.

## Clean: money-ledger reconstruction beyond the earlier capture bug

Detailed transaction allocations maintain a running balance from the captured vanilla before-balance to the actual after-balance, including remainder/reconciliation handling.

No second arithmetic reconstruction defect was found beyond the earlier **severance misattribution**, which poisons the row before the otherwise-correct materialization layer sees it.

---

# Pass 6: Interoperability and Harmony Ordering

## Overall result

IMDC is not generally careless about Harmony ordering. Several integrations are deliberately coordinated, especially:

- Save Write Ordering Fix
- show/cast settlement with known mods
- Assistant Manager room production
- release-blocking behavior in Unavailable Idols Fix

However, three concrete bundled-mod interactions are wrong because another mod can return `false` from a Prefix before IMDC's late state-capturing Prefix runs, while IMDC's Postfix still executes.

## 1. Moderate: UIF temporary single preservation can create false `single_cast_changed` history

Unavailable Idols Fix patches:

```csharp
singles._single.RemoveGirl(...)
```

and, during a temporary unavailability/removal scope, returns `false` so the idol remains in the single.

IMDC's single cast patch takes its pre-state snapshot at `Priority.Last`.

If UIF's boolean Prefix runs first and skips the original, IMDC's stateful Prefix may not run, but its Postfix still does.

IMDC then receives `__state = null` and substitutes a fresh/default snapshot, effectively treating the previous cast as empty and previous status as normal.

Possible sequence:

```text
real before: A, B, C
UIF blocks RemoveGirl(B)
real after:  A, B, C
IMDC fake before from null state: empty
IMDC after: A, B, C
→ false single_cast_changed
```

Existing cast members can be reported as additions even though nothing changed.

This is reachable through normal gameplay with UIF during temporary unavailability flows such as injury, depression handling, or hiatus while the idol belongs to an unreleased single.

**Classification:** confirmed bundled-mod persistent-history bug.

### Why equivalent show/concert UIF paths are cleaner

UIF's show/concert removal patches explicitly run **after** IMDC, allowing IMDC to snapshot the true before-state before UIF suppresses the mutation.

The single-removal patch lacks that protection.

### Fix direction

Do not interpret missing `__state` as an empty/default historical state. Either:

- snapshot at `Priority.First`
- explicitly order before UIF
- or suppress inference when the pre-state snapshot is missing

## 2. Moderate: UIF can veto injury/depression/hiatus while IMDC still records the medical event

UIF intentionally returns `false` for medical transitions on idols in `announced_graduation` state.

IMDC's medical patches use late Prefix snapshots and unconditional Postfix capture.

When UIF skips the original before IMDC's Prefix runs, IMDC's Postfix can still record:

- `medical_injury`
- `medical_depression`
- `medical_hiatus_started`

without the requested vanilla mutation actually occurring.

The payload can even contradict itself, for example:

```text
medical_event_type = injury
medical_current_status = announced_graduation
```

This is reachable normally because UIF modifies activity eligibility so announced-graduation idols can still reach vanilla's daily injury/depression attempt machinery, then vetoes the actual state transition.

**Classification:** confirmed bundled-mod false-history bug.

### Fix direction

Require a real postcondition transition, e.g.:

```text
injury: previous != injured AND final == injured
```

and similarly for depression/hiatus, ideally also requiring a valid pre-snapshot.

## 3. Moderate: Assistant Manager replaces `loans.AddLoan()` before IMDC snapshots the pre-state

Assistant Manager patches `loans.AddLoan()` with a Prefix that performs its own loan logic and always returns `false` to skip vanilla.

IMDC's `AddLoan()` snapshot Prefix is `Priority.Last`, so Assistant Manager can mutate the loan before IMDC's snapshot runs, or prevent IMDC's snapshot from running at all.

IMDC's Postfix then accepts `snapshotBefore = null` and substitutes current values.

Possible malformed fields on a valid loan:

```text
money_before = money_after
money_delta = 0
debt_before = current debt after operation
active_before = false/default
```

The `loan_added` event itself may still be correct, but its before/after financial context is inaccurate.

IMDC already solves the analogous Assistant Manager room-production replacement problem by taking snapshots at `Priority.First`. The loan hook does not.

**Classification:** confirmed bundled-mod normal-gameplay history-quality bug.

### Lower-severity branch

If Assistant Manager rejects a developing loan because no suitable office exists, IMDC can theoretically still emit `loan_added` without proving membership/count changed. Normal UI validation usually prevents this direct invalid call.

## 4. Low–Moderate conditional: SWOF queued writer can theoretically recreate a save after IMDC archives it

Save Write Ordering Fix exposes exclusive file-access helpers, but IMDC only reflects the SWOF health property and does not use those exclusive-access APIs.

Possible race:

```text
SWOF has queued write for save X
→ save directory deleted
→ IMDC confirms absent and archives/detaches sidecar
→ SWOF queued writer later creates directory again
→ vanilla save X reappears without active IMDC sidecar
```

Normal UI read/list flows usually force SWOF to drain pending writes before the player reaches deletion, so this was not elevated to an ordinary UI bug.

**Classification:** conditional direct/programmatic interoperability gap.

## Out of scope / not an issue: hypothetical later unknown transpiler rewriting the final IL

SWOF reports healthy after its transpiler successfully rewrites all five expected vanilla save callers.

A hypothetical later third-party transpiler could still remove/bypass SWOF's queue call after that health flag was set.

IMDC would continue trusting the health flag and might skip its own detached stable snapshot.

No supplied bundled mod was found doing this.

Under this audit's evidence policy, this is not actionable: it depends on an unspecified future third-party transpiler that is absent from the supplied mod set. IMDC is not expected to defend against an unlimited set of hypothetical future IL rewrites.

**Classification:** **not an issue for this audit; theoretical third-party scenario only.**

## 5. Low structural: IMDC save/load transpilers are intentionally strict

IMDC expects exact shapes such as exactly one matching `DataSaver.saveData<SaveManager.SavedData>` call in each target.

A very invasive third-party transpiler that runs earlier can cause IMDC patch installation to fail loudly rather than silently degrade.

For persistence-sensitive code, fail-fast is arguably the safer tradeoff.

## 6. Low: money-source stack tracing cannot see a previous Prefix that already returned

IMDC can classify a direct external mod call to `resources.Add()` because the external assembly remains on the call stack.

But if another mod Prefix modifies a vanilla method's transaction arguments and returns before vanilla later calls `resources.Add()`, the external Prefix no longer appears in the stack trace. The transaction may therefore be classified as vanilla even though another mod altered it.

No supplied Cosmo mod was found creating a concrete harmful case.

**Classification:** provenance limitation, not a confirmed bundled-mod bug.

## Important false positive ruled out: UIF clinic `CancelJob`

Initially suspected because UIF can veto `CancelJob()` while IMDC has a room-work cancellation Postfix.

However UIF runs at `Priority.First` and skips the original before IMDC's later stateful Prefix can create the cancellation snapshot. The IMDC Postfix receives null/default state, and `CaptureRoomWorkCancelled(null)` emits nothing.

**Not a bug.**

## Known interoperability areas that checked out well

- Save Write Ordering Fix normal save chain
- UIF blocked releases
- UIF show/concert cast preservation
- No Bullying Policy integration
- Assistant Manager room production
- Room Assignment Fix checked overlap
- Graduation Rebalances / Show Cast Assignment Fix around settled show state
- Divorce Fix checked marriage outcomes
- Graduation Details persistence bridge

---

# Consolidated Confirmed Issues

The original audit lettering is retained for stable cross-reference. **C, V, X, and Z are intentionally absent from the confirmed-issue set after policy review:** C is unsupported backward compatibility, V is absence of an arbitrary retention cap, X is an unsupported hypothetical third-party scenario, and Z is stale/untracked build output rather than a `cc924b6` source defect.

## Critical/High impact within IMDC history

### A. Staff severance stale ambient context

**Type:** capture correctness / persistent ledger corruption

**Effect:** next unrelated transaction can be mislabeled as severance and inherit wrong staff metadata.

**Normal vanilla path:** yes.

### B. Agency room identity is not durable across load

**Type:** historical reconstruction / identity model

**Effect:** all loaded rooms can collapse to ID `0`, and new post-restart rooms can reuse historical IDs.

**Normal vanilla path:** yes.

---

## Moderate issues

### D. UIF temporary single-preservation creates false cast changes

**Type:** mod interoperability / missing pre-state

### E. UIF medical vetoes still produce medical history

**Type:** mod interoperability / missing postcondition validation

### F. Assistant Manager loan replacement causes malformed before/delta fields

**Type:** mod interoperability / late snapshot

### G. Theater IDs are recyclable

**Type:** historical entity collision

### H. Cafe IDs are recyclable

**Type:** historical entity collision

### I. Backup healing can preserve `.bak` but delete the journal that completes it

**Type:** crash recovery redundancy

### J. Empty/torn preferred journal can mask valid `.bak.imdc.journal`

**Type:** crash recovery journal selection

### K. Static `Shows.CancelShow()` does not produce `show_cancelled`

**Type:** missing lifecycle capture

### L. Show canonicalization can fail at the 256-event flush boundary

**Type:** session consistency / duplicate history

### M. Final-scandal audition no-op can emit `audition_started`

**Type:** false lifecycle event

### N. Duplicate/no-op random-event scheduling can emit duplicate `random_event_started`

**Type:** false lifecycle event

---

## Low / semantic / robustness issues

### O. `*_status_changed` streams are not exhaustive

Direct vanilla status assignments bypass the setter hooks.

### P. `loan.PayOff()` programmatic no-op can emit `loan_paid_off`

Normal UI blocks it.

### Q. Duplicate `data_girls.Hire()` can emit false `idol_hired`

Normal UI generally prevents it.

### R. Invalid `Rivals.UpdateTrends()` can emit false trend update

Normal UI guards it.

### S. Invalid/non-contained `agency.DestroyRoom()` can emit false room destruction

Normal UI supplies a real room.

### T. Event Catalog advertises three built-in timeline types that retention drops before query

- `idol_status_changed`
- `research_points_accrued`
- `idol_earnings_recorded`

### U. Crash-orphaned `.imdc.tmp.*` files are never scavenged later

### W. SWOF deletion race can theoretically resurrect a vanilla save after IMDC archives its sidecar

Conditional/direct/racy.

### Y. Stack-trace money attribution cannot identify a previous external Prefix that already returned

Provenance limitation.

---

# Policy / Non-Issue Notes

The following observations remain documented for context but are **not bugs to fix** under the audit's requirements:

- **Older sidecar compatibility:** IMDC 3.4.6 intentionally supports sidecar format 5 only. Backward compatibility with 3.4.5/v4 is not required.
- **Unbounded retained data/history:** absence of an arbitrary forward-retention, JSON nesting/depth, or similar mod-data cap is not inherently defective. A concrete supported-use failure is required before it becomes an issue.
- **Unknown future transpilers:** unsupported hypothetical Harmony rewrites that are not present in the supplied code/mod set are out of scope.
- **Generated DLL revision metadata:** the stale DLL in the supplied working-tree ZIP is not tracked at `cc924b6`; project/mod version metadata is 3.4.6, `*.dll` is already ignored, and rebuilding from the desired checkout regenerates the assembly metadata.

---

# Important False Positives Explicitly Ruled Out

The following were investigated and should **not** be reported as bugs without new evidence:

## Load restoration timing before `LoadEvent`

Correct. Vanilla mutates `PlayerData.LastSave` during `LoadEvent`, so IMDC must restore/fingerprint before that mutation.

## Policy selection confirmation popup

Correct. IMDC patches `_Select`, the actual commit, not the public wrapper.

## Dating stage callback timing

Correct. `Date_Popup.End(callback)` invokes its callback synchronously before return.

## Reload synthesizing policy/research history

Correctly suppressed by `saveLoadPreparationActive`.

## Ordinary room-work cancellation/completion logic

Generally matches vanilla completion/cancel semantics.

## UIF clinic `CancelJob`

No false cancellation event because IMDC receives no valid cancellation snapshot.

## UIF blocked single/show/concert releases

UIF deliberately throws/swallow-finalizes to bypass IMDC release postfixes rather than merely returning false.

## UIF show/concert temporary cast preservation

Explicit Harmony ordering lets IMDC snapshot the real before-state, then compare to the unchanged after-state.

## No Bullying Policy checked overlap

IMDC requires an actual bullied-state change.

## Assistant Manager room production

IMDC intentionally snapshots at `Priority.First` and handles the replacement Prefix.

## Canonical show payload `ShowId` omission

The field is not serialized; entity identity carries the show ID.

## Branch rewind EventId reuse

No reuse. Active history rewinds, but the global issued sequence watermark remains high.

## Branch rewind idempotency leakage

No stale-future idempotency keys survive the rewind.

## Shared-event per-idol fan-out

Intentional and coherent for the checked participant schemas.

## Timeline pagination by EventId

The API resolves the row and pages by `(GameDateKey, Sequence)`, not raw numeric EventId ordering.

## Money ledger arithmetic/materialization

No second arithmetic reconstruction bug found beyond earlier bad source attribution at capture time.

## Normal SWOF + IMDC save-write chain

The supplied versions coordinate deliberately and the SWOF health signal is only set after all five expected save callers are rewritten.

---

# Architectural Strengths Confirmed During the Audit

It is worth recording what **did not** fail, because many apparent issues were ruled out only after tracing the implementation deeply.

## Persistence strengths

- exact vanilla-save fingerprinting in v5 checkpoints
- conservative exact-checkpoint activation instead of "newest wins"
- first-time adoption of vanilla saves without sidecars
- ability to survive out-of-order completion of vanilla asynchronous saves
- v2 journal transaction framing
- invisible incomplete transactions
- compact-base SHA-256 journal pairing
- replay suffix validation
- idempotency reconstruction
- branch rewind reconstruction
- background compaction same-path locking
- topology/archive epoch protection

## Query/reconstruction strengths

- high-watermark EventId discipline across rewind
- active-branch-only custom-state reconstruction
- correct reactivation of idempotency after rewind
- per-idol shared-event projection
- timeline pagination by resolved row position
- money transaction balance reconstruction

## Interoperability strengths

- deliberate Save Write Ordering Fix ordering
- deliberately late canonical show observation after known show/cast mods
- deliberately early room-production snapshots for Assistant Manager
- explicit UIF ordering for show/concert cast preservation
- UIF release blockers specifically designed to avoid false IMDC release postfixes

---

# Recommended Fix Themes

Many of the confirmed bugs reduce to a few reusable design rules.

## 1. Never treat a missing Harmony `__state` as a legitimate default historical "before" state

A prior mod Prefix may legally return `false` and skip later Prefixes while Postfixes still run.

Missing `__state` should generally mean:

```text
before-state unknown → do not infer a transition
```

This would harden:

- UIF single cast preservation
- UIF medical vetoes
- Assistant Manager loans
- future replacement/veto mods

## 2. Validate real postconditions before emitting lifecycle events

Examples:

- medical injury only if final status is actually injured
- loan payoff only if active before and inactive after
- show cancellation based on actual removal / cancellation semantics rather than a status vanilla never sets
- audition start only if the audition really began
- random-event start only if active-event collection actually gained the intended event

## 3. Do not promote ephemeral/recyclable vanilla IDs into permanent historical identity without a generation layer

This applies strongly to:

- agency rooms
- theaters
- cafes

An IMDC-owned persistent generation identity is safer than raw vanilla runtime/list IDs.

## 4. Track recovery provenance precisely

A boolean like `recoveredFromBackup` is not enough if recovery may involve different physical journal files.

Recovery should remember:

- which base was used
- which journal was used
- whether the journal header actually matched the base

Then healing can preserve a truly complete known-good backup generation.

## 5. Keep ordinary and canonical show records in the same pending compaction window

While post-mod settlement is active, defer flushes that would separate the ordinary row from the later canonical row.

## 6. Align public event documentation with retention policy

If an event type is intentionally transient/internal and cannot appear in the public timeline, mark it that way or remove it from the timeline event catalog.

## 7. Add startup scavenging for abandoned temp files

Clean stale:

```text
*.imdc.tmp.*
```

files that are not canonical targets and are older than a conservative threshold.

---

# Final Audit Position After Passes 1–6

IMDataCore 3.4.6 is **not fundamentally broken**. Its normal persistence mechanics are stronger than vanilla's raw asynchronous save writes, and several difficult areas such as exact checkpoint identity, rewind reconstruction, transaction replay, idempotency, and cross-mod show settlement are thoughtfully designed.

The defects found are concentrated in four categories:

1. **capture hooks that assume a mutation happened when vanilla or another mod may have skipped it**
2. **historical identity keys that vanilla does not preserve uniquely**
3. **narrow crash-recovery edge cases around backup generations**
4. **public API/documentation inconsistencies where advertised behavior does not match retained/queryable history**

The most urgent correctness fixes are the severance attribution hook, durable agency-room identity, and the Harmony-veto/missing-`__state` class of bugs exposed by Unavailable Idols Fix and Assistant Manager.

