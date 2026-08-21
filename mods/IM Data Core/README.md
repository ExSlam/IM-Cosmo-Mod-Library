# IM Data Core 3.4

IM Data Core is the shared persistence and historical-event backend used by Cosmo Idol Manager mods. It keeps mod-owned state and selected gameplay history tied to the exact vanilla save file without modifying vanilla save JSON.

IMDC 3.4.6 writes sidecar format 5 and transactional journal format 2. This development build intentionally accepts only sidecar format 5; older sidecars are left untouched rather than migrated at runtime.

IMDC 3.4.6 strengthens exact-save checkpoints with a SHA-256 content fingerprint of the vanilla `SavedData` graph, keeps the enabled-mod inventory, anchors newly adopted vanilla careers before any explicit IMDC-only flush, and preserves deleted-save sidecars under `OLD` archive directories. Append-only generations still use the SHA-256-bound transactional journal and periodically compact into the atomic v5 snapshot.

## Services

- Namespaced custom JSON: `TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`
- Timeline events: `TryAppendCustomEvent`, `TryAppendCustomEventOnce`, `TryReadRecentEventsForIdol`, `TryReadEventsForIdolPage`
- Exact cash ledger: `TryReadMoneyTransactions`, `TryGetMoneyTransactionTotals`, `TryGetMoneyLedgerCoverageStart`
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

When an existing sidecar does not contain an exact checkpoint for the vanilla save being loaded, IMDC 3.4.6 **fails closed**. It detaches supplemental state for that physical save, protects the existing sidecar from overwrite, and does not activate history using a date-only approximation.

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
- when compaction creates `<sidecar>.imdc.bak`, its matching previous journal is preserved as `<sidecar>.imdc.bak.imdc.journal`; if that copy is interrupted or fails, recovery can pair the backup base with the still-present current journal when its stored base hash matches;
- event payloads and custom SET values cache their validated storage-form JSON, so old immutable rows are not reparsed on later saves;
- the streaming writer copies reusable character buffers directly to its `TextWriter`, avoiding a temporary string allocation for every record;
- forward-save watermarks skip complete history trim scans when no record can lie beyond the checkpoint;
- background compaction snapshots immutable persisted prefixes from the already-loaded engine and verifies the physical base hash/journal length before replacement, avoiding a second full deserialized history graph;
- base history is validated once and committed journal suffixes are validated incrementally; already-monotonic current-format event/mutation lists skip redundant sorting;
- save-boundary single chart reconciliation tracks only unresolved released singles instead of rescanning every historical single;
- runtime locks are released before serialization and durable disk I/O; physical sidecar locks are process-wide per canonical path so replacement engines and old background compactors cannot race the same files;
- `TryGetPersistenceDiagnostics` exposes counts, base/journal sizes, last persistence mode, recovery/block state, and generation information without performing I/O.

Save Write Ordering Fix is an optional optimization. IMDC skips its standalone full `SavedData` JSON clone only when SWOF's public health flag confirms that all five required vanilla write callers were actually intercepted. If verification is unavailable or false, IMDC keeps the defensive clone.

## Substory completion after load

Vanilla persists its dialogue queue. IMDC 3.4 rebuilds its transient pending-substory completion counters from that restored queue after load. A dialogue queued before saving can therefore still produce its normal `substory_completed` event after the save is reloaded and the dialogue eventually closes.

## Current-format-only persistence

IMDC 3.4.6 reads only sidecar format 5 and transactional journal format 2. Older lightweight sidecars and unsupported journals are intentionally rejected rather than migrated at runtime.

Pre-2.0 database persistence is also not imported by the runtime mod. Historical migration belongs in a separate purpose-built utility.

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

## Portrait identity

Idol lifecycle events preserve the raw idol type, custom-id/addressable identity, and exact body/hair/face/accessory asset IDs. Consumers can use these vanilla-style references without persisting rendered portrait images.
