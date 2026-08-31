using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Bound semantic registration to the exact vanilla _Coroutine(ID) dispatcher.
    /// The private factory observers below only accept iterators created inside this
    /// synchronous scope.
    /// </summary>
    [HarmonyPatch]
    internal static class DelayedTutorialContinuationDispatch_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Tutorial_Actions), "_Coroutine", new[] { typeof(string) });
            if (method == null || method.ReturnType != typeof(void))
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "Tutorial_Actions._Coroutine(string) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Tutorial_Actions).FullName, "_Coroutine");
            }
            DelayedTutorialContinuationPatchHealth.ReportTargetResolved("Tutorial_Actions._Coroutine(string)");
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(string ID, ref DelayedTutorialContinuationRepair.DispatchScope __state)
        {
            __state = DelayedTutorialContinuationRepair.BeginDispatch(ID);
        }

        [HarmonyPostfix]
        private static void Postfix(DelayedTutorialContinuationRepair.DispatchScope __state)
        {
            DelayedTutorialContinuationRepair.EndDispatch(__state);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(
            DelayedTutorialContinuationRepair.DispatchScope __state,
            Exception __exception)
        {
            if (__exception != null)
            {
                DelayedTutorialContinuationRepair.EndDispatch(__state);
            }
            return __exception;
        }
    }

    /// <summary>
    /// Capture the exact generated carrier objects. A08 independently observes these
    /// same two factories and remains the stale-execution owner.
    /// </summary>
    [HarmonyPatch]
    internal static class DelayedTutorialContinuationFactory_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return RequireFactory("Manager_After_Conversation");
            yield return RequireFactory("Activities_After_Hire");
        }

        private static MethodInfo RequireFactory(string name)
        {
            MethodInfo method = typeof(Tutorial_Actions).GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "Tutorial_Actions." + name + "() private IEnumerator factory could not be resolved.");
                throw new MissingMethodException(typeof(Tutorial_Actions).FullName, name);
            }
            DelayedTutorialContinuationPatchHealth.ReportTargetResolved("Tutorial_Actions." + name + "()");
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(MethodBase __originalMethod, ref IEnumerator __result)
        {
            if (__originalMethod == null)
            {
                return;
            }
            string kind = string.Equals(__originalMethod.Name, "Manager_After_Conversation", StringComparison.Ordinal)
                ? DelayedTutorialContinuationRepair.ManagerKind
                : string.Equals(__originalMethod.Name, "Activities_After_Hire", StringComparison.Ordinal)
                    ? DelayedTutorialContinuationRepair.ActivitiesKind
                    : null;
            DelayedTutorialContinuationRepair.RegisterIterator(__result, kind);
        }
    }

    /// <summary>
    /// Observation-only lifecycle hooks for both generated carriers. There is no bool
    /// Prefix here; A08's ShouldRunMoveNext remains the sole execution decision.
    /// </summary>
    [HarmonyPatch]
    internal static class DelayedTutorialContinuationMoveNextObservation_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return RequireMoveNext("<Manager_After_Conversation>d__4");
            yield return RequireMoveNext("<Activities_After_Hire>d__5");
        }

        private static MethodInfo RequireMoveNext(string iteratorName)
        {
            Type iteratorType = typeof(Tutorial_Actions).GetNestedType(iteratorName, BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "Tutorial_Actions." + iteratorName + " generated iterator type could not be resolved.");
                throw new MissingMemberException(typeof(Tutorial_Actions).FullName, iteratorName);
            }

            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                bool nameMatches = string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                {
                    DelayedTutorialContinuationPatchHealth.ReportTargetResolved(
                        "Tutorial_Actions." + iteratorName + ".MoveNext()");
                    return method;
                }
            }

            DelayedTutorialContinuationPatchHealth.ReportFailure(
                "Tutorial_Actions." + iteratorName + ".MoveNext() could not be resolved with the audited bool signature.");
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            DelayedTutorialContinuationRepair.ObserveMoveNextReturn(__instance, __result);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception != null)
            {
                DelayedTutorialContinuationRepair.ObserveMoveNextFault(__instance, __exception);
            }
            return __exception;
        }
    }

    /// <summary>
    /// Rewrites only the two source-proven WaitForSeconds(5f) operands in
    /// Activities_After_Hire. The manager continuation has no scaled-time delay.
    /// </summary>
    [HarmonyPatch]
    internal static class DelayedTutorialContinuationActivitiesDelay_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(Tutorial_Actions).GetNestedType(
                "<Activities_After_Hire>d__5",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "Tutorial_Actions.<Activities_After_Hire>d__5 generated iterator type could not be resolved for delay rewriting.");
                throw new MissingMemberException(typeof(Tutorial_Actions).FullName, "<Activities_After_Hire>d__5");
            }

            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                bool nameMatches = string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                {
                    // Logical target was already counted by the lifecycle observer.
                    return method;
                }
            }
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ConstructorInfo waitConstructor = typeof(WaitForSeconds).GetConstructor(new[] { typeof(float) });
            MethodInfo resolver = AccessTools.Method(
                typeof(DelayedTutorialContinuationRepair),
                nameof(DelayedTutorialContinuationRepair.ResolveActivitiesWaitSeconds),
                new[] { typeof(object), typeof(float), typeof(int) });

            List<CodeInstruction> result = new List<CodeInstruction>(instructions);
            if (waitConstructor == null || resolver == null)
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "A14 could not resolve WaitForSeconds(float) or its activities delay resolver.");
                return result;
            }

            List<int> originalSites = new List<int>();
            int wrappedSiteCount = 0;
            int wrappedOrdinalMask = 0;
            bool malformedWrappedSite = false;
            for (int i = 0; i + 1 < result.Count; i++)
            {
                if (result[i + 1].opcode != OpCodes.Newobj ||
                    !Equals(result[i + 1].operand, waitConstructor))
                {
                    continue;
                }

                float value;
                if (TryReadFloat(result[i], out value) &&
                    value == DelayedTutorialContinuationRepair.VanillaActivitiesDelaySeconds)
                {
                    originalSites.Add(i);
                    continue;
                }

                if (result[i].opcode != OpCodes.Call || !Equals(result[i].operand, resolver))
                {
                    continue;
                }

                wrappedSiteCount++;
                int ordinal;
                if (i < 3 || result[i - 3].opcode != OpCodes.Ldarg_0 ||
                    !TryReadFloat(result[i - 2], out value) ||
                    value != DelayedTutorialContinuationRepair.VanillaActivitiesDelaySeconds ||
                    !TryReadInt32(result[i - 1], out ordinal) ||
                    ordinal < 0 || ordinal >=
                        DelayedTutorialContinuationPatchHealth.ExpectedActivitiesWaitForSecondsSiteCount ||
                    (wrappedOrdinalMask & (1 << ordinal)) != 0)
                {
                    malformedWrappedSite = true;
                    continue;
                }

                wrappedOrdinalMask |= 1 << ordinal;
            }

            int logicalSiteCount = originalSites.Count + wrappedSiteCount;
            DelayedTutorialContinuationPatchHealth.ReportActivitiesWaitForSecondsSites(logicalSiteCount);

            int expectedOrdinalMask =
                (1 << DelayedTutorialContinuationPatchHealth.ExpectedActivitiesWaitForSecondsSiteCount) - 1;
            if (originalSites.Count == 0 &&
                wrappedSiteCount == DelayedTutorialContinuationPatchHealth.ExpectedActivitiesWaitForSecondsSiteCount &&
                !malformedWrappedSite &&
                wrappedOrdinalMask == expectedOrdinalMask)
            {
                // HarmonyX can feed the already-composed body back through stored
                // transpilers when a later Prefix/Postfix is attached to the same
                // generated MoveNext. The exact resolver shape is already ours, so
                // treat it as the same two healthy logical sites and do not wrap it
                // a second time.
                return result;
            }

            if (originalSites.Count !=
                    DelayedTutorialContinuationPatchHealth.ExpectedActivitiesWaitForSecondsSiteCount ||
                wrappedSiteCount != 0)
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "A14 expected either two untouched Activities_After_Hire WaitForSeconds(5f) sites " +
                    "or two exact already-wrapped resolver sites, but observed " +
                    originalSites.Count + " untouched and " + wrappedSiteCount +
                    (malformedWrappedSite ? " malformed wrapped" : " wrapped") + " site(s).");
                return result;
            }

            // Insert from the last site backward so the original indexes stay stable.
            for (int ordinal = originalSites.Count - 1; ordinal >= 0; ordinal--)
            {
                int index = originalSites[ordinal];
                float originalDelay = Convert.ToSingle(result[index].operand);
                result[index].opcode = OpCodes.Ldarg_0;
                result[index].operand = null;
                result.Insert(index + 1, new CodeInstruction(OpCodes.Ldc_R4, originalDelay));
                result.Insert(index + 2, new CodeInstruction(OpCodes.Ldc_I4, ordinal));
                result.Insert(index + 3, new CodeInstruction(OpCodes.Call, resolver));
            }

            return result;
        }

        private static bool TryReadFloat(CodeInstruction instruction, out float value)
        {
            value = 0f;
            if (instruction == null || instruction.opcode != OpCodes.Ldc_R4 ||
                instruction.operand == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToSingle(instruction.operand);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryReadInt32(CodeInstruction instruction, out int value)
        {
            value = 0;
            if (instruction == null)
            {
                return false;
            }

            if (instruction.opcode == OpCodes.Ldc_I4_0)
            {
                return true;
            }
            if (instruction.opcode == OpCodes.Ldc_I4_1)
            {
                value = 1;
                return true;
            }
            if (instruction.opcode != OpCodes.Ldc_I4 &&
                instruction.opcode != OpCodes.Ldc_I4_S)
            {
                return false;
            }

            try
            {
                value = Convert.ToInt32(instruction.operand);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// variables.LoadFunction() adopts the target save's exact tutorial-variable list.
    /// Restore only after that idempotency witness is live.
    /// </summary>
    [HarmonyPatch]
    internal static class DelayedTutorialContinuationLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(variables), "LoadFunction", Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                DelayedTutorialContinuationPatchHealth.ReportFailure(
                    "variables.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(variables).FullName, "LoadFunction");
            }
            DelayedTutorialContinuationPatchHealth.ReportTargetResolved("variables.LoadFunction()");
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            DelayedTutorialContinuationRepair.ClearBeforeTargetVariablesLoad();
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            DelayedTutorialContinuationRepair.RestoreAfterTargetVariablesLoad();
        }
    }
}
