using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A34 patches only mainScript.TimeProgress's generated MoveNext. It inserts
    /// the missing monthly event immediately after vanilla invokes onNewDay, so
    /// monthly subscribers run only on genuine game-time day transitions and not
    /// during save loading or arbitrary staticVars.SetTime calls.
    /// </summary>
    [HarmonyPatch]
    internal static class MonthlyTransition_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(mainScript).GetNestedType(
                "<TimeProgress>d__96",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                MonthlyTransitionPatchHealth.ReportFailure(
                    "mainScript.<TimeProgress>d__96 could not be resolved with the audited generated-type name.");
                throw new MissingMemberException(typeof(mainScript).FullName, "<TimeProgress>d__96");
            }

            if (!MonthlyTransitionRepair.MonthEventFieldResolved)
            {
                MonthlyTransitionPatchHealth.ReportFailure(
                    "mainScript.onNewMonth backing delegate could not be resolved.");
                throw new MissingFieldException(typeof(mainScript).FullName, "onNewMonth");
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
                    MonthlyTransitionPatchHealth.ReportTargetResolved();
                    return method;
                }
            }

            MonthlyTransitionPatchHealth.ReportFailure(
                "mainScript.<TimeProgress>d__96.MoveNext could not be resolved with the audited zero-argument bool signature.");
            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo newDayInvoke = AccessTools.Method(typeof(mainScript.newDay), "Invoke");
            MethodInfo raiseMonth = AccessTools.Method(
                typeof(MonthlyTransitionRepair),
                "RaiseAfterVanillaNewDay");

            if (newDayInvoke == null || raiseMonth == null)
            {
                MonthlyTransitionPatchHealth.ReportFailure(
                    "A34 could not resolve the audited onNewDay delegate Invoke or monthly repair helper.");
                throw new MissingMethodException("A34 monthly transition injection methods could not be resolved.");
            }

            List<CodeInstruction> source = new List<CodeInstruction>(instructions);
            int observed = 0;

            for (int i = 0; i < source.Count; i++)
            {
                CodeInstruction instruction = source[i];
                yield return instruction;

                if (!Calls(instruction, newDayInvoke))
                {
                    continue;
                }

                observed++;

                // The generated iterator keeps its mainScript owner in local 1 in
                // this audited build. Reuse the exact load immediately preceding
                // the onNewDay backing-field load rather than pinning a local index.
                if (i < 2 || !LoadsMainScriptOwner(source[i - 2]))
                {
                    MonthlyTransitionPatchHealth.ReportFailure(
                        "A34 found mainScript.newDay.Invoke, but its audited mainScript owner load was not two instructions earlier.");
                    throw new InvalidOperationException(
                        "Unexpected mainScript.TimeProgress IL shape at the onNewDay invocation.");
                }

                CodeInstruction ownerLoad = new CodeInstruction(
                    source[i - 2].opcode,
                    source[i - 2].operand);
                yield return ownerLoad;
                yield return new CodeInstruction(OpCodes.Call, raiseMonth);
            }

            MonthlyTransitionPatchHealth.ReportInjectionSiteCount(observed);
            if (observed != MonthlyTransitionPatchHealth.ExpectedInjectionSiteCount)
            {
                throw new InvalidOperationException(
                    "Unexpected mainScript.TimeProgress onNewDay invocation count for A34.");
            }
        }

        private static bool Calls(CodeInstruction instruction, MethodInfo target)
        {
            if (instruction == null || target == null)
            {
                return false;
            }

            if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
            {
                return false;
            }

            MethodInfo operand = instruction.operand as MethodInfo;
            return operand == target;
        }

        private static bool LoadsMainScriptOwner(CodeInstruction instruction)
        {
            if (instruction == null)
            {
                return false;
            }

            // In the audited MoveNext body the owner is copied to a mainScript local
            // before the switch and that same local is loaded before every event.
            return instruction.opcode == OpCodes.Ldloc_0 ||
                instruction.opcode == OpCodes.Ldloc_1 ||
                instruction.opcode == OpCodes.Ldloc_2 ||
                instruction.opcode == OpCodes.Ldloc_3 ||
                instruction.opcode == OpCodes.Ldloc_S ||
                instruction.opcode == OpCodes.Ldloc;
        }
    }
}
