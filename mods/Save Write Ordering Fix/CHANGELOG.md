# Changelog

## Unreleased - Harmony recomposition qualification

- Made all four SWOF transport transpilers recognize their own complete exact replacement call shapes when HarmonyX recomposes a caller after initial installation.
- Kept authoritative SNLF delegation first: a complete SNLF shape remains delegated success, while a complete standalone SWOF shape remains local success.
- Mixed SNLF/SWOF/vanilla, partial, duplicate, or signature-changed shapes still fail health checks instead of being double-wrapped or treated as authoritative.
- Both Debug and Release behavior retain caller-level interception only; no constructed `DataSaver<T>` method is patched.

## 1.4.5 - TX source/static qualification

- Added `Test-TransportCoexistenceSourceQualification.py` to cross-check SWOF, SNLF, and the supplied decompiled Idol Manager source.
- Source-qualified independent-install dependency contracts TX-08/TX-09, exact patch-shape health failure TX-15, and the no-constructed-generic rule TX-16.
- Locked the source-side one-owner/delegation wiring for TX-10 through TX-13 without falsely claiming the live concurrency/physical-write outcomes.
- Added an explicit TX source-vs-runtime status table to `docs/TEST_MATRIX.md`.
- No transport behavior changed; production source differs from dev.4 only by the public development version string.
- Final 1.4.0 remains gated on live Unity/Mono/Harmony transport and coexistence qualification.

## 1.4.0-dev.4 - documentation/source-guard closure

- Replaced the stale 1.2.0 `docs/TEST_MATRIX.md` with the cumulative 1.4 transport/coexistence matrix.
- Added the previously referenced `scripts/Test-PatchHealthSource.py` as a real source/IL-shape guard.
- The new guard asserts the exact five career writer methods, seven career reader methods / eight read sites, one GlobalData writer/read pair, no constructed-generic Harmony patch, four `Priority.Last` transport surfaces, SNLF delegation ordering, and effective-owner semantics.
- Added a documentation/source-guard contract to prevent the stale test-matrix/script drift from returning.
- No transport behavior changed in this increment; only the public development version constant changed in production source.
- Source/static qualification only. Final 1.4.0 still requires live SWOF-only and SNLF+SWOF Mono/Harmony transport qualification.

## 1.4.0-dev.3 - effective-owner API increment

- Added public `EffectiveTransportHealthy`, `TransportOwner`, and `IsAuthoritativeTransport`.
- Effective health follows authoritative SNLF when delegated and otherwise requires complete local SavedData + GlobalData interception.
- `TransportOwner` reports the actual physical transport owner; `IsAuthoritativeTransport` is false during SNLF delegation so SWOF never claims queue ownership it does not have.
- Preserved every dev.2 delegation path and every existing public coordination signature.
- Source/static qualification only; no live Mono/Harmony coexistence claim is made.

## 1.4.0-dev.2 - Save n Load Fixes delegation increment

- Cumulative over 1.4.0-dev.1 GlobalData transport; standalone career and GlobalData coordinator behavior remains present.
- Adds reflection-only discovery of `SaveNLoadFixes.SaveTransportApi` with no compile-time SNLF dependency.
- Requires SNLF transport API version 1+, authoritative status, and owner `com.cosmo.savenloadfixes` before runtime coordination delegation is enabled.
- Adds `HarmonyAfter("com.cosmo.savenloadfixes")` to all four SWOF transport transpilers.
- Recognizes SNLF's exact `OrderedSaveTransport` SavedData/GlobalData read/write replacement call shapes as delegated success and leaves those callers unchanged, preventing a second SWOF queue/read owner.
- Existing `SavedDataInterceptionHealthy` and `GlobalDataInterceptionHealthy` report SNLF effective health while SNLF is authoritative; standalone health behavior remains local.
- Existing `HasPendingWrites`, wait, exclusive-file, and exclusive-directory APIs forward to SNLF while it is authoritative and otherwise retain the local 1.3/GlobalData coordinator.
- Effective-owner public API expansion and stale `docs/TEST_MATRIX.md` cleanup remain later 1.4 cumulative changes.
- Source/static qualification only; no project build or live Mono/Harmony coexistence claim is made.

