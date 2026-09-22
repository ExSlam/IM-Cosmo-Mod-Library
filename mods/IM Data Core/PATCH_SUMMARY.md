# IM Data Core 3.4.33 patch summary

## Current implementation and qualification (2026-09-09)

The active loader accepts only native sidecar v6 / journal v3. It rejects older and newer unsupported generations without conversion or backup downgrade. Unknown forward-envelope versions are protected before parsing schema-dependent fields.

Current evidence is recorded in [V6V3_QUALIFICATION_STATUS.md](docs/V6V3_QUALIFICATION_STATUS.md). Native Unity tests pass selected persistence, consumer, Save As and normal Save & Quit/restart cases, including SNLF/SWOF combinations. Native testing also found and fixed omitted GD record lists and IMDC random-event effect arrays. The complete 23-regression gate, visual UI checks and interruption/stress cases remain open.


> **Current storage compatibility policy (3.4.34+):** IM Data Core supports only **sidecar v6 + journal v3**. Sidecar v1-v5 and journal v1-v2 are unsupported release inputs. IMDC does **not** promise, qualify, or require in-place migration, conversion, adoption, or rewrite from those older storage generations. Encountering an unsupported older generation must fail closed without converting it or overwriting its bytes. Any v5/v2 migration language retained below is historical design/task context, not a current compatibility commitment.

Historical task entries below intentionally retain the storage version that was live at that task checkpoint; they do not override the current v6/v3-only support policy.

## 3.4.33 Wave 3 Task 3 - namespace bootstrap and reentrancy hardening

1. **Registration ordering:** consumer code calls `TryRegisterNamespace(...)` directly at a safe gameplay point; `IsReady()` is observational rather than a prerequisite, so consumer/IMDC Harmony postfix order cannot permanently suppress registration.
2. **Bounded retry:** template/onboarding guidance treats a false registration result as visible failure that may receive a bounded later retry, never as a reason to poll `IsReady()` indefinitely.
3. **Cancellation precondition:** `business.CancelContract(...)` snapshots exact `ActiveProposals` membership plus immutable history identity/payload before vanilla mutation.
4. **Cancellation postcondition:** `contract_cancelled` is emitted only after the exact snapshotted proposal reference is absent; detached/stale/repeated calls and exceptions do not create lifecycle terminals.
5. **Independent money observation:** liability deductions remain independently observable through the money ledger even if the lifecycle postcondition is false.
6. **Nested activity/crisis context:** activity-income and concert-crisis ambient attribution use thread-local frame stacks and restore the previous outer frame from the owning finalizer.
7. **Nested scandal/blackmail context:** scandal parameter mutation uses thread-local depth, while blackmail trigger results use thread-local frames so nested calls cannot clear outer state.
8. **Nested money context:** money attribution uses owned thread-local frames plus transient exact-use frames; frames are installed before detail construction and restored only by the owning finalizer, including exception paths.
9. **Storage/catalog stability:** no event or payload schema migration is introduced; live sidecar/journal remain v5/v2 and the Event Catalog remains 173 queryable types across 41 domains.
10. **Verification:** two new deterministic source/fixture tests raise the cumulative suite from 50 to 52 tests.

## 3.4.32 Wave 3 Task 2 - historical references and lifetime semantics

1. **Money reference schema:** current money-detail rows are stamped `reference_schema_version = 1`; legacy rows remain schema 0 and expose explicit false knownness instead of accidental default-zero references.
2. **Staff references:** salary and severance money rows carry the durable vanilla staff ID plus knownness while retaining human-readable labels.
3. **Facility references:** theater and cafe money rows carry the canonical IMDataCore facility generation plus facility kind, so raw vanilla ID reuse does not merge distinct facility lifetimes.
4. **Project/member references:** single, show, cafe, and concert details carry structured project/member IDs as true JSON arrays where applicable; active-contract money details reuse the same contract history resolver as lifecycle history.
5. **Natural loan maturity:** `loans.OnNewWeek()` emits `loan_matured` only for exact loans that were active and overdue before vanilla processing, are inactive afterward, and remain in `loans.Loans`; payoff/cancellation terminals remain distinct.
6. **Maturity timing:** the terminal includes the actual weekly processing date separately from the contractual loan end date.
7. **Election cancellation linkage:** the election parent is snapshotted before the UI cancellation path clears `ReleaseSingle`, then transferred to `single_cancelled` with explicit knownness; direct/legacy callers remain conservative.
8. **Transient safety:** election-cancellation context is thread-scoped, exact-reference matched, nesting-safe, and cleared from a Harmony finalizer even when vanilla throws.
9. **Storage ownership:** no sidecar/journal migration is introduced; live persistence remains v5/v2 while the previously staged v6/v3 code remains disabled.
10. **Verification:** two new deterministic source/fixture tests raise the cumulative suite from 48 to 50; the regenerated Event Catalog contains 173 queryable types across 41 domains.

## 3.4.31 Wave 3 Task 1 - payload/timing semantics

