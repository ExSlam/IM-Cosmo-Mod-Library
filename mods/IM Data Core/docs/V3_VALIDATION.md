# IM Data Core 3 validation notes

The v3 source package was checked against the uploaded Cosmo Mod Library and decompiled Idol Manager source.

## Static checks completed

- 27 C# source files are present after intentionally removing the legacy flat-file importer and renaming the persistence partial.
- Public declaration scan matches the uploaded 2.0.8 public surface: 39 declarations versus 39, with no removed or added public declarations.
- `info.json` and the v3 sidecar example parse as JSON.
- IM Data Core and template `.csproj` files parse as XML.
- Project, metadata, and sidecar versions agree on `3.0.0` / format `3`.
- All C# files pass delimiter/string/comment lexical-balance checks.
- Runtime source contains no legacy flat-file importer, old database filename, old fallback filename, or legacy storage-discovery symbols.
- The v3 serializer does not write `EventId`, `GameDateKey`, `PayloadJson`, or `ValueJson` fields.
- `RelativeSavePath` is written once at the sidecar root.
- Structural `Payload` and custom SET `Value` members are written as JSON values.
- Corrupt/newer-sidecar write blocking, per-namespace quota accounting, no-op mutation suppression, and `.imdc.bak` retention are present.
- The decompiled game contains five concrete `DataSaver.saveData<SaveManager.SavedData>` call sites, matching the five concrete save callers targeted by IM Data Core's save lifecycle patch.

## Compiler note

This packaging environment does not contain `dotnet`, MSBuild, Roslyn `csc`, Mono `mcs`, or the Idol Manager reference DLL set, so a real .NET Framework 4.6 compilation could not be executed here. Build the package in the Cosmo Mod Library solution as the final compiler/runtime gate.
