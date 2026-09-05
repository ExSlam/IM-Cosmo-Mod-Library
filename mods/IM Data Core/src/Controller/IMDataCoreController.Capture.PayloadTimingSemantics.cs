using System;
using System.Collections.Generic;

namespace IMDataCore
{
    /// <summary>
    /// Wave 3 payload/timing helpers. Runtime maps here are correlation-only and are
    /// intentionally not restored as gameplay state.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string BlackmailOccurrencePrefix = "bm:";

        private readonly Dictionary<Date_Influence._blackmail, string> blackmailOccurrenceByReference =
            new Dictionary<Date_Influence._blackmail, string>();

        private static string CreateBlackmailOccurrenceId()
        {
            return BlackmailOccurrencePrefix + Guid.NewGuid().ToString("N");
        }

        private void ResetPayloadTimingRuntimeStateLocked()
        {
            blackmailOccurrenceByReference.Clear();
        }

        private string ResolveBlackmailOccurrenceIdLocked(Date_Influence._blackmail blackmail)
        {
            if (blackmail == null)
            {
                return string.Empty;
            }

            string occurrenceId;
            if (blackmailOccurrenceByReference.TryGetValue(blackmail, out occurrenceId) &&
                !string.IsNullOrEmpty(occurrenceId))
            {
                return occurrenceId;
            }

            occurrenceId = CreateBlackmailOccurrenceId();
            blackmailOccurrenceByReference[blackmail] = occurrenceId;
            return occurrenceId;
        }

        internal BlackmailDequeueSnapshot CreateInfluenceBlackmailDequeueSnapshot()
        {
            BlackmailDequeueSnapshot snapshot = new BlackmailDequeueSnapshot();
            if (Date_Influence.Blackmail == null)
            {
                return snapshot;
            }

            snapshot.QueueSizeBefore = Date_Influence.Blackmail.Count;
            int simulatedQueueSize = snapshot.QueueSizeBefore;
            for (int index = Date_Influence.Blackmail.Count - 1;
                index >= CoreConstants.ZeroBasedListStartIndex;
                index--)
            {
                Date_Influence._blackmail item = Date_Influence.Blackmail[index];
                if (item != null && item.ReportDate <= staticVars.dateTime)
                {
                    snapshot.DueBefore.Add(item);
                    snapshot.QueueSizeBeforeRemoval[item] = simulatedQueueSize;
                    simulatedQueueSize--;
                }
            }
            return snapshot;
        }

        internal void CaptureInfluenceBlackmailDequeued(BlackmailDequeueSnapshot snapshotBefore)
        {
            if (snapshotBefore == null || snapshotBefore.DueBefore == null ||
                snapshotBefore.DueBefore.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < snapshotBefore.DueBefore.Count;
                    index++)
                {
                    Date_Influence._blackmail blackmail = snapshotBefore.DueBefore[index];
                    if (blackmail == null || blackmail.Spy == null || blackmail.Target == null ||
                        ContainsBlackmailReference(Date_Influence.Blackmail, blackmail))
                    {
                        continue;
                    }

                    string occurrenceId = ResolveBlackmailOccurrenceIdLocked(blackmail);
                    InfluenceBlackmailEventPayload payload = new InfluenceBlackmailEventPayload
                    {
                        spy_id = blackmail.Spy.id,
                        target_id = blackmail.Target.id,
                        influence_action = CoreConstants.InfluenceLifecycleActionBlackmailDequeued,
                        report_date = ResolveDateString(blackmail.ReportDate),
                        days_until_report = (int)(blackmail.ReportDate - staticVars.dateTime).TotalDays,
                        blackmail_occurrence_id = occurrenceId,
                        queue_size_before_dequeue = snapshotBefore.ResolveQueueSizeBeforeRemoval(blackmail),
                        queue_size_after_dequeue = Math.Max(0, snapshotBefore.ResolveQueueSizeBeforeRemoval(blackmail) - 1),
                        success_tier = CoreConstants.InvalidIdValue,
                        influence_award_planned = CoreConstants.ZeroBasedListStartIndex,
                        influence_award_applied_known = false,
                        event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                    };

                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        blackmail.Spy.id,
                        CoreConstants.EventEntityKindInfluence,
                        blackmail.Spy.id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        CoreConstants.EventTypeInfluenceBlackmailDequeued,
                        "patch.Date_Influence.CheckBlackmailQueue.Postfix",
                        CoreJsonUtility.SerializeObjectPayload(payload));

                    blackmailOccurrenceByReference.Remove(blackmail);
                }

                FlushAfterCaptureLocked();
            }
        }

        private static bool ContainsBlackmailReference(
            List<Date_Influence._blackmail> values,
            Date_Influence._blackmail target)
        {
            if (values == null || target == null)
            {
                return false;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex; index < values.Count; index++)
            {
                if (ReferenceEquals(values[index], target))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal sealed class BlackmailDequeueSnapshot
    {
        internal int QueueSizeBefore;
        internal List<Date_Influence._blackmail> DueBefore = new List<Date_Influence._blackmail>();
        internal Dictionary<Date_Influence._blackmail, int> QueueSizeBeforeRemoval =
            new Dictionary<Date_Influence._blackmail, int>();

        internal int ResolveQueueSizeBeforeRemoval(Date_Influence._blackmail blackmail)
        {
            int value;
            return blackmail != null && QueueSizeBeforeRemoval.TryGetValue(blackmail, out value)
                ? value
                : QueueSizeBefore;
        }
    }
}
