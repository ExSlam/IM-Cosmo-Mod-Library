using System.Collections.Generic;

namespace StaffPortraits
{
    internal enum StaffPortraitPackSource
    {
        Staff = 0,
        UniqueIdol = 1
    }

    internal sealed class StaffPortraitPack
    {
        internal string PackId;
        internal string DisplayName;
        internal string Author;
        internal string RootDirectory;
        internal readonly List<StaffPortraitAsset> BodyAssets = new List<StaffPortraitAsset>();
        internal readonly List<StaffPortraitAsset> HairAssets = new List<StaffPortraitAsset>();
        internal readonly List<StaffPortraitAsset> FaceAssets = new List<StaffPortraitAsset>();
        internal readonly List<StaffPortraitAsset> AccessoryAssets = new List<StaffPortraitAsset>();

        internal bool HasRequiredAssets
        {
            get
            {
                return BodyAssets.Count > 0 && HairAssets.Count > 0 && FaceAssets.Count > 0;
            }
        }
    }

    internal sealed class StaffPortraitAsset
    {
        internal string AssetId;
        internal string DisplayName;
        internal string StableId;
        internal string FullPath;
        internal data_girls_textures._spriteType Type;
        internal data_girls_textures._textureAsset GameAsset;
    }

    internal sealed class StaffPortraitSelection
    {
        internal StaffPortraitPack Pack;
        internal int BodyIndex;
        internal int HairIndex;
        internal int FaceIndex;
        internal int AccessoryIndex = StaffPortraitsConstants.AccessoryNoneSelectionIndex;
    }
}
