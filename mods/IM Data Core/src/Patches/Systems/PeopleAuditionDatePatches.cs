using System;
using HarmonyLib;

namespace IMDataCore
{
    /// <summary>
    /// Observes the authoritative post-generation audition slate. GenerateGirls is
    /// private vanilla implementation state, so the target is intentionally named.
    /// </summary>
    [HarmonyPatch(typeof(Auditions), CoreConstants.HarmonyAuditionsGenerateGirlsMethodName, new Type[] { typeof(Auditions.data) })]
    internal static class Auditions_GenerateGirls_IMDataCoreHistory_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Auditions.data _data)
        {
            IMDataCoreController.Instance.CaptureAuditionCandidatesGenerated(_data);
        }
    }

    /// <summary>
    /// Audition completion becomes authoritative only after the player has finished
    /// hiring/rejecting candidates and Popup_Audition.Close returns successfully.
    /// </summary>
    [HarmonyPatch(typeof(Popup_Audition), nameof(Popup_Audition.Close))]
    internal static class Popup_Audition_Close_IMDataCoreHistory_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Auditions.data ___Data)
        {
            IMDataCoreController.Instance.CaptureAuditionCompleted(___Data);
        }
    }

    /// <summary>
    /// Tags the synchronous audition-card -> data_girls.Hire handoff with truthful
    /// hire provenance without changing vanilla state.
    /// </summary>
    [HarmonyPatch(typeof(Audition_Data_Card), nameof(Audition_Data_Card.Hire))]
    internal static class Audition_Data_Card_Hire_IMDataCoreProvenance_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(out string __state)
        {
            __state = IdolHireProvenanceContext.Enter("audition");
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception, string __state)
        {
            IdolHireProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    /// <summary>
    /// Tags the synchronous graduation-successor -> data_girls.Hire handoff.
    /// </summary>
    [HarmonyPatch(typeof(Date_Graduation), nameof(Date_Graduation.Find_Successor), new Type[] { typeof(data_girls.girls), typeof(bool) })]
    internal static class Date_Graduation_Find_Successor_IMDataCoreProvenance_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(out string __state)
        {
            __state = IdolHireProvenanceContext.Enter("graduation_successor");
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception, string __state)
        {
            IdolHireProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    /// <summary>
    /// Captures the authoritative generic-date composite after vanilla has already
    /// selected Next_Location / Wear_Masks and successfully built the dialogue.
    /// </summary>
    [HarmonyPatch(typeof(Dating), nameof(Dating.GenerateGenericDate), new Type[] { typeof(data_girls.girls) })]
    internal static class Dating_GenerateGenericDate_IMDataCoreHistory_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Girl, out GenericDateOccurrenceSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateGenericDateOccurrenceSnapshot(Girl);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl, GenericDateOccurrenceSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureGenericDatePresented(Girl, __state);
        }
    }

    /// <summary>
    /// Completes a pending generic-date occurrence only after the dialogue action
    /// dispatcher has committed vanilla's dating/add_points effect.
    /// </summary>
    [HarmonyPatch(typeof(vn_actions), nameof(vn_actions.Do), new Type[] { typeof(data_dialogues._action), typeof(Event_Manager._activeEvent) })]
    internal static class vn_actions_Do_IMDataCoreGenericDateCompletion_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_dialogues._action action)
        {
            IMDataCoreController.Instance.CaptureGenericDateCompletedFromDialogueAction(action);
        }
    }

    /// <summary>
    /// Captures the semantic flirt result after DoFlirt has applied the outcome to
    /// DatingData. This deliberately avoids calling GetOutcome or GetPotential-like
    /// non-pure helpers from logging code.
    /// </summary>
    [HarmonyPatch(typeof(Date_Flirt), nameof(Date_Flirt.DoFlirt), new Type[] { typeof(data_girls.girls) })]
    internal static class Date_Flirt_DoFlirt_IMDataCoreHistory_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Girl, out PlayerFlirtSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreatePlayerFlirtSnapshot(Girl);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl, PlayerFlirtSnapshot __state)
        {
            IMDataCoreController.Instance.CapturePlayerFlirtOutcome(Girl, __state);
        }
    }
}
