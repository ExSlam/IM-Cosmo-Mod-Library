using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// The supplied source has exactly one Date_Graduation.StartIntroductions()
    /// caller: Find_Successor(). Register only after vanilla successfully schedules
    /// its existing five-second coroutine.
    /// </summary>
    [HarmonyPatch]
    internal static class SuccessorIntroductionSchedule_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Date_Graduation),
                nameof(Date_Graduation.StartIntroductions),
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                SuccessorIntroductionPatchHealth.ReportFailure(
                    "Date_Graduation.StartIntroductions() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Date_Graduation).FullName, "StartIntroductions");
            }

            SuccessorIntroductionPatchHealth.ReportTargetResolved("Date_Graduation.StartIntroductions()");
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            SuccessorIntroductionRepair.RegisterScheduledIntroduction();
        }
    }

    /// <summary>
    /// Observe the one vanilla semantic conversion point. The token is cleared only
    /// after data_girls.StartIntroductions() successfully drains new_girls.
    /// </summary>
    [HarmonyPatch]
    internal static class SuccessorIntroductionFlush_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(data_girls),
                nameof(data_girls.StartIntroductions),
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void) || !method.IsStatic)
            {
                SuccessorIntroductionPatchHealth.ReportFailure(
                    "data_girls.StartIntroductions() could not be resolved with the audited static void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, "StartIntroductions");
            }

            SuccessorIntroductionPatchHealth.ReportTargetResolved("data_girls.StartIntroductions()");
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(ref SuccessorIntroductionRepair.FlushState __state)
        {
            __state = SuccessorIntroductionRepair.BeginIntroductionFlush();
        }

        [HarmonyPostfix]
        private static void Postfix(SuccessorIntroductionRepair.FlushState __state)
        {
            SuccessorIntroductionRepair.CompleteIntroductionFlush(__state);
        }
    }

    /// <summary>
    /// data_girls.LoadFunction() reconstructs the complete target idol roster but its
    /// private Reset() does not clear static new_girls. Clear stale references first,
    /// then rebind the A11 section after the fresh roster exists.
    /// </summary>
    [HarmonyPatch]
    internal static class SuccessorIntroductionLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(data_girls),
                "LoadFunction",
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                SuccessorIntroductionPatchHealth.ReportFailure(
                    "data_girls.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, "LoadFunction");
            }

            SuccessorIntroductionPatchHealth.ReportTargetResolved("data_girls.LoadFunction()");
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            SuccessorIntroductionRepair.ClearBeforeTargetGirlLoad();
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            SuccessorIntroductionRepair.RestoreAfterTargetGirlLoad();
        }
    }
}
