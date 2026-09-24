using SaveNLoadFixes.Safety;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Repairs;
using SaveNLoadFixes.Transport;

namespace SaveNLoadFixes
{
    /// <summary>
    /// Lightweight public diagnostics for the cumulative Save n Load Fixes build.
    /// RepairsImplemented remains the eventual all-repairs completion flag; individual
    /// development repair families expose their own narrow health/diagnostic members.
    /// </summary>
    public static class SaveNLoadFixesDiagnostics
    {
        public static string ModName
        {
            get { return SaveNLoadFixesConstants.ModName; }
        }

        public static string HarmonyId
        {
            get { return SaveNLoadFixesConstants.HarmonyId; }
        }

        public static string Version
        {
            get { return SaveNLoadFixesConstants.Version; }
        }

        public static string DevelopmentStage
        {
            get { return SaveNLoadFixesConstants.DevelopmentStage; }
        }

        public static bool FansWatchShowsWideNumericCompatibilityActive
        {
            get { return FansWatchShowsWideNumericInterop.ProfileActive; }
        }

        public static string FansWatchShowsWideNumericCompatibilityStatus
        {
            get { return FansWatchShowsWideNumericInterop.Status; }
        }

        public static long FansWatchShowsWideNumericCompatibilityAppliedCount
        {
            get { return FansWatchShowsWideNumericInterop.AppliedCount; }
        }

        public static bool TbsBalancePatchWideNumericCompatibilityActive
        {
            get { return TbsBalancePatchWideNumericInterop.ProfileActive; }
        }

        public static string TbsBalancePatchWideNumericCompatibilityStatus
        {
            get { return TbsBalancePatchWideNumericInterop.Status; }
        }

        public static long TbsBalancePatchWideNumericCompatibilityAppliedCount
        {
            get { return TbsBalancePatchWideNumericInterop.AppliedCount; }
        }

        public static bool RepairsImplemented
        {
            get { return true; }
        }

        public static bool OrderedTransportImplemented
        {
            get { return SaveTransportApi.IsAuthoritativeTransport; }
        }

        public static bool CheckpointGateImplemented
        {
            get { return true; }
        }

        public static int ActiveCheckpointBlockerCount
        {
            get { return CheckpointGate.ActiveBlockerCount; }
        }

        public static string ActiveCheckpointBlockers
        {
            get { return CheckpointGate.ActiveBlockerDescription; }
        }

        public static string LastCheckpointBlockDiagnostic
        {
            get { return CheckpointGate.LastBlockedDiagnostic; }
        }

        public static bool LoadEpochImplemented
        {
            get { return true; }
        }

        public static bool LoadEpochInterceptionHealthy
        {
            get { return LoadEpochPatchHealth.IsHealthy; }
        }

        public static long CurrentLoadEpoch
        {
            get { return LoadEpoch.Current; }
        }

        public static long SuccessfulCareerLoadCount
        {
            get { return LoadEpoch.SuccessfulCareerLoadCount; }
        }

        public static string LastLoadEpochAdvanceReason
        {
            get { return LoadEpoch.LastAdvanceReason; }
        }

        public static bool EventTemplatesPendingOpenRepairImplemented
        {
            get { return EventTemplatesCheckpointRepair.IsImplemented; }
        }

        public static int EventTemplatesPendingOpenCount
        {
            get { return EventTemplatesCheckpointRepair.PendingCount; }
        }

        public static long EventTemplatesPendingOpenRegisteredCount
        {
            get { return EventTemplatesCheckpointRepair.RegisteredCount; }
        }

        public static long EventTemplatesPopupHandoffCount
        {
            get { return EventTemplatesCheckpointRepair.PopupHandoffCount; }
        }

        public static long EventTemplatesStaleSuppressionCount
        {
            get { return EventTemplatesCheckpointRepair.StaleSuppressionCount; }
        }

        public static string LastEventTemplatesCheckpointDiagnostic
        {
            get { return EventTemplatesCheckpointRepair.LastDiagnostic; }
        }


        public static bool SemanticCallbackFinalFrameRepairImplemented
        {
            get { return SemanticCallbackCheckpointRepair.IsImplemented; }
        }

        public static long SemanticCallbackTrackedCount
        {
            get { return SemanticCallbackCheckpointRepair.ActiveTrackedCount; }
        }

        public static long SemanticCallbackRegisteredCount
        {
            get { return SemanticCallbackCheckpointRepair.RegisteredCount; }
        }

        public static long SemanticCallbackCompletedCount
        {
            get { return SemanticCallbackCheckpointRepair.CompletedCount; }
        }

        public static long SemanticCallbackStaleSuppressionCount
        {
            get { return SemanticCallbackCheckpointRepair.StaleSuppressionCount; }
        }

        public static long SemanticCallbackFaultCleanupCount
        {
            get { return SemanticCallbackCheckpointRepair.FaultCleanupCount; }
        }

        public static long SemanticCallbackGarbageCollectedCleanupCount
        {
            get { return SemanticCallbackCheckpointRepair.GarbageCollectedCleanupCount; }
        }

        public static string LastSemanticCallbackCheckpointDiagnostic
        {
            get { return SemanticCallbackCheckpointRepair.LastDiagnostic; }
        }

        public static bool BirthdayQueueRepairImplemented
        {
            get { return BirthdayCheckpointRepair.IsImplemented; }
        }

        public static bool BirthdayQueueBlockerActive
        {
            get { return BirthdayCheckpointRepair.IsQueueBlockerActive; }
        }

        public static long BirthdayTrackedIteratorCount
        {
            get { return BirthdayCheckpointRepair.ActiveTrackedIteratorCount; }
        }

        public static long BirthdayRegisteredIteratorCount
        {
            get { return BirthdayCheckpointRepair.RegisteredIteratorCount; }
        }

        public static long BirthdayCompletedIteratorCount
        {
            get { return BirthdayCheckpointRepair.CompletedIteratorCount; }
        }

        public static long BirthdayStaleIteratorSuppressionCount
        {
            get { return BirthdayCheckpointRepair.StaleIteratorSuppressionCount; }
        }

        public static long BirthdayStaleQueueClearCount
        {
            get { return BirthdayCheckpointRepair.StaleQueueClearCount; }
        }

        public static long BirthdayStaleQueueClearedGirlCount
        {
            get { return BirthdayCheckpointRepair.StaleQueueClearedGirlCount; }
        }

        public static long LegacyBirthdayReconstructionCount
        {
            get { return BirthdayCheckpointRepair.LegacyReconstructionCount; }
        }

        public static long LegacyBirthdayReconstructedGirlCount
        {
            get { return BirthdayCheckpointRepair.LegacyReconstructedGirlCount; }
        }

        public static long LegacyBirthdayNoWitnessSkipCount
        {
            get { return BirthdayCheckpointRepair.LegacyNoWitnessSkipCount; }
        }

        public static string LastBirthdayCheckpointDiagnostic
        {
            get { return BirthdayCheckpointRepair.LastDiagnostic; }
        }

        public static bool SskPostPaymentRepairImplemented
        {
            get { return SskCheckpointRepair.IsImplemented; }
        }

        public static long SskActivePostPaymentLaunchCount
        {
            get { return SskCheckpointRepair.ActiveLaunchCount; }
        }

        public static long SskPostPaymentRegisteredCount
        {
            get { return SskCheckpointRepair.RegisteredCount; }
        }

        public static long SskPostPaymentCompletedIteratorCount
        {
            get { return SskCheckpointRepair.CompletedIteratorCount; }
        }

        public static long SskSynchronousPopupHandoffCount
        {
            get { return SskCheckpointRepair.SynchronousPopupHandoffCount; }
        }

        public static long SskQueuedPopupHandoffCount
        {
            get { return SskCheckpointRepair.QueuedPopupHandoffCount; }
        }

        public static long SskStaleIteratorSuppressionCount
        {
            get { return SskCheckpointRepair.StaleSuppressionCount; }
        }

        public static long SskFaultCleanupCount
        {
            get { return SskCheckpointRepair.FaultCleanupCount; }
        }

        public static long SskGarbageCollectedCleanupCount
        {
            get { return SskCheckpointRepair.GarbageCollectedCleanupCount; }
        }

        public static string LastSskCheckpointDiagnostic
        {
            get { return SskCheckpointRepair.LastDiagnostic; }
        }

        public static bool DeferredCarrierEpochRepairImplemented
        {
            get { return DeferredCarrierEpochRepair.IsImplemented; }
        }

        public static long DeferredCarrierEpochTrackedCount
        {
            get { return DeferredCarrierEpochRepair.ActiveTrackedCount; }
        }

        public static long DeferredCarrierEpochRegisteredCount
        {
            get { return DeferredCarrierEpochRepair.RegisteredCount; }
        }

        public static long DeferredCarrierEpochCompletedCount
        {
            get { return DeferredCarrierEpochRepair.CompletedCount; }
        }

        public static long DeferredCarrierEpochStaleSuppressionCount
        {
            get { return DeferredCarrierEpochRepair.StaleSuppressionCount; }
        }

        public static long DeferredCarrierEpochExplicitCancellationSuppressionCount
        {
            get { return DeferredCarrierEpochRepair.ExplicitCancellationSuppressionCount; }
        }

        public static long DeferredCarrierEpochFaultCleanupCount
        {
            get { return DeferredCarrierEpochRepair.FaultCleanupCount; }
        }

        public static long DeferredCarrierEpochGarbageCollectedCleanupCount
        {
            get { return DeferredCarrierEpochRepair.GarbageCollectedCleanupCount; }
        }

        public static long SubstoryQueueMutexResetCount
        {
            get { return DeferredCarrierEpochRepair.SubstoryMutexResetCount; }
        }

