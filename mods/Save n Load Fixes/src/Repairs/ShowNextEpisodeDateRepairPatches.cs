using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A26 patches only Shows._show.GetNextEpisodeDate(DateTime). Vanilla's first
    /// `_start.AddDays(1.0)` leaves the returned DateTime on the stack and immediately
    /// pops it. The later weekday-loop AddDays call is already assigned correctly.
    ///
    /// This transpiler changes only that one Pop into Starg_S for argument 1 (_start),
    /// preserving the original DateTime.AddDays call, its 1-day constant, the weekday
    /// loop, overload behavior, and GetCancelationDate() consumer unchanged.
    /// </summary>
    [HarmonyPatch]
    internal static class ShowNextEpisodeDate_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Shows._show),
                nameof(Shows._show.GetNextEpisodeDate),
                new[] { typeof(DateTime) });

            if (method == null || method.ReturnType != typeof(DateTime))
            {
                ShowNextEpisodeDatePatchHealth.ReportFailure(
                    "Shows._show.GetNextEpisodeDate(DateTime) could not be resolved with the audited signature");
                throw new MissingMethodException(
                    typeof(Shows._show).FullName,
                    nameof(Shows._show.GetNextEpisodeDate));
            }

            ShowNextEpisodeDatePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo addDays = AccessTools.Method(
                typeof(DateTime),
                nameof(DateTime.AddDays),
                new[] { typeof(double) });

            List<CodeInstruction> result = new List<CodeInstruction>(instructions);
            if (addDays == null)
            {
                ShowNextEpisodeDatePatchHealth.ReportFailure(
                    "A26 could not resolve DateTime.AddDays(double)");
                return result;
            }

            List<int> discardedResultPops = new List<int>();
            for (int index = 0; index + 1 < result.Count; index++)
            {
                if (result[index].Calls(addDays) && result[index + 1].opcode == OpCodes.Pop)
                {
                    discardedResultPops.Add(index + 1);
                }
            }

            ShowNextEpisodeDatePatchHealth.ReportDiscardedAdvanceSites(discardedResultPops.Count);
            if (discardedResultPops.Count !=
                ShowNextEpisodeDatePatchHealth.ExpectedDiscardedAdvanceSiteCount)
            {
                // Fail closed: a changed method shape remains completely vanilla rather
                // than partially rewriting a different AddDays call.
                return result;
            }

            CodeInstruction discardedResult = result[discardedResultPops[0]];

            // Instance method argument layout is: arg0 = this, arg1 = DateTime _start.
            // Reusing the original Pop instruction preserves any labels/exception blocks
            // attached to that exact IL position while making the immutable result live.
            discardedResult.opcode = OpCodes.Starg_S;
            discardedResult.operand = (byte)1;

            return result;
        }
    }
}
