using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class DialogueEditorNode : Node
    {
        public NodeData Data { get; }

        public Port InputPort;
        public Port OutputPort;

        private GraphData currentGraph;

        public DialogueEditorNode(Vector2 position, NodeData data, GraphData graph)
        {
            Data = data;
            currentGraph = graph;

            title = data.title;
            style.width = 220;

            InputPort = InstantiatePort(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,
                typeof(float)
            );
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                typeof(float)
            );
            OutputPort.portName = "Out";
            outputContainer.Add(OutputPort);

            var textField = new TextField("Name");
            textField.SetValueWithoutNotify(data.title);
            textField.RegisterValueChangedCallback(evt =>
            {
                Data.title = evt.newValue;
                title = evt.newValue;
                EditorUtility.SetDirty(currentGraph);
            });
            extensionContainer.Add(textField);

            var dialogueField = new TextField("Dialogue");
            dialogueField.SetValueWithoutNotify(data.dialogue);
            dialogueField.RegisterValueChangedCallback(evt =>
            {
                Data.dialogue = evt.newValue;
                EditorUtility.SetDirty(currentGraph);
            });
            extensionContainer.Add(dialogueField);

            CreateActorDropdown();
            CreateConditionsFoldout();

            RefreshExpandedState();
            RefreshPorts();

            SetPosition(new Rect(position, new Vector2(220, 150)));
        }

        private void CreateActorDropdown()
        {
            var actorNames = currentGraph.actors
                .Select(actor => actor.name)
                .ToList();

            if (actorNames.Count == 0)
                actorNames.Add("No actors");

            string currentActorName = "No actors";

            if (!string.IsNullOrEmpty(Data.actorGuid))
            {
                var actor = currentGraph.actors
                    .FirstOrDefault(a => a.guid == Data.actorGuid);

                if (actor != null)
                    currentActorName = actor.name;
            }
            else if (currentGraph.actors.Count > 0)
            {
                Data.actorGuid = currentGraph.actors[0].guid;
                currentActorName = currentGraph.actors[0].name;
            }

            var actorDropdown = new PopupField<string>(
                "Actor",
                actorNames,
                currentActorName
            );

            actorDropdown.RegisterValueChangedCallback(evt =>
            {
                var selectedActor = currentGraph.actors
                    .FirstOrDefault(a => a.name == evt.newValue);

                if (selectedActor != null)
                {
                    Data.actorGuid = selectedActor.guid;
                    EditorUtility.SetDirty(currentGraph);
                }
            });

            extensionContainer.Add(actorDropdown);
        }

        private void CreateConditionsFoldout()
        {
            var conditionsFoldout = new Foldout
            {
                text = "Conditions",
                value = true
            };

            var conditionsContainer = new VisualElement();
            conditionsFoldout.Add(conditionsContainer);

            if (Data.conditions == null)
                Data.conditions = new List<NodeConditionData>();

            foreach (var condition in Data.conditions)
            {
                AddConditionRow(conditionsContainer, condition);
            }

            Button addConditionButton = new Button(() =>
            {
                if (currentGraph.conditions.Count == 0)
                    return;

                var conditionData = new NodeConditionData
                {
                    conditionGuid = currentGraph.conditions[0].guid,
                    requiredValue = false
                };

                Data.conditions.Add(conditionData);
                AddConditionRow(conditionsContainer, conditionData);

                EditorUtility.SetDirty(currentGraph);
            })
            {
                text = "+ Add Condition"
            };

            conditionsFoldout.Add(addConditionButton);
            extensionContainer.Add(conditionsFoldout);
        }

        private void AddConditionRow(VisualElement container, NodeConditionData data)
        {
            var conditionNames = currentGraph.conditions
                .Select(condition => condition.name)
                .ToList();

            if (conditionNames.Count == 0)
                conditionNames.Add("No conditions");

            string currentConditionName = "No conditions";

            var currentCondition = currentGraph.conditions
                .FirstOrDefault(c => c.guid == data.conditionGuid);

            if (currentCondition != null)
                currentConditionName = currentCondition.name;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var dropdown = new PopupField<string>(
                conditionNames,
                currentConditionName
            );

            dropdown.style.flexGrow = 1;

            dropdown.RegisterValueChangedCallback(evt =>
            {
                var selectedCondition = currentGraph.conditions
                    .FirstOrDefault(c => c.name == evt.newValue);

                if (selectedCondition != null)
                {
                    data.conditionGuid = selectedCondition.guid;
                    EditorUtility.SetDirty(currentGraph);
                }
            });

            var toggle = new Toggle();
            toggle.value = data.requiredValue;

            toggle.RegisterValueChangedCallback(evt =>
            {
                data.requiredValue = evt.newValue;
                EditorUtility.SetDirty(currentGraph);
            });

            var removeButton = new Button(() =>
            {
                Data.conditions.Remove(data);
                container.Remove(row);
                EditorUtility.SetDirty(currentGraph);
            })
            {
                text = "X"
            };

            row.Add(dropdown);
            row.Add(toggle);
            row.Add(removeButton);

            container.Add(row);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            evt.menu.AppendAction("Duplicate node", action =>
            {
                var graphView = this.GetFirstAncestorOfType<DialoguesView>();
                if (graphView == null)
                    return;

                Vector2 pos = GetPosition().position + new Vector2(30, 30);
                graphView.CreateNode(pos, Data.title + " 1", Data.dialogue);
            });
        }
    }
}