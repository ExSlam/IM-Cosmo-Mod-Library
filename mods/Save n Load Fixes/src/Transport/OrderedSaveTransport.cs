using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace SaveNLoadFixes.Transport
{
    internal static class SavePathResolver
    {
        internal static string ResolveWritePath(string dataFileName, bool fullPath)
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
                path = Path.Combine(Application.persistentDataPath, "data");
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

            string path = Path.Combine(Application.persistentDataPath, "data");
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
                return path;
            }
        }
    }

    internal sealed class FrozenSaveWrite
    {
        internal string TargetPath = string.Empty;
        internal string Payload = string.Empty;
        internal object DeferredDataToSerialize;
        internal bool SerializeOnWriter;
        internal bool IsJson = true;
    }

    internal sealed class SavePathQueue
    {
        internal readonly object SyncRoot = new object();
        internal readonly Queue<FrozenSaveWrite> PendingWrites =
            new Queue<FrozenSaveWrite>();

        internal bool Draining;
        internal bool ExternalAccessActive;
    }

    internal sealed class SaveDirectoryExclusiveLease : IDisposable
    {
        private List<SavePathQueue> queues;
        private string directoryPath;

        internal SaveDirectoryExclusiveLease(
            string normalizedDirectoryPath,
            List<SavePathQueue> acquiredQueues)
        {
            directoryPath = normalizedDirectoryPath ?? string.Empty;
            queues = acquiredQueues ?? new List<SavePathQueue>();
        }

        public void Dispose()
        {
            List<SavePathQueue> acquiredQueues =
                Interlocked.Exchange(ref queues, null);
            if (acquiredQueues == null)
            {
                return;
            }

            string registeredDirectory = directoryPath;
            directoryPath = string.Empty;

            OrderedSaveTransport.ReleaseExclusiveQueues(acquiredQueues);
            OrderedSaveTransport.ReleaseExclusiveDirectoryRegistration(
                registeredDirectory);
        }
    }

    /// <summary>
    /// Save n Load Fixes' embedded ordered vanilla transport.
    ///
    /// FIFO ordering is per physical file. The caller thread freezes the requested
    /// payload before queue admission, then one foreground writer drains each path.
    /// Reads wait for the matching path to drain. No constructed DataSaver&lt;T&gt;
    /// generic method is Harmony-patched.
    /// </summary>
    internal static class OrderedSaveTransport
    {
        private static readonly object RegistrySync = new object();

        private static readonly StringComparer PathComparer =
            Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        private static readonly Dictionary<string, SavePathQueue> Queues =
            new Dictionary<string, SavePathQueue>(PathComparer);

        private static readonly List<string> ExclusiveDirectories =
            new List<string>();

        internal static void QueueSavedDataWrite(
            SaveManager.SavedData dataToSave,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            string targetPath =
                SavePathResolver.ResolveWritePath(dataFileName, fullPath);

            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogError(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Repair-dependent SavedData request was not written because its physical path could not be resolved.");
                return;
            }

            string payload;
            string checkpointId;
            string error;
            if (!RepairEnvelopeTransport.TryFreezeSavedDataPayload(
                    dataToSave,
                    isJson,
                    out payload,
                    out checkpointId,
                    out error))
            {
                // Never move repair-envelope capture onto the writer thread. A failed
                // exact freeze must not produce a vanilla/repair split-brain checkpoint.
                Debug.LogError(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Repair-dependent SavedData request was not written: " +
                    (error ?? string.Empty));
                return;
            }

            QueuePreparedWrite(
                targetPath,
                payload,
                null,
                false,
                true);
        }

        internal static void QueueGlobalDataWrite(
            SaveManager.GlobalData dataToSave,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            string targetPath =
                SavePathResolver.ResolveWritePath(dataFileName, fullPath);

            if (string.IsNullOrEmpty(targetPath))
            {
                FallbackGlobalDataToVanilla(
                    dataToSave,
                    dataFileName,
                    isJson,
                    fullPath,
                    "Could not resolve the physical GlobalData path.");
                return;
            }

            QueueObjectWrite(
                dataToSave,
                targetPath,
                isJson,
                "GlobalData");
        }

        internal static SaveManager.SavedData LoadSavedDataAfterPendingWrites(
            string dataFileName)
        {
            WaitForReadPath(dataFileName, "SavedData");
            string physicalPath = SavePathResolver.ResolveReadPath(dataFileName);
            return RepairEnvelopeTransport.LoadSavedDataFromPhysicalPath(physicalPath);
        }

        internal static SaveManager.GlobalData LoadGlobalDataAfterPendingWrites(
            string dataFileName)
        {
            WaitForReadPath(dataFileName, "GlobalData");
            return DataSaver.loadData<SaveManager.GlobalData>(dataFileName);
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

        internal static bool TryAcquireExclusiveDirectoryAccess(
            string normalizedDirectoryPath,
            int timeoutMilliseconds,
            out IDisposable exclusiveAccess,
            out string errorMessage)
        {
            exclusiveAccess = null;
            errorMessage = string.Empty;

            Stopwatch stopwatch =
                timeoutMilliseconds == Timeout.Infinite
                    ? null
                    : Stopwatch.StartNew();
            List<KeyValuePair<string, SavePathQueue>> matchingQueues =
                new List<KeyValuePair<string, SavePathQueue>>();
            bool directoryRegistered = false;

            lock (RegistrySync)
            {
                while (HasOverlappingExclusiveDirectoryLocked(
                    normalizedDirectoryPath))
                {
                    if (timeoutMilliseconds == Timeout.Infinite)
                    {
                        Monitor.Wait(RegistrySync);
                        continue;
                    }

                    int remaining = GetRemainingMilliseconds(
                        timeoutMilliseconds,
                        stopwatch);
                    if (remaining <= 0 ||
                        !Monitor.Wait(RegistrySync, remaining))
                    {
                        errorMessage =
                            "Timed out waiting for exclusive vanilla save-directory access.";
                        return false;
                    }
                }

                ExclusiveDirectories.Add(normalizedDirectoryPath);
                directoryRegistered = true;

                foreach (KeyValuePair<string, SavePathQueue> entry in Queues)
                {
                    if (IsSameOrContainedPath(
                            normalizedDirectoryPath,
                            entry.Key))
                    {
                        matchingQueues.Add(entry);
                    }
                }
            }

            matchingQueues.Sort(
                delegate(
                    KeyValuePair<string, SavePathQueue> left,
                    KeyValuePair<string, SavePathQueue> right)
                {
                    return PathComparer.Compare(left.Key, right.Key);
                });

            List<SavePathQueue> acquiredQueues = new List<SavePathQueue>();
            try
            {
                for (int index = 0; index < matchingQueues.Count; index++)
                {
                    int remaining = timeoutMilliseconds == Timeout.Infinite
                        ? Timeout.Infinite
                        : GetRemainingMilliseconds(timeoutMilliseconds, stopwatch);

                    if (remaining == 0 ||
                        !TryAcquireQueueExclusive(
                            matchingQueues[index].Value,
                            remaining,
                            out errorMessage))
                    {
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            errorMessage =
                                "Timed out waiting for exclusive vanilla save-directory access.";
                        }
                        return false;
                    }

                    acquiredQueues.Add(matchingQueues[index].Value);
                }

                exclusiveAccess = new SaveDirectoryExclusiveLease(
                    normalizedDirectoryPath,
                    acquiredQueues);
                directoryRegistered = false;
                acquiredQueues = null;
                return true;
            }
            finally
            {
                if (acquiredQueues != null)
                {
                    ReleaseExclusiveQueues(acquiredQueues);
                }

                if (directoryRegistered)
                {
                    ReleaseExclusiveDirectoryRegistration(
                        normalizedDirectoryPath);
                }
            }
        }

        internal static bool TryRunExclusiveFileAccess(
            string normalizedPath,
            Action fileAction,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            Stopwatch stopwatch =
                timeoutMilliseconds == Timeout.Infinite
                    ? null
                    : Stopwatch.StartNew();

            SavePathQueue queue;
            if (!TryGetOrCreateQueueForExclusiveFile(
                    normalizedPath,
                    timeoutMilliseconds,
                    stopwatch,
                    out queue,
                    out errorMessage))
            {
                return false;
            }

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

        internal static void ReleaseExclusiveDirectoryRegistration(
            string normalizedDirectoryPath)
        {
            if (string.IsNullOrEmpty(normalizedDirectoryPath))
            {
                return;
            }

            lock (RegistrySync)
            {
                for (int index = ExclusiveDirectories.Count - 1; index >= 0; index--)
                {
                    if (PathComparer.Equals(
                            ExclusiveDirectories[index],
                            normalizedDirectoryPath))
                    {
                        ExclusiveDirectories.RemoveAt(index);
                        break;
                    }
                }

                Monitor.PulseAll(RegistrySync);
            }
        }

        internal static void ReleaseExclusiveQueues(
            List<SavePathQueue> acquiredQueues)
        {
            if (acquiredQueues == null)
            {
                return;
            }

            for (int index = acquiredQueues.Count - 1; index >= 0; index--)
            {
                SavePathQueue queue = acquiredQueues[index];
                if (queue == null)
                {
                    continue;
                }

                bool startDrainer = false;
                lock (queue.SyncRoot)
                {
                    queue.ExternalAccessActive = false;
                    if (queue.PendingWrites.Count > 0 && !queue.Draining)
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

        private static void QueueObjectWrite(
            object dataToSave,
            string targetPath,
            bool isJson,
            string payloadKind)
        {
            string payload;
            try
            {
                payload = isJson
                    ? JsonUtility.ToJson(dataToSave, true)
                    : Convert.ToString(dataToSave, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Could not freeze " +
                    payloadKind +
                    " on the caller thread: " +
                    exception.Message +
                    " Retrying serialization inside the ordered writer.");
                payload = null;
            }

            QueuePreparedWrite(
                targetPath,
                payload ?? string.Empty,
                payload == null ? dataToSave : null,
                payload == null,
                isJson);
        }

        private static void QueuePreparedWrite(
            string targetPath,
            string payload,
            object deferredDataToSerialize,
            bool serializeOnWriter,
            bool isJson)
        {
            SavePathQueue queue;
            bool startDrainer = false;

            lock (RegistrySync)
            {
                while (IsPathBlockedByExclusiveDirectoryLocked(targetPath))
                {
                    Monitor.Wait(RegistrySync);
                }

                if (!Queues.TryGetValue(targetPath, out queue))
                {
                    queue = new SavePathQueue();
                    Queues.Add(targetPath, queue);
                }

                lock (queue.SyncRoot)
                {
                    queue.PendingWrites.Enqueue(
                        new FrozenSaveWrite
                        {
                            TargetPath = targetPath,
                            Payload = payload ?? string.Empty,
                            DeferredDataToSerialize = deferredDataToSerialize,
                            SerializeOnWriter = serializeOnWriter,
                            IsJson = isJson
                        });

                    if (!queue.Draining &&
                        !queue.ExternalAccessActive)
                    {
                        queue.Draining = true;
                        startDrainer = true;
                    }

                    Monitor.PulseAll(queue.SyncRoot);
                }
            }

            if (startDrainer)
            {
                StartDrainer(queue);
            }
        }

        private static void WaitForReadPath(
            string dataFileName,
            string payloadKind)
        {
            string physicalPath =
                SavePathResolver.ResolveReadPath(dataFileName);

            if (string.IsNullOrEmpty(physicalPath))
            {
                return;
            }

            bool completed = WaitForPath(
                physicalPath,
                SaveNLoadFixesConstants.LoadWaitTimeoutMilliseconds);

            if (!completed)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Timed out waiting for an ordered " +
                    payloadKind +
                    " write before reading " +
                    physicalPath +
                    ". Vanilla load will continue.");
            }
        }

        private static bool TryAcquireQueueExclusive(
            SavePathQueue queue,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;
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
                return true;
            }
        }

        private static bool IsPathBlockedByExclusiveDirectoryLocked(
            string normalizedPath)
        {
            for (int index = 0; index < ExclusiveDirectories.Count; index++)
            {
                if (IsSameOrContainedPath(
                        ExclusiveDirectories[index],
                        normalizedPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasOverlappingExclusiveDirectoryLocked(
            string normalizedDirectoryPath)
        {
            for (int index = 0; index < ExclusiveDirectories.Count; index++)
            {
                string existing = ExclusiveDirectories[index];
                if (IsSameOrContainedPath(existing, normalizedDirectoryPath) ||
                    IsSameOrContainedPath(normalizedDirectoryPath, existing))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameOrContainedPath(
            string directoryPath,
            string candidatePath)
        {
            if (string.IsNullOrEmpty(directoryPath) ||
                string.IsNullOrEmpty(candidatePath))
            {
                return false;
            }

            if (PathComparer.Equals(directoryPath, candidatePath))
            {
                return true;
            }

            string normalizedDirectory = directoryPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string prefix = normalizedDirectory + Path.DirectorySeparatorChar;
            StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return candidatePath.StartsWith(prefix, comparison);
        }

        private static void FallbackGlobalDataToVanilla(
            SaveManager.GlobalData dataToSave,
            string dataFileName,
            bool isJson,
            bool fullPath,
            string reason)
        {
            Debug.LogWarning(
                SaveNLoadFixesConstants.LogPrefix +
                reason +
                " Falling back to vanilla asynchronous saving for this request.");

            DataSaver.saveData<SaveManager.GlobalData>(
                dataToSave,
                dataFileName,
                isJson,
                fullPath);
        }

        private static bool TryGetOrCreateQueueForExclusiveFile(
            string path,
            int timeoutMilliseconds,
            Stopwatch stopwatch,
            out SavePathQueue queue,
            out string errorMessage)
        {
            queue = null;
            errorMessage = string.Empty;

            lock (RegistrySync)
            {
                while (IsPathBlockedByExclusiveDirectoryLocked(path))
                {
                    if (timeoutMilliseconds == Timeout.Infinite)
                    {
                        Monitor.Wait(RegistrySync);
                        continue;
                    }

                    int remaining = GetRemainingMilliseconds(
                        timeoutMilliseconds,
                        stopwatch);
                    if (remaining <= 0 ||
                        !Monitor.Wait(RegistrySync, remaining))
                    {
                        errorMessage =
                            "Timed out waiting for an overlapping exclusive save-directory lease.";
                        return false;
                    }
                }

                if (!Queues.TryGetValue(path, out queue))
                {
                    queue = new SavePathQueue();
                    Queues.Add(path, queue);
                }

                return true;
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

            thread.IsBackground = false;
            thread.Name = "Idol Manager SNLF ordered save writer";
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
                string payload = write.Payload ?? string.Empty;
                if (write.SerializeOnWriter)
                {
                    payload = write.IsJson
                        ? JsonUtility.ToJson(write.DeferredDataToSerialize, true)
                        : Convert.ToString(
                            write.DeferredDataToSerialize,
                            CultureInfo.InvariantCulture);
                }

                string directory =
                    Path.GetDirectoryName(write.TargetPath);

                if (!string.IsNullOrEmpty(directory) &&
                    !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] bytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);
                File.WriteAllBytes(write.TargetPath, bytes);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Failed to write ordered vanilla data to: " +
                    write.TargetPath.Replace("/", "\\"));
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Error: " +
                    exception.Message);
            }
        }
    }
}
