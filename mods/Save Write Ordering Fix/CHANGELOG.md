# Changelog

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
