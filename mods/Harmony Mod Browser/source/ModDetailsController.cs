using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace HarmonyModBrowser
{
    internal sealed class ModDetailsController : MonoBehaviour
    {
        private const string OverlayName = "HMB Mod Details";
        private const string PanelName = "Details Panel";
        private const string TitleObjectName = "Details Title";
        private const string CloseObjectName = "Close Details";
        private const string PortraitObjectName = "Mod Thumbnail";
        private const string MissingPortraitObjectName = "Missing Thumbnail";
        private const string ScrollObjectName = "Full Mod Details";
        private const string NameObjectName = "Mod Name";
        private const string VersionObjectName = "Mod Version";
        private const string AuthorObjectName = "Mod Author";
        private const string DescriptionHeadingObjectName = "Description Heading";
        private const string DescriptionObjectName = "Mod Description";
        private const string TitleKey = "HMB_DETAILS_TITLE";
        private const string VersionKey = "HMB_DETAILS_VERSION";
        private const string AuthorKey = "HMB_DETAILS_AUTHOR";
        private const string DescriptionKey = "HMB_DETAILS_DESCRIPTION";
        private const string NotProvidedKey = "HMB_DETAILS_NOT_PROVIDED";
        private const string MissingThumbnailKey = "HMB_DETAILS_NO_THUMBNAIL";
        private const string MissingDescriptionKey = "HMB_DETAILS_NO_DESCRIPTION";
        private const string DefaultTitle = "Mod details";
        private const string DefaultVersion = "Version: {0}";
        private const string DefaultAuthor = "Author: {0}";
        private const string DefaultDescription = "Description";
        private const string DefaultNotProvided = "Not provided";
        private const string DefaultMissingThumbnail = "No thumbnail available.";
        private const string DefaultMissingDescription = "No description provided.";
        private const float Inset = 18f;
        private const float HeaderTop = 12f;
        private const float HeaderHeight = 50f;
        private const float BodyTop = 82f;
        private const float CloseWidth = 144f;
        private const float PortraitFraction = 0.28f;
        private const float MaximumPortraitSize = 256f;
        private const float HeadingFontSize = 23f;
        private const float BodyFontSize = 17f;
        private const int TextPadding = 10;
        private const float TextGap = 12f;
        private const float PortraitPixelsPerUnit = 100f;

        private Mods._mod mod;
        private GameObject overlay;
        private RectTransform panel;
        private RectTransform portraitRect;
        private Image portrait;
        private TextMeshProUGUI missingPortrait, title, nameLabel, versionLabel, authorLabel, descriptionHeading, description;
        private Button closeButton;
        private ScrollAreaParts body;
        private Coroutine thumbnailWork;
        private UnityWebRequest thumbnailRequest;
        private Sprite ownedSprite;
        private Texture2D ownedTexture;
        private Vector2 laidOutSize;

        internal static void Show(Mods_Popup popup, Mods._mod selected)
        {
            if (popup == null || selected == null) return;
            ModDetailsController controller = popup.GetComponent<ModDetailsController>();
            if (controller == null) controller = popup.gameObject.AddComponent<ModDetailsController>();
            controller.Open(popup, selected);
        }

        private void Awake() { Language.onReset += OnLanguageReset; }

        private void Open(Mods_Popup popup, Mods._mod selected)
        {
            Close();
            mod = selected;
            TextMeshProUGUI template = RuntimeUi.FindTextTemplate(popup);
            overlay = RuntimeUi.CreatePanel(OverlayName, popup.transform, RuntimeUi.BackdropColor);
            RuntimeUi.SetStretch(overlay.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            overlay.transform.SetAsLastSibling();
            panel = RuntimeUi.CreatePanel(PanelName, overlay.transform, RuntimeUi.PanelColor).GetComponent<RectTransform>();
            ScreenFittedPanel.Attach(panel);

            title = RuntimeUi.CreateText(TitleObjectName, panel, string.Empty, HeadingFontSize,
                TextAlignmentOptions.MidlineLeft, template, RuntimeUi.TextColor);
            RuntimeUi.TopRow(title.rectTransform, HeaderTop, HeaderHeight, Inset, CloseWidth + Inset * 2f);
            title.enableWordWrapping = true;
            closeButton = RuntimeUi.CreateButton(CloseObjectName, panel,
                HmbLocalization.Get(HmbConstants.CloseKey, HmbConstants.DefaultClose), template, Close);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
            closeRect.sizeDelta = new Vector2(CloseWidth, HeaderHeight);
            closeRect.anchoredPosition = new Vector2(-Inset, -HeaderTop);
            closeRect.localScale = Vector3.one;

            portraitRect = RuntimeUi.CreatePanel(PortraitObjectName, panel, RuntimeUi.FieldColor).GetComponent<RectTransform>();
            portrait = portraitRect.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            missingPortrait = RuntimeUi.CreateText(MissingPortraitObjectName, portraitRect,
                string.Empty, BodyFontSize, TextAlignmentOptions.Center, template, RuntimeUi.MutedTextColor);
            RuntimeUi.SetStretch(missingPortrait.rectTransform, Inset, Inset, Inset, Inset);
            missingPortrait.enableWordWrapping = true;

            body = RuntimeUi.CreateScrollArea(ScrollObjectName, panel);
            VerticalLayoutGroup textLayout = body.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.padding = new RectOffset(TextPadding, TextPadding, TextPadding, TextPadding);
            textLayout.spacing = TextGap;
            textLayout.childControlWidth = textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            nameLabel = AddBodyText(NameObjectName, HeadingFontSize, template);
            nameLabel.fontStyle = FontStyles.Bold;
            versionLabel = AddBodyText(VersionObjectName, BodyFontSize, template);
            authorLabel = AddBodyText(AuthorObjectName, BodyFontSize, template);
            descriptionHeading = AddBodyText(DescriptionHeadingObjectName, BodyFontSize, template);
            descriptionHeading.fontStyle = FontStyles.Bold;
            description = AddBodyText(DescriptionObjectName, BodyFontSize, template);
            ApplyText();
            LateUpdate();
            RuntimeUi.ResetScroll(body.ScrollRect);
            thumbnailWork = StartCoroutine(LoadThumbnail());
        }

        private TextMeshProUGUI AddBodyText(string objectName, float size, TextMeshProUGUI template)
        {
            TextMeshProUGUI label = RuntimeUi.CreateText(objectName, body.Content, string.Empty,
                size, TextAlignmentOptions.TopLeft, template, RuntimeUi.TextColor);
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = false;
            return label;
        }

        private void LateUpdate()
        {
            if (panel == null || body == null || panel.rect.size == laidOutSize) return;
            laidOutSize = panel.rect.size;
            float leftColumn = panel.rect.width * PortraitFraction;
            float portraitSize = Mathf.Min(MaximumPortraitSize, leftColumn - Inset * 2f);
            portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.anchoredPosition = new Vector2(Inset, -BodyTop);
            portraitRect.sizeDelta = new Vector2(portraitSize, portraitSize);
            RuntimeUi.SetStretch(body.ScrollRect.GetComponent<RectTransform>(), leftColumn, Inset, Inset, BodyTop);
        }

        private void ApplyText()
        {
            if (overlay == null || mod == null) return;
            title.text = HmbLocalization.Get(TitleKey, DefaultTitle);
            string absent = HmbLocalization.Get(NotProvidedKey, DefaultNotProvided);
            nameLabel.text = string.IsNullOrWhiteSpace(mod.Title) ? mod.ModName : mod.Title;
            versionLabel.text = HmbLocalization.Format(VersionKey, DefaultVersion,
                string.IsNullOrWhiteSpace(mod.Version) ? absent : mod.Version);
            authorLabel.text = HmbLocalization.Format(AuthorKey, DefaultAuthor,
                string.IsNullOrWhiteSpace(mod.Author) ? absent : mod.Author);
            descriptionHeading.text = HmbLocalization.Get(DescriptionKey, DefaultDescription);
            description.text = string.IsNullOrWhiteSpace(mod.Description)
                ? HmbLocalization.Get(MissingDescriptionKey, DefaultMissingDescription) : mod.Description;
            missingPortrait.text = HmbLocalization.Get(MissingThumbnailKey, DefaultMissingThumbnail);
            RuntimeUi.SetButtonLabel(closeButton, HmbLocalization.Get(HmbConstants.CloseKey, HmbConstants.DefaultClose));
            HmbGameFont.ApplyAll(overlay);
        }

        private IEnumerator LoadThumbnail()
        {
            string path;
            try { path = mod.GetThumbPath(); }
            catch { yield break; }
            if (!File.Exists(path)) yield break;
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(new Uri(Path.GetFullPath(path)).AbsoluteUri, true);
            thumbnailRequest = request;
            try
            {
                yield return request.SendWebRequest();
                if (overlay == null || request.isHttpError || request.isNetworkError) yield break;
                ownedTexture = DownloadHandlerTexture.GetContent(request);
                if (ownedTexture == null) yield break;
                ownedSprite = Sprite.Create(ownedTexture, new Rect(0f, 0f, ownedTexture.width, ownedTexture.height),
                    new Vector2(0.5f, 0.5f), PortraitPixelsPerUnit);
                portrait.sprite = ownedSprite;
                missingPortrait.gameObject.SetActive(false);
            }
            finally
            {
                if (thumbnailRequest == request) thumbnailRequest = null;
                request.Dispose();
                thumbnailWork = null;
            }
        }

        private void OnLanguageReset()
        {
            ApplyText();
            if (body != null) RuntimeUi.ResetScroll(body.ScrollRect);
        }

        internal void Close()
        {
            if (thumbnailWork != null) StopCoroutine(thumbnailWork);
            thumbnailWork = null;
            if (thumbnailRequest != null)
            {
                thumbnailRequest.Abort();
                thumbnailRequest.Dispose();
                thumbnailRequest = null;
            }
            if (overlay != null)
            {
                overlay.SetActive(false);
                UnityEngine.Object.Destroy(overlay);
            }
            if (ownedSprite != null) UnityEngine.Object.Destroy(ownedSprite);
            if (ownedTexture != null) UnityEngine.Object.Destroy(ownedTexture);
            ownedSprite = null;
            ownedTexture = null;
            overlay = null;
            panel = null;
            laidOutSize = Vector2.zero;
            body = null;
            mod = null;
        }

        private void OnDisable() { Close(); }

        private void OnDestroy()
        {
            Language.onReset -= OnLanguageReset;
            Close();
        }
    }
}
