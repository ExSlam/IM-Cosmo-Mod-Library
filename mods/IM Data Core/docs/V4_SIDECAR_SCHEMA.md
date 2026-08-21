# IM Data Core v4 sidecar schema (historical)

> Historical reference only. IMDC 3.4.6 writes and accepts sidecar format 5 only. See [`V5_SIDECAR_SCHEMA.md`](V5_SIDECAR_SCHEMA.md) for the current schema. The runtime does not migrate or activate v4 sidecars in this development build.

This document records the private sidecar representation formerly written by IMDC 3.4.5. Consumer mods should use `IMDataCoreApi`, not depend on historical field names.

## Root

| Field | Type | Meaning |
| --- | --- | --- |
| `FormatName` | string | `IMDataCore.LightweightSidecar`. |
| `FormatVersion` | integer | `4`. |
| `RelativeSavePath` | string | Supported vanilla save path relative to `data`. |
| `LastIssuedSequence` | integer | Highest sequence issued for this branch lineage. |
| `Checkpoints` | array | Exact vanilla-save stamps, sequence watermarks, and enabled-mod inventories. |
| `Events` | array | Immutable built-in and consumer events. |
| `CustomMutations` | array | Ordered SET/REMOVE history for namespaced custom state. |

## Checkpoint

Required fields: `LastSave`, `PlaytimeSeconds`, `GameDateTime`, `Sequence`, `EnabledMods`. The checkpoint inherits `RelativeSavePath` from its enclosing document.

`EnabledMods` contains one row for every mod whose vanilla `Mods._mod.IsEnabled()` was true at the save boundary. This deliberately uses Idol Manager's mod registry rather than Harmony ownership or IMDC participation, so JSON-only mods are included.

Each enabled-mod row contains:

| Field | Type | Meaning |
| --- | --- | --- |
| `ModName` | string | Stable vanilla mod identifier, falling back to title only when no mod name exists. |
| `Title` | string | Player-facing mod title. |
| `Author` | string | Declared author. |
| `Version` | string | Declared mod version. |
| `DllNames` | string[] | Unique DLL filenames found below the mod folder, sorted case-insensitively. Empty for JSON-only mods. |

On exact-checkpoint load IMDC compares these rows against the current registry. Missing mods, installed-but-disabled mods, and enabled mods whose author/version/DLL filename set differs are logged as checkpoint compatibility warnings. The comparison is diagnostic and does not prevent vanilla loading.

A format-3 checkpoint has no `EnabledMods` field and reads as an empty inventory.

## Events and custom mutations

Event/custom-mutation storage remains structurally compatible with format 3: payloads and SET values are real JSON values, `EventId` is derived from `Sequence`, and `GameDateKey` is derived from `GameDateTime`. Transactional journal format remains 2.

## Validation invariants

In addition to the existing sequence/date/token invariants, format 4 requires `EnabledMods` to be an array; every row must contain string `ModName`, `Title`, `Author`, `Version`, and a string-array `DllNames`.
