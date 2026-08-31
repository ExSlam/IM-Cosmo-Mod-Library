using System.Collections.Generic;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs N14 / finding #31. Date_Popup's player influence action clears only
    /// the selected idol's DatingData. When the partner is another idol, vanilla's
    /// authoritative Relationships._relationship remains Dating and the partner
    /// remains taken. SNLF resolves only an already-existing, uniquely provable
    /// dating relationship and delegates the teardown to vanilla BreakUp().
    /// </summary>
    internal static class ForcedBreakupRepair
    {
        private static long idolRelationshipBreakupCount;
        private static long nonIdolPartnerPassThroughCount;
        private static long unresolvedIdolRelationshipCount;
        private static long ambiguousIdolRelationshipCount;
        private static long skippedCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return ForcedBreakupPatchHealth.IsHealthy; }
        }

        internal static long IdolRelationshipBreakupCount { get { return Interlocked.Read(ref idolRelationshipBreakupCount); } }
        internal static long NonIdolPartnerPassThroughCount { get { return Interlocked.Read(ref nonIdolPartnerPassThroughCount); } }
        internal static long UnresolvedIdolRelationshipCount { get { return Interlocked.Read(ref unresolvedIdolRelationshipCount); } }
        internal static long AmbiguousIdolRelationshipCount { get { return Interlocked.Read(ref ambiguousIdolRelationshipCount); } }
        internal static long SkippedCount { get { return Interlocked.Read(ref skippedCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        /// <summary>
        /// Runs before vanilla clears the selected idol's DatingData. This timing is
        /// required because GetGirlfriend()/the relationship graph are only coherent
        /// while the selected idol is still marked taken_idol.
        /// </summary>
        internal static void RepairIdolRelationshipBeforeVanillaClear(Date_Popup popup)
        {
            if (popup == null || popup.Girl == null || popup.Girl.DatingData == null)
            {
                Interlocked.Increment(ref skippedCount);
                lastDiagnostic = "N14 skipped because Date_Popup, Girl, or DatingData was null.";
                return;
            }

            data_girls.girls girl = popup.Girl;
            if (girl.DatingData.Partner_Status != data_girls.girls._dating_data._partner_status.taken_idol)
            {
                // Outside partners have no Relationships._relationship to tear down.
                // Vanilla's original SetDatingStatus(free) remains the correct path.
                Interlocked.Increment(ref nonIdolPartnerPassThroughCount);
                lastDiagnostic = "N14 left a non-idol partner breakup on vanilla's original DatingData path.";
                return;
            }

            List<Relationships._relationship> relationships = Relationships.GetAllRelationships(girl, false);
            Relationships._relationship candidate = null;
            int candidateCount = 0;

            if (relationships != null)
            {
                foreach (Relationships._relationship relationship in relationships)
                {
                    if (!IsProvableDatingRelationship(relationship, girl))
                    {
                        continue;
                    }

                    candidate = relationship;
                    candidateCount++;
                    if (candidateCount > 1)
                    {
                        break;
                    }
                }
            }

            if (candidateCount == 0 || candidate == null)
            {
                Interlocked.Increment(ref unresolvedIdolRelationshipCount);
                lastDiagnostic =
                    "N14 found taken_idol DatingData but no existing Dating relationship; no relationship was fabricated.";
                return;
            }

            if (candidateCount != 1)
            {
                Interlocked.Increment(ref ambiguousIdolRelationshipCount);
                lastDiagnostic =
                    "N14 found multiple existing Dating relationships for the selected idol and failed closed rather than guessing a partner.";
                return;
            }

            candidate.BreakUp();
            Interlocked.Increment(ref idolRelationshipBreakupCount);
            lastDiagnostic =
                "N14 delegated the player-forced idol-idol breakup to vanilla Relationships._relationship.BreakUp().";
        }

        private static bool IsProvableDatingRelationship(
            Relationships._relationship relationship,
            data_girls.girls girl)
        {
            if (relationship == null || !relationship.Dating || relationship.Girls == null ||
                relationship.Girls.Count != 2 || relationship.Girls[0] == null || relationship.Girls[1] == null)
            {
                return false;
            }

            if (relationship.Girls[0] != girl && relationship.Girls[1] != girl)
            {
                return false;
            }

            data_girls.girls partner = relationship.Girls[0] == girl
                ? relationship.Girls[1]
                : relationship.Girls[0];

            return partner != girl &&
                partner.DatingData != null &&
                partner.DatingData.Partner_Status == data_girls.girls._dating_data._partner_status.taken_idol;
        }
    }

    internal static class ForcedBreakupPatchHealth
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
                    failure = "N14 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N14 patch failure";
            }
        }
    }
}
