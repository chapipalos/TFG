#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DialogueFramework.Editor
{
    /// <summary>
    /// Modal input dialog for the editor. Used by EditorWindow to rename
    /// conversations and similar simple text-input flows.
    /// Usage:
    ///     string newName = EditorInputDialog.Show("Title", "Prompt:", "default");
    ///     if (newName != null) { ... }
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string m_InputText = "";
        private string m_Message = "";
        private bool m_Submitted = false;

        public static string Show(string title, string message, string defaultValue = "")
        {
            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.m_Message = message;
            window.m_InputText = defaultValue;
            window.position = new Rect(
                Screen.width / 2f,
                Screen.height / 2f,
                320,
                100);
            window.ShowModal();
            return window.m_Submitted ? window.m_InputText : null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(m_Message);
            m_InputText = EditorGUILayout.TextField(m_InputText);

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                m_Submitted = true;
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                m_Submitted = false;
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
