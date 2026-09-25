# Reverse matching old IMDataCore data to vanilla saves

Data Migration Tool 1.3.0 can start from an old IMDataCore 1.x database/fallback file and locate the vanilla Idol Manager save(s) that most likely belong to it.

## Why this is needed

Many early IMDataCore 1.x save folders use keys such as:

```text
freeplay_tv2_koreantes_translationsystest_NONE
```

That value is an IMDataCore save key, not the vanilla manual-slot folder name. The vanilla slot can instead be an opaque token such as `df550082`, and the early database does not contain a direct conversion from the agency key to that token.

## Matching strategy

Data Migration Tool does not try to split underscore-delimited agency keys back into names. That would be ambiguous when player/group names contain underscores or when the 1.x 64-character sanitization/truncation rule applies.

Instead it:

1. Inspects the selected legacy source and reads its real `save_key` values.
2. Scans supported save JSON files under the selected Idol Manager `data` tree.
3. Parses each valid save using the same save reader used by migration.
4. Reconstructs the exact historical 1.x agency key and file-scoped key for that vanilla save.
5. Compares the reconstructed keys to the legacy database key(s).
6. Displays every candidate together with the group, player, opaque vanilla folder token, last-save time, playtime, in-game date, content counts, and relative path.

An exact reconstructed agency/file key is therefore much stronger than heuristic parsing of the old folder name.

## Match strengths

- **Exact 1.x file-save key:** strongest match when the save remains at the same absolute path used by the old IMDataCore build.
- **Exact 1.x agency key:** strong match for early/agency-scoped 1.x data; several snapshots from the same campaign may legitimately share this key.
- **File path token match:** useful when a save tree was moved. The relative-path token matches, but the old absolute-path hash no longer can, so this result requires manual verification.
- **Source-folder fallback:** lower-confidence compatibility hint when the legacy file's parent directory itself matches a reconstructed key or vanilla slot token.

Reverse matching identifies candidates. It does not weaken late-1.3 exact-checkpoint validation, and it does not make a weak path-token match equivalent to a verified checkpoint.

## GUI

Browse to the legacy `im_data_core.db` / `im_data_core.fallback.json` and click **Inspect**. Data Migration Tool automatically scans the default Idol Manager `data` directory when available. The **Likely vanilla save (from legacy source)** list shows candidates. Select one and click **Use save** to populate the normal vanilla-save field without discarding the inspected legacy source.

Use **Scan vanilla data…** to choose another live Idol Manager data tree.

## CLI

```text
& ".\IMDataCore Data Migration Tool.exe" reverse-match \
  --source "...\im_data_core.db"
```

Optional switches:

```text
--data-root "C:\Users\<user>\AppData\LocalLow\Glitch Pitch\Idol Manager\data"
--save-key "<legacy save_key>"
```

When the SQLite database contains multiple save keys, `--save-key` restricts the reverse search to one of them.