        public static string LastDeferredCarrierEpochDiagnostic
        {
            get { return DeferredCarrierEpochRepair.LastDiagnostic; }
        }

        public static bool RiskyMarketingZeroRepairImplemented
        {
            get { return RiskyMarketingZeroRepair.IsImplemented; }
        }

        public static long RiskyMarketingAuthoritativeZeroObservedCount
        {
            get { return RiskyMarketingZeroRepair.AuthoritativeZeroObservedCount; }
        }

        public static long RiskyMarketingZeroRestoredAfterRerollCount
        {
            get { return RiskyMarketingZeroRepair.RestoredAfterRerollCount; }
        }

        public static long RiskyMarketingZeroCurrentFormatNoChangeCount
        {
            get { return RiskyMarketingZeroRepair.CurrentFormatNoChangeCount; }
        }

        public static long RiskyMarketingZeroLegacyOrUnknownSkippedCount
        {
            get { return RiskyMarketingZeroRepair.LegacyOrUnknownSkippedCount; }
        }

        public static string LastRiskyMarketingZeroDiagnostic
        {
            get { return RiskyMarketingZeroRepair.LastDiagnostic; }
        }

        public static bool LastGirlIdRepairImplemented
        {
            get { return LastGirlIdRepair.IsImplemented; }
        }

        public static long LastGirlIdCapturedCount
        {
            get { return LastGirlIdRepair.CapturedCount; }
        }

        public static long LastGirlIdRestoredCount
        {
            get { return LastGirlIdRepair.RestoredCount; }
        }

        public static long LastGirlIdNoChangeCount
        {
            get { return LastGirlIdRepair.NoChangeCount; }
        }

        public static long LastGirlIdCaptureFailureCount
        {
            get { return LastGirlIdRepair.CaptureFailureCount; }
        }

        public static long LastGirlIdSpuriousAdvanceRemovedCount
        {
            get { return LastGirlIdRepair.SpuriousAdvanceRemovedCount; }
        }

        public static int LastGirlIdLastSerializedAllocator
        {
            get { return LastGirlIdRepair.LastSerializedAllocator; }
        }

        public static int LastGirlIdLastObservedBeforeRestore
        {
            get { return LastGirlIdRepair.LastObservedBeforeRestore; }
        }

        public static string LastGirlIdDiagnostic
        {
            get { return LastGirlIdRepair.LastDiagnostic; }
        }

        public static bool BankruptcyDangerRepairImplemented
        {
            get { return BankruptcyDangerRepair.IsImplemented; }
        }

        public static long BankruptcyDangerReconstructedCount
        {
            get { return BankruptcyDangerRepair.ReconstructedCount; }
        }

        public static long BankruptcyDangerNegativeMoneyDangerCount
        {
            get { return BankruptcyDangerRepair.NegativeMoneyDangerCount; }
        }

        public static long BankruptcyDangerNonNegativeMoneyClearCount
        {
            get { return BankruptcyDangerRepair.NonNegativeMoneyClearCount; }
        }

        public static long BankruptcyDangerMissingMoneyRowCount
        {
            get { return BankruptcyDangerRepair.MissingMoneyRowCount; }
        }

        public static long BankruptcyDangerDuplicateMoneyRowCount
        {
            get { return BankruptcyDangerRepair.DuplicateMoneyRowCount; }
        }

        public static long BankruptcyDangerFailureCount
        {
            get { return BankruptcyDangerRepair.FailureCount; }
        }

        public static long BankruptcyDangerLastSerializedMoney
        {
            get { return BankruptcyDangerRepair.LastSerializedMoney; }
        }

        public static long BankruptcyDangerLastPreservedDeadlineTicks
        {
            get { return BankruptcyDangerRepair.LastPreservedDeadlineTicks; }
        }

        public static string LastBankruptcyDangerDiagnostic
        {
            get { return BankruptcyDangerRepair.LastDiagnostic; }
        }

        public static bool ConcertFinishDateRepairImplemented
        {
            get { return ConcertFinishDateRepair.IsImplemented; }
        }

        public static long ConcertFinishDateLoaderInitiateCount
        {
            get { return ConcertFinishDateRepair.LoaderInitiateCount; }
        }

        public static long ConcertFinishDateRestoredCount
        {
            get { return ConcertFinishDateRepair.RestoredCount; }
        }

        public static long ConcertFinishDateUnfinishedWithoutSavedDateCount
        {
            get { return ConcertFinishDateRepair.UnfinishedWithoutSavedDateCount; }
        }

        public static long ConcertFinishDateFinishedConcertCount
        {
            get { return ConcertFinishDateRepair.FinishedConcertCount; }
        }

        public static long ConcertFinishDateChangedByInitiateCount
        {
            get { return ConcertFinishDateRepair.ChangedByInitiateCount; }
        }

        public static long ConcertFinishDateLastRestoredTicks
        {
            get { return ConcertFinishDateRepair.LastRestoredTicks; }
        }

        public static long ConcertFinishDateLastOverwrittenTicks
        {
            get { return ConcertFinishDateRepair.LastOverwrittenTicks; }
        }

        public static string LastConcertFinishDateDiagnostic
        {
            get { return ConcertFinishDateRepair.LastDiagnostic; }
        }

        public static bool TriviaZeroCounterRepairImplemented
        {
            get { return TriviaCounterRepair.IsImplemented; }
        }

        public static long TriviaGirlsLoadPrefixCount
        {
            get { return TriviaCounterRepair.GirlsLoadPrefixCount; }
        }

        public static long TriviaGraduationLoadPrefixCount
        {
            get { return TriviaCounterRepair.GraduationLoadPrefixCount; }
        }

        public static long TriviaRowsVisitedCount
        {
            get { return TriviaCounterRepair.RowsVisitedCount; }
        }

        public static long TriviaNonzeroResetCount
        {
            get { return TriviaCounterRepair.NonzeroResetCount; }
        }

        public static long TriviaNullRowCount
        {
            get { return TriviaCounterRepair.NullRowCount; }
        }

        public static long TriviaResetFailureCount
        {
            get { return TriviaCounterRepair.ResetFailureCount; }
        }

        public static string LastTriviaZeroCounterDiagnostic
        {
            get { return TriviaCounterRepair.LastDiagnostic; }
        }


        public static bool AwardHerChoiceRepairImplemented
        {
            get { return AwardHerChoiceRepair.IsImplemented; }
        }

        public static long AwardHerChoiceFirstResolutionCount
        {
            get { return AwardHerChoiceRepair.FirstResolutionCount; }
        }

        public static long AwardHerChoiceCachedReuseCount
        {
            get { return AwardHerChoiceRepair.CachedReuseCount; }
        }

        public static long AwardHerChoicePlayerResolutionCount
        {
            get { return AwardHerChoiceRepair.PlayerResolutionCount; }
        }

        public static long AwardHerChoiceFamilyResolutionCount
        {
            get { return AwardHerChoiceRepair.FamilyResolutionCount; }
        }

        public static long AwardHerChoiceUnexpectedResolutionCount
        {
            get { return AwardHerChoiceRepair.UnexpectedResolutionCount; }
        }

        public static string LastAwardHerChoiceDiagnostic
        {
            get { return AwardHerChoiceRepair.LastDiagnostic; }
        }

        public static bool GraduationDateRepairImplemented
        {
            get { return GraduationDateRepair.IsImplemented; }
        }

        public static long GraduationDateAddMonthsAppliedCount
        {
            get { return GraduationDateRepair.AddMonthsAppliedCount; }
        }

        public static long GraduationDateAddDaysAppliedCount
        {
            get { return GraduationDateRepair.AddDaysAppliedCount; }
        }

        public static int GraduationDateResolvedTargetMethodCount
        {
            get { return GraduationDatePatchHealth.ResolvedTargetMethodCount; }
        }

        public static int GraduationDateObservedAdjustmentSiteCount
        {
            get { return GraduationDatePatchHealth.ObservedAdjustmentSiteCount; }
        }

        public static long GraduationDateLastOriginalTicks
        {
            get { return GraduationDateRepair.LastOriginalTicks; }
        }

        public static long GraduationDateLastAdjustedTicks
        {
            get { return GraduationDateRepair.LastAdjustedTicks; }
        }

        public static int GraduationDateLastMonthsDelta
        {
            get { return GraduationDateRepair.LastMonthsDelta; }
        }

        public static double GraduationDateLastDaysDelta
        {
            get { return GraduationDateRepair.LastDaysDelta; }
        }

        public static string LastGraduationDateDiagnostic
        {
            get { return GraduationDateRepair.LastDiagnostic; }
        }

        public static bool ShowNextEpisodeDateRepairImplemented
        {
            get { return ShowNextEpisodeDateRepair.IsImplemented; }
        }

        public static int ShowNextEpisodeDateResolvedTargetMethodCount
        {
            get { return ShowNextEpisodeDateRepair.ResolvedTargetMethodCount; }
        }

        public static int ShowNextEpisodeDateObservedDiscardedAdvanceSiteCount
        {
            get { return ShowNextEpisodeDateRepair.ObservedDiscardedAdvanceSiteCount; }
        }

        public static string LastShowNextEpisodeDateDiagnostic
        {
            get { return ShowNextEpisodeDateRepair.LastDiagnostic; }
        }

        public static bool TutorialActiveIdRepairImplemented
        {
            get { return TutorialActiveIdRepair.IsImplemented; }
        }

        public static int TutorialActiveIdResolvedTargetMethodCount
        {
            get { return TutorialActiveIdPatchHealth.ResolvedTargetCount; }
        }

        public static long TutorialActiveIdResetBoundaryCount
        {
            get { return TutorialActiveIdRepair.ResetBoundaryCount; }
        }

