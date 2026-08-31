using System;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A27: Tutorial.Active_Tutorial_ID is process-static lifecycle state that
    /// vanilla sets when a tutorial starts but never clears on normal completion, Quit,
    /// Reset, or load. agency._room.DoBusiness() then treats the stale string
    /// "10_business_deals" as a permanent forced-success switch.
    ///
    /// The repair owns no persistence. It clears the transient scalar at the audited
    /// terminal/reset boundaries and, immediately before vanilla business resolution,
    /// removes any leftover ID when no tutorial window is actually active. Vanilla's
    /// original business success roll and original business-tutorial override remain in
    /// place; the override can survive only while Tutorial.Is_Tutorial() is true.
    /// </summary>
    internal static class TutorialActiveIdRepair
    {
        internal const string BusinessTutorialId = "10_business_deals";

        private static long resetBoundaryCount;
        private static long terminalContinueBoundaryCount;
        private static long quitBoundaryCount;
        private static long businessNormalizationCount;
        private static long staleIdClearedCount;
        private static long activeBusinessTutorialObservedCount;
        private static long businessNoTutorialObservedCount;
        private static long runtimeFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return TutorialActiveIdPatchHealth.IsHealthy; }
        }

        internal static long ResetBoundaryCount
        {
            get { return Interlocked.Read(ref resetBoundaryCount); }
        }

        internal static long TerminalContinueBoundaryCount
        {
            get { return Interlocked.Read(ref terminalContinueBoundaryCount); }
        }

        internal static long QuitBoundaryCount
        {
            get { return Interlocked.Read(ref quitBoundaryCount); }
        }

        internal static long BusinessNormalizationCount
        {
            get { return Interlocked.Read(ref businessNormalizationCount); }
        }

        internal static long StaleIdClearedCount
        {
            get { return Interlocked.Read(ref staleIdClearedCount); }
        }

        internal static long ActiveBusinessTutorialObservedCount
        {
            get { return Interlocked.Read(ref activeBusinessTutorialObservedCount); }
        }

        internal static long BusinessNoTutorialObservedCount
        {
            get { return Interlocked.Read(ref businessNoTutorialObservedCount); }
        }

        internal static long RuntimeFailureCount
        {
            get { return Interlocked.Read(ref runtimeFailureCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void ClearAfterTutorialReset()
        {
            Interlocked.Increment(ref resetBoundaryCount);
            ClearActiveId("Tutorial.Reset() boundary");
        }

        internal static void ClearBeforeTutorialHide(Tutorial_Window tutorialWindow)
        {
            // Stock Tutorial_Window has exactly two calls to this Hide wrapper: the
            // terminal no-next-card path and Quit_Confirm(). Clearing at the wrapper
            // Prefix happens before Popup.Hide() can synchronously release PopupCounter.
            try
            {
                if (tutorialWindow != null &&
                    tutorialWindow.Data != null &&
                    tutorialWindow.Data.GetStatus() == Tutorial._status.Done)
                {
                    Interlocked.Increment(ref terminalContinueBoundaryCount);
                    ClearActiveId("normal tutorial completion before popup hide");
                    return;
                }

                if (Tutorial.Save_Data != null &&
                    Tutorial.Save_Data.Type == Tutorial._save_data._type.Ignore)
                {
                    Interlocked.Increment(ref quitBoundaryCount);
                    ClearActiveId("Tutorial_Window.Quit_Confirm() before popup hide");
                    return;
                }

                // Defensive stock-future/modded call: a hidden tutorial window is not a
                // genuine active tutorial. Clear the transient scalar rather than let a
                // hidden window retain a gameplay override.
                ClearActiveId("Tutorial_Window.Hide(Action) boundary");
            }
            catch (Exception exception)
            {
                RecordRuntimeFailure(
                    "Could not classify Tutorial_Window.Hide(Action); clearing transient Active_Tutorial_ID defensively: " +
                    exception.GetType().Name + ": " + exception.Message);
                ClearActiveId("unclassified Tutorial_Window.Hide(Action) boundary");
            }
        }

        internal static void NormalizeBeforeBusinessResolution()
        {
            Interlocked.Increment(ref businessNormalizationCount);

            bool tutorialWindowActive;
            try
            {
                tutorialWindowActive = Tutorial.Is_Tutorial();
            }
            catch (Exception exception)
            {
                // If the repair cannot prove a live tutorial window, fail toward vanilla's
                // ordinary business roll rather than preserving a process-stale forced ID.
                RecordRuntimeFailure(
                    "Tutorial.Is_Tutorial() failed before business resolution; clearing transient Active_Tutorial_ID: " +
                    exception.GetType().Name + ": " + exception.Message);
                ClearActiveId("business resolution with unprovable tutorial window");
                return;
            }

            if (!tutorialWindowActive)
            {
                Interlocked.Increment(ref businessNoTutorialObservedCount);
                ClearActiveId("business resolution with no active tutorial window");
                return;
            }

            if (string.Equals(
                Tutorial.Active_Tutorial_ID,
                BusinessTutorialId,
                StringComparison.Ordinal))
            {
                Interlocked.Increment(ref activeBusinessTutorialObservedCount);
                lastDiagnostic =
                    "Observed genuine active business tutorial; vanilla 10_business_deals forced-success branch remains eligible.";
                return;
            }

            lastDiagnostic =
                "Observed an active non-business tutorial; vanilla business success remains on its ordinary result path.";
        }

        private static void ClearActiveId(string reason)
        {
            string previous = Tutorial.Active_Tutorial_ID;
            Tutorial.Active_Tutorial_ID = string.Empty;

            if (!string.IsNullOrEmpty(previous))
            {
                Interlocked.Increment(ref staleIdClearedCount);
                lastDiagnostic =
                    "Cleared transient Tutorial.Active_Tutorial_ID ('" + previous + "') at " + reason + ".";
            }
            else
            {
                lastDiagnostic =
                    "Tutorial.Active_Tutorial_ID was already empty at " + reason + ".";
            }
        }

        private static void RecordRuntimeFailure(string diagnostic)
        {
            Interlocked.Increment(ref runtimeFailureCount);
            lastDiagnostic = diagnostic ?? "unknown tutorial Active_Tutorial_ID repair failure";
        }
    }

    internal static class TutorialActiveIdPatchHealth
    {
        internal const int ExpectedTargetCount = 3;

        private static readonly object Sync = new object();
        private static bool tutorialResetResolved;
        private static bool tutorialHideResolved;
        private static bool doBusinessResolved;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return tutorialResetResolved &&
                           tutorialHideResolved &&
                           doBusinessResolved &&
                           string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetCount
        {
            get
            {
                lock (Sync)
                {
                    int count = 0;
                    if (tutorialResetResolved) count++;
                    if (tutorialHideResolved) count++;
                    if (doBusinessResolved) count++;
                    return count;
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

        internal static void ReportTutorialResetResolved()
        {
            lock (Sync) tutorialResetResolved = true;
        }

        internal static void ReportTutorialHideResolved()
        {
            lock (Sync) tutorialHideResolved = true;
        }

        internal static void ReportDoBusinessResolved()
        {
            lock (Sync) doBusinessResolved = true;
        }

        internal static void ReportFailure(string message)
        {
            lock (Sync)
            {
                failure = message ?? "unknown tutorial Active_Tutorial_ID patch failure";
            }
        }
    }
}
