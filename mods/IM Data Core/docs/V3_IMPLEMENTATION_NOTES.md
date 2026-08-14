# IM Data Core 3 implementation notes

## Goals

Version 3 keeps the memory-first, exact-save-scoped architecture introduced by the lightweight sidecar while removing storage shapes inherited from a row/text-column mindset.

The disk format is a document. It should therefore use JSON objects, arrays, scalars, and null directly rather than encoding nested JSON or ID collections into strings.

## Disk-format changes

`LightweightCoreStorageEngine.SidecarFormatVersion` is `3`.

V3 writes:

- event `Payload` as a real JSON value
- custom SET `Value` as a real JSON value
- built-in ID lists as JSON arrays where the runtime payload previously used a comma-delimited string
- built-in money details as nested `detail` JSON

V3 no longer writes:

- event `EventId` separately from `Sequence`
- event/custom `GameDateKey`
- checkpoint `RelativeSavePath`
- `PayloadJson` or `ValueJson` as quoted JSON strings
- an empty value member on REMOVE

The in-memory/public contract can still expose `EventId`, `GameDateKey`, `PayloadJson`, and string custom JSON because those are convenient API views rather than reasons to duplicate them on disk.

## V1/V2 normalization

The v3 codec accepts lightweight sidecar format versions 1, 2, and 3. Older sidecars are parsed into the v3 runtime record model. A successful later write emits v3 only.

This is a sidecar-format migration, not a migration from pre-2.0 database persistence.

## Runtime indexes

Derived state is rebuilt after load:

- event query indexes
- active mutation-sequence set
- materialized custom values
- per-namespace custom-data quota usage

None of those structures is serialized.

## Custom-data quota accounting

V3 maintains per-namespace key count and normalized-value character usage incrementally. SET validation therefore does not rescan the entire namespace.

The mutation layer also suppresses two no-op records:

- SET where normalized JSON equals the current value
- REMOVE where the key does not exist

## JSON validation

Public custom JSON and custom-event payload JSON are parsed and normalized before they enter durable history. Invalid JSON is rejected at the API boundary instead of being stored as opaque text that could fail a future save.

## Sidecar failure policy

Missing and unreadable are different states.

- Missing: initialize an ordinary empty writable physical branch.
- Corrupt/invalid/newer format: preserve the existing file, expose safe empty supplemental state, and block writes to that same path.

The controller does not convert an unreadable existing sidecar into writable empty state.

## Atomic persistence

Writes use a private validated temporary file followed by atomic promotion. Replacement retains one `.imdc.bak` previous generation.

## Save/load lifecycle

The concrete Idol Manager save callers remain the integration points. IMDC does not patch a constructed reference-type `DataSaver<T>` generic method. The save hook prepares one detached `SavedData` snapshot before vanilla's asynchronous worker receives it, keeping the vanilla stamp and IMDC checkpoint aligned.

During load, sidecar restoration occurs after vanilla assigns the deserialized `SavedData` and before `SaveManager.LoadEvent` subscribers run. Persistent public mutations during load reconstruction use the frozen newly-loaded game date.

## Public API decision

Version 3 intentionally does not replace the public string JSON interface with IMDC-private JSON AST types. Cosmo consumers already parse/produce JSON using their preferred library, and exposing the private codec would increase coupling.

The compatibility alias `IMDataCoreAPI` is also retained because removing it produces consumer churn with no storage benefit.
