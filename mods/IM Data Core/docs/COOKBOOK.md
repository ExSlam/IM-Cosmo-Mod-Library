# IM Data Core Cookbook

This cookbook contains practical integration patterns with enough detail to use in production mods.

Unlike the quick-start, each recipe explains why the pattern is useful and what failure cases to handle.

## Recipe 1: Safe one-time initialization

Use this when your mod loads into gameplay and you want exactly one namespace session.

```csharp
using HarmonyLib;
using IMDataCore;
using UnityEngine;

internal static class DataCoreState
{
    internal const string NamespaceId = "com.example.your_mod";
    internal static IMDataCoreSession Session;
    internal static bool RegistrationAttempted;

    internal static void TryInitialize()
    {
        if (Session != null)
        {
            return;
        }

        if (!IMDataCoreApi.IsReady())
        {
            return;
        }

        string error;
        if (!IMDataCoreApi.TryRegisterNamespace(NamespaceId, out Session, out error))
        {
            if (!RegistrationAttempted)
            {
                Debug.LogWarning("[YourMod] Data Core registration failed: " + error);
            }

            RegistrationAttempted = true;
            return;
        }

        RegistrationAttempted = true;
        Debug.Log("[YourMod] Data Core registration succeeded.");
    }
}

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class PopupManager_Start_DataCoreInit_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        DataCoreState.TryInitialize();
    }
}
```

Why this helps:

- Avoids duplicate namespace registration attempts.
- Avoids log spam when readiness is delayed.

## Recipe 2: Stable custom data key design

A good key strategy prevents collisions and makes debugging easier.

Recommended pattern:

- Per-entity: `idol_<id>_snapshot`
- Per-feature: `feature_<name>_state`
- Indexed collections: `index_<name>_v1`

Example:

```csharp
internal static string BuildIdolSnapshotKey(int idolId)
{
    return "idol_" + idolId + "_snapshot";
}
```

Versioning tip:

- Include version suffix if schema evolves (`_v2`).

## Recipe 3: Save snapshot with explicit error handling

```csharp
internal static bool TrySaveSnapshot(string dataKey, string payloadJson)
{
    if (DataCoreState.Session == null)
    {
        return false;
    }

    string error;
    bool ok = IMDataCoreApi.TrySetCustomJson(DataCoreState.Session, dataKey, payloadJson, out error);
    if (!ok)
    {
        UnityEngine.Debug.LogWarning("[YourMod] TrySetCustomJson failed for key '" + dataKey + "': " + error);
    }

    return ok;
}
```

Failure classes to expect:

- Invalid key token format
- Namespace quota exceeded
- Value too large
- Session invalidated

## Recipe 4: Read snapshot with graceful fallback

```csharp
internal static string LoadSnapshotOrDefault(string dataKey, string defaultJson)
{
    if (DataCoreState.Session == null)
    {
        return defaultJson;
    }

    string json;
    string error;
    if (!IMDataCoreApi.TryGetCustomJson(DataCoreState.Session, dataKey, out json, out error))
    {
        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogWarning("[YourMod] TryGetCustomJson failed for key '" + dataKey + "': " + error);
        }

        return defaultJson;
    }

    return json;
}
```

Why this helps:

- Keeps game flow stable when data is missing/corrupt.
- Avoids null handling complexity in higher layers.

## Recipe 5: Append immutable events + maintain mutable snapshot

This is the recommended dual-write model:

- Event log for history
- Snapshot for latest state

```csharp
internal static void RecordPromotionAndUpdateCache(int idolId, int fanGain, string cacheJson)
{
    if (DataCoreState.Session == null)
    {
        return;
    }

    string payloadJson = "{\"fan_gain\":" + fanGain + "}";

    string eventError;
    if (!IMDataCoreApi.TryAppendCustomEvent(
        DataCoreState.Session,
        idolId,
        "idol",
        idolId.ToString(),
        "promotion_bonus_applied",
        payloadJson,
        "mod.com.example.your_mod.PromotionPatch.Postfix",
        out eventError))
    {
        UnityEngine.Debug.LogWarning("[YourMod] Event append failed: " + eventError);
    }

    string key = "idol_" + idolId + "_snapshot";
    string dataError;
    if (!IMDataCoreApi.TrySetCustomJson(DataCoreState.Session, key, cacheJson, out dataError))
    {
        UnityEngine.Debug.LogWarning("[YourMod] Snapshot save failed: " + dataError);
    }
}
```

## Recipe 6: Idempotent event append for load/retry callbacks

Use caller-controlled idempotency when one logical occurrence can be observed more than once. Do not deduplicate by payload equality.

```csharp
internal static void RecordSceneCompletionOnce(
    int idolId,
    string sceneId,
    long occurrenceTicks)
{
    if (DataCoreState.Session == null)
    {
        return;
    }

    string idempotencyKey =
        "scene." + idolId + "." + sceneId + "." + occurrenceTicks;
    string payloadJson = "{\"scene_id\":\"" + sceneId + "\"}";
    string error;

    if (!IMDataCoreApi.TryAppendCustomEventOnce(
        DataCoreState.Session,
        idempotencyKey,
        idolId,
        "substory",
        sceneId,
        "scene_completed",
        payloadJson,
        "mod.com.example.your_mod.ScenePatch.Postfix",
        out error))
    {
        UnityEngine.Debug.LogWarning(
            "[YourMod] Idempotent event append failed: " + error);
    }
}
```

