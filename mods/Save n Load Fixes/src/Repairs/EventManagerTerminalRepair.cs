using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs N13 / finding #30. Vanilla consumes two Event_Manager terminal
    /// paths without transitioning the live occurrence from active to complete:
    /// a resultless ConcludeEvent reply and the automatic SNS-only delivery path.
    /// The repair changes only the already-serialized vanilla state field.
    /// </summary>
    internal static class EventManagerTerminalRepair
    {
        private static long resultlessCompletionCount;
        private static long snsOnlyCompletionCount;
        private static long alreadyTerminalCount;
        private static long skippedCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return EventManagerTerminalPatchHealth.IsHealthy; }
        }

        internal static long ResultlessCompletionCount
        {
            get { return Interlocked.Read(ref resultlessCompletionCount); }
        }

        internal static long SnsOnlyCompletionCount
        {
            get { return Interlocked.Read(ref snsOnlyCompletionCount); }
        }

        internal static long AlreadyTerminalCount
        {
            get { return Interlocked.Read(ref alreadyTerminalCount); }
        }

        internal static long SkippedCount
        {
            get { return Interlocked.Read(ref skippedCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        /// <summary>
        /// Injected immediately before the first frozen ConcludeEvent return, which the
        /// audited source guard proves is the no-results early return. The ordinary
        /// final return is not modified. The consumed occurrence is still active here
        /// and is canonicalized before any Harmony Postfix observes the method.
        /// </summary>
        internal static void CompleteResultlessEventBeforeReturn(Event_Manager manager)
        {
            if (manager == null)
            {
                Interlocked.Increment(ref skippedCount);
                lastDiagnostic = "N13 resultless terminal repair skipped because Event_Manager was null.";
                return;
            }

            Event_Manager._activeEvent activeEvent = manager.GetActiveEvent();
            if (activeEvent == null)
            {
                Interlocked.Increment(ref alreadyTerminalCount);
                lastDiagnostic =
                    "N13 ConcludeEvent return observed no active event; vanilla had already reached its ordinary complete state.";
                return;
            }

            if (activeEvent.state != Event_Manager._activeEvent._state.active)
            {
                Interlocked.Increment(ref skippedCount);
                lastDiagnostic =
                    "N13 resultless terminal repair found an event that was not in the audited active state.";
                return;
            }

            activeEvent.state = Event_Manager._activeEvent._state.complete;
            Interlocked.Increment(ref resultlessCompletionCount);
            lastDiagnostic =
                "N13 canonicalized a consumed resultless Event_Manager occurrence to complete before ConcludeEvent returned.";
        }

        /// <summary>
        /// Injected immediately after Event_Manager.AddSNS(...) in the exact
        /// SNS-only OpenPopup iterator branch. At this point the automatic SNS reply
        /// has committed, so the occurrence can become terminal before presentation
        /// continues. No generic SNS sink is patched.
        /// </summary>
        internal static void CompleteSnsOnlyEventAfterDelivery(
            Event_Manager._activeEvent activeEvent)
        {
            if (activeEvent == null)
            {
                Interlocked.Increment(ref skippedCount);
                lastDiagnostic = "N13 SNS-only terminal repair skipped because the active event was null.";
                return;
            }

            if (activeEvent.state == Event_Manager._activeEvent._state.complete)
            {
                Interlocked.Increment(ref alreadyTerminalCount);
                lastDiagnostic = "N13 SNS-only terminal repair observed an occurrence that was already complete.";
                return;
            }

            if (activeEvent.state != Event_Manager._activeEvent._state.active ||
                !activeEvent.IsSNS())
            {
                Interlocked.Increment(ref skippedCount);
                lastDiagnostic =
                    "N13 SNS-only terminal repair refused to canonicalize a non-active or non-SNS occurrence.";
                return;
            }

            activeEvent.state = Event_Manager._activeEvent._state.complete;
            Interlocked.Increment(ref snsOnlyCompletionCount);
            lastDiagnostic =
                "N13 canonicalized an automatically delivered SNS-only Event_Manager occurrence to complete.";
        }
    }

    internal static class EventManagerTerminalPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 2;
        internal const int ExpectedConcludeReturnSiteCount = 2;
        internal const int ExpectedSnsDeliverySiteCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static int observedConcludeReturnSiteCount = -1;
        private static int observedSnsDeliverySiteCount = -1;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        observedConcludeReturnSiteCount == ExpectedConcludeReturnSiteCount &&
                        observedSnsDeliverySiteCount == ExpectedSnsDeliverySiteCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount
        {
            get { lock (Sync) { return resolvedTargetMethodCount; } }
        }

        internal static int ObservedConcludeReturnSiteCount
        {
            get { lock (Sync) { return observedConcludeReturnSiteCount; } }
        }

        internal static int ObservedSnsDeliverySiteCount
        {
            get { lock (Sync) { return observedSnsDeliverySiteCount; } }
        }

        internal static string Failure
        {
            get { lock (Sync) { return failure; } }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N13 resolved more target methods than the frozen two-method manifest.";
                }
            }
        }

        internal static void ReportConcludeReturnSites(int observed)
        {
            lock (Sync)
            {
                observedConcludeReturnSiteCount = observed;
                if (observed != ExpectedConcludeReturnSiteCount)
                {
                    failure =
                        "N13 Event_Manager.ConcludeEvent return-site count changed from the frozen two-site manifest.";
                }
            }
        }

        internal static void ReportSnsDeliverySites(int observed)
        {
            lock (Sync)
            {
                observedSnsDeliverySiteCount = observed;
                if (observed != ExpectedSnsDeliverySiteCount)
                {
                    failure =
                        "N13 Event_Manager.<OpenPopup>d__46 SNS delivery-site count changed from the frozen one-site manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N13 patch failure";
            }
        }
    }
}
