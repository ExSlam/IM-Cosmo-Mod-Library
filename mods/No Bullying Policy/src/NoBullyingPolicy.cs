using UnityEngine;
using HarmonyLib;
using ModLocalizationSystem;

namespace NoBullyingPolicyMod
{
    internal static class C
    {
        // Numeric enum values keep the policy data loadable without modifying the game's enum definitions.
        internal const int PolicyTypeBullying = 100;
        internal const int PolicyValueDefault = 1001;
        internal const int PolicyValueDisabled = 1002;

        internal const string RelationshipsTargetMethod = "Do_Bullying";
        internal const string PolicyObjName = "Policy_Bullying";
        internal const string PolicyStartMethodName = "Start";
    }

    // 1. Localization Hook: Translates the JSON keys into text right after the policy loads
    [HarmonyPatch(typeof(policies.value), nameof(policies.value.SetData))]
    internal static class PolicyValue_SetData_Patch
    {
        private static void Postfix(policies.value __instance)
        {
            // Only attempt to translate our custom policy to prevent interfering with base game
            if ((int)__instance.Type == C.PolicyTypeBullying)
            {
                if (!string.IsNullOrEmpty(__instance.Title))
                {
                    __instance.Title = ModLocalization.GetRaw(__instance.Title);
                }
                
                if (!string.IsNullOrEmpty(__instance.Tooltip_Value))
                {
                    __instance.Tooltip_Value = ModLocalization.GetRaw(__instance.Tooltip_Value);
                }
            }
        }
    }

    // 2. Prevent new bullying
    [HarmonyPatch(typeof(Relationships._clique), nameof(Relationships._clique.AddBulliedGirl))]
    internal static class Clique_AddBulliedGirl_Patch
    {
        private static bool Prefix()
        {
            // Retrieve our custom policy using the integer
            policies.value activePolicy = policies.GetSelectedPolicyValue((policies._type)C.PolicyTypeBullying);
            
            // 1002 is the "No Bullying" option.
            if (activePolicy != null && (int)activePolicy.Value == C.PolicyValueDisabled)
            {
                // Skip the original method, preventing new girls from being targeted
                return false; 
            }
            return true;
        }
    }

    // 3. Stop ongoing bullying and prevent weekly penalties
    [HarmonyPatch(typeof(Relationships), C.RelationshipsTargetMethod)]
    internal static class Relationships_Do_Bullying_Patch
    {
        private static bool Prefix()
        {
            policies.value activePolicy = policies.GetSelectedPolicyValue((policies._type)C.PolicyTypeBullying);

            if (activePolicy != null && (int)activePolicy.Value == C.PolicyValueDisabled)
            {
                // Loop through all cliques and forcefully end any ongoing bullying
                foreach (Relationships._clique clique in Relationships.Cliques)
                {
                    // Iterate backward because StopBullying modifies the list
                    for (int i = clique.Bullied_Girls.Count - 1; i >= 0; i--)
                    {
                        clique.StopBullying(clique.Bullied_Girls[i]);
                    }
                }
                
                // Skip the original method so no weekly mental stamina damage is applied
                return false; 
            }
            
            // If the policy is set to "Default", let the base game handle the bullying
            return true; 
        }
    }

    [HarmonyPatch(typeof(Policy), C.PolicyStartMethodName)]
    internal static class Policy_Start_UI_Injection_Patch
    {
        private static bool isInjecting;

        private static void Postfix(Policy __instance)
        {
            TryInjectPolicyRow(__instance);
        }

        private static void TryInjectPolicyRow(Policy template)
        {
            if (isInjecting || template == null || (int)template.type == C.PolicyTypeBullying)
            {
                return;
            }

            Transform container = template.transform.parent;
            if (container == null || HasBullyingPolicyRow(container))
            {
                return;
            }

            isInjecting = true;
            try
            {
                GameObject newPolicyObj = Object.Instantiate(template.gameObject, container);
                newPolicyObj.name = C.PolicyObjName;
                newPolicyObj.transform.SetAsLastSibling();

                Policy policyScript = newPolicyObj.GetComponent<Policy>();
                if (policyScript != null)
                {
                    policyScript.type = (policies._type)C.PolicyTypeBullying;
                }
            }
            finally
            {
                isInjecting = false;
            }
        }

        private static bool HasBullyingPolicyRow(Transform container)
        {
            Policy[] policyRows = container.GetComponentsInChildren<Policy>(true);
            for (int i = 0; i < policyRows.Length; i++)
            {
                Policy row = policyRows[i];
                if (row != null && ((int)row.type == C.PolicyTypeBullying || row.gameObject.name == C.PolicyObjName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