1. **Single appeal semantics:** `single_fan_appeal_*` reads the release-time `single.FanAppeal`; the opinion-time `ReleaseData.FanAppeal` is preserved separately as `single_opinion_fan_appeal_*`.
2. **Contract presentation timing:** `contract_window_opened` is emitted from `business.SetProposal(...)`, not from later contract activation, and remains correlated to the proposal `p:<guid>`.
3. **Substory queue versus presentation:** new captures distinguish `substory_queued` from `substory_presented`; ordinary dialogue actor state is observed after vanilla `BeforeStart`, scene presentation reuses the existing `ss:<guid>` occurrence, and legacy `substory_started` rows remain readable.
4. **Departure provenance:** audited graduation/fire/marriage/bankruptcy/story caller scopes annotate departure history with controlled cause/source fields; unknown callers remain `unknown`.
5. **Structured task constraints:** task lifecycle payloads expose single genre/lyrics and show genre/medium IDs plus titles without parsing localized descriptions.
6. **Status provenance:** medical, room-practice, cafe, scene, graduation, and story status transitions carry controlled cause/source-kind attribution while the shared `SetStatus` sink remains observational.
7. **Blackmail timing:** one history-only `bm:<guid>` links enqueue, trigger, and source-proven dequeue; queue-size names match actual checkpoints, and influence reward is `planned` with applied-known false at trigger/dequeue time.
8. **Storage ownership:** no sidecar/journal migration or new gameplay-state restoration is introduced; live persistence remains v5/v2.
9. **Verification:** two new deterministic source/fixture tests raise the cumulative suite from 46 to 48; the regenerated Event Catalog contains 172 queryable types across 41 domains.

## 3.4.30 Wave 2 Task 7 - lifecycle/history closure

1. **Clique succession:** `Relationships._clique.AddMember(...)` snapshots membership and leader before vanilla `UpdateLeader()`, so `clique_joined` preserves truthful previous/current leader context and suppresses no-op joins.
2. **Training/treatment lifecycle:** training and medical-recovery assignment emit one shared `room_work_assigned`; direct `FinishPractice(true)` / `CancelTreatment()` terminals are captured without duplicating outer `CancelJob()` and natural treatment completion remains completion, not cancellation.
3. **Progression milestones:** `_activity.LevelUp()`, `tasks.AddTask_SummerGames(...)`, and `SEvent_Tour.country.LevelUp()` now emit `activity_level_up`, `summer_games_objective_activated`, and `tour_country_level_up` only after proven state transitions.
4. **VN direct status restores:** the audited chapter-5/chapter-6 direct `girl.status = normal` paths feed the existing status-history seam without changing story state or repairing anything.
5. **Semantic chronology:** bounded capture scopes defer nested child rows without assigning sequence numbers until the prerequisite outer event is recorded, fixing the audited loan/show/room/business/clique inversions without global timeline sorting.
6. **Shared group rows:** `group_created` / `group_disbanded` are persisted once with the member list and exposed to per-idol queries through shared participant indexing instead of one physical duplicate row per member.
7. **Chart milestone split:** chart resolution emits `single_chart_result` with release/cast/chart context and never calls the `single_released` producer, keeping release occurrences immutable.
8. **Storage ownership:** no new live continuation fields or checkpoint schema are introduced; sidecar format 5 / journal format 2 remain current.
9. **Verification:** two new deterministic source/fixture tests raise the cumulative suite from 44 to 46; the regenerated Event Catalog contains 169 queryable types across 41 domains.

## 3.4.29 Wave 2 Task 6 - split event/relationship history

1. **Relationship birth:** `Relationships.GetRelationship(...)` emits `idol_relationship_created` only for a proven exact runtime insertion and preserves the Initialize()-chosen `Dynamic`; save/load bootstrap is suppressed.
2. **Random-event occurrence:** successful starts allocate history-only `re:<guid>` IDs; definition IDs remain reusable streams rather than occurrence identities.
3. **Selected contract:** business-conditioned random events preserve the exact selected active-contract history ID through start and terminal using the existing durable contract resolver.
4. **Random terminal recovery:** load/F9 transient reset can recover the newest still-open `re:` from active-branch history without restoring Event_Manager state.
5. **Template lifecycle:** `template_event_presented` / `template_event_concluded` use `te:<guid>` correlation and preserve actors, selected parts, exact displayed replies, semantic variables, and selected reply/action context.
6. **No template reroll:** displayed reply variants are passively observed at `Event_ReplyButton.Set(...)`; IMDC never calls `Active_Template.GetReplies()`.
7. **SNS-only terminal:** exact `<OpenPopup>d__46.MoveNext` completion emits one `random_event_concluded` with truthful terminal state, `reply_effects_applied = false`, and `resource_delta_known = false`; IMDC never repairs state.
8. **Forced-breakup action:** `Date_Popup.OnClick_ForceBreakup()` emits one `player_forced_breakup` with target/partner, relationship before/after, and influence delta. Capture brackets SNLF when present but never calls breakup/status repair.
9. **Storage/verification:** live persistence stays v5/v2; two new deterministic tests raise the suite to 44; the Event Catalog contains 164 queryable types across 41 domains.

## 3.4.25 Wave 2 Task 2 - business proposals and developing-loan terminals

1. **Proposal occurrence:** `business.SetProposal(...)` Postfix allocates one history-only `p:<guid>` at the completed proposal/popup presentation boundary.
2. **Symmetric terminals:** `business.Accept()` and `business.Decline()` snapshot `ActiveProposal` before vanilla mutation and emit accepted/declined rows under the same occurrence; a direct/modded bypass can receive a terminal ID without fabricating a generated row.
3. **Candidate and negotiation context:** each proposal row preserves candidate order, selected idol, final nullable negotiation state, coefficient, attempt count, raw/effective values, stamina/liability/duration, and available staff/presentation metadata without invoking RNG.
4. **Contract correlation:** a continuing accepted proposal stores the exact resulting contract history `EntityId` selected by the existing contract-acceptance resolver, so v5 legacy keys and future v6 `g:` generations remain aligned.
5. **Decline history:** declined offers retain the complete pre-decline proposal snapshot and explicitly carry no resulting contract reference.
6. **Developing-loan terminal:** the existing `agency._room.CancelJob()` patch now snapshots a developing room loan and emits `loan_cancelled` only after proving the same reference changed from contained in `loans.Loans` to absent.
7. **Payoff distinction:** loan lifecycle payloads expose collection membership before/after; payoff remains a retained-object active -> inactive terminal and cannot be mistaken for cancellation/removal.
8. **Branch cleanup/storage:** transient proposal correlation is cleared on load/F9; no new checkpoint state is added and live sidecar/journal formats remain v5/v2.
9. **Verification:** two new deterministic tests raise the cumulative source/fixture suite to 36 tests; all pre-existing and new tests pass.

