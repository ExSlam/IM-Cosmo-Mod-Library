# IMDataCore Data Migration Tool

**Purpose:** migrate legacy IMDataCore **1.0.0–1.3.0** data to the current IMDataCore **sidecar format v6**, with current **journal format v3** compatibility.


## What is new in 2.0.2

- Completed localization of the main validation/migration log instead of dumping English migration-layer diagnostics into non-English interfaces.
- Localized source type, version detection explanations, checkpoint status, warnings, validation/migration results, repair summaries, and common cleanup messages.
- Localized save-kind labels, playtime, idol/staff/single/show counts, and game-version labels in the save identity summary.
- Japanese, Russian, Korean, and Simplified Chinese log output now uses native-script user-facing terminology; literal paths, hashes, filenames, and version numbers remain exact.

## What is new in 1.10.0

- Added a plain-language **Help** guide for regular users, with step-by-step sections for individual migration, starting from an old IMDataCore file, validation, branch repair, bulk contamination scan, bulk conversion, manual match resolution, future-tail truncation, cleanup, common statuses, and logs.
- Help is available from the main window and from the Bulk Scan, Bulk Convert, Match Resolver, and Branch Repair windows. Contextual Help buttons open the guide near the relevant section.
- The Help popup uses the same single two-axis scroll surface as the other custom popups, so large fonts, DPI scaling, and 1080p-class working areas cannot strand content offscreen.
- Help text is provided in all seven GUI languages and intentionally avoids developer terminology where a normal user explanation is enough.

## What is new in 1.9.3

- Bulk contaminated-save policy dropdown now measures its localized items and expands to the longest label, including after font scaling, so option text is not clipped.

- Returned all GUI buttons to standard rectangular Windows button styling.
- Removed blue/accent button backgrounds so button text always uses the normal system foreground/background combination.
- Retained font-aware auto-sizing so localized labels and larger accessibility font settings do not clip.


## What is new in 1.9.1

- All custom popup windows now place their complete UI inside one shared two-axis scroll viewport. If DPI/font scaling or a smaller working area would otherwise push controls offscreen, the popup itself exposes horizontal and vertical scrolling instead.
- Legacy cleanup now also moves the immediate per-save legacy folder to the Windows Recycle Bin after its source files have been removed, but only when that folder is empty. Non-empty folders are preserved to avoid deleting unrelated files.

## What is new in 1.9.0

- Added opt-in **Windows Recycle Bin cleanup** for legacy sources after successful, post-write-revalidated migration.
- Added **Recycle successfully migrated…** and **Recycle orphaned sources…** controls to Bulk Convert.
- Strict orphan cleanup only includes `No vanilla match` entries; weak/ambiguous/validation-failed/superseded sources are protected.
- Added post-write v6 revalidation before a source can become cleanup-eligible.


## What is new in 1.8.0

- Bulk conversion now has **Resolve match…** for weak, ambiguous, and unmatched legacy rows. It shows all recognized vanilla saves, automatic match score/type, identity details, dates, and paths, and lets the user explicitly choose the intended save.
- A user-confirmed manual match may supersede another planned source targeting the same v6 output, but only after a collision confirmation prompt.
- Monotonic legacy histories that merely continue past the selected vanilla save are now treated as **future-tail** cases rather than branch contamination. Data Migration Tool can conservatively retain events only through the vanilla save date and discard mutable legacy `custom_data`, which cannot be rolled back safely.
- Bulk-plan JSON now records whether a pairing was manually confirmed.


## What is new in 1.7.3

- Fixed seven localization dictionary initializer errors introduced in 1.7.2.
- Fixed bulk-convert progress callbacks by exposing the progress object through `IProgress<BulkConvertProgress>`.
- No migration, repair, matching, or sidecar-format behavior changed in this patch.

## What is new in 1.7.2

- The main title/settings/tools card is now collapsible with **Show header / Hide header**.
- The header/settings card now lives inside the main scrolling workflow, so it scrolls away instead of remaining sticky while working farther down the form.
- The banner control remains reachable from the compact header expander strip even when the header content itself is collapsed.
- **Validate only / Repair branch contamination / Migrate copy / Open destination** were moved out of the sticky footer and into a card at the end of the main scrolling workflow.
- The new header toggle is translated in all seven GUI languages and includes Narrator accessibility metadata.