## 1.4.0-dev.1 - GlobalData transport increment

- Preserves all SWOF 1.3.0 career `SavedData` ordering, read fences, per-path FIFO behavior, caller-thread payload freezing, file exclusivity, directory admission fences, and public API signatures.
- Adds concrete caller-level interception for `SaveManager.SaveGlobalData()` and `SaveManager.LoadGlobalData()` without Harmony-patching constructed `DataSaver<T>` methods.
- Freezes populated `SaveManager.GlobalData` on the caller thread after `SaveGlobalDataEvent`, then queues `global_data.json` through the existing per-path coordinator.
- Coordinates `LoadGlobalData()` against pending writes to the same physical `global_data.json` before invoking vanilla `DataSaver.loadData<GlobalData>`.
- Adds `SaveWriteOrderingApi.GlobalDataInterceptionHealthy`, requiring exactly one successful GlobalData writer and one successful GlobalData reader interception.
- Makes the exceptional deferred serialization payload internal request type payload-agnostic so SavedData and GlobalData share the same tracked writer fallback.
- This is a development 1.4 cumulative increment only; SNLF delegation/effective-owner behavior is intentionally not implemented yet.
- Source/static qualification only; no project build or live Mono/Harmony claim is made.

## 1.3.0

- Added `SaveWriteOrderingApi.TryAcquireExclusiveDirectoryAccess`, returning an `IDisposable` lease that atomically blocks new queue admission beneath a physical save directory, drains every queue admitted before the boundary, and keeps later writes blocked until disposal.
- IM Data Core can now hold that directory boundary across vanilla deletion and supplemental archival, preventing both earlier queued writers and newly arriving writes from recreating the deleted save while the sidecar is detached.
- A caller-thread serialization failure after path resolution now remains coordinator-tracked: SWOF retries serialization on the ordered writer instead of launching vanilla's untracked asynchronous fallback, so directory leases can still drain/fence the request.
- The lease API is optional for ordinary save/load callers; per-file FIFO ordering, load coordination, and the existing exclusive-file APIs are unchanged.

## 1.2.0

- Added `SaveWriteOrderingApi.SavedDataInterceptionHealthy`, which becomes true only after all five required vanilla `SavedData` write callers were successfully transpiled exactly once.
- Explicitly added the documented `HarmonyAfter` ordering for IM Data Core and Graduation Details alongside `Priority.Last`.
- Cooperating mods can now distinguish "assembly loaded" from "write interception is actually healthy" and retain their defensive fallback if a game/mod update prevents one caller patch.
- The ordered writer, per-path FIFO behavior, load-side coordination, and public exclusive-file APIs are otherwise unchanged from 1.1.0.

## 1.1.0

- Rebuilt the mod around Idol Manager's concrete vanilla save/load callers.
- Removed all Harmony patches on constructed `DataSaver<T>` methods.
- Eliminated the Mono generic-sharing failure mode that could affect `GlobalData`.
- Replaced the five known concrete vanilla `SavedData` write calls with a per-path FIFO writer.
- Freezes JSON at save-request time so delayed writes cannot observe later `SaveManager.Data` mutations.
- Added concrete read coordination for every known vanilla `SavedData` read site:
  - actual SaveManager loads,
  - latest-autosave inspection,
  - manual save-list reads,
  - story save/playthrough reads.
- Runs after IM Data Core and Graduation Details caller-level persistence transpilers.
- Retains a cooperative API for mods that directly touch vanilla save JSON.

## 1.0.1

- Compile-only ambiguity fixes.
- Superseded because the constructed-generic Harmony architecture was unsafe on Mono.

## 1.0.0

- Initial implementation.
- Superseded.
