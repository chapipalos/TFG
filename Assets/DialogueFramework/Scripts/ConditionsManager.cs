using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    public class ConditionManager : MonoBehaviour
    {
        public static ConditionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitFromGraph();
        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnsubscribeAll();
                Instance = null;
            }
        }

        [Tooltip("GraphData del que se leen los valores iniciales.")]
        public GraphData graphData;

        [Tooltip("Cada binding conecta un nombre de evento con una condición del grafo.")]
        public List<EventConditionBinding> bindings = new();

        private readonly Dictionary<string, bool> values = new();

        // ── Init ──────────────────────────────────────────────────────────────

        private void InitFromGraph()
        {
            if (graphData == null) return;
            foreach (var cond in graphData.conditions)
                values[cond.guid] = cond.value;
        }

        private void OnEnable() => SubscribeAll();
        private void OnDisable() => UnsubscribeAll();

        private void SubscribeAll()
        {
            foreach (var b in bindings)
            {
                if (string.IsNullOrEmpty(b.eventName)) continue;
                var captured = b;
                b.callback = () => SetValue(captured.conditionGuid, captured.valueToSet);
                GameEventBus.Subscribe(b.eventName, b.callback);
            }
        }

        private void UnsubscribeAll()
        {
            foreach (var b in bindings)
            {
                if (string.IsNullOrEmpty(b.eventName) || b.callback == null) continue;
                GameEventBus.Unsubscribe(b.eventName, b.callback);
                b.callback = null;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public bool GetValue(string conditionGuid)
        {
            if (values.TryGetValue(conditionGuid, out bool v)) return v;
            Debug.LogWarning($"[ConditionManager] Condición no encontrada: {conditionGuid}");
            return false;
        }

        public void SetValue(string conditionGuid, bool value)
        {
            values[conditionGuid] = value;
            Debug.Log($"[ConditionManager] {conditionGuid} → {value}");
        }

        public bool EvaluateAll(List<NodeConditionData> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;
            foreach (var c in conditions)
                if (GetValue(c.conditionGuid) != c.requiredValue) return false;
            return true;
        }
    }

    [System.Serializable]
    public class EventConditionBinding
    {
        [Tooltip("Nombre del evento. Debe coincidir exactamente con lo que se pasa a GameEventBus.Raise().")]
        public string eventName;

        [Tooltip("GUID de la condición en GraphData.")]
        public string conditionGuid;

        [Tooltip("Valor que toma la condición cuando el evento se lanza.")]
        public bool valueToSet = true;

        // Guardamos la referencia al callback para poder desuscribirnos exactamente
        [System.NonSerialized]
        public System.Action callback;
    }
}