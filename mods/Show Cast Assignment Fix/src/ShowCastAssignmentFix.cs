using System.Collections.Generic;
using HarmonyLib;

namespace ShowCastAssignmentFix
{
    /// <summary>
    /// Enforces the one-idol-per-slot rule for a permanent radio, internet, or TV-show cast.
    /// Vanilla has a UI-side availability check, but it can be bypassed and the saved show data
    /// accepts duplicate idol references.  Keep the first slot and clear later duplicates.
    /// </summary>
    internal static class PermanentCastGuard
    {
        internal static bool IsPermanentCast(Shows._show show)
        {
            return show != null && show.castType == Shows._show._castType.permanentCast;
        }

        internal static bool IsSameIdol(data_girls.girls first, data_girls.girls second)
        {
            return first != null && second != null && (first == second || first.id == second.id);
        }

        internal static bool ContainsOtherSlot(data_girls.girls[] slots, int selectedSlot, data_girls.girls girl)
        {
            if (slots == null || girl == null)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (i != selectedSlot && IsSameIdol(slots[i], girl))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool Normalize(data_girls.girls[] slots)
        {
            if (slots == null)
            {
                return false;
            }

            bool changed = false;
            HashSet<int> assignedIdols = new HashSet<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                data_girls.girls girl = slots[i];
                if (girl == null)
                {
                    continue;
                }

                if (!assignedIdols.Add(girl.id))
                {
                    slots[i] = null;
                    changed = true;
                }
            }

            return changed;
        }

        internal static void DeduplicateResult(List<data_girls.girls> cast)
        {
            if (cast == null)
            {
                return;
            }

            HashSet<int> assignedIdols = new HashSet<int>();
            for (int i = 0; i < cast.Count; i++)
            {
                data_girls.girls girl = cast[i];
                if (girl == null || !assignedIdols.Add(girl.id))
                {
                    cast.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    /// <summary>
    /// Last-line UI guard. If a stale selectable button tries to set a girl who occupies another
    /// permanent-cast slot, leave the selection panel open and reject the duplicate.
    /// </summary>
    [HarmonyPatch(typeof(Show_Popup), nameof(Show_Popup.SetGirl))]
    internal static class ShowPopupSetGirlDuplicateGuardPatch
    {
        private static bool Prefix(data_girls.girls girl, data_girls.girls[] ___girls, int ___selectedGirlSlot)
        {
            return !PermanentCastGuard.ContainsOtherSlot(___girls, ___selectedGirlSlot, girl);
        }
    }

    /// <summary>
    /// Cleans a legacy duplicate cast before its show editor renders it.
    /// </summary>
    [HarmonyPatch(typeof(Show_Popup), nameof(Show_Popup.Reset))]
    internal static class ShowPopupResetCastNormalizationPatch
    {
        private static void Prefix(object[] __args)
        {
            if (__args == null || __args.Length == 0)
            {
                return;
            }

            Shows._show show = __args[0] as Shows._show;
            if (PermanentCastGuard.IsPermanentCast(show))
            {
                PermanentCastGuard.Normalize(show.girls);
            }
        }
    }

    /// <summary>
    /// Defends creation and editing against direct calls that bypass the selection UI.
    /// </summary>
    [HarmonyPatch(typeof(Show_Popup), "NewShow")]
    internal static class ShowPopupNewShowCastNormalizationPatch
    {
        private static void Prefix(Shows._show._castType? ___castType, data_girls.girls[] ___girls)
        {
            if (___castType.HasValue && ___castType.Value == Shows._show._castType.permanentCast)
            {
                PermanentCastGuard.Normalize(___girls);
            }
        }
    }

    [HarmonyPatch(typeof(Show_Popup), "SaveShow")]
    internal static class ShowPopupSaveShowCastNormalizationPatch
    {
        private static void Prefix(Shows._show._castType? ___castType, data_girls.girls[] ___girls)
        {
            if (___castType.HasValue && ___castType.Value == Shows._show._castType.permanentCast)
            {
                PermanentCastGuard.Normalize(___girls);
            }
        }
    }

    /// <summary>
    /// Existing saves or another mod can still provide malformed show data. Normalize it before an
    /// episode performs any fame, fan, stamina, training, or payout calculations.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.NewEpisode))]
    internal static class ShowNewEpisodeCastNormalizationPatch
    {
        private static void Prefix(Shows._show __instance)
        {
            if (PermanentCastGuard.IsPermanentCast(__instance))
            {
                PermanentCastGuard.Normalize(__instance.girls);
            }
        }
    }

    /// <summary>
    /// Firing or graduating a cast member can populate more than one slot through the vanilla
    /// replacement path; normalize the resulting permanent cast immediately.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.RemoveGirl))]
    internal static class ShowRemoveGirlCastNormalizationPatch
    {
        private static void Postfix(Shows._show __instance)
        {
            if (PermanentCastGuard.IsPermanentCast(__instance))
            {
                PermanentCastGuard.Normalize(__instance.girls);
            }
        }
    }

    /// <summary>
    /// Safety net for code that reads a malformed permanent cast before the next episode/editor
    /// pass. Gameplay consumers receive every idol at most once.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.GetCast))]
    internal static class ShowGetCastDeduplicationPatch
    {
        private static void Postfix(Shows._show __instance, ref List<data_girls.girls> __result)
        {
            if (PermanentCastGuard.IsPermanentCast(__instance))
            {
                PermanentCastGuard.DeduplicateResult(__result);
            }
        }
    }
}
