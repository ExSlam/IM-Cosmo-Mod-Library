using System;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A25 by preserving vanilla's exact DateTime delta formulas while making
    /// their immutable return values authoritative. Harmony transpilers route the six
    /// audited Graduation_Date AddMonths/AddDays statements through these pure helpers
    /// and store the returned DateTime back into the same idol field.
    /// </summary>
    internal static class GraduationDateRepair
    {
        private static long addMonthsAppliedCount;
        private static long addDaysAppliedCount;
        private static long lastOriginalTicks;
        private static long lastAdjustedTicks;
        private static int lastMonthsDelta;
        private static long lastDaysDeltaBits;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return GraduationDatePatchHealth.IsHealthy; }
        }

        internal static long AddMonthsAppliedCount
        {
            get { return Interlocked.Read(ref addMonthsAppliedCount); }
        }

        internal static long AddDaysAppliedCount
        {
            get { return Interlocked.Read(ref addDaysAppliedCount); }
        }

        internal static long LastOriginalTicks
        {
            get { return Interlocked.Read(ref lastOriginalTicks); }
        }

        internal static long LastAdjustedTicks
        {
            get { return Interlocked.Read(ref lastAdjustedTicks); }
        }

        internal static int LastMonthsDelta
        {
            get { return Volatile.Read(ref lastMonthsDelta); }
        }

        internal static double LastDaysDelta
        {
            get { return BitConverter.Int64BitsToDouble(Interlocked.Read(ref lastDaysDeltaBits)); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        /// <summary>
        /// Static replacement for DateTime.AddMonths at the audited Graduation_Date
        /// statements. The transpiler supplies the exact vanilla-computed input date and
        /// month delta, then stores this returned value back into Graduation_Date.
        /// </summary>
        internal static DateTime ApplyMonths(DateTime value, int months)
        {
            DateTime adjusted = value.AddMonths(months);
            Interlocked.Exchange(ref lastOriginalTicks, value.Ticks);
            Interlocked.Exchange(ref lastAdjustedTicks, adjusted.Ticks);
            Volatile.Write(ref lastMonthsDelta, months);
            Interlocked.Increment(ref addMonthsAppliedCount);
            lastDiagnostic =
                "Applied Graduation_Date.AddMonths(" + months + ") from ticks=" +
                value.Ticks + " to ticks=" + adjusted.Ticks + ".";
            return adjusted;
        }

        /// <summary>
        /// Static replacement for DateTime.AddDays at the audited Graduation_Date
        /// statements. The supplied delta remains entirely vanilla-owned, including the
        /// rounded dynamic value produced by Graduation_Date_Update().
        /// </summary>
        internal static DateTime ApplyDays(DateTime value, double days)
        {
            DateTime adjusted = value.AddDays(days);
            Interlocked.Exchange(ref lastOriginalTicks, value.Ticks);
            Interlocked.Exchange(ref lastAdjustedTicks, adjusted.Ticks);
            Interlocked.Exchange(ref lastDaysDeltaBits, BitConverter.DoubleToInt64Bits(days));
            Interlocked.Increment(ref addDaysAppliedCount);
            lastDiagnostic =
                "Applied Graduation_Date.AddDays(" + days + ") from ticks=" +
                value.Ticks + " to ticks=" + adjusted.Ticks + ".";
            return adjusted;
        }
    }

    internal static class GraduationDatePatchHealth
    {
        internal const int ExpectedTargetMethodCount = 5;
        internal const int ExpectedAdjustmentSiteCount = 6;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static int reportedTargetMethodCount;
        private static int observedAdjustmentSiteCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        reportedTargetMethodCount == ExpectedTargetMethodCount &&
                        observedAdjustmentSiteCount == ExpectedAdjustmentSiteCount &&
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

        internal static int ObservedAdjustmentSiteCount
        {
            get
            {
                lock (Sync)
                {
                    return observedAdjustmentSiteCount;
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
                    failure = "A25 resolved more target methods than the frozen five-method manifest.";
                }
            }
        }

        internal static void ReportSites(string methodId, int observed, int expected)
        {
            lock (Sync)
            {
                reportedTargetMethodCount++;
                observedAdjustmentSiteCount += observed;
                if (observed != expected)
                {
                    failure =
                        "A25 expected " + expected + " Graduation_Date immutable-DateTime site(s) in " +
                        methodId + " but observed " + observed + ".";
                }
                else if (reportedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A25 received more transpiler site reports than the frozen five-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A25 patch failure";
            }
        }
    }
}
