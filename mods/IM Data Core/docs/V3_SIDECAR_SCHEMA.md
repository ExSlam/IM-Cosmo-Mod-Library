# IM Data Core v3 sidecar schema

This document describes the private persisted representation. Consumer mods should use `IMDataCoreApi`, not depend on these field names.

## Root

| Field | Type | Meaning |
| --- | --- | --- |
| `FormatName` | string | Must be `IMDataCore.LightweightSidecar`. |
| `FormatVersion` | integer | `3`. |
| `RelativeSavePath` | string | Supported vanilla save path relative to `data`. |
| `LastIssuedSequence` | integer | Highest sequence issued for this branch lineage. |
| `Checkpoints` | array | Vanilla-save stamp to sequence-watermark mappings. |
| `Events` | array | Immutable built-in and consumer events. |
| `CustomMutations` | array | Ordered SET/REMOVE history for namespaced custom state. |

## Checkpoint

Required fields: `LastSave`, `PlaytimeSeconds`, `GameDateTime`, `Sequence`.

The checkpoint inherits `RelativeSavePath` from its enclosing document.

## Event

Required fields: `Sequence`, `GameDateTime`, `IdolId`, `EntityKind`, `EntityId`, `EventType`, `SourcePatch`, `NamespaceIdentifier`, `Payload`.

`Payload` may be any valid JSON value. Built-in IMDC events normally use an object. Namespaced custom events retain consumer payload semantics.

`EventId` is not stored: public `EventId` equals `Sequence`.

`GameDateKey` is not stored: it is derived from `GameDateTime`.

## Custom mutation

Required for all operations: `Sequence`, `GameDateTime`, `NamespaceIdentifier`, `DataKey`, `Operation`.

For `SET`, `Value` is required and may be any valid JSON value.

For `REMOVE`, `Value` is omitted.

## Validation invariants

- format name/version must be supported;
- document relative path must match the physical sidecar scope;
- sequences must be positive and unique across events and custom mutations;
- checkpoint sequences must be valid watermarks;
- `LastIssuedSequence` must not be below any stored source-record sequence;
- event/custom `GameDateTime` must parse using the round-trip format;
- event payloads and SET values must be valid JSON;
- custom operations must be `SET` or `REMOVE`;
- token and quota rules are enforced by the storage/API layers.
