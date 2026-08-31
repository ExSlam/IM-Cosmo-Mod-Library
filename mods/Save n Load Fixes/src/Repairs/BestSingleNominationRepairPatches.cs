using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A30 patches the private temporary-award creation seam rather than SetWins().
    /// The best_single row therefore contains its authoritative nominee from birth,
    /// and later result/variable/history consumers all observe the same stored object.
    /// </summary>
    [HarmonyPatch]
    internal static class BestSingleNomination_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Awards),
                "AddTempNomination",
                new[]
                {
                    typeof(Awards._type),
                    typeof(data_girls.girls),
                    typeof(singles._single)
                });

            if (method == null || method.ReturnType != typeof(void))
            {
                BestSingleNominationPatchHealth.ReportFailure(
                    "Awards.AddTempNomination(_type, girls, _single) could not be resolved with the audited private-static signature");
                throw new MissingMethodException(typeof(Awards).FullName, "AddTempNomination");
            }

            BestSingleNominationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(
            Awards._type Type,
            ref singles._single Single)
        {
            BestSingleNominationRepair.BindBestSingleNominee(Type, ref Single);
        }
    }
}
