using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueFramework
{
    public class DialogueManager : MonoBehaviour
    {
        public bool m_IsFloatingDialogue = false;

        public bool m_DialogueRunning = false;

        [Header("Graph")]
        public GraphData m_GraphData;

        [Tooltip("Name of the conversation inside the GraphData (e.g. 'Aldric_Forge').")]
        public string m_ConversationName;

        [Header("UI — dialogue")]
        public GameObject m_DialoguePanel;
        private RectTransform m_DialoguePanelRect;
        public GameObject m_ActorPanel;
        public TextMeshProUGUI m_ActorText;
        public TextMeshProUGUI m_DialogueText;
        public RectTransform m_DialogueBarProgress;

        [Header("UI — navigation")]
        public Button m_NextDialogueButton;
        private TextMeshProUGUI m_NextDialogueButtonText;
        public string m_NextButtonTextSkip = "Skip";
        public string m_NextButtonTextContinue = "Continue";
        public Transform m_RepliesPanel;
        private Image m_RepliesPanelImage;
        private RectTransform m_RepliesPanelRect;
        public Button m_ReplyButtonPrefab;

        [Header("Typewriter")]
        [Range(0.01f, 0.2f)]
        public float m_CharDelay = 0.04f;

        [Tooltip("Suavizado de la barra de progreso. Más alto = más rápido. 10≈instantáneo, 4≈muy suave.")]
        [Range(1f, 20f)]
        public float m_ProgressBarSmoothing = 8f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Dictionary<string, DialogueNode> m_NodesByGuid = new();
        private Dictionary<string, List<NodeLinkData>> m_OutgoingLinks = new();

        // Resolved at StartDialogue from m_ConversationName
        private string m_ConversationGuid;

        private DialogueNode m_CurrentNode;
        public DialogueNode CurrentNode => m_CurrentNode;

        public static DialogueManager Active { get; private set; }

        private readonly List<Button> m_SpawnedReplyButtons = new();
        private int m_CurrentReplyIndex = 0;

        private float m_CurrentBarProgress = 0f;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            if (!m_IsFloatingDialogue)
            {
                m_RepliesPanelRect = m_RepliesPanel.GetComponent<RectTransform>();
                m_RepliesPanelImage = m_RepliesPanel.TryGetComponent<Image>(out var img) ? img : null;
                m_NextDialogueButtonText = m_NextDialogueButton.GetComponentInChildren<TextMeshProUGUI>();

                m_NextDialogueButton.onClick.RemoveAllListeners();
                m_NextDialogueButton.onClick.AddListener(OnNextPressed);
            }

            m_DialoguePanelRect = m_DialoguePanel.GetComponent<RectTransform>();
            m_DialoguePanel.SetActive(false);
        }

        public void StartDialogue()
        {
            if (m_GraphData == null) { Debug.LogError("[Dialogue] GraphData not assigned."); return; }

            // Resolve the conversation guid from its name
            if (string.IsNullOrEmpty(m_ConversationName))
            {
                Debug.LogError("[Dialogue] Conversation name is empty. Set m_ConversationName in the inspector.");
                return;
            }

            var conv = m_GraphData.s_Conversations.Find(c => c.s_CName == m_ConversationName);
            if (conv == null)
            {
                Debug.LogError($"[Dialogue] Conversation '{m_ConversationName}' not found in GraphData.");
                return;
            }
            m_ConversationGuid = conv.s_CGuid;

            BuildNodes();
            BuildLinks();

            string startGuid = FindStartNodeGuid();
            if (!string.IsNullOrEmpty(startGuid) && m_NodesByGuid.TryGetValue(startGuid, out var startNode))
                ShowNode(startNode);
            else
                Debug.LogWarning($"[Dialogue] No start node found in conversation '{m_ConversationName}'.");

            m_DialoguePanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_DialoguePanelRect);
            m_DialogueRunning = true;
            if (!m_IsFloatingDialogue)
                Active = this;

            if (m_IsFloatingDialogue)
            {
                CurrentNode?.SkipToEnd();

            }
        }

        void Update()
        {
            if (!m_DialogueRunning) return;
            if (m_CurrentNode == null) { m_DialogueText.text = ""; return; }

            bool wasRunning = m_CurrentNode.GetRunning();
            m_CurrentNode.UpdateDialogue(m_CharDelay);
            m_DialogueText.text = m_CurrentNode.m_StringBuilder.ToString();

            float targetProgress = string.IsNullOrEmpty(m_CurrentNode.m_Node.s_Dialogue) ? 1f :
                (float)m_CurrentNode.m_StringBuilder.Length / m_CurrentNode.m_Node.s_Dialogue.Length;

            float t = 1f - Mathf.Exp(-m_ProgressBarSmoothing * Time.deltaTime);
            m_CurrentBarProgress = Mathf.Lerp(m_CurrentBarProgress, targetProgress, t);

            UpdateBarProgress(m_CurrentBarProgress);

            if (wasRunning && !m_CurrentNode.GetRunning() && !m_IsFloatingDialogue)
            {
                if (HasVisibleReplies(m_CurrentNode.m_Node))
                {
                    m_NextDialogueButton.gameObject.SetActive(false);
                    ShowReplyButtons();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_RepliesPanelRect);
                }
                else
                {
                    if (m_NextDialogueButtonText != null)
                        m_NextDialogueButtonText.text = m_NextButtonTextContinue;
                }
            }
        }

        // ── Graph building ────────────────────────────────────────────────────

        private void BuildNodes()
        {
            m_NodesByGuid.Clear();
            foreach (var data in m_GraphData.s_Nodes)
            {
                // Only load nodes belonging to this conversation
                if (data.s_ConversationGuid != m_ConversationGuid) continue;
                m_NodesByGuid[data.s_NGuid] = new DialogueNode { m_Node = data };
            }
        }

        private void BuildLinks()
        {
            m_OutgoingLinks.Clear();
            foreach (var link in m_GraphData.s_Links)
            {
                // Only keep links whose endpoints are both in this conversation
                if (!m_NodesByGuid.ContainsKey(link.s_OutputNodeGuid)) continue;
                if (!m_NodesByGuid.ContainsKey(link.s_InputNodeGuid)) continue;

                if (!m_OutgoingLinks.ContainsKey(link.s_OutputNodeGuid))
                    m_OutgoingLinks[link.s_OutputNodeGuid] = new List<NodeLinkData>();
                m_OutgoingLinks[link.s_OutputNodeGuid].Add(link);
            }
        }

        private string FindStartNodeGuid()
        {
            // Compute incoming links only from nodes in this conversation
            var hasIncoming = new HashSet<string>();
            foreach (var link in m_GraphData.s_Links)
            {
                if (!m_NodesByGuid.ContainsKey(link.s_OutputNodeGuid)) continue;
                if (!m_NodesByGuid.ContainsKey(link.s_InputNodeGuid)) continue;
                hasIncoming.Add(link.s_InputNodeGuid);
            }

            foreach (var guid in m_NodesByGuid.Keys)
                if (!hasIncoming.Contains(guid)) return guid;

            // Fallback: first node of the conversation
            foreach (var guid in m_NodesByGuid.Keys) return guid;
            return null;
        }

        // ── Node display ──────────────────────────────────────────────────────

        private void ShowNode(DialogueNode dialogueNode)
        {
            m_CurrentNode = dialogueNode;

            ExecuteNodeEffects(m_CurrentNode.m_Node);
            m_CurrentNode.StartDialogue();

            m_CurrentBarProgress = 0f;
            UpdateBarProgress(0f);

            if (m_ActorText != null)
            {
                var actor = m_GraphData.s_Actors.Find(a => a.s_AGuid == m_CurrentNode.m_Node.s_ActorGuid);
                m_ActorPanel.SetActive(actor != null);
                m_ActorText.text = actor?.s_ActorName ?? "";
            }

            if (!m_IsFloatingDialogue)
            {
                ClearReplyButtons();

                if (HasVisibleReplies(m_CurrentNode.m_Node))
                {
                    SpawnReplyButtons(m_CurrentNode.m_Node);
                }
                else if (m_RepliesPanelImage != null)
                {
                    m_RepliesPanelImage.enabled = false;
                }

                m_NextDialogueButton.gameObject.SetActive(true);
                if (m_NextDialogueButtonText != null)
                    m_NextDialogueButtonText.text = m_NextButtonTextSkip;
            }
        }

        // ── Node effects ─────────────────────────────────────────────────────

        private void ExecuteNodeEffects(NodeData node)
        {
            if (node.s_Effects == null || node.s_Effects.Count == 0) return;

            var qm = QuestManager.m_Instance;

            foreach (var effect in node.s_Effects)
            {
                switch (effect._EffectType)
                {
                    case NodeEffectType.QuestStart:
                        if (string.IsNullOrEmpty(effect.s_QuestGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager not found."); break; }
                        qm.StartQuest(effect.s_QuestGuid);
                        break;

                    case NodeEffectType.QuestComplete:
                        if (string.IsNullOrEmpty(effect.s_QuestGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager not found."); break; }
                        qm.CompleteQuest(effect.s_QuestGuid);
                        break;

                    case NodeEffectType.QuestFail:
                        if (string.IsNullOrEmpty(effect.s_QuestGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager not found."); break; }
                        qm.FailQuest(effect.s_QuestGuid);
                        break;

                    case NodeEffectType.ObjectiveComplete:
                        if (string.IsNullOrEmpty(effect.s_QuestGuid)) break;
                        if (string.IsNullOrEmpty(effect.s_ObjectiveGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager not found."); break; }
                        qm.SetObjectiveCompleted(effect.s_QuestGuid, effect.s_ObjectiveGuid, true);
                        break;
                }
            }
        }

        // ── Conditions ────────────────────────────────────────────────────────

        private bool NodeConditionsMet(NodeData node)
        {
            if (node.s_QuestRequirements != null && node.s_QuestRequirements.Count > 0)
            {
                var qm = QuestManager.m_Instance;
                if (qm == null) return false;

                foreach (var req in node.s_QuestRequirements)
                {
                    if (string.IsNullOrEmpty(req.s_QuestGuid)) continue;
                    if (qm.GetStatus(req.s_QuestGuid) != req.s_RequiredStatus)
                        return false;
                }
            }

            if (node.s_ObjectiveRequirements != null && node.s_ObjectiveRequirements.Count > 0)
            {
                var qm = QuestManager.m_Instance;
                if (qm == null) return false;

                foreach (var req in node.s_ObjectiveRequirements)
                {
                    if (string.IsNullOrEmpty(req.s_QuestGuid)) continue;
                    if (string.IsNullOrEmpty(req.s_ObjectiveGuid)) continue;

                    bool isCompleted = qm.IsObjectiveCompleted(req.s_QuestGuid, req.s_ObjectiveGuid);
                    if (isCompleted != req.s_MustBeCompleted)
                        return false;
                }
            }

            return true;
        }

        public bool HasVisibleReplies(NodeData node)
            => node.s_Replies != null && node.s_Replies.Count > 0;

        // ── Reply buttons ─────────────────────────────────────────────────────

        private void SpawnReplyButtons(NodeData node)
        {
            foreach (var reply in node.s_Replies)
            {
                var captured = reply;
                var btn = Instantiate(m_ReplyButtonPrefab, m_RepliesPanel);
                btn.gameObject.SetActive(false);
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = reply.s_ReplyText;
                btn.onClick.AddListener(() => OnReplyPressed(captured.s_RGuid));
                m_SpawnedReplyButtons.Add(btn);
            }
            m_CurrentReplyIndex = 0;
        }

        private void ShowReplyButtons()
        {
            if (m_RepliesPanelImage != null)
                m_RepliesPanelImage.enabled = true;
            foreach (var btn in m_SpawnedReplyButtons)
                if (btn != null) btn.gameObject.SetActive(true);

            HighlightReply(m_CurrentReplyIndex);
        }

        private void ClearReplyButtons()
        {
            foreach (var btn in m_SpawnedReplyButtons)
                if (btn != null) Destroy(btn.gameObject);
            m_SpawnedReplyButtons.Clear();
        }

        // ── Input handlers ────────────────────────────────────────────────────

        public void OnNextPressed()
        {
            if (m_CurrentNode == null) return;

            if (m_CurrentNode.GetRunning())
            {
                m_CurrentNode.SkipToEnd();
                if (HasVisibleReplies(m_CurrentNode.m_Node))
                {
                    m_NextDialogueButton.gameObject.SetActive(false);
                    ShowReplyButtons();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_RepliesPanelRect);
                }
                else
                {
                    if (m_NextDialogueButtonText != null)
                        m_NextDialogueButtonText.text = m_NextButtonTextContinue;
                }
                return;
            }

            if (HasVisibleReplies(m_CurrentNode.m_Node)) return;

            AdvanceViaPort("");
        }

        private void OnReplyPressed(string replyGuid)
        {
            if (m_CurrentNode == null) return;

            if (m_CurrentNode.GetRunning())
            {
                m_CurrentNode.SkipToEnd();
                return;
            }

            AdvanceViaPort(replyGuid);
        }

        public void OnChangedReplySelection(int opt)
        {
            if (m_CurrentNode == null) return;
            if (!HasVisibleReplies(m_CurrentNode.m_Node)) return;

            if (m_CurrentNode.GetRunning()) return;
            if (m_SpawnedReplyButtons.Count == 0) return;

            int count = m_SpawnedReplyButtons.Count;
            m_CurrentReplyIndex = ((m_CurrentReplyIndex + opt) % count + count) % count;

            UpdateReplyButtonsSelected(m_SpawnedReplyButtons[m_CurrentReplyIndex]);
        }

        public void OnReplyPressedAction()
        {
            if (m_CurrentNode == null) return;
            if (m_CurrentNode.GetRunning()) return;
            if (!HasVisibleReplies(m_CurrentNode.m_Node)) return;
            if (m_SpawnedReplyButtons.Count == 0) return;

            var selectedButton = m_SpawnedReplyButtons[m_CurrentReplyIndex];
            selectedButton.onClick?.Invoke();
        }

        private void UpdateReplyButtonsSelected(Button nextButton)
        {
            HighlightReply(m_CurrentReplyIndex);
        }

        private void HighlightReply(int index)
        {
            if (m_SpawnedReplyButtons.Count == 0) return;
            if (index < 0 || index >= m_SpawnedReplyButtons.Count) return;

            for (int i = 0; i < m_SpawnedReplyButtons.Count; i++)
            {
                var btn = m_SpawnedReplyButtons[i];
                if (btn == null) continue;

                var img = btn.GetComponent<Image>();
                if (img == null) continue;

                var colors = btn.colors;
                img.color = (i == index) ? colors.highlightedColor : colors.normalColor;
            }
        }

        private void AdvanceViaPort(string outputPortGuid)
        {
            string currentGuid = m_CurrentNode.m_Node.s_NGuid;

            if (!m_OutgoingLinks.TryGetValue(currentGuid, out var links) || links.Count == 0)
            {
                EndDialogue();
                return;
            }

            NodeLinkData matched = null;
            foreach (var link in links)
            {
                bool portMatch = string.IsNullOrEmpty(outputPortGuid)
                    ? string.IsNullOrEmpty(link.s_OutputPortGuid)
                    : link.s_OutputPortGuid == outputPortGuid;

                if (!portMatch) continue;
                if (!m_NodesByGuid.TryGetValue(link.s_InputNodeGuid, out var candidate)) continue;
                if (!NodeConditionsMet(candidate.m_Node)) continue;

                matched = link;
                break;
            }

            if (matched == null)
            {
                Debug.LogWarning($"[Dialogue] No valid link from '{m_CurrentNode.m_Node.s_NodeTitle}'.");
                EndDialogue();
                return;
            }

            if (!m_NodesByGuid.TryGetValue(matched.s_InputNodeGuid, out var nextNode))
            {
                Debug.LogWarning($"[Dialogue] Destiny node not found: {matched.s_InputNodeGuid}");
                EndDialogue();
                return;
            }

            ShowNode(nextNode);
        }

        public void EndDialogue()
        {
            if (!m_IsFloatingDialogue)
            {
                ClearReplyButtons();
                m_NextDialogueButton.gameObject.SetActive(false);
            }
            m_CurrentNode = null;
            m_DialogueRunning = false;
            m_DialoguePanel.SetActive(false);

            if (!m_IsFloatingDialogue && Active == this) Active = null;
        }

        public void UpdateBarProgress(float progress)
        {
            if (m_DialogueBarProgress != null)
                m_DialogueBarProgress.localScale = new Vector3(-Mathf.Clamp01(progress), 1f, 1f);
        }
    }

    // ── DialogueNode ──────────────────────────────────────────────────────────

    [System.Serializable]
    public class DialogueNode
    {
        public NodeData m_Node;
        public StringBuilder m_StringBuilder = new StringBuilder();

        private bool m_NodeRunning = false;
        private int m_TextCharIndex = 0;
        private float m_Timer = 0f;

        public bool GetRunning() => m_NodeRunning;
        public void SetRunning(bool v) => m_NodeRunning = v;

        public void StartDialogue()
        {
            m_StringBuilder.Clear();
            m_TextCharIndex = 0;
            m_Timer = 0f;
            m_NodeRunning = true;
        }

        public void UpdateDialogue(float charDelay)
        {
            if (!m_NodeRunning || string.IsNullOrEmpty(m_Node.s_Dialogue)) return;

            m_Timer += Time.deltaTime;
            while (m_Timer >= charDelay && m_TextCharIndex < m_Node.s_Dialogue.Length)
            {
                m_StringBuilder.Append(m_Node.s_Dialogue[m_TextCharIndex]);
                m_TextCharIndex++;
                m_Timer -= charDelay;
            }

            if (m_TextCharIndex >= m_Node.s_Dialogue.Length)
                m_NodeRunning = false;
        }

        public void SkipToEnd()
        {
            m_StringBuilder.Clear();
            m_StringBuilder.Append(m_Node.s_Dialogue ?? "");
            m_TextCharIndex = m_Node.s_Dialogue?.Length ?? 0;
            m_Timer = 0f;
            m_NodeRunning = false;
        }
    }
}