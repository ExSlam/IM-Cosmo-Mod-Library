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
        internal bool DeletionAllowed = true;
        internal IDisposable SaveWriteDirectoryLease;
    }

    internal static class SaveWriteOrderingDeletionInterop
    {
        private const string AssemblyName = "com.cosmo.savewriteorderingfix";
        private const string ApiTypeName =
            "SaveWriteOrderingFix.SaveWriteOrderingApi";
        private const string AcquireDirectoryMethodName =
            "TryAcquireExclusiveDirectoryAccess";
        private const int AcquireTimeoutMilliseconds = 30000;

        private static readonly object LookupLock = new object();
        private static MethodInfo acquireDirectoryMethod;

        internal static bool TryAcquireDirectoryLease(
            string vanillaDirectoryPath,
            out IDisposable lease,
            out string errorMessage)
        {
            lease = null;
            errorMessage = string.Empty;

            Assembly swofAssembly = null;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Assembly candidate = assemblies[index];
                System.Reflection.AssemblyName name = candidate != null ? candidate.GetName() : null;
                if (name != null && string.Equals(
                        name.Name,
                        AssemblyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    swofAssembly = candidate;
                    break;
                }
            }

            // SWOF remains optional. If it is not loaded, vanilla deletion proceeds
            // and IMDC's own archive topology protection remains sufficient.
            if (swofAssembly == null)
            {
                return true;
            }

            MethodInfo method = acquireDirectoryMethod;
            if (method == null)
            {
                lock (LookupLock)
                {
                    method = acquireDirectoryMethod;
                    if (method == null)
                    {
                        Type apiType = swofAssembly.GetType(ApiTypeName, false);
                        method = apiType != null
                            ? apiType.GetMethod(
                                AcquireDirectoryMethodName,
                                BindingFlags.Public | BindingFlags.Static)
                            : null;
                        if (method != null)
                        {
                            acquireDirectoryMethod = method;
                        }
                    }
                }
            }

            if (method == null)
            {
                errorMessage =
                    "Save Write Ordering Fix is loaded without the required " +
                    "exclusive-directory lease API.";
                return false;
            }

            try
            {
                object[] arguments = new object[]
                {
                    vanillaDirectoryPath,
                    AcquireTimeoutMilliseconds,
                    null,
                    string.Empty
                };
                object result = method.Invoke(null, arguments);
                bool acquired = result is bool && (bool)result;
                lease = arguments[2] as IDisposable;
                errorMessage = arguments[3] as string ?? string.Empty;

                if (!acquired || lease == null)
                {
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        errorMessage =
                            "Save Write Ordering Fix did not grant an exclusive " +
                            "directory lease.";
                    }
                    if (lease != null)
                    {
                        lease.Dispose();
                        lease = null;
                    }
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage =
                    "Save Write Ordering Fix directory coordination failed: " +
                    exception.Message;
                if (lease != null)
                {
                    lease.Dispose();
                    lease = null;
                }
                return false;
            }
        }
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
                DeletedSaveDirectoryArchiveState state =
                    new DeletedSaveDirectoryArchiveState
                    {
                        VanillaDirectoryPath = Path.GetFullPath(vanillaDirectoryPath)
                    };

                string coordinationError;
                if (!SaveWriteOrderingDeletionInterop.TryAcquireDirectoryLease(
                        state.VanillaDirectoryPath,
                        out state.SaveWriteDirectoryLease,
                        out coordinationError))
                {
                    state.DeletionAllowed = false;
                    CoreLog.Warn(
                        "IM Data Core blocked save deletion because Save Write " +
                        "Ordering Fix could not establish an exclusive directory " +
                        "boundary: " + coordinationError);
                }

                return state;
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
            if (state == null)
            {
                return;
            }

            try
            {
                if (!state.DeletionAllowed ||
                    string.IsNullOrEmpty(state.VanillaDirectoryPath) ||
                    Directory.Exists(state.VanillaDirectoryPath))
                {
                    return;
                }

                IMDataCoreController.Instance.OnVanillaSaveDirectoryDeleted(
                    state.VanillaDirectoryPath);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not preserve deleted-save persistence: " +
                    exception.Message);
            }
            finally
            {
                if (state.SaveWriteDirectoryLease != null)
                {
                    try
                    {
                        state.SaveWriteDirectoryLease.Dispose();
                    }
                    catch (Exception exception)
                    {
                        CoreLog.Warn(
                            "IM Data Core could not release Save Write Ordering " +
                            "Fix deletion coordination: " + exception.Message);
                    }
                    state.SaveWriteDirectoryLease = null;
                }
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
        private static bool Prefix(
            Popup_Save __instance,
            out DeletedSaveDirectoryArchiveState __state)
        {
            __state = null;
            try
            {
                if (__instance == null || __instance.SaveFile_ID == 0)
                {
                    return true;
                }

                __state = DeletedSaveDirectoryArchiveBinding.Capture(
                    Path.Combine(
                        Application.persistentDataPath,
                        "data",
                        "manual_saves",
                        __instance.SaveFile_ID.ToString()));
                return __state == null || __state.DeletionAllowed;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not prepare manual-save archival: " +
                    exception.Message);
                return true;
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
        private static bool Prefix(
            Popup_Load_Story.save_info Save,
            out DeletedSaveDirectoryArchiveState __state)
        {
            __state = null;
            try
            {
                if (Save == null)
                {
                    return true;
                }

                __state = DeletedSaveDirectoryArchiveBinding.Capture(
                    Save.GetDirectory());
                return __state == null || __state.DeletionAllowed;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not prepare story-save archival: " +
                    exception.Message);
                return true;
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
        private static bool Prefix(
            Popup_Load_Story.playthrough_info Playthrough,
            out DeletedSaveDirectoryArchiveState __state)
        {
            __state = null;
            try
            {
                if (Playthrough == null)
                {
                    return true;
                }

                __state = DeletedSaveDirectoryArchiveBinding.Capture(
                    Playthrough.Dir);
                return __state == null || __state.DeletionAllowed;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not prepare playthrough archival: " +
                    exception.Message);
                return true;
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
