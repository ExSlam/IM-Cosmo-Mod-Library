using System;
using StaffPortraits.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StaffPortraits
{
    // Scene controls retain Idol Manager's gradient, shadow, fonts and hover colors.
    // Generic MUIP resource buttons have their own label/style managers.
    internal static class StaffPortraitUi
    {
        private const string SettingsButtonsPath = "ScrollRect/Container";
        private static readonly string[] NativeButtonNames = { "Settings", "Main Menu" };
        private const string MissingButtonMessage = "[StaffPortraits] Native Settings button template is unavailable.";
        private const float MinimumFontSize = 12f;
        private const float ButtonFontSize = 17f;
        private const float ButtonTextInset = 8f;
        private const float VisibleAlpha = 1f;

        internal static GameObject Object(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            result.layer = parent.gameObject.layer;
            return result;
        }

        internal static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        internal static TextMeshProUGUI Label(Transform parent, string name, string text,
            float left, float top, float width, float height, float fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI label = IMUiPrimitives.CreateText(parent, new IMUiTextOptions
            {
                ObjectName = name, Text = text, FontSize = fontSize,
                Alignment = alignment, WordWrap = true, RaycastTarget = false
            });
            label.color = mainScript.black32;
            label.enableAutoSizing = true;
            label.fontSizeMin = MinimumFontSize;
            label.fontSizeMax = fontSize;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Place(label.rectTransform, left, top, width, height);
            return label;
        }

        internal static Button Button(Transform parent, string name, string label,
            float left, float top, float width, float height, UnityAction callback)
        {
            GameObject template = FindNativeButton();
            if (template == null) throw new InvalidOperationException(MissingButtonMessage);
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = name;
            clone.SetActive(true);
            IMUiKit.ApplyLayerRecursively(clone, parent.gameObject.layer);
            IMUiKit.RebindAllButtons(clone, callback);
            IMUiKit.ActivateButtonDefaults(clone);
            foreach (CanvasGroup group in clone.GetComponentsInChildren<CanvasGroup>(true))
            {
                group.alpha = VisibleAlpha;
                group.interactable = group.blocksRaycasts = true;
            }
            foreach (ButtonDefault native in clone.GetComponentsInChildren<ButtonDefault>(true))
            {
                native.OnHover = new ButtonDefault.MyEventType();
                native.DefaultTooltip = string.Empty;
                native.SetTooltip(string.Empty);
            }
            foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic.gameObject == clone;
            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            Place(rect, left, top, width, height);
            Button button = clone.GetComponent<Button>();
            SetButtonText(button, label);
            return button;
        }

        private static GameObject FindNativeButton()
        {
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            Tabs_Manager tabs = main == null || main.Data == null ? null : main.Data.GetComponent<Tabs_Manager>();
            Tabs_Manager._tab settings = tabs == null ? null : tabs.GetTab(Tabs_Manager._tab._type.settings);
            if (settings == null || settings.Tab == null) return null;
            Transform root = settings.Tab.transform;
            Transform container = root.Find(SettingsButtonsPath);
            return FindNativeButton(container != null ? container : root) ?? FindNativeButton(root);
        }

        private static GameObject FindNativeButton(Transform root)
        {
            foreach (string name in NativeButtonNames)
                foreach (Button candidate in root.GetComponentsInChildren<Button>(true))
                    if (candidate.GetComponent<ButtonDefault>() != null
                        && candidate.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0
                        && candidate.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true).Length > 0)
                        return candidate.gameObject;
            return null;
        }

        internal static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            // Reset every inherited language binding; Lang_Button.Start otherwise restores
            // the Settings caption after our translated label has been assigned.
            foreach (Lang_Button binding in button.gameObject.GetComponentsInChildren<Lang_Button>(true))
            {
                binding.Tooltip = string.Empty;
                binding.Constant = binding.GetComponent<TextMeshProUGUI>() != null ? value : string.Empty;
            }
            VanillaUiFonts.ApplyGameFont(button.gameObject);
            foreach (TextMeshProUGUI label in button.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.gameObject.SetActive(true);
                label.text = value;
                label.color = mainScript.white32;
                label.alignment = TextAlignmentOptions.Center;
                label.enableWordWrapping = true;
                label.enableAutoSizing = true;
                label.fontSizeMin = MinimumFontSize;
                label.fontSizeMax = ButtonFontSize;
                label.raycastTarget = false;
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(ButtonTextInset, 0f);
                label.rectTransform.offsetMax = new Vector2(-ButtonTextInset, 0f);
            }
        }

        internal static void SetInteractable(Button button, bool enabled)
        {
            if (button == null) return;
            button.interactable = enabled;
            ButtonDefault native = button.GetComponent<ButtonDefault>();
            if (native != null) native.Activate(enabled, false);
        }
    }
}
