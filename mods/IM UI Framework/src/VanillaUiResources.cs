using System;
using System.Collections.Generic;
using System.Reflection;
using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.UI;
using ModernNotificationManager = Michsky.UI.ModernUIPack.NotificationManager;
using ModernProgressBar = Michsky.UI.ModernUIPack.ProgressBar;
using ModernTooltipManager = Michsky.UI.ModernUIPack.TooltipManager;

namespace IMUiFramework
{
    /// <summary>
    /// Complete catalog of runtime-loadable UI prefabs recovered from Idol Manager's IM_Data Resources assets.
    /// Paths are Resources.Load paths: no Resources/ prefix and no .prefab extension.
    /// </summary>
    public static class VanillaUiPrefabCatalog
    {
        public static class AnimatedIcon
        {
            public const string Hamburger_Menu = "animated icon/Hamburger Menu";
            public const string Heart_Pop = "animated icon/Heart Pop";
            public const string Load = "animated icon/Load";
            public const string Lock = "animated icon/Lock";
            public const string Message_Bubbles = "animated icon/Message Bubbles";
            public const string No_to_Yes = "animated icon/No to Yes";
            public const string Notification_Bell = "animated icon/Notification Bell";
            public const string Sand_Clock = "animated icon/Sand Clock";
            public const string Slider = "animated icon/Slider";
            public const string Window = "animated icon/Window";
            public const string Yes_to_No = "animated icon/Yes to No";
        }

