using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public class ConditionsView : ListGraphView<ConditionEditorNode, ConditionData>
    {
        protected override float NodeHeight => 80f;

        public ConditionsView(EditorWindow window, GraphData graph)
            : base(window, graph) { }

        protected override List<ConditionData> GetDataList(GraphData graph)
            => graph.conditions;

        protected override ConditionData BuildNewData() => new ConditionData
        {
            guid  = System.Guid.NewGuid().ToString(),
            name  = "New Condition",
            value = false
        };

        protected override ConditionEditorNode CreateNodeFromData(ConditionData data)
            => new ConditionEditorNode(new Vector2(0, NodeHeight), data);

        public ConditionEditorNode CreateCondition()         => CreateItem();
        public void RemoveCondition(ConditionEditorNode node) => RemoveItem(node);
    }
}
