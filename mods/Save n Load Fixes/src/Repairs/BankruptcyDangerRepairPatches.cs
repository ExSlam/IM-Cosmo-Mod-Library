using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A06 is a loader-local Postfix. loans.LoadFunction() has already restored the
    /// target save's BankruptcyDate when this runs; the repair reconstructs only the
    /// missing boolean from target SavedData and never invokes vanilla's deadline-
    /// rewriting private setter.
    /// </summary>
    [HarmonyPatch]
    internal static class BankruptcyDanger_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(loans),
                nameof(loans.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                BankruptcyDangerPatchHealth.ReportFailure(
                    "loans.LoadFunction() could not be resolved with the audited public void signature");
                throw new MissingMethodException(
                    typeof(loans).FullName,
                    nameof(loans.LoadFunction));
            }

            BankruptcyDangerPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            BankruptcyDangerRepair.ReconstructFromSerializedMoney();
        }
    }
}