        public static class Button
        {
            public const string basic_gradient_Blue = "button/basic - gradient/Blue";
            public const string basic_gradient_Brown = "button/basic - gradient/Brown";
            public const string basic_gradient_Gray = "button/basic - gradient/Gray";
            public const string basic_gradient_Green = "button/basic - gradient/Green";
            public const string basic_gradient_Night = "button/basic - gradient/Night";
            public const string basic_gradient_Orange = "button/basic - gradient/Orange";
            public const string basic_gradient_Pink = "button/basic - gradient/Pink";
            public const string basic_gradient_Purple = "button/basic - gradient/Purple";
            public const string basic_gradient_Red = "button/basic - gradient/Red";
            public const string basic_gradient_White = "button/basic - gradient/White";
            public const string basic_only_icon_Blue = "button/basic - only icon/Blue";
            public const string basic_only_icon_Brown = "button/basic - only icon/Brown";
            public const string basic_only_icon_Gray = "button/basic - only icon/Gray";
            public const string basic_only_icon_Green = "button/basic - only icon/Green";
            public const string basic_only_icon_Night = "button/basic - only icon/Night";
            public const string basic_only_icon_Orange = "button/basic - only icon/Orange";
            public const string basic_only_icon_Pink = "button/basic - only icon/Pink";
            public const string basic_only_icon_Purple = "button/basic - only icon/Purple";
            public const string basic_only_icon_Red = "button/basic - only icon/Red";
            public const string basic_only_icon_Standard = "button/basic - only icon/Standard";
            public const string basic_only_icon_White = "button/basic - only icon/White";
            public const string basic_outline_gradient_Blue = "button/basic - outline gradient/Blue";
            public const string basic_outline_gradient_Brown = "button/basic - outline gradient/Brown";
            public const string basic_outline_gradient_Gray = "button/basic - outline gradient/Gray";
            public const string basic_outline_gradient_Green = "button/basic - outline gradient/Green";
            public const string basic_outline_gradient_Night = "button/basic - outline gradient/Night";
            public const string basic_outline_gradient_Orange = "button/basic - outline gradient/Orange";
            public const string basic_outline_gradient_Pink = "button/basic - outline gradient/Pink";
            public const string basic_outline_gradient_Purple = "button/basic - outline gradient/Purple";
            public const string basic_outline_gradient_Red = "button/basic - outline gradient/Red";
            public const string basic_outline_gradient_White = "button/basic - outline gradient/White";
            public const string basic_outline_only_icon_Blue = "button/basic - outline only icon/Blue";
            public const string basic_outline_only_icon_Brown = "button/basic - outline only icon/Brown";
            public const string basic_outline_only_icon_Gray = "button/basic - outline only icon/Gray";
            public const string basic_outline_only_icon_Green = "button/basic - outline only icon/Green";
            public const string basic_outline_only_icon_Night = "button/basic - outline only icon/Night";
            public const string basic_outline_only_icon_Orange = "button/basic - outline only icon/Orange";
            public const string basic_outline_only_icon_Pink = "button/basic - outline only icon/Pink";
            public const string basic_outline_only_icon_Purple = "button/basic - outline only icon/Purple";
            public const string basic_outline_only_icon_Red = "button/basic - outline only icon/Red";
            public const string basic_outline_only_icon_Standard = "button/basic - outline only icon/Standard";
            public const string basic_outline_only_icon_White = "button/basic - outline only icon/White";
            public const string basic_outline_with_icon_Blue = "button/basic - outline with icon/Blue";
            public const string basic_outline_with_icon_Brown = "button/basic - outline with icon/Brown";
            public const string basic_outline_with_icon_Gray = "button/basic - outline with icon/Gray";
            public const string basic_outline_with_icon_Green = "button/basic - outline with icon/Green";
            public const string basic_outline_with_icon_Night = "button/basic - outline with icon/Night";
            public const string basic_outline_with_icon_Orange = "button/basic - outline with icon/Orange";
            public const string basic_outline_with_icon_Pink = "button/basic - outline with icon/Pink";
            public const string basic_outline_with_icon_Purple = "button/basic - outline with icon/Purple";
            public const string basic_outline_with_icon_Red = "button/basic - outline with icon/Red";
            public const string basic_outline_with_icon_Standard = "button/basic - outline with icon/Standard";
            public const string basic_outline_with_icon_White = "button/basic - outline with icon/White";
            public const string basic_outline_Blue = "button/basic - outline/Blue";
            public const string basic_outline_Brown = "button/basic - outline/Brown";
            public const string basic_outline_Gray = "button/basic - outline/Gray";
            public const string basic_outline_Green = "button/basic - outline/Green";
            public const string basic_outline_Night = "button/basic - outline/Night";
            public const string basic_outline_Orange = "button/basic - outline/Orange";
            public const string basic_outline_Pink = "button/basic - outline/Pink";
            public const string basic_outline_Purple = "button/basic - outline/Purple";
            public const string basic_outline_Red = "button/basic - outline/Red";
            public const string basic_outline_Standard = "button/basic - outline/Standard";
            public const string basic_outline_White = "button/basic - outline/White";
            public const string basic_with_icon_Blue = "button/basic - with icon/Blue";
            public const string basic_with_icon_Brown = "button/basic - with icon/Brown";
            public const string basic_with_icon_Gray = "button/basic - with icon/Gray";
            public const string basic_with_icon_Green = "button/basic - with icon/Green";
            public const string basic_with_icon_Night = "button/basic - with icon/Night";
            public const string basic_with_icon_Orange = "button/basic - with icon/Orange";
            public const string basic_with_icon_Pink = "button/basic - with icon/Pink";
            public const string basic_with_icon_Purple = "button/basic - with icon/Purple";
            public const string basic_with_icon_Red = "button/basic - with icon/Red";
            public const string basic_with_icon_Standard = "button/basic - with icon/Standard";
            public const string basic_with_icon_White = "button/basic - with icon/White";
            public const string basic_Blue = "button/basic/Blue";
            public const string basic_Brown = "button/basic/Brown";
            public const string basic_Gray = "button/basic/Gray";
            public const string basic_Green = "button/basic/Green";
            public const string basic_Night = "button/basic/Night";
            public const string basic_Orange = "button/basic/Orange";
            public const string basic_Pink = "button/basic/Pink";
            public const string basic_Purple = "button/basic/Purple";
            public const string basic_Red = "button/basic/Red";
            public const string basic_Standard = "button/basic/Standard";
            public const string basic_White = "button/basic/White";
            public const string radial_only_icon_Blue = "button/radial - only icon/Blue";
            public const string radial_only_icon_Brown = "button/radial - only icon/Brown";
            public const string radial_only_icon_Gray = "button/radial - only icon/Gray";
            public const string radial_only_icon_Green = "button/radial - only icon/Green";
            public const string radial_only_icon_Night = "button/radial - only icon/Night";
            public const string radial_only_icon_Orange = "button/radial - only icon/Orange";
            public const string radial_only_icon_Pink = "button/radial - only icon/Pink";
            public const string radial_only_icon_Purple = "button/radial - only icon/Purple";
            public const string radial_only_icon_Red = "button/radial - only icon/Red";
            public const string radial_only_icon_Standard = "button/radial - only icon/Standard";
            public const string radial_only_icon_White = "button/radial - only icon/White";
            public const string radial_outline_only_icon_Blue = "button/radial - outline only icon/Blue";
            public const string radial_outline_only_icon_Brown = "button/radial - outline only icon/Brown";
            public const string radial_outline_only_icon_Gray = "button/radial - outline only icon/Gray";
            public const string radial_outline_only_icon_Green = "button/radial - outline only icon/Green";
            public const string radial_outline_only_icon_Night = "button/radial - outline only icon/Night";
            public const string radial_outline_only_icon_Orange = "button/radial - outline only icon/Orange";
            public const string radial_outline_only_icon_Pink = "button/radial - outline only icon/Pink";
            public const string radial_outline_only_icon_Purple = "button/radial - outline only icon/Purple";
            public const string radial_outline_only_icon_Red = "button/radial - outline only icon/Red";
            public const string radial_outline_only_icon_Standard = "button/radial - outline only icon/Standard";
            public const string radial_outline_only_icon_White = "button/radial - outline only icon/White";
            public const string rounded_gradient_Blue = "button/rounded - gradient/Blue";
            public const string rounded_gradient_Brown = "button/rounded - gradient/Brown";
            public const string rounded_gradient_Gray = "button/rounded - gradient/Gray";
            public const string rounded_gradient_Green = "button/rounded - gradient/Green";
            public const string rounded_gradient_Night = "button/rounded - gradient/Night";
            public const string rounded_gradient_Orange = "button/rounded - gradient/Orange";
            public const string rounded_gradient_Pink = "button/rounded - gradient/Pink";
            public const string rounded_gradient_Purple = "button/rounded - gradient/Purple";
            public const string rounded_gradient_Red = "button/rounded - gradient/Red";
            public const string rounded_gradient_White = "button/rounded - gradient/White";
            public const string rounded_outline_gradient_Blue = "button/rounded - outline gradient/Blue";
            public const string rounded_outline_gradient_Brown = "button/rounded - outline gradient/Brown";
            public const string rounded_outline_gradient_Gray = "button/rounded - outline gradient/Gray";
            public const string rounded_outline_gradient_Green = "button/rounded - outline gradient/Green";
            public const string rounded_outline_gradient_Night = "button/rounded - outline gradient/Night";
            public const string rounded_outline_gradient_Orange = "button/rounded - outline gradient/Orange";
            public const string rounded_outline_gradient_Pink = "button/rounded - outline gradient/Pink";
            public const string rounded_outline_gradient_Purple = "button/rounded - outline gradient/Purple";
            public const string rounded_outline_gradient_Red = "button/rounded - outline gradient/Red";
            public const string rounded_outline_gradient_White = "button/rounded - outline gradient/White";
            public const string rounded_outline_Blue = "button/rounded - outline/Blue";
            public const string rounded_outline_Brown = "button/rounded - outline/Brown";
            public const string rounded_outline_Gray = "button/rounded - outline/Gray";
            public const string rounded_outline_Green = "button/rounded - outline/Green";
            public const string rounded_outline_Night = "button/rounded - outline/Night";
            public const string rounded_outline_Orange = "button/rounded - outline/Orange";
            public const string rounded_outline_Pink = "button/rounded - outline/Pink";
            public const string rounded_outline_Purple = "button/rounded - outline/Purple";
            public const string rounded_outline_Red = "button/rounded - outline/Red";
            public const string rounded_outline_Standard = "button/rounded - outline/Standard";
            public const string rounded_outline_White = "button/rounded - outline/White";
            public const string rounded_Blue = "button/rounded/Blue";
            public const string rounded_Brown = "button/rounded/Brown";
            public const string rounded_Gray = "button/rounded/Gray";
            public const string rounded_Green = "button/rounded/Green";
            public const string rounded_Night = "button/rounded/Night";
            public const string rounded_Orange = "button/rounded/Orange";
            public const string rounded_Pink = "button/rounded/Pink";
            public const string rounded_Purple = "button/rounded/Purple";
            public const string rounded_Red = "button/rounded/Red";
            public const string rounded_Standard = "button/rounded/Standard";
            public const string rounded_White = "button/rounded/White";
        }

