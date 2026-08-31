using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Safety
{
    /// <summary>
    /// Process-local timeline generation used to invalidate deferred work created
    /// by a discarded same-process career timeline.
    ///
    /// Task 2 provides the generation primitive and successful-load adoption seam.
    /// Later Sprint 1D repair tasks capture/check this epoch only for the finite
    /// authoritative deferred carriers selected by the audit.
    /// </summary>
    internal static class LoadEpoch
    {
        private static long currentEpoch = 1L;
        private static long successfulCareerLoadCount;
        private static readonly object DiagnosticSync = new object();
        private static string lastAdvanceReason = string.Empty;

        internal static long Current
        {
            get { return Interlocked.Read(ref currentEpoch); }
        }

        internal static long SuccessfulCareerLoadCount
        {
            get { return Interlocked.Read(ref successfulCareerLoadCount); }
        }

        internal static string LastAdvanceReason
        {
            get
            {
                lock (DiagnosticSync)
                {
                    return lastAdvanceReason;
                }
            }
        }

        /// <summary>
        /// Capture the generation in which a deferred carrier is created.
        /// </summary>
        internal static long Capture()
        {
            return Current;
        }

        /// <summary>
        /// Returns true only when work still belongs to the active process-local
        /// timeline. This deliberately contains no CLR object identity checks.
        /// </summary>
        internal static bool IsCurrent(long capturedEpoch)
        {
            return capturedEpoch == Current;
        }

        /// <summary>
        /// Stack-compatible replacement for SaveManager.Data's stfld in the two
        /// audited LoadData overloads. A successful non-null target advances the
        /// epoch immediately before the target DTO replaces manager.Data. A failed
        /// load still assigns vanilla's null result but does not invalidate the
        /// current timeline's pending work.
        /// </summary>
        internal static void AssignLoadedDataWithEpoch(
            SaveManager manager,
            SaveManager.SavedData loadedData)
        {
            if (loadedData != null)
            {
                Advance("SaveManager.LoadData successful target adoption");
                Interlocked.Increment(ref successfulCareerLoadCount);
            }

            manager.Data = loadedData;
        }

        private static long Advance(string reason)
        {
            long nextEpoch = Interlocked.Increment(ref currentEpoch);

            // A successful target load establishes a new timeline. Any transient
            // blocker token belonging to an older timeline must not survive that
            // adoption. Token IDs remain monotonic, so late disposal of an old
            // lease cannot release a future blocker.
            CheckpointGate.ResetForNewTimeline();

            lock (DiagnosticSync)
            {
                lastAdvanceReason = reason ?? string.Empty;
            }

            Debug.Log(
                SaveNLoadFixesConstants.LogPrefix +
                "LoadEpoch advanced to " +
                nextEpoch.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ": " +
                (reason ?? string.Empty));

            return nextEpoch;
        }
    }

    /// <summary>
    /// Separate health accounting for the two career-load assignment seams. The
    /// epoch primitive remains inspectable even when a future vanilla update moves
    /// one assignment and causes interception health to fail.
    /// </summary>
    internal static class LoadEpochPatchHealth
    {
        private static readonly object Sync = new object();
        private static readonly HashSet<string> ReportedCallers = new HashSet<string>();
        private static bool failureReported;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return !failureReported && ReportedCallers.Count == 2;
                }
            }
        }

        internal static void ReportCaller(MethodBase caller, int observedAssignments)
        {
            string callerId = BuildCallerId(caller);
            bool healthy = observedAssignments == 1;

            lock (Sync)
            {
                ReportedCallers.Add(callerId);
                if (!healthy)
                {
                    failureReported = true;
                }
            }

            if (!healthy)
            {
                Debug.LogError(
                    SaveNLoadFixesConstants.LogPrefix +
                    "LoadEpoch interception mismatch at " +
                    callerId +
                    ": expected 1 SaveManager.Data assignment, observed " +
                    observedAssignments.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ".");
            }
        }

        internal static void ReportFailure(MethodBase caller, string reason)
        {
            lock (Sync)
            {
                failureReported = true;
                ReportedCallers.Add(BuildCallerId(caller));
            }

            Debug.LogError(
                SaveNLoadFixesConstants.LogPrefix +
                "LoadEpoch interception failed at " +
                BuildCallerId(caller) +
                ": " +
                (reason ?? string.Empty));
        }

        private static string BuildCallerId(MethodBase caller)
        {
            if (caller == null)
            {
                return "<unknown>";
            }

            ParameterInfo[] parameters = caller.GetParameters();
            string parameterName = parameters.Length == 1
                ? parameters[0].ParameterType.FullName
                : "?";
            string declaringName = caller.DeclaringType == null
                ? "<unknown>"
                : caller.DeclaringType.FullName;

            return declaringName + "." + caller.Name + "(" + parameterName + ")";
        }
    }
}
