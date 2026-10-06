using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarmonyModBrowser
{
    public sealed class UpdateSourceSearchController : MonoBehaviour
    {
        private const float HeaderHeight = 98f;
        private const float HeaderGap = 4f;

        private readonly List<Mods._mod> localMods = new List<Mods._mod>();
        private readonly List<Mods._mod> filtered = new List<Mods._mod>();
        private readonly List<GameObject> cards = new List<GameObject>();

        private Mods_Upload_Update owner;
        private RectTransform content;
        private GridLayoutGroup grid;
        private ScrollRect scrollRect;
        private RectTransform viewport;
        private DockedModList dock;
        private ResponsiveModGrid responsiveGrid;
        private TextMeshProUGUI textTemplate;
        private GameObject headerRoot;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI countLabel;
        private SearchFieldParts search;
        private int originalPaddingTop;
        private bool layoutCaptured;
        private bool initialized;
        private bool languageSubscribed;
        private string activeQuery = string.Empty;
        private string pendingQuery = string.Empty;
        private int generation;
        private Coroutine searchCoroutine;
        private Coroutine renderCoroutine;

        internal static UpdateSourceSearchController Attach(Mods_Upload_Update uploadUpdate)
        {
            if (uploadUpdate == null)
            {
                return null;
            }
            UpdateSourceSearchController controller = uploadUpdate.GetComponent<UpdateSourceSearchController>();
            if (controller == null)
            {
                controller = uploadUpdate.gameObject.AddComponent<UpdateSourceSearchController>();
            }
            controller.owner = uploadUpdate;
            controller.Begin();
            BrowserRegistry.UpdateSourceSearch = controller;
            return controller;
        }

        private void Begin()
        {
            if (!InitializeIfNeeded())
            {
                return;
            }
            generation++;
            activeQuery = string.Empty;
            pendingQuery = string.Empty;
            if (search != null && search.Input != null)
            {
                search.Input.SetTextWithoutNotify(string.Empty);
            }
            ReloadLocalMods();
            ApplyFilter();
            HmbLocalization.Reload();
            ApplyLocalizedUi();
            RenderList();
        }

        private bool InitializeIfNeeded()
        {
            if (initialized)
            {
                return true;
            }
            if (owner == null || owner.Container == null || owner.prefab_mod_button == null)
            {
                return false;
            }

            content = owner.Container.transform as RectTransform;
            if (content == null)
            {
                return false;
            }
            grid = owner.Container.GetComponent<GridLayoutGroup>();
            scrollRect = RuntimeUi.FindScrollRect(content);
            if (grid == null || scrollRect == null)
            {
                HmbLog.Warning("Could not find the local-version update list grid/scroll components; leaving the vanilla list intact.");
                return false;
            }
            viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;
            if (viewport == null)
            {
                return false;
            }
            textTemplate = RuntimeUi.FindTextTemplate(owner);

            originalPaddingTop = grid.padding.top;
            layoutCaptured = true;
            dock = new DockedModList(scrollRect, HeaderHeight, 0f, HeaderGap);
            responsiveGrid = ResponsiveModGrid.Attach(grid, viewport,
                new Vector2(ResponsiveModGrid.ChooserCardWidth, ResponsiveModGrid.ChooserCardHeight),
                ResponsiveModGrid.MaximumChooserColumns);
            CreateHeader();
            Language.onReset += OnLanguageReset;
            languageSubscribed = true;
            initialized = true;
            return true;
        }

        private void CreateHeader()
        {
            headerRoot = RuntimeUi.CreatePanel("HMB Update Source Header", dock.Root, RuntimeUi.PanelColor);
            RectTransform rect = headerRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(6f, -HeaderHeight);
            rect.offsetMax = new Vector2(-6f, 0f);
            headerRoot.transform.SetAsLastSibling();

            titleLabel = RuntimeUi.CreateText(
                "Title",
                headerRoot.transform,
                HmbConstants.DefaultUpdateSourceTitle,
                15f,
                TextAlignmentOptions.MidlineLeft,
                textTemplate,
                RuntimeUi.MutedTextColor);
            RuntimeUi.SetNormalizedRect(titleLabel.rectTransform, 0.01f, 0.54f, 0.66f, 0.98f, 1f);

            countLabel = RuntimeUi.CreateText(
                "Count",
                headerRoot.transform,
                string.Empty,
                14f,
                TextAlignmentOptions.MidlineRight,
                textTemplate,
                RuntimeUi.MutedTextColor);
            RuntimeUi.SetNormalizedRect(countLabel.rectTransform, 0.67f, 0.54f, 0.99f, 0.98f, 1f);

            titleLabel.enableWordWrapping = true;
            countLabel.enableWordWrapping = true;
            search = RuntimeUi.CreateSearchField(
                "Search",
                headerRoot.transform,
                HmbConstants.DefaultUpdateSourceSearchPlaceholder,
                textTemplate);
            RuntimeUi.SetNormalizedRect(search.Input.GetComponent<RectTransform>(), 0.01f, 0.04f, 0.99f, 0.51f, 1f);
            search.Input.onValueChanged.AddListener(OnSearchChanged);
        }

        private void ReloadLocalMods()
        {
            localMods.Clear();
            if (Mods._Mods == null)
            {
                return;
            }
            for (int i = 0; i < Mods._Mods.Count; i++)
            {
                Mods._mod mod = Mods._Mods[i];
                if (mod != null && !mod.IsWorkshop())
                {
                    localMods.Add(mod);
                }
            }
        }

        private void ApplyFilter()
        {
            filtered.Clear();
            for (int i = 0; i < localMods.Count; i++)
            {
                if (ModSearch.Matches(localMods[i], activeQuery))
                {
                    filtered.Add(localMods[i]);
                }
            }
            UpdateCount();
        }

        private void UpdateCount()
        {
            if (countLabel == null)
            {
                return;
            }
            countLabel.text = HmbLocalization.Format(HmbConstants.LocalCountFormatKey, HmbConstants.DefaultLocalCountFormat, filtered.Count);
        }

        private void RenderList()
        {
            generation++;
            StopRender();
            DestroyCards();
            if (!initialized || owner == null || owner.prefab_mod_button == null)
            {
                return;
            }
            renderCoroutine = StartCoroutine(RenderIncrementally(generation));
        }

        private IEnumerator RenderIncrementally(int renderGeneration)
        {
            int renderedThisFrame = 0;
            for (int i = 0; i < filtered.Count; i++)
            {
                if (renderGeneration != generation || owner == null || content == null)
                {
                    renderCoroutine = null;
                    yield break;
                }
                Mods._mod mod = filtered[i];
                GameObject root = null;
                try
                {
                    root = UnityEngine.Object.Instantiate(owner.prefab_mod_button);
                    root.transform.SetParent(content, false);
                    Mods_Upload_Update_Button button = root.GetComponent<Mods_Upload_Update_Button>();
                    if (button == null)
                    {
                        throw new InvalidOperationException("Update source prefab has no Mods_Upload_Update_Button component.");
                    }
                    button.Set(mod, owner);
                    RuntimeUi.ArrangeSelectionCard(button);
                    cards.Add(root);
                    root.SetActive(true);
                }
                catch (Exception ex)
                {
                    if (root != null)
                    {
                        UnityEngine.Object.Destroy(root);
                    }
                    HmbLog.Warning("Failed to create local update-version card for '" + (mod == null ? "<null>" : mod.Title) + "': " + ex.Message);
                }

                renderedThisFrame++;
                if (renderedThisFrame >= HmbConstants.ChooserCardsPerFrame)
                {
                    renderedThisFrame = 0;
                    yield return null;
                }
            }
            responsiveGrid.Refresh();
            RuntimeUi.ResetScroll(scrollRect);
            renderCoroutine = null;
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
            ApplyFilter();
            RenderList();
        }

        private void ApplyLocalizedUi()
        {
            if (titleLabel != null)
            {
                titleLabel.text = HmbLocalization.Get(HmbConstants.UpdateSourceTitleKey, HmbConstants.DefaultUpdateSourceTitle);
            }
            if (search != null && search.Placeholder != null)
            {
                search.Placeholder.text = HmbLocalization.Get(HmbConstants.UpdateSourceSearchPlaceholderKey, HmbConstants.DefaultUpdateSourceSearchPlaceholder);
            }
            UpdateCount();
            HmbGameFont.ApplyAll(headerRoot);
        }

        private void OnLanguageReset()
        {
            HmbLocalization.Reload();
            ApplyLocalizedUi();
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
            if (content != null)
            {
                RuntimeUi.DestroyChildren(content);
            }
        }

        private void StopRender()
        {
            if (renderCoroutine != null)
            {
                StopCoroutine(renderCoroutine);
                renderCoroutine = null;
            }
        }

        private void OnDisable()
        {
            generation++;
            StopRender();
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            generation++;
            StopRender();
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
            if (layoutCaptured && grid != null)
            {
                grid.padding.top = originalPaddingTop;
            }
            if (headerRoot != null)
            {
                UnityEngine.Object.Destroy(headerRoot);
            }
            if (responsiveGrid != null) responsiveGrid.Restore();
            if (dock != null) dock.Restore();
            if (BrowserRegistry.UpdateSourceSearch == this)
            {
                BrowserRegistry.UpdateSourceSearch = null;
            }
        }
    }
}