        public static class ContextMenu
        {
            public const string Context_Menu = "context menu/Context Menu";
            public const string Context_Menu_Button = "context menu/Context Menu Button";
        }

        public static class Dropdown
        {
            public const string Standard = "dropdown/Dropdown";
            public const string Dropdown_Multi_Select = "dropdown/Dropdown - Multi Select";
            public const string Dropdown_Item = "dropdown/Dropdown Item";
            public const string Multi_Select_Dropdown_Item = "dropdown/Multi Select Dropdown Item";
        }

        public static class HorizontalSelector
        {
            public const string Horizontal_Selector = "horizontal selector/Horizontal Selector";
            public const string Indicator_Item = "horizontal selector/Indicator Item";
        }

        public static class InputField
        {
            public const string Input_Field_Fading_Left = "input field/Input Field - Fading (Left)";
            public const string Input_Field_Fading_Middle = "input field/Input Field - Fading (Middle)";
            public const string Input_Field_Fading_Right = "input field/Input Field - Fading (Right)";
            public const string Input_Field_Multi_Line = "input field/Input Field - Multi-Line";
            public const string Input_Field_Standard_Left = "input field/Input Field - Standard (Left)";
            public const string Input_Field_Standard_Middle = "input field/Input Field - Standard (Middle)";
            public const string Input_Field_Standard_Right = "input field/Input Field - Standard (Right)";
        }

        public static class ListView
        {
            public const string List_View = "list view/List View";
        }

        public static class ModalWindow
        {
            public const string Style_1 = "modal window/Style 1";
            public const string Style_2 = "modal window/Style 2";
        }

        public static class MovableWindow
        {
            public const string Movable_Window = "movable window/Movable Window";
        }

        public static class Notification
        {
            public const string Fading_Notification = "notification/Fading Notification";
            public const string Popup_Notification = "notification/Popup Notification";
            public const string Sliding_Notification = "notification/Sliding Notification";
        }

        public static class Other
        {
            public const string Canvas = "other/Canvas";
        }

        public static class ProgressBar
        {
            public const string PB_Radial_Bold = "progress bar/PB - Radial (Bold)";
            public const string PB_Radial_Light = "progress bar/PB - Radial (Light)";
            public const string PB_Radial_Regular = "progress bar/PB - Radial (Regular)";
            public const string PB_Radial_Thin = "progress bar/PB - Radial (Thin)";
            public const string PB_Radial_Filled_Horizontal = "progress bar/PB - Radial Filled Horizontal";
            public const string PB_Radial_Filled_Vertical = "progress bar/PB - Radial Filled Vertical";
            public const string PB_Standard = "progress bar/PB - Standard";
        }

        public static class ProgressBarLoop
        {
            public const string PB_Loop_Radial_Material = "progress bar (loop)/PB Loop - Radial Material";
            public const string PB_Loop_Radial_Pie = "progress bar (loop)/PB Loop - Radial Pie";
            public const string PB_Loop_Radial_Run = "progress bar (loop)/PB Loop - Radial Run";
            public const string PB_Loop_Radial_Trapez = "progress bar (loop)/PB Loop - Radial Trapez";
            public const string PB_Loop_Standard_Fastly = "progress bar (loop)/PB Loop - Standard Fastly";
            public const string PB_Loop_Standard_Run = "progress bar (loop)/PB Loop - Standard Run";
        }

        public static class Scrollbar
        {
            public const string Standard = "scrollbar/Scrollbar";
        }

        public static class Slider
        {
            public const string gradient_Slider_Gradient = "slider/gradient/Slider - Gradient";
            public const string gradient_Slider_Gradient_Popup = "slider/gradient/Slider - Gradient (Popup)";
            public const string gradient_Slider_Gradient_Value = "slider/gradient/Slider - Gradient (Value)";
            public const string outline_Slider_Outline = "slider/outline/Slider - Outline";
            public const string outline_Slider_Outline_Popup = "slider/outline/Slider - Outline (Popup)";
            public const string outline_Slider_Outline_Value = "slider/outline/Slider - Outline (Value)";
            public const string radial_Slider_Radial = "slider/radial/Slider - Radial";
            public const string radial_Slider_Radial_Gradient = "slider/radial/Slider - Radial (Gradient)";
            public const string range_Slider_Range = "slider/range/Slider - Range";
            public const string standard_Slider_Standard = "slider/standard/Slider - Standard";
            public const string standard_Slider_Standard_Popup = "slider/standard/Slider - Standard (Popup)";
            public const string standard_Slider_Standard_Value = "slider/standard/Slider - Standard (Value)";
        }

        public static class Switch
        {
            public const string Switch_Standard = "switch/Switch - Standard";
        }

        public static class Toggle
        {
            public const string Toggle_Standard = "toggle/Toggle - Standard";
            public const string Toggle_Standard_Bold = "toggle/Toggle - Standard (Bold)";
            public const string Toggle_Standard_Light = "toggle/Toggle - Standard (Light)";
            public const string Toggle_Standard_Regular = "toggle/Toggle - Standard (Regular)";
            public const string Toggle_Group_Panel = "toggle/Toggle Group Panel";
        }

        public static class Tooltip
        {
            public const string Standard = "tooltip/Tooltip";
        }

        public static class WindowManager
        {
            public const string Window_Manager = "window manager/Window Manager";
        }

