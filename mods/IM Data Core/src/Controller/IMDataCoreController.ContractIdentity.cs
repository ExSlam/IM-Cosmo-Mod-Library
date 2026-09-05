using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Wave-1 contract-generation identity. The live 3.4.24 persistence writer is
    /// still v5/v2, so canonical contract EntityId emission is gated on the v6
    /// identity runtime. The allocation/binding/checkpoint machinery is complete
    /// here so the later v6 cutover cannot fall back to the legacy coarse key.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string ContractGenerationPrefix = "g:";
        private const string ContractIdentityWitnessPrefix = "sha256:";

        private sealed class ContractIdentityRuntimeBinding
        {
            internal string EntityId = string.Empty;
            internal long CoverageStartSequence;
            internal string Origin = LightweightIdentityBindingSchema.OriginNative;
            internal string LegacyCandidateKey = string.Empty;
        }

        private sealed class PendingContractGenerationFrame
        {
            internal business BusinessSystem;
            internal business._proposal SourceProposalReference;
            internal ContractAcceptedSnapshot Snapshot;
        }

        private readonly Dictionary<business.active_proposal, ContractIdentityRuntimeBinding>
            contractIdentityByReference =
                new Dictionary<business.active_proposal, ContractIdentityRuntimeBinding>();

        // A list rather than one ambient slot keeps nested/re-entrant Accept calls
        // independent. AddActiveProposal consumes only the latest matching frame.
        private readonly List<PendingContractGenerationFrame>
            pendingContractGenerationFrames =
                new List<PendingContractGenerationFrame>();

        private List<LightweightIdentityBindingRecord> pendingLoadedContractBindings;
        private bool pendingLoadedContractBindingsComplete;

        private static bool CanonicalContractIdentityRuntimeEnabled
        {
            get
            {
                return LightweightCoreStorageEngine.DurableV6RuntimeEnabled;
            }
        }

        private static string CreateContractGenerationId()
        {
            return ContractGenerationPrefix + Guid.NewGuid().ToString("N");
        }

        private long ResolveNextContractCoverageSequenceLocked()
        {
            return captureSequence == long.MaxValue
                ? long.MaxValue
                : captureSequence + 1L;
        }

        /// <summary>
        /// Reserves a generation before business.Accept enters nested
        /// AddActiveProposal. The source proposal reference is runtime-only and is
        /// used solely to match the nested call; the snapshot clone remains the
        /// immutable historical payload source.
        /// </summary>
        private void ReserveContractGenerationForAcceptanceLocked(
            business businessSystem,
            business._proposal sourceProposalReference,
            ContractAcceptedSnapshot snapshot)
        {
            if (snapshot == null || sourceProposalReference == null)
            {
                return;
            }

            snapshot.ContractGenerationId = CreateContractGenerationId();
            snapshot.ContractCoverageStartSequence =
                ResolveNextContractCoverageSequenceLocked();
            snapshot.SourceProposalReference = sourceProposalReference;

            string contractTypeCode =
                CoreEnumNameMapping.ToBusinessContractTypeCode(
                    sourceProposalReference.type);
            DateTime contractStartDate = snapshot.AcceptedDate;
            DateTime contractEndDate =
                sourceProposalReference.duration > CoreConstants.ZeroBasedListStartIndex
                    ? contractStartDate.AddMonths(sourceProposalReference.duration)
                    : contractStartDate;
            int idolId = sourceProposalReference.girl != null
                ? sourceProposalReference.girl.id
                : CoreConstants.InvalidIdValue;
            snapshot.LegacyCandidateKey = BuildContractEntityIdentifier(
                idolId,
                contractTypeCode,
                contractEndDate);

            pendingContractGenerationFrames.Add(
                new PendingContractGenerationFrame
                {
                    BusinessSystem = businessSystem,
                    SourceProposalReference = sourceProposalReference,
                    Snapshot = snapshot
                });
        }

        internal void CompleteContractAcceptanceIdentity(
            ContractAcceptedSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                for (int index = pendingContractGenerationFrames.Count - 1;
                    index >= CoreConstants.ZeroBasedListStartIndex;
                    index--)
                {
                    PendingContractGenerationFrame frame =
                        pendingContractGenerationFrames[index];
                    if (frame != null && ReferenceEquals(frame.Snapshot, snapshot))
                    {
                        pendingContractGenerationFrames.RemoveAt(index);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Binds the object inserted by AddActiveProposal. Nested Accept uses its
        /// reserved generation; a direct/modded insertion receives a fresh one.
        /// </summary>
        internal string BindContractActivationIdentity(
            business businessSystem,
            business.active_proposal activeContract,
            business._proposal sourceProposal)
        {
            if (activeContract == null)
            {
                return string.Empty;
            }

            lock (runtimeLock)
            {
                ContractIdentityRuntimeBinding existing;
                if (contractIdentityByReference.TryGetValue(
                        activeContract,
                        out existing) &&
                    existing != null &&
                    !string.IsNullOrEmpty(existing.EntityId))
                {
                    return existing.EntityId;
                }

                string generationId = string.Empty;
                long coverageStartSequence = ResolveNextContractCoverageSequenceLocked();
                string legacyCandidateKey = BuildLegacyContractCandidateKey(
                    activeContract);

                for (int index = pendingContractGenerationFrames.Count - 1;
                    index >= CoreConstants.ZeroBasedListStartIndex;
                    index--)
                {
                    PendingContractGenerationFrame frame =
                        pendingContractGenerationFrames[index];
                    if (frame == null ||
                        !ReferenceEquals(frame.BusinessSystem, businessSystem) ||
                        !ReferenceEquals(frame.SourceProposalReference, sourceProposal) ||
                        frame.Snapshot == null ||
                        string.IsNullOrEmpty(frame.Snapshot.ContractGenerationId))
                    {
                        continue;
                    }

                    generationId = frame.Snapshot.ContractGenerationId;
                    coverageStartSequence =
                        frame.Snapshot.ContractCoverageStartSequence;
                    if (!string.IsNullOrEmpty(frame.Snapshot.LegacyCandidateKey))
                    {
                        legacyCandidateKey = frame.Snapshot.LegacyCandidateKey;
                    }
                    break;
                }

                if (string.IsNullOrEmpty(generationId))
                {
                    generationId = CreateContractGenerationId();
                }

                contractIdentityByReference[activeContract] =
                    new ContractIdentityRuntimeBinding
                    {
                        EntityId = generationId,
                        CoverageStartSequence = coverageStartSequence,
                        Origin = LightweightIdentityBindingSchema.OriginNative,
                        LegacyCandidateKey = legacyCandidateKey
                    };
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindContract,
                    legacyCandidateKey,
                    CoreConstants.EventEntityKindContract,
                    generationId,
                    LightweightIdentityBindingSchema.CandidateSourceNativeBinding);
                return generationId;
            }
        }

        internal string ResolveContractHistoryEntityIdForMoney(business.active_proposal activeContract)
        {
            if (activeContract == null)
            {
                return string.Empty;
            }

            lock (runtimeLock)
            {
                int idolId = activeContract.Girl != null
                    ? activeContract.Girl.id
                    : CoreConstants.InvalidIdValue;
                return ResolveContractHistoryEntityIdentifierLocked(
                    activeContract,
                    idolId,
                    CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type),
                    activeContract.EndDate);
            }
        }

        private string ResolveContractHistoryEntityIdentifierLocked(
            business.active_proposal activeContract,
            int idolId,
            string contractTypeCode,
            DateTime contractEndDate)
        {
            if (CanonicalContractIdentityRuntimeEnabled && activeContract != null)
            {
                ContractIdentityRuntimeBinding binding;
                if (contractIdentityByReference.TryGetValue(
                        activeContract,
                        out binding) &&
                    binding != null &&
                    !string.IsNullOrEmpty(binding.EntityId))
                {
                    return binding.EntityId;
                }
            }

            return BuildContractEntityIdentifier(
                idolId,
                contractTypeCode,
                contractEndDate);
        }

        private static string ResolveAcceptedContractHistoryEntityIdentifier(
            ContractAcceptedSnapshot snapshot,
            int idolId,
            string contractTypeCode,
            DateTime contractEndDate)
        {
            if (CanonicalContractIdentityRuntimeEnabled &&
                snapshot != null &&
                !string.IsNullOrEmpty(snapshot.ContractGenerationId))
            {
                return snapshot.ContractGenerationId;
            }

            return BuildContractEntityIdentifier(
                idolId,
                contractTypeCode,
                contractEndDate);
        }

        private static string BuildLegacyContractCandidateKey(
            business.active_proposal activeContract)
        {
            if (activeContract == null)
            {
                return string.Empty;
            }

            int idolId = activeContract.Girl != null
                ? activeContract.Girl.id
                : CoreConstants.InvalidIdValue;
            return BuildContractEntityIdentifier(
                idolId,
                CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type),
                activeContract.EndDate);
        }

        private static string BuildLegacyContractCandidateKey(
            business.active_proposal_data savedRow)
        {
            if (savedRow == null)
            {
                return string.Empty;
            }

            DateTime endDate;
            try
            {
                endDate = ExtensionMethods.ToDateTime(savedRow.EndDate);
            }
            catch
            {
                return string.Empty;
            }

            return BuildContractEntityIdentifier(
                savedRow.Girl,
                CoreEnumNameMapping.ToBusinessContractTypeCode(savedRow.Type),
                endDate);
        }

        /// <summary>
        /// Builds the validation witness from exactly the fields vanilla persists
        /// in business.active_proposal_data. Ordinal + this witness is the exact
        /// checkpoint locator; neither is a gameplay-state restoration source.
        /// </summary>
        internal static string BuildContractIdentityValidationFingerprint(
            business.active_proposal_data savedRow)
        {
            if (savedRow == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(512);
            AppendContractWitnessField(builder, savedRow.isGroup ? "1" : "0");
            AppendContractWitnessField(
                builder,
                savedRow.Girl.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(
                builder,
                ((int)savedRow.Skill).ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(
                builder,
                ((int)savedRow.Type).ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.Payment_per_week.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.Buzz_per_week.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.Fame_per_week.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.Stamina_per_week.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.Fans_per_week.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.Agent_Name ?? string.Empty);
            AppendContractWitnessField(builder, savedRow.Product_Name ?? string.Empty);
            AppendContractWitnessField(builder, savedRow.Liability.ToString(CultureInfo.InvariantCulture));
            AppendContractWitnessField(builder, savedRow.EndDate ?? string.Empty);

            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                return ContractIdentityWitnessPrefix +
                    ToLowerHexForContractIdentity(sha256.ComputeHash(bytes));
            }
        }

        private static void AppendContractWitnessField(
            StringBuilder builder,
            string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append('|');
        }

        private static string ToLowerHexForContractIdentity(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static bool ContractSavedRowMatchesLive(
            business.active_proposal_data savedRow,
            business.active_proposal liveContract)
        {
            if (savedRow == null || liveContract == null)
            {
                return false;
            }

            int liveGirlId = liveContract.Girl != null
                ? liveContract.Girl.id
                : CoreConstants.InvalidIdValue;
            if (savedRow.isGroup != liveContract.isGroup ||
                savedRow.Girl != liveGirlId ||
                savedRow.Skill != liveContract.Skill ||
                savedRow.Type != liveContract.Type ||
                savedRow.Payment_per_week != liveContract.Payment_per_week ||
                savedRow.Buzz_per_week != liveContract.Buzz_per_week ||
                savedRow.Fame_per_week != liveContract.Fame_per_week ||
                savedRow.Stamina_per_week != liveContract.Stamina_per_week ||
                savedRow.Fans_per_week != liveContract.Fans_per_week ||
                savedRow.Liability != liveContract.Liability ||
                !string.Equals(savedRow.Agent_Name ?? string.Empty, liveContract.Agent_Name ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(savedRow.Product_Name ?? string.Empty, liveContract.Product_Name ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            DateTime savedEndDate;
            try
            {
                savedEndDate = ExtensionMethods.ToDateTime(savedRow.EndDate);
            }
            catch
            {
                return false;
            }
            return savedEndDate == liveContract.EndDate;
        }

        internal List<LightweightIdentityBindingRecord>
            CaptureContractIdentityBindingsForCheckpointLocked(
                SaveManager.SavedData savedData)
        {
            List<LightweightIdentityBindingRecord> result =
                new List<LightweightIdentityBindingRecord>();
            if (!CanonicalContractIdentityRuntimeEnabled ||
                savedData == null ||
                savedData.business__ActiveProposalsData == null)
            {
                return result;
            }

            business businessSystem = ResolveBusinessSystemForContractIdentity();
            if (businessSystem == null || businessSystem.ActiveProposals == null ||
                businessSystem.ActiveProposals.Count !=
                    savedData.business__ActiveProposalsData.Count)
            {
                return result;
            }

            for (int ordinal = CoreConstants.ZeroBasedListStartIndex;
                ordinal < savedData.business__ActiveProposalsData.Count;
                ordinal++)
            {
                business.active_proposal_data savedRow =
                    savedData.business__ActiveProposalsData[ordinal];
                business.active_proposal liveContract =
                    businessSystem.ActiveProposals[ordinal];
                if (!ContractSavedRowMatchesLive(savedRow, liveContract))
                {
                    // Never bind the right generation to the wrong saved row.
                    continue;
                }

                ContractIdentityRuntimeBinding runtimeBinding;
                if (!contractIdentityByReference.TryGetValue(
                        liveContract,
                        out runtimeBinding) ||
                    runtimeBinding == null ||
                    string.IsNullOrEmpty(runtimeBinding.EntityId))
                {
                    runtimeBinding = new ContractIdentityRuntimeBinding
                    {
                        EntityId = CreateContractGenerationId(),
                        CoverageStartSequence = ResolveNextContractCoverageSequenceLocked(),
                        Origin = LightweightIdentityBindingSchema.OriginNative,
                        LegacyCandidateKey = BuildLegacyContractCandidateKey(savedRow)
                    };
                    contractIdentityByReference[liveContract] = runtimeBinding;
                }

                List<string> legacyCandidates = new List<string>();
                string legacyCandidateKey = !string.IsNullOrEmpty(runtimeBinding.LegacyCandidateKey)
                    ? runtimeBinding.LegacyCandidateKey
                    : BuildLegacyContractCandidateKey(savedRow);
                if (!string.IsNullOrEmpty(legacyCandidateKey))
                {
                    legacyCandidates.Add(legacyCandidateKey);
                }

                result.Add(new LightweightIdentityBindingRecord
                {
                    EntityKind = CoreConstants.EventEntityKindContract,
                    EntityId = runtimeBinding.EntityId,
                    ContainerKind = LightweightIdentityBindingSchema.ContainerBusinessActiveProposals,
                    ContainerOrdinal = ordinal,
                    ParentEntityKind = string.Empty,
                    ParentEntityId = string.Empty,
                    ChildLocator = string.Empty,
                    ValidationFingerprint = BuildContractIdentityValidationFingerprint(savedRow),
                    Origin = runtimeBinding.Origin,
                    CoverageStartSequence = Math.Min(runtimeBinding.CoverageStartSequence, captureSequence),
                    LegacyCandidateKeys = legacyCandidates
                });
            }

            return result;
        }

        internal void PrepareContractIdentityBindingsForLoad(
            bool exactCheckpointSelected,
            bool identityBindingsComplete,
            IReadOnlyList<LightweightIdentityBindingRecord> identityBindings)
        {
            lock (runtimeLock)
            {
                contractIdentityByReference.Clear();
                pendingContractGenerationFrames.Clear();
                pendingLoadedContractBindings = null;
                pendingLoadedContractBindingsComplete = false;

                if (!CanonicalContractIdentityRuntimeEnabled ||
                    !exactCheckpointSelected ||
                    !identityBindingsComplete ||
                    identityBindings == null)
                {
                    return;
                }

                List<LightweightIdentityBindingRecord> contracts =
                    new List<LightweightIdentityBindingRecord>();
                for (int index = 0; index < identityBindings.Count; index++)
                {
                    LightweightIdentityBindingRecord binding = identityBindings[index];
                    if (binding != null &&
                        string.Equals(
                            binding.EntityKind,
                            CoreConstants.EventEntityKindContract,
                            StringComparison.Ordinal))
                    {
                        contracts.Add(CloneContractIdentityBinding(binding));
                    }
                }

                pendingLoadedContractBindings = contracts;
                pendingLoadedContractBindingsComplete = true;
            }
        }

        internal void AssociateLoadedContractIdentities(business businessSystem)
        {
            if (!CanonicalContractIdentityRuntimeEnabled || businessSystem == null)
            {
                return;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForContractIdentity();
            lock (runtimeLock)
            {
                if (TryAdoptLoadedContractIdentitiesLocked(businessSystem))
                {
                    pendingLoadedContractBindings = null;
                    pendingLoadedContractBindingsComplete = false;
                    return;
                }

                if (!pendingLoadedContractBindingsComplete ||
                    pendingLoadedContractBindings == null ||
                    savedData == null ||
                    savedData.business__ActiveProposalsData == null ||
                    businessSystem.ActiveProposals == null)
                {
                    return;
                }

                for (int index = 0; index < pendingLoadedContractBindings.Count; index++)
                {
                    LightweightIdentityBindingRecord binding =
                        pendingLoadedContractBindings[index];
                    if (binding == null ||
                        binding.ContainerOrdinal < CoreConstants.ZeroBasedListStartIndex ||
                        binding.ContainerOrdinal >= savedData.business__ActiveProposalsData.Count ||
                        binding.ContainerOrdinal >= businessSystem.ActiveProposals.Count)
                    {
                        continue;
                    }

                    business.active_proposal_data savedRow =
                        savedData.business__ActiveProposalsData[binding.ContainerOrdinal];
                    business.active_proposal liveContract =
                        businessSystem.ActiveProposals[binding.ContainerOrdinal];
                    string expectedFingerprint =
                        BuildContractIdentityValidationFingerprint(savedRow);
                    if (!string.Equals(
                            expectedFingerprint,
                            binding.ValidationFingerprint,
                            StringComparison.Ordinal) ||
                        !ContractSavedRowMatchesLive(savedRow, liveContract))
                    {
                        continue;
                    }

                    contractIdentityByReference[liveContract] =
                        new ContractIdentityRuntimeBinding
                        {
                            EntityId = binding.EntityId ?? string.Empty,
                            CoverageStartSequence = binding.CoverageStartSequence,
                            Origin = binding.Origin ?? string.Empty,
                            LegacyCandidateKey = binding.LegacyCandidateKeys != null &&
                                binding.LegacyCandidateKeys.Count > 0
                                    ? binding.LegacyCandidateKeys[0]
                                    : string.Empty
                        };
                }

                pendingLoadedContractBindings = null;
                pendingLoadedContractBindingsComplete = false;
            }
        }

        internal void RetireContractIdentityIfRemoved(
            business businessSystem,
            business.active_proposal activeContract)
        {
            if (activeContract == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                if (businessSystem != null &&
                    businessSystem.ActiveProposals != null &&
                    businessSystem.ActiveProposals.Contains(activeContract))
                {
                    return;
                }
                contractIdentityByReference.Remove(activeContract);
            }
        }

        private void RetireContractIdentityLocked(
            business.active_proposal activeContract)
        {
            if (activeContract != null)
            {
                contractIdentityByReference.Remove(activeContract);
            }
        }

        private static LightweightIdentityBindingRecord CloneContractIdentityBinding(
            LightweightIdentityBindingRecord source)
        {
            return new LightweightIdentityBindingRecord
            {
                EntityKind = source.EntityKind ?? string.Empty,
                EntityId = source.EntityId ?? string.Empty,
                ContainerKind = source.ContainerKind ?? string.Empty,
                ContainerOrdinal = source.ContainerOrdinal,
                ParentEntityKind = source.ParentEntityKind ?? string.Empty,
                ParentEntityId = source.ParentEntityId ?? string.Empty,
                ChildLocator = source.ChildLocator ?? string.Empty,
                ValidationFingerprint = source.ValidationFingerprint ?? string.Empty,
                Origin = source.Origin ?? string.Empty,
                CoverageStartSequence = source.CoverageStartSequence,
                LegacyCandidateKeys = source.LegacyCandidateKeys != null
                    ? new List<string>(source.LegacyCandidateKeys)
                    : new List<string>()
            };
        }

        private static business ResolveBusinessSystemForContractIdentity()
        {
            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            return main != null && main.Data != null
                ? main.Data.GetComponent<business>()
                : null;
        }

        private static SaveManager.SavedData ResolveCurrentSavedDataForContractIdentity()
        {
            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            return main != null ? main.GetSavedData() : null;
        }

        private void ResetContractIdentityRuntimeStateLocked()
        {
            contractIdentityByReference.Clear();
            pendingContractGenerationFrames.Clear();
            pendingLoadedContractBindings = null;
            pendingLoadedContractBindingsComplete = false;
        }
    }
}
