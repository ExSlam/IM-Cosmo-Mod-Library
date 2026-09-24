# Changelog

## Rivals Reborn wide-numeric compatibility and documentation consolidation

- Added optional reflection-only Rivals Reborn wide-numeric interop with no `rivalsreborn.dll` reference and no RR persistence ownership.
- Added exact truncate-toward-zero `Single` product arithmetic and exact weighted-product comparison for RR formulas that operate on widened Int64 fan/sales/salary values.
- Repaired RR fan growth, accusation growth, rival sales, idol/demographic fan allocation, former-idol founding, poach target/buyout/salary paths, yearly shakeout ordering, award score input, wide fan formatting, founder eligibility, player-to-rival Fame derivation, and dating cover-up fan consumption.
- Preserved RR RNG/settings and final remainder semantics; bounded or intrinsically floating-point RR paths remain RR-owned.
- Added frozen RR arithmetic regression oracles at the `2^24` boundary, `Int32.MaxValue`, above `2^32`, `2^53`, overflow, and exact shakeout comparisons. Live compiled/Unity RR integration qualification remains a separate release gate.
- Consolidated durable SNLF purpose, contracts, compatibility notes, API rules, patch inventory, and validation status into `README.md`; removed historical sprint/task reports, A33 planning notes, one-off shutdown qualification markdown, and staged RR notes from the source bundle.
- Corrected the unreleased RR bridge to compile against Idol Manager's Unity API by using `Mathf.Log(value, 10f)` instead of unavailable `Mathf.Log10`, and kept RR yearly-shakeout SNS posting reflection-only through `RivalsReborn.News.PostSNS` rather than introducing a direct RR symbol reference.

## 5.5.2 - Monthly transition callback repair

- Added A34 to restore the vanilla `mainScript.onNewMonth` callback that `TimeProgress` declares/subscribes but never raises.
- Injects exactly once after vanilla `onNewDay` in the audited generated `mainScript.<TimeProgress>d__96.MoveNext` body and raises the monthly delegate only when the new in-game date is day 1.
- Restores vanilla `Awards.OnNewMonth()` Best Employer condition tracking and `data_girls.OnNewMonth()` earnings-history rollover without duplicating either subscriber's logic.
- Does not fire on save load or arbitrary `staticVars.SetTime` calls. Existing saves cannot reconstruct Best Employer failures from already elapsed months before A34 was active; subsequent month boundaries are tracked normally.
- Bumps the public version from `5.5.1` to `5.5.2` under the decimal-carry release numbering scheme.

## 5.5.1 - Audition portrait hardening and version correction

- Corrects the previous `0.55.1` label to `5.5.1`. Minor and patch components are decimal digits (0-9), carrying to the next component when incremented past 9; the major component can grow beyond 9.

- Adds the same vanilla-visual-preserving audition portrait queue/retry/replacement safety used by the patched Targeted Auditions source.
- Dynamically checks Harmony ownership of `Audition_Closed_Card.Set`.
- If `com.tel.customauditions` owns the Targeted Auditions portrait-hardening patch, this SNLF repair stands down.
- If Targeted Auditions is absent, SNLF limits full-portrait loading to five concurrent requests, retries a failed candidate's own portrait once, and replaces a persistently unrenderable candidate with a fully generated vanilla idol.
- Never substitutes another candidate's sprite and never switches to the cropped middle portrait.
- Excludes `Auditions.type.custom` so scripted/story auditions are untouched.

## 0.55.0 - Save completion barrier and participating mod storage

- Wait for outstanding SNLF writes and registered sidecar attempts before in-game Save and Exit or Return to Main Menu. Keep Unity's update loop running until every attempt is terminal, including failed/cancelled attempts.
- Add task-based sidecar writer registration and explicit completion tickets for existing mod hooks. Subscriber exceptions are isolated; a duplicate ticket completion cannot release another attempt.
- Add versioned, namespaced mod payload storage outside the repair root and vanilla DTO. Owning mods accept/reject/migrate their schemas; missing mods, incompatible records and future containers retain their opaque data.
- Add opt-in EroEvents integer payload codecs with exact Int32-to-Int64 widening and checked, all-or-nothing Int64-to-Int32 reads.
- Add native Unity tests for delayed physical writes, main-thread sidecar completion, failures, cancellation, compatible updates, absent owners, future formats, physical restart and numeric boundaries. No installed mod deployment or user save migration.

## 0.54.0 - A33.2-A33.6 wide numeric continuity completion

- Runtime hotfix 3: completed the money-display integrity audit. The weekly theater hover now includes current streaming income normalized by the game's four-weeks-per-month convention; full-width money formatting is exact across `Int64.MinValue`; salary/earnings UI no longer uses float or abbreviated money; new-single production cost no longer round-trips through `Single`; and staff severance now widens before the 48-week product across affordability, debit, context-menu, tooltip, and firing-dialogue paths.
- Runtime hotfix 2: corrected Theater > Pricing streaming revenue display for wide subscriber totals. The UI postfix now rewrites `Pricing_Sub_Revenue` from the exact checked `Int64` subscription-revenue path, preventing the `Int32.MinValue` (-¥2,147,483,648) clamp seen with 8,445,017 subscribers at ¥1,000/month.
- Runtime hotfix: removed the A33 dependency on `System.Numerics.BigInteger`. Idol Manager's Unity/Mono profile can fail to load `System.Numerics.dll`, which caused patched salary/UI evaluation to throw after an otherwise successful vanilla/older-save load and could make most idols disappear from the rendered list. A private exact signed arbitrary-precision integer now provides the same non-observable rational intermediates without an optional framework assembly. Vanilla/pre-envelope SavedData remains transparent and no idol recovery/migration logic was added.
- Completed the A33 producer graph with checked `Int64` arithmetic and transactional preflights across resource/rent/accounting, business, salary, loan, fan distribution, singles, shows, tours, theaters, cafés, concerts, SSK, research, Stats, story, VN actions, and mod-expanded inputs. Theater revenue now widens before multiplication for every `Int32` ticket price, including ¥24,750,000,000 at `250 × ¥99,000,000`.
- Added authoritative wide shadows for vanilla `Int32` storage that can hold scalable fan, subscriber, release, tour, history, story, loan, and café values. `wide_numeric_state_version = 2` serializes every exact `Int64` as a deliberate canonical decimal string and validates complete type, identity, count, ordinal/date, and compatibility-mirror witnesses before restore.
- Preserved version-1 A33 checkpoints while adding the exact chapter-four scandal comparison baseline in version 2. The absent v1-only value receives a deterministic compatibility seed with an explicit non-exact diagnostic; malformed or partial current sections remain invalid.
- Closed exact consumers across affordability/eligibility, gameplay comparisons, resource notifications, scripts, tooltips, popups, lines, animations, and summary calculations. Exact scandal totals now reach dating, resource displays, scandal rows/tooltips, and chapter-four qualification; tour fame penalties and show-release fame no longer wrap a scalable fame resource. Narrow unchanged vanilla APIs clamp only at their explicit compatibility boundary.
- Made multi-row theater/café rollover and load restoration transactional, retained exact sidecar identity during the 90-row reindex, and keyed draft-tour state by object identity so assigning its persistent ID cannot orphan its wide values.
- Added checked fan allocation/conservation and safe-range behavior across demographic, oshihen, single/show/café/activity paths, plus guarded persistent lifetime counters and ID allocators. Exhaustion or signed-`Int64` overflow fails closed, names the seam and operands, and vetoes checkpoint writes instead of committing wrapped state.
- Expanded A33 health to the complete frozen continuation target set, kept Harmony recomposition idempotent, and integrated both A33 manifests and exact-state capture into the checkpoint gate.
- Expanded source/implementation contracts and the runtime envelope harness for section-v2 completeness, v1 compatibility, canonical decimal boundaries, scandal/story state, draft-tour identity, every frozen game target, and desktop-Harmony applicability. The project builds with zero warnings; the checked arithmetic harness passes 581 assertions and the isolated envelope/Harmony harness passes 1,329 checks. Live Unity overwrite-save, Save As, autosave, F9, restart, repeated-load, and mod-combination qualification remains the release gate.

## 0.53.0 - A33.1 checked-wide numeric foundation

