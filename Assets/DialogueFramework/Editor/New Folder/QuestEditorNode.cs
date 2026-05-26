using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class QuestEditorNode : Node, IListNode<QuestData>
    {
        public QuestData Data { get; }

        public QuestEditorNode(Vector2 size, QuestData data)
        {
            Data  = data;
            title = data.title;

            style.width  = size.x;
            style.height = size.y;

            var titleField = new TextField("Title");
            titleField.SetValueWithoutNotify(data.title);
            titleField.RegisterValueChangedCallback(evt =>
            {
                Data.title = evt.newValue;
                title      = evt.newValue;
            });
            extensionContainer.Add(titleField);

            var descriptionField = new TextField("Description");
            descriptionField.SetValueWithoutNotify(data.description);
            descriptionField.RegisterValueChangedCallback(evt => Data.description = evt.newValue);
            extensionContainer.Add(descriptionField);

            extensionContainer.Add(new Label("\nObjectives\n"));

            var objectivesList = new ListView(
                data.objectives,
                24,
                () => new TextField(),
                (element, i) =>
                {
                    var objective = data.objectives[i];
                    var field     = element as TextField;

                    field.label = "Objective";
                    field.SetValueWithoutNotify(objective.description);
                    field.RegisterValueChangedCallback(evt => objective.description = evt.newValue);
                }
            );

            objectivesList.style.height = 80;
            extensionContainer.Add(objectivesList);

            var addObjectiveButton = new Button(() =>
            {
                data.objectives.Add(new QuestObjectiveData
                {
                    guid                 = System.Guid.NewGuid().ToString(),
                    description          = "New Objective",
                    requiredCompletedState = true
                });
                objectivesList.Rebuild();
            }) { text = "Add Objective" };
            extensionContainer.Add(addObjectiveButton);

            var removeObjectiveButton = new Button(() =>
            {
                if (data.objectives.Count == 0) return;
                data.objectives.RemoveAt(data.objectives.Count - 1);
                objectivesList.Rebuild();
            }) { text = "Remove Objective" };
            extensionContainer.Add(removeObjectiveButton);

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