## What is new in 1.7.1

- Added a translated **Show banner / Hide banner** control to collapse the decorative top banner and recover vertical space on 1080p/high-DPI displays.
- The banner remains responsive when expanded and recalculates its height from the current client area.
- Reworked the bulk contamination scanner so progress/summary text no longer causes the results and details sections to repeatedly resize during a scan.
- The bulk scanner now uses a stable horizontal results/details split and a fixed-height status area that only changes when the user changes GUI font scale.
- The bulk results grid explicitly uses fixed-width columns with both scrollbars enabled, so wide result tables remain reachable on 1080p instead of forcing the window wider or clipping columns.

## What is new in 1.7.0

- Added a GUI and CLI **bulk convert** workflow for whole legacy IMDataCore archives.
- Bulk conversion reverse-matches legacy streams to vanilla saves, validates each proposed v6 output, and detects destination collisions.
- Detected contaminated timelines can be automatically branch-salvaged when evidence is strong enough, clean-baseline repaired, or skipped according to policy.
- Existing outputs are skipped unless archival overwrite is explicitly enabled.
- Bulk conversion plans and run results can be exported as JSON.

See `docs/BULK_CONVERT.md` for the safety model and CLI examples.


## 1.6.3 1080p layout, button sizing, and locale auto-detection

- Reworked the main header so language, font-size, bulk-scan, Logs, and About controls stay inside the visible window and wrap when necessary.
- Reworked the bottom Validate / Repair / Migrate / Open action bar so it fills the available width and wraps instead of drifting offscreen.
- Buttons now auto-size their width and height from localized text and the selected font scale, preventing clipped labels at larger font settings and DPI scales.
- The legacy-input table now gives label/action columns their natural width while the central path/value column consumes the remaining space; the work area can scroll when a very large font genuinely needs extra room.
- The validation/migration log gets a larger minimum share of the widescreen split so it does not collapse into a narrow strip.
- The banner is now painted by a dedicated responsive control that preserves aspect ratio and centers the full graphic inside the available width.
- Main, bulk-scan, repair, and About windows size themselves against the current Windows working area, improving behavior on 1080p displays with display scaling.
- GUI language is auto-detected from the Windows UI culture at startup. English variants map to English; French, Russian, Japanese, Korean, Brazilian Portuguese, and Simplified Chinese map to their built-in translations when applicable. Detection failures and unsupported locales fall back to English.

## 1.6.1 startup reliability and diagnostics

- Fixed a WinForms startup crash introduced by the responsive split layout: the main `SplitContainer` no longer assigns a large splitter distance or panel minimum sizes before the control has a real laid-out width. The split is now calculated after layout with bounds checks.
- Added persistent diagnostic logging under `%LOCALAPPDATA%\Cosmo\IMDataCore Data Migration Tool\Logs`.
- Added a **Logs** button to the main GUI.
- Startup exceptions are caught and shown in a fallback dialog with the exact diagnostic log path.
- The existing validation/migration log now mirrors its messages into the session diagnostic file.


## 1.6.0: branch cross-contamination repair

Data Migration Tool now has a dedicated **Repair branch contamination…** workflow. It splits a divergent legacy event stream at backwards-time save/load boundaries, scores each branch against identities present in the selected vanilla save, and shows the evidence before writing anything.

Two repair modes are available:

- **Conservative branch salvage** keeps only the selected branch through the vanilla save's game date. It drops later events, invalid-date events, every other branch, and all legacy `custom_data` because 1.x did not preserve branch-versioned custom-data history.
- **Clean baseline repair** discards all legacy events/custom data and creates only a fresh exact v6 checkpoint bound to the selected vanilla save. This is the safe fallback when the correct branch cannot be proven.

