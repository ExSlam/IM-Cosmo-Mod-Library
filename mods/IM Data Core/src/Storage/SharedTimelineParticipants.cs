using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    internal enum SharedTimelineParticipantResolution
    {
        NotCandidate,
        Shared,
        ValidEmpty,
        Malformed
    }

    /// <summary>
    /// First-class participant model for built-in shared timeline events.
    ///
    /// A shared event is persisted once with IdolId=-1. Its compact historical
    /// participant data is used only to rebuild the derived in-memory idol
    /// indexes. Public reads may reconstruct tiny idol-specific fields from the
    /// shared payload without mutating the durable row.
    /// </summary>
    internal static class SharedTimelineParticipants
    {
        internal static bool TryGetParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            return ResolveParticipantIds(record, out participantIds) ==
                SharedTimelineParticipantResolution.Shared;
        }

        internal static SharedTimelineParticipantResolution ResolveParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            if (!IsSharedEventCandidate(record))
            {
                return SharedTimelineParticipantResolution.NotCandidate;
            }

            if (!TryReadParticipantMetadata(record, out participantIds))
            {
                return SharedTimelineParticipantResolution.Malformed;
            }

            return participantIds.Count > 0
                ? SharedTimelineParticipantResolution.Shared
                : SharedTimelineParticipantResolution.ValidEmpty;
        }

        private static bool TryReadParticipantMetadata(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            if (record == null ||
                !string.IsNullOrEmpty(record.NamespaceIdentifier))
            {
                return false;
            }

            string entityKind = record.EntityKind ?? string.Empty;
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindShow,
                    StringComparison.Ordinal) &&
                IsSharedShowEventType(record.EventType))
            {
                return TryGetShowParticipantIds(record, out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindSingle,
                    StringComparison.Ordinal) &&
                IsSharedSingleEventType(record.EventType))
            {
                return TryGetSingleParticipantIds(record, out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindConcert,
                    StringComparison.Ordinal) &&
                IsSharedConcertEventType(record.EventType))
            {
                return TryGetConcertParticipantIds(record, out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindTour,
                    StringComparison.Ordinal) &&
                IsSharedTourEventType(record.EventType))
            {
                return TryReadTourParticipantIds(
                    record.PayloadJson,
                    out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindElection,
                    StringComparison.Ordinal) &&
                IsSharedElectionEventType(record.EventType))
            {
                return TryGetElectionParticipantIds(record, out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindRoomWork,
                    StringComparison.Ordinal) &&
                (string.Equals(
                     record.EventType ?? string.Empty,
                     CoreConstants.EventTypeRoomWorkCompleted,
                     StringComparison.Ordinal) ||
                 string.Equals(
                     record.EventType ?? string.Empty,
                     CoreConstants.EventTypeRoomWorkCancelled,
                     StringComparison.Ordinal)))
            {
                return TryReadCsv(
                    record.PayloadJson,
                    CoreConstants.JsonFieldRoomWorkParticipantIdList,
                    out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindRelationship,
                    StringComparison.Ordinal) &&
                IsSharedRelationshipEventType(record.EventType))
            {
                return TryReadPair(
                    record.PayloadJson,
                    CoreConstants.JsonFieldRelationshipIdolAId,
                    CoreConstants.JsonFieldRelationshipIdolBId,
                    out participantIds);
            }
            if (string.Equals(
                    entityKind,
                    CoreConstants.EventEntityKindMentorship,
                    StringComparison.Ordinal) &&
                IsSharedMentorshipEventType(record.EventType))
            {
                return TryReadPair(
                    record.PayloadJson,
                    CoreConstants.JsonFieldMentorId,
                    CoreConstants.JsonFieldKohaiId,
                    out participantIds);
            }
            if (IsSharedNarrativeEvent(record))
            {
                string participantPropertyName = string.Equals(
                        entityKind,
                        CoreConstants.EventEntityKindRandomEvent,
                        StringComparison.Ordinal)
                    ? CoreConstants.JsonFieldRandomEventActorIdList
                    : CoreConstants.JsonFieldSubstoryActorIdList;
                return TryReadCsv(
                    record.PayloadJson,
                    participantPropertyName,
                    out participantIds);
            }

            return false;
        }

        internal static bool IsSharedEvent(LightweightEventRecord record)
        {
            List<int> ignored;
            return ResolveParticipantIds(record, out ignored) ==
                SharedTimelineParticipantResolution.Shared;
        }

        /// <summary>
        /// Returns true for an envelope whose built-in type requires participant
        /// metadata, even when that metadata is malformed. Storage uses this to
        /// quarantine bad shared rows instead of leaking them into every diary as
        /// global events.
        /// </summary>
        internal static bool IsSharedEventCandidate(LightweightEventRecord record)
        {
            if (record == null ||
                record.IdolId != CoreConstants.InvalidIdValue ||
                !string.IsNullOrEmpty(record.NamespaceIdentifier))
            {
                return false;
            }

            string entityKind = record.EntityKind ?? string.Empty;
            string eventType = record.EventType ?? string.Empty;
            return (string.Equals(entityKind, CoreConstants.EventEntityKindShow, StringComparison.Ordinal) && IsSharedShowEventType(eventType)) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindSingle, StringComparison.Ordinal) && IsSharedSingleEventType(eventType)) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindConcert, StringComparison.Ordinal) && IsSharedConcertEventType(eventType)) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindTour, StringComparison.Ordinal) && IsSharedTourEventType(eventType)) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindElection, StringComparison.Ordinal) && IsSharedElectionEventType(eventType)) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindRoomWork, StringComparison.Ordinal) &&
                 (string.Equals(eventType, CoreConstants.EventTypeRoomWorkCompleted, StringComparison.Ordinal) ||
                  string.Equals(eventType, CoreConstants.EventTypeRoomWorkCancelled, StringComparison.Ordinal))) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindRelationship, StringComparison.Ordinal) && IsSharedRelationshipEventType(eventType)) ||
                (string.Equals(entityKind, CoreConstants.EventEntityKindMentorship, StringComparison.Ordinal) && IsSharedMentorshipEventType(eventType)) ||
                IsSharedNarrativeEvent(record);
        }

        internal static string ExpandPayloadForPublic(
            LightweightEventRecord record,
            int requestedIdolId)
        {
            if (record == null ||
                requestedIdolId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return record == null
                    ? CoreConstants.EmptyJsonObject
                    : record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            }

            string eventType = record.EventType ?? string.Empty;
            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeSingleReleased,
                    StringComparison.Ordinal))
            {
                return ExpandSingleRelease(record, requestedIdolId);
            }

            if (string.Equals(
                    record.EntityKind ?? string.Empty,
                    CoreConstants.EventEntityKindConcert,
                    StringComparison.Ordinal) ||
                string.Equals(
                    record.EntityKind ?? string.Empty,
                    CoreConstants.EventEntityKindTour,
                    StringComparison.Ordinal) ||
                string.Equals(
                    record.EntityKind ?? string.Empty,
                    CoreConstants.EventEntityKindRoomWork,
                    StringComparison.Ordinal))
            {
                return ExpandIdolIdentifier(record.PayloadJson, requestedIdolId);
            }

            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeElectionFinished,
                    StringComparison.Ordinal))
            {
                return ExpandElectionFinalResult(record, requestedIdolId);
            }

            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeElectionResultsGenerated,
                    StringComparison.Ordinal))
            {
                return ExpandElectionGeneratedResult(record, requestedIdolId);
            }

            return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
        }

        private static bool TryGetShowParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            string eventType = record.EventType ?? string.Empty;
            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeShowCastChanged,
                    StringComparison.Ordinal))
            {
                return TryReadCastMutationParticipantIds(
                    record.PayloadJson,
                    CoreConstants.JsonFieldShowCastIdListBefore,
                    CoreConstants.JsonFieldShowCastCountBefore,
                    CoreConstants.JsonFieldShowCastIdListAfter,
                    CoreConstants.JsonFieldShowCastCountAfter,
                    CoreConstants.JsonFieldShowCastIdListAdded,
                    CoreConstants.JsonFieldShowCastIdListRemoved,
                    CoreConstants.JsonFieldShowRemovedIdolId,
                    out participantIds);
            }

            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeShowConfigurationChanged,
                    StringComparison.Ordinal))
            {
                return TryReadCsvUnion(
                    record.PayloadJson,
                    new string[]
                    {
                        CoreConstants.JsonFieldShowCastIdListBefore,
                        CoreConstants.JsonFieldShowCastIdListAfter
                    },
                    new string[]
                    {
                        CoreConstants.JsonFieldShowCastCountBefore,
                        CoreConstants.JsonFieldShowCastCountAfter
                    },
                    out participantIds);
            }

            return TryReadCsvWithCount(
                record.PayloadJson,
                CoreConstants.JsonFieldShowCastIdList,
                CoreConstants.JsonFieldShowCastCount,
                out participantIds);
        }

        private static bool TryGetSingleParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            string eventType = record.EventType ?? string.Empty;
            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeSingleReleased,
                    StringComparison.Ordinal))
            {
                List<int> castSlots;
                if (!TryReadSingleCastSlotIds(
                        record.PayloadJson,
                        out castSlots))
                {
                    participantIds = new List<int>();
                    return false;
                }

                participantIds = new List<int>();
                for (int slotIndex = CoreConstants.ZeroBasedListStartIndex;
                    slotIndex < castSlots.Count;
                    slotIndex++)
                {
                    if (castSlots[slotIndex] >=
                        CoreConstants.MinimumValidIdolIdentifier)
                    {
                        participantIds.Add(castSlots[slotIndex]);
                    }
                }
                return true;
            }

            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeSingleGroupChanged,
                    StringComparison.Ordinal))
            {
                return TryReadCsvUnion(
                    record.PayloadJson,
                    new string[]
                    {
                        CoreConstants.JsonFieldSingleCastIdListBefore,
                        CoreConstants.JsonFieldSingleCastIdListAfter
                    },
                    new string[]
                    {
                        CoreConstants.JsonFieldSingleCastCountBefore,
                        CoreConstants.JsonFieldSingleCastCountAfter
                    },
                    out participantIds);
            }

            if (!string.Equals(
                    eventType,
                    CoreConstants.EventTypeSingleCastChanged,
                    StringComparison.Ordinal))
            {
                return TryReadCsvWithCount(
                    record.PayloadJson,
                    CoreConstants.JsonFieldSingleCastIdList,
                    CoreConstants.JsonFieldSingleCastCount,
                    out participantIds);
            }

            return TryReadCastMutationParticipantIds(
                record.PayloadJson,
                CoreConstants.JsonFieldSingleCastIdListBefore,
                CoreConstants.JsonFieldSingleCastCountBefore,
                CoreConstants.JsonFieldSingleCastIdListAfter,
                CoreConstants.JsonFieldSingleCastCountAfter,
                CoreConstants.JsonFieldSingleCastIdListAdded,
                CoreConstants.JsonFieldSingleCastIdListRemoved,
                CoreConstants.JsonFieldSingleRemovedIdolId,
                out participantIds);
        }

        private static bool TryGetConcertParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            string eventType = record.EventType ?? string.Empty;
            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeConcertCastChanged,
                    StringComparison.Ordinal))
            {
                return TryReadCastMutationParticipantIds(
                    record.PayloadJson,
                    CoreConstants.JsonFieldConcertParticipantIdListBefore,
                    CoreConstants.JsonFieldConcertParticipantCountBefore,
                    CoreConstants.JsonFieldConcertParticipantIdListAfter,
                    CoreConstants.JsonFieldConcertParticipantCountAfter,
                    CoreConstants.JsonFieldConcertParticipantIdListAdded,
                    CoreConstants.JsonFieldConcertParticipantIdListRemoved,
                    CoreConstants.JsonFieldConcertRemovedIdolId,
                    out participantIds);
            }

            if (string.Equals(
                    eventType,
                    CoreConstants.EventTypeConcertConfigurationChanged,
                    StringComparison.Ordinal))
            {
                return TryReadCsvUnion(
                    record.PayloadJson,
                    new string[]
                    {
                        CoreConstants.JsonFieldConcertParticipantIdListBefore,
                        CoreConstants.JsonFieldConcertParticipantIdListAfter
                    },
                    new string[]
                    {
                        CoreConstants.JsonFieldConcertParticipantCountBefore,
                        CoreConstants.JsonFieldConcertParticipantCountAfter
                    },
                    out participantIds);
            }

            return TryReadCsvWithCount(
                record.PayloadJson,
                CoreConstants.JsonFieldConcertParticipantIdList,
                CoreConstants.JsonFieldConcertParticipantCount,
                out participantIds);
        }

        private static bool TryGetElectionParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            int resultCount;
            string rankingSummary;
            if (!LightweightSidecarJson.TryReadIntProperty(
                    record.PayloadJson,
                    CoreConstants.JsonFieldElectionResultCount,
                    out resultCount) ||
                resultCount < CoreConstants.ZeroBasedListStartIndex ||
                !LightweightSidecarJson.TryReadStringProperty(
                    record.PayloadJson,
                    CoreConstants.JsonFieldElectionRankingSummary,
                    out rankingSummary))
            {
                participantIds = new List<int>();
                return false;
            }

            List<int> rankedParticipantIds;
            if (!TryReadElectionRankingParticipantIds(
                    rankingSummary,
                    resultCount,
                    out rankedParticipantIds))
            {
                participantIds = new List<int>();
                return false;
            }

            if (string.Equals(
                    record.EventType ?? string.Empty,
                    CoreConstants.EventTypeElectionResultsGenerated,
                    StringComparison.Ordinal))
            {
                string generatedResultSummary;
                if (!LightweightSidecarJson.TryReadStringProperty(
                        record.PayloadJson,
                        CoreConstants.JsonFieldElectionGeneratedResultSummary,
                        out generatedResultSummary))
                {
                    participantIds = new List<int>();
                    return false;
                }

                if (!TryReadGeneratedElectionParticipantIds(
                    generatedResultSummary,
                    out participantIds))
                {
                    return false;
                }

                HashSet<int> eligibleIds = new HashSet<int>(participantIds);
                for (int rankedIndex = CoreConstants.ZeroBasedListStartIndex;
                    rankedIndex < rankedParticipantIds.Count;
                    rankedIndex++)
                {
                    if (!eligibleIds.Contains(rankedParticipantIds[rankedIndex]))
                    {
                        participantIds.Clear();
                        return false;
                    }
                }

                return true;
            }

            participantIds = rankedParticipantIds;
            return true;
        }

        private static string ExpandSingleRelease(
            LightweightEventRecord record,
            int requestedIdolId)
        {
            List<int> castSlotIds;
            if (!TryReadStrictSlotCsv(
                    record.PayloadJson,
                    CoreConstants.JsonFieldSingleCastIdList,
                    out castSlotIds))
            {
                return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            }

            int positionIndex = castSlotIds.IndexOf(requestedIdolId);
            if (positionIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            }

            return LightweightSidecarJson.ExpandSingleReleasePayloadForPublic(
                record.PayloadJson,
                requestedIdolId,
                positionIndex,
                ResolveSingleSenbatsuRowIndex(positionIndex),
                positionIndex == CoreConstants.SenbatsuCenterPositionIndex);
        }

        private static string ExpandElectionFinalResult(
            LightweightEventRecord record,
            int requestedIdolId)
        {
            string rankingSummary;
            if (!LightweightSidecarJson.TryReadStringProperty(
                    record.PayloadJson,
                    CoreConstants.JsonFieldElectionRankingSummary,
                    out rankingSummary))
            {
                return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            }

            string[] entries = rankingSummary.Split('|');
            for (int index = 0; index < entries.Length; index++)
            {
                string[] fields = entries[index].Split(':');
                int place;
                int idolId;
                long votes;
                int famePoints;
                if (fields.Length == 4 &&
                    int.TryParse(fields[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out place) &&
                    int.TryParse(fields[1], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out idolId) &&
                    long.TryParse(fields[2], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out votes) &&
                    int.TryParse(fields[3], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out famePoints) &&
                    idolId == requestedIdolId)
                {
                    Dictionary<string, long> projection =
                        new Dictionary<string, long>(StringComparer.Ordinal)
                        {
                            { CoreConstants.JsonFieldIdolId, idolId },
                            { CoreConstants.JsonFieldElectionPlace, place },
                            { CoreConstants.JsonFieldElectionVotes, votes },
                            { CoreConstants.JsonFieldElectionFamePoints, famePoints }
                        };
                    return LightweightSidecarJson.ExpandIntegerPayloadForPublic(
                        record.PayloadJson,
                        projection);
                }
            }

            return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
        }

        private static string ExpandElectionGeneratedResult(
            LightweightEventRecord record,
            int requestedIdolId)
        {
            string generatedSummary;
            if (!LightweightSidecarJson.TryReadStringProperty(
                    record.PayloadJson,
                    CoreConstants.JsonFieldElectionGeneratedResultSummary,
                    out generatedSummary))
            {
                return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            }

            string[] entries = generatedSummary.Split('|');
            for (int index = 0; index < entries.Length; index++)
            {
                string[] fields = entries[index].Split(':');
                int idolId;
                int expectedPlace;
                if (fields.Length == 2 &&
                    int.TryParse(fields[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out idolId) &&
                    int.TryParse(fields[1], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out expectedPlace) &&
                    idolId == requestedIdolId)
                {
                    int generatedPlace = CoreConstants.InvalidIdValue;
                    long votes = CoreConstants.ZeroLongValue;
                    int famePoints = CoreConstants.ZeroBasedListStartIndex;
                    TryResolveElectionRankingTuple(
                        record.PayloadJson,
                        requestedIdolId,
                        out generatedPlace,
                        out votes,
                        out famePoints);

                    Dictionary<string, long> projection =
                        new Dictionary<string, long>(StringComparer.Ordinal)
                        {
                            { CoreConstants.JsonFieldIdolId, idolId },
                            { CoreConstants.JsonFieldElectionExpectedPlace, expectedPlace },
                            { CoreConstants.JsonFieldElectionGeneratedPlace, generatedPlace },
                            { CoreConstants.JsonFieldElectionGeneratedVotes, votes },
                            { CoreConstants.JsonFieldElectionGeneratedFamePoints, famePoints }
                        };
                    return LightweightSidecarJson.ExpandIntegerPayloadForPublic(
                        record.PayloadJson,
                        projection);
                }
            }

            return record.PayloadJson ?? CoreConstants.EmptyJsonObject;
        }

        private static bool TryResolveElectionRankingTuple(
            string payloadJson,
            int requestedIdolId,
            out int place,
            out long votes,
            out int famePoints)
        {
            place = CoreConstants.InvalidIdValue;
            votes = CoreConstants.ZeroLongValue;
            famePoints = CoreConstants.ZeroBasedListStartIndex;
            string rankingSummary;
            if (!LightweightSidecarJson.TryReadStringProperty(
                    payloadJson,
                    CoreConstants.JsonFieldElectionRankingSummary,
                    out rankingSummary))
            {
                return false;
            }

            string[] entries = rankingSummary.Split('|');
            for (int index = 0; index < entries.Length; index++)
            {
                string[] fields = entries[index].Split(':');
                int candidatePlace;
                int candidateIdolId;
                long candidateVotes;
                int candidateFamePoints;
                if (fields.Length == 4 &&
                    int.TryParse(fields[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out candidatePlace) &&
                    int.TryParse(fields[1], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out candidateIdolId) &&
                    long.TryParse(fields[2], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out candidateVotes) &&
                    int.TryParse(fields[3], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out candidateFamePoints) &&
                    candidateIdolId == requestedIdolId)
                {
                    place = candidatePlace;
                    votes = candidateVotes;
                    famePoints = candidateFamePoints;
                    return true;
                }
            }

            return false;
        }

        private static string ExpandIdolIdentifier(
            string payloadJson,
            int requestedIdolId)
        {
            Dictionary<string, long> projection =
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    { CoreConstants.JsonFieldIdolId, requestedIdolId }
                };
            return LightweightSidecarJson.ExpandIntegerPayloadForPublic(
                payloadJson,
                projection);
        }

        private static bool TryReadCsv(
            string payloadJson,
            string propertyName,
            out List<int> participantIds)
        {
            string csv;
            if (!LightweightSidecarJson.TryReadStringProperty(
                    payloadJson,
                    propertyName,
                    out csv))
            {
                participantIds = new List<int>();
                return false;
            }

            return TryParseStrictIdentifierCsv(csv, false, out participantIds);
        }

        internal static bool TryReadTourParticipantIds(
            string payloadJson,
            out List<int> participantIds)
        {
            return TryReadCsvWithCount(
                payloadJson,
                CoreConstants.JsonFieldTourParticipantIdList,
                CoreConstants.JsonFieldTourParticipantCount,
                out participantIds);
        }

        internal static bool TryReadSingleCastSlotIds(
            string payloadJson,
            out List<int> slotIds)
        {
            return TryReadStrictSlotCsv(
                payloadJson,
                CoreConstants.JsonFieldSingleCastIdList,
                out slotIds);
        }

        private static bool TryReadCsvWithCount(
            string payloadJson,
            string propertyName,
            string countPropertyName,
            out List<int> participantIds)
        {
            int expectedCount;
            if (!LightweightSidecarJson.TryReadIntProperty(
                    payloadJson,
                    countPropertyName,
                    out expectedCount) ||
                expectedCount < CoreConstants.ZeroBasedListStartIndex ||
                !TryReadCsv(payloadJson, propertyName, out participantIds))
            {
                participantIds = new List<int>();
                return false;
            }

            return participantIds.Count == expectedCount;
        }

        private static bool TryReadCastMutationParticipantIds(
            string payloadJson,
            string beforeListPropertyName,
            string beforeCountPropertyName,
            string afterListPropertyName,
            string afterCountPropertyName,
            string addedListPropertyName,
            string removedListPropertyName,
            string removedIdPropertyName,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            List<int> beforeIds;
            List<int> afterIds;
            List<int> addedIds;
            List<int> removedIds;
            int removedId;
            if (!TryReadCsvWithCount(
                    payloadJson,
                    beforeListPropertyName,
                    beforeCountPropertyName,
                    out beforeIds) ||
                !TryReadCsvWithCount(
                    payloadJson,
                    afterListPropertyName,
                    afterCountPropertyName,
                    out afterIds) ||
                !TryReadCsv(
                    payloadJson,
                    addedListPropertyName,
                    out addedIds) ||
                !TryReadCsv(
                    payloadJson,
                    removedListPropertyName,
                    out removedIds) ||
                !LightweightSidecarJson.TryReadIntProperty(
                    payloadJson,
                    removedIdPropertyName,
                    out removedId) ||
                (removedId < CoreConstants.MinimumValidIdolIdentifier &&
                 removedId != CoreConstants.InvalidIdValue))
            {
                return false;
            }

            HashSet<int> beforeSet = new HashSet<int>(beforeIds);
            HashSet<int> afterSet = new HashSet<int>(afterIds);
            HashSet<int> expectedAdded = new HashSet<int>(afterSet);
            expectedAdded.ExceptWith(beforeSet);
            HashSet<int> expectedRemoved = new HashSet<int>(beforeSet);
            expectedRemoved.ExceptWith(afterSet);
            if (!expectedAdded.SetEquals(addedIds) ||
                !expectedRemoved.SetEquals(removedIds) ||
                (removedId >= CoreConstants.MinimumValidIdolIdentifier &&
                 !expectedRemoved.Contains(removedId)))
            {
                return false;
            }

            HashSet<int> emitted = new HashSet<int>();
            AppendDistinct(beforeIds, emitted, participantIds);
            AppendDistinct(afterIds, emitted, participantIds);
            return true;
        }

        private static bool TryReadCsvUnion(
            string payloadJson,
            IReadOnlyList<string> propertyNames,
            IReadOnlyList<string> countPropertyNames,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            HashSet<int> emitted = new HashSet<int>();
            if (propertyNames == null ||
                countPropertyNames == null ||
                propertyNames.Count != countPropertyNames.Count)
            {
                return false;
            }
            if (propertyNames != null)
            {
                for (int propertyIndex = 0;
                    propertyIndex < propertyNames.Count;
                    propertyIndex++)
                {
                    string csv;
                    if (!LightweightSidecarJson.TryReadStringProperty(
                            payloadJson,
                            propertyNames[propertyIndex],
                            out csv))
                    {
                        participantIds.Clear();
                        return false;
                    }
                    List<int> values;
                    if (!TryParseStrictIdentifierCsv(csv, false, out values))
                    {
                        participantIds.Clear();
                        return false;
                    }

                    string countPropertyName = countPropertyNames[propertyIndex];
                    if (!string.IsNullOrEmpty(countPropertyName))
                    {
                        int expectedCount;
                        if (!LightweightSidecarJson.TryReadIntProperty(
                                payloadJson,
                                countPropertyName,
                                out expectedCount) ||
                            expectedCount < CoreConstants.ZeroBasedListStartIndex ||
                            values.Count != expectedCount)
                        {
                            participantIds.Clear();
                            return false;
                        }
                    }
                    AppendDistinct(values, emitted, participantIds);
                }
            }

            return true;
        }

        private static bool TryReadStrictSlotCsv(
            string payloadJson,
            string propertyName,
            out List<int> slotIds)
        {
            string csv;
            if (!LightweightSidecarJson.TryReadStringProperty(
                    payloadJson,
                    propertyName,
                    out csv))
            {
                slotIds = new List<int>();
                return false;
            }

            return TryParseStrictIdentifierCsv(csv, true, out slotIds);
        }

        private static bool TryParseStrictIdentifierCsv(
            string csv,
            bool preserveEmptySlots,
            out List<int> values)
        {
            values = new List<int>();
            if (string.IsNullOrWhiteSpace(csv))
            {
                return true;
            }

            HashSet<int> emitted = new HashSet<int>();
            string[] tokens = csv.Split(',');
            for (int tokenIndex = CoreConstants.ZeroBasedListStartIndex;
                tokenIndex < tokens.Length;
                tokenIndex++)
            {
                int parsed;
                if (string.IsNullOrWhiteSpace(tokens[tokenIndex]) ||
                    !int.TryParse(
                        tokens[tokenIndex].Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsed) ||
                    (parsed < CoreConstants.MinimumValidIdolIdentifier &&
                     (!preserveEmptySlots ||
                      parsed != CoreConstants.InvalidIdValue)) ||
                    (parsed >= CoreConstants.MinimumValidIdolIdentifier &&
                     !emitted.Add(parsed)))
                {
                    values.Clear();
                    return false;
                }

                values.Add(parsed);
            }

            return true;
        }

        private static bool TryReadPair(
            string payloadJson,
            string firstPropertyName,
            string secondPropertyName,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            int first;
            int second;
            if (!LightweightSidecarJson.TryReadIntProperty(
                    payloadJson,
                    firstPropertyName,
                    out first) ||
                !LightweightSidecarJson.TryReadIntProperty(
                    payloadJson,
                    secondPropertyName,
                    out second) ||
                first < CoreConstants.MinimumValidIdolIdentifier ||
                second < CoreConstants.MinimumValidIdolIdentifier ||
                first == second)
            {
                return false;
            }

            participantIds.Add(first);
            participantIds.Add(second);
            return true;
        }

        private static bool IsSharedShowEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeShowCreated, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowReleased, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowCancelled, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowStatusChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowEpisodeReleased, StringComparison.Ordinal) ||
                string.Equals(normalized, "show_episode", StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowCastChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowConfigurationChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowRelaunchStarted, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeShowRelaunchFinished, StringComparison.Ordinal);
        }

        private static bool IsSharedSingleEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeSingleCreated, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeSingleReleased, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeSingleCancelled, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeSingleStatusChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeSingleCastChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeSingleGroupChanged, StringComparison.Ordinal);
        }

        private static bool IsSharedConcertEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeConcertCreated, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeConcertStarted, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeConcertFinished, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeConcertCancelled, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeConcertCastChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeConcertConfigurationChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeConcertStatusChanged, StringComparison.Ordinal);
        }

        private static bool IsSharedTourEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeTourStarted, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeTourFinished, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeTourCancelled, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeTourCountryResult, StringComparison.Ordinal);
        }

        private static bool IsSharedElectionEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeElectionResultsGenerated, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeElectionFinished, StringComparison.Ordinal);
        }

        private static bool IsSharedRelationshipEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeIdolDatingStarted, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeIdolDatingEnded, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeIdolRelationshipStatusChanged, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeIdolRelationshipRemoved, StringComparison.Ordinal);
        }

        private static bool IsSharedMentorshipEventType(string eventType)
        {
            string normalized = eventType ?? string.Empty;
            return string.Equals(normalized, CoreConstants.EventTypeMentorshipStarted, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeMentorshipEnded, StringComparison.Ordinal) ||
                string.Equals(normalized, CoreConstants.EventTypeMentorshipWeeklyTick, StringComparison.Ordinal);
        }

        private static bool IsSharedNarrativeEvent(LightweightEventRecord record)
        {
            string entityKind = record.EntityKind ?? string.Empty;
            string eventType = record.EventType ?? string.Empty;
            if (string.Equals(entityKind, CoreConstants.EventEntityKindRandomEvent, StringComparison.Ordinal))
            {
                return string.Equals(eventType, CoreConstants.EventTypeRandomEventStarted, StringComparison.Ordinal) ||
                    string.Equals(eventType, CoreConstants.EventTypeRandomEventConcluded, StringComparison.Ordinal);
            }

            return string.Equals(entityKind, CoreConstants.EventEntityKindSubstory, StringComparison.Ordinal) &&
                (string.Equals(eventType, CoreConstants.EventTypeSubstoryStarted, StringComparison.Ordinal) ||
                 string.Equals(eventType, CoreConstants.EventTypeSubstoryDelayed, StringComparison.Ordinal) ||
                 string.Equals(eventType, CoreConstants.EventTypeSubstoryCompleted, StringComparison.Ordinal));
        }

        private static bool TryReadGeneratedElectionParticipantIds(
            string summary,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            if (string.IsNullOrWhiteSpace(summary))
            {
                return true;
            }

            HashSet<int> emitted = new HashSet<int>();
            string[] entries = summary.Split('|');
            for (int index = 0; index < entries.Length; index++)
            {
                string[] fields = entries[index].Split(':');
                int idolId;
                int expectedPlace;
                if (fields.Length != 2 ||
                    !int.TryParse(
                        fields[0],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out idolId) ||
                    !int.TryParse(
                        fields[1],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out expectedPlace) ||
                    idolId < CoreConstants.MinimumValidIdolIdentifier ||
                    !emitted.Add(idolId))
                {
                    participantIds.Clear();
                    return false;
                }

                participantIds.Add(idolId);
            }

            return true;
        }

        private static bool TryReadElectionRankingParticipantIds(
            string summary,
            int expectedCount,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            if (expectedCount == CoreConstants.ZeroBasedListStartIndex)
            {
                return string.IsNullOrWhiteSpace(summary);
            }
            if (expectedCount < CoreConstants.ZeroBasedListStartIndex ||
                string.IsNullOrWhiteSpace(summary))
            {
                return false;
            }

            HashSet<int> emitted = new HashSet<int>();
            HashSet<int> emittedPlaces = new HashSet<int>();
            string[] entries = summary.Split('|');
            if (entries.Length != expectedCount)
            {
                return false;
            }
            for (int index = 0; index < entries.Length; index++)
            {
                string[] fields = entries[index].Split(':');
                int place;
                int idolId;
                long votes;
                int famePoints;
                if (fields.Length != 4 ||
                    !int.TryParse(
                        fields[0],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out place) ||
                    !int.TryParse(
                        fields[1],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out idolId) ||
                    !long.TryParse(
                        fields[2],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out votes) ||
                    !int.TryParse(
                        fields[3],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out famePoints) ||
                    place < 1 ||
                    idolId < CoreConstants.MinimumValidIdolIdentifier ||
                    votes < CoreConstants.ZeroLongValue ||
                    famePoints < CoreConstants.ZeroBasedListStartIndex ||
                    !emittedPlaces.Add(place) ||
                    !emitted.Add(idolId))
                {
                    participantIds.Clear();
                    return false;
                }

                participantIds.Add(idolId);
            }

            return true;
        }

        private static void AppendDistinct(
            IReadOnlyList<int> values,
            ISet<int> emitted,
            ICollection<int> target)
        {
            if (values == null)
            {
                return;
            }

            for (int index = 0; index < values.Count; index++)
            {
                int value = values[index];
                if (value >= CoreConstants.MinimumValidIdolIdentifier &&
                    emitted.Add(value))
                {
                    target.Add(value);
                }
            }
        }

        private static int ResolveSingleSenbatsuRowIndex(int positionIndex)
        {
            if (positionIndex <= CoreConstants.SenbatsuCenterPositionIndex)
            {
                return 0;
            }
            if (positionIndex <= 2)
            {
                return 1;
            }
            if (positionIndex <= 5)
            {
                return 2;
            }
            if (positionIndex <= 9)
            {
                return 3;
            }
            if (positionIndex <= 14)
            {
                return 4;
            }

            return 5;
        }
    }
}
