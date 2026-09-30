using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ModLocalizationSystem;
using CheatsMod.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static partial class IdolTargetCheatPopup
    {
        private const string SelectVictimKey = "ui.bullying.select_victim";
        private const string SelectCliquesKey = "ui.bullying.select_cliques";
        private const string SelectStoppedKey = "ui.bullying.select_stopped";
        private const string VictimInstructionsKey = "ui.bullying.victim_instructions";
        private const string CliqueInstructionsKey = "ui.bullying.clique_instructions";
        private const string StoppedInstructionsKey = "ui.bullying.stopped_instructions";
        private const string VictimLabelKey = "ui.bullying.victim";
        private const string SelectedCliquesKey = "ui.bullying.selected_cliques";
        private const string SelectedStoppedKey = "ui.bullying.selected_stopped";
        private const string NextKey = "ui.bullying.next";
        private const string ConfirmBulliesKey = "ui.bullying.ok";
        private const string BackKey = "ui.bullying.back";
        private const string NoAvailableCliquesKey = "notification.no_available_bully_cliques";
        private const string BullyingAdjustedKey = "notification.bullying_adjusted";
        private const string BullyingSelectionName = "BullyingSelection";
        private const string BullyingFooterName = "BullyingContinue";
        private const string BullyingBackButtonName = "BullyingBack";
        private const string BullyingCancelButtonName = "BullyingCancel";
        private const string BullyingErrorPrefix = "[CheatsMod] Adjust-bullying cheat failed: ";
        private const string NoSelectedCliquesKey = "ui.bullying.none_cliques";
        private const string NoSelectedStoppedKey = "ui.bullying.none_stopped";
        private const string BullyingNamesScrollName = "BullyingSelectionSummary";
        private const string BullyingNamesTextName = "Names";
        private const string NameLineSeparator = "\n";
        private const float BullyingInstructionsHeight = 100f;
        private const float BullyingNamesHeadingOffset = 174f;
        private const float BullyingNamesListOffset = 212f;
        private const float BullyingNamesHeadingHeight = 30f;
        private const int BullyingNamesPadding = 12;
        private const int BullyingVictimStepFooterButtonCount = 2;
        private const int BullyingLaterStepFooterButtonCount = 3;
        private const float BullyingFooterButtonGap = 16f;
        private const byte SelectionOverlayAlpha = 72;
        // Optional No Bullying Policy integration. Everything specific to that mod is
        // discovered at runtime so Cheats Mod has no compile-time dependency on it.
        private const string NoBullyingPolicyAssemblyName = "com.cosmo.nobullyingpolicy";
        private const string NoBullyingPolicyHarmonyOwnerId = "com.cosmo.nobullyingpolicy";
        private const string NoBullyingPolicyConstantsTypeName = "NoBullyingPolicyMod.C";
        private const string NoBullyingPolicyTypeFieldName = "PolicyTypeBullying";
        private const string NoBullyingPolicyDefaultFieldName = "PolicyValueDefault";
        private const string NoBullyingPolicyDisabledFieldName = "PolicyValueDisabled";
        private const string HarmonyTypeName = "HarmonyLib.Harmony";
        private const string HarmonyGetPatchInfoMethodName = "GetPatchInfo";
        private const string HarmonyPatchOwnersMemberName = "Owners";
        private const string HarmonyPatchOwnerPropertyName = "Owner";
        private const string HarmonyPatchOwnerFieldName = "owner";
        private static readonly string[] HarmonyPatchCollectionMemberNames =
        {
            "Prefixes", "Postfixes", "Transpilers", "Finalizers"
        };
        private const string NoBullyingPolicyWarningKey = "notification.bullying_no_bullying_policy_warning";
        private const string NativeBullyingRejectedKey = "notification.bullying_native_rejected";

        private enum BullyingStep
        {
            Victim,
            Cliques,
            StoppedMembers
        }

        private sealed class NativeBullyingStartPreview
        {
            internal bool ThickSkinBlocked;
            internal bool VictimMentalStaminaPenalty;
            internal bool BullyingRemainsActive;
            internal bool CliqueDisbands;
            internal data_girls.girls EjectedDatingMember;
            internal data_girls.girls NewLeader;
            internal readonly List<data_girls.girls> VictimRelationshipPenaltyMembers = new List<data_girls.girls>();
            internal readonly List<data_girls.girls> SkippedVictimRelationshipPenaltyMembers = new List<data_girls.girls>();
            internal readonly List<data_girls.girls> RemainingMembers = new List<data_girls.girls>();
        }

        private static BullyingStep bullyingStep;
        private static Transform bullyingPanel;
        private static TextMeshProUGUI bullyingTitle;
        private static TextMeshProUGUI bullyingPickerLabel;
        private static GameObject bullyingPickerScroll;
        private static Button bullyingBackButton;
        private static Button bullyingCancelButton;
        private static Button bullyingContinueButton;
        private static TextMeshProUGUI bullyingNamesHeading;
        private static TextMeshProUGUI bullyingNamesText;
        private static IMUiScrollViewHandle bullyingNamesScroll;
        private static readonly HashSet<int> selectedStoppedBullyingIds = new HashSet<int>();
        private static readonly List<Relationships._clique> selectedBullyingCliques = new List<Relationships._clique>();
        private static readonly Dictionary<int, GameObject> bullyingHighlights = new Dictionary<int, GameObject>();
        private static readonly Dictionary<Relationships._clique, GameObject> bullyingCliqueHighlights =
            new Dictionary<Relationships._clique, GameObject>();

        internal static void OpenAdjustBullying()
        {
            Open(Mode.AddBullies);
        }

        // Kept as a compatibility alias for older Mod Buttons data and callers.
        internal static void OpenAddBullies()
        {
            OpenAdjustBullying();
        }

        private static string BullyingText(string key)
        {
            return ModLocalization.Get(key, string.Empty);
        }

        private static string GetBullyingHeading()
        {
            if (bullyingStep == BullyingStep.Cliques) return BullyingText(SelectCliquesKey);
            if (bullyingStep == BullyingStep.StoppedMembers) return BullyingText(SelectStoppedKey);
            return BullyingText(SelectVictimKey);
        }

        private static void ResetBullyingSelection()
        {
            bullyingStep = BullyingStep.Victim;
            selectedStoppedBullyingIds.Clear();
            selectedBullyingCliques.Clear();
            bullyingHighlights.Clear();
            bullyingCliqueHighlights.Clear();
            bullyingPanel = null;
            bullyingTitle = null;
            bullyingPickerLabel = null;
            bullyingPickerScroll = null;
            bullyingBackButton = null;
            bullyingCancelButton = null;
            bullyingContinueButton = null;
            bullyingNamesHeading = null;
            bullyingNamesText = null;
            bullyingNamesScroll = null;
        }

        private static bool IsAvailableBullyingIdol(data_girls.girls girl)
        {
            return girl != null
                && data_girls.girl != null
                && data_girls.girl.Contains(girl)
                && girl.Type == data_girls.girls._type.NORMAL
                && girl.status != data_girls._status.graduated;
        }

        private static bool IsEligibleBullyingClique(Relationships._clique clique)
        {
            if (clique == null || Relationships.Cliques == null || !Relationships.Cliques.Contains(clique)
                || clique.Members == null || clique.Members.Count == 0 || !IsAvailableBullyingIdol(selectedGirl))
            {
                return false;
            }
            if (clique.Members.Contains(selectedGirl)) return false;
            foreach (data_girls.girls member in clique.Members)
                if (IsAvailableBullyingIdol(member)) return true;
            return false;
        }

        private static List<Relationships._clique> BuildEligibleBullyingCliques()
        {
            List<Relationships._clique> result = new List<Relationships._clique>();
            if (Relationships.Cliques == null) return result;
            foreach (Relationships._clique clique in Relationships.Cliques)
                if (IsEligibleBullyingClique(clique)) result.Add(clique);
            result.Sort(delegate(Relationships._clique left, Relationships._clique right)
            {
                return string.Compare(SafeGirlName(left == null ? null : left.Leader),
                    SafeGirlName(right == null ? null : right.Leader), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static void SelectExistingBullyingCliques()
        {
            selectedBullyingCliques.Clear();
            foreach (Relationships._clique clique in BuildEligibleBullyingCliques())
                if (clique.IsBullied(selectedGirl)) selectedBullyingCliques.Add(clique);
        }

        private static void LoadExistingStoppedBullyingSelections()
        {
            selectedStoppedBullyingIds.Clear();
            foreach (Relationships._clique clique in selectedBullyingCliques)
            {
                if (!IsEligibleBullyingClique(clique) || !clique.IsBullied(selectedGirl)) continue;
                Relationships._clique._stopped_bullying record = clique.GetStoppedBullying(selectedGirl);
                if (record == null || record.Girls == null) continue;
                foreach (data_girls.girls member in record.Girls)
                    if (IsAvailableBullyingIdol(member) && clique.Members.Contains(member))
                        selectedStoppedBullyingIds.Add(member.id);
            }
        }

        private static List<data_girls.girls> BuildStoppedMemberChoices()
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            foreach (Relationships._clique clique in selectedBullyingCliques)
            {
                if (!IsEligibleBullyingClique(clique)) continue;
                foreach (data_girls.girls member in clique.Members)
                {
                    if (IsAvailableBullyingIdol(member) && member != selectedGirl && !result.Contains(member))
                        result.Add(member);
                }
            }
            result.Sort(delegate(data_girls.girls left, data_girls.girls right)
            {
                return string.Compare(SafeGirlName(left), SafeGirlName(right), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static void CreateBullyingHighlight(GameObject item, data_girls.girls girl)
        {
            GameObject highlight = CreateUIObject(BullyingSelectionName, item.transform);
            Stretch(highlight.GetComponent<RectTransform>());
            Image image = highlight.AddComponent<Image>();
            Color32 selectedColor = mainScript.green32;
            selectedColor.a = SelectionOverlayAlpha;
            image.color = selectedColor;
            image.raycastTarget = false;
            highlight.SetActive(false);
            bullyingHighlights[girl.id] = highlight;
        }

        private static void CreateBullyingFooter(Transform panel)
        {
            bullyingBackButton = CreateButton(
                panel,
                BullyingBackButtonName,
                BullyingText(BackKey),
                CloseButtonWidth,
                CloseButtonHeight,
                ReturnToPreviousBullyingStep);
            bullyingCancelButton = CreateButton(
                panel,
                BullyingCancelButtonName,
                CheatUi.Text(CheatUi.CancelKey),
                CloseButtonWidth,
                CloseButtonHeight,
                Close);
            bullyingContinueButton = CreateButton(
                panel,
                BullyingFooterName,
                BullyingText(NextKey),
                CloseButtonWidth,
                CloseButtonHeight,
                ContinueBullyingSelection);
            RefreshBullyingFooter();
        }

        private static void RefreshBullyingFooter()
        {
            if (bullyingCancelButton == null || bullyingContinueButton == null) return;

            float footerTop = PanelHeight - CheatUi.FooterInset;
            int visibleButtonCount = bullyingStep == BullyingStep.Victim
                ? BullyingVictimStepFooterButtonCount : BullyingLaterStepFooterButtonCount;
            float footerWidth = visibleButtonCount * CloseButtonWidth
                + (visibleButtonCount - 1) * BullyingFooterButtonGap;
            float buttonLeft = (PanelWidth - footerWidth) * CheatUi.Center;

            if (bullyingBackButton != null)
            {
                bool showBack = bullyingStep != BullyingStep.Victim;
                bullyingBackButton.gameObject.SetActive(showBack);
                if (showBack)
                {
                    CheatUi.Place(bullyingBackButton.GetComponent<RectTransform>(), buttonLeft, footerTop,
                        CloseButtonWidth, CloseButtonHeight);
                    buttonLeft += CloseButtonWidth + BullyingFooterButtonGap;
                }
            }

            CheatUi.Place(bullyingCancelButton.GetComponent<RectTransform>(), buttonLeft, footerTop,
                CloseButtonWidth, CloseButtonHeight);
            buttonLeft += CloseButtonWidth + BullyingFooterButtonGap;
            CheatUi.Place(bullyingContinueButton.GetComponent<RectTransform>(), buttonLeft, footerTop,
                CloseButtonWidth, CloseButtonHeight);
        }

        private static void SelectBullyingGirl(data_girls.girls girl)
        {
            if (!IsAvailableBullyingIdol(girl)) return;

            if (bullyingStep == BullyingStep.Victim)
            {
                selectedGirl = girl;
            }
            else if (bullyingStep == BullyingStep.StoppedMembers && BuildStoppedMemberChoices().Contains(girl))
            {
                if (!selectedStoppedBullyingIds.Add(girl.id)) selectedStoppedBullyingIds.Remove(girl.id);
            }
            RefreshBullyingSelection();
        }

        private static void ToggleBullyingClique(Relationships._clique clique)
        {
            if (!IsEligibleBullyingClique(clique)) return;
            if (selectedBullyingCliques.Contains(clique)) selectedBullyingCliques.Remove(clique);
            else selectedBullyingCliques.Add(clique);
            selectedStoppedBullyingIds.Clear();
            RefreshBullyingSelection();
        }

        private static void RefreshBullyingSelection()
        {
            bool victimAvailable = IsAvailableBullyingIdol(selectedGirl);
            if (selectedIdolText != null)
            {
                selectedIdolText.text = victimAvailable
                    ? string.Format(CheatUi.Culture, BullyingText(VictimLabelKey), SafeGirlName(selectedGirl))
                    : BullyingText(SelectVictimKey);
            }
            if (detailText != null)
            {
                detailText.text = bullyingStep == BullyingStep.Victim ? BullyingText(VictimInstructionsKey)
                    : bullyingStep == BullyingStep.Cliques ? BullyingText(CliqueInstructionsKey)
                    : BullyingText(StoppedInstructionsKey);
            }

            foreach (KeyValuePair<int, GameObject> entry in bullyingHighlights)
            {
                if (entry.Value == null) continue;
                entry.Value.SetActive(bullyingStep == BullyingStep.Victim
                    ? victimAvailable && selectedGirl.id == entry.Key
                    : bullyingStep == BullyingStep.StoppedMembers && selectedStoppedBullyingIds.Contains(entry.Key));
            }
            foreach (KeyValuePair<Relationships._clique, GameObject> entry in bullyingCliqueHighlights)
            {
                if (entry.Value != null)
                    entry.Value.SetActive(bullyingStep == BullyingStep.Cliques && selectedBullyingCliques.Contains(entry.Key));
            }

            RefreshBullyingNamesList();
            if (bullyingContinueButton != null)
            {
                bool canContinue = victimAvailable;
                if (bullyingStep == BullyingStep.Cliques) canContinue = victimAvailable;
                else if (bullyingStep == BullyingStep.StoppedMembers) canContinue = victimAvailable;
                CheatUi.SetButtonInteractable(bullyingContinueButton, canContinue);
                CheatUi.SetButtonText(bullyingContinueButton,
                    BullyingText(bullyingStep == BullyingStep.StoppedMembers ? ConfirmBulliesKey : NextKey));
            }
            RefreshBullyingFooter();
        }

        private static void CreateBullyingNamesList(Transform panel, float left)
        {
            bullyingNamesHeading = CheatUi.LabelAt(panel, SelectedCliquesKey, left,
                BodyTop + BullyingNamesHeadingOffset, RightWidth, BullyingNamesHeadingHeight);
            bullyingNamesScroll = CheatUi.Scroll(panel, BullyingNamesScrollName, Vector2.zero, Vector2.zero);
            float top = BodyTop + BullyingNamesListOffset;
            CheatUi.Place(bullyingNamesScroll.Root.GetComponent<RectTransform>(), left, top,
                RightWidth, PanelHeight - BodyBottom - top);
            VerticalLayoutGroup layout = bullyingNamesScroll.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(BullyingNamesPadding, BullyingNamesPadding,
                BullyingNamesPadding, BullyingNamesPadding);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            ContentSizeFitter fitter = bullyingNamesScroll.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bullyingNamesText = CreateText(bullyingNamesScroll.Content, BullyingNamesTextName, string.Empty,
                CheatUi.BodyFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            bullyingNamesText.enableAutoSizing = false;
            bullyingNamesText.enableWordWrapping = true;
            bullyingNamesText.richText = false;
            bullyingNamesScroll.Root.SetActive(false);
            bullyingNamesHeading.gameObject.SetActive(false);
        }

        private static void RefreshBullyingNamesList()
        {
            if (bullyingNamesScroll == null || bullyingNamesText == null || bullyingNamesHeading == null) return;
            bool show = bullyingStep != BullyingStep.Victim;
            bullyingNamesScroll.Root.SetActive(show);
            bullyingNamesHeading.gameObject.SetActive(show);
            if (!show) return;

            List<string> effects = BuildBullyingEffectPreview(bullyingStep == BullyingStep.StoppedMembers);
            string effectsText = BuildEffectsText(effects);

            if (bullyingStep == BullyingStep.Cliques)
            {
                List<string> names = new List<string>();
                foreach (Relationships._clique clique in selectedBullyingCliques)
                {
                    if (!IsEligibleBullyingClique(clique)) continue;
                    names.Add(string.Format(CheatUi.Culture, BullyingText("ui.bullying.clique_summary_item"),
                        SafeGirlName(clique.Leader), clique.Members.Count));
                }
                names.Sort(StringComparer.Create(CheatUi.Culture, true));
                bullyingNamesHeading.text = string.Format(CheatUi.Culture, BullyingText(SelectedCliquesKey), names.Count);
                string namesText = names.Count == 0 ? BullyingText(NoSelectedCliquesKey)
                    : string.Join(NameLineSeparator, names.ToArray());
                bullyingNamesText.text = namesText + NameLineSeparator + NameLineSeparator + effectsText;
            }
            else
            {
                List<string> names = new List<string>();
                foreach (data_girls.girls girl in BuildStoppedMemberChoices())
                {
                    if (!selectedStoppedBullyingIds.Contains(girl.id)) continue;
                    Relationships._clique clique = girl.GetClique();
                    names.Add(string.Format(CheatUi.Culture, BullyingText("ui.bullying.stopped_summary_item"),
                        SafeGirlName(girl), SafeGirlName(clique == null ? null : clique.Leader)));
                }
                names.Sort(StringComparer.Create(CheatUi.Culture, true));
                bullyingNamesHeading.text = string.Format(CheatUi.Culture, BullyingText(SelectedStoppedKey), names.Count);
                string namesText = names.Count == 0 ? BullyingText(NoSelectedStoppedKey)
                    : string.Join(NameLineSeparator, names.ToArray());
                bullyingNamesText.text = namesText + NameLineSeparator + NameLineSeparator + effectsText;
            }
            IMUiKit.RebuildLayout(bullyingNamesScroll.Content);
        }

        private static string BuildEffectsText(List<string> effects)
        {
            string heading = BullyingText("ui.bullying.effects_heading");
            if (effects == null || effects.Count == 0)
                return heading + NameLineSeparator + BullyingText("ui.bullying.effects_none");

            List<string> lines = new List<string>();
            foreach (string effect in effects)
            {
                if (!string.IsNullOrEmpty(effect)) lines.Add("• " + effect);
            }
            if (lines.Count == 0) lines.Add(BullyingText("ui.bullying.effects_none"));
            return heading + NameLineSeparator + string.Join(NameLineSeparator, lines.ToArray());
        }

        private static void ContinueBullyingSelection()
        {
            try
            {
                if (!IsAvailableBullyingIdol(selectedGirl))
                {
                    NotifyWarning(NoSelectionKey, NoSelectionFallback);
                    return;
                }

                PopupManager manager = GetPopupManager();
                GameObject prefab = manager == null ? null : GetStylistGirlButtonPrefab(manager);
                if (prefab == null || bullyingPanel == null)
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                if (bullyingStep == BullyingStep.Victim)
                {
                    List<Relationships._clique> eligibleCliques = BuildEligibleBullyingCliques();
                    if (eligibleCliques.Count == 0)
                    {
                        NotifyWarning(NoAvailableCliquesKey, string.Empty);
                        return;
                    }
                    bullyingStep = BullyingStep.Cliques;
                    SelectExistingBullyingCliques();
                    selectedStoppedBullyingIds.Clear();
                    ReplaceBullyingWithCliquePicker(eligibleCliques, prefab);
                }
                else if (bullyingStep == BullyingStep.Cliques)
                {
                    selectedBullyingCliques.RemoveAll(delegate(Relationships._clique clique)
                    {
                        return !IsEligibleBullyingClique(clique);
                    });
                    bullyingStep = BullyingStep.StoppedMembers;
                    LoadExistingStoppedBullyingSelections();
                    ReplaceBullyingWithGirlPicker(BuildStoppedMemberChoices(), prefab);
                }
                else
                {
                    ApplySelectedBullyingCliques();
                    return;
                }

                if (bullyingTitle != null) bullyingTitle.text = GetBullyingHeading();
                RefreshBullyingSelection();
            }
            catch (Exception exception)
            {
                Debug.LogError(BullyingErrorPrefix + exception);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static void ReturnToPreviousBullyingStep()
        {
            if (bullyingStep == BullyingStep.Victim) return;

            try
            {
                PopupManager manager = GetPopupManager();
                GameObject prefab = manager == null ? null : GetStylistGirlButtonPrefab(manager);
                if (prefab == null || bullyingPanel == null)
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                if (bullyingStep == BullyingStep.StoppedMembers)
                {
                    bullyingStep = BullyingStep.Cliques;
                    selectedStoppedBullyingIds.Clear();
                    ReplaceBullyingWithCliquePicker(BuildEligibleBullyingCliques(), prefab);
                }
                else
                {
                    bullyingStep = BullyingStep.Victim;
                    selectedBullyingCliques.Clear();
                    selectedStoppedBullyingIds.Clear();
                    ReplaceBullyingWithGirlPicker(BuildEligibleGirls(Mode.AddBullies), prefab);
                }
                if (bullyingTitle != null) bullyingTitle.text = GetBullyingHeading();
                RefreshBullyingSelection();
            }
            catch (Exception exception)
            {
                Debug.LogError(BullyingErrorPrefix + exception);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static void DestroyBullyingPicker()
        {
            bullyingHighlights.Clear();
            bullyingCliqueHighlights.Clear();
            if (bullyingPickerLabel != null)
            {
                bullyingPickerLabel.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(bullyingPickerLabel.gameObject);
                bullyingPickerLabel = null;
            }
            if (bullyingPickerScroll != null)
            {
                bullyingPickerScroll.SetActive(false);
                UnityEngine.Object.Destroy(bullyingPickerScroll);
                bullyingPickerScroll = null;
            }
        }

        private static void ReplaceBullyingWithGirlPicker(List<data_girls.girls> eligible, GameObject prefab)
        {
            DestroyBullyingPicker();
            CreateGirlPicker(bullyingPanel, eligible, prefab);
        }

        private static void ReplaceBullyingWithCliquePicker(List<Relationships._clique> eligible, GameObject prefab)
        {
            DestroyBullyingPicker();
            bullyingPickerLabel = CheatUi.LabelAt(bullyingPanel, SelectCliquesKey, Margin, BodyTop,
                LeftWidth, PickerLabelHeight);
            bullyingPickerLabel.text = GetBullyingHeading();
            IMUiScrollViewHandle scroll = CheatUi.Scroll(bullyingPanel, "BullyingCliquePicker", Vector2.zero, Vector2.zero);
            CheatUi.Place(scroll.Root.GetComponent<RectTransform>(), Margin, BodyTop + PickerLabelHeight,
                LeftWidth, PanelHeight - BodyTop - BodyBottom - PickerLabelHeight);
            VerticalLayoutGroup layout = scroll.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = scroll.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float cardWidth = LeftWidth - 50f;
            for (int index = 0; index < eligible.Count; index++)
            {
                int displayIndex = index + 1;
                Relationships._clique clique = eligible[index];
                GameObject highlight;
                CliqueDisplayUi.CreateCard(scroll.Content, "BullyClique" + index, clique, displayIndex,
                    cardWidth, prefab, delegate { ToggleBullyingClique(clique); }, out highlight);
                bullyingCliqueHighlights[clique] = highlight;
            }
            bullyingPickerScroll = scroll.Root;
            scroll.ScrollRect.verticalNormalizedPosition = 1f;
        }

        private static List<string> BuildBullyingEffectPreview(bool includeStoppedChanges)
        {
            List<string> effects = new List<string>();
            if (!IsAvailableBullyingIdol(selectedGirl)) return effects;

            data_girls.girls victim = selectedGirl;
            List<Relationships._clique> candidates = BuildEligibleBullyingCliques();
            bool hasNewNativeStart = false;
            foreach (Relationships._clique clique in candidates)
            {
                if (selectedBullyingCliques.Contains(clique) && !clique.IsBullied(victim))
                {
                    hasNewNativeStart = true;
                    break;
                }
            }
            policies.value blockingNoBullyingPolicy;
            int noBullyingDefaultValue;
            if (hasNewNativeStart
                && TryGetBlockingNoBullyingPolicy(out blockingNoBullyingPolicy, out noBullyingDefaultValue))
            {
                effects.Add(BullyingText("ui.bullying.effect_policy_bypass"));
            }

            foreach (Relationships._clique clique in candidates)
            {
                bool selected = selectedBullyingCliques.Contains(clique);
                bool wasBullying = clique.IsBullied(victim);
                string leaderName = SafeGirlName(clique.Leader);

                if (!selected)
                {
                    if (wasBullying)
                    {
                        effects.Add(string.Format(CheatUi.Culture,
                            BullyingText("ui.bullying.effect_stop_clique"), leaderName, SafeGirlName(victim)));
                    }
                    continue;
                }

                bool willRemainBullying;
                if (!wasBullying)
                {
                    NativeBullyingStartPreview preview = PreviewNativeBullyingStart(clique, victim);
                    AddNativeStartEffectPreview(effects, clique, victim, preview);
                    if (includeStoppedChanges)
                        AddStoppedMemberEffectPreview(effects, clique, victim, false, preview);
                    willRemainBullying = preview != null && preview.BullyingRemainsActive;
                    if (willRemainBullying && includeStoppedChanges
                        && AreAllFutureBullyingMembersStopped(preview.RemainingMembers))
                        willRemainBullying = false;
                }
                else
                {
                    if (includeStoppedChanges)
                        AddStoppedMemberEffectPreview(effects, clique, victim, true, null);
                    willRemainBullying = !includeStoppedChanges
                        || !AreAllFutureBullyingMembersStopped(clique.Members);
                }

                if (willRemainBullying)
                {
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_weekly_stamina"), SafeGirlName(victim), leaderName));
                }
            }
            return effects;
        }

        private static bool AreAllFutureBullyingMembersStopped(List<data_girls.girls> members)
        {
            if (members == null || members.Count == 0) return false;
            int availableMembers = 0;
            int stoppedMembers = 0;
            foreach (data_girls.girls member in members)
            {
                if (member == null) continue;
                availableMembers++;
                if (selectedStoppedBullyingIds.Contains(member.id)) stoppedMembers++;
            }
            return availableMembers > 0 && stoppedMembers >= availableMembers;
        }

        private static void AddNativeStartEffectPreview(List<string> effects, Relationships._clique clique,
            data_girls.girls victim, NativeBullyingStartPreview preview)
        {
            if (effects == null || clique == null || victim == null || preview == null) return;
            string leaderName = SafeGirlName(clique.Leader);
            string victimName = SafeGirlName(victim);

            if (preview.ThickSkinBlocked)
            {
                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText("ui.bullying.effect_thick_skin_block"), victimName, leaderName));
                return;
            }

            effects.Add(string.Format(CheatUi.Culture,
                BullyingText("ui.bullying.effect_start_clique"), leaderName, victimName));

            foreach (data_girls.girls member in preview.VictimRelationshipPenaltyMembers)
            {
                Relationships._relationship relationship = FindExistingRelationship(member, victim);
                float before = relationship == null ? 0.5f : relationship.Ratio;
                float after = PreviewRelationshipRatioAfterAdd(relationship, member, victim, -2f);
                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText("ui.bullying.effect_bond_negative"), SafeGirlName(member), victimName,
                    FormatRelationshipPercent(before), FormatRelationshipPercent(after)));
            }

            if (preview.EjectedDatingMember != null)
            {
                data_girls.girls ejected = preview.EjectedDatingMember;
                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText("ui.bullying.effect_dating_ejection"), SafeGirlName(ejected), victimName, leaderName));

                foreach (data_girls.girls remaining in preview.RemainingMembers)
                {
                    Relationships._relationship relationship = FindExistingRelationship(remaining, ejected);
                    float before = relationship == null ? 0.5f : relationship.Ratio;
                    float after = PreviewRelationshipRatioAfterAdd(relationship, remaining, ejected, -2f);
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_bond_ejection"), SafeGirlName(remaining), SafeGirlName(ejected),
                        FormatRelationshipPercent(before), FormatRelationshipPercent(after)));
                }

                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText("ui.bullying.effect_ejected_stamina"), SafeGirlName(ejected)));

                if (preview.BullyingRemainsActive)
                {
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_ejection_still_bullies"),
                        leaderName, victimName, SafeGirlName(ejected)));
                }

                if (preview.CliqueDisbands)
                {
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_clique_disbands"), leaderName));
                }
                else if (ejected == clique.Leader)
                {
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_leader_ejected"), SafeGirlName(ejected),
                        SafeGirlName(preview.NewLeader), victimName));
                }
            }
            else if (preview.VictimMentalStaminaPenalty)
            {
                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText("ui.bullying.effect_victim_stamina"), victimName));
            }
        }

        private static void AddStoppedMemberEffectPreview(List<string> effects, Relationships._clique clique,
            data_girls.girls victim, bool wasBullying, NativeBullyingStartPreview nativePreview)
        {
            if (effects == null || clique == null || victim == null || clique.Members == null) return;
            string leaderName = SafeGirlName(clique.Leader);
            string victimName = SafeGirlName(victim);

            List<data_girls.girls> futureMembers = new List<data_girls.girls>();
            if (wasBullying)
            {
                futureMembers.AddRange(clique.Members);
            }
            else if (nativePreview != null && nativePreview.BullyingRemainsActive)
            {
                futureMembers.AddRange(nativePreview.RemainingMembers);
            }

            if (!wasBullying && nativePreview != null && nativePreview.EjectedDatingMember != null
                && selectedStoppedBullyingIds.Contains(nativePreview.EjectedDatingMember.id))
            {
                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText("ui.bullying.effect_stopped_member_ejected"),
                    SafeGirlName(nativePreview.EjectedDatingMember), victimName, leaderName));
            }

            if (!wasBullying && (nativePreview == null || !nativePreview.BullyingRemainsActive))
            {
                foreach (data_girls.girls member in clique.Members)
                {
                    if (member == null || !selectedStoppedBullyingIds.Contains(member.id)
                        || (nativePreview != null && member == nativePreview.EjectedDatingMember)) continue;
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_stopped_not_applied"), SafeGirlName(member), leaderName));
                }
                return;
            }

            int selectedFutureCount = 0;
            foreach (data_girls.girls member in futureMembers)
                if (member != null && selectedStoppedBullyingIds.Contains(member.id)) selectedFutureCount++;

            if (futureMembers.Count > 0 && selectedFutureCount >= futureMembers.Count)
            {
                effects.Add(string.Format(CheatUi.Culture,
                    BullyingText(wasBullying ? "ui.bullying.effect_all_stopped" : "ui.bullying.effect_all_stopped_after_start"),
                    leaderName, victimName));
                return;
            }

            Relationships._clique._stopped_bullying currentRecord = wasBullying
                ? clique.GetStoppedBullying(victim) : null;
            foreach (data_girls.girls member in futureMembers)
            {
                if (member == null) continue;
                bool desiredStopped = selectedStoppedBullyingIds.Contains(member.id);
                bool currentlyStopped = currentRecord != null && currentRecord.Girls != null
                    && currentRecord.Girls.Contains(member);
                if (desiredStopped == currentlyStopped) continue;

                if (desiredStopped)
                {
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText(wasBullying ? "ui.bullying.effect_member_stops" : "ui.bullying.effect_member_stops_after_start"),
                        SafeGirlName(member), victimName));
                }
                else
                {
                    effects.Add(string.Format(CheatUi.Culture,
                        BullyingText("ui.bullying.effect_member_resumes"), SafeGirlName(member), victimName));
                }
            }
        }

        private static NativeBullyingStartPreview PreviewNativeBullyingStart(Relationships._clique clique,
            data_girls.girls victim)
        {
            NativeBullyingStartPreview preview = new NativeBullyingStartPreview();
            if (clique == null || victim == null || clique.Members == null) return preview;

            if (victim.trait == traits._trait._type.Thick_Skin)
            {
                preview.ThickSkinBlocked = true;
                return preview;
            }

            preview.RemainingMembers.AddRange(clique.Members);
            for (int index = clique.Members.Count - 1; index >= 0; index--)
            {
                data_girls.girls member = clique.Members[index];
                if (member == null) continue;
                Relationships._relationship relationship = FindExistingRelationship(member, victim);
                if (relationship != null && relationship.Dating)
                {
                    preview.EjectedDatingMember = member;
                    preview.RemainingMembers.Remove(member);
                    preview.VictimMentalStaminaPenalty = false;
                    for (int skippedIndex = index - 1; skippedIndex >= 0; skippedIndex--)
                    {
                        data_girls.girls skippedMember = clique.Members[skippedIndex];
                        if (skippedMember != null)
                            preview.SkippedVictimRelationshipPenaltyMembers.Add(skippedMember);
                    }

                    if (preview.RemainingMembers.Count == 1)
                    {
                        preview.CliqueDisbands = true;
                        preview.BullyingRemainsActive = false;
                    }
                    else if (preview.RemainingMembers.Count == 0)
                    {
                        preview.BullyingRemainsActive = false;
                    }
                    else if (member == clique.Leader)
                    {
                        preview.NewLeader = PredictCliqueLeader(preview.RemainingMembers);
                        preview.BullyingRemainsActive = false;
                    }
                    else
                    {
                        preview.BullyingRemainsActive = true;
                    }
                    return preview;
                }
                preview.VictimRelationshipPenaltyMembers.Add(member);
            }

            preview.VictimMentalStaminaPenalty = true;
            preview.BullyingRemainsActive = true;
            return preview;
        }

        private static data_girls.girls PredictCliqueLeader(List<data_girls.girls> members)
        {
            if (members == null) return null;
            float bestScore = 0f;
            data_girls.girls best = null;
            foreach (data_girls.girls member in members)
            {
                if (member == null) continue;
                float score = member.getParam(data_girls._paramType.funny).val
                    + member.getParam(data_girls._paramType.smart).val;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = member;
                }
            }
            return best;
        }

        private static Relationships._relationship FindExistingRelationship(data_girls.girls first,
            data_girls.girls second)
        {
            if (first == null || second == null || Relationships.RelationshipsData == null) return null;
            foreach (Relationships._relationship relationship in Relationships.RelationshipsData)
            {
                if (relationship == null || relationship.Girls == null || relationship.Girls.Count != 2) continue;
                if ((relationship.Girls[0] == first && relationship.Girls[1] == second)
                    || (relationship.Girls[0] == second && relationship.Girls[1] == first))
                    return relationship;
            }
            return null;
        }

        private static float PreviewRelationshipRatioAfterAdd(Relationships._relationship relationship,
            data_girls.girls first, data_girls.girls second, float value)
        {
            List<int> vals = relationship == null || relationship.Vals == null
                ? new List<int> { 1, -1 } : new List<int>(relationship.Vals);
            float temp = relationship == null ? 0f : relationship.Temp;
            float adjusted = value;
            if (adjusted > 0f) adjusted /= 2f;
            if ((first != null && first.trait == traits._trait._type.Loyal
                    || second != null && second.trait == traits._trait._type.Loyal) && adjusted < 0f)
                adjusted /= 2f;

            if (adjusted >= 1f)
            {
                while (adjusted >= 1f)
                {
                    vals.Add(1);
                    adjusted -= 1f;
                }
            }
            else if (adjusted <= -1f)
            {
                while (adjusted <= -1f)
                {
                    vals.Add(-1);
                    adjusted += 1f;
                }
            }

            temp += adjusted;
            if (temp > 1f)
            {
                vals.Add(1);
                temp -= 1f;
            }
            else if (temp < -1f)
            {
                vals.Add(-1);
                temp += 1f;
            }
            while (vals.Count > 20) vals.RemoveAt(0);

            int positive = 0;
            int negative = 0;
            foreach (int val in vals)
            {
                if (val > 0) positive++;
                else if (val < 0) negative++;
            }
            if (positive == 0 && negative == 0) return 0.5f;
            return (float)positive / (positive + negative);
        }

        private static string FormatRelationshipPercent(float ratio)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f).ToString(CheatUi.Culture);
        }

        private static void ApplySelectedBullyingCliques()
        {
            if (!IsAvailableBullyingIdol(selectedGirl))
            {
                NotifyWarning(NoSelectionKey, NoSelectionFallback);
                return;
            }

            data_girls.girls victim = selectedGirl;
            List<Relationships._clique> candidates = BuildEligibleBullyingCliques();
            int activeCliques = 0;
            int stoppedMembers = 0;
            int removedCliques = 0;
            int rejectedCliques = 0;
            bool bypassedNoBullyingPolicy = false;

            foreach (Relationships._clique clique in candidates)
            {
                bool selected = selectedBullyingCliques.Contains(clique);
                bool wasBullying = clique.IsBullied(victim);
                if (!selected)
                {
                    if (wasBullying)
                    {
                        clique.StopBullying(victim);
                        removedCliques++;
                    }
                    continue;
                }

                if (!wasBullying)
                {
                    bool bypassedForClique;
                    bool started = TryStartBullyingWithNativeOperation(clique, victim, out bypassedForClique);
                    bypassedNoBullyingPolicy |= bypassedForClique;
                    if (!started)
                    {
                        rejectedCliques++;
                        continue;
                    }
                }

                if (Relationships.Cliques == null || !Relationships.Cliques.Contains(clique)
                    || !clique.IsBullied(victim))
                {
                    rejectedCliques++;
                    continue;
                }

                clique.AddKnownBulliedGirl(victim);
                List<data_girls.girls> stopped = new List<data_girls.girls>();
                foreach (data_girls.girls member in clique.Members)
                {
                    if (member != null && selectedStoppedBullyingIds.Contains(member.id)) stopped.Add(member);
                }

                if (stopped.Count == 0)
                {
                    RemoveStoppedBullyingRecord(clique, victim);
                    activeCliques++;
                    continue;
                }

                if (stopped.Count >= clique.Members.Count)
                {
                    clique.StopBullying(victim);
                    removedCliques++;
                    continue;
                }

                Relationships._clique._stopped_bullying record = clique.GetStoppedBullying(victim);
                if (record == null)
                {
                    record = new Relationships._clique._stopped_bullying { Target = victim };
                    clique.StoppedBullying.Add(record);
                }
                record.Girls.Clear();
                foreach (data_girls.girls member in stopped)
                    if (!record.Girls.Contains(member)) record.Girls.Add(member);
                stoppedMembers += record.Girls.Count;
                activeCliques++;
            }

            RefreshGirlAndList(victim);
            string message = string.Format(CheatUi.Culture, BullyingText(BullyingAdjustedKey),
                SafeGirlName(victim), activeCliques, stoppedMembers, removedCliques);
            Close();
            NotifySuccess(message, NotificationManager._notification._type.idol_relationship_change);
            if (bypassedNoBullyingPolicy)
                NotifyWarning(NoBullyingPolicyWarningKey, string.Empty);
            if (rejectedCliques > 0)
                NotifyWarning(NativeBullyingRejectedKey, string.Empty);
        }

        private static bool TryStartBullyingWithNativeOperation(Relationships._clique clique,
            data_girls.girls victim, out bool bypassedNoBullyingPolicy)
        {
            bypassedNoBullyingPolicy = false;
            if (clique == null || victim == null) return false;

            policies.value blockingPolicy;
            int defaultPolicyValue;
            bool hasBlockingNoBullyingPolicy =
                TryGetBlockingNoBullyingPolicy(out blockingPolicy, out defaultPolicyValue);
            policies._value originalPolicyValue = policies._value.NONE;

            try
            {
                // Preserve the game's normal bullying consequences. The optional No Bullying
                // Policy integration is touched only when reflection confirms that mod's own
                // Harmony patch is active and its disabling option is currently selected.
                if (hasBlockingNoBullyingPolicy)
                {
                    originalPolicyValue = blockingPolicy.Value;
                    blockingPolicy.Value = (policies._value)defaultPolicyValue;
                    bypassedNoBullyingPolicy = true;
                }

                clique.AddBulliedGirl(victim);
            }
            finally
            {
                if (hasBlockingNoBullyingPolicy)
                    blockingPolicy.Value = originalPolicyValue;
            }

            return clique.IsBullied(victim);
        }

        private static bool TryGetBlockingNoBullyingPolicy(
            out policies.value blockingPolicy, out int defaultPolicyValue)
        {
            blockingPolicy = null;
            defaultPolicyValue = 0;

            Assembly noBullyingAssembly = FindLoadedAssemblyByName(NoBullyingPolicyAssemblyName);
            if (noBullyingAssembly == null || !IsNoBullyingPolicyPatchEnabled())
                return false;

            Type constantsType = noBullyingAssembly.GetType(NoBullyingPolicyConstantsTypeName, false);
            if (constantsType == null)
                return false;

            int policyTypeValue;
            int disabledPolicyValue;
            if (!TryReadIntConstant(constantsType, NoBullyingPolicyTypeFieldName, out policyTypeValue)
                || !TryReadIntConstant(constantsType, NoBullyingPolicyDefaultFieldName, out defaultPolicyValue)
                || !TryReadIntConstant(constantsType, NoBullyingPolicyDisabledFieldName, out disabledPolicyValue))
            {
                blockingPolicy = null;
                defaultPolicyValue = 0;
                return false;
            }

            policies.value selectedPolicy =
                policies.GetSelectedPolicyValue((policies._type)policyTypeValue);
            if (selectedPolicy == null || (int)selectedPolicy.Value != disabledPolicyValue)
                return false;

            blockingPolicy = selectedPolicy;
            return true;
        }

        private static Assembly FindLoadedAssemblyByName(string assemblyName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;
                try
                {
                    AssemblyName loadedName = assembly.GetName();
                    if (loadedName != null
                        && string.Equals(loadedName.Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return assembly;
                    }
                }
                catch (Exception)
                {
                    // Ignore dynamic/broken assemblies and keep looking.
                }
            }
            return null;
        }

        private static bool TryReadIntConstant(Type type, string fieldName, out int value)
        {
            value = 0;
            if (type == null) return false;
            try
            {
                FieldInfo field = type.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null) return false;
                object rawValue = field.GetValue(null);
                if (rawValue == null) return false;
                value = Convert.ToInt32(rawValue);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsNoBullyingPolicyPatchEnabled()
        {
            try
            {
                Type harmonyType = FindLoadedTypeByName(HarmonyTypeName);
                if (harmonyType == null) return false;

                MethodInfo getPatchInfo = harmonyType.GetMethod(
                    HarmonyGetPatchInfoMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new Type[] { typeof(MethodBase) },
                    null);
                MethodInfo addBulliedGirl = typeof(Relationships._clique).GetMethod(
                    nameof(Relationships._clique.AddBulliedGirl),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(data_girls.girls) },
                    null);
                if (getPatchInfo == null || addBulliedGirl == null) return false;

                object patchInfo = getPatchInfo.Invoke(null, new object[] { addBulliedGirl });
                return PatchInfoHasOwner(patchInfo, NoBullyingPolicyHarmonyOwnerId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Type FindLoadedTypeByName(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;
                try
                {
                    Type type = assembly.GetType(typeName, false);
                    if (type != null) return type;
                }
                catch (Exception)
                {
                    // Ignore dynamic/broken assemblies and keep looking.
                }
            }
            return null;
        }

        private static bool PatchInfoHasOwner(object patchInfo, string ownerId)
        {
            if (patchInfo == null || string.IsNullOrEmpty(ownerId)) return false;

            IEnumerable owners = GetEnumerableMember(patchInfo, HarmonyPatchOwnersMemberName);
            if (EnumerableContainsOwner(owners, ownerId)) return true;

            foreach (string collectionName in HarmonyPatchCollectionMemberNames)
            {
                IEnumerable patches = GetEnumerableMember(patchInfo, collectionName);
                if (patches == null) continue;
                foreach (object patch in patches)
                {
                    if (patch == null) continue;
                    object owner = GetMemberValue(patch, HarmonyPatchOwnerPropertyName)
                        ?? GetMemberValue(patch, HarmonyPatchOwnerFieldName);
                    if (owner is string
                        && string.Equals((string)owner, ownerId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static IEnumerable GetEnumerableMember(object instance, string memberName)
        {
            return GetMemberValue(instance, memberName) as IEnumerable;
        }

        private static object GetMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrEmpty(memberName)) return null;
            Type type = instance.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null) return property.GetValue(instance, null);
                FieldInfo field = type.GetField(memberName, flags);
                return field == null ? null : field.GetValue(instance);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool EnumerableContainsOwner(IEnumerable values, string ownerId)
        {
            if (values == null) return false;
            foreach (object value in values)
            {
                string text = value as string;
                if (text != null && string.Equals(text, ownerId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void RemoveStoppedBullyingRecord(Relationships._clique clique, data_girls.girls victim)
        {
            if (clique == null || clique.StoppedBullying == null) return;
            for (int index = clique.StoppedBullying.Count - 1; index >= 0; index--)
            {
                Relationships._clique._stopped_bullying record = clique.StoppedBullying[index];
                if (record != null && record.Target == victim) clique.StoppedBullying.RemoveAt(index);
            }
        }
    }
}
