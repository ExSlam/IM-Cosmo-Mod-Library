# IM Data Core 3.4 validation notes

This revision was checked against the supplied Cosmo Mod Library source and the supplied decompiled Idol Manager source.

## Persistence changes checked statically

- The runtime accepts only sidecar format **3** and transactional journal format **2**. Older formats are intentionally rejected and are not migrated.
- **New Save** writes only checkpoints for the new physical target but no longer prunes checkpoints for other active save paths. A later **Overwrite Save** to an older path therefore preserves the per-path checkpoint-prefix invariant used by incremental journaling.
- Physical sidecar I/O gates are process-wide and keyed by full path. Loading/replacing an engine holds the same path gate through initialization and engine installation so a queued compactor from the old engine cannot replace the new engine's base or delete its journal.
- The standalone defensive `SavedData` clone is fail-open. Clone failure is logged and vanilla receives its original `SavedData` object instead of IMDC aborting the save call.
- Routine compaction is byte/ratio driven. The transaction-count ceiling scales from **2,048** to **32,768** transactions instead of forcing a full rewrite after 256 small saves.
- Background compaction shallow-copies persisted prefixes from the already-loaded immutable records instead of deserializing the complete base+journal into another history object graph. Before replacement it verifies the physical base SHA-256 and journal length against the committed generation.
- A base document is fully validated once. Replayed journal rows are validated as a suffix using the base validation state, avoiding a second complete-history validation pass.
- Event and custom-mutation lists skip sorting when a linear monotonicity scan shows they are already in current writer order.
- Save-boundary single chart reconciliation tracks unresolved released singles rather than rescanning the complete historical singles collection on every save.
- Post-mod show reconciliation reuses scratch collections rather than allocating a new `HashSet` and stale-ID list at each save boundary.
- Backup recovery can pair `.imdc.bak` with the still-present current journal when its stored base hash matches, covering the compaction window before the journal is copied to `.imdc.bak.imdc.journal`. If that copy fails, the current journal is deliberately retained.

## Source/package checks

- `assets/info.json` and `IM Data Core.csproj` report **3.4.1**.
- Edited JSON metadata parses as strict JSON and the project file parses as XML.
- A string/comment-aware delimiter scan is run over the IM Data Core C# sources before packaging.
- Current source/docs use Idol Manager's **New Save** / **Overwrite Save** terminology and contain no references to the removed legacy journal-version constant.

## Compiler/runtime limitation

This execution environment does not contain `dotnet`, MSBuild, Roslyn `csc`, or Mono `mcs`. A real .NET Framework/Unity compile and in-game runtime test therefore cannot be performed here. The source changes are statically checked before packaging, but the pre-existing compiled `bin`/`obj` outputs are **not** builds of these 3.4 source edits.

## Recommended in-game regression matrix

- Overwrite A -> New Save B -> Overwrite A, both with and without newly captured events.
- Overwrite A -> New Save B -> New Save C -> Overwrite A.
- Trigger background compaction, then load the same physical save while the worker is active; the newly loaded engine must retain all later checkpoints/events.
- Kill the process at journal BEGIN/record/COMMIT boundaries and during compaction replacement; committed transactions must replay once, torn transactions must not become visible, and backup recovery must remain usable.
- Exercise a large history with hundreds of checkpoint-only saves and verify the transaction-count replay ceiling does not force premature compaction.
- Run without Save Write Ordering Fix and force a defensive clone failure; vanilla save execution must continue.
- Load a current v3 sidecar with a non-empty transactional journal and verify journal suffix validation, checkpoint activation, and subsequent Overwrite Save.
