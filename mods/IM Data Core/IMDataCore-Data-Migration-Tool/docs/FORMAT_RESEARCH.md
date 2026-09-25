# IMDataCore 1.x → current sidecar research notes

These notes were traced directly from the supplied `Cosmo-Mod-Library` Git history and its supplied working tree. The archive is at Git HEAD `5e348d5a0412` with substantial uncommitted IM Data Core changes; the target v6/v3 schema below comes from those supplied working-tree files, not from HEAD alone.

## Release commits

| Release | Commit | Persistence observations |
|---|---|---|
| 1.0.0 | `2606945` | SQLite schema_version `2`; unversioned JSON fallback `FlatFileState`; per-save storage under `Mods\IMDataCore\saves\<saveKey>` |
| 1.1.0 | `d228b03` | Same persistence envelope/schema; adds room-work event types/payloads |
| 1.2.0 | `2928a4c` | Same persistence envelope/schema; adds money-ledger event types/read logic |
| 1.3.0 initial | `e1c7740` | Root moves toward persistent sibling `IMDataCore\saves\<saveKey>` instead of volatile `Mods`; payload envelope still initially unchanged |
| 1.3.0 follow-ups | `0014acb`, `9623c08`, `c978462`, `181c501` | Saving/loading overhaul. Late 1.3 fallback gains format/integrity/checkpoint envelopes; SQLite gains retained save-generation checkpoint snapshot tables |
| 2.0.0 | `3a267a1` | Commit message: JSON append-only architecture; **older IMDataCore saved data is not supported** |

The current supplied working tree reports IM Data Core `3.4.34`; its storage constants are `SidecarFormatVersion = 6` and `JournalFormatVersion = 3`.

## 1.0–early-1.3 fallback JSON layout

The unversioned `FlatFileState` fields, in declaration/Unity-serialization order, are:

```text
NextEventId : Int64
Events : List<FlatFileEventRecord>
CustomData : List<FlatFileCustomDataRecord>
SingleParticipation : List<FlatFileSingleParticipationRecord>
StatusWindows : List<FlatFileStatusWindowRecord>
ShowCastWindows : List<FlatFileShowCastWindowRecord>
ContractWindows : List<FlatFileContractWindowRecord>
RelationshipWindows : List<FlatFileRelationshipWindowRecord>
TourParticipation : List<FlatFileTourParticipationRecord>
AwardResults : List<FlatFileAwardResultProjectionRecord>
ElectionResults : List<FlatFileElectionResultProjectionRecord>
PushWindows : List<FlatFilePushWindowRecord>
```

`FlatFileEventRecord` is stable across the release commits:

```text
EventId : Int64
GameDateKey : Int32
GameDateTime : string
IdolId : Int32
EntityKind : string
EntityId : string
EventType : string
SourcePatch : string
NamespaceIdentifier : string
PayloadJson : string
```

`FlatFileCustomDataRecord` is likewise stable:

```text
NamespaceIdentifier : string
DataKey : string
ValueJson : string
UpdatedUtc : string
```

The complete projection record shapes in 1.0.0, and unchanged through the unversioned 1.1/1.2/early-1.3 fallback envelope, are:

```text
FlatFileSingleParticipationRecord
  SingleId : Int32
  IdolId : Int32
  RowIndex : Int32
  PositionIndex : Int32
  IsCenterFlag : Int32
  ReleaseDate : string

FlatFileStatusWindowRecord
  IdolId : Int32
  StatusType : string
  StartDate : string
  EndDate : string

FlatFileShowCastWindowRecord
  ShowId : string
  IdolId : Int32
  StartDate : string
  EndDate : string
  EndReason : string
  PayloadJson : string

FlatFileContractWindowRecord
  ContractKey : string
  IdolId : Int32
  StartDate : string
  EndDate : string
  EndReason : string
  PayloadJson : string

FlatFileRelationshipWindowRecord
  RelationshipKey : string
  IdolId : Int32
  RelationshipType : string
  StartDate : string
  EndDate : string
  EndReason : string
  PayloadJson : string

FlatFileTourParticipationRecord
  TourId : string
  IdolId : Int32
  LifecycleAction : string
  EventDate : string
  PayloadJson : string

FlatFileAwardResultProjectionRecord
  AwardKey : string
  IdolId : Int32
  EventDate : string
  PayloadJson : string

FlatFileElectionResultProjectionRecord
  ElectionId : string
  IdolId : Int32
  EventDate : string
  PayloadJson : string

FlatFilePushWindowRecord
  SlotKey : string
  IdolId : Int32
  StartDate : string
  EndDate : string
  LastDaysInSlot : Int32
  EndReason : string
  PayloadJson : string
```

The projection record layouts are retained in Data Migration Tool only for recognition/count auditing. Cosmo's later `LegacyFlatFileImporter` explicitly says they are redundant projections and intentionally omits them when importing the authoritative event/custom state.

## Late-1.3 fallback JSON

By `181c501`, `FlatFileState` adds:

```text
FormatVersion
IntegritySha256
CheckpointFingerprint
CheckpointEventWatermark
CheckpointSnapshotJson
CheckpointCreatedUtc
Checkpoints[]
```

Current late-1.3 fallback format is `2`; the code also understands format `1` and the older unversioned shape. Integrity is SHA-256 over Unity's compact `JsonUtility.ToJson` representation with `IntegritySha256` temporarily set to an empty string. Data Migration Tool verifies this without mutating the source.

The retained checkpoint fingerprint is built from the raw vanilla save file as:

```text
v1:<raw-byte-length>:<lowercase-sha256(raw-save-bytes)>
```

Data Migration Tool uses an exact matching embedded `SnapshotJson` when one exists. If a checkpoint-capable source has no exact match, normal safe migration stops.

