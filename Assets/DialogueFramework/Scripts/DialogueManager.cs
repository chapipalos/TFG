using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueFramework
{
    public class DialogueManager : MonoBehaviour
    {
        public bool startDialogue = false;

        [Header("Graph")]
        public GraphData graphData;

        [Header("UI — dialogue")]
        public GameObject m_DialoguePanel;
        private RectTransform dialoguePanelRect;
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
        private RectTransform repliesPanelRect;
        public Button m_ReplyButtonPrefab;

        [Header("Typewriter")]
        [Range(0.01f, 0.2f)]
        public float charDelay = 0.04f;

        [Tooltip("Suavizado de la barra de progreso. Más alto = más rápido. 10≈instantáneo, 4≈muy suave.")]
        [Range(1f, 20f)]
        public float progressBarSmoothing = 8f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Dictionary<string, DialogueNode> nodesByGuid = new();
        private Dictionary<string, List<NodeLinkData>> outgoingLinks = new();

        private DialogueNode currentNode;
        public DialogueNode CurrentNode => currentNode;

        private readonly List<Button> spawnedReplyButtons = new();
        private int currentReplyIndex = 0;

        // Valor actual visible de la barra (separado del progreso real del texto)
        private float currentBarProgress = 0f;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            repliesPanelRect = m_RepliesPanel.GetComponent<RectTransform>();
            m_RepliesPanelImage = m_RepliesPanel.TryGetComponent<Image>(out var img) ? img : null;
            dialoguePanelRect = m_DialoguePanel.GetComponent<RectTransform>();
            m_NextDialogueButtonText = m_NextDialogueButton.GetComponentInChildren<TextMeshProUGUI>();

            m_NextDialogueButton.onClick.RemoveAllListeners();
            m_NextDialogueButton.onClick.AddListener(OnNextPressed);

            m_DialoguePanel.SetActive(false);
        }

        public void StartDialogue()
        {
            if (graphData == null) { Debug.LogError("[Dialogue] GraphData not assigned."); return; }

            BuildNodes();
            BuildLinks();

            string startGuid = FindStartNodeGuid();
            if (!string.IsNullOrEmpty(startGuid) && nodesByGuid.TryGetValue(startGuid, out var startNode))
                ShowNode(startNode);
            else
                Debug.LogWarning("[Dialogue] No start node found.");

            m_DialoguePanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialoguePanelRect);
            startDialogue = true;
        }

        void Update()
        {
            if (!startDialogue) return;
            if (currentNode == null) { m_DialogueText.text = ""; return; }

            bool wasRunning = currentNode.getRunning();
            currentNode.UpdateDialogue(charDelay);
            m_DialogueText.text = currentNode.stringBuilder.ToString();

            // Progreso objetivo (avanza a saltos, un salto por carácter añadido)
            float targetProgress = string.IsNullOrEmpty(currentNode.node.dialogue) ? 1f :
                (float)currentNode.stringBuilder.Length / currentNode.node.dialogue.Length;

            // Lerp continuo hacia el objetivo. Time.deltaTime hace que sea
            // independiente del framerate. 1 - exp(-k·dt) es un suavizado
            // exponencial estable a cualquier fps (no oscila ni se pasa).
            float t = 1f - Mathf.Exp(-progressBarSmoothing * Time.deltaTime);
            currentBarProgress = Mathf.Lerp(currentBarProgress, targetProgress, t);

            UpdateBarProgress(currentBarProgress);

            if (wasRunning && !currentNode.getRunning())
            {
                if (HasVisibleReplies(currentNode.node))
                {
                    m_NextDialogueButton.gameObject.SetActive(false);
                    ShowReplyButtons();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(repliesPanelRect);
                }
                else
                {
                    m_NextDialogueButtonText.text = m_NextButtonTextContinue;
                }
            }
        }

        // ── Graph building ────────────────────────────────────────────────────

        private void BuildNodes()
        {
            nodesByGuid.Clear();
            foreach (var data in graphData.nodes)
                nodesByGuid[data.guid] = new DialogueNode { node = data };
        }

        private void BuildLinks()
        {
            outgoingLinks.Clear();
            foreach (var link in graphData.links)
            {
                if (!outgoingLinks.ContainsKey(link.outputNodeGuid))
                    outgoingLinks[link.outputNodeGuid] = new List<NodeLinkData>();
                outgoingLinks[link.outputNodeGuid].Add(link);
            }
        }

        private string FindStartNodeGuid()
        {
            var hasIncoming = new HashSet<string>();
            foreach (var link in graphData.links) hasIncoming.Add(link.inputNodeGuid);
            foreach (var node in graphData.nodes)
                if (!hasIncoming.Contains(node.guid)) return node.guid;
            return graphData.nodes.Count > 0 ? graphData.nodes[0].guid : null;
        }

        // ── Node display ──────────────────────────────────────────────────────

        private void ShowNode(DialogueNode dialogueNode)
        {
            currentNode = dialogueNode;

            ExecuteNodeEffects(currentNode.node);
            currentNode.StartDialogue();

            // Reiniciar la barra a 0 visualmente al empezar un nodo nuevo
            currentBarProgress = 0f;
            UpdateBarProgress(0f);

            if (m_ActorText != null)
            {
                var actor = graphData.actors.Find(a => a.guid == currentNode.node.actorGuid);
                m_ActorPanel.SetActive(actor != null);
                m_ActorText.text = actor?.name ?? "";
            }

            ClearReplyButtons();

            if (HasVisibleReplies(currentNode.node))
            {
                SpawnReplyButtons(currentNode.node);
            }
            else if(m_RepliesPanelImage != null)
            {
                m_RepliesPanelImage.enabled = false;
            }

            m_NextDialogueButton.gameObject.SetActive(true);
            m_NextDialogueButtonText.text = m_NextButtonTextSkip;
        }

        // ── Quest actions ─────────────────────────────────────────────────────

        // ── Node effects ─────────────────────────────────────────────────────

        private void ExecuteNodeEffects(NodeData node)
        {
            if (node.effects == null || node.effects.Count == 0) return;

            var qm = QuestManager.Instance;
            var cm = ConditionManager.Instance;

            foreach (var effect in node.effects)
            {
                switch (effect.type)
                {
                    // ── Quest effects ────────────────────────────────────────
                    case NodeEffectType.QuestStart:
                        if (string.IsNullOrEmpty(effect.questGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager no encontrado."); break; }
                        qm.StartQuest(effect.questGuid);
                        break;

                    case NodeEffectType.QuestComplete:
                        if (string.IsNullOrEmpty(effect.questGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager no encontrado."); break; }
                        qm.CompleteQuest(effect.questGuid);
                        break;

                    case NodeEffectType.QuestFail:
                        if (string.IsNullOrEmpty(effect.questGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager no encontrado."); break; }
                        qm.FailQuest(effect.questGuid);
                        break;

                    case NodeEffectType.ObjectiveComplete:
                        if (string.IsNullOrEmpty(effect.questGuid)) break;
                        if (string.IsNullOrEmpty(effect.objectiveGuid)) break;
                        if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager no encontrado."); break; }
                        qm.SetObjectiveCompleted(effect.questGuid, effect.objectiveGuid, true);
                        break;

                    // ── Condition effects ────────────────────────────────────
                    case NodeEffectType.ConditionSetTrue:
                        if (string.IsNullOrEmpty(effect.conditionGuid)) break;
                        if (cm == null) { Debug.LogWarning("[Dialogue] ConditionManager no encontrado."); break; }
                        cm.SetValue(effect.conditionGuid, true);
                        break;

                    case NodeEffectType.ConditionSetFalse:
                        if (string.IsNullOrEmpty(effect.conditionGuid)) break;
                        if (cm == null) { Debug.LogWarning("[Dialogue] ConditionManager no encontrado."); break; }
                        cm.SetValue(effect.conditionGuid, false);
                        break;

                    case NodeEffectType.ConditionToggle:
                        if (string.IsNullOrEmpty(effect.conditionGuid)) break;
                        if (cm == null) { Debug.LogWarning("[Dialogue] ConditionManager no encontrado."); break; }
                        cm.ToggleValue(effect.conditionGuid);
                        break;
                }
            }
        }

        // ── Conditions ────────────────────────────────────────────────────────

        private bool NodeConditionsMet(NodeData node)
        {
            var cm = ConditionManager.Instance;
            if (cm != null && !cm.EvaluateAll(node.conditions))
                return false;

            if (node.questRequirements != null && node.questRequirements.Count > 0)
            {
                var qm = QuestManager.Instance;
                if (qm == null) return false;

                foreach (var req in node.questRequirements)
                {
                    if (string.IsNullOrEmpty(req.questGuid)) continue;
                    if (qm.GetStatus(req.questGuid) != req.requiredStatus)
                        return false;
                }
            }

            return true;
        }

        public bool HasVisibleReplies(NodeData node)
            => node.replies != null && node.replies.Count > 0;

        // ── Reply buttons ─────────────────────────────────────────────────────

        private void SpawnReplyButtons(NodeData node)
        {
            foreach (var reply in node.replies)
            {
                var captured = reply;
                var btn = Instantiate(m_ReplyButtonPrefab, m_RepliesPanel);
                btn.gameObject.SetActive(false);
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = reply.text;
                btn.onClick.AddListener(() => OnReplyPressed(captured.guid));
                spawnedReplyButtons.Add(btn);
            }
            currentReplyIndex = 0;
        }

        private void ShowReplyButtons()
        {
            if (m_RepliesPanelImage != null)
                m_RepliesPanelImage.enabled = true;
            foreach (var btn in spawnedReplyButtons)
                if (btn != null) btn.gameObject.SetActive(true);

            HighlightReply(currentReplyIndex);
        }

        private void ClearReplyButtons()
        {
            foreach (var btn in spawnedReplyButtons)
                if (btn != null) Destroy(btn.gameObject);
            spawnedReplyButtons.Clear();
        }

        // ── Input handlers ────────────────────────────────────────────────────

        public void OnNextPressed()
        {
            if (currentNode == null) return;

            if (currentNode.getRunning())
            {
                currentNode.SkipToEnd();
                if (HasVisibleReplies(currentNode.node))
                {
                    m_NextDialogueButton.gameObject.SetActive(false);
                    ShowReplyButtons();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(repliesPanelRect);
                }
                else
                {
                    m_NextDialogueButtonText.text = m_NextButtonTextContinue;
                }
                return;
            }

            if (HasVisibleReplies(currentNode.node)) return;

            AdvanceViaPort("");
        }

        private void OnReplyPressed(string replyGuid)
        {
            if (currentNode == null) return;

            if (currentNode.getRunning())
            {
                currentNode.SkipToEnd();
                return;
            }

            AdvanceViaPort(replyGuid);
        }

        public void OnChangedReplySelection(int opt)
        {
            if (currentNode == null) return;
            if (!HasVisibleReplies(currentNode.node)) return;

            if (currentNode.getRunning()) return;
            if (spawnedReplyButtons.Count == 0) return;

            int count = spawnedReplyButtons.Count;
            currentReplyIndex = ((currentReplyIndex + opt) % count + count) % count;

            UpdateReplyButtonsSelected(spawnedReplyButtons[currentReplyIndex]);
        }

        public void OnReplyPressedAction()
        {
            if (currentNode == null) return;
            if (currentNode.getRunning()) return;
            if (!HasVisibleReplies(currentNode.node)) return;
            if (spawnedReplyButtons.Count == 0) return;

            var selectedButton = spawnedReplyButtons[currentReplyIndex];
            selectedButton.onClick?.Invoke();
        }

        private void UpdateReplyButtonsSelected(Button nextButton)
        {
            // Mismo motivo: solo highlight visual, no .Select().
            HighlightReply(currentReplyIndex);
        }

        private void HighlightReply(int index)
        {
            if (spawnedReplyButtons.Count == 0) return;
            if (index < 0 || index >= spawnedReplyButtons.Count) return;

            for (int i = 0; i < spawnedReplyButtons.Count; i++)
            {
                var btn = spawnedReplyButtons[i];
                if (btn == null) continue;

                var img = btn.GetComponent<Image>();
                if (img == null) continue;

                var colors = btn.colors;
                img.color = (i == index) ? colors.selectedColor : colors.normalColor;
            }
        }

        private void AdvanceViaPort(string outputPortGuid)
        {
            string currentGuid = currentNode.node.guid;

            if (!outgoingLinks.TryGetValue(currentGuid, out var links) || links.Count == 0)
            {
                EndDialogue();
                return;
            }

            NodeLinkData matched = null;
            foreach (var link in links)
            {
                bool portMatch = string.IsNullOrEmpty(outputPortGuid)
                    ? string.IsNullOrEmpty(link.outputPortGuid)
                    : link.outputPortGuid == outputPortGuid;

                if (!portMatch) continue;
                if (!nodesByGuid.TryGetValue(link.inputNodeGuid, out var candidate)) continue;
                if (!NodeConditionsMet(candidate.node)) continue;

                matched = link;
                break;
            }

            if (matched == null)
            {
                Debug.LogWarning($"[Dialogue] Ningún link válido desde '{currentNode.node.title}'.");
                EndDialogue();
                return;
            }

            if (!nodesByGuid.TryGetValue(matched.inputNodeGuid, out var nextNode))
            {
                Debug.LogWarning($"[Dialogue] Nodo destino no encontrado: {matched.inputNodeGuid}");
                EndDialogue();
                return;
            }

            ShowNode(nextNode);
        }

        private void EndDialogue()
        {
            Debug.Log("[Dialogue] Fin del diálogo.");
            ClearReplyButtons();
            m_NextDialogueButton.gameObject.SetActive(false);
            currentNode = null;
            startDialogue = false;
            m_DialoguePanel.SetActive(false);
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
        public NodeData node;
        public StringBuilder stringBuilder = new StringBuilder();

        private bool nodeRunning = false;
        private int textCharIndex = 0;
        private float timer = 0f;

        public bool getRunning() => nodeRunning;
        public void setRunning(bool v) => nodeRunning = v;

        public void StartDialogue()
        {
            stringBuilder.Clear();
            textCharIndex = 0;
            timer = 0f;
            nodeRunning = true;
        }

        public void UpdateDialogue(float charDelay)
        {
            if (!nodeRunning || string.IsNullOrEmpty(node.dialogue)) return;

            timer += Time.deltaTime;
            while (timer >= charDelay && textCharIndex < node.dialogue.Length)
            {
                stringBuilder.Append(node.dialogue[textCharIndex]);
                textCharIndex++;
                timer -= charDelay;
            }

            if (textCharIndex >= node.dialogue.Length)
                nodeRunning = false;
        }

        public void SkipToEnd()
        {
            stringBuilder.Clear();
            stringBuilder.Append(node.dialogue ?? "");
            textCharIndex = node.dialogue?.Length ?? 0;
            timer = 0f;
            nodeRunning = false;
        }
    }
}