using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A17 patches the stock girl-selection commit seam. Pushes.OnPushClick() passes
    /// the actual static Pushes.Girls list into Girl_Select_Popup.Set(...), so receiver
    /// reference identity distinguishes the pushed-idol editor from every other use of
    /// the generic girl selector. The Prefix runs before vanilla overwrites the slot.
    /// </summary>
    [HarmonyPatch]
    internal static class PushSlotDuration_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Girl_Select_Popup),
                "OnClick",
                new[] { typeof(data_girls.girls) });

            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void))
            {
                PushSlotDurationPatchHealth.ReportFailure(
                    "Girl_Select_Popup.OnClick(data_girls.girls) could not be resolved with the audited public-instance void signature");
                throw new MissingMethodException(typeof(Girl_Select_Popup).FullName, "OnClick");
            }

            PushSlotDurationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            Girl_Select_Popup __instance,
            data_girls.girls girl)
        {
            PushSlotDurationRepair.ResetDurationBeforePushSelection(__instance, girl);
        }
    }
}
