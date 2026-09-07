using System;
using System.Collections.Generic;
using System.Reflection;
using ModLocalizationSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CosmoSaveNotifications
{
    // Embedded in each save mod; only the authoritative progress owner calls it.
    // Vanilla postpones notifications while a popup/VN suspends time, drops them
    // when ten are visible, and filters "other" through user category settings.
    // Save status needs to remain visible during those very conditions. Reuse the
    // game's notification prefab and history without its gameplay-event queue.
    internal static class SaveNotifications
    {
        internal const string StartedKey = "SAVE_PROGRESS_STARTED";
        internal const string CompletedKey = "SAVE_PROGRESS_COMPLETED";
        internal const string FailedKey = "SAVE_PROGRESS_FAILED";
        internal const string CompanionFailedKey = "SAVE_PROGRESS_COMPANION_FAILED";
        private const int HistoryLimit = 100;
        private static readonly Queue<NotificationManager._notification> Pending =
            new Queue<NotificationManager._notification>();
        private static readonly ModLocalizer Localizer =
            ModLocalization.ForAssembly(Assembly.GetExecutingAssembly());

        // Both entry points are called on Unity's main thread, never by a writer.
        internal static void Show(string key, Color32 color)
        {
            Pending.Enqueue(new NotificationManager._notification
            {
                Date = staticVars.dateTime,
                BarColor = color,
                Text = Localizer.Get(key, key),
                Type = NotificationManager._notification._type.other
            });
            Pump();
        }

        internal static void Pump()
        {
            if (Pending.Count == 0) return;
            NotificationsContainer container = UnityEngine.Object.FindObjectOfType<NotificationsContainer>();
            if (container == null || container.prefab_notification == null) return;
            while (Pending.Count > 0)
            {
                NotificationManager._notification notification = Pending.Peek();
                GameObject view = null;
                try
                {
                    view = UnityEngine.Object.Instantiate(container.prefab_notification,
                        container.transform, false);
                    view.GetComponent<Notification>().Set(notification);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(
                        container.GetComponent<RectTransform>());
                    NotificationManager.Notifications.Add(notification);
                    if (NotificationManager.Notifications.Count > HistoryLimit)
                        NotificationManager.Notifications.RemoveAt(0);
                    Pending.Dequeue();
                }
                catch (Exception exception)
                {
                    if (view != null) UnityEngine.Object.Destroy(view);
                    Debug.LogWarning("[Save progress] Could not display notification: " + exception.Message);
                    return;
                }
            }
        }
    }
}
