using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    /// <summary>
    /// Bus de eventos estático. No necesita assets ni ScriptableObjects.
    /// Los eventos se identifican por nombre (string).
    ///
    /// Emitir:    GameEventBus.Raise("OnHierroRecogido");
    /// Escuchar:  GameEventBus.Subscribe("OnHierroRecogido", MiCallback);
    /// Dejar:     GameEventBus.Unsubscribe("OnHierroRecogido", MiCallback);
    /// </summary>
    public static class GameEventBus
    {
        private static readonly Dictionary<string, Action> events = new();

        public static void Raise(string eventName)
        {
            if (events.TryGetValue(eventName, out var action))
            {
                Debug.Log($"[GameEventBus] {eventName}");
                action.Invoke();
            }
        }

        public static void Subscribe(string eventName, Action callback)
        {
            if (!events.ContainsKey(eventName))
                events[eventName] = null;
            events[eventName] += callback;
        }

        public static void Unsubscribe(string eventName, Action callback)
        {
            if (events.ContainsKey(eventName))
                events[eventName] -= callback;
        }

        /// <summary>Limpia todos los eventos. Llámalo al cambiar de escena.</summary>
        public static void Clear() => events.Clear();
    }
}