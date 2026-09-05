# IM Data Core 3.4.33

IM Data Core is the shared persistence and historical-event backend used by Cosmo Idol Manager mods. It keeps mod-owned state and selected gameplay history tied to the exact vanilla save file without modifying vanilla save JSON.

IMDC 3.4.33 writes sidecar format 5 and transactional journal format 2. Runtime IMDC still accepts exactly sidecar format 5. The cumulative Wave-0 storage foundation is complete in staged form, all five Wave-1 family-specific durable-identity contracts are implemented, and the shared Area-#12 D06-D10 compatibility layer is now staged as well. Business contracts use opaque `g:` generations, cliques use opaque `q:` generations, bullying intervals use opaque `b:` episode generations parented to the clique generation, generated non-custom tasks use opaque `t:` occurrence generations, and room-work SSK/tour targets use the existing durable room generation plus distinct `ssk:<ID>` / `tour:<ID>` owner namespaces. Exact legacy-unbound checkpoints deterministically derive forward generations from the selected checkpoint stamp plus serialized locator/witness and migration salt, while checkpoint-owned legacy candidate metadata preserves `Exact` / `Ambiguous` / `Unresolved` quality without rewriting old event rows. The normal runtime v6/v3 cutover remains intentionally disabled, so the four opaque-generation families and durable candidate multimap are not advertised as restart-stable until v6 is live; the existing room/theater/cafe generations and SSK/tour room-work identity remain safe on v5.

IMDC 3.4.33 uses SHA-256 content-fingerprinted exact-save checkpoints, keeps the enabled-mod inventory and durable agency-room generation map, anchors newly adopted vanilla careers before any explicit IMDC-only flush, and preserves deleted-save sidecars under `OLD` archive directories. Append-only generations use the SHA-256-bound transactional journal and periodically compact into the atomic v5 snapshot.

### Wave 3 Task 3: namespace bootstrap, cancellation proof, and reentrancy hardening

Version 3.4.33 completes findings #53 through #55 without changing IMDataCore event schemas or live storage formats. Consumer namespace registration now calls `TryRegisterNamespace(...)` directly at a safe gameplay point; the registration API owns safe runtime initialization, while `IsReady()` remains observational and is no longer a one-shot prerequisite that can lose an early registration because of Harmony postfix ordering. The template and onboarding docs also describe a bounded later retry when registration itself reports failure.

Business-contract cancellation now snapshots exact collection membership and immutable contract history context before vanilla mutation, then emits `contract_cancelled` only after proving that the same proposal reference changed from contained to absent. Detached, stale, repeated, and throwing cancellation calls cannot manufacture lifecycle terminals. Liability money observation remains independent, so a real vanilla deduction can still produce its ledger row even when no contract lifecycle transition occurred.

The audited ambient capture bridges are now nesting-safe. Activity-income attribution, concert-crisis choice context, scandal parameter mutation suppression, blackmail trigger results, and money-ledger attribution use thread-local frame/depth semantics with owning Harmony finalizers restoring the previous outer frame. Money frames are installed before detail construction so exceptions cannot pop an unrelated outer operation, while transient show-profit attribution remains scoped to the exact resource mutation it decorates.

Live persistence remains sidecar v5 / journal v2 and the Event Catalog remains 173 queryable built-in event types across 41 domains. See [`docs/IMDC_WAVE3_TASK3.md`](docs/IMDC_WAVE3_TASK3.md).

### Wave 3 Task 2: historical references and lifetime semantics

Version 3.4.32 implements findings #48 through #52 without changing IMDataCore current-state ownership or live storage formats. Current money-detail rows now use reference schema version 1 with explicit knownness. Salary and severance rows carry staff IDs; theater and cafe rows carry canonical facility generations; single/show/cafe/concert rows carry structured project/member references; and active business-contract money rows reuse the same history identity resolver as contract lifecycle capture. Legacy money rows remain schema 0 and unknown references normalize to invalid or empty values instead of misleading zero IDs.

Natural loan expiry now emits `loan_matured` from the authoritative weekly transition only when the exact loan was active and overdue before processing, becomes inactive afterward, and remains in the loan collection. The processing date is preserved separately from the contractual end date. Election-release cancellation also snapshots its linked election before the UI path clears the parent `ReleaseSingle` pointer, then transfers that observed identity and knownness into `single_cancelled` through exact-reference, nesting-safe transient context.

Live persistence remains sidecar v5 / journal v2; the previously staged v6/v3 formats remain disabled. See [`docs/IMDC_WAVE3_TASK2.md`](docs/IMDC_WAVE3_TASK2.md).

### Wave 3 Task 1: payload and timing semantics

Version 3.4.31 implements findings #41 through #47 without changing IMDataCore current-state ownership or live storage formats. Single release history now separates the true release-time `single.FanAppeal` vector from the opinion-time `ReleaseData.FanAppeal` vector. Contract-window history is emitted at the actual `business.SetProposal(...)` presentation boundary rather than contract activation. Substories now distinguish `substory_queued` from `substory_presented`, with actor snapshots taken after vanilla's `BeforeStart` callback but without IMDC invoking that callback.

Idol departures and status transitions now carry controlled provenance from source-proven callers, with `unknown` used instead of inference when a caller is not recognized. Generated-task payloads expose structured single/show constraint IDs and titles rather than requiring consumers to parse localized descriptions. Blackmail history now uses a history-only `bm:<guid>` occurrence across enqueue, trigger, and dequeue, names queue checkpoints according to when they are observed, and records the trigger's influence reward as planned rather than falsely claiming it has already been applied.

