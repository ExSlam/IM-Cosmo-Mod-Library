using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Shared Area-#12 compatibility layer over the four opaque-generation
    /// families plus room-work owner namespaces. Candidate metadata is branch
    /// state: exact checkpoint selection replaces it wholesale, so F9 cannot
    /// leak identities from a discarded branch.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private readonly List<LightweightIdentityCandidateRecord>
            identityCandidatesForCurrentBranch =
                new List<LightweightIdentityCandidateRecord>();

        private VanillaSaveStamp pendingIdentityAdoptionStamp;
        private long pendingIdentityAdoptionSequence;
        private bool pendingLegacyUnboundIdentityAdoption;

        private static LightweightIdentityCandidateRecord CloneIdentityCandidate(
            LightweightIdentityCandidateRecord source)
        {
            return new LightweightIdentityCandidateRecord
            {
                LegacyEntityKind = source.LegacyEntityKind ?? string.Empty,
                LegacyEntityId = source.LegacyEntityId ?? string.Empty,
                CanonicalEntityKind = source.CanonicalEntityKind ?? string.Empty,
                CanonicalEntityId = source.CanonicalEntityId ?? string.Empty,
                ExactAlias = source.ExactAlias,
                ExactSequenceStartInclusive = source.ExactSequenceStartInclusive,
                ExactSequenceEndInclusive = source.ExactSequenceEndInclusive,
                SourceKind = source.SourceKind ?? string.Empty
            };
        }

        private void RegisterIdentityCandidateLocked(
            string legacyEntityKind,
            string legacyEntityId,
            string canonicalEntityKind,
            string canonicalEntityId,
            string sourceKind)
        {
            if (string.IsNullOrEmpty(legacyEntityKind) ||
                string.IsNullOrEmpty(legacyEntityId) ||
                string.IsNullOrEmpty(canonicalEntityKind) ||
                string.IsNullOrEmpty(canonicalEntityId))
            {
                return;
            }

            for (int index = 0; index < identityCandidatesForCurrentBranch.Count; index++)
            {
                LightweightIdentityCandidateRecord existing =
                    identityCandidatesForCurrentBranch[index];
                if (existing != null &&
                    string.Equals(existing.LegacyEntityKind, legacyEntityKind, StringComparison.Ordinal) &&
                    string.Equals(existing.LegacyEntityId, legacyEntityId, StringComparison.Ordinal) &&
                    string.Equals(existing.CanonicalEntityKind, canonicalEntityKind, StringComparison.Ordinal) &&
                    string.Equals(existing.CanonicalEntityId, canonicalEntityId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            identityCandidatesForCurrentBranch.Add(
                new LightweightIdentityCandidateRecord
                {
                    LegacyEntityKind = legacyEntityKind,
                    LegacyEntityId = legacyEntityId,
                    CanonicalEntityKind = canonicalEntityKind,
                    CanonicalEntityId = canonicalEntityId,
                    ExactAlias = false,
                    ExactSequenceStartInclusive =
                        LightweightIdentityBindingSchema.NoExactAliasSequence,
                    ExactSequenceEndInclusive =
                        LightweightIdentityBindingSchema.NoExactAliasSequence,
                    SourceKind = sourceKind ?? string.Empty
                });
        }

        private void MergeIdentityCandidatesFromBindingsLocked(
            IReadOnlyList<LightweightIdentityBindingRecord> bindings)
        {
            if (bindings == null)
            {
                return;
            }
            for (int index = 0; index < bindings.Count; index++)
            {
                LightweightIdentityBindingRecord binding = bindings[index];
                if (binding == null || binding.LegacyCandidateKeys == null)
                {
                    continue;
                }
                string sourceKind = string.Equals(
                    binding.Origin,
                    LightweightIdentityBindingSchema.OriginMigrationAdopted,
                    StringComparison.Ordinal)
                        ? LightweightIdentityBindingSchema.CandidateSourceMigrationAdoption
                        : LightweightIdentityBindingSchema.CandidateSourceNativeBinding;
                for (int candidateIndex = 0;
                    candidateIndex < binding.LegacyCandidateKeys.Count;
                    candidateIndex++)
                {
                    RegisterIdentityCandidateLocked(
                        binding.EntityKind,
                        binding.LegacyCandidateKeys[candidateIndex],
                        binding.EntityKind,
                        binding.EntityId,
                        sourceKind);
                }
            }
        }

        private List<LightweightIdentityCandidateRecord>
            CaptureIdentityCandidateSnapshotLocked(
                IReadOnlyList<LightweightIdentityBindingRecord> bindings)
        {
            MergeIdentityCandidatesFromBindingsLocked(bindings);
            List<LightweightIdentityCandidateRecord> result =
                new List<LightweightIdentityCandidateRecord>(
                    identityCandidatesForCurrentBranch.Count);
            for (int index = 0; index < identityCandidatesForCurrentBranch.Count; index++)
            {
                LightweightIdentityCandidateRecord record =
                    identityCandidatesForCurrentBranch[index];
                if (record != null)
                {
                    result.Add(CloneIdentityCandidate(record));
                }
            }
            return result;
        }

        internal void PrepareSharedIdentityCompatibilityForLoad(
            bool exactCheckpointSelected,
            bool identityBindingsComplete,
            VanillaSaveStamp stamp,
            long checkpointSequence,
            IReadOnlyList<LightweightIdentityCandidateRecord> candidates)
        {
            lock (runtimeLock)
            {
                identityCandidatesForCurrentBranch.Clear();
                pendingIdentityAdoptionStamp = null;
                pendingIdentityAdoptionSequence = 0L;
                pendingLegacyUnboundIdentityAdoption = false;

                if (!exactCheckpointSelected)
                {
                    return;
                }

                if (candidates != null)
                {
                    for (int index = 0; index < candidates.Count; index++)
                    {
                        LightweightIdentityCandidateRecord record = candidates[index];
                        if (record != null)
                        {
                            identityCandidatesForCurrentBranch.Add(
                                CloneIdentityCandidate(record));
                        }
                    }
                }

                if (!identityBindingsComplete && stamp != null)
                {
                    pendingIdentityAdoptionStamp = stamp;
                    pendingIdentityAdoptionSequence = checkpointSequence;
                    pendingLegacyUnboundIdentityAdoption = true;
                }
            }
        }

        private bool TryGetIdentityAdoptionAnchorLocked(
            out VanillaSaveStamp stamp,
            out long checkpointSequence)
        {
            stamp = pendingIdentityAdoptionStamp;
            checkpointSequence = pendingIdentityAdoptionSequence;
            return pendingLegacyUnboundIdentityAdoption && stamp != null;
        }

        private void ClearIdentityAdoptionContextLocked()
        {
            pendingIdentityAdoptionStamp = null;
            pendingIdentityAdoptionSequence = 0L;
            pendingLegacyUnboundIdentityAdoption = false;
        }

        private void ResetSharedIdentityCompatibilityRuntimeStateLocked()
        {
            identityCandidatesForCurrentBranch.Clear();
            ClearIdentityAdoptionContextLocked();
        }

        internal void RegisterRoomWorkLegacyCandidate(
            string roomGenerationId,
            string ownerKind,
            string ownerId)
        {
            if (!(string.Equals(ownerKind, "ssk", StringComparison.Ordinal) ||
                string.Equals(ownerKind, "tour", StringComparison.Ordinal)) ||
                string.IsNullOrEmpty(roomGenerationId) ||
                string.IsNullOrEmpty(ownerId))
            {
                return;
            }

            lock (runtimeLock)
            {
                string legacyId = string.Concat(
                    roomGenerationId,
                    ":event:",
                    ownerId);
                string canonicalId = string.Concat(
                    roomGenerationId,
                    ":",
                    ownerKind,
                    ":",
                    ownerId);
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindRoomWork,
                    legacyId,
                    CoreConstants.EventEntityKindRoomWork,
                    canonicalId,
                    LightweightIdentityBindingSchema.CandidateSourceRoomWorkNamespace);
            }
        }

        private bool TryAdoptLoadedContractIdentitiesLocked(business businessSystem)
        {
            VanillaSaveStamp stamp;
            long checkpointSequence;
            if (!TryGetIdentityAdoptionAnchorLocked(out stamp, out checkpointSequence) ||
                businessSystem == null)
            {
                return false;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForContractIdentity();
            if (savedData == null || savedData.business__ActiveProposalsData == null ||
                businessSystem.ActiveProposals == null)
            {
                return true;
            }

            int count = Math.Min(
                savedData.business__ActiveProposalsData.Count,
                businessSystem.ActiveProposals.Count);
            for (int ordinal = 0; ordinal < count; ordinal++)
            {
                business.active_proposal_data savedRow =
                    savedData.business__ActiveProposalsData[ordinal];
                business.active_proposal liveContract =
                    businessSystem.ActiveProposals[ordinal];
                if (savedRow == null || !ContractSavedRowMatchesLive(savedRow, liveContract))
                {
                    continue;
                }

                string fingerprint = BuildContractIdentityValidationFingerprint(savedRow);
                string entityId = LightweightIdentityBindingSchema.CreateDeterministicAdoptedEntityId(
                    ContractGenerationPrefix,
                    stamp,
                    checkpointSequence,
                    CoreConstants.EventEntityKindContract,
                    LightweightIdentityBindingSchema.ContainerBusinessActiveProposals,
                    ordinal,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    fingerprint);
                string legacyCandidate = BuildLegacyContractCandidateKey(savedRow);
                contractIdentityByReference[liveContract] = new ContractIdentityRuntimeBinding
                {
                    EntityId = entityId,
                    CoverageStartSequence = checkpointSequence,
                    Origin = LightweightIdentityBindingSchema.OriginMigrationAdopted,
                    LegacyCandidateKey = legacyCandidate
                };
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindContract,
                    legacyCandidate,
                    CoreConstants.EventEntityKindContract,
                    entityId,
                    LightweightIdentityBindingSchema.CandidateSourceMigrationAdoption);
            }
            return true;
        }

        private bool TryAdoptLoadedCliqueIdentitiesLocked()
        {
            VanillaSaveStamp stamp;
            long checkpointSequence;
            if (!TryGetIdentityAdoptionAnchorLocked(out stamp, out checkpointSequence))
            {
                return false;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForCliqueIdentity();
            if (savedData == null || savedData.Relationships__Cliques == null ||
                Relationships.Cliques == null)
            {
                return true;
            }

            int count = Math.Min(savedData.Relationships__Cliques.Count, Relationships.Cliques.Count);
            for (int ordinal = 0; ordinal < count; ordinal++)
            {
                Relationships.SaveData_Clique savedRow = savedData.Relationships__Cliques[ordinal];
                Relationships._clique liveClique = Relationships.Cliques[ordinal];
                if (savedRow == null || !CliqueSavedRowMatchesLive(savedRow, liveClique))
                {
                    continue;
                }

                string fingerprint = BuildCliqueIdentityValidationFingerprint(savedRow);
                string entityId = LightweightIdentityBindingSchema.CreateDeterministicAdoptedEntityId(
                    CliqueGenerationPrefix,
                    stamp,
                    checkpointSequence,
                    CoreConstants.EventEntityKindClique,
                    LightweightIdentityBindingSchema.ContainerRelationshipsCliques,
                    ordinal,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    fingerprint);
                string legacyCandidate = BuildCliqueLegacyCandidateKey(savedRow);
                cliqueIdentityByReference[liveClique] = new CliqueIdentityRuntimeBinding
                {
                    EntityId = entityId,
                    CoverageStartSequence = checkpointSequence,
                    Origin = LightweightIdentityBindingSchema.OriginMigrationAdopted,
                    LegacyCandidateKey = legacyCandidate
                };
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindClique,
                    legacyCandidate,
                    CoreConstants.EventEntityKindClique,
                    entityId,
                    LightweightIdentityBindingSchema.CandidateSourceMigrationAdoption);
            }
            return true;
        }

        private bool TryAdoptLoadedBullyingIdentitiesLocked()
        {
            VanillaSaveStamp stamp;
            long checkpointSequence;
            if (!TryGetIdentityAdoptionAnchorLocked(out stamp, out checkpointSequence))
            {
                return false;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForCliqueIdentity();
            if (savedData == null || savedData.Relationships__Cliques == null ||
                Relationships.Cliques == null)
            {
                return true;
            }

            int count = Math.Min(savedData.Relationships__Cliques.Count, Relationships.Cliques.Count);
            for (int cliqueOrdinal = 0; cliqueOrdinal < count; cliqueOrdinal++)
            {
                Relationships.SaveData_Clique savedRow = savedData.Relationships__Cliques[cliqueOrdinal];
                Relationships._clique liveClique = Relationships.Cliques[cliqueOrdinal];
                if (savedRow == null || savedRow.Bullied_Girls == null ||
                    !CliqueSavedRowMatchesLive(savedRow, liveClique))
                {
                    continue;
                }

                CliqueIdentityRuntimeBinding parentBinding;
                if (!cliqueIdentityByReference.TryGetValue(liveClique, out parentBinding) ||
                    parentBinding == null || string.IsNullOrEmpty(parentBinding.EntityId))
                {
                    continue;
                }

                Dictionary<int, BullyingIdentityRuntimeBinding> byTarget =
                    new Dictionary<int, BullyingIdentityRuntimeBinding>();
                HashSet<int> seenTargets = new HashSet<int>();
                for (int targetIndex = 0; targetIndex < savedRow.Bullied_Girls.Count; targetIndex++)
                {
                    int targetId = savedRow.Bullied_Girls[targetIndex];
                    if (targetId < CoreConstants.MinimumValidIdolIdentifier || !seenTargets.Add(targetId))
                    {
                        continue;
                    }
                    data_girls.girls liveTarget = ResolveLiveBulliedTarget(liveClique, targetId);
                    if (liveTarget == null)
                    {
                        continue;
                    }

                    string childLocator = BuildBullyingChildLocator(targetId);
                    string fingerprint = BuildBullyingIdentityValidationFingerprint(savedRow, targetId);
                    string entityId = LightweightIdentityBindingSchema.CreateDeterministicAdoptedEntityId(
                        BullyingEpisodeGenerationPrefix,
                        stamp,
                        checkpointSequence,
                        CoreConstants.EventEntityKindBullying,
                        LightweightIdentityBindingSchema.ContainerRelationshipsCliques,
                        cliqueOrdinal,
                        CoreConstants.EventEntityKindClique,
                        parentBinding.EntityId,
                        childLocator,
                        fingerprint);
                    string legacyCandidate = BuildBullyingEntityIdentifier(savedRow.Leader, targetId);
                    BullyingIdentityRuntimeBinding runtimeBinding = new BullyingIdentityRuntimeBinding
                    {
                        EntityId = entityId,
                        ParentCliqueEntityId = parentBinding.EntityId,
                        TargetId = targetId,
                        CoverageStartSequence = checkpointSequence,
                        Origin = LightweightIdentityBindingSchema.OriginMigrationAdopted
                    };
                    AddBullyingLegacyCandidateKey(runtimeBinding, legacyCandidate);
                    byTarget[targetId] = runtimeBinding;
                    RegisterIdentityCandidateLocked(
                        CoreConstants.EventEntityKindBullying,
                        legacyCandidate,
                        CoreConstants.EventEntityKindBullying,
                        entityId,
                        LightweightIdentityBindingSchema.CandidateSourceMigrationAdoption);
                }
                if (byTarget.Count > 0)
                {
                    bullyingIdentityByCliqueAndTargetId[liveClique] = byTarget;
                }
            }
            return true;
        }

        private bool TryAdoptLoadedTaskIdentitiesLocked()
        {
            VanillaSaveStamp stamp;
            long checkpointSequence;
            if (!TryGetIdentityAdoptionAnchorLocked(out stamp, out checkpointSequence))
            {
                return false;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForTaskIdentity();
            if (savedData == null || savedData.tasks__TaskData == null || tasks.ActiveTasks == null)
            {
                return true;
            }

            int count = Math.Min(savedData.tasks__TaskData.Count, tasks.ActiveTasks.Count);
            for (int ordinal = 0; ordinal < count; ordinal++)
            {
                tasks.TaskData savedRow = savedData.tasks__TaskData[ordinal];
                tasks._task liveTask = tasks.ActiveTasks[ordinal];
                if (savedRow == null || !string.IsNullOrEmpty(savedRow.Custom ?? string.Empty) ||
                    !TaskSavedRowMatchesLive(savedRow, liveTask))
                {
                    continue;
                }

                string fingerprint = BuildTaskIdentityValidationFingerprint(savedRow);
                string entityId = LightweightIdentityBindingSchema.CreateDeterministicAdoptedEntityId(
                    TaskOccurrenceGenerationPrefix,
                    stamp,
                    checkpointSequence,
                    CoreConstants.EventEntityKindTask,
                    LightweightIdentityBindingSchema.ContainerTasksTaskData,
                    ordinal,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    fingerprint);
                string legacyCandidate = BuildTaskLegacyCandidateKey(savedRow);
                generatedTaskIdentityByReference[liveTask] = new TaskIdentityRuntimeBinding
                {
                    EntityId = entityId,
                    CoverageStartSequence = checkpointSequence,
                    Origin = LightweightIdentityBindingSchema.OriginMigrationAdopted,
                    LegacyCandidateKey = legacyCandidate
                };
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindTask,
                    legacyCandidate,
                    CoreConstants.EventEntityKindTask,
                    entityId,
                    LightweightIdentityBindingSchema.CandidateSourceMigrationAdoption);
            }
            return true;
        }

        internal bool TryResolveLegacyIdentityCandidates(
            string entityKind,
            string legacyEntityId,
            out IMDataCoreIdentityResolution resolution,
            out string errorMessage)
        {
            resolution = new IMDataCoreIdentityResolution
            {
                EntityKind = entityKind ?? string.Empty,
                RequestedKey = legacyEntityId ?? string.Empty,
                Quality = IMDataCoreIdentityResolutionQuality.Unresolved
            };
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(entityKind) || string.IsNullOrEmpty(legacyEntityId))
            {
                errorMessage = "The identity kind and legacy key are required.";
                return false;
            }

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                // Candidate links become durable checkpoint-owned branch state only
                // in sidecar v6. The live v5 runtime must not expose transient
                // process-local observations as if they survived restart/F9.
                if (!LightweightCoreStorageEngine.DurableV6RuntimeEnabled)
                {
                    return true;
                }

                HashSet<string> candidates = new HashSet<string>(StringComparer.Ordinal);
                HashSet<string> exactCandidates = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < identityCandidatesForCurrentBranch.Count; index++)
                {
                    LightweightIdentityCandidateRecord record =
                        identityCandidatesForCurrentBranch[index];
                    if (record == null ||
                        !string.Equals(record.LegacyEntityKind, entityKind, StringComparison.Ordinal) ||
                        !string.Equals(record.LegacyEntityId, legacyEntityId, StringComparison.Ordinal) ||
                        !string.Equals(record.CanonicalEntityKind, entityKind, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    string canonicalCandidate = record.CanonicalEntityId ?? string.Empty;
                    if (string.IsNullOrEmpty(canonicalCandidate))
                    {
                        continue;
                    }
                    candidates.Add(canonicalCandidate);
                    if (record.ExactAlias &&
                        captureSequence >= record.ExactSequenceStartInclusive &&
                        captureSequence <= record.ExactSequenceEndInclusive)
                    {
                        exactCandidates.Add(canonicalCandidate);
                    }
                }

                foreach (string candidate in candidates)
                {
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        resolution.CanonicalEntityIds.Add(candidate);
                    }
                }
                resolution.CanonicalEntityIds.Sort(StringComparer.Ordinal);

                if (exactCandidates.Count == 1)
                {
                    foreach (string exactCandidate in exactCandidates)
                    {
                        if (!string.IsNullOrEmpty(exactCandidate) &&
                            resolution.CanonicalEntityIds.Contains(exactCandidate))
                        {
                            resolution.Quality = IMDataCoreIdentityResolutionQuality.Exact;
                            resolution.CanonicalEntityId = exactCandidate;
                        }
                        break;
                    }
                }
                else if (resolution.CanonicalEntityIds.Count > 0)
                {
                    resolution.Quality = IMDataCoreIdentityResolutionQuality.Ambiguous;
                }
                return true;
            }
        }

        internal bool TryResolveCurrentIdentity(
            string entityKind,
            string locator,
            out IMDataCoreIdentityResolution resolution,
            out string errorMessage)
        {
            resolution = new IMDataCoreIdentityResolution
            {
                EntityKind = entityKind ?? string.Empty,
                RequestedKey = locator ?? string.Empty,
                Quality = IMDataCoreIdentityResolutionQuality.Unresolved
            };
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(entityKind) || string.IsNullOrEmpty(locator))
            {
                errorMessage = "The identity kind and stable locator are required.";
                return false;
            }

            Dictionary<string, string> parts = ParseIdentityLocator(locator);
            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                string canonicalId = ResolveCurrentIdentityLocked(entityKind, parts);
                if (!string.IsNullOrEmpty(canonicalId))
                {
                    resolution.Quality = IMDataCoreIdentityResolutionQuality.Exact;
                    resolution.CanonicalEntityId = canonicalId;
                    resolution.CanonicalEntityIds.Add(canonicalId);
                }
                return true;
            }
        }

        private static Dictionary<string, string> ParseIdentityLocator(string locator)
        {
            Dictionary<string, string> parts =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] tokens = (locator ?? string.Empty).Split(';');
            for (int index = 0; index < tokens.Length; index++)
            {
                int split = tokens[index].IndexOf('=');
                if (split <= 0 || split >= tokens[index].Length - 1)
                {
                    continue;
                }
                parts[tokens[index].Substring(0, split).Trim()] =
                    tokens[index].Substring(split + 1).Trim();
            }
            return parts;
        }

        private static bool TryReadLocatorInt(
            Dictionary<string, string> parts,
            string key,
            out int value)
        {
            value = CoreConstants.InvalidIdValue;
            string raw;
            return parts != null &&
                parts.TryGetValue(key, out raw) &&
                int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private string ResolveCurrentIdentityLocked(
            string entityKind,
            Dictionary<string, string> parts)
        {
            int ordinal;
            if (CanonicalContractIdentityRuntimeEnabled &&
                string.Equals(entityKind, CoreConstants.EventEntityKindContract, StringComparison.Ordinal) &&
                TryReadLocatorInt(parts, "ordinal", out ordinal))
            {
                business businessSystem = ResolveBusinessSystemForContractIdentity();
                if (businessSystem != null && businessSystem.ActiveProposals != null &&
                    ordinal >= 0 && ordinal < businessSystem.ActiveProposals.Count)
                {
                    ContractIdentityRuntimeBinding binding;
                    if (contractIdentityByReference.TryGetValue(
                        businessSystem.ActiveProposals[ordinal], out binding) && binding != null)
                    {
                        return binding.EntityId ?? string.Empty;
                    }
                }
                return string.Empty;
            }

            if (CanonicalCliqueIdentityRuntimeEnabled &&
                string.Equals(entityKind, CoreConstants.EventEntityKindClique, StringComparison.Ordinal) &&
                TryReadLocatorInt(parts, "ordinal", out ordinal) &&
                Relationships.Cliques != null && ordinal >= 0 && ordinal < Relationships.Cliques.Count)
            {
                CliqueIdentityRuntimeBinding binding;
                if (cliqueIdentityByReference.TryGetValue(Relationships.Cliques[ordinal], out binding) &&
                    binding != null)
                {
                    return binding.EntityId ?? string.Empty;
                }
                return string.Empty;
            }

            if (CanonicalTaskIdentityRuntimeEnabled &&
                string.Equals(entityKind, CoreConstants.EventEntityKindTask, StringComparison.Ordinal) &&
                TryReadLocatorInt(parts, "ordinal", out ordinal) &&
                tasks.ActiveTasks != null && ordinal >= 0 && ordinal < tasks.ActiveTasks.Count)
            {
                TaskIdentityRuntimeBinding binding;
                if (generatedTaskIdentityByReference.TryGetValue(tasks.ActiveTasks[ordinal], out binding) &&
                    binding != null)
                {
                    return binding.EntityId ?? string.Empty;
                }
                return string.Empty;
            }

            if (CanonicalBullyingIdentityRuntimeEnabled &&
                string.Equals(entityKind, CoreConstants.EventEntityKindBullying, StringComparison.Ordinal))
            {
                int cliqueOrdinal;
                int targetId;
                if (TryReadLocatorInt(parts, "clique_ordinal", out cliqueOrdinal) &&
                    TryReadLocatorInt(parts, "target", out targetId) &&
                    Relationships.Cliques != null && cliqueOrdinal >= 0 &&
                    cliqueOrdinal < Relationships.Cliques.Count)
                {
                    Dictionary<int, BullyingIdentityRuntimeBinding> byTarget;
                    BullyingIdentityRuntimeBinding binding;
                    Relationships._clique clique = Relationships.Cliques[cliqueOrdinal];
                    if (bullyingIdentityByCliqueAndTargetId.TryGetValue(clique, out byTarget) &&
                        byTarget != null && byTarget.TryGetValue(targetId, out binding) &&
                        binding != null)
                    {
                        return binding.EntityId ?? string.Empty;
                    }
                }
                return string.Empty;
            }

            int floorIndex;
            int roomIndex;
            if (TryReadLocatorInt(parts, "floor", out floorIndex) &&
                TryReadLocatorInt(parts, "room", out roomIndex))
            {
                agency._room room = ResolveCurrentRoomBySavedOrdinalLocked(floorIndex, roomIndex);
                string roomGeneration;
                if (room == null || !agencyRoomEntityIdByReference.TryGetValue(room, out roomGeneration))
                {
                    return string.Empty;
                }

                if (string.Equals(entityKind, CoreConstants.EventEntityKindAgencyRoom, StringComparison.Ordinal))
                {
                    return roomGeneration ?? string.Empty;
                }
                if (string.Equals(entityKind, CoreConstants.EventEntityKindTheater, StringComparison.Ordinal))
                {
                    return room.type == agency._type.theatre
                        ? roomGeneration ?? string.Empty
                        : string.Empty;
                }
                if (string.Equals(entityKind, CoreConstants.EventEntityKindCafe, StringComparison.Ordinal))
                {
                    return room.type == agency._type.cafeAndShop
                        ? roomGeneration ?? string.Empty
                        : string.Empty;
                }

                if (string.Equals(entityKind, CoreConstants.EventEntityKindRoomWork, StringComparison.Ordinal))
                {
                    string owner;
                    int ownerId;
                    if (parts.TryGetValue("owner", out owner) &&
                        TryReadLocatorInt(parts, "id", out ownerId))
                    {
                        bool isCurrentOwner =
                            string.Equals(owner, "ssk", StringComparison.Ordinal)
                                ? room.SSK != null && room.SSK.ID == ownerId
                                : string.Equals(owner, "tour", StringComparison.Ordinal)
                                    ? room.tour != null && room.tour.ID == ownerId
                                    : false;
                        if (isCurrentOwner)
                        {
                            return string.Concat(
                                roomGeneration,
                                ":",
                                owner,
                                ":",
                                ownerId.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static agency._room ResolveCurrentRoomBySavedOrdinalLocked(
            int floorIndex,
            int roomIndex)
        {
            if (floorIndex < 0 || roomIndex < 0 || Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            SaveManager.SavedData savedData = main != null ? main.GetSavedData() : null;
            agency agencySystem = main != null && main.Data != null
                ? main.Data.GetComponent<agency>()
                : null;
            if (savedData == null || savedData.agency__Floors == null ||
                floorIndex >= savedData.agency__Floors.Count || agencySystem == null)
            {
                return null;
            }
            agency.FloorData floor = savedData.agency__Floors[floorIndex];
            if (floor == null || floor.Rooms == null || roomIndex >= floor.Rooms.Count)
            {
                return null;
            }

            int flatIndex = roomIndex;
            for (int index = 0; index < floorIndex; index++)
            {
                agency.FloorData earlier = savedData.agency__Floors[index];
                if (earlier != null && earlier.Rooms != null)
                {
                    flatIndex += earlier.Rooms.Count;
                }
            }
            List<agency._room> rooms = agencySystem.allRooms(true, true);
            return rooms != null && flatIndex >= 0 && flatIndex < rooms.Count
                ? rooms[flatIndex]
                : null;
        }
    }
}