## 3.4.24 Wave 2 Task 1 - people/auditions/hire/date history

1. **Audition occurrence:** committed auditions allocate one `a:<guid>` history occurrence and use it for start, cost, generated-slate, and completion rows.
2. **Candidate slate:** `Auditions.GenerateGirls(...)` Postfix snapshots the authoritative generated candidates with occurrence-local `:candidate:<ordinal>` IDs and consumer-useful generated profiles.
3. **Audition terminal:** `Popup_Audition.Close()` Postfix records hired/rejected outcomes from actual roster containment and then retires the runtime audition context.
4. **Hire baseline:** `idol_hired` records provenance plus names/profile/trait/sexuality/birthday/peak age and parameter backing values/potentials; `GetPotential()` is never invoked by capture.
5. **Generic-date timing:** `Dating.GenerateGenericDate(...)` records location/masks under a `d:<guid>` occurrence; completion waits until the `vn_actions.Do` Postfix after `dating/add_points`.
6. **Flirt semantics:** `Date_Flirt.DoFlirt(...)` records the post-applied semantic outcome and before/after dating knowledge/counters without rendered dialogue or RNG interception.
7. **Branch cleanup:** audition/date transient runtime observations are cleared with the existing load/F9 identity reset and never become current-state checkpoint data.
8. **Verification:** two new deterministic tests raise the cumulative source/fixture suite to 34 tests; live storage stays v5/v2.

## 3.4.23 Wave 1 shared identity compatibility

1. **Explicit candidate branch state:** staged v6 checkpoints now require `IdentityCandidates`; migrated v5 checkpoints remain explicitly legacy-unbound.
2. **Deterministic adoption:** exact legacy-unbound loads derive forward `g:` / `q:` / `b:` / `t:` IDs from checkpoint identity, family locator/witness, and a migration salt, with no synthetic birth rows.
3. **No false aliases:** legacy keys map to zero, one, or many canonical candidates. `Exact` requires an independently proven bounded sequence interval; otherwise candidate presence is `Ambiguous`.
4. **Branch-safe F9:** exact checkpoint preparation clears and replaces candidate/generation caches before family rebind/adoption.
5. **Public #66 resolver:** preferred, reflection-friendly, and uppercase facades expose current identity and legacy-candidate resolution through stable descriptor strings, never CLR object references.
6. **Live-v5 truthfulness:** room/theater/cafe and SSK/tour room-work can resolve from existing durable v5 state; four opaque generations and durable candidate lookup stay unresolved until v6 is live.
7. **Verification:** new deterministic fixture/source tests raise the cumulative suite to 32 tests, and every full v6 checkpoint fixture now carries explicit candidate state.

## 3.4.22 Wave 1 identity 5 - room-work SSK/tour namespace

1. **Independent owner domains:** SSK and tour keep their separately persisted vanilla project IDs; no new random work generation is introduced.
2. **Canonical child namespace:** room-work target identity is `g:<room>:ssk:<SSK.ID>` for SSK and `g:<room>:tour:<tour.ID>` for tour.
3. **Payload alignment:** `room_work_kind` uses the same `ssk` / `tour` owner token as the timeline child key.
4. **Completion symmetry:** `DoSSKProduction` and `DoTourProduction` completion snapshots use the canonical owner token.
5. **Cancellation symmetry:** `CancelJob()` snapshots use the identical owner token, so completion and cancellation for one project remain on one target stream.
6. **No fifth checkpoint binding:** the room generation is already checkpointed and vanilla already saves the two project-ID domains.
7. **Legacy preservation:** historical `g:<room>:event:<N>` rows and `room_work_kind = event` payloads are not rewritten or guessed during migration.
8. **Ambiguity staged for compatibility:** an old `event:<N>` row may have zero, one, or multiple canonical candidates; the Task-15 collision fixture deliberately carries both SSK and tour candidates with `Ambiguous` quality and no selected exact target.
9. **Live v5 safe:** unlike opaque contract/clique/bullying/task generations, this namespace split is immediately durable on sidecar v5 / journal v2.
10. **Verification:** Task-15 source/fixture regressions raise the cumulative deterministic suite to 30 tests.

## 3.4.21 Wave 1 identity 4 - generated task occurrence