Live persistence remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE3_TASK1.md`](docs/IMDC_WAVE3_TASK1.md).

### Wave 2 Task 7: lifecycle and history closure

Version 3.4.30 completes the remaining Wave-2 lifecycle/history closure group (#32 through #40) without changing vanilla save ownership or IMDC live storage formats. Clique joins now snapshot the old leader before vanilla can recalculate succession, so `clique_joined` reports truthful leader-before/leader-after context instead of two copies of the post-join leader.

Training and treatment now have explicit `room_work_assigned` history, while direct forced training termination and direct treatment cancellation produce the existing room-work terminal semantics without double-counting `agency._room.CancelJob()` or misclassifying natural treatment completion as cancellation. Activity level-ups, Summer Games objective activation baselines, tour-country level progression, and direct VN story status restorations now have historical capture at their authoritative mutation seams.

Nested Harmony capture now uses a semantic capture scope for the audited prerequisite chains. Child rows are held without sequence numbers until the outer prerequisite event has been enqueued, fixing loan-added/initialized, show-released/episode, room-built/facility-created, contract-accepted/activated, and recursive-clique ordering without globally sorting unrelated history. Group creation/disbanding is stored once as a shared row with participant indexing instead of physically fanning out one duplicate row per member. Single chart resolution is also split from release history into `single_chart_result`, so a chart backfill can never manufacture a second `single_released` occurrence.

Live persistence remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE2_TASK7.md`](docs/IMDC_WAVE2_TASK7.md).

### Wave 2 Task 6: split event and relationship history

Version 3.4.29 completes the plan's split event/relationship history group (#1, #2, #6, #30, #31) while deliberately leaving every split finding's live continuation/repair half outside IMDataCore. True runtime idol-idol relationship creation now emits `idol_relationship_created` with the initial randomized `Dynamic`, while load/bootstrap reconstruction is suppressed.

Random-event runs now carry history-only `re:<guid>` occurrence IDs. Business-conditioned starts record the exact already-selected durable contract history identity and retain it through the terminal. SNS-only events, which vanilla never routes through `ConcludeEvent()`, now receive one correlated `random_event_concluded` row from the exact generated iterator; IMDataCore never repairs event state and explicitly marks SNS reply effects/resource deltas as not observed.

Template events now carry history-only `te:<guid>` presentation/conclusion correlation. The popup/button seams passively preserve the exact reply variants vanilla rendered, selected part/value context, actors, and semantic variables without calling the random `GetReplies()` selector again. `Date_Popup.OnClick_ForceBreakup()` also emits one semantic `player_forced_breakup` row around the observed before/after state. Its capture brackets Save n Load Fixes when installed but performs no breakup repair itself.

Live persistence remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE2_TASK6.md`](docs/IMDC_WAVE2_TASK6.md).

### Wave 2 Task 5: tasks, substories, and scene completion history

Version 3.4.28 completes the plan's tasks/substories/scenes group (#27, #28, #29). Finding #27 remains satisfied by the existing Wave-1 generated-task birth seam: `tasks.GenerateTask(...)` proves one newly appended generated task, assigns/reuses that occurrence identity, and emits exactly one `task_added`. Task 5 deliberately does not add a duplicate birth producer.

Reversible chapter-3 objectives now emit `task_unfulfilled` only for a proven `Fulfilled: true -> false` transition through `tasks._task.Unfulfill()`. The row uses the same task history identity resolver and lifecycle payload as the existing completion/failure/done family, so a generated task's rollback stays on its existing occurrence stream instead of minting a second identity.

Scene-type substories now carry an explicit history-only `ss:<guid>` occurrence ID from queue insertion through room-scene completion. IMDC binds the exact queued object at the vanilla `Scenes.Set(...)` presentation seam, snapshots the active room scene before `agency._room.SubstoryFinish()` clears it, and emits one correlated `substory_completed` terminal. After load/F9 clears transient maps, correlation can recover from the newest still-open occurrence on the selected active history branch. This is history correlation only: IMDC does not restore `room.substoryScene`, and Save n Load Fixes remains the live current-state continuity owner where installed. A separate scene-presentation history event remains reserved for later finding #43 rather than being invented early here.

Live persistence remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE2_TASK5.md`](docs/IMDC_WAVE2_TASK5.md).

### Wave 2 Task 4: show episodes, cancellation intent, and award speeches

Version 3.4.27 completes the plan's shows/singles/awards group (#5, #21, #22, #26) as historical capture only. `show_episode_released` now preserves the completed episode's exact seven-axis `FanAppeal` summary and the canonical 12-segment demographic audience breakdown, using the same stable segment-key ordering as single-release history. The ordinary episode producer and post-mod canonical producer carry the same fields.

Deferred show cancellation now has explicit lifecycle history. `_show.Cancel()` emits `show_cancellation_scheduled` only for a real `ToCancel: false -> true` transition that leaves the show non-canceled, while immediate or eventually-consumed cancellation remains the existing terminal `show_cancelled` row. `_show.DontCancel()` emits `show_cancellation_withdrawn` only for a real `true -> false` transition. IMDC does not project a cancellation date from vanilla's buggy helper.

Award dialogue now emits one `award_speech_delivered` row per live speech object. IMDC marks only the actual `awards_solo_thanks` / `awards_group_thanks` dialogue execution, passively observes the game's `GetThanks()` result, and weakly deduplicates later getter evaluations. It never calls `GetThanks()` or `mainScript.chance(...)` to manufacture history. When Save n Load Fixes is present, its `her_choice` cache remains the current-state consistency owner and IMDC simply records that authoritative returned category. Live persistence remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE2_TASK4.md`](docs/IMDC_WAVE2_TASK4.md).


### Wave 2 Task 2: business proposals and developing-loan terminals

Version 3.4.25 implements the plan's business/loan/contracts group (#15, #16, #23) as historical capture only. `business.SetProposal(...)` now establishes one runtime `p:<guid>` proposal occurrence at the authoritative presentation boundary, and `business.Accept()` / `business.Decline()` terminate that same occurrence as `business_proposal_accepted` or `business_proposal_declined`. Proposal payloads preserve the ordered candidate slate, selected idol, final negotiation result/coefficient/attempt count, raw and effective proposal values, and other proposal context without re-running generation or negotiation RNG.

For continuing accepted proposals, the terminal proposal row also carries the exact `EntityId` chosen by the existing contract-acceptance history path, so the transient offer occurrence can be joined to the resulting durable contract stream without changing contract identity semantics. Declines retain the full pre-decline snapshot and never fabricate a contract reference. Proposal occurrence bookkeeping is process-local observation state and is cleared on the existing load/F9 runtime reset.

