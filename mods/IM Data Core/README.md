# IM Data Core 3.3

IM Data Core is the shared persistence and historical-event backend used by Cosmo Idol Manager mods. It keeps mod-owned state and selected gameplay history tied to the exact vanilla save file without modifying vanilla save JSON.

IMDC 3.3 keeps sidecar format version 3 while changing the persistence transport for long campaigns. Append-only save generations are written to a small SHA-256-bound journal and periodically compacted into the existing atomic v3 snapshot; destructive branch changes still force a complete snapshot.

## Services

- Namespaced custom JSON: `TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`
- Timeline events: `TryAppendCustomEvent`, `TryAppendCustomEventOnce`, `TryReadRecentEventsForIdol`, `TryReadEventsForIdolPage`
- Exact cash ledger: `TryReadMoneyTransactions`, `TryGetMoneyLedgerCoverageStart`
- Explicit sidecar persistence: `TryFlushNow`
- Active physical-save identity: `TryGetActiveSaveKey`
- Read-only persistence telemetry: `TryGetPersistenceDiagnostics`

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

When an existing sidecar does not contain an exact checkpoint for the vanilla save being loaded, IMDC 3.3 **fails closed**. It detaches supplemental state for that physical save, protects the existing sidecar from overwrite, and does not activate history using a date-only approximation.

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

IMDC keeps complete source history, so retained disk history still grows with genuine event volume. Version 3.3 avoids reprocessing that complete history on every ordinary save:

- a compact v3 sidecar remains the base snapshot;
- append-only generations are written to `<sidecar>.imdc.journal`, whose header contains the SHA-256 of the exact base file it extends;
- normal save preparation copies only newly appended immutable records, not every historical event;
- a journal is compacted when it reaches at least 1 MiB and is at least as large as its base snapshot, or after 1,024 journal entries;
- rewinds, destructive branch changes, recovery writes, or an incompatible baseline immediately use a full atomic snapshot instead;
- an interrupted final journal line is treated as a torn tail and excluded; a mismatched journal hash is never replayed onto another base;
- when compaction creates `<sidecar>.imdc.bak`, its matching previous journal is preserved as `<sidecar>.imdc.bak.imdc.journal`;
- event payloads and custom SET values cache their validated storage-form JSON, so old immutable rows are not reparsed on later saves;
- the streaming writer copies reusable character buffers directly to its `TextWriter`, avoiding a temporary string allocation for every record;
- forward-save watermarks skip complete history trim scans when no record can lie beyond the checkpoint;
- runtime locks are released before serialization and durable disk I/O, and different sidecar paths use independent persistence locks;
- `TryGetPersistenceDiagnostics` exposes counts, base/journal sizes, last persistence mode, recovery/block state, and generation information without performing I/O.

Save Write Ordering Fix is an optional optimization. IMDC skips its standalone full `SavedData` JSON clone only when SWOF's public health flag confirms that all five required vanilla write callers were actually intercepted. If verification is unavailable or false, IMDC keeps the defensive clone.

## Substory completion after load

Vanilla persists its dialogue queue. IMDC 3.3 rebuilds its transient pending-substory completion counters from that restored queue after load. A dialogue queued before saving can therefore still produce its normal `substory_completed` event after the save is reloaded and the dialogue eventually closes.

## V1/V2 sidecar compatibility

Existing lightweight sidecars with format version 1 or 2 are still readable. They are normalized in memory and written as format version 3 at a later successful persistence boundary.

Pre-2.0 database persistence is not imported by the runtime mod. Historical database migration belongs in a separate purpose-built migration utility. IMDC 3.3 does not probe old database files or old fallback locations.

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

bool IMDataCoreApi.TryReadEventsForIdolPage(
    int idolId,
    long beforeEventIdExclusive,
    int maxCount,
    out List<IMDataCoreEvent> events,
    out bool hasMore,
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
