using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A02: preserves the private diminishing-return ordinal used by project progress
    /// parameter Add(...) methods. Vanilla aliases each live parameter list into the
    /// SavedData DTO, but Unity JsonUtility omits the private counter field. The exact
    /// ordinal is therefore captured into the SNLF envelope before JSON serialization.
    ///
    /// Records are sparse: a missing key in a present A02 section means counter == 0.
    /// A pre-A02 save also deterministically restores all audited counters to zero.
    /// Public val/progress fields are never used to guess an ordinal.
    /// </summary>
    internal static class ProjectProgressCounterRepair
    {
        internal const int SectionVersion = 1;

        internal enum OwnerKind
        {
            Single = 1,
            Show = 2,
            Concert = 3,
            Tour = 4,
            Ssk = 5
        }

        private static readonly FieldInfo SingleCounterField =
            AccessTools.Field(typeof(singles._single._param), "counter");
        private static readonly FieldInfo ShowCounterField =
            AccessTools.Field(typeof(Shows._show._progressable), "counter");
        private static readonly FieldInfo ConcertCounterField =
            AccessTools.Field(typeof(SEvent_Concerts._concert._progressable), "counter");
        private static readonly FieldInfo TourCounterField =
            AccessTools.Field(typeof(SEvent_Tour.tour._progressable), "counter");
        private static readonly FieldInfo SskCounterField =
            AccessTools.Field(typeof(SEvent_SSK._SSK._progressable), "counter");

        private static long captureFailureCount;
        private static long restoredLoadCount;
        private static long restoredParameterCount;
        private static long restoredNonZeroCounterCount;
        private static long legacyZeroFallbackLoadCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return CounterFieldsResolved && ProjectProgressCounterPatchHealth.IsHealthy;
            }
        }

        internal static bool CounterFieldsResolved
        {
            get
            {
                return SingleCounterField != null &&
                    ShowCounterField != null &&
                    ConcertCounterField != null &&
                    TourCounterField != null &&
                    SskCounterField != null;
            }
        }

        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredParameterCount { get { return Interlocked.Read(ref restoredParameterCount); } }
        internal static long RestoredNonZeroCounterCount { get { return Interlocked.Read(ref restoredNonZeroCounterCount); } }
        internal static long LegacyZeroFallbackLoadCount { get { return Interlocked.Read(ref legacyZeroFallbackLoadCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            records = new List<ProjectProgressCounterRecordV1>();
            error = string.Empty;

            if (dataToSave == null)
            {
                return CaptureFailed("A02 cannot capture counters from a null SavedData request.", out error);
            }

            if (!CounterFieldsResolved)
            {
                return CaptureFailed("A02 could not resolve all five audited private counter fields.", out error);
            }

            if (!CaptureSingles(dataToSave.singles__Singles, records, out error) ||
                !CaptureShows(dataToSave.shows__Shows, records, out error) ||
                !CaptureConcerts(dataToSave.SEvent_Concert__Concerts, records, out error) ||
                !CaptureTours(dataToSave.SEvent_Tour__Tours, records, out error) ||
                !CaptureSsks(dataToSave.SEvent_SSK__Elections, records, out error))
            {
                return false;
            }

            records.Sort(
                delegate(ProjectProgressCounterRecordV1 left, ProjectProgressCounterRecordV1 right)
                {
                    int kindCompare = left.owner_kind.CompareTo(right.owner_kind);
                    if (kindCompare != 0)
                    {
                        return kindCompare;
                    }

                    int ownerCompare = left.owner_id.CompareTo(right.owner_id);
                    return ownerCompare != 0
                        ? ownerCompare
                        : left.parameter_type.CompareTo(right.parameter_type);
                });

            return true;
        }

        internal static void RestoreAfterCareerLoad(SaveManager manager)
        {
            SaveManager.SavedData target = manager == null ? null : manager.Data;
            if (target == null)
            {
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "A02 failed closed because the adopted SavedData object has no repair-envelope read association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            if (!CounterFieldsResolved)
            {
                RecordInvalid("A02 private counter fields are not all resolvable in this game build.");
                return;
            }

            List<CounterAssignment> assignments;
            Dictionary<string, CounterAssignment> assignmentsByKey;
            string structureError;
            if (!TryBuildAssignments(target, out assignments, out assignmentsByKey, out structureError))
            {
                RecordInvalid("A02 target/live progressable structure mismatch: " + structureError);
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.project_progress_counters_version == 0)
            {
                if (!ApplyAssignments(assignments, out structureError))
                {
                    RecordInvalid("A02 could not apply deterministic legacy zero counters: " + structureError);
                    return;
                }

                Interlocked.Increment(ref legacyZeroFallbackLoadCount);
                lastDiagnostic = "A02 loaded a pre-A02 save; all audited project progress counters use the deterministic zero fallback.";
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (!state.Valid ||
                envelopeRecords.project_progress_counters_version != SectionVersion ||
                envelopeRecords.project_progress_counters == null)
            {
                RecordInvalid("A02 repair section is invalid, unsupported, or missing its sparse counter list.");
                return;
            }

            HashSet<string> sparseKeys = new HashSet<string>(StringComparer.Ordinal);
            int nonZeroCount = 0;
            for (int index = 0; index < envelopeRecords.project_progress_counters.Count; index++)
            {
                ProjectProgressCounterRecordV1 record = envelopeRecords.project_progress_counters[index];
                if (record == null ||
                    !IsValidOwnerKind(record.owner_kind) ||
                    record.owner_id < 0 ||
                    !IsValidParameterType(record.owner_kind, record.parameter_type) ||
                    record.counter <= 0)
                {
                    RecordInvalid("A02 sparse counter record contains an invalid owner, parameter type, or non-positive counter.");
                    return;
                }

                string key = BuildKey(record.owner_kind, record.owner_id, record.parameter_type);
                if (!sparseKeys.Add(key))
                {
                    RecordInvalid("A02 sparse counter section contains a duplicate owner/parameter key.");
                    return;
                }

                CounterAssignment assignment;
                if (!assignmentsByKey.TryGetValue(key, out assignment) || assignment == null)
                {
                    RecordInvalid("A02 sparse counter record does not resolve to an exact loaded project parameter.");
                    return;
                }

                assignment.DesiredValue = record.counter;
                nonZeroCount++;
            }

            if (!ApplyAssignments(assignments, out structureError))
            {
                RecordInvalid("A02 validated counter section could not be applied atomically: " + structureError);
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredParameterCount, assignments.Count);
            Interlocked.Add(ref restoredNonZeroCounterCount, nonZeroCount);
            lastDiagnostic = "A02 restored sparse project progress counters for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
        }

        private static bool CaptureSingles(
            List<singles.SinglesData> owners,
            List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            if (owners == null)
            {
                return CaptureFailed("A02 SavedData singles list is null after SaveEvent population.", out error);
            }

            HashSet<int> ownerIds = new HashSet<int>();
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                singles.SinglesData owner = owners[ownerIndex];
                if (owner == null || owner.id < 0 || !ownerIds.Add(owner.id) || owner.parameters == null)
                {
                    return CaptureFailed("A02 SavedData singles contain a null/invalid/duplicate owner or null parameter list.", out error);
                }

                HashSet<int> parameterTypes = new HashSet<int>();
                for (int parameterIndex = 0; parameterIndex < owner.parameters.Count; parameterIndex++)
                {
                    singles._single._param parameter = owner.parameters[parameterIndex];
                    int type = parameter == null ? -1 : (int)parameter.type;
                    if (parameter == null ||
                        !IsValidParameterType((int)OwnerKind.Single, type) ||
                        !parameterTypes.Add(type))
                    {
                        return CaptureFailed("A02 single parameters contain a null, invalid, or duplicate parameter type.", out error);
                    }

                    if (!CaptureCounter(
                            (int)OwnerKind.Single,
                            owner.id,
                            type,
                            parameter,
                            SingleCounterField,
                            records,
                            out error))
                    {
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CaptureShows(
            List<Shows.ShowData> owners,
            List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            if (owners == null)
            {
                return CaptureFailed("A02 SavedData shows list is null after SaveEvent population.", out error);
            }

            HashSet<int> ownerIds = new HashSet<int>();
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                Shows.ShowData owner = owners[ownerIndex];
                if (owner == null || owner.id < 0 || !ownerIds.Add(owner.id) || owner.parameters == null)
                {
                    return CaptureFailed("A02 SavedData shows contain a null/invalid/duplicate owner or null parameter list.", out error);
                }

                HashSet<int> parameterTypes = new HashSet<int>();
                for (int parameterIndex = 0; parameterIndex < owner.parameters.Count; parameterIndex++)
                {
                    Shows._show._progressable parameter = owner.parameters[parameterIndex];
                    int type = parameter == null ? -1 : (int)parameter.type;
                    if (parameter == null ||
                        !IsValidParameterType((int)OwnerKind.Show, type) ||
                        !parameterTypes.Add(type))
                    {
                        return CaptureFailed("A02 show parameters contain a null, invalid, or duplicate parameter type.", out error);
                    }

                    if (!CaptureCounter(
                            (int)OwnerKind.Show,
                            owner.id,
                            type,
                            parameter,
                            ShowCounterField,
                            records,
                            out error))
                    {
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CaptureConcerts(
            List<SEvent_Concerts.ConcertData> owners,
            List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            if (owners == null)
            {
                return CaptureFailed("A02 SavedData concerts list is null after SaveEvent population.", out error);
            }

            HashSet<int> ownerIds = new HashSet<int>();
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                SEvent_Concerts.ConcertData owner = owners[ownerIndex];
                if (owner == null || owner.ID < 0 || !ownerIds.Add(owner.ID) || owner.parameters == null)
                {
                    return CaptureFailed("A02 SavedData concerts contain a null/invalid/duplicate owner or null parameter list.", out error);
                }

                HashSet<int> parameterTypes = new HashSet<int>();
                for (int parameterIndex = 0; parameterIndex < owner.parameters.Count; parameterIndex++)
                {
                    SEvent_Concerts._concert._progressable parameter = owner.parameters[parameterIndex];
                    int type = parameter == null ? -1 : (int)parameter.type;
                    if (parameter == null ||
                        !IsValidParameterType((int)OwnerKind.Concert, type) ||
                        !parameterTypes.Add(type))
                    {
                        return CaptureFailed("A02 concert parameters contain a null, invalid, or duplicate parameter type.", out error);
                    }

                    if (!CaptureCounter(
                            (int)OwnerKind.Concert,
                            owner.ID,
                            type,
                            parameter,
                            ConcertCounterField,
                            records,
                            out error))
                    {
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CaptureTours(
            List<SEvent_Tour.TourData> owners,
            List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            if (owners == null)
            {
                return CaptureFailed("A02 SavedData tours list is null after SaveEvent population.", out error);
            }

            HashSet<int> ownerIds = new HashSet<int>();
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                SEvent_Tour.TourData owner = owners[ownerIndex];
                if (owner == null || owner.ID < 0 || !ownerIds.Add(owner.ID) || owner.parameters == null)
                {
                    return CaptureFailed("A02 SavedData tours contain a null/invalid/duplicate owner or null parameter list.", out error);
                }

                HashSet<int> parameterTypes = new HashSet<int>();
                for (int parameterIndex = 0; parameterIndex < owner.parameters.Count; parameterIndex++)
                {
                    SEvent_Tour.tour._progressable parameter = owner.parameters[parameterIndex];
                    int type = parameter == null ? -1 : (int)parameter.type;
                    if (parameter == null ||
                        !IsValidParameterType((int)OwnerKind.Tour, type) ||
                        !parameterTypes.Add(type))
                    {
                        return CaptureFailed("A02 tour parameters contain a null, invalid, or duplicate parameter type.", out error);
                    }

                    if (!CaptureCounter(
                            (int)OwnerKind.Tour,
                            owner.ID,
                            type,
                            parameter,
                            TourCounterField,
                            records,
                            out error))
                    {
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CaptureSsks(
            List<SEvent_SSK.SSKData> owners,
            List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            if (owners == null)
            {
                return CaptureFailed("A02 SavedData SSK list is null after SaveEvent population.", out error);
            }

            HashSet<int> ownerIds = new HashSet<int>();
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                SEvent_SSK.SSKData owner = owners[ownerIndex];
                if (owner == null || owner.ID < 0 || !ownerIds.Add(owner.ID) || owner.parameters == null)
                {
                    return CaptureFailed("A02 SavedData SSK rows contain a null/invalid/duplicate owner or null parameter list.", out error);
                }

                HashSet<int> parameterTypes = new HashSet<int>();
                for (int parameterIndex = 0; parameterIndex < owner.parameters.Count; parameterIndex++)
                {
                    SEvent_SSK._SSK._progressable parameter = owner.parameters[parameterIndex];
                    int type = parameter == null ? -1 : (int)parameter.type;
                    if (parameter == null ||
                        !IsValidParameterType((int)OwnerKind.Ssk, type) ||
                        !parameterTypes.Add(type))
                    {
                        return CaptureFailed("A02 SSK parameters contain a null, invalid, or duplicate parameter type.", out error);
                    }

                    if (!CaptureCounter(
                            (int)OwnerKind.Ssk,
                            owner.ID,
                            type,
                            parameter,
                            SskCounterField,
                            records,
                            out error))
                    {
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CaptureCounter(
            int ownerKind,
            int ownerId,
            int parameterType,
            object parameter,
            FieldInfo field,
            List<ProjectProgressCounterRecordV1> records,
            out string error)
        {
            int counter;
            if (!TryReadCounter(parameter, field, out counter) || counter < 0)
            {
                return CaptureFailed("A02 could not read a non-negative private project progress counter.", out error);
            }

            if (counter > 0)
            {
                records.Add(
                    new ProjectProgressCounterRecordV1
                    {
                        owner_kind = ownerKind,
                        owner_id = ownerId,
                        parameter_type = parameterType,
                        counter = counter
                    });
            }

            error = string.Empty;
            return true;
        }

        private static bool TryBuildAssignments(
            SaveManager.SavedData target,
            out List<CounterAssignment> assignments,
            out Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            assignments = new List<CounterAssignment>();
            byKey = new Dictionary<string, CounterAssignment>(StringComparer.Ordinal);
            error = string.Empty;

            return BuildSingleAssignments(target.singles__Singles, assignments, byKey, out error) &&
                BuildShowAssignments(target.shows__Shows, assignments, byKey, out error) &&
                BuildConcertAssignments(target.SEvent_Concert__Concerts, assignments, byKey, out error) &&
                BuildTourAssignments(target.SEvent_Tour__Tours, assignments, byKey, out error) &&
                BuildSskAssignments(target.SEvent_SSK__Elections, assignments, byKey, out error);
        }

        private static bool BuildSingleAssignments(
            List<singles.SinglesData> savedOwners,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            if (savedOwners == null || singles.Singles == null || savedOwners.Count != singles.Singles.Count)
            {
                error = "single owner count/list mismatch";
                return false;
            }

            Dictionary<int, singles._single> liveById = new Dictionary<int, singles._single>();
            for (int index = 0; index < singles.Singles.Count; index++)
            {
                singles._single live = singles.Singles[index];
                if (live == null || live.id < 0 || liveById.ContainsKey(live.id))
                {
                    error = "live singles contain a null, invalid, or duplicate ID";
                    return false;
                }
                liveById.Add(live.id, live);
            }

            HashSet<int> savedIds = new HashSet<int>();
            for (int index = 0; index < savedOwners.Count; index++)
            {
                singles.SinglesData saved = savedOwners[index];
                singles._single live;
                if (saved == null || saved.id < 0 || !savedIds.Add(saved.id) ||
                    !liveById.TryGetValue(saved.id, out live) || live == null ||
                    saved.parameters == null || live.parameters == null ||
                    !object.ReferenceEquals(saved.parameters, live.parameters))
                {
                    error = "saved single did not resolve to the exact reconstructed parameter list";
                    return false;
                }

                if (!BuildParameterAssignments(
                        (int)OwnerKind.Single,
                        saved.id,
                        saved.parameters,
                        SingleCounterField,
                        assignments,
                        byKey,
                        out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool BuildShowAssignments(
            List<Shows.ShowData> savedOwners,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            if (savedOwners == null || Shows.shows == null || savedOwners.Count != Shows.shows.Count)
            {
                error = "show owner count/list mismatch";
                return false;
            }

            Dictionary<int, Shows._show> liveById = new Dictionary<int, Shows._show>();
            for (int index = 0; index < Shows.shows.Count; index++)
            {
                Shows._show live = Shows.shows[index];
                if (live == null || live.id < 0 || liveById.ContainsKey(live.id))
                {
                    error = "live shows contain a null, invalid, or duplicate ID";
                    return false;
                }
                liveById.Add(live.id, live);
            }

            HashSet<int> savedIds = new HashSet<int>();
            for (int index = 0; index < savedOwners.Count; index++)
            {
                Shows.ShowData saved = savedOwners[index];
                Shows._show live;
                if (saved == null || saved.id < 0 || !savedIds.Add(saved.id) ||
                    !liveById.TryGetValue(saved.id, out live) || live == null ||
                    saved.parameters == null || live.parameters == null ||
                    !object.ReferenceEquals(saved.parameters, live.parameters))
                {
                    error = "saved show did not resolve to the exact reconstructed parameter list";
                    return false;
                }

                if (!BuildParameterAssignments(
                        (int)OwnerKind.Show,
                        saved.id,
                        saved.parameters,
                        ShowCounterField,
                        assignments,
                        byKey,
                        out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool BuildConcertAssignments(
            List<SEvent_Concerts.ConcertData> savedOwners,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            if (savedOwners == null || SEvent_Concerts.Concerts == null || savedOwners.Count != SEvent_Concerts.Concerts.Count)
            {
                error = "concert owner count/list mismatch";
                return false;
            }

            Dictionary<int, SEvent_Concerts._concert> liveById = new Dictionary<int, SEvent_Concerts._concert>();
            for (int index = 0; index < SEvent_Concerts.Concerts.Count; index++)
            {
                SEvent_Concerts._concert live = SEvent_Concerts.Concerts[index];
                if (live == null || live.ID < 0 || liveById.ContainsKey(live.ID))
                {
                    error = "live concerts contain a null, invalid, or duplicate ID";
                    return false;
                }
                liveById.Add(live.ID, live);
            }

            HashSet<int> savedIds = new HashSet<int>();
            for (int index = 0; index < savedOwners.Count; index++)
            {
                SEvent_Concerts.ConcertData saved = savedOwners[index];
                SEvent_Concerts._concert live;
                if (saved == null || saved.ID < 0 || !savedIds.Add(saved.ID) ||
                    !liveById.TryGetValue(saved.ID, out live) || live == null ||
                    saved.parameters == null || live.parameters == null ||
                    !object.ReferenceEquals(saved.parameters, live.parameters))
                {
                    error = "saved concert did not resolve to the exact reconstructed parameter list";
                    return false;
                }

                if (!BuildParameterAssignments(
                        (int)OwnerKind.Concert,
                        saved.ID,
                        saved.parameters,
                        ConcertCounterField,
                        assignments,
                        byKey,
                        out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool BuildTourAssignments(
            List<SEvent_Tour.TourData> savedOwners,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            if (savedOwners == null || SEvent_Tour.Tours == null || savedOwners.Count != SEvent_Tour.Tours.Count)
            {
                error = "tour owner count/list mismatch";
                return false;
            }

            Dictionary<int, SEvent_Tour.tour> liveById = new Dictionary<int, SEvent_Tour.tour>();
            for (int index = 0; index < SEvent_Tour.Tours.Count; index++)
            {
                SEvent_Tour.tour live = SEvent_Tour.Tours[index];
                if (live == null || live.ID < 0 || liveById.ContainsKey(live.ID))
                {
                    error = "live tours contain a null, invalid, or duplicate ID";
                    return false;
                }
                liveById.Add(live.ID, live);
            }

            HashSet<int> savedIds = new HashSet<int>();
            for (int index = 0; index < savedOwners.Count; index++)
            {
                SEvent_Tour.TourData saved = savedOwners[index];
                SEvent_Tour.tour live;
                if (saved == null || saved.ID < 0 || !savedIds.Add(saved.ID) ||
                    !liveById.TryGetValue(saved.ID, out live) || live == null ||
                    saved.parameters == null || live.parameters == null ||
                    !object.ReferenceEquals(saved.parameters, live.parameters))
                {
                    error = "saved tour did not resolve to the exact reconstructed parameter list";
                    return false;
                }

                if (!BuildParameterAssignments(
                        (int)OwnerKind.Tour,
                        saved.ID,
                        saved.parameters,
                        TourCounterField,
                        assignments,
                        byKey,
                        out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool BuildSskAssignments(
            List<SEvent_SSK.SSKData> savedOwners,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            if (savedOwners == null || SEvent_SSK.Elections == null || savedOwners.Count != SEvent_SSK.Elections.Count)
            {
                error = "SSK owner count/list mismatch";
                return false;
            }

            Dictionary<int, SEvent_SSK._SSK> liveById = new Dictionary<int, SEvent_SSK._SSK>();
            for (int index = 0; index < SEvent_SSK.Elections.Count; index++)
            {
                SEvent_SSK._SSK live = SEvent_SSK.Elections[index];
                if (live == null || live.ID < 0 || liveById.ContainsKey(live.ID))
                {
                    error = "live SSK list contains a null, invalid, or duplicate ID";
                    return false;
                }
                liveById.Add(live.ID, live);
            }

            HashSet<int> savedIds = new HashSet<int>();
            for (int index = 0; index < savedOwners.Count; index++)
            {
                SEvent_SSK.SSKData saved = savedOwners[index];
                SEvent_SSK._SSK live;
                if (saved == null || saved.ID < 0 || !savedIds.Add(saved.ID) ||
                    !liveById.TryGetValue(saved.ID, out live) || live == null ||
                    saved.parameters == null || live.parameters == null ||
                    !object.ReferenceEquals(saved.parameters, live.parameters))
                {
                    error = "saved SSK did not resolve to the exact reconstructed parameter list";
                    return false;
                }

                if (!BuildParameterAssignments(
                        (int)OwnerKind.Ssk,
                        saved.ID,
                        saved.parameters,
                        SskCounterField,
                        assignments,
                        byKey,
                        out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool BuildParameterAssignments(
            int ownerKind,
            int ownerId,
            List<singles._single._param> parameters,
            FieldInfo field,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            HashSet<int> types = new HashSet<int>();
            for (int index = 0; index < parameters.Count; index++)
            {
                singles._single._param parameter = parameters[index];
                int type = parameter == null ? -1 : (int)parameter.type;
                if (parameter == null || !IsValidParameterType(ownerKind, type) || !types.Add(type))
                {
                    error = "single parameter list contains a null, invalid, or duplicate type";
                    return false;
                }
                if (!AddAssignment(ownerKind, ownerId, type, parameter, field, assignments, byKey, out error))
                {
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private static bool BuildParameterAssignments(
            int ownerKind,
            int ownerId,
            List<Shows._show._progressable> parameters,
            FieldInfo field,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            HashSet<int> types = new HashSet<int>();
            for (int index = 0; index < parameters.Count; index++)
            {
                Shows._show._progressable parameter = parameters[index];
                int type = parameter == null ? -1 : (int)parameter.type;
                if (parameter == null || !IsValidParameterType(ownerKind, type) || !types.Add(type))
                {
                    error = "show parameter list contains a null, invalid, or duplicate type";
                    return false;
                }
                if (!AddAssignment(ownerKind, ownerId, type, parameter, field, assignments, byKey, out error))
                {
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private static bool BuildParameterAssignments(
            int ownerKind,
            int ownerId,
            List<SEvent_Concerts._concert._progressable> parameters,
            FieldInfo field,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            HashSet<int> types = new HashSet<int>();
            for (int index = 0; index < parameters.Count; index++)
            {
                SEvent_Concerts._concert._progressable parameter = parameters[index];
                int type = parameter == null ? -1 : (int)parameter.type;
                if (parameter == null || !IsValidParameterType(ownerKind, type) || !types.Add(type))
                {
                    error = "concert parameter list contains a null, invalid, or duplicate type";
                    return false;
                }
                if (!AddAssignment(ownerKind, ownerId, type, parameter, field, assignments, byKey, out error))
                {
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private static bool BuildParameterAssignments(
            int ownerKind,
            int ownerId,
            List<SEvent_Tour.tour._progressable> parameters,
            FieldInfo field,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            HashSet<int> types = new HashSet<int>();
            for (int index = 0; index < parameters.Count; index++)
            {
                SEvent_Tour.tour._progressable parameter = parameters[index];
                int type = parameter == null ? -1 : (int)parameter.type;
                if (parameter == null || !IsValidParameterType(ownerKind, type) || !types.Add(type))
                {
                    error = "tour parameter list contains a null, invalid, or duplicate type";
                    return false;
                }
                if (!AddAssignment(ownerKind, ownerId, type, parameter, field, assignments, byKey, out error))
                {
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private static bool BuildParameterAssignments(
            int ownerKind,
            int ownerId,
            List<SEvent_SSK._SSK._progressable> parameters,
            FieldInfo field,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            HashSet<int> types = new HashSet<int>();
            for (int index = 0; index < parameters.Count; index++)
            {
                SEvent_SSK._SSK._progressable parameter = parameters[index];
                int type = parameter == null ? -1 : (int)parameter.type;
                if (parameter == null || !IsValidParameterType(ownerKind, type) || !types.Add(type))
                {
                    error = "SSK parameter list contains a null, invalid, or duplicate type";
                    return false;
                }
                if (!AddAssignment(ownerKind, ownerId, type, parameter, field, assignments, byKey, out error))
                {
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private static bool AddAssignment(
            int ownerKind,
            int ownerId,
            int parameterType,
            object parameter,
            FieldInfo field,
            List<CounterAssignment> assignments,
            Dictionary<string, CounterAssignment> byKey,
            out string error)
        {
            string key = BuildKey(ownerKind, ownerId, parameterType);
            if (byKey.ContainsKey(key))
            {
                error = "duplicate project progress owner/parameter key";
                return false;
            }

            CounterAssignment assignment = new CounterAssignment
            {
                Key = key,
                Field = field,
                Target = parameter,
                DesiredValue = 0
            };
            assignments.Add(assignment);
            byKey.Add(key, assignment);
            error = string.Empty;
            return true;
        }

        private static bool ApplyAssignments(List<CounterAssignment> assignments, out string error)
        {
            error = string.Empty;
            for (int index = 0; index < assignments.Count; index++)
            {
                CounterAssignment assignment = assignments[index];
                int current;
                if (assignment == null ||
                    assignment.Field == null ||
                    assignment.Target == null ||
                    assignment.DesiredValue < 0 ||
                    !TryReadCounter(assignment.Target, assignment.Field, out current) ||
                    current < 0)
                {
                    error = "could not snapshot all current counters before atomic assignment";
                    return false;
                }
                assignment.OldValue = current;
            }

            int applied = 0;
            try
            {
                for (; applied < assignments.Count; applied++)
                {
                    CounterAssignment assignment = assignments[applied];
                    assignment.Field.SetValue(assignment.Target, assignment.DesiredValue);
                }
                return true;
            }
            catch (Exception exception)
            {
                for (int rollback = applied - 1; rollback >= 0; rollback--)
                {
                    try
                    {
                        CounterAssignment assignment = assignments[rollback];
                        assignment.Field.SetValue(assignment.Target, assignment.OldValue);
                    }
                    catch (Exception)
                    {
                        // Best-effort rollback. The caller reports the failed-closed state.
                    }
                }

                error = exception.Message;
                return false;
            }
        }

        private static bool TryReadCounter(object parameter, FieldInfo field, out int counter)
        {
            counter = 0;
            if (parameter == null || field == null)
            {
                return false;
            }

            try
            {
                object value = field.GetValue(parameter);
                if (!(value is int))
                {
                    return false;
                }
                counter = (int)value;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool IsValidOwnerKind(int ownerKind)
        {
            return ownerKind >= (int)OwnerKind.Single && ownerKind <= (int)OwnerKind.Ssk;
        }

        internal static bool IsValidParameterType(int ownerKind, int parameterType)
        {
            switch ((OwnerKind)ownerKind)
            {
                case OwnerKind.Single:
                    return parameterType >= (int)singles._single._param._type.lyrics &&
                        parameterType <= (int)singles._single._param._type.marketing;
                case OwnerKind.Show:
                    return parameterType >= (int)Shows._show._progressable._type.concept &&
                        parameterType <= (int)Shows._show._progressable._type.logistics;
                case OwnerKind.Concert:
                    return parameterType >= (int)SEvent_Concerts._concert._progressable._type.production &&
                        parameterType <= (int)SEvent_Concerts._concert._progressable._type.rehearsals;
                case OwnerKind.Tour:
                    return parameterType >= (int)SEvent_Tour.tour._progressable._type.production &&
                        parameterType <= (int)SEvent_Tour.tour._progressable._type.logistics;
                case OwnerKind.Ssk:
                    return parameterType >= (int)SEvent_SSK._SSK._progressable._type.production &&
                        parameterType <= (int)SEvent_SSK._SSK._progressable._type.logistics;
                default:
                    return false;
            }
        }

        internal static string BuildKey(int ownerKind, int ownerId, int parameterType)
        {
            return ownerKind.ToString(CultureInfo.InvariantCulture) + ":" +
                ownerId.ToString(CultureInfo.InvariantCulture) + ":" +
                parameterType.ToString(CultureInfo.InvariantCulture);
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = diagnostic ?? "A02 envelope capture failed.";
            error = lastDiagnostic;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "A02 repair section failed validation.";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }

        private sealed class CounterAssignment
        {
            internal string Key;
            internal FieldInfo Field;
            internal object Target;
            internal int DesiredValue;
            internal int OldValue;
        }
    }

    internal static class ProjectProgressCounterPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 2;

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

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A02 resolved more SaveManager.LoadData overloads than the frozen two-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A02 patch failure";
            }
        }
    }
}
