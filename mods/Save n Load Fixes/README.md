# Save n Load Fixes

## Version 0.55.0

Save and Exit now waits for SNLF's pending writes and registered mod save attempts
to **finish**, whether they succeed, fault or cancel. Return to Main Menu uses the
same barrier. Unity's update loop remains alive for callbacks/coroutines; a timeout
does not count as completion. Mods must track their asynchronous work through
`SaveParticipationApi`; SNLF cannot discover arbitrary detached mod threads.

`ModDataApi` provides isolated, owner-namespaced payloads in the same physical save.
The mod owns its schema compatibility and migration policy independently of its
DLL version. Absent, incompatible and future-format payloads are preserved without
being restored into vanilla state. `EroEventsDataCodec` supplies opt-in exact
Int32/Int64 payload conversion. See [the integration contract](docs/MOD_PERSISTENCE_API.md)
and [native completion qualification](docs/SHUTDOWN_QUALIFICATION_2026_09_22.md).

Version 0.54.1 fixes save notifications for autosaves, manual saves, and chapter/story-slot writes. Each actual write gets independent start/completion tracking, including concurrent saves whose per-path attempt IDs coincide. `tests/Test-SaveProgressRuntime.ps1` runs the production coordinator and writer completion callback against overlapping writes, failures, and worker-thread completion.

**Runtime compatibility hotfix:** SNLF must load ordinary vanilla saves and older SNLF saves without requiring prior SNLF metadata. The current Task-6 hotfix removes A33's `System.Numerics` runtime dependency after Unity/Mono was observed throwing from the patched idol salary/UI path even though vanilla `SaveManager.LoadData` had succeeded. SNLF now keeps its exact wide arithmetic self-contained. No vanilla idol migration or guessed recovery path is involved.

## Version 0.54.0

Version 0.54.0 completes the A33.1-A33.6 wide-numeric implementation. Scalable money,
fan, sales, audience, subscriber, project, story, and accounting paths now calculate in
checked `Int64`; narrow vanilla storage is paired with exact SNLF decimal-string shadows;
and comparisons, tooltips, animations, scripts, counters, and persistent ID allocators no
longer silently wrap those values. In particular, theater ticket sales calculate directly
as `Int64`: 250 visitors at ¥99,000,000 each produces the exact ¥24,750,000,000 result.
The theater Pricing UI also renders subscription revenue from the exact `Int64` path, so wide monthly streaming income is not narrowed back to `Int32` for display.
The money-display audit also makes the weekly theater hover include streaming income, keeps full-width monetary text exact, removes float precision loss from idol earnings and new-single production costs, and carries staff severance through exact checked `Int64` calculation and display paths.
An actual signed-`Int64` overflow or exhausted persistent `Int32` identity refuses the
operation, latches a named diagnostic, and blocks checkpoint writes instead of wrapping
or saturating; in-game load remains available for recovery.

Post-0.52 envelope qualification fixes a release-blocking persistence defect: the
physical v0.52 save could contain only the SNLF header while silently omitting
`records`. SNLF now writes the finite repair graph independently of Unity's nested
DTO writer and verifies raw presence plus a complete deserialize/value round trip
before queueing any repair-dependent save. A header-only root is invalid, never an
exact empty checkpoint.

The startup compatibility path is now type-preserving. Vanilla
`SaveManager.FixSaveFile` used SimpleJSON to rename legacy idol parameter key `val`
to `_val`, then rewrote the entire save; this game's `JSONData.ToString()` quotes
every scalar and therefore changed SNLF numbers/Booleans into strings. SNLF keeps
the one audited rename but applies it directly to the original UTF-8 JSON text,
leaving every other token untouched. The interception is part of transport health.
Already-rewritten envelopes are recovered only when every scalar has the uniform
SimpleJSON string shape and every schema-directed conversion has a unique canonical
preimage. Unknown envelope members, mixed token types, ambiguous `"null"`, and
locale-dependent decimal/thousands collisions remain invalid rather than being
guessed. The V1 business-proposal
record also carries an additive exact decimal witness for its sole `Int64`, so large
liabilities never depend on SimpleJSON's lossy `Double` fallback.

SNS state is now part of the same SNLF envelope as a non-recursive ordered node
table. This preserves the exact finite message/reply tree after load even though
vanilla's compiled `_message.Replies` schema is recursive. The vanilla field remains
in the vanilla JSON for compatibility without SNLF, so Unity may still print its
schema-depth warning; exact SNLF restoration no longer depends on the truncated
recursive representation. After the complete envelope passes its strict serialization
round trip, SNLF logs `SNS Fix successfully serialized`; after an adopted checkpoint is
validated and reconstructed, it logs `SNS Fix successfully deserialized and restored`.
These messages identify the independent exact SNS path without claiming that Unity's
compatibility warning itself was suppressed.

The embedded transport and standalone SWOF paths are also Harmony-recomposition
safe: each accepts its own complete exact replacement shape on re-entry while still
rejecting mixed, partial, or duplicate call-site shapes. HarmonyX's opaque `0/0`
intermediate observations are neutral and pending: they cannot establish authority or
poison a later exact composition. SNLF logs one positive transport self-check only when
all 5 SavedData writers, 7 readers / 8 read sites, and both GlobalData callers have
reported their complete exact shapes, together with the one audited type-preserving
`FixSaveFile` migration site.

Post-0.52 runtime qualification fix: A14's Harmony bookkeeping and activities-delay
transpiler are re-entry-safe when HarmonyX recomposes the generated tutorial iterator.
This prevents a false A14 health failure from vetoing legacy-save `Save As` while retaining
the exact two-site manifest and malformed/partial-shape refusal.

