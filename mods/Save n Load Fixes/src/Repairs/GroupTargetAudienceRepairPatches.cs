using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Completes only the save-side GroupData copier. Vanilla remains responsible
    /// for constructing the DTO, copying every other field, serializing it, and
    /// restoring the three audience fields through Groups._group.Set(GroupData).
    /// </summary>
    [HarmonyPatch]
    internal static class GroupTargetAudience_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Groups.GroupData),
                "Set",
                new[] { typeof(Groups._group) });

            if (method == null || method.ReturnType != typeof(void))
            {
                GroupTargetAudiencePatchHealth.ReportFailure(
                    "Groups.GroupData.Set(Groups._group) could not be resolved with the audited save-side copier signature");
                throw new MissingMethodException(typeof(Groups.GroupData).FullName, "Set");
            }

            GroupTargetAudiencePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(
            Groups.GroupData __instance,
            Groups._group Group)
        {
            GroupTargetAudienceRepair.CompleteSaveSideCopy(__instance, Group);
        }
    }
}
