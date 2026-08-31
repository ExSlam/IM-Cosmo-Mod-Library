using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Registers the exact iterator created by SEvent_SSK.StartSSK_Coroutine().
    /// The factory executes synchronously inside StartSSK after production-cost debit
    /// and before StartSSK returns to the frame loop.
    /// </summary>
    [HarmonyPatch]
    internal static class SskStartCoroutineFactory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(SEvent_SSK).GetMethod(
                "StartSSK_Coroutine",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                SskCheckpointPatchHealth.ReportFailure(
                    "SEvent_SSK.StartSSK_Coroutine()",
                    "private IEnumerator factory could not be resolved");
                throw new MissingMethodException(
                    typeof(SEvent_SSK).FullName,
                    "StartSSK_Coroutine");
            }

            SskCheckpointPatchHealth.ReportFactoryResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(
            SEvent_SSK __instance,
            ref IEnumerator __result)
        {
            SskCheckpointRepair.Register(__result, __instance);
        }
    }

    /// <summary>
    /// Selectively guards only the generated two-second SSK launch carrier.
    /// </summary>
    [HarmonyPatch]
    internal static class SskStartCoroutineMoveNext_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(SEvent_SSK).GetNestedType(
                "<StartSSK_Coroutine>d__28",
                BindingFlags.NonPublic);

            if (iteratorType == null)
            {
                SskCheckpointPatchHealth.ReportFailure(
                    "SEvent_SSK.<StartSSK_Coroutine>d__28",
                    "generated iterator type could not be resolved");
                throw new MissingMemberException(
                    typeof(SEvent_SSK).FullName,
                    "<StartSSK_Coroutine>d__28");
            }

            MethodInfo moveNext = FindMoveNext(iteratorType);
            if (moveNext == null)
            {
                SskCheckpointPatchHealth.ReportFailure(
                    iteratorType.FullName + ".MoveNext",
                    "zero-argument bool MoveNext method could not be resolved");
                throw new MissingMethodException(
                    iteratorType.FullName,
                    "MoveNext");
            }

            SskCheckpointPatchHealth.ReportMoveNextResolved();
            return moveNext;
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (SskCheckpointRepair.PrepareMoveNext(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            SskCheckpointRepair.ObserveMoveNextReturn(__instance, __result);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(
            object __instance,
            Exception __exception)
        {
            if (__exception != null)
            {
                SskCheckpointRepair.ObserveMoveNextFault(
                    __instance,
                    __exception);
            }

            return __exception;
        }

        private static MethodInfo FindMoveNext(Type iteratorType)
        {
            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

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

            return null;
        }
    }

    /// <summary>
    /// Observes actual opening of only the SSK result popup. PopupManager.Open can
    /// merely enqueue, so the Task-6 blocker cannot hand off at that caller return.
    /// </summary>
    [HarmonyPatch]
    internal static class SskResultPopupOpen_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(PopupManager._popup).GetMethod(
                nameof(PopupManager._popup.Open),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                SskCheckpointPatchHealth.ReportFailure(
                    "PopupManager._popup.Open()",
                    "popup instance Open method could not be resolved");
                throw new MissingMethodException(
                    typeof(PopupManager._popup).FullName,
                    nameof(PopupManager._popup.Open));
            }

            SskCheckpointPatchHealth.ReportPopupOpenResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(PopupManager._popup __instance, out bool __state)
        {
            __state = __instance != null && __instance.open;
        }

        [HarmonyPostfix]
        private static void Postfix(PopupManager._popup __instance, bool __state)
        {
            // _popup.Open() returns immediately when already open. Observe only an
            // actual false -> true transition so an unrelated repeated Open call
            // cannot release a queued SSK launch prematurely.
            if (!__state && __instance != null && __instance.open)
            {
                SskCheckpointRepair.MarkSskPopupActuallyOpened(__instance);
            }
        }
    }
}
