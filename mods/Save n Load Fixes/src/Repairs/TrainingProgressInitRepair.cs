using System;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A01. Vanilla serializes the current training Progress, startTime, and
    /// CompletionTime but omits the private runtime Progress_Init baseline consumed by
    /// girlTraining OnTimeTick(). Reconstruct the omitted baseline exactly from the
    /// adopted target save instead of persisting a second copy.
    /// </summary>
    internal static class TrainingProgressInitRepair
    {
        private static long reconstructedCount;
        private static long nonTrainingPassThroughCount;
        private static long invalidSerializedInputCount;
        private static long targetSaveTimeUnavailableCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return TrainingProgressInitPatchHealth.IsHealthy; }
        }

        internal static long ReconstructedCount { get { return Interlocked.Read(ref reconstructedCount); } }
        internal static long NonTrainingPassThroughCount { get { return Interlocked.Read(ref nonTrainingPassThroughCount); } }
        internal static long InvalidSerializedInputCount { get { return Interlocked.Read(ref invalidSerializedInputCount); } }
        internal static long TargetSaveTimeUnavailableCount { get { return Interlocked.Read(ref targetSaveTimeUnavailableCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        /// <summary>
        /// Called after vanilla has rebuilt one room from its RoomData row. Only active
        /// girlTraining rooms need Progress_Init because every other OnTimeTick branch
        /// uses CalcProgress(), which ignores this private baseline.
        /// </summary>
        internal static void ReconstructFromTargetSave(agency.RoomData saved, agency._room loaded)
        {
            if (saved == null || loaded == null)
            {
                Interlocked.Increment(ref invalidSerializedInputCount);
                lastDiagnostic = "A01 skipped because the serialized or reconstructed room was null.";
                return;
            }

            if (saved.status != agency._room._status.girlTraining)
            {
                Interlocked.Increment(ref nonTrainingPassThroughCount);
                return;
            }

            if (loaded.status != agency._room._status.girlTraining ||
                saved.CompletionTime <= 0f ||
                float.IsNaN(saved.CompletionTime) ||
                float.IsInfinity(saved.CompletionTime) ||
                float.IsNaN(saved.Progress) ||
                float.IsInfinity(saved.Progress) ||
                string.IsNullOrEmpty(saved.startTime))
            {
                Interlocked.Increment(ref invalidSerializedInputCount);
                lastDiagnostic = "A01 failed closed for a malformed girlTraining room row.";
                return;
            }

            DateTime targetSaveTime;
            if (!TryGetTargetSaveTime(out targetSaveTime))
            {
                Interlocked.Increment(ref targetSaveTimeUnavailableCount);
                lastDiagnostic = "A01 could not read the adopted target save's serialized game time.";
                return;
            }

            DateTime savedStartTime;
            try
            {
                savedStartTime = ExtensionMethods.ToDateTime(saved.startTime);
            }
            catch (Exception)
            {
                Interlocked.Increment(ref invalidSerializedInputCount);
                lastDiagnostic = "A01 failed closed because the saved training startTime was invalid.";
                return;
            }

            double elapsedMinutes = (targetSaveTime - savedStartTime).TotalMinutes;
            if (elapsedMinutes < 0d || double.IsNaN(elapsedMinutes) || double.IsInfinity(elapsedMinutes))
            {
                Interlocked.Increment(ref invalidSerializedInputCount);
                lastDiagnostic = "A01 failed closed because target save time precedes the saved training startTime.";
                return;
            }

            double progressInit = (double)saved.Progress -
                (elapsedMinutes / (double)saved.CompletionTime);
            if (double.IsNaN(progressInit) || double.IsInfinity(progressInit) ||
                progressInit > float.MaxValue || progressInit < -float.MaxValue)
            {
                Interlocked.Increment(ref invalidSerializedInputCount);
                lastDiagnostic = "A01 failed closed because the derived Progress_Init was not a finite float.";
                return;
            }

            // Deliberately do not clamp. This is the algebraic inverse of vanilla's
            // CalcProgress_Init(), and clamping would change a valid serialized timeline.
            loaded.Progress_Init = (float)progressInit;
            Interlocked.Increment(ref reconstructedCount);
            lastDiagnostic = "A01 reconstructed training Progress_Init from the adopted target save.";
        }

        private static bool TryGetTargetSaveTime(out DateTime targetSaveTime)
        {
            targetSaveTime = default(DateTime);
            try
            {
                if (Camera.main == null)
                {
                    return false;
                }

                mainScript main = Camera.main.GetComponent<mainScript>();
                if (main == null)
                {
                    return false;
                }

                SaveManager.SavedData target = main.GetSavedData();
                if (target == null || string.IsNullOrEmpty(target.staticVars__dateTime))
                {
                    return false;
                }

                targetSaveTime = ExtensionMethods.ToDateTime(target.staticVars__dateTime);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal static class TrainingProgressInitPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount { get { lock (Sync) { return resolvedTargetMethodCount; } } }
        internal static string Failure { get { lock (Sync) { return failure; } } }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A01 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A01 patch failure";
            }
        }
    }
}
