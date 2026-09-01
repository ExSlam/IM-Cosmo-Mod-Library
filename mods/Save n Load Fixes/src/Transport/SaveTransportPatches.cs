using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Transport
{
    internal static class TransportPatchHelpers
    {
        private const int OpaqueRecomposition = -1;

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

        internal static bool IsFileWriteAllText(
            CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction == null
                ? null
                : instruction.operand as MethodInfo;
            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(File) ||
                !string.Equals(calledMethod.Name, nameof(File.WriteAllText), StringComparison.Ordinal) ||
                !calledMethod.IsStatic ||
                calledMethod.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = calledMethod.GetParameters();
            return parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType == typeof(string);
        }

        internal static bool IsParameterlessInstanceToString(
            CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction == null
                ? null
                : instruction.operand as MethodInfo;
            return calledMethod != null &&
                !calledMethod.IsStatic &&
                string.Equals(
                    calledMethod.Name,
                    nameof(object.ToString),
                    StringComparison.Ordinal) &&
                calledMethod.ReturnType == typeof(string) &&
                calledMethod.GetParameters().Length == 0;
        }

        internal static bool IsExactStaticCall(
            CodeInstruction instruction,
            MethodInfo expectedMethod)
        {
            MethodInfo calledMethod = instruction == null
                ? null
                : instruction.operand as MethodInfo;

            if (calledMethod == null || expectedMethod == null ||
                instruction.opcode != OpCodes.Call ||
                calledMethod.DeclaringType != expectedMethod.DeclaringType ||
                !string.Equals(calledMethod.Name, expectedMethod.Name, StringComparison.Ordinal) ||
                calledMethod.ReturnType != expectedMethod.ReturnType)
            {
                return false;
            }

            ParameterInfo[] calledParameters = calledMethod.GetParameters();
            ParameterInfo[] expectedParameters = expectedMethod.GetParameters();
            if (calledParameters.Length != expectedParameters.Length)
            {
                return false;
            }

            for (int index = 0; index < calledParameters.Length; index++)
            {
                if (calledParameters[index].ParameterType != expectedParameters[index].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int RewriteOrRecognize(
            List<CodeInstruction> instructions,
            Func<CodeInstruction, bool> isVanillaSite,
            MethodInfo replacement,
            int expectedCount,
            MethodBase originalMethod)
        {
            int vanillaCount = 0;
            int replacementCount = 0;

            for (int index = 0; index < instructions.Count; index++)
            {
                CodeInstruction instruction = instructions[index];
                if (isVanillaSite(instruction))
                {
                    vanillaCount++;
                }
                else if (IsExactStaticCall(instruction, replacement))
                {
                    replacementCount++;
                }
            }

            if (vanillaCount == expectedCount && replacementCount == 0)
            {
                for (int index = 0; index < instructions.Count; index++)
                {
                    if (!isVanillaSite(instructions[index]))
                    {
                        continue;
                    }

                    instructions[index].opcode = OpCodes.Call;
                    instructions[index].operand = replacement;
                }

                return expectedCount;
            }

            if (vanillaCount == 0 && replacementCount == expectedCount)
            {
                // HarmonyX can re-enter this transpiler while composing a later mod.
                // The exact SNLF call shape is already installed, so this is the same
                // healthy logical interception and must not be wrapped or failed.
                return expectedCount;
            }

            if (vanillaCount == 0 && replacementCount == 0)
            {
                // HarmonyX can briefly expose neither call during an initial or later
                // recomposition. The health ledger treats this sentinel as pending;
                // it can never establish authority or poison a later exact result.
                return OpaqueRecomposition;
            }

            Debug.LogWarning(
                SaveNLoadFixesConstants.LogPrefix +
                "Rejected a mixed or partial transport shape in " +
                DescribeMethod(originalMethod) +
                ": " + vanillaCount.ToString(CultureInfo.InvariantCulture) +
                " untouched vanilla site(s), " +
                replacementCount.ToString(CultureInfo.InvariantCulture) +
                " exact SNLF replacement site(s), expected one complete shape of " +
                expectedCount.ToString(CultureInfo.InvariantCulture) + ".");
            return 0;
        }

        internal static int CombineRequiredRewriteResults(
            int first,
            int second)
        {
            if (first == 1 && second == 1)
            {
                return 1;
            }
            if (first == OpaqueRecomposition &&
                second == OpaqueRecomposition)
            {
                return OpaqueRecomposition;
            }
            return 0;
        }

        internal static void ReportCaller(
            TransportPatchSurface surface,
            MethodBase originalMethod,
            int replacedCount,
            int expectedCount)
        {
            string callerIdentity = BuildCallerIdentity(originalMethod);
            if (replacedCount == OpaqueRecomposition)
            {
                // HarmonyX can expose an empty/opaque intermediate before the exact
                // composed body. Record no success and no failure. A later complete
                // observation must still account for this caller before authority can
                // activate; if it never arrives, the provider remains fail-closed.
                TransportPatchHealth.ReportOpaqueRecomposition(
                    surface,
                    callerIdentity,
                    expectedCount);
                return;
            }

            bool providerActivated = TransportPatchHealth.ReportCaller(
                surface,
                callerIdentity,
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
            else if (providerActivated)
            {
                Debug.Log(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Embedded ordered transport self-check passed: 5/5 SavedData write callers, " +
                    "7/7 SavedData read callers (8 call sites), 1/1 type-preserving startup migration, " +
                    "and 1/1 GlobalData write/read callers. " +
                    "SNLF is the authoritative save transport.");
            }
        }
    }

    /// <summary>
    /// Keeps vanilla FixSaveFile's one audited val-to-_val migration while replacing
    /// only its final SimpleJSON whole-document rewrite with a raw-token-preserving
    /// implementation.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaStartupSavedDataMigration_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            return TransportPatchHelpers.RequireMethod(
                typeof(SaveManager),
                "FixSaveFile",
                new Type[] { typeof(bool) });
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(StartupSaveFileMigration),
                nameof(StartupSaveFileMigration.WriteTypePreservingMigration),
                new Type[] { typeof(string), typeof(string) });
            MethodInfo suppressReserialization = AccessTools.Method(
                typeof(StartupSaveFileMigration),
                nameof(StartupSaveFileMigration.SuppressSimpleJsonReserialization),
                new Type[] { typeof(object) });
            if (replacement == null || suppressReserialization == null)
            {
                TransportPatchHealth.ReportFailure(
                    TransportPatchSurface.StartupSavedDataMigration);
                return instructions;
            }

            List<CodeInstruction> result = new List<CodeInstruction>(instructions);
            int suppressedCount = TransportPatchHelpers.RewriteOrRecognize(
                result,
                TransportPatchHelpers.IsParameterlessInstanceToString,
                suppressReserialization,
                1,
                __originalMethod);
            int writeReplacementCount = TransportPatchHelpers.RewriteOrRecognize(
                result,
                TransportPatchHelpers.IsFileWriteAllText,
                replacement,
                1,
                __originalMethod);
            int replacedCount = TransportPatchHelpers.CombineRequiredRewriteResults(
                suppressedCount,
                writeReplacementCount);
            if (replacedCount == 0)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "FixSaveFile did not expose one complete JSONNode.ToString/File.WriteAllText " +
                    "pair. The type-preserving startup migration is not authoritative.");
            }

            TransportPatchHelpers.ReportCaller(
                TransportPatchSurface.StartupSavedDataMigration,
                __originalMethod,
                replacedCount,
                1);
            return result;
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
            int replacedCount = TransportPatchHelpers.RewriteOrRecognize(
                result,
                delegate(CodeInstruction instruction)
                {
                    return TransportPatchHelpers.IsDataSaverWriteOf(
                        instruction,
                        typeof(SaveManager.SavedData));
                },
                replacement,
                1,
                __originalMethod);

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

            int expectedCount =
                __originalMethod != null &&
                __originalMethod.DeclaringType == typeof(SaveManager) &&
                string.Equals(
                    __originalMethod.Name,
                    nameof(SaveManager.GetLatestAutosavePath),
                    StringComparison.Ordinal)
                    ? 2
                    : 1;

            int replacedCount = TransportPatchHelpers.RewriteOrRecognize(
                result,
                delegate(CodeInstruction instruction)
                {
                    return TransportPatchHelpers.IsDataSaverReadOf(
                        instruction,
                        typeof(SaveManager.SavedData));
                },
                replacement,
                expectedCount,
                __originalMethod);

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
            int replacedCount = TransportPatchHelpers.RewriteOrRecognize(
                result,
                delegate(CodeInstruction instruction)
                {
                    return TransportPatchHelpers.IsDataSaverWriteOf(
                        instruction,
                        typeof(SaveManager.GlobalData));
                },
                replacement,
                1,
                __originalMethod);

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
            int replacedCount = TransportPatchHelpers.RewriteOrRecognize(
                result,
                delegate(CodeInstruction instruction)
                {
                    return TransportPatchHelpers.IsDataSaverReadOf(
                        instruction,
                        typeof(SaveManager.GlobalData));
                },
                replacement,
                1,
                __originalMethod);

            TransportPatchHelpers.ReportCaller(
                TransportPatchSurface.GlobalDataRead,
                __originalMethod,
                replacedCount,
                1);
            return result;
        }
    }
}
