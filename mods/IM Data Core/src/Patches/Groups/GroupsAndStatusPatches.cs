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
    /// Captures idol transfer events between groups.
    /// </summary>
    [HarmonyPatch(typeof(Groups), nameof(Groups.Transfer))]
    internal static class Groups_Transfer_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures source group state before transfer mutation executes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Girl, out GroupTransferSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateGroupTransferSnapshot(Girl);
        }

        /// <summary>
        /// Records one transfer event after membership mutation completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl, Groups._group Group, GroupTransferSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureIdolGroupTransferred(Girl, Group, __state);
        }
    }

    /// <summary>
    /// Captures group-disband lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Groups), nameof(Groups.Disband))]
    internal static class Groups_Disband_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-disband group state before transfers/cancellations mutate membership.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Groups._group DisbandedGroup, out GroupDisbandSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateGroupDisbandSnapshot(DisbandedGroup);
        }

        /// <summary>
        /// Records one group-disband event after the operation completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Groups._group DisbandedGroup, GroupDisbandSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureGroupDisbanded(DisbandedGroup, __state);
        }
    }

    /// <summary>
    /// Captures new-group lifecycle events from creation popup completion.
    /// </summary>
    [HarmonyPatch(typeof(New_Group_Popup), nameof(New_Group_Popup.OnContinue))]
    internal static class New_Group_Popup_OnContinue_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures existing group id set before creation mutates group list.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out GroupCreationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateGroupCreationSnapshot();
        }

        /// <summary>
        /// Records one group-created event after the new group is committed.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(GroupCreationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureGroupCreated(__state);
        }
    }

    /// <summary>
    /// Captures group parameter-point mutations from parameter-track AddPoints overload.
    /// </summary>
    [HarmonyPatch(typeof(Groups._group), nameof(Groups._group.AddPoints), new Type[] { typeof(data_girls._paramType), typeof(int) })]
    internal static class Groups_group_AddPoints_Param_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation group parameter-point state.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Groups._group __instance, data_girls._paramType Type, out GroupParamPointChangeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateGroupParamPointChangeSnapshot(__instance, Type);
        }

        /// <summary>
        /// Records one parameter-point mutation event after mutation completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Groups._group __instance, data_girls._paramType Type, int val, GroupParamPointChangeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureGroupParamPointsChanged(__instance, Type, val, __state);
        }
    }

    /// <summary>
    /// Captures group appeal-point spend mutations.
    /// </summary>
    [HarmonyPatch(typeof(Groups._group), nameof(Groups._group.SpendPoints), new Type[] { typeof(data_girls._paramType), typeof(resources.fanType), typeof(int) })]
    internal static class Groups_group_SpendPoints_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation state for group appeal-point spending.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            Groups._group __instance,
            data_girls._paramType Type,
            resources.fanType Target,
            int Val,
            out GroupAppealPointSpendSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateGroupAppealPointSpendSnapshot(__instance, Type, Target, Val);
        }

        /// <summary>
        /// Records one group appeal-point spend event after mutation completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            Groups._group __instance,
            data_girls._paramType Type,
            resources.fanType Target,
            int Val,
            GroupAppealPointSpendSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureGroupAppealPointsSpent(__instance, Type, Target, Val, __state);
        }
    }

    /// <summary>
    /// Captures idol status transitions with previous/new state values.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.SetStatus))]
    internal static class data_girls_girls_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous status before game code mutates it.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance != null ? __instance.status : data_girls._status.normal;
        }

        /// <summary>
        /// Records transition after status mutation has completed.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            data_girls._status finalStatus = __instance != null ? __instance.status : __state;
            IMDataCoreController.Instance.CaptureStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures producer dating status transitions.
    /// </summary>
    [HarmonyPatch(typeof(Dating._partner), nameof(Dating._partner.SetStatus))]
    internal static class Dating_partner_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous producer-dating status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Dating._partner __instance, out Dating._partner._status __state)
        {
            __state = __instance != null ? __instance.Status : Dating._partner._status.NONE;
        }

        /// <summary>
        /// Records producer-dating status change after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Dating._partner __instance, Dating._partner._status _Status, Dating._partner._status __state)
        {
            Dating._partner._status finalStatus = __instance != null ? __instance.Status : __state;
            IMDataCoreController.Instance.CaptureDatingPartnerStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures idol dating partner status transitions.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls._dating_data), nameof(data_girls.girls._dating_data.SetDatingStatus))]
    internal static class data_girls_girls_dating_data_SetDatingStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous idol dating partner status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls._dating_data __instance, out data_girls.girls._dating_data._partner_status __state)
        {
            __state = __instance != null ? __instance.Partner_Status : data_girls.girls._dating_data._partner_status.free;
        }

        /// <summary>
        /// Records idol dating partner status change after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            data_girls.girls._dating_data __instance,
            data_girls.girls._dating_data._partner_status _NewStatus,
            data_girls.girls._dating_data._partner_status __state)
        {
            data_girls.girls._dating_data._partner_status finalStatus = __instance != null ? __instance.Partner_Status : __state;
            IMDataCoreController.Instance.CaptureIdolDatingStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
}