Developing room loans now receive a distinct `loan_cancelled` terminal event from `agency._room.CancelJob()`, but only when IMDC proves the exact loan reference was contained in `loans.Loans` before the call and absent afterward. The row uses the same vanilla loan ID and records collection membership before/after. Ordinary payoff remains `loan_paid_off`: payoff requires active-before -> inactive-after and leaves the loan object in the collection, so cancellation and settlement cannot collapse into one lifecycle meaning. Live persistence remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE2_TASK2.md`](docs/IMDC_WAVE2_TASK2.md).

### Wave 2 Task 1: people, auditions, hire, and date history

Version 3.4.26 continues Wave 2 with the groups/social/rivals group (#20, #24, #25). `clique_created` now records the authoritative first-member birth without inventing a join; `player_bullying_intervention` preserves partial/full stop semantics under the existing bullying identity; and monthly rival capture emits sparse per-group creation/retirement rows from exact before/after object-reference diffs while retaining one aggregate market row. Live persistence remains sidecar v5 / journal v2.

Version 3.4.24 starts Wave 2 with the plan's people/auditions/hire/date group (#14, #17-#19). Audition history now uses one runtime `a:<guid>` occurrence ID across `audition_started`, `audition_candidates_generated`, audition cost, and `audition_completed`. Candidate summaries receive deterministic occurrence-local `:candidate:<ordinal>` IDs, preserve generated profile/rarity, and the terminal snapshot records which candidates were actually hired versus rejected. The start payload keeps its pre-generation count explicitly as `candidate_count_at_start`; the authoritative generated slate size lives on the post-`GenerateGirls` event.

`idol_hired` now carries truthful hire provenance (`audition`, `graduation_successor`, `unique_story`, or `generic`) plus a hire-time generated profile. Parameter values and potentials are read directly from backing fields. IMDC never calls `param.GetPotential()` for logging, because that getter can generate/mutate potential and advance vanilla RNG. A zero backing potential is retained with `potential_materialized = false` rather than being fabricated.

Generic dates now produce correlated `player_generic_date_presented` and `player_generic_date_completed` rows under one `d:<guid>` occurrence. Location and mask state are captured only after `Dating.GenerateGenericDate(...)` owns them; the completed row is emitted from the `vn_actions.Do` Postfix only after the exact `dating/add_points` action has committed. The older `player_date_interaction` remains the attempt/route-decision view. `Date_Flirt.DoFlirt(...)` now emits `player_flirt_outcome` from the already-applied `DatingData.Previous_Attempt`, preserving per-occurrence semantic results without logging dialogue wording or invoking the RNG-producing `GetOutcome()` helper.

These are historical ledger additions only. They do not duplicate audition/date/idol current-state persistence in the sidecar, and live storage remains sidecar v5 / journal v2. See [`docs/IMDC_WAVE2_TASK1.md`](docs/IMDC_WAVE2_TASK1.md).

### Shared identity compatibility and #66 resolver

Version 3.4.24 completes the shared Area-#12 D06-D10 compatibility layer around the five family contracts. Every staged v6 checkpoint now carries explicit `IdentityCandidates` beside `IdentityBindings`. Candidate state is branch-owned: exact checkpoint selection clears the current compatibility view before native rebind or legacy adoption, so F9 cannot inherit generations from a discarded branch.

A migrated legacy-unbound checkpoint derives forward `g:` / `q:` / `b:` / `t:` IDs deterministically from the exact vanilla checkpoint stamp, family serialized ordinal/parent/child locator, structural SHA-256 witness, and migration salt `imdc.identity.adoption.v1`. This creates no synthetic acceptance, clique birth, bullying start, or task birth. Legacy rows remain under their original `EntityId`; candidate metadata is a multimap and never a global rewrite dictionary.

The read-only #66 resolver is available through `IMDataCoreApi`, `IMDataCoreInteropApi`, and `IMDataCoreAPI`. `TryResolveCurrentIdentity` uses documented stable locator descriptors rather than CLR object references. `TryResolveLegacyIdentityCandidates` returns the complete candidate set plus `Unresolved`, `Ambiguous`, or `Exact`; `CanonicalEntityId` is populated only for a proven exact result. On the live v5 runtime the resolver exposes only identities whose durable ingredients already survive v5, such as room/theater/cafe and SSK/tour room-work. The four opaque generation families and durable candidate lookup remain unresolved until the v6 checkpoint generation is active. See [`docs/IMDC_WAVE1_TASK6.md`](docs/IMDC_WAVE1_TASK6.md).

### Generated non-custom task birth and staged occurrence identity

Wave 1 Identity 4 coordinates the generated-task identity repair with finding #27 at the same authoritative source seam. `tasks.GenerateTask(...)` directly constructs, randomizes, and appends a durable non-custom task without calling the existing `AddTask`/`AddTaskContinue` hooks. IMDC now snapshots the active-task reference set before the call, identifies exactly one newly appended object afterward, allocates an opaque `t:<guid>` occurrence generation, and emits the previously missing `task_added` row.

Custom/scripted task IDs retain their existing definition-stream semantics. For generated tasks, the historical `type|goal|girl` fallback is only a legacy candidate because different occurrences can reuse it even when their randomized genre/lyrics/medium/skill constraints differ. Exact staged-v6 checkpoints bind the live task by serialized `tasks__TaskData` ordinal plus SHA-256 over every field vanilla saves. Rebinding after `tasks.LoadFunction` requires ordinal, witness, and reconstructed-row equality. The witness validates a row but never allocates identity: a later generated occurrence may reproduce the exact same full `TaskData` row and still receives a new `t:` generation.

Version 3.4.24 keeps live persistence at sidecar v5 / journal v2. Finding #27's real generated-task birth row is safe to emit now using the existing coarse v5 `EntityId`; canonical `t:` emission stays gated until v6 can persist and rebind the occurrence generation. Legacy/adopted tasks already present at a migration boundary do not receive a synthetic birth row. See [`docs/IMDC_WAVE1_TASK4.md`](docs/IMDC_WAVE1_TASK4.md).


### Wave 5 Task 6: current v6/v3 combined correctness audit

IMDataCore 3.4.33 now runs **sidecar v6 / journal v3** as one atomic live persistence generation. The combined static audit with Save n Load Fixes 0.54.0 and Save Write Ordering Fix 1.4.5 also hardens correctness-first load waiting, partial-provider deletion safety, canonical transport path keying, and SWOF-to-SNLF delegation identity. Task 5 forward-extension preservation/fail-closed rules therefore apply to the active generation. No new backward-compatibility feature is added by this task. See [`docs/IMDC_WAVE5_TASK6_CURRENT_V6V3_COMBINED_CORRECTNESS_AUDIT.md`](docs/IMDC_WAVE5_TASK6_CURRENT_V6V3_COMBINED_CORRECTNESS_AUDIT.md).

### Wave 5 Task 4: companion transport reference validation

The reflection-only ordered transport bridge is validated against the supplied Save n Load Fixes and Save Write Ordering Fix source/Release-DLL packages. Direct SNLF trust requires transport API version >= 1 plus the exact SNLF owner ID; current SWOF effective health requires a recognized SWOF-or-delegated-SNLF owner, while older SavedData-only SWOF compatibility remains available. See [`docs/IMDC_WAVE5_TASK4_COMPANION_TRANSPORT_REFERENCE_VALIDATION.md`](docs/IMDC_WAVE5_TASK4_COMPANION_TRANSPORT_REFERENCE_VALIDATION.md).

### Wave 5 Task 3: v6/v3 static release preflight

IMDataCore 3.4.33 now has one atomic source-level activation predicate for the
staged sidecar-v6/journal-v3 generation. Canonical identity and structured coverage
cannot become live from a sidecar-only edit, and physical initialization rejects
split 6/2 or 5/3 configurations. Static migration, API, regression-registry,
transport, and current-state nonduplication prerequisites are verified, while the
real compile/Unity cutover gate remains open. Production still writes sidecar v5 /
journal v2. See
[`docs/IMDC_WAVE5_TASK3_V6V3_STATIC_PREFLIGHT.md`](docs/IMDC_WAVE5_TASK3_V6V3_STATIC_PREFLIGHT.md).

## Services

- Namespaced custom JSON: `TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`
- Timeline events: `TryAppendCustomEvent`, `TryAppendCustomEventOnce`, `TryReadRecentEventsForIdol`, `TryReadEventsForIdolPage`, `TryReadHistoryPage`
- Exact cash ledger: `TryReadMoneyTransactions`, `TryReadMoneyTransactionsPage`, `TryGetMoneyTransactionTotals`, `TryGetMoneyLedgerCoverageStart`
- Explicit sidecar persistence: `TryFlushNow`
- Active physical-save identity: `TryGetActiveSaveKey`
- Read-only persistence telemetry: `TryGetPersistenceDiagnostics`
- Current-generation identity resolution: `TryResolveCurrentIdentity`
- Legacy coarse-key candidate lookup: `TryResolveLegacyIdentityCandidates`

Built-in capture covers singles, shows, contracts, groups, tours, elections, concerts, idols, staff, relationships, finance, activities, story/system transitions, and other gameplay events. See [`docs/EVENT_CATALOG.md`](docs/EVENT_CATALOG.md).

The Event Catalog separates **173 queryable built-in event types** from three internal transient streams (`idol_status_changed`, `research_points_accrued`, and `idol_earnings_recorded`) that retention intentionally removes before public timeline queries. Catalog presence does not imply queryability for those explicitly marked transient types.


For a complete career-wide durable-history browser, use `TryReadHistoryPage(...)`. It returns each retained physical occurrence exactly once from the selected branch and does not require a live idol ID. `TryReadEventsForIdolPage(...)` remains the compatibility view for one known idol plus global-relevant rows and participant-expands shared occurrences.

For dense exact money detail, use `TryReadMoneyTransactionsPage(...)`. It preserves the existing game-date range semantics but continues by the last returned transaction `EventId`, so a single game day can exceed 10,000 rows without making the suffix unreachable. The legacy `TryReadMoneyTransactions(...)` reader remains capped for compatibility; aggregate totals remain uncapped.

Lifecycle capture is postcondition-driven where vanilla can legally no-op: IMDC records the requested action only after the relevant state mutation is observed. In particular, static show cancellation is keyed to actual removal from `Shows.shows`, audition starts are suppressed on the terminal-scandal early-return path, random-event starts require a newly appended active event, loan payoff requires `active before -> inactive after`, idol hiring requires `absent before -> contained after`, rival trend refresh requires both vanilla eligibility and a changed update marker, and agency room destruction requires `contained before -> absent after`. Missing Harmony pre-state is treated as unknown rather than a legitimate default state. Staff severance money metadata is scoped only around the actual `Fire_Severance()` deduction and is cleared on both normal and exceptional exits.

Show history has an additional compaction rule because IMDC deliberately observes certain show mutations twice: once at the ordinary vanilla hook and once after known cast-mutating mods settle. Capture-triggered threshold flushing is deferred while that post-mod settlement is open, and the show editor scope spans the complete `Show_Popup.OnContinue()` commit. This keeps the ordinary and canonical rows in the same pending batch so canonical show episode/cast compaction cannot change the current-session answer merely because the 256-event threshold was crossed between the two observations. Explicit forced save/read flushes remain authoritative and are not silently disabled.

`single_status_changed`, `show_status_changed`, and `tour_status_changed` are **setter-observation streams**, not exhaustive state-transition journals. Vanilla performs some lifecycle transitions through direct field assignments: initial single release, initial show release, and tour completion are represented by `single_released`, `show_released`, and `tour_finished` respectively. Consumers reconstructing lifecycle history should use the dedicated lifecycle events rather than assuming every status mutation appears as `*_status_changed`.

### Durable room/theater/cafe history identity

Vanilla agency-room IDs are runtime-only, and theater/cafe IDs can be recycled after the highest-numbered instance is destroyed. IMDC therefore does **not** use those vanilla IDs as durable timeline identity for `agency_room`, `theater`, or `cafe` history. Each physical agency room receives an IMDC-owned generation identifier such as `g:<guid>`; theater and cafe events use the generation of their owning room within their own `EntityKind` namespace. Room-work compound identities use the same generation prefix.

The raw vanilla `room_id`, `theater_id`, and `cafe_id` payload fields are retained for immediate game-state correlation. They are not stable historical keys. Consumers grouping historical rows should use `(EntityKind, EntityId)`.

Every v5 checkpoint freezes the room-generation map in vanilla's serialized floor/room order and reassociates it while vanilla reconstructs rooms on load. `AgencyRoomIdentities` is required in every accepted v5 checkpoint, including an empty array when the save contains no rooms. A format-5 checkpoint that omits the field is invalid rather than treated as an older compatible schema.

### Staged durable contract-generation identity

Wave 1 Identity 1 implements the Area-#12 contract identity repair behind the v6 identity-runtime gate. `business.Accept` reserves an opaque `g:<guid>` generation before vanilla's nested `AddActiveProposal`; the nested insertion binds the new `active_proposal` to that reservation, while a direct/modded insertion receives a separate fresh generation. Acceptance, activation/window-open, weekly, cancellation, natural-completion, and break capture all use the same generation-aware resolver once v6 is live.

Exact checkpoint persistence uses the serialized `business__ActiveProposalsData` ordinal plus a SHA-256 witness over the fields vanilla actually saves. Rebinding after `business.LoadFunction` requires the ordinal, witness, and reconstructed row to agree. The historical `idol|type|end-day` key is retained only as a legacy candidate witness, so two simultaneous byte-identical saved contracts can still own different canonical generations by ordinal.

Version 3.4.24 deliberately does **not** emit those `g:` IDs into the live v5 event stream because v5 has no durable contract-binding checkpoint field. Doing so would fragment one contract across restart. The generation path activates automatically only when the live sidecar generation reaches the staged v6 contract. See [`docs/V6_IDENTITY_BINDING_SCHEMA.md`](docs/V6_IDENTITY_BINDING_SCHEMA.md) and [`docs/IMDC_WAVE1_TASK1.md`](docs/IMDC_WAVE1_TASK1.md).

### Staged durable clique-generation identity

Wave 1 Identity 2 applies the same generation theorem to `Relationships._clique`. Vanilla persists mutable clique state but no clique instance ID, while the historical sorted-member signature can both change during one clique lifetime and recur in a later clique. The staged forward path therefore allocates one opaque `q:<guid>` at `Relationships.StartNewClique` and keeps that generation through joins, leaves, leader changes, and recursive clique collapse.

Exact checkpoint persistence uses the serialized `Relationships__Cliques` ordinal plus SHA-256 over the complete saved clique row. Rebinding after `Relationships.LoadFunction` requires ordinal, fingerprint, and reconstructed clique state to agree. The old sorted-member signature remains only a `LegacyCandidateKey`; equal signatures or equal saved-row fingerprints across two different clique lifetimes never imply that the generations are equal. Version 3.4.24 still emits legacy clique signatures on the live v5 event wire, so this staged identity cannot fragment history before v6 checkpoint persistence is enabled. See [`docs/IMDC_WAVE1_TASK2.md`](docs/IMDC_WAVE1_TASK2.md).

### Staged durable bullying-episode identity

Wave 1 Identity 3 gives each uninterrupted bullying interval one opaque `b:<guid>` generation. The generation is created only after `Relationships._clique.AddBulliedGirl(...)` proves a real not-bullied -> bullied transition. Clique generation is carried only as parent correlation; the mutable clique leader remains payload and legacy-candidate context, so leader succession cannot rename an active bullying episode.

Exact checkpoint persistence stores each active episode as a child binding of its exact clique generation: the parent `Relationships__Cliques` ordinal, parent `q:` ID, `bullied_target:<idolId>` child locator, and a target-specific SHA-256 witness derived from the complete saved clique row plus target ID must all agree. Rebinding runs only after the parent clique generation has been rebound. A stopped episode retires only after its terminal history row is enqueued; bullying the same target again later receives a fresh `b:` generation even if the same clique, target locator, and saved-row witness recur. Old `leader|target` keys remain compatibility candidates only.

Version 3.4.24 still writes sidecar v5 / journal v2, so the live event wire continues using the historical leader/target identifier until v6 identity checkpoints are active. The new `clique_generation_id` bullying payload correlation is emitted only when the v6 generation resolver is active. See [`docs/IMDC_WAVE1_TASK3.md`](docs/IMDC_WAVE1_TASK3.md).

### Durable room-work SSK/tour owner namespace

Wave 1 Identity 5 removes the one remaining room-work target collision without adding a fifth random generation family. SSK and tour IDs come from independent persisted vanilla allocators, so the canonical child identity now keeps the existing room-generation prefix and uses `g:<room>:ssk:<SSK.ID>` versus `g:<room>:tour:<tour.ID>`. Completion and `CancelJob()` capture use the same owner token in both `EntityId` and `room_work_kind`, so all stages for one target remain on one stream while equal numeric IDs across SSK and tour never merge.

This split is safe on the live v5 format because v5 already checkpoints the room generation and vanilla already persists both project-ID domains. No extra identity binding record or random work GUID is required. Existing `g:<room>:event:<N>` rows are never rewritten: their old payloads do not contain a source-authoritative discriminator that can safely prove SSK versus tour in bulk. They remain legacy rows for the shared candidate-multimap compatibility layer, which must expose ambiguity instead of silently choosing one target. See [`docs/IMDC_WAVE1_TASK5.md`](docs/IMDC_WAVE1_TASK5.md).

### Staged durable namespace ownership

The sidecar-v6 logical model now carries a document-level `NamespaceOwnerBindings` provenance collection for namespaced custom state and custom history. Ownership is deliberately **not** checkpoint state: exact F9/Save-As branch selection may rewind custom rows, but it must not erase the durable owner lineage that controls who can reclaim that namespace after restart.

Each namespace uses an immutable revision chain. Native v6 ownership begins at revision 1, a legitimate binary upgrade/reinstall appends a later revision under the same stable assembly lineage while rotating the strong MVID/location/SHA-256 witness, and v5 migration creates only a revision-1 `legacy_unbound` record with unknown owner. Ordinary first registration cannot turn that unknown record into a known owner; migration adoption is a separate explicit authorization path. `EnabledMods` remains diagnostic inventory and is never used as namespace authority. See [`docs/NAMESPACE_OWNER_PROVENANCE.md`](docs/NAMESPACE_OWNER_PROVENANCE.md).

The live v5/v2 writer does not serialize this collection yet, so IMDC 3.4.24 does not claim durable owner enforcement in normal runtime activation before a later deliberate v6/v3 runtime cutover. Same-process registration still uses the existing strong assembly identity.

### Staged coverage and capability knownness

The sidecar-v6 logical model now also requires `CoverageModelVersion`, an immutable `CoverageCapabilitySets` catalog, and sequence-owned `CoverageTransitions`. Capability-set identity is derived from canonical semantic `{Token, Revision}` content rather than package version or the sidecar format. Namespace descriptors are additionally bound to the durable #60 owner lineage.

Coverage transitions consume the same monotonic sequence space as events and custom mutations. `CareerStart` represents native new-career observation, while loaded-career boundaries use exact checkpoint anchors for `LateAdoption`, `LegacyResume`, and namespace process gaps. Missing coverage remains unknown rather than complete-empty, positive legacy rows remain evidence of themselves, and v1-v5 migration creates no fake historical frontier. Capability descriptors and owner provenance do not rewind; coverage transitions do.

The staged journal-v3 path now publishes `COVERAGE_CAPABILITY_SET`, `NAMESPACE_OWNER_BINDING`, `COVERAGE_TRANSITION`, and `HISTORICAL_BASELINE_ASSERTION` additions in the same committed count envelope. Task 9 completes the final frozen v3 semantic row codec and adds the bounded `group_target_audience_origin` baseline carrier. Baseline assertions rewind with branch state; capability descriptors and owner provenance do not. The structured public coverage/knownness APIs remain later Wave-4 work. See [`docs/V6_COVERAGE_SCHEMA.md`](docs/V6_COVERAGE_SCHEMA.md) and [`docs/V6_HISTORICAL_BASELINE_SCHEMA.md`](docs/V6_HISTORICAL_BASELINE_SCHEMA.md).

### Staged migration provenance and downgrade protection

Sidecar-v6 logical documents now require non-rewinding `MigrationProvenance`. Native generations identify themselves as `native_v6`; v1-v5 conversions identify themselves as `legacy_migration` and retain the validated source sidecar version, a normalized source-document SHA-256, the source sequence high watermark, the v6/v3 target generation, and a deterministic save-scope-bound conversion ID. Reopening a committed migrated document therefore has an explicit destination identity and cannot be mistaken for a new native career or silently rerun legacy conversion semantics. Migrated coverage may begin only with a later exact `LegacyResume` boundary, never a fabricated `CareerStart` or `LateAdoption`. See [`docs/V6_MIGRATION_PROVENANCE_SCHEMA.md`](docs/V6_MIGRATION_PROVENANCE_SCHEMA.md).

The live v5 reader also distinguishes unsupported authoritative generations from ordinary corruption. An unsupported primary sidecar, or an unsupported journal whose SHA-256 affinity matches the candidate compact base, makes that save scope write-protected and blocks fallback to an older `.imdc.bak` generation. This prevents backup recovery from becoming an accidental downgrade writer. Finding #58 remains intact: an unsupported journal bound to a different base hash is still classified as a stale suffix and may be ignored while the healthy candidate base is considered.

## Save ownership

Each physical vanilla save owns one mirrored IMDC sidecar beneath the sibling `IMDataCore` directory:

- `data\auto_save.json` -> `IMDataCore\auto_save.json`
- `data\manual_saves\<id>\save.json` -> `IMDataCore\manual_saves\<id>\save.json`
- `data\story_mode\<playthrough>\chapter_3\save.json` -> `IMDataCore\story_mode\<playthrough>\chapter_3\save.json`

`global_data.json` is not a game-save scope and never receives an IMDC sidecar.

## Version 5 sidecar

The current private disk format remains:

- `FormatName`: `IMDataCore.LightweightSidecar`
- `FormatVersion`: `5`

V5 keeps JSON-native event/custom-data storage and adds `ContentFingerprint` to every checkpoint. The fingerprint is `sha256:<64 lowercase hex characters>` over Unity's compact JSON representation of that exact vanilla `SavedData` state. A built-in event can look like:

```json
{
  "Sequence": 420,
  "GameDateTime": "2028-04-16T00:00:00.0000000",
  "IdolId": 14,
  "EntityKind": "single",
  "EntityId": "32",
  "EventType": "single_released",
  "SourcePatch": "SingleRelease",
  "NamespaceIdentifier": "",
  "Payload": {
    "title": "Example",
    "cast_id_list": [14, 7, 21],
    "sales": 18324
  }
}
```

A namespaced event created through `TryAppendCustomEventOnce` may additionally contain an optional `IdempotencyKey`. Sidecar formats older than 5 are not accepted by this development build.

The public `IMDataCoreEvent.PayloadJson`, `EventId`, and `GameDateKey` members remain available. IMDC reconstructs those views from the v5 document so consumers do not need to understand the private sidecar schema.

## Exact checkpoint loading

A checkpoint identifies one vanilla save state using its physical relative path, vanilla `LastSave`, playtime seconds, game date/time, the vanilla-content SHA-256 fingerprint, and the IMDC sequence watermark. The content fingerprint removes the same-second collision that is possible if timestamp/playtime fields alone are used.

Each v5 checkpoint also freezes the enabled Idol Manager mod set. Each row stores the mod name/title, author, declared version, and every DLL filename found under that mod's folder; JSON-only mods remain represented with an empty DLL list. On later load, including after returning to the main menu or restarting the game, IMDC compares that saved inventory to the current registry and logs missing, disabled, and metadata/DLL mismatches without blocking vanilla load.

Every v5 checkpoint freezes a required `AgencyRoomIdentities` snapshot. It records one IMDC room-generation ID for each serialized vanilla room and is used only to restore durable historical identity after load; it does not modify vanilla save JSON or change exact-checkpoint identity. The array may be empty for a save with no agency rooms, but the field itself may not be omitted.

When an existing sidecar does not contain an exact checkpoint for the vanilla save being loaded, IMDC 3.4.25 **fails closed**. It detaches supplemental state for that physical save, protects the existing sidecar from overwrite, and does not activate history using a date-only approximation.

This avoids cross-branch leakage when two different save histories happen to share the same in-game date.

## Adoption of existing vanilla careers

When a vanilla career is loaded for the first time with no IMDC sidecar, IMDC creates an in-memory sequence-0 checkpoint for that exact loaded `SavedData` state. It does not write anything merely because the save was loaded. If a consumer later calls `TryFlushNow`, the new sidecar already contains an exact anchor for the vanilla file and can be matched safely on the next load.

## Deleted-save archives

Vanilla save deletion never deletes IMDC history. After a successful vanilla delete, IMDC archives the mirrored save directory by renaming it in place:

```text
f294ee32     -> f294ee32OLD
f294ee32OLD  -> existing archive, so the next deletion becomes f294ee32OLD2
```

Story-playthrough deletion archives the mirrored playthrough directory as one unit. The archive operation is serialized against IMDC loads, writes, and background compaction so a stale queued writer cannot recreate the deleted path after archival. If the rename fails, the original supplemental directory is left untouched and writes back into that deleted-save directory are blocked for the remainder of the process.

If the deleted path was active, IMDC detaches its physical binding but keeps the logical in-memory history so a later vanilla New Save/Save As can preserve that career branch under a new path.

## Backup recovery

Atomic replacement retains one sibling:

```text
<sidecar>.imdc.bak
```

If the primary sidecar is unreadable or invalid, IMDC validates the backup. A valid backup can be used as the recovery source for the session. The damaged primary is left untouched during recovery, and the known-good backup is preserved when a later successful save replaces the damaged primary.

Recovery tracks the exact journal whose parsed header matched the backup base. If an interrupted compaction left the matching journal at the primary journal path, the later healing write first publishes that journal durably as `<sidecar>.imdc.bak.imdc.journal`; only then may it remove the stale primary-journal copy. If publication fails, the source journal is kept so the recovered backup generation is not weakened. Empty or first-header-torn preferred journals are not considered base-hash matches and therefore cannot hide a valid backup journal.

The recovered document still has to contain an exact checkpoint for the vanilla save being loaded. Backup recovery never weakens checkpoint matching.

## Custom event idempotency

`TryAppendCustomEvent` is intentionally append-only. Repeating the call creates another event because two identical payloads can represent two real occurrences.

`TryAppendCustomEventOnce` is for callbacks that may replay, such as load reconstruction, retry paths, or duplicate hooks. The caller supplies an `idempotencyKey` that identifies one logical occurrence. The identity is:

```text
caller namespace + idempotency key
```

If that identity already exists on the active branch, the API returns success without adding another event. The key is stored with the event, so deduplication survives saving and reloading. If the player rewinds to an exact checkpoint before that event existed, the key is no longer active and the occurrence can legitimately be recorded again.

Use occurrence-specific keys. Do not use a permanent key such as `promotion` if promotions can happen more than once.

## Long-campaign persistence

IMDC keeps complete source history, so retained disk history still grows with genuine event volume. Version 3.4 avoids reprocessing that complete history on every ordinary save:

- a compact v5 sidecar remains the base snapshot;
- append-only generations are written to `<sidecar>.imdc.journal`, whose header contains the SHA-256 of the exact base file it extends;
- normal save preparation copies only newly appended immutable records, not every historical event;
- journals use transactional format 2: `BEGIN`, bounded per-record NDJSON rows, then `COMMIT`; older journal formats are intentionally rejected;
- routine compaction is queued when journal bytes reach a 1-16 MiB bounded base-relative threshold; a size-scaled 2,048-32,768 transaction ceiling exists only to bound pathological replay depth;
- rewinds, destructive branch changes, recovery writes, New Save, or an incompatible baseline immediately use a full atomic snapshot instead;
- an incomplete v2 transaction is ignored, a completely written retry is idempotent by declared counts, and a mismatched journal hash is never replayed onto another base;
- when compaction creates `<sidecar>.imdc.bak`, its matching previous journal is preserved as `<sidecar>.imdc.bak.imdc.journal`; if that copy is interrupted or fails, recovery can pair the backup base with the still-present current journal only after parsing a real matching header, and a later healing write preserves that recovery journal beside the backup before removing its primary-path copy;
- missing/empty/first-header-torn journals are distinguished from real base-hash matches, so a torn preferred primary journal cannot mask a valid `.imdc.bak.imdc.journal`;
- when a physical save scope is initialized, IMDC best-effort scavenges only its exact sidecar-derived temp files that are at least 24 hours old; fresh temp files and unrelated files are left alone;
- event payloads and custom SET values cache their validated storage-form JSON, so old immutable rows are not reparsed on later saves;
- the streaming writer copies reusable character buffers directly to its `TextWriter`, avoiding a temporary string allocation for every record;
- forward-save watermarks skip complete history trim scans when no record can lie beyond the checkpoint;
- background compaction snapshots immutable persisted prefixes from the already-loaded engine and verifies the physical base hash/journal length before replacement, avoiding a second full deserialized history graph;
- base history is validated once and committed journal suffixes are validated incrementally; already-monotonic current-format event/mutation lists skip redundant sorting;
- save-boundary single chart reconciliation tracks only unresolved released singles instead of rescanning every historical single;
- runtime locks are released before serialization and durable disk I/O; physical sidecar locks are process-wide per canonical path so replacement engines and old background compactors cannot race the same files;
- `TryGetPersistenceDiagnostics` exposes counts, base/journal sizes, last persistence mode, recovery/block state, and generation information without performing I/O.

An ordered save transport is an optional optimization and coordination provider. IMDC discovers companion transports by reflection only, preferring authoritative + healthy `SaveNLoadFixes.SaveTransportApi`, then healthy `SaveWriteOrderingFix.SaveWriteOrderingApi`. IMDC skips its standalone full `SavedData` JSON clone only when the selected provider positively proves healthy SavedData transport; assembly presence alone is insufficient. If no provider is proven healthy, IMDC keeps the defensive clone. The standalone path has layered detachment: the normal `JsonUtility` round trip followed, if reconstruction fails, by a Unity-serialized-field graph clone whose compact JSON is checked against the original whenever that original JSON was available. The fallback deliberately uses only `JsonUtility` APIs available in Idol Manager's Unity runtime. IMDC therefore does not immediately hand vanilla the live `SaveManager.Data` graph merely because the first JSON reconstruction failed.

Save-directory deletion and IMDC archival use the same effective-provider decision. IMDC acquires at most one directory-scoped exclusive lease, directly from authoritative SNLF when selected or otherwise from healthy SWOF. This drains earlier ordered writes before deletion and keeps later writes from crossing the delete/archive boundary without double-locking two transport coordinators. If a selected provider cannot grant the boundary, IMDC leaves the save in place rather than accepting a deletion that a queued writer could resurrect. If neither provider is healthy, IMDC keeps its standalone deletion/archive safety path.

Harmony-veto interoperability follows a conservative rule: missing `__state` is unknown, never an empty/default historical state, and lifecycle events require a real after-state transition. IMDC explicitly snapshots before Unavailable Idols Fix on single removal and medical transitions, and before Assistant Manager on `loans.AddLoan`.

## Substory completion after load

Vanilla persists its dialogue queue. IMDC 3.4 rebuilds its transient pending-substory completion counters from that restored queue after load. A dialogue queued before saving can therefore still produce its normal `substory_completed` event after the save is reloaded and the dialogue eventually closes.

## Current-format-only persistence

IMDC 3.4.25 reads only sidecar format 5 and replays only transactional journal format 2 for the matching compact-base generation. Journal headers are decoded through a version-agnostic affinity envelope first: an unsupported IMDC journal bound to a different compact-base SHA-256 is treated as a stale generation suffix, while an unsupported journal whose hash matches the candidate base fails closed and write-protects that save scope because it may contain authoritative committed state. An unsupported primary sidecar likewise blocks backup fallback, preventing an older runtime from healing newer/unknown semantics backward. Older lightweight sidecars are not migrated by the normal runtime.

The staged v6 event codec carries `ParticipantSchemaVersion`. Repository history places the released canonical shared-envelope participant contract in sidecar format v2, so migrated v2-v5 shared candidates remain strict. A bounded v1 archival/synthetic shared candidate may derive only a redundant participant count from an authoritative stored ID list; if the stored row still cannot prove participant identity, the occurrence remains durable with `ParticipantKnownness = Unknown` and is not projected onto guessed idols. Current contradictory list/count/pair metadata remains malformed and quarantined.

Pre-2.0 database persistence is also not imported by the runtime mod. Historical migration belongs in a separate purpose-built utility.

## Source versions and generated build artifacts

The source of truth for this development tree is the checked-out source plus project/mod metadata. `IM Data Core.csproj` and `assets/info.json` carry the mod version. Generated DLLs, PDBs, `bin/`, `obj/`, and `artifacts/` are build outputs and are ignored by the repository. A stale locally bundled DLL can therefore report revision metadata from an older build even when the source tree is correct; rebuild the mod from the desired commit instead of treating that generated DLL metadata as an IMDC runtime/source defect.

## Custom-data behavior

Consumer custom values must be valid JSON documents. IMDC normalizes them before mutation history is recorded.

Current quotas:

- maximum 4,096 keys per namespace
- an individual normalized value may use up to the namespace budget
- maximum 5 MiB normalized JSON character budget per namespace

Quota accounting is maintained incrementally rather than rescanning the entire namespace on each SET. A SET to the already-materialized value and a REMOVE of a missing key are logical no-ops and do not grow mutation history.

## Public API

Preferred type: `IMDataCoreApi`. The compatibility alias `IMDataCoreAPI` remains available.

```csharp
bool IMDataCoreApi.IsReady();

