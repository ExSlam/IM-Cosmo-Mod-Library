using System;
using System.Collections.Generic;

namespace IMDataCore
{
    /// <summary>
    /// Versioned participant-schema contract for built-in shared history rows.
    /// Version zero means the migrated source did not prove the current shared
    /// participant contract. Version one is the current strict list/count/pair
    /// contract. Namespaced and non-shared rows use zero as not-applicable.
    /// </summary>
    internal static class LightweightParticipantSchema
    {
        internal const int LegacyUnknownSchemaVersion = 0;
        internal const int CurrentSchemaVersion = 1;

        // Repository history: the canonical shared-envelope participant model
        // first shipped while the outer sidecar was format v2. Format v1 predates
        // that storage contract. Therefore v2-v5 candidates stay strict; only a
        // v1 archival/synthetic shared candidate is eligible for bounded legacy
        // redundancy derivation.
        internal const int FirstReleasedSharedEnvelopeFormatVersion = 2;

        private sealed class RedundantCountPair
        {
            internal string ListProperty = string.Empty;
            internal string CountProperty = string.Empty;
        }

        private static readonly RedundantCountPair[] RedundantCountPairs =
        {
            Pair(CoreConstants.JsonFieldShowCastIdList, CoreConstants.JsonFieldShowCastCount),
            Pair(CoreConstants.JsonFieldShowCastIdListBefore, CoreConstants.JsonFieldShowCastCountBefore),
            Pair(CoreConstants.JsonFieldShowCastIdListAfter, CoreConstants.JsonFieldShowCastCountAfter),
            Pair(CoreConstants.JsonFieldSingleCastIdList, CoreConstants.JsonFieldSingleCastCount),
            Pair(CoreConstants.JsonFieldSingleCastIdListBefore, CoreConstants.JsonFieldSingleCastCountBefore),
            Pair(CoreConstants.JsonFieldSingleCastIdListAfter, CoreConstants.JsonFieldSingleCastCountAfter),
            Pair(CoreConstants.JsonFieldConcertParticipantIdList, CoreConstants.JsonFieldConcertParticipantCount),
            Pair(CoreConstants.JsonFieldConcertParticipantIdListBefore, CoreConstants.JsonFieldConcertParticipantCountBefore),
            Pair(CoreConstants.JsonFieldConcertParticipantIdListAfter, CoreConstants.JsonFieldConcertParticipantCountAfter),
            Pair(CoreConstants.JsonFieldTourParticipantIdList, CoreConstants.JsonFieldTourParticipantCount)
        };

        internal static void InitializeCurrentRecord(LightweightEventRecord record)
        {
            if (record == null)
            {
                return;
            }

            record.ParticipantSchemaVersion =
                SharedTimelineParticipants.IsSharedEventCandidate(record)
                    ? CurrentSchemaVersion
                    : LegacyUnknownSchemaVersion;
        }

        /// <summary>
        /// Frozen source-format mapping for released lightweight generations.
        /// The shared envelope and its participant metadata were already present
        /// in sidecar format v2. Consequently v2-v5 candidates stay on the strict
        /// current contract. Format v1 predates that shared-envelope contract and
        /// is the only bounded archival/synthetic compatibility generation. Field
        /// absence by itself never makes a v2-v5 row legacy.
        /// </summary>
        internal static void PrepareMigratedRecord(
            LightweightEventRecord record,
            int sourceFormatVersion)
        {
            if (record == null)
            {
                return;
            }
            if (!SharedTimelineParticipants.IsSharedEventCandidate(record))
            {
                record.ParticipantSchemaVersion = LegacyUnknownSchemaVersion;
                return;
            }

            if (sourceFormatVersion >=
                FirstReleasedSharedEnvelopeFormatVersion)
            {
                record.ParticipantSchemaVersion = CurrentSchemaVersion;
                return;
            }

            List<int> ignored;
            SharedTimelineParticipantResolution strictResolution =
                SharedTimelineParticipants.ResolveParticipantIdsCurrentSchema(
                    record,
                    out ignored);
            if (strictResolution == SharedTimelineParticipantResolution.Shared ||
                strictResolution == SharedTimelineParticipantResolution.ValidEmpty)
            {
                record.ParticipantSchemaVersion = CurrentSchemaVersion;
                return;
            }

            string originalPayload = record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            string originalStorage = record.StoragePayloadJson ?? string.Empty;
            string candidatePayload = originalPayload;
            string candidateStorage = originalStorage;
            bool changed = false;

            for (int index = 0; index < RedundantCountPairs.Length; index++)
            {
                string nextPayload;
                string nextStorage;
                bool pairChanged;
                if (!LightweightSidecarJson.TryDeriveIdentifierCountForMigration(
                        candidatePayload,
                        RedundantCountPairs[index].ListProperty,
                        RedundantCountPairs[index].CountProperty,
                        out nextPayload,
                        out nextStorage,
                        out pairChanged))
                {
                    continue;
                }
                if (pairChanged)
                {
                    candidatePayload = nextPayload;
                    candidateStorage = nextStorage;
                    changed = true;
                }
            }

            if (changed)
            {
                record.PayloadJson = candidatePayload;
                record.StoragePayloadJson = candidateStorage;
                strictResolution =
                    SharedTimelineParticipants.ResolveParticipantIdsCurrentSchema(
                        record,
                        out ignored);
                if (strictResolution == SharedTimelineParticipantResolution.Shared ||
                    strictResolution == SharedTimelineParticipantResolution.ValidEmpty)
                {
                    record.ParticipantSchemaVersion = CurrentSchemaVersion;
                    return;
                }

                // Do not partially rewrite an occurrence whose participant
                // identity is still not source-provable.
                record.PayloadJson = originalPayload;
                record.StoragePayloadJson = originalStorage;
            }

            record.ParticipantSchemaVersion = LegacyUnknownSchemaVersion;
        }

        internal static void ValidateV6Record(LightweightEventRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The v6 participant-schema validator received a null event.");
            }
            bool candidate = SharedTimelineParticipants.IsSharedEventCandidate(record);
            if (!candidate)
            {
                if (record.ParticipantSchemaVersion != LegacyUnknownSchemaVersion)
                {
                    throw new FormatException(
                        "A non-shared v6 event declares a shared participant schema.");
                }
                return;
            }
            if (record.ParticipantSchemaVersion != LegacyUnknownSchemaVersion &&
                record.ParticipantSchemaVersion != CurrentSchemaVersion)
            {
                throw new FormatException(
                    "A v6 shared event declares an unsupported participant schema version.");
            }
        }

        private static RedundantCountPair Pair(string listProperty, string countProperty)
        {
            return new RedundantCountPair
            {
                ListProperty = listProperty ?? string.Empty,
                CountProperty = countProperty ?? string.Empty
            };
        }
    }
}
