using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using MichskyUIGradient = UnityEngine.UI.Michsky.UI.ModernUIPack.UIGradient;
using CinematicBloom = UnityStandardAssets.CinematicEffects.Bloom;
using CinematicLensAberrations = UnityStandardAssets.CinematicEffects.LensAberrations;
using ImageEffectsAntialiasing = UnityStandardAssets.ImageEffects.Antialiasing;
using ImageEffectsBloom = UnityStandardAssets.ImageEffects.Bloom;
using ImageEffectsAAMode = UnityStandardAssets.ImageEffects.AAMode;

namespace IMUiFramework
{
    public static class IMUiBridges
    {
        private const string LogPrefix = "[IMUiFramework.Bridges]";

        public static bool TryGetDefaultGameCamera(out Camera camera)
        {
            camera = Camera.main;
            return camera != null;
        }

        public static CinematicBloom EnsureCinematicBloom(
            Camera camera,
            float intensity = 0.7f,
            float threshold = 0.9f,
            float radius = 2f,
            float softKnee = 0.5f,
            bool highQuality = true,
            bool antiFlicker = false,
            bool enabled = true)
        {
            if (camera == null)
            {
                return null;
            }

            CinematicBloom bloom = AddOrGetComponent<CinematicBloom>(camera.gameObject);
            CinematicBloom.Settings settings = bloom.settings;
            settings.intensity = Mathf.Max(0f, intensity);
            settings.threshold = Mathf.Max(0f, threshold);
            settings.radius = Mathf.Clamp(radius, 0f, 10f);
            settings.softKnee = Mathf.Clamp01(softKnee);
            settings.highQuality = highQuality;
            settings.antiFlicker = antiFlicker;
            bloom.settings = settings;
            bloom.enabled = enabled;
            return bloom;
        }

        public static CinematicLensAberrations EnsureCinematicLensAberrations(
            Camera camera,
            bool enableDistortion = false,
            float distortionAmount = 0f,
            bool enableVignette = false,
            float vignetteIntensity = 1.4f,
            float vignetteSmoothness = 0.8f,
            bool enableChromaticAberration = false,
            float chromaticAmount = 0f,
            bool enabled = true)
        {
            if (camera == null)
            {
                return null;
            }

            CinematicLensAberrations lens = AddOrGetComponent<CinematicLensAberrations>(camera.gameObject);

            CinematicLensAberrations.DistortionSettings dist = lens.distortion;
            dist.enabled = enableDistortion;
            dist.amount = distortionAmount;
            lens.distortion = dist;

            CinematicLensAberrations.VignetteSettings vignette = lens.vignette;
            vignette.enabled = enableVignette;
            vignette.intensity = Mathf.Max(0f, vignetteIntensity);
            vignette.smoothness = Mathf.Clamp01(vignetteSmoothness);
            lens.vignette = vignette;

            CinematicLensAberrations.ChromaticAberrationSettings chroma = lens.chromaticAberration;
            chroma.enabled = enableChromaticAberration;
            chroma.amount = Mathf.Max(0f, chromaticAmount);
            lens.chromaticAberration = chroma;

            lens.enabled = enabled;
            return lens;
        }

        public static ImageEffectsAntialiasing EnsureImageEffectsAntialiasing(
            Camera camera,
            ImageEffectsAAMode mode = ImageEffectsAAMode.FXAA3Console,
            float edgeThresholdMin = 0.05f,
            float edgeThreshold = 0.2f,
            float edgeSharpness = 4f,
            bool enabled = true)
        {
            if (camera == null)
            {
                return null;
            }

            ImageEffectsAntialiasing aa = AddOrGetComponent<ImageEffectsAntialiasing>(camera.gameObject);
            aa.mode = mode;
            aa.edgeThresholdMin = Mathf.Max(0f, edgeThresholdMin);
            aa.edgeThreshold = Mathf.Max(0f, edgeThreshold);
            aa.edgeSharpness = Mathf.Max(0f, edgeSharpness);
            aa.enabled = enabled;
            return aa;
        }

        public static ImageEffectsBloom EnsureImageEffectsBloom(
            Camera camera,
            float bloomIntensity = 0.5f,
            float bloomThreshold = 0.5f,
            int blurIterations = 2,
            bool enabled = true)
        {
            if (camera == null)
            {
                return null;
            }

            ImageEffectsBloom bloom = AddOrGetComponent<ImageEffectsBloom>(camera.gameObject);
            bloom.bloomIntensity = Mathf.Max(0f, bloomIntensity);
            bloom.bloomThreshold = Mathf.Max(0f, bloomThreshold);
            bloom.bloomBlurIterations = Mathf.Clamp(blurIterations, 1, 10);
            bloom.enabled = enabled;
            return bloom;
        }

