using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Wave-1 bullying-episode identity. One episode generation begins only
    /// when a target actually enters one clique's Bullied_Girls collection and
    /// remains stable until that exact interval ends. Clique generation is
    /// correlation/parent identity; mutable leader identity is never part of
    /// the canonical episode key.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string BullyingEpisodeGenerationPrefix = "b:";
        private const string BullyingIdentityWitnessPrefix = "sha256:";

        private sealed class BullyingIdentityRuntimeBinding
        {
            internal string EntityId = string.Empty;
            internal string ParentCliqueEntityId = string.Empty;
            internal int TargetId = CoreConstants.InvalidIdValue;
            internal long CoverageStartSequence;
            internal string Origin = LightweightIdentityBindingSchema.OriginNative;
            internal readonly List<string> LegacyCandidateKeys = new List<string>();
        }

        private readonly Dictionary<Relationships._clique, Dictionary<int, BullyingIdentityRuntimeBinding>>
            bullyingIdentityByCliqueAndTargetId =
                new Dictionary<Relationships._clique, Dictionary<int, BullyingIdentityRuntimeBinding>>();

        private List<LightweightIdentityBindingRecord> pendingLoadedBullyingBindings;
        private bool pendingLoadedBullyingBindingsComplete;

        private static bool CanonicalBullyingIdentityRuntimeEnabled
        {
            get
            {
                return LightweightCoreStorageEngine.DurableV6RuntimeEnabled;
            }
        }

        private static string CreateBullyingEpisodeGenerationId()
        {
            return BullyingEpisodeGenerationPrefix + Guid.NewGuid().ToString("N");
        }

        private long ResolveNextBullyingCoverageSequenceLocked()
        {
            return captureSequence == long.MaxValue
                ? long.MaxValue
                : captureSequence + 1L;
        }

        private string ResolveParentCliqueGenerationForBullyingLocked(
            Relationships._clique clique)
        {
            if (clique == null || !CanonicalBullyingIdentityRuntimeEnabled)
            {
                return string.Empty;
            }

            CliqueIdentityRuntimeBinding cliqueBinding =
                EnsureCliqueIdentityBindingLocked(
                    clique,
                    LightweightIdentityBindingSchema.OriginNative,
                    BuildCliqueSignature(clique));
            return cliqueBinding != null
                ? cliqueBinding.EntityId ?? string.Empty
                : string.Empty;
        }

        private static string BuildBullyingChildLocator(int targetId)
        {
            if (targetId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return string.Empty;
            }

            return LightweightIdentityBindingSchema.BullyingChildLocatorPrefix +
                targetId.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseBullyingChildLocator(
            string childLocator,
            out int targetId)
        {
            targetId = CoreConstants.InvalidIdValue;
            if (string.IsNullOrEmpty(childLocator) ||
                !childLocator.StartsWith(
                    LightweightIdentityBindingSchema.BullyingChildLocatorPrefix,
                    StringComparison.Ordinal))
            {
                return false;
            }

            string numericPart = childLocator.Substring(LightweightIdentityBindingSchema.BullyingChildLocatorPrefix.Length);
            return int.TryParse(
                    numericPart,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out targetId) &&
                targetId >= CoreConstants.MinimumValidIdolIdentifier;
        }

        private static void AddBullyingLegacyCandidateKey(
            BullyingIdentityRuntimeBinding binding,
            string candidateKey)
        {
            if (binding == null || string.IsNullOrEmpty(candidateKey))
            {
                return;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < binding.LegacyCandidateKeys.Count;
                index++)
            {
                if (string.Equals(
                        binding.LegacyCandidateKeys[index],
                        candidateKey,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }
            binding.LegacyCandidateKeys.Add(candidateKey);
        }

        private BullyingIdentityRuntimeBinding EnsureBullyingEpisodeIdentityLocked(
            Relationships._clique clique,
            data_girls.girls target,
            string origin,
            string legacyCandidateKey)
        {
            if (clique == null ||
                target == null ||
                target.id < CoreConstants.MinimumValidIdolIdentifier ||
                !clique.IsBullied(target))
            {
                return null;
            }

            Dictionary<int, BullyingIdentityRuntimeBinding> byTarget;
            if (!bullyingIdentityByCliqueAndTargetId.TryGetValue(clique, out byTarget) ||
                byTarget == null)
            {
                byTarget = new Dictionary<int, BullyingIdentityRuntimeBinding>();
                bullyingIdentityByCliqueAndTargetId[clique] = byTarget;
            }

            BullyingIdentityRuntimeBinding existing;
            if (byTarget.TryGetValue(target.id, out existing) && existing != null)
            {
                AddBullyingLegacyCandidateKey(existing, legacyCandidateKey);
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindBullying,
                    legacyCandidateKey,
                    CoreConstants.EventEntityKindBullying,
                    existing.EntityId,
                    LightweightIdentityBindingSchema.CandidateSourceNativeBinding);
                if (string.IsNullOrEmpty(existing.ParentCliqueEntityId))
                {
                    existing.ParentCliqueEntityId =
                        ResolveParentCliqueGenerationForBullyingLocked(clique);
                }
                return existing;
            }

            string parentCliqueEntityId =
                ResolveParentCliqueGenerationForBullyingLocked(clique);
            if (string.IsNullOrEmpty(parentCliqueEntityId))
            {
                return null;
            }

            BullyingIdentityRuntimeBinding created =
                new BullyingIdentityRuntimeBinding
                {
                    EntityId = CreateBullyingEpisodeGenerationId(),
                    ParentCliqueEntityId = parentCliqueEntityId,
                    TargetId = target.id,
                    CoverageStartSequence = ResolveNextBullyingCoverageSequenceLocked(),
                    Origin = string.IsNullOrEmpty(origin)
                        ? LightweightIdentityBindingSchema.OriginNative
                        : origin
                };
            AddBullyingLegacyCandidateKey(created, legacyCandidateKey);
            byTarget[target.id] = created;
            RegisterIdentityCandidateLocked(
                CoreConstants.EventEntityKindBullying,
                legacyCandidateKey,
                CoreConstants.EventEntityKindBullying,
                created.EntityId,
                LightweightIdentityBindingSchema.CandidateSourceNativeBinding);
            return created;
        }

        /// <summary>
        /// Resolves the bullying start identity after AddBulliedGirl proves the
        /// not-bullied -> bullied transition. Returns the legacy leader|target
        /// key until the staged v6 runtime is active.
        /// </summary>
        private string ResolveBullyingStartedEntityIdentifierLocked(
            Relationships._clique clique,
            data_girls.girls target,
            int leaderId,
            out string parentCliqueGenerationId)
        {
            parentCliqueGenerationId = string.Empty;
            string legacyIdentifier = target != null
                ? BuildBullyingEntityIdentifier(leaderId, target.id)
                : string.Empty;

            if (CanonicalBullyingIdentityRuntimeEnabled && target != null)
            {
                BullyingIdentityRuntimeBinding binding =
                    EnsureBullyingEpisodeIdentityLocked(
                        clique,
                        target,
                        LightweightIdentityBindingSchema.OriginNative,
                        legacyIdentifier);
                if (binding != null && !string.IsNullOrEmpty(binding.EntityId))
                {
                    parentCliqueGenerationId = binding.ParentCliqueEntityId ?? string.Empty;
                    return binding.EntityId;
                }
            }

            return !string.IsNullOrEmpty(legacyIdentifier)
                ? legacyIdentifier
                : CoreConstants.UnknownBullyingEntityIdentifier;
        }

        /// <summary>
        /// Freezes the canonical episode/parent generation into a pre-mutation
        /// stop snapshot. Repeated nested stop observers can then name the same
        /// terminal episode even after the live map is retired.
        /// </summary>
        private void CaptureBullyingIdentityForStopSnapshotLocked(
            Relationships._clique clique,
            data_girls.girls target,
            int leaderId,
            BullyingStateSnapshot snapshot)
        {
            if (!CanonicalBullyingIdentityRuntimeEnabled ||
                clique == null ||
                target == null ||
                snapshot == null ||
                !clique.IsBullied(target))
            {
                return;
            }

            string legacyIdentifier =
                BuildBullyingEntityIdentifier(leaderId, target.id);
            BullyingIdentityRuntimeBinding binding =
                EnsureBullyingEpisodeIdentityLocked(
                    clique,
                    target,
                    LightweightIdentityBindingSchema.OriginNative,
                    legacyIdentifier);
            if (binding == null)
            {
                return;
            }

            snapshot.BullyingEpisodeId = binding.EntityId ?? string.Empty;
            snapshot.ParentCliqueGenerationId =
                binding.ParentCliqueEntityId ?? string.Empty;
        }

        private string ResolveBullyingEndedEntityIdentifierLocked(
            Relationships._clique clique,
            BullyingStateSnapshot snapshot,
            out string parentCliqueGenerationId)
        {
            parentCliqueGenerationId = snapshot != null
                ? snapshot.ParentCliqueGenerationId ?? string.Empty
                : string.Empty;
            if (CanonicalBullyingIdentityRuntimeEnabled &&
                snapshot != null &&
                !string.IsNullOrEmpty(snapshot.BullyingEpisodeId))
            {
                return snapshot.BullyingEpisodeId;
            }

            string legacyIdentifier = snapshot != null
                ? BuildBullyingEntityIdentifier(snapshot.LeaderId, snapshot.TargetId)
                : string.Empty;
            return !string.IsNullOrEmpty(legacyIdentifier)
                ? legacyIdentifier
                : CoreConstants.UnknownBullyingEntityIdentifier;
        }

        private void RetireBullyingEpisodeIdentityLocked(
            Relationships._clique clique,
            int targetId,
            string expectedEpisodeId)
        {
            if (clique == null || targetId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            Dictionary<int, BullyingIdentityRuntimeBinding> byTarget;
            if (!bullyingIdentityByCliqueAndTargetId.TryGetValue(clique, out byTarget) ||
                byTarget == null)
            {
                return;
            }

            BullyingIdentityRuntimeBinding binding;
            if (!byTarget.TryGetValue(targetId, out binding) || binding == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(expectedEpisodeId) &&
                !string.Equals(
                    expectedEpisodeId,
                    binding.EntityId,
                    StringComparison.Ordinal))
            {
                return;
            }

            byTarget.Remove(targetId);
            if (byTarget.Count == 0)
            {
                bullyingIdentityByCliqueAndTargetId.Remove(clique);
            }
        }

        private void RetireAllBullyingIdentityForCliqueLocked(
            Relationships._clique clique)
        {
            if (clique != null)
            {
                bullyingIdentityByCliqueAndTargetId.Remove(clique);
            }
        }

        private static string BuildBullyingIdentityValidationFingerprint(
            Relationships.SaveData_Clique savedCliqueRow,
            int targetId)
        {
            string cliqueFingerprint =
                BuildCliqueIdentityValidationFingerprint(savedCliqueRow);
            string canonical = string.Concat(
                "bullying-binding-v1\n",
                cliqueFingerprint ?? string.Empty,
                "\ntarget:",
                targetId.ToString(CultureInfo.InvariantCulture));
            byte[] bytes = Encoding.UTF8.GetBytes(canonical);
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(digest.Length * 2);
                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < digest.Length;
                    index++)
                {
                    builder.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));
                }
                return BullyingIdentityWitnessPrefix + builder.ToString();
            }
        }

        private static bool SavedCliqueContainsBulliedTarget(
            Relationships.SaveData_Clique savedCliqueRow,
            int targetId)
        {
            return savedCliqueRow != null &&
                savedCliqueRow.Bullied_Girls != null &&
                savedCliqueRow.Bullied_Girls.Contains(targetId);
        }

        private static data_girls.girls ResolveLiveBulliedTarget(
            Relationships._clique clique,
            int targetId)
        {
            if (clique == null || clique.Bullied_Girls == null)
            {
                return null;
            }

            data_girls.girls resolved = null;
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < clique.Bullied_Girls.Count;
                index++)
            {
                data_girls.girls candidate = clique.Bullied_Girls[index];
                if (candidate == null || candidate.id != targetId)
                {
                    continue;
                }
                if (resolved != null)
                {
                    return null;
                }
                resolved = candidate;
            }
            return resolved;
        }

        internal List<LightweightIdentityBindingRecord>
            CaptureBullyingIdentityBindingsForCheckpointLocked(
                SaveManager.SavedData savedData)
        {
            List<LightweightIdentityBindingRecord> result =
                new List<LightweightIdentityBindingRecord>();
            if (!CanonicalBullyingIdentityRuntimeEnabled ||
                savedData == null ||
                savedData.Relationships__Cliques == null ||
                Relationships.Cliques == null ||
                savedData.Relationships__Cliques.Count != Relationships.Cliques.Count)
            {
                return result;
            }

            List<Relationships._clique> staleCliques =
                new List<Relationships._clique>();
            foreach (Relationships._clique clique in bullyingIdentityByCliqueAndTargetId.Keys)
            {
                if (!Relationships.Cliques.Contains(clique))
                {
                    staleCliques.Add(clique);
                }
            }
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < staleCliques.Count;
                index++)
            {
                bullyingIdentityByCliqueAndTargetId.Remove(staleCliques[index]);
            }

            for (int cliqueOrdinal = CoreConstants.ZeroBasedListStartIndex;
                cliqueOrdinal < savedData.Relationships__Cliques.Count;
                cliqueOrdinal++)
            {
                Relationships.SaveData_Clique savedCliqueRow =
                    savedData.Relationships__Cliques[cliqueOrdinal];
                Relationships._clique liveClique = Relationships.Cliques[cliqueOrdinal];
                if (!CliqueSavedRowMatchesLive(savedCliqueRow, liveClique) ||
                    savedCliqueRow == null ||
                    savedCliqueRow.Bullied_Girls == null)
                {
                    continue;
                }

                CliqueIdentityRuntimeBinding parentCliqueBinding;
                if (!cliqueIdentityByReference.TryGetValue(
                        liveClique,
                        out parentCliqueBinding) ||
                    parentCliqueBinding == null ||
                    string.IsNullOrEmpty(parentCliqueBinding.EntityId))
                {
                    continue;
                }

                HashSet<int> seenTargets = new HashSet<int>();
                for (int targetIndex = CoreConstants.ZeroBasedListStartIndex;
                    targetIndex < savedCliqueRow.Bullied_Girls.Count;
                    targetIndex++)
                {
                    int targetId = savedCliqueRow.Bullied_Girls[targetIndex];
                    if (targetId < CoreConstants.MinimumValidIdolIdentifier ||
                        !seenTargets.Add(targetId))
                    {
                        continue;
                    }

                    data_girls.girls liveTarget =
                        ResolveLiveBulliedTarget(liveClique, targetId);
                    if (liveTarget == null)
                    {
                        continue;
                    }

                    string legacyCandidateKey =
                        BuildBullyingEntityIdentifier(savedCliqueRow.Leader, targetId);
                    BullyingIdentityRuntimeBinding episodeBinding =
                        EnsureBullyingEpisodeIdentityLocked(
                            liveClique,
                            liveTarget,
                            LightweightIdentityBindingSchema.OriginNative,
                            legacyCandidateKey);
                    if (episodeBinding == null ||
                        string.IsNullOrEmpty(episodeBinding.EntityId) ||
                        !string.Equals(
                            episodeBinding.ParentCliqueEntityId,
                            parentCliqueBinding.EntityId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    List<string> legacyCandidates =
                        new List<string>(episodeBinding.LegacyCandidateKeys);
                    legacyCandidates.Sort(StringComparer.Ordinal);
                    result.Add(new LightweightIdentityBindingRecord
                    {
                        EntityKind = CoreConstants.EventEntityKindBullying,
                        EntityId = episodeBinding.EntityId,
                        ContainerKind = LightweightIdentityBindingSchema.ContainerRelationshipsCliques,
                        ContainerOrdinal = cliqueOrdinal,
                        ParentEntityKind = CoreConstants.EventEntityKindClique,
                        ParentEntityId = parentCliqueBinding.EntityId,
                        ChildLocator = BuildBullyingChildLocator(targetId),
                        ValidationFingerprint =
                            BuildBullyingIdentityValidationFingerprint(
                                savedCliqueRow,
                                targetId),
                        Origin = episodeBinding.Origin,
                        CoverageStartSequence = Math.Min(
                            episodeBinding.CoverageStartSequence,
                            captureSequence),
                        LegacyCandidateKeys = legacyCandidates
                    });
                }
            }
            return result;
        }

        internal void PrepareBullyingIdentityBindingsForLoad(
            bool exactCheckpointSelected,
            bool identityBindingsComplete,
            IReadOnlyList<LightweightIdentityBindingRecord> identityBindings)
        {
            lock (runtimeLock)
            {
                bullyingIdentityByCliqueAndTargetId.Clear();
                pendingLoadedBullyingBindings = null;
                pendingLoadedBullyingBindingsComplete = false;

                if (!CanonicalBullyingIdentityRuntimeEnabled ||
                    !exactCheckpointSelected ||
                    !identityBindingsComplete ||
                    identityBindings == null)
                {
                    return;
                }

                List<LightweightIdentityBindingRecord> bullying =
                    new List<LightweightIdentityBindingRecord>();
                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < identityBindings.Count;
                    index++)
                {
                    LightweightIdentityBindingRecord binding = identityBindings[index];
                    if (binding != null &&
                        string.Equals(
                            binding.EntityKind,
                            CoreConstants.EventEntityKindBullying,
                            StringComparison.Ordinal))
                    {
                        bullying.Add(CloneBullyingIdentityBinding(binding));
                    }
                }
                pendingLoadedBullyingBindings = bullying;
                pendingLoadedBullyingBindingsComplete = true;
            }
        }

        internal void AssociateLoadedBullyingIdentities()
        {
            if (!CanonicalBullyingIdentityRuntimeEnabled)
            {
                return;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForCliqueIdentity();
            lock (runtimeLock)
            {
                if (TryAdoptLoadedBullyingIdentitiesLocked())
                {
                    pendingLoadedBullyingBindings = null;
                    pendingLoadedBullyingBindingsComplete = false;
                    return;
                }

                if (!pendingLoadedBullyingBindingsComplete ||
                    pendingLoadedBullyingBindings == null ||
                    savedData == null ||
                    savedData.Relationships__Cliques == null ||
                    Relationships.Cliques == null)
                {
                    return;
                }

                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < pendingLoadedBullyingBindings.Count;
                    index++)
                {
                    LightweightIdentityBindingRecord binding =
                        pendingLoadedBullyingBindings[index];
                    int targetId;
                    if (binding == null ||
                        binding.ContainerOrdinal < CoreConstants.ZeroBasedListStartIndex ||
                        binding.ContainerOrdinal >= savedData.Relationships__Cliques.Count ||
                        binding.ContainerOrdinal >= Relationships.Cliques.Count ||
                        !TryParseBullyingChildLocator(binding.ChildLocator, out targetId))
                    {
                        continue;
                    }

                    Relationships.SaveData_Clique savedCliqueRow =
                        savedData.Relationships__Cliques[binding.ContainerOrdinal];
                    Relationships._clique liveClique =
                        Relationships.Cliques[binding.ContainerOrdinal];
                    CliqueIdentityRuntimeBinding parentCliqueBinding;
                    if (!cliqueIdentityByReference.TryGetValue(
                            liveClique,
                            out parentCliqueBinding) ||
                        parentCliqueBinding == null ||
                        !string.Equals(
                            parentCliqueBinding.EntityId,
                            binding.ParentEntityId,
                            StringComparison.Ordinal) ||
                        !SavedCliqueContainsBulliedTarget(savedCliqueRow, targetId) ||
                        !CliqueSavedRowMatchesLive(savedCliqueRow, liveClique) ||
                        !string.Equals(
                            BuildBullyingIdentityValidationFingerprint(
                                savedCliqueRow,
                                targetId),
                            binding.ValidationFingerprint,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    data_girls.girls liveTarget =
                        ResolveLiveBulliedTarget(liveClique, targetId);
                    if (liveTarget == null)
                    {
                        continue;
                    }

                    Dictionary<int, BullyingIdentityRuntimeBinding> byTarget;
                    if (!bullyingIdentityByCliqueAndTargetId.TryGetValue(
                            liveClique,
                            out byTarget) ||
                        byTarget == null)
                    {
                        byTarget = new Dictionary<int, BullyingIdentityRuntimeBinding>();
                        bullyingIdentityByCliqueAndTargetId[liveClique] = byTarget;
                    }

                    BullyingIdentityRuntimeBinding runtimeBinding =
                        new BullyingIdentityRuntimeBinding
                        {
                            EntityId = binding.EntityId ?? string.Empty,
                            ParentCliqueEntityId = binding.ParentEntityId ?? string.Empty,
                            TargetId = targetId,
                            CoverageStartSequence = binding.CoverageStartSequence,
                            Origin = binding.Origin ?? string.Empty
                        };
                    if (binding.LegacyCandidateKeys != null)
                    {
                        for (int candidateIndex = CoreConstants.ZeroBasedListStartIndex;
                            candidateIndex < binding.LegacyCandidateKeys.Count;
                            candidateIndex++)
                        {
                            AddBullyingLegacyCandidateKey(
                                runtimeBinding,
                                binding.LegacyCandidateKeys[candidateIndex]);
                        }
                    }
                    byTarget[targetId] = runtimeBinding;
                }

                pendingLoadedBullyingBindings = null;
                pendingLoadedBullyingBindingsComplete = false;
            }
        }

        private static LightweightIdentityBindingRecord CloneBullyingIdentityBinding(
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

        private void ResetBullyingIdentityRuntimeStateLocked()
        {
            bullyingIdentityByCliqueAndTargetId.Clear();
            pendingLoadedBullyingBindings = null;
            pendingLoadedBullyingBindingsComplete = false;
        }
    }
}
