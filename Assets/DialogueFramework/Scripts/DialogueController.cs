using DialogueFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueFramework
{
    public class DialogueController : MonoBehaviour
    {
        public InputActionReference m_EnterOption;
        public InputActionReference m_NextReply;
        public InputActionReference m_PreviousReply;

        private DialogueManager m_DialogueManager;

        private void Awake()
        {
            m_DialogueManager = GetComponentInParent<DialogueManager>();
        }

        private void OnEnable()
        {
            m_EnterOption.action.performed += OnEnterPerformed;
            m_NextReply.action.performed += OnNextReplyPerformed;
            m_PreviousReply.action.performed += OnPreviousReplyPerformed;
        }

        private void OnDisable()
        {
            m_EnterOption.action.performed -= OnEnterPerformed;
            m_NextReply.action.performed -= OnNextReplyPerformed;
            m_PreviousReply.action.performed -= OnPreviousReplyPerformed;
        }

        private void OnEnterPerformed(InputAction.CallbackContext context)
        {
            if (m_DialogueManager == null || m_DialogueManager.CurrentNode == null)
                return;

            if (m_DialogueManager.CurrentNode.GetRunning())
            {
                m_DialogueManager.OnNextPressed();
                return;
            }

            if (m_DialogueManager.HasVisibleReplies(m_DialogueManager.CurrentNode.m_Node))
                m_DialogueManager.OnReplyPressedAction();
            else
                m_DialogueManager.OnNextPressed();
        }

        private void OnNextReplyPerformed(InputAction.CallbackContext context)
        {
            if (m_DialogueManager == null || m_DialogueManager.CurrentNode == null) return;
            m_DialogueManager.OnChangedReplySelection(1);
        }

        private void OnPreviousReplyPerformed(InputAction.CallbackContext context)
        {
            if (m_DialogueManager == null || m_DialogueManager.CurrentNode == null) return;
            m_DialogueManager.OnChangedReplySelection(-1);
        }
    }
}