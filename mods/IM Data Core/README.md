# IM Data Core 3

IM Data Core is the shared persistence and historical-event backend used by Cosmo Idol Manager mods. It keeps mod-owned state and selected gameplay history tied to the exact vanilla save file without modifying vanilla save JSON.

Version 3 makes the sidecar format document-native JSON while preserving the public API used by existing Cosmo mods.

## Services

- Namespaced custom JSON: `TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`
- Timeline events: `TryAppendCustomEvent`, `TryReadRecentEventsForIdol`
- Exact cash ledger: `TryReadMoneyTransactions`, `TryGetMoneyLedgerCoverageStart`
- Explicit sidecar persistence: `TryFlushNow`
- Active physical-save identity: `TryGetActiveSaveKey`

Built-in capture covers singles, shows, contracts, groups, tours, elections, concerts, idols, staff, relationships, finance, activities, story/system transitions, and other gameplay events. See `docs/EVENT_CATALOG.md`.

## Save ownership

Each physical vanilla save owns one mirrored IMDC sidecar beneath the sibling `IMDataCore` directory:

- `data\auto_save.json` -> `IMDataCore\auto_save.json`
- `data\manual_saves\<id>\save.json` -> `IMDataCore\manual_saves\<id>\save.json`
- `data\story_mode\<playthrough>\chapter_3\save.json` -> `IMDataCore\story_mode\<playthrough>\chapter_3\save.json`

`global_data.json` is not a game-save scope and never receives an IMDC sidecar.

## Version 3 sidecar

The current private disk format is:

- `FormatName`: `IMDataCore.LightweightSidecar`
- `FormatVersion`: `3`

V3 stores actual JSON values rather than JSON encoded inside strings. For example:

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

Custom SET mutations likewise store `Value` as an object, array, number, string, boolean, or null. REMOVE mutations omit `Value`.

The disk format no longer stores:

- `PayloadJson` as an escaped JSON string
- custom `ValueJson` as an escaped JSON string
- `detail_json` as nested encoded JSON for built-in money payloads
- comma-delimited built-in ID lists where an array is appropriate
- duplicated event `EventId` when it is identical to `Sequence`
- derived `GameDateKey`
- checkpoint `RelativeSavePath` repeated on every checkpoint

The public `IMDataCoreEvent.PayloadJson`, `EventId`, and `GameDateKey` members remain available. IMDC reconstructs those views from the v3 document so existing Cosmo consumers do not need to understand the private sidecar schema.

## V1/V2 sidecar compatibility

Existing lightweight sidecars with format version 1 or 2 are still readable. They are normalized in memory and are written as format version 3 at a later successful persistence boundary.

Pre-2.0 database persistence is **not** imported by the runtime mod. Historical database migration belongs in a separate purpose-built migration utility. IMDC 3 does not probe old database files or old fallback locations.

## Corrupt or newer sidecars

An existing sidecar that cannot be safely read is never silently treated as a normal writable empty sidecar.

If the file is corrupt, invalid for the current physical save, or uses a newer unsupported format, IMDC protects that path from overwrite and exposes safe empty supplemental state for the session. Saving to a different physical path can still establish a new writable branch.

Atomic replacement retains one sibling `.imdc.bak` generation as a last-known-good recovery aid.

## Memory-first persistence

Event capture and custom-data mutation happen in memory. The sidecar is serialized at a real vanilla save boundary or when a consumer calls `TryFlushNow`.

There is no persistence polling loop, timer, queue-size flush threshold, SQL transaction pump, or alternate runtime backend.

The sidecar is an event-sourced document containing:

- checkpoints
- source events
- ordered custom-data mutations

Runtime indexes and materialized custom values are rebuilt from those records.

## Vanilla save alignment

Idol Manager serializes vanilla saves on a worker thread. IM Data Core's save hooks use a detached `SaveManager.SavedData` snapshot at the concrete vanilla save call sites so the checkpoint stamp and the object handed to vanilla `DataSaver` describe the same save request.

The mod does not add private fields to `SavedData` and does not Harmony-patch constructed `DataSaver<T>` generic methods.

## Checkpoints and rollback

A checkpoint identifies a vanilla save by:

- document-owned relative vanilla save path
- `LastSave`
- playtime seconds
- game date/time
- IMDC sequence watermark

The active branch is fenced by both sequence and game date. Future-dated history cannot become visible merely because its sequence is below an older checkpoint watermark.

## Custom-data behavior

Consumer custom values must be valid JSON documents. IMDC normalizes them before mutation history is recorded.

Current quotas:

- maximum 4,096 keys per namespace
- maximum 65,536 characters per individual normalized value
- maximum 5 MiB normalized JSON character budget per namespace

Quota accounting is maintained incrementally rather than rescanning the entire namespace on each SET.

A SET to the already-materialized value and a REMOVE of a missing key are logical no-ops and do not grow mutation history.

## Public API

Preferred type: `IMDataCoreApi`.

The compatibility alias `IMDataCoreAPI` remains available.

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
```

See `docs/START_HERE.md`, `docs/COOKBOOK.md`, and `templates/IMDataCore.TemplateMod` for integration examples.

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
│   ├── START_HERE.md
│   ├── COOKBOOK.md
│   ├── EVENT_CATALOG.md
│   ├── NAMING_CONVENTIONS.md
│   ├── STORAGE_LAYOUT.md
│   ├── V3_IMPLEMENTATION_NOTES.md
│   ├── V3_MIGRATION.md
│   └── V3_SIDECAR_SCHEMA.md
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
