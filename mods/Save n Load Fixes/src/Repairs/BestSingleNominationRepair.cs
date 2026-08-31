using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A30: Awards.GenerateTempNominations() creates the authoritative
    /// best_single temporary award row with Single == null even though SetWins()
    /// later evaluates that stored field through Awards.IsWin_Single(award.Single).
    ///
    /// Binding is performed at the existing private AddTempNomination(...) creation
    /// boundary. Only the best_single/null argument pair is touched. The first and
    /// only SNLF nominee-selection call is vanilla Awards.GetNominatedSingle(); its
    /// returned object is then written into the row by vanilla AddTempNomination().
    /// No candidate is regenerated at SetWins(), and no persistence is introduced.
    /// </summary>
    internal static class BestSingleNominationRepair
    {
        private static long selectionAttemptCount;
        private static long boundCount;
        private static long nullSelectionCount;
        private static long preboundObservedCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return BestSingleNominationPatchHealth.IsHealthy; }
        }

        internal static long SelectionAttemptCount
        {
            get { return Interlocked.Read(ref selectionAttemptCount); }
        }

        internal static long BoundCount
        {
            get { return Interlocked.Read(ref boundCount); }
        }

        internal static long NullSelectionCount
        {
            get { return Interlocked.Read(ref nullSelectionCount); }
        }

        internal static long PreboundObservedCount
        {
            get { return Interlocked.Read(ref preboundObservedCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void BindBestSingleNominee(
            Awards._type type,
            ref singles._single single)
        {
            if (type != Awards._type.best_single)
            {
                return;
            }

            if (single != null)
            {
                Interlocked.Increment(ref preboundObservedCount);
                lastDiagnostic =
                    "A30 observed an already-bound best_single temporary nomination and left it unchanged.";
                return;
            }

            Interlocked.Increment(ref selectionAttemptCount);
            singles._single selected = Awards.GetNominatedSingle();
            if (selected == null)
            {
                Interlocked.Increment(ref nullSelectionCount);
                lastDiagnostic =
                    "A30 asked vanilla for the best_single nominee at creation time, but vanilla returned null.";
                return;
            }

            single = selected;
            Interlocked.Increment(ref boundCount);
            lastDiagnostic =
                "A30 bound the best_single temporary nomination to the one vanilla-selected single before row creation.";
        }
    }

    internal static class BestSingleNominationPatchHealth
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
                        "A30 resolved more AddTempNomination targets than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A30 patch failure";
            }
        }
    }
}
