# IM Data Core

IM Data Core is a backend framework mod for Idol Manager. It gives other mods a stable way to persist custom JSON data and append timeline events without each mod reinventing save handling.

This documentation is written for readers with no prior knowledge of:
- Idol Manager internals
- Harmony patching
- Mod save lifecycle design

If you are new to modding, start with `docs/START_HERE.md`.

## What IM Data Core does

IM Data Core provides 2 services:
1. Namespaced key/value storage for custom JSON (`TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`)
2. Timeline event ledger for built-in and custom events (`TryAppendCustomEvent`, `TryReadRecentEventsForIdol`)

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
IM Data Core derives the active `save_key` from game state and keeps storage partitioned per save.

### Storage backend
At runtime, IM Data Core prefers native SQLite (`winsqlite3`). If unavailable, it falls back to flat JSON file storage.

Per-save storage location:
- `Application.persistentDataPath\Mods\IMDataCore\saves\<save_key>\im_data_core.db`
- fallback: `Application.persistentDataPath\Mods\IMDataCore\saves\<save_key>\im_data_core.fallback.json`

Typical Windows `Application.persistentDataPath`:
- `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager`

### Write buffering and flush policy
Writes are buffered for performance, then flushed automatically by interval/threshold. IM Data Core also flushes before save operations to reduce risk of data loss during save transitions.

### Built-in vs custom payload persistence
- Built-in captured events are stored in canonical event + typed schema forms.
- Custom events retain caller-provided `payloadJson` in the shared event stream under your namespace.

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
- `bool TryFlushNow(out string errorMessage)`
- `bool TryGetActiveSaveKey(out string saveKey, out string errorMessage)`

## 1.0 API stability contract

- `IMDataCoreApi` with compatibility alias `IMDataCoreAPI` public method signatures are stable for the `1.x` line.
- `IMDataCoreSession.NamespaceIdentifier` and `IMDataCoreEvent` public properties are stable for `1.x` consumers.
- Internal controller/storage/patch implementation details are not public API and may evolve without breaking semantic versioning.

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
  - Payload/snapshot models and shared utilities that are still central
- `src/Core/`
  - `CorePathsAndRuntime.cs` (save-key/path resolution + runtime capability probing)
- `src/Storage/`
  - `ICoreStorageEngine.cs`
  - `FlatFileCoreStorageEngine.cs`
  - `SqliteCoreStorageEngine.cs`
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
Writes are buffered. Call `TryFlushNow` when you need immediate persistence.

### Custom event rejected
Validate `entityKind` and `eventType` token format/length and ensure payload is valid JSON text.

## Compatibility and safety guidance

- Keep patch logic additive and exception-safe.
- Do not write directly to IM Data Core tables from external mods.
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