Repair output includes an optional `cosmo.imdatacore.data-migration-tool.branch-repair` provenance extension with the chosen mode, branch range, and retained/dropped counts. The CLI adds `repair-analyze` and `repair`. See `docs/BRANCH_REPAIR.md`.

## 1.5.0: responsive and accessible GUI

The Windows GUI uses a wider responsive layout aimed at 1920×1080 screens, a split work/log view, card-style surfaces with softly rounded sections, a cleaner color palette, font scaling from 95% through 160%, and screen-reader/Narrator accessibility names/descriptions on the primary controls.

## 1.4.0: bulk contamination / branch-boundary scanning

Data Migration Tool can now recursively scan an old IMDataCore root and audit every `im_data_core.db` and `im_data_core.fallback.json` it finds. The scanner analyzes each SQLite `save_key` separately, walks legacy events in `event_id` order, and detects backwards jumps in game time. Those jumps are reported as likely save-load / branch boundaries.

The GUI button **Bulk contamination scan** opens a table containing the source file, save key, event count, branch count, likely primary divergence event, pre-reset date, reset-to date, rollback span, and whether late-1.3 checkpoint support exists. Selecting a row shows every detected backwards-time boundary for that stream. Reports can be exported as CSV or JSON.

CLI example:

```powershell
& ".\IMDataCore Data Migration Tool.exe" bulk-scan --root "C:\Old IMDataCore saves\saves" --csv contamination.csv --json contamination.json --include-clean
```

A backwards-time boundary is strong evidence that one persistent 1.x event stream received events after loading an earlier save/branch. The scanner deliberately reports evidence rather than deleting or rewriting anything. A clean result means **no backwards game-time reset was detected**, not proof that every row belongs to one vanilla save. See `docs/CONTAMINATION_SCAN.md`.


### 1.3.3 game-date compatibility and pre-checkpoint safety

Idol Manager serializes `staticVars__dateTime` as `yyyy-MM-dd HH:mm:ss`, while current IMDataCore event/custom-mutation records use .NET round-trip (`o`) timestamps. Data Migration Tool now preserves the vanilla-format date verbatim in the v6 checkpoint (so checkpoint matching remains exact) and converts only synthetic custom-data mutations to round-trip form. For pre-checkpoint SQLite databases, Data Migration Tool also blocks by default when legacy events are later than the selected vanilla save's game date; the advanced unverified-association option can override this explicitly.


## 1.3.2 reverse-match auto-apply

When reverse matching is started from a legacy IMDataCore source with no vanilla save selected, a **unique exact historical save-key match** is now applied automatically. Data Migration Tool fills the Raw vanilla game save field, preserves the selected legacy IMDataCore file, selects the matching SQLite `save_key` when applicable, and resets the destination root to the matched save's normal live `IMDataCore` root. Ambiguous/tied candidates still require manual selection.

## What is new in 1.3.1

- The About dialog now shows the full GitHub and Steam destination URLs directly beneath their descriptive labels.
- Each two-line label + URL block remains clickable.


## What is new in 1.3.0

- Added **reverse matching** from an old IMDataCore 1.x database/fallback file back to likely vanilla Idol Manager saves.
- Data Migration Tool now scans the vanilla `data` tree, recomputes the actual historical 1.x agency/file keys for every recognized save, and ranks exact matches without trying to ambiguously split underscore-delimited old keys.
- Reverse-match candidates show the opaque vanilla folder token (for example `df550082`), group, player, last-save time, playtime, in-game date, content counts, and relative save path.
- Added a lower-confidence moved-tree fallback for `file_..._<hash>` keys: the relative path token can still match when the absolute-path hash changed.
- Added GUI **Use save** / **Scan vanilla data…** controls and the CLI `reverse-match` / `find-save` command.
- Added translations for the reverse-matching UI in all seven built-in GUI languages.

## What is new in 1.2.2

- Removed the two nullable-reference compiler warnings in the WinForms title-font initialization.
- No migration, localization, or file-format behavior changed.


## What is new in 1.2.1