- Added the pure checked `WideNumericMath` substrate for signed `Int64` add, subtract, negate, multiply, multiply-add, aggregation, exact rational/decimal midpoint-to-even rounding, exact `Int32` conversion, and explicitly compatibility-only clamping.
- Added an idempotent 17-target A33.1 Harmony health manifest. It covers checked resource/fan mutations, three fan aggregates, all nine mandatory late-widen sites, show long totals/profit, and loan availability/debt aggregates without changing Assembly-CSharp field or method signatures.
- Fixed theater ticket revenue at the first operator. `GetNumberOfVisitors()` and `Ticket_Price` are converted to `Int64` before multiplication, preserving ¥10,000,000 for the stock upper case, ¥24,750,000,000 for `250 × ¥99,000,000`, and ¥536,870,911,750 for `250 × Int32.MaxValue`; the exact result continues into vanilla's existing `Int64` stat/save fields.
- Preflighted `resources._Add` and `resources._fan.AddPeople` before any live mutation while retaining vanilla ownership of clamps, fan distribution, statistics, and observer callbacks. Checked replacements also cover fan bucket/idol totals, show revenue aggregation and episode cost, loan committed/debt totals, business history/liability/earned-money products, group buzz, concert production cost, and the severance comparison.
- Replaced concert cost's `Int32` sum/product and wide-to-`Single` path with checked `Int64` operands and exact application of the compiled binary-rational discount coefficients. Exhaustive venue/difficulty/discount checks over vanilla's complete 0-10-song legal domain are bit-identical; mod-expanded values no longer inherit `Single` precision loss. Business decimal liability coefficients retain midpoint-to-even behavior without an unchecked intermediate.
- Added named failure diagnostics and checkpoint integration. A signed-`Int64` overflow refuses the patched arithmetic result, latches the operands/context, and blocks autosave/manual save; target-manifest or runtime witness failure likewise prevents A33.1 from claiming healthy protection. In-game load remains available as a recovery action, while the process-latched diagnostic continues blocking writes until restart.
- Kept narrow ABI compatibility explicit: `Staff_Fire.GetSeverance()` still returns `Int32` and fails closed rather than wrapping if the exact value is outside that ABI. Authoritative wide shadows for genuinely narrow persistent endpoints remain scheduled for later A33 segments.
- Added no A33 envelope field in this first slice. Theater revenue and the widened persisted outputs covered here already have vanilla `Int64` endpoints; the still-narrow severance ABI fails closed outside range rather than claiming a stored wide value. The byte-preserving raw `FixSaveFile` migration therefore remains unchanged, while deliberate decimal-string shadows and their codec work stay assigned to A33.4.
- Added executable arithmetic boundary/oracle coverage and source/implementation contracts, and synchronized the code, project, metadata, README, and changelog version to 0.53.0.

- Replaced vanilla `SaveManager.FixSaveFile`'s destructive whole-document SimpleJSON write while preserving its one intended `data_girls__Girls[*].parameters[*].val -> _val` migration. The exact `JSONNode.ToString()` evaluation is suppressed as well, so SNLF edits only the matching raw UTF-8 property names, preserves the original BOM choice and every other JSON token, and leaves already-current files byte-for-byte untouched.
- Added the `FixSaveFile` replacement as a fifth fail-closed embedded-transport health surface. SNLF now claims transport authority only after the exact one-site startup migration, all 5 SavedData writers, all 7 readers / 8 read sites, and both GlobalData callers report complete shapes.
- Added narrowly gated recovery for saves already rewritten by vanilla SimpleJSON. Recovery requires the recognized SNLF header plus a uniformly stringified entire envelope; every known numeric/Boolean value must have one canonical schema-directed preimage. Mixed raw/string scalar shapes, ambiguous `"null"`, malformed numbers, and non-unique Float/Int64 images remain invalid.
- Recovery now proves complete V1 schema coverage instead of silently dropping uniformly quoted unknown fields, and models SimpleJSON's culture-dependent dot-as-thousands failure (for example, invariant `0.1` becoming `"1"`). Integral-looking `Single` values are admitted only when their preimage is unique or another fractional `Single` in the same envelope proves the rewrite used a non-collapsing culture class.
- Future-proofed N02's sole V1 `Int64` (`selected_business_proposal.liability`) with an additive canonical `liability_decimal` string witness. New checkpoints require the numeric value and decimal witness to agree; a rewritten large numeric token is accepted only when it is the exact SimpleJSON forward image of that witness.
- Pre-witness rewritten `Int64` values are recovered only within a conservative one-to-one Int32/Double/G15 domain and after an adjacent-preimage collision check. Values whose original integer cannot be proved unique remain rejected so SNLF never guesses exact state.
- Kept SimpleJSON itself unpatched. Its global parser/serializer remains available to vanilla and other mods without SNLF changing their behavior.
- Fixed the v0.52 header-only repair-envelope failure. SNLF no longer asks Unity's type-layout serializer to emit its nested repair DTO graph; a bounded public-field JSON writer now emits the complete finite envelope and rejects cycles/non-finite numbers.
- Fixed the follow-on legacy-save overwrite veto observed when saving May 14 over a May 12 checkpoint. The exact game's `JsonUtility` changed a populated `records.relationship_dynamics` list during read-back, so SNLF now reads its own repair root with a strict, bounded, type-preserving parser. Every outgoing root must pass raw `records`/`relationship_dynamics` presence checks and a full value comparison through that same reader before its physical write can enter the queue.
- The strict repair-root reader rejects duplicate object keys, malformed/trailing JSON, type coercion, non-finite or out-of-range numbers, and excessive depth/node counts. It preserves field initializers only for genuinely absent later V1 sections, retaining forward section compatibility without accepting the old header-only root as exact.
- Fixed transport self-certification under HarmonyX's first-seen opaque `0 vanilla / 0 SNLF` intermediate passes. Opaque observations are now neutral/pending: they never establish authority, never erase prior proof, and no longer permanently poison the later complete composition. Missing exact observations still keep authority disabled, while mixed, partial, duplicate, and changed-signature shapes still fail closed.
- Replaced the misleading per-caller opaque-recomposition warning flood with one positive transport self-check message emitted only when all audited SavedData and GlobalData caller/site counts are exact and SNLF has become authoritative.
- A raw envelope whose `records` object is absent is now explicitly invalid. Deserializer field defaults can no longer turn a missing payload into an authoritative empty N01 set and produce the misleading one-to-one relationship-count error.
- Added SNLF-owned exact SNS persistence. The recursive vanilla `_message.Replies` tree is captured as an ordered flat node table and restored transactionally after `SNS_Manager.LoadFunction()`. Older saves/envelopes retain vanilla behavior; cycles, shared nodes, malformed parents, and non-contiguous sibling order fail closed.
- Kept the vanilla SNS field in the vanilla payload for no-mod compatibility. Unity can still report its schema-level recursion-depth warning while processing `SaveManager.SavedData`, but SNLF no longer relies on that depth-limited representation for exact SNS restoration.
- Added explicit SNS Fix success messages after the complete non-recursive payload passes serialization/round-trip injection and after a loaded checkpoint is deserialized, validated, and restored. The save-side message explains that Unity's recursive depth warning does not affect SNLF's exact SNS payload.
- Made all four embedded SNLF transport transpilers re-entry-safe. A complete already-installed SNLF call shape is now the same healthy logical interception during HarmonyX recomposition; mixed, partial, or duplicate shapes remain unhealthy.
- Fixed legacy-save `Save As` requests being refused by A14 after HarmonyX recomposed the delayed tutorial iterator. A14 now treats repeat discovery of the same audited target as idempotent and recognizes its own exact two-site delay-resolver IL without wrapping it twice.
- Preserved the fail-closed contract: only the complete `{0, 1}` resolver ordinal set is accepted; mixed, partial, duplicate, malformed, or genuinely changed `WaitForSeconds(5f)` shapes still veto repair-dependent saves.
- Expanded A14 capture errors with resolved-target and wait-site health counts so future runtime patch incompatibilities identify the failing seam directly.
- Fixed A29 legacy baseline synthesis running before vanilla's delayed `Groups._Load` membership reconstruction. Pre-A29 loads now defer only that synthesis to the exact group-ready seam, bind it to `LoadEpoch`, and refuse an intervening checkpoint instead of freezing a false null baseline.
- Made A29's five-target Harmony health manifest idempotent under logical target rediscovery while retaining exact unique-target cardinality.

## 0.52.0 - Sprint 1D Task 50

