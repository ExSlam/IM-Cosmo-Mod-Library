using System;
using HarmonyLib;
using UnityEngine;

namespace SinglesMarketingCheckboxFix
{
    /// <summary>
    /// SingleInDevelopmentButton.UpdateParam hides the Marketing checklist row when a
    /// single has no marketing selected, but vanilla never explicitly re-enables that same
    /// row when the rendered single does have marketing. This is visible after tab/list
    /// refreshes because the row can inherit an inactive state while the underlying single
    /// data remains valid.
    /// </summary>
    internal static class MarketingCheckboxVisibility
    {
        internal static void Reconcile(SingleInDevelopmentButton button)
        {
            if (button == null)
            {
                return;
            }

            Reconcile(button, button.stars_marketing, button.single != null
                ? button.single.GetParam(singles._single._param._type.marketing)
                : null);
        }

        internal static void Reconcile(
            SingleInDevelopmentButton button,
            GameObject targetObj,
            singles._single._param param)
        {
            if (button == null || targetObj == null || param == null ||
                param.type != singles._single._param._type.marketing)
            {
                return;
            }

            bool hasMarketing = button.single != null &&
                                button.single.marketing != null &&
                                button.single.marketing.Count > 0;
            if (targetObj.activeSelf != hasMarketing)
            {
                targetObj.SetActive(hasMarketing);
            }
        }

        internal static void ReconcileVisibleSingleButtons()
        {
            foreach (SingleInDevelopmentButton button in UnityEngine.Object.FindObjectsOfType<SingleInDevelopmentButton>())
            {
                Reconcile(button);
            }
        }
    }

    [HarmonyPatch(typeof(SingleInDevelopmentButton), "UpdateParam",
        new Type[] { typeof(GameObject), typeof(singles._single._param) })]
    internal static class SingleInDevelopmentButtonUpdateParamPatch
    {
        private static void Postfix(
            SingleInDevelopmentButton __instance,
            GameObject targetObj,
            singles._single._param param)
        {
            MarketingCheckboxVisibility.Reconcile(__instance, targetObj, param);
        }
    }

    [HarmonyPatch(typeof(SingleInDevelopmentButton), nameof(SingleInDevelopmentButton.SetSingle))]
    internal static class SingleInDevelopmentButtonSetSinglePatch
    {
        private static void Postfix(SingleInDevelopmentButton __instance)
        {
            MarketingCheckboxVisibility.Reconcile(__instance);
        }
    }

    [HarmonyPatch(typeof(Tabs_Manager), nameof(Tabs_Manager.OpenTab))]
    internal static class TabsManagerOpenTabPatch
    {
        private static void Postfix(Tabs_Manager._tab._type Type)
        {
            if (Type == Tabs_Manager._tab._type.singles)
            {
                MarketingCheckboxVisibility.ReconcileVisibleSingleButtons();
            }
        }
    }
}
