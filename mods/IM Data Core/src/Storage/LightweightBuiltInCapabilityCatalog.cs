using System.Collections.Generic;

namespace IMDataCore
{
    /// <summary>
    /// Immutable semantic capability catalog for the first sidecar-v6 coverage generation.
    /// Built-in capability tokens intentionally equal the public durable event-type tokens;
    /// Revision is the semantic contract revision for that token, not the package version.
    /// Legacy v1-v5 history is never backfilled from this catalog: the first repaired v6
    /// activation begins at an exact runtime boundary. Future semantic changes must bump the
    /// affected token revision even when the event name and package version stay unchanged.
    /// </summary>
    internal static class LightweightBuiltInCapabilityCatalog
    {
        internal const int CatalogRevision = 1;
        internal const int QueryableDurableCapabilityCount = 173;

        internal static LightweightCoverageCapabilitySetRecord CreateCurrentDescriptor()
        {
            return LightweightCoverageSchema.CreateBuiltInCapabilitySet(
                BuildCurrentCapabilities());
        }

        internal static List<LightweightCoverageCapabilityRevisionRecord>
            BuildCurrentCapabilities()
        {
            List<LightweightCoverageCapabilityRevisionRecord> capabilities =
                new List<LightweightCoverageCapabilityRevisionRecord>
                {
                    Create(CoreConstants.EventTypeActivityLevelUp, CatalogRevision), // activity
                    Create(CoreConstants.EventTypeActivityPerformance, CatalogRevision), // activity
                    Create(CoreConstants.EventTypeActivityPromotion, CatalogRevision), // activity
                    Create(CoreConstants.EventTypeActivitySpaTreatment, CatalogRevision), // activity
                    Create(CoreConstants.EventTypeAgencyRoomBuilt, CatalogRevision), // agency
                    Create(CoreConstants.EventTypeAgencyRoomCostPaid, CatalogRevision), // agency
                    Create(CoreConstants.EventTypeAgencyRoomDestroyed, CatalogRevision), // agency
                    Create(CoreConstants.EventTypeAuditionCandidatesGenerated, CatalogRevision), // audition
                    Create(CoreConstants.EventTypeAuditionCompleted, CatalogRevision), // audition
                    Create(CoreConstants.EventTypeAuditionCooldownReset, CatalogRevision), // audition
                    Create(CoreConstants.EventTypeAuditionCostPaid, CatalogRevision), // audition
                    Create(CoreConstants.EventTypeAuditionFailureTriggered, CatalogRevision), // audition
                    Create(CoreConstants.EventTypeAuditionStarted, CatalogRevision), // audition
                    Create(CoreConstants.EventTypeAwardNominated, CatalogRevision), // award
                    Create(CoreConstants.EventTypeAwardResult, CatalogRevision), // award
                    Create(CoreConstants.EventTypeAwardSpeechDelivered, CatalogRevision), // award
                    Create(CoreConstants.EventTypeBankruptcyCheck, CatalogRevision), // bankruptcy
                    Create(CoreConstants.EventTypeBankruptcyDangerSet, CatalogRevision), // bankruptcy
                    Create(CoreConstants.EventTypeBullyingEnded, CatalogRevision), // bullying
                    Create(CoreConstants.EventTypeBullyingStarted, CatalogRevision), // bullying
                    Create(CoreConstants.EventTypeBusinessProposalAccepted, CatalogRevision), // business
                    Create(CoreConstants.EventTypeBusinessProposalDeclined, CatalogRevision), // business
                    Create(CoreConstants.EventTypeBusinessProposalGenerated, CatalogRevision), // business
                    Create(CoreConstants.EventTypeCafeCreated, CatalogRevision), // cafe
                    Create(CoreConstants.EventTypeCafeDailyResult, CatalogRevision), // cafe
                    Create(CoreConstants.EventTypeCafeDestroyed, CatalogRevision), // cafe
                    Create(CoreConstants.EventTypeCliqueCreated, CatalogRevision), // clique
                    Create(CoreConstants.EventTypeCliqueJoined, CatalogRevision), // clique
                    Create(CoreConstants.EventTypeCliqueLeft, CatalogRevision), // clique
                    Create(CoreConstants.EventTypeConcertCancelled, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertCardUsed, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertCastChanged, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertConfigurationChanged, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertCreated, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertCrisisApplied, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertCrisisDecision, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertFinalResolved, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertFinished, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertStarted, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeConcertStatusChanged, CatalogRevision), // concert
                    Create(CoreConstants.EventTypeContractAccepted, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractActivated, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractBroken, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractCancelled, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractFinished, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractWeeklyBenefitsApplied, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractWeeklyEarningsApplied, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeContractWindowOpened, CatalogRevision), // contract
                    Create(CoreConstants.EventTypeDatingPartnerStatusChanged, CatalogRevision), // dating
                    Create(CoreConstants.EventTypeEconomyDailyTick, CatalogRevision), // economy
                    Create(CoreConstants.EventTypeEconomyWeeklyExpenseApplied, CatalogRevision), // economy
                    Create(CoreConstants.EventTypeElectionCancelled, CatalogRevision), // election
                    Create(CoreConstants.EventTypeElectionCreated, CatalogRevision), // election
                    Create(CoreConstants.EventTypeElectionFinished, CatalogRevision), // election
                    Create(CoreConstants.EventTypeElectionPlaceAdjusted, CatalogRevision), // election
                    Create(CoreConstants.EventTypeElectionResultsGenerated, CatalogRevision), // election
                    Create(CoreConstants.EventTypeElectionStarted, CatalogRevision), // election
                    Create(CoreConstants.EventTypeElectionStatusChanged, CatalogRevision), // election
                    Create(CoreConstants.EventTypeGroupAppealPointsSpent, CatalogRevision), // group
                    Create(CoreConstants.EventTypeGroupCreated, CatalogRevision), // group
                    Create(CoreConstants.EventTypeGroupDisbanded, CatalogRevision), // group
                    Create(CoreConstants.EventTypeGroupParamPointsChanged, CatalogRevision), // group
                    Create(CoreConstants.EventTypeIdolDatingEnded, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolDatingStarted, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolDatingStatusChanged, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolGraduated, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolGraduationAnnounced, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolGraduationOutcome, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolGroupTransferred, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolHired, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolOutfitChanged, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolRelationshipCreated, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolRelationshipRemoved, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolRelationshipStatusChanged, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeIdolSalaryChanged, CatalogRevision), // idol
                    Create(CoreConstants.EventTypeInfluenceBlackmailDequeued, CatalogRevision), // influence
                    Create(CoreConstants.EventTypeInfluenceBlackmailQueued, CatalogRevision), // influence
                    Create(CoreConstants.EventTypeInfluenceBlackmailTriggered, CatalogRevision), // influence
                    Create(CoreConstants.EventTypeLoanAdded, CatalogRevision), // loan
                    Create(CoreConstants.EventTypeLoanCancelled, CatalogRevision), // loan
                    Create(CoreConstants.EventTypeLoanInitialized, CatalogRevision), // loan
                    Create(CoreConstants.EventTypeLoanMatured, CatalogRevision), // loan
                    Create(CoreConstants.EventTypeLoanPaidOff, CatalogRevision), // loan
                    Create(CoreConstants.EventTypeMedicalDepression, CatalogRevision), // medical
                    Create(CoreConstants.EventTypeMedicalHealed, CatalogRevision), // medical
                    Create(CoreConstants.EventTypeMedicalHiatusFinished, CatalogRevision), // medical
                    Create(CoreConstants.EventTypeMedicalHiatusStarted, CatalogRevision), // medical
                    Create(CoreConstants.EventTypeMedicalInjury, CatalogRevision), // medical
                    Create(CoreConstants.EventTypeMentorshipEnded, CatalogRevision), // mentorship
                    Create(CoreConstants.EventTypeMentorshipStarted, CatalogRevision), // mentorship
                    Create(CoreConstants.EventTypeMentorshipWeeklyTick, CatalogRevision), // mentorship
                    Create(CoreConstants.EventTypeMoneyLedgerCoverageStarted, CatalogRevision), // money
                    Create(CoreConstants.EventTypeMoneyTransaction, CatalogRevision), // money
                    Create(CoreConstants.EventTypePlayerBullyingIntervention, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerDateInteraction, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerFlirtOutcome, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerForcedBreakup, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerGenericDateCompleted, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerGenericDatePresented, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerMarriageOutcome, CatalogRevision), // player
                    Create(CoreConstants.EventTypePlayerRelationshipChanged, CatalogRevision), // player
                    Create(CoreConstants.EventTypePolicyDecisionSelected, CatalogRevision), // policy
                    Create(CoreConstants.EventTypePushWindowDayIncrement, CatalogRevision), // push
                    Create(CoreConstants.EventTypePushWindowEnded, CatalogRevision), // push
                    Create(CoreConstants.EventTypePushWindowStarted, CatalogRevision), // push
                    Create(CoreConstants.EventTypeRandomEventConcluded, CatalogRevision), // random
                    Create(CoreConstants.EventTypeRandomEventStarted, CatalogRevision), // random
                    Create(CoreConstants.EventTypeResearchParamAssigned, CatalogRevision), // research
                    Create(CoreConstants.EventTypeResearchParamLevelUp, CatalogRevision), // research
                    Create(CoreConstants.EventTypeResearchPointsPurchased, CatalogRevision), // research
                    Create(CoreConstants.EventTypeRivalGroupCreated, CatalogRevision), // rival
                    Create(CoreConstants.EventTypeRivalGroupRetired, CatalogRevision), // rival
                    Create(CoreConstants.EventTypeRivalMonthlyRecalculated, CatalogRevision), // rival
                    Create(CoreConstants.EventTypeRivalTrendsUpdated, CatalogRevision), // rival
                    Create(CoreConstants.EventTypeRoomWorkAssigned, CatalogRevision), // room
                    Create(CoreConstants.EventTypeRoomWorkCancelled, CatalogRevision), // room
                    Create(CoreConstants.EventTypeRoomWorkCompleted, CatalogRevision), // room
                    Create(CoreConstants.EventTypeScandalCheck, CatalogRevision), // scandal
                    Create(CoreConstants.EventTypeScandalMitigated, CatalogRevision), // scandal
                    Create(CoreConstants.EventTypeScandalPointsChanged, CatalogRevision), // scandal
                    Create(CoreConstants.EventTypeShowCancellationScheduled, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowCancellationWithdrawn, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowCancelled, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowCastChanged, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowConfigurationChanged, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowCreated, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowEpisodeReleased, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowRelaunchFinished, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowRelaunchStarted, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowReleased, CatalogRevision), // show
                    Create(CoreConstants.EventTypeShowStatusChanged, CatalogRevision), // show
                    Create(CoreConstants.EventTypeSingleCancelled, CatalogRevision), // single
                    Create(CoreConstants.EventTypeSingleCastChanged, CatalogRevision), // single
                    Create(CoreConstants.EventTypeSingleChartResult, CatalogRevision), // single
                    Create(CoreConstants.EventTypeSingleCreated, CatalogRevision), // single
                    Create(CoreConstants.EventTypeSingleGroupChanged, CatalogRevision), // single
                    Create(CoreConstants.EventTypeSingleReleased, CatalogRevision), // single
                    Create(CoreConstants.EventTypeSingleStatusChanged, CatalogRevision), // single
                    Create(CoreConstants.EventTypeStaffFired, CatalogRevision), // staff
                    Create(CoreConstants.EventTypeStaffFiredSeverance, CatalogRevision), // staff
                    Create(CoreConstants.EventTypeStaffHired, CatalogRevision), // staff
                    Create(CoreConstants.EventTypeStaffLevelUp, CatalogRevision), // staff
                    Create(CoreConstants.EventTypeStatusEnded, CatalogRevision), // status
                    Create(CoreConstants.EventTypeStatusStarted, CatalogRevision), // status
                    Create(CoreConstants.EventTypeStoryRouteLocked, CatalogRevision), // story
                    Create(CoreConstants.EventTypeSubstoryCompleted, CatalogRevision), // substory
                    Create(CoreConstants.EventTypeSubstoryDelayed, CatalogRevision), // substory
                    Create(CoreConstants.EventTypeSubstoryPresented, CatalogRevision), // substory
                    Create(CoreConstants.EventTypeSubstoryQueued, CatalogRevision), // substory
                    Create(CoreConstants.EventTypeSubstoryStarted, CatalogRevision), // substory
                    Create(CoreConstants.EventTypeSummerGamesFinalized, CatalogRevision), // summer
                    Create(CoreConstants.EventTypeSummerGamesObjectiveActivated, CatalogRevision), // summer
                    Create(CoreConstants.EventTypeTaskAdded, CatalogRevision), // task
                    Create(CoreConstants.EventTypeTaskCompleted, CatalogRevision), // task
                    Create(CoreConstants.EventTypeTaskDone, CatalogRevision), // task
                    Create(CoreConstants.EventTypeTaskFailed, CatalogRevision), // task
                    Create(CoreConstants.EventTypeTaskRemovedOnGraduation, CatalogRevision), // task
                    Create(CoreConstants.EventTypeTaskUnfulfilled, CatalogRevision), // task
                    Create(CoreConstants.EventTypeTemplateEventConcluded, CatalogRevision), // template
                    Create(CoreConstants.EventTypeTemplateEventPresented, CatalogRevision), // template
                    Create(CoreConstants.EventTypeTheaterCreated, CatalogRevision), // theater
                    Create(CoreConstants.EventTypeTheaterDailyResult, CatalogRevision), // theater
                    Create(CoreConstants.EventTypeTheaterDestroyed, CatalogRevision), // theater
                    Create(CoreConstants.EventTypeTourCancelled, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeTourCountryLevelUp, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeTourCountryResult, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeTourCreated, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeTourFinished, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeTourStarted, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeTourStatusChanged, CatalogRevision), // tour
                    Create(CoreConstants.EventTypeWishDone, CatalogRevision), // wish
                    Create(CoreConstants.EventTypeWishFulfilled, CatalogRevision), // wish
                    Create(CoreConstants.EventTypeWishGenerated, CatalogRevision), // wish
                };

            if (capabilities.Count != QueryableDurableCapabilityCount)
            {
                throw new System.InvalidOperationException(
                    "The built-in capability catalog count does not match the frozen Wave-4 contract.");
            }
            return capabilities;
        }

        private static LightweightCoverageCapabilityRevisionRecord Create(
            string token,
            int revision)
        {
            return new LightweightCoverageCapabilityRevisionRecord
            {
                Token = token ?? string.Empty,
                Revision = revision
            };
        }
    }
}
