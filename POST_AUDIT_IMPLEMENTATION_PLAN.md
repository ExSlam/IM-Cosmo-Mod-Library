# Post-Audit Implementation and Regression Plan

> **SNLF shutdown follow-up (2026-09-22):** SNLF 0.55.0 adds an attempt-completion barrier for Save and Exit/Main Menu, tracked third-party sidecar hooks, and isolated mod-owned payload schemas. Success, failure and cancellation are terminal outcomes. See the [integration contract](mods/Save%20n%20Load%20Fixes/docs/MOD_PERSISTENCE_API.md) and [native qualification](mods/Save%20n%20Load%20Fixes/docs/SHUTDOWN_QUALIFICATION_2026_09_22.md). This follow-up does not alter the frozen audit counts or close unrelated IMDC regression obligations.

> **Automated testing follow-up (2026-09-21):** native storage, crash/restart, repair ownership, public-read and Save & Quit suites now have an [automated runner and evidence record](mods/IM%20Data%20Core/docs/V6V3_AUTOMATED_QUALIFICATION_2026_09_21.md). These persistence tests require no visual verification. The record distinguishes passing bounded fixtures from the still-open full audit matrix.

> **Implementation follow-up (2026-09-09):** the v6/v3 loader enforces the no-migration policy and protects unsupported forward-envelope schemas from backup downgrade. Actual Unity tests also found and fixed omitted collections in Graduation Details persistence and IMDC random-event payloads. Native consumer/save/load evidence and remaining qualification work are tracked in [`V6V3_QUALIFICATION_STATUS.md`](mods/IM%20Data%20Core/docs/V6V3_QUALIFICATION_STATUS.md). Frozen audit counts remain unchanged. Historical task checkpoints below are not current release-status assertions.


> **Current storage compatibility policy (3.4.34+):** IM Data Core supports only **sidecar v6 + journal v3**. Sidecar v1-v5 and journal v1-v2 are unsupported release inputs. IMDC does **not** promise, qualify, or require in-place migration, conversion, adoption, or rewrite from those older storage generations. Encountering an unsupported older generation must fail closed without converting it or overwriting its bytes. Any v5/v2 migration language retained below is historical design/task context, not a current compatibility commitment.

**Current product-scope override (2026-09-07):** frozen audit finding #56 is intentionally retired from the release obligation. IMDataCore will support only sidecar v6 / journal v3 and will not ship v1-v5/v1-v2 migration. Historical ledger counts remain frozen for audit traceability, but the active release scope is **58 numbered IMDataCore obligations + 5 durable-identity contracts**.

Source basis: reconciled `README_IMDataCore_Persistence_Audit.md`, Cosmo Mod Library commit `98e8e56c86513c2ec3f05b1f9b5be7d7cd2f7312`, supplied decompiled Idol Manager source, and current Save Write Ordering Fix 1.3.0 source.

This document is an **implementation plan**, not a new source-audit pass. Source-proven defects and contracts remain exactly as counted in the audit. New architecture choices below are labeled as planning decisions and do not create new findings unless later source/runtime evidence proves a new defect.

## 1. Frozen source-audit ledger

The implementation baseline is frozen at:

- **68 numbered findings**;
- **59 IMDataCore responsibilities** among those 68;
- **14 numbered Save n Load Fixes responsibilities**;
- **6 split-responsibility findings**: #1, #2, #5, #6, #30, #31;
- **5 separate IMDataCore durable-identity defects**;
- **32 additional non-numbered Save n Load Fixes repair families**;
- **1 ordered save/read transport defect family**, tracked separately from the 32 additional repairs;
- **1 deliberate deterministic-replay limitation** (`UnityEngine.Random.state`) outside both core backlogs;
- **154 IMDataCore regression contracts**; and
- **47 Save n Load Fixes regression contracts**.

No implementation wave may silently change those counts. If coding uncovers genuinely new source behavior, reopen the audit ledger explicitly rather than smuggling the change into a work package.

### 2026-09-01 explicit ledger reopen: SNLF-A33

The post-freeze wide-numeric audit proved a new vanilla repair family covering scalable
`Int32` intermediates/storage, lossy `Int64` -> `Single` -> `Int32` paths, unchecked
`Int64` arithmetic, and late widening after an `Int32` expression. This is explicitly
tracked as **SNLF-A33 — Wide numeric continuity and overflow repair**; accepting it makes
the additional-repair backlog 33 families. Version 0.54.0 now implements A33.1-A33.6,
including exact wide persistence and consumer/allocator closure. Its source, contract,
arithmetic, and isolated envelope/Harmony runtime gates pass; the live Unity checkpoint
and mod-combination matrix remains pending. A33 gates remain named separately from the
frozen 1-47 count until release qualification and a deliberate ledger merge. The complete
evidence, persistence contract, mod-aware theater-ticket rule,
FixSaveFile implications, implementation segments, and version policy are in
[`mods/Save n Load Fixes/A33_WIDE_NUMERIC_AUDIT_AND_PLAN.md`](mods/Save%20n%20Load%20Fixes/A33_WIDE_NUMERIC_AUDIT_AND_PLAN.md).

## 2. Target deliverables

### D1. Save n Load Fixes

Create a new standalone Cosmo mod. No Save n Load Fixes project exists in the supplied tree today.

**Proposed identity:**

- folder: `mods/Save n Load Fixes`
- assembly: `com.cosmo.savenloadfixes`
- Harmony ID: `com.cosmo.savenloadfixes`
- root namespace: `SaveNLoadFixes`
- development version: `0.1.0`
- first complete release after all SNLF regression gates: `1.0.0`

The mod must:

1. implement all 14 numbered vanilla repair obligations;
2. implement all 32 additional confirmed vanilla repair families;
3. own all supplemental continuation state it introduces;
4. contain its own Mono-safe ordered `SavedData` and `GlobalData` transport;
5. have **no runtime dependency on standalone Save Write Ordering Fix**;
6. never use IMDataCore shared history as vanilla restore authority; and
7. remain useful when IMDataCore is absent.

### D2. Standalone Save Write Ordering Fix

Update the current **1.3.0** project as a separately shippable transport-only mod.

**Proposed next version:** `1.4.0`.

It must:

1. retain the current no-constructed-generic-Harmony-patch rule;
2. retain ordered career `SavedData` writes and coordinated career reads;
3. add the source-proven `GlobalData` sibling write/read coverage;
4. add Save n Load Fixes delegation/coexistence;
5. preserve the existing public API signatures for old consumers; and
6. add explicit health/owner diagnostics for the new two-provider world.

### D3. IMDataCore

Implement the 58 active numbered backend/history obligations plus the five durable-identity repair contracts. Frozen finding #56 is retained as historical audit evidence but deliberately retired from the current product scope by the no-backwards-compatibility policy. The only planned forward persistence generation remains:

- **sidecar v6**;
- **journal v3**.

IMDataCore must not absorb Save n Load Fixes continuation state merely because it already has a sidecar.

### D4. Integrated regression closure

Implementation is complete only after:

