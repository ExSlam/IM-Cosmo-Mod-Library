using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Vanilla Event_Overlord.LoadFunction() does only Reset(), which replaces the
    /// pacing anchor with StartDate. Restore N08 immediately after that reset.
    /// </summary>
    [HarmonyPatch]
    internal static class EventOverlordLoadLatestEvent_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Event_Overlord),
                nameof(Event_Overlord.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                EventOverlordLatestEventPatchHealth.ReportFailure(
                    "Event_Overlord.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Event_Overlord).FullName, "LoadFunction");
            }

            EventOverlordLatestEventPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            EventOverlordLatestEventRepair.RestoreAfterVanillaLoad();
        }
    }
}
