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

            // FIX: persist node position changes when the user drags a node
            graphViewChanged = OnGraphViewChanged;

            // Nodes and edges are NOT loaded here.
            // NodeGraphSaveUtility.Load() populates the view so that
            // nodes and links are always restored together in the right order.
        }

        // ── Node creation ─────────────────────────────────────────────────────

        public DialogueEditorNode CreateNode(
            Vector2 position,
            string nodeName = "New node",
            string dialogue = "")
        {
            // FIX: guard against null graph instead of throwing a NullReferenceException
            if (currentGraph == null)
            {
                Debug.LogError("[DialoguesView] Cannot create node — no GraphData assigned.");
                return null;
            }

            var data = new NodeData
            {
                guid = System.Guid.NewGuid().ToString(),
                title = nodeName,
                dialogue = dialogue,
                position = position,
                conditions = new List<NodeConditionData>()
            };

            currentGraph.nodes.Add(data);
            EditorUtility.SetDirty(currentGraph);

            return CreateNodeFromData(data);
        }

        public DialogueEditorNode CreateNodeFromData(NodeData data)
        {
            var node = new DialogueEditorNode(data.position, data, currentGraph);
            AddElement(node);
            return node;
        }

        // ── Graph changes ─────────────────────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // FIX: persist position when nodes are moved
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is DialogueEditorNode node)
                        node.Data.position = node.GetPosition().position;
                }

                if (currentGraph != null)
                    EditorUtility.SetDirty(currentGraph);
            }

            // Persist edge removals (node deletions are handled by NodeGraphSaveUtility.Save)
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is DialogueEditorNode node && currentGraph != null)
                    {
                        currentGraph.nodes.Remove(node.Data);
                        EditorUtility.SetDirty(currentGraph);
                    }
                }
            }

            return change;
        }

        // ── Port compatibility ────────────────────────────────────────────────

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                port != startPort &&
                port.node != startPort.node &&
                port.direction != startPort.direction
            ).ToList();
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        public void ClearGraph()
        {
            foreach (var edge in edges.ToList())
                RemoveElement(edge);

            foreach (var node in nodes.ToList())
                RemoveElement(node);
        }

        // ── Context menu ──────────────────────────────────────────────────────

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            // Convert mouse position from panel space to graph content space
            Vector2 mousePos = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            evt.menu.AppendAction("Create node", _ =>
            {
                CreateNode(mousePos);
            });
        }
    }
}