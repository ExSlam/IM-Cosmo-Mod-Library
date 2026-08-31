using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A24 patches the non-pure getter itself so every vanilla delivery call site sees
    /// one occurrence-stable result. The first call runs vanilla unchanged; later calls
    /// for that same her_choice _speech skip the original and return the cached result.
    /// </summary>
    [HarmonyPatch]
    internal static class AwardHerChoice_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Awards._speech),
                nameof(Awards._speech.GetThanks),
                Type.EmptyTypes);

            if (method == null ||
                method.ReturnType != typeof(Date_GroupTalk._message._category))
            {
                AwardHerChoicePatchHealth.ReportFailure(
                    "Awards._speech.GetThanks() could not be resolved with the audited no-argument category-returning signature");
                throw new MissingMethodException(
                    typeof(Awards._speech).FullName,
                    nameof(Awards._speech.GetThanks));
            }

            AwardHerChoicePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static bool Prefix(
            Awards._speech __instance,
            ref Date_GroupTalk._message._category __result)
        {
            Date_GroupTalk._message._category cached;
            if (!AwardHerChoiceRepair.TryUseCachedResolution(__instance, out cached))
            {
                return true;
            }

            __result = cached;
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(
            Awards._speech __instance,
            Date_GroupTalk._message._category __result)
        {
            AwardHerChoiceRepair.ObserveOriginalResolution(__instance, __result);
        }
    }
}
