using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Wave 2 Task 7 historical lifecycle closure captures. These rows describe
    /// durable game mutations but do not own or restore the corresponding live state.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        internal void CaptureActivityLevelUp(Activities._activity activity, int levelBefore)
        {
            if (activity == null || activity.lvl <= levelBefore)
            {
                return;
            }

            ActivityLevelUpPayload payload = new ActivityLevelUpPayload
            {
                activity_type = activity.type.ToString(),
                activity_title = activity.GetTitle() ?? string.Empty,
                activity_level_before = levelBefore,
                activity_level_after = activity.lvl,
                activity_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
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
                    CoreConstants.EventEntityKindActivity,
                    activity.type.ToString(),
                    CoreConstants.EventTypeActivityLevelUp,
                    CoreConstants.EventSourceActivityLevelUpPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        internal SummerGamesObjectiveSnapshot CreateSummerGamesObjectiveSnapshot(string objectiveCode)
        {
            SummerGamesObjectiveSnapshot snapshot = new SummerGamesObjectiveSnapshot
            {
                ObjectiveCode = objectiveCode ?? string.Empty
            };
            if (tasks.Story_Data == null)
            {
                return snapshot;
            }

            snapshot.DisplayTaskBefore = tasks.Story_Data.ch4_display_task;
            snapshot.DeadlineBefore = tasks.Story_Data.ch4_deadline ?? string.Empty;
            snapshot.ScandalPointsBefore = SaveNLoadFixesWideNumericInterop.GetStoryCh4ScandalPoints();
            snapshot.ChartUnlockedBefore = tasks.Story_Data.ch4_chart_unlocked;
            snapshot.FansNeededBefore = SaveNLoadFixesWideNumericInterop.GetStoryCh4FansNeeded();
            return snapshot;
        }

        internal void CaptureSummerGamesObjectiveActivated(SummerGamesObjectiveSnapshot snapshot)
        {
            if (snapshot == null || tasks.Story_Data == null ||
                (snapshot.ObjectiveCode != "ch4_1" && snapshot.ObjectiveCode != "ch4_2" &&
                 snapshot.ObjectiveCode != "ch4_3" && snapshot.ObjectiveCode != "ch4_4"))
            {
                return;
            }

            long scandalPointsAfter = SaveNLoadFixesWideNumericInterop.GetStoryCh4ScandalPoints();
            long fansNeededAfter = SaveNLoadFixesWideNumericInterop.GetStoryCh4FansNeeded();
            bool changed =
                snapshot.DisplayTaskBefore != tasks.Story_Data.ch4_display_task ||
                !string.Equals(snapshot.DeadlineBefore ?? string.Empty, tasks.Story_Data.ch4_deadline ?? string.Empty, StringComparison.Ordinal) ||
                snapshot.ScandalPointsBefore != scandalPointsAfter ||
                snapshot.ChartUnlockedBefore != tasks.Story_Data.ch4_chart_unlocked ||
                snapshot.FansNeededBefore != fansNeededAfter;
            if (!changed)
            {
                return;
            }

            SummerGamesObjectivePayload payload = new SummerGamesObjectivePayload
            {
                objective_code = snapshot.ObjectiveCode,
                display_task_before = snapshot.DisplayTaskBefore,
                display_task_after = tasks.Story_Data.ch4_display_task,
                deadline_before = snapshot.DeadlineBefore ?? string.Empty,
                deadline_after = tasks.Story_Data.ch4_deadline ?? string.Empty,
                scandal_points_before = snapshot.ScandalPointsBefore,
                scandal_points_after = scandalPointsAfter,
                chart_unlocked_before = snapshot.ChartUnlockedBefore,
                chart_unlocked_after = tasks.Story_Data.ch4_chart_unlocked,
                fans_needed_before = snapshot.FansNeededBefore,
                fans_needed_after = fansNeededAfter,
                activation_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
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
                    CoreConstants.EventEntityKindSummerGames,
                    snapshot.ObjectiveCode,
                    CoreConstants.EventTypeSummerGamesObjectiveActivated,
                    CoreConstants.EventSourceSummerGamesObjectivePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        internal void CaptureTourCountryLevelUp(SEvent_Tour.country country, int levelBefore)
        {
            if (country == null || country.Level <= levelBefore)
            {
                return;
            }

            TourCountryLevelUpPayload payload = new TourCountryLevelUpPayload
            {
                country_type = country.Type.ToString(),
                country_title = country.GetTitle() ?? string.Empty,
                country_level_before = levelBefore,
                country_level_after = country.Level,
                progression_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
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
                    CoreConstants.EventEntityKindTour,
                    string.Concat("country:", country.Type.ToString()),
                    CoreConstants.EventTypeTourCountryLevelUp,
                    CoreConstants.EventSourceTourCountryLevelUpPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        internal StoryStatusRestoreSnapshot CreateStoryStatusRestoreSnapshot(string formula)
        {
            StoryStatusRestoreSnapshot snapshot = new StoryStatusRestoreSnapshot
            {
                Formula = formula ?? string.Empty
            };
            if (formula == "story_chapter_5_quitter_returns")
            {
                AddStoryStatusRestoreCandidate(snapshot, tasks.Story_Data != null ? tasks.Story_Data.Get_Girl_Quitter() : null);
            }
            else if (formula == "story_chapter_6_rival_epi")
            {
                if (tasks.Story_Data != null)
                {
                    AddStoryStatusRestoreCandidate(snapshot, tasks.Story_Data.Get_Girl_Quitter());
                    AddStoryStatusRestoreCandidate(snapshot, tasks.Story_Data.Get_Girl_Sent_To_Rival());
                }
            }
            return snapshot;
        }

        private static void AddStoryStatusRestoreCandidate(StoryStatusRestoreSnapshot snapshot, data_girls.girls idol)
        {
            if (snapshot == null || idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }
            for (int index = 0; index < snapshot.Idols.Count; index++)
            {
                if (snapshot.Idols[index].Idol == idol)
                {
                    return;
                }
            }
            snapshot.Idols.Add(new StoryStatusRestoreCandidate
            {
                Idol = idol,
                StatusBefore = idol.status
            });
        }

        internal void CaptureStoryStatusRestorations(StoryStatusRestoreSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Idols == null)
            {
                return;
            }
            for (int index = 0; index < snapshot.Idols.Count; index++)
            {
                StoryStatusRestoreCandidate candidate = snapshot.Idols[index];
                if (candidate == null || candidate.Idol == null || candidate.Idol.status == candidate.StatusBefore)
                {
                    continue;
                }
                CaptureStatusTransition(
                    candidate.Idol,
                    candidate.StatusBefore,
                    candidate.Idol.status,
                    CoreConstants.EventSourceVnStoryStatusRestorePatch);
            }
        }
    }

    [Serializable]
    internal sealed class ActivityLevelUpPayload
    {
        public string activity_type = string.Empty;
        public string activity_title = string.Empty;
        public int activity_level_before;
        public int activity_level_after;
        public string activity_date = string.Empty;
    }

    internal sealed class SummerGamesObjectiveSnapshot
    {
        internal string ObjectiveCode = string.Empty;
        internal bool DisplayTaskBefore;
        internal string DeadlineBefore = string.Empty;
        internal long ScandalPointsBefore;
        internal bool ChartUnlockedBefore;
        internal long FansNeededBefore;
    }

    [Serializable]
    internal sealed class SummerGamesObjectivePayload
    {
        public string objective_code = string.Empty;
        public bool display_task_before;
        public bool display_task_after;
        public string deadline_before = string.Empty;
        public string deadline_after = string.Empty;
        public long scandal_points_before;
        public long scandal_points_after;
        public bool chart_unlocked_before;
        public bool chart_unlocked_after;
        public long fans_needed_before;
        public long fans_needed_after;
        public string activation_date = string.Empty;
    }

    [Serializable]
    internal sealed class TourCountryLevelUpPayload
    {
        public string country_type = string.Empty;
        public string country_title = string.Empty;
        public int country_level_before;
        public int country_level_after;
        public string progression_date = string.Empty;
    }

    internal sealed class StoryStatusRestoreSnapshot
    {
        internal string Formula = string.Empty;
        internal List<StoryStatusRestoreCandidate> Idols = new List<StoryStatusRestoreCandidate>();
    }

    internal sealed class StoryStatusRestoreCandidate
    {
        internal data_girls.girls Idol;
        internal data_girls._status StatusBefore;
    }
}
