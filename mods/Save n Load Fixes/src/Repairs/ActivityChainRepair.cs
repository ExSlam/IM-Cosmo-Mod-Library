using System;
using System.Collections.Generic;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs numbered finding N07. Vanilla owns Activities.Chain only as a static
    /// runtime list: fresh restart clears it in Awake(), while same-process F9 can
    /// retain a discarded timeline's list. V1 section-marked repair state restores
    /// the target save's exact ordered future intent after vanilla Activities.LoadFunction.
    /// </summary>
    internal static class ActivityChainRepair
    {
        internal const int SectionVersion = 1;

        private static long restoredLoadCount;
        private static long restoredEntryCount;
        private static long legacyEmptyFallbackCount;
        private static long staleChainClearCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return ActivityChainPatchHealth.IsHealthy; }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredEntryCount { get { return Interlocked.Read(ref restoredEntryCount); } }
        internal static long LegacyEmptyFallbackCount { get { return Interlocked.Read(ref legacyEmptyFallbackCount); } }
        internal static long StaleChainClearCount { get { return Interlocked.Read(ref staleChainClearCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static void RestoreAfterVanillaLoad()
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                ClearStaleChain();
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N07 cleared stale Activities.Chain because the adopted SavedData object has no repair-envelope read association.";
                RenderChainBestEffort();
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.activity_chain_version == 0)
            {
                ClearStaleChain();
                Interlocked.Increment(ref legacyEmptyFallbackCount);
                lastDiagnostic = "N07 loaded a pre-N07 save; exact future intent is unavailable, so the deterministic fresh-process empty-chain baseline was applied.";
                RenderChainBestEffort();
                return;
            }

            if (!state.Valid ||
                state.Envelope.records.activity_chain_version != SectionVersion ||
                state.Envelope.records.activity_chain == null)
            {
                ClearStaleChain();
                Interlocked.Increment(ref invalidSectionCount);
                lastDiagnostic = "N07 failed closed after clearing stale Activities.Chain because the repair section is invalid or unsupported.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                RenderChainBestEffort();
                return;
            }

            List<Activity._type> restored = new List<Activity._type>();
            List<int> saved = state.Envelope.records.activity_chain;
            for (int index = 0; index < saved.Count; index++)
            {
                int activityType = saved[index];
                if (activityType < (int)Activity._type.performance ||
                    activityType > (int)Activity._type.spa_treatment)
                {
                    ClearStaleChain();
                    Interlocked.Increment(ref invalidSectionCount);
                    lastDiagnostic = "N07 failed closed because the saved chain contains an activity type outside the audited enum domain.";
                    Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                    RenderChainBestEffort();
                    return;
                }

                restored.Add((Activity._type)activityType);
            }

            // Replace only after the complete ordered section validates. Do not call
            // Chain_Add because its current max-chain gate could mutate the target save.
            ClearStaleChain();
            Activities.Chain.AddRange(restored);
            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredEntryCount, restored.Count);
            lastDiagnostic = "N07 restored exact ordered Activities.Chain future intent for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
            RenderChainBestEffort();
        }

        private static void ClearStaleChain()
        {
            if (Activities.Chain == null)
            {
                Activities.Chain = new List<Activity._type>();
                return;
            }

            if (Activities.Chain.Count != 0)
            {
                Interlocked.Increment(ref staleChainClearCount);
            }

            Activities.Chain.Clear();
        }

        private static void RenderChainBestEffort()
        {
            try
            {
                if (mainScript.WidgetDetails != null)
                {
                    mainScript.WidgetDetails.RenderChain();
                }
            }
            catch (Exception)
            {
                // Presentation refresh must never alter the authoritative restore result.
            }
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }

                mainScript main = Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal static class ActivityChainPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N07 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N07 patch failure";
            }
        }
    }
}
