# IM Data Core v5 sidecar schema

This document describes the private sidecar representation written and accepted by IMDC 3.4.7. Consumer mods should use `IMDataCoreApi` instead of depending on these field names.

Current persistence versions:

- sidecar `FormatName`: `IMDataCore.LightweightSidecar`
- sidecar `FormatVersion`: `5`
- journal `FormatName`: `IMDataCore.LightweightJournal`
- journal `FormatVersion`: `2`

This development build intentionally has no runtime read/migration compatibility for older sidecar formats.

## Root

| Field | Type | Meaning |
| --- | --- | --- |
| `FormatName` | string | Must be `IMDataCore.LightweightSidecar`. |
| `FormatVersion` | integer | Must be `5`. |
| `RelativeSavePath` | string | Supported vanilla save path relative to the `data` root. |
| `LastIssuedSequence` | integer | Highest sequence issued for this branch lineage. |
| `Checkpoints` | array | Exact vanilla-save identities mapped to IMDC sequence watermarks. |
| `Events` | array | Immutable built-in and consumer timeline events. |
| `CustomMutations` | array | Ordered SET/REMOVE history for namespaced custom state. |

The document owns `RelativeSavePath`; checkpoint rows do not repeat it on disk.

## Checkpoint

Required fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `LastSave` | string | Vanilla `PlayerData.LastSave`, serialized at vanilla second resolution. |
| `PlaytimeSeconds` | integer | Vanilla `PlayerData.Playtime_Seconds`. |
| `GameDateTime` | string | Vanilla game date in `yyyy-MM-dd HH:mm:ss` form. |
| `ContentFingerprint` | string | `sha256:` plus 64 lowercase hex characters over compact vanilla `SavedData` JSON. |
| `Sequence` | integer | IMDC sequence watermark for this vanilla save boundary; may be `0` for a newly adopted pre-IMDC save. |
| `EnabledMods` | array | Enabled Idol Manager mod inventory captured at the save boundary. |
| `AgencyRoomIdentities` | array | Required durable agency-room generation snapshot in vanilla serialized floor/room order; may be empty when the save has no rooms. |

A mod snapshot contains required string fields `ModName`, `Title`, `Author`, `Version` and required string-array `DllNames`. JSON-only mods are represented with an empty DLL list.

### Exact checkpoint identity

Checkpoint identity is:

```text
normalized RelativeSavePath
+ LastSave
+ PlaytimeSeconds
+ GameDateTime
+ ContentFingerprint
```

`Sequence` and `EnabledMods` are checkpoint contents, not identity fields.

### Durable agency-room identity snapshot

Every accepted v5 checkpoint contains `AgencyRoomIdentities`. The field is required but deliberately **not** part of exact vanilla-save identity. It is an array in vanilla's serialized agency floor/room order. Each record contains:

| Field | Type | Meaning |
|---|---|---|
| `EntityId` | string | IMDC-owned durable room-generation identifier (`g:<guid>`). |
| `FloorIndex` | int | Zero-based index in `SavedData.agency__Floors`. |
| `RoomIndex` | int | Zero-based room index within that saved floor. |
| `RoomTypeRaw` | int | Raw vanilla `agency._type` value used to validate reassociation. |
| `TheaterId` | int | Saved vanilla `TheaterID`; used as an additional layout check for theater/cafe rooms. |

The field may be an empty array when the vanilla save has no agency rooms. A format-5 checkpoint that omits `AgencyRoomIdentities` is invalid and is not treated as an earlier compatible v5 schema. If a present snapshot is structurally valid but does not match the loaded vanilla room layout, IMDC fails safe by assigning fresh forward-safe generations instead of binding history to the wrong rooms.

`ContentFingerprint` is computed from:

```csharp
UnityEngine.JsonUtility.ToJson(savedData, false)
```

