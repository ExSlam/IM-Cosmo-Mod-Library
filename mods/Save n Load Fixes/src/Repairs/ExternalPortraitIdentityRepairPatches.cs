using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class ExternalPortraitIdentityIdolLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(data_girls), nameof(data_girls.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                ExternalPortraitIdentityPatchHealth.ReportFailure("data_girls.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, nameof(data_girls.LoadFunction));
            }
            ExternalPortraitIdentityPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            ExternalPortraitIdentityRepair.BeginIdolLoad();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            ExternalPortraitIdentityRepair.EndIdolLoad();
        }
    }

    [HarmonyPatch]
    internal static class ExternalPortraitIdentityStaffLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(staff), nameof(staff.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                ExternalPortraitIdentityPatchHealth.ReportFailure("staff.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(staff).FullName, nameof(staff.LoadFunction));
            }
            ExternalPortraitIdentityPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            ExternalPortraitIdentityRepair.BeginStaffLoad();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            ExternalPortraitIdentityRepair.EndStaffLoad();
        }
    }

    [HarmonyPatch]
    internal static class ExternalPortraitIdentityIdolSave_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(data_girls), nameof(data_girls.SaveFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                ExternalPortraitIdentityPatchHealth.ReportFailure("data_girls.SaveFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, nameof(data_girls.SaveFunction));
            }
            ExternalPortraitIdentityPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            ExternalPortraitIdentityRepair.RewriteIdolSaveData();
        }
    }

    [HarmonyPatch]
    internal static class ExternalPortraitIdentityStaffSave_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(staff), nameof(staff.SaveFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                ExternalPortraitIdentityPatchHealth.ReportFailure("staff.SaveFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(staff).FullName, nameof(staff.SaveFunction));
            }
            ExternalPortraitIdentityPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            ExternalPortraitIdentityRepair.RewriteStaffSaveData();
        }
    }
}
