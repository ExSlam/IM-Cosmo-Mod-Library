using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A32: Groups.GroupData declares the three target-audience fields and
    /// Groups._group.Set(GroupData) restores them, but the save-side
    /// GroupData.Set(_group) copier omits them. Preserve the live group's exact
    /// target-audience enums in the vanilla DTO after the ordinary copier runs.
    /// </summary>
    internal static class GroupTargetAudienceRepair
    {
        private static long appliedCount;
        private static long skippedCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return GroupTargetAudiencePatchHealth.IsHealthy; }
        }

        internal static long AppliedCount
        {
            get { return Interlocked.Read(ref appliedCount); }
        }

        internal static long SkippedCount
        {
            get { return Interlocked.Read(ref skippedCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void CompleteSaveSideCopy(
            Groups.GroupData data,
            Groups._group group)
        {
            if (data == null || group == null)
            {
                Interlocked.Increment(ref skippedCount);
                lastDiagnostic =
                    "A32 could not complete a group target-audience DTO copy because the DTO or source group was null.";
                return;
            }

            data.Appeal_Gender = group.Appeal_Gender;
            data.Appeal_Hardcoreness = group.Appeal_Hardcoreness;
            data.Appeal_Age = group.Appeal_Age;

            Interlocked.Increment(ref appliedCount);
            lastDiagnostic =
                "A32 copied the live group's three target-audience enums into the vanilla GroupData DTO.";
        }
    }

    internal static class GroupTargetAudiencePatchHealth
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
                        "A32 resolved more Groups.GroupData.Set(_group) targets than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A32 patch failure";
            }
        }
    }
}
