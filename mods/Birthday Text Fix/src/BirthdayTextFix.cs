using System;
using HarmonyLib;

namespace BirthdayTextFix
{
    /// <summary>
    /// Vanilla inserts the idol's age into the English birthday-title localization text,
    /// but the ordinal suffix in that text is static. Correct only that suffix after
    /// vanilla has rendered the popup, leaving all non-English languages untouched.
    /// </summary>
    [HarmonyPatch(typeof(Birthday_Popup), "RenderTitle")]
    internal static class BirthdayPopupRenderTitlePatch
    {
        private static readonly string[] EnglishOrdinalSuffixes = { "st", "nd", "rd", "th" };

        private static void Postfix(Birthday_Popup __instance)
        {
            if (__instance == null || __instance.Girl == null || __instance.Title == null)
            {
                return;
            }

            if (staticVars.Settings == null || staticVars.Settings.Language != "en")
            {
                return;
            }

            int age = __instance.Girl.GetAge();
            string ageText = age.ToString();
            string birthdayText = Language.Insert("BD__TITLE_2", ageText);
            string correctedBirthdayText = CorrectOrdinalSuffix(birthdayText, age, ageText);

            if (ReferenceEquals(correctedBirthdayText, birthdayText)
                || string.Equals(correctedBirthdayText, birthdayText, StringComparison.Ordinal))
            {
                return;
            }

            ExtensionMethods.SetText(
                __instance.Title,
                Language.Insert("BD__TITLE_1", __instance.Girl.GetName(true))
                + " "
                + correctedBirthdayText);
        }

        internal static string CorrectOrdinalSuffix(string birthdayText, int age, string ageText)
        {
            if (string.IsNullOrEmpty(birthdayText) || string.IsNullOrEmpty(ageText))
            {
                return birthdayText;
            }

            int ageIndex = birthdayText.IndexOf(ageText, StringComparison.Ordinal);
            if (ageIndex < 0)
            {
                return birthdayText;
            }

            int suffixIndex = ageIndex + ageText.Length;
            if (suffixIndex + 2 > birthdayText.Length)
            {
                return birthdayText;
            }

            string currentSuffix = birthdayText.Substring(suffixIndex, 2);
            if (!IsEnglishOrdinalSuffix(currentSuffix))
            {
                return birthdayText;
            }

            string correctSuffix = ExtensionMethods.GetOrdinal(age);
            if (string.Equals(currentSuffix, correctSuffix, StringComparison.Ordinal))
            {
                return birthdayText;
            }

            return birthdayText.Substring(0, suffixIndex)
                + correctSuffix
                + birthdayText.Substring(suffixIndex + 2);
        }

        private static bool IsEnglishOrdinalSuffix(string suffix)
        {
            for (int i = 0; i < EnglishOrdinalSuffixes.Length; i++)
            {
                if (string.Equals(suffix, EnglishOrdinalSuffixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
