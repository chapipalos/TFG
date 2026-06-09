#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogueFramework.Editor
{
    [CustomEditor(typeof(DialogueManager))]
    public class DialogueManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty m_FloatingDialogue;

        private SerializedProperty m_StartDialogueProp;
        private SerializedProperty m_GraphDataProp;
        private SerializedProperty m_ConversationNameProp;

        // UI - dialogue
        private SerializedProperty m_DialoguePanelProp;
        private SerializedProperty m_ActorPanelProp;
        private SerializedProperty m_ActorTextProp;
        private SerializedProperty m_DialogueTextProp;
        private SerializedProperty m_DialogueBarProgressProp;

        // UI - navigation
        private SerializedProperty m_NextDialogueButtonProp;
        private SerializedProperty m_NextButtonTextSkipProp;
        private SerializedProperty m_NextButtonTextContinueProp;
        private SerializedProperty m_RepliesPanelProp;
        private SerializedProperty m_ReplyButtonPrefabProp;

        // Typewriter
        private SerializedProperty m_CharDelayProp;
        private SerializedProperty m_ProgressBarSmoothingProp;

        private void OnEnable()
        {
            m_FloatingDialogue = serializedObject.FindProperty("m_IsFloatingDialogue");

            m_StartDialogueProp = serializedObject.FindProperty("m_DialogueRunning");
            m_GraphDataProp = serializedObject.FindProperty("m_GraphData");
            m_ConversationNameProp = serializedObject.FindProperty("m_ConversationName");

            m_DialoguePanelProp = serializedObject.FindProperty("m_DialoguePanel");
            m_ActorPanelProp = serializedObject.FindProperty("m_ActorPanel");
            m_ActorTextProp = serializedObject.FindProperty("m_ActorText");
            m_DialogueTextProp = serializedObject.FindProperty("m_DialogueText");
            m_DialogueBarProgressProp = serializedObject.FindProperty("m_DialogueBarProgress");

            m_NextDialogueButtonProp = serializedObject.FindProperty("m_NextDialogueButton");
            m_NextButtonTextSkipProp = serializedObject.FindProperty("m_NextButtonTextSkip");
            m_NextButtonTextContinueProp = serializedObject.FindProperty("m_NextButtonTextContinue");
            m_RepliesPanelProp = serializedObject.FindProperty("m_RepliesPanel");
            m_ReplyButtonPrefabProp = serializedObject.FindProperty("m_ReplyButtonPrefab");

            m_CharDelayProp = serializedObject.FindProperty("m_CharDelay");
            m_ProgressBarSmoothingProp = serializedObject.FindProperty("m_ProgressBarSmoothing");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_FloatingDialogue);

            EditorGUILayout.PropertyField(m_StartDialogueProp);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_GraphDataProp);

            DrawConversationDropdown();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("UI — dialogue", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_DialoguePanelProp);
            EditorGUILayout.PropertyField(m_ActorPanelProp);
            EditorGUILayout.PropertyField(m_ActorTextProp);
            EditorGUILayout.PropertyField(m_DialogueTextProp);
            EditorGUILayout.PropertyField(m_DialogueBarProgressProp);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("UI — navigation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_NextDialogueButtonProp);
            EditorGUILayout.PropertyField(m_NextButtonTextSkipProp);
            EditorGUILayout.PropertyField(m_NextButtonTextContinueProp);
            EditorGUILayout.PropertyField(m_RepliesPanelProp);
            EditorGUILayout.PropertyField(m_ReplyButtonPrefabProp);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Typewriter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_CharDelayProp);
            EditorGUILayout.PropertyField(m_ProgressBarSmoothingProp);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConversationDropdown()
        {
            var graphData = m_GraphDataProp.objectReferenceValue as GraphData;

            if (graphData == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a GraphData asset to select a conversation.",
                    MessageType.Info);
                return;
            }

            if (graphData.s_Conversations == null || graphData.s_Conversations.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The assigned GraphData has no conversations defined.",
                    MessageType.Warning);
                return;
            }

            var names = new List<string>();
            foreach (var c in graphData.s_Conversations)
                names.Add(c.s_CName);

            string currentName = m_ConversationNameProp.stringValue;

            int currentIdx = names.IndexOf(currentName);
            if (currentIdx < 0) currentIdx = 0;

            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUILayout.Popup("Conversation", currentIdx, names.ToArray());
            if (EditorGUI.EndChangeCheck() || string.IsNullOrEmpty(currentName) || !names.Contains(currentName))
            {
                m_ConversationNameProp.stringValue = names[newIdx];
            }
        }
    }
}
#endif