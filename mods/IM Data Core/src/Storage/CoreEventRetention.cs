using System;
using System.Collections.Generic;

namespace IMDataCore
{
    /// <summary>
    /// Persistence policy for built-in event rows that are implementation telemetry
    /// rather than durable historical facts.
    ///
    /// Vanilla is canonical for the current values represented by these streams:
    /// current idol status, research category points/assignments, and idol earnings
    /// current-month/history data. IMDC keeps the higher-level historical events
    /// surrounding those systems, but does not permanently store every technical
    /// mutation tick.
    ///
    /// Custom/namespaced events are never filtered, even if a consumer happens to
    /// reuse one of the built-in event type strings.
    /// </summary>
    internal static class CoreEventRetention
    {
        internal static bool ShouldPersist(PendingEvent pending)
        {
            return pending == null ||
                ShouldPersist(
                    pending.NamespaceIdentifier,
                    pending.EventType);
        }

        internal static bool ShouldPersist(LightweightEventRecord record)
        {
            return record == null ||
                ShouldPersist(
                    record.NamespaceIdentifier,
                    record.EventType);
        }

        internal static List<LightweightEventRecord> FilterLoadedEvents(
            IReadOnlyList<LightweightEventRecord> source,
            out int removedCount)
        {
            removedCount = 0;
            List<LightweightEventRecord> retained =
                new List<LightweightEventRecord>();

            if (source == null)
            {
                return retained;
            }

            retained.Capacity = source.Count;
            for (int index = 0; index < source.Count; index++)
            {
                LightweightEventRecord record = source[index];
                if (record == null)
                {
                    continue;
                }

                if (!ShouldPersist(record))
                {
                    removedCount++;
                    continue;
                }

                retained.Add(record);
            }

            return retained;
        }

        private static bool ShouldPersist(
            string namespaceIdentifier,
            string eventType)
        {
            // IMDC must never reinterpret or prune consumer-owned custom events.
            if (!string.IsNullOrEmpty(namespaceIdentifier))
            {
                return true;
            }

            string normalizedEventType = eventType ?? string.Empty;

            return !string.Equals(
                       normalizedEventType,
                       CoreConstants.EventTypeStatusChanged,
                       StringComparison.Ordinal) &&
                !string.Equals(
                       normalizedEventType,
                       CoreConstants.EventTypeResearchPointsAccrued,
                       StringComparison.Ordinal) &&
                !string.Equals(
                       normalizedEventType,
                       CoreConstants.EventTypeIdolEarningsRecorded,
                       StringComparison.Ordinal);
        }
    }
}
