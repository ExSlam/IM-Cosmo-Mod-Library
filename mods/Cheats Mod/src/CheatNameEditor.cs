using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CheatsMod
{
    internal sealed class CheatNameEditor : MonoBehaviour
    {
        private const int MaximumNamePartLength = 64;
        private const float Width = 520f;
        private const float Height = 370f;
        private const float Margin = 24f;
        private const float LabelHeight = 28f;
        private const float FirstLabelTop = 76f;
        private const float FirstInputTop = 106f;
        private const float LastLabelTop = 162f;
        private const float LastInputTop = 192f;
        private const float MessageTop = 242f;
        private const float FooterTop = 304f;
        private const float FooterWidth = 220f;
        private const float FooterGap = 32f;
        private const string ObjectName = "CheatNameEditor";
        private const string TitleKey = "ui.unique_idols.edit_name";
        private const string FirstNameKey = "ui.editor.first_name";
        private const string LastNameKey = "ui.editor.last_name";
        private const string InvalidNameKey = "ui.editor.invalid_name";
        private static CheatNameEditor activeEditor;
        private InputField firstName;
        private InputField lastName;
        private TextMeshProUGUI message;
        private Button apply;
        private Action<string, string> submit;

        internal static void Show(Transform owner, string first, string last, Action<string, string> onSubmit)
        {
            CheatNumericEditor.CancelActive();
            CancelActive();
            Transform panel;
            GameObject root = CheatUi.CreateInputSheet(owner, ObjectName, CheatUi.Text(TitleKey),
                new Vector2(Width, Height), out panel);
            CheatNameEditor editor = root.AddComponent<CheatNameEditor>();
            activeEditor = editor;
            editor.submit = onSubmit;
            CheatUi.LabelAt(panel, FirstNameKey, Margin, FirstLabelTop, Width - Margin * 2f, LabelHeight);
            editor.firstName = CreateInput(panel, FirstInputTop, first);
            CheatUi.LabelAt(panel, LastNameKey, Margin, LastLabelTop, Width - Margin * 2f, LabelHeight);
            editor.lastName = CreateInput(panel, LastInputTop, last);
            editor.message = CheatUi.LabelAt(panel, InvalidNameKey, Margin, MessageTop,
                Width - Margin * 2f, CheatUi.TitleHeight, CheatUi.SmallFontSize);
            editor.message.color = mainScript.red32;
            CheatUi.ButtonAt(panel, CheatUi.CancelKey, Margin, FooterTop, FooterWidth, delegate { editor.Cancel(); });
            editor.apply = CheatUi.ButtonAt(panel, CheatUi.ApplyKey, Margin + FooterWidth + FooterGap,
                FooterTop, FooterWidth, editor.Save);
            editor.firstName.onValueChanged.AddListener(delegate { editor.Validate(); });
            editor.lastName.onValueChanged.AddListener(delegate { editor.Validate(); });
            editor.Validate();
            editor.firstName.Select();
            editor.firstName.ActivateInputField();
        }

        private static InputField CreateInput(Transform panel, float top, string value)
        {
            InputField input = CheatUi.NumericInput(panel);
            input.characterLimit = MaximumNamePartLength;
            input.text = value ?? string.Empty;
            CheatUi.Place(input.GetComponent<RectTransform>(), Margin, top, Width - Margin * 2f, CheatUi.RowHeight);
            return input;
        }

        private void Validate()
        {
            string first = firstName.text.Trim();
            string last = lastName.text.Trim();
            bool valid = (first.Length > 0 || last.Length > 0) && ValidPart(first) && ValidPart(last);
            CheatUi.SetButtonInteractable(apply, valid);
            message.gameObject.SetActive(!valid);
        }

        private static bool ValidPart(string value)
        {
            if (value.Length > MaximumNamePartLength) return false;
            foreach (char character in value) if (char.IsControl(character)) return false;
            return true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                InputField next = firstName.isFocused ? lastName : firstName;
                next.Select();
                next.ActivateInputField();
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Save();
        }

        private void Save()
        {
            Validate();
            if (!apply.interactable) return;
            Action<string, string> callback = submit;
            string first = firstName.text.Trim();
            string last = lastName.text.Trim();
            Cancel();
            if (callback != null) callback(first, last);
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
}