1. **Authoritative birth seam:** `tasks.GenerateTask(...)` now snapshots active task references and emits one `task_added` only when exactly one new non-custom task object is proven.
2. **Opaque occurrence identity:** every observed generated non-custom task receives one `t:<guid>` generation; custom/scripted task IDs keep their existing definition-stream behavior.
3. **Legacy key demoted:** `type|goal|girl` remains only a compatibility candidate. Randomized requirements omitted from that tuple never define occurrence identity.
4. **Content is not identity:** the exact Task-14 fixture includes a later occurrence whose complete saved `TaskData` row and SHA-256 witness are identical to an older occurrence while its `t:` generation remains distinct.
5. **Exact checkpoint locator:** generated tasks bind by serialized `tasks__TaskData` ordinal plus full-row SHA-256 witness across Type/Goal/Substory, single/show constraints, Girl, AgentName, Skill, Custom, AvailableFrom, and Fulfilled.
6. **Exact reconstruction:** after `tasks.LoadFunction`, rebinding requires ordinal, witness, empty `Custom`, and reconstructed-row equality.
7. **Terminal lifetime:** completion/failure/done/graduation-removal use the canonical generation when v6 is live; removed-task bindings retire only after terminal history is enqueued/flushed.
8. **No synthetic migration birth:** legacy/adopted tasks do not receive `task_added` merely because they exist at an exact load boundary.
9. **Safe v5 improvement:** finding #27's birth row is emitted on live v5 using the existing coarse key; canonical `t:` history remains gated until sidecar v6 / journal v3 can persist exact bindings.
10. **Verification:** Task-14 source/fixture regressions raise the cumulative deterministic suite to 28 tests.

## 3.4.20 Wave 1 identity 3 - bullying episode generation

1. **Opaque episode identity:** one real false -> true `AddBulliedGirl` transition allocates one IMDC-owned `b:<guid>` generation. The leader is not part of canonical identity.
2. **Stable through leader succession:** start/terminal capture keeps one `b:` episode while the clique leader changes; parent `q:` generation is correlation metadata and old `leader|target` forms are compatibility candidates only.
3. **Fresh recurrence:** once an episode ends and its binding retires, bullying the same target again later allocates a new `b:` ID even under the same clique generation and identical structural witness.
4. **Nested terminal safety:** `BullyingStateSnapshot` carries the episode plus parent clique generation through `_clique.StopBullying`, relationship-level stop logic, and recursive clique `Quit`. Retirement occurs only after terminal history is enqueued.
5. **Exact child checkpoint locator:** active episodes bind through the parent `Relationships__Cliques` ordinal + parent `q:` generation + `bullied_target:<idolId>` + target-specific SHA-256 witness.
6. **Parent-first reconstruction:** staged v6 checkpoint projection records clique bindings before bullying children, and `Relationships.LoadFunction` associates clique generations before episode children. Missing/mismatched parent generations never rebind a child.
7. **Schema hardening:** v6 validation now rejects malformed bullied-target locators, missing parent clique generations, and duplicate family/container/ordinal/parent/child locators.
8. **Payload correlation without v5 leak:** canonical v6 bullying payloads can include `clique_generation_id`; the field is omitted while the live v5 identity gate is off, preserving the current wire shape.
9. **No half-upgrade:** canonical bullying `b:` emission remains gated until the live sidecar generation is v6. Version 3.4.20 still writes v5/v2 and therefore retains legacy leader/target EntityIds on live rows.
10. **Verification:** Task-13 fixtures prove leader succession continuity and later same-target recurrence under the same parent clique/witness with a distinct episode generation.

## 3.4.19 Wave 1 identity 2 - clique generation

1. **Opaque clique lifetime:** every forward clique receives one IMDC-owned `q:<guid>` generation at authoritative `Relationships.StartNewClique` birth rather than deriving canonical identity from current members.
2. **Mutable signatures demoted:** join/leave capture keeps `CliqueSignature` as payload/legacy discovery state, while the staged v6 EntityId resolver keeps one generation through member and leader mutation.
3. **Recursive quit safety:** `CliqueQuitSnapshot` retains the pre-mutation generation so nested last-member `Quit` callbacks cannot lose identity after the outer frame removes the clique. Runtime identity retires only after leave plus induced bullying terminal capture has completed and the clique is absent.
4. **Exact checkpoint locator:** live cliques bind through serialized `Relationships__Cliques` ordinal plus SHA-256 over the complete saved clique row, including ordered member/bullying collections and stopped-bullying child rows.
5. **Exact reconstruction:** after `Relationships.LoadFunction`, rebinding requires ordinal, witness, and reconstructed-row equality. A mismatch is never rebound by member signature alone.
6. **Legacy candidate only:** sorted-member signatures are retained only in `LegacyCandidateKeys`; the same signature or even the same complete saved-row fingerprint may belong to separate generations at different exact checkpoints.
7. **Cumulative projection:** exact checkpoint projection now combines the already-staged contract bindings and clique bindings. Bullying and generated-task generation identities remain separate later Wave-1 tasks.
8. **No synthetic birth history:** the private `StartNewClique` patch allocates identity only. It does not invent the separately planned clique-birth historical event.
9. **No half-upgrade:** canonical clique generation emission remains gated until the live sidecar generation is v6. Version 3.4.19 still writes v5/v2 and therefore retains legacy clique signatures on live event rows.
10. **Verification:** Task-12 fixtures prove one clique keeps one generation while its mutable signature changes and a later new clique can reuse an identical saved-row witness without inheriting the older generation.

## 3.4.18 Wave 1 identity 1 - contract generation

