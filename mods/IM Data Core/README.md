# IM Data Core 3.4.7

IM Data Core is the shared persistence and historical-event backend used by Cosmo Idol Manager mods. It keeps mod-owned state and selected gameplay history tied to the exact vanilla save file without modifying vanilla save JSON.

IMDC 3.4.7 writes sidecar format 5 and transactional journal format 2. Runtime IMDC accepts exactly sidecar format 5; older sidecars are left untouched rather than migrated at runtime.

IMDC 3.4.7 uses SHA-256 content-fingerprinted exact-save checkpoints, keeps the enabled-mod inventory and durable agency-room generation map, anchors newly adopted vanilla careers before any explicit IMDC-only flush, and preserves deleted-save sidecars under `OLD` archive directories. Append-only generations use the SHA-256-bound transactional journal and periodically compact into the atomic v5 snapshot.

## Services

- Namespaced custom JSON: `TrySetCustomJson`, `TryGetCustomJson`, `TryRemoveCustomJson`
- Timeline events: `TryAppendCustomEvent`, `TryAppendCustomEventOnce`, `TryReadRecentEventsForIdol`, `TryReadEventsForIdolPage`
- Exact cash ledger: `TryReadMoneyTransactions`, `TryGetMoneyTransactionTotals`, `TryGetMoneyLedgerCoverageStart`
- Explicit sidecar persistence: `TryFlushNow`
- Active physical-save identity: `TryGetActiveSaveKey`
- Read-only persistence telemetry: `TryGetPersistenceDiagnostics`

Built-in capture covers singles, shows, contracts, groups, tours, elections, concerts, idols, staff, relationships, finance, activities, story/system transitions, and other gameplay events. See [`docs/EVENT_CATALOG.md`](docs/EVENT_CATALOG.md).

The Event Catalog separates **143 queryable built-in event types** from three internal transient streams (`idol_status_changed`, `research_points_accrued`, and `idol_earnings_recorded`) that retention intentionally removes before public timeline queries. Catalog presence does not imply queryability for those explicitly marked transient types.

Lifecycle capture is postcondition-driven where vanilla can legally no-op: IMDC records the requested action only after the relevant state mutation is observed. In particular, static show cancellation is keyed to actual removal from `Shows.shows`, audition starts are suppressed on the terminal-scandal early-return path, random-event starts require a newly appended active event, loan payoff requires `active before -> inactive after`, idol hiring requires `absent before -> contained after`, rival trend refresh requires both vanilla eligibility and a changed update marker, and agency room destruction requires `contained before -> absent after`. Missing Harmony pre-state is treated as unknown rather than a legitimate default state. Staff severance money metadata is scoped only around the actual `Fire_Severance()` deduction and is cleared on both normal and exceptional exits.

Show history has an additional compaction rule because IMDC deliberately observes certain show mutations twice: once at the ordinary vanilla hook and once after known cast-mutating mods settle. Capture-triggered threshold flushing is deferred while that post-mod settlement is open, and the show editor scope spans the complete `Show_Popup.OnContinue()` commit. This keeps the ordinary and canonical rows in the same pending batch so canonical show episode/cast compaction cannot change the current-session answer merely because the 256-event threshold was crossed between the two observations. Explicit forced save/read flushes remain authoritative and are not silently disabled.

`single_status_changed`, `show_status_changed`, and `tour_status_changed` are **setter-observation streams**, not exhaustive state-transition journals. Vanilla performs some lifecycle transitions through direct field assignments: initial single release, initial show release, and tour completion are represented by `single_released`, `show_released`, and `tour_finished` respectively. Consumers reconstructing lifecycle history should use the dedicated lifecycle events rather than assuming every status mutation appears as `*_status_changed`.

### Durable room/theater/cafe history identity

Vanilla agency-room IDs are runtime-only, and theater/cafe IDs can be recycled after the highest-numbered instance is destroyed. IMDC therefore does **not** use those vanilla IDs as durable timeline identity for `agency_room`, `theater`, or `cafe` history. Each physical agency room receives an IMDC-owned generation identifier such as `g:<guid>`; theater and cafe events use the generation of their owning room within their own `EntityKind` namespace. Room-work compound identities use the same generation prefix.

The raw vanilla `room_id`, `theater_id`, and `cafe_id` payload fields are retained for immediate game-state correlation. They are not stable historical keys. Consumers grouping historical rows should use `(EntityKind, EntityId)`.

Every v5 checkpoint freezes the room-generation map in vanilla's serialized floor/room order and reassociates it while vanilla reconstructs rooms on load. `AgencyRoomIdentities` is required in every accepted v5 checkpoint, including an empty array when the save contains no rooms. A format-5 checkpoint that omits the field is invalid rather than treated as an older compatible schema.

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

Every v5 checkpoint freezes a required `AgencyRoomIdentities` snapshot. It records one IMDC room-generation ID for each serialized vanilla room and is used only to restore durable historical identity after load; it does not modify vanilla save JSON or change exact-checkpoint identity. The array may be empty for a save with no agency rooms, but the field itself may not be omitted.

