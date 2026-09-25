# Bulk contamination and branch-boundary scanner

IMDataCore 1.x could keep one persistent event stream while the player loaded an older vanilla save. In pre-checkpoint generations, that could produce a stream whose `event_id` continues increasing while the recorded game time suddenly moves backwards. Data Migration Tool treats that backwards-time jump as a **likely save branch boundary**.

## What is scanned

The bulk scanner recursively searches the selected root for:

- `im_data_core.db`
- `im_data_core.fallback.json`

For SQLite files, every distinct `save_key` is analyzed independently. A single database can therefore produce multiple report rows.

## Divergence detection

Events are ordered by legacy `event_id`. Data Migration Tool parses each valid game timestamp and compares it with the previous valid event timestamp. If the next event has an earlier game time, Data Migration Tool records a divergence point containing:

- previous event ID and game date
- first event ID after the reset and its game date
- rollback length in seconds/days

Every backwards-time boundary is retained in the detailed report. The **primary divergence** is the largest rollback in that stream; ties use the earliest divergence event.

Example:

```text
event 44751  2023-11-13 07:27:30
     ↓ load/branch reset
 event 44752  2021-07-15 00:00:00
```

The report would identify event `44752` as the divergence event and report the rollback interval from the previous event.

## Status meanings

- **Contaminated / divergent timeline**: one or more backwards-time boundaries were found and the source does not have late-1.3 checkpoint support.
- **Branch reset(s) detected; checkpoint-capable**: the stream contains branch resets, but the late-1.3 source also has checkpoint machinery that may allow an exact checkpoint migration when paired with the correct vanilla save.
- **No branch reset detected**: no backwards-time boundary was found.
- **No reset detected; invalid date rows present**: no reset was found among parseable rows, but some event timestamps could not be interpreted.
- **Scan error**: the file could not be analyzed; the detailed panel/export records the reason.

A clean scan is not a mathematical proof that a database contains no contamination. If two branches happen to advance monotonically in game time, or if old mutable state was overwritten without an event-time reset, event chronology alone cannot prove the difference. The scanner is intentionally evidence-based and non-destructive.

## GUI

Open **Bulk contamination scan** from the main window. Pick the legacy root and click **Scan root**. The results table is sortable. Selecting a row shows all detected branch boundaries. Use **Export CSV** or **Export JSON** to save an audit report.

## CLI

```powershell
& ".\IMDataCore Data Migration Tool.exe" bulk-scan --root "D:\Backups\IMDataCore\saves"
```

Only divergent streams are printed by default. Add `--include-clean` to print every stream.

```powershell
& ".\IMDataCore Data Migration Tool.exe" bulk-scan `
  --root "D:\Backups\IMDataCore\saves" `
  --include-clean `
  --csv "D:\Reports\imdc-contamination.csv" `
  --json "D:\Reports\imdc-contamination.json"
```

The CSV/JSON reports always contain all scanned streams, regardless of `--include-clean`.