        public static void SetKnownCameraEffectsEnabled(Camera camera, bool enabled)
        {
            if (camera == null)
            {
                return;
            }

            CinematicBloom cinematicBloom = camera.GetComponent<CinematicBloom>();
            if (cinematicBloom != null)
            {
                cinematicBloom.enabled = enabled;
            }

            CinematicLensAberrations lens = camera.GetComponent<CinematicLensAberrations>();
            if (lens != null)
            {
                lens.enabled = enabled;
            }

            ImageEffectsAntialiasing aa = camera.GetComponent<ImageEffectsAntialiasing>();
            if (aa != null)
            {
                aa.enabled = enabled;
            }

            ImageEffectsBloom bloom = camera.GetComponent<ImageEffectsBloom>();
            if (bloom != null)
            {
                bloom.enabled = enabled;
            }
        }

        public static bool TryFindModernDropdownTemplate(out CustomDropdown template)
        {
            template = null;
            PopupManager manager;
            if (!IMUiKit.TryGetPopupManager(out manager))
            {
                return false;
            }

            PopupManager._type[] preferredPopups =
            {
                PopupManager._type.main_menu_settings,
                PopupManager._type.settings_difficulty
            };

            for (int i = 0; i < preferredPopups.Length; i++)
            {
                GameObject popup;
                try
                {
                    popup = PopupManager.GetObject(preferredPopups[i]);
                }
                catch
                {
                    popup = null;
                }

                if (popup == null)
                {
                    continue;
                }

                CustomDropdown found = popup.GetComponentInChildren<CustomDropdown>(true);
                if (found != null)
                {
                    template = found;
                    return true;
                }
            }

            if (manager.popups != null)
            {
                for (int i = 0; i < manager.popups.Length; i++)
                {
                    PopupManager._popup entry = manager.popups[i];
                    if (entry == null || entry.obj == null)
                    {
                        continue;
                    }

                    CustomDropdown found = entry.obj.GetComponentInChildren<CustomDropdown>(true);
                    if (found != null)
                    {
                        template = found;
                        return true;
                    }
                }
            }

            template = UnityEngine.Object.FindObjectOfType<CustomDropdown>();
            return template != null;
        }

        public static bool TryCreateModernDropdown(
            Transform parent,
            string objectName,
            string label,
            IList<string> items,
            int selectedIndex,
            UnityAction<int> onChanged,
            out GameObject dropdownObject,
            out CustomDropdown dropdown)
        {
            dropdownObject = null;
            dropdown = null;
            if (parent == null)
            {
                return false;
            }

            CustomDropdown template;
            if (!TryFindModernDropdownTemplate(out template) || template == null)
            {
                Log("TryCreateModernDropdown failed: no CustomDropdown template found.");
                return false;
            }

            dropdownObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            dropdownObject.name = string.IsNullOrEmpty(objectName) ? "IMUiFramework_ModernDropdown" : objectName;
            dropdownObject.SetActive(true);
            IMUiKit.ClearLocalizationComponents(dropdownObject);

            dropdown = dropdownObject.GetComponent<CustomDropdown>();
            if (dropdown == null)
            {
                dropdown = dropdownObject.GetComponentInChildren<CustomDropdown>(true);
            }
            if (dropdown == null)
            {
                UnityEngine.Object.Destroy(dropdownObject);
                dropdownObject = null;
                return false;
            }

            SetDropdownLabel(dropdownObject, label);

            dropdown.dropdownItems.Clear();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    string text = items[i] ?? string.Empty;
                    dropdown.CreateNewItemFast(text, null);
                }
            }

            dropdown.dropdownEvent = new CustomDropdown.DropdownEvent();
            if (onChanged != null)
            {
                dropdown.dropdownEvent.AddListener(onChanged);
            }

            dropdown.SetupDropdown();
            int count = dropdown.dropdownItems != null ? dropdown.dropdownItems.Count : 0;
            if (count > 0)
            {
                int clamped = Mathf.Clamp(selectedIndex, 0, count - 1);
                dropdown.ChangeDropdownInfo(clamped);
            }

