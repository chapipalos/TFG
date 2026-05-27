using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueFramework
{
    public class Dialogue : MonoBehaviour
    {
        public bool startDialogue = false;

        [Header("Graph")]
        public GraphData graphData;

        [Header("UI - canvas")]
        private Canvas dialogueCanvas;

        [Header("UI — dialogue")]
        public TextMeshProUGUI m_ActorText;
        public TextMeshProUGUI m_DialogueText;

        [Header("UI — navigation")]
        public Button m_NextDialogueButton;
        public Transform m_RepliesPanel;
        public Button m_ReplyButtonPrefab;

        [Header("Typewriter")]
        [Range(0.01f, 0.2f)]
        public float charDelay = 0.04f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Dictionary<string, DialogueNode> nodesByGuid = new();
        private Dictionary<string, List<NodeLinkData>> outgoingLinks = new();

        private DialogueNode currentNode;
        private readonly List<Button> spawnedReplyButtons = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            dialogueCanvas = GetComponent<Canvas>();
            if (dialogueCanvas == null)
            {
                Debug.LogError("[Dialogue] No se encontró un Canvas en este GameObject.");
                return;
            }

            // FIX 1: registrar el listener UNA sola vez aquí.
            // Antes se llamaba dentro de StartDialogue(), acumulando un listener
            // extra en cada conversación → OnNextPressed se ejecutaba N veces.
            m_NextDialogueButton.onClick.RemoveAllListeners();
            m_NextDialogueButton.onClick.AddListener(OnNextPressed);

            dialogueCanvas.gameObject.SetActive(false);
        }

        // ── Public entry point ────────────────────────────────────────────────

        public void StartDialogue()
        {
            if (graphData == null) { Debug.LogError("[Dialogue] GraphData not assigned."); return; }

            // Reconstruir siempre para reflejar cambios en condiciones / quests
            BuildNodes();
            BuildLinks();

            string startGuid = FindStartNodeGuid();
            if (!string.IsNullOrEmpty(startGuid) && nodesByGuid.TryGetValue(startGuid, out var startNode))
                ShowNode(startNode);
            else
                Debug.LogWarning("[Dialogue] No start node found.");

            dialogueCanvas.gameObject.SetActive(true);
            startDialogue = true;
        }

        // ── Update ────────────────────────────────────────────────────────────

        void Update()
        {
            if (!startDialogue) return;
            if (currentNode == null) { m_DialogueText.text = ""; return; }

            bool wasRunning = currentNode.getRunning();
            currentNode.UpdateDialogue(charDelay);
            m_DialogueText.text = currentNode.stringBuilder.ToString();

            // Typewriter terminó solo → mostrar replies si las hay
            if (wasRunning && !currentNode.getRunning())
            {
                if (HasVisibleReplies(currentNode.node))
                {
                    m_NextDialogueButton.gameObject.SetActive(false);
                    m_RepliesPanel.gameObject.SetActive(true);
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

            ExecuteQuestActions(currentNode.node);
            currentNode.StartDialogue();

            if (m_ActorText != null)
            {
                var actor = graphData.actors.Find(a => a.guid == currentNode.node.actorGuid);
                m_ActorText.text = actor?.name ?? "";
            }

            m_NextDialogueButton.gameObject.SetActive(true);
            m_RepliesPanel.gameObject.SetActive(false);

            ClearReplyButtons();

            if (HasVisibleReplies(currentNode.node))
                SpawnReplyButtons(currentNode.node);
        }

        // ── Quest actions ─────────────────────────────────────────────────────

        private void ExecuteQuestActions(NodeData node)
        {
            if (node.questActions == null || node.questActions.Count == 0) return;

            var qm = QuestManager.Instance;
            if (qm == null) { Debug.LogWarning("[Dialogue] QuestManager no encontrado."); return; }

            foreach (var action in node.questActions)
            {
                if (string.IsNullOrEmpty(action.questGuid)) continue;
                switch (action.action)
                {
                    case QuestActionType.Start: qm.StartQuest(action.questGuid); break;
                    case QuestActionType.Complete: qm.CompleteQuest(action.questGuid); break;
                    case QuestActionType.Fail: qm.FailQuest(action.questGuid); break;
                }
            }
        }

        // ── Conditions ────────────────────────────────────────────────────────

        private bool NodeConditionsMet(NodeData node)
        {
            // 1. Condiciones booleanas (items, zonas, etc.)
            var cm = ConditionManager.Instance;
            if (cm != null && !cm.EvaluateAll(node.conditions))
                return false;

            // 2. Requisitos de estado de quest
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

        private bool HasVisibleReplies(NodeData node)
            => node.replies != null && node.replies.Count > 0;

        // ── Reply buttons ─────────────────────────────────────────────────────

        private void SpawnReplyButtons(NodeData node)
        {
            foreach (var reply in node.replies)
            {
                var captured = reply;
                var btn = Instantiate(m_ReplyButtonPrefab, m_RepliesPanel);
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = reply.text;
                btn.onClick.AddListener(() => OnReplyPressed(captured.guid));
                spawnedReplyButtons.Add(btn);
            }
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
                    m_RepliesPanel.gameObject.SetActive(true);
                }
                return;
            }

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

                // FIX 2: evaluar condiciones en runtime, no en tiempo de construcción.
                // Antes BuildNodes() se llamaba una vez y los nodos no se reevaluaban.
                // Ahora se comprueba en cada avance con el estado actual del ConditionManager.
                if (!NodeConditionsMet(candidate.node)) continue;

                matched = link;
                break;
            }

            if (matched == null)
            {
                // FIX 3: si ningún link pasa las condiciones, loguear cuáles fallaron
                // para facilitar el debug en lugar de silenciosamente cerrar el diálogo.
                Debug.LogWarning($"[Dialogue] Ningún link válido desde '{currentNode.node.title}'. " +
                                 $"Comprueba que las condiciones del nodo destino se cumplen.");
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
            // FIX 1b: NO llamar RemoveAllListeners() aquí — el listener de Start
            // debe sobrevivir entre conversaciones.
            m_NextDialogueButton.gameObject.SetActive(false);
            m_RepliesPanel.gameObject.SetActive(false);
            currentNode = null;
            startDialogue = false;
            dialogueCanvas.gameObject.SetActive(false);
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