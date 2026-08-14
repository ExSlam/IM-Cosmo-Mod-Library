# IM Data Core v3 sidecar schema

This document describes the private persisted representation used by IMDC 3.2. Consumer mods should use `IMDataCoreApi`, not depend on these field names.

The release version is 3.4.0, while the compact sidecar `FormatVersion` remains `3`. This build accepts only the current v3 sidecar and transactional journal format 2. Older sidecar and journal formats are intentionally unsupported; no compatibility migration path is maintained.

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

The checkpoint inherits `RelativeSavePath` from its enclosing document. Activation requires an exact vanilla-save stamp match. Current IMDC does not activate an existing sidecar through a date-only fallback when no checkpoint matches.

## Event

Required fields: `Sequence`, `GameDateTime`, `IdolId`, `EntityKind`, `EntityId`, `EventType`, `SourcePatch`, `NamespaceIdentifier`, `Payload`.

Optional field:

| Field | Type | Meaning |
| --- | --- | --- |
| `IdempotencyKey` | string | Caller-supplied identity for `TryAppendCustomEventOnce`. Valid only on namespaced custom events. |

`Payload` may be any valid JSON value. Built-in IMDC events normally use an object. Namespaced custom events retain consumer payload semantics.

`EventId` is not stored: public `EventId` equals `Sequence`.

`GameDateKey` is not stored: it is derived from `GameDateTime`.

### Idempotency invariant

For events carrying `IdempotencyKey`, the pair:

```text
NamespaceIdentifier + IdempotencyKey
```

must be unique in the active persisted event set. Reusing the same pair through `TryAppendCustomEventOnce` is an idempotent success and does not append another row.

Because the lookup is rebuilt from the active branch, rewinding to an exact checkpoint before the event existed removes that occurrence from active history and allows it to be recorded again later.

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
- nonempty event `IdempotencyKey` values must satisfy token rules, belong to a nonempty namespace, and be unique by namespace/key pair;
- custom-data token and quota rules are enforced by the storage/API layers.
