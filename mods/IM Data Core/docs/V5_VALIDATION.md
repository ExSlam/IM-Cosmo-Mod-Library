# IM Data Core 3.4.6 validation notes

This revision was statically checked against the supplied Cosmo Mod Library source and the supplied decompiled Idol Manager source. No Unity/.NET compiler or game runtime was available in the analysis environment, so these notes intentionally distinguish static verification from runtime testing.

## Persistence changes checked statically

- The runtime writes and accepts sidecar format **5** only and keeps transactional journal format **2**.
- Every v5 checkpoint requires a valid `sha256:<64 lowercase hex>` `ContentFingerprint`.
- Exact checkpoint identity includes normalized relative path, vanilla `LastSave`, playtime seconds, vanilla game date/time, and the content fingerprint.
- Standalone save freezing registers the SHA-256 of the compact JSON already used to create the detached `SavedData`, avoiding a second full serialization on that path.
- The SHA-256 helper feeds UTF-8 in bounded chunks and keeps surrogate pairs together across chunk boundaries.
- Loading a physical vanilla save with no existing sidecar seeds an in-memory sequence-0 checkpoint before the engine is installed. A subsequent `TryFlushNow` therefore has an exact vanilla anchor.
- Checkpoint date watermarks parse `checkpoint.GameDateTime` with vanilla `ExtensionMethods.ToDateTime`; event/custom mutation watermarks retain round-trip timestamp parsing.
- New Save still serializes only checkpoints for its target physical path without discarding the active multi-path checkpoint ledger needed by later Overwrite Save operations.
- Physical sidecar I/O remains process-wide per canonical path. Loads, writes, and background compaction use a shared persistence-topology lease.
- Deleted-save archival uses an exclusive persistence-topology lease and advances per-path archive epochs at boundary completion so stale prepared snapshots cannot become current again.
- Archive naming is non-destructive and collision-safe: `nameOLD`, `nameOLD2`, `nameOLD3`, ... .
- If archival fails, the source directory is preserved and writes beneath that deleted scope are blocked for the process.
- Deleting the active save detaches its physical scope while retaining the logical in-memory branch.
- Standalone defensive `SavedData` cloning is layered: normal `FromJson`, then a Unity-serialized-field graph clone. The fallback clone is reserialized; when the original compact JSON exists, equivalence is required before the clone is trusted. `FromJsonOverwrite` is intentionally not used because Idol Manager's UnityEngine API does not expose it. The outer Harmony boundary remains fail-open only after all detachment strategies fail, so IMDC still cannot block vanilla saving.
- Backup/journal recovery still requires exact checkpoint activation after a document is recovered.
- New checkpoints may contain the additive `AgencyRoomIdentities` v5 field. When present, records require non-empty unique generation IDs and valid saved floor/room/type metadata; missing fields on early v5 checkpoints are accepted.
- Room-identity restoration validates the snapshot against the exact vanilla `SavedData` room layout before binding it to reconstructed rooms. A missing/incompatible snapshot falls forward to new generation IDs rather than binding old history to the wrong room.
- Historical `agency_room`, `theater`, and `cafe` `EntityId` values use the IMDC room generation; raw runtime/recyclable vanilla IDs remain payload data only.

## Vanilla targets checked

The supplied decompiled game has three relevant user-save directory deletion methods, all patched by `CoreSaveDeletionPatches.cs`:

1. `Popup_Save.Delete()` deletes `data/manual_saves/<id>` and swallows `Directory.Delete` errors. IMDC captures the path in Prefix and its Harmony Finalizer archives only if the directory is absent afterward.
2. `Popup_Load_Story.Delete_Save(save_info)` deletes `Save.GetDirectory()`. Its Finalizer checks actual directory absence, so a successful deletion is still archived if later vanilla UI cleanup throws, while a failed deletion is left alone. The original exception is returned unchanged.
3. `Popup_Load_Story.Delete_Playthrough(playthrough_info)` deletes `Playthrough.Dir`. The same Finalizer rule archives the mirrored playthrough subtree as one unit without changing vanilla exception behavior.

Story autosaves hide their delete UI in `Playthrough_Save.Set`, so the apparent possibility of deleting the broad story/data root is not an ordinary vanilla UI path. IMDC path containment also refuses the private IMDC root itself as an archive source.

