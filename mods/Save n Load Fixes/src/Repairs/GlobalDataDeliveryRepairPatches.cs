using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Observe each concrete GlobalData read around vanilla event delivery. Ordered file transport may replace the concrete read call inside this same method, but its
    /// method-level lifecycle and event invocation remain vanilla-owned.
    /// </summary>
    [HarmonyPatch]
    internal static class GlobalDataDeliveryLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(SaveManager), nameof(SaveManager.LoadGlobalData), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                GlobalDataDeliveryPatchHealth.ReportFailure("SaveManager.LoadGlobalData() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, nameof(SaveManager.LoadGlobalData));
            }

            GlobalDataDeliveryPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(out long __state)
        {
            __state = GlobalDataDeliveryRepair.BeginLoadAttempt();
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance, long __state)
        {
            GlobalDataDeliveryRepair.CompleteLoadAttempt(__instance, __state);
        }
    }

    /// <summary>
    /// staticVars.Awake subscribes LoadSettings inside the original body. Postfix therefore is
    /// the earliest source-local boundary at which the normal consumer is definitely ready.
    /// </summary>
    [HarmonyPatch]
    internal static class GlobalDataConsumerReady_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(staticVars), "Awake", Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                GlobalDataDeliveryPatchHealth.ReportFailure("staticVars.Awake() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(staticVars).FullName, "Awake");
            }

            GlobalDataDeliveryPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(staticVars __instance)
        {
            GlobalDataDeliveryRepair.ReplayPendingAfterConsumerReady(__instance);
        }
    }

    /// <summary>
    /// Passive observation of the sole normal vanilla GlobalData consumer. This patch never
    /// suppresses or replaces LoadSettings; it only records whether vanilla already delivered.
    /// </summary>
    [HarmonyPatch]
    internal static class GlobalDataConsumerInvocation_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(staticVars), nameof(staticVars.LoadSettings), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                GlobalDataDeliveryPatchHealth.ReportFailure("staticVars.LoadSettings() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(staticVars).FullName, nameof(staticVars.LoadSettings));
            }

            GlobalDataDeliveryPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            GlobalDataDeliveryRepair.ObserveLoadSettingsInvocation();
        }
    }
}
