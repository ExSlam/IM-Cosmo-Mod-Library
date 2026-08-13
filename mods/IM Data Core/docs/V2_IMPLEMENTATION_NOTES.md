# IM Data Core 2.0 implementation notes

This file is the durable implementation contract and progress log for the
lightweight persistence refactor. It records decisions that must remain stable
while the old storage implementation is removed.

## Architectural boundary

```text
Vanilla save = canonical game state
IMDC         = history + transient state + supplementation
```

IMDC must never reconstruct or overwrite vanilla-owned current state. A load
first lets vanilla restore Idol Manager, then IMDC aligns only its own history,
custom state, and supplemental views with the loaded checkpoint.

## Non-negotiable safety rules

- Vanilla save JSON is never written, renamed, moved, replaced, or deleted by
  IMDC.
- `data/global_data.json` is explicitly rejected and is never a save scope.
- Every IMDC mutation target is canonically contained beneath
  `Application.persistentDataPath/IMDataCore`.
- The sidecar root is a sibling of vanilla `data`; there is no required
  `IMDataCore/saves` layer.
- Normal runtime uses the already-deserialized `SaveManager.SavedData` and does
  not read vanilla save files.
- A persistence failure is logged but never blocks or repairs vanilla saving or
  loading.

## Save scope and sidecar mapping

The exact path below the canonical vanilla `data` root is preserved below the
canonical IMDC root:

```text
data/auto_save.json
  -> IMDataCore/auto_save.json

data/manual_saves/<opaque-id>/save.json
  -> IMDataCore/manual_saves/<opaque-id>/save.json

data/story_mode/<playthrough>/chapter_3/save.json
  -> IMDataCore/story_mode/<playthrough>/chapter_3/save.json
```

Opaque path segments are copied verbatim. Player-facing save or group names are
not substitutes for resolved physical path segments.

The existing public logical `save_key` is distinct from this physical path and
must remain compatible with repository consumers.

## Verified vanilla lifecycle facts

- `SaveManager.GetSaveFileName(bool)` produces direct freeplay
  `auto_save`/`manual_save` names and Story
  `story_mode/<SaveFolderName>/auto_save|manual_save` names.
- The legacy freeplay popup writes `manual_saves/<integer>/save`; the shared
  load/save popup also writes opaque `Hash.Generate(8)` directories. Story
  manual saves are below
  `story_mode/<SaveFolderName>/manual_saves/<opaque>/save.json`.
- `Hash.Generate(8)` returns the final eight characters of a GUID string. IMDC
  treats the segment as opaque rather than depending on that implementation.
- chapter saves use rooted
  `data/story_mode/<SaveFolderName>/chapter_0..chapter_6/save` arguments even
  though vanilla passes `fullPath = false`.
- `GetLatestAutosavePath` compares freeplay and per-playthrough Story autosaves
  by parsed vanilla `LastSave` and returns the selected absolute `.json` path.
- `staticVars.SaveFunction`, invoked through `SaveEvent` before all five
  `SavedData` writes, calls `SetLastSaveNow(true)` and then copies both
  `PlayerData` and `staticVars.dateTime` into `SavedData`.
- `LastSave` is a local `DateTime.Now` formatted as
  `yyyy-MM-dd HH:mm:ss`. `Playtime_Seconds` is a `long` increased by the rounded
  real-time interval since the preceding `LastSave` when saving.
- both `SaveManager.LoadData` overloads assign the deserialized object to
  `SaveManager.Data` before `LoadEvent`. IMDC copies the stamp immediately after
  that assignment because `staticVars.LoadFunction` later replaces live
  `PlayerData` and calls `SetLastSaveNow(false)`, changing its `LastSave`.

## Checkpoint identity and sequence

The vanilla checkpoint identity is:

```text
resolved relative save path
+ SavedData.staticVars__PlayerData.LastSave
+ SavedData.staticVars__PlayerData.Playtime_Seconds
+ SavedData.staticVars__dateTime
```

The path selects the sidecar, so it may be implicit inside checkpoint rows.
Each checkpoint stores only the vanilla metadata and an IMDC sequence
watermark:

```text
(LastSave, Playtime_Seconds, game date) -> sequence
```

The sequence is not vanilla-save identity. On load, the exact vanilla stamp is
looked up first; its mapped sequence determines how much IMDC history becomes
active. The numerically greatest sequence is never selected merely because it
is greatest.

The rollback scenario discussed with the repository owner is therefore:

```text
manual checkpoint M -> sequence 650
later autosave A     -> sequence 900
load M               -> activate IMDC through 650
```

