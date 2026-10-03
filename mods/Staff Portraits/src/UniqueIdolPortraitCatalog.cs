using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace StaffPortraits
{
    /// <summary>
    /// Builds Staff Stylist-only pack views over the unique-idol portrait assets that Idol Manager
    /// has already loaded. These wrappers never replace or re-register the source assets: applying
    /// one keeps the source mod's original texture asset so vanilla save/load retains its own ID.
    /// </summary>
    internal static class UniqueIdolPortraitCatalog
    {
        private const string DefaultTextureAssetModName = "";
        private const string GroupKeySeparator = "|";
        private const string DisplaySourceSeparator = " — ";
        private const string FallbackPackIdPrefix = "unique:";
        private const string FallbackPackDisplayPrefix = "#";

        private static readonly List<StaffPortraitPack> Packs = new List<StaffPortraitPack>();
        private static readonly Dictionary<data_girls_textures._textureAsset, StaffPortraitAsset> AssetsByRuntimeObject =
            new Dictionary<data_girls_textures._textureAsset, StaffPortraitAsset>();
        private static readonly Dictionary<string, StaffPortraitAsset> AssetsBySourceStableId =
            new Dictionary<string, StaffPortraitAsset>(StringComparer.Ordinal);

        internal static IList<StaffPortraitPack> AllPacks
        {
            get { return Packs; }
        }

        internal static void Refresh()
        {
            Packs.Clear();
            AssetsByRuntimeObject.Clear();
            AssetsBySourceStableId.Clear();

            HashSet<string> visitedDiscoveryModNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visitedGroupKeys = new HashSet<string>(StringComparer.Ordinal);
            DiscoverGroupsForMod(DefaultTextureAssetModName, visitedDiscoveryModNames, visitedGroupKeys);

            if (Mods._Mods != null)
            {
                for (int modIndex = 0; modIndex < Mods._Mods.Count; modIndex++)
                {
                    Mods._mod mod = Mods._Mods[modIndex];
                    if (mod == null || !mod.IsEnabled())
                    {
                        continue;
                    }

                    DiscoverGroupsForMod(mod.ModName ?? DefaultTextureAssetModName,
                        visitedDiscoveryModNames, visitedGroupKeys);
                }
            }

            Packs.Sort(delegate(StaffPortraitPack left, StaffPortraitPack right)
            {
                return string.Compare(left.DisplayName, right.DisplayName,
                    StringComparison.CurrentCultureIgnoreCase);
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

            StaffPortraitAsset asset;
            if (AssetsByRuntimeObject.TryGetValue(runtimeAsset, out asset)
                && asset != null
                && !string.IsNullOrEmpty(asset.StableId))
            {
                stableId = asset.StableId;
                return true;
            }

            string sourceStableId = GetSourceStableId(runtimeAsset);
            if (string.IsNullOrEmpty(sourceStableId)
                || !AssetsBySourceStableId.TryGetValue(sourceStableId, out asset)
                || asset == null)
            {
                return false;
            }

            stableId = sourceStableId;
            return true;
        }

        private static void DiscoverGroupsForMod(
            string discoveryModName,
            HashSet<string> visitedDiscoveryModNames,
            HashSet<string> visitedGroupKeys)
        {
            if (!visitedDiscoveryModNames.Add(discoveryModName ?? DefaultTextureAssetModName))
            {
                return;
            }

            List<data_girls_textures._textureAsset> bodyAssets = data_girls_textures.GetTextureAssets(
                data_girls_textures._spriteType.body,
                StaffPortraitsConstants.NoSelectionIndex,
                discoveryModName ?? DefaultTextureAssetModName);
            if (bodyAssets == null)
            {
                return;
            }

            for (int assetIndex = 0; assetIndex < bodyAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset bodyAsset = bodyAssets[assetIndex];
                if (bodyAsset == null
                    || bodyAsset.type != data_girls_textures._spriteType.body
                    || !bodyAsset.Unique)
                {
                    continue;
                }

                string effectiveModName = bodyAsset.Add_To_Default
                    ? DefaultTextureAssetModName
                    : bodyAsset.ModName ?? discoveryModName ?? DefaultTextureAssetModName;
                string groupKey = BuildGroupKey(effectiveModName, bodyAsset.body_id);
                if (!visitedGroupKeys.Add(groupKey))
                {
                    continue;
                }

                StaffPortraitPack pack = BuildPack(bodyAsset, effectiveModName);
                if (pack == null || !pack.HasRequiredAssets)
                {
                    continue;
                }

                Packs.Add(pack);
                RegisterPackAssets(pack);
            }
        }

        private static StaffPortraitPack BuildPack(
            data_girls_textures._textureAsset referenceBodyAsset,
            string effectiveModName)
        {
            if (referenceBodyAsset == null)
            {
                return null;
            }

            StaffPortraitPack pack = new StaffPortraitPack();
            pack.PackId = FallbackPackIdPrefix + BuildGroupKey(effectiveModName, referenceBodyAsset.body_id);
            pack.DisplayName = BuildPackDisplayName(referenceBodyAsset);
            pack.Author = string.Empty;
            pack.RootDirectory = null;

            AddPartAssets(pack, data_girls_textures._spriteType.body, referenceBodyAsset.body_id, effectiveModName);
            AddPartAssets(pack, data_girls_textures._spriteType.hair, referenceBodyAsset.body_id, effectiveModName);
            AddPartAssets(pack, data_girls_textures._spriteType.face, referenceBodyAsset.body_id, effectiveModName);
            AddPartAssets(pack, data_girls_textures._spriteType.acc, referenceBodyAsset.body_id, effectiveModName);
            return pack;
        }

        private static void AddPartAssets(
            StaffPortraitPack pack,
            data_girls_textures._spriteType spriteType,
            int bodyId,
            string effectiveModName)
        {
            List<data_girls_textures._textureAsset> loadedAssets = data_girls_textures.GetTextureAssets(
                spriteType,
                bodyId,
                effectiveModName ?? DefaultTextureAssetModName);
            if (loadedAssets == null)
            {
                return;
            }

            List<data_girls_textures._textureAsset> usableAssets = new List<data_girls_textures._textureAsset>();
            for (int assetIndex = 0; assetIndex < loadedAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset sourceAsset = loadedAssets[assetIndex];
                // These assets have already been accepted and loaded by Idol Manager's own
                // unique-idol pipeline. Do not impose Staff Pack's 1024x1500 authoring contract
                // here: existing unique-idol mods can legitimately use a different full-canvas
                // width while still keeping their own body/face/hair/accessory layers aligned.
                if (sourceAsset == null
                    || sourceAsset.type != spriteType
                    || usableAssets.Contains(sourceAsset))
                {
                    continue;
                }

                usableAssets.Add(sourceAsset);
            }

            usableAssets.Sort(CompareSourceAssets);
            List<StaffPortraitAsset> targetAssets = StaffPortraitCatalog.GetAssetList(pack, spriteType);
            for (int assetIndex = 0; assetIndex < usableAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset sourceAsset = usableAssets[assetIndex];
                string sourceStableId = GetSourceStableId(sourceAsset);
                if (string.IsNullOrEmpty(sourceStableId))
                {
                    continue;
                }

                StaffPortraitAsset asset = new StaffPortraitAsset();
                asset.AssetId = sourceStableId;
                asset.DisplayName = GetAssetDisplayName(sourceAsset);
                asset.StableId = sourceStableId;
                asset.FullPath = sourceAsset.path;
                asset.Type = spriteType;
                asset.GameAsset = sourceAsset;
                targetAssets.Add(asset);
            }
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

                if (!AssetsByRuntimeObject.ContainsKey(asset.GameAsset))
                {
                    AssetsByRuntimeObject.Add(asset.GameAsset, asset);
                }
                if (!string.IsNullOrEmpty(asset.StableId)
                    && !AssetsBySourceStableId.ContainsKey(asset.StableId))
                {
                    AssetsBySourceStableId.Add(asset.StableId, asset);
                }
            }
        }

        private static string BuildGroupKey(string modName, int bodyId)
        {
            return string.Concat(
                modName ?? DefaultTextureAssetModName,
                GroupKeySeparator,
                bodyId.ToString(CultureInfo.InvariantCulture));
        }

        private static string BuildPackDisplayName(data_girls_textures._textureAsset referenceBodyAsset)
        {
            string characterName = GetAssignedAssetName(referenceBodyAsset);
            string sourceTitle = GetSupplyingModTitle(referenceBodyAsset.ModName);
            if (string.IsNullOrEmpty(characterName))
            {
                characterName = FallbackPackDisplayPrefix
                    + referenceBodyAsset.body_id.ToString(CultureInfo.CurrentCulture);
            }

            if (string.IsNullOrEmpty(sourceTitle))
            {
                return characterName;
            }

            return string.Concat(characterName, DisplaySourceSeparator, sourceTitle);
        }

        private static string GetSupplyingModTitle(string assetModName)
        {
            if (string.IsNullOrEmpty(assetModName) || Mods._Mods == null)
            {
                return string.Empty;
            }

            for (int modIndex = 0; modIndex < Mods._Mods.Count; modIndex++)
            {
                Mods._mod mod = Mods._Mods[modIndex];
                if (mod == null
                    || !string.Equals(mod.ModName, assetModName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return string.IsNullOrEmpty(mod.Title) ? mod.ModName : mod.Title;
            }

            return assetModName;
        }

        private static string GetAssignedAssetName(data_girls_textures._textureAsset asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(asset.first_name))
            {
                return asset.last_name ?? asset.Unique_ID ?? string.Empty;
            }
            if (string.IsNullOrEmpty(asset.last_name))
            {
                return asset.first_name;
            }

            return string.Concat(asset.first_name, " ", asset.last_name);
        }

        private static string GetAssetDisplayName(data_girls_textures._textureAsset asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            string fileName = string.IsNullOrEmpty(asset.path)
                ? string.Empty
                : Path.GetFileNameWithoutExtension(asset.path);
            return !string.IsNullOrEmpty(fileName)
                ? fileName
                : asset.part_id.ToString(CultureInfo.CurrentCulture);
        }

        private static string GetSourceStableId(data_girls_textures._textureAsset asset)
        {
            if (asset == null)
            {
                return null;
            }

            try
            {
                string stableId = asset.GetID();
                return string.IsNullOrEmpty(stableId) ? null : stableId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static int CompareSourceAssets(
            data_girls_textures._textureAsset left,
            data_girls_textures._textureAsset right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left == null)
            {
                return 1;
            }
            if (right == null)
            {
                return -1;
            }

            int partIdComparison = left.part_id.CompareTo(right.part_id);
            return partIdComparison != 0
                ? partIdComparison
                : string.Compare(left.path, right.path, StringComparison.OrdinalIgnoreCase);
        }
    }
}