- Implemented A23 / regression 23 deterministic legacy rival bootstrap, completing the planned source repair-family backlog.
- Scoped deterministic replacement strictly to private `Rivals.Generate()` while nested under `Rivals.LoadFunction()` for a target save whose serialized rival-group list is empty. New-career `Start()` and later monthly `GenerateGroup()` gameplay remain vanilla.
- Reproduced the full audited bootstrap with repair-owned deterministic entropy: trend direction/points for genre/choreography/lyrics, fifty group names/fan bands/rising flags/genres, fixed top-three fan/genre overrides, story rival/Phantasm flags, descending sorts, and all three `LinearFunction` initializations.
- Stabilized vanilla's destructive `rival_group` name-pool consumption by snapshotting the canonical pool before the first `Generate()` mutation and restoring it for each legacy migration; repeated same-process loads therefore do not drift merely because a prior discarded bootstrap removed names.
- Added no repair-envelope payload; the next ordinary vanilla save serializes the migrated groups/trends normally.
- `SaveNLoadFixesDiagnostics.RepairsImplemented` now reports true because A23 closes the final planned source repair family. Runtime/live Harmony and release-matrix qualification remain separate and are not claimed.
- Full cumulative source/static suite: 105/105 scripts pass against the supplied decompiled game source. No project build was attempted.

## 0.51.0 - Sprint 1D Task 49

- Implemented A22 deterministic legacy relationship bootstrap.
- Scoped deterministic replacement only to `Relationships.InitialCreationForOldSave()` and `_relationship.Initialize()` calls reached inside that compatibility path; ordinary relationship creation remains vanilla.
- Preserved vanilla `Vals` seeding, `CanDate() -> positive`, `Recalc(false)`, and the exact positive/neutral/negative enum domain while replacing `UnityEngine.Random.Range(1, 4)` with the repair-owned deterministic stream keyed by stable unordered idol-pair identity.
- Added no A22 envelope section; reconstructed rows serialize normally and existing N01 remains sole current-format persistence owner for `_relationship.Dynamic`.
- Added no checkpoint blocker, LoadEpoch carrier, global coroutine behavior, SWOF dependency, IMDataCore current-state field, or Unity/System RNG migration use.
- Source/static validation only; no project build or live Unity/Harmony runtime claim is made.

## 0.50.0 - Sprint 1D Task 48

- Implemented A21 deterministic legacy aggregate-fan redistribution.
- Replaced only private `data_girls.RedistributeFansOnOldSaveLoad()`; ordinary gameplay fan APIs remain untouched.
- Preserved vanilla's fame-weighted/equal idol allocation and appeal-weighted demographic allocation while replacing both shuffled rounding orders with the repair-owned deterministic stream introduced by A20.
- Preserved the old saved aggregate total exactly, including deterministic final rounding residue; current-format saves with existing active-idol fan state remain a no-op.
- Deliberately does not reapply best-debut award/nomination positive-fan multipliers because the legacy aggregate already represents committed fans.
- Added no repair-envelope state, Unity RNG use, checkpoint blocker, LoadEpoch carrier, or IMDataCore current-state ownership.
- Source/static validation only; no project build or live Unity/Harmony runtime claim is made.

# Changelog

## 0.49.0 - Sprint 1D Task 47

- Added A20 deterministic legacy idol-profile migration for missing `GirlData.birthday` and legacy `peakAge <= 1` fallback state.
- Migration runs before `data_girls.LoadFunction()` reconstruction and populates only fields that vanilla itself classifies as legacy-missing; current saved values are left untouched.
- Added repair-owned `LegacyMigrationDeterminism` with fixed algorithm version 1, stable tuple hashing, and a custom SplitMix64 stream. It does not use `UnityEngine.Random`, `System.Random`, or `string.GetHashCode()`.
- Seed identity prefers the normalized physical save path attached to the exact weak `SavedData` read association, plus serialized save version/date/allocator and per-idol stable ID + ordinal + field discriminator.
- Birthday synthesis mirrors vanilla's `targetDate.AddYears(-24).AddYears(Range(0,12)).AddMonths(Range(0,12)).AddDays(Range(0,31))` domain exactly; peak age remains `Range(16,25)` / 16-24.
- The migrated values become authoritative immediately in the target DTO and are committed by vanilla on the next ordinary save; no A20 repair-envelope section or IMDataCore state is introduced.
- If physical-path association is unavailable, A20 remains deterministic from stable target-save metadata and reports the degraded identity source diagnostically.
- Added A20 source/implementation guards and advanced the cumulative development version to 0.49.0 / Task 47.
- No project build or live Unity/Harmony qualification is claimed in this environment.

## 0.48.0 - Sprint 1D Task 46

- Added A31 unresolved external portrait identity preservation for both idols and staff.
- Added repair-envelope V1 section `external_portrait_identities_version = 1` with sparse unresolved tokens keyed by entity kind + stable entity ID + sprite type and carrying the original `asset_id`.
- Current-format load validates the complete section and rewrites the exact target DTO slot to the original ID before vanilla lookup; exact provider return clears the token naturally, while same-type fallback or null resolution keeps it unresolved.
- Pre-A31 targets are observed without inventing history: an original ID can be preserved only if it still exists in the target DTO. Already-overwritten substitute IDs cannot be reverse-guessed.
- Idol/staff save Postfixes restore the unresolved original ID into vanilla DTOs before caller-thread freeze and recreate a missing DTO portrait slot when vanilla omitted a null runtime asset.
- Does not patch `data_girls_textures.GetTextureAssetByID(...)`, infer aliases from names/types, consume RNG, or create IMDataCore ownership.
- Added A31 source/implementation contract guards and advanced cumulative source/static coverage to Task 46.

## 0.47.0 - Sprint 1D Task 45

- Added A28 / regression 40 `Awards.TempNominations` award-eve continuation persistence.
- Repair-envelope V1 now stores each pending row as award type, year, nullable nominee idol ID, and nullable nominee single ID, preserving live order and source-supported type uniqueness.
- Added one narrow `Awards.LoadFunction()` Prefix/Postfix pair: clear stale same-process `TempNominations` before vanilla reset/load, then late-rebind exact current-format rows after vanilla idol/single reconstruction.
- Current-format empty sections are authoritative; malformed or unsupported present sections fail closed instead of becoming legacy/empty state.
- Added conservative pre-A28 compatibility only for the exact award-eve date when saved speech **types** witness the pending slate. Speech giver, thanks-target idol, and thanks-target staff IDs are never interpreted as nominee identity.
- Legacy individual nominees are reconstructed deterministically from target-loaded state. Legacy `best_single` reproduces vanilla eligibility and chart-position ordering but uses stable single ID for equal-position ties rather than consuming `mainScript.chance(50)`.
- Preserved the already-implemented A30 current-format best-single binding: A28 persists/restores its exact bound single and never calls `Awards.GetNominatedSingle()` during restore.
- Left canonical `Awards.Reset_After_Awards()` ownership untouched; once vanilla consumes the occurrence, the next checkpoint naturally records authoritative empty A28 state.
- Added A28 source/implementation contract guards and advanced cumulative source/static coverage to Task 45.
- No project build or live Unity/Harmony qualification is claimed in this environment.

## 0.46.0 - Sprint 1D Task 44

- Implemented A14 / regression 34 delayed tutorial continuation continuity.
- Scoped semantic registration to `Tutorial_Actions._Coroutine(string)` and the exact `Manager_After_Conversation` / `Activities_After_Hire` generated carriers already guarded by A08.
- Extended repair-envelope V1 with `delayed_tutorial_continuations_version = 1` and rows containing only fixed continuation kind plus optional remaining scaled presentation delay.
- Save capture consults the exact caller-thread target `variables__variables`; tokens whose target tutorial is already `available`/`done` are not persisted.
- Load restores only after `variables.LoadFunction()` adopts target status, discards idempotently completed targets, and otherwise re-enters vanilla `Tutorial_Actions._Coroutine(kind)` exactly once.
- The activities path preserves the next dialogue-check delay frontier across its two source-proven `WaitForSeconds(5f)` sites using `Time.time`; manager continuation has no timer and cannot carry one.
- A08 remains the sole bool `MoveNext` stale-execution guard; partial A14 restore rollback revokes admitted carriers through A08.
- Full cumulative source/static suite: 93/93 scripts pass. No project build or live Unity/Harmony runtime claim is made.

