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
    /// Observe the exact private factory that vanilla reaches only after
    /// BanGirlFromAutoJobs() has inserted a non-duplicate list membership.
    /// </summary>
    [HarmonyPatch]
    internal static class TemporaryAutoTaskBanFactory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(agency).GetMethod(
                "ReturnGirl",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(data_girls.girls) },
                null);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                TemporaryAutoTaskBanPatchHealth.ReportFailure(
                    "agency.ReturnGirl(girls) private IEnumerator factory could not be resolved with the audited signature.");
                throw new MissingMethodException(typeof(agency).FullName, "ReturnGirl");
            }

            TemporaryAutoTaskBanPatchHealth.ReportTargetResolved("agency.ReturnGirl(girls)");
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(data_girls.girls _GIRL, ref IEnumerator __result)
        {
            TemporaryAutoTaskBanRepair.RegisterIterator(__result, _GIRL);
        }
    }

    /// <summary>
    /// Rewrites only the delay operand feeding the single audited
    /// WaitForSeconds(60f) constructor. The same generated MoveNext remains guarded
    /// by A08's prefix; A13 never returns a competing bool execution decision.
    /// </summary>
    [HarmonyPatch]
    internal static class TemporaryAutoTaskBanMoveNext_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(agency).GetNestedType("<ReturnGirl>d__63", BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                TemporaryAutoTaskBanPatchHealth.ReportFailure(
                    "agency.<ReturnGirl>d__63 generated iterator type could not be resolved.");
                throw new MissingMemberException(typeof(agency).FullName, "<ReturnGirl>d__63");
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
                    TemporaryAutoTaskBanPatchHealth.ReportTargetResolved("agency.<ReturnGirl>d__63.MoveNext()");
                    return method;
                }
            }

            TemporaryAutoTaskBanPatchHealth.ReportFailure(
                "agency.<ReturnGirl>d__63.MoveNext() could not be resolved with the audited bool signature.");
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ConstructorInfo waitConstructor = typeof(WaitForSeconds).GetConstructor(
                new[] { typeof(float) });
            MethodInfo resolver = AccessTools.Method(
                typeof(TemporaryAutoTaskBanRepair),
                nameof(TemporaryAutoTaskBanRepair.ResolveWaitSeconds),
                new[] { typeof(object), typeof(float) });

            List<CodeInstruction> result = new List<CodeInstruction>(instructions);
            if (waitConstructor == null || resolver == null)
            {
                TemporaryAutoTaskBanPatchHealth.ReportFailure(
                    "A13 could not resolve WaitForSeconds(float) or its delay resolver.");
                return result;
            }

            List<int> sites = new List<int>();
            for (int i = 0; i + 1 < result.Count; i++)
            {
                CodeInstruction instruction = result[i];
                if (instruction.opcode != OpCodes.Ldc_R4 || instruction.operand == null)
                {
                    continue;
                }

                float value;
                try
                {
                    value = Convert.ToSingle(instruction.operand);
                }
                catch (Exception)
                {
                    continue;
                }

                if (value == TemporaryAutoTaskBanRepair.VanillaDelaySeconds &&
                    result[i + 1].opcode == OpCodes.Newobj &&
                    Equals(result[i + 1].operand, waitConstructor))
                {
                    sites.Add(i);
                }
            }

            TemporaryAutoTaskBanPatchHealth.ReportWaitForSecondsSites(sites.Count);
            if (sites.Count != TemporaryAutoTaskBanPatchHealth.ExpectedWaitForSecondsSiteCount)
            {
                return result;
            }

            int index = sites[0];
            float originalDelay = Convert.ToSingle(result[index].operand);

            // Preserve all labels/blocks on the original instruction by turning that
            // exact position into ldarg.0. The preceding ldarg.0 still supplies the
            // state-machine instance needed by stfld <>2__current; this second one is
            // consumed only by ResolveWaitSeconds(iterator, originalDelay).
            result[index].opcode = OpCodes.Ldarg_0;
            result[index].operand = null;
            result.Insert(index + 1, new CodeInstruction(OpCodes.Ldc_R4, originalDelay));
            result.Insert(index + 2, new CodeInstruction(OpCodes.Call, resolver));
            return result;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            TemporaryAutoTaskBanRepair.ObserveExpiryMoveNextReturn(__instance, __result);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception != null)
            {
                TemporaryAutoTaskBanRepair.ObserveExpiryFault(__instance, __exception);
            }
            return __exception;
        }
    }

    /// <summary>
    /// data_girls.LoadFunction() is the exact roster-replacement seam already used by
    /// A11. Clear discarded object membership first; restore current-format A13 state
    /// only after vanilla has constructed the fresh idol objects.
    /// </summary>
    [HarmonyPatch]
    internal static class TemporaryAutoTaskBanLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(data_girls), "LoadFunction", Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                TemporaryAutoTaskBanPatchHealth.ReportFailure(
                    "data_girls.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, "LoadFunction");
            }

            TemporaryAutoTaskBanPatchHealth.ReportTargetResolved("data_girls.LoadFunction()");
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            TemporaryAutoTaskBanRepair.ClearBeforeTargetGirlLoad();
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            TemporaryAutoTaskBanRepair.RestoreAfterTargetGirlLoad();
        }
    }
}