The same qualification pass fixes pre-A29 old-save synthesis timing. Vanilla clears
`Groups.Groups_` during `LoadEvent` and reconstructs group membership in private
`Groups._Load` several frames later, so A29 now defers its release-formula baseline
until that exact seam. The pending work is `LoadEpoch`-bound, and a checkpoint during
the short unresolved window fails closed instead of persisting a false null baseline.

`Save n Load Fixes` is the dedicated Cosmo repair mod for vanilla save/load continuity,
reconstruction, and closely related verified gameplay-state bugs identified by the
persistence audit.

### Current development state

This is a cumulative development build through **A33.6: wide numeric continuity and overflow repair**, over Sprint 1D Task 50 / A23 and all earlier repair/transport work. All six A33 implementation segments compile, pass their decompiled-source and implementation contracts, and pass the isolated arithmetic/envelope/Harmony runtime harnesses. The live Unity overwrite-save, Save As, autosave, F9, restart, repeated-load, and mod-combination matrix remains a separate release qualification gate.

Implemented cumulatively through 0.55.0:

- **A33.1-A33.6 wide numeric continuity and overflow repair:** checked add/subtract/negate/multiply, exact rational/decimal midpoint-to-even rounding, exact `Int64` aggregation, and explicit narrow-ABI compatibility conversions now cover the audited resource, rent, business, loan, salary, fan engine, single, show, tour, theater, café, concert, SSK, research, story, VN, statistics, and UI paths. Multi-step mutations are preflighted transactionally, including mod-expanded theater prices. SNLF persists exact tour/single/show/theater/Stats/story/loan/café shadows as canonical decimal strings in `wide_numeric_state_version = 2`, accepts version 1 with an honest legacy fallback for its newly added chapter-four scandal baseline, validates identity/count/mirror witnesses before restore, and follows draft tours by object identity across ID assignment. Exact scandal totals and chapter-three/four targets flow through gameplay and display consumers; bounded vanilla ABI endpoints clamp only at the compatibility edge. Persistent counters and allocators fail closed at exhaustion instead of wrapping. Both frozen Harmony manifests are recomposition-idempotent, health-gated, and part of the checkpoint veto.

- **A23 deterministic legacy rival bootstrap:** SNLF scopes a replacement only to the `Rivals.Generate()` call reached from `Rivals.LoadFunction()` when the exact target save serializes zero rival groups. The replacement mirrors vanilla's complete bootstrap: all genre/choreography/lyrics trend rows, 50 groups, exact fan-band ranges, rising/genre/name choices, fixed top-three overrides, story rival/Phantasm tagging, descending sorts, and the three `LinearFunction` initializations. Every bootstrap random choice comes from the shared repair-owned deterministic stream keyed to the physical save/migration identity. Because vanilla `GenerateGroup()` destructively removes chosen names from the global `rival_group` pool, A23 also snapshots that pool before the first `Generate()` mutation and restores it at each legacy migration so repeated F9 loads do not inherit discarded-timeline name consumption. New-career `Start()`, later `OnNewMonth()` generation, and normal `GenerateGroup()` remain vanilla-owned. No repair-envelope field is added because the next ordinary vanilla save serializes the migrated rival ecosystem normally.

- **A22 deterministic legacy relationship bootstrap:** SNLF scopes replacement behavior only to vanilla `InitialCreationForOldSave()`. Within that compatibility scope, `_relationship.Initialize()` keeps `Vals = [+1,-1]`, `CanDate() -> positive`, the exact Dynamic 1..3 domain, and `Recalc(false)`, while deriving the non-date-compatible choice from stable unordered idol-pair identity via the shared repair-owned deterministic stream. Ordinary relationship creation stays vanilla; no new envelope section is added because existing N01 remains the sole persistence owner for current-format `_relationship.Dynamic`.

- **A21 deterministic legacy aggregate-fan migration:** SNLF replaces only vanilla's private `RedistributeFansOnOldSaveLoad()` compatibility bootstrap. If any active idol already has per-idol fans, the repair is an exact no-op. Otherwise it reads the saved `resources.Fans_Legacy` aggregate, uses the Task-47 repair-owned deterministic stream to reproduce vanilla's shuffled fame-weighted/equal idol apportionment and appeal-weighted demographic apportionment, and assigns all rounding residue deterministically while verifying the reconstructed active-idol total equals the old saved aggregate exactly. Ordinary positive-fan award/nomination multipliers are deliberately not replayed because those fans are already committed in the legacy total. No Unity RNG, repair-envelope state, checkpoint blocker, LoadEpoch carrier, or IMDataCore state is added; the next ordinary save commits the synthesized per-idol vectors through vanilla `GirlData.Fans`.

- **A20 deterministic legacy idol birthday/peak-age migration:** before vanilla `data_girls.LoadFunction()` reconstructs idols, SNLF populates only the exact legacy-missing branches (`birthday == null`, `peakAge <= 1`) in the target vanilla DTO. A repair-owned versioned deterministic stream is seeded from the exact weakly-associated physical save path when available, serialized target save identity, stable idol ID + saved ordinal, and field discriminator. Birthday mirrors vanilla's `-24 years + Range(0,12) years + Range(0,12) months + Range(0,31) days` domain; peak age remains 16-24. No Unity RNG, supplemental repair-envelope state, or IMDataCore current-state ownership is introduced. Repeated loads of the same untouched legacy save therefore synthesize the same migration, and the next normal vanilla save commits those values into the modern fields.