1. **Opaque occurrence identity:** every accepted/activated contract receives an IMDC-owned `g:<guid>` generation in the staged v6 identity runtime rather than deriving canonical identity from idol/type/end-date terms.
2. **Nested handoff correctness:** `business.Accept` reserves the generation before vanilla enters nested `AddActiveProposal`; a nesting-safe pending frame and `ContractAcceptedSnapshot` carry the same identity through activation and outer acceptance. Finalizer cleanup prevents a failed acceptance from contaminating the next contract.
3. **Direct insertion:** a direct/modded `AddActiveProposal` call with no matching acceptance frame allocates its own fresh generation.
4. **Exact checkpoint locator:** active contracts bind through serialized `business__ActiveProposalsData` ordinal plus SHA-256 over exactly the saved row fields. Equal fingerprints at different ordinals remain separate generations.
5. **Legacy candidate only:** the old `idol|type|end-day` key is stored only in `LegacyCandidateKeys`. Task 11 creates no unconditional old-key -> generation alias.
6. **Exact reconstruction:** after `business.LoadFunction`, a staged v6 binding is accepted only when its ordinal exists, its saved-row fingerprint matches, and the reconstructed `active_proposal` exactly matches the saved row.
7. **Lifecycle continuity:** acceptance, activation/window-open, weekly accrual, cancellation, natural completion, and break captures all route through one generation-aware entity resolver. Terminal object bindings retire only after the terminal occurrence is enqueued/observed removed.
8. **Checkpoint-clone repair:** generic storage-engine checkpoint cloning now preserves `IdentityBindingsVersion`, `IdentityBindingsComplete`, and the full binding collection.
9. **No half-upgrade:** canonical contract generation emission remains gated until the live sidecar generation is v6. Version 3.4.18 still writes v5/v2 and therefore retains legacy contract `EntityId` values on the live wire.
10. **Verification:** the Task-11 fixture deliberately uses two byte-identical saved contract rows with the same legacy candidate key and proves ordinal + witness keeps their two generations distinct.


## 3.4.17 Wave 0 task 10 - migration provenance and fail-closed downgrade

1. **Required v6 provenance:** every staged v6 document carries `MigrationProvenance`; missing/unsupported provenance fails validation instead of becoming implicit native state.
2. **Native versus converted identity:** `native_v6` records contain no legacy conversion metadata. `legacy_migration` records retain source format/version, normalized source-document SHA-256, source sequence high watermark, target v6/v3 generation, and deterministic conversion ID.
3. **Idempotent conversion identity:** conversion ID is SHA-256 over canonical provenance plus normalized save scope, with no wall-clock input. A committed v6 migration is therefore recognizable as already converted.
4. **Non-rewinding physical provenance:** F9/branch selection rewinds events, coverage transitions, identity bindings, and baseline assertions where appropriate, but never rewrites migration source/conversion identity.
5. **No backdated coverage:** migrated v6 documents cannot carry `CareerStart` or `LateAdoption`; current repaired coverage may begin later as exact `LegacyResume`.
6. **Unsupported primary preservation:** the live v5 reader raises a typed unsupported-generation signal. `Initialize(...)` blocks backup recovery and persistence for that save scope rather than loading an older `.imdc.bak` and later overwriting authoritative unsupported bytes.
7. **Matching unsupported journal preservation:** after #58 establishes base affinity, an unsupported matching-base journal also write-protects the generation. An unsupported wrong-hash journal remains a stale suffix and does not poison a healthy base.
8. **Runtime isolation:** live wire constants remain sidecar 5 / journal 2. Task 10 closes Wave-0 storage foundation work without enabling normal v6/v3 publication.
9. **Verification:** Task-10 source/contract fixtures cover native/migrated provenance, bad conversion identity, backdated coverage rejection, unsupported-primary preservation, matching unsupported-journal preservation, and #58 stale wrong-hash continuity.

## 3.4.16 Wave 0 task 9 - bounded historical-baseline assertions

1. **Allow-listed v6 branch carrier:** staged v6 now requires `HistoricalBaselineAssertions`; the only current `BaselineKind` is `group_target_audience_origin`.
2. **No generic bootstrap bag:** assertions cannot mirror repaired vanilla current state or manufacture no-evidence placeholders. Migration initializes the collection empty.
3. **Explicit historical quality:** `Exact` requires exactly one proven candidate on all three axes; `Ambiguous` preserves complete multi-candidate evidence without selecting a trio; `Unknown` preserves bounded partial evidence while leaving at least one axis unresolved.
4. **Exact load boundary:** repaired-save and legacy point-pattern assertions require the same content-fingerprinted `AnchorCheckpointKey` used by coverage transitions and must be ordered after that checkpoint.
5. **Deterministic semantic identity:** `AssertionId` is content-addressed independently of the allocated sequence, preventing repeated activation of the same semantic source boundary from becoming competing origins.
6. **Shared-sequence rewind semantics:** assertion sequences collide-check against events/custom mutations/coverage transitions and assertions rewind with branch state, unlike capability descriptors and #60 owner provenance.
7. **Journal-v3 completion:** `HISTORICAL_BASELINE_ASSERTION` is implemented under the existing seven-family BEGIN/COMMIT envelope; no frozen v3 semantic row kind remains deferred.
8. **Public-quality staging:** `IMDataCoreHistoricalBaselineQuality` and `IMDataCoreHistoricalBaselineAssertion` are public types, while live retrieval remains unadvertised until v6/v3 publication exists.
9. **Runtime isolation:** live constants remain sidecar 5 / journal 2; the v5 compact and v2 journal writers still contain no historical-baseline collection.


## 3.4.15 Wave 0 task 8 - coverage/capability/namespace structures

