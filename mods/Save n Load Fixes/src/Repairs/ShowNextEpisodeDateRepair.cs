namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A26: Shows._show.GetNextEpisodeDate(DateTime) intends to advance the
    /// supplied starting date by one day before searching for the next launch weekday,
    /// but DateTime is immutable and vanilla discards that first AddDays result.
    ///
    /// The implementation is intentionally IL-local. It does not reimplement weekday
    /// arithmetic or cancellation-date logic; the transpiler only changes the one
    /// discarded AddDays result into an assignment back to the existing _start argument.
    /// </summary>
    internal static class ShowNextEpisodeDateRepair
    {
        internal static bool IsImplemented
        {
            get { return ShowNextEpisodeDatePatchHealth.IsHealthy; }
        }

        internal static int ResolvedTargetMethodCount
        {
            get { return ShowNextEpisodeDatePatchHealth.ResolvedTargetMethodCount; }
        }

        internal static int ObservedDiscardedAdvanceSiteCount
        {
            get { return ShowNextEpisodeDatePatchHealth.ObservedDiscardedAdvanceSiteCount; }
        }

        internal static string LastDiagnostic
        {
            get { return ShowNextEpisodeDatePatchHealth.LastDiagnostic; }
        }
    }

    internal static class ShowNextEpisodeDatePatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;
        internal const int ExpectedDiscardedAdvanceSiteCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static int observedDiscardedAdvanceSiteCount = -1;
        private static string failure = string.Empty;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        observedDiscardedAdvanceSiteCount == ExpectedDiscardedAdvanceSiteCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount;
                }
            }
        }

        internal static int ObservedDiscardedAdvanceSiteCount
        {
            get
            {
                lock (Sync)
                {
                    return observedDiscardedAdvanceSiteCount;
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

        internal static string LastDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastDiagnostic;
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A26 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportDiscardedAdvanceSites(int observed)
        {
            lock (Sync)
            {
                observedDiscardedAdvanceSiteCount = observed;
                if (observed != ExpectedDiscardedAdvanceSiteCount)
                {
                    failure =
                        "Shows._show.GetNextEpisodeDate(DateTime) expected exactly one discarded " +
                        "initial DateTime.AddDays result but observed " + observed + ".";
                    lastDiagnostic = failure;
                    return;
                }

                lastDiagnostic =
                    "A26 rewrote the single discarded initial AddDays result into assignment " +
                    "back to the _start argument.";
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A26 patch failure";
                lastDiagnostic = failure;
            }
        }
    }
}
