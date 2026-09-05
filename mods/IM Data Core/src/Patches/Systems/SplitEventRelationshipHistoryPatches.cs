using System;
using System.Reflection;
using HarmonyLib;

namespace IMDataCore
{
    /// <summary>
    /// Observes only true runtime pair creation and preserves the Initialize()-chosen
    /// relationship Dynamic. Load reconstruction is filtered by the controller.
    /// </summary>
    [HarmonyPatch(typeof(Relationships), nameof(Relationships.GetRelationship), new Type[] { typeof(data_girls.girls), typeof(data_girls.girls) })]
    internal static class Relationships_GetRelationship_IMDataCoreHistory_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(data_girls.girls Girl1, data_girls.girls Girl2, out bool __state)
        {
            __state = IMDataCoreController.Instance.RelationshipPairExistsBefore(Girl1, Girl2);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Relationships._relationship __result, bool __state)
        {
            IMDataCoreController.Instance.CaptureIdolRelationshipCreated(__result, __state);
        }
    }

    /// <summary>
    /// Captures the semantic player action around the same private button boundary SNLF
    /// repairs. Prefix must see the untouched relationship; Postfix observes the final
    /// state after any cooperating repair and vanilla's own influence charge.
    /// </summary>
    [HarmonyPatch]
    internal static class Date_Popup_ForceBreakup_IMDataCoreHistory_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Date_Popup), "OnClick_ForceBreakup", Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                throw new MissingMethodException(typeof(Date_Popup).FullName, "OnClick_ForceBreakup");
            }
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore("com.cosmo.savenloadfixes")]
        private static void Prefix(Date_Popup __instance, out PlayerForcedBreakupSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreatePlayerForcedBreakupSnapshot(
                __instance != null ? __instance.Girl : null);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.savenloadfixes")]
        private static void Postfix(PlayerForcedBreakupSnapshot __state)
        {
            IMDataCoreController.Instance.CapturePlayerForcedBreakup(__state);
        }
    }

    /// <summary>
    /// Observes the generated automatic SNS-only carrier. A false MoveNext result is
    /// terminal for this iterator; IsSNS() prevents normal popup events from being closed.
    /// This patch never writes Event_Manager state.
    /// </summary>
    [HarmonyPatch]
    internal static class Event_Manager_SnsOnlyMoveNext_IMDataCoreHistory_Patch
    {
        private static readonly FieldInfo ActiveEventField = ResolveActiveEventField();

        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(Event_Manager).GetNestedType("<OpenPopup>d__46", BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                throw new MissingMemberException(typeof(Event_Manager).FullName, "<OpenPopup>d__46");
            }

            foreach (MethodInfo method in iteratorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                bool nameMatches = string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                {
                    return method;
                }
            }

            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        private static FieldInfo ResolveActiveEventField()
        {
            Type iteratorType = typeof(Event_Manager).GetNestedType("<OpenPopup>d__46", BindingFlags.NonPublic);
            return iteratorType != null
                ? iteratorType.GetField("activeEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : null;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.savenloadfixes")]
        private static void Postfix(object __instance, bool __result)
        {
            if (__result || __instance == null || ActiveEventField == null)
            {
                return;
            }

            Event_Manager._activeEvent activeEvent = ActiveEventField.GetValue(__instance) as Event_Manager._activeEvent;
            if (activeEvent != null && activeEvent.IsSNS())
            {
                IMDataCoreController.Instance.CaptureRandomEventSnsOnlyTerminal(activeEvent);
            }
        }
    }

    /// <summary>
    /// Brackets Event_Popup's template overload so reply variants are observed after
    /// vanilla chooses them, without making another GetReplies() call.
    /// </summary>
    [HarmonyPatch(typeof(Event_Popup), nameof(Event_Popup.Set), new Type[] { typeof(Event_Templates._active_template) })]
    internal static class Event_Popup_SetTemplate_IMDataCoreHistory_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Event_Templates._active_template Active_Template)
        {
            IMDataCoreController.Instance.BeginTemplatePopupPresentation(Active_Template);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Event_Templates._active_template Active_Template)
        {
            IMDataCoreController.Instance.CaptureTemplateEventPresented(Active_Template);
        }
    }

    /// <summary>
    /// Passively records each exact template reply object handed to a rendered button.
    /// </summary>
    [HarmonyPatch(typeof(Event_ReplyButton), nameof(Event_ReplyButton.Set), new Type[] { typeof(Event_Templates._template._reply), typeof(Event_Templates._active_template) })]
    internal static class Event_ReplyButton_SetTemplate_IMDataCoreHistory_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Event_Templates._template._reply _reply, Event_Templates._active_template activeEvent)
        {
            IMDataCoreController.Instance.RecordTemplatePresentedReply(_reply, activeEvent);
        }
    }

    /// <summary>
    /// Captures the selected template reply and before/after semantic variable state.
    /// </summary>
    [HarmonyPatch(typeof(Event_Templates), nameof(Event_Templates.ConcludeEvent), new Type[] { typeof(Event_Templates._template._reply) })]
    internal static class Event_Templates_ConcludeEvent_IMDataCoreHistory_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Event_Templates._template._reply Reply, out TemplateEventConcludeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateTemplateEventConcludeSnapshot(Reply);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(TemplateEventConcludeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureTemplateEventConcluded(__state);
        }
    }
}