        private static readonly string[] allPrefabPaths = new string[]
        {
            "animated icon/Hamburger Menu",
            "animated icon/Heart Pop",
            "animated icon/Load",
            "animated icon/Lock",
            "animated icon/Message Bubbles",
            "animated icon/No to Yes",
            "animated icon/Notification Bell",
            "animated icon/Sand Clock",
            "animated icon/Slider",
            "animated icon/Window",
            "animated icon/Yes to No",
            "button/basic - gradient/Blue",
            "button/basic - gradient/Brown",
            "button/basic - gradient/Gray",
            "button/basic - gradient/Green",
            "button/basic - gradient/Night",
            "button/basic - gradient/Orange",
            "button/basic - gradient/Pink",
            "button/basic - gradient/Purple",
            "button/basic - gradient/Red",
            "button/basic - gradient/White",
            "button/basic - only icon/Blue",
            "button/basic - only icon/Brown",
            "button/basic - only icon/Gray",
            "button/basic - only icon/Green",
            "button/basic - only icon/Night",
            "button/basic - only icon/Orange",
            "button/basic - only icon/Pink",
            "button/basic - only icon/Purple",
            "button/basic - only icon/Red",
            "button/basic - only icon/Standard",
            "button/basic - only icon/White",
            "button/basic - outline gradient/Blue",
            "button/basic - outline gradient/Brown",
            "button/basic - outline gradient/Gray",
            "button/basic - outline gradient/Green",
            "button/basic - outline gradient/Night",
            "button/basic - outline gradient/Orange",
            "button/basic - outline gradient/Pink",
            "button/basic - outline gradient/Purple",
            "button/basic - outline gradient/Red",
            "button/basic - outline gradient/White",
            "button/basic - outline only icon/Blue",
            "button/basic - outline only icon/Brown",
            "button/basic - outline only icon/Gray",
            "button/basic - outline only icon/Green",
            "button/basic - outline only icon/Night",
            "button/basic - outline only icon/Orange",
            "button/basic - outline only icon/Pink",
            "button/basic - outline only icon/Purple",
            "button/basic - outline only icon/Red",
            "button/basic - outline only icon/Standard",
            "button/basic - outline only icon/White",
            "button/basic - outline with icon/Blue",
            "button/basic - outline with icon/Brown",
            "button/basic - outline with icon/Gray",
            "button/basic - outline with icon/Green",
            "button/basic - outline with icon/Night",
            "button/basic - outline with icon/Orange",
            "button/basic - outline with icon/Pink",
            "button/basic - outline with icon/Purple",
            "button/basic - outline with icon/Red",
            "button/basic - outline with icon/Standard",
            "button/basic - outline with icon/White",
            "button/basic - outline/Blue",
            "button/basic - outline/Brown",
            "button/basic - outline/Gray",
            "button/basic - outline/Green",
            "button/basic - outline/Night",
            "button/basic - outline/Orange",
            "button/basic - outline/Pink",
            "button/basic - outline/Purple",
            "button/basic - outline/Red",
            "button/basic - outline/Standard",
            "button/basic - outline/White",
            "button/basic - with icon/Blue",
            "button/basic - with icon/Brown",
            "button/basic - with icon/Gray",
            "button/basic - with icon/Green",
            "button/basic - with icon/Night",
            "button/basic - with icon/Orange",
            "button/basic - with icon/Pink",
            "button/basic - with icon/Purple",
            "button/basic - with icon/Red",
            "button/basic - with icon/Standard",
            "button/basic - with icon/White",
            "button/basic/Blue",
            "button/basic/Brown",
            "button/basic/Gray",
            "button/basic/Green",
            "button/basic/Night",
            "button/basic/Orange",
            "button/basic/Pink",
            "button/basic/Purple",
            "button/basic/Red",
            "button/basic/Standard",
            "button/basic/White",
            "button/radial - only icon/Blue",
            "button/radial - only icon/Brown",
            "button/radial - only icon/Gray",
            "button/radial - only icon/Green",
            "button/radial - only icon/Night",
            "button/radial - only icon/Orange",
            "button/radial - only icon/Pink",
            "button/radial - only icon/Purple",
            "button/radial - only icon/Red",
            "button/radial - only icon/Standard",
            "button/radial - only icon/White",
            "button/radial - outline only icon/Blue",
            "button/radial - outline only icon/Brown",
            "button/radial - outline only icon/Gray",
            "button/radial - outline only icon/Green",
            "button/radial - outline only icon/Night",
            "button/radial - outline only icon/Orange",
            "button/radial - outline only icon/Pink",
            "button/radial - outline only icon/Purple",
            "button/radial - outline only icon/Red",
            "button/radial - outline only icon/Standard",
            "button/radial - outline only icon/White",
            "button/rounded - gradient/Blue",
            "button/rounded - gradient/Brown",
            "button/rounded - gradient/Gray",
            "button/rounded - gradient/Green",
            "button/rounded - gradient/Night",
            "button/rounded - gradient/Orange",
            "button/rounded - gradient/Pink",
            "button/rounded - gradient/Purple",
            "button/rounded - gradient/Red",
            "button/rounded - gradient/White",
            "button/rounded - outline gradient/Blue",
            "button/rounded - outline gradient/Brown",
            "button/rounded - outline gradient/Gray",
            "button/rounded - outline gradient/Green",
            "button/rounded - outline gradient/Night",
            "button/rounded - outline gradient/Orange",
            "button/rounded - outline gradient/Pink",
            "button/rounded - outline gradient/Purple",
            "button/rounded - outline gradient/Red",
            "button/rounded - outline gradient/White",
            "button/rounded - outline/Blue",
            "button/rounded - outline/Brown",
            "button/rounded - outline/Gray",
            "button/rounded - outline/Green",
            "button/rounded - outline/Night",
            "button/rounded - outline/Orange",
            "button/rounded - outline/Pink",
            "button/rounded - outline/Purple",
            "button/rounded - outline/Red",
            "button/rounded - outline/Standard",
            "button/rounded - outline/White",
            "button/rounded/Blue",
            "button/rounded/Brown",
            "button/rounded/Gray",
            "button/rounded/Green",
            "button/rounded/Night",
            "button/rounded/Orange",
            "button/rounded/Pink",
            "button/rounded/Purple",
            "button/rounded/Red",
            "button/rounded/Standard",
            "button/rounded/White",
            "context menu/Context Menu",
            "context menu/Context Menu Button",
            "dropdown/Dropdown",
            "dropdown/Dropdown - Multi Select",
            "dropdown/Dropdown Item",
            "dropdown/Multi Select Dropdown Item",
            "horizontal selector/Horizontal Selector",
            "horizontal selector/Indicator Item",
            "input field/Input Field - Fading (Left)",
            "input field/Input Field - Fading (Middle)",
            "input field/Input Field - Fading (Right)",
            "input field/Input Field - Multi-Line",
            "input field/Input Field - Standard (Left)",
            "input field/Input Field - Standard (Middle)",
            "input field/Input Field - Standard (Right)",
            "list view/List View",
            "modal window/Style 1",
            "modal window/Style 2",
            "movable window/Movable Window",
            "notification/Fading Notification",
            "notification/Popup Notification",
            "notification/Sliding Notification",
            "other/Canvas",
            "progress bar (loop)/PB Loop - Radial Material",
            "progress bar (loop)/PB Loop - Radial Pie",
            "progress bar (loop)/PB Loop - Radial Run",
            "progress bar (loop)/PB Loop - Radial Trapez",
            "progress bar (loop)/PB Loop - Standard Fastly",
            "progress bar (loop)/PB Loop - Standard Run",
            "progress bar/PB - Radial (Bold)",
            "progress bar/PB - Radial (Light)",
            "progress bar/PB - Radial (Regular)",
            "progress bar/PB - Radial (Thin)",
            "progress bar/PB - Radial Filled Horizontal",
            "progress bar/PB - Radial Filled Vertical",
            "progress bar/PB - Standard",
            "scrollbar/Scrollbar",
            "slider/gradient/Slider - Gradient",
            "slider/gradient/Slider - Gradient (Popup)",
            "slider/gradient/Slider - Gradient (Value)",
            "slider/outline/Slider - Outline",
            "slider/outline/Slider - Outline (Popup)",
            "slider/outline/Slider - Outline (Value)",
            "slider/radial/Slider - Radial",
            "slider/radial/Slider - Radial (Gradient)",
            "slider/range/Slider - Range",
            "slider/standard/Slider - Standard",
            "slider/standard/Slider - Standard (Popup)",
            "slider/standard/Slider - Standard (Value)",
            "switch/Switch - Standard",
            "toggle/Toggle - Standard",
            "toggle/Toggle - Standard (Bold)",
            "toggle/Toggle - Standard (Light)",
            "toggle/Toggle - Standard (Regular)",
            "toggle/Toggle Group Panel",
            "tooltip/Tooltip",
            "window manager/Window Manager",
        };

