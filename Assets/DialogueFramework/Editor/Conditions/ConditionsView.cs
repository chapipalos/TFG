using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class ConditionsView : GraphView
    {
        public EditorWindow Window { get; }

        public readonly List<ConditionEditorNode> conditionNodes = new List<ConditionEditorNode>();
        private GraphData currentGraph;

        private float scrollOffsetY = 0f;
        private const float NodeHeight = 80f;
        private const float Spacing = 10f;
        private const float ScrollSpeed = 30f;

        public ConditionsView(EditorWindow window, GraphData graph)
        {
            Window = window;

            Insert(0, new GridBackground());

            // Quita el zoom con rueda
            // SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            //this.AddManipulator(new ContentDragger());
            //this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            serializeGraphElements = OnSerializeGraphElements;
            unserializeAndPaste = OnUnserializeAndPaste;
            graphViewChanged = OnGraphViewChanged;

            currentGraph = graph;
            if (currentGraph != null)
            {
                foreach (var conditionData in currentGraph.conditions)
                {
                    CreateConditionFromData(conditionData);
                }
            }

            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
            RegisterCallback<WheelEvent>(OnWheelEvent);
        }

        public ConditionEditorNode CreateCondition(string conditionName = "New condition")
        {
            if (currentGraph == null)
            {
                Debug.LogError("No hay GraphData asignado.");
                return null;
            }

            var conditionData = new ConditionData
            {
                guid = System.Guid.NewGuid().ToString(),
                name = conditionName,
                value = false
            };

            currentGraph.conditions.Add(conditionData);
            EditorUtility.SetDirty(currentGraph);

            float width = contentRect.width > 0 ? contentRect.width : layout.width;

            if (width <= 0)
                width = 300f;

            var node = new ConditionEditorNode(new Vector2(width, NodeHeight), conditionData);

            AddElement(node);
            conditionNodes.Add(node);

            ReorderConditions();

            return node;
        }

        public ConditionEditorNode CreateConditionFromData(ConditionData data)
        {
            var node = new ConditionEditorNode(new Vector2(GetCurrentWidth(), NodeHeight), data);
            AddElement(node);
            conditionNodes.Add(node);
            ReorderConditions();
            return node;
        }

        public void RemoveCondition(ConditionEditorNode node)
        {
            RemoveElement(node);
            conditionNodes.Remove(node);

            if (currentGraph != null)
            {
                currentGraph.conditions.Remove(node.Data);
                EditorUtility.SetDirty(currentGraph);
            }
            ReorderConditions();
        }

        private void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            ReorderConditions();
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            float visibleHeight = layout.height;
            float totalHeight = conditionNodes.Count * (NodeHeight + Spacing);

            float maxScroll = Mathf.Max(0, totalHeight - visibleHeight);

            scrollOffsetY += evt.delta.y * ScrollSpeed;
            scrollOffsetY = Mathf.Clamp(scrollOffsetY, 0, maxScroll);

            ReorderConditions();

            evt.StopPropagation();
        }

        private float GetCurrentWidth()
        {
            float width = contentRect.width > 0 ? contentRect.width : layout.width;

            if (width <= 0)
                width = 300f;

            return width;
        }

        private void ReorderConditions()
        {
            float width = GetCurrentWidth();

            for (int i = 0; i < conditionNodes.Count; i++)
            {
                var node = conditionNodes[i];

                node.SetNodeSize(width, NodeHeight);

                float y = i * (NodeHeight + Spacing) - scrollOffsetY;
                node.SetPosition(new Rect(0, y, width, NodeHeight));
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            return graphViewChange;
        }

        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            return string.Empty;
        }

        private void OnUnserializeAndPaste(string operationName, string data)
        {
        }
    }
}
