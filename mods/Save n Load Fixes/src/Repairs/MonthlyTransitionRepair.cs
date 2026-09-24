using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A34 restores mainScript.onNewMonth at the calendar boundary where vanilla
    /// already detected a new day. Vanilla declares and subscribes this event but
    /// TimeProgress never invokes it, leaving monthly subscribers dormant.
    /// </summary>
    internal static class MonthlyTransitionRepair
    {
        private static readonly FieldInfo MonthEventField =
            AccessTools.Field(typeof(mainScript), "onNewMonth");

        private static long firstOfMonthBoundaryCount;
        private static long monthEventInvocationCount;
        private static long noSubscriberCount;
        private static string lastDiagnostic =
            "A34 has not yet observed a first-of-month game-time boundary.";

        internal static bool IsImplemented
        {
            get { return MonthlyTransitionPatchHealth.IsHealthy && MonthEventField != null; }
        }

        internal static bool MonthEventFieldResolved
        {
            get { return MonthEventField != null; }
        }

        internal static long FirstOfMonthBoundaryCount
        {
            get { return Interlocked.Read(ref firstOfMonthBoundaryCount); }
        }

        internal static long MonthEventInvocationCount
        {
            get { return Interlocked.Read(ref monthEventInvocationCount); }
        }

        internal static long NoSubscriberCount
        {
            get { return Interlocked.Read(ref noSubscriberCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        /// <summary>
        /// Called only from the injected TimeProgress site immediately after
        /// vanilla onNewDay has fired. Because game time advances continuously,
        /// the first day of a month is the exact missing monthly transition seam.
        /// </summary>
        internal static void RaiseAfterVanillaNewDay(mainScript owner)
        {
            if (staticVars.dateTime.Day != 1)
            {
                return;
            }

            Interlocked.Increment(ref firstOfMonthBoundaryCount);

            if (owner == null)
            {
                lastDiagnostic =
                    "A34 observed the first day of a month, but the TimeProgress owner was null.";
                return;
            }

            if (MonthEventField == null)
            {
                lastDiagnostic =
                    "A34 observed the first day of a month, but mainScript.onNewMonth could not be resolved.";
                return;
            }

            mainScript.newWeek callbacks =
                MonthEventField.GetValue(owner) as mainScript.newWeek;
            if (callbacks == null)
            {
                Interlocked.Increment(ref noSubscriberCount);
                lastDiagnostic =
                    "A34 observed the first day of a month; mainScript.onNewMonth had no subscribers.";
                return;
            }

            // Intentionally do not catch subscriber exceptions. A restored vanilla
            // event must preserve normal delegate invocation/failure semantics.
            callbacks();
            Interlocked.Increment(ref monthEventInvocationCount);
            lastDiagnostic =
                "A34 raised mainScript.onNewMonth once after vanilla onNewDay on the first day of the month.";
        }
    }

    internal static class MonthlyTransitionPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;
        internal const int ExpectedInjectionSiteCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static int injectionSiteCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        injectionSiteCount == ExpectedInjectionSiteCount &&
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

        internal static int InjectionSiteCount
        {
            get
            {
                lock (Sync)
                {
                    return injectionSiteCount;
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
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure =
                        "A34 resolved more TimeProgress.MoveNext targets than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportInjectionSiteCount(int observed)
        {
            lock (Sync)
            {
                injectionSiteCount = observed;
                if (observed != ExpectedInjectionSiteCount)
                {
                    failure =
                        "A34 expected exactly one mainScript.onNewDay invocation site in TimeProgress.MoveNext, found " +
                        observed.ToString() + ".";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A34 patch failure";
            }
        }
    }
}