- IMDataCore regressions **1-154** pass or have source-equivalent automated coverage;
- Save n Load Fixes regressions **1-47** pass or have source-equivalent automated coverage;
- transport/coexistence tests in this plan pass; and
- all four install permutations are exercised: neither transport mod, SWOF only, SNLF only, both.

---

# 3. Save n Load Fixes architecture

## 3.1 Project layout

Recommended initial tree:

```text
mods/Save n Load Fixes/
  Save n Load Fixes.csproj
  README.md
  CHANGELOG.md
  assets/
    info.json
  docs/
    TEST_MATRIX.md
    REPAIR_STATE_SCHEMA.md
    TRANSPORT_COEXISTENCE.md
  src/
    Core/
      SnlfConstants.cs
      SnlfDiagnostics.cs
      SnlfPatchHealth.cs
    Transport/
      SaveTransportApi.cs
      SaveTransportProviderDiscovery.cs
      OrderedIoCoordinator.cs
      SavedDataTransportPatches.cs
      GlobalDataTransportPatches.cs
      LegacySwofInteropPatches.cs
    Persistence/
      RepairEnvelopeV1.cs
      RepairEnvelopeCodec.cs
      RepairSnapshotBuilder.cs
      RepairLoadContext.cs
      RepairRestoreCoordinator.cs
    Safety/
      CheckpointGate.cs
      LoadEpoch.cs
      DeferredWorkGuards.cs
    Repairs/
      RelationshipsRepair.cs
      EventManagerRepair.cs
      SubstoriesRepair.cs
      AgencyRoomRepair.cs
      ShowsRepair.cs
      ActivitiesRepair.cs
      TutorialRepair.cs
      AwardsRepair.cs
      LegacyMigrationRepair.cs
      PortraitIdentityRepair.cs
      GlobalDataRepair.cs
      DateTimeRepair.cs
      MiscRepair.cs
```

Use the repository's existing `Directory.Build.props` (`net46`, explicit compile items, Assembly-CSharp/0Harmony/Unity references). Do not add a constructed `DataSaver<T>` Harmony patch.

## 3.2 One persistence owner

The source-audit ownership theorem becomes an implementation rule:

- **vanilla or Save n Load Fixes owns current gameplay continuation state**;
- IMDataCore may record a distinct historical occurrence, but may not restore the current value from history;
- technical repair values persisted by SNLF remain SNLF-owned even if a future optional adapter stores them through another service;
- a missing SNLF repair record must never be filled from IMDataCore history unless a dedicated, audited migration contract explicitly proves equivalence.

## 3.3 Preferred repair-envelope design

### Source-established requirement

Several SNLF repairs need state that vanilla does not serialize, while others can be repaired from existing vanilla DTOs. The stateful repairs need exact association with the physical `SavedData` request. The embedded ordered transport already freezes the exact save request on the caller thread.

### Post-audit design decision

Use a **versioned SNLF repair envelope embedded as an additional root object in the same frozen `SavedData` JSON**.

Proposed root key:

```text
__cosmo_save_n_load_fixes
```

Proposed envelope header:

```text
format_name: cosmo-save-n-load-fixes
format_version: 1
checkpoint_id: <opaque GUID>
mod_version: <SNLF version>
game_date: <display/diagnostic only>
records: <typed repair sections>
```

Why this is preferred:

1. the vanilla DTO and repair state share **one physical file commit**;
2. two saves with otherwise byte-identical vanilla state still have distinct SNLF checkpoint IDs;
3. Save As naturally carries the correct repair state;
4. no cross-file commit/pointer race exists;
5. F9/restart reads exactly the repair state written with that file; and
6. SNLF does not depend on IMDataCore or standalone SWOF.

### Mandatory Phase-0 compatibility spike

**Current implementation status:** a runtime-only `RepairEnvelopeUnknownRootProbe.cs` fixture now exists in the Sprint-1B SNLF test tree, but it has **not** been executed here because this environment lacks Idol Manager's Unity/Mono runtime and private DLL set. The production writer therefore still does not inject the envelope.

Before committing this format, prove on the actual Idol Manager Mono/Unity runtime that:

1. a save containing an unknown top-level `__cosmo_save_n_load_fixes` object loads normally through vanilla `JsonUtility.FromJson<SaveManager.SavedData>` when SNLF is absent;
2. `SaveManager.FixSaveFile(...)` preserves the unknown root when it parses/writes the file through `SimpleJSON`;
3. manual, autosave, chapter, story overwrite, and story new-save flows all tolerate the root;
4. save-list readers tolerate the root; and
5. removing SNLF does not make an existing SNLF save unloadable.

With SNLF present, the coordinated reader should read raw JSON, extract the SNLF root, remove it from the vanilla JSON tree/string, and deserialize the vanilla portion into `SaveManager.SavedData`. This avoids relying on unknown-field behavior for SNLF's own read path.

If the no-mod compatibility spike fails, stop and redesign before implementing stateful repairs. Do not fall back to a loosely paired sidecar without an exact transaction design.

## 3.4 Repair-envelope shape

Prefer strongly typed, sparse sections rather than one unbounded key/value dumping ground. The initial schema should carry only the repair families that actually need supplemental state.

Recommended V1 sections:

- relationship dynamics keyed by unordered idol pair;
- selected random-event business proposal locator/witness;
- queued substory `BeforeStart` semantic descriptors;
- active room `substoryScene` dialogue identity;
- show last-episode `FanAppeal`;
- `Activities.Chain` ordered future intent;
- `Event_Overlord.Latest_Event` exact game date;
- room `girl_paused` and `business_minutes_before_finish`;
- tutorial performance/promotion nullable baselines;
- delayed ambient scene semantic job;
- private `_progressable.counter` entries;
- pending graduation-successor introductions;
- temporary auto-task ban entries with remaining scaled-time delay;
- delayed tutorial continuation kind/delay;
- recent-activity recency clock data and `Activities.LastHeal`;
- `Substories_Manager.PreviousNewSubstory`;
- pushed-slot previous occupant/baseline needed by the audited contract;
- award-eve `TempNominations` slate;
- `singles.FanAppeal_LastSingle` seven-axis vector plus source single ID;
- unresolved external portrait `(sprite type, asset_id)` identities for idol/staff.

Do **not** put the following into the envelope merely because SNLF can:

- `Training.Progress_Init` when it is exactly derivable;
- `LastGirlID` when it can be restored around load;
- `BankruptcyDanger` when it can be reconstructed without rewriting the deadline;
- unfinished concert `FinishDate` when the saved DTO already has the exact value;
- trivia zero counters already present in vanilla DTOs;
- group target-audience fields already present in the DTO shape but omitted only by the save-side copier;
- the `Event_Templates` selected-to-popup gap, birthday between-popup gap, SSK paid-launch gap, or semantic post-dialogue final-frame gap, which are checkpoint-blocker problems rather than persistence problems;
- stale tutorial active ID;
- award `her_choice` occurrence cache after the speech is finished;
- any global Unity RNG state.

## 3.5 Save pipeline

For every concrete `SavedData` write caller:

