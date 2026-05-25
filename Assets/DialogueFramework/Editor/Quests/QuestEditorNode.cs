using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class QuestEditorNode : Node
    {
        public QuestData Data { get; }

        public QuestEditorNode(Vector2 size, QuestData data)
        {
            Data = data;

            title = data.title;
            style.width = 220;

            // title
            extensionContainer.Add(new Label("Title"));

            var textField = new TextField("Title");
            textField.SetValueWithoutNotify(data.title);
            textField.RegisterValueChangedCallback(evt =>
            {
                Data.title = evt.newValue;
                title = evt.newValue;
            });
            extensionContainer.Add(textField);

            // description
            extensionContainer.Add(new Label("Description"));

            var descriptionField = new TextField("Description");
            descriptionField.SetValueWithoutNotify(data.description);
            descriptionField.RegisterValueChangedCallback(evt =>
            {
                Data.description = evt.newValue;
            });
            extensionContainer.Add(descriptionField);

            // objectives
            extensionContainer.Add(new Label("Objectives"));

            var objectivesList = new ListView(data.objectives, 20, () => new Label(), (element, i) =>
            {
                var objective = data.objectives[i];
                (element as Label).text = objective.description;
            });
            extensionContainer.Add(objectivesList);

            SetPosition(new Rect(Vector2.zero, size));

            RefreshExpandedState();
            RefreshPorts();
        }

        public void SetNodeSize(float width, float height)
        {
            style.width = width;
            style.height = height;

            Rect rect = GetPosition();
            rect.width = width;
            rect.height = height;
            SetPosition(rect);
        }
    }
}
