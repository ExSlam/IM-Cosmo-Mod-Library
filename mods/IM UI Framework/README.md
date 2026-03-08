# IM UI Framework (Intermediary Mod)

`IM UI Framework` is a reusable helper layer for Idol Manager modders.

It targets three common problems:
- Modifying existing UI safely.
- Adding new buttons that match game style.
- Building fully custom popups that still look and behave like base game UI.

## Included API

Namespace: `IMUiFramework`

Main class: `IMUiKit`

Core methods:
- `TryAddTopMenuButton(...)`
- `TryCreatePopupScaffold(...)`
- `TryRegisterPopup(...)`
- `CreateStagedVariablesState()`
- `BindStagedApplyCancelButtons(...)`
- `TryCreateSettingsSlider(...)`
- `TryCreateSettingsCheckbox(...)`
- `BindLanguageData(...)`
- `ResolveLanguageDataText(...)`
- `TrySyncBackdropWithActiveManagedPopups(...)`
- `TryRunPopupBackdropSafetyNet(...)`
- `CloneStyledButton(...)`
- `CreateStyledButton(...)`
- `CreateButtonFromTemplateOrStyle(...)`
- `TryAppendProfileExtra(...)`
- `CreateText(...)`
- `CreateLegacyText(...)`
- `SetText(...)`
- `ClearChildren(...)`
- `RebuildLayout(...)`
- `CreateVerticalLayoutContainer(...)`
- `CreateHorizontalLayoutContainer(...)`
- `CreateDivider(...)`
- `TryCreateProfileText(...)`
- `TryCreateProfileDivider(...)`
- `MeasurePreferredTextWidth(...)`
- `ConfigureButtonLayout(...)`
- `RebindAllButtons(...)`
- `ActivateButtonDefaults(...)`
- `ApplyLayerRecursively(...)`

`PopupScaffold` provides:
- `Root`
- `Popup`
- `PanelRect`
- `TitleText`
- `ContentRoot`
- `ScrollRect`
- `CloseButton`

Bridge class: `IMUiBridges`

Dedicated bridge/helper UI methods:
- `TryCreateBridgeShowcasePopup(...)`
- `TryCreateBridgeShowcaseContent(...)`
- `TryCreateCameraEffectsHelperPanel(...)`
- `TryCreateModernDropdownHelperPanel(...)`
- `TryCreateTooltipHelperPanel(...)`
- `TryCreateGradientPreviewHelperPanel(...)`

Low-level bridge methods:
- `EnsureCinematicBloom(...)`
- `EnsureCinematicLensAberrations(...)`
- `EnsureImageEffectsAntialiasing(...)`
- `EnsureImageEffectsBloom(...)`
- `TryCreateModernDropdown(...)`
- `TryCloneModernWindowManager(...)`
- `AddSoftMask(...)`
- `TryEnsureBoundTooltipItem(...)`
- `AddBoundTooltipTrigger(...)`
- `TryCreateLegacyToolTipWidget(...)`
- `AddLegacyToolTipTrigger(...)`
- `TryCreateHoverTooltipWidget(...)`
- `AddHoverTooltipTrigger(...)`
- `AddUiGradient(...)`
- `AddTwoColorUiGradient(...)`

Supported namespaces:
- `UnityStandardAssets.CinematicEffects`
- `UnityStandardAssets.ImageEffects`
- `Michsky.UI.ModernUIPack`
- `UnityEngine.UI.Extensions`
- `UnityEngine.UI.Michsky.UI.ModernUIPack`

## Minimal usage: add a top button + custom popup

```csharp
using HarmonyLib;
using IMUiFramework;
using TMPro;
using UnityEngine;

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class DemoPatch
{
    private static PopupScaffold scaffold;

    private static void Postfix(PopupManager __instance)
    {
        if (!IMUiKit.IsInitialized)
        {
            IMUiKit.Initialize(__instance);
        }

        GameObject button;
        if (IMUiKit.TryAddTopMenuButton(
            "MyDemoButton",
            "Demo",
            "Open demo popup",
            ToggleDemoPopup,
            out button))
        {
            // Button injected near Awards
        }

        if (scaffold == null)
        {
            PopupScaffold created;
            if (IMUiKit.TryCreatePopupScaffold("MyDemoPopup", "Demo Popup", new Vector2(860f, 520f), out created))
            {
                scaffold = created;
                IMUiKit.CreateText(scaffold.ContentRoot, "Body", "Hello from IMUiFramework.", 22, TextAlignmentOptions.Center, mainScript.black32);
                IMUiKit.RebuildLayout(scaffold.ContentRoot);
            }
        }
    }

    private static void ToggleDemoPopup()
    {
        if (scaffold == null || scaffold.Root == null)
        {
            return;
        }

        if (scaffold.Root.activeSelf)
        {
            scaffold.Hide();
        }
        else
        {
            scaffold.Show();
        }
    }
}
```

