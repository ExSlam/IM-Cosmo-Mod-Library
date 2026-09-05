# Changelog

## 3.4.33

- Activated sidecar v6 / journal v3 as the atomic current persistence generation in Wave 5 Task 6 and converted the static staging guards into current-generation anti-rollback guards.
- Completed a combined IMDataCore/SNLF/SWOF correctness pass: coordinated loads no longer outrun an active write after a fixed timeout, destructive deletion fails closed under recognized-but-unhealthy transport state, transport path keys require canonical absolute paths, and SWOF verifies SNLF assembly identity before reflection delegation.
- Synchronized SWOF public version identity to 1.4.5 and restored its advertised coexistence source-qualification test. Task 6 adds no new backward-compatibility feature; fresh build and runtime/fault qualification of this exact candidate remain release gates.

- Closed IMDataCore Wave 5 Task 4 as FINAL-STATIC companion transport reference validation against the supplied Save n Load Fixes and Save Write Ordering Fix source/Release-DLL packages.
- Hardened direct SNLF trust with API-version and exact owner validation, and current SWOF trust with effective-owner validation while preserving legacy SavedData-only SWOF fallback.
- Recorded companion reference SHA-256 fingerprints; live sidecar v5 / journal v2 and package version 3.4.33 remain unchanged.

- Closed IMDataCore Wave 5 Task 3 as FINAL-STATIC v6/v3 release preflight: canonical identity and structured coverage now share one atomic 6/3 activation predicate, and physical initialization rejects split 6/2 or 5/3 live-format edits.
- Added machine-checked static cutover prerequisites for migration, staged schemas, public coverage/identity/history/money APIs, the #1-#154 registry, ordered transport cooperation, and repair-owned current-state nonduplication; live persistence remains 5/2.

- Closed IMDataCore Wave 5 Task 2 as FINAL-STATIC ordered transport cooperation: authoritative healthy Save n Load Fixes is now preferred directly, with healthy Save Write Ordering Fix as fallback and standalone IMDC as the final safety path.
- Unified save-snapshot optimization and save-directory deletion behind one reflection-only effective-provider resolver; deletion now acquires at most one effective-owner directory lease and never independently coordinates both transport mods.
- Closed IMDataCore Wave 5 Task 1 as FINAL-STATIC regression closure for audit contracts #1-#154, with a machine-readable evidence registry and dedicated aggregate regressions #147-#154; runtime v6/v3 activation remains separately gated.
- Closed IMDataCore Wave 4 Task 4, cursor-complete money-detail paginator finding #68, as a FINAL-STATIC query-product milestone without changing the live v5/v2 persistence gate.
- Added `TryReadMoneyTransactionsPage(...)` across the preferred, reflection-friendly, and uppercase facades, preserving the existing date-bucket range while continuing by durable EventId/shared sequence inside dense days.
- Made money cursors exact active-branch/range witnesses: discarded F9/load cursors, changed-range cursors, and namespaced lookalikes fail closed; restart/compaction preserves surviving EventIds.
- Kept the legacy capped money detail reader and uncapped aggregate totals unchanged. Wave 4 public query product #61-#68 is now FINAL-STATIC complete.
- Closed IMDataCore Wave 4 Task 3, canonical career-wide durable-history paginator finding #67, as a FINAL-STATIC query-product milestone without changing the live v5/v2 persistence gate.
- Added `TryReadHistoryPage(...)` across the preferred, reflection-friendly, and uppercase facades, paging the selected branch directly by stable EventId/shared sequence and returning each retained physical occurrence exactly once.
- Kept `TryReadEventsForIdolPage(...)` unchanged as the participant-expanded idol + global compatibility view; discarded-branch canonical cursors fail closed after F9/load.
- Closed IMDataCore Wave 4 Task 2, public current-generation identity resolver finding #66, as a FINAL-STATIC query-product milestone without changing the live v5/v2 persistence gate.
- Hardened legacy candidate resolution so redundant overlapping exact proofs for the same canonical generation remain `Exact`, while conflicting exact targets remain `Ambiguous`.
- Added canonical-kind filtering so malformed cross-kind candidate records cannot be returned under the requested identity family.
- Implemented IMDataCore Wave 3 Task 3, namespace bootstrap/order/reentrancy findings #53-#55, without changing live persistence formats or adding event schemas.
- Removed the one-shot `IsReady()` prerequisite from the consumer registration template and onboarding guidance; `TryRegisterNamespace(...)` now directly owns safe runtime initialization, with bounded retry guidance when registration itself fails.
- Hardened `business.CancelContract(...)` history with contained-before plus exact-absent-after proof and immutable pre-mutation identity/payload snapshots, preventing detached, repeated, stale, or throwing calls from fabricating `contract_cancelled` terminals.
- Kept liability money observation independent from contract lifecycle proof, so a real vanilla deduction can still be ledgered even when no contract terminal is valid.
- Replaced the audited one-slot ambient bridges with nesting-safe thread-local stacks/depth frames for activity earnings, concert crisis choices, scandal parameter mutation, blackmail trigger results, and money-ledger attribution.
- Made money attribution exception-safe by installing owned frames before detail construction and restoring only from the owning Harmony finalizer; transient show-profit frames auto-retire at their exact resource mutation.
- Added two Wave-3 Task-3 source/fixture regressions, raising the cumulative deterministic suite from 50 to 52 tests.
- Kept the Event Catalog at 173 queryable built-in event types across 41 domains because Task 3 hardens semantics without adding a new public event type or payload field.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.33.

