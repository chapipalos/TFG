using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        [Tooltip("GraphData del que se leen las definiciones de quests.")]
        public GraphData graphData;

        private readonly Dictionary<string, QuestRuntimeData> quests = new();

        public event Action<string, QuestStatus> OnQuestStatusChanged;
        public event Action<string, string, bool> OnObjectiveChanged;

        // ── Public API ────────────────────────────────────────────────────────

        public void StartQuest(string questGuid)
        {
            EnsureEntry(questGuid);
            var entry = quests[questGuid];
            if (entry.Status != QuestStatus.NotStarted) return;

            entry.Status = QuestStatus.Active;
            Debug.Log($"[QuestManager] Quest iniciada: {GetQuestTitle(questGuid)}");
            NotifyStatusChanged(questGuid, entry.Status);
        }

        public void SetObjectiveCompleted(string questGuid, string objectiveGuid, bool completed = true)
        {
            EnsureEntry(questGuid);
            var entry = quests[questGuid];
            if (entry.Status == QuestStatus.NotStarted ||
                entry.Status == QuestStatus.Completed ||
                entry.Status == QuestStatus.Failed) return;

            entry.ObjectiveCompleted[objectiveGuid] = completed;

            if (entry.Status == QuestStatus.Active)
            {
                entry.Status = QuestStatus.InProgress;
                NotifyStatusChanged(questGuid, entry.Status);
            }

            OnObjectiveChanged?.Invoke(questGuid, objectiveGuid, completed);

            if (AllObjectivesCompleted(questGuid))
                CompleteQuest(questGuid);
        }

        public void CompleteQuest(string questGuid)
        {
            EnsureEntry(questGuid);
            if (quests[questGuid].Status == QuestStatus.Completed) return;
            quests[questGuid].Status = QuestStatus.Completed;
            Debug.Log($"[QuestManager] Quest completada: {GetQuestTitle(questGuid)}");
            NotifyStatusChanged(questGuid, quests[questGuid].Status);
        }

        public void FailQuest(string questGuid)
        {
            EnsureEntry(questGuid);
            if (quests[questGuid].Status == QuestStatus.Failed) return;
            quests[questGuid].Status = QuestStatus.Failed;
            Debug.Log($"[QuestManager] Quest fallida: {GetQuestTitle(questGuid)}");
            NotifyStatusChanged(questGuid, quests[questGuid].Status);
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public QuestStatus GetStatus(string questGuid)
        {
            EnsureEntry(questGuid);
            return quests[questGuid].Status;
        }

        public bool IsNotStarted(string g) => GetStatus(g) == QuestStatus.NotStarted;
        public bool IsActive(string g) => GetStatus(g) == QuestStatus.Active;
        public bool IsInProgress(string g) => GetStatus(g) == QuestStatus.InProgress;
        public bool IsCompleted(string g) => GetStatus(g) == QuestStatus.Completed;
        public bool IsFailed(string g) => GetStatus(g) == QuestStatus.Failed;

        public (int completed, int total) GetProgress(string questGuid)
        {
            if (graphData == null) return (0, 0);
            var quest = graphData.quests.Find(q => q.guid == questGuid);
            if (quest == null) return (0, 0);
            EnsureEntry(questGuid);
            var entry = quests[questGuid];
            int total = quest.objectives.Count, completed = 0;
            foreach (var obj in quest.objectives)
                if (entry.ObjectiveCompleted.TryGetValue(obj.guid, out bool v) && v) completed++;
            return (completed, total);
        }

        public IEnumerable<(QuestData data, QuestRuntimeData runtime)> GetAllQuests()
        {
            if (graphData == null) yield break;
            foreach (var q in graphData.quests)
            {
                EnsureEntry(q.guid);
                yield return (q, quests[q.guid]);
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void EnsureEntry(string questGuid)
        {
            if (!quests.ContainsKey(questGuid))
                quests[questGuid] = new QuestRuntimeData();
        }

        private bool AllObjectivesCompleted(string questGuid)
        {
            if (graphData == null) return false;
            var quest = graphData.quests.Find(q => q.guid == questGuid);
            if (quest == null || quest.objectives.Count == 0) return false;
            var entry = quests[questGuid];
            foreach (var obj in quest.objectives)
                if (!entry.ObjectiveCompleted.TryGetValue(obj.guid, out bool v) || !v) return false;
            return true;
        }

        private string GetQuestTitle(string questGuid)
        {
            var q = graphData?.quests.Find(q => q.guid == questGuid);
            return q != null ? q.title : questGuid;
        }

        /// <summary>
        /// FIX PRINCIPAL: cuando una quest cambia de estado, actualiza
        /// automáticamente las condiciones booleanas en ConditionManager.
        ///
        /// Convención de nombres de condición (creadas por el generador):
        ///   Quest_{guid}_NotStarted
        ///   Quest_{guid}_Active
        ///   Quest_{guid}_InProgress
        ///   Quest_{guid}_Completed
        ///   Quest_{guid}_Failed
        ///
        /// El ConditionManager las busca por nombre y actualiza su valor.
        /// Así el diálogo siempre refleja el estado real de la quest.
        /// </summary>
        private void NotifyStatusChanged(string questGuid, QuestStatus status)
        {
            OnQuestStatusChanged?.Invoke(questGuid, status);
        }
    }

    public class QuestRuntimeData
    {
        public QuestStatus Status = QuestStatus.NotStarted;
        public readonly Dictionary<string, bool> ObjectiveCompleted = new();
    }

    public enum QuestStatus
    {
        NotStarted,
        Active,
        InProgress,
        Completed,
        Failed
    }
}