## 0.45.0 - Sprint 1D Task 43

- Implemented A13 / regression 33 temporary `agency.GirlsBannedFromAutoTasks` continuity.
- Persist stable idol ID plus exact remaining `WaitForSeconds`-domain delay in a section-marked repair-envelope V1 list.
- Measure remaining duration only with Unity scaled `Time.time`; process-offline wall time, realtime, and unscaled time never consume the saved remainder.
- Preserve vanilla duplicate semantics: `BanGirlFromAutoJobs()` still returns before scheduling when membership already exists, so duplicate calls never extend expiry.
- Reuse the exact private `ReturnGirl` iterator and its single `WaitForSeconds(60f)` site; A13 changes only the delay operand for restored carriers while A08 remains the sole bool `MoveNext` stale/rollback suppression owner.
- Clear stale F9 membership before target idol reconstruction, validate the complete A13 section against the exact target roster, rebind fresh idols, and restore through vanilla `BanGirlFromAutoJobs(...)`.
- Current-format empty is authoritative, pre-A13 absence remains unknown, malformed sections fail closed, unresolved runtime identities prevent partial restore, and partial scheduling rollback revokes admitted carriers through A08.
- Full cumulative source/static suite: 91/91 scripts pass. No project build or live Unity/game runtime claim is made.

## 0.44.0 - Sprint 1D Task 42

- Implemented A11 / regression 31 graduation-successor introduction continuity.
- Captures the ordered stable idol IDs only after vanilla `Date_Graduation.StartIntroductions()` successfully schedules the already-hired successor presentation; no successor generation, hire, or RNG is replayed.
- Extended repair-envelope V1 with `successor_introductions_version = 1` plus ordered `successor_introduction_girl_ids`, validating every pending ID against the exact caller-thread target `SavedData` roster.
- Clears stale same-process `data_girls.new_girls` references before `data_girls.LoadFunction()` reconstructs target idols, then rebinds only fresh loaded objects by stable ID.
- Reuses vanilla `Date_Graduation.StartIntroductions()` for the small five-second presentation delay and the existing A08 `<_StartIntroductions>d__10` `LoadEpoch` guard as the sole stale-carrier execution owner.
- Current-format empty sections are authoritative; pre-A11 absence remains unknown; malformed current sections fail closed; unresolved loaded participants are dropped/logged without creating replacement idols.
- Full cumulative source/static suite: 89/89 scripts pass. No project build or live Unity/game runtime claim is made.

## 0.43.0 - Sprint 1D Task 41

- Closed N12-D / finding #13 source-equivalent regression coverage for restart, F9, rollback, repeated untouched-save load, already-due jobs, zero-idol jobs, authoritative empty state, malformed-current/legacy state, missing runtime witnesses, stale agency-loader completion, and exact-once original-vs-replacement ownership.
- Hardened N12-C transactional rollback by explicitly cancelling each replacement in A08 before best-effort `StopCoroutine`; if Unity stop cleanup fails, A08 still suppresses that exact iterator before its next `MoveNext` can reach `room.assign(...)`.
- Added dedicated N12-D implementation/source regression oracles while preserving A08 as the sole bool stale-carrier execution guard.
- Advanced cumulative development version to 0.43.0.


## 0.42.0 - Sprint 1D Task 40

- Added N12-C exact load-time restore/reschedule for section-marked pending ambient-scene jobs.
- Tags each exact private `agency.LoadData()` iterator with its creation `LoadEpoch`, then observes terminal `agency.<LoadData>d__80.MoveNext` only after vanilla has waited for idols/groups, rebuilt rooms, reset floor IDs, and rendered the agency; stale agency loaders are ignored only as N12 restore triggers and are never suppressed.
- Validates the complete N12 section against the exact target `SavedData` before resolving any live object; present-invalid state fails closed, legacy absence remains unknown, and current-format empty remains authoritative empty.
- Rebinds fresh rooms by `FloorID + room ordinal + room type` and fresh idols by unique canonical saved/live IDs.
- Recreates only vanilla `Scenes.AssignSceneWithDelay(...)`, verifies N12-A captured the exact replacement semantics, then admits all replacement coroutines without calling `GenerateScene`, RNG, or `room.assign(...)` directly.
- Adds same-target duplicate suppression, load-epoch transaction checks, and rollback of partially admitted replacement coroutines/N12 registry state. A08 remains the sole stale `MoveNext` mutation guard.
- Source/static validation only; no build or live Unity/game runtime claim is made.

## 0.41.0 - Sprint 1D Task 39

- Added N12-B repair-envelope persistence for N12-A's detached current-epoch pending ambient-scene jobs.
- Added section marker `pending_ambient_scenes_version = 1` plus sparse records for canonical due game time, scene type, structural `FloorID + room ordinal + room type`, and ordered idol IDs.
- Capture validates every referenced room and idol against the exact caller-thread `SaveManager.SavedData` request before transport serializes it; ambiguous/missing witnesses fail checkpoint freeze.
- Preserves authoritative empty pending-job sections and allows vanilla's legitimate zero-idol scene types.
- Uses vanilla `ExtensionMethods.ToDataString` / `ToDateTime` as an exact second-precision round-trip contract for the scheduled game time.
- Adds no load restore/reschedule, `room.assign(...)`, `StartCoroutine(...)`, RNG replay, checkpoint blocker, alternate persistence channel, or competing LoadEpoch mechanism.
- Source/static validation only; no build or live Unity/game runtime claim is made.

## 0.40.0 - Sprint 1D Task 38

- Added N12-A pending delayed ambient-scene semantic capture at the exact `Scenes.AssignSceneWithDelay(...)` factory.
- Captures detached due game time, scene type, canonical unique idol IDs, and structural `FloorID + room ordinal + room type`; never runtime room IDs or live room/idol references.
- Reuses A08's already-shipped `<AssignSceneWithDelay>d__12.MoveNext` `LoadEpoch` guard as the sole stale-timeline mutation suppressor instead of adding a competing bool Prefix.
- Adds exact lifecycle accounting for normal completion, stale retirement, faults, and abandoned iterators, plus a detached current-epoch snapshot API for N12-B.
- Adds no repair-envelope schema change, restart restoration/rescheduling, RNG replay, checkpoint blocker, or IMDataCore current-state ownership.
- Source/static validation only; no build or live Unity/game runtime claim is made.

## 0.39.0 - Sprint 1D Task 37

- Added N04 active room `substoryScene` dialogue-identity persistence and late structural rebinding.
- Repair-envelope V1 records only saved FloorID + room ordinal + room type + canonical scene dialogue ID.
- Capture requires exact serialized/live floor and room shape, exact `substoryScene` status, canonical dialogue identity, and dialogue type `scene`.
- Restore runs in `agency.GetRoomDataForLoading(RoomData)` after vanilla reconstructs each room and before agency rendering, assigning only the missing pointer and never replaying `room.assign(...)`.
- Current-format N04 sections cover every saved `substoryScene` room exactly once; malformed/missing-definition cases fail closed. Pre-N04 saves do not fabricate an unknowable scene ID.
- No scene replay, RNG, checkpoint blocker, LoadEpoch carrier, or IMDataCore current-state ownership is added.
- No live Unity/game runtime claim is made.

## 0.38.0 - Sprint 1D Task 36

- Added N03 queued substory `BeforeStart` semantic descriptor persistence and late callback rebinding.
- Frozen the supplied vanilla callback surface at exactly 13 direct non-null scheduling sites: one `Event_Overlord` site and twelve `Dating` sites.
- Collapsed those sites into six bounded semantic setup kinds carrying stable idol IDs plus only source-specific mask/question arguments. No `Action`, closure, or delegate object is serialized.
- Correlated every descriptor to the exact persisted queue occurrence by queue ordinal plus dialogue ID, launch time, delay, and debug witnesses.
- Rebuilt callbacks resolve loaded idols by stable ID and preserve source-deferred presentation setup, including the generic-date secondary-idol choice at presentation time.
- Unknown live callbacks fail repair-envelope freeze visibly; pre-N03 saves do not fabricate missing semantics.
- Full cumulative source/static suite: 77/77 scripts pass. No live Unity/game runtime claim is made.

## 0.37.0 - Sprint 1D Task 35

