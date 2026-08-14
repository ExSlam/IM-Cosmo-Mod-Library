# IM Data Core 3.1

IM Data Core is the shared persistence and historical-event backend used by Cosmo Idol Manager mods. It keeps mod-owned state and selected gameplay history tied to the exact vanilla save file without modifying vanilla save JSON.

IMDC 3.1 keeps sidecar format version 3. The release adds exact-checkpoint fail-closed loading, backup recovery, branch-aware custom-event idempotency, corrected mixed idol/global timeline reads, and lower-memory long-campaign persistence.

## Services

- Namespaced custom JSON: `TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`
- Timeline events: `TryAppendCustomEvent`, `TryAppendCustomEventOnce`, `TryReadRecentEventsForIdol`
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

The current private disk format remains:

- `FormatName`: `IMDataCore.LightweightSidecar`
- `FormatVersion`: `3`

V3 stores actual JSON values rather than JSON encoded inside strings. A built-in event can look like:

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

A namespaced event created through `TryAppendCustomEventOnce` may additionally contain an optional `IdempotencyKey`. Older v3 sidecars do not need this field and remain valid.

The public `IMDataCoreEvent.PayloadJson`, `EventId`, and `GameDateKey` members remain available. IMDC reconstructs those views from the v3 document so consumers do not need to understand the private sidecar schema.

## Exact checkpoint loading

A checkpoint identifies one vanilla save state using its physical relative path, vanilla `LastSave`, playtime seconds, game date/time, and the IMDC sequence watermark.

When an existing sidecar does not contain an exact checkpoint for the vanilla save being loaded, IMDC 3.1 **fails closed**. It detaches supplemental state for that physical save, protects the existing sidecar from overwrite, and does not activate history using a date-only approximation.

This avoids cross-branch leakage when two different save histories happen to share the same in-game date.

## Backup recovery

Atomic replacement retains one sibling:

```text
<sidecar>.imdc.bak
```

If the primary sidecar is unreadable or invalid, IMDC validates the backup. A valid backup can be used as the recovery source for the session. The damaged primary is left untouched during recovery, and the known-good backup is preserved when a later successful save replaces the damaged primary.

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

IMDC keeps complete source history, so sidecar size still grows with genuine event volume. Version 3.1 reduces the avoidable costs around that history:

- save preparation takes shallow immutable record snapshots instead of deep-cloning every event and mutation;
- runtime locks are released before JSON serialization and durable disk I/O;
- JSON is streamed directly to the temporary file instead of building one full sidecar string and then a second full UTF-8 byte array;
- event timeline indexes stay sorted as records enter them, allowing recent idol/global reads to merge directly without per-read full-list sorting;
- loaded records and active branch records reuse immutable event objects where safe;
- persistence logs event count, custom-mutation count, checkpoint count, bytes, and elapsed milliseconds for real campaign profiling;
- when Save Write Ordering Fix is loaded, IMDC avoids an otherwise redundant full `SavedData` JSON clone because that mod freezes the exact vanilla payload synchronously after IMDC's save hook.

These changes target campaigns with many idols and many years of retained history without changing the full-history semantics.

## Substory completion after load

Vanilla persists its dialogue queue. IMDC 3.1 rebuilds its transient pending-substory completion counters from that restored queue after load. A dialogue queued before saving can therefore still produce its normal `substory_completed` event after the save is reloaded and the dialogue eventually closes.

## V1/V2 sidecar compatibility

Existing lightweight sidecars with format version 1 or 2 are still readable. They are normalized in memory and written as format version 3 at a later successful persistence boundary.

Pre-2.0 database persistence is not imported by the runtime mod. Historical database migration belongs in a separate purpose-built migration utility. IMDC 3.1 does not probe old database files or old fallback locations.

## Custom-data behavior

Consumer custom values must be valid JSON documents. IMDC normalizes them before mutation history is recorded.

Current quotas:

- maximum 4,096 keys per namespace
- maximum 65,536 characters per individual normalized value
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
