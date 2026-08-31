using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Safety
{
    /// <summary>
    /// Replaces only the SaveManager.Data field assignment in both audited career
    /// LoadData overloads. The replacement consumes the same (manager, SavedData)
    /// stack shape as stfld, so it composes independently with Sprint 1B's reader
    /// transport transpiler whether that load call is vanilla or already rewritten.
    /// </summary>
    [HarmonyPatch]
    internal static class LoadEpochCareerLoad_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo pathLoad = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(string) });
            MethodInfo autoLoad = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(bool) });

            if (pathLoad == null)
            {
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    "LoadData(string)");
            }

            if (autoLoad == null)
            {
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    "LoadData(bool)");
            }

            yield return pathLoad;
            yield return autoLoad;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            FieldInfo dataField = AccessTools.Field(
                typeof(SaveManager),
                nameof(SaveManager.Data));
            MethodInfo replacement = AccessTools.Method(
                typeof(LoadEpoch),
                nameof(LoadEpoch.AssignLoadedDataWithEpoch),
                new Type[]
                {
                    typeof(SaveManager),
                    typeof(SaveManager.SavedData)
                });

            if (dataField == null || replacement == null)
            {
                LoadEpochPatchHealth.ReportFailure(
                    __originalMethod,
                    "required field/replacement method could not be resolved");
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (instruction.opcode != OpCodes.Stfld ||
                    !object.Equals(instruction.operand, dataField))
                {
                    continue;
                }

                // stfld consumes (SaveManager instance, SavedData value) and
                // returns void. The static replacement has the identical stack
                // contract and performs the same assignment after epoch adoption.
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            LoadEpochPatchHealth.ReportCaller(
                __originalMethod,
                replacedCount);
            return result;
        }
    }
}
