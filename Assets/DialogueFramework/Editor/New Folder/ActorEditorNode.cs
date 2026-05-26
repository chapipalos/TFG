using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace DialogueFramework.Editor
{
    public class ActorEditorNode : Node, IListNode<ActorData>
    {
        public ActorData Data { get; }

        public ActorEditorNode(Vector2 size, ActorData data)
        {
            Data  = data;
            title = data.name;

            style.width  = size.x;
            style.height = size.y;

            var textField = new TextField("Name");
            textField.SetValueWithoutNotify(data.name);
            textField.RegisterValueChangedCallback(evt =>
            {
                Data.name = evt.newValue;
                title     = evt.newValue;
            });

            extensionContainer.Add(textField);

            RefreshExpandedState();
            RefreshPorts();

            SetPosition(new Rect(Vector2.zero, size));
        }

        public void SetNodeSize(float width, float height)
        {
            style.width  = width;
            style.height = height;

            Rect rect = GetPosition();
            rect.width  = width;
            rect.height = height;
            SetPosition(rect);
        }
    }
}
