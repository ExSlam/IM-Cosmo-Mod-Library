using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        private readonly ConditionalWeakTable<Awards._speech, AwardSpeechDeliveryMarker> awardSpeechDeliveryMarkers =
            new ConditionalWeakTable<Awards._speech, AwardSpeechDeliveryMarker>();

        internal ShowCancellationTransitionSnapshot CreateShowCancellationTransitionSnapshot(Shows._show show)
        {
            if (show == null)
            {
                return null;
            }

            return new ShowCancellationTransitionSnapshot
            {
                ShowStatusBefore = show.status,
                ShowToCancelBefore = show.ToCancel
            };
        }

        internal void CaptureShowCancelTransition(Shows._show show, ShowCancellationTransitionSnapshot snapshot)
        {
            if (show == null || snapshot == null)
            {
                return;
            }

            if (snapshot.ShowStatusBefore != Shows._show._status.canceled && show.status == Shows._show._status.canceled)
            {
                CaptureShowCancelled(show, CoreConstants.EventSourceShowCancelMethodPatch);
                return;
            }

            if (!snapshot.ShowToCancelBefore && show.ToCancel && show.status != Shows._show._status.canceled)
            {
                CaptureShowCancellationTransition(
                    show,
                    snapshot,
                    CoreConstants.EventTypeShowCancellationScheduled,
                    CoreConstants.EventSourceShowCancelMethodPatch);
            }
        }

        internal void CaptureShowDontCancelTransition(Shows._show show, ShowCancellationTransitionSnapshot snapshot)
        {
            if (show == null || snapshot == null || !snapshot.ShowToCancelBefore || show.ToCancel)
            {
                return;
            }

            CaptureShowCancellationTransition(
                show,
                snapshot,
                CoreConstants.EventTypeShowCancellationWithdrawn,
                CoreConstants.EventSourceShowDontCancelMethodPatch);
        }

        private void CaptureShowCancellationTransition(
            Shows._show show,
            ShowCancellationTransitionSnapshot snapshot,
            string eventTypeCode,
            string sourcePatchCode)
        {
            ShowCancellationTransitionPayload payload = new ShowCancellationTransitionPayload
            {
                ShowId = show.id,
                ShowTitle = show.title ?? string.Empty,
                ShowEpisodeCount = show.episodeCount,
                PreviousShowStatus = CoreEnumNameMapping.ToShowStatusCode(snapshot.ShowStatusBefore),
                NewShowStatus = CoreEnumNameMapping.ToShowStatusCode(show.status),
                ShowToCancelBefore = snapshot.ShowToCancelBefore,
                ShowToCancelAfter = show.ToCancel
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindShow,
                    show.id.ToString(CultureInfo.InvariantCulture),
                    eventTypeCode,
                    sourcePatchCode,
                    CoreJsonUtility.SerializeShowCancellationTransitionPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        internal void CaptureAwardSpeechDelivered(
            Awards._speech speech,
            Date_GroupTalk._message._category resolvedThanks)
        {
            if (speech == null)
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

                AwardSpeechDeliveryMarker existingMarker;
                if (awardSpeechDeliveryMarkers.TryGetValue(speech, out existingMarker))
                {
                    return;
                }

                Awards._award awardOccurrence = ResolveAwardSpeechOccurrence(speech.Type);
                int awardYear = awardOccurrence != null ? awardOccurrence.Year : staticVars.dateTime.Year;
                int awardSubjectIdolId = awardOccurrence != null && awardOccurrence.Girl != null
                    ? awardOccurrence.Girl.id
                    : CoreConstants.InvalidIdValue;
                int awardSingleId = awardOccurrence != null && awardOccurrence.Single != null
                    ? awardOccurrence.Single.id
                    : CoreConstants.InvalidIdValue;
                int speechGiverId = speech.Girl != null ? speech.Girl.id : CoreConstants.InvalidIdValue;

                AwardSpeechPayload payload = new AwardSpeechPayload
                {
                    AwardType = CoreEnumNameMapping.ToAwardTypeCode(speech.Type),
                    AwardYear = awardYear,
                    AwardIsNomination = awardOccurrence != null && awardOccurrence.IsNomination,
                    AwardWon = speech.Won,
                    AwardSingleId = awardSingleId,
                    AwardSubjectIdolId = awardSubjectIdolId,
                    AwardSpeechGiverId = speechGiverId,
                    AwardConfiguredThanks = CoreEnumNameMapping.ToAwardSpeechThanksCode(speech.Thanks),
                    AwardResolvedThanks = CoreEnumNameMapping.ToAwardResolvedThanksCode(resolvedThanks),
                    AwardTargetIdolId = speech.Target_Girl != null ? speech.Target_Girl.id : CoreConstants.InvalidIdValue,
                    AwardTargetStaffId = speech.Target_Staff != null ? speech.Target_Staff.id : CoreConstants.InvalidIdValue
                };

                string awardEntityIdentifier = BuildAwardEntityIdentifier(
                    payload.AwardType,
                    awardYear,
                    awardSubjectIdolId,
                    awardSingleId);

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    speechGiverId,
                    CoreConstants.EventEntityKindAward,
                    awardEntityIdentifier,
                    CoreConstants.EventTypeAwardSpeechDelivered,
                    CoreConstants.EventSourceAwardSpeechGetThanksPatch,
                    CoreJsonUtility.SerializeAwardSpeechPayload(payload));
                awardSpeechDeliveryMarkers.Add(speech, new AwardSpeechDeliveryMarker());
                FlushAfterCaptureLocked();
            }
        }

        private static Awards._award ResolveAwardSpeechOccurrence(Awards._type awardType)
        {
            Awards._award temporaryNomination = Awards.GetTempNomination(awardType);
            if (temporaryNomination != null)
            {
                return temporaryNomination;
            }

            int currentYear = staticVars.dateTime.Year;
            Awards._award resolved = FindAwardOccurrence(Awards._Awards, awardType, currentYear);
            return resolved ?? FindAwardOccurrence(Awards._Nominations, awardType, currentYear);
        }

        private static Awards._award FindAwardOccurrence(
            IList<Awards._award> awards,
            Awards._type awardType,
            int awardYear)
        {
            if (awards == null)
            {
                return null;
            }

            for (int index = awards.Count - 1; index >= CoreConstants.ZeroBasedListStartIndex; index--)
            {
                Awards._award award = awards[index];
                if (award != null && award.Type == awardType && award.Year == awardYear)
                {
                    return award;
                }
            }

            return null;
        }

        private sealed class AwardSpeechDeliveryMarker
        {
        }
    }

    internal sealed class ShowCancellationTransitionSnapshot
    {
        public Shows._show._status ShowStatusBefore;
        public bool ShowToCancelBefore;
    }

    [Serializable]
    internal sealed class ShowCancellationTransitionPayload
    {
        public int ShowId;
        public string ShowTitle = string.Empty;
        public int ShowEpisodeCount;
        public string PreviousShowStatus = string.Empty;
        public string NewShowStatus = string.Empty;
        public bool ShowToCancelBefore;
        public bool ShowToCancelAfter;
    }

    [Serializable]
    internal sealed class AwardSpeechPayload
    {
        public string AwardType = string.Empty;
        public int AwardYear;
        public bool AwardIsNomination;
        public bool AwardWon;
        public int AwardSingleId = CoreConstants.InvalidIdValue;
        public int AwardSubjectIdolId = CoreConstants.InvalidIdValue;
        public int AwardSpeechGiverId = CoreConstants.InvalidIdValue;
        public string AwardConfiguredThanks = string.Empty;
        public string AwardResolvedThanks = string.Empty;
        public int AwardTargetIdolId = CoreConstants.InvalidIdValue;
        public int AwardTargetStaffId = CoreConstants.InvalidIdValue;
    }
}
