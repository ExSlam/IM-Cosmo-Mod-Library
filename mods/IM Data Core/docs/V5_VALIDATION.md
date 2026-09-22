# IM Data Core 3.4.24 validation notes (historical)


> **Current storage compatibility policy (3.4.34+):** IM Data Core supports only **sidecar v6 + journal v3**. Sidecar v1-v5 and journal v1-v2 are unsupported release inputs. IMDC does **not** promise, qualify, or require in-place migration, conversion, adoption, or rewrite from those older storage generations. Encountering an unsupported older generation must fail closed without converting it or overwriting its bytes. Any v5/v2 migration language retained below is historical design/task context, not a current compatibility commitment.

This revision was statically checked against the supplied Cosmo Mod Library source and the supplied decompiled Idol Manager source. No Unity/.NET compiler or game runtime was available in the analysis environment, so these notes intentionally distinguish static verification from runtime testing.

## Current 3.4.34 compatibility disposition

The migration checks below document an abandoned development path. IMDC 3.4.34 supports only sidecar v6 + journal v3. v1-v5 sidecars and v1-v2 journals are not supported for migration into the current format and are not release-qualification fixtures.

## Wave 0 task 1 migration checks

- Added a separate bounded migration entry point for lightweight sidecar formats **1 through 5** with a logical target version of **6**.
- The ordinary runtime reader remains exact-v5 and the live writer remains sidecar **5** / journal **2** in this task.
- v1/v2 migration enforces the historically shipped `EventId == Sequence` and stored `GameDateKey == GameDateTime` contracts and parses embedded event/custom JSON exactly once into the structural representation.
- v3 keeps `EnabledMods` provenance unknown; v4 can carry the mod snapshot while the checkpoint content fingerprint remains unknown; v5 requires the content fingerprint and treats a missing early-v5 `AgencyRoomIdentities` member as legacy-unbound rather than as a complete empty map.
- Event and custom-mutation sequence numbers and list order are preserved; duplicate/out-of-order source sequences fail migration rather than being renumbered.
- Pre-2.0 database persistence remains outside this lightweight JSON codec family.
- Source and fixture checks are in `tests/Test-LegacySidecarMigrationSource.py` and `tests/Test-LegacySidecarMigrationContract.py`. A .NET compiler/Unity runtime was not present in the work environment, so no compiled-runtime claim is made for this task.

## Wave 0 task 2 identity-binding schema checks

- Added staged sidecar-v6 checkpoint `IdentityBindingsVersion == 1`, `IdentityBindingsComplete`, and `IdentityBindings` fields without changing the live v5 serializer.
- Complete v6 fixtures cover contract, clique, bullying, and generated-task generation bindings; migrated fixtures use `IdentityBindingsComplete = false` with an empty list to represent legacy-unbound knownness.
- Exact locator containers were rechecked in both the supplied decompiled `SaveManager.SavedData` graph and `Assembly-CSharp.dll`: `business__ActiveProposalsData`, `Relationships__Cliques`, and `tasks__TaskData`.
- Bullying bindings require the parent clique generation plus a bullied-target child locator; the other three families use their serialized container ordinal plus structural fingerprint.
- Canonical `(EntityKind, EntityId)` duplicates, invalid witness hashes, invalid coverage boundaries, wrong family/container pairs, and duplicate legacy candidate keys fail schema validation.
- `tests/Test-V6IdentityBindingSchemaSource.py` proves the normal v5 checkpoint appender contains none of the staged v6 members. `tests/Test-V6IdentityBindingSchemaContract.py` covers the physical fixture contract.

## Wave 0 task 3 journal-v3 checks

