using System;
using System.Collections.Generic;
using ModLocalizationSystem;
using CheatsMod.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static class IdolRelationshipCheatPopup
    {
        private const int PopupTypeValue = 1431194195;
        private const float PanelWidth = 1160f;
        private const float PanelHeight = 700f;
        private const float Margin = 24f;
        private const float TargetPickerWidth = 620f;
        private const float RelationshipListWidth = 700f;
        private const float ColumnGap = 22f;
        private const float BodyTop = 82f;
        private const float BodyBottom = 70f;
        private const float PickerLabelHeight = 30f;
        private const float FooterButtonWidth = 160f;
        private const float FooterButtonHeight = 38f;
        private const float FooterGap = 16f;
        private const float RelationshipRowHeight = 130f;
        private const float RelationshipCardLeft = 18f;
        private const float RelationshipCardTop = 3f;
        private const float RelationshipEditLeft = 252f;
        private const float RelationshipEditTop = 46f;
        private const float RelationshipEditSize = 34f;
        private const float RelationshipInfoLeft = 302f;
        private const byte SelectionAlpha = 72;

        private const string SelectTargetKey = "ui.relationship_cheat.select_target";
        private const string SelectedTargetKey = "ui.relationship_cheat.target";
        private const string SelectedCountKey = "ui.relationship_cheat.selected_count";
        private const string NextKey = "ui.relationship_cheat.next";
        private const string BackKey = "ui.relationship_cheat.back";
        private const string ApplyKey = "ui.relationship_cheat.apply";
        private const string NoPeersKey = "notification.relationship_cheat.no_available";
        private const string AdjustedKey = "notification.relationship_cheat.adjusted";
        private const string NoChangesKey = "notification.relationship_cheat.no_changes";
        private const string FailedKey = "notification.selected_idol_cheat_failed";

        private enum Mode
        {
            Friends,
            BestFriends,
            Disliked,
            Hated
        }

        private sealed class RelationshipDraft
        {
            internal data_girls.girls Peer;
            internal float ValuePercent;
            internal Relationships._relationship._dynamic Trend;
            internal bool Dirty;
        }

        private sealed class RelationshipRowUi
        {
            internal GameObject Highlight;
            internal TextMeshProUGUI Info;
        }

        private static GameObject popupRoot;
        private static Transform panel;
        private static TextMeshProUGUI title;
        private static TextMeshProUGUI pickerHeading;
        private static GameObject pickerRoot;
        private static TextMeshProUGUI selectedText;
        private static TextMeshProUGUI detailText;
        private static TextMeshProUGUI selectedNamesText;
        private static Button backButton;
        private static Button cancelButton;
        private static Button continueButton;
        private static data_girls.girls targetGirl;
        private static Mode currentMode;
        private static bool selectingPeers;
        private static readonly HashSet<int> selectedPeerIds = new HashSet<int>();
        private static readonly Dictionary<int, GameObject> targetHighlights = new Dictionary<int, GameObject>();
        private static readonly Dictionary<int, RelationshipDraft> drafts = new Dictionary<int, RelationshipDraft>();
        private static readonly Dictionary<int, RelationshipRowUi> peerRows = new Dictionary<int, RelationshipRowUi>();

        internal static void OpenFriends() { Open(Mode.Friends); }
        internal static void OpenBestFriends() { Open(Mode.BestFriends); }
        internal static void OpenDisliked() { Open(Mode.Disliked); }
        internal static void OpenHated() { Open(Mode.Hated); }

        private static void Open(Mode mode)
        {
            try
            {
                PopupManager manager = GetPopupManager();
                if (manager == null || data_girls.girl == null)
                {
                    NotifyWarning("notification.game_unavailable");
                    return;
                }

                List<data_girls.girls> girls = BuildAvailableGirls();
                if (girls.Count == 0)
                {
                    NotifyWarning("notification.no_selectable_idols");
                    return;
                }

                GameObject prefab = GetGirlButtonPrefab(manager);
                if (prefab == null)
                {
                    NotifyWarning(FailedKey);
                    return;
                }

                ResetState();
                currentMode = mode;
                DestroyExistingRoot();
                popupRoot = CheatUi.CreateShell(manager, "RelationshipCheatPopup", new Vector2(PanelWidth, PanelHeight), out panel);
                title = CheatUi.Label(panel, "Title", GetTitle(), CheatUi.TitleFontSize,
                    TextAlignmentOptions.Center, mainScript.black32);
                CheatUi.Place(title.rectTransform, Margin, CheatUi.TitleInset,
                    PanelWidth - Margin * 2f, CheatUi.TitleHeight);

                CreateTargetPicker(girls, prefab);
                CreateDetails();
                CreateFooter();
                if (!CheatUi.Register(PopupTypeValue, popupRoot))
                {
                    DestroyExistingRoot();
                    NotifyWarning(FailedKey);
                    return;
                }

                Refresh();
                PopupManager.OpenPopup((PopupManager._type)PopupTypeValue);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Relationship adjust popup failed: " + exception);
                NotifyWarning(FailedKey);
            }
        }

        private static void ResetState()
        {
            selectingPeers = false;
            targetGirl = null;
            selectedPeerIds.Clear();
            targetHighlights.Clear();
            drafts.Clear();
            peerRows.Clear();
            panel = null;
            title = null;
            pickerHeading = null;
            pickerRoot = null;
            selectedText = null;
            detailText = null;
            selectedNamesText = null;
            backButton = null;
            cancelButton = null;
            continueButton = null;
        }

        private static List<data_girls.girls> BuildAvailableGirls()
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl != null
                    && girl.Type == data_girls.girls._type.NORMAL
                    && girl.status != data_girls._status.graduated)
                {
                    result.Add(girl);
                }
            }
            result.Sort(delegate(data_girls.girls left, data_girls.girls right)
            {
                return string.Compare(SafeName(left), SafeName(right), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static List<data_girls.girls> BuildPeerChoices()
        {
            List<data_girls.girls> result = BuildAvailableGirls();
            result.RemoveAll(delegate(data_girls.girls girl)
            {
                return girl == targetGirl || Relationships.GetRelationship(targetGirl, girl) == null;
            });
            return result;
        }

        private static void CreateTargetPicker(List<data_girls.girls> girls, GameObject prefab)
        {
            targetHighlights.Clear();
            pickerHeading = CheatUi.LabelAt(panel, SelectTargetKey, Margin, BodyTop, TargetPickerWidth, PickerLabelHeight);
            IMUiScrollViewHandle picker = CheatUi.Picker(panel, "RelationshipTargetPicker", Margin,
                BodyTop + PickerLabelHeight, TargetPickerWidth,
                PanelHeight - BodyTop - BodyBottom - PickerLabelHeight,
                girls, prefab, SelectTarget, delegate(GameObject card, data_girls.girls girl)
                {
                    GameObject highlight = CheatUi.Object("RelationshipTargetSelection", card.transform);
                    CheatUi.Stretch(highlight.GetComponent<RectTransform>());
                    Image image = highlight.AddComponent<Image>();
                    Color32 color = mainScript.green32;
                    color.a = SelectionAlpha;
                    image.color = color;
                    image.raycastTarget = false;
                    highlight.SetActive(false);
                    targetHighlights[girl.id] = highlight;
                });
            pickerRoot = picker.Root;
        }

        private static void CreateRelationshipList(List<data_girls.girls> peers, GameObject prefab)
        {
            peerRows.Clear();
            pickerHeading = CheatUi.LabelAt(panel, GetSelectPeersKey(), Margin, BodyTop,
                RelationshipListWidth, PickerLabelHeight);
            pickerHeading.text = Text(GetSelectPeersKey());
            IMUiScrollViewHandle scroll = CheatUi.Scroll(panel, "RelationshipAdjustList", Vector2.zero, Vector2.zero);
            CheatUi.Place(scroll.Root.GetComponent<RectTransform>(), Margin, BodyTop + PickerLabelHeight,
                RelationshipListWidth, PanelHeight - BodyTop - BodyBottom - PickerLabelHeight);
            VerticalLayoutGroup layout = scroll.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = scroll.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (data_girls.girls peer in peers) CreateRelationshipRow(scroll.Content, prefab, peer);
            pickerRoot = scroll.Root;
            scroll.ScrollRect.verticalNormalizedPosition = 1f;
        }

        private static void CreateRelationshipRow(Transform parent, GameObject prefab, data_girls.girls peer)
        {
            GameObject root = CheatUi.Object("RelationshipRow_" + peer.id, parent);
            LayoutElement rowLayout = root.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = RelationshipRowHeight;
            rowLayout.minHeight = RelationshipRowHeight;
            rowLayout.flexibleWidth = 1f;
            Image surface = root.AddComponent<Image>();
            IMUiPrimitives.TryCopyVanillaPanelVisual(surface);
            surface.color = new Color32(248, 246, 250, 255);
            surface.raycastTarget = false;

            GameObject card = UnityEngine.Object.Instantiate(prefab, root.transform, false);
            IMUiKit.ApplyLayerRecursively(card, panel.gameObject.layer);
            GirlButtonSmall nativeCard = card.GetComponent<GirlButtonSmall>();
            if (nativeCard != null)
            {
                nativeCard.DontDisableIfTraining = nativeCard.DontDisableIfHiatus = true;
                nativeCard.SetGirl(peer, false);
            }
            IMUiKit.RebindAllButtons(card, delegate { TogglePeer(peer); });
            IMUiKit.ActivateButtonDefaults(card);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
                CheatUi.Place(cardRect, RelationshipCardLeft, RelationshipCardTop,
                    CheatUi.PickerCellWidth, CheatUi.PickerCellHeight);

            GameObject highlight = CheatUi.Object("RelationshipSelection", card.transform);
            CheatUi.Stretch(highlight.GetComponent<RectTransform>());
            Image highlightImage = highlight.AddComponent<Image>();
            Color32 selectionColor = mainScript.green32;
            selectionColor.a = SelectionAlpha;
            highlightImage.color = selectionColor;
            highlightImage.raycastTarget = false;
            highlight.SetActive(false);

            Button edit = CheatUi.NumericButton(root.transform, CheatUi.NumericAction.Edit,
                delegate { OpenBondEditor(peer); });
            CheatUi.Place(edit.GetComponent<RectTransform>(), RelationshipEditLeft, RelationshipEditTop,
                RelationshipEditSize, RelationshipEditSize);

            TextMeshProUGUI info = CheatUi.Label(root.transform, "BondInfo", string.Empty,
                CheatUi.SmallFontSize, TextAlignmentOptions.MidlineLeft, mainScript.black32);
            CheatUi.Place(info.rectTransform, RelationshipInfoLeft, 14f,
                RelationshipListWidth - RelationshipInfoLeft - 36f, 102f);
            info.enableWordWrapping = true;

            peerRows[peer.id] = new RelationshipRowUi { Highlight = highlight, Info = info };
        }

        private static void ReplacePicker(Action create)
        {
            targetHighlights.Clear();
            peerRows.Clear();
            if (pickerHeading != null)
            {
                pickerHeading.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(pickerHeading.gameObject);
                pickerHeading = null;
            }
            if (pickerRoot != null)
            {
                pickerRoot.SetActive(false);
                UnityEngine.Object.Destroy(pickerRoot);
                pickerRoot = null;
            }
            create();
        }

        private static void CreateDetails()
        {
            float left = Margin + RelationshipListWidth + ColumnGap;
            float rightWidth = PanelWidth - left - Margin;
            selectedText = CheatUi.Label(panel, "SelectedTarget", string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(selectedText.rectTransform, left, BodyTop, rightWidth, 40f);
            detailText = CheatUi.Label(panel, "Instructions", string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(detailText.rectTransform, left, BodyTop + 54f, rightWidth, 156f);
            selectedNamesText = CheatUi.Label(panel, "SelectedNames", string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(selectedNamesText.rectTransform, left, BodyTop + 226f, rightWidth, 270f);
            selectedNamesText.enableAutoSizing = true;
            selectedNamesText.enableWordWrapping = true;
        }

        private static void CreateFooter()
        {
            backButton = CheatUi.Button(panel, "Back", Text(BackKey), FooterButtonWidth,
                FooterButtonHeight, Back);
            cancelButton = CheatUi.Button(panel, "Cancel", CheatUi.Text(CheatUi.CancelKey), FooterButtonWidth,
                FooterButtonHeight, Close);
            continueButton = CheatUi.Button(panel, "Continue", Text(NextKey), FooterButtonWidth,
                FooterButtonHeight, Continue);
            RefreshFooter();
        }

        private static void RefreshFooter()
        {
            if (cancelButton == null || continueButton == null) return;
            int count = selectingPeers ? 3 : 2;
            float width = count * FooterButtonWidth + (count - 1) * FooterGap;
            float left = (PanelWidth - width) * CheatUi.Center;
            float top = PanelHeight - CheatUi.FooterInset;

            if (backButton != null)
            {
                backButton.gameObject.SetActive(selectingPeers);
                if (selectingPeers)
                {
                    CheatUi.Place(backButton.GetComponent<RectTransform>(), left, top,
                        FooterButtonWidth, FooterButtonHeight);
                    left += FooterButtonWidth + FooterGap;
                }
            }
            CheatUi.Place(cancelButton.GetComponent<RectTransform>(), left, top,
                FooterButtonWidth, FooterButtonHeight);
            left += FooterButtonWidth + FooterGap;
            CheatUi.Place(continueButton.GetComponent<RectTransform>(), left, top,
                FooterButtonWidth, FooterButtonHeight);
        }

        private static void SelectTarget(data_girls.girls girl)
        {
            if (girl == null || selectingPeers) return;
            targetGirl = girl;
            Refresh();
        }

        private static void EnterPeerAdjustment(GameObject prefab)
        {
            List<data_girls.girls> peers = BuildPeerChoices();
            if (peers.Count == 0)
            {
                NotifyWarning(NoPeersKey);
                return;
            }

            selectingPeers = true;
            selectedPeerIds.Clear();
            drafts.Clear();
            foreach (data_girls.girls peer in peers)
            {
                Relationships._relationship relationship = Relationships.GetRelationship(targetGirl, peer);
                if (relationship == null) continue;
                RelationshipDraft draft = new RelationshipDraft
                {
                    Peer = peer,
                    ValuePercent = Mathf.Clamp(relationship.Ratio * 100f, 0f, 100f),
                    Trend = relationship.Dynamic,
                    Dirty = false
                };
                drafts[peer.id] = draft;
                if (IsModeStatus(relationship.Status)) selectedPeerIds.Add(peer.id);
            }
            ReplacePicker(delegate { CreateRelationshipList(peers, prefab); });
            Refresh();
        }

        private static void TogglePeer(data_girls.girls peer)
        {
            RelationshipDraft draft;
            if (peer == null || !drafts.TryGetValue(peer.id, out draft)) return;
            if (selectedPeerIds.Contains(peer.id))
            {
                draft.ValuePercent = 50f;
                draft.Trend = Relationships._relationship._dynamic.neutral;
            }
            else
            {
                draft.ValuePercent = GetDefaultPercent();
                draft.Trend = GetDefaultTrend();
            }
            draft.Dirty = true;
            SyncSelectionFromDraft(draft);
            Refresh();
        }

        private static void OpenBondEditor(data_girls.girls peer)
        {
            RelationshipDraft draft;
            if (peer == null || !drafts.TryGetValue(peer.id, out draft) || targetGirl == null) return;
            string editorTitle = string.Format(CheatUi.Culture, Text("ui.relationship_cheat.edit_bond_title"),
                SafeName(targetGirl), SafeName(peer));
            RelationshipBondEditor.Show(panel, editorTitle, draft.ValuePercent, draft.Trend,
                delegate(float value, Relationships._relationship._dynamic trend)
                {
                    draft.ValuePercent = NormalizePercent(value);
                    draft.Trend = trend;
                    draft.Dirty = true;
                    SyncSelectionFromDraft(draft);
                    Refresh();
                });
        }

        private static void SyncSelectionFromDraft(RelationshipDraft draft)
        {
            if (draft == null || draft.Peer == null) return;
            if (IsModeStatus(GetStatusForPercent(draft.ValuePercent))) selectedPeerIds.Add(draft.Peer.id);
            else selectedPeerIds.Remove(draft.Peer.id);
        }

        private static void Refresh()
        {
            bool targetValid = targetGirl != null && BuildAvailableGirls().Contains(targetGirl);
            if (selectedText != null)
                selectedText.text = targetValid
                    ? string.Format(CheatUi.Culture, Text(SelectedTargetKey), SafeName(targetGirl))
                    : Text(SelectTargetKey);
            if (detailText != null)
                detailText.text = Text(selectingPeers ? GetPeerInstructionsKey() : GetTargetInstructionsKey());

            if (!selectingPeers)
            {
                if (pickerHeading != null) pickerHeading.text = Text(SelectTargetKey);
                foreach (KeyValuePair<int, GameObject> pair in targetHighlights)
                    if (pair.Value != null) pair.Value.SetActive(targetValid && targetGirl.id == pair.Key);
                if (selectedNamesText != null)
                {
                    selectedNamesText.gameObject.SetActive(false);
                    selectedNamesText.text = string.Empty;
                }
            }
            else
            {
                if (pickerHeading != null) pickerHeading.text = Text(GetSelectPeersKey());
                foreach (KeyValuePair<int, RelationshipRowUi> pair in peerRows)
                {
                    RelationshipDraft draft;
                    if (!drafts.TryGetValue(pair.Key, out draft) || pair.Value == null) continue;
                    bool selected = selectedPeerIds.Contains(pair.Key);
                    if (pair.Value.Highlight != null) pair.Value.Highlight.SetActive(selected);
                    if (pair.Value.Info != null) pair.Value.Info.text = GetDraftInfo(draft);
                }
                if (selectedNamesText != null)
                {
                    selectedNamesText.gameObject.SetActive(true);
                    selectedNamesText.text = string.Format(CheatUi.Culture, Text(SelectedCountKey), selectedPeerIds.Count)
                        + "\n\n" + Text("ui.relationship_cheat.adjust_hint")
                        + "\n\n" + Text("ui.relationship_cheat.trend_persistence_note");
                }
            }

            if (continueButton != null)
            {
                CheatUi.SetButtonText(continueButton, Text(selectingPeers ? ApplyKey : NextKey));
                CheatUi.SetButtonInteractable(continueButton, targetValid);
            }
            RefreshFooter();
        }

        private static string GetDraftInfo(RelationshipDraft draft)
        {
            Relationships._relationship._status status = GetStatusForPercent(draft.ValuePercent);
            return string.Format(CheatUi.Culture, Text("ui.relationship_cheat.value_line"),
                    draft.ValuePercent.ToString("0.#", CheatUi.Culture))
                + "\n" + string.Format(CheatUi.Culture, Text("ui.relationship_cheat.status_line"), GetStatusText(status))
                + "\n" + string.Format(CheatUi.Culture, Text("ui.relationship_cheat.trend_line"), GetTrendText(draft.Trend));
        }

        private static void Continue()
        {
            try
            {
                if (targetGirl == null)
                {
                    NotifyWarning("notification.no_selected_idol");
                    return;
                }
                if (selectingPeers)
                {
                    Apply();
                    return;
                }

                PopupManager manager = GetPopupManager();
                GameObject prefab = manager == null ? null : GetGirlButtonPrefab(manager);
                if (prefab == null)
                {
                    NotifyWarning(FailedKey);
                    return;
                }
                EnterPeerAdjustment(prefab);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Relationship adjust selection failed: " + exception);
                NotifyWarning(FailedKey);
            }
        }

        private static void Back()
        {
            if (!selectingPeers) return;
            try
            {
                RelationshipBondEditor.CancelActive();
                PopupManager manager = GetPopupManager();
                GameObject prefab = manager == null ? null : GetGirlButtonPrefab(manager);
                if (prefab == null)
                {
                    NotifyWarning(FailedKey);
                    return;
                }
                selectingPeers = false;
                selectedPeerIds.Clear();
                drafts.Clear();
                ReplacePicker(delegate { CreateTargetPicker(BuildAvailableGirls(), prefab); });
                Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Relationship adjust back failed: " + exception);
                NotifyWarning(FailedKey);
            }
        }

        private static void Apply()
        {
            int count = 0;
            foreach (KeyValuePair<int, RelationshipDraft> pair in drafts)
            {
                RelationshipDraft draft = pair.Value;
                if (draft == null || !draft.Dirty || draft.Peer == null) continue;
                Relationships._relationship relationship = Relationships.GetRelationship(targetGirl, draft.Peer);
                if (relationship == null) continue;
                draft.ValuePercent = SetRelationshipValue(relationship, draft.ValuePercent);
                relationship.Dynamic = draft.Trend;
                count++;
                RefreshGirl(draft.Peer);
            }
            RefreshGirl(targetGirl);
            string targetName = SafeName(targetGirl);
            Close();
            NotificationManager.AddNotification(count == 0
                    ? Text(NoChangesKey)
                    : string.Format(CheatUi.Culture, Text(AdjustedKey), count, targetName),
                mainScript.green32, NotificationManager._notification._type.idol_relationship_change);
        }

        private static float SetRelationshipValue(Relationships._relationship relationship, float percent)
        {
            int positives;
            int total;
            FindBestFraction(percent, out positives, out total);
            relationship.Vals.Clear();
            relationship.Temp = 0f;
            int emittedPositives = 0;
            for (int index = 0; index < total; index++)
            {
                int desiredPositives = Mathf.RoundToInt((index + 1) * positives / (float)total);
                if (desiredPositives > emittedPositives)
                {
                    relationship.Vals.Add(1);
                    emittedPositives++;
                }
                else
                {
                    relationship.Vals.Add(-1);
                }
            }
            relationship.Recalc(true);
            return relationship.Ratio * 100f;
        }

        private static float NormalizePercent(float percent)
        {
            int positives;
            int total;
            FindBestFraction(percent, out positives, out total);
            return 100f * positives / total;
        }

        private static void FindBestFraction(float percent, out int bestPositives, out int bestTotal)
        {
            percent = Mathf.Clamp(percent, 0f, 100f);
            bestPositives = 0;
            bestTotal = 20;
            float bestError = float.MaxValue;
            for (int total = 1; total <= 20; total++)
            {
                for (int positives = 0; positives <= total; positives++)
                {
                    float represented = 100f * positives / total;
                    float error = Mathf.Abs(represented - percent);
                    if (error < bestError - 0.0001f || (Mathf.Abs(error - bestError) <= 0.0001f && total > bestTotal))
                    {
                        bestError = error;
                        bestPositives = positives;
                        bestTotal = total;
                    }
                }
            }
        }

        private static Relationships._relationship._status GetStatusForPercent(float percent)
        {
            float ratio = percent / 100f;
            if (ratio > 0.9f) return Relationships._relationship._status.best_friends;
            if (ratio > 0.7f) return Relationships._relationship._status.friends;
            if (ratio < 0.2f) return Relationships._relationship._status.hates;
            if (ratio < 0.4f) return Relationships._relationship._status.dislikes;
            return Relationships._relationship._status.normal;
        }

        private static bool IsModeStatus(Relationships._relationship._status status)
        {
            return status == (currentMode == Mode.Friends ? Relationships._relationship._status.friends
                : currentMode == Mode.BestFriends ? Relationships._relationship._status.best_friends
                : currentMode == Mode.Disliked ? Relationships._relationship._status.dislikes
                : Relationships._relationship._status.hates);
        }

        private static float GetDefaultPercent()
        {
            return currentMode == Mode.Friends ? 80f
                : currentMode == Mode.BestFriends ? 95f
                : currentMode == Mode.Disliked ? 25f : 10f;
        }

        private static Relationships._relationship._dynamic GetDefaultTrend()
        {
            return currentMode == Mode.Friends || currentMode == Mode.BestFriends
                ? Relationships._relationship._dynamic.positive
                : Relationships._relationship._dynamic.negative;
        }

        private static string GetTitle()
        {
            return Text(currentMode == Mode.Friends ? "ui.relationship_cheat.friends.title"
                : currentMode == Mode.BestFriends ? "ui.relationship_cheat.best_friends.title"
                : currentMode == Mode.Disliked ? "ui.relationship_cheat.disliked.title"
                : "ui.relationship_cheat.hated.title");
        }

        private static string GetTargetInstructionsKey()
        {
            return currentMode == Mode.Friends ? "ui.relationship_cheat.friends.target_instructions"
                : currentMode == Mode.BestFriends ? "ui.relationship_cheat.best_friends.target_instructions"
                : currentMode == Mode.Disliked ? "ui.relationship_cheat.disliked.target_instructions"
                : "ui.relationship_cheat.hated.target_instructions";
        }

        private static string GetSelectPeersKey()
        {
            return currentMode == Mode.Friends ? "ui.relationship_cheat.friends.select"
                : currentMode == Mode.BestFriends ? "ui.relationship_cheat.best_friends.select"
                : currentMode == Mode.Disliked ? "ui.relationship_cheat.disliked.select"
                : "ui.relationship_cheat.hated.select";
        }

        private static string GetPeerInstructionsKey()
        {
            return currentMode == Mode.Friends ? "ui.relationship_cheat.friends.peer_instructions"
                : currentMode == Mode.BestFriends ? "ui.relationship_cheat.best_friends.peer_instructions"
                : currentMode == Mode.Disliked ? "ui.relationship_cheat.disliked.peer_instructions"
                : "ui.relationship_cheat.hated.peer_instructions";
        }

        private static string GetStatusText(Relationships._relationship._status status)
        {
            return Text(status == Relationships._relationship._status.best_friends ? "ui.relationship_cheat.status.best_friends"
                : status == Relationships._relationship._status.friends ? "ui.relationship_cheat.status.friends"
                : status == Relationships._relationship._status.dislikes ? "ui.relationship_cheat.status.dislikes"
                : status == Relationships._relationship._status.hates ? "ui.relationship_cheat.status.hates"
                : "ui.relationship_cheat.status.normal");
        }

        private static string GetTrendText(Relationships._relationship._dynamic trend)
        {
            return Text(trend == Relationships._relationship._dynamic.positive ? "ui.relationship_cheat.trend.positive"
                : trend == Relationships._relationship._dynamic.negative ? "ui.relationship_cheat.trend.negative"
                : "ui.relationship_cheat.trend.neutral");
        }

        private static string Text(string key)
        {
            return ModLocalization.Get(key, key);
        }

        private static string SafeName(data_girls.girls girl)
        {
            if (girl == null) return string.Empty;
            try { return girl.GetName(true); }
            catch { return string.Empty; }
        }

        private static void RefreshGirl(data_girls.girls girl)
        {
            if (girl != null && girl.Update != null) girl.Update();
            data_girls dataGirls = GetDataComponent<data_girls>();
            if (dataGirls != null) dataGirls.UpdateList(true);
        }

        private static void NotifyWarning(string key)
        {
            NotificationManager.AddNotification(Text(key), mainScript.red32,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void Close()
        {
            RelationshipBondEditor.CancelActive();
            ResetState();
            PopupManager.Close_();
        }

        private static void DestroyExistingRoot()
        {
            if (popupRoot != null)
            {
                UnityEngine.Object.Destroy(popupRoot);
                popupRoot = null;
            }
        }

        private static PopupManager GetPopupManager()
        {
            GameObject data = GetMainData();
            return data == null ? null : data.GetComponent<PopupManager>();
        }

        private static T GetDataComponent<T>() where T : Component
        {
            GameObject data = GetMainData();
            return data == null ? null : data.GetComponent<T>();
        }

        private static GameObject GetMainData()
        {
            Camera camera = Camera.main;
            if (camera == null) return null;
            mainScript main = camera.GetComponent<mainScript>();
            return main == null ? null : main.Data;
        }

        private static GameObject GetGirlButtonPrefab(PopupManager manager)
        {
            PopupManager._popup entry = manager.GetByType(PopupManager._type.girl_styling);
            if (entry == null || entry.obj == null) return null;
            Styling_Popup popup = entry.obj.GetComponent<Styling_Popup>();
            return popup == null ? null : popup.prefab_girl_button;
        }
    }

    internal sealed class RelationshipBondEditor : MonoBehaviour
    {
        private const float Width = 620f;
        private const float Height = 420f;
        private const float Margin = 24f;
        private const float ValueTop = 104f;
        private const float TrendLabelTop = 172f;
        private const float TrendButtonTop = 210f;
        private const float TrendButtonHeight = 40f;
        private const float TrendGap = 12f;
        private const float NoteTop = 270f;
        private const float FooterTop = 354f;
        private const float FooterWidth = 190f;
        private const float FooterGap = 32f;
        private const string ObjectName = "RelationshipBondEditor";
        private static RelationshipBondEditor activeEditor;

        private float valuePercent;
        private Relationships._relationship._dynamic trend;
        private Action<float, Relationships._relationship._dynamic> submit;
        private CheatNumericRow valueRow;
        private Button positiveButton;
        private Button neutralButton;
        private Button negativeButton;

        internal static void Show(Transform owner, string title, float value,
            Relationships._relationship._dynamic currentTrend,
            Action<float, Relationships._relationship._dynamic> onSubmit)
        {
            CheatNameEditor.CancelActive();
            CheatNumericEditor.CancelActive();
            CancelActive();
            Transform panel;
            GameObject overlay = CheatUi.CreateInputSheet(owner, ObjectName, title,
                new Vector2(Width, Height), out panel);
            RelationshipBondEditor editor = overlay.AddComponent<RelationshipBondEditor>();
            activeEditor = editor;
            editor.valuePercent = Mathf.Clamp(value, 0f, 100f);
            editor.trend = currentTrend == Relationships._relationship._dynamic.NONE
                ? Relationships._relationship._dynamic.neutral : currentTrend;
            editor.submit = onSubmit;

            editor.valueRow = new CheatNumericRow(panel, "ui.relationship_cheat.edit_value",
                Margin, ValueTop, 0f, 100f,
                delegate { return editor.valuePercent; },
                delegate(float next) { editor.valuePercent = next; }, true);

            CheatUi.LabelAt(panel, "ui.relationship_cheat.edit_trend", Margin, TrendLabelTop,
                Width - Margin * 2f, 30f);
            float buttonWidth = (Width - Margin * 2f - TrendGap * 2f) / 3f;
            editor.positiveButton = CheatUi.Button(panel, "PositiveTrend",
                ModLocalization.Get("ui.relationship_cheat.trend.positive", "Positive"),
                buttonWidth, TrendButtonHeight,
                delegate { editor.SetTrend(Relationships._relationship._dynamic.positive); });
            editor.neutralButton = CheatUi.Button(panel, "NeutralTrend",
                ModLocalization.Get("ui.relationship_cheat.trend.neutral", "Neutral"),
                buttonWidth, TrendButtonHeight,
                delegate { editor.SetTrend(Relationships._relationship._dynamic.neutral); });
            editor.negativeButton = CheatUi.Button(panel, "NegativeTrend",
                ModLocalization.Get("ui.relationship_cheat.trend.negative", "Negative"),
                buttonWidth, TrendButtonHeight,
                delegate { editor.SetTrend(Relationships._relationship._dynamic.negative); });
            CheatUi.Place(editor.positiveButton.GetComponent<RectTransform>(), Margin, TrendButtonTop,
                buttonWidth, TrendButtonHeight);
            CheatUi.Place(editor.neutralButton.GetComponent<RectTransform>(), Margin + buttonWidth + TrendGap,
                TrendButtonTop, buttonWidth, TrendButtonHeight);
            CheatUi.Place(editor.negativeButton.GetComponent<RectTransform>(),
                Margin + (buttonWidth + TrendGap) * 2f, TrendButtonTop, buttonWidth, TrendButtonHeight);

            TextMeshProUGUI note = CheatUi.Label(panel, "TrendPersistenceNote",
                ModLocalization.Get("ui.relationship_cheat.editor_note", string.Empty),
                CheatUi.SmallFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(note.rectTransform, Margin, NoteTop, Width - Margin * 2f, 62f);
            note.enableWordWrapping = true;

            CheatUi.ButtonAt(panel, CheatUi.CancelKey, Margin, FooterTop, FooterWidth,
                delegate { editor.Cancel(); });
            CheatUi.ButtonAt(panel, CheatUi.ApplyKey, Margin + FooterWidth + FooterGap,
                FooterTop, FooterWidth, editor.Save);
            editor.RefreshTrendButtons();
        }

        private void SetTrend(Relationships._relationship._dynamic next)
        {
            trend = next;
            RefreshTrendButtons();
        }

        private void RefreshTrendButtons()
        {
            if (positiveButton != null)
                CheatUi.SetButtonInteractable(positiveButton, trend != Relationships._relationship._dynamic.positive);
            if (neutralButton != null)
                CheatUi.SetButtonInteractable(neutralButton, trend != Relationships._relationship._dynamic.neutral);
            if (negativeButton != null)
                CheatUi.SetButtonInteractable(negativeButton, trend != Relationships._relationship._dynamic.negative);
            if (valueRow != null) valueRow.Refresh();
        }

        private void Save()
        {
            Action<float, Relationships._relationship._dynamic> callback = submit;
            float value = valuePercent;
            Relationships._relationship._dynamic selectedTrend = trend;
            Cancel();
            if (callback != null) callback(value, selectedTrend);
        }

        private void Cancel()
        {
            CheatNumericEditor.CancelActive();
            if (activeEditor == this) activeEditor = null;
            submit = null;
            gameObject.SetActive(false);
            UnityEngine.Object.Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (activeEditor == this) activeEditor = null;
            submit = null;
        }

        internal static bool CancelActive()
        {
            if (activeEditor == null || !activeEditor.gameObject.activeInHierarchy) return false;
            activeEditor.Cancel();
            return true;
        }
    }

    internal static class CliqueCheatPopup
    {
        private const int PopupTypeValue = 1431194196;
        private const float PanelWidth = 1160f;
        private const float PanelHeight = 700f;
        private const float Margin = 24f;
        private const float BodyTop = 82f;
        private const float BodyBottom = 70f;
        private const float FooterButtonWidth = 180f;
        private const float FooterButtonHeight = 38f;
        private const float FooterGap = 16f;
        private const int MaximumCliqueMembers = 5;
        private const byte SelectionAlpha = 72;

        private static GameObject popupRoot;
        private static Transform panel;
        private static Relationships._clique editingClique;
        private static bool creatingClique;
        private static bool hostileRemoval;
        private static bool confirmingDisband;
        private static readonly HashSet<int> selectedMemberIds = new HashSet<int>();
        private static readonly Dictionary<int, GameObject> highlights = new Dictionary<int, GameObject>();
        private static TextMeshProUGUI memberSummary;
        private static TextMeshProUGUI instructions;
        private static Button peacefulButton;
        private static Button hostileButton;
        private static Button applyButton;
        private static Button disbandButton;

        internal static void Open()
        {
            try
            {
                PopupManager manager = GetPopupManager();
                if (manager == null || data_girls.girl == null)
                {
                    NotifyWarning("notification.game_unavailable");
                    return;
                }
                DestroyExistingRoot();
                popupRoot = CheatUi.CreateShell(manager, "CliqueCheatPopup", new Vector2(PanelWidth, PanelHeight), out panel);
                ShowCliqueList();
                if (!CheatUi.Register(PopupTypeValue, popupRoot))
                {
                    DestroyExistingRoot();
                    NotifyWarning("notification.selected_idol_cheat_failed");
                    return;
                }
                PopupManager.OpenPopup((PopupManager._type)PopupTypeValue);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Clique cheat popup failed: " + exception);
                NotifyWarning("notification.selected_idol_cheat_failed");
            }
        }

        private static void ShowCliqueList()
        {
            ClearPanel();
            editingClique = null;
            creatingClique = false;
            confirmingDisband = false;
            TextMeshProUGUI title = CheatUi.Label(panel, "Title", Text("ui.clique_cheat.title"),
                CheatUi.TitleFontSize, TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(title.rectTransform, Margin, CheatUi.TitleInset,
                PanelWidth - Margin * 2f, CheatUi.TitleHeight);

            TextMeshProUGUI description = CheatUi.Label(panel, "Description", Text("ui.clique_cheat.list_instructions"),
                CheatUi.BodyFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(description.rectTransform, Margin, BodyTop, PanelWidth - Margin * 2f, 54f);

            IMUiScrollViewHandle scroll = CheatUi.Scroll(panel, "CliqueList", Vector2.zero, Vector2.zero);
            CheatUi.Place(scroll.Root.GetComponent<RectTransform>(), Margin, BodyTop + 62f,
                PanelWidth - Margin * 2f, PanelHeight - BodyTop - BodyBottom - 62f);
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

            List<Relationships._clique> cliques = GetValidCliques();
            PopupManager manager = GetPopupManager();
            GameObject prefab = manager == null ? null : GetGirlButtonPrefab(manager);
            if (cliques.Count == 0)
            {
                TextMeshProUGUI empty = CheatUi.Label(scroll.Content, "NoCliques", Text("ui.clique_cheat.none"),
                    CheatUi.BodyFontSize, TextAlignmentOptions.Center, mainScript.black32);
                LayoutElement emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 52f;
            }
            else if (prefab == null)
            {
                TextMeshProUGUI unavailable = CheatUi.Label(scroll.Content, "CliqueCardsUnavailable",
                    Text("notification.selected_idol_cheat_failed"), CheatUi.BodyFontSize,
                    TextAlignmentOptions.Center, mainScript.black32);
                LayoutElement unavailableLayout = unavailable.gameObject.AddComponent<LayoutElement>();
                unavailableLayout.preferredHeight = 52f;
            }
            else
            {
                float cardWidth = PanelWidth - Margin * 2f - 50f;
                for (int index = 0; index < cliques.Count; index++)
                {
                    int displayIndex = index + 1;
                    Relationships._clique clique = cliques[index];
                    GameObject unusedHighlight;
                    CliqueDisplayUi.CreateCard(scroll.Content, "Clique" + index, clique, displayIndex,
                        cardWidth, prefab, delegate { BeginEdit(clique); }, out unusedHighlight);
                }
            }

            Button create = CheatUi.Button(panel, "CreateClique", Text("ui.clique_cheat.create"),
                FooterButtonWidth, FooterButtonHeight, BeginCreate);
            Button close = CheatUi.Button(panel, "Close", CheatUi.Text(CheatUi.CloseKey),
                FooterButtonWidth, FooterButtonHeight, Close);
            float width = FooterButtonWidth * 2f + FooterGap;
            float left = (PanelWidth - width) * CheatUi.Center;
            float top = PanelHeight - CheatUi.FooterInset;
            CheatUi.Place(create.GetComponent<RectTransform>(), left, top, FooterButtonWidth, FooterButtonHeight);
            CheatUi.Place(close.GetComponent<RectTransform>(), left + FooterButtonWidth + FooterGap, top,
                FooterButtonWidth, FooterButtonHeight);
        }

        private static void BeginEdit(Relationships._clique clique)
        {
            if (clique == null || !Relationships.Cliques.Contains(clique))
            {
                ShowCliqueList();
                return;
            }
            editingClique = clique;
            creatingClique = false;
            hostileRemoval = false;
            confirmingDisband = false;
            selectedMemberIds.Clear();
            foreach (data_girls.girls girl in clique.Members)
                if (IsAvailableGirl(girl)) selectedMemberIds.Add(girl.id);
            ShowMemberEditor();
        }

        private static void BeginCreate()
        {
            editingClique = null;
            creatingClique = true;
            hostileRemoval = false;
            confirmingDisband = false;
            selectedMemberIds.Clear();
            ShowMemberEditor();
        }

        private static void ShowMemberEditor()
        {
            ClearPanel();
            highlights.Clear();
            PopupManager manager = GetPopupManager();
            GameObject prefab = manager == null ? null : GetGirlButtonPrefab(manager);
            if (prefab == null)
            {
                NotifyWarning("notification.selected_idol_cheat_failed");
                ShowCliqueList();
                return;
            }

            string titleText = Text(creatingClique ? "ui.clique_cheat.create_title" : "ui.clique_cheat.edit_title");
            TextMeshProUGUI title = CheatUi.Label(panel, "Title", titleText, CheatUi.TitleFontSize,
                TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(title.rectTransform, Margin, CheatUi.TitleInset,
                PanelWidth - Margin * 2f, CheatUi.TitleHeight);

            const float pickerWidth = 620f;
            TextMeshProUGUI pickerLabel = CheatUi.LabelAt(panel, "ui.clique_cheat.select_members",
                Margin, BodyTop, pickerWidth, 30f);
            pickerLabel.text = Text("ui.clique_cheat.select_members");
            List<data_girls.girls> choices = BuildMemberChoices();
            IMUiScrollViewHandle picker = CheatUi.Picker(panel, "CliqueMemberPicker", Margin, BodyTop + 30f,
                pickerWidth, PanelHeight - BodyTop - BodyBottom - 30f, choices, prefab, ToggleMember,
                delegate(GameObject card, data_girls.girls girl)
                {
                    GameObject highlight = CheatUi.Object("CliqueSelection", card.transform);
                    CheatUi.Stretch(highlight.GetComponent<RectTransform>());
                    Image image = highlight.AddComponent<Image>();
                    Color32 color = mainScript.green32;
                    color.a = SelectionAlpha;
                    image.color = color;
                    image.raycastTarget = false;
                    highlight.SetActive(false);
                    highlights[girl.id] = highlight;
                });

            float rightLeft = Margin + pickerWidth + 26f;
            float rightWidth = PanelWidth - rightLeft - Margin;
            memberSummary = CheatUi.Label(panel, "MemberSummary", string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(memberSummary.rectTransform, rightLeft, BodyTop, rightWidth, 84f);
            instructions = CheatUi.Label(panel, "Instructions", string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(instructions.rectTransform, rightLeft, BodyTop + 92f, rightWidth, 124f);

            if (!creatingClique)
            {
                TextMeshProUGUI modeLabel = CheatUi.Label(panel, "RemovalMode",
                    Text("ui.clique_cheat.removal_mode"), CheatUi.BodyFontSize,
                    TextAlignmentOptions.TopLeft, mainScript.black32);
                CheatUi.Place(modeLabel.rectTransform, rightLeft, BodyTop + 228f, rightWidth, 32f);
                float half = (rightWidth - 12f) / 2f;
                peacefulButton = CheatUi.Button(panel, "Peaceful", Text("ui.clique_cheat.peaceful"),
                    half, 40f, delegate { SetRemovalMode(false); });
                hostileButton = CheatUi.Button(panel, "Hostile", Text("ui.clique_cheat.hostile"),
                    half, 40f, delegate { SetRemovalMode(true); });
                CheatUi.Place(peacefulButton.GetComponent<RectTransform>(), rightLeft, BodyTop + 264f, half, 40f);
                CheatUi.Place(hostileButton.GetComponent<RectTransform>(), rightLeft + half + 12f,
                    BodyTop + 264f, half, 40f);

                disbandButton = CheatUi.Button(panel, "Disband", Text("ui.clique_cheat.disband"),
                    rightWidth, 42f, Disband);
                CheatUi.Place(disbandButton.GetComponent<RectTransform>(), rightLeft, BodyTop + 326f,
                    rightWidth, 42f);
            }

            Button back = CheatUi.Button(panel, "Back", Text("ui.clique_cheat.back"), FooterButtonWidth,
                FooterButtonHeight, ShowCliqueList);
            Button cancel = CheatUi.Button(panel, "Cancel", CheatUi.Text(CheatUi.CancelKey), FooterButtonWidth,
                FooterButtonHeight, Close);
            applyButton = CheatUi.Button(panel, "Apply", Text(creatingClique ? "ui.clique_cheat.create_apply" : "ui.clique_cheat.apply"),
                FooterButtonWidth, FooterButtonHeight, ApplyMembership);
            float footerWidth = FooterButtonWidth * 3f + FooterGap * 2f;
            float footerLeft = (PanelWidth - footerWidth) * CheatUi.Center;
            float footerTop = PanelHeight - CheatUi.FooterInset;
            CheatUi.Place(back.GetComponent<RectTransform>(), footerLeft, footerTop,
                FooterButtonWidth, FooterButtonHeight);
            CheatUi.Place(cancel.GetComponent<RectTransform>(), footerLeft + FooterButtonWidth + FooterGap,
                footerTop, FooterButtonWidth, FooterButtonHeight);
            CheatUi.Place(applyButton.GetComponent<RectTransform>(),
                footerLeft + (FooterButtonWidth + FooterGap) * 2f, footerTop,
                FooterButtonWidth, FooterButtonHeight);
            RefreshMemberEditor();
        }

        private static List<data_girls.girls> BuildMemberChoices()
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (!IsAvailableGirl(girl)) continue;
                Relationships._clique current = girl.GetClique();
                if (creatingClique)
                {
                    if (current == null) result.Add(girl);
                }
                else if (current == null || current == editingClique)
                {
                    result.Add(girl);
                }
            }
            result.Sort(delegate(data_girls.girls left, data_girls.girls right)
            {
                return string.Compare(SafeName(left), SafeName(right), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static void ToggleMember(data_girls.girls girl)
        {
            if (!IsAvailableGirl(girl)) return;
            if (selectedMemberIds.Contains(girl.id))
            {
                selectedMemberIds.Remove(girl.id);
            }
            else
            {
                if (selectedMemberIds.Count >= MaximumCliqueMembers)
                {
                    NotifyWarning("notification.clique_cheat.max_members");
                    return;
                }
                Relationships._clique current = girl.GetClique();
                if (current != null && current != editingClique)
                {
                    NotifyWarning("notification.clique_cheat.already_member");
                    return;
                }
                selectedMemberIds.Add(girl.id);
            }
            confirmingDisband = false;
            RefreshMemberEditor();
        }

        private static void SetRemovalMode(bool hostile)
        {
            hostileRemoval = hostile;
            confirmingDisband = false;
            RefreshMemberEditor();
        }

        private static void RefreshMemberEditor()
        {
            foreach (KeyValuePair<int, GameObject> pair in highlights)
                if (pair.Value != null) pair.Value.SetActive(selectedMemberIds.Contains(pair.Key));

            List<string> names = new List<string>();
            foreach (data_girls.girls girl in BuildMemberChoices())
                if (selectedMemberIds.Contains(girl.id)) names.Add(SafeName(girl));
            names.Sort(StringComparer.Create(CheatUi.Culture, true));

            if (memberSummary != null)
            {
                string leader = creatingClique || editingClique == null ? Text("ui.clique_cheat.new_clique") : SafeName(editingClique.Leader);
                memberSummary.text = string.Format(CheatUi.Culture, Text("ui.clique_cheat.summary"),
                    leader, selectedMemberIds.Count, MaximumCliqueMembers);
            }
            if (instructions != null)
            {
                instructions.text = Text(creatingClique
                    ? "ui.clique_cheat.create_instructions"
                    : hostileRemoval ? "ui.clique_cheat.hostile_instructions" : "ui.clique_cheat.peaceful_instructions");
            }
            if (peacefulButton != null && hostileButton != null)
            {
                CheatUi.SetButtonInteractable(peacefulButton, hostileRemoval);
                CheatUi.SetButtonInteractable(hostileButton, !hostileRemoval);
            }
            if (disbandButton != null)
            {
                CheatUi.SetButtonText(disbandButton, Text(confirmingDisband
                    ? "ui.clique_cheat.confirm_disband" : "ui.clique_cheat.disband"));
            }
            if (applyButton != null)
            {
                bool valid = creatingClique ? selectedMemberIds.Count >= 1
                    : selectedMemberIds.Count >= 2 && selectedMemberIds.Count <= MaximumCliqueMembers;
                CheatUi.SetButtonInteractable(applyButton, valid);
            }
        }

        private static void ApplyMembership()
        {
            try
            {
                List<data_girls.girls> selected = ResolveSelectedMembers();
                if (creatingClique)
                {
                    if (selected.Count < 1 || selected.Count > MaximumCliqueMembers)
                    {
                        NotifyWarning("notification.clique_cheat.invalid_members");
                        return;
                    }
                    Relationships._clique clique = new Relationships._clique();
                    foreach (data_girls.girls girl in selected) clique.Members.Add(girl);
                    clique.Leader = null;
                    clique.UpdateLeader();
                    if (clique.Leader == null) clique.Leader = selected[0];
                    if (Relationships.Cliques == null) Relationships.Cliques = new List<Relationships._clique>();
                    Relationships.Cliques.Add(clique);
                    RefreshGirls(selected);
                    NotificationManager.AddNotification(
                        string.Format(CheatUi.Culture, Text("notification.clique_cheat.created"), selected.Count),
                        mainScript.green32, NotificationManager._notification._type.idol_relationship_change);
                    ShowCliqueList();
                    return;
                }

                if (editingClique == null || !Relationships.Cliques.Contains(editingClique))
                {
                    NotifyWarning("notification.clique_cheat.missing");
                    ShowCliqueList();
                    return;
                }
                if (selected.Count < 2 || selected.Count > MaximumCliqueMembers)
                {
                    NotifyWarning("notification.clique_cheat.invalid_existing_members");
                    return;
                }

                List<data_girls.girls> original = new List<data_girls.girls>(editingClique.Members);
                foreach (data_girls.girls girl in selected)
                {
                    if (editingClique.Members.Contains(girl)) continue;
                    if (girl.GetClique() != null)
                    {
                        NotifyWarning("notification.clique_cheat.already_member");
                        return;
                    }
                    editingClique.Members.Add(girl);
                }

                List<data_girls.girls> removed = new List<data_girls.girls>();
                foreach (data_girls.girls girl in original)
                {
                    if (girl != null && !selected.Contains(girl)) removed.Add(girl);
                }
                foreach (data_girls.girls girl in removed)
                {
                    if (Relationships.Cliques.Contains(editingClique)) editingClique.Quit(girl, hostileRemoval);
                }
                if (Relationships.Cliques.Contains(editingClique)) editingClique.UpdateLeader();

                RefreshGirls(selected);
                RefreshGirls(removed);
                NotificationManager.AddNotification(
                    string.Format(CheatUi.Culture, Text("notification.clique_cheat.updated"),
                        selected.Count, removed.Count, hostileRemoval ? Text("ui.clique_cheat.hostile") : Text("ui.clique_cheat.peaceful")),
                    mainScript.green32, NotificationManager._notification._type.idol_relationship_change);
                ShowCliqueList();
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Clique membership edit failed: " + exception);
                NotifyWarning("notification.selected_idol_cheat_failed");
            }
        }

        private static void Disband()
        {
            if (editingClique == null || !Relationships.Cliques.Contains(editingClique))
            {
                ShowCliqueList();
                return;
            }
            if (!confirmingDisband)
            {
                confirmingDisband = true;
                RefreshMemberEditor();
                return;
            }

            List<data_girls.girls> formerMembers = new List<data_girls.girls>(editingClique.Members);
            Relationships.Cliques.Remove(editingClique);
            RefreshGirls(formerMembers);
            NotificationManager.AddNotification(Text("notification.clique_cheat.disbanded"), mainScript.green32,
                NotificationManager._notification._type.idol_relationship_change);
            ShowCliqueList();
        }

        private static List<data_girls.girls> ResolveSelectedMembers()
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (IsAvailableGirl(girl) && selectedMemberIds.Contains(girl.id)) result.Add(girl);
            }
            result.Sort(delegate(data_girls.girls left, data_girls.girls right)
            {
                return string.Compare(SafeName(left), SafeName(right), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static List<Relationships._clique> GetValidCliques()
        {
            List<Relationships._clique> result = new List<Relationships._clique>();
            if (Relationships.Cliques == null) return result;
            foreach (Relationships._clique clique in Relationships.Cliques)
                if (clique != null && clique.Members != null && clique.Members.Count > 0) result.Add(clique);
            result.Sort(delegate(Relationships._clique left, Relationships._clique right)
            {
                return string.Compare(SafeName(left.Leader), SafeName(right.Leader), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static bool IsAvailableGirl(data_girls.girls girl)
        {
            return girl != null && data_girls.girl != null && data_girls.girl.Contains(girl)
                && girl.Type == data_girls.girls._type.NORMAL
                && girl.status != data_girls._status.graduated;
        }

        private static string SafeName(data_girls.girls girl)
        {
            if (girl == null) return Text("ui.clique_cheat.no_leader");
            try { return girl.GetName(true); }
            catch { return Text("ui.clique_cheat.no_leader"); }
        }

        private static void RefreshGirls(IList<data_girls.girls> girls)
        {
            if (girls != null)
            {
                foreach (data_girls.girls girl in girls)
                    if (girl != null && girl.Update != null) girl.Update();
            }
            data_girls dataGirls = GetDataComponent<data_girls>();
            if (dataGirls != null) dataGirls.UpdateList(true);
        }

        private static string Text(string key)
        {
            return ModLocalization.Get(key, key);
        }

        private static void NotifyWarning(string key)
        {
            NotificationManager.AddNotification(Text(key), mainScript.red32,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void ClearPanel()
        {
            if (panel == null) return;
            for (int index = panel.childCount - 1; index >= 0; index--)
            {
                Transform child = panel.GetChild(index);
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            memberSummary = null;
            instructions = null;
            peacefulButton = null;
            hostileButton = null;
            applyButton = null;
            disbandButton = null;
            highlights.Clear();
        }

        private static void Close()
        {
            editingClique = null;
            selectedMemberIds.Clear();
            PopupManager.Close_();
        }

        private static void DestroyExistingRoot()
        {
            if (popupRoot != null)
            {
                UnityEngine.Object.Destroy(popupRoot);
                popupRoot = null;
            }
        }

        private static PopupManager GetPopupManager()
        {
            GameObject data = GetMainData();
            return data == null ? null : data.GetComponent<PopupManager>();
        }

        private static T GetDataComponent<T>() where T : Component
        {
            GameObject data = GetMainData();
            return data == null ? null : data.GetComponent<T>();
        }

        private static GameObject GetMainData()
        {
            Camera camera = Camera.main;
            if (camera == null) return null;
            mainScript main = camera.GetComponent<mainScript>();
            return main == null ? null : main.Data;
        }

        private static GameObject GetGirlButtonPrefab(PopupManager manager)
        {
            PopupManager._popup entry = manager.GetByType(PopupManager._type.girl_styling);
            if (entry == null || entry.obj == null) return null;
            Styling_Popup popup = entry.obj.GetComponent<Styling_Popup>();
            return popup == null ? null : popup.prefab_girl_button;
        }
    }
}
