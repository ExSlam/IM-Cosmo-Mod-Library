using System;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using ModLocalizationSystem;
using UnityEngine;

namespace StaffPortraits
{
    internal static class StaffPortraitCatalog
    {
        private static readonly List<StaffPortraitPack> Packs = new List<StaffPortraitPack>();
        private static readonly Dictionary<string, StaffPortraitAsset> AssetsByStableId =
            new Dictionary<string, StaffPortraitAsset>(StringComparer.Ordinal);
        private static readonly Dictionary<data_girls_textures._textureAsset, StaffPortraitAsset> AssetsByRuntimeObject =
            new Dictionary<data_girls_textures._textureAsset, StaffPortraitAsset>();
        private static bool loaded;
        private static readonly byte[] PngSignature =
        {
            137, 80, 78, 71, 13, 10, 26, 10
        };

        internal static IList<StaffPortraitPack> AllPacks
        {
            get
            {
                EnsureLoaded();
                return Packs;
            }
        }

        internal static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            if (Mods._Mods == null)
            {
                // PopupManager can initialize before the enabled-mod registry is ready. Leave the
                // catalog retryable so the first staff hire or Staff Stylist open can load packs.
                return;
            }

            loaded = true;
            Packs.Clear();
            AssetsByStableId.Clear();
            AssetsByRuntimeObject.Clear();

            HashSet<string> visitedPackPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int modIndex = 0; modIndex < Mods._Mods.Count; modIndex++)
            {
                Mods._mod mod = Mods._Mods[modIndex];
                if (mod == null || !mod.IsEnabled() || string.IsNullOrEmpty(mod.Path))
                {
                    continue;
                }

                string packPath = Path.Combine(
                    mod.Path,
                    StaffPortraitsConstants.StaffPortraitDirectoryName,
                    StaffPortraitsConstants.PackFileName);
                TryLoadPack(packPath, mod.Path, visitedPackPaths);
            }