- Added staged journal format **3** header and transaction fixtures while keeping the live runtime journal at **2**.
- BEGIN/COMMIT framing counts seven audited v6/v3 collections: checkpoint, event, custom mutation, coverage capability set, coverage transition, namespace owner binding, and historical baseline assertion.
- Staged v3 `CHECKPOINT` rows use the v6 checkpoint identity-binding serializer/reader; existing event/custom mutation rows reuse their current structural codecs.
- All seven frozen journal-v3 semantic row families now have codecs. The retained deferred-extension compatibility helper validates monotonic counts and returns false because no known v3 family remains deferred.
- COMMIT target counts and `LastIssuedSequence` must match BEGIN and observed rows exactly. Torn/uncommitted transactions remain non-visible.
- `tests/Test-JournalV3SchemaSource.py` proves the live v2 serializer still uses the v5 checkpoint appender and live constants remain sidecar 5 / journal 2. `tests/Test-JournalV3SchemaContract.py` exercises format-3 framing, count matching, v6 checkpoint shape, COMMIT mismatch rejection, and committed coverage/owner/baseline extension rows.
- Finding #58 was intentionally not part of Task 3; Task 4 below now supplies the shared affinity-before-version classifier while retaining the strict-v3 direct schema fixture.

## Wave 0 task 4 journal-affinity checks

- Added a minimal version-agnostic header envelope containing `FormatName`, positive `FormatVersion`, and a syntactically valid 64-hex SHA-256 `BaseFileHash`.
- The classifier compares the stored base hash with the candidate compact-base hash before deciding whether the declared journal version is supported.
- Supported wrong-hash and unsupported wrong-hash journals both classify as stale `HeaderMismatch` without parsing a transaction body.
- Unsupported matching-hash journals remain hard failures because they may contain authoritative committed rows not present in the compact base.
- Backup recovery now allows an unsupported wrong-hash preferred journal to fall through to a valid sibling backup journal while a matching unsupported preferred journal stops fallback.
- Existing-journal append validation uses the same classifier, preventing append to a wrong-generation or unsupported matching journal.
- The staged v3 selector wraps the same generic classifier with journal version 3, so planned v6/v3 migration/recovery cannot regress to version-before-affinity ordering.
- `tests/Test-JournalAffinitySource.py` and `tests/Test-JournalAffinityContract.py` cover finding #58 / regression #80 semantics.

## Persistence changes checked statically

- The runtime writes and accepts sidecar format **5** only and keeps transactional journal format **2**.
- Journal transaction bodies are replayed only for matching format-2 generations, but header probing is version-agnostic through base-hash affinity: unsupported wrong-generation suffixes are stale; unsupported matching suffixes fail closed.
- Every v5 checkpoint requires a valid `sha256:<64 lowercase hex>` `ContentFingerprint`.
- Exact checkpoint identity includes normalized relative path, vanilla `LastSave`, playtime seconds, vanilla game date/time, and the content fingerprint.
- Standalone save freezing registers the SHA-256 of the compact JSON already used to create the detached `SavedData`, avoiding a second full serialization on that path.
- The SHA-256 helper feeds UTF-8 in bounded chunks and keeps surrogate pairs together across chunk boundaries.
- Loading a physical vanilla save with no existing sidecar seeds an in-memory sequence-0 checkpoint before the engine is installed. A subsequent `TryFlushNow` therefore has an exact vanilla anchor.
- Checkpoint date watermarks parse `checkpoint.GameDateTime` with vanilla `ExtensionMethods.ToDateTime`; event/custom mutation watermarks retain round-trip timestamp parsing.
- New Save still serializes only checkpoints for its target physical path without discarding the active multi-path checkpoint ledger needed by later Overwrite Save operations.
- Physical sidecar I/O remains process-wide per canonical path. Loads, writes, and background compaction use a shared persistence-topology lease.
- Deleted-save archival uses an exclusive persistence-topology lease and advances per-path archive epochs at boundary completion so stale prepared snapshots cannot become current again.
- Archive naming is non-destructive and collision-safe: `nameOLD`, `nameOLD2`, `nameOLD3`, ... .
- If archival fails, the source directory is preserved and writes beneath that deleted scope are blocked for the process.
- Deleting the active save detaches its physical scope while retaining the logical in-memory branch.
- Standalone defensive `SavedData` cloning is layered: normal `FromJson`, then a Unity-serialized-field graph clone. The fallback clone is reserialized; when the original compact JSON exists, equivalence is required before the clone is trusted. `FromJsonOverwrite` is intentionally not used because Idol Manager's UnityEngine API does not expose it. The outer Harmony boundary remains fail-open only after all detachment strategies fail, so IMDC still cannot block vanilla saving.
- Backup/journal recovery still requires exact checkpoint activation after a document is recovered.
- Every v5 checkpoint requires `AgencyRoomIdentities`. The array may be empty, but omission is invalid; records require non-empty unique generation IDs and valid saved floor/room/type metadata.
- Room-identity restoration validates the required snapshot against the exact vanilla `SavedData` room layout before binding it to reconstructed rooms. A present but layout-incompatible snapshot falls forward to new generation IDs rather than binding history to the wrong room; an omitted field is rejected during v5 decoding.
- Historical `agency_room`, `theater`, and `cafe` `EntityId` values use the IMDC room generation; raw runtime/recyclable vanilla IDs remain payload data only.

