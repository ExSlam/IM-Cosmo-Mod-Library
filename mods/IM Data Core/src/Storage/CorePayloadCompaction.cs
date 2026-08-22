using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Lossless physical compaction for built-in IMDC events.
    ///
    /// Money details keep the same public DTO semantics while the private JSON
    /// omits fields whose values are already the DTO default. Shared built-in
    /// occurrences are stored once and indexed under their historical participant
    /// idol ids by SharedTimelineParticipants. Consumer-owned namespaced events are
    /// never rewritten.
    /// </summary>
    internal static class CorePayloadCompaction
    {
        private const string MoneyTransactionEventType = "money_transaction";
        private const string ShowEpisodeReleasedEventType = "show_episode_released";
        private const string ShowCastIdListPropertyName = "show_cast_id_list";
        private const string ShowEpisodeCountPropertyName = "show_episode_count";
        private const string ShowEpisodeDatePropertyName = "show_episode_date";
        private const string ShowCastChangedEventType = "show_cast_changed";
        internal const string CanonicalShowEpisodeSource =
            "patch.imdatacore.postmod.Shows._show.NewEpisode.Finalizer";
        internal const string CanonicalShowCastSourcePrefix =
            "patch.imdatacore.postmod.show_cast.";

        internal static List<PendingEvent> CompactPendingEvents(
            IReadOnlyList<PendingEvent> source,
            out int sparseMoneyPayloadCount,
            out int sharedParticipantRowsRemoved)
        {
            sparseMoneyPayloadCount = 0;
            sharedParticipantRowsRemoved = 0;
            List<PendingEvent> result = new List<PendingEvent>();
            if (source == null || source.Count == 0)
            {
                return result;
            }

            Dictionary<string, List<PendingEvent>> episodeGroups =
                new Dictionary<string, List<PendingEvent>>(StringComparer.Ordinal);
            Dictionary<string, List<PendingEvent>> castChangeGroups =
                new Dictionary<string, List<PendingEvent>>(StringComparer.Ordinal);

            for (int index = 0; index < source.Count; index++)
            {
                PendingEvent pending = source[index];
                if (pending == null)
                {
                    continue;
                }

                PendingEvent compacted = ClonePending(pending);
                bool payloadChanged;
                compacted.PayloadJson = CompactPayload(
                    compacted.NamespaceIdentifier,
                    compacted.EventType,
                    compacted.PayloadJson,
                    out payloadChanged);
                if (payloadChanged)
                {
                    sparseMoneyPayloadCount++;
                }

                string episodeIdentity;
                List<int> participantIds;
                if (TryGetSharedShowParticipantIds(
                        compacted.NamespaceIdentifier,
                        compacted.EventType,
                        compacted.PayloadJson,
                        out participantIds) &&
                    TryBuildShowEpisodeIdentity(
                        compacted.EntityKind,
                        compacted.EntityId,
                        compacted.EventType,
                        compacted.GameDateTime,
                        compacted.PayloadJson,
                        out episodeIdentity))
                {
                    compacted.IdolId = CoreConstants.InvalidIdValue;
                    AddGrouped(episodeGroups, episodeIdentity, compacted);
                    continue;
                }

                if (IsBuiltInShowCastChange(
                        compacted.NamespaceIdentifier,
                        compacted.EventType))
                {
                    AddGrouped(
                        castChangeGroups,
                        BuildShowMutationIdentity(
                            compacted.EntityId,
                            compacted.GameDateTime),
                        compacted);
                    continue;
                }

                result.Add(compacted);
            }

            foreach (KeyValuePair<string, List<PendingEvent>> pair in episodeGroups)
            {
                AppendPendingEpisodeRepresentatives(
                    pair.Value,
                    result,
                    ref sharedParticipantRowsRemoved);
            }

            foreach (KeyValuePair<string, List<PendingEvent>> pair in castChangeGroups)
            {
                AppendPendingCastChangeRepresentatives(
                    pair.Value,
                    result,
                    ref sharedParticipantRowsRemoved);
            }

            result.Sort(ComparePendingEventsBySequenceAscending);
            return result;
        }

        internal static List<LightweightEventRecord> CompactLoadedEvents(
            IReadOnlyList<LightweightEventRecord> source,
            out int sparseMoneyPayloadCount,
            out int sharedParticipantRowsRemoved)
        {
            sparseMoneyPayloadCount = 0;
            sharedParticipantRowsRemoved = 0;
            List<LightweightEventRecord> result =
                new List<LightweightEventRecord>();
            if (source == null || source.Count == 0)
            {
                return result;
            }

            Dictionary<string, List<LightweightEventRecord>> episodeGroups =
                new Dictionary<string, List<LightweightEventRecord>>(
                    StringComparer.Ordinal);
            Dictionary<string, List<LightweightEventRecord>> castChangeGroups =
                new Dictionary<string, List<LightweightEventRecord>>(
                    StringComparer.Ordinal);

            for (int index = 0; index < source.Count; index++)
            {
                LightweightEventRecord record = source[index];
                if (record == null)
                {
                    continue;
                }

                bool payloadChanged;
                string compactedPayload = CompactPayload(
                    record.NamespaceIdentifier,
                    record.EventType,
                    record.PayloadJson,
                    out payloadChanged);
                LightweightEventRecord compacted = record;
                if (payloadChanged)
                {
                    compacted = CloneEvent(record);
                    compacted.PayloadJson = compactedPayload;
                    compacted.StoragePayloadJson = string.Empty;
                    sparseMoneyPayloadCount++;
                }

                string episodeIdentity;
                List<int> participantIds;
                if (TryGetSharedShowParticipantIds(
                        compacted.NamespaceIdentifier,
                        compacted.EventType,
                        compacted.PayloadJson,
                        out participantIds) &&
                    TryBuildShowEpisodeIdentity(
                        compacted.EntityKind,
                        compacted.EntityId,
                        compacted.EventType,
                        compacted.GameDateTime,
                        compacted.PayloadJson,
                        out episodeIdentity))
                {
                    if (ReferenceEquals(compacted, record))
                    {
                        compacted = CloneEvent(record);
                    }
                    compacted.IdolId = CoreConstants.InvalidIdValue;
                    AddGrouped(episodeGroups, episodeIdentity, compacted);
                    continue;
                }

                if (IsBuiltInShowCastChange(
                        compacted.NamespaceIdentifier,
                        compacted.EventType))
                {
                    AddGrouped(
                        castChangeGroups,
                        BuildShowMutationIdentity(
                            compacted.EntityId,
                            compacted.GameDateTime),
                        compacted);
                    continue;
                }

                result.Add(compacted);
            }

            foreach (KeyValuePair<string, List<LightweightEventRecord>> pair
                in episodeGroups)
            {
                AppendLoadedEpisodeRepresentatives(
                    pair.Value,
                    result,
                    ref sharedParticipantRowsRemoved);
            }

            foreach (KeyValuePair<string, List<LightweightEventRecord>> pair
                in castChangeGroups)
            {
                AppendLoadedCastChangeRepresentatives(
                    pair.Value,
                    result,
                    ref sharedParticipantRowsRemoved);
            }

            result.Sort(CompareEventsBySequenceAscending);
            return result;
        }

        internal static string ExpandMoneyTransactionPayloadForPublic(
            LightweightEventRecord record)
        {
            if (record == null ||
                !string.Equals(
                    record.EventType ?? string.Empty,
                    MoneyTransactionEventType,
                    StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(record.NamespaceIdentifier))
            {
                return record == null
                    ? CoreConstants.EmptyJsonObject
                    : record.PayloadJson ?? CoreConstants.EmptyJsonObject;
            }

            return LightweightSidecarJson.ExpandMoneyTransactionPayloadForPublic(
                record.PayloadJson);
        }

        internal static string ExpandSharedTimelinePayloadForPublic(
            LightweightEventRecord record,
            int requestedIdolId)
        {
            return SharedTimelineParticipants.ExpandPayloadForPublic(
                record,
                requestedIdolId);
        }

        internal static bool TryGetSharedShowParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            return record != null &&
                TryGetSharedShowParticipantIds(
                    record.NamespaceIdentifier,
                    record.EventType,
                    record.PayloadJson,
                    out participantIds);
        }

        internal static bool TryGetSharedTimelineParticipantIds(
            LightweightEventRecord record,
            out List<int> participantIds)
        {
            return SharedTimelineParticipants.TryGetParticipantIds(
                record,
                out participantIds);
        }

        internal static bool IsSharedTimelineEvent(
            LightweightEventRecord record)
        {
            return SharedTimelineParticipants.IsSharedEvent(record);
        }

        internal static bool IsSharedShowEvent(
            LightweightEventRecord record)
        {
            List<int> ignored;
            return record != null &&
                record.IdolId == CoreConstants.InvalidIdValue &&
                TryGetSharedShowParticipantIds(record, out ignored);
        }

        private static string CompactPayload(
            string namespaceIdentifier,
            string eventType,
            string payloadJson,
            out bool changed)
        {
            changed = false;
            if (!string.IsNullOrEmpty(namespaceIdentifier) ||
                !string.Equals(
                    eventType ?? string.Empty,
                    MoneyTransactionEventType,
                    StringComparison.Ordinal))
            {
                return payloadJson ?? CoreConstants.EmptyJsonObject;
            }

            return LightweightSidecarJson.CompactMoneyTransactionPayload(
                payloadJson,
                out changed);
        }

        private static bool TryGetSharedShowParticipantIds(
            string namespaceIdentifier,
            string eventType,
            string payloadJson,
            out List<int> participantIds)
        {
            participantIds = new List<int>();
            if (!string.IsNullOrEmpty(namespaceIdentifier) ||
                !IsShowEpisodeEventType(eventType))
            {
                return false;
            }

            return LightweightSidecarJson.TryReadCsvIntProperty(
                payloadJson,
                ShowCastIdListPropertyName,
                CoreConstants.MinimumValidIdolIdentifier,
                out participantIds);
        }

        private static bool IsShowEpisodeEventType(string eventType)
        {
            return string.Equals(
                eventType ?? string.Empty,
                ShowEpisodeReleasedEventType,
                StringComparison.Ordinal);
        }

        private static bool IsBuiltInShowCastChange(
            string namespaceIdentifier,
            string eventType)
        {
            return string.IsNullOrEmpty(namespaceIdentifier) &&
                string.Equals(
                    eventType ?? string.Empty,
                    ShowCastChangedEventType,
                    StringComparison.Ordinal);
        }

        private static bool IsCanonicalShowEpisodeSource(string sourcePatch)
        {
            return string.Equals(
                sourcePatch ?? string.Empty,
                CanonicalShowEpisodeSource,
                StringComparison.Ordinal);
        }

        private static bool IsCanonicalShowCastSource(string sourcePatch)
        {
            return (sourcePatch ?? string.Empty).StartsWith(
                CanonicalShowCastSourcePrefix,
                StringComparison.Ordinal);
        }

        private static bool TryBuildShowEpisodeIdentity(
            string entityKind,
            string entityId,
            string eventType,
            string gameDateTime,
            string payloadJson,
            out string identity)
        {
            identity = string.Empty;
            int episodeCount;
            if (!LightweightSidecarJson.TryReadIntProperty(
                    payloadJson,
                    ShowEpisodeCountPropertyName,
                    out episodeCount))
            {
                return false;
            }

            string episodeDate;
            if (!LightweightSidecarJson.TryReadStringProperty(
                    payloadJson,
                    ShowEpisodeDatePropertyName,
                    out episodeDate) ||
                string.IsNullOrEmpty(episodeDate))
            {
                episodeDate = gameDateTime ?? string.Empty;
            }

            StringBuilder builder = new StringBuilder(128);
            AppendIdentityPart(builder, entityKind);
            AppendIdentityPart(builder, entityId);
            AppendIdentityPart(builder, eventType);
            AppendIdentityPart(
                builder,
                episodeCount.ToString(CultureInfo.InvariantCulture));
            AppendIdentityPart(builder, episodeDate);
            identity = builder.ToString();
            return true;
        }

        private static string BuildShowMutationIdentity(
            string entityId,
            string gameDateTime)
        {
            StringBuilder builder = new StringBuilder(64);
            AppendIdentityPart(builder, entityId);
            AppendIdentityPart(builder, gameDateTime);
            return builder.ToString();
        }

        private static void AddGrouped<T>(
            Dictionary<string, List<T>> groups,
            string key,
            T value)
        {
            List<T> rows;
            if (!groups.TryGetValue(key, out rows))
            {
                rows = new List<T>();
                groups.Add(key, rows);
            }
            rows.Add(value);
        }

        private static void AppendPendingEpisodeRepresentatives(
            List<PendingEvent> group,
            List<PendingEvent> result,
            ref int removedCount)
        {
            if (group == null || group.Count == 0)
            {
                return;
            }

            PendingEvent canonical = null;
            for (int index = 0; index < group.Count; index++)
            {
                PendingEvent row = group[index];
                if (row != null &&
                    IsCanonicalShowEpisodeSource(row.SourcePatch) &&
                    (canonical == null ||
                     row.CaptureSequence > canonical.CaptureSequence))
                {
                    canonical = row;
                }
            }

            if (canonical != null)
            {
                result.Add(canonical);
                removedCount += Math.Max(0, group.Count - 1);
                return;
            }

            Dictionary<string, PendingEvent> byPayload =
                new Dictionary<string, PendingEvent>(StringComparer.Ordinal);
            for (int index = 0; index < group.Count; index++)
            {
                PendingEvent row = group[index];
                if (row == null)
                {
                    continue;
                }
                string payload = row.PayloadJson ?? CoreConstants.EmptyJsonObject;
                PendingEvent existing;
                if (!byPayload.TryGetValue(payload, out existing) ||
                    row.CaptureSequence < existing.CaptureSequence)
                {
                    byPayload[payload] = row;
                }
            }
            foreach (PendingEvent row in byPayload.Values)
            {
                result.Add(row);
            }
            removedCount += Math.Max(0, group.Count - byPayload.Count);
        }

        private static void AppendLoadedEpisodeRepresentatives(
            List<LightweightEventRecord> group,
            List<LightweightEventRecord> result,
            ref int removedCount)
        {
            if (group == null || group.Count == 0)
            {
                return;
            }

            LightweightEventRecord canonical = null;
            for (int index = 0; index < group.Count; index++)
            {
                LightweightEventRecord row = group[index];
                if (row != null &&
                    IsCanonicalShowEpisodeSource(row.SourcePatch) &&
                    (canonical == null || row.Sequence > canonical.Sequence))
                {
                    canonical = row;
                }
            }

            if (canonical != null)
            {
                result.Add(canonical);
                removedCount += Math.Max(0, group.Count - 1);
                return;
            }

            Dictionary<string, LightweightEventRecord> byPayload =
                new Dictionary<string, LightweightEventRecord>(StringComparer.Ordinal);
            for (int index = 0; index < group.Count; index++)
            {
                LightweightEventRecord row = group[index];
                if (row == null)
                {
                    continue;
                }
                string payload = row.PayloadJson ?? CoreConstants.EmptyJsonObject;
                LightweightEventRecord existing;
                if (!byPayload.TryGetValue(payload, out existing) ||
                    row.Sequence < existing.Sequence)
                {
                    byPayload[payload] = row;
                }
            }
            foreach (LightweightEventRecord row in byPayload.Values)
            {
                result.Add(row);
            }
            removedCount += Math.Max(0, group.Count - byPayload.Count);
        }

        private static void AppendPendingCastChangeRepresentatives(
            List<PendingEvent> group,
            List<PendingEvent> result,
            ref int removedCount)
        {
            bool hasCanonical = false;
            for (int index = 0; index < group.Count; index++)
            {
                if (group[index] != null &&
                    IsCanonicalShowCastSource(group[index].SourcePatch))
                {
                    hasCanonical = true;
                    break;
                }
            }

            if (hasCanonical)
            {
                for (int index = 0; index < group.Count; index++)
                {
                    PendingEvent row = group[index];
                    if (row == null)
                    {
                        continue;
                    }
                    if (!IsCanonicalShowCastSource(row.SourcePatch))
                    {
                        removedCount++;
                        continue;
                    }
                    result.Add(row);
                }
                return;
            }

            // Old sidecars can contain one byte-identical cast transition per
            // participating idol. Without a settled observer marker, collapse
            // only exact payload duplicates and keep the earliest sequence.
            Dictionary<string, PendingEvent> byPayload =
                new Dictionary<string, PendingEvent>(StringComparer.Ordinal);
            for (int index = 0; index < group.Count; index++)
            {
                PendingEvent row = group[index];
                if (row == null)
                {
                    continue;
                }
                string payload = row.PayloadJson ?? CoreConstants.EmptyJsonObject;
                PendingEvent existing;
                if (!byPayload.TryGetValue(payload, out existing) ||
                    row.CaptureSequence < existing.CaptureSequence)
                {
                    byPayload[payload] = row;
                }
            }
            foreach (PendingEvent row in byPayload.Values)
            {
                result.Add(row);
            }
            removedCount += Math.Max(0, group.Count - byPayload.Count);
        }

        private static void AppendLoadedCastChangeRepresentatives(
            List<LightweightEventRecord> group,
            List<LightweightEventRecord> result,
            ref int removedCount)
        {
            bool hasCanonical = false;
            for (int index = 0; index < group.Count; index++)
            {
                if (group[index] != null &&
                    IsCanonicalShowCastSource(group[index].SourcePatch))
                {
                    hasCanonical = true;
                    break;
                }
            }

            if (hasCanonical)
            {
                for (int index = 0; index < group.Count; index++)
                {
                    LightweightEventRecord row = group[index];
                    if (row == null)
                    {
                        continue;
                    }
                    if (!IsCanonicalShowCastSource(row.SourcePatch))
                    {
                        removedCount++;
                        continue;
                    }
                    result.Add(row);
                }
                return;
            }

            Dictionary<string, LightweightEventRecord> byPayload =
                new Dictionary<string, LightweightEventRecord>(StringComparer.Ordinal);
            for (int index = 0; index < group.Count; index++)
            {
                LightweightEventRecord row = group[index];
                if (row == null)
                {
                    continue;
                }
                string payload = row.PayloadJson ?? CoreConstants.EmptyJsonObject;
                LightweightEventRecord existing;
                if (!byPayload.TryGetValue(payload, out existing) ||
                    row.Sequence < existing.Sequence)
                {
                    byPayload[payload] = row;
                }
            }
            foreach (LightweightEventRecord row in byPayload.Values)
            {
                result.Add(row);
            }
            removedCount += Math.Max(0, group.Count - byPayload.Count);
        }

        private static int ComparePendingEventsBySequenceAscending(
            PendingEvent left,
            PendingEvent right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }
            return left.CaptureSequence.CompareTo(right.CaptureSequence);
        }

        private static PendingEvent ClonePending(PendingEvent source)
        {
            return new PendingEvent
            {
                CaptureSequence = source.CaptureSequence,
                GameDateKey = source.GameDateKey,
                GameDateTime = source.GameDateTime ?? string.Empty,
                IdolId = source.IdolId,
                EntityKind = source.EntityKind ?? string.Empty,
                EntityId = source.EntityId ?? string.Empty,
                EventType = source.EventType ?? string.Empty,
                SourcePatch = source.SourcePatch ?? string.Empty,
                NamespaceIdentifier =
                    source.NamespaceIdentifier ?? string.Empty,
                IdempotencyKey = source.IdempotencyKey ?? string.Empty,
                PayloadJson =
                    source.PayloadJson ?? CoreConstants.EmptyJsonObject
            };
        }

        private static LightweightEventRecord CloneEvent(
            LightweightEventRecord source)
        {
            return new LightweightEventRecord
            {
                Sequence = source.Sequence,
                GameDateKey = source.GameDateKey,
                GameDateTime = source.GameDateTime ?? string.Empty,
                IdolId = source.IdolId,
                EntityKind = source.EntityKind ?? string.Empty,
                EntityId = source.EntityId ?? string.Empty,
                EventType = source.EventType ?? string.Empty,
                SourcePatch = source.SourcePatch ?? string.Empty,
                NamespaceIdentifier =
                    source.NamespaceIdentifier ?? string.Empty,
                IdempotencyKey = source.IdempotencyKey ?? string.Empty,
                PayloadJson =
                    source.PayloadJson ?? CoreConstants.EmptyJsonObject,
                StoragePayloadJson = source.StoragePayloadJson ?? string.Empty
            };
        }

        private static int CompareEventsBySequenceAscending(
            LightweightEventRecord left,
            LightweightEventRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }

            int sequenceComparison = left.Sequence.CompareTo(right.Sequence);
            return sequenceComparison != 0
                ? sequenceComparison
                : left.Sequence.CompareTo(right.Sequence);
        }

        private static void AppendIdentityPart(
            StringBuilder builder,
            string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(
                normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append('|');
        }
    }
}