- Added N02 exact random-event selected-business-proposal continuity.
- Repair-envelope V1 now carries one section-marked active-event locator/witness plus selected active-proposal ordinal and complete structural witness.
- Capture requires exact serialized/live event and proposal cardinality and exact reference identity for the selected proposal.
- Load clears stale `Event_Manager.VARIABLE__BIZ_PROPOSAL` before Event_Manager reconstruction and rebinds only after both complete `SaveManager.LoadData(...)` paths finish.
- Legacy/pre-N02 saves leave the original random selection unknown and null. No reroll, qualifying-pool search, or IMDataCore restoration state is used.
- Cumulative static suite advances to 75/75 guards and 79/79 production C# compile-manifest coverage.

## 0.36.0 - Sprint 1D Task 34

### Added
- A29 exact `singles.FanAppeal_LastSingle` comparison-baseline continuity.
- Repair-envelope V1 `fan_appeal_last_single_version = 1` with explicit nullability, source released-single ID, and all seven `resources.fanType` ratios.
- One pre-load stale-clear seam, one release source-ID observer, and post-career-load restore on both `SaveManager.LoadData` overloads.
- Source and implementation guards proving `ReleaseData.FanAppeal` / `GetAppealForOpinion()` are not used as substitutes.

### Behavior
- Repaired saves restore a fresh exact seven-axis vector only after source single ID rebinding validates against the target save and loaded singles list.
- Pre-A29 saves choose a deterministic target released single, synthesize with the actual release-time `RecalcFanAppeal(source.GetSenbatsuStats(), true)` formula, restore the source single's original FanAppeal list, and commit the synthesized baseline on the next repaired save.
- Null baseline remains explicitly representable. Non-finite, duplicate, missing-axis, invalid-source, and malformed-section states fail closed.
- Adds no RNG, checkpoint blocker, LoadEpoch carrier, alternate persistence channel, or IMDataCore current-state ownership.

### Validation
- Full cumulative source/static guard suite plus both transport manifests is required before packaging.
- No live Unity/game runtime claim is made.

## 0.35.0 - Sprint 1D Task 33

### Added
- A17 pushed-slot duration-baseline normalization at `Girl_Select_Popup.OnClick(data_girls.girls)`.
- One first-priority Prefix with exact `Pushes.Girls` receiver identity proof.
- Source and implementation guards for the click-to-next-day half-state and the already-safe `Pushes.RemovePush()` path.

### Behavior
- Resets only `Pushes.Days[slot]` when the pushed idol identity actually changes.
- Preserves the counter for unchanged identity and passes through every non-Pushes use of the generic girl selector.
- Leaves `Pushes.Girls`, `GirlsLastDay`, daily influence logic, rendering, save/load, and removal behavior vanilla-owned.
- Adds no repair-envelope field, migration state, RNG behavior, blocker, LoadEpoch carrier, or IMDataCore current-state ownership.

### Validation
- Full cumulative source/static guard suite plus transport manifests is required before packaging.
- No live Unity/game runtime claim is made.

## 0.34.0 - Sprint 1D Task 32

### Added
- A16 exact `Substories_Manager.PreviousNewSubstory` spacing-anchor continuity.
- Repair-envelope V1 `previous_new_substory_version = 1` plus one `previous_new_substory_game_date`.
- One post-load A16 patch on `Substories_Manager.LoadFunction()`.
- Source and implementation guards for the complete audited legacy fallback chain and F9-safe target-DTO evidence.

### Behavior
- Repaired saves persist and restore the exact private anchor after vanilla target substory reconstruction.
- Pre-A16 saves prefer the latest relevant queued launch time, then complete authoritative saved `Date_LastTriggered` evidence.
- If no relevant ID has ever been used, migration restores `staticVars.StartDate`; if `UsedSubstories` proves occurrence but exact timing is unavailable, migration uses the target-save game date conservatively.
- Runtime `data_dialogues._dialogue.Date_LastTriggered` is never consulted as migration evidence, avoiding stale sparse-overlay state after F9.
- No dialogue replay, RNG, blocker, extra epoch carrier, or IMDataCore current-state ownership is added.

### Validation
- Full cumulative source/static guard suite plus transport caller manifest is required before packaging.
- No live Unity/game runtime claim is made.

## 0.33.0 - Sprint 1D Task 31

### Added
- A15 bounded recent-activity recency continuity for `Stats.Activities_Stats` and derived `Activities.LastHeal`.
- Repair-envelope V1 `recent_activity_timeline_version = 1` with compact `{activity_type, game_date}` rows.
- One post-load A15 patch on `Stats.LoadFunction()` after vanilla clears the dated list.
- Source and implementation guards for strict 10-day pruning, VN consumers, and spa-heal derivation.

### Behavior
- Captures all three built-in activity types whose timestamps are strictly later than target-save time minus ten days.
- Rejects future-dated rows and refuses to freeze a recent `LastHeal` that is not represented by the bounded spa timeline.
- Restores the exact bounded list and derives `LastHeal` from the newest spa row.
- Uses an empty recent timeline plus a deterministic ten-day-old spa anchor for pre-A15 saves.
- Does not call activity gameplay methods, consume RNG, or add blocker/epoch/IMDataCore ownership.

### Validation
- Full cumulative source/static guard suite plus transport caller manifest is required before packaging.
- No live Unity/game runtime claim is made.

## 0.32.0 - Sprint 1D Task 30

### Added
- A02 private project `_progressable.counter` continuity across singles, shows, concerts, tours, and SSK.
- Repair-envelope V1 `project_progress_counters_version = 1` with sparse owner-kind/stable-owner-ID/parameter-enum counter records.
- Two post-load A02 patches covering both `SaveManager.LoadData` overloads after the full vanilla `LoadEvent` reconstruction fan-out.
- Source and implementation guards for all five copied diminishing-return counter families.

### Behavior
- Captures exact private counters from the same caller-thread DTO parameter objects vanilla is about to serialize.
- Stores only positive counters; section-present missing keys mean exact zero.
- Uses zero as the deterministic fallback for pre-A02 saves and never infers the ordinal from public progress values.
- Validates all five loaded owner families and applies the complete counter set atomically with rollback-aware assignment.
- Does not call project `Add(...)`, replay production, consume RNG, alter public `val`/`progress`, or add checkpoint/epoch behavior.

### Validation
- Full cumulative source/static guard suite plus transport caller manifest is required before packaging.
- No live Unity/game runtime claim is made.

## 0.31.0 - Sprint 1D Task 29

- Implemented N11 / finding #12 tutorial performance/promotion activity-baseline continuity.
- Extended repair-envelope V1 backward-compatibly with a section marker and explicit presence flags so initialized zero remains distinct from null/uninitialized.
- Caller-thread capture reads the two private `Tutorial_Reqs` baselines and validates initialized values against the exact target SavedData aggregate activity counters.
- A Postfix on `Tutorial.LoadFunction()` restores both baselines only after vanilla `Tutorial.Reset()` / `Tutorial_Reqs.Reset()` has run.
- Pre-N11 saves explicitly remain at vanilla `-1` so first post-migration checks seed from target counters; malformed present sections fail closed.
- Added no tutorial action replay, RNG behavior, checkpoint blocker, LoadEpoch carrier, alternate persistence channel, or IMDataCore current-state copy.

## 0.30.0 - Sprint 1D Task 28

- Implemented N10 / finding #10 `agency._room.business_minutes_before_finish` continuity.
- Extended repair-envelope V1 backward-compatibly with a section marker plus sparse structural-room records carrying the exact finite nonzero hidden minutes.
- Caller-thread capture requires exact serialized/live floor order, FloorID, room ordinal/type/status, and saved/live business-type agreement; runtime room IDs are not used.
- A Postfix on `agency.GetRoomDataForLoading(RoomData)` restores only the hidden float after the full N10 section validates.
- Pre-N10 saves leave the lost hidden remainder unknown; malformed present sections fail closed.
- Added no proposal replay/selection, timer rebasing, value clamping, new persistence channel, checkpoint blocker, LoadEpoch carrier, RNG behavior, or IMDataCore current-state persistence.

## 0.29.0 - Sprint 1D Task 27

