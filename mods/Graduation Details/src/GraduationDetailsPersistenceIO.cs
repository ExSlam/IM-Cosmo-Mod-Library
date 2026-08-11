using System;
using System.IO;
using System.Text;

namespace GraduationDetails
{
    /// <summary>
    /// Small filesystem boundary for the lightweight persistence layer. It provides
    /// containment and durable-write primitives only; vanilla files are never
    /// written, fingerprinted, renamed, or deleted here.
    /// </summary>
    internal static class GraduationDetailsPersistenceIO
    {
        internal static bool IsSafeLeafFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                string.Equals(fileName, ".", StringComparison.Ordinal) ||
                string.Equals(fileName, "..", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                return !Path.IsPathRooted(fileName) &&
                    string.Equals(
                        Path.GetFileName(fileName),
                        fileName,
                        StringComparison.Ordinal) &&
                    fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                    fileName.IndexOf(Path.DirectorySeparatorChar) < 0 &&
                    fileName.IndexOf(Path.AltDirectorySeparatorChar) < 0;
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
            return IsSafeLeafFileName(fileName) &&
                TryGetContainedPath(
                    rootDirectory,
                    fileName,
                    forWrite,
                    out path);
        }

        internal static bool TryGetContainedPath(
            string rootDirectory,
            string relativePath,
            bool forWrite,
            out string path)
        {
            path = "";
            if (string.IsNullOrWhiteSpace(rootDirectory) ||
                string.IsNullOrWhiteSpace(relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                return false;
            }

            try
            {
                string root = NormalizeDirectory(rootDirectory);
                string candidate = Path.GetFullPath(
                    Path.Combine(root, relativePath));
                if (!IsPathContainedByNormalizedRoot(root, candidate) ||
                    HasReparsePointBelowRoot(root, candidate, forWrite) ||
                    ((File.Exists(candidate) || Directory.Exists(candidate)) &&
                     (File.GetAttributes(candidate) &
                        FileAttributes.ReparsePoint) != 0))
                {
                    return false;
                }

                path = candidate;
                return true;
            }
            catch
            {
                path = "";
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
            if (string.IsNullOrWhiteSpace(rootDirectory) ||
                string.IsNullOrWhiteSpace(candidatePath))
            {
                return false;
            }

            try
            {
                string root = NormalizeDirectory(rootDirectory);
                string candidate = Path.GetFullPath(candidatePath);
                string prefix = BuildDirectoryPrefix(root);
                if (!candidate.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string relativePath = candidate.Substring(prefix.Length);
                return TryGetContainedPath(
                        root,
                        relativePath,
                        forWrite,
                        out normalizedPath) &&
                    string.Equals(
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

        internal static void WriteUtf8Durable(
            string filePath,
            string content)
        {
            string parentDirectory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                throw new IOException(
                    "A durable file must have a parent directory.");
            }

            Directory.CreateDirectory(parentDirectory);
            byte[] bytes = new UTF8Encoding(false).GetBytes(content ?? "");
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

        internal static void CopyFileDurable(
            string sourcePath,
            string destinationPath)
        {
            string parentDirectory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                throw new IOException(
                    "A copied file must have a parent directory.");
            }

            Directory.CreateDirectory(parentDirectory);
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

        private static string NormalizeDirectory(string directoryPath)
        {
            string normalizedPath = Path.GetFullPath(directoryPath);
            string pathRoot = Path.GetPathRoot(normalizedPath) ?? "";
            int minimumLength = pathRoot.Length;
            int endIndex = normalizedPath.Length;
            while (endIndex > minimumLength)
            {
                char character = normalizedPath[endIndex - 1];
                if (character != Path.DirectorySeparatorChar &&
                    character != Path.AltDirectorySeparatorChar)
                {
                    break;
                }
                endIndex--;
            }

            return endIndex == normalizedPath.Length
                ? normalizedPath
                : normalizedPath.Substring(0, endIndex);
        }

        private static string BuildDirectoryPrefix(string directoryPath)
        {
            return directoryPath + Path.DirectorySeparatorChar;
        }

        private static bool IsPathContainedByNormalizedRoot(
            string normalizedRoot,
            string candidatePath)
        {
            return candidatePath.StartsWith(
                BuildDirectoryPrefix(normalizedRoot),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasReparsePointBelowRoot(
            string normalizedRoot,
            string candidatePath,
            bool forWrite)
        {
            if (Directory.Exists(normalizedRoot) &&
                (File.GetAttributes(normalizedRoot) &
                    FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            string relativePath = candidatePath.Substring(
                BuildDirectoryPrefix(normalizedRoot).Length);
            string[] pathParts = relativePath.Split(
                new char[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);
            int directoryPartCount = forWrite
                ? Math.Max(0, pathParts.Length - 1)
                : pathParts.Length;
            string currentPath = normalizedRoot;
            for (int index = 0; index < directoryPartCount; index++)
            {
                currentPath = Path.Combine(currentPath, pathParts[index]);
                if (Directory.Exists(currentPath) &&
                    (File.GetAttributes(currentPath) &
                        FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
