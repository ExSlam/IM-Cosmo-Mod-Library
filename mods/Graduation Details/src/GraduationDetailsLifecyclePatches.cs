using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace GraduationDetails
{
    /// <summary>
    /// Detaches Graduation Details from the previous save while a new game has no
    /// physical save path.
    /// </summary>
    [HarmonyPatch(
        typeof(MainMenu_LoadGameManager),
        nameof(MainMenu_LoadGameManager.StartNewGame))]
    internal static class MainMenu_LoadGameManager_StartNewGame_GraduationDetailsScopeReset_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            GraduationDetailsSaveLifecycleBinding.StartNewGame();
        }
    }

    /// <summary>
    /// Captures each concrete vanilla SavedData write at its non-generic caller.
    /// Patching DataSaver's constructed generic directly is unsafe on Mono, where
    /// reference-type generic instantiations can share native code.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataWrite_GraduationDetailsSaveScope_Patch
    {
        private const string DataSaverSaveMethodName = "saveData";

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

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase __originalMethod)
        {
            LocalBuilder dataToSaveLocal = generator.DeclareLocal(
                typeof(SaveManager.SavedData));
            LocalBuilder dataFileNameLocal = generator.DeclareLocal(
                typeof(string));
            LocalBuilder isJsonLocal = generator.DeclareLocal(typeof(bool));
            LocalBuilder fullPathLocal = generator.DeclareLocal(typeof(bool));
            MethodInfo prepareSaveWriteMethod = AccessTools.Method(
                typeof(GraduationDetailsSaveLifecycleBinding),
                nameof(GraduationDetailsSaveLifecycleBinding.PrepareSaveWrite),
                new Type[]
                {
                    typeof(SaveManager.SavedData),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });
            if (prepareSaveWriteMethod == null)
            {
                throw new MissingMethodException(
                    typeof(GraduationDetailsSaveLifecycleBinding).FullName,
                    nameof(GraduationDetailsSaveLifecycleBinding.PrepareSaveWrite));
            }

            int injectedWriteCount = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!IsSavedDataWrite(instruction))
                {
                    yield return instruction;
                    continue;
                }

                // The four DataSaver arguments are already on the evaluation stack.
                // Save them in reverse order, notify Graduation Details on the main
                // thread, then restore the exact arguments for vanilla's async call.
                CodeInstruction firstInjectedInstruction =
                    new CodeInstruction(OpCodes.Stloc, fullPathLocal);
                firstInjectedInstruction.labels.AddRange(instruction.labels);
                firstInjectedInstruction.blocks.AddRange(instruction.blocks);
                instruction.labels.Clear();
                instruction.blocks.Clear();

                yield return firstInjectedInstruction;
                yield return new CodeInstruction(OpCodes.Stloc, isJsonLocal);
                yield return new CodeInstruction(
                    OpCodes.Stloc,
                    dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Stloc, dataToSaveLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataToSaveLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, isJsonLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return new CodeInstruction(
                    OpCodes.Call,
                    prepareSaveWriteMethod);
                yield return new CodeInstruction(OpCodes.Ldloc, dataToSaveLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, isJsonLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return instruction;
                injectedWriteCount++;
            }

            if (injectedWriteCount != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one SavedData write in " +
                    DescribeMethod(__originalMethod) + "; found " +
                    injectedWriteCount.ToString() + ".");
            }
        }

        private static bool IsSavedDataWrite(CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction.operand as MethodInfo;
            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(DataSaver) ||
                !string.Equals(
                    calledMethod.Name,
                    DataSaverSaveMethodName,
                    StringComparison.Ordinal) ||
                !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments = calledMethod.GetGenericArguments();
            ParameterInfo[] parameters = calledMethod.GetParameters();
            return genericArguments.Length == 1 &&
                genericArguments[0] == typeof(SaveManager.SavedData) &&
                calledMethod.ReturnType == typeof(void) &&
                parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(SaveManager.SavedData) &&
                parameters[1].ParameterType == typeof(string) &&
                parameters[2].ParameterType == typeof(bool) &&
                parameters[3].ParameterType == typeof(bool);
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
            return method == null
                ? "an unknown vanilla caller"
                : method.DeclaringType.FullName + "." + method.Name;
        }
    }

    /// <summary>
    /// Captures the actual DataSaver load argument and restores Graduation Details
    /// immediately after vanilla assigns SaveManager.Data, before any LoadEvent
    /// subscriber can mutate the serialized save stamp.
    /// </summary>
    [HarmonyPatch]
    internal static class SaveManager_LoadData_GraduationDetailsSaveScope_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return VanillaSavedDataWrite_GraduationDetailsSaveScope_Patch
                .RequireMethod(
                    typeof(SaveManager),
                    nameof(SaveManager.LoadData),
                    new Type[] { typeof(string) });
            yield return VanillaSavedDataWrite_GraduationDetailsSaveScope_Patch
                .RequireMethod(
                    typeof(SaveManager),
                    nameof(SaveManager.LoadData),
                    new Type[] { typeof(bool) });
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase __originalMethod)
        {
            return GraduationDetailsSaveLoadDataCaptureTranspiler.Inject(
                instructions,
                generator,
                __originalMethod);
        }
    }

    internal static class GraduationDetailsSaveLoadDataCaptureTranspiler
    {
        private const string DataSaverLoadMethodName = "loadData";

        internal static IEnumerable<CodeInstruction> Inject(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase originalMethod)
        {
            LocalBuilder dataFileNameLocal = generator.DeclareLocal(
                typeof(string));
            FieldInfo saveDataField = AccessTools.Field(
                typeof(SaveManager),
                nameof(SaveManager.Data));
            MethodInfo captureMethod = AccessTools.Method(
                typeof(GraduationDetailsSaveLifecycleBinding),
                nameof(GraduationDetailsSaveLifecycleBinding.CaptureLoadedSaveData),
                new Type[] { typeof(SaveManager), typeof(string) });
            if (saveDataField == null)
            {
                throw new MissingFieldException(
                    typeof(SaveManager).FullName,
                    nameof(SaveManager.Data));
            }
            if (captureMethod == null)
            {
                throw new MissingMethodException(
                    typeof(GraduationDetailsSaveLifecycleBinding).FullName,
                    nameof(GraduationDetailsSaveLifecycleBinding.CaptureLoadedSaveData));
            }

            int interceptedReadCount = 0;
            int injectedCaptureCount = 0;
            bool savedDataReadSeen = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (IsSavedDataRead(instruction))
                {
                    // SaveManager's receiver for the following stfld is already below
                    // the string argument. Pop only the string, then restore it for the
                    // original DataSaver call so the vanilla stack remains unchanged.
                    CodeInstruction firstInjectedInstruction =
                        new CodeInstruction(OpCodes.Stloc, dataFileNameLocal);
                    firstInjectedInstruction.labels.AddRange(instruction.labels);
                    firstInjectedInstruction.blocks.AddRange(instruction.blocks);
                    instruction.labels.Clear();
                    instruction.blocks.Clear();

                    yield return firstInjectedInstruction;
                    yield return new CodeInstruction(
                        OpCodes.Ldloc,
                        dataFileNameLocal);
                    yield return instruction;
                    interceptedReadCount++;
                    savedDataReadSeen = true;
                    continue;
                }

                yield return instruction;
                if (instruction.opcode != OpCodes.Stfld ||
                    !Equals(instruction.operand, saveDataField))
                {
                    continue;
                }
                if (!savedDataReadSeen)
                {
                    throw new InvalidOperationException(
                        "SaveManager.Data was assigned before its SavedData read in " +
                        VanillaSavedDataWrite_GraduationDetailsSaveScope_Patch
                            .DescribeMethod(originalMethod) + ".");
                }

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(
                    OpCodes.Ldloc,
                    dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Call, captureMethod);
                injectedCaptureCount++;
            }

            if (interceptedReadCount != 1 || injectedCaptureCount != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one SavedData read and one SaveManager.Data " +
                    "assignment in " +
                    VanillaSavedDataWrite_GraduationDetailsSaveScope_Patch
                        .DescribeMethod(originalMethod) +
                    "; found " + interceptedReadCount.ToString() + " and " +
                    injectedCaptureCount.ToString() + ".");
            }
        }

        private static bool IsSavedDataRead(CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction.operand as MethodInfo;
            if (calledMethod == null ||
                calledMethod.DeclaringType != typeof(DataSaver) ||
                !string.Equals(
                    calledMethod.Name,
                    DataSaverLoadMethodName,
                    StringComparison.Ordinal) ||
                !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments = calledMethod.GetGenericArguments();
            ParameterInfo[] parameters = calledMethod.GetParameters();
            return genericArguments.Length == 1 &&
                genericArguments[0] == typeof(SaveManager.SavedData) &&
                calledMethod.ReturnType == typeof(SaveManager.SavedData) &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(string);
        }
    }

    /// <summary>
    /// Exception boundary between injected vanilla call sites and Graduation
    /// Details persistence. Sidecar failures must never prevent vanilla from
    /// saving, loading, or starting a new game.
    /// </summary>
    internal static class GraduationDetailsSaveLifecycleBinding
    {
        internal static void PrepareSaveWrite(
            SaveManager.SavedData savedData,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            if (savedData == null)
            {
                return;
            }

            try
            {
                GraduationDetailsPersistenceController.PrepareVanillaSaveWrite(
                    savedData,
                    dataFileName,
                    isJson,
                    fullPath);
            }
            catch (Exception exception)
            {
                WarnSafely(
                    "Graduation Details could not capture a vanilla save boundary: " +
                    exception.Message);
            }
        }

        internal static void CaptureLoadedSaveData(
            SaveManager saveManager,
            string dataFileName)
        {
            if (saveManager == null || saveManager.Data == null)
            {
                return;
            }

            try
            {
                GraduationDetailsPersistenceController.OnVanillaSaveDataRead(
                    saveManager.Data,
                    dataFileName);
            }
            catch (Exception exception)
            {
                WarnSafely(
                    "Graduation Details could not restore the loaded save sidecar: " +
                    exception.Message);
            }
        }

        internal static void StartNewGame()
        {
            try
            {
                GraduationDetailsPersistenceController.OnNewGameStarting();
            }
            catch (Exception exception)
            {
                WarnSafely(
                    "Graduation Details could not reset its new-game save scope: " +
                    exception.Message);
            }
        }

        private static void WarnSafely(string message)
        {
            try
            {
                UnityEngine.Debug.LogWarning("[Graduation Details] " + message);
            }
            catch
            {
                // Logging must not turn a sidecar failure into a vanilla failure.
            }
        }
    }
}
