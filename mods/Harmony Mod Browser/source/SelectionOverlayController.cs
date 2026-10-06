using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarmonyModBrowser
{
    internal enum SelectionMode
    {
        Upload,
        UpdateTarget
    }

    public sealed class SelectionOverlayController : MonoBehaviour
    {
        private const float PanelInset = 16f;
        private const float TitleTop = 12f;
        private const float TitleHeight = 54f;
        private const float CloseWidth = 144f;
        private const float SearchTop = 76f;
        private const float SearchHeight = 42f;
        private const float StatusTop = 126f;
        private const float StatusHeight = 44f;
        private const float ListTop = 178f;
        private const float FooterHeight = 58f;
        private const float CardGap = 8f;

        private readonly List<Mods._mod> candidates = new List<Mods._mod>();
        private readonly List<Mods._mod> filtered = new List<Mods._mod>();
        private readonly List<GameObject> cards = new List<GameObject>();

        private Mods_Popup popup;
        private SelectionMode mode;
        private GameObject overlayRoot;
        private GameObject panelRoot;
        private TextMeshProUGUI textTemplate;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI statusLabel;
        private TextMeshProUGUI pageLabel;
        private SearchFieldParts search;
        private Button closeButton;
        private Button previousButton;
        private Button nextButton;
        private ScrollAreaParts scroll;
        private GameObject cardPrefab;

        private int currentPage;
        private int generation;
        private string activeQuery = string.Empty;
        private string pendingQuery = string.Empty;
        private Coroutine renderCoroutine;
        private Coroutine searchCoroutine;
        private Coroutine ownershipCoroutine;
        private bool languageSubscribed;

        internal static void Show(Mods_Popup owner, SelectionMode selectionMode)
        {
            if (owner == null)
            {
                return;
            }

            SelectionOverlayController controller = owner.GetComponent<SelectionOverlayController>();
            if (controller == null)
            {
                controller = owner.gameObject.AddComponent<SelectionOverlayController>();
            }
            controller.popup = owner;
            controller.Open(selectionMode);
            BrowserRegistry.SelectionOverlay = controller;
        }

        internal void RefreshOwnershipSensitiveUi()
        {
            if (overlayRoot == null || mode != SelectionMode.UpdateTarget)
            {
                return;
            }
            ReloadCandidates();
            ApplyFilter(resetPage: false);
            UpdateStatus();
            RenderCurrentPage();
        }

        private void Open(SelectionMode selectionMode)
        {
            CloseOverlay();
            mode = selectionMode;
            currentPage = 0;
            activeQuery = string.Empty;
            pendingQuery = string.Empty;
            generation++;

            textTemplate = RuntimeUi.FindTextTemplate(popup);
            ResolveCardPrefab();
            if (cardPrefab == null)
            {
                HmbLog.Warning("Could not find the game's upload/update selection card prefab.");
                return;
            }

            CreateOverlay();
            HmbLocalization.Reload();
            ApplyLocalizedUi();
            ReloadCandidates();
            ApplyFilter(resetPage: true);
            UpdateStatus();
            RenderCurrentPage();

            if (!languageSubscribed)
            {
                Language.onReset += OnLanguageReset;
                languageSubscribed = true;
            }
            if (mode == SelectionMode.UpdateTarget)
            {
                StartOwnershipPollingIfNeeded();
            }
        }

        private void ResolveCardPrefab()
        {
            cardPrefab = null;
            if (popup == null || popup.Upload_Update_Popup == null)
            {
                return;
            }
            Mods_Upload_Update uploadUpdate = popup.Upload_Update_Popup.GetComponent<Mods_Upload_Update>();
            if (uploadUpdate != null)
            {
                cardPrefab = uploadUpdate.prefab_mod_button;
            }
        }

        private void CreateOverlay()
        {
            overlayRoot = RuntimeUi.CreatePanel("HMB Selection Overlay", popup.transform, RuntimeUi.BackdropColor);
            RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
            RuntimeUi.SetStretch(overlayRect, 0f, 0f, 0f, 0f);
            overlayRoot.transform.SetAsLastSibling();

            panelRoot = RuntimeUi.CreatePanel("Panel", overlayRoot.transform, RuntimeUi.PanelColor);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.04f, 0.05f);
            panelRect.anchorMax = new Vector2(0.96f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            ScreenFittedPanel.Attach(panelRect);

            titleLabel = RuntimeUi.CreateText(
                "Title",
                panelRoot.transform,
                string.Empty,
                24f,
                TextAlignmentOptions.MidlineLeft,
                textTemplate,
                RuntimeUi.TextColor);
            RuntimeUi.TopRow(titleLabel.rectTransform, TitleTop, TitleHeight, PanelInset, CloseWidth + PanelInset * 2f);
            titleLabel.enableWordWrapping = true;
            titleLabel.fontStyle |= FontStyles.Bold;

            closeButton = RuntimeUi.CreateButton("Close", panelRoot.transform, HmbConstants.DefaultClose, textTemplate, CloseOverlay);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-PanelInset, -TitleTop);
            closeRect.sizeDelta = new Vector2(CloseWidth, TitleHeight);

            string placeholder = mode == SelectionMode.Upload
                ? HmbLocalization.Get(HmbConstants.UploadSearchPlaceholderKey, HmbConstants.DefaultUploadSearchPlaceholder)
                : HmbLocalization.Get(HmbConstants.UpdateSearchPlaceholderKey, HmbConstants.DefaultUpdateSearchPlaceholder);
            search = RuntimeUi.CreateSearchField("Search", panelRoot.transform, placeholder, textTemplate);
            RuntimeUi.TopRow(search.Input.GetComponent<RectTransform>(), SearchTop, SearchHeight, PanelInset, PanelInset);
            search.Input.onValueChanged.AddListener(OnSearchChanged);

            statusLabel = RuntimeUi.CreateText(
                "Status",
                panelRoot.transform,
                string.Empty,
                15f,
                TextAlignmentOptions.MidlineLeft,
                textTemplate,
                RuntimeUi.MutedTextColor);
            RuntimeUi.TopRow(statusLabel.rectTransform, StatusTop, StatusHeight, PanelInset, PanelInset);
            statusLabel.enableWordWrapping = true;

            scroll = RuntimeUi.CreateGridScrollArea(
                "Chooser Scroll",
                panelRoot.transform,
                new Vector2(ResponsiveModGrid.ChooserCardWidth, ResponsiveModGrid.ChooserCardHeight),
                new Vector2(CardGap, CardGap),
                ResponsiveModGrid.MaximumChooserColumns);
            RuntimeUi.SetStretch(scroll.ScrollRect.GetComponent<RectTransform>(), PanelInset, FooterHeight + CardGap, PanelInset, ListTop);

            previousButton = RuntimeUi.CreateButton("Previous", panelRoot.transform, HmbConstants.DefaultPrevious, textTemplate, PreviousPage);
            RuntimeUi.SetNormalizedRect(previousButton.GetComponent<RectTransform>(), 0.03f, 0f, 0.32f, 0f, 0f);

            pageLabel = RuntimeUi.CreateText(
                "Page",
                panelRoot.transform,
                string.Empty,
                16f,
                TextAlignmentOptions.Midline,
                textTemplate,
                RuntimeUi.TextColor);
            RuntimeUi.SetNormalizedRect(pageLabel.rectTransform, 0.34f, 0f, 0.66f, 0f, 0f);

            nextButton = RuntimeUi.CreateButton("Next", panelRoot.transform, HmbConstants.DefaultNext, textTemplate, NextPage);
            RuntimeUi.SetNormalizedRect(nextButton.GetComponent<RectTransform>(), 0.68f, 0f, 0.97f, 0f, 0f);
            foreach (RectTransform rect in new[] { previousButton.GetComponent<RectTransform>(),
                pageLabel.rectTransform, nextButton.GetComponent<RectTransform>() })
            {
                rect.offsetMin = new Vector2(0f, CardGap);
                rect.offsetMax = new Vector2(0f, FooterHeight - CardGap);
            }
        }

        private void ReloadCandidates()
        {
            candidates.Clear();
            if (Mods._Mods == null)
            {
                return;
            }

            for (int i = 0; i < Mods._Mods.Count; i++)
            {
                Mods._mod mod = Mods._Mods[i];
                if (mod == null)
                {
                    continue;
                }

                if (mode == SelectionMode.Upload)
                {
                    if (!mod.IsWorkshop())
                    {
                        candidates.Add(mod);
                    }
                }
                else if (SteamOwnershipMonitor.IsOwnedWorkshop(mod))
                {
                    candidates.Add(mod);
                }
            }
        }

        private void ApplyFilter(bool resetPage)
        {
            filtered.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (ModSearch.Matches(candidates[i], activeQuery))
                {
                    filtered.Add(candidates[i]);
                }
            }

            if (resetPage)
            {
                currentPage = 0;
            }
            int pageCount = GetPageCount();
            if (currentPage >= pageCount)
            {
                currentPage = Mathf.Max(0, pageCount - 1);
            }
        }

        private void UpdateStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            if (mode == SelectionMode.UpdateTarget && SteamOwnershipMonitor.HasPendingWorkshopOwnership())
            {
                statusLabel.text = HmbLocalization.Get(HmbConstants.LoadingOwnershipKey, HmbConstants.DefaultLoadingOwnership);
                return;
            }
            if (mode == SelectionMode.UpdateTarget && SteamOwnershipMonitor.QueryFailed)
            {
                statusLabel.text = HmbLocalization.Get(HmbConstants.OwnershipUnavailableKey, HmbConstants.DefaultOwnershipUnavailable);
                return;
            }

            if (filtered.Count == 0)
            {
                statusLabel.text = mode == SelectionMode.Upload
                    ? HmbLocalization.Get(HmbConstants.NoLocalModsKey, HmbConstants.DefaultNoLocalMods)
                    : HmbLocalization.Get(HmbConstants.NoOwnedWorkshopKey, HmbConstants.DefaultNoOwnedWorkshop);
                return;
            }

            statusLabel.text = mode == SelectionMode.Upload
                ? HmbLocalization.Format(HmbConstants.LocalCountFormatKey, HmbConstants.DefaultLocalCountFormat, filtered.Count)
                : HmbLocalization.Format(HmbConstants.OwnedCountFormatKey, HmbConstants.DefaultOwnedCountFormat, filtered.Count);
        }

        private void RenderCurrentPage()
        {
            generation++;
            StopRenderCoroutine();
            DestroyCards();
            if (scroll == null)
            {
                return;
            }

            int pageCount = GetPageCount();
            if (pageLabel != null)
            {
                pageLabel.text = HmbLocalization.Format(HmbConstants.PageFormatKey, HmbConstants.DefaultPageFormat, pageCount == 0 ? 0 : currentPage + 1, pageCount);
            }
            if (previousButton != null)
            {
                RuntimeUi.SetButtonInteractable(previousButton, currentPage > 0);
            }
            if (nextButton != null)
            {
                RuntimeUi.SetButtonInteractable(nextButton, currentPage + 1 < pageCount);
            }
            UpdateStatus();
            RuntimeUi.ResetScroll(scroll.ScrollRect);

            if (filtered.Count == 0)
            {
                return;
            }
            renderCoroutine = StartCoroutine(RenderCardsIncrementally(generation));
        }

        private IEnumerator RenderCardsIncrementally(int renderGeneration)
        {
            int start = currentPage * HmbConstants.ChooserPageSize;
            int end = Mathf.Min(start + HmbConstants.ChooserPageSize, filtered.Count);
            int renderedThisFrame = 0;
            for (int i = start; i < end; i++)
            {
                if (renderGeneration != generation || overlayRoot == null)
                {
                    renderCoroutine = null;
                    yield break;
                }
                CreateSelectionCard(filtered[i]);
                renderedThisFrame++;
                if (renderedThisFrame >= HmbConstants.ChooserCardsPerFrame)
                {
                    renderedThisFrame = 0;
                    yield return null;
                }
            }
            RuntimeUi.ResetScroll(scroll.ScrollRect);
            renderCoroutine = null;
        }

        private void CreateSelectionCard(Mods._mod mod)
        {
            GameObject root = null;
            try
            {
                root = UnityEngine.Object.Instantiate(cardPrefab);
                root.name = "HMB Select - " + (mod.Title ?? mod.ModName ?? "Mod");
                root.transform.SetParent(scroll.Content, false);
                Mods_Upload_Update_Button card = root.GetComponent<Mods_Upload_Update_Button>();
                if (card == null)
                {
                    throw new InvalidOperationException("Selection prefab has no Mods_Upload_Update_Button component.");
                }
                card.Set(mod, null);
                RuntimeUi.ArrangeSelectionCard(card);
                if (card.Button_Text != null)
                {
                    card.Button_Text.SetActive(true);
                    ExtensionMethods.SetText(card.Button_Text, HmbLocalization.Get(HmbConstants.SelectKey, HmbConstants.DefaultSelect));
                }
                if (card.Button_Spinner != null)
                {
                    card.Button_Spinner.SetActive(false);
                }
                if (card.Button_Upload != null)
                {
                    Button button = card.Button_Upload.GetComponent<Button>();
                    if (button == null)
                    {
                        button = card.Button_Upload.AddComponent<Button>();
                    }
                    RuntimeUi.SetButtonLabel(button, HmbLocalization.Get(HmbConstants.SelectKey, HmbConstants.DefaultSelect));
                    RuntimeUi.SetButtonInteractable(button, true);
                    button.onClick = new Button.ButtonClickedEvent();
                    Mods._mod selected = mod;
                    button.onClick.AddListener(delegate { Select(selected); });
                    ButtonDefault buttonDefault = card.Button_Upload.GetComponent<ButtonDefault>();
                    if (buttonDefault != null)
                    {
                        buttonDefault.Activate(true);
                    }
                }
                cards.Add(root);
                root.SetActive(true);
            }
            catch (Exception ex)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
                HmbLog.Warning("Failed to create chooser card for '" + (mod == null ? "<null>" : mod.Title) + "': " + ex.Message);
            }
        }

        private void Select(Mods._mod mod)
        {
            if (mod == null)
            {
                return;
            }

            SelectionMode selectedMode = mode;
            CloseOverlay();
            if (selectedMode == SelectionMode.Upload)
            {
                Mods_Popup.Upload(mod);
            }
            else if (SteamOwnershipMonitor.IsOwnedWorkshop(mod))
            {
                Mods_Popup.UploadUpdate(mod);
            }
            else
            {
                HmbLog.Warning("Refused to open the update flow for a Workshop item not verified as owned by the current Steam account.");
            }
        }

        private int GetPageCount()
        {
            if (filtered.Count == 0)
            {
                return 0;
            }
            return Mathf.CeilToInt(filtered.Count / (float)HmbConstants.ChooserPageSize);
        }

        private void PreviousPage()
        {
            if (currentPage <= 0)
            {
                return;
            }
            currentPage--;
            RenderCurrentPage();
        }

        private void NextPage()
        {
            int pageCount = GetPageCount();
            if (currentPage + 1 >= pageCount)
            {
                return;
            }
            currentPage++;
            RenderCurrentPage();
        }

        private void OnSearchChanged(string value)
        {
            pendingQuery = value ?? string.Empty;
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
            }
            searchCoroutine = StartCoroutine(ApplySearchAfterDelay());
        }

        private IEnumerator ApplySearchAfterDelay()
        {
            yield return new WaitForSecondsRealtime(HmbConstants.SearchDebounceSeconds);
            searchCoroutine = null;
            activeQuery = pendingQuery;
            ApplyFilter(resetPage: true);
            RenderCurrentPage();
        }

        private void StartOwnershipPollingIfNeeded()
        {
            if (ownershipCoroutine != null)
            {
                StopCoroutine(ownershipCoroutine);
            }
            if (SteamOwnershipMonitor.HasPendingWorkshopOwnership())
            {
                ownershipCoroutine = StartCoroutine(PollOwnership());
            }
        }

        private IEnumerator PollOwnership()
        {
            int lastRevision = SteamOwnershipMonitor.Revision;
            while (overlayRoot != null && mode == SelectionMode.UpdateTarget && SteamOwnershipMonitor.HasPendingWorkshopOwnership())
            {
                yield return new WaitForSecondsRealtime(HmbConstants.OwnershipPollSeconds);
                if (lastRevision != SteamOwnershipMonitor.Revision)
                {
                    lastRevision = SteamOwnershipMonitor.Revision;
                    RefreshOwnershipSensitiveUi();
                }
            }
            ownershipCoroutine = null;
            if (overlayRoot != null && mode == SelectionMode.UpdateTarget)
            {
                RefreshOwnershipSensitiveUi();
            }
        }

        private void ApplyLocalizedUi()
        {
            if (titleLabel != null)
            {
                titleLabel.text = mode == SelectionMode.Upload
                    ? HmbLocalization.Get(HmbConstants.UploadChooserTitleKey, HmbConstants.DefaultUploadChooserTitle)
                    : HmbLocalization.Get(HmbConstants.UpdateChooserTitleKey, HmbConstants.DefaultUpdateChooserTitle);
            }
            if (search != null && search.Placeholder != null)
            {
                search.Placeholder.text = mode == SelectionMode.Upload
                    ? HmbLocalization.Get(HmbConstants.UploadSearchPlaceholderKey, HmbConstants.DefaultUploadSearchPlaceholder)
                    : HmbLocalization.Get(HmbConstants.UpdateSearchPlaceholderKey, HmbConstants.DefaultUpdateSearchPlaceholder);
            }
            RuntimeUi.SetButtonLabel(closeButton, HmbLocalization.Get(HmbConstants.CloseKey, HmbConstants.DefaultClose));
            RuntimeUi.SetButtonLabel(previousButton, HmbLocalization.Get(HmbConstants.PreviousKey, HmbConstants.DefaultPrevious));
            RuntimeUi.SetButtonLabel(nextButton, HmbLocalization.Get(HmbConstants.NextKey, HmbConstants.DefaultNext));
            HmbGameFont.ApplyAll(panelRoot);
        }

        private void OnLanguageReset()
        {
            HmbLocalization.Reload();
            ApplyLocalizedUi();
            UpdateStatus();
            RenderCurrentPage();
        }

        private void DestroyCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    UnityEngine.Object.Destroy(cards[i]);
                }
            }
            cards.Clear();
            if (scroll != null)
            {
                RuntimeUi.DestroyChildren(scroll.Content);
            }
        }

        private void StopRenderCoroutine()
        {
            if (renderCoroutine != null)
            {
                StopCoroutine(renderCoroutine);
                renderCoroutine = null;
            }
        }

        private void CloseOverlay()
        {
            generation++;
            StopRenderCoroutine();
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
            if (ownershipCoroutine != null)
            {
                StopCoroutine(ownershipCoroutine);
                ownershipCoroutine = null;
            }
            DestroyCards();
            if (overlayRoot != null)
            {
                UnityEngine.Object.Destroy(overlayRoot);
                overlayRoot = null;
            }
            panelRoot = null;
            search = null;
            statusLabel = null;
            pageLabel = null;
            previousButton = null;
            nextButton = null;
            closeButton = null;
            scroll = null;
        }

        private void OnDisable()
        {
            CloseOverlay();
        }

        private void OnDestroy()
        {
            CloseOverlay();
            if (languageSubscribed)
            {
                Language.onReset -= OnLanguageReset;
                languageSubscribed = false;
            }
            if (BrowserRegistry.SelectionOverlay == this)
            {
                BrowserRegistry.SelectionOverlay = null;
            }
        }
    }
}
