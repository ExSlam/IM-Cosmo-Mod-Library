using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Wave-2 Task 3 historical capture for clique birth, player anti-bullying
    /// interventions, and sparse per-rival-group monthly turnover.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        /// <summary>
        /// Captures the one clique appended by the authoritative private
        /// Relationships.StartNewClique(...) birth seam.
        /// </summary>
        internal void CaptureCliqueCreated(
            int previousCliqueCount,
            data_girls.girls foundingMember)
        {
            Relationships._clique createdClique = ResolveCreatedClique(
                previousCliqueCount,
                foundingMember);
            if (createdClique == null ||
                foundingMember == null ||
                foundingMember.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            int leaderId = ResolveCliqueLeaderIdOrInvalid(createdClique);
            string cliqueSignature = BuildCliqueSignature(createdClique);
            CliqueLifecyclePayload payload = new CliqueLifecyclePayload
            {
                IdolId = foundingMember.id,
                CliqueLeaderId = leaderId,
                CliqueLeaderIdBefore = CoreConstants.InvalidIdValue,
                CliqueLeaderIdAfter = leaderId,
                CliqueMemberCount = createdClique.Members != null
                    ? createdClique.Members.Count
                    : CoreConstants.ZeroBasedListStartIndex,
                CliqueSignature = cliqueSignature,
                CliqueQuitWasViolent = false
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string cliqueEntityIdentifier =
                    ResolveCliqueHistoryEntityIdentifierLocked(
                        createdClique,
                        cliqueSignature);
                if (CanonicalCliqueIdentityRuntimeEnabled &&
                    !string.IsNullOrEmpty(cliqueEntityIdentifier))
                {
                    payload.CliqueGenerationId = cliqueEntityIdentifier;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    foundingMember.id,
                    CoreConstants.EventEntityKindClique,
                    cliqueEntityIdentifier,
                    CoreConstants.EventTypeCliqueCreated,
                    CoreConstants.EventSourceRelationshipsStartNewCliquePatch,
                    CoreJsonUtility.SerializeCliqueLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Resolves exactly one newly appended clique from the birth seam. Refuses
        /// to guess if a modded nested mutation appended multiple plausible rows.
        /// </summary>
        private static Relationships._clique ResolveCreatedClique(
            int previousCliqueCount,
            data_girls.girls foundingMember)
        {
            if (Relationships.Cliques == null ||
                foundingMember == null ||
                Relationships.Cliques.Count <= previousCliqueCount)
            {
                return null;
            }

            int startIndex = previousCliqueCount < CoreConstants.ZeroBasedListStartIndex
                ? CoreConstants.ZeroBasedListStartIndex
                : previousCliqueCount;
            Relationships._clique createdClique = null;
            for (int index = startIndex; index < Relationships.Cliques.Count; index++)
            {
                Relationships._clique candidate = Relationships.Cliques[index];
                if (candidate == null ||
                    candidate.Members == null ||
                    !candidate.Members.Contains(foundingMember))
                {
                    continue;
                }

                if (createdClique != null)
                {
                    return null;
                }
                createdClique = candidate;
            }

            return createdClique;
        }

        /// <summary>
        /// Captures immutable pre-mutation context for Date_Influence.Bullying_Text.
        /// No dialogue generator, chance helper, or relationship mutation is called.
        /// </summary>
        internal PlayerBullyingInterventionSnapshot
            CreatePlayerBullyingInterventionSnapshot(
                data_girls.girls actingIdol,
                data_girls.girls targetIdol)
        {
            PlayerBullyingInterventionSnapshot snapshot =
                new PlayerBullyingInterventionSnapshot
                {
                    ActingIdol = actingIdol,
                    TargetIdol = targetIdol,
                    ActingIdolId = actingIdol != null
                        ? actingIdol.id
                        : CoreConstants.InvalidIdValue,
                    TargetIdolId = targetIdol != null
                        ? targetIdol.id
                        : CoreConstants.InvalidIdValue
                };

            if (actingIdol == null ||
                targetIdol == null ||
                actingIdol.id < CoreConstants.MinimumValidIdolIdentifier ||
                targetIdol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return snapshot;
            }

            Relationships._clique clique = actingIdol.GetClique();
            snapshot.Clique = clique;
            if (clique == null ||
                clique.Members == null ||
                !clique.Members.Contains(actingIdol) ||
                !clique.IsBullied(targetIdol))
            {
                return snapshot;
            }

            snapshot.WasValidBefore = true;
            snapshot.LeaderIdBefore = ResolveCliqueLeaderIdOrInvalid(clique);
            snapshot.CliqueSignatureBefore = BuildCliqueSignature(clique);
            snapshot.MemberIdsBefore = CaptureCliqueMemberIds(clique);
            snapshot.StoppedMemberIdsBefore =
                CaptureEffectiveStoppedMemberIds(clique, targetIdol, true);
            snapshot.BullyingStateBefore =
                CreateBullyingStopSnapshot(clique, targetIdol);
            snapshot.InfluenceCostLevel = Date_Influence.Bullying_Cost(actingIdol);
            snapshot.InfluencePointCost = Relationships_Player.GetPointsByLevel(
                snapshot.InfluenceCostLevel);
            return snapshot;
        }

        /// <summary>
        /// Emits one semantic player intervention only when the authoritative
        /// method actually changes the set of bullies or ends the episode.
        /// </summary>
        internal void CapturePlayerBullyingIntervention(
            PlayerBullyingInterventionSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.WasValidBefore ||
                snapshot.Clique == null ||
                snapshot.TargetIdol == null ||
                snapshot.ActingIdolId < CoreConstants.MinimumValidIdolIdentifier ||
                snapshot.TargetIdolId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            bool bullyingActiveAfter = snapshot.Clique.IsBullied(snapshot.TargetIdol);
            List<int> stoppedAfter = bullyingActiveAfter
                ? CaptureEffectiveStoppedMemberIds(
                    snapshot.Clique,
                    snapshot.TargetIdol,
                    true)
                : new List<int>(snapshot.MemberIdsBefore);
            List<int> newlyStopped = ExceptIds(
                stoppedAfter,
                snapshot.StoppedMemberIdsBefore);
            bool fullStop = !bullyingActiveAfter;
            if (!fullStop && newlyStopped.Count == 0)
            {
                return;
            }

            int leaderIdAfter = ResolveCliqueLeaderIdOrInvalid(snapshot.Clique);
            PlayerBullyingInterventionPayload payload =
                new PlayerBullyingInterventionPayload
                {
                    acting_idol_id = snapshot.ActingIdolId,
                    target_idol_id = snapshot.TargetIdolId,
                    clique_leader_id_before = snapshot.LeaderIdBefore,
                    clique_leader_id_after = leaderIdAfter,
                    clique_signature = snapshot.CliqueSignatureBefore ?? string.Empty,
                    clique_member_count = snapshot.Clique.Members != null
                        ? snapshot.Clique.Members.Count
                        : CoreConstants.ZeroBasedListStartIndex,
                    stopped_member_ids_before = new List<int>(
                        snapshot.StoppedMemberIdsBefore),
                    stopped_member_ids_after = stoppedAfter,
                    newly_stopped_member_ids = newlyStopped,
                    bullying_active_before = true,
                    bullying_active_after = bullyingActiveAfter,
                    result = fullStop
                        ? CoreConstants.BullyingInterventionResultFullStop
                        : CoreConstants.BullyingInterventionResultPartialStop,
                    influence_cost_level = snapshot.InfluenceCostLevel,
                    influence_point_cost = snapshot.InfluencePointCost,
                    event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string parentCliqueGenerationId;
                string bullyingEntityIdentifier =
                    ResolveBullyingEndedEntityIdentifierLocked(
                        snapshot.Clique,
                        snapshot.BullyingStateBefore,
                        out parentCliqueGenerationId);
                payload.bullying_episode_id =
                    snapshot.BullyingStateBefore != null
                        ? snapshot.BullyingStateBefore.BullyingEpisodeId ?? string.Empty
                        : string.Empty;
                payload.clique_generation_id = parentCliqueGenerationId ?? string.Empty;

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    snapshot.TargetIdolId,
                    CoreConstants.EventEntityKindBullying,
                    bullyingEntityIdentifier,
                    CoreConstants.EventTypePlayerBullyingIntervention,
                    CoreConstants.EventSourceDateInfluenceBullyingTextPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        private static List<int> CaptureCliqueMemberIds(Relationships._clique clique)
        {
            List<int> result = new List<int>();
            if (clique == null || clique.Members == null)
            {
                return result;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < clique.Members.Count;
                index++)
            {
                data_girls.girls member = clique.Members[index];
                if (member != null &&
                    member.id >= CoreConstants.MinimumValidIdolIdentifier &&
                    !result.Contains(member.id))
                {
                    result.Add(member.id);
                }
            }
            result.Sort();
            return result;
        }

        /// <summary>
        /// Returns the semantic set of clique members no longer bullying Target.
        /// Once the whole bullying episode ends, every current member is stopped
        /// even though vanilla removes its temporary StoppedBullying row.
        /// </summary>
        private static List<int> CaptureEffectiveStoppedMemberIds(
            Relationships._clique clique,
            data_girls.girls target,
            bool bullyingActive)
        {
            if (!bullyingActive)
            {
                return CaptureCliqueMemberIds(clique);
            }

            List<int> result = new List<int>();
            if (clique == null || target == null)
            {
                return result;
            }

            Relationships._clique._stopped_bullying stopped =
                clique.GetStoppedBullying(target);
            if (stopped == null || stopped.Girls == null)
            {
                return result;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < stopped.Girls.Count;
                index++)
            {
                data_girls.girls member = stopped.Girls[index];
                if (member != null &&
                    member.id >= CoreConstants.MinimumValidIdolIdentifier &&
                    !result.Contains(member.id))
                {
                    result.Add(member.id);
                }
            }
            result.Sort();
            return result;
        }

        private static List<int> ExceptIds(List<int> after, List<int> before)
        {
            List<int> result = new List<int>();
            if (after == null)
            {
                return result;
            }
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < after.Count;
                index++)
            {
                int value = after[index];
                if ((before == null || !before.Contains(value)) &&
                    !result.Contains(value))
                {
                    result.Add(value);
                }
            }
            result.Sort();
            return result;
        }

        /// <summary>
        /// Captures exact object-reference state before the monthly rival mutation.
        /// </summary>
        private static List<RivalGroupStateSnapshot> CaptureRivalGroupStates(
            IReadOnlyList<Rivals._group> groups)
        {
            List<RivalGroupStateSnapshot> result =
                new List<RivalGroupStateSnapshot>();
            if (groups == null)
            {
                return result;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < groups.Count;
                index++)
            {
                Rivals._group group = groups[index];
                if (group != null)
                {
                    result.Add(CreateRivalGroupStateSnapshot(group));
                }
            }
            return result;
        }

        private static RivalGroupStateSnapshot CreateRivalGroupStateSnapshot(
            Rivals._group group)
        {
            RivalGroupStateSnapshot snapshot = new RivalGroupStateSnapshot();
            if (group == null)
            {
                return snapshot;
            }

            snapshot.GroupReference = group;
            snapshot.GroupId = group.ID;
            snapshot.GroupName = group.GroupName ?? string.Empty;
            snapshot.Fans = group.Fans;
            snapshot.IsRising = group.IsRising;
            snapshot.IsRival = group.IsRival;
            snapshot.IsPhantasm = group.IsPhantasm;
            snapshot.IsDead = group.IsDead;
            snapshot.IsGenreFixed = group.IsGenreFixed;
            snapshot.GenreId = group.Genre != null
                ? group.Genre.id
                : CoreConstants.InvalidIdValue;
            snapshot.SingleCount = group.Singles != null
                ? group.Singles.Count
                : CoreConstants.ZeroBasedListStartIndex;
            return snapshot;
        }

        /// <summary>
        /// Emits per-group creation/retirement rows from the monthly exact
        /// before/after diff. Called with runtimeLock held and intentionally does
        /// not flush; the parent monthly capture performs one shared flush.
        /// </summary>
        private void CaptureRivalGroupLifecycleRowsLocked(
            RivalMarketSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            List<RivalGroupStateSnapshot> before =
                snapshotBefore.GroupStatesBefore ??
                new List<RivalGroupStateSnapshot>();
            IReadOnlyList<Rivals._group> after = Rivals.Groups;
            if (after == null)
            {
                return;
            }

            // Retirement is an alive -> dead transition on the exact same object.
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < before.Count;
                index++)
            {
                RivalGroupStateSnapshot stateBefore = before[index];
                if (stateBefore == null ||
                    stateBefore.GroupReference == null ||
                    stateBefore.IsDead ||
                    !ContainsRivalGroupReference(after, stateBefore.GroupReference) ||
                    !stateBefore.GroupReference.IsDead)
                {
                    continue;
                }

                EnqueueRivalGroupLifecycleRowLocked(
                    CoreConstants.EventTypeRivalGroupRetired,
                    CoreConstants.RivalGroupLifecycleActionRetired,
                    stateBefore,
                    CreateRivalGroupStateSnapshot(stateBefore.GroupReference));
            }

            // Birth is an object reference absent before and present after. This
            // is immune to SortGroups() reorder and does not label baseline groups.
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < after.Count;
                index++)
            {
                Rivals._group group = after[index];
                if (group == null || WasRivalGroupPresentBefore(before, group))
                {
                    continue;
                }

                EnqueueRivalGroupLifecycleRowLocked(
                    CoreConstants.EventTypeRivalGroupCreated,
                    CoreConstants.RivalGroupLifecycleActionCreated,
                    null,
                    CreateRivalGroupStateSnapshot(group));
            }
        }

        private static bool ContainsRivalGroupReference(
            IReadOnlyList<Rivals._group> groups,
            Rivals._group target)
        {
            if (groups == null || target == null)
            {
                return false;
            }
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < groups.Count;
                index++)
            {
                if (object.ReferenceEquals(groups[index], target))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool WasRivalGroupPresentBefore(
            List<RivalGroupStateSnapshot> before,
            Rivals._group target)
        {
            if (before == null || target == null)
            {
                return false;
            }
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < before.Count;
                index++)
            {
                RivalGroupStateSnapshot state = before[index];
                if (state != null &&
                    object.ReferenceEquals(state.GroupReference, target))
                {
                    return true;
                }
            }
            return false;
        }

        private void EnqueueRivalGroupLifecycleRowLocked(
            string eventType,
            string action,
            RivalGroupStateSnapshot before,
            RivalGroupStateSnapshot after)
        {
            RivalGroupStateSnapshot identityState = after ?? before;
            if (identityState == null || identityState.GroupId < 0)
            {
                return;
            }

            RivalGroupLifecycleEventPayload payload =
                new RivalGroupLifecycleEventPayload
                {
                    rival_group_action = action ?? string.Empty,
                    rival_group_id = identityState.GroupId,
                    rival_group_name = identityState.GroupName ?? string.Empty,
                    present_before = before != null,
                    present_after = after != null,
                    fans_before = before != null ? before.Fans : 0L,
                    fans_after = after != null ? after.Fans : 0L,
                    is_rising_before = before != null && before.IsRising,
                    is_rising_after = after != null && after.IsRising,
                    is_dead_before = before != null && before.IsDead,
                    is_dead_after = after != null && after.IsDead,
                    is_rival = identityState.IsRival,
                    is_phantasm = identityState.IsPhantasm,
                    is_genre_fixed = identityState.IsGenreFixed,
                    genre_id = identityState.GenreId,
                    single_count_before = before != null
                        ? before.SingleCount
                        : CoreConstants.ZeroBasedListStartIndex,
                    single_count_after = after != null
                        ? after.SingleCount
                        : CoreConstants.ZeroBasedListStartIndex,
                    event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                };

            EnqueueEventRecordLocked(
                staticVars.dateTime,
                CoreConstants.InvalidIdValue,
                CoreConstants.EventEntityKindRivalGroup,
                identityState.GroupId.ToString(CultureInfo.InvariantCulture),
                eventType,
                CoreConstants.EventSourceRivalsOnNewMonthPatch,
                CoreJsonUtility.SerializeObjectPayload(payload));
        }
    }

    internal sealed class PlayerBullyingInterventionSnapshot
    {
        internal data_girls.girls ActingIdol;
        internal data_girls.girls TargetIdol;
        internal Relationships._clique Clique;
        internal BullyingStateSnapshot BullyingStateBefore;
        internal int ActingIdolId = CoreConstants.InvalidIdValue;
        internal int TargetIdolId = CoreConstants.InvalidIdValue;
        internal int LeaderIdBefore = CoreConstants.InvalidIdValue;
        internal string CliqueSignatureBefore = string.Empty;
        internal List<int> MemberIdsBefore = new List<int>();
        internal List<int> StoppedMemberIdsBefore = new List<int>();
        internal int InfluenceCostLevel;
        internal int InfluencePointCost;
        internal bool WasValidBefore;
    }

    [Serializable]
    internal sealed class PlayerBullyingInterventionPayload
    {
        public int acting_idol_id = CoreConstants.InvalidIdValue;
        public int target_idol_id = CoreConstants.InvalidIdValue;
        public int clique_leader_id_before = CoreConstants.InvalidIdValue;
        public int clique_leader_id_after = CoreConstants.InvalidIdValue;
        public int clique_member_count;
        public string clique_signature = string.Empty;
        public string clique_generation_id = string.Empty;
        public string bullying_episode_id = string.Empty;
        public List<int> stopped_member_ids_before = new List<int>();
        public List<int> stopped_member_ids_after = new List<int>();
        public List<int> newly_stopped_member_ids = new List<int>();
        public bool bullying_active_before;
        public bool bullying_active_after;
        public string result = string.Empty;
        public int influence_cost_level;
        public int influence_point_cost;
        public string event_date = string.Empty;
    }

    internal sealed class RivalGroupStateSnapshot
    {
        internal Rivals._group GroupReference;
        internal int GroupId = CoreConstants.InvalidIdValue;
        internal string GroupName = string.Empty;
        internal long Fans;
        internal bool IsRising;
        internal bool IsRival;
        internal bool IsPhantasm;
        internal bool IsDead;
        internal bool IsGenreFixed;
        internal int GenreId = CoreConstants.InvalidIdValue;
        internal int SingleCount;
    }

    [Serializable]
    internal sealed class RivalGroupLifecycleEventPayload
    {
        public string rival_group_action = string.Empty;
        public int rival_group_id = CoreConstants.InvalidIdValue;
        public string rival_group_name = string.Empty;
        public bool present_before;
        public bool present_after;
        public long fans_before;
        public long fans_after;
        public bool is_rising_before;
        public bool is_rising_after;
        public bool is_dead_before;
        public bool is_dead_after;
        public bool is_rival;
        public bool is_phantasm;
        public bool is_genre_fixed;
        public int genre_id = CoreConstants.InvalidIdValue;
        public int single_count_before;
        public int single_count_after;
        public string event_date = string.Empty;
    }
}
