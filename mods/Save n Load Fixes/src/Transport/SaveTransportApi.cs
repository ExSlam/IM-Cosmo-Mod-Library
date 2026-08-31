using System;
using System.Threading;
using SaveNLoadFixes.Transport;

namespace SaveNLoadFixes
{
    /// <summary>
    /// Public coordination surface for Save n Load Fixes' embedded ordered transport.
    /// The provider is authoritative only after all audited SavedData/GlobalData
    /// caller-level read/write patches report healthy interception.
    ///
    /// Legacy SWOF and legacy IMDataCore compatibility adapters are intentionally not
    /// implemented in Sprint 1B. They are deferred until the companion mods are updated.
    /// </summary>
    public static class SaveTransportApi
    {
        public static int Version
        {
            get { return SaveNLoadFixesConstants.TransportApiVersion; }
        }

        public static bool IsAuthoritativeTransport
        {
            get { return TransportPatchHealth.ProviderActivated; }
        }

        public static string TransportOwner
        {
            get
            {
                return IsAuthoritativeTransport
                    ? SaveNLoadFixesConstants.HarmonyId
                    : string.Empty;
            }
        }

        public static bool HasPendingWrites(string absoluteSavePath)
        {
            if (!IsAuthoritativeTransport)
            {
                return false;
            }

            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSavePath,
                    out normalizedPath))
            {
                return false;
            }