bool IMDataCoreApi.TryRegisterNamespace(
    string namespaceIdentifier,
    out IMDataCoreSession session,
    out string errorMessage);

bool IMDataCoreApi.TrySetCustomJson(
    IMDataCoreSession session,
    string dataKey,
    string jsonValue,
    out string errorMessage);

bool IMDataCoreApi.TryGetCustomJson(
    IMDataCoreSession session,
    string dataKey,
    out string jsonValue,
    out string errorMessage);

// Optional reflection-based integrations can use IMDataCoreInteropApi and pass
// their own Assembly explicitly when caller identity must survive MethodInfo.Invoke.

bool IMDataCoreApi.TryRemoveCustomJson(
    IMDataCoreSession session,
    string dataKey,
    out string errorMessage);

bool IMDataCoreApi.TryAppendCustomEvent(
    IMDataCoreSession session,
    int idolId,
    string entityKind,
    string entityId,
    string eventType,
    string payloadJson,
    string sourcePatch,
    out string errorMessage);

bool IMDataCoreApi.TryAppendCustomEventOnce(
    IMDataCoreSession session,
    string idempotencyKey,
    int idolId,
    string entityKind,
    string entityId,
    string eventType,
    string payloadJson,
    string sourcePatch,
    out string errorMessage);

bool IMDataCoreApi.TryReadEventsForIdolPage(
    int idolId,
    long beforeEventIdExclusive,
    int maxCount,
    out List<IMDataCoreEvent> events,
    out bool hasMore,
    out string errorMessage);


