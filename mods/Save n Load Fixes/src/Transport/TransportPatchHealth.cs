using System;
using System.Collections.Generic;

namespace SaveNLoadFixes.Transport
{
    internal enum TransportPatchSurface
    {
        SavedDataWrite = 0,
        SavedDataRead = 1,
        GlobalDataWrite = 2,
        GlobalDataRead = 3
    }

    /// <summary>
    /// Central patch-health ledger for the embedded ordered transport.
    ///
    /// Sprint 1B reports concrete caller-level transpiler results into this tracker.
    /// The provider becomes authoritative automatically only when every required
    /// SavedData and GlobalData read/write surface matches the audited source shape.
    /// </summary>
    internal static class TransportPatchHealth
    {
        internal const int ExpectedSavedDataWriteCallerCount = 5;
        internal const int ExpectedSavedDataWriteCallSiteCount = 5;
        internal const int ExpectedSavedDataReadCallerCount = 7;
        internal const int ExpectedSavedDataReadCallSiteCount = 8;
        internal const int ExpectedGlobalDataWriteCallerCount = 1;
        internal const int ExpectedGlobalDataWriteCallSiteCount = 1;
        internal const int ExpectedGlobalDataReadCallerCount = 1;
        internal const int ExpectedGlobalDataReadCallSiteCount = 1;

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<TransportPatchSurface, SurfaceState> States =
            new Dictionary<TransportPatchSurface, SurfaceState>
            {
                { TransportPatchSurface.SavedDataWrite, new SurfaceState() },
                { TransportPatchSurface.SavedDataRead, new SurfaceState() },
                { TransportPatchSurface.GlobalDataWrite, new SurfaceState() },
                { TransportPatchSurface.GlobalDataRead, new SurfaceState() }
            };

        private static bool providerActivated;

        internal static bool ProviderActivated
        {
            get
            {
                lock (SyncRoot)
                {
                    return providerActivated;
                }
            }
        }

        internal static void ReportCaller(
            TransportPatchSurface surface,
            string callerIdentity,
            int matchingCallSites,
            int expectedMatchingCallSites)
        {
            if (string.IsNullOrEmpty(callerIdentity))
            {
                throw new ArgumentException("callerIdentity must not be empty.", nameof(callerIdentity));
            }

            if (matchingCallSites < 0 || expectedMatchingCallSites < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matchingCallSites),
                    "Call-site counts must be non-negative.");
            }

