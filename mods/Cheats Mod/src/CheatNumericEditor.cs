using System;
using System.Globalization;
using CheatsMod.EmbeddedIMUiFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CheatsMod
{
    internal sealed class CheatNumericRow
    {
        private const float CaptionWidth = 138f;
        private const float LimitWidth = 56f;
        private const float IconWidth = 32f;
        private const float ValueWidth = 70f;
        private const float Gap = 6f;
        private const float Step = 1f;
        private const string ValueName = "Value";
        private const string ValueFormatKey = "ui.editor.value_range";
        private readonly Func<float> read;
        private readonly Action<float> write;
        private readonly float minimum;
        private readonly float maximum;
        private readonly bool wholeNumbers;
        private readonly TextMeshProUGUI valueLabel;
        private readonly Button lower;
        private readonly Button higher;
        private readonly Button minButton;
        private readonly Button maxButton;

        internal CheatNumericRow(Transform panel, string captionKey, float left, float top,
            float minimum, float maximum, Func<float> read, Action<float> write, bool wholeNumbers = false)
        {
            this.read = read;
            this.write = write;
            this.minimum = minimum;
            this.maximum = maximum;
            this.wholeNumbers = wholeNumbers;
            CheatUi.LabelAt(panel, captionKey, left, top, CaptionWidth, CheatUi.RowHeight);
            float cursor = left + CaptionWidth + Gap;
            minButton = CheatUi.ButtonAt(panel, CheatUi.MinKey, cursor, top, LimitWidth, delegate { Set(minimum); });
            cursor += LimitWidth + Gap;
            lower = CheatUi.NumericButton(panel, CheatUi.NumericAction.Decrease, delegate { Set(read() - Step); });
            CheatUi.Place(lower.GetComponent<RectTransform>(), cursor, top, IconWidth, CheatUi.RowHeight);
            cursor += IconWidth + Gap;
            valueLabel = CheatUi.Label(panel, ValueName, string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(valueLabel.rectTransform, cursor, top, ValueWidth, CheatUi.RowHeight);
            cursor += ValueWidth + Gap;
            higher = CheatUi.NumericButton(panel, CheatUi.NumericAction.Increase, delegate { Set(read() + Step); });
            CheatUi.Place(higher.GetComponent<RectTransform>(), cursor, top, IconWidth, CheatUi.RowHeight);
            cursor += IconWidth + Gap;
            Button edit = CheatUi.NumericButton(panel, CheatUi.NumericAction.Edit, delegate
            {
                CheatNumericEditor.Show(panel, CheatUi.Text(captionKey), read(), minimum, maximum, wholeNumbers, Set);
            });
            CheatUi.Place(edit.GetComponent<RectTransform>(), cursor, top, IconWidth, CheatUi.RowHeight);
            cursor += IconWidth + Gap;
            maxButton = CheatUi.ButtonAt(panel, CheatUi.MaxKey, cursor, top, LimitWidth, delegate { Set(maximum); });
            Refresh();
        }

        private void Set(float value)
        {
            write(Mathf.Clamp(wholeNumbers ? Mathf.Round(value) : value, minimum, maximum));
            Refresh();
        }

        internal void Refresh()
        {
            float value = read();
            string format = wholeNumbers ? CheatUi.WholeNumberFormat : CheatUi.NumberFormat;
            valueLabel.text = string.Format(CheatUi.Culture, CheatUi.Text(ValueFormatKey),
                value.ToString(format, CheatUi.Culture), maximum.ToString(format, CheatUi.Culture));
            SetEnabled(lower, value > minimum);
            SetEnabled(minButton, value > minimum);
            SetEnabled(higher, value < maximum);
            SetEnabled(maxButton, value < maximum);
        }

        private static void SetEnabled(Button button, bool enabled)
        {
            button.interactable = enabled;
            ButtonDefault native = button.GetComponent<ButtonDefault>();
            if (native != null) native.Activate(enabled);
        }
    }

    // The numeric sheet belongs to its selector. It does not enqueue a second game popup,
    // which would otherwise wait until the selector closed and discard the staged values.
    internal sealed class CheatNumericEditor : MonoBehaviour
    {
        internal const int InputCharacterLimit = 12;
        private const float Width = 460f;
        private const float Height = 300f;
        private const float Margin = 24f;
        private const float InputTop = 130f;
        private const float MessageTop = 184f;
        private const float FooterTop = 242f;
        private const float FooterWidth = 190f;
        private const float FooterGap = 32f;
        private const string OverlayName = "CheatNumericEditor";
        private const string BoundsKey = "ui.editor.bounds";
        private const string IntegerBoundsKey = "ui.editor.integer_bounds";
        private const string InvalidKey = "ui.editor.invalid";
        private static CheatNumericEditor activeEditor;
        private InputField input;
        private TextMeshProUGUI message;
        private Button apply;
        private float minimum;
        private float maximum;
        private float parsedValue;
        private bool wholeNumbers;
        private Action<float> submit;

        internal static void Show(Transform owner, string title, float value, float min, float max,
            bool integers, Action<float> onSubmit)
        {
            CheatNameEditor.CancelActive();
            CancelActive();
            Transform panel;
            GameObject overlay = CheatUi.CreateInputSheet(owner, OverlayName, title,
                new Vector2(Width, Height), out panel);
            CheatNumericEditor editor = overlay.AddComponent<CheatNumericEditor>();
            activeEditor = editor;
            editor.minimum = min;
            editor.maximum = max;
            editor.wholeNumbers = integers;
            editor.submit = onSubmit;
            TextMeshProUGUI bounds = CheatUi.LabelAt(panel, integers ? IntegerBoundsKey : BoundsKey,
                Margin, CheatUi.TitleInset + CheatUi.TitleHeight, Width - Margin * 2f, CheatUi.TitleHeight);
            bounds.text = string.Format(CheatUi.Culture, bounds.text, min, max);
            editor.input = CheatUi.NumericInput(panel);
            CheatUi.Place(editor.input.GetComponent<RectTransform>(), Margin, InputTop, Width - Margin * 2f, CheatUi.RowHeight);
            editor.message = CheatUi.LabelAt(panel, InvalidKey, Margin, MessageTop,
                Width - Margin * 2f, CheatUi.TitleHeight, CheatUi.SmallFontSize);
            editor.message.color = mainScript.red32;
            CheatUi.ButtonAt(panel, CheatUi.CancelKey, Margin, FooterTop, FooterWidth, delegate { editor.Cancel(); });
            editor.apply = CheatUi.ButtonAt(panel, CheatUi.ApplyKey,
                Margin + FooterWidth + FooterGap, FooterTop, FooterWidth, editor.Save);
            editor.input.onValueChanged.AddListener(editor.Validate);
            editor.input.text = value.ToString(integers ? CheatUi.WholeNumberFormat : CheatUi.NumberFormat, CheatUi.Culture);
            editor.Validate(editor.input.text);
            editor.input.Select();
            editor.input.ActivateInputField();
        }

        private void Validate(string text)
        {
            bool valid = float.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CheatUi.Culture, out parsedValue)
                && !float.IsNaN(parsedValue) && !float.IsInfinity(parsedValue)
                && parsedValue >= minimum && parsedValue <= maximum
                && (!wholeNumbers || parsedValue == Mathf.Floor(parsedValue));
            CheatUi.SetButtonInteractable(apply, valid);
            message.gameObject.SetActive(!valid);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Save();
        }

        private void Save()
        {
            Validate(input.text);
            if (!apply.interactable) return;
            Action<float> callback = submit;
            float value = parsedValue;
            Cancel();
            if (callback != null) callback(value);
        }

        private void Cancel()
        {
            if (activeEditor == this) activeEditor = null;
            submit = null;
            gameObject.SetActive(false);
            UnityEngine.Object.Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (activeEditor == this) activeEditor = null;
            submit = null;
        }

        internal static bool CancelActive()
        {
            if (activeEditor == null || !activeEditor.gameObject.activeInHierarchy) return false;
            activeEditor.Cancel();
            return true;
        }
    }

    [HarmonyPatch(typeof(PopupManager), nameof(PopupManager.Close), new Type[] { typeof(Action) })]
    internal static class CheatNumericEditorClose
    {
        // Escape closes the focused numeric sheet first, leaving the selector and its popup queue intact.
        private static bool Prefix() { return !CheatNameEditor.CancelActive() && !CheatNumericEditor.CancelActive(); }
    }
}