Reaching the same in-game date on the continued branch does not confuse the
new autosave with the old one: the resolved path, real-world vanilla save time,
and/or accumulated playtime differ. Stamps whose complete tuples are identical
represent the same vanilla checkpoint identity.

Every rollback-relevant IMDC mutation shares one monotonically increasing
sequence space, including built-in/custom events and custom JSON SET/REMOVE
mutations.

## Lightweight sidecar format

Release version is `2.0.0`.

The sidecar has an explicit format name and its own format version so it cannot be confused with earlier persistence formats that used their own numeric versioning.

Format identity:

```text
formatName    = "IMDataCore.LightweightSidecar"
formatVersion = 2
```

Persisted source data is limited to:

- the format identity and exact relative vanilla path;
- the next/last-issued mutation sequence;
- lightweight checkpoint-to-sequence mappings;
- historical custom JSON SET/REMOVE mutations;
- IMDC-owned historical, transient, or supplemental event records.

Runtime dictionaries and query indexes are rebuilt from those records after a load. Whole vanilla objects, `SaveManager.SavedData`, current vanilla collections, embedded snapshots, hashes, and derived indexes are not persisted.

Shared built-in timeline records use the same rule: one occurrence is stored
once with explicit primitive participant identity. Derived per-idol references
and reconstructed idol-specific result fields exist only at query time.

## Runtime and persistence flow

Ordinary capture:

```text
allocate sequence -> append small in-memory record -> update index -> return
```

No event causes a filesystem write, whole-sidecar serialization, periodic flush, queue-threshold flush, vanilla hash, or per-frame persistence pump.

Vanilla save boundary:

1. Use the actual `SavedData` and resolved vanilla target supplied at the
   vanilla `DataSaver.saveData<SavedData>` callsite.
2. Build the vanilla checkpoint stamp without modifying the object.
3. Map that stamp to the current IMDC sequence.
4. If saving to another target, copy the current logical in-memory branch to
   that target; never merge stale target history.
5. Atomically write one IMDC-owned sidecar beneath the IMDC root.
6. Let vanilla saving succeed or fail independently.

Vanilla load:

1. Capture the actual selected vanilla path.
2. After `SaveManager.Data` is assigned, but before `LoadEvent`, open the
   mirrored sidecar and build the stamp from the loaded `SavedData`.
3. If an exact checkpoint exists, activate records belonging to its sequence
   watermark.
4. Otherwise, use the loaded game date as a non-destructive fallback cutoff.
5. Rebuild custom state and query indexes in memory before other `LoadEvent`
   subscribers run.
6. Keep newer sidecar history unchanged on disk until a later vanilla save or
   explicit `TryFlushNow` commits the active branch.

`TryFlushNow` writes the current IMDC branch only. It does not save, read, poll,
or modify vanilla and fails cleanly before a valid physical scope exists.

## Custom data

Custom JSON is an ordered mutation log:

```text
sequence, game date, namespace, key, SET/REMOVE, SET value
```

REMOVE is a tombstone. The current dictionary is materialized in memory.
Existing token validation, namespace ownership, sessions, and quotas remain in
force.

## Historical capture and duplication rule

Existing useful capture remains. Stable public event and money-ledger models
remain compatible.

Persist a captured field only when it is historical or supplemental and cannot
be recovered with the same meaning from the currently loaded vanilla state.
Stable vanilla IDs are preferred over copied objects. Capture-time values remain
when replacing them with mutable current vanilla values would destroy historical
semantics.

Derived projections are rebuilt from the canonical IMDC event log where
practical rather than serialized as duplicate state.

Completed duplication audit:

- the v2 document contains no `SavedData`, vanilla entity object, vanilla
  collection, derived projection, or checkpoint snapshot;
- old single/status/window/result projection DTOs and live allocations were
  removed;
- four byte-identical legacy alias events are no longer emitted when a canonical
  event already exists;
- payload IDs already carried by `EntityId`, relationship keys equal to
  `EntityId`, and lifecycle/status echoes already encoded by `EventType` were
  removed from new serialization;
- unused save-key, migration-source-path, and migration-marker fields were
  removed from the runtime DTOs.

The event record retains sequence and stable public event ID, historical date,
idol/entity references, event/source/namespace semantics, and supplemental
payload JSON. Custom mutations retain exactly the fields required for ordered,
rollback-safe namespaced `SET`/`REMOVE` replay.

Some release/episode payload values also exist in vanilla's current saved
single/show structures. They remain intentionally for 2.0 where repository
consumers expose their capture-time semantics and where canceled/destroyed
vanilla entities may no longer be resolvable. Removing more of those values
requires query-time enrichment plus a coordinated consumer fallback change;
silently substituting mutable current values would break historical meaning.

