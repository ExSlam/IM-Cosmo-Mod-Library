using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Captures the exact semantic job after vanilla has already selected the due
    /// time, room, scene type, and idol list, but before StartCoroutine owns the
    /// returned iterator.
    /// </summary>
    [HarmonyPatch]
    internal static class PendingAmbientSceneFactory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(Scenes).GetMethod(
                "AssignSceneWithDelay",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(DateTime),
                    typeof(agency._room),
                    typeof(Scenes.type),
                    typeof(List<data_girls.girls>)
                },
                null);

            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                PendingAmbientScenePatchHealth.ReportFailure(
                    "Scenes.AssignSceneWithDelay(DateTime, agency._room, Scenes.type, List<data_girls.girls>)",
                    "private IEnumerator factory could not be resolved with the audited signature");
                throw new MissingMethodException(typeof(Scenes).FullName, "AssignSceneWithDelay");
            }

            PendingAmbientScenePatchHealth.ReportResolved(method);
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(
            ref IEnumerator __result,
            DateTime StartTime,
            agency._room room,
            Scenes.type randType,
            List<data_girls.girls> usedGirls,
            agency ___Agency)
        {
            PendingAmbientSceneRepair.RegisterScheduledJob(
                __result,
                StartTime,
                room,
                randType,
                usedGirls,
                ___Agency);
        }
    }

    /// <summary>
    /// Observes lifecycle of the exact delayed-scene state machine. The Prefix is
    /// intentionally void and Priority.First: it retires stale N12 bookkeeping
    /// before A08's existing default-priority bool Prefix suppresses the old
    /// MoveNext. N12-A never returns false and never becomes a second mutation guard.
    /// </summary>
    [HarmonyPatch]
    internal static class PendingAmbientSceneMoveNext_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(Scenes).GetNestedType(
                "<AssignSceneWithDelay>d__12",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                PendingAmbientScenePatchHealth.ReportFailure(
                    "Scenes.<AssignSceneWithDelay>d__12",
                    "generated iterator type could not be resolved");
                throw new MissingMemberException(typeof(Scenes).FullName, "<AssignSceneWithDelay>d__12");
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
                    PendingAmbientScenePatchHealth.ReportResolved(method);
                    return method;
                }
            }

            PendingAmbientScenePatchHealth.ReportFailure(
                iteratorType.FullName + ".MoveNext",
                "zero-argument bool MoveNext method could not be resolved");
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(object __instance)
        {
            PendingAmbientSceneRepair.ObserveMoveNextEntry(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            PendingAmbientSceneRepair.ObserveMoveNextReturn(__instance, __result);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception != null)
            {
                PendingAmbientSceneRepair.ObserveMoveNextFault(__instance, __exception);
            }
            return __exception;
        }
    }
}
