using System;
using System.Collections.Generic;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs numbered finding N01. Vanilla persists relationship pair/history data
    /// but omits _relationship.Dynamic, while weekly Do_Dynamic consumes it. Restore
    /// the exact value from the SNLF envelope after vanilla relationship reconstruction.
    /// </summary>
    internal static class RelationshipDynamicRepair
    {
        private static long restoredLoadCount;
        private static long restoredRelationshipCount;
        private static long legacyEnvelopeAbsentCount;
        private static long invalidEnvelopeCount;
        private static long associationMissingCount;
        private static long exactSetMismatchCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return RelationshipDynamicPatchHealth.IsHealthy; }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredRelationshipCount { get { return Interlocked.Read(ref restoredRelationshipCount); } }
        internal static long LegacyEnvelopeAbsentCount { get { return Interlocked.Read(ref legacyEnvelopeAbsentCount); } }
        internal static long InvalidEnvelopeCount { get { return Interlocked.Read(ref invalidEnvelopeCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long ExactSetMismatchCount { get { return Interlocked.Read(ref exactSetMismatchCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static void RestoreAfterVanillaLoad()
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N01 skipped because the adopted SavedData object has no repair-envelope read association.";
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacyEnvelopeAbsentCount);
                lastDiagnostic = "N01 loaded a pre-envelope save; exact historical Dynamic values are unavailable and vanilla state is left untouched.";
                return;
            }

            if (!state.Valid || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.relationship_dynamics == null)
            {
                Interlocked.Increment(ref invalidEnvelopeCount);
                lastDiagnostic = "N01 failed closed because the repair envelope is invalid for relationship dynamics.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            List<RelationshipDynamicRecordV1> records =
                state.Envelope.records.relationship_dynamics;
            Dictionary<string, Relationships._relationship> currentByPair =
                new Dictionary<string, Relationships._relationship>(StringComparer.Ordinal);
            HashSet<string> duplicateCurrentPairs =
                new HashSet<string>(StringComparer.Ordinal);

            List<Relationships._relationship> current = Relationships.RelationshipsData;
            if (current == null)
            {
                RecordMismatch("N01 current relationship registry is null after vanilla LoadFunction.");
                return;
            }

            for (int index = 0; index < current.Count; index++)
            {
                Relationships._relationship relationship = current[index];
                int low;
                int high;
                if (!TryGetPair(relationship, out low, out high))
                {
                    RecordMismatch("N01 found an invalid relationship pair after vanilla reconstruction.");
                    return;
                }

                string key = RepairEnvelopeBuilder.BuildPairKey(low, high);
                if (currentByPair.ContainsKey(key))
                {
                    duplicateCurrentPairs.Add(key);
                }
                else
                {
                    currentByPair.Add(key, relationship);
                }
            }

            if (duplicateCurrentPairs.Count != 0 || currentByPair.Count != records.Count)
            {
                RecordMismatch("N01 envelope/current relationship pair counts do not form an exact one-to-one set.");
                return;
            }

            HashSet<string> envelopePairs = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < records.Count; index++)
            {
                RelationshipDynamicRecordV1 record = records[index];
                int low;
                int high;
                if (record == null ||
                    !RepairEnvelopeBuilder.TryNormalizePair(
                        record.girl_id_low,
                        record.girl_id_high,
                        out low,
                        out high) ||
                    low != record.girl_id_low ||
                    high != record.girl_id_high ||
                    record.dynamic < (int)Relationships._relationship._dynamic.NONE ||
                    record.dynamic > (int)Relationships._relationship._dynamic.negative)
                {
                    RecordMismatch("N01 envelope contains an invalid normalized pair or Dynamic enum value.");
                    return;
                }

                string key = RepairEnvelopeBuilder.BuildPairKey(low, high);
                if (!envelopePairs.Add(key) || !currentByPair.ContainsKey(key))
                {
                    RecordMismatch("N01 envelope relationship pairs do not exactly match vanilla reconstruction.");
                    return;
                }
            }

            // Apply only after the entire pair set validates. There is no partial-exact mode.
            for (int index = 0; index < records.Count; index++)
            {
                RelationshipDynamicRecordV1 record = records[index];
                string key = RepairEnvelopeBuilder.BuildPairKey(
                    record.girl_id_low,
                    record.girl_id_high);
                currentByPair[key].Dynamic =
                    (Relationships._relationship._dynamic)record.dynamic;
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredRelationshipCount, records.Count);
            lastDiagnostic = "N01 restored exact relationship Dynamic values for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
        }

        private static bool TryGetPair(
            Relationships._relationship relationship,
            out int low,
            out int high)
        {
            low = 0;
            high = 0;
            if (relationship == null ||
                relationship.Girls == null ||
                relationship.Girls.Count != 2 ||
                relationship.Girls[0] == null ||
                relationship.Girls[1] == null)
            {
                return false;
            }

            return RepairEnvelopeBuilder.TryNormalizePair(
                relationship.Girls[0].id,
                relationship.Girls[1].id,
                out low,
                out high);
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }

                mainScript main = Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void RecordMismatch(string diagnostic)
        {
            Interlocked.Increment(ref exactSetMismatchCount);
            lastDiagnostic = diagnostic ?? "N01 exact relationship-set validation failed.";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }
    }

    internal static class RelationshipDynamicPatchHealth
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

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N01 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N01 patch failure";
            }
        }
    }
}
