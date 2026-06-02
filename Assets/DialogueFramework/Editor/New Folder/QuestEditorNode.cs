using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class QuestEditorNode : Node, IListNode<QuestData>
    {
        public QuestData m_Data { get; }

        public QuestEditorNode(Vector2 size, QuestData data)
        {
            m_Data  = data;
            title = data.s_QuestTitle;

            style.width  = size.x;
            style.height = size.y;

            var titleField = new TextField("Title");
            titleField.SetValueWithoutNotify(data.s_QuestTitle);
            titleField.RegisterValueChangedCallback(evt =>
            {
                m_Data.s_QuestTitle = evt.newValue;
                title      = evt.newValue;
            });
            extensionContainer.Add(titleField);

            var descriptionField = new TextField("Description");
            descriptionField.SetValueWithoutNotify(data.s_QuestDescription);
            descriptionField.RegisterValueChangedCallback(evt => m_Data.s_QuestDescription = evt.newValue);
            extensionContainer.Add(descriptionField);

            extensionContainer.Add(new Label("\nObjectives\n"));

            var objectivesList = new ListView(
                data.s_QuestObjectives,
                24,
                () => new TextField(),
                (element, i) =>
                {
                    var objective = data.s_QuestObjectives[i];
                    var field     = element as TextField;

                    field.label = "Objective";
                    field.SetValueWithoutNotify(objective.s_ObjectiveDescription);
                    field.RegisterValueChangedCallback(evt => objective.s_ObjectiveDescription = evt.newValue);
                }
            );

            objectivesList.style.height = 80;
            extensionContainer.Add(objectivesList);

            var addObjectiveButton = new Button(() =>
            {
                data.s_QuestObjectives.Add(new QuestObjectiveData
                {
                    s_OGuid                 = System.Guid.NewGuid().ToString(),
                    s_ObjectiveDescription          = "New Objective",
                    s_RequiredCompletedState = true
                });
                objectivesList.Rebuild();
            }) { text = "Add Objective" };
            extensionContainer.Add(addObjectiveButton);

            var removeObjectiveButton = new Button(() =>
            {
                if (data.s_QuestObjectives.Count == 0) return;
                data.s_QuestObjectives.RemoveAt(data.s_QuestObjectives.Count - 1);
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
