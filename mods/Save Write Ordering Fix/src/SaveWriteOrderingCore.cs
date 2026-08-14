using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace SaveWriteOrderingFix
{
    internal static class SaveWriteOrderingConstants
    {
        internal const string HarmonyId = "com.cosmo.savewriteorderingfix";
        internal const string IMDataCoreHarmonyId = "com.cosmo.imdatacore";
        internal const string GraduationDetailsHarmonyId =
            "com.cosmo.graduationdetails";

        internal const string DataSaverSaveMethodName = "saveData";
        internal const string DataSaverLoadMethodName = "loadData";

        internal const int LoadWaitTimeoutMilliseconds = 30000;
        internal const string LogPrefix = "[Save Write Ordering Fix] ";
    }

    /// <summary>
    /// Optional cooperation API for mods that bypass Idol Manager's normal save/load
    /// callers and directly access the physical vanilla save JSON.
    ///
    /// Normal Harmony mods that patch SaveManager, Popup_Save, Popup_Load_Story, or
    /// their save/load events do not need to call this API.
    /// </summary>
    public static class SaveWriteOrderingApi
    {
        public const string Version = "1.2.0";

        /// <summary>
        /// True only after every known vanilla SavedData write caller has been
        /// inspected and exactly one DataSaver.saveData&lt;SavedData&gt; call in each
        /// caller was replaced by the ordered writer. Merely loading this assembly is
        /// intentionally not enough to make this property true.
        /// </summary>
        public static bool SavedDataInterceptionHealthy
        {
            get { return SaveWriteOrderingPatchHealth.IsSavedDataInterceptionHealthy; }
        }

        public static bool HasPendingWrites(string absoluteSavePath)
        {
            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSavePath,
                    out normalizedPath))
            {
                return false;
            }

            return OrderedSaveCoordinator.HasPendingWrites(normalizedPath);
        }

        public static bool TryWaitForPendingWrites(
            string absoluteSavePath,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!IsValidTimeout(timeoutMilliseconds))
            {
                errorMessage = "timeoutMilliseconds must be -1 or greater.";
                return false;
            }

            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSavePath,
                    out normalizedPath))
            {
                errorMessage = "absoluteSavePath must be a valid absolute path.";
                return false;
            }

            if (!OrderedSaveCoordinator.WaitForPath(
                    normalizedPath,
                    timeoutMilliseconds))
            {
                errorMessage =
                    "Timed out waiting for pending ordered vanilla save writes.";
                return false;
            }

            return true;
        }

        public static bool TryRunExclusiveFileAccess(
            string absoluteSavePath,
            Action fileAction,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (fileAction == null)
            {
                errorMessage = "fileAction cannot be null.";
                return false;
            }

            if (!IsValidTimeout(timeoutMilliseconds))
            {
                errorMessage = "timeoutMilliseconds must be -1 or greater.";
                return false;
            }

            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSavePath,
                    out normalizedPath))
            {
                errorMessage = "absoluteSavePath must be a valid absolute path.";
                return false;
            }

            return OrderedSaveCoordinator.TryRunExclusiveFileAccess(
                normalizedPath,
                fileAction,
                timeoutMilliseconds,
                out errorMessage);
        }

        private static bool IsValidTimeout(int timeoutMilliseconds)
        {
            return timeoutMilliseconds == Timeout.Infinite ||
                   timeoutMilliseconds >= 0;
        }
    }

    /// <summary>
    /// Records the result of caller-level transpilation so cooperating mods can
    /// distinguish "assembly loaded" from "all required save callers patched".
    /// </summary>
    internal static class SaveWriteOrderingPatchHealth
    {
        private const int ExpectedSavedDataWriteCallerCount = 5;
        private static readonly object SyncRoot = new object();
        private static readonly HashSet<string> SuccessfulCallers =
            new HashSet<string>(StringComparer.Ordinal);
        private static bool sawFailure;

        internal static bool IsSavedDataInterceptionHealthy
        {
            get
            {
                lock (SyncRoot)
                {
                    return !sawFailure &&
                        SuccessfulCallers.Count == ExpectedSavedDataWriteCallerCount;
                }
            }
        }

        internal static void ReportSavedDataWriteCaller(
            System.Reflection.MethodBase method,
            bool success)
        {
            string identity = BuildMethodIdentity(method);
            lock (SyncRoot)
            {
                if (!success)
                {
                    sawFailure = true;
                    return;
                }

                SuccessfulCallers.Add(identity);
            }
        }

        private static string BuildMethodIdentity(System.Reflection.MethodBase method)
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
    }

    internal static class SavePathResolver
    {
        internal static string ResolveWritePath(
            string dataFileName,
            bool fullPath)
        {
            if (string.IsNullOrEmpty(dataFileName))
            {
                return string.Empty;
            }

            string path;
            if (fullPath)
            {
                path = dataFileName;
            }
            else
            {
                path = Path.Combine(
                    Application.persistentDataPath,
                    "data");
                path = Path.Combine(path, dataFileName + ".json");
            }

            return NormalizePath(path);
        }

        internal static string ResolveReadPath(string dataFileName)
        {
            if (string.IsNullOrEmpty(dataFileName))
            {
                return string.Empty;
            }

            // Reproduce vanilla DataSaver.loadData<T> path construction exactly.
            string path = Path.Combine(
                Application.persistentDataPath,
                "data");
            path = Path.Combine(path, dataFileName + ".json");
            path = path.Replace(".json.json", ".json");

            return NormalizePath(path);
        }

        internal static bool TryNormalizeAbsolutePath(
            string absolutePath,
            out string normalizedPath)
        {
            normalizedPath = string.Empty;

            if (string.IsNullOrWhiteSpace(absolutePath) ||
                !Path.IsPathRooted(absolutePath))
            {
                return false;
            }

            normalizedPath = NormalizePath(absolutePath);
            return !string.IsNullOrEmpty(normalizedPath);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                // Do not prevent a vanilla operation solely because normalization
                // failed on an unusual path/runtime combination.
                return path;
            }
        }
    }

    internal sealed class FrozenSaveWrite
    {
        internal string TargetPath = string.Empty;
        internal string Payload = string.Empty;
    }

    internal sealed class SavePathQueue
    {
        internal readonly object SyncRoot = new object();
        internal readonly Queue<FrozenSaveWrite> PendingWrites =
            new Queue<FrozenSaveWrite>();

        internal bool Draining;
        internal bool ExternalAccessActive;
    }

    /// <summary>
    /// FIFO ordering is per physical save path. Separate save files retain independent
    /// asynchronous workers.
    /// </summary>
    internal static class OrderedSaveCoordinator
    {
        private static readonly object RegistrySync = new object();

        private static readonly StringComparer PathComparer =
            Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        private static readonly Dictionary<string, SavePathQueue> Queues =
            new Dictionary<string, SavePathQueue>(PathComparer);

        /// <summary>
        /// Replacement for the concrete vanilla SavedData call sites only.
        ///
        /// The JSON payload is frozen on the calling thread before it enters the FIFO.
        /// This closes both halves of the vanilla race:
        /// 1. save requests cannot finish out of order for one physical path;
        /// 2. a delayed write cannot observe later mutations of SaveManager.Data.
        ///
        /// No DataSaver<T> method itself is Harmony-patched.
        /// </summary>
        internal static void QueueSavedDataWrite(
            SaveManager.SavedData dataToSave,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            if (dataToSave == null)
            {
                FallbackToVanilla(
                    dataToSave,
                    dataFileName,
                    isJson,
                    fullPath,
                    "SavedData argument was null.");
                return;
            }

            string targetPath =
                SavePathResolver.ResolveWritePath(dataFileName, fullPath);

            if (string.IsNullOrEmpty(targetPath))
            {
                FallbackToVanilla(
                    dataToSave,
                    dataFileName,
                    isJson,
                    fullPath,
                    "Could not resolve the physical save path.");
                return;
            }

            string payload;
            try
            {
                if (isJson)
                {
                    // Vanilla uses JsonUtility.ToJson(..., true) on its worker.
                    // Doing the serialization now freezes the exact state belonging
                    // to this save request before any later request can mutate it.
                    payload = JsonUtility.ToJson(dataToSave, true);
                }
                else
                {
                    payload = Convert.ToString(
                        dataToSave,
                        CultureInfo.InvariantCulture);
                }
            }
            catch (Exception exception)
            {
                FallbackToVanilla(
                    dataToSave,
                    dataFileName,
                    isJson,
                    fullPath,
                    "Could not freeze the save payload: " +
                    exception.Message);
                return;
            }

            SavePathQueue queue = GetOrCreateQueue(targetPath);
            bool startDrainer = false;

            lock (queue.SyncRoot)
            {
                queue.PendingWrites.Enqueue(
                    new FrozenSaveWrite
                    {
                        TargetPath = targetPath,
                        Payload = payload ?? string.Empty
                    });

                if (!queue.Draining &&
                    !queue.ExternalAccessActive)
                {
                    queue.Draining = true;
                    startDrainer = true;
                }

                Monitor.PulseAll(queue.SyncRoot);
            }

            if (startDrainer)
            {
                StartDrainer(queue);
            }
        }

        internal static SaveManager.SavedData LoadSavedDataAfterPendingWrites(
            string dataFileName)
        {
            string physicalPath =
                SavePathResolver.ResolveReadPath(dataFileName);

            if (!string.IsNullOrEmpty(physicalPath))
            {
                bool completed = WaitForPath(
                    physicalPath,
                    SaveWriteOrderingConstants.LoadWaitTimeoutMilliseconds);

                if (!completed)
                {
                    Debug.LogWarning(
                        SaveWriteOrderingConstants.LogPrefix +
                        "Timed out waiting for an ordered write before reading " +
                        physicalPath +
                        ". Vanilla load will continue.");
                }
            }

            // Calling the generic method is safe. The unsafe operation was Harmony-
            // patching a constructed reference-type generic on Mono. This assembly
            // never patches DataSaver<T> itself.
            return DataSaver.loadData<SaveManager.SavedData>(dataFileName);
        }

        internal static bool HasPendingWrites(string normalizedPath)
        {
            SavePathQueue queue = TryGetQueue(normalizedPath);
            if (queue == null)
            {
                return false;
            }

            lock (queue.SyncRoot)
            {
                return queue.Draining ||
                       queue.PendingWrites.Count > 0 ||
                       queue.ExternalAccessActive;
            }
        }

        internal static bool WaitForPath(
            string normalizedPath,
            int timeoutMilliseconds)
        {
            SavePathQueue queue = TryGetQueue(normalizedPath);
            if (queue == null)
            {
                return true;
            }

            Stopwatch stopwatch =
                timeoutMilliseconds == Timeout.Infinite
                    ? null
                    : Stopwatch.StartNew();

            lock (queue.SyncRoot)
            {
                while (IsBusy(queue))
                {
                    if (timeoutMilliseconds == Timeout.Infinite)
                    {
                        Monitor.Wait(queue.SyncRoot);
                        continue;
                    }

                    int remaining = GetRemainingMilliseconds(
                        timeoutMilliseconds,
                        stopwatch);

                    if (remaining <= 0 ||
                        !Monitor.Wait(queue.SyncRoot, remaining))
                    {
                        return !IsBusy(queue);
                    }
                }
            }

            return true;
        }

        internal static bool TryRunExclusiveFileAccess(
            string normalizedPath,
            Action fileAction,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            SavePathQueue queue = GetOrCreateQueue(normalizedPath);

            Stopwatch stopwatch =
                timeoutMilliseconds == Timeout.Infinite
                    ? null
                    : Stopwatch.StartNew();

            lock (queue.SyncRoot)
            {
                while (queue.Draining ||
                       queue.PendingWrites.Count > 0 ||
                       queue.ExternalAccessActive)
                {
                    if (timeoutMilliseconds == Timeout.Infinite)
                    {
                        Monitor.Wait(queue.SyncRoot);
                        continue;
                    }

                    int remaining = GetRemainingMilliseconds(
                        timeoutMilliseconds,
                        stopwatch);

                    if (remaining <= 0 ||
                        !Monitor.Wait(queue.SyncRoot, remaining))
                    {
                        errorMessage =
                            "Timed out waiting for exclusive vanilla save-file access.";
                        return false;
                    }
                }

                queue.ExternalAccessActive = true;
            }

            try
            {
                fileAction();
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
            finally
            {
                bool startDrainer = false;

                lock (queue.SyncRoot)
                {
                    queue.ExternalAccessActive = false;

                    if (queue.PendingWrites.Count > 0 &&
                        !queue.Draining)
                    {
                        queue.Draining = true;
                        startDrainer = true;
                    }

                    Monitor.PulseAll(queue.SyncRoot);
                }

                if (startDrainer)
                {
                    StartDrainer(queue);
                }
            }
        }

        private static void FallbackToVanilla(
            SaveManager.SavedData dataToSave,
            string dataFileName,
            bool isJson,
            bool fullPath,
            string reason)
        {
            Debug.LogWarning(
                SaveWriteOrderingConstants.LogPrefix +
                reason +
                " Falling back to vanilla asynchronous saving for this request.");

            DataSaver.saveData<SaveManager.SavedData>(
                dataToSave,
                dataFileName,
                isJson,
                fullPath);
        }

        private static SavePathQueue GetOrCreateQueue(string path)
        {
            lock (RegistrySync)
            {
                SavePathQueue queue;
                if (!Queues.TryGetValue(path, out queue))
                {
                    queue = new SavePathQueue();
                    Queues.Add(path, queue);
                }

                return queue;
            }
        }

        private static SavePathQueue TryGetQueue(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            lock (RegistrySync)
            {
                SavePathQueue queue;
                return Queues.TryGetValue(path, out queue)
                    ? queue
                    : null;
            }
        }

        private static bool IsBusy(SavePathQueue queue)
        {
            return queue.Draining ||
                   queue.PendingWrites.Count > 0 ||
                   queue.ExternalAccessActive;
        }

        private static int GetRemainingMilliseconds(
            int timeoutMilliseconds,
            Stopwatch stopwatch)
        {
            long elapsed = stopwatch == null
                ? 0L
                : stopwatch.ElapsedMilliseconds;

            long remaining =
                (long)timeoutMilliseconds - elapsed;

            if (remaining <= 0L)
            {
                return 0;
            }

            return remaining > int.MaxValue
                ? int.MaxValue
                : (int)remaining;
        }

        private static void StartDrainer(SavePathQueue queue)
        {
            Thread thread = new Thread(
                new ThreadStart(
                    delegate
                    {
                        DrainQueue(queue);
                    }));

            // Vanilla's DataSaver threads are foreground threads. Preserve that
            // shutdown behavior so queued saves are not silently abandoned.
            thread.IsBackground = false;
            thread.Name = "Idol Manager ordered save writer";
            thread.Start();
        }

        private static void DrainQueue(SavePathQueue queue)
        {
            Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

            while (true)
            {
                FrozenSaveWrite write;

                lock (queue.SyncRoot)
                {
                    while (queue.ExternalAccessActive)
                    {
                        Monitor.Wait(queue.SyncRoot);
                    }

                    if (queue.PendingWrites.Count == 0)
                    {
                        queue.Draining = false;
                        Monitor.PulseAll(queue.SyncRoot);
                        return;
                    }

                    write = queue.PendingWrites.Dequeue();
                }

                WriteFrozenPayload(write);

                lock (queue.SyncRoot)
                {
                    Monitor.PulseAll(queue.SyncRoot);
                }
            }
        }

        private static void WriteFrozenPayload(FrozenSaveWrite write)
        {
            if (write == null ||
                string.IsNullOrEmpty(write.TargetPath))
            {
                return;
            }

            try
            {
                string directory =
                    Path.GetDirectoryName(write.TargetPath);

                if (!string.IsNullOrEmpty(directory) &&
                    !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] bytes = Encoding.UTF8.GetBytes(
                    write.Payload ?? string.Empty);

                File.WriteAllBytes(write.TargetPath, bytes);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Failed to write ordered vanilla save to: " +
                    write.TargetPath.Replace("/", "\\"));
                Debug.LogWarning(
                    SaveWriteOrderingConstants.LogPrefix +
                    "Error: " +
                    exception.Message);
            }
        }
    }
}