## Vanilla targets checked

The supplied decompiled game has three relevant user-save directory deletion methods, all patched by `CoreSaveDeletionPatches.cs`:

1. `Popup_Save.Delete()` deletes `data/manual_saves/<id>` and swallows `Directory.Delete` errors. IMDC captures the path in Prefix and its Harmony Finalizer archives only if the directory is absent afterward.
2. `Popup_Load_Story.Delete_Save(save_info)` deletes `Save.GetDirectory()`. Its Finalizer checks actual directory absence, so a successful deletion is still archived if later vanilla UI cleanup throws, while a failed deletion is left alone. The original exception is returned unchanged.
3. `Popup_Load_Story.Delete_Playthrough(playthrough_info)` deletes `Playthrough.Dir`. The same Finalizer rule archives the mirrored playthrough subtree as one unit without changing vanilla exception behavior.

Story autosaves hide their delete UI in `Playthrough_Save.Set`, so the apparent possibility of deleting the broad story/data root is not an ordinary vanilla UI path. IMDC path containment also refuses the private IMDC root itself as an archive source.

## Completed static distribution checks

Before packaging this source revision, the following checks were completed successfully:

- `assets/info.json` parsed as strict JSON and reports version **3.4.24**.
- `IM Data Core.csproj` parsed as XML and reports version **3.4.24**.
- Runtime sidecar validation requires `FormatVersion == SidecarFormatVersion == 5`; `JournalFormatVersion == 2`.
- Every checkpoint construction/serialization path includes `ContentFingerprint`, and every persistence snapshot construction includes `PathArchiveEpoch`.
- All three deletion patch target methods and their exact vanilla deletion paths were rechecked in the supplied decompilation.
- All three deletion hooks use Harmony Finalizers and preserve the original vanilla exception unchanged.
- No Git command was invoked and no Git working tree, index, commit, or metadata was modified; no Git metadata is packaged.
- All 43 C# sources pass a string/comment-aware delimiter scan, and all 32 cumulative deterministic source/fixture scripts pass.
- Every full staged-v6 checkpoint fixture carries explicit `IdentityCandidates`; migrated legacy-unbound checkpoints keep `IdentityBindingsComplete = false` and no exact aliases.
- Shared identity fixtures prove deterministic adoption, ambiguous contract and SSK/tour legacy candidates, one bounded exact-alias interval, and unresolved lookup behavior.
- Public #66 resolver source checks cover preferred, reflection-friendly, and uppercase facades and verify live-v5 gating of non-durable opaque generations/candidate state.
- Current-facing documentation contains no stale claim that v2-v4 sidecars are accepted; obsolete v2-v4 schema/migration/validation/implementation-note files are removed from the packaged tree.
- The repository ignores `*.dll`, `*.pdb`, `**/bin/`, `**/obj/`, and `artifacts/`; stale generated DLL revision metadata is not treated as source-version authority.
- The Pass 1 standalone snapshot helper preserves the original five vanilla `SavedData` call sites and does not change SWOF Harmony ordering.