- **A31 unresolved external portrait identity preservation:** when an idol/staff save references an external portrait asset that is currently unavailable, vanilla `GetTextureAssetByID(...)` may bind an unrelated same-type fallback and later save that fallback ID. SNLF now records only unresolved `(entity kind, stable entity ID, sprite type, original asset_id)` tokens. Current-format load validates the whole section, rewrites the exact target DTO slot back to the original ID before vanilla reconstruction, then compares the fresh runtime asset to that ID: exact reappearance clears the token naturally; same-type substitution or a null asset keeps it unresolved. Idol/staff save Postfixes rewrite the original ID back into the vanilla DTO, recreating a missing DTO slot when vanilla omitted a null asset, before the caller-thread repair-envelope freeze. Pre-A31 saves can preserve an original ID only when that ID still survives in the target DTO; already-corrupted substitute-only saves are not guessed backward. No title/type similarity, provider rename guess, global `GetTextureAssetByID` patch, RNG, or IMDataCore state is introduced.

- **A28 award-eve `Awards.TempNominations` continuity:** repair-envelope V1 now persists the exact pending award slate as ordered source-proven award type + year + nullable stable idol/single identity. `Awards.LoadFunction()` Prefix clears discarded same-process RAM before vanilla `Reset()`/load, and its Postfix restores only after vanilla has reached its existing idol/single late-binding seam. Current-format empty is authoritative; malformed/present state fails closed. For pre-A28 saves, SNLF reconstructs only on the exact award-eve date when saved speech **types** prove a pending slate, never treating mutable speech-giver/thanks targets as nominee identity. Individual nominees are rebuilt deterministically from target-loaded state; `best_single` mirrors vanilla eligibility/ordering but replaces its 50/50 equal-position RNG with a stable single-ID tie-break. Canonical `Reset_After_Awards()` remains vanilla-owned, so a post-awards save naturally captures an empty A28 section.

- **A14 delayed tutorial continuation:** SNLF scopes semantic tracking to the exact `Tutorial_Actions._Coroutine(ID)` dispatcher and the two source-proven private carriers only: `manager_after_conversation -> 8_hire_a_manager` and `activities_after_hire -> 5_activities`. Repair-envelope V1 persists the fixed kind plus an optional scaled-time remainder for the activities path, validated against the exact target `SavedData.variables__variables` idempotency witness. Target load clears discarded registry state before `variables.LoadFunction()`, discards a token when its target tutorial is already `available`/`done`, and otherwise re-enters vanilla `_Coroutine(kind)` once. The activities remainder is measured by SNLF's process-local Unity-scaled frame clock; the current two-stage 5-second frontier is folded into one saved total and replayed through the same vanilla generated iterator without a durable phase field. Its Harmony transpiler accepts either the exact two untouched vanilla sites or its own exact already-composed `{0, 1}` resolver sites, making recomposition idempotent while every mixed/partial shape remains unhealthy. A08 remains the sole stale-`MoveNext` execution guard for both tutorial carriers.

- **A13 temporary `GirlsBannedFromAutoTasks` continuity:** SNLF observes the exact private `agency.ReturnGirl(girl)` carrier created only after vanilla's duplicate-ban `Contains(...)` early return, preserves A08 as the sole generated-`MoveNext` execution guard, and rewrites only that carrier's single `WaitForSeconds(60f)` delay operand when restoring a save. Repair-envelope V1 stores stable idol ID + remaining scaled-time seconds measured from `Time.time`; target load clears discarded object membership before idol reconstruction, validates the complete section against the exact `SavedData` roster, rebinds fresh idols, and reuses vanilla `BanGirlFromAutoJobs(...)` to create one expiry each. Duplicate normal ban calls cannot extend expiry, offline wall-clock time never consumes the saved remainder, current-format empty is authoritative, legacy absence remains unknown, and partial restore rollback revokes replacement carriers through A08.

- **A11 graduation-successor introduction continuation:** when the audited `Date_Graduation.Find_Successor(...)` path has already durably hired a successor and successfully schedules its five-second introduction carrier, SNLF snapshots the ordered stable IDs currently represented by `data_girls.new_girls`. Repair-envelope V1 stores only those IDs with an explicit section marker. On target load, SNLF clears stale `new_girls` references before `data_girls.LoadFunction()` reconstructs the roster, validates the section against the exact target `SavedData`, rebinds IDs only to fresh loaded idols, drops/logs runtime-unresolved participants without generation or hire replay, and calls vanilla `Date_Graduation.StartIntroductions()` to recreate the normal five-second presentation delay. Current-format empty is authoritative empty; pre-A11 absence remains unknown. A08 remains the sole `<_StartIntroductions>d__10.MoveNext` stale-carrier guard.

- **N12-D pending ambient `Scenes` regression closure:** source/static regression oracles now jointly cover fresh restart, F9, rollback to an older save, repeated untouched-save loading, already-due and zero-idol jobs, authoritative empty state, malformed-current and legacy-absence handling, missing runtime witnesses, stale agency-loader completion, and exact old-original vs current replacement ownership. The closure pass also hardens Task 40 rollback: every replacement carrier has its A08 execution permission explicitly revoked before best-effort `StopCoroutine`, so even a Unity stop failure cannot later reach `room.assign(...)`. A08 remains the sole bool `MoveNext` suppression owner.

