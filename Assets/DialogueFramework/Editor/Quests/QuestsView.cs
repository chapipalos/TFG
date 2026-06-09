using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public class QuestsView : ListGraphView<QuestEditorNode, QuestData>
    {
        protected override float m_NodeHeight => 250f;

        public QuestsView(EditorWindow window, GraphData graph)
            : base(window, graph) { }

        protected override List<QuestData> GetDataList(GraphData graph)
            => graph.s_Quests;

        protected override QuestData BuildNewData() => new QuestData
        {
            s_QGuid        = System.Guid.NewGuid().ToString(),
            s_QuestTitle       = "New Quest",
            s_QuestDescription = "",
            s_QuestObjectives  = new List<QuestObjectiveData>()
        };

        protected override QuestEditorNode CreateNodeFromData(QuestData data)
            => new QuestEditorNode(new Vector2(0, m_NodeHeight), data);

        public QuestEditorNode CreateQuest()         => CreateItem();
        public void RemoveQuest(QuestEditorNode node) => RemoveItem(node);
    }
}