- Added four GUI languages: **Brazilian Portuguese, Russian, French, and Korean**.
- The GUI language selector now offers seven built-in languages in total: English, Brazilian Portuguese, Russian, French, Japanese, Korean, and Simplified Chinese.
- The new translations cover the main migration form, file/folder prompts, save-identification labels, migration-choice labels, and the About dialog.

## What is new in 1.2.0

- Added **GUI language selection** with built-in English, Japanese, and Simplified Chinese translations.
- Added a top **banner image** in the main window.
- Added a dedicated **About** dialog with copyright, attribution to Cosmo, graphics attribution to ChatGPT, and project links.
- Clarified authorship: this tool is presented as a tool by **Cosmo**.

**Author: Cosmo**  
Offline Windows migrator for Idol Manager's **IM Data Core 1.0.0 through 1.3.0** legacy persistence into the current **`IMDataCore.LightweightSidecar` format 6** layout used by the supplied Cosmo Mod Library working tree.

Data Migration Tool is intentionally an external converter. IM Data Core 2.0.0 (`3a267a1`) explicitly stopped supporting the 1.x backend, while the supplied current working tree identifies the physical sidecar as **v6** and journal as **v3**. This utility converts the old database once, without loading old storage code into the game.

## What it migrates

Data Migration Tool preserves the authoritative 1.x **event stream** and **namespaced custom-data state**. Old projection tables/arrays are validated/count-audited but are not emitted as duplicate history. This matches Cosmo's own 2.0 `LegacyFlatFileImporter` design, which deliberately imported events plus custom state and omitted redundant materialized projections.

The generated sidecar contains one exact checkpoint bound to the selected raw vanilla save. V6-only identity-binding collections are emitted in the schema-sanctioned legacy-unbound state (`IdentityBindingsVersion=1`, `IdentityBindingsComplete=false`, empty lists), namespace ownership is emitted as `legacy_unbound`, and newer coverage/baseline collections start empty. Current IM Data Core can therefore parse the sidecar immediately and populate newer checkpoint/coverage fields as subsequent saves occur rather than receiving fabricated historical certainty.

## Safe migration workflow

1. **Close Idol Manager.** Data Migration Tool refuses a live-tree write when it can detect the game running.
2. Start from either side: select the corresponding raw vanilla save, **or** browse to an old `im_data_core.db` / fallback file and let Data Migration Tool reverse-match it against the vanilla `data` tree.
3. Data Migration Tool displays human-readable save identity: save label when present, group/agency, player fields, story/free-play mode, chapter, last-save time, playtime, in-game date, game version, object counts, and the relative vanilla path.
4. Data Migration Tool reconstructs both historical 1.x save-key schemes and scans `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager\Mods\IMDataCore\saves` for likely `im_data_core.db` / fallback matches. Manual slots such as `df550082` are displayed explicitly as slot tokens. You can also scan a copied/archived legacy root.
5. Choose a suggested legacy match, or when starting from the old database choose a reverse-matched vanilla save. Reverse matching compares reconstructed 1.x keys rather than guessing from the opaque folder name.
6. The destination defaults to the current mirrored root `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager\IMDataCore` and preserves the vanilla save's relative path.
7. Use **Validate only** first, then **Migrate copy**. If validation reports cross-branch contamination, use **Repair branch contamination…** and review the branch evidence before choosing salvage or clean baseline.
8. Existing sidecars are never overwritten by default. If overwrite is explicitly enabled, the primary, journal, `.imdc.bak`, and backup journal are first moved to timestamped `data-migration-tool-backup-*` files.

See `docs/SAVE_IDENTIFICATION.md` for the exact save-identity fields and reconstructed 1.x key algorithms, and `docs/REVERSE_MATCHING.md` for the reverse-search algorithm and confidence levels.

For late-1.3 sources that contain exact save-generation checkpoints, Data Migration Tool requires the legacy `v1:<length>:<sha256(raw bytes)>` fingerprint to match the selected vanilla save. The advanced override exists for recovery work, but is deliberately off by default. 1.0–1.2 and early-1.3 sources predate exact checkpoint binding, so their source/save association is necessarily user-supplied; the newly generated v6 checkpoint is still bound exactly to the selected save.