1. let normal caller prefixes/transpilers and vanilla `SaveEvent` complete;
2. enforce `CheckpointGate` before admitting the write;
3. capture a detached `RepairEnvelopeV1` from authoritative current runtime state;
4. serialize the vanilla `SavedData` immediately on the caller thread;
5. inject the already-frozen repair envelope into the root JSON;
6. enqueue the final bytes in the per-physical-path FIFO;
7. return control to vanilla caller while the ordered writer handles physical I/O.

**No writer-thread fallback may serialize a live repair object.** If SNLF cannot freeze a repair-dependent save exactly, fail that ordered request visibly instead of producing a checkpoint whose vanilla DTO and repair state were sampled at different times.

## 3.6 Load pipeline

At in-game load/F9:

1. increment `LoadEpoch` **before target DTO replacement**;
2. clear transient repair registries and stale object-reference maps;
3. wait for any pending ordered write to the selected physical path;
4. read the raw save bytes;
5. extract/validate `RepairEnvelopeV1` when present;
6. deserialize the vanilla portion into `SaveManager.SavedData`;
7. stage the envelope in `RepairLoadContext` keyed to the current load epoch;
8. let vanilla load subscribers reconstruct objects;
9. restore each repair at its source-correct load seam, using stable IDs/locators rather than pre-load CLR references;
10. execute late rebinding only after prerequisites exist; and
11. clear the load context after terminal load completion/finalizer.

Envelope absent means **pre-SNLF/legacy save**, not corruption. Use only the deterministic compatibility fallbacks already approved by the audit.

Envelope present but structurally invalid must never be treated as an empty exact repair state. Log a targeted error and fail closed for the affected repair family.

## 3.7 Checkpoint gate

Create one central gate with named blocker tokens. It must affect:

- autosave eligibility;
- F5/manual `SaveData(false, true)`;
- in-game F9/load; and
- any SNLF-owned explicit save helper added later.

Initial blocker families:

