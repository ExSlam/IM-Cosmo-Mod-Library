# IM Data Core

IM Data Core is a lightweight history and supplemental-data framework for Idol Manager. It gives other mods a stable way to persist rollback-safe custom JSON, append timeline events, and read save-scoped financial history without each mod reinventing vanilla save handling.

This documentation is written for readers with no prior knowledge of:

- Idol Manager internals
- Harmony patching
- Mod save lifecycle design

If you are new to modding, start with `docs/START_HERE.md`.

## What IM Data Core does

IM Data Core provides three main services:

1. Namespaced key/value storage for custom JSON (`TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`)
2. Timeline event ledger for built-in and custom events (`TryAppendCustomEvent`, `TryReadRecentEventsForIdol`)
3. Exact save-scoped cash history (`TryReadMoneyTransactions`, `TryGetMoneyLedgerCoverageStart`)

It also captures many base-game lifecycle changes, including singles, shows, contracts, tours, relationships, staff, idols, finance, activities, stories, concerts, elections, and other gameplay transitions, into its own event stream.

## Why this exists

Base-game save data is designed primarily to restore current game state. It does not preserve every historical transition that a mod may want for analytics, audit views, or long-term timelines.

IM Data Core records selected historical and supplemental information so consumer mods can build features such as:

- Career timelines
- Audit and history interfaces
- Derived statistics from historical events
- Save-scoped mod state
- Financial ledgers
- Cross-session supplemental caches

Vanilla remains authoritative for ordinary game state. IM Data Core stores only its own supplemental history and consumer-owned data.

## Key concepts

- `Namespace`: A consumer mod's ownership scope inside IM Data Core.
- `Session`: A token returned when a namespace is registered. Namespace-owned API calls require it.
- `Custom JSON`: A consumer mod's own saved JSON value, identified by a `dataKey`.
- `Custom event`: A timeline event appended by a consumer mod with an idol/entity, event type, payload, and source patch.
- `save_key`: IM Data Core's logical identifier for the currently active vanilla save scope.
- `Physical save scope`: The exact vanilla save file path that owns a sidecar.
- `Checkpoint`: A mapping between one exact vanilla save stamp and an IM Data Core history watermark.
- `Active branch`: The event and custom-data history currently exposed to API readers after a save is loaded or a branch is rolled back.

The visible save name entered by the player is not used as the physical sidecar identity. IM Data Core follows the actual vanilla save path.

## Runtime behavior and persistence model

### Exact vanilla save scope

IM Data Core binds to the exact vanilla save file selected for load or write.

The sidecar path mirrors the vanilla path below Idol Manager's `data` directory into a sibling `IMDataCore` directory. Opaque vanilla path segments are preserved verbatim.

Examples:

- `data\auto_save.json` -> `IMDataCore\auto_save.json`
- `data\manual_saves\1c5ec635\save.json` -> `IMDataCore\manual_saves\1c5ec635\save.json`
- `data\story_mode\<playthrough>\auto_save.json` -> `IMDataCore\story_mode\<playthrough>\auto_save.json`
- `data\story_mode\<playthrough>\chapter_3\save.json` -> `IMDataCore\story_mode\<playthrough>\chapter_3\save.json`

There is no required `IMDataCore\saves` directory layer.

`data\global_data.json` is not a game-save scope and is explicitly rejected.

Typical Windows `Application.persistentDataPath`:

- `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager`

See `docs/STORAGE_LAYOUT.md` for the complete path mapping and legacy compatibility behavior.

### Lightweight JSON sidecar

IM Data Core 2.0 uses the lightweight JSON sidecar as its only runtime persistence implementation. There is no secondary persistence backend.

The sidecar identifies itself as:

- Format name: `IMDataCore.LightweightSidecar`
- Current format version: `2`

Format-version-1 lightweight sidecars remain readable for compatibility. A later persistence boundary writes the current format.

The sidecar contains source history rather than duplicated read projections. Runtime dictionaries and indexes are rebuilt from the active event and custom-mutation history.

### Memory-first writes

Ordinary event captures and custom JSON mutations update IM Data Core's in-memory active branch first.

IM Data Core serializes the sidecar at a real vanilla save boundary or when a consumer explicitly calls `TryFlushNow`.

There is no three-second timer, event-count persistence threshold, filesystem polling loop, or per-frame transaction pump.

