# Save n Load Fixes

## Version 5.5.6

Current source version: **5.5.6**. This tree also contains cumulative Rivals Reborn wide-numeric compatibility work.

Save n Load Fixes (SNLF) is the Cosmo repair mod for Idol Manager save/load continuity, deterministic reconstruction of legacy or omitted state, safe save transport, and verified gameplay-state bugs that become persistence bugs. It is deliberately conservative: repair state must be attributable to the exact save being loaded, widened arithmetic must not silently wrap, and an uncertain repair should fail closed rather than guess.

Release versions use decimal carry for all components: `5.5.9` advances to `5.6.0`, and `9.9.9` advances to `10.0.0`. The old `0.55.1` label was corrected to `5.5.1`; older changelog labels are retained as historical records.

Implemented cumulatively through 5.5.6: all released SNLF repair/transport families listed below, plus the unreleased cumulative Rivals Reborn compatibility additions documented in this tree.

See `CHANGELOG.md` for release history. This README is the canonical current contract and patch inventory. Historical sprint/task notes, one-off qualification reports, and staged RR notes have been folded into these two files and removed from the source bundle.

## Compatibility and optional integrations

SNLF has no compile-time dependency on Rivals Reborn or the Tel Mod Library. Optional integrations are discovered at runtime and are skipped when their target mod is absent.

- **Save Write Ordering Fix (SWOF):** compatible. When both are present, the current standalone SWOF recognizes SNLF's ordered transport and delegates to it.
- **CreateAnAlbum:** current CAA contains its own fallback SavedData transport. For the supplied current CAA source, use **SWOF together with SNLF + CAA** so CAA disables its embedded fallback and SNLF remains the authoritative transport owner. This is a transport-composition requirement, not a numeric-width requirement.
- **Rivals Reborn:** SNLF contains an optional reflection-only wide-numeric bridge keyed to Harmony owner `rivalsreborn`. It does not reference `rivalsreborn.dll` and adds no RR persistence schema.
- **Fans Watch Shows:** SNLF contains an optional, strict compatibility profile for TrueBlueSwablu's `com.tbs.fanswatch` **1.0.0** / assembly **1.0.0.0**. For that audited version only, SNLF reproduces the mod's existing audience formula and settings with wide-number-safe arithmetic. Unknown FWS versions are left untouched and reported as unsupported until reviewed. SNLF has no compile-time FWS dependency.
- **TBS Balance Patch:** SNLF contains an optional, strict profile for TrueBlueSwablu's `com.tbs.balancepatch` **1.0.0** / assembly **1.0.0.0**. The profile preserves that version's configured proposal, tour, sister-group, salary, CD-softcap, and hard-mode loan formulas on SNLF's widened values; it also deliberately corrects the audited 1.0.0 main-group fan postfix so it applies the fame-scaled multiplier it computes rather than the raw `MAIN_GROUP_MULT`. Unknown versions are left untouched until reviewed. SNLF has no compile-time Balance Patch dependency.

- **Shelon Tweaks & QoL Improvements:** SNLF contains an optional strict compatibility profile for `im.mod.shelon.tweaksnqol` assembly **1.0.0.0** / informational version **1.0.0**. For that audited build only, A20 legacy peak-age migration uses the mod's actual `Random.Range(23,45)` behavior (ages 23-44) instead of vanilla 16-24. The show-tooltip patch is untouched. Unknown versions fall back to vanilla A20 migration behavior.
- **Tel Mod Library:** SNLF has narrowly scoped reflection interop for current-head Going Viral, Fan Attrition, Stale Theater Shows, Extended SSK, Unofficial Patch, MBTI Personalities, Sister Groups, Tour Stamina, Traits Fix, and Traits Expansion where those mods intersect SNLF replacement paths. This is **not** a blanket claim that every Tel mod composes with every unrelated mod. Policy/UI JSON conflicts, for example, are outside SNLF's save/numeric contract.
- **EroEvents:** `EroEventsDataCodec` is an opt-in exact Int32/Int64 payload codec owned by `com.seraph.eroevents`. It does not automatically migrate EroEvents variables or alter EroEvents policy definitions.
- **IM Data Core / Graduation Details:** SNLF retains the coordinated checkpoint/witness behavior implemented by the current source. IMDC remains responsible for its own sidecar durability and schema.