## Completed static distribution checks

Before packaging this source revision, the following checks were completed successfully:

- `assets/info.json` parsed as strict JSON and reports version **3.4.6**.
- `IM Data Core.csproj` parsed as XML and reports version **3.4.6**.
- `MinimumSupportedSidecarFormatVersion == SidecarFormatVersion == 5`; `JournalFormatVersion == 2`.
- Every checkpoint construction/serialization path includes `ContentFingerprint`, and every persistence snapshot construction includes `PathArchiveEpoch`.
- All three deletion patch target methods and their exact vanilla deletion paths were rechecked in the supplied decompilation.
- All three deletion hooks use Harmony Finalizers and preserve the original vanilla exception unchanged.
- `git diff --check` passes for the IM Data Core tree.
- All C# sources pass a string/comment-aware delimiter scan.
- Current-facing documentation contains no stale claim that v3/v4 sidecars are accepted. Historical v2/v3/v4 documents are marked as historical.
- The repository ignores `*.dll`, `*.pdb`, `**/bin/`, `**/obj/`, and `artifacts/`; stale generated DLL revision metadata is not treated as source-version authority.
- The Pass 1 standalone snapshot helper preserves the original five vanilla `SavedData` call sites and does not change SWOF Harmony ordering.

These are static checks, not a substitute for compilation or in-game regression testing.

## Recommended in-game regression matrix

- Save twice to the same physical slot within one wall-clock second after changing vanilla state; verify the two v5 checkpoints have different content fingerprints when `SavedData` differs.
- Save, reload, and verify the compact reserialization fingerprint matches the checkpoint created at save time.
- Load a vanilla career that has never had IMDC persistence, mutate IMDC state, call `TryFlushNow`, restart, and verify exact checkpoint activation succeeds.
- Load an existing valid v5 sidecar with no matching checkpoint and verify IMDC fails closed without overwriting it.
- Overwrite A -> New Save B -> Overwrite A, with and without newly captured events.
- Trigger background compaction while deleting the same vanilla save and verify the archived IMDC directory contains a coherent generation and the original path is not recreated by a stale writer.
- Delete the same recycled save identifier repeatedly and verify `OLD`, `OLD2`, `OLD3`, ... archives coexist without overwrite.
- Delete an active save, then perform New Save/Save As and verify the logical in-memory history follows the new path without rewriting the archived old path.
- Force archive rename failure (for example with an external file lock/permission denial), verify the IMDC source directory remains intact, and verify writes to that deleted scope are blocked for the remainder of the process.
- Kill the process at journal BEGIN/record/COMMIT boundaries and during compaction replacement; committed transactions must replay once and torn transactions must not become visible.
- Construct interrupted-compaction recovery with a valid `.imdc.bak` plus matching primary `.imdc.journal`, no backup journal, and a corrupt primary base. Recover, persist once, then verify `.imdc.bak.imdc.journal` exists and the preserved backup generation still reconstructs the same document after the primary journal is cleaned. Inject a backup-journal copy failure and verify the original primary journal is retained instead.
- Construct backup recovery with a valid backup base and valid `.imdc.bak.imdc.journal`, while the preferred primary journal is (a) empty and (b) torn before a complete header. Both cases must fall through to the valid backup journal; neither may report a positive base-hash match.
- Seed sidecar-derived snapshot and backup-journal temp files older than 24 hours plus fresh equivalents and unrelated `.tmp` files. Initializing that physical scope must remove only the stale IMDC-owned candidates.
- Save with Harmony, JSON-only, and multi-DLL mods enabled; then change their state and verify checkpoint mod diagnostics remain diagnostic-only.
- Build at least two rooms, save, restart, and verify each reconstructed room retains the same IMDC generation `EntityId`; destroy/rebuild the highest-numbered room and verify the new room receives a different generation.
- Destroy the highest-numbered theater and cafe, rebuild so vanilla reuses the raw ID, and verify timeline grouping remains separated by IMDC generation while payload `theater_id` / `cafe_id` still expose the reused vanilla value.
- Load an early format-5 checkpoint with no `AgencyRoomIdentities` field and verify it remains readable, assigns fresh forward-safe room generations, and persists them at the next exact checkpoint.
