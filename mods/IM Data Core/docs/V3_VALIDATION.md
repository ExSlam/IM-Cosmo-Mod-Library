# IM Data Core 3.2 validation notes

This revision was checked against the uploaded Cosmo Mod Library and decompiled Idol Manager source. EroEvents is intentionally unchanged and is outside the 3.2 edit set.

## Static checks completed

- `info.json` and `IM Data Core.csproj` both report **3.2.0**; the persisted sidecar `FormatVersion` intentionally remains **3**.
- Existing public API methods remain available, and both public API aliases expose `TryReadEventsForIdolPage(...)` alongside the existing recent-event API.
- Paged idol reads use the same merged idol/global ordering as recent reads, seek both projections with binary search, use an exclusive EventId cursor, and return `hasMore`.
- A randomized behavioral model completed **5,000 / 5,000** pagination cases without a gap, duplicate, or ordering mismatch, including equal-date events and changing page sizes.
- Normal v3 sidecar loads use a buffered `FileStream` with `FileOptions.SequentialScan` and a forward-only parser instead of first materializing the complete sidecar as a `string`.
- Normal writer-order v3 event/checkpoint/custom-data arrays are converted record-by-record. Unusual but valid top-level property order remains compatible through a deferred structural-value fallback.
- Structurally parsed v3 event payloads and custom SET values are not reparsed solely for JSON normalization during document validation. Legacy v1/v2 JSON-string payloads retain their validation/normalization path.
- Checkpoint duplicate validation uses a `HashSet<CheckpointIdentity>` with the same identity fields and path comparison semantics as the former pairwise scan.
- Primary-to-`.imdc.bak` recovery, exact-checkpoint activation, branch rollback, idempotent custom-event keys, and save-boundary persistence semantics remain in place.
- A string/comment-aware delimiter scan over the edited IM Data Core and Idol Career Diary runtime C# sources reports no unbalanced delimiters or unterminated literals/comments.
- Edited `info.json` files parse as strict JSON and edited `.csproj` files parse as XML.
- No EroEvents source, asset, project, metadata, or diary-definition file is modified by this revision.

## Idol Career Diary integration checks

- Idol Career Diary requires IM Data Core **3.2+** and resolves the paged timeline API through the existing reflection bridge.
- Initial diary opening requests only the newest raw page. Older pages are fetched on demand for result expansion or cooperatively across frames while a non-empty search needs complete-history coverage.
- Timeline search caches a query-independent corpus per loaded EventId, avoiding repeated presentation/payload/election-name expansion on subsequent searches.
- The diary continues to use the cloned vanilla profile panel as its single scroll owner. Search controls use a vertically sized block with a full-width TMP input and a separate flexible-width action row, avoiding a nested ScrollRect and reducing narrow-width overlap risk.
- Rendered result GameObjects remain bounded by the existing result window (`300`, then `+100` through Show More); pagination expands the model history without instantiating the entire career at once.
- Custom diary-rule lookup is deterministic: exact ID rules outrank prefixes, longer prefixes outrank shorter prefixes, and otherwise equal rules use stable source-mod/file/index tie-breaking rather than filesystem enumeration order.

## Compiler and runtime note

This packaging environment does not contain `dotnet`, MSBuild, Roslyn `csc`, or Mono `mcs`, so a real .NET Framework 4.6 compile and an in-game Unity runtime test could not be performed here.

The delivery archives therefore omit existing `bin`/`obj` output and stale compiled DLLs rather than presenting them as builds of the modified source. Compile these source packages in the normal Cosmo Mod Library development environment before deployment.
