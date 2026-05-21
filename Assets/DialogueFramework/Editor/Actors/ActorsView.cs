using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ActorsView : GraphView
{
    public EditorWindow Window { get; }

    private readonly List<ActorEditorNode> actorNodes = new();
    private GraphData currentGraph;

    private float scrollOffsetY = 0f;
    private const float NodeHeight = 150f;
    private const float Spacing = 10f;
    private const float ScrollSpeed = 30f;

    public ActorsView(EditorWindow window, GraphData graph)
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
            foreach (var actorData in currentGraph.actors)
            {
                CreateActorFromData(actorData);
            }
        }

        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        RegisterCallback<WheelEvent>(OnWheelEvent);
    }

    public ActorEditorNode CreateActor(string actorName = "New actor")
    {
        if (currentGraph == null)
        {
            Debug.LogError("No hay GraphData asignado.");
            return null;
        }

        var actorData = new ActorData
        {
            guid = System.Guid.NewGuid().ToString(),
            name = actorName
        };

        currentGraph.actors.Add(actorData);
        EditorUtility.SetDirty(currentGraph);

        float width = GetCurrentWidth();
        var node = new ActorEditorNode(new Vector2(width, NodeHeight), actorData);

        AddElement(node);
        actorNodes.Add(node);

        ReorderActors();

        return node;
    }

    public ActorEditorNode CreateActorFromData(ActorData data)
    {
        var node = new ActorEditorNode(new Vector2(GetCurrentWidth(), NodeHeight), data);
        AddElement(node);
        actorNodes.Add(node);
        ReorderActors();
        return node;
    }

    public void RemoveActor(ActorEditorNode node)
    {
        RemoveElement(node);
        actorNodes.Remove(node);

        if (currentGraph != null)
        {
            currentGraph.actors.Remove(node.Data);
            EditorUtility.SetDirty(currentGraph);
        }

        ReorderActors();
    }

    private void OnGeometryChangedEvent(GeometryChangedEvent evt)
    {
        ReorderActors();
    }

    private void OnWheelEvent(WheelEvent evt)
    {
        if (actorNodes.Count == 0)
            return;
        float visibleHeight = layout.height;
        float totalHeight = actorNodes.Count * (NodeHeight + Spacing);

        float maxScroll = Mathf.Max(0, totalHeight - visibleHeight);

        scrollOffsetY += evt.delta.y * ScrollSpeed;
        scrollOffsetY = Mathf.Clamp(scrollOffsetY, 0, maxScroll);

        ReorderActors();

        evt.StopPropagation();
    }

    private float GetCurrentWidth()
    {
        float width = contentRect.width > 0 ? contentRect.width : layout.width;

        if (width <= 0)
            width = 300f;

        return width;
    }

    private void ReorderActors()
    {
        float width = GetCurrentWidth();

        for (int i = 0; i < actorNodes.Count; i++)
        {
            var node = actorNodes[i];

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