These are static checks, not a substitute for compilation or in-game regression testing.

## Recommended in-game regression matrix

- Save twice to the same physical slot within one wall-clock second after changing vanilla state; verify the two v5 checkpoints have different content fingerprints when `SavedData` differs.
- Save, reload, and verify the compact reserialization fingerprint matches the checkpoint created at save time.
- Load a vanilla career that has never had IMDC persistence, mutate IMDC state, call `TryFlushNow`, restart, and verify exact checkpoint activation succeeds.
- Load an existing valid v5 sidecar with no matching checkpoint and verify IMDC fails closed without overwriting it.
- Overwrite A -> New Save B -> Overwrite A, with and without newly captured events.
- Trigger background compaction while deleting the same vanilla save and verify the archived IMDC directory contains a coherent generation and the original path is not recreated by a stale writer.
- Delete the same recycled save identifier repeatedly and verify `OLD`, `OLD2`, `OLD3`, ... archives coexist without overwrite.
- Delete an active save, then perform New Save/Save As and verify the logical in-memory history follows the new path without rewriting the archived old path.
- Force archive rename failure (for example with an external file lock/permission denial), verify the IMDC source directory remains intact, and verify writes to that deleted scope are blocked for the remainder of the process.
- Kill the process at journal BEGIN/record/COMMIT boundaries and during compaction replacement; committed transactions must replay once and torn transactions must not become visible.
- Construct interrupted-compaction recovery with a valid `.imdc.bak` plus matching primary `.imdc.journal`, no backup journal, and a corrupt primary base. Recover, persist once, then verify `.imdc.bak.imdc.journal` exists and the preserved backup generation still reconstructs the same document after the primary journal is cleaned. Inject a backup-journal copy failure and verify the original primary journal is retained instead.
- Construct backup recovery with a valid backup base and valid `.imdc.bak.imdc.journal`, while the preferred primary journal is (a) empty and (b) torn before a complete header. Both cases must fall through to the valid backup journal; neither may report a positive base-hash match.
- Pair a healthy compact base with (a) a supported wrong-hash journal, (b) an unsupported wrong-hash journal, and (c) an unsupported matching-hash journal. Cases (a)/(b) must classify stale and force a clean future snapshot; case (c) must hard-fail. Repeat during backup recovery and verify only the nonmatching preferred journal falls through to the valid sibling journal.
- Seed sidecar-derived snapshot and backup-journal temp files older than 24 hours plus fresh equivalents and unrelated `.tmp` files. Initializing that physical scope must remove only the stale IMDC-owned candidates.
- Save with Harmony, JSON-only, and multi-DLL mods enabled; then change their state and verify checkpoint mod diagnostics remain diagnostic-only.
- Build at least two rooms, save, restart, and verify each reconstructed room retains the same IMDC generation `EntityId`; destroy/rebuild the highest-numbered room and verify the new room receives a different generation.
- Destroy the highest-numbered theater and cafe, rebuild so vanilla reuses the raw ID, and verify timeline grouping remains separated by IMDC generation while payload `theater_id` / `cafe_id` still expose the reused vanilla value.
- Remove `AgencyRoomIdentities` from an otherwise valid format-5 checkpoint and verify the sidecar is rejected as malformed rather than treated as an older compatible v5 schema.

## Wave-1 Task-1 staged contract-generation identity checks

The cumulative source/fixture suite additionally runs `Test-ContractGenerationIdentitySource.py` and `Test-ContractGenerationIdentityContract.py`. The source test requires `business.Accept` to reserve a generation before nested `AddActiveProposal`, verifies normal and exceptional cleanup of the nesting-safe pending frame, requires direct/modded insertions to allocate independently, and checks that activation is bound before its history capture. Acceptance, activation, weekly, cancellation, completion, and break-event capture routes all pass through the generation-aware resolver; terminal bindings are retired only after the terminal row is enqueued.

