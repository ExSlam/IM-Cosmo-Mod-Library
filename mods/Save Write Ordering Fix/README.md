# Save Write Ordering Fix

## Version 1.4.5

Fixes vanilla same-path save ordering races. This cumulative development 1.4 build preserves the complete 1.3 career transport, adds ordered `GlobalData` write/read coverage, and now delegates transport coordination to authoritative Save n Load Fixes when both mods are installed.

Post-1.4.5 runtime qualification makes every transport transpiler idempotent under
HarmonyX recomposition. SWOF now distinguishes a complete already-installed local
replacement from an untouched vanilla call and from authoritative SNLF delegation;
mixed, partial, duplicate, or changed shapes still fail closed.

## Important: 1.4.5 retains the Mono-safe 1.3 architecture

Do not use 1.0.0 or 1.0.1.

Those versions Harmony-patched constructed `DataSaver<T>` methods. That is unsafe on Idol Manager's Mono runtime because reference-type generic instantiations may share runtime code. In practice, a `SavedData` patch can interfere with other `DataSaver<T>` uses such as `GlobalData`.

Version 1.4.5 contains **no Harmony patch on `DataSaver<T>`**.

## Development 1.4 change 5: transport-coexistence source qualification

This increment adds an explicit TX source/static qualification layer without changing transport behavior. The new guard cross-checks SWOF against the shipped SNLF provider contract and the supplied decompiled game source. It source-qualifies independent-install dependencies (TX-08/TX-09), exact shape-failure behavior (TX-15), and the no-constructed-generic rule (TX-16), while locking the source-side one-owner/delegation wiring for TX-10 through TX-13.

Those latter TX cases are **not** claimed complete from source alone: final 1.4.0 still requires live Unity/Mono proof that one logical request produces one physical owner and that delegated wait/file/directory exclusion behaves correctly under concurrency. TX-01 through TX-07, TX-10 through TX-14 runtime halves, TX-17, and TX-18 remain release-qualification work.


## Development 1.4 change 4: test-matrix and source-guard closure

The stale 1.2-era `docs/TEST_MATRIX.md` has been replaced with the cumulative 1.4 matrix. It now covers the exact five career write methods, seven career reader methods / eight read call sites, the one GlobalData writer and reader, SWOF-only transport behavior, SNLF delegation, effective-owner semantics, public coordination APIs, and the remaining live release gates.

The previously referenced but missing `scripts/Test-PatchHealthSource.py` now exists as a real source/IL-shape guard. It asserts the exact vanilla caller manifest method-by-method, verifies caller-level Mono-safe interception, freezes `Priority.Last` plus SNLF ordering/delegation, checks the absence of constructed-generic `DataSaver<T>` Harmony targets, and verifies the effective-owner API contract.

This change intentionally does **not** alter transport behavior. Aside from the development version constant, the production transport implementation is unchanged from dev.3. The source-side Sprint-1C work is therefore at the release-gate boundary; final 1.4.0 still requires live Mono/Harmony validation, especially SWOF-only GlobalData behavior and the both-installed one-owner/no-duplicate-write matrix.

## Development 1.4 change 2: Save n Load Fixes delegation

When `SaveNLoadFixes.SaveTransportApi` is present and reports `IsAuthoritativeTransport == true`, SNLF must remain the one effective physical transport owner. SWOF discovers that API by reflection only; it has no SNLF assembly reference.

All four SWOF transport transpilers now declare `HarmonyAfter("com.cosmo.savenloadfixes")`. If the caller IL already contains SNLF's exact `OrderedSaveTransport` replacement, SWOF treats that as delegated success and returns the call site unchanged instead of installing a second local queue/read owner. This recognition is based on the replacement method's declaring type, name, signature, and return type, not on patch-time provider activation, because Harmony can execute individual transpilers before SNLF has finished aggregating its four health surfaces.

At runtime, while SNLF is authoritative:

