using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal sealed class DeletedSaveDirectoryArchiveState
    {
        internal string VanillaDirectoryPath = string.Empty;
    }

    internal static class DeletedSaveDirectoryArchiveBinding
    {
        internal static DeletedSaveDirectoryArchiveState Capture(
            string vanillaDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(vanillaDirectoryPath))
            {
                return null;
            }

            try
            {
                return new DeletedSaveDirectoryArchiveState
                {
                    VanillaDirectoryPath = Path.GetFullPath(vanillaDirectoryPath)
                };
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not capture a vanilla deletion path: " +
                    exception.Message);
                return null;
            }
        }

        internal static void ArchiveAfterSuccessfulDelete(
            DeletedSaveDirectoryArchiveState state)
        {
            if (state == null ||
                string.IsNullOrEmpty(state.VanillaDirectoryPath) ||
                Directory.Exists(state.VanillaDirectoryPath))
            {
                return;
            }

            try
            {
                IMDataCoreController.Instance.OnVanillaSaveDirectoryDeleted(
                    state.VanillaDirectoryPath);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not preserve deleted-save persistence: " +
                    exception.Message);
            }
        }
    }

    /// <summary>
    /// Legacy/freeplay manual saves delete a numeric directory and swallow any
    /// Directory.Delete exception. Capture the intended directory first, then archive
    /// IMDC only when the vanilla directory is absent after the original method.
    /// </summary>
    [HarmonyPatch(typeof(Popup_Save), "Delete")]
    internal static class Popup_Save_Delete_IMDataCoreArchive_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            Popup_Save __instance,
            out DeletedSaveDirectoryArchiveState __state)
        {
            __state = null;
            try
            {
                if (__instance == null || __instance.SaveFile_ID == 0)
                {
                    return;
                }

                __state = DeletedSaveDirectoryArchiveBinding.Capture(
                    Path.Combine(
                        Application.persistentDataPath,
                        "data",
                        "manual_saves",
                        __instance.SaveFile_ID.ToString()));
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not prepare manual-save archival: " +
                    exception.Message);
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(
            Exception __exception,
            DeletedSaveDirectoryArchiveState __state)
        {
            DeletedSaveDirectoryArchiveBinding.ArchiveAfterSuccessfulDelete(__state);
            return __exception;
        }
    }

    /// <summary>
    /// Story-mode deletion removes exactly Save.GetDirectory(). The finalizer checks
    /// the captured directory even if later vanilla UI cleanup throws; archival occurs
    /// only when the vanilla directory is actually absent, and the original exception
    /// is preserved unchanged.
    /// </summary>
    [HarmonyPatch]
    internal static class Popup_Load_Story_Delete_Save_IMDataCoreArchive_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Popup_Load_Story),
                "Delete_Save",
                new Type[] { typeof(Popup_Load_Story.save_info) });
            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(Popup_Load_Story).FullName,
                    "Delete_Save");
            }
            return method;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            Popup_Load_Story.save_info Save,
            out DeletedSaveDirectoryArchiveState __state)
        {
            __state = null;
            try
            {
                if (Save != null)
                {
                    __state = DeletedSaveDirectoryArchiveBinding.Capture(
                        Save.GetDirectory());
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not prepare story-save archival: " +
                    exception.Message);
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(
            Exception __exception,
            DeletedSaveDirectoryArchiveState __state)
        {
            DeletedSaveDirectoryArchiveBinding.ArchiveAfterSuccessfulDelete(__state);
            return __exception;
        }
    }

    /// <summary>
    /// Deleting a story playthrough removes the whole playthrough directory. Archive
    /// the matching IMDC subtree as one unit so all career-diary material stays
    /// together for later export.
    /// </summary>
    [HarmonyPatch]
    internal static class Popup_Load_Story_Delete_Playthrough_IMDataCoreArchive_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Popup_Load_Story),
                "Delete_Playthrough",
                new Type[] { typeof(Popup_Load_Story.playthrough_info) });
            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(Popup_Load_Story).FullName,
                    "Delete_Playthrough");
            }
            return method;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            Popup_Load_Story.playthrough_info Playthrough,
            out DeletedSaveDirectoryArchiveState __state)
        {
            __state = null;
            try
            {
                if (Playthrough != null)
                {
                    __state = DeletedSaveDirectoryArchiveBinding.Capture(
                        Playthrough.Dir);
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not prepare playthrough archival: " +
                    exception.Message);
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(
            Exception __exception,
            DeletedSaveDirectoryArchiveState __state)
        {
            DeletedSaveDirectoryArchiveBinding.ArchiveAfterSuccessfulDelete(__state);
            return __exception;
        }
    }
}
