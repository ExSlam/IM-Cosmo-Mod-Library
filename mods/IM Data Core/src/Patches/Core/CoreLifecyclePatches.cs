using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Initializes IM Data Core once popup systems are active in gameplay scenes.
    /// </summary>
    [HarmonyPatch(typeof(PopupManager), CoreConstants.HarmonyPopupManagerStartMethodName)]
    internal static class PopupManager_Start_IMDataCoreBootstrap_Patch
    {
        /// <summary>
        /// Ensures runtime initialization after popup manager startup.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            IMDataCoreController.Instance.BootstrapIfNeeded();
        }
    }

    /// <summary>
    /// Clears path-based save scoping when starting a brand-new game flow.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu_LoadGameManager), nameof(MainMenu_LoadGameManager.StartNewGame))]
    internal static class MainMenu_LoadGameManager_StartNewGame_IMDataCoreScopeReset_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            IMDataCoreController.Instance.OnSaveLoadStarting(string.Empty);
        }
    }

    /// <summary>
    /// Forces IM Data Core flush before any explicit save-event dispatch.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CallSaveEvent))]
    internal static class SaveManager_CallSaveEvent_IMDataCoreFlush_Patch
    {
        /// <summary>
        /// Persists buffered data prior to save event propagation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            IMDataCoreController.Instance.ForceFlushBeforeSave();
        }
    }

    /// <summary>
    /// Forces IM Data Core flush before save-data writes.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveData))]
    internal static class SaveManager_SaveData_IMDataCoreFlush_Patch
    {
        /// <summary>
        /// Persists buffered data before save-data file creation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(bool autoSave)
        {
            string savePath = CoreSaveFilePathResolver.ResolveForSaveData(autoSave);
            IMDataCoreController.Instance.OnSaveWriteStarting(savePath);
            IMDataCoreController.Instance.ForceFlushBeforeSave();
        }
    }

    /// <summary>
    /// Forces IM Data Core flush before chapter-save writes.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveChapter))]
    internal static class SaveManager_SaveChapter_IMDataCoreFlush_Patch
    {
        /// <summary>
        /// Persists buffered data before chapter save creation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(tasks._chapter Chapter)
        {
            string savePath = CoreSaveFilePathResolver.ResolveForSaveChapter(Chapter);
            IMDataCoreController.Instance.OnSaveWriteStarting(savePath);
            IMDataCoreController.Instance.ForceFlushBeforeSave();
        }
    }

    /// <summary>
    /// Binds IM Data Core to the target manual save-slot path before popup-driven manual saves.
    /// </summary>
    [HarmonyPatch(typeof(Popup_Save), Popup_Save_Save_IMDataCoreSaveScope_Patch.PopupSaveMethodName)]
    internal static class Popup_Save_Save_IMDataCoreSaveScope_Patch
    {
        internal const string PopupSaveMethodName = "Save";

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Popup_Save __instance)
        {
            string savePath = CoreSaveFilePathResolver.ResolveForPopupManualSave(__instance);
            IMDataCoreController.Instance.OnSaveWriteStarting(savePath);
        }
    }

    /// <summary>
    /// Binds IM Data Core to the selected save slot when overwrite-save is requested from story load UI.
    /// </summary>
    [HarmonyPatch(typeof(Popup_Load_Story), Popup_Load_Story_DoOverwriteSave_IMDataCoreSaveScope_Patch.DoOverwriteSaveMethodName)]
    internal static class Popup_Load_Story_DoOverwriteSave_IMDataCoreSaveScope_Patch
    {
        internal const string DoOverwriteSaveMethodName = "Do_Overwrite_Save";

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Popup_Load_Story.save_info Save)
        {
            if (Save == null || string.IsNullOrEmpty(Save.Path_File))
            {
                return;
            }

            IMDataCoreController.Instance.OnSaveWriteStarting(Save.Path_File);
        }
    }

    /// <summary>
    /// Captures the file set before and after "New Save" so IM Data Core can bind to the exact new save path.
    /// </summary>
    [HarmonyPatch(typeof(Popup_Load_Story), Popup_Load_Story_DoNewSave_IMDataCoreSaveScope_Patch.DoNewSaveMethodName)]
    internal static class Popup_Load_Story_DoNewSave_IMDataCoreSaveScope_Patch
    {
        internal const string DoNewSaveMethodName = "Do_New_Save";

        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref HashSet<string> __state)
        {
            CoreSaveFilePathResolver.BeginNewSaveHashCapture();
            __state = CoreSaveFilePathResolver.CaptureExistingManualSaveFilesForNewSave();
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(HashSet<string> __state)
        {
            string savePath = CoreSaveFilePathResolver.ResolveDoNewSavePathFromCapturedHash();
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = CoreSaveFilePathResolver.ResolveNewManualSavePathAfterSnapshot(__state);
            }

            if (string.IsNullOrEmpty(savePath))
            {
                return;
            }

            IMDataCoreController.Instance.OnSaveWriteStarting(savePath);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                CoreSaveFilePathResolver.CancelNewSaveHashCapture();
            }

            return __exception;
        }
    }

    /// <summary>
    /// Captures hash tokens produced during Do_New_Save path construction.
    /// </summary>
    [HarmonyPatch(typeof(Hash), nameof(Hash.Generate))]
    internal static class Hash_Generate_IMDataCoreNewSaveHashCapture_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(int NumberOfCharacters, string __result)
        {
            CoreSaveFilePathResolver.TryCaptureGeneratedHashToken(NumberOfCharacters, __result);
        }
    }

    /// <summary>
    /// Resolves game save-file paths so IM Data Core can bind storage by save file rather than by agency identity.
    /// </summary>
    internal static class CoreSaveFilePathResolver
    {
        private const string StoryModeFolderName = "story_mode";
        private const string DataFolderName = "data";
        private const string AutoSaveFileName = "auto_save";
        private const string ManualSaveFileName = "manual_save";
        private const string ManualSavesFolderName = "manual_saves";
        private const string SaveFileName = "save";
        private const string SaveFileExtension = ".json";
        private const string PopupSaveGetNewSaveFileIdMethodName = "GetNewSaveFileID";
        private const int ExpectedSaveHashCharacterCount = 8;

        private static readonly object newSaveHashCaptureLock = new object();
        private static bool newSaveHashCaptureEnabled;
        private static string capturedNewSaveHashToken = string.Empty;

        internal static string ResolveFromLoadFlag(bool autoSave)
        {
            if (autoSave)
            {
                return SaveManager.GetLatestAutosavePath();
            }

            if (staticVars.PlayerData != null && staticVars.IsStoryMode())
            {
                return Path.Combine(
                    StoryModeFolderName,
                    staticVars.PlayerData.GetSaveFolderName(),
                    ManualSaveFileName);
            }

            return ManualSaveFileName;
        }

        internal static string ResolveForSaveData(bool autoSave)
        {
            string prefixPath = string.Empty;
            if (staticVars.PlayerData != null && staticVars.IsStoryMode())
            {
                prefixPath = Path.Combine(StoryModeFolderName, staticVars.PlayerData.GetSaveFolderName());
            }

            return Path.Combine(prefixPath, autoSave ? AutoSaveFileName : ManualSaveFileName);
        }

        internal static string ResolveForSaveChapter(tasks._chapter chapter)
        {
            if (staticVars.PlayerData == null)
            {
                return string.Empty;
            }

            string chapterFolderName = Popup_Load_Story.GetFolderNameFromChapter(chapter);
            if (string.IsNullOrEmpty(chapterFolderName))
            {
                return string.Empty;
            }

            return Path.Combine(
                Application.persistentDataPath,
                DataFolderName,
                StoryModeFolderName,
                staticVars.PlayerData.GetSaveFolderName(),
                chapterFolderName,
                SaveFileName);
        }

        internal static string ResolveForPopupManualSave(Popup_Save popupSave)
        {
            if (popupSave == null)
            {
                return string.Empty;
            }

            int saveFileId = popupSave.SaveFile_ID;
            if (saveFileId == CoreConstants.ZeroBasedListStartIndex)
            {
                MethodInfo methodGetNewSaveFileId =
                    AccessTools.Method(typeof(Popup_Save), PopupSaveGetNewSaveFileIdMethodName);
                if (methodGetNewSaveFileId != null)
                {
                    object methodResult = methodGetNewSaveFileId.Invoke(popupSave, null);
                    if (methodResult != null)
                    {
                        saveFileId = Convert.ToInt32(methodResult, CultureInfo.InvariantCulture);
                    }
                }
            }

            if (saveFileId <= CoreConstants.ZeroBasedListStartIndex)
            {
                return string.Empty;
            }

            return Path.Combine(
                ManualSavesFolderName,
                saveFileId.ToString(CultureInfo.InvariantCulture),
                SaveFileName);
        }

        internal static HashSet<string> CaptureExistingManualSaveFilesForNewSave()
        {
            HashSet<string> existingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string manualSaveRootPath = ResolveManualSaveRootForNewSave();
            if (string.IsNullOrEmpty(manualSaveRootPath))
            {
                return existingFiles;
            }

            List<string> saveFiles = EnumerateManualSaveFiles(manualSaveRootPath);
            for (int fileIndex = CoreConstants.ZeroBasedListStartIndex; fileIndex < saveFiles.Count; fileIndex++)
            {
                string normalizedPath = NormalizeSaveFilePath(saveFiles[fileIndex]);
                if (!string.IsNullOrEmpty(normalizedPath))
                {
                    existingFiles.Add(normalizedPath);
                }
            }

            return existingFiles;
        }

        internal static void BeginNewSaveHashCapture()
        {
            lock (newSaveHashCaptureLock)
            {
                newSaveHashCaptureEnabled = true;
                capturedNewSaveHashToken = string.Empty;
            }
        }

        internal static void CancelNewSaveHashCapture()
        {
            lock (newSaveHashCaptureLock)
            {
                newSaveHashCaptureEnabled = false;
                capturedNewSaveHashToken = string.Empty;
            }
        }

        internal static void TryCaptureGeneratedHashToken(int numberOfCharacters, string generatedToken)
        {
            lock (newSaveHashCaptureLock)
            {
                if (!newSaveHashCaptureEnabled)
                {
                    return;
                }

                if (numberOfCharacters != ExpectedSaveHashCharacterCount)
                {
                    return;
                }

                if (string.IsNullOrEmpty(generatedToken) || !string.IsNullOrEmpty(capturedNewSaveHashToken))
                {
                    return;
                }

                capturedNewSaveHashToken = generatedToken;
            }
        }

        internal static string ResolveDoNewSavePathFromCapturedHash()
        {
            string capturedToken;
            lock (newSaveHashCaptureLock)
            {
                newSaveHashCaptureEnabled = false;
                capturedToken = capturedNewSaveHashToken;
                capturedNewSaveHashToken = string.Empty;
            }

            if (string.IsNullOrEmpty(capturedToken))
            {
                return string.Empty;
            }

            string candidatePath;
            if (Popup_Load_Story.Story_Mode)
            {
                string playthroughPath = SaveManager.GetPlaythroughPath();
                if (string.IsNullOrEmpty(playthroughPath))
                {
                    return string.Empty;
                }

                candidatePath = Path.Combine(
                    playthroughPath,
                    ManualSavesFolderName,
                    capturedToken,
                    SaveFileName + SaveFileExtension);
            }
            else
            {
                candidatePath = Path.Combine(
                    Application.persistentDataPath,
                    DataFolderName,
                    ManualSavesFolderName,
                    capturedToken,
                    SaveFileName + SaveFileExtension);
            }

            return NormalizeSaveFilePath(candidatePath);
        }

        internal static string ResolveNewManualSavePathAfterSnapshot(HashSet<string> filesBeforeSave)
        {
            string manualSaveRootPath = ResolveManualSaveRootForNewSave();
            if (string.IsNullOrEmpty(manualSaveRootPath))
            {
                return string.Empty;
            }

            List<string> saveFiles = EnumerateManualSaveFiles(manualSaveRootPath);
            string newestNewPath = string.Empty;
            DateTime newestNewWriteTimeUtc = DateTime.MinValue;
            for (int fileIndex = CoreConstants.ZeroBasedListStartIndex; fileIndex < saveFiles.Count; fileIndex++)
            {
                string normalizedPath = NormalizeSaveFilePath(saveFiles[fileIndex]);
                if (string.IsNullOrEmpty(normalizedPath))
                {
                    continue;
                }

                if (filesBeforeSave != null && filesBeforeSave.Contains(normalizedPath))
                {
                    continue;
                }

                DateTime writeTimeUtc = File.GetLastWriteTimeUtc(normalizedPath);
                if (writeTimeUtc <= newestNewWriteTimeUtc)
                {
                    continue;
                }

                newestNewWriteTimeUtc = writeTimeUtc;
                newestNewPath = normalizedPath;
            }

            if (!string.IsNullOrEmpty(newestNewPath))
            {
                return newestNewPath;
            }

            return ResolveMostRecentManualSaveFilePath(manualSaveRootPath);
        }

        private static string ResolveManualSaveRootForNewSave()
        {
            if (Popup_Load_Story.Story_Mode)
            {
                string playthroughPath = SaveManager.GetPlaythroughPath();
                if (string.IsNullOrEmpty(playthroughPath))
                {
                    return string.Empty;
                }

                return Path.Combine(playthroughPath, ManualSavesFolderName);
            }

            return Path.Combine(Application.persistentDataPath, DataFolderName, ManualSavesFolderName);
        }

        private static List<string> EnumerateManualSaveFiles(string rootPath)
        {
            List<string> saveFiles = new List<string>();
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                return saveFiles;
            }

            string[] slotDirectories = Directory.GetDirectories(rootPath);
            for (int directoryIndex = CoreConstants.ZeroBasedListStartIndex; directoryIndex < slotDirectories.Length; directoryIndex++)
            {
                string savePath = Path.Combine(slotDirectories[directoryIndex], SaveFileName + SaveFileExtension);
                if (File.Exists(savePath))
                {
                    saveFiles.Add(savePath);
                }
            }

            return saveFiles;
        }

        private static string ResolveMostRecentManualSaveFilePath(string rootPath)
        {
            List<string> saveFiles = EnumerateManualSaveFiles(rootPath);
            string newestPath = string.Empty;
            DateTime newestWriteTimeUtc = DateTime.MinValue;

            for (int fileIndex = CoreConstants.ZeroBasedListStartIndex; fileIndex < saveFiles.Count; fileIndex++)
            {
                string normalizedPath = NormalizeSaveFilePath(saveFiles[fileIndex]);
                if (string.IsNullOrEmpty(normalizedPath))
                {
                    continue;
                }

                DateTime writeTimeUtc = File.GetLastWriteTimeUtc(normalizedPath);
                if (writeTimeUtc <= newestWriteTimeUtc)
                {
                    continue;
                }

                newestWriteTimeUtc = writeTimeUtc;
                newestPath = normalizedPath;
            }

            return newestPath;
        }

        private static string NormalizeSaveFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string candidatePath = path.Trim();
            if (!candidatePath.EndsWith(SaveFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                candidatePath += SaveFileExtension;
            }

            if (!Path.IsPathRooted(candidatePath))
            {
                candidatePath = Path.Combine(Application.persistentDataPath, DataFolderName, candidatePath);
            }

            try
            {
                return Path.GetFullPath(candidatePath);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Binds IM Data Core to the explicit path-based save file when loading via `LoadData(string path)`.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadData), new Type[] { typeof(string) })]
    internal static class SaveManager_LoadDataPath_IMDataCoreSaveScope_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(string path)
        {
            IMDataCoreController.Instance.OnSaveLoadStarting(path);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SaveManager __instance, string path)
        {
            if (__instance == null || __instance.Data == null)
            {
                return;
            }

            IMDataCoreController.Instance.OnSaveLoaded(path);
        }
    }

    /// <summary>
    /// Binds IM Data Core to auto/manual save files selected through `LoadData(bool autoSave)`.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadData), new Type[] { typeof(bool) })]
    internal static class SaveManager_LoadDataAutoFlag_IMDataCoreSaveScope_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(bool autoSave, ref string __state)
        {
            __state = CoreSaveFilePathResolver.ResolveFromLoadFlag(autoSave);
            IMDataCoreController.Instance.OnSaveLoadStarting(__state);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SaveManager __instance, string __state)
        {
            if (__instance == null || __instance.Data == null)
            {
                return;
            }

            IMDataCoreController.Instance.OnSaveLoaded(__state);
        }
    }
}
