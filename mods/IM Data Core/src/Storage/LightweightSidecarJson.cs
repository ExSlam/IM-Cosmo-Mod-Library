using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Schema-specific JSON codec for the lightweight IM Data Core sidecar.
    ///
    /// This deliberately does not use UnityEngine.JsonUtility. The sidecar is
    /// private IMDC data, and relying on Unity's serializer allowed supported
    /// scalar fields to be written while the record collections were silently
    /// omitted in the actual Idol Manager runtime.
    ///
    /// The codec always writes Checkpoints, Events, and CustomMutations, even
    /// when they are empty. Deserialization rejects a document that omits any of
    /// those required collections so a header-only/truncated sidecar can never
    /// be accepted as a valid empty history.
    /// </summary>
    internal static class LightweightSidecarJson
    {
        /// <summary>
        /// Streams one sidecar directly to a writer. This keeps peak save-time
        /// memory proportional to the largest individual record instead of the
        /// complete campaign history.
        /// </summary>
        internal static void SerializeTo(
            TextWriter writer,
            LightweightSidecarDocument document)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            ValidateSerializableDocument(document);

            StringBuilder fragment = new StringBuilder(512);
            char[] fragmentBuffer = new char[1024];
            writer.Write('{');

            fragment.Length = 0;
            AppendPropertyName(fragment, "FormatName");
            AppendString(fragment, document.FormatName ?? string.Empty);
            fragment.Append(',');
            AppendPropertyName(fragment, "FormatVersion");
            AppendInt32(fragment, document.FormatVersion);
            fragment.Append(',');
            AppendPropertyName(fragment, "RelativeSavePath");
            AppendString(fragment, document.RelativeSavePath ?? string.Empty);
            fragment.Append(',');
            AppendPropertyName(fragment, "LastIssuedSequence");
            AppendInt64(fragment, document.LastIssuedSequence);
            fragment.Append(',');
            AppendPropertyName(fragment, "Checkpoints");
            WriteBuilder(writer, fragment, ref fragmentBuffer);

            writer.Write('[');
            for (int index = 0; index < document.Checkpoints.Count; index++)
            {
                if (index > 0)
                {
                    writer.Write(',');
                }
                fragment.Length = 0;
                AppendCheckpointRecord(fragment, document.Checkpoints[index]);
                WriteBuilder(writer, fragment, ref fragmentBuffer);
            }
            writer.Write(']');
            writer.Write(',');

            fragment.Length = 0;
            AppendPropertyName(fragment, "Events");
            WriteBuilder(writer, fragment, ref fragmentBuffer);
            writer.Write('[');
            for (int index = 0; index < document.Events.Count; index++)
            {
                if (index > 0)
                {
                    writer.Write(',');
                }
                fragment.Length = 0;
                AppendEventRecord(fragment, document.Events[index]);
                WriteBuilder(writer, fragment, ref fragmentBuffer);
            }
            writer.Write(']');
            writer.Write(',');

            fragment.Length = 0;
            AppendPropertyName(fragment, "CustomMutations");
            WriteBuilder(writer, fragment, ref fragmentBuffer);
            writer.Write('[');
            for (int index = 0; index < document.CustomMutations.Count; index++)
            {
                if (index > 0)
                {
                    writer.Write(',');
                }
                fragment.Length = 0;
                AppendCustomMutationRecord(
                    fragment,
                    document.CustomMutations[index]);
                WriteBuilder(writer, fragment, ref fragmentBuffer);
            }
            writer.Write(']');
            writer.Write('}');
        }

        internal static string SerializeJournalHeader(string baseFileHash)
        {
            StringBuilder builder = new StringBuilder(192);
            builder.Append('{');
            AppendPropertyName(builder, "FormatName");
            AppendString(builder, LightweightCoreStorageEngine.JournalFormatName);
            builder.Append(',');
            AppendPropertyName(builder, "FormatVersion");
            AppendInt32(builder, LightweightCoreStorageEngine.JournalFormatVersion);
            builder.Append(',');
            AppendPropertyName(builder, "BaseFileHash");
            AppendString(builder, baseFileHash ?? string.Empty);
            builder.Append('}');
            return builder.ToString();
        }

        internal static bool TryReadJournalHeader(
            string json,
            out string baseFileHash,
            out int formatVersion,
            out string errorMessage)
        {
            baseFileHash = string.Empty;
            formatVersion = 0;
            errorMessage = string.Empty;
            try
            {
                JsonValue rootValue = new JsonParser(json).ParseDocument();
                Dictionary<string, JsonValue> root = RequireObject(
                    rootValue,
                    "The IMDC journal header must be a JSON object.");
                string formatName = RequireString(root, "FormatName");
                formatVersion = RequireInt32(root, "FormatVersion");
                if (!string.Equals(
                        formatName,
                        LightweightCoreStorageEngine.JournalFormatName,
                        StringComparison.Ordinal) ||
                    formatVersion !=
                        LightweightCoreStorageEngine.JournalFormatVersion)
                {
                    errorMessage = "The IMDC journal format is unsupported.";
                    return false;
                }

                baseFileHash = RequireString(root, "BaseFileHash");
                if (string.IsNullOrEmpty(baseFileHash))
                {
                    errorMessage = "The IMDC journal base hash is empty.";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Writes one v2 journal transaction as bounded NDJSON records. A commit
        /// marker is the logical durability boundary, so replay can ignore a
        /// partially written transaction without ever materializing one giant
        /// save-delta string.
        /// </summary>
        internal static void SerializeJournalTransactionTo(
            TextWriter writer,
            LightweightSidecarDocument document,
            int checkpointStartIndex,
            int eventStartIndex,
            int customMutationStartIndex,
            int baseCheckpointCount,
            int baseEventCount,
            int baseCustomMutationCount)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            ValidateSerializableDocument(document);
            if (checkpointStartIndex < 0 ||
                checkpointStartIndex > document.Checkpoints.Count ||
                eventStartIndex < 0 ||
                eventStartIndex > document.Events.Count ||
                customMutationStartIndex < 0 ||
                customMutationStartIndex > document.CustomMutations.Count ||
                baseCheckpointCount < 0 ||
                baseEventCount < 0 ||
                baseCustomMutationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "A journal transaction count or start index is invalid.");
            }

            int targetCheckpointCount = checked(
                baseCheckpointCount +
                document.Checkpoints.Count - checkpointStartIndex);
            int targetEventCount = checked(
                baseEventCount + document.Events.Count - eventStartIndex);
            int targetCustomMutationCount = checked(
                baseCustomMutationCount +
                document.CustomMutations.Count - customMutationStartIndex);

            StringBuilder fragment = new StringBuilder(512);
            char[] fragmentBuffer = new char[1024];
            AppendJournalTransactionBoundary(
                fragment,
                "BEGIN",
                baseCheckpointCount,
                baseEventCount,
                baseCustomMutationCount,
                targetCheckpointCount,
                targetEventCount,
                targetCustomMutationCount,
                document.LastIssuedSequence,
                true);
            WriteBuilder(writer, fragment, ref fragmentBuffer);
            writer.Write('\n');

            for (int index = checkpointStartIndex;
                index < document.Checkpoints.Count;
                index++)
            {
                fragment.Length = 0;
                fragment.Append('{');
                AppendPropertyName(fragment, "Kind");
                AppendString(fragment, "CHECKPOINT");
                fragment.Append(',');
                AppendPropertyName(fragment, "Record");
                AppendCheckpointRecord(fragment, document.Checkpoints[index]);
                fragment.Append('}');
                WriteBuilder(writer, fragment, ref fragmentBuffer);
                writer.Write('\n');
            }

            for (int index = eventStartIndex;
                index < document.Events.Count;
                index++)
            {
                fragment.Length = 0;
                fragment.Append('{');
                AppendPropertyName(fragment, "Kind");
                AppendString(fragment, "EVENT");
                fragment.Append(',');
                AppendPropertyName(fragment, "Record");
                AppendEventRecord(fragment, document.Events[index]);
                fragment.Append('}');
                WriteBuilder(writer, fragment, ref fragmentBuffer);
                writer.Write('\n');
            }

            for (int index = customMutationStartIndex;
                index < document.CustomMutations.Count;
                index++)
            {
                fragment.Length = 0;
                fragment.Append('{');
                AppendPropertyName(fragment, "Kind");
                AppendString(fragment, "CUSTOM_MUTATION");
                fragment.Append(',');
                AppendPropertyName(fragment, "Record");
                AppendCustomMutationRecord(
                    fragment,
                    document.CustomMutations[index]);
                fragment.Append('}');
                WriteBuilder(writer, fragment, ref fragmentBuffer);
                writer.Write('\n');
            }

            fragment.Length = 0;
            AppendJournalTransactionBoundary(
                fragment,
                "COMMIT",
                baseCheckpointCount,
                baseEventCount,
                baseCustomMutationCount,
                targetCheckpointCount,
                targetEventCount,
                targetCustomMutationCount,
                document.LastIssuedSequence,
                false);
            WriteBuilder(writer, fragment, ref fragmentBuffer);
            writer.Write('\n');
        }

        internal static bool TryReplayJournalTransactions(
            TextReader reader,
            bool journalEndsWithNewline,
            LightweightSidecarDocument document,
            out int journalEntryCount,
            out bool forceFullSnapshot,
            out string errorMessage)
        {
            journalEntryCount = 0;
            forceFullSnapshot = false;
            errorMessage = string.Empty;
            if (reader == null || document == null)
            {
                errorMessage = "The IMDC journal replay input is invalid.";
                return false;
            }

            bool transactionOpen = false;
            bool applyTransaction = false;
            int baseCheckpointCount = 0;
            int baseEventCount = 0;
            int baseCustomMutationCount = 0;
            int targetCheckpointCount = 0;
            int targetEventCount = 0;
            int targetCustomMutationCount = 0;
            long targetLastIssuedSequence = 0L;
            int observedCheckpointCount = 0;
            int observedEventCount = 0;
            int observedCustomMutationCount = 0;
            List<LightweightCheckpointRecord> pendingCheckpoints = null;
            List<LightweightEventRecord> pendingEvents = null;
            List<LightweightCustomMutationRecord> pendingCustomMutations = null;

            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                bool isPhysicalTail = reader.Peek() < 0;
                try
                {
                    JsonValue rootValue = new JsonParser(line).ParseDocument();
                    Dictionary<string, JsonValue> root = RequireObject(
                        rootValue,
                        "An IMDC journal v2 row must be a JSON object.");
                    string kind = RequireString(root, "Kind");

                    if (!transactionOpen)
                    {
                        if (!string.Equals(kind, "BEGIN", StringComparison.Ordinal))
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction must begin with BEGIN.");
                        }

                        baseCheckpointCount = RequireInt32(
                            root,
                            "BaseCheckpointCount");
                        baseEventCount = RequireInt32(root, "BaseEventCount");
                        baseCustomMutationCount = RequireInt32(
                            root,
                            "BaseCustomMutationCount");
                        targetCheckpointCount = RequireInt32(
                            root,
                            "TargetCheckpointCount");
                        targetEventCount = RequireInt32(
                            root,
                            "TargetEventCount");
                        targetCustomMutationCount = RequireInt32(
                            root,
                            "TargetCustomMutationCount");
                        targetLastIssuedSequence = RequireInt64(
                            root,
                            "LastIssuedSequence");

                        if (baseCheckpointCount < 0 || baseEventCount < 0 ||
                            baseCustomMutationCount < 0 ||
                            targetCheckpointCount < baseCheckpointCount ||
                            targetEventCount < baseEventCount ||
                            targetCustomMutationCount < baseCustomMutationCount ||
                            targetLastIssuedSequence < document.LastIssuedSequence)
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction has invalid count or sequence bounds.");
                        }

                        bool startsAtCurrentState =
                            document.Checkpoints.Count == baseCheckpointCount &&
                            document.Events.Count == baseEventCount &&
                            document.CustomMutations.Count == baseCustomMutationCount;
                        bool isAlreadyApplied =
                            document.Checkpoints.Count == targetCheckpointCount &&
                            document.Events.Count == targetEventCount &&
                            document.CustomMutations.Count == targetCustomMutationCount &&
                            document.LastIssuedSequence == targetLastIssuedSequence;
                        if (!startsAtCurrentState && !isAlreadyApplied)
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction does not continue the current durable state.");
                        }

                        applyTransaction = startsAtCurrentState;
                        observedCheckpointCount = 0;
                        observedEventCount = 0;
                        observedCustomMutationCount = 0;
                        pendingCheckpoints = applyTransaction
                            ? new List<LightweightCheckpointRecord>()
                            : null;
                        pendingEvents = applyTransaction
                            ? new List<LightweightEventRecord>()
                            : null;
                        pendingCustomMutations = applyTransaction
                            ? new List<LightweightCustomMutationRecord>()
                            : null;
                        transactionOpen = true;
                        continue;
                    }

                    if (string.Equals(kind, "CHECKPOINT", StringComparison.Ordinal))
                    {
                        observedCheckpointCount++;
                        if (observedCheckpointCount >
                            targetCheckpointCount - baseCheckpointCount)
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction contains too many checkpoint rows.");
                        }
                        if (applyTransaction)
                        {
                            pendingCheckpoints.Add(ReadCheckpoint(
                                RequireMember(root, "Record"),
                                document.RelativeSavePath));
                        }
                    }
                    else if (string.Equals(kind, "EVENT", StringComparison.Ordinal))
                    {
                        observedEventCount++;
                        if (observedEventCount > targetEventCount - baseEventCount)
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction contains too many event rows.");
                        }
                        if (applyTransaction)
                        {
                            pendingEvents.Add(ReadEvent(
                                RequireMember(root, "Record")));
                        }
                    }
                    else if (string.Equals(
                        kind,
                        "CUSTOM_MUTATION",
                        StringComparison.Ordinal))
                    {
                        observedCustomMutationCount++;
                        if (observedCustomMutationCount >
                            targetCustomMutationCount - baseCustomMutationCount)
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction contains too many custom mutation rows.");
                        }
                        if (applyTransaction)
                        {
                            pendingCustomMutations.Add(ReadCustomMutation(
                                RequireMember(root, "Record")));
                        }
                    }
                    else if (string.Equals(kind, "COMMIT", StringComparison.Ordinal))
                    {
                        int committedTargetCheckpointCount = RequireInt32(
                            root,
                            "TargetCheckpointCount");
                        int committedTargetEventCount = RequireInt32(
                            root,
                            "TargetEventCount");
                        int committedTargetCustomMutationCount = RequireInt32(
                            root,
                            "TargetCustomMutationCount");
                        long committedLastIssuedSequence = RequireInt64(
                            root,
                            "LastIssuedSequence");

                        if (committedTargetCheckpointCount != targetCheckpointCount ||
                            committedTargetEventCount != targetEventCount ||
                            committedTargetCustomMutationCount !=
                                targetCustomMutationCount ||
                            committedLastIssuedSequence != targetLastIssuedSequence ||
                            observedCheckpointCount !=
                                targetCheckpointCount - baseCheckpointCount ||
                            observedEventCount != targetEventCount - baseEventCount ||
                            observedCustomMutationCount !=
                                targetCustomMutationCount - baseCustomMutationCount)
                        {
                            throw new FormatException(
                                "An IMDC journal v2 transaction commit does not match its BEGIN boundary.");
                        }

                        if (applyTransaction)
                        {
                            document.Checkpoints.AddRange(pendingCheckpoints);
                            document.Events.AddRange(pendingEvents);
                            document.CustomMutations.AddRange(
                                pendingCustomMutations);
                            document.LastIssuedSequence =
                                targetLastIssuedSequence;
                        }

                        journalEntryCount++;
                        transactionOpen = false;
                        applyTransaction = false;
                        pendingCheckpoints = null;
                        pendingEvents = null;
                        pendingCustomMutations = null;
                    }
                    else
                    {
                        throw new FormatException(
                            "An IMDC journal v2 transaction contains an unknown row kind.");
                    }
                }
                catch (Exception exception)
                {
                    if (isPhysicalTail && !journalEndsWithNewline)
                    {
                        forceFullSnapshot = true;
                        return true;
                    }

                    errorMessage = exception.Message;
                    return false;
                }
            }

            if (transactionOpen)
            {
                // A transaction without COMMIT is never visible, even if its last
                // complete row happened to reach disk before the process stopped.
                forceFullSnapshot = true;
            }

            return true;
        }

        private static void AppendJournalTransactionBoundary(
            StringBuilder builder,
            string kind,
            int baseCheckpointCount,
            int baseEventCount,
            int baseCustomMutationCount,
            int targetCheckpointCount,
            int targetEventCount,
            int targetCustomMutationCount,
            long lastIssuedSequence,
            bool includeBaseCounts)
        {
            builder.Length = 0;
            builder.Append('{');
            AppendPropertyName(builder, "Kind");
            AppendString(builder, kind);
            if (includeBaseCounts)
            {
                builder.Append(',');
                AppendPropertyName(builder, "BaseCheckpointCount");
                AppendInt32(builder, baseCheckpointCount);
                builder.Append(',');
                AppendPropertyName(builder, "BaseEventCount");
                AppendInt32(builder, baseEventCount);
                builder.Append(',');
                AppendPropertyName(builder, "BaseCustomMutationCount");
                AppendInt32(builder, baseCustomMutationCount);
            }
            builder.Append(',');
            AppendPropertyName(builder, "TargetCheckpointCount");
            AppendInt32(builder, targetCheckpointCount);
            builder.Append(',');
            AppendPropertyName(builder, "TargetEventCount");
            AppendInt32(builder, targetEventCount);
            builder.Append(',');
            AppendPropertyName(builder, "TargetCustomMutationCount");
            AppendInt32(builder, targetCustomMutationCount);
            builder.Append(',');
            AppendPropertyName(builder, "LastIssuedSequence");
            AppendInt64(builder, lastIssuedSequence);
            builder.Append('}');
        }

        private static void WriteBuilder(
            TextWriter writer,
            StringBuilder builder,
            ref char[] buffer)
        {
            if (builder == null || builder.Length == 0)
            {
                return;
            }

            if (buffer == null || buffer.Length < builder.Length)
            {
                int newLength = buffer == null || buffer.Length == 0
                    ? 1024
                    : buffer.Length;
                while (newLength < builder.Length)
                {
                    newLength = checked(newLength * 2);
                }
                buffer = new char[newLength];
            }

            builder.CopyTo(0, buffer, 0, builder.Length);
            writer.Write(buffer, 0, builder.Length);
        }

        private static void ValidateSerializableDocument(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (document.Checkpoints == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar Checkpoints collection is null.");
            }
            if (document.Events == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar Events collection is null.");
            }
            if (document.CustomMutations == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar CustomMutations collection is null.");
            }
        }

        /// <summary>
        /// Streams the sidecar from a reader and materializes one history record at
        /// a time. Normal v5 files therefore never require a second in-memory copy
        /// of the complete JSON text or a complete JSON DOM for all campaign rows.
        /// </summary>
        internal static LightweightSidecarDocument DeserializeFrom(
            TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            StreamingJsonParser parser = new StreamingJsonParser(reader);
            LightweightSidecarDocument document = new LightweightSidecarDocument
            {
                Checkpoints = new List<LightweightCheckpointRecord>(),
                Events = new List<LightweightEventRecord>(),
                CustomMutations = new List<LightweightCustomMutationRecord>()
            };

            HashSet<string> seenProperties =
                new HashSet<string>(StringComparer.Ordinal);
            bool hasFormatName = false;
            bool hasFormatVersion = false;
            bool hasRelativeSavePath = false;
            bool hasLastIssuedSequence = false;
            bool hasCheckpoints = false;
            bool hasEvents = false;
            bool hasCustomMutations = false;

            JsonValue deferredCheckpoints = null;
            JsonValue deferredEvents = null;
            JsonValue deferredCustomMutations = null;

            parser.SkipWhitespace();
            parser.Expect('{');
            parser.SkipWhitespace();
            if (!parser.TryConsume('}'))
            {
                while (true)
                {
                    string propertyName = parser.ParsePropertyName();
                    if (!seenProperties.Add(propertyName))
                    {
                        throw new FormatException(
                            "Duplicate JSON object property '" +
                            propertyName + "'.");
                    }

                    parser.Expect(':');
                    switch (propertyName)
                    {
                        case "FormatName":
                            document.FormatName =
                                parser.ParseRequiredString(propertyName);
                            hasFormatName = true;
                            break;

                        case "FormatVersion":
                            document.FormatVersion =
                                parser.ParseRequiredInt32(propertyName);
                            if (document.FormatVersion !=
                                LightweightCoreStorageEngine.SidecarFormatVersion)
                            {
                                throw new FormatException(
                                    "The lightweight sidecar format is unsupported by this IM Data Core version.");
                            }
                            hasFormatVersion = true;
                            break;

                        case "RelativeSavePath":
                            document.RelativeSavePath =
                                parser.ParseRequiredString(propertyName);
                            hasRelativeSavePath = true;
                            break;

                        case "LastIssuedSequence":
                            document.LastIssuedSequence =
                                parser.ParseRequiredInt64(propertyName);
                            hasLastIssuedSequence = true;
                            break;

                        case "Checkpoints":
                            hasCheckpoints = true;
                            if (hasFormatVersion && hasRelativeSavePath)
                            {
                                document.Checkpoints =
                                    ReadCheckpointArrayFromStream(
                                        parser,
                                        document.RelativeSavePath);
                            }
                            else
                            {
                                deferredCheckpoints = parser.ParseValue();
                            }
                            break;

                        case "Events":
                            hasEvents = true;
                            if (hasFormatVersion)
                            {
                                document.Events =
                                    ReadEventArrayFromStream(parser);
                            }
                            else
                            {
                                deferredEvents = parser.ParseValue();
                            }
                            break;

                        case "CustomMutations":
                            hasCustomMutations = true;
                            if (hasFormatVersion)
                            {
                                document.CustomMutations =
                                    ReadCustomMutationArrayFromStream(parser);
                            }
                            else
                            {
                                deferredCustomMutations = parser.ParseValue();
                            }
                            break;

                        default:
                            // Tolerate unknown top-level fields within the current v5 schema
                            // while still validating their JSON syntax.
                            parser.ParseValue();
                            break;
                    }

                    parser.SkipWhitespace();
                    if (parser.TryConsume('}'))
                    {
                        break;
                    }

                    parser.Expect(',');
                }
            }

            parser.EnsureEnd();

            if (!hasFormatName ||
                !hasFormatVersion ||
                !hasRelativeSavePath ||
                !hasLastIssuedSequence ||
                !hasCheckpoints ||
                !hasEvents ||
                !hasCustomMutations)
            {
                throw new FormatException(
                    "The lightweight sidecar is missing one or more required fields.");
            }

            if (deferredCheckpoints != null)
            {
                if (deferredCheckpoints.Kind != JsonValueKind.Array)
                {
                    throw new FormatException(
                        "The lightweight sidecar field 'Checkpoints' must be a JSON array.");
                }

                document.Checkpoints = ReadCheckpoints(
                    deferredCheckpoints.ArrayValue,
                    document.RelativeSavePath);
            }

            if (deferredEvents != null)
            {
                if (deferredEvents.Kind != JsonValueKind.Array)
                {
                    throw new FormatException(
                        "The lightweight sidecar field 'Events' must be a JSON array.");
                }

                document.Events = ReadEvents(deferredEvents.ArrayValue);
            }

            if (deferredCustomMutations != null)
            {
                if (deferredCustomMutations.Kind != JsonValueKind.Array)
                {
                    throw new FormatException(
                        "The lightweight sidecar field 'CustomMutations' must be a JSON array.");
                }

                document.CustomMutations = ReadCustomMutations(
                    deferredCustomMutations.ArrayValue);
            }

            return document;
        }

        private static List<LightweightCheckpointRecord>
            ReadCheckpointArrayFromStream(
                StreamingJsonParser parser,
                string documentRelativeSavePath)
        {
            List<LightweightCheckpointRecord> records =
                new List<LightweightCheckpointRecord>();
            parser.Expect('[');
            parser.SkipWhitespace();
            if (parser.TryConsume(']'))
            {
                return records;
            }

            while (true)
            {
                records.Add(
                    ReadCheckpoint(
                        parser.ParseValue(),
                        documentRelativeSavePath));
                parser.SkipWhitespace();
                if (parser.TryConsume(']'))
                {
                    break;
                }

                parser.Expect(',');
            }

            return records;
        }

        private static List<LightweightEventRecord> ReadEventArrayFromStream(
            StreamingJsonParser parser)
        {
            List<LightweightEventRecord> records =
                new List<LightweightEventRecord>();
            parser.Expect('[');
            parser.SkipWhitespace();
            if (parser.TryConsume(']'))
            {
                return records;
            }

            while (true)
            {
                records.Add(ReadEvent(parser.ParseValue()));
                parser.SkipWhitespace();
                if (parser.TryConsume(']'))
                {
                    break;
                }

                parser.Expect(',');
            }

            return records;
        }

        private static List<LightweightCustomMutationRecord>
            ReadCustomMutationArrayFromStream(
                StreamingJsonParser parser)
        {
            List<LightweightCustomMutationRecord> records =
                new List<LightweightCustomMutationRecord>();
            parser.Expect('[');
            parser.SkipWhitespace();
            if (parser.TryConsume(']'))
            {
                return records;
            }

            while (true)
            {
                records.Add(
                    ReadCustomMutation(parser.ParseValue()));
                parser.SkipWhitespace();
                if (parser.TryConsume(']'))
                {
                    break;
                }

                parser.Expect(',');
            }

            return records;
        }

        /// <summary>
        /// Validates and normalizes one arbitrary JSON document. The public API
        /// still exchanges JSON as strings because that is a convenient mod boundary,
        /// while the v5 sidecar stores the parsed value structurally.
        /// </summary>
        internal static bool TryNormalizeJsonDocument(
            string json,
            out string normalizedJson,
            out string errorMessage)
        {
            normalizedJson = string.Empty;
            errorMessage = string.Empty;
            if (json == null)
            {
                errorMessage = "JSON value cannot be null.";
                return false;
            }

            try
            {
                JsonValue value = new JsonParser(json).ParseDocument();
                normalizedJson = SerializeJsonValue(value);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = "The value is not valid JSON: " + exception.Message;
                return false;
            }
        }


        internal static string CompactMoneyTransactionPayload(
            string payloadJson,
            out bool changed)
        {
            changed = false;
            if (string.IsNullOrEmpty(payloadJson))
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue outerValue = new JsonParser(payloadJson).ParseDocument();
                if (outerValue == null || outerValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                JsonValue detailMember;
                if (!outerValue.ObjectValue.TryGetValue(
                        "detail_json",
                        out detailMember) ||
                    detailMember == null ||
                    detailMember.Kind != JsonValueKind.String ||
                    string.IsNullOrEmpty(detailMember.StringValue))
                {
                    return payloadJson;
                }

                JsonValue detailValue =
                    new JsonParser(detailMember.StringValue).ParseDocument();
                if (detailValue == null || detailValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                List<string> keysToRemove = new List<string>();
                foreach (KeyValuePair<string, JsonValue> pair
                    in detailValue.ObjectValue)
                {
                    if (string.Equals(
                            pair.Key,
                            "kind",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (IsMoneyDetailDefault(pair.Key, pair.Value))
                    {
                        keysToRemove.Add(pair.Key);
                    }
                }

                if (keysToRemove.Count == 0)
                {
                    return payloadJson;
                }

                for (int index = 0; index < keysToRemove.Count; index++)
                {
                    detailValue.ObjectValue.Remove(keysToRemove[index]);
                }

                outerValue.ObjectValue["detail_json"] = new JsonValue
                {
                    Kind = JsonValueKind.String,
                    StringValue = SerializeJsonValue(detailValue)
                };

                changed = true;
                return SerializeJsonValue(outerValue);
            }
            catch
            {
                // Payload compaction is an optimization. Preserve the original
                // capture verbatim if a future payload shape is not understood.
                changed = false;
                return payloadJson;
            }
        }

        internal static bool TryReadCsvIntProperty(
            string json,
            string propertyName,
            int minimumValue,
            out List<int> values)
        {
            values = new List<int>();
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                JsonValue root = new JsonParser(json).ParseDocument();
                if (root == null || root.Kind != JsonValueKind.Object)
                {
                    return false;
                }

                JsonValue property;
                if (!root.ObjectValue.TryGetValue(propertyName, out property) ||
                    property == null ||
                    property.Kind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(property.StringValue))
                {
                    return false;
                }

                HashSet<int> uniqueValues = new HashSet<int>();
                string[] tokens = property.StringValue.Split(',');
                for (int index = 0; index < tokens.Length; index++)
                {
                    int parsed;
                    if (int.TryParse(
                            tokens[index].Trim(),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out parsed) &&
                        parsed >= minimumValue &&
                        uniqueValues.Add(parsed))
                    {
                        values.Add(parsed);
                    }
                }

                return values.Count > 0;
            }
            catch
            {
                values.Clear();
                return false;
            }
        }

        internal static bool TryReadIntProperty(
            string json,
            string propertyName,
            out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                JsonValue root = new JsonParser(json).ParseDocument();
                JsonValue property;
                long parsed;
                if (root == null ||
                    root.Kind != JsonValueKind.Object ||
                    !root.ObjectValue.TryGetValue(propertyName, out property) ||
                    property == null ||
                    property.Kind != JsonValueKind.Number ||
                    !long.TryParse(
                        property.NumberValue,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsed) ||
                    parsed < int.MinValue ||
                    parsed > int.MaxValue)
                {
                    return false;
                }

                value = (int)parsed;
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        internal static bool TryReadStringProperty(
            string json,
            string propertyName,
            out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                JsonValue root = new JsonParser(json).ParseDocument();
                JsonValue property;
                if (root == null ||
                    root.Kind != JsonValueKind.Object ||
                    !root.ObjectValue.TryGetValue(propertyName, out property) ||
                    property == null ||
                    property.Kind != JsonValueKind.String)
                {
                    return false;
                }

                value = property.StringValue ?? string.Empty;
                return true;
            }
            catch
            {
                value = string.Empty;
                return false;
            }
        }

        internal static string ExpandMoneyTransactionPayloadForPublic(
            string payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson))
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue outerValue = new JsonParser(payloadJson).ParseDocument();
                if (outerValue == null || outerValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                JsonValue detailMember;
                if (!outerValue.ObjectValue.TryGetValue(
                        "detail_json",
                        out detailMember) ||
                    detailMember == null ||
                    detailMember.Kind != JsonValueKind.String ||
                    string.IsNullOrEmpty(detailMember.StringValue))
                {
                    return payloadJson;
                }

                JsonValue detailValue =
                    new JsonParser(detailMember.StringValue).ParseDocument();
                if (detailValue == null || detailValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                AddMoneyDetailDefaults(detailValue.ObjectValue);
                outerValue.ObjectValue["detail_json"] = new JsonValue
                {
                    Kind = JsonValueKind.String,
                    StringValue = SerializeJsonValue(detailValue)
                };
                return SerializeJsonValue(outerValue);
            }
            catch
            {
                return payloadJson;
            }
        }

        internal static string ExpandSingleReleasePayloadForPublic(
            string payloadJson,
            int idolId,
            int positionIndex,
            int rowIndex,
            bool isCenter)
        {
            if (string.IsNullOrEmpty(payloadJson) ||
                idolId < CoreConstants.MinimumValidIdolIdentifier ||
                positionIndex < CoreConstants.ZeroBasedListStartIndex ||
                rowIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue payloadValue =
                    new JsonParser(payloadJson).ParseDocument();
                if (payloadValue == null ||
                    payloadValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                // Keep the ordered cast list public: it is the minimal historical
                // source needed to rebuild the complete release-time senbatsu,
                // including members vanilla later replaces with null.
                payloadValue.ObjectValue[CoreConstants.JsonFieldIdolId] =
                    CreateNumberValue(idolId);
                payloadValue.ObjectValue[CoreConstants.JsonFieldPositionIndex] =
                    CreateNumberValue(positionIndex);
                payloadValue.ObjectValue[CoreConstants.JsonFieldRowIndex] =
                    CreateNumberValue(rowIndex);
                payloadValue.ObjectValue[CoreConstants.JsonFieldIsCenter] =
                    new JsonValue
                    {
                        Kind = JsonValueKind.Boolean,
                        BooleanValue = isCenter
                    };

                return SerializeJsonValue(payloadValue);
            }
            catch
            {
                // Public projection is fail-soft. The stored history remains
                // unchanged if a future payload shape cannot be expanded.
                return payloadJson;
            }
        }

        /// <summary>
        /// Adds or replaces integer fields on a cloned public payload. Shared
        /// timeline rows use this to reconstruct the small per-idol projection
        /// while leaving the persisted common snapshot unchanged.
        /// </summary>
        internal static string ExpandIntegerPayloadForPublic(
            string payloadJson,
            IReadOnlyDictionary<string, long> projectedValues)
        {
            if (string.IsNullOrEmpty(payloadJson) ||
                projectedValues == null ||
                projectedValues.Count == 0)
            {
                return payloadJson ?? string.Empty;
            }

            try
            {
                JsonValue payloadValue =
                    new JsonParser(payloadJson).ParseDocument();
                if (payloadValue == null ||
                    payloadValue.Kind != JsonValueKind.Object)
                {
                    return payloadJson;
                }

                foreach (KeyValuePair<string, long> projectedValue
                    in projectedValues)
                {
                    if (string.IsNullOrEmpty(projectedValue.Key))
                    {
                        continue;
                    }

                    payloadValue.ObjectValue[projectedValue.Key] =
                        CreateNumberValue(projectedValue.Value);
                }

                return SerializeJsonValue(payloadValue);
            }
            catch
            {
                // Projection is an API convenience. Never alter or reject the
                // durable row when a future payload shape is not understood.
                return payloadJson;
            }
        }

        private static JsonValue CreateNumberValue(int value)
        {
            return CreateNumberValue((long)value);
        }

        private static JsonValue CreateNumberValue(long value)
        {
            return new JsonValue
            {
                Kind = JsonValueKind.Number,
                NumberValue = value.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static bool IsMoneyDetailDefault(
            string fieldName,
            JsonValue value)
        {
            if (string.IsNullOrEmpty(fieldName) || value == null)
            {
                return false;
            }

            MoneyDetailDefaultKind defaultKind;
            if (!TryGetMoneyDetailDefaultKind(fieldName, out defaultKind))
            {
                // Future fields are retained until this compactor explicitly
                // knows their declared default, preventing accidental data loss.
                return false;
            }

            switch (defaultKind)
            {
                case MoneyDetailDefaultKind.EmptyString:
                    return value.Kind == JsonValueKind.String &&
                        string.IsNullOrEmpty(value.StringValue);

                case MoneyDetailDefaultKind.Zero:
                    double number;
                    return value.Kind == JsonValueKind.Number &&
                        double.TryParse(
                            value.NumberValue,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out number) &&
                        number == 0d;

                case MoneyDetailDefaultKind.MinusOne:
                    long integer;
                    return value.Kind == JsonValueKind.Number &&
                        long.TryParse(
                            value.NumberValue,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out integer) &&
                        integer == -1L;

                case MoneyDetailDefaultKind.False:
                    return value.Kind == JsonValueKind.Boolean &&
                        !value.BooleanValue;

                case MoneyDetailDefaultKind.EmptyArray:
                    return value.Kind == JsonValueKind.Array &&
                        (value.ArrayValue == null || value.ArrayValue.Count == 0);

                default:
                    return false;
            }
        }

        private static void AddMoneyDetailDefaults(
            Dictionary<string, JsonValue> fields)
        {
            if (fields == null)
            {
                return;
            }

            string[] fieldNames = MoneyDetailFieldNames;
            for (int index = 0; index < fieldNames.Length; index++)
            {
                string fieldName = fieldNames[index];
                if (fields.ContainsKey(fieldName))
                {
                    continue;
                }

                MoneyDetailDefaultKind defaultKind;
                if (TryGetMoneyDetailDefaultKind(fieldName, out defaultKind))
                {
                    fields.Add(fieldName, CreateMoneyDetailDefault(defaultKind));
                }
            }
        }

        private static JsonValue CreateMoneyDetailDefault(
            MoneyDetailDefaultKind defaultKind)
        {
            switch (defaultKind)
            {
                case MoneyDetailDefaultKind.EmptyString:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.String,
                        StringValue = string.Empty
                    };

                case MoneyDetailDefaultKind.MinusOne:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Number,
                        NumberValue = "-1"
                    };

                case MoneyDetailDefaultKind.False:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Boolean,
                        BooleanValue = false
                    };

                case MoneyDetailDefaultKind.EmptyArray:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Array,
                        ArrayValue = new List<JsonValue>()
                    };

                default:
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Number,
                        NumberValue = "0"
                    };
            }
        }

        private static bool TryGetMoneyDetailDefaultKind(
            string fieldName,
            out MoneyDetailDefaultKind defaultKind)
        {
            defaultKind = MoneyDetailDefaultKind.Zero;

            switch (fieldName)
            {
                case "contract_type_code":
                case "contractor_name":
                case "product_name":
                case "idol_name":
                case "single_title":
                case "single_group_name":
                case "single_genre_token":
                case "single_lyrics_token":
                case "single_choreography_token":
                case "show_title":
                case "show_medium_token":
                case "show_genre_token":
                case "show_host_token":
                case "staff_name":
                case "staff_role_code":
                case "theater_title":
                case "theater_income_type":
                case "theater_performance_type":
                case "theater_audience_type":
                case "cafe_title":
                case "cafe_dish_title":
                case "cafe_dish_type":
                case "cafe_appeal_type":
                case "concert_title":
                case "concert_venue":
                    defaultKind = MoneyDetailDefaultKind.EmptyString;
                    return true;

                case "idol_id":
                case "theater_id":
                case "cafe_id":
                case "concert_id":
                    defaultKind = MoneyDetailDefaultKind.MinusOne;
                    return true;

                case "single_marketing_tokens":
                case "participant_names":
                case "cafe_staff_names":
                    defaultKind = MoneyDetailDefaultKind.EmptyArray;
                    return true;

                case "has_fan_audience":
                case "concert_finished":
                    defaultKind = MoneyDetailDefaultKind.False;
                    return true;

                case "payment_amount":
                case "stamina_cost":
                case "liability_amount":
                case "multiplier":
                case "negotiations":
                case "gross_revenue":
                case "production_cost":
                case "show_episode_number":
                case "show_audience":
                case "show_fan_audience":
                case "show_fatigue":
                case "show_weekly_budget":
                case "salary_amount":
                case "idol_fame":
                case "idol_scandal_points":
                case "theater_ticket_price":
                case "theater_attendance":
                case "theater_subscription_price":
                case "theater_subscriber_delta":
                case "theater_subscriber_total":
                case "cafe_new_fans":
                case "concert_ticket_price":
                case "concert_projected_attendance":
                case "concert_projected_hype":
                case "concert_finished_hype":
                case "concert_finished_revenue":
                case "concert_finished_profit":
                case "concert_accident_count":
                case "concert_accident_successes":
                case "concert_accident_failures":
                case "concert_accident_critical_failures":
                    defaultKind = MoneyDetailDefaultKind.Zero;
                    return true;

                default:
                    return false;
            }
        }

        private static readonly string[] MoneyDetailFieldNames =
        {
            "contract_type_code",
            "contractor_name",
            "product_name",
            "payment_amount",
            "stamina_cost",
            "liability_amount",
            "idol_id",
            "idol_name",
            "multiplier",
            "negotiations",
            "single_title",
            "single_group_name",
            "single_genre_token",
            "single_lyrics_token",
            "single_choreography_token",
            "single_marketing_tokens",
            "participant_names",
            "gross_revenue",
            "production_cost",
            "show_title",
            "show_medium_token",
            "show_genre_token",
            "show_host_token",
            "show_episode_number",
            "show_audience",
            "has_fan_audience",
            "show_fan_audience",
            "show_fatigue",
            "show_weekly_budget",
            "staff_name",
            "staff_role_code",
            "salary_amount",
            "idol_fame",
            "idol_scandal_points",
            "theater_id",
            "theater_title",
            "theater_income_type",
            "theater_ticket_price",
            "theater_performance_type",
            "theater_audience_type",
            "theater_attendance",
            "theater_subscription_price",
            "theater_subscriber_delta",
            "theater_subscriber_total",
            "cafe_id",
            "cafe_title",
            "cafe_dish_title",
            "cafe_dish_type",
            "cafe_staff_names",
            "cafe_new_fans",
            "cafe_appeal_type",
            "concert_id",
            "concert_title",
            "concert_venue",
            "concert_ticket_price",
            "concert_projected_attendance",
            "concert_projected_hype",
            "concert_finished_hype",
            "concert_finished",
            "concert_finished_revenue",
            "concert_finished_profit",
            "concert_accident_count",
            "concert_accident_successes",
            "concert_accident_failures",
            "concert_accident_critical_failures"
        };

        private enum MoneyDetailDefaultKind
        {
            EmptyString,
            Zero,
            MinusOne,
            False,
            EmptyArray
        }

        private static string SerializeJsonValue(JsonValue value)
        {
            StringBuilder builder = new StringBuilder(256);
            AppendJsonValue(builder, value);
            return builder.ToString();
        }

        private static void AppendJsonValue(
            StringBuilder builder,
            JsonValue value)
        {
            if (value == null || value.Kind == JsonValueKind.Null)
            {
                builder.Append("null");
                return;
            }

            if (value.Kind == JsonValueKind.Object)
            {
                builder.Append('{');
                bool first = true;
                List<string> propertyNames = value.ObjectValue == null
                    ? new List<string>()
                    : new List<string>(value.ObjectValue.Keys);
                propertyNames.Sort(StringComparer.Ordinal);

                for (int index = 0; index < propertyNames.Count; index++)
                {
                    string propertyName = propertyNames[index];
                    JsonValue propertyValue = value.ObjectValue[propertyName];
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    AppendString(builder, propertyName);
                    builder.Append(':');
                    AppendJsonValue(builder, propertyValue);
                }

                builder.Append('}');
                return;
            }

            if (value.Kind == JsonValueKind.Array)
            {
                builder.Append('[');
                for (int index = 0;
                    value.ArrayValue != null && index < value.ArrayValue.Count;
                    index++)
                {
                    if (index > 0)
                    {
                        builder.Append(',');
                    }

                    AppendJsonValue(builder, value.ArrayValue[index]);
                }

                builder.Append(']');
                return;
            }

            if (value.Kind == JsonValueKind.String)
            {
                AppendString(builder, value.StringValue);
                return;
            }

            if (value.Kind == JsonValueKind.Number)
            {
                builder.Append(value.NumberValue ?? "0");
                return;
            }

            if (value.Kind == JsonValueKind.Boolean)
            {
                builder.Append(value.BooleanValue ? "true" : "false");
                return;
            }

            builder.Append("null");
        }

        private static void AppendCheckpointRecord(
            StringBuilder builder,
            LightweightCheckpointRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar contains a null checkpoint record.");
            }

            builder.Append('{');
            AppendPropertyName(builder, "LastSave");
            AppendString(builder, record.LastSave ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "PlaytimeSeconds");
            AppendInt64(builder, record.PlaytimeSeconds);
            builder.Append(',');
            AppendPropertyName(builder, "GameDateTime");
            AppendString(builder, record.GameDateTime ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "ContentFingerprint");
            AppendString(builder, record.ContentFingerprint ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "Sequence");
            AppendInt64(builder, record.Sequence);
            builder.Append(',');
            AppendPropertyName(builder, "EnabledMods");
            AppendModSnapshots(builder, record.EnabledMods);
            if (record.AgencyRoomIdentities == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar checkpoint is missing AgencyRoomIdentities.");
            }
            builder.Append(',');
            AppendPropertyName(builder, "AgencyRoomIdentities");
            AppendAgencyRoomIdentities(builder, record.AgencyRoomIdentities);
            builder.Append('}');
        }

        private static void AppendAgencyRoomIdentities(
            StringBuilder builder,
            List<LightweightAgencyRoomIdentityRecord> records)
        {
            builder.Append('[');
            bool wroteAny = false;
            for (int index = 0; index < records.Count; index++)
            {
                LightweightAgencyRoomIdentityRecord record = records[index];
                if (record == null)
                {
                    continue;
                }
                if (wroteAny)
                {
                    builder.Append(',');
                }
                builder.Append('{');
                AppendPropertyName(builder, "EntityId");
                AppendString(builder, record.EntityId ?? string.Empty);
                builder.Append(',');
                AppendPropertyName(builder, "FloorIndex");
                AppendInt32(builder, record.FloorIndex);
                builder.Append(',');
                AppendPropertyName(builder, "RoomIndex");
                AppendInt32(builder, record.RoomIndex);
                builder.Append(',');
                AppendPropertyName(builder, "RoomTypeRaw");
                AppendInt32(builder, record.RoomTypeRaw);
                builder.Append(',');
                AppendPropertyName(builder, "TheaterId");
                AppendInt32(builder, record.TheaterId);
                builder.Append('}');
                wroteAny = true;
            }
            builder.Append(']');
        }

        private static void AppendModSnapshots(
            StringBuilder builder,
            List<LightweightModSnapshotRecord> records)
        {
            builder.Append('[');
            bool wroteAny = false;
            if (records != null)
            {
                for (int index = 0; index < records.Count; index++)
                {
                    LightweightModSnapshotRecord record = records[index];
                    if (record == null)
                    {
                        continue;
                    }
                    if (wroteAny)
                    {
                        builder.Append(',');
                    }
                    wroteAny = true;

                    builder.Append('{');
                    AppendPropertyName(builder, "ModName");
                    AppendString(builder, record.ModName ?? string.Empty);
                    builder.Append(',');
                    AppendPropertyName(builder, "Title");
                    AppendString(builder, record.Title ?? string.Empty);
                    builder.Append(',');
                    AppendPropertyName(builder, "Author");
                    AppendString(builder, record.Author ?? string.Empty);
                    builder.Append(',');
                    AppendPropertyName(builder, "Version");
                    AppendString(builder, record.Version ?? string.Empty);
                    builder.Append(',');
                    AppendPropertyName(builder, "DllNames");
                    AppendStringArray(builder, record.DllNames);
                    builder.Append('}');
                }
            }
            builder.Append(']');
        }

        private static void AppendStringArray(
            StringBuilder builder,
            List<string> values)
        {
            builder.Append('[');
            if (values != null)
            {
                for (int index = 0; index < values.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(',');
                    }
                    AppendString(builder, values[index] ?? string.Empty);
                }
            }
            builder.Append(']');
        }

        private static void AppendEventRecord(
            StringBuilder builder,
            LightweightEventRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar contains a null event record.");
            }

            builder.Append('{');
            AppendPropertyName(builder, "Sequence");
            AppendInt64(builder, record.Sequence);
            builder.Append(',');
            AppendPropertyName(builder, "GameDateTime");
            AppendString(builder, record.GameDateTime ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "IdolId");
            AppendInt32(builder, record.IdolId);
            builder.Append(',');
            AppendPropertyName(builder, "EntityKind");
            AppendString(builder, record.EntityKind ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "EntityId");
            AppendString(builder, record.EntityId ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "EventType");
            AppendString(builder, record.EventType ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "SourcePatch");
            AppendString(builder, record.SourcePatch ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "NamespaceIdentifier");
            AppendString(builder, record.NamespaceIdentifier ?? string.Empty);
            if (!string.IsNullOrEmpty(record.IdempotencyKey))
            {
                builder.Append(',');
                AppendPropertyName(builder, "IdempotencyKey");
                AppendString(builder, record.IdempotencyKey);
            }
            builder.Append(',');
            AppendPropertyName(builder, "Payload");
            string storagePayload = record.StoragePayloadJson;
            if (string.IsNullOrWhiteSpace(storagePayload))
            {
                string normalizedRuntime;
                string prepareError;
                if (!TryNormalizeEventPayloadForStorage(
                        record.PayloadJson,
                        !string.IsNullOrEmpty(record.NamespaceIdentifier),
                        out normalizedRuntime,
                        out storagePayload,
                        out prepareError))
                {
                    throw new FormatException(
                        "An event payload is not valid JSON: " + prepareError);
                }
            }
            builder.Append(storagePayload);
            builder.Append('}');
        }

        private static void AppendCustomMutationRecord(
            StringBuilder builder,
            LightweightCustomMutationRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException(
                    "The lightweight sidecar contains a null custom mutation.");
            }

            builder.Append('{');
            AppendPropertyName(builder, "Sequence");
            AppendInt64(builder, record.Sequence);
            builder.Append(',');
            AppendPropertyName(builder, "GameDateTime");
            AppendString(builder, record.GameDateTime ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "NamespaceIdentifier");
            AppendString(builder, record.NamespaceIdentifier ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "DataKey");
            AppendString(builder, record.DataKey ?? string.Empty);
            builder.Append(',');
            AppendPropertyName(builder, "Operation");
            AppendString(builder, record.Operation ?? string.Empty);

            if (string.Equals(
                    record.Operation,
                    LightweightCoreStorageEngine.CustomOperationSet,
                    StringComparison.Ordinal))
            {
                builder.Append(',');
                AppendPropertyName(builder, "Value");
                string storageValue = record.StorageValueJson;
                if (string.IsNullOrWhiteSpace(storageValue))
                {
                    string normalizedValue;
                    string normalizeError;
                    if (!TryNormalizeJsonDocument(
                            record.ValueJson,
                            out normalizedValue,
                            out normalizeError))
                    {
                        throw new FormatException(
                            "A custom-data value is not valid JSON: " +
                            normalizeError);
                    }
                    storageValue = normalizedValue;
                }
                builder.Append(storageValue);
            }

            builder.Append('}');
        }


        private static List<LightweightCheckpointRecord> ReadCheckpoints(
            List<JsonValue> values,
            string documentRelativeSavePath)
        {
            List<LightweightCheckpointRecord> records =
                new List<LightweightCheckpointRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                records.Add(
                    ReadCheckpoint(
                        values[index],
                        documentRelativeSavePath));
            }

            return records;
        }

        private static LightweightCheckpointRecord ReadCheckpoint(
            JsonValue value,
            string documentRelativeSavePath)
        {
            Dictionary<string, JsonValue> item = RequireObject(
                value,
                "A checkpoint entry must be a JSON object.");

            return new LightweightCheckpointRecord
            {
                RelativeSavePath = documentRelativeSavePath ?? string.Empty,
                LastSave = RequireString(item, "LastSave"),
                PlaytimeSeconds = RequireInt64(item, "PlaytimeSeconds"),
                GameDateTime = RequireString(item, "GameDateTime"),
                ContentFingerprint = RequireString(item, "ContentFingerprint"),
                Sequence = RequireInt64(item, "Sequence"),
                EnabledMods = ReadModSnapshots(item),
                AgencyRoomIdentities = ReadAgencyRoomIdentities(item)
            };
        }

        private static List<LightweightAgencyRoomIdentityRecord> ReadAgencyRoomIdentities(
            Dictionary<string, JsonValue> checkpoint)
        {
            List<JsonValue> values = RequireArray(
                checkpoint,
                "AgencyRoomIdentities");
            List<LightweightAgencyRoomIdentityRecord> records =
                new List<LightweightAgencyRoomIdentityRecord>(values.Count);
            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "An agency-room identity entry must be a JSON object.");
                records.Add(new LightweightAgencyRoomIdentityRecord
                {
                    EntityId = RequireString(item, "EntityId"),
                    FloorIndex = RequireInt32(item, "FloorIndex"),
                    RoomIndex = RequireInt32(item, "RoomIndex"),
                    RoomTypeRaw = RequireInt32(item, "RoomTypeRaw"),
                    TheaterId = RequireInt32(item, "TheaterId")
                });
            }

            return records;
        }

        private static List<LightweightModSnapshotRecord> ReadModSnapshots(
            Dictionary<string, JsonValue> checkpoint)
        {
            List<LightweightModSnapshotRecord> records =
                new List<LightweightModSnapshotRecord>();
            List<JsonValue> values = RequireArray(checkpoint, "EnabledMods");

            for (int index = 0; index < values.Count; index++)
            {
                Dictionary<string, JsonValue> item = RequireObject(
                    values[index],
                    "An enabled-mod snapshot must be a JSON object.");
                records.Add(new LightweightModSnapshotRecord
                {
                    ModName = RequireString(item, "ModName"),
                    Title = RequireString(item, "Title"),
                    Author = RequireString(item, "Author"),
                    Version = RequireString(item, "Version"),
                    DllNames = ReadStringArray(RequireArray(item, "DllNames"), "DllNames")
                });
            }
            return records;
        }

        private static List<string> ReadStringArray(
            List<JsonValue> values,
            string fieldName)
        {
            List<string> result = new List<string>();
            if (values == null)
            {
                return result;
            }
            for (int index = 0; index < values.Count; index++)
            {
                JsonValue value = values[index];
                if (value == null || value.Kind != JsonValueKind.String)
                {
                    throw new FormatException(
                        "The lightweight sidecar field '" + fieldName +
                        "' must contain only strings.");
                }
                result.Add(value.StringValue ?? string.Empty);
            }
            return result;
        }


        private static List<LightweightEventRecord> ReadEvents(
            List<JsonValue> values)
        {
            List<LightweightEventRecord> records =
                new List<LightweightEventRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                records.Add(ReadEvent(values[index]));
            }

            return records;
        }

        private static LightweightEventRecord ReadEvent(
            JsonValue value)
        {
            Dictionary<string, JsonValue> item = RequireObject(
                value,
                "An event entry must be a JSON object.");

            long sequence = RequireInt64(item, "Sequence");
            string gameDateTime = RequireString(item, "GameDateTime");
            string namespaceIdentifier = RequireString(
                item,
                "NamespaceIdentifier");
            int gameDateKey = BuildGameDateKeyFromRoundTrip(
                gameDateTime,
                "event");
            JsonValue payloadValue = RequireMember(item, "Payload");
            string storagePayloadJson = SerializeJsonValue(payloadValue);
            if (string.IsNullOrEmpty(namespaceIdentifier))
            {
                TransformEventPayloadForRuntime(payloadValue);
            }
            string payloadJson = SerializeJsonValue(payloadValue);

            return new LightweightEventRecord
            {
                Sequence = sequence,
                GameDateKey = gameDateKey,
                GameDateTime = gameDateTime,
                IdolId = RequireInt32(item, "IdolId"),
                EntityKind = RequireString(item, "EntityKind"),
                EntityId = RequireString(item, "EntityId"),
                EventType = RequireString(item, "EventType"),
                SourcePatch = RequireString(item, "SourcePatch"),
                NamespaceIdentifier = namespaceIdentifier,
                IdempotencyKey = ReadOptionalString(
                    item,
                    "IdempotencyKey"),
                PayloadJson = payloadJson,
                StoragePayloadJson = storagePayloadJson
            };
        }



        private static List<LightweightCustomMutationRecord> ReadCustomMutations(
            List<JsonValue> values)
        {
            List<LightweightCustomMutationRecord> records =
                new List<LightweightCustomMutationRecord>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                records.Add(ReadCustomMutation(values[index]));
            }

            return records;
        }

        private static LightweightCustomMutationRecord ReadCustomMutation(
            JsonValue value)
        {
            Dictionary<string, JsonValue> item = RequireObject(
                value,
                "A custom mutation entry must be a JSON object.");

            string operation = RequireString(item, "Operation");
            string gameDateTime = RequireString(item, "GameDateTime");
            int gameDateKey = BuildGameDateKeyFromRoundTrip(
                gameDateTime,
                "custom-data mutation");

            string valueJson = string.Equals(
                operation,
                LightweightCoreStorageEngine.CustomOperationSet,
                StringComparison.Ordinal)
                ? SerializeJsonValue(RequireMember(item, "Value"))
                : string.Empty;
            string storageValueJson = valueJson;

            return new LightweightCustomMutationRecord
            {
                Sequence = RequireInt64(item, "Sequence"),
                GameDateKey = gameDateKey,
                GameDateTime = gameDateTime,
                NamespaceIdentifier = RequireString(
                    item,
                    "NamespaceIdentifier"),
                DataKey = RequireString(item, "DataKey"),
                Operation = operation,
                ValueJson = valueJson,
                StorageValueJson = storageValueJson
            };
        }

        internal static bool TryNormalizeEventPayloadForStorage(
            string json,
            bool isNamespacedCustomEvent,
            out string normalizedRuntimeJson,
            out string storageJson,
            out string errorMessage)
        {
            normalizedRuntimeJson = string.Empty;
            storageJson = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = "The JSON document is empty.";
                return false;
            }

            try
            {
                JsonValue value = new JsonParser(json).ParseDocument();
                normalizedRuntimeJson = SerializeJsonValue(value);
                if (!isNamespacedCustomEvent)
                {
                    TransformEventPayloadForStorage(value);
                }
                storageJson = SerializeJsonValue(value);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                normalizedRuntimeJson = string.Empty;
                storageJson = string.Empty;
                return false;
            }
        }


        private static JsonValue ParseJsonForStorage(
            string json,
            string description)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException(
                    (description ?? "A JSON value") + " is empty.");
            }

            try
            {
                return new JsonParser(json).ParseDocument();
            }
            catch (Exception exception)
            {
                throw new FormatException(
                    (description ?? "A JSON value") +
                    " is not valid JSON: " +
                    exception.Message,
                    exception);
            }
        }

        private static int BuildGameDateKeyFromRoundTrip(
            string gameDateTime,
            string recordKind)
        {
            DateTime parsed;
            if (!DateTime.TryParseExact(
                    gameDateTime ?? string.Empty,
                    CoreConstants.RoundTripDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsed))
            {
                throw new FormatException(
                    "The lightweight sidecar " +
                    (recordKind ?? "record") +
                    " has an invalid GameDateTime.");
            }

            return CoreDateTimeUtility.BuildGameDateKey(parsed);
        }

        /// <summary>
        /// Makes built-in event payloads document-native on disk while keeping the
        /// established string-based public payload contract. Only built-in IMDC event payloads are rewritten; namespaced custom-event JSON is left untouched.
        /// </summary>
        private static void TransformEventPayloadForStorage(JsonValue value)
        {
            if (value == null)
            {
                return;
            }

            if (value.Kind == JsonValueKind.Array)
            {
                for (int index = 0;
                    value.ArrayValue != null && index < value.ArrayValue.Count;
                    index++)
                {
                    TransformEventPayloadForStorage(value.ArrayValue[index]);
                }
                return;
            }

            if (value.Kind != JsonValueKind.Object ||
                value.ObjectValue == null)
            {
                return;
            }

            JsonValue detailJson;
            if (value.ObjectValue.TryGetValue("detail_json", out detailJson) &&
                detailJson != null &&
                detailJson.Kind == JsonValueKind.String)
            {
                if (string.IsNullOrWhiteSpace(detailJson.StringValue))
                {
                    value.ObjectValue.Remove("detail_json");
                }
                else
                {
                    try
                    {
                        JsonValue detailValue =
                            new JsonParser(detailJson.StringValue).ParseDocument();
                        value.ObjectValue.Remove("detail_json");
                        value.ObjectValue["detail"] = detailValue;
                    }
                    catch
                    {
                        // A malformed historical detail payload is left untouched.
                    }
                }
            }

            List<string> propertyNames =
                new List<string>(value.ObjectValue.Keys);
            for (int index = 0; index < propertyNames.Count; index++)
            {
                string propertyName = propertyNames[index];
                JsonValue member;
                if (!value.ObjectValue.TryGetValue(propertyName, out member) ||
                    member == null)
                {
                    continue;
                }

                if (member.Kind == JsonValueKind.String &&
                    IsDelimitedListProperty(propertyName))
                {
                    value.ObjectValue[propertyName] =
                        ParseDelimitedListForStorage(
                            propertyName,
                            member.StringValue);
                    continue;
                }

                TransformEventPayloadForStorage(member);
            }
        }

        /// <summary>
        /// Reconstructs the stable public/runtime payload shape from the native v5
        /// disk representation. This keeps existing Cosmo consumers source-compatible
        /// without storing JSON or identifier arrays as quoted strings.
        /// </summary>
        private static void TransformEventPayloadForRuntime(JsonValue value)
        {
            if (value == null)
            {
                return;
            }

            if (value.Kind == JsonValueKind.Array)
            {
                for (int index = 0;
                    value.ArrayValue != null && index < value.ArrayValue.Count;
                    index++)
                {
                    TransformEventPayloadForRuntime(value.ArrayValue[index]);
                }
                return;
            }

            if (value.Kind != JsonValueKind.Object ||
                value.ObjectValue == null)
            {
                return;
            }

            JsonValue detail;
            if (value.ObjectValue.TryGetValue("detail", out detail) &&
                detail != null &&
                !value.ObjectValue.ContainsKey("detail_json"))
            {
                // Restore any JSON-native list fields inside the detail object
                // before re-encoding the established public detail_json view.
                TransformEventPayloadForRuntime(detail);
                value.ObjectValue.Remove("detail");
                value.ObjectValue["detail_json"] = new JsonValue
                {
                    Kind = JsonValueKind.String,
                    StringValue = SerializeJsonValue(detail)
                };
            }

            List<string> propertyNames =
                new List<string>(value.ObjectValue.Keys);
            for (int index = 0; index < propertyNames.Count; index++)
            {
                string propertyName = propertyNames[index];
                JsonValue member;
                if (!value.ObjectValue.TryGetValue(propertyName, out member) ||
                    member == null)
                {
                    continue;
                }

                if (member.Kind == JsonValueKind.Array &&
                    IsDelimitedListProperty(propertyName))
                {
                    value.ObjectValue[propertyName] = new JsonValue
                    {
                        Kind = JsonValueKind.String,
                        StringValue = SerializeDelimitedListForRuntime(
                            member.ArrayValue)
                    };
                    continue;
                }

                TransformEventPayloadForRuntime(member);
            }
        }

        private static bool IsDelimitedListProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            string normalized = propertyName
                .Replace("_", string.Empty)
                .ToLowerInvariant();

            return normalized.IndexOf(
                    "idlist",
                    StringComparison.Ordinal) >= 0 ||
                string.Equals(
                    normalized,
                    "removedtaskcustomlist",
                    StringComparison.Ordinal);
        }

        private static JsonValue ParseDelimitedListForStorage(
            string propertyName,
            string rawValue)
        {
            JsonValue array = new JsonValue
            {
                Kind = JsonValueKind.Array,
                ArrayValue = new List<JsonValue>()
            };

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return array;
            }

            bool identifierList =
                (propertyName ?? string.Empty)
                    .Replace("_", string.Empty)
                    .ToLowerInvariant()
                    .IndexOf(
                        "idlist",
                        StringComparison.Ordinal) >= 0;

            string[] tokens = rawValue.Split(',');
            for (int index = 0; index < tokens.Length; index++)
            {
                string token = tokens[index].Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                long identifier;
                if (identifierList &&
                    long.TryParse(
                        token,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out identifier))
                {
                    array.ArrayValue.Add(
                        new JsonValue
                        {
                            Kind = JsonValueKind.Number,
                            NumberValue = identifier.ToString(
                                CultureInfo.InvariantCulture)
                        });
                }
                else
                {
                    array.ArrayValue.Add(
                        new JsonValue
                        {
                            Kind = JsonValueKind.String,
                            StringValue = token
                        });
                }
            }

            return array;
        }

        private static string SerializeDelimitedListForRuntime(
            List<JsonValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < values.Count; index++)
            {
                JsonValue value = values[index];
                if (value == null)
                {
                    continue;
                }

                string token;
                if (value.Kind == JsonValueKind.Number)
                {
                    token = value.NumberValue ?? string.Empty;
                }
                else if (value.Kind == JsonValueKind.String)
                {
                    token = value.StringValue ?? string.Empty;
                }
                else
                {
                    token = SerializeJsonValue(value);
                }

                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(token);
            }

            return builder.ToString();
        }

        private static Dictionary<string, JsonValue> RequireObject(
            JsonValue value,
            string errorMessage)
        {
            if (value == null || value.Kind != JsonValueKind.Object)
            {
                throw new FormatException(errorMessage);
            }

            return value.ObjectValue;
        }

        private static List<JsonValue> RequireArray(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value = RequireMember(source, name);
            if (value.Kind != JsonValueKind.Array)
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' must be a JSON array.");
            }

            return value.ArrayValue;
        }

        private static string ReadOptionalString(
            Dictionary<string, JsonValue> source,
            string propertyName)
        {
            JsonValue value;
            if (source == null ||
                !source.TryGetValue(propertyName, out value))
            {
                return string.Empty;
            }

            if (value == null || value.Kind != JsonValueKind.String)
            {
                throw new FormatException(
                    "JSON property '" + propertyName + "' must be a string when present.");
            }

            return value.StringValue ?? string.Empty;
        }

        private static string RequireString(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value = RequireMember(source, name);
            if (value.Kind != JsonValueKind.String)
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' must be a JSON string.");
            }

            return value.StringValue ?? string.Empty;
        }

        private static int RequireInt32(
            Dictionary<string, JsonValue> source,
            string name)
        {
            long value = RequireInt64(source, name);
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' is outside the Int32 range.");
            }

            return (int)value;
        }

        private static long RequireInt64(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value = RequireMember(source, name);
            long result;
            if (value.Kind != JsonValueKind.Number ||
                !long.TryParse(
                    value.NumberValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                throw new FormatException(
                    "The lightweight sidecar field '" + name +
                    "' must be a JSON integer.");
            }

            return result;
        }

        private static JsonValue RequireMember(
            Dictionary<string, JsonValue> source,
            string name)
        {
            JsonValue value;
            if (!source.TryGetValue(name, out value) || value == null)
            {
                throw new FormatException(
                    "The lightweight sidecar is missing required field '" +
                    name + "'.");
            }

            return value;
        }

        private static void AppendPropertyName(
            StringBuilder builder,
            string name)
        {
            AppendString(builder, name);
            builder.Append(':');
        }

        private static void AppendInt32(StringBuilder builder, int value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendInt64(StringBuilder builder, long value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendString(
            StringBuilder builder,
            string value)
        {
            builder.Append('"');

            if (!string.IsNullOrEmpty(value))
            {
                for (int index = 0; index < value.Length; index++)
                {
                    char character = value[index];
                    switch (character)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            if (character < ' ')
                            {
                                builder.Append("\\u");
                                builder.Append(
                                    ((int)character).ToString(
                                        "x4",
                                        CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                builder.Append(character);
                            }

                            break;
                    }
                }
            }

            builder.Append('"');
        }

        private enum JsonValueKind
        {
            Null,
            Object,
            Array,
            String,
            Number,
            Boolean
        }

        private sealed class JsonValue
        {
            internal JsonValueKind Kind;
            internal Dictionary<string, JsonValue> ObjectValue;
            internal List<JsonValue> ArrayValue;
            internal string StringValue;
            internal string NumberValue;
            internal bool BooleanValue;
        }

        /// <summary>
        /// Minimal forward-only JSON parser used by sidecar loading. It keeps only
        /// the current record tree alive while the schema-specific reader converts
        /// it into the durable IMDC record model.
        /// </summary>
        private sealed class StreamingJsonParser
        {
            private readonly TextReader reader;
            private long offset;

            internal StreamingJsonParser(TextReader reader)
            {
                this.reader = reader;
            }

            internal void EnsureEnd()
            {
                SkipWhitespace();
                if (reader.Peek() >= 0)
                {
                    throw Error("Unexpected trailing JSON content.");
                }
            }

            internal string ParsePropertyName()
            {
                SkipWhitespace();
                if (reader.Peek() != '"')
                {
                    throw Error("Expected a JSON object property name.");
                }

                return ParseString();
            }

            internal string ParseRequiredString(string propertyName)
            {
                JsonValue value = ParseValue();
                if (value == null || value.Kind != JsonValueKind.String)
                {
                    throw new FormatException(
                        "The lightweight sidecar field '" + propertyName +
                        "' must be a JSON string.");
                }

                return value.StringValue ?? string.Empty;
            }

            internal int ParseRequiredInt32(string propertyName)
            {
                long value = ParseRequiredInt64(propertyName);
                if (value < int.MinValue || value > int.MaxValue)
                {
                    throw new FormatException(
                        "The lightweight sidecar field '" + propertyName +
                        "' is outside the Int32 range.");
                }

                return (int)value;
            }

            internal long ParseRequiredInt64(string propertyName)
            {
                JsonValue value = ParseValue();
                long result;
                if (value == null ||
                    value.Kind != JsonValueKind.Number ||
                    !long.TryParse(
                        value.NumberValue,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out result))
                {
                    throw new FormatException(
                        "The lightweight sidecar field '" + propertyName +
                        "' must be a JSON integer.");
                }

                return result;
            }

            internal JsonValue ParseValue()
            {
                SkipWhitespace();
                int next = reader.Peek();
                if (next < 0)
                {
                    throw Error("Unexpected end of JSON input.");
                }

                char character = (char)next;
                switch (character)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return new JsonValue
                        {
                            Kind = JsonValueKind.String,
                            StringValue = ParseString()
                        };
                    case 't':
                        ReadLiteral("true");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Boolean,
                            BooleanValue = true
                        };
                    case 'f':
                        ReadLiteral("false");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Boolean,
                            BooleanValue = false
                        };
                    case 'n':
                        ReadLiteral("null");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Null
                        };
                    default:
                        if (character == '-' ||
                            (character >= '0' && character <= '9'))
                        {
                            return new JsonValue
                            {
                                Kind = JsonValueKind.Number,
                                NumberValue = ParseNumber()
                            };
                        }

                        throw Error("Unexpected JSON token.");
                }
            }

            internal void SkipWhitespace()
            {
                while (true)
                {
                    int next = reader.Peek();
                    if (next < 0)
                    {
                        return;
                    }

                    char character = (char)next;
                    if (character == ' ' ||
                        character == '\t' ||
                        character == '\r' ||
                        character == '\n')
                    {
                        ReadCharacter();
                        continue;
                    }

                    return;
                }
            }

            internal bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (reader.Peek() == expected)
                {
                    ReadCharacter();
                    return true;
                }

                return false;
            }

            internal void Expect(char expected)
            {
                SkipWhitespace();
                if (reader.Peek() != expected)
                {
                    throw Error(
                        "Expected JSON character '" + expected + "'.");
                }

                ReadCharacter();
            }

            private JsonValue ParseObject()
            {
                Expect('{');
                SkipWhitespace();

                Dictionary<string, JsonValue> values =
                    new Dictionary<string, JsonValue>(StringComparer.Ordinal);

                if (TryConsume('}'))
                {
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Object,
                        ObjectValue = values
                    };
                }

                while (true)
                {
                    string name = ParsePropertyName();
                    Expect(':');
                    JsonValue value = ParseValue();

                    if (values.ContainsKey(name))
                    {
                        throw Error(
                            "Duplicate JSON object property '" + name + "'.");
                    }

                    values.Add(name, value);
                    SkipWhitespace();

                    if (TryConsume('}'))
                    {
                        break;
                    }

                    Expect(',');
                }

                return new JsonValue
                {
                    Kind = JsonValueKind.Object,
                    ObjectValue = values
                };
            }

            private JsonValue ParseArray()
            {
                Expect('[');
                SkipWhitespace();

                List<JsonValue> values = new List<JsonValue>();
                if (TryConsume(']'))
                {
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Array,
                        ArrayValue = values
                    };
                }

                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();

                    if (TryConsume(']'))
                    {
                        break;
                    }

                    Expect(',');
                }

                return new JsonValue
                {
                    Kind = JsonValueKind.Array,
                    ArrayValue = values
                };
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder builder = new StringBuilder();

                while (true)
                {
                    int raw = ReadCharacter();
                    if (raw < 0)
                    {
                        throw Error("Unterminated JSON string.");
                    }

                    char character = (char)raw;
                    if (character == '"')
                    {
                        return builder.ToString();
                    }

                    if (character == '\\')
                    {
                        int escapedRaw = ReadCharacter();
                        if (escapedRaw < 0)
                        {
                            throw Error("Unterminated JSON escape sequence.");
                        }

                        char escaped = (char)escapedRaw;
                        switch (escaped)
                        {
                            case '"':
                                builder.Append('"');
                                break;
                            case '\\':
                                builder.Append('\\');
                                break;
                            case '/':
                                builder.Append('/');
                                break;
                            case 'b':
                                builder.Append('\b');
                                break;
                            case 'f':
                                builder.Append('\f');
                                break;
                            case 'n':
                                builder.Append('\n');
                                break;
                            case 'r':
                                builder.Append('\r');
                                break;
                            case 't':
                                builder.Append('\t');
                                break;
                            case 'u':
                                builder.Append(ParseUnicodeEscape());
                                break;
                            default:
                                throw Error("Invalid JSON escape sequence.");
                        }

                        continue;
                    }

                    if (character < ' ')
                    {
                        throw Error(
                            "Unescaped control character in JSON string.");
                    }

                    builder.Append(character);
                }
            }

            private char ParseUnicodeEscape()
            {
                int value = 0;
                for (int index = 0; index < 4; index++)
                {
                    int raw = ReadCharacter();
                    if (raw < 0)
                    {
                        throw Error("Incomplete JSON unicode escape.");
                    }

                    char character = (char)raw;
                    value <<= 4;
                    if (character >= '0' && character <= '9')
                    {
                        value += character - '0';
                    }
                    else if (character >= 'a' && character <= 'f')
                    {
                        value += character - 'a' + 10;
                    }
                    else if (character >= 'A' && character <= 'F')
                    {
                        value += character - 'A' + 10;
                    }
                    else
                    {
                        throw Error("Invalid JSON unicode escape.");
                    }
                }

                return (char)value;
            }

            private string ParseNumber()
            {
                StringBuilder builder = new StringBuilder();
                if (reader.Peek() == '-')
                {
                    builder.Append((char)ReadCharacter());
                    if (reader.Peek() < 0)
                    {
                        throw Error("Incomplete JSON number.");
                    }
                }

                int next = reader.Peek();
                if (next == '0')
                {
                    builder.Append((char)ReadCharacter());
                }
                else
                {
                    if (next < '1' || next > '9')
                    {
                        throw Error("Invalid JSON number.");
                    }

                    while (reader.Peek() >= '0' && reader.Peek() <= '9')
                    {
                        builder.Append((char)ReadCharacter());
                    }
                }

                if (reader.Peek() == '.')
                {
                    builder.Append((char)ReadCharacter());
                    int fractionDigits = 0;
                    while (reader.Peek() >= '0' && reader.Peek() <= '9')
                    {
                        builder.Append((char)ReadCharacter());
                        fractionDigits++;
                    }

                    if (fractionDigits == 0)
                    {
                        throw Error("Invalid JSON number fraction.");
                    }
                }

                int exponent = reader.Peek();
                if (exponent == 'e' || exponent == 'E')
                {
                    builder.Append((char)ReadCharacter());
                    int sign = reader.Peek();
                    if (sign == '+' || sign == '-')
                    {
                        builder.Append((char)ReadCharacter());
                    }

                    int exponentDigits = 0;
                    while (reader.Peek() >= '0' && reader.Peek() <= '9')
                    {
                        builder.Append((char)ReadCharacter());
                        exponentDigits++;
                    }

                    if (exponentDigits == 0)
                    {
                        throw Error("Invalid JSON number exponent.");
                    }
                }

                return builder.ToString();
            }

            private void ReadLiteral(string literal)
            {
                for (int index = 0; index < literal.Length; index++)
                {
                    int raw = ReadCharacter();
                    if (raw != literal[index])
                    {
                        throw Error("Invalid JSON literal.");
                    }
                }
            }

            private int ReadCharacter()
            {
                int value = reader.Read();
                if (value >= 0)
                {
                    offset++;
                }

                return value;
            }

            private FormatException Error(string message)
            {
                return new FormatException(
                    message + " Offset: " +
                    offset.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private sealed class JsonParser
        {
            private readonly string json;
            private int index;

            internal JsonParser(string json)
            {
                this.json = json ?? string.Empty;
            }

            internal JsonValue ParseDocument()
            {
                SkipWhitespace();
                JsonValue value = ParseValue();
                SkipWhitespace();

                if (index != json.Length)
                {
                    throw Error("Unexpected trailing JSON content.");
                }

                return value;
            }

            private JsonValue ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length)
                {
                    throw Error("Unexpected end of JSON input.");
                }

                char character = json[index];
                switch (character)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return new JsonValue
                        {
                            Kind = JsonValueKind.String,
                            StringValue = ParseString()
                        };
                    case 't':
                        ReadLiteral("true");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Boolean,
                            BooleanValue = true
                        };
                    case 'f':
                        ReadLiteral("false");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Boolean,
                            BooleanValue = false
                        };
                    case 'n':
                        ReadLiteral("null");
                        return new JsonValue
                        {
                            Kind = JsonValueKind.Null
                        };
                    default:
                        if (character == '-' ||
                            (character >= '0' && character <= '9'))
                        {
                            return new JsonValue
                            {
                                Kind = JsonValueKind.Number,
                                NumberValue = ParseNumber()
                            };
                        }

                        throw Error("Unexpected JSON token.");
                }
            }

            private JsonValue ParseObject()
            {
                Expect('{');
                SkipWhitespace();

                Dictionary<string, JsonValue> values =
                    new Dictionary<string, JsonValue>(StringComparer.Ordinal);

                if (TryConsume('}'))
                {
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Object,
                        ObjectValue = values
                    };
                }

                while (true)
                {
                    SkipWhitespace();
                    if (index >= json.Length || json[index] != '"')
                    {
                        throw Error("Expected a JSON object property name.");
                    }

                    string name = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    JsonValue value = ParseValue();

                    if (values.ContainsKey(name))
                    {
                        throw Error(
                            "Duplicate JSON object property '" + name + "'.");
                    }

                    values.Add(name, value);
                    SkipWhitespace();

                    if (TryConsume('}'))
                    {
                        break;
                    }

                    Expect(',');
                }

                return new JsonValue
                {
                    Kind = JsonValueKind.Object,
                    ObjectValue = values
                };
            }

            private JsonValue ParseArray()
            {
                Expect('[');
                SkipWhitespace();

                List<JsonValue> values = new List<JsonValue>();
                if (TryConsume(']'))
                {
                    return new JsonValue
                    {
                        Kind = JsonValueKind.Array,
                        ArrayValue = values
                    };
                }

                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();

                    if (TryConsume(']'))
                    {
                        break;
                    }

                    Expect(',');
                }

                return new JsonValue
                {
                    Kind = JsonValueKind.Array,
                    ArrayValue = values
                };
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder builder = new StringBuilder();

                while (index < json.Length)
                {
                    char character = json[index++];
                    if (character == '"')
                    {
                        return builder.ToString();
                    }

                    if (character == '\\')
                    {
                        if (index >= json.Length)
                        {
                            throw Error("Unterminated JSON escape sequence.");
                        }

                        char escaped = json[index++];
                        switch (escaped)
                        {
                            case '"':
                                builder.Append('"');
                                break;
                            case '\\':
                                builder.Append('\\');
                                break;
                            case '/':
                                builder.Append('/');
                                break;
                            case 'b':
                                builder.Append('\b');
                                break;
                            case 'f':
                                builder.Append('\f');
                                break;
                            case 'n':
                                builder.Append('\n');
                                break;
                            case 'r':
                                builder.Append('\r');
                                break;
                            case 't':
                                builder.Append('\t');
                                break;
                            case 'u':
                                builder.Append(ParseUnicodeEscape());
                                break;
                            default:
                                throw Error("Invalid JSON escape sequence.");
                        }

                        continue;
                    }

                    if (character < ' ')
                    {
                        throw Error(
                            "Unescaped control character in JSON string.");
                    }

                    builder.Append(character);
                }

                throw Error("Unterminated JSON string.");
            }

            private char ParseUnicodeEscape()
            {
                if (index + 4 > json.Length)
                {
                    throw Error("Incomplete JSON unicode escape.");
                }

                int value = 0;
                for (int offset = 0; offset < 4; offset++)
                {
                    char character = json[index++];
                    value <<= 4;

                    if (character >= '0' && character <= '9')
                    {
                        value += character - '0';
                    }
                    else if (character >= 'a' && character <= 'f')
                    {
                        value += character - 'a' + 10;
                    }
                    else if (character >= 'A' && character <= 'F')
                    {
                        value += character - 'A' + 10;
                    }
                    else
                    {
                        throw Error("Invalid JSON unicode escape.");
                    }
                }

                return (char)value;
            }

            private string ParseNumber()
            {
                int start = index;

                if (json[index] == '-')
                {
                    index++;
                    if (index >= json.Length)
                    {
                        throw Error("Incomplete JSON number.");
                    }
                }

                if (json[index] == '0')
                {
                    index++;
                }
                else
                {
                    if (json[index] < '1' || json[index] > '9')
                    {
                        throw Error("Invalid JSON number.");
                    }

                    while (index < json.Length &&
                           json[index] >= '0' &&
                           json[index] <= '9')
                    {
                        index++;
                    }
                }

                if (index < json.Length && json[index] == '.')
                {
                    index++;
                    int fractionStart = index;
                    while (index < json.Length &&
                           json[index] >= '0' &&
                           json[index] <= '9')
                    {
                        index++;
                    }

                    if (fractionStart == index)
                    {
                        throw Error("Invalid JSON number fraction.");
                    }
                }

                if (index < json.Length &&
                    (json[index] == 'e' || json[index] == 'E'))
                {
                    index++;
                    if (index < json.Length &&
                        (json[index] == '+' || json[index] == '-'))
                    {
                        index++;
                    }

                    int exponentStart = index;
                    while (index < json.Length &&
                           json[index] >= '0' &&
                           json[index] <= '9')
                    {
                        index++;
                    }

                    if (exponentStart == index)
                    {
                        throw Error("Invalid JSON number exponent.");
                    }
                }

                return json.Substring(start, index - start);
            }

            private void ReadLiteral(string literal)
            {
                if (index + literal.Length > json.Length ||
                    string.CompareOrdinal(
                        json,
                        index,
                        literal,
                        0,
                        literal.Length) != 0)
                {
                    throw Error("Invalid JSON literal.");
                }

                index += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length)
                {
                    char character = json[index];
                    if (character == ' ' ||
                        character == '\t' ||
                        character == '\r' ||
                        character == '\n')
                    {
                        index++;
                        continue;
                    }

                    break;
                }
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (index >= json.Length || json[index] != expected)
                {
                    throw Error(
                        "Expected JSON character '" + expected + "'.");
                }

                index++;
            }

            private FormatException Error(string message)
            {
                return new FormatException(
                    message + " Offset: " +
                    index.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }
    }
}