## 1.x SQLite layout

All release commits use the same core `schema_version = 2`. Important tables are:

```text
event_stream(
  event_id INTEGER PRIMARY KEY AUTOINCREMENT,
  save_key TEXT NOT NULL,
  game_date_key INTEGER NOT NULL,
  game_datetime TEXT NOT NULL,
  idol_id INTEGER,
  entity_kind TEXT NOT NULL,
  entity_id TEXT,
  event_type TEXT NOT NULL,
  source_patch TEXT NOT NULL,
  namespace_id TEXT NOT NULL DEFAULT '',
  payload_json TEXT NOT NULL
)

custom_data(
  save_key TEXT NOT NULL,
  namespace_id TEXT NOT NULL,
  data_key TEXT NOT NULL,
  value_json TEXT NOT NULL,
  updated_utc TEXT NOT NULL,
  PRIMARY KEY(save_key, namespace_id, data_key)
)
```

The remaining 1.x projection tables use these stable columns:

```text
single_participation(save_key, single_id, idol_id, row_index, position_index, is_center, release_date)
status_window(window_id, save_key, idol_id, status_type, start_date, end_date)
show_cast_window(window_id, save_key, show_id, idol_id, start_date, end_date, end_reason)
contract_window(window_id, save_key, contract_key, idol_id, start_date, end_date, end_reason)
relationship_window(window_id, save_key, relationship_key, idol_id, relationship_type, start_date, end_date, end_reason)
tour_participation(row_id, save_key, tour_id, idol_id, lifecycle_action, event_date)
award_result_projection(row_id, save_key, award_key, idol_id, event_date)
election_result_projection(row_id, save_key, election_id, idol_id, event_date)
push_window(window_id, save_key, slot_key, idol_id, start_date, end_date, last_days_in_slot, end_reason)
```

Late 1.x SQLite also creates typed `evt_*` payload tables dynamically from built-in event payload schemas. Those tables are derivative of `event_stream.payload_json` and are deliberately not imported as a second source of truth.

Late 1.3 adds retained exact checkpoint generations:

```text
storage_save_generation(
  generation_id INTEGER PRIMARY KEY AUTOINCREMENT,
  save_key,
  vanilla_save_fingerprint,
  event_watermark,
  mutable_table_count,
  checkpoint_created_utc
)

storage_save_generation_table(
  generation_id,
  table_name,
  snapshot_table_name,
  PRIMARY KEY(generation_id, table_name)
)
```

There is also an older single-checkpoint seam (`storage_save_checkpoint` / `storage_save_checkpoint_table`) that the late-1.3 engine migrates forward. Data Migration Tool recognizes both. The SQLite checkpoint does **not** snapshot `event_stream`; it records an `event_watermark`, while save-scoped mutable tables such as `custom_data` are represented by manifest snapshot tables. Data Migration Tool therefore reads events only through the matching watermark and reads custom state from the matching snapshot table.

## Current physical sidecar path

Current `CorePaths` no longer requires a `saves` layer. It mirrors the exact relative vanilla save path from:

```text
<persistent root>\data\<relative save path>
```

to:

```text
<persistent root>\IMDataCore\<relative save path>
```

For example:

```text
...\Idol Manager\data\manual_saves\ABC\save.json
→ ...\Idol Manager\IMDataCore\manual_saves\ABC\save.json
```

Data Migration Tool reproduces the current path acceptance rules, including direct `auto_save.json`/`manual_save.json`, manual-saves folders, and story-mode/chapter shapes, and rejects `global_data.json`.

## Current v6 checkpoint fingerprint

Current `VanillaSavedDataFingerprint` is:

```text
sha256:<SHA-256(UnityEngine.JsonUtility.ToJson(savedData, false) UTF-8 bytes)>
```

Vanilla saves are Unity's pretty spelling of that same ordered JSON graph. Data Migration Tool removes only insignificant JSON whitespace outside strings from the selected raw save, preserving property order, string escapes, and numeric lexemes, then hashes the resulting UTF-8 bytes. This avoids a parse/re-serialize round trip that could change number formatting.

Checkpoint identity additionally stores:

```text
RelativeSavePath
LastSave = staticVars__PlayerData.LastSave
PlaytimeSeconds = staticVars__PlayerData.Playtime_Seconds
GameDateTime = staticVars__dateTime
ContentFingerprint
Sequence
```

## V6 fields intentionally left for current IMDC to populate

Data Migration Tool does not invent data that 1.x could not know. The generated checkpoint starts with:

```text
IdentityBindingsVersion = 1
IdentityBindingsComplete = false
IdentityBindings = []
IdentityCandidates = []
AgencyRoomIdentities = []
EnabledMods = []
```

Namespace owner bindings are emitted as durable `legacy_unbound` records. `CoverageModelVersion=1`, with empty capability/transition collections, and historical baseline assertions start empty. These are valid v6 shapes and preserve the distinction between “unknown historically” and “known empty.”

## Provenance bridge

The current v6 validator's `legacy_migration` record accepts only source format name `IMDataCore.LightweightSidecar` with source version 1–5. Pre-2.0 database files are not part of that family. Data Migration Tool therefore:

1. treats the extracted event/custom state as a deterministic synthetic lightweight-v1 logical source for the parser-visible provenance seam;
2. uses a valid `legacy_migration` v6 provenance record with sidecar source version `1`;
3. preserves the true pre-2 backend, detected/selected 1.x release, legacy checkpoint status, source hash, projection counts, and warnings in optional forward extension `cosmo.imdatacore.data-migration-tool.pre2-migration`;
4. marks the extension `RequiredForRead=false`, matching the current v6 opaque-extension contract.

This is intentionally explicit in the file rather than silently claiming that a pre-2 database was native v6.
