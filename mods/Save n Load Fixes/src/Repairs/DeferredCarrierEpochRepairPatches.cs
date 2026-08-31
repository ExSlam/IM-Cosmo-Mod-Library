using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Registers exactly the seven A08 iterator factories not already owned by the
    /// family-specific Event Templates, semantic callback, birthday, and SSK repairs.
    /// </summary>
    [HarmonyPatch]
    internal static class DeferredCarrierFactories_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in ResolveFactories())
            {
                DeferredCarrierEpochPatchHealth.ReportFactoryResolved(method);
                yield return method;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(
            MethodBase __originalMethod,
            ref IEnumerator __result)
        {
            DeferredCarrierEpochRepair.Register(__result, __originalMethod);
        }

        private static IEnumerable<MethodInfo> ResolveFactories()
        {
            yield return RequireFactory(
                typeof(Substories_Manager),
                "_CheckDialogueQueue",
                Type.EmptyTypes);
            yield return RequireFactory(
                typeof(Activities),
                "Chain_Progress_Do",
                Type.EmptyTypes);
            yield return RequireFactory(
                typeof(Scenes),
                "AssignSceneWithDelay",
                new[]
                {
                    typeof(DateTime),
                    typeof(agency._room),
                    typeof(Scenes.type),
                    typeof(List<data_girls.girls>)
                });
            yield return RequireFactory(
                typeof(Date_Graduation),
                "_StartIntroductions",
                Type.EmptyTypes);
            yield return RequireFactory(
                typeof(agency),
                "ReturnGirl",
                new[] { typeof(data_girls.girls) });
            yield return RequireFactory(
                typeof(Tutorial_Actions),
                "Manager_After_Conversation",
                Type.EmptyTypes);
            yield return RequireFactory(
                typeof(Tutorial_Actions),
                "Activities_After_Hire",
                Type.EmptyTypes);
        }

        private static MethodInfo RequireFactory(
            Type owner,
            string name,
            Type[] parameterTypes)
        {
            MethodInfo method = owner.GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);

            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                string seam = owner.FullName + "." + name;
                DeferredCarrierEpochPatchHealth.ReportFailure(
                    seam,
                    "private IEnumerator factory could not be resolved with the audited signature");
                throw new MissingMethodException(owner.FullName, name);
            }

            return method;
        }
    }

    /// <summary>
    /// Guards only the seven exact generated MoveNext state machines in the A08
    /// manifest. No recurring scheduler/UI/infrastructure coroutine is targeted.
    /// </summary>
    [HarmonyPatch]
    internal static class DeferredCarrierMoveNext_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo moveNext in ResolveMoveNextMethods())
            {
                DeferredCarrierEpochPatchHealth.ReportMoveNextResolved(moveNext);
                yield return moveNext;
            }
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (DeferredCarrierEpochRepair.ShouldRunMoveNext(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            DeferredCarrierEpochRepair.ObserveMoveNextReturn(__instance, __result);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception != null)
            {
                DeferredCarrierEpochRepair.ObserveMoveNextFault(
                    __instance,
                    __exception);
            }

            return __exception;
        }

        private static IEnumerable<MethodInfo> ResolveMoveNextMethods()
        {
            yield return RequireMoveNext(
                typeof(Substories_Manager),
                "<_CheckDialogueQueue>d__18");
            yield return RequireMoveNext(
                typeof(Activities),
                "<Chain_Progress_Do>d__53");
            yield return RequireMoveNext(
                typeof(Scenes),
                "<AssignSceneWithDelay>d__12");
            yield return RequireMoveNext(
                typeof(Date_Graduation),
                "<_StartIntroductions>d__10");
            yield return RequireMoveNext(
                typeof(agency),
                "<ReturnGirl>d__63");
            yield return RequireMoveNext(
                typeof(Tutorial_Actions),
                "<Manager_After_Conversation>d__4");
            yield return RequireMoveNext(
                typeof(Tutorial_Actions),
                "<Activities_After_Hire>d__5");
        }

        private static MethodInfo RequireMoveNext(Type owner, string nestedName)
        {
            Type iteratorType = owner.GetNestedType(nestedName, BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                DeferredCarrierEpochPatchHealth.ReportFailure(
                    owner.FullName + "." + nestedName,
                    "generated iterator type could not be resolved");
                throw new MissingMemberException(owner.FullName, nestedName);
            }

            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                bool nameMatches =
                    string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches &&
                    method.ReturnType == typeof(bool) &&
                    method.GetParameters().Length == 0)
                {
                    return method;
                }
            }

            DeferredCarrierEpochPatchHealth.ReportFailure(
                iteratorType.FullName + ".MoveNext",
                "zero-argument bool MoveNext method could not be resolved");
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }
    }

    /// <summary>
    /// Resets only Substories_Manager's coroutine mutex on target reconstruction.
    /// The authoritative dialogue queue itself is rebuilt by vanilla LoadFunction.
    /// </summary>
    [HarmonyPatch]
    internal static class SubstoriesQueueMutexLoadReset_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(Substories_Manager).GetMethod(
                "LoadFunction",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                DeferredCarrierEpochPatchHealth.ReportFailure(
                    "Substories_Manager.LoadFunction()",
                    "public target-load reconstruction seam could not be resolved");
                throw new MissingMethodException(
                    typeof(Substories_Manager).FullName,
                    "LoadFunction");
            }

            DeferredCarrierEpochPatchHealth.ReportSubstoryMutexResetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(ref bool ___checkingQueue)
        {
            DeferredCarrierEpochRepair.ResetSubstoryQueueMutex(ref ___checkingQueue);
        }
    }
}
