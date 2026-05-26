using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueFramework
{
    public class Dialogue : MonoBehaviour
    {
        [Header("Graph")]
        public GraphData graphData;

        [Header("UI — dialogue")]
        public TextMeshProUGUI m_ActorText;
        public TextMeshProUGUI m_DialogueText;

        [Header("UI — navigation")]
        public Button m_NextDialogueButton;
        public Transform m_RepliesPanel;
        public Button m_ReplyButtonPrefab;

        [Header("Typewriter")]
        [Tooltip("Segundos entre cada carácter. 0.04 = ~25 chars/seg, 0.02 = ~50 chars/seg.")]
        [Range(0.01f, 0.2f)]
        public float charDelay = 0.04f;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Dictionary<string, DialogueNode> nodesByGuid = new();
        private Dictionary<string, List<NodeLinkData>> outgoingLinks = new();

        private DialogueNode currentNode;
        private readonly List<Button> spawnedReplyButtons = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Start()
        {
            if (graphData == null) { Debug.LogError("GraphData not assigned."); return; }

            BuildNodes();
            BuildLinks();

            string startGuid = FindStartNodeGuid();

            if (!string.IsNullOrEmpty(startGuid) && nodesByGuid.TryGetValue(startGuid, out var startNode))
                ShowNode(startNode);
            else
                Debug.LogWarning("No start node found.");

            m_NextDialogueButton.onClick.AddListener(OnNextPressed);
        }

        void Update()
        {
            if (currentNode == null)
            {
                m_DialogueText.text = "";
                return;
            }

            bool wasRunning = currentNode.getRunning();
            currentNode.UpdateDialogue(charDelay);
            m_DialogueText.text = currentNode.stringBuilder.ToString();

            // Detectar el momento exacto en que el typewriter termina solo
            // (sin que el usuario haya pulsado saltar).
            if (wasRunning && !currentNode.getRunning())
            {
                bool hasReplies = currentNode.node.replies != null &&
                                  currentNode.node.replies.Count > 0;
                if (hasReplies)
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
            foreach (var nodeData in graphData.nodes)
                nodesByGuid[nodeData.guid] = new DialogueNode { node = nodeData };
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
            var nodesWithIncoming = new HashSet<string>();
            foreach (var link in graphData.links)
                nodesWithIncoming.Add(link.inputNodeGuid);

            foreach (var node in graphData.nodes)
                if (!nodesWithIncoming.Contains(node.guid))
                    return node.guid;

            return graphData.nodes.Count > 0 ? graphData.nodes[0].guid : null;
        }

        // ── Node display ──────────────────────────────────────────────────────

        private void ShowNode(DialogueNode dialogueNode)
        {
            currentNode = dialogueNode;
            currentNode.StartDialogue();

            if (m_ActorText != null)
            {
                var actor = graphData.actors.Find(a => a.guid == currentNode.node.actorGuid);
                m_ActorText.text = actor != null ? actor.name : "";
            }

            // Durante el typewriter el botón Next siempre está visible para saltar.
            // El panel de replies se muestra solo cuando termina de escribir.
            m_NextDialogueButton.gameObject.SetActive(true);
            m_RepliesPanel.gameObject.SetActive(false);

            ClearReplyButtons();

            // Pre-instanciar replies ya (panel oculto) para que al terminar
            // el typewriter solo haya que activar el panel.
            bool hasReplies = currentNode.node.replies != null &&
                              currentNode.node.replies.Count > 0;

            if (hasReplies)
                SpawnReplyButtons(currentNode.node);
        }

        // ── Reply buttons ─────────────────────────────────────────────────────

        private void SpawnReplyButtons(NodeData node)
        {
            foreach (var reply in node.replies)
            {
                var capturedReply = reply;
                var btn = Instantiate(m_ReplyButtonPrefab, m_RepliesPanel);
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = reply.text;
                btn.onClick.AddListener(() => OnReplyPressed(capturedReply.guid));
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

            // Si el texto aún se está escribiendo, saltar al final.
            if (currentNode.getRunning())
            {
                currentNode.SkipToEnd();
                // Si hay replies, mostrarlas ahora que el texto es visible.
                bool hasReplies = currentNode.node.replies != null &&
                                  currentNode.node.replies.Count > 0;
                if (hasReplies)
                {
                    m_NextDialogueButton.gameObject.SetActive(false);
                    m_RepliesPanel.gameObject.SetActive(true);
                }
                return;
            }

            // Texto ya completo y no hay replies → avanzar normalmente.
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

            NodeLinkData matchedLink = null;
            foreach (var link in links)
            {
                bool portMatches = string.IsNullOrEmpty(outputPortGuid)
                    ? string.IsNullOrEmpty(link.outputPortGuid)
                    : link.outputPortGuid == outputPortGuid;

                if (portMatches) { matchedLink = link; break; }
            }

            if (matchedLink == null)
            {
                Debug.LogWarning($"No link found for port '{outputPortGuid}' on node '{currentGuid}'.");
                EndDialogue();
                return;
            }

            if (!nodesByGuid.TryGetValue(matchedLink.inputNodeGuid, out var nextNode))
            {
                Debug.LogWarning($"Target node not found: {matchedLink.inputNodeGuid}");
                EndDialogue();
                return;
            }

            ShowNode(nextNode);
        }

        private void EndDialogue()
        {
            Debug.Log("Dialogue ended.");
            ClearReplyButtons();
            m_NextDialogueButton.gameObject.SetActive(false);
            m_RepliesPanel.gameObject.SetActive(false);
            currentNode = null;
        }
    }

    // ── DialogueNode ─────────────────────────────────────────────────────────

    [System.Serializable]
    public class DialogueNode
    {
        public NodeData node;
        public StringBuilder stringBuilder = new StringBuilder();

        private bool nodeRunning = false;
        private int textCharIndex = 0;
        private float timer = 0f;   // acumula tiempo entre caracteres

        public bool getRunning() => nodeRunning;
        public void setRunning(bool v) => nodeRunning = v;

        public void StartDialogue()
        {
            stringBuilder.Clear();
            textCharIndex = 0;
            timer = 0f;
            nodeRunning = true;
        }

        /// <param name="charDelay">Segundos entre cada carácter.</param>
        public void UpdateDialogue(float charDelay)
        {
            if (!nodeRunning || string.IsNullOrEmpty(node.dialogue))
                return;

            timer += Time.deltaTime;

            // Añade todos los caracteres que correspondan al tiempo acumulado.
            // Si el juego va a 30fps y charDelay es 0.04s, puede tocar añadir
            // 0 o 1 carácter por frame según el acumulado — nunca se pierde tiempo.
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