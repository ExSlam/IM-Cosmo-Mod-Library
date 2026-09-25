# Save identification and legacy-match behavior

Data Migration Tool identifies the selected vanilla save before migration so users do not have to recognize opaque folders such as `df550082` by sight.

## Human-readable fields

The selected `SaveManager.SavedData` JSON is read without modifying it. Data Migration Tool displays:

- `staticVars__PlayerData.SaveFileName` when present;
- `GroupName`;
- `FirstName` + `LastName`;
- `IsStoryMode`, `Chapter`, and `Difficulty`;
- `LastSave` and `Playtime_Seconds`;
- `staticVars__dateTime`;
- root save `version`;
- counts of girls/idols, staff, singles, and shows;
- the vanilla relative save path and its opaque slot token.

This follows the supplied decompiled Idol Manager source. `Popup_Save._save_data` uses group name, player name, and last-save time for ordinary saves. `Popup_Load_Story` additionally reads save-file name, chapter, playtime, and in-game date for story saves.

## Reconstructed IMDataCore 1.x keys

The supplied Cosmo Mod Library Git history shows two 1.x save-key paths in `CorePathsAndRuntime.cs`.

### File-scoped key

Data Migration Tool reproduces the historical algorithm:

1. normalize the absolute vanilla save path and lowercase it;
2. resolve it relative to `<persistent>\\data` when possible;
3. replace path separators with `_`;
4. keep only `[A-Za-z0-9_.-]` and truncate the path token to 32 characters;
5. SHA-256 the lowercase normalized absolute path and keep the first 16 lowercase hex characters;
6. build `file_<path-token>_<hash>` and sanitize/truncate to 64 characters.

### Legacy agency key

Data Migration Tool computes both forms used by the historical code. It prefers sanitized `SaveFolderName` when stored (and for story saves can recover the story save-folder token from the vanilla relative path). It also always reconstructs the metadata fallback:

`freeplay|story` + first name + last name + group name + chapter enum name, joined with `_` and sanitized/truncated to 64 characters.

## Candidate discovery

The default old root is:

`%USERPROFILE%\\AppData\\LocalLow\\Glitch Pitch\\Idol Manager\\Mods\\IMDataCore\\saves`

Data Migration Tool also accepts a moved `IMDataCore` root, the `saves` root itself, or an individual copied per-save folder. It looks for `im_data_core.db` and `im_data_core.fallback.json` and ranks folder matches in this order:

1. exact reconstructed file-scoped key;
2. exact reconstructed agency key;
3. exact reconstructed fallback agency key / embedded `SaveFolderName`;
4. exact vanilla slot token such as `df550082`;
5. folder name containing that slot token.

SQLite is preferred over fallback JSON when both exist for the same match because 1.x used SQLite as the primary backend when the native runtime was available.

Candidate discovery is a usability aid, not a replacement for checkpoint verification. Late-1.3 checkpoint-capable sources are still required to match the selected vanilla save's exact historical `v1:<length>:<sha256(raw bytes)>` fingerprint unless the advanced override is explicitly enabled.


For the opposite direction, see `REVERSE_MATCHING.md`. Reverse matching compares the old database's actual `save_key` values against both reconstructed agency forms and the reconstructed file-scoped key for every recognized vanilla save.
