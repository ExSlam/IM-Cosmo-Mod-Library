using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveNLoadFixes.Transport
{
    internal static class SaveShutdownCoordinator
    {
        private static readonly object Sync = new object();
        private static readonly Queue<string> Failures = new Queue<string>();
        private static bool waiting;
        private static bool quit;
        private static bool allowQuit;
        private static bool startingSave;
        private static bool subscribed;
        private static bool priorPause;
        private static bool quitIssued;
        private static SaveShutdownPump pump;
        internal static bool IsWaiting { get { return waiting; } }
        internal static bool BlockNewSave { get { return waiting && !startingSave; } }

        internal static void EnsurePump()
        {
            if (pump == null)
            {
                GameObject host = new GameObject("SNLF save completion");
                UnityEngine.Object.DontDestroyOnLoad(host);
                pump = host.AddComponent<SaveShutdownPump>();
            }
            if (subscribed) return;
            subscribed = true;
            // The supplied Unity player is older than some reference assemblies.
            // Discover this optional event instead of emitting a hard dependency.
            EventInfo wants = typeof(Application).GetEvent("wantsToQuit", BindingFlags.Public | BindingFlags.Static);
            if (wants != null) wants.AddEventHandler(null, new Func<bool>(WantsToQuit));
        }

        internal static void SaveAndExit(Settings_Tab settings, bool exitApplication)
        {
            EnsurePump();
            if (waiting) { if (exitApplication) quit = true; return; }
            waiting = true;
            quit = exitApplication;
            priorPause = staticVars.dateTimeForcedPause;
            staticVars.dateTimeForcedPause = true;
            startingSave = true;
            try
            {
                // Preserve the actual vanilla save route, including its SaveEvent
                // subscribers and every caller-level IMDC/SNLF patch.
                AccessTools.Method(typeof(Settings_Tab), "Save").Invoke(settings, null);
            }
            catch (Exception exception)
            {
                ReportFailure("Save and exit: " + (exception.InnerException ?? exception));
            }
            finally { startingSave = false; }
            // Always leave at least one Update for main-thread continuations.
        }

        private static bool WantsToQuit()
        {
            if (allowQuit) return true;
            if (!startingSave && !OrderedSaveTransport.HasAnyPendingWrites() && SaveParticipationApi.PendingAttemptCount == 0)
                return true;
            if (!waiting)
            {
                priorPause = staticVars.dateTimeForcedPause;
                staticVars.dateTimeForcedPause = true;
            }
            waiting = true;
            quit = true;
            return false;
        }

        internal static void ReportFailure(string message) { lock (Sync) Failures.Enqueue(message); }
        private static void DrainFailures()
        {
            string[] messages;
            lock (Sync) { messages = Failures.ToArray(); Failures.Clear(); }
            foreach (string message in messages) Debug.LogError(SaveNLoadFixesConstants.LogPrefix + message);
        }
        internal static void Pump()
        {
            // Another wantsToQuit subscriber can veto our request. If the game
            // reaches another Update, reopen admission instead of stranding all
            // later saves behind a shutdown that never happened. This player can
            // call OnApplicationQuit even when a wantsToQuit handler vetoes exit;
            // continued Update execution is the reliable cancellation witness.
            if (quitIssued)
            {
                quitIssued = false;
                allowQuit = false;
                staticVars.dateTimeForcedPause = priorPause;
                SaveParticipationApi.ResumeAfterMenu();
            }
            DrainFailures();
            if (!waiting || startingSave || OrderedSaveTransport.HasAnyPendingWrites() || !SaveParticipationApi.TrySealExit()) return;
            DrainFailures();
            // Completion, not success. Faulted/cancelled attempts release this
            // barrier exactly as successful attempts do. No timeout pretends an
            // unfinished attempt has ended; the update loop remains alive.
            waiting = false;
            if (quit)
            {
                allowQuit = true;
                quitIssued = true;
                Application.Quit();
            }
            else
            {
                try { SceneManager.LoadScene("Main Menu"); }
                finally
                {
                    Persistence.ModDataStorage.ClearActive();
                    staticVars.dateTimeForcedPause = priorPause;
                    SaveParticipationApi.ResumeAfterMenu();
                }
            }
        }
    }

    public sealed class SaveShutdownPump : MonoBehaviour
    {
        private void Update() { SaveShutdownCoordinator.Pump(); }
    }

    [HarmonyPatch(typeof(Settings_Tab), "OnQuit")]
    internal static class SettingsQuit_SaveCompletion_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Settings_Tab __instance) { SaveShutdownCoordinator.SaveAndExit(__instance, true); return false; }
    }

    [HarmonyPatch(typeof(Settings_Tab), "OnMainMenu")]
    internal static class SettingsMenu_SaveCompletion_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Settings_Tab __instance) { SaveShutdownCoordinator.SaveAndExit(__instance, false); return false; }
    }
}
