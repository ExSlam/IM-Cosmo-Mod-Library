using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using SaveNLoadFixes.Repairs;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Persistence
{
    /// <summary>
    /// Freezes the repair envelope with the caller-thread vanilla DTO and associates
    /// extracted envelope state with the exact SavedData instance returned by a read.
    /// The association is weak so save-list inspection cannot create a process-lifetime
    /// cache of historical DTOs.
    /// </summary>
    internal static class RepairEnvelopeTransport
    {
        private static readonly object RegistrySync = new object();
        private sealed class RegisteredContentFingerprint
        {
            internal string Value = string.Empty;
        }

        private static readonly ConditionalWeakTable<SaveManager.SavedData, RepairEnvelopeLoadState>
            LoadStates = new ConditionalWeakTable<SaveManager.SavedData, RepairEnvelopeLoadState>();
        private static readonly ConditionalWeakTable<SaveManager.SavedData, RegisteredContentFingerprint>
            RegisteredContentFingerprints =
                new ConditionalWeakTable<SaveManager.SavedData, RegisteredContentFingerprint>();

        private static long frozenCheckpointCount;
        private static long envelopeReadCount;
        private static long simpleJsonRecoveredReadCount;
        private static long legacyReadCount;
        private static long invalidEnvelopeReadCount;
        private static long freezeFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static long FrozenCheckpointCount { get { return Interlocked.Read(ref frozenCheckpointCount); } }
        internal static long EnvelopeReadCount { get { return Interlocked.Read(ref envelopeReadCount); } }
        internal static long SimpleJsonRecoveredReadCount { get { return Interlocked.Read(ref simpleJsonRecoveredReadCount); } }
        internal static long LegacyReadCount { get { return Interlocked.Read(ref legacyReadCount); } }
        internal static long InvalidEnvelopeReadCount { get { return Interlocked.Read(ref invalidEnvelopeReadCount); } }
        internal static long FreezeFailureCount { get { return Interlocked.Read(ref freezeFailureCount); } }
        internal static string LastDiagnostic { get { lock (RegistrySync) { return lastDiagnostic; } } }

        /// <summary>
        /// Registers IM Data Core's exact save-side content fingerprint against the
        /// SavedData instance that SNLF is about to freeze. The registration is weak
        /// and one-shot: TryFreezeSavedDataPayload consumes it into the repair envelope.
        /// </summary>
        internal static bool TryRegisterSavedDataContentFingerprint(
            SaveManager.SavedData savedData,
            string fingerprint,
            out string error)
        {
            error = string.Empty;
            if (savedData == null)
            {
                error = "SavedData is null.";
                return false;
            }
            if (!RepairEnvelopeContentFingerprint.IsValid(fingerprint))
            {
                error = "The supplied content fingerprint is not a canonical SHA-256 witness.";
                return false;
            }

            lock (RegistrySync)
            {
                RegisteredContentFingerprints.Remove(savedData);
                RegisteredContentFingerprints.Add(
                    savedData,
                    new RegisteredContentFingerprint { Value = fingerprint });
            }
            return true;
        }

        /// <summary>
        /// Returns the checkpoint witness carried by the validated SNLF envelope. A
        /// legacy flag is also returned for a validated pre-bridge envelope that has
        /// no IMDC witness yet. IMDC may use that flag for one conservative, unique-
        /// candidate migration without weakening ordinary exact matching afterward.
        /// </summary>
        internal static bool TryGetLoadedSavedDataCheckpointIdentity(
            SaveManager.SavedData savedData,
            out string fingerprint,
            out bool legacyEnvelopeWithoutFingerprint,
            out string error)
        {
            fingerprint = string.Empty;
            legacyEnvelopeWithoutFingerprint = false;
            error = string.Empty;
            if (savedData == null)
            {
                error = "SavedData is null.";
                return false;
            }

            lock (RegistrySync)
            {
                RepairEnvelopeLoadState state;
                if (!LoadStates.TryGetValue(savedData, out state) ||
                    state == null || !state.Present || !state.Valid ||
                    state.Envelope == null)
                {
                    error =
                        "SavedData does not have a validated SNLF repair-envelope load state.";
                    return false;
                }

                string stored = state.Envelope.imdc_content_fingerprint ?? string.Empty;
                if (RepairEnvelopeContentFingerprint.IsValid(stored))
                {
                    fingerprint = stored;
                    return true;
                }

                legacyEnvelopeWithoutFingerprint = string.IsNullOrEmpty(stored);
                return true;
            }
        }

        internal static bool TryFreezeSavedDataPayload(
            SaveManager.SavedData dataToSave,
            bool isJson,
            out string payload,
            out string checkpointId,
            out string error)
        {
            payload = string.Empty;
            checkpointId = string.Empty;
            error = string.Empty;

            if (!isJson)
            {
                return FreezeFailed("Repair-dependent SavedData writes must use JSON.", out error);
            }

            RepairEnvelopeV1 envelope;
            if (!RepairEnvelopeBuilder.TryBuild(dataToSave, out envelope, out error))
            {
                Interlocked.Increment(ref freezeFailureCount);
                SetDiagnostic("Repair envelope capture failed: " + error);
                return false;
            }

            lock (RegistrySync)
            {
                RegisteredContentFingerprint registered;
                if (RegisteredContentFingerprints.TryGetValue(
                        dataToSave,
                        out registered) &&
                    registered != null &&
                    RepairEnvelopeContentFingerprint.IsValid(registered.Value))
                {
                    envelope.imdc_content_fingerprint = registered.Value;
                }
                RegisteredContentFingerprints.Remove(dataToSave);
            }

            string vanillaJson;
            try
            {
                vanillaJson = JsonUtility.ToJson(dataToSave, true);
            }
            catch (Exception exception)
            {
                return FreezeFailed(
                    "Caller-thread SavedData serialization failed: " + exception.Message,
                    out error);
            }

            if (!RepairEnvelopeCodec.TryInject(
                    vanillaJson,
                    envelope,
                    out payload,
                    out error))
            {
                Interlocked.Increment(ref freezeFailureCount);
                SetDiagnostic("Repair envelope injection failed: " + error);
                return false;
            }

            checkpointId = envelope.checkpoint_id;
            Interlocked.Increment(ref frozenCheckpointCount);
            SetDiagnostic("Frozen one SavedData request with SNLF checkpoint " + checkpointId + ".");
            SnsMessageRepair.ReportSerializedCheckpointSuccess(
                checkpointId,
                envelope.records.sns_message_nodes.Count);
            return true;
        }

        internal static SaveManager.SavedData LoadSavedDataFromPhysicalPath(
            string physicalPath)
        {
            if (string.IsNullOrEmpty(physicalPath))
            {
                return null;
            }

            if (!Directory.Exists(Path.GetDirectoryName(physicalPath)))
            {
                Debug.LogWarning("Directory does not exist");
                return null;
            }

            if (!File.Exists(physicalPath))
            {
                Debug.Log("File does not exist: " + physicalPath);
                return null;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(physicalPath);
                Debug.Log("Loaded Data from: " + physicalPath.Replace("/", "\\"));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed To Load Data from: " + physicalPath.Replace("/", "\\"));
                Debug.LogWarning("Error: " + exception.Message);
                return null;
            }

            string fullJson;
            try
            {
                fullJson = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                fullJson = Encoding.ASCII.GetString(bytes);
            }

            string vanillaJson;
            RepairEnvelopeLoadState state;
            string fatalError;
            if (!RepairEnvelopeCodec.TryExtractAndStrip(
                    fullJson,
                    out vanillaJson,
                    out state,
                    out fatalError))
            {
                Interlocked.Increment(ref invalidEnvelopeReadCount);
                SetDiagnostic("SavedData repair-root extraction failed: " + fatalError);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
                return null;
            }

            SaveManager.SavedData loaded;
            try
            {
                loaded = JsonUtility.FromJson<SaveManager.SavedData>(vanillaJson);
            }
            catch (Exception exception)
            {
                SetDiagnostic("Vanilla SavedData deserialization failed after repair-root stripping: " + exception.Message);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
                return null;
            }

            if (loaded == null)
            {
                return null;
            }

            state.PhysicalPath = physicalPath;

            lock (RegistrySync)
            {
                LoadStates.Add(loaded, state);
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacyReadCount);
                SetDiagnostic("Loaded a legacy/pre-envelope SavedData object.");
            }
            else if (!state.Valid)
            {
                Interlocked.Increment(ref invalidEnvelopeReadCount);
                SetDiagnostic("Loaded SavedData with an invalid SNLF repair envelope: " + state.Error);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
            }
            else
            {
                Interlocked.Increment(ref envelopeReadCount);
                if (state.RecoveredFromSimpleJsonRewrite)
                {
                    Interlocked.Increment(ref simpleJsonRecoveredReadCount);
                    SetDiagnostic(
                        "Recovered SNLF checkpoint " + state.Envelope.checkpoint_id +
                        " from vanilla FixSaveFile's uniform SimpleJSON scalar rewrite; " +
                        "every typed value passed canonical exactness checks. The next save " +
                        "will write the normal typed JSON form.");
                    Debug.LogWarning(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
                }
                else
                {
                    SetDiagnostic("Loaded SavedData with SNLF checkpoint " + state.Envelope.checkpoint_id + ".");
                }
            }

            return loaded;
        }

        internal static bool TryGetLoadState(
            SaveManager.SavedData loaded,
            out RepairEnvelopeLoadState state)
        {
            state = null;
            if (loaded == null)
            {
                return false;
            }

            lock (RegistrySync)
            {
                return LoadStates.TryGetValue(loaded, out state);
            }
        }

        private static bool FreezeFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown repair-envelope freeze failure";
            Interlocked.Increment(ref freezeFailureCount);
            SetDiagnostic(error);
            return false;
        }

        private static void SetDiagnostic(string value)
        {
            lock (RegistrySync)
            {
                lastDiagnostic = value ?? string.Empty;
            }
        }
    }
}