- `EventTemplatesPendingOpen` (#6);
- `SemanticCallbackFinalFrame` (Area #2 continuation boundary);
- `BirthdayQueueBetweenPopups`;
- `SskPostPaymentLaunch`;
- any finite Area-#2 source-verified unsafe transition already represented by regression #35.

Do not use broad object-presence proxies such as `Active_Template != null` when the audit rejected them.

First implementation can **block** unsafe manual save/load with a clear diagnostic. Deferred replay of the user's save command is a later usability enhancement, not required for semantic correctness.

## 3.8 Load epoch

`LoadEpoch` is a monotonic process-local generation, incremented before same-process target load/new-game reconstruction. Every guarded delayed callback/coroutine captures the epoch at scheduling time and exits before mutation when the captured epoch no longer matches.

Apply it only to the finite audited authoritative carriers. Do not call global `StopAllCoroutines()`.

---

# 4. Embedded ordered transport in Save n Load Fixes

## 4.1 Source-established transport surface

Current standalone SWOF 1.3.0 proves the Mono-safe shape:

- do not Harmony-patch constructed `DataSaver<T>` methods;
- replace concrete caller instructions instead;
- freeze `SavedData` JSON synchronously;
- FIFO-order writes per physical path;
- coordinate reads against pending writes;
- expose file/directory exclusivity for cooperating mods.

The supplied source has five concrete `SavedData` write callers and seven reader methods containing the known direct `SavedData` load call sites. The audit separately proves one `SaveManager.SaveGlobalData()` write and one `SaveManager.LoadGlobalData()` read remain outside SWOF 1.3.0.

**Exact current 1.3.0 caller manifest to preserve in source tests:**

- writes: `SaveManager.SaveData(bool,bool)`, `SaveManager.SaveChapter(tasks._chapter)`, `Popup_Save.Save()`, `Popup_Load_Story.Do_Overwrite_Save(...)`, `Popup_Load_Story.Do_New_Save(string)`;
- reader methods: `SaveManager.GetLatestAutosavePath()`, both `SaveManager.LoadData(...)` overloads, `Popup_Load_Story.Get_Playthrough_Info(string)`, `Popup_Load_Story.Get_Saves(...)`, `Popup_Save._save_data.Set(string)`, and `Popup_Save._save_data.SetAutosave()`;
- global sibling: `SaveManager.SaveGlobalData()` contains the one `DataSaver.saveData<SaveManager.GlobalData>(...)` call and `SaveManager.LoadGlobalData()` contains the one corresponding `loadData` call.

Do not encode only aggregate counts in the tests. Assert the exact method manifest so a future new caller fails loudly instead of leaving transport coverage silently partial.

## 4.2 SNLF effective-owner rule

When Save n Load Fixes is installed, **SNLF owns effective transport** even if standalone SWOF is also installed.

Reasons:

- SNLF must remain standalone;
- SNLF must inject/extract the repair envelope at the exact `SavedData` transport boundary;
- two independent per-path queues cannot coordinate correctly; and
- IMDataCore directory deletion/exclusive-file clients must see the queue that actually owns the write.

## 4.3 Harmony ordering

**Implemented Sprint-1B initial rule:** SNLF's four transport transpiler classes use `HarmonyPriority(Priority.Last)` and target only the original closed vanilla `DataSaver.saveData<T>` / `loadData<T>` calls at the audited concrete callers. They intentionally carry **no `HarmonyAfter`/`HarmonyBefore` dependency on old SWOF or old IMDataCore**.

This is a development-stage isolation rule, not the final coexistence topology. Save Write Ordering Fix and IMDataCore will be updated later against SNLF's provider contract. At that later point, define ordering/delegation between the **updated** assemblies so exactly one effective writer/reader owner rewrites each concrete call site. Do not add old-call-shape recognition to the new SNLF transport core merely to bridge obsolete releases.

Graduation Details currently uses `Priority.First` on its caller-level save preparation path, so SNLF's `Priority.Last` transport substitution naturally occurs after that preparation without adding an old-provider compatibility dependency.

## 4.4 SNLF transport API

Expose a small public API so IMDataCore and updated SWOF can cooperate without a compile-time dependency:

```text
SaveNLoadFixes.SaveTransportApi.Version
SaveNLoadFixes.SaveTransportApi.SavedDataInterceptionHealthy
SaveNLoadFixes.SaveTransportApi.GlobalDataInterceptionHealthy
SaveNLoadFixes.SaveTransportApi.IsAuthoritativeTransport
SaveNLoadFixes.SaveTransportApi.TransportOwner
SaveNLoadFixes.SaveTransportApi.HasPendingWrites(path)
SaveNLoadFixes.SaveTransportApi.TryWaitForPendingWrites(...)
SaveNLoadFixes.SaveTransportApi.TryRunExclusiveFileAccess(...)
SaveNLoadFixes.SaveTransportApi.TryAcquireExclusiveDirectoryAccess(...)
```

Keep signatures parallel to SWOF where practical so reflection adapters stay tiny.

## 4.5 GlobalData in embedded transport

Patch only the concrete methods:

- `SaveManager.SaveGlobalData()`;
- `SaveManager.LoadGlobalData()`.

Freeze `SaveManager.GlobalData` immediately after `SaveGlobalDataEvent` has populated `Settings`, `Log`, `Events_Log`, `Seen_CGs`, and `Transferred_CGs`. Queue `global_data.json` through the same coordinator class, but do not attach career repair-envelope state to it.

## 4.6 Legacy compatibility is deliberately deferred

Do **not** implement compatibility adapters for older Save Write Ordering Fix or older IMDataCore releases during the initial SNLF transport work. Both companion mods will be updated later against SNLF's current provider API.

The initial SNLF transport therefore has no compile-time or reflection dependency on old SWOF/IMDataCore APIs and does not recognize old SWOF replacement call shapes. If a legacy coexistence bridge is ever needed for release migration, design it much later from the then-current release matrix rather than freezing obsolete behavior into the new transport core.

---

# 5. Save n Load Fixes work-item matrix

`Persist` means the exact repair needs an SNLF-owned record in the repair envelope. `Gate/Epoch` means the fix relies on checkpoint safety or load-generation invalidation rather than durable state.

## 5.1 Numbered repair findings: 14

| Work ID | Audit item | Primary implementation | Persist? | Primary source seam | Regression |
|---|---|---|---|---|---|
| `SNLF-N01` | #1 relationship initial `Dynamic` | snapshot unordered idol-pair -> dynamic; restore after relationship reconstruction | **Yes** | `Relationships._relationship`, relationship save/load reconstruction | 1 |
| `SNLF-N02` | #2 exact event-selected business contract | persist active-event -> proposal locator + structural witness; rebind only on exact match | **Yes** | `Event_Manager.VARIABLE__BIZ_PROPOSAL`, `business.ActiveProposals` | 2 |
| `SNLF-N03` | #3 queued substory `BeforeStart` semantics | semantic descriptor registry for the 13 verified call sites; rebuild fresh callback | **Yes** | `Substories_Manager.StartDialogue` queue save/load | 3 |
| `SNLF-N04` | #4 room `substoryScene` identity | persist dialogue identity against structural room locator; late rebind | **Yes** | `agency._room`, room save/load | 4 |
| `SNLF-N05` | #5 show last-episode `FanAppeal` | persist exact fan-appeal vector by show ID | **Yes** | `Shows._show.FanAppeal`, show save/load | 5 |
| `SNLF-N06` | #6 pending `Event_Templates` handoff | named checkpoint blocker from successful selection until popup open | **Gate** | `Event_Templates` selection / `_OpenPopup` | 6 |
| `SNLF-N07` | #7 `Activities.Chain` | persist ordered enum list; invalidate discarded chain iterator | **Yes + Epoch** | `Activities.Chain`, `Chain_Progress_Do` | 7 |
| `SNLF-N08` | #8 `Event_Overlord.Latest_Event` | persist exact game date; restore after vanilla reset; deterministic target-date legacy fallback | **Yes** | `Event_Overlord.LoadFunction/Reset` | 8 |
| `SNLF-N09` | #9 room `girl_paused` | persist displaced idol ID by room locator; rebind loaded idol | **Yes** | `agency._room.Pause/ResumeTraining` | 9 |
| `SNLF-N10` | #10 `business_minutes_before_finish` | persist exact remaining minutes by room locator | **Yes** | `agency._room.PauseBusinessProposal` | 10 |
| `SNLF-N11` | #12 tutorial baselines | persist nullable performance/promotion baselines; restore after reset | **Yes** | `Tutorial_Reqs` / tutorial load | 11 |
| `SNLF-N12` | #13 delayed ambient scene assignment | persist semantic job, due game time, idol IDs, room locator; epoch-guard stale coroutine | **Yes + Epoch** | `Scenes`/room assignment coroutine | 12 |
| `SNLF-N13` | #30 `Event_Manager` nonstandard terminal state | set `state=complete` on resultless completion before IMDC observes; handle SNS-only terminal path | No | `Event_Manager.ConcludeEvent`, SNS auto-delivery | 27 |
| `SNLF-N14` | #31 forced idol-idol breakup inconsistency | resolve actual partner relationship and invoke coherent breakup semantics | No | `Date_Popup.OnClick_ForceBreakup`, `Relationships._relationship.BreakUp` | covered by split/integration suite |

## 5.2 Additional confirmed repairs: 32

| Work ID | Repair family | Primary implementation | Persist? | Primary seam | Regression |
|---|---|---|---|---|---|
| `SNLF-A01` | Training `Progress_Init` | deterministic reconstruction from saved progress/start/completion/target-save time | No | room training load | 15 |
| `SNLF-A02` | private `_progressable.counter` | sparse owner-kind + stable owner ID + parameter enum record | **Yes** | singles/show/concert/tour/SSK progressables | 13 |
| `SNLF-A03` | risky single `Marketing_Result == 0f` | remove zero-as-uninitialized load sentinel behavior | No | single load | 14 |
| `SNLF-A04` | unsafe F5/manual save boundary | central `CheckpointGate` at `SaveManager.SaveData`; autosave gate shares blockers | Gate | `SaveManager.Update`, `SaveData`, `CanAutosave` | 16, 35 |
| `SNLF-A05` | `data_girls.LastGirlID` load inflation | capture saved allocator and restore after reconstruction | No | `data_girls.LoadFunction` | 17 |
| `SNLF-A06` | `loans.BankruptcyDanger` reset/deadline extension | reconstruct bool from saved/current money while preserving loaded date; never call setter | No | `loans.LoadFunction` | 18 |
| `SNLF-A07` | unfinished concert `FinishDate` overwrite | preserve/reapply saved date around load/initiation | No | `SEvent_Concerts.LoadFunction`, `_concert.Initiate` | 19 |
| `SNLF-A08` | F9 stale coroutine/deferred-work leakage | process-local monotonic load epoch + finite guarded carriers | Epoch | `SaveManager.LoadData` + audited carriers | 28 |
| `SNLF-A09` | semantic post-dialogue/post-popup continuation | checkpoint blocker until synchronous callback completion + epoch invalidation | Gate + Epoch | `ActiveDialogueController` / `vn_actions` carriers | 29 |
| `SNLF-A10` | birthday queue / graduation-check loss | between-popup checkpoint blocker; bounded compatibility reconstruction from saved same-day witness | Gate | `Birthday.Queue`, `_MoveQueue` | 30 |
| `SNLF-A11` | successor introduction loss | ordered pending idol IDs + optional remaining presentation delay; late rebind | **Yes** | `Find_Successor`, `data_girls.new_girls`, `StartIntroductions` | 31 |
| `SNLF-A12` | SSK post-payment half-commit | paid-launch checkpoint blocker through results/popup commit; epoch invalidate | Gate + Epoch | `SEvent_SSK.StartSSK` | 32 |
| `SNLF-A13` | temporary `GirlsBannedFromAutoTasks` | idol ID + remaining scaled-time delay; duplicate ban does not extend | **Yes + Epoch** | ban helper / `CanAutoTrain` | 33 |
| `SNLF-A14` | delayed tutorial continuation | fixed continuation kind + optional remaining delay; idempotent target-state check | **Yes + Epoch** | tutorial iterators | 34 |
| `SNLF-A15` | recent-activity recency clock | persist bounded exact activity rows + `LastHeal` as audited | **Yes** | `Stats.AddActivity`, load reset | 37 |
| `SNLF-A16` | `PreviousNewSubstory` spacing anchor | exact game-date record; deterministic target-date legacy fallback | **Yes** | `Substories_Manager.Check_TooManyDates` | 38 |
| `SNLF-A17` | pushed-slot `GirlsLastDay` baseline | persist/restore exact prior slot occupant/baseline needed to preserve `Days` semantics | **Yes** | `Pushes`, girl-select path, `OnNewDay` | 43 |
| `SNLF-A18` | trivia zero-counter load skip | assign serialized zero as authoritative | No | `girls_trivia.LoadFunction`, `Graduation_Trivia.LoadFunction` | 44 |
| `SNLF-A19` | startup `GlobalData` delivery race | exactly-once late delivery after `staticVars` subscribes if data already loaded | No | `SaveManager.Awake/LoadGlobalData`, `staticVars.Awake/LoadSettings` | 45 |
| `SNLF-A20` | legacy idol birthday/peak-age rerandomization | deterministic migration per saved idol | No new record once vanilla fields are saved | legacy girl load | 20 |
| `SNLF-A21` | legacy aggregate-fan redistribution | deterministic stable redistribution preserving aggregate totals | No new record once converted | resources/fan legacy load | 21 |
| `SNLF-A22` | legacy relationship bootstrap rerandomization | deterministic old-save relationship/dynamic generation | No extra record beyond N01 after repaired save | relationship old-save bootstrap | 22 |
| `SNLF-A23` | legacy rival bootstrap rerandomization | deterministic old-save rival generation | No extra record once vanilla serializes result | rival legacy load | 23 |
| `SNLF-A24` | award `her_choice` repeated reroll | resolve once per speech occurrence; cache only for occurrence lifetime | No | award speech handlers / `_speech.GetThanks` | 24 |
| `SNLF-A25` | ignored immutable graduation `DateTime` adjustments | assign returned `DateTime`; use narrow transpilers where formula duplication would be risky | No | injury/depression/graduation/business/daily drift | 25 |
| `SNLF-A26` | show next-episode/cancellation helper | advance incoming date by one day before weekday loop | No | `_show.GetNextEpisodeDate` | 26 |
| `SNLF-A27` | stale `Tutorial.Active_Tutorial_ID` | clear/normalize lifecycle and gate business override on actual active tutorial | No | tutorial start/quit/reset/load; room business | 39 |
| `SNLF-A28` | `Awards.TempNominations` loss | persist minimal pending slate; late rebind idol/single IDs; clear after awards reset | **Yes** | award eve save/load/reset | 40 |
| `SNLF-A29` | `FanAppeal_LastSingle` loss/stale retention | persist seven ratios + source single ID; clear stale RAM before restore | **Yes** | `singles.ReleaseSingle`, save/load | 41 |
| `SNLF-A30` | best-single nomination missing single binding | bind nominated single once at temp nomination creation; reuse stored occurrence | No | `Awards.GenerateTempNominations` / `SetWins` | 42 |
| `SNLF-A31` | unresolved external portrait identity destroyed by fallback | keep original missing `(type, asset_id)` independently from temporary render fallback; re-resolve later | **Yes** | idol/staff texture load/save | 46 |
| `SNLF-A32` | group target-audience DTO round trip | write `Appeal_Gender/Hardcoreness/Age` in save-side copier | No | `Groups.GroupData.Set(_group)` | 47 |

## 5.3 Implementation order inside SNLF

Do **not** implement the table top-to-bottom. Use dependency waves.

### SNLF Wave 0 - skeleton and transport proof

Deliver:

- project/csproj/assets;
- patch-health diagnostics;
- ordered `SavedData` write/read transport;
- ordered `GlobalData` write/read transport;
- SNLF repair-envelope compatibility spike;
- basic direct/compatibility APIs;
- no semantic repair yet beyond source-only smoke patches.

Exit gate: every vanilla save/read caller is either intercepted exactly once or health is false with a named failure.

### SNLF Wave 1 - safety infrastructure

Implement:

- `CheckpointGate`;
- `LoadEpoch`;
- stale repair-state clearing;
- save/load context;
- current-save envelope extraction/injection;
- diagnostics for missing/malformed repair envelope.

Then implement blocker/epoch work first: N06, A04, A08, A09, A10, A12.

Exit gate: regression 16/28/29/30/32/35/36 source families cannot write or replay a half-committed timeline.

### SNLF Wave 2 - low-risk deterministic no-storage repairs

Implement the smallest source-local fixes:

- A03, A05, A06, A07, A18, A24, A25, A26, A27, A30, A32;
- N13 (#30);
- N14 (#31).

These should establish patch style, diagnostics, and regression harness without schema pressure.

### SNLF Wave 3 - core repair-envelope state

Implement stable, simple state first:

- N01, N05, N07, N08, N09, N10, N11;
- A02, A15, A16, A17, A29.

Then run repeated save/restart/F9/rollback tests before adding more complex object-rebinding state.

### SNLF Wave 4 - semantic rebinding/deferred state

Implement:

- N02 selected contract;
- N03 queued substory semantic descriptors;
- N04 room substory scene;
- N12 delayed ambient scene;
- A11 successor introductions;
- A13 temporary auto-task bans;
- A14 delayed tutorial continuation;
- A28 award-eve nominations;
- A31 unresolved portrait identity.

These require late binding and therefore should not be mixed into the first envelope milestone.

### SNLF Wave 5 - legacy deterministic migration

Implement A20-A23 plus the legacy compatibility halves already embedded in N01/N08/N11/A10/A28/A29/A31.

Every migration must be idempotent for the same source save. Never consume global random state as the migration oracle.

### SNLF Wave 6 - full regression closure

Run Save n Load Fixes regressions 1-47 in four modes:

1. fresh restart;
2. same-process F9;
3. rollback to older save;
4. repeated load of the same untouched save.

Then add the transport/coexistence matrix below.

---

# 6. Standalone Save Write Ordering Fix 1.4.0 plan

## 6.1 Preserve 1.3.0 safety architecture

Keep:

- caller-level transpilers only;
- no Harmony patch on constructed `DataSaver<T>`;
- per-physical-path FIFO;
- synchronous payload freezing;
- coordinated reads;
- file exclusive access;
- directory admission fence + drain;
- `Priority.Last` after normal persistence-preparation patches.

## 6.2 Add GlobalData transport

Add concrete caller transpilers for:

- `SaveManager.SaveGlobalData()` -> ordered writer for `SaveManager.GlobalData`;
- `SaveManager.LoadGlobalData()` -> wait-for-path then vanilla-compatible load.

Track GlobalData patch health separately from SavedData health.

Proposed API additions:

```text
GlobalDataInterceptionHealthy
EffectiveTransportHealthy
TransportOwner
IsAuthoritativeTransport
```

Keep `SavedDataInterceptionHealthy` for compatibility.

## 6.3 Save n Load Fixes delegation

Updated SWOF must discover `SaveNLoadFixes.SaveTransportApi` by reflection.

When SNLF is authoritative:

- do not create local write queues for intercepted save/global paths;
- transpilers that see an SNLF replacement treat it as **delegated success**, not a failure;
- all public wait/exclusive APIs delegate to SNLF;
- `SavedDataInterceptionHealthy` reports effective health so existing IMDataCore versions do not mistake delegation for transport failure;
- `TransportOwner` reports SNLF;
- do not recursively delegate back to SWOF.

Add `HarmonyAfter("com.cosmo.savenloadfixes")` to the standalone transport transpilers.

## 6.4 Refactor payload type

Current 1.3.0 `FrozenSaveWrite` contains a `SaveManager.SavedData DeferredDataToSerialize`. To support GlobalData cleanly, refactor the internal request into a payload-agnostic form such as:

```text
TargetPath
FrozenPayload
DeferredSerializer (only if retaining the existing exceptional fallback)
```

or two explicit concrete request kinds. Do not introduce a generic Harmony patch to make the implementation look elegant.

## 6.5 Documentation/test cleanup

The supplied SWOF tree has two planning/test-document drifts to fix alongside 1.4.0:

- `docs/TEST_MATRIX.md` still labels itself **1.2.0** while the mod is 1.3.0;
- that file references `scripts/Test-PatchHealthSource.py`, but no such script exists in the supplied repository snapshot.

Either add the source test script or remove the claim. For 1.4.0, prefer adding a real source/IL-shape test that verifies:

- five SavedData write callers;
- expected SavedData read call sites;
- one GlobalData write caller;
- one GlobalData read caller;
- no constructed generic DataSaver Harmony patch;
- SNLF ordering/delegation attributes; and
- effective health semantics.

---

# 7. Transport coexistence matrix

| Installed mods | Effective writer/reader owner | Standalone SWOF local queue | SWOF public API | Expected behavior |
|---|---|---|---|---|
| neither | vanilla `DataSaver` | N/A | N/A | vanilla races remain; only control fixture |
| SWOF 1.4 only | SWOF | active | local | ordered SavedData + GlobalData |
| SNLF only | SNLF | absent | absent | ordered SavedData + GlobalData + repair envelope |
| SNLF + SWOF 1.4 | **SNLF** | inactive/delegated | forwards to SNLF | one queue, one transport owner, no duplicate write |
| SNLF + legacy SWOF | **unsupported during initial development** | unspecified | no shim | legacy compatibility explicitly deferred until much later |

### Required coexistence tests

Use a separate transport suite so the frozen 47 SNLF regression numbers remain unchanged.

- `TX-01`: two rapid saves to one slot, final file is second request.
- `TX-02`: two different save paths remain independently asynchronous.
- `TX-03`: save-list read during pending write waits only on same path.
- `TX-04`: story chapter/overwrite/new-save caller coverage.
- `TX-05`: GlobalData rapid saves preserve request order and frozen contents.
- `TX-06`: GlobalData load waits for pending GlobalData write.
- `TX-07`: SNLF repair envelope matches the same frozen request under rapid saves.
- `TX-08`: SNLF-only mode has no SWOF assembly dependency.
- `TX-09`: SWOF-only mode has no SNLF assembly dependency.
- `TX-10`: both current mods produce exactly one physical write per request.
- `TX-11`: SWOF public `TryWaitForPendingWrites` delegates to the SNLF queue when both active.
- `TX-12`: SWOF public file exclusive access delegates to SNLF.
- `TX-13`: SWOF directory lease delegates to SNLF and prevents save-directory resurrection during delete/archive.
- `TX-14`: updated IMDataCore uses the SNLF provider for wait/file/directory coordination when SNLF is authoritative; legacy IMDataCore compatibility is not part of this test.
- `TX-15`: transport patch-health false when a required caller shape changes.
- `TX-16`: no constructed `DataSaver<T>` Harmony patch exists in either mod.
- `TX-17`: repair-envelope unknown-root compatibility with SNLF disabled.
- `TX-18`: F5/Save As/restart/F9 preserve one exact repair envelope and never attach a future envelope to an older vanilla save.

---

# 8. IMDataCore implementation plan

Implement IMDataCore in dependency order rather than finding-number order.

## IMDC Wave 0 - v6/v3 storage foundation

Do first because later identity/coverage/public API work depends on it.

Includes:

- #56 unsupported-generation handling: current v6/v3 accepts no pre-v6 storage and fails closed without conversion or overwrite;
- Area #12 v6 checkpoint identity-binding schema;
- journal v3 row/transaction shape;
- #58 minimal version-agnostic journal header/base-hash affinity;
- #57 version-aware shared participant/payload compatibility;
- #59 load-time shared-row normalization/compaction equivalence;
- #60 durable namespace-owner provenance;
- #61-#65 coverage/capability/namespace structures;
- #63 `HistoricalBaselineAssertions` branch carrier + public quality;
- migration provenance and fail-closed downgrade behavior.

Exit gate: regressions #48-#50, #57-#65, #72-#90, #91-#127, #140-#154 that are storage-generation/unsupported-format-sensitive have an executable or deterministic fixture path.

## IMDC Wave 1 - five durable-identity contracts

### Identity 1: contract generation

- allocate an opaque IMDC generation in `business.Accept` before nested `AddActiveProposal`;
- carry it through a scoped pending context;
- bind reconstructed active proposal by exact saved ordinal + structural witness;
- emit acceptance/activation/terminal rows under the same generation;
- legacy `idol|type|end-day` remains a candidate key, never a global exact alias.

### Identity 2: clique generation

- allocate at authoritative `Relationships.StartNewClique` birth;
- retain across leader/member mutations;
- checkpoint by serialized clique ordinal + witness;
- never derive canonical identity from current sorted membership.

### Identity 3: bullying episode generation

- allocate on not-bullied -> bullied transition;
- use a distinct episode ID, not `clique + target` as identity;
- carry parent clique generation as context;
- retire only after terminal history is enqueued/committed;
- restart of bullying against the same target gets a new episode ID.

### Identity 4: generated non-custom task generation

- allocate at `tasks.GenerateTask(...)` birth/insertion;
- coordinate with finding #27's missing `task_added` occurrence;
- checkpoint by saved task ordinal + full structural witness;
- custom scripted definition IDs keep their definition-stream semantics.

### Identity 5: room-work SSK/tour namespace

- keep canonical room-generation prefix;
- change child owner key from ambiguous `event:<N>` to `ssk:<ID>` or `tour:<ID>`;
- no extra random generation is required because vanilla already persists the two owner ID domains separately;
- legacy rows remain ambiguous candidates when owner cannot be proven.

### Shared identity compatibility

Implement Area #12 D06-D10 as one layer:

- `IdentityBindingsVersion` / completeness marker;
- native v6 complete binding snapshots;
- native v6 checkpoints carry complete/explicit binding knownness;
- unsupported pre-v6 checkpoints are never adopted or converted;
- candidate multimap with `Exact/Ambiguous/Unresolved` quality;
- branch-safe F9 rebinding;
- public resolver required by #66.

## IMDC Wave 2 - lifecycle/history capture (#14-#40)

Implement source-local event gaps after identity foundations are stable. Suggested grouping:

- people/auditions/hire/date: #14, #17-#19;
- business/loan/contracts: #15, #16, #23;
- groups/social/rivals: #20, #24, #25;
- shows/singles/awards: #5, #21, #22, #26;
- tasks/substories/scenes: #27-#29;
- split event/relationship history: #1, #2, #6, #30, #31;
- other lifecycle/history findings #32-#40 according to the existing Area #8 matrix.

Do not implement split findings' current continuation half in IMDC.

## IMDC Wave 3 - payload/timing/reference semantics (#41-#55)

**Implementation checkpoint:** Wave 3 Task 1, payload/timing #41-#47, is complete in IMDataCore 3.4.31; Wave 3 Task 2, historical references/lifetime #48-#52, is complete in 3.4.32; and Wave 3 Task 3, namespace bootstrap/order/reentrancy #53-#55, is complete in 3.4.33. Wave 3 is complete. The next planned phase is Wave 4 public query product #61-#68.

Apply the closed Area #9/#13/#14 contracts:

- payload/timing #41-#47;
- historical references/lifetime #48-#52;
- namespace bootstrap/order/reentrancy #53-#55.

Run legacy/current schema fixtures after every payload change so #57 is not regressed.

## IMDC Wave 4 - public query product (#61-#68)

**Implementation checkpoint:** Wave 4 Task 1, structured coverage/knownness **#61-#65**, is **FINAL-STATIC in IMDataCore 3.4.33**. Its source/deterministic implementation and dormant v6/v3 acceptance contracts are closed while the production storage gate intentionally remains sidecar v5 / journal v2. Real compilation plus Idol Manager/Unity #120-#127 execution is tracked separately in `mods/IM Data Core/docs/IMDC_WAVE4_V6V3_CUTOVER_VALIDATION_GATE.md`; the trusted legacy-unbound namespace owner authorization channel is tracked in `mods/IM Data Core/docs/IMDC_LEGACY_NAMESPACE_AUTHORIZATION_FOLLOWUP.md`. Neither follow-up redefines the remaining Wave 4 query-product tasks #66-#68.

**Implementation checkpoint:** Wave 4 Task 2, public current-generation resolver **#66**, is **FINAL-STATIC in IMDataCore 3.4.33**. The resolver surface was staged early in Wave 1 Task 6; Task 2 revalidates it against the finalized selected-branch v6 identity model, preserves live-v5 fail-closed behavior for opaque generations/candidate persistence, deduplicates redundant exact-alias evidence by canonical target, and rejects cross-kind candidate leakage. The next planned query-product task is **Wave 4 Task 3 / #67**, followed by **Task 4 / #68**.

**Implementation checkpoint:** Wave 4 Task 3, canonical career-wide durable-history paginator **#67**, is **FINAL-STATIC in IMDataCore 3.4.33**. `TryReadHistoryPage(...)` now exposes the selected branch's canonical physical `activeEvents` stream through the preferred, reflection-friendly, and uppercase facades; pages by stable EventId/shared sequence; preserves one-row-per-occurrence shared/global/namespaced semantics; and fails closed on discarded-branch cursors. The existing idol paginator remains the participant-expanded compatibility view. No storage-generation change is required. Task 3 remains closed.

**Implementation checkpoint:** Wave 4 Task 4, cursor-complete money-detail paginator **#68**, is **FINAL-STATIC in IMDataCore 3.4.33**. `TryReadMoneyTransactionsPage(...)` preserves the existing half-open game-date bucket semantics while adding exact active-branch EventId/shared-sequence continuation inside dense days; discarded-branch, changed-range, and namespaced-lookalike cursors fail closed; aggregate totals remain unchanged and uncapped. No storage-generation change is required. **Wave 4 public query product #61-#68 is FINAL-STATIC complete. The next planned phase is Wave 5 full 1-154 regression closure.**

After storage and identity are stable:

- structured coverage/knownness APIs (#61-#65);
- public current-generation resolver (#66);
- canonical career-wide durable-history paginator (#67);
- cursor-complete money-detail paginator (#68);
- both preferred and reflection-friendly facades;
- old money coverage method retained only as conservative compatibility convenience.

## IMDC Wave 5 - full 1-154 regression closure

**Implementation checkpoint:** Wave 5 Task 1, full #1-#154 static regression closure, is **FINAL-STATIC in IMDataCore 3.4.33**. A machine-readable registry maps every audit regression number to executable source/model evidence, and dedicated aggregate oracles now implement #147-#154. Native v6/v3 compile/runtime execution remains owned by `mods/IM Data Core/docs/IMDC_WAVE4_V6V3_CUTOVER_VALIDATION_GATE.md`; production remains sidecar v5 / journal v2 until that gate passes.

Run the full existing suite against:

- native v6 career;
- unsupported v1-v5 sidecar / v1-v2 journal rejection fixtures;
- native v6/v3 restart/rewind/compaction/Save-As fixtures;
- restart;
- F9 branch rewind;
- compaction;
- Save As;
- missing/reinstalled external content;
- namespace owner absent/present/upgraded;
- dense money history;
- identity reuse.

---

# 9. IMDataCore and SNLF transport cooperation

**Implementation checkpoint:** Wave 5 Task 2, ordered transport cooperation, is **FINAL-STATIC in IMDataCore 3.4.33**. IMDataCore now discovers the effective provider by reflection, preferring authoritative + healthy `SaveNLoadFixes.SaveTransportApi`, falling back to healthy `SaveWriteOrderingFix.SaveWriteOrderingApi`, and otherwise retaining its standalone detached-save/deletion safety path. Save-boundary optimization and directory deletion share one provider decision, so deletion acquires at most one effective-owner lease. The live sidecar v5 / journal v2 gate is unchanged.

**Implementation checkpoint:** Wave 5 Task 3, v6/v3 static release preflight, is **FINAL-STATIC in IMDataCore 3.4.33**. Canonical identity and structured coverage now share one atomic 6/3 activation predicate; physical initialization rejects split 6/2 or 5/3 configurations. Static migration/API/regression/nonduplication prerequisites are machine-checked. Live activation remains sidecar v5 / journal v2 until the separate compile/Unity cutover gate passes.

**Implementation checkpoint:** Wave 5 Task 4, companion transport reference validation, is **FINAL-STATIC in IMDataCore 3.4.33**. The reflection contract has been checked against supplied SNLF/SWOF source and Release DLLs; direct SNLF requires API version >= 1 plus exact owner identity, while current SWOF effective-health trust requires a recognized SWOF/delegated-SNLF owner. Legacy SWOF fallback and the live sidecar v5 / journal v2 gate remain unchanged.

Current IMDataCore 3.4.7 looks specifically for `SaveWriteOrderingFix.SaveWriteOrderingApi` and uses SWOF's health/exclusive-directory API when present. Update this in the IMDataCore implementation wave without changing defect counts.

Recommended provider preference:

1. `SaveNLoadFixes.SaveTransportApi` if authoritative and healthy;
2. otherwise `SaveWriteOrderingFix.SaveWriteOrderingApi` if healthy;
3. otherwise IMDataCore keeps its existing detached `SavedData` defensive snapshot and standalone deletion safety path.

Rename the internal concept from “Save Write Ordering Fix health” to **ordered transport provider health**. Keep reflection-only discovery so IMDataCore gains no hard dependency on either mod.

For save-directory deletion/archive, obtain the exclusive directory lease from the **effective owner**. Never acquire independent leases from two queue coordinators.

---

# 10. Regression execution strategy

## 10.1 Source/IL-shape tests first

Before launching Unity, add source tests that mechanically assert:

- expected concrete save/write target methods still exist;
- transpilers expect exactly one matching call per required method where the contract says one;
- no constructed generic `DataSaver<T>` Harmony patch exists;
- SNLF/SWOF Harmony ordering IDs are present;
- SNLF owns transport when both are present;
- repair-envelope schema version is explicit;
- all 47 SNLF regressions remain listed exactly once;
- all 154 IMDC regressions remain listed exactly once.

## 10.2 Runtime fixture tiers

### Tier 1 - smoke

- new game;
- autosave;
- manual save;
- F5;
- F9;
- fresh restart;
- Save As;
- GlobalData save/load.

### Tier 2 - repair-state families

Table-drive every SNLF persisted section across:

- save -> continue -> load same save;
- save -> mutate -> F9;
- save -> restart;
- save A -> save B -> load A;
- repeated load of untouched A;
- SNLF disabled/re-enabled compatibility where safe.

### Tier 3 - transport stress

Run TX-01 through TX-18 with artificial barriers/delays in the writer.

### Tier 4 - IMDC integration

Run split findings and transport provider permutations with IMDataCore enabled. Verify:

- no duplicate repair-owned current value enters IMDC sidecar;
- IMDC sees repaired current runtime when capturing new history;
- deletion/archive uses the effective queue owner;
- sidecar checkpoints align with the same physical vanilla save request.

## 10.3 Failure policy

A runtime save is allowed to fail softly only when continuing would not create a falsely valid checkpoint. Rules:

- unknown pre-fix repair state -> deterministic audited compatibility fallback or explicit unknown;
- malformed SNLF envelope -> do not reinterpret as exact empty state;
- failed exact repair-envelope freeze -> do not enqueue a mismatched vanilla save;
- unsupported pre-v6 IMDC storage -> preserve source bytes, perform no conversion, and remain fail closed;
- failed transport interception health -> expose diagnostics and keep conservative fallback behavior.

---

# 11. First development sprint

The first coding sprint should deliberately avoid the hardest semantic repairs.

## Sprint 1A - repository/project foundation

**Current implementation status:** source foundation implemented. Project/solution metadata, diagnostics, patch-health bookkeeping, concrete `DataSaver` source guards, and the fail-closed transport API skeleton are present. No Harmony save/load interception is active yet. Full `dotnet build` validation remains a developer-environment gate because the current execution environment does not contain the repository's private game/Harmony/Unity DLL set or a .NET/MSBuild toolchain.

1. add `mods/Save n Load Fixes/` project and metadata;
2. build cleanly under the repository `Directory.Build.props`;
3. add diagnostics and patch-health infrastructure;
4. add source tests for concrete DataSaver call-site counts;
5. add SNLF transport API skeleton.

## Sprint 1B - embedded transport

**Current implementation status:** transport source implementation is complete for the audited vanilla surfaces and all source/static guards pass. The current execution environment still lacks a .NET/MSBuild toolchain, private game/Harmony/Unity reference DLLs, and a Unity runtime, so compiled/Harmony-runtime proof remains an external gate. The repair-envelope unknown-root spike is intentionally still open because it specifically requires the real Unity/Mono serializer behavior.

Completed cumulatively:

1. port/refactor the ordered coordinator into the SNLF namespace with no old-provider dependency;
2. intercept all five audited `SavedData` write callers;
3. intercept all seven known `SavedData` reader methods / eight read call sites;
4. add `GlobalData` write/read interception;
5. freeze both DTO types on the caller thread after vanilla save-population events;
6. expose operational wait/file/directory coordination only when all four patch surfaces are healthy;
7. preserve per-path FIFO writers and add timeout-aware file-exclusive admission beneath directory leases;
8. prove by source/static guards that SNLF has no SWOF/old-IMDataCore coupling and no generic `DataSaver<T>` Harmony target.

Still open inside the broader Sprint-1B release gate:

9. run the repair-envelope injection/extraction unknown-root compatibility spike in the actual Idol Manager Unity/Mono runtime;
10. compile and exercise the transport against the repository's private runtime DLL set.

## Sprint 1C - standalone SWOF 1.4 branch

1. add GlobalData support;
2. add SNLF discovery/delegation;
3. add effective-owner health API;
4. update README/test matrix;
5. add the missing source test referenced by current docs.

## Sprint 1D - safety infrastructure

1. implement `CheckpointGate`;
2. implement `LoadEpoch`;
3. wire F5/F9/autosave controls;
4. implement only N06/A04/A08/A09/A10/A12 first;
5. run regression 16/28/29/30/32/35/36 plus TX suite.

Only after these foundations are stable should the project start persisting relationship dynamics, chains, room state, tutorial baselines, and the more complex rebinding families.

---

# 12. Release gates

## Save n Load Fixes 0.x

Allowed to ship only as development/test builds. Must have transport health diagnostics and must not claim all 46 repair families are complete.

## Save n Load Fixes 1.0.0

Requires:

- all 14 numbered repairs implemented;
- all 32 additional repairs implemented;
- regressions 1-47 passing;
- TX-01 through TX-18 passing;
- SNLF-only operation passing;
- SNLF + SWOF 1.4 coexistence passing;
- explicit documentation of behavior when SNLF is removed from an SNLF-managed save.

## Save Write Ordering Fix 1.4.0

Requires:

- current 1.3 career behavior preserved;
- GlobalData write/read ordered;
- SNLF delegation clean;
- existing public API compatible;
- test matrix corrected from stale 1.2.0 wording;
- source test script actually present.

## IMDataCore release containing v6/v3

Requires:

- sidecar v6 / journal v3 as the only supported storage generation;
- five identity contracts implemented;
- unsupported v1-v5/v1-v2 storage rejected without conversion or overwrite;
- public coverage/identity/history/money APIs complete;
- full 1-154 acceptance coverage;
- Save n Load Fixes current-state nonduplication checks passing.

---

# 13. Definition of done

A repair is done only when all of the following are true:

1. the source method/path that owns the bug is patched narrowly;
2. ownership matches the audit ledger;
3. exact current state has one persistence owner;
4. pre-fix compatibility is deterministic or explicitly unknown;
5. F9 cannot leak discarded-timeline state;
6. fresh restart reconstructs the same repaired state;
7. repeated loading of an untouched save is idempotent;
8. Save As preserves the intended repair state;
9. SWOF/SNLF coexistence does not create duplicate writes or independent queues; and
10. the corresponding audit regression contract is executable and passes.

The recommended immediate coding path is now: finish the two remaining **Sprint 1B runtime gates** (compiled transport/Harmony proof plus repair-envelope unknown-root spike), then move to **Sprint 1D safety infrastructure (`CheckpointGate` + `LoadEpoch`)** before stateful semantic repairs. Standalone SWOF 1.4.0 and IMDataCore should be updated later against the current SNLF provider contract; no old-release compatibility shim is required during this stage.
