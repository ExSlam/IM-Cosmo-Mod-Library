using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Settled-state observer for show history.
    ///
    /// IMDC does not predict vanilla or another mod's cast decision. It samples the
    /// authoritative runtime after gameplay/fix patches have finished, remembers only
    /// a transient baseline, and emits history when the settled durable configuration
    /// actually changes. Rotating/entire-group weekly participants are episode facts,
    /// not durable configuration mutations.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private readonly Dictionary<int, PostModShowConfigurationSnapshot>
            postModShowConfigurationByShowId =
                new Dictionary<int, PostModShowConfigurationSnapshot>();
        private int postModShowSettlementDepth;

        internal void ResetPostModShowObservation()
        {
            lock (runtimeLock)
            {
                postModShowConfigurationByShowId.Clear();
                postModShowSettlementDepth = 0;
            }
        }

        internal void BeginPostModShowSettlement()
        {
            lock (runtimeLock)
            {
                postModShowSettlementDepth++;
            }
        }

        internal bool EndPostModShowSettlement()
        {
            lock (runtimeLock)
            {
                if (postModShowSettlementDepth <= 0)
                {
                    postModShowSettlementDepth = 0;
                    return true;
                }

                postModShowSettlementDepth--;
                return postModShowSettlementDepth == 0;
            }
        }

        internal void SeedPostModShowObservationAfterLoad()
        {
            lock (runtimeLock)
            {
                SeedPostModShowObservationLocked();
            }
        }

        internal void ReconcileAllPostModShows(string sourcePatch)
        {
            lock (runtimeLock)
            {
                string errorMessage = string.Empty;
                if (saveLoadPreparationActive ||
                    postModShowSettlementDepth > 0 ||
                    !EnsureInitializedLocked(out errorMessage))
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        CoreLog.Warn(errorMessage);
                    }
                    return;
                }

                if (Shows.shows == null)
                {
                    return;
                }

                HashSet<int> liveShowIds = new HashSet<int>();
                for (int index = 0; index < Shows.shows.Count; index++)
                {
                    Shows._show show = Shows.shows[index];
                    if (show == null || show.id < 0)
                    {
                        continue;
                    }

                    liveShowIds.Add(show.id);
                    ReconcilePostModShowLocked(show, sourcePatch);
                }

                List<int> staleIds = new List<int>();
                foreach (int showId in postModShowConfigurationByShowId.Keys)
                {
                    if (!liveShowIds.Contains(showId))
                    {
                        staleIds.Add(showId);
                    }
                }
                for (int index = 0; index < staleIds.Count; index++)
                {
                    postModShowConfigurationByShowId.Remove(staleIds[index]);
                }
            }
        }

        internal void ObservePostModShowMutation(
            Shows._show show,
            string sourcePatch)
        {
            lock (runtimeLock)
            {
                if (show == null ||
                    saveLoadPreparationActive ||
                    postModShowSettlementDepth > 0)
                {
                    return;
                }

                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                ReconcilePostModShowLocked(show, sourcePatch);
                FlushAfterCaptureLocked();
            }
        }

        internal void ObservePostModShowEpisode(Shows._show show)
        {
            lock (runtimeLock)
            {
                if (show == null ||
                    saveLoadPreparationActive ||
                    postModShowSettlementDepth > 0)
                {
                    return;
                }

                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                // Any durable cast mutation performed by vanilla or another mod
                // during this episode is recorded before the episode fact itself.
                ReconcilePostModShowLocked(
                    show,
                    CorePayloadCompaction.CanonicalShowCastSourcePrefix +
                        "episode");

                CaptureCanonicalPostModShowEpisodeLocked(show);
                FlushAfterCaptureLocked();
            }
        }

        private void SeedPostModShowObservationLocked()
        {
            postModShowConfigurationByShowId.Clear();
            postModShowSettlementDepth = 0;
            if (Shows.shows == null)
            {
                return;
            }

            for (int index = 0; index < Shows.shows.Count; index++)
            {
                Shows._show show = Shows.shows[index];
                if (show == null || show.id < 0)
                {
                    continue;
                }

                postModShowConfigurationByShowId[show.id] =
                    CapturePostModShowConfiguration(show);
            }
        }

        private void ReconcilePostModShowLocked(
            Shows._show show,
            string sourcePatch)
        {
            if (show == null || show.id < 0)
            {
                return;
            }

            PostModShowConfigurationSnapshot current =
                CapturePostModShowConfiguration(show);
            PostModShowConfigurationSnapshot previous;
            if (!postModShowConfigurationByShowId.TryGetValue(
                    show.id,
                    out previous))
            {
                // Creation/lifecycle captures already describe the initial state.
                // The observer baseline is transient and must not manufacture a
                // historical transition merely because it first saw the show now.
                postModShowConfigurationByShowId[show.id] = current;
                return;
            }

            if (!HasDurableShowCastConfigurationChanged(previous, current))
            {
                postModShowConfigurationByShowId[show.id] = current;
                return;
            }

            // Permanent-cast slot configuration is the durable member state.
            // For other modes, a cast-mode transition has no durable member slots,
            // so use the settled effective cast only to make the transition useful
            // to per-idol timeline indexing. Weekly rotating/entire-group changes
            // still do not trigger configuration events by themselves.
            List<int> beforeIds = previous.CastType ==
                Shows._show._castType.permanentCast
                    ? GetValidIds(previous.PermanentSlotIds)
                    : new List<int>(previous.EffectiveCastIds);
            List<int> afterIds = current.CastType ==
                Shows._show._castType.permanentCast
                    ? GetValidIds(current.PermanentSlotIds)
                    : new List<int>(current.EffectiveCastIds);
            List<int> addedIds = Difference(afterIds, beforeIds);
            List<int> removedIds = Difference(beforeIds, afterIds);

            ShowCastChangePayload payload = new ShowCastChangePayload
            {
                ShowTitle = show.title ?? string.Empty,
                PreviousShowStatus =
                    CoreEnumNameMapping.ToShowStatusCode(previous.Status),
                NewShowStatus =
                    CoreEnumNameMapping.ToShowStatusCode(current.Status),
                ShowCastTypeBefore =
                    CoreEnumNameMapping.ToShowCastTypeCode(previous.CastType),
                ShowCastTypeAfter =
                    CoreEnumNameMapping.ToShowCastTypeCode(current.CastType),
                ShowCastCountBefore = beforeIds.Count,
                ShowCastCountAfter = afterIds.Count,
                ShowCastIdListBefore = BuildDelimitedIdentifierList(beforeIds),
                ShowCastIdListAfter = BuildDelimitedIdentifierList(afterIds),
                ShowCastIdListAdded = BuildDelimitedIdentifierList(addedIds),
                ShowCastIdListRemoved = BuildDelimitedIdentifierList(removedIds),
                ShowRemovedIdolId =
                    removedIds.Count == 1
                        ? removedIds[0]
                        : CoreConstants.InvalidIdValue
            };

            DateTime gameDate = staticVars.dateTime;
            EnqueueEventRecordLocked(
                gameDate,
                CoreConstants.InvalidIdValue,
                CoreConstants.EventEntityKindShow,
                show.id.ToString(CultureInfo.InvariantCulture),
                CoreConstants.EventTypeShowCastChanged,
                string.IsNullOrEmpty(sourcePatch)
                    ? CorePayloadCompaction.CanonicalShowCastSourcePrefix +
                        "reconcile"
                    : sourcePatch,
                CoreJsonUtility.SerializeShowCastChangePayload(payload));

            postModShowConfigurationByShowId[show.id] = current;
        }

        private void CaptureCanonicalPostModShowEpisodeLocked(Shows._show show)
        {
            if (show == null)
            {
                return;
            }

            List<int> participantIds =
                ResolveDistinctShowCastIdolIdentifiers(show);

            long previousAudience = ResolvePreviousLongMetric(show.audience);
            long latestAudience = ResolveLatestLongMetric(show.audience);
            long previousRevenue = ResolvePreviousLongMetric(show.revenue);
            long latestRevenue = ResolveLatestLongMetric(show.revenue);
            int previousFans = ResolvePreviousIntMetric(show.fans);
            int latestFans = ResolveLatestIntMetric(show.fans);
            int previousBuzz = ResolvePreviousIntMetric(show.buzz);
            int latestBuzz = ResolveLatestIntMetric(show.buzz);
            float previousFatigue = ResolvePreviousFloatMetric(show.fatigue);
            float latestFatigue = ResolveLatestFloatMetric(show.fatigue);
            int previousFame = ResolvePreviousIntMetric(show.fame);
            int latestFame = ResolveLatestIntMetric(show.fame);
            int previousFamePoints = ResolvePreviousIntMetric(show.famePoints);
            int latestFamePoints = ResolveLatestIntMetric(show.famePoints);
            long previousProfit = previousRevenue - show.cost;
            long latestProfit = latestRevenue - show.cost;

            ShowEpisodePayload payload = new ShowEpisodePayload
            {
                ShowTitle = show.title ?? string.Empty,
                ShowCastType =
                    CoreEnumNameMapping.ToShowCastTypeCode(show.castType),
                ShowEpisodeCount = show.episodeCount,
                ShowEpisodeDate =
                    CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime),
                ShowCastCount = participantIds.Count,
                ShowCastIdList = BuildDelimitedIdentifierList(participantIds),
                ShowPreviousAudience = previousAudience,
                ShowLatestAudience = latestAudience,
                ShowAudienceDelta = latestAudience - previousAudience,
                ShowPreviousRevenue = previousRevenue,
                ShowLatestRevenue = latestRevenue,
                ShowRevenueDelta = latestRevenue - previousRevenue,
                ShowPreviousProfit = previousProfit,
                ShowLatestProfit = latestProfit,
                ShowProfitDelta = latestProfit - previousProfit,
                ShowPreviousNewFans = previousFans,
                ShowLatestNewFans = latestFans,
                ShowNewFansDelta = latestFans - previousFans,
                ShowPreviousBuzz = previousBuzz,
                ShowLatestBuzz = latestBuzz,
                ShowBuzzDelta = latestBuzz - previousBuzz,
                ShowPreviousFatigue = previousFatigue,
                ShowLatestFatigue = latestFatigue,
                ShowFatigueDelta = latestFatigue - previousFatigue,
                ShowPreviousFame = previousFame,
                ShowLatestFame = latestFame,
                ShowFameDelta = latestFame - previousFame,
                ShowPreviousFamePoints = previousFamePoints,
                ShowLatestFamePoints = latestFamePoints,
                ShowFamePointsDelta = latestFamePoints - previousFamePoints,
                ShowEpisodeBudget = show.cost,
                ShowStaminaCost = show.GetStaminaCost()
            };

            EnqueueEventRecordLocked(
                staticVars.dateTime,
                CoreConstants.InvalidIdValue,
                CoreConstants.EventEntityKindShow,
                show.id.ToString(CultureInfo.InvariantCulture),
                CoreConstants.EventTypeShowEpisodeReleased,
                CorePayloadCompaction.CanonicalShowEpisodeSource,
                CoreJsonUtility.SerializeShowEpisodePayload(payload));
        }

        private static PostModShowConfigurationSnapshot
            CapturePostModShowConfiguration(Shows._show show)
        {
            PostModShowConfigurationSnapshot snapshot =
                new PostModShowConfigurationSnapshot
                {
                    ShowId = show == null
                        ? CoreConstants.InvalidIdValue
                        : show.id,
                    CastType = show == null
                        ? default(Shows._show._castType)
                        : show.castType,
                    Status = show == null
                        ? default(Shows._show._status)
                        : show.status
                };

            // Only permanent-cast slots are durable member configuration.
            // Rotating and entire-group members are episode-time participants.
            if (show != null &&
                show.castType == Shows._show._castType.permanentCast &&
                show.girls != null)
            {
                for (int index = 0; index < show.girls.Length; index++)
                {
                    data_girls.girls idol = show.girls[index];
                    snapshot.PermanentSlotIds.Add(
                        idol != null &&
                        idol.id >= CoreConstants.MinimumValidIdolIdentifier
                            ? idol.id
                            : CoreConstants.InvalidIdValue);
                }
            }

            if (show != null)
            {
                List<data_girls.girls> effectiveCast = show.GetCast();
                HashSet<int> emitted = new HashSet<int>();
                if (effectiveCast != null)
                {
                    for (int index = 0; index < effectiveCast.Count; index++)
                    {
                        data_girls.girls idol = effectiveCast[index];
                        if (idol != null &&
                            idol.id >= CoreConstants.MinimumValidIdolIdentifier &&
                            emitted.Add(idol.id))
                        {
                            snapshot.EffectiveCastIds.Add(idol.id);
                        }
                    }
                }
            }

            return snapshot;
        }

        private static bool HasDurableShowCastConfigurationChanged(
            PostModShowConfigurationSnapshot previous,
            PostModShowConfigurationSnapshot current)
        {
            if (previous == null || current == null)
            {
                return false;
            }
            if (previous.CastType != current.CastType)
            {
                return true;
            }
            if (previous.PermanentSlotIds.Count != current.PermanentSlotIds.Count)
            {
                return true;
            }
            for (int index = 0; index < previous.PermanentSlotIds.Count; index++)
            {
                if (previous.PermanentSlotIds[index] !=
                    current.PermanentSlotIds[index])
                {
                    return true;
                }
            }
            return false;
        }

        private static List<int> GetValidIds(IReadOnlyList<int> source)
        {
            List<int> result = new List<int>();
            if (source == null)
            {
                return result;
            }
            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] >= CoreConstants.MinimumValidIdolIdentifier)
                {
                    result.Add(source[index]);
                }
            }
            return result;
        }

        private static List<int> Difference(
            IReadOnlyList<int> left,
            IReadOnlyList<int> right)
        {
            HashSet<int> rightSet = new HashSet<int>();
            if (right != null)
            {
                for (int index = 0; index < right.Count; index++)
                {
                    rightSet.Add(right[index]);
                }
            }

            List<int> result = new List<int>();
            HashSet<int> emitted = new HashSet<int>();
            if (left != null)
            {
                for (int index = 0; index < left.Count; index++)
                {
                    int value = left[index];
                    if (!rightSet.Contains(value) && emitted.Add(value))
                    {
                        result.Add(value);
                    }
                }
            }
            return result;
        }
    }

    internal sealed class PostModShowConfigurationSnapshot
    {
        internal int ShowId = CoreConstants.InvalidIdValue;
        internal Shows._show._castType CastType;
        internal Shows._show._status Status;
        internal readonly List<int> PermanentSlotIds = new List<int>();
        internal readonly List<int> EffectiveCastIds = new List<int>();
    }
}
