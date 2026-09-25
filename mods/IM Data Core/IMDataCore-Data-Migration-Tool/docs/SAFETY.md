# Safety model

Data Migration Tool is designed to fail closed around the two files that matter most: the raw vanilla save and an already-existing current IMDataCore sidecar.

- The vanilla save is **read-only**. The program never writes, renames, deletes, or reparents anything under the selected `data` tree.
- SQLite sources are copied, along with adjacent `-wal`/`-shm` files, into a temporary directory before `winsqlite3` opens them.
- The default destination is the sibling current `IMDataCore` tree resolved from the selected vanilla save's actual path, not from a hard-coded username.
- Existing current sidecars are not overwritten unless the user explicitly opts in.
- Overwrite mode first moves the live sidecar, live journal, live `.imdc.bak`, and backup journal to timestamped archive names. It does not silently delete them.
- New output is written to a same-directory temporary file, flushed to disk, then atomically renamed into place.
- A stale journal is never allowed to remain beside the newly generated base.
- The GUI and CLI block live-tree migration when Idol Manager can be detected as running.
- Late-1.3 exact-checkpoint sources must match the selected vanilla file unless the advanced override is intentionally enabled.

The tool is still an offline compatibility utility. Test on copies before trusting a valued campaign, especially while the target v6/v3 code is still being actively developed.

## Recycle-Bin cleanup

Data Migration Tool cleanup actions use the Windows Recycle Bin rather than permanent deletion. They are opt-in and separately confirmed for orphan cleanup. A source is eligible for automatic post-migration cleanup only after its written v6 output passes post-write static validation and exact vanilla checkpoint/path verification.

"Orphaned" is intentionally narrow: only a bulk-plan entry with **No vanilla match** and no candidate score is eligible. Data Migration Tool does not classify weak, ambiguous, validation-failed, or superseded sources as disposable orphans.
