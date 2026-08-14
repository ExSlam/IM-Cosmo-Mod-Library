using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

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
            try
            {
                IMDataCoreController.Instance.BootstrapIfNeeded();
            }
            catch (Exception exception)
            {
                // Supplemental initialization must never break vanilla gameplay.
                CoreLog.Warn(
                    "IM Data Core bootstrap failed without blocking vanilla: " +
                    exception.Message);
            }
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
            try
            {
                IMDataCoreController.Instance.OnNewGameStarting();
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core new-game reset failed without blocking vanilla: " +
                    exception.Message);
            }
        }
    }

    /// <summary>
    /// Persists the lightweight IMDC branch immediately before vanilla schedules
    /// its SavedData write. A detached JSON-equivalent SavedData snapshot is passed
    /// to both IMDC and DataSaver so the background vanilla serializer cannot observe
    /// later mutations of the live SaveManager.Data object.
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

            MethodInfo prepareSaveWriteMethod = AccessTools.Method(
                typeof(CoreSaveLifecycleBinding),
                nameof(CoreSaveLifecycleBinding.PrepareSaveWrite),
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
                    typeof(CoreSaveLifecycleBinding).FullName,
                    nameof(CoreSaveLifecycleBinding.PrepareSaveWrite));
            }

            MethodInfo stableSnapshotMethod = AccessTools.Method(
                typeof(VanillaSavedDataWrite_IMDataCoreSaveScope_Patch),
                nameof(CreateStableSaveSnapshot),
                new Type[] { typeof(SaveManager.SavedData) });
            if (stableSnapshotMethod == null)
            {
                throw new MissingMethodException(
                    typeof(VanillaSavedDataWrite_IMDataCoreSaveScope_Patch).FullName,
                    nameof(CreateStableSaveSnapshot));
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
                // Store them temporarily, detach the SavedData graph, let IMDC
                // persist against that snapshot, then call vanilla with the same
                // snapshot and the original target arguments.
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

                // Vanilla DataSaver serializes on a worker thread. Resolve one
                // stable save object now and pass that same object to IMDC and
                // DataSaver so their checkpoint/file identities cannot diverge.
                // CreateStableSaveSnapshot can reuse the object when Save Write
                // Ordering Fix will synchronously freeze the exact payload next.
                yield return new CodeInstruction(OpCodes.Ldloc, dataToSaveLocal);
                yield return new CodeInstruction(
                    OpCodes.Call,
                    stableSnapshotMethod);
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
                    (__originalMethod == null
                        ? "an unknown vanilla save caller."
                        : __originalMethod.DeclaringType.FullName +
                          "." +
                          __originalMethod.Name +
                          "; found " +
                          injectedWriteCount.ToString() +
                          "."));
            }
        }

        private static SaveManager.SavedData CreateStableSaveSnapshot(
            SaveManager.SavedData source)
        {
            if (source == null)
            {
                return null;
            }

            // Save Write Ordering Fix runs after IMDC's caller transpiler and
            // freezes the exact SavedData payload synchronously. When it is loaded,
            // a second full JsonUtility round-trip here only doubles save-time CPU
            // and allocation cost for large campaigns.
            if (IsSaveWriteOrderingFixLoaded())
            {
                return source;
            }

            // Standalone IMDC still protects vanilla's worker-thread serializer from
            // later mutations by detaching the graph before checkpointing it.
            string json = UnityEngine.JsonUtility.ToJson(source, false);
            SaveManager.SavedData snapshot =
                UnityEngine.JsonUtility.FromJson<SaveManager.SavedData>(json);
            if (snapshot == null)
            {
                throw new InvalidOperationException(
                    "Unity JsonUtility returned a null SavedData snapshot.");
            }

            return snapshot;
        }

        private static bool IsSaveWriteOrderingFixLoaded()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                AssemblyName name = assemblies[index] != null
                    ? assemblies[index].GetName()
                    : null;
                if (name != null && string.Equals(
                        name.Name,
                        "com.cosmo.savewriteorderingfix",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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
    /// Shared lightweight save-boundary entry point.
    /// </summary>
    internal static class CoreSaveLifecycleBinding
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
                // The persistence layer resolves and validates the actual DataSaver target.
                // No vanilla serialization, SHA fingerprint, staging directory, or
                // asynchronous vanilla-file observation belongs in this hook.
                IMDataCoreController.Instance.PrepareVanillaSaveWrite(
                    savedData,
                    dataFileName,
                    isJson,
                    fullPath);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core sidecar save preparation failed without blocking vanilla: " +
                    exception.Message);
            }
        }
    }

    /// <summary>
    /// Reproduces only the one vanilla manual-load name needed before the bool
    /// LoadData overload resolves/deserializes it. All final validation remains in
    /// the persistence layer and CorePaths.
    /// </summary>
    internal static class CoreSaveFilePathResolver
    {
        private const string StoryModeFolderName = "story_mode";
        private const string ManualSaveFileName = "manual_save";

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
    /// Per-thread vanilla LoadData state. SaveManager loading is main-thread work,
    /// but a small stack makes nested overloads safe and prevents one overload from
    /// stealing another overload's resolved autosave path.
    /// </summary>
    internal sealed class CoreSaveLoadPatchState
    {
        internal string RequestedSavePath = string.Empty;
        internal bool IsAutosaveRequest;
        internal bool PathResolved;
        internal bool RestorationPerformed;
        internal bool CompletionPerformed;
    }

    internal static class CoreSaveLoadContext
    {
        [ThreadStatic]
        private static Stack<CoreSaveLoadPatchState> stateStack;

        internal static CoreSaveLoadPatchState Current
        {
            get
            {
                return stateStack != null && stateStack.Count > 0
                    ? stateStack.Peek()
                    : null;
            }
        }

        internal static void Begin(CoreSaveLoadPatchState state)
        {
            if (state == null)
            {
                return;
            }

            if (stateStack == null)
            {
                stateStack = new Stack<CoreSaveLoadPatchState>();
            }

            stateStack.Push(state);
        }

        internal static void End(CoreSaveLoadPatchState state)
        {
            if (state == null || stateStack == null || stateStack.Count == 0)
            {
                return;
            }

            if (ReferenceEquals(stateStack.Peek(), state))
            {
                stateStack.Pop();
            }

            if (stateStack.Count == 0)
            {
                stateStack = null;
            }
        }
    }

    /// <summary>
    /// Captures the deserialized SavedData immediately after SaveManager.Data is
    /// assigned and before vanilla invokes LoadEvent. This is the one and only
    /// IMDC restoration point for a successful LoadData invocation.
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

            if (saveDataField == null || captureMethod == null)
            {
                throw new MissingMemberException(
                    "IM Data Core could not resolve the SaveManager.Data load hook.");
            }

            List<CodeInstruction> patchedInstructions =
                new List<CodeInstruction>();
            int injectedCaptureCount = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                patchedInstructions.Add(instruction);

                if (instruction.opcode == OpCodes.Stfld &&
                    Equals(instruction.operand, saveDataField))
                {
                    patchedInstructions.Add(
                        new CodeInstruction(OpCodes.Ldarg_0));
                    patchedInstructions.Add(
                        new CodeInstruction(OpCodes.Call, captureMethod));
                    injectedCaptureCount++;
                }
            }

            if (injectedCaptureCount != 1)
            {
                throw new InvalidOperationException(
                    "IM Data Core requires exactly one pre-LoadEvent SaveManager.Data capture; found " +
                    injectedCaptureCount.ToString() +
                    ".");
            }

            return patchedInstructions;
        }

        private static void CaptureLoadedSaveData(SaveManager saveManager)
        {
            CoreSaveLoadPatchState state = CoreSaveLoadContext.Current;
            if (saveManager == null ||
                saveManager.Data == null ||
                state == null ||
                state.RestorationPerformed ||
                !state.PathResolved ||
                string.IsNullOrWhiteSpace(state.RequestedSavePath))
            {
                return;
            }

            // Mark first, not after the callback. Even if a future controller
            // regression unexpectedly throws, this vanilla LoadData invocation
            // must never perform a second restoration after LoadEvent.
            state.RestorationPerformed = true;

            try
            {
                IMDataCoreController.Instance.OnVanillaSaveDataRead(
                    saveManager.Data,
                    state.RequestedSavePath);
            }
            catch (Exception exception)
            {
                // Persistence is already fail-soft, but keep Harmony completely
                // insulated from any future regression at this boundary.
                CoreLog.Warn(
                    "IM Data Core pre-LoadEvent restoration failed without blocking vanilla: " +
                    exception.Message);

                try
                {
                    IMDataCoreController.Instance.CancelVanillaLoadPreparation();
                }
                catch
                {
                    // Never allow supplemental cleanup to escape into vanilla.
                }
            }
        }
    }

    internal static class CoreSaveLoadLifecycle
    {
        internal static CoreSaveLoadPatchState BeginExplicitLoad(string path)
        {
            CoreSaveLoadPatchState state = new CoreSaveLoadPatchState
            {
                RequestedSavePath = path ?? string.Empty,
                PathResolved = !string.IsNullOrWhiteSpace(path)
            };

            CoreSaveLoadContext.Begin(state);
            return state;
        }

        internal static CoreSaveLoadPatchState BeginAutoFlagLoad(bool autoSave)
        {
            CoreSaveLoadPatchState state = new CoreSaveLoadPatchState
            {
                IsAutosaveRequest = autoSave
            };

            if (!autoSave)
            {
                state.RequestedSavePath =
                    CoreSaveFilePathResolver.ResolveManualLoadPath();
                state.PathResolved =
                    !string.IsNullOrWhiteSpace(state.RequestedSavePath);
            }

            CoreSaveLoadContext.Begin(state);
            return state;
        }

        internal static void CaptureResolvedAutosavePath(string savePath)
        {
            CoreSaveLoadPatchState state = CoreSaveLoadContext.Current;
            if (state == null ||
                !state.IsAutosaveRequest ||
                state.PathResolved)
            {
                return;
            }

            state.RequestedSavePath = savePath ?? string.Empty;
            state.PathResolved =
                !string.IsNullOrWhiteSpace(state.RequestedSavePath);
        }

        internal static void CompleteSuccessfulLoad(
            SaveManager saveManager,
            CoreSaveLoadPatchState state)
        {
            if (state == null || state.CompletionPerformed)
            {
                CoreSaveLoadContext.End(state);
                return;
            }

            state.CompletionPerformed = true;

            try
            {
                // A successful load should normally have passed the injected
                // pre-LoadEvent restoration. Never perform a restoration here.
                if (state.RestorationPerformed)
                {
                    IMDataCoreController.Instance.OnVanillaLoadCompleted();
                }
                else if (saveManager != null &&
                         saveManager.Data != null &&
                         state.PathResolved)
                {
                    // This is deliberately diagnostic only. Re-running restoration
                    // after LoadEvent would recreate the original bug.
                    CoreLog.Warn(
                        "IM Data Core did not observe its required pre-LoadEvent restoration; " +
                        "the postfix will not perform a late second restore.");
                }
            }
            catch (Exception exception)
            {
                // No Harmony postfix exception may interrupt scene/game progression.
                CoreLog.Warn(
                    "IM Data Core load completion failed without blocking vanilla: " +
                    exception.Message);

                try
                {
                    IMDataCoreController.Instance.CancelVanillaLoadPreparation();
                }
                catch
                {
                }
            }
            finally
            {
                CoreSaveLoadContext.End(state);
            }
        }

        internal static void AbortLoad(
            Exception exception,
            CoreSaveLoadPatchState state)
        {
            try
            {
                if (exception != null &&
                    state != null &&
                    state.RestorationPerformed &&
                    !state.CompletionPerformed)
                {
                    IMDataCoreController.Instance.CancelVanillaLoadPreparation();
                }
            }
            catch
            {
                // Vanilla's original exception, if any, is authoritative.
            }
            finally
            {
                CoreSaveLoadContext.End(state);
            }
        }
    }

    /// <summary>
    /// Loads an explicitly selected vanilla save. IMDC restores exactly once at the
    /// injected pre-LoadEvent point. The postfix performs completion only.
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
            __state = CoreSaveLoadLifecycle.BeginExplicitLoad(path);
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
            CoreSaveLoadLifecycle.CompleteSuccessfulLoad(
                __instance,
                __state);
        }

        [HarmonyFinalizer]
        private static void Finalizer(
            Exception __exception,
            CoreSaveLoadPatchState __state)
        {
            CoreSaveLoadLifecycle.AbortLoad(
                __exception,
                __state);
        }
    }

    /// <summary>
    /// Receives the autosave path from vanilla's own autosave selection. There is
    /// no second GetLatestAutosavePath scan by IMDC.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.GetLatestAutosavePath))]
    internal static class SaveManager_GetLatestAutosavePath_IMDataCoreSaveScope_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(string __result)
        {
            CoreSaveLoadLifecycle.CaptureResolvedAutosavePath(__result);
        }
    }

    /// <summary>
    /// Loads the vanilla auto/manual selection overload. As with the explicit path
    /// overload, restoration occurs once before LoadEvent and never in the postfix.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.LoadData),
        new Type[] { typeof(bool) })]
    internal static class SaveManager_LoadDataAutoFlag_IMDataCoreSaveScope_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            bool autoSave,
            ref CoreSaveLoadPatchState __state)
        {
            __state = CoreSaveLoadLifecycle.BeginAutoFlagLoad(autoSave);
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
            CoreSaveLoadLifecycle.CompleteSuccessfulLoad(
                __instance,
                __state);
        }

        [HarmonyFinalizer]
        private static void Finalizer(
            Exception __exception,
            CoreSaveLoadPatchState __state)
        {
            CoreSaveLoadLifecycle.AbortLoad(
                __exception,
                __state);
        }
    }
}
