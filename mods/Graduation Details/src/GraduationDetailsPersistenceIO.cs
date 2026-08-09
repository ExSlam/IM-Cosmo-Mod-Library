using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GraduationDetails
{
    /// <summary>
    /// Parameterized filesystem primitives used by the persistence layer. This type deliberately
    /// has no Unity dependencies so containment, fingerprint, and atomic-pointer behavior can be
    /// regression-tested against disposable directories.
    /// </summary>
    internal static class GraduationDetailsPersistenceIO
    {
        internal sealed class FileFingerprint
        {
            internal bool Exists;
            internal long Length;
            internal long LastWriteUtcTicks;
            internal string Sha256 = "";

            internal bool SameFileState(FileFingerprint other)
            {
                return other != null
                    && Exists == other.Exists
                    && Length == other.Length
                    && LastWriteUtcTicks == other.LastWriteUtcTicks
                    && string.Equals(Sha256, other.Sha256, StringComparison.Ordinal);
            }
        }

        internal static bool IsSafeLeafFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)
                || string.Equals(fileName, ".", StringComparison.Ordinal)
                || string.Equals(fileName, "..", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                return !Path.IsPathRooted(fileName)
                    && string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
                    && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                    && fileName.IndexOf(Path.DirectorySeparatorChar) < 0
                    && fileName.IndexOf(Path.AltDirectorySeparatorChar) < 0;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryGetSafeLeafPath(
            string rootDirectory,
            string fileName,
            bool forWrite,
            out string path)
        {
            path = "";
            return IsSafeLeafFileName(fileName)
                && TryGetContainedPath(rootDirectory, fileName, forWrite, out path);
        }

        internal static bool TryGetContainedPath(
            string rootDirectory,
            string relativePath,
            bool forWrite,
            out string path)
        {
            path = "";
            if (string.IsNullOrWhiteSpace(rootDirectory)
                || string.IsNullOrWhiteSpace(relativePath)
                || Path.IsPathRooted(relativePath))
            {
                return false;
            }

            try
            {
                string root = NormalizeDirectory(rootDirectory);
                string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
                if (!IsPathContainedByNormalizedRoot(root, candidate)
                    || HasReparsePointBelowRoot(root, candidate, forWrite)
                    || ((File.Exists(candidate) || Directory.Exists(candidate))
                        && (File.GetAttributes(candidate) & FileAttributes.ReparsePoint) != 0))
                {
                    return false;
                }
                path = candidate;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsPathContainedBy(string rootDirectory, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || string.IsNullOrWhiteSpace(candidatePath))
            {
                return false;
            }
            try
            {
                return IsPathContainedByNormalizedRoot(
                    NormalizeDirectory(rootDirectory),
                    Path.GetFullPath(candidatePath));
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryValidatePathUnderRoot(
            string rootDirectory,
            string candidatePath,
            bool forWrite,
            out string normalizedPath)
        {
            normalizedPath = "";
            try
            {
                string root = NormalizeDirectory(rootDirectory);
                string candidate = Path.GetFullPath(candidatePath);
                string prefix = root + Path.DirectorySeparatorChar;
                if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                string relative = candidate.Substring(prefix.Length);
                return TryGetContainedPath(root, relative, forWrite, out normalizedPath)
                    && string.Equals(
                        candidate,
                        normalizedPath,
                        StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                normalizedPath = "";
                return false;
            }
        }

        internal static bool TryNormalizeFilePath(string filePath, out string normalizedPath)
        {
            normalizedPath = "";
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }
            try
            {
                normalizedPath = Path.GetFullPath(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryCaptureFingerprint(string filePath, out FileFingerprint fingerprint)
        {
            fingerprint = new FileFingerprint();
            string normalized;
            if (!TryNormalizeFilePath(filePath, out normalized))
            {
                return false;
            }
            try
            {
                FileInfo before = new FileInfo(normalized);
                before.Refresh();
                if (!before.Exists)
                {
                    // This represents a real missing-file sample. A later creator is a subsequent
                    // transition and will be observed by the next fingerprint capture.
                    return true;
                }

                long beforeLength = before.Length;
                long beforeLastWriteTicks = before.LastWriteTimeUtc.Ticks;
                if (beforeLength < 0L || beforeLength > int.MaxValue)
                {
                    fingerprint = null;
                    return false;
                }

                byte[] firstRead = new byte[(int)beforeLength];
                byte[] secondRead = new byte[(int)beforeLength];
                // FileShare.ReadWrite avoids causing vanilla's asynchronous File.WriteAllBytes to
                // fail with a sharing violation. Independent streams avoid FileStream read-buffer
                // reuse; two identical complete reads plus metadata checks are required.
                if (!TryReadCompleteFile(normalized, beforeLength, firstRead))
                {
                    fingerprint = null;
                    return false;
                }

                FileInfo middle = new FileInfo(normalized);
                middle.Refresh();
                if (!middle.Exists
                    || middle.Length != beforeLength
                    || middle.LastWriteTimeUtc.Ticks != beforeLastWriteTicks
                    || !TryReadCompleteFile(normalized, beforeLength, secondRead)
                    || !BytesEqual(firstRead, secondRead))
                {
                    fingerprint = null;
                    return false;
                }

                FileInfo after = new FileInfo(normalized);
                after.Refresh();
                if (!after.Exists
                    || after.Length != beforeLength
                    || after.LastWriteTimeUtc.Ticks != beforeLastWriteTicks)
                {
                    fingerprint = null;
                    return false;
                }

                fingerprint.Exists = true;
                fingerprint.Length = beforeLength;
                fingerprint.LastWriteUtcTicks = beforeLastWriteTicks;
                fingerprint.Sha256 = ComputeSha256(firstRead);
                return true;
            }
            catch
            {
                fingerprint = null;
                return false;
            }
        }

        private static bool TryReadCompleteFile(
            string filePath,
            long expectedLength,
            byte[] buffer)
        {
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {
                return stream.Length == expectedLength
                    && ReadExactly(stream, buffer)
                    && stream.Position == expectedLength
                    && stream.Length == expectedLength;
            }
        }

        private static bool ReadExactly(FileStream stream, byte[] buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read <= 0)
                {
                    return false;
                }
                offset += read;
            }
            // If the file grew without stream.Length reflecting it at an earlier check, this
            // extra read prevents accepting a truncated prefix as the complete payload.
            return stream.ReadByte() == -1;
        }

        private static bool BytesEqual(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }
            return true;
        }

        internal static string ComputeSha256(byte[] bytes)
        {
            if (bytes == null)
            {
                return "";
            }
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        internal static bool HasExplicitRootArrayProperty(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }
            try
            {
                int index = 0;
                SkipWhitespace(json, ref index);
                if (!Consume(json, ref index, '{'))
                {
                    return false;
                }
                bool found = false;
                while (true)
                {
                    SkipWhitespace(json, ref index);
                    if (Consume(json, ref index, '}'))
                    {
                        SkipWhitespace(json, ref index);
                        return found && index == json.Length;
                    }

                    string name;
                    if (!TryReadJsonString(json, ref index, out name))
                    {
                        return false;
                    }
                    SkipWhitespace(json, ref index);
                    if (!Consume(json, ref index, ':'))
                    {
                        return false;
                    }
                    SkipWhitespace(json, ref index);
                    if (string.Equals(name, propertyName, StringComparison.Ordinal))
                    {
                        if (found || index >= json.Length || json[index] != '[')
                        {
                            return false;
                        }
                        found = true;
                    }
                    if (!SkipJsonValue(json, ref index))
                    {
                        return false;
                    }
                    SkipWhitespace(json, ref index);
                    if (Consume(json, ref index, ','))
                    {
                        continue;
                    }
                    if (!Consume(json, ref index, '}'))
                    {
                        return false;
                    }
                    SkipWhitespace(json, ref index);
                    return found && index == json.Length;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool SkipJsonValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
            {
                return false;
            }
            if (json[index] == '"')
            {
                string ignored;
                return TryReadJsonString(json, ref index, out ignored);
            }
            if (json[index] == '{')
            {
                return SkipJsonObject(json, ref index);
            }
            if (json[index] == '[')
            {
                return SkipJsonArray(json, ref index);
            }
            if (MatchesToken(json, ref index, "true")
                || MatchesToken(json, ref index, "false")
                || MatchesToken(json, ref index, "null"))
            {
                return true;
            }
            return SkipJsonNumber(json, ref index);
        }

        private static bool SkipJsonObject(string json, ref int index)
        {
            if (!Consume(json, ref index, '{'))
            {
                return false;
            }
            SkipWhitespace(json, ref index);
            if (Consume(json, ref index, '}'))
            {
                return true;
            }
            while (true)
            {
                string ignored;
                if (!TryReadJsonString(json, ref index, out ignored))
                {
                    return false;
                }
                SkipWhitespace(json, ref index);
                if (!Consume(json, ref index, ':'))
                {
                    return false;
                }
                if (!SkipJsonValue(json, ref index))
                {
                    return false;
                }
                SkipWhitespace(json, ref index);
                if (Consume(json, ref index, '}'))
                {
                    return true;
                }
                if (!Consume(json, ref index, ','))
                {
                    return false;
                }
                SkipWhitespace(json, ref index);
            }
        }

        private static bool SkipJsonArray(string json, ref int index)
        {
            if (!Consume(json, ref index, '['))
            {
                return false;
            }
            SkipWhitespace(json, ref index);
            if (Consume(json, ref index, ']'))
            {
                return true;
            }
            while (true)
            {
                if (!SkipJsonValue(json, ref index))
                {
                    return false;
                }
                SkipWhitespace(json, ref index);
                if (Consume(json, ref index, ']'))
                {
                    return true;
                }
                if (!Consume(json, ref index, ','))
                {
                    return false;
                }
                SkipWhitespace(json, ref index);
            }
        }

        private static bool MatchesToken(string json, ref int index, string token)
        {
            if (index + token.Length > json.Length
                || !string.Equals(
                    json.Substring(index, token.Length),
                    token,
                    StringComparison.Ordinal))
            {
                return false;
            }
            int end = index + token.Length;
            if (end < json.Length
                && !char.IsWhiteSpace(json[end])
                && json[end] != ','
                && json[end] != ']'
                && json[end] != '}')
            {
                return false;
            }
            index = end;
            return true;
        }

        private static bool SkipJsonNumber(string json, ref int index)
        {
            int start = index;
            if (index < json.Length && json[index] == '-')
            {
                index++;
            }
            if (index >= json.Length)
            {
                index = start;
                return false;
            }
            if (json[index] == '0')
            {
                index++;
            }
            else if (json[index] >= '1' && json[index] <= '9')
            {
                while (index < json.Length && json[index] >= '0' && json[index] <= '9')
                {
                    index++;
                }
            }
            else
            {
                index = start;
                return false;
            }
            if (index < json.Length && json[index] == '.')
            {
                index++;
                int fractionStart = index;
                while (index < json.Length && json[index] >= '0' && json[index] <= '9')
                {
                    index++;
                }
                if (index == fractionStart)
                {
                    index = start;
                    return false;
                }
            }
            if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
            {
                index++;
                if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                {
                    index++;
                }
                int exponentStart = index;
                while (index < json.Length && json[index] >= '0' && json[index] <= '9')
                {
                    index++;
                }
                if (index == exponentStart)
                {
                    index = start;
                    return false;
                }
            }
            if (index < json.Length
                && !char.IsWhiteSpace(json[index])
                && json[index] != ','
                && json[index] != ']'
                && json[index] != '}')
            {
                index = start;
                return false;
            }
            return index > start;
        }

        private static bool TryReadJsonString(string json, ref int index, out string value)
        {
            value = "";
            if (!Consume(json, ref index, '"'))
            {
                return false;
            }
            StringBuilder builder = new StringBuilder();
            while (index < json.Length)
            {
                char current = json[index++];
                if (current == '"')
                {
                    value = builder.ToString();
                    return true;
                }
                if (current != '\\')
                {
                    if (current < 0x20)
                    {
                        return false;
                    }
                    builder.Append(current);
                    continue;
                }
                if (index >= json.Length)
                {
                    return false;
                }
                char escaped = json[index++];
                switch (escaped)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length)
                        {
                            return false;
                        }
                        int code;
                        if (!int.TryParse(
                            json.Substring(index, 4),
                            System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out code))
                        {
                            return false;
                        }
                        builder.Append((char)code);
                        index += 4;
                        break;
                    default:
                        return false;
                }
            }
            return false;
        }

        private static void SkipWhitespace(string value, ref int index)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index++;
            }
        }

        private static bool Consume(string value, ref int index, char expected)
        {
            if (index >= value.Length || value[index] != expected)
            {
                return false;
            }
            index++;
            return true;
        }

        internal static void WriteUtf8Durable(string filePath, string content)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(content ?? "");
            string parent = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(parent))
            {
                throw new IOException("A durable file must have a parent directory.");
            }
            Directory.CreateDirectory(parent);
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        internal static void CopyFileDurable(string sourcePath, string destinationPath)
        {
            string parent = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(parent))
            {
                throw new IOException("A copied file must have a parent directory.");
            }
            Directory.CreateDirectory(parent);
            using (FileStream source = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            using (FileStream destination = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                source.CopyTo(destination);
                destination.Flush(true);
            }
        }

        internal static void WritePointerAtomically(
            string pointerPath,
            string backupPath,
            string pointerValue)
        {
            string parent = Path.GetDirectoryName(pointerPath);
            if (string.IsNullOrEmpty(parent))
            {
                throw new IOException("An active snapshot pointer must have a parent directory.");
            }
            Directory.CreateDirectory(parent);
            string temporaryPath = pointerPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            WriteUtf8Durable(temporaryPath, pointerValue ?? "");
            try
            {
                if (File.Exists(pointerPath))
                {
                    File.Replace(temporaryPath, pointerPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, pointerPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static string NormalizeDirectory(string directory)
        {
            return Path.GetFullPath(directory).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
        }

        private static bool IsPathContainedByNormalizedRoot(string root, string candidate)
        {
            return candidate.StartsWith(
                root + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasReparsePointBelowRoot(
            string normalizedRoot,
            string candidatePath,
            bool forWrite)
        {
            if (Directory.Exists(normalizedRoot)
                && (File.GetAttributes(normalizedRoot) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            string relative = candidatePath.Substring(
                (normalizedRoot + Path.DirectorySeparatorChar).Length);
            string[] parts = relative.Split(
                new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
            string current = normalizedRoot;
            int directoryPartCount = forWrite ? Math.Max(0, parts.Length - 1) : parts.Length;
            for (int i = 0; i < directoryPartCount; i++)
            {
                current = Path.Combine(current, parts[i]);
                if (Directory.Exists(current)
                    && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
