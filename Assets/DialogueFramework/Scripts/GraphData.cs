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

        // NEW: player reply options — each gets its own output port in the editor
        public List<PlayerReplyData> replies = new();
    }

    [Serializable]
    public class PlayerReplyData
    {
        /// <summary>Stable identifier used to match ports across save/load.</summary>
        public string guid;

        /// <summary>Text shown to the player as a dialogue choice.</summary>
        public string text;
    }

    [Serializable]
    public class NodeConditionData
    {
        public string conditionGuid;
        public bool requiredValue;
    }

    [Serializable]
    public class NodeLinkData
    {
        public string outputNodeGuid;
        public string inputNodeGuid;

        // NEW: GUID of the PlayerReplyData whose port generated this link.
        // Empty string means the link comes from the generic OutputPort (no replies).
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
}