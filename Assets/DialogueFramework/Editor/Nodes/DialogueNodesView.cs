using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class DialoguesView : GraphView
    {
        public EditorWindow Window { get; }

        private GraphData currentGraph;
        private string currentConversationGuid;

        public DialoguesView(EditorWindow window, GraphData graph)
        {
            Window = window;
            currentGraph = graph;

            Insert(0, new GridBackground());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            serializeGraphElements = _ => string.Empty;
            unserializeAndPaste = (_, _) => { };

            graphViewChanged = OnGraphViewChanged;
        }

        public string CurrentConversationGuid => currentConversationGuid;

        /// <summary>Cambia la conversación activa y recarga el view solo con sus nodos.</summary>
        public void SetCurrentConversation(string conversationGuid)
        {
            currentConversationGuid = conversationGuid;
            NodeGraphSaveUtility.Load(this, currentGraph);
        }

        // ── Node creation ─────────────────────────────────────────────────────

        public DialogueEditorNode CreateNode(
            Vector2 position,
            string nodeName = "New node",
            string dialogue = "")
        {
            if (currentGraph == null)
            {
                Debug.LogError("[DialoguesView] Cannot create node — no GraphData assigned.");
                return null;
            }

            if (string.IsNullOrEmpty(currentConversationGuid))
            {
                EditorUtility.DisplayDialog(
                    "Sin conversación seleccionada",
                    "Selecciona o crea una conversación antes de añadir nodos.",
                    "OK");
                return null;
            }

            var data = new NodeData
            {
                s_NGuid = System.Guid.NewGuid().ToString(),
                s_NodeTitle = nodeName,
                s_Dialogue = dialogue,
                s_NodePosition = position,
                s_ConversationGuid = currentConversationGuid
            };

            currentGraph.s_Nodes.Add(data);
            EditorUtility.SetDirty(currentGraph);

            return CreateNodeFromData(data);
        }

        public DialogueEditorNode CreateNodeFromData(NodeData data)
        {
            var node = new DialogueEditorNode(data.s_NodePosition, data, currentGraph);
            AddElement(node);
            return node;
        }

        // ── Graph changes ─────────────────────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                    if (element is DialogueEditorNode node)
                        node.m_Data.s_NodePosition = node.GetPosition().position;

                if (currentGraph != null)
                    EditorUtility.SetDirty(currentGraph);
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is DialogueEditorNode node && currentGraph != null)
                    {
                        currentGraph.s_Nodes.Remove(node.m_Data);
                        EditorUtility.SetDirty(currentGraph);
                    }
                }
            }

            return change;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                port != startPort &&
                port.node != startPort.node &&
                port.direction != startPort.direction
            ).ToList();
        }

        public void ClearGraph()
        {
            foreach (var edge in edges.ToList())
                RemoveElement(edge);

            foreach (var node in nodes.ToList())
                RemoveElement(node);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            Vector2 mousePos = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            evt.menu.AppendAction("Create node", _ =>
            {
                CreateNode(mousePos);
            });
        }
    }
}