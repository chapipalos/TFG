#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogueFramework.Editor
{
    [CustomEditor(typeof(QuestManager))]
    public class QuestManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var qm = (QuestManager)target;

            EditorGUI.BeginChangeCheck();
            qm.m_GraphData = (GraphData)EditorGUILayout.ObjectField(
                "Graph Data", qm.m_GraphData, typeof(GraphData), false);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(qm);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Objective bindings", EditorStyles.boldLabel);

            if (qm.m_GraphData == null)
            {
                EditorGUILayout.HelpBox("Assign a Graphdata to configure the bindings.", MessageType.Info);
                return;
            }

            if (qm.m_GraphData.s_Quests.Count == 0)
            {
                EditorGUILayout.HelpBox("The GraphData doesn't have any quest defined.", MessageType.Warning);
                return;
            }

            var questNames = new List<string>();
            var questGuids = new List<string>();
            foreach (var q in qm.m_GraphData.s_Quests)
            {
                questNames.Add(q.s_QuestTitle);
                questGuids.Add(q.s_QGuid);
            }

            for (int i = 0; i < qm.m_ObjectiveBindings.Count; i++)
            {
                var b = qm.m_ObjectiveBindings[i];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Event name
                EditorGUI.BeginChangeCheck();
                string newEvent = EditorGUILayout.TextField("Event Name", b.m_EventName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(qm, "Edit Binding Event");
                    b.m_EventName = newEvent;
                    EditorUtility.SetDirty(qm);
                }

                // Quest dropdown
                int questIdx = questGuids.IndexOf(b.m_QuestGuid);
                if (questIdx < 0)
                {
                    questIdx = 0;
                    if (questGuids.Count > 0)
                    {
                        b.m_QuestGuid = questGuids[0];
                        b.m_ObjectiveGuid = "";
                        EditorUtility.SetDirty(qm);
                    }
                }

                EditorGUI.BeginChangeCheck();
                int newQuestIdx = EditorGUILayout.Popup("Quest", questIdx, questNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(qm, "Edit Binding Quest");
                    b.m_QuestGuid = questGuids[newQuestIdx];
                    b.m_ObjectiveGuid = "";
                    EditorUtility.SetDirty(qm);
                }

                // Objective dropdown
                var selectedQuest = qm.m_GraphData.s_Quests.Find(q => q.s_QGuid == b.m_QuestGuid);
                if (selectedQuest != null && selectedQuest.s_QuestObjectives.Count > 0)
                {
                    var objNames = new List<string>();
                    var objGuids = new List<string>();
                    foreach (var obj in selectedQuest.s_QuestObjectives)
                    {
                        objNames.Add(obj.s_ObjectiveDescription);
                        objGuids.Add(obj.s_OGuid);
                    }

                    int objIdx = objGuids.IndexOf(b.m_ObjectiveGuid);
                    if (objIdx < 0)
                    {
                        objIdx = 0;
                        if (objGuids.Count > 0)
                        {
                            b.m_ObjectiveGuid = objGuids[0];
                            EditorUtility.SetDirty(qm);
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    int newObjIdx = EditorGUILayout.Popup("Objective", objIdx, objNames.ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(qm, "Edit Binding Objective");
                        b.m_ObjectiveGuid = objGuids[newObjIdx];
                        EditorUtility.SetDirty(qm);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This quest doesn't have any objectives", MessageType.Warning);
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Delete", GUILayout.Width(80)))
                {
                    Undo.RecordObject(qm, "Remove Binding");
                    qm.m_ObjectiveBindings.RemoveAt(i);
                    EditorUtility.SetDirty(qm);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add Binding"))
            {
                Undo.RecordObject(qm, "Add Binding");

                var binding = new EventObjectiveBinding();

                if (qm.m_GraphData != null && qm.m_GraphData.s_Quests.Count > 0)
                {
                    var firstQuest = qm.m_GraphData.s_Quests[0];
                    binding.m_QuestGuid = firstQuest.s_QGuid;

                    if (firstQuest.s_QuestObjectives.Count > 0)
                        binding.m_ObjectiveGuid = firstQuest.s_QuestObjectives[0].s_OGuid;
                }

                qm.m_ObjectiveBindings.Add(binding);
                EditorUtility.SetDirty(qm);
            }
        }
    }
}
#endif