### BuffMe 1.0.0 compatibility

When the exact audited **Vanilas BuffMe 1.0.0** assembly is enabled, SNLF replaces BuffMe's positive money/fan `Double` narrowing with wide-safe arithmetic. It also corrects BuffMe 1.0.0's duplicate fan multiplication: a positive semantic fan award receives the configured `Fan_Multiplier` exactly once, whether it enters through `resources._Add(fans, ...)`, a direct idol `AddFans(...)` call, or an SNLF wide continuation. BuffMe's stamina reduction patch remains untouched. Unknown BuffMe versions are not overridden.

SNLF compatibility corrections used to bridge clamped vanilla ABI values are not treated as second rewards. Business and café correction deltas are calculated as the difference between the BuffMe-scaled exact reward and the BuffMe-scaled compatibility reward.

### A35 business proposal payment continuation

Vanilla business proposal payments are widened before the original `Mathf.FloorToInt` generation boundary. SNLF keeps the exact base/effective payment in runtime sidecars, uses the vanilla `Int32` fields only as compatibility mirrors, carries exact weekly payments into accepted contracts, uses the exact values for liability, agency/idol earnings, weekly profit, history, and contract UI, and persists out-of-`Int32` active-contract payments in `wide_numeric_state_version = 3`. Pre-v3 saves seed business-contract payments from their surviving vanilla `Int32` values; SNLF does not claim to reconstruct already-lost historical high bits.

## Core save transport contract

SNLF owns an ordered `SavedData` transport rather than patching a constructed `DataSaver<T>` generic method. Its frozen manifest covers the five concrete SavedData writers, seven reader methods / eight read sites, both GlobalData callers, and the audited startup `FixSaveFile` migration seam.

The transport contract is:

1. Vanilla/mod save-event collection runs first.
2. Repair state and registered embedded mod payloads are captured synchronously on the caller/Unity thread.
3. The complete payload is validated before queue admission.
4. Physical writes are serialized per destination path.
5. The newest same-path attempt is tracked through completion.
6. If the newest overwrite fails, a same-process read refuses to silently return the older bytes still on disk. A later successful write clears that stale-read guard.
7. Recomposition is accepted only when the complete exact SNLF replacement shape is observed. Mixed, partial, duplicate, or malformed transport shapes do not establish health.

Startup legacy-key repair is type preserving. SNLF performs only the audited `val` to `_val` rename directly in the original UTF-8 JSON instead of letting vanilla SimpleJSON rewrite every scalar as a string.

### Save completion and shutdown

`Settings_Tab.OnQuit` and Return to Main Menu use the real vanilla `Save()` route, then wait until SNLF's physical writes and every registered mod save attempt reach a terminal state. Unity's update loop stays alive so coroutines and main-thread continuations can finish.

A task that succeeds, faults, or is cancelled is terminal. A task that never completes keeps normal shutdown pending. A timeout is never relabeled as successful completion. Forced process termination, crashes, or OS kills are outside this guarantee.

When another mod vetoes the eventual quit, SNLF reopens save admission on the next update after Unity continues running.

### Save progress notifications

Each actual SNLF-owned SavedData write emits a start notification. Completion is emitted only after the physical writer succeeds and coordinated participating persistence reaches a terminal result. Failure has a separate notification. Worker-thread completions are marshalled back through Unity update before touching notification UI.

## Checkpoint safety and repair envelope

Repair-dependent state is stored under SNLF's `__cosmo_save_n_load_fixes` root in the same physical save. A present malformed envelope is never treated as an exact empty checkpoint. Legacy absence means that older saves may require deterministic reconstruction or may have unknowable state, depending on the repair family.

The checkpoint gate blocks a write when SNLF knows it cannot serialize a repair-dependent state exactly. The four semantic producer families are:

- `EventTemplatesPendingOpen`
- `SemanticCallbackFinalFrame`
- `BirthdayQueueBetweenPopups`
- `SskPostPaymentLaunch`

Wide-numeric and transport health add their own fail-closed conditions. Actual signed-Int64 overflow or exhausted persistent Int32 identity space refuses the operation instead of wrapping or saturating authoritative state. In-game load remains available for recovery.

## Participating mod APIs

