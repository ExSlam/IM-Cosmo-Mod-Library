# IM Data Core 3.4.6 patch summary

## 3.4.6 persistence-correctness changes

This revision closes four persistence/lifecycle defects found by static comparison with the supplied decompiled Idol Manager code and hardens the new deletion archive path against concurrent IMDC I/O.

1. **Collision-resistant exact checkpoints:** sidecar format 5 adds `ContentFingerprint`, a SHA-256 fingerprint of Unity's compact serialized vanilla `SavedData`. Exact checkpoint identity now includes normalized save path, vanilla `LastSave`, playtime seconds, vanilla game date/time, and this content fingerprint. Distinct vanilla saves can no longer collapse merely because their timestamp/playtime fields collide within the same second.
2. **Low-overhead fingerprinting:** standalone IMDC reuses the compact JSON already produced by its defensive `SavedData` freeze. SHA-256 consumes UTF-8 in bounded chunks so fingerprinting does not allocate a second save-sized byte array. When Save Write Ordering Fix is positively verified and IMDC skips its own freeze, one compact serialization is performed to obtain the fingerprint.
3. **Anchored adoption:** loading a vanilla career with no existing IMDC sidecar seeds an in-memory sequence-0 exact checkpoint. Loading alone still performs no unsolicited write, but a later `TryFlushNow` cannot create an unanchored sidecar that fails exact matching on the next load.
4. **Checkpoint watermark parser fix:** checkpoint `GameDateTime` is parsed through vanilla `ExtensionMethods.ToDateTime`, matching the persisted vanilla `yyyy-MM-dd HH:mm:ss` representation. Event/custom-mutation round-trip timestamp parsing remains unchanged.
5. **Deleted-save preservation:** successful vanilla deletion archives the matching mirrored IMDC directory instead of deleting it. `<name>` becomes `<name>OLD`; collisions use `<name>OLD2`, `<name>OLD3`, and so on. Whole story-playthrough deletion archives the corresponding mirrored playthrough tree as one unit.
6. **Archive/write serialization:** an exclusive persistence-topology lease serializes archival against sidecar load, write, and background compaction. Prepared snapshots carry per-path archive epochs, so a snapshot prepared before deletion cannot recreate the old path after archival.
7. **Archive-failure safety:** if the preservation rename fails, existing supplemental files are left untouched and writes beneath that deleted-save directory are blocked for the remainder of the process.
8. **Active-scope deletion:** deleting the currently active save detaches the physical scope while preserving the logical in-memory branch, allowing a later vanilla New Save/Save As to carry that history to a new path.
9. **Current-format-only development policy:** sidecar format 5 is the only accepted sidecar format. Older sidecars are preserved on disk but are not migrated or activated by this build. Transactional journal format remains 2.

## Retained 3.4.x behavior

- Exact checkpoints still capture the enabled Idol Manager mod inventory and emit diagnostic missing/disabled/metadata/DLL mismatch warnings without blocking vanilla load.
- Existing unmatched sidecars still fail closed rather than activating by in-game date alone.
- New Save/Overwrite Save keeps the active multi-path checkpoint ledger consistent while each physical sidecar serializes only checkpoints for its own target path.
- Process-wide per-path I/O locks, journal hashing, transactional journal replay, atomic compact-base replacement, backup recovery, and background compaction verification remain in place.
- Vanilla remains canonical: IMDC failures are fail-soft/fail-open at Harmony boundaries and do not intentionally prevent a vanilla save or load action.
- Public API behavior, money-ledger aggregation, reflection-safe interop, custom-event idempotency, portrait identity capture, graduation outcome capture, and staff-severance attribution remain unchanged.

## Compatibility and versions

- Project/mod version: **3.4.6**.
- Sidecar `FormatName`: `IMDataCore.LightweightSidecar`.
- Sidecar `FormatVersion`: **5 only**.
- Journal `FormatName`: `IMDataCore.LightweightJournal`.
- Journal `FormatVersion`: **2**.
- No runtime backwards-compatibility or migration path is provided for older sidecar formats in this development build.

## Vanilla deletion targets verified

The preservation hooks correspond to the three game-save directory deletion methods in the supplied decompilation:

- `Popup_Save.Delete()` for legacy/freeplay manual saves.
- `Popup_Load_Story.Delete_Save(save_info)` for a story save directory.
- `Popup_Load_Story.Delete_Playthrough(playthrough_info)` for a complete story playthrough directory.

Autosave UI does not expose the story save delete button, and unrelated `Directory.Delete` uses such as portrait/temp cleanup are intentionally not patched.
