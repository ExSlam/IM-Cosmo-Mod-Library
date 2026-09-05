# IM Data Core - Start Here (Beginner-Friendly)

This guide explains exactly how to use IM Data Core 3.4.24 from another Idol Manager mod, even if you are new to Harmony and mod persistence. Public JSON arguments must be valid JSON documents; IMDC normalizes them before they enter history.

IM Data Core stores each sidecar under a mirrored representation of its exact
vanilla save path. This development build accepts sidecar format 5 only. For
path mapping, checkpoint identity, deleted-save archival, and journal details,
see [`STORAGE_LAYOUT.md`](STORAGE_LAYOUT.md).

## What you are building

You will build a mod integration that can:

1. Register with IM Data Core
2. Save custom JSON state
3. Read custom JSON state
4. Append custom timeline events

## Prerequisites

- Idol Manager is installed
- BepInEx modding environment is working
- IM Data Core mod folder exists:
  - `...\Cosmo-Mod-Library\mods\IM Data Core`
- You can build your own mod DLL

If you are new to Harmony:

- Harmony patches let your code run before or after a game method.
- A `Postfix` runs after the original method.
- You can call IM Data Core API methods from inside those callbacks.

## Step 1: Add the IM Data Core DLL reference

Add this reference to your mod `.csproj`:

```xml
<ItemGroup>
  <Reference Include="com.cosmo.imdatacore">
    <HintPath>..\..\path\to\com.cosmo.imdatacore.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

Why this matters:

- Without this reference, your mod cannot call `IMDataCoreApi`.

## Step 2: Create shared bridge state

Create a static class in your mod to hold one session:

```csharp
using IMDataCore;