When an existing sidecar does not contain an exact checkpoint for the vanilla save being loaded, IMDC 3.4.7 **fails closed**. It detaches supplemental state for that physical save, protects the existing sidecar from overwrite, and does not activate history using a date-only approximation.

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

Recovery tracks the exact journal whose parsed header matched the backup base. If an interrupted compaction left the matching journal at the primary journal path, the later healing write first publishes that journal durably as `<sidecar>.imdc.bak.imdc.journal`; only then may it remove the stale primary-journal copy. If publication fails, the source journal is kept so the recovered backup generation is not weakened. Empty or first-header-torn preferred journals are not considered base-hash matches and therefore cannot hide a valid backup journal.

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
- when compaction creates `<sidecar>.imdc.bak`, its matching previous journal is preserved as `<sidecar>.imdc.bak.imdc.journal`; if that copy is interrupted or fails, recovery can pair the backup base with the still-present current journal only after parsing a real matching header, and a later healing write preserves that recovery journal beside the backup before removing its primary-path copy;
- missing/empty/first-header-torn journals are distinguished from real base-hash matches, so a torn preferred primary journal cannot mask a valid `.imdc.bak.imdc.journal`;
- when a physical save scope is initialized, IMDC best-effort scavenges only its exact sidecar-derived temp files that are at least 24 hours old; fresh temp files and unrelated files are left alone;
- event payloads and custom SET values cache their validated storage-form JSON, so old immutable rows are not reparsed on later saves;
- the streaming writer copies reusable character buffers directly to its `TextWriter`, avoiding a temporary string allocation for every record;
- forward-save watermarks skip complete history trim scans when no record can lie beyond the checkpoint;
- background compaction snapshots immutable persisted prefixes from the already-loaded engine and verifies the physical base hash/journal length before replacement, avoiding a second full deserialized history graph;
- base history is validated once and committed journal suffixes are validated incrementally; already-monotonic current-format event/mutation lists skip redundant sorting;
- save-boundary single chart reconciliation tracks only unresolved released singles instead of rescanning every historical single;
- runtime locks are released before serialization and durable disk I/O; physical sidecar locks are process-wide per canonical path so replacement engines and old background compactors cannot race the same files;
- `TryGetPersistenceDiagnostics` exposes counts, base/journal sizes, last persistence mode, recovery/block state, and generation information without performing I/O.

Save Write Ordering Fix is an optional optimization. IMDC skips its standalone full `SavedData` JSON clone only when SWOF's public health flag confirms that all five required vanilla write callers were actually intercepted. If verification is unavailable or false, IMDC keeps the defensive clone. The standalone path now has layered detachment: the normal `JsonUtility` round trip followed, if reconstruction fails, by a Unity-serialized-field graph clone whose compact JSON is checked against the original whenever that original JSON was available. The fallback deliberately uses only `JsonUtility` APIs available in Idol Manager's Unity runtime. IMDC therefore does not immediately hand vanilla the live `SaveManager.Data` graph merely because the first JSON reconstruction failed.

When Save Write Ordering Fix 1.3.0 is present, IMDC also acquires SWOF's directory-scoped exclusive lease around vanilla save-directory deletion and IMDC archival. This drains earlier ordered writes before deletion and keeps them from crossing the delete/archive boundary. If SWOF is loaded but cannot grant the boundary, IMDC leaves the save in place instead of accepting a deletion that could be undone by a queued writer. Pass 6 does not support an older loaded SWOF for this boundary: update SWOF to 1.3.0 or newer rather than relying on a compatibility fallback.

Harmony-veto interoperability follows a conservative rule: missing `__state` is unknown, never an empty/default historical state, and lifecycle events require a real after-state transition. IMDC explicitly snapshots before Unavailable Idols Fix on single removal and medical transitions, and before Assistant Manager on `loans.AddLoan`.

## Substory completion after load

Vanilla persists its dialogue queue. IMDC 3.4 rebuilds its transient pending-substory completion counters from that restored queue after load. A dialogue queued before saving can therefore still produce its normal `substory_completed` event after the save is reloaded and the dialogue eventually closes.

## Current-format-only persistence

IMDC 3.4.7 reads only sidecar format 5 and transactional journal format 2. Older lightweight sidecars and unsupported journals are intentionally rejected rather than migrated at runtime.

Pre-2.0 database persistence is also not imported by the runtime mod. Historical migration belongs in a separate purpose-built utility.

## Source versions and generated build artifacts

The source of truth for this development tree is the checked-out source plus project/mod metadata. `IM Data Core.csproj` and `assets/info.json` carry the mod version. Generated DLLs, PDBs, `bin/`, `obj/`, and `artifacts/` are build outputs and are ignored by the repository. A stale locally bundled DLL can therefore report revision metadata from an older build even when the source tree is correct; rebuild the mod from the desired commit instead of treating that generated DLL metadata as an IMDC runtime/source defect.

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

See [`docs/START_HERE.md`](docs/START_HERE.md), [`docs/COOKBOOK.md`](docs/COOKBOOK.md), and [`templates/IMDataCore.TemplateMod`](templates/IMDataCore.TemplateMod/) for integration examples.

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