and SHA-256 hashed as UTF-8. The stored value is lowercase hexadecimal with a `sha256:` prefix. This makes two distinct vanilla save graphs distinguishable even when vanilla timestamp/playtime fields collide within the same second.

Checkpoint `GameDateTime` is parsed using vanilla `ExtensionMethods.ToDateTime`; it is intentionally not parsed with IMDC's event round-trip timestamp parser.

### Adopted-save anchor

When IMDC loads a valid physical vanilla save that has no sidecar, it may create a sequence-0 checkpoint in memory for the exact loaded save. Loading alone does not persist the sidecar. A later `TryFlushNow` or normal save can persist that checkpoint so the first IMDC generation remains exactly matchable.

## Event

Required stored fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `Sequence` | integer | Positive source-record sequence. Public `EventId` is derived from this. |
| `GameDateTime` | string | IMDC round-trip date/time string. |
| `IdolId` | integer | Idol identity, or the event's sentinel/global value where applicable. |
| `EntityKind` | string | Event entity kind. |
| `EntityId` | string | Durable entity identity. For agency rooms, theaters, and cafes this is an IMDC-owned room-generation ID rather than the runtime/recyclable vanilla ID. |
| `EventType` | string | Event type token. |
| `SourcePatch` | string | Capture provenance. |
| `NamespaceIdentifier` | string | Empty for built-in events; consumer namespace for custom events. |
| `Payload` | JSON value | Structural event payload. |

Optional field:

| Field | Type | Meaning |
| --- | --- | --- |
| `IdempotencyKey` | string | Namespace-scoped occurrence identity used by `TryAppendCustomEventOnce`. |

`GameDateKey` and duplicated public `EventId` are not stored. They are reconstructed from source fields.

For nonempty `IdempotencyKey`, the active persisted pair `NamespaceIdentifier + IdempotencyKey` must be unique.

## Custom mutation

Required for all operations:

- `Sequence`
- `GameDateTime`
- `NamespaceIdentifier`
- `DataKey`
- `Operation`

For `SET`, structural JSON field `Value` is required. For `REMOVE`, `Value` is omitted.

Event and custom-mutation `GameDateTime` values use IMDC's round-trip timestamp representation, unlike checkpoint dates.

## Core validation invariants

A v5 document is rejected if any applicable invariant fails, including:

- format name/version mismatch;
- physical sidecar scope and declared `RelativeSavePath` mismatch;
- null required source-record collections;
- nonpositive, duplicate, or out-of-order event/custom-mutation sequences;
- checkpoint sequence below zero or above `LastIssuedSequence`;
- malformed or missing checkpoint `ContentFingerprint`;
- duplicate exact checkpoint identities;
- missing `AgencyRoomIdentities`, or malformed records including empty/duplicate generation IDs or invalid indexes/types;
- `LastIssuedSequence` below a stored source-record sequence;
- malformed custom operations, tokens, idempotency keys, payloads, or custom values;
- custom-data quota/token violations enforced by the storage/API layers.

## Transactional journal

The v5 compact sidecar may be extended by `<sidecar>.imdc.journal`. Journal format 2 begins with a header containing the exact compact base SHA-256, followed by bounded NDJSON transactions. Each transaction uses `BEGIN`, source-record rows, and `COMMIT`.

A torn transaction without a valid commit is ignored. Base/target counts make a complete retry idempotent. A journal whose declared base hash does not match the compact sidecar is not replayed onto that sidecar.

## Backup generation

A healthy compact-base replacement may retain:

```text
<sidecar>.imdc.bak
<sidecar>.imdc.bak.imdc.journal
```

A recovered backup must still contain an exact v5 checkpoint for the loaded vanilla save. Recovery never weakens checkpoint identity.

## Deleted-save archives

Deleted vanilla saves do not cause IMDC sidecar deletion. The mirrored IMDC directory is renamed to `<name>OLD`, then `<name>OLD2`, `<name>OLD3`, and so on for collisions. The archived directory is historical material and is not an active sidecar scope.
