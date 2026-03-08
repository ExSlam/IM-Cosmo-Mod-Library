using System.Collections.Generic;
using HarmonyLib;
using IMDataCore;
using UnityEngine;

namespace IMDataCore.TemplateMod
{
    /// <summary>
    /// End-to-end template bridge showing registration, custom data, custom events,
    /// and optional read/flush helpers for first-time IM Data Core users.
    /// </summary>
    internal static class TemplateDataCoreBridge
    {
        private const string NamespaceIdentifier = "com.example.template_mod";
        private const string SampleEntityKind = "idol";
        private const string SampleDataKey = "idol_123_snapshot";
        private const string SampleEventType = "template_event_fired";
        private const string SampleEventSource = "mod.com.example.template_mod.TemplatePatch.Postfix";

        private static IMDataCoreSession sharedSession;

        /// <summary>
        /// Attempts one-time namespace registration once IM Data Core reports ready.
        /// </summary>
        internal static bool InitializeIfAvailable()
        {
            if (sharedSession != null)
            {
                return true;
            }

            if (!IMDataCoreApi.IsReady())
            {
                return false;
            }

            string errorMessage;
            if (!IMDataCoreApi.TryRegisterNamespace(NamespaceIdentifier, out sharedSession, out errorMessage))
            {
                Debug.LogWarning("[TemplateMod] Namespace registration failed: " + errorMessage);
                return false;
            }

            Debug.Log("[TemplateMod] IM Data Core namespace registered.");
            return true;
        }

        /// <summary>
        /// Writes one sample JSON state value under a stable data key.
        /// </summary>
        internal static bool SaveSampleState()
        {
            if (sharedSession == null)
            {
                return false;
            }

            string payloadJson = "{\"feature_enabled\":true,\"version\":1}";
            string errorMessage;
            if (!IMDataCoreApi.TrySetCustomJson(sharedSession, SampleDataKey, payloadJson, out errorMessage))
            {
                Debug.LogWarning("[TemplateMod] TrySetCustomJson failed: " + errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Appends one sample custom timeline event.
        /// </summary>
        internal static bool AppendSampleEvent(int idolId)
        {
            if (sharedSession == null)
            {
                return false;
            }

            string payloadJson = "{\"note\":\"template flow\"}";
            string errorMessage;
            if (!IMDataCoreApi.TryAppendCustomEvent(
                sharedSession,
                idolId,
                SampleEntityKind,
                idolId.ToString(),
                SampleEventType,
                payloadJson,
                SampleEventSource,
                out errorMessage))
            {
                Debug.LogWarning("[TemplateMod] TryAppendCustomEvent failed: " + errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads one sample JSON value. Returns false when key is missing or request fails.
        /// </summary>
        internal static bool TryLoadSampleState(out string json)
        {
            json = string.Empty;
            if (sharedSession == null)
            {
                return false;
            }

            string errorMessage;
            if (!IMDataCoreApi.TryGetCustomJson(sharedSession, SampleDataKey, out json, out errorMessage))
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Debug.LogWarning("[TemplateMod] TryGetCustomJson failed: " + errorMessage);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads recent events for one idol. Useful for timeline UI examples.
        /// </summary>
        internal static List<IMDataCoreEvent> ReadRecentEventsForIdol(int idolId, int maxCount)
        {
            List<IMDataCoreEvent> events;
            string errorMessage;
            if (!IMDataCoreApi.TryReadRecentEventsForIdol(idolId, maxCount, out events, out errorMessage))
            {
                Debug.LogWarning("[TemplateMod] TryReadRecentEventsForIdol failed: " + errorMessage);
                return new List<IMDataCoreEvent>();
            }

            return events;
        }

        /// <summary>
        /// Forces persistence immediately. Optional helper for critical transitions.
        /// </summary>
        internal static bool FlushNow()
        {
            string errorMessage;
            if (!IMDataCoreApi.TryFlushNow(out errorMessage))
            {
                Debug.LogWarning("[TemplateMod] TryFlushNow failed: " + errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Unregisters namespace session during plugin shutdown.
        /// </summary>
        internal static void Shutdown()
        {
            if (sharedSession == null)
            {
                return;
            }

            string errorMessage;
            IMDataCoreApi.TryUnregisterNamespace(sharedSession, out errorMessage);
            sharedSession = null;
        }
    }

    /// <summary>
    /// Template bootstrap patch: initialize the bridge after popup systems are ready.
    /// </summary>
    [HarmonyPatch(typeof(PopupManager), "Start")]
    internal static class PopupManager_Start_TemplateDataCoreBootstrap_Patch
    {
        [HarmonyPostfix]
        private static void InitializeAfterPopupManagerStart()
        {
            TemplateDataCoreBridge.InitializeIfAvailable();
        }
    }
}
