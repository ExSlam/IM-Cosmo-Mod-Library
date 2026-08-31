using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A05 is a loader-local Prefix/Postfix repair. Prefix snapshots the already-
    /// serialized allocator before vanilla reconstructs saved idols; Postfix restores
    /// that value after temporary GenerateGirl() allocations have finished.
    /// </summary>
    [HarmonyPatch]
    internal static class LastGirlId_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(data_girls),
                nameof(data_girls.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                LastGirlIdPatchHealth.ReportFailure(
                    "data_girls.LoadFunction() could not be resolved with the audited public void signature");
                throw new MissingMethodException(
                    typeof(data_girls).FullName,
                    nameof(data_girls.LoadFunction));
            }

            LastGirlIdPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(ref LastGirlIdRepair.LoadState __state)
        {
            __state = LastGirlIdRepair.CaptureSerializedAllocator();
        }

        [HarmonyPostfix]
        private static void Postfix(LastGirlIdRepair.LoadState __state)
        {
            LastGirlIdRepair.RestoreSerializedAllocator(__state);
        }
    }
}
