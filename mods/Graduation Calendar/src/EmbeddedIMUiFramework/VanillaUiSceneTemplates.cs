using System;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    /// <summary>
    /// Access to UI patterns that are serialized in Idol Manager's loaded scenes rather than
    /// registered as Resources prefabs. Popup roots remain addressable while closed/inactive,
    /// because PopupManager keeps serialized GameObject references to them.
    /// </summary>
    public static class VanillaUiSceneTemplates
    {
        private const string ProducerContractsSliderPath = "Panel/Slider";
        private const string ProducerSalariesSliderPath = "Panel/Slider";
        private const string ProducerLoansSliderPath = "Panel/Credit History/Slider";

        // Scene-derived dimensions shared by Producer Contracts, Salaries and Loans.
        // All three lists use the same Slider + SliderDefault scrollbar surrogate.
        public const float ProducerListSliderWidth = 20f;
        public const float ProducerListSliderScale = 1.1004126f;
        public const float ProducerListSliderRightCenterInset = 19.34f;
        public const float ProducerListViewportRightInset = 26f;
        public const float ProducerListHandleDiameter = 11.14f;
        public const float ProducerListHandleSlideInset = 20f;
        public const float ProducerListBackgroundWidth = 4.21f;

        private static Slider cachedProducerListSliderTemplate;

        /// <summary>
        /// Returns a popup root without opening or activating it. This is safe for closed popups:
        /// PopupManager's serialized entry already references the inactive scene object.
        /// </summary>
        public static bool TryGetPopupRoot(PopupManager._type popupType, out GameObject popupRoot)
        {
            return VanillaUiSceneCatalog.TryGetPopupRoot(popupType, out popupRoot);
        }

        /// <summary>
        /// Finds a child below a serialized popup root, including inactive descendants.
        /// </summary>
        public static bool TryFindPopupChild(PopupManager._type popupType, string relativePath, out Transform child)
        {
            return VanillaUiSceneCatalog.TryFindPopupChild(popupType, relativePath, out child);
        }

        /// <summary>
        /// Backward-compatible exact clone helper. Version 3 callers that want controller stripping can use
        /// VanillaUiSceneCatalog.TryClonePopupChild with VanillaUiCloneMode.Template or VisualOnly.
        /// </summary>
        public static bool TryClonePopupChild(
            PopupManager._type popupType,
            string relativePath,
            Transform parent,
            string objectName,
            out GameObject instance)
        {
            return VanillaUiSceneCatalog.TryClonePopupChild(
                popupType,
                relativePath,
                parent,
                objectName,
                VanillaUiCloneMode.Exact,
                true,
                out instance);
        }

        /// <summary>
        /// Clones Idol Manager's native producer-list scroll indicator and binds it to a ScrollRect.
        /// This intentionally uses UnityEngine.UI.Slider, not Scrollbar. Contracts, Salaries and
        /// Loans all leave ScrollRect.verticalScrollbar null and manually synchronize a fixed-size
        /// circular Slider handle through SliderDefault. That is why the vanilla thumb never grows
        /// into the proportional pill produced by Scrollbar.size.
        /// </summary>
        public static bool TryCreateProducerListScrollSlider(
            Transform parent,
            ScrollRect target,
            string objectName,
            out GameObject instance,
            out Slider slider)
        {
            instance = null;
            slider = null;
            if (parent == null || target == null)
            {
                return false;
            }

            Slider template;
            if (!TryGetProducerListSliderTemplate(out template) || template == null)
            {
                return false;
            }

            instance = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            if (instance == null)
            {
                return false;
            }

            instance.name = string.IsNullOrEmpty(objectName) ? "VanillaListSlider" : objectName;
            IMUiKit.ApplyLayerRecursively(instance, parent.gameObject.layer);

            slider = instance.GetComponent<Slider>();
            if (slider == null)
            {
                slider = IMUiCompat.GetComponentInChildren<Slider>(instance);
            }
            if (slider == null)
            {
                UnityEngine.Object.Destroy(instance);
                instance = null;
                return false;
            }

            // The cloned scene Slider has a serialized listener pointing back to the original
            // Contracts/Salaries/Loans ScrollRect. Clear it before attaching the new target.
            slider.onValueChanged = new Slider.SliderEvent();
            slider.direction = Slider.Direction.BottomToTop;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            // ScrollRect must not own a Unity Scrollbar here. Its automatic size calculation is
            // precisely the behavior the vanilla producer-list pattern avoids.
            target.verticalScrollbar = null;
            target.verticalScrollbarSpacing = 0f;

            ReserveProducerListViewportGutter(target);

            ProducerListScrollBinding binding = instance.GetComponent<ProducerListScrollBinding>();
            if (binding == null)
            {
                binding = instance.AddComponent<ProducerListScrollBinding>();
            }
            binding.Initialize(target, slider);

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetAsLastSibling();
            }

            instance.SetActive(true);
            return true;
        }

        /// <summary>
        /// Reserves the same approximately 26-unit right edge used by the producer list viewport,
        /// so content does not sit beneath the separate Slider track.
        /// </summary>
        public static void ReserveProducerListViewportGutter(ScrollRect target)
        {
            if (target == null || target.viewport == null)
            {
                return;
            }

            RectTransform viewport = target.viewport;
            Vector2 offsetMax = viewport.offsetMax;
            if (offsetMax.x > -ProducerListViewportRightInset)
            {
                offsetMax.x = -ProducerListViewportRightInset;
                viewport.offsetMax = offsetMax;
            }
        }

        private static bool TryGetProducerListSliderTemplate(out Slider template)
        {
            template = cachedProducerListSliderTemplate;
            if (template != null)
            {
                return true;
            }

            if (TryGetSliderAt(PopupManager._type.producer_contracts, ProducerContractsSliderPath, out template)
                || TryGetSliderAt(PopupManager._type.producer_salaries, ProducerSalariesSliderPath, out template)
                || TryGetSliderAt(PopupManager._type.producer_loans, ProducerLoansSliderPath, out template))
            {
                cachedProducerListSliderTemplate = template;
                return true;
            }

            template = null;
            return false;
        }

        private static bool TryGetSliderAt(PopupManager._type popupType, string relativePath, out Slider slider)
        {
            slider = null;
            Transform child;
            if (!TryFindPopupChild(popupType, relativePath, out child) || child == null)
            {
                return false;
            }

            slider = child.GetComponent<Slider>();
            return slider != null;
        }

        /// <summary>
        /// Two-way normalized-position bridge matching the game's SliderDefault wiring while also
        /// keeping the cloned root sized to the target ScrollRect at arbitrary custom-popup sizes.
        /// </summary>
        private sealed class ProducerListScrollBinding : MonoBehaviour
        {
            private ScrollRect scrollRect;
            private Slider slider;
            private RectTransform sliderRect;
            private RectTransform scrollRectTransform;
            private bool suppressEvents;
            private float lastTargetHeight = -1f;

            internal void Initialize(ScrollRect target, Slider targetSlider)
            {
                Detach();
                scrollRect = target;
                slider = targetSlider;
                sliderRect = slider != null ? slider.GetComponent<RectTransform>() : null;
                scrollRectTransform = scrollRect != null ? scrollRect.GetComponent<RectTransform>() : null;

                if (scrollRect == null || slider == null)
                {
                    return;
                }

                slider.onValueChanged.AddListener(OnSliderValueChanged);
                scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
                UpdateGeometry(true);
                SyncFromScrollRect();
            }

            private void OnEnable()
            {
                UpdateGeometry(true);
                SyncFromScrollRect();
            }

            private void LateUpdate()
            {
                UpdateGeometry(false);
            }

            private void OnDestroy()
            {
                Detach();
            }

            private void Detach()
            {
                if (scrollRect != null)
                {
                    scrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);
                }
                if (slider != null)
                {
                    slider.onValueChanged.RemoveListener(OnSliderValueChanged);
                }
            }

            private void OnScrollRectValueChanged(Vector2 value)
            {
                if (suppressEvents || slider == null)
                {
                    return;
                }

                suppressEvents = true;
                slider.SetValueWithoutNotify(value.y);
                suppressEvents = false;
            }

            private void OnSliderValueChanged(float value)
            {
                if (suppressEvents || scrollRect == null)
                {
                    return;
                }

                suppressEvents = true;
                scrollRect.verticalNormalizedPosition = value;
                suppressEvents = false;
            }

            private void SyncFromScrollRect()
            {
                if (scrollRect == null || slider == null)
                {
                    return;
                }

                suppressEvents = true;
                slider.SetValueWithoutNotify(scrollRect.verticalNormalizedPosition);
                suppressEvents = false;
            }

            private void UpdateGeometry(bool force)
            {
                if (sliderRect == null || scrollRectTransform == null)
                {
                    return;
                }

                float targetHeight = scrollRectTransform.rect.height;
                if (targetHeight <= 0f)
                {
                    return;
                }
                if (!force && Mathf.Abs(targetHeight - lastTargetHeight) < 0.01f)
                {
                    return;
                }

                lastTargetHeight = targetHeight;
                float scale = ProducerListSliderScale;
                sliderRect.anchorMin = new Vector2(1f, 0.5f);
                sliderRect.anchorMax = new Vector2(1f, 0.5f);
                sliderRect.pivot = new Vector2(0.5f, 0.5f);
                sliderRect.localScale = new Vector3(scale, scale, 1f);
                sliderRect.sizeDelta = new Vector2(ProducerListSliderWidth, targetHeight / scale);
                sliderRect.anchoredPosition = new Vector2(-ProducerListSliderRightCenterInset, 0f);
            }
        }
    }
}
