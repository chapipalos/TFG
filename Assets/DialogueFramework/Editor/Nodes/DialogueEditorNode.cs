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
        public NodeData m_Data { get; }

        public Port m_InputPort;
        public Port m_OutputPort;

        private readonly Dictionary<string, Port> m_ReplyPorts = new();
        private GraphData m_CurrentGraph;

        public DialogueEditorNode(Vector2 position, NodeData data, GraphData graph)
        {
            m_Data = data;
            m_CurrentGraph = graph;

            title = data.s_NodeTitle;
            style.width = 260;

            // ── Input port ────────────────────────────────────────────────────
            m_InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            m_InputPort.portName = "In";
            inputContainer.Add(m_InputPort);

            // ── Generic output port (hidden when replies exist) ────────────────
            m_OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            m_OutputPort.portName = "Out";
            outputContainer.Add(m_OutputPort);

            // ── Fields ────────────────────────────────────────────────────────
            var titleField = new TextField("Name");
            titleField.SetValueWithoutNotify(data.s_NodeTitle);
            titleField.RegisterValueChangedCallback(evt =>
            {
                m_Data.s_NodeTitle = evt.newValue;
                title = evt.newValue;
                EditorUtility.SetDirty(m_CurrentGraph);
            });
            extensionContainer.Add(titleField);

            var dialogueField = new TextField("Dialogue");
            dialogueField.multiline = true;
            dialogueField.SetValueWithoutNotify(data.s_Dialogue);
            dialogueField.RegisterValueChangedCallback(evt =>
            {
                m_Data.s_Dialogue = evt.newValue;
                EditorUtility.SetDirty(m_CurrentGraph);
            });
            extensionContainer.Add(dialogueField);

            BuildActorDropdown();
            BuildObjectiveRequirementsFoldout();
            BuildQuestRequirementsFoldout();
            BuildRepliesFoldout();
            BuildEffectsFoldout();

            RefreshExpandedState();
            RefreshPorts();

            SetPosition(new Rect(position, new Vector2(260, 150)));
        }

        // ── Reply ports (public API for SaveUtility) ──────────────────────────

        public Port GetReplyPort(string replyGuid)
            => m_ReplyPorts.TryGetValue(replyGuid, out var port) ? port : null;

        public IEnumerable<(string replyGuid, Port port)> GetAllReplyPorts()
            => m_ReplyPorts.Select(kv => (kv.Key, kv.Value));

        // ── Replies foldout ───────────────────────────────────────────────────

        private void BuildRepliesFoldout()
        {
            if (m_Data.s_Replies == null)
                m_Data.s_Replies = new List<PlayerReplyData>();

            var foldout = new Foldout { text = "Player replies", value = true };

            foreach (var reply in m_Data.s_Replies)
                AddReplyRow(foldout, reply);

            var addButton = new Button(() =>
            {
                var reply = new PlayerReplyData
                {
                    s_RGuid = System.Guid.NewGuid().ToString(),
                    s_ReplyText = "New reply"
                };
                m_Data.s_Replies.Add(reply);
                AddReplyRow(foldout, reply);
                EditorUtility.SetDirty(m_CurrentGraph);
                UpdateGenericPortVisibility();
            })
            { text = "+ Add Reply" };

            foldout.Add(addButton);
            extensionContainer.Add(foldout);

            UpdateGenericPortVisibility();
        }

        private void AddReplyRow(VisualElement container, PlayerReplyData reply)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            port.portName = reply.s_ReplyText;
            outputContainer.Add(port);
            m_ReplyPorts[reply.s_RGuid] = port;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var textField = new TextField();
            textField.style.flexGrow = 1;
            textField.SetValueWithoutNotify(reply.s_ReplyText);
            textField.RegisterValueChangedCallback(evt =>
            {
                reply.s_ReplyText = evt.newValue;
                port.portName = evt.newValue;
                EditorUtility.SetDirty(m_CurrentGraph);
            });

            var removeButton = new Button(() =>
            {
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
                m_ReplyPorts.Remove(reply.s_RGuid);
                m_Data.s_Replies.Remove(reply);
                container.Remove(row);

                EditorUtility.SetDirty(m_CurrentGraph);
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

        // ── Node effects foldout ─────────────────────────────────────────────

        private void BuildEffectsFoldout()
        {
            if (m_Data.s_Effects == null)
                m_Data.s_Effects = new List<NodeEffectData>();

            var foldout = new Foldout { text = "Node Effects", value = false };
            var container = new VisualElement();
            foldout.Add(container);

            foreach (var effect in m_Data.s_Effects)
                AddEffectRow(container, effect);

            foldout.Add(new Button(() =>
            {
                var effect = new NodeEffectData { _EffectType = NodeEffectType.QuestStart };
                AutoAssignTargetGuid(effect);

                m_Data.s_Effects.Add(effect);
                AddEffectRow(container, effect);
                EditorUtility.SetDirty(m_CurrentGraph);
            })
            { text = "+ Add Effect" });

            extensionContainer.Add(foldout);
        }

        private void AddEffectRow(VisualElement container, NodeEffectData effect)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Column;
            row.style.marginBottom = 4;

            var typeNames = System.Enum.GetNames(typeof(NodeEffectType)).ToList();
            var typeDropdown = new PopupField<string>("Type", typeNames, effect._EffectType.ToString());

            var targetRow = new VisualElement();
            targetRow.style.flexDirection = FlexDirection.Column;

            System.Action rebuildTargetDropdown = null;
            rebuildTargetDropdown = () =>
            {
                targetRow.Clear();

                bool isObjective = effect._EffectType == NodeEffectType.ObjectiveComplete;

                var questDropdown = BuildDynamicDropdown(
                    label: "Quest",
                    getChoices: () => m_CurrentGraph.s_Quests.Select(q => q.s_QuestTitle).ToList(),
                    emptyLabel: "No quests",
                    getCurrentValue: () =>
                    {
                        var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QGuid == effect.s_QuestGuid);
                        return q?.s_QuestTitle ?? (m_CurrentGraph.s_Quests.Count > 0 ? m_CurrentGraph.s_Quests[0].s_QuestTitle : "No quests");
                    },
                    onValueChanged: name =>
                    {
                        var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QuestTitle == name);
                        if (q != null)
                        {
                            effect.s_QuestGuid = q.s_QGuid;
                            if (isObjective)
                                effect.s_ObjectiveGuid = q.s_QuestObjectives.Count > 0 ? q.s_QuestObjectives[0].s_OGuid : "";
                            EditorUtility.SetDirty(m_CurrentGraph);
                            rebuildTargetDropdown();
                        }
                    }
                );
                questDropdown.style.flexGrow = 1;
                targetRow.Add(questDropdown);

                if (isObjective)
                {
                    var quest = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QGuid == effect.s_QuestGuid);
                    if (quest != null && quest.s_QuestObjectives.Count > 0)
                    {
                        var objectiveDropdown = BuildDynamicDropdown(
                            label: "Objective",
                            getChoices: () => quest.s_QuestObjectives.Select(o => o.s_ObjectiveDescription).ToList(),
                            emptyLabel: "No objectives",
                            getCurrentValue: () =>
                            {
                                var o = quest.s_QuestObjectives.FirstOrDefault(o => o.s_OGuid == effect.s_ObjectiveGuid);
                                return o?.s_ObjectiveDescription ?? quest.s_QuestObjectives[0].s_ObjectiveDescription;
                            },
                            onValueChanged: name =>
                            {
                                var o = quest.s_QuestObjectives.FirstOrDefault(o => o.s_ObjectiveDescription == name);
                                if (o != null)
                                {
                                    effect.s_ObjectiveGuid = o.s_OGuid;
                                    EditorUtility.SetDirty(m_CurrentGraph);
                                }
                            }
                        );
                        objectiveDropdown.style.flexGrow = 1;
                        targetRow.Add(objectiveDropdown);
                    }
                }
            };

            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (System.Enum.TryParse<NodeEffectType>(evt.newValue, out var t))
                {
                    effect._EffectType = t;
                    AutoAssignTargetGuid(effect);
                    EditorUtility.SetDirty(m_CurrentGraph);
                    rebuildTargetDropdown();
                }
            });

            var removeBtn = new Button(() =>
            {
                m_Data.s_Effects.Remove(effect);
                container.Remove(row);
                EditorUtility.SetDirty(m_CurrentGraph);
            })
            { text = "✕ Remove effect" };

            row.Add(typeDropdown);
            row.Add(targetRow);
            row.Add(removeBtn);
            container.Add(row);

            rebuildTargetDropdown();
        }

        private void AutoAssignTargetGuid(NodeEffectData effect)
        {
            if (string.IsNullOrEmpty(effect.s_QuestGuid) && m_CurrentGraph.s_Quests.Count > 0)
                effect.s_QuestGuid = m_CurrentGraph.s_Quests[0].s_QGuid;

            if (effect._EffectType == NodeEffectType.ObjectiveComplete && string.IsNullOrEmpty(effect.s_ObjectiveGuid))
            {
                var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QGuid == effect.s_QuestGuid);
                if (q != null && q.s_QuestObjectives.Count > 0)
                    effect.s_ObjectiveGuid = q.s_QuestObjectives[0].s_OGuid;
            }
        }

        private void UpdateGenericPortVisibility()
        {
            bool hasReplies = m_Data.s_Replies != null && m_Data.s_Replies.Count > 0;
            m_OutputPort.style.display = hasReplies ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ── Actor dropdown ────────────────────────────────────────────────────

        private void BuildActorDropdown()
        {
            var actorDropdown = BuildDynamicDropdown(
                label: "Actor",
                getChoices: () =>
                {
                    var list = new List<string> { "None" };
                    list.AddRange(m_CurrentGraph.s_Actors.Select(a => a.s_ActorName));
                    return list;
                },
                emptyLabel: "None",
                getCurrentValue: () =>
                {
                    if (string.IsNullOrEmpty(m_Data.s_ActorGuid))
                        return "None";

                    var actor = m_CurrentGraph.s_Actors.FirstOrDefault(a => a.s_AGuid == m_Data.s_ActorGuid);
                    return actor != null ? actor.s_ActorName : "None";
                },
                onValueChanged: name =>
                {
                    if (name == "None")
                    {
                        m_Data.s_ActorGuid = string.Empty;
                        EditorUtility.SetDirty(m_CurrentGraph);
                        return;
                    }

                    var actor = m_CurrentGraph.s_Actors.FirstOrDefault(a => a.s_ActorName == name);
                    if (actor != null)
                    {
                        m_Data.s_ActorGuid = actor.s_AGuid;
                        EditorUtility.SetDirty(m_CurrentGraph);
                    }
                }
            );
            extensionContainer.Add(actorDropdown);
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

        // ── Quest requirements foldout ───────────────────────────────────────

        private void BuildQuestRequirementsFoldout()
        {
            if (m_Data.s_QuestRequirements == null)
                m_Data.s_QuestRequirements = new List<NodeQuestRequirement>();

            var foldout = new Foldout { text = "Quest requirements", value = false };
            var container = new VisualElement();
            foldout.Add(container);

            foreach (var req in m_Data.s_QuestRequirements)
                AddQuestRequirementRow(container, req);

            foldout.Add(new Button(() =>
            {
                if (m_CurrentGraph.s_Quests.Count == 0) return;
                var req = new NodeQuestRequirement
                {
                    s_QuestGuid = m_CurrentGraph.s_Quests[0].s_QGuid,
                    s_RequiredStatus = QuestStatus.Active
                };
                m_Data.s_QuestRequirements.Add(req);
                AddQuestRequirementRow(container, req);
                EditorUtility.SetDirty(m_CurrentGraph);
            })
            { text = "+ Add Quest Requirement" });

            extensionContainer.Add(foldout);
        }

        private void AddQuestRequirementRow(VisualElement container, NodeQuestRequirement req)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var questDropdown = BuildDynamicDropdown(
                label: string.Empty,
                getChoices: () => m_CurrentGraph.s_Quests.Select(q => q.s_QuestTitle).ToList(),
                emptyLabel: "No quests",
                getCurrentValue: () =>
                {
                    var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QGuid == req.s_QuestGuid);
                    return q?.s_QuestTitle ?? "No quests";
                },
                onValueChanged: name =>
                {
                    var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QuestTitle == name);
                    if (q != null) { req.s_QuestGuid = q.s_QGuid; EditorUtility.SetDirty(m_CurrentGraph); }
                }
            );
            questDropdown.style.flexGrow = 1;

            var statusNames = System.Enum.GetNames(typeof(QuestStatus)).ToList();
            var currentStatus = req.s_RequiredStatus.ToString();
            var statusDropdown = new PopupField<string>(statusNames, currentStatus);
            statusDropdown.style.width = 100;
            statusDropdown.RegisterValueChangedCallback(evt =>
            {
                if (System.Enum.TryParse<QuestStatus>(evt.newValue, out var s))
                {
                    req.s_RequiredStatus = s;
                    EditorUtility.SetDirty(m_CurrentGraph);
                }
            });

            var removeBtn = new Button(() =>
            {
                m_Data.s_QuestRequirements.Remove(req);
                container.Remove(row);
                EditorUtility.SetDirty(m_CurrentGraph);
            })
            { text = "✕" };

            row.Add(questDropdown);
            row.Add(statusDropdown);
            row.Add(removeBtn);
            container.Add(row);
        }

        // ── Objective requirements foldout ────────────────────────────────────

        private void BuildObjectiveRequirementsFoldout()
        {
            if (m_Data.s_ObjectiveRequirements == null)
                m_Data.s_ObjectiveRequirements = new List<NodeObjectiveRequirement>();

            var foldout = new Foldout { text = "Objective requirements", value = true };
            var container = new VisualElement();
            foldout.Add(container);

            foreach (var req in m_Data.s_ObjectiveRequirements)
                AddObjectiveRequirementRow(container, req);

            foldout.Add(new Button(() =>
            {
                if (m_CurrentGraph.s_Quests.Count == 0) return;

                var firstQuest = m_CurrentGraph.s_Quests[0];
                if (firstQuest.s_QuestObjectives.Count == 0) return;

                var req = new NodeObjectiveRequirement
                {
                    s_QuestGuid = firstQuest.s_QGuid,
                    s_ObjectiveGuid = firstQuest.s_QuestObjectives[0].s_OGuid,
                    s_MustBeCompleted = true
                };
                m_Data.s_ObjectiveRequirements.Add(req);
                AddObjectiveRequirementRow(container, req);
                EditorUtility.SetDirty(m_CurrentGraph);
            })
            { text = "+ Add Objective Requirement" });

            extensionContainer.Add(foldout);
        }

        private void AddObjectiveRequirementRow(VisualElement container, NodeObjectiveRequirement req)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Column;
            row.style.marginBottom = 4;

            System.Action rebuildObjectiveDropdown = null;
            var objectiveDropdownContainer = new VisualElement();
            objectiveDropdownContainer.style.flexGrow = 1;

            var questDropdown = BuildDynamicDropdown(
                label: "Quest",
                getChoices: () => m_CurrentGraph.s_Quests.Select(q => q.s_QuestTitle).ToList(),
                emptyLabel: "No quests",
                getCurrentValue: () =>
                {
                    var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QGuid == req.s_QuestGuid);
                    return q?.s_QuestTitle ?? (m_CurrentGraph.s_Quests.Count > 0 ? m_CurrentGraph.s_Quests[0].s_QuestTitle : "No quests");
                },
                onValueChanged: name =>
                {
                    var q = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QuestTitle == name);
                    if (q != null)
                    {
                        req.s_QuestGuid = q.s_QGuid;
                        req.s_ObjectiveGuid = q.s_QuestObjectives.Count > 0 ? q.s_QuestObjectives[0].s_OGuid : "";
                        EditorUtility.SetDirty(m_CurrentGraph);
                        rebuildObjectiveDropdown?.Invoke();
                    }
                }
            );

            var objectiveRow = new VisualElement();
            objectiveRow.style.flexDirection = FlexDirection.Row;
            objectiveRow.style.alignItems = Align.Center;

            rebuildObjectiveDropdown = () =>
            {
                objectiveDropdownContainer.Clear();

                var quest = m_CurrentGraph.s_Quests.FirstOrDefault(q => q.s_QGuid == req.s_QuestGuid);
                if (quest == null || quest.s_QuestObjectives.Count == 0)
                {
                    objectiveDropdownContainer.Add(new Label("No objectives"));
                    return;
                }

                var objectiveDropdown = BuildDynamicDropdown(
                    label: "Objective",
                    getChoices: () => quest.s_QuestObjectives.Select(o => o.s_ObjectiveDescription).ToList(),
                    emptyLabel: "No objectives",
                    getCurrentValue: () =>
                    {
                        var o = quest.s_QuestObjectives.FirstOrDefault(o => o.s_OGuid == req.s_ObjectiveGuid);
                        return o?.s_ObjectiveDescription ?? quest.s_QuestObjectives[0].s_ObjectiveDescription;
                    },
                    onValueChanged: name =>
                    {
                        var o = quest.s_QuestObjectives.FirstOrDefault(o => o.s_ObjectiveDescription == name);
                        if (o != null)
                        {
                            req.s_ObjectiveGuid = o.s_OGuid;
                            EditorUtility.SetDirty(m_CurrentGraph);
                        }
                    }
                );
                objectiveDropdown.style.flexGrow = 1;
                objectiveDropdownContainer.Add(objectiveDropdown);
            };

            objectiveRow.Add(objectiveDropdownContainer);

            var bottomRow = new VisualElement();
            bottomRow.style.flexDirection = FlexDirection.Row;
            bottomRow.style.alignItems = Align.Center;
            bottomRow.style.justifyContent = Justify.SpaceBetween;

            var toggle = new Toggle("Must be completed");
            toggle.value = req.s_MustBeCompleted;
            toggle.RegisterValueChangedCallback(evt =>
            {
                req.s_MustBeCompleted = evt.newValue;
                EditorUtility.SetDirty(m_CurrentGraph);
            });
            toggle.style.flexGrow = 1;

            var removeBtn = new Button(() =>
            {
                m_Data.s_ObjectiveRequirements.Remove(req);
                container.Remove(row);
                EditorUtility.SetDirty(m_CurrentGraph);
            })
            { text = "✕" };
            removeBtn.style.flexShrink = 0;
            removeBtn.style.width = 24;

            bottomRow.Add(toggle);
            bottomRow.Add(removeBtn);

            row.Add(questDropdown);
            row.Add(objectiveRow);
            row.Add(bottomRow);
            container.Add(row);

            rebuildObjectiveDropdown();
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
                var clone = graphView.CreateNode(pos, m_Data.s_NodeTitle + " (copy)", m_Data.s_Dialogue);
                if (clone == null) return;

                clone.m_Data.s_ActorGuid = m_Data.s_ActorGuid;

                clone.m_Data.s_QuestRequirements = m_Data.s_QuestRequirements
                    .Select(r => new NodeQuestRequirement
                    {
                        s_QuestGuid = r.s_QuestGuid,
                        s_RequiredStatus = r.s_RequiredStatus
                    }).ToList();

                clone.m_Data.s_ObjectiveRequirements = m_Data.s_ObjectiveRequirements
                    .Select(r => new NodeObjectiveRequirement
                    {
                        s_QuestGuid = r.s_QuestGuid,
                        s_ObjectiveGuid = r.s_ObjectiveGuid,
                        s_MustBeCompleted = r.s_MustBeCompleted
                    }).ToList();

                clone.m_Data.s_Effects = m_Data.s_Effects
                    .Select(e => new NodeEffectData
                    {
                        _EffectType = e._EffectType,
                        s_QuestGuid = e.s_QuestGuid,
                        s_ObjectiveGuid = e.s_ObjectiveGuid
                    }).ToList();

                clone.m_Data.s_Replies = m_Data.s_Replies
                    .Select(r => new PlayerReplyData
                    {
                        s_RGuid = System.Guid.NewGuid().ToString(),
                        s_ReplyText = r.s_ReplyText
                    }).ToList();

                EditorUtility.SetDirty(m_CurrentGraph);
            });
        }
    }
}