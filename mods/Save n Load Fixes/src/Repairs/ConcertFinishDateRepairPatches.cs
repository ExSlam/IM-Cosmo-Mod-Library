using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A07 patches only SEvent_Concerts.LoadFunction(). It replaces the single
    /// loader-local _concert.Initiate() call with a stack-compatible SNLF wrapper so
    /// normal concert creation and every other Initiate() caller remain untouched.
    /// </summary>
    [HarmonyPatch]
    internal static class ConcertFinishDate_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SEvent_Concerts),
                nameof(SEvent_Concerts.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                ConcertFinishDatePatchHealth.ReportFailure(
                    "SEvent_Concerts.LoadFunction() could not be resolved with the audited public void signature");
                throw new MissingMethodException(
                    typeof(SEvent_Concerts).FullName,
                    nameof(SEvent_Concerts.LoadFunction));
            }

            ConcertFinishDatePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo vanillaInitiate = AccessTools.Method(
                typeof(SEvent_Concerts._concert),
                nameof(SEvent_Concerts._concert.Initiate),
                Type.EmptyTypes);
            MethodInfo replacement = AccessTools.Method(
                typeof(ConcertFinishDateRepair),
                nameof(ConcertFinishDateRepair.InitiateLoadedConcertPreservingFinishDate),
                new[] { typeof(SEvent_Concerts._concert) });

            List<CodeInstruction> result = new List<CodeInstruction>(instructions);
            if (vanillaInitiate == null || replacement == null)
            {
                ConcertFinishDatePatchHealth.ReportFailure(
                    "A07 could not resolve the vanilla Initiate() call or SNLF replacement wrapper");
                return result;
            }

            int observed = 0;
            for (int index = 0; index < result.Count; index++)
            {
                if (result[index].Calls(vanillaInitiate))
                {
                    observed++;
                }
            }

            ConcertFinishDatePatchHealth.ReportInitiateSites(observed);
            if (observed != 1)
            {
                // Fail closed: do not partially rewrite a changed loader shape.
                return result;
            }

            for (int index = 0; index < result.Count; index++)
            {
                CodeInstruction instruction = result[index];
                if (!instruction.Calls(vanillaInitiate))
                {
                    continue;
                }

                // The original instance call consumes one _concert from the stack and
                // returns void. The static wrapper has the same stack contract.
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }

            return result;
        }
    }
}
