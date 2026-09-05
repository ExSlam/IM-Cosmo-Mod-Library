using System;
using HarmonyLib;

namespace IMDataCore
{
    internal static class AwardSpeechDeliveryContext
    {
        [ThreadStatic]
        private static Awards._speech currentSpeech;

        internal static Awards._speech Push(Awards._speech speech)
        {
            Awards._speech previousSpeech = currentSpeech;
            currentSpeech = speech;
            return previousSpeech;
        }

        internal static void Restore(Awards._speech previousSpeech)
        {
            currentSpeech = previousSpeech;
        }

        internal static bool IsCurrent(Awards._speech speech)
        {
            return speech != null && object.ReferenceEquals(currentSpeech, speech);
        }
    }

    internal sealed class AwardSpeechDeliveryPatchState
    {
        public bool ContextPushed;
        public Awards._speech PreviousSpeech;
    }

    /// <summary>
    /// Marks only the actual awards thanks-dialogue execution as a delivered-speech context.
    /// </summary>
    [HarmonyPatch(typeof(vn_actions), "DoCustom")]
    internal static class vn_actions_DoCustom_IMDataCoreAwardSpeechContext_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(string formula, out AwardSpeechDeliveryPatchState __state)
        {
            __state = new AwardSpeechDeliveryPatchState();
            if (!string.Equals(formula, CoreConstants.AwardSpeechSoloThanksFormula, StringComparison.Ordinal) &&
                !string.Equals(formula, CoreConstants.AwardSpeechGroupThanksFormula, StringComparison.Ordinal))
            {
                return;
            }

            Awards._speech selectedSpeech = Award_Speeches_Popup.SelectedSpeech;
            if (selectedSpeech == null)
            {
                return;
            }

            __state.PreviousSpeech = AwardSpeechDeliveryContext.Push(selectedSpeech);
            __state.ContextPushed = true;
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception, AwardSpeechDeliveryPatchState __state)
        {
            if (__state != null && __state.ContextPushed)
            {
                AwardSpeechDeliveryContext.Restore(__state.PreviousSpeech);
            }

            return __exception;
        }
    }

    /// <summary>
    /// Passively observes the result vanilla (or a compatibility repair) already resolved.
    /// </summary>
    [HarmonyPatch(typeof(Awards._speech), nameof(Awards._speech.GetThanks))]
    internal static class Awards_speech_GetThanks_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Awards._speech __instance, Date_GroupTalk._message._category __result)
        {
            if (!AwardSpeechDeliveryContext.IsCurrent(__instance))
            {
                return;
            }

            IMDataCoreController.Instance.CaptureAwardSpeechDelivered(__instance, __result);
        }
    }
}
