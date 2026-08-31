using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Safety
{
    /// <summary>
    /// Cumulative Unity scaled time for reference sets where the stripped
    /// UnityEngine.CoreModule omits the otherwise standard Time.time getter.
    ///
    /// UnityEngine.Time.deltaTime is already scaled by Time.timeScale and is zero
    /// while scaled time is paused. Sampling it once per rendered frame therefore
    /// preserves the same duration domain used by WaitForSeconds without consuming
    /// realtime or unscaled wall time. The frame guard also makes the clock safe if
    /// another mod causes SaveManager.Update to be invoked more than once per frame.
    /// </summary>
    internal static class ScaledTimeClock
    {
        private static readonly object Sync = new object();

        private static int lastObservedFrame = -1;
        private static double elapsedScaledSeconds;
        private static bool updateTargetResolved;
        private static string failure = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                lock (Sync)
                {
                    return updateTargetResolved && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static float Now
        {
            get
            {
                ObserveFrame();
                lock (Sync)
                {
                    return (float)elapsedScaledSeconds;
                }
            }
        }

        internal static void ObserveFrame()
        {
            int frame;
            float delta;
            try
            {
                frame = UnityEngine.Time.frameCount;
                delta = UnityEngine.Time.deltaTime;
            }
            catch (Exception exception)
            {
                ReportFailure("Scaled-time sampling failed: " + exception.Message);
                return;
            }

            lock (Sync)
            {
                if (frame == lastObservedFrame)
                {
                    return;
                }
                lastObservedFrame = frame;

                if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0f)
                {
                    failure = "Unity supplied an invalid scaled frame delta.";
                    return;
                }

                double next = elapsedScaledSeconds + delta;
                if (double.IsNaN(next) || double.IsInfinity(next) || next > float.MaxValue)
                {
                    failure = "The process-local scaled-time accumulator overflowed.";
                    return;
                }
                elapsedScaledSeconds = next;
            }
        }

        internal static void ReportUpdateTargetResolved()
        {
            lock (Sync)
            {
                updateTargetResolved = true;
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown scaled-time clock failure";
            }
        }
    }

    /// <summary>
    /// SaveManager.Update is the source-proven gameplay-frame seam that also owns
    /// F5/F9 polling. This prefix observes time only; it never skips or changes the
    /// vanilla Update body.
    /// </summary>
    [HarmonyPatch]
    internal static class ScaledTimeClockUpdate_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                "Update",
                Type.EmptyTypes);
            if (method == null)
            {
                const string diagnostic =
                    "Scaled-time clock could not resolve SaveManager.Update().";
                ScaledTimeClock.ReportFailure(diagnostic);
                throw new MissingMethodException(diagnostic);
            }

            ScaledTimeClock.ReportUpdateTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            ScaledTimeClock.ObserveFrame();
        }
    }
}
