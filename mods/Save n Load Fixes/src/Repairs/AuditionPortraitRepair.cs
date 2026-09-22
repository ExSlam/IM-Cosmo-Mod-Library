using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Keeps audition portrait presentation vanilla-identical while preventing a large audition from
    /// launching every full-size portrait load at once. A portrait is retried once; if it still cannot
    /// resolve, that audition candidate is completely replaced with a newly generated vanilla idol.
    /// </summary>
    internal static class AuditionPortraitRepair
    {
        private const int MaxConcurrentPortraitLoads = 5;
        private const float AttemptTimeoutSeconds = 6f;
        private const int MaxPortraitRetriesBeforeReplacement = 1;
        private const int MaxVanillaReplacementAttempts = 3;

        private enum LoadStatus
        {
            Queued,
            Loading,
            Complete,
            Failed
        }

        private sealed class PopupState
        {
            internal Popup_Audition Popup;
            internal Auditions.data Data;
            internal readonly List<LoadItem> Items = new List<LoadItem>();
        }

        private sealed class LoadItem
        {
            internal Audition_Closed_Card Card;
            internal Auditions.data._girl Candidate;
            internal LoadStatus Status;
            internal float AttemptStartedAt;
            internal int RetryCount;
            internal int ReplacementCount;
            internal Coroutine RunningCoroutine;
        }

        private static readonly Dictionary<int, PopupState> popupStates = new Dictionary<int, PopupState>();
        private static readonly Dictionary<Auditions.data._girl, PopupState> candidateOwners =
            new Dictionary<Auditions.data._girl, PopupState>();

        private static readonly FieldInfo closedCardGirlField =
            AccessTools.Field(typeof(Audition_Closed_Card), "Girl");
        private static readonly FieldInfo textureAssetsField =
            AccessTools.Field(typeof(data_girls_textures), "textureAssets");

        private const string TargetedAuditionsHarmonyId = "com.tel.customauditions";

        internal static bool TargetedAuditionsOwnsPortraitHardening()
        {
            try
            {
                MethodBase method = AccessTools.Method(typeof(Audition_Closed_Card), "Set");
                Patches patches = method != null ? Harmony.GetPatchInfo(method) : null;
                return patches != null && patches.Owners != null && patches.Owners.Contains(TargetedAuditionsHarmonyId);
            }
            catch
            {
                return false;
            }
        }

        internal static void BeginPopup(Popup_Audition popup, Auditions.data data)
        {
            if (TargetedAuditionsOwnsPortraitHardening())
            {
                return;
            }

            if (popup == null)
            {
                return;
            }

            EndPopup(popup);

            // Story/custom auditions have hand-authored candidates and must remain completely untouched.
            if (data == null || data.Type == Auditions.type.custom || data.Girls == null || data.Girls.Count == 0)
            {
                return;
            }

            PopupState state = new PopupState
            {
                Popup = popup,
                Data = data
            };

            popupStates[popup.GetInstanceID()] = state;
            foreach (Auditions.data._girl candidate in data.Girls)
            {
                if (candidate != null)
                {
                    candidateOwners[candidate] = state;
                }
            }
        }

        internal static void EndPopup(Popup_Audition popup)
        {
            if (popup == null)
            {
                return;
            }

            int id = popup.GetInstanceID();
            if (!popupStates.TryGetValue(id, out PopupState state))
            {
                return;
            }

            foreach (LoadItem item in state.Items)
            {
                StopRunningAttempt(item);
                if (item.Candidate != null)
                {
                    candidateOwners.Remove(item.Candidate);
                }
            }

            foreach (Auditions.data._girl candidate in state.Data?.Girls ?? Enumerable.Empty<Auditions.data._girl>())
            {
                if (candidate != null)
                {
                    candidateOwners.Remove(candidate);
                }
            }

            popupStates.Remove(id);
        }

        /// <summary>
        /// Returns true when this card belongs to a normal audition and its vanilla full portrait load
        /// has been queued by this hardening layer. The caller should skip Audition_Closed_Card.Set's
        /// original body in that case.
        /// </summary>
        internal static bool TryQueueCard(Audition_Closed_Card card, Auditions.data._girl candidate)
        {
            if (TargetedAuditionsOwnsPortraitHardening())
            {
                return false;
            }

            if (card == null || candidate == null || candidate.girl == null || card.Portrait == null)
            {
                return false;
            }

            if (!candidateOwners.TryGetValue(candidate, out PopupState state) || state == null)
            {
                return false;
            }

            // This is the only state assignment performed by vanilla Set before it starts the portrait
            // coroutine. Preserve it exactly so click/reveal behavior remains vanilla-identical.
            closedCardGirlField?.SetValue(card, candidate);

            Image image = card.Portrait.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
            }

            state.Items.Add(new LoadItem
            {
                Card = card,
                Candidate = candidate,
                Status = LoadStatus.Queued
            });

            Pump(state);
            return true;
        }

        internal static void UpdateReadiness(Popup_Audition popup, ref bool result)
        {
            if (TargetedAuditionsOwnsPortraitHardening())
            {
                EndPopup(popup);
                return;
            }

            if (popup == null || !popupStates.TryGetValue(popup.GetInstanceID(), out PopupState state))
            {
                return;
            }

            RefreshAttempts(state);
            Pump(state);

            if (AllCardPortraitsReady(state))
            {
                result = true;
            }
        }

        private static void RefreshAttempts(PopupState state)
        {
            float now = Time.unscaledTime;
            foreach (LoadItem item in state.Items.ToArray())
            {
                if (item.Status != LoadStatus.Loading)
                {
                    continue;
                }

                if (HasPortrait(item))
                {
                    item.Status = LoadStatus.Complete;
                    item.RunningCoroutine = null;
                    continue;
                }

                if (now - item.AttemptStartedAt < AttemptTimeoutSeconds)
                {
                    continue;
                }

                StopRunningAttempt(item);

                if (item.RetryCount < MaxPortraitRetriesBeforeReplacement)
                {
                    item.RetryCount++;
                    item.Status = LoadStatus.Queued;
                    Debug.LogWarning(
                        "[Save n Load Fixes] Full audition portrait timed out; retrying the same candidate once.");
                    continue;
                }

                if (item.ReplacementCount >= MaxVanillaReplacementAttempts)
                {
                    item.Status = LoadStatus.Failed;
                    Debug.LogError(
                        "[Save n Load Fixes] An audition portrait still cannot be loaded after vanilla replacement attempts. " +
                        "The popup will remain waiting rather than display the wrong idol portrait.");
                    continue;
                }

                if (ReplaceWithVanillaCandidate(state, item))
                {
                    item.ReplacementCount++;
                    item.RetryCount = 0;
                    item.Status = LoadStatus.Queued;
                    Debug.LogWarning(
                        "[Save n Load Fixes] Replaced an unrenderable audition candidate with a newly generated vanilla idol.");
                }
                else
                {
                    item.Status = LoadStatus.Failed;
                    Debug.LogError(
                        "[Save n Load Fixes] Could not generate a vanilla replacement for an unrenderable audition candidate.");
                }
            }
        }

        private static void Pump(PopupState state)
        {
            int active = state.Items.Count(item => item.Status == LoadStatus.Loading);
            if (active >= MaxConcurrentPortraitLoads)
            {
                return;
            }

            data_girls_textures textures = GetGirlTextures();
            if (textures == null)
            {
                return;
            }

            foreach (LoadItem item in state.Items)
            {
                if (active >= MaxConcurrentPortraitLoads)
                {
                    break;
                }
                if (item.Status != LoadStatus.Queued || item.Card == null || item.Candidate?.girl == null || item.Card.Portrait == null)
                {
                    continue;
                }

                Image image = item.Card.Portrait.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = null;
                }

                // Run the exact same IEnumerator used by vanilla _setFullPortrait(). We retain the Coroutine
                // handle only so a genuinely stuck attempt can be stopped before retry/replacement.
                IEnumerator routine = textures.setPortrait(
                    item.Candidate.girl,
                    new List<GameObject> { item.Card.Portrait },
                    0f,
                    null);

                item.RunningCoroutine = textures.StartCoroutine(routine);
                item.AttemptStartedAt = Time.unscaledTime;
                item.Status = LoadStatus.Loading;
                active++;
            }
        }

        private static bool ReplaceWithVanillaCandidate(PopupState state, LoadItem item)
        {
            try
            {
                data_girls dataGirls = GetDataGirls();
                data_girls_textures._textureAsset vanillaBody = GetRandomVanillaBody();
                if (dataGirls == null || vanillaBody == null)
                {
                    return false;
                }

                data_girls.girls replacement = dataGirls.GenerateGirl(
                    true,
                    Auditions.data._girl._type.normal,
                    vanillaBody);
                if (replacement == null)
                {
                    return false;
                }

                // Mutate the existing wrapper rather than swapping wrapper objects. Audition_Closed_Card stores
                // that wrapper privately, so the reveal card, stats, name, traits and textures all now refer to
                // the replacement idol with no stale unique-idol details left behind.
                item.Candidate.girl = replacement;
                item.Candidate.type = DetermineVanillaRarity(replacement, vanillaBody);
                item.Candidate.CardObject = item.Card != null ? item.Card.gameObject : null;

                if (item.Card != null)
                {
                    closedCardGirlField?.SetValue(item.Card, item.Candidate);
                    Image image = item.Card.Portrait != null ? item.Card.Portrait.GetComponent<Image>() : null;
                    if (image != null)
                    {
                        image.sprite = null;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Save n Load Fixes] Vanilla audition-candidate replacement failed:\n" + ex);
                return false;
            }
        }

        private static Auditions.data._girl._type DetermineVanillaRarity(
            data_girls.girls girl,
            data_girls_textures._textureAsset body)
        {
            if (body != null && !string.IsNullOrEmpty(body.Value))
            {
                return body.GetRarity();
            }
            if (girl != null && girl.IsOP())
            {
                return Auditions.data._girl._type.platinum;
            }
            return Auditions.data._girl._type.normal;
        }

        private static data_girls_textures._textureAsset GetRandomVanillaBody()
        {
            List<data_girls_textures._textureAsset> assets =
                textureAssetsField?.GetValue(null) as List<data_girls_textures._textureAsset>;
            if (assets == null)
            {
                return null;
            }

            List<data_girls_textures._textureAsset> vanillaBodies = assets.Where(asset =>
                asset != null &&
                asset.type == data_girls_textures._spriteType.body &&
                !asset.IsModded() &&
                asset.CanBeHired()).ToList();

            if (vanillaBodies.Count == 0)
            {
                return null;
            }

            return vanillaBodies[UnityEngine.Random.Range(0, vanillaBodies.Count)];
        }

        private static bool HasPortrait(LoadItem item)
        {
            if (item?.Card == null || item.Card.Portrait == null)
            {
                return false;
            }
            Image image = item.Card.Portrait.GetComponent<Image>();
            return image != null && image.sprite != null;
        }

        private static bool AllCardPortraitsReady(PopupState state)
        {
            if (state.Popup == null || state.Popup.Cards_Container == null)
            {
                return false;
            }

            foreach (Transform child in state.Popup.Cards_Container.transform)
            {
                Audition_Closed_Card card = child.GetComponent<Audition_Closed_Card>();
                if (card == null || card.Portrait == null)
                {
                    continue;
                }
                Image image = card.Portrait.GetComponent<Image>();
                if (image == null || image.sprite == null)
                {
                    return false;
                }
            }
            return true;
        }

        private static void StopRunningAttempt(LoadItem item)
        {
            if (item?.RunningCoroutine == null)
            {
                return;
            }

            data_girls_textures textures = GetGirlTextures();
            if (textures != null)
            {
                textures.StopCoroutine(item.RunningCoroutine);
            }
            item.RunningCoroutine = null;
        }

        private static data_girls GetDataGirls()
        {
            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            return main?.Data != null ? main.Data.GetComponent<data_girls>() : null;
        }

        private static data_girls_textures GetGirlTextures()
        {
            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            return main?.Data != null ? main.Data.GetComponent<data_girls_textures>() : null;
        }
    }

    [HarmonyPatch(typeof(Popup_Audition), "Set", new Type[] { typeof(Auditions.data), typeof(bool) })]
    internal static class Popup_Audition_Set_PortraitRepair
    {
        private static void Prefix(Popup_Audition __instance, Auditions.data _data)
        {
            AuditionPortraitRepair.BeginPopup(__instance, _data);
        }

        private static Exception Finalizer(Popup_Audition __instance, Exception __exception)
        {
            if (__exception != null)
            {
                AuditionPortraitRepair.EndPopup(__instance);
            }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Popup_Audition), "Reset")]
    internal static class Popup_Audition_Reset_PortraitRepair
    {
        private static void Postfix(Popup_Audition __instance)
        {
            AuditionPortraitRepair.EndPopup(__instance);
        }
    }

    [HarmonyPatch(typeof(Popup_Audition), "Close")]
    internal static class Popup_Audition_Close_PortraitRepair
    {
        private static void Prefix(Popup_Audition __instance)
        {
            AuditionPortraitRepair.EndPopup(__instance);
        }
    }

    [HarmonyPatch(typeof(Popup_Audition), "PortraitsLoaded")]
    internal static class Popup_Audition_PortraitsLoaded_PortraitRepair
    {
        private static void Postfix(Popup_Audition __instance, ref bool __result)
        {
            AuditionPortraitRepair.UpdateReadiness(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(Audition_Closed_Card), "Set")]
    internal static class Audition_Closed_Card_Set_PortraitRepair
    {
        private static bool Prefix(Audition_Closed_Card __instance, Auditions.data._girl _girl)
        {
            return !AuditionPortraitRepair.TryQueueCard(__instance, _girl);
        }
    }
}