internal static class DataCoreBridge
{
    internal static IMDataCoreSession Session;
    internal const string NamespaceId = "com.example.your_mod";
}
```

Why one shared session:

- Registration is namespace-scoped and assembly-bound.
- In live 3.4.24 v5/v2 persistence, this authorization is still process-local. The staged sidecar-v6 model adds durable document-level owner provenance, but it is intentionally not the normal runtime authority until a later deliberate v6/v3 runtime cutover.
- Do not treat checkpoint `EnabledMods` or being first to register after restart as proof of historical namespace ownership. Migrated v1-v5 namespaces remain explicitly legacy-unbound until a separate adoption policy authorizes a durable owner.
- Reusing one session avoids duplicate registration logic.

## Step 3: Register directly at a safe gameplay point

Patch a late game point such as `PopupManager.Start` and register once:

```csharp
using HarmonyLib;
using IMDataCore;
using UnityEngine;

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class PopupManager_Start_YourModInit_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (DataCoreBridge.Session != null)
        {
            return;
        }

        string error;
        if (!IMDataCoreApi.TryRegisterNamespace(DataCoreBridge.NamespaceId, out DataCoreBridge.Session, out error))
        {
            Debug.LogWarning("[YourMod] Namespace registration failed: " + error);
        }
    }
}
```

Important:

- `NamespaceId` must be unique and token-safe.
- Recommended format: reverse-domain (`com.author.modname`).
- `TryRegisterNamespace(...)` owns safe IM Data Core initialization under the runtime lock. Call it directly at your chosen safe gameplay point.
- `IsReady()` is observational only. Do not use it as a one-shot prerequisite for registration, because Harmony postfix ordering against another mod is not guaranteed.
- If registration fails, keep the error visible and retry from a bounded later safe point rather than permanently treating a null session as absence.

## Optional: resolve current durable history identity

Version 3.4.24 exposes a read-only #66 resolver for generation-scoped identities. It accepts stable locator descriptors, not game CLR object references.

```csharp
IMDataCoreIdentityResolution identity;
string identityError;
if (IMDataCoreApi.TryResolveCurrentIdentity(
        "agency_room",
        "floor=0;room=1",
        out identity,
        out identityError) &&
    identity.Quality == IMDataCoreIdentityResolutionQuality.Exact)
{
    string canonicalRoomId = identity.CanonicalEntityId;
}
```

Supported locator shapes are documented in `IMDC_WAVE1_TASK6.md`. Legacy coarse keys use `TryResolveLegacyIdentityCandidates`; always inspect `Quality` and the full `CanonicalEntityIds` list. `Ambiguous` is a successful compatibility answer, not an error to be guessed away. On live v5, opaque contract/clique/bullying/task generations and the durable candidate multimap remain `Unresolved` until v6 persistence can preserve them across restart.

## Step 4: Save custom JSON

Use `TrySetCustomJson` for current-state snapshots or indexes:

```csharp
internal static void SaveIdolSnapshot(int idolId, string payloadJson)
{
    if (DataCoreBridge.Session == null)
    {
        return;
    }

    string key = "idol_" + idolId + "_snapshot";
    string error;
    if (!IMDataCoreApi.TrySetCustomJson(DataCoreBridge.Session, key, payloadJson, out error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TrySetCustomJson failed: " + error);
    }
}
```

Example payload:

```json
{"mood":"focused","training_level":3}
```

## Step 5: Read custom JSON

```csharp
internal static bool TryLoadIdolSnapshot(int idolId, out string json)
{
    json = string.Empty;

    if (DataCoreBridge.Session == null)
    {
        return false;
    }

    string key = "idol_" + idolId + "_snapshot";
    string error;
    if (!IMDataCoreApi.TryGetCustomJson(DataCoreBridge.Session, key, out json, out error))
    {
        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogWarning("[YourMod] TryGetCustomJson failed: " + error);
        }

        return false;
    }

    return true;
}
```

Interpretation tip:

- `false` + empty error usually means key not found.

## Step 6: Append a custom timeline event

Use events for immutable history:

```csharp
internal static void AppendPromotionEvent(int idolId, int fanGain)
{
    if (DataCoreBridge.Session == null)
    {
        return;
    }

    string payload = "{\"fan_gain\":" + fanGain + "}";
    string error;
    if (!IMDataCoreApi.TryAppendCustomEvent(
        DataCoreBridge.Session,
        idolId,
        "idol",
        idolId.ToString(),
        "promotion_bonus_applied",
        payload,
        "mod.com.example.your_mod.PromotionPatch.Postfix",
        out error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TryAppendCustomEvent failed: " + error);
    }
}
```

When to use this:

- You care about historical sequence, not only latest state.

### Step 6A: Make replay-prone callbacks idempotent

Use `TryAppendCustomEventOnce` when the same logical occurrence might be observed more than once because of load reconstruction, retries, or overlapping hooks. Supply a stable occurrence key containing only letters, digits, `_`, `-`, or `.` and no more than 192 characters.

```csharp
internal static void AppendPromotionEventOnce(int idolId, long promotionOccurrenceId, int fanGain)
{
    if (DataCoreBridge.Session == null)
    {
        return;
    }

    string idempotencyKey =
        "promotion." + idolId + "." + promotionOccurrenceId;
    string payload = "{\"fan_gain\":" + fanGain + "}";
    string error;
    if (!IMDataCoreApi.TryAppendCustomEventOnce(
        DataCoreBridge.Session,
        idempotencyKey,
        idolId,
        "idol",
        idolId.ToString(),
        "promotion_bonus_applied",
        payload,
        "mod.com.example.your_mod.PromotionPatch.Postfix",
        out error))
    {
        UnityEngine.Debug.LogWarning(
            "[YourMod] TryAppendCustomEventOnce failed: " + error);
    }
}
```

The key is scoped to your registered namespace and persisted with the event. Repeating the same key on the same active branch returns success without another row. Loading an exact checkpoint from before that event rewinds the key along with the event, so a later legitimate occurrence can be recorded.

Choose a key for one occurrence, not one event type. `promotion` is too broad if promotions can happen more than once.

## Step 7: Read recent events

```csharp
using System.Collections.Generic;
using IMDataCore;

internal static List<IMDataCoreEvent> ReadRecentEvents(int idolId, int maxCount)
{
    List<IMDataCoreEvent> events;
    string error;

    if (!IMDataCoreApi.TryReadRecentEventsForIdol(idolId, maxCount, out events, out error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TryReadRecentEventsForIdol failed: " + error);
        return new List<IMDataCoreEvent>();
    }

    return events;
}
```

### Read complete career-wide history in pages

For a complete durable history browser, walk the canonical #67 paginator. It returns each retained physical occurrence once and does not require a live idol ID.

```csharp
internal static List<IMDataCoreEvent> ReadAllHistory()
{
    List<IMDataCoreEvent> all = new List<IMDataCoreEvent>();
    long before = 0L;
    bool hasMore;

    do
    {
        List<IMDataCoreEvent> page;
        string error;
        if (!IMDataCoreApi.TryReadHistoryPage(
                before,
                500,
                out page,
                out hasMore,
                out error))
        {
            UnityEngine.Debug.LogWarning(
                "[YourMod] TryReadHistoryPage failed: " + error);
            break;
        }

        if (page.Count == 0)
        {
            break;
        }

        all.AddRange(page);
        before = page[page.Count - 1].EventId;
    }
    while (hasMore);

    return all;
}
```

`TryReadHistoryPage(...)` is newest-to-oldest by durable shared sequence. Always continue with the `EventId` of the **last row returned**. If F9/load discarded that cursor's branch, the call fails closed and the consumer should restart from `beforeEventIdExclusive <= 0` for the newly selected branch.

`TryReadEventsForIdolPage(...)` remains available when you specifically want one known idol's participant-expanded compatibility view. Its ordering remains `(GameDateKey, Sequence)` and its cursor should likewise be the last row returned.

Row absence is not a completeness proof. Use the structured coverage APIs when you need to distinguish Complete, Partial, and Unknown history.

## Step 8: Optional explicit flush

If your mod needs its current IMDC branch persisted before the next vanilla
save, and a physical vanilla save scope already exists:

```csharp
internal static void FlushNow()
{
    string error;
    if (!IMDataCoreApi.TryFlushNow(out error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TryFlushNow failed: " + error);
    }
}
```

`TryFlushNow` writes only the IMDC sidecar. It does not trigger or modify a
vanilla save, and it returns a clean failure before any physical vanilla save
scope exists. If IMDC has just adopted an existing vanilla career that had no
sidecar, the load seeds an in-memory sequence-0 exact checkpoint first, so an
explicit flush cannot create an unanchored sidecar.

## Step 9: Optional shutdown cleanup

```csharp
internal static void Shutdown()
{
    if (DataCoreBridge.Session == null)
    {
        return;
    }

    string error;
    IMDataCoreApi.TryUnregisterNamespace(DataCoreBridge.Session, out error);
    DataCoreBridge.Session = null;
}
```

## Save deletion behavior

When the player deletes a vanilla save through the supported vanilla UI, IMDC preserves the matching supplemental directory by renaming it with an `OLD` suffix rather than deleting it. Name collisions use `OLD2`, `OLD3`, and so on. This is automatic; consumer mods should not rename or delete IMDC sidecars themselves.

## Common mistakes and fixes

- Registration fails with namespace already claimed:
  - Use a unique namespace value.
- Event append fails with invalid token:
  - Check `entityKind` and `eventType` characters/length.
- Reads seem stale:
  - Check the session and returned error. Reads already include in-memory
    mutations; no disk flush is required first.
- `IsReady()` is false at an early observation point:
  - That does not block registration. Call `TryRegisterNamespace(...)` at your chosen safe gameplay point; the registration API owns initialization.

## What to read next

- [`COOKBOOK.md`](COOKBOOK.md): deeper patterns and production-grade usage
- [`NAMING_CONVENTIONS.md`](NAMING_CONVENTIONS.md): rename safety and contract boundaries
- [`EVENT_CATALOG.md`](EVENT_CATALOG.md): built-in event names and payload fields