        public static IList<string> AllPrefabPaths
        {
            get { return Array.AsReadOnly(allPrefabPaths); }
        }
    }

    public enum VanillaButtonStyle
    {
        Basic,
        BasicGradient,
        BasicOnlyIcon,
        BasicOutline,
        BasicOutlineGradient,
        BasicOutlineOnlyIcon,
        BasicOutlineWithIcon,
        BasicWithIcon,
        RadialOnlyIcon,
        RadialOutlineOnlyIcon,
        Rounded,
        RoundedGradient,
        RoundedOutline,
        RoundedOutlineGradient
    }

    public enum VanillaButtonPalette
    {
        Standard,
        Blue,
        Brown,
        Gray,
        Green,
        Night,
        Orange,
        Pink,
        Purple,
        Red,
        White
    }

    public sealed class VanillaUiPrefabDescriptor
    {
        public readonly string ResourcePath;
        public readonly string Category;
        public readonly string Name;

        public VanillaUiPrefabDescriptor(string resourcePath)
        {
            ResourcePath = resourcePath ?? string.Empty;
            int slash = ResourcePath.IndexOf('/');
            Category = slash >= 0 ? ResourcePath.Substring(0, slash) : string.Empty;
            Name = slash >= 0 ? ResourcePath.Substring(slash + 1) : ResourcePath;
        }

        public override string ToString() { return ResourcePath; }
    }

    /// <summary>
    /// Loads and instantiates Idol Manager's original Resources prefabs. This is the preferred source for vanilla UI.
    /// </summary>
    public static class VanillaUiResources
    {
        public const string MuipManagerPath = "MUIP Manager";
        public const string TmpSettingsPath = "TMP Settings";
        public const string LiberationSansSdfPath = "fonts & materials/LiberationSans SDF";

        private static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private static UIManager cachedMuipManager;

        public static UIManager GetMuipManager()
        {
            if (cachedMuipManager == null)
            {
                cachedMuipManager = Resources.Load<UIManager>(MuipManagerPath);
            }
            return cachedMuipManager;
        }

        public static bool TryGetMuipManager(out UIManager manager)
        {
            manager = GetMuipManager();
            return manager != null;
        }

        public static UIManager CloneMuipManager()
        {
            UIManager source = GetMuipManager();
            if (source == null) return null;
            UIManager clone = UnityEngine.Object.Instantiate(source);
            clone.name = "IMUiFramework Runtime MUIP Theme";
            return clone;
        }

        public static UIManager CreateMuipTheme(Action<UIManager> configure)
        {
            UIManager theme = CloneMuipManager();
            if (theme != null && configure != null) configure(theme);
            return theme;
        }

        public static GameObject LoadPrefab(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            resourcePath = NormalizeResourcePath(resourcePath);
            GameObject cached;
            if (prefabCache.TryGetValue(resourcePath, out cached) && cached != null) return cached;
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null) prefabCache[resourcePath] = prefab;
            return prefab;
        }

        public static bool TryLoadPrefab(string resourcePath, out GameObject prefab)
        {
            prefab = LoadPrefab(resourcePath);
            return prefab != null;
        }

        public static T Load<T>(string resourcePath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            return Resources.Load<T>(NormalizeResourcePath(resourcePath));
        }

        public static GameObject InstantiatePrefab(
            string resourcePath, Transform parent, string objectName = null, bool active = true, UIManager theme = null)
        {
            GameObject prefab = LoadPrefab(resourcePath);
            if (prefab == null) return null;
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            if (!string.IsNullOrEmpty(objectName)) instance.name = objectName;
            if (parent != null) IMUiKit.ApplyLayerRecursively(instance, parent.gameObject.layer);
            if (theme != null) ApplyTheme(instance, theme, true);
            instance.SetActive(active);
            return instance;
        }

        public static GameObject InstantiatePrefab(
            string resourcePath, Transform parent, string objectName, bool active, VanillaUiThemeSettings themeSettings)
        {
            UIManager runtimeTheme = null;
            if (themeSettings != null) runtimeTheme = themeSettings.CreateRuntimeAsset();
            GameObject instance = InstantiatePrefab(resourcePath, parent, objectName, active, runtimeTheme);
            if (instance == null)
            {
                if (runtimeTheme != null) UnityEngine.Object.Destroy(runtimeTheme);
                return null;
            }
            if (runtimeTheme != null)
            {
                VanillaUiThemeLifetime owner = instance.AddComponent<VanillaUiThemeLifetime>();
                owner.Theme = runtimeTheme;
            }
            return instance;
        }