## 3.4.32

- Implemented IMDataCore Wave 3 Task 2, historical reference/lifetime findings #48-#52, without advancing live persistence formats.
- Added money-detail reference schema version 1 with explicit knownness and durable/stable references for staff, facilities, projects, members, cafe dishes/workers, concert setlists, and active business contracts.
- Kept legacy money-detail rows readable as reference schema 0 and normalized unknown legacy references to invalid/empty values rather than exposing misleading zero IDs.
- Added `loan_matured` from the authoritative weekly loan transition, requiring an exact active-before to inactive-after transition while the same loan remains in the vanilla loan collection.
- Preserved the weekly processing date separately from the loan end date so delayed maturity processing is represented truthfully.
- Preserved linked election identity for `single_cancelled` by snapshotting the election parent before `SingleInDevelopmentButton.OnCancel()` clears `ReleaseSingle`, with exact-reference, nested, and exception-safe transient context.
- Added two Wave-3 Task-2 source/fixture regressions, raising the cumulative deterministic suite from 48 to 50 tests.
- Regenerated the Event Catalog to 173 queryable built-in event types across 41 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.32.

## 3.4.31

- Implemented IMDataCore Wave 3 Task 1, payload/timing semantics findings #41-#47, without adding current-state persistence.
- Split single release appeal semantics: `single_fan_appeal_*` now records the true release-time `single.FanAppeal`, while new `single_opinion_fan_appeal_*` fields preserve `ReleaseData.FanAppeal` separately.
- Moved `contract_window_opened` to the authoritative `business.SetProposal(...)` popup/presentation boundary while preserving the proposal `p:<guid>` occurrence correlation.
- Replaced new queue-time `substory_started` capture with `substory_queued` and added `substory_presented` at the actual dialogue/scene presentation seam; the legacy event constant remains readable for old rows.
- Added controlled `idol_departure_cause` / `idol_departure_source` and `status_cause` / `status_source_kind` provenance from audited caller scopes, with explicit `unknown` fallback and no state repair.
- Added structured task-constraint ID/title payload fields for single genre/lyrics and show genre/medium.
- Split blackmail history into correlated enqueue, trigger, and dequeue checkpoints under history-only `bm:<guid>` occurrences; reward values are explicitly planned at trigger time and never mislabeled as already applied.
- Added two Wave-3 Task-1 source/fixture regressions, raising the cumulative deterministic suite from 46 to 48 tests.
- Regenerated the Event Catalog to 172 queryable built-in event types across 41 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.31.

## 3.4.30

