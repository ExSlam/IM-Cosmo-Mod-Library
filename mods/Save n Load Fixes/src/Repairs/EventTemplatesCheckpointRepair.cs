using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repair N06 / audit #6: closes the Event_Templates selected-to-popup checkpoint
    /// gap without serializing the rich Active_Template composite.
    ///
    /// A blocker is created only for the exact OpenPopup iterator produced after a
    /// template has successfully reached _OpenPopup(). The blocker remains active
    /// while vanilla waits for a free popup queue/dialogue boundary and is released
    /// only after PopupManager.Open(random_event, true) has synchronously acquired
    /// the vanilla popup guard. The iterator is also bound to the LoadEpoch in which
    /// it was created, so discarded F9 work cannot open against the target timeline.
    /// </summary>
    internal static class EventTemplatesCheckpointRepair
    {
        private sealed class PendingOpen
        {
            internal CheckpointBlockerLease Lease;
            internal long Epoch;
            internal string TemplateId;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<object, PendingOpen> PendingByIterator =
            new Dictionary<object, PendingOpen>();

        private static long registeredCount;
        private static long popupHandoffCount;
        private static long staleSuppressionCount;
        private static string lastDiagnostic = string.Empty;
        private static int unhealthyRegistrationWarningLogged;

        internal static bool IsImplemented
        {
            get { return EventTemplatesCheckpointPatchHealth.IsHealthy; }
        }

        internal static int PendingCount
        {
            get
            {
                lock (Sync)
                {
                    return PendingByIterator.Count;
                }
            }
        }

        internal static long RegisteredCount
        {
            get { return Interlocked.Read(ref registeredCount); }
        }

        internal static long PopupHandoffCount
        {
            get { return Interlocked.Read(ref popupHandoffCount); }
        }

        internal static long StaleSuppressionCount
        {
            get { return Interlocked.Read(ref staleSuppressionCount); }
        }

        internal static string LastDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastDiagnostic;
                }
            }
        }

        internal static void RegisterPendingOpen(IEnumerator iterator)
        {
            if (iterator == null)
            {
                return;
            }

            // Never acquire a blocker if the exact popup-open handoff seam could not
            // be established. A half-installed blocker would risk permanently
            // suppressing checkpoints after the first template event.
            if (!EventTemplatesCheckpointPatchHealth.IsHealthy)
            {
                if (Interlocked.Exchange(ref unhealthyRegistrationWarningLogged, 1) == 0)
                {
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "Event_Templates checkpoint repair is not healthy; pending-open " +
                        "blockers will not be acquired.");
                }
                return;
            }

            object iteratorKey = iterator;
            long epoch = LoadEpoch.Capture();
            string templateId = TryGetActiveTemplateId();
            CheckpointBlockerLease lease = CheckpointGate.Acquire(
                CheckpointBlockerKind.EventTemplatesPendingOpen,
                BuildBlockerDetail(templateId, epoch));

            bool duplicate;
            lock (Sync)
            {
                duplicate = PendingByIterator.ContainsKey(iteratorKey);
                if (!duplicate)
                {
                    PendingByIterator.Add(
                        iteratorKey,
                        new PendingOpen
                        {
                            Lease = lease,
                            Epoch = epoch,
                            TemplateId = templateId
                        });
                    lastDiagnostic =
                        "Registered Event_Templates pending-open blocker for template '" +
                        templateId +
                        "' at load epoch " +
                        epoch.ToString(CultureInfo.InvariantCulture) +
                        ".";
                }
            }

            if (duplicate)
            {
                lease.Dispose();
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Duplicate Event_Templates OpenPopup iterator registration was ignored.");
                return;
            }

            Interlocked.Increment(ref registeredCount);
        }

        /// <summary>
        /// Prefix guard for the exact generated Event_Templates.OpenPopup iterator.
        /// Returning false causes Harmony to skip the stale iterator's MoveNext body.
        /// </summary>
        internal static bool ShouldRunMoveNext(object iterator)
        {
            if (iterator == null)
            {
                return true;
            }

            PendingOpen pending;
            lock (Sync)
            {
                if (!PendingByIterator.TryGetValue(iterator, out pending))
                {
                    return true;
                }
            }

            if (LoadEpoch.IsCurrent(pending.Epoch))
            {
                return true;
            }

            RemoveAndDispose(iterator, pending);
            Interlocked.Increment(ref staleSuppressionCount);

            string diagnostic =
                "Discarded stale Event_Templates pending-open iterator for template '" +
                pending.TemplateId +
                "' from load epoch " +
                pending.Epoch.ToString(CultureInfo.InvariantCulture) +
                "; current epoch is " +
                LoadEpoch.Current.ToString(CultureInfo.InvariantCulture) +
                ".";
            SetLastDiagnostic(diagnostic);
            Debug.Log(SaveNLoadFixesConstants.LogPrefix + diagnostic);
            return false;
        }

        /// <summary>
        /// Called immediately after the exact PopupManager.Open(random_event, true)
        /// instruction in the generated iterator. Popup.Show increments PopupCounter
        /// synchronously via GameObject activation, so a positive counter proves the
        /// vanilla popup guard has acquired the checkpoint-safety responsibility.
        /// </summary>
        internal static void MarkPopupOpened(object iterator)
        {
            if (iterator == null)
            {
                return;
            }

            PendingOpen pending;
            lock (Sync)
            {
                if (!PendingByIterator.TryGetValue(iterator, out pending))
                {
                    return;
                }
            }

            if (PopupManager.PopupCounter <= 0)
            {
                string noGuardDiagnostic =
                    "Event_Templates random-event popup call returned without acquiring " +
                    "PopupManager.PopupCounter protection; the SNLF blocker remains active.";
                SetLastDiagnostic(noGuardDiagnostic);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + noGuardDiagnostic);
                return;
            }

            RemoveAndDispose(iterator, pending);
            Interlocked.Increment(ref popupHandoffCount);

            string diagnostic =
                "Event_Templates pending-open blocker handed checkpoint safety to the " +
                "vanilla popup guard for template '" +
                pending.TemplateId +
                "'.";
            SetLastDiagnostic(diagnostic);
        }

        private static void RemoveAndDispose(object iterator, PendingOpen expected)
        {
            CheckpointBlockerLease lease = null;
            lock (Sync)
            {
                PendingOpen current;
                if (PendingByIterator.TryGetValue(iterator, out current) &&
                    object.ReferenceEquals(current, expected))
                {
                    PendingByIterator.Remove(iterator);
                    lease = current.Lease;
                }
            }

            if (lease != null)
            {
                lease.Dispose();
            }
        }

        private static string TryGetActiveTemplateId()
        {
            try
            {
                Event_Templates._active_template active = Event_Templates.Active_Template;
                if (active == null || active.Template == null ||
                    string.IsNullOrEmpty(active.Template.ID))
                {
                    return "<unknown>";
                }

                return active.Template.ID;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Could not read Event_Templates.Active_Template identity while " +
                    "registering checkpoint blocker. " +
                    exception.GetType().Name +
                    ": " +
                    exception.Message);
                return "<unknown>";
            }
        }

        private static string BuildBlockerDetail(string templateId, long epoch)
        {
            return "template=" +
                   (templateId ?? "<unknown>") +
                   ";epoch=" +
                   epoch.ToString(CultureInfo.InvariantCulture);
        }

        private static void SetLastDiagnostic(string diagnostic)
        {
            lock (Sync)
            {
                lastDiagnostic = diagnostic ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Health for the three exact seams Task 3 relies on: the private OpenPopup
    /// iterator factory, its generated MoveNext, and one PopupManager.Open call in
    /// that MoveNext body. Blockers are not acquired unless this contract is healthy.
    /// </summary>
    internal static class EventTemplatesCheckpointPatchHealth
    {
        private static readonly object Sync = new object();
        private static bool factoryResolved;
        private static bool moveNextResolved;
        private static bool moveNextOpenSiteHealthy;
        private static bool failureReported;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return factoryResolved &&
                           moveNextResolved &&
                           moveNextOpenSiteHealthy &&
                           !failureReported;
                }
            }
        }

        internal static void ReportFactoryResolved()
        {
            lock (Sync)
            {
                factoryResolved = true;
            }
        }

        internal static void ReportMoveNextResolved()
        {
            lock (Sync)
            {
                moveNextResolved = true;
            }
        }

        internal static void ReportMoveNextOpenSites(MethodBase method, int observed)
        {
            lock (Sync)
            {
                moveNextOpenSiteHealthy = observed == 1;
                if (observed != 1)
                {
                    failureReported = true;
                }
            }

            if (observed != 1)
            {
                Debug.LogError(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Event_Templates OpenPopup iterator patch expected exactly one " +
                    "PopupManager.Open call but observed " +
                    observed.ToString(CultureInfo.InvariantCulture) +
                    " in " +
                    DescribeMethod(method) +
                    ".");
            }
        }

        internal static void ReportFailure(string seam, string reason)
        {
            lock (Sync)
            {
                failureReported = true;
            }

            Debug.LogError(
                SaveNLoadFixesConstants.LogPrefix +
                "Event_Templates checkpoint repair failed to resolve " +
                (seam ?? "<unknown seam>") +
                ": " +
                (reason ?? string.Empty));
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "<unknown method>";
            }

            return (method.DeclaringType == null
                    ? "<unknown type>"
                    : method.DeclaringType.FullName) +
                   "." +
                   method.Name;
        }
    }
}
