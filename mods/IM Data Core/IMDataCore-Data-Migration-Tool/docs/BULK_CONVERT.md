# Bulk conversion

Data Migration Tool 1.7.0 adds a bulk conversion planner and runner for legacy IMDataCore archives.

## Safety model

Bulk conversion does not simply recurse through legacy databases and write them blindly. It first:

1. Reads the vanilla Idol Manager `data` tree once and reconstructs the historical IMDataCore 1.x matching keys for recognized saves.
2. Recursively scans the selected legacy root for IMDataCore databases/fallback JSON and performs contamination analysis per save stream.
3. Reverse-matches each legacy stream to a vanilla save. Automatic bulk conversion requires a unique high-confidence match. Weak or tied matches are left for manual review.
4. Validates the planned v6 output before marking a row ready.
5. Detects multiple legacy streams that target the same v6 output and prevents double-writing. A strictly stronger match can supersede weaker matches; equal-confidence collisions require manual review.

## Contaminated-save policy

Three policies are available:

- **Auto repair** (default): use conservative branch salvage when branch analysis has high/medium identity evidence; otherwise use clean-baseline repair.
- **Skip contaminated**: leave every timeline with a detected backwards-time branch boundary unselected.
- **Clean baseline**: discard legacy history/custom state for every contaminated stream and create only a correctly bound v6 checkpoint.

Branch repair never invents missing historical events. Legacy 1.x `custom_data` is discarded in repaired streams because it was mutable current state rather than branch-versioned history.

## Existing outputs

By default, an item whose destination already exists is skipped during the write phase. Enabling **Archive and overwrite existing destination artifacts** uses Data Migration Tool's normal archival backup behavior before replacement.

## GUI

Open **Bulk convert** from the main window or from the bulk contamination scanner. Choose the legacy root, vanilla `data` root, and destination IMDataCore root. Use **Analyze / build plan** before writing. Rows can be individually selected or cleared. The plan and run results can be exported as JSON.

## CLI

Plan only:

```text
& ".\IMDataCore Data Migration Tool.exe" bulk-plan --legacy-root "C:\path\to\old IMDataCore saves" --data-root "C:\Users\You\AppData\LocalLow\Glitch Pitch\Idol Manager\data" --contaminated auto --plan-json bulk-plan.json
```

Convert:

```text
& ".\IMDataCore Data Migration Tool.exe" bulk-convert --legacy-root "C:\path\to\old IMDataCore saves" --data-root "C:\Users\You\AppData\LocalLow\Glitch Pitch\Idol Manager\data" --contaminated auto --result-json bulk-result.json
```

Use `--dry-run` to validate all ready rows without writing. Use `--overwrite` to archive and replace existing destination artifacts.


## Manual match resolution

Rows marked **Weak vanilla match**, **Ambiguous vanilla match**, or **No vanilla match** are safety holds for unattended conversion, not permanent failures. Select the row and use **Resolve match…** to inspect candidate vanilla saves and explicitly confirm the correct pairing. The resolver shows the automatic match score/type plus save identity, last-save time, game date, and relative path. It also permits browsing to a raw save manually.

Exact file-key matches are still preferred automatically. Exact agency-key ties are deliberately left to the user because old agency-scoped keys did not retain enough information to distinguish multiple saves from the same campaign.

## Future-tail histories

IMDataCore 1.x could persist while gameplay continued even when the user did not overwrite the corresponding vanilla save slot. A monotonic legacy timeline extending beyond the selected vanilla save date therefore does not by itself prove cross-branch contamination. For a single monotonic branch, Data Migration Tool can use **future-tail truncation**: retain only legacy events at or before the vanilla checkpoint and drop mutable legacy custom-data state, because its earlier value cannot be reconstructed safely. A backwards-time branch reset remains a separate contamination signal and continues through the branch-repair path.

## Legacy source cleanup (1.9.0+)

Cleanup is always opt-in. Data Migration Tool never permanently deletes a legacy database during normal planning, validation, conversion, or repair.

The Bulk Convert window provides three cleanup controls:

- **After each successful conversion, move its legacy source to the Windows Recycle Bin**: after the v6 sidecar is written and post-write revalidated, the exact legacy source used for that item is moved to the Windows Recycle Bin. SQLite `-wal`, `-shm`, and `-journal` companions are moved with it when present.
- **Recycle successfully migrated…**: performs the same cleanup later for successful items in the current bulk run that have not already been cleaned up.
- **Recycle orphaned sources…**: moves only strict `No vanilla match` sources to the Windows Recycle Bin after a separate confirmation. Weak matches, ambiguous matches, validation failures, contaminated-but-repairable sources, and superseded sources are deliberately excluded.

A successful migration is cleanup-eligible only after Data Migration Tool reopens the written sidecar, deserializes it as v6, reruns the strict v6 validator, and confirms that its relative save path and checkpoint fingerprint still match the selected vanilla save. A cleanup failure does not retroactively mark the migration as failed; it is logged separately.

CLI equivalents are `--recycle-after-success` and `--recycle-orphans` for `bulk-convert`. Single-save `migrate` and `repair` also accept `--recycle-source`.

For legacy SQLite files containing more than one `save_key`, Data Migration Tool treats the physical DB as one cleanup unit. It will not recycle that DB after a partial bulk run: every planned stream from that source must be selected, ready, and successfully converted. Likewise, orphan cleanup recycles a shared DB only when every stream discovered in that DB is a strict orphan.

### Empty per-save folder cleanup

When a legacy source is recycled, Data Migration Tool also checks the immediate folder that contained it. If the source database/JSON and its SQLite companion files were removed and the folder is now empty, that per-save folder is sent to the Windows Recycle Bin as well. Data Migration Tool never recursively removes a non-empty folder and never walks upward into the shared `saves` root.
