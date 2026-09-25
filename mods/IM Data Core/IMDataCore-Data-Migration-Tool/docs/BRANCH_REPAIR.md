# Branch cross-contamination repair

IMDataCore 1.x could keep one persistent event stream under a save key even after the game loaded an older vanilla save. When that happened, events from multiple save branches could coexist in the same legacy database. Data Migration Tool 1.6.0 adds a deliberately conservative repair workflow for that case.

## What Data Migration Tool can prove

Data Migration Tool orders legacy events by `event_id` and splits the stream when a later event has an earlier game date than the preceding event. A backwards jump is treated as a branch/load boundary. For each resulting branch, Data Migration Tool compares direct event identity references against the selected vanilla save:

- `idol_id` and idol/girl entity IDs against idols in the vanilla save;
- staff entity IDs against staff in the vanilla save;
- single entity IDs against singles in the vanilla save;
- show entity IDs against shows in the vanilla save.

Only direct, numeric identity evidence is scored. Events without an identity that can be checked are neither assumed valid nor assumed invalid.

The branch score rewards direct matches and penalizes direct mismatches. A branch is auto-recommended only when the evidence gives it a clear high- or medium-confidence lead. Otherwise Data Migration Tool does not automatically choose a branch.

## Conservative branch salvage

Conservative salvage:

1. keeps only events belonging to the selected branch;
2. drops any event later than the selected vanilla save's `staticVars__dateTime`;
3. drops legacy events with invalid game-date values;
4. preserves the retained event IDs rather than renumbering history;
5. discards **all legacy `custom_data` rows**;
6. creates a new sidecar-v6 checkpoint bound exactly to the selected vanilla save;
7. records the repair mode, selected branch, original/retained/dropped counts, and repair summary in the optional `cosmo.imdatacore.data-migration-tool.branch-repair` forward-extension record.

`custom_data` is always discarded during branch repair because 1.x stored only the current mutable value. It did not preserve historical values per branch, so Data Migration Tool cannot prove which custom-data value belonged to an earlier vanilla checkpoint.

Projection tables/arrays remain non-authoritative audit information and are not injected as duplicate history.

## Clean-baseline repair

If no branch can be identified confidently, the safe option is **Clean baseline repair**. It discards all legacy events and mutable custom data and writes only a correctly bound v6 checkpoint. This does not reconstruct missing history. It creates a parser-safe current-generation starting point so current IMDataCore can populate modern state on later game saves.

## Manual branch choice

The GUI lists every detected branch with:

- event-ID range;
- game-date range;
- total events;
- events at/before the selected vanilla save;
- events later than the selected save;
- direct identity matches;
- direct identity mismatches;
- branch score and evidence strength.

A user may manually select a branch that is not the recommended branch, but Data Migration Tool requires an explicit warning confirmation before writing it.

## CLI

Analyze without writing:

```powershell
& ".\IMDataCore Data Migration Tool.exe" repair-analyze `
  --source "...\im_data_core.db" `
  --save "...\data\manual_saves\<slot>\save.json"
```

Use the recommended branch when confidence is sufficient:

```powershell
& ".\IMDataCore Data Migration Tool.exe" repair `
  --source "...\im_data_core.db" `
  --save "...\save.json" `
  --repair-mode salvage
```

Choose a branch explicitly:

```powershell
& ".\IMDataCore Data Migration Tool.exe" repair `
  --source "...\im_data_core.db" `
  --save "...\save.json" `
  --repair-mode salvage `
  --branch 2
```

Create a clean baseline:

```powershell
& ".\IMDataCore Data Migration Tool.exe" repair `
  --source "...\im_data_core.db" `
  --save "...\save.json" `
  --repair-mode clean
```

## Limits

A repair cannot manufacture events that the correct branch never recorded. It also cannot recover historical `custom_data` values that 1.x overwrote in place. A monotonically increasing cross-contamination case may have no backwards-time boundary and can remain undetectable without stronger checkpoint or identity evidence. Data Migration Tool therefore presents repair as evidence-based salvage, not perfect historical reconstruction.
