using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StaffPortraits
{
    internal static class StaffPortraitAssignment
    {
        internal static bool IsListedStaff(staff._staff staffer)
        {
            return staffer != null
                && staffer.type != staff._type.player
                && staffer.type != staff._type.player_female;
        }

        internal static bool IsEditableStaff(staff._staff staffer)
        {
            if (!IsListedStaff(staffer) || staffer.UniqueType != staff._staff._unique_type.NONE)
                return false;
            if (!staffer.IsIdol()) return true;

            // Vanilla IsIdol only checks for texture assets, so it also reports ordinary
            // staff as idols after this mod assigns a portrait. Permit our own parts to be
            // edited again while preserving former idols and other authored portraits.
            foreach (data_girls.girls._textureAsset part in staffer.textureAssets)
            {
                string stableId;
                if (part == null || !TryGetManagedStableId(part.asset, out stableId))
                    return false;
            }
            return true;
        }

        internal static void BackfillEligibleStaff()
        {
            StaffPortraitCatalog.EnsureLoaded();
            if (staff.Staff == null || StaffPortraitCatalog.AllPacks.Count == 0)
            {
                return;
            }

            for (int staffIndex = 0; staffIndex < staff.Staff.Count; staffIndex++)
            {
                TryAssignIfMissing(staff.Staff[staffIndex]);
            }
        }

        internal static void TryAssignIfMissing(staff._staff staffer)
        {
            if (!IsEditableStaff(staffer) || HasAnyPortraitSlots(staffer))
            {
                return;
            }

            IList<StaffPortraitPack> packs = StaffPortraitCatalog.AllPacks;
            if (packs.Count == 0)
            {
                return;
            }

            List<StaffPortraitPack> usablePacks = new List<StaffPortraitPack>();
            for (int packIndex = 0; packIndex < packs.Count; packIndex++)
            {
                if (packs[packIndex] != null && packs[packIndex].HasRequiredAssets)
                {
                    usablePacks.Add(packs[packIndex]);
                }
            }
            if (usablePacks.Count == 0)
            {
                return;
            }

            StaffPortraitPack pack = usablePacks[UnityEngine.Random.Range(0, usablePacks.Count)];
            StaffPortraitSelection selection = CreateRandomSelection(pack);
            Apply(staffer, selection);
        }

        internal static StaffPortraitSelection CreateRandomSelection(StaffPortraitPack pack)
        {
            StaffPortraitSelection selection = new StaffPortraitSelection();
            selection.Pack = pack;
            if (pack == null)
            {
                return selection;
            }

            selection.BodyIndex = RandomIndex(pack.BodyAssets.Count);
            selection.HairIndex = RandomIndex(pack.HairAssets.Count);
            selection.FaceIndex = RandomIndex(pack.FaceAssets.Count);
            selection.AccessoryIndex = pack.AccessoryAssets.Count == 0
                || UnityEngine.Random.Range(0, pack.AccessoryAssets.Count + 1) == pack.AccessoryAssets.Count
                    ? StaffPortraitsConstants.AccessoryNoneSelectionIndex
                    : RandomIndex(pack.AccessoryAssets.Count);
            return selection;
        }

        internal static bool Apply(staff._staff staffer, StaffPortraitSelection selection)
        {
            if (!IsEditableStaff(staffer)
                || selection == null
                || selection.Pack == null
                || !selection.Pack.HasRequiredAssets)
            {
                return false;
            }

            StaffPortraitAsset body = GetSelectedAsset(selection.Pack.BodyAssets, selection.BodyIndex);
            StaffPortraitAsset hair = GetSelectedAsset(selection.Pack.HairAssets, selection.HairIndex);
            StaffPortraitAsset face = GetSelectedAsset(selection.Pack.FaceAssets, selection.FaceIndex);
            StaffPortraitAsset accessory = GetSelectedAsset(selection.Pack.AccessoryAssets, selection.AccessoryIndex);
            if (body == null || hair == null || face == null)
            {
                return false;
            }

            List<data_girls.girls._textureAsset> selectedAssets = new List<data_girls.girls._textureAsset>();
            selectedAssets.Add(Wrap(body));
            selectedAssets.Add(Wrap(hair));
            selectedAssets.Add(Wrap(face));
            if (accessory != null)
            {
                selectedAssets.Add(Wrap(accessory));
            }

            staffer.textureAssets = selectedAssets;
            RefreshStaffTextureData(staffer);
            return true;
        }

        internal static StaffPortraitSelection CreateSelectionFromCurrentPortrait(
            staff._staff staffer,
            IList<StaffPortraitPack> candidatePacks,
            StaffPortraitPack fallbackPack)
        {
            StaffPortraitSelection fallback = new StaffPortraitSelection();
            fallback.Pack = fallbackPack;
            fallback.BodyIndex = StaffPortraitsConstants.FirstIndex;
            fallback.HairIndex = StaffPortraitsConstants.FirstIndex;
            fallback.FaceIndex = StaffPortraitsConstants.FirstIndex;
            fallback.AccessoryIndex = StaffPortraitsConstants.AccessoryNoneSelectionIndex;

            if (staffer == null
                || staffer.textureAssets == null
                || candidatePacks == null
                || fallbackPack == null)
            {
                return fallback;
            }

            for (int textureIndex = 0; textureIndex < staffer.textureAssets.Count; textureIndex++)
            {
                data_girls.girls._textureAsset wrapper = staffer.textureAssets[textureIndex];
                if (wrapper == null || wrapper.asset == null)
                {
                    continue;
                }

                string stableId;
                if (!TryGetManagedStableId(wrapper.asset, out stableId))
                {
                    continue;
                }

                StaffPortraitSelection matchingSelection;
                if (TryBuildSelectionForCurrentAssets(staffer, candidatePacks, stableId, out matchingSelection))
                {
                    return matchingSelection;
                }
            }

            return fallback;
        }

        private static bool TryBuildSelectionForCurrentAssets(
            staff._staff staffer,
            IList<StaffPortraitPack> packs,
            string anyStableId,
            out StaffPortraitSelection selection)
        {
            selection = null;
            if (packs == null)
            {
                return false;
            }
            for (int packIndex = 0; packIndex < packs.Count; packIndex++)
            {
                StaffPortraitPack pack = packs[packIndex];
                if (pack == null || !ContainsStableId(pack, anyStableId))
                {
                    continue;
                }

                StaffPortraitSelection current = new StaffPortraitSelection();
                current.Pack = pack;
                current.BodyIndex = FindCurrentAssetIndex(staffer, pack.BodyAssets, data_girls_textures._spriteType.body);
                current.HairIndex = FindCurrentAssetIndex(staffer, pack.HairAssets, data_girls_textures._spriteType.hair);
                current.FaceIndex = FindCurrentAssetIndex(staffer, pack.FaceAssets, data_girls_textures._spriteType.face);
                current.AccessoryIndex = FindCurrentAssetIndex(staffer, pack.AccessoryAssets, data_girls_textures._spriteType.acc);
                if (current.BodyIndex >= 0 && current.HairIndex >= 0 && current.FaceIndex >= 0)
                {
                    selection = current;
                    return true;
                }
            }
            return false;
        }

        private static bool ContainsStableId(StaffPortraitPack pack, string stableId)
        {
            return FindAssetIndexByStableId(pack.BodyAssets, stableId) >= 0
                || FindAssetIndexByStableId(pack.HairAssets, stableId) >= 0
                || FindAssetIndexByStableId(pack.FaceAssets, stableId) >= 0
                || FindAssetIndexByStableId(pack.AccessoryAssets, stableId) >= 0;
        }

        private static int FindCurrentAssetIndex(
            staff._staff staffer,
            List<StaffPortraitAsset> assets,
            data_girls_textures._spriteType type)
        {
            if (staffer.textureAssets == null)
            {
                return StaffPortraitsConstants.NoSelectionIndex;
            }

            for (int textureIndex = 0; textureIndex < staffer.textureAssets.Count; textureIndex++)
            {
                data_girls.girls._textureAsset wrapper = staffer.textureAssets[textureIndex];
                if (wrapper == null || wrapper.type != type || wrapper.asset == null)
                {
                    continue;
                }

                for (int assetIndex = 0; assetIndex < assets.Count; assetIndex++)
                {
                    StaffPortraitAsset candidate = assets[assetIndex];
                    if (candidate != null && ReferenceEquals(candidate.GameAsset, wrapper.asset))
                    {
                        return assetIndex;
                    }
                }

                string stableId;
                if (!TryGetManagedStableId(wrapper.asset, out stableId))
                {
                    return StaffPortraitsConstants.NoSelectionIndex;
                }
                return FindAssetIndexByStableId(assets, stableId);
            }

            return type == data_girls_textures._spriteType.acc
                ? StaffPortraitsConstants.AccessoryNoneSelectionIndex
                : StaffPortraitsConstants.NoSelectionIndex;
        }

        private static int FindAssetIndexByStableId(List<StaffPortraitAsset> assets, string stableId)
        {
            for (int assetIndex = 0; assetIndex < assets.Count; assetIndex++)
            {
                if (assets[assetIndex] != null && string.Equals(assets[assetIndex].StableId, stableId, StringComparison.Ordinal))
                {
                    return assetIndex;
                }
            }
            return StaffPortraitsConstants.NoSelectionIndex;
        }

        private static bool TryGetManagedStableId(
            data_girls_textures._textureAsset runtimeAsset,
            out string stableId)
        {
            if (StaffPortraitCatalog.TryGetStableId(runtimeAsset, out stableId))
            {
                return true;
            }

            return UniqueIdolPortraitCatalog.TryGetStableId(runtimeAsset, out stableId);
        }

        private static bool HasAnyPortraitSlots(staff._staff staffer)
        {
            return staffer != null
                && staffer.textureAssets != null
                && staffer.textureAssets.Count > 0;
        }

        private static data_girls.girls._textureAsset Wrap(StaffPortraitAsset asset)
        {
            return new data_girls.girls._textureAsset
            {
                type = asset.Type,
                asset = asset.GameAsset
            };
        }

        private static StaffPortraitAsset GetSelectedAsset(List<StaffPortraitAsset> assets, int index)
        {
            return assets != null && index >= 0 && index < assets.Count ? assets[index] : null;
        }

        private static int RandomIndex(int count)
        {
            return count <= 0 ? StaffPortraitsConstants.NoSelectionIndex : UnityEngine.Random.Range(0, count);
        }

        private static void RefreshStaffTextureData(staff._staff staffer)
        {
            try
            {
                MethodInfo refreshMethod = AccessTools.Method(staffer.GetType(), StaffPortraitsConstants.StaffTextureRefreshMethodName);
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(staffer, null);
                    // UpdateTextureData replaces IDs only. As in vanilla staff loading,
                    // queue the composite so picker thumbnails and staff cards receive sprites.
                    data_girls.girls._texture renderedTexture = staffer.texture;
                    data_girls.girls portraitRequest = new data_girls.girls
                    {
                        texture = renderedTexture,
                        textureAssets = staffer.textureAssets
                    };
                    portraitRequest.TexturesUpdate += delegate
                    {
                        // A later Apply can replace the texture before this request completes.
                        if (ReferenceEquals(staffer.texture, renderedTexture) && staffer.UpdatePortrait != null)
                            staffer.UpdatePortrait();
                    };
                    data_girls_textures.AddToQueue(portraitRequest);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Staff portrait data was assigned, but the live texture refresh could not run: " + exception.Message);
            }
        }
    }
}