1. **Explicit v6 coverage model:** staged v6 now requires `CoverageModelVersion = 1`, `CoverageCapabilitySets`, and `CoverageTransitions`; absence is not silently treated as complete-empty.
2. **Immutable semantic descriptors:** capability-set IDs are SHA-256 identities over canonical scope/owner plus sorted semantic `{Token, Revision}` pairs. Package version, sidecar format, and observed event-name sets are not substitutes.
3. **Owner-bound namespace capability:** namespace descriptors require the latest known #60 owner lineage. Durable owner registration alone does not begin consumer coverage.
4. **Branch-owned exact boundaries:** coverage transitions consume the shared monotonic event/custom sequence space and may carry an exact content-fingerprinted checkpoint anchor. `CareerStart` is unanchored; loaded-career `LateAdoption`, `LegacyResume`, and process-gap boundaries require an exact anchor.
5. **Conservative migration:** v1-v5 conversion creates the required empty coverage model only. It does not infer historical completeness from format, package version, `EnabledMods`, first row, current runtime, or legacy namespace presence.
6. **v3 atomic publication:** staged journal-v3 now serializes/replays `COVERAGE_CAPABILITY_SET`, `NAMESPACE_OWNER_BINDING`, and `COVERAGE_TRANSITION` rows under the existing transaction counts and validates descriptor/owner/anchor references after commit.
7. **Shared-sequence integrity:** coverage transition sequences are collision-checked against events and custom mutations, remain monotonic but need not be dense, and participate in the issued high watermark.
8. **Task boundary retained:** `HISTORICAL_BASELINE_ASSERTION` remains the only deferred v3 semantic row family. Structured public coverage/knownness APIs remain scheduled for Wave 4 rather than being claimed early.
9. **Runtime isolation:** normal persistence constants remain sidecar 5 / journal 2; live v5 serialization contains none of the staged coverage fields.

## 3.4.14 Wave 0 task 7 - durable namespace-owner provenance

1. **Document-level owner provenance:** staged sidecar-v6 gains `NamespaceOwnerBindings`, separate from rewindable checkpoints. A populated namespaced event/custom-data scope must have a durable owner-binding revision chain.
2. **Stable lineage + rotating witness:** the v6 record stores an explicit stable owner lineage; the staged helper can derive a native assembly-lineage candidate from assembly name/culture/public-key token with assembly version deliberately excluded. The existing full-name/MVID/location/SHA-256 identity remains the strong rotating witness. Legitimate upgrade/reinstall appends a new revision under the same lineage.
3. **Immutable revision chain:** owner changes append `BindingRevision` records rather than rewriting the previous provenance row. Same owner + unchanged schema/witness is restart-idempotent. The v3 journal can therefore publish actual namespace-owner changes under its existing monotonic count model without a journal-v4 bump.
4. **Legacy truthfulness:** v1-v5 migration creates only deterministic revision-1 `legacy_unbound` records for namespace tokens proven by namespaced events/custom mutations. Owner ID, witness, mod title, author, DLL name, and checkpoint inventory are never invented as migration certainty.
5. **No first-claimant adoption:** ordinary owner refresh refuses legacy-unbound records. The separate adoption helper requires an explicit migration authorization and records the next revision as `migration_adopted`.
6. **Collision/upgrade rules:** a known binding may rotate its strong witness only when stable lineage matches. A different stable lineage is rejected; prior strong witnesses remain in the immutable revision trail.
7. **v3 atomic parity:** `NAMESPACE_OWNER_BINDING` is no longer a deferred row kind. Staged journal-v3 write/replay counts, serializes, validates, and commits owner-binding revisions atomically with other transaction rows. Coverage/baseline row families remain deferred.
8. **Runtime isolation:** normal runtime constants remain sidecar 5 / journal 2. Live v5 serialization intentionally omits `NamespaceOwnerBindings`; Task 7 stages the v6/v3 ownership contract without claiming an early cutover.

## 3.4.13 Wave 0 task 6 - loaded shared-row compaction equivalence

1. **Legacy envelope normalization:** loaded per-idol `show_cast_changed` rows are converted to the canonical shared `IdolId = -1` envelope only when their immutable stored payload validates as a complete participant set containing the historical envelope idol.
2. **Occurrence equivalence is stronger than timestamp:** duplicate legacy fan-out collapse requires exact payload plus the same historical `SourcePatch` inside the same show/timestamp bucket. The earliest sequence representative wins.
3. **Canonical dominance removed on disk load:** a canonical post-mod row no longer deletes every noncanonical row sharing `(show, GameDateTime)`. Payload-distinct or source-distinct occurrences remain independent.
4. **No live-state reconstruction:** invalid or incomplete participant metadata is left unnormalized and remains subject to finding #57's strict knownness/quarantine rules; current show objects are never consulted.
5. **Publication awareness:** envelope-only normalization increments an explicit load-transform counter, so even a single surviving repaired representative forces the next full compact snapshot instead of leaving the old physical representation as the append baseline.
6. **Live settlement unchanged:** `CompactPendingEvents` retains its bounded current-session canonical-settlement behavior. Task 6 changes only loaded-history normalization.
7. **Verification:** deterministic fixtures cover regression #85/#86 behavior, sequence preservation, idempotence, source compatibility, malformed-witness rejection, and canonical/legacy coexistence. Live sidecar/journal wire versions remain 5/2.

## 3.4.12 Wave 0 task 5 - version-aware shared participant compatibility

