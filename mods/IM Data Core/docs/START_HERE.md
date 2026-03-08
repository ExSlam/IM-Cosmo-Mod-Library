# IM Data Core - Start Here (Beginner-Friendly)

This guide explains exactly how to use IM Data Core from another Idol Manager mod, even if you are new to Harmony and mod persistence.

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
- Reusing one session avoids duplicate registration logic.

## Step 3: Initialize when IM Data Core is ready

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

        if (!IMDataCoreApi.IsReady())
        {
            Debug.Log("[YourMod] IM Data Core not ready yet.");
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

## Step 8: Optional explicit flush

If your mod is about to trigger a risky transition and you want immediate persistence:

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

## Common mistakes and fixes

- Registration fails with namespace already claimed:
  - Use a unique namespace value.
- Event append fails with invalid token:
  - Check `entityKind` and `eventType` characters/length.
- Reads seem stale:
  - Call `TryFlushNow` before reading.
- `IsReady()` never true at early startup:
  - Initialize later in lifecycle.

## What to read next

- `docs/COOKBOOK.md`: deeper patterns and production-grade usage
- `docs/NAMING_CONVENTIONS.md`: rename safety and contract boundaries
- `docs/EVENT_CATALOG.md`: built-in event names and payload fields
