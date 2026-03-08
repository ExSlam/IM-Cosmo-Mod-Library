using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        /// <summary>
        /// Captures one single-created lifecycle event for each cast idol.
        /// </summary>
        internal void CaptureSingleCreated(singles._single createdSingle)
        {
            if (createdSingle == null)
            {
                return;
            }

            List<int> castIdolIdentifiers = ResolveDistinctSingleCastIdolIdentifiers(createdSingle);
            SingleLifecyclePayload payload = BuildSingleLifecyclePayload(
                createdSingle,
                castIdolIdentifiers,
                CoreConstants.SingleLifecycleActionCreated);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string eventPayloadJson = CoreJsonUtility.SerializeSingleLifecyclePayload(payload);
                string singleEntityIdentifier = createdSingle.id.ToString(CultureInfo.InvariantCulture);

                if (castIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSingle,
                        singleEntityIdentifier,
                        CoreConstants.EventTypeSingleCreated,
                        CoreConstants.EventSourceSingleAddNewPatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < castIdolIdentifiers.Count; castIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindSingle,
                            singleEntityIdentifier,
                            CoreConstants.EventTypeSingleCreated,
                            CoreConstants.EventSourceSingleAddNewPatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one released single and records per-idol participation projections.
        /// </summary>
        internal void CaptureSingleReleased(singles._single releasedSingle, string sourcePatch = null)
        {
            if (releasedSingle == null || releasedSingle.girls == null || releasedSingle.girls.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            string resolvedSourcePatch = string.IsNullOrEmpty(sourcePatch)
                ? CoreConstants.EventSourceSingleReleasePatch
                : sourcePatch;

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                int gameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate);
                string gameDateTime = CoreDateTimeUtility.ToRoundTripString(gameDate);
                string releaseDate = ResolveReleaseDate(releasedSingle, gameDate);
                long totalSales = ResolveTotalSales(releasedSingle);
                int quality = ResolveQuality(releasedSingle);
                int fanSatisfaction = ResolveFanSatisfaction(releasedSingle);
                int fanBuzz = ResolveFanBuzz(releasedSingle);
                int newFans = ResolveNewFans(releasedSingle);
                int newHardcoreFans = ResolveNewHardcoreFans(releasedSingle);
                int newCasualFans = ResolveNewCasualFans(releasedSingle);
                int singleQuantity = ResolveSingleQuantity(releasedSingle);
                long singleProductionCost = ResolveSingleProductionCost(releasedSingle);
                float singleMarketingResult = ResolveSingleMarketingResult(releasedSingle);
                string singleMarketingResultStatus = ResolveSingleMarketingResultStatus(releasedSingle);
                long singleGrossRevenue = ResolveSingleGrossRevenue(releasedSingle);
                int singleOneCdCost = ResolveSingleOneCdCost(releasedSingle);
                int singleOneCdRevenue = ResolveSingleOneCdRevenue(releasedSingle);
                long singleOtherExpenses = ResolveSingleOtherExpenses(releasedSingle);
                bool singleIsGroupHandshake = ResolveSingleIsGroupHandshake(releasedSingle);
                bool singleIsIndividualHandshake = ResolveSingleIsIndividualHandshake(releasedSingle);
                int singleFamePointsAwarded = ResolveSingleFamePointsAwarded(releasedSingle);
                long singleProfit = ResolveSingleProfit(releasedSingle);
                float singleSalesPerFan = ResolveSalesPerFan(releasedSingle);
                float singleFameOfSenbatsu = ResolveFameOfSenbatsu(releasedSingle);
                bool singleMostPopularGenre = ResolveMostPopularGenre(releasedSingle);
                bool singleMostPopularLyrics = ResolveMostPopularLyrics(releasedSingle);
                bool singleMostPopularChoreo = ResolveMostPopularChoreo(releasedSingle);
                float singleFanAppealMale = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.male);
                float singleFanAppealFemale = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.female);
                float singleFanAppealCasual = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.casual);
                float singleFanAppealHardcore = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.hardcore);
                float singleFanAppealTeen = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.teen);
                float singleFanAppealYoungAdult = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.youngAdult);
                float singleFanAppealAdult = ResolveReleaseFanAppealRatio(releasedSingle, resources.fanType.adult);
                string singleFanSegmentSalesSummary = BuildSingleFanSegmentSalesSummary(releasedSingle);
                string singleFanSegmentNewFansSummary = BuildSingleFanSegmentNewFansSummary(releasedSingle);
                string singleSenbatsuStatsSnapshot = BuildSingleSenbatsuStatsSnapshot(releasedSingle);
                int chartPosition = ResolveChartPosition(releasedSingle);
                string singleStatus = CoreEnumNameMapping.ToSingleStatusCode(releasedSingle.status);

                for (int i = CoreConstants.ZeroBasedListStartIndex; i < releasedSingle.girls.Count; i++)
                {
                    data_girls.girls idol = releasedSingle.girls[i];
                    if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        continue;
                    }

                    int positionIndex = singles.GetGirlsPositionInSenbatsu(releasedSingle.girls, idol);
                    int rowIndex = singles.GetGirlsRowInSenbatsu(releasedSingle.girls, idol);
                    bool isCenter = positionIndex == CoreConstants.SenbatsuCenterPositionIndex;

                    SingleParticipationPayload payload = new SingleParticipationPayload
                    {
                        SingleId = releasedSingle.id,
                        SingleTitle = releasedSingle.title ?? string.Empty,
                        SingleStatus = singleStatus,
                        IdolId = idol.id,
                        RowIndex = rowIndex,
                        PositionIndex = positionIndex,
                        IsCenter = isCenter,
                        SingleReleaseDate = releaseDate,
                        TotalSales = totalSales,
                        Quality = quality,
                        FanSatisfaction = fanSatisfaction,
                        FanBuzz = fanBuzz,
                        NewFans = newFans,
                        NewHardcoreFans = newHardcoreFans,
                        NewCasualFans = newCasualFans,
                        SingleQuantity = singleQuantity,
                        SingleProductionCost = singleProductionCost,
                        SingleMarketingResult = singleMarketingResult,
                        SingleMarketingResultStatus = singleMarketingResultStatus,
                        SingleGrossRevenue = singleGrossRevenue,
                        SingleOneCdCost = singleOneCdCost,
                        SingleOneCdRevenue = singleOneCdRevenue,
                        SingleOtherExpenses = singleOtherExpenses,
                        SingleIsGroupHandshake = singleIsGroupHandshake,
                        SingleIsIndividualHandshake = singleIsIndividualHandshake,
                        SingleFamePointsAwarded = singleFamePointsAwarded,
                        SingleProfit = singleProfit,
                        SingleSalesPerFan = singleSalesPerFan,
                        SingleFameOfSenbatsu = singleFameOfSenbatsu,
                        SingleMostPopularGenre = singleMostPopularGenre,
                        SingleMostPopularLyrics = singleMostPopularLyrics,
                        SingleMostPopularChoreo = singleMostPopularChoreo,
                        SingleFanAppealMale = singleFanAppealMale,
                        SingleFanAppealFemale = singleFanAppealFemale,
                        SingleFanAppealCasual = singleFanAppealCasual,
                        SingleFanAppealHardcore = singleFanAppealHardcore,
                        SingleFanAppealTeen = singleFanAppealTeen,
                        SingleFanAppealYoungAdult = singleFanAppealYoungAdult,
                        SingleFanAppealAdult = singleFanAppealAdult,
                        SingleFanSegmentSalesSummary = singleFanSegmentSalesSummary,
                        SingleFanSegmentNewFansSummary = singleFanSegmentNewFansSummary,
                        SingleSenbatsuStatsSnapshot = singleSenbatsuStatsSnapshot,
                        ChartPosition = chartPosition
                    };

                    string payloadJson = CoreJsonUtility.SerializeSingleParticipationPayload(payload);
                    PendingEvent pendingEvent = new PendingEvent
                    {
                        SaveKey = activeSaveKey,
                        GameDateKey = gameDateKey,
                        GameDateTime = gameDateTime,
                        IdolId = idol.id,
                        EntityKind = CoreConstants.EventEntityKindSingle,
                        EntityId = releasedSingle.id.ToString(CultureInfo.InvariantCulture),
                        EventType = CoreConstants.EventTypeSingleReleased,
                        SourcePatch = resolvedSourcePatch,
                        PayloadJson = payloadJson
                    };
                    PendingEvent participationRecordedEvent = new PendingEvent
                    {
                        SaveKey = activeSaveKey,
                        GameDateKey = gameDateKey,
                        GameDateTime = gameDateTime,
                        IdolId = idol.id,
                        EntityKind = CoreConstants.EventEntityKindSingle,
                        EntityId = releasedSingle.id.ToString(CultureInfo.InvariantCulture),
                        EventType = CoreConstants.EventTypeSingleParticipationRecorded,
                        SourcePatch = resolvedSourcePatch,
                        PayloadJson = payloadJson
                    };

                    SingleParticipationProjection projection = new SingleParticipationProjection
                    {
                        SaveKey = activeSaveKey,
                        SingleId = releasedSingle.id,
                        IdolId = idol.id,
                        RowIndex = rowIndex,
                        PositionIndex = positionIndex,
                        IsCenterFlag = isCenter ? CoreConstants.EnabledCenterFlag : CoreConstants.DisabledCenterFlag,
                        ReleaseDate = releaseDate
                    };

                    bufferedEvents.Add(pendingEvent);
                    bufferedEvents.Add(participationRecordedEvent);
                    bufferedSingleParticipationRows.Add(projection);
                }

                if (!FlushLocked(false, out errorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageFlushFailed + errorMessage);
                }
            }
        }

        /// <summary>
        /// Captures one resolved chart-position snapshot for a released single.
        /// </summary>
        internal void CaptureSingleChartPositionResolved(singles._single releasedSingle, int chartPosition, string sourcePatch = null)
        {
            if (releasedSingle == null
                || releasedSingle.id < CoreConstants.MinimumValidIdolIdentifier
                || chartPosition <= CoreConstants.ZeroBasedListStartIndex
                || releasedSingle.status != singles._single._status.released)
            {
                return;
            }

            bool shouldCapture = false;
            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                int knownChartPosition;
                if (resolvedSingleChartPositionBySingleId.TryGetValue(releasedSingle.id, out knownChartPosition)
                    && knownChartPosition == chartPosition)
                {
                    return;
                }

                resolvedSingleChartPositionBySingleId[releasedSingle.id] = chartPosition;
                if (releasedSingle.ReleaseData != null)
                {
                    releasedSingle.ReleaseData.Chart_Position = chartPosition;
                }

                shouldCapture = true;
            }

            if (!shouldCapture)
            {
                return;
            }

            string resolvedSourcePatch = string.IsNullOrEmpty(sourcePatch)
                ? CoreConstants.EventSourceSingleChartPopupPatch
                : sourcePatch;
            CaptureSingleReleased(releasedSingle, resolvedSourcePatch);
        }

        /// <summary>
        /// Captures one canceled single lifecycle event for each cast idol.
        /// </summary>
        internal void CaptureSingleCancelled(singles._single cancelledSingle)
        {
            if (cancelledSingle == null)
            {
                return;
            }

            List<int> castIdolIdentifiers = ResolveDistinctSingleCastIdolIdentifiers(cancelledSingle);
            SingleLifecyclePayload payload = BuildSingleLifecyclePayload(
                cancelledSingle,
                castIdolIdentifiers,
                CoreConstants.SingleLifecycleActionCancelled);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string eventPayloadJson = CoreJsonUtility.SerializeSingleLifecyclePayload(payload);
                string singleEntityIdentifier = cancelledSingle.id.ToString(CultureInfo.InvariantCulture);

                if (castIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSingle,
                        singleEntityIdentifier,
                        CoreConstants.EventTypeSingleCancelled,
                        CoreConstants.EventSourceSingleCancelPatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < castIdolIdentifiers.Count; castIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindSingle,
                            singleEntityIdentifier,
                            CoreConstants.EventTypeSingleCancelled,
                            CoreConstants.EventSourceSingleCancelPatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one single status transition event for each cast idol.
        /// </summary>
        internal void CaptureSingleStatusTransition(
            singles._single single,
            singles._single._status previousStatus,
            singles._single._status newStatus)
        {
            if (single == null)
            {
                return;
            }

            string previousStatusCode = CoreEnumNameMapping.ToSingleStatusCode(previousStatus);
            string newStatusCode = CoreEnumNameMapping.ToSingleStatusCode(newStatus);
            if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
            {
                return;
            }

            List<int> castIdolIdentifiers = ResolveDistinctSingleCastIdolIdentifiers(single);
            SingleStatusPayload payload = new SingleStatusPayload
            {
                SingleId = single.id,
                SingleTitle = single.title ?? string.Empty,
                PreviousSingleStatus = previousStatusCode,
                NewSingleStatus = newStatusCode,
                SingleCastCount = castIdolIdentifiers.Count,
                SingleCastIdList = BuildDelimitedIdentifierList(castIdolIdentifiers),
                SingleIsDigital = single.IsDigital(),
                SingleLinkedElectionId = ResolveElectionIdFromSingle(single)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string eventPayloadJson = CoreJsonUtility.SerializeSingleStatusPayload(payload);
                string singleEntityIdentifier = single.id.ToString(CultureInfo.InvariantCulture);

                if (castIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSingle,
                        singleEntityIdentifier,
                        CoreConstants.EventTypeSingleStatusChanged,
                        CoreConstants.EventSourceSingleStatusPatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < castIdolIdentifiers.Count; castIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindSingle,
                            singleEntityIdentifier,
                            CoreConstants.EventTypeSingleStatusChanged,
                            CoreConstants.EventSourceSingleStatusPatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation single cast state for remove-girl hooks.
        /// </summary>
        internal SingleCastChangeSnapshot CreateSingleCastChangeSnapshot(singles._single single)
        {
            SingleCastChangeSnapshot snapshot = new SingleCastChangeSnapshot();
            if (single == null)
            {
                return snapshot;
            }

            snapshot.SingleCastIdolIdentifiersBefore = ResolveDistinctSingleCastIdolIdentifiers(single);
            snapshot.SingleStatusBefore = single.status;
            Groups._group groupBefore = single.GetGroup();
            if (groupBefore != null)
            {
                snapshot.SingleGroupIdBefore = groupBefore.ID;
                snapshot.SingleGroupTitleBefore = groupBefore.Title ?? string.Empty;
                snapshot.SingleGroupStatusBefore = CoreEnumNameMapping.ToGroupStatusCode(groupBefore.Status);
            }

            return snapshot;
        }

        /// <summary>
        /// Captures one single cast-change event after `RemoveGirl` mutates cast state.
        /// </summary>
        internal void CaptureSingleCastChanged(
            singles._single single,
            data_girls.girls removedIdol,
            SingleCastChangeSnapshot snapshot)
        {
            CaptureSingleCastChangedCore(
                single,
                removedIdol,
                snapshot,
                CoreConstants.EventSourceSingleRemoveGirlPatch);
        }

        /// <summary>
        /// Captures one single cast-change event committed from the single senbatsu popup.
        /// </summary>
        internal void CaptureSingleCastChangedFromPopup(singles._single single, SingleCastChangeSnapshot snapshot)
        {
            CaptureSingleCastChangedCore(
                single,
                null,
                snapshot,
                CoreConstants.EventSourceSinglePopupSenbatsuConfirmPatch);
        }

        /// <summary>
        /// Captures one single-group reassignment event committed from the single senbatsu popup.
        /// </summary>
        internal void CaptureSingleGroupChangedFromPopup(singles._single single, SingleCastChangeSnapshot snapshot)
        {
            if (single == null)
            {
                return;
            }

            SingleCastChangeSnapshot previousState = snapshot ?? new SingleCastChangeSnapshot();
            Groups._group groupAfter = single.GetGroup();
            int groupIdAfter = groupAfter != null ? groupAfter.ID : CoreConstants.InvalidIdValue;
            if (previousState.SingleGroupIdBefore == groupIdAfter)
            {
                return;
            }

            List<int> castIdolIdentifiersBefore = previousState.SingleCastIdolIdentifiersBefore ?? new List<int>();
            List<int> castIdolIdentifiersAfter = ResolveDistinctSingleCastIdolIdentifiers(single);
            SingleGroupChangePayload payload = BuildSingleGroupChangePayload(
                single,
                previousState,
                castIdolIdentifiersAfter,
                groupAfter);
            string eventPayloadJson = CoreJsonUtility.SerializeSingleGroupChangePayload(payload);
            List<int> impactedIdolIdentifiers = ResolveDistinctUnionIdentifiers(
                castIdolIdentifiersBefore,
                castIdolIdentifiersAfter,
                CoreConstants.InvalidIdValue);
            string singleEntityIdentifier = single.id.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (impactedIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSingle,
                        singleEntityIdentifier,
                        CoreConstants.EventTypeSingleGroupChanged,
                        CoreConstants.EventSourceSinglePopupSenbatsuConfirmPatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < impactedIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            impactedIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindSingle,
                            singleEntityIdentifier,
                            CoreConstants.EventTypeSingleGroupChanged,
                            CoreConstants.EventSourceSinglePopupSenbatsuConfirmPatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one single cast-change event for any source that mutates cast state.
        /// </summary>
        private void CaptureSingleCastChangedCore(
            singles._single single,
            data_girls.girls removedIdol,
            SingleCastChangeSnapshot snapshot,
            string sourcePatchCode)
        {
            if (single == null)
            {
                return;
            }

            SingleCastChangeSnapshot previousState = snapshot ?? new SingleCastChangeSnapshot();
            List<int> castIdolIdentifiersBefore = previousState.SingleCastIdolIdentifiersBefore ?? new List<int>();
            List<int> castIdolIdentifiersAfter = ResolveDistinctSingleCastIdolIdentifiers(single);
            List<int> addedCastIdolIdentifiers = ResolveAddedIdentifiers(castIdolIdentifiersBefore, castIdolIdentifiersAfter);
            List<int> removedCastIdolIdentifiers = ResolveRemovedIdentifiers(castIdolIdentifiersBefore, castIdolIdentifiersAfter);
            bool statusChanged = previousState.SingleStatusBefore != single.status;

            if (!statusChanged
                && addedCastIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount
                && removedCastIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            SingleCastChangePayload payload = BuildSingleCastChangePayload(
                single,
                previousState,
                castIdolIdentifiersAfter,
                addedCastIdolIdentifiers,
                removedCastIdolIdentifiers,
                removedIdol);
            string eventPayloadJson = CoreJsonUtility.SerializeSingleCastChangePayload(payload);
            List<int> impactedIdolIdentifiers = ResolveDistinctUnionIdentifiers(
                castIdolIdentifiersBefore,
                castIdolIdentifiersAfter,
                removedIdol != null ? removedIdol.id : CoreConstants.InvalidIdValue);
            string singleEntityIdentifier = single.id.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (impactedIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSingle,
                        singleEntityIdentifier,
                        CoreConstants.EventTypeSingleCastChanged,
                        sourcePatchCode,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < impactedIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            impactedIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindSingle,
                            singleEntityIdentifier,
                            CoreConstants.EventTypeSingleCastChanged,
                            sourcePatchCode,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation group state for idol-transfer hooks.
        /// </summary>
        internal GroupTransferSnapshot CreateGroupTransferSnapshot(data_girls.girls idol)
        {
            GroupTransferSnapshot snapshot = new GroupTransferSnapshot();
            if (idol == null)
            {
                return snapshot;
            }

            Groups._group fromGroup = Groups.GetGroup(idol);
            if (fromGroup == null)
            {
                return snapshot;
            }

            snapshot.FromGroupId = fromGroup.ID;
            snapshot.FromGroupTitle = fromGroup.Title ?? string.Empty;
            snapshot.FromGroupStatus = CoreEnumNameMapping.ToGroupStatusCode(fromGroup.Status);
            return snapshot;
        }

        /// <summary>
        /// Captures one idol group-transfer event.
        /// </summary>
        internal void CaptureIdolGroupTransferred(
            data_girls.girls idol,
            Groups._group toGroup,
            GroupTransferSnapshot snapshot)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            GroupTransferSnapshot previousState = snapshot ?? new GroupTransferSnapshot();
            int toGroupId = toGroup != null ? toGroup.ID : CoreConstants.InvalidIdValue;
            if (previousState.FromGroupId >= CoreConstants.MinimumValidIdolIdentifier
                && toGroupId >= CoreConstants.MinimumValidIdolIdentifier
                && previousState.FromGroupId == toGroupId)
            {
                return;
            }

            IdolGroupTransferPayload payload = BuildIdolGroupTransferPayload(idol, toGroup, previousState);
            int groupEntityId = toGroupId >= CoreConstants.MinimumValidIdolIdentifier
                ? toGroupId
                : previousState.FromGroupId;
            string groupEntityIdentifier = groupEntityId.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindGroup,
                    groupEntityIdentifier,
                    CoreConstants.EventTypeIdolGroupTransferred,
                    CoreConstants.EventSourceGroupsTransferPatch,
                    CoreJsonUtility.SerializeIdolGroupTransferPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation group state for disband hooks.
        /// </summary>
        internal GroupDisbandSnapshot CreateGroupDisbandSnapshot(Groups._group disbandedGroup)
        {
            GroupDisbandSnapshot snapshot = new GroupDisbandSnapshot();
            if (disbandedGroup == null)
            {
                return snapshot;
            }

            snapshot.GroupId = disbandedGroup.ID;
            snapshot.GroupTitle = disbandedGroup.Title ?? string.Empty;
            snapshot.GroupStatus = CoreEnumNameMapping.ToGroupStatusCode(disbandedGroup.Status);
            snapshot.GroupDateCreated = ResolveDateTimeOrEmpty(disbandedGroup.Date_Created);
            snapshot.GroupSingleCount = ResolveGroupSingleCount(disbandedGroup);
            snapshot.GroupNonReleasedSingleCount = ResolveGroupNonReleasedSingleCount(disbandedGroup);
            snapshot.GroupMemberIdolIdentifiers = ResolveDistinctGroupMemberIdolIdentifiers(disbandedGroup);
            return snapshot;
        }

        /// <summary>
        /// Captures one group-disband lifecycle event.
        /// </summary>
        internal void CaptureGroupDisbanded(Groups._group disbandedGroup, GroupDisbandSnapshot snapshot)
        {
            GroupDisbandSnapshot previousState = snapshot ?? new GroupDisbandSnapshot();
            int groupId = disbandedGroup != null
                ? disbandedGroup.ID
                : previousState.GroupId;
            if (groupId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            List<int> memberIdolIdentifiers = previousState.GroupMemberIdolIdentifiers ?? new List<int>();
            string eventDate = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime);
            GroupLifecyclePayload payload = BuildGroupLifecyclePayload(
                disbandedGroup,
                memberIdolIdentifiers,
                CoreConstants.GroupLifecycleActionDisbanded,
                eventDate,
                previousState.GroupDateCreated,
                previousState.GroupSingleCount,
                previousState.GroupNonReleasedSingleCount);
            if (string.IsNullOrEmpty(payload.GroupTitle))
            {
                payload.GroupTitle = previousState.GroupTitle ?? string.Empty;
            }
            if (string.IsNullOrEmpty(payload.GroupStatus))
            {
                payload.GroupStatus = previousState.GroupStatus ?? CoreConstants.StatusCodeUnknown;
            }

            string groupEntityIdentifier = groupId.ToString(CultureInfo.InvariantCulture);
            string eventPayloadJson = CoreJsonUtility.SerializeGroupLifecyclePayload(payload);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (memberIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindGroup,
                        groupEntityIdentifier,
                        CoreConstants.EventTypeGroupDisbanded,
                        CoreConstants.EventSourceGroupsDisbandPatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < memberIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            memberIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindGroup,
                            groupEntityIdentifier,
                            CoreConstants.EventTypeGroupDisbanded,
                            CoreConstants.EventSourceGroupsDisbandPatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation group id state for new-group creation hooks.
        /// </summary>
        internal GroupCreationSnapshot CreateGroupCreationSnapshot()
        {
            return new GroupCreationSnapshot
            {
                ExistingGroupIds = ResolveDistinctGroupIds(Groups.Groups_)
            };
        }

        /// <summary>
        /// Captures one group-created lifecycle event.
        /// </summary>
        internal void CaptureGroupCreated(GroupCreationSnapshot snapshot)
        {
            Groups._group createdGroup = ResolveCreatedGroupFromSnapshot(snapshot);
            if (createdGroup == null)
            {
                return;
            }

            List<int> memberIdolIdentifiers = ResolveDistinctGroupMemberIdolIdentifiers(createdGroup);
            GroupLifecyclePayload payload = BuildGroupLifecyclePayload(
                createdGroup,
                memberIdolIdentifiers,
                CoreConstants.GroupLifecycleActionCreated,
                CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime),
                ResolveDateTimeOrEmpty(createdGroup.Date_Created),
                ResolveGroupSingleCount(createdGroup),
                ResolveGroupNonReleasedSingleCount(createdGroup));
            string eventPayloadJson = CoreJsonUtility.SerializeGroupLifecyclePayload(payload);
            string groupEntityIdentifier = createdGroup.ID.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (memberIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindGroup,
                        groupEntityIdentifier,
                        CoreConstants.EventTypeGroupCreated,
                        CoreConstants.EventSourceNewGroupPopupOnContinuePatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < memberIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            memberIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindGroup,
                            groupEntityIdentifier,
                            CoreConstants.EventTypeGroupCreated,
                            CoreConstants.EventSourceNewGroupPopupOnContinuePatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation state for group parameter-point changes.
        /// </summary>
        internal GroupParamPointChangeSnapshot CreateGroupParamPointChangeSnapshot(
            Groups._group group,
            data_girls._paramType parameterType)
        {
            GroupParamPointChangeSnapshot snapshot = new GroupParamPointChangeSnapshot();
            if (group == null)
            {
                return snapshot;
            }

            snapshot.GroupId = group.ID;
            snapshot.GroupTitle = group.Title ?? string.Empty;
            snapshot.GroupStatus = CoreEnumNameMapping.ToGroupStatusCode(group.Status);
            snapshot.GroupSourceParamType = CoreEnumNameMapping.ToIdolParameterCode(parameterType);
            snapshot.GroupPointsBefore = ResolveGroupParameterPoints(group, parameterType);
            snapshot.GroupAvailablePointsBefore = ResolveGroupAvailableParameterPoints(group, parameterType);
            return snapshot;
        }

        /// <summary>
        /// Captures one group parameter-point mutation event.
        /// </summary>
        internal void CaptureGroupParamPointsChanged(
            Groups._group group,
            data_girls._paramType parameterType,
            int requestedPoints,
            GroupParamPointChangeSnapshot snapshot)
        {
            if (group == null)
            {
                return;
            }

            GroupParamPointChangeSnapshot previousState = snapshot ?? new GroupParamPointChangeSnapshot();
            int pointsBefore = previousState.GroupPointsBefore;
            int pointsAfter = ResolveGroupParameterPoints(group, parameterType);
            int pointsDelta = pointsAfter - pointsBefore;
            if (pointsDelta == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            GroupParamPointsPayload payload = new GroupParamPointsPayload
            {
                GroupId = group.ID,
                GroupTitle = group.Title ?? previousState.GroupTitle ?? string.Empty,
                GroupStatus = CoreEnumNameMapping.ToGroupStatusCode(group.Status),
                GroupSourceParamType = CoreEnumNameMapping.ToIdolParameterCode(parameterType),
                GroupPointsRequested = requestedPoints,
                GroupPointsBefore = pointsBefore,
                GroupPointsAfter = pointsAfter,
                GroupPointsDelta = pointsDelta,
                GroupAvailablePointsBefore = previousState.GroupAvailablePointsBefore,
                GroupAvailablePointsAfter = ResolveGroupAvailableParameterPoints(group, parameterType),
                GroupEventDate = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindGroup,
                    group.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeGroupParamPointsChanged,
                    CoreConstants.EventSourceGroupsGroupAddPointsParamPatch,
                    CoreJsonUtility.SerializeGroupParamPointsPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation state for group appeal-point spend mutations.
        /// </summary>
        internal GroupAppealPointSpendSnapshot CreateGroupAppealPointSpendSnapshot(
            Groups._group group,
            data_girls._paramType parameterType,
            resources.fanType targetFanType,
            int requestedPoints)
        {
            GroupAppealPointSpendSnapshot snapshot = new GroupAppealPointSpendSnapshot();
            if (group == null)
            {
                return snapshot;
            }

            snapshot.GroupId = group.ID;
            snapshot.GroupTitle = group.Title ?? string.Empty;
            snapshot.GroupStatus = CoreEnumNameMapping.ToGroupStatusCode(group.Status);
            snapshot.GroupSourceParamType = CoreEnumNameMapping.ToIdolParameterCode(parameterType);
            snapshot.GroupTargetFanType = CoreEnumNameMapping.ToFanTypeCode(targetFanType);
            snapshot.GroupPointsRequested = requestedPoints;
            snapshot.GroupAvailablePointsBefore = ResolveGroupAvailableParameterPoints(group, parameterType);
            snapshot.GroupPointsSpentBefore = ResolveGroupParameterSpentPoints(group, parameterType);
            snapshot.GroupTargetPointsBefore = ResolveGroupFanAppealPoints(group, targetFanType);
            return snapshot;
        }

        /// <summary>
        /// Captures one group appeal-point spend event.
        /// </summary>
        internal void CaptureGroupAppealPointsSpent(
            Groups._group group,
            data_girls._paramType parameterType,
            resources.fanType targetFanType,
            int requestedPoints,
            GroupAppealPointSpendSnapshot snapshot)
        {
            if (group == null)
            {
                return;
            }

            GroupAppealPointSpendSnapshot previousState = snapshot ?? new GroupAppealPointSpendSnapshot();
            int pointsSpentAfter = ResolveGroupParameterSpentPoints(group, parameterType);
            int appliedPoints = pointsSpentAfter - previousState.GroupPointsSpentBefore;
            if (appliedPoints <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            GroupAppealPointsSpentPayload payload = new GroupAppealPointsSpentPayload
            {
                GroupId = group.ID,
                GroupTitle = group.Title ?? previousState.GroupTitle ?? string.Empty,
                GroupStatus = CoreEnumNameMapping.ToGroupStatusCode(group.Status),
                GroupSourceParamType = CoreEnumNameMapping.ToIdolParameterCode(parameterType),
                GroupTargetFanType = CoreEnumNameMapping.ToFanTypeCode(targetFanType),
                GroupPointsRequested = requestedPoints,
                GroupPointsApplied = appliedPoints,
                GroupAvailablePointsBefore = previousState.GroupAvailablePointsBefore,
                GroupAvailablePointsAfter = ResolveGroupAvailableParameterPoints(group, parameterType),
                GroupPointsSpentBefore = previousState.GroupPointsSpentBefore,
                GroupPointsSpentAfter = pointsSpentAfter,
                GroupTargetPointsBefore = previousState.GroupTargetPointsBefore,
                GroupTargetPointsAfter = ResolveGroupFanAppealPoints(group, targetFanType),
                GroupEventDate = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindGroup,
                    group.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeGroupAppealPointsSpent,
                    CoreConstants.EventSourceGroupsGroupSpendPointsPatch,
                    CoreJsonUtility.SerializeGroupAppealPointsSpentPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one idol-hired lifecycle event.
        /// </summary>
        internal void CaptureIdolHired(data_girls.girls idol)
        {
            CaptureIdolLifecycleEvent(
                idol,
                CoreConstants.EventTypeIdolHired,
                CoreConstants.EventSourceDataGirlsHirePatch,
                CoreConstants.IdolLifecycleActionHired,
                string.Empty,
                false);
        }

        /// <summary>
        /// Captures one idol-graduation-announced lifecycle event.
        /// </summary>
        internal void CaptureIdolGraduationAnnounced(data_girls.girls idol)
        {
            CaptureIdolLifecycleEvent(
                idol,
                CoreConstants.EventTypeIdolGraduationAnnounced,
                CoreConstants.EventSourceDataGirlsGraduationAnnounceConfirmPatch,
                CoreConstants.IdolLifecycleActionGraduationAnnounced,
                string.Empty,
                false);
        }

        /// <summary>
        /// Captures one idol-graduated lifecycle event.
        /// </summary>
        internal void CaptureIdolGraduated(data_girls.girls idol, bool graduatedWithDialogue, string customTrivia)
        {
            CaptureIdolLifecycleEvent(
                idol,
                CoreConstants.EventTypeIdolGraduated,
                CoreConstants.EventSourceDataGirlsGraduatePatch,
                CoreConstants.IdolLifecycleActionGraduated,
                customTrivia,
                graduatedWithDialogue);
        }

        /// <summary>
        /// Captures pre-mutation salary state for one idol.
        /// </summary>
        internal SalaryChangeSnapshot CreateSalaryChangeSnapshot(data_girls.girls idol)
        {
            SalaryChangeSnapshot snapshot = new SalaryChangeSnapshot();
            if (idol == null)
            {
                return snapshot;
            }

            snapshot.SalaryBefore = idol.salary;
            snapshot.SalarySatisfactionBefore = idol.GetSalarySatisfaction_Percentage();
            return snapshot;
        }

        /// <summary>
        /// Captures one idol salary-change event.
        /// </summary>
        internal void CaptureIdolSalaryChanged(
            data_girls.girls idol,
            SalaryChangeSnapshot snapshot,
            string salaryActionCode,
            string sourcePatchCode)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            SalaryChangeSnapshot previousState = snapshot ?? new SalaryChangeSnapshot();
            IdolSalaryChangePayload payload = BuildIdolSalaryChangePayload(idol, previousState, salaryActionCode);
            if (payload.SalaryDelta == CoreConstants.ZeroLongValue)
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

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindIdol,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeIdolSalaryChanged,
                    sourcePatchCode,
                    CoreJsonUtility.SerializeIdolSalaryChangePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one idol lifecycle record for hire/graduation milestones.
        /// </summary>
        private void CaptureIdolLifecycleEvent(
            data_girls.girls idol,
            string eventTypeCode,
            string sourcePatchCode,
            string lifecycleActionCode,
            string customTrivia,
            bool graduatedWithDialogue)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            IdolLifecyclePayload payload = BuildIdolLifecyclePayload(idol, lifecycleActionCode, customTrivia, graduatedWithDialogue);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindIdol,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    eventTypeCode,
                    sourcePatchCode,
                    CoreJsonUtility.SerializeIdolLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one idol status transition and updates status-window projections.
        /// </summary>
        internal void CaptureStatusTransition(data_girls.girls idol, data_girls._status previousStatus, data_girls._status newStatus)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
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

                string previousStatusCode = CoreStatusMapping.ToStatusCode(previousStatus);
                string newStatusCode = CoreStatusMapping.ToStatusCode(newStatus);
                if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
                {
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string transitionDate = CoreDateTimeUtility.ToRoundTripString(gameDate);
                string idolEntityIdentifier = idol.id.ToString(CultureInfo.InvariantCulture);

                StatusTransitionPayload payload = new StatusTransitionPayload
                {
                    IdolId = idol.id,
                    PreviousStatus = previousStatusCode,
                    NewStatus = newStatusCode
                };
                string eventPayloadJson = CoreJsonUtility.SerializeStatusTransitionPayload(payload);

                PendingEvent pendingEvent = new PendingEvent
                {
                    SaveKey = activeSaveKey,
                    GameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate),
                    GameDateTime = transitionDate,
                    IdolId = idol.id,
                    EntityKind = CoreConstants.EventEntityKindStatus,
                    EntityId = idolEntityIdentifier,
                    EventType = CoreConstants.EventTypeStatusChanged,
                    SourcePatch = CoreConstants.EventSourceStatusTransitionPatch,
                    PayloadJson = eventPayloadJson
                };

                StatusTransitionProjection transition = new StatusTransitionProjection
                {
                    SaveKey = activeSaveKey,
                    IdolId = idol.id,
                    PreviousStatusCode = previousStatusCode,
                    NewStatusCode = newStatusCode,
                    TransitionDate = transitionDate
                };

                bufferedEvents.Add(pendingEvent);
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindStatus,
                    idolEntityIdentifier,
                    CoreConstants.EventTypeStatusChangedLegacy,
                    CoreConstants.EventSourceStatusTransitionPatch,
                    eventPayloadJson);

                if (CoreConstants.StatusCodesTrackedAsWindows.Contains(previousStatusCode))
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        idol.id,
                        CoreConstants.EventEntityKindStatus,
                        idolEntityIdentifier,
                        CoreConstants.EventTypeStatusEnded,
                        CoreConstants.EventSourceStatusTransitionPatch,
                        eventPayloadJson);
                }

                if (CoreConstants.StatusCodesTrackedAsWindows.Contains(newStatusCode))
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        idol.id,
                        CoreConstants.EventEntityKindStatus,
                        idolEntityIdentifier,
                        CoreConstants.EventTypeStatusStarted,
                        CoreConstants.EventSourceStatusTransitionPatch,
                        eventPayloadJson);
                }

                bufferedStatusTransitions.Add(transition);

                if (!FlushLocked(false, out errorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageFlushFailed + errorMessage);
                }
            }
        }

        /// <summary>
        /// Captures producer-dating status transitions for one partner.
        /// </summary>
        internal void CaptureDatingPartnerStatusTransition(Dating._partner partner, Dating._partner._status previousStatus, Dating._partner._status newStatus)
        {
            if (partner == null || partner.GirlID < CoreConstants.MinimumValidIdolIdentifier)
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

                string previousStatusCode = CoreEnumNameMapping.ToDatingPartnerStatusCode(previousStatus);
                string newStatusCode = CoreEnumNameMapping.ToDatingPartnerStatusCode(newStatus);
                if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
                {
                    return;
                }

                DatingPartnerStatusPayload payload = new DatingPartnerStatusPayload
                {
                    IdolId = partner.GirlID,
                    PreviousStatus = previousStatusCode,
                    NewStatus = newStatusCode,
                    DatingRoute = CoreEnumNameMapping.ToDatingPartnerRouteCode(partner.Route),
                    DatingRouteStage = CoreEnumNameMapping.ToDatingPartnerRouteStageCode(partner.Progress)
                };

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    partner.GirlID,
                    CoreConstants.EventEntityKindDatingRelationship,
                    partner.GirlID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeDatingPartnerStatusChanged,
                    CoreConstants.EventSourceDatingPartnerStatusPatch,
                    CoreJsonUtility.SerializeDatingPartnerStatusPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures idol dating partner-status transitions from `data_girls`.
        /// </summary>
        internal void CaptureIdolDatingStatusTransition(
            data_girls.girls._dating_data datingData,
            data_girls.girls._dating_data._partner_status previousStatus,
            data_girls.girls._dating_data._partner_status newStatus)
        {
            if (datingData == null)
            {
                return;
            }

            int idolId = ResolveIdolIdentifierFromDatingData(datingData);
            if (idolId < CoreConstants.MinimumValidIdolIdentifier)
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

                string previousStatusCode = CoreEnumNameMapping.ToIdolDatingPartnerStatusCode(previousStatus);
                string newStatusCode = CoreEnumNameMapping.ToIdolDatingPartnerStatusCode(newStatus);
                if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
                {
                    return;
                }

                IdolDatingStatusPayload payload = new IdolDatingStatusPayload
                {
                    IdolId = idolId,
                    PreviousPartnerStatus = previousStatusCode,
                    NewPartnerStatus = newStatusCode,
                    HadScandal = datingData.Had_Scandal,
                    HadScandalEver = datingData.Had_Scandal_Ever,
                    UsedGoods = datingData.Used_Goods,
                    DatedIdol = datingData.Dated_Idol
                };

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idolId,
                    CoreConstants.EventEntityKindIdolDatingState,
                    idolId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeIdolDatingStatusChanged,
                    CoreConstants.EventSourceIdolDatingStatusPatch,
                    CoreJsonUtility.SerializeIdolDatingStatusPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures proposal acceptance snapshot state before business mutation runs.
        /// </summary>
        internal ContractAcceptedSnapshot CreateContractAcceptedSnapshot(business businessSystem)
        {
            ContractAcceptedSnapshot snapshot = new ContractAcceptedSnapshot();
            if (businessSystem == null || businessSystem.ActiveProposal == null)
            {
                return snapshot;
            }

            business._proposal proposalSnapshot = businessSystem.ActiveProposal.Clone();
            if (proposalSnapshot == null)
            {
                return snapshot;
            }

            snapshot.AcceptedProposal = proposalSnapshot;
            snapshot.AcceptedDate = staticVars.dateTime;
            snapshot.TargetIdolIdentifiers = ResolveProposalTargetIdolIdentifiers(proposalSnapshot);
            return snapshot;
        }

    }
}