1. **Explicit v6 event discriminator:** staged sidecar-v6 and journal-v3 event rows now carry `ParticipantSchemaVersion`; live v5/v2 rows do not gain a new wire field.
2. **Source-history mapping, not missing-field inference:** repository history places the canonical shared-envelope participant model in sidecar v2. Migrated v2-v5 shared candidates therefore remain strict current-schema rows even when a required count/list/pair member is absent or contradictory.
3. **Bounded v1 compatibility:** sidecar v1 predates the shared-envelope contract. An archival/synthetic v1 shared candidate may derive only a missing redundant count from an authoritative stored participant-ID list. No idol identity is reconstructed from live objects.
4. **Explicit knownness:** shared participant resolution distinguishes exact, unknown legacy identity, and malformed current metadata. Unknown legacy rows remain in the authoritative event stream without being broadcast through the global/per-idol derived indexes.
5. **Current corruption stays quarantined:** schema-1 rows still use the strict list/count/pair parser; contradictory current-v6 metadata remains malformed and cannot be normalized merely because a list is present.
6. **v3 parity:** staged journal-v3 event rows serialize/deserialize the same v6 participant discriminator, preserving the Task-3 atomic transaction envelope.
7. **Verification:** Task-5 fixtures cover exact v1 count derivation, v1 insufficient-identity unknownness, a v2 missing-count strict negative control, and contradictory current-v6 quarantine. No compile/runtime claim is made without a .NET/Mono toolchain.

## 3.4.11 Wave 0 task 4 - journal affinity before version support

1. **Minimal affinity envelope:** journal header decoding now reads `FormatName`, declared `FormatVersion`, and a valid SHA-256 `BaseFileHash` without first requiring that transaction version to be supported.
2. **Stale generation classification:** a recognized IMDC journal whose hash differs from the candidate compact base is `HeaderMismatch` regardless of whether its version is supported. No transaction body is parsed.
3. **Matching unsupported fail-closed:** if the journal hash matches the candidate compact base but the declared version is unsupported, load still fails. Potential committed suffix state is never discarded to make the base appear loadable.
4. **Backup fallback preserved:** a preferred unsupported wrong-hash journal can no longer mask a valid sibling backup journal; a preferred matching unsupported journal remains authoritative enough to stop fallback.
5. **v2/v3 parity:** live v2 replay/append and the staged v3 migration/recovery selector share the same affinity classifier. The strict staged-v3 schema reader remains available for direct format fixtures.
6. **No format cutover:** sidecar/journal wire versions remain 5/2 in the live runtime. Task 4 changes generation selection and error classification only.
7. **Verification:** Task-4 source/contract tests cover regression #80's three header pairings plus backup fallback and staged-v3 parity. No compile/runtime claim is made without a .NET/Mono toolchain.

## 3.4.10 Wave 0 task 3 - staged journal-v3 transaction framing

1. **Frozen v3 envelope:** BEGIN/COMMIT now has a staged format-3 shape with base/target counts for checkpoints, events, custom mutations, coverage capability sets, coverage transitions, durable namespace-owner bindings, and historical baseline assertions.
2. **v6 checkpoint journal row:** staged v3 checkpoint rows serialize/deserialize the complete Task-2 v6 identity-binding checkpoint shape rather than the live v5 checkpoint shape.
3. **Atomicity retained:** target counts are monotonic, COMMIT must match BEGIN exactly, and an uncommitted/torn transaction is never materialized.
4. **Known future members reserved:** v3 row tokens for capability descriptors, coverage transitions, owner bindings, and historical baseline assertions are frozen now. Their later semantic codecs can land without inventing journal v4.
5. **Safe deferral:** a delta in a reserved extension family before its codec exists requires a full v6 snapshot rather than a partial v3 append.
6. **Runtime isolation:** normal runtime constants remain sidecar 5 / journal 2. Finding #58's minimal version-agnostic header/base-affinity logic remains the next Wave-0 task.

## 3.4.9 Wave 0 task 2 - v6 checkpoint identity-binding schema

1. **Explicit v6 binding completeness:** every logical-v6 checkpoint carries `IdentityBindingsVersion == 1`, a boolean completeness flag, and a required binding array. Migrated v1-v5 checkpoints are explicitly incomplete/legacy-unbound rather than falsely complete-empty.
2. **Four canonical generation families:** the schema covers contract, clique, bullying episode, and generated non-custom task bindings. Room-work keeps its already-checkpointed room generation and does not receive a redundant random binding.
3. **Source-matched locators:** exact row locations use the serialized Idol Manager containers `business__ActiveProposalsData`, `Relationships__Cliques`, and `tasks__TaskData`; bullying additionally identifies the parent clique generation and target child locator.
4. **Validation-only witnesses:** each binding carries a structural `sha256:` witness, native/adopted origin, coverage start sequence, and optional legacy candidate keys. These values correlate history and validate rebinding; they are not a second vanilla continuation save.
5. **Isolated v6 codec:** `SerializeV6LogicalDocumentTo` / `DeserializeV6LogicalDocument` read and write the new checkpoint shape for migration/schema work. The ordinary v5 checkpoint codec deliberately omits all identity-binding fields.
6. **No premature journal cutover:** live constants remain sidecar format 5 / journal format 2. The matching journal-v3 row/transaction shape is the next Wave-0 task.
7. **Verification:** source/fixture checks cover complete v6 bindings, migrated legacy-unbound checkpoints, duplicate canonical binding rejection, and the v5 non-leak guard. Decompiled source plus `Assembly-CSharp.dll` were checked for all three serialized-container field names.

## 3.4.8 Wave 0 task 1 - bounded legacy migration codec

