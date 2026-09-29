using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string SelectBulliesKey = "ui.bullying.select_bullies";
        private const string VictimInstructionsKey = "ui.bullying.victim_instructions";
        private const string BullyInstructionsKey = "ui.bullying.bully_instructions";
        private const string VictimLabelKey = "ui.bullying.victim";
        private const string SelectedBulliesKey = "ui.bullying.selected_bullies";
        private const string NextKey = "ui.bullying.next";
        private const string ConfirmBulliesKey = "ui.bullying.ok";
        private const string NoAvailableBulliesKey = "notification.no_available_bullies";
        private const string BulliesAddedKey = "notification.bullies_added";
        private const string BullyingSelectionName = "BullyingSelection";
        private const string BullyingFooterName = "BullyingContinue";
        private const string BullyingErrorPrefix = "[CheatsMod] Add-bullies cheat failed: ";
        private const string NoSelectedBulliesKey = "ui.bullying.none_selected";
        private const string BullyingNamesScrollName = "SelectedBullyNames";
        private const string BullyingNamesTextName = "Names";
        private const string NameLineSeparator = "\n";
        private const float BullyingInstructionsHeight = 100f;
        private const float BullyingNamesHeadingOffset = 174f;
        private const float BullyingNamesListOffset = 212f;
        private const float BullyingNamesHeadingHeight = 30f;
        private const int BullyingNamesPadding = 12;
        private const float FooterBottomInset = 50f;
        private const float CenterDivisor = 2f;
        private const byte SelectionOverlayAlpha = 72;

        private static bool selectingBullies;
        private static Transform bullyingPanel;
        private static TextMeshProUGUI bullyingTitle;
        private static TextMeshProUGUI bullyingPickerLabel;
        private static GameObject bullyingPickerScroll;
        private static Button bullyingContinueButton;
        private static TextMeshProUGUI bullyingNamesHeading;
        private static TextMeshProUGUI bullyingNamesText;
        private static IMUiScrollViewHandle bullyingNamesScroll;
        private static readonly HashSet<int> selectedBullyIds = new HashSet<int>();
        private static readonly Dictionary<int, GameObject> bullyingHighlights = new Dictionary<int, GameObject>();

        internal static void OpenAddBullies()
        {
            Open(Mode.AddBullies);
        }

        private static string BullyingText(string key)
        {
            return ModLocalization.Get(key, string.Empty);
        }

        private static string GetBullyingHeading()
        {
            return BullyingText(selectingBullies ? SelectBulliesKey : SelectVictimKey);
        }

        private static void ResetBullyingSelection()
        {
            selectingBullies = false;
            selectedBullyIds.Clear();
            bullyingHighlights.Clear();
            bullyingPanel = null;
            bullyingTitle = null;
            bullyingPickerLabel = null;
            bullyingPickerScroll = null;
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

        private static bool CanAddBully(data_girls.girls bully)
        {
            if (!IsAvailableBullyingIdol(bully) || bully == selectedGirl)
            {
                return false;
            }

            Relationships._clique clique = bully.GetClique();
            return clique == null || !clique.IsBulliedBy(bully, selectedGirl);
        }

        private static void CreateBullyingHighlight(GameObject item, data_girls.girls girl)
        {
            // A non-interactive overlay remains green even when the native card changes hover color.
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
            bullyingContinueButton = CreateButton(
                panel,
                BullyingFooterName,
                BullyingText(NextKey),
                CloseButtonWidth,
                CloseButtonHeight,
                ContinueBullyingSelection);
            SetRect(
                bullyingContinueButton.GetComponent<RectTransform>(),
                (PanelWidth - CloseButtonWidth) / CenterDivisor,
                -PanelHeight + FooterBottomInset,
                CloseButtonWidth,
                CloseButtonHeight,
                true);
        }

        private static void SelectBullyingGirl(data_girls.girls girl)
        {
            if (!IsAvailableBullyingIdol(girl))
            {
                return;
            }

            if (!selectingBullies)
            {
                selectedGirl = girl;
            }
            else if (CanAddBully(girl) && !selectedBullyIds.Add(girl.id))
            {
                selectedBullyIds.Remove(girl.id);
            }
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
                detailText.text = selectingBullies
                    ? BullyingText(BullyInstructionsKey)
                    : BullyingText(VictimInstructionsKey);
            }
            RefreshBullyingNamesList();
            foreach (KeyValuePair<int, GameObject> entry in bullyingHighlights)
            {
                if (entry.Value != null)
                {
                    entry.Value.SetActive(selectingBullies
                        ? selectedBullyIds.Contains(entry.Key)
                        : victimAvailable && selectedGirl.id == entry.Key);
                }
            }
            if (bullyingContinueButton != null)
            {
                CheatUi.SetButtonInteractable(bullyingContinueButton, victimAvailable
                    && (!selectingBullies || selectedBullyIds.Count > CheatAmounts.ZeroCount));
                CheatUi.SetButtonText(bullyingContinueButton,
                    BullyingText(selectingBullies ? ConfirmBulliesKey : NextKey));
            }
        }

        private static void CreateBullyingNamesList(Transform panel, float left)
        {
            bullyingNamesHeading = CheatUi.LabelAt(panel, SelectedBulliesKey, left,
                BodyTop + BullyingNamesHeadingOffset, RightWidth, BullyingNamesHeadingHeight);
            bullyingNamesScroll = CheatUi.Scroll(panel, BullyingNamesScrollName, Vector2.zero, Vector2.zero);
            float top = BodyTop + BullyingNamesListOffset;
            CheatUi.Place(bullyingNamesScroll.Root.GetComponent<RectTransform>(), left, top,
                RightWidth, PanelHeight - BodyBottom - top);
            VerticalLayoutGroup layout = bullyingNamesScroll.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(BullyingNamesPadding, BullyingNamesPadding, BullyingNamesPadding, BullyingNamesPadding);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            ContentSizeFitter fitter = bullyingNamesScroll.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bullyingNamesText = CreateText(bullyingNamesScroll.Content, BullyingNamesTextName, string.Empty,
                CheatUi.BodyFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            // Let the content grow and scroll instead of shrinking an entire roster to fit.
            bullyingNamesText.enableAutoSizing = false;
            bullyingNamesText.enableWordWrapping = true;
            bullyingNamesText.richText = false;
            bullyingNamesScroll.Root.SetActive(false);
            bullyingNamesHeading.gameObject.SetActive(false);
        }

        private static void RefreshBullyingNamesList()
        {
            if (bullyingNamesScroll == null || bullyingNamesText == null || bullyingNamesHeading == null) return;
            bullyingNamesScroll.Root.SetActive(selectingBullies);
            bullyingNamesHeading.gameObject.SetActive(selectingBullies);
            if (!selectingBullies) return;
            List<string> names = new List<string>();
            foreach (data_girls.girls girl in BuildEligibleGirls(Mode.AddBullies))
                if (selectedBullyIds.Contains(girl.id) && CanAddBully(girl)) names.Add(SafeGirlName(girl));
            names.Sort(StringComparer.Create(CheatUi.Culture, true));
            bullyingNamesHeading.text = string.Format(CheatUi.Culture, BullyingText(SelectedBulliesKey), names.Count);
            bullyingNamesText.text = names.Count == CheatAmounts.ZeroCount
                ? BullyingText(NoSelectedBulliesKey) : string.Join(NameLineSeparator, names.ToArray());
            IMUiKit.RebuildLayout(bullyingNamesScroll.Content);
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
                if (selectingBullies)
                {
                    ApplySelectedBullies();
                    return;
                }

                List<data_girls.girls> eligible = BuildEligibleGirls(Mode.AddBullies);
                eligible.RemoveAll(delegate(data_girls.girls girl) { return !CanAddBully(girl); });
                if (eligible.Count == CheatAmounts.ZeroCount)
                {
                    NotifyWarning(NoAvailableBulliesKey, string.Empty);
                    return;
                }
                PopupManager manager = GetPopupManager();
                GameObject prefab = manager == null ? null : GetStylistGirlButtonPrefab(manager);
                if (prefab == null || bullyingPanel == null)
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                // Replace only the picker inside the registered popup. Keeping the popup root
                // preserves the game's queue, paused state and backdrop across the two steps.
                selectingBullies = true;
                selectedBullyIds.Clear();
                bullyingHighlights.Clear();
                bullyingPickerLabel.gameObject.SetActive(false);
                bullyingPickerScroll.SetActive(false);
                UnityEngine.Object.Destroy(bullyingPickerLabel.gameObject);
                UnityEngine.Object.Destroy(bullyingPickerScroll);
                CreateGirlPicker(bullyingPanel, eligible, prefab);
                bullyingTitle.text = GetBullyingHeading();
                RefreshBullyingSelection();
            }
            catch (Exception exception)
            {
                Debug.LogError(BullyingErrorPrefix + exception);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static void ApplySelectedBullies()
        {
            List<data_girls.girls> bullies = BuildEligibleGirls(Mode.AddBullies);
            bullies.RemoveAll(delegate(data_girls.girls girl)
            {
                return !selectedBullyIds.Contains(girl.id) || !CanAddBully(girl);
            });
            if (bullies.Count == CheatAmounts.ZeroCount)
            {
                NotifyWarning(NoAvailableBulliesKey, string.Empty);
                return;
            }

            if (Relationships.Cliques == null)
            {
                Relationships.Cliques = new List<Relationships._clique>();
            }
            Dictionary<Relationships._clique, List<data_girls.girls>> selectedByClique =
                new Dictionary<Relationships._clique, List<data_girls.girls>>();
            foreach (data_girls.girls bully in bullies)
            {
                Relationships._clique clique = bully.GetClique();
                if (clique == null)
                {
                    // Vanilla also starts cliques with a single member. Existing clique
                    // memberships are never rearranged just to satisfy the cheat.
                    clique = new Relationships._clique { Leader = bully };
                    clique.Members.Add(bully);
                    Relationships.Cliques.Add(clique);
                }
                List<data_girls.girls> members;
                if (!selectedByClique.TryGetValue(clique, out members))
                {
                    members = new List<data_girls.girls>();
                    selectedByClique.Add(clique, members);
                }
                members.Add(bully);
            }

            foreach (KeyValuePair<Relationships._clique, List<data_girls.girls>> entry in selectedByClique)
            {
                AddBulliesToClique(entry.Key, entry.Value, selectedGirl);
            }

            data_girls.girls victim = selectedGirl;
            RefreshGirlAndList(victim);
            string message = string.Format(
                CheatUi.Culture, BullyingText(BulliesAddedKey), bullies.Count, SafeGirlName(victim));
            Close();
            NotifySuccess(message, NotificationManager._notification._type.idol_relationship_change);
        }

        private static void AddBulliesToClique(
            Relationships._clique clique,
            List<data_girls.girls> addedBullies,
            data_girls.girls victim)
        {
            // Vanilla AddBulliedGirl recruits the entire clique, changes relationships,
            // and may eject a member dating the victim. Set only the requested bullying
            // links using the game's persisted per-member opt-out list instead.
            bool alreadyTargeted = clique.Bullied_Girls.Contains(victim);
            if (!alreadyTargeted)
            {
                clique.Bullied_Girls.Add(victim);
            }

            Relationships._clique._stopped_bullying exclusions = clique.GetStoppedBullying(victim);
            if (exclusions == null)
            {
                exclusions = new Relationships._clique._stopped_bullying { Target = victim };
                clique.StoppedBullying.Add(exclusions);
            }
            foreach (data_girls.girls member in clique.Members)
            {
                if (member == victim || (!alreadyTargeted && !addedBullies.Contains(member)))
                {
                    if (!exclusions.Girls.Contains(member))
                    {
                        exclusions.Girls.Add(member);
                    }
                }
            }
            foreach (data_girls.girls bully in addedBullies)
            {
                exclusions.Girls.RemoveAll(delegate(data_girls.girls girl) { return girl == bully; });
            }
            clique.AddKnownBulliedGirl(victim);
        }
    }
}