- **N12-C delayed ambient `Scenes` exact restore/reschedule:** SNLF tags each exact private `agency.LoadData()` iterator with its creation `LoadEpoch` and uses only current-epoch terminal `<LoadData>d__80.MoveNext` completion as the restore trigger after the target floor/room graph is rebuilt and rendered. It then validates the complete current-format N12 section against the exact target `SavedData`, resolves fresh loaded rooms and canonical idols, invokes only the private vanilla `Scenes.AssignSceneWithDelay(...)` factory, verifies N12-A registered the exact same semantic job, and starts replacements only after the whole set is ready. Present-malformed sections fail closed; pre-N12-B sections remain unknown; authoritative empty sections schedule nothing. The restore transaction suppresses duplicate attempts for the same target, aborts if `LoadEpoch` changes, and stops/cancels partially admitted replacements on failure. It never calls `GenerateScene`, consumes RNG, or directly calls `room.assign(...)`; A08 remains the sole stale-`MoveNext` execution guard.

- **N12-B pending ambient `Scenes` repair-envelope persistence:** the exact caller-thread `SavedData` freeze now consumes N12-A's detached current-epoch snapshot and emits a section-marked V1 list containing only canonical due game time, scene type, structural `FloorID + room ordinal + room type`, and ordered idol IDs. Every referenced room and idol is witnessed against that exact target DTO before the checkpoint is admitted; zero-idol jobs remain valid because vanilla schedules several zero-girl scene types. Missing/ambiguous target witnesses or non-round-trippable due times fail the save freeze rather than attaching repair state to the wrong checkpoint. N12-B itself still adds **no load rebinding, coroutine restart, `room.assign(...)`, RNG, or second epoch guard**; Task 40 supplies N12-C separately.

- **N12-A delayed ambient `Scenes` semantic-job capture:** the exact private `Scenes.AssignSceneWithDelay(DateTime, agency._room, Scenes.type, List<data_girls.girls>)` factory now registers every already-selected pending scene as detached semantic state: due game time, scene type, canonical unique idol IDs, and structural `FloorID + room ordinal + room type`. The registry keeps no live room/idol references, retires on completion/fault/abandonment, filters stale epochs from snapshots, and relies on the already-implemented A08 `<AssignSceneWithDelay>d__12.MoveNext` `LoadEpoch` guard as the sole discarded-timeline mutation suppressor. Task 38 itself intentionally added **no repair-envelope field or restore/reschedule behavior**. Task 39 supplies the separate N12-B persistence layer above; Task 40 now supplies N12-C exact rebinding/rescheduling.

- **N04 active room `substoryScene` dialogue identity:** repair-envelope V1 now stores the canonical scene dialogue ID against saved `FloorID` + room ordinal + room type for every room whose serialized/live status is exactly `substoryScene`. `agency.GetRoomDataForLoading(RoomData)` late-rebinds the canonical loaded dialogue before agency rendering; current-format sections must cover every active scene room exactly once. Pre-N04 saves do not fabricate the unknowable dialogue identity, and restore never calls `room.assign(...)`, `DrawSpriteScene()`, RNG, or any scene replay path.

- **N03 queued substory `BeforeStart` semantic descriptors:** repair-envelope V1 now stores queue-correlated semantic setup descriptors for the exact 13 supplied vanilla non-null scheduling sites. Capture classifies six bounded setup kinds and persists only queue witnesses, stable idol IDs, mask intent, and the generic-date question code. Load reattaches fresh callbacks after `Substories_Manager.LoadFunction()` rebuilds the target queue; callbacks resolve loaded idols by ID at execution time. Unknown opaque delegates fail checkpoint freeze rather than being silently dropped, and pre-N03 rows receive no fabricated callback.

- **N02 selected random-event business proposal:** repair-envelope V1 now stores the exact pending active-event ordinal/witness plus the selected active-proposal ordinal and complete proposal structural witness. Load clears stale `VARIABLE__BIZ_PROPOSAL` before Event_Manager reconstruction and late-rebinds only after the complete career load proves both ordinal candidates exactly. Pre-N02 saves leave the non-inferable random selection null rather than rerolling or guessing.

- **A29 `FanAppeal_LastSingle` comparison baseline:** repair-envelope V1 now stores an exact nullable seven-axis release-time vector plus source released-single ID. Load clears stale static RAM before singles reconstruction and restores a fresh list after the full career load. Pre-A29 saves use a deterministic target-state synthesis through `RecalcFanAppeal(..., true)` and never substitute the distinct `ReleaseData.FanAppeal` opinion vector.

- **A17 pushed-slot duration baseline:** the generic girl-selector commit is normalized only when its receiver is the exact `Pushes.Girls` list; a changed idol identity resets that slot's `Days` synchronously before vanilla writes the new idol, while unchanged identity and all non-Pushes selectors pass through. No repair-envelope field is added.

- **A16 `Substories_Manager.PreviousNewSubstory` spacing-anchor continuity**:
  - extend repair-envelope V1 with one section-marked exact game-date anchor for repaired saves;
  - restore only after `Substories_Manager.LoadFunction()` rebuilds the target queue/used/actor state;
  - reconstruct pre-A16 saves from target DTO evidence in the audited order: latest queued `too_many_dates*` launch time, complete authoritative saved `Date_LastTriggered` witnesses, ordinary `staticVars.StartDate` when no relevant ID was ever used, then target-save date when occurrence is known but timing is unavailable;
  - never consult live `data_dialogues._dialogue.Date_LastTriggered` during migration, preventing sparse-overlay F9 leakage; and
  - add no dialogue replay, RNG, checkpoint blocker, extra LoadEpoch carrier, or IMDataCore current-state copy.