            lock (SyncRoot)
            {
                SurfaceState state = States[surface];
                state.CallerCallSiteCounts[callerIdentity] = matchingCallSites;
                if (matchingCallSites != expectedMatchingCallSites)
                {
                    state.SawFailure = true;
                }

                providerActivated = AreAllSurfacesHealthyLocked();
            }
        }

        internal static void ReportFailure(TransportPatchSurface surface)
        {
            lock (SyncRoot)
            {
                States[surface].SawFailure = true;
                providerActivated = false;
            }
        }

        internal static bool TryActivateProvider(out string errorMessage)
        {
            lock (SyncRoot)
            {
                providerActivated = AreAllSurfacesHealthyLocked();
                if (!providerActivated)
                {
                    errorMessage = "Required transport patch surfaces are not healthy.";
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }
        }

        private static bool AreAllSurfacesHealthyLocked()
        {
            return IsSurfaceHealthyLocked(
                       TransportPatchSurface.SavedDataWrite,
                       ExpectedSavedDataWriteCallerCount,
                       ExpectedSavedDataWriteCallSiteCount) &&
                   IsSurfaceHealthyLocked(
                       TransportPatchSurface.SavedDataRead,
                       ExpectedSavedDataReadCallerCount,
                       ExpectedSavedDataReadCallSiteCount) &&
                   IsSurfaceHealthyLocked(
                       TransportPatchSurface.GlobalDataWrite,
                       ExpectedGlobalDataWriteCallerCount,
                       ExpectedGlobalDataWriteCallSiteCount) &&
                   IsSurfaceHealthyLocked(
                       TransportPatchSurface.GlobalDataRead,
                       ExpectedGlobalDataReadCallerCount,
                       ExpectedGlobalDataReadCallSiteCount);
        }

        internal static TransportPatchHealthSnapshot GetSnapshot()
        {
            lock (SyncRoot)
            {
                return new TransportPatchHealthSnapshot
                {
                    ProviderActivated = providerActivated,
                    SavedDataWrite = BuildSurfaceSnapshotLocked(
                        TransportPatchSurface.SavedDataWrite,
                        ExpectedSavedDataWriteCallerCount,
                        ExpectedSavedDataWriteCallSiteCount),
                    SavedDataRead = BuildSurfaceSnapshotLocked(
                        TransportPatchSurface.SavedDataRead,
                        ExpectedSavedDataReadCallerCount,
                        ExpectedSavedDataReadCallSiteCount),
                    GlobalDataWrite = BuildSurfaceSnapshotLocked(
                        TransportPatchSurface.GlobalDataWrite,
                        ExpectedGlobalDataWriteCallerCount,
                        ExpectedGlobalDataWriteCallSiteCount),
                    GlobalDataRead = BuildSurfaceSnapshotLocked(
                        TransportPatchSurface.GlobalDataRead,
                        ExpectedGlobalDataReadCallerCount,
                        ExpectedGlobalDataReadCallSiteCount)
                };
            }
        }

        private static bool IsSurfaceHealthyLocked(
            TransportPatchSurface surface,
            int expectedCallerCount,
            int expectedCallSiteCount)
        {
            SurfaceState state = States[surface];
            if (state.SawFailure || state.CallerCallSiteCounts.Count != expectedCallerCount)
            {
                return false;
            }

            int observedCallSites = 0;
            foreach (KeyValuePair<string, int> pair in state.CallerCallSiteCounts)
            {
                observedCallSites += pair.Value;
            }

            return observedCallSites == expectedCallSiteCount;
        }

        private static TransportSurfaceHealthSnapshot BuildSurfaceSnapshotLocked(
            TransportPatchSurface surface,
            int expectedCallerCount,
            int expectedCallSiteCount)
        {
            SurfaceState state = States[surface];
            int observedCallSites = 0;
            foreach (KeyValuePair<string, int> pair in state.CallerCallSiteCounts)
            {
                observedCallSites += pair.Value;
            }

            return new TransportSurfaceHealthSnapshot
            {
                ExpectedCallerCount = expectedCallerCount,
                ObservedCallerCount = state.CallerCallSiteCounts.Count,
                ExpectedCallSiteCount = expectedCallSiteCount,
                ObservedCallSiteCount = observedCallSites,
                SawFailure = state.SawFailure,
                Healthy = !state.SawFailure &&
                    state.CallerCallSiteCounts.Count == expectedCallerCount &&
                    observedCallSites == expectedCallSiteCount
            };
        }

        private sealed class SurfaceState
        {
            internal readonly Dictionary<string, int> CallerCallSiteCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            internal bool SawFailure;
        }
    }

    internal sealed class TransportPatchHealthSnapshot
    {
        internal bool ProviderActivated;
        internal TransportSurfaceHealthSnapshot SavedDataWrite;
        internal TransportSurfaceHealthSnapshot SavedDataRead;
        internal TransportSurfaceHealthSnapshot GlobalDataWrite;
        internal TransportSurfaceHealthSnapshot GlobalDataRead;
    }

    internal sealed class TransportSurfaceHealthSnapshot
    {
        internal int ExpectedCallerCount;
        internal int ObservedCallerCount;
        internal int ExpectedCallSiteCount;
        internal int ObservedCallSiteCount;
        internal bool SawFailure;
        internal bool Healthy;
    }
}
