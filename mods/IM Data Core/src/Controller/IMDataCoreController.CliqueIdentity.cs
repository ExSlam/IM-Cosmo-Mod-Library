using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Wave-1 clique-generation identity. A clique generation belongs to the
    /// runtime clique object for its whole semantic lifetime; mutable leader and
    /// membership state remain payload/witness data rather than identity.
    /// Canonical clique EntityId emission is gated on the staged v6 identity
    /// runtime so the live v5 writer cannot create non-rebindable generations.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string CliqueGenerationPrefix = "q:";
        private const string CliqueIdentityWitnessPrefix = "sha256:";

        private sealed class CliqueIdentityRuntimeBinding
        {
            internal string EntityId = string.Empty;
            internal long CoverageStartSequence;
            internal string Origin = LightweightIdentityBindingSchema.OriginNative;
            internal string LegacyCandidateKey = string.Empty;
        }

        private readonly Dictionary<Relationships._clique, CliqueIdentityRuntimeBinding>
            cliqueIdentityByReference =
                new Dictionary<Relationships._clique, CliqueIdentityRuntimeBinding>();

        private List<LightweightIdentityBindingRecord> pendingLoadedCliqueBindings;
        private bool pendingLoadedCliqueBindingsComplete;

        private static bool CanonicalCliqueIdentityRuntimeEnabled
        {
            get
            {
                return LightweightCoreStorageEngine.DurableV6RuntimeEnabled;
            }
        }

        private static string CreateCliqueGenerationId()
        {
            return CliqueGenerationPrefix + Guid.NewGuid().ToString("N");
        }

        private long ResolveNextCliqueCoverageSequenceLocked()
        {
            return captureSequence == long.MaxValue
                ? long.MaxValue
                : captureSequence + 1L;
        }

        /// <summary>
        /// Binds the clique appended by vanilla StartNewClique. The previous
        /// count is captured before the private birth method runs so a modded
        /// nested/list mutation cannot make an older clique look newly born.
        /// </summary>
        internal void BindNewCliqueIdentity(
            int previousCliqueCount,
            data_girls.girls foundingMember)
        {
            if (Relationships.Cliques == null ||
                Relationships.Cliques.Count <= previousCliqueCount)
            {
                return;
            }

            Relationships._clique createdClique = null;
            int startIndex = previousCliqueCount < CoreConstants.ZeroBasedListStartIndex
                ? CoreConstants.ZeroBasedListStartIndex
                : previousCliqueCount;
            for (int index = startIndex; index < Relationships.Cliques.Count; index++)
            {
                Relationships._clique candidate = Relationships.Cliques[index];
                if (candidate == null ||
                    foundingMember == null ||
                    candidate.Members == null ||
                    !candidate.Members.Contains(foundingMember))
                {
                    continue;
                }

                if (createdClique != null)
                {
                    // More than one plausible appended clique is not an exact
                    // birth locator. Refuse to guess which object was created.
                    return;
                }
                createdClique = candidate;
            }

            if (createdClique == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                EnsureCliqueIdentityBindingLocked(
                    createdClique,
                    LightweightIdentityBindingSchema.OriginNative,
                    BuildCliqueSignature(createdClique));
            }
        }

        private CliqueIdentityRuntimeBinding EnsureCliqueIdentityBindingLocked(
            Relationships._clique clique,
            string origin,
            string legacyCandidateKey)
        {
            if (clique == null)
            {
                return null;
            }

            CliqueIdentityRuntimeBinding existing;
            if (cliqueIdentityByReference.TryGetValue(clique, out existing) &&
                existing != null &&
                !string.IsNullOrEmpty(existing.EntityId))
            {
                if (string.IsNullOrEmpty(existing.LegacyCandidateKey) &&
                    !string.IsNullOrEmpty(legacyCandidateKey))
                {
                    existing.LegacyCandidateKey = legacyCandidateKey;
                }
                RegisterIdentityCandidateLocked(
                    CoreConstants.EventEntityKindClique,
                    legacyCandidateKey,
                    CoreConstants.EventEntityKindClique,
                    existing.EntityId,
                    LightweightIdentityBindingSchema.CandidateSourceNativeBinding);
                return existing;
            }

            CliqueIdentityRuntimeBinding created =
                new CliqueIdentityRuntimeBinding
                {
                    EntityId = CreateCliqueGenerationId(),
                    CoverageStartSequence = ResolveNextCliqueCoverageSequenceLocked(),
                    Origin = string.IsNullOrEmpty(origin)
                        ? LightweightIdentityBindingSchema.OriginNative
                        : origin,
                    LegacyCandidateKey = legacyCandidateKey ?? string.Empty
                };
            cliqueIdentityByReference[clique] = created;
            RegisterIdentityCandidateLocked(
                CoreConstants.EventEntityKindClique,
                created.LegacyCandidateKey,
                CoreConstants.EventEntityKindClique,
                created.EntityId,
                LightweightIdentityBindingSchema.CandidateSourceNativeBinding);
            return created;
        }

        private string ResolveCliqueHistoryEntityIdentifierLocked(
            Relationships._clique clique,
            string legacySignature)
        {
            return ResolveCliqueHistoryEntityIdentifierLocked(
                clique,
                legacySignature,
                string.Empty);
        }

        private string ResolveCliqueHistoryEntityIdentifierLocked(
            Relationships._clique clique,
            string legacySignature,
            string snapshotGenerationId)
        {
            if (CanonicalCliqueIdentityRuntimeEnabled)
            {
                if (!string.IsNullOrEmpty(snapshotGenerationId))
                {
                    return snapshotGenerationId;
                }

                CliqueIdentityRuntimeBinding binding =
                    EnsureCliqueIdentityBindingLocked(
                        clique,
                        LightweightIdentityBindingSchema.OriginNative,
                        legacySignature);
                if (binding != null && !string.IsNullOrEmpty(binding.EntityId))
                {
                    return binding.EntityId;
                }
            }

            return !string.IsNullOrEmpty(legacySignature)
                ? legacySignature
                : CoreConstants.UnknownCliqueEntityIdentifier;
        }

        internal string CaptureCliqueGenerationForQuitSnapshot(
            Relationships._clique clique)
        {
            if (!CanonicalCliqueIdentityRuntimeEnabled || clique == null)
            {
                return string.Empty;
            }

            lock (runtimeLock)
            {
                CliqueIdentityRuntimeBinding binding =
                    EnsureCliqueIdentityBindingLocked(
                        clique,
                        LightweightIdentityBindingSchema.OriginNative,
                        BuildCliqueSignature(clique));
                return binding != null ? binding.EntityId : string.Empty;
            }
        }

        internal void RetireCliqueIdentityIfRemoved(Relationships._clique clique)
        {
            if (clique == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                if (Relationships.Cliques != null &&
                    Relationships.Cliques.Contains(clique))
                {
                    return;
                }
                RetireAllBullyingIdentityForCliqueLocked(clique);
                cliqueIdentityByReference.Remove(clique);
            }
        }

        private static string BuildCliqueLegacyCandidateKey(
            Relationships.SaveData_Clique savedRow)
        {
            if (savedRow == null || savedRow.Members == null ||
                savedRow.Members.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            List<int> memberIds = new List<int>();
            for (int index = 0; index < savedRow.Members.Count; index++)
            {
                int memberId = savedRow.Members[index];
                if (memberId >= CoreConstants.MinimumValidIdolIdentifier)
                {
                    memberIds.Add(memberId);
                }
            }
            if (memberIds.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            memberIds.Sort();
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < memberIds.Count; index++)
            {
                if (index > CoreConstants.ZeroBasedListStartIndex)
                {
                    builder.Append(CoreConstants.CliqueSignatureMemberSeparator);
                }
                builder.Append(memberIds[index].ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        internal static string BuildCliqueIdentityValidationFingerprint(
            Relationships.SaveData_Clique savedRow)
        {
            if (savedRow == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(768);
            AppendCliqueWitnessField(
                builder,
                savedRow.Leader.ToString(CultureInfo.InvariantCulture));
            AppendCliqueWitnessIntList(builder, savedRow.Members);
            AppendCliqueWitnessField(builder, savedRow.Known ? "1" : "0");
            AppendCliqueWitnessIntList(builder, savedRow.Bullied_Girls);
            AppendCliqueWitnessIntList(builder, savedRow.KnownBulliedGirls);

            List<Relationships.SaveData_Clique._stopped_bullying> stopped =
                savedRow.StoppedBullying;
            AppendCliqueWitnessField(
                builder,
                (stopped != null ? stopped.Count : 0)
                    .ToString(CultureInfo.InvariantCulture));
            if (stopped != null)
            {
                for (int index = 0; index < stopped.Count; index++)
                {
                    Relationships.SaveData_Clique._stopped_bullying row = stopped[index];
                    AppendCliqueWitnessField(
                        builder,
                        row != null
                            ? row.Target.ToString(CultureInfo.InvariantCulture)
                            : CoreConstants.InvalidIdValue.ToString(CultureInfo.InvariantCulture));
                    AppendCliqueWitnessIntList(builder, row != null ? row.Girls : null);
                }
            }

            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                return CliqueIdentityWitnessPrefix +
                    ToLowerHexForCliqueIdentity(sha256.ComputeHash(bytes));
            }
        }

        private static void AppendCliqueWitnessIntList(
            StringBuilder builder,
            IList<int> values)
        {
            int count = values != null ? values.Count : 0;
            AppendCliqueWitnessField(
                builder,
                count.ToString(CultureInfo.InvariantCulture));
            if (values == null)
            {
                return;
            }

            for (int index = 0; index < values.Count; index++)
            {
                AppendCliqueWitnessField(
                    builder,
                    values[index].ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AppendCliqueWitnessField(
            StringBuilder builder,
            string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append('|');
        }

        private static string ToLowerHexForCliqueIdentity(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static bool CliqueSavedRowMatchesLive(
            Relationships.SaveData_Clique savedRow,
            Relationships._clique liveClique)
        {
            if (savedRow == null || liveClique == null)
            {
                return false;
            }

            int liveLeader = liveClique.Leader != null
                ? liveClique.Leader.id
                : CoreConstants.InvalidIdValue;
            if (savedRow.Leader != liveLeader || savedRow.Known != liveClique.Known ||
                !CliqueSavedIdListMatchesLive(savedRow.Members, liveClique.Members) ||
                !CliqueSavedIdListMatchesLive(savedRow.Bullied_Girls, liveClique.Bullied_Girls) ||
                !CliqueSavedIdListMatchesLive(savedRow.KnownBulliedGirls, liveClique.KnownBulliedGirls))
            {
                return false;
            }

            List<Relationships.SaveData_Clique._stopped_bullying> savedStopped =
                savedRow.StoppedBullying;
            List<Relationships._clique._stopped_bullying> liveStopped =
                liveClique.StoppedBullying;
            int savedCount = savedStopped != null ? savedStopped.Count : 0;
            int liveCount = liveStopped != null ? liveStopped.Count : 0;
            if (savedCount != liveCount)
            {
                return false;
            }

            for (int index = 0; index < savedCount; index++)
            {
                Relationships.SaveData_Clique._stopped_bullying saved = savedStopped[index];
                Relationships._clique._stopped_bullying live = liveStopped[index];
                if (saved == null || live == null)
                {
                    if (!ReferenceEquals(saved, null) || !ReferenceEquals(live, null))
                    {
                        return false;
                    }
                    continue;
                }

                int liveTarget = live.Target != null
                    ? live.Target.id
                    : CoreConstants.InvalidIdValue;
                if (saved.Target != liveTarget ||
                    !CliqueSavedIdListMatchesLive(saved.Girls, live.Girls))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CliqueSavedIdListMatchesLive(
            IList<int> savedIds,
            IList<data_girls.girls> liveGirls)
        {
            int savedCount = savedIds != null ? savedIds.Count : 0;
            int liveCount = liveGirls != null ? liveGirls.Count : 0;
            if (savedCount != liveCount)
            {
                return false;
            }

            for (int index = 0; index < savedCount; index++)
            {
                data_girls.girls liveGirl = liveGirls[index];
                int liveId = liveGirl != null
                    ? liveGirl.id
                    : CoreConstants.InvalidIdValue;
                if (savedIds[index] != liveId)
                {
                    return false;
                }
            }
            return true;
        }

        internal List<LightweightIdentityBindingRecord>
            CaptureCliqueIdentityBindingsForCheckpointLocked(
                SaveManager.SavedData savedData)
        {
            List<LightweightIdentityBindingRecord> result =
                new List<LightweightIdentityBindingRecord>();
            if (!CanonicalCliqueIdentityRuntimeEnabled ||
                savedData == null ||
                savedData.Relationships__Cliques == null ||
                Relationships.Cliques == null ||
                Relationships.Cliques.Count != savedData.Relationships__Cliques.Count)
            {
                return result;
            }

            // Remove stale runtime-only references before checkpoint projection.
            List<Relationships._clique> stale = new List<Relationships._clique>();
            foreach (Relationships._clique clique in cliqueIdentityByReference.Keys)
            {
                if (!Relationships.Cliques.Contains(clique))
                {
                    stale.Add(clique);
                }
            }
            for (int index = 0; index < stale.Count; index++)
            {
                cliqueIdentityByReference.Remove(stale[index]);
            }

            for (int ordinal = CoreConstants.ZeroBasedListStartIndex;
                ordinal < savedData.Relationships__Cliques.Count;
                ordinal++)
            {
                Relationships.SaveData_Clique savedRow =
                    savedData.Relationships__Cliques[ordinal];
                Relationships._clique liveClique = Relationships.Cliques[ordinal];
                if (!CliqueSavedRowMatchesLive(savedRow, liveClique))
                {
                    continue;
                }

                string legacyCandidateKey = BuildCliqueLegacyCandidateKey(savedRow);
                CliqueIdentityRuntimeBinding runtimeBinding =
                    EnsureCliqueIdentityBindingLocked(
                        liveClique,
                        LightweightIdentityBindingSchema.OriginNative,
                        legacyCandidateKey);
                if (runtimeBinding == null || string.IsNullOrEmpty(runtimeBinding.EntityId))
                {
                    continue;
                }

                List<string> legacyCandidates = new List<string>();
                string candidate = !string.IsNullOrEmpty(runtimeBinding.LegacyCandidateKey)
                    ? runtimeBinding.LegacyCandidateKey
                    : legacyCandidateKey;
                if (!string.IsNullOrEmpty(candidate))
                {
                    legacyCandidates.Add(candidate);
                }

                result.Add(new LightweightIdentityBindingRecord
                {
                    EntityKind = CoreConstants.EventEntityKindClique,
                    EntityId = runtimeBinding.EntityId,
                    ContainerKind = LightweightIdentityBindingSchema.ContainerRelationshipsCliques,
                    ContainerOrdinal = ordinal,
                    ParentEntityKind = string.Empty,
                    ParentEntityId = string.Empty,
                    ChildLocator = string.Empty,
                    ValidationFingerprint = BuildCliqueIdentityValidationFingerprint(savedRow),
                    Origin = runtimeBinding.Origin,
                    CoverageStartSequence = Math.Min(runtimeBinding.CoverageStartSequence, captureSequence),
                    LegacyCandidateKeys = legacyCandidates
                });
            }
            return result;
        }

        internal void PrepareCliqueIdentityBindingsForLoad(
            bool exactCheckpointSelected,
            bool identityBindingsComplete,
            IReadOnlyList<LightweightIdentityBindingRecord> identityBindings)
        {
            lock (runtimeLock)
            {
                cliqueIdentityByReference.Clear();
                pendingLoadedCliqueBindings = null;
                pendingLoadedCliqueBindingsComplete = false;

                if (!CanonicalCliqueIdentityRuntimeEnabled ||
                    !exactCheckpointSelected ||
                    !identityBindingsComplete ||
                    identityBindings == null)
                {
                    return;
                }

                List<LightweightIdentityBindingRecord> cliques =
                    new List<LightweightIdentityBindingRecord>();
                for (int index = 0; index < identityBindings.Count; index++)
                {
                    LightweightIdentityBindingRecord binding = identityBindings[index];
                    if (binding != null &&
                        string.Equals(
                            binding.EntityKind,
                            CoreConstants.EventEntityKindClique,
                            StringComparison.Ordinal))
                    {
                        cliques.Add(CloneCliqueIdentityBinding(binding));
                    }
                }

                pendingLoadedCliqueBindings = cliques;
                pendingLoadedCliqueBindingsComplete = true;
            }
        }

        internal void AssociateLoadedCliqueIdentities()
        {
            if (!CanonicalCliqueIdentityRuntimeEnabled)
            {
                return;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForCliqueIdentity();
            lock (runtimeLock)
            {
                if (TryAdoptLoadedCliqueIdentitiesLocked())
                {
                    pendingLoadedCliqueBindings = null;
                    pendingLoadedCliqueBindingsComplete = false;
                    return;
                }

                if (!pendingLoadedCliqueBindingsComplete ||
                    pendingLoadedCliqueBindings == null ||
                    savedData == null ||
                    savedData.Relationships__Cliques == null ||
                    Relationships.Cliques == null)
                {
                    return;
                }

                for (int index = 0; index < pendingLoadedCliqueBindings.Count; index++)
                {
                    LightweightIdentityBindingRecord binding =
                        pendingLoadedCliqueBindings[index];
                    if (binding == null ||
                        binding.ContainerOrdinal < CoreConstants.ZeroBasedListStartIndex ||
                        binding.ContainerOrdinal >= savedData.Relationships__Cliques.Count ||
                        binding.ContainerOrdinal >= Relationships.Cliques.Count)
                    {
                        continue;
                    }

                    Relationships.SaveData_Clique savedRow =
                        savedData.Relationships__Cliques[binding.ContainerOrdinal];
                    Relationships._clique liveClique =
                        Relationships.Cliques[binding.ContainerOrdinal];
                    string expectedFingerprint =
                        BuildCliqueIdentityValidationFingerprint(savedRow);
                    if (!string.Equals(
                            expectedFingerprint,
                            binding.ValidationFingerprint,
                            StringComparison.Ordinal) ||
                        !CliqueSavedRowMatchesLive(savedRow, liveClique))
                    {
                        continue;
                    }

                    cliqueIdentityByReference[liveClique] =
                        new CliqueIdentityRuntimeBinding
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

                pendingLoadedCliqueBindings = null;
                pendingLoadedCliqueBindingsComplete = false;
            }
        }

        private static LightweightIdentityBindingRecord CloneCliqueIdentityBinding(
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

        private static SaveManager.SavedData ResolveCurrentSavedDataForCliqueIdentity()
        {
            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            return main != null ? main.GetSavedData() : null;
        }

        private void ResetCliqueIdentityRuntimeStateLocked()
        {
            cliqueIdentityByReference.Clear();
            pendingLoadedCliqueBindings = null;
            pendingLoadedCliqueBindingsComplete = false;
        }
    }
}
