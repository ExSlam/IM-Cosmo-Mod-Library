using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        /// <summary>
        /// Captures one world-tour creation lifecycle event.
        /// </summary>
        internal void CaptureTourCreated(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return;
            }

            TourLifecyclePayload payload = BuildTourLifecyclePayload(
                tour,
                null,
                string.Empty,
                CoreConstants.TourLifecycleActionCreated);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTour,
                    tour.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeTourCreated,
                    CoreConstants.EventSourceTourSetPatch,
                    CoreJsonUtility.SerializeTourLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one world-tour start lifecycle event and participant rows.
        /// </summary>
        internal void CaptureTourStarted(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return;
            }

            List<int> participantIdolIdentifiers = ResolveDistinctActiveIdolIdentifiers();
            string tourStartDate = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime);
            TourLifecyclePayload lifecyclePayload = BuildTourLifecyclePayload(
                tour,
                participantIdolIdentifiers,
                tourStartDate,
                CoreConstants.TourLifecycleActionStarted);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string tourEntityIdentifier = tour.ID.ToString(CultureInfo.InvariantCulture);

                tourRuntimeStateByTourId[tour.ID] = new TourRuntimeCaptureState
                {
                    StartDate = tourStartDate,
                    ParticipantIdolIdentifiers = new List<int>(participantIdolIdentifiers)
                };

                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTour,
                    tourEntityIdentifier,
                    CoreConstants.EventTypeTourStarted,
                    CoreConstants.EventSourceTourStartPatch,
                    CoreJsonUtility.SerializeTourLifecyclePayload(lifecyclePayload));

                for (int participantIndex = CoreConstants.ZeroBasedListStartIndex; participantIndex < participantIdolIdentifiers.Count; participantIndex++)
                {
                    int participantIdolIdentifier = participantIdolIdentifiers[participantIndex];
                    TourParticipationPayload participationPayload = BuildTourParticipationPayload(
                        tour.ID,
                        participantIdolIdentifier,
                        participantIdolIdentifiers,
                        CoreConstants.TourLifecycleActionStarted);

                    EnqueueEventRecordLocked(
                        gameDate,
                        participantIdolIdentifier,
                        CoreConstants.EventEntityKindTour,
                        tourEntityIdentifier,
                        CoreConstants.EventTypeTourParticipation,
                        CoreConstants.EventSourceTourStartPatch,
                        CoreJsonUtility.SerializeTourParticipationPayload(participationPayload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates one finish snapshot before world-tour finalization mutates manager state.
        /// </summary>
        internal TourFinishSnapshot CreateTourFinishSnapshot(SEvent_Tour tourSystem)
        {
            TourFinishSnapshot snapshot = new TourFinishSnapshot();
            if (tourSystem == null || tourSystem.Tour == null)
            {
                return snapshot;
            }

            snapshot.Tour = tourSystem.Tour;
            return snapshot;
        }

        /// <summary>
        /// Captures one world-tour finish lifecycle event with country and idol participation rows.
        /// </summary>
        internal void CaptureTourFinished(TourFinishSnapshot finishSnapshot)
        {
            if (finishSnapshot == null || finishSnapshot.Tour == null)
            {
                return;
            }

            SEvent_Tour.tour finishedTour = finishSnapshot.Tour;
            List<int> participantIdolIdentifiers = null;
            string tourStartDate = string.Empty;
            TourRuntimeCaptureState runtimeTourState;

            lock (runtimeLock)
            {
                if (tourRuntimeStateByTourId.TryGetValue(finishedTour.ID, out runtimeTourState))
                {
                    participantIdolIdentifiers = runtimeTourState.ParticipantIdolIdentifiers != null
                        ? new List<int>(runtimeTourState.ParticipantIdolIdentifiers)
                        : new List<int>();
                    tourStartDate = runtimeTourState.StartDate ?? string.Empty;
                }
            }

            if (participantIdolIdentifiers == null)
            {
                participantIdolIdentifiers = ResolveDistinctActiveIdolIdentifiers();
            }

            TourLifecyclePayload lifecyclePayload = BuildTourLifecyclePayload(
                finishedTour,
                participantIdolIdentifiers,
                tourStartDate,
                CoreConstants.TourLifecycleActionFinished);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string tourEntityIdentifier = finishedTour.ID.ToString(CultureInfo.InvariantCulture);

                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTour,
                    tourEntityIdentifier,
                    CoreConstants.EventTypeTourFinished,
                    CoreConstants.EventSourceTourFinishPatch,
                    CoreJsonUtility.SerializeTourLifecyclePayload(lifecyclePayload));

                if (finishedTour.SelectedCountries != null)
                {
                    for (int countryIndex = CoreConstants.ZeroBasedListStartIndex; countryIndex < finishedTour.SelectedCountries.Count; countryIndex++)
                    {
                        SEvent_Tour.tour.selectedCountry selectedCountry = finishedTour.SelectedCountries[countryIndex];
                        if (selectedCountry == null || selectedCountry.Country == null)
                        {
                            continue;
                        }

                        string countryCode = CoreEnumNameMapping.ToTourCountryCode(selectedCountry.Country.Type);
                        TourCountryResultPayload countryResultPayload = BuildTourCountryResultPayload(finishedTour, selectedCountry);

                        EnqueueEventRecordLocked(
                            gameDate,
                            CoreConstants.InvalidIdValue,
                            CoreConstants.EventEntityKindTour,
                            BuildTourCountryEntityIdentifier(finishedTour.ID, countryCode),
                            CoreConstants.EventTypeTourCountryResult,
                            CoreConstants.EventSourceTourFinishPatch,
                            CoreJsonUtility.SerializeTourCountryResultPayload(countryResultPayload));
                    }
                }

                for (int participantIndex = CoreConstants.ZeroBasedListStartIndex; participantIndex < participantIdolIdentifiers.Count; participantIndex++)
                {
                    int participantIdolIdentifier = participantIdolIdentifiers[participantIndex];
                    TourParticipationPayload participationPayload = BuildTourParticipationPayload(
                        finishedTour.ID,
                        participantIdolIdentifier,
                        participantIdolIdentifiers,
                        CoreConstants.TourLifecycleActionFinished);

                    EnqueueEventRecordLocked(
                        gameDate,
                        participantIdolIdentifier,
                        CoreConstants.EventEntityKindTour,
                        tourEntityIdentifier,
                        CoreConstants.EventTypeTourParticipation,
                        CoreConstants.EventSourceTourFinishPatch,
                        CoreJsonUtility.SerializeTourParticipationPayload(participationPayload));
                }

                tourRuntimeStateByTourId.Remove(finishedTour.ID);

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one world-tour cancellation lifecycle event and optional participant rows.
        /// </summary>
        internal void CaptureTourCancelled(SEvent_Tour.tour cancelledTour)
        {
            if (cancelledTour == null)
            {
                return;
            }

            List<int> participantIdolIdentifiers = null;
            string tourStartDate = string.Empty;
            TourRuntimeCaptureState runtimeTourState;

            lock (runtimeLock)
            {
                if (tourRuntimeStateByTourId.TryGetValue(cancelledTour.ID, out runtimeTourState))
                {
                    participantIdolIdentifiers = runtimeTourState.ParticipantIdolIdentifiers != null
                        ? new List<int>(runtimeTourState.ParticipantIdolIdentifiers)
                        : new List<int>();
                    tourStartDate = runtimeTourState.StartDate ?? string.Empty;
                }
            }

            if (participantIdolIdentifiers == null)
            {
                participantIdolIdentifiers = new List<int>();
            }

            TourLifecyclePayload lifecyclePayload = BuildTourLifecyclePayload(
                cancelledTour,
                participantIdolIdentifiers,
                tourStartDate,
                CoreConstants.TourLifecycleActionCancelled);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string tourEntityIdentifier = cancelledTour.ID.ToString(CultureInfo.InvariantCulture);

                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTour,
                    tourEntityIdentifier,
                    CoreConstants.EventTypeTourCancelled,
                    CoreConstants.EventSourceTourCancelPatch,
                    CoreJsonUtility.SerializeTourLifecyclePayload(lifecyclePayload));

                for (int participantIndex = CoreConstants.ZeroBasedListStartIndex; participantIndex < participantIdolIdentifiers.Count; participantIndex++)
                {
                    int participantIdolIdentifier = participantIdolIdentifiers[participantIndex];
                    TourParticipationPayload participationPayload = BuildTourParticipationPayload(
                        cancelledTour.ID,
                        participantIdolIdentifier,
                        participantIdolIdentifiers,
                        CoreConstants.TourLifecycleActionCancelled);

                    EnqueueEventRecordLocked(
                        gameDate,
                        participantIdolIdentifier,
                        CoreConstants.EventEntityKindTour,
                        tourEntityIdentifier,
                        CoreConstants.EventTypeTourParticipation,
                        CoreConstants.EventSourceTourCancelPatch,
                        CoreJsonUtility.SerializeTourParticipationPayload(participationPayload));
                }

                tourRuntimeStateByTourId.Remove(cancelledTour.ID);

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one tour status transition event.
        /// </summary>
        internal void CaptureTourStatusTransition(SEvent_Tour.tour tour, SEvent_Tour.tour._status previousStatus, SEvent_Tour.tour._status newStatus)
        {
            if (tour == null)
            {
                return;
            }

            string previousStatusCode = CoreEnumNameMapping.ToTourStatusCode(previousStatus);
            string newStatusCode = CoreEnumNameMapping.ToTourStatusCode(newStatus);
            if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
            {
                return;
            }

            long totalAudience = CoreConstants.ZeroLongValue;
            long totalRevenue = CoreConstants.ZeroLongValue;
            long totalNewFans = CoreConstants.ZeroLongValue;
            if (tour.SelectedCountries != null)
            {
                for (int countryIndex = CoreConstants.ZeroBasedListStartIndex; countryIndex < tour.SelectedCountries.Count; countryIndex++)
                {
                    SEvent_Tour.tour.selectedCountry country = tour.SelectedCountries[countryIndex];
                    if (country == null)
                    {
                        continue;
                    }

                    totalAudience += country.Audience;
                    totalRevenue += country.Revenue;
                    totalNewFans += country.NewFans;
                }
            }

            TourStatusPayload payload = new TourStatusPayload
            {
                TourId = tour.ID,
                PreviousTourStatus = previousStatusCode,
                NewTourStatus = newStatusCode,
                SelectedCountryCount = tour.SelectedCountries != null ? tour.SelectedCountries.Count : CoreConstants.ZeroBasedListStartIndex,
                TourTotalAudience = totalAudience,
                TourTotalRevenue = totalRevenue,
                TourTotalNewFans = totalNewFans,
                TourProductionCost = tour.ProductionCost,
                TourExpectedRevenue = tour.ExpectedRevenue,
                TourSaving = tour.Saving,
                TourFinishDate = CoreDateTimeUtility.ToRoundTripString(tour.FinishDate)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTour,
                    tour.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeTourStatusChanged,
                    CoreConstants.EventSourceTourStatusPatch,
                    CoreJsonUtility.SerializeTourStatusPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one election-creation lifecycle event.
        /// </summary>
        internal void CaptureElectionCreated(SEvent_SSK._SSK election)
        {
            if (election == null)
            {
                return;
            }

            ElectionLifecyclePayload payload = BuildElectionLifecyclePayload(election, CoreConstants.ElectionLifecycleActionCreated);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindElection,
                    election.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeElectionCreated,
                    CoreConstants.EventSourceElectionSetPatch,
                    CoreJsonUtility.SerializeElectionLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one election-cancel lifecycle event.
        /// </summary>
        internal void CaptureElectionCancelled(SEvent_SSK._SSK election)
        {
            if (election == null)
            {
                return;
            }

            ElectionLifecyclePayload payload = BuildElectionLifecyclePayload(election, CoreConstants.ElectionLifecycleActionCancelled);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindElection,
                    election.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeElectionCancelled,
                    CoreConstants.EventSourceElectionCancelPatch,
                    CoreJsonUtility.SerializeElectionLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures generated election rankings for all currently eligible idols.
        /// </summary>
        internal void CaptureElectionResultsGenerated(SEvent_SSK._SSK election)
        {
            if (election == null || data_girls.girl == null)
            {
                return;
            }

            Dictionary<int, SEvent_SSK._SSK._result> resultRowsByIdolId = new Dictionary<int, SEvent_SSK._SSK._result>();
            if (election.Results != null)
            {
                for (int resultIndex = CoreConstants.ZeroBasedListStartIndex; resultIndex < election.Results.Count; resultIndex++)
                {
                    SEvent_SSK._SSK._result resultRow = election.Results[resultIndex];
                    if (resultRow == null || resultRow.Girl == null || resultRow.Girl.id < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        continue;
                    }

                    resultRowsByIdolId[resultRow.Girl.id] = resultRow;
                }
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string electionEntityIdentifier = election.ID.ToString(CultureInfo.InvariantCulture);
                string electionBroadcastTypeCode = CoreEnumNameMapping.ToElectionBroadcastCode(election.Broadcast);

                for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < data_girls.girl.Count; idolIndex++)
                {
                    data_girls.girls idol = data_girls.girl[idolIndex];
                    if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier || !idol.CanParticipateInSSK())
                    {
                        continue;
                    }

                    int generatedPlace = ResolveElectionPlaceForIdol(election, idol.id);
                    long generatedVotes = CoreConstants.ZeroLongValue;
                    int generatedFamePoints = CoreConstants.ZeroBasedListStartIndex;
                    SEvent_SSK._SSK._result resultRow;
                    if (resultRowsByIdolId.TryGetValue(idol.id, out resultRow))
                    {
                        generatedVotes = resultRow.Votes;
                        generatedFamePoints = resultRow.FamePoints;
                    }

                    ElectionGeneratedResultPayload payload = new ElectionGeneratedResultPayload
                    {
                        ElectionId = election.ID,
                        IdolId = idol.id,
                        ElectionExpectedPlace = idol.SSK_Expected_Place,
                        ElectionGeneratedPlace = generatedPlace,
                        ElectionGeneratedVotes = generatedVotes,
                        ElectionGeneratedFamePoints = generatedFamePoints,
                        ElectionBroadcastType = electionBroadcastTypeCode
                    };

                    EnqueueEventRecordLocked(
                        gameDate,
                        idol.id,
                        CoreConstants.EventEntityKindElection,
                        electionEntityIdentifier,
                        CoreConstants.EventTypeElectionResultsGenerated,
                        CoreConstants.EventSourceElectionGenerateResultsPatch,
                        CoreJsonUtility.SerializeElectionGeneratedResultPayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures snapshot data before one election place-adjustment mutation.
        /// </summary>
        internal ElectionPlaceAdjustmentSnapshot CreateElectionPlaceAdjustmentSnapshot(SEvent_SSK._SSK election, int requestedPlace)
        {
            ElectionPlaceAdjustmentSnapshot snapshot = new ElectionPlaceAdjustmentSnapshot
            {
                RequestedPlace = requestedPlace
            };

            if (election == null || election.Results_Next_Girl == null || election.Results_Next_Girl.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return snapshot;
            }

            snapshot.TargetIdolId = election.Results_Next_Girl.id;
            snapshot.PreviousPlace = ResolveElectionPlaceForIdol(election, snapshot.TargetIdolId);
            snapshot.HasCandidate = true;
            return snapshot;
        }

        /// <summary>
        /// Captures one election place-adjustment event after manual ranking edits.
        /// </summary>
        internal void CaptureElectionPlaceAdjusted(SEvent_SSK._SSK election, ElectionPlaceAdjustmentSnapshot snapshot)
        {
            if (election == null || snapshot == null || !snapshot.HasCandidate || snapshot.TargetIdolId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            int adjustedPlace = ResolveElectionPlaceForIdol(election, snapshot.TargetIdolId);
            if (adjustedPlace == snapshot.PreviousPlace)
            {
                return;
            }

            ElectionPlaceAdjustedPayload payload = new ElectionPlaceAdjustedPayload
            {
                ElectionId = election.ID,
                IdolId = snapshot.TargetIdolId,
                ElectionPlaceBefore = snapshot.PreviousPlace,
                ElectionPlaceAfter = adjustedPlace,
                ElectionBroadcastType = CoreEnumNameMapping.ToElectionBroadcastCode(election.Broadcast)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    snapshot.TargetIdolId,
                    CoreConstants.EventEntityKindElection,
                    election.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeElectionPlaceAdjusted,
                    CoreConstants.EventSourceElectionSetPlacePatch,
                    CoreJsonUtility.SerializeElectionPlaceAdjustedPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one election status transition event.
        /// </summary>
        internal void CaptureElectionStatusTransition(SEvent_SSK._SSK election, SEvent_Tour.tour._status previousStatus, SEvent_Tour.tour._status newStatus)
        {
            if (election == null)
            {
                return;
            }

            string previousStatusCode = CoreEnumNameMapping.ToTourStatusCode(previousStatus);
            string newStatusCode = CoreEnumNameMapping.ToTourStatusCode(newStatus);
            if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
            {
                return;
            }

            ElectionStatusPayload payload = new ElectionStatusPayload
            {
                ElectionId = election.ID,
                PreviousElectionStatus = previousStatusCode,
                NewElectionStatus = newStatusCode,
                ElectionBroadcastType = CoreEnumNameMapping.ToElectionBroadcastCode(election.Broadcast),
                ElectionSingleId = ResolveSingleIdOrInvalid(election.Single),
                ElectionConcertId = ResolveConcertIdOrInvalid(election.Concert),
                ElectionReleaseSingleId = ResolveSingleIdOrInvalid(election.ReleaseSingle),
                ElectionResultCount = election.Results != null ? election.Results.Count : CoreConstants.ZeroBasedListStartIndex,
                ElectionFinishDate = CoreDateTimeUtility.ToRoundTripString(election.FinishDate)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindElection,
                    election.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeElectionStatusChanged,
                    CoreConstants.EventSourceElectionStatusPatch,
                    CoreJsonUtility.SerializeElectionStatusPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one election-result event for each idol in result rankings.
        /// </summary>
        internal void CaptureElectionResults(SEvent_SSK._SSK election)
        {
            if (election == null || election.Results == null || election.Results.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string electionFinishDate = CoreDateTimeUtility.ToRoundTripString(election.FinishDate);
                string electionBroadcastCode = CoreEnumNameMapping.ToElectionBroadcastCode(election.Broadcast);
                string electionEntityIdentifier = election.ID.ToString(CultureInfo.InvariantCulture);
                ElectionLifecyclePayload lifecyclePayload = BuildElectionLifecyclePayload(election, CoreConstants.ElectionLifecycleActionFinished);

                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindElection,
                    electionEntityIdentifier,
                    CoreConstants.EventTypeElectionFinished,
                    CoreConstants.EventSourceElectionResultPatch,
                    CoreJsonUtility.SerializeElectionLifecyclePayload(lifecyclePayload));

                for (int resultIndex = CoreConstants.ZeroBasedListStartIndex; resultIndex < election.Results.Count; resultIndex++)
                {
                    SEvent_SSK._SSK._result result = election.Results[resultIndex];
                    if (result == null || result.Girl == null || result.Girl.id < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        continue;
                    }

                    ElectionResultPayload payload = new ElectionResultPayload
                    {
                        ElectionId = election.ID,
                        IdolId = result.Girl.id,
                        ElectionPlace = result.Place,
                        ElectionVotes = result.Votes,
                        ElectionFamePoints = result.FamePoints,
                        ElectionBroadcastType = electionBroadcastCode,
                        ElectionFinishDate = electionFinishDate
                    };

                    EnqueueEventRecordLocked(
                        gameDate,
                        result.Girl.id,
                        CoreConstants.EventEntityKindElection,
                        electionEntityIdentifier,
                        CoreConstants.EventTypeElectionResultRecorded,
                        CoreConstants.EventSourceElectionResultPatch,
                        CoreJsonUtility.SerializeElectionResultPayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures snapshot data before one loan mutation executes.
        /// </summary>
        internal LoanMutationSnapshot CreateLoanMutationSnapshot(loans._loan loan)
        {
            LoanMutationSnapshot snapshot = new LoanMutationSnapshot
            {
                Money = resources.Money(),
                TotalDebt = loans.GetTotalDebt(),
                TotalPaymentPerWeek = loans.GetTotalPaymentPerWeek(),
                ActiveLoanCount = CountActiveLoans(),
                TotalLoanCount = loans.Loans != null ? loans.Loans.Count : CoreConstants.ZeroBasedListStartIndex
            };

            if (loan == null)
            {
                return snapshot;
            }

            snapshot.LoanId = loan.ID;
            snapshot.LoanActive = loan.Active;
            snapshot.LoanDebt = loan.GetDebt();
            snapshot.LoanCanPayOff = loan.CanPayOff();
            snapshot.LoanInDevelopment = loan.IsInDevelopment();
            snapshot.LoanDaysToDevelop = loan.GetDaysToDevelop();
            snapshot.LoanStartDate = loan.StartDate == DateTime.MinValue ? string.Empty : CoreDateTimeUtility.ToRoundTripString(loan.StartDate);
            snapshot.LoanEndDate = loan.EndDate == default(DateTime) ? string.Empty : CoreDateTimeUtility.ToRoundTripString(loan.EndDate);
            return snapshot;
        }

        /// <summary>
        /// Captures one loan-added event after `loans.AddLoan` mutates game state.
        /// </summary>
        internal void CaptureLoanAdded(loans._loan loan, LoanMutationSnapshot snapshotBefore)
        {
            CaptureLoanLifecycleEvent(
                loan,
                snapshotBefore,
                CoreConstants.EventTypeLoanAdded,
                CoreConstants.LoanLifecycleActionAdded,
                CoreConstants.EventSourceLoansAddLoanPatch);
        }

        /// <summary>
        /// Captures one loan-initialized event after `_loan.Initialize` grants funds and activates terms.
        /// </summary>
        internal void CaptureLoanInitialized(loans._loan loan, LoanMutationSnapshot snapshotBefore)
        {
            CaptureLoanLifecycleEvent(
                loan,
                snapshotBefore,
                CoreConstants.EventTypeLoanInitialized,
                CoreConstants.LoanLifecycleActionInitialized,
                CoreConstants.EventSourceLoansInitializePatch);
        }

        /// <summary>
        /// Captures one loan payoff event after `_loan.PayOff` clears active debt.
        /// </summary>
        internal void CaptureLoanPaidOff(loans._loan loan, LoanMutationSnapshot snapshotBefore)
        {
            if (loan == null)
            {
                return;
            }

            if (snapshotBefore != null && !snapshotBefore.LoanActive && !loan.Active)
            {
                return;
            }

            CaptureLoanLifecycleEvent(
                loan,
                snapshotBefore,
                CoreConstants.EventTypeLoanPaidOff,
                CoreConstants.LoanLifecycleActionPaidOff,
                CoreConstants.EventSourceLoansPayOffPatch);
        }

        /// <summary>
        /// Captures snapshot data before `SetBankruptcyDanger` mutates static flags.
        /// </summary>
        internal BankruptcyDangerSnapshot CreateBankruptcyDangerSnapshot()
        {
            return new BankruptcyDangerSnapshot
            {
                DangerBefore = loans.BankruptcyDanger,
                BankruptcyDateBefore = loans.BankruptcyDate,
                MoneyBefore = resources.Money(),
                TotalDebtBefore = loans.GetTotalDebt()
            };
        }

        /// <summary>
        /// Captures one bankruptcy-danger mutation event after `SetBankruptcyDanger`.
        /// </summary>
        internal void CaptureBankruptcyDangerSet(bool requestedValue, BankruptcyDangerSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            bool dangerAfter = loans.BankruptcyDanger;
            DateTime bankruptcyDateAfter = loans.BankruptcyDate;
            if (snapshotBefore.DangerBefore == dangerAfter && snapshotBefore.BankruptcyDateBefore == bankruptcyDateAfter)
            {
                return;
            }

            BankruptcyDangerEventPayload payload = new BankruptcyDangerEventPayload
            {
                requested_value = requestedValue,
                bankruptcy_danger_before = snapshotBefore.DangerBefore,
                bankruptcy_danger_after = dangerAfter,
                bankruptcy_date_before = snapshotBefore.BankruptcyDateBefore == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(snapshotBefore.BankruptcyDateBefore),
                bankruptcy_date_after = bankruptcyDateAfter == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(bankruptcyDateAfter),
                bankruptcy_days_remaining_after = loans.DaysTillBankruptcy(),
                money_after = resources.Money(),
                total_debt_after = loans.GetTotalDebt()
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindBankruptcy,
                    CoreConstants.EventEntityKindBankruptcy,
                    CoreConstants.EventTypeBankruptcyDangerSet,
                    CoreConstants.EventSourceLoansSetBankruptcyDangerPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures snapshot data before one bankruptcy run-fail check.
        /// </summary>
        internal BankruptcyCheckSnapshot CreateBankruptcyCheckSnapshot()
        {
            return new BankruptcyCheckSnapshot
            {
                DangerBefore = loans.BankruptcyDanger,
                DaysRemainingBefore = loans.DaysTillBankruptcy(),
                MoneyBefore = resources.Money(),
                TotalDebtBefore = loans.GetTotalDebt(),
                BailoutUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagBankruptcyBailOut),
                StoryRecruitUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagStoryRecruit),
                GameOverUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagGameOverBankruptcy)
            };
        }

        /// <summary>
        /// Captures one bankruptcy-check event when check logic starts a dialogue/fail path.
        /// </summary>
        internal void CaptureBankruptcyCheck(BankruptcyCheckSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            bool bailoutUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagBankruptcyBailOut);
            bool storyRecruitUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagStoryRecruit);
            bool gameOverUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagGameOverBankruptcy);

            string triggeredDialogue = string.Empty;
            if (!snapshotBefore.BailoutUsedBefore && bailoutUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagBankruptcyBailOut;
            }
            else if (!snapshotBefore.StoryRecruitUsedBefore && storyRecruitUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagStoryRecruit;
            }
            else if (!snapshotBefore.GameOverUsedBefore && gameOverUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagGameOverBankruptcy;
            }

            if (string.IsNullOrEmpty(triggeredDialogue))
            {
                return;
            }

            BankruptcyCheckEventPayload payload = new BankruptcyCheckEventPayload
            {
                bankruptcy_danger_before = snapshotBefore.DangerBefore,
                bankruptcy_danger_after = loans.BankruptcyDanger,
                bankruptcy_days_remaining_before = snapshotBefore.DaysRemainingBefore,
                bankruptcy_days_remaining_after = loans.DaysTillBankruptcy(),
                money_before = snapshotBefore.MoneyBefore,
                money_after = resources.Money(),
                total_debt_before = snapshotBefore.TotalDebtBefore,
                total_debt_after = loans.GetTotalDebt(),
                bailout_used_before = snapshotBefore.BailoutUsedBefore,
                bailout_used_after = bailoutUsedAfter,
                story_recruit_used_before = snapshotBefore.StoryRecruitUsedBefore,
                story_recruit_used_after = storyRecruitUsedAfter,
                game_over_bankruptcy_used_before = snapshotBefore.GameOverUsedBefore,
                game_over_bankruptcy_used_after = gameOverUsedAfter,
                triggered_dialogue = triggeredDialogue
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindBankruptcy,
                    CoreConstants.EventEntityKindBankruptcy,
                    CoreConstants.EventTypeBankruptcyCheck,
                    CoreConstants.EventSourceBankruptcyCheckBankruptcyPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures snapshot data before one scandal check evaluation.
        /// </summary>
        internal ScandalCheckSnapshot CreateScandalCheckSnapshot(Bankruptcy bankruptcySystem, bool testGameOver)
        {
            return new ScandalCheckSnapshot
            {
                TestGameOver = testGameOver,
                ScandalPointsBefore = resources.GetScandalPointsTotal(),
                ScandalThreshold = ResolveScandalThreshold(bankruptcySystem),
                FirstScandalUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagFirstScandalPoints),
                WarningUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagGameOverWarningScandalPoints),
                ParentsUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagScandalPointsParents),
                GameOverUsedBefore = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagGameOverScandalPoints),
                ScandalParentCooldownBefore = tasks.Story_Data != null && tasks.Story_Data.IsOnScandalParentCooldown(),
                AuditionFailureBefore = tasks.Story_Data != null && tasks.Story_Data.Scandal_Auditions_No_More,
                ActiveIdolCountBefore = CountNonGraduatedIdols()
            };
        }

        /// <summary>
        /// Captures one scandal-check event when check logic starts a dialogue/fail path.
        /// </summary>
        internal void CaptureScandalCheck(ScandalCheckSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            bool firstScandalUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagFirstScandalPoints);
            bool warningUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagGameOverWarningScandalPoints);
            bool parentsUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagScandalPointsParents);
            bool gameOverUsedAfter = Substories_Manager.IsUsed(CoreConstants.SubstoryFlagGameOverScandalPoints);
            bool scandalParentCooldownAfter = tasks.Story_Data != null && tasks.Story_Data.IsOnScandalParentCooldown();
            bool auditionFailureAfter = tasks.Story_Data != null && tasks.Story_Data.Scandal_Auditions_No_More;

            string triggeredDialogue = string.Empty;
            if (!snapshotBefore.FirstScandalUsedBefore && firstScandalUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagFirstScandalPoints;
            }
            else if (!snapshotBefore.WarningUsedBefore && warningUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagGameOverWarningScandalPoints;
            }
            else if (!snapshotBefore.ParentsUsedBefore && parentsUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagScandalPointsParents;
            }
            else if (!snapshotBefore.GameOverUsedBefore && gameOverUsedAfter)
            {
                triggeredDialogue = CoreConstants.SubstoryFlagGameOverScandalPoints;
            }

            if (string.IsNullOrEmpty(triggeredDialogue) && snapshotBefore.AuditionFailureBefore == auditionFailureAfter)
            {
                return;
            }

            ScandalCheckEventPayload payload = new ScandalCheckEventPayload
            {
                test_go = snapshotBefore.TestGameOver,
                scandal_points_total_before = snapshotBefore.ScandalPointsBefore,
                scandal_points_total_after = resources.GetScandalPointsTotal(),
                scandal_threshold = snapshotBefore.ScandalThreshold,
                first_scandal_used_before = snapshotBefore.FirstScandalUsedBefore,
                first_scandal_used_after = firstScandalUsedAfter,
                warning_used_before = snapshotBefore.WarningUsedBefore,
                warning_used_after = warningUsedAfter,
                parents_used_before = snapshotBefore.ParentsUsedBefore,
                parents_used_after = parentsUsedAfter,
                game_over_used_before = snapshotBefore.GameOverUsedBefore,
                game_over_used_after = gameOverUsedAfter,
                scandal_parent_cooldown_before = snapshotBefore.ScandalParentCooldownBefore,
                scandal_parent_cooldown_after = scandalParentCooldownAfter,
                audition_failure_before = snapshotBefore.AuditionFailureBefore,
                audition_failure_after = auditionFailureAfter,
                active_idol_count_before = snapshotBefore.ActiveIdolCountBefore,
                active_idol_count_after = CountNonGraduatedIdols(),
                triggered_dialogue = triggeredDialogue
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindScandal,
                    CoreConstants.EventEntityKindScandal,
                    CoreConstants.EventTypeScandalCheck,
                    CoreConstants.EventSourceBankruptcyCheckScandalPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures snapshot data before audition-failure logic graduates all active idols.
        /// </summary>
        internal AuditionFailureSnapshot CreateAuditionFailureSnapshot()
        {
            return new AuditionFailureSnapshot
            {
                AuditionFailureBefore = tasks.Story_Data != null && tasks.Story_Data.Scandal_Auditions_No_More,
                ActiveIdolCountBefore = CountNonGraduatedIdols()
            };
        }

        /// <summary>
        /// Captures one audition-failure event after idol wipeout logic executes.
        /// </summary>
        internal void CaptureAuditionFailureTriggered(AuditionFailureSnapshot snapshotBefore)
        {
            AuditionFailureEventPayload payload = new AuditionFailureEventPayload
            {
                audition_failure_before = snapshotBefore != null && snapshotBefore.AuditionFailureBefore,
                audition_failure_after = tasks.Story_Data != null && tasks.Story_Data.Scandal_Auditions_No_More,
                active_idol_count_before = snapshotBefore != null ? snapshotBefore.ActiveIdolCountBefore : CoreConstants.ZeroBasedListStartIndex,
                active_idol_count_after = CountNonGraduatedIdols()
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindScandal,
                    CoreConstants.EventEntityKindScandal,
                    CoreConstants.EventTypeAuditionFailureTriggered,
                    CoreConstants.EventSourceBankruptcyTriggerAuditionFailurePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one loan lifecycle event with before/after economic context.
        /// </summary>
        private void CaptureLoanLifecycleEvent(
            loans._loan loan,
            LoanMutationSnapshot snapshotBefore,
            string eventType,
            string lifecycleAction,
            string sourcePatch)
        {
            if (loan == null)
            {
                return;
            }

            long moneyAfter = resources.Money();
            long totalDebtAfter = loans.GetTotalDebt();
            int totalPaymentAfter = loans.GetTotalPaymentPerWeek();
            int activeLoanCountAfter = CountActiveLoans();
            int totalLoanCountAfter = loans.Loans != null ? loans.Loans.Count : CoreConstants.ZeroBasedListStartIndex;

            LoanLifecycleEventPayload payload = new LoanLifecycleEventPayload
            {
                loan_id = loan.ID,
                loan_lifecycle_action = lifecycleAction ?? string.Empty,
                loan_type = CoreEnumNameMapping.ToLoanTypeCode(loan.Type),
                loan_duration = CoreEnumNameMapping.ToLoanDurationCode(loan.Duration),
                loan_active_before = snapshotBefore != null && snapshotBefore.LoanActive,
                loan_active_after = loan.Active,
                loan_amount = loan.Amount,
                loan_payment_per_week = loan.PaymentPerWeek,
                loan_interest_rate = loan.InterestRate,
                loan_start_date = loan.StartDate == DateTime.MinValue ? string.Empty : CoreDateTimeUtility.ToRoundTripString(loan.StartDate),
                loan_end_date = loan.EndDate == default(DateTime) ? string.Empty : CoreDateTimeUtility.ToRoundTripString(loan.EndDate),
                loan_debt_before = snapshotBefore != null ? snapshotBefore.LoanDebt : loan.GetDebt(),
                loan_debt_after = loan.GetDebt(),
                loan_can_pay_off_after = loan.CanPayOff(),
                loan_in_development_after = loan.IsInDevelopment(),
                loan_days_to_develop = loan.GetDaysToDevelop(),
                loan_count_active = activeLoanCountAfter,
                loan_count_total = totalLoanCountAfter,
                loan_total_payment_per_week = totalPaymentAfter,
                loan_total_debt = totalDebtAfter,
                money_before = snapshotBefore != null ? snapshotBefore.Money : moneyAfter,
                money_after = moneyAfter,
                money_delta = moneyAfter - (snapshotBefore != null ? snapshotBefore.Money : moneyAfter)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindLoan,
                    loan.ID.ToString(CultureInfo.InvariantCulture),
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Resolves active (not fully settled) loan count.
        /// </summary>
        private static int CountActiveLoans()
        {
            if (loans.Loans == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int activeLoanCount = CoreConstants.ZeroBasedListStartIndex;
            for (int loanIndex = CoreConstants.ZeroBasedListStartIndex; loanIndex < loans.Loans.Count; loanIndex++)
            {
                loans._loan loan = loans.Loans[loanIndex];
                if (loan != null && loan.Active)
                {
                    activeLoanCount++;
                }
            }

            return activeLoanCount;
        }

        /// <summary>
        /// Resolves current count of non-graduated idols.
        /// </summary>
        private static int CountNonGraduatedIdols()
        {
            if (data_girls.girl == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int activeIdolCount = CoreConstants.ZeroBasedListStartIndex;
            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < data_girls.girl.Count; idolIndex++)
            {
                data_girls.girls idol = data_girls.girl[idolIndex];
                if (idol != null && idol.status != data_girls._status.graduated)
                {
                    activeIdolCount++;
                }
            }

            return activeIdolCount;
        }

        /// <summary>
        /// Resolves the private scandal threshold from bankruptcy manager with safe fallback.
        /// </summary>
        private static int ResolveScandalThreshold(Bankruptcy bankruptcySystem)
        {
            if (bankruptcySystem == null)
            {
                return CoreConstants.DefaultScandalGameOverThreshold;
            }

            try
            {
                object thresholdValue = Traverse.Create(bankruptcySystem).Field(CoreConstants.ReflectionBankruptcyScandalGameOverThresholdFieldName).GetValue();
                if (thresholdValue == null)
                {
                    return CoreConstants.DefaultScandalGameOverThreshold;
                }

                return Convert.ToInt32(thresholdValue, CultureInfo.InvariantCulture);
            }
            catch
            {
                return CoreConstants.DefaultScandalGameOverThreshold;
            }
        }

        /// <summary>
        /// Captures snapshot data before one policy selection mutation.
        /// </summary>
        internal PolicySelectionSnapshot CreatePolicySelectionSnapshot(policies.value policyValue)
        {
            PolicySelectionSnapshot snapshot = new PolicySelectionSnapshot();
            if (policyValue == null)
            {
                return snapshot;
            }

            snapshot.PreviousSelectedValueCode = ResolveSelectedPolicyValueCode(policyValue.Type);
            return snapshot;
        }

        /// <summary>
        /// Captures one policy decision event after `_Select(bool)` commits selection.
        /// </summary>
        internal void CapturePolicyDecision(policies.value policyValue, bool freeSelection, PolicySelectionSnapshot snapshotBefore)
        {
            if (policyValue == null)
            {
                return;
            }

            string previousValue = snapshotBefore != null
                ? snapshotBefore.PreviousSelectedValueCode
                : ResolveSelectedPolicyValueCode(policyValue.Type);
            string newValue = CoreEnumNameMapping.ToPolicyValueCode(policyValue.Value);
            int policyCost = policyValue.Cost;

            if (string.Equals(previousValue, newValue, StringComparison.Ordinal) && policyCost <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            PolicyDecisionEventPayload payload = new PolicyDecisionEventPayload
            {
                policy_type = CoreEnumNameMapping.ToPolicyTypeCode(policyValue.Type),
                previous_value = previousValue ?? string.Empty,
                new_value = newValue ?? string.Empty,
                policy_cost = policyCost,
                free_selection = freeSelection,
                decision_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string entityIdentifier =
                    (payload.policy_type ?? CoreConstants.StatusCodeUnknown)
                    + CoreConstants.SaveKeyJoinSeparator
                    + (payload.new_value ?? CoreConstants.StatusCodeUnknown);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindPolicy,
                    entityIdentifier,
                    CoreConstants.EventTypePolicyDecisionSelected,
                    CoreConstants.EventSourcePolicySelectPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Resolves currently selected policy value for one policy type.
        /// </summary>
        private static string ResolveSelectedPolicyValueCode(policies._type policyType)
        {
            if (policies.Values == null)
            {
                return CoreConstants.StatusCodeUnknown;
            }

            for (int valueIndex = CoreConstants.ZeroBasedListStartIndex; valueIndex < policies.Values.Count; valueIndex++)
            {
                policies.value value = policies.Values[valueIndex];
                if (value == null || value.Type != policyType || !value.Selected)
                {
                    continue;
                }

                return CoreEnumNameMapping.ToPolicyValueCode(value.Value);
            }

            return CoreConstants.StatusCodeUnknown;
        }

        /// <summary>
        /// Captures pre-activity economy and roster snapshot before one activity action.
        /// </summary>
        internal ActivityActionSnapshot CreateActivityActionSnapshot()
        {
            return new ActivityActionSnapshot
            {
                MoneyBefore = resources.Money(),
                FansBefore = resources.GetFansTotal(null),
                ActiveIdolCountBefore = data_girls.GetActiveGirlsCounter(null)
            };
        }

        /// <summary>
        /// Captures one performance activity result event.
        /// </summary>
        internal void CaptureActivityPerformance(Activities activitiesSystem, ActivityActionSnapshot snapshotBefore)
        {
            ActivityActionSnapshot fallbackSnapshot = snapshotBefore ?? CreateActivityActionSnapshot();
            int activeIdolCountAfter = data_girls.GetActiveGirlsCounter(null);
            long moneyAfter = resources.Money();
            long moneyDelta = moneyAfter - fallbackSnapshot.MoneyBefore;

            int activityLevel = CoreConstants.ZeroBasedListStartIndex;
            if (activitiesSystem != null && activitiesSystem.GetActivity(Activity._type.performance) != null)
            {
                activityLevel = activitiesSystem.GetActivity(Activity._type.performance).lvl;
            }

            ActivityActionEventPayload payload = new ActivityActionEventPayload
            {
                activity_type = CoreConstants.EarningsSourceActivitiesPerformance,
                activity_level = activityLevel,
                activity_bonus = Activities.GetBonus(true),
                money_before = fallbackSnapshot.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyDelta,
                fans_before = fallbackSnapshot.FansBefore,
                fans_after = resources.GetFansTotal(null),
                fans_delta = resources.GetFansTotal(null) - fallbackSnapshot.FansBefore,
                active_idol_count_before = fallbackSnapshot.ActiveIdolCountBefore,
                active_idol_count_after = activeIdolCountAfter,
                per_idol_earnings = activeIdolCountAfter > CoreConstants.ZeroBasedListStartIndex
                    ? Mathf.RoundToInt((float)moneyDelta / activeIdolCountAfter)
                    : CoreConstants.ZeroBasedListStartIndex,
                stamina_cost = activitiesSystem != null ? activitiesSystem.GetStaminaCost() : CoreConstants.ZeroBasedListStartIndex,
                activity_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            EmitActivityActionEvent(
                payload,
                CoreConstants.EventTypeActivityPerformance,
                CoreConstants.EventSourceActivitiesPerformancePatch,
                CoreConstants.EarningsSourceActivitiesPerformance);
        }

        /// <summary>
        /// Captures one promotion activity result event.
        /// </summary>
        internal void CaptureActivityPromotion(Activities activitiesSystem, ActivityActionSnapshot snapshotBefore)
        {
            ActivityActionSnapshot fallbackSnapshot = snapshotBefore ?? CreateActivityActionSnapshot();
            long fansAfter = resources.GetFansTotal(null);

            int activityLevel = CoreConstants.ZeroBasedListStartIndex;
            if (activitiesSystem != null && activitiesSystem.GetActivity(Activity._type.promotion) != null)
            {
                activityLevel = activitiesSystem.GetActivity(Activity._type.promotion).lvl;
            }

            ActivityActionEventPayload payload = new ActivityActionEventPayload
            {
                activity_type = CoreConstants.EarningsSourceActivitiesPromotion,
                activity_level = activityLevel,
                activity_bonus = Activities.GetBonus(false),
                money_before = fallbackSnapshot.MoneyBefore,
                money_after = resources.Money(),
                money_delta = resources.Money() - fallbackSnapshot.MoneyBefore,
                fans_before = fallbackSnapshot.FansBefore,
                fans_after = fansAfter,
                fans_delta = fansAfter - fallbackSnapshot.FansBefore,
                active_idol_count_before = fallbackSnapshot.ActiveIdolCountBefore,
                active_idol_count_after = data_girls.GetActiveGirlsCounter(null),
                stamina_cost = -3f,
                activity_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            EmitActivityActionEvent(
                payload,
                CoreConstants.EventTypeActivityPromotion,
                CoreConstants.EventSourceActivitiesPromotionPatch,
                CoreConstants.EarningsSourceActivitiesPromotion);
        }

        /// <summary>
        /// Captures one spa-treatment activity result event.
        /// </summary>
        internal void CaptureActivitySpaTreatment(ActivityActionSnapshot snapshotBefore)
        {
            ActivityActionSnapshot fallbackSnapshot = snapshotBefore ?? CreateActivityActionSnapshot();
            ActivityActionEventPayload payload = new ActivityActionEventPayload
            {
                activity_type = CoreConstants.EarningsSourceActivitiesSpaTreatment,
                activity_level = CoreConstants.ZeroBasedListStartIndex,
                activity_bonus = 1f,
                money_before = fallbackSnapshot.MoneyBefore,
                money_after = resources.Money(),
                money_delta = resources.Money() - fallbackSnapshot.MoneyBefore,
                fans_before = fallbackSnapshot.FansBefore,
                fans_after = resources.GetFansTotal(null),
                fans_delta = resources.GetFansTotal(null) - fallbackSnapshot.FansBefore,
                active_idol_count_before = fallbackSnapshot.ActiveIdolCountBefore,
                active_idol_count_after = data_girls.GetActiveGirlsCounter(null),
                spa_heal = Activities.GetSpaHeal(),
                spa_cost = Activities.GetSpaCost(),
                activity_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            EmitActivityActionEvent(
                payload,
                CoreConstants.EventTypeActivitySpaTreatment,
                CoreConstants.EventSourceActivitiesSpaTreatmentPatch,
                CoreConstants.EarningsSourceActivitiesSpaTreatment);
        }

        /// <summary>
        /// Captures pre-mutation idol earnings snapshot.
        /// </summary>
        internal IdolEarningsSnapshot CreateIdolEarningsSnapshot(data_girls.girls idol)
        {
            return new IdolEarningsSnapshot
            {
                EarningsCurrentMonthBefore = idol != null ? idol.Earnings_CurrentMonth : CoreConstants.ZeroLongValue
            };
        }

        /// <summary>
        /// Captures one per-idol earnings mutation with inferred source attribution.
        /// </summary>
        internal void CaptureIdolEarnings(data_girls.girls idol, long earningsDelta, IdolEarningsSnapshot snapshotBefore, string earningsSource)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            if (earningsDelta == CoreConstants.ZeroLongValue)
            {
                return;
            }

            long earningsBefore = snapshotBefore != null
                ? snapshotBefore.EarningsCurrentMonthBefore
                : idol.Earnings_CurrentMonth - earningsDelta;
            long earningsAfter = idol.Earnings_CurrentMonth;
            string sourceCode = string.IsNullOrEmpty(earningsSource)
                ? CoreConstants.EarningsSourceUnknown
                : earningsSource;

            IdolEarningsEventPayload payload = new IdolEarningsEventPayload
            {
                idol_id = idol.id,
                earnings_delta = earningsDelta,
                earnings_current_month_before = earningsBefore,
                earnings_current_month_after = earningsAfter,
                earnings_source = sourceCode,
                earnings_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    idol.id,
                    CoreConstants.EventEntityKindIdolEarnings,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeIdolEarningsRecorded,
                    CoreConstants.EventSourceDataGirlsEarnPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Emits one activity action event with shared persistence wiring.
        /// </summary>
        private void EmitActivityActionEvent(
            ActivityActionEventPayload payload,
            string eventType,
            string sourcePatch,
            string activityEntityIdentifier)
        {
            if (payload == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindActivity,
                    activityEntityIdentifier ?? CoreConstants.StatusCodeUnknown,
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-destroy snapshot for one theater id.
        /// </summary>
        internal TheaterLifecycleSnapshot CreateTheaterDestroySnapshot(int theaterId)
        {
            Theaters._theater theater = Theaters.GetTheater(theaterId);
            agency._room theaterRoom = theater != null ? theater.GetRoom() : null;
            return CreateTheaterLifecycleSnapshot(theater, theaterRoom);
        }

        /// <summary>
        /// Captures one theater-created lifecycle event.
        /// </summary>
        internal void CaptureTheaterCreated(agency._room room)
        {
            if (room == null || room.TheaterID < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            Theaters._theater theater = Theaters.GetTheater(room.TheaterID);
            TheaterLifecycleSnapshot snapshot = CreateTheaterLifecycleSnapshot(theater, room);
            EmitTheaterLifecycleEvent(
                snapshot,
                CoreConstants.TheaterLifecycleActionCreated,
                CoreConstants.EventTypeTheaterCreated,
                CoreConstants.EventSourceTheatersNewTheaterPatch);
        }

        /// <summary>
        /// Captures one theater-destroyed lifecycle event.
        /// </summary>
        internal void CaptureTheaterDestroyed(TheaterLifecycleSnapshot snapshotBefore)
        {
            EmitTheaterLifecycleEvent(
                snapshotBefore,
                CoreConstants.TheaterLifecycleActionDestroyed,
                CoreConstants.EventTypeTheaterDestroyed,
                CoreConstants.EventSourceTheatersDestroyTheaterPatch);
        }

        /// <summary>
        /// Captures pre-complete-day snapshot for theater daily result derivation.
        /// </summary>
        internal TheaterCompleteDaySnapshot CreateTheaterCompleteDaySnapshot()
        {
            TheaterCompleteDaySnapshot snapshot = new TheaterCompleteDaySnapshot
            {
                MoneyBefore = resources.Money()
            };

            if (Theaters.Theaters_ == null)
            {
                return snapshot;
            }

            for (int theaterIndex = CoreConstants.ZeroBasedListStartIndex; theaterIndex < Theaters.Theaters_.Count; theaterIndex++)
            {
                Theaters._theater theater = Theaters.Theaters_[theaterIndex];
                if (theater == null || theater.ID < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                snapshot.StatCountByTheaterId[theater.ID] = theater.Stats != null ? theater.Stats.Count : CoreConstants.ZeroBasedListStartIndex;
                snapshot.SubscriberTotalByTheaterId[theater.ID] = theater.GetSubscribers();
            }

            return snapshot;
        }

        /// <summary>
        /// Captures per-theater daily completion rows after `CompleteDay`.
        /// </summary>
        internal void CaptureTheaterCompleteDay(TheaterCompleteDaySnapshot snapshotBefore)
        {
            if (snapshotBefore == null || Theaters.Theaters_ == null || Theaters.Theaters_.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            long moneyAfter = resources.Money();
            for (int theaterIndex = CoreConstants.ZeroBasedListStartIndex; theaterIndex < Theaters.Theaters_.Count; theaterIndex++)
            {
                Theaters._theater theater = Theaters.Theaters_[theaterIndex];
                if (theater == null || theater.ID < CoreConstants.MinimumValidIdolIdentifier || theater.Stats == null)
                {
                    continue;
                }

                int statCountBefore;
                if (!snapshotBefore.StatCountByTheaterId.TryGetValue(theater.ID, out statCountBefore))
                {
                    statCountBefore = CoreConstants.ZeroBasedListStartIndex;
                }

                if (theater.Stats.Count <= statCountBefore || theater.Stats.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    continue;
                }

                Theaters._theater._stat latestStat = theater.Stats[theater.Stats.Count - CoreConstants.LastElementOffsetFromCount];
                if (latestStat == null)
                {
                    continue;
                }

                long subscribersBefore;
                if (!snapshotBefore.SubscriberTotalByTheaterId.TryGetValue(theater.ID, out subscribersBefore))
                {
                    subscribersBefore = CoreConstants.ZeroLongValue;
                }

                TheaterDailyResultEventPayload payload = new TheaterDailyResultEventPayload
                {
                    theater_id = theater.ID,
                    theater_title = theater.GetTitle() ?? string.Empty,
                    group_id = theater.Group,
                    result_date = latestStat.Date == default(DateTime)
                        ? CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                        : CoreDateTimeUtility.ToRoundTripString(latestStat.Date),
                    schedule_type = latestStat.Schedule != null
                        ? CoreEnumNameMapping.ToTheaterScheduleTypeCode(latestStat.Schedule.Type)
                        : CoreConstants.StatusCodeUnknown,
                    schedule_fan_type_everyone = latestStat.Schedule != null && latestStat.Schedule.FanType_Everyone,
                    schedule_fan_type = latestStat.Schedule != null
                        ? CoreEnumNameMapping.ToFanTypeCode(latestStat.Schedule.FanType)
                        : CoreConstants.StatusCodeUnknown,
                    attendance = latestStat.Attendance,
                    revenue = latestStat.Revenue,
                    subscribers_delta = latestStat.Subscribers,
                    subscribers_total = theater.GetSubscribers(),
                    avg_attendance_7d = theater.GetAvgAttendance(),
                    avg_revenue_7d = theater.GetAvgRevenue(),
                    weekly_schedule_summary = BuildTheaterScheduleSummary(theater),
                    total_money_before = snapshotBefore.MoneyBefore,
                    total_money_after = moneyAfter,
                    total_money_delta = moneyAfter - snapshotBefore.MoneyBefore
                };

                lock (runtimeLock)
                {
                    string errorMessage;
                    if (!EnsureInitializedLocked(out errorMessage))
                    {
                        CoreLog.Warn(errorMessage);
                        return;
                    }

                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindTheater,
                        theater.ID.ToString(CultureInfo.InvariantCulture),
                        CoreConstants.EventTypeTheaterDailyResult,
                        CoreConstants.EventSourceTheatersCompleteDayPatch,
                        CoreJsonUtility.SerializeObjectPayload(payload));
                }
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Emits one theater lifecycle event from a normalized theater snapshot.
        /// </summary>
        private void EmitTheaterLifecycleEvent(
            TheaterLifecycleSnapshot snapshot,
            string lifecycleAction,
            string eventType,
            string sourcePatch)
        {
            if (snapshot == null || snapshot.TheaterId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            TheaterLifecycleEventPayload payload = new TheaterLifecycleEventPayload
            {
                theater_id = snapshot.TheaterId,
                theater_lifecycle_action = lifecycleAction ?? string.Empty,
                theater_title = snapshot.TheaterTitle ?? string.Empty,
                group_id = snapshot.GroupId,
                room_theater_id = snapshot.RoomTheaterId,
                ticket_price = snapshot.TicketPrice,
                subscription_price = snapshot.SubscriptionPrice,
                streaming_researched = snapshot.StreamingResearched,
                equipment_purchased = snapshot.EquipmentPurchased,
                subscriber_total = snapshot.SubscriberTotal,
                schedule_summary = snapshot.ScheduleSummary ?? string.Empty,
                lifecycle_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTheater,
                    snapshot.TheaterId.ToString(CultureInfo.InvariantCulture),
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates a normalized lifecycle snapshot for one theater.
        /// </summary>
        private static TheaterLifecycleSnapshot CreateTheaterLifecycleSnapshot(Theaters._theater theater, agency._room room)
        {
            TheaterLifecycleSnapshot snapshot = new TheaterLifecycleSnapshot();
            if (theater == null)
            {
                return snapshot;
            }

            snapshot.TheaterId = theater.ID;
            snapshot.TheaterTitle = theater.GetTitle() ?? string.Empty;
            snapshot.GroupId = theater.Group;
            snapshot.RoomTheaterId = room != null
                ? room.TheaterID
                : (theater.GetRoom() != null ? theater.GetRoom().TheaterID : CoreConstants.InvalidIdValue);
            snapshot.TicketPrice = theater.Ticket_Price;
            snapshot.SubscriptionPrice = theater.Subscription_Price;
            snapshot.StreamingResearched = theater.Streaming_Researched;
            snapshot.EquipmentPurchased = theater.Equipment_Purchased;
            snapshot.SubscriberTotal = theater.GetSubscribers();
            snapshot.ScheduleSummary = BuildTheaterScheduleSummary(theater);
            return snapshot;
        }

        /// <summary>
        /// Builds one compact summary of weekly theater schedule slots.
        /// </summary>
        private static string BuildTheaterScheduleSummary(Theaters._theater theater)
        {
            if (theater == null || theater.Schedule == null || theater.Schedule.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(theater.Schedule.Count * 16);
            for (int scheduleIndex = CoreConstants.ZeroBasedListStartIndex; scheduleIndex < theater.Schedule.Count; scheduleIndex++)
            {
                Theaters._theater._schedule schedule = theater.Schedule[scheduleIndex];
                if (scheduleIndex > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                summaryBuilder.Append(scheduleIndex.ToString(CultureInfo.InvariantCulture));
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                if (schedule == null)
                {
                    summaryBuilder.Append(CoreConstants.StatusCodeUnknown);
                    continue;
                }

                summaryBuilder.Append(CoreEnumNameMapping.ToTheaterScheduleTypeCode(schedule.Type));
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                if (schedule.FanType_Everyone)
                {
                    summaryBuilder.Append(CoreConstants.TheaterScheduleFanTypeEveryone);
                }
                else
                {
                    summaryBuilder.Append(CoreEnumNameMapping.ToFanTypeCode(schedule.FanType));
                }
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Captures pre-destroy snapshot for one cafe id.
        /// </summary>
        internal CafeLifecycleSnapshot CreateCafeDestroySnapshot(int cafeId)
        {
            Cafes._cafe cafe = Cafes.GetCafe(cafeId);
            agency._room room = ResolveCafeRoom(cafeId);
            return CreateCafeLifecycleSnapshot(cafe, room);
        }

        /// <summary>
        /// Captures one cafe-created lifecycle event.
        /// </summary>
        internal void CaptureCafeCreated(agency._room room)
        {
            if (room == null || room.TheaterID < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            Cafes._cafe cafe = Cafes.GetCafe(room.TheaterID);
            CafeLifecycleSnapshot snapshot = CreateCafeLifecycleSnapshot(cafe, room);
            EmitCafeLifecycleEvent(
                snapshot,
                CoreConstants.CafeLifecycleActionCreated,
                CoreConstants.EventTypeCafeCreated,
                CoreConstants.EventSourceCafesNewCafePatch);
        }

        /// <summary>
        /// Captures one cafe-destroyed lifecycle event.
        /// </summary>
        internal void CaptureCafeDestroyed(CafeLifecycleSnapshot snapshotBefore)
        {
            EmitCafeLifecycleEvent(
                snapshotBefore,
                CoreConstants.CafeLifecycleActionDestroyed,
                CoreConstants.EventTypeCafeDestroyed,
                CoreConstants.EventSourceCafesDestroyCafePatch);
        }

        /// <summary>
        /// Captures pre-render snapshot for one cafe daily processing step.
        /// </summary>
        internal CafeRenderSnapshot CreateCafeRenderSnapshot(Cafes._cafe cafe)
        {
            return new CafeRenderSnapshot
            {
                MoneyBefore = resources.Money(),
                StatCountBefore = cafe != null && cafe.Stats != null
                    ? cafe.Stats.Count
                    : CoreConstants.ZeroBasedListStartIndex
            };
        }

        /// <summary>
        /// Captures one cafe daily render result row.
        /// </summary>
        internal void CaptureCafeRenderResult(agency._room room, Cafes._cafe cafe, CafeRenderSnapshot snapshotBefore)
        {
            if (cafe == null || cafe.ID < CoreConstants.MinimumValidIdolIdentifier || cafe.Stats == null || snapshotBefore == null)
            {
                return;
            }

            if (cafe.Stats.Count <= snapshotBefore.StatCountBefore || cafe.Stats.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            Cafes._cafe._stat latestStat = cafe.Stats[cafe.Stats.Count - CoreConstants.LastElementOffsetFromCount];
            if (latestStat == null)
            {
                return;
            }

            Cafes._cafe._dish dish = cafe.GetDishByID(latestStat.Dish_ID);
            long moneyAfter = resources.Money();
            CafeDailyResultEventPayload payload = new CafeDailyResultEventPayload
            {
                cafe_id = cafe.ID,
                cafe_title = cafe.Title ?? string.Empty,
                group_id = cafe.Group,
                dish_id = latestStat.Dish_ID,
                dish_type = dish != null
                    ? CoreEnumNameMapping.ToCafeDishTypeCode(dish.Type)
                    : CoreConstants.StatusCodeUnknown,
                dish_title = dish != null ? (dish.Title ?? string.Empty) : string.Empty,
                profit = latestStat.Profit,
                new_fans = latestStat.New_Fans,
                fan_type = CoreEnumNameMapping.ToFanTypeCode(latestStat.Fan_Type),
                staffed_idol_count = latestStat.Girls != null ? latestStat.Girls.Count : CoreConstants.ZeroBasedListStartIndex,
                staffed_idol_id_list = latestStat.Girls != null
                    ? BuildDelimitedIdentifierList(latestStat.Girls)
                    : string.Empty,
                result_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime),
                total_money_before = snapshotBefore.MoneyBefore,
                total_money_after = moneyAfter,
                total_money_delta = moneyAfter - snapshotBefore.MoneyBefore
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindCafe,
                    cafe.ID.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeCafeDailyResult,
                    CoreConstants.EventSourceCafesRenderCafePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Emits one cafe lifecycle event from a normalized snapshot.
        /// </summary>
        private void EmitCafeLifecycleEvent(
            CafeLifecycleSnapshot snapshot,
            string lifecycleAction,
            string eventType,
            string sourcePatch)
        {
            if (snapshot == null || snapshot.CafeId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            CafeLifecycleEventPayload payload = new CafeLifecycleEventPayload
            {
                cafe_id = snapshot.CafeId,
                cafe_lifecycle_action = lifecycleAction ?? string.Empty,
                cafe_title = snapshot.CafeTitle ?? string.Empty,
                group_id = snapshot.GroupId,
                room_theater_id = snapshot.RoomTheaterId,
                wait_staff_count = snapshot.WaitStaffCount,
                working_staff_count = snapshot.WorkingStaffCount,
                cafe_priority = snapshot.CafePriority ?? string.Empty,
                staff_priority = snapshot.StaffPriority ?? string.Empty,
                menu_summary = snapshot.MenuSummary ?? string.Empty,
                lifecycle_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindCafe,
                    snapshot.CafeId.ToString(CultureInfo.InvariantCulture),
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates a normalized lifecycle snapshot for one cafe.
        /// </summary>
        private static CafeLifecycleSnapshot CreateCafeLifecycleSnapshot(Cafes._cafe cafe, agency._room room)
        {
            CafeLifecycleSnapshot snapshot = new CafeLifecycleSnapshot();
            if (cafe == null)
            {
                return snapshot;
            }

            snapshot.CafeId = cafe.ID;
            snapshot.CafeTitle = cafe.Title ?? string.Empty;
            snapshot.GroupId = cafe.Group;
            snapshot.RoomTheaterId = room != null ? room.TheaterID : CoreConstants.InvalidIdValue;
            snapshot.WaitStaffCount = cafe.WaitStaff != null ? cafe.WaitStaff.Count : CoreConstants.ZeroBasedListStartIndex;
            snapshot.WorkingStaffCount = cafe.WorkingGirls != null ? cafe.WorkingGirls.Count : CoreConstants.ZeroBasedListStartIndex;
            snapshot.CafePriority = CoreEnumNameMapping.ToCafePriorityCode(cafe.Cafe_Prio);
            snapshot.StaffPriority = CoreEnumNameMapping.ToCafeStaffPriorityCode(cafe.Staff_Prio);
            snapshot.MenuSummary = BuildCafeMenuSummary(cafe);
            return snapshot;
        }

        /// <summary>
        /// Resolves one room reference for cafe id by scanning agency room list.
        /// </summary>
        private static agency._room ResolveCafeRoom(int cafeId)
        {
            List<agency._room> rooms = agency.GetRooms(agency._type.cafeAndShop);
            if (rooms == null)
            {
                return null;
            }

            for (int roomIndex = CoreConstants.ZeroBasedListStartIndex; roomIndex < rooms.Count; roomIndex++)
            {
                agency._room room = rooms[roomIndex];
                if (room != null && room.TheaterID == cafeId)
                {
                    return room;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds one compact summary of cafe weekly menu configuration.
        /// </summary>
        private static string BuildCafeMenuSummary(Cafes._cafe cafe)
        {
            if (cafe == null || cafe.Menu == null || cafe.Menu.Length < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(cafe.Menu.Length * 16);
            for (int dayIndex = CoreConstants.ZeroBasedListStartIndex; dayIndex < cafe.Menu.Length; dayIndex++)
            {
                if (dayIndex > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                summaryBuilder.Append(dayIndex.ToString(CultureInfo.InvariantCulture));
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                int dishId = cafe.Menu[dayIndex];
                summaryBuilder.Append(dishId.ToString(CultureInfo.InvariantCulture));
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one staff lifecycle operation.
        /// </summary>
        internal StaffLifecycleSnapshot CreateStaffLifecycleSnapshot(staff._staff staffer)
        {
            StaffLifecycleSnapshot snapshot = new StaffLifecycleSnapshot
            {
                ScandalPointsBefore = resources.Get(resources.type.scandalPoints, true),
                MoneyBefore = resources.Money()
            };

            if (staffer == null)
            {
                return snapshot;
            }

            snapshot.StaffId = staffer.id;
            snapshot.StaffName = staffer.GetName(true, false) ?? string.Empty;
            snapshot.StaffType = CoreEnumNameMapping.ToStaffTypeCode(staffer.type);
            snapshot.StaffUniqueType = CoreEnumNameMapping.ToStaffUniqueTypeCode(staffer.UniqueType);
            snapshot.StaffSalary = staffer.salary;
            snapshot.StaffLevelBefore = staffer.GetLevel();
            snapshot.CanFireBefore = staffer.CanFire();
            snapshot.CanFireSeveranceBefore = staffer.CanFire_Severance();
            snapshot.FirePointsCostBefore = staffer.PointsToFire();
            snapshot.SeveranceCostBefore = staffer.Severance();
            snapshot.HireDate = staffer.HireDate == default(DateTime)
                ? string.Empty
                : CoreDateTimeUtility.ToRoundTripString(staffer.HireDate);
            snapshot.WasInStaffListBefore = staff.Staff != null && staff.Staff.Contains(staffer);

            agency._type? roomType = staffer.GetRoomType();
            snapshot.RoomType = roomType.HasValue
                ? CoreEnumNameMapping.ToAgencyRoomTypeCode(roomType.Value)
                : CoreConstants.StatusCodeUnknown;
            return snapshot;
        }

        /// <summary>
        /// Captures one staff-hired lifecycle event.
        /// </summary>
        internal void CaptureStaffHired(staff._staff staffer)
        {
            EmitStaffLifecycleEvent(
                staffer,
                null,
                CoreConstants.EventTypeStaffHired,
                CoreConstants.EventSourceStaffHirePatch,
                CoreConstants.StaffLifecycleActionHired,
                false);
        }

        /// <summary>
        /// Captures one staff-fire lifecycle event.
        /// </summary>
        internal void CaptureStaffFired(staff._staff staffer, bool force, StaffLifecycleSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            bool stillInStaffList = staff.Staff != null && staff.Staff.Contains(staffer);
            if (stillInStaffList)
            {
                return;
            }

            EmitStaffLifecycleEvent(
                staffer,
                snapshotBefore,
                CoreConstants.EventTypeStaffFired,
                CoreConstants.EventSourceStaffFirePatch,
                CoreConstants.StaffLifecycleActionFired,
                force);
        }

        /// <summary>
        /// Captures one staff severance-fire lifecycle event.
        /// </summary>
        internal void CaptureStaffFiredSeverance(staff._staff staffer, StaffLifecycleSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            bool stillInStaffList = staff.Staff != null && staff.Staff.Contains(staffer);
            if (stillInStaffList)
            {
                return;
            }

            EmitStaffLifecycleEvent(
                staffer,
                snapshotBefore,
                CoreConstants.EventTypeStaffFiredSeverance,
                CoreConstants.EventSourceStaffFireSeverancePatch,
                CoreConstants.StaffLifecycleActionFiredSeverance,
                false);
        }

        /// <summary>
        /// Captures one staff level-up lifecycle event.
        /// </summary>
        internal void CaptureStaffLevelUp(staff._staff staffer, StaffLifecycleSnapshot snapshotBefore)
        {
            if (staffer == null)
            {
                return;
            }

            int previousLevel = snapshotBefore != null ? snapshotBefore.StaffLevelBefore : staffer.GetLevel();
            int currentLevel = staffer.GetLevel();
            if (previousLevel == currentLevel && !staffer.LevelledUp)
            {
                return;
            }

            EmitStaffLifecycleEvent(
                staffer,
                snapshotBefore,
                CoreConstants.EventTypeStaffLevelUp,
                CoreConstants.EventSourceStaffLevelUpPatch,
                CoreConstants.StaffLifecycleActionLevelUp,
                false);
        }

        /// <summary>
        /// Emits one staff lifecycle event with before/after economy context.
        /// </summary>
        private void EmitStaffLifecycleEvent(
            staff._staff staffer,
            StaffLifecycleSnapshot snapshotBefore,
            string eventType,
            string sourcePatch,
            string actionCode,
            bool fireForced)
        {
            int staffId = staffer != null ? staffer.id : (snapshotBefore != null ? snapshotBefore.StaffId : CoreConstants.InvalidIdValue);
            if (staffId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            string staffName = staffer != null
                ? (staffer.GetName(true, false) ?? string.Empty)
                : (snapshotBefore != null ? snapshotBefore.StaffName : string.Empty);
            string staffType = staffer != null
                ? CoreEnumNameMapping.ToStaffTypeCode(staffer.type)
                : (snapshotBefore != null ? snapshotBefore.StaffType : CoreConstants.StatusCodeUnknown);
            string uniqueType = staffer != null
                ? CoreEnumNameMapping.ToStaffUniqueTypeCode(staffer.UniqueType)
                : (snapshotBefore != null ? snapshotBefore.StaffUniqueType : CoreConstants.StatusCodeUnknown);
            int levelBefore = snapshotBefore != null ? snapshotBefore.StaffLevelBefore : (staffer != null ? staffer.GetLevel() : CoreConstants.ZeroBasedListStartIndex);
            int levelAfter = staffer != null ? staffer.GetLevel() : levelBefore;

            string roomType = CoreConstants.StatusCodeUnknown;
            if (staffer != null)
            {
                agency._type? resolvedRoomType = staffer.GetRoomType();
                roomType = resolvedRoomType.HasValue
                    ? CoreEnumNameMapping.ToAgencyRoomTypeCode(resolvedRoomType.Value)
                    : CoreConstants.StatusCodeUnknown;
            }
            else if (snapshotBefore != null)
            {
                roomType = snapshotBefore.RoomType ?? CoreConstants.StatusCodeUnknown;
            }

            StaffLifecycleEventPayload payload = new StaffLifecycleEventPayload
            {
                staff_id = staffId,
                staff_name = staffName,
                staff_type = staffType,
                staff_unique_type = uniqueType,
                staff_action = actionCode ?? string.Empty,
                staff_salary = staffer != null ? staffer.salary : (snapshotBefore != null ? snapshotBefore.StaffSalary : CoreConstants.ZeroBasedListStartIndex),
                staff_level_before = levelBefore,
                staff_level_after = levelAfter,
                fire_forced = fireForced,
                can_fire_before = snapshotBefore != null && snapshotBefore.CanFireBefore,
                can_fire_severance_before = snapshotBefore != null && snapshotBefore.CanFireSeveranceBefore,
                fire_points_cost = snapshotBefore != null ? snapshotBefore.FirePointsCostBefore : CoreConstants.ZeroBasedListStartIndex,
                severance_cost = snapshotBefore != null ? snapshotBefore.SeveranceCostBefore : CoreConstants.ZeroBasedListStartIndex,
                scandal_points_before = snapshotBefore != null ? snapshotBefore.ScandalPointsBefore : resources.Get(resources.type.scandalPoints, true),
                scandal_points_after = resources.Get(resources.type.scandalPoints, true),
                money_before = snapshotBefore != null ? snapshotBefore.MoneyBefore : resources.Money(),
                money_after = resources.Money(),
                room_type = roomType,
                hire_date = snapshotBefore != null ? snapshotBefore.HireDate : (staffer != null && staffer.HireDate != default(DateTime) ? CoreDateTimeUtility.ToRoundTripString(staffer.HireDate) : string.Empty),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindStaff,
                    staffId.ToString(CultureInfo.InvariantCulture),
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one research category parameter assignment.
        /// </summary>
        internal ResearchSetParamSnapshot CreateResearchSetParamSnapshot(Research.category category)
        {
            ResearchSetParamSnapshot snapshot = new ResearchSetParamSnapshot
            {
                ResearchPointsBefore = category != null ? category.GetPoints() : CoreConstants.ZeroLongValue
            };

            if (category == null || category.Param == null)
            {
                return snapshot;
            }

            snapshot.PreviousParamTypeCode = CoreEnumNameMapping.ToSingleParamTypeCode(category.Param.type);
            snapshot.PreviousParamId = category.Param.id;
            snapshot.PreviousParamTitle = category.Param.GetTitle() ?? string.Empty;
            return snapshot;
        }

        /// <summary>
        /// Captures one research category parameter assignment event.
        /// </summary>
        internal void CaptureResearchParamAssigned(Research.category category, singles._param prm, ResearchSetParamSnapshot snapshotBefore)
        {
            if (category == null || snapshotBefore == null)
            {
                return;
            }

            singles._param assignedParam = category.Param;
            string previousParamTypeCode = snapshotBefore.PreviousParamTypeCode ?? CoreConstants.StatusCodeUnknown;
            int previousParamId = snapshotBefore.PreviousParamId;
            string previousParamTitle = snapshotBefore.PreviousParamTitle ?? string.Empty;
            string newParamTypeCode = assignedParam != null
                ? CoreEnumNameMapping.ToSingleParamTypeCode(assignedParam.type)
                : CoreConstants.StatusCodeUnknown;
            int newParamId = assignedParam != null ? assignedParam.id : CoreConstants.InvalidIdValue;
            string newParamTitle = assignedParam != null ? (assignedParam.GetTitle() ?? string.Empty) : string.Empty;

            if (string.Equals(previousParamTypeCode, newParamTypeCode, StringComparison.Ordinal)
                && previousParamId == newParamId
                && string.Equals(previousParamTitle, newParamTitle, StringComparison.Ordinal))
            {
                return;
            }

            string researchTypeCode = CoreEnumNameMapping.ToResearchTypeCode(category.Type);
            ResearchParamAssignmentEventPayload payload = new ResearchParamAssignmentEventPayload
            {
                research_type = researchTypeCode,
                previous_param_type = previousParamTypeCode,
                previous_param_id = previousParamId,
                previous_param_title = previousParamTitle,
                new_param_type = newParamTypeCode,
                new_param_id = newParamId,
                new_param_title = newParamTitle,
                research_points_before = snapshotBefore.ResearchPointsBefore,
                research_points_after = category.GetPoints(),
                has_param_after = assignedParam != null,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindResearch,
                    researchTypeCode,
                    CoreConstants.EventTypeResearchParamAssigned,
                    CoreConstants.EventSourceResearchCategorySetParamPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one research points purchase.
        /// </summary>
        internal ResearchBuyPointsSnapshot CreateResearchBuyPointsSnapshot(Research.category category)
        {
            return new ResearchBuyPointsSnapshot
            {
                MoneyBefore = resources.Money(),
                BuyingCostBefore = Research.Buying_Cost,
                PointsBefore = category != null ? category.GetPoints() : CoreConstants.ZeroLongValue,
                PointsRawBefore = category != null ? category.Points : 0f
            };
        }

        /// <summary>
        /// Captures one research points purchase event.
        /// </summary>
        internal void CaptureResearchPointsPurchased(Research.category category, ResearchBuyPointsSnapshot snapshotBefore)
        {
            if (category == null || snapshotBefore == null)
            {
                return;
            }

            long pointsAfter = category.GetPoints();
            float pointsRawAfter = category.Points;
            long moneyAfter = resources.Money();
            long buyingCostAfter = Research.Buying_Cost;
            if (snapshotBefore.PointsBefore == pointsAfter
                && Math.Abs(snapshotBefore.PointsRawBefore - pointsRawAfter) <= Mathf.Epsilon
                && snapshotBefore.MoneyBefore == moneyAfter
                && snapshotBefore.BuyingCostBefore == buyingCostAfter)
            {
                return;
            }

            string researchTypeCode = CoreEnumNameMapping.ToResearchTypeCode(category.Type);
            ResearchPointsPurchaseEventPayload payload = new ResearchPointsPurchaseEventPayload
            {
                research_type = researchTypeCode,
                points_before = snapshotBefore.PointsBefore,
                points_after = pointsAfter,
                points_raw_before = snapshotBefore.PointsRawBefore,
                points_raw_after = pointsRawAfter,
                purchase_cost_before = snapshotBefore.BuyingCostBefore,
                purchase_cost_after = buyingCostAfter,
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindResearch,
                    researchTypeCode,
                    CoreConstants.EventTypeResearchPointsPurchased,
                    CoreConstants.EventSourceResearchCategoryBuyPointsPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one research points accrual tick.
        /// </summary>
        internal ResearchAddPointsSnapshot CreateResearchAddPointsSnapshot(Research.category category)
        {
            return new ResearchAddPointsSnapshot
            {
                PointsBefore = category != null ? category.GetPoints() : CoreConstants.ZeroLongValue,
                PointsRawBefore = category != null ? category.Points : 0f
            };
        }

        /// <summary>
        /// Captures one research points accrual event from `Research.category.AddPoints`.
        /// </summary>
        internal void CaptureResearchPointsAccrued(Research.category category, float requestedDelta, ResearchAddPointsSnapshot snapshotBefore)
        {
            if (category == null || snapshotBefore == null)
            {
                return;
            }

            long pointsAfter = category.GetPoints();
            float pointsRawAfter = category.Points;
            float pointsRawDelta = pointsRawAfter - snapshotBefore.PointsRawBefore;
            long pointsDelta = pointsAfter - snapshotBefore.PointsBefore;
            if (Math.Abs(pointsRawDelta) <= Mathf.Epsilon && pointsDelta == CoreConstants.ZeroLongValue)
            {
                return;
            }

            string researchTypeCode = CoreEnumNameMapping.ToResearchTypeCode(category.Type);
            singles._param activeParam = category.Param;
            ResearchPointsAccruedEventPayload payload = new ResearchPointsAccruedEventPayload
            {
                research_type = researchTypeCode,
                points_raw_before = snapshotBefore.PointsRawBefore,
                points_raw_after = pointsRawAfter,
                points_raw_delta = pointsRawDelta,
                points_before = snapshotBefore.PointsBefore,
                points_after = pointsAfter,
                points_delta = pointsDelta,
                points_requested_delta = requestedDelta,
                active_param_type = activeParam != null
                    ? CoreEnumNameMapping.ToSingleParamTypeCode(activeParam.type)
                    : CoreConstants.StatusCodeUnknown,
                active_param_id = activeParam != null ? activeParam.id : CoreConstants.InvalidIdValue,
                active_param_title = activeParam != null ? (activeParam.GetTitle() ?? string.Empty) : string.Empty,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindResearch,
                    researchTypeCode,
                    CoreConstants.EventTypeResearchPointsAccrued,
                    CoreConstants.EventSourceResearchCategoryAddPointsPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one single research-parameter level-up.
        /// </summary>
        internal ResearchParamLevelUpSnapshot CreateResearchParamLevelUpSnapshot(singles._param param)
        {
            ResearchParamLevelUpSnapshot snapshot = new ResearchParamLevelUpSnapshot();
            if (param == null)
            {
                return snapshot;
            }

            Research.category category = param.GetResearchCategory();
            snapshot.LevelBefore = param.GetLevel(false);
            snapshot.ParamExpBefore = param.exp;
            snapshot.PointsNeededBefore = param.PointsNeededForUpgrade();
            snapshot.SavedPointsBefore = param.GetSavedPoints();
            snapshot.CategoryPointsRawBefore = category != null ? category.Points : 0f;
            snapshot.ResearchTypeCode = category != null
                ? CoreEnumNameMapping.ToResearchTypeCode(category.Type)
                : CoreConstants.StatusCodeUnknown;
            return snapshot;
        }

        /// <summary>
        /// Captures one single research-parameter level-up event.
        /// </summary>
        internal void CaptureResearchParamLevelUp(singles._param param, ResearchParamLevelUpSnapshot snapshotBefore)
        {
            if (param == null || snapshotBefore == null)
            {
                return;
            }

            int levelAfter = param.GetLevel(false);
            if (levelAfter <= snapshotBefore.LevelBefore)
            {
                return;
            }

            Research.category category = param.GetResearchCategory();
            long savedPointsAfter = param.GetSavedPoints();
            float categoryPointsRawAfter = category != null ? category.Points : snapshotBefore.CategoryPointsRawBefore;
            string researchTypeCode = category != null
                ? CoreEnumNameMapping.ToResearchTypeCode(category.Type)
                : snapshotBefore.ResearchTypeCode;
            string paramEntityIdentifier = BuildResearchParamEntityIdentifier(param);

            ResearchParamLevelUpEventPayload payload = new ResearchParamLevelUpEventPayload
            {
                research_type = researchTypeCode,
                param_type = CoreEnumNameMapping.ToSingleParamTypeCode(param.type),
                param_id = param.id,
                param_title = param.GetTitle() ?? string.Empty,
                level_before = snapshotBefore.LevelBefore,
                level_after = levelAfter,
                points_needed_before = snapshotBefore.PointsNeededBefore,
                saved_points_before = snapshotBefore.SavedPointsBefore,
                saved_points_after = savedPointsAfter,
                param_exp_before = snapshotBefore.ParamExpBefore,
                param_exp_after = param.exp,
                category_points_raw_before = snapshotBefore.CategoryPointsRawBefore,
                category_points_raw_after = categoryPointsRawAfter,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindResearch,
                    paramEntityIdentifier,
                    CoreConstants.EventTypeResearchParamLevelUp,
                    CoreConstants.EventSourceSinglesParamLevelUpPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Builds one stable research parameter entity identifier.
        /// </summary>
        private static string BuildResearchParamEntityIdentifier(singles._param param)
        {
            if (param == null)
            {
                return CoreConstants.StatusCodeUnknown;
            }

            return string.Concat(
                CoreEnumNameMapping.ToSingleParamTypeCode(param.type),
                CoreConstants.SaveKeyJoinSeparator,
                param.id.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one route-lock operation.
        /// </summary>
        internal RouteLockSnapshot CreateRouteLockSnapshot()
        {
            RouteLockSnapshot snapshot = new RouteLockSnapshot
            {
                RouteBefore = tasks.Story_Data != null ? tasks.Story_Data.Route : tasks._route.NONE
            };

            if (tasks.ActiveTasks == null)
            {
                return snapshot;
            }

            for (int taskIndex = CoreConstants.ZeroBasedListStartIndex; taskIndex < tasks.ActiveTasks.Count; taskIndex++)
            {
                tasks._task task = tasks.ActiveTasks[taskIndex];
                string taskIdentifier = BuildTaskIdentifier(task, null);
                if (!string.IsNullOrEmpty(taskIdentifier))
                {
                    snapshot.ActiveTaskCustomsBefore.Add(taskIdentifier);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Captures one story route-lock event.
        /// </summary>
        internal void CaptureRouteLocked(tasks._route requestedRoute, RouteLockSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            string routeBeforeCode = CoreEnumNameMapping.ToTaskRouteCode(snapshotBefore.RouteBefore);
            tasks._route routeAfter = tasks.Story_Data != null ? tasks.Story_Data.Route : requestedRoute;
            string routeAfterCode = CoreEnumNameMapping.ToTaskRouteCode(routeAfter);

            List<string> activeTaskIdentifiersAfter = new List<string>();
            if (tasks.ActiveTasks != null)
            {
                for (int taskIndex = CoreConstants.ZeroBasedListStartIndex; taskIndex < tasks.ActiveTasks.Count; taskIndex++)
                {
                    tasks._task task = tasks.ActiveTasks[taskIndex];
                    string taskIdentifier = BuildTaskIdentifier(task, null);
                    if (!string.IsNullOrEmpty(taskIdentifier))
                    {
                        activeTaskIdentifiersAfter.Add(taskIdentifier);
                    }
                }
            }

            List<string> removedTaskIdentifiers = ResolveRemovedTaskIdentifiers(
                snapshotBefore.ActiveTaskCustomsBefore,
                activeTaskIdentifiersAfter);
            if (string.Equals(routeBeforeCode, routeAfterCode, StringComparison.Ordinal)
                && removedTaskIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            StoryRouteLockEventPayload payload = new StoryRouteLockEventPayload
            {
                route_before = routeBeforeCode,
                route_after = routeAfterCode,
                active_task_count_before = snapshotBefore.ActiveTaskCustomsBefore != null
                    ? snapshotBefore.ActiveTaskCustomsBefore.Count
                    : CoreConstants.ZeroBasedListStartIndex,
                active_task_count_after = activeTaskIdentifiersAfter.Count,
                removed_task_count = removedTaskIdentifiers.Count,
                removed_task_custom_list = removedTaskIdentifiers.Count >= CoreConstants.MinimumNonEmptyCollectionCount
                    ? string.Join(CoreConstants.IdentifierListSeparator, removedTaskIdentifiers.ToArray())
                    : string.Empty,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTask,
                    routeAfterCode,
                    CoreConstants.EventTypeStoryRouteLocked,
                    CoreConstants.EventSourceTasksLockRoutePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one task lifecycle operation.
        /// </summary>
        internal TaskLifecycleSnapshot CreateTaskLifecycleSnapshot(tasks._task task)
        {
            TaskLifecycleSnapshot snapshot = new TaskLifecycleSnapshot();
            if (task == null)
            {
                return snapshot;
            }

            snapshot.TaskCustom = task.Custom ?? string.Empty;
            snapshot.TaskTypeCode = CoreEnumNameMapping.ToTaskTypeCode(task.Type);
            snapshot.TaskGoalCode = CoreEnumNameMapping.ToTaskGoalCode(task.Goal);
            snapshot.TaskSubstory = task.Substory;
            snapshot.TaskGirlId = task.Girl != null ? task.Girl.id : CoreConstants.InvalidIdValue;
            snapshot.TaskSkillCode = CoreEnumNameMapping.ToIdolParameterCode(task.Skill);
            snapshot.TaskAgentName = task.AgentName ?? string.Empty;
            snapshot.FulfilledBefore = task.Fulfilled;
            snapshot.WasActiveBefore = tasks.ActiveTasks != null && tasks.ActiveTasks.Contains(task);
            snapshot.RouteCodeBefore = CoreEnumNameMapping.ToTaskRouteCode(tasks.Story_Data != null ? tasks.Story_Data.Route : tasks._route.NONE);
            snapshot.AvailableFrom = task.AvailableFrom.HasValue
                ? CoreDateTimeUtility.ToRoundTripString(task.AvailableFrom.Value)
                : string.Empty;
            return snapshot;
        }

        /// <summary>
        /// Captures one task completion event.
        /// </summary>
        internal void CaptureTaskCompleted(tasks._task task, TaskLifecycleSnapshot snapshotBefore)
        {
            if (task == null || snapshotBefore == null)
            {
                return;
            }

            if (snapshotBefore.FulfilledBefore || !task.Fulfilled)
            {
                return;
            }

            EmitTaskLifecycleEvent(
                task,
                snapshotBefore,
                CoreConstants.EventTypeTaskCompleted,
                CoreConstants.EventSourceTasksTaskOnCompletePatch,
                CoreConstants.TaskLifecycleActionCompleted,
                false);
        }

        /// <summary>
        /// Captures one task failure event.
        /// </summary>
        internal void CaptureTaskFailed(tasks._task task, TaskLifecycleSnapshot snapshotBefore)
        {
            if (task == null || snapshotBefore == null)
            {
                return;
            }

            bool activeAfter = tasks.ActiveTasks != null && tasks.ActiveTasks.Contains(task);
            if (snapshotBefore.WasActiveBefore == activeAfter && snapshotBefore.FulfilledBefore == task.Fulfilled)
            {
                return;
            }

            EmitTaskLifecycleEvent(
                task,
                snapshotBefore,
                CoreConstants.EventTypeTaskFailed,
                CoreConstants.EventSourceTasksTaskOnFailPatch,
                CoreConstants.TaskLifecycleActionFailed,
                false);
        }

        /// <summary>
        /// Captures one task done event.
        /// </summary>
        internal void CaptureTaskDone(tasks._task task, bool skipLock, TaskLifecycleSnapshot snapshotBefore)
        {
            if (task == null || snapshotBefore == null)
            {
                return;
            }

            bool activeAfter = tasks.ActiveTasks != null && tasks.ActiveTasks.Contains(task);
            if (snapshotBefore.WasActiveBefore && activeAfter)
            {
                return;
            }

            EmitTaskLifecycleEvent(
                task,
                snapshotBefore,
                CoreConstants.EventTypeTaskDone,
                CoreConstants.EventSourceTasksTaskDonePatch,
                CoreConstants.TaskLifecycleActionDone,
                skipLock);
        }

        /// <summary>
        /// Emits one task lifecycle event with normalized before/after task context.
        /// </summary>
        private void EmitTaskLifecycleEvent(
            tasks._task task,
            TaskLifecycleSnapshot snapshotBefore,
            string eventType,
            string sourcePatch,
            string actionCode,
            bool skipLock)
        {
            if (task == null || snapshotBefore == null)
            {
                return;
            }

            bool activeAfter = tasks.ActiveTasks != null && tasks.ActiveTasks.Contains(task);
            int taskGirlId = task.Girl != null ? task.Girl.id : snapshotBefore.TaskGirlId;

            TaskLifecycleEventPayload payload = new TaskLifecycleEventPayload
            {
                task_action = actionCode ?? string.Empty,
                task_custom = task.Custom ?? snapshotBefore.TaskCustom ?? string.Empty,
                task_type = CoreEnumNameMapping.ToTaskTypeCode(task.Type),
                task_goal = CoreEnumNameMapping.ToTaskGoalCode(task.Goal),
                task_substory = task.Substory,
                task_girl_id = taskGirlId,
                task_skill = CoreEnumNameMapping.ToIdolParameterCode(task.Skill),
                task_agent_name = task.AgentName ?? string.Empty,
                fulfilled_before = snapshotBefore.FulfilledBefore,
                fulfilled_after = task.Fulfilled,
                active_before = snapshotBefore.WasActiveBefore,
                active_after = activeAfter,
                skip_lock = skipLock,
                route = CoreEnumNameMapping.ToTaskRouteCode(tasks.Story_Data != null ? tasks.Story_Data.Route : tasks._route.NONE),
                available_from = snapshotBefore.AvailableFrom ?? string.Empty,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            string entityIdentifier = BuildTaskIdentifier(task, snapshotBefore);
            int idolIdForEvent = taskGirlId >= CoreConstants.MinimumValidIdolIdentifier
                ? taskGirlId
                : CoreConstants.InvalidIdValue;

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    idolIdForEvent,
                    CoreConstants.EventEntityKindTask,
                    entityIdentifier,
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Builds one stable task identifier based on explicit custom id or fallback metadata.
        /// </summary>
        private static string BuildTaskIdentifier(tasks._task task, TaskLifecycleSnapshot snapshotBefore)
        {
            string customId = task != null ? (task.Custom ?? string.Empty) : (snapshotBefore != null ? snapshotBefore.TaskCustom ?? string.Empty : string.Empty);
            if (!string.IsNullOrEmpty(customId))
            {
                return customId;
            }

            string taskTypeCode = task != null
                ? CoreEnumNameMapping.ToTaskTypeCode(task.Type)
                : (snapshotBefore != null ? snapshotBefore.TaskTypeCode ?? CoreConstants.StatusCodeUnknown : CoreConstants.StatusCodeUnknown);
            string taskGoalCode = task != null
                ? CoreEnumNameMapping.ToTaskGoalCode(task.Goal)
                : (snapshotBefore != null ? snapshotBefore.TaskGoalCode ?? CoreConstants.StatusCodeUnknown : CoreConstants.StatusCodeUnknown);
            int taskGirlId = task != null && task.Girl != null
                ? task.Girl.id
                : (snapshotBefore != null ? snapshotBefore.TaskGirlId : CoreConstants.InvalidIdValue);

            return string.Concat(
                taskTypeCode,
                CoreConstants.SaveKeyJoinSeparator,
                taskGoalCode,
                CoreConstants.SaveKeyJoinSeparator,
                taskGirlId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Resolves task identifiers removed after route lock by multiset difference.
        /// </summary>
        private static List<string> ResolveRemovedTaskIdentifiers(IReadOnlyList<string> beforeIdentifiers, IReadOnlyList<string> afterIdentifiers)
        {
            List<string> removedIdentifiers = new List<string>();
            if (beforeIdentifiers == null || beforeIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return removedIdentifiers;
            }

            Dictionary<string, int> afterCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (afterIdentifiers != null)
            {
                for (int afterIndex = CoreConstants.ZeroBasedListStartIndex; afterIndex < afterIdentifiers.Count; afterIndex++)
                {
                    string afterIdentifier = afterIdentifiers[afterIndex] ?? string.Empty;
                    if (afterCounts.ContainsKey(afterIdentifier))
                    {
                        afterCounts[afterIdentifier]++;
                    }
                    else
                    {
                        afterCounts[afterIdentifier] = 1;
                    }
                }
            }

            for (int beforeIndex = CoreConstants.ZeroBasedListStartIndex; beforeIndex < beforeIdentifiers.Count; beforeIndex++)
            {
                string beforeIdentifier = beforeIdentifiers[beforeIndex] ?? string.Empty;
                int afterCount;
                if (afterCounts.TryGetValue(beforeIdentifier, out afterCount) && afterCount > CoreConstants.ZeroBasedListStartIndex)
                {
                    afterCounts[beforeIdentifier] = afterCount - 1;
                    continue;
                }

                removedIdentifiers.Add(beforeIdentifier);
            }

            return removedIdentifiers;
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one concert tactical card play.
        /// </summary>
        internal ConcertCardUseSnapshot CreateConcertCardUseSnapshot(SEvent_Concerts manager)
        {
            ConcertCardUseSnapshot snapshot = new ConcertCardUseSnapshot();
            SEvent_Concerts._concert concert = manager != null ? manager.Concert : null;
            if (concert == null)
            {
                return snapshot;
            }

            snapshot.ConcertId = concert.ID;
            snapshot.CardsBefore = concert.Cards != null ? concert.Cards.Count : CoreConstants.ZeroBasedListStartIndex;
            snapshot.CardAccidentHappeningBefore = concert.Card_Accident_Happening;
            snapshot.CardAccidentSuccessBefore = concert.Card_Accident_Success_Chance;
            snapshot.CardNoCriticalFailureBefore = concert.Card_No_Critical_Failure;
            snapshot.NoAccidentCounterBefore = concert.No_Accident_Counter;
            snapshot.UsedAccidentCountBefore = concert.UsedAccidents != null ? concert.UsedAccidents.Count : CoreConstants.ZeroBasedListStartIndex;
            return snapshot;
        }

        /// <summary>
        /// Captures one concert tactical card usage event.
        /// </summary>
        internal void CaptureConcertCardUsed(SEvent_Concerts manager, SEvent_Concerts._card card, ConcertCardUseSnapshot snapshotBefore)
        {
            if (manager == null || snapshotBefore == null)
            {
                return;
            }

            SEvent_Concerts._concert concert = manager.Concert;
            if (concert == null || snapshotBefore.ConcertId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            int cardsAfter = concert.Cards != null ? concert.Cards.Count : CoreConstants.ZeroBasedListStartIndex;
            float accidentHappeningAfter = concert.Card_Accident_Happening;
            float accidentSuccessAfter = concert.Card_Accident_Success_Chance;
            bool noCriticalFailureAfter = concert.Card_No_Critical_Failure;
            int noAccidentCounterAfter = concert.No_Accident_Counter;
            int usedAccidentCountAfter = concert.UsedAccidents != null ? concert.UsedAccidents.Count : CoreConstants.ZeroBasedListStartIndex;

            bool changed =
                cardsAfter != snapshotBefore.CardsBefore
                || Math.Abs(accidentHappeningAfter - snapshotBefore.CardAccidentHappeningBefore) > Mathf.Epsilon
                || Math.Abs(accidentSuccessAfter - snapshotBefore.CardAccidentSuccessBefore) > Mathf.Epsilon
                || noCriticalFailureAfter != snapshotBefore.CardNoCriticalFailureBefore
                || noAccidentCounterAfter != snapshotBefore.NoAccidentCounterBefore
                || usedAccidentCountAfter != snapshotBefore.UsedAccidentCountBefore;
            if (!changed)
            {
                return;
            }

            ConcertCardUsedEventPayload payload = new ConcertCardUsedEventPayload
            {
                concert_id = concert.ID,
                card_type = card != null ? CoreEnumNameMapping.ToConcertCardTypeCode(card.Type) : CoreConstants.StatusCodeUnknown,
                card_level = card != null ? card.Level : CoreConstants.ZeroBasedListStartIndex,
                card_effect_value = card != null ? card.EffectVal() : CoreConstants.ZeroBasedListStartIndex,
                cards_before = snapshotBefore.CardsBefore,
                cards_after = cardsAfter,
                card_consumed = cardsAfter < snapshotBefore.CardsBefore,
                card_accident_happening_before = snapshotBefore.CardAccidentHappeningBefore,
                card_accident_happening_after = accidentHappeningAfter,
                card_accident_success_before = snapshotBefore.CardAccidentSuccessBefore,
                card_accident_success_after = accidentSuccessAfter,
                card_no_critical_failure_before = snapshotBefore.CardNoCriticalFailureBefore,
                card_no_critical_failure_after = noCriticalFailureAfter,
                no_accident_counter_before = snapshotBefore.NoAccidentCounterBefore,
                no_accident_counter_after = noAccidentCounterAfter,
                used_accident_count_before = snapshotBefore.UsedAccidentCountBefore,
                used_accident_count_after = usedAccidentCountAfter,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string concertEntityIdentifier = concert.ID.ToString(CultureInfo.InvariantCulture);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindConcert,
                    concertEntityIdentifier,
                    CoreConstants.EventTypeConcertCardUsed,
                    CoreConstants.EventSourceConcertUseCardPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one concert crisis choice/result event from `Concert_CrisisPopup.Option`.
        /// </summary>
        internal ConcertCrisisChoiceSnapshot CaptureConcertCrisisDecision(Concert_CrisisPopup popup, bool safeChoice)
        {
            ConcertCrisisChoiceSnapshot snapshot = new ConcertCrisisChoiceSnapshot
            {
                ChoiceSafe = safeChoice,
                ChoiceTypeCode = safeChoice ? CoreConstants.ConcertCrisisChoiceSafe : CoreConstants.ConcertCrisisChoiceRisky
            };

            SEvent_Concerts._concert concert = ResolveConcertFromCrisisPopup(popup);
            SEvent_Concerts._accident accident = ResolveAccidentFromCrisisPopup(popup);
            SEvent_Concerts._accident._result result = ResolveResultFromCrisisPopup(popup);

            if (concert != null)
            {
                snapshot.ConcertId = concert.ID;
            }

            if (accident != null)
            {
                snapshot.AccidentTitle = accident.Title ?? string.Empty;
            }

            if (result != null)
            {
                snapshot.ResultTypeCode = CoreEnumNameMapping.ToConcertAccidentResultTypeCode(result.Type);
                snapshot.ExpectedHypeDelta = result.Hype;
            }

            if (snapshot.ConcertId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return snapshot;
            }

            ConcertCrisisDecisionEventPayload payload = new ConcertCrisisDecisionEventPayload
            {
                concert_id = snapshot.ConcertId,
                accident_title = snapshot.AccidentTitle,
                choice_safe = snapshot.ChoiceSafe,
                choice_type = snapshot.ChoiceTypeCode,
                result_type = snapshot.ResultTypeCode ?? CoreConstants.StatusCodeUnknown,
                result_hype_delta = snapshot.ExpectedHypeDelta,
                accident_success_chance = concert != null ? concert.AccidentSuccessChance() : CoreConstants.ZeroBasedListStartIndex,
                no_critical_failure = concert != null && concert.Card_No_Critical_Failure,
                used_accident_count = concert != null && concert.UsedAccidents != null
                    ? concert.UsedAccidents.Count
                    : CoreConstants.ZeroBasedListStartIndex,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return snapshot;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindConcert,
                    snapshot.ConcertId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeConcertCrisisDecision,
                    CoreConstants.EventSourceConcertCrisisOptionPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }

            return snapshot;
        }

        /// <summary>
        /// Captures pre-mutation snapshot before `Concert_CrisisPopup.Close` applies hype effects.
        /// </summary>
        internal ConcertCrisisAppliedSnapshot CreateConcertCrisisAppliedSnapshot(Concert_CrisisPopup popup, ConcertCrisisChoiceSnapshot choiceSnapshot)
        {
            ConcertCrisisAppliedSnapshot snapshot = new ConcertCrisisAppliedSnapshot();
            if (choiceSnapshot != null)
            {
                snapshot.ConcertId = choiceSnapshot.ConcertId;
                snapshot.AccidentTitle = choiceSnapshot.AccidentTitle ?? string.Empty;
                snapshot.ChoiceSafe = choiceSnapshot.ChoiceSafe;
                snapshot.ChoiceTypeCode = choiceSnapshot.ChoiceTypeCode ?? CoreConstants.ConcertCrisisChoiceUnknown;
                snapshot.ResultTypeCode = choiceSnapshot.ResultTypeCode ?? CoreConstants.StatusCodeUnknown;
                snapshot.ExpectedHypeDelta = choiceSnapshot.ExpectedHypeDelta;
            }

            SEvent_Concerts._concert concert = ResolveConcertFromCrisisPopup(popup);
            SEvent_Concerts._accident accident = ResolveAccidentFromCrisisPopup(popup);
            SEvent_Concerts._accident._result result = ResolveResultFromCrisisPopup(popup);

            if (concert != null)
            {
                snapshot.ConcertId = concert.ID;
                snapshot.HypeBefore = concert.Hype;
            }

            if (string.IsNullOrEmpty(snapshot.AccidentTitle) && accident != null)
            {
                snapshot.AccidentTitle = accident.Title ?? string.Empty;
            }

            if ((string.IsNullOrEmpty(snapshot.ResultTypeCode) || string.Equals(snapshot.ResultTypeCode, CoreConstants.StatusCodeUnknown, StringComparison.Ordinal))
                && result != null)
            {
                snapshot.ResultTypeCode = CoreEnumNameMapping.ToConcertAccidentResultTypeCode(result.Type);
            }

            if (result != null)
            {
                snapshot.ExpectedHypeDelta = result.Hype;
            }

            return snapshot;
        }

        /// <summary>
        /// Captures one concert crisis outcome application event after close applies hype delta.
        /// </summary>
        internal void CaptureConcertCrisisApplied(Concert_CrisisPopup popup, ConcertCrisisAppliedSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            SEvent_Concerts._concert concert = ResolveConcertFromCrisisPopup(popup);
            int concertId = concert != null ? concert.ID : snapshotBefore.ConcertId;
            if (concertId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            float hypeAfter = concert != null ? concert.Hype : snapshotBefore.HypeBefore;
            ConcertCrisisAppliedEventPayload payload = new ConcertCrisisAppliedEventPayload
            {
                concert_id = concertId,
                accident_title = snapshotBefore.AccidentTitle ?? string.Empty,
                choice_safe = snapshotBefore.ChoiceSafe,
                choice_type = snapshotBefore.ChoiceTypeCode ?? CoreConstants.ConcertCrisisChoiceUnknown,
                result_type = snapshotBefore.ResultTypeCode ?? CoreConstants.StatusCodeUnknown,
                expected_hype_delta = snapshotBefore.ExpectedHypeDelta,
                hype_before = snapshotBefore.HypeBefore,
                hype_after = hypeAfter,
                hype_delta_applied = hypeAfter - snapshotBefore.HypeBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindConcert,
                    concertId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeConcertCrisisApplied,
                    CoreConstants.EventSourceConcertCrisisClosePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Resolves private concert reference from one crisis popup instance.
        /// </summary>
        private static SEvent_Concerts._concert ResolveConcertFromCrisisPopup(Concert_CrisisPopup popup)
        {
            if (popup == null)
            {
                return null;
            }

            try
            {
                object concertValue = Traverse.Create(popup).Field(CoreConstants.ReflectionConcertCrisisPopupConcertFieldName).GetValue();
                return concertValue as SEvent_Concerts._concert;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves private accident reference from one crisis popup instance.
        /// </summary>
        private static SEvent_Concerts._accident ResolveAccidentFromCrisisPopup(Concert_CrisisPopup popup)
        {
            if (popup == null)
            {
                return null;
            }

            try
            {
                object accidentValue = Traverse.Create(popup).Field(CoreConstants.ReflectionConcertCrisisPopupAccidentFieldName).GetValue();
                return accidentValue as SEvent_Concerts._accident;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves private selected-result reference from one crisis popup instance.
        /// </summary>
        private static SEvent_Concerts._accident._result ResolveResultFromCrisisPopup(Concert_CrisisPopup popup)
        {
            if (popup == null)
            {
                return null;
            }

            try
            {
                object resultValue = Traverse.Create(popup).Field(CoreConstants.ReflectionConcertCrisisPopupResultFieldName).GetValue();
                return resultValue as SEvent_Concerts._accident._result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for final concert resolution outcomes.
        /// </summary>
        internal ConcertFinishSnapshot CreateConcertFinishSnapshot(Concert_Popup popup)
        {
            ConcertFinishSnapshot snapshot = new ConcertFinishSnapshot
            {
                MoneyBefore = resources.Money()
            };

            SEvent_Concerts._concert concert = popup != null ? popup.Concert : null;
            if (concert == null)
            {
                return snapshot;
            }

            snapshot.ConcertId = concert.ID;
            if (concert.ProjectedValues != null)
            {
                snapshot.ActualRevenue = concert.ProjectedValues.Actual_Revenue;
                snapshot.ActualProfit = concert.ProjectedValues.GetActualProfit();
                snapshot.TicketPrice = concert.ProjectedValues.TicketPrice;
            }

            snapshot.UsedAccidentCountBefore = concert.UsedAccidents != null ? concert.UsedAccidents.Count : CoreConstants.ZeroBasedListStartIndex;
            snapshot.NoAccidentCounterBefore = concert.No_Accident_Counter;

            List<int> payoutRecipients = ResolveConcertEarningsRecipientIdolIdentifiers(concert);
            for (int recipientIndex = CoreConstants.ZeroBasedListStartIndex; recipientIndex < payoutRecipients.Count; recipientIndex++)
            {
                int idolId = payoutRecipients[recipientIndex];
                data_girls.girls idol = data_girls.GetGirlByID(idolId);
                if (idol == null)
                {
                    continue;
                }

                snapshot.EarningsBeforeByIdolId[idol.id] = idol.Earnings_CurrentMonth;
            }

            foreach (resources.fanType fanType in Enum.GetValues(typeof(resources.fanType)))
            {
                snapshot.FanOpinionScoreBeforeByType[fanType] = ResolveFanOpinionScore(fanType);
            }

            return snapshot;
        }

        /// <summary>
        /// Captures one final concert resolution event.
        /// </summary>
        internal void CaptureConcertFinalResolved(Concert_Popup popup, ConcertFinishSnapshot snapshotBefore)
        {
            if (popup == null || snapshotBefore == null)
            {
                return;
            }

            SEvent_Concerts._concert concert = popup.Concert;
            if (concert == null || snapshotBefore.ConcertId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            Dictionary<int, long> payoutByIdolId = new Dictionary<int, long>();
            long payoutTotal = CoreConstants.ZeroLongValue;
            if (snapshotBefore.EarningsBeforeByIdolId != null)
            {
                foreach (KeyValuePair<int, long> entry in snapshotBefore.EarningsBeforeByIdolId)
                {
                    data_girls.girls idol = data_girls.GetGirlByID(entry.Key);
                    if (idol == null)
                    {
                        continue;
                    }

                    long payoutDelta = idol.Earnings_CurrentMonth - entry.Value;
                    if (payoutDelta == CoreConstants.ZeroLongValue)
                    {
                        continue;
                    }

                    payoutByIdolId[idol.id] = payoutDelta;
                    payoutTotal += payoutDelta;
                }
            }

            List<string> fanOpinionShiftSegments = new List<string>();
            foreach (resources.fanType fanType in Enum.GetValues(typeof(resources.fanType)))
            {
                int beforeScore;
                if (!snapshotBefore.FanOpinionScoreBeforeByType.TryGetValue(fanType, out beforeScore))
                {
                    beforeScore = CoreConstants.ZeroBasedListStartIndex;
                }

                int afterScore = ResolveFanOpinionScore(fanType);
                int deltaScore = afterScore - beforeScore;
                if (deltaScore == CoreConstants.ZeroBasedListStartIndex)
                {
                    continue;
                }

                fanOpinionShiftSegments.Add(
                    string.Concat(
                        CoreEnumNameMapping.ToFanTypeCode(fanType),
                        CoreConstants.SingleFanSegmentValueSeparator,
                        deltaScore.ToString(CultureInfo.InvariantCulture)));
            }

            long moneyAfter = resources.Money();
            ConcertFinalResolvedEventPayload payload = new ConcertFinalResolvedEventPayload
            {
                concert_id = concert.ID,
                actual_revenue = concert.ProjectedValues != null ? concert.ProjectedValues.Actual_Revenue : snapshotBefore.ActualRevenue,
                actual_profit = concert.ProjectedValues != null ? concert.ProjectedValues.GetActualProfit() : snapshotBefore.ActualProfit,
                ticket_price = concert.ProjectedValues != null ? concert.ProjectedValues.TicketPrice : snapshotBefore.TicketPrice,
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                idol_payout_count = payoutByIdolId.Count,
                idol_payout_total = payoutTotal,
                idol_payout_by_id = BuildIdolDeltaSummary(payoutByIdolId),
                fan_opinion_shift = fanOpinionShiftSegments.Count >= CoreConstants.MinimumNonEmptyCollectionCount
                    ? string.Join(CoreConstants.SingleFanSegmentEntrySeparator, fanOpinionShiftSegments.ToArray())
                    : string.Empty,
                used_accident_count = concert.UsedAccidents != null ? concert.UsedAccidents.Count : snapshotBefore.UsedAccidentCountBefore,
                used_accident_titles = BuildAccidentTitleSummary(concert.UsedAccidents),
                no_accident_counter = concert.No_Accident_Counter,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string concertEntityIdentifier = concert.ID.ToString(CultureInfo.InvariantCulture);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindConcert,
                    concertEntityIdentifier,
                    CoreConstants.EventTypeConcertFinalResolved,
                    CoreConstants.EventSourceConcertPopupFinishPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Resolves ordered concert payout recipients from non-MC setlist songs.
        /// </summary>
        private static List<int> ResolveConcertEarningsRecipientIdolIdentifiers(SEvent_Concerts._concert concert)
        {
            List<int> recipients = new List<int>();
            if (concert == null || concert.SetListItems == null)
            {
                return recipients;
            }

            for (int itemIndex = CoreConstants.ZeroBasedListStartIndex; itemIndex < concert.SetListItems.Count; itemIndex++)
            {
                SEvent_Concerts._concert.ISetlistItem setlistItem = concert.SetListItems[itemIndex];
                if (setlistItem == null || setlistItem.isMC())
                {
                    continue;
                }

                List<data_girls.girls> girls = setlistItem.GetGirls(true);
                if (girls == null || girls.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    continue;
                }

                data_girls.girls singer = girls[CoreConstants.ZeroBasedListStartIndex];
                if (singer == null || singer.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (!recipients.Contains(singer.id))
                {
                    recipients.Add(singer.id);
                }
            }

            return recipients;
        }

        /// <summary>
        /// Resolves the signed fan-opinion score for one fan type.
        /// </summary>
        private static int ResolveFanOpinionScore(resources.fanType fanType)
        {
            resources._fanOpinion fanOpinion = resources.GetFanOpinion(fanType);
            if (fanOpinion == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return fanOpinion.GetPositive() - fanOpinion.GetNegative();
        }

        /// <summary>
        /// Builds a deterministic idol-to-delta summary string.
        /// </summary>
        private static string BuildIdolDeltaSummary(Dictionary<int, long> deltasByIdolId)
        {
            if (deltasByIdolId == null || deltasByIdolId.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            List<int> idolIds = new List<int>(deltasByIdolId.Keys);
            idolIds.Sort();

            StringBuilder builder = new StringBuilder(idolIds.Count * 16);
            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < idolIds.Count; idolIndex++)
            {
                if (idolIndex > CoreConstants.ZeroBasedListStartIndex)
                {
                    builder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                int idolId = idolIds[idolIndex];
                builder.Append(idolId.ToString(CultureInfo.InvariantCulture));
                builder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                builder.Append(deltasByIdolId[idolId].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Builds a deterministic summary string of accidents used during a concert.
        /// </summary>
        private static string BuildAccidentTitleSummary(IReadOnlyList<SEvent_Concerts._accident> usedAccidents)
        {
            if (usedAccidents == null || usedAccidents.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            List<string> titles = new List<string>();
            for (int accidentIndex = CoreConstants.ZeroBasedListStartIndex; accidentIndex < usedAccidents.Count; accidentIndex++)
            {
                SEvent_Concerts._accident accident = usedAccidents[accidentIndex];
                if (accident == null)
                {
                    continue;
                }

                string title = accident.Title ?? string.Empty;
                if (string.IsNullOrEmpty(title))
                {
                    continue;
                }

                titles.Add(title);
            }

            if (titles.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            return string.Join(CoreConstants.SingleFanSegmentEntrySeparator, titles.ToArray());
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one explicit election start operation.
        /// </summary>
        internal ElectionStartSnapshot CreateElectionStartSnapshot(SEvent_SSK manager)
        {
            ElectionStartSnapshot snapshot = new ElectionStartSnapshot
            {
                MoneyBefore = resources.Money()
            };

            SEvent_SSK._SSK election = manager != null ? manager.SSK : null;
            snapshot.StartCost = election != null ? election.GetProductionCost() : CoreConstants.ZeroLongValue;
            return snapshot;
        }

        /// <summary>
        /// Captures one explicit election started milestone event.
        /// </summary>
        internal void CaptureElectionStarted(SEvent_SSK._SSK election, ElectionStartSnapshot snapshotBefore)
        {
            if (election == null)
            {
                return;
            }

            ElectionStartSnapshot snapshot = snapshotBefore ?? new ElectionStartSnapshot();
            long moneyAfter = resources.Money();
            ElectionStartedEventPayload payload = new ElectionStartedEventPayload
            {
                election_id = election.ID,
                election_lifecycle_action = CoreConstants.ElectionLifecycleActionStarted,
                election_status = CoreEnumNameMapping.ToTourStatusCode(election.Status),
                election_broadcast_type = CoreEnumNameMapping.ToElectionBroadcastCode(election.Broadcast),
                election_single_id = ResolveSingleIdOrInvalid(election.Single),
                election_concert_id = ResolveConcertIdOrInvalid(election.Concert),
                election_release_single_id = ResolveSingleIdOrInvalid(election.ReleaseSingle),
                election_result_count = election.Results != null ? election.Results.Count : CoreConstants.ZeroBasedListStartIndex,
                election_finish_date = CoreDateTimeUtility.ToRoundTripString(election.FinishDate),
                start_cost = snapshot.StartCost,
                money_before = snapshot.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshot.MoneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string electionEntityIdentifier = election.ID.ToString(CultureInfo.InvariantCulture);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindElection,
                    electionEntityIdentifier,
                    CoreConstants.EventTypeElectionStarted,
                    CoreConstants.EventSourceElectionStartPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for one idol wish lifecycle operation.
        /// </summary>
        internal WishLifecycleSnapshot CreateWishLifecycleSnapshot(data_girls.girls idol)
        {
            WishLifecycleSnapshot snapshot = new WishLifecycleSnapshot();
            if (idol == null)
            {
                return snapshot;
            }

            snapshot.IdolId = idol.id;
            snapshot.WishTypeCodeBefore = CoreEnumNameMapping.ToWishTypeCode(idol.Wish_Type);
            snapshot.WishFormulaBefore = idol.Wish_Formula ?? string.Empty;
            snapshot.WishFulfilledBefore = idol.Wish_Fulfilled;
            snapshot.WishEffectUntilBefore = idol.Wish_Effect_Until;
            snapshot.InfluencePointsBefore = idol.GetRelationshipWithPlayer_Points(Relationships_Player._type.Influence);
            snapshot.InfluenceLevelBefore = idol.GetRelationshipLevel(Relationships_Player._type.Influence);
            snapshot.MentalStaminaBefore = ResolveIdolMentalStamina(idol);
            return snapshot;
        }

        /// <summary>
        /// Captures one wish generated event.
        /// </summary>
        internal void CaptureWishGenerated(data_girls.girls idol, WishLifecycleSnapshot snapshotBefore)
        {
            if (idol == null || snapshotBefore == null)
            {
                return;
            }

            string wishTypeAfter = CoreEnumNameMapping.ToWishTypeCode(idol.Wish_Type);
            string wishFormulaAfter = idol.Wish_Formula ?? string.Empty;
            if (string.Equals(snapshotBefore.WishTypeCodeBefore, wishTypeAfter, StringComparison.Ordinal)
                && string.Equals(snapshotBefore.WishFormulaBefore, wishFormulaAfter, StringComparison.Ordinal)
                && snapshotBefore.WishFulfilledBefore == idol.Wish_Fulfilled)
            {
                return;
            }

            EmitWishLifecycleEvent(
                idol,
                snapshotBefore,
                CoreConstants.EventTypeWishGenerated,
                CoreConstants.EventSourceDataGirlsWishGeneratePatch,
                CoreConstants.WishLifecycleActionGenerated);
        }

        /// <summary>
        /// Captures one wish fulfilled event.
        /// </summary>
        internal void CaptureWishFulfilled(data_girls.girls idol, WishLifecycleSnapshot snapshotBefore)
        {
            if (idol == null || snapshotBefore == null)
            {
                return;
            }

            if (snapshotBefore.WishFulfilledBefore || !idol.Wish_Fulfilled)
            {
                return;
            }

            EmitWishLifecycleEvent(
                idol,
                snapshotBefore,
                CoreConstants.EventTypeWishFulfilled,
                CoreConstants.EventSourceDataGirlsWishFulfillPatch,
                CoreConstants.WishLifecycleActionFulfilled);
        }

        /// <summary>
        /// Captures one wish done event.
        /// </summary>
        internal void CaptureWishDone(data_girls.girls idol, WishLifecycleSnapshot snapshotBefore)
        {
            if (idol == null || snapshotBefore == null)
            {
                return;
            }

            string wishTypeAfter = CoreEnumNameMapping.ToWishTypeCode(idol.Wish_Type);
            float mentalStaminaAfter = ResolveIdolMentalStamina(idol);
            int influencePointsAfter = idol.GetRelationshipWithPlayer_Points(Relationships_Player._type.Influence);
            if (string.Equals(snapshotBefore.WishTypeCodeBefore, wishTypeAfter, StringComparison.Ordinal)
                && string.Equals(snapshotBefore.WishFormulaBefore ?? string.Empty, idol.Wish_Formula ?? string.Empty, StringComparison.Ordinal)
                && snapshotBefore.WishFulfilledBefore == idol.Wish_Fulfilled
                && snapshotBefore.InfluencePointsBefore == influencePointsAfter
                && Math.Abs(snapshotBefore.MentalStaminaBefore - mentalStaminaAfter) <= Mathf.Epsilon)
            {
                return;
            }

            EmitWishLifecycleEvent(
                idol,
                snapshotBefore,
                CoreConstants.EventTypeWishDone,
                CoreConstants.EventSourceDataGirlsWishDonePatch,
                CoreConstants.WishLifecycleActionDone);
        }

        /// <summary>
        /// Emits one normalized wish lifecycle event.
        /// </summary>
        private void EmitWishLifecycleEvent(
            data_girls.girls idol,
            WishLifecycleSnapshot snapshotBefore,
            string eventType,
            string sourcePatch,
            string wishActionCode)
        {
            if (idol == null || snapshotBefore == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            int influencePointsAfter = idol.GetRelationshipWithPlayer_Points(Relationships_Player._type.Influence);
            int influenceLevelAfter = idol.GetRelationshipLevel(Relationships_Player._type.Influence);
            float mentalStaminaAfter = ResolveIdolMentalStamina(idol);

            WishLifecycleEventPayload payload = new WishLifecycleEventPayload
            {
                idol_id = idol.id,
                wish_action = wishActionCode ?? string.Empty,
                wish_type_before = snapshotBefore.WishTypeCodeBefore ?? CoreConstants.StatusCodeUnknown,
                wish_type_after = CoreEnumNameMapping.ToWishTypeCode(idol.Wish_Type),
                wish_formula_before = snapshotBefore.WishFormulaBefore ?? string.Empty,
                wish_formula_after = idol.Wish_Formula ?? string.Empty,
                wish_fulfilled_before = snapshotBefore.WishFulfilledBefore,
                wish_fulfilled_after = idol.Wish_Fulfilled,
                wish_effect_until_before = ResolveWishEffectDateString(snapshotBefore.WishEffectUntilBefore),
                wish_effect_until_after = ResolveWishEffectDateString(idol.Wish_Effect_Until),
                influence_points_before = snapshotBefore.InfluencePointsBefore,
                influence_points_after = influencePointsAfter,
                influence_points_delta = influencePointsAfter - snapshotBefore.InfluencePointsBefore,
                influence_level_before = snapshotBefore.InfluenceLevelBefore,
                influence_level_after = influenceLevelAfter,
                mental_stamina_before = snapshotBefore.MentalStaminaBefore,
                mental_stamina_after = mentalStaminaAfter,
                mental_stamina_delta = mentalStaminaAfter - snapshotBefore.MentalStaminaBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    idol.id,
                    CoreConstants.EventEntityKindWish,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Resolves current idol mental stamina, guarding null parameter slots.
        /// </summary>
        private static float ResolveIdolMentalStamina(data_girls.girls idol)
        {
            if (idol == null)
            {
                return 0f;
            }

            data_girls.girls.param mentalStamina = idol.getParam(data_girls._paramType.mentalStamina);
            return mentalStamina != null ? mentalStamina.val : 0f;
        }

        /// <summary>
        /// Formats wish-effect dates with empty output for uninitialized values.
        /// </summary>
        private static string ResolveWishEffectDateString(DateTime value)
        {
            if (value.Year <= 1901)
            {
                return string.Empty;
            }

            return CoreDateTimeUtility.ToRoundTripString(value);
        }

        /// <summary>
        /// Captures scandal-point mutations using raw float values for full precision.
        /// </summary>
        internal void CaptureScandalPointsMutation(
            data_girls.girls idol,
            float previousScandalPointsRaw,
            string scandalMutationSourceCode,
            string sourcePatchCode)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            float newScandalPointsRaw = ResolveIdolScandalPointsRawValue(idol);
            float sanitizedPreviousScandalPointsRaw = float.IsNaN(previousScandalPointsRaw)
                ? newScandalPointsRaw
                : previousScandalPointsRaw;

            float scandalPointDeltaRaw = newScandalPointsRaw - sanitizedPreviousScandalPointsRaw;
            if (Mathf.Abs(scandalPointDeltaRaw) <= CoreConstants.ScandalPointsDeltaEpsilon)
            {
                return;
            }

            int previousScandalPointsRounded = Mathf.RoundToInt(sanitizedPreviousScandalPointsRaw);
            int newScandalPointsRounded = Mathf.RoundToInt(newScandalPointsRaw);
            int scandalPointDeltaRounded = newScandalPointsRounded - previousScandalPointsRounded;

            ScandalPointsPayload payload = new ScandalPointsPayload
            {
                IdolId = idol.id,
                PreviousScandalPoints = previousScandalPointsRounded,
                NewScandalPoints = newScandalPointsRounded,
                ScandalPointDelta = scandalPointDeltaRounded,
                PreviousScandalPointsRaw = sanitizedPreviousScandalPointsRaw,
                NewScandalPointsRaw = newScandalPointsRaw,
                ScandalPointDeltaRaw = scandalPointDeltaRaw,
                ScandalMutationSource = scandalMutationSourceCode ?? string.Empty
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindScandal,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeScandalPointsChanged,
                    sourcePatchCode ?? CoreConstants.EventSourceScandalParameterAddPatch,
                    CoreJsonUtility.SerializeScandalPointsPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures medical lifecycle events as explicit, queryable history items.
        /// </summary>
        internal void CaptureMedicalLifecycleEvent(
            data_girls.girls idol,
            string medicalEventTypeCode,
            bool medicalFinishWasForced,
            data_girls._status previousStatus,
            string sourcePatchCode)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            MedicalEventPayload payload = new MedicalEventPayload
            {
                IdolId = idol.id,
                MedicalEventType = medicalEventTypeCode ?? string.Empty,
                MedicalPreviousStatus = CoreStatusMapping.ToStatusCode(previousStatus),
                MedicalCurrentStatus = CoreStatusMapping.ToStatusCode(idol.status),
                MedicalHiatusEndDate = idol.HiatusEnd == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(idol.HiatusEnd),
                MedicalFinishWasForced = medicalFinishWasForced,
                MedicalInjuryCounter = idol.Injury_Counter,
                MedicalDepressionCounter = idol.Depression_Counter
            };

            string medicalEventType = medicalEventTypeCode ?? string.Empty;
            string eventType;
            if (string.Equals(medicalEventType, CoreConstants.MedicalEventTypeInjury, StringComparison.Ordinal))
            {
                eventType = CoreConstants.EventTypeMedicalInjury;
            }
            else if (string.Equals(medicalEventType, CoreConstants.MedicalEventTypeDepression, StringComparison.Ordinal))
            {
                eventType = CoreConstants.EventTypeMedicalDepression;
            }
            else if (string.Equals(medicalEventType, CoreConstants.MedicalEventTypeHiatusStarted, StringComparison.Ordinal))
            {
                eventType = CoreConstants.EventTypeMedicalHiatusStarted;
            }
            else if (string.Equals(medicalEventType, CoreConstants.MedicalEventTypeHealed, StringComparison.Ordinal))
            {
                eventType = CoreConstants.EventTypeMedicalHealed;
            }
            else
            {
                eventType = CoreConstants.EventTypeMedicalHiatusFinished;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindMedical,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    eventType,
                    sourcePatchCode ?? CoreConstants.EventSourceMedicalInjuryPatch,
                    CoreJsonUtility.SerializeMedicalEventPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for agency room build operations.
        /// </summary>
        internal AgencyRoomBuildSnapshot CreateAgencyRoomBuildSnapshot(int requestedTypeRaw, bool buildFlag)
        {
            AgencyRoomBuildSnapshot snapshot = new AgencyRoomBuildSnapshot
            {
                MoneyBefore = resources.Money(),
                RequestedTypeRaw = requestedTypeRaw,
                BuildFlag = buildFlag
            };

            List<agency._room> rooms = agency.GetRooms();
            if (rooms == null)
            {
                return snapshot;
            }

            for (int roomIndex = CoreConstants.ZeroBasedListStartIndex; roomIndex < rooms.Count; roomIndex++)
            {
                agency._room room = rooms[roomIndex];
                if (room != null && room.id >= CoreConstants.MinimumValidIdolIdentifier)
                {
                    snapshot.ExistingRoomIds.Add(room.id);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Captures agency room build lifecycle and capex spend events.
        /// </summary>
        internal void CaptureAgencyRoomBuilt(agency agencySystem, int requestedTypeRaw, bool buildFlag, AgencyRoomBuildSnapshot snapshotBefore)
        {
            agency._type requestedType = requestedTypeRaw >= CoreConstants.ZeroBasedListStartIndex
                ? (agency._type)requestedTypeRaw
                : agency._type.yourOffice;
            agency._room builtRoom = FindBuiltAgencyRoom(snapshotBefore);
            if (builtRoom == null)
            {
                return;
            }

            int floorId;
            int floorIndex;
            TryResolveAgencyRoomLocation(agencySystem, builtRoom, out floorId, out floorIndex);

            long moneyBefore = snapshotBefore != null ? snapshotBefore.MoneyBefore : resources.Money();
            long moneyAfter = resources.Money();
            int roomCost = agencySystem != null ? agencySystem.roomCost(requestedType) : CoreConstants.ZeroBasedListStartIndex;
            string roomTypeCode = CoreEnumNameMapping.ToAgencyRoomTypeCode(requestedType);

            AgencyRoomLifecycleEventPayload lifecyclePayload = new AgencyRoomLifecycleEventPayload
            {
                room_id = builtRoom.id,
                room_type = roomTypeCode,
                floor_id = floorId,
                floor_index = floorIndex,
                room_space = agency.roomSpace(requestedType),
                room_cost = roomCost,
                build_flag = buildFlag,
                room_lifecycle_action = CoreConstants.AgencyRoomLifecycleActionBuilt,
                money_before = moneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - moneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            AgencyRoomCostPaidEventPayload costPayload = new AgencyRoomCostPaidEventPayload
            {
                room_id = builtRoom.id,
                room_type = roomTypeCode,
                room_cost = roomCost,
                build_flag = buildFlag,
                money_before = moneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - moneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindAgencyRoom,
                    builtRoom.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeAgencyRoomBuilt,
                    CoreConstants.EventSourceAgencyAddRoomPatch,
                    CoreJsonUtility.SerializeObjectPayload(lifecyclePayload));

                if (buildFlag && roomCost > CoreConstants.ZeroBasedListStartIndex && moneyAfter < moneyBefore)
                {
                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindAgencyRoom,
                        builtRoom.id.ToString(CultureInfo.InvariantCulture),
                        CoreConstants.EventTypeAgencyRoomCostPaid,
                        CoreConstants.EventSourceAgencyAddRoomPatch,
                        CoreJsonUtility.SerializeObjectPayload(costPayload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for agency room destruction.
        /// </summary>
        internal AgencyRoomDestroySnapshot CreateAgencyRoomDestroySnapshot(agency agencySystem, agency._room roomToDestroy)
        {
            AgencyRoomDestroySnapshot snapshot = new AgencyRoomDestroySnapshot
            {
                MoneyBefore = resources.Money()
            };

            if (roomToDestroy == null)
            {
                return snapshot;
            }

            snapshot.RoomId = roomToDestroy.id;
            snapshot.RoomType = roomToDestroy.type;
            snapshot.TheaterId = roomToDestroy.TheaterID;
            snapshot.RoomSpace = agency.roomSpace(roomToDestroy.type);
            snapshot.RoomCost = agencySystem != null ? agencySystem.roomCost(roomToDestroy.type) : CoreConstants.ZeroBasedListStartIndex;
            TryResolveAgencyRoomLocation(agencySystem, roomToDestroy, out snapshot.FloorId, out snapshot.FloorIndex);
            return snapshot;
        }

        /// <summary>
        /// Captures one agency room destruction lifecycle event.
        /// </summary>
        internal void CaptureAgencyRoomDestroyed(AgencyRoomDestroySnapshot snapshotBefore)
        {
            if (snapshotBefore == null || snapshotBefore.RoomId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            long moneyAfter = resources.Money();
            AgencyRoomLifecycleEventPayload payload = new AgencyRoomLifecycleEventPayload
            {
                room_id = snapshotBefore.RoomId,
                room_type = CoreEnumNameMapping.ToAgencyRoomTypeCode(snapshotBefore.RoomType),
                floor_id = snapshotBefore.FloorId,
                floor_index = snapshotBefore.FloorIndex,
                room_space = snapshotBefore.RoomSpace,
                room_cost = snapshotBefore.RoomCost,
                build_flag = false,
                room_lifecycle_action = CoreConstants.AgencyRoomLifecycleActionDestroyed,
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindAgencyRoom,
                    snapshotBefore.RoomId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeAgencyRoomDestroyed,
                    CoreConstants.EventSourceAgencyDestroyRoomPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for audition start.
        /// </summary>
        internal AuditionStartSnapshot CreateAuditionStartSnapshot(Auditions.data auditionData)
        {
            return new AuditionStartSnapshot
            {
                MoneyBefore = resources.Money(),
                ProgressBefore = auditionData != null ? auditionData.Progress : 0f,
                RegionalCooldownBefore = Auditions.Regional_Date,
                NationwideCooldownBefore = Auditions.Nationwide_Date
            };
        }

        /// <summary>
        /// Captures audition-start lifecycle and spend events.
        /// </summary>
        internal void CaptureAuditionStarted(Auditions.data auditionData, bool shouldPay, AuditionStartSnapshot snapshotBefore)
        {
            if (auditionData == null)
            {
                return;
            }

            string auditionTypeCode = ResolveAuditionTypeCode(auditionData.Type);
            int auditionCost = auditionData.GetCost();
            long moneyBefore = snapshotBefore != null ? snapshotBefore.MoneyBefore : resources.Money();
            long moneyAfter = resources.Money();

            AuditionStartedEventPayload startedPayload = new AuditionStartedEventPayload
            {
                audition_type = auditionTypeCode,
                should_pay = shouldPay,
                cost = auditionCost,
                progress_before = snapshotBefore != null ? snapshotBefore.ProgressBefore : 0f,
                progress_after = auditionData.Progress,
                generated_candidate_count = auditionData.Girls != null ? auditionData.Girls.Count : CoreConstants.ZeroBasedListStartIndex,
                regional_cooldown_before = ResolveDateString(snapshotBefore != null ? snapshotBefore.RegionalCooldownBefore : Auditions.Regional_Date),
                regional_cooldown_after = ResolveDateString(Auditions.Regional_Date),
                nationwide_cooldown_before = ResolveDateString(snapshotBefore != null ? snapshotBefore.NationwideCooldownBefore : Auditions.Nationwide_Date),
                nationwide_cooldown_after = ResolveDateString(Auditions.Nationwide_Date),
                money_before = moneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - moneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            AuditionCostPaidEventPayload costPayload = new AuditionCostPaidEventPayload
            {
                audition_type = auditionTypeCode,
                spend_reason = CoreConstants.EventTypeAuditionStarted,
                cost = auditionCost,
                money_before = moneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - moneyBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindAudition,
                    auditionTypeCode,
                    CoreConstants.EventTypeAuditionStarted,
                    CoreConstants.EventSourceAuditionsGeneratePatch,
                    CoreJsonUtility.SerializeObjectPayload(startedPayload));

                if (shouldPay && auditionCost > CoreConstants.ZeroBasedListStartIndex && moneyAfter < moneyBefore)
                {
                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindAudition,
                        auditionTypeCode,
                        CoreConstants.EventTypeAuditionCostPaid,
                        CoreConstants.EventSourceAuditionsGeneratePatch,
                        CoreJsonUtility.SerializeObjectPayload(costPayload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for audition cooldown reset.
        /// </summary>
        internal AuditionCooldownResetSnapshot CreateAuditionCooldownResetSnapshot(CM_Player_Audition_Cooldown._type resetType)
        {
            return new AuditionCooldownResetSnapshot
            {
                CanResetBefore = Auditions.CanResetCooldown(resetType),
                ResetCostBefore = Auditions.GetCooldownCost(resetType),
                RegionalDaysRemainingBefore = Auditions.GetDaysTillCanProduce(Auditions.type.regional),
                NationwideDaysRemainingBefore = Auditions.GetDaysTillCanProduce(Auditions.type.nationwide),
                RegionalCooldownBefore = Auditions.Regional_Date,
                NationwideCooldownBefore = Auditions.Nationwide_Date,
                MoneyBefore = resources.Money(),
                ResearchPlayerPointsBefore = Research.GetCategory(Research.type.player).GetPoints()
            };
        }

        /// <summary>
        /// Captures one audition cooldown reset request and spend outcome.
        /// </summary>
        internal void CaptureAuditionCooldownReset(CM_Player_Audition_Cooldown._type resetType, AuditionCooldownResetSnapshot snapshotBefore)
        {
            if (snapshotBefore == null || !snapshotBefore.CanResetBefore)
            {
                return;
            }

            long moneyAfter = resources.Money();
            long researchPointsAfter = Research.GetCategory(Research.type.player).GetPoints();
            bool resetApplied = !Auditions.IsOnCooldown();

            AuditionCooldownResetEventPayload payload = new AuditionCooldownResetEventPayload
            {
                reset_type = resetType.ToString().ToLowerInvariant(),
                can_reset_before = snapshotBefore.CanResetBefore,
                reset_cost = snapshotBefore.ResetCostBefore,
                regional_days_remaining_before = snapshotBefore.RegionalDaysRemainingBefore,
                nationwide_days_remaining_before = snapshotBefore.NationwideDaysRemainingBefore,
                regional_cooldown_before = ResolveDateString(snapshotBefore.RegionalCooldownBefore),
                regional_cooldown_after = ResolveDateString(Auditions.Regional_Date),
                nationwide_cooldown_before = ResolveDateString(snapshotBefore.NationwideCooldownBefore),
                nationwide_cooldown_after = ResolveDateString(Auditions.Nationwide_Date),
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                research_player_points_before = snapshotBefore.ResearchPlayerPointsBefore,
                research_player_points_after = researchPointsAfter,
                research_player_points_delta = researchPointsAfter - snapshotBefore.ResearchPlayerPointsBefore,
                reset_applied = resetApplied,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindAudition,
                    payload.reset_type,
                    CoreConstants.EventTypeAuditionCooldownReset,
                    CoreConstants.EventSourceAuditionsResetCooldownPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for random-event scheduling.
        /// </summary>
        internal RandomEventStartSnapshot CreateRandomEventStartSnapshot(Event_Manager manager)
        {
            return new RandomEventStartSnapshot
            {
                MoneyBefore = resources.Money(),
                FansBefore = resources.GetFansTotal(null),
                FameBefore = (int)resources.Get(resources.type.fame, true),
                BuzzBefore = (int)resources.Get(resources.type.buzz, true),
                ActiveEventCountBefore = manager != null && manager.activeEvents != null
                    ? manager.activeEvents.Count
                    : CoreConstants.ZeroBasedListStartIndex
            };
        }

        /// <summary>
        /// Captures one random-event start with actor assignment and immediate effects.
        /// </summary>
        internal void CaptureRandomEventStarted(
            Event_Manager manager,
            Event_Manager._randomEvent randomEvent,
            DateTime scheduledDate,
            bool forceRequested,
            RandomEventStartSnapshot snapshotBefore)
        {
            if (manager == null || randomEvent == null)
            {
                return;
            }

            Event_Manager._activeEvent activeEvent = FindMatchingActiveRandomEvent(manager, randomEvent, scheduledDate);
            if (activeEvent == null)
            {
                return;
            }

            long moneyBefore = snapshotBefore != null ? snapshotBefore.MoneyBefore : resources.Money();
            long fansBefore = snapshotBefore != null ? snapshotBefore.FansBefore : resources.GetFansTotal(null);
            int fameBefore = snapshotBefore != null ? snapshotBefore.FameBefore : (int)resources.Get(resources.type.fame, true);
            int buzzBefore = snapshotBefore != null ? snapshotBefore.BuzzBefore : (int)resources.Get(resources.type.buzz, true);

            long moneyAfter = resources.Money();
            long fansAfter = resources.GetFansTotal(null);
            int fameAfter = (int)resources.Get(resources.type.fame, true);
            int buzzAfter = (int)resources.Get(resources.type.buzz, true);

            RandomEventStartedEventPayload payload = new RandomEventStartedEventPayload
            {
                random_event_id = randomEvent.id ?? string.Empty,
                random_event_title = randomEvent.title ?? string.Empty,
                random_event_state = ResolveRandomEventStateCode(activeEvent.state),
                random_event_force = forceRequested,
                random_event_mod_name = randomEvent.ModName ?? string.Empty,
                scheduled_date = CoreDateTimeUtility.ToRoundTripString(scheduledDate),
                actor_count = activeEvent.actors != null ? activeEvent.actors.Count : CoreConstants.ZeroBasedListStartIndex,
                actors_summary = BuildRandomEventActorSummary(activeEvent.actors),
                requirement_count = randomEvent.requirement != null ? randomEvent.requirement.Count : CoreConstants.ZeroBasedListStartIndex,
                startup_effect_count = randomEvent.effects != null ? randomEvent.effects.Count : CoreConstants.ZeroBasedListStartIndex,
                money_before = moneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - moneyBefore,
                fans_before = fansBefore,
                fans_after = fansAfter,
                fans_delta = fansAfter - fansBefore,
                fame_before = fameBefore,
                fame_after = fameAfter,
                fame_delta = fameAfter - fameBefore,
                buzz_before = buzzBefore,
                buzz_after = buzzAfter,
                buzz_delta = buzzAfter - buzzBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRandomEvent,
                    payload.random_event_id,
                    CoreConstants.EventTypeRandomEventStarted,
                    CoreConstants.EventSourceEventManagerStartEventPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for random-event conclusion.
        /// </summary>
        internal RandomEventConcludeSnapshot CreateRandomEventConcludeSnapshot(Event_Manager manager, Event_Manager._randomEvent._reply reply)
        {
            RandomEventConcludeSnapshot snapshot = new RandomEventConcludeSnapshot
            {
                ActiveEventBefore = manager != null ? manager.GetActiveEvent() : null,
                MoneyBefore = resources.Money(),
                FansBefore = resources.GetFansTotal(null),
                FameBefore = (int)resources.Get(resources.type.fame, true),
                BuzzBefore = (int)resources.Get(resources.type.buzz, true)
            };

            if (snapshot.ActiveEventBefore == null)
            {
                return snapshot;
            }

            snapshot.ActiveEventStateBefore = ResolveRandomEventStateCode(snapshot.ActiveEventBefore.state);
            snapshot.ActorSummaryBefore = BuildRandomEventActorSummary(snapshot.ActiveEventBefore.actors);

            business businessSystem = Camera.main != null
                ? Camera.main.GetComponent<mainScript>().Data.GetComponent<business>()
                : null;
            if (businessSystem != null && reply != null)
            {
                snapshot.EstimatedLiabilityBefore = businessSystem.GetLiability(snapshot.ActiveEventBefore.actors, reply.Effects);
            }

            return snapshot;
        }

        /// <summary>
        /// Captures one random-event conclusion with selected reply/effect context.
        /// </summary>
        internal void CaptureRandomEventConcluded(
            Event_Manager manager,
            Event_Manager._randomEvent._reply reply,
            RandomEventConcludeSnapshot snapshotBefore)
        {
            if (manager == null || reply == null || snapshotBefore == null || snapshotBefore.ActiveEventBefore == null)
            {
                return;
            }

            Event_Manager._activeEvent activeEvent = snapshotBefore.ActiveEventBefore;
            Event_Manager._randomEvent randomEvent = activeEvent.data;
            if (randomEvent == null)
            {
                return;
            }

            long moneyAfter = resources.Money();
            long fansAfter = resources.GetFansTotal(null);
            int fameAfter = (int)resources.Get(resources.type.fame, true);
            int buzzAfter = (int)resources.Get(resources.type.buzz, true);

            RandomEventConcludedEventPayload payload = new RandomEventConcludedEventPayload
            {
                random_event_id = randomEvent.id ?? string.Empty,
                random_event_title = randomEvent.title ?? string.Empty,
                random_event_state_before = snapshotBefore.ActiveEventStateBefore,
                random_event_state_after = ResolveRandomEventStateCode(activeEvent.state),
                reply_index = ResolveRandomEventReplyIndex(randomEvent, reply),
                reply_text = reply.text ?? string.Empty,
                reply_description = reply.description ?? string.Empty,
                reply_effect_count = reply.Effects != null ? reply.Effects.Count : CoreConstants.ZeroBasedListStartIndex,
                reply_effect_summary = BuildDialogueActionSummary(reply.Effects),
                reply_effect_entries = BuildDialogueActionEntries(reply.Effects),
                actors_summary = snapshotBefore.ActorSummaryBefore,
                estimated_liability = snapshotBefore.EstimatedLiabilityBefore,
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                fans_before = snapshotBefore.FansBefore,
                fans_after = fansAfter,
                fans_delta = fansAfter - snapshotBefore.FansBefore,
                fame_before = snapshotBefore.FameBefore,
                fame_after = fameAfter,
                fame_delta = fameAfter - snapshotBefore.FameBefore,
                buzz_before = snapshotBefore.BuzzBefore,
                buzz_after = buzzAfter,
                buzz_delta = buzzAfter - snapshotBefore.BuzzBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRandomEvent,
                    payload.random_event_id,
                    CoreConstants.EventTypeRandomEventConcluded,
                    CoreConstants.EventSourceEventManagerConcludeEventPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation queue state for one substory start request.
        /// </summary>
        internal SubstoryStartSnapshot CreateSubstoryStartSnapshot(data_dialogues._dialogue dialogue)
        {
            string dialogueId = dialogue != null ? (dialogue.id ?? string.Empty) : string.Empty;
            return new SubstoryStartSnapshot
            {
                QueueCountBefore = Substories_Manager.dialogueQueue != null ? Substories_Manager.dialogueQueue.Count : CoreConstants.ZeroBasedListStartIndex,
                DelayedCountBefore = Substories_Manager.Delayed_Queue != null ? Substories_Manager.Delayed_Queue.Count : CoreConstants.ZeroBasedListStartIndex,
                WasUsedBefore = !string.IsNullOrEmpty(dialogueId) && Substories_Manager.IsUsed(dialogueId),
                WasDelayedBefore = !string.IsNullOrEmpty(dialogueId) && Substories_Manager.Delayed_Queue != null && Substories_Manager.Delayed_Queue.Contains(dialogueId),
                WasQueuedBefore = !string.IsNullOrEmpty(dialogueId) && QueueContainsSubstoryId(dialogueId)
            };
        }

        /// <summary>
        /// Captures substory queue transitions as started or delayed lifecycle events.
        /// </summary>
        internal void CaptureSubstoryStartOrDelay(
            data_dialogues._dialogue dialogue,
            DateTime launchTime,
            bool debug,
            Action beforeStart,
            SubstoryStartSnapshot snapshotBefore)
        {
            if (dialogue == null || string.IsNullOrEmpty(dialogue.id))
            {
                return;
            }

            int queueCountAfter = Substories_Manager.dialogueQueue != null ? Substories_Manager.dialogueQueue.Count : CoreConstants.ZeroBasedListStartIndex;
            int delayedCountAfter = Substories_Manager.Delayed_Queue != null ? Substories_Manager.Delayed_Queue.Count : CoreConstants.ZeroBasedListStartIndex;
            bool usedAfter = Substories_Manager.IsUsed(dialogue.id);
            bool delayedAfter = Substories_Manager.Delayed_Queue != null && Substories_Manager.Delayed_Queue.Contains(dialogue.id);
            bool queuedAfter = QueueContainsSubstoryId(dialogue.id);

            bool started = queuedAfter && (snapshotBefore == null || queueCountAfter > snapshotBefore.QueueCountBefore || !snapshotBefore.WasQueuedBefore);
            bool delayed = delayedAfter && (snapshotBefore == null || delayedCountAfter > snapshotBefore.DelayedCountBefore || !snapshotBefore.WasDelayedBefore);
            if (!started && !delayed)
            {
                return;
            }

            SubstoryLifecycleEventPayload payload = new SubstoryLifecycleEventPayload
            {
                substory_id = dialogue.id ?? string.Empty,
                substory_parent_id = dialogue.parent ?? string.Empty,
                substory_type = ResolveSubstoryTypeCode(dialogue),
                debug_mode = debug,
                had_before_start_callback = beforeStart != null,
                used_before = snapshotBefore != null && snapshotBefore.WasUsedBefore,
                used_after = usedAfter,
                queue_count_before = snapshotBefore != null ? snapshotBefore.QueueCountBefore : CoreConstants.ZeroBasedListStartIndex,
                queue_count_after = queueCountAfter,
                delayed_queue_count_before = snapshotBefore != null ? snapshotBefore.DelayedCountBefore : CoreConstants.ZeroBasedListStartIndex,
                delayed_queue_count_after = delayedCountAfter,
                scheduled_launch_time = CoreDateTimeUtility.ToRoundTripString(launchTime),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (started)
                {
                    payload.substory_lifecycle_action = CoreConstants.SubstoryLifecycleActionStarted;
                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSubstory,
                        payload.substory_id,
                        CoreConstants.EventTypeSubstoryStarted,
                        CoreConstants.EventSourceSubstoriesStartDialoguePatch,
                        CoreJsonUtility.SerializeObjectPayload(payload));

                    if (dialogue.type == data_dialogues._dialogue._type.dialogue)
                    {
                        TrackSubstoryQueuedLocked(payload.substory_id);
                    }
                }

                if (delayed)
                {
                    payload.substory_lifecycle_action = CoreConstants.SubstoryLifecycleActionDelayed;
                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindSubstory,
                        payload.substory_id,
                        CoreConstants.EventTypeSubstoryDelayed,
                        CoreConstants.EventSourceSubstoriesStartDialoguePatch,
                        CoreJsonUtility.SerializeObjectPayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures current active-dialogue state for deferred substory completion.
        /// </summary>
        internal SubstoryCompletionSnapshot CreateSubstoryCompletionSnapshot(ActiveDialogueController dialogueController)
        {
            SubstoryCompletionSnapshot snapshot = new SubstoryCompletionSnapshot();
            if (dialogueController == null || dialogueController.dialogue == null)
            {
                return snapshot;
            }

            data_dialogues._dialogue dialogue = dialogueController.dialogue;
            snapshot.DialogueId = dialogue.id ?? string.Empty;
            snapshot.ParentDialogueId = dialogue.parent ?? string.Empty;
            snapshot.DialogueTypeCode = ResolveSubstoryTypeCode(dialogue);
            if (string.IsNullOrEmpty(snapshot.DialogueId))
            {
                return snapshot;
            }

            lock (runtimeLock)
            {
                int pendingCount;
                snapshot.ShouldEmit =
                    pendingSubstoryCompletionCountByDialogueId.TryGetValue(snapshot.DialogueId, out pendingCount)
                    && pendingCount > CoreConstants.ZeroBasedListStartIndex;
            }

            return snapshot;
        }

        /// <summary>
        /// Captures substory completion once active dialogue is closed.
        /// </summary>
        internal void CaptureSubstoryCompleted(SubstoryCompletionSnapshot snapshotBefore)
        {
            if (snapshotBefore == null || !snapshotBefore.ShouldEmit || string.IsNullOrEmpty(snapshotBefore.DialogueId))
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (!TryConsumeSubstoryCompletionLocked(snapshotBefore.DialogueId))
                {
                    return;
                }

                SubstoryLifecycleEventPayload payload = new SubstoryLifecycleEventPayload
                {
                    substory_id = snapshotBefore.DialogueId,
                    substory_parent_id = snapshotBefore.ParentDialogueId,
                    substory_type = snapshotBefore.DialogueTypeCode,
                    substory_lifecycle_action = CoreConstants.SubstoryLifecycleActionCompleted,
                    used_before = Substories_Manager.IsUsed(snapshotBefore.DialogueId),
                    used_after = Substories_Manager.IsUsed(snapshotBefore.DialogueId),
                    queue_count_before = Substories_Manager.dialogueQueue != null ? Substories_Manager.dialogueQueue.Count : CoreConstants.ZeroBasedListStartIndex,
                    queue_count_after = Substories_Manager.dialogueQueue != null ? Substories_Manager.dialogueQueue.Count : CoreConstants.ZeroBasedListStartIndex,
                    delayed_queue_count_before = Substories_Manager.Delayed_Queue != null ? Substories_Manager.Delayed_Queue.Count : CoreConstants.ZeroBasedListStartIndex,
                    delayed_queue_count_after = Substories_Manager.Delayed_Queue != null ? Substories_Manager.Delayed_Queue.Count : CoreConstants.ZeroBasedListStartIndex,
                    scheduled_launch_time = string.Empty,
                    event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                };

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindSubstory,
                    payload.substory_id,
                    CoreConstants.EventTypeSubstoryCompleted,
                    CoreConstants.EventSourceSubstoriesStartDialoguePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for passive economy ticks.
        /// </summary>
        internal EconomyTickSnapshot CreateEconomyTickSnapshot()
        {
            return new EconomyTickSnapshot
            {
                MoneyBefore = resources.Money(),
                FansBefore = resources.GetFansTotal(null),
                FameBefore = (int)resources.Get(resources.type.fame, true),
                BuzzBefore = (int)resources.Get(resources.type.buzz, true),
                FameLevelBefore = resources.GetFameLevel(),
                FameProgressBefore = resources.GetFameProgress()
            };
        }

        /// <summary>
        /// Captures weekly passive-expense application.
        /// </summary>
        internal void CaptureEconomyWeeklyExpense(resources resourceSystem, EconomyTickSnapshot snapshotBefore)
        {
            if (resourceSystem == null || snapshotBefore == null)
            {
                return;
            }

            long moneyAfter = resources.Money();
            EconomyWeeklyExpenseEventPayload payload = new EconomyWeeklyExpenseEventPayload
            {
                weekly_expense = resourceSystem.Money_WeeklyExpenses(),
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                fans_total = resources.GetFansTotal(null),
                fame_points = (int)resources.Get(resources.type.fame, true),
                buzz_points = (int)resources.Get(resources.type.buzz, true),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindEconomy,
                    "weekly",
                    CoreConstants.EventTypeEconomyWeeklyExpenseApplied,
                    CoreConstants.EventSourceResourcesOnNewWeekPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures daily passive economy deltas.
        /// </summary>
        internal void CaptureEconomyDailyTick(resources resourceSystem, EconomyTickSnapshot snapshotBefore)
        {
            if (resourceSystem == null || snapshotBefore == null)
            {
                return;
            }

            long moneyAfter = resources.Money();
            long fansAfter = resources.GetFansTotal(null);
            int fameAfter = (int)resources.Get(resources.type.fame, true);
            int buzzAfter = (int)resources.Get(resources.type.buzz, true);

            EconomyDailyTickEventPayload payload = new EconomyDailyTickEventPayload
            {
                money_before = snapshotBefore.MoneyBefore,
                money_after = moneyAfter,
                money_delta = moneyAfter - snapshotBefore.MoneyBefore,
                expected_daily_profit = resourceSystem.Money_DailyProfit(),
                buzz_before = snapshotBefore.BuzzBefore,
                buzz_after = buzzAfter,
                buzz_delta = buzzAfter - snapshotBefore.BuzzBefore,
                expected_daily_buzz_gain = resourceSystem.Buzz_Daily() - resourceSystem.GetDailyBuzzReduction(),
                fame_before = snapshotBefore.FameBefore,
                fame_after = fameAfter,
                fame_delta = fameAfter - snapshotBefore.FameBefore,
                expected_daily_fame_gain = resourceSystem.Fame_Daily(),
                fans_before = snapshotBefore.FansBefore,
                fans_after = fansAfter,
                fans_delta = fansAfter - snapshotBefore.FansBefore,
                fans_change_counter = resources.FansChange,
                fame_level_before = snapshotBefore.FameLevelBefore,
                fame_level_after = resources.GetFameLevel(),
                fame_progress_before = snapshotBefore.FameProgressBefore,
                fame_progress_after = resources.GetFameProgress(),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindEconomy,
                    "daily",
                    CoreConstants.EventTypeEconomyDailyTick,
                    CoreConstants.EventSourceResourcesOnNewDayPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures blackmail queue additions.
        /// </summary>
        internal void CaptureInfluenceBlackmailQueued(data_girls.girls spy, data_girls.girls target)
        {
            if (spy == null || target == null || spy.id < CoreConstants.MinimumValidIdolIdentifier || target.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            DateTime reportDate = staticVars.dateTime.AddDays(7.0);
            InfluenceBlackmailEventPayload payload = new InfluenceBlackmailEventPayload
            {
                spy_id = spy.id,
                target_id = target.id,
                influence_action = CoreConstants.InfluenceLifecycleActionBlackmailQueued,
                report_date = CoreDateTimeUtility.ToRoundTripString(reportDate),
                days_until_report = 7,
                queue_size_after = Date_Influence.Blackmail != null ? Date_Influence.Blackmail.Count : CoreConstants.ZeroBasedListStartIndex,
                success_tier = CoreConstants.InvalidIdValue,
                influence_award = CoreConstants.ZeroBasedListStartIndex,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    spy.id,
                    CoreConstants.EventEntityKindInfluence,
                    spy.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeInfluenceBlackmailQueued,
                    CoreConstants.EventSourceDateInfluenceAddBlackmailPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures blackmail trigger outcomes.
        /// </summary>
        internal void CaptureInfluenceBlackmailTriggered(Date_Influence._blackmail blackmail, int successTier)
        {
            if (blackmail == null || blackmail.Spy == null || blackmail.Target == null)
            {
                return;
            }

            if (blackmail.Spy.id < CoreConstants.MinimumValidIdolIdentifier || blackmail.Target.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            int influenceAward = CoreConstants.ZeroBasedListStartIndex;
            if (successTier == 1)
            {
                influenceAward = 2;
            }
            else if (successTier >= 2)
            {
                influenceAward = 5;
            }

            InfluenceBlackmailEventPayload payload = new InfluenceBlackmailEventPayload
            {
                spy_id = blackmail.Spy.id,
                target_id = blackmail.Target.id,
                influence_action = CoreConstants.InfluenceLifecycleActionBlackmailTriggered,
                report_date = ResolveDateString(blackmail.ReportDate),
                days_until_report = (int)(blackmail.ReportDate - staticVars.dateTime).TotalDays,
                queue_size_after = Date_Influence.Blackmail != null ? Date_Influence.Blackmail.Count : CoreConstants.ZeroBasedListStartIndex,
                success_tier = successTier,
                influence_award = influenceAward,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    blackmail.Spy.id,
                    CoreConstants.EventEntityKindInfluence,
                    blackmail.Spy.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeInfluenceBlackmailTriggered,
                    CoreConstants.EventSourceDateInfluenceBlackmailTriggerPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures mentorship-start lifecycle events.
        /// </summary>
        internal void CaptureMentorshipStarted(data_girls.girls mentor, data_girls.girls kohai)
        {
            if (mentor == null || kohai == null || mentor.id < CoreConstants.MinimumValidIdolIdentifier || kohai.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            MentorshipLifecycleEventPayload payload = new MentorshipLifecycleEventPayload
            {
                mentorship_action = CoreConstants.MentorshipLifecycleActionStarted,
                mentor_id = mentor.id,
                kohai_id = kohai.id,
                active_mentor_count = Girls_Mentors.Mentors != null ? Girls_Mentors.Mentors.Count : CoreConstants.ZeroBasedListStartIndex,
                senpai_to_kohai_ratio_before = ResolveRelationshipRatio(mentor, kohai),
                senpai_to_kohai_ratio_after = ResolveRelationshipRatio(mentor, kohai),
                kohai_to_senpai_ratio_before = ResolveRelationshipRatio(kohai, mentor),
                kohai_to_senpai_ratio_after = ResolveRelationshipRatio(kohai, mentor),
                mentor_pairs_summary = BuildMentorPairsSummary(Girls_Mentors.Mentors),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string mentorshipId = BuildMentorshipIdentifier(payload.mentor_id, payload.kohai_id);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    payload.mentor_id,
                    CoreConstants.EventEntityKindMentorship,
                    mentorshipId,
                    CoreConstants.EventTypeMentorshipStarted,
                    CoreConstants.EventSourceGirlsMentorsAddKohaiPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for mentorship removal.
        /// </summary>
        internal MentorshipRemoveSnapshot CreateMentorshipRemoveSnapshot(data_girls.girls mentor, data_girls.girls kohai)
        {
            MentorshipRemoveSnapshot snapshot = new MentorshipRemoveSnapshot();
            if (Girls_Mentors.Mentors == null)
            {
                return snapshot;
            }

            for (int mentorIndex = CoreConstants.ZeroBasedListStartIndex; mentorIndex < Girls_Mentors.Mentors.Count; mentorIndex++)
            {
                Girls_Mentors._mentor mentorRow = Girls_Mentors.Mentors[mentorIndex];
                if (mentorRow == null || mentorRow.Senpai == null || mentorRow.Kohai == null)
                {
                    continue;
                }

                bool matchesMentor = mentor != null && ReferenceEquals(mentorRow.Senpai, mentor);
                bool matchesKohai = kohai != null && ReferenceEquals(mentorRow.Kohai, kohai);
                if (!matchesMentor && !matchesKohai)
                {
                    continue;
                }

                snapshot.RemovedPairs.Add(CreateMentorshipPairSnapshot(mentorRow));
            }

            return snapshot;
        }

        /// <summary>
        /// Captures mentorship-end lifecycle events for each removed pair.
        /// </summary>
        internal void CaptureMentorshipEnded(MentorshipRemoveSnapshot snapshotBefore)
        {
            if (snapshotBefore == null || snapshotBefore.RemovedPairs == null || snapshotBefore.RemovedPairs.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string pairsSummary = BuildMentorPairsSummary(Girls_Mentors.Mentors);
                int activeMentorCount = Girls_Mentors.Mentors != null ? Girls_Mentors.Mentors.Count : CoreConstants.ZeroBasedListStartIndex;
                for (int pairIndex = CoreConstants.ZeroBasedListStartIndex; pairIndex < snapshotBefore.RemovedPairs.Count; pairIndex++)
                {
                    MentorshipPairSnapshot pair = snapshotBefore.RemovedPairs[pairIndex];
                    if (pair == null || pair.MentorId < CoreConstants.MinimumValidIdolIdentifier || pair.KohaiId < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        continue;
                    }

                    data_girls.girls mentorAfter = data_girls.GetGirlByID(pair.MentorId);
                    data_girls.girls kohaiAfter = data_girls.GetGirlByID(pair.KohaiId);

                    MentorshipLifecycleEventPayload payload = new MentorshipLifecycleEventPayload
                    {
                        mentorship_action = CoreConstants.MentorshipLifecycleActionEnded,
                        mentor_id = pair.MentorId,
                        kohai_id = pair.KohaiId,
                        active_mentor_count = activeMentorCount,
                        senpai_to_kohai_ratio_before = pair.SenpaiToKohaiRatioBefore,
                        senpai_to_kohai_ratio_after = ResolveRelationshipRatio(mentorAfter, kohaiAfter),
                        kohai_to_senpai_ratio_before = pair.KohaiToSenpaiRatioBefore,
                        kohai_to_senpai_ratio_after = ResolveRelationshipRatio(kohaiAfter, mentorAfter),
                        mentor_pairs_summary = pairsSummary,
                        event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                    };

                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        payload.mentor_id,
                        CoreConstants.EventEntityKindMentorship,
                        BuildMentorshipIdentifier(payload.mentor_id, payload.kohai_id),
                        CoreConstants.EventTypeMentorshipEnded,
                        CoreConstants.EventSourceGirlsMentorsRemoveKohaiPatch,
                        CoreJsonUtility.SerializeObjectPayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation mentorship weekly tick snapshot.
        /// </summary>
        internal MentorshipWeeklySnapshot CreateMentorshipWeeklySnapshot()
        {
            MentorshipWeeklySnapshot snapshot = new MentorshipWeeklySnapshot();
            if (Girls_Mentors.Mentors == null)
            {
                return snapshot;
            }

            for (int mentorIndex = CoreConstants.ZeroBasedListStartIndex; mentorIndex < Girls_Mentors.Mentors.Count; mentorIndex++)
            {
                Girls_Mentors._mentor mentor = Girls_Mentors.Mentors[mentorIndex];
                if (mentor == null || mentor.Senpai == null || mentor.Kohai == null)
                {
                    continue;
                }

                snapshot.PairSnapshots.Add(CreateMentorshipPairSnapshot(mentor));
            }

            return snapshot;
        }

        /// <summary>
        /// Captures mentor weekly relationship tick deltas.
        /// </summary>
        internal void CaptureMentorshipWeeklyTick(MentorshipWeeklySnapshot snapshotBefore)
        {
            if (snapshotBefore == null || snapshotBefore.PairSnapshots == null || snapshotBefore.PairSnapshots.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string pairsSummary = BuildMentorPairsSummary(Girls_Mentors.Mentors);
                int activeMentorCount = Girls_Mentors.Mentors != null ? Girls_Mentors.Mentors.Count : CoreConstants.ZeroBasedListStartIndex;
                for (int pairIndex = CoreConstants.ZeroBasedListStartIndex; pairIndex < snapshotBefore.PairSnapshots.Count; pairIndex++)
                {
                    MentorshipPairSnapshot pair = snapshotBefore.PairSnapshots[pairIndex];
                    if (pair == null || pair.MentorId < CoreConstants.MinimumValidIdolIdentifier || pair.KohaiId < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        continue;
                    }

                    data_girls.girls mentorAfter = data_girls.GetGirlByID(pair.MentorId);
                    data_girls.girls kohaiAfter = data_girls.GetGirlByID(pair.KohaiId);
                    MentorshipLifecycleEventPayload payload = new MentorshipLifecycleEventPayload
                    {
                        mentorship_action = CoreConstants.MentorshipLifecycleActionWeeklyTick,
                        mentor_id = pair.MentorId,
                        kohai_id = pair.KohaiId,
                        active_mentor_count = activeMentorCount,
                        senpai_to_kohai_ratio_before = pair.SenpaiToKohaiRatioBefore,
                        senpai_to_kohai_ratio_after = ResolveRelationshipRatio(mentorAfter, kohaiAfter),
                        kohai_to_senpai_ratio_before = pair.KohaiToSenpaiRatioBefore,
                        kohai_to_senpai_ratio_after = ResolveRelationshipRatio(kohaiAfter, mentorAfter),
                        mentor_pairs_summary = pairsSummary,
                        event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                    };

                    EnqueueEventRecordLocked(
                        staticVars.dateTime,
                        payload.mentor_id,
                        CoreConstants.EventEntityKindMentorship,
                        BuildMentorshipIdentifier(payload.mentor_id, payload.kohai_id),
                        CoreConstants.EventTypeMentorshipWeeklyTick,
                        CoreConstants.EventSourceGirlsMentorsOnNewWeekPatch,
                        CoreJsonUtility.SerializeObjectPayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot for rival-market updates.
        /// </summary>
        internal RivalMarketSnapshot CreateRivalMarketSnapshot()
        {
            return new RivalMarketSnapshot
            {
                MonthIndexBefore = Rivals.Date_To_Month != null ? Rivals.Date_To_Month.Count : CoreConstants.ZeroBasedListStartIndex,
                ActiveGroupCountBefore = CountAliveRivalGroups(Rivals.Groups),
                RisingGroupCountBefore = CountRisingRivalGroups(Rivals.Groups),
                DeadGroupCountBefore = CountDeadRivalGroups(Rivals.Groups),
                OfficeResearchPointsBefore = Research.GetCategory(Research.type.office).GetPoints(),
                TrendLastUpdatedBefore = Rivals.Trend_Data != null ? (Rivals.Trend_Data.LastUpdated ?? string.Empty) : string.Empty,
                TrendsGenreSummaryBefore = BuildRivalTrendDataSummary(Rivals.Trend_Data != null ? Rivals.Trend_Data.Genres : null),
                TrendsLyricsSummaryBefore = BuildRivalTrendDataSummary(Rivals.Trend_Data != null ? Rivals.Trend_Data.Lyrics : null),
                TrendsChoreoSummaryBefore = BuildRivalTrendDataSummary(Rivals.Trend_Data != null ? Rivals.Trend_Data.Choreo : null)
            };
        }

        /// <summary>
        /// Captures rival trend update snapshots with research spend.
        /// </summary>
        internal void CaptureRivalTrendsUpdated(RivalMarketSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            long officePointsAfter = Research.GetCategory(Research.type.office).GetPoints();
            RivalMarketEventPayload payload = new RivalMarketEventPayload
            {
                rival_action = CoreConstants.RivalLifecycleActionTrendsUpdated,
                month_index_before = snapshotBefore.MonthIndexBefore,
                month_index_after = Rivals.Date_To_Month != null ? Rivals.Date_To_Month.Count : CoreConstants.ZeroBasedListStartIndex,
                active_group_count_before = snapshotBefore.ActiveGroupCountBefore,
                active_group_count_after = CountAliveRivalGroups(Rivals.Groups),
                rising_group_count_before = snapshotBefore.RisingGroupCountBefore,
                rising_group_count_after = CountRisingRivalGroups(Rivals.Groups),
                dead_group_count_before = snapshotBefore.DeadGroupCountBefore,
                dead_group_count_after = CountDeadRivalGroups(Rivals.Groups),
                office_research_points_before = snapshotBefore.OfficeResearchPointsBefore,
                office_research_points_after = officePointsAfter,
                office_research_points_delta = officePointsAfter - snapshotBefore.OfficeResearchPointsBefore,
                trend_update_cost = Rivals.Trend_Update_Cost,
                trends_genre_summary = BuildRivalTrendDataSummary(Rivals.Trend_Data != null ? Rivals.Trend_Data.Genres : null),
                trends_lyrics_summary = BuildRivalTrendDataSummary(Rivals.Trend_Data != null ? Rivals.Trend_Data.Lyrics : null),
                trends_choreo_summary = BuildRivalTrendDataSummary(Rivals.Trend_Data != null ? Rivals.Trend_Data.Choreo : null),
                trend_last_updated_before = snapshotBefore.TrendLastUpdatedBefore,
                trend_last_updated_after = Rivals.Trend_Data != null ? (Rivals.Trend_Data.LastUpdated ?? string.Empty) : string.Empty,
                show_popup = false,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRivalMarket,
                    "trends",
                    CoreConstants.EventTypeRivalTrendsUpdated,
                    CoreConstants.EventSourceRivalsUpdateTrendsPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures monthly rival-market recalculation context.
        /// </summary>
        internal void CaptureRivalMonthlyRecalculated(bool showPopup, RivalMarketSnapshot snapshotBefore)
        {
            if (snapshotBefore == null)
            {
                return;
            }

            long officePointsAfter = Research.GetCategory(Research.type.office).GetPoints();
            RivalMarketEventPayload payload = new RivalMarketEventPayload
            {
                rival_action = CoreConstants.RivalLifecycleActionMonthlyRecalculated,
                month_index_before = snapshotBefore.MonthIndexBefore,
                month_index_after = Rivals.Date_To_Month != null ? Rivals.Date_To_Month.Count : CoreConstants.ZeroBasedListStartIndex,
                active_group_count_before = snapshotBefore.ActiveGroupCountBefore,
                active_group_count_after = CountAliveRivalGroups(Rivals.Groups),
                rising_group_count_before = snapshotBefore.RisingGroupCountBefore,
                rising_group_count_after = CountRisingRivalGroups(Rivals.Groups),
                dead_group_count_before = snapshotBefore.DeadGroupCountBefore,
                dead_group_count_after = CountDeadRivalGroups(Rivals.Groups),
                office_research_points_before = snapshotBefore.OfficeResearchPointsBefore,
                office_research_points_after = officePointsAfter,
                office_research_points_delta = officePointsAfter - snapshotBefore.OfficeResearchPointsBefore,
                trend_update_cost = Rivals.Trend_Update_Cost,
                trends_genre_summary = BuildRivalTrendRuntimeSummary(Rivals.Genres),
                trends_lyrics_summary = BuildRivalTrendRuntimeSummary(Rivals.Lyrics),
                trends_choreo_summary = BuildRivalTrendRuntimeSummary(Rivals.Choreography),
                trend_last_updated_before = snapshotBefore.TrendLastUpdatedBefore,
                trend_last_updated_after = Rivals.Trend_Data != null ? (Rivals.Trend_Data.LastUpdated ?? string.Empty) : string.Empty,
                show_popup = showPopup,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRivalMarket,
                    "monthly",
                    CoreConstants.EventTypeRivalMonthlyRecalculated,
                    CoreConstants.EventSourceRivalsOnNewMonthPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation snapshot before summer games finalization spend.
        /// </summary>
        internal SummerGamesFinalizeSnapshot CreateSummerGamesFinalizeSnapshot(Summer_Games._data summerGamesData)
        {
            SummerGamesFinalizeSnapshot snapshot = new SummerGamesFinalizeSnapshot
            {
                VocalPointsBefore = Research.GetCategory(Research.type.vocal).GetPoints(),
                PlayerPointsBefore = Research.GetCategory(Research.type.player).GetPoints(),
                DancePointsBefore = Research.GetCategory(Research.type.dance).GetPoints()
            };

            if (summerGamesData == null)
            {
                return snapshot;
            }

            snapshot.SelectedSingleIdBefore = summerGamesData.Selected_Single;
            snapshot.GenreIdBefore = summerGamesData.Genre;
            snapshot.LyricsIdBefore = summerGamesData.Lyrics;
            snapshot.ChoreographyIdBefore = summerGamesData.Choreo;
            snapshot.GenreCostBefore = summerGamesData.GetCost(singles._param._type.genre);
            snapshot.LyricsCostBefore = summerGamesData.GetCost(singles._param._type.lyrics);
            snapshot.ChoreographyCostBefore = summerGamesData.GetCost(singles._param._type.choreography);
            snapshot.TotalCostBefore = summerGamesData.GetTotalCost();
            snapshot.WasFinalizedBefore = summerGamesData.IsFinalized;
            return snapshot;
        }

        /// <summary>
        /// Captures summer games finalization spend and resulting state.
        /// </summary>
        internal void CaptureSummerGamesFinalized(Summer_Games._data summerGamesData, SummerGamesFinalizeSnapshot snapshotBefore)
        {
            if (summerGamesData == null || snapshotBefore == null)
            {
                return;
            }

            if (snapshotBefore.WasFinalizedBefore || !summerGamesData.IsFinalized)
            {
                return;
            }

            long vocalAfter = Research.GetCategory(Research.type.vocal).GetPoints();
            long playerAfter = Research.GetCategory(Research.type.player).GetPoints();
            long danceAfter = Research.GetCategory(Research.type.dance).GetPoints();

            int happyMemberCount = CoreConstants.ZeroBasedListStartIndex;
            if (summerGamesData.Members != null)
            {
                for (int memberIndex = CoreConstants.ZeroBasedListStartIndex; memberIndex < summerGamesData.Members.Count; memberIndex++)
                {
                    if (summerGamesData.IsMemberHappy(memberIndex))
                    {
                        happyMemberCount++;
                    }
                }
            }

            SummerGamesFinalizedEventPayload payload = new SummerGamesFinalizedEventPayload
            {
                selected_single_id = summerGamesData.Selected_Single,
                genre_id = summerGamesData.Genre,
                lyrics_id = summerGamesData.Lyrics,
                choreography_id = summerGamesData.Choreo,
                genre_cost = snapshotBefore.GenreCostBefore,
                lyrics_cost = snapshotBefore.LyricsCostBefore,
                choreography_cost = snapshotBefore.ChoreographyCostBefore,
                total_cost = snapshotBefore.TotalCostBefore,
                was_finalized_before = snapshotBefore.WasFinalizedBefore,
                is_finalized_after = summerGamesData.IsFinalized,
                vocal_points_before = snapshotBefore.VocalPointsBefore,
                vocal_points_after = vocalAfter,
                vocal_points_delta = vocalAfter - snapshotBefore.VocalPointsBefore,
                player_points_before = snapshotBefore.PlayerPointsBefore,
                player_points_after = playerAfter,
                player_points_delta = playerAfter - snapshotBefore.PlayerPointsBefore,
                dance_points_before = snapshotBefore.DancePointsBefore,
                dance_points_after = danceAfter,
                dance_points_delta = danceAfter - snapshotBefore.DancePointsBefore,
                happy_member_count = happyMemberCount,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindSummerGames,
                    payload.selected_single_id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeSummerGamesFinalized,
                    CoreConstants.EventSourceSummerGamesOnProceedPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Formats a game DateTime with empty output for uninitialized sentinels.
        /// </summary>
        private static string ResolveDateString(DateTime value)
        {
            if (value == default(DateTime) || value.Year <= 1901)
            {
                return string.Empty;
            }

            return CoreDateTimeUtility.ToRoundTripString(value);
        }

        /// <summary>
        /// Resolves one normalized audition type code.
        /// </summary>
        private static string ResolveAuditionTypeCode(Auditions.type auditionType)
        {
            return auditionType.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Resolves one newly built agency room by id-diff against a pre-build snapshot.
        /// </summary>
        private static agency._room FindBuiltAgencyRoom(AgencyRoomBuildSnapshot snapshotBefore)
        {
            List<agency._room> rooms = agency.GetRooms();
            if (rooms == null || rooms.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return null;
            }

            agency._room newestRoom = null;
            int newestRoomId = CoreConstants.InvalidIdValue;
            for (int roomIndex = CoreConstants.ZeroBasedListStartIndex; roomIndex < rooms.Count; roomIndex++)
            {
                agency._room room = rooms[roomIndex];
                if (room == null || room.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (snapshotBefore != null && snapshotBefore.ExistingRoomIds != null && snapshotBefore.ExistingRoomIds.Contains(room.id))
                {
                    continue;
                }

                if (room.id > newestRoomId)
                {
                    newestRoomId = room.id;
                    newestRoom = room;
                }
            }

            return newestRoom;
        }

        /// <summary>
        /// Resolves floor location metadata for one agency room.
        /// </summary>
        private static bool TryResolveAgencyRoomLocation(agency agencySystem, agency._room room, out int floorId, out int floorIndex)
        {
            floorId = CoreConstants.InvalidIdValue;
            floorIndex = CoreConstants.InvalidIdValue;
            if (room == null)
            {
                return false;
            }

            agency resolvedAgency = agencySystem;
            if (resolvedAgency == null && Camera.main != null)
            {
                resolvedAgency = Camera.main.GetComponent<mainScript>().Data.GetComponent<agency>();
            }

            if (resolvedAgency == null || resolvedAgency.floors == null)
            {
                return false;
            }

            for (int currentFloorIndex = CoreConstants.ZeroBasedListStartIndex; currentFloorIndex < resolvedAgency.floors.Count; currentFloorIndex++)
            {
                agency._floor floor = resolvedAgency.floors[currentFloorIndex];
                if (floor == null || floor.floor == null)
                {
                    continue;
                }

                if (!floor.floor.Contains(room))
                {
                    continue;
                }

                floorId = floor.FloorID;
                floorIndex = currentFloorIndex;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves matching random event instance from active-event queue.
        /// </summary>
        private static Event_Manager._activeEvent FindMatchingActiveRandomEvent(Event_Manager manager, Event_Manager._randomEvent randomEvent, DateTime scheduledDate)
        {
            if (manager == null || randomEvent == null || manager.activeEvents == null)
            {
                return null;
            }

            Event_Manager._activeEvent fallbackMatch = null;
            for (int eventIndex = manager.activeEvents.Count - CoreConstants.LastElementOffsetFromCount; eventIndex >= CoreConstants.ZeroBasedListStartIndex; eventIndex--)
            {
                Event_Manager._activeEvent activeEvent = manager.activeEvents[eventIndex];
                if (activeEvent == null || activeEvent.data == null)
                {
                    continue;
                }

                if (activeEvent.state == Event_Manager._activeEvent._state.complete)
                {
                    continue;
                }

                if (ReferenceEquals(activeEvent.data, randomEvent) && activeEvent.date == scheduledDate)
                {
                    return activeEvent;
                }

                if (ReferenceEquals(activeEvent.data, randomEvent) && fallbackMatch == null)
                {
                    fallbackMatch = activeEvent;
                }
            }

            return fallbackMatch;
        }

        /// <summary>
        /// Resolves one normalized random-event state code.
        /// </summary>
        private static string ResolveRandomEventStateCode(Event_Manager._activeEvent._state state)
        {
            return state.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Resolves reply index from random-event option list by reference match.
        /// </summary>
        private static int ResolveRandomEventReplyIndex(Event_Manager._randomEvent randomEvent, Event_Manager._randomEvent._reply reply)
        {
            if (randomEvent == null || reply == null || randomEvent.reply == null)
            {
                return CoreConstants.InvalidIdValue;
            }

            for (int replyIndex = CoreConstants.ZeroBasedListStartIndex; replyIndex < randomEvent.reply.Count; replyIndex++)
            {
                if (ReferenceEquals(randomEvent.reply[replyIndex], reply))
                {
                    return replyIndex;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Builds compact actor summary for one active random event.
        /// </summary>
        private static string BuildRandomEventActorSummary(IReadOnlyList<Event_Manager._activeEvent._actor> actors)
        {
            if (actors == null || actors.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(actors.Count * 24);
            for (int actorIndex = CoreConstants.ZeroBasedListStartIndex; actorIndex < actors.Count; actorIndex++)
            {
                Event_Manager._activeEvent._actor actor = actors[actorIndex];
                if (actor == null)
                {
                    continue;
                }

                if (summaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                string actorKind = "unknown";
                int actorId = CoreConstants.InvalidIdValue;
                if (actor.girl != null && actor.girl.id >= CoreConstants.MinimumValidIdolIdentifier)
                {
                    actorKind = CoreConstants.EventEntityKindIdol;
                    actorId = actor.girl.id;
                }
                else if (actor.staff != null && actor.staff.id >= CoreConstants.MinimumValidIdolIdentifier)
                {
                    actorKind = CoreConstants.EventEntityKindStaff;
                    actorId = actor.staff.id;
                }

                summaryBuilder.Append(actorKind);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(actorId.ToString(CultureInfo.InvariantCulture));
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(actor.data != null ? actor.data.name ?? string.Empty : string.Empty);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(actor.GetName(true));
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Builds compact effect summary for dialogue action lists.
        /// </summary>
        private static string BuildDialogueActionSummary(IReadOnlyList<data_dialogues._action> actions)
        {
            if (actions == null || actions.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(actions.Count * 32);
            for (int actionIndex = CoreConstants.ZeroBasedListStartIndex; actionIndex < actions.Count; actionIndex++)
            {
                data_dialogues._action action = actions[actionIndex];
                if (action == null)
                {
                    continue;
                }

                if (summaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                summaryBuilder.Append(action.target ?? string.Empty);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(action.parameter ?? string.Empty);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(action.formula ?? string.Empty);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(action.special ?? string.Empty);
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Builds structured effect entries for one random-event reply.
        /// </summary>
        private static RandomEventReplyEffectEntry[] BuildDialogueActionEntries(IReadOnlyList<data_dialogues._action> actions)
        {
            if (actions == null || actions.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return null;
            }

            List<RandomEventReplyEffectEntry> entries = new List<RandomEventReplyEffectEntry>(actions.Count);
            for (int actionIndex = CoreConstants.ZeroBasedListStartIndex; actionIndex < actions.Count; actionIndex++)
            {
                data_dialogues._action action = actions[actionIndex];
                if (action == null)
                {
                    continue;
                }

                entries.Add(new RandomEventReplyEffectEntry
                {
                    target = action.target ?? string.Empty,
                    parameter = action.parameter ?? string.Empty,
                    formula = action.formula ?? string.Empty,
                    special = action.special ?? string.Empty
                });
            }

            return entries.Count < CoreConstants.MinimumNonEmptyCollectionCount
                ? null
                : entries.ToArray();
        }

        /// <summary>
        /// Resolves one normalized substory dialogue type code.
        /// </summary>
        private static string ResolveSubstoryTypeCode(data_dialogues._dialogue dialogue)
        {
            if (dialogue == null)
            {
                return CoreConstants.StatusCodeUnknown;
            }

            return dialogue.type.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Returns true when queue currently contains one dialogue with matching id.
        /// </summary>
        private static bool QueueContainsSubstoryId(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId) || Substories_Manager.dialogueQueue == null)
            {
                return false;
            }

            for (int queueIndex = CoreConstants.ZeroBasedListStartIndex; queueIndex < Substories_Manager.dialogueQueue.Count; queueIndex++)
            {
                Substories_Manager._dialogueQueue queuedDialogue = Substories_Manager.dialogueQueue[queueIndex];
                if (queuedDialogue != null
                    && queuedDialogue.dialogue != null
                    && string.Equals(queuedDialogue.dialogue.id, dialogueId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tracks started substories for deferred completion emission.
        /// </summary>
        private void TrackSubstoryQueuedLocked(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                return;
            }

            int queuedCount;
            if (!pendingSubstoryCompletionCountByDialogueId.TryGetValue(dialogueId, out queuedCount))
            {
                queuedCount = CoreConstants.ZeroBasedListStartIndex;
            }

            pendingSubstoryCompletionCountByDialogueId[dialogueId] = queuedCount + 1;
        }

        /// <summary>
        /// Consumes one pending substory completion token.
        /// </summary>
        private bool TryConsumeSubstoryCompletionLocked(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                return false;
            }

            int queuedCount;
            if (!pendingSubstoryCompletionCountByDialogueId.TryGetValue(dialogueId, out queuedCount))
            {
                return false;
            }

            if (queuedCount <= 1)
            {
                pendingSubstoryCompletionCountByDialogueId.Remove(dialogueId);
            }
            else
            {
                pendingSubstoryCompletionCountByDialogueId[dialogueId] = queuedCount - 1;
            }

            return true;
        }

        /// <summary>
        /// Resolves directional relationship ratio for one idol pair.
        /// </summary>
        private static float ResolveRelationshipRatio(data_girls.girls source, data_girls.girls target)
        {
            if (source == null || target == null)
            {
                return 0f;
            }

            var relationship = source.GetRelationship(target);
            if (relationship == null)
            {
                return 0f;
            }

            return relationship.Ratio;
        }

        /// <summary>
        /// Creates one mentorship pair snapshot from live mentor row.
        /// </summary>
        private static MentorshipPairSnapshot CreateMentorshipPairSnapshot(Girls_Mentors._mentor mentor)
        {
            MentorshipPairSnapshot snapshot = new MentorshipPairSnapshot();
            if (mentor == null || mentor.Senpai == null || mentor.Kohai == null)
            {
                return snapshot;
            }

            snapshot.MentorId = mentor.Senpai.id;
            snapshot.KohaiId = mentor.Kohai.id;
            snapshot.SenpaiToKohaiRatioBefore = ResolveRelationshipRatio(mentor.Senpai, mentor.Kohai);
            snapshot.KohaiToSenpaiRatioBefore = ResolveRelationshipRatio(mentor.Kohai, mentor.Senpai);
            return snapshot;
        }

        /// <summary>
        /// Builds deterministic mentorship entity identifier.
        /// </summary>
        private static string BuildMentorshipIdentifier(int mentorId, int kohaiId)
        {
            return string.Concat(
                mentorId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                kohaiId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds compact mentor-pair summary from current mentor list.
        /// </summary>
        private static string BuildMentorPairsSummary(IReadOnlyList<Girls_Mentors._mentor> mentors)
        {
            if (mentors == null || mentors.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(mentors.Count * 16);
            for (int mentorIndex = CoreConstants.ZeroBasedListStartIndex; mentorIndex < mentors.Count; mentorIndex++)
            {
                Girls_Mentors._mentor mentor = mentors[mentorIndex];
                if (mentor == null || mentor.Senpai == null || mentor.Kohai == null)
                {
                    continue;
                }

                if (summaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                summaryBuilder.Append(mentor.Senpai.id.ToString(CultureInfo.InvariantCulture));
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(mentor.Kohai.id.ToString(CultureInfo.InvariantCulture));
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Builds compact trend summary from persisted trend-data list.
        /// </summary>
        private static string BuildRivalTrendDataSummary(IReadOnlyList<Rivals._trend_data._param> trends)
        {
            if (trends == null || trends.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(trends.Count * 18);
            for (int trendIndex = CoreConstants.ZeroBasedListStartIndex; trendIndex < trends.Count; trendIndex++)
            {
                Rivals._trend_data._param trend = trends[trendIndex];
                if (trend == null)
                {
                    continue;
                }

                if (summaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                summaryBuilder.Append(trend.Title ?? string.Empty);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(trend.IsRising ? "rising" : "falling");
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Builds compact trend summary from runtime trend list.
        /// </summary>
        private static string BuildRivalTrendRuntimeSummary(IReadOnlyList<Rivals._trend> trends)
        {
            if (trends == null || trends.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder(trends.Count * 24);
            for (int trendIndex = CoreConstants.ZeroBasedListStartIndex; trendIndex < trends.Count; trendIndex++)
            {
                Rivals._trend trend = trends[trendIndex];
                if (trend == null)
                {
                    continue;
                }

                if (summaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                }

                summaryBuilder.Append(trend.Param != null ? trend.Param.GetTitle() : string.Empty);
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(trend.IsRising ? "rising" : "falling");
                summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                summaryBuilder.Append(trend.Points.ToString(CultureInfo.InvariantCulture));
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Counts alive rival groups.
        /// </summary>
        private static int CountAliveRivalGroups(IReadOnlyList<Rivals._group> groups)
        {
            if (groups == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int count = CoreConstants.ZeroBasedListStartIndex;
            for (int groupIndex = CoreConstants.ZeroBasedListStartIndex; groupIndex < groups.Count; groupIndex++)
            {
                Rivals._group group = groups[groupIndex];
                if (group != null && !group.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts rising alive rival groups.
        /// </summary>
        private static int CountRisingRivalGroups(IReadOnlyList<Rivals._group> groups)
        {
            if (groups == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int count = CoreConstants.ZeroBasedListStartIndex;
            for (int groupIndex = CoreConstants.ZeroBasedListStartIndex; groupIndex < groups.Count; groupIndex++)
            {
                Rivals._group group = groups[groupIndex];
                if (group != null && !group.IsDead && group.IsRising)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts dead rival groups.
        /// </summary>
        private static int CountDeadRivalGroups(IReadOnlyList<Rivals._group> groups)
        {
            if (groups == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int count = CoreConstants.ZeroBasedListStartIndex;
            for (int groupIndex = CoreConstants.ZeroBasedListStartIndex; groupIndex < groups.Count; groupIndex++)
            {
                Rivals._group group = groups[groupIndex];
                if (group != null && group.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

    }
}