### SaveParticipationApi

Mods that own asynchronous sidecars can participate in SNLF's save/shutdown barrier. A registered callback runs at the concrete SavedData write seam after vanilla save-event collection and before payload freeze/queue admission.

```csharp
IDisposable registration = SaveParticipationApi.RegisterSidecarWriter(
    "com.example.mod", request =>
    {
        string frozen = SerializeStateOnUnityThread();
        return Task.Run(() => WriteSidecar(request.AbsoluteSavePath, frozen));
    });
```

The returned task must cover the complete IO/cleanup lifetime. Returning an already-completed task while detached work continues violates the contract. Throwing/faulting/cancelling reports failure but does not prevent other subscribers from running.

A mod with its own save hook may instead call `BeginSidecarAttempt(owner, absoluteSavePath)` before starting work and complete that attempt only when all work has ended. SNLF cannot infer arbitrary unregistered background threads.

### ModDataApi

Mods may store isolated owner-namespaced payloads inside the same physical save:

```csharp
IDisposable registration = ModDataApi.Register(
    "com.example.mod",
    stored => RestoreOrMigrate(stored),
    () => new ModPayload(CurrentSchemaVersion, SerializeState()));
```

The owner controls its schema independently of DLL version. Unsupported/future owner records are preserved rather than restored into runtime state. A failed restore/capture does not authorize destructive replacement. Missing owners are carried forward unchanged while SNLF remains installed.

The container format is versioned and stores each payload as validated JSON text so numeric token spelling is preserved and no CLR polymorphic activation is required.

### EroEventsDataCodec

`EncodeInt32` writes schema 1 and `EncodeInt64` writes schema 2. Int64 readers accept both and widen schema-1 values exactly. Int32 readers accept either only when every value fits Int32. Decimal strings and exact integer JSON tokens are accepted; fractional, exponent, malformed, noncanonical, or overflowing values are rejected. Values above `2^53` never pass through `Double`.

This codec is only a storage option for EroEvents or another explicit caller. SNLF does not reinterpret EroEvents' existing string variables automatically.

## Monthly transition repair (A34)

A34 restores the vanilla `mainScript.onNewMonth` callback that is declared and subscribed but never invoked by `mainScript.TimeProgress`. The patch targets only the generated `TimeProgress.MoveNext` state machine and injects the existing monthly delegate immediately after vanilla raises `onNewDay`; it invokes `onNewMonth` only when the new game date is day 1. This restores vanilla monthly subscribers without duplicating their logic or firing during save loading. In the audited vanilla build, this reactivates `Awards.OnNewMonth()` (Best Employer condition tracking) and `data_girls.OnNewMonth()` (monthly idol earnings-history rollover). Existing saves cannot reconstruct missed historical monthly Best Employer failures from periods before A34 was active; future month boundaries are tracked normally, and the next post-awards yearly reset begins a fully repaired award cycle.

## Wide numeric continuity (A33)

A33 widens audited scalable money, fan, sales, audience, subscriber, project, story, accounting, salary, theater, tour, show, café, concert, SSK, research, statistics, and UI paths to checked Int64/exact arithmetic while preserving narrow vanilla ABI boundaries where required.

The numeric rules are:

- widen before the first lossy or overflowing operation;
- preserve the exact compiled IEEE-754 `Single` coefficient when reproducing a vanilla/mod formula;
- use the original rounding rule: midpoint-to-even, floor, ceiling, or truncation toward zero as appropriate;
- preflight multi-step authoritative mutations where partial mutation would corrupt state;
- never silently wrap Int64;
- clamp only at deliberate compatibility/UI ABI edges, never in authoritative storage;
- keep exact repair shadows as canonical decimal strings where vanilla storage cannot represent the authoritative value;
- avoid a `System.Numerics` runtime dependency so old Unity/Mono loads do not depend on an unavailable assembly.

`wide_numeric_state_version = 2` is the current exact-shadow format. Version 1 remains readable under its documented legacy fallback rules.

## Rivals Reborn wide-numeric compatibility

The RR bridge is cumulative and reflection-only. Every transpiler validates the expected current-head method shape; if a target no longer matches, SNLF logs one warning and leaves that RR method unchanged rather than guessing. RR absence or an RR shape mismatch does not globally poison SNLF's save transport.