- `SavedDataInterceptionHealthy` reports SNLF's effective SavedData health for compatibility with existing consumers;
- `GlobalDataInterceptionHealthy` reports SNLF's effective GlobalData health;
- `HasPendingWrites(...)` forwards to SNLF;
- `TryWaitForPendingWrites(...)` forwards to SNLF;
- `TryRunExclusiveFileAccess(...)` forwards to SNLF; and
- `TryAcquireExclusiveDirectoryAccess(...)` forwards to SNLF.

If SNLF is absent or non-authoritative, SWOF keeps using its local per-path coordinator exactly as a standalone transport provider. Delegation never calls back through SWOF, so no provider loop is introduced.

## Development 1.4 change 3: effective-owner API

The public API now exposes `EffectiveTransportHealthy`, `TransportOwner`, and `IsAuthoritativeTransport`. `EffectiveTransportHealthy` follows the actual active provider across both SavedData and GlobalData. `TransportOwner` reports `com.cosmo.savenloadfixes` while authoritative SNLF owns transport, otherwise reports `com.cosmo.savewriteorderingfix` only when standalone SWOF has complete local coverage. `IsAuthoritativeTransport` deliberately means **SWOF itself owns transport**, so it is false during SNLF delegation even though the existing coordination APIs keep forwarding successfully.

This is still **not final SWOF 1.4.0**. Source-side Sprint-1C implementation and documentation are now closed; live Mono/Harmony coexistence and transport-stress qualification remain the release boundary.

## Development 1.4 change 1 foundation: GlobalData transport

The supplied game source contains one sibling GlobalData write/read pair outside SWOF 1.3.0:

- `SaveManager.SaveGlobalData()` -> `DataSaver.saveData<SaveManager.GlobalData>(..., "global_data", true, false)`
- `SaveManager.LoadGlobalData()` -> `DataSaver.loadData<SaveManager.GlobalData>("global_data")`

This increment patches those **concrete callers only**. `SaveGlobalDataEvent` still runs first, then SWOF freezes the populated GlobalData JSON on the caller thread and queues `global_data.json` through the same per-path FIFO used by career saves. `LoadGlobalData()` waits only for that physical path before calling vanilla `DataSaver.loadData<GlobalData>`.

In standalone mode, `SaveWriteOrderingApi.GlobalDataInterceptionHealthy` becomes true only when the writer and reader are each intercepted exactly once. Under authoritative SNLF, the same property reports SNLF effective health instead. The original local GlobalData coordinator remains available when SWOF is installed by itself.

## How the preserved 1.3 career transport works

Idol Manager has five concrete vanilla places that write `SaveManager.SavedData`:

- `SaveManager.SaveData(bool, bool)`
- `SaveManager.SaveChapter(tasks._chapter)`
- `Popup_Save.Save()`
- `Popup_Load_Story.Do_Overwrite_Save(save_info)`
- `Popup_Load_Story.Do_New_Save(string)`

The mod transpiles those concrete callers only. At the final `DataSaver.saveData<SaveManager.SavedData>` instruction, it substitutes an ordered writer with the **same four arguments and void return type**.

For each physical save path:

1. The exact JSON belonging to the request is frozen immediately.
2. The request enters a FIFO queue for that path.
3. A background writer writes the queued requests in request order.
4. A newer save can no longer finish before an older one and then be overwritten by that older request.
5. Different physical save paths retain independent asynchronous writers.

If caller-thread JSON freezing throws after the physical path has been resolved, 1.3.0 does **not** escape to vanilla's untracked `DataSaver.saveData` thread. The request stays in the same ordered queue and retries serialization once on that queue's writer thread. That exceptional retry can still observe a later mutation of the live `SavedData` object, just as vanilla's delayed serializer can, but it remains visible to queue draining and directory-exclusive deletion leases. If the retry also throws, the ordered write fails and is logged rather than creating an untracked writer that could cross a deletion boundary.

The surrounding vanilla save method still runs normally, including SaveEvent, screenshots, popup behavior, and other Harmony patches.

## IM Data Core and Graduation Details compatibility

The career save transpiler runs at `Priority.Last` and declares `HarmonyAfter` for:

- `com.cosmo.savenloadfixes`
- `com.cosmo.imdatacore`
- `com.cosmo.graduationdetails`

