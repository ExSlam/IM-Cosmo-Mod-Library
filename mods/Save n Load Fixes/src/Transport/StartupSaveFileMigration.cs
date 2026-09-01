using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using SaveNLoadFixes.Persistence;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Transport
{
    /// <summary>
    /// Replaces only FixSaveFile's final destructive SimpleJSON write. Vanilla uses
    /// that pass solely to rename legacy idol parameter key "val" to "_val", but the
    /// bundled JSONData.ToString() quotes every scalar in the entire document. Apply
    /// the same narrow rename directly to the original UTF-8 text instead.
    /// </summary>
    internal static class StartupSaveFileMigration
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private static long attemptCount;
        private static long migratedFileCount;
        private static long migratedFieldCount;
        private static long failureCount;
        private static string lastDiagnostic = string.Empty;

        internal static long AttemptCount { get { return Interlocked.Read(ref attemptCount); } }
        internal static long MigratedFileCount { get { return Interlocked.Read(ref migratedFileCount); } }
        internal static long MigratedFieldCount { get { return Interlocked.Read(ref migratedFieldCount); } }
        internal static long FailureCount { get { return Interlocked.Read(ref failureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static string SuppressSimpleJsonReserialization(
            object discardedSimpleJsonDocument)
        {
            // FixSaveFile has already put its JSONNode on the evaluation stack. Return
            // a harmless placeholder for the raw migration sink without ever asking
            // JSONNode.ToString() to rebuild the whole document.
            return string.Empty;
        }

        internal static void WriteTypePreservingMigration(
            string path,
            string discardedSimpleJsonOutput)
        {
            Interlocked.Increment(ref attemptCount);

            string normalizedPath;
            if (!SavePathResolver.TryNormalizeAbsolutePath(path, out normalizedPath))
            {
                RecordFailure("FixSaveFile supplied a non-absolute save path; the original file was preserved.");
                return;
            }

            int migratedFields = 0;
            string actionError = string.Empty;
            bool completed = OrderedSaveTransport.TryRunExclusiveFileAccess(
                normalizedPath,
                delegate
                {
                    byte[] originalBytes = File.ReadAllBytes(normalizedPath);
                    bool hasUtf8Bom = originalBytes.Length >= 3 &&
                        originalBytes[0] == 0xEF &&
                        originalBytes[1] == 0xBB &&
                        originalBytes[2] == 0xBF;
                    int textOffset = hasUtf8Bom ? 3 : 0;
                    string originalJson = StrictUtf8.GetString(
                        originalBytes,
                        textOffset,
                        originalBytes.Length - textOffset);

                    string migratedJson;
                    string migrationError;
                    if (!TryMigrateLegacyGirlParameterNames(
                            originalJson,
                            out migratedJson,
                            out migratedFields,
                            out migrationError))
                    {
                        throw new InvalidDataException(migrationError);
                    }

                    if (migratedFields == 0)
                    {
                        return;
                    }

                    byte[] body = Utf8NoBom.GetBytes(migratedJson);
                    if (!hasUtf8Bom)
                    {
                        File.WriteAllBytes(normalizedPath, body);
                        return;
                    }

                    byte[] withBom = new byte[body.Length + 3];
                    withBom[0] = 0xEF;
                    withBom[1] = 0xBB;
                    withBom[2] = 0xBF;
                    Buffer.BlockCopy(body, 0, withBom, 3, body.Length);
                    File.WriteAllBytes(normalizedPath, withBom);
                },
                Timeout.Infinite,
                out actionError);

            if (!completed)
            {
                RecordFailure(
                    "Type-preserving FixSaveFile migration failed; the original file was preserved: " +
                    (actionError ?? string.Empty));
                return;
            }

            if (migratedFields > 0)
            {
                Interlocked.Increment(ref migratedFileCount);
                Interlocked.Add(ref migratedFieldCount, migratedFields);
                lastDiagnostic = "Migrated " + migratedFields.ToString() +
                    " legacy idol parameter key(s) without reserializing SavedData.";
                Debug.Log(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
            }
            else
            {
                lastDiagnostic =
                    "FixSaveFile required no legacy idol parameter-key migration; SavedData bytes were left untouched.";
            }
        }

        internal static bool TryMigrateLegacyGirlParameterNames(
            string originalJson,
            out string migratedJson,
            out int migratedFieldCount,
            out string error)
        {
            migratedJson = originalJson ?? string.Empty;
            migratedFieldCount = 0;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(originalJson))
            {
                error = "SavedData JSON is empty.";
                return false;
            }

            bool foundGirls;
            int ignoredPropertyStart;
            int ignoredPropertyEnd;
            int girlsStart;
            int girlsEnd;
            if (!RepairEnvelopeCodec.TryFindRootPropertyForRawMigration(
                    originalJson,
                    "data_girls__Girls",
                    out foundGirls,
                    out ignoredPropertyStart,
                    out ignoredPropertyEnd,
                    out girlsStart,
                    out girlsEnd,
                    out error))
            {
                return false;
            }

            if (!foundGirls || girlsStart >= girlsEnd || originalJson[girlsStart] != '[')
            {
                return true;
            }

            List<JsonSlice> girls;
            if (!TryEnumerateArray(originalJson, girlsStart, girlsEnd, out girls, out error))
            {
                return false;
            }

            List<int> underscoreInsertions = new List<int>();
            for (int girlIndex = 0; girlIndex < girls.Count; girlIndex++)
            {
                JsonSlice girl = girls[girlIndex];
                if (girl.Start >= girl.End || originalJson[girl.Start] != '{')
                {
                    continue;
                }

                string girlJson = originalJson.Substring(girl.Start, girl.End - girl.Start);
                bool foundParameters;
                int parametersStart;
                int parametersEnd;
                if (!TryFindLocalPropertyValue(
                        girlJson,
                        "parameters",
                        out foundParameters,
                        out parametersStart,
                        out parametersEnd,
                        out error))
                {
                    return false;
                }

                if (!foundParameters || parametersStart >= parametersEnd ||
                    girlJson[parametersStart] != '[')
                {
                    continue;
                }

                List<JsonSlice> parameters;
                int absoluteParametersStart = girl.Start + parametersStart;
                int absoluteParametersEnd = girl.Start + parametersEnd;
                if (!TryEnumerateArray(
                        originalJson,
                        absoluteParametersStart,
                        absoluteParametersEnd,
                        out parameters,
                        out error))
                {
                    return false;
                }

                for (int parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
                {
                    JsonSlice parameter = parameters[parameterIndex];
                    if (parameter.Start >= parameter.End || originalJson[parameter.Start] != '{')
                    {
                        continue;
                    }

                    string parameterJson = originalJson.Substring(
                        parameter.Start,
                        parameter.End - parameter.Start);

                    bool foundCurrent;
                    int ignoredValueStart;
                    int ignoredValueEnd;
                    if (!TryFindLocalPropertyValue(
                            parameterJson,
                            "_val",
                            out foundCurrent,
                            out ignoredValueStart,
                            out ignoredValueEnd,
                            out error))
                    {
                        return false;
                    }
                    if (foundCurrent)
                    {
                        continue;
                    }

                    bool foundLegacy;
                    int legacyPropertyStart;
                    if (!TryFindLocalProperty(
                            parameterJson,
                            "val",
                            out foundLegacy,
                            out legacyPropertyStart,
                            out ignoredValueStart,
                            out ignoredValueEnd,
                            out error))
                    {
                        return false;
                    }
                    if (!foundLegacy)
                    {
                        continue;
                    }

                    underscoreInsertions.Add(parameter.Start + legacyPropertyStart + 1);
                }
            }

            if (underscoreInsertions.Count == 0)
            {
                return true;
            }

            underscoreInsertions.Sort();
            StringBuilder builder = new StringBuilder(
                originalJson,
                originalJson.Length + underscoreInsertions.Count);
            for (int index = underscoreInsertions.Count - 1; index >= 0; index--)
            {
                builder.Insert(underscoreInsertions[index], '_');
            }

            migratedJson = builder.ToString();
            migratedFieldCount = underscoreInsertions.Count;
            return true;
        }

        private static bool TryFindLocalPropertyValue(
            string objectJson,
            string key,
            out bool found,
            out int valueStart,
            out int valueEnd,
            out string error)
        {
            int ignoredPropertyStart;
            return TryFindLocalProperty(
                objectJson,
                key,
                out found,
                out ignoredPropertyStart,
                out valueStart,
                out valueEnd,
                out error);
        }

        private static bool TryFindLocalProperty(
            string objectJson,
            string key,
            out bool found,
            out int propertyStart,
            out int valueStart,
            out int valueEnd,
            out string error)
        {
            int propertyEnd;
            return RepairEnvelopeCodec.TryFindRootPropertyForRawMigration(
                objectJson,
                key,
                out found,
                out propertyStart,
                out propertyEnd,
                out valueStart,
                out valueEnd,
                out error);
        }

        private static bool TryEnumerateArray(
            string json,
            int arrayStart,
            int arrayEnd,
            out List<JsonSlice> values,
            out string error)
        {
            values = new List<JsonSlice>();
            error = string.Empty;
            if (string.IsNullOrEmpty(json) || arrayStart < 0 || arrayEnd > json.Length ||
                arrayStart >= arrayEnd || json[arrayStart] != '[')
            {
                error = "SavedData JSON contains an invalid array boundary.";
                return false;
            }

            int index = RepairEnvelopeCodec.SkipWhitespaceForRawMigration(
                json,
                arrayStart + 1);
            if (index < arrayEnd && json[index] == ']')
            {
                return index + 1 == arrayEnd;
            }

            while (index < arrayEnd)
            {
                int valueEnd;
                if (!RepairEnvelopeCodec.TrySkipJsonValueForRawMigration(
                        json,
                        index,
                        out valueEnd) ||
                    valueEnd <= index || valueEnd > arrayEnd)
                {
                    error = "SavedData JSON contains an unterminated array value.";
                    return false;
                }

                values.Add(new JsonSlice(index, valueEnd));
                index = RepairEnvelopeCodec.SkipWhitespaceForRawMigration(json, valueEnd);
                if (index < arrayEnd && json[index] == ']')
                {
                    if (index + 1 != arrayEnd)
                    {
                        error = "SavedData JSON array has trailing content.";
                        return false;
                    }
                    return true;
                }

                if (index >= arrayEnd || json[index] != ',')
                {
                    error = "SavedData JSON array values are not comma-separated.";
                    return false;
                }

                index = RepairEnvelopeCodec.SkipWhitespaceForRawMigration(json, index + 1);
                if (index >= arrayEnd || json[index] == ']')
                {
                    error = "SavedData JSON array has a trailing comma.";
                    return false;
                }
            }

            error = "SavedData JSON array is unterminated.";
            return false;
        }

        private static void RecordFailure(string diagnostic)
        {
            Interlocked.Increment(ref failureCount);
            lastDiagnostic = diagnostic ?? "unknown type-preserving FixSaveFile failure";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }

        private struct JsonSlice
        {
            internal readonly int Start;
            internal readonly int End;

            internal JsonSlice(int start, int end)
            {
                Start = start;
                End = end;
            }
        }
    }
}
