using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace HarmonyModBrowser
{
    public sealed class ModBrowserController : MonoBehaviour
    {
        private const float HeaderHeight = 42f;
        private const float FooterHeight = 78f;
        private const float ControlGap = 4f;
        private const float MetadataHeight = 19f;
        private const float MetadataGap = 2f;
        private const float ThumbnailPixelsPerUnit = 100f;
        private const int FirstPage = 0;
        private const string SourceFilterObjectName = "Source Filter";
        private const string BackButtonPath = "Cancel";
        private const string ThumbnailButtonName = "HMB Thumbnail Details";

        private enum SourceFilterMode
        {
            All,
            Local,
            Workshop
        }

        private sealed class PageCard
        {
            internal Mods._mod Mod;
            internal GameObject Root;
            internal Mod_Button Button;
            internal Image ThumbnailImage;
            internal Sprite RuntimeSprite;
            internal Texture2D RuntimeTexture;
        }

        private static readonly Dictionary<int, ModBrowserController> ManagedButtons = new Dictionary<int, ModBrowserController>();

        private readonly List<Mods._mod> allMods = new List<Mods._mod>();
        private readonly List<Mods._mod> filteredMods = new List<Mods._mod>();
        private readonly List<PageCard> pageCards = new List<PageCard>();

        private Mods_Popup popup;
        private RectTransform content;
        private GridLayoutGroup grid;
        private ScrollRect scrollRect;
        private RectTransform viewport;
        private DockedModList dock;
        private ResponsiveModGrid responsiveGrid;
        private TextMeshProUGUI textTemplate;

        private GameObject headerRoot;
        private GameObject footerRoot;
        private SearchFieldParts search;
        private TextMeshProUGUI resultLabel;
        private TextMeshProUGUI pageLabel;
        private Button previousButton;
        private Button nextButton;
        private Button sourceFilterButton;

        private int originalPaddingTop;
        private int originalPaddingBottom;
        private Vector2 originalSpacing;
        private bool layoutCaptured;
        private bool initialized;
        private bool rendering;
        private bool languageSubscribed;
        private int currentPage;
        private int generation;
        private SourceFilterMode sourceFilter = SourceFilterMode.All;
        private string activeQuery = string.Empty;
        private string pendingQuery = string.Empty;

        private Coroutine pageCoroutine;
        private Coroutine thumbnailCoroutine;
        private Coroutine searchCoroutine;
        private UnityWebRequest activeThumbnailRequest;

        internal static ModBrowserController GetOrCreate(Mods_Popup owner)
        {
            if (owner == null)
            {
                return null;
            }
            ModBrowserController controller = owner.GetComponent<ModBrowserController>();
            if (controller == null)
            {
                controller = owner.gameObject.AddComponent<ModBrowserController>();
            }
            controller.popup = owner;
            BrowserRegistry.MainBrowser = controller;
            return controller;
        }

        internal static bool IsManaged(Mod_Button button)
        {
            if (button == null)
            {
                return false;
            }
            ModBrowserController controller;
            return ManagedButtons.TryGetValue(button.GetInstanceID(), out controller) && controller != null;
        }

        internal bool BeginRender()
        {
            if (popup == null || popup.Container == null || popup.prefab_mod_button == null)
            {
                return false;
            }
            if (!InitializeIfNeeded())
            {
                return false;
            }

            rendering = true;
            ReloadAllMods();
            HmbGlyphWarmup.QueueLoadedMods();
            ApplyFilter();
            currentPage = FirstPage;
            RenderCurrentPage();
            return true;
        }

        internal void RefreshOwnershipSensitiveUi()
        {
            if (!initialized || !rendering || !isActiveAndEnabled)
            {
                return;
            }
            RenderCurrentPage();
        }

        private bool InitializeIfNeeded()
        {
            if (initialized)
            {
                return true;
            }

            content = popup.Container.transform as RectTransform;
            if (content == null)
            {
                return false;
            }
            grid = popup.Container.GetComponent<GridLayoutGroup>();
            scrollRect = RuntimeUi.FindScrollRect(content);
            if (grid == null || scrollRect == null)
            {
                HmbLog.Warning("Could not find the vanilla Mods grid/scroll components; falling back to the game's renderer.");
                return false;
            }
            viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;
            if (viewport == null)
            {
                return false;
            }

            textTemplate = RuntimeUi.FindTextTemplate(popup);
            originalPaddingTop = grid.padding.top;
            originalPaddingBottom = grid.padding.bottom;
            originalSpacing = grid.spacing;
            layoutCaptured = true;
            RuntimeUi.InitializeTemplates(popup, scrollRect);
            dock = new DockedModList(scrollRect, HeaderHeight, FooterHeight, ControlGap);
            responsiveGrid = ResponsiveModGrid.Attach(grid, viewport, grid.cellSize,
                ResponsiveModGrid.MaximumBrowserColumns);
            Vector2 compactSpacing = grid.spacing;
            compactSpacing.y = Mathf.Min(compactSpacing.y, 4f);
            grid.spacing = compactSpacing;

            CreateControls();
            HmbLocalization.Reload();
            ApplyLocalizedUi();
            Language.onReset += OnLanguageReset;
            languageSubscribed = true;
            initialized = true;
            BrowserRegistry.MainBrowser = this;
            return true;
        }

        private void CreateControls()
        {
            headerRoot = RuntimeUi.CreatePanel("HMB Browser Header", dock.Root, RuntimeUi.PanelColor);
            RectTransform headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(8f, -HeaderHeight);
            headerRect.offsetMax = new Vector2(-8f, 0f);
            headerRoot.transform.SetAsLastSibling();

            search = RuntimeUi.CreateSearchField(
                "Search",
                headerRoot.transform,
                HmbLocalization.Get(HmbConstants.SearchPlaceholderKey, HmbConstants.DefaultSearchPlaceholder),
                textTemplate);
            RuntimeUi.SetNormalizedRect(search.Input.GetComponent<RectTransform>(), 0.01f, 0.04f, 0.99f, 0.96f, 2f);
            search.Input.onValueChanged.AddListener(OnSearchChanged);

            sourceFilterButton = RuntimeUi.CreateButton(
                SourceFilterObjectName, popup.transform, HmbConstants.DefaultAllMods,
                textTemplate, CycleSourceFilter);
            OppositeBackButtonLayout.Attach(sourceFilterButton.GetComponent<RectTransform>(),
                popup.transform.Find(BackButtonPath) as RectTransform, dock.Root.parent as RectTransform);

            footerRoot = RuntimeUi.CreatePanel("HMB Browser Footer", dock.Root, RuntimeUi.PanelColor);
            RectTransform footerRect = footerRoot.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.offsetMin = new Vector2(8f, 0f);
            footerRect.offsetMax = new Vector2(-8f, FooterHeight);
            footerRoot.transform.SetAsLastSibling();

            resultLabel = RuntimeUi.CreateText(
                "Result Count",
                footerRoot.transform,
                string.Empty,
                16f,
                TextAlignmentOptions.MidlineLeft,
                textTemplate,
                RuntimeUi.MutedTextColor);
            RuntimeUi.SetNormalizedRect(resultLabel.rectTransform, 0.02f, 0.64f, 0.98f, 0.98f, 1f);

            previousButton = RuntimeUi.CreateButton(
                "Previous",
                footerRoot.transform,
                HmbConstants.DefaultPrevious,
                textTemplate,
                PreviousPage);
            RuntimeUi.SetNormalizedRect(previousButton.GetComponent<RectTransform>(), 0.01f, 0.06f, 0.32f, 0.57f, 1f);

            pageLabel = RuntimeUi.CreateText(
                "Page Label",
                footerRoot.transform,
                string.Empty,
                17f,
                TextAlignmentOptions.Midline,
                textTemplate,
                RuntimeUi.TextColor);
            RuntimeUi.SetNormalizedRect(pageLabel.rectTransform, 0.33f, 0.06f, 0.67f, 0.57f, 1f);

            nextButton = RuntimeUi.CreateButton(
                "Next",
                footerRoot.transform,
                HmbConstants.DefaultNext,
                textTemplate,
                NextPage);
            RuntimeUi.SetNormalizedRect(nextButton.GetComponent<RectTransform>(), 0.68f, 0.06f, 0.99f, 0.57f, 1f);
        }

        private void OnLanguageReset()
        {
            HmbLocalization.Reload();
            ApplyLocalizedUi();
            RenderCurrentPage();
        }

        private void ApplyLocalizedUi()
        {
            if (search != null && search.Placeholder != null)
            {
                search.Placeholder.text = HmbLocalization.Get(HmbConstants.SearchPlaceholderKey, HmbConstants.DefaultSearchPlaceholder);
            }
            RuntimeUi.SetButtonLabel(previousButton, HmbLocalization.Get(HmbConstants.PreviousKey, HmbConstants.DefaultPrevious));
            RuntimeUi.SetButtonLabel(nextButton, HmbLocalization.Get(HmbConstants.NextKey, HmbConstants.DefaultNext));
            UpdateSourceFilterLabel();
            UpdateNavigationState();
            HmbGameFont.ApplyAll(headerRoot);
            HmbGameFont.ApplyAll(footerRoot);
        }

        private void ReloadAllMods()
        {
            allMods.Clear();
            if (Mods._Mods == null)
            {
                return;
            }
            for (int i = 0; i < Mods._Mods.Count; i++)
            {
                Mods._mod mod = Mods._Mods[i];
                if (mod != null)
                {
                    allMods.Add(mod);
                }
            }
        }

        private void ApplyFilter()
        {
            filteredMods.Clear();
            for (int i = 0; i < allMods.Count; i++)
            {
                Mods._mod mod = allMods[i];
                if (!MatchesSource(mod))
                {
                    continue;
                }
                if (ModSearch.Matches(mod, activeQuery))
                {
                    filteredMods.Add(mod);
                }
            }
            UpdateNavigationState();
        }

        private bool MatchesSource(Mods._mod mod)
        {
            if (mod == null)
            {
                return false;
            }
            switch (sourceFilter)
            {
                case SourceFilterMode.Local:
                    return !mod.IsWorkshop();
                case SourceFilterMode.Workshop:
                    return mod.IsWorkshop();
                default:
                    return true;
            }
        }

        private void CycleSourceFilter()
        {
            if (sourceFilter == SourceFilterMode.All)
            {
                sourceFilter = SourceFilterMode.Local;
            }
            else if (sourceFilter == SourceFilterMode.Local)
            {
                sourceFilter = SourceFilterMode.Workshop;
            }
            else
            {
                sourceFilter = SourceFilterMode.All;
            }
            currentPage = FirstPage;
            UpdateSourceFilterLabel();
            ApplyFilter();
            RenderCurrentPage();
        }

        private void UpdateSourceFilterLabel()
        {
            string text;
            switch (sourceFilter)
            {
                case SourceFilterMode.Local:
                    text = HmbLocalization.Get(HmbConstants.LocalKey, HmbConstants.DefaultLocal);
                    break;
                case SourceFilterMode.Workshop:
                    text = HmbLocalization.Get(HmbConstants.WorkshopKey, HmbConstants.DefaultWorkshop);
                    break;
                default:
                    text = HmbLocalization.Get(HmbConstants.AllModsKey, HmbConstants.DefaultAllMods);
                    break;
            }
            RuntimeUi.SetButtonLabel(sourceFilterButton, text);
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
            string next = (pendingQuery ?? string.Empty).Trim();
            if (string.Equals(next, activeQuery, StringComparison.Ordinal))
            {
                yield break;
            }
            activeQuery = next;
            currentPage = FirstPage;
            ApplyFilter();
            RenderCurrentPage();
        }

        private int TotalPages
        {
            get
            {
                if (filteredMods.Count == 0)
                {
                    return 0;
                }
                return (filteredMods.Count + HmbConstants.MainPageSize - 1) / HmbConstants.MainPageSize;
            }
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
            if (currentPage >= TotalPages - 1)
            {
                return;
            }
            currentPage++;
            RenderCurrentPage();
        }

        private void UpdateNavigationState()
        {
            int pages = TotalPages;
            if (pages <= 0)
            {
                currentPage = 0;
            }
            else
            {
                currentPage = Mathf.Clamp(currentPage, 0, pages - 1);
            }
            if (previousButton != null)
            {
                RuntimeUi.SetButtonInteractable(previousButton, pages > 0 && currentPage > 0);
            }
            if (nextButton != null)
            {
                RuntimeUi.SetButtonInteractable(nextButton, pages > 0 && currentPage < pages - 1);
            }
            if (pageLabel != null)
            {
                pageLabel.text = pages == 0
                    ? HmbLocalization.Get(HmbConstants.NoResultsKey, HmbConstants.DefaultNoResults)
                    : HmbLocalization.Format(HmbConstants.PageFormatKey, HmbConstants.DefaultPageFormat, currentPage + 1, pages);
            }
            if (resultLabel != null)
            {
                resultLabel.text = string.IsNullOrEmpty(activeQuery)
                    ? HmbLocalization.Format(HmbConstants.CountFormatKey, HmbConstants.DefaultCountFormat, filteredMods.Count, HmbConstants.MainPageSize)
                    : HmbLocalization.Format(HmbConstants.MatchCountFormatKey, HmbConstants.DefaultMatchCountFormat, filteredMods.Count, HmbConstants.MainPageSize);
            }
        }

        private void RenderCurrentPage()
        {
            if (!initialized || !rendering)
            {
                return;
            }
            generation++;
            int renderGeneration = generation;
            StopPageWork();
            DestroyCurrentCards();
            UpdateNavigationState();
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 1f;
            }
            pageCoroutine = StartCoroutine(RenderPage(renderGeneration));
        }

        private IEnumerator RenderPage(int renderGeneration)
        {
            yield return null;
            if (!rendering || renderGeneration != generation)
            {
                pageCoroutine = null;
                yield break;
            }

            int start = currentPage * HmbConstants.MainPageSize;
            int end = Mathf.Min(filteredMods.Count, start + HmbConstants.MainPageSize);
            int created = 0;
            for (int i = start; i < end; i++)
            {
                if (!rendering || renderGeneration != generation)
                {
                    pageCoroutine = null;
                    yield break;
                }
                CreateCard(filteredMods[i]);
                created++;
                if (created >= HmbConstants.MainCardsPerFrame)
                {
                    created = 0;
                    yield return null;
                }
            }
            responsiveGrid.Refresh();
            RuntimeUi.ResetScroll(scrollRect);
            pageCoroutine = null;
            thumbnailCoroutine = StartCoroutine(LoadThumbnails(renderGeneration));
        }

        private void CreateCard(Mods._mod mod)
        {
            GameObject root = UnityEngine.Object.Instantiate(popup.prefab_mod_button);
            Mod_Button button = root == null ? null : root.GetComponent<Mod_Button>();
            if (root == null || button == null)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
                return;
            }

            Image thumbnail = button.Screenshot == null ? null : button.Screenshot.GetComponent<Image>();
            PageCard card = new PageCard
            {
                Mod = mod,
                Root = root,
                Button = button,
                ThumbnailImage = thumbnail
            };

            ManagedButtons[button.GetInstanceID()] = this;
            try
            {
                root.SetActive(false);
                button.Set(mod);
                DisableRedundantTooltips(button);
                CreateCardMetadata(card);
                RuntimeUi.FitText(button.Title, true);
                RuntimeUi.FitText(button.Description, true);
                root.transform.SetParent(content, false);
                AttachThumbnailButton(card);
                if (thumbnail != null)
                {
                    thumbnail.sprite = null;
                    thumbnail.enabled = false;
                }
                pageCards.Add(card);
                root.SetActive(true);
            }
            catch (Exception ex)
            {
                ManagedButtons.Remove(button.GetInstanceID());
                UnityEngine.Object.Destroy(root);
                HmbLog.Warning("Failed to create mod card for '" + (mod == null ? "<null>" : mod.Title) + "': " + ex.Message);
            }
        }

        private static void DisableRedundantTooltips(Mod_Button button)
        {
            if (button == null)
            {
                return;
            }
            ButtonDefault rootDefault = button.GetComponent<ButtonDefault>();
            if (rootDefault != null)
            {
                rootDefault.SetTooltip(null);
                rootDefault.DefaultTooltip = string.Empty;
                rootDefault.forceTooltip = false;
                rootDefault.active = false;
            }
            if (button.Title_BG != null)
            {
                ButtonDefault titleDefault = button.Title_BG.GetComponent<ButtonDefault>();
                if (titleDefault != null)
                {
                    titleDefault.SetTooltip(null);
                    titleDefault.DefaultTooltip = string.Empty;
                    titleDefault.forceTooltip = false;
                    titleDefault.active = false;
                }
            }
        }

        private void CreateCardMetadata(PageCard card)
        {
            if (card == null || card.Mod == null || card.Button == null || card.Button.Description == null)
            {
                return;
            }
            RectTransform description = card.Button.Description.transform as RectTransform;
            if (description == null || description.parent == null)
            {
                return;
            }

            GameObject row = new GameObject("HMB Metadata", typeof(RectTransform));
            row.transform.SetParent(description.parent, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(description.anchorMin.x, description.anchorMax.y);
            rowRect.anchorMax = new Vector2(description.anchorMax.x, description.anchorMax.y);
            rowRect.pivot = new Vector2(description.pivot.x, 1f);
            rowRect.offsetMin = new Vector2(description.offsetMin.x, description.offsetMax.y - MetadataHeight);
            rowRect.offsetMax = new Vector2(description.offsetMax.x, description.offsetMax.y);
            Vector2 top = description.offsetMax;
            top.y -= MetadataHeight + MetadataGap;
            description.offsetMax = top;

            float cursor = 0f;
            if (!string.IsNullOrWhiteSpace(card.Mod.Version))
            {
                TextMeshProUGUI version = RuntimeUi.CreateText(
                    "Version",
                    row.transform,
                    HmbLocalization.Format(HmbConstants.VersionFormatKey, HmbConstants.DefaultVersionFormat, card.Mod.Version),
                    12.5f,
                    TextAlignmentOptions.MidlineLeft,
                    textTemplate,
                    RuntimeUi.MutedTextColor);
                RuntimeUi.SetNormalizedRect(version.rectTransform, 0f, 0f, 0.58f, 1f);
                cursor = 0.60f;
            }

            string sourceText = card.Mod.IsWorkshop()
                ? HmbLocalization.Get(HmbConstants.WorkshopKey, HmbConstants.DefaultWorkshop)
                : HmbLocalization.Get(HmbConstants.LocalKey, HmbConstants.DefaultLocal);
            GameObject badge = RuntimeUi.CreatePanel(
                "Source",
                row.transform,
                card.Mod.IsWorkshop() ? RuntimeUi.WorkshopBadgeColor : RuntimeUi.LocalBadgeColor);
            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            RuntimeUi.SetNormalizedRect(badgeRect, cursor, 0.08f, cursor == 0f ? 0.40f : 1f, 0.92f);
            Image badgeImage = badge.GetComponent<Image>();
            badgeImage.raycastTarget = false;
            TextMeshProUGUI badgeText = RuntimeUi.CreateText(
                "Text",
                badge.transform,
                sourceText,
                11.5f,
                TextAlignmentOptions.Midline,
                textTemplate,
                RuntimeUi.TextColor);
            badgeText.fontStyle |= FontStyles.Bold;
            badgeText.enableAutoSizing = true;
            badgeText.fontSizeMin = 9f;
            badgeText.fontSizeMax = 11.5f;
            RuntimeUi.SetStretch(badgeText.rectTransform, 4f, 0f, 4f, 0f);
        }

        private IEnumerator LoadThumbnails(int renderGeneration)
        {
            for (int i = 0; i < pageCards.Count; i++)
            {
                if (!rendering || renderGeneration != generation)
                {
                    thumbnailCoroutine = null;
                    yield break;
                }
                PageCard card = pageCards[i];
                if (card == null || card.Mod == null || card.ThumbnailImage == null || card.Root == null)
                {
                    continue;
                }
                string path = card.Mod.GetThumbPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    continue;
                }

                Sprite sprite = null;
                Texture2D texture = null;
                UnityWebRequest request = null;
                try
                {
                    request = UnityWebRequestTexture.GetTexture(ToFileUri(path), true);
                    activeThumbnailRequest = request;
                    yield return request.SendWebRequest();
                    if (!rendering || renderGeneration != generation)
                    {
                        thumbnailCoroutine = null;
                        yield break;
                    }
                    if (!request.isNetworkError && !request.isHttpError)
                    {
                        texture = DownloadHandlerTexture.GetContent(request);
                        if (texture != null)
                        {
                            sprite = Sprite.Create(
                                texture,
                                new Rect(0f, 0f, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f),
                                ThumbnailPixelsPerUnit);
                        }
                    }
                }
                finally
                {
                    if (request != null)
                    {
                        request.Dispose();
                    }
                    if (activeThumbnailRequest == request)
                    {
                        activeThumbnailRequest = null;
                    }
                }

                if (sprite == null && rendering && renderGeneration == generation)
                {
                    sprite = IMG2Sprite.instance.LoadNewSprite(path, ThumbnailPixelsPerUnit);
                    texture = sprite == null ? null : sprite.texture;
                }

                if (sprite != null && rendering && renderGeneration == generation && card.Root != null)
                {
                    card.RuntimeSprite = sprite;
                    card.RuntimeTexture = texture;
                    card.ThumbnailImage.sprite = sprite;
                    card.ThumbnailImage.enabled = true;
                }
                else
                {
                    ReleaseThumbnail(sprite, texture);
                }
                yield return null;
            }
            thumbnailCoroutine = null;
        }

        private static string ToFileUri(string path)
        {
            try
            {
                return new Uri(Path.GetFullPath(path)).AbsoluteUri;
            }
            catch
            {
                return "file:///" + (path ?? string.Empty).Replace('\\', '/');
            }
        }

        private void AttachThumbnailButton(PageCard card)
        {
            if (card.Button.Screenshot == null) return;
            // Only the portrait area gets a click target. The card's native actions
            // remain separate siblings, so enabling/uploading cannot open details.
            GameObject hitArea = RuntimeUi.CreatePanel(ThumbnailButtonName,
                card.Button.Screenshot.transform.parent, Color.clear);
            RuntimeUi.SetStretch(hitArea.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            Button trigger = hitArea.AddComponent<Button>();
            trigger.targetGraphic = hitArea.GetComponent<Image>();
            trigger.transition = Selectable.Transition.None;
            trigger.onClick.AddListener(() => ModDetailsController.Show(popup, card.Mod));
        }

        private void DestroyCurrentCards()
        {
            for (int i = 0; i < pageCards.Count; i++)
            {
                PageCard card = pageCards[i];
                if (card == null)
                {
                    continue;
                }
                if (card.Button != null)
                {
                    ManagedButtons.Remove(card.Button.GetInstanceID());
                }
                if (card.ThumbnailImage != null && card.ThumbnailImage.sprite == card.RuntimeSprite)
                {
                    card.ThumbnailImage.sprite = null;
                    card.ThumbnailImage.enabled = false;
                }
                ReleaseThumbnail(card.RuntimeSprite, card.RuntimeTexture);
                if (card.Root != null)
                {
                    UnityEngine.Object.Destroy(card.Root);
                }
            }
            pageCards.Clear();
            RuntimeUi.DestroyChildren(content);
        }

        private static void ReleaseThumbnail(Sprite sprite, Texture2D texture)
        {
            if (sprite != null)
            {
                UnityEngine.Object.Destroy(sprite);
            }
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        private void StopPageWork()
        {
            if (pageCoroutine != null)
            {
                StopCoroutine(pageCoroutine);
                pageCoroutine = null;
            }
            if (thumbnailCoroutine != null)
            {
                StopCoroutine(thumbnailCoroutine);
                thumbnailCoroutine = null;
            }
            if (activeThumbnailRequest != null)
            {
                try
                {
                    activeThumbnailRequest.Abort();
                }
                catch
                {
                }
                activeThumbnailRequest.Dispose();
                activeThumbnailRequest = null;
            }
        }

        private void OnDisable()
        {
            rendering = false;
            generation++;
            StopPageWork();
            for (int i = 0; i < pageCards.Count; i++)
            {
                PageCard card = pageCards[i];
                if (card == null)
                {
                    continue;
                }
                if (card.ThumbnailImage != null && card.ThumbnailImage.sprite == card.RuntimeSprite)
                {
                    card.ThumbnailImage.sprite = null;
                    card.ThumbnailImage.enabled = false;
                }
                ReleaseThumbnail(card.RuntimeSprite, card.RuntimeTexture);
                card.RuntimeSprite = null;
                card.RuntimeTexture = null;
            }
        }

        private void OnEnable()
        {
            if (initialized)
            {
                rendering = true;
                if (pageCards.Count > 0 && thumbnailCoroutine == null)
                {
                    int resumeGeneration = ++generation;
                    thumbnailCoroutine = StartCoroutine(LoadThumbnails(resumeGeneration));
                }
            }
        }

        private void OnDestroy()
        {
            rendering = false;
            generation++;
            StopPageWork();
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
            if (languageSubscribed)
            {
                Language.onReset -= OnLanguageReset;
                languageSubscribed = false;
            }
            DestroyCurrentCards();
            if (headerRoot != null)
            {
                UnityEngine.Object.Destroy(headerRoot);
            }
            if (footerRoot != null)
            {
                UnityEngine.Object.Destroy(footerRoot);
            }
            if (sourceFilterButton != null) UnityEngine.Object.Destroy(sourceFilterButton.gameObject);
            if (layoutCaptured && grid != null)
            {
                grid.padding.top = originalPaddingTop;
                grid.padding.bottom = originalPaddingBottom;
                grid.spacing = originalSpacing;
            }
            if (responsiveGrid != null) responsiveGrid.Restore();
            if (dock != null) dock.Restore();
            if (BrowserRegistry.MainBrowser == this)
            {
                BrowserRegistry.MainBrowser = null;
            }
        }
    }
}