- Implemented N09 / finding #9 interrupted-training `agency._room.girl_paused` continuity.
- Extended repair-envelope V1 backward-compatibly with a section marker and sparse records keyed by serialized FloorID + room ordinal + room type, carrying only the displaced idol ID.
- Caller-thread capture requires exact serialized/live agency floor structure and canonical idol identity; runtime `_room.id` is never used.
- `agency.LoadFunction()` Prefix clears stale same-process paused-idol references, while the existing `GetRoomDataForLoading(RoomData)` seam late-rebinds the loaded idol without replaying ResumeTraining/assign.
- Pre-N09 saves do not fabricate a displaced idol; malformed present sections fail closed.
- Added no new persistence channel, checkpoint blocker, LoadEpoch carrier, RNG behavior, or IMDataCore current-state copy.

## 0.28.0 - Sprint 1D Task 26

- Implemented N08 / finding #8 `Event_Overlord.Latest_Event` continuity.
- Extended repair-envelope V1 with a section-marked exact pacing timestamp captured from the same caller-thread SavedData request.
- Added a one-target Postfix on `Event_Overlord.LoadFunction()` so the validated timestamp is restored after vanilla resets it to `staticVars.StartDate`.
- Pre-N08 saves use the exact serialized target-save game date as the deterministic conservative pacing anchor; same-process RAM is never used as fallback.
- Present malformed/unsupported N08 sections fail closed, and future/corrupt anchors are rejected on capture and restore.
- Added no random-event replay, RNG replay, checkpoint blocker, new LoadEpoch carrier, separate persistence channel, or IMDataCore current-state copy.

## 0.27.0 - Sprint 1D Task 25

- Implemented N07 / finding #7 `Activities.Chain` ordered future-intent continuity using the active V1 SNLF repair envelope.
- Extended V1 backward-compatibly with an `activity_chain_version` marker plus the exact ordered `Activity._type` list captured from the same SavedData request.
- `Activities.LoadFunction()` Postfix clears stale same-process chain state, restores a fully validated Task-25+ section atomically, and uses deterministic empty-chain fallback for pre-N07 saves whose exact future intent was never serialized.
- Existing A08 LoadEpoch protection remains the sole stale-`Chain_Progress_Do` carrier invalidation; N07 adds no dispatch-phase token and does not replay `Performance`, `Promotion`, or `SpaTreatment` itself.
- Added N07 diagnostics and source/static guards; no new persistence channel, checkpoint blocker, RNG behavior, or IMDataCore current-state persistence.

## 0.26.0 - Sprint 1D Task 24

- Implemented N05 / finding #5 show last-episode `FanAppeal` continuity using the existing V1 SNLF repair envelope.
- Extended V1 backward-compatibly with a section-level `show_fan_appeals_version` marker plus per-show ordered fan-type/ratio entries keyed by vanilla show ID.
- Caller-thread capture validates an exact unique-ID match between the populated `SavedData.shows__Shows` rows and live `Shows.shows`; inner FanAppeal order is preserved exactly.
- A Postfix on `Shows.LoadFunction()` validates the complete reconstructed show set and every ratio before atomically replacing only each show's `FanAppeal` list.
- Task-23 V1 envelopes remain valid; absence of the N05 section marker means exact last-episode appeal is unknown and vanilla state is left untouched rather than interpreted as an empty vector.
- Added N05 diagnostics and source/static guards; no new transport channel, episode replay, FanAppeal recalculation, RNG behavior, blocker, epoch carrier, or IMDataCore current-state persistence.

## 0.25.0 - Sprint 1D Task 23

- Began Wave 3 core repair-envelope state with N01 / finding #1 relationship `Dynamic` continuity.
- Activated the planned `__cosmo_save_n_load_fixes` V1 root with `format_name`, `format_version`, opaque `checkpoint_id`, mod/game-date diagnostics, and a typed sparse `relationship_dynamics` section.
- N01 captures exact `Dynamic` by normalized unordered idol pair only when every vanilla serialized relationship row maps one-to-one to live runtime state.
- SavedData transport now freezes vanilla JSON plus repair state on the caller thread and queues one combined payload; repair-dependent SavedData no longer has a writer-thread/vanilla fallback that could split checkpoint state.
- Coordinated SavedData reads now read raw bytes, extract and strip the SNLF root, deserialize only the vanilla JSON into `SaveManager.SavedData`, and weakly associate the validated envelope with the exact returned DTO.
- `Relationships.LoadFunction()` Postfix restores dynamics only after vanilla reconstruction and only when the complete current/envelope pair sets validate exactly. Legacy envelope absence is left untouched; a present invalid envelope fails closed.
- The runtime-only no-mod unknown-root compatibility probe remains unexecuted in this environment, so no removal/no-SNLF or compiled Unity-runtime compatibility claim is made.

## 0.24.0 - Sprint 1D Task 22

- Implemented A19 startup `GlobalData` exactly-once consumer delivery.
- Brackets concrete `SaveManager.LoadGlobalData()` calls and observes whether vanilla actually invokes `staticVars.LoadSettings()` during that same read attempt.
- A successful read with no consumer invocation stores one process-local pending reference to that exact `SaveManager.GlobalData` object.
- `staticVars.Awake()` Postfix runs after vanilla subscribes `LoadSettings`; it claims the pending object once, verifies it is still the manager's current object, and calls only `LoadSettings()` directly.
- A newer GlobalData read supersedes any older pending object, while normal event delivery suppresses replay for the satisfied attempt.
- Does not re-deserialize `global_data.json`, invoke `LoadGlobalDataEvent`, persist the latch, or alter ordered GlobalData file transport.
- Added three-target patch health, read-only diagnostics, and source/static guards for both startup orders and the complete `LoadSettings()` side-effect surface.

## 0.23.0 - Sprint 1D Task 21

- Implemented A01 training `Progress_Init` reconstruction at private `agency.GetRoomDataForLoading(RoomData)`.
- Reconstructs the omitted private training baseline only for serialized `girlTraining` rooms using the exact audited inverse formula from saved `Progress`, `startTime`, `CompletionTime`, and target-save `staticVars__dateTime`.
- Reads target-save time from the adopted `SavedData` DTO instead of relying on LoadEvent subscriber order.
- Fails closed for malformed timestamps, non-positive completion time, non-finite values, or target time preceding training start.
- Writes only `loaded.Progress_Init`; does not clamp progress, rebase the schedule, alter start/finish/completion fields, or persist a duplicate baseline.
- Added one-target patch health, read-only diagnostics, and source/static guards.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG behavior, old-provider compatibility, or IMDataCore current-state persistence.

## 0.22.0 - Sprint 1D Task 20

- Implemented N14 / finding #31 player forced idol-idol breakup coherence.
- Prefix-patched only private `Date_Popup.OnClick_ForceBreakup()` before vanilla's selected-idol-only `DatingData` clear.
- When the selected idol is `taken_idol`, SNLF enumerates only existing relationships, requires exactly one concrete two-idol `Dating` relationship, and delegates teardown to vanilla `Relationships._relationship.BreakUp()` exactly once.
- Outside-partner breakups remain on vanilla's original path; missing or ambiguous idol relationships fail closed and no relationship is fabricated.
- The repair never calls relationship-creating `Relationships.GetRelationship(...)` and does not duplicate `BreakUp()`'s dynamic, stamina, dual-status, or notification effects.
- Added one-target patch health, read-only diagnostics, and source/static guards.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG behavior, old-provider compatibility, or IMDataCore current-state persistence.

## 0.21.0 - Sprint 1D Task 19

- Implemented N13 / finding #30 `Event_Manager` nonstandard terminal-state repair.
- `ConcludeEvent(_reply)` is transpiled at its first frozen return, the source-proven no-results early return, so the consumed occurrence becomes `complete` inside the original method before Harmony Postfix observers sample terminal state; the ordinary final return is untouched.
- `Event_Manager.<OpenPopup>d__46.MoveNext` is transpiled at its single automatic `Event_Manager.AddSNS(...)` site so an SNS-only occurrence becomes `complete` immediately after automatic SNS delivery commits.
- No generic `SNS_Manager`/`SNS_Popup` sink is patched, and normal popup/SNS presentation remains vanilla-owned.
- Added two-target patch health, read-only diagnostics, and source/static guards for the resultless, SNS-only, load-coercion, and restart-suppression defect chain.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG behavior, old-provider compatibility, or IMDataCore current-state persistence.

## 0.20.0 - Sprint 1D Task 18

