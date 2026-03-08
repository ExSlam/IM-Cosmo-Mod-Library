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

## Recipe 6: Read recent events for UI timeline

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

Ordering note:
- API returns newest-first ordering scoped to idol + global relevant events.

## Recipe 7: Flush before irreversible transitions

Use for transitions where you want persistence certainty:
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

## Recipe 8: Defensive shutdown

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

## Recipe 9: Retry-on-ready helper

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

## Recipe 10: Event naming conventions for long-term maintainability

Use predictable naming:
- `entityKind`: object type (`idol`, `contract`, `show`, `tour`)
- `eventType`: specific mutation (`contract_liability_applied`)
- `sourcePatch`: explicit provenance (`mod.<harmony_id>.<class>.<method>.Postfix`)

Benefits:
- Easier analytics queries
- Better debugging
- Safer cross-mod reasoning

## Recipe 11: JSON schema migration strategy

For custom JSON snapshots:
1. Store schema version in payload (`"schema":2`)
2. On load, migrate old versions to current in-memory model
3. Save back migrated payload with current schema

This avoids breaking old save histories when your mod evolves.

## Recipe 12: Error triage checklist

When an API call fails:
1. Verify `Session` is not null
2. Validate token format/length (`namespace`, `dataKey`, `entityKind`, `eventType`)
3. Check payload size/quota
4. Log `errorMessage` exactly
5. Retry after `TryFlushNow` if issue appears timing-related

## Anti-patterns to avoid

- Treating timeline events as mutable state source for every read path
  - Use snapshots for current state; events for history
- Writing directly into IM Data Core internal tables
  - Use API for compatibility
- Spamming registration calls every frame
  - Register once, reuse session
- Ignoring errors silently
  - At minimum, log with key/context

## Related docs

- `docs/START_HERE.md` for first integration
- `docs/NAMING_CONVENTIONS.md` for rename safety rules
- `docs/EVENT_CATALOG.md` for built-in event and payload constants
