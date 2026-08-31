using System;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs the current-format risky-marketing zero sentinel collision.
    /// Vanilla serializes Marketing_Result, then rerolls an authoritative 0f while
    /// reconstructing the single. The repair restores only the already-serialized
    /// value after vanilla reconstruction. It intentionally does not own new state.
    /// </summary>
    internal static class RiskyMarketingZeroRepair
    {
        private static long authoritativeZeroObservedCount;
        private static long restoredAfterRerollCount;
        private static long currentFormatNoChangeCount;
        private static long legacyOrUnknownSkippedCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return RiskyMarketingZeroPatchHealth.IsHealthy; }
        }

        internal static long AuthoritativeZeroObservedCount
        {
            get { return Interlocked.Read(ref authoritativeZeroObservedCount); }
        }

        internal static long RestoredAfterRerollCount
        {
            get { return Interlocked.Read(ref restoredAfterRerollCount); }
        }

        internal static long CurrentFormatNoChangeCount
        {
            get { return Interlocked.Read(ref currentFormatNoChangeCount); }
        }

        internal static long LegacyOrUnknownSkippedCount
        {
            get { return Interlocked.Read(ref legacyOrUnknownSkippedCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void RestoreSerializedCurrentFormatValue(
            singles.SinglesData serialized,
            singles._single loaded)
        {
            if (serialized == null || loaded == null || serialized.Marketing_Result != 0f)
            {
                return;
            }

            Interlocked.Increment(ref authoritativeZeroObservedCount);

            string saveVersion;
            if (!TryGetCurrentTargetSaveVersion(out saveVersion) ||
                !IsCurrentFormat(saveVersion))
            {
                Interlocked.Increment(ref legacyOrUnknownSkippedCount);
                lastDiagnostic =
                    "Skipped risky-marketing zero restoration for legacy/unknown save version '" +
                    (saveVersion ?? "<unavailable>") +
                    "'; legacy compatibility remains a later migration concern.";
                return;
            }

            bool vanillaRerolled = loaded.Marketing_Result != serialized.Marketing_Result;

            // The serialized DTO is authoritative for current-format saves. Reassign it
            // after vanilla's zero-sentinel branch. Marketing_Result_Status was already
            // restored independently by vanilla and is intentionally left untouched.
            loaded.Marketing_Result = serialized.Marketing_Result;

            if (vanillaRerolled)
            {
                Interlocked.Increment(ref restoredAfterRerollCount);
                lastDiagnostic =
                    "Restored authoritative risky-marketing result 0 after vanilla rerolled it during load.";
            }
            else
            {
                Interlocked.Increment(ref currentFormatNoChangeCount);
                lastDiagnostic =
                    "Observed authoritative risky-marketing result 0; vanilla reroll happened to remain 0, so no value change was needed.";
            }
        }

        private static bool TryGetCurrentTargetSaveVersion(out string version)
        {
            version = null;

            try
            {
                Camera camera = Camera.main;
                if (camera == null)
                {
                    return false;
                }

                mainScript main = camera.GetComponent<mainScript>();
                if (main == null)
                {
                    return false;
                }

                SaveManager.SavedData data = main.GetSavedData();
                if (data == null)
                {
                    return false;
                }

                version = data.version;
                return !string.IsNullOrEmpty(version);
            }
            catch (Exception exception)
            {
                lastDiagnostic =
                    "Could not resolve target save version for risky-marketing zero repair: " +
                    exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private static bool IsCurrentFormat(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            // The supplied audited game snapshot is v1.0.6. This first low-risk repair
            // deliberately handles current-format saves only. Older-save migration is
            // reserved for the later compatibility wave rather than guessing whether an
            // absent legacy JSON field was a legitimate zero.
            return (int)mainScript.GetVersion(version) >= (int)mainScript._version.v1_0_6;
        }
    }

    internal static class RiskyMarketingZeroPatchHealth
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
                failure = message ?? "unknown risky-marketing patch failure";
            }
        }
    }
}