- **A15 recent-activity recency-clock continuity**:
  - extend repair-envelope V1 with a section-marked rolling 10-day `{activity_type, game_date}` timeline;
  - retain every performance, promotion, and spa occurrence strictly newer than `target_save_time - 10 days`;
  - restore `Stats.Activities_Stats` after vanilla `Stats.LoadFunction()` clears it;
  - derive `Activities.LastHeal` from the newest restored spa row instead of persisting a second field;
  - use an empty timeline plus deterministic old-enough spa anchor for pre-A15 saves whose exact recent history is unrecoverable; and
  - add no activity replay, RNG, checkpoint blocker, extra LoadEpoch carrier, or IMDataCore current-state copy.
- **A02 private project progress-counter continuity**:
  - extend repair-envelope V1 with a section-marked sparse `(owner kind, stable owner ID, parameter enum) -> counter` list;
  - cover singles, shows, concerts, tours, and SSK using the exact private counters consumed by their diminishing-return `Add(...)` implementations;
  - capture from the exact caller-thread SavedData parameter objects before Unity `JsonUtility` drops the private field;
  - restore only after the complete `SaveManager.LoadEvent` fan-out, validating exact reconstructed owner/list identity before any counter is changed;
  - treat missing sparse keys and pre-A02 saves as deterministic counter zero rather than guessing from public `val`/`progress`; and
  - add no RNG replay, project replay, public-progress rewrite, blocker, extra LoadEpoch carrier, or IMDataCore current-state copy.
- **N11 tutorial activity-baseline continuity**:
  - extend repair-envelope V1 with a section marker and explicit nullable semantics for performance/promotion baselines;
  - preserve initialized zero distinctly from null/uninitialized;
  - restore after vanilla `Tutorial.LoadFunction()` resets both private counters to `-1`;
  - validate initialized values against the exact target `Stats__data` aggregate counters before adopting either baseline; and
  - keep pre-N11 saves uninitialized so the first post-migration requirement check seeds naturally from target state.
- **N10 paused-business remaining-minutes continuity**:
  - extend repair-envelope V1 with a section-marked sparse room-locator -> float duration record;
  - capture only finite nonzero hidden remainders against an exact serialized/live floor and room structure;
  - restore only `business_minutes_before_finish` at `GetRoomDataForLoading(RoomData)` after full section validation;
  - keep pre-N10 saves compatible without fabricating an unknowable hidden remainder; and
  - do not select/restart a business proposal, rewrite the intervening job timer, clamp the vanilla float, or add RNG/blocker/epoch behavior.
- **N08 `Event_Overlord.Latest_Event` pacing continuity**:
  - extend repair-envelope V1 with a section-marked exact random-event pacing timestamp;
  - capture the private live anchor from the exact caller-thread SavedData request and reject impossible future values;
  - restore after vanilla `Event_Overlord.LoadFunction()` resets the field to `StartDate`;
  - for pre-N08 saves, conservatively anchor pacing to the serialized target-save game date rather than stale RAM;
  - leave present malformed/unsupported N08 sections fail-closed rather than disguising corruption as legacy; and
  - add no event replay, RNG replay, checkpoint blocker, additional LoadEpoch carrier, or IMDataCore current-state copy.
- **N07 `Activities.Chain` ordered future-intent continuity**:
  - persist the exact ordered activity enum list in section-marked V1 state;
  - clear stale same-process chain state before legacy fallback or exact restore;
  - use the deterministic empty-chain baseline for pre-N07 saves;
  - reuse A08 LoadEpoch invalidation for stale `Chain_Progress_Do` carriers; and
  - add no dispatch-phase token or duplicate activity execution.
- **N05 show last-episode `FanAppeal` continuity**:
  - extend the existing V1 envelope with a section-marked per-show ordered fan-type/ratio vector;
  - bind capture to the exact `shows__Shows` rows and unique live show IDs from the same save request;
  - preserve inner FanAppeal list order because equal-ratio tie behavior can depend on `FanAppeal[0]`;
  - restore only after `Shows.LoadFunction()` rebuilds the show registry and only after exact all-show validation;
  - treat Task-23 V1 envelopes without the N05 section marker as compatible older saves with unknown appeal state; and
  - add no show recalculation, episode replay, RNG behavior, checkpoint blocker, or IMDataCore current-state copy.
- ordered `SavedData`/`GlobalData` transport at the audited concrete callers, with per-path FIFO,
  coordinated reads, file/directory exclusivity, and fail-closed provider health;
- central `CheckpointGate` enforcement for autosave, F5/manual save, and unsafe in-game F9/load;
- monotonic `LoadEpoch` adoption only on successful non-null career loads;
- N06 Event Templates pending-open checkpoint atomicity;
- A09 semantic callback final-frame checkpoint atomicity;
- A10 birthday between-popup checkpoint safety plus bounded same-day-history legacy reconstruction;
- A12 SSK post-payment checkpoint atomicity, including queued result-popup handoff;
- A08 selective stale-carrier invalidation across the complete finite audited carrier surface,
  without `StopAllCoroutines()`; and
- **A03 risky-single `Marketing_Result == 0f` restoration for current-format saves**:
  - patch only private `singles.GetSingleDataForLoading(SinglesData)`;
  - let vanilla reconstruct normally;
  - when the serialized current-format result is authoritative zero, restore that serialized zero
    in a Postfix after vanilla's invalid zero-sentinel reroll branch;
  - leave `Marketing_Result_Status` to vanilla's existing restore path;
  - add no persistence, blocker, epoch carrier, or RNG replay; and
  - conservatively skip older/unknown save versions until the later legacy-migration wave.
