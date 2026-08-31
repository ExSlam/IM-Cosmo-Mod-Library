using System;
using System.Collections.Generic;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A17: changing a pushed idol through Girl_Select_Popup updates
    /// Pushes.Girls immediately, while vanilla waits until the next OnNewDay() to
    /// notice GirlsLastDay != Girls and reset Pushes.Days. A checkpoint in that gap
    /// serializes the new idol together with the previous idol's accumulated Days;
    /// Pushes.LoadData() then seeds GirlsLastDay from the newly loaded Girls value and
    /// permanently erases the pending-change witness.
    ///
    /// The repair owns no persistence. Immediately before the audited popup commit,
    /// it recognizes the exact Pushes.Girls receiver list by object identity and resets
    /// only the selected slot's Days when the idol reference actually changes. Vanilla
    /// still performs the list assignment, button rendering, popup close, daily update,
    /// influence reward, and all save/load work.
    /// </summary>
    internal static class PushSlotDurationRepair
    {
        private static long resetCount;
        private static long nonPushReceiverPassThroughCount;
        private static long sameIdentityPassThroughCount;
        private static long invalidShapeCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return PushSlotDurationPatchHealth.IsHealthy; }
        }

        internal static long ResetCount
        {
            get { return Interlocked.Read(ref resetCount); }
        }

        internal static long NonPushReceiverPassThroughCount
        {
            get { return Interlocked.Read(ref nonPushReceiverPassThroughCount); }
        }

        internal static long SameIdentityPassThroughCount
        {
            get { return Interlocked.Read(ref sameIdentityPassThroughCount); }
        }

        internal static long InvalidShapeCount
        {
            get { return Interlocked.Read(ref invalidShapeCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void ResetDurationBeforePushSelection(
            Girl_Select_Popup popup,
            data_girls.girls selectedGirl)
        {
            if (popup == null)
            {
                RecordInvalid("A17 observed a null Girl_Select_Popup instance.");
                return;
            }

            List<data_girls.girls> receiver = popup.ReceiverArray;
            if (!object.ReferenceEquals(receiver, Pushes.Girls))
            {
                Interlocked.Increment(ref nonPushReceiverPassThroughCount);
                return;
            }

            int slot = popup.ID;
            if (receiver == null ||
                Pushes.Days == null ||
                slot < 0 ||
                slot >= receiver.Count ||
                slot >= Pushes.Days.Count)
            {
                RecordInvalid(
                    "A17 could not prove a valid pushed-slot/Days shape at Girl_Select_Popup.OnClick().");
                return;
            }

            data_girls.girls previousGirl = receiver[slot];
            if (object.ReferenceEquals(previousGirl, selectedGirl))
            {
                Interlocked.Increment(ref sameIdentityPassThroughCount);
                lastDiagnostic =
                    "A17 observed an unchanged pushed-idol identity and preserved the existing Days counter.";
                return;
            }

            Pushes.Days[slot] = 0;
            Interlocked.Increment(ref resetCount);
            lastDiagnostic =
                "A17 synchronously reset Pushes.Days[" + slot +
                "] before vanilla committed a changed Pushes.Girls slot identity.";
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidShapeCount);
            lastDiagnostic = diagnostic ?? "unknown A17 pushed-slot shape failure";
        }
    }

    internal static class PushSlotDurationPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount;
                }
            }
        }

        internal static string Failure
        {
            get
            {
                lock (Sync)
                {
                    return failure;
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure =
                        "A17 resolved more Girl_Select_Popup.OnClick targets than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A17 patch failure";
            }
        }
    }
}
