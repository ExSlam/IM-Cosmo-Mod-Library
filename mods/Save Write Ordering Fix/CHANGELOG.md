# Changelog

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
