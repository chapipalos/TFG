using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    [CreateAssetMenu(fileName = "NodeGraph", menuName = "Tools/Node Graph")]
    public class GraphData : ScriptableObject
    {
        public List<ActorData> actors = new();
        public List<ConditionData> conditions = new();
        public List<QuestData> quests = new();
        public List<NodeData> nodes = new();
        public List<NodeLinkData> links = new();
    }

    [Serializable]
    public class ActorData
    {
        public string guid;
        public string name;
    }

    [Serializable]
    public class ConditionData
    {
        public string guid;
        public string name;
        public bool value;
    }

    [Serializable]
    public class NodeData
    {
        public string guid;
        public string title;
        public string dialogue;
        public Vector2 position;
        public string actorGuid;
        public string questGuid;

        public List<NodeConditionData> conditions = new();
        public List<NodeQuestActionData> questActions = new();
        public List<PlayerReplyData> replies = new();
        public List<NodeEffectData> effects = new();

        public List<NodeQuestRequirement> questRequirements = new();
    }

    [Serializable]
    public class PlayerReplyData
    {
        public string guid;
        public string text;
    }

    [Serializable]
    public class NodeConditionData
    {
        public string conditionGuid;
        public bool requiredValue;
    }

    /// <summary>
    /// Requisito de estado de quest para un nodo.
    /// El nodo solo es válido si la quest indicada está en el estado indicado.
    /// </summary>
    [Serializable]
    public class NodeQuestRequirement
    {
        public string questGuid;
        public QuestStatus requiredStatus;
    }

    [Serializable]
    public class NodeLinkData
    {
        public string outputNodeGuid;
        public string inputNodeGuid;
        public string outputPortGuid;
    }

    [Serializable]
    public class QuestData
    {
        public string guid;
        public string title;
        public string description;
        public List<QuestObjectiveData> objectives = new();
    }

    [Serializable]
    public class QuestObjectiveData
    {
        public string guid;
        public string description;
        public bool requiredCompletedState;
    }

    [Serializable]
    public class NodeQuestActionData
    {
        public string questGuid;
        public QuestActionType action;
    }

    public enum QuestActionType
    {
        Start,
        Complete,
        Fail
    }

    [Serializable]
    public class NodeEffectData
    {
        public NodeEffectType type;
        public string questGuid;
        public string conditionGuid;
        public string objectiveGuid;   // ← NUEVO, usado solo si type es ObjectiveComplete
    }

    public enum NodeEffectType
    {
        QuestStart,
        QuestComplete,
        QuestFail,
        ObjectiveComplete,    // ← NUEVO

        ConditionSetTrue,
        ConditionSetFalse,
        ConditionToggle
    }
}