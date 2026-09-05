using System;
using HarmonyLib;

namespace IMDataCore
{
    [HarmonyPatch(typeof(Scenes), nameof(Scenes.Set), new Type[] { typeof(data_dialogues._dialogue) })]
    internal static class Scenes_Set_IMDataCoreSubstoryScene_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            data_dialogues._dialogue res_dialogue,
            out SubstoryScenePresentationSnapshot __state)
        {
            __state = IMDataCoreController.Instance
                .CreateSubstoryScenePresentationSnapshot(res_dialogue);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            data_dialogues._dialogue res_dialogue,
            SubstoryScenePresentationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSubstoryScenePresented(
                res_dialogue,
                __state);
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.SubstoryFinish))]
    internal static class agency_room_SubstoryFinish_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            agency._room __instance,
            out SubstorySceneCompletionSnapshot __state)
        {
            __state = IMDataCoreController.Instance
                .CreateSubstorySceneCompletionSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SubstorySceneCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSubstorySceneCompleted(__state);
        }
    }
}