## Minimal usage: profile Extras line

```csharp
[HarmonyPatch(typeof(Profile_Popup), "RenderTab_Extras")]
internal static class ProfilePatch
{
    private static void Postfix(Profile_Popup __instance)
    {
        if (__instance == null || __instance.Girl == null)
        {
            return;
        }

        IMUiKit.TryAppendProfileExtra(__instance, "<color=#5274FF>Example:</color> Added by framework", true);
    }
}
```

## Minimal usage: staged settings controls (Apply/Cancel)

```csharp
using IMUiFramework;
using UnityEngine;
using UnityEngine.UI;

// Assume scaffold was created via TryCreatePopupScaffold(...)
StagedVariablesState stage = IMUiKit.CreateStagedVariablesState();

GameObject speedSlider;
IMUiKit.TryCreateSettingsSlider(
    scaffold.ContentRoot,
    "SpeedSlider",
    "MyMod_Speed",
    "MYMOD__SPEED",
    1f,
    20f,
    5f,
    stage,
    out speedSlider,
    true);

GameObject enabledCheckbox;
IMUiKit.TryCreateSettingsCheckbox(
    scaffold.ContentRoot,
    "EnabledCheckbox",
    "MyMod_Enabled",
    "MYMOD__ENABLED",
    true,
    stage,
    out enabledCheckbox);

Button apply = IMUiKit.CreateStyledButton(scaffold.PanelRect, "Apply", "APPLY", 140f, 36f, null);
Button cancel = IMUiKit.CreateStyledButton(scaffold.PanelRect, "Cancel", "CANCEL", 140f, 36f, null);

IMUiKit.BindStagedApplyCancelButtons(stage, apply, cancel, null, null, scaffold.Popup, true);
```

## Minimal usage: one-call bridge helper showcase popup

```csharp
using HarmonyLib;
using IMUiFramework;
using UnityEngine;

[HarmonyPatch(typeof(PopupManager), "Start")]
internal static class BridgeShowcasePatch
{
    private static PopupScaffold bridgeScaffold;

    private static void Postfix(PopupManager __instance)
    {
        if (!IMUiKit.IsInitialized)
        {
            IMUiKit.Initialize(__instance);
        }

        if (bridgeScaffold == null)
        {
            GameObject showcaseRoot;
            IMUiBridges.TryCreateBridgeShowcasePopup(
                "MyBridgeShowcasePopup",
                "UI Bridge Showcase",
                new Vector2(900f, 580f),
                Camera.main,
                out bridgeScaffold,
                out showcaseRoot);
        }
    }
}
```

## 1.0 API stability contract

- `IMUiKit`, `IMUiBridges`, `PopupScaffold`, `ToolTipTriggerBridge`, and `HoverTooltipTriggerBridge` public members are the supported API for `1.x`.
- Method names and signatures in that public surface are treated as stable across `1.x` patch/minor updates.
- Internal classes (`internal` visibility) are runtime implementation details and may change without notice.

## Notes

- This framework reads style templates from base game popups/buttons when possible.
- If templates are missing, it falls back to safe runtime-created controls.
- Popup close/hide patching now resolves `PopupManager.Close` overloads dynamically and reconciles stale queue/blur/backdrop state more aggressively.
- Runtime popup recovery can now be called directly through `TrySyncBackdropWithActiveManagedPopups(...)` and `TryRunPopupBackdropSafetyNet(...)`.
- The built-in `UI Bridge` helper/showcase button is controlled by `IMUiFramework.config.ini` (`enable_bridge_showcase`).
- Default release behavior keeps `enable_bridge_showcase=false` so dependency installs are non-intrusive.
- `HoverTooltip` requires a `GUICamera` object in scene.
- `ToolTip` works only for `ScreenSpaceCamera` canvases (UI Extensions behavior).
- It is intentionally utility-focused, not a forced architecture.

## Build

Project file:
- `mods/IM UI Framework/IM UI Framework.csproj`

Example command:
- `dotnet build "mods/IM UI Framework/IM UI Framework.csproj" -c Release`