- Implemented A32 group target-audience DTO round-trip repair at save-side `Groups.GroupData.Set(Groups._group)`.
- The Postfix writes exact live `Appeal_Gender`, `Appeal_Hardcoreness`, and `Appeal_Age` values into the vanilla DTO; the existing `_group.Set(GroupData)` loader already restores all three.
- Fan/point arrays and all other group serialization remain vanilla-owned.
- Pre-fix historical audience provenance is not guessed.
- Added one-target patch health, read-only diagnostics, and source/static guards.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG behavior, or IMDataCore current-state persistence.

## 0.19.0 - Sprint 1D Task 17

- Added A30 best-single temporary nomination binding at private `Awards.AddTempNomination(...)`.
- The `best_single` row now receives one vanilla `Awards.GetNominatedSingle()` result before construction and `SetWins()` consumes that stored candidate.
- No persistence, RNG replacement, repair-envelope state, blocker, or new epoch carrier.
- Cumulative source/static regression suite retained.

## 0.18.0 - Sprint 1D Task 16

- Implemented A27 stale `Tutorial.Active_Tutorial_ID` lifecycle / business-success override repair.
- `Tutorial.Reset()` now clears the transient scalar, covering startup/reset and same-process target-load reconstruction through vanilla `Tutorial.LoadFunction()`.
- `Tutorial_Window.Hide(Action)` is Prefix-patched at the shared terminal boundary; stock source has exactly two callers (final `OnContinue()` and `Quit_Confirm()`), so the ID clears before `Popup.Hide()` can synchronously release popup ownership.
- Added a defense-in-depth Prefix on private `agency._room.DoBusiness()` that clears any leftover ID whenever `Tutorial.Is_Tutorial()` is false before vanilla evaluates its existing `10_business_deals` forced-success branch.
- Vanilla's ordinary staff business-success RNG calculation and genuine active-business-tutorial forced-success semantics remain untouched.
- Added three-target patch health, read-only diagnostics, and source/static guards freezing the start/reset/terminal/Quit/business-override defect chain.
- Added no persistence, repair-envelope state, business result replay, RNG call, checkpoint blocker, LoadEpoch carrier, old-provider compatibility, or IMDataCore current-state storage.

## 0.17.0 - Sprint 1D Task 15

- Implemented A26 `_show.GetNextEpisodeDate(DateTime)` discarded initial one-day advance.
- Added one one-method/one-site fail-closed transpiler that finds the native `DateTime.AddDays(double)` result immediately discarded by `pop` and assigns that same result back to `_start`.
- Vanilla's one-day constant, weekday loop, no-argument overload, and `GetCancelationDate()` remain unchanged; cancellation-date semantics improve transitively through the corrected helper.
- Added exact target/site patch health, read-only diagnostics, and source/static guards freezing the one discarded initial advance, the assigned loop advance, and the cancellation helper dependency.
- Added no repair-envelope state, duplicate calendar algorithm, RNG behavior, checkpoint blocker, LoadEpoch carrier, old-provider compatibility, or IMDataCore current-state persistence.

## 0.16.0 - Sprint 1D Task 14

- Implemented A25 ignored immutable-`DateTime` graduation-date adjustments across the six audited `Graduation_Date` call sites.
- Added one five-method/six-site fail-closed transpiler covering injury, depression, best-friend/clique graduation social effects, accepted business proposals, and `Graduation_Date_Update()`.
- The transpiler preserves the owning idol reference, uses the exact vanilla-computed month/day delta, routes native `DateTime` arithmetic through pure helpers, and stores the returned value back into the same field.
- Vanilla control flow remains authoritative, including the best-friend exclusion from the three-month clique adjustment and the complete dynamic `Graduation_Date_Update()` formula.
- Added exact target/site patch health, read-only diagnostics, and source/static guards for all six discarded statements plus assigned positive controls.
- Added no repair-envelope state, duplicated graduation formula, RNG behavior, checkpoint blocker, LoadEpoch carrier, old-provider compatibility, or IMDataCore current-state persistence.

## 0.15.0 - Sprint 1D Task 13

- Implemented A24 award `her_choice` single-resolution consistency at `Awards._speech.GetThanks()`.
- The first game-requested `her_choice` result for a live `_speech` runs through vanilla unchanged and becomes the occurrence-authoritative result.
- Subsequent `GetThanks()` calls on that exact speech return the cached category, preventing displayed text, placeholder/target branches, and the player-thanked achievement branch from disagreeing within one delivery.
- The cache is weakly keyed to the live speech object, non-persistent, and does not roll RNG independently; non-`her_choice` calls remain fully vanilla-owned.
- Added exact getter patch health, read-only diagnostics, and source/static guards freezing the eight-call solo/group delivery corpus.
- Added no repair-envelope state, VN-handler rewrite, checkpoint blocker, LoadEpoch carrier, old-provider compatibility, or IMDataCore historical storage.

## 0.14.0 - Sprint 1D Task 12

- Implemented A18 trivia zero-counter authoritative restoration across `girls_trivia.LoadFunction()` and `Graduation_Trivia.LoadFunction()`.
- Added one Prefix per copied loader that resets the current authored process-static counters to zero before vanilla applies target-save rows.
- Vanilla loader bodies remain authoritative for all nonzero saved rows and unknown-ID handling; explicit serialized zero now remains zero across same-process F9/rollback.
- Added dual-target patch health, read-only diagnostics, and source/static guards freezing the copied save/load zero-skip mechanism.
- Added no repair-envelope state, loader-body replacement, checkpoint blocker, LoadEpoch carrier, RNG work, old-provider compatibility, or IMDataCore persistence.

## 0.13.0 - Sprint 1D Task 11

- Implemented A07 unfinished-concert `FinishDate` preservation inside `SEvent_Concerts.LoadFunction()`.
- Added a fail-closed loader-local transpiler that expects exactly one `_concert.Initiate()` call and replaces only that call with a stack-compatible SNLF wrapper.
- The wrapper snapshots the already-reconstructed `FinishDate`, runs vanilla `Initiate()` exactly once, and reapplies the date only for unfinished concerts with an authoritative non-default saved date.
- Finished concerts and unfinished rows with no serialized date retain vanilla semantics.
- Restoration occurs before list insertion and before `SpecialEvents_Manager.RenderTab()` uses concert `FinishDate` as `LaunchDate`, preventing load-time project reordering.
- Added read-only A07 diagnostics plus source/static guards for the save/restore/Initiate/RenderTab defect chain.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, global `_concert.Initiate()` patch, old-provider compatibility, or IMDataCore persistence.

## 0.12.0 - Sprint 1D Task 10

- Implemented A06 `loans.BankruptcyDanger` reconstruction as a narrow `loans.LoadFunction()` Postfix.
- Reads the target save's serialized money `ResourceData` directly so repair correctness does not depend on `LoadEvent` subscriber ordering.
- Reconstructs only the boolean as `serializedMoney < 0` and preserves the `BankruptcyDate` already restored by vanilla.
- Never calls vanilla's private danger setter, so loading cannot extend/reset the saved bankruptcy deadline.
- Uses the last matching serialized money row if malformed input contains duplicates, matching vanilla's sequential resource application; missing money fails closed.
- Added read-only diagnostics plus source/static guards for the save/load/deadline-reset defect mechanism.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, live-money fallback, old-provider compatibility, or IMDataCore persistence.

## 0.11.0 - Sprint 1D Task 9

- Implemented A05 `data_girls.LastGirlID` load-inflation repair as a narrow `data_girls.LoadFunction()` Prefix/Postfix.
- Prefix snapshots authoritative `data_girls__LastGirlID` from target `SavedData`; Postfix restores that exact allocator after vanilla reconstructs saved idols through temporary `GenerateGirl()` IDs.
- Added read-only diagnostics for capture, restore, no-change, capture-failure, and suppressed spurious allocator advances.
- Added source/static guards that freeze the vanilla save/restore/temporary-allocation/ID-overwrite mechanism.
- Added no repair-envelope state, generated-id replay, checkpoint blocker, LoadEpoch carrier, old-provider compatibility, or IMDataCore persistence.

## 0.10.0 - Sprint 1D Task 8

- Began the low-risk no-storage repair wave after A08 safety closure.
- Implemented A03 risky-single `Marketing_Result == 0f` current-format restoration at the private `singles.GetSingleDataForLoading(SinglesData)` seam.
- Restores the already-serialized authoritative zero in a Postfix after vanilla's invalid zero-sentinel reroll branch; `Marketing_Result_Status` remains vanilla-owned.
- Conservatively gates the first implementation to the supplied v1.0.6 current format and skips older/unknown save versions for the later legacy migration wave.
- Added no repair-envelope state, checkpoint blocker, LoadEpoch carrier, RNG replay, old-provider compatibility, or IMDataCore persistence.