            Packs.Sort(delegate(StaffPortraitPack left, StaffPortraitPack right)
            {
                return string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            });
        }

        internal static bool TryGetStableId(
            data_girls_textures._textureAsset runtimeAsset,
            out string stableId)
        {
            stableId = null;
            if (runtimeAsset == null)
            {
                return false;
            }

            EnsureLoaded();
            StaffPortraitAsset asset;
            if (!AssetsByRuntimeObject.TryGetValue(runtimeAsset, out asset) || asset == null)
            {
                return false;
            }

            stableId = asset.StableId;
            return !string.IsNullOrEmpty(stableId);
        }

        internal static bool IsStaffPortraitStableId(string stableId)
        {
            return !string.IsNullOrEmpty(stableId)
                && stableId.StartsWith(StaffPortraitsConstants.AssetIdPrefix, StringComparison.Ordinal);
        }

        internal static bool TryResolveStableId(
            string stableId,
            data_girls_textures._spriteType requestedType,
            out data_girls_textures._textureAsset runtimeAsset)
        {
            runtimeAsset = null;
            if (!IsStaffPortraitStableId(stableId))
            {
                return false;
            }

            EnsureLoaded();
            StaffPortraitAsset asset;
            if (!AssetsByStableId.TryGetValue(stableId, out asset)
                || asset == null
                || asset.GameAsset == null)
            {
                return true;
            }

            if (requestedType != data_girls_textures._spriteType.NONE && asset.Type != requestedType)
            {
                return true;
            }

            runtimeAsset = asset.GameAsset;
            return true;
        }

        private static void TryLoadPack(string packPath, string sourceModPath, HashSet<string> visitedPackPaths)
        {
            try
            {
                if (string.IsNullOrEmpty(packPath) || !File.Exists(packPath))
                {
                    return;
                }

                string fullPackPath = Path.GetFullPath(packPath);
                FileInfo packFileInfo = new FileInfo(fullPackPath);
                if (packFileInfo.Length <= 0 || packFileInfo.Length > StaffPortraitsConstants.MaximumPackJsonBytes)
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Skipped portrait pack with an invalid manifest size: " + fullPackPath);
                    return;
                }

                if (!visitedPackPaths.Add(fullPackPath))
                {
                    return;
                }

                JSONNode root = JSON.Parse(File.ReadAllText(fullPackPath));
                if (root == null || root["formatVersion"].AsInt != StaffPortraitsConstants.PackFormatVersion)
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Skipped portrait pack with an unsupported format version: " + fullPackPath);
                    return;
                }

                string packId = root["packId"].Value;
                if (!IsSafeIdentifier(packId))
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Skipped portrait pack with an invalid packId: " + fullPackPath);
                    return;
                }

                StaffPortraitPack pack = new StaffPortraitPack();
                pack.PackId = packId;
                pack.DisplayName = LocalizePackText(sourceModPath, root["displayNameKey"].Value, root["displayName"].Value, packId);
                pack.Author = SafeDisplayText(root["author"].Value, string.Empty);
                pack.RootDirectory = Path.GetDirectoryName(fullPackPath);

                JSONArray assetArray = root["assets"].AsArray;
                if (assetArray == null || assetArray.Count > StaffPortraitsConstants.MaximumAssetsPerPack)
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Skipped portrait pack with an invalid asset count: " + fullPackPath);
                    return;
                }

                HashSet<string> assetIds = new HashSet<string>(StringComparer.Ordinal);
                for (int assetIndex = 0; assetIndex < assetArray.Count; assetIndex++)
                {
                    TryAddAsset(pack, sourceModPath, assetArray[assetIndex], assetIds);
                }

                if (!pack.HasRequiredAssets)
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Skipped portrait pack without body, hair, and face assets: " + fullPackPath);
                    return;
                }

                if (IsPackIdLoaded(pack.PackId))
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Skipped portrait pack with a duplicate packId: " + pack.PackId);
                    return;
                }

                Packs.Add(pack);
                RegisterPackAssets(pack);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Could not load portrait pack: " + exception.Message);
            }
        }

        private static void TryAddAsset(
            StaffPortraitPack pack,
            string sourceModPath,
            JSONNode assetNode,
            HashSet<string> assetIds)
        {
            if (pack == null || assetNode == null)
            {
                return;
            }

            string assetId = assetNode["id"].Value;
            if (!IsSafeIdentifier(assetId) || !assetIds.Add(assetId))
            {
                return;
            }

            data_girls_textures._spriteType spriteType;
            if (!TryParseSpriteType(assetNode["type"].Value, out spriteType))
            {
                return;
            }

            string relativePath = assetNode["file"].Value;
            string fullPath;
            if (!TryResolveContainedFile(pack.RootDirectory, relativePath, out fullPath))
            {
                return;
            }

            int width;
            int height;
            if (!TryReadPngDimensions(fullPath, out width, out height))
            {
                return;
            }

            if (width != StaffPortraitsConstants.PortraitCanvasWidthPixels
                || height != StaffPortraitsConstants.PortraitCanvasHeightPixels)
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix
                    + "Skipped portrait asset that is not "
                    + StaffPortraitsConstants.PortraitCanvasWidthPixels
                    + " x "
                    + StaffPortraitsConstants.PortraitCanvasHeightPixels
                    + " pixels: "
                    + fullPath);
                return;
            }

            StaffPortraitAsset asset = new StaffPortraitAsset();
            asset.AssetId = assetId;
            asset.DisplayName = LocalizePackText(sourceModPath, assetNode["displayNameKey"].Value, assetNode["displayName"].Value, assetId);
            asset.FullPath = fullPath;
            asset.Type = spriteType;
            asset.StableId = BuildStableId(pack.PackId, spriteType, assetId);
            asset.GameAsset = CreateGameAsset(pack, asset);
            if (asset.GameAsset == null)
            {
                return;
            }

            GetAssetList(pack, spriteType).Add(asset);
        }

        private static data_girls_textures._textureAsset CreateGameAsset(
            StaffPortraitPack pack,
            StaffPortraitAsset asset)
        {
            data_girls_textures._textureAsset gameAsset =
                Activator.CreateInstance(typeof(data_girls_textures._textureAsset), true)
                as data_girls_textures._textureAsset;
            if (gameAsset == null)
            {
                return null;
            }

            gameAsset.type = asset.Type;
            gameAsset.path = asset.FullPath;
            gameAsset.body_id = StablePositiveHash(pack.PackId);
            gameAsset.part_id = StablePositiveHash(pack.PackId + "/" + asset.AssetId);
            return gameAsset;
        }

        private static bool IsPackIdLoaded(string packId)
        {
            for (int packIndex = 0; packIndex < Packs.Count; packIndex++)
            {
                StaffPortraitPack loadedPack = Packs[packIndex];
                if (loadedPack != null && string.Equals(loadedPack.PackId, packId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static void RegisterPackAssets(StaffPortraitPack pack)
        {
            RegisterAssetList(pack.BodyAssets);
            RegisterAssetList(pack.HairAssets);
            RegisterAssetList(pack.FaceAssets);
            RegisterAssetList(pack.AccessoryAssets);
        }

        private static void RegisterAssetList(List<StaffPortraitAsset> assets)
        {
            for (int assetIndex = 0; assetIndex < assets.Count; assetIndex++)
            {
                StaffPortraitAsset asset = assets[assetIndex];
                if (asset == null || asset.GameAsset == null)
                {
                    continue;
                }

                if (AssetsByStableId.ContainsKey(asset.StableId))
                {
                    Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Duplicate stable portrait asset ID ignored: " + asset.StableId);
                    continue;
                }

                AssetsByStableId.Add(asset.StableId, asset);
                AssetsByRuntimeObject.Add(asset.GameAsset, asset);
            }
        }

        internal static List<StaffPortraitAsset> GetAssetList(
            StaffPortraitPack pack,
            data_girls_textures._spriteType spriteType)
        {
            if (pack == null)
            {
                return new List<StaffPortraitAsset>();
            }

            switch (spriteType)
            {
                case data_girls_textures._spriteType.body:
                    return pack.BodyAssets;
                case data_girls_textures._spriteType.hair:
                    return pack.HairAssets;
                case data_girls_textures._spriteType.face:
                    return pack.FaceAssets;
                case data_girls_textures._spriteType.acc:
                    return pack.AccessoryAssets;
                default:
                    return new List<StaffPortraitAsset>();
            }
        }

        private static bool TryParseSpriteType(
            string value,
            out data_girls_textures._spriteType spriteType)
        {
            spriteType = data_girls_textures._spriteType.NONE;
            if (string.Equals(value, "body", StringComparison.OrdinalIgnoreCase))
            {
                spriteType = data_girls_textures._spriteType.body;
                return true;
            }
            if (string.Equals(value, "hair", StringComparison.OrdinalIgnoreCase))
            {
                spriteType = data_girls_textures._spriteType.hair;
                return true;
            }
            if (string.Equals(value, "face", StringComparison.OrdinalIgnoreCase))
            {
                spriteType = data_girls_textures._spriteType.face;
                return true;
            }
            if (string.Equals(value, "acc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "accessory", StringComparison.OrdinalIgnoreCase))
            {
                spriteType = data_girls_textures._spriteType.acc;
                return true;
            }
            return false;
        }

        private static bool TryResolveContainedFile(
            string packRoot,
            string relativePath,
            out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrEmpty(packRoot)
                || string.IsNullOrEmpty(relativePath)
                || Path.IsPathRooted(relativePath))
            {
                return false;
            }

            string normalizedRoot = Path.GetFullPath(packRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(Path.Combine(packRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }

        private static bool TryReadPngDimensions(string path, out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                FileInfo imageFileInfo = new FileInfo(path);
                if (imageFileInfo.Length <= 0 || imageFileInfo.Length > StaffPortraitsConstants.MaximumPortraitPngBytes)
                {
                    return false;
                }

                byte[] header = new byte[StaffPortraitsConstants.PngHeaderBytes];
                using (FileStream stream = File.OpenRead(path))
                {
                    if (stream.Read(header, 0, header.Length) != header.Length)
                    {
                        return false;
                    }
                }

                for (int signatureIndex = 0; signatureIndex < PngSignature.Length; signatureIndex++)
                {
                    if (header[signatureIndex] != PngSignature[signatureIndex])
                    {
                        return false;
                    }
                }

                width = ReadBigEndianInt32(header, StaffPortraitsConstants.PngWidthOffset);
                height = ReadBigEndianInt32(header, StaffPortraitsConstants.PngHeightOffset);
                return width > 0 && height > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            int value = 0;
            for (int byteIndex = 0; byteIndex < StaffPortraitsConstants.Int32ByteCount; byteIndex++)
            {
                value = (value << StaffPortraitsConstants.BitsPerByte) | bytes[offset + byteIndex];
            }
            return value;
        }

        private static string BuildStableId(
            string packId,
            data_girls_textures._spriteType spriteType,
            string assetId)
        {
            return StaffPortraitsConstants.AssetIdPrefix
                + packId
                + StaffPortraitsConstants.StableIdSeparator
                + spriteType.ToString()
                + StaffPortraitsConstants.StableIdSeparator
                + assetId;
        }

        private static int StablePositiveHash(string value)
        {
            unchecked
            {
                int hash = StaffPortraitsConstants.StableHashSeed;
                string source = value ?? string.Empty;
                for (int characterIndex = 0; characterIndex < source.Length; characterIndex++)
                {
                    hash = hash * StaffPortraitsConstants.StableHashMultiplier + source[characterIndex];
                }
                if (hash == int.MinValue)
                {
                    return int.MaxValue;
                }

                int positiveHash = Math.Abs(hash);
                return positiveHash == 0
                    ? StaffPortraitsConstants.MinimumPositiveHashValue
                    : positiveHash;
            }
        }

        private static bool IsSafeIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > StaffPortraitsConstants.MaximumIdentifierCharacters)
            {
                return false;
            }

            for (int characterIndex = 0; characterIndex < value.Length; characterIndex++)
            {
                char character = value[characterIndex];
                if (!(char.IsLetterOrDigit(character) || character == '.' || character == '_' || character == '-'))
                {
                    return false;
                }
            }
            return true;
        }


        private static string LocalizePackText(
            string sourceModPath,
            string localizationKey,
            string fallbackText,
            string finalFallback)
        {
            string fallback = SafeDisplayText(fallbackText, finalFallback);
            if (string.IsNullOrEmpty(sourceModPath) || string.IsNullOrEmpty(localizationKey))
            {
                return fallback;
            }

            return ModLocalization.ForDirectory(sourceModPath).Get(localizationKey, fallback);
        }

        private static string SafeDisplayText(string value, string fallback)
        {
            string trimmed = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return fallback;
            }
            return trimmed.Length <= StaffPortraitsConstants.MaximumDisplayTextCharacters
                ? trimmed
                : trimmed.Substring(0, StaffPortraitsConstants.MaximumDisplayTextCharacters);
        }
    }
}
