using System;
using System.Collections;
using HarmonyLib;

namespace FasterSkipDialogue
{
    internal static class PatchTargets
    {
        internal const string SkipCoroutineMethodName = "_Skip";
        internal const string ControllerStartMethodName = "Start";
        internal const string DoMetaMethodName = "DoMeta";
        internal const string DoCustomMethodName = "DoCustom";
        internal const string FadeToBlackParameter = "fade_to_black";
        internal const string StopSkippingParameter = "stop_skipping";
    }

    /// <summary>
    /// Keep the vanilla Skip button and OnSkip() implementation, but replace the coroutine it
    /// starts. This preserves the base game's button state/color behavior and compatibility with
    /// patches around OnSkip itself.
    /// </summary>
    [HarmonyPatch(typeof(ActiveDialogueController), PatchTargets.SkipCoroutineMethodName)]
    internal static class ActiveDialogueControllerSkipCoroutinePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ActiveDialogueController __instance, ref IEnumerator __result)
        {
            __result = FasterSkipLoop.Run(__instance);
        }
    }

    /// <summary>
    /// Finish typewriter text immediately while fast skip is running. This avoids needing two
    /// synthetic clicks per message and works for both vanilla and data-driven mod dialogue.
    /// </summary>
    [HarmonyPatch(typeof(vn_text), nameof(vn_text.Set), new Type[] { typeof(string), typeof(bool), typeof(bool) })]
    internal static class VnTextSetFastSkipPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(vn_text __instance)
        {
            if (__instance == null ||
                !ActiveDialogueController.Skip ||
                !ActiveDialogueController.ShowingDialogue ||
                ActiveDialogueController.ShowingPopup)
            {
                return;
            }

            if (__instance.animatingText)
            {
                __instance.StopAnimation();
            }
        }
    }

    /// <summary>
    /// Hide() is the real scene boundary. Disarm skip here so queued/new dialogue cannot inherit
    /// it, while FasterSkipRuntime keeps DOTween accelerated just long enough to finish closing
    /// fades quickly.
    /// </summary>
    [HarmonyPatch(typeof(ActiveDialogueController), nameof(ActiveDialogueController.Hide), new Type[0])]
    internal static class ActiveDialogueControllerHideFastSkipPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ActiveDialogueController __instance, MusicManager ___musicManager)
        {
            FasterSkipRuntime.MarkSceneEnding(__instance);
            SceneMusicCleanup.EndDialogue(__instance, ___musicManager);
        }
    }

    /// <summary>
    /// Set() marks a new top-level dialogue. Vanilla already sets Skip=false here; the prefix also
    /// restores any global DOTween baseline left by the prior scene before setup begins.
    /// Internal instant_transition does not call Set(), so an EroEvents-style segmented cutscene
    /// continues skipping until it really ends.
    /// </summary>
    [HarmonyPatch(
        typeof(ActiveDialogueController),
        nameof(ActiveDialogueController.Set),
        new Type[]
        {
            typeof(data_dialogues._dialogue),
            typeof(bool),
            typeof(float),
            typeof(Substories_Manager._substoryData)
        })]
    internal static class ActiveDialogueControllerSetFastSkipPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ActiveDialogueController __instance)
        {
            FasterSkipRuntime.ResetForNewDialogue(__instance);
        }
    }

    /// <summary>
    /// Defensive scene-level cleanup. A newly initialized controller must never inherit a global
    /// tween multiplier from a controller destroyed by a scene change or ending sequence.
    /// </summary>
    [HarmonyPatch(typeof(ActiveDialogueController), PatchTargets.ControllerStartMethodName)]
    internal static class ActiveDialogueControllerStartFastSkipPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            FasterSkipRuntime.ResetForControllerStart();
            ActiveDialogueController.Skip = false;
        }
    }

    /// <summary>
    /// Vanilla fade_to_black and stop_skipping actions explicitly clear the Skip flag. Faster Skip
    /// promises that an armed session pauses only for player interaction or a true scene end, so
    /// preserve the flag across those actions while still letting every action effect execute.
    /// </summary>
    [HarmonyPatch(
        typeof(vn_actions),
        PatchTargets.DoMetaMethodName,
        new Type[] { typeof(string), typeof(string), typeof(Event_Manager._activeEvent) })]
    internal static class VnActionsDoMetaFastSkipPatch
    {
        private static void Prefix(string parameter, out bool __state)
        {
            bool canClearSkip = string.Equals(parameter, PatchTargets.FadeToBlackParameter, StringComparison.Ordinal) ||
                                string.Equals(parameter, PatchTargets.StopSkippingParameter, StringComparison.Ordinal);

            __state = canClearSkip && FasterSkipRuntime.ShouldProtectSkipState();
        }

        private static void Postfix(bool __state)
        {
            FasterSkipRuntime.RestoreProtectedSkipState(__state);
        }
    }

    /// <summary>
    /// Several vanilla custom VN sequences temporarily force Skip=false while running a cinematic
    /// transition. Preserve an already-armed Faster Skip session unless the custom sequence
    /// actually calls ActiveDialogueController.Hide(), which marks the true scene end.
    /// </summary>
    [HarmonyPatch(typeof(vn_actions), PatchTargets.DoCustomMethodName, new Type[] { typeof(string) })]
    internal static class VnActionsDoCustomFastSkipPatch
    {
        private static void Prefix(out bool __state)
        {
            __state = FasterSkipRuntime.ShouldProtectSkipState();
        }

        private static void Postfix(bool __state)
        {
            FasterSkipRuntime.RestoreProtectedSkipState(__state);
        }
    }
    /// <summary>
    /// MusicManager intentionally blocks StopStorySong() for three seconds after a
    /// story track starts. Fast skip can finish an award/VN scene inside that window,
    /// causing ActiveDialogueController.Hide()->MusicManager.Resume() to leave the
    /// story track playing in normal gameplay. Hide() is already our authoritative
    /// scene boundary, so release the private guard exactly once for the first
    /// StopStorySong() reached after that boundary and otherwise leave vanilla music
    /// behavior untouched.
    /// </summary>
    [HarmonyPatch(
        typeof(MusicManager),
        nameof(MusicManager.StopStorySong),
        new Type[] { typeof(bool) })]
    internal static class MusicManagerStopStorySongFastSkipSceneEndPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref bool ___blockStopping)
        {
            if (FasterSkipRuntime.TryConsumeSceneEndStoryMusicStopGuardRelease())
            {
                ___blockStopping = false;
            }
        }
    }

}