## 0.9.0 - Sprint 1D Task 7

- Implemented A08 finite same-process/F9 stale-carrier invalidation for the seven audited authoritative iterator families not already guarded by Tasks 3-6.
- Added weak generation tracking for `_CheckDialogueQueue`, `Activities.Chain_Progress_Do`, `Scenes.AssignSceneWithDelay`, `Date_Graduation._StartIntroductions`, `agency.ReturnGirl`, and the two delayed `Tutorial_Actions` iterators.
- Added exact generated-`MoveNext()` stale suppression with normal-completion/fault cleanup and manifest health requiring all seven factory plus seven carrier seams.
- Reset only the runtime `Substories_Manager.checkingQueue` mutex during target reconstruction so suppressing an old queue iterator cannot strand the loaded dialogue queue.
- Added no new CheckpointGate blocker, repair-envelope state, continuation persistence, broad coroutine cancellation, or old-provider compatibility.

## 0.8.0 - Sprint 1D Task 6

- Implemented A12 / Area #2 A2-A12 SSK post-payment checkpoint atomicity.
- Added one `SskPostPaymentLaunch` lease from `StartSSK_Coroutine()` iterator creation through committed cooldown/status/results and actual result-popup acquisition.
- Added selective `LoadEpoch` suppression only to `SEvent_SSK.<StartSSK_Coroutine>d__28.MoveNext`.
- Added exact queued-popup handling: a `PopupManager.Open(sevent_SSK, true)` request that only queues does not release the blocker; `PopupManager._popup.Open()` releases only after the actual SSK result popup acquires `PopupCounter` protection.
- Kept the blocker through `SSKPopup.Reset()` and `SSKPopup.StartSSK(...)` on the synchronous-open path.
- Added exception/finalizer cleanup so abandoned/faulted carriers do not strand the gate.
- Added no persisted SSK phase, no `StartSSK()` replay, no global coroutine cancellation, no repair-envelope state, and no old-provider compatibility.

## 0.7.0 - Sprint 1D Task 5

- Implemented A10 / Area #2 A2-A10 birthday queue / pending graduation-check continuity.
- Added one `BirthdayQueueBetweenPopups` lease while vanilla `Birthday.Queue` is non-empty, synchronized at the sole vanilla Add/Remove seams.
- Added selective `LoadEpoch` suppression only to `Birthday.<_MoveQueue>d__4.MoveNext` so discarded F9 carriers cannot reread target `Queue[0]`.
- Added stale static birthday-queue cleanup before target idols are reconstructed.
- Added bounded pre-fix reconstruction using target-save date plus same-day current-age `Graduation_History` witnesses; no witness fabricates nothing.
- Added loaded-roster-order pending-tail reconstruction through normal `Birthday.MoveQueue()` with rollback if SNLF-created progression cannot start.
- Added no birthday serialization, repair-envelope record, global coroutine cancellation, deterministic RNG replay, SSK repair, or old-provider compatibility.

## 0.6.0 - Sprint 1D Task 4

- Implemented A09 / Area #2 A2-A08 semantic post-dialogue/post-popup final-frame checkpoint atomicity.
- Added central registration across four audited callback iterator factories, covering the frozen 13-site vanilla scheduling corpus.
- Added `SemanticCallbackFinalFrame` leases from iterator creation through synchronous callback completion.
- Added selective `LoadEpoch` suppression to only the four generated callback `MoveNext()` carriers.
- Added exception-path lease cleanup while returning Harmony's original exception unchanged.
- Added weak iterator-key tracking plus finalizer safety so bookkeeping does not strongly retain abandoned carriers.
- Added read-only A09 diagnostics and source/static guards for all four factories, four generated carriers, and 13 scheduling sites.
- Added no callback serialization, repair-envelope record, global coroutine cancellation, birthday repair, or SSK repair.

## 0.5.0 - Sprint 1D Task 3

- Implemented N06 / audit #6 `Event_Templates` selected-to-popup checkpoint atomicity.
- Added exact pending-open blocker registration on the private `OpenPopup()` iterator factory after successful template construction reaches `_OpenPopup()`.
- Added a selective `LoadEpoch` guard only to the generated `Event_Templates.<OpenPopup>d__11.MoveNext` carrier.
- Added exact one-call `PopupManager.Open(random_event, true)` handoff injection; the SNLF blocker releases only after synchronous `PopupManager.PopupCounter` protection is confirmed.
- Added fail-safe patch health so an unrecognized handoff shape does not acquire a blocker that could become permanent.
- Added read-only N06 diagnostics and source/static regression guards.
- Preserved the audit prohibition on using `Active_Template != null` as a save blocker.
- Added no repair-envelope record, no global coroutine cancellation, and no additional semantic blocker family.

## 0.4.0 - Sprint 1D Task 2

- Added process-local monotonic `LoadEpoch` with capture/current helpers for finite audited deferred carriers.
- Added a stack-compatible career-load adoption seam for both `SaveManager.LoadData(...)` overloads.
- Epoch advancement occurs only for a non-null loaded `SavedData` and immediately before `SaveManager.Data` replacement, so failed loads do not invalidate current-timeline work.
- Added independent two-overload LoadEpoch interception-health diagnostics.
- Added CheckpointGate timeline reset on successful target adoption without reusing blocker token IDs.
- Added source/static LoadEpoch regression guards and retained every Sprint 1A/1B/Task-1 invariant.
- Deliberately did not add `StopAllCoroutines()`, carrier-specific guards, semantic blocker producers, repair-envelope injection, or old-provider compatibility.

## 0.3.0 - Sprint 1D Task 1

- Added the central, nesting-safe `CheckpointGate` registry with disposable named blocker leases.
- Added initial blocker identities for Event Templates pending-open, semantic callback final-frame, birthday between-popup, and SSK post-payment transitions; no semantic producer is active yet.
- Added a `SaveManager.CanAutosave()` postfix that preserves vanilla predicates and suppresses autosave only for active SNLF blockers.
- Added a manual/F5 `SaveManager.SaveData(false, ...)` prefix enforcing vanilla transient checkpoint safety plus SNLF blockers.
- Added guarded in-game prefixes for both `SaveManager.LoadData(...)` overloads, with main-menu bypass and explicit rejection diagnostics.
- Added public read-only CheckpointGate diagnostics while keeping semantic repair completion false.
- Added source/static CheckpointGate regression guards and retained all Sprint 1A/1B transport invariants.
- Kept `LoadEpoch`, repair-envelope injection, semantic blocker producers, and old-provider compatibility out of Task 1.

## 0.2.0

- Implemented the embedded ordered transport core while retaining every Sprint 1A project/API/health invariant.
- Added caller-level Harmony transpilers for all 5 audited `SavedData` write callers and all 7 audited reader methods / 8 read call sites.
- Added ordered `GlobalData` write and coordinated `GlobalData` read interception at `SaveManager.SaveGlobalData()` / `LoadGlobalData()`.
- Added caller-thread payload freezing, per-path FIFO foreground writers, same-path read waits, exclusive-file access, and queue-admission-safe exclusive-directory leases.
- Made transport authority fail closed unless all four audited read/write surfaces report exact caller/call-site health.
- Added a timeout-aware exclusive-file admission path when an overlapping directory lease is active.
- Added cumulative foundation, embedded-transport, source-manifest, and snapshot-boundary static/source tests.
- Kept repair-envelope injection, semantic repairs, `CheckpointGate`, `LoadEpoch`, and old SWOF/old IMDataCore compatibility out of Sprint 1B.

## 0.1.0

- Added the initial `Save n Load Fixes` project with assembly/Harmony ID `com.cosmo.savenloadfixes`.
- Added public foundation and transport diagnostics.
- Added central patch-health bookkeeping for 5/5 `SavedData` writes, 7 callers/8 call sites for `SavedData` reads, and the 1/1 `GlobalData` write/read surfaces planned for Sprint 1B.
- Added a fail-closed transport API skeleton; no save I/O is intercepted in this release.
- Added a source-level `DataSaver` call-site guard against the audited decompiled vanilla source.
- Explicitly deferred compatibility shims for older Save Write Ordering Fix and older IMDataCore releases until those mods are updated.
