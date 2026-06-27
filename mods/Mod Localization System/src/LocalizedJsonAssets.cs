using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace ModLocalizationSystem
{
    /// <summary>
    /// Applies localized whole-file JSON replacements to Idol Manager's normal
    /// mod discovery APIs. This also supports data-only mods because the shared
    /// localization runtime owns the Harmony patches. A matching English asset
    /// under Localization/en is required, so vanilla root-only mods are skipped.
    /// </summary>
    internal static class LocalizedJsonAssets
    {
        internal static bool IsSupportedRelativePath(string relativePath)
        {
            string normalized = NormalizeRelativePath(relativePath);
            return normalized.StartsWith("JSON/", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        internal static string ResolveForGame(string modDirectory, string relativePath)
        {
            string resolved = ModLocalization.ForDirectory(modDirectory).GetLocalizedAssetPath(relativePath);
            return string.IsNullOrEmpty(resolved) ? resolved : resolved.Replace('\\', '/');
        }

        internal static bool TryGetRelativePath(string filePath, string modDirectory, out string relativePath)
        {
            relativePath = string.Empty;
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(modDirectory))
            {
                return false;
            }

            try
            {
                string root = Path.GetFullPath(modDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(filePath);
                if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                relativePath = candidate.Substring(root.Length).Replace('\\', '/');
                return relativePath.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        internal static bool PathsEqual(string left, string right)
        {
            try
            {
                return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeRelativePath(string relativePath)
        {
            return (relativePath ?? string.Empty)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');
        }
    }

    [HarmonyPatch(typeof(Mods), nameof(Mods.GetFilePaths))]
    internal static class LocalizedJsonGetFilePathsPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(string path, ref List<string> __result)
        {
            if (__result == null || !LocalizedJsonAssets.IsSupportedRelativePath(path))
            {
                return;
            }

            string relativePath = (path ?? string.Empty)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int resultIndex = 0; resultIndex < __result.Count; resultIndex++)
            {
                string resultPath = __result[resultIndex];
                for (int modIndex = 0; modIndex < Mods._Mods.Count; modIndex++)
                {
                    Mods._mod mod = Mods._Mods[modIndex];
                    if (mod == null || string.IsNullOrEmpty(mod.Path))
                    {
                        continue;
                    }

                    string normalModPath = Path.Combine(mod.Path, relativePath);
                    if (!LocalizedJsonAssets.PathsEqual(resultPath, normalModPath))
                    {
                        continue;
                    }

                    string localizedPath = LocalizedJsonAssets.ResolveForGame(mod.Path, relativePath);
                    if (!string.IsNullOrEmpty(localizedPath))
                    {
                        __result[resultIndex] = localizedPath;
                    }
                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mods), nameof(Mods.GetModPaths))]
    internal static class LocalizedJsonGetModPathsPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(string path, ref List<Mods._modPath> __result)
        {
            if (__result == null || !LocalizedJsonAssets.IsSupportedRelativePath(path))
            {
                return;
            }

            string relativePath = (path ?? string.Empty)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int i = 0; i < __result.Count; i++)
            {
                Mods._modPath item = __result[i];
                if (item == null || item.mod == null || string.IsNullOrEmpty(item.mod.Path))
                {
                    continue;
                }

                string localizedPath = LocalizedJsonAssets.ResolveForGame(item.mod.Path, relativePath);
                if (!string.IsNullOrEmpty(localizedPath))
                {
                    item.path = localizedPath;
                }
            }
        }
    }

    // Characters is the one text-bearing JSON loader that bypasses both Mods
    // path helpers. Redirect its per-mod file before it is read.
    [HarmonyPatch(typeof(Characters), "LoadFilePath")]
    internal static class LocalizedJsonCharactersPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(ref string filePath, string ModPath)
        {
            string relativePath;
            if (!LocalizedJsonAssets.TryGetRelativePath(filePath, ModPath, out relativePath)
                || !LocalizedJsonAssets.IsSupportedRelativePath(relativePath))
            {
                return;
            }

            string localizedPath = LocalizedJsonAssets.ResolveForGame(ModPath, relativePath);
            if (!string.IsNullOrEmpty(localizedPath))
            {
                filePath = localizedPath;
            }
        }
    }
}