        public static long TutorialActiveIdTerminalContinueBoundaryCount
        {
            get { return TutorialActiveIdRepair.TerminalContinueBoundaryCount; }
        }

        public static long TutorialActiveIdQuitBoundaryCount
        {
            get { return TutorialActiveIdRepair.QuitBoundaryCount; }
        }

        public static long TutorialActiveIdBusinessNormalizationCount
        {
            get { return TutorialActiveIdRepair.BusinessNormalizationCount; }
        }

        public static long TutorialActiveIdStaleIdClearedCount
        {
            get { return TutorialActiveIdRepair.StaleIdClearedCount; }
        }

        public static long TutorialActiveIdActiveBusinessTutorialObservedCount
        {
            get { return TutorialActiveIdRepair.ActiveBusinessTutorialObservedCount; }
        }

        public static long TutorialActiveIdBusinessNoTutorialObservedCount
        {
            get { return TutorialActiveIdRepair.BusinessNoTutorialObservedCount; }
        }

        public static long TutorialActiveIdRuntimeFailureCount
        {
            get { return TutorialActiveIdRepair.RuntimeFailureCount; }
        }

        public static string LastTutorialActiveIdDiagnostic
        {
            get { return TutorialActiveIdRepair.LastDiagnostic; }
        }

        public static bool BestSingleNominationRepairImplemented
        {
            get { return BestSingleNominationRepair.IsImplemented; }
        }

