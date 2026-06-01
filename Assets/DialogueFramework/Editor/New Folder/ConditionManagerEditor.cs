#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogueFramework.Editor
{
    [CustomEditor(typeof(ConditionManager))]
    public class ConditionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var cm = (ConditionManager)target;

            // Dibuja graphData y el resto de campos normales
            EditorGUI.BeginChangeCheck();
            cm.graphData = (GraphData)EditorGUILayout.ObjectField(
                "Graph Data", cm.graphData, typeof(GraphData), false);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(cm);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Bindings", EditorStyles.boldLabel);

            if (cm.graphData == null)
            {
                EditorGUILayout.HelpBox(
                    "Asigna un GraphData para poder configurar los bindings.",
                    MessageType.Info);
                return;
            }

            // Construir lista de condiciones del grafo para el dropdown
            List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();
            foreach (var quest in cm.graphData.quests)
            {
                foreach (var obj in quest.objectives)
                {
                    objectives.Add(obj);
                }
            }
            var names = new List<string> { "— select —" };
            var guids = new List<string> { "" };

            foreach (var o in objectives)
            {
                names.Add(o.description);
                guids.Add(o.guid);
            }

            //var conditions = cm.graphData.conditions;
            //foreach (var c in conditions)
            //{
            //    names.Add(c.name);
            //    guids.Add(c.guid);
            //}

            // Dibujar cada binding
            for (int i = 0; i < cm.bindings.Count; i++)
            {
                var b = cm.bindings[i];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Event Name
                EditorGUI.BeginChangeCheck();
                string newEvent = EditorGUILayout.TextField("Event Name", b.eventName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cm, "Edit Binding Event");
                    b.eventName = newEvent;
                    EditorUtility.SetDirty(cm);
                }

                // Condition — dropdown con nombres legibles
                int currentIdx = guids.IndexOf(b.conditionGuid);
                if (currentIdx < 0) currentIdx = 0;

                EditorGUI.BeginChangeCheck();
                int newIdx = EditorGUILayout.Popup("Condition", currentIdx, names.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cm, "Edit Binding Condition");
                    b.conditionGuid = guids[newIdx];
                    EditorUtility.SetDirty(cm);
                }

                // Value To Set
                EditorGUI.BeginChangeCheck();
                bool newVal = EditorGUILayout.Toggle("Value To Set", b.valueToSet);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cm, "Edit Binding Value");
                    b.valueToSet = newVal;
                    EditorUtility.SetDirty(cm);
                }

                // Botón eliminar
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Eliminar", GUILayout.Width(80)))
                {
                    Undo.RecordObject(cm, "Remove Binding");
                    cm.bindings.RemoveAt(i);
                    EditorUtility.SetDirty(cm);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // Botón añadir
            if (GUILayout.Button("+ Add Binding"))
            {
                Undo.RecordObject(cm, "Add Binding");
                cm.bindings.Add(new EventConditionBinding());
                EditorUtility.SetDirty(cm);
            }
        }
    }
}
#endif