using System;
using System.Reflection;
using HarmonyLib;

namespace DialogueClickRaceFix
{
    /// <summary>
    /// Vanilla clears activeNode before Hide() has finished its fade-out. During that
    /// closing window ShowingDialogue can still be true and the UI can still deliver
    /// another click. OnScreenClick then falls through to Next(), which calls
    /// data_dialogues.GetNextNode(null).
    ///
    /// Do not blanket-block null activeNode clicks: vanilla intentionally supports
    /// null-node OnProceed and Dramatic_CG paths during some transitions. Instead,
    /// suppress only the state that would otherwise reach the final Next() call.
    /// </summary>
    internal static class DialogueClickRaceGuard
    {
        private static readonly FieldInfo OnProceedField =
            AccessTools.Field(typeof(ActiveDialogueController), "OnProceed");

        private static readonly FieldInfo DramaticCgField =
            AccessTools.Field(typeof(ActiveDialogueController), "Dramatic_CG");

        internal static bool ShouldSuppressLateClick(ActiveDialogueController controller)
        {
            if (controller == null || controller.activeNode != null)
            {
                return false;
            }

            // These are legitimate early-return paths in vanilla OnScreenClick.
            if (controller.DisableClick || controller.Transitioning)
            {
                return false;
            }

            // OnProceed and Dramatic_CG are private static vanilla fields. They are
            // intentionally handled before activeNode in OnScreenClick, including
            // during chapter/CG transitions where activeNode can legitimately be null.
            if (OnProceedField == null || DramaticCgField == null)
            {
                // If a game update changes these fields, fail open rather than block
                // an unknown dialogue flow.
                return false;
            }

            Action onProceed = OnProceedField.GetValue(null) as Action;
            bool dramaticCg = (bool)DramaticCgField.GetValue(null);
            if (onProceed != null || dramaticCg)
            {
                return false;
            }

            // Preserve vanilla's "finish animating text" path. If the textbox or
            // component is unexpectedly missing, do not hide that unrelated problem.
            if (controller.textBox == null)
            {
                return false;
            }

            vn_text text = controller.textBox.GetComponent<vn_text>();
            if (text == null || text.animatingText)
            {
                return false;
            }

            // activeNode is null, no transition handler consumed the click, and text
            // is not animating. Vanilla's only remaining action is Next(), which is
            // the unsafe late-click race we are fixing.
            return true;
        }
    }

    [HarmonyPatch(typeof(ActiveDialogueController), nameof(ActiveDialogueController.OnScreenClick))]
    internal static class ActiveDialogueControllerOnScreenClickPatch
    {
        private static bool Prefix(ActiveDialogueController __instance)
        {
            return !DialogueClickRaceGuard.ShouldSuppressLateClick(__instance);
        }
    }
}
