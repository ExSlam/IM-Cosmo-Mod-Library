using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Sidecar-v6 bounded historical-baseline carrier for finding #63.
    /// Assertions are rewindable branch records in the shared sequence space.
    /// They are deliberately not a generic current-state bootstrap bag.
    /// </summary>
    internal static class LightweightHistoricalBaselineSchema
    {
        internal const int AssertionSchemaVersion = 1;
        internal const string BaselineKindGroupTargetAudienceOrigin =
            "group_target_audience_origin";
        internal const string QualityExact = "Exact";
        internal const string QualityAmbiguous = "Ambiguous";
        internal const string QualityUnknown = "Unknown";
        internal const string SourceObservedCreation = "observed_creation";
        internal const string SourceRepairedSaveWitness = "repaired_save_witness";
        internal const string SourceLegacyPointPattern = "legacy_point_pattern";
        internal const string AssertionIdPrefix = "baseline-v1:";
        internal const int MaximumEvidenceTokenLength = 64;

        internal static string BuildAssertionId(
            LightweightHistoricalBaselineAssertionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            StringBuilder builder = new StringBuilder(512);
            AppendCanonicalField(builder, record.BaselineKind ?? string.Empty);
            AppendCanonicalField(builder, record.EntityKind ?? string.Empty);
            AppendCanonicalField(builder, record.EntityId ?? string.Empty);
            AppendCanonicalField(
                builder,
                record.AssertionSchemaVersion.ToString(CultureInfo.InvariantCulture));
            AppendCanonicalField(builder, record.Quality ?? string.Empty);
            AppendCanonicalField(builder, record.GroupAppealGender ?? string.Empty);
            AppendCanonicalField(
                builder,
                record.GroupAppealHardcoreness ?? string.Empty);
            AppendCanonicalField(builder, record.GroupAppealAge ?? string.Empty);
            AppendCanonicalList(builder, record.GenderCandidateCodes);
            AppendCanonicalList(builder, record.HardcorenessCandidateCodes);
            AppendCanonicalList(builder, record.AgeCandidateCodes);
            AppendCanonicalField(builder, record.SourceKind ?? string.Empty);
            AppendCanonicalField(builder, record.AnchorCheckpointKey ?? string.Empty);

            byte[] payload = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                return AssertionIdPrefix + ToLowerHex(sha256.ComputeHash(payload));
            }
        }

        internal static IMDataCoreHistoricalBaselineAssertion ToPublicAssertion(
            LightweightHistoricalBaselineAssertionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }
            IMDataCoreHistoricalBaselineQuality quality;
            if (string.Equals(record.Quality, QualityExact, StringComparison.Ordinal))
            {
                quality = IMDataCoreHistoricalBaselineQuality.Exact;
            }
            else if (string.Equals(
                record.Quality,
                QualityAmbiguous,
                StringComparison.Ordinal))
            {
                quality = IMDataCoreHistoricalBaselineQuality.Ambiguous;
            }
            else if (string.Equals(
                record.Quality,
                QualityUnknown,
                StringComparison.Ordinal))
            {
                quality = IMDataCoreHistoricalBaselineQuality.Unknown;
            }
            else
            {
                throw new FormatException(
                    "A historical-baseline assertion has an unsupported public quality.");
            }

            return new IMDataCoreHistoricalBaselineAssertion
            {
                AssertionId = record.AssertionId,
                Sequence = record.Sequence,
                GameDateTime = record.GameDateTime,
                BaselineKind = record.BaselineKind,
                EntityKind = record.EntityKind,
                EntityId = record.EntityId,
                AssertionSchemaVersion = record.AssertionSchemaVersion,
                Quality = quality,
                GroupAppealGender = record.GroupAppealGender,
                GroupAppealHardcoreness = record.GroupAppealHardcoreness,
                GroupAppealAge = record.GroupAppealAge,
                GenderCandidateCodes = new List<string>(
                    record.GenderCandidateCodes ?? new List<string>()),
                HardcorenessCandidateCodes = new List<string>(
                    record.HardcorenessCandidateCodes ?? new List<string>()),
                AgeCandidateCodes = new List<string>(
                    record.AgeCandidateCodes ?? new List<string>()),
                SourceKind = record.SourceKind,
                AnchorCheckpointKey = record.AnchorCheckpointKey
            };
        }

        internal static void ValidateDocumentForV6(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new FormatException(
                    "The v6 historical-baseline document is missing.");
            }
            if (document.HistoricalBaselineAssertions == null)
            {
                throw new FormatException(
                    "The v6 HistoricalBaselineAssertions collection is missing.");
            }

            Dictionary<string, LightweightCheckpointRecord> anchors =
                BuildUniqueCheckpointAnchorMap(document.Checkpoints);
            HashSet<string> assertionIds =
                new HashSet<string>(StringComparer.Ordinal);
            long previousSequence = 0L;
            for (int index = 0;
                index < document.HistoricalBaselineAssertions.Count;
                index++)
            {
                LightweightHistoricalBaselineAssertionRecord assertion =
                    document.HistoricalBaselineAssertions[index];
                ValidateAssertion(assertion, anchors);
                if (assertion.Sequence <= previousSequence)
                {
                    throw new FormatException(
                        "Historical baseline assertions are not in increasing sequence order.");
                }
                previousSequence = assertion.Sequence;
                if (!assertionIds.Add(assertion.AssertionId))
                {
                    throw new FormatException(
                        "The v6 document contains a duplicate historical-baseline assertion identity.");
                }
            }
        }

        internal static void ValidateAssertion(
            LightweightHistoricalBaselineAssertionRecord assertion,
            IDictionary<string, LightweightCheckpointRecord> anchors)
        {
            if (assertion == null ||
                assertion.AssertionSchemaVersion != AssertionSchemaVersion ||
                assertion.Sequence <= 0L)
            {
                throw new FormatException(
                    "A historical-baseline assertion has invalid version or sequence metadata.");
            }

            DateTime parsedGameDate;
            if (!DateTime.TryParseExact(
                    assertion.GameDateTime ?? string.Empty,
                    CoreConstants.RoundTripDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsedGameDate))
            {
                throw new FormatException(
                    "A historical-baseline assertion has an invalid GameDateTime.");
            }

            int groupId;
            if (!string.Equals(
                    assertion.BaselineKind,
                    BaselineKindGroupTargetAudienceOrigin,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    assertion.EntityKind,
                    CoreConstants.EventEntityKindGroup,
                    StringComparison.Ordinal) ||
                !int.TryParse(
                    assertion.EntityId ?? string.Empty,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out groupId) ||
                groupId < 0)
            {
                throw new FormatException(
                    "A historical-baseline assertion is outside the allow-listed group-origin schema.");
            }

            ValidateCandidateList(
                assertion.GenderCandidateCodes,
                "gender");
            ValidateCandidateList(
                assertion.HardcorenessCandidateCodes,
                "hardcoreness");
            ValidateCandidateList(
                assertion.AgeCandidateCodes,
                "age");

            bool exact = string.Equals(
                assertion.Quality,
                QualityExact,
                StringComparison.Ordinal);
            bool ambiguous = string.Equals(
                assertion.Quality,
                QualityAmbiguous,
                StringComparison.Ordinal);
            bool unknown = string.Equals(
                assertion.Quality,
                QualityUnknown,
                StringComparison.Ordinal);
            if (!exact && !ambiguous && !unknown)
            {
                throw new FormatException(
                    "A historical-baseline assertion has an unsupported quality.");
            }

            int genderCount = assertion.GenderCandidateCodes.Count;
            int hardcorenessCount = assertion.HardcorenessCandidateCodes.Count;
            int ageCount = assertion.AgeCandidateCodes.Count;
            int totalCandidates = genderCount + hardcorenessCount + ageCount;

            if (exact)
            {
                if (genderCount != 1 || hardcorenessCount != 1 || ageCount != 1 ||
                    !string.Equals(
                        assertion.GroupAppealGender,
                        assertion.GenderCandidateCodes[0],
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        assertion.GroupAppealHardcoreness,
                        assertion.HardcorenessCandidateCodes[0],
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        assertion.GroupAppealAge,
                        assertion.AgeCandidateCodes[0],
                        StringComparison.Ordinal))
                {
                    throw new FormatException(
                        "An Exact group-origin assertion must carry exactly one proven candidate on every axis.");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(assertion.GroupAppealGender) ||
                    !string.IsNullOrEmpty(assertion.GroupAppealHardcoreness) ||
                    !string.IsNullOrEmpty(assertion.GroupAppealAge))
                {
                    throw new FormatException(
                        "A non-Exact group-origin assertion cannot select an authoritative appeal trio.");
                }

                if (ambiguous &&
                    (genderCount < 1 || hardcorenessCount < 1 || ageCount < 1 ||
                        (genderCount == 1 &&
                            hardcorenessCount == 1 &&
                            ageCount == 1)))
                {
                    throw new FormatException(
                        "An Ambiguous group-origin assertion requires complete candidate coverage with at least one multiply-positive axis.");
                }

                if (unknown &&
                    (totalCandidates < 1 ||
                        (genderCount > 0 &&
                            hardcorenessCount > 0 &&
                            ageCount > 0)))
                {
                    throw new FormatException(
                        "An Unknown group-origin assertion must preserve bounded evidence while leaving at least one axis unresolved.");
                }
            }

            bool observedCreation = string.Equals(
                assertion.SourceKind,
                SourceObservedCreation,
                StringComparison.Ordinal);
            bool repairedSave = string.Equals(
                assertion.SourceKind,
                SourceRepairedSaveWitness,
                StringComparison.Ordinal);
            bool legacyPointPattern = string.Equals(
                assertion.SourceKind,
                SourceLegacyPointPattern,
                StringComparison.Ordinal);
            if (!observedCreation && !repairedSave && !legacyPointPattern)
            {
                throw new FormatException(
                    "A historical-baseline assertion has an unsupported evidence source.");
            }
            if ((observedCreation || repairedSave) && !exact)
            {
                throw new FormatException(
                    "Observed-creation and repaired-save group-origin witnesses must be Exact.");
            }

            bool requiresAnchor = repairedSave || legacyPointPattern;
            if (requiresAnchor &&
                string.IsNullOrEmpty(assertion.AnchorCheckpointKey))
            {
                throw new FormatException(
                    "An adoption/load-derived historical baseline requires an exact checkpoint anchor.");
            }
            if (observedCreation &&
                !string.IsNullOrEmpty(assertion.AnchorCheckpointKey))
            {
                throw new FormatException(
                    "An observed-creation historical baseline cannot be backdated through a load checkpoint anchor.");
            }

            if (!string.IsNullOrEmpty(assertion.AnchorCheckpointKey))
            {
                LightweightCheckpointRecord checkpoint;
                if (anchors == null ||
                    !anchors.TryGetValue(
                        assertion.AnchorCheckpointKey,
                        out checkpoint) ||
                    checkpoint == null ||
                    checkpoint.Sequence >= assertion.Sequence)
                {
                    throw new FormatException(
                        "A historical-baseline checkpoint anchor does not resolve to one earlier exact checkpoint.");
                }
            }

            string expectedAssertionId = BuildAssertionId(assertion);
            if (!string.Equals(
                    assertion.AssertionId,
                    expectedAssertionId,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A historical-baseline assertion ID does not match its canonical content.");
            }
        }

        private static Dictionary<string, LightweightCheckpointRecord>
            BuildUniqueCheckpointAnchorMap(
                IList<LightweightCheckpointRecord> checkpoints)
        {
            Dictionary<string, LightweightCheckpointRecord> anchors =
                new Dictionary<string, LightweightCheckpointRecord>(
                    StringComparer.Ordinal);
            HashSet<string> duplicates =
                new HashSet<string>(StringComparer.Ordinal);
            if (checkpoints != null)
            {
                for (int index = 0; index < checkpoints.Count; index++)
                {
                    LightweightCheckpointRecord checkpoint = checkpoints[index];
                    if (checkpoint == null)
                    {
                        continue;
                    }
                    string key = LightweightCoverageSchema.BuildAnchorCheckpointKey(
                        checkpoint);
                    if (anchors.ContainsKey(key))
                    {
                        duplicates.Add(key);
                    }
                    else
                    {
                        anchors.Add(key, checkpoint);
                    }
                }
            }
            foreach (string duplicate in duplicates)
            {
                anchors.Remove(duplicate);
            }
            return anchors;
        }

        private static void ValidateCandidateList(
            IList<string> values,
            string axisName)
        {
            if (values == null)
            {
                throw new FormatException(
                    "A group-origin candidate list is missing for axis " +
                    axisName + ".");
            }
            string previous = string.Empty;
            for (int index = 0; index < values.Count; index++)
            {
                string value = values[index] ?? string.Empty;
                if (string.IsNullOrEmpty(value) ||
                    value.Length > MaximumEvidenceTokenLength ||
                    !string.Equals(
                        value,
                        CoreTokenUtility.SanitizeToken(
                            value,
                            MaximumEvidenceTokenLength),
                        StringComparison.Ordinal) ||
                    (index > 0 &&
                        StringComparer.Ordinal.Compare(previous, value) >= 0))
                {
                    throw new FormatException(
                        "A group-origin candidate list is invalid or non-canonical for axis " +
                        axisName + ".");
                }
                previous = value;
            }
        }

        private static void AppendCanonicalList(
            StringBuilder builder,
            IList<string> values)
        {
            if (values == null)
            {
                AppendCanonicalField(builder, string.Empty);
                return;
            }
            for (int index = 0; index < values.Count; index++)
            {
                AppendCanonicalField(builder, values[index] ?? string.Empty);
            }
            builder.Append('#');
        }

        private static void AppendCanonicalField(
            StringBuilder builder,
            string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append(';');
        }

        private static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null)
            {
                return string.Empty;
            }
            const string Hex = "0123456789abcdef";
            char[] output = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                output[index * 2] = Hex[value >> 4];
                output[(index * 2) + 1] = Hex[value & 0x0F];
            }
            return new string(output);
        }
    }

    [Serializable]
    internal sealed class LightweightHistoricalBaselineAssertionRecord
    {
        public string AssertionId = string.Empty;
        public long Sequence;
        public string GameDateTime = string.Empty;
        public string BaselineKind = string.Empty;
        public string EntityKind = string.Empty;
        public string EntityId = string.Empty;
        public int AssertionSchemaVersion;
        public string Quality = string.Empty;
        public string GroupAppealGender = string.Empty;
        public string GroupAppealHardcoreness = string.Empty;
        public string GroupAppealAge = string.Empty;
        public List<string> GenderCandidateCodes = new List<string>();
        public List<string> HardcorenessCandidateCodes = new List<string>();
        public List<string> AgeCandidateCodes = new List<string>();
        public string SourceKind = string.Empty;
        public string AnchorCheckpointKey = string.Empty;
    }
}
