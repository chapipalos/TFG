using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class QuestsView : GraphView
{
    public EditorWindow Window { get; }

    private readonly List<QuestEditorNode> questsNodes = new();
    private GraphData currentGraph;

    private float scrollOffsetY = 0f;
    private const float NodeHeight = 150f;
    private const float Spacing = 10f;
    private const float ScrollSpeed = 30f;

    public QuestsView(EditorWindow window, GraphData graph)
    {
        Window = window;

        Insert(0, new GridBackground());

        //this.AddManipulator(new ContentDragger());
        //this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        serializeGraphElements = OnSerializeGraphElements;
        unserializeAndPaste = OnUnserializeAndPaste;
        graphViewChanged = OnGraphViewChanged;

        currentGraph = graph;

        if (currentGraph != null)
        {
            foreach (var questData in currentGraph.quests)
            {
                CreateQuestFromData(questData);
            }
        }

        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        RegisterCallback<WheelEvent>(OnWheelEvent);
    }

    public QuestEditorNode CreateQuest(string questName = "New Quest")
    {
        if (currentGraph == null)
        {
            Debug.LogError("No hay GraphData asignado.");
            return null;
        }

        var questData = new QuestData
        {
            guid = System.Guid.NewGuid().ToString(),
            title = questName,
            description = "",
            objectives = new List<QuestObjectiveData>()
        };

        currentGraph.quests.Add(questData);
        EditorUtility.SetDirty(currentGraph);

        return CreateQuestFromData(questData);
    }

    public void RemoveQuest(QuestEditorNode node)
    {
        if (node == null)
            return;

        RemoveElement(node);
        questsNodes.Remove(node);

        if (currentGraph != null)
        {
            currentGraph.quests.Remove(node.Data);
            EditorUtility.SetDirty(currentGraph);
        }

        ReorderQuests();
    }

    public QuestEditorNode CreateQuestFromData(QuestData data)
    {
        var questNode = new QuestEditorNode(new Vector2(GetCurrentWidth(), NodeHeight), data);
        AddElement(questNode);
        questsNodes.Add(questNode);
        ReorderQuests();
        return questNode;
    }

    private float GetCurrentWidth()
    {
        float width = contentRect.width > 0 ? contentRect.width : layout.width;

        if (width <= 0)
            width = 300f;

        return width;
    }

    private void ReorderQuests()
    {
        float width = GetCurrentWidth();

        for (int i = 0; i < questsNodes.Count; i++)
        {
            var node = questsNodes[i];

            node.SetNodeSize(width, NodeHeight);

            float y = i * (NodeHeight + Spacing) - scrollOffsetY;
            node.SetPosition(new Rect(0, y, width, NodeHeight));
        }
    }

    private void OnGeometryChangedEvent(GeometryChangedEvent evt)
    {
        ReorderQuests();
    }

    private void OnWheelEvent(WheelEvent evt)
    {
        if (questsNodes.Count == 0)
            return;
        float visibleHeight = layout.height;
        float totalHeight = questsNodes.Count * (NodeHeight + Spacing);

        float maxScroll = Mathf.Max(0, totalHeight - visibleHeight);

        scrollOffsetY += evt.delta.y * ScrollSpeed;
        scrollOffsetY = Mathf.Clamp(scrollOffsetY, 0, maxScroll);

        ReorderQuests();

        evt.StopPropagation();
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