`TryFlushNow` is available when a consumer needs immediate sidecar persistence. Before a physical vanilla save scope exists, it returns a clean failure instead of inventing a save target.

### Stable vanilla save snapshot at save boundaries

Idol Manager's vanilla `DataSaver` performs JSON serialization on a worker thread. Passing the live `SaveManager.SavedData` object directly to that worker would allow the object to change after IM Data Core had already captured its checkpoint stamp.

To keep the vanilla file and IM Data Core checkpoint aligned, the save lifecycle patch now creates one detached `SaveManager.SavedData` snapshot before the asynchronous vanilla write begins.

That same detached snapshot is used for both:

1. IM Data Core checkpoint and sidecar preparation
2. The original vanilla `DataSaver.saveData<SaveManager.SavedData>` call

This prevents IM Data Core from checkpointing one state while vanilla asynchronously serializes a later mutation of the original live object.

IM Data Core does not add private metadata to the vanilla save JSON. The snapshot contains the vanilla `SavedData` representation that was already going to be serialized.

### Load order and save switching

On a successful vanilla load, IM Data Core restores the target supplemental branch immediately after vanilla assigns the newly deserialized `SaveManager.Data` and before vanilla invokes `SaveManager.LoadEvent`.

This timing is intentional. Vanilla load subscribers can mutate runtime globals and save metadata during `LoadEvent`, so the checkpoint identity must be captured from the data that actually came from disk.

A successful load performs one IM Data Core restoration. The later load-completion hook finalizes runtime state but does not restore the sidecar a second time.

When the player switches between saves, IM Data Core replaces the active storage engine with the engine belonging to the newly loaded physical save scope and clears runtime capture buffers before normal gameplay resumes.

### Public API use during vanilla loading

Consumer mods are allowed to read their IM Data Core data while vanilla `LoadEvent` is reconstructing game state.

IM Data Core does not block:

- `TryGetCustomJson`
- Event and ledger reads
- Consumer mods reading their own independent sidecars
- Consumer mods using their own persistence systems
- Consumer mods that reference IM Data Core directly
- Consumer mods that call the public API through reflection

During the load-reconstruction window, the global `staticVars.dateTime` can temporarily still reflect the save that was active before the load. Persistent IM Data Core writes therefore do not blindly use that mutable global.

IM Data Core freezes the newly loaded save's game date from the deserialized vanilla `SavedData` and uses that frozen date for public persistent mutations during the reconstruction window, including:

- `TrySetCustomJson`
- `TryRemoveCustomJson`
- `TryAppendCustomEvent`

If the newly loaded save date cannot be established, a persistent mutation fails cleanly instead of being stamped with the previous save's date.

After vanilla load completion, normal writes resume using the current runtime game date.

### Checkpoints and rollback

Each checkpoint records the exact vanilla save stamp:

- Relative vanilla save path
- `LastSave`
- Playtime seconds
- Vanilla game date/time
- IM Data Core sequence watermark

When an exact checkpoint is found, IM Data Core does not activate rows by sequence alone.

An event, custom-data mutation, or older checkpoint is eligible for the active branch only when it is both:

1. At or below the checkpoint sequence watermark
2. At or before the checkpoint's vanilla game date

This temporal fence prevents a future-dated row from becoming visible merely because its sequence number is below a checkpoint watermark.

If no exact checkpoint exists, IM Data Core falls back conservatively to the loaded vanilla game date and activates only history at or before that date.

### Save-side temporal fence

The same rule is enforced before a checkpoint is persisted.

Before writing a sidecar for a vanilla save, IM Data Core prunes the active branch so the persisted branch cannot contain:

- Events with a sequence newer than the checkpoint
- Custom mutations with a sequence newer than the checkpoint
- Checkpoints newer than the checkpoint
- Events or custom mutations dated after the vanilla save's game date
- Older checkpoint records dated after the vanilla save's game date

This prevents a transient runtime contamination from being cemented into an earlier save's sidecar.

The global issued-sequence watermark is intentionally not rewound when an older branch is activated or pruned. New records therefore continue receiving monotonically increasing identifiers even after loading an earlier save.

### Save As and branched saves

Creating a new vanilla save from the currently loaded game creates a new physical save scope. IM Data Core can carry the currently active history into that new target, but subsequent saves remain isolated by their actual vanilla paths and checkpoints.

For example, if Save B is created from Save A in March, both saves may legitimately share history through March. Continuing Save A into August must not cause Save B's March checkpoint to expose or persist August rows when Save B is loaded again.

