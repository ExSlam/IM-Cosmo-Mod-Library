# IM Data Core

IM Data Core is a lightweight history and supplemental-data framework for Idol
Manager. It gives other mods a stable way to persist rollback-safe custom JSON
and timeline events without each mod reinventing save handling.

This documentation is written for readers with no prior knowledge of:
- Idol Manager internals
- Harmony patching
- Mod save lifecycle design

If you are new to modding, start with `docs/START_HERE.md`.

## What IM Data Core does

IM Data Core provides 3 services:
1. Namespaced key/value storage for custom JSON (`TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`)
2. Timeline event ledger for built-in and custom events (`TryAppendCustomEvent`, `TryReadRecentEventsForIdol`)
3. Exact save-scoped cash history (`TryReadMoneyTransactions`, `TryGetMoneyLedgerCoverageStart`)

It also captures many base-game lifecycle changes (singles, shows, contracts, tours, relationships, finance, etc.) into its own event stream.

## Why this exists

Base-game save data is oriented around restoring current game state, not preserving full historical event timelines for mod analytics. Many in-memory transitions are ephemeral. IM Data Core captures and persists these transitions to a save-scoped backend so mods can build features like:
- Career timelines
- Audit/history UIs
- Derived statistics from historical events
- Cross-session mod state caches

## Key concepts (plain language)

- `Namespace`: Your mod's ownership scope inside IM Data Core (for isolation).
- `Session`: A token returned when your namespace is registered; required for API calls.
- `Custom JSON`: Your mod's own saved state blobs keyed by `dataKey`.
- `Custom event`: A timeline row your mod appends with `entityKind`, `eventType`, `payloadJson`.
- `save_key`: IM Data Core's identifier for the currently active game save scope.

## Runtime behavior and persistence model

### Save scope
IM Data Core binds to the exact vanilla save file selected for load or write. The
visible sidecar directory mirrors that save's path below Idol Manager's `data`
folder, while the internal `save_key` remains stable for existing API consumers.

### Lightweight sidecar

Version 2.0 uses one JSON sidecar implementation. There is no normal-runtime
SQLite provider, database schema, or fallback backend. The exact path below
vanilla's `data` directory is mirrored below its sibling `IMDataCore` directory:

- `data\auto_save.json` -> `IMDataCore\auto_save.json`
- `data\manual_saves\1c5ec635\save.json` ->
  `IMDataCore\manual_saves\1c5ec635\save.json`
- `data\story_mode\<playthrough>\auto_save.json` ->
  `IMDataCore\story_mode\<playthrough>\auto_save.json`
- `data\story_mode\<playthrough>\chapter_3\save.json` ->
  `IMDataCore\story_mode\<playthrough>\chapter_3\save.json`

Opaque vanilla path segments are preserved verbatim. There is no required
`IMDataCore\saves` layer. The sidecar identifies itself as
`IMDataCore.LightweightSidecar`, format version `1`; it is not a copy of the
vanilla JSON. See [`docs/STORAGE_LAYOUT.md`](docs/STORAGE_LAYOUT.md).

Typical Windows `Application.persistentDataPath`:
- `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager`

### Persistence and rollback

Ordinary events and custom JSON mutations update memory only. IMDC serializes
once at a real vanilla save boundary, or when a consumer explicitly calls
`TryFlushNow`. There is no three-second timer, event-count threshold, filesystem
poll, or per-frame transaction pump.

