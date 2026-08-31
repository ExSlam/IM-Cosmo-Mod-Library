using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Registers the exact iterator returned by each of the four audited callback
    /// factories. This centralizes all 13 vanilla scheduling sites without patching
    /// their business logic or serializing their Action closures.
    /// </summary>
    [HarmonyPatch]
    internal static class SemanticCallbackFactories_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = new MethodInfo[]
            {
                ResolveFactory(
                    typeof(ActiveDialogueController),
                    "_DoAfterDialogue"),
                ResolveFactory(typeof(vn_actions), "DoAfterDialogue"),
                ResolveFactory(typeof(vn_actions), "DoAfterPopups"),
                ResolveFactory(typeof(vn_actions), "DoAfterPopupsAndDialogue")
            };

            foreach (MethodInfo method in methods)
            {
                if (method == null)
                {
                    continue;
                }

                SemanticCallbackCheckpointPatchHealth.ReportFactoryResolved(method);
                yield return method;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(
            Action callback,
            ref IEnumerator __result,
            MethodBase __originalMethod)
        {
            SemanticCallbackCheckpointRepair.Register(
                __result,
                callback,
                __originalMethod);
        }

        private static MethodInfo ResolveFactory(Type ownerType, string methodName)
        {
            MethodInfo method = ownerType.GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Action) },
                null);

            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                SemanticCallbackCheckpointPatchHealth.ReportFailure(
                    ownerType.FullName + "." + methodName + "(Action)",
                    "IEnumerator callback factory could not be resolved");
                return null;
            }

            return method;
        }
    }

    /// <summary>
    /// Selectively guards only the four generated callback iterators chosen by the
    /// audit. A stale epoch skips MoveNext entirely. A normal false return releases
    /// the lease after callback completion, while the finalizer path releases the
    /// lease on exception without swallowing that exception.
    /// </summary>
    [HarmonyPatch]
    internal static class SemanticCallbackMoveNext_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = new MethodInfo[]
            {
                ResolveMoveNext(
                    typeof(ActiveDialogueController),
                    "<_DoAfterDialogue>d__126"),
                ResolveMoveNext(typeof(vn_actions), "<DoAfterDialogue>d__12"),
                ResolveMoveNext(typeof(vn_actions), "<DoAfterPopups>d__13"),
                ResolveMoveNext(typeof(vn_actions), "<DoAfterPopupsAndDialogue>d__14")
            };

            foreach (MethodInfo method in methods)
            {
                if (method == null)
                {
                    continue;
                }

                SemanticCallbackCheckpointPatchHealth.ReportMoveNextResolved(method);
                yield return method;
            }
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (SemanticCallbackCheckpointRepair.ShouldRunMoveNext(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            if (!__result)
            {
                SemanticCallbackCheckpointRepair.MarkMoveNextCompleted(__instance);
            }
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(
            object __instance,
            Exception __exception)
        {
            if (__exception != null)
            {
                SemanticCallbackCheckpointRepair.MarkMoveNextFaulted(
                    __instance,
                    __exception);
            }

            return __exception;
        }

        private static MethodInfo ResolveMoveNext(
            Type ownerType,
            string iteratorTypeName)
        {
            Type iteratorType = ownerType.GetNestedType(
                iteratorTypeName,
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                SemanticCallbackCheckpointPatchHealth.ReportFailure(
                    ownerType.FullName + "." + iteratorTypeName,
                    "generated iterator type could not be resolved");
                return null;
            }

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

            SemanticCallbackCheckpointPatchHealth.ReportFailure(
                iteratorType.FullName + ".MoveNext",
                "zero-argument bool MoveNext method could not be resolved");
            return null;
        }
    }
}
