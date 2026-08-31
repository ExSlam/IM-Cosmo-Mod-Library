using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Transport
{
    internal static class TransportPatchHelpers
    {
        internal static string BuildCallerIdentity(MethodBase method)
        {
            if (method == null)
            {
                return "<unknown>";
            }

            Type declaringType = method.DeclaringType;
            return string.Concat(
                declaringType != null ? declaringType.FullName : "<global>",
                "::",
                method.Name,
                "::",
                method.MetadataToken.ToString(CultureInfo.InvariantCulture));
        }

        internal static MethodBase RequireMethod(
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

        internal static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "an unknown vanilla transport caller";
            }

            return method.DeclaringType.FullName + "." + method.Name;
        }

        internal static bool IsDataSaverWriteOf(
            CodeInstruction instruction,
            Type dataType)
        {
            MethodInfo calledMethod = instruction == null
                ? null
                : instruction.operand as MethodInfo;

            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(DataSaver) ||
                !string.Equals(
                    calledMethod.Name,
                    SaveNLoadFixesConstants.DataSaverSaveMethodName,
                    StringComparison.Ordinal) ||
                !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments = calledMethod.GetGenericArguments();
            ParameterInfo[] parameters = calledMethod.GetParameters();

            return genericArguments.Length == 1 &&
                   genericArguments[0] == dataType &&
                   parameters.Length == 4 &&
                   parameters[0].ParameterType == dataType &&
                   parameters[1].ParameterType == typeof(string) &&
                   parameters[2].ParameterType == typeof(bool) &&
                   parameters[3].ParameterType == typeof(bool) &&
                   calledMethod.ReturnType == typeof(void);
        }

        internal static bool IsDataSaverReadOf(
            CodeInstruction instruction,
            Type dataType)
        {
            MethodInfo calledMethod = instruction == null
                ? null
                : instruction.operand as MethodInfo;

            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(DataSaver) ||
                !string.Equals(
                    calledMethod.Name,
                    SaveNLoadFixesConstants.DataSaverLoadMethodName,
                    StringComparison.Ordinal) ||
                !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments = calledMethod.GetGenericArguments();
            ParameterInfo[] parameters = calledMethod.GetParameters();

            return genericArguments.Length == 1 &&
                   genericArguments[0] == dataType &&
                   parameters.Length == 1 &&
                   parameters[0].ParameterType == typeof(string) &&
                   calledMethod.ReturnType == dataType;
        }

        internal static void ReportCaller(
            TransportPatchSurface surface,
            MethodBase originalMethod,
            int replacedCount,
            int expectedCount)
        {
            TransportPatchHealth.ReportCaller(
                surface,
                BuildCallerIdentity(originalMethod),
                replacedCount,
                expectedCount);

            if (replacedCount != expectedCount)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Expected " +
                    expectedCount.ToString(CultureInfo.InvariantCulture) +
                    " transport call site(s) in " +
                    DescribeMethod(originalMethod) +
                    " but found " +
                    replacedCount.ToString(CultureInfo.InvariantCulture) +
                    ". Embedded transport health is not authoritative.");
            }
        }
    }

    /// <summary>
    /// Replaces only the five audited concrete SavedData write call sites.
    /// Never patch DataSaver.saveData&lt;T&gt; itself: reference-type generic sharing on
    /// Mono makes caller-level replacement the safe interception boundary.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataWrite_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveData),
                new Type[] { typeof(bool), typeof(bool) });

            yield return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveChapter),
                new Type[] { typeof(tasks._chapter) });

            yield return TransportPatchHelpers.RequireMethod(
                typeof(Popup_Save),
                "Save",
                Type.EmptyTypes);

            yield return TransportPatchHelpers.RequireMethod(
                typeof(Popup_Load_Story),
                "Do_Overwrite_Save",
                new Type[] { typeof(Popup_Load_Story.save_info) });

            yield return TransportPatchHelpers.RequireMethod(
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
                typeof(OrderedSaveTransport),
                nameof(OrderedSaveTransport.QueueSavedDataWrite),
                new Type[]
                {
                    typeof(SaveManager.SavedData),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });

            if (replacement == null)
            {
                TransportPatchHealth.ReportFailure(
                    TransportPatchSurface.SavedDataWrite);
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (!TransportPatchHelpers.IsDataSaverWriteOf(
                        instruction,
                        typeof(SaveManager.SavedData)))
                {
                    continue;
                }

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            TransportPatchHelpers.ReportCaller(
                TransportPatchSurface.SavedDataWrite,
                __originalMethod,
                replacedCount,
                1);
            return result;
        }
    }

    /// <summary>
    /// Coordinates all seven audited SavedData reader callers, including both
    /// loadData&lt;SavedData&gt; sites inside GetLatestAutosavePath.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataRead_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.GetLatestAutosavePath),
                Type.EmptyTypes);

            yield return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(string) });

            yield return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(bool) });

            yield return TransportPatchHelpers.RequireMethod(
                typeof(Popup_Load_Story),
                "Get_Playthrough_Info",
                new Type[] { typeof(string) });

            yield return TransportPatchHelpers.RequireMethod(
                typeof(Popup_Load_Story),
                "Get_Saves",
                new Type[] { typeof(Popup_Load_Story.playthrough_info) });

            yield return TransportPatchHelpers.RequireMethod(
                typeof(Popup_Save._save_data),
                nameof(Popup_Save._save_data.Set),
                new Type[] { typeof(string) });

            yield return TransportPatchHelpers.RequireMethod(
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
                typeof(OrderedSaveTransport),
                nameof(OrderedSaveTransport.LoadSavedDataAfterPendingWrites),
                new Type[] { typeof(string) });

            if (replacement == null)
            {
                TransportPatchHealth.ReportFailure(
                    TransportPatchSurface.SavedDataRead);
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (!TransportPatchHelpers.IsDataSaverReadOf(
                        instruction,
                        typeof(SaveManager.SavedData)))
                {
                    continue;
                }

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            int expectedCount =
                __originalMethod != null &&
                __originalMethod.DeclaringType == typeof(SaveManager) &&
                string.Equals(
                    __originalMethod.Name,
                    nameof(SaveManager.GetLatestAutosavePath),
                    StringComparison.Ordinal)
                    ? 2
                    : 1;

            TransportPatchHelpers.ReportCaller(
                TransportPatchSurface.SavedDataRead,
                __originalMethod,
                replacedCount,
                expectedCount);
            return result;
        }
    }

    [HarmonyPatch]
    internal static class VanillaGlobalDataWrite_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveGlobalData),
                Type.EmptyTypes);
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(OrderedSaveTransport),
                nameof(OrderedSaveTransport.QueueGlobalDataWrite),
                new Type[]
                {
                    typeof(SaveManager.GlobalData),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });

            if (replacement == null)
            {
                TransportPatchHealth.ReportFailure(
                    TransportPatchSurface.GlobalDataWrite);
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (!TransportPatchHelpers.IsDataSaverWriteOf(
                        instruction,
                        typeof(SaveManager.GlobalData)))
                {
                    continue;
                }

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            TransportPatchHelpers.ReportCaller(
                TransportPatchSurface.GlobalDataWrite,
                __originalMethod,
                replacedCount,
                1);
            return result;
        }
    }

    [HarmonyPatch]
    internal static class VanillaGlobalDataRead_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.LoadGlobalData),
                Type.EmptyTypes);
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(OrderedSaveTransport),
                nameof(OrderedSaveTransport.LoadGlobalDataAfterPendingWrites),
                new Type[] { typeof(string) });

            if (replacement == null)
            {
                TransportPatchHealth.ReportFailure(
                    TransportPatchSurface.GlobalDataRead);
                return instructions;
            }

            List<CodeInstruction> result =
                new List<CodeInstruction>(instructions);
            int replacedCount = 0;

            foreach (CodeInstruction instruction in result)
            {
                if (!TransportPatchHelpers.IsDataSaverReadOf(
                        instruction,
                        typeof(SaveManager.GlobalData)))
                {
                    continue;
                }

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacedCount++;
            }

            TransportPatchHelpers.ReportCaller(
                TransportPatchSurface.GlobalDataRead,
                __originalMethod,
                replacedCount,
                1);
            return result;
        }
    }
}