Each tiny checkpoint maps vanilla's exact `(relative path, LastSave, playtime,
game date)` stamp to an IMDC mutation-sequence watermark. Loading a known save
activates only IMDC history through its mapped sequence and rebuilds the custom
dictionary/indexes. If no exact checkpoint exists, the loaded vanilla game date
is a conservative fallback cutoff. Newer sidecar history remains unchanged on
disk until the active branch is later saved or explicitly flushed.

Vanilla remains canonical for ordinary game state. IMDC persists historical,
transient, or supplemental data that vanilla does not retain; it never injects
metadata into or rewrites a vanilla save. `data\global_data.json` is explicitly
rejected as a save scope.

### Built-in vs custom payload persistence

- Built-in captures retain historical/capture-time context and stable vanilla
  entity IDs, rather than a second collection of current vanilla objects.
- Custom events retain caller-provided `payloadJson` under the caller's namespace.
- Derived runtime indexes are rebuilt from the event/mutation history instead of
  being serialized as duplicate projections.

## Public API (contract)

Namespace: `IMDataCore`
Main facade: `IMDataCoreApi`

Methods:
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

- `IMDataCoreApi` and compatibility alias `IMDataCoreAPI` retain their existing public method signatures in 2.0.
- `IMDataCoreSession.NamespaceIdentifier`, `IMDataCoreEvent`, and `IMDataCoreMoneyTransaction` retain their externally visible models.
- Money transactions expose `SectionCode` and optional structured `Details`. Structured details currently cover business contracts, single releases, show episodes, individual idol salaries, individual staff salaries, daily theater attendance, monthly theater streaming, daily cafe results, and concert costs/revenue. Staff salary details include the stable job-role code plus level and progress snapshots for every staff skill; concert details include the ordered song/talk setlist and accident outcomes.
- Single releases and show episodes are represented by paired income/expense records in one transaction group, preserving gross revenue and production cost/weekly budget separately. A zero-cost expense record is retained for digital-only singles.
- Internal controller/storage/patch implementation details are not public API and may evolve without breaking semantic versioning.

The 2.0 breaking change is the private persistence format and migration policy,
not the consumer API. New captures also stop emitting four byte-for-byte legacy
alias rows (`single_participation_recorded`, `show_episode`,
`contract_canceled`, and `status_changed`) when their canonical event is already
emitted. Imported historical aliases remain readable; consumers should use the
canonical `single_released`, `show_episode_released`, `contract_cancelled`, and
`idol_status_changed` names for new data.

New 2.0 payloads also omit identifiers already present in `EntityId`
(`single_id`, `show_id`, `concert_id`, `tour_id`, `election_id`, and
`relationship_pair_key`) plus lifecycle/status echoes already encoded by the
event type. Historical imported payloads remain byte-for-byte readable.

## Token and quota rules

Allowed token characters:
- `a-z`, `A-Z`, `0-9`, `_`, `-`, `.`

Length limits:
- Namespace: `3..64`
- Data key: `1..128`
- Entity kind: `1..64`
- Event type: `1..64`
- Source patch (sanitized): max `128`

Custom JSON quotas (per save + namespace):
- Max keys: `4096`
- Max total value chars: `5 MB`
- Max single value chars: `65536`

## Minimal integration flow

1. Reference `com.cosmo.imdatacore.dll`.
2. Wait for `IMDataCoreApi.IsReady()`.
3. Register one namespace.
4. Save/read custom JSON and append custom events.
5. Optionally unregister session on shutdown.

Reference snippet for `.csproj`:

```xml
<ItemGroup>
  <Reference Include="com.cosmo.imdatacore">
    <HintPath>..\..\path\to\com.cosmo.imdatacore.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

See full beginner walkthrough: `docs/START_HERE.md`.

## Source layout (current)

After refactor, source code is intentionally split by responsibility:

- `src/IMDataCore.cs`
  - Core constants
  - Public API facade (`IMDataCoreApi` / compatibility alias)
  - Main controller core (`IMDataCoreController` root + shared logic)
  - Payload and transient pre/post-capture models plus shared utilities
- `src/Core/`
  - `CorePathsAndRuntime.cs` (contained vanilla-path mirroring, logical keys,
    and read-only legacy-source discovery)
- `src/Storage/`
  - `LightweightCoreStorageEngine.cs` (the sole runtime sidecar)
  - `LegacyFlatFileImporter.cs` (isolated, read-only compatibility importer)
- `src/Controller/`
  - `IMDataCoreController` capture partials split by domain clusters
- `src/Patches/`
  - Harmony patch classes split by domain (`Singles`, `Shows`, `Contracts`, etc.)

This split is structural only; API contracts and patch targets remain unchanged.

## Harmony basics (for newcomers)

Harmony lets mods intercept game methods without editing game binaries directly.

Common patch styles:
- Prefix: runs before original method
- Postfix: runs after original method
- Transpiler: rewrites IL (advanced)

IM Data Core mostly uses prefix/postfix capture patches. If you are writing a consumer mod, you do not need to patch IM Data Core itself; you only call its API from your own patches.

## Troubleshooting

### `IsReady()` is false
IM Data Core initializes after gameplay UI startup. Initialize later (for example after `PopupManager.Start`).

### Namespace registration fails
Likely causes:
- Namespace already claimed by another assembly
- Invalid namespace token format

Use a unique reverse-domain namespace, e.g. `com.yourname.yourmod`.

### `TryGetCustomJson` returns false, empty error
Usually means key not found, not a hard failure.

### Writes seem delayed
Mutations are intentionally memory-first. They persist at the next vanilla save.
Call `TryFlushNow` when you need immediate sidecar persistence; it returns a
clean failure before the game has a physical save scope.

### Custom event rejected
Validate `entityKind` and `eventType` token format/length and ensure payload is valid JSON text.

## Compatibility and safety guidance

- Keep patch logic additive and exception-safe.
- Do not write directly to the IM Data Core sidecar from external mods.
- Use API calls for forward compatibility.
- Use stable `sourcePatch` strings so event provenance stays readable.

## Documentation map

- `docs/START_HERE.md`
  - Beginner, end-to-end first integration
- `docs/COOKBOOK.md`
  - Reusable implementation patterns with rationale
- `docs/NAMING_CONVENTIONS.md`
  - Rename safety and contract boundaries
- `docs/EVENT_CATALOG.md`
  - Generated built-in event and payload field catalog

Regenerate catalog after changing event/field constants in source:

```powershell
.\scripts\Generate-EventCatalog.ps1
```

The generator scans `src/**/*.cs` so refactors that move constant declarations do not break catalog generation.

## Maintainer notes

When refactoring this codebase:
- Keep public API signatures unchanged unless intentionally versioning.
- Keep Harmony target symbols unchanged unless intentional behavior change.
- Prefer small commits with a build after each structural move.
- Avoid reintroducing disabled duplicate legacy storage blocks.
