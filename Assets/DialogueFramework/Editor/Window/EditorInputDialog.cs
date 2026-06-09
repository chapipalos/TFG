#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class EditorInputDialog : EditorWindow
    {
        private Action<string> m_OnSubmit;
        private string m_InitialText;
        private string m_Message;

        /// <summary>
        /// Opens a non-modal input window. The callback is invoked with
        /// the typed string when the user clicks OK / presses Enter.
        /// Not invoked when the user cancels.
        /// </summary>
        public static void Show(string title, string message, string defaultValue, Action<string> onSubmit)
        {
            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.m_Message = message;
            window.m_InitialText = defaultValue;
            window.m_OnSubmit = onSubmit;

            var size = new Vector2(340, 120);
            var main = EditorGUIUtility.GetMainWindowPosition();
            var center = new Vector2(
                main.x + (main.width - size.x) * 0.5f,
                main.y + (main.height - size.y) * 0.5f);
            window.position = new Rect(center, size);
            window.minSize = size;
            window.maxSize = size;

            window.ShowUtility();
            window.Focus();
        }

        public new void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            // Message label
            var label = new Label(m_Message);
            label.style.marginBottom = 6;
            root.Add(label);

            // Text field
            var textField = new TextField();
            textField.value = m_InitialText;
            textField.style.marginBottom = 12;
            root.Add(textField);

            // Buttons row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.FlexEnd;

            var okButton = new Button(() =>
            {
                var callback = m_OnSubmit;
                var text = textField.value;
                m_OnSubmit = null;
                Close();
                callback?.Invoke(text);
            })
            { text = "OK" };
            okButton.style.width = 80;
            okButton.style.marginRight = 4;

            var cancelButton = new Button(Close) { text = "Cancel" };
            cancelButton.style.width = 80;

            buttonRow.Add(okButton);
            buttonRow.Add(cancelButton);
            root.Add(buttonRow);

            // Submit on Enter, cancel on Escape
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    var callback = m_OnSubmit;
                    var text = textField.value;
                    m_OnSubmit = null;
                    Close();
                    callback?.Invoke(text);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    Close();
                    evt.StopPropagation();
                }
            });

            // Give focus to the text field after the window is ready
            textField.schedule.Execute(() => textField.Focus()).StartingIn(50);
        }
    }
}
#endif