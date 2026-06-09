using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager m_Instance { get; private set; }

        private void Awake()
        {
            if (m_Instance != null && m_Instance != this) { Destroy(gameObject); return; }
            m_Instance = this;
        }
        private void OnDestroy()
        {
            if (m_Instance == this)
            {
                UnsubscribeAll();
                m_Instance = null;
            }
        }

        [Tooltip("GraphData containing the quest definitions.")]
        public GraphData m_GraphData;

        private readonly Dictionary<string, QuestRuntimeData> m_Quests = new();

        public event Action<string, QuestStatus> OnQuestStatusChanged;
        public event Action<string, string, bool> OnObjectiveChanged;

        [Tooltip("Each binding connects an event name to a quest objective. " +
         "When the event is raised, the objective is marked as completed.")]
        public List<EventObjectiveBinding> m_ObjectiveBindings = new();


        public void StartQuest(string questGuid)
        {
            EnsureEntry(questGuid);
            var entry = m_Quests[questGuid];
            if (entry.m_Status != QuestStatus.NotStarted) return;

            entry.m_Status = QuestStatus.Active;
            NotifyStatusChanged(questGuid, entry.m_Status);
        }

        public bool IsObjectiveCompleted(string questGuid, string objectiveGuid)
        {
            EnsureEntry(questGuid);
            return m_Quests[questGuid].m_ObjectiveCompleted.TryGetValue(objectiveGuid, out bool v) && v;
        }

        public void SetObjectiveCompleted(string questGuid, string objectiveGuid, bool completed = true)
        {
            EnsureEntry(questGuid);
            var entry = m_Quests[questGuid];
            if (entry.m_Status == QuestStatus.NotStarted ||
                entry.m_Status == QuestStatus.Completed ||
                entry.m_Status == QuestStatus.Failed) return;

            entry.m_ObjectiveCompleted[objectiveGuid] = completed;

            if (entry.m_Status == QuestStatus.Active)
            {
                entry.m_Status = QuestStatus.InProgress;
                NotifyStatusChanged(questGuid, entry.m_Status);
            }

            OnObjectiveChanged?.Invoke(questGuid, objectiveGuid, completed);

            if (AllObjectivesCompleted(questGuid))
                CompleteQuest(questGuid);
        }

        public void CompleteQuest(string questGuid)
        {
            EnsureEntry(questGuid);
            if (m_Quests[questGuid].m_Status == QuestStatus.Completed) return;

            if (m_GraphData != null)
            {
                var quest = m_GraphData.s_Quests.Find(q => q.s_QGuid == questGuid);
                if (quest != null)
                {
                    var entry = m_Quests[questGuid];
                    foreach (var obj in quest.s_QuestObjectives)
                    {
                        entry.m_ObjectiveCompleted[obj.s_OGuid] = true;
                        OnObjectiveChanged?.Invoke(questGuid, obj.s_OGuid, true);
                    }
                }
            }

            m_Quests[questGuid].m_Status = QuestStatus.Completed;
            NotifyStatusChanged(questGuid, m_Quests[questGuid].m_Status);
        }

        public void FailQuest(string questGuid)
        {
            EnsureEntry(questGuid);
            if (m_Quests[questGuid].m_Status == QuestStatus.Failed) return;
            m_Quests[questGuid].m_Status = QuestStatus.Failed;
            NotifyStatusChanged(questGuid, m_Quests[questGuid].m_Status);
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public QuestStatus GetStatus(string questGuid)
        {
            EnsureEntry(questGuid);
            return m_Quests[questGuid].m_Status;
        }

        public bool IsNotStarted(string g) => GetStatus(g) == QuestStatus.NotStarted;
        public bool IsActive(string g) => GetStatus(g) == QuestStatus.Active;
        public bool IsInProgress(string g) => GetStatus(g) == QuestStatus.InProgress;
        public bool IsCompleted(string g) => GetStatus(g) == QuestStatus.Completed;
        public bool IsFailed(string g) => GetStatus(g) == QuestStatus.Failed;

        public (int completed, int total) GetProgress(string questGuid)
        {
            if (m_GraphData == null) return (0, 0);
            var quest = m_GraphData.s_Quests.Find(q => q.s_QGuid == questGuid);
            if (quest == null) return (0, 0);
            EnsureEntry(questGuid);
            var entry = m_Quests[questGuid];
            int total = quest.s_QuestObjectives.Count, completed = 0;
            foreach (var obj in quest.s_QuestObjectives)
                if (entry.m_ObjectiveCompleted.TryGetValue(obj.s_OGuid, out bool v) && v) completed++;
            return (completed, total);
        }

        public IEnumerable<(QuestData data, QuestRuntimeData runtime)> GetAllQuests()
        {
            if (m_GraphData == null) yield break;
            foreach (var q in m_GraphData.s_Quests)
            {
                EnsureEntry(q.s_QGuid);
                yield return (q, m_Quests[q.s_QGuid]);
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void EnsureEntry(string questGuid)
        {
            if (!m_Quests.ContainsKey(questGuid))
                m_Quests[questGuid] = new QuestRuntimeData();
        }

        private bool AllObjectivesCompleted(string questGuid)
        {
            if (m_GraphData == null) return false;
            var quest = m_GraphData.s_Quests.Find(q => q.s_QGuid == questGuid);
            if (quest == null || quest.s_QuestObjectives.Count == 0) return false;
            var entry = m_Quests[questGuid];
            foreach (var obj in quest.s_QuestObjectives)
                if (!entry.m_ObjectiveCompleted.TryGetValue(obj.s_OGuid, out bool v) || !v) return false;
            return true;
        }

        private string GetQuestTitle(string questGuid)
        {
            var q = m_GraphData?.s_Quests.Find(q => q.s_QGuid == questGuid);
            return q != null ? q.s_QuestTitle : questGuid;
        }

        private void NotifyStatusChanged(string questGuid, QuestStatus status)
        {
            OnQuestStatusChanged?.Invoke(questGuid, status);
        }

        private void OnEnable() => SubscribeAll();
        private void OnDisable() => UnsubscribeAll();

        private void SubscribeAll()
        {
            foreach (var b in m_ObjectiveBindings)
            {
                if (string.IsNullOrEmpty(b.m_EventName)) continue;
                if (string.IsNullOrEmpty(b.m_QuestGuid)) continue;
                if (string.IsNullOrEmpty(b.m_ObjectiveGuid)) continue;

                var captured = b;
                b.m_Callback = () =>
                {
                    if (GetStatus(captured.m_QuestGuid) == QuestStatus.NotStarted)
                    {
                        Debug.Log($"[QuestManager] Event '{captured.m_EventName}' ignored — quest not started.");
                        return;
                    }

                    SetObjectiveCompleted(captured.m_QuestGuid, captured.m_ObjectiveGuid, true);
                };
                GameEventBus.Subscribe(b.m_EventName, b.m_Callback);
            }
        }

        private void UnsubscribeAll()
        {
            foreach (var b in m_ObjectiveBindings)
            {
                if (string.IsNullOrEmpty(b.m_EventName) || b.m_Callback == null) continue;
                GameEventBus.Unsubscribe(b.m_EventName, b.m_Callback);
                b.m_Callback = null;
            }
        }
    }

    public class QuestRuntimeData
    {
        public QuestStatus m_Status = QuestStatus.NotStarted;
        public readonly Dictionary<string, bool> m_ObjectiveCompleted = new();
    }

    public enum QuestStatus
    {
        NotStarted,
        Active,
        InProgress,
        Completed,
        Failed
    }

    [System.Serializable]
    public class EventObjectiveBinding
    {
        [Tooltip("Event name. Must match GameEventBus.Raise(...).")]
        public string m_EventName;

        [Tooltip("GUID of the quest to which the objective belongs.")]
        public string m_QuestGuid;

        [Tooltip("GUID of the objective that will be completed when the event is triggered.")]
        public string m_ObjectiveGuid;

        [System.NonSerialized]
        public System.Action m_Callback;
    }
}