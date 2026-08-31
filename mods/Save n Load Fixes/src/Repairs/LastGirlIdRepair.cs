using System;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A05: data_girls.LoadFunction() restores the serialized LastGirlID,
    /// then reconstructs every saved idol through GenerateGirl(), which consumes a
    /// fresh temporary ID before vanilla overwrites it with the saved idol ID.
    ///
    /// The serialized allocator is already authoritative. Preserve it across the
    /// reconstruction loop instead of introducing any new persistence.
    /// </summary>
    internal static class LastGirlIdRepair
    {
        internal struct LoadState
        {
            internal bool Captured;
            internal int SerializedLastGirlId;
        }

        private static long capturedCount;
        private static long restoredCount;
        private static long noChangeCount;
        private static long captureFailureCount;
        private static long spuriousAdvanceRemovedCount;
        private static int lastSerializedAllocator;
        private static int lastObservedBeforeRestore;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return LastGirlIdPatchHealth.IsHealthy; }
        }

        internal static long CapturedCount
        {
            get { return Interlocked.Read(ref capturedCount); }
        }

        internal static long RestoredCount
        {
            get { return Interlocked.Read(ref restoredCount); }
        }

        internal static long NoChangeCount
        {
            get { return Interlocked.Read(ref noChangeCount); }
        }

        internal static long CaptureFailureCount
        {
            get { return Interlocked.Read(ref captureFailureCount); }
        }

        internal static long SpuriousAdvanceRemovedCount
        {
            get { return Interlocked.Read(ref spuriousAdvanceRemovedCount); }
        }

        internal static int LastSerializedAllocator
        {
            get { return Volatile.Read(ref lastSerializedAllocator); }
        }

        internal static int LastObservedBeforeRestore
        {
            get { return Volatile.Read(ref lastObservedBeforeRestore); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static LoadState CaptureSerializedAllocator()
        {
            LoadState state = new LoadState();

            try
            {
                Camera camera = Camera.main;
                if (camera == null)
                {
                    return CaptureFailed("Camera.main is unavailable at data_girls.LoadFunction entry");
                }

                mainScript main = camera.GetComponent<mainScript>();
                if (main == null)
                {
                    return CaptureFailed("mainScript is unavailable at data_girls.LoadFunction entry");
                }

                SaveManager.SavedData data = main.GetSavedData();
                if (data == null)
                {
                    return CaptureFailed("target SavedData is unavailable at data_girls.LoadFunction entry");
                }

                state.Captured = true;
                state.SerializedLastGirlId = data.data_girls__LastGirlID;

                Interlocked.Increment(ref capturedCount);
                Volatile.Write(ref lastSerializedAllocator, state.SerializedLastGirlId);
                lastDiagnostic =
                    "Captured serialized data_girls.LastGirlID=" +
                    state.SerializedLastGirlId +
                    " before idol reconstruction.";
                return state;
            }
            catch (Exception exception)
            {
                return CaptureFailed(
                    "Could not capture serialized data_girls.LastGirlID: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
        }

        internal static void RestoreSerializedAllocator(LoadState state)
        {
            if (!state.Captured)
            {
                return;
            }

            int observed = data_girls.LastGirlID;
            Volatile.Write(ref lastObservedBeforeRestore, observed);

            if (observed == state.SerializedLastGirlId)
            {
                Interlocked.Increment(ref noChangeCount);
                lastDiagnostic =
                    "data_girls.LastGirlID remained equal to serialized allocator " +
                    state.SerializedLastGirlId +
                    " after reconstruction; no correction was needed.";
                return;
            }

            data_girls.LastGirlID = state.SerializedLastGirlId;
            Interlocked.Increment(ref restoredCount);

            if (observed > state.SerializedLastGirlId)
            {
                Interlocked.Add(
                    ref spuriousAdvanceRemovedCount,
                    (long)observed - state.SerializedLastGirlId);
            }

            lastDiagnostic =
                "Restored serialized data_girls.LastGirlID=" +
                state.SerializedLastGirlId +
                " after vanilla reconstruction left allocator=" + observed + ".";
        }

        private static LoadState CaptureFailed(string diagnostic)
        {
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = diagnostic ?? "unknown LastGirlID capture failure";
            return new LoadState();
        }
    }

    internal static class LastGirlIdPatchHealth
    {
        private static readonly object Sync = new object();
        private static bool targetResolved;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return targetResolved && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static string Failure
        {
            get
            {
                lock (Sync)
                {
                    return failure;
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                targetResolved = true;
            }
        }

        internal static void ReportFailure(string message)
        {
            lock (Sync)
            {
                failure = message ?? "unknown LastGirlID patch failure";
            }
        }
    }
}