Semantics:

- identity is registered namespace + `idempotencyKey`;
- a repeated key on the active branch is a successful no-op;
- the key is persisted with the event, so dedupe survives reload;
- rewinding to an exact checkpoint before the event removes it from active history, so the occurrence can happen again after the rewind;
- use only letters, digits, `_`, `-`, and `.` in the key, with a maximum of 192 characters.

## Recipe 7: Read recent events for UI timeline

```csharp
using System.Collections.Generic;
using IMDataCore;

internal static List<IMDataCoreEvent> GetTimelineRows(int idolId)
{
    List<IMDataCoreEvent> events;
    string error;

    if (!IMDataCoreApi.TryReadRecentEventsForIdol(idolId, 200, out events, out error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TryReadRecentEventsForIdol failed: " + error);
        return new List<IMDataCoreEvent>();
    }

    return events;
}
```

For a complete career/history browser, prefer `TryReadEventsForIdolPage` and pass `page[page.Count - 1].EventId` as the exclusive cursor for the next page. This avoids a hard dependency on any single recent-event request size while preserving newest-to-oldest ordering.

Ordering note:

- API returns newest-first ordering scoped to idol + global relevant events.

## Recipe 8: Flush before irreversible transitions

Use when a physical vanilla save scope already exists and you need the current
IMDC branch on disk before the next vanilla save:

- major scene unloads
- external export workflows
- manual "save now" UI in your mod

```csharp
internal static void FlushWithLog()
{
    string error;
    if (!IMDataCoreApi.TryFlushNow(out error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TryFlushNow failed: " + error);
    }
}
```

This writes only IMDC's sidecar. It neither invokes nor modifies vanilla save
handling, and it fails cleanly when no physical vanilla save scope exists. If
IMDC has just adopted an existing vanilla career with no sidecar, the load has
already seeded a sequence-0 exact checkpoint in memory, so this explicit flush
cannot create an unanchored sidecar.

## Recipe 9: Defensive shutdown

```csharp
internal static void DisposeDataCoreSession()
{
    if (DataCoreState.Session == null)
    {
        return;
    }

    string error;
    IMDataCoreApi.TryUnregisterNamespace(DataCoreState.Session, out error);
    if (!string.IsNullOrEmpty(error))
    {
        UnityEngine.Debug.LogWarning("[YourMod] TryUnregisterNamespace warning: " + error);
    }

    DataCoreState.Session = null;
}
```

## Recipe 10: Retry-on-ready helper

If your mod loads before IM Data Core is ready, call a polling helper from update/tick hooks until session exists.

```csharp
internal static void EnsureDataCoreReady()
{
    if (DataCoreState.Session != null)
    {
        return;
    }

    DataCoreState.TryInitialize();
}
```

## Recipe 11: Event naming conventions for long-term maintainability

Use predictable naming:

- `entityKind`: object type (`idol`, `contract`, `show`, `tour`)
- `eventType`: specific mutation (`contract_liability_applied`)
- `sourcePatch`: explicit provenance (`mod.<harmony_id>.<class>.<method>.Postfix`)

Benefits:

- Easier analytics queries
- Better debugging
- Safer cross-mod reasoning

## Deleted-save archives

Consumer mods do not need to intercept save deletion. IMDC patches the vanilla deletion paths itself and preserves the mirrored supplemental directory by renaming it to `<name>OLD`, then `<name>OLD2`, and so on if archives already exist. Whole story-playthrough deletion preserves the entire mirrored playthrough tree.

Do not treat an `OLD` directory as the active save scope. It is retained historical material intended for later export/recovery tooling. If archival fails, IMDC leaves the directory untouched and blocks writes back into that deleted scope for the remainder of the process.

## Recipe 12: JSON schema migration strategy

For custom JSON snapshots:

1. Store schema version in payload (`"schema":2`)
2. On load, migrate old versions to current in-memory model
3. Save back migrated payload with current schema

This avoids breaking old save histories when your mod evolves.

## Recipe 13: Error triage checklist

When an API call fails:

1. Verify `Session` is not null
2. Validate token format/length (`namespace`, `dataKey`, `entityKind`, `eventType`)
3. Check payload size/quota
4. Log `errorMessage` exactly
5. Do not use `TryFlushNow` as an API retry mechanism; reads already include
   in-memory mutations. Preserve and diagnose the original error.

## Anti-patterns to avoid

- Treating timeline events as mutable state source for every read path
  - Use snapshots for current state; events for history
- Writing directly into the IM Data Core sidecar
  - Use API for compatibility
- Spamming registration calls every frame
  - Register once, reuse session
- Ignoring errors silently
  - At minimum, log with key/context

## Related docs

- `docs/START_HERE.md` for first integration
- `docs/NAMING_CONVENTIONS.md` for rename safety rules
- `docs/EVENT_CATALOG.md` for built-in event and payload constants
- `docs/V5_SIDECAR_SCHEMA.md` for the current private v5 disk schema
- `docs/STORAGE_LAYOUT.md` for exact checkpoint identity, journaling, and deleted-save archives