        public static int BestSingleNominationResolvedTargetMethodCount
        {
            get { return BestSingleNominationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long BestSingleNominationSelectionAttemptCount
        {
            get { return BestSingleNominationRepair.SelectionAttemptCount; }
        }

        public static long BestSingleNominationBoundCount
        {
            get { return BestSingleNominationRepair.BoundCount; }
        }

        public static long BestSingleNominationNullSelectionCount
        {
            get { return BestSingleNominationRepair.NullSelectionCount; }
        }

        public static long BestSingleNominationPreboundObservedCount
        {
            get { return BestSingleNominationRepair.PreboundObservedCount; }
        }

        public static string LastBestSingleNominationDiagnostic
        {
            get { return BestSingleNominationRepair.LastDiagnostic; }
        }

        public static bool MonthlyTransitionRepairImplemented
        {
            get { return MonthlyTransitionRepair.IsImplemented; }
        }

        public static int MonthlyTransitionResolvedTargetMethodCount
        {
            get { return MonthlyTransitionPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int MonthlyTransitionInjectionSiteCount
        {
            get { return MonthlyTransitionPatchHealth.InjectionSiteCount; }
        }

        public static long MonthlyTransitionFirstOfMonthBoundaryCount
        {
            get { return MonthlyTransitionRepair.FirstOfMonthBoundaryCount; }
        }

        public static long MonthlyTransitionEventInvocationCount
        {
            get { return MonthlyTransitionRepair.MonthEventInvocationCount; }
        }

        public static long MonthlyTransitionNoSubscriberCount
        {
            get { return MonthlyTransitionRepair.NoSubscriberCount; }
        }

        public static string LastMonthlyTransitionDiagnostic
        {
            get { return MonthlyTransitionRepair.LastDiagnostic; }
        }

        public static bool GroupTargetAudienceRepairImplemented
        {
            get { return GroupTargetAudienceRepair.IsImplemented; }
        }

        public static int GroupTargetAudienceResolvedTargetMethodCount
        {
            get { return GroupTargetAudiencePatchHealth.ResolvedTargetMethodCount; }
        }

        public static long GroupTargetAudienceAppliedCount
        {
            get { return GroupTargetAudienceRepair.AppliedCount; }
        }

        public static long GroupTargetAudienceSkippedCount
        {
            get { return GroupTargetAudienceRepair.SkippedCount; }
        }

        public static string LastGroupTargetAudienceDiagnostic
        {
            get { return GroupTargetAudienceRepair.LastDiagnostic; }
        }

        public static bool EventManagerTerminalRepairImplemented
        {
            get { return EventManagerTerminalRepair.IsImplemented; }
        }

        public static int EventManagerTerminalResolvedTargetMethodCount
        {
            get { return EventManagerTerminalPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int EventManagerTerminalObservedConcludeReturnSiteCount
        {
            get { return EventManagerTerminalPatchHealth.ObservedConcludeReturnSiteCount; }
        }

        public static int EventManagerTerminalObservedSnsDeliverySiteCount
        {
            get { return EventManagerTerminalPatchHealth.ObservedSnsDeliverySiteCount; }
        }

        public static long EventManagerResultlessCompletionCount
        {
            get { return EventManagerTerminalRepair.ResultlessCompletionCount; }
        }

        public static long EventManagerSnsOnlyCompletionCount
        {
            get { return EventManagerTerminalRepair.SnsOnlyCompletionCount; }
        }

        public static long EventManagerTerminalAlreadyCompleteCount
        {
            get { return EventManagerTerminalRepair.AlreadyTerminalCount; }
        }

        public static long EventManagerTerminalSkippedCount
        {
            get { return EventManagerTerminalRepair.SkippedCount; }
        }

        public static string LastEventManagerTerminalDiagnostic
        {
            get { return EventManagerTerminalRepair.LastDiagnostic; }
        }

        public static bool ForcedBreakupRepairImplemented
        {
            get { return ForcedBreakupRepair.IsImplemented; }
        }

        public static int ForcedBreakupResolvedTargetMethodCount
        {
            get { return ForcedBreakupPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long ForcedBreakupIdolRelationshipBreakupCount
        {
            get { return ForcedBreakupRepair.IdolRelationshipBreakupCount; }
        }

        public static long ForcedBreakupNonIdolPartnerPassThroughCount
        {
            get { return ForcedBreakupRepair.NonIdolPartnerPassThroughCount; }
        }

        public static long ForcedBreakupUnresolvedIdolRelationshipCount
        {
            get { return ForcedBreakupRepair.UnresolvedIdolRelationshipCount; }
        }

        public static long ForcedBreakupAmbiguousIdolRelationshipCount
        {
            get { return ForcedBreakupRepair.AmbiguousIdolRelationshipCount; }
        }

        public static long ForcedBreakupSkippedCount
        {
            get { return ForcedBreakupRepair.SkippedCount; }
        }

        public static string LastForcedBreakupDiagnostic
        {
            get { return ForcedBreakupRepair.LastDiagnostic; }
        }

        public static bool TrainingProgressInitRepairImplemented
        {
            get { return TrainingProgressInitRepair.IsImplemented; }
        }

        public static int TrainingProgressInitResolvedTargetMethodCount
        {
            get { return TrainingProgressInitPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long TrainingProgressInitReconstructedCount
        {
            get { return TrainingProgressInitRepair.ReconstructedCount; }
        }

        public static long TrainingProgressInitNonTrainingPassThroughCount
        {
            get { return TrainingProgressInitRepair.NonTrainingPassThroughCount; }
        }

        public static long TrainingProgressInitInvalidSerializedInputCount
        {
            get { return TrainingProgressInitRepair.InvalidSerializedInputCount; }
        }

        public static long TrainingProgressInitTargetSaveTimeUnavailableCount
        {
            get { return TrainingProgressInitRepair.TargetSaveTimeUnavailableCount; }
        }

        public static string LastTrainingProgressInitDiagnostic
        {
            get { return TrainingProgressInitRepair.LastDiagnostic; }
        }

        public static bool GlobalDataDeliveryRepairImplemented
        {
            get { return GlobalDataDeliveryRepair.IsImplemented; }
        }

        public static int GlobalDataDeliveryResolvedTargetMethodCount
        {
            get { return GlobalDataDeliveryPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long GlobalDataDeliveryLoadAttemptCount
        {
            get { return GlobalDataDeliveryRepair.LoadAttemptCount; }
        }

        public static long GlobalDataDeliverySuccessfulLoadCount
        {
            get { return GlobalDataDeliveryRepair.SuccessfulLoadCount; }
        }

        public static long GlobalDataDeliveryNormalObservedCount
        {
            get { return GlobalDataDeliveryRepair.NormalDeliveryObservedCount; }
        }

        public static long GlobalDataDeliveryPendingCreatedCount
        {
            get { return GlobalDataDeliveryRepair.PendingDeliveryCreatedCount; }
        }

        public static long GlobalDataDeliveryPendingSupersededCount
        {
            get { return GlobalDataDeliveryRepair.PendingDeliverySupersededCount; }
        }

        public static long GlobalDataDeliveryDeferredReplayCount
        {
            get { return GlobalDataDeliveryRepair.DeferredReplayCount; }
        }

        public static long GlobalDataDeliveryConsumerInvocationCount
        {
            get { return GlobalDataDeliveryRepair.ConsumerInvocationCount; }
        }

        public static long GlobalDataDeliveryRuntimeFailureCount
        {
            get { return GlobalDataDeliveryRepair.RuntimeFailureCount; }
        }

        public static string LastGlobalDataDeliveryDiagnostic
        {
            get { return GlobalDataDeliveryRepair.LastDiagnostic; }
        }

        public static bool RelationshipDynamicRepairImplemented
        {
            get { return RelationshipDynamicRepair.IsImplemented; }
        }

        public static long RelationshipDynamicRestoredLoadCount
        {
            get { return RelationshipDynamicRepair.RestoredLoadCount; }
        }

        public static long RelationshipDynamicRestoredRelationshipCount
        {
            get { return RelationshipDynamicRepair.RestoredRelationshipCount; }
        }

        public static long RelationshipDynamicLegacyEnvelopeAbsentCount
        {
            get { return RelationshipDynamicRepair.LegacyEnvelopeAbsentCount; }
        }

        public static long RelationshipDynamicInvalidEnvelopeCount
        {
            get { return RelationshipDynamicRepair.InvalidEnvelopeCount; }
        }

        public static long RelationshipDynamicAssociationMissingCount
        {
            get { return RelationshipDynamicRepair.AssociationMissingCount; }
        }

        public static long RelationshipDynamicExactSetMismatchCount
        {
            get { return RelationshipDynamicRepair.ExactSetMismatchCount; }
        }

        public static string LastRelationshipDynamicDiagnostic
        {
            get { return RelationshipDynamicRepair.LastDiagnostic; }
        }

        public static bool ShowFanAppealRepairImplemented
        {
            get { return ShowFanAppealRepair.IsImplemented; }
        }

        public static long ShowFanAppealRestoredLoadCount
        {
            get { return ShowFanAppealRepair.RestoredLoadCount; }
        }

        public static long ShowFanAppealRestoredShowCount
        {
            get { return ShowFanAppealRepair.RestoredShowCount; }
        }

        public static long ShowFanAppealLegacySectionAbsentCount
        {
            get { return ShowFanAppealRepair.LegacySectionAbsentCount; }
        }

        public static long ShowFanAppealInvalidEnvelopeCount
        {
            get { return ShowFanAppealRepair.InvalidEnvelopeCount; }
        }

        public static long ShowFanAppealAssociationMissingCount
        {
            get { return ShowFanAppealRepair.AssociationMissingCount; }
        }

        public static long ShowFanAppealExactSetMismatchCount
        {
            get { return ShowFanAppealRepair.ExactSetMismatchCount; }
        }

        public static string LastShowFanAppealDiagnostic
        {
            get { return ShowFanAppealRepair.LastDiagnostic; }
        }

        public static bool ActivityChainRepairImplemented
        {
            get { return ActivityChainRepair.IsImplemented; }
        }

        public static long ActivityChainRestoredLoadCount
        {
            get { return ActivityChainRepair.RestoredLoadCount; }
        }

        public static long ActivityChainRestoredEntryCount
        {
            get { return ActivityChainRepair.RestoredEntryCount; }
        }

        public static long ActivityChainLegacyEmptyFallbackCount
        {
            get { return ActivityChainRepair.LegacyEmptyFallbackCount; }
        }

        public static long ActivityChainStaleClearCount
        {
            get { return ActivityChainRepair.StaleChainClearCount; }
        }

        public static long ActivityChainInvalidSectionCount
        {
            get { return ActivityChainRepair.InvalidSectionCount; }
        }

        public static long ActivityChainAssociationMissingCount
        {
            get { return ActivityChainRepair.AssociationMissingCount; }
        }

        public static string LastActivityChainDiagnostic
        {
            get { return ActivityChainRepair.LastDiagnostic; }
        }

        public static bool EventOverlordLatestEventRepairImplemented
        {
            get { return EventOverlordLatestEventRepair.IsImplemented; }
        }

        public static long EventOverlordLatestEventRestoredLoadCount
        {
            get { return EventOverlordLatestEventRepair.RestoredLoadCount; }
        }

        public static long EventOverlordLatestEventLegacyTargetDateFallbackCount
        {
            get { return EventOverlordLatestEventRepair.LegacyTargetDateFallbackCount; }
        }

        public static long EventOverlordLatestEventInvalidSectionCount
        {
            get { return EventOverlordLatestEventRepair.InvalidSectionCount; }
        }

        public static long EventOverlordLatestEventAssociationMissingCount
        {
            get { return EventOverlordLatestEventRepair.AssociationMissingCount; }
        }

        public static long EventOverlordLatestEventCaptureFailureCount
        {
            get { return EventOverlordLatestEventRepair.CaptureFailureCount; }
        }

        public static string LastEventOverlordLatestEventDiagnostic
        {
            get { return EventOverlordLatestEventRepair.LastDiagnostic; }
        }

        public static bool PausedTrainingGirlRepairImplemented
        {
            get { return PausedTrainingGirlRepair.IsImplemented; }
        }

        public static long PausedTrainingGirlRestoredLoadCount
        {
            get { return PausedTrainingGirlRepair.RestoredLoadCount; }
        }

        public static long PausedTrainingGirlRestoredRoomCount
        {
            get { return PausedTrainingGirlRepair.RestoredRoomCount; }
        }

        public static long PausedTrainingGirlLegacySectionAbsentCount
        {
            get { return PausedTrainingGirlRepair.LegacySectionAbsentCount; }
        }

        public static long PausedTrainingGirlStaleClearCount
        {
            get { return PausedTrainingGirlRepair.StaleClearCount; }
        }

        public static long PausedTrainingGirlInvalidSectionCount
        {
            get { return PausedTrainingGirlRepair.InvalidSectionCount; }
        }

        public static long PausedTrainingGirlAssociationMissingCount
        {
            get { return PausedTrainingGirlRepair.AssociationMissingCount; }
        }

        public static long PausedTrainingGirlCaptureFailureCount
        {
            get { return PausedTrainingGirlRepair.CaptureFailureCount; }
        }

        public static string LastPausedTrainingGirlDiagnostic
        {
            get { return PausedTrainingGirlRepair.LastDiagnostic; }
        }

        public static bool BusinessRemainingMinutesRepairImplemented
        {
            get { return BusinessRemainingMinutesRepair.IsImplemented; }
        }

        public static long BusinessRemainingMinutesRestoredLoadCount
        {
            get { return BusinessRemainingMinutesRepair.RestoredLoadCount; }
        }

        public static long BusinessRemainingMinutesRestoredRoomCount
        {
            get { return BusinessRemainingMinutesRepair.RestoredRoomCount; }
        }

        public static long BusinessRemainingMinutesLegacySectionAbsentCount
        {
            get { return BusinessRemainingMinutesRepair.LegacySectionAbsentCount; }
        }

        public static long BusinessRemainingMinutesInvalidSectionCount
        {
            get { return BusinessRemainingMinutesRepair.InvalidSectionCount; }
        }

        public static long BusinessRemainingMinutesAssociationMissingCount
        {
            get { return BusinessRemainingMinutesRepair.AssociationMissingCount; }
        }

        public static long BusinessRemainingMinutesCaptureFailureCount
        {
            get { return BusinessRemainingMinutesRepair.CaptureFailureCount; }
        }

        public static string LastBusinessRemainingMinutesDiagnostic
        {
            get { return BusinessRemainingMinutesRepair.LastDiagnostic; }
        }

        public static bool TutorialActivityBaselineRepairImplemented
        {
            get { return TutorialActivityBaselineRepair.IsImplemented; }
        }

        public static long TutorialActivityBaselineRestoredLoadCount
        {
            get { return TutorialActivityBaselineRepair.RestoredLoadCount; }
        }

        public static long TutorialActivityBaselineRestoredInitializedCount
        {
            get { return TutorialActivityBaselineRepair.RestoredInitializedBaselineCount; }
        }

        public static long TutorialActivityBaselineLegacyUninitializedCount
        {
            get { return TutorialActivityBaselineRepair.LegacyUninitializedCount; }
        }

        public static long TutorialActivityBaselineInvalidSectionCount
        {
            get { return TutorialActivityBaselineRepair.InvalidSectionCount; }
        }

        public static long TutorialActivityBaselineAssociationMissingCount
        {
            get { return TutorialActivityBaselineRepair.AssociationMissingCount; }
        }

        public static long TutorialActivityBaselineCaptureFailureCount
        {
            get { return TutorialActivityBaselineRepair.CaptureFailureCount; }
        }

        public static string LastTutorialActivityBaselineDiagnostic
        {
            get { return TutorialActivityBaselineRepair.LastDiagnostic; }
        }

        public static bool ProjectProgressCounterRepairImplemented
        {
            get { return ProjectProgressCounterRepair.IsImplemented; }
        }

        public static int ProjectProgressCounterResolvedTargetMethodCount
        {
            get { return ProjectProgressCounterPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long ProjectProgressCounterRestoredLoadCount
        {
            get { return ProjectProgressCounterRepair.RestoredLoadCount; }
        }

        public static long ProjectProgressCounterRestoredParameterCount
        {
            get { return ProjectProgressCounterRepair.RestoredParameterCount; }
        }

        public static long ProjectProgressCounterRestoredNonZeroCount
        {
            get { return ProjectProgressCounterRepair.RestoredNonZeroCounterCount; }
        }

        public static long ProjectProgressCounterLegacyZeroFallbackCount
        {
            get { return ProjectProgressCounterRepair.LegacyZeroFallbackLoadCount; }
        }

        public static long ProjectProgressCounterInvalidSectionCount
        {
            get { return ProjectProgressCounterRepair.InvalidSectionCount; }
        }

        public static long ProjectProgressCounterAssociationMissingCount
        {
            get { return ProjectProgressCounterRepair.AssociationMissingCount; }
        }

        public static long ProjectProgressCounterCaptureFailureCount
        {
            get { return ProjectProgressCounterRepair.CaptureFailureCount; }
        }

        public static string LastProjectProgressCounterDiagnostic
        {
            get { return ProjectProgressCounterRepair.LastDiagnostic; }
        }

        public static bool RecentActivityRecencyRepairImplemented
        {
            get { return RecentActivityRecencyRepair.IsImplemented; }
        }

        public static int RecentActivityRecencyResolvedTargetMethodCount
        {
            get { return RecentActivityRecencyPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long RecentActivityRecencyRestoredLoadCount
        {
            get { return RecentActivityRecencyRepair.RestoredLoadCount; }
        }

        public static long RecentActivityRecencyRestoredRowCount
        {
            get { return RecentActivityRecencyRepair.RestoredRowCount; }
        }

        public static long RecentActivityRecencyRestoredSpaAnchorCount
        {
            get { return RecentActivityRecencyRepair.RestoredSpaAnchorCount; }
        }

        public static long RecentActivityRecencyLegacyEmptyFallbackCount
        {
            get { return RecentActivityRecencyRepair.LegacyEmptyFallbackCount; }
        }

        public static long RecentActivityRecencyInvalidSectionCount
        {
            get { return RecentActivityRecencyRepair.InvalidSectionCount; }
        }

        public static long RecentActivityRecencyAssociationMissingCount
        {
            get { return RecentActivityRecencyRepair.AssociationMissingCount; }
        }

        public static long RecentActivityRecencyCaptureFailureCount
        {
            get { return RecentActivityRecencyRepair.CaptureFailureCount; }
        }

        public static string LastRecentActivityRecencyDiagnostic
        {
            get { return RecentActivityRecencyRepair.LastDiagnostic; }
        }

        public static bool PreviousNewSubstoryRepairImplemented
        {
            get { return PreviousNewSubstoryRepair.IsImplemented; }
        }

        public static int PreviousNewSubstoryResolvedTargetMethodCount
        {
            get { return PreviousNewSubstoryPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long PreviousNewSubstoryRestoredLoadCount
        {
            get { return PreviousNewSubstoryRepair.RestoredLoadCount; }
        }

        public static long PreviousNewSubstoryLegacyQueuedFallbackCount
        {
            get { return PreviousNewSubstoryRepair.LegacyQueuedFallbackCount; }
        }

        public static long PreviousNewSubstoryLegacyDialogueDateFallbackCount
        {
            get { return PreviousNewSubstoryRepair.LegacyDialogueDateFallbackCount; }
        }

        public static long PreviousNewSubstoryLegacyStartDateFallbackCount
        {
            get { return PreviousNewSubstoryRepair.LegacyStartDateFallbackCount; }
        }

        public static long PreviousNewSubstoryLegacyTargetDateFallbackCount
        {
            get { return PreviousNewSubstoryRepair.LegacyTargetDateFallbackCount; }
        }

        public static long PreviousNewSubstoryInvalidSectionCount
        {
            get { return PreviousNewSubstoryRepair.InvalidSectionCount; }
        }

        public static long PreviousNewSubstoryAssociationMissingCount
        {
            get { return PreviousNewSubstoryRepair.AssociationMissingCount; }
        }

        public static long PreviousNewSubstoryCaptureFailureCount
        {
            get { return PreviousNewSubstoryRepair.CaptureFailureCount; }
        }

        public static string LastPreviousNewSubstoryDiagnostic
        {
            get { return PreviousNewSubstoryRepair.LastDiagnostic; }
        }

        public static bool PushSlotDurationRepairImplemented
        {
            get { return PushSlotDurationRepair.IsImplemented; }
        }

        public static int PushSlotDurationResolvedTargetMethodCount
        {
            get { return PushSlotDurationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long PushSlotDurationResetCount
        {
            get { return PushSlotDurationRepair.ResetCount; }
        }

        public static long PushSlotDurationNonPushReceiverPassThroughCount
        {
            get { return PushSlotDurationRepair.NonPushReceiverPassThroughCount; }
        }

        public static long PushSlotDurationSameIdentityPassThroughCount
        {
            get { return PushSlotDurationRepair.SameIdentityPassThroughCount; }
        }

        public static long PushSlotDurationInvalidShapeCount
        {
            get { return PushSlotDurationRepair.InvalidShapeCount; }
        }

        public static string LastPushSlotDurationDiagnostic
        {
            get { return PushSlotDurationRepair.LastDiagnostic; }
        }

        public static bool FanAppealLastSingleRepairImplemented
        {
            get { return FanAppealLastSingleRepair.IsImplemented; }
        }

        public static int FanAppealLastSingleResolvedTargetMethodCount
        {
            get { return FanAppealLastSinglePatchHealth.ResolvedTargetMethodCount; }
        }

        public static long FanAppealLastSingleStaleClearCount
        {
            get { return FanAppealLastSingleRepair.StaleClearCount; }
        }

        public static long FanAppealLastSingleReleaseObservedCount
        {
            get { return FanAppealLastSingleRepair.ReleaseObservedCount; }
        }

        public static long FanAppealLastSingleRestoredExactCount
        {
            get { return FanAppealLastSingleRepair.RestoredExactCount; }
        }

        public static long FanAppealLastSingleRestoredNullCount
        {
            get { return FanAppealLastSingleRepair.RestoredNullCount; }
        }

        public static long FanAppealLastSingleLegacySynthesizedCount
        {
            get { return FanAppealLastSingleRepair.LegacySynthesizedCount; }
        }

        public static long FanAppealLastSingleLegacyNoReleasedSingleCount
        {
            get { return FanAppealLastSingleRepair.LegacyNoReleasedSingleCount; }
        }

        public static long FanAppealLastSingleLegacyDeferredCount
        {
            get { return FanAppealLastSingleRepair.LegacyDeferredCount; }
        }

        public static long FanAppealLastSingleLegacyDeferredDiscardedCount
        {
            get { return FanAppealLastSingleRepair.LegacyDeferredDiscardedCount; }
        }

        public static long FanAppealLastSingleInvalidSectionCount
        {
            get { return FanAppealLastSingleRepair.InvalidSectionCount; }
        }

        public static long FanAppealLastSingleAssociationMissingCount
        {
            get { return FanAppealLastSingleRepair.AssociationMissingCount; }
        }

        public static long FanAppealLastSingleCaptureFailureCount
        {
            get { return FanAppealLastSingleRepair.CaptureFailureCount; }
        }

        public static string LastFanAppealLastSingleDiagnostic
        {
            get { return FanAppealLastSingleRepair.LastDiagnostic; }
        }

        public static bool SelectedBusinessProposalRepairImplemented
        {
            get { return SelectedBusinessProposalRepair.IsImplemented; }
        }

        public static int SelectedBusinessProposalResolvedTargetMethodCount
        {
            get { return SelectedBusinessProposalPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long SelectedBusinessProposalCaptureFailureCount
        {
            get { return SelectedBusinessProposalRepair.CaptureFailureCount; }
        }

        public static long SelectedBusinessProposalStaleClearCount
        {
            get { return SelectedBusinessProposalRepair.StaleClearCount; }
        }

        public static long SelectedBusinessProposalRestoredCount
        {
            get { return SelectedBusinessProposalRepair.RestoredCount; }
        }

        public static long SelectedBusinessProposalRestoredNullCount
        {
            get { return SelectedBusinessProposalRepair.RestoredNullCount; }
        }

        public static long SelectedBusinessProposalLegacyMissingCount
        {
            get { return SelectedBusinessProposalRepair.LegacyMissingCount; }
        }

        public static long SelectedBusinessProposalInvalidSectionCount
        {
            get { return SelectedBusinessProposalRepair.InvalidSectionCount; }
        }

        public static long SelectedBusinessProposalAssociationMissingCount
        {
            get { return SelectedBusinessProposalRepair.AssociationMissingCount; }
        }

        public static string LastSelectedBusinessProposalDiagnostic
        {
            get { return SelectedBusinessProposalRepair.LastDiagnostic; }
        }

        public static bool QueuedBeforeStartRepairImplemented
        {
            get { return QueuedBeforeStartRepair.IsImplemented; }
        }

        public static int QueuedBeforeStartResolvedTargetMethodCount
        {
            get { return QueuedBeforeStartPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long QueuedBeforeStartCaptureFailureCount
        {
            get { return QueuedBeforeStartRepair.CaptureFailureCount; }
        }

        public static long QueuedBeforeStartClassifiedCallbackCount
        {
            get { return QueuedBeforeStartRepair.ClassifiedCallbackCount; }
        }

        public static long QueuedBeforeStartRestoredDescriptorCount
        {
            get { return QueuedBeforeStartRepair.RestoredDescriptorCount; }
        }

        public static long QueuedBeforeStartLegacyMissingCount
        {
            get { return QueuedBeforeStartRepair.LegacyMissingCount; }
        }

        public static long QueuedBeforeStartInvalidSectionCount
        {
            get { return QueuedBeforeStartRepair.InvalidSectionCount; }
        }

        public static long QueuedBeforeStartAssociationMissingCount
        {
            get { return QueuedBeforeStartRepair.AssociationMissingCount; }
        }

        public static long QueuedBeforeStartRuntimeResolutionFailureCount
        {
            get { return QueuedBeforeStartRepair.RuntimeResolutionFailureCount; }
        }

        public static string LastQueuedBeforeStartDiagnostic
        {
            get { return QueuedBeforeStartRepair.LastDiagnostic; }
        }

        public static long RepairEnvelopeFrozenCheckpointCount
        {
            get { return RepairEnvelopeTransport.FrozenCheckpointCount; }
        }

        public static long RepairEnvelopeReadCount
        {
            get { return RepairEnvelopeTransport.EnvelopeReadCount; }
        }

        public static long RepairEnvelopeSimpleJsonRecoveredReadCount
        {
            get { return RepairEnvelopeTransport.SimpleJsonRecoveredReadCount; }
        }

        public static long RepairEnvelopeLegacyReadCount
        {
            get { return RepairEnvelopeTransport.LegacyReadCount; }
        }

        public static long RepairEnvelopeInvalidReadCount
        {
            get { return RepairEnvelopeTransport.InvalidEnvelopeReadCount; }
        }

        public static long RepairEnvelopeFreezeFailureCount
        {
            get { return RepairEnvelopeTransport.FreezeFailureCount; }
        }

        public static string LastRepairEnvelopeDiagnostic
        {
            get { return RepairEnvelopeTransport.LastDiagnostic; }
        }

        public static long StartupSaveMigrationAttemptCount
        {
            get { return StartupSaveFileMigration.AttemptCount; }
        }

        public static long StartupSaveMigrationMigratedFileCount
        {
            get { return StartupSaveFileMigration.MigratedFileCount; }
        }

        public static long StartupSaveMigrationMigratedFieldCount
        {
            get { return StartupSaveFileMigration.MigratedFieldCount; }
        }

        public static long StartupSaveMigrationFailureCount
        {
            get { return StartupSaveFileMigration.FailureCount; }
        }

        public static string LastStartupSaveMigrationDiagnostic
        {
            get { return StartupSaveFileMigration.LastDiagnostic; }
        }

        public static bool SnsMessageExactStateRepairImplemented
        {
            get { return SnsMessageRepair.IsImplemented; }
        }

        public static long SnsMessageCapturedCheckpointCount
        {
            get { return SnsMessageRepair.CapturedCheckpointCount; }
        }

        public static long SnsMessageCapturedNodeCount
        {
            get { return SnsMessageRepair.CapturedNodeCount; }
        }

        public static long SnsMessageRestoredLoadCount
        {
            get { return SnsMessageRepair.RestoredLoadCount; }
        }

        public static long SnsMessageRestoredNodeCount
        {
            get { return SnsMessageRepair.RestoredNodeCount; }
        }

        public static long SnsMessageLegacySectionAbsentCount
        {
            get { return SnsMessageRepair.LegacySectionAbsentCount; }
        }

        public static long SnsMessageInvalidSectionCount
        {
            get { return SnsMessageRepair.InvalidSectionCount; }
        }

        public static long SnsMessageAssociationMissingCount
        {
            get { return SnsMessageRepair.AssociationMissingCount; }
        }

        public static string LastSnsMessageDiagnostic
        {
            get { return SnsMessageRepair.LastDiagnostic; }
        }

        public static bool RoomSubstorySceneRepairImplemented
        {
            get { return RoomSubstorySceneRepair.IsImplemented; }
        }

        public static int RoomSubstorySceneResolvedTargetMethodCount
        {
            get { return RoomSubstoryScenePatchHealth.ResolvedTargetMethodCount; }
        }

        public static long RoomSubstorySceneRestoredLoadCount
        {
            get { return RoomSubstorySceneRepair.RestoredLoadCount; }
        }

        public static long RoomSubstorySceneRestoredRoomCount
        {
            get { return RoomSubstorySceneRepair.RestoredRoomCount; }
        }

        public static long RoomSubstorySceneLegacySectionAbsentCount
        {
            get { return RoomSubstorySceneRepair.LegacySectionAbsentCount; }
        }

        public static long RoomSubstorySceneStaleClearCount
        {
            get { return RoomSubstorySceneRepair.StaleClearCount; }
        }

        public static long RoomSubstorySceneInvalidSectionCount
        {
            get { return RoomSubstorySceneRepair.InvalidSectionCount; }
        }

        public static long RoomSubstorySceneAssociationMissingCount
        {
            get { return RoomSubstorySceneRepair.AssociationMissingCount; }
        }

        public static long RoomSubstorySceneCaptureFailureCount
        {
            get { return RoomSubstorySceneRepair.CaptureFailureCount; }
        }

        public static long RoomSubstorySceneRuntimeResolutionFailureCount
        {
            get { return RoomSubstorySceneRepair.RuntimeResolutionFailureCount; }
        }

        public static string LastRoomSubstorySceneDiagnostic
        {
            get { return RoomSubstorySceneRepair.LastDiagnostic; }
        }

        public static bool PendingAmbientSceneRepairImplemented
        {
            get { return PendingAmbientSceneRepair.IsImplemented; }
        }

        public static int PendingAmbientSceneResolvedTargetMethodCount
        {
            get { return PendingAmbientScenePatchHealth.ResolvedTargetMethodCount; }
        }

        public static long PendingAmbientSceneTrackedCount
        {
            get { return PendingAmbientSceneRepair.ActiveTrackedCount; }
        }

        public static int PendingAmbientSceneCurrentEpochJobCount
        {
            get { return PendingAmbientSceneRepair.CurrentEpochJobCount; }
        }

        public static long PendingAmbientSceneRegisteredCount
        {
            get { return PendingAmbientSceneRepair.RegisteredCount; }
        }

        public static long PendingAmbientSceneCompletedCount
        {
            get { return PendingAmbientSceneRepair.CompletedCount; }
        }

        public static long PendingAmbientSceneStaleRetiredCount
        {
            get { return PendingAmbientSceneRepair.StaleRetiredCount; }
        }

        public static long PendingAmbientSceneFaultRetiredCount
        {
            get { return PendingAmbientSceneRepair.FaultRetiredCount; }
        }

        public static long PendingAmbientSceneAbandonedCount
        {
            get { return PendingAmbientSceneRepair.AbandonedCount; }
        }

        public static long PendingAmbientSceneRejectedCaptureCount
        {
            get { return PendingAmbientSceneRepair.RejectedCaptureCount; }
        }

        public static string LastPendingAmbientSceneDiagnostic
        {
            get { return PendingAmbientSceneRepair.LastDiagnostic; }
        }

        public static bool PendingAmbientScenePersistenceImplemented
        {
            get { return PendingAmbientScenePersistence.IsImplemented; }
        }

        public static long PendingAmbientSceneCapturedCheckpointCount
        {
            get { return PendingAmbientScenePersistence.CapturedCheckpointCount; }
        }

        public static long PendingAmbientSceneCapturedJobCount
        {
            get { return PendingAmbientScenePersistence.CapturedJobCount; }
        }

        public static long PendingAmbientSceneEnvelopeCaptureFailureCount
        {
            get { return PendingAmbientScenePersistence.CaptureFailureCount; }
        }

        public static string LastPendingAmbientScenePersistenceDiagnostic
        {
            get { return PendingAmbientScenePersistence.LastDiagnostic; }
        }

        public static long PendingAmbientSceneRestoreRollbackRetiredCount
        {
            get { return PendingAmbientSceneRepair.RestoreRollbackRetiredCount; }
        }

        public static bool PendingAmbientSceneRestoreImplemented
        {
            get { return PendingAmbientSceneRestore.IsImplemented; }
        }

        public static int PendingAmbientSceneRestoreResolvedTargetMethodCount
        {
            get { return PendingAmbientSceneRestorePatchHealth.ResolvedTargetMethodCount; }
        }

        public static long PendingAmbientSceneRestoredLoadCount
        {
            get { return PendingAmbientSceneRestore.RestoredLoadCount; }
        }

        public static long PendingAmbientSceneRescheduledJobCount
        {
            get { return PendingAmbientSceneRestore.RescheduledJobCount; }
        }

        public static long PendingAmbientSceneRestoreLegacySectionAbsentCount
        {
            get { return PendingAmbientSceneRestore.LegacySectionAbsentCount; }
        }

        public static long PendingAmbientSceneRestoreInvalidSectionCount
        {
            get { return PendingAmbientSceneRestore.InvalidSectionCount; }
        }

        public static long PendingAmbientSceneRestoreAssociationMissingCount
        {
            get { return PendingAmbientSceneRestore.AssociationMissingCount; }
        }

        public static long PendingAmbientSceneRestoreRuntimeResolutionFailureCount
        {
            get { return PendingAmbientSceneRestore.RuntimeResolutionFailureCount; }
        }

        public static long PendingAmbientSceneRestoreSchedulingFailureCount
        {
            get { return PendingAmbientSceneRestore.SchedulingFailureCount; }
        }

        public static long PendingAmbientSceneRestoreDuplicateAttemptSuppressedCount
        {
            get { return PendingAmbientSceneRestore.DuplicateAttemptSuppressedCount; }
        }

        public static long PendingAmbientSceneRestoreStaleAgencyLoadTriggerIgnoredCount
        {
            get { return PendingAmbientSceneRestore.StaleAgencyLoadTriggerIgnoredCount; }
        }

        public static string LastPendingAmbientSceneRestoreDiagnostic
        {
            get { return PendingAmbientSceneRestore.LastDiagnostic; }
        }

        public static bool SuccessorIntroductionRepairImplemented
        {
            get { return SuccessorIntroductionRepair.IsImplemented; }
        }

        public static int SuccessorIntroductionResolvedTargetMethodCount
        {
            get { return SuccessorIntroductionPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long SuccessorIntroductionScheduledCount
        {
            get { return SuccessorIntroductionRepair.ScheduledCount; }
        }

        public static long SuccessorIntroductionSupersededScheduleCount
        {
            get { return SuccessorIntroductionRepair.SupersededScheduleCount; }
        }

        public static long SuccessorIntroductionCapturedCheckpointCount
        {
            get { return SuccessorIntroductionRepair.CapturedCheckpointCount; }
        }

        public static long SuccessorIntroductionCapturedPendingCount
        {
            get { return SuccessorIntroductionRepair.CapturedPendingCount; }
        }

        public static long SuccessorIntroductionCaptureFailureCount
        {
            get { return SuccessorIntroductionRepair.CaptureFailureCount; }
        }

        public static long SuccessorIntroductionCompletedFlushCount
        {
            get { return SuccessorIntroductionRepair.CompletedFlushCount; }
        }

        public static long SuccessorIntroductionStaleNewGirlsClearCount
        {
            get { return SuccessorIntroductionRepair.StaleNewGirlsClearCount; }
        }

        public static long SuccessorIntroductionStalePendingTokenClearCount
        {
            get { return SuccessorIntroductionRepair.StalePendingTokenClearCount; }
        }

        public static long SuccessorIntroductionRestoredLoadCount
        {
            get { return SuccessorIntroductionRepair.RestoredLoadCount; }
        }

        public static long SuccessorIntroductionRestoredGirlCount
        {
            get { return SuccessorIntroductionRepair.RestoredGirlCount; }
        }

        public static long SuccessorIntroductionUnresolvedLoadedGirlCount
        {
            get { return SuccessorIntroductionRepair.UnresolvedLoadedGirlCount; }
        }

        public static long SuccessorIntroductionLegacySectionAbsentCount
        {
            get { return SuccessorIntroductionRepair.LegacySectionAbsentCount; }
        }

        public static long SuccessorIntroductionInvalidSectionCount
        {
            get { return SuccessorIntroductionRepair.InvalidSectionCount; }
        }

        public static long SuccessorIntroductionAssociationMissingCount
        {
            get { return SuccessorIntroductionRepair.AssociationMissingCount; }
        }

        public static long SuccessorIntroductionSchedulingFailureCount
        {
            get { return SuccessorIntroductionRepair.SchedulingFailureCount; }
        }

        public static string LastSuccessorIntroductionDiagnostic
        {
            get { return SuccessorIntroductionRepair.LastDiagnostic; }
        }

        public static bool TemporaryAutoTaskBanRepairImplemented
        {
            get { return TemporaryAutoTaskBanRepair.IsImplemented; }
        }

        public static int TemporaryAutoTaskBanResolvedTargetMethodCount
        {
            get { return TemporaryAutoTaskBanPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int TemporaryAutoTaskBanObservedWaitForSecondsSiteCount
        {
            get { return TemporaryAutoTaskBanPatchHealth.ObservedWaitForSecondsSiteCount; }
        }

        public static long TemporaryAutoTaskBanRegisteredCount
        {
            get { return TemporaryAutoTaskBanRepair.RegisteredCount; }
        }

        public static long TemporaryAutoTaskBanDuplicateFactoryCount
        {
            get { return TemporaryAutoTaskBanRepair.DuplicateFactoryCount; }
        }

        public static long TemporaryAutoTaskBanCompletedExpiryCount
        {
            get { return TemporaryAutoTaskBanRepair.CompletedExpiryCount; }
        }

        public static long TemporaryAutoTaskBanCapturedCheckpointCount
        {
            get { return TemporaryAutoTaskBanRepair.CapturedCheckpointCount; }
        }

        public static long TemporaryAutoTaskBanCapturedBanCount
        {
            get { return TemporaryAutoTaskBanRepair.CapturedBanCount; }
        }

        public static long TemporaryAutoTaskBanCaptureFailureCount
        {
            get { return TemporaryAutoTaskBanRepair.CaptureFailureCount; }
        }

        public static long TemporaryAutoTaskBanRestoredLoadCount
        {
            get { return TemporaryAutoTaskBanRepair.RestoredLoadCount; }
        }

        public static long TemporaryAutoTaskBanRestoredBanCount
        {
            get { return TemporaryAutoTaskBanRepair.RestoredBanCount; }
        }

        public static long TemporaryAutoTaskBanLegacySectionAbsentCount
        {
            get { return TemporaryAutoTaskBanRepair.LegacySectionAbsentCount; }
        }

        public static long TemporaryAutoTaskBanInvalidSectionCount
        {
            get { return TemporaryAutoTaskBanRepair.InvalidSectionCount; }
        }

        public static long TemporaryAutoTaskBanRuntimeResolutionFailureCount
        {
            get { return TemporaryAutoTaskBanRepair.RuntimeResolutionFailureCount; }
        }

        public static long TemporaryAutoTaskBanRollbackCount
        {
            get { return TemporaryAutoTaskBanRepair.RollbackCount; }
        }

        public static string LastTemporaryAutoTaskBanDiagnostic
        {
            get { return TemporaryAutoTaskBanRepair.LastDiagnostic; }
        }

        public static bool DelayedTutorialContinuationRepairImplemented
        {
            get { return DelayedTutorialContinuationRepair.IsImplemented; }
        }

        public static int DelayedTutorialContinuationResolvedTargetMethodCount
        {
            get { return DelayedTutorialContinuationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int DelayedTutorialContinuationObservedWaitForSecondsSiteCount
        {
            get { return DelayedTutorialContinuationPatchHealth.ObservedActivitiesWaitForSecondsSiteCount; }
        }

        public static long DelayedTutorialContinuationRegisteredCount
        {
            get { return DelayedTutorialContinuationRepair.RegisteredCount; }
        }

        public static long DelayedTutorialContinuationDuplicateFactoryCount
        {
            get { return DelayedTutorialContinuationRepair.DuplicateFactoryCount; }
        }

        public static long DelayedTutorialContinuationCompletedCount
        {
            get { return DelayedTutorialContinuationRepair.CompletedCount; }
        }

        public static long DelayedTutorialContinuationCapturedCheckpointCount
        {
            get { return DelayedTutorialContinuationRepair.CapturedCheckpointCount; }
        }

        public static long DelayedTutorialContinuationCapturedCount
        {
            get { return DelayedTutorialContinuationRepair.CapturedContinuationCount; }
        }

        public static long DelayedTutorialContinuationRestoredLoadCount
        {
            get { return DelayedTutorialContinuationRepair.RestoredLoadCount; }
        }

        public static long DelayedTutorialContinuationRestoredCount
        {
            get { return DelayedTutorialContinuationRepair.RestoredContinuationCount; }
        }

        public static long DelayedTutorialContinuationIdempotentDiscardCount
        {
            get { return DelayedTutorialContinuationRepair.IdempotentDiscardCount; }
        }

        public static long DelayedTutorialContinuationLegacySectionAbsentCount
        {
            get { return DelayedTutorialContinuationRepair.LegacySectionAbsentCount; }
        }

        public static long DelayedTutorialContinuationInvalidSectionCount
        {
            get { return DelayedTutorialContinuationRepair.InvalidSectionCount; }
        }

        public static long DelayedTutorialContinuationAssociationMissingCount
        {
            get { return DelayedTutorialContinuationRepair.AssociationMissingCount; }
        }

        public static long DelayedTutorialContinuationSchedulingFailureCount
        {
            get { return DelayedTutorialContinuationRepair.SchedulingFailureCount; }
        }

        public static long DelayedTutorialContinuationRollbackCount
        {
            get { return DelayedTutorialContinuationRepair.RollbackCount; }
        }

        public static string LastDelayedTutorialContinuationDiagnostic
        {
            get { return DelayedTutorialContinuationRepair.LastDiagnostic; }
        }

        public static bool AwardTempNominationRepairImplemented
        {
            get { return AwardTempNominationRepair.IsImplemented; }
        }

        public static int AwardTempNominationResolvedTargetMethodCount
        {
            get { return AwardTempNominationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long AwardTempNominationCapturedCheckpointCount
        {
            get { return AwardTempNominationRepair.CapturedCheckpointCount; }
        }

        public static long AwardTempNominationCapturedCount
        {
            get { return AwardTempNominationRepair.CapturedNominationCount; }
        }

        public static long AwardTempNominationStaleClearCount
        {
            get { return AwardTempNominationRepair.StaleClearCount; }
        }

        public static long AwardTempNominationRestoredExactLoadCount
        {
            get { return AwardTempNominationRepair.RestoredExactLoadCount; }
        }

        public static long AwardTempNominationRestoredExactCount
        {
            get { return AwardTempNominationRepair.RestoredExactNominationCount; }
        }

        public static long AwardTempNominationRestoredEmptyLoadCount
        {
            get { return AwardTempNominationRepair.RestoredEmptyLoadCount; }
        }

        public static long AwardTempNominationLegacyReconstructedLoadCount
        {
            get { return AwardTempNominationRepair.LegacyReconstructedLoadCount; }
        }

        public static long AwardTempNominationLegacyReconstructedCount
        {
            get { return AwardTempNominationRepair.LegacyReconstructedNominationCount; }
        }

        public static long AwardTempNominationLegacyNoWitnessCount
        {
            get { return AwardTempNominationRepair.LegacyNoWitnessCount; }
        }

        public static long AwardTempNominationLegacyNullableNomineeCount
        {
            get { return AwardTempNominationRepair.LegacyNullableNomineeCount; }
        }

        public static long AwardTempNominationInvalidSectionCount
        {
            get { return AwardTempNominationRepair.InvalidSectionCount; }
        }

        public static long AwardTempNominationAssociationMissingCount
        {
            get { return AwardTempNominationRepair.AssociationMissingCount; }
        }

        public static string LastAwardTempNominationDiagnostic
        {
            get { return AwardTempNominationRepair.LastDiagnostic; }
        }

        public static bool ExternalPortraitIdentityRepairImplemented
        {
            get { return ExternalPortraitIdentityRepair.IsImplemented; }
        }

        public static int ExternalPortraitIdentityResolvedTargetMethodCount
        {
            get { return ExternalPortraitIdentityPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int ExternalPortraitIdentityUnresolvedCount
        {
            get { return ExternalPortraitIdentityRepair.UnresolvedIdentityCount; }
        }

        public static long ExternalPortraitIdentityDetectedFallbackCount
        {
            get { return ExternalPortraitIdentityRepair.DetectedFallbackCount; }
        }

        public static long ExternalPortraitIdentityExactRebindCount
        {
            get { return ExternalPortraitIdentityRepair.ExactRebindCount; }
        }

        public static long ExternalPortraitIdentitySavedDtoRewriteCount
        {
            get { return ExternalPortraitIdentityRepair.SavedDtoRewriteCount; }
        }

        public static long ExternalPortraitIdentityCapturedCheckpointCount
        {
            get { return ExternalPortraitIdentityRepair.CapturedCheckpointCount; }
        }

        public static long ExternalPortraitIdentityCapturedIdentityCount
        {
            get { return ExternalPortraitIdentityRepair.CapturedIdentityCount; }
        }

        public static long ExternalPortraitIdentityLegacySectionAbsentCount
        {
            get { return ExternalPortraitIdentityRepair.LegacySectionAbsentCount; }
        }

        public static long ExternalPortraitIdentityInvalidSectionCount
        {
            get { return ExternalPortraitIdentityRepair.InvalidSectionCount; }
        }

        public static long ExternalPortraitIdentityAssociationMissingCount
        {
            get { return ExternalPortraitIdentityRepair.AssociationMissingCount; }
        }

        public static long ExternalPortraitIdentityRuntimeResolutionFailureCount
        {
            get { return ExternalPortraitIdentityRepair.RuntimeResolutionFailureCount; }
        }

        public static string LastExternalPortraitIdentityDiagnostic
        {
            get { return ExternalPortraitIdentityRepair.LastDiagnostic; }
        }

        public static bool LegacyIdolProfileMigrationImplemented
        {
            get { return LegacyIdolProfileMigration.IsImplemented; }
        }

        public static int LegacyIdolProfileMigrationResolvedTargetMethodCount
        {
            get { return LegacyIdolProfileMigrationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int LegacyMigrationDeterminismAlgorithmVersion
        {
            get { return LegacyMigrationDeterminism.AlgorithmVersion; }
        }

        public static long LegacyIdolProfileMigratedLoadCount
        {
            get { return LegacyIdolProfileMigration.MigratedLoadCount; }
        }

        public static long LegacyIdolProfileMigratedBirthdayCount
        {
            get { return LegacyIdolProfileMigration.MigratedBirthdayCount; }
        }

        public static long LegacyIdolProfileMigratedPeakAgeCount
        {
            get { return LegacyIdolProfileMigration.MigratedPeakAgeCount; }
        }

        public static long LegacyIdolProfilePhysicalPathUnavailableCount
        {
            get { return LegacyIdolProfileMigration.PhysicalPathUnavailableCount; }
        }

        public static long LegacyIdolProfileSkippedInvalidTargetCount
        {
            get { return LegacyIdolProfileMigration.SkippedInvalidTargetCount; }
        }

        public static string LastLegacyIdolProfileMigrationDiagnostic
        {
            get { return LegacyIdolProfileMigration.LastDiagnostic; }
        }

        public static bool LegacyAggregateFanMigrationImplemented
        {
            get { return LegacyAggregateFanMigration.IsImplemented; }
        }

        public static int LegacyAggregateFanMigrationResolvedTargetMethodCount
        {
            get { return LegacyAggregateFanMigrationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long LegacyAggregateFanObservedBranchCount
        {
            get { return LegacyAggregateFanMigration.ObservedLegacyBranchCount; }
        }

        public static long LegacyAggregateFanMigratedLoadCount
        {
            get { return LegacyAggregateFanMigration.MigratedLoadCount; }
        }

        public static long LegacyAggregateFanMigratedFanTotal
        {
            get { return LegacyAggregateFanMigration.MigratedFanTotal; }
        }

        public static long LegacyAggregateFanAlreadyModernSkipCount
        {
            get { return LegacyAggregateFanMigration.AlreadyModernSkipCount; }
        }

        public static long LegacyAggregateFanZeroAggregateSkipCount
        {
            get { return LegacyAggregateFanMigration.ZeroAggregateSkipCount; }
        }

        public static long LegacyAggregateFanInvalidTargetSkipCount
        {
            get { return LegacyAggregateFanMigration.InvalidTargetSkipCount; }
        }

        public static long LegacyAggregateFanPhysicalPathUnavailableCount
        {
            get { return LegacyAggregateFanMigration.PhysicalPathUnavailableCount; }
        }

        public static long LegacyAggregateFanExactTotalVerificationFailureCount
        {
            get { return LegacyAggregateFanMigration.ExactTotalVerificationFailureCount; }
        }

        public static string LastLegacyAggregateFanMigrationDiagnostic
        {
            get { return LegacyAggregateFanMigration.LastDiagnostic; }
        }

        public static bool LegacyRelationshipBootstrapMigrationImplemented
        {
            get { return LegacyRelationshipBootstrapMigration.IsImplemented; }
        }

        public static int LegacyRelationshipBootstrapMigrationResolvedTargetMethodCount
        {
            get { return LegacyRelationshipBootstrapMigrationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long LegacyRelationshipBootstrapObservedBootstrapCount
        {
            get { return LegacyRelationshipBootstrapMigration.ObservedBootstrapCount; }
        }

        public static long LegacyRelationshipBootstrapInitializedCount
        {
            get { return LegacyRelationshipBootstrapMigration.DeterministicallyInitializedCount; }
        }

        public static long LegacyRelationshipBootstrapPositiveCompatibilityCount
        {
            get { return LegacyRelationshipBootstrapMigration.PositiveCompatibilityCount; }
        }

        public static long LegacyRelationshipBootstrapDeterministicDomainCount
        {
            get { return LegacyRelationshipBootstrapMigration.DeterministicDomainCount; }
        }

        public static long LegacyRelationshipBootstrapPhysicalPathUnavailableCount
        {
            get { return LegacyRelationshipBootstrapMigration.PhysicalPathUnavailableCount; }
        }

        public static long LegacyRelationshipBootstrapInvalidPairCount
        {
            get { return LegacyRelationshipBootstrapMigration.InvalidPairCount; }
        }

        public static string LastLegacyRelationshipBootstrapMigrationDiagnostic
        {
            get { return LegacyRelationshipBootstrapMigration.LastDiagnostic; }
        }

        public static bool LegacyRivalBootstrapMigrationImplemented
        {
            get { return LegacyRivalBootstrapMigration.IsImplemented; }
        }

        public static int LegacyRivalBootstrapMigrationResolvedTargetMethodCount
        {
            get { return LegacyRivalBootstrapMigrationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long LegacyRivalBootstrapObservedLoadScopeCount
        {
            get { return LegacyRivalBootstrapMigration.ObservedLoadScopeCount; }
        }

        public static long LegacyRivalBootstrapEligibleLegacyLoadCount
        {
            get { return LegacyRivalBootstrapMigration.EligibleLegacyLoadCount; }
        }

        public static long LegacyRivalBootstrapGeneratedLoadCount
        {
            get { return LegacyRivalBootstrapMigration.DeterministicallyGeneratedLoadCount; }
        }

        public static long LegacyRivalBootstrapGeneratedTrendCount
        {
            get { return LegacyRivalBootstrapMigration.GeneratedTrendCount; }
        }

        public static long LegacyRivalBootstrapGeneratedGroupCount
        {
            get { return LegacyRivalBootstrapMigration.GeneratedGroupCount; }
        }

        public static long LegacyRivalBootstrapPhysicalPathUnavailableCount
        {
            get { return LegacyRivalBootstrapMigration.PhysicalPathUnavailableCount; }
        }

        public static long LegacyRivalBootstrapInvalidTargetCount
        {
            get { return LegacyRivalBootstrapMigration.InvalidTargetCount; }
        }

        public static string LastLegacyRivalBootstrapMigrationDiagnostic
        {
            get { return LegacyRivalBootstrapMigration.LastDiagnostic; }
        }

        public static bool WideNumericRepairImplemented
        {
            get { return WideNumericRepair.IsImplemented; }
        }

        public static bool WideNumericRepairHealthy
        {
            get { return WideNumericRepair.IsHealthy; }
        }

        public static bool WideNumericFailureLatched
        {
            get { return WideNumericRepair.HasLatchedFailure; }
        }

        public static int WideNumericExpectedTargetMethodCount
        {
            get { return WideNumericPatchHealth.ExpectedTargetMethodCount; }
        }

        public static int WideNumericResolvedTargetMethodCount
        {
            get { return WideNumericPatchHealth.ResolvedTargetMethodCount; }
        }

        public static int WideNumericContinuationExpectedTargetMethodCount
        {
            get { return WideNumericContinuationPatchHealth.ExpectedTargetMethodCount; }
        }

        public static int WideNumericContinuationResolvedTargetMethodCount
        {
            get { return WideNumericContinuationPatchHealth.ResolvedTargetMethodCount; }
        }

        public static long WideNumericRestoredSectionCount
        {
            get { return WideNumericState.RestoredSectionCount; }
        }

        public static long WideNumericLegacySeedCount
        {
            get { return WideNumericState.LegacySeedCount; }
        }

        public static string LastWideNumericStateDiagnostic
        {
            get { return WideNumericState.LastDiagnostic; }
        }

        public static long WideNumericCheckedOperationCount
        {
            get { return WideNumericRepair.CheckedOperationCount; }
        }

        public static long WideNumericOverflowFailureCount
        {
            get { return WideNumericRepair.OverflowFailureCount; }
        }

        public static long WideNumericInvariantFailureCount
        {
            get { return WideNumericRepair.InvariantFailureCount; }
        }

        public static long WideNumericBusinessHistoryCorrectionCount
        {
            get { return WideNumericRepair.BusinessHistoryCorrectionCount; }
        }

        public static string LastWideNumericDiagnostic
        {
            get
            {
                if (!string.IsNullOrEmpty(WideNumericPatchHealth.Failure))
                {
                    return WideNumericPatchHealth.Failure;
                }

                if (!string.IsNullOrEmpty(WideNumericContinuationPatchHealth.Failure))
                {
                    return WideNumericContinuationPatchHealth.Failure;
                }

                if (!string.IsNullOrEmpty(WideNumericRepair.LastDiagnostic))
                {
                    return WideNumericRepair.LastDiagnostic;
                }

                if (!string.IsNullOrEmpty(WideNumericState.LastDiagnostic))
                {
                    return WideNumericState.LastDiagnostic;
                }

                return WideNumericPatchHealth.LastDiagnostic;
            }
        }

    }
}
