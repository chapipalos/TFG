using UnityEngine;
using UnityEngine.InputSystem;

namespace DialogueFramework
{
    public class DialogueController : MonoBehaviour
    {
        private bool m_SkipContinuePressed;
        private bool m_NextReplyPressed;
        private bool m_PreviousReplyPressed;

        // ── Input callbacks ───────────────────────────────────────────────

        void OnSkipContinue(InputValue v)
        {
            if (v.isPressed) m_SkipContinuePressed = true;
        }

        void OnNextReply(InputValue v)
        {
            if (v.isPressed) m_NextReplyPressed = true;
        }

        void OnPreviousReply(InputValue v)
        {
            if (v.isPressed) m_PreviousReplyPressed = true;
        }

        // ── Update ────────────────────────────────────────────────────────

        private void Update()
        {
            if (m_SkipContinuePressed)
            {
                m_SkipContinuePressed = false;
                OnEnterPerformed();
            }
            if (m_NextReplyPressed)
            {
                m_NextReplyPressed = false;
                OnNextReplyPerformed();
            }
            if (m_PreviousReplyPressed)
            {
                m_PreviousReplyPressed = false;
                OnPreviousReplyPerformed();
            }
        }

        // ── Input handlers ────────────────────────────────────────────────

        private void OnEnterPerformed()
        {
            var dm = DialogueManager.Active;
            if (dm == null || dm.CurrentNode == null) return;

            if (dm.CurrentNode.GetRunning())
            {
                dm.OnNextPressed();
                return;
            }

            if (dm.HasVisibleReplies(dm.CurrentNode.m_Node))
                dm.OnReplyPressedAction();
            else
                dm.OnNextPressed();
        }

        private void OnNextReplyPerformed()
        {
            var dm = DialogueManager.Active;
            if (dm == null || dm.CurrentNode == null) return;
            dm.OnChangedReplySelection(1);
        }

        private void OnPreviousReplyPerformed()
        {
            var dm = DialogueManager.Active;
            if (dm == null || dm.CurrentNode == null) return;
            dm.OnChangedReplySelection(-1);
        }
    }
}