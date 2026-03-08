# IM Data Core Naming Conventions and Rename Safety

This guide explains which names are implementation detail and which names are API contracts.

If you are unsure whether a rename is safe, use this document before refactoring.

## Why this matters

Mod integrations break most often because one of these changes happened unintentionally:
- API method name changed
- Harmony patch target symbol changed
- Required token format violated

This guide helps prevent those breakages.

## Core rule

- You can freely rename your own code symbols.
- You must not rename IM Data Core public API symbols.
- You must not rename game method targets in Harmony attributes unless you intentionally retarget patches.

## Rename matrix

| Item | Can rename? | Details |
| --- | --- | --- |
| Local variables (`error`, `json`) | Yes | Purely local implementation detail. |
| Your helper methods/classes/files | Yes | Update call sites and project references. |
| Your mod C# namespace | Yes | Safe if your own code updates consistently. |
| `NamespaceIdentifier` string value | Yes (recommended) | Must remain unique and pass token rules. |
| `dataKey`, `entityKind`, `eventType`, `sourcePatch` values | Yes | Must follow token/length rules. |
| `IMDataCoreApi` type and method names | No | Public contract used by consuming mods. |
| `IMDataCoreSession`, `IMDataCoreEvent` type names | No | Public contract. |
| Harmony patch target symbols in `[HarmonyPatch(...)]` | No (unless intentional retarget) | Must match base-game symbols. |
| Prefix/Postfix callback method names without method attributes | No | Convention names must be exact (`Prefix`, `Postfix`). |
| Prefix/Postfix callback method names with `[HarmonyPrefix]` / `[HarmonyPostfix]` | Yes | Attributes bind callback role, name becomes arbitrary. |

## Token rules (what string values must satisfy)

Allowed characters:
- `a-z`, `A-Z`, `0-9`, `_`, `-`, `.`

Length rules:
- Namespace: `3..64`
- Data key: `1..128`
- Entity kind: `1..64`
- Event type: `1..64`
- Source patch: sanitized, max `128`

Practical recommendation:
- Use lowercase snake_case for most keys and event names.
- Use reverse-domain namespace IDs (`com.author.mod`).

## Safe Harmony rename patterns

### Pattern A: Keep convention callbacks

```csharp
[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class PopupManager_Start_MyPatch
{
    private static void Postfix()
    {
        // logic
    }
}
```

Here `Postfix` should remain named `Postfix` unless you add an explicit method attribute.

### Pattern B: Explicit method attributes (rename-safe callback names)

```csharp
[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class PopupManager_Start_MyPatch
{
    [HarmonyPostfix]
    private static void InitializeDataCoreAfterPopupStart()
    {
        // logic
    }
}
```

Here callback method name is flexible.

## Common rename mistakes and their impact

- Renaming `IMDataCoreApi.TrySetCustomJson` call to a non-existent method:
  - Compile-time error in consuming mod.
- Changing `[HarmonyPatch(typeof(X), "MethodName")]` target string:
  - Patch no longer applies, behavior silently missing.
- Using spaces or slashes in `eventType`:
  - Event append rejected by token validation.

## Recommended naming style for long-lived mods

- Namespace: `com.author.modname`
- Data keys: `entity_<id>_snapshot_v1`
- Event type: `<domain>_<action>_<phase>`
- Source patch: `mod.<harmony_id>.<class>.<method>.<prefix_or_postfix>`

Examples:
- `idol_42_snapshot_v2`
- `contract_liability_applied`
- `mod.com.example.my_mod.ContractPatch.Apply.Postfix`

## Refactor checklist

Before renaming:
1. Identify whether symbol is contract or implementation detail.
2. For Harmony callbacks, verify whether role is convention- or attribute-bound.
3. Validate string token values against allowed format.

After renaming:
1. Build mod project.
2. Confirm Harmony patch still applies at runtime.
3. Verify IM Data Core calls still succeed.

## Quick decision tree

- Is this your own helper symbol? -> Rename freely.
- Is this an IM Data Core public API symbol? -> Do not rename.
- Is this a Harmony target symbol? -> Rename only if intentionally retargeting.
- Is this a string token sent to IM Data Core? -> Rename allowed, but validate token rules.