- Implemented IMDataCore Wave 2 Task 7, the remaining lifecycle/history closure group (#32-#40), without taking ownership of vanilla current-state restoration.
- Fixed clique-join leader history by snapshotting the previous leader before `_clique.AddMember(...)` can run `UpdateLeader()`, while suppressing no-op joins.
- Added shared `room_work_assigned` history for idol training and medical treatment, plus direct training/treatment terminal capture that deduplicates nested `CancelJob()` cleanup and keeps natural treatment completion distinct from cancellation.
- Added `activity_level_up`, `summer_games_objective_activated`, and `tour_country_level_up` milestones at their authoritative transition seams.
- Added historical observation of the audited chapter-5/chapter-6 VN direct idol-status restorations through the existing status-transition event family.
- Added bounded semantic capture scopes for the audited nested Harmony chronology chains, ensuring prerequisite outer rows receive sequence numbers before nested child rows without globally sorting unrelated captures.
- Changed `group_created` / `group_disbanded` persistence to one shared physical row with participant indexing instead of one duplicate row per member.
- Added `single_chart_result` so chart backfill/resolution is a distinct milestone and can never re-emit `single_released`.
- Added two Wave-2 Task-7 source/fixture regressions, raising the cumulative deterministic suite from 44 to 46 tests.
- Regenerated the Event Catalog to 169 queryable built-in event types across 41 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.30.

## 3.4.29

- Implemented IMDataCore Wave 2 Task 6, the post-audit split event/relationship history group (#1, #2, #6, #30, #31), as historical-only capture.
- Added `idol_relationship_created` at the authoritative `Relationships.GetRelationship(...)` birth seam, preserving the initial randomized relationship `Dynamic` while suppressing load/bootstrap reconstruction.
- Added history-only `re:<guid>` random-event occurrence correlation and selected business-contract context using the existing durable contract history identity resolver.
- Added active-branch recovery for the newest still-open random-event occurrence after transient load/F9 reset without restoring Event_Manager current state.
- Added `template_event_presented` / `template_event_concluded` under history-only `te:<guid>` occurrences. Exact displayed reply variants are observed from `Event_ReplyButton.Set(...)`; IMDC never calls the random `GetReplies()` selector itself.
- Added SNS-only `random_event_concluded` capture from the audited `Event_Manager.<OpenPopup>d__46.MoveNext` terminal path, with no state mutation and explicit unknown resource-delta / unapplied-reply-effect semantics.
- Added one semantic `player_forced_breakup` row around `Date_Popup.OnClick_ForceBreakup()`, ordered before/after Save n Load Fixes when present but performing no repair itself.
- Added two Wave-2 Task-6 source/fixture regressions, raising the cumulative deterministic suite from 42 to 44 tests.
- Regenerated the Event Catalog to 164 queryable built-in event types across 41 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.29.

## 3.4.28

- Implemented IMDataCore Wave 2 Task 5, the post-audit tasks/substories/scenes group (#27, #28, #29), as historical-only capture.
- Kept finding #27 on its existing authoritative `tasks.GenerateTask(...)` seam from Wave 1: generated-task birth still emits exactly one `task_added` and Task 5 adds no duplicate producer.
- Added `task_unfulfilled` at `tasks._task.Unfulfill()`, gated strictly on `Fulfilled: true -> false` and routed through the existing task history identity/payload resolver.
- Added history-only `ss:<guid>` occurrence correlation for scene-type substories, allocated at exact queue insertion and retained through vanilla scene presentation into the owning dance-studio room.
- Added one `substory_completed` terminal at `agency._room.SubstoryFinish()` from a pre-clear scene snapshot, preserving the same scene occurrence ID and actor/idol context without fabricating a presentation event.
- Added active-branch recovery of the newest still-open scene occurrence after load/F9 clears transient object maps, so duplicate runs of one reusable scene definition remain distinguishable while IMDC still owns no gameplay current-state restoration.
- Added two Wave-2 Task-5 source/fixture regressions, raising the cumulative deterministic suite from 40 to 42 tests.
- Regenerated the Event Catalog to 160 queryable built-in event types across 40 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.28.

## 3.4.27

- Implemented IMDataCore Wave 2 Task 4, the post-audit shows/singles/awards group (#5, #21, #22, #26), as historical-only capture.
- Extended `show_episode_released` with the exact completed-episode `show_fan_appeal_summary` and canonical 12-segment `show_fan_segment_audience_summary` on both ordinary and post-mod canonical producers.
- Added `show_cancellation_scheduled` for real deferred `ToCancel: false -> true` transitions and `show_cancellation_withdrawn` for real `true -> false` withdrawals, while keeping immediate/consumed terminal cancellation as the existing `show_cancelled` row only.
- Added `award_speech_delivered` at the actual solo/group thanks-dialogue boundary. IMDC passively observes the first game-requested `Awards._speech.GetThanks()` result, records speech giver/configured/resolved thanks and target identities, and never rolls `her_choice` itself.
- Added weak per-speech deduplication so repeated vanilla getter evaluations produce one historical speech occurrence; this composes with Save n Load Fixes' first-resolution cache without API coupling.
- Added two Wave-2 Task-4 source/fixture regressions, raising the cumulative deterministic suite from 38 to 40 tests.
- Regenerated the Event Catalog to 159 queryable built-in event types across 40 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.27.

## 3.4.26

- Implemented IMDataCore Wave 2 Task 3, the post-audit groups/social/rivals group (#20, #24, #25), as historical-only capture.
- Added `player_bullying_intervention` at `Date_Influence.Bullying_Text(...)`, preserving acting/target idol, stopped-member before/after/delta, partial/full outcome, influence cost, and correlation to the existing bullying episode identity without rerunning RNG.
- Added `clique_created` at the authoritative private `Relationships.StartNewClique(...)` seam, preserving the founder/initial leader without fabricating a first `clique_joined` row.
- Added sparse `rival_group_created` / `rival_group_retired` rows to the existing monthly rival snapshot. Diffing uses exact object references so sorting cannot generate false lifecycle rows; baseline groups remain baseline semantics.
- Kept `rival_monthly_recalculated` exactly one aggregate row per observed monthly recalculation and kept per-group identity/state in separate `rival_group` rows keyed by vanilla's persistent ID.
- Added two Wave-2 Task-3 source/fixture regressions, raising the cumulative deterministic suite from 36 to 38 tests.
- Regenerated the Event Catalog to 156 queryable built-in event types across 40 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.26.

## 3.4.25

- Implemented IMDataCore Wave 2 Task 2, the post-audit business/loan/contracts group (#15, #16, #23), as historical-only capture.
- Added proposal occurrence history at the authoritative `business.SetProposal(...)` presentation boundary. One runtime `p:<guid>` now correlates `business_proposal_generated` with exactly one accepted or declined terminal when the normal vanilla path is observed.
- Added complete accepted/declined proposal snapshots: ordered candidate slate, selected idol, final negotiation known/success state, coefficient and attempt count, raw/effective proposal values, stamina/liability/duration, staff and presentation context. Capture never calls negotiation/generation RNG.
- Correlated continuing accepted proposals to the exact `EntityId` selected by the existing `contract_accepted` history path, preserving live-v5 legacy contract keys and staged-v6 `g:` generation semantics without creating a second contract identity system.
- Added `loan_cancelled` for developing room-loan cancellation at `agency._room.CancelJob()`, gated by exact reference membership proof (`contained before -> absent after`) and keyed to the same vanilla loan ID.
- Extended loan lifecycle payloads with `loan_contained_before` / `loan_contained_after`, keeping payoff semantically distinct: `loan_paid_off` remains active-before -> inactive-after while the loan object stays retained in `loans.Loans`.
- Added load/F9 cleanup for transient proposal occurrence correlation only; no proposal current-state persistence or new checkpoint field was introduced.
- Added two Wave-2 source/fixture regressions, raising the cumulative deterministic suite from 34 to 36 tests; all 36 pass in this package.
- Regenerated the Event Catalog to 152 queryable built-in event types across 40 domains plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.25.

## 3.4.24

- Started IMDataCore Wave 2 with the plan's people/auditions/hire/date group (#14 and #17-#19), keeping all work historical-only.
- Added `a:<guid>` audition occurrence correlation across start, cost, candidate-generation, and completion rows, plus occurrence-local candidate IDs and terminal hired/rejected outcomes.
- Added `audition_candidates_generated` at the private authoritative `Auditions.GenerateGirls(...)` post-generation seam and `audition_completed` after `Popup_Audition.Close()` returns. The start payload now labels its pre-generation count as `candidate_count_at_start`; final slate size belongs to the generation/completion rows.
- Enriched `idol_hired` with source provenance and a hire-time generated profile. Parameter logging reads `_val` and `potential` directly and records `potential_materialized`; it never calls non-pure `GetPotential()` merely to populate history.
- Added `player_generic_date_presented` / `player_generic_date_completed` under one `d:<guid>` occurrence. The completed row is emitted only after vanilla's dialogue `dating/add_points` action has committed.
- Added `player_flirt_outcome` after `Date_Flirt.DoFlirt(...)`, taking the semantic result from the already-applied `DatingData.Previous_Attempt` and preserving discovery/failure categories without dialogue text or RNG interception.
- Added Wave-2 source/fixture regressions, raising the cumulative deterministic suite from 32 to 34 tests.
- Regenerated the Event Catalog to 148 queryable built-in event types plus the same three internal transient streams.
- Kept live sidecar format 5 / journal format 2 and bumped project/mod metadata to 3.4.24.

## 3.4.23

- Implemented the shared Area-#12 D06-D10 identity-compatibility layer after all five family-specific Wave-1 identity contracts.
- Added checkpoint-owned `IdentityCandidates` metadata beside the v6 identity-binding collection. Candidate links preserve legacy coarse keys without rewriting legacy event rows or manufacturing global one-to-one aliases.
- Added deterministic migration-boundary adoption for contract `g:`, clique `q:`, bullying episode `b:`, and generated-task `t:` identities. IDs are SHA-256-derived from the exact checkpoint stamp, family serialized locator/witness, parent/child locator where applicable, and a migration-version salt. No birth event is synthesized.
- Added branch-safe compatibility state replacement so exact F9/checkpoint selection clears discarded runtime bindings/candidates before native rebind or deterministic legacy adoption. Failed vanilla-load finalizers also clear the aborted branch candidate/adoption state instead of leaving transient compatibility evidence behind.
- Added explicit candidate quality through `Unresolved`, `Ambiguous`, and `Exact`. Multiple canonical candidates remain a valid result; a bounded exact alias is honored only inside its independently proven sequence interval.
- Added the read-only #66 identity resolver to `IMDataCoreApi`, `IMDataCoreInteropApi`, and `IMDataCoreAPI`. Stable locator descriptors are used instead of exposing CLR object references.
- Kept live sidecar v5 / journal v2 truthful: room/theater/cafe and SSK/tour room-work can resolve from already-durable v5 ingredients, while the four opaque-generation families and durable legacy-candidate lookup remain unresolved until sidecar v6 is live.
- Added shared-identity source/fixture regressions and made `IdentityCandidates` explicit on every full v6 checkpoint fixture. The cumulative deterministic suite now contains 32 tests.
- Bumped project and mod metadata to 3.4.23.

## 3.4.22

- Implemented IMDataCore Wave 1 Identity 5 only: room-work SSK/tour owner namespace identity, completing the five family-specific Wave-1 identity contracts.
- Replaced the ambiguous room-work child owner token `event` with `ssk` for SSK production and `tour` for tour production on both completion and `CancelJob()` cancellation snapshots.
- Preserved the existing durable `g:<room-generation>` prefix, producing canonical `g:<room>:ssk:<SSK.ID>` versus `g:<room>:tour:<tour.ID>` streams without adding another random generation.
- Kept `room_work_kind` semantically aligned with the same `ssk` / `tour` owner token so payload and timeline identity cannot disagree.
- Added no checkpoint binding family: vanilla already persists SSK and tour IDs in independent ID domains, and the room-generation prefix is already checkpointed.
- Preserved all historical `g:<room>:event:<N>` rows unchanged. Their payload lacks a source-authoritative discriminator for safe bulk reclassification, so ambiguous SSK/tour candidates remain for the shared Area-#12 compatibility layer rather than being guessed during migration.
- Added Task-15 source/fixture regressions for equal SSK/tour numeric IDs in one room generation, completion/cancellation stream consistency, and the legacy two-candidate ambiguity case. The cumulative deterministic suite now contains 30 tests.
- Kept normal runtime persistence at sidecar v5 / journal v2. Unlike the four opaque-generation families, this namespace split is safe to emit immediately on v5 because all identity components are already durably persisted.
- Bumped project and mod metadata to 3.4.22.

## 3.4.21

- Implemented IMDataCore Wave 1 Identity 4 only: durable generated non-custom task occurrence identity, coordinated with finding #27's missing generated-task birth history.
- Added a `tasks.GenerateTask(...)` Prefix/Postfix seam that proves exactly one new active-task reference, allocates one opaque `t:<guid>` occurrence generation, and emits exactly one real `task_added` row for that generated task.
- Kept custom/scripted task `Custom` IDs on their existing definition-stream semantics; only non-custom generated occurrences receive `t:` generations.
- Routed generated-task completion/failure/done/graduation-removal history through the generation-aware resolver when v6 is live and retire the runtime generation only after terminal history is enqueued/flushed and the object is absent.
- Added exact staged-v6 task bindings by serialized `tasks__TaskData` ordinal plus SHA-256 over all 13 saved `TaskData` fields. Rebinding after `tasks.LoadFunction` requires ordinal, witness, and reconstructed-row equality.
- Preserved the legacy `type|goal|girl` key only as candidate metadata. The Task-14 fixture proves separate occurrences can share the legacy key, and can even reproduce an identical full saved row/witness, while retaining distinct canonical `t:` generations.
- Strengthened v6 validation so generated-task bindings cannot carry parent/child locators.
- Emitted finding #27's generated-task `task_added` row on the current v5 runtime using the existing coarse task key, while keeping canonical `t:` EntityIds gated until sidecar v6 can durably rebind them.
- Added Task-14 source/fixture regressions. The cumulative deterministic suite now contains 28 tests.
- Kept normal runtime persistence at sidecar v5 / journal v2; room-work SSK/tour namespace identity remains Wave 1 Identity 5.
- Bumped project and mod metadata to 3.4.21.

## 3.4.20

- Implemented IMDataCore Wave 1 Identity 3 only: durable bullying-episode generation identity.
- Added one opaque `b:<guid>` per real not-bullied -> bullied interval. Clique generation is retained only as parent correlation, so a leader change cannot split one continuing episode and a later recurrence against the same target receives a fresh occurrence ID.
- Carried canonical episode/parent identity through pre-stop snapshots used by `_clique.StopBullying`, relationship-level stop flows, and recursive clique `Quit`, then retired the episode only after terminal history is enqueued.
- Added staged v6 child bindings for active bullied targets using exact `Relationships__Cliques` ordinal, parent `q:` generation, `bullied_target:<idolId>` locator, and target-specific SHA-256 witness. Rebinding requires the parent clique binding and reconstructed target to match.
- Strengthened v6 checkpoint validation so bullying children require a valid parent clique generation and valid bullied-target locator, and duplicate serialized locators fail closed.
- Preserved old `leader|target` values only as legacy candidate keys; leadership succession may add multiple candidates to one episode without changing the canonical `b:` generation.
- Added optional v6-only `clique_generation_id` correlation to bullying payload serialization; live v5 payloads remain wire-shape compatible because the field is omitted while the canonical identity gate is disabled.
- Added Task-13 source/fixture regressions for leader succession, terminal retirement, exact parent/child rebinding, and same-clique/same-target episode recurrence. The cumulative deterministic suite now contains 26 tests.
- Kept normal runtime persistence at sidecar v5 / journal v2; generated-task and room-work identity remain later Wave-1 tasks.
- Bumped project and mod metadata to 3.4.20.

## 3.4.19

- Implemented IMDataCore Wave 1 Identity 2: stable clique-generation identity for the staged sidecar-v6 checkpoint contract.
- Added an opaque `q:<guid>` generation at the authoritative private `Relationships.StartNewClique` birth seam; mutable leader/member state is no longer the canonical forward identity once v6 is live.
- Routed clique join/leave EntityId selection through a generation-aware resolver while preserving the current sorted-member signature as payload/legacy candidate metadata.
- Added a quit snapshot generation witness so recursive `Relationships._clique.Quit` retains one clique generation through nested last-member callbacks; the runtime binding retires only after leave and induced bullying terminal capture completes and the clique is physically absent.
- Added exact checkpoint clique bindings keyed by serialized `Relationships__Cliques` ordinal plus SHA-256 over the complete saved clique row: leader, ordered members, known state, bullied/known-bullied lists, and stopped-bullying rows.
- Added post-`Relationships.LoadFunction` clique rebinding requiring ordinal, fingerprint, and reconstructed-row equality before a generation is attached to a fresh `_clique` object.
- Preserved old sorted-member signatures only in `LegacyCandidateKeys`; equal signatures or equal structural witnesses across different clique lifetimes never establish a global exact alias.
- Combined contract and clique checkpoint binding projection while keeping bullying/generated-task identity work deferred to their own Wave-1 tasks.
- Kept canonical clique generation emission gated until sidecar v6 becomes the live writer; normal 3.4.19 persistence remains sidecar v5 / journal v2 and live clique history therefore retains legacy signatures.
- Added Task-12 clique identity source/fixture regressions, bringing the cumulative deterministic suite to 24 tests.
- Bumped project and mod metadata to 3.4.19.

## 3.4.18

- Implemented IMDataCore Wave 1 Identity 1: opaque contract-generation identity for the staged sidecar-v6 checkpoint contract.
- `business.Accept` now reserves a generation before its nested `AddActiveProposal` call, carries it in a nesting-safe pending frame and `ContractAcceptedSnapshot`, and clears the frame on both normal and exceptional exits.
- `AddActiveProposal` binds the newly inserted `active_proposal` to the reserved generation; direct/modded insertion without an enclosing acceptance receives an independent fresh generation.
- Added contract checkpoint bindings keyed by serialized `business__ActiveProposalsData` ordinal plus a SHA-256 witness over exactly the fields vanilla saves. Two byte-identical rows may therefore retain separate generations by ordinal.
- Contract bindings preserve the old `idol|type|end-day` identity only in `LegacyCandidateKeys`; no one-to-one legacy alias is created.
- Added exact-load contract rebinding after `business.LoadFunction`, with ordinal + witness + reconstructed-row verification before a generation is attached to a live object.
- Routed acceptance, activation, weekly accrual, cancellation, natural completion, and break-event entity selection through the generation-aware resolver, with terminal retirement after the terminal row is enqueued.
- Fixed the cumulative staged-v6 checkpoint clone path so `IdentityBindingsVersion`, completeness, and binding records survive generic storage snapshots instead of being dropped.
- Kept canonical generation emission gated until sidecar v6 becomes the live writer; normal 3.4.18 persistence remains sidecar v5 / journal v2 and therefore continues emitting legacy contract EntityIds rather than creating non-durable half-upgraded history.
- Added Task-11 contract identity source/fixture regressions, bringing the cumulative deterministic suite to 22 tests.
- Corrected the historical Task-9 changelog heading/version from 3.4.17 to its actual cumulative version 3.4.16.
- Bumped project and mod metadata to 3.4.18.

## 3.4.17

- Implemented IMDataCore Wave 0 Task 10: deterministic migration provenance and fail-closed downgrade behavior, completing the staged Wave-0 v6/v3 storage foundation.
- Added required sidecar-v6 `MigrationProvenance` with explicit `native_v6` versus `legacy_migration` origin, source/target storage generations, normalized source-document SHA-256, source sequence high watermark, and deterministic save-scope-bound conversion identity.
- Kept migration provenance non-rewinding and validated it against coverage origin: a migrated generation cannot masquerade as `CareerStart` or `LateAdoption`; repaired post-migration coverage begins only through a later exact `LegacyResume` boundary.
- Added a typed unsupported-sidecar signal so normal v5 activation treats an unsupported primary generation as authoritative/write-protected instead of falling through to an older `.imdc.bak` and potentially healing newer semantics backward.
- Extended the same write-protection decision to an unsupported journal whose base SHA-256 matches the candidate compact base. Finding #58 remains unchanged for wrong-hash unsupported journals, which are stale suffixes rather than authoritative state.
- Added Task-10 migration provenance and downgrade fixtures/source contracts and refreshed all full-v6 fixtures with required provenance. The cumulative deterministic suite now contains 20 tests.
- Kept normal runtime persistence at sidecar v5 / journal v2; this task completes the storage foundation but does not silently enable v6/v3 publication.
- Bumped project and mod metadata to 3.4.17.

## 3.4.16

- Implemented IMDataCore Wave 0 Task 9: finding #63 bounded `HistoricalBaselineAssertions` branch carrier plus public-quality staging.
- Added required sidecar-v6 `HistoricalBaselineAssertions`, allow-listed only to `group_target_audience_origin`, with deterministic semantic IDs, shared sequences, schema version, source provenance, and exact checkpoint anchors for load/adoption-derived evidence.
- Added strict `Exact` / `Ambiguous` / `Unknown` evidence rules. Exact requires one independently proven candidate on all three axes; ambiguous/unknown assertions cannot select an authoritative trio; no-evidence is represented by no assertion.
- Kept v1-v5 migration conservative: the new collection is initialized empty and migration performs no group-appeal backfill, point-pattern inference, or current-value mirroring.
- Implemented the previously reserved journal-v3 `HISTORICAL_BASELINE_ASSERTION` row codec, count path, replay buffering, shared-sequence validation, and atomic document validation. Every frozen Task-3 semantic row family now has a codec.
- Added staged public `IMDataCoreHistoricalBaselineQuality` and `IMDataCoreHistoricalBaselineAssertion` types without claiming a live retrieval method before the v6/v3 cutover.
- Added Task-9 exact/ambiguous/unknown and negative fixtures plus cumulative source/contract regressions; live persistence remains sidecar v5 / journal v2.
- Bumped project and mod metadata to 3.4.16.

## 3.4.15

- Implemented IMDataCore Wave 0 Task 8: findings #61-#65 staged coverage/capability/namespace structures for sidecar v6 / journal v3.
- Added required `CoverageModelVersion`, immutable `CoverageCapabilitySets`, and branch-owned shared-sequence `CoverageTransitions` with exact checkpoint anchors for loaded-career boundaries.
- Bound namespace capability descriptors to the durable #60 owner lineage and kept registration distinct from explicit namespace capability declaration.
- Extended staged journal-v3 write/replay so capability descriptors, namespace-owner revisions, and coverage transitions publish atomically under BEGIN/COMMIT counts; only historical baseline assertions remain deferred.
- Kept v1-v5 migration conservative: positive legacy rows remain evidence, old absence remains unknown, and migration creates no fake `CareerStart`, `LateAdoption`, `LegacyResume`, or namespace coverage epoch.
- Added Task-8 source/fixture regressions for canonical descriptor identity, exact anchors, namespace owner mismatch, unsorted descriptors, shared-sequence collisions, v3 publication, and live v5/v2 isolation.
- Kept normal runtime persistence at sidecar v5 / journal v2; structured public coverage APIs remain later Wave-4 work.
- Bumped project and mod metadata to 3.4.15.

## 3.4.14

- Implemented IMDataCore Wave 0 Task 7 only: finding #60, durable document-level namespace-owner provenance for the staged sidecar-v6 / journal-v3 generation.
- Added required v6 `NamespaceOwnerBindings` as immutable per-namespace revision chains. Ownership provenance is non-rewinding security/access metadata rather than exact-checkpoint gameplay state.
- Split stable owner lineage from the existing strong assembly witness so legitimate binary upgrades/reinstalls can retain one owner lineage while MVID/location/content-hash witnesses rotate explicitly. Same owner + unchanged witness/schema is restart-idempotent and does not append a redundant revision.
- Added strict revision-chain validation: contiguous revisions, stable known-owner lineage, prior-witness retention on upgrades, and rejection of populated namespaces without an owner-binding chain.
- Extended bounded v1-v5 migration so every persisted namespaced event/custom-mutation token receives deterministic revision-1 `legacy_unbound` provenance. No ownership is inferred from `EnabledMods`, title/author/version metadata, DLL name, or the current first registrant.
- Added an explicit legacy-adoption helper that refuses ordinary first-claim adoption and appends a `migration_adopted` revision only after a separate authorization decision.
- Filled the Task-3 `NAMESPACE_OWNER_BINDING` journal-v3 row codec and count path so owner revisions publish atomically without requiring journal v4; coverage/baseline row families remain deferred.
- Added Task-7 source/fixture regressions for native owner chains, idempotent restart, witness rotation, unrelated-lineage rejection, legacy-unbound migration/adoption, duplicate revisions, missing bindings, v6 codec isolation, and journal-v3 owner rows.
- Kept normal runtime persistence at sidecar v5 / journal v2; durable v6 owner enforcement remains staged until the remaining Wave-0 cutover work is complete.
- Bumped project and mod metadata to 3.4.14.

## 3.4.13

- Implemented IMDataCore Wave 0 Task 6 only: finding #59, load-time shared-row normalization and compaction equivalence for historical `show_cast_changed` rows.
- Repaired loaded legacy per-idol show-cast fan-out so a payload-complete historical representative is cloned to the canonical shared envelope (`IdolId = -1`) only when the immutable payload validates as a complete participant set containing the stored envelope idol.
- Legacy fan-out collapse now requires the same show/timestamp bucket, byte-equivalent payload, and compatible historical `SourcePatch`; the earliest sequence is retained so compaction cannot move an occurrence across an exact-checkpoint boundary.
- Removed loaded-history canonical dominance: a canonical post-mod cast row no longer causes every noncanonical row at the same show/timestamp to be discarded. Source-distinct and payload-distinct occurrences survive independently.
- Added an explicit load-transform normalization count so envelope-only repairs force the next full compact snapshot even when no duplicate row was removed.
- Preserved Task-5 strict participant knownness: malformed/insufficient payloads are not fanned out or reconstructed from live show state. The pending/live settlement compactor is unchanged.
- Added Task-6 source/fixture regressions for legacy fan-out normalization, earliest-sequence retention, second-load idempotence, same-timestamp distinct-transition preservation, source-witness separation, malformed witness rejection, and canonical/legacy non-collapse.
- Kept live sidecar/journal wire versions at 5/2 and bumped project/mod metadata to 3.4.13.

## 3.4.12

- Implemented IMDataCore Wave 0 Task 5 only: finding #57, version-aware built-in shared participant/payload compatibility.
- Added a staged v6 `ParticipantSchemaVersion` discriminator for event rows and carried it through the staged journal-v3 event codec while leaving the live v5/v2 wire format unchanged.
- Source history fixes the shared-envelope boundary at sidecar v2: released v2-v5 shared rows stay on the strict current participant contract, while only the bounded v1 archival/synthetic compatibility generation may derive redundant participant counts from authoritative stored ID lists.
- Added explicit `Exact` / `Unknown` / `Malformed` participant knownness so insufficient legacy identity is preserved without guessing from current live objects, while contradictory current-format rows remain quarantined.
- Added canonical A14-E17-style fixtures for a v1 list-without-count row, an insufficient-identity v1 row, a v2 missing-count negative control, and a contradictory current-v6 row.
- Bumped project and mod metadata to 3.4.12.

## 3.4.11

- Implemented IMDataCore Wave 0 Task 4 only: finding #58, journal header/base-hash affinity before journal-version support classification.
- Added a version-agnostic minimal journal affinity envelope that parses `FormatName`, positive `FormatVersion`, and a syntactically valid 64-hex SHA-256 `BaseFileHash` before deciding whether the body version is supported.
- Repaired live v2 replay so a recognized unsupported journal with a nonmatching base hash is classified as a stale `HeaderMismatch`, while a matching unsupported journal still fails closed because it may contain authoritative committed rows.
- Preserved backup-recovery authority: a preferred unsupported wrong-hash journal now falls through to the sibling backup journal, while a preferred unsupported matching-hash journal stops fallback.
- Applied the same classifier to existing-journal append validation and exposed a staged journal-v3 candidate wrapper so the planned v6/v3 migration/recovery path uses the same affinity-before-version ordering.
- Added Task-4 source/contract regressions for supported/unsupported wrong-hash and matching-hash cases, malformed/foreign headers, backup fallback, and staged v3 parity.
- Kept live on-disk persistence at sidecar v5 / journal v2; finding #58 changes generation selection/error classification only and requires no format bump.
- Bumped project and mod metadata to 3.4.11.

## 3.4.10

- Implemented IMDataCore Wave 0 Task 3 only: staged journal-v3 row and transaction framing for the sidecar-v6 generation.
- Added a frozen seven-collection BEGIN/COMMIT envelope covering checkpoints, events, custom mutations, capability descriptors, coverage transitions, durable namespace-owner bindings, and historical baseline assertions.
- Added strict staged journal-v3 header encode/decode while leaving finding #58's version-agnostic base-affinity decision order for the next task.
- Added staged v3 transaction serialization/replay for the already-implemented families. Checkpoint rows use the sidecar-v6 identity-binding codec; event and custom-mutation rows retain their existing structural codecs.
- Reserved the audited v3 extension row kinds `COVERAGE_CAPABILITY_SET`, `COVERAGE_TRANSITION`, `NAMESPACE_OWNER_BINDING`, and `HISTORICAL_BASELINE_ASSERTION` without inventing their later semantic record members early. Deferred extension deltas require a full v6 snapshot until their owning Wave-0 tasks land.
- Preserved torn/uncommitted transaction atomicity and exact BEGIN/COMMIT target-count validation in the staged replay path.
- Kept live runtime persistence at sidecar v5 / journal v2; the existing v2 writer/replayer and live format constants remain unchanged.
- Added Task-3 source/contract regression fixtures and cumulative documentation. No compiled DLL is claimed because no .NET/Mono compiler is available in the execution environment.
- Bumped project and mod metadata to 3.4.10.

## 3.4.9

- Implemented IMDataCore Wave 0 Task 2 only: the sidecar-v6 exact-checkpoint identity-binding schema required by Audit Area #12.
- Added required checkpoint `IdentityBindingsVersion`, `IdentityBindingsComplete`, and `IdentityBindings` fields for the four IMDC-owned generation families: contract, clique, bullying episode, and generated non-custom task.
- Added concrete serialized-container locators matching Idol Manager `SavedData`: `business__ActiveProposalsData`, `Relationships__Cliques`, and `tasks__TaskData`; bullying bindings additionally carry parent-clique and bullied-target child locators.
- Added validation-witness, origin, coverage-boundary, and legacy-candidate metadata without using those fields to reconstruct vanilla gameplay state.
- Migrated v1-v5 logical checkpoints now explicitly target identity-binding schema version 1 with `IdentityBindingsComplete = false`; an empty migrated binding list is therefore legacy-unbound, not proof that no live identity-bearing object existed.
- Added an isolated logical-v6 sidecar serializer/reader plus positive complete, legacy-unbound, and duplicate-binding fixtures. The live runtime writer/reader remains exact sidecar v5 and journal v2; journal v3 is intentionally deferred to the next Wave-0 task.
- Verified the three locator field names against both the supplied decompiled game source and `Assembly-CSharp.dll`. No new direct game/Harmony type reference is introduced by this schema layer.
- Bumped project and mod metadata to 3.4.9.

## 3.4.8

- Began IMDataCore Wave 0 with finding #56 only: added a bounded one-time migration decoder for lightweight sidecar formats v1-v5 with a logical v6 target.
- Reintroduced the historically shipped v1/v2 validation contracts only inside the migration path: `EventId == Sequence`, `GameDateKey` consistency, and one-time embedded JSON normalization. The ordinary runtime sidecar reader remains exact-v5.
- Preserved migration knownness instead of inventing state: pre-v4 mod inventory is unknown, pre-v5 content fingerprint is not exact, and an early-v5 checkpoint with no `AgencyRoomIdentities` member is legacy-unbound rather than an empty complete map.
- Preserved event/custom sequence numbers and ordering and reject duplicate/out-of-order migration input instead of synthesizing history.
- Kept live persistence at sidecar v5 / journal v2 for this task. Migrated v6-target logical documents are not published until the remaining v6/v3 Wave-0 foundation is implemented.
- Added v1-v5 migration fixtures plus source/contract regression checks. No compiler/runtime claim is made because the supplied execution environment has no .NET build toolchain.
- Bumped project and mod metadata to 3.4.8.

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
