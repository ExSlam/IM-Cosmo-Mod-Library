using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace DivorceFix
{
    [HarmonyPatch(typeof(Dating), nameof(Dating.AfterMarriage))]
    internal static class Dating_AfterMarriage_Patch
    {
        private static void Postfix(data_girls.girls Girl)
        {
            if (!Dating.Girl_Quit_Triggered || Dating.Good_Outcome || Girl == null
                || Girl.status != data_girls._status.graduated || Dating.Partners == null)
            {
                return;
            }

            foreach (Dating._partner partner in Dating.Partners)
            {
                if (partner != null && partner.GirlID == Girl.id
                    && partner.Status == Dating._partner._status.married)
                {
                    partner.SetStatus(Dating._partner._status.ended);
                }
            }
        }
    }

    [HarmonyPatch]
    internal static class SaveManager_LoadData_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SaveManager), nameof(SaveManager.LoadData), new[] { typeof(string) });
            yield return AccessTools.Method(typeof(SaveManager), nameof(SaveManager.LoadData), new[] { typeof(bool) });
        }

        private static void Postfix(SaveManager __instance)
        {
            // LoadEvent subscriber order is not guaranteed. Dating.LoadFunction may
            // run before the new roster, player identity or player staff is loaded.
            // Run after the whole load and use only the adopted save's own records.
            SaveManager.SavedData save = __instance.Data;
            if (!SaveManager.CanContinue || save == null
                || Dating.Partners == null || Dating.Partners.Count == 0
                || !ReferenceEquals(Dating.Partners, save.Dating__Partners)
                || save.data_girls__Girls == null || staticVars.PlayerData == null
                || staff.Staff == null || staff.GetPlayer() == null)
            {
                return;
            }

            string template;
            if (Language.Data == null || !Language.Data.TryGetValue("MARRIAGE_BAD_OUTCOME", out template)
                || string.IsNullOrEmpty(template))
            {
                return;
            }

            string badOutcomeText = Language.Insert("MARRIAGE_BAD_OUTCOME", new string[]
            {
                staticVars.PlayerData.GetPlayerName(staticVars._playerData.name_type.full_name, true)
            });

            foreach (Dating._partner partner in Dating.Partners)
            {
                if (partner == null || partner.Status != Dating._partner._status.married)
                {
                    continue;
                }

                data_girls.GirlData girl = save.data_girls__Girls.Find(
                    candidate => candidate != null && candidate.id == partner.GirlID);
                if (girl == null)
                {
                    continue;
                }

                // Old saves have no structured divorce marker. Only the exact known
                // outcome is safe to repair; other languages/custom text stay intact.
                if (girl.status == data_girls._status.graduated
                    && string.Equals(girl.Graduation_Trivia_Text, badOutcomeText, StringComparison.Ordinal))
                {
                    partner.SetStatus(Dating._partner._status.ended);
                }
            }
        }
    }
}
