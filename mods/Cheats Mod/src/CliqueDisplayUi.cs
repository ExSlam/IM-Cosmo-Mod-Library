using System;
using System.Collections.Generic;
using CheatsMod.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static class CliqueDisplayUi
    {
        private const float CompactBreakpoint = 650f;
        private const float CardPadding = 14f;
        private const float HeadingHeight = 28f;
        private const float SectionHeadingHeight = 24f;
        private const float PortraitWidth = 145f;
        private const float PortraitHeight = 79f;
        private const float PortraitGap = 12f;
        private const float CompactIdolCardInset = 112f;
        private const float WideIdolCardInset = 86f;
        private const float CompactLeaderCardTop = 108f;
        private const float CompactMembersHeadingTop = 198f;
        private const float CompactMembersCardTop = 282f;
        private const float CompactNoMembersLabelTop = 244f;
        private const float CompactCardBottomPadding = 14f;
        private const float CompactNoMembersCardHeight = 300f;
        private const float WideCardTop = 112f;
        private const float WideCardHeight = 200f;
        private const int CompactMemberColumns = 2;
        private const int CompactMaximumVisibleMembers = 4;
        private const byte SelectionAlpha = 62;
        private static readonly Color32 CardColor = new Color32(248, 246, 250, 255);

        internal static float GetCardHeight(float width, Relationships._clique clique)
        {
            if (width > CompactBreakpoint) return WideCardHeight;

            int memberCount = GetNonLeaderMembers(clique).Count;
            if (memberCount == 0) return CompactNoMembersCardHeight;

            int visibleMemberCount = Mathf.Min(memberCount, CompactMaximumVisibleMembers);
            int memberRows = Mathf.CeilToInt((float)visibleMemberCount / CompactMemberColumns);
            float lastRowTop = CompactMembersCardTop
                + Mathf.Max(0, memberRows - 1) * (PortraitHeight + PortraitGap);
            float height = lastRowTop + PortraitHeight + CompactCardBottomPadding;

            if (memberCount > CompactMaximumVisibleMembers)
                height += SectionHeadingHeight + CompactCardBottomPadding;
            return height;
        }

        internal static GameObject CreateCard(Transform parent, string name, Relationships._clique clique,
            int displayIndex, float width, GameObject girlButtonPrefab, Action onClick, out GameObject selectionOverlay)
        {
            float height = GetCardHeight(width, clique);
            GameObject root = CheatUi.Object(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image surface = root.AddComponent<Image>();
            IMUiPrimitives.TryCopyVanillaPanelVisual(surface);
            surface.color = CardColor;
            surface.raycastTarget = true;

            Button button = root.AddComponent<Button>();
            button.targetGraphic = surface;
            button.onClick = new Button.ButtonClickedEvent();
            if (onClick != null) button.onClick.AddListener(delegate { onClick(); });

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            layout.flexibleWidth = 1f;

            TextMeshProUGUI title = CheatUi.Label(root.transform, "CliqueTitle",
                string.Format(CheatUi.Culture, CheatUi.Text("ui.clique_card.title"), displayIndex),
                CheatUi.BodyFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(title.rectTransform, CardPadding, 8f, width - CardPadding * 2f, HeadingHeight);

            if (width <= CompactBreakpoint)
                CreateCompactContents(root.transform, clique, width, girlButtonPrefab, onClick);
            else
                CreateWideContents(root.transform, clique, width, girlButtonPrefab, onClick);

            selectionOverlay = CheatUi.Object("SelectedClique", root.transform);
            CheatUi.Stretch(selectionOverlay.GetComponent<RectTransform>());
            Image highlight = selectionOverlay.AddComponent<Image>();
            Color32 selected = mainScript.green32;
            selected.a = SelectionAlpha;
            highlight.color = selected;
            highlight.raycastTarget = false;
            selectionOverlay.SetActive(false);
            return root;
        }

        private static void CreateCompactContents(Transform root, Relationships._clique clique, float width,
            GameObject prefab, Action onClick)
        {
            TextMeshProUGUI leaderHeading = CheatUi.Label(root, "LeaderHeading", CheatUi.Text("ui.clique_card.leader"),
                CheatUi.SmallFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(leaderHeading.rectTransform, CardPadding, 40f, width - CardPadding * 2f, SectionHeadingHeight);
            if (clique != null && clique.Leader != null)
                CreateMiniIdol(root, prefab, clique.Leader, CompactIdolCardInset, CompactLeaderCardTop, onClick);
            else
                CreateMissingLabel(root, "MissingLeader", CheatUi.Text("ui.clique_cheat.no_leader"),
                    CompactIdolCardInset, CompactLeaderCardTop, PortraitWidth, PortraitHeight);

            TextMeshProUGUI membersHeading = CheatUi.Label(root, "MembersHeading", CheatUi.Text("ui.clique_card.members"),
                CheatUi.SmallFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(membersHeading.rectTransform, CardPadding, CompactMembersHeadingTop,
                width - CardPadding * 2f, SectionHeadingHeight);

            List<data_girls.girls> members = GetNonLeaderMembers(clique);
            if (members.Count == 0)
            {
                CreateMissingLabel(root, "NoMembers", CheatUi.Text("ui.clique_card.none_members"),
                    CompactIdolCardInset, CompactNoMembersLabelTop, width - CompactIdolCardInset - CardPadding, 40f);
                return;
            }

            // Compact clique cards deliberately use two columns. The native idol card has
            // artwork/text that bleeds beyond its nominal RectTransform, so a three-column
            // layout places the first card too close to the scroll viewport edge.
            const int columns = CompactMemberColumns;
            for (int index = 0; index < members.Count && index < CompactMaximumVisibleMembers; index++)
            {
                int row = index / columns;
                int column = index % columns;
                float left = CompactIdolCardInset + column * (PortraitWidth + PortraitGap);
                float top = CompactMembersCardTop + row * (PortraitHeight + PortraitGap);
                CreateMiniIdol(root, prefab, members[index], left, top, onClick);
            }
            if (members.Count > CompactMaximumVisibleMembers)
            {
                TextMeshProUGUI more = CheatUi.Label(root, "MoreMembers",
                    string.Format(CheatUi.Culture, CheatUi.Text("ui.clique_card.more_members"), members.Count - CompactMaximumVisibleMembers),
                    CheatUi.SmallFontSize, TextAlignmentOptions.Center, mainScript.black32);
                CheatUi.Place(more.rectTransform, CompactIdolCardInset,
                    CompactMembersCardTop + 2f * (PortraitHeight + PortraitGap),
                    PortraitWidth * CompactMemberColumns + PortraitGap, SectionHeadingHeight);
            }
        }

        private static void CreateWideContents(Transform root, Relationships._clique clique, float width,
            GameObject prefab, Action onClick)
        {
            const float leaderWidth = 200f;
            const float sectionGap = 28f;
            float membersLeft = CardPadding + leaderWidth + sectionGap;
            float leaderCardLeft = WideIdolCardInset;
            float membersCardLeft = membersLeft + (WideIdolCardInset - CardPadding);
            float membersWidth = width - membersLeft - CardPadding;
            float membersCardWidth = width - membersCardLeft - CardPadding;

            TextMeshProUGUI leaderHeading = CheatUi.Label(root, "LeaderHeading", CheatUi.Text("ui.clique_card.leader"),
                CheatUi.SmallFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(leaderHeading.rectTransform, CardPadding, 40f, leaderWidth, SectionHeadingHeight);
            if (clique != null && clique.Leader != null)
                CreateMiniIdol(root, prefab, clique.Leader, leaderCardLeft, WideCardTop, onClick);
            else
                CreateMissingLabel(root, "MissingLeader", CheatUi.Text("ui.clique_cheat.no_leader"),
                    leaderCardLeft, WideCardTop, PortraitWidth, PortraitHeight);

            TextMeshProUGUI membersHeading = CheatUi.Label(root, "MembersHeading", CheatUi.Text("ui.clique_card.members"),
                CheatUi.SmallFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(membersHeading.rectTransform, membersLeft, 40f, membersWidth, SectionHeadingHeight);

            List<data_girls.girls> members = GetNonLeaderMembers(clique);
            if (members.Count == 0)
            {
                CreateMissingLabel(root, "NoMembers", CheatUi.Text("ui.clique_card.none_members"),
                    membersCardLeft, WideCardTop, membersCardWidth, PortraitHeight);
                return;
            }

            int maxVisible = Mathf.Max(1, Mathf.FloorToInt((membersCardWidth + PortraitGap) / (PortraitWidth + PortraitGap)));
            for (int index = 0; index < members.Count && index < maxVisible; index++)
            {
                CreateMiniIdol(root, prefab, members[index], membersCardLeft + index * (PortraitWidth + PortraitGap),
                    WideCardTop, onClick);
            }
            if (members.Count > maxVisible)
            {
                TextMeshProUGUI more = CheatUi.Label(root, "MoreMembers",
                    string.Format(CheatUi.Culture, CheatUi.Text("ui.clique_card.more_members"), members.Count - maxVisible),
                    CheatUi.SmallFontSize, TextAlignmentOptions.Center, mainScript.black32);
                float moreLeft = membersCardLeft + (maxVisible - 1) * (PortraitWidth + PortraitGap);
                CheatUi.Place(more.rectTransform, moreLeft, WideCardTop, PortraitWidth, PortraitHeight);
            }
        }

        private static List<data_girls.girls> GetNonLeaderMembers(Relationships._clique clique)
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            if (clique == null || clique.Members == null) return result;
            foreach (data_girls.girls girl in clique.Members)
                if (girl != null && girl != clique.Leader) result.Add(girl);
            result.Sort(delegate(data_girls.girls left, data_girls.girls right)
            {
                return string.Compare(SafeName(left), SafeName(right), StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static void CreateMiniIdol(Transform parent, GameObject prefab, data_girls.girls girl,
            float left, float top, Action onClick)
        {
            if (prefab == null || girl == null)
            {
                CreateMissingLabel(parent, "MissingIdol", SafeName(girl), left, top, PortraitWidth, PortraitHeight);
                return;
            }

            GameObject wrapper = CheatUi.Object("CliqueIdol_" + girl.id, parent);
            RectTransform wrapperRect = wrapper.GetComponent<RectTransform>();
            CheatUi.Place(wrapperRect, left, top, PortraitWidth, PortraitHeight);
            wrapperRect.anchorMin = wrapperRect.anchorMax = wrapperRect.pivot = new Vector2(0f, 1f);

            GameObject item = UnityEngine.Object.Instantiate(prefab, wrapper.transform, false);
            item.name = "IdolCard";
            IMUiKit.ApplyLayerRecursively(item, parent.gameObject.layer);
            GirlButtonSmall nativeCard = item.GetComponent<GirlButtonSmall>();
            if (nativeCard != null)
            {
                nativeCard.DontDisableIfTraining = nativeCard.DontDisableIfHiatus = true;
                nativeCard.SetGirl(girl, false);
            }
            if (onClick != null) IMUiKit.RebindAllButtons(item, delegate { onClick(); });
            IMUiKit.ActivateButtonDefaults(item);

            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchorMin = itemRect.anchorMax = itemRect.pivot = new Vector2(0f, 1f);
                itemRect.anchoredPosition = Vector2.zero;
                float scale = Mathf.Min(PortraitWidth / CheatUi.PickerCellWidth,
                    PortraitHeight / CheatUi.PickerCellHeight);
                itemRect.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private static void CreateMissingLabel(Transform parent, string name, string value,
            float left, float top, float width, float height)
        {
            TextMeshProUGUI label = CheatUi.Label(parent, name, value, CheatUi.SmallFontSize,
                TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(label.rectTransform, left, top, width, height);
        }

        private static string SafeName(data_girls.girls girl)
        {
            if (girl == null) return string.Empty;
            try { return girl.GetName(true); }
            catch { return string.Empty; }
        }
    }
}
