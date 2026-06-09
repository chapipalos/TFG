using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueFramework
{
    /// <summary>
    /// Static event bus. Does not require assets or ScriptableObjects.
    /// Events are identified by name (string).
    ///
    /// Raise:       GameEventBus.Raise("OnIronCollected");
    /// Subscribe:   GameEventBus.Subscribe("OnIronCollected", MyCallback);
    /// Unsubscribe: GameEventBus.Unsubscribe("OnIronCollected", MyCallback);
    /// </summary>
    public static class GameEventBus
    {
        private static readonly Dictionary<string, Action> m_Events = new();

        public static void Raise(string eventName)
        {
            if (m_Events.TryGetValue(eventName, out var action))
            {
                action.Invoke();
            }
        }

        public static void Subscribe(string eventName, Action callback)
        {
            if (!m_Events.ContainsKey(eventName))
                m_Events[eventName] = null;
            m_Events[eventName] += callback;
        }

        public static void Unsubscribe(string eventName, Action callback)
        {
            if (m_Events.ContainsKey(eventName))
                m_Events[eventName] -= callback;
        }

        /// <summary>Clears all events. Call when changing the scene.</summary>
        public static void Clear() => m_Events.Clear();
    }
}