            return true;
        }

        public static bool TryCloneModernWindowManager(
            Transform parent,
            string objectName,
            out GameObject windowObject,
            out WindowManager windowManager)
        {
            windowObject = null;
            windowManager = null;
            if (parent == null)
            {
                return false;
            }

            WindowManager template = UnityEngine.Object.FindObjectOfType<WindowManager>();
            if (template == null)
            {
                Log("TryCloneModernWindowManager failed: no WindowManager template found.");
                return false;
            }

            windowObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            windowObject.name = string.IsNullOrEmpty(objectName) ? "IMUiFramework_ModernWindow" : objectName;
            windowObject.SetActive(true);
            windowManager = windowObject.GetComponent<WindowManager>();
            return windowManager != null;
        }

        public static SoftMaskScript AddSoftMask(
            Graphic target,
            Texture alphaMask = null,
            float cutOff = 0f,
            bool hardBlend = false,
            bool flipAlphaMask = false,
            bool dontClipMaskScalingRect = false,
            RectTransform maskArea = null)
        {
            if (target == null)
            {
                return null;
            }

            SoftMaskScript softMask = AddOrGetComponent<SoftMaskScript>(target.gameObject);
            softMask.MaskArea = maskArea != null ? maskArea : target.GetComponent<RectTransform>();
            softMask.AlphaMask = alphaMask;
            softMask.CutOff = Mathf.Clamp01(cutOff);
            softMask.HardBlend = hardBlend;
            softMask.FlipAlphaMask = flipAlphaMask;
            softMask.DontClipMaskScalingRect = dontClipMaskScalingRect;
            return softMask;
        }

        public static bool TryEnsureBoundTooltipItem(Canvas canvas, out BoundTooltipItem tooltipItem)
        {
            tooltipItem = UnityEngine.Object.FindObjectOfType<BoundTooltipItem>();
            if (tooltipItem != null)
            {
                return true;
            }

            if (canvas == null)
            {
                return false;
            }

            GameObject root = IMUiKit.CreateUiObject("IMUiFramework_BoundTooltip", canvas.transform);
            Image bg = root.AddComponent<Image>();
            bg.color = new Color32(20, 20, 20, 215);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(280f, 44f);

            Text txt = IMUiKit.CreateLegacyText(root.transform, "Text", string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0f, 0f);
            txtRect.anchorMax = new Vector2(1f, 1f);
            txtRect.offsetMin = new Vector2(10f, 8f);
            txtRect.offsetMax = new Vector2(-10f, -8f);

            tooltipItem = root.AddComponent<BoundTooltipItem>();
            tooltipItem.TooltipText = txt;
            tooltipItem.ToolTipOffset = new Vector3(14f, -14f, 0f);
            return tooltipItem != null;
        }

        public static BoundTooltipTrigger AddBoundTooltipTrigger(
            GameObject target,
            string text,
            bool useMousePosition = true,
            Vector3 offset = default(Vector3))
        {
            if (target == null)
            {
                return null;
            }

            BoundTooltipTrigger trigger = AddOrGetComponent<BoundTooltipTrigger>(target);
            trigger.text = text ?? string.Empty;
            trigger.useMousePosition = useMousePosition;
            trigger.offset = offset == default(Vector3) ? new Vector3(12f, -12f, 0f) : offset;
            return trigger;
        }

        public static bool TryCreateLegacyToolTipWidget(Canvas canvas, out ToolTip toolTip)
        {
            toolTip = UnityEngine.Object.FindObjectOfType<ToolTip>();
            if (toolTip != null)
            {
                return true;
            }

            if (canvas == null)
            {
                return false;
            }

            GameObject root = IMUiKit.CreateUiObject("IMUiFramework_ToolTip", canvas.transform);
            Image bg = root.AddComponent<Image>();
            bg.color = new Color32(20, 20, 20, 220);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(260f, 42f);

            Text text = IMUiKit.CreateLegacyText(root.transform, "Text", string.Empty, 14, TextAnchor.MiddleCenter, Color.white);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            toolTip = root.AddComponent<ToolTip>();
            return toolTip != null;
        }

        public static ToolTipTriggerBridge AddLegacyToolTipTrigger(
            GameObject target,
            ToolTip toolTip,
            string text)
        {
            if (target == null || toolTip == null)
            {
                return null;
            }

            ToolTipTriggerBridge trigger = AddOrGetComponent<ToolTipTriggerBridge>(target);
            trigger.ToolTip = toolTip;
            trigger.Text = text ?? string.Empty;
            return trigger;
        }

        public static bool TryCreateHoverTooltipWidget(Canvas canvas, out HoverTooltip hoverTooltip)
        {
            hoverTooltip = UnityEngine.Object.FindObjectOfType<HoverTooltip>();
            if (hoverTooltip != null)
            {
                return true;
            }

            if (canvas == null)
            {
                return false;
            }

            GameObject guiCamObject = GameObject.Find("GUICamera");
            if (guiCamObject == null || guiCamObject.GetComponent<Camera>() == null)
            {
                Log("TryCreateHoverTooltipWidget failed: GUICamera object is required by HoverTooltip.");
                return false;
            }

            GameObject root = IMUiKit.CreateUiObject("IMUiFramework_HoverTooltipBG", canvas.transform);
            Image bg = root.AddComponent<Image>();
            bg.color = new Color32(20, 20, 20, 204);
            RectTransform bgRect = root.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(0f, 0f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.sizeDelta = new Vector2(260f, 42f);

            GameObject content = IMUiKit.CreateUiObject("Content", root.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            Text txt = IMUiKit.CreateLegacyText(content.transform, "Text", string.Empty, 14, TextAnchor.MiddleCenter, Color.white);
            RectTransform textRect = txt.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            hoverTooltip = content.AddComponent<HoverTooltip>();
            hoverTooltip.horizontalPadding = 20;
            hoverTooltip.verticalPadding = 14;
            hoverTooltip.thisText = txt;
            hoverTooltip.hlG = hlg;
            hoverTooltip.bgImage = bgRect;
            return hoverTooltip != null;
        }

        public static HoverTooltipTriggerBridge AddHoverTooltipTrigger(
            GameObject target,
            HoverTooltip hoverTooltip,
            string text)
        {
            if (target == null || hoverTooltip == null)
            {
                return null;
            }

            HoverTooltipTriggerBridge trigger = AddOrGetComponent<HoverTooltipTriggerBridge>(target);
            trigger.HoverTooltip = hoverTooltip;
            trigger.Text = text ?? string.Empty;
            return trigger;
        }

        public static MichskyUIGradient AddUiGradient(
            Graphic target,
            Gradient gradient,
            MichskyUIGradient.Type type = MichskyUIGradient.Type.Vertical,
            MichskyUIGradient.Blend blend = MichskyUIGradient.Blend.Override,
            float offset = 0f,
            float zoom = 1f,
            bool modifyVertices = true)
        {
            if (target == null)
            {
                return null;
            }

            MichskyUIGradient uiGradient = AddOrGetComponent<MichskyUIGradient>(target.gameObject);
            uiGradient.GradientType = type;
            uiGradient.BlendMode = blend;
            uiGradient.Offset = offset;
            uiGradient.Zoom = zoom;
            uiGradient.ModifyVertices = modifyVertices;
            if (gradient != null)
            {
                uiGradient.EffectGradient = gradient;
            }

            target.SetVerticesDirty();
            return uiGradient;
        }

        public static MichskyUIGradient AddTwoColorUiGradient(
            Graphic target,
            Color a,
            Color b,
            MichskyUIGradient.Type type = MichskyUIGradient.Type.Vertical,
            MichskyUIGradient.Blend blend = MichskyUIGradient.Blend.Override,
            float offset = 0f,
            float zoom = 1f)
        {
            Gradient gradient = new Gradient();
            gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(a, 0f),
                new GradientColorKey(b, 1f)
            };
            return AddUiGradient(target, gradient, type, blend, offset, zoom, true);
        }

        public static bool TryCreateBridgeShowcasePopup(
            string popupName,
            string popupTitle,
            Vector2 panelSize,
            Camera camera,
            out PopupScaffold scaffold,
            out GameObject showcaseRoot)
        {
            scaffold = null;
            showcaseRoot = null;

            PopupScaffold created;
            if (!IMUiKit.TryCreatePopupScaffold(
                string.IsNullOrEmpty(popupName) ? "IMUiFramework_BridgeShowcasePopup" : popupName,
                string.IsNullOrEmpty(popupTitle) ? "UI Bridge Showcase" : popupTitle,
                panelSize,
                out created))
            {
                return false;
            }

            scaffold = created;
            Canvas canvas = ResolveCanvas(null, scaffold.Root != null ? scaffold.Root.transform : null);
            if (!TryCreateBridgeShowcaseContent(scaffold.ContentRoot, canvas, camera, out showcaseRoot))
            {
                return false;
            }

            IMUiKit.RebuildLayout(scaffold.ContentRoot);
            return true;
        }

        public static bool TryCreateBridgeShowcaseContent(
            Transform parent,
            Canvas tooltipCanvas,
            Camera camera,
            out GameObject showcaseRoot)
        {
            showcaseRoot = null;
            if (parent == null)
            {
                return false;
            }

            showcaseRoot = IMUiKit.CreateUiObject("IMUiFramework_BridgeShowcase", parent);
            VerticalLayoutGroup layout = showcaseRoot.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = showcaseRoot.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI header = IMUiKit.CreateText(
                showcaseRoot.transform,
                "Header",
                "Bridge Helper UIs",
                28,
                TextAlignmentOptions.Left,
                new Color32(56, 39, 74, 255));
            AddLayoutElement(header.gameObject, -1f, 36f);

            Text intro = IMUiKit.CreateLegacyText(
                showcaseRoot.transform,
                "Intro",
                "Ready-made helper panels for CinematicEffects, ImageEffects, ModernUIPack, UI Extensions, and UIGradient.",
                13,
                TextAnchor.MiddleLeft,
                new Color32(52, 52, 52, 255));
            AddLayoutElement(intro.gameObject, -1f, 22f);

            GameObject cameraPanel;
            TryCreateCameraEffectsHelperPanel(showcaseRoot.transform, camera, out cameraPanel);

            GameObject modernPanel;
            CustomDropdown sampleDropdown;
            TryCreateModernDropdownHelperPanel(
                showcaseRoot.transform,
                "IMUiFramework_ShowcaseDropdownPanel",
                "Quality",
                new string[]
                {
                    ModLocalization.GetRaw("Low"),
                    ModLocalization.GetRaw("Medium"),
                    ModLocalization.GetRaw("High"),
                    ModLocalization.GetRaw("Ultra")
                },
                1,
                null,
                out modernPanel,
                out sampleDropdown);

            GameObject tooltipPanel;
            Button tooltipButton;
            TryCreateTooltipHelperPanel(showcaseRoot.transform, tooltipCanvas, "Hover Me", "Bridge tooltip demo", out tooltipPanel, out tooltipButton);

            GameObject gradientPanel;
            Image preview;
            MichskyUIGradient uiGradient;
            TryCreateGradientPreviewHelperPanel(
                showcaseRoot.transform,
                "Gradient Preview",
                new Color32(244, 118, 170, 255),
                new Color32(113, 166, 255, 255),
                new Vector2(420f, 52f),
                out gradientPanel,
                out preview,
                out uiGradient);

            return true;
        }

        public static bool TryCreateCameraEffectsHelperPanel(
            Transform parent,
            Camera camera,
            out GameObject panelRoot)
        {
            panelRoot = null;
            if (parent == null)
            {
                return false;
            }

            panelRoot = CreateBridgePanelContainer(parent, "IMUiFramework_CameraEffectsPanel");
            CreateBridgeSectionTitle(panelRoot.transform, "CinematicEffects + ImageEffects");

            if (camera == null)
            {
                TryGetDefaultGameCamera(out camera);
            }

            Text status = IMUiKit.CreateLegacyText(panelRoot.transform, "Status", string.Empty, 13, TextAnchor.MiddleLeft, new Color32(45, 45, 45, 255));
            AddLayoutElement(status.gameObject, -1f, 22f);

            if (camera == null)
            {
                status.text = ModLocalization.GetRaw("No Camera found. Pass a camera or wait until game scene initializes.");
                return true;
            }

            CinematicBloom existingCinematic = camera.GetComponent<CinematicBloom>();
            ImageEffectsBloom existingImageBloom = camera.GetComponent<ImageEffectsBloom>();
            float cinematicIntensity = existingCinematic != null ? existingCinematic.settings.intensity : 0.7f;
            float imageIntensity = existingImageBloom != null ? existingImageBloom.bloomIntensity : 0.5f;

            Action refresh = delegate
            {
                bool enabled = AreKnownEffectsEnabled(camera);
                status.text = string.Format(
                    ModLocalization.GetRaw("FX: {0} | CineBloom: {1:0.00} | ImgBloom: {2:0.00}"),
                    enabled ? ModLocalization.GetRaw("Enabled") : ModLocalization.GetRaw("Disabled"),
                    cinematicIntensity,
                    imageIntensity);
            };

            GameObject rowEnable = CreateHorizontalButtonRow(panelRoot.transform, "EnableRow");
            CreateBridgeButton(rowEnable.transform, "EnableFX", "Enable FX", 120f, delegate { SetKnownCameraEffectsEnabled(camera, true); refresh(); });
            CreateBridgeButton(rowEnable.transform, "DisableFX", "Disable FX", 120f, delegate { SetKnownCameraEffectsEnabled(camera, false); refresh(); });
            CreateBridgeButton(rowEnable.transform, "CinematicPreset", "Cinematic", 120f, delegate
            {
                EnsureCinematicBloom(camera, 0.9f, 0.9f, 2.4f, 0.6f, true, true, true);
                EnsureCinematicLensAberrations(camera, false, 0f, true, 1.2f, 0.75f, false, 0f, true);
                refresh();
            });

            GameObject rowCinematic = CreateHorizontalButtonRow(panelRoot.transform, "CinematicAdjustRow");
            CreateBridgeButton(rowCinematic.transform, "CineMinus", "Cine -", 88f, delegate
            {
                cinematicIntensity = Mathf.Clamp(cinematicIntensity - 0.1f, 0f, 3f);
                EnsureCinematicBloom(camera, cinematicIntensity, 0.9f, 2f, 0.5f, true, false, true);
                refresh();
            });
            CreateBridgeButton(rowCinematic.transform, "CinePlus", "Cine +", 88f, delegate
            {
                cinematicIntensity = Mathf.Clamp(cinematicIntensity + 0.1f, 0f, 3f);
                EnsureCinematicBloom(camera, cinematicIntensity, 0.9f, 2f, 0.5f, true, false, true);
                refresh();
            });
            CreateBridgeButton(rowCinematic.transform, "AAFxaa", "AA FXAA", 100f, delegate
            {
                EnsureImageEffectsAntialiasing(camera, ImageEffectsAAMode.FXAA3Console, 0.05f, 0.2f, 4f, true);
                refresh();
            });

            GameObject rowImage = CreateHorizontalButtonRow(panelRoot.transform, "ImageAdjustRow");
            CreateBridgeButton(rowImage.transform, "ImgMinus", "Img -", 88f, delegate
            {
                imageIntensity = Mathf.Clamp(imageIntensity - 0.1f, 0f, 3f);
                EnsureImageEffectsBloom(camera, imageIntensity, 0.5f, 2, true);
                refresh();
            });
            CreateBridgeButton(rowImage.transform, "ImgPlus", "Img +", 88f, delegate
            {
                imageIntensity = Mathf.Clamp(imageIntensity + 0.1f, 0f, 3f);
                EnsureImageEffectsBloom(camera, imageIntensity, 0.5f, 2, true);
                refresh();
            });
            CreateBridgeButton(rowImage.transform, "ImagePreset", "ImageFX", 100f, delegate
            {
                EnsureImageEffectsBloom(camera, 0.65f, 0.55f, 3, true);
                EnsureImageEffectsAntialiasing(camera, ImageEffectsAAMode.FXAA3Console, 0.05f, 0.2f, 4f, true);
                imageIntensity = 0.65f;
                refresh();
            });

            refresh();
            return true;
        }

        public static bool TryCreateModernDropdownHelperPanel(
            Transform parent,
            string objectName,
            string label,
            IList<string> items,
            int selectedIndex,
            UnityAction<int> onChanged,
            out GameObject panelRoot,
            out CustomDropdown dropdown)
        {
            panelRoot = null;
            dropdown = null;
            if (parent == null)
            {
                return false;
            }

            panelRoot = CreateBridgePanelContainer(parent, string.IsNullOrEmpty(objectName) ? "IMUiFramework_ModernDropdownPanel" : objectName);
            CreateBridgeSectionTitle(panelRoot.transform, "ModernUIPack Dropdown Bridge");

            Text description = IMUiKit.CreateLegacyText(
                panelRoot.transform,
                "Description",
                "Clones a native CustomDropdown template and rewires items/events for mod use.",
                12,
                TextAnchor.MiddleLeft,
                new Color32(45, 45, 45, 255));
            AddLayoutElement(description.gameObject, -1f, 20f);

            GameObject dropdownObject;
            if (!TryCreateModernDropdown(panelRoot.transform, "BridgeDropdown", label, items, selectedIndex, onChanged, out dropdownObject, out dropdown))
            {
                Text missing = IMUiKit.CreateLegacyText(
                    panelRoot.transform,
                    "MissingTemplate",
                    "No ModernUIPack dropdown template found yet. Open a base settings popup first, then retry.",
                    12,
                    TextAnchor.MiddleLeft,
                    new Color32(170, 60, 60, 255));
                AddLayoutElement(missing.gameObject, -1f, 20f);
                return false;
            }

            AddLayoutElement(dropdownObject, -1f, 40f);
            return true;
        }

        public static bool TryCreateTooltipHelperPanel(
            Transform parent,
            Canvas canvas,
            string buttonLabel,
            string tooltipText,
            out GameObject panelRoot,
            out Button demoButton)
        {
            panelRoot = null;
            demoButton = null;
            if (parent == null)
            {
                return false;
            }

            panelRoot = CreateBridgePanelContainer(parent, "IMUiFramework_TooltipPanel");
            CreateBridgeSectionTitle(panelRoot.transform, "UI Extensions Tooltips");

            Canvas resolvedCanvas = ResolveCanvas(canvas, parent);
            Text status = IMUiKit.CreateLegacyText(panelRoot.transform, "Status", string.Empty, 12, TextAnchor.MiddleLeft, new Color32(45, 45, 45, 255));
            AddLayoutElement(status.gameObject, -1f, 20f);
            if (resolvedCanvas == null)
            {
                status.text = ModLocalization.GetRaw("No canvas found for tooltip widgets.");
                return false;
            }

            string label = string.IsNullOrEmpty(buttonLabel) ? ModLocalization.GetRaw("Hover Me") : buttonLabel;
            string tip = string.IsNullOrEmpty(tooltipText) ? ModLocalization.GetRaw("Tooltip bridge demo") : tooltipText;

            GameObject row = CreateHorizontalButtonRow(panelRoot.transform, "TooltipButtonRow");
            demoButton = CreateBridgeButton(row.transform, "BoundTooltipDemoButton", label, 180f, null);
            BoundTooltipItem boundItem;
            bool boundReady = TryEnsureBoundTooltipItem(resolvedCanvas, out boundItem);
            if (boundReady)
            {
                AddBoundTooltipTrigger(demoButton.gameObject, tip, true, new Vector3(12f, -12f, 0f));
            }

            Button legacyButton = CreateBridgeButton(row.transform, "LegacyTooltipDemoButton", "Legacy", 96f, null);
            ToolTip legacyTip;
            bool legacyReady = TryCreateLegacyToolTipWidget(resolvedCanvas, out legacyTip);
            if (legacyReady && legacyTip != null)
            {
                AddLegacyToolTipTrigger(legacyButton.gameObject, legacyTip, tip + " " + ModLocalization.GetRaw("(Legacy)"));
            }

            Button hoverButton = CreateBridgeButton(row.transform, "HoverTooltipDemoButton", "Hover", 96f, null);
            HoverTooltip hoverTip;
            bool hoverReady = TryCreateHoverTooltipWidget(resolvedCanvas, out hoverTip);
            if (hoverReady && hoverTip != null)
            {
                AddHoverTooltipTrigger(hoverButton.gameObject, hoverTip, tip + " " + ModLocalization.GetRaw("(Hover)"));
            }

            status.text = string.Format(
                ModLocalization.GetRaw("Bound: {0} | Legacy: {1} | Hover: {2}"),
                boundReady ? ModLocalization.GetRaw("Ready") : ModLocalization.GetRaw("Missing"),
                legacyReady ? ModLocalization.GetRaw("Ready") : ModLocalization.GetRaw("Missing"),
                hoverReady ? ModLocalization.GetRaw("Ready") : ModLocalization.GetRaw("Missing"));

            return true;
        }

        public static bool TryCreateGradientPreviewHelperPanel(
            Transform parent,
            string label,
            Color startColor,
            Color endColor,
            Vector2 previewSize,
            out GameObject panelRoot,
            out Image preview,
            out MichskyUIGradient gradient)
        {
            panelRoot = null;
            preview = null;
            gradient = null;
            if (parent == null)
            {
                return false;
            }

            panelRoot = CreateBridgePanelContainer(parent, "IMUiFramework_GradientPanel");
            CreateBridgeSectionTitle(panelRoot.transform, "UIGradient Bridge");

            Text info = IMUiKit.CreateLegacyText(
                panelRoot.transform,
                "Info",
                string.IsNullOrEmpty(label) ? "Preview" : label,
                12,
                TextAnchor.MiddleLeft,
                new Color32(45, 45, 45, 255));
            AddLayoutElement(info.gameObject, -1f, 18f);

            GameObject previewObject = IMUiKit.CreateUiObject("GradientPreview", panelRoot.transform);
            preview = previewObject.AddComponent<Image>();
            preview.color = Color.white;
            AddLayoutElement(previewObject, previewSize.x > 1f ? previewSize.x : 380f, previewSize.y > 1f ? previewSize.y : 52f);

            Color colorA = startColor;
            Color colorB = endColor;
            Image previewImage = preview;
            gradient = AddTwoColorUiGradient(previewImage, colorA, colorB, MichskyUIGradient.Type.Horizontal, MichskyUIGradient.Blend.Override, 0f, 1f);

            GameObject buttonRow = CreateHorizontalButtonRow(panelRoot.transform, "GradientButtons");
            CreateBridgeButton(buttonRow.transform, "Swap", "Swap", 96f, delegate
            {
                Color swap = colorA;
                colorA = colorB;
                colorB = swap;
                AddTwoColorUiGradient(previewImage, colorA, colorB, MichskyUIGradient.Type.Horizontal, MichskyUIGradient.Blend.Override, 0f, 1f);
            });
            CreateBridgeButton(buttonRow.transform, "Randomize", "Random", 96f, delegate
            {
                colorA = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
                colorB = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
                AddTwoColorUiGradient(previewImage, colorA, colorB, MichskyUIGradient.Type.Horizontal, MichskyUIGradient.Blend.Override, 0f, 1f);
            });

            return true;
        }

        private static T AddOrGetComponent<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                return null;
            }

            T comp = go.GetComponent<T>();
            if (comp != null)
            {
                return comp;
            }

            return go.AddComponent<T>();
        }

        private static GameObject CreateBridgePanelContainer(Transform parent, string objectName)
        {
            GameObject panel = IMUiKit.CreateUiObject(string.IsNullOrEmpty(objectName) ? "IMUiFramework_BridgePanel" : objectName, parent);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color32(250, 249, 248, 255);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLayoutElement(panel, -1f, -1f);
            return panel;
        }

        private static TextMeshProUGUI CreateBridgeSectionTitle(Transform parent, string text)
        {
            TextMeshProUGUI title = IMUiKit.CreateText(
                parent,
                "SectionTitle",
                text ?? string.Empty,
                22,
                TextAlignmentOptions.Left,
                new Color32(62, 45, 84, 255));
            AddLayoutElement(title.gameObject, -1f, 30f);
            return title;
        }

        private static GameObject CreateHorizontalButtonRow(Transform parent, string objectName)
        {
            GameObject row = IMUiKit.CreateUiObject(string.IsNullOrEmpty(objectName) ? "ButtonRow" : objectName, parent);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            AddLayoutElement(row, -1f, 40f);
            return row;
        }

        private static Button CreateBridgeButton(
            Transform parent,
            string objectName,
            string label,
            float width,
            UnityAction onClick)
        {
            Button button = IMUiKit.CreateStyledButton(
                parent,
                string.IsNullOrEmpty(objectName) ? "Button" : objectName,
                label ?? string.Empty,
                width > 1f ? width : 140f,
                32f,
                onClick);
            AddLayoutElement(button.gameObject, width > 1f ? width : 140f, 32f);
            return button;
        }

        private static void AddLayoutElement(GameObject obj, float preferredWidth, float preferredHeight)
        {
            if (obj == null)
            {
                return;
            }

            LayoutElement layout = obj.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = obj.AddComponent<LayoutElement>();
            }

            if (preferredWidth > 0f)
            {
                layout.preferredWidth = preferredWidth;
            }
            if (preferredHeight > 0f)
            {
                layout.preferredHeight = preferredHeight;
            }
        }

        private static Canvas ResolveCanvas(Canvas canvas, Transform parent)
        {
            if (canvas != null)
            {
                return canvas;
            }

            if (parent != null)
            {
                Canvas fromParent = parent.GetComponentInParent<Canvas>();
                if (fromParent != null)
                {
                    return fromParent;
                }
            }

            return UnityEngine.Object.FindObjectOfType<Canvas>();
        }

        private static bool AreKnownEffectsEnabled(Camera camera)
        {
            if (camera == null)
            {
                return false;
            }

            CinematicBloom cinematicBloom = camera.GetComponent<CinematicBloom>();
            if (cinematicBloom != null && cinematicBloom.enabled)
            {
                return true;
            }

            CinematicLensAberrations lens = camera.GetComponent<CinematicLensAberrations>();
            if (lens != null && lens.enabled)
            {
                return true;
            }

            ImageEffectsAntialiasing aa = camera.GetComponent<ImageEffectsAntialiasing>();
            if (aa != null && aa.enabled)
            {
                return true;
            }

            ImageEffectsBloom bloom = camera.GetComponent<ImageEffectsBloom>();
            return bloom != null && bloom.enabled;
        }

        private static void SetDropdownLabel(GameObject dropdownRoot, string label)
        {
            if (dropdownRoot == null || string.IsNullOrEmpty(label))
            {
                return;
            }

            Lang_Button lang = dropdownRoot.GetComponentInChildren<Lang_Button>(true);
            if (lang != null)
            {
                lang.Constant = string.Empty;
            }

            TextMeshProUGUI text = null;
            if (lang != null)
            {
                text = lang.GetComponent<TextMeshProUGUI>();
            }
            if (text == null)
            {
                text = dropdownRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void Log(string message)
        {
            Debug.Log(LogPrefix + " " + message);
        }
    }

    public sealed class ToolTipTriggerBridge : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public ToolTip ToolTip;
        public string Text;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ToolTip != null)
            {
                ToolTip.SetTooltip(Text ?? string.Empty);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ToolTip != null)
            {
                ToolTip.HideTooltip();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (ToolTip != null)
            {
                ToolTip.SetTooltip(Text ?? string.Empty);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (ToolTip != null)
            {
                ToolTip.HideTooltip();
            }
        }
    }

    public sealed class HoverTooltipTriggerBridge : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public HoverTooltip HoverTooltip;
        public string Text;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (HoverTooltip != null)
            {
                HoverTooltip.SetTooltip(Text ?? string.Empty);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (HoverTooltip != null)
            {
                HoverTooltip.HideTooltip();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (HoverTooltip != null)
            {
                HoverTooltip.SetTooltip(Text ?? string.Empty);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (HoverTooltip != null)
            {
                HoverTooltip.HideTooltip();
            }
        }
    }
}
