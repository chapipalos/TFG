using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public static class NodeGraphSaveUtility
    {
        // ── Save ──────────────────────────────────────────────────────────────

        public static void Save(DialoguesView graphView, GraphData graphData)
        {
            if (graphView == null || graphData == null)
            {
                Debug.LogError("[SaveUtility] GraphView or GraphData is null.");
                return;
            }

            Undo.RecordObject(graphData, "Save Node Graph");

            string currentConversation = graphView.CurrentConversationGuid;

            // FIX: solo borrar nodos y links de la conversación activa.
            // Los de otras conversaciones deben preservarse en el asset.
            if (!string.IsNullOrEmpty(currentConversation))
            {
                graphData.s_Nodes.RemoveAll(n => n.s_ConversationGuid == currentConversation);

                // Borrar links cuyos extremos sean nodos que vamos a reescribir
                var nodesInConversation = new HashSet<string>();
                foreach (var n in graphView.nodes.ToList().OfType<DialogueEditorNode>())
                    nodesInConversation.Add(n.m_Data.s_NGuid);

                graphData.s_Links.RemoveAll(l =>
                    nodesInConversation.Contains(l.s_OutputNodeGuid) ||
                    nodesInConversation.Contains(l.s_InputNodeGuid));
            }

            // ── Nodes ─────────────────────────────────────────────────────────
            var graphNodes = graphView.nodes.ToList().OfType<DialogueEditorNode>().ToList();
            var validGuids = new HashSet<string>();

            foreach (var node in graphNodes)
            {
                node.m_Data.s_NodePosition = node.GetPosition().position;

                if (string.IsNullOrEmpty(node.m_Data.s_NGuid))
                    node.m_Data.s_NGuid = GUID.Generate().ToString();

                node.m_Data.s_ConversationGuid = currentConversation;
                
                graphData.s_Nodes.Add(node.m_Data);
                validGuids.Add(node.m_Data.s_NGuid);
            }

            // ── Edges ─────────────────────────────────────────────────────────
            foreach (var edge in graphView.edges.ToList())
            {
                if (edge.output?.node is not DialogueEditorNode outputNode) continue;
                if (edge.input?.node is not DialogueEditorNode inputNode) continue;

                if (!validGuids.Contains(outputNode.m_Data.s_NGuid) ||
                    !validGuids.Contains(inputNode.m_Data.s_NGuid))
                    continue;

                string outputPortGuid = string.Empty;
                foreach (var (replyGuid, port) in outputNode.GetAllReplyPorts())
                {
                    if (port == edge.output)
                    {
                        outputPortGuid = replyGuid;
                        break;
                    }
                }

                graphData.s_Links.Add(new NodeLinkData
                {
                    s_OutputNodeGuid = outputNode.m_Data.s_NGuid,
                    s_InputNodeGuid = inputNode.m_Data.s_NGuid,
                    s_OutputPortGuid = outputPortGuid
                });
            }

            EditorUtility.SetDirty(graphData);
            AssetDatabase.SaveAssets();
        }

        // ── Load ──────────────────────────────────────────────────────────────

        public static void Load(DialoguesView graphView, GraphData graphData)
        {
            if (graphView == null || graphData == null)
            {
                Debug.LogError("[SaveUtility] GraphView or GraphData is null.");
                return;
            }

            graphView.ClearGraph();

            string currentConversation = graphView.CurrentConversationGuid;
            if (string.IsNullOrEmpty(currentConversation)) return;

            // ── Nodes ─────────────────────────────────────────────────────────
            var createdNodes = new Dictionary<string, DialogueEditorNode>();

            foreach (var nodeData in graphData.s_Nodes)
            {
                // FILTRO: solo nodos de la conversación activa
                if (nodeData.s_ConversationGuid != currentConversation) continue;

                var node = graphView.CreateNodeFromData(nodeData);
                if (node == null || string.IsNullOrEmpty(node.m_Data.s_NGuid)) continue;
                createdNodes[node.m_Data.s_NGuid] = node;
            }

            // ── Edges ─────────────────────────────────────────────────────────
            foreach (var linkData in graphData.s_Links)
            {
                // FILTRO: solo links cuyos dos extremos estén en la conversación visible
                if (!createdNodes.TryGetValue(linkData.s_OutputNodeGuid, out var outputNode)) continue;
                if (!createdNodes.TryGetValue(linkData.s_InputNodeGuid, out var inputNode)) continue;
                if (inputNode.m_InputPort == null) continue;

                Port outputPort;
                if (!string.IsNullOrEmpty(linkData.s_OutputPortGuid))
                {
                    outputPort = outputNode.GetReplyPort(linkData.s_OutputPortGuid);
                    if (outputPort == null) continue;
                }
                else
                {
                    outputPort = outputNode.m_OutputPort;
                    if (outputPort == null) continue;
                }

                var edge = outputPort.ConnectTo(inputNode.m_InputPort);
                graphView.AddElement(edge);
            }

            foreach (var node in createdNodes.Values)
            {
                node.RefreshPorts();
                node.RefreshExpandedState();
            }
        }
    }
}