- **A05 `data_girls.LastGirlID` allocator restoration**:
  - patch only `data_girls.LoadFunction()` with a Prefix/Postfix pair;
  - snapshot authoritative `data_girls__LastGirlID` from target `SavedData` before idol reconstruction;
  - let vanilla rebuild every saved idol normally through `GenerateGirl(...)`;
  - restore the exact serialized allocator after vanilla overwrites each temporary generated ID with its saved idol ID; and
  - add no persistence, replay, checkpoint blocker, or LoadEpoch carrier.
- **A06 `loans.BankruptcyDanger` reconstruction**:
  - patch only `loans.LoadFunction()` with a Postfix;
  - read the target save's serialized `resources.type.money` row directly rather than depending on LoadEvent subscriber order;
  - set `loans.BankruptcyDanger` directly to `serializedMoney < 0`;
  - preserve the `BankruptcyDate` already restored by vanilla and never call the private deadline-rewriting setter;
  - use the last serialized money row if malformed input contains duplicates, matching vanilla's sequential resource-load behavior; and
  - fail closed when no serialized money row exists instead of borrowing stale process RAM.
- **A07 unfinished-concert `FinishDate` preservation**:
  - patch only `SEvent_Concerts.LoadFunction()` with a caller-local transpiler;
  - replace its single `_concert.Initiate()` call with a stack-compatible wrapper;
  - snapshot the `FinishDate` already reconstructed from `ConcertData`, run vanilla `Initiate()` exactly once, then reapply that date for unfinished concerts;
  - leave finished concerts and unfinished rows with no serialized date on vanilla semantics;
  - restore the date before the concert is added and before `SpecialEvents_Manager.RenderTab()` consumes it as `LaunchDate`; and
  - do not patch `_concert.Initiate()` globally or add any supplemental persistence.
- **A18 trivia zero-counter authoritative restoration**:
  - patch both copied `LoadFunction()` methods (`girls_trivia` and `Graduation_Trivia`) with narrow Prefixes;
  - reset the current authored process-static counter tables to zero before vanilla target-row application;
  - let vanilla continue normally so all serialized nonzero rows are applied exactly as before;
  - explicit serialized zero rows therefore remain authoritative zero across same-process F9/rollback;
  - preserve vanilla unknown-ID handling and avoid duplicating either loader body; and
  - add no supplemental persistence, checkpoint blocker, LoadEpoch carrier, or migration guess.
- **A24 award `her_choice` single-resolution consistency**:
  - patch only `Awards._speech.GetThanks()` at the non-pure semantic boundary;
  - let the first call on each live `her_choice` speech execute vanilla unchanged and cache that returned category;
  - return the same category on every later `GetThanks()` call for that exact `_speech` occurrence;
  - weakly key the cache to the speech object so completed/replaced speeches are not retained as durable state;
  - leave all non-`her_choice` getters on the vanilla path and never call `mainScript.chance(...)` from SNLF; and
  - add no persistence, checkpoint blocker, LoadEpoch carrier, VN-handler rewrite, or historical event storage.
- **A25 immutable `DateTime` graduation-date adjustments**:
  - patch only the five audited owner methods containing the six discarded `Graduation_Date.AddMonths/AddDays` results;
  - cover injury (-6 months), depression (-12 months), best-friend graduation (-12 months), other eligible clique peers (-3 months), accepted business proposal (+7 days), and the rounded dynamic `Graduation_Date_Update()` daily delta;
  - use one fail-closed field-assignment transpiler that preserves the original owner reference, vanilla-computed delta, and `DateTime` arithmetic, then stores the returned value back into `Graduation_Date`;
  - preserve the best-friend exclusion from the three-month clique branch because vanilla control flow remains untouched;
  - do not duplicate the dynamic graduation-retention formula or any audited delta constant in SNLF production logic; and
  - add no persistence, checkpoint blocker, LoadEpoch carrier, RNG behavior, or IMDataCore current-state storage.
- **A26 show next-episode / cancellation-date helper repair**:
  - patch only `Shows._show.GetNextEpisodeDate(DateTime)`;
  - freeze the one discarded initial `_start.AddDays(1.0)` result as the single A26 rewrite site;
  - preserve vanilla's native `DateTime.AddDays(double)` call and one-day constant, changing only the discarded-result `pop` into assignment back to `_start`;
  - leave the weekday loop, no-argument overload, and `GetCancelationDate()` implementation untouched so both next-episode and cancellation-date callers consume the corrected helper semantics;
  - fail closed if the parameter helper no longer contains exactly one discarded `AddDays` result; and
  - add no persistence, checkpoint blocker, LoadEpoch carrier, RNG behavior, duplicate calendar algorithm, or IMDataCore current-state storage.
- **A27 stale `Tutorial.Active_Tutorial_ID` lifecycle / business-success override**:
  - patch the private static `Tutorial.Reset()` boundary so startup/load/reset cannot retain a discarded-timeline active tutorial ID;
  - Prefix `Tutorial_Window.Hide(Action)`, whose supplied stock source has exactly two callers (terminal completion and Quit), so the scalar clears before `Popup.Hide()` can release popup ownership;
  - classify terminal `Done` versus explicit `Ignore`/Quit for diagnostics while intermediate cards retain the live ID because they never call the Hide wrapper;
  - Prefix private `agency._room.DoBusiness()` only to normalize a leftover ID when `Tutorial.Is_Tutorial()` is false before vanilla evaluates its existing `10_business_deals` override;
  - preserve vanilla's ordinary `mainScript.chance(staffer.GetBusinessSuccessChance())` calculation and genuine active-business-tutorial forced success unchanged; and
  - add no persistence, repair envelope, business-result replay, RNG call, checkpoint blocker, LoadEpoch carrier, or IMDataCore current-state storage.

