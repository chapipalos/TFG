using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    public List<NodeConditionData> conditions = new();
    public List<NodeQuestActionData> questActions = new();
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