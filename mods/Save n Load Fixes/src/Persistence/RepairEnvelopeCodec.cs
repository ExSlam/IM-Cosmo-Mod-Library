using System;
using UnityEngine;

namespace SaveNLoadFixes.Persistence
{
    internal sealed class RepairEnvelopeLoadState
    {
        internal bool Present;
        internal bool Valid;
        internal RepairEnvelopeV1 Envelope;
        internal string Error = string.Empty;
        internal string PhysicalPath = string.Empty;
    }

    /// <summary>
    /// Injects/extracts the SNLF root without teaching vanilla SavedData about the
    /// repair schema. With SNLF present the reader always strips the root before
    /// JsonUtility deserializes SaveManager.SavedData.
    /// </summary>
    internal static class RepairEnvelopeCodec
    {
        internal static bool TryInject(
            string vanillaJson,
            RepairEnvelopeV1 envelope,
            out string mergedJson,
            out string error)
        {
            mergedJson = string.Empty;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(vanillaJson) || envelope == null)
            {
                error = "Vanilla JSON or repair envelope is unavailable.";
                return false;
            }

            int rootOpen = SkipWhitespace(vanillaJson, 0);
            if (rootOpen >= vanillaJson.Length || vanillaJson[rootOpen] != '{')
            {
                error = "Vanilla SavedData JSON is not a root object.";
                return false;
            }

            string envelopeJson;
            try
            {
                envelopeJson = JsonUtility.ToJson(envelope, false);
            }
            catch (Exception exception)
            {
                error = "Repair envelope serialization failed: " + exception.Message;
                return false;
            }

            int firstContent = SkipWhitespace(vanillaJson, rootOpen + 1);
            bool emptyRoot = firstContent < vanillaJson.Length &&
                vanillaJson[firstContent] == '}';
            string property = "\"" + RepairEnvelopeConstants.RootKey + "\":" + envelopeJson;
            string insertion = emptyRoot ? property : property + ",";

            mergedJson = vanillaJson.Insert(rootOpen + 1, insertion);
            return true;
        }

        internal static bool TryExtractAndStrip(
            string fullJson,
            out string vanillaJson,
            out RepairEnvelopeLoadState state,
            out string fatalError)
        {
            vanillaJson = fullJson ?? string.Empty;
            state = new RepairEnvelopeLoadState
            {
                Present = false,
                Valid = true,
                Envelope = null,
                Error = string.Empty
            };
            fatalError = string.Empty;

            if (string.IsNullOrWhiteSpace(fullJson))
            {
                fatalError = "SavedData JSON is empty.";
                return false;
            }

            int propertyStart;
            int propertyEnd;
            int valueStart;
            int valueEnd;
            bool found;
            if (!TryFindRootProperty(
                    fullJson,
                    RepairEnvelopeConstants.RootKey,
                    out found,
                    out propertyStart,
                    out propertyEnd,
                    out valueStart,
                    out valueEnd,
                    out fatalError))
            {
                return false;
            }

            if (!found)
            {
                return true;
            }

            state.Present = true;
            string envelopeJson = fullJson.Substring(valueStart, valueEnd - valueStart);
            RepairEnvelopeV1 envelope = null;
            try
            {
                envelope = JsonUtility.FromJson<RepairEnvelopeV1>(envelopeJson);
            }
            catch (Exception exception)
            {
                state.Valid = false;
                state.Error = "Repair envelope JSON could not be deserialized: " + exception.Message;
            }

            if (state.Valid)
            {
                string validationError;
                if (!ValidateEnvelope(envelope, out validationError))
                {
                    state.Valid = false;
                    state.Error = validationError;
                }
                else
                {
                    state.Envelope = envelope;
                }
            }

            int removalStart = propertyStart;
            int removalEnd = propertyEnd;

            int previous = PreviousNonWhitespace(fullJson, propertyStart - 1);
            int next = SkipWhitespace(fullJson, propertyEnd);
            if (previous >= 0 && fullJson[previous] == ',')
            {
                removalStart = previous;
            }
            else if (next < fullJson.Length && fullJson[next] == ',')
            {
                removalEnd = next + 1;
            }

            vanillaJson = fullJson.Remove(removalStart, removalEnd - removalStart);
            return true;
        }

        private static bool ValidateEnvelope(
            RepairEnvelopeV1 envelope,
            out string error)
        {
            error = string.Empty;
            if (envelope == null)
            {
                error = "Repair envelope is null.";
                return false;
            }

            if (!string.Equals(
                    envelope.format_name,
                    RepairEnvelopeConstants.FormatName,
                    StringComparison.Ordinal))
            {
                error = "Repair envelope format_name is not recognized.";
                return false;
            }

            if (envelope.format_version != RepairEnvelopeConstants.FormatVersion)
            {
                error = "Repair envelope format_version is not supported.";
                return false;
            }

            Guid checkpoint;
            if (string.IsNullOrEmpty(envelope.checkpoint_id) ||
                !Guid.TryParse(envelope.checkpoint_id, out checkpoint))
            {
                error = "Repair envelope checkpoint_id is missing or invalid.";
                return false;
            }

            if (envelope.records == null ||
                envelope.records.relationship_dynamics == null)
            {
                error = "Repair envelope relationship_dynamics section is missing.";
                return false;
            }

            return true;
        }