- **A30 best-single temporary nomination binding**:
  - patch only private `Awards.AddTempNomination(_type, girls, _single)` at row creation;
  - when the type is `best_single` and the supplied single is null, call vanilla `Awards.GetNominatedSingle()` exactly once and pass that object into the original row constructor;
  - preserve already-bound rows and all non-best-single nomination paths unchanged;
  - let `Awards.SetWins()` consume the stored `award.Single` normally, preventing nomination/win-resolution candidate rerolls; and
  - add no persistence, repair envelope, checkpoint blocker, LoadEpoch carrier, or custom RNG.
- **A32 group target-audience DTO round-trip repair**:
  - patch only save-side `Groups.GroupData.Set(Groups._group)` with one Postfix;
  - copy exact live `Appeal_Gender`, `Appeal_Hardcoreness`, and `Appeal_Age` values into the vanilla DTO;
  - leave `Fans`, `Points`, member IDs, single IDs, and every other group field on vanilla's copier path;
  - rely on the existing `_group.Set(GroupData)` loader, which already restores all three audience fields; and
  - add no supplemental persistence, legacy provenance guess, checkpoint blocker, LoadEpoch carrier, or IMDataCore current-state copy.

- **N13 / finding #30 `Event_Manager` nonstandard terminal-state repair**:
  - patch `Event_Manager.ConcludeEvent(_reply)` inside the original method body so the no-results early return canonicalizes the consumed live occurrence to `complete` before any Harmony Postfix observer runs;
  - patch only `Event_Manager.<OpenPopup>d__46.MoveNext` at its single automatic `Event_Manager.AddSNS(...)` call so an SNS-only occurrence becomes `complete` immediately after its SNS effect commits;
  - preserve ordinary result-bearing events, popup/SNS presentation, reply effects, liability handling, and vanilla serialization;
  - leave load-time `active -> complete` coercion in place only as vanilla compatibility for already-written pre-fix saves; and
  - add no supplemental persistence, repair-envelope state, checkpoint blocker, LoadEpoch carrier, generic SNS sink patch, or IMDataCore current-state storage.

- **N14 / finding #31 player forced idol-idol breakup coherence**:
  - Prefix only private `Date_Popup.OnClick_ForceBreakup()` before vanilla clears the selected idol's `DatingData`;
  - when the selected idol is `taken_idol`, enumerate only existing `Relationships.RelationshipsData` entries through `GetAllRelationships(...)`;
  - require exactly one concrete two-idol `Dating == true` relationship whose partner is also marked `taken_idol`;
  - delegate the coherent teardown to vanilla `Relationships._relationship.BreakUp()` exactly once, preserving its dual-status, relationship dynamic, stamina, and notification semantics;
  - leave outside-partner breakups on vanilla's original target-only `DatingData` path;
  - fail closed on missing/ambiguous idol relationships without calling the relationship-creating `GetRelationship(...)` helper; and
  - add no supplemental persistence, repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG behavior, or IMDataCore current-state storage.

- **A01 training `Progress_Init` reconstruction**:
  - patch only private `agency.GetRoomDataForLoading(RoomData)` with one Postfix after vanilla restores the room DTO;
  - restrict the repair to serialized `girlTraining` rooms, because that is the only `OnTimeTick()` branch that consumes `Progress_Init`;
  - derive the exact private baseline as `saved Progress - ((target save game time - saved startTime).TotalMinutes / saved CompletionTime)`;
  - read the adopted target save's serialized `staticVars__dateTime` directly instead of depending on LoadEvent subscriber ordering;
  - fail closed for null/malformed rows, non-positive completion time, invalid dates, or a target time before the saved training start;
  - write only `loaded.Progress_Init`, without clamping, rebasing, rescheduling, or mutating the saved progress/timing fields; and
  - add no repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG behavior, or IMDataCore current-state storage.

- **A19 startup `GlobalData` exactly-once delivery**:
  - bracket each concrete `SaveManager.LoadGlobalData()` call with one process-local delivery attempt;
  - passively observe `staticVars.LoadSettings()` so a normal vanilla event delivery satisfies that exact attempt without replay;
  - when deserialization succeeds before a consumer exists, retain one pending reference to the exact current `SaveManager.GlobalData` object;
  - after `staticVars.Awake()` performs the vanilla `LoadGlobalDataEvent += LoadSettings` subscription, claim the pending object once and call only `LoadSettings()` directly;
  - let any newer `LoadGlobalData()` attempt supersede an older undelivered object and validate reference identity before replay;
  - never re-deserialize `global_data.json`, never invoke `LoadGlobalDataEvent` from SNLF, and never persist the latch; and
  - keep ordered GlobalData file transport and consumer-readiness repair as separate layers.