        public static bool TryInstantiatePrefab<T>(
            string resourcePath, Transform parent, string objectName, out GameObject instance, out T component,
            Action<T> configure = null, Action<VanillaUiThemeSettings> configureTheme = null, bool active = true) where T : Component
        {
            component = null;
            VanillaUiThemeSettings settings = null;
            if (configureTheme != null)
            {
                settings = VanillaUiThemeSettings.FromVanilla();
                if (settings != null) configureTheme(settings);
            }
            instance = settings == null
                ? InstantiatePrefab(resourcePath, parent, objectName, active, (UIManager)null)
                : InstantiatePrefab(resourcePath, parent, objectName, active, settings);
            if (instance == null) return false;
            component = instance.GetComponent<T>();
            if (component == null) component = IMUiCompat.GetComponentInChildren<T>(instance);
            if (component == null)
            {
                UnityEngine.Object.Destroy(instance);
                instance = null;
                return false;
            }
            if (configure != null) configure(component);
            return true;
        }

        public static bool TryCreateScrollbar(
            Transform parent, string objectName, out GameObject instance, out Scrollbar scrollbar,
            Action<Scrollbar> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.Scrollbar.Standard, parent, objectName, out instance, out scrollbar, configure, configureTheme, true);
        }

        public static bool TryCreateModernButton(
            Transform parent, string objectName, out GameObject instance, out ButtonManager manager,
            string resourcePath = VanillaUiPrefabCatalog.Button.basic_outline_Standard,
            Action<ButtonManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out manager, configure, configureTheme, true);
        }

        public static bool TryCreateBasicButton(
            Transform parent, string objectName, out GameObject instance, out ButtonManagerBasic manager,
            string resourcePath = VanillaUiPrefabCatalog.Button.basic_Standard,
            Action<ButtonManagerBasic> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out manager, configure, configureTheme, true);
        }

        public static bool TryCreateBasicIconButton(
            Transform parent, string objectName, out GameObject instance, out ButtonManagerBasicIcon manager,
            string resourcePath = VanillaUiPrefabCatalog.Button.basic_only_icon_Standard,
            Action<ButtonManagerBasicIcon> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out manager, configure, configureTheme, true);
        }

        public static bool TryCreateBasicWithIconButton(
            Transform parent, string objectName, out GameObject instance, out ButtonManagerBasicWithIcon manager,
            string resourcePath = VanillaUiPrefabCatalog.Button.basic_with_icon_Standard,
            Action<ButtonManagerBasicWithIcon> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out manager, configure, configureTheme, true);
        }

        public static bool TryCreateOutlineIconButton(
            Transform parent, string objectName, out GameObject instance, out ButtonManagerIcon manager,
            string resourcePath = VanillaUiPrefabCatalog.Button.basic_outline_only_icon_Standard,
            Action<ButtonManagerIcon> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out manager, configure, configureTheme, true);
        }

        public static bool TryCreateOutlineWithIconButton(
            Transform parent, string objectName, out GameObject instance, out ButtonManagerWithIcon manager,
            string resourcePath = VanillaUiPrefabCatalog.Button.basic_outline_with_icon_Standard,
            Action<ButtonManagerWithIcon> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out manager, configure, configureTheme, true);
        }

        public static bool TryCreateAnimatedIcon(
            Transform parent, string objectName, out GameObject instance, out AnimatedIconHandler control,
            string resourcePath = VanillaUiPrefabCatalog.AnimatedIcon.Load,
            Action<AnimatedIconHandler> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateDropdown(
            Transform parent, string objectName, out GameObject instance, out CustomDropdown dropdown,
            string resourcePath = VanillaUiPrefabCatalog.Dropdown.Standard,
            Action<CustomDropdown> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out dropdown, configure, configureTheme, true);
        }

        public static bool TryCreateMultiSelectDropdown(
            Transform parent, string objectName, out GameObject instance, out DropdownMultiSelect dropdown,
            Action<DropdownMultiSelect> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.Dropdown.Dropdown_Multi_Select, parent, objectName, out instance, out dropdown, configure, configureTheme, true);
        }

        public static bool TryCreateInputField(
            Transform parent, string objectName, out GameObject instance, out CustomInputField inputField,
            string resourcePath = VanillaUiPrefabCatalog.InputField.Input_Field_Standard_Left,
            Action<CustomInputField> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out inputField, configure, configureTheme, true);
        }

        public static bool TryCreateSlider(
            Transform parent, string objectName, out GameObject instance, out SliderManager slider,
            string resourcePath = VanillaUiPrefabCatalog.Slider.standard_Slider_Standard,
            Action<SliderManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out slider, configure, configureTheme, true);
        }

        public static bool TryCreateRangeSlider(
            Transform parent, string objectName, out GameObject instance, out RangeSlider slider,
            Action<RangeSlider> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.Slider.range_Slider_Range, parent, objectName, out instance, out slider, configure, configureTheme, true);
        }

        public static bool TryCreateRadialSlider(
            Transform parent, string objectName, out GameObject instance, out RadialSlider slider,
            string resourcePath = VanillaUiPrefabCatalog.Slider.radial_Slider_Radial,
            Action<RadialSlider> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out slider, configure, configureTheme, true);
        }

        public static bool TryCreateSwitch(
            Transform parent, string objectName, out GameObject instance, out SwitchManager control,
            Action<SwitchManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.Switch.Switch_Standard, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateToggle(
            Transform parent, string objectName, out GameObject instance, out ToggleAnim control,
            string resourcePath = VanillaUiPrefabCatalog.Toggle.Toggle_Standard,
            Action<ToggleAnim> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateProgressBar(
            Transform parent, string objectName, out GameObject instance, out ModernProgressBar control,
            string resourcePath = VanillaUiPrefabCatalog.ProgressBar.PB_Standard,
            Action<ModernProgressBar> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateLoopProgressBar(
            Transform parent, string objectName, out GameObject instance, out UIManagerProgressBarLoop control,
            string resourcePath = VanillaUiPrefabCatalog.ProgressBarLoop.PB_Loop_Standard_Run,
            Action<UIManagerProgressBarLoop> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateFilledProgressBar(
            Transform parent, string objectName, out GameObject instance, out PBFilled control,
            string resourcePath = VanillaUiPrefabCatalog.ProgressBar.PB_Radial_Filled_Horizontal,
            Action<PBFilled> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateListView(
            Transform parent, string objectName, out GameObject instance, out ScrollRect scrollRect,
            Action<ScrollRect> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.ListView.List_View, parent, objectName, out instance, out scrollRect, configure, configureTheme, true);
        }

        public static bool TryCreateMovableWindow(
            Transform parent, string objectName, out GameObject instance, out WindowDragger dragger,
            Action<WindowDragger> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.MovableWindow.Movable_Window, parent, objectName, out instance, out dragger, configure, configureTheme, true);
        }

        public static bool TryCreateModalWindow(
            Transform parent, string objectName, out GameObject instance, out ModalWindowManager control,
            string resourcePath = VanillaUiPrefabCatalog.ModalWindow.Style_1,
            Action<ModalWindowManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateTooltip(
            Transform parent, string objectName, out GameObject instance, out ModernTooltipManager control,
            Action<ModernTooltipManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.Tooltip.Standard, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateContextMenu(
            Transform parent, string objectName, out GameObject instance, out ContextMenuManager control,
            Action<ContextMenuManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.ContextMenu.Context_Menu, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateHorizontalSelector(
            Transform parent, string objectName, out GameObject instance, out HorizontalSelector control,
            Action<HorizontalSelector> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.HorizontalSelector.Horizontal_Selector, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateNotification(
            Transform parent, string objectName, out GameObject instance, out ModernNotificationManager control,
            string resourcePath = VanillaUiPrefabCatalog.Notification.Popup_Notification,
            Action<ModernNotificationManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(resourcePath, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static bool TryCreateWindowManager(
            Transform parent, string objectName, out GameObject instance, out WindowManager control,
            Action<WindowManager> configure = null, Action<VanillaUiThemeSettings> configureTheme = null)
        {
            return TryInstantiatePrefab(VanillaUiPrefabCatalog.WindowManager.Window_Manager, parent, objectName, out instance, out control, configure, configureTheme, true);
        }

        public static string GetButtonResourcePath(VanillaButtonStyle style, VanillaButtonPalette palette)
        {
            bool gradient = style == VanillaButtonStyle.BasicGradient ||
                style == VanillaButtonStyle.BasicOutlineGradient ||
                style == VanillaButtonStyle.RoundedGradient ||
                style == VanillaButtonStyle.RoundedOutlineGradient;
            if (gradient && palette == VanillaButtonPalette.Standard)
            {
                return null;
            }

            string directory;
            switch (style)
            {
                case VanillaButtonStyle.Basic: directory = "basic"; break;
                case VanillaButtonStyle.BasicGradient: directory = "basic - gradient"; break;
                case VanillaButtonStyle.BasicOnlyIcon: directory = "basic - only icon"; break;
                case VanillaButtonStyle.BasicOutline: directory = "basic - outline"; break;
                case VanillaButtonStyle.BasicOutlineGradient: directory = "basic - outline gradient"; break;
                case VanillaButtonStyle.BasicOutlineOnlyIcon: directory = "basic - outline only icon"; break;
                case VanillaButtonStyle.BasicOutlineWithIcon: directory = "basic - outline with icon"; break;
                case VanillaButtonStyle.BasicWithIcon: directory = "basic - with icon"; break;
                case VanillaButtonStyle.RadialOnlyIcon: directory = "radial - only icon"; break;
                case VanillaButtonStyle.RadialOutlineOnlyIcon: directory = "radial - outline only icon"; break;
                case VanillaButtonStyle.Rounded: directory = "rounded"; break;
                case VanillaButtonStyle.RoundedGradient: directory = "rounded - gradient"; break;
                case VanillaButtonStyle.RoundedOutline: directory = "rounded - outline"; break;
                case VanillaButtonStyle.RoundedOutlineGradient: directory = "rounded - outline gradient"; break;
                default: return null;
            }

            return "button/" + directory + "/" + palette.ToString();
        }

        public static IList<string> FindPrefabPaths(string category = null, string contains = null)
        {
            List<string> matches = new List<string>();
            IList<string> paths = VanillaUiPrefabCatalog.AllPrefabPaths;
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                VanillaUiPrefabDescriptor descriptor = new VanillaUiPrefabDescriptor(path);
                if (!string.IsNullOrEmpty(category) && !string.Equals(descriptor.Category, category, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(contains) && path.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                matches.Add(path);
            }
            return matches.AsReadOnly();
        }

        public static string GetDefaultResourcePathForComponent(Type componentType)
        {
            if (componentType == null) return null;
            if (componentType == typeof(Scrollbar)) return VanillaUiPrefabCatalog.Scrollbar.Standard;
            if (componentType == typeof(ButtonManager)) return VanillaUiPrefabCatalog.Button.basic_outline_Standard;
            if (componentType == typeof(ButtonManagerBasic)) return VanillaUiPrefabCatalog.Button.basic_Standard;
            if (componentType == typeof(ButtonManagerBasicIcon)) return VanillaUiPrefabCatalog.Button.basic_only_icon_Standard;
            if (componentType == typeof(ButtonManagerBasicWithIcon)) return VanillaUiPrefabCatalog.Button.basic_with_icon_Standard;
            if (componentType == typeof(ButtonManagerIcon)) return VanillaUiPrefabCatalog.Button.basic_outline_only_icon_Standard;
            if (componentType == typeof(ButtonManagerWithIcon)) return VanillaUiPrefabCatalog.Button.basic_outline_with_icon_Standard;
            if (componentType == typeof(AnimatedIconHandler)) return VanillaUiPrefabCatalog.AnimatedIcon.Load;
            if (componentType == typeof(CustomDropdown)) return VanillaUiPrefabCatalog.Dropdown.Standard;
            if (componentType == typeof(DropdownMultiSelect)) return VanillaUiPrefabCatalog.Dropdown.Dropdown_Multi_Select;
            if (componentType == typeof(CustomInputField)) return VanillaUiPrefabCatalog.InputField.Input_Field_Standard_Left;
            if (componentType == typeof(HorizontalSelector)) return VanillaUiPrefabCatalog.HorizontalSelector.Horizontal_Selector;
            if (componentType == typeof(ModalWindowManager)) return VanillaUiPrefabCatalog.ModalWindow.Style_1;
            if (componentType == typeof(ModernNotificationManager)) return VanillaUiPrefabCatalog.Notification.Popup_Notification;
            if (componentType == typeof(ModernProgressBar)) return VanillaUiPrefabCatalog.ProgressBar.PB_Standard;
            if (componentType == typeof(SliderManager)) return VanillaUiPrefabCatalog.Slider.standard_Slider_Standard;
            if (componentType == typeof(RangeSlider)) return VanillaUiPrefabCatalog.Slider.range_Slider_Range;
            if (componentType == typeof(RangeMinSlider)) return VanillaUiPrefabCatalog.Slider.range_Slider_Range;
            if (componentType == typeof(RangeMaxSlider)) return VanillaUiPrefabCatalog.Slider.range_Slider_Range;
            if (componentType == typeof(RadialSlider)) return VanillaUiPrefabCatalog.Slider.radial_Slider_Radial;
            if (componentType == typeof(PBFilled)) return VanillaUiPrefabCatalog.ProgressBar.PB_Radial_Filled_Horizontal;
            if (componentType == typeof(UIManagerProgressBarLoop)) return VanillaUiPrefabCatalog.ProgressBarLoop.PB_Loop_Standard_Run;
            if (componentType == typeof(ScrollRect)) return VanillaUiPrefabCatalog.ListView.List_View;
            if (componentType == typeof(WindowDragger)) return VanillaUiPrefabCatalog.MovableWindow.Movable_Window;
            if (componentType == typeof(WindowManagerButton)) return VanillaUiPrefabCatalog.WindowManager.Window_Manager;
            if (componentType == typeof(SwitchManager)) return VanillaUiPrefabCatalog.Switch.Switch_Standard;
            if (componentType == typeof(ToggleAnim)) return VanillaUiPrefabCatalog.Toggle.Toggle_Standard;
            if (componentType == typeof(ModernTooltipManager)) return VanillaUiPrefabCatalog.Tooltip.Standard;
            if (componentType == typeof(ContextMenuManager)) return VanillaUiPrefabCatalog.ContextMenu.Context_Menu;
            if (componentType == typeof(WindowManager)) return VanillaUiPrefabCatalog.WindowManager.Window_Manager;
            return null;
        }

        public static bool TryGetDefaultPrefabForComponent<T>(out GameObject prefab) where T : Component
        {
            string path = GetDefaultResourcePathForComponent(typeof(T));
            prefab = string.IsNullOrEmpty(path) ? null : LoadPrefab(path);
            return prefab != null;
        }

        public static int ApplyTheme(GameObject root, UIManager theme, bool refreshImmediately)
        {
            if (root == null || theme == null) return 0;
            int assigned = 0;
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                Type type = behaviour.GetType();
                FieldInfo field = type.GetField("UIManagerAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null || !typeof(UIManager).IsAssignableFrom(field.FieldType)) continue;
                try
                {
                    field.SetValue(behaviour, theme);
                    assigned++;
                    if (refreshImmediately) RefreshUiManagerBehaviour(behaviour, type);
                }
                catch { }
            }
            return assigned;
        }

        public static int ApplyTheme(GameObject root, VanillaUiThemeSettings settings, bool refreshImmediately = true)
        {
            if (settings == null) return 0;
            UIManager theme = settings.CreateRuntimeAsset();
            if (theme == null) return 0;
            int assigned = ApplyTheme(root, theme, refreshImmediately);
            if (assigned <= 0)
            {
                UnityEngine.Object.Destroy(theme);
                return 0;
            }
            VanillaUiThemeLifetime owner = root.GetComponent<VanillaUiThemeLifetime>();
            if (owner == null) owner = root.AddComponent<VanillaUiThemeLifetime>();
            owner.ReplaceOwnedTheme(theme);
            return assigned;
        }

        public static int ConfigureComponents<T>(GameObject root, Action<T> configure) where T : Component
        {
            if (root == null || configure == null) return 0;
            T[] components = root.GetComponentsInChildren<T>(true);
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null) continue;
                configure(component);
                count++;
            }
            return count;
        }

        public static int ApplyTheme(GameObject root, Action<VanillaUiThemeSettings> configureTheme)
        {
            if (root == null) return 0;
            VanillaUiThemeSettings settings = VanillaUiThemeSettings.FromVanilla();
            if (settings == null) return 0;
            if (configureTheme != null) configureTheme(settings);
            return ApplyTheme(root, settings, true);
        }

        public static IList<VanillaUiPrefabDescriptor> DescribeAllPrefabs()
        {
            List<VanillaUiPrefabDescriptor> list = new List<VanillaUiPrefabDescriptor>();
            IList<string> paths = VanillaUiPrefabCatalog.AllPrefabPaths;
            for (int i = 0; i < paths.Count; i++) list.Add(new VanillaUiPrefabDescriptor(paths[i]));
            return list.AsReadOnly();
        }

        public static string NormalizeResourcePath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return string.Empty;
            string path = resourcePath.Replace('\\', '/').Trim();
            const string prefix = "Resources/";
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) path = path.Substring(prefix.Length);
            if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 7);
            if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 6);
            return path.TrimStart('/');
        }

        private static void RefreshUiManagerBehaviour(MonoBehaviour behaviour, Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || !method.Name.StartsWith("Update", StringComparison.Ordinal) || method.Name == "Update") continue;
                if (method.ReturnType != typeof(void) || method.GetParameters().Length != 0) continue;
                try { method.Invoke(behaviour, null); } catch { }
                return;
            }
        }
    }

    internal sealed class VanillaUiThemeLifetime : MonoBehaviour
    {
        internal UIManager Theme;
        internal void ReplaceOwnedTheme(UIManager theme)
        {
            if (Theme != null && Theme != theme) UnityEngine.Object.Destroy(Theme);
            Theme = theme;
        }
        private void OnDestroy()
        {
            if (Theme != null) UnityEngine.Object.Destroy(Theme);
            Theme = null;
        }
    }
}