bool IMDataCoreApi.TryReadHistoryPage(
    long beforeEventIdExclusive,
    int maxCount,
    out List<IMDataCoreEvent> events,
    out bool hasMore,
    out string errorMessage);

bool IMDataCoreApi.TryReadMoneyTransactionsPage(
    DateTime startInclusive,
    DateTime endExclusive,
    long afterEventIdExclusive,
    int maxCount,
    out List<IMDataCoreMoneyTransaction> transactions,
    out bool hasMore,
    out string errorMessage);
```

See [`docs/START_HERE.md`](docs/START_HERE.md), [`docs/COOKBOOK.md`](docs/COOKBOOK.md), and [`templates/IMDataCore.TemplateMod`](templates/IMDataCore.TemplateMod/) for integration examples.

## Repository layout

```text
IM Data Core/
├── IM Data Core.csproj
├── README.md
├── CHANGELOG.md
├── assets/
│   ├── info.json
│   └── steam description.txt
├── docs/
├── scripts/
├── src/
└── templates/
```

## Build

From the Cosmo Mod Library root:

```powershell
dotnet build "mods\IM Data Core\IM Data Core.csproj" -c Release
```

The repository's shared `Directory.Build.props` supplies the framework, Harmony, Unity, and Idol Manager references.

## Portrait identity

Idol lifecycle events preserve the raw idol type, custom-id/addressable identity, and exact body/hair/face/accessory asset IDs. Consumers can use these vanilla-style references without persisting rendered portrait images.