            return OrderedSaveTransport.HasPendingWrites(normalizedPath);
        }

        public static bool SavedDataInterceptionHealthy
        {
            get
            {
                TransportPatchHealthSnapshot snapshot = TransportPatchHealth.GetSnapshot();
                return snapshot.ProviderActivated &&
                    snapshot.SavedDataWrite.Healthy &&
                    snapshot.SavedDataRead.Healthy;
            }
        }

        public static bool GlobalDataInterceptionHealthy
        {
            get
            {
                TransportPatchHealthSnapshot snapshot = TransportPatchHealth.GetSnapshot();
                return snapshot.ProviderActivated &&
                    snapshot.GlobalDataWrite.Healthy &&
                    snapshot.GlobalDataRead.Healthy;
            }
        }

        public static bool TryGetDiagnostics(
            out SaveTransportDiagnostics diagnostics,
            out string errorMessage)
        {
            TransportPatchHealthSnapshot snapshot = TransportPatchHealth.GetSnapshot();
            diagnostics = SaveTransportDiagnostics.FromSnapshot(snapshot);
            errorMessage = string.Empty;
            return true;
        }

        public static bool TryWaitForPendingWrites(
            string absoluteSavePath,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!RequireAuthoritativeProvider(out errorMessage) ||
                !IsValidTimeout(timeoutMilliseconds))
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "timeoutMilliseconds must be -1 or greater.";
                }
                return false;
            }

            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSavePath,
                    out normalizedPath))
            {
                errorMessage = "absoluteSavePath must be a valid absolute path.";
                return false;
            }

            if (!OrderedSaveTransport.WaitForPath(
                    normalizedPath,
                    timeoutMilliseconds))
            {
                errorMessage =
                    "Timed out waiting for pending Save n Load Fixes ordered writes.";
                return false;
            }

            return true;
        }

        public static bool TryRunExclusiveFileAccess(
            string absoluteSavePath,
            Action fileAction,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!RequireAuthoritativeProvider(out errorMessage))
            {
                return false;
            }

            if (fileAction == null)
            {
                errorMessage = "fileAction cannot be null.";
                return false;
            }

            if (!IsValidTimeout(timeoutMilliseconds))
            {
                errorMessage = "timeoutMilliseconds must be -1 or greater.";
                return false;
            }

            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSavePath,
                    out normalizedPath))
            {
                errorMessage = "absoluteSavePath must be a valid absolute path.";
                return false;
            }

            return OrderedSaveTransport.TryRunExclusiveFileAccess(
                normalizedPath,
                fileAction,
                timeoutMilliseconds,
                out errorMessage);
        }

        public static bool TryAcquireExclusiveDirectoryAccess(
            string absoluteSaveDirectory,
            int timeoutMilliseconds,
            out IDisposable lease,
            out string errorMessage)
        {
            lease = null;
            errorMessage = string.Empty;

            if (!RequireAuthoritativeProvider(out errorMessage))
            {
                return false;
            }

            if (!IsValidTimeout(timeoutMilliseconds))
            {
                errorMessage = "timeoutMilliseconds must be -1 or greater.";
                return false;
            }

            string normalizedDirectoryPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(
                    absoluteSaveDirectory,
                    out normalizedDirectoryPath))
            {
                errorMessage =
                    "absoluteSaveDirectory must be a valid absolute path.";
                return false;
            }

            return OrderedSaveTransport.TryAcquireExclusiveDirectoryAccess(
                normalizedDirectoryPath,
                timeoutMilliseconds,
                out lease,
                out errorMessage);
        }

        private static bool RequireAuthoritativeProvider(out string errorMessage)
        {
            if (IsAuthoritativeTransport)
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = SaveNLoadFixesConstants.TransportNotActiveMessage;
            return false;
        }

        private static bool IsValidTimeout(int timeoutMilliseconds)
        {
            return timeoutMilliseconds == Timeout.Infinite ||
                   timeoutMilliseconds >= 0;
        }
    }

    public sealed class SaveTransportDiagnostics
    {
        public string ModVersion { get; private set; }
        public string DevelopmentStage { get; private set; }
        public int ApiVersion { get; private set; }
        public bool ProviderActivated { get; private set; }
        public SaveTransportSurfaceDiagnostics SavedDataWrite { get; private set; }
        public SaveTransportSurfaceDiagnostics SavedDataRead { get; private set; }
        public SaveTransportSurfaceDiagnostics GlobalDataWrite { get; private set; }
        public SaveTransportSurfaceDiagnostics GlobalDataRead { get; private set; }

        internal static SaveTransportDiagnostics FromSnapshot(
            TransportPatchHealthSnapshot snapshot)
        {
            return new SaveTransportDiagnostics
            {
                ModVersion = SaveNLoadFixesConstants.Version,
                DevelopmentStage = SaveNLoadFixesConstants.DevelopmentStage,
                ApiVersion = SaveNLoadFixesConstants.TransportApiVersion,
                ProviderActivated = snapshot.ProviderActivated,
                SavedDataWrite = SaveTransportSurfaceDiagnostics.FromSnapshot(snapshot.SavedDataWrite),
                SavedDataRead = SaveTransportSurfaceDiagnostics.FromSnapshot(snapshot.SavedDataRead),
                GlobalDataWrite = SaveTransportSurfaceDiagnostics.FromSnapshot(snapshot.GlobalDataWrite),
                GlobalDataRead = SaveTransportSurfaceDiagnostics.FromSnapshot(snapshot.GlobalDataRead)
            };
        }
    }

    public sealed class SaveTransportSurfaceDiagnostics
    {
        public int ExpectedCallerCount { get; private set; }
        public int ObservedCallerCount { get; private set; }
        public int ExpectedCallSiteCount { get; private set; }
        public int ObservedCallSiteCount { get; private set; }
        public bool SawFailure { get; private set; }
        public bool Healthy { get; private set; }

        internal static SaveTransportSurfaceDiagnostics FromSnapshot(
            TransportSurfaceHealthSnapshot snapshot)
        {
            return new SaveTransportSurfaceDiagnostics
            {
                ExpectedCallerCount = snapshot.ExpectedCallerCount,
                ObservedCallerCount = snapshot.ObservedCallerCount,
                ExpectedCallSiteCount = snapshot.ExpectedCallSiteCount,
                ObservedCallSiteCount = snapshot.ObservedCallSiteCount,
                SawFailure = snapshot.SawFailure,
                Healthy = snapshot.Healthy
            };
        }
    }
}
