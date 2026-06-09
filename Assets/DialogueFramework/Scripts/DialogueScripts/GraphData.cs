using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    [CreateAssetMenu(fileName = "NodeGraph", menuName = "Tools/Node Graph")]
    public class GraphData : ScriptableObject
    {
        public List<ActorData> s_Actors = new();
        public List<QuestData> s_Quests = new();
        public List<NodeData> s_Nodes = new();
        public List<NodeLinkData> s_Links = new();

        public List<ConversationData> s_Conversations = new();
    }

    [Serializable]
    public class ConversationData
    {
        public string s_CGuid;
        public string s_CName;
    }

    [Serializable]
    public class ActorData
    {
        public string s_AGuid;
        public string s_ActorName;
    }

    [Serializable]
    public class NodeData
    {
        public string s_ConversationGuid;

        public string s_NGuid;
        public string s_NodeTitle;
        public string s_Dialogue;
        public Vector2 s_NodePosition;
        public string s_ActorGuid;
        public string s_QuestGuid;

        public List<NodeObjectiveRequirement> s_ObjectiveRequirements = new();
        public List<NodeQuestActionData> s_QuestActions = new();
        public List<PlayerReplyData> s_Replies = new();
        public List<NodeEffectData> s_Effects = new();

        public List<NodeQuestRequirement> s_QuestRequirements = new();
    }

    [Serializable]
    public class PlayerReplyData
    {
        public string s_RGuid;
        public string s_ReplyText;
    }

    [Serializable]
    public class NodeQuestRequirement
    {
        public string s_QuestGuid;
        public QuestStatus s_RequiredStatus;
    }

    [Serializable]
    public class NodeObjectiveRequirement
    {
        public string s_QuestGuid;
        public string s_ObjectiveGuid;
        public bool s_MustBeCompleted;
    }

    [Serializable]
    public class NodeLinkData
    {
        public string s_OutputNodeGuid;
        public string s_InputNodeGuid;
        public string s_OutputPortGuid;
    }

    [Serializable]
    public class QuestData
    {
        public string s_QGuid;
        public string s_QuestTitle;
        public string s_QuestDescription;
        public List<QuestObjectiveData> s_QuestObjectives = new();
    }

    [Serializable]
    public class QuestObjectiveData
    {
        public string s_OGuid;
        public string s_ObjectiveDescription;
        public bool s_RequiredCompletedState;
    }

    [Serializable]
    public class NodeQuestActionData
    {
        public string s_QuestGuid;
        public QuestActionType s_Action;
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
        public NodeEffectType _EffectType;
        public string s_QuestGuid;
        public string s_ObjectiveGuid;
    }

    public enum NodeEffectType
    {
        QuestStart,
        QuestComplete,
        QuestFail,
        ObjectiveComplete
    }
}