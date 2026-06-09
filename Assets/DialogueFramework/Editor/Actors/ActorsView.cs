using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public class ActorsView : ListGraphView<ActorEditorNode, ActorData>
    {
        protected override float m_NodeHeight => 150f;

        public ActorsView(EditorWindow window, GraphData graph)
            : base(window, graph) { }

        protected override List<ActorData> GetDataList(GraphData graph)
            => graph.s_Actors;

        protected override ActorData BuildNewData() => new ActorData
        {
            s_AGuid = System.Guid.NewGuid().ToString(),
            s_ActorName = "New Actor"
        };

        protected override ActorEditorNode CreateNodeFromData(ActorData data)
            => new ActorEditorNode(new Vector2(0, m_NodeHeight), data);

        // Kept for backwards compatibility with EditorWindow button callbacks
        public ActorEditorNode CreateActor()   => CreateItem();
        public void RemoveActor(ActorEditorNode node) => RemoveItem(node);
    }
}
