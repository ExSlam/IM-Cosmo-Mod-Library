using System;
using System.Threading;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Transport;

namespace SaveNLoadFixes
{
    /// <summary>
    /// Public coordination surface for Save n Load Fixes' embedded ordered transport.
    /// The provider is authoritative only after all audited SavedData/GlobalData
    /// caller-level read/write patches and the type-preserving startup migration
    /// report healthy interception.
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
                    snapshot.SavedDataRead.Healthy &&
                    snapshot.StartupSavedDataMigration.Healthy;
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

        /// <summary>
        /// Requires the next SNLF freeze of this exact SavedData instance to carry
        /// an IM Data Core durable checkpoint witness. IMDC calls this before its
        /// save-boundary work begins. If that work does not later publish the exact
        /// fingerprint, SNLF refuses to write a vanilla checkpoint that IMDC cannot
        /// match durably.
        /// </summary>
        public static bool TryRequireSavedDataCheckpointWitness(
            SaveManager.SavedData savedData,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!RequireAuthoritativeProvider(out errorMessage))
            {
                return false;
            }

            return RepairEnvelopeTransport.TryRequireSavedDataContentFingerprint(
                savedData,
                out errorMessage);
        }

        /// <summary>
        /// Associates IM Data Core's exact SavedData fingerprint with the next SNLF
        /// freeze of this object. IMDC calls this only after its matching checkpoint
        /// has been durably persisted. The witness is stored inside SNLF's repair
        /// envelope and therefore survives vanilla FixSaveFile's startup rewrite.
        /// </summary>
        public static bool TryRegisterSavedDataContentFingerprint(
            SaveManager.SavedData savedData,
            string fingerprint,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!RequireAuthoritativeProvider(out errorMessage))
            {
                return false;
            }

            return RepairEnvelopeTransport.TryRegisterSavedDataContentFingerprint(
                savedData,
                fingerprint,
                out errorMessage);
        }

        /// <summary>
        /// Reads the IMDC checkpoint witness from the validated repair envelope bound
        /// to this loaded SavedData object. The legacy flag is true only for a
        /// validated older envelope that predates this fingerprint bridge.
        /// </summary>
        public static bool TryGetLoadedSavedDataCheckpointIdentity(
            SaveManager.SavedData savedData,
            out string fingerprint,
            out bool legacyEnvelopeWithoutFingerprint,
            out string errorMessage)
        {
            fingerprint = string.Empty;
            legacyEnvelopeWithoutFingerprint = false;
            errorMessage = string.Empty;
            if (!RequireAuthoritativeProvider(out errorMessage))
            {
                return false;
            }

            return RepairEnvelopeTransport.TryGetLoadedSavedDataCheckpointIdentity(
                savedData,
                out fingerprint,
                out legacyEnvelopeWithoutFingerprint,
                out errorMessage);
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
        public SaveTransportSurfaceDiagnostics StartupSavedDataMigration { get; private set; }

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
                GlobalDataRead = SaveTransportSurfaceDiagnostics.FromSnapshot(snapshot.GlobalDataRead),
                StartupSavedDataMigration = SaveTransportSurfaceDiagnostics.FromSnapshot(
                    snapshot.StartupSavedDataMigration)
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