The SavedData read and GlobalData write/read transpilers also declare `HarmonyAfter("com.cosmo.savenloadfixes")` so SNLF's embedded provider gets first ownership when present.

Both current mods patch the same concrete save callers and leave the final vanilla `DataSaver.saveData<SaveManager.SavedData>` call in place. Their sidecar/checkpoint preparation therefore runs first. Save Write Ordering Fix then replaces only that final write instruction.

No IM Data Core API or assembly reference is required.

### Verified interception health

Cooperating persistence mods can query:

```csharp
bool healthy = SaveWriteOrderingFix.SaveWriteOrderingApi.SavedDataInterceptionHealthy;
```

Standalone, the value is true only after all five required write callers were successfully intercepted exactly once. When authoritative SNLF is installed, the compatibility property forwards SNLF's effective health so existing IM Data Core versions do not interpret delegation as failure.

## Load-side coordination

Idol Manager also reads `SavedData` directly while:

- loading a selected save,
- resolving the latest autosave,
- building manual save lists,
- building story save/playthrough lists.

The preserved 1.3 read path replaces the concrete `DataSaver.loadData<SaveManager.SavedData>` instructions at all known vanilla caller sites with a wrapper that waits for an ordered write to the same physical file to finish, then calls vanilla `DataSaver.loadData<SaveManager.SavedData>`.

Again, `DataSaver<T>` itself is not Harmony-patched.

## Other Harmony mods

Mods that patch the concrete vanilla save/load callers continue to participate in the same Harmony methods.

Save Write Ordering Fix intentionally runs its caller transpilers last so ordinary caller-level prefixes, postfixes, and earlier transpilers can do their work first.

A mod that directly Harmony-patches a constructed reference-type `DataSaver<T>` specialization is outside the Mono-safe convention this mod follows. The game can still invoke such a patch because this mod calls vanilla `DataSaver.loadData<SavedData>` for reads and uses vanilla-compatible JSON for writes, but Save Write Ordering Fix does not depend on, reproduce, or encourage generic-method Harmony patching.

## Mods that directly edit vanilla save JSON

Globally patching `System.IO.File` would be invasive and could affect unrelated game/mod I/O, so this mod does not do that.

For a mod that directly reads/writes a physical vanilla save file, this assembly exposes the same API as before. In the both-installed configuration these calls transparently forward to authoritative SNLF so consumers coordinate with the queue that actually owns the write:

```csharp
SaveWriteOrderingFix.SaveWriteOrderingApi.TryWaitForPendingWrites(
    absoluteSavePath,
    30000,
    out errorMessage);
```

For read/modify/write operations:

```csharp
SaveWriteOrderingFix.SaveWriteOrderingApi.TryRunExclusiveFileAccess(
    absoluteSavePath,
    () =>
    {
        // Direct File I/O here.
    },
    30000,
    out errorMessage);
```

Later ordered vanilla writes to the same path wait until the exclusive operation finishes.

For directory deletion/archive operations, the preserved 1.3 API also exposes a lease that **atomically closes queue admission** beneath the directory, drains every ordered save queue that was admitted before the boundary, and keeps both existing and newly arriving writes blocked until the caller disposes it:

```csharp
IDisposable lease;
string errorMessage;
if (SaveWriteOrderingFix.SaveWriteOrderingApi.TryAcquireExclusiveDirectoryAccess(
        absoluteSaveDirectory,
        30000,
        out lease,
        out errorMessage))
{
    try
    {
        // Delete/archive the directory while earlier queued writes are drained.
    }
    finally
    {
        lease.Dispose();
    }
}
```

IM Data Core uses this boundary around vanilla save-directory deletion so neither an earlier queued SWOF writer nor a newly arriving write can recreate the directory while/after IMDC archives the matching supplemental sidecar. Queue admission and directory-lease registration share one coordinator lock, so a write is either admitted before the lease and drained, or admitted only after the lease is released.

## Build

From the Cosmo Mod Library root:

```powershell
dotnet build "mods\Save Write Ordering Fix\Save Write Ordering Fix.csproj" -c Release
```
