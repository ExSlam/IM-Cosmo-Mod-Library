using System;
using System.Collections.Generic;
using System.Globalization;
using ModLocalizationSystem;
using UnityEngine;

namespace CheatsMod
{
    public static partial class Cheats
    {
        private const float CompletedRoomProgress = 1f;
        private const float InitialRoomProgress = 0f;
        private const float InstantCompletionWindowMinutes = 1f;
        private const long CompletionDeadlineOffsetTicks = -1L;
        private const string RoomActivitiesCompletedKey = "notification.room_activities_completed";
        private const string NoRoomActivitiesKey = "notification.no_room_activities";
        private const string RoomActivitiesFailedKey = "notification.room_activities_failed";
        private const string RoomCompletionErrorFormat = "[CheatsMod] Could not finish room {0} ({1}): {2}";

        private sealed class RoomActivitySnapshot
        {
            internal agency._room Room;
            internal agency._room._status Status;
            internal DateTime StartedAt;
            internal data_girls.girls Idol;
            internal staff._staff Staffer;

            internal bool IsCurrent
            {
                get
                {
                    return Room.status == Status
                        && Room.startTime == StartedAt
                        && Room.girl == Idol
                        && Room.staffer == Staffer;
                }
            }
        }

        private static void CompleteRoomActivitiesCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            agency agencyData = GetDataComponent<agency>();
            if (agencyData == null)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationGameUnavailable, CheatFallbackText.NotificationGameUnavailable);
                return;
            }

            // Capture current jobs before any completion can trigger automatic room reassignment.
            // Idle rooms, already-ready results and continuous daily research/cafe/theater work
            // have no finite room timer to finish, so never tick them or issue duplicate payouts.
            List<RoomActivitySnapshot> activities = new List<RoomActivitySnapshot>();
            foreach (agency._room room in agencyData.allRooms(false, true))
            {
                if (room != null && HasTimedRoomActivity(room))
                {
                    activities.Add(new RoomActivitySnapshot
                    {
                        Room = room,
                        Status = room.status,
                        StartedAt = room.startTime,
                        Idol = room.girl,
                        Staffer = room.staffer
                    });
                }
            }

            int completedCount = CheatAmounts.ZeroCount;
            int failedCount = CheatAmounts.ZeroCount;
            DateTime now = staticVars.dateTime;
            foreach (RoomActivitySnapshot activity in activities)
            {
                if (!activity.IsCurrent)
                {
                    continue;
                }

                agency._room room = activity.Room;
                try
                {
                    // A finite one-minute elapsed window also works while the game is paused.
                    // Vanilla uses both fractional progress and a strict finishTime < now check.
                    room.startTime = now.AddMinutes(-InstantCompletionWindowMinutes);
                    room.finishTime = now.AddTicks(CompletionDeadlineOffsetTicks);
                    room.CompletionTime = InstantCompletionWindowMinutes;
                    room.Progress_Init = InitialRoomProgress;
                    room.Progress = CompletedRoomProgress;
                    room.OnTimeTick();
                    completedCount++;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    Debug.LogError(string.Format(
                        CultureInfo.InvariantCulture,
                        RoomCompletionErrorFormat,
                        room.id,
                        activity.Status,
                        exception));
                }
            }

            if (completedCount > CheatAmounts.ZeroCount)
            {
                RefreshIdolList();
                RefreshStaffList();
                NotificationManager.AddNotification(
                    string.Format(CultureInfo.CurrentCulture,
                        ModLocalization.Get(RoomActivitiesCompletedKey, string.Empty), completedCount),
                    mainScript.green32,
                    NotificationManager._notification._type.other);
            }
            else if (failedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(NoRoomActivitiesKey, string.Empty);
            }

            if (failedCount > CheatAmounts.ZeroCount)
            {
                NotificationManager.AddNotification(
                    string.Format(CultureInfo.CurrentCulture,
                        ModLocalization.Get(RoomActivitiesFailedKey, string.Empty), failedCount),
                    mainScript.red32,
                    NotificationManager._notification._type.other);
            }
        }

        private static bool HasTimedRoomActivity(agency._room room)
        {
            if (room.type == agency._type.cafeAndShop)
            {
                return false;
            }

            switch (room.status)
            {
                case agency._room._status.girlTraining:
                case agency._room._status.singleProduction:
                case agency._room._status.showProduction:
                case agency._room._status.concertProduction:
                case agency._room._status.SSKProduction:
                case agency._room._status.tourProduction:
                case agency._room._status.business:
                case agency._room._status.audition:
                case agency._room._status.loan:
                case agency._room._status.date:
                case agency._room._status.injury_treatment:
                case agency._room._status.depression_treatment:
                case agency._room._status.scene:
                    return true;
                default:
                    return false;
            }
        }
    }
}