The sequence-plus-game-date checkpoint fence is designed to preserve this branch behavior.

### Built-in and custom payload persistence

- Built-in captures retain historical or capture-time context and stable vanilla entity IDs rather than serializing a second copy of current vanilla game objects.
- Custom events retain caller-provided `payloadJson` under the caller's namespace.
- Custom JSON is reconstructed from ordered custom-mutation history.
- Derived runtime indexes are rebuilt instead of being serialized as duplicate projections.

## Public API

Namespace: `IMDataCore`

Main facade: `IMDataCoreApi`

Compatibility alias: `IMDataCoreAPI`

Public methods:

- `bool IsReady()`
- `bool TryRegisterNamespace(string namespaceIdentifier, out IMDataCoreSession session, out string errorMessage)`
- `bool TryUnregisterNamespace(IMDataCoreSession session, out string errorMessage)`
- `bool TrySetCustomJson(IMDataCoreSession session, string dataKey, string jsonValue, out string errorMessage)`
- `bool TryGetCustomJson(IMDataCoreSession session, string dataKey, out string jsonValue, out string errorMessage)`
- `bool TryRemoveCustomJson(IMDataCoreSession session, string dataKey, out string errorMessage)`
- `bool TryAppendCustomEvent(IMDataCoreSession session, int idolId, string entityKind, string entityId, string eventType, string payloadJson, string sourcePatch, out string errorMessage)`
- `bool TryReadRecentEventsForIdol(int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)`
- `bool TryReadMoneyTransactions(DateTime startInclusive, DateTime endExclusive, int maxCount, out List<IMDataCoreMoneyTransaction> transactions, out bool wasTruncated, out string errorMessage)`
- `bool TryGetMoneyLedgerCoverageStart(out DateTime coverageStart, out string errorMessage)`
- `bool TryFlushNow(out string errorMessage)`
- `bool TryGetActiveSaveKey(out string saveKey, out string errorMessage)`

## 2.0 API compatibility contract

- `IMDataCoreApi` and compatibility alias `IMDataCoreAPI` retain their existing public method signatures in the 2.0 line.
- `IMDataCoreSession.NamespaceIdentifier`, `IMDataCoreEvent`, and `IMDataCoreMoneyTransaction` retain their externally visible consumer models.
- Persistence implementation, sidecar format, controller internals, checkpoint mechanics, and Harmony implementation details are private and may evolve without changing the consumer API.
- The 2.0 persistence architecture is lightweight JSON and save-path scoped. Consumers should never depend on old private persistence internals.

Money transactions expose `SectionCode` and optional structured `Details`. Structured details currently cover business contracts, single releases, show episodes, individual idol salaries, individual staff salaries, daily theater attendance, monthly theater streaming, daily cafe results, and concert costs/revenue.

Staff salary details include the stable job-role code plus level and progress snapshots for every staff skill. Concert details include the ordered song/talk setlist and accident outcomes.

Single releases and show episodes are represented by paired income/expense records in one transaction group so gross revenue and production cost or weekly budget remain distinct. A zero-cost expense record is retained for digital-only singles.

New 2.0 captures do not emit redundant aliases or per-idol projection rows when
the canonical event already contains all historical information:

- `single_participation_recorded` -> use `single_released`
- `show_episode` -> use `show_episode_released`
- `contract_canceled` -> use `contract_cancelled`
- `status_changed` -> use `idol_status_changed`
- `concert_participation` -> use the shared concert lifecycle event
- `tour_participation` -> use the shared tour lifecycle/country event
- `election_result_recorded` -> use the shared election result event

Imported historical aliases remain readable.

Built-in multi-idol occurrences now use a first-class shared participant model:
one physical row is stored with `IdolId = -1`, its compact participant IDs
rebuild the nonserialized per-idol indexes, and public reads substitute the
requested idol ID. This covers Shows, Singles, Concerts, Tours, Elections,
completed/cancelled room work, idol-idol relationships, mentorship, random
events, and substories. Missing or malformed participant metadata is
quarantined rather than exposed globally to unrelated idols.

For `single_released`, `single_cast_id_list` preserves senbatsu slot order (`-1`
marks an empty slot), including idols whom vanilla removes from the live
formation after graduation. Per-idol reads derive `idol_id`, `position_index`,
`row_index`, and `is_center`. Election reads similarly derive each idol's
place, votes, fame points, and expected place from the shared primitive ranking
summaries. Concert setlists and common results are therefore stored once rather
than once per participant.

