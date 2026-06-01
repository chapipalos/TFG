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

        // Single input port — always present
        public Port InputPort;

        // Generic output port — visible only when there are no replies
        public Port OutputPort;

        // One output port per reply — keyed by PlayerReplyData.guid
        private readonly Dictionary<string, Port> replyPorts = new();

        private GraphData currentGraph;

        public DialogueEditorNode(Vector2 position, NodeData data, GraphData graph)
        {
            Data = data;
            currentGraph = graph;

            title = data.title;
            style.width = 260;

            // ── Input port ────────────────────────────────────────────────────
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            // ── Generic output port (hidden when replies exist) ────────────────
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            OutputPort.portName = "Out";
            outputContainer.Add(OutputPort);

            // ── Fields ────────────────────────────────────────────────────────
            var titleField = new TextField("Name");
            titleField.SetValueWithoutNotify(data.title);
            titleField.RegisterValueChangedCallback(evt =>
            {
                Data.title = evt.newValue;
                title = evt.newValue;
                EditorUtility.SetDirty(currentGraph);
            });
            extensionContainer.Add(titleField);

            var dialogueField = new TextField("Dialogue");
            dialogueField.multiline = true;
            dialogueField.SetValueWithoutNotify(data.dialogue);
            dialogueField.RegisterValueChangedCallback(evt =>
            {
                Data.dialogue = evt.newValue;
                EditorUtility.SetDirty(currentGraph);
            });
            extensionContainer.Add(dialogueField);

            BuildActorDropdown();
            BuildConditionsFoldout();
            BuildQuestRequirementsFoldout();
            BuildQuestDropdown();
            BuildRepliesFoldout();

            RefreshExpandedState();
            RefreshPorts();

            SetPosition(new Rect(position, new Vector2(260, 150)));
        }

        // ── Reply ports (public API for SaveUtility) ──────────────────────────

        /// <summary>Returns the output port for a given reply GUID, or null.</summary>
        public Port GetReplyPort(string replyGuid)
            => replyPorts.TryGetValue(replyGuid, out var port) ? port : null;

        /// <summary>Returns all reply ports in order.</summary>
        public IEnumerable<(string replyGuid, Port port)> GetAllReplyPorts()
            => replyPorts.Select(kv => (kv.Key, kv.Value));

        // ── Replies foldout ───────────────────────────────────────────────────

        private void BuildRepliesFoldout()
        {
            if (Data.replies == null)
                Data.replies = new List<PlayerReplyData>();

            var foldout = new Foldout { text = "Player replies", value = true };

            // Restore existing replies
            foreach (var reply in Data.replies)
                AddReplyRow(foldout, reply);

            var addButton = new Button(() =>
            {
                var reply = new PlayerReplyData
                {
                    guid = System.Guid.NewGuid().ToString(),
                    text = "New reply"
                };
                Data.replies.Add(reply);
                AddReplyRow(foldout, reply);
                EditorUtility.SetDirty(currentGraph);
                UpdateGenericPortVisibility();
            })
            { text = "+ Add Reply" };

            foldout.Add(addButton);
            extensionContainer.Add(foldout);

            UpdateGenericPortVisibility();
        }

        private void AddReplyRow(VisualElement container, PlayerReplyData reply)
        {
            // Create the output port for this reply
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            port.portName = reply.text;
            outputContainer.Add(port);
            replyPorts[reply.guid] = port;

            // Row with text field + remove button inside the foldout
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var textField = new TextField();
            textField.style.flexGrow = 1;
            textField.SetValueWithoutNotify(reply.text);
            textField.RegisterValueChangedCallback(evt =>
            {
                reply.text = evt.newValue;
                port.portName = evt.newValue;   // keep port label in sync
                EditorUtility.SetDirty(currentGraph);
            });

            var removeButton = new Button(() =>
            {
                // Disconnect and remove port
                if (port.connected)
                {
                    var edgesToRemove = port.connections.ToList();
                    foreach (var edge in edgesToRemove)
                    {
                        edge.input.Disconnect(edge);
                        edge.output.Disconnect(edge);
                        var gv = this.GetFirstAncestorOfType<DialoguesView>();
                        gv?.RemoveElement(edge);
                    }
                }

                outputContainer.Remove(port);
                replyPorts.Remove(reply.guid);
                Data.replies.Remove(reply);
                container.Remove(row);

                EditorUtility.SetDirty(currentGraph);
                UpdateGenericPortVisibility();
                RefreshPorts();
                RefreshExpandedState();
            })
            { text = "✕" };

            row.Add(textField);
            row.Add(removeButton);
            container.Add(row);

            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>
        /// The generic OutputPort is only shown when there are no replies.
        /// When replies exist each reply has its own port, so the generic
        /// one would be redundant and confusing.
        /// </summary>
        private void UpdateGenericPortVisibility()
        {
            bool hasReplies = Data.replies != null && Data.replies.Count > 0;
            OutputPort.style.display = hasReplies ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ── Actor dropdown ────────────────────────────────────────────────────

        private void BuildActorDropdown()
        {
            var actorDropdown = BuildDynamicDropdown(
                label: "Actor",
                getChoices: () =>
                {
                    var list = new List<string> { "None" };
                    list.AddRange(currentGraph.actors.Select(a => a.name));
                    return list;
                },
                emptyLabel: "None",
                getCurrentValue: () =>
                {
                    if (string.IsNullOrEmpty(Data.actorGuid))
                        return "None";

                    var actor = currentGraph.actors.FirstOrDefault(a => a.guid == Data.actorGuid);
                    return actor != null ? actor.name : "None";
                },
                onValueChanged: name =>
                {
                    if (name == "None")
                    {
                        Data.actorGuid = string.Empty;
                        EditorUtility.SetDirty(currentGraph);
                        return;
                    }

                    var actor = currentGraph.actors.FirstOrDefault(a => a.name == name);
                    if (actor != null)
                    {
                        Data.actorGuid = actor.guid;
                        EditorUtility.SetDirty(currentGraph);
                    }   
                }
            );
            extensionContainer.Add(actorDropdown);
        }

        // ── Quest dropdown ────────────────────────────────────────────────────

        private void BuildQuestDropdown()
        {
            var questDropdown = BuildDynamicDropdown(
                label: "Quest",
                getChoices: () =>
                {
                    var list = new List<string> { "None" };
                    list.AddRange(currentGraph.quests.Select(q => q.title));
                    return list;
                },
                emptyLabel: "None",
                getCurrentValue: () =>
                {
                    if (!string.IsNullOrEmpty(Data.questGuid))
                    {
                        var quest = currentGraph.quests.FirstOrDefault(q => q.guid == Data.questGuid);
                        if (quest != null) return quest.title;
                    }
                    return "None";
                },
                onValueChanged: name =>
                {
                    if (name == "None")
                        Data.questGuid = string.Empty;
                    else
                    {
                        var quest = currentGraph.quests.FirstOrDefault(q => q.title == name);
                        if (quest != null) Data.questGuid = quest.guid;
                    }
                    EditorUtility.SetDirty(currentGraph);
                }
            );
            extensionContainer.Add(questDropdown);
        }

        // ── Generic live dropdown ─────────────────────────────────────────────

        private PopupField<string> BuildDynamicDropdown(
            string label,
            System.Func<List<string>> getChoices,
            string emptyLabel,
            System.Func<string> getCurrentValue,
            System.Action<string> onValueChanged)
        {
            var choices = getChoices();
            if (choices.Count == 0) choices.Add(emptyLabel);

            var current = getCurrentValue();
            if (!choices.Contains(current)) current = choices[0];

            var field = new PopupField<string>(label, choices, current);

            field.RegisterCallback<FocusInEvent>(_ =>
            {
                var fresh = getChoices();
                if (fresh.Count == 0) fresh.Add(emptyLabel);
                field.choices.Clear();
                foreach (var c in fresh) field.choices.Add(c);
                if (!field.choices.Contains(field.value))
                    field.SetValueWithoutNotify(field.choices[0]);
            });

            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            return field;
        }

        // ── Conditions foldout ────────────────────────────────────────────────

        private void BuildConditionsFoldout()
        {
            var foldout = new Foldout { text = "Conditions", value = true };
            var conditionsContainer = new VisualElement();
            foldout.Add(conditionsContainer);

            if (Data.conditions == null)
                Data.conditions = new List<NodeConditionData>();

            foreach (var condition in Data.conditions)
                AddConditionRow(conditionsContainer, condition);

            foldout.Add(new Button(() =>
            {
                if (currentGraph.conditions.Count == 0) return;
                var newCondition = new NodeConditionData
                {
                    conditionGuid = currentGraph.conditions[0].guid,
                    requiredValue = false
                };
                Data.conditions.Add(newCondition);
                AddConditionRow(conditionsContainer, newCondition);
                EditorUtility.SetDirty(currentGraph);
            })
            { text = "+ Add Condition" });

            extensionContainer.Add(foldout);
        }

        private void AddConditionRow(VisualElement container, NodeConditionData data)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var dropdown = BuildDynamicDropdown(
                label: string.Empty,
                getChoices: () => currentGraph.conditions.Select(c => c.name).ToList(),
                emptyLabel: "No conditions",
                getCurrentValue: () =>
                {
                    var c = currentGraph.conditions.FirstOrDefault(c => c.guid == data.conditionGuid);
                    return c?.name ?? "No conditions";
                },
                onValueChanged: name =>
                {
                    var selected = currentGraph.conditions.FirstOrDefault(c => c.name == name);
                    if (selected != null)
                    {
                        data.conditionGuid = selected.guid;
                        EditorUtility.SetDirty(currentGraph);
                    }
                }
            );
            dropdown.style.flexGrow = 1;

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
            { text = "X" };

            row.Add(dropdown);
            row.Add(toggle);
            row.Add(removeButton);
            container.Add(row);
        }

        // ── Quest requirements foldout ───────────────────────────────────────

        private void BuildQuestRequirementsFoldout()
        {
            if (Data.questRequirements == null)
                Data.questRequirements = new List<NodeQuestRequirement>();

            var foldout = new Foldout { text = "Quest requirements", value = false };
            var container = new VisualElement();
            foldout.Add(container);

            foreach (var req in Data.questRequirements)
                AddQuestRequirementRow(container, req);

            foldout.Add(new Button(() =>
            {
                if (currentGraph.quests.Count == 0) return;
                var req = new NodeQuestRequirement
                {
                    questGuid = currentGraph.quests[0].guid,
                    requiredStatus = QuestStatus.Active
                };
                Data.questRequirements.Add(req);
                AddQuestRequirementRow(container, req);
                EditorUtility.SetDirty(currentGraph);
            })
            { text = "+ Add Quest Requirement" });

            extensionContainer.Add(foldout);
        }

        private void AddQuestRequirementRow(VisualElement container, NodeQuestRequirement req)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            // Quest dropdown
            var questDropdown = BuildDynamicDropdown(
                label: string.Empty,
                getChoices: () => currentGraph.quests.Select(q => q.title).ToList(),
                emptyLabel: "No quests",
                getCurrentValue: () =>
                {
                    var q = currentGraph.quests.FirstOrDefault(q => q.guid == req.questGuid);
                    return q?.title ?? "No quests";
                },
                onValueChanged: name =>
                {
                    var q = currentGraph.quests.FirstOrDefault(q => q.title == name);
                    if (q != null) { req.questGuid = q.guid; EditorUtility.SetDirty(currentGraph); }
                }
            );
            questDropdown.style.flexGrow = 1;

            // Status dropdown
            var statusNames = System.Enum.GetNames(typeof(QuestStatus)).ToList();
            var currentStatus = req.requiredStatus.ToString();
            var statusDropdown = new PopupField<string>(statusNames, currentStatus);
            statusDropdown.style.width = 100;
            statusDropdown.RegisterValueChangedCallback(evt =>
            {
                if (System.Enum.TryParse<QuestStatus>(evt.newValue, out var s))
                {
                    req.requiredStatus = s;
                    EditorUtility.SetDirty(currentGraph);
                }
            });

            var removeBtn = new Button(() =>
            {
                Data.questRequirements.Remove(req);
                container.Remove(row);
                EditorUtility.SetDirty(currentGraph);
            })
            { text = "✕" };

            row.Add(questDropdown);
            row.Add(statusDropdown);
            row.Add(removeBtn);
            container.Add(row);
        }

        // ── Context menu ──────────────────────────────────────────────────────

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            evt.menu.AppendAction("Duplicate node", _ =>
            {
                var graphView = this.GetFirstAncestorOfType<DialoguesView>();
                if (graphView == null) return;

                Vector2 pos = GetPosition().position + new Vector2(30, 30);
                var clone = graphView.CreateNode(pos, Data.title + " (copy)", Data.dialogue);
                if (clone == null) return;

                clone.Data.actorGuid = Data.actorGuid;
                clone.Data.questGuid = Data.questGuid;
                clone.Data.conditions = Data.conditions
                    .Select(c => new NodeConditionData
                    {
                        conditionGuid = c.conditionGuid,
                        requiredValue = c.requiredValue
                    }).ToList();

                // Deep-copy replies (new GUIDs so ports don't clash)
                clone.Data.replies = Data.replies
                    .Select(r => new PlayerReplyData
                    {
                        guid = System.Guid.NewGuid().ToString(),
                        text = r.text
                    }).ToList();

                EditorUtility.SetDirty(currentGraph);
            });
        }
    }
}