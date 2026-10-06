using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace HarmonyModBrowser
{
    internal static class HmbConstants
    {
        internal const string HarmonyId = "com.cosmo.harmonymodbrowser";
        internal const string DllFileName = HarmonyId + ".dll";
        internal const string DisplayName = "Harmony Mod Browser";
        internal const string Version = "1.0.0";

        internal const int MainPageSize = 24;
        internal const int MainCardsPerFrame = 4;
        internal const int ChooserPageSize = 16;
        internal const int ChooserCardsPerFrame = 4;
        internal const float SearchDebounceSeconds = 0.18f;
        internal const float OwnershipPollSeconds = 0.35f;

        internal const string SearchPlaceholderKey = "HMB_SEARCH_PLACEHOLDER";
        internal const string UploadSearchPlaceholderKey = "HMB_UPLOAD_SEARCH_PLACEHOLDER";
        internal const string UpdateSearchPlaceholderKey = "HMB_UPDATE_SEARCH_PLACEHOLDER";
        internal const string UpdateSourceSearchPlaceholderKey = "HMB_UPDATE_SOURCE_SEARCH_PLACEHOLDER";
        internal const string PreviousKey = "HMB_PREVIOUS";
        internal const string NextKey = "HMB_NEXT";
        internal const string NoResultsKey = "HMB_NO_RESULTS";
        internal const string PageFormatKey = "HMB_PAGE_FORMAT";
        internal const string CountFormatKey = "HMB_COUNT_FORMAT";
        internal const string MatchCountFormatKey = "HMB_MATCH_COUNT_FORMAT";
        internal const string VersionFormatKey = "HMB_VERSION_FORMAT";
        internal const string LocalKey = "HMB_LOCAL";
        internal const string WorkshopKey = "HMB_WORKSHOP";
        internal const string AllModsKey = "HMB_ALL_MODS";
        internal const string UploadModKey = "HMB_UPLOAD_MOD";
        internal const string UpdateModKey = "HMB_UPDATE_MOD";
        internal const string SelectKey = "HMB_SELECT";
        internal const string CloseKey = "HMB_CLOSE";
        internal const string UploadChooserTitleKey = "HMB_UPLOAD_CHOOSER_TITLE";
        internal const string UpdateChooserTitleKey = "HMB_UPDATE_CHOOSER_TITLE";
        internal const string UpdateSourceTitleKey = "HMB_UPDATE_SOURCE_TITLE";
        internal const string LoadingOwnershipKey = "HMB_LOADING_OWNERSHIP";
        internal const string OwnershipUnavailableKey = "HMB_OWNERSHIP_UNAVAILABLE";
        internal const string NoLocalModsKey = "HMB_NO_LOCAL_MODS";
        internal const string NoOwnedWorkshopKey = "HMB_NO_OWNED_WORKSHOP";
        internal const string LocalCountFormatKey = "HMB_LOCAL_COUNT_FORMAT";
        internal const string OwnedCountFormatKey = "HMB_OWNED_COUNT_FORMAT";

        internal const string DefaultSearchPlaceholder = "Search mods...";
        internal const string DefaultUploadSearchPlaceholder = "Search local mods to upload...";
        internal const string DefaultUpdateSearchPlaceholder = "Search your Workshop mods...";
        internal const string DefaultUpdateSourceSearchPlaceholder = "Search local versions...";
        internal const string DefaultPrevious = "Previous";
        internal const string DefaultNext = "Next";
        internal const string DefaultNoResults = "No results";
        internal const string DefaultPageFormat = "Page {0} / {1}";
        internal const string DefaultCountFormat = "{0} mods  |  {1}/page";
        internal const string DefaultMatchCountFormat = "{0} matches  |  {1}/page";
        internal const string DefaultVersionFormat = "version {0}";
        internal const string DefaultLocal = "Local";
        internal const string DefaultWorkshop = "Workshop";
        internal const string DefaultAllMods = "All Mods";
        internal const string DefaultUploadMod = "Upload Mod";
        internal const string DefaultUpdateMod = "Update Mod";
        internal const string DefaultSelect = "Select";
        internal const string DefaultClose = "Close";
        internal const string DefaultUploadChooserTitle = "Upload a local mod";
        internal const string DefaultUpdateChooserTitle = "Update one of your Workshop mods";
        internal const string DefaultUpdateSourceTitle = "Choose the local version to upload";
        internal const string DefaultLoadingOwnership = "Loading Steam Workshop ownership...";
        internal const string DefaultOwnershipUnavailable = "Steam ownership details could not be loaded. Only verified owned items are shown.";
        internal const string DefaultNoLocalMods = "No local mods found.";
        internal const string DefaultNoOwnedWorkshop = "No Workshop mods owned by this Steam account were found.";
        internal const string DefaultLocalCountFormat = "{0} local mods";
        internal const string DefaultOwnedCountFormat = "{0} owned Workshop mods";
    }

    internal static class HmbLog
    {
        internal static void Info(string message)
        {
            Debug.Log("[Harmony Mod Browser] " + message);
        }

        internal static void Warning(string message)
        {
            Debug.LogWarning("[Harmony Mod Browser] " + message);
        }

        internal static void Error(string message)
        {
            Debug.LogError("[Harmony Mod Browser] " + message);
        }
    }

    internal static class HmbLocalization
    {
        internal static string Get(string key, string fallback)
        {
            return ModLocalizationSystem.ModLocalization.Get(key, fallback);
        }

        internal static string Format(string key, string fallback, params object[] args)
        {
            string template = Get(key, fallback);
            try { return string.Format(template, args); }
            catch (FormatException) { return template; }
        }

        internal static void Reload()
        {
            ModLocalizationSystem.ModLocalization.ReloadAll();
        }
    }

    internal static class ModSearch
    {
        private static readonly char[] Separators = { ' ', '\t', '\r', '\n' };

        internal static bool Matches(Mods._mod mod, string query)
        {
            if (mod == null)
            {
                return false;
            }
            string normalized = (query ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                return true;
            }

            string[] terms = normalized.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < terms.Length; i++)
            {
                string term = terms[i];
                if (!Contains(mod.Title, term) &&
                    !Contains(mod.Description, term) &&
                    !Contains(mod.Author, term) &&
                    !Contains(mod.Version, term) &&
                    !Contains(mod.ModName, term) &&
                    !TagsContain(mod.Tags, term))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool Contains(string value, string term)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(term) &&
                   value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TagsContain(List<string> tags, string term)
        {
            if (tags == null)
            {
                return false;
            }
            for (int i = 0; i < tags.Count; i++)
            {
                if (Contains(tags[i], term))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal static class SteamOwnershipMonitor
    {
        internal static int Revision { get; private set; }
        internal static bool QueryCompleted { get; private set; }
        internal static bool QueryFailed { get; private set; }

        internal static void Reset()
        {
            QueryCompleted = !SafeIsSteam();
            QueryFailed = false;
            Revision++;
        }

        internal static void NotifyCompleted()
        {
            QueryCompleted = true;
            QueryFailed = false;
            Revision++;
            BrowserRegistry.RefreshOwnershipSensitiveViews();
        }

        internal static void NotifyFailed()
        {
            QueryCompleted = true;
            QueryFailed = true;
            Revision++;
            BrowserRegistry.RefreshOwnershipSensitiveViews();
        }

        internal static bool HasPendingWorkshopOwnership()
        {
            if (QueryCompleted || !SafeIsSteam() || Mods._Mods == null)
            {
                return false;
            }

            for (int i = 0; i < Mods._Mods.Count; i++)
            {
                Mods._mod mod = Mods._Mods[i];
                if (mod != null && mod.IsWorkshop() && !mod.SteamDetails.HasValue)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsOwnedWorkshop(Mods._mod mod)
        {
            if (mod == null || !mod.IsWorkshop() || !mod.SteamDetails.HasValue)
            {
                return false;
            }
            try
            {
                return mod.IsPlayerCreated();
            }
            catch
            {
                return false;
            }
        }

        internal static bool SafeIsSteam()
        {
            try
            {
                return mainScript.IsSteam();
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class BrowserRegistry
    {
        internal static ModBrowserController MainBrowser;
        internal static SelectionOverlayController SelectionOverlay;
        internal static UpdateSourceSearchController UpdateSourceSearch;

        internal static void RefreshOwnershipSensitiveViews()
        {
            if (MainBrowser != null)
            {
                MainBrowser.RefreshOwnershipSensitiveUi();
            }
            if (SelectionOverlay != null)
            {
                SelectionOverlay.RefreshOwnershipSensitiveUi();
            }
        }
    }
}
