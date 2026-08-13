using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Debug = UnityEngine.Debug;

namespace SaveWriteOrderingFix
{
    /// <summary>
    /// Mono-safe save interception.
    ///
    /// Idol Manager's DataSaver.saveData<T> is a generic method. Harmony documents
    /// that reference-type generic instantiations can share runtime code, so this mod
    /// never Harmony-patches DataSaver<SavedData> directly. Instead it replaces only
    /// the known concrete vanilla SavedData call sites.
    ///
    /// Priority.Last plus HarmonyAfter for the two Cosmo supplemental persistence mods
    /// allows their caller-level transpilers to prepare sidecars/checkpoints first.
    /// They leave the original DataSaver call in place, which this final transpiler
    /// then replaces with the ordered writer using the exact same four arguments.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataWrite_SaveWriteOrdering_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveData),
                new Type[] { typeof(bool), typeof(bool) });

            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveChapter),
                new Type[] { typeof(tasks._chapter) });

            yield return RequireMethod(
                typeof(Popup_Save),
                "Save",
                Type.EmptyTypes);

            yield return RequireMethod(
                typeof(Popup_Load_Story),
                "Do_Overwrite_Save",
                new Type[] { typeof(Popup_Load_Story.save_info) });

            yield return RequireMethod(
                typeof(Popup_Load_Story),
                nameof(Popup_Load_Story.Do_New_Save),
                new Type[] { typeof(string) });
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(OrderedSaveCoordinator),
                nameof(OrderedSaveCoordinator.QueueSavedDataWrite),
                new Type[]
                {
                    typeof(SaveManager.SavedData),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });

            if (replacement == null)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Could not resolve the ordered save replacement. " +
                    "Leaving vanilla caller unchanged.");
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);

            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (!IsSavedDataWrite(instruction))
                {
                    continue;
                }

                // Same static signature and void return type as the closed generic
                // DataSaver call at this concrete call site, so the evaluation stack,
                // labels, and exception blocks remain untouched.
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            if (replacedCount != 1)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Expected exactly one vanilla SavedData write in " +
                    DescribeMethod(__originalMethod) +
                    " but found " +
                    replacedCount.ToString() +
                    ". The method was left " +
                    (replacedCount == 0
                        ? "without a Save Write Ordering replacement."
                        : "with every matching SavedData call ordered."));
            }

            return result;
        }

        private static bool IsSavedDataWrite(
            CodeInstruction instruction)
        {
            MethodInfo calledMethod =
                instruction == null
                    ? null
                    : instruction.operand as MethodInfo;

            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(DataSaver) ||
                !string.Equals(
                    calledMethod.Name,
                    SaveWriteOrderingConstants.DataSaverSaveMethodName,
                    StringComparison.Ordinal) ||
                !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments =
                calledMethod.GetGenericArguments();
            ParameterInfo[] parameters =
                calledMethod.GetParameters();

            return genericArguments.Length == 1 &&
                   genericArguments[0] ==
                       typeof(SaveManager.SavedData) &&
                   parameters.Length == 4 &&
                   parameters[0].ParameterType ==
                       typeof(SaveManager.SavedData) &&
                   parameters[1].ParameterType == typeof(string) &&
                   parameters[2].ParameterType == typeof(bool) &&
                   parameters[3].ParameterType == typeof(bool);
        }

        private static MethodBase RequireMethod(
            Type declaringType,
            string methodName,
            Type[] parameterTypes)
        {
            MethodInfo method = AccessTools.Method(
                declaringType,
                methodName,
                parameterTypes);

            if (method == null)
            {
                throw new MissingMethodException(
                    declaringType.FullName,
                    methodName);
            }

            return method;
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "an unknown vanilla save caller";
            }

            return method.DeclaringType.FullName +
                   "." +
                   method.Name;
        }
    }

    /// <summary>
    /// Mono-safe read coordination.
    ///
    /// Every concrete vanilla call site that reads SaveManager.SavedData is patched
    /// rather than DataSaver.loadData<SavedData> itself. This includes actual game
    /// loading plus autosave/manual/story save-list reads, so UI inspection cannot
    /// race an ordered write still in flight.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataRead_SaveWriteOrdering_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.GetLatestAutosavePath),
                Type.EmptyTypes);

            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(string) });

            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(bool) });

            yield return RequireMethod(
                typeof(Popup_Load_Story),
                "Get_Playthrough_Info",
                new Type[] { typeof(string) });

            yield return RequireMethod(
                typeof(Popup_Load_Story),
                "Get_Saves",
                new Type[] { typeof(Popup_Load_Story.playthrough_info) });

            yield return RequireMethod(
                typeof(Popup_Save._save_data),
                nameof(Popup_Save._save_data.Set),
                new Type[] { typeof(string) });

            yield return RequireMethod(
                typeof(Popup_Save._save_data),
                nameof(Popup_Save._save_data.SetAutosave),
                Type.EmptyTypes);
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(OrderedSaveCoordinator),
                nameof(
                    OrderedSaveCoordinator
                        .LoadSavedDataAfterPendingWrites),
                new Type[] { typeof(string) });

            if (replacement == null)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Could not resolve the coordinated SavedData reader. " +
                    "Leaving vanilla caller unchanged.");
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);

            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (!IsSavedDataRead(instruction))
                {
                    continue;
                }

                // Closed DataSaver.loadData<SavedData>(string) and the replacement
                // both consume one string and return SaveManager.SavedData.
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            if (replacedCount == 0)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "No vanilla SavedData reads were found in " +
                    DescribeMethod(__originalMethod) +
                    ". The method was left unchanged.");
            }

            return result;
        }

        private static bool IsSavedDataRead(
            CodeInstruction instruction)
        {
            MethodInfo calledMethod =
                instruction == null
                    ? null
                    : instruction.operand as MethodInfo;

            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(DataSaver) ||
                !string.Equals(
                    calledMethod.Name,
                    SaveWriteOrderingConstants.DataSaverLoadMethodName,
                    StringComparison.Ordinal) ||
                !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments =
                calledMethod.GetGenericArguments();
            ParameterInfo[] parameters =
                calledMethod.GetParameters();

            return genericArguments.Length == 1 &&
                   genericArguments[0] ==
                       typeof(SaveManager.SavedData) &&
                   parameters.Length == 1 &&
                   parameters[0].ParameterType == typeof(string) &&
                   calledMethod.ReturnType ==
                       typeof(SaveManager.SavedData);
        }

        private static MethodBase RequireMethod(
            Type declaringType,
            string methodName,
            Type[] parameterTypes)
        {
            MethodInfo method = AccessTools.Method(
                declaringType,
                methodName,
                parameterTypes);

            if (method == null)
            {
                throw new MissingMethodException(
                    declaringType.FullName,
                    methodName);
            }

            return method;
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "an unknown vanilla read caller";
            }

            return method.DeclaringType.FullName +
                   "." +
                   method.Name;
        }
    }
}