Graduation-sensitive mutable state is captured before vanilla culls it. This
includes released-single formations, cancelled room assignments, active tasks,
individual active contracts, idol-idol relationship rows, mentorship pairs,
clique bullying targets/visibility, and unfinished concert rosters. Graduated
idol IDs remain stable historical identifiers; later live-object cleanup does
not remove their sidecar history.

New 2.0 payloads also omit identifiers that are already represented by `EntityId`, including `single_id`, `show_id`, `concert_id`, `tour_id`, `election_id`, and `relationship_pair_key`, along with lifecycle or status echoes already encoded by the event type. Historical imported payloads remain readable.

## Token and quota rules

Allowed token characters:

- `a-z`
- `A-Z`
- `0-9`
- `_`
- `-`
- `.`

Length limits:

- Namespace: `3..64`
- Data key: `1..128`
- Entity kind: `1..64`
- Event type: `1..64`
- Source patch after sanitization: max `128`

Custom JSON quotas per save and namespace:

- Max keys: `4096`
- Max total value characters: `5 MB`
- Max single value characters: `65536`

## Minimal integration flow

1. Reference `com.cosmo.imdatacore.dll`, or resolve its public API through reflection if you intentionally want an optional runtime integration.
2. Wait for `IMDataCoreApi.IsReady()`.
3. Register one unique namespace.
4. Read and write custom JSON or append custom events as needed.
5. Let normal vanilla saves persist the active IM Data Core branch.
6. Call `TryFlushNow` only when you specifically need immediate sidecar persistence.
7. Optionally unregister the namespace session on shutdown.

Reference snippet for a direct project reference:

