# Save Write Ordering Fix

**Version 1.1.0**

Fixes a vanilla bug where rapidly saving to the same slot can let an older save finish later and overwrite the newer save.

## Important: 1.1.0 replaces the 1.0.x implementation

Do not use 1.0.0 or 1.0.1.

Those versions Harmony-patched constructed `DataSaver<T>` methods. That is unsafe on Idol Manager's Mono runtime because reference-type generic instantiations may share runtime code. In practice, a `SavedData` patch can interfere with other `DataSaver<T>` uses such as `GlobalData`.

Version 1.1.0 contains **no Harmony patch on `DataSaver<T>`**.

## How 1.1.0 works

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

The surrounding vanilla save method still runs normally, including SaveEvent, screenshots, popup behavior, and other Harmony patches.

## IM Data Core and Graduation Details compatibility

The save transpiler runs at `Priority.Last` and declares `HarmonyAfter` for:

- `com.cosmo.imdatacore`
- `com.cosmo.graduationdetails`

Both current mods patch the same concrete save callers and leave the final vanilla `DataSaver.saveData<SaveManager.SavedData>` call in place. Their sidecar/checkpoint preparation therefore runs first. Save Write Ordering Fix then replaces only that final write instruction.

No IM Data Core API or assembly reference is required.

## Load-side coordination

Idol Manager also reads `SavedData` directly while:

- loading a selected save,
- resolving the latest autosave,
- building manual save lists,
- building story save/playthrough lists.

Version 1.1.0 replaces the concrete `DataSaver.loadData<SaveManager.SavedData>` instructions at all known vanilla caller sites with a wrapper that waits for an ordered write to the same physical file to finish, then calls vanilla `DataSaver.loadData<SaveManager.SavedData>`.

Again, `DataSaver<T>` itself is not Harmony-patched.

## Other Harmony mods

Mods that patch the concrete vanilla save/load callers continue to participate in the same Harmony methods.

Save Write Ordering Fix intentionally runs its caller transpilers last so ordinary caller-level prefixes, postfixes, and earlier transpilers can do their work first.

A mod that directly Harmony-patches a constructed reference-type `DataSaver<T>` specialization is outside the Mono-safe convention this mod follows. The game can still invoke such a patch because this mod calls vanilla `DataSaver.loadData<SavedData>` for reads and uses vanilla-compatible JSON for writes, but Save Write Ordering Fix does not depend on, reproduce, or encourage generic-method Harmony patching.

## Mods that directly edit vanilla save JSON

Globally patching `System.IO.File` would be invasive and could affect unrelated game/mod I/O, so this mod does not do that.

For a mod that directly reads/writes a physical vanilla save file, this assembly exposes:

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

## Build

Place the project at:

`mods/Save Write Ordering Fix/`

Then add it to the solution if needed:

```powershell
dotnet sln "Cosmo Mod Library.sln" add "mods\Save Write Ordering Fix\Save Write Ordering Fix.csproj"
```

Build:

```powershell
dotnet build "Cosmo Mod Library.sln" -c Release
```
