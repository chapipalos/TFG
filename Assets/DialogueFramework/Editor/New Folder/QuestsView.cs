using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public class QuestsView : ListGraphView<QuestEditorNode, QuestData>
    {
        protected override float NodeHeight => 250f;

        public QuestsView(EditorWindow window, GraphData graph)
            : base(window, graph) { }

        protected override List<QuestData> GetDataList(GraphData graph)
            => graph.quests;

        protected override QuestData BuildNewData() => new QuestData
        {
            guid        = System.Guid.NewGuid().ToString(),
            title       = "New Quest",
            description = "",
            objectives  = new List<QuestObjectiveData>()
        };

        protected override QuestEditorNode CreateNodeFromData(QuestData data)
            => new QuestEditorNode(new Vector2(0, NodeHeight), data);

        public QuestEditorNode CreateQuest()         => CreateItem();
        public void RemoveQuest(QuestEditorNode node) => RemoveItem(node);
    }
}
