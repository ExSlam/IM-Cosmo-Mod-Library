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
    /// Priority.Last plus HarmonyAfter allows persistence-preparation patches to run
    /// first. Save n Load Fixes is special: when its embedded transport has already
    /// rewritten a concrete call site, SWOF recognizes that exact replacement shape
    /// as delegated success and does not install a second queue owner.
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
        [HarmonyAfter(
            SaveWriteOrderingConstants.SnlHarmonyId,
            SaveWriteOrderingConstants.IMDataCoreHarmonyId,
            SaveWriteOrderingConstants.GraduationDetailsHarmonyId)]
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
                SaveWriteOrderingPatchHealth.ReportSavedDataWriteCaller(
                    __originalMethod,
                    false);
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Could not resolve the ordered save replacement. " +
                    "Leaving vanilla caller unchanged.");
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);

            int delegatedCount = CountSnlSavedDataWrites(result);
            if (delegatedCount > 0)
            {
                bool delegatedSuccess = delegatedCount == 1;
                SaveWriteOrderingPatchHealth.ReportSavedDataWriteCaller(
                    __originalMethod,
                    delegatedSuccess);
                if (!delegatedSuccess)
                {
                    Debug.LogWarning(
                        SaveWriteOrderingConstants.LogPrefix +
                        "Expected exactly one SNLF SavedData transport replacement in " +
                        DescribeMethod(__originalMethod) +
                        " but found " + delegatedCount.ToString() + ".");
                }
                return result;
            }

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

            bool success = replacedCount == 1;
            SaveWriteOrderingPatchHealth.ReportSavedDataWriteCaller(
                __originalMethod,
                success);

            if (!success)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Expected exactly one vanilla SavedData write in " +
                    DescribeMethod(__originalMethod) +
                    " but found " + replacedCount.ToString() + ".");
            }

            return result;
        }

        private static int CountSnlSavedDataWrites(
            List<CodeInstruction> instructions)
        {
            int count = 0;
            for (int i = 0; i < instructions.Count; i++)
            {
                MethodInfo calledMethod = instructions[i] == null
                    ? null
                    : instructions[i].operand as MethodInfo;
                if (SnlTransportProvider.IsEmbeddedTransportCall(
                        calledMethod,
                        "QueueSavedDataWrite",
                        typeof(void),
                        typeof(SaveManager.SavedData),
                        typeof(string),
                        typeof(bool),
                        typeof(bool)))
                {
                    count++;
                }
            }
            return count;
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
        [HarmonyAfter(SaveWriteOrderingConstants.SnlHarmonyId)]
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

            int expectedCount =
                __originalMethod != null &&
                __originalMethod.DeclaringType == typeof(SaveManager) &&
                string.Equals(
                    __originalMethod.Name,
                    nameof(SaveManager.GetLatestAutosavePath),
                    StringComparison.Ordinal)
                    ? 2
                    : 1;

            int delegatedCount = CountSnlSavedDataReads(result);
            if (delegatedCount > 0)
            {
                if (delegatedCount != expectedCount)
                {
                    Debug.LogWarning(
                        SaveWriteOrderingConstants.LogPrefix +
                        "Expected " + expectedCount.ToString() +
                        " SNLF SavedData read replacement(s) in " +
                        DescribeMethod(__originalMethod) +
                        " but found " + delegatedCount.ToString() +
                        ". SWOF will not install a second read owner in that caller.");
                }
                return result;
            }

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

            if (replacedCount != expectedCount)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Expected " + expectedCount.ToString() +
                    " vanilla SavedData read(s) in " +
                    DescribeMethod(__originalMethod) +
                    " but found " + replacedCount.ToString() + ".");
            }

            return result;
        }

        private static int CountSnlSavedDataReads(
            List<CodeInstruction> instructions)
        {
            int count = 0;
            for (int i = 0; i < instructions.Count; i++)
            {
                MethodInfo calledMethod = instructions[i] == null
                    ? null
                    : instructions[i].operand as MethodInfo;
                if (SnlTransportProvider.IsEmbeddedTransportCall(
                        calledMethod,
                        "LoadSavedDataAfterPendingWrites",
                        typeof(SaveManager.SavedData),
                        typeof(string)))
                {
                    count++;
                }
            }
            return count;
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
    /// <summary>
    /// Development 1.4 GlobalData write ordering. This patches only the one concrete
    /// SaveManager.SaveGlobalData caller and never Harmony-patches DataSaver<T>.
    /// SaveGlobalDataEvent has already populated _GlobalData before the replaced call.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaGlobalDataWrite_SaveWriteOrdering_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.SaveGlobalData),
                Type.EmptyTypes);
            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    nameof(SaveManager.SaveGlobalData));
            }
            return method;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(SaveWriteOrderingConstants.SnlHarmonyId)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(OrderedSaveCoordinator),
                nameof(OrderedSaveCoordinator.QueueGlobalDataWrite),
                new Type[]
                {
                    typeof(SaveManager.GlobalData),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });

            if (replacement == null)
            {
                SaveWriteOrderingPatchHealth.ReportGlobalDataWriteCaller(
                    __originalMethod,
                    false);
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Could not resolve the ordered GlobalData writer. Leaving vanilla caller unchanged.");
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int delegatedCount = CountSnlGlobalDataWrites(result);
            if (delegatedCount > 0)
            {
                bool delegatedSuccess = delegatedCount == 1;
                SaveWriteOrderingPatchHealth.ReportGlobalDataWriteCaller(
                    __originalMethod,
                    delegatedSuccess);
                if (!delegatedSuccess)
                {
                    Debug.LogWarning(
                        SaveWriteOrderingConstants.LogPrefix +
                        "Expected exactly one SNLF GlobalData write replacement but found " +
                        delegatedCount.ToString() + ".");
                }
                return result;
            }

            int replacedCount = 0;
            foreach (CodeInstruction instruction in result)
            {
                if (!IsGlobalDataWrite(instruction))
                {
                    continue;
                }

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            bool success = replacedCount == 1;
            SaveWriteOrderingPatchHealth.ReportGlobalDataWriteCaller(
                __originalMethod,
                success);
            if (!success)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Expected exactly one vanilla GlobalData write in SaveManager.SaveGlobalData but found " +
                    replacedCount.ToString() + ".");
            }

            return result;
        }

        private static int CountSnlGlobalDataWrites(
            List<CodeInstruction> instructions)
        {
            int count = 0;
            for (int i = 0; i < instructions.Count; i++)
            {
                MethodInfo calledMethod = instructions[i] == null
                    ? null
                    : instructions[i].operand as MethodInfo;
                if (SnlTransportProvider.IsEmbeddedTransportCall(
                        calledMethod,
                        "QueueGlobalDataWrite",
                        typeof(void),
                        typeof(SaveManager.GlobalData),
                        typeof(string),
                        typeof(bool),
                        typeof(bool)))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool IsGlobalDataWrite(CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction == null
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

            Type[] genericArguments = calledMethod.GetGenericArguments();
            ParameterInfo[] parameters = calledMethod.GetParameters();
            return genericArguments.Length == 1 &&
                genericArguments[0] == typeof(SaveManager.GlobalData) &&
                parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(SaveManager.GlobalData) &&
                parameters[1].ParameterType == typeof(string) &&
                parameters[2].ParameterType == typeof(bool) &&
                parameters[3].ParameterType == typeof(bool);
        }
    }

    /// <summary>
    /// Development 1.4 GlobalData read fence. LoadGlobalData waits only for the
    /// physical global_data.json queue and then calls vanilla DataSaver.loadData<T>.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaGlobalDataRead_SaveWriteOrdering_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadGlobalData),
                Type.EmptyTypes);
            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    nameof(SaveManager.LoadGlobalData));
            }
            return method;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(SaveWriteOrderingConstants.SnlHarmonyId)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(OrderedSaveCoordinator),
                nameof(OrderedSaveCoordinator.LoadGlobalDataAfterPendingWrites),
                new Type[] { typeof(string) });

            if (replacement == null)
            {
                SaveWriteOrderingPatchHealth.ReportGlobalDataReadCaller(
                    __originalMethod,
                    false);
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Could not resolve the coordinated GlobalData reader. Leaving vanilla caller unchanged.");
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int delegatedCount = CountSnlGlobalDataReads(result);
            if (delegatedCount > 0)
            {
                bool delegatedSuccess = delegatedCount == 1;
                SaveWriteOrderingPatchHealth.ReportGlobalDataReadCaller(
                    __originalMethod,
                    delegatedSuccess);
                if (!delegatedSuccess)
                {
                    Debug.LogWarning(
                        SaveWriteOrderingConstants.LogPrefix +
                        "Expected exactly one SNLF GlobalData read replacement but found " +
                        delegatedCount.ToString() + ".");
                }
                return result;
            }

            int replacedCount = 0;
            foreach (CodeInstruction instruction in result)
            {
                if (!IsGlobalDataRead(instruction))
                {
                    continue;
                }

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            bool success = replacedCount == 1;
            SaveWriteOrderingPatchHealth.ReportGlobalDataReadCaller(
                __originalMethod,
                success);
            if (!success)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Expected exactly one vanilla GlobalData read in SaveManager.LoadGlobalData but found " +
                    replacedCount.ToString() + ".");
            }

            return result;
        }

        private static int CountSnlGlobalDataReads(
            List<CodeInstruction> instructions)
        {
            int count = 0;
            for (int i = 0; i < instructions.Count; i++)
            {
                MethodInfo calledMethod = instructions[i] == null
                    ? null
                    : instructions[i].operand as MethodInfo;
                if (SnlTransportProvider.IsEmbeddedTransportCall(
                        calledMethod,
                        "LoadGlobalDataAfterPendingWrites",
                        typeof(SaveManager.GlobalData),
                        typeof(string)))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool IsGlobalDataRead(CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction == null
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

            Type[] genericArguments = calledMethod.GetGenericArguments();
            ParameterInfo[] parameters = calledMethod.GetParameters();
            return genericArguments.Length == 1 &&
                genericArguments[0] == typeof(SaveManager.GlobalData) &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(string) &&
                calledMethod.ReturnType == typeof(SaveManager.GlobalData);
        }
    }

}