        private static bool TryFindRootProperty(
            string json,
            string targetKey,
            out bool found,
            out int propertyStart,
            out int propertyEnd,
            out int valueStart,
            out int valueEnd,
            out string error)
        {
            found = false;
            propertyStart = -1;
            propertyEnd = -1;
            valueStart = -1;
            valueEnd = -1;
            error = string.Empty;

            int index = SkipWhitespace(json, 0);
            if (index >= json.Length || json[index] != '{')
            {
                error = "SavedData JSON root is not an object.";
                return false;
            }

            index++;
            while (true)
            {
                index = SkipWhitespace(json, index);
                if (index >= json.Length)
                {
                    error = "SavedData JSON root object is unterminated.";
                    return false;
                }

                if (json[index] == '}')
                {
                    return true;
                }

                if (json[index] == ',')
                {
                    index++;
                    index = SkipWhitespace(json, index);
                }

                int currentPropertyStart = index;
                string key;
                int afterKey;
                if (!TryReadJsonString(json, index, out key, out afterKey))
                {
                    error = "SavedData JSON contains an invalid root property name.";
                    return false;
                }

                index = SkipWhitespace(json, afterKey);
                if (index >= json.Length || json[index] != ':')
                {
                    error = "SavedData JSON root property is missing a colon.";
                    return false;
                }

                index = SkipWhitespace(json, index + 1);
                int currentValueStart = index;
                int currentValueEnd;
                if (!TrySkipJsonValue(json, index, out currentValueEnd))
                {
                    error = "SavedData JSON contains an unterminated root property value.";
                    return false;
                }

                if (string.Equals(key, targetKey, StringComparison.Ordinal))
                {
                    found = true;
                    propertyStart = currentPropertyStart;
                    propertyEnd = currentValueEnd;
                    valueStart = currentValueStart;
                    valueEnd = currentValueEnd;
                    return true;
                }

                index = SkipWhitespace(json, currentValueEnd);
                if (index >= json.Length)
                {
                    error = "SavedData JSON root object is unterminated after a property value.";
                    return false;
                }

                if (json[index] == '}')
                {
                    return true;
                }

                if (json[index] != ',')
                {
                    error = "SavedData JSON root properties are not comma-separated.";
                    return false;
                }
            }
        }

        private static bool TryReadJsonString(
            string json,
            int start,
            out string value,
            out int nextIndex)
        {
            value = string.Empty;
            nextIndex = start;
            if (start >= json.Length || json[start] != '"')
            {
                return false;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            bool escaped = false;
            for (int index = start + 1; index < json.Length; index++)
            {
                char current = json[index];
                if (escaped)
                {
                    // Root keys produced by JsonUtility/SimpleJSON are ordinary ASCII.
                    // Preserve escaped characters distinctly so an escaped lookalike cannot
                    // accidentally match SNLF's reserved root key.
                    builder.Append('\\');
                    builder.Append(current);
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '"')
                {
                    value = builder.ToString();
                    nextIndex = index + 1;
                    return true;
                }

                builder.Append(current);
            }

            return false;
        }

        private static bool TrySkipJsonValue(
            string json,
            int start,
            out int endExclusive)
        {
            endExclusive = start;
            if (start >= json.Length)
            {
                return false;
            }

            char first = json[start];
            if (first == '"')
            {
                string ignored;
                return TryReadJsonString(json, start, out ignored, out endExclusive);
            }

            if (first == '{' || first == '[')
            {
                char open = first;
                char close = first == '{' ? '}' : ']';
                int depth = 0;
                bool inString = false;
                bool escaped = false;

                for (int index = start; index < json.Length; index++)
                {
                    char current = json[index];
                    if (inString)
                    {
                        if (escaped)
                        {
                            escaped = false;
                        }
                        else if (current == '\\')
                        {
                            escaped = true;
                        }
                        else if (current == '"')
                        {
                            inString = false;
                        }
                        continue;
                    }

                    if (current == '"')
                    {
                        inString = true;
                    }
                    else if (current == open)
                    {
                        depth++;
                    }
                    else if (current == close)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            endExclusive = index + 1;
                            return true;
                        }
                    }
                }

                return false;
            }

            int primitive = start;
            while (primitive < json.Length &&
                   json[primitive] != ',' &&
                   json[primitive] != '}' &&
                   json[primitive] != ']')
            {
                primitive++;
            }

            endExclusive = primitive;
            return primitive > start;
        }

        private static int SkipWhitespace(string value, int index)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index++;
            }
            return index;
        }

        private static int PreviousNonWhitespace(string value, int index)
        {
            while (index >= 0 && char.IsWhiteSpace(value[index]))
            {
                index--;
            }
            return index;
        }
    }
}