```xml
<ItemGroup>
  <Reference Include="com.cosmo.imdatacore">
    <HintPath>..\..\path\to\com.cosmo.imdatacore.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

See `docs/START_HERE.md` for the full beginner walkthrough.

## Consumer persistence guidance

IM Data Core owns only its own sidecar tree and public API state.

A consumer mod may also maintain a separate sidecar or other persistent data independently. IM Data Core does not require every mod to route all persistence through IM Data Core.

If a consumer uses IM Data Core during vanilla loading:

- Reads are supported during the load-reconstruction window.
- Custom IM Data Core writes are protected against inheriting the previous save's date.
- Independent file I/O performed by the consumer remains the consumer's responsibility.
- Avoid writing directly into IM Data Core's sidecar files. Use the public API instead.

For optional integrations, reflection is acceptable as long as the consumer validates the API type and method signatures it expects and handles the dependency being absent.

## Source layout

The source is split by responsibility:

- `src/IMDataCore.cs`
  - Core constants
  - Public API facade (`IMDataCoreApi` and compatibility alias)
  - Main controller core and shared runtime logic
  - Payload and transient capture models
- `src/Core/`
  - `CorePathsAndRuntime.cs`
  - Save-path mirroring, logical save keys, runtime capability probing, and legacy-source discovery
- `src/Storage/`
  - `LightweightCoreStorageEngine.cs`
  - Current lightweight sidecar, checkpoint activation, temporal fencing, custom-data materialization, indexes, and serialization
  - `LegacyFlatFileImporter.cs`
  - Isolated compatibility importer for eligible older data
- `src/Controller/`
  - `IMDataCoreController` capture partials split by domain clusters
  - `IMDataCoreController.PersistenceV2.cs` for save/load branch coordination
- `src/Patches/`
  - Harmony patch classes split by domain
  - `Patches/Core/CoreLifecyclePatches.cs` for vanilla save/load lifecycle binding and stable save snapshots

Internal structure is not part of the public consumer API.

## Harmony basics

Harmony lets mods intercept game methods without editing game binaries directly.

Common patch styles:

- Prefix: runs before the original method
- Postfix: runs after the original method
- Transpiler: rewrites IL

IM Data Core uses all three where appropriate. Consumer mods normally do not need to patch IM Data Core itself. They should call its public API from their own code or patches.

## Troubleshooting

### `IsReady()` is false

IM Data Core initializes after gameplay UI startup. Initialize or retry later, for example after `PopupManager.Start`.

### Namespace registration fails

Likely causes:

- Namespace already claimed by another assembly
- Invalid namespace token format

Use a unique reverse-domain namespace such as `com.yourname.yourmod`.

### `TryGetCustomJson` returns false with an empty error

This normally means the requested key does not exist. It is not necessarily a hard failure.

### Writes seem delayed

Mutations are intentionally memory-first. They normally persist at the next vanilla save boundary.

Call `TryFlushNow` when immediate IM Data Core sidecar persistence is actually required.

### A persistent write fails during vanilla loading

IM Data Core permits reads while vanilla is reconstructing a loaded save. Persistent custom writes require a trustworthy game date for the newly loaded save.

If that date could not be parsed, the write fails rather than using the previous save's runtime date. Log the returned `errorMessage` and retry after load completion if appropriate.

### Custom event rejected

Check that:

- `entityKind` is valid
- `eventType` is valid
- Tokens fit the documented length limits
- `payloadJson` is not null
- The namespace session belongs to the calling assembly

### An older save appears to contain newer history

Current checkpoint activation uses both sequence and game-date ceilings, and save persistence applies the same temporal fence before writing the branch.

If this still occurs:

1. Confirm the vanilla save path and matching mirrored IM Data Core sidecar path.
2. Confirm only one current IM Data Core DLL is loaded.
3. Preserve the vanilla save, sidecar, and IM Data Core log before saving again.
4. Compare the checkpoint's relative path, `LastSave`, playtime, game date, and sequence with the suspect rows.

Do not edit the sidecar by hand unless you have preserved an untouched copy for diagnosis.

### Sidecar reports an unsupported format

Current runtime accepts lightweight sidecar format versions `1` and `2`. Other formats are rejected rather than guessed.

### Sidecar belongs to a different vanilla save path

IM Data Core validates the sidecar's declared relative save path against the physical vanilla save scope. A mismatch is rejected to prevent cross-save attachment.

## Compatibility and safety guidance

- Keep consumer patch logic additive and exception-safe.
- Do not write directly to IM Data Core sidecar files from external mods.
- Use the public API for forward compatibility.
- Use stable `sourcePatch` strings so event provenance remains readable.
- Treat `GameDateTime` and `GameDateKey` as the event occurrence timestamp supplied by IM Data Core. Payload fields may describe other lifecycle dates such as contract end dates or release dates and should not automatically replace the event occurrence date.
- Do not assume the player's visible save name identifies a persistence scope. The actual vanilla save path does.
- Do not assume sequence alone is a sufficient rollback boundary. IM Data Core's internal checkpoint implementation also enforces the vanilla game-date ceiling.
- Consumer-owned sidecars should establish their own save identity and load-order rules instead of relying on IM Data Core internals.

## Documentation map

- `docs/START_HERE.md`
  - Beginner, end-to-end first integration
- `docs/COOKBOOK.md`
  - Reusable implementation patterns with rationale
- `docs/NAMING_CONVENTIONS.md`
  - Rename safety and contract boundaries
- `docs/STORAGE_LAYOUT.md`
  - Physical save-scope mirroring, sidecar layout, and migration behavior
- `docs/EVENT_CATALOG.md`
  - Generated built-in event and payload field catalog

Regenerate the event catalog after changing event or payload-field constants in source:

```powershell
.\scripts\Generate-EventCatalog.ps1
```

The generator scans `src/**/*.cs`, so refactors that move constant declarations do not break catalog generation.

## Maintainer notes

When changing save/load or persistence code:

- Keep public API signatures unchanged unless intentionally versioning the consumer contract.
- Keep Harmony target symbols unchanged unless the behavior change is deliberate and reviewed.
- Restore a loaded IM Data Core branch exactly once per successful vanilla load.
- Capture vanilla save identity from the deserialized or detached `SavedData`, not from runtime globals that may already have changed.
- Use the same detached vanilla `SavedData` snapshot for IM Data Core checkpointing and the asynchronous vanilla save call.
- Preserve both checkpoint sequence and checkpoint game-date fences on load.
- Apply the same sequence and game-date fence before persisting a checkpoint.
- Do not rewind the global issued-sequence watermark when rolling back to an older branch.
- Keep public reads available during vanilla load reconstruction.
- Never timestamp a persistent load-window mutation from a stale previous-save runtime date.
- Keep physical sidecar-path validation strict.
- Prefer focused commits and build after persistence or lifecycle changes.
- Avoid reintroducing duplicate legacy storage paths or a second late restore.
