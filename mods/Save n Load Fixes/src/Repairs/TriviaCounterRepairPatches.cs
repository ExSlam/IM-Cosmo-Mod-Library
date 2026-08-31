using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A18 is a two-loader Prefix repair. Each Prefix clears the current authored
    /// counters to zero, then vanilla LoadFunction() remains authoritative for applying
    /// its serialized nonzero rows. This makes serialized zero authoritative on F9
    /// without duplicating either loader or changing unknown-ID handling.
    /// </summary>
    [HarmonyPatch]
    internal static class GirlsTriviaZeroCounter_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(girls_trivia),
                nameof(girls_trivia.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                TriviaCounterPatchHealth.ReportFailure(
                    "girls_trivia.LoadFunction() could not be resolved with the audited public void signature");
                throw new MissingMethodException(
                    typeof(girls_trivia).FullName,
                    nameof(girls_trivia.LoadFunction));
            }

            TriviaCounterPatchHealth.ReportGirlsTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            TriviaCounterRepair.ResetGirlsTriviaCounters();
        }
    }

    [HarmonyPatch]
    internal static class GraduationTriviaZeroCounter_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Graduation_Trivia),
                nameof(Graduation_Trivia.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                TriviaCounterPatchHealth.ReportFailure(
                    "Graduation_Trivia.LoadFunction() could not be resolved with the audited public void signature");
                throw new MissingMethodException(
                    typeof(Graduation_Trivia).FullName,
                    nameof(Graduation_Trivia.LoadFunction));
            }

            TriviaCounterPatchHealth.ReportGraduationTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            TriviaCounterRepair.ResetGraduationTriviaCounters();
        }
    }
}
