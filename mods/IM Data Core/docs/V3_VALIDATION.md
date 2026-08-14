# IM Data Core 3.1 validation notes

The patched source package was checked against the uploaded Cosmo Mod Library, decompiled Idol Manager source, and EroEvents source used for this revision.

## Static checks completed

- 27 C# runtime source files are present after removing the obsolete `LegacyFlatFileImporter.cs` stub.
- Existing public API methods remain present and `TryAppendCustomEventOnce` is added to both `IMDataCoreApi` and compatibility alias `IMDataCoreAPI`.
- `info.json` reports `3.1.0`, the IM Data Core project reports `3.1.0`, and the sidecar format intentionally remains version `3`.
- Edited metadata/example JSON files parse as strict JSON.
- IM Data Core, template, and EroEvents `.csproj` files parse as XML.
- 45 C# source files across the patched IMDC and EroEvents packages pass string/comment-aware delimiter-balance scanning.
- Runtime IMDC source contains no SQLite/SQL-provider path and no `TryActivateThroughGameDate` fallback.
- The unmatched-checkpoint load path explicitly enters protected read-only supplemental state only when an existing sidecar document was actually loaded; a new physical save with no sidecar remains writable.
- Pending substory completion reconstruction reads vanilla's restored `Substories_Manager.dialogueQueue`.
- Recent idol timeline code contains the two-way idol/global merge and incrementally sorted timeline insertion path.
- `TryAppendCustomEventOnce` persists optional `IdempotencyKey`, validates namespace/key uniqueness, and rebuilds the active idempotency lookup from branch events.
- Sidecar loading contains primary-to-`.imdc.bak` recovery and still applies exact-checkpoint activation after recovery.
- Persistence uses the streaming `SerializeTo(TextWriter, ...)` path and performs JSON/fsync work outside the controller runtime lock after creating a stable persistence snapshot. Same-path generations suppress only snapshots older than a newer already-durable write.
- Normal forward saves no longer rebuild and re-sort every derived event/custom-data index when checkpoint trimming removes no active mutations.
- Save lifecycle contains the conditional Save Write Ordering Fix fast path that avoids a redundant IMDC `SavedData` JSON clone when that assembly is present.
- Namespaced custom events are excluded from internal money-ledger classification even when their event type text resembles a built-in money event.
- EroEvents metadata/project versions agree on `1.0.1`.
- All 26 EroEvents career-diary metadata entries match only `substory_completed`.
- The EroEvents bridge resolves `TryAppendCustomEventOnce` when available and retains the older append API fallback.
- Source diffs pass `git diff --check` for whitespace errors.

## Existing EroEvents relaxed JSON assets

Several pre-existing EroEvents gameplay asset files are not strict RFC-style JSON when parsed by Python's standard `json` module. Those files were byte-for-byte unchanged by this patch and are outside the diary/IMDC edits. The edited EroEvents `info.json` and career-diary JSON both pass strict JSON parsing.

The final cleaned-package static validation pass completed 84 checks with 84 passes.

## Compiler note

This packaging environment does not contain `dotnet`, MSBuild, Roslyn `csc`, Mono `mcs`, or the Idol Manager reference DLL set, so a real .NET Framework 4.6 compilation could not be executed here.

Stale `bin` and `obj` output from the uploaded source trees is omitted from the delivery archives rather than being presented as a build of the patched source. Build in the Cosmo Mod Library / EroEvents development environment as the final compiler and in-game runtime gate.
