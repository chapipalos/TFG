using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueFramework
{
    public class Dialogue : MonoBehaviour
    {
        public GraphData graphData;

        private Dictionary<string, DialogueNode> nodesByGuid = new Dictionary<string, DialogueNode>();
        private Dictionary<string, List<string>> outgoingLinks = new Dictionary<string, List<string>>();

        private DialogueNode currentNode;

        public TextMeshProUGUI m_DialogueText;
        public Button m_NextDialogueButton;

        void Start()
        {
            if (graphData == null)
            {
                Debug.LogError("GraphData no asignado.");
                return;
            }

            BuildNodes();
            BuildLinks();

            string startNodeGuid = FindStartNodeGuid();

            if (!string.IsNullOrEmpty(startNodeGuid) && nodesByGuid.TryGetValue(startNodeGuid, out DialogueNode startNode))
            {
                currentNode = startNode;
                currentNode.StartDialogue();
            }
            else
            {
                Debug.LogWarning("No se encontró nodo inicial.");
            }

            m_NextDialogueButton.onClick.AddListener(NextDialoguePressed);
        }

        void Update()
        {
            if (currentNode != null)
            {
                currentNode.UpdateDialogue();
                m_DialogueText.text = currentNode.stringBuilder.ToString();
            }
            else
            {
                m_DialogueText.text = "";
            }
        }

        private void BuildNodes()
        {
            nodesByGuid.Clear();

            foreach (NodeData nodeData in graphData.nodes)
            {
                DialogueNode dialogueNode = new DialogueNode();
                dialogueNode.node = nodeData;
                nodesByGuid[nodeData.guid] = dialogueNode;
            }
        }

        private void BuildLinks()
        {
            outgoingLinks.Clear();

            foreach (NodeLinkData link in graphData.links)
            {
                if (!outgoingLinks.ContainsKey(link.outputNodeGuid))
                {
                    outgoingLinks[link.outputNodeGuid] = new List<string>();
                }

                outgoingLinks[link.outputNodeGuid].Add(link.inputNodeGuid);
            }
        }

        private string FindStartNodeGuid()
        {
            HashSet<string> nodesWithIncomingLinks = new HashSet<string>();

            foreach (NodeLinkData link in graphData.links)
            {
                nodesWithIncomingLinks.Add(link.inputNodeGuid);
            }

            foreach (NodeData node in graphData.nodes)
            {
                if (!nodesWithIncomingLinks.Contains(node.guid))
                {
                    return node.guid;
                }
            }

            if (graphData.nodes.Count > 0)
                return graphData.nodes[0].guid;

            return null;
        }

        public void NextDialoguePressed()
        {
            if (currentNode == null)
                return;

            if (currentNode.getRunning())
            {
                currentNode.SkipToEnd();
                return;
            }

            string currentGuid = currentNode.node.guid;

            if (!outgoingLinks.TryGetValue(currentGuid, out List<string> nextNodes) || nextNodes.Count == 0)
            {
                Debug.Log("Fin del diálogo.");
                currentNode = null;
                return;
            }

            // De momento toma el primer enlace de salida
            string nextGuid = nextNodes[0];

            if (nodesByGuid.TryGetValue(nextGuid, out DialogueNode nextNode))
            {
                currentNode = nextNode;
                currentNode.StartDialogue();
            }
            else
            {
                Debug.LogWarning($"No se encontró el nodo con guid: {nextGuid}");
                currentNode = null;
            }
        }
    }

    [System.Serializable]
    public class DialogueNode
    {
        public NodeData node;
        public StringBuilder stringBuilder = new StringBuilder();

        private bool nodeRunning = false;
        private int textCharIndex = 0;

        public bool getRunning()
        {
            return nodeRunning;
        }

        public void setRunning(bool running)
        {
            nodeRunning = running;
        }

        public void StartDialogue()
        {
            stringBuilder.Clear();
            textCharIndex = 0;
            nodeRunning = true;
        }

        public void UpdateDialogue()
        {
            if (!nodeRunning || string.IsNullOrEmpty(node.dialogue))
                return;

            if (textCharIndex < node.dialogue.Length)
            {
                stringBuilder.Append(node.dialogue[textCharIndex]);
                textCharIndex++;
            }
            else
            {
                nodeRunning = false;
            }
        }

        public void SkipToEnd()
        {
            stringBuilder.Clear();
            stringBuilder.Append(node.dialogue);
            textCharIndex = node.dialogue != null ? node.dialogue.Length : 0;
            nodeRunning = false;
        }
    }
}