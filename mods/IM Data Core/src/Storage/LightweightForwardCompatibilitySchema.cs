using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Forward-only extension lane for the current sidecar-v6 / journal-v3
    /// generation. This is not a legacy migration surface.
    ///
    /// Future IMDataCore builds may append opaque optional extension records while
    /// retaining the v6/v3 framing. Older v6/v3 readers preserve those records
    /// as the same opaque PayloadJson string value without interpreting them.
    /// Any extension whose semantics are required to interpret the durable state
    /// must set RequiredForRead=true; a reader that does not explicitly understand
    /// that extension fails closed and leaves the physical generation untouched.
    /// </summary>
    internal static class LightweightForwardCompatibilitySchema
    {
        internal const int CompatibilitySchemaVersion = 1;

        // Current v6/v3 has no semantic extension that is required for read.
        // Future builds that add one while retaining v6/v3 must add its stable ID
        // here in the build that understands it.
        private static readonly HashSet<string> SupportedRequiredExtensionIds =
            new HashSet<string>(StringComparer.Ordinal);

        internal static void InitializeCurrent(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            document.ForwardCompatibilitySchemaVersion =
                CompatibilitySchemaVersion;
            if (document.ForwardExtensions == null)
            {
                document.ForwardExtensions =
                    new List<LightweightForwardExtensionRecord>();
            }
        }

        internal static void ValidateDocumentForV6(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (document.ForwardCompatibilitySchemaVersion !=
                CompatibilitySchemaVersion)
            {
                throw new FormatException(
                    "The sidecar-v6 forward-compatibility schema version is unsupported.");
            }
            if (document.ForwardExtensions == null)
            {
                throw new FormatException(
                    "The sidecar-v6 forward-extension collection is missing.");
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < document.ForwardExtensions.Count; index++)
            {
                LightweightForwardExtensionRecord record =
                    document.ForwardExtensions[index];
                ValidateRecord(record);
                if (!ids.Add(record.ExtensionId))
                {
                    throw new FormatException(
                        "The sidecar-v6 forward-extension collection contains a duplicate ExtensionId.");
                }

                if (record.RequiredForRead &&
                    !SupportedRequiredExtensionIds.Contains(record.ExtensionId))
                {
                    throw new LightweightUnsupportedForwardExtensionException(
                        record.ExtensionId,
                        record.ExtensionSchemaVersion);
                }
            }
        }

        internal static void ValidateRecord(
            LightweightForwardExtensionRecord record)
        {
            if (record == null)
            {
                throw new FormatException(
                    "A sidecar-v6 forward-extension record is null.");
            }
            if (!IsValidExtensionId(record.ExtensionId))
            {
                throw new FormatException(
                    "A sidecar-v6 forward-extension record has an invalid ExtensionId.");
            }
            if (record.ExtensionSchemaVersion <= 0)
            {
                throw new FormatException(
                    "A sidecar-v6 forward-extension schema version must be positive.");
            }
            if (record.PayloadJson == null)
            {
                throw new FormatException(
                    "A sidecar-v6 forward-extension payload is null.");
            }
        }

        internal static bool IsSupportedRequiredExtension(
            LightweightForwardExtensionRecord record)
        {
            return record != null &&
                (!record.RequiredForRead ||
                 SupportedRequiredExtensionIds.Contains(record.ExtensionId));
        }

        private static bool IsValidExtensionId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 160)
            {
                return false;
            }

            bool hasNamespaceSeparator = false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool allowed =
                    (character >= 'a' && character <= 'z') ||
                    (character >= 'A' && character <= 'Z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '_' ||
                    character == '-' ||
                    character == ':' ||
                    character == '/';
                if (!allowed)
                {
                    return false;
                }
                if (character == '.' || character == ':' || character == '/')
                {
                    hasNamespaceSeparator = true;
                }
            }
            return hasNamespaceSeparator;
        }
    }

    [Serializable]
    internal sealed class LightweightForwardExtensionRecord
    {
        public string ExtensionId = string.Empty;
        public int ExtensionSchemaVersion = 1;
        public bool RequiredForRead;
        public string PayloadJson = string.Empty;
    }

    /// <summary>
    /// Typed preservation signal. The storage engine treats this exactly like an
    /// unsupported newer physical generation: do not recover an older backup and
    /// do not overwrite the current bytes.
    /// </summary>
    internal sealed class LightweightUnsupportedForwardExtensionException :
        FormatException
    {
        internal readonly string ExtensionId;
        internal readonly int ExtensionSchemaVersion;

        internal LightweightUnsupportedForwardExtensionException(
            string extensionId,
            int extensionSchemaVersion)
            : base(
                "The IMDC sidecar contains required forward extension '" +
                (extensionId ?? string.Empty) +
                "' schema " +
                extensionSchemaVersion.ToString(CultureInfo.InvariantCulture) +
                " which this build does not understand.")
        {
            ExtensionId = extensionId ?? string.Empty;
            ExtensionSchemaVersion = extensionSchemaVersion;
        }
    }
}