The exact checkpoint fixture deliberately contains two byte-identical `business__ActiveProposalsData` rows with the same legacy `idol|type|end-day` key. They retain different canonical `g:` generations because their exact checkpoint locators are `(serialized ordinal, structural witness)`. The witness is SHA-256 over exactly the vanilla-saved active-proposal fields; it is validation metadata and is never used as a substitute contract identity or gameplay restoration source.

The supplied decompiled `business.cs` is the source oracle for this contract: `Accept()` synchronously calls `AddActiveProposal(...)`; `GetActiveProposalsDataForSaving()` writes the active-proposal DTO list in order; and `LoadFunction()` reconstructs `ActiveProposals` from that ordered list. Final package validation rechecks those seams plus the corresponding `Assembly-CSharp.dll` symbols. Canonical generation emission remains gated while the runtime constants are sidecar 5 / journal 2, so this development build does not create generation-keyed history that v5 cannot persist across restart.

## Wave-0 Task-5 staged participant-schema checks

The cumulative source/fixture suite additionally runs `Test-ParticipantSchemaSource.py` and `Test-ParticipantSchemaContract.py`. They assert that live v5 serialization does not gain the staged field, staged v6/v3 does carry it, v2-v5 shared candidates remain strict, bounded v1 compatibility derives only source-provable redundant counts, unknown legacy identity is never guessed, and contradictory current metadata remains malformed.

## Wave-0 Task-10 migration-provenance and downgrade checks

The cumulative source/fixture suite additionally runs `Test-MigrationProvenanceSource.py` and `Test-MigrationProvenanceContract.py`. They require a validated v6 `MigrationProvenance` root, distinguish `native_v6` from deterministic `legacy_migration`, reject bad conversion IDs and backdated migrated coverage, and verify that every full-v6 fixture carries explicit provenance.

The source contract also verifies the live downgrade boundary: an unsupported primary sidecar write-protects the current save scope before `.imdc.bak` recovery is attempted; an unsupported matching-base journal sets the same protection only after #58 proves affinity; and an unsupported wrong-hash journal remains stale `HeaderMismatch` rather than poisoning a healthy base. Live wire constants remain sidecar 5 / journal 2.

## Wave-0 Task-8 staged coverage/capability checks

The cumulative source/fixture suite additionally runs `Test-CoverageSchemaSource.py` and `Test-CoverageSchemaContract.py`. They assert required v6 coverage collections/model version, canonical immutable capability-set identity, durable-owner-bound namespace descriptors, exact checkpoint anchors, shared event/custom/coverage sequence uniqueness, conservative v1-v5 migration with no backdated frontier, and implemented journal-v3 capability/coverage rows. Live v5/v2 serialization remains isolated. The cumulative suite also runs `Test-HistoricalBaselineSource.py` and `Test-HistoricalBaselineContract.py`, covering exact/ambiguous/unknown #63 evidence, exact checkpoint anchors, shared-sequence collisions, conservative legacy-empty migration, public quality staging, and the completed v3 baseline row.

## Wave-1 Task-3 staged bullying-episode identity checks

The cumulative source/fixture suite additionally runs `Test-BullyingEpisodeIdentitySource.py` and `Test-BullyingEpisodeIdentityContract.py`. Source guards require a canonical `b:` episode to begin only on the real `AddBulliedGirl` false -> true transition, preserve that episode through pre-stop snapshots and leader succession, retire it only after terminal capture, project it as a child of the exact `q:` clique binding, and associate child bindings only after clique rebinding.

The recurrence fixture proves two separate episodes against the same target may share the same parent clique generation, child locator, and exact structural witness while retaining different `b:` identities. The first episode also records both old and new `leader|target` compatibility keys after leader succession without treating either key as canonical identity.
