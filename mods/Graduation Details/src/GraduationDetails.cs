using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDetails
{
    internal enum CustodyOwner
    {
        Unknown = 0,
        Idol = 1,
        Player = 2,
        None = 3
    }

    [Serializable]
    internal sealed class MarriageRecord
    {
        public int GirlId = -1;
        public bool MarriedToPlayer;
        public string PlayerName = "";
        public int KidsCount = -1;
        public CustodyOwner Custody = CustodyOwner.Unknown;
    }

    [Serializable]
    internal sealed class MarriageRecordList
    {
        public List<MarriageRecord> Records = new List<MarriageRecord>();
    }

    internal static class GraduationDetailsPaths
    {
        private const string BaseFolder = "GraduationDetails";

        internal static string RootDir
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "Mods", BaseFolder);
            }
        }

        internal static string SavesRootDir
        {
            get
            {
                return Path.Combine(RootDir, "saves");
            }
        }

        internal static string GetSaveKey()
        {
            try
            {
                if (staticVars.PlayerData == null)
                {
                    return "default";
                }

                string folder = staticVars.PlayerData.SaveFolderName;
                if (string.IsNullOrEmpty(folder) && staticVars.PlayerData.IsStoryMode)
                {
                    folder = staticVars.PlayerData.GetSaveFolderName();
                }
                if (!string.IsNullOrEmpty(folder))
                {
                    string fromFolder = SanitizeFileToken(folder);
                    if (!string.IsNullOrEmpty(fromFolder))
                    {
                        return fromFolder;
                    }
                }

                string fallback = string.Join("_", new string[]
                {
                    staticVars.PlayerData.IsStoryMode ? "story" : "freeplay",
                    staticVars.PlayerData.FirstName ?? "",
                    staticVars.PlayerData.LastName ?? "",
                    staticVars.PlayerData.GroupName ?? "",
                    staticVars.PlayerData.Chapter.ToString()
                });
                string token = SanitizeFileToken(fallback);
                if (!string.IsNullOrEmpty(token))
                {
                    return token;
                }
            }
            catch
            {
            }
            return "default";
        }

        internal static string GetSaveDir()
        {
            return Path.Combine(SavesRootDir, GetSaveKey());
        }

        internal static string GetScopedFilePath(string fileName)
        {
            return Path.Combine(GetSaveDir(), fileName);
        }

        internal static string GetLegacyFilePath(string fileName)
        {
            return Path.Combine(RootDir, fileName);
        }

        internal static string GetScopedPortraitDir()
        {
            return Path.Combine(GetSaveDir(), "Portraits");
        }

        internal static string GetLegacyPortraitDir()
        {
            return Path.Combine(RootDir, "Portraits");
        }

        internal static void TryMigrateLegacyFileOnce(string fileName)
        {
            string scoped = GetScopedFilePath(fileName);
            if (File.Exists(scoped))
            {
                return;
            }

            string legacy = GetLegacyFilePath(fileName);
            if (!File.Exists(legacy))
            {
                return;
            }

            try
            {
                if (Directory.Exists(SavesRootDir))
                {
                    string[] dirs = Directory.GetDirectories(SavesRootDir);
                    if (dirs != null && dirs.Length > 0)
                    {
                        return;
                    }
                }

                string dir = Path.GetDirectoryName(scoped);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(legacy, scoped, false);
            }
            catch
            {
            }
        }

        private static string SanitizeFileToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            HashSet<char> invalidSet = new HashSet<char>(invalid);
            char[] chars = new char[value.Length];
            int count = 0;

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                {
                    chars[count++] = c;
                    continue;
                }
                if (!char.IsControl(c) && !invalidSet.Contains(c))
                {
                    chars[count++] = '_';
                }
            }

            if (count == 0)
            {
                return "";
            }

            string token = new string(chars, 0, count).Trim('_');
            while (token.Contains("__"))
            {
                token = token.Replace("__", "_");
            }
            if (token.Length > 64)
            {
                token = token.Substring(0, 64);
            }
            return token;
        }
    }

    internal static class MarriageRecordStore
    {
        private static readonly Dictionary<int, MarriageRecord> Records = new Dictionary<int, MarriageRecord>();
        private static bool loaded;
        private static string loadedScope = "";

        private static string DataPath
        {
            get
            {
                return GraduationDetailsPaths.GetScopedFilePath("marriage_data.json");
            }
        }

        private static string LegacyDataPath
        {
            get
            {
                return GraduationDetailsPaths.GetLegacyFilePath("marriage_data.json");
            }
        }

        internal static MarriageRecord GetRecord(int girlId)
        {
            EnsureLoaded();
            MarriageRecord record;
            if (Records.TryGetValue(girlId, out record))
            {
                return record;
            }
            return null;
        }

        internal static void Upsert(MarriageRecord record)
        {
            if (record == null || record.GirlId < 0)
            {
                return;
            }
            EnsureLoaded();
            Records[record.GirlId] = record;
            Save();
        }

        private static void EnsureLoaded()
        {
            string scope = GraduationDetailsPaths.GetSaveKey();
            if (loaded && loadedScope == scope)
            {
                return;
            }
            loadedScope = scope;
            loaded = true;
            Records.Clear();
            try
            {
                GraduationDetailsPaths.TryMigrateLegacyFileOnce("marriage_data.json");
                if (!File.Exists(DataPath))
                {
                    // Legacy fallback for users with old installs and no scoped files yet.
                    if (!File.Exists(LegacyDataPath))
                    {
                        return;
                    }
                }
                string loadPath = File.Exists(DataPath) ? DataPath : LegacyDataPath;
                if (!File.Exists(loadPath))
                {
                    return;
                }
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                MarriageRecordList list = JsonUtility.FromJson<MarriageRecordList>(json);
                if (list == null || list.Records == null)
                {
                    return;
                }
                Records.Clear();
                foreach (MarriageRecord record in list.Records)
                {
                    if (record == null || record.GirlId < 0)
                    {
                        continue;
                    }
                    Records[record.GirlId] = record;
                }
            }
            catch
            {
                Records.Clear();
            }
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(DataPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                MarriageRecordList list = new MarriageRecordList();
                list.Records = Records.Values.ToList();
                string json = JsonUtility.ToJson(list, true);
                File.WriteAllText(DataPath, json);
            }
            catch
            {
                // Ignore persistence errors to avoid breaking the game loop.
            }
        }
    }

    internal static class MarriageContext
    {
        internal static bool Active;
        internal static int GirlId = -1;
        internal static string PlayerName = "";
        internal static int KidsCount = -1;
        internal static CustodyOwner Custody = CustodyOwner.Unknown;

        internal static void Begin(data_girls.girls girl)
        {
            Reset();
            if (girl == null)
            {
                return;
            }
            Active = true;
            GirlId = girl.id;
            PlayerName = staticVars.PlayerData.GetPlayerName(staticVars._playerData.name_type.full_name, true);
        }

        internal static void SetKids(int count)
        {
            if (!Active)
            {
                return;
            }
            KidsCount = count;
            if (count <= 0)
            {
                Custody = CustodyOwner.None;
            }
        }

        internal static void SetCustody(string custodyString, bool goodOutcome, int numberOfKids)
        {
            if (!Active)
            {
                return;
            }
            if (KidsCount < 0)
            {
                KidsCount = numberOfKids;
            }
            if (goodOutcome || numberOfKids <= 0)
            {
                if (Custody == CustodyOwner.Unknown)
                {
                    Custody = CustodyOwner.None;
                }
                return;
            }
            if (string.IsNullOrEmpty(custodyString))
            {
                return;
            }
            if (custodyString.Contains("[g:casual]"))
            {
                Custody = CustodyOwner.Idol;
                return;
            }
            string youText = Language.Data["NOTIF__IDOL_REL_YOU"];
            if (!string.IsNullOrEmpty(youText) && custodyString.Contains(youText))
            {
                Custody = CustodyOwner.Player;
            }
        }

        internal static void SaveAndClear()
        {
            if (!Active || GirlId < 0)
            {
                Reset();
                return;
            }
            MarriageRecord record = new MarriageRecord
            {
                GirlId = GirlId,
                MarriedToPlayer = true,
                PlayerName = PlayerName ?? "",
                KidsCount = KidsCount,
                Custody = Custody
            };
            MarriageRecordStore.Upsert(record);
            Reset();
        }

        private static void Reset()
        {
            Active = false;
            GirlId = -1;
            PlayerName = "";
            KidsCount = -1;
            Custody = CustodyOwner.Unknown;
        }
    }

    [Serializable]
    internal sealed class StaffIdolRecord
    {
        public int StaffId = -1;
        public int GirlId = -1;
        public bool CapturedAtHire;
    }

    [Serializable]
    internal sealed class StaffIdolRecordList
    {
        public List<StaffIdolRecord> Records = new List<StaffIdolRecord>();
    }

    internal static class StaffIdolStore
    {
        private static readonly Dictionary<int, StaffIdolRecord> StaffToGirl = new Dictionary<int, StaffIdolRecord>();
        private static bool loaded;
        private static string loadedScope = "";

        private static string DataPath
        {
            get
            {
                return GraduationDetailsPaths.GetScopedFilePath("staff_idol_map.json");
            }
        }

        private static string LegacyDataPath
        {
            get
            {
                return GraduationDetailsPaths.GetLegacyFilePath("staff_idol_map.json");
            }
        }

        internal static bool TryGetRecord(int staffId, out StaffIdolRecord record)
        {
            EnsureLoaded();
            return StaffToGirl.TryGetValue(staffId, out record);
        }

        internal static bool TryGetGirlId(int staffId, out int girlId)
        {
            StaffIdolRecord record;
            if (TryGetRecord(staffId, out record))
            {
                girlId = record.GirlId;
                return true;
            }
            girlId = -1;
            return false;
        }

        internal static bool IsFormerIdolStaff(staff._staff staffer)
        {
            if (staffer == null || !staffer.IsIdol())
            {
                return false;
            }
            return staffer.UniqueType == staff._staff._unique_type.NONE;
        }

        internal static bool TryResolveStaffer(staff._staff staffer, out int girlId)
        {
            girlId = -1;
            if (staffer == null)
            {
                return false;
            }
            StaffIdolRecord record;
            if (TryGetRecord(staffer.id, out record))
            {
                girlId = record.GirlId;
                if (record.CapturedAtHire)
                {
                    return true;
                }
                data_girls.girls mapped = data_girls.GetGirlByID(girlId);
                if (IsLikelyMatch(staffer, mapped))
                {
                    return true;
                }
                StaffToGirl.Remove(staffer.id);
                Save();
                girlId = -1;
            }
            if (!IsFormerIdolStaff(staffer))
            {
                return false;
            }
            girlId = TryResolveGirlId(staffer);
            if (girlId < 0)
            {
                return false;
            }
            Upsert(staffer.id, girlId);
            return true;
        }

        internal static void Upsert(int staffId, int girlId)
        {
            Upsert(staffId, girlId, false);
        }

        internal static void Upsert(int staffId, int girlId, bool capturedAtHire)
        {
            if (staffId < 0 || girlId < 0)
            {
                return;
            }
            EnsureLoaded();
            StaffIdolRecord record;
            if (!StaffToGirl.TryGetValue(staffId, out record) || record == null)
            {
                record = new StaffIdolRecord
                {
                    StaffId = staffId,
                    GirlId = girlId,
                    CapturedAtHire = capturedAtHire
                };
            }
            else
            {
                record.GirlId = girlId;
                if (capturedAtHire)
                {
                    record.CapturedAtHire = true;
                }
            }
            StaffToGirl[staffId] = record;
            Save();
        }

        internal static void BackfillFromStaff()
        {
            EnsureLoaded();
            if (staff.Staff == null)
            {
                return;
            }
            foreach (staff._staff staffer in staff.Staff)
            {
                if (staffer == null)
                {
                    continue;
                }
                int girlId;
                if (!TryResolveStaffer(staffer, out girlId))
                {
                    continue;
                }
                data_girls.girls girl = data_girls.GetGirlByID(girlId);
                if (girl != null && GraduationSnapshotStore.GetSnapshot(girlId) == null)
                {
                    GraduationSnapshotStore.Capture(girl);
                }
            }
            Save();
        }

        private static int TryResolveGirlId(staff._staff staffer)
        {
            if (staffer == null || !staffer.IsIdol())
            {
                return -1;
            }
            if (data_girls.girl == null)
            {
                return -1;
            }
            int resolved = FindMatchByName(staffer, true);
            if (resolved >= 0)
            {
                return resolved;
            }
            resolved = FindMatchByName(staffer, false);
            if (resolved >= 0)
            {
                return resolved;
            }
            resolved = FindMatch(staffer, true);
            if (resolved >= 0)
            {
                return resolved;
            }
            return FindMatch(staffer, false);
        }

        private static int FindMatchByName(staff._staff staffer, bool requireGraduated)
        {
            if (staffer == null || data_girls.girl == null)
            {
                return -1;
            }
            List<data_girls.girls> matches = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl == null)
                {
                    continue;
                }
                if (requireGraduated && girl.status != data_girls._status.graduated)
                {
                    continue;
                }
                if (NamesMatch(staffer, girl))
                {
                    matches.Add(girl);
                }
            }
            if (matches.Count == 1)
            {
                return matches[0].id;
            }
            if (matches.Count > 1)
            {
                data_girls.girls textureMatch = null;
                foreach (data_girls.girls match in matches)
                {
                    if (TextureAssetsMatch(staffer.textureAssets, match.textureAssets))
                    {
                        if (textureMatch != null)
                        {
                            return -1;
                        }
                        textureMatch = match;
                    }
                }
                if (textureMatch != null)
                {
                    return textureMatch.id;
                }
            }
            return -1;
        }

        private static int FindMatch(staff._staff staffer, bool requireGraduated)
        {
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl == null)
                {
                    continue;
                }
                if (requireGraduated && girl.status != data_girls._status.graduated)
                {
                    continue;
                }
                if (TextureAssetsMatch(staffer.textureAssets, girl.textureAssets))
                {
                    return girl.id;
                }
            }
            return -1;
        }

        private static bool IsLikelyMatch(staff._staff staffer, data_girls.girls girl)
        {
            if (staffer == null || girl == null)
            {
                return false;
            }
            if (NamesMatch(staffer, girl))
            {
                return true;
            }
            bool missingNames = string.IsNullOrEmpty(staffer.firstName) || string.IsNullOrEmpty(staffer.lastName)
                || string.IsNullOrEmpty(girl.firstName) || string.IsNullOrEmpty(girl.lastName);
            if (missingNames)
            {
                return TextureAssetsMatch(staffer.textureAssets, girl.textureAssets);
            }
            return false;
        }

        private static bool NamesMatch(staff._staff staffer, data_girls.girls girl)
        {
            if (staffer == null || girl == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(staffer.firstName) && !string.IsNullOrEmpty(staffer.lastName)
                && !string.IsNullOrEmpty(girl.firstName) && !string.IsNullOrEmpty(girl.lastName))
            {
                if (string.Equals(staffer.firstName, girl.firstName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(staffer.lastName, girl.lastName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            if (!string.IsNullOrEmpty(staffer.nickname) && !string.IsNullOrEmpty(girl.nickname))
            {
                if (string.Equals(staffer.nickname, girl.nickname, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TextureAssetsMatch(List<data_girls.girls._textureAsset> staffAssets, List<data_girls.girls._textureAsset> girlAssets)
        {
            if (staffAssets == null || girlAssets == null)
            {
                return false;
            }
            if (staffAssets.Count == 0 || girlAssets.Count == 0)
            {
                return false;
            }
            HashSet<string> staffIds = new HashSet<string>();
            foreach (data_girls.girls._textureAsset asset in staffAssets)
            {
                if (asset == null || asset.asset == null)
                {
                    continue;
                }
                staffIds.Add(asset.asset.GetID());
            }
            if (staffIds.Count == 0)
            {
                return false;
            }
            foreach (data_girls.girls._textureAsset asset in girlAssets)
            {
                if (asset == null || asset.asset == null)
                {
                    return false;
                }
                if (!staffIds.Contains(asset.asset.GetID()))
                {
                    return false;
                }
            }
            return true;
        }

        private static void EnsureLoaded()
        {
            string scope = GraduationDetailsPaths.GetSaveKey();
            if (loaded && loadedScope == scope)
            {
                return;
            }
            loadedScope = scope;
            loaded = true;
            StaffToGirl.Clear();
            try
            {
                GraduationDetailsPaths.TryMigrateLegacyFileOnce("staff_idol_map.json");
                if (!File.Exists(DataPath))
                {
                    if (!File.Exists(LegacyDataPath))
                    {
                        return;
                    }
                }
                string loadPath = File.Exists(DataPath) ? DataPath : LegacyDataPath;
                if (!File.Exists(loadPath))
                {
                    return;
                }
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                StaffIdolRecordList list = JsonUtility.FromJson<StaffIdolRecordList>(json);
                if (list == null || list.Records == null)
                {
                    return;
                }
                StaffToGirl.Clear();
                foreach (StaffIdolRecord record in list.Records)
                {
                    if (record == null || record.StaffId < 0 || record.GirlId < 0)
                    {
                        continue;
                    }
                    StaffToGirl[record.StaffId] = record;
                }
            }
            catch
            {
                StaffToGirl.Clear();
            }
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(DataPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                StaffIdolRecordList list = new StaffIdolRecordList();
                foreach (KeyValuePair<int, StaffIdolRecord> entry in StaffToGirl)
                {
                    if (entry.Value != null)
                    {
                        list.Records.Add(entry.Value);
                    }
                }
                string json = JsonUtility.ToJson(list, true);
                File.WriteAllText(DataPath, json);
            }
            catch
            {
                // Ignore persistence errors to avoid breaking the game loop.
            }
        }
    }

    internal static class StaffHireContext
    {
        internal static bool Active;
        internal static int GirlId = -1;

        internal static void Begin(data_girls.girls girl)
        {
            Active = girl != null;
            GirlId = girl != null ? girl.id : -1;
        }

        internal static void Complete(staff._staff staffer)
        {
            if (staffer != null && GirlId >= 0)
            {
                StaffIdolStore.Upsert(staffer.id, GirlId, true);
            }
            Clear();
        }

        internal static void Clear()
        {
            Active = false;
            GirlId = -1;
        }
    }

    [Serializable]
    internal sealed class GraduationSnapshot
    {
        public int GirlId = -1;
        public string Birthdate = "";
        public int AgeAtGraduation = -1;
        public string PortraitFile = "";
        public List<FanSnapshot> Fans = new List<FanSnapshot>();
        public List<BondSectionSnapshot> Bonds = new List<BondSectionSnapshot>();
    }

    [Serializable]
    internal sealed class GraduationSnapshotList
    {
        public List<GraduationSnapshot> Records = new List<GraduationSnapshot>();
    }

    [Serializable]
    internal sealed class FanSnapshot
    {
        public resources.fanType Gender;
        public resources.fanType Hardcoreness;
        public resources.fanType Age;
        public long People;
        public float Appeal;
        public float Opinion;
    }

    internal enum BondSectionType
    {
        CliqueKnown = 0,
        CliqueUnknown = 1,
        Bullies = 2,
        BulliedBy = 3,
        BestFriends = 4,
        Friends = 5,
        Dislikes = 6,
        Hates = 7,
        NoInfo = 8
    }

    [Serializable]
    internal sealed class BondEntry
    {
        public int GirlId = -1;
        public bool Known;
        public float RelationshipRatio = 0.5f;
        public bool IsDatingKnown;
    }

    [Serializable]
    internal sealed class BondSectionSnapshot
    {
        public BondSectionType Type;
        public int LeaderId = -1;
        public List<BondEntry> Entries = new List<BondEntry>();
    }

    internal static class GraduationSnapshotStore
    {
        private static readonly Dictionary<int, GraduationSnapshot> Records = new Dictionary<int, GraduationSnapshot>();
        private static bool loaded;
        private static string loadedScope = "";

        private static string DataPath
        {
            get
            {
                return GraduationDetailsPaths.GetScopedFilePath("graduation_snapshots.json");
            }
        }

        private static string LegacyDataPath
        {
            get
            {
                return GraduationDetailsPaths.GetLegacyFilePath("graduation_snapshots.json");
            }
        }

        private static string PortraitDir
        {
            get
            {
                return GraduationDetailsPaths.GetScopedPortraitDir();
            }
        }

        private static string LegacyPortraitDir
        {
            get
            {
                return GraduationDetailsPaths.GetLegacyPortraitDir();
            }
        }

        internal static GraduationSnapshot GetSnapshot(int girlId)
        {
            EnsureLoaded();
            GraduationSnapshot record;
            if (Records.TryGetValue(girlId, out record))
            {
                return record;
            }
            return null;
        }

        internal static void Capture(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            GraduationSnapshot existing = GetSnapshot(girl.id);
            GraduationSnapshot snapshot = existing;
            if (snapshot == null)
            {
                snapshot = new GraduationSnapshot
                {
                    GirlId = girl.id,
                    PortraitFile = girl.id + ".png"
                };
            }

            // Keep existing metadata if already captured; this avoids drifting "at graduation" data later.
            if (string.IsNullOrEmpty(snapshot.Birthdate))
            {
                snapshot.Birthdate = ExtensionMethods.ToDataString(girl.birthday);
            }
            if (snapshot.AgeAtGraduation <= 0)
            {
                snapshot.AgeAtGraduation = girl.GetAge();
            }

            // Build current snapshots.
            List<FanSnapshot> candidateFans = BuildFanSnapshot(girl);
            List<BondSectionSnapshot> candidateBonds = BuildBondSnapshot(girl);

            // Preserve the best available fan data.
            // Staff-hire flow can occur after fan buckets are cleared/reset, so avoid overwriting a stronger snapshot.
            if (ShouldReplaceFans(existing, candidateFans))
            {
                snapshot.Fans = candidateFans;
            }
            else if (snapshot.Fans == null)
            {
                snapshot.Fans = new List<FanSnapshot>();
            }

            // Preserve meaningful bond data similarly.
            if (ShouldReplaceBonds(existing, candidateBonds))
            {
                snapshot.Bonds = candidateBonds;
            }
            else if (snapshot.Bonds == null)
            {
                snapshot.Bonds = new List<BondSectionSnapshot>();
            }

            Upsert(snapshot);
            TryCapturePortrait(girl, snapshot);
        }

        internal static void Backfill(List<data_girls.girls> girls)
        {
            if (girls == null)
            {
                return;
            }
            foreach (data_girls.girls girl in girls)
            {
                if (girl == null || girl.status != data_girls._status.graduated)
                {
                    continue;
                }
                if (GetSnapshot(girl.id) != null)
                {
                    continue;
                }
                Capture(girl);
            }
        }

        internal static string GetPortraitPath(GraduationSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.PortraitFile))
            {
                return "";
            }
            string scopedPath = Path.Combine(PortraitDir, snapshot.PortraitFile);
            if (File.Exists(scopedPath))
            {
                return scopedPath;
            }
            string legacyPath = Path.Combine(LegacyPortraitDir, snapshot.PortraitFile);
            if (File.Exists(legacyPath))
            {
                return legacyPath;
            }
            return scopedPath;
        }

        internal static long GetTotalFans(GraduationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Fans == null)
            {
                return 0L;
            }
            long total = 0L;
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan != null)
                {
                    total += fan.People;
                }
            }
            return total;
        }

        internal static long GetFanCount(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            FanSnapshot fan = GetFanSnapshot(snapshot, gender, hardcoreness, age);
            return fan != null ? fan.People : 0L;
        }

        internal static long GetFanCount(GraduationSnapshot snapshot, resources.fanType type)
        {
            if (snapshot == null || snapshot.Fans == null)
            {
                return 0L;
            }
            long total = 0L;
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan == null)
                {
                    continue;
                }
                if (fan.Gender == type || fan.Hardcoreness == type || fan.Age == type)
                {
                    total += fan.People;
                }
            }
            return total;
        }

        internal static float GetFanRatio(GraduationSnapshot snapshot, resources.fanType type)
        {
            long total = GetTotalFans(snapshot);
            if (total == 0L)
            {
                return 1f;
            }
            long count = GetFanCount(snapshot, type);
            return (float)count / (float)total;
        }

        internal static float GetFanAppeal(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            FanSnapshot fan = GetFanSnapshot(snapshot, gender, hardcoreness, age);
            return fan != null ? fan.Appeal : 0f;
        }

        internal static float GetFanOpinion(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            FanSnapshot fan = GetFanSnapshot(snapshot, gender, hardcoreness, age);
            return fan != null ? fan.Opinion : 0.5f;
        }

        internal static bool HasFanSnapshot(GraduationSnapshot snapshot)
        {
            return snapshot != null && snapshot.Fans != null && snapshot.Fans.Count > 0;
        }

        internal static bool HasBondSnapshot(GraduationSnapshot snapshot)
        {
            return snapshot != null && snapshot.Bonds != null && snapshot.Bonds.Count > 0;
        }

        private static FanSnapshot GetFanSnapshot(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            if (snapshot == null || snapshot.Fans == null)
            {
                return null;
            }
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan == null)
                {
                    continue;
                }
                if (fan.Gender == gender && fan.Hardcoreness == hardcoreness && fan.Age == age)
                {
                    return fan;
                }
            }
            return null;
        }

        private static bool ShouldReplaceFans(GraduationSnapshot existing, List<FanSnapshot> candidateFans)
        {
            if (candidateFans == null)
            {
                return existing == null || existing.Fans == null;
            }

            long candidateTotal = GetTotalFansFromList(candidateFans);
            if (existing == null || existing.Fans == null || existing.Fans.Count == 0)
            {
                return candidateFans.Count > 0;
            }

            long existingTotal = GetTotalFans(existing);
            if (existingTotal <= 0L)
            {
                return candidateFans.Count > 0;
            }

            // Never overwrite real fan history with empty/zero snapshots captured later.
            if (candidateTotal <= 0L)
            {
                return false;
            }

            // Prefer the richer snapshot when both are meaningful.
            return candidateTotal >= existingTotal;
        }

        private static long GetTotalFansFromList(List<FanSnapshot> fans)
        {
            if (fans == null)
            {
                return 0L;
            }
            long total = 0L;
            foreach (FanSnapshot fan in fans)
            {
                if (fan != null)
                {
                    total += fan.People;
                }
            }
            return total;
        }

        private static bool ShouldReplaceBonds(GraduationSnapshot existing, List<BondSectionSnapshot> candidateBonds)
        {
            if (candidateBonds == null)
            {
                return existing == null || existing.Bonds == null;
            }

            if (existing == null || existing.Bonds == null || existing.Bonds.Count == 0)
            {
                return candidateBonds.Count > 0;
            }

            bool existingMeaningful = HasMeaningfulBonds(existing.Bonds);
            bool candidateMeaningful = HasMeaningfulBonds(candidateBonds);
            if (!existingMeaningful)
            {
                return candidateBonds.Count > 0;
            }
            if (!candidateMeaningful)
            {
                return false;
            }
            return candidateBonds.Count >= existing.Bonds.Count;
        }

        private static bool HasMeaningfulBonds(List<BondSectionSnapshot> bonds)
        {
            if (bonds == null || bonds.Count == 0)
            {
                return false;
            }
            if (bonds.Count == 1)
            {
                BondSectionSnapshot only = bonds[0];
                if (only != null && only.Type == BondSectionType.NoInfo && (only.Entries == null || only.Entries.Count == 0))
                {
                    return false;
                }
            }
            return true;
        }

        private static List<FanSnapshot> BuildFanSnapshot(data_girls.girls girl)
        {
            List<FanSnapshot> list = new List<FanSnapshot>();
            if (girl == null || girl.Fans == null)
            {
                return list;
            }
            foreach (resources._fan fan in girl.Fans)
            {
                if (fan == null)
                {
                    continue;
                }
                FanSnapshot snapshot = new FanSnapshot
                {
                    Gender = fan.gender,
                    Hardcoreness = fan.hardcoreness,
                    Age = fan.age,
                    People = fan.people,
                    Appeal = fan.appeal,
                    Opinion = fan.Ratio
                };
                list.Add(snapshot);
            }
            return list;
        }

        private static List<BondSectionSnapshot> BuildBondSnapshot(data_girls.girls girl)
        {
            List<BondSectionSnapshot> sections = new List<BondSectionSnapshot>();
            if (girl == null)
            {
                return sections;
            }

            Relationships._clique clique = girl.GetClique();
            if (clique != null)
            {
                AddBondSection(sections,
                    clique.Known ? BondSectionType.CliqueKnown : BondSectionType.CliqueUnknown,
                    BuildEntriesFromGirls(clique.Members, girl, clique.Known, null),
                    clique.Leader != null ? clique.Leader.id : -1);

                if (clique.Bullied_Girls != null && clique.Bullied_Girls.Count > 0)
                {
                    AddBondSection(sections,
                        BondSectionType.Bullies,
                        BuildEntriesFromGirls(clique.Bullied_Girls, girl, false, clique.KnownBulliedGirls),
                        clique.Leader != null ? clique.Leader.id : -1);
                }
            }

            List<Relationships._clique> bullyingCliques = girl.GetCliquesThatBully(false);
            if (bullyingCliques != null)
            {
                foreach (Relationships._clique bullyClique in bullyingCliques)
                {
                    if (bullyClique == null)
                    {
                        continue;
                    }
                    bool targetKnown = bullyClique.KnownBulliedGirls != null && bullyClique.KnownBulliedGirls.Contains(girl);
                    bool known = false;
                    List<data_girls.girls> knownGirls = null;
                    if (targetKnown)
                    {
                        if (bullyClique.Known)
                        {
                            known = true;
                        }
                        else
                        {
                            known = false;
                            knownGirls = new List<data_girls.girls>();
                            if (bullyClique.Leader != null)
                            {
                                knownGirls.Add(bullyClique.Leader);
                            }
                        }
                    }
                    List<BondEntry> entries = BuildEntriesFromGirls(bullyClique.Members, girl, known, knownGirls);
                    AddBondSection(sections, BondSectionType.BulliedBy, entries, bullyClique.Leader != null ? bullyClique.Leader.id : -1);
                }
            }

            List<Relationships._relationship> allRelationships = Relationships.GetAllRelationships(girl, false);
            List<Relationships._relationship> bestFriends = new List<Relationships._relationship>();
            List<Relationships._relationship> friends = new List<Relationships._relationship>();
            List<Relationships._relationship> dislikes = new List<Relationships._relationship>();
            List<Relationships._relationship> hates = new List<Relationships._relationship>();
            foreach (Relationships._relationship relationship in allRelationships)
            {
                if (relationship == null || relationship.Girls == null || relationship.Girls.Count < 2)
                {
                    continue;
                }
                if (relationship.Girls[0] == null || relationship.Girls[1] == null)
                {
                    continue;
                }
                if (relationship.Girls[0].status == data_girls._status.graduated || relationship.Girls[1].status == data_girls._status.graduated)
                {
                    continue;
                }
                if (relationship.Status == Relationships._relationship._status.best_friends)
                {
                    bestFriends.Add(relationship);
                }
                else if (relationship.Status == Relationships._relationship._status.friends)
                {
                    friends.Add(relationship);
                }
                else if (relationship.Status == Relationships._relationship._status.dislikes)
                {
                    dislikes.Add(relationship);
                }
                else if (relationship.Status == Relationships._relationship._status.hates)
                {
                    hates.Add(relationship);
                }
            }

            AddBondSection(sections, BondSectionType.BestFriends, BuildEntriesFromRelationships(bestFriends, girl), -1);
            AddBondSection(sections, BondSectionType.Friends, BuildEntriesFromRelationships(friends, girl), -1);
            AddBondSection(sections, BondSectionType.Dislikes, BuildEntriesFromRelationships(dislikes, girl), -1);
            AddBondSection(sections, BondSectionType.Hates, BuildEntriesFromRelationships(hates, girl), -1);

            if (sections.Count == 0)
            {
                sections.Add(new BondSectionSnapshot
                {
                    Type = BondSectionType.NoInfo
                });
            }
            return sections;
        }

        private static void AddBondSection(List<BondSectionSnapshot> sections, BondSectionType type, List<BondEntry> entries, int leaderId)
        {
            if (sections == null || entries == null || entries.Count == 0)
            {
                return;
            }
            BondSectionSnapshot snapshot = new BondSectionSnapshot
            {
                Type = type,
                LeaderId = leaderId,
                Entries = entries
            };
            sections.Add(snapshot);
        }

        private static List<BondEntry> BuildEntriesFromGirls(List<data_girls.girls> girls, data_girls.girls parent, bool known, List<data_girls.girls> knownGirls)
        {
            List<BondEntry> entries = new List<BondEntry>();
            if (girls == null)
            {
                return entries;
            }
            foreach (data_girls.girls other in girls)
            {
                BondEntry entry = new BondEntry();
                if (other != null)
                {
                    bool isKnown = known || other == parent || (knownGirls != null && knownGirls.Contains(other));
                    entry.GirlId = other.id;
                    entry.Known = isKnown;
                    FillBondEntryStats(entry, parent, other);
                }
                else
                {
                    entry.GirlId = -1;
                    entry.Known = false;
                }
                entries.Add(entry);
            }
            return entries;
        }

        private static List<BondEntry> BuildEntriesFromRelationships(List<Relationships._relationship> relationships, data_girls.girls parent)
        {
            List<BondEntry> entries = new List<BondEntry>();
            if (relationships == null)
            {
                return entries;
            }
            foreach (Relationships._relationship relationship in relationships)
            {
                if (relationship == null || relationship.Girls == null || relationship.Girls.Count < 2)
                {
                    continue;
                }
                bool known = relationship.IsRelationshipKnown();
                if (!known)
                {
                    entries.Add(new BondEntry
                    {
                        GirlId = -1,
                        Known = false,
                        RelationshipRatio = relationship.Ratio,
                        IsDatingKnown = relationship.IsDatingAndKnown()
                    });
                    continue;
                }
                data_girls.girls other = relationship.Girls[0] == parent ? relationship.Girls[1] : relationship.Girls[0];
                if (other == null)
                {
                    entries.Add(new BondEntry
                    {
                        GirlId = -1,
                        Known = false,
                        RelationshipRatio = relationship.Ratio,
                        IsDatingKnown = relationship.IsDatingAndKnown()
                    });
                    continue;
                }
                entries.Add(new BondEntry
                {
                    GirlId = other.id,
                    Known = true,
                    RelationshipRatio = relationship.Ratio,
                    IsDatingKnown = relationship.IsDatingAndKnown()
                });
            }
            return entries;
        }

        private static void FillBondEntryStats(BondEntry entry, data_girls.girls parent, data_girls.girls other)
        {
            if (entry == null || parent == null || other == null || other == parent)
            {
                return;
            }
            Relationships._relationship relationship = TryGetRelationship(parent, other);
            if (relationship != null)
            {
                entry.RelationshipRatio = relationship.Ratio;
                entry.IsDatingKnown = relationship.IsDatingAndKnown();
            }
        }

        private static Relationships._relationship TryGetRelationship(data_girls.girls girl, data_girls.girls other)
        {
            if (girl == null || other == null)
            {
                return null;
            }
            Relationships._relationship relationship = girl.GetCachedRelationship(other);
            if (relationship != null)
            {
                return relationship;
            }
            relationship = other.GetCachedRelationship(girl);
            if (relationship != null)
            {
                return relationship;
            }
            if (Relationships.RelationshipsData == null)
            {
                return null;
            }
            foreach (Relationships._relationship candidate in Relationships.RelationshipsData)
            {
                if (candidate == null || candidate.Girls == null || candidate.Girls.Count < 2)
                {
                    continue;
                }
                if ((candidate.Girls[0] == girl && candidate.Girls[1] == other) || (candidate.Girls[0] == other && candidate.Girls[1] == girl))
                {
                    return candidate;
                }
            }
            return null;
        }

        private static void Upsert(GraduationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.GirlId < 0)
            {
                return;
            }
            EnsureLoaded();
            Records[snapshot.GirlId] = snapshot;
            Save();
        }

        private static void EnsureLoaded()
        {
            string scope = GraduationDetailsPaths.GetSaveKey();
            if (loaded && loadedScope == scope)
            {
                return;
            }
            loadedScope = scope;
            loaded = true;
            Records.Clear();
            try
            {
                GraduationDetailsPaths.TryMigrateLegacyFileOnce("graduation_snapshots.json");
                if (!File.Exists(DataPath))
                {
                    if (!File.Exists(LegacyDataPath))
                    {
                        return;
                    }
                }
                string loadPath = File.Exists(DataPath) ? DataPath : LegacyDataPath;
                if (!File.Exists(loadPath))
                {
                    return;
                }
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                GraduationSnapshotList list = JsonUtility.FromJson<GraduationSnapshotList>(json);
                if (list == null || list.Records == null)
                {
                    return;
                }
                Records.Clear();
                foreach (GraduationSnapshot snapshot in list.Records)
                {
                    if (snapshot == null || snapshot.GirlId < 0)
                    {
                        continue;
                    }
                    Records[snapshot.GirlId] = snapshot;
                }
            }
            catch
            {
                Records.Clear();
            }
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(DataPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                GraduationSnapshotList list = new GraduationSnapshotList();
                list.Records = Records.Values.ToList();
                string json = JsonUtility.ToJson(list, true);
                File.WriteAllText(DataPath, json);
            }
            catch
            {
                // Ignore persistence errors to avoid breaking the game loop.
            }
        }

        private static void TryCapturePortrait(data_girls.girls girl, GraduationSnapshot snapshot)
        {
            string destPath = GetPortraitPath(snapshot);
            if (string.IsNullOrEmpty(destPath))
            {
                return;
            }
            if (File.Exists(destPath))
            {
                return;
            }
            string sourcePath = GetSourcePortraitPath(girl);
            if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
            {
                CopyPortrait(sourcePath, destPath);
                return;
            }
            mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (main == null || main.Data == null)
            {
                return;
            }
            data_girls_textures textures = main.Data.GetComponent<data_girls_textures>();
            if (textures == null)
            {
                return;
            }
            data_girls_textures.AddToQueue(girl, null);
            main.StartCoroutine(WaitForPortraitAndCopy(girl, destPath));
        }

        private static string GetSourcePortraitPath(data_girls.girls girl)
        {
            if (girl == null || girl.texture == null)
            {
                return "";
            }
            return girl.texture.GetBigPortraitURL();
        }

        private static IEnumerator WaitForPortraitAndCopy(data_girls.girls girl, string destPath)
        {
            float start = Time.realtimeSinceStartup;
            const float TimeoutSeconds = 5f;
            while (Time.realtimeSinceStartup - start < TimeoutSeconds)
            {
                string sourcePath = GetSourcePortraitPath(girl);
                if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
                {
                    CopyPortrait(sourcePath, destPath);
                    yield break;
                }
                yield return null;
            }
        }

        private static void CopyPortrait(string sourcePath, string destPath)
        {
            try
            {
                string dir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(sourcePath, destPath, true);
            }
            catch
            {
                // Ignore file copy errors to avoid breaking the game loop.
            }
        }
    }

    internal static class GraduationDetailsState
    {
        internal static bool Active;
        internal static int GirlId = -1;
        internal static bool AllowNonGraduated;

        internal static void Begin(data_girls.girls girl, bool allowNonGraduated = false)
        {
            if (girl == null || (!allowNonGraduated && girl.status != data_girls._status.graduated))
            {
                Clear();
                return;
            }
            Active = true;
            GirlId = girl.id;
            AllowNonGraduated = allowNonGraduated;
        }

        internal static bool IsFor(data_girls.girls girl)
        {
            if (!Active || girl == null || girl.id != GirlId)
            {
                return false;
            }
            if (!AllowNonGraduated && girl.status != data_girls._status.graduated)
            {
                return false;
            }
            return true;
        }

        internal static void Clear()
        {
            Active = false;
            GirlId = -1;
            AllowNonGraduated = false;
        }
    }

    internal static class GraduationDetailsProfile
    {
        internal static void Show(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            GraduationDetailsState.Begin(girl, false);

            OpenProfile(girl);
        }

        internal static void ShowForStaff(staff._staff staffer)
        {
            if (staffer == null)
            {
                return;
            }
            int girlId;
            if (!StaffIdolStore.TryResolveStaffer(staffer, out girlId))
            {
                return;
            }
            data_girls.girls girl = data_girls.GetGirlByID(girlId);
            if (girl == null)
            {
                return;
            }
            GraduationDetailsState.Begin(girl, true);
            OpenProfile(girl);
        }

        private static void OpenProfile(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return;
            }
            PopupManager popupManager = main.Data.GetComponent<PopupManager>();
            if (popupManager == null)
            {
                return;
            }
            PopupManager._popup popup = popupManager.GetByType(PopupManager._type.girl_profile);
            if (popup == null || popup.obj == null)
            {
                return;
            }
            Profile_Popup profile = popup.obj.GetComponent<Profile_Popup>();
            if (profile == null)
            {
                return;
            }
            popupManager.Open(PopupManager._type.girl_profile, true);
            profile.Set(girl);
            profile.SetTab(Profile_Popup._tabs.jobs);
        }
    }

    internal sealed class GraduationDetailsButton : MonoBehaviour
    {
        private int girlId = -1;
        private Graphic rootGraphic;
        private Button button;

        internal void SetGirl(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            this.girlId = girl.id;
            EnsureClickableTarget();
        }

        private void EnsureClickableTarget()
        {
            if (rootGraphic == null)
            {
                rootGraphic = GetComponent<Graphic>();
            }
            if (rootGraphic == null)
            {
                Image image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                rootGraphic = image;
            }
            rootGraphic.raycastTarget = true;

            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                {
                    button = gameObject.AddComponent<Button>();
                }
                button.transition = Selectable.Transition.None;
                button.targetGraphic = rootGraphic;
            }
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != rootGraphic)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        private void OnClick()
        {
            data_girls.girls girl = data_girls.GetGirlByID(girlId);
            if (girl == null)
            {
                return;
            }
            GraduationDetailsProfile.Show(girl);
        }
    }

    internal sealed class StaffProfileButton : MonoBehaviour
    {
        private int staffId = -1;

        internal void SetStaff(staff._staff staffer)
        {
            staffId = staffer != null ? staffer.id : -1;
        }

        public void OnClick()
        {
            staff._staff staffer = staff.GetStaffByID(staffId);
            if (staffer == null)
            {
                return;
            }
            GraduationDetailsProfile.ShowForStaff(staffer);
            ContextMenuController.Hide_();
        }
    }

    internal static class StaffProfileContextMenu
    {
        internal static void TryInject(ContextMenuController cmc, staff._staff staffer)
        {
            if (cmc == null || staffer == null)
            {
                return;
            }
            int girlId;
            bool mapped = StaffIdolStore.TryResolveStaffer(staffer, out girlId);
            if (!mapped && !StaffIdolStore.IsFormerIdolStaff(staffer))
            {
                return;
            }
            GameObject menu = cmc.open_mainMenu;
            if (menu == null)
            {
                return;
            }
            if (menu.GetComponentInChildren<StaffProfileButton>(true) != null)
            {
                return;
            }
            GameObject template = GetTemplateButton(menu);
            if (template == null)
            {
                return;
            }
            GameObject button = UnityEngine.Object.Instantiate(template);
            button.name = "ProfileButton";
            button.transform.SetParent(template.transform.parent, false);
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.SetActive(true);
            ConfigureProfileButton(button, staffer, mapped);
        }

        private static GameObject GetTemplateButton(GameObject menu)
        {
            GameObject candidate = FindButtonByField(menu, "Nickname");
            if (candidate != null)
            {
                return candidate;
            }
            candidate = FindButtonByField(menu, "Fire");
            if (candidate != null)
            {
                return candidate;
            }
            ContextMenuButton contextButton = menu != null ? menu.GetComponentInChildren<ContextMenuButton>(true) : null;
            if (contextButton != null)
            {
                return contextButton.gameObject;
            }
            ButtonDefault buttonDefault = menu != null ? menu.GetComponentInChildren<ButtonDefault>(true) : null;
            if (buttonDefault != null)
            {
                return buttonDefault.gameObject;
            }
            Button uiButton = menu != null ? menu.GetComponentInChildren<Button>(true) : null;
            return uiButton != null ? uiButton.gameObject : null;
        }

        private static GameObject FindButtonByField(GameObject menu, string fieldName)
        {
            if (menu == null)
            {
                return null;
            }
            MonoBehaviour[] behaviours = menu.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }
                System.Reflection.FieldInfo field = behaviour.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field == null || field.FieldType != typeof(GameObject))
                {
                    continue;
                }
                GameObject obj = field.GetValue(behaviour) as GameObject;
                if (obj != null)
                {
                    return obj;
                }
            }
            return null;
        }

        private static void ConfigureProfileButton(GameObject button, staff._staff staffer, bool mapped)
        {
            if (button == null)
            {
                return;
            }
            DisableLocalization(button);
            ContextMenuButton contextButton = button.GetComponent<ContextMenuButton>();
            if (contextButton != null)
            {
                contextButton.prefab_childMenu = null;
                if (contextButton.arrow != null)
                {
                    contextButton.arrow.SetActive(false);
                }
            }
            ButtonDefault buttonDefault = button.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.SetTooltip(
                    mapped
                        ? ModLocalization.Get("context_menu.profile.tooltip_ready", "Show idol profile")
                        : ModLocalization.Get("context_menu.profile.tooltip_pending", "Show idol profile (data pending)"));
                buttonDefault.Activate(true, false);
            }
            SetButtonLabel(button, contextButton, buttonDefault, ModLocalization.Get("context_menu.profile.label", "Profile"));
            Button uiButton = button.GetComponent<Button>();
            if (uiButton == null)
            {
                uiButton = button.AddComponent<Button>();
            }
            uiButton.onClick = new Button.ButtonClickedEvent();
            StaffProfileButton handler = button.GetComponent<StaffProfileButton>();
            if (handler == null)
            {
                handler = button.AddComponent<StaffProfileButton>();
            }
            handler.SetStaff(staffer);
            uiButton.onClick.AddListener(handler.OnClick);
        }

        private static void DisableLocalization(GameObject root)
        {
            if (root == null)
            {
                return;
            }
            Lang_Button[] langButtons = root.GetComponentsInChildren<Lang_Button>(true);
            foreach (Lang_Button langButton in langButtons)
            {
                if (langButton == null)
                {
                    continue;
                }
                langButton.Constant = "";
                langButton.Tooltip = "";
                langButton.enabled = false;
            }
        }

        private static void SetButtonLabel(GameObject button, ContextMenuButton contextButton, ButtonDefault buttonDefault, string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                if (contextButton != null && contextButton.text != null)
                {
                    ExtensionMethods.SetText(contextButton.text, label);
                }
                if (buttonDefault != null && buttonDefault.Text != null)
                {
                    ExtensionMethods.SetText(buttonDefault.Text, label);
                }
                TextMeshProUGUI[] tmps = button.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI tmp in tmps)
                {
                    if (tmp != null)
                    {
                        tmp.text = label;
                    }
                }
                Text[] texts = button.GetComponentsInChildren<Text>(true);
                foreach (Text text in texts)
                {
                    if (text != null)
                    {
                        text.text = label;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GirlButton_Graduated), nameof(GirlButton_Graduated.Set))]
    internal static class GirlButton_Graduated_Set_Patch
    {
        private static void Postfix(GirlButton_Graduated __instance, data_girls.girls _Girl)
        {
            if (__instance == null || _Girl == null)
            {
                return;
            }
            GraduationDetailsButton button = __instance.gameObject.GetComponent<GraduationDetailsButton>();
            if (button == null)
            {
                button = __instance.gameObject.AddComponent<GraduationDetailsButton>();
            }
            button.SetGirl(_Girl);
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), nameof(ContextMenuController.Show), new Type[] { typeof(staff._staff) })]
    internal static class ContextMenuController_Show_Staff_Patch
    {
        private static void Postfix(ContextMenuController __instance, staff._staff _staff)
        {
            StaffProfileContextMenu.TryInject(__instance, _staff);
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), nameof(ContextMenuController.Show), new Type[] { typeof(agency._room) })]
    internal static class ContextMenuController_Show_Room_Patch
    {
        private static void Postfix(ContextMenuController __instance, agency._room _room)
        {
            if (__instance == null)
            {
                return;
            }
            staff._staff staffer = __instance.Staff;
            if (staffer == null && _room != null)
            {
                staffer = _room.staffer;
            }
            StaffProfileContextMenu.TryInject(__instance, staffer);
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), nameof(Profile_Popup.Set))]
    internal static class Profile_Popup_Set_Patch
    {
        private static void Prefix(Profile_Popup __instance, data_girls.girls _Girl)
        {
            if (_Girl == null || !GraduationDetailsState.IsFor(_Girl))
            {
                GraduationDetailsState.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderHeader")]
    internal static class Profile_Popup_RenderHeader_Patch
    {
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (!GraduationDetailsState.IsFor(__instance.Girl))
            {
                return;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(__instance.Girl.id);
            if (snapshot == null)
            {
                return;
            }
            ApplySnapshotDob(__instance, snapshot);
            ApplySnapshotPortrait(__instance, snapshot);
        }

        private static void ApplySnapshotDob(Profile_Popup profile, GraduationSnapshot snapshot)
        {
            if (profile.Header_DateOfBirth == null)
            {
                return;
            }
            DateTime birthDate = profile.Girl != null ? profile.Girl.birthday : DateTime.MinValue;
            if (!string.IsNullOrEmpty(snapshot.Birthdate))
            {
                try
                {
                    birthDate = ExtensionMethods.ToDateTime(snapshot.Birthdate);
                }
                catch
                {
                    // Ignore parse errors; fall back to current data.
                }
            }
            int age = snapshot.AgeAtGraduation > 0 ? snapshot.AgeAtGraduation : (profile.Girl != null ? profile.Girl.GetAge() : 0);
            string dob = ExtensionMethods.ToString_Loc(birthDate, "DATETIME__BIRTHDAY");
            string ageText = Language.Insert("PROFILE__AGE", new string[]
            {
                age.ToString()
            });
            string text = Language.Data["PROFILE__BIRTHDATE"] + ": " + dob + " " + ageText;
            ExtensionMethods.SetText(profile.Header_DateOfBirth, text);
        }

        private static void ApplySnapshotPortrait(Profile_Popup profile, GraduationSnapshot snapshot)
        {
            string portraitPath = GraduationSnapshotStore.GetPortraitPath(snapshot);
            if (string.IsNullOrEmpty(portraitPath) || !File.Exists(portraitPath))
            {
                return;
            }
            Image portrait = profile.Portrait != null ? profile.Portrait.GetComponent<Image>() : null;
            Image shadow = profile.Portrait_Shadow != null ? profile.Portrait_Shadow.GetComponent<Image>() : null;
            if (portrait == null && shadow == null)
            {
                return;
            }
            string cacheKey = ("file://" + portraitPath).Replace("\\", "").Replace("/", "");
            Sprite cached = LoadTexture.GetSprite(cacheKey);
            if (cached != null)
            {
                if (portrait != null)
                {
                    portrait.sprite = cached;
                }
                if (shadow != null)
                {
                    shadow.sprite = cached;
                }
                return;
            }
            if (LoadTexture.instance != null)
            {
                if (portrait != null)
                {
                    LoadTexture.instance.StartCoroutine(LoadTexture.LoadSprite(portraitPath, portrait, null));
                }
                if (shadow != null)
                {
                    LoadTexture.instance.StartCoroutine(LoadTexture.LoadSprite(portraitPath, shadow, null));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderTab_Jobs")]
    internal static class Profile_Popup_RenderTab_Jobs_Patch
    {
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (!GraduationDetailsState.IsFor(__instance.Girl))
            {
                SetActiveSafe(__instance.Jobs_Singles, true);
                SetActiveSafe(__instance.Jobs_Shows, true);
                SetActiveSafe(__instance.Jobs_Contracts, true);
                return;
            }

            data_girls.girls girl = __instance.Girl;
            if (girl == null)
            {
                return;
            }

            string earnings = ModLocalization.Get("jobs.total_earnings_prefix", "Total earnings: ")
                + ExtensionMethods.formatMoney(girl.GetTotalEarnings(), false, false, false);
            ExtensionMethods.SetText(__instance.Jobs_Salary, earnings);

            string singlesList = BuildReleasedSinglesList(girl);
            ExtensionMethods.SetText(__instance.Jobs_Earnings, Language.Data["SINGLES__PARTICIPATION"] + ":\n" + singlesList);

            ExtensionMethods.SetText(__instance.Jobs_Shows, BuildMarriageText(girl));
            ExtensionMethods.SetText(__instance.Jobs_Contracts, BuildCustodyText(girl));

            SetActiveSafe(__instance.Jobs_Singles, false);

            if (__instance.Jobs_Container != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.Jobs_Container.GetComponent<RectTransform>());
            }
        }

        private static void SetActiveSafe(GameObject obj, bool active)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }

        private static string BuildReleasedSinglesList(data_girls.girls girl)
        {
            if (girl == null || singles.Singles == null)
            {
                return Language.Data["SSK__NA"];
            }
            List<string> lines = new List<string>();
            foreach (singles._single single in singles.Singles)
            {
                if (single == null || single.status != singles._single._status.released)
                {
                    continue;
                }
                if (single.girls == null || !single.girls.Contains(girl))
                {
                    continue;
                }
                string line = single.title;
                if (single.GetCenter() == girl)
                {
                    line += " (" + Language.Data["SINGLES__CENTER"] + ")";
                }
                lines.Add(line);
            }
            if (lines.Count == 0)
            {
                return Language.Data["SSK__NA"];
            }
            return string.Join("\n", lines.ToArray());
        }

        private static string FormatLocalizedValue(string templateOrPrefix, string value)
        {
            string safeTemplate = templateOrPrefix ?? string.Empty;
            string safeValue = value ?? string.Empty;
            if (safeTemplate.Contains("{0}"))
            {
                try
                {
                    return string.Format(safeTemplate, safeValue);
                }
                catch
                {
                }
            }
            return safeTemplate + safeValue;
        }

        private static string FormatLocalizedCount(string templateOrPrefix, int count, string legacySuffix)
        {
            string safeTemplate = templateOrPrefix ?? string.Empty;
            string countText = count.ToString();
            if (safeTemplate.Contains("{0}"))
            {
                try
                {
                    return string.Format(safeTemplate, countText);
                }
                catch
                {
                }
            }
            return safeTemplate + countText + legacySuffix + ")";
        }

        private static string BuildMarriageText(data_girls.girls girl)
        {
            MarriageRecord record = MarriageRecordStore.GetRecord(girl.id);
            if (record != null && record.MarriedToPlayer)
            {
                string name = record.PlayerName;
                if (string.IsNullOrEmpty(name))
                {
                    name = Language.Data["NOTIF__IDOL_REL_YOU"];
                }
                return FormatLocalizedValue(
                    ModLocalization.Get("jobs.married_to_prefix", "Married to {0}"),
                    name);
            }
            return ModLocalization.Get("jobs.married_to_none", "Married to: No");
        }

        private static string BuildCustodyText(data_girls.girls girl)
        {
            MarriageRecord record = MarriageRecordStore.GetRecord(girl.id);
            if (record == null)
            {
                return ModLocalization.Get("jobs.custody_unknown", "Custody: Unknown");
            }
            if (record.KidsCount < 0)
            {
                return ModLocalization.Get("jobs.custody_unknown", "Custody: Unknown");
            }
            int custodyCount = record.KidsCount;
            string suffix = custodyCount == 1
                ? ModLocalization.Get("jobs.custody_child_singular", " child")
                : ModLocalization.Get("jobs.custody_child_plural", " children");
            if (record.Custody == CustodyOwner.Player)
            {
                return FormatLocalizedCount(
                    ModLocalization.Get("jobs.custody_player_prefix", "Children Living With: Player ({0})"),
                    custodyCount,
                    suffix);
            }
            if (record.Custody == CustodyOwner.Idol)
            {
                return FormatLocalizedCount(
                    ModLocalization.Get("jobs.custody_idol_prefix", "Children Living With: Idol ({0})"),
                    custodyCount,
                    suffix);
            }
            if (record.Custody == CustodyOwner.None)
            {
                return ModLocalization.Get("jobs.custody_none", "Children: None");
            }
            return ModLocalization.Get("jobs.custody_unknown", "Children Living With: Unknown");
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderTab_Fans")]
    internal static class Profile_Popup_RenderTab_Fans_Patch
    {
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null || !GraduationDetailsState.IsFor(__instance.Girl))
            {
                return;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(__instance.Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return;
            }
            TextMeshProUGUI text = __instance.Fans_Text_Total != null ? __instance.Fans_Text_Total.GetComponent<TextMeshProUGUI>() : null;
            if (text == null)
            {
                return;
            }
            long total = GraduationSnapshotStore.GetTotalFans(snapshot);
            text.text = Language.Data["TOTAL"] + ": " + ExtensionMethods.formatNumber(total, false, false);
        }
    }

    [HarmonyPatch(typeof(Profile_Fans_Pies), nameof(Profile_Fans_Pies.Render))]
    internal static class Profile_Fans_Pies_Render_Patch
    {
        private enum PieColor
        {
            Blue = 0,
            Green = 1,
            Gold = 2
        }

        private static bool Prefix(Profile_Fans_Pies __instance, data_girls.girls _Girl)
        {
            if (__instance == null || _Girl == null || !GraduationDetailsState.IsFor(_Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(_Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            RenderSnapshot(__instance, snapshot);
            return false;
        }

        private static void RenderSnapshot(Profile_Fans_Pies pies, GraduationSnapshot snapshot)
        {
            long total = GraduationSnapshotStore.GetTotalFans(snapshot);
            float male = 0.5f;
            float female = 0.5f;
            float casual = 0.5f;
            float hardcore = 0.5f;
            float teen = 0.33f;
            float youngAdult = 0.33f;
            float adult = 0.33f;
            if (total != 0L)
            {
                male = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.male);
                female = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.female);
                casual = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.casual);
                hardcore = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.hardcore);
                teen = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.teen);
                youngAdult = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.youngAdult);
                adult = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.adult);
            }

            if (male > female || male == female)
            {
                SetPie(pies.Fans_Pie_Male, male, PieColor.Green);
                SetPie(pies.Fans_Pie_Female, female, PieColor.Blue);
                SetValue(pies.Male, resources.fanType.male, male, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.male));
                SetValue(pies.Female, resources.fanType.female, female, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.female));
            }
            else
            {
                SetPie(pies.Fans_Pie_Male, male, PieColor.Blue);
                SetPie(pies.Fans_Pie_Female, female, PieColor.Green);
                SetValue(pies.Male, resources.fanType.male, male, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.male));
                SetValue(pies.Female, resources.fanType.female, female, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.female));
            }

            if (casual > hardcore || casual == hardcore)
            {
                SetPie(pies.Fans_Pie_Casual, casual, PieColor.Green);
                SetPie(pies.Fans_Pie_Hardcore, hardcore, PieColor.Blue);
                SetValue(pies.Casual, resources.fanType.casual, casual, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.casual));
                SetValue(pies.Hardcore, resources.fanType.hardcore, hardcore, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.hardcore));
            }
            else
            {
                SetPie(pies.Fans_Pie_Casual, casual, PieColor.Blue);
                SetPie(pies.Fans_Pie_Hardcore, hardcore, PieColor.Green);
                SetValue(pies.Casual, resources.fanType.casual, casual, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.casual));
                SetValue(pies.Hardcore, resources.fanType.hardcore, hardcore, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.hardcore));
            }

            PieColor teenColor = PieColor.Blue;
            PieColor yaColor = PieColor.Gold;
            PieColor adultColor = PieColor.Green;
            if (teen != youngAdult || teen != adult)
            {
                if (teen > youngAdult && teen > adult)
                {
                    teenColor = PieColor.Green;
                    if (youngAdult > adult)
                    {
                        yaColor = PieColor.Gold;
                        adultColor = PieColor.Blue;
                    }
                    else
                    {
                        yaColor = PieColor.Blue;
                        adultColor = PieColor.Gold;
                    }
                }
                else if (youngAdult > teen && youngAdult > adult)
                {
                    yaColor = PieColor.Green;
                    if (teen > adult)
                    {
                        teenColor = PieColor.Gold;
                        adultColor = PieColor.Blue;
                    }
                    else
                    {
                        teenColor = PieColor.Blue;
                        adultColor = PieColor.Gold;
                    }
                }
                else
                {
                    adultColor = PieColor.Green;
                    if (youngAdult > teen)
                    {
                        yaColor = PieColor.Gold;
                        teenColor = PieColor.Blue;
                    }
                    else
                    {
                        yaColor = PieColor.Blue;
                        teenColor = PieColor.Gold;
                    }
                }
            }
            SetPie(pies.Fans_Pie_Teen, teen, teenColor);
            SetPie(pies.Fans_Pie_YA, youngAdult, yaColor);
            SetPie(pies.Fans_Pie_Adult, adult + teen, adultColor);
            SetValue(pies.Teen, resources.fanType.teen, teen, teenColor, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.teen));
            SetValue(pies.YoungAdult, resources.fanType.youngAdult, youngAdult, yaColor, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.youngAdult));
            SetValue(pies.Adult, resources.fanType.adult, adult, adultColor, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.adult));
        }

        private static Color32 GetPieColor(PieColor color)
        {
            switch (color)
            {
                case PieColor.Blue:
                    return mainScript.lightBlue32;
                case PieColor.Green:
                    return mainScript.green_light32;
                case PieColor.Gold:
                    return mainScript.gold32;
                default:
                    return mainScript.blue32;
            }
        }

        private static Color32 GetValueColor(PieColor color)
        {
            switch (color)
            {
                case PieColor.Blue:
                    return mainScript.blue32;
                case PieColor.Green:
                    return mainScript.green32;
                case PieColor.Gold:
                    return mainScript.gold32;
                default:
                    return mainScript.blue32;
            }
        }

        private static void SetPie(GameObject obj, float val, PieColor color)
        {
            if (obj == null)
            {
                return;
            }
            Image image = obj.GetComponent<Image>();
            if (image == null)
            {
                return;
            }
            image.fillAmount = val;
            image.color = GetPieColor(color);
        }

        private static void SetValue(GameObject obj, resources.fanType type, float ratio, PieColor color, long count)
        {
            if (obj == null)
            {
                return;
            }
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                return;
            }
            string value = ExtensionMethods.size(resources.GetFanTitle(type), 10) + "\n";
            value += ExtensionMethods.formatNumber(count, false, false);
            value = value + " [" + ExtensionMethods.toPercent(ratio) + "%]";
            text.text = value;
            text.color = GetValueColor(color);
        }
    }

    [HarmonyPatch(typeof(Profile_Fan), nameof(Profile_Fan.Set_Fans_Number))]
    internal static class Profile_Fan_Set_Fans_Number_Patch
    {
        private static bool Prefix(Profile_Fan __instance, data_girls.girls Girl, long Total)
        {
            if (__instance == null || Girl == null || !GraduationDetailsState.IsFor(Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            long total = GraduationSnapshotStore.GetTotalFans(snapshot);
            long count = GraduationSnapshotStore.GetFanCount(snapshot, __instance.Gender, __instance.Hardcoreness, __instance.Age);
            float ratio = total > 0L ? (float)count / (float)total : 0f;
            Color32 barColor = count == 0L ? mainScript.lightBlue32 : mainScript.green_light32;
            Color32 textColor = count == 0L ? mainScript.blue32 : mainScript.green32;
            Image barImage = __instance.Bar != null ? __instance.Bar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                barImage.color = barColor;
            }
            TextMeshProUGUI value = __instance.Value != null ? __instance.Value.GetComponent<TextMeshProUGUI>() : null;
            if (value != null)
            {
                value.color = textColor;
                value.text = ExtensionMethods.formatNumber(count, false, true) + " [" + ExtensionMethods.toPercent(ratio) + "%]";
            }
            RectTransform barTransform = __instance.Bar != null ? __instance.Bar.GetComponent<RectTransform>() : null;
            if (barTransform != null)
            {
                barTransform.localScale = new Vector2(1f, ratio);
            }
            Image portrait = __instance.Portrait != null ? __instance.Portrait.GetComponent<Image>() : null;
            if (portrait != null)
            {
                portrait.fillAmount = 0.97f;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Profile_Fan), nameof(Profile_Fan.Set_Appeal))]
    internal static class Profile_Fan_Set_Appeal_Patch
    {
        private static bool Prefix(Profile_Fan __instance, data_girls.girls Girl)
        {
            if (__instance == null || Girl == null || !GraduationDetailsState.IsFor(Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            float val = GraduationSnapshotStore.GetFanAppeal(snapshot, __instance.Gender, __instance.Hardcoreness, __instance.Age);
            Color32 barColor = mainScript.lightBlue32;
            Color32 textColor = mainScript.blue32;
            if (val > 0.5f)
            {
                barColor = mainScript.green_light32;
                textColor = mainScript.green32;
            }
            else if (val < 0.25f)
            {
                barColor = mainScript.red_light32;
                textColor = mainScript.red32;
            }
            Image barImage = __instance.Bar != null ? __instance.Bar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                barImage.color = barColor;
            }
            TextMeshProUGUI value = __instance.Value != null ? __instance.Value.GetComponent<TextMeshProUGUI>() : null;
            if (value != null)
            {
                value.color = textColor;
                value.text = ExtensionMethods.toPercent(val) + "%";
            }
            RectTransform barTransform = __instance.Bar != null ? __instance.Bar.GetComponent<RectTransform>() : null;
            if (barTransform != null)
            {
                barTransform.localScale = new Vector2(1f, val);
            }
            Image portrait = __instance.Portrait != null ? __instance.Portrait.GetComponent<Image>() : null;
            if (portrait != null)
            {
                portrait.fillAmount = val;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Profile_Fan), nameof(Profile_Fan.Set_Opinion))]
    internal static class Profile_Fan_Set_Opinion_Patch
    {
        private static bool Prefix(Profile_Fan __instance, data_girls.girls Girl)
        {
            if (__instance == null || Girl == null || !GraduationDetailsState.IsFor(Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            float val = GraduationSnapshotStore.GetFanOpinion(snapshot, __instance.Gender, __instance.Hardcoreness, __instance.Age);
            Color32 barColor = mainScript.lightBlue32;
            Color32 textColor = mainScript.blue32;
            if (val > 0.75f)
            {
                barColor = mainScript.green_light32;
                textColor = mainScript.green32;
            }
            else if (val < 0.5f)
            {
                barColor = mainScript.red_light32;
                textColor = mainScript.red32;
            }
            Image barImage = __instance.Bar != null ? __instance.Bar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                barImage.color = barColor;
            }
            RectTransform barTransform = __instance.Bar != null ? __instance.Bar.GetComponent<RectTransform>() : null;
            if (barTransform != null)
            {
                barTransform.localScale = new Vector2(1f, val);
            }
            Image portrait = __instance.Portrait != null ? __instance.Portrait.GetComponent<Image>() : null;
            if (portrait != null)
            {
                portrait.fillAmount = val;
            }
            float scaled = val * 200f - 100f;
            TextMeshProUGUI value = __instance.Value != null ? __instance.Value.GetComponent<TextMeshProUGUI>() : null;
            if (value != null)
            {
                value.color = textColor;
                value.text = Mathf.RoundToInt(scaled) + "%";
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderTab_Bonds")]
    internal static class Profile_Popup_RenderTab_Bonds_Patch
    {
        private static bool Prefix(Profile_Popup __instance)
        {
            if (__instance == null || !GraduationDetailsState.IsFor(__instance.Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(__instance.Girl.id);
            if (snapshot == null)
            {
                return true;
            }
            RenderSnapshot(__instance, snapshot);
            return false;
        }

        private static void RenderSnapshot(Profile_Popup profile, GraduationSnapshot snapshot)
        {
            if (profile == null || profile.Bonds_Container == null)
            {
                return;
            }
            ExtensionMethods.destroyChildren(profile.Bonds_Container.transform);
            bool rendered = false;
            if (snapshot.Bonds != null)
            {
                foreach (BondSectionSnapshot section in snapshot.Bonds)
                {
                    if (section == null)
                    {
                        continue;
                    }
                    if (section.Type == BondSectionType.NoInfo)
                    {
                        continue;
                    }
                    string title = GetSectionTitle(section);
                    if (!string.IsNullOrEmpty(title))
                    {
                        AddTitle(profile, title);
                    }
                    if (section.Entries != null && section.Entries.Count > 0)
                    {
                        RenderEntries(profile, profile.Girl, section.Entries);
                        rendered = true;
                    }
                    else
                    {
                        rendered = true;
                    }
                }
            }
            if (!rendered)
            {
                AddTitle(profile, Language.Data["PROFILE__NO_INFO"]);
            }
        }

        private static string GetSectionTitle(BondSectionSnapshot section)
        {
            switch (section.Type)
            {
                case BondSectionType.CliqueKnown:
                {
                    data_girls.girls leader = data_girls.GetGirlByID(section.LeaderId);
                    if (leader == null)
                    {
                        return Language.Data["PROFILE__UNKNOWN_CLIQUE"];
                    }
                    return Language.Insert("PROFILE__CLIQUE", new string[]
                    {
                        leader.GetName(true)
                    });
                }
                case BondSectionType.CliqueUnknown:
                    return Language.Data["PROFILE__UNKNOWN_CLIQUE"];
                case BondSectionType.Bullies:
                    return Language.Data["PROFILE__BULLIES"];
                case BondSectionType.BulliedBy:
                    return Language.Data["PROFILE__BULLIED_BY"];
                case BondSectionType.BestFriends:
                    return Language.Data["PROFILE__BEST_FRIENDS"];
                case BondSectionType.Friends:
                    return Language.Data["PROFILE__FRIENDS"];
                case BondSectionType.Dislikes:
                    return Language.Data["PROFILE__DISLIKES"];
                case BondSectionType.Hates:
                    return Language.Data["PROFILE__HATES"];
                case BondSectionType.NoInfo:
                    return Language.Data["PROFILE__NO_INFO"];
                default:
                    return "";
            }
        }

        private static void AddTitle(Profile_Popup profile, string text)
        {
            if (profile.prefab_Bonds_Title == null)
            {
                return;
            }
            GameObject obj = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Title);
            ExtensionMethods.SetText(obj, text);
            obj.transform.SetParent(profile.Bonds_Container.transform, false);
        }

        private static void RenderEntries(Profile_Popup profile, data_girls.girls parent, List<BondEntry> entries)
        {
            if (profile.prefab_Bonds_Container == null || profile.prefab_Bonds_Girl == null || entries == null || entries.Count == 0)
            {
                return;
            }
            int count = 0;
            GameObject container = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Container);
            foreach (BondEntry entry in entries)
            {
                GameObject item = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Girl);
                Profile_Bond bond = item.GetComponent<Profile_Bond>();
                if (bond != null && entry != null && entry.Known && entry.GirlId >= 0)
                {
                    data_girls.girls other = data_girls.GetGirlByID(entry.GirlId);
                    if (other != null)
                    {
                        ApplyBond(bond, other, parent, entry);
                    }
                    else
                    {
                        bond.Set_Unknown();
                    }
                }
                else if (bond != null)
                {
                    bond.Set_Unknown();
                }
                item.transform.SetParent(container.transform, false);
                count++;
                if (count == 3)
                {
                    count = 0;
                    container.transform.SetParent(profile.Bonds_Container.transform, false);
                    container = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Container);
                }
            }
            if (count > 0)
            {
                container.transform.SetParent(profile.Bonds_Container.transform, false);
            }
        }

        private static void ApplyBond(Profile_Bond bond, data_girls.girls girl, data_girls.girls parent, BondEntry entry)
        {
            if (bond == null || girl == null || parent == null)
            {
                return;
            }
            bond.Unknown.SetActive(false);
            bond.Portrait.SetActive(true);
            bond.Name.SetActive(true);
            bond.RelationshipBar.SetActive(true);
            bond.Girl = girl;
            bond.Parent = parent;
            GirlProfileOnHover hover = bond.GetComponent<GirlProfileOnHover>();
            if (hover != null)
            {
                hover.Set(girl, false);
            }
            Image portrait = bond.Portrait != null ? bond.Portrait.GetComponent<Image>() : null;
            if (portrait != null && girl.texture != null)
            {
                portrait.sprite = girl.texture.middle;
            }
            ExtensionMethods.SetText(bond.Name, girl.GetName(true));

            float ratio = entry != null ? Mathf.Clamp01(entry.RelationshipRatio) : 0.5f;
            RectTransform bar = bond.RelationshipBar != null ? bond.RelationshipBar.GetComponent<RectTransform>() : null;
            if (bar != null)
            {
                bar.localScale = new Vector2(ratio, 1f);
            }
            Image barImage = bond.RelationshipBar != null ? bond.RelationshipBar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                Color32 color = mainScript.lightBlue32;
                if (entry != null && entry.IsDatingKnown)
                {
                    color = mainScript.pink32;
                }
                else
                {
                    Relationships._relationship._status status = StatusFromRatio(ratio);
                    if (status == Relationships._relationship._status.best_friends || status == Relationships._relationship._status.friends)
                    {
                        color = mainScript.green32;
                    }
                    else if (status == Relationships._relationship._status.dislikes)
                    {
                        color = mainScript.red32;
                    }
                    else if (status == Relationships._relationship._status.hates)
                    {
                        color = mainScript.black32;
                    }
                }
                barImage.color = color;
            }
        }

        private static Relationships._relationship._status StatusFromRatio(float ratio)
        {
            if (ratio > 0.9f)
            {
                return Relationships._relationship._status.best_friends;
            }
            if (ratio > 0.7f)
            {
                return Relationships._relationship._status.friends;
            }
            if (ratio < 0.2f)
            {
                return Relationships._relationship._status.hates;
            }
            if (ratio < 0.4f)
            {
                return Relationships._relationship._status.dislikes;
            }
            return Relationships._relationship._status.normal;
        }
    }

    [HarmonyPatch(typeof(PopupManager), "Close", new Type[] { typeof(Action) })]
    internal static class PopupManager_Close_Patch
    {
        private static void Postfix(PopupManager __instance)
        {
            // Only clear when profile popup is no longer open. Other popup closes (tooltips, etc.)
            // should not disable graduation snapshot rendering while profile is active.
            if (__instance != null)
            {
                PopupManager._popup profilePopup = __instance.GetByType(PopupManager._type.girl_profile);
                if (profilePopup != null && profilePopup.open)
                {
                    return;
                }
            }
            GraduationDetailsState.Clear();
        }
    }

    [HarmonyPatch(typeof(Dating), nameof(Dating.Marriage_Girl_Quits))]
    internal static class Dating_Marriage_Girl_Quits_Patch
    {
        private static void Prefix(data_girls.girls Girl)
        {
            MarriageContext.Begin(Girl);
        }

        private static void Postfix()
        {
            MarriageContext.SaveAndClear();
        }
    }

    [HarmonyPatch(typeof(Dating), "Marriage_Player_Quits")]
    internal static class Dating_Marriage_Player_Quits_Patch
    {
        private static void Prefix()
        {
            MarriageContext.Begin(Dating.GetWife());
        }

        private static void Postfix()
        {
            MarriageContext.SaveAndClear();
        }
    }

    [HarmonyPatch(typeof(Dating), "M_get_number_of_kids")]
    internal static class Dating_M_get_number_of_kids_Patch
    {
        private static void Postfix(int __result)
        {
            if (MarriageContext.Active)
            {
                MarriageContext.SetKids(__result);
            }
        }
    }

    [HarmonyPatch(typeof(Dating), "M_get_custody_string")]
    internal static class Dating_M_get_custody_string_Patch
    {
        private static void Postfix(string __result, bool good_outcome, int number_of_kids)
        {
            if (MarriageContext.Active)
            {
                MarriageContext.SetCustody(__result, good_outcome, number_of_kids);
            }
        }
    }

    [HarmonyPatch(typeof(Date_Graduation), nameof(Date_Graduation.Hire_As_Staffer))]
    internal static class Date_Graduation_Hire_As_Staffer_Patch
    {
        private static void Prefix(data_girls.girls Girl)
        {
            if (Girl == null)
            {
                return;
            }
            StaffHireContext.Begin(Girl);
            GraduationSnapshotStore.Capture(Girl);
        }

        private static void Postfix()
        {
            if (StaffHireContext.Active)
            {
                StaffHireContext.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.Hire))]
    internal static class staff_Hire_Patch
    {
        private static void Postfix(staff._staff Staffer)
        {
            if (!StaffHireContext.Active)
            {
                return;
            }
            if (StaffHireContext.GirlId >= 0)
            {
                data_girls.girls girl = data_girls.GetGirlByID(StaffHireContext.GirlId);
                if (girl != null && GraduationSnapshotStore.GetSnapshot(girl.id) == null)
                {
                    GraduationSnapshotStore.Capture(girl);
                }
            }
            StaffHireContext.Complete(Staffer);
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.LoadFunction))]
    internal static class staff_LoadFunction_Patch
    {
        private static void Postfix()
        {
            StaffIdolStore.BackfillFromStaff();
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduate))]
    internal static class data_girls_girls_Graduate_Patch
    {
        private static void Prefix(data_girls.girls __instance)
        {
            GraduationSnapshotStore.Capture(__instance);
        }
    }

    [HarmonyPatch(typeof(data_girls), nameof(data_girls.LoadFunction))]
    internal static class data_girls_LoadFunction_Patch
    {
        private static void Postfix()
        {
            GraduationSnapshotStore.Backfill(data_girls.girl);
        }
    }
}