1. **Frozen v1-v5 migration boundary:** a separate migration decoder accepts only released lightweight sidecar generations 1 through 5 and targets logical generation 6. Pre-2.0 database persistence stays outside this codec family.
2. **Historical v1/v2 validation retained:** migration requires `EventId == Sequence`, re-derives and verifies `GameDateKey` from `GameDateTime`, and normalizes the stringified event/custom JSON into structural storage form exactly once.
3. **Knownness is explicit:** missing v3 mod inventory is unknown, v4 has no exact content fingerprint, and an early-v5 checkpoint without `AgencyRoomIdentities` is marked legacy-unbound rather than falsely complete.
4. **Chronology is preserved:** event/custom sequence numbers and source ordering are never renumbered or synthesized; invalid duplicates/out-of-order rows fail closed.
5. **No premature live-format cutover:** the normal reader/writer remains exact sidecar v5 / journal v2. The migration result is not published until later Wave-0 work completes the v6/v3 storage foundation.
6. **Verification:** added positive v1-v5 fixtures, current-v5 room-binding coverage, and negative v1/v2 historical-validation fixtures. The supplied game DLL reference set was checked for all project references; no .NET compiler was available in the execution environment.

## 3.4.8 current-format cleanup

1. **Exact v5 runtime policy:** runtime sidecar validation now accepts only `FormatVersion == 5`; the unused minimum-supported-version range and pre-v3 event/custom-mutation decoding branches are removed.
2. **Required room generations:** every accepted v5 checkpoint must contain `AgencyRoomIdentities`, including an empty array when no rooms exist. The unreleased early-v5 missing-field compatibility path is removed; present-but-inconsistent snapshots still fail safe rather than misbinding history.
3. **Dead persistence codecs removed:** unused whole-document string serialization/deserialization and the superseded pre-transaction journal-entry codec are removed. Current persistence continues to use streaming v5 sidecars and journal-format-2 `BEGIN`/row/`COMMIT` transactions.
4. **Historical event alias removed:** the old `show_episode` timeline alias is no longer recognized; current built-in show episode history uses `show_episode_released`. The unrelated current money-detail token `show_episode` is retained.
5. **Internal compatibility wrappers removed:** obsolete checkpoint overloads and unused save-scope wrapper names are removed; current call sites use the canonical APIs directly.
6. **Historical docs removed:** v2-v4 implementation, migration, validation, schema, and example files are no longer packaged with IMDC. Historical-format knowledge belongs in a future external migrator rather than the runtime mod.

## Post-`cc924b6` audit follow-up - Pass 6

1. **UIF single-removal veto:** IMDC captures `singles._single.RemoveGirl` pre-state at `Priority.First` and explicitly orders before UIF. UIF declares the reciprocal `HarmonyAfter`; missing state is non-evidence rather than an empty cast.
2. **UIF medical vetoes:** injury, depression, and hiatus-start use reference-type pre-state and require the requested final status transition. Announced-graduation vetoes cannot emit false medical history.
3. **Assistant Manager loan replacement:** IMDC snapshots `loans.AddLoan` at `Priority.First` before Assistant Manager and emits `loan_added` only when the loan collection actually gains the loan, preserving true before/delta fields and suppressing rejected developing-loan calls.
4. **SWOF deletion boundary:** Save Write Ordering Fix 1.3.0 exposes a directory-exclusive lease. IMDC holds it across vanilla save-directory deletion and supplemental archival so an earlier queued writer cannot recreate the deleted save after sidecar detachment. Failed coordination blocks the deletion.
5. **Scope policy:** no backward-compatibility work was required or added for Pass 6; hypothetical unknown-transpiler and stack-provenance limitations remain out of the fix queue.


## Post-`cc924b6` audit follow-up - Pass 5

1. **Durable agency-room generations:** timeline history no longer promotes runtime-only `agency._room.id` into a persistent key. Each room receives an IMDC-owned `g:<guid>` generation.
2. **Exact-checkpoint reassociation:** v5 checkpoints freeze room generations in vanilla's serialized floor/room order; the load hook reassociates those generations as vanilla reconstructs each room. In 3.4.8 the field is required for every accepted v5 checkpoint.
3. **Theater/cafe collision prevention:** theater and cafe history use the owning room generation as `EntityId`, so destroyed highest IDs may be recycled by vanilla without merging two historical facilities. Raw vanilla IDs remain payload fields.
4. **Room-work continuity:** room-work compound history uses the durable room generation component and therefore survives save/load identity loss in vanilla.
5. **Public catalog contract:** `idol_status_changed`, `research_points_accrued`, and `idol_earnings_recorded` are explicitly catalogued as internal transient/non-queryable streams; the remaining 143 built-in types are the queryable catalog.
6. **Current schema:** sidecar format remains 5 and journal format remains 2. Version 3.4.8 requires the room-generation checkpoint member rather than retaining the unreleased early-v5 optional-field compatibility path.

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

- Project/mod version: **3.4.20**.
- Sidecar `FormatName`: `IMDataCore.LightweightSidecar`.
- Sidecar `FormatVersion`: **5 only**.
- Journal `FormatName`: `IMDataCore.LightweightJournal`.
- Journal `FormatVersion`: **2**.
- No runtime sidecar migration path is provided for older sidecar formats in this development build. Unsupported journal bodies are replayed only when their declared version is supported and their base hash matches. Unsupported wrong-generation suffixes are classified stale by #58; unsupported primary sidecars and unsupported matching-base journals write-protect the save scope so older backup recovery cannot become a downgrade writer.

## Vanilla deletion targets verified

The preservation hooks correspond to the three game-save directory deletion methods in the supplied decompilation:

- `Popup_Save.Delete()` for legacy/freeplay manual saves.
- `Popup_Load_Story.Delete_Save(save_info)` for a story save directory.
- `Popup_Load_Story.Delete_Playthrough(playthrough_info)` for a complete story playthrough directory.

Autosave UI does not expose the story save delete button, and unrelated `Directory.Delete` uses such as portrait/temp cleanup are intentionally not patched.
