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

            if (currentGraph != null)
            {
                foreach (var nodeData in currentGraph.nodes)
                {
                    CreateNodeFromData(nodeData);
                }
            }

            serializeGraphElements = OnSerializeGraphElements;
            unserializeAndPaste = OnUnserializeAndPaste;
            graphViewChanged = OnGraphViewChanged;
        }

        public DialogueEditorNode CreateNode(Vector2 position, string nodeName = "New node", string dialogue = "")
        {
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

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            return graphViewChange;
        }

        private string OnSerializeGraphElements(System.Collections.Generic.IEnumerable<GraphElement> elements)
        {
            return string.Empty;
        }

        private void OnUnserializeAndPaste(string operationName, string data)
        {
        }

        public override System.Collections.Generic.List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
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

            Vector2 mousePosition = evt.localMousePosition;

            evt.menu.AppendAction("Crear Nuevo Nodo test", (action) =>
            {
                AddElement(CreateNode(mousePosition));
            });
        }
    }
}