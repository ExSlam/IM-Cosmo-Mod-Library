using System;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A07: SEvent_Concerts.GetConcertDataForLoading() restores the serialized
    /// FinishDate, but the immediately following _concert.Initiate() call overwrites
    /// unfinished concerts with staticVars.dateTime. The loader then renders the special-
    /// events tab using that corrupted date as LaunchDate.
    ///
    /// This wrapper is used only at the single Initiate() call inside
    /// SEvent_Concerts.LoadFunction(). It preserves vanilla Initiate() semantics and
    /// reapplies only a non-default loaded FinishDate for an unfinished concert.
    /// </summary>
    internal static class ConcertFinishDateRepair
    {
        private static long loaderInitiateCount;
        private static long restoredCount;
        private static long unfinishedWithoutSavedDateCount;
        private static long finishedConcertCount;
        private static long changedByInitiateCount;
        private static long lastRestoredTicks;
        private static long lastOverwrittenTicks;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return ConcertFinishDatePatchHealth.IsHealthy; }
        }

        internal static long LoaderInitiateCount
        {
            get { return Interlocked.Read(ref loaderInitiateCount); }
        }

        internal static long RestoredCount
        {
            get { return Interlocked.Read(ref restoredCount); }
        }

        internal static long UnfinishedWithoutSavedDateCount
        {
            get { return Interlocked.Read(ref unfinishedWithoutSavedDateCount); }
        }

        internal static long FinishedConcertCount
        {
            get { return Interlocked.Read(ref finishedConcertCount); }
        }

        internal static long ChangedByInitiateCount
        {
            get { return Interlocked.Read(ref changedByInitiateCount); }
        }

        internal static long LastRestoredTicks
        {
            get { return Interlocked.Read(ref lastRestoredTicks); }
        }

        internal static long LastOverwrittenTicks
        {
            get { return Interlocked.Read(ref lastOverwrittenTicks); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        /// <summary>
        /// Stack-compatible replacement for the audited loader-local call to
        /// SEvent_Concerts._concert.Initiate(). The wrapper intentionally calls vanilla
        /// Initiate() exactly once and does not patch Initiate() globally.
        /// </summary>
        internal static void InitiateLoadedConcertPreservingFinishDate(
            SEvent_Concerts._concert concert)
        {
            if (concert == null)
            {
                // Preserve the original call site's failure semantics rather than silently
                // accepting malformed reconstruction input.
                throw new NullReferenceException(
                    "SEvent_Concerts.LoadFunction produced a null concert before Initiate().");
            }

            DateTime serializedFinishDate = concert.FinishDate;
            SEvent_Tour.tour._status serializedStatus = concert.Status;

            Interlocked.Increment(ref loaderInitiateCount);

            // Vanilla remains authoritative for every other Initiate() side effect.
            concert.Initiate();

            if (serializedStatus == SEvent_Tour.tour._status.finished)
            {
                Interlocked.Increment(ref finishedConcertCount);
                lastDiagnostic =
                    "Finished concert ID=" + concert.ID +
                    " retained vanilla Initiate semantics; FinishDate required no A07 restoration.";
                return;
            }

            if (serializedFinishDate == default(DateTime))
            {
                // Empty/legacy rows have no authoritative date to restore. Keep vanilla's
                // initialization to the current game date instead of fabricating one.
                Interlocked.Increment(ref unfinishedWithoutSavedDateCount);
                lastDiagnostic =
                    "Unfinished concert ID=" + concert.ID +
                    " had no serialized FinishDate; vanilla Initiate date was retained.";
                return;
            }

            DateTime overwrittenFinishDate = concert.FinishDate;
            if (overwrittenFinishDate != serializedFinishDate)
            {
                Interlocked.Increment(ref changedByInitiateCount);
            }

            concert.FinishDate = serializedFinishDate;
            Interlocked.Exchange(ref lastRestoredTicks, serializedFinishDate.Ticks);
            Interlocked.Exchange(ref lastOverwrittenTicks, overwrittenFinishDate.Ticks);
            Interlocked.Increment(ref restoredCount);

            lastDiagnostic =
                "Restored unfinished concert ID=" + concert.ID +
                " FinishDate ticks=" + serializedFinishDate.Ticks +
                " after loader-local Initiate observed ticks=" + overwrittenFinishDate.Ticks + ".";
        }
    }

    internal static class ConcertFinishDatePatchHealth
    {
        private static readonly object Sync = new object();
        private static bool targetResolved;
        private static int observedInitiateSites = -1;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return targetResolved &&
                        observedInitiateSites == 1 &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ObservedInitiateSites
        {
            get
            {
                lock (Sync)
                {
                    return observedInitiateSites;
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

        internal static void ReportInitiateSites(int observed)
        {
            lock (Sync)
            {
                observedInitiateSites = observed;
                if (observed != 1)
                {
                    failure =
                        "SEvent_Concerts.LoadFunction expected exactly one loader-local " +
                        "_concert.Initiate() call but observed " + observed + ".";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A07 patch failure";
            }
        }
    }
}