### Exact Single-product arithmetic

`WideNumericMath.TruncateSingleProduct(long, params float[])` decomposes each compiled `Single` coefficient exactly, performs the product as an exact rational, truncates toward zero like the original C# `(long)` cast, and requires the final result to fit Int64. `WideNumericRepair.TruncateSingleProduct` supplies the normal named overflow diagnostic/latch behavior.

`CompareSingleProducts(...)` compares weighted RR fan scores without materializing an Int64 product, so a conceptual `Fans * Momentum * 2.5f` value may exceed Int64 while the comparison itself remains exact.

### Patched RR paths

- `RosterSim.DampFanGrowth`: both fan-growth products keep RR's own RNG/settings but no longer narrow the fan total to `float`.
- `SpecialLabels.DoAccusation`: `Fans * 1.35f` uses exact truncating Single arithmetic.
- `RosterSim.AdjustSales`: initial fan-to-sales conversion, momentum scaling, and post-350,000 damping all keep RR's original truncation semantics without losing low fan/sales bits.
- `Portraits.ComputeIdolFans`: applies RR's existing float share to the Int64 group fan total without first narrowing that total.
- `Portraits.RefreshFans`: demographic allocation uses the same exact product while preserving RR's final `total - allocated` remainder bucket, so fan conservation remains explicit.
- `SpecialLabels.FoundFromRetiree`: seeds the rival group from `girl.GetFans_Total(null)` rather than the capped Int32 `girl.fans` mirror.
- `PoachSim.PickTarget`: exact fan total is used for the 2,000-fan threshold and popularity/log input.
- `PoachSim.BuyoutPrice`: exact fan total, checked Int64 multiply/add, RR's original wealth coefficient, and original final rounding-to-thousands behavior.
- `PoachSim.ResolveTempt`: salary `* 1.2f` and expected salary `* 1.1f` use exact truncating Single arithmetic before RR's existing `Math.Max` decision.
- `RosterSim.YearlyShakeout`: weakest-label ordering compares exact weighted fan products, including the former-idol `2.5f` multiplier, without overflowing a materialized Int64 score.
- `RivalAwards.PickLabel`: once fan totals exceed Single's exact-integer range, `log10` is taken from the Int64 value instead of a lossy float-cast fan count. RR's final award score remains a float because momentum/noise are intrinsically floating point.
- `RivalsUI.FormatFans`: large fan counts are divided as `decimal` before `K`/`M` formatting.
- `SpecialLabels.QueueFounderRoll`, `RosterSim.FromPlayerGirl`, and `XRelSim.ResolveDatingChoice`: stable gameplay consumers now read exact player-idol fan totals rather than the capped `girl.fans` mirror.

Deliberately not patched: bounded scout arithmetic that cannot reach Single's loss boundary under current RR ranges, intrinsically floating-point probability/momentum systems, debug-only fan reads, and the RR graduation `fans >= 5000` callback where an Int32 cap cannot change the threshold result.

The RR bridge adds no SNLF repair-envelope state because RR's authoritative rival group fans and single sales are already Int64 and RR serializes its own long fields exactly. This compatibility layer repairs calculations/consumers, not RR storage ownership.

## Repair inventory

Historical A/N task identifiers remain useful for changelog archaeology, but current behavior is grouped here by contract instead of by sprint.

### Transport, startup, and lifecycle

- ordered SavedData write/read transport with Harmony recomposition health checks;
- ordered GlobalData transport and exactly-once startup `GlobalData` delivery;
- type-preserving startup `FixSaveFile` legacy rename;
- newest-failed-overwrite stale-read refusal;
- save-progress notifications and shutdown/main-menu completion barrier;
- IM Data Core coordinated checkpoint/witness outcome handling;
- LoadEpoch invalidation for stale same-process delayed work.

### Deterministic legacy reconstruction

- idol birthday and peak-age migration;
- legacy aggregate-fan redistribution;
- legacy relationship bootstrap;
- legacy rival ecosystem bootstrap.

These migrations use repair-owned deterministic streams keyed to stable target-save identity instead of consuming the live Unity RNG. They only replace the proven legacy-missing branch and let the next normal save commit modern vanilla state.

### Persisted or reconstructed continuity

