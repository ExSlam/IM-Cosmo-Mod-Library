using HarmonyLib;

namespace DivorceFix
{
    [HarmonyPatch(typeof(Dating), nameof(Dating.AfterMarriage))]
    internal static class Dating_AfterMarriage_Patch
    {
        private static void Postfix(data_girls.girls Girl)
        {
            if (!Dating.Girl_Quit_Triggered || Dating.Good_Outcome || Girl == null)
            {
                return;
            }

            Dating._partner partner = Girl.GetDatingData();
            if (partner == null)
            {
                return;
            }

            if (partner.Status == Dating._partner._status.married)
            {
                partner.SetStatus(Dating._partner._status.ended);
            }
        }
    }

    [HarmonyPatch(typeof(Dating), "LoadFunction")]
    internal static class Dating_LoadFunction_Patch
    {
        private static void Postfix()
        {
            if (Dating.Partners == null || Dating.Partners.Count == 0 || staticVars.PlayerData == null)
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

                data_girls.girls girl = partner.Girl;
                if (girl == null)
                {
                    continue;
                }

                // Repair only known legacy bugged saves: bad divorce outcome stored as still married.
                if (girl.status == data_girls._status.graduated && girl.Graduation_Trivia_Text == badOutcomeText)
                {
                    partner.SetStatus(Dating._partner._status.ended);
                }
            }
        }
    }
}
