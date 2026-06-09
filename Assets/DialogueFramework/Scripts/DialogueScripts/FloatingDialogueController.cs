using DialogueFramework;
using UnityEngine;

namespace DialogueFramework
{
    public class FloatingDialogueController : MonoBehaviour
    {
        private DialogueManager m_DialogueManager;
        public GameObject m_Canvas;

        private Camera m_PlayerCamera;
        private bool m_PlayerInside;

        private void Awake()
        {
            m_DialogueManager = GetComponent<DialogueManager>();
            m_PlayerCamera = Camera.main;
        }

        private void Update()
        {
            if (!m_DialogueManager.m_DialogueRunning) return;
            if (m_PlayerCamera == null) return;
            if (m_Canvas == null) return;

            var lookDirection = m_Canvas.transform.position - m_PlayerCamera.transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
                m_Canvas.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (m_DialogueManager == null) return;
            if (m_PlayerInside) return;

            m_PlayerInside = true;

            if (!m_DialogueManager.m_DialogueRunning)
                m_DialogueManager.StartDialogue();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (m_DialogueManager == null) return;
            if (!m_PlayerInside) return;

            m_PlayerInside = false;

            if (m_DialogueManager.m_DialogueRunning)
                m_DialogueManager.EndDialogue();
        }
    }
}