## GUI and CLI

Launch `IMDataCore Data Migration Tool.exe` with no arguments for the WinForms GUI.

```text
& ".\IMDataCore Data Migration Tool.exe" save-info --save "...\data\manual_saves\df550082\save.json"

& ".\IMDataCore Data Migration Tool.exe" inspect --source "...\im_data_core.fallback.json"

& ".\IMDataCore Data Migration Tool.exe" reverse-match \
  --source "...\im_data_core.db"

& ".\IMDataCore Data Migration Tool.exe" repair-analyze \
  --source "...\im_data_core.db" \
  --save "...\data\manual_saves\<slot>\save.json"

& ".\IMDataCore Data Migration Tool.exe" repair \
  --source "...\im_data_core.db" \
  --save "...\data\manual_saves\<slot>\save.json" \
  --repair-mode salvage

& ".\IMDataCore Data Migration Tool.exe" validate \
  --source "...\im_data_core.db" \
  --save "...\data\manual_saves\<slot>\save.json" \
  --save-key "<legacy-key>"

& ".\IMDataCore Data Migration Tool.exe" migrate \
  --source "...\im_data_core.fallback.json" \
  --save "...\data\auto_save.json"
```

The `save-info` command can also take `--legacy-root <dir>` to scan a moved backup tree. `reverse-match` can take `--data-root <dir>` and, for multi-key SQLite databases, `--save-key <key>`. Migration CLI switches are `--output-root`, `--save-key`, `--legacy-version 1.0.0|1.1.0|1.2.0|1.3.0`, `--overwrite`, and the recovery-only `--allow-unverified-checkpoint`. Repair adds `--repair-mode salvage|clean` and optional `--branch <n>`.

## Build

Requirements: Windows 10/11, .NET 8 SDK, and Visual Studio 2022 or `dotnet` CLI. There are **no NuGet package dependencies**. SQLite reading uses Windows' built-in `winsqlite3`, and the source database plus any `-wal`/`-shm` siblings are copied to a temporary directory before being opened so the original legacy database is never touched.

```powershell
./scripts/StaticCheck.ps1
./scripts/Build.ps1
```

Or open `IMDataCore.Data Migration Tool.sln` and build Release/Any CPU. The project targets `net8.0-windows` with WinForms.


### Building inside Cosmo-Mod-Library

Data Migration Tool includes its own `Directory.Build.props` and also explicitly enables SDK default compile items in the project file. This is intentional: the Cosmo-Mod-Library root `Directory.Build.props` is for Unity/.NET Framework mod DLLs and sets `EnableDefaultCompileItems=false`. Without this isolation, a nested standalone tool can appear to restore successfully but compile no `.cs` files, producing `CS5001` even though `Program.Main` exists.

## Important provenance note

The current v6 implementation only accepts built-in `legacy_migration` provenance from lightweight sidecar v1–v5. The pre-2.0 1.x database is older than that family. Data Migration Tool therefore uses a deterministic **synthetic lightweight-v1 provenance bridge** for the parser-visible migration record and stores the true source backend/version/checkpoint status/hash in an optional forward-extension record named `cosmo.imdatacore.data-migration-tool.pre2-migration`. The extension is `RequiredForRead=false`, so current v6 readers preserve it without needing to understand it. See `docs/FORMAT_RESEARCH.md` for the exact rationale and commit trail.

## Package status

**1.1.0 save identification update:** after selecting a vanilla save, the GUI now exposes the human-readable identity fields used by Idol Manager's own load UI, the opaque vanilla slot token, the embedded `SaveFolderName`, both reconstructed IMDataCore 1.x key candidates, and likely legacy source files under the historical storage root. The CLI has the same information through `save-info`.

**1.0.1 packaging fix:** isolates the standalone .NET 8 project from Cosmo-Mod-Library's parent Unity `Directory.Build.props`, preventing `CS5001` caused by inherited `EnableDefaultCompileItems=false`.


Before using repair on a valued campaign, test on copied saves first, inspect the generated JSON/provenance, and verify load/save behavior with the exact IM Data Core build you plan to use.