- relationship `Dynamic` values;
- ordered `Activities.Chain` future intent;
- paused-training displaced idol (`girl_paused`);
- room `substoryScene` dialogue identity;
- queued substory `BeforeStart` semantic setup descriptors;
- selected random-event business proposal;
- pending ambient `Scenes` capture, persistence, exact rebinding/rescheduling, and rollback protection;
- award-eve temporary nominations and best-single binding;
- `FanAppeal_LastSingle` release comparison baseline;
- show last-episode fan-appeal state;
- recent-activity recency clock;
- private project progress counter;
- tutorial activity baseline and delayed tutorial continuation;
- temporary auto-task bans with remaining scaled-time delay;
- graduation-successor introduction continuation;
- previous-new-substory spacing anchor;
- paused-business remaining minutes;
- Event Overlord latest-event pacing state;
- unresolved external portrait asset identity;
- SNS exact nonrecursive message/reply state;
- SSK post-payment launch continuity and other checkpoint-gated semantic callbacks.

### Direct state/gameplay repairs with persistence impact

- training `Progress_Init` reconstruction;
- risky-single `Marketing_Result == 0f` restoration;
- `data_girls.LastGirlID` allocator restoration;
- `loans.BankruptcyDanger` reconstruction;
- unfinished-concert `FinishDate` preservation;
- trivia zero-counter authoritative restoration;
- award `her_choice` single-resolution consistency;
- immutable graduation-date adjustment repair;
- show next-episode/cancellation-date helper repair;
- stale `Tutorial.Active_Tutorial_ID` lifecycle/business-success override;
- group target-audience DTO round-trip repair;
- Event Manager nonstandard terminal-state repair;
- player forced idol-idol breakup coherence;
- pushed-slot duration baseline reset when the selected idol changes;
- wide money/fan/sales/audience/subscriber/statistics and display continuity under A33.

The source under `src/Repairs` is the authoritative implementation list. `CHANGELOG.md` records when each family entered the project and retains the detailed historical task numbering.

## Failure and ownership rules

SNLF distinguishes three cases deliberately:

- **Legacy absence:** the save predates a repair section. SNLF either runs a deterministic, source-proven migration or leaves unknowable state unknown.
- **Current-format authoritative empty:** a valid section explicitly says there is nothing to restore.
- **Present malformed/inconsistent state:** fail closed. Do not reinterpret it as empty and do not guess a replacement.

SNLF does not claim ownership of another mod's gameplay schema merely because it can transport that mod's payload. Optional compatibility code must preserve the originating mod's RNG/settings/formulas except at the specific lossy numeric or persistence boundary being repaired.

## Validation status

The repository contains source/contract checks plus isolated runtime harnesses. The current RR bridge has a dedicated source contract that verifies reflection-only dependency, patch wiring, exact-fan bridging, checked poach arithmetic, exact shakeout comparison, display/scoring fixes, and lack of a new persistence schema.

`WideNumericRuntimeHarness.cs` contains frozen RR arithmetic regression oracles for:

- parity through Single's exact-integer boundary (`2^24`);
- `16,777,217`;
- `Int32.MaxValue`;
- values above `2^32`;
- `2^53`;
- negative truncate-toward-zero behavior;
- RR sales/salary coefficients;
- Int64 overflow refusal;
- exact yearly-shakeout comparisons, including products that would overflow a materialized Int64 score.

In this editing environment no compilation or Unity runtime execution was performed. Therefore the source now contains the regression harnesses, but live RR integration qualification still belongs to a compiled test pass. That live pass should verify unchanged RNG call counts, demographic fan conservation, >Int32 former-idol founding, poach pricing, salary mutation, save/load/restart behavior, and that overflow aborts before authoritative mutation.

For source-only verification, run the Python files under `tests/`, including `Test-RivalsRebornWideNumericInteropSource.py`, `Test-WideNumericContract.py`, `Test-WideNumericSource.py`, `Test-TelModLibraryInteropSource.py`, `Test-TransportCorrectnessAudit.py`, and `Test-VersionSync.py`. Tests that require the decompiled game tree accept/use the source root expected by that script.

The normal repository build still requires the game's private assemblies, Harmony/Unity references, and a suitable .NET/MSBuild toolchain.
