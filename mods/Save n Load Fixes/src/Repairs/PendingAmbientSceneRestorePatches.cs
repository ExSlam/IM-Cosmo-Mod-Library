using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Tags each exact agency LoadData iterator with the LoadEpoch in which its
    /// target load launched. This is observation only: N12-C never suppresses or
    /// mutates agency reconstruction, and A08 remains the stale gameplay-carrier guard.
    /// </summary>
    [HarmonyPatch]
    internal static class PendingAmbientSceneAgencyLoadFactory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(agency).GetMethod(
                "LoadData",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                PendingAmbientSceneRestorePatchHealth.ReportFailure(
                    "agency.LoadData()",
                    "private IEnumerator factory could not be resolved with the audited signature");
                throw new MissingMethodException(typeof(agency).FullName, "LoadData");
            }

            PendingAmbientSceneRestorePatchHealth.ReportResolved(method);
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(ref IEnumerator __result)
        {
            PendingAmbientSceneRestore.RegisterAgencyLoadIterator(__result);
        }
    }

    /// <summary>
    /// Observes only terminal completion of the exact asynchronous agency loader,
    /// after vanilla has waited for idols/groups, rebuilt floors/rooms, reset floor
    /// IDs, rendered the agency, and rebound room staff. Stale loader generations
    /// are ignored as restore triggers but their vanilla MoveNext is never suppressed.
    /// </summary>
    [HarmonyPatch]
    internal static class PendingAmbientSceneAgencyLoadComplete_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(agency).GetNestedType(
                "<LoadData>d__80",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                PendingAmbientSceneRestorePatchHealth.ReportFailure(
                    "agency.<LoadData>d__80",
                    "generated agency load iterator type could not be resolved");
                throw new MissingMemberException(typeof(agency).FullName, "<LoadData>d__80");
            }

            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                bool nameMatches =
                    string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                {
                    PendingAmbientSceneRestorePatchHealth.ReportResolved(method);
                    return method;
                }
            }

            PendingAmbientSceneRestorePatchHealth.ReportFailure(
                iteratorType.FullName + ".MoveNext",
                "zero-argument bool MoveNext method could not be resolved");
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(object __instance, bool __result)
        {
            PendingAmbientSceneRestore.ObserveAgencyLoadMoveNext(__instance, __result);
        }
    }
}
