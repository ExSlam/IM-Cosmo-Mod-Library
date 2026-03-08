using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Captures push assignment changes on policy push updates.
    /// </summary>
    [HarmonyPatch(typeof(Pushes), nameof(Pushes.SetPushes))]
    internal static class Pushes_SetPushes_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation push slot snapshot for change detection.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out List<int> __state)
        {
            __state = IMDataCoreController.Instance.CreatePushSlotSnapshot(Pushes.Girls);
        }

        /// <summary>
        /// Records push start/end transitions after push slots are reassigned.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(List<data_girls.girls> _girls, List<int> __state)
        {
            IList<data_girls.girls> currentPushGirls = _girls ?? Pushes.Girls;
            IMDataCoreController.Instance.CapturePushSetAssignments(__state, currentPushGirls, Pushes.Days);
        }
    }

    /// <summary>
    /// Captures explicit push removals.
    /// </summary>
    [HarmonyPatch(typeof(Pushes), nameof(Pushes.RemovePush))]
    internal static class Pushes_RemovePush_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures push slot and idol assignment before removal mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Girl, out PushRemovalSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreatePushRemovalSnapshot(Girl);
        }

        /// <summary>
        /// Records push end event after removal is applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PushRemovalSnapshot __state)
        {
            IMDataCoreController.Instance.CapturePushRemovalSnapshot(__state);
        }
    }

    /// <summary>
    /// Captures daily push aging progression.
    /// </summary>
    [HarmonyPatch(typeof(Pushes), CoreConstants.HarmonyPushesOnNewDayMethodName)]
    internal static class Pushes_OnNewDay_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records per-slot day-increment events after one game-day tick.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            IMDataCoreController.Instance.CapturePushDayIncrements();
        }
    }

    /// <summary>
    /// Captures idol-idol dating start transitions.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._relationship), nameof(Relationships._relationship.StartDating))]
    internal static class Relationships_relationship_StartDating_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation dating state before relationship start logic.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._relationship __instance, out bool __state)
        {
            __state = IMDataCoreController.Instance.CreateRelationshipDatingSnapshot(__instance);
        }

        /// <summary>
        /// Records idol-idol dating start event after relationship mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._relationship __instance, bool __state)
        {
            IMDataCoreController.Instance.CaptureIdolDatingStarted(__instance, __state);
        }
    }

    /// <summary>
    /// Captures idol-idol dating break-up transitions.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._relationship), nameof(Relationships._relationship.BreakUp))]
    internal static class Relationships_relationship_BreakUp_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation dating state before break-up logic.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._relationship __instance, out bool __state)
        {
            __state = IMDataCoreController.Instance.CreateRelationshipDatingSnapshot(__instance);
        }

        /// <summary>
        /// Records idol-idol dating end event after relationship mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._relationship __instance, bool __state)
        {
            IMDataCoreController.Instance.CaptureIdolDatingEnded(__instance, __state);
        }
    }

    /// <summary>
    /// Captures idol-idol relationship status transitions from score updates.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._relationship), nameof(Relationships._relationship.Add))]
    internal static class Relationships_relationship_Add_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation relationship status before score update logic.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._relationship __instance, out Relationships._relationship._status __state)
        {
            __state = IMDataCoreController.Instance.CreateRelationshipStatusSnapshot(__instance);
        }

        /// <summary>
        /// Records relationship status transition after score update logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._relationship __instance, float val, Relationships._relationship._status __state)
        {
            IMDataCoreController.Instance.CaptureIdolRelationshipStatusChanged(__instance, __state, val);
        }
    }

    /// <summary>
    /// Captures relationship-level stop-bullying flows.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._relationship), nameof(Relationships._relationship.StopBullying))]
    internal static class Relationships_relationship_StopBullying_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation bullying state before relationship stop-bullying logic.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._relationship __instance, out RelationshipStopBullyingSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateRelationshipStopBullyingSnapshot(__instance);
        }

        /// <summary>
        /// Records bullying-end events after relationship stop-bullying logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RelationshipStopBullyingSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRelationshipStopBullying(__state);
        }
    }

    /// <summary>
    /// Captures clique join events.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._clique), nameof(Relationships._clique.AddMember))]
    internal static class Relationships_clique_AddMember_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records clique join event after one idol is added to a clique.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._clique __instance, data_girls.girls Girl)
        {
            IMDataCoreController.Instance.CaptureCliqueMemberJoined(__instance, Girl);
        }
    }

    /// <summary>
    /// Captures clique leave events.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._clique), nameof(Relationships._clique.Quit))]
    internal static class Relationships_clique_Quit_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation membership state before clique quit handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._clique __instance, data_girls.girls Girl, out CliqueQuitSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateCliqueQuitSnapshot(__instance, Girl);
        }

        /// <summary>
        /// Records clique leave event after quit logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._clique __instance, data_girls.girls Girl, bool violent, CliqueQuitSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureCliqueMemberLeft(__instance, Girl, violent, __state);
        }
    }

    /// <summary>
    /// Captures bullying start events initiated by cliques.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._clique), nameof(Relationships._clique.AddBulliedGirl))]
    internal static class Relationships_clique_AddBulliedGirl_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation bullied-state snapshot for one target idol.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._clique __instance, data_girls.girls Girl, out bool __state)
        {
            __state = __instance != null && Girl != null && __instance.IsBullied(Girl);
        }

        /// <summary>
        /// Records bullying start event after clique bullying mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._clique __instance, data_girls.girls Girl, bool __state)
        {
            IMDataCoreController.Instance.CaptureBullyingStarted(__instance, Girl, __state);
        }
    }

    /// <summary>
    /// Captures bullying stop events initiated by cliques.
    /// </summary>
    [HarmonyPatch(typeof(Relationships._clique), nameof(Relationships._clique.StopBullying), new Type[] { typeof(data_girls.girls) })]
    internal static class Relationships_clique_StopBullying_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation bullied-state snapshot for one target idol.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships._clique __instance, data_girls.girls Girl, out bool __state)
        {
            __state = __instance != null && Girl != null && __instance.IsBullied(Girl);
        }

        /// <summary>
        /// Records bullying end event after clique stop-bullying mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._clique __instance, data_girls.girls Girl, bool __state)
        {
            IMDataCoreController.Instance.CaptureBullyingEnded(__instance, Girl, __state);
        }
    }

    /// <summary>
    /// Captures scandal points popup mitigation actions.
    /// </summary>
    [HarmonyPatch(typeof(ScandalPoints_Popup), nameof(ScandalPoints_Popup.OnContinue))]
    internal static class ScandalPoints_Popup_OnContinue_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures selected scandal mitigation values before they are applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out ScandalMitigationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateScandalMitigationSnapshot();
        }

        /// <summary>
        /// Records scandal mitigation results after popup apply logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ScandalMitigationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureScandalMitigation(__state);
        }
    }

    /// <summary>
    /// Captures player-idol relationship points mutations.
    /// </summary>
    [HarmonyPatch(typeof(Relationships_Player), nameof(Relationships_Player.AddPoints))]
    internal static class Relationships_Player_AddPoints_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation relationship points for one idol/type tuple.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Relationships_Player._type Type, data_girls.girls Girl, out int __state)
        {
            __state = IMDataCoreController.Instance.CreatePlayerRelationshipPointsSnapshot(Type, Girl);
        }

        /// <summary>
        /// Records player-idol relationship delta after points mutation logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships_Player._type Type, data_girls.girls Girl, int Points, int __state)
        {
            IMDataCoreController.Instance.CapturePlayerRelationshipChanged(Type, Girl, Points, __state);
        }
    }

    /// <summary>
    /// Captures generic player date interactions.
    /// </summary>
    [HarmonyPatch(typeof(Dating), nameof(Dating.GoOnDate))]
    internal static class Dating_GoOnDate_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-interaction dating snapshot before date logic runs.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Girl, out PlayerDateInteractionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreatePlayerDateInteractionSnapshot(Girl);
        }

        /// <summary>
        /// Records one player date interaction event after date logic returns.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl, List<string> __result, PlayerDateInteractionSnapshot __state)
        {
            IMDataCoreController.Instance.CapturePlayerDateInteractionFromGoOnDate(Girl, __state, __result);
        }
    }

    /// <summary>
    /// Captures specific player date interactions.
    /// </summary>
    [HarmonyPatch(typeof(Dating), nameof(Dating.GoOnSpecificDate))]
    internal static class Dating_GoOnSpecificDate_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-interaction dating snapshot before specific date logic runs.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Girl, out PlayerDateInteractionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreatePlayerDateInteractionSnapshot(Girl);
        }

        /// <summary>
        /// Records one specific-date interaction event after date logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl, PlayerDateInteractionSnapshot __state)
        {
            IMDataCoreController.Instance.CapturePlayerDateInteractionFromGoOnSpecificDate(Girl, __state);
        }
    }

    /// <summary>
    /// Captures marriage outcome setup when idol quits for marriage.
    /// </summary>
    [HarmonyPatch(typeof(Dating), nameof(Dating.Marriage_Girl_Quits))]
    internal static class Dating_Marriage_Girl_Quits_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records marriage outcome variables after `Marriage_Girl_Quits` completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl)
        {
            IMDataCoreController.Instance.CapturePlayerMarriageOutcomeFromGirlQuits(Girl);
        }
    }

    /// <summary>
    /// Captures marriage outcome finalization after marriage flow continuation.
    /// </summary>
    [HarmonyPatch(typeof(Dating), nameof(Dating.AfterMarriage))]
    internal static class Dating_AfterMarriage_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records final marriage outcome snapshot after `AfterMarriage` executes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl)
        {
            IMDataCoreController.Instance.CapturePlayerMarriageOutcomeFromAfterMarriage(Girl);
        }
    }

    /// <summary>
}