## Legacy JSON compatibility policy

Earlier IM Data Core releases used several persistence formats. Version 2 only retains automatic migration support for the late-1.3 fallback JSON format that can be matched safely to the loaded vanilla checkpoint.

Legacy sources are treated as immutable migration inputs:

- Never move, edit, truncate, or delete a legacy fallback file.
- Import only fields whose semantics can be verified.
- Omit legacy projections that merely duplicate vanilla current state or can be rebuilt from imported IMDC events.
- A latest-only custom value with no history becomes one migration-baseline `SET` mutation; earlier timestamps are not invented.
- Unsupported or ambiguous historical formats are ignored rather than requiring old runtime persistence code.

Verified compatibility matrix:

| Legacy source | Automatic action in 2.0 |
| --- | --- |
| Early/unversioned fallback JSON | Ignored because latest-only custom/projection state cannot be mapped safely to an exact rollback checkpoint. |
| Late 1.3 fallback JSON `FormatVersion = 2` | Imported only after integrity validation and an exact in-memory vanilla fingerprint match. |
| Late 1.3 fallback `.bak` / `.tmp` recovery candidates | Evaluated read-only under the same strict rules; conflicting matches are rejected. |

The late-1.3 importer exists only for a one-time compatibility decision when the current sidecar is absent. It never reads the vanilla file and never writes or promotes a legacy source. Events retain their historical payloads and public IDs; current custom values become baseline `SET` mutations at the loaded game date. Old projection arrays are deliberately omitted.

The late-1.3 importer exists only for a one-time compatibility decision when the
new sidecar is absent. It never reads the vanilla file, never opens SQLite, and
never writes or promotes a legacy source. Events retain their historical
payloads and public IDs; current custom values become baseline `SET` mutations
at the loaded game date. Old projection tables/arrays are deliberately omitted.

this legacy json import code must be removed. the old version of imdatacore did not save in both json and sqlite. it only saved in sqlite that exists on every modern windows 10/11 machine and so almost none of the players have migratable data. I'll consider making a tool to migrate old data from sql dbs some other time.
I would remove the pre-2.0 flat-JSON importer completely. Keeping it no longer buys meaningful player compatibility, while it does add a surprisingly large amount of migration-only code, path probing, hashing, DTOs, and load-time branching.

## Removed architecture

The version-2 implementation no longer contains:

- secondary persistence backend selection or provider probing;
- vanilla/save-sidecar generation snapshots;
- save/load staging directories, publish journals, or expected-byte observation;
- periodic or queue-threshold persistence;
- `PopupManager.Update` persistence processing;
- serialized derived projections that can be rebuilt from event history.

Runtime persistence is the lightweight JSON sidecar only.

## Progress checklist

- [x] Architectural boundary confirmed with repository owner.
- [x] Checkpoint identity versus sequence responsibility confirmed.
- [x] Release bump to 2.0.0 confirmed.
- [x] Verify and document complete vanilla save/load behavior.
- [x] Verify 1.2.0 and 1.3.0 legacy formats.
- [x] Implement contained mirrored-path resolver and tests.
- [x] Implement lightweight sidecar and atomic IMDC-only writer.
- [x] Implement event/custom mutation sequence and materialized indexes.
- [x] Implement exact checkpoint and game-date fallback activation.
- [x] Rewire vanilla save/load/new-game hooks.
- [x] Implement safe legacy import where proven practical.
- [x] Remove obsolete runtime architecture.
- [x] Update public documentation and event catalog as needed.
- [x] Build and run path, rollback, safety, and duplication audits.

## Completed validation

- `dotnet build "mods/IM Data Core/IM Data Core.csproj" --configuration Release --no-restore`
  completed with zero warnings and zero errors.
- `scripts/Test-CorePaths.ps1 -SkipBuild` passed exact Freeplay, Story,
  chapter, direct/manual, and opaque-directory mappings plus traversal,
  `global_data.json`, containment, and mutation-sentinel checks.
- `scripts/Test-LightweightPersistence.ps1 -SkipBuild` passed exact rollback,
  game-date fallback, divergent-branch commit, custom `SET`/`REMOVE`, compact
  schema, duplicate/watermark rejection, atomic-failure preservation, and
  valid-only money-ledger truncation tests.
- Before rebuilding the final Release output, a reflection comparison between
  the pre-refactor 1.3.0 assembly and the new 2.0.0 assembly found the same 137
  public surface entries with zero removals and zero additions.
- The dependent `Monthly Ledger` project built with zero warnings and zero
  errors against the refactored project.
- Final storage, API/runtime, duplication, filesystem-mutation, and obsolete-
  architecture audits found no remaining actionable defect.
