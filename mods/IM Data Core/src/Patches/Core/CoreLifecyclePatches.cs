using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Initializes IM Data Core once popup systems are active in gameplay scenes.
    /// </summary>
    [HarmonyPatch(
        typeof(PopupManager),
        CoreConstants.HarmonyPopupManagerStartMethodName)]
    internal static class PopupManager_Start_IMDataCoreBootstrap_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            IMDataCoreController.Instance.BootstrapIfNeeded();
        }
    }

    /// <summary>
    /// Detaches storage from the previous playthrough before a new game starts.
    /// </summary>
    [HarmonyPatch(
        typeof(MainMenu_LoadGameManager),
        nameof(MainMenu_LoadGameManager.StartNewGame))]
    internal static class MainMenu_LoadGameManager_StartNewGame_IMDataCoreScopeReset_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            IMDataCoreController.Instance.OnNewGameStarting();
        }
    }

    /// <summary>
    /// Persists buffered records before vanilla dispatches its save event.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CallSaveEvent))]
    internal static class SaveManager_CallSaveEvent_IMDataCoreFlush_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            IMDataCoreController.Instance.ForceFlushBeforeSave();
        }
    }

    /// <summary>
    /// Commits save-scope persistence immediately after vanilla successfully schedules
    /// a SavedData write. Patching DataSaver's constructed generic is unsafe on Mono,
    /// where reference-type generic instantiations can share native code.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataWrite_IMDataCoreSaveScope_Patch
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
            MethodInfo commitSaveWriteMethod = AccessTools.Method(
                typeof(CoreSaveLifecycleBinding),
                nameof(CoreSaveLifecycleBinding.CommitSaveWrite),
                new Type[] { typeof(string), typeof(bool) });
            int injectedWriteCount = CoreConstants.ZeroBasedListStartIndex;

            foreach (CodeInstruction instruction in instructions)
            {
                if (!IsSavedDataWrite(instruction))
                {
                    yield return instruction;
                    continue;
                }

                // The four DataSaver arguments are already on the evaluation stack.
                // Preserve them, call vanilla unchanged, and commit only after that call
                // returns successfully. Exceptions leave the prior scope untouched.
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
                yield return new CodeInstruction(
                    OpCodes.Ldloc,
                    dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, isJsonLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return instruction;
                yield return new CodeInstruction(
                    OpCodes.Ldloc,
                    dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return new CodeInstruction(
                    OpCodes.Call,
                    commitSaveWriteMethod);
                injectedWriteCount++;
            }

            if (injectedWriteCount != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one SavedData write in " +
                    (__originalMethod == null
                        ? "an unknown vanilla save caller."
                        : __originalMethod.DeclaringType.FullName +
                          "." +
                          __originalMethod.Name +
                          "."));
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
                parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(SaveManager.SavedData) &&
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
    }

    /// <summary>
    /// Shared commit entry point for exact, non-generic vanilla save call sites.
    /// </summary>
    internal static class CoreSaveLifecycleBinding
    {
        internal static void CommitSaveWrite(
            string dataFileName,
            bool fullPath)
        {
            string savePath =
                CoreSaveFilePathResolver.ResolveDataSaverWritePath(
                    dataFileName,
                    fullPath);
            if (!CorePaths.IsSupportedGameSavePath(savePath))
            {
                return;
            }

            IMDataCoreController.Instance.OnSaveWriteStarting(savePath);
            IMDataCoreController.Instance.ForceFlushBeforeSave();
        }
    }

    /// <summary>
    /// Reproduces the path construction in non-generic vanilla save/load callers.
    /// </summary>
    internal static class CoreSaveFilePathResolver
    {
        private const string StoryModeFolderName = "story_mode";
        private const string DataFolderName = "data";
        private const string ManualSaveFileName = "manual_save";
        private const string SaveFileExtension = ".json";

        internal static string ResolveDataSaverWritePath(
            string dataFileName,
            bool fullPath)
        {
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return string.Empty;
            }

            try
            {
                string candidatePath = dataFileName;
                if (!fullPath)
                {
                    candidatePath = Path.Combine(
                        Application.persistentDataPath,
                        DataFolderName,
                        dataFileName + SaveFileExtension);
                }

                return Path.GetFullPath(candidatePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string ResolveManualLoadPath()
        {
            if (staticVars.PlayerData != null && staticVars.IsStoryMode())
            {
                return Path.Combine(
                    StoryModeFolderName,
                    staticVars.PlayerData.GetSaveFolderName(),
                    ManualSaveFileName);
            }

            return ManualSaveFileName;
        }

    }

    /// <summary>
    /// Captures PlayerData immediately after the non-generic SaveManager caller assigns
    /// the deserialized save, before vanilla invokes LoadEvent subscribers.
    /// </summary>
    internal static class CoreSaveLoadDataCaptureTranspiler
    {
        internal static IEnumerable<CodeInstruction> Inject(
            IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo saveDataField = AccessTools.Field(
                typeof(SaveManager),
                nameof(SaveManager.Data));
            MethodInfo captureMethod = AccessTools.Method(
                typeof(CoreSaveLoadDataCaptureTranspiler),
                nameof(CaptureLoadedSaveData));

            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld &&
                    Equals(instruction.operand, saveDataField))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        captureMethod);
                }
            }
        }

        private static void CaptureLoadedSaveData(SaveManager saveManager)
        {
            if (saveManager == null || saveManager.Data == null)
            {
                return;
            }

            IMDataCoreController.Instance.OnVanillaSaveDataRead(
                saveManager.Data);
        }
    }

    internal sealed class CoreSaveLoadPatchState
    {
        internal CoreSaveScope PreviousSaveScope;
        internal string RequestedSavePath = string.Empty;
        internal bool PreparationStarted;
    }

    /// <summary>
    /// Binds storage to the explicit save path loaded by vanilla.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.LoadData),
        new Type[] { typeof(string) })]
    internal static class SaveManager_LoadDataPath_IMDataCoreSaveScope_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            string path,
            ref CoreSaveLoadPatchState __state)
        {
            __state = new CoreSaveLoadPatchState
            {
                PreviousSaveScope =
                    IMDataCoreController.Instance.CaptureActiveSaveScope(),
                RequestedSavePath = path ?? string.Empty
            };

            if (!CorePaths.IsSupportedGameSavePath(path))
            {
                return;
            }

            IMDataCoreController.Instance.OnSaveLoadStarting(path);
            __state.PreparationStarted = true;
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return CoreSaveLoadDataCaptureTranspiler.Inject(instructions);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            SaveManager __instance,
            CoreSaveLoadPatchState __state)
        {
            if (__state == null || !__state.PreparationStarted)
            {
                return;
            }

            if (__instance == null || __instance.Data == null)
            {
                IMDataCoreController.Instance.OnSaveLoadFailed(
                    __state.PreviousSaveScope);
                return;
            }

            IMDataCoreController.Instance.OnVanillaSaveDataRead(
                __instance.Data);
            IMDataCoreController.Instance.OnSaveLoaded(
                __state.RequestedSavePath);
        }

        [HarmonyFinalizer]
        private static void Finalizer(
            Exception __exception,
            CoreSaveLoadPatchState __state)
        {
            if (__exception != null &&
                __state != null &&
                __state.PreparationStarted)
            {
                IMDataCoreController.Instance.OnSaveLoadFailed(
                    __state.PreviousSaveScope);
            }
        }
    }

    /// <summary>
    /// Receives the autosave path from vanilla's one and only autosave scan.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetLatestAutosavePath))]
    internal static class SaveManager_GetLatestAutosavePath_IMDataCoreSaveScope_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(string __result)
        {
            SaveManager_LoadDataAutoFlag_IMDataCoreSaveScope_Patch
                .CaptureResolvedAutosavePath(__result);
        }
    }

    /// <summary>
    /// Binds storage for the auto/manual LoadData overload without calling
    /// GetLatestAutosavePath a second time from a prefix.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.LoadData),
        new Type[] { typeof(bool) })]
    internal static class SaveManager_LoadDataAutoFlag_IMDataCoreSaveScope_Patch
    {
        [ThreadStatic]
        private static CoreSaveLoadPatchState pendingAutosaveLoadState;

        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            bool autoSave,
            ref CoreSaveLoadPatchState __state)
        {
            __state = new CoreSaveLoadPatchState
            {
                PreviousSaveScope =
                    IMDataCoreController.Instance.CaptureActiveSaveScope()
            };

            if (autoSave)
            {
                pendingAutosaveLoadState = __state;
                return;
            }

            PrepareLoad(
                __state,
                CoreSaveFilePathResolver.ResolveManualLoadPath());
        }

        internal static void CaptureResolvedAutosavePath(string savePath)
        {
            CoreSaveLoadPatchState loadState = pendingAutosaveLoadState;
            if (loadState == null || loadState.PreparationStarted)
            {
                return;
            }

            PrepareLoad(loadState, savePath);
        }

        private static void PrepareLoad(
            CoreSaveLoadPatchState loadState,
            string savePath)
        {
            if (loadState == null ||
                !CorePaths.IsSupportedGameSavePath(savePath))
            {
                return;
            }

            loadState.RequestedSavePath = savePath;
            IMDataCoreController.Instance.OnSaveLoadStarting(savePath);
            loadState.PreparationStarted = true;
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return CoreSaveLoadDataCaptureTranspiler.Inject(instructions);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            SaveManager __instance,
            CoreSaveLoadPatchState __state)
        {
            ClearPendingAutosaveLoadState(__state);
            if (__state == null || !__state.PreparationStarted)
            {
                return;
            }

            if (__instance == null || __instance.Data == null)
            {
                IMDataCoreController.Instance.OnSaveLoadFailed(
                    __state.PreviousSaveScope);
                return;
            }

            IMDataCoreController.Instance.OnVanillaSaveDataRead(
                __instance.Data);
            IMDataCoreController.Instance.OnSaveLoaded(
                __state.RequestedSavePath);
        }

        [HarmonyFinalizer]
        private static void Finalizer(
            Exception __exception,
            CoreSaveLoadPatchState __state)
        {
            ClearPendingAutosaveLoadState(__state);
            if (__exception != null &&
                __state != null &&
                __state.PreparationStarted)
            {
                IMDataCoreController.Instance.OnSaveLoadFailed(
                    __state.PreviousSaveScope);
            }
        }

        private static void ClearPendingAutosaveLoadState(
            CoreSaveLoadPatchState completedState)
        {
            if (ReferenceEquals(
                pendingAutosaveLoadState,
                completedState))
            {
                pendingAutosaveLoadState = null;
            }
        }
    }
}
