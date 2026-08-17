using System;
using TMPro;
using UnityEngine;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    /// <summary>
    /// Friendly categories for every Modern UI Pack control family shipped in Idol Manager's Resources.
    /// Exact variants remain available through VanillaUiPrefabCatalog and the ResourcePath override.
    /// </summary>
    public enum VanillaControlType
    {
        AnimatedIcon,
        Button,
        ButtonIcon,
        ButtonWithIcon,
        ButtonOutline,
        ButtonOutlineIcon,
        ButtonOutlineWithIcon,
        ContextMenu,
        ContextMenuButton,
        Dropdown,
        DropdownItem,
        MultiSelectDropdown,
        MultiSelectDropdownItem,
        HorizontalSelector,
        HorizontalSelectorIndicator,
        InputField,
        ListView,
        ModalWindow,
        MovableWindow,
        Notification,
        Canvas,
        ProgressBar,
        LoopProgressBar,
        Scrollbar,
        Slider,
        RangeSlider,
        RadialSlider,
        Switch,
        Toggle,
        ToggleGroupPanel,
        Tooltip,
        WindowManager
    }

    public sealed class VanillaControlOptions
    {
        public VanillaControlType Type = VanillaControlType.Button;
        public string ResourcePath;
        public string ObjectName;
        public bool Active = true;
        public bool ApplyGameFont = true;
        public bool ApplyVanillaTheme = true;
        public IMUiTheme SemanticTheme;
        public IMUiThemeApplication SemanticThemeApplication = IMUiThemeApplication.Interactive;
        public Action<VanillaUiThemeSettings> ConfigureTheme;
        public Action<GameObject> Configure;
    }

    /// <summary>
    /// One-entry-point factory for the complete runtime Resources UI set. It complements the typed
    /// VanillaUiResources methods: mod code can ask for a control family or pass any of the 218 exact
    /// VanillaUiPrefabCatalog resource paths without writing prefab lookup/theme/font boilerplate.
    /// </summary>
    public static class VanillaUiControlFactory
    {
        public static bool TryCreate(
            VanillaControlType type,
            Transform parent,
            string objectName,
            out GameObject instance)
        {
            VanillaControlOptions options = new VanillaControlOptions();
            options.Type = type;
            options.ObjectName = objectName;
            return TryCreate(parent, options, out instance);
        }

        public static bool TryCreate(
            Transform parent,
            VanillaControlOptions options,
            out GameObject instance)
        {
            instance = null;
            if (parent == null || options == null)
            {
                return false;
            }

            string path = string.IsNullOrEmpty(options.ResourcePath)
                ? GetDefaultResourcePath(options.Type)
                : options.ResourcePath;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            VanillaUiThemeSettings settings = null;
            if (options.SemanticTheme != null)
            {
                settings = IMUiMuipThemeBridge.CreateSettings(options.SemanticTheme);
            }
            else if (options.ApplyVanillaTheme)
            {
                settings = VanillaUiThemeSettings.FromVanilla();
            }
            if (settings != null && options.ConfigureTheme != null)
            {
                options.ConfigureTheme(settings);
            }

            instance = settings == null
                ? VanillaUiResources.InstantiatePrefab(path, parent, options.ObjectName, false, (Michsky.UI.ModernUIPack.UIManager)null)
                : VanillaUiResources.InstantiatePrefab(path, parent, options.ObjectName, false, settings);
            if (instance == null)
            {
                return false;
            }

            if (options.ApplyGameFont)
            {
                VanillaUiFonts.ApplyGameFont(instance, true);
            }
            if (options.SemanticTheme != null)
            {
                IMUiStyle.ApplyTheme(instance, options.SemanticTheme, options.SemanticThemeApplication, true);
            }
            if (options.Configure != null)
            {
                options.Configure(instance);
            }
            instance.SetActive(options.Active);
            return true;
        }

        public static bool TryCreateResource(
            string resourcePath,
            Transform parent,
            string objectName,
            bool active,
            out GameObject instance)
        {
            VanillaControlOptions options = new VanillaControlOptions();
            options.ResourcePath = resourcePath;
            options.ObjectName = objectName;
            options.Active = active;
            return TryCreate(parent, options, out instance);
        }

        public static string GetDefaultResourcePath(VanillaControlType type)
        {
            switch (type)
            {
                case VanillaControlType.AnimatedIcon:
                    return VanillaUiPrefabCatalog.AnimatedIcon.Load;
                case VanillaControlType.Button:
                    return VanillaUiPrefabCatalog.Button.basic_Standard;
                case VanillaControlType.ButtonIcon:
                    return VanillaUiPrefabCatalog.Button.basic_only_icon_Standard;
                case VanillaControlType.ButtonWithIcon:
                    return VanillaUiPrefabCatalog.Button.basic_with_icon_Standard;
                case VanillaControlType.ButtonOutline:
                    return VanillaUiPrefabCatalog.Button.basic_outline_Standard;
                case VanillaControlType.ButtonOutlineIcon:
                    return VanillaUiPrefabCatalog.Button.basic_outline_only_icon_Standard;
                case VanillaControlType.ButtonOutlineWithIcon:
                    return VanillaUiPrefabCatalog.Button.basic_outline_with_icon_Standard;
                case VanillaControlType.ContextMenu:
                    return VanillaUiPrefabCatalog.ContextMenu.Context_Menu;
                case VanillaControlType.ContextMenuButton:
                    return VanillaUiPrefabCatalog.ContextMenu.Context_Menu_Button;
                case VanillaControlType.Dropdown:
                    return VanillaUiPrefabCatalog.Dropdown.Standard;
                case VanillaControlType.DropdownItem:
                    return VanillaUiPrefabCatalog.Dropdown.Dropdown_Item;
                case VanillaControlType.MultiSelectDropdown:
                    return VanillaUiPrefabCatalog.Dropdown.Dropdown_Multi_Select;
                case VanillaControlType.MultiSelectDropdownItem:
                    return VanillaUiPrefabCatalog.Dropdown.Multi_Select_Dropdown_Item;
                case VanillaControlType.HorizontalSelector:
                    return VanillaUiPrefabCatalog.HorizontalSelector.Horizontal_Selector;
                case VanillaControlType.HorizontalSelectorIndicator:
                    return VanillaUiPrefabCatalog.HorizontalSelector.Indicator_Item;
                case VanillaControlType.InputField:
                    return VanillaUiPrefabCatalog.InputField.Input_Field_Standard_Middle;
                case VanillaControlType.ListView:
                    return VanillaUiPrefabCatalog.ListView.List_View;
                case VanillaControlType.ModalWindow:
                    return VanillaUiPrefabCatalog.ModalWindow.Style_1;
                case VanillaControlType.MovableWindow:
                    return VanillaUiPrefabCatalog.MovableWindow.Movable_Window;
                case VanillaControlType.Notification:
                    return VanillaUiPrefabCatalog.Notification.Popup_Notification;
                case VanillaControlType.Canvas:
                    return VanillaUiPrefabCatalog.Other.Canvas;
                case VanillaControlType.ProgressBar:
                    return VanillaUiPrefabCatalog.ProgressBar.PB_Standard;
                case VanillaControlType.LoopProgressBar:
                    return VanillaUiPrefabCatalog.ProgressBarLoop.PB_Loop_Standard_Run;
                case VanillaControlType.Scrollbar:
                    return VanillaUiPrefabCatalog.Scrollbar.Standard;
                case VanillaControlType.Slider:
                    return VanillaUiPrefabCatalog.Slider.standard_Slider_Standard;
                case VanillaControlType.RangeSlider:
                    return VanillaUiPrefabCatalog.Slider.range_Slider_Range;
                case VanillaControlType.RadialSlider:
                    return VanillaUiPrefabCatalog.Slider.radial_Slider_Radial;
                case VanillaControlType.Switch:
                    return VanillaUiPrefabCatalog.Switch.Switch_Standard;
                case VanillaControlType.Toggle:
                    return VanillaUiPrefabCatalog.Toggle.Toggle_Standard;
                case VanillaControlType.ToggleGroupPanel:
                    return VanillaUiPrefabCatalog.Toggle.Toggle_Group_Panel;
                case VanillaControlType.Tooltip:
                    return VanillaUiPrefabCatalog.Tooltip.Standard;
                case VanillaControlType.WindowManager:
                    return VanillaUiPrefabCatalog.WindowManager.Window_Manager;
                default:
                    return string.Empty;
            }
        }
    }
}