- **N01 / finding #1 relationship `Dynamic` continuity**:
  - activate the planned versioned `__cosmo_save_n_load_fixes` root for repair-dependent `SavedData` checkpoints;
  - capture a V1 `relationship_dynamics` section keyed by deterministic sorted idol-ID pairs and bind every record to the exact vanilla relationship rows in the same save request;
  - freeze vanilla `SavedData` JSON and the repair envelope synchronously on the caller thread, inject them into one physical payload, and never fall back to writer-thread serialization for a repair-dependent checkpoint;
  - on read, extract and remove the SNLF root before `JsonUtility` reconstructs vanilla `SavedData`, then weakly associate the validated envelope with that exact DTO instance;
  - restore `Relationships._relationship.Dynamic` only after vanilla `Relationships.LoadFunction()` has reconstructed the complete relationship set, and only after the envelope/current unordered-pair sets validate one-to-one;
  - treat envelope absence as a legacy/pre-fix save whose exact historical Dynamic values are unavailable, and treat a present invalid envelope as an explicit fail-closed condition rather than an exact empty state; and
  - add no IMDataCore current-state copy, no relationship creation, no RNG replay, and no partial-exact restoration.

- **N07 / finding #7 `Activities.Chain` ordered future-intent continuity**:
  - extend the active V1 repair envelope with a section-marked exact ordered `Activity._type` list;
  - capture the static chain from the same caller-thread SavedData request without sorting or adding a separate dispatch-phase token;
  - after vanilla `Activities.LoadFunction()`, clear discarded-timeline static chain state and atomically restore a fully validated Task-25+ section;
  - for pre-N07 saves, use the deterministic fresh-process empty-chain baseline rather than leaking same-process F9 state;
  - rely on the already-existing A08 LoadEpoch guard to suppress stale `<Chain_Progress_Do>d__53` carriers from the discarded timeline;
  - leave vanilla responsible for scheduling the restored head on the next expired-activity tick and for removing index zero only after the activity call commits; and
  - add no new persistence channel, checkpoint blocker, RNG behavior, activity replay, or IMDataCore current-state copy.


- **N09 / finding #9 paused-training `girl_paused` continuity**:
  - extend V1 with a section-marked list keyed by serialized `FloorID` + room ordinal + room type and stable displaced idol ID;
  - capture only when serialized/live agency floor structures match exactly and the normal room `girl` association is already cleared;
  - clear stale same-process `girl_paused` references at the agency load boundary;
  - late-rebind the loaded idol during `GetRoomDataForLoading(RoomData)` without invoking `ResumeTraining()` or `assign(...)`;
  - treat pre-N09 saves as unknown rather than fabricating a displaced idol; and
  - never use runtime `_room.id`, add RNG behavior, replay work, or mirror current state into IMDataCore.

The embedded transport still does **not** Harmony-patch a constructed `DataSaver<T>` generic
method, and the cumulative source still has no old SWOF/old IMDataCore compatibility dependency.

### Task-27 boundary

Wave 3 is active through N01, N05, N07, N08, and N09. The V1 envelope remains embedded in the same frozen `SavedData` JSON under `__cosmo_save_n_load_fixes`; section markers distinguish later exact repair families from older V1 checkpoints that never captured them. N07 adds ordered `Activities.Chain` future intent while reusing A08 LoadEpoch invalidation for stale delayed carriers. Current-runtime validation in this environment remains source/static only. The plan's no-mod unknown-root compatibility probe still requires the actual Idol Manager Unity/Mono runtime before a release claim is appropriate.

The four `CheckpointGate` producer families remain exactly:

- `EventTemplatesPendingOpen`;
- `SemanticCallbackFinalFrame`;
- `BirthdayQueueBetweenPopups`;
- `SskPostPaymentLaunch`.

### Source/static tests

With the supplied decompile available, run all `tests/Test-*Source.py` files with
`--source-root "/path/to/IM Source Code"`, then all `tests/Test-*Contract.py` files and
`tests/Test-PatchManifestSync.py`. The cumulative suite is expected to pass as one unit.

The normal repository build requires the private game/Harmony/Unity reference DLLs and a
.NET/MSBuild toolchain. This development stream is intentionally source/static validated in the
current environment.

## Failed-overwrite stale-load guard

The ordered SavedData transport now records the outcome of the newest same-path save request from caller-thread repair-envelope freeze through the physical writer. If that newest request fails before queue admission or fails while writing the file, a subsequent SavedData read in the same process returns no save instead of silently loading the older bytes still present on disk. A later successful save to the path clears the failed-attempt state. This prevents a rapid overwrite-then-load sequence from pairing an old vanilla checkpoint with a newer supplemental sidecar.
## IMDataCore coordinated checkpoint outcome

Transport API v3 retains the older hard-witness requirement call for compatibility, but current IM Data Core no longer uses that call as the normal save contract. IMDC still publishes the exact SHA-256 witness only after its sidecar generation is durable. If IMDC participates but fails before that boundary, SNLF may still complete the vanilla save and records `imdc_persistence_state = 2` with no witness. A later load treats that as an explicitly failed modern IMDC companion, not as a pre-bridge legacy envelope eligible for unique-candidate sidecar adoption. A successful companion records `imdc_persistence_state = 1` together with the canonical witness; old/no-companion envelopes remain state `0`. The failed-overwrite stale-read guard remains a second safety layer for failures of SNLF's own physical vanilla write.

## Save progress notifications

When SNLF owns the ordered `SavedData` transport, every actual write emits `Game saving started`. `Game saving completed.` follows only after the writer succeeds and participating IM Data Core persistence reaches a terminal result. A failed sidecar gets a separate red notification; a failed vanilla write emits `Game saving failed.` The game's notification prefab/history is used directly so forced pauses, the Other category filter, and a busy gameplay notification queue cannot hide save status. Start and completion are displayed on separate frames, and each path/attempt is tracked independently.

Completion notifications are marshalled through a `mainScript.Update` postfix so worker-thread transport results never call Unity notification UI directly. SNLF does not intercept or defer `PopupManager.Close` for this notification-based UX.
