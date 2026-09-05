using System;
using System.Collections.Generic;
using System.IO;

namespace IMDataCore
{
    /// <summary>
    /// Knownness carried forward while a released lightweight sidecar is decoded
    /// into the next logical schema.  Empty legacy collections are deliberately
    /// not treated as proof that the corresponding source schema knew the value.
    /// </summary>
    internal sealed class LegacyCheckpointMigrationMetadata
    {
        internal int CheckpointIndex;
        internal bool EnabledModsKnown;
        internal bool ContentFingerprintExact;
        internal bool AgencyRoomIdentitiesBound;
        internal int IdentityBindingsVersion;
        internal bool IdentityBindingsComplete;
    }

    /// <summary>
    /// One bounded v1-v5 conversion result.  The normal runtime reader remains an
    /// exact-current-schema reader; this object is the hand-off to the v6 storage
    /// foundation and is not itself published over the source generation.
    /// </summary>
    internal sealed class LegacySidecarMigrationResult
    {
        internal const int V6TargetFormatVersion =
            LightweightIdentityBindingSchema.SidecarFormatVersion;

        internal int SourceFormatVersion;
        internal int TargetFormatVersion = V6TargetFormatVersion;
        internal string SourceFormatName = string.Empty;
        internal LightweightSidecarDocument Document;
        internal List<LegacyCheckpointMigrationMetadata> Checkpoints =
            new List<LegacyCheckpointMigrationMetadata>();
    }

    /// <summary>
    /// Bounded one-time migration entry point for lightweight JSON generations.
    /// Pre-2.0 database persistence is intentionally outside this codec family.
    /// </summary>
    internal static class LegacySidecarMigration
    {
        internal const int MinimumSourceFormatVersion = 1;
        internal const int MaximumSourceFormatVersion = 5;
        internal const int TargetFormatVersion =
            LegacySidecarMigrationResult.V6TargetFormatVersion;

        internal static bool TryDecodeToV6LogicalDocument(
            TextReader reader,
            out LegacySidecarMigrationResult result,
            out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;
            if (reader == null)
            {
                errorMessage = "The legacy sidecar reader is null.";
                return false;
            }

            try
            {
                result = LightweightSidecarJson.DeserializeForMigration(reader);
                return result != null;
            }
            catch (Exception exception)
            {
                errorMessage =
                    "The lightweight sidecar could not be migrated: " +
                    exception.Message;
                result = null;
                return false;
            }
        }
    }
}
