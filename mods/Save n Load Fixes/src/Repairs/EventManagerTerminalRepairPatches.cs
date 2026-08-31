using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Canonicalizes the resultless ConcludeEvent path inside the original method
    /// body, before Harmony Postfixes observe terminal state. The frozen method contains
    /// two returns; only the first, source-proven no-results early return, is modified.
    /// The ordinary final return remains untouched.
    /// </summary>
    [HarmonyPatch]
    internal static class EventManagerConcludeTerminal_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Event_Manager),
                nameof(Event_Manager.ConcludeEvent),
                new[] { typeof(Event_Manager._randomEvent._reply) });

            if (method == null || method.ReturnType != typeof(void))
            {
                EventManagerTerminalPatchHealth.ReportFailure(
                    "Event_Manager.ConcludeEvent(_reply) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Event_Manager).FullName, nameof(Event_Manager.ConcludeEvent));
            }

            EventManagerTerminalPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> original = new List<CodeInstruction>(instructions);
            MethodInfo helper = AccessTools.Method(
                typeof(EventManagerTerminalRepair),
                nameof(EventManagerTerminalRepair.CompleteResultlessEventBeforeReturn),
                new[] { typeof(Event_Manager) });

            if (helper == null)
            {
                EventManagerTerminalPatchHealth.ReportFailure(
                    "N13 resultless-terminal helper could not be resolved.");
                return original;
            }

            int observed = 0;
            foreach (CodeInstruction instruction in original)
            {
                if (instruction.opcode == OpCodes.Ret)
                {
                    observed++;
                }
            }

            EventManagerTerminalPatchHealth.ReportConcludeReturnSites(observed);
            if (observed != EventManagerTerminalPatchHealth.ExpectedConcludeReturnSiteCount)
            {
                return original;
            }

            List<CodeInstruction> patched = new List<CodeInstruction>();
            int returnOrdinal = 0;
            foreach (CodeInstruction instruction in original)
            {
                if (instruction.opcode != OpCodes.Ret)
                {
                    patched.Add(instruction);
                    continue;
                }

                returnOrdinal++;
                if (returnOrdinal != 1)
                {
                    // The second/final return belongs to vanilla's ordinary result-bearing
                    // path and remains byte-for-byte semantically untouched by N13.
                    patched.Add(instruction);
                    continue;
                }

                CodeInstruction loadManager = new CodeInstruction(OpCodes.Ldarg_0);

                // Branches targeting the audited early ret must enter the repair helper.
                // Move labels/exception-block metadata to the first inserted opcode.
                loadManager.labels.AddRange(instruction.labels);
                instruction.labels.Clear();
                loadManager.blocks.AddRange(instruction.blocks);
                instruction.blocks.Clear();

                patched.Add(loadManager);
                patched.Add(new CodeInstruction(OpCodes.Call, helper));
                patched.Add(instruction);
            }

            return patched;
        }
    }

    /// <summary>
    /// Repairs only the automatic SNS-only branch of Event_Manager.OpenPopup's
    /// generated iterator, immediately after its one Event_Manager.AddSNS call.
    /// </summary>
    [HarmonyPatch]
    internal static class EventManagerSnsOnlyTerminal_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(Event_Manager).GetNestedType(
                "<OpenPopup>d__46",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                EventManagerTerminalPatchHealth.ReportFailure(
                    "Event_Manager.<OpenPopup>d__46 generated iterator type could not be resolved.");
                throw new MissingMemberException(typeof(Event_Manager).FullName, "<OpenPopup>d__46");
            }

            MethodInfo moveNext = FindMoveNext(iteratorType);
            if (moveNext == null)
            {
                EventManagerTerminalPatchHealth.ReportFailure(
                    "Event_Manager.<OpenPopup>d__46.MoveNext could not be resolved.");
                throw new MissingMethodException(iteratorType.FullName, "MoveNext");
            }

            EventManagerTerminalPatchHealth.ReportTargetResolved();
            return moveNext;
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            List<CodeInstruction> original = new List<CodeInstruction>(instructions);
            MethodInfo addSns = AccessTools.Method(typeof(Event_Manager), nameof(Event_Manager.AddSNS));
            MethodInfo helper = AccessTools.Method(
                typeof(EventManagerTerminalRepair),
                nameof(EventManagerTerminalRepair.CompleteSnsOnlyEventAfterDelivery),
                new[] { typeof(Event_Manager._activeEvent) });
            FieldInfo activeEventField = AccessTools.Field(
                __originalMethod.DeclaringType,
                "activeEvent");

            if (addSns == null || helper == null || activeEventField == null ||
                activeEventField.FieldType != typeof(Event_Manager._activeEvent))
            {
                EventManagerTerminalPatchHealth.ReportFailure(
                    "N13 could not resolve Event_Manager.AddSNS, the SNS terminal helper, or the generated activeEvent field.");
                return original;
            }

            int observed = 0;
            foreach (CodeInstruction instruction in original)
            {
                if (instruction.Calls(addSns))
                {
                    observed++;
                }
            }

            EventManagerTerminalPatchHealth.ReportSnsDeliverySites(observed);
            if (observed != EventManagerTerminalPatchHealth.ExpectedSnsDeliverySiteCount)
            {
                return original;
            }

            List<CodeInstruction> patched = new List<CodeInstruction>();
            foreach (CodeInstruction instruction in original)
            {
                patched.Add(instruction);
                if (!instruction.Calls(addSns))
                {
                    continue;
                }

                patched.Add(new CodeInstruction(OpCodes.Ldarg_0));
                patched.Add(new CodeInstruction(OpCodes.Ldfld, activeEventField));
                patched.Add(new CodeInstruction(OpCodes.Call, helper));
            }

            return patched;
        }

        private static MethodInfo FindMoveNext(Type iteratorType)
        {
            foreach (MethodInfo method in iteratorType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
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
}
