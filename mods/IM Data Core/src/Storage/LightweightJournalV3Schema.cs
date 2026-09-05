using System;

namespace IMDataCore
{
    /// <summary>
    /// Staged journal-v3 transaction framing for the sidecar-v6 generation.
    /// Normal runtime persistence remains journal v2 until the rest of Wave 0
    /// supplies the v6 coverage/provenance record codecs and publication path.
    /// </summary>
    internal static class LightweightJournalV3Schema
    {
        internal const int JournalFormatVersion = 3;

        internal const string KindBegin = "BEGIN";
        internal const string KindCheckpoint = "CHECKPOINT";
        internal const string KindEvent = "EVENT";
        internal const string KindCustomMutation = "CUSTOM_MUTATION";
        internal const string KindForwardExtension = "FORWARD_EXTENSION";
        internal const string KindCoverageCapabilitySet =
            "COVERAGE_CAPABILITY_SET";
        internal const string KindCoverageTransition = "COVERAGE_TRANSITION";
        internal const string KindNamespaceOwnerBinding =
            "NAMESPACE_OWNER_BINDING";
        internal const string KindHistoricalBaselineAssertion =
            "HISTORICAL_BASELINE_ASSERTION";
        internal const string KindCommit = "COMMIT";

        internal static void ValidateCounts(
            LightweightJournalV3Counts counts,
            string parameterName)
        {
            if (counts == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (counts.CheckpointCount < 0 ||
                counts.EventCount < 0 ||
                counts.CustomMutationCount < 0 ||
                counts.ForwardExtensionCount < 0 ||
                counts.CoverageCapabilitySetCount < 0 ||
                counts.CoverageTransitionCount < 0 ||
                counts.NamespaceOwnerBindingCount < 0 ||
                counts.HistoricalBaselineAssertionCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Journal-v3 collection counts cannot be negative.");
            }
        }

        internal static void ValidateMonotonicTarget(
            LightweightJournalV3Counts baseCounts,
            LightweightJournalV3Counts targetCounts)
        {
            ValidateCounts(baseCounts, "baseCounts");
            ValidateCounts(targetCounts, "targetCounts");
            if (targetCounts.CheckpointCount < baseCounts.CheckpointCount ||
                targetCounts.EventCount < baseCounts.EventCount ||
                targetCounts.CustomMutationCount <
                    baseCounts.CustomMutationCount ||
                targetCounts.ForwardExtensionCount <
                    baseCounts.ForwardExtensionCount ||
                targetCounts.CoverageCapabilitySetCount <
                    baseCounts.CoverageCapabilitySetCount ||
                targetCounts.CoverageTransitionCount <
                    baseCounts.CoverageTransitionCount ||
                targetCounts.NamespaceOwnerBindingCount <
                    baseCounts.NamespaceOwnerBindingCount ||
                targetCounts.HistoricalBaselineAssertionCount <
                    baseCounts.HistoricalBaselineAssertionCount)
            {
                throw new FormatException(
                    "A journal-v3 transaction target cannot shrink a durable collection.");
            }
        }

        /// <summary>
        /// Task 9 completes the last frozen journal-v3 semantic row family.
        /// The compatibility gate is retained for callers compiled against the
        /// staged API, but no known v3 collection delta now requires fallback.
        /// </summary>
        internal static bool RequiresFullSnapshotForDeferredExtensionDelta(
            LightweightJournalV3Counts baseCounts,
            LightweightJournalV3Counts targetCounts)
        {
            ValidateMonotonicTarget(baseCounts, targetCounts);
            return false;
        }
    }

    [Serializable]
    internal sealed class LightweightJournalV3Counts
    {
        public int CheckpointCount;
        public int EventCount;
        public int CustomMutationCount;
        public int ForwardExtensionCount;
        public int CoverageCapabilitySetCount;
        public int CoverageTransitionCount;
        public int NamespaceOwnerBindingCount;
        public int HistoricalBaselineAssertionCount;

        internal LightweightJournalV3Counts Clone()
        {
            return new LightweightJournalV3Counts
            {
                CheckpointCount = CheckpointCount,
                EventCount = EventCount,
                CustomMutationCount = CustomMutationCount,
                ForwardExtensionCount = ForwardExtensionCount,
                CoverageCapabilitySetCount = CoverageCapabilitySetCount,
                CoverageTransitionCount = CoverageTransitionCount,
                NamespaceOwnerBindingCount = NamespaceOwnerBindingCount,
                HistoricalBaselineAssertionCount =
                    HistoricalBaselineAssertionCount
            };
        }
